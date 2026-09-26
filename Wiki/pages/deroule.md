# Déroulé d'une partie

Une partie enchaîne des cycles de jour et de nuit autour de Nyxessa.

## Die and retry {décidé}

Deathless est un jeu de type **die and retry** : on recommence après une défaite. Une partie dure **environ 45 minutes en moyenne**, soit une dizaine de cycles.

## Fin de partie {décidé}

À la défaite comme à la victoire, un **écran de score** s'affiche. Chaque joueur choisit ensuite :

- **Rejouer** : c'est un vote « prêt ». Quand tous les joueurs restants sont prêts, une nouvelle partie commence.
- **Arrêter** : retour au menu principal.

### Écran de score : une compétition {décidé}

L'écran de score met les joueurs en compétition. Pour chaque catégorie, le meilleur joueur est mis en avant.

| Catégorie | Meilleur quand | Statut |
|---|---|---|
| Or rapporté | le plus élevé | {décidé} |
| Dégâts infligés | le plus élevé | {décidé} |
| Ennemis tués | le plus élevé | {décidé} |
| Nombre de morts | le plus bas | {décidé} |
| Coups critiques | le plus élevé | {décidé} |
| Dégâts évités à Nyxessa | le plus élevé | {décidé} |
| Soins prodigués | le plus élevé | {décidé} |

- **Dégâts évités à Nyxessa** {à équilibrer} : tuer un squelette qui frappait Nyxessa compte 10 s de ses coups ; l'étourdir compte ses coups pendant la durée de l'étourdissement.

L'écran rappelle aussi le résultat de l'équipe : victoire ou nuit atteinte, durée de la partie, or total.

- **Défaite** {décidé} : la partie est perdue quand **Nyxessa est détruite**. Tant qu'elle tient, les joueurs morts réapparaissent.

## Mort et réapparition {décidé}

- **Mort** : tout joueur ou villageois du camp des héros qui meurt **se dissout**, comme lors d'une téléportation, et **son énergie retourne à Nyxessa**.
- **Villageois** : ils réapparaissent au **jour suivant**.
- **Joueurs** : ils réapparaissent près de Nyxessa au bout de N secondes. **Chaque mort allonge ce délai.**
- **Dans tous les cas**, un joueur mort revient **au début de la nouvelle journée**, même si son délai n'est pas écoulé.
- Valeurs de départ : 8 s pour la première mort, +4 s à chaque mort suivante {à équilibrer}. Le compteur de morts se remet à zéro à chaque partie {décidé}.
- **Entre deux parties**, on ne garde **rien** : chaque partie repart de zéro {décidé}.

## Victoire {décidé}

La partie est **gagnée en survivant à la nuit 12** et à son boss final, Nyxar, le Nécromancien, à l'aube. Une partie gagnée dure donc 12 cycles, soit environ 50 minutes. La plupart des parties perdues s'arrêtent entre la nuit 8 et la nuit 11.

## Le cycle {décidé}

| Phase | Durée | Ce qui se passe |
|---|---|---|
| Jour | 120 s | Le portail est présent : on peut passer au donjon. |
| Crépuscule | 5 s | Transition vers la nuit : le portail se referme et disparaît, l'énergie retourne à Nyxessa. |
| Nuit | 120 s | Le portail est absent, seul son socle reste. |
| Aube | 5 s | Transition vers le jour : Nyxessa envoie sa charge et le portail réapparaît. |

Un cycle complet dure **4 min 10 s**. Les 5 s de crépuscule et d'aube laissent le temps aux animations de se lire.

## Le donjon {décidé}

Le donjon est **régénéré chaque nuit** : chaque jour, le portail mène à un donjon nouveau.

**Taille unique** {décidé} : tous les donjons ont la même taille. Elle respecte une consigne : un aller-retour complet, de l'arrivée par le portail au retour au village, est **faisable en 90 secondes** (le jour dure 120 s). **Forme** {décidé} : plutôt **ouvert**, sur **2 à 3 étages** reliés par des escaliers. Quand on entre dans une pièce sous un étage, **l'étage du dessus disparaît** pour qu'on voie bien dedans (chaque joueur pour lui-même). Peu de couloirs : de **grandes salles ouvertes et lisibles**, pas un labyrinthe. Des balcons et mezzanines longent les murs ; **3 escaliers au plus**, en pierre ou en bois. Un **vrai 2e étage** plein, relié par l'un de ces escaliers. **Consigne des 90 s** : le chemin le plus long (arrivée, butin le plus lointain, portail de retour, escaliers et eau compris) fait au plus 250 m.

**Eau** {décidé} : un demi-niveau peut être rempli d'**eau jusqu'aux genoux**, bordé de murets, où l'on descend par un escalier. **L'eau ralentit les déplacements** (héros comme squelettes). Eau bleue, jamais verte. Ralentissement {à équilibrer}.

**Pas de faux butin** {décidé} : dans le décor, rien ne ressemble à du butin (tas d'or, pièces, coffres, sacs) s'il ne rapporte rien. Seul le vrai butin en a l'allure, pour ne pas tromper le jugement des joueurs. Mesure exacte et contenu du donjon : {à confirmer}.

**Butin** {décidé} : on ne rapporte du donjon que de l'**or**, versé à la caisse commune au retour par le portail.

**Entrer et sortir par la touche Interagir** {décidé} (décision de Quentin du 26/09/2026) : on passe les portails, à l'aller comme au retour, avec la touche Interagir (E, X ou Carré), près du portail (environ 3 m). On n'entre plus en marchant dedans. Voir [Le portail](portail.md).

**Dans le jeu (26/09/2026)** :
- **Le donjon du jour** : au lever du jour, l'hôte tire une graine et construit le donjon ; les autres joueurs reçoivent la graine et construisent le même. Le donjon est dans la même scène que le village, loin de lui.
- **Portails** : de jour, près du portail du village, l'invite « Entrer dans le donjon » s'affiche ; la touche Interagir fait passer. On arrive sur la dalle d'arrivée du donjon. À côté se trouve le portail de retour, le même disque de gemmes vertes que celui du village, toujours ouvert. Près de lui, « Revenir au village » ramène devant le portail du village, du côté de Nyxessa. Le passage se fait avec l'effet de téléportation (le corps part en gemmes vers le portail de départ, puis les gemmes jaillissent du portail d'arrivée et le reforment), vu par tous les joueurs. Pendant le passage, le joueur ne peut plus bouger.
- **Emplacements de butin** : 7 emplacements, avec les montants suivants.

  | Butin | Nombre | Or | Comment on le prend |
  |---|---|---|---|
  | Grand coffre | 1 | 120 | Touche Interagir. Il est sur le 2e étage. |
  | Coffre | 2 | 50 | Touche Interagir. |
  | Tas d'or | 4 | 20 | On passe dessus. |

  **Ouvrir un coffre est gratuit** {décidé} : on ne dépense jamais d'or pour l'ouvrir. Plus tard, certains coffres pourront demander une **clé** {à confirmer}. L'invite dit seulement « Ouvrir le coffre », sans montant.

  **Pas de cadenas** {décidé} (décision de Quentin du 26/09/2026) : plus aucun cadenas sur les coffres. Pour l'instant, tous les coffres du donjon s'ouvrent sans clé : à la touche Interagir, le couvercle bascule directement. Les coffres sont des modèles sans serrure (`Assets/Art/Coffres`).

  Un butin n'est pris qu'une fois : c'est l'hôte qui décide. Les montants augmentent de 10 % par nuit déjà passée. Tous les montants sont {à équilibrer}.
- **Or porté** : l'or pris est **porté** par le joueur. Le HUD l'affiche sous la caisse commune (« or porté · au donjon »). Il est versé à la caisse commune au retour par le portail.
- **Gardiens** : 6 squelettes gardent le butin, dont 35 % de guerriers et le reste de sbires {à équilibrer}. Ils apparaissent au lever du jour sur les points d'apparition les plus proches du butin. Ils restent à leur poste et poursuivent les joueurs qui approchent. Ils n'attaquent pas Nyxessa, ne rapportent pas d'or et disparaissent au crépuscule.
- **Ambiance** : pour le joueur au donjon, le lieu est sombre, éclairé par les torches, sans ciel.
- **Eau** : ralentissement ×0,6, pour les héros comme pour les squelettes {à équilibrer}.
- **Mort au donjon** : un joueur qui meurt au donjon perd l'or qu'il portait, comme un joueur rappelé (Nyxessa en garde la part de son palier). Il réapparaît au village {à confirmer}.

**Or des vagues** {décidé} : chaque squelette tué pendant les vagues rapporte de l'or à la caisse commune (montants dans [Ennemis](ennemis.md)). Cette règle était prévue en attendant le donjon. Elle **reste en place** maintenant que le donjon est arrivé, jusqu'à ce que Quentin décide de la réajuster.

### Rester au donjon à la tombée de la nuit {décidé}

- **Alerte** : avant la fermeture du portail, les joueurs au donjon sont prévenus, à l'écran et par un son. Délai d'alerte : 15 s avant le crépuscule {à équilibrer}.
- **Rappel** : un joueur encore au donjon au crépuscule est **rappelé de force par Nyxessa** avant que le portail se ferme. Il revient au village.
- **Perte de butin** : un joueur rappelé perd le butin qu'il portait depuis le donjon, **en totalité** au départ.
- **Améliorations de Nyxessa** : chaque palier de Nyxessa lui fait **garder une part** de ce butin.

| Palier de Nyxessa | Part du butin gardée |
|---|---|
| 1 | 0 % |
| 2 | 20 % |
| 3 | 40 % |
| 4 | 60 % |
| 5 | 75 % |

Ces parts sont {à équilibrer}. Rentrer par le portail avant la nuit garde toujours 100 % du butin.

Dans le jeu :
- **Alerte** : 15 s avant le crépuscule, un joueur au donjon voit « Le portail se ferme dans N s : rentrez au village ! » et entend Nyxessa. Cette alerte remplace, pour lui, celle de la tombée de la nuit.
- **Rappel** : au crépuscule, Nyxessa le ramène près d'elle, avec l'effet de téléportation. La part gardée va à la caisse commune, et le HUD affiche par exemple « Rappelé par Nyxessa : 88 or gardés, 132 perdus ».

## Vote « prêt » {décidé}

- Pendant le jour, chaque joueur peut se déclarer **prêt**, et annuler son vote.
- Le vote n'est possible que quand **toute l'équipe est rentrée** au village : si un joueur est au donjon, le vote est inactif.
- Quand **tous les joueurs** sont prêts, le jour est écourté : le crépuscule commence après un court compte à rebours de 5 s {à équilibrer}.
- Le HUD affiche le nombre de joueurs prêts, par exemple « Prêts 2 / 3 ».

## Les nuits {décidé}

### Structure d'une nuit

- **Vagues** : trois vagues lancées à 0 s, 40 s et 80 s. À partir de la nuit 9, quatre vagues, à 0, 30, 60 et 90 s.
- **Trajet** : les squelettes mettent environ 20 s pour marcher de leur clairière au village. Chaque vague laisse un temps de combat puis un court répit.
- **Clairières actives** : une au début, deux à partir de la nuit 3, les trois à partir de la nuit 5. Les clairières actives sont annoncées au crépuscule.
- **À l'aube** : les squelettes encore debout se désintègrent {effet validé}. L'enjeu est de tenir jusqu'au jour, pas de tout tuer.

### Montée en difficulté {décidé}

Pour un joueur :

| Nuit | Clairières | Ennemis | Nouveauté |
|---|---|---|---|
| 1 | 1 | 8 | Sbires seulement |
| 2 | 1 | 12 | Guerriers |
| 3 | 2 | 16 | Voleurs |
| 4 | 2 | 20 | |
| 5 | 3 | 25 | Mages, premier élite |
| 6 | 3 | 30 | Mages plus nombreux |
| 7 | 3 | 34 | Deux élites par nuit |
| 8 | 3 | 38 | |
| 9 | 3 | 42 | Quatre vagues, points de vie +10 % |
| 10 | 3 | 44 | Mini-boss : **Morgrim, le Roi des os** |
| 11 | 3 | 46 | Points de vie +20 % |
| 12 | 3 | 48 | Boss final : **Nyxar, le Nécromancien**, victoire à l'aube |

- **Nuits 1 à 3** : apprentissage. **Nuits 4 à 7** : pression, pendant qu'on monte les paliers de Nyxessa (un palier toutes les deux nuits environ). **À partir de la nuit 8** : difficile.

### Règles universelles {décidé}

- **Nombre de joueurs** : **4 au maximum** {décidé}.
- **Joueurs en plus** : chaque joueur supplémentaire ajoute 60 % d'ennemis {à équilibrer}.
- **Plafond** : 60 squelettes en même temps sur le terrain. Au-delà, on n'en ajoute plus et on augmente leurs points de vie à la place, pour la lisibilité et les performances.

## À décider

