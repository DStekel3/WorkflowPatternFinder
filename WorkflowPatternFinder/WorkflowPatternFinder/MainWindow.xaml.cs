using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using WorkflowEventLogFixer;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Button = System.Windows.Controls.Button;
using DataFormats = System.Windows.Forms.DataFormats;
using Timer = System.Windows.Forms.Timer;
using IronPython.Runtime.Operations;
using System.Globalization;
using System.Text;
using System.Threading;
using WorkflowPatternFinder.Properties;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace WorkflowPatternFinder
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private bool _treatAsInducedSubTree = false;
    private bool _countNumberOfPatternsWithinModel = false;
    private string _notePadPath;
    private string _importExcelDir;
    private string _promBasePath;
    private string _noiseThreshold;
    private string _incorrectDir = "Incorrect directory!";
    private string _incorrectFile = "Incorrect file!";
    private string _missingScriptFile = "Missing ProM scripts!";
    private string _helpText = "Fill in term...";
    private List<PatternObject> _foundPatterns = new List<PatternObject>();
    private Window tFinder;
    private Dictionary<string, ModelMatches> _modelMatches = new Dictionary<string, ModelMatches>();

    public MainWindow()
    {
      InitializeComponent();
      Program.CheckIfPythonAndJavaAreInstalled();
      FindNotePadPath();
    }

    private void FindNotePadPath()
    {
      var standardPath = @"C:\Program Files\Notepad++\notepad++.exe";
      var standardPathx86 = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
      if(File.Exists(standardPath))
      {
        _notePadPath = standardPath;
      }
      else if(File.Exists(standardPathx86))
      {
        _notePadPath = standardPathx86;
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
      fbd.SelectedPath = Properties.Settings.Default.TreeFolder;
      DialogResult result = fbd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(Directory.EnumerateFiles(fbd.SelectedPath).Any(file => file.EndsWith(".ptml")))
        {
          ImportTreeLabel.Content = fbd.SelectedPath;
          //var possibleModelFile = Path.Combine(Path.GetDirectoryName(fbd.SelectedPath), "trained.gz");
          //if(File.Exists(possibleModelFile))
          //{
          Properties.Settings.Default.TreeFolder = fbd.SelectedPath;
          Properties.Settings.Default.Save();
          //ModelPathLabel.Content = possibleModelFile;
          //}
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
      ofd.Title = "Select a file containing your process tree pattern (.ptml format).";
      ofd.FileName = Settings.Default.PatternFile;
      ofd.Filter = "Ptml files |*.ptml";
      if(!File.Exists(ofd.FileName))
      {
        ofd.FileName = Path.Combine(Program.GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\WorkflowPatternFinder\Example Patterns\accordeer1.ptml");
      }
      DialogResult result = ofd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(ofd.FileName.EndsWith(".ptml"))
        {
          ImportPatternLabel.Content = ofd.FileName;
          Settings.Default.PatternFile = ofd.FileName;
          Settings.Default.Save();
        }
        else
        {
          UpdateButtonText(ImportPatternButton, _incorrectFile, 3000);
        }
      }
    }

    private void StartTreeButton_Click(object sender, RoutedEventArgs e)
    {
      ClearValidOccurencesView();

      // update the python .exe path
      if(string.IsNullOrEmpty(SubTreeFinder._pythonExe))
      {
        if(Program.CheckIfPythonAndJavaAreInstalled())
        {
          SubTreeFinder.SetPythonExe(Program.GetPythonExe());
        }
      }
      SearchPatternOccurences();
    }

    private void SearchPatternOccurences()
    {
      // UI-linked code needs to be executed before new Thread is started.
      if(!PathsExists() || !double.TryParse(SimTresholdValue.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double threshold))
      {
        UpdateButtonText(TreeStartButton, "Incorrect inputs!");
        return;
      }
      ResultDebug.Content = "Searching for this pattern...";
      ChangeEnabledTreeButtons(false);
      ((GridView)ValidOccurencesView.View).Columns[1].Header = _countNumberOfPatternsWithinModel ? "# of Occurrences" : "Similarity Scores";
      TreeProgressBar.IsIndeterminate = true;
      _simThresholdCache = threshold;
      _inducedCache = _treatAsInducedSubTree;
      _similarityVariantCache = ((System.Windows.Controls.Label)SimilarityVariantComboBox.SelectedItem).Content.ToString();
      _modelMatches.Clear();
      OpenVariationViewerButton.Visibility = Visibility.Hidden;

      _treeBasePathCache = ImportTreeLabel.Content.ToString();
      _patternPathCache = ImportPatternLabel.Content.ToString();
      if(File.Exists(ModelPathLabel.Content.ToString()))
      {
        _modelPathCache = ModelPathLabel.Content.ToString();
      }
      else
      {
        _modelPathCache = Path.Combine(Program.GetWord2VecBasePath(), @"sonar-320.bin");
        ModelPathLabel.Content = _modelPathCache;
      }
      _scriptPathCache = Path.Combine(Program.GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\Gensim.py");
      _filterModelCache = StripPunctuation(FilterModelBox.Text).Trim();

      // Start new thread which calls the Python-gensim script.
      Task DoWork()
      {
        var tasks = new List<Task>
        {
          Task.Run((Action)CallGensimScript)
        };
        return Task.WhenAll(tasks);
      }
      StartGensimTask(DoWork);
    }

    private void StartGensimTask(Func<Task> task, Action completedTask = null)
    {
      TreeProgressBar.IsIndeterminate = true;
      ChangeEnabledTreeButtons(false);

      var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Task.Factory
        .StartNew(() =>
          task()
            .ContinueWith(async t =>
            {
              await FinishGensimScriptTask(t.Exception);
              completedTask?.Invoke();
            }, scheduler));
    }

    void CallGensimScript()
    {
      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = Program.GetPythonExe(),
        Arguments = $"\"{_scriptPathCache}\" \"{_modelPathCache}\" \"{_treeBasePathCache}\" \"{_patternPathCache}\" \"{_inducedCache}\" \"{_simThresholdCache.ToString(CultureInfo.InvariantCulture)}\" \"{_countNumberOfPatternsWithinModel}\" \"{_similarityVariantCache}\" \"{_filterModelCache}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        CreateNoWindow = true
      };

      var timer = new Stopwatch();
      timer.Start();

      //cmd is full path to python.exe
      //args is path to .py file and any cmd line args
      using(Process process = Process.Start(start))
      {
        using(StreamReader reader = process?.StandardOutput)
        {
          string result = reader?.ReadToEnd();
          timer.Stop();
          var lines = result.Replace("\r\n", "|").Split('|').ToList();
          foreach(var line in lines)
          {
            Debug.WriteLine(line);
          }
          var resultingOutput = lines.SkipWhile(c => !c.StartsWith("Results coming up!")).Skip(1).ToList();
          var overviewOneLiner = resultingOutput.First().Split('/');

          _matchRatio = new Tuple<string, string, string>(overviewOneLiner[0], overviewOneLiner[1], overviewOneLiner[2]);
          double.TryParse(overviewOneLiner[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _avgScore);
          try
          {
            var variantLines = resultingOutput.GetRange(1, resultingOutput.IndexOf("Valid trees:") - 1);
            foreach(var variantLine in variantLines)
            {
              var components = variantLine.Split(';');
              var treePath = components[0];
              var pTerm = components[1];
              var tTerm = components[2];
              var tSentence = components[3];
              var mScore = components[4];
              var variantId = components[5];

              if(!_modelMatches.ContainsKey(treePath))
              {
                _modelMatches.Add(treePath, new ModelMatches());
              }
              if(!_modelMatches[treePath].Variants.ContainsKey(variantId))
              {
                _modelMatches[treePath].Variants.Add(variantId, new MatchVariant());
              }
              if(!double.TryParse(mScore, out double x))
              {
                Debug.Write("Ehm....");
              }

              var newTermMatch = new TermMatch
              {
                PatternTerm = pTerm,
                TreeTerm = tTerm,
                Score = mScore,
                TreeSentence = tSentence
              };

              _modelMatches[treePath].Variants[variantId].Matches.Add(newTermMatch);
            }
          }
          catch(Exception e)
          {
            throw e;
          }

          foreach(var modelMatch in _modelMatches.Values)
          {
            foreach(var variant in modelMatch.Variants.Values)
            {
              variant.ComputeOverallScore();
            }
          }

          var validSubtrees = resultingOutput.SkipWhile(c => !c.StartsWith("Valid trees:")).Skip(1);
          foreach(string validTree in validSubtrees)
          {
            if(!string.IsNullOrEmpty(validTree))
            {
              var splittedResult = validTree.Split(';');
              var treePath = splittedResult[0];
              var score = splittedResult[1].Split('-').ToList();
              var patternMembers = splittedResult.Skip(2).ToList();

              if(File.Exists(treePath))
              {
                var nodeMatches = new List<KeyValuePair<string, string>>();
                foreach(var nodeMatch in patternMembers)
                {
                  var nodeMatchParts = RemoveSpecialCharacters(nodeMatch).Split(' ').ToList();
                  if(nodeMatchParts.Count > 5)
                  {
                    Debug.Write("Eh");
                  }
                  var treeNode = nodeMatchParts[0];
                  var patternNode = nodeMatchParts[1];
                  var wordMatchScore = nodeMatchParts[2];
                  var matchWord = nodeMatchParts[3];
                  var treeSentence = "";
                  nodeMatches.Add(new KeyValuePair<string, string>(treeNode, $"{patternNode}:{matchWord}"));
                }
                var newPattern = new PatternObject(treePath, score, nodeMatches);
                _foundPatterns.Add(newPattern);
                //validOutput.Add(treePath, double.Parse(score, CultureInfo.InvariantCulture));
                //_validOutputCache.Add($"{newPattern.DatabaseName}", newPattern.Scores);
                Debug.WriteLine($"{treePath} is a subtree!");
              }
              else
              {
                throw new Exception("Incorrect file path given!");
              }
            }
          }
          Debug.WriteLine($"The process took {timer.Elapsed.Seconds} seconds!");
        }
      }
    }

    public static string RemoveSpecialCharacters(string str)
    {
      return Regex.Replace(str, "[',()]+", "", RegexOptions.Compiled);
    }

    private void ListView_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var listName = ((System.Windows.Controls.ListView)sender).Name;
      ProcessTreeObject selectedTree = null;

      PatternObject selectedPattern = null;
      if(listName == "ProcessTreeView")
      {
        if(ProcessTreeView.SelectedItem != null)
        {
          selectedTree = ((ProcessTreeObject)ProcessTreeView.SelectedItem);
        }
      }
      else if(listName == "ValidOccurencesView")
      {
        if(ValidOccurencesView.SelectedItem != null)
        {
          var patternSummary = ((ValidOccurencesViewObject)ValidOccurencesView.SelectedItem).TreeSummary;
          selectedPattern = _foundPatterns.Single(p => p.TreeSummary == patternSummary);
        }
      }
      else
      {
        return;
      }

      if(selectedPattern != null || selectedTree != null)
      {
        var patternMembers = "";
        var selectedItem = selectedPattern ?? selectedTree;
        var patternSize = 1;
        if(listName == "ValidOccurencesView")
        {
          patternMembers = string.Join(",", selectedPattern.Ids.Select(t => $"{t.Key}:{t.Value}"));
          patternSize = selectedPattern.Ids.Count / selectedPattern.Scores.Count;
        }
        if(e.ChangedButton == MouseButton.Left)
        {
          RenderTreeInPython(selectedItem.TreePath, patternMembers, selectedItem.TreeSummary, patternSize);
        }
        else if(e.ChangedButton == MouseButton.Right)
        {
          OpenFileInNotePad(selectedItem.TreePath);
        }
      }
    }

    private void ImportTreeLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ImportTreeLabel.Content.ToString();
      OpenDirectoryInExplorer(selectedFile);
    }

    private void ImportPatternLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedFile = ImportPatternLabel.Content.ToString();
      if(File.Exists(selectedFile))
      {
        if(e.ChangedButton == MouseButton.Left)
        {
          RenderTreeInPython(selectedFile, "", Path.GetFileNameWithoutExtension(selectedFile));
        }
        else if(e.ChangedButton == MouseButton.Right)
        {
          OpenFileInNotePad(selectedFile);
        }
      }
    }

    private void PreProcessingButton_Click(object sender, RoutedEventArgs e)
    {
      ChangeEnabledPreProcessingButtons(false);
      _importExcelDir = ImportExcelDirectoryLabel.Content.ToString();
      _promBasePath = PromLabel.Content.ToString();
      _noiseThreshold = InductiveMinerNoiseThresholdTextBox.Text;

      Task DoWork()
      {
        var tasks = new List<Task>
        {
          Task.Run((Action)StartPreprocessing)
        };
        return Task.WhenAll(tasks);
      }
      StartPreprocessingTask(DoWork);
    }

    void StartPreprocessing()
    {
      Program.PreProcessingPhase(_importExcelDir, _promBasePath, _noiseThreshold);
    }

    private void StartPreprocessingTask(Func<Task> task, Action completedTask = null)
    {
      ProcessTreeView.Items.Clear();
      ConsoleLabel.Content = "Processing...";
      PreProgress.IsIndeterminate = true;

      var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Task.Factory
        .StartNew(() =>
          task()
            .ContinueWith(async t =>
            {
              FinishPreprocessingTask(t.Exception);
              completedTask?.Invoke();
            }, scheduler));
    }

    private async Task FinishUIupdateTask(Exception ex)
    {
      foreach(var obj in _processtreeFolderCache)
      {
        ProcessTreeView.Items.Add(obj);
      }
      ProcessTreeViewLabel.Content = $"Process trees created \t{_processtreeFolderCache.Count} models loaded";

      UpdateButtonText(PreProcessingButton, "Done!");
      ConsoleLabel.Content = "Start...";
      ChangeEnabledPreProcessingButtons(true);
      PreProgress.IsIndeterminate = false;
    }

    private async Task FinishGensimScriptTask(Exception ex)
    {
      foreach(var pattern in _foundPatterns.OrderByDescending(c => c.Scores.Average()).OrderByDescending(c => c.Scores.Count))
      {
        var path = pattern.TreePath;
        var roundedScores = pattern.Scores.Select(s => Math.Round(s, 3)).ToList();
        var scoreBinding = pattern.Scores.Count == 1 ? roundedScores.First().ToString(CultureInfo.InvariantCulture) : $"{pattern.Scores.Count} ({string.Join(",", roundedScores)})";

        ValidOccurencesView.Items.Add(new ValidOccurencesViewObject(path) { SimilarityScore = scoreBinding });
      }
      ResultDebug.Content = $"Found {_matchRatio.Item1} matches in {_matchRatio.Item2} out of {_matchRatio.Item3} models.\nThe average overall score is {Math.Round(_avgScore, 3).ToString(CultureInfo.InvariantCulture)}.";
      UpdateButtonText(TreeStartButton, "Done!");
      ChangeEnabledTreeButtons(true);
      TreeProgressBar.IsIndeterminate = false;
      OpenVariationViewerButton.Visibility = _modelMatches.Any() ? Visibility.Visible : Visibility.Hidden;
    }

    private void FinishPreprocessingTask(Exception ex)
    {
      TryToUpdateUI();
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
      if(Directory.Exists(Settings.Default.DataFolder))
      {
        fbd.SelectedPath = Settings.Default.DataFolder;
      }
      else
      {
        fbd.SelectedPath = Path.Combine(Program.GetDatasetBasePath());
      }
      DialogResult result = fbd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        UpdateExcelDirectoryUI(fbd.SelectedPath);
        var t = new Stopwatch();
        t.Start();
        TryToUpdateUI();
        t.Stop();
        Settings.Default.DataFolder = fbd.SelectedPath;
        Settings.Default.Save();
      }
    }

    public string _processTreeDirectoryCache { get; set; }
    public List<ProcessTreeObject> _processtreeFolderCache = new List<ProcessTreeObject>();
    private string _treeBasePathCache;
    private string _patternPathCache;
    private string _modelPathCache;
    private string _scriptPathCache;
    private bool _inducedCache;
    private string _similarityVariantCache;
    private double _simThresholdCache;
    private Dictionary<string, double> _validOutputCache;
    private string _filterModelCache;
    private Tuple<string, string, string> _matchRatio;
    private double _avgScore;

    private void TryToUpdateUI()
    {
      ProcessTreeView.Items.Clear();
      _processtreeFolderCache.Clear();
      _processTreeDirectoryCache = Path.Combine(ImportExcelDirectoryLabel.Content.ToString(), "ptml");
      Task DoWork()
      {
        var tasks = new List<Task>
        {
          Task.Run((Action)StartUpdatingUI)
        };
        return Task.WhenAll(tasks);
      }
      StartUpdateUITask(DoWork);
    }

    private void StartUpdateUITask(Func<Task> task, Action completedTask = null)
    {
      ConsoleLabel.Content = "Loading Trees...";
      PreProgress.IsIndeterminate = true;
      ChangeEnabledPreProcessingButtons(false);

      var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Task.Factory
        .StartNew(() =>
          task()
            .ContinueWith(async t =>
            {
              await FinishUIupdateTask(t.Exception);
              completedTask?.Invoke();
            }, scheduler));
    }

    void StartUpdatingUI()
    {
      TryToUpdateProcessTreeView();
    }

    private void TryToUpdateProcessTreeView()
    {
      if(Directory.Exists(_processTreeDirectoryCache))
      {
        var allFiles = Directory.GetFiles(_processTreeDirectoryCache);
        
        foreach(string path in allFiles)
        {
          _processtreeFolderCache.Add(new ProcessTreeObject(path));
        }
      }
    }

    private void UpdateExcelDirectoryUI(string path)
    {
      if(Directory.EnumerateFiles(path).Any(s => s.EndsWith(".xlsx")))
      {
        ImportExcelDirectoryLabel.Content = path;
        _importExcelDir = path;
        Program.InitializePaths(_importExcelDir);
      }
      else
      {
        UpdateButtonText(ImportExcelDirectoryButton, _incorrectDir, 3000);
      }
    }

    private void PromLabel_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      var selectedDirectory = PromLabel.Content.ToString();
      OpenDirectoryInExplorer(selectedDirectory);
    }

    private void PromButton_Click(object sender, RoutedEventArgs e)
    {
      var scriptFiles = PromCustomFileNames.GetAllNames();
      FolderBrowserDialog ofd = new FolderBrowserDialog();

      var standardPath = Program.GetProMBasePath();
      if(File.Exists(Settings.Default.PromFolder))
      {
        standardPath = Settings.Default.PromFolder;
      }

      if(Directory.Exists(standardPath))
      {
        ofd.SelectedPath = standardPath;
      }
      DialogResult result = ofd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(Directory.Exists(ofd.SelectedPath))
        {
          var files = Directory.EnumerateFiles(ofd.SelectedPath);
          foreach(var scriptFileName in scriptFiles)
          {
            if(!files.Any(f => f.EndsWith(scriptFileName)))
            {
              UpdateButtonText(PromButton, _missingScriptFile);
              break;
            }
          }
          _promBasePath = ofd.SelectedPath;
          PromLabel.Content = _promBasePath;
          Program.UpdateScriptFilePaths(_promBasePath);
          Settings.Default.PromFolder = ofd.SelectedPath;
          Settings.Default.Save();
        }
        else
        {
          UpdateButtonText(PromButton, _incorrectFile);
        }
      }
    }

    private void ChangeEnabledPreProcessingButtons(bool isEnabled)
    {
      // Change state of controls in Pre-Processing tab.
      PreProcessingButton.IsEnabled = isEnabled;
      ImportExcelDirectoryButton.IsEnabled = isEnabled;
      PromButton.IsEnabled = isEnabled;
      RemakeProcessTreesButton.IsEnabled = isEnabled;
      InductiveMinerNoiseThresholdTextBox.IsEnabled = isEnabled;
    }

    private void ChangeEnabledTreeButtons(bool isEnabled)
    {
      // Change ability to use controls controls on Tree Pattern tab.
      TreeStartButton.IsEnabled = isEnabled;
      ImportPatternButton.IsEnabled = isEnabled;
      ImportTreeButton.IsEnabled = isEnabled;
      PatternMatchingComboBox.IsEnabled = isEnabled;
      CountCheckBox.IsEnabled = isEnabled;
      SimilarityVariantComboBox.IsEnabled = isEnabled;
      SimTresholdValue.IsEnabled = isEnabled;
      FilterModelBox.IsEnabled = isEnabled;
    }

    private void ClearValidOccurencesView()
    {
      _foundPatterns.Clear();
      ValidOccurencesView.Items.Clear();
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
        button.Foreground = Brushes.White;
        timer.Stop();
      };
      timer.Start();
      button.Content = message;
      button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 50, 50));
    }

    private void OpenFileInNotePad(string filePath)
    {
      Process.Start(_notePadPath, filePath);
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
      var modelPath = ModelPathLabel.Content.ToString();
      if(!File.Exists(modelPath))
      {
        UpdateButtonText(TrainModelButton, "Incorrect inputs!");
        return;
      }

      var scriptPath = Path.Combine(Program.GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\PlotModel.py");

      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = Program.GetPythonExe(),
        Arguments = $"\"{scriptPath}\" \"{modelPath}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true
      };

      Process.Start(start);
    }

    private void CheckThresholdInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !e.Text.Any(x => Char.IsDigit(x) || '.'.Equals(x));
    }

    private void CheckIfInputIsDigit(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !e.Text.Any(x => Char.IsDigit(x));
    }

    private void ChangeModelButton_Click(object sender, RoutedEventArgs e)
    {
      OpenFileDialog ofd = new OpenFileDialog();
      ofd.DefaultExt = ".bin";
      ofd.Title = "Select a word2vec model.";
      ofd.Filter = "Word2Vec model | *.bin";
      if(File.Exists(Settings.Default.Word2VecFile))
      {
        ofd.FileName = Settings.Default.Word2VecFile;
      }
      else
      {
        ofd.FileName = Path.Combine(Program.GetWord2VecBasePath(), @"sonar-320.bin");
      }
      DialogResult result = ofd.ShowDialog();
      if(result == System.Windows.Forms.DialogResult.OK)
      {
        if(ofd.FileName.EndsWith(".bin"))
        {
          ModelPathLabel.Content = ofd.FileName;
        }
        else
        {
          UpdateButtonText(TrainModelButton, _incorrectFile, 3000);
        }
      }
    }

    public void RenderTreeFromChildWindow(string filePath)
    {
      var selectedPattern = _foundPatterns.Single(p => p.TreePath == filePath);
      
      var patternMembers = string.Join(",", selectedPattern.Ids.Select(t => $"{t.Key}:{t.Value}"));
      var patternSize = selectedPattern.Ids.Count / selectedPattern.Scores.Count;
      
      RenderTreeInPython(filePath, patternMembers, Path.GetFileNameWithoutExtension(filePath), patternSize);
    }

    private void RenderTreeInPython(string filePath, string patternMembers = "", string treeSummary = "", int patternSize = 1)
    {
      var scriptPath = Path.Combine(Program.GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\RenderTree.py");

      ProcessStartInfo start = new ProcessStartInfo
      {
        FileName = Program.GetPythonExe(),
        Arguments = $"\"{scriptPath}\" \"{filePath}\" \"{patternMembers}\" \"{treeSummary}\" \"{patternSize}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden
      };

      ThreadStart ths = () => Process.Start(start);
      Thread th = new Thread(ths);
      th.Start();

      Activate();
    }

    private void ValidOccurencesView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if(e.Key == Key.Enter)
      {
        var selectedItem = ValidOccurencesView.SelectedItem;
        var selectedFile = selectedItem?.ToString().Split('\t').First();
        if(File.Exists(selectedFile))
        {
          var selectedPattern = _foundPatterns.Single(p => p.TreePath == selectedFile);
          var patternMembers = string.Join(",", selectedPattern.Ids);
          RenderTreeInPython(selectedFile, patternMembers);
        }
      }
    }

    private void CountCheckBox_Click(object sender, RoutedEventArgs e)
    {
      if(CountCheckBox.IsChecked == true)
      {
        _countNumberOfPatternsWithinModel = true;
      }
      else
      {
        _countNumberOfPatternsWithinModel = false;
      }
    }

    private void RemakeProcessTreesButton_Click(object sender, RoutedEventArgs e)
    {
      PreProgress.IsIndeterminate = true;
      ChangeEnabledPreProcessingButtons(false);
      _importExcelDir = ImportExcelDirectoryLabel.Content.ToString();
      _promBasePath = PromLabel.Content.ToString();
      var noiseThreshold = InductiveMinerNoiseThresholdTextBox.Text;
      if(Directory.Exists(_importExcelDir) && Directory.Exists(_promBasePath))
      {
        Program.RemakeProcessTrees(_importExcelDir, _promBasePath, noiseThreshold);
      }
      ChangeEnabledPreProcessingButtons(true);
      PreProgress.IsIndeterminate = false;
    }

    private void TermQueryButton_Click(object sender, RoutedEventArgs e)
    {
      SimilarTermsList.Items.Clear();
      if(Program.CheckIfPythonAndJavaAreInstalled())
      {
        var modelpath = ModelPathLabel.Content.ToString();
        if(File.Exists(modelpath))
        {
          var currentTerm = TermQueryTextBox.Text;
          var output = Program.GetSimilarTerms(modelpath, currentTerm).SkipWhile(l => !l.Contains("Similar terms:")).ToList();
          foreach(var line in output.Skip(1))
          {
            if(!string.IsNullOrEmpty(line))
            {
              Debug.WriteLine(line);
              var lineSplit = line.Split(':');
              var term = lineSplit[0];
              var score = lineSplit[1];
              if(score.Length > 7)
              {
                score = score.Substring(0, 6);
              }

              SimilarTermsList.Items.Add(new MatchingTerm() { Term = term, Score = score });
            }
          }
        }
        else
        {
          UpdateButtonText(TermQueryButton, _incorrectFile, 3000);
        }
      }
    }

    private void SentenceQueryButton_Click(object sender, RoutedEventArgs e)
    {
      SimilarTermsList.Items.Clear();
      if(Program.CheckIfPythonAndJavaAreInstalled())
      {
        var modelpath = ModelPathLabel.Content.ToString();
        if(File.Exists(modelpath))
        {
          var currentTerm = TermQueryTextBox.Text.ToLower();
          var scriptPath = Path.Combine(Program.GetToolBasePath(), @"WorkflowPatternFinder\WorkflowPatternFinder\Gensim\SentenceQuery.py");
          var output = Program.GetSentences(scriptPath, modelpath, currentTerm).SkipWhile(l => !l.Contains("Sentences:")).ToList();
          foreach(var line in output.Skip(1))
          {
            if(!string.IsNullOrEmpty(line))
            {
              Debug.WriteLine(line);
              var lineSplit = line.Split();
              var term = string.Join(" ", lineSplit.TakeWhile(x => !x.isnumeric()).ToList());
              var score = lineSplit.LastOrDefault();

              SimilarTermsList.Items.Add(new MatchingTerm() { Term = term, Score = score });
            }
          }
        }
        else
        {
          UpdateButtonText(SentenceQueryButton, _incorrectFile, 3000);
        }
      }
    }

    private void KeysDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if(Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.F))
      {
        if(!((TabItem)TabControl.SelectedValue).Header.ToString().Contains("Word2Vec"))
        {
          return;
        }

        tFinder = new TermFinder {Owner = this};
        tFinder.Show();
        tFinder.Activate();
      }
    }

    public static string StripPunctuation(string s)
    {
      var sb = new StringBuilder();
      foreach(char c in s)
      {
        if(!char.IsPunctuation(c))
          sb.Append(c);
      }
      return sb.ToString();
    }

    private void TermQueryTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if(sender is TextBox)
      {
        if(((TextBox)sender).Text.Replace(" ", "").Length == 0)
          //If nothing has been entered yet.
          ((TextBox)sender).Text = _helpText;
      }
    }

    private void TermQueryTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      if(sender is TextBox)
      {
        if(((TextBox)sender).Text == _helpText)
          //If nothing has been entered yet.
          ((TextBox)sender).Text = "";
      }
    }

    private void OpenVariationViewerButton_Click(object sender, RoutedEventArgs e)
    {
      var variationViewer = new VariationViewer(_modelMatches) {Owner = this};
      variationViewer.Show();
      variationViewer.Focus();
    }

    private void PatternMatchingComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedValue = ((ComboBox) sender).SelectedIndex;
      _treatAsInducedSubTree = selectedValue == 0;
    }
  }
}