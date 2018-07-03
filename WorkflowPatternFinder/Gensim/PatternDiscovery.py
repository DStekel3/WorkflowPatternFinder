import gensim # learn more: https://python.org/pypi/gensim
import sys
import enum
from ProcessTree import *
from Node import *
from ProcessTreeLoader import *
from Query import *
import itertools
from copy import deepcopy

import PatternDiscovery

class PatternDiscovery(object):
  _pythonExe = None
  _word2VecTrainedModelPath = None
  _query = None
  _dict = []
  _orderedTypes = ["Sequence", "XorLoop", "SequenceLoop", "AndLoop"]
  _similarityVariant = "max"

  def LoadWord2VecModel(self):
    if self._word2VecTrainedModelPath is not None:
      self._query = Query()
      self._query.LoadBinModel(self._word2VecTrainedModelPath)
    else:
      self._word2VecTrainedModelPath = r'C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\wikipedia-160.bin'
      # raise ValueError('The path to model is not set yet.')
      self.LoadWord2VecModel()

  def GetMatches(self, T, P, isInduced=False):
    # walk through tree nodes, using breadth first traversal
    self._dict = []
    p = P.GetRoot()
    nodelist = [T.GetRoot()]
    allMatches = []
    isMatch = True
    while(isMatch):
      result = self.GetMatch(T, P, isInduced)
      isMatch = result[0]
      links = result[1]
      if isMatch:
        allMatches.extend(links)
        for treeId in [a[0] for a in links]:
          treeNode = T.GetNode(treeId)
          if treeNode.GetType() == "ManualTask":
            T.RemoveNode(treeNode)
    return allMatches

  def GetValidSubTrees(self, T, P, isInduced=False):
      self._dict = []
      flatten = lambda x: [item for sublist in x for item in sublist]
      p = P.GetRoot()
      treeNodes = T.GetNodes()
      allPatterns = []
      allSelections = []
      for t in treeNodes:
        if True: #tNode.GetId() not in [i[0] for i in allPatterns] and tNode.GetId() not in
                 #self._dict:
          result = (True, [])
          while result[0]:
            if isInduced:
              result = self.GetInducedMatch(t, p)
            else:
              result = self.GetSubsumedMatch(t, p)
            if result[0]:
              newPattern = False
              for member in result[1]:
                #print("matching: member = ", str(member), "pattern members =
                #", str(result[1]))
                if member not in allPatterns:
                  newPattern = True
              if newPattern:
                selection = []
                for tuple in result[1]:
                  selection.append(T.GetNode(tuple[0]).GetNumber())
                  if tuple not in allPatterns:
                    allPatterns.append(tuple)
                  if tuple[0] not in self._dict:
                    self._dict.append(tuple[0])
              else:
                break
      return allPatterns

  def GetMatch(self, T, P, isInduced):
    # walk through tree nodes, using breadth first traversal
    self._dict = []
    p = P.GetRoot()
    nodelist = [T.GetRoot()]
    while len(nodelist) > 0:
      # pop first element and add its children to the nodelist
      t = nodelist.pop(0)
      for child in t.GetChildren():
        nodelist.append(child)
      result = (False, [])
      if self.AreSimilar(t, p):
        if isInduced:
          result = self.GetInducedMatch(t, p)
        else:
          result = self.GetEmbeddedMatchDFS(t, p)
        if result[0]:
          return result
    return (False, [])

  def GetInducedMatch(self, t, p, allMatches = []):
    # search for a node similar as pNode
    if p.GetType() in self._orderedTypes:
      return self.GetInducedOr(t, p, allMatches)
    else:
      return self.GetInducedUn(t, p, allMatches)
    return (False, [])

  def GetEmbeddedMatchDFS(self, t, p, allMatches=[]):
    # search for a node similar as pNode
    if p.GetType() in self._orderedTypes:
      return self.GetEmbeddedOrDFS(t, p, allMatches)
    else:
      return self.GetEmbeddedUnDFS(t, p, allMatches)
    return (False, [])

  def GetEmbeddedOrDFS(self, t, p, allMatches):
    rootMatch = self.AreSimilar(t,p)
    rootScore = rootMatch[1]
    if rootMatch[0]:
      pChildren = p.GetChildren()
      tDescendants = t.GetAllDescendants()
      # first try to find an induced match
      match = self.GetInducedOr(t,p, allMatches)
      if match[0]:
        return match

      matches = [(t.GetId(), p.GetId(), rootMatch[1], self.GetPatternWord(rootMatch))]
      startIndex = 0
      for pc in pChildren:
          bestMatch = (None, 0.0)
          for tc in tDescendants[startIndex:len(tDescendants) - (len(pChildren) - pChildren.index(pc) - 1)]:
              if tc.GetId() not in [i[0] for i in (allMatches + matches)]:
                      match = self.GetEmbeddedMatchDFS(tc,pc, allMatches + matches)
                      if(match[0]):
                          rootScore = match[1][0][2]
                          bestMatch = (match[1], rootScore)
          if bestMatch[0] != None:
              matches.extend(bestMatch[0])
              tNodeId = bestMatch[0][0][0]
              startIndex = tDescendants.index([i for i in tDescendants if i.GetId() == tNodeId][0])+1

      if len(matches) == p.GetSubtreeSize():
        return (True, matches)
    return (False, [])

  def GetEmbeddedUnDFS(self, t, p, allMatches):
    rootMatch = self.AreSimilar(t,p)
    rootScore = rootMatch[1]
    if rootMatch[0]:
      # first try to find an induced match
      match = self.GetInducedUn(t,p, allMatches)
      if match[0]:
        return match

      matches = [(t.GetId(), p.GetId(), rootMatch[1], self.GetPatternWord(rootMatch))]
      for pc in p.GetChildren():
        bestMatch = (None, 0.0)
        for tc in t.GetAllDescendants():
              if tc.GetId() not in [i[0] for i in (allMatches + matches)]:
                      match = self.GetEmbeddedMatchDFS(tc,pc, allMatches + matches)
                      if(match[0]):
                          bestMatch = (match[1], match[1][0][2])
                          print('best match:', bestMatch)
        if bestMatch[0] != None:
            matches.extend(bestMatch[0])
      if len(matches) == p.GetSubtreeSize():
        return (True, matches)
    return (False, [])

  def GetInducedOr(self, t, p, allMatches):
    # check whether the given nodes are similar
    equal = self.AreSimilar(t, p)
    rootScore = equal[1]
    if equal[0]:
      pChildren = p.GetChildren()
      tChildren = t.GetChildren()
      matches = [(t.GetId(), p.GetId(), rootScore, self.GetPatternWord(equal))]
      if (len(pChildren) == 0):
        return (True, matches)
      startIndex = 0
      #for pc in pChildren:
      #    for tc in tChildren[startIndex:len(tChildren)]:
      #        if self.AreSimilar(tc, pc)[0]:
      #            match = self.GetInducedMatch(tc, pc)
      #            if match[0]:
      #                matches.extend(match[1])
      #                startIndex = tChildren.index(tc)
      #                break
      for pc in pChildren:
          bestMatch = (None, 0.0)
          for tc in tChildren[startIndex:len(tChildren) - (len(pChildren) - pChildren.index(pc) - 1)]:
              if tc.GetId() not in [i[0] for i in (allMatches + matches)]:
                  score = self.AreSimilar(tc, pc)
                  if(score[0] and score[1] > bestMatch[1]):
                      match = self.GetInducedMatch(tc,pc)
                      if(match[0]):
                          bestMatch = (match[1], score[1])
          if bestMatch[0] != None:
              matches.extend(bestMatch[0])
              startIndex = tChildren.index(tc) + 1
      if len(matches) == p.GetSubtreeSize():
            return (True, matches)
    return (False, [])

  def GetInducedUn(self, t, p, allMatches):
    # check whether the given nodes are similar
    equal = self.AreSimilar(t, p)
    rootScore = equal[1]
    if equal[0]:
      pChildren = p.GetChildren()
      tChildren = t.GetChildren()
      matches = [(t.GetId(), p.GetId(), rootScore, self.GetPatternWord(equal))]
      if (len(pChildren) == 0):
        return (True, matches)
      for pc in pChildren:
          bestMatch = (None, 0.0)
          for tc in tChildren:
              if tc.GetId() not in [i[0] for i in allMatches + matches]:
                  score = self.AreSimilar(tc, pc)
                  if(score[0] and score[1] > bestMatch[1]):
                      match = self.GetInducedMatch(tc,pc, allMatches)
                      if(match[0]):
                          bestMatch = (match[1], score[1])
          if bestMatch[0] != None:
              matches.extend(bestMatch[0])
      if len(matches) == p.GetSubtreeSize():
            return (True, matches)
    return (False, [])
  
  def GetSetsOfChildren(self, tNode, size):
    children = tNode.GetChildren()
    return list(itertools.combinations(children, size))
  
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
      return ""

  def AreSimilar(self, tNode, pNode):
    if pNode.GetType() == tNode.GetType():
      if pNode.GetType() == 'ManualTask':
        return self.AreSimilarAccordingToDoc2Vec(tNode, pNode)
      else:
        return (True, 1, tNode.GetId(), '')
    else:
        return (False, 0, tNode.GetId(), '')
    
  def AreSimilarAccordingToDoc2Vec(self, tNode, pNode):
    score = 0
    if self._similarityVariant == "max":
      score = self._query.GetSentenceSimilarityMaxVariant(tNode.GetEvent(), pNode.GetEvent())
    elif self._similarityVariant == "average":
      score = self._query.GetSentenceSimilarityAverageVariant(tNode.GetEvent(), pNode.GetEvent())
    if score[0] > self._simThreshold:
      return (True, score[0], tNode.GetId(), score[1])
    return (False, score[0], tNode.GetId(), score[1])
  
  def SetTrainedModelPath(self, modelPath):
    self._word2VecTrainedModelPath = modelPath
    self.LoadWord2VecModel()
    
  def SetSimilarityThreshold(self, simThreshold):
    self._simThreshold = float(simThreshold)
    # print('sim threshold: ' + str(self._simThreshold))

  def SetSimilarityVariant(self, similarityVariant):
    self._similarityVariant = similarityVariant
    