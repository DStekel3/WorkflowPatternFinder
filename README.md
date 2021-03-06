# WorkflowPatternFinder
This tool translates workflow logs (.xlsx) into process trees (.ptml), which are used for finding patterns in the corresponding workflow models. 

In short, The Workflow PatternFinder applies the following steps:
1. Splits a .xlsx file (assuming that a file contains event logs from different workflow models). Translates .xlsx documents into mulitple .csv files. Each .csv file stands for the event log of one workflow model. 

2. Converts .csv files into .xes files. This is done by applying a program which is written in Java (see https://github.com/DStekel3/CSV-to-XES). However, you do not have to download this program since the corresponding .jar is already included in WorkflowPatternFinder.

3. Uses the .xes files as input for the application of the Inductive Miner! The Inductive Miner translates a .xes file into a Process tree (.ptml). These tree files get saved in a sublfolder called 'ptml'. The Inductive Miner is a plug-in in a tool called ProM (http://www.promtools.org/doku.php). ProM is one of the requirements you need to have installed in order to be able to run WorkflowPatternFinder.

5. Gives a view of the process trees found. Furthermore, an NLP-technique named Word2Vec can be applied as a way to recognize certain patterns in workflows. Given a pattern as a .ptml file, this program will return all workflow models which contain this pattern. For more info about Word2Vec, I would like to refer you to http://mccormickml.com/2016/04/19/word2vec-tutorial-the-skip-gram-model/.

6. Since the pattern finder bases its outcome on a trained Word2Vec model, extra functionality is added for adjusting this model. Think about tweaking some parameters of Word2Vec. At last, some functions are added to give the user some more insight on the model:
  - You have the option to view which terms are returned as similar, given a couple of words as input. 
  - The program is also able to return a list of sentences used for the Word2Vec model that contain your input sentence.

Requirements:
- ProM (http://www.promtools.org/doku.php). Be sure to install all packages needed for the Inductive Miner.
- Python 3.4 or higher (https://www.python.org/downloads/) with the following modules:
    1. gensim         (```pip install gensim```)
    2. nltk           (```pip install nltk```)
        - also start python cmd and execute command ```nltk.download("stopwords")```
    3. enums          (```pip install enums```)
    4. graphviz       (```pip install graphviz```)
    5. BeautifulSoup4 (```pip install bs4```)
    6. unidecode      (```pip install unidecode```)
- Java JDK 8 or higher (http://www.oracle.com/technetwork/java/javase/downloads/jdk8-downloads-2133151.html)
- Be sure to set the paths to your Python and Java installation directory as environment variable. The tool uses the command ```where python``` and ```where java``` to find their locations.
<h1>User Manual</h1>
The program basically consists of three tabs. 

Some files can be viewed in Notepad or a PDF viewer. A field where such actions are enabled contains the following codes:
- <b>(PDF)</b> : Opens process tree in a PDF viewer when <b>double left-click</b> on process tree.
- <b>(F)</b>   :   Opens file in Notepad(++) when <b>double right-click</b> file path or process tree.
- <b>(D)</b>   :   Opens directory in system explorer when <b>double left-click</b> on directory path.

<h3>Tab 1 is used for the pre-processing phase.</h3>

![Picture of Tab1](https://github.com/DStekel3/WorkflowPatternFinder/blob/master/Tab1_EDIT.png)
1. Select the directory which contains .xlsx files <b>(D)</b>.
2. Select the directory where ProM is installed <b>(D)</b>.
3. Specify a noise threshold (range: 0-1) for the Inductive Miner infrequent algorithm. This algorithm converts your data into process trees. This threshold is used for filtering out infrequent workflow events. For instance, if your threshold is 0.2, then the events that occur in less than 20% of the workflow traces will be removed.
4. When the previous actions are completed, you can start the pre-processing phase by clicking on this button.
5. Resulting process trees are shown in this table. Whenever the program already finds process trees after you execute step 1, this table also gets updated <b>(PDF + F)</b>.
6. If you want to run the Inductive Miner again, use this button. This skips a lot of other pre-processing functions.

<h3>Tab 2 is for finding a pattern in your processed process trees.</h3>

![Picture of Tab2](https://github.com/DStekel3/WorkflowPatternFinder/blob/master/Tab2_EDIT.png)
1. Select a directory which contains process trees (.ptml files). This directory can be found within the (.xlsx) directory you have selected during the pre-processing phase <b>(D)</b>.
2. Use this textbox to filter which models you are interested in.
3. Select a process tree file (.ptml) which you want to use as workflow pattern. Example patterns are given [here](https://github.com/DStekel3/WorkflowPatternFinder/tree/master/WorkflowPatternFinder/WorkflowPatternFinder/Example%20Patterns) <b>(PDF + F)</b>.
4. Select if you want to apply induced matching or embedded matching.
5. Set a checkmark here if you want to retrieve possibly multiple matches within a tree, otherwise leave it open to stop searching in a model when a match is found.
6. Set a similarity threshold (range: 0-1), used to determine whether terms match or not.
7. Select a similarity variant. The max variant only considers the best match between terms, whereas the average variant considers the average over all matches between terms. 
8. When the previous steps are completed, you can start searching for your given pattern (see step 2) in the given set of process trees  (see step 3).
9. Found matches are shown in this table <b>(PDF + F)</b>.


<h3>Tab 3: select a word2vec model used in tab 2, but you can also experiment here.</h3>

![Picture of Tab3](https://github.com/DStekel3/WorkflowPatternFinder/blob/master/Tab3_EDIT.png)

1. Select a word2vec model (.bin file). Such models are put in the "datasets" directory [here](https://github.com/DStekel3/WorkflowPatternFinder/tree/master/WorkflowPatternFinder/Gensim/datasets). Be aware of there large size.
2. Type a term in this textbox...
3. ... and press this button to get the most similar terms, given by the word2vec model but also by [mijnwoordenboek.nl/synoniemen](http://www.mijnwoordenboek.nl/synoniem.php).
4. The resulting terms get displayed here.

<h3> Tab 4 is for training a word2vec model based on your own data: </h3>

![Picture of Tab4](https://github.com/DStekel3/WorkflowPatternFinder/blob/master/Tab4.png)

1. Select a folder which contains .txt files. Each .txt file needs to have sentences. Each sentence is supposed to be on a seperate line.
2. Set values for the window size and min count of Word2Vec. The window size is the window which Word2Vec uses to determine which surrounding words are context words of a given target word. For example in "Who let the dogs out" and using a window size of 1, 'the' and 'out' are trained as the context words of 'dog'. The min count is the minimum number of times that a word needs to be used in your dataset, otherwise this word will not be included in the training process.

<h1>Creating Patterns</h1>
You also need to define a workflow pattern yourself.
An example is given below and is called the approval pattern ('accordeer patroon' in dutch). 

![figure_accordeerpatroon](https://github.com/DStekel3/WorkflowPatternFinder/blob/master/accordeerpatroon.png)

```xml
<?xml version="1.0" encoding="ISO-8859-1"?>
<ptml>
<processTree id="1488d49a-26a2-48a2-bf03-04abe8a6b317" name="test" root="f5e48d37-e0d4-4d12-92b7-4876e19c6ef3">
<xor id="f5e48d37-e0d4-4d12-92b7-4876e19c6ef3" name=""/>
<manualTask id="fee0310f-4f87-429c-abd4-b416e4ed24a3" name="afkeuren"/>
<manualTask id="aee0310f-4f87-429c-abd4-b416e4ed24a3" name="goedkeuren"/>
<parentsNode id="adf11c87-1cc1-46da-8590-a38296b0c2f7" sourceId="f5e48d37-e0d4-4d12-92b7-4876e19c6ef3" targetId="fee0310f-4f87-429c-abd4-b416e4ed24a3"/>
<parentsNode id="aef11c87-1cc1-46da-8590-a38296b0c2f7" sourceId="f5e48d37-e0d4-4d12-92b7-4876e19c6ef3" targetId="aee0310f-4f87-429c-abd4-b416e4ed24a3"/>
</processTree>
</ptml>
```

The ```<ptml>``` element consists of a ```<processTree>``` element, a set of nodes and a set of ```<parentsNode>```. Node that all elements have an ```id``` property and must have a GUID as value. 
1. ```<processTree>``` needs a ```root``` property, with the ```id``` value of the root node.
2. The set of nodes consists of ```xor, sequence, and, xorLoop, sequenceLoop, andLoop``` and ```manualTask``` elements. Each element needs an ```id``` property and a ```name``` property. However, the value of ```name``` is only relevant for ```manualTask``` elements.
3. At last, you need to define the parent-child relationships between the given nodes. Use ```<parentsNode id='' sourceId='' targetId=''/>```. ```sourceId``` refers to the parent node and ```targetId``` refers to the child node. 

Finally, save it as a .ptml file.

<h1>References:</h1>

1. [Dutch word embeddings datasets](https://github.com/clips/dutchembeddings).
