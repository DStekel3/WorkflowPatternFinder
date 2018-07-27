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
      self._word2VecTrainedModelPath = r'C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\sonar-320.bin'
      # raise ValueError('The path to model is not set yet.')
      self.LoadWord2VecModel()

  def GetMatches(self, T, P, isInduced=True):
    # continue searching as long as you find a match.
    allMatches = []
    theirScores = []
    isMatch = True
    while(isMatch):
      result = self.GetMatchPostOrder(T, P, isInduced) #self.GetMatch(T, P, isInduced)
      matchedNodes = result[0]
      score = result[1]
      isMatch = any(matchedNodes)
      if isMatch:
        links = matchedNodes
        allMatches.extend(links)
        theirScores.append(score)
        print(links)
        removeNodes = [T.GetNode(a[0]) for a in links if T.GetNode(a[0]).GetType() == "ManualTask"]
        if not any(removeNodes):
          removeNodes = [T.GetNode(a[0]) for a in links]
        for treeNode in removeNodes:
          T.RemoveNode(treeNode)
    return (allMatches, theirScores)

  def GetMatchPostOrder(self, T, P, isInduced=True):
    # walk through tree nodes, using post order traversal
    self._dict = []
    self._dataTree = T
    p = P.GetRoot()
    nodelist = T.GetPostOrder()
    bestMatch = ((), 0)
    while len(nodelist) > 0:
      t = nodelist.pop(0)
      result = (False, [])
      if self.AreSimilar(t, p):
        if isInduced:
          result = self.GetInducedMatch(t, p)
        else:
          result = self.GetEmbeddedMatch(t, p)
        score = self.GetPatternMatchScore(result)
        if result and score > bestMatch[1]:
          bestMatch = (result, score)
          if score == 1:
            break
    return bestMatch

  def GetPatternMatchScore(self, matches):
    if any(matches):
      return sum([i[2] for i in matches]) / len(matches)
    return 0

  def GetInducedMatch(self, t, p, allMatches=[]):
    # search for a node similar as pNode
    if p.GetType() in self._orderedTypes:
      return self.GetInducedOr(t, p, allMatches)
    else:
      return self.GetInducedUn(t, p, allMatches)
    return []

  def GetEmbeddedMatch(self, t, p, allMatches=[]):
    rootMatch = self.AreSimilar(t,p)
    if rootMatch and not self.IsAscendantOfMatches(t, allMatches):
      # first try to find an induced match
      rootScore = rootMatch[0]
      matches = [(t.GetId(), p.GetId(), rootScore, self.GetPatternWord(rootMatch))]
      for pc in p.GetChildren():
        bestMatch = (None, 0.0)
        for tc in t.GetChildren():
              if tc.GetId() not in [i[0] for i in (allMatches + matches)]:
                      match = self.GetEmbeddedMatch(tc, pc, allMatches + matches)
                      score = self.GetPatternMatchScore(match)
                      if(match and score > bestMatch[1]):
                          bestMatch = (match, score)
                          if score >= 1:
                            break
        if bestMatch[0] != None:
            matches.extend(bestMatch[0])
      if len(matches) == p.GetSubtreeSize():
        return matches

    if not p.IsRoot():
      for td in [tc for tc in t.GetChildren() if tc.GetId() not in [i[0] for i in allMatches]]:
          match = self.GetEmbeddedMatch(td, p, allMatches)
          if match:
            return match
    return []

  def IsAscendantOfMatches(self, tc, allMatches):
    matchIds = [i[0] for i in allMatches]
    for matchId in matchIds: 
      node = self._dataTree.GetNode(matchId)
      desc = node.GetAscendants()
      if tc.GetId() in [i.GetId() for i in desc]:
        return True
    return False

  def GetInducedOr(self, t, p, allMatches):
    # check whether the given nodes are similar
    equal = self.AreSimilar(t, p)
    if equal:
      pChildren = p.GetChildren()
      tChildren = t.GetChildren()
      rootScore = equal[0]
      matches = [(t.GetId(), p.GetId(), rootScore, self.GetPatternWord(equal))]
      if (len(pChildren) == 0):
        return matches
      startIndex = 0
      for pc in pChildren:
          bestMatch = (None, 0.0)
          for tc in tChildren[startIndex:len(tChildren) - (len(pChildren) - pChildren.index(pc) - 1)]:
              if tc.GetId() not in [i[0] for i in (allMatches + matches)]:
                  match = self.GetInducedMatch(tc,pc)
                  score = self.GetPatternMatchScore(match)
                  if(match and score > bestMatch[1]):
                    bestMatch = (match, score)
                    if score >= 1:
                      break
          if bestMatch[0] != None:
              matches.extend(bestMatch[0])
              startIndex = tChildren.index(tc) + 1
      if len(matches) == p.GetSubtreeSize():
            return matches
    return []

  def GetInducedUn(self, t, p, allMatches):
    # check whether the given nodes are similar
    equal = self.AreSimilar(t, p)
    if equal:
      pChildren = p.GetChildren()
      tChildren = t.GetChildren()
      rootScore = equal[0]
      matches = [(t.GetId(), p.GetId(), rootScore, self.GetPatternWord(equal))]
      if (len(pChildren) == 0):
        return matches
      for pc in pChildren:
          bestMatch = (None, 0.0)
          for tc in tChildren:
              if tc.GetId() not in [i[0] for i in allMatches + matches]:
                  match = self.GetInducedMatch(tc,pc, allMatches)   
                  score = self.GetPatternMatchScore(match)
                  if(match and score > bestMatch[1]):
                    bestMatch = (match, score)
          if bestMatch[0] != None:
              matches.extend(bestMatch[0])
      if len(matches) == p.GetSubtreeSize():
            return matches
    return []
  
  def GetSetsOfChildren(self, tNode, size):
    children = tNode.GetChildren()
    return list(itertools.combinations(children, size))

  def GetPatternWord(self, tuple):
    try:
      if len(tuple) == 3:
        return tuple[2]
      elif len(tuple) == 4:
        return (tuple[2], self.EscapeSpecialChars(tuple[3]))
    except:
      return ""

  def EscapeSpecialChars(self, sentence):
      result = sentence.replace('\n', '_')
      result = result.replace('&amp;', '&')
      result = result.replace('&lt;', '<')
      result = result.replace('&gt;', '>')
      result = result.replace('&quot;', '"')
      result = result.replace('&apos;', '`')
      result = result.replace('&euml;', 'ë')
      return result

  def AreSimilar(self, tNode, pNode):
    if pNode.GetType() == tNode.GetType():
      if pNode.GetType() == 'ManualTask':
        return self.AreSimilarAccordingToDoc2Vec(tNode, pNode)
      else:
        return (1, tNode.GetId(), '')
    else:
        return False
    
  def AreSimilarAccordingToDoc2Vec(self, tNode, pNode):
    score = 0
    if self._similarityVariant == "max":
      score = self._query.GetSentenceSimilarityMaxVariant(tNode.GetEvent(), pNode.GetEvent())
    elif self._similarityVariant == "average":
      score = self._query.GetSentenceSimilarityAverageVariant(tNode.GetEvent(), pNode.GetEvent())
    if score[0] >= self._simThreshold:
      return (score[0], tNode.GetId(), score[1], tNode.GetEvent().replace(' ', '_'))
    return ()
  
  def SetTrainedModelPath(self, modelPath):
    self._word2VecTrainedModelPath = modelPath
    self.LoadWord2VecModel()
    
  def SetSimilarityThreshold(self, simThreshold):
    self._simThreshold = float(simThreshold)
    # print('sim threshold: ' + str(self._simThreshold))

  def SetSimilarityVariant(self, similarityVariant):
    self._similarityVariant = similarityVariant
