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

def PrintMatchVariants(validMatches, patternTree):
  variantSize = patternTree.GetNumberOfManualTasks()
  for validMatch in validMatches:
    treePath, overallScore, termMatches = validMatch
    patternEvents = {}
    totalNumOfManualTasksFound = 0
    for termMatch in termMatches:
      treeId, patternId, score, term = termMatch
      if term != "":
        if patternId not in patternEvents:
          patternEvents[patternId] = patternTree.GetNode(patternId).GetEvent()
        patternTerm = patternEvents[patternId]
        treeWord, sentence = term
        print(treePath+";"+patternTerm+";"+treeWord+";"+sentence.replace(';', '')+";"+str(score)+";"+str(int(totalNumOfManualTasksFound/variantSize)))
        totalNumOfManualTasksFound = totalNumOfManualTasksFound+1

for index in range(0, len(sys.argv)):
    arg = sys.argv[index]
    print('arg['+ str(index) + '] = '+arg)
if len(sys.argv) == 9:
    modelPath, treeBasePath, patternPath = sys.argv[1], sys.argv[2], sys.argv[3]
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
          if bool(re.match(regexPattern, str(summary), re.IGNORECASE)):
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
          #result = finder.GetMatch(tree, pattern, induced)
          result = finder.GetMatchPostOrder(tree, pattern, induced)
          if result[0]:
              print('\t found pattern in this tree.')
              validTrees.append((treePath, str(result[1]), result[0]))
        else:
          result = finder.GetMatches(tree, pattern, induced)
          if any(result):
            validTrees.append((treePath, "-".join([str(i) for i in result[1]]), result[0]))
     
    print("Results coming up!")
    # Print an overview of the matches found as a one-liner.
    numOfMatches = sum([len(i[1].split('-')) for i in validTrees])
    if(numOfMatches == 0):
      sys.exit()
    totalScore = sum([float(s) for sublist in [i[1].split('-') for i in validTrees] for s in sublist])
    avgScore = totalScore/numOfMatches
    print(str(numOfMatches)+"/"+str(len(validTrees))+"/"+str(len(allTreePaths))+"/"+str(avgScore))

    # Print all match variants.
    PrintMatchVariants(validTrees, pattern)

    # Print a complete one-liner per model that contains a match.
    if len(validTrees) > 0:
        print("Valid trees:")
        for tree in validTrees:
            path, score, patternMembers = tree
            print(path + ";" + str(score) + ";" + ";".join(map(str, patternMembers)))

else:
  print("False number of arguments")