# Gensim
from gensim.models import *
import time
import os

start = time.time()
# load the word2vec.c-format vectors
model = KeyedVectors.load("datasets/wikipedia-160.bin", mmap='r')

# model = Doc2Vec.load("datasets/wikipedia-160.gz", mmap=None)
print('time elapsed: ', time.time()-start, 'seconds')

# force the unit-normalization, desctructively in-place (clobbering the non-normalized vectors)
# model.init_sims(replace=True)

# model.save_word2vec_format('datasets/wikipedia-160.bin')

print(model.most_similar('goedkeuren'))

# save the model
# new_path = os.path.abspath("datasets/wikipedia-160.gz")
# model.save(os.path.abspath(new_path))