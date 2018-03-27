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
  
  def IsTupleTrue(self, tuple):
    if len(tuple) == 2:
      return tuple[0]
    raise ValueError('This is not a proper tuple.')
    return False

  def IsValidSubTree(self, tree, pattern, induced=False):
    print('induced is ',induced)
    tNode = tree.GetRoot()
    pNode = pattern.GetRoot()
    return self.DoesBranchContainPattern(tNode, pNode, induced)
    
  def DoesBranchContainPattern(self, tNode, pNode, induced=False):
    simTuple = self.AreSimilar(tNode, pNode)    
    if self.IsTupleTrue(simTuple):
      pChildren = pNode.GetChildren()
      tChildren = tNode.GetChildren()
      if not self.ContainsSiblings(tNode, pNode):
        return (False, 0)
      if(any(pChildren)):
        pFound = []
        for patternChild in pChildren:
          for treeChild in tChildren:
            patternChildFound = self.DoesBranchContainPattern(treeChild, patternChild, induced)
            if self.IsTupleTrue(patternChildFound):
                pFound.append(patternChildFound)
                break
        if len(pFound) == len(pChildren):
            pFound.append(simTuple)
            total = pFound
            min_score = 1
            for bool, score in total:
              if score < min_score:
                min_score = score
            return (True, min_score)
      else:
        return (True, self.GetScore(simTuple))
    elif induced and not pNode.IsRoot():
      return (False, 0)
    if induced and pNode.IsRoot() or not induced:
      for treeChild in tNode.GetChildren():
        found = self.DoesBranchContainPattern(treeChild, pNode, induced)
        if(self.IsTupleTrue(found)):
          return (True, self.GetScore(found))
    return (False, 0)

  def GetScore(self, tuple):
    if len(tuple) == 2:
        return tuple[1]
    raise ValueError("This is not a proper tuple.")

  def AreSimilar(self, tNode, pNode):
    if pNode.GetType() == tNode.GetType():
      if pNode.GetType() == 'ManualTask':
        return self.AreSimilarAccordingToDoc2Vec(tNode, pNode)
      else:
        return (True, 1)
    else:
        return (False, 0)
    
  # AreSimilarAccordingToDoc2Vec needs to be implemented
  def AreSimilarAccordingToDoc2Vec(self, tNode, pNode):
    score = self._query.GetSimilarity(tNode.GetEvent(), pNode.GetEvent())
    print('score of ' + tNode.GetEvent().rstrip() + ";" + pNode.GetEvent().rstrip() + ' is ' + str(score))
    if score > self._simThreshold:
      return (True, score)
    return (False, score)
    
  def ContainsSiblings(self, tNode, pNode):
    similars = []
    pSiblings = pNode.GetSiblings()
    tSiblings = tNode.GetSiblings()
    for pSibling in pSiblings:
      for tSibling in tSiblings:
        if self.IsTupleTrue(self.AreSimilar(pSibling, tSibling)):
          similars.append(True)
    if len(similars) == len(pSiblings):
      return True
    return False
  
  def SetTrainedModelPath(self, modelPath):
    self._word2VecTrainedModelPath = modelPath
    self.LoadWord2VecModel()
    
  def SetSimilarityThreshold(self, simThreshold):
    self._simThreshold = float(simThreshold)
    print('sim threshold: ' + str(self._simThreshold))