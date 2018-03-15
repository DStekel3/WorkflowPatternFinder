import sys
from Node import *

class ProcessTree(object):
    """A Process Tree object."""
    _filePath = None
    _id = None
    _rootId = None
    _root = None
    _nodes = []

    def __init__(self, filePath, id, rootId):
        self._filePath = filePath
        self._id = id
        self._rootId = rootId

    def AddNode(self, node):
        self._nodes.append(node)
        if node.GetId() == self._rootId:
            node.SetRoot(True)
            self._root = node
    
    def GetNode(self, id):
        for node in self._nodes:
            if node.GetId() == id:
                return node
        raise ValueError('This node cannot be found!')

    def GetId(self):
        return self._id
      
    def GetRoot(self):
        return self._root
    
    def SetParentalRelation(self, parentId, childId):
        parent = self.GetNode(parentId)
        child = self.GetNode(childId)
        parent.AddChild(child)
        child.SetParent(parent)

    def GetTreeSize(self):
        total = 0
        nodelist = []
        root = self.GetRoot()
        nodelist.append(root)
        while any(nodelist):
            node = nodelist.pop()
            total = total +1
            for child in node.GetChildren():
                nodelist.append(child)
        return total