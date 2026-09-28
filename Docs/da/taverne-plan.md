# Taverne : plan de circulation intérieur

Demande de Quentin du 28/09/2026 : « il faut imaginer le plan de circulation à l'intérieur pour correctement évaluer l'empreinte extérieure ». La taverne est le bâtiment le plus chargé du village ; les autres maisons se dimensionneront de la même façon. **L'intérieur dicte l'emprise** : intérieur et extérieur ont les mêmes cotes, à l'épaisseur des murs près.

## Les échelles, d'abord

Rappel de Quentin : respecter l'échelle des personnages. Tout part de là.

| Repère | Valeur | Source |
|---|---|---|
| Personnage | 2,3 m de haut, la tête fait 46 % de la hauteur, environ 1 m de large | `Docs/style-kaykit.md` § 2.1 |
| Porte d'une maison | 1,8 × 2,6 m au clair | décision du 26/09/2026 |
| Caméra à l'épaule | 5,5 m derrière le héros, tangage 22° : l'œil est à 5,1 m en arrière et 3,7 m de haut | `GameBalance` (`cameraDistance`, `cameraTangageDefaut`, `cameraHauteur`) |
| Deux personnages qui se croisent | 2,2 m | 2 × 1 m + marge |

Conséquences pour tout intérieur :

- **Hauteur libre d'au moins 4,5 m** là où les joueurs marchent (la caméra est à 3,7 m) : la salle de la taverne est à double hauteur, 5 m sous les poutres.
- **5 à 6 m de recul libre** derrière un joueur tourné vers ce qu'il regarde (comptoir, estrade) : sinon la caméra se colle à son dos.
- **Allée principale de 3 m**, allées secondaires de 1,5 m au moins.
- **Mobilier à l'échelle des personnages** (grosses têtes, corps trapus) : plateau de table à 1,05 m, tabouret 0,6 m, comptoir 1,3 m, tonneau en perce 1,2 m de diamètre.

Les images de Grok dessinent des personnages d'environ 1,2 m dans un mobilier d'adultes : l'ambiance est bonne, les proportions sont à reprendre sur ce plan.

## Le plan

**Version 2, validée par Quentin le 28/09/2026** sur le volume gris construit dans `sandbox-level` (`TaverneVolumeBuilder`, scène `Assets/Scenes/TaverneVolume.unity`), d'après l'image cible `Docs/references/taverne-interieur-grok-cible.webp`. La version 1 (galerie, escalier, grande table à bancs) est abandonnée : poteaux, dessous de galerie et escalier gênaient la caméra.

Intérieur **16 × 11 m** (176 m²), **une seule grande salle**, porte au sud, face à Nyxessa. Origine au coin sud-ouest, x vers l'est, y vers le nord.

| Élément | Emprise (m) | Cotes | Note |
|---|---|---|---|
| Porte double | x 6,4 à 9,6 sur le mur sud | 3,2 × 2,8 m | perron de pierre |
| Allée principale | x 6,5 à 9,5, de la porte au comptoir | 3 m de large | jamais de meuble dedans |
| Comptoir | x 6,4 à 12,4 ; y 8,6 à 9,5 | 6 × 0,9 m, 1,3 m de haut | quatre joueurs de front ; la Bavaroise au milieu derrière |
| Service | derrière le comptoir | 1,5 m de passage | libre, accessible par les deux bouts |
| Râtelier | coin nord-est, contre le mur est (x 14,9 à 16 ; y 8,3 à 11) | six tonneaux Ø 1,2 m sur trois rangs | 4,2 m de haut |
| Sol nu devant le comptoir | x 6,4 à 12,4 ; y 3,6 à 8,6 | 6 × 5 m au moins (8,4 × 5,9 m mesurés) | recul de caméra assuré |
| Estrade | coin nord-ouest, x 0,4 à 5 ; y 8 à 11 | pan coupé, 0,4 m de haut, une marche | le barde |
| Âtre | mur ouest, y 4,5 à 7,5 | 3 m de large, saillie 1,4 m | feu vivant (`ForgeFeu`), conduit de pierre |
| Table du clochard | centre (2,4 ; 2,4) | Ø 1,6 m | près du feu, le clochard assis dos au mur ouest |
| Tables rondes | quart sud-est | Ø 1,6 m, quatre tabourets | **trois tables**, disposées régulièrement (demande de Quentin) |
| Fenêtres | deux au sud (x 3 et 13), deux à l'ouest, une à l'est | 1,4 × 1,6 m | |

Mesures relevées : 5,2 m libres sous entraits ; la caméra à l'épaule garde tout son recul dans 62 % des cas (74 % dans le centre vide) ; dos à un mur à 1 m, elle se colle au joueur (la salle fait 11 m de profondeur) ; le linteau de la porte la gêne entre 2,5 et 5 m après le seuil. À voir à l'étape 4 : le mobilier est haut pour un personnage assis (plateau à 1,05 m, jambes courtes).

## Qui est là

- **La tavernière : la Bavaroise** (candidate du mois, `classe-bavaroise.md`), derrière le comptoir. Elle remplace le tavernier actuel ; le menu ne change pas (repas, bière, tournée).
- **Le barde** (`classe-barde.md`), sur l'estrade : il joue du luth, **musique festive** en boucle dans la taverne (spatialisée sur l'estrade, portée courte pour ne pas couvrir le village).
- **Le clochard pétomane** (`classe-clochard.md`), client récurrent : à sa table près de l'âtre, bouteille en main.
- **Les quatre joueurs**, avec de la place autour d'eux.

Ces trois personnages restent des candidats jouables ; à la taverne ils sont des villageois (pas de combat, animations d'attente et quelques gestes).

## L'empreinte extérieure qui en découle

| | Taverne | Maison standard |
|---|---|---|
| Murs (nu de l'enduit) | **16,6 × 11,6 m** | 7,6 × 6 m |
| Sablière haute | 5,4 m (salle à double hauteur) | 4,4 m |
| Toit | 40° à demi-croupes, faîtage à 10,8 m | 45°, faîtage vers 8 m |
| Porte | double, 3,2 × 2,8 m | 1,8 × 2,6 m |

La taverne est donc **le grand bâtiment du village** : deux fois la largeur d'une maison. Sur le plan cible (plan n° 3), elle prend la place de deux maisons voisines ou s'installe en retrait ; à régler quand les cinq autres intérieurs seront dimensionnés (forge, boutique du mécano, boutique du druide, maison du sorcier, maison de décor fermée).

## Reste à trancher

- Clients anonymes en plus (deux ou trois villageois aux tables) ?
- La taverne la nuit : ouverte, ou fermée comme les boutiques (voir « Maisons la nuit » dans À décider) ?
