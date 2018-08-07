# This script reads in a collection of excel files...
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')

import gensim
import time
import sys 
from pathlib import Path
import os
import csv
import re
from gensim.parsing.preprocessing import strip_short, strip_numeric, strip_punctuation
from gensim.parsing.preprocessing import stem # applies stemming to text. This function is not used yet

directory = r"C:\Thesis\Profit analyses\txt"
_window = 2
_min_count = 1

args = sys.argv
print("# of args: "+str(len(args)))

for arg in args:
  print(arg)

if len(args) >= 2:
    print("args:", args)
    directory = args[1]
    if os.path.isdir(args[1]):
      print("valid directory found!")
    else:
      print(args[1]+ " is not a directory!")

if len(args) >= 4:
        _min_count = int(args[2])
        _window = int(args[3])

pathlist = Path(directory).glob('*.txt')

corpus = []


import nltk
from nltk.corpus import stopwords
stoplist = set(stopwords.words('dutch'))
# {'door', 'heeft', 'doch', 'dit', 'ja', 'is', 'zonder', 'van',
# 'onder', 'reeds', 'met', 'moet', 'tegen', 'geweest', 'ons', 'alles', 
# 'worden', 'hun', 'me', 'wie', 'omdat', 'kan', 'ze', 'hem', 'veel', 'mij', 
# 'de', 'hebben', 'niet', 'uit', 'dat', 'zal', 'daar', 'ben', 'als', 'hier', 
# 'waren', 'kunnen', 'nog', 'of', 'kon', 'was', 'mijn', 'zij', 'tot', 'je', 'over', 
# 'toch', 'niets', 'doen', 'al', 'het', 'een', 'voor', 'zo', 'nu', 'wordt', 'men', 'naar', 
# 'want', 'dus', 'ook', 'andere', 'toen', 'iets', 'had', 'zou', 'er', 'ge', 'na', 'u', 'meer', 
# 'aan', 'eens', 'wil', 'die', 'altijd', 'hoe', 'iemand', 'om', 'deze', 'bij', 'werd', 'dan', 
# 'en', 'in', 'te', 'haar', 'zich', 'der', 'geen', 'uw', 'hij', 'wat', 'heb', 'op', 'maar', 
# 'wezen', 'ik', 'zijn', 'zelf'}


for txtObj in pathlist:
  with open(str(txtObj)) as infile:
    for line in infile:
        sentence = strip_short(strip_numeric(strip_punctuation(line.lower())), 3)
        wordlist = sentence.split()
        corpus.append([w for w in wordlist if w not in stoplist]) 

print('number of sentences: '+str(len(corpus)))


import gensim
from gensim import models

from gensim.models import Word2Vec

print("training word2vec model...")
start = time.time()
model = gensim.models.Word2Vec(corpus, window=_window, min_count=_min_count, workers = 8)
print(model)
print("Training took ", round(time.time() - start, 3), "seconds.")

import os
new_path = os.path.abspath(os.path.join(directory, os.pardir)) + r"\trained.bin"
model.save(os.path.abspath(new_path))
print("Saved model to file: ", new_path)
print("Press Enter to continue...")
input()

####       
#### Bag-of-words approach: Latent Semantic Analysis
####
         
# # Count word frequencies
# from collections import defaultdict
# frequency = defaultdict(int)
# for text in texts:
    # for token in text:
        # frequency[token] += 1

# # Create a set of frequent words
# # Only keep words that appear more than once
# processed_corpus = [[token for token in text if frequency[token] > 1] for text in texts]
# # print("processed corpus:", processed_corpus)

# from gensim import corpora, similarities

# dictionary = corpora.Dictionary(processed_corpus)
# # print("dictionary:", dictionary)

# print("token2id:", dictionary.token2id)

# bow_corpus = [dictionary.doc2bow(text) for text in processed_corpus]
# # print("bow_corpus:", bow_corpus)

# from gensim import models
# # train the model
# tfidf = models.TfidfModel(bow_corpus)

# vec = dictionary.doc2bow("Beoordelen Afkeuren".lower().split())
# index = similarities.SparseMatrixSimilarity(tfidf[bow_corpus], num_features=len(dictionary))
# sims = index[tfidf[vec]]
# threshold = 0.5
# winners = [doc[0] for doc in list(enumerate(sims)) if doc[1] > threshold]
# print("winners:", winners)
# for winner in winners:
    # print(winner, processed_corpus[winner])
# print([doc for doc in list(enumerate(sims)) if doc[0] in winners])