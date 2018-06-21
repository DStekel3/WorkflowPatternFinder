import gensim # learn more: https://python.org/pypi/gensim
import sys
import enum
from ProcessTree import *
from Node import *
from ProcessTreeLoader import *
from Query import *
import itertools
from PatternDiscovery import *

modelPath = r"C:\Thesis\Profit analyses\04-04-2018\trained.gz"
simThreshold = 0.6
pattern = ProcessTreeLoader.LoadTree(r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\WorkflowPatternFinder\Example Patterns\accordeer1.ptml") #(r"C:\temp\test.ptml")
tree = ProcessTreeLoader.LoadTree(r"C:\Thesis\Profit analyses\testmap\testje\subsumed3.ptml") #(r"C:\Thesis\Profit analyses\22-02-2018\accordeer1.ptml")
pd = PatternDiscovery()
pd.SetTrainedModelPath(modelPath)
pd.SetSimilarityThreshold(simThreshold)
pd.SetSimilarityVariant("max")
print(pd.GetMatch(tree, pattern, False))