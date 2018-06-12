import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')

import gensim
import time
import sys 
from pathlib import Path
import os
import csv
import re
from gensim.parsing.preprocessing import strip_short, strip_numeric, strip_punctuation
from gensim.parsing.preprocessing import stem # applies stemming to text. This function is not used yet
import os.path

modelFile = r"C:\Thesis\Profit analyses\testmap\trained.gz"
inputSentence = []

args = sys.argv
print("# of args: "+str(len(args)))

for arg in args:
  print(arg)

if len(args) == 3:
  print("args:", args)
  modelFile = args[1]
  inputSentence = args[2].split()
  if os.path.isfile(modelFile):
    print("valid model file found!")
    lines = [line.rstrip('\n') for line in open(os.path.join(os.path.dirname(modelFile), "sentences.txt"), 'r')]
    interestingLines = [sentence.split() for sentence in lines if set(inputSentence).issubset(set(sentence.split()[0:len(sentence.split())-1]))]
    print('Sentences:')
    result = []
    sortedLines = sorted(interestingLines, key=lambda x: int(x[-1]), reverse=True)
    for line in sortedLines:
      print(" ".join(line))
  else:
    print(args[1]+ " is not a model file!")
else:
  print("")
