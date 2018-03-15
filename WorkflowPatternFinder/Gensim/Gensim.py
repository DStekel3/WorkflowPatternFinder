import enums # learn more: https://python.org/pypi/enums
import sys
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *


if len(sys.argv)==4:
    modelPath = sys.argv[1]
    treePath = sys.argv[2]
    patternPath = sys.argv[3]
    
    print('loading tree...')
    tree = ProcessTreeLoader.LoadTree(treePath)
    print('loading pattern...')
    pattern = ProcessTreeLoader.LoadTree(patternPath)
    finder = SubTreeFinder()
    finder.SetTrainedModelPath(modelPath)
    result = finder.IsValidSubTree(tree, pattern, False)
    print(result)
else:
  print("False number of arguments")

