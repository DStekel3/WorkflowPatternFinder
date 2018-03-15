#!/usr/bin/python
import sys
import os
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import gensim
from gensim import models
from gensim.models import Doc2Vec
import nltk

# print(len(sys.argv))
# for arg in sys.argv:
  # print(arg)
  
result = 0.000
for arg, x in sys.argv:
    print 'argument ', x, arg

if len(sys.argv)==4:
    path_to_model = os.path.abspath(os.path.join(sys.argv[1], os.pardir))
    model = Doc2Vec.load(path_to_model+"\\trained.gz", mmap=None)
    for first in sys.argv[2].lower().split():
      for second in sys.argv[3].lower().split():
        if first in model.wv.vocab and second in model.wv.vocab:
          score = model.wv.similarity(first, second)
          if score > result:
            result = model.wv.similarity(first, second)
    print(result)
else:
  print("False number of arguments")