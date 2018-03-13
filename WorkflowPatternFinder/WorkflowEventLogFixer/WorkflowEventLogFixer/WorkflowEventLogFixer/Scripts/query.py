#!/usr/bin/python
import sys, getopt, os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import Doc2Vec
import nltk

print(len(sys.argv))
for arg in sys.argv:
  print(arg)
if len(sys.argv)==4:
    print(sys.argv)
    path_to_model = os.path.abspath(os.path.join(sys.argv[1], os.pardir))
    for first in sys.argv[2].lower().split():
      for second in sys.argv[3].lower().split():
        model = Doc2Vec.load(path_to_model+"\\trained_old.gz", mmap=None)
        if first in model.wv.vocab and second in model.wv.vocab:
          print(first+";"+second+":", model.wv.similarity(first, second))
else:
  print("False number of arguments")