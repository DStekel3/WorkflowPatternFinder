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

    def GetSentenceSimilarity(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')

        for patternWord in patternSentence.lower().split():
            for treeWord in treeSentence.lower().split():
                if treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                    score = self._model.wv.similarity(patternWord, treeWord)
                    if score > result[0]:
                        print ('returning '+ str((self._model.wv.similarity(patternWord, treeWord), treeWord)))
                        result = (self._model.wv.similarity(patternWord, treeWord), treeWord)
        # print('returning: ' +str(result))
        return result

    def GetMostSimilarTerms(self, term):
        similarTerms = self._model.wv.most_similar(positive=[term.lower()], topn=20)
        return similarTerms
        
        