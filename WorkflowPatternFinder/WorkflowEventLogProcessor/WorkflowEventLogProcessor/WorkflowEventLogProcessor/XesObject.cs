using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowEventLogProcessor
{
  public class XesObject
  {
    public string Event { get; set; }

    public string Trace { get; set; }

    public string Timestamp { get; set; }

    public string LifeCycle { get; set; }
    public XesObject(Event csv)
    {
      Trace = csv.InstanceId;
      Event = $"{csv.TaakOmschrijving}|{csv.ActieOmschrijving}";
      Timestamp = csv.Eind;
      LifeCycle = "complete";
    }
  }
}
