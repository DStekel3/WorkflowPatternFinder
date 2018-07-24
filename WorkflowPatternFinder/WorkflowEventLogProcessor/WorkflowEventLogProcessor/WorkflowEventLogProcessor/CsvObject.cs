using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogProcessor
{
  public class CsvObject
  {
    public string Workflow { get; set; }
    public string WorkflowOmschrijving { get; set; }
    public string DossierItem { get; set; }
    public string TypeDossierItem { get; set; }
    public string Taak { get; set; }
    public string TaakOmschrijving { get; set; }
    public string Volgnummer { get; set; }
    public string VolgnummerVia { get; set; }
    public string ActieType { get; set; }
    public string ActieOmschrijving { get; set; }
    public string ActietypeOmschrijving { get; set; }
    public string Begin { get; set; }
    public string Eind { get; set; }
    public string StandaardBijschrift { get; set; }
    public string Status { get; set; }
    public string DoorPersoon { get; set; }
    public string WorkflowGegevensStatus { get; set; }
  }
}
