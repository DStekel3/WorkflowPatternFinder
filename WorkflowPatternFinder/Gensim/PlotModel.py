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
import matplotlib


model = Doc2Vec.load("C:\Thesis\Profit analyses\testmap\trained.gz", mmap=None)
vocab = list(model.wv.vocab)
X = model[vocab]
tsne = TSNE(n_components=2)
X_tsne = tsne.fit_transform(X)
df = pd.DataFrame(X_tsne, index=vocab, columns=['x', 'y'])
fig = plt.figure()
ax = fig.add_subplot(1, 1, 1)

ax.scatter(df['x'], df['y'])