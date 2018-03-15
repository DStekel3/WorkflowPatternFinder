using System.Collections.Generic;
using IronPython.Hosting;
using Microsoft.Scripting;

namespace WorkflowEventLogFixer
{
  public static class IronPython
  {
    public static void RunScript(string script, List<ProcessTree> trees)
    {
      var engine = Python.CreateEngine();
      var scope = engine.CreateScope();
      List<string> argv = new List<string>
      {
        @"C:\Thesis\Profit analyses\testmap\ptml",
        "goedkeuren",
        "afkeuren"
      };
      engine.GetSysModule().SetVariable("argv", argv);
      dynamic py = engine.ExecuteFile(script);
    }
  }
}
