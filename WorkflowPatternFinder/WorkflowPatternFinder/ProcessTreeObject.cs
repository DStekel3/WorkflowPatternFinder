using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowPatternFinder
{
  public class ProcessTreeObject
  {
    public string TreePath { get; set; }
    public string WorkflowName { get; set; }
    public string DatabaseName { get; set; }
    public string Quality { get; set; }
    public string TreeSummary { get; set; }

    public ProcessTreeObject(string filePath)
    {
      if (File.Exists(filePath))
      {
        TreePath = filePath;
        DatabaseName = Path.GetFileNameWithoutExtension(filePath)?.Split('-')[0];
        WorkflowName = GetWorkflowName(filePath);
        TreeSummary = $"{DatabaseName}-{WorkflowName}";
      }
      else
      {
        throw new Exception("incorrect file path given.");
      }
    }

    public string GetWorkflowName(string filePath)
    {
      try
      {
        var workflowNameFile = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(filePath)), "workflownames.csv");
        var names = File.ReadAllLines(workflowNameFile);
        var fileKey = Path.GetFileNameWithoutExtension(filePath);
        var workflowName = names.First(r => r.Split(';')[0] == fileKey).Split(';')[1];
        return workflowName;
      }
      catch
      {
        return Guid.NewGuid().ToString();
      }
    }
  }
}
