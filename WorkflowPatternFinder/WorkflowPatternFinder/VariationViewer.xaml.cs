using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WorkflowPatternFinder
{
  /// <summary>
  /// Interaction logic for VariationViewer.xaml
  /// </summary>
  public partial class VariationViewer
  {
    private bool _distinctVariantsOnly;
    private readonly Dictionary<string, ModelMatches> _modelVariants;
    public VariationViewer(Dictionary<string, ModelMatches> modelVariants)
    {
      _modelVariants = modelVariants;
      InitializeComponent();
      _distinctVariantsOnly = false || ViewDistinctVariantsCheckBox.IsChecked != null && (bool)ViewDistinctVariantsCheckBox.IsChecked;
      LoadVariations(modelVariants, _distinctVariantsOnly);
      RefreshTitle();
    }

    private void RefreshTitle()
    {
      if((bool)ViewDistinctVariantsCheckBox.IsChecked)
      {
        Title.Content = "All Distinct Variations";
      }
      else
      {
        Title.Content = "All Variations";
      }
    }

    private void LoadVariations(Dictionary<string, ModelMatches> modelVariants, bool distinctVariantsOnly)
    {
      //variantGrid.ItemsSource = null;
      variantGrid.DataContext = null;
      //variantGrid.Columns.Clear();
      //variantGrid.Items.Clear();
      if(!modelVariants.Any())
      {
        return;
      }

      var data = new DataTable();
      data.Columns.Add("Id", typeof(int));

      var firstMatch = modelVariants.Values.First().Variants.First();
      foreach(var termMatch in firstMatch.Value.Matches)
      {
        data.Columns.Add($"Matches with \"{termMatch.PatternTerm}\"");
      }
      data.Columns.Add("Overall Score", typeof(double));
      if(!distinctVariantsOnly)
      {
        data.Columns.Add("Workflow File", typeof(string));
        data = GetAllVariants(data, modelVariants);
      }
      else
      {
        data = GetDistinctVariants(data, modelVariants);
      }

      variantGrid.DataContext = data.DefaultView;
    }

    private DataTable GetDistinctVariants(DataTable data, Dictionary<string, ModelMatches> modelVariants)
    {
      int id = 1;
      var uniqueVariants = new List<List<object>>();
      foreach(var modelMatch in modelVariants)
      {
        foreach(var matchVariant in modelMatch.Value.Variants)
        {
          var score = matchVariant.Value.Score;
          var row = new List<object> { id };
          foreach(var termMatch in matchVariant.Value.Matches)
          {
            row.Add($"{termMatch.TreeTerm} -> ({termMatch.TreeSentence.Replace('_', ' ')})");
          }
          row.Add(score);

          bool isNewVariant = uniqueVariants.All(v => !CompareRows(v, row));

          if(isNewVariant)
          {
            data.Rows.Add(row.ToArray<object>());
            uniqueVariants.Add(row);
            id++;
          }
          else
          {
            Debug.Write("This variant is already used.");
          }
        }
      }
      return data;
    }

    /// <summary>
    /// Compares two lists of strings using LINQ's SequenceEqual. Note that each list respresents a row and we do not want to 
    /// include the row id's during this comparison.
    /// </summary>
    public bool CompareRows(List<object> row1, List<object> row2)
    {
      var r1Converted = row1.GetRange(1, row1.Count - 1).OfType<string>();
      var r2Converted = row2.GetRange(1, row2.Count - 1).OfType<string>();
      return r1Converted.SequenceEqual(r2Converted, StringComparer.OrdinalIgnoreCase);
    }

    private DataTable GetAllVariants(DataTable data, Dictionary<string, ModelMatches> modelVariants)
    {
      int id = 1;
      foreach(var modelMatch in modelVariants)
      {
        foreach(var matchVariant in modelMatch.Value.Variants)
        {
          var treePath = modelMatch.Key;
          var score = matchVariant.Value.Score;
          var row = new List<object> { id };
          foreach(var termMatch in matchVariant.Value.Matches)
          {
            row.Add($"{termMatch.TreeTerm} -> ({termMatch.TreeSentence.Replace('_', ' ')})");
          }
          row.Add(score);
          row.Add(treePath);
          data.Rows.Add(row.ToArray<object>());
          id++;
        }
      }
      return data;
    }

    private void ViewDistinctVariantsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
      var isChecked = ((CheckBox)sender).IsChecked;
      if(isChecked != null) _distinctVariantsOnly = (bool)isChecked;
      LoadVariations(_modelVariants, _distinctVariantsOnly);

      if(!variantGrid.Items.IsEmpty)
      {
        variantGrid.Focus();
        variantGrid.ScrollIntoView(variantGrid.Items[0]);
      }

      RefreshTitle();
    }

    private void KeyIsPressed(object sender, KeyEventArgs e)
    {
      if(e.Key == Key.Escape)
      {
        Close();
        Owner.Focus();
      }
    }
  }
}
