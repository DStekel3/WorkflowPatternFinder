#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import Doc2Vec
import nltk


class Query(object):
    
    def __init__(self):
        self._model = None

    def LoadModel(self, path_to_model):
        # print('loading model')
        self._model = Doc2Vec.load(path_to_model, mmap=None)
        # print('loaded model')

    def GetSentenceSimilarityMaxVariant(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')

        for patternWord in patternSentence.lower().split():
            for treeWord in treeSentence.lower().split():
                if treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                    score = self._model.wv.similarity(patternWord, treeWord)
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
          result = (sum(scores)/float(len(scores)), bestWord)
        return result

    def GetMostSimilarTerms(self, term):
      if term.strip() == "":
        return []
      if self._model is None:
        raise ValueError('The model is not loaded yet.')
        return []
      else:
        terms = term.lower().split()
        myWords = []
        for word in terms:
          if word in self._model.wv.vocab:
            myWords.append(word)
        similarTerms = self._model.wv.most_similar(positive=myWords, topn=100)
        return similarTerms
        
        