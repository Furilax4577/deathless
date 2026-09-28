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

Intérieur **16 × 11 m** (176 m²), porte au sud, face à Nyxessa. Origine au coin sud-ouest, x vers l'est, y vers le nord.

| Élément | Emprise (m) | Cotes | Note |
|---|---|---|---|
| Porte double | x 6,4 à 9,6 sur le mur sud | 3,2 × 2,8 m | deux vantaux ouverts vers l'intérieur, contre le mur |
| Allée principale | x 6,6 à 9,4, de la porte au comptoir | 3 m de large | jamais de meuble dedans |
| Comptoir | x 8,5 à 14,5 ; y 8,6 à 9,5 | 6 × 0,9 m, 1,3 m de haut | quatre joueurs de front (4 × 1,2 m) ; point d'interaction au milieu |
| Service | x 8,5 à 16 ; y 9,5 à 11 | 1,5 m de passage | la Bavaroise, quatre tonneaux en perce contre le mur nord, étagères de chopes |
| Galerie | au-dessus du service et du comptoir | plancher à 3,4 m | décor ; les joueurs ne passent jamais dessous |
| Escalier | x 14,8 à 16 ; y 4 à 8,6 | 1,2 m de large | monte à la galerie ; fermé par une corde en haut (décor) |
| Place des joueurs | x 7 à 14,8 ; y 3,6 à 8,6 | 7,8 × 5 m | sol nu devant le comptoir ; recul de caméra assuré |
| Estrade | x 0,4 à 4,4 ; y 8 à 11 | 4 × 3 m, 0,4 m de haut | le barde ; piste de 3,4 × 3 m devant (x 4,6 à 8) |
| Âtre | x 0 à 1,2 ; y 4 à 7 (mur ouest) | 3 m de large, foyer en saillie | feu vivant (`ForgeFeu`), cheminée massive dehors |
| Grande table | x 3 à 6,6 ; y 4,2 à 6,8 avec ses bancs | table 3,6 × 1,2 m | six places (1,2 m par convive) |
| Banc du clochard | x 1,6 à 3,6 ; y 2,6 à 3,2 | 2 × 0,6 m | entre l'âtre et la fenêtre sud-ouest, dos au mur |
| Tables rondes | centres (11,4 ; 2) et (14,4 ; 2) | Ø 1,6 m, quatre tabourets | le long du mur sud, à l'est de la porte |
| Fenêtres | deux au sud, une par pignon, deux au nord (galerie) | | plus larges que le standard (1,15 m) |

Schéma : dans le fil du 28/09/2026 et sur la page « Direction artistique » du wiki (`taverne-coupe`, `taverne-ambiance`).

## Qui est là

- **La tavernière : la Bavaroise** (candidate du mois, `classe-bavaroise.md`), derrière le comptoir. Elle remplace le tavernier actuel ; le menu ne change pas (repas, bière, tournée).
- **Le barde** (`classe-barde.md`), sur l'estrade : il joue du luth, **musique festive** en boucle dans la taverne (spatialisée sur l'estrade, portée courte pour ne pas couvrir le village).
- **Le clochard pétomane** (`classe-clochard.md`), client récurrent : sur son banc près de l'âtre, bouteille en main.
- **Les quatre joueurs**, avec de la place autour d'eux.

Ces trois personnages restent des candidats jouables ; à la taverne ils sont des villageois (pas de combat, animations d'attente et quelques gestes).

## L'empreinte extérieure qui en découle

| | Taverne | Maison standard |
|---|---|---|
| Murs (nu de l'enduit) | **16,6 × 11,6 m** | 7,6 × 6 m |
| Sablière haute | 5,4 m (salle à double hauteur) | 4,4 m |
| Toit | 45 à 50°, faîtage vers 11 m | 45°, faîtage vers 8 m |
| Porte | double, 3,2 × 2,8 m | 1,8 × 2,6 m |

La taverne est donc **le grand bâtiment du village** : deux fois la largeur d'une maison. Sur le plan cible (plan n° 3), elle prend la place de deux maisons voisines ou s'installe en retrait ; à régler quand les cinq autres intérieurs seront dimensionnés (forge, boutique du mécano, boutique du druide, maison du sorcier, maison de décor fermée).

## Reste à trancher

- La galerie : décor seulement, ou accessible (deuxième niveau jouable, caméra à revoir) ?
- Clients anonymes en plus (deux ou trois villageois aux tables) ?
- La taverne la nuit : ouverte, ou fermée comme les boutiques (voir « Maisons la nuit » dans À décider) ?
