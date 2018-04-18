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
simThreshold = 0.8
pattern = ProcessTreeLoader.LoadTree(r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\WorkflowPatternFinder\Example Patterns\testPattern.ptml") #(r"C:\temp\test.ptml")
tree = ProcessTreeLoader.LoadTree(r"C:\Thesis\Profit analyses\04-04-2018\ptml\O40264AA-47.ptml") #(r"C:\Thesis\Profit analyses\22-02-2018\testPattern.ptml")
pd = PatternDiscovery()
pd.SetTrainedModelPath(modelPath)
pd.SetSimilarityThreshold(simThreshold)
pd.SetSimilarityVariant("max")
print(pd.IsValidSubTree(tree, pattern, False))