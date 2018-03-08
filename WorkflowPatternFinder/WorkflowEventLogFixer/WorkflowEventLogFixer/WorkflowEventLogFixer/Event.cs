using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogFixer
{
  public class Event
  {
    public string EventID { get; set; }
    public string Doorlooptijd { get; set; }
    public string WorkflowID { get; set; }
    public string WorkflowOmschrijving { get; set; }
    public string InstanceID { get; set; }
    public string TypeDossierItem { get; set; }
    public string TaakID { get; set; }
    public string TaakOmschrijving { get; set; }
    public string ActieID { get; set; }
    public string ActieType { get; set; }
    public string ActieOmschrijving { get; set; }
    public string ActieBijschrift { get; set; }
    public string Begin { get; set; }
    public string Eind { get; set; }
  }
}
