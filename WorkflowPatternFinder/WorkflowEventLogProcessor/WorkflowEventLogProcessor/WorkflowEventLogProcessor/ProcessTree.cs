using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowEventLogProcessor
{
  public class ProcessTree
  {
    private readonly string _filePath;
    private readonly Guid _id;
    private readonly Guid _rootId;
    private Node _root;
    private readonly List<Node> _nodes = new List<Node>();

    public ProcessTree(string filePath, string id, string rootId)
    {
      Guid.TryParseExact(id, "D", out _id);
      Guid.TryParseExact(rootId, "D", out _rootId);
      _filePath = filePath;
    }

    public void AddNode(Node node)
    {
      _nodes.Add(node);
      if(node.GetId() == _rootId)
      {
        node.SetRoot(true);
        _root = node;
      }
    }

    private Node GetNode(Guid id)
    {
      try
      {
        return _nodes.Single(n => n.GetId() == id);
      }
      catch(InvalidOperationException e)
      {
        throw new InvalidOperationException($"The Guid {id} you look for is not unique!", e);
      }
    }

    public Guid GetId()
    {
      return _id;
    }

    public Node GetRoot()
    {
      return _root;
    }

    public void SetParentalRelation(Guid parentId, Guid childId)
    {
      Node parent = GetNode(parentId);
      Node child = GetNode(childId);
      parent.AddChild(child);
      child.SetParent(parent);
    }

    public string GetFilePath()
    {
      return _filePath;
    }
  }
}