# Le portail

Le portail mène du village au donjon. C'est un disque de gemmes vertes d'environ 3 m de diamètre, posé sur un petit socle de pierre près de Nyxessa.

## Ouverture et fermeture {décidé}

- **Ouverture** : Nyxessa envoie une charge d'énergie en arc jusqu'au portail. Le portail ne s'ouvre qu'à l'arrivée de la charge.
- **Fermeture** : le portail se referme et l'énergie repart vers Nyxessa.

## Jour et nuit {décidé}

- **Le jour**, le portail est **présent** : Nyxessa l'ouvre avec sa charge au lever du jour, et on peut passer au donjon.
- **La nuit**, le portail est **absent** : il se referme à la tombée de la nuit, l'énergie retourne à Nyxessa, et seul le socle de pierre reste sur la place (le socle reste, confirmé).
- Durées : 120 s de jour, 120 s de nuit, 5 s de transition entre les deux. Voir [Déroulé d'une partie](deroule.md).
- Un joueur resté au donjon à la tombée de la nuit est rappelé par Nyxessa. Voir [Déroulé d'une partie](deroule.md).

## Passage {décidé}

- **Touche Interagir** (décision de Quentin du 26/09/2026) : on passe un portail, à l'aller comme au retour, avec la touche Interagir (E, X ou Carré), à environ 3 m du portail au plus. On n'entre plus en marchant dedans. L'invite s'affiche au centre de l'écran : « Entrer dans le donjon » au portail du village, « Revenir au village » au portail de retour.
- Pendant le passage, le joueur ne peut plus bouger : son corps part en gemmes vers le centre du portail, puis se reforme de l'autre côté.
- Quand un joueur **entre** dans le portail, des anneaux partent du centre du disque comme une goutte tombée dans l'eau.
- Quand un joueur **sort** d'un portail, les anneaux font le chemin inverse, du bord vers le centre.
- Chaque passage fait réagir Nyxessa.

## Portail de retour {décidé}

Le portail qui ramène du donjon au village n'est pas alimenté par Nyxessa : il s'ouvre et se ferme sans charge.

## Dans le jeu (26/09/2026)

- **Entrée** : de jour, près du portail du village, un joueur appuie sur Interagir (« Entrer dans le donjon »). Son corps part en gemmes vers le centre du portail, puis les gemmes jaillissent du portail de retour du donjon et le reforment sur la dalle d'arrivée, juste à côté.
- **Retour** : près du portail de retour du donjon, il appuie sur Interagir (« Revenir au village »). Il réapparaît 5 m devant le portail du village, du côté de Nyxessa, et les gemmes jaillissent du portail. L'or qu'il porte est versé à la caisse commune.
- **Portail de retour** {décidé} : c'est le même disque de gemmes vertes que celui du village (les portails sont verts partout, énergie de Nyxessa ; Quentin, 26/09/2026), sur son socle de pierre, contre le mur sud de la salle d'arrivée. Il est toujours ouvert et bourdonne comme celui du village. Il remplace l'anneau de bronze de la version 0.5, qui ne ressemblait pas à un portail.
- **Visibilité** : les autres joueurs voient le passage : dissolution vers le portail de départ, reconstitution depuis le portail d'arrivée, avec les anneaux sur les deux portails.
- **Portail fermé** : la nuit, le portail est fermé et on ne peut pas passer.
## Animations d'entrée et de sortie {décidé} (26/09/2026)

- Le passage joue deux clips KayKit du rig Medium (pack Character Animations 1.1, catalogue dans [Animations](animations.md)) : **« Apparition, sortie du sol »** (`Spawn_Ground`) et **« Apparition, chute du ciel »** (`Spawn_Air`), tous deux d'une fois, 1,30 s.
- **Attribution** (réversible) : **arrivée au donjon** → `Spawn_Air`, le héros tombe du plafond dans la salle d'arrivée ; **retour au village** → `Spawn_Ground`, le héros sort du sol devant le portail. Le rappel au crépuscule par Nyxessa suit la même règle : arrivée au village avec `Spawn_Ground`.
- **Déroulé, sans contrôle du début à la fin** : l'effet de départ en gemmes (PortalTransit.Depart, onde du portail, son) se joue en entier, le héros immobile ; la téléportation n'a lieu qu'une fois cet effet fini. À l'arrivée, le corps se reforme en gemmes (PortalTransit.Arrive), puis le clip d'arrivée correspondant se joue en entier, dès sa première image (le corps réapparaît en l'air au tout début de `Spawn_Air`) ; le contrôle ne revient qu'à la fin du clip.
- **Visibilité** : les autres postes voient le même clip sur la marionnette du joueur, par le NetworkAnimator (comme les emotes de la roue).
- **Contrôleurs** : sous-machine « Portail » commune aux 5 classes jouables (`Assets/Editor/Jeu/EmotesBuilder.cs`, `AjouterPortailArrivee`, même modèle que la sous-machine « Emotes »), déclencheur `PortailArrivee` et booléen `PortailAir`.
