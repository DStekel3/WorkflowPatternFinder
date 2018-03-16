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
        print('loading model')
        self._model = Doc2Vec.load(path_to_model, mmap=None)
        print('loaded model')

    def GetSimilarity(self, fstEvent, sndEvent):
        if self._model is None:
            raise ValueError('The model is not loaded yet.')
        result = 0.000
        for first in fstEvent.lower().split():
            for second in sndEvent.lower().split():
                if first in self._model.wv.vocab and second in self._model.wv.vocab:
                    score = self._model.wv.similarity(first, second)
                    if score > result:
                        result = self._model.wv.similarity(first, second)
        return result