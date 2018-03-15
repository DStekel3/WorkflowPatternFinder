import enum
from enum import Enum # learn more: https://python.org/pypi/enum
import sys
from ProcessTree import *
from Node import *
import xml.etree.ElementTree as ET

class NodeType(Enum):
    Xor = 1
    XorLoop = 2
    And = 3
    AndLoop = 4
    Sequence = 5
    SequenceLoop = 6
    ManualTask = 7
    Tau = 8

class ProcessTreeLoader(object):
    """This class loads in a process tree from a .ptml file."""
    @classmethod
    def LoadTree(self, filePath):
        ProcessTreeLoader.sourceFile = filePath
        tree = None
        doc = ET.parse(filePath)
        for child in doc.iter():
          if child.tag == 'processTree':
            tree = ProcessTree(filePath, child.attrib['id'], child.attrib['root'])
          elif child.tag == 'xor':
            tree.AddNode(Node(NodeType.Xor, child.attrib['id']))
          elif child.tag == 'xorLoop':
            tree.AddNode(Node(NodeType.XorLoop, child.attrib['id']))
          elif child.tag == 'and':
            tree.AddNode(Node(NodeType.And, child.attrib['id']))
          elif child.tag == 'andLoop':
            tree.AddNode(Node(NodeType.AndLoop, child.attrib['id']))
          elif child.tag == 'sequence':
            tree.AddNode(Node(NodeType.Sequence, child.attrib['id']))
          elif child.tag == 'sequenceLoop':
            tree.AddNode(Node(NodeType.SequenceLoop, child.attrib['id']))
          elif child.tag == 'manualTask':
            tree.AddNode(Node(NodeType.ManualTask, child.attrib['id'], child.attrib['name']))
          elif child.tag == 'automaticTask':
            tree.AddNode(Node(NodeType.Tau, child.attrib['id']))
          elif child.tag == 'parentsNode':
            parentId = child.attrib['sourceId']
            childId = child.attrib['targetId']
            tree.SetParentalRelation(parentId, childId)
        print('tree size:', tree.GetTreeSize())
        return tree  
            