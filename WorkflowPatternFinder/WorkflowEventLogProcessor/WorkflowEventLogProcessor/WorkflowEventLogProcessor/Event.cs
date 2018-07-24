using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogProcessor
{
  public class Event
  {
    public string EventId { get; set; }
    public string Doorlooptijd { get; set; }
    public string WorkflowId { get; set; }
    public string WorkflowOmschrijving { get; set; }
    public string InstanceId { get; set; }
    public string TypeDossierItem { get; set; }
    public string TaakId { get; set; }
    public string TaakOmschrijving { get; set; }
    public string ActieId { get; set; }
    public string ActieType { get; set; }
    public string ActieOmschrijving { get; set; }
    public string ActieBijschrift { get; set; }
    public string Begin { get; set; }
    public string Eind { get; set; }
  }
}
