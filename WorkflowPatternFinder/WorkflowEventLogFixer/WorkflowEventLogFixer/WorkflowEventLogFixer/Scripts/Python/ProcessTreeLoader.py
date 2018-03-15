import sys
from enum import Enum

class NodeType(enum):
    xor = 1
    xorLoop = 2
    and = 3
    andLoop = 4
    sequence = 5
    sequenceLoop = 6
    manualTask = 7
    tau = 8
    
class ProcessTreeLoader:
    def LoadTree(filePath):
        sourceFile = filePath
        