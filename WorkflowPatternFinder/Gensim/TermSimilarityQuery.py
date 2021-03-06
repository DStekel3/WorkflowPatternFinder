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
from SynoniemenDotNet import *
import unidecode

args = sys.argv
print(len(args))         

for arg in args:
  print('arg: '+arg)

if len(args) == 3:
  modelpath = args[1]
  myTerm = args[2]
  query = Query()
  # query.LoadModel(modelpath)
  query.LoadBinModel(modelpath)
  similarTerms = query.GetMostSimilarTerms(myTerm)

  parser = XmlParser()
  terms = strip_punctuation(myTerm.lower()).split()
  synonyms = []
  antonyms = []
  for term in terms:
    synonyms.extend(query.FindSynonyms(term))
    antonyms.extend(query.FindAntonyms(term))
  
    if any(similarTerms):
      similarTerms = list(filter(lambda x: x[0] not in antonyms, similarTerms))

    filteredTerms = similarTerms.copy()

  print("Similar terms:")
  for synonym in synonyms:
    print(synonym+":1")
  for term in filteredTerms:
    try:
      print(str(term[0])+":"+str(term[1]))
    except:
      {}