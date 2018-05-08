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

directory = r"C:\Thesis\Profit analyses\testmap\csv"
_window = 2
_min_count = 5
_epochs = 5

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

if len(args) == 5:
        _window = int(args[2])
        _min_count = int(args[3])
        _epochs = int(args[4])

pathlist = Path(directory).glob('*.csv')

raw_corpus = []

fileIndex = 1


for pathObj in pathlist:
    path = str(pathObj)
    # print(path)
    print("Reading file "+ str(fileIndex), end="\r")
    fileIndex = fileIndex + 1
    # from openpyxl import load_workbook
    # wb = load_workbook(path, read_only=True)
    # ws = wb[wb.sheetnames[0]]
    
    with open(path) as csvfile:
        ws = csv.DictReader(csvfile, delimiter=';')
        # print("Loading in csv took", round(time.time() - start, 3), "seconds.")
        
        # read all distinct sentences in the current file. We define a sentence as the combination of "taakomschrijving" and "actieomschrijving".
        # here you iterate over the rows in the specific column
        sentences = []
        
        for row in ws:
            # sys.stdout.write("reading line " + str(ws.line_num) + "\r")
            try:
                event = row['Event']
                sentence = strip_short(strip_numeric(strip_punctuation(event)), 4)
                if sentence.encode("utf-8") not in sentences:
                    sentences.append(sentence.encode("utf-8")) 
            except ValueError:
                print("Could not obtain value of row", row)
                break
        # print("Extending the corpus took", round(time.time() - start, 3), "seconds.")
        raw_corpus.extend(sentences)
  
# print("raw corpus:", raw_corpus)

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

# Lowercase each document, split it by white space and filter out stopwords
texts = [[word for word in document.lower().split() if word.decode('utf-8') not in stoplist and not any(char>126 for char in word.lower())]
         for document in raw_corpus]

distinctTexts = [list(x) for x in set(tuple(x) for x in texts)]

import os.path

f = open(os.path.join(os.path.dirname(directory), "sentences.txt"), "w")

for line in distinctTexts:
  print(line)
  elements = [word.decode('utf-8') for word in line]
  elements.append(str(texts.count(line)))
  f.write(' '.join(elements)+"\n")
f.close()

print('number of sentences: '+str(len(texts)))

####
#### Doc2Vec approach
####

import gensim
from gensim import models
from gensim.models import doc2vec
from gensim.models.doc2vec import TaggedDocument

print('tagging docs')

taggedDocs = []
for idx, doc in enumerate(texts):
  taggedDocs.append(TaggedDocument([x.decode() for x in doc], str(idx)))

from gensim.models import Doc2Vec

model = gensim.models.Doc2Vec(taggedDocs, window=_window, min_count=_min_count, epochs=_epochs, corpus_count = len(taggedDocs), workers = 8)
print(model)

start = time.time()
model.train(taggedDocs, total_examples=model.corpus_count, epochs=model.epochs)
print("Training the model took ", round(time.time() - start, 3), "seconds.")
import os

new_path = os.path.abspath(os.path.join(directory, os.pardir)) + "/trained.gz"
model.save(os.path.abspath(new_path))
print("Trained model and saved to ", new_path)

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