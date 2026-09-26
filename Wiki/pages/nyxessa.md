# Nyxessa, la relique

{icone-grande nyxessa}

Nyxessa est une force mystérieuse, celle dont sont issus les squelettes et qu'utilisent les héros (voir [L'univers](univers.md)). Au centre du village, elle prend la forme d'une gemme verte à facettes qui flotte et tourne au-dessus d'un rocher, entourée d'une ceinture de petites gemmes en orbite.

## Vie

- **2 000 points de vie**, sans régénération à l'aube {à équilibrer}. Quand elle est détruite, la partie est perdue.

## Réactions {décidé}

Nyxessa réagit visiblement à ce qui se passe autour d'elle.

| Événement | Réaction |
|---|---|
| Ouverture du portail | La ceinture s'élargit et tourne plus vite, un éclat de lumière, puis Nyxessa envoie une charge au portail. |
| Fermeture du portail | L'énergie revient du portail vers Nyxessa, puis la ceinture se resserre, ralentit, et la lumière baisse. |
| Tir d'un missile magique | Le cristal pulse et recule légèrement, un éclat part au point de tir. |
| Passage d'un joueur dans le portail | Une onde fait le tour de la ceinture. |

## Missiles magiques {décidé}

Nyxessa tire des missiles en forme de crâne faits de gemmes vertes, une fois et demie plus gros que le missile du mage squelette.

- **Stock** : Nyxessa dispose d'un stock de N missiles. N dépend du **palier d'amélioration** de Nyxessa.
- **Deux temps de recharge** :
  - **Régénération** : le temps pour récupérer un missile dans le stock.
  - **Intervalle de tir** : le temps minimum entre deux tirs.
- **Dégâts** : ils dépendent aussi du palier d'amélioration, comme les deux temps de recharge.
- **Décision de tir** : Nyxessa choisit elle-même quand tirer et combien de missiles envoyer, selon le stock disponible.
- **Compteur dans le HUD** {décidé} : en haut, à droite de la barre de Nyxessa, une icône de crâne vert et le stock (« 3 / 5 »). L'icône se remplit de bas en haut pendant la régénération du prochain missile ; elle est entièrement allumée quand le stock est plein. Un missile gagné la fait grossir un instant, un missile tiré fait un bref éclat vert. Voir [Interface](interface.md).

### Paliers {à équilibrer}

Les paliers de Nyxessa améliorent ses missiles et la part de butin qu'elle sauve quand elle rappelle un joueur resté au donjon (voir [Déroulé d'une partie](deroule.md)).

Cinq paliers, soit environ un palier toutes les deux nuits sur une partie de 45 minutes. Les dégâts supposent un squelette sbire à 100 points de vie.

| Palier | Stock N | Régénération d'un missile | Intervalle de tir | Dégâts | Missiles par nuit | Dégâts par nuit |
|---|---|---|---|---|---|---|
| 1 | 2 | 12 s | 1,5 s | 40 | 12 | 480 |
| 2 | 3 | 10 s | 1,2 s | 55 | 15 | 825 |
| 3 | 4 | 8 s | 1,0 s | 75 | 19 | 1 425 |
| 4 | 6 | 6,5 s | 0,8 s | 100 | 24 | 2 400 |
| 5 | 8 | 5 s | 0,6 s | 130 | 32 | 4 160 |

- Au palier 1, il faut trois missiles pour un sbire : Nyxessa aide, mais ne tient pas seule.
- Au palier 5, un missile tue un sbire, et le stock permet des salves.
- **Coût** des paliers 2 à 5 : ×1, ×2, ×3,5 et ×5,5 d'un coût de base, payé par la caisse commune.
- Coût de base : **100 or**, soit 100, 200, 350 et 550 or pour les paliers 2 à 5 {à équilibrer}.

### Achats à la relique {décidé}

De **jour**, sur le dessus du plateau de Nyxessa, la touche **Interagir** (E, X, Carré) ouvre le **menu d'achat des paliers** : missiles de Nyxessa et bouclier du sorcier. Chaque ligne montre le palier atteint, ce qu'apporte le suivant et son prix ; l'or est pris dans la **caisse commune**. En multijoueur, **l'hôte décide** : un achat demandé par un autre joueur lui est transmis, et tous entendent le palier monter. Le menu se ferme à la tombée de la nuit. {{dev: (`AchatRelique`, `Partie.Acheter`, écran `EcranAchat`)}}

### Règles de tir {décidé}

- **Portée** : 30 m, jusqu'à la lisière de la forêt. Les ennemis sous les arbres sont hors d'atteinte.
- **Priorité des cibles** :
  - d'abord un ennemi qui frappe Nyxessa ou son bouclier ;
  - ensuite un mage lanceur de crâne, un ennemi d'élite ou un boss ;
  - sinon l'ennemi le plus proche de Nyxessa.
- **Salves** :
  - en temps normal, un missile par cible, en gardant un missile en réserve ;
  - contre un groupe de trois ennemis ou plus, ou contre un élite, une salve jusqu'à vider le stock sauf un ;
  - si Nyxessa est frappée, elle vide tout son stock.

## Bouclier

Un bouclier cylindrique de gemmes en lévitation protège Nyxessa {effet validé}. Sa couleur indique sa solidité : bleu quand il est plein, orange quand il est entamé, rouge quand il est près de céder.

### Règles {décidé}

- **Invocation** : c'est un **villageois sorcier** qui invoque le bouclier, **seulement la nuit**. Au crépuscule, il sort de sa maison et vient se placer près de Nyxessa ; à l'aube, il rentre chez lui.
- **Posture du sorcier** {décidé} : il incante **dos à Nyxessa**, face à l'extérieur, à l'intérieur du bouclier. Il peut se tourner **de quelques degrés** vers les ennemis qui arrivent devant lui, mais garde toujours Nyx dans le dos. À sa mort, il **tombe en arrière**, sans que rien ne le gêne.
- **Plateforme** {décidé} : la plateforme de pierre de Nyxessa a le **diamètre du bouclier** ; le cylindre de gemmes repose sur son bord.
- **Valeurs de départ** {à équilibrer} : bouclier de 5,3 m de rayon et 6 m de haut, palier 1 au départ (les suivants s'achètent à la relique) ; incantation de 3 s ; sorcier : 60 points de vie, 3,2 m/s, pivot de ±20°, incantation à 3,6 m du centre de Nyxessa. S'il cède sur un coup, le reste de ce coup passe à Nyxessa. Tant qu'il est levé, il protège aussi le sorcier.
- **Encaissement** : la quantité de dégâts que le bouclier peut absorber est **améliorable**.
- **Riposte** : quand il est frappé, le bouclier **renvoie des dégâts** à l'attaquant.
- **Canalisation** : au départ, le sorcier tient le bouclier par sa seule incantation. Une **amélioration** du sorcier lui apprend à **canaliser l'énergie de Nyxessa** : un lien d'énergie apparaît entre la relique et son bâton, et il puise dans sa force {décidé}. Elle s'obtient au **palier 4** du bouclier {décidé}.
  - **Effet** {décidé} : le bouclier devient **plus puissant encore**. Le sorcier puise dans l'**énergie** de Nyxessa, **jamais dans sa vie** : la relique ne perd rien.
  - **Signe visuel** {décidé} : un lien d'énergie se forme entre le **bâton levé** du sorcier et Nyxessa, tant qu'il canalise.
  - Gain de puissance : {à équilibrer}.

### À décider

- **Signe visuel de l'amélioration** {décidé} : chaque palier rend le bouclier **plus dense** : davantage de gemmes dans le mur et davantage de gemmes en lévitation autour. Un bouclier qui encaisse plus paraît plus épais.
- **Paliers** {décidé} : 5 paliers, achetés **à la relique**, comme ses missiles, aux mêmes coûts (100, 200, 350 et 550 or pour les paliers 2 à 5) {à équilibrer}.
- **Durée** {décidé} : le sorcier invoque le bouclier au début de la nuit ; il tient **jusqu'à être brisé**.
- **Bouclier brisé** {décidé} : le sorcier **meurt**. Comme un héros, il se dissout et son énergie retourne à Nyxessa {{dev: (même effet que la mort d'un allié, `MortAllie`)}}. Plus de bouclier jusqu'à la nuit suivante ; le sorcier **réapparaît le jour suivant**.
- **Valeurs par palier** {à équilibrer} :

| Palier | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Encaissement | 150 | 260 | 370 | 480 | 600 |
| Dégâts renvoyés par coup | 5 | 9 | 12 | 16 | 20 |
