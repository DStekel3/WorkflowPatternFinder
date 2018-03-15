from System import DateTime, String
import gensim
from gensim import *
import sys
sys.path.append(r"C:\Program Files (x86)\IronPython-2.7.7\Lib")
import os
import getopt
import warnings
warnings.filterwarnings(action='ignore', category=UserWarning, module='gensim')
import nltk

class Iron:
        value = 0
        def __init__(self):
            self.model = ""
            pass

        def HelloWorld(self):
            self.value = 3
            blurb = String.Format("{0} {1}", "Hello World! The current date and time is ", DateTime.Now) 
            return blurb

        def GetValue(self):
            return self.value

        def LoadModel(self):
            path_to_model = os.path.abspath(os.path.join(sys.argv[1], os.pardir))
            self.model = Doc2Vec.load(path_to_model+"\\trained.gz", mmap=None)

        def CalculateSimilarity(self, sentence_1, sentence_2):
            result = 0.000
            # print(sys.argv)
            for first in sentence_1.lower().split():
                for second in sentence_2.lower().split():
                    if first in self.model.wv.vocab and second in self.model.wv.vocab:
                        # print(first+";"+second+":")
                        score = self.model.wv.similarity(first, second)
                        if score > result:
                            result = score
            return result