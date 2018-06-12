from urllib.request import urlopen
from bs4 import BeautifulSoup

class XmlParser:
  def __init__(self):
    self._baseUrl = "https://synoniemen.net/index.php?zoekterm="
    self._woordenboekSynonymsUrl = "http://www.mijnwoordenboek.nl/synoniem.php?lang=NL&woord="
    self._woordenboekAntonymsUrl = "http://www.mijnwoordenboek.nl/antoniem.php?lang=NL&woord="
    
  def GetSynonyms(self, word):
    zoekTerm = word
    htmlFile = urlopen(self._baseUrl+zoekTerm)
    content = htmlFile.read()
    htmlFile.close()
    soup = BeautifulSoup(content, "lxml")
    trefwoordTabel = soup.find("dl", attrs={'class':'alstrefwoordtabel'})
    allSynonyms = []
    allSynonyms.extend(self.AlternativeApproach(list(trefwoordTabel.children)[1]))
    for synonym in allSynonyms:
      try:
        print(synonym)
      except:
        print('---')
    return allSynonyms

  def GetBasicSynonyms(self, xmlPart):
    synonyms = []
    if hasattr(xmlPart, 'children'):
            for mini in xmlPart.children:
              if mini.name == 'dt':
                for z in mini.children:
                  if z.name == 'strong':
                    #print(z.string)
                    break
              elif mini.name == 'dd':
                for synonym in mini.children:
                  if synonym.name == 'a':
                    print(synonym.string)
                    synonyms.append(synonym)
                break
    return synonyms

  def AlternativeApproach(self, xmlPart):
    synonyms = []
    if (hasattr(xmlPart, 'children')):
      currentChild = list(xmlPart.children)[0]
      while(currentChild != "als synoniem van een ander trefwoord:"):
        if currentChild.name == 'a':
          synonyms.append(currentChild.text) 
        
        currentChild = currentChild.next_element
    return synonyms

  def WoordenboekGetAntonyms(self, word):
    antonyms = []
    zoekTerm = word
    htmlFile = urlopen(self._woordenboekAntonymsUrl+zoekTerm)
    content = htmlFile.read()
    htmlFile.close()
    soup = BeautifulSoup(content, "lxml")
    tabel = soup.find("ul", attrs={'class':'icons-ul'})
    if tabel != None:
      for item in tabel:
        if hasattr(item, 'children'):
          for child in list(item.children):
            if child.name == 'a':
              print(child.text)
              antonyms.append(child.text)
              itsSynonyms = self.WoordenboekGetSynonyms(child.text)
              antonyms.extend(itsSynonyms)
    return antonyms

  def WoordenboekGetSynonyms(self, word):
    synonyms = []
    zoekTerm = word
    htmlFile = urlopen(self._woordenboekSynonymsUrl+zoekTerm)
    content = htmlFile.read()
    htmlFile.close()
    soup = BeautifulSoup(content, "lxml")
    tabel = soup.find("ul", attrs={'class':'icons-ul'})
    try:
      for item in tabel:
        if hasattr(item, 'children'):
          for child in list(item.children):
            if child.name == 'a':
              print(child.text)  
              synonyms.append(child.text)
    except: return []
    return synonyms
    




# x = XmlParser()
# x.GetSynonyms('beoordelen')
# x.WoordenboekGetAntonyms('goedkeuren')
# x.WoordenboekGetSynonyms('goedkeuren')