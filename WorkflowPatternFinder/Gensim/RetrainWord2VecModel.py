# This script reads in a collection of excel files...
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')

import gensim
import time
import sys 
from pathlib import Path
import os
import gensim
from gensim import models
from gensim.models import doc2vec
from gensim.models import Doc2Vec
from gensim.models.doc2vec import TaggedDocument

path_to_model = r"C:\Thesis\Profit analyses\testmap\trained.gz"
_window = 2
_min_count = 5
_epochs = 10

args = sys.argv
sys.stdout.writelines("# of args: "+str(len(args)))

for arg in args:
  sys.stdout.writelines(arg)
  print(arg)

if len(args) >= 2:
    print("args:", args)
    path_to_model = args[1]
    if os.path.isfile(args[1]):
      sys.stdout.writelines("valid file found!")
    else:
      sys.stdout.writelines(args[1]+ " is not a file!")

if len(args) == 5:
        _window = args[2]
        _min_count = args[3]
        _epochs = args[4]

model = Doc2Vec.load(path_to_model, mmap=None)
model._clear_post_train()

model.train(documents = model.docvecs, total_examples=model.corpus_count, epochs=model.epochs)

model.save(path_to_model)
print("Trained model and saved to "+path_to_model)