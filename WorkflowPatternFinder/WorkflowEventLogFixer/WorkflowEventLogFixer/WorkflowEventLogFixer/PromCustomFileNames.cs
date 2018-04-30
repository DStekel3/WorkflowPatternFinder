using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogFixer
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
    public static string GetPetriNetMiner()
    {
      return "PetriNetMiner.txt";
    }
    public static string GetPTQualityMiner()
    {
      return "PTQualityMiner.txt";
    }

    public static string GetCLI()
    {
      return "ProM_CLI.bat";
    }

    public static List<string> GetAllNames()
    {
      return new List<string>
      {
        GetEvoluationaryTreeMiner(),
        GetPetriNetMiner(),
        GetProcessTreeMiner(),
        GetPTQualityMiner(),
        GetCLI()
      };
    }
  }
}
