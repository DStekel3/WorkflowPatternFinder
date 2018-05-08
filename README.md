# WorkflowPatternFinder
This tool translates workflow logs (.xlsx) into process trees (.ptml), which are used for finding patterns in the corresponding workflow models. 

In short, The Workflow PatternFinder applies the following steps:
1. Splits a .xlsx file (assuming that a file contains event logs from different workflow models). Translates .xlsx documents into mulitple .csv files. Each .csv file stands for the event log of one workflow model. 

2. Converts .csv files into .xes files. This is done by applying a program which is written in Java (see https://github.com/DStekel3/CSV-to-XES). However, you do not have to download this program since the corresponding .jar is already included in WorkflowPatternFinder.

3. Uses the .xes files as input for the application of the Inductive Miner! The Inductive Miner translates a .xes file into a Petri Net (.pnml) or a Process tree (.ptml). Although we do not use the petri net files later, both types are being saves into subfolders. The Inductive Miner is a plug-in in a tool called ProM. ProM is one of the requirements you need to have installed in order to be able to run WorkflowPatternFinder.

5. Gives a view of the process trees found. Furthermore, an NLP-technique named Word2Vec can be applied as a way to recognize certain patterns in workflows. Given a pattern as a .ptml file, this program will return all workflow models which contain this pattern. For more info about Word2Vec, I would like to refer you to http://mccormickml.com/2016/04/19/word2vec-tutorial-the-skip-gram-model/.

6. Since the pattern finder bases its outcome on a trained Word2Vec model, extra functionality is added for adjusting this model. Think about tweaking some parameters of Word2Vec. At last, some functions are added to give the user some more insight on the model:
  - You have the option to view which terms are returned as similar, given a couple of words as input. 
  - The program is also able to return a list of sentences used for the Word2Vec model that contain your input sentence.

Requirements:
- ProM (http://www.promtools.org/doku.php). Be sure to install all packages needed for the Inductive Miner.
- Python 3.4 or higher (https://www.python.org/downloads/)
- Java JDK 8 or higher (http://www.oracle.com/technetwork/java/javase/downloads/jdk8-downloads-2133151.html)
