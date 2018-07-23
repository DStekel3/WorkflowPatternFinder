using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowPatternFinder
{
  public class TermMatch
  {
    public string PatternTerm { get; set; }
    public string TreeTerm { get; set; }
    public string TreeSentence { get; set; }
    public string Score { get; set; }
  }
}
