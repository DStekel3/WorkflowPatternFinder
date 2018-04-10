#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import Doc2Vec
import nltk
from Query import *

args = sys.argv
print(len(args))

for arg in args:
  print('arg: '+arg)

if len(args) == 3:
  modelpath = args[1]
  myTerm = args[2]
  query = Query()

  query.LoadModel(modelpath)
  similarTerms = query.GetMostSimilarTerms(myTerm)
  #print(similarTerms)
  print("Similar terms:")
  for term in similarTerms:
    print(str(term[0])+":"+str(term[1]))
