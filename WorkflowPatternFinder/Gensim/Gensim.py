import enums # learn more: https://python.org/pypi/enums
import sys
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
import os
from os import listdir
from os.path import isfile, join

for arg in sys.argv:
    print(arg)
if len(sys.argv)==6:
    modelPath = sys.argv[1]
    treeBasePath = sys.argv[2]
    patternPath = sys.argv[3]
    induced = False
    if sys.argv[4] == 'True':
        induced = True
    simThreshold = float(sys.argv[5])

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
        result = finder.IsValidSubTree(tree, pattern, induced)
        if result:
            validTrees.append(treePath)
        print("Final result:"+str(result))
    
    if len(validTrees) > 0:
        print("Valid trees:")
        for validTree in validTrees:
            print(validTree, )
else:
  print("False number of arguments")

