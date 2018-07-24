using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogProcessor
{
  public static class PromCustomFileNames
  {
    public static string GetProcessTreeMiner()
    {
      return "ProcessTreeMiner.txt";
    }
    public static string GetEvoluationaryTreeMiner()
    {
      return "EvolutionaryTreeMiner.txt";
    }

    public static string GetCli()
    {
      return "ProM_CLI.bat";
    }

    public static List<string> GetAllNames()
    {
      return new List<string>
      {
        GetEvoluationaryTreeMiner(),
        GetProcessTreeMiner(),
        GetCli()
      };
    }
  }
}
