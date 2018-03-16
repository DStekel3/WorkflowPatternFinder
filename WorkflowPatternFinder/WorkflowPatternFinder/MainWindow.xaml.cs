﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using IronPython.Hosting;
using WorkflowEventLogFixer;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.Forms.DataFormats;
using Timer = System.Windows.Forms.Timer;

namespace WorkflowPatternFinder
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private bool _treatAsInducedSubTree = false;
    private string _notePadPath;
    private string _importExcelDir;
    private string _promScriptPath;
    private string _incorrectDir = "Incorrect directory!";
    private string _incorrectFile = "Incorrect file!";

    public MainWindow()
    {
      InitializeComponent();
      FindNotePadPath();
    }

    private void FindNotePadPath()
    {
      var standardPath = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
      if(File.Exists(standardPath))
      {
        _notePadPath = standardPath;
      }
      else
      {
        Process cmd = new Process();
        cmd.StartInfo.FileName = "cmd.exe";
        cmd.StartInfo.Arguments = "/C where notepad";
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();
        cmd.WaitForExit();
        var installPath = cmd.StandardOutput.ReadLine();
        if(installPath.Contains("notepad") && installPath.EndsWith(".exe"))
        {
          _notePadPath = installPath;
        }
      }
    }

    private void ImportTreeButton_Click(object sender, RoutedEventArgs e)
    {
      FolderBrowserDialog fbd = new FolderBrowserDialog();
      fbd.Description = "Select a directory that contains .ptml files.";
      fbd.SelectedPath = @"C:\Thesis\Profit analyses\testmap\ptml";
      DialogResult result = fbd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(Directory.EnumerateFiles(fbd.SelectedPath).Any(file => file.EndsWith(".ptml")))
        {
          ImportTreeLabel.Content = fbd.SelectedPath;
        }
        else
        {
          UpdateButtonText(ImportTreeButton, _incorrectDir, 3000);
        }
      }
    }

    private void ImportPatternButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.DefaultExt = ".ptml";
      ofd.Title = "Select a file containing your pattern (.ptml).";
      ofd.FileName = @"C:\Thesis\Profit analyses\22-02-2018\testPattern.ptml";
      DialogResult result = ofd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(ofd.FileName.EndsWith(".ptml"))
        {
          ImportPatternLabel.Content = ofd.FileName;
        }
        else
        {
          UpdateButtonText(ImportPatternButton, _incorrectFile, 3000);
        }
      }
    }

    private void TreeStartButton_Click(object sender, RoutedEventArgs e)
    {
      ClearValidOccurencesList();
      if(!PathsExists())
      {
        UpdateButtonText(TreeStartButton, "Incorrect inputs!");
        return;
      }
      TreeStartButton.Content = "Busy...";

      ChangeEnabledTreeButtons(false);
      TreeProgressBar.IsIndeterminate = true;
      
      var induced = _treatAsInducedSubTree;

      // update the python .exe path
      if(string.IsNullOrEmpty(SubTreeFinder._pythonExe))
      {
        if(Program.CheckIfPythonAndJavaAreInstalled())
        {
          SubTreeFinder.SetPythonExe(Program.GetPythonExe());
        }
      }

      var validSubTrees = CallGensim(induced);
      ResultDebug.Content = $"Found {validSubTrees.Count} model(s) that contain the given pattern.";

      TreeStartButton.Content = "Start mining...";
      UpdateButtonText(TreeStartButton, "Done!");
      ClearDebugLabel();
      ChangeEnabledTreeButtons(true);
      TreeProgressBar.IsIndeterminate = false;
    }

    private List<string> CallGensim(bool induced)
    {
      List<string> properSubTrees = new List<string>();
      var treeBasePath = ImportTreeLabel.Content.ToString();
      var patternPath = ImportPatternLabel.Content.ToString();
      var modelPath = Path.Combine(Directory.GetParent(ImportTreeLabel.Content.ToString()).FullName, "trained.gz");
      var scriptPath = @"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\Gensim.py";

      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = Program.GetPythonExe(),
        Arguments = $"\"{scriptPath}\" \"{modelPath}\" \"{treeBasePath}\" \"{patternPath}\" \"{induced}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true
      };
      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      using(Process process = Process.Start(start))
      {
        using(StreamReader reader = process?.StandardOutput)
        {
          string result = reader?.ReadToEnd();
          var lines = result.Replace("\r\n", "|").Split('|').ToList();
          foreach(var line in lines)
            Debug.WriteLine(line);
          var validSubTrees = lines.SkipWhile(c => !c.StartsWith("Valid trees:")).Skip(1);
          foreach(string treePath in validSubTrees)
          {
            if(File.Exists(treePath))
            {
              properSubTrees.Add(treePath);
              ValidOccurencesList.Items.Add(treePath);
              Debug.WriteLine($"{treePath} is a subtree!");
            }
          }
        }
      }
      return properSubTrees;
    }

    public static void RunScript(string scriptPath, string modelPath, string treePath, string patternPath)
    {
      var pythonPath = Program.GetPythonExe();
      var options = new Dictionary<string, object> { ["Debug"] = true };
      var engine = Python.CreateEngine(options);
      var script = engine.CreateScriptSourceFromFile(Path.Combine(pythonPath, "movement.py"));
      var scope = engine.CreateScope();

      List<string> argv = new List<string>
      {
        $"\"{@"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\Gensim.py"}\"",
        $"\"{modelPath}\"",
        $"\"{treePath}\"," +
        $"\"{patternPath}\""
      };
      engine.GetSysModule().SetVariable("argv", argv);
      var py = engine.ExecuteFile(scriptPath).GetVariable("result");

    }

    private void InducedCheckBox_Click(object sender, RoutedEventArgs e)
    {
      if(InducedCheckBox.IsChecked == true)
      {
        _treatAsInducedSubTree = true;
      }
      else
      {
        _treatAsInducedSubTree = false;
      }
    }

    private void ValidOccurencesList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ValidOccurencesList.SelectedItem.ToString();
      OpenFileInNotePad(selectedFile);
    }

    private void ImportTreeLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ImportTreeLabel.Content.ToString();
      OpenDirectoryInExplorer(selectedFile);
    }

    private void ImportPatternLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ImportPatternLabel.Content.ToString();
      OpenFileInNotePad(selectedFile);
    }

    private void PreProcessingButton_Click(object sender, RoutedEventArgs e)
    {
      ChangeEnabledPreProcessingButtons(false);
      _importExcelDir = ImportExcelDirectoryLabel.Content.ToString();
      _promScriptPath = PromScriptLabel.Content.ToString();

      Task DoWork()
      {
        var tasks = new List<Task>
        {
          Task.Run((Action)StartPreprocessing)
        };
        return Task.WhenAll(tasks);
      }
      StartTask(DoWork);
    }


    void StartPreprocessing()
    {
      Program.PreProcessingPhase(_importExcelDir, _promScriptPath);
    }

    private void StartTask(Func<Task> task, Action completedTask = null)
    {
      ConsoleLabel.Content = "Busy...";
      PreProgress.IsIndeterminate = true;

      var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Task.Factory
        .StartNew(() =>
          task()
            .ContinueWith(async t =>
            {
              await FinishTask(t.Exception);
              completedTask?.Invoke();
            }, scheduler));
    }

    private async Task FinishTask(Exception ex)
    {
      UpdateButtonText(PreProcessingButton, "Done!");
      ConsoleLabel.Content = "Start...";
      ChangeEnabledPreProcessingButtons(true);
      PreProgress.IsIndeterminate = false;
    }

    private void ImportExcelDirectoryLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ImportExcelDirectoryLabel.Content.ToString();
      OpenDirectoryInExplorer(selectedFile);
    }

    private void ImportExcelDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
      FolderBrowserDialog fbd = new FolderBrowserDialog();
      fbd.Description = "Select a directory that contains workflow logs in .xlsx format.";
      fbd.SelectedPath = @"C:\Thesis\Profit analyses\testmap";
      DialogResult result = fbd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        UpdateExcelDirectoryUI(fbd.SelectedPath);
      }
    }

    private void UpdateExcelDirectoryUI(string path)
    {
      if(Directory.EnumerateFiles(path).Any(s => s.EndsWith(".xlsx")))
      {
        ImportExcelDirectoryLabel.Content = path;
      }
      else
      {
        UpdateButtonText(ImportExcelDirectoryButton, _incorrectDir, 3000);
      }
    }

    private void PromScriptLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = PromScriptLabel.Content.ToString();
      OpenFileInNotePad(selectedFile);
    }

    private void PromScriptButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.FileName = @"C:\Users\dst\eclipse-workspace\ProM\ProcessTreeMiner.txt";
      DialogResult result = ofd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(ofd.FileName.EndsWith(".txt"))
        {
          PromScriptLabel.Content = ofd.FileName;
        }
        else
        {
          UpdateButtonText(PromScriptButton, _incorrectFile, 3000);
        }
      }
    }

    private void ChangeEnabledPreProcessingButtons(bool isEnabled)
    {
      // pre processing buttons
      PreProcessingButton.IsEnabled = isEnabled;
      ImportExcelDirectoryButton.IsEnabled = isEnabled;
      PromScriptButton.IsEnabled = isEnabled;
    }

    private void ChangeEnabledTreeButtons(bool isEnabled)
    {
      //tree finder buttons
      TreeStartButton.IsEnabled = isEnabled;
      ImportPatternButton.IsEnabled = isEnabled;
      ImportTreeButton.IsEnabled = isEnabled;
      InducedCheckBox.IsEnabled = isEnabled;
    }

    private void ClearValidOccurencesList()
    {
      ValidOccurencesList.Items.Clear();
    }

    private void ClearDebugLabel(int interval = 5000)
    {
      var timer = new Timer { Interval = interval };
      timer.Tick += (s, f) =>
      {
        DebugLabel.Content = "";
        timer.Stop();
      };
      timer.Start();
    }

    private bool PathsExists()
    {
      if(File.Exists(ImportPatternLabel.Content.ToString()))
      {
        if(Directory.Exists(ImportTreeLabel.Content.ToString()))
        {
          return true;
        }
      }
      return false;
    }

    private void UpdateButtonText(Button button, string message, int interval = 5000)
    {
      var normal = button.Content.ToString();
      var timer = new Timer { Interval = interval };
      timer.Tick += (s, f) =>
      {
        button.Content = normal;
        button.Foreground = Brushes.Black;
        timer.Stop();
      };
      timer.Start();
      button.Content = message;
      button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
    }

    private void OpenFileInNotePad(string filePath)
    {
      if(File.Exists(filePath))
      {
        Process.Start(_notePadPath, filePath);
      }
    }

    private void OpenDirectoryInExplorer(string directoryPath)
    {
      if(Directory.Exists(directoryPath))
      {
        Process.Start(directoryPath);
      }
    }

    private void ExcelDrop(object sender, System.Windows.DragEventArgs e)
    {
      if(e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        // Note that you can have more than one file.
        var file = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(Directory.Exists).First();
        UpdateExcelDirectoryUI(file);
      }
    }

    private void ShowModel_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
