from urllib.request import urlopen
from bs4 import BeautifulSoup
baseUrl = "https://synoniemen.net/index.php?zoekterm="
zoekTerm = "beoordelen"
htmlFile = urlopen(baseUrl+zoekTerm)
content = htmlFile.read()
htmlFile.close()
soup = BeautifulSoup(content, "lxml")
trefwoordTabel = soup.find("dl", attrs={'class':'alstrefwoordtabel'})
child  = list(trefwoordTabel.children)[1]
if hasattr(child, 'children'):
            for mini in child.children:
              if mini.name == 'dt':
                for z in mini.children:
                  if z.name == 'strong':
                    print(z.string)
                    break
              elif mini.name == 'dd':
                for synonym in mini.children:
                  if synonym.name == 'a':
                    print(synonym.string)