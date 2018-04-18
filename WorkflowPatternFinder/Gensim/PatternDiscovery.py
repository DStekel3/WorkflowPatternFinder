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
  _orderedTypes = ["sequence", "xorLoop", "sequenceLoop", "andLoop"]
  _similarityVariant = "average"

  def LoadWord2VecModel(self):
    if self._word2VecTrainedModelPath is not None:
      self._query = Query()
      self._query.LoadModel(self._word2VecTrainedModelPath)
    else:
      raise ValueError('The path to model is not set yet.')

  def GetValidSubTrees(self, tree, pattern, isInduced=False):
      self._dict = []
      flatten = lambda x: [item for sublist in x for item in sublist]
      pNode = pattern.GetRoot()
      treeNodes = tree.GetNodes()
      allPatterns = []
      allSelections = []
      for tNode in treeNodes:
        if True: #tNode.GetId() not in [i[0] for i in allPatterns] and tNode.GetId() not in self._dict:
          result = (True, [])
          while result[0]:
            if isInduced:
              result = self.IsInducedPattern(tNode, pNode)
            else:
              result = self.IsSubsumedPattern(tNode, pNode)
            if result[0]:
              newPattern = False
              for member in result[1]:
                print("matching: member = ", str(member), "pattern members = ", str(result[1]))
                if member not in allPatterns:
                  newPattern = True
              if newPattern:
                selection = []
                for tuple in result[1]:
                  selection.append(tree.GetNode(tuple[0]).GetNumber())
                  if tuple not in allPatterns:
                    allPatterns.append(tuple)
                  if tuple[0] not in self._dict:
                    self._dict.append(tuple[0])
              else:
                break
      return allPatterns

  def IsValidSubTree(self, tree, pattern, isInduced):
    # walk through tree nodes, using breadth first search
    self._dict = []
    pNode = pattern.GetRoot()
    nodelist = [tree.GetRoot()]
    while len(nodelist) > 0:
      # pop first element and add its children to the nodelist
      node = nodelist.pop(0)
      for child in node.GetChildren():
        nodelist.append(child)
      result = (False, [])
      if isInduced:
        result = self.IsInducedPattern(node, pNode)
      else:
        result = self.IsSubsumedPattern(node, pNode)
      if result[0]:
        selection = []
        for selectedNode in [i[0] for i in result[1]]:
          selection.append(tree.GetNode(selectedNode).GetNumber())
        return result
    return (False, [])

  def IsSubsumedPattern(self, tNode, pNode, selectedNodes = []):
    # check whether the given nodes are similar
    equal = self.AreSimilar(tNode, pNode)
    rootScore = equal[1]
    if equal[0]:
      tChildren = tNode.GetChildren()
      matches = [(tNode.GetId(), pNode.GetId(), rootScore, self.GetPatternWord(equal))]
      pChildren = pNode.GetChildren()
      for p in pChildren:
        for t in tChildren:
          if t.GetId() not in self._dict and t.GetId() not in [i[0] for i in matches + selectedNodes]:
            newSelection = deepcopy(selectedNodes)
            newSelection.extend(matches)
            ans = self.IsSubsumedPattern(t, p, newSelection)
            if ans[0]:
              matches.extend(ans[1])
              break
      if len(matches) == len(pNode.GetDescendants()) + 1:
        return (True, matches)
    if not pNode.IsRoot():
      for child in tNode.GetChildren():
        if child.GetId() not in self._dict and child.GetId() not in [i[0] for i in selectedNodes]:
          ans = self.IsSubsumedPattern(child, pNode, selectedNodes)
          if ans[0]:
            return ans
    return (False, [])

  def IsInducedPattern(self, tNode, pNode):
    # search for a node similar as pNode
    if pNode.GetType() in self._orderedTypes:
      return self.IsInducedPatternOrdered(tNode, pNode)
    else:
      return self.IsInducedPatternUnordered(tNode, pNode)
    return (False, [])

  def IsInducedPatternOrdered(self, tNode, pNode):
    # check whether the given nodes are similar
    equal = self.AreSimilar(tNode, pNode)
    rootScore = equal[1]
    if equal[0]:
      pChildren = pNode.GetChildren()
      matches = [(tNode.GetId(), pNode.GetId(), rootScore, self.GetPatternWord(equal))]
      if (len(pChildren) == 0):
        return (True, matches)
      # get possible sets of siblings in the tree and try to match them with
      # the pattern children
      sets = self.GetSetsOfChildren(tNode, len(pChildren))
      for index in range(0, len(sets)):
        set = list(sets[index])
        matches = [(tNode.GetId(), pNode.GetId(), rootScore)]
        score = 0
        match = None
        for n in range(0, len(set)):
          if set[n].GetId() not in [i[0] for i in matches]:
            ans = self.AreSimilar(set[n], pChildren[n])
            if ans[0]:
              matches.append((set[n], pChildren[n].GetId(), ans[1]))
        if len(matches) == len(pChildren) + 1:
          final = []
          matches = [(tNode.GetId(), pNode.GetId(), rootScore, self.GetPatternWord(equal))]
          for place in range(0, len(pChildren)):
            lastAns = self.IsInducedPattern(set[place], pChildren[place])
            if p.GetId() not in [i[0] for i in matches]:
              if lastAns[0]:
                final.append(lastAns)
                matches.extend(lastAns[1])
          if len(final) == len(pChildren) and False not in [res[0] for res in final]:
            return (True, matches)
    return (False, [])

  def IsInducedPatternUnordered(self, tNode, pNode):
    # check whether the given nodes are similar
    equal = self.AreSimilar(tNode, pNode)
    rootScore = equal[1]
    if equal[0]:
      pChildren = pNode.GetChildren()
      matches = [(tNode.GetId(), pNode.GetId(), rootScore, self.GetPatternWord(equal))]
      if len(pChildren) == 0:
        return (True, matches)
      # get possible sets of siblings in the tree and try to match them with
      # the pattern children
      sets = self.GetSetsOfChildren(tNode, len(pChildren))
      for index in range(0, len(sets)):
        set = list(sets[index])
        matches = [(tNode.GetId(), pNode.GetId(), rootScore)]
        for p in pChildren:
          score = 0
          match = None
          for s in set:
            if s.GetId() not in [i[0] for i in matches]:
              ans = self.AreSimilar(s, p)
              if ans[0] and ans[1] > score:
                score = ans[1]
                match = s
          if match != None:
            matches.append((match.GetId(), p.GetId(), score))
        # when this set matches the children, move within your pattern tree and
        # search further...
        if len(matches) == len(pChildren) + 1:
          final = []
          matches = [(tNode.GetId(), pNode.GetId(), rootScore, self.GetPatternWord(equal))]
          for p in pChildren:
            t = set[pChildren.index(p)]
            if p.GetId() not in [i[0] for i in matches]:
              pChildAns = self.IsInducedPattern(t, p)
              if pChildAns[0]:
                final.append(pChildAns)
                matches.extend(pChildAns[1])
          if len(final) == len(pChildren) and False not in [res[0] for res in final]:
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