# Ennemis

Les ennemis sont des **squelettes**, issus de la force de Nyxessa, qui viennent la récupérer (voir [L'univers](univers.md)). Ils sortent de terre dans les trois clairières de la forêt, au nord, au sud-est et au sud-ouest, puis marchent sur le village et sur Nyxessa.

## Apparition {effet validé}

Un squelette **sort de terre** : il remonte du sol avec une gerbe de mottes de terre. Voir [Le village](village.md) pour la position des clairières.

## Mort {effet validé}

Un squelette vaincu se **désintègre** en gemmes couleur os qui montent et s'éteignent. La même désintégration touche les squelettes encore debout quand la nuit se termine, à l'aube.

## Types de squelettes {décidé}

{dev} Les modèles viennent du pack KayKit Skeletons.

| Modèle | Allure | Rôle |
|---|---|---|
| Guerrier | Heaume à cornes, arme de mêlée | Lent et solide, frappe fort, bloque les joueurs |
| Mage | Chapeau pointu, bâton | Reste à distance et tire le missile crâne |
| Voleur | Capuche, lames | Rapide, contourne et vise les joueurs isolés |
| Sbire | Sans casque, le plus simple | Nombreux et fragiles, foncent sur Nyxessa |

Ordre d'apparition dans la partie : sbires dès la nuit 1, guerriers nuit 2, voleurs nuit 3, mages (lanceurs de crâne) et premier élite nuit 5, mages plus nombreux nuit 6, Morgrim, le Roi des os (mini-boss) nuit 10, Nécromancien (boss final) nuit 12. Voir [Déroulé d'une partie](deroule.md) {décidé}.

{dev} Le casque du squelette guerrier sert aussi de modèle au heaume du rugissement du viking.

## Élites {décidé}

Un élite est un squelette ordinaire qui porte un peu de la force de Nyxessa :

- environ **1,3 fois plus grand** ;
- **trois fois plus de points de vie** et des dégâts plus forts ;
- **yeux verts** et légère **aura de gemmes vertes**.

Valeurs exactes : {à équilibrer}.

## Boss {décidé}

| Nuit | Boss | Allure |
|---|---|---|
| 10 | **Morgrim, le Roi des os**, mini-boss (le Golem) | Grand squelette massif, hache géante |
| 12 | **Nyxar, le Nécromancien**, boss final, ancien possesseur de Nyxessa (voir [L'univers](univers.md)) | Couronne à crâne, robe violette, grimoire, grande faux et faucille, **yeux verts** qui brillent de la force de Nyxessa |

### Morgrim, le Roi des os {décidé}

Colosse très résistant et lent, il marche droit sur Nyxessa. Chaque attaque se prépare longtemps, pour laisser le temps de parer ou d'esquiver :

- **Balayage** de hache en arc devant lui ;
- **Coup écrasé** au sol, qui fait une onde de choc autour de lui ;
- **Cri** qui renforce les squelettes proches.

Points de vie et dégâts : {à équilibrer}.

### Nyxar, le Nécromancien {décidé}

Invocateur qui combat à distance :

- il **garde ses distances** et **se téléporte** quand on l'approche ;
- il tire des **salves de crânes** ;
- il **relève des squelettes** du sol autour de lui ;
- il **fauche à la faux** ceux qui le serrent de près ;
- son **fragment de Nyxessa brille** : c'est son **point faible**.

Points de vie, dégâts et cadence : {à équilibrer}.

## Mage

- Le mage squelette tire un **missile en forme de crâne** fait de gemmes {effet validé}. {{dev: Il s'appelait « nécromancien » avant que ce nom ne soit réservé au boss final.}}
- C'est le même missile que celui de Nyxessa, à taille normale ; celui de Nyxessa est une fois et demie plus gros {décidé}.
- Modèle : le squelette mage KayKit {décidé}. Sa vie et son comportement sont {à confirmer}.

## Comportement et détection

- **Cible prioritaire** {décidé} : les squelettes marchent vers Nyxessa. Un joueur qui les frappe, ou qui passe à moins de 4 m, devient leur cible pendant quelques secondes, puis ils reprennent leur route. Le voleur fait exception : il chasse les joueurs isolés. Distance et durée {à équilibrer}.
- **Détection de l'assassin furtif** {décidé} : cône de vue d'environ 6 m devant le squelette, 1,5 m dans son dos. Voir [Classes](classes.md).
- Vie, vitesse, dégâts et or rapporté par type. Les calculs de Nyxessa supposent un sbire à 100 points de vie {à équilibrer}.
