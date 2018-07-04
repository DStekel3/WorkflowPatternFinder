import enums # learn more: https://python.org/pypi/enums
import sys
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
from PatternDiscovery import *
import os
import os.path
from os import listdir
from os.path import isfile, join, basename
import re
import csv

flatten = lambda x: [item for sublist in x for item in sublist]

for index in range(0, len(sys.argv)):
    arg = sys.argv[index]
    print('arg['+ str(index) + '] = '+arg)
if len(sys.argv) == 9:
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
    patternMatch = sys.argv[8]
    print("pattern match:", patternMatch)
    regexPattern  = "^(.*)"+patternMatch+"(.*)$"
    print("pattern regex: ",regexPattern)

    validTrees = []
    allFilePaths = [join(treeBasePath, f) for f in listdir(treeBasePath) if isfile(join(treeBasePath, f))]
    allTreePaths = [f for f in allFilePaths if bool(str(f).endswith('ptml'))]
    
    if(patternMatch != ""):
      workflowNamesPath = os.path.join(os.path.dirname(treeBasePath), "workflownames.csv")
      filteredTreeNames = []
      allRows = []
      with open(workflowNamesPath, 'r') as csvfile:
        spamreader = csv.reader(csvfile, delimiter=';')
        for row in spamreader:
          summary = "-".join(row)
          if bool(re.match(regexPattern, str(summary))):
            filteredTreeNames.append(row[0])
      allTreePaths = [f for f in allTreePaths if os.path.splitext(basename(f))[0] in filteredTreeNames]

    pattern = ProcessTreeLoader.LoadTree(patternPath)
    finder = PatternDiscovery()
    finder.SetTrainedModelPath(modelPath)
    finder.SetSimilarityThreshold(simThreshold)
    finder.SetSimilarityVariant(similarityVariant)
    for treePath in allTreePaths:
        print('Searching in tree ' + str(allTreePaths.index(treePath) + 1) + ' of ' + str(len(allTreePaths)))
        tree = ProcessTreeLoader.LoadTree(treePath)
        if not countPatterns:
          result = finder.GetMatch(tree, pattern, induced)
          if result[0]:
              print('\t found pattern in this tree.')
              validTrees.append((treePath, result[1]))
        else:
          #result = finder.GetValidSubTrees(tree, pattern, induced)
          result = finder.GetMatches(tree, pattern, induced)
          if any(result):
            validTrees.append((treePath, len(result)/pattern.GetTreeSize(), result))
     
    print("Results coming up!")
    print(str(len(validTrees)) +"/"+str(len(allTreePaths))+" matches found.")    
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