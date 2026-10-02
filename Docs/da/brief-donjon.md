# Brief Grok du donjon, version « vraies salles » (02/10/2026)

Retour de Quentin : le donjon actuel ne convainc pas ; il le veut **plus ouvert, avec de vraies salles, moins d'effet « balcon parisien »** (des galeries et mezzanines en bois qui bordent les murs et surplombent le sol, partout). Ce brief fixe la **disposition cible** et donne les **prompts Grok** (plan, coupe isométrique, vues à hauteur de joueur, salles). Règles de construction déjà décidées : `Wiki/pages/donjon.md` (lieu clos sans ciel, torches, brouillard, murs pleins, ossements, coffres vidés, gardiens, découpe autour du héros). Échelle et caméra : `Docs/da/maisons-plans.md` (personnage 2,3 m, caméra à l'épaule 5,5 m / 22°, 4,5 m de hauteur libre, 5 à 6 m de recul).

## Ce qui change

| Avant | Cible |
|---|---|
| Blocs de halls bordés de balcons et de mezzanines à arcades de bois ; plusieurs niveaux qui se surplombent | **Salles distinctes**, de vraies pièces à une seule hauteur, reliées par des **ouvertures larges** (arches de pierre de 3 à 4 m, couloirs de 3 m) ; **aucun balcon qui surplombe une salle** |
| Aucune porte, arcades de bois partout | Arches de pierre et couloirs ; des **encadrements** de pierre aux ouvertures (pas de porte fermée : décision conservée) |
| Beaucoup de niveaux | **Un niveau principal** ; du relief par des **marches et des estrades basses** (au plus 1,2 m : fosse, estrade, bassin) et **un seul étage** accessible par un large escalier, jamais en balcon ouvert sur une salle |
| Murs d'enceinte à fenêtres fermées et balcons | Murs pleins en gros blocs de pierre, **piliers** et **voûtes** qui rythment la salle ; **plafond visible** (voûtes, poutres) à 5 à 7 m |

## Version 2 : le croquis de Quentin (02/10/2026), qui remplace la disposition à six salles ci-dessous

![Croquis de Quentin](../references/donjon-croquis-quentin-20261002.jpg)

Quentin : « Plus comme ça, le donjon ». Lecture du croquis : **un seul grand volume fermé, en terrasses étagées**, avec **trois niveaux de sol pleins**, séparés par de **gros murs de soutènement** (jamais un balcon qui surplombe) :

| Niveau | Rôle | Dimensions | Hauteur du sol |
|---|---|---|---|
| **Lvl 0** | Grande salle principale en bas (arrivée, gros combats, piliers) : le plus grand espace, ouvert | environ 28 × 18 m | 0 m |
| **Lvl 1** | Terrasse à droite et au fond, contre le mur nord-est ; **un large escalier de 4 m sur son côté droit** (le long du mur est) y monte depuis le Lvl 0 | environ 14 × 12 m | + 3 m |
| **Lvl 2** | Terrasse à gauche et au fond, contre le mur nord-ouest, plus haute ; **un large escalier de 4 m à gauche** (le long du mur ouest) y monte depuis le Lvl 0, et un **mur épais en L** la sépare du Lvl 1 | environ 12 × 12 m | + 6 m |

- **Murs de soutènement** : pleins, en gros blocs, de 3 m de haut, qui bordent les terrasses ; ce sont eux que le croquis dessine en épaisseur. **Une arche (porte ouverte, sans vantail) dans chacun de deux murs** : l'un, en face avant du Lvl 2, et l'autre dans le mur épais entre les niveaux 2 et 1 : elles donnent sur de **petites pièces ou couloirs cachés dans la masse** (réserve, crypte ou passage secret : à détailler plus tard).
- **Un plafond commun** : une voûte haute (10 à 14 m) couvre tout le volume ; sur les terrasses il reste **au moins 4,5 m de hauteur libre** au-dessus du sol (caméra) ; la hauteur est tenue par des **piliers** sur le Lvl 0 et le long des murs de soutènement.
- **Jeu** : les monstres peuvent tirer depuis la terrasse haute ; les joueurs montent par les deux escaliers (flanquement), le butin est sur les terrasses (Lvl 1 : armurerie et coffres ; Lvl 2 : la salle du trésor et les gardiens principaux), l'arrivée est au Lvl 0. **Pas de balcon, pas de galerie surplombante, pas de mezzanine** : tout est du sol plein avec un mur de soutènement dessous.

## Ancienne disposition (6 salles, remplacée par la version 2) (plan, 6 salles, un circuit avec boucle)

Origine : l'**arrivée** au sud. Toutes les salles ont **au moins 12 × 10 m** de sol dégagé (la place de quatre joueurs et de leurs caméras), des **couloirs de 3 m** au moins, aucun cul-de-sac étroit.

1. **Salle d'arrivée** (12 × 10 m) : le portail de retour, dalle d'arrivée en bois sombre ; torches ; deux ouvertures (nord, est).
2. **Grande salle** (24 × 18 m, voûtée à 7 m, **quatre gros piliers**) : le centre du donjon, de grands espaces dégagés entre les piliers, une **estrade basse** (0,6 m) au nord ; **quatre ouvertures** (sud vers l'arrivée, est, ouest, nord).
3. **Armurerie** (14 × 10 m) à l'est : râteliers, tonneaux, caisses, deux coffres ; gardiens.
4. **Crypte / ossuaire** (14 × 12 m) à l'ouest : niches et sarcophages le long des murs, ossements, **crânes aux points d'apparition** (les squelettes en sortent : un crâne posé près de chaque point, pas dessus).
5. **Citerne** (16 × 12 m) au nord-est : un **bassin d'eau peu profonde** au centre (on le traverse en ralentissant ; deux passerelles de pierre), colonnes dans l'eau.
6. **Salle du trésor** (12 × 10 m) au nord, au bout : un **escalier large de 4 m** de six marches descend dans la salle ; **coffres** le long des murs, une **estrade** centrale ; **les gardiens principaux** ; **c'est la seule salle à l'étage** (ou en contrebas) : jamais de galerie surplombante.
**Boucle** : grande salle → armurerie → citerne → trésor → crypte → grande salle (couloirs de 3 m), pour qu'on ne revienne pas toujours par le même chemin.

## Bloc de style commun (en anglais, à coller en tête de chaque demande)

```
Low-poly 3D game dungeon, chunky toy-like proportions with softly beveled edges, like a hand-made cozy low-poly game,
NOT realistic, NOT clay-like. Big rounded stone blocks (about 40 cm), thick round pillars, smooth vaults, dark grey-blue
stone with warm torchlight. FLAT UNLIT-LOOKING COLORS in clean zones, no photographic detail, no wood grain, no
grime, no noise. Dim closed underground place, NO sky, NO windows to the outside, NO daylight. Warm orange torches on
the walls (flames orange and yellow, NEVER green). The only green light in the whole game is the emerald relic magic:
no green anywhere except the return portal.
NO balconies, NO mezzanines, NO galleries or walkways running along the walls above the floor, NO wooden arcades:
every room is a real separate room with one single floor level, walls that rise straight to a vaulted ceiling.
```

## 1. Plan en vue de dessus (1:1)

```
Top-down orthographic floor plan of a game dungeon, no perspective, the roof removed, drawn as a clean low-poly game map:
six distinct rooms connected by wide stone-arched openings and 3 m wide straight corridors, a loop circuit. From the
bottom (south) to the top: a small arrival room with a dark wooden arrival slab and a glowing emerald green portal;
north of it a very large central great hall (twice as large as the others) with four big round pillars and a low stone
dais at its north end, with four openings (south, east, west, north); to the EAST an armory room with weapon racks,
barrels and crates; to the WEST a crypt with niches, sarcophagi and bones along the walls; to the NORTH-EAST a cistern room
with a large shallow pool in the middle crossed by two stone walkways, columns standing in the water; at the far NORTH a
treasure room reached by a wide six-step staircase, with chests along the walls and a central dais. All rooms are at
least 12 x 10 meters. The rooms are shown with their floors (dark stone slabs), their furniture and props, torches as small
orange dots on the walls, and the corridors; no characters. Seen from directly above. Plain dark background. No text,
no labels, no legend.
```

## 2. Coupe isométrique, toit retiré (16:9)

```
Isometric cutaway view of a low-poly game dungeon seen from the south-west at 45 degrees from above, the roof and the front
walls removed so every room is visible: a small arrival room with a glowing emerald portal, a very large vaulted central
great hall with four big round pillars and a low dais, an armory with weapon racks, a crypt with sarcophagi and bones, a
cistern with a shallow pool and stone walkways, and a treasure room up a wide staircase with chests. Rooms are separated by
thick stone walls with wide arched openings and straight corridors; walls of big rounded stone blocks; warm orange
torches; one single floor level (the treasure room up six steps). Four tiny chunky big-headed adventurers (knight with red
cape, viking, mage with purple pointed hat, hooded ranger) in the great hall for scale: each is only as tall as one wall block.
NO balconies, NO mezzanines, NO galleries over the rooms.
```

## 3. Grande salle, vue à hauteur de joueur (16:9)

```
Interior of the central great hall of a low-poly game dungeon, seen from the game's third-person shoulder camera: 5.5 m
behind and 3.7 m above a chunky big-headed knight with a red cape standing in the middle of the room, looking forward
and slightly down, field of view 60 degrees. The hall is 24 m wide and 18 m deep with a vaulted stone ceiling about 7 m
high, four big round stone pillars, large open floor of dark stone slabs, a low stone dais at the far end, wide arched
openings leading to other rooms on each wall, warm orange torches on the walls and pillars, dim blue-grey stone,
a faint mist near the floor. A few bones and a barrel in the corners only. NO balconies, NO galleries, NO upper floor
visible, NO windows. The floor is mostly empty so four players can fight.
```

## 4. Les autres salles (même cadrage, une image chacune)

À ajouter après le bloc de style et le cadrage de la vue 3 (« seen from the game's third-person shoulder camera… ») :

- **Armurerie** : « a 14 x 10 m armory room, weapon racks full of swords and axes along the walls, shield rows, barrels and crates in the corners, two closed wooden chests, a big anvil-less workbench against one wall, vaulted ceiling 5 m high, wide arch behind the camera; open empty floor in the middle. »
- **Crypte** : « a 14 x 12 m crypt, rows of stone niches with skulls along the walls, three big stone sarcophagi with lids ajar against the walls, piles of bones and a few skulls lying on the floor near the walls, low vaulted ceiling 5 m high, cold blue-grey stone, a few torches; the middle of the floor is open. »
- **Citerne** : « a 16 x 12 m cistern room, a large shallow pool of dark blue water in the middle (knee deep, flat water surface) crossed by two straight stone walkways 3 m wide, thick round columns standing in the water holding a vaulted ceiling 6 m high, stone floor around the pool, torches on the columns, a faint mist above the water. »
- **Salle du trésor** : « a 12 x 10 m treasure room entered down a wide six-step stone staircase (4 m wide) at the camera's back, four closed wooden chests with iron bands along the walls, a low round stone dais in the middle, warm golden-orange torchlight, vaulted ceiling 5 m high, NO gold piles on the floor (coins stay in the chests), nothing overhanging. »
- **Salle d'arrivée** : « a 12 x 10 m arrival room with a dark wooden arrival slab on the floor, an arched stone frame on the south wall holding a swirling emerald green portal (the only green), two torches, two wide arched openings north and east. »

## Prompts de la version 2 (croquis de Quentin)

### 1 bis. Plan en vue de dessus avec niveaux (1:1)

```
Top-down orthographic plan of a game dungeon made of ONE big enclosed vaulted hall with three stepped terraces, no
perspective, the roof removed, drawn as a clean low-poly game map. The big main floor (Level 0, 28 x 18 m, dark stone
slabs, four round pillars, a dark wooden arrival slab and a glowing emerald green portal at the south edge) occupies the
whole bottom. At the top right corner a raised terrace (Level 1, 14 x 12 m, 3 m higher) with weapon racks and chests,
reached by a wide 4 m staircase running along the east wall. At the top left corner a higher terrace (Level 2, 12 x 12 m,
6 m higher) with chests and a central dais, reached by a wide 4 m staircase running along the west wall. A thick L-shaped
stone wall separates the two terraces; thick retaining walls (3 m high, shown with their thickness) border the terraces;
two arched doorways (open, no door leaf) in the retaining walls lead to small hidden rooms. Warm orange torch dots on the
walls. Each level has a slightly different shade of dark stone so the three heights are readable; stairs drawn with steps.
No characters. Plain dark background. No text, no labels, no legend.
```

### 2 bis. Coupe isométrique, comme le croquis (16:9)

```
Isometric cutaway view from the south-west at 45 degrees from above of a low-poly game dungeon: a single huge enclosed
hall with the roof and the front walls removed, built as THREE STEPPED TERRACES with solid stone retaining walls (no
balconies, no overhangs, no galleries): a big lower floor in front (Level 0, with four round pillars, an arrival slab and a
glowing emerald portal), a raised terrace at the back right (Level 1) reached by a wide staircase along the right wall, and a
higher terrace at the back left (Level 2) reached by a wide staircase along the left wall, a thick L-shaped wall between the
two terraces, and two arched doorways in the retaining walls. Big rounded stone blocks, warm orange torches, dark grey-blue
stone. Four tiny chunky big-headed adventurers (knight with red cape, viking, mage with purple pointed hat, hooded ranger)
on the lower floor for scale: each is only as tall as one wall block.
```

### 3 bis. Depuis le Lvl 0 vers les terrasses, à hauteur de joueur (16:9)

```
Interior of the big stepped hall of a low-poly game dungeon seen from the game's third-person shoulder camera: 5.5 m behind
and 3.7 m above a chunky big-headed knight with a red cape standing on the big lower floor, looking at the two raised
terraces ahead: on the right a terrace 3 m higher with a wide staircase along the right wall, on the left a terrace 6 m
higher with a wide staircase along the left wall, thick retaining walls of big rounded stone blocks between the levels,
two arched doorways in them, four big round pillars on the lower floor, a high vaulted ceiling, warm orange torches, dim
blue-grey stone, a faint mist on the floor. NO balconies, NO galleries, NO overhanging platforms: every level is solid
floor on top of a solid wall. Field of view 60 degrees.
```

## Réception (côté Unity)

Ces images servent de **cible de disposition et d'ambiance** : le générateur du donjon (`Assets/Scripts/Donjon/DonjonGenerateur.cs`, `DonjonPlan.cs`, `DonjonKit.asset` ; décisions `Wiki/pages/donjon.md`) sera refondu en **salles** (plan à six salles, ouvertures de 3 à 4 m, voûtes, piliers, estrades basses, pas de balcon) ; les modèles du kit (murs, piliers, arches, voûtes, râteliers, sarcophages) peuvent venir de Grok → Tripo comme les maisons (chaque pièce séparée) ou du pack KayKit Dungeon actuel selon ce que Quentin choisit après les images.
