import sys
from Node import *

class ProcessTree(object):
    """A Process Tree object."""

    def __init__(self, filePath, id, rootId):
        self._filePath = filePath
        self._id = id
        self._rootId = rootId
        self._root = None
        self._nodes = []

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
  
    def GetNodes(self):
        return self._nodes

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
        return len(self._nodes)