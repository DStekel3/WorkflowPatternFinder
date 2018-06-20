using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowPatternFinder
{
  public class ValidOccurencesViewObject : ProcessTreeObject
  {
    public ValidOccurencesViewObject(string filePath) : base(filePath)
    {
      
    }

    public double SimilarityScore { get; set; }
  }
}
