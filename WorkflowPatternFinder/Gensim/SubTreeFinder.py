import gensim # learn more: https://python.org/pypi/gensim
import sys
import enum
from ProcessTree import *
from Node import *
from ProcessTreeLoader import *
from Query import *

class SubTreeFinder(object):
  _pythonExe = None
  _word2VecTrainedModelPath = None
  _query = None
  
  def LoadWord2VecModel(self):
    if self._word2VecTrainedModelPath is not None:
      self._query = Query()
      self._query.LoadModel(self._word2VecTrainedModelPath)
    else:
      raise ValueError('The path to model is not set yet.')
  
  def IsValidSubTree(self, tree, pattern, induced = False):
    print('induced is ',induced)
    tNode = tree.GetRoot()
    pNode = pattern.GetRoot()
    return self.DoesBranchContainPattern(tNode, pNode, induced)
    
  def DoesBranchContainPattern(self, tNode, pNode, induced = False):
    if self.AreSimilar(tNode, pNode):
      pChildren = pNode.GetChildren()
      tChildren = tNode.GetChildren()
      if not self.ContainsSiblings(tNode, pNode):
        return False
      if(any(pChildren)):
        for patternChild in pChildren:
          for treeChild in tChildren:
            print('Looking for a pattern child further in the tree', len(pChildren), len(tChildren))
            patternChildFound = self.DoesBranchContainPattern(treeChild, patternChild, induced)
            if(patternChildFound):
              return True
          break
      else:
        return True
    elif induced and not pNode.IsRoot():
      return False
    if induced and pNode.IsRoot() or not induced:
      for treeChild in tNode.GetChildren():
        print('Looking for the current node further in the tree')
        found = self.DoesBranchContainPattern(treeChild, pNode, induced)
        if(found):
          return True
    return False
 
  def AreSimilar(self, tNode, pNode):
    print(pNode.GetType(), tNode.GetType())
    print('is equal:', pNode.GetType() == tNode.GetType())
    if pNode.GetType() == tNode.GetType():
      if pNode.GetType() == 'ManualTask':
        return self.AreSimilarAccordingToDoc2Vec(tNode, pNode)
      else:
        return True
    else:
        return False
    
  # AreSimilarAccordingToDoc2Vec needs to be implemented
  def AreSimilarAccordingToDoc2Vec(self, tNode, pNode):
    score = Query.GetSimilarity(tNode.GetEvent(), pNode.GetEvent())
    print('score of ', tNode.GetEvent(), pNode.GetEvent(), ':', score)
    if score > 0.0:
      return True
    return False
    
  def ContainsSiblings(self, tNode, pNode):
    tSiblings = tNode.GetSiblings()
    pSiblings = pNode.GetSiblings()
    for p in pSiblings:
      if p not in tSiblings:
        return False
    return True
  
  def SetTrainedModelPath(self, modelPath):
    self._word2VecTrainedModelPath = modelPath
    self.LoadWord2VecModel()
    