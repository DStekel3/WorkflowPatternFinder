from graphviz import Digraph
import os
os.environ["PATH"] += os.pathsep + 'C:/Program Files (x86)/Graphviz2.38/bin/'
import enums # learn more: https://python.org/pypi/enums
import sys
import random
from Node import *
from ProcessTree import *
from ProcessTreeLoader import *
from SubTreeFinder import *
from os import listdir
from os.path import isfile, join

if len(sys.argv) == 4:
  tree = ProcessTreeLoader.LoadTree(sys.argv[1])
  patternMembers = sys.argv[2].split(',')
  workflowName = sys.argv[3]

  dot = Digraph(comment='Workflow Model')

  root = tree.GetRoot()
  nodelist = [root]
  while any(nodelist):
    currentNode = nodelist.pop(0)
    myColor = 'black'
    if currentNode.GetId() in patternMembers:
      myColor = 'red'
    dot.node(currentNode.GetId(), currentNode.GetEvent(), color = myColor)
    parent = currentNode.GetParent()
    if parent:
      dot.edge(parent.GetId(), currentNode.GetId())
    for child in currentNode.GetChildren():
      nodelist.append(child)
    
  print(dot.source)
  dot.render(workflowName+str(random.randint(1,100001))+".gv", view=True)
  
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