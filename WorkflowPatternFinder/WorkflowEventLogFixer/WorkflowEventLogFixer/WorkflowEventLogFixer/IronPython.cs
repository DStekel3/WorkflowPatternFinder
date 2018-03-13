using IronPython.Hosting;
using Microsoft.Scripting;

namespace WorkflowEventLogFixer
{
  public class IronPython
  {
    public IronPython(string script)
    {
      var engine = Python.CreateEngine();
      var scope = engine.CreateScope();
      var source = engine.CreateScriptSourceFromString(script, SourceCodeKind.Statements);
      var compiled = source.Compile();
      var result = compiled.Execute(scope);
    }
  }
}
