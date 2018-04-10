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

  def IsValidSubTree(self, tree, pattern, induced=False):
    # print('induced is ',induced)
    tNode = tree.GetRoot()
    pNode = pattern.GetRoot()
    return self.DoesBranchContainPattern(tNode, pNode, [], induced)

  def GetValidSubTrees(self, tree, pattern, induced=False):
    flatten = lambda x: [item for sublist in x for item in sublist]
    pNode = pattern.GetRoot()
    treeNodes = tree.GetNodes()
    allPatterns = []
    for tNode in treeNodes:
      if tNode.GetId() not in [i[0] for i in allPatterns]:
        result = self.DoesBranchContainPattern(tNode, pNode, allPatterns, induced)
        if result[0]:
          newPattern = True
          for member in result[2]:
            print("matching: member = ", str(member), "result[2] = ", str(result[2]))
            if member in allPatterns:
              newPattern = False
              break
          if newPattern:
            for tuple in result[2]:
              allPatterns.append(tuple)    
    return allPatterns

  def DoesBranchContainPattern(self, tNode, pNode, allPatterns, induced=False):
    patternMembers = []
    simTuple = self.AreSimilar(tNode, pNode)
    if self.IsTupleTrue(simTuple):
      patternMembers.append((tNode.GetId(), self.GetPatternWord(simTuple)))
      print ('pattern members: '+str(patternMembers))
      pChildren = pNode.GetChildren()
      tChildren = tNode.GetChildren()
      if self.ContainsSiblings(tNode, pNode):
        if(any(pChildren)):
          pFound = []
          treeIds = []
          for patternChild in pChildren:
            for treeChild in tChildren:
              if treeChild.GetId() not in treeIds:
                patternChildFound = self.DoesBranchContainPattern(treeChild, patternChild, allPatterns, induced)
                if self.IsTupleTrue(patternChildFound):
                    pFound.append((self.GetScore(patternChildFound), self.GetPatternMembers(patternChildFound)))
                    treeIds.append(treeChild.GetId())
                    break
          if len(pFound) == len(pChildren):
              for node in pFound:
                print('node: ', str(node), ", patternMembers: ", str(patternMembers)) 
                patternMembers.extend((node[1]))
              pFound.append((self.GetScore(simTuple), self.GetPatternMembers(simTuple)))
              total = pFound
              min_score = 1
              try:
                for found in total:
                  score = found[0]
                  if score < min_score:
                    min_score = score
                return (True, min_score, patternMembers)
              except:
                raise ValueError('object in total is not correct')
        else:
          print ('in else statement')
          return (True, self.GetScore(simTuple), patternMembers)
    if induced and pNode.IsRoot() or not induced:
      for treeChild in tNode.GetChildren():
        found = self.DoesBranchContainPattern(treeChild, pNode, allPatterns, induced)
        if(self.IsTupleTrue(found)):
          return (True, self.GetScore(found), self.GetPatternMembers(found), self.GetPatternWord(found))
    return (False, 0)

  def IsTupleTrue(self, tuple):
    try:
      return tuple[0]
    except:
      raise ValueError("This is not a proper tuple.")

  def GetScore(self, tuple):
    try:
      return tuple[1]
    except:
      raise ValueError("This is not a proper tuple.")

  def GetPatternMembers(self, tuple):
    try:
      return tuple[2]
    except:
      raise ValueError("This is not a proper tuple.")

  def GetPatternWord(self, tuple):
    try:
      return tuple[3]
    except:
      return "";

  def AreSimilar(self, tNode, pNode):
    if pNode.GetType() == tNode.GetType():
      if pNode.GetType() == 'ManualTask':
        return self.AreSimilarAccordingToDoc2Vec(tNode, pNode)
      else:
        return (True, 1, tNode.GetId(), '')
    else:
        return (False, 0, tNode.GetId(), '')
    
  def AreSimilarAccordingToDoc2Vec(self, tNode, pNode):
    score = self._query.GetSimilarity(tNode.GetEvent(), pNode.GetEvent())
    if score[0] > self._simThreshold:
      return (True, score[0], tNode.GetId(), score[1])
    return (False, score[0], tNode.GetId(), score[1])
    
  def ContainsSiblings(self, tNode, pNode):
    similars = []
    pSiblings = pNode.GetSiblings()
    tSiblings = tNode.GetSiblings()
    for pSibling in pSiblings:
      for tSibling in tSiblings:
        if self.IsTupleTrue(self.AreSimilar(pSibling, tSibling)):
          similars.append(tSibling.GetId())
    if len(similars) == len(pSiblings):
      return (True, similars)
    return (False, [])
  
  def SetTrainedModelPath(self, modelPath):
    self._word2VecTrainedModelPath = modelPath
    self.LoadWord2VecModel()
    
  def SetSimilarityThreshold(self, simThreshold):
    self._simThreshold = float(simThreshold)
    # print('sim threshold: ' + str(self._simThreshold))