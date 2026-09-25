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

L'écran rappelle aussi le résultat de l'équipe : victoire ou nuit atteinte, durée de la partie, or total.

- **Défaite** {décidé} : la partie est perdue quand **Nyxessa est détruite**. Tant qu'elle tient, les joueurs morts réapparaissent.

## Mort et réapparition {décidé}

- **Mort** : tout joueur ou villageois du camp des héros qui meurt **se dissout**, comme lors d'une téléportation, et **son énergie retourne à Nyxessa**.
- **Villageois** : ils réapparaissent au **jour suivant**.
- **Joueurs** : ils réapparaissent près de Nyxessa au bout de N secondes. **Chaque mort allonge ce délai.**
- **Dans tous les cas**, un joueur mort revient **au début de la nouvelle journée**, même si son délai n'est pas écoulé.
- Valeurs de départ : 8 s pour la première mort, +4 s à chaque mort suivante {à équilibrer}. Le compteur de morts se remet à zéro chaque partie ou chaque jour : {à confirmer}.
- Ce qu'on garde d'une partie à l'autre : {à confirmer}.

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
| 6 | 3 | 30 | Mages lanceurs de crâne |
| 7 | 3 | 34 | Deux élites par nuit |
| 8 | 3 | 38 | |
| 9 | 3 | 42 | Quatre vagues, points de vie +10 % |
| 10 | 3 | 44 | Mini-boss : **Morgrim, le Roi des os** |
| 11 | 3 | 46 | Points de vie +20 % |
| 12 | 3 | 48 | Boss final : **Nyxar, le Nécromancien**, victoire à l'aube |

- **Nuits 1 à 3** : apprentissage. **Nuits 4 à 7** : pression, pendant qu'on monte les paliers de Nyxessa (un palier toutes les deux nuits environ). **À partir de la nuit 8** : difficile.

### Règles universelles {décidé}

- **Joueurs en plus** : chaque joueur supplémentaire ajoute 60 % d'ennemis {à équilibrer}.
- **Plafond** : 60 squelettes en même temps sur le terrain. Au-delà, on n'en ajoute plus et on augmente leurs points de vie à la place, pour la lisibilité et les performances.

## À décider

