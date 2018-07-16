#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import *
import nltk
from SynoniemenDotNet import XmlParser
from nltk.tag import UnigramTagger, BigramTagger, PerceptronTagger
from nltk.corpus import alpino as alp
from gensim.parsing.preprocessing import strip_short, strip_numeric, strip_punctuation
import time
import pickle
import spacy

class Query(object):
    
    def __init__(self):
        self._model = None
        self._synonyms = {}
        self._antonyms = {}
        self._xmlParser = XmlParser()
        #training_corpus = alp.tagged_sents()

        curTime = time.time()
        # PerceptronTagger (takes very long to train)
        
        #try:
        #    f = open('tagger.pckl', 'rb')
        #    self._tagger = pickle.load(f)
        #    f.close()
        #except:
        #    self._tagger = PerceptronTagger(load=True)
        #    self._tagger.train(list(training_corpus))
        #    f = open('tagger.pckl', 'wb')
        #    pickle.dump(self._tagger, f)
        #    f.close()

        # spaCy tokenizer, tagger
        self._tagger = spacy.load('nl')        

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

    def WordsHaveSameType(self, patternWord, treeWord):
        pTag = self._tagger(patternWord)
        tTag = self._tagger(treeWord)
        if [(w.text, w.pos_) for w in pTag][0][1] == [(w.text, w.pos_) for w in tTag][0][1]:
          return True
        return False

    def GetSentenceSimilarityMaxVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')
        filteredPatternSentence = strip_numeric(strip_punctuation(patternSentence.lower())).split()        
        filteredTreeSentence = strip_numeric(strip_punctuation(treeSentence.lower())).split()

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

    def GetSentenceSimilarityAllAverageVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')

        result = (0.0, '')
        filteredPatternSentence = strip_numeric(strip_punctuation(patternSentence.lower())).split()        
        filteredTreeSentence = strip_numeric(strip_punctuation(treeSentence.lower())).split()

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
        for patternWord in patternSentence.lower().split():
            for treeWord in treeSentence.lower().split():
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
        terms = strip_punctuation(term.lower()).split()
        myWords = []
        for word in terms:
          if word in self._model.wv.vocab:
            myWords.append(word)
        if any(myWords):
          similarTerms = self._model.wv.most_similar(positive=myWords, topn=300)
          return similarTerms
        return []
        
        