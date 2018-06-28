# Gensim
from gensim.models import *

model = KeyedVectors.load_word2vec_format(r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\combined-320.txt")
model.save(r"C:\Users\dst\Source\Repos\WorkflowPatternFinder\WorkflowPatternFinder\Gensim\datasets\combined-320.bin")
print('done')
exit()
