# Ennemis

Les ennemis sont des **squelettes**, issus de la force de Nyxessa, qui viennent la récupérer (voir [L'univers](univers.md)). Ils sortent de terre dans les trois clairières de la forêt, au nord, au sud-est et au sud-ouest, puis marchent sur le village et sur Nyxessa.

## Apparition {effet validé}

Un squelette **sort de terre** : il remonte du sol avec une gerbe de mottes de terre. Voir [Le village](village.md) pour la position des clairières.

## Mort {effet validé}

Un squelette vaincu se **désintègre** en gemmes couleur os qui montent et s'éteignent. La même désintégration touche les squelettes encore debout quand la nuit se termine, à l'aube.

## Types de squelettes {à confirmer}

Les modèles disponibles dans le pack KayKit Skeletons :

| Modèle | Allure | Rôle possible |
|---|---|---|
| Guerrier | Heaume à cornes, arme de mêlée | {à confirmer} |
| Mage | Chapeau pointu, bâton | {à confirmer} |
| Voleur | Capuche, lames | {à confirmer} |
| Sbire | Sans casque, le plus simple | {à confirmer} |

Ordre d'apparition dans la partie : sbires dès la nuit 1, guerriers nuit 2, voleurs nuit 3, mages et premier élite nuit 5, mages lanceurs de crâne nuit 6, Golem (mini-boss) nuit 10, Nécromancien (boss final) nuit 12. Voir [Déroulé d'une partie](deroule.md) {décidé}.

{dev} Le casque du squelette guerrier sert aussi de modèle au heaume du rugissement du viking.

## Boss {décidé}

| Nuit | Boss | Allure |
|---|---|---|
| 10 | **Golem**, mini-boss | Grand squelette massif, hache géante |
| 12 | **Nyxar, le Nécromancien**, boss final, ancien possesseur de Nyxessa (voir [L'univers](univers.md)) | Couronne à crâne, robe violette, grimoire, grande faux et faucille, **yeux verts** qui brillent de la force de Nyxessa |

Comportements, points de vie et attaques des boss : {à confirmer}.

## Mage lanceur de crâne

- Le mage squelette tire un **missile en forme de crâne** fait de gemmes {effet validé}. {{dev: Il s'appelait « nécromancien » avant que ce nom ne soit réservé au boss final.}}
- C'est le même missile que celui de Nyxessa, à taille normale ; celui de Nyxessa est une fois et demie plus gros {décidé}.
- Modèle : le squelette mage KayKit {décidé}. Sa vie et son comportement sont {à confirmer}.

## Comportement et détection {à confirmer}

- Cible prioritaire : Nyxessa, les joueurs, ou le plus proche.
- Détection des joueurs, en particulier de l'assassin en mode furtif, qui n'est repéré que de près.
- Vie, vitesse, dégâts et or rapporté par type. Les calculs de Nyxessa supposent un sbire à 100 points de vie {à équilibrer}.
