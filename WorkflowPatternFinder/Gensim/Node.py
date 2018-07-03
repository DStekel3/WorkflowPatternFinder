import sys
from copy import copy

class Node(object):
    """A Node object."""

    def __init__(self, type, id, name=None):
        self._type = type
        self._name = name
        self._id = id
        self._parent = None
        self._children = [] 
        self._isVisited = False
        self._number = -1

        if name != None:
          self._name = self.EscapeCharacters(name)
        else:
          typeOfNode = str(type).split('.')[1]
          if "Xor" in typeOfNode:
            self._name = "X"
          elif "And" in typeOfNode:
            self._name = "+"
          elif "Sequence" in typeOfNode:
            self._name = "&rarr;"
          elif typeOfNode == "Tau":
            self._name = "&tau;"
          if "Loop" in typeOfNode:
            self._name = self._name + " Loop"
        self._isRoot = False

    def EscapeCharacters(self, name):
      result = name.replace('|', '\n')
      result = result.replace('&', '&amp;')
      result = result.replace('<', '&lt;')
      result = result.replace('>', '&gt;')
      result = result.replace('"', '&quot;')
      result = result.replace('`', '&apos;')
      result = result.replace('ë', '&euml;')
      return result

    def GetId(self):
        return self._id

    def GetType(self):
        return self._type.name

    def GetNumber(self):
        return self._number

    def GetParent(self):
        return self._parent

    def GetSiblings(self):
        if self.GetParent() != None:
            c = self.GetParent().GetChildren()
            return list([c[i] for i in range(len(c)) if c[i].GetEvent() != self.GetEvent()])
        return list()

    
    def GetChildren(self):
        return self._children

    def GetEvent(self):
        return self._name

    def GetDescendants(self):
        descendants = []
        nodelist = copy(self.GetChildren())
        while len(nodelist) > 0:
          current = nodelist.pop(0)
          descendants.append(current)
          currentChildren = copy(current.GetChildren())
          if len(currentChildren) > 0:
            descendants.extend(currentChildren)
        return descendants

    def IsRoot(self):
        return self._isRoot
  
    def Visit(self, bool):
        self._isVisited = bool

    def IsVisited(self):
        return self._isVisited

    def AddChild(self, child):
        self._children.append(child)

    def RemoveChild(self, child):
        self._children.remove(child)
    

    def SetRoot(self, isRoot):
        self._isRoot = isRoot

    def SetParent(self, parent):
        self._parent = parent
  
    def SetNumber(self, number):
        self._number = number

    def GetSubtreeSize(self):
        return len(self.GetAllDescendants()) + 1

    def GetAllDescendants(self):
        descendants = []
        for child in self.GetChildren():
          descendants.append(child)
          subnodes = child.GetAllDescendants()
          descendants.extend(subnodes)
        return descendants