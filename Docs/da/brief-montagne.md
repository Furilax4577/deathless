# Brief Grok → Tripo de la montagne (01/10/2026)

La montagne du nord (falaise, cascade, grotte du portail, pierrier) se fait en **kit hybride**, pas en un seul modèle : la **structure de jeu reste procédurale** (gradins de la falaise à 18, 28 et 38 m, crête infranchissable, bordure du pierrier : ces cotes décident du NavMesh et des collisions) ; **Grok → Tripo → Blender** fournit l'habillage : une **pièce héros** (la face avec la grotte et la cascade, ce que le joueur voit tout le temps) et des **blocs et rochers réutilisables**. La cascade elle-même reste un effet dans le moteur (nappe d'eau et gemmes), pas un modèle. Chaîne et pièges : `Docs/da/brief-grok-tripo.md`.

Pourquoi pas une montagne d'un bloc : on ne la voit que du sud (le dos ne sert à rien), elle fait plus de 100 m de large, et Tripo ne tient pas une telle échelle ni des cotes exactes. Pourquoi Tripo quand même : des blocs arrondis et variés sont ce qu'il fait le mieux, et la vue aérienne de Grok du 01/10/2026 en montre le style visé (gros blocs arrondis, mousse, cascade).

## Par où commencer (essai à faible coût)

**Étape 1 : la pièce héros seule** (image A, puis Tripo). C'est la pièce qui compte le plus et le meilleur test : si Grok et Tripo la rendent bien (blocs arrondis, grotte et cascade à leur place, pas de facettes plates), on passe à l'étape 2 (blocs A à E et pierrier). Sinon, on garde la structure procédurale actuelle (blocs générés par code) et on n'a rien perdu : une image Grok, un modèle Tripo. Le reste de la paroi et le pierrier restent générés par code tant que l'étape 2 n'est pas décidée.

## Ce qu'on commande

| Pièce | Quantité | Dimensions réelles | Vues à demander | Triangles visés |
|---|---|---|---|---|
| **Pièce héros** : face de falaise avec grotte (gauche), cascade et bassin (centre-droit) | 1 | 34 m de large × 18 m de haut × 12 m de profondeur (bassin compris) | de face (image A, 16:9), plus un trois-quarts gauche (image B) si Tripo accepte la multivue | 12 000 à 15 000 |
| **Blocs de falaise** : A empilement large, B bloc d'angle convexe, C gradin plat, D pic fin, E lèvre de cascade | 5 | A 14 × 9 × 8 m ; B 10 × 9 × 10 m ; C 16 × 4 × 6 m ; D 5 × 16 × 5 m ; E 8 × 6 × 6 m | une image par bloc, trois-quarts avant gauche, légèrement en plongée (20°) | 2 500 à 4 000 chacun |
| **Pierrier** : amas de rochers (F), grand rocher isolé (G), dalle plate (H), menhir (I), petits cailloux (J) | 5 | F 8 × 3 × 6 m ; G 2,5 m ; H 3 × 0,6 × 2 m ; I 1 × 3 × 1 m ; J 1 m | une image chacun | 300 à 1 500 |

Les 10 petites pièces et blocs passent par le **mode « Images par lots »** de Tripo (jusqu'à 10 modèles séparés à la fois). La pièce héros se fait seule, avec la multivue de l'onglet **Modèle**.

## Placement (plan du village, `Docs/outils/plan_village.py`)

Falaise : arrière du village, y = 40 m ; bassin au pied de la cascade en (0 ; 38) ; grotte en (-14 ; 40), à gauche de la cascade ; route de la grotte (4 m, pavée, lanternes) du plateau à son seuil ; pierrier entre la falaise et les maisons (r = 26 à 34 m). La pièce héros couvre x de -24 à +10 m (grotte à -14, cascade à 0). Le reste de la paroi (x de -90 à -24 et de +10 à +90, et les gradins hauts) est fait de blocs A à E posés sur la structure procédurale. Gabarit à cotes de la pièce héros : `Docs/da/gabarits/montagne-face.png` (`python Docs/outils/montagne_face.py`).

## Bloc commun (à coller en tête de chaque demande, en anglais)

```
Low-poly 3D game asset, SMOOTH dense faceting: many small soft-beveled facets blending into big rounded, sculpted
boulders. NOT large flat polygon planes, NOT a crystal or gem-cut look, no hard visible triangle edges. Chunky,
toy-like, rounded forms with softly beveled edges, like a hand-made cozy low-poly game. Matte, clean simple surfaces:
no photographic rock texture, no fine cracks, no noise, no painted grime. Warm-neutral grey stone with a subtle
color variation from one boulder to the next (a few slightly warmer and slightly cooler greys), and a few small
patches of dull olive-green moss on the upper surfaces only. Single isolated asset centered in frame, entire object
visible with margin, nothing cropped. Plain flat dark grey background (#2b2b2e), no floor, no ground disc, no cast
shadow, no scenery, no other objects, no characters. Soft even studio lighting from the front, no strong rim light,
no dramatic shadows, no fog, no bloom. No text, no labels, no UI, no watermark.
World color rule: the only green light in the world is the emerald relic magic and the cave portal; moss is a dull
olive green and never glows.
```

Format : 1:1 pour les blocs et rochers ; 16:9 pour la pièce héros.

## Image A : pièce héros, vue de face (16:9, avec `montagne-face.png` comme image de départ si possible)

```
Straight-on front elevation, the camera at mid-height looking exactly horizontally at a massive cliff face made of
big stacked rounded boulders in horizontal layers with ledges, 34 m wide and 18 m high (a chunky 2.3 m character
would be only as tall as one boulder at the bottom). On the left, at the foot of the cliff, an arched cave mouth 8 m
wide and 5 m high, dark inside, with a swirling emerald-green magic portal glowing softly far inside (the only
glowing green), a flat level floor and a pale flagstone threshold in front. Left of the center, nothing but boulders.
At the center-right, a narrow waterfall 3 m wide pouring from a notch near the top of the face, down the rocks,
into a round clear-blue pool about 8 m across at the foot of the cliff, with a few pebbles around it. The top edge
is a rough skyline of rounded boulders; the left and right ends taper down into low boulder piles so the piece can
be joined to other pieces. Flat ground at the bottom; nothing above or behind the cliff, only the plain background.
```

## Image B : pièce héros, trois-quarts gauche (facultative, mêmes éléments, mêmes cotes)

Même texte, avec « Three-quarter view from the front-left, about 30 degrees to the left, slightly from above ; the pool, the waterfall and the cave mouth are all visible; same cliff, same composition and same dimensions ». Elle donne la profondeur du bassin et le retrait de la grotte.

## Blocs A à E (une image chacune, 1:1)

À ajouter après le bloc commun, avec « Three-quarter view from the front-left, slightly from above (about 20 degrees) » :

- **A, empilement large** : « a wide stack of five or six big rounded boulders of different sizes forming a cliff block, 14 m wide, 9 m high, 8 m deep, flat bottom, a rough stepped top. »
- **B, bloc d'angle** : « a massive convex corner block of stacked rounded boulders that turns a corner of a cliff, 10 m wide, 9 m high, 10 m deep, flat bottom. »
- **C, gradin** : « a long low stepped rocky ledge, like a wide stair of flat-topped rounded boulders, 16 m long, 4 m high, 6 m deep, flat bottom. »
- **D, pic** : « a tall slender rounded rock spire made of stacked boulders, 5 m wide, 16 m high, 5 m deep, narrow at the top, flat bottom. »
- **E, lèvre de cascade** : « a rocky notch where a stream falls: two rounded boulder shoulders with a smooth rounded channel 2.5 m wide between them, 8 m wide, 6 m high, 6 m deep, flat bottom; no water, only the dry channel. »

## Pierrier F à J (une image chacune, 1:1)

À ajouter après le bloc commun :

- **F, amas** : « a loose cluster of five to seven rounded boulders of very different sizes lying together, 8 m wide, 3 m high, 6 m deep. »
- **G, grand rocher** : « one single big rounded boulder, 2.5 m wide, slightly flattened, sitting on the ground. »
- **H, dalle** : « one flat, wide, rounded rock slab, 3 m long, 0,6 m thick, lying flat. »
- **I, menhir** : « one standing rounded stone, 1 m wide, 3 m tall. »
- **J, cailloux** : « a small group of four or five rounded pebbles and stones, together 1 m wide. »

## Réglages Tripo

- **Blocs et rochers (A à J)** : onglet **Image** (une image par modèle, ou **Images par lots** pour tous d'un coup) ; **Modèle HD**, IA H3.1 ; **Générer par parties : désactivé** (inutile pour de la roche) ; Qualité de maillage **Ultra** ; **Texture 2K activée** ; **PBR désactivé** ; **Supprimer l'éclairage activé** ; **Triangle** ; **100 000 polygones** ; confidentialité privée ; export FBX avec texture.
- **Pièce héros** : onglet **Modèle** (multivue), face + trois-quarts gauche si disponible ; mêmes réglages, **texture 4K**, **200 000 polygones**.

## Réception dans Blender puis Unity

1. **Échelle** : redimensionner chaque pièce **d'après sa boîte englobante** aux dimensions du tableau (Tripo n'a aucune échelle métrique) ; origine au centre du dessous, face avant vers -Y.
2. **Décimation** aux budgets du tableau, **normales lissées** (comme la Bavaroise v4 : faces lisses, arêtes dures seulement aux vrais angles, Weighted Normal), **un seul atlas partagé de 2048²** pour tout le kit (un matériau pour toute la montagne), cuisson depuis la haute définition.
3. **Unity** : prefabs avec **collisions simplifiées** (boîtes ou maillage convexe, jamais le maillage rendu sur la grande face) ; les blocs ne sont pas marqués marchables dans le NavMesh (obstacles), seule la structure procédurale décide de la crête infranchissable ; instances répétées (rotations, échelles) pour limiter les appels de rendu ; **LOD** ou distance de coupe pour les petits rochers.
4. **Cascade et bassin** : effets dans le moteur ; la grotte garde son volume simple de 8 × 6 × 5 m avec le portail (`PortalVoxel`), dont l'intérieur reste procédural.

## Vérifications avant d'envoyer à Tripo

Sujet seul, entier, centré, fond uni, aucune ombre au sol ; blocs **arrondis, sans grandes facettes plates** ; mousse mate, jamais lumineuse ; aucun vert luisant hors du portail ; pour la pièce héros, grotte à gauche, cascade au centre-droit, bassin en pied, comme sur le gabarit.
