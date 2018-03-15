import sys

class Node(object):
    """A Node object."""
    _type = None
    _id = ""
    _parent = None
    _children = []
    _name = ""
    _isRoot = False

    def __init__(self, type, id, name = None):
        self._type = type
        self._name = name
        self._id = id

    def GetId(self):
        return self._id

    def GetType(self):
        return self._type.name

    def SetParent(self, parent):
        self._parent = parent

    def AddChild(self, child):
        self._children.append(child)

    def GetChildren(self):
        return self._children

    def GetEvent(self):
        return self._name

    def IsRoot(self):
        return self._isRoot

    def SetRoot(self, isRoot):
        self._isRoot = isRoot

    def GetParent(self):
        return self._parent

    def GetSiblings(self):
        if self.GetParent() != None:
            c = self.GetParent().GetChildren()
            return list([c[i] for i in range(len(c)) if c[i].GetEvent() != self.GetEvent()])
        return list()