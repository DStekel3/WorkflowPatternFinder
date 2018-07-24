using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using WorkflowEventLogProcessor;

namespace WorkflowPatternFinder
{
  /// <summary>
  /// Interaction logic for TermFinder.xaml
  /// </summary>
  public partial class TermFinder
  {
    private string _findTerm = string.Empty;
    private List<MatchingTerm> _matches = new List<MatchingTerm>();
    private int _selectedIndex;

    public TermFinder()
    {
      InitializeComponent();
      FindTermTextBox.Focus();
    }

    private void FindTermButton_Click(object sender, RoutedEventArgs e)
    {
      FindTermInListView();
    }

    private void KeyPressed(object sender, KeyEventArgs e)
    {
      if(e.Key == Key.Escape)
      {
        Close();
        Owner.Focus();
      }
      else if(e.Key == Key.Enter && Keyboard.IsKeyDown(Key.Enter))
      {
        Debug.WriteLine(e.Key.ToString(), Keyboard.IsKeyDown(e.Key));
        FindTermInListView();
      }
    }

    private void FindTermInListView()
    {
      var term = FindTermTextBox.Text.ToLower();
      if(!string.IsNullOrEmpty(term))
      {
        // find all matches and save them in a list.
        if(term != _findTerm)
        {
          _findTerm = term;
          _matches.Clear();
          _selectedIndex = 0;

          var items = ((MainWindow)Owner).SimilarTermsList.Items;
          for(int t = 0; t < items.Count; t++)
          {
            var currentItem = (MatchingTerm)items.GetItemAt(t);
            if(currentItem.Term.Contains(term))
            {
              _matches.Add(currentItem);
            }
          }
        }

        if(_matches.Count > 0)
        {
          // re-use this list as long as the term (input by user) is not changed. Iterate over all matches when the user keeps pressing on the find button.
          var selectedItem = _matches[_selectedIndex];
          ((MainWindow)Owner).SimilarTermsList.SelectedItem = selectedItem;
          ((MainWindow)Owner).SimilarTermsList.ScrollIntoView(selectedItem);
          _selectedIndex++;
          if(_selectedIndex == _matches.Count)
          {
            _selectedIndex = 0;
          }
        }
      }
    }
  }
}
