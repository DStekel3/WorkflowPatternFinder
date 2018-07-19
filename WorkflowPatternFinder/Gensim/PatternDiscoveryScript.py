import gensim # learn more: https://python.org/pypi/gensim
import sys
import enum
from ProcessTree import *
from Node import *
from ProcessTreeLoader import *
from Query import *
import itertools
from PatternDiscovery import *

simThreshold = 0.8
countPatterns = True
isInduced = False
pattern = ProcessTreeLoader.LoadTree(r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\WorkflowPatternFinder\Example Patterns\accordeer1.ptml") #(r"C:\temp\test.ptml")
tree = ProcessTreeLoader.LoadTree(r"C:\Thesis\Profit analyses\04-04-2018\ptml\O59533AA-39.ptml") #(r"C:\Thesis\Profit analyses\22-02-2018\accordeer1.ptml")
pd = PatternDiscovery()
pd.LoadWord2VecModel()
pd.SetSimilarityThreshold(simThreshold)
pd.SetSimilarityVariant("max")
if countPatterns:
  match = pd.GetMatches(tree, pattern, isInduced)
  score = ",".join([str(i) for i in match[1]])
  numberOfMatches = len(score)
  print("# of matches:", numberOfMatches)
else:
  match = pd.GetMatchPostOrder(tree, pattern, isInduced)
  score = match[1]
  
print(match)
print("score:", score) 
