using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using IronPython.Modules;
using OfficeOpenXml;

namespace WorkflowEventLogProcessor
{
  public static class Program
  {
    private static string _baseDirectory = "";
    private static readonly string BasePromPath = GetProMBasePath();
    private static readonly string BaseToolPath = GetToolBasePath();
    private static string _baseCsvFileDirectory = Path.Combine(_baseDirectory, "csv");
    private static string _baseXesFileDirectory = Path.Combine(_baseDirectory, "xes");
    private static string _basePtmlFileDirectory = Path.Combine(_baseDirectory, "ptml");
    private static string _pythonExe = "";
    private static string _javaExe = "";
    private static readonly string Word2VecScriptFile = Path.Combine(BaseToolPath, @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\TrainWord2VecModel.py");
    private static string _processTreeScriptFile = Path.Combine(BasePromPath, "ProcessTreeMiner.txt");
    private static string _promCli = Path.Combine(BasePromPath, "ProM_CLI.bat");
    private static readonly string CsvToXesBat = Path.Combine(BaseToolPath,
      @"WorkflowPatternFinder\WorkflowPatternFinder\WorkflowEventLogProcessor\WorkflowEventLogProcessor\WorkflowEventLogProcessor\CSVtoXES\CsvToXesDirectory.bat");

    private static readonly List<string> ModelQualityCache = new List<string>();

    //convert each event log to:
    // 1. A csv-file, which is filtered on workflow instances.
    // 2. A xes-file, which is needed for further workflow analysis.

    public static void Main(string[] args)
    {

    }

    public static string GetProMBasePath()
    {
      return ConfigurationManager.AppSettings["PromBasePath"];
    }

    public static string GetToolBasePath()
    {
      return ConfigurationManager.AppSettings["ToolBasePath"];
    }

    public static string GetDatasetBasePath()
    {
      return ConfigurationManager.AppSettings["DatasetBasePath"];
    }

    public static string GetWord2VecBasePath()
    {
      return Path.Combine(GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\");
    }

    public static void PreProcessingPhase(string importDir, string promScript, string noiseThreshold, bool completePreprocessing)
    {
      InitializePaths(importDir, promScript);

      CreateDirectoriesIfNeeded();

      if(CheckIfPythonAndJavaAreInstalled())
      {
        CheckExistanceOfScriptFiles();

        // The all pre-processing steps. (Creating csv- and xes-files)
        if (completePreprocessing)
        {
          var files = Directory.EnumerateFiles(_baseDirectory).Where(c => c.EndsWith(".xlsx")).ToList();

          var workflowNames = new Dictionary<string, string>();

          for (int t = 0; t < files.Count; t++)
          {
            var file = files[t];
            Console.WriteLine($"Busy with {Path.GetFileNameWithoutExtension(file)}...({t + 1}/{files.Count})");
            var workflowNamesFound = SplitExcelFileIntoSeparateWorkflowLogs(file);
            foreach (var name in workflowNamesFound)
            {
              workflowNames.Add(name.Key, name.Value);
            }
          }

          // Write workflow descriptions to a separate file. This way, we can find the name of a workflow model by reading within this file.
          var workflowNameFile = Path.Combine(_baseDirectory, "workflownames.csv");
          WriteWorkflowNamesToFile(workflowNames, workflowNameFile);

          //Creating XES files
          ConvertCsvToXesFiles();
        }

        // Create ptml files
        CreatePtmlFilesWithImi(noiseThreshold);
      }
    }

    public static void InitializePaths(string importDir, string promPath = null)
    {
      if(!string.IsNullOrEmpty(promPath))
      {
        UpdateScriptFilePaths(promPath);
      }
      _baseDirectory = importDir;

      // update all directory paths
      _baseCsvFileDirectory = Path.Combine(_baseDirectory, "csv");
      _baseXesFileDirectory = Path.Combine(_baseDirectory, "xes");
      _basePtmlFileDirectory = Path.Combine(_baseDirectory, "ptml");
      ModelQualityCache.Clear();
    }
    
    public static bool CheckIfPythonAndJavaAreInstalled()
    {
      ProcessStartInfo info = new ProcessStartInfo
      {
        FileName = "cmd.exe",
        Arguments = "/C where python",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      var cmd = new Process();
      cmd.StartInfo = info;
      cmd.Start();
      cmd.WaitForExit();
      var pythonPath = cmd.StandardOutput.ReadLine();
      cmd.Close();

      if(pythonPath != null && (File.Exists(pythonPath) && pythonPath.EndsWith(".exe")))
      {
        _pythonExe = pythonPath;
      }
      else
      {
        MessageBox.Show("Python is not installed.");
        //return false;
      }
      cmd.StartInfo.Arguments = "/C where java";
      cmd.Start();
      cmd.WaitForExit();
      var javaPath = cmd.StandardOutput.ReadLine();
      cmd.Close();

      if(javaPath != null && (File.Exists(javaPath) && javaPath.EndsWith(".exe")))
      {
        _javaExe = javaPath;
      }
      else
      {
        MessageBox.Show("Java is not installed.");
        return false;
      }
      return true;
    }


    public static List<ProcessTree> LoadProcessTrees(string basePtmlFileDirectory)
    {
      return Directory.EnumerateFiles(basePtmlFileDirectory).Select(file => ProcessTreeLoader.LoadTree(file)).ToList();
    }

    private static void CheckExistanceOfScriptFiles()
    {
      if(!File.Exists(_pythonExe))
      {
        throw new Exception("Python executable not found.");
      }
      if(!File.Exists(_processTreeScriptFile))
      {
        throw new Exception("Process tree script not found.");
      }
    }

    private static void CreateDirectoriesIfNeeded()
    {
      if(!Directory.Exists(_baseDirectory))
      {
        throw new Exception("This directory does not exist.");
      }
      if(!Directory.Exists(_baseCsvFileDirectory))
      {
        Directory.CreateDirectory(_baseCsvFileDirectory);
      }
      if(!Directory.Exists(_baseXesFileDirectory))
      {
        Directory.CreateDirectory(_baseXesFileDirectory);
      }
      if(!Directory.Exists(_basePtmlFileDirectory))
      {
        Directory.CreateDirectory(_basePtmlFileDirectory);
      }
    }

    /// <summary>
    /// Calls inductive miner script which input xes-directory and outputs ptml files.
    /// </summary>
    private static void CreatePtmlFilesWithImi(string noiseThreshold)
    {
      UpdatePathsAndNoiseThresholdInProcessTreeScript(_processTreeScriptFile, _baseXesFileDirectory, _basePtmlFileDirectory, noiseThreshold);
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        CreateNoWindow = true,
        WorkingDirectory = BasePromPath ?? throw new InvalidOperationException(),
        FileName = _promCli,
        Arguments = $"-f {Path.GetFileName(_processTreeScriptFile)}",
        WindowStyle = ProcessWindowStyle.Hidden
      };
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
    }

    private static Dictionary<string, string> SplitExcelFileIntoSeparateWorkflowLogs(string file)
    {
      var events = GetEvents(file);
      var groups = events.GroupBy(e => e.WorkflowId);
      Dictionary<string, string> workflowNames = new Dictionary<string, string>();
      foreach(var group in groups)
      {
        string csvFile = $"{Path.Combine(_baseCsvFileDirectory, Path.GetFileNameWithoutExtension(file) ?? throw new InvalidOperationException())}-{group.Key}.csv";
        workflowNames.Add(Path.GetFileNameWithoutExtension(csvFile), group.First().WorkflowOmschrijving);
        var filteredEvents = FilterEvents(group.ToList());
        WriteCsv(filteredEvents, csvFile);
      }
      return workflowNames;
    }

    private static void WriteWorkflowNamesToFile(Dictionary<string, string> workflowNames, string workflowNameFile)
    {
      if(File.Exists(workflowNameFile))
      {
        File.Delete(workflowNameFile);
      }
      using(var writer = new StreamWriter(workflowNameFile))
      {
        foreach(var name in workflowNames)
        {
          var row = $"{name.Key};{name.Value}";
          writer.WriteLine(row);
        }
      }
    }

    static List<Event> GetEvents(string excelFile)
    {
      using(ExcelPackage xlPackage = new ExcelPackage(new FileInfo(excelFile)))
      {
        var myWorksheet = xlPackage.Workbook.Worksheets.First();
        var totalRows = myWorksheet.Dimension.End.Row;
        var totalColumns = myWorksheet.Dimension.End.Column;

        var events = new List<Event>();
        for(int rowNum = 2; rowNum <= totalRows; rowNum++)
        {
          var row = myWorksheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value?.ToString() ?? string.Empty).ToList();


          events.Add(new Event
          {
            EventId = row[0],
            Doorlooptijd = row[1],
            WorkflowId = row[2],
            WorkflowOmschrijving = row[3],
            InstanceId = row[4],
            TypeDossierItem = row[5],
            TaakId = row[6],
            TaakOmschrijving = row[7],
            ActieId = row[8],
            ActieType = row[9],
            ActieOmschrijving = row[10],
            ActieBijschrift = row[11],
            Begin = row[12],
            Eind = row[13]
          });
        }

        return events
          .OrderBy(e => e.WorkflowId).ToList()
          .OrderBy(e => e.InstanceId).ToList()
          .OrderBy(e => e.Eind).ToList();
      }
    }

    private static void ConvertCsvToXesFiles()
    {
      var jarFile = Path.Combine(BaseToolPath, @"WorkflowPatternFinder\WorkflowPatternFinder\WorkflowEventLogProcessor\WorkflowEventLogProcessor\WorkflowEventLogProcessor\CSVtoXES\CSVtoXESDir.jar");
      var startInfo = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Hidden,
        UseShellExecute = false,
        FileName = CsvToXesBat,
        Arguments = $"\"{_javaExe}\" \"{jarFile}\" \"{_baseCsvFileDirectory}\" \"{_baseXesFileDirectory}\"",
        WorkingDirectory = Path.GetDirectoryName(CsvToXesBat) ?? throw new InvalidOperationException()
      };

      Process process = new Process { StartInfo = startInfo };
      process.Start();
      process.WaitForExit();
      process.Close();
    }

    public static List<XesObject> FilterEvents(List<Event> events)
    {
      var activityKeys = new Dictionary<string, string>();
      var totalEventLog = new List<Event>();
      var currentInstance = "-1";
      var dossierItemEvents = new List<Event>();
      events.Reverse();

      var badInstances = new List<string>();

      foreach(Event currentEvent in events)
      {
        if(!badInstances.Contains(currentEvent.InstanceId))
        {
          if(!EventContainsNoise(currentEvent))
          {
            if(currentEvent.InstanceId != currentInstance)
            {
              totalEventLog.AddRange(dossierItemEvents);
              currentInstance = currentEvent.InstanceId;
              dossierItemEvents.Clear();
            }

            if(!activityKeys.ContainsKey($"{currentEvent.TaakId}:{currentEvent.ActieId}"))
            {
              activityKeys.Add($"{currentEvent.TaakId}:{currentEvent.ActieId}", $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}");
            }

            if(activityKeys[$"{currentEvent.TaakId}:{currentEvent.ActieId}"] != $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}")
            {
              Console.WriteLine($"{currentEvent.WorkflowId}");
              dossierItemEvents.Clear();
              break;
            }

            dossierItemEvents.Add(currentEvent);
          }
          else
          {
            badInstances.Add(currentEvent.InstanceId);
          }
        }
      }
      if(badInstances.Any())
      {
        Console.WriteLine($"Removed {badInstances.Count} bad instances.");
      }
      totalEventLog.AddRange(dossierItemEvents);
      totalEventLog.Reverse();

      var filteredLog = new List<XesObject>();
      foreach(Event currentEvent in totalEventLog)
      {
        filteredLog.Add(new XesObject(currentEvent));
      }

      return filteredLog;
    }

    /// <summary>
    /// Update paths to ProM script files.
    /// </summary>
    /// <param name="promBasePath"></param>
    public static void UpdateScriptFilePaths(string promBasePath)
    {
      _processTreeScriptFile = Path.Combine(promBasePath, PromCustomFileNames.GetProcessTreeMiner());
      _promCli = Path.Combine(promBasePath, PromCustomFileNames.GetCli());
    }

    private static bool EventContainsNoise(Event currentEvent)
    {
      if(!int.TryParse(currentEvent.InstanceId, out int a))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.TaakId, out int b))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.ActieId, out int c))
      {
        return true;
      }
      if(currentEvent.TaakOmschrijving == "NULL")
      {
        return true;
      }
      if(currentEvent.ActieOmschrijving == "NULL")
      {
        return true;
      }
      if(currentEvent.Eind == "NULL")
      {
        return true;
      }
      return false;
    }

    private static async void WriteCsv<T>(IEnumerable<T> items, string path)
    {
      var itemType = typeof(T);
      var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField)
        .OrderBy(p => p.Name).ToList();

      using(var writer = new StreamWriter(path))
      {
        string columnNames = string.Join(";", props.Select(p => p.Name));
        await writer.WriteLineAsync(columnNames);

        foreach(var item in items)
        {
          string row = string.Join(";", props.Select(p => p.GetValue(item, null)));
          await writer.WriteLineAsync(row.Replace("\"", ""));
        }
      }
    }

    /// <summary>
    /// Re-writes the xes- and ptml-directory paths in the script.
    /// </summary>
    /// <param name="scriptPath"></param>
    /// <param name="xesDirectoryPath">Directory which contains xes files. Used as input for the IM.</param>
    /// <param name="ptmlDirectoryPath">Directory where resulting ptml files are saved. Used as output for the IM.</param>
    private static void UpdatePathsAndNoiseThresholdInProcessTreeScript(string scriptPath, string xesDirectoryPath, string ptmlDirectoryPath, string noiseThreshold = "0.2")
    {
      // new path lines in script
      string xesLineToWrite = $"xesDirectoryPath = \"{xesDirectoryPath}\\\";".Replace("\\", "\\\\");
      string ptmlLineToWrite = $"ptmlDirectoryPath = \"{ptmlDirectoryPath}\\\";".Replace("\\", "\\\\");
      string noiseThresholdLine = $"parameters.setNoiseThreshold({noiseThreshold}f);";

      if(File.Exists(scriptPath))
      {
        string[] lines = File.ReadAllLines(scriptPath);

        if(lines.Length > 0)
        {
          // Write the new file over the old file.
          using(StreamWriter writer = new StreamWriter(scriptPath))
          {
            for(int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
              if(currentLine == 2)
              {
                writer.WriteLine(xesLineToWrite);
              }
              else if(currentLine == 3)
              {
                writer.WriteLine(ptmlLineToWrite);
              }
              else if(currentLine == 18)
              {
                writer.WriteLine(noiseThresholdLine);
              }
              else
              {
                writer.WriteLine(lines[currentLine]);
              }
            }
          }
        }
        else
        {
          throw new Exception("Script is empty.");
        }
      }
    }

    public static void ApplyWord2VecThroughGensimScript(string csvDirectory)
    {
      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = _pythonExe,
        Arguments = $"\"{Word2VecScriptFile}\" \"{csvDirectory}\"",
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Maximized
      };
      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      Process.Start(start);
    }

    public static void TrainWord2VecModel(string scriptFile, string csvDirectory, string windowSize, string minCount, string epochs)
    {
      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = _pythonExe,
        Arguments = $"\"{scriptFile}\" \"{csvDirectory}\" \"{windowSize}\" \"{minCount}\" \"{epochs}\"",
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Hidden
      };
      var process = Process.Start(start);
      process.WaitForExit();
      process.Close();
    }

    public static string GetPythonExe()
    {
      return _pythonExe;
    }

    public static List<string> GetSimilarTerms(string modelpath, string currentTerm)
    {
      var scriptPath = Path.Combine(GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\TermSimilarityQuery.py");

      var process = new Process
      {
        StartInfo =
        {
          FileName = _pythonExe,
          CreateNoWindow = true,
          Arguments = $"\"{scriptPath}\" \"{modelpath}\" \"{currentTerm}\"",
          UseShellExecute = false,
          WindowStyle = ProcessWindowStyle.Hidden,
          RedirectStandardOutput = true,
          }
      };
      process.Start();
      var output = process.StandardOutput.ReadToEnd().Replace("\r\n", "|").Split('|').ToList();
      return output;
    }

    public static List<string> GetSentences(string scriptPath, string modelpath, string currentTerm)
    {
      var process = new Process
      {
        StartInfo =
        {
          FileName = _pythonExe,
          Arguments = $"\"{scriptPath}\" \"{modelpath}\" \"{currentTerm}\"",
          UseShellExecute = false,
          CreateNoWindow = true,
          WindowStyle = ProcessWindowStyle.Hidden,
          RedirectStandardOutput = true
        }
      };

      process.Start();
        var output = process.StandardOutput.ReadToEnd().Replace("\r\n", "|").Split('|').ToList();
        return output;
    }
  }
}