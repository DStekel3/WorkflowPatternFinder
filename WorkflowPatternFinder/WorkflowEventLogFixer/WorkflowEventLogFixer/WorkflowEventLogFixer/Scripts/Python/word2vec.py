# This script reads in a collection of excel files...
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')

import gensim
import time
import sys 
from pathlib import Path

directory = r"C:\Thesis\Profit analyses\testmap\csv"

args = sys.argv
print("# of args: ", len(args))
for arg in args:
  sys.stdout.write(arg)
if len(args) == 2:
    print("args:", args)
    directory = args[2]
    if os.path.isdir(args[2]):
      sys.stdout.write("valid directory found!")
    else:
      sys.stdout.write(args[2], "is not a directory!")
pathlist = Path(directory).glob('*.csv')

raw_corpus = []

fileIndex = 1

for pathObj in pathlist:
    start = time.time()
    path = str(pathObj)
    # print(path)
    print("Reading file "+ str(fileIndex), end="\r")
    fileIndex = fileIndex + 1
    # from openpyxl import load_workbook
    # wb = load_workbook(path, read_only=True)
    # ws = wb[wb.sheetnames[0]]
    import csv
    import re
    from gensim.parsing.preprocessing import strip_short, strip_numeric, strip_punctuation
    from gensim.parsing.preprocessing import stem # applies stemming to text. This function is not used yet
    
    with open(path) as csvfile:
        ws = csv.DictReader(csvfile, delimiter=';')
        # print("Loading in csv took", round(time.time() - start, 3), "seconds.")
        start = time.time()
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
# print("stop words:", stoplist)

# Lowercase each document, split it by white space and filter out stopwords
texts = [[word for word in document.lower().split() if word not in stoplist and not any(char>126 for char in word)]
         for document in raw_corpus]
        
####
#### Doc2Vec approach
####

import gensim
from gensim import models
from gensim.models import doc2vec
from gensim.models.doc2vec import TaggedDocument

taggedDocs = []
for idx, doc in enumerate(texts):
  taggedDocs.append(TaggedDocument([x.decode() for x in doc], str(idx)))

from gensim.models import Doc2Vec
model = gensim.models.Doc2Vec(taggedDocs, window=2, min_count = 5, workers = 4, corpus_count = len(taggedDocs), epochs=10)
print(model)

model.train(taggedDocs, total_examples=model.corpus_count, epochs=model.epochs)

import os

model.save(os.path.abspath(os.path.join(directory, os.pardir)) + "/trained.gz")
print("Trained model and saved to "+dir_path + "/trained.gz")

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