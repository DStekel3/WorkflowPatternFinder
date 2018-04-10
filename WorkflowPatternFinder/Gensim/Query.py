#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import Doc2Vec
import nltk
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')


class Query(object):
    
    def __init__(self):
        self._model = None

    def LoadModel(self, path_to_model):
        # print('loading model')
        self._model = Doc2Vec.load(path_to_model, mmap=None)
        # print('loaded model')

    def GetSimilarity(self, treeSentence, patternSentence):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = (0.0, '')
        for treeWord in treeSentence.lower().split():
            for patternWord in patternSentence.lower().split():
                if treeWord in self._model.wv.vocab and patternWord in self._model.wv.vocab:
                    score = self._model.wv.similarity(treeWord, patternWord)
                    if score > result[0]:
                        result = (self._model.wv.similarity(treeWord, patternWord), treeWord)
        # print('returning: ' +str(result))
        return result