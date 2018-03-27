using System.Collections.Generic;

namespace WorkflowPatternFinder
{
  public class PatternObject
  {
    public string FilePath = "";
    public double Score = 0.0;
    public List<string> Ids = new List<string>();

    public PatternObject(string filePath, string score, List<string> ids)
    {
      FilePath = filePath;
      Score = double.Parse(score);
      Ids = ids;
    }
  }
}