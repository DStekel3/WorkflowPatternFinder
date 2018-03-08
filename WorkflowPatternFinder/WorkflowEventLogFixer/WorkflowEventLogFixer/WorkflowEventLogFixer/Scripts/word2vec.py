# This script reads in a collection of excel files...
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')

import gensim
import time
import sys 
from pathlib import Path

directory = r"C:\Thesis\Profit analyses\22-02-2018\csv"

args = sys.argv
print("# of args: ", len(args))
if (len(args) > 1):
    print("args:", args)
    directory = args[1]

pathlist = Path(directory).glob('*.csv')

raw_corpus = []

fileIndex = 1

for pathObj in pathlist:
    start = time.time()
    path = str(pathObj)
    # print(path)
    print("Reading file "+ str(fileIndex))
    fileIndex = fileIndex + 1
    # from openpyxl import load_workbook
    # wb = load_workbook(path, read_only=True)
    # ws = wb[wb.sheetnames[0]]
    import csv
    import re
    from gensim.parsing.preprocessing import strip_short
    from gensim.parsing.preprocessing import strip_numeric
    from gensim.parsing.preprocessing import strip_punctuation
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
                sentence = strip_short(strip_numeric(strip_punctuation(event)), 3)
                if sentence.encode("utf-8") not in sentences:
                    sentences.append(sentence.encode("utf-8")) 
            except ValueError:
                print("Could not obtain value of row", row)
                break
        # print("Extending the corpus took", round(time.time() - start, 3), "seconds.")
        raw_corpus.extend(sentences)
  
print("raw corpus:", raw_corpus)

import nltk
from nltk.corpus import stopwords
stoplist = set(stopwords.words('dutch'))

# Lowercase each document, split it by white space and filter out stopwords
texts = [[word for word in document.lower().split() if word not in stoplist and not any(char>126 for char in word)]
         for document in raw_corpus]
         
# Count word frequencies
from collections import defaultdict
frequency = defaultdict(int)
for text in texts:
    for token in text:
        frequency[token] += 1

# Create a set of frequent words
# Only keep words that appear more than once
processed_corpus = [[token for token in text if frequency[token] > 1] for text in texts]
print("processed corpus:", processed_corpus)

from gensim import corpora

dictionary = corpora.Dictionary(processed_corpus)
print("dictionary:", dictionary)

print("token2id:", dictionary.token2id)

bow_corpus = [dictionary.doc2bow(text) for text in processed_corpus]

print("bow_corpus:", bow_corpus)

from gensim import models
# train the model
tfidf = models.TfidfModel(bow_corpus)