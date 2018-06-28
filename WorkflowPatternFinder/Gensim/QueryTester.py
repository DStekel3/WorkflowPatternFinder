import gensim
from Query import *


query = Query()
query.LoadBinModel()
answer = query.GetSentenceSimilarityMaxVariant("plaatsen", "goedkeuring")
print(answer)
