#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import *
import nltk
from nltk.corpus import stopwords
from SynoniemenDotNet import XmlParser
from nltk.tag import UnigramTagger, BigramTagger, PerceptronTagger
from nltk.corpus import alpino as alp
from gensim.parsing.preprocessing import strip_short, strip_numeric, strip_punctuation
import time
import pickle

class Query(object):
    
    def __init__(self):
        self._model = None
        self._synonyms = {}
        self._antonyms = {}
        self._xmlParser = XmlParser()
        self._stoplist = stopwords.words('dutch')
        #training_corpus = alp.tagged_sents()

        curTime = time.time()

    def LoadModel(self, path_to_model):
        # print('loading model')
        self._model = Doc2Vec.load(path_to_model, mmap=None)
        # print('loaded model')

    def LoadBinModel(self, path_to_model=r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\sonar-320.bin"):
        self._model = KeyedVectors.load(path_to_model, mmap='r')

    def FindSynonyms(self, word):
        synonyms = self._xmlParser.WoordenboekGetSynonyms(word)
        self._synonyms[word] = synonyms
        return list(set(synonyms))

    def FindAntonyms(self, word):
        antonyms = self._xmlParser.WoordenboekGetAntonyms(word)
        theirSynonyms = []
        for antonym in antonyms:
          theirSynonyms.extend(self._xmlParser.WoordenboekGetSynonyms(antonym))
        for syn in theirSynonyms:
          if syn not in antonyms:
            antonyms.extend(syn)
        self._antonyms[word] = antonyms
        return list(set(antonyms))


    def RemoveStopwords(self, listOfWords):
        return [w for w in listOfWords if w[0] not in self._stoplist]

    def NormalizeSentence(self, sentence):
        s = strip_numeric(strip_punctuation(sentence.lower())).split()
        return [w for w in s if w not in self._stoplist]

    def GetSentenceSimilarityMaxVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')
        filteredPatternSentence = self.NormalizeSentence(patternSentence)
        filteredTreeSentence = self.NormalizeSentence(treeSentence)
        #filteredPatternSentence = strip_numeric(strip_punctuation(patternSentence.lower())).split()
        #filteredTreeSentence = strip_numeric(strip_punctuation(treeSentence.lower())).split()

        for patternWord in filteredPatternSentence:
            if patternWord not in self._synonyms:
                self.FindSynonyms(patternWord)
            if patternWord not in self._antonyms:
                self.FindAntonyms(patternWord)
            synonyms = self._synonyms[patternWord]
            antonyms = self._antonyms[patternWord]
            for treeWord in filteredTreeSentence:
                  score = -1
                  # uncomment line to add lemmatization check! (Matching on verbs/nouns etc. only)
                  #if self.WordsHaveSameType(patternWord, treeWord):
                  if treeWord in antonyms:
                      return (0.0, '')
                  elif treeWord in synonyms:
                      score = 1
                  elif treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                      score = self._model.wv.similarity(patternWord, treeWord)
                  if score > 1:
                      score = 1
                  if score > result[0]:
                      # print('found (' + str(score) + "," + treeWord + ')')
                      result = (score, treeWord)
        # print('returning: ' +str(result))
        return result

    def GetSentenceSimilarityAllAverageVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')

        result = (0.0, '')
        filteredPatternSentence = self.NormalizeSentence(patternSentence)
        print("from " + patternSentence + " to "+ filteredPatternSentence)
        print("from " + treeSentence+ " to "+ filteredTreeSentence)

        filteredTreeSentence = self.NormalizeSentence(treeSentence)

        for patternWord in filteredPatternSentence:
            if patternWord not in self._synonyms:
                self.FindSynonyms(patternWord)
            if patternWord not in self._antonyms:
                self.FindAntonyms(patternWord)
            synonyms = self._synonyms[patternWord]
            antonyms = self._antonyms[patternWord]
            for treeWord in filteredTreeSentence:
                score = -1
                if self.WordsHaveSameType(patternWord, treeWord):
                  if treeWord in synonyms:
                      score = 1
                  elif treeWord in antonyms:
                      score = 0
                  elif treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                      score = self._model.wv.similarity(patternWord, treeWord)
                  if score > 1:
                      score = 1
                  if score > result[0]:
                      # print('found (' + str(score) + "," + treeWord + ')')
                      result = (score, treeWord)
        # print('returning: ' +str(result))
        return result

    def GetSentenceSimilarityAverageVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')
        scores = []
        bestWord = ""
        filteredPatternSentence = self.NormalizeSentence(patternSentence)
        filteredTreeSentence = self.NormalizeSentence(treeSentence)
        for patternWord in filteredPatternSentence:
            for treeWord in filteredTreeSentence:
                if treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                    score = self._model.wv.similarity(patternWord, treeWord)
                    if len(scores) == 0 or score > max(scores):
                      bestWord = treeWord
                    scores.append(score)
                    # print('found (' + str(score) + "," + treeWord + ')')
        # print('returning: ' +str(result))
        if len(scores) > 0:
          result = (sum(scores) / float(len(scores)), bestWord)
        return result

    def GetMostSimilarTerms(self, term):
      if term.strip() == "":
        return []
      if self._model is None:
        raise ValueError('The model is not loaded yet.')
        return []
      else:
        words = self.NormalizeSentence(term)
        myWords = []
        for word in words:
          if word in self._model.wv.vocab:
            myWords.append(word)
        if any(myWords):
          similarTerms = self._model.wv.most_similar(positive=myWords, topn=300)
          return self.RemoveStopwords(similarTerms)
        return []
        
        