from graphviz import Digraph
import os
os.environ["PATH"] += os.pathsep + 'C:/Program Files (x86)/Graphviz2.38/bin/'
import enums # learn more: https://python.org/pypi/enums
import sys
import re
import random
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
from os import listdir
from os.path import isfile, join

myColors = ['red', 'green', 'lightblue', 'brown', 'pink', 'orange']
matchSize = 1

def GetNextColor(matchIndex):
  return myColors[int(matchIndex / matchSize) % len(myColors)]

if len(sys.argv) == 5:
  tree = ProcessTreeLoader.LoadTree(sys.argv[1])
  patternMembers = []
  if any(sys.argv[2]):
    patternMembers = sys.argv[2].split(',')
  # print('pattern members: ',patternMembers)
  
  patternIds = []
  for pattern in patternMembers:
    patternParts = pattern.split(':')
    patternIds.append((patternParts[0], patternParts[2]))
  workflowName = sys.argv[3]
  matchSize = int(sys.argv[4])
  # print("match size:",matchSize)
  
  myGraph = Digraph()
  root = tree.GetRoot()
  nodelist = [root]
  number = 1
  while any(nodelist):
    currentNode = nodelist.pop(0)
    # print('get event: ',currentNode.GetEvent())
    nodeLabel = currentNode.GetEvent()
    if currentNode.GetId() in [f[0] for f in patternIds]:
      myColor = GetNextColor(([f[0] for f in patternIds]).index(currentNode.GetId()))
      specialWord = [f[1] for f in patternIds if f[0] == currentNode.GetId()][0]
      # print('special word: ' + specialWord)
      if(specialWord != ""):
        pat = re.compile(specialWord, re.IGNORECASE)
        nodeLabel = "<" + pat.sub("<u>" + specialWord + "</u>", currentNode.GetEvent()).replace("\n", "<br/>") + ">"
      myGraph.node(currentNode.GetId(), nodeLabel, style='filled', color = myColor, xlabel= str(number))
    else:
      myGraph.node(currentNode.GetId(), nodeLabel, color = 'black', xlabel= str(number))
    parent = currentNode.GetParent()
    if parent:
      myGraph.edge(parent.GetId(), currentNode.GetId())
    for child in currentNode.GetChildren():
      nodelist.append(child)
    number += 1
    
  # try:
      # print(myGraph.source)
  # except:
      # print("something went wrong when printing the graph to console.")
  myGraph.render(''.join(e for e in workflowName if e.isalnum()) + str(random.randint(1,100001)) + ".gv", view=True)
  
exit
### example graph:
#dot = Digraph(comment='The Round Table')
#dot.node('A', 'King Arthur')
#dot.node('B', 'Sir Bedevere the Wise')
#dot.node('L', 'Sir Lancelot the Brave')
#dot.edges(['AB', 'AL'])
#dot.edge('B', 'L', constraint='false')
#print(dot.source) 
#dot.render("tree_output/tree.gv", view=True)