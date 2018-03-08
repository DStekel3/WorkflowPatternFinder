using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowEventLogFixer
{
    public class Node
    {
        private ProcessTreeLoader.NodeType _type;
        private Guid _id = Guid.Empty;
        private Node _parent;
        private List<Node> _children = new List<Node>();
        private string _name;
        private bool _isRoot;

        public Node(ProcessTreeLoader.NodeType type, string id, string name)
        {
            _type = type;
            _name = name;
            Guid.TryParseExact(id, "D", out _id);
        }

        public Node(ProcessTreeLoader.NodeType type, string id)
        {
            _type = type;
            Guid.TryParseExact(id, "D", out _id);
            _name = _type.ToString();
        }

        public Guid GetId()
        {
            return _id;
        }

        public new ProcessTreeLoader.NodeType GetType()
        {
            return _type;
        }

        public void SetParent(Node parent)
        {
            _parent = parent;
        }

        public void AddChild(Node child)
        {
            _children.Add(child);
        }

        public List<Node> GetChildren()
        {
            return _children;
        }

        public string GetEvent()
        {
            return _name;
        }

        public bool IsRoot()
        {
            return _isRoot;
        }

        public void SetRoot(bool isRoot)
        {
            _isRoot = isRoot;
        }

        public List<string> GetSiblings()
        {
            if(GetParent() != null)
            {
                return GetParent().GetChildren().Where(c => c.GetEvent() != GetEvent()).Select(c => c.GetEvent()).ToList();
            }
            return new List<string>();
        }

        private Node GetParent()
        {
            return _parent;
        }
    }
}