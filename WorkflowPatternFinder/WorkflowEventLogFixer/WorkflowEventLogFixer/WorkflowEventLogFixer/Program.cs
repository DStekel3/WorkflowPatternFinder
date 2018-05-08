using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OfficeOpenXml;

namespace WorkflowEventLogFixer
{
  public static class Program
  {
    private static string _baseDirectory = "";
    private static string _baseCsvFileDirectory = Path.Combine(_baseDirectory, "csv");
    private static string _baseXesFileDirectory = Path.Combine(_baseDirectory, "xes");
    private static string _basePtmlFileDirectory = Path.Combine(_baseDirectory, "ptml");
    private static string _basePnmlFileDirectory = Path.Combine(_baseDirectory, "pnml");
    private static string _baseEtmDirectory = Path.Combine(_baseDirectory, "etm");
    private static string _PTQualityOutputFile = Path.Combine(_baseDirectory, "PTQ_output.txt");
    private static string _pythonExe = "";
    private static string _javaExe = "";
    private static string _word2VecScriptFile = @"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\TrainWord2VecModel.py";
    private static string _processTreeScriptFile = @"C:\Users\dst\eclipse-workspace\ProM\ProcessTreeMiner.txt";
    private static string _petriNetScriptFile = @"C:\Users\dst\eclipse-workspace\ProM\PetriNetMiner.txt";
    private static string _etmScriptFile = @"C:\Users\dst\eclipse-workspace\ProM\EvolutionaryTreeMiner.txt";
    private static string _PTQualityScriptFile = @"C:\Users\dst\eclipse-workspace\ProM\MinePTQuality.txt";
    private static string _promCLI = @"C:\Users\dst\eclipse-workspace\ProM\ProM_CLI.bat";

    private static List<string> _modelQualityCache = new List<string>();

    //convert each event log to:
    // 1. A csv-file, which is filtered on workflow instances.
    // 2. A xes-file, which is needed for further workflow analysis.


    public static void Main(string[] args)
    {

    }

    public static void PreProcessingPhase(string importDir, string promScript, string noiseThreshold)
    {
      InitializePaths(importDir, promScript);

      CreateDirectoriesIfNeeded();

      if(CheckIfPythonAndJavaAreInstalled())
      {
        CheckExistanceOfScriptFiles();

        //var files = Directory.EnumerateFiles(_baseDirectory).Where(c => c.EndsWith(".xlsx")).ToList();

        //var workflowNames = new Dictionary<string, string>();

        //for(int t = 0; t < files.Count; t++)
        //{
        //  var file = files[t];
        //  Console.WriteLine($"Busy with {Path.GetFileNameWithoutExtension(file)}...({t + 1}/{files.Count})");
        //  var workflowNamesFound = SplitExcelFileIntoSeparateWorkflowLogs(file);
        //  foreach(var name in workflowNamesFound)
        //  {
        //    workflowNames.Add(name.Key, name.Value);
        //  }
        //}

        // Write workflow descriptions to a separate file. This way, we can find the name of a workflow model by reading within this file.
        var workflowNameFile = Path.Combine(_baseDirectory, "workflownames.csv");
        //WriteWorkflowNamesToFile(workflowNames, workflowNameFile);

        //Apply word2vec throughout the workflow logs and give similar events similar names.
        //ApplyWord2VecThroughGensimScript(_baseCsvFileDirectory);

        //Console.WriteLine("Creating XES files...");
        //ConvertCsvToXesFiles();

        // Create ptml files
        //CreatePtmlFilesWithImi(noiseThreshold);

        // Create pnml files
        //CreatePnmlFiles();

        // Create ptml files with Evolutionary Tree Miner
        //CreatePtmlFilesWithETM();

        // Compute quality dimensions
        MineProcessTreeQuality();
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
      _basePnmlFileDirectory = Path.Combine(_baseDirectory, "pnml");
      _baseEtmDirectory = Path.Combine(_baseDirectory, "etm");
      _PTQualityOutputFile = Path.Combine(_baseDirectory, "PTQ_output.txt");
      _modelQualityCache.Clear();
    }

    public static void RemakeProcessTrees(string importDir, string promPath, string noiseThreshold)
    {
      InitializePaths(importDir, promPath);

      // Create ptml files with the inductive miner (infrequent)
      CreatePtmlFilesWithImi(noiseThreshold);

      // Create pnml files
      CreatePnmlFiles();

      //Create ptml files with the evolutionary tree miner
      CreatePtmlFilesWithETM();

      // Compute quality dimensions
      MineProcessTreeQuality();
    }

    private static void CreatePtmlFilesWithETM()
    {
      UpdatePathsAndNoiseThresholdInEtmScript();
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        CreateNoWindow = true,
        WorkingDirectory = Path.GetDirectoryName(_etmScriptFile) ?? throw new InvalidOperationException(),
        FileName = "ProM_CLI.bat",
        Arguments = $"-f {Path.GetFileName(_etmScriptFile)}"
      };
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
    }

    private static void UpdatePathsAndNoiseThresholdInEtmScript()
    {
      // new path lines in script
      string xesLineToWrite = $"xesDirectoryPath = \"{_baseXesFileDirectory}\\\";".Replace("\\", "\\\\");
      string etmLineToWrite = $"etmDirectoryPath = \"{_baseEtmDirectory}\\\";".Replace("\\", "\\\\");

      if(File.Exists(_etmScriptFile))
      {
        string[] lines = File.ReadAllLines(_etmScriptFile);

        if(lines.Length > 0)
        {
          // Write the new file over the old file.
          using(StreamWriter writer = new StreamWriter(_etmScriptFile))
          {
            for(int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
              if(lines[currentLine].Contains("xesDirectoryPath ="))
              {
                writer.WriteLine(xesLineToWrite);
              }
              else if(lines[currentLine].Contains("etmDirectoryPath ="))
              {
                writer.WriteLine(etmLineToWrite);
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

    private static void CreatePnmlFiles()
    {
      UpdatePathsInPetriNetScript(_petriNetScriptFile, _baseXesFileDirectory, _basePnmlFileDirectory);
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        CreateNoWindow = true,
        WorkingDirectory = Path.GetDirectoryName(_petriNetScriptFile) ?? throw new InvalidOperationException(),
        FileName = "ProM_CLI.bat",
        Arguments = $"-f {Path.GetFileName(_petriNetScriptFile)}"
      };
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
    }

    private static void UpdatePathsInPetriNetScript(string scriptPath, string xesDirectoryPath, string basePnmlFileDirectory)
    {
      // new path lines in script
      string xesLineToWrite = $"xesDirectoryPath = \"{xesDirectoryPath}\\\";".Replace("\\", "\\\\");
      string ptmlLineToWrite = $"pnmlDirectoryPath = \"{basePnmlFileDirectory}\\\";".Replace("\\", "\\\\");

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
      Process cmd = new Process();
      cmd.StartInfo = info;
      cmd.Start();
      cmd.WaitForExit();
      var pythonPath = cmd.StandardOutput.ReadLine();
      cmd.Close();

      if(File.Exists(pythonPath) && pythonPath.EndsWith(".exe"))
      {
        _pythonExe = pythonPath;
      }
      else
      {
        MessageBox.Show("Python is not installed.");
        return false;
      }
      cmd.StartInfo.Arguments = "/C where java";
      cmd.Start();
      cmd.WaitForExit();
      var javaPath = cmd.StandardOutput.ReadLine();
      cmd.Close();

      if(File.Exists(javaPath) && javaPath.EndsWith(".exe"))
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

    private static ProcessTree LoadSingleTree(string filePath)
    {
      return ProcessTreeLoader.LoadTree(filePath);
    }


    private static void CheckExistanceOfScriptFiles()
    {
      if(!File.Exists(_pythonExe))
      {
        throw new Exception("Python executable not found.");
      }
      if(!File.Exists(_word2VecScriptFile))
      {
        throw new Exception("Word2Vec script not found.");
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
      if(!Directory.Exists(_basePnmlFileDirectory))
      {
        Directory.CreateDirectory(_basePnmlFileDirectory);
      }
      if(!Directory.Exists(_baseEtmDirectory))
      {
        Directory.CreateDirectory(_baseEtmDirectory);
      }
    }

    /// <summary>
    /// Calls inductive miner script which input xes-directory and outputs ptml files.
    /// </summary>
    /// <param name="processTreeScriptFile"></param>
    private static void CreatePtmlFilesWithImi(string noiseThreshold)
    {
      UpdatePathsAndNoiseThresholdInProcessTreeScript(_processTreeScriptFile, _baseXesFileDirectory, _basePtmlFileDirectory, noiseThreshold);
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        CreateNoWindow = true,
        WorkingDirectory = Path.GetDirectoryName(_processTreeScriptFile) ?? throw new InvalidOperationException(),
        FileName = "ProM_CLI.bat",
        Arguments = $"-f {Path.GetFileName(_processTreeScriptFile)}"
      };
      process.StartInfo = startInfo;
      process.Start();
      process.WaitForExit();
    }

    private static Dictionary<string, string> SplitExcelFileIntoSeparateWorkflowLogs(string file)
    {
      var events = GetEvents(file);
      var groups = events.GroupBy(e => e.WorkflowID);
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
      using(var writer = new StreamWriter(workflowNameFile))
      {
        foreach(var name in workflowNames)
        {
          var row = $"{name.Key};{name.Value}";
          writer.WriteLine(row);
        }
      }
      File.SetAttributes(workflowNameFile, FileAttributes.ReadOnly);
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
            EventID = row[0],
            Doorlooptijd = row[1],
            WorkflowID = row[2],
            WorkflowOmschrijving = row[3],
            InstanceID = row[4],
            TypeDossierItem = row[5],
            TaakID = row[6],
            TaakOmschrijving = row[7],
            ActieID = row[8],
            ActieType = row[9],
            ActieOmschrijving = row[10],
            ActieBijschrift = row[11],
            Begin = row[12],
            Eind = row[13]
          });
        }

        return events
          .OrderBy(e => e.WorkflowID).ToList()
          .OrderBy(e => e.InstanceID).ToList()
          .OrderBy(e => e.Eind).ToList();
      }
    }

    private static void ConvertCsvToXesFiles()
    {
      var startInfo = new ProcessStartInfo
      {
        WindowStyle = ProcessWindowStyle.Maximized,
        UseShellExecute = false,
        FileName = @"C:\Users\dst\Source\Repos\CSV-to-XES\CSVtoXES\CsvToXesDirectory.bat",
        Arguments = $"\"{_javaExe}\" \"{_baseCsvFileDirectory}\" \"{_baseXesFileDirectory}\"",
        WorkingDirectory = @"C:\Users\dst\Source\Repos\CSV-to-XES\CSVtoXES"
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
        if(!badInstances.Contains(currentEvent.InstanceID))
        {
          if(!EventContainsNoise(currentEvent))
          {
            if(currentEvent.InstanceID != currentInstance)
            {
              totalEventLog.AddRange(dossierItemEvents);
              currentInstance = currentEvent.InstanceID;
              dossierItemEvents.Clear();
            }

            if(!activityKeys.ContainsKey($"{currentEvent.TaakID}:{currentEvent.ActieID}"))
            {
              activityKeys.Add($"{currentEvent.TaakID}:{currentEvent.ActieID}", $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}");
            }

            if(activityKeys[$"{currentEvent.TaakID}:{currentEvent.ActieID}"] != $"{currentEvent.TaakOmschrijving}:{currentEvent.ActieOmschrijving}")
            {
              Console.WriteLine($"{currentEvent.WorkflowID}");
              dossierItemEvents.Clear();
              break;
            }

            dossierItemEvents.Add(currentEvent);
          }
          else
          {
            badInstances.Add(currentEvent.InstanceID);
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
      _petriNetScriptFile = Path.Combine(promBasePath, PromCustomFileNames.GetPetriNetMiner());
      _etmScriptFile = Path.Combine(promBasePath, PromCustomFileNames.GetEvoluationaryTreeMiner());
      _PTQualityScriptFile = Path.Combine(promBasePath, PromCustomFileNames.GetPTQualityMiner());
      _promCLI = Path.Combine(promBasePath, PromCustomFileNames.GetCLI());
    }

    private static bool EventContainsNoise(Event currentEvent)
    {
      if(!int.TryParse(currentEvent.InstanceID, out int a))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.TaakID, out int b))
      {
        return true;
      }
      if(!int.TryParse(currentEvent.ActieID, out int c))
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
        Arguments = $"\"{_word2VecScriptFile}\" \"{csvDirectory}\"",
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
        WindowStyle = ProcessWindowStyle.Maximized
      };
      var process = Process.Start(start);
      process.WaitForExit();
      process.Close();
    }

    public static bool DoesProcessTreeQualityFileExist()
    {
      return File.Exists(_PTQualityOutputFile);
    }

    public static string GetProcessTreeQuality(string ptmlFile)
    {
      if(!_modelQualityCache.Any())
      {
        _modelQualityCache = File.ReadAllLines(_PTQualityOutputFile).ToList();
      }

      var resultingLine = _modelQualityCache.SingleOrDefault(f => f.StartsWith(ptmlFile));
      if(resultingLine != null)
      {
        return resultingLine.Split(';')[1].Split(' ')[0];
      }
      return "-";
    }

    public static void MineProcessTreeQuality()
    {
      UpdatePathsInMinePTQualityScript(_PTQualityScriptFile, _baseXesFileDirectory, _basePtmlFileDirectory);
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        WorkingDirectory = Path.GetDirectoryName(_PTQualityScriptFile) ?? throw new InvalidOperationException(),
        FileName = _promCLI,
        Arguments = $"-f {Path.GetFileName(_PTQualityScriptFile)}",
        UseShellExecute = false
      };
      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      var p = Process.Start(startInfo);
      p.WaitForExit();
      p.Close();
    }

    private static void UpdatePathsInPTQualityScript(string scriptPath, string xesPath, string treePath)
    {
      // new path lines in script
      string xesLineToWrite = $"xesPath = \"{xesPath}\";".Replace("\\", "\\\\");
      string treeLineToWrite = $"treePath = \"{treePath}\";".Replace("\\", "\\\\");

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
                writer.WriteLine(treeLineToWrite);
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


    private static void UpdatePathsInMinePTQualityScript(string scriptFile, string xesDirectoryPath, string ptmlDirectoryPath)
    {
      // new path lines in script
      string xesLineToWrite = $"xesPath = \"{xesDirectoryPath}\\\";".Replace("\\", "\\\\");
      string treeLineToWrite = $"treePath = \"{ptmlDirectoryPath}\\\";".Replace("\\", "\\\\");

      if(File.Exists(scriptFile))
      {
        string[] lines = File.ReadAllLines(scriptFile);

        if(lines.Length > 0)
        {
          // Write the new file over the old file.
          using(StreamWriter writer = new StreamWriter(scriptFile))
          {
            for(int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
              if(currentLine == 2)
              {
                writer.WriteLine(xesLineToWrite);
              }
              else if(currentLine == 3)
              {
                writer.WriteLine(treeLineToWrite);
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

    

    public static string GetPythonExe()
    {
      return _pythonExe;
    }

    public static List<string> GetSimilarTerms(string scriptPath, string modelpath, string currentTerm)
    {
      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = _pythonExe,
        Arguments = $"\"{scriptPath}\" \"{modelpath}\" \"{currentTerm}\"",
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Maximized,
        RedirectStandardOutput = true
      };
      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      using(var process = Process.Start(start))
      {
        using(StreamReader reader = process?.StandardOutput)
        {
          var output = reader?.ReadToEnd().Replace("\r\n", "|").Split('|').ToList();
          return output;
        }
      }
    }

    public static List<string> GetSentences(string scriptPath, string modelpath, string currentTerm)
    {
      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = _pythonExe,
        Arguments = $"\"{scriptPath}\" \"{modelpath}\" \"{currentTerm}\"",
        UseShellExecute = false,
        WindowStyle = ProcessWindowStyle.Maximized,
        RedirectStandardOutput = true
      };
      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      using(var process = Process.Start(start))
      {
        using(StreamReader reader = process?.StandardOutput)
        {
          var output = reader?.ReadToEnd().Replace("\r\n", "|").Split('|').ToList();
          return output;
        }
      }
    }
  }
}