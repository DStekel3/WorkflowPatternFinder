using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Scripting;

namespace WorkflowPatternFinder
{
  public class PatternObject : ProcessTreeObject
  {
    public List<double> Scores = new List<double>();
    public List<KeyValuePair<string, string>> Ids = new List<KeyValuePair<string, string>>();

    public PatternObject(string filePath, List<string> scores, List<KeyValuePair<string, string>> ids) : base (filePath)
    {
      foreach (string s in scores)
      {
        Scores.Add(Double.Parse(s, CultureInfo.InvariantCulture));
      }
      Ids = ids;
    }
  }
}