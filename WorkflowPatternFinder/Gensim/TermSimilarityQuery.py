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
import spacy
import unidecode

args = sys.argv
print(len(args))
_tagger = spacy.load('nl')         

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
  synonyms = query.FindSynonyms(myTerm)
  antonyms = query.FindAntonyms(myTerm)
  
  if any(similarTerms):
    similarTerms = list(filter(lambda x: x[0] not in antonyms, similarTerms))

  acceptedTypeOfWords = []
  pTag = _tagger(unidecode.unidecode(myTerm))
  for i in range(0,len(myTerm.split(' '))):
    pos = pTag[i].pos_
    if pos not in acceptedTypeOfWords:
      acceptedTypeOfWords.append(pos)

  filteredTerms = similarTerms.copy()

  for term in similarTerms:
    word = unidecode.unidecode(term[0])
    tTag = _tagger(word)
    for i in range(0,len(word.split(' '))):
      pos = tTag[i].pos_
      if pos not in acceptedTypeOfWords:
        filteredTerms.remove(term)
    

  print("Similar terms:")
  for synonym in synonyms:
    print(synonym+":1")
  for term in filteredTerms:
    try:
      print(str(term[0])+":"+str(term[1]))
    except:
      {}