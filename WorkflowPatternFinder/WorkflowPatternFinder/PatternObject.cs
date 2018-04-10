using System.Collections.Generic;

namespace WorkflowPatternFinder
{
  public class PatternObject
  {
    public string FilePath = "";
    public double Score = 0.0;
    public List<KeyValuePair<string, string>> Ids = new List<KeyValuePair<string, string>>();

    public PatternObject(string filePath, string score, List<KeyValuePair<string, string>> ids)
    {
      FilePath = filePath;
      Score = double.Parse(score);
      Ids = ids;
    }
  }
}