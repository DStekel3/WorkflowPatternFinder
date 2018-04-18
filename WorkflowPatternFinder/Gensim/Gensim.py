import enums # learn more: https://python.org/pypi/enums
import sys
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
from PatternDiscovery import *
import os
from os import listdir
from os.path import isfile, join

flatten = lambda x: [item for sublist in x for item in sublist]

for index in range(0, len(sys.argv)):
    arg = sys.argv[index]
    print('arg['+ str(index) + '] = '+arg)
if len(sys.argv) == 8:
    modelPath = sys.argv[1]
    treeBasePath = sys.argv[2]
    patternPath = sys.argv[3]
    induced = False
    countPatterns = False
    if sys.argv[4] == 'True':
        induced = True
    simThreshold = float(sys.argv[5])
    if sys.argv[6] == 'True':
      countPatterns = True
    similarityVariant = sys.argv[7]
    validTrees = []
    allFilePaths = [join(treeBasePath, f) for f in listdir(treeBasePath) if isfile(join(treeBasePath, f))]
    allTreePaths = [f for f in allFilePaths if str(f).endswith('ptml')]
    pattern = ProcessTreeLoader.LoadTree(patternPath)
    finder = PatternDiscovery()
    finder.SetTrainedModelPath(modelPath)
    finder.SetSimilarityThreshold(simThreshold)
    finder.SetSimilarityVariant(similarityVariant)
    
    for treePath in allTreePaths:
        print('Searching in tree ' + str(allTreePaths.index(treePath) + 1) + ' of ' + str(len(allTreePaths)))
        tree = ProcessTreeLoader.LoadTree(treePath)
        if not countPatterns:
          result = finder.IsValidSubTree(tree, pattern, induced)
          if result[0]:
              print('\t found pattern in this tree.')
              validTrees.append((treePath, result[1]))
        else:
          result = finder.GetValidSubTrees(tree, pattern, induced)
          if any(result):
            validTrees.append((treePath, len(result)/pattern.GetTreeSize(), result))
          
    if len(validTrees) > 0:
        print("Valid trees:")
        for tree in validTrees:
            path = tree[0]
            score = "0"
            patternMembers = []
            if not countPatterns:
              score = min([i[2] for i in tree[1]])
              patternMembers = tree[1]
            else:
              score = tree[1]
              patternMembers = tree[2]
            print(path + ";" + str(score) + ";" + ";".join(map(str, patternMembers)))

else:
  print("False number of arguments")