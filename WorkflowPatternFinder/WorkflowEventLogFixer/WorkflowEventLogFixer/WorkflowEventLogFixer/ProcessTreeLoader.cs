using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WorkflowEventLogFixer
{
  public static class ProcessTreeLoader
  {
    public enum NodeType
    {
      xor,
      xorLoop,
      and,
      andLoop,
      sequence,
      sequenceLoop,
      manualTask,
      tau
    };

    public static ProcessTree LoadTree(string filePath)
    {
      var sourceFile = filePath;
      ProcessTree tree = null;
      XmlReaderSettings settings = new XmlReaderSettings
      {
        DtdProcessing = DtdProcessing.Parse
      };
      XmlReader reader = XmlReader.Create(sourceFile, settings);
      reader.MoveToContent();
      while(reader.Read())
      {
        if(reader.IsStartElement())
        {
          switch(reader.Name)
          {
            case "processTree":
              {
                tree = new ProcessTree(filePath, reader["id"], reader["root"]);
                break;
              }
            case "xor":
              {
                tree.AddNode(new Node(NodeType.xor, reader["id"]));
                break;
              }
            case "xorLoop":
              {
                tree.AddNode(new Node(NodeType.xorLoop, reader["id"]));
                break;
              }
            case "and":
              {
                tree.AddNode(new Node(NodeType.and, reader["id"]));
                break;
              }
            case "andLoop":
              {
                tree.AddNode(new Node(NodeType.andLoop, reader["id"]));
                break;
              }
            case "sequence":
              {
                tree.AddNode(new Node(NodeType.sequence, reader["id"]));
                break;
              }
            case "sequenceLoop":
              {
                tree.AddNode(new Node(NodeType.sequenceLoop, reader["id"]));
                break;
              }
            case "manualTask":
              {
                tree.AddNode(new Node(NodeType.manualTask, reader["id"], reader["name"]));
                break;
              }
            case "automaticTask":
              {
                tree.AddNode(new Node(NodeType.tau, reader["id"]));
                break;
              }
            case "parentsNode":
              {
                Guid parentId = new Guid(reader["sourceId"] ?? throw new InvalidOperationException());
                Guid childId = new Guid(reader["targetId"] ?? throw new InvalidOperationException());
                tree.SetParentalRelation(parentId, childId);
                break;
              }
          }
        }
      }
      reader.Close();
      return tree;
    }
  }
}