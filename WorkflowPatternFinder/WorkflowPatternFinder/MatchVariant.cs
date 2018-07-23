using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace WorkflowPatternFinder
{
  public class MatchVariant
  {
    public List<TermMatch> Matches = new List<TermMatch>();
    public double Score { get; set; }

    public void ComputeOverallScore()
    {
      try
      {
        Score = Matches.Average(item => double.Parse(item.Score, CultureInfo.InvariantCulture));
      }
      catch(Exception e)
      {
        throw e;
      }
    }
  }
}
