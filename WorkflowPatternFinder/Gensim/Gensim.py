import enums # learn more: https://python.org/pypi/enums
import sys
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
import os
from os import listdir
from os.path import isfile, join

flatten = lambda x: [item for sublist in x for item in sublist]

for arg in sys.argv:
    print(arg)
if len(sys.argv) == 7:
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
    validTrees = []
    allFilePaths = [join(treeBasePath, f) for f in listdir(treeBasePath) if isfile(join(treeBasePath, f))]
    allTreePaths = [f for f in allFilePaths if str(f).endswith('ptml')]
    for treePath in allTreePaths:
        print('tree path:', treePath)
        print('loading tree...')
        tree = ProcessTreeLoader.LoadTree(treePath)
        print('loading pattern...')
        pattern = ProcessTreeLoader.LoadTree(patternPath)
        finder = SubTreeFinder()
        finder.SetTrainedModelPath(modelPath)
        finder.SetSimilarityThreshold(simThreshold)
        if not countPatterns:
          result = finder.IsValidSubTree(tree, pattern, induced)
          if result[0]:
              validTrees.append((treePath, result[1], result[2]))
          print("Final result:" + str(result))
        else:
          result = finder.GetValidSubTrees(tree, pattern, induced)
          if any(result):
            validTrees.append((treePath, len(result), flatten(result)))
          # Implement that script returns number of patterns instead of score of pattern
    if len(validTrees) > 0:
        print("Valid trees:")
        for path, score, patternMembers in validTrees:
            print(path + "," + str(score)+","+",".join(patternMembers))
else:
  print("False number of arguments")

