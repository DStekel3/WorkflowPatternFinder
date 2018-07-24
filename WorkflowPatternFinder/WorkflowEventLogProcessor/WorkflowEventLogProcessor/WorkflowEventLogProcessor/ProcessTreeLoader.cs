using System;
using System.Xml;

namespace WorkflowEventLogProcessor
{
  public static class ProcessTreeLoader
  {
    public enum NodeType
    {
      Xor,
      XorLoop,
      And,
      AndLoop,
      Sequence,
      SequenceLoop,
      ManualTask,
      Tau
    };

    public static ProcessTree LoadTree(string filePath)
    {
      var sourceFile = filePath;
      ProcessTree tree = null;
      var settings = new XmlReaderSettings
      {
        DtdProcessing = DtdProcessing.Parse
      };
      var reader = XmlReader.Create(sourceFile, settings);
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
                tree?.AddNode(new Node(NodeType.Xor, reader["id"]));
                break;
              }
            case "xorLoop":
              {
                tree?.AddNode(new Node(NodeType.XorLoop, reader["id"]));
                break;
              }
            case "and":
              {
                tree?.AddNode(new Node(NodeType.And, reader["id"]));
                break;
              }
            case "andLoop":
              {
                tree?.AddNode(new Node(NodeType.AndLoop, reader["id"]));
                break;
              }
            case "sequence":
              {
                tree?.AddNode(new Node(NodeType.Sequence, reader["id"]));
                break;
              }
            case "sequenceLoop":
              {
                tree?.AddNode(new Node(NodeType.SequenceLoop, reader["id"]));
                break;
              }
            case "manualTask":
              {
                tree?.AddNode(new Node(NodeType.ManualTask, reader["id"], reader["name"]));
                break;
              }
            case "automaticTask":
              {
                tree?.AddNode(new Node(NodeType.Tau, reader["id"]));
                break;
              }
            case "parentsNode":
              {
                var parentId = new Guid(reader["sourceId"] ?? throw new InvalidOperationException());
                var childId = new Guid(reader["targetId"] ?? throw new InvalidOperationException());
                tree?.SetParentalRelation(parentId, childId);
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