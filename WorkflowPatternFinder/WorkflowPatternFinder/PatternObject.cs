using System.Collections.Generic;
using System.Globalization;

namespace WorkflowPatternFinder
{
  public class PatternObject : ProcessTreeObject
  {
    public double Score;
    public List<KeyValuePair<string, string>> Ids = new List<KeyValuePair<string, string>>();

    public PatternObject(string filePath, string score, List<KeyValuePair<string, string>> ids) : base (filePath)
    {
      Score = double.Parse(score, CultureInfo.InvariantCulture);
      Ids = ids;
    }
  }
}