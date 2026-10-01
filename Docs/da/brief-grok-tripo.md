# Brief Grok → Tripo (01/10/2026, retravaillé le 01/10/2026 : « trop carré »)

Pour que les images de Grok passent directement dans la chaîne **Grok → Tripo (Image to 3D) → Blender → Unity** (essai réussi avec la Bavaroise, 01/10/2026). Grok fait le design ; Tripo reconstruit le volume à partir d'**une seule image** ; Blender allège en facettes, pose le squelette KayKit et reporte la texture. Ce qui aide Tripo : une image nette, un sujet seul, entier, sans décor ni ombre portée, sous une lumière égale.

À coller en tête de chaque demande à Grok (en anglais, Grok suit mieux), puis la fiche de l'objet.

## Correction du 01/10/2026 : « trop carré »

![Bavaroise, premier essai : trop carrée](../../ArtSources/References/Personnages/bavaroise_grok_v1_trop_carre.jpg) **Bavaroise, premier essai** | Trop carrée : grandes facettes plates, tablier en damier géométrique
![Référence de Quentin : personnages KayKit natifs](../../ArtSources/References/Personnages/kaykit_reference_facettage.png) **Référence de Quentin** | Personnages KayKit natifs du jeu : facettage dense et doux, silhouette ronde, presque lisse

Verdict de Quentin : « low poly trop de surfaces planes et d'angles ». Comparée aux personnages KayKit déjà dans le jeu (chevalier, viking, rôdeur, assassin, sorcière, mécano), la première Bavaroise a de **grandes facettes plates bien visibles** (visage en gros pans, cheveux en blocs hexagonaux, tablier en damier géométrique taillé dans la géométrie) qui donnent un effet « taille de pierre précieuse », alors que les personnages KayKit ont un facettage **beaucoup plus fin et dense**, qui se lit presque lisse et rond à distance de jeu. L'ancien bloc commun demandait explicitement « large readable facets » et « few large readable shapes » : c'est l'inverse de ce qu'il faut. Nouveau bloc ci-dessous. {dev} La décimation Blender (`ArtSources/Personnages/*/`\*`_pipeline.py`, normales plates, budget ~6 000 triangles) est probablement une deuxième cause du même effet : à revoir une fois qu'on aura confirmé que l'image Grok seule s'améliore.

## Bloc commun (toujours)

```
Low-poly 3D game asset, SMOOTH dense faceting: many small soft-beveled facets blending into one continuous rounded,
sculpted form. NOT large flat polygon planes, NOT a crystal/gem-cut look, NOT visible hard triangle edges up close.
Reference look: hand-sculpted mobile-game chibi toy figures (dense small facets, rounded limbs and curves, smoothed
edges everywhere except true seams like clothing hems, buckle edges, prop outlines) — NOT a faceted low-poly-art
look with big triangles. One solid flat color per small facet, matte, no glossy highlights, no texture noise, no
painted grime. A checkered or patterned fabric (gingham, plaid) is a flat painted color pattern, never hard
geometric facets cut into the mesh. Chunky toy-like fantasy chibi proportions, rounded silhouette, never boxy or
blocky.
Single isolated asset centered in frame, entire object visible with margin, nothing cropped.
Plain flat dark grey background (#2b2b2e), no floor, no ground disc, no cast shadow, no scenery, no other objects.
Soft even studio lighting from the front, no strong rim light, no dramatic shadows, no fog, no bloom.
Square image 1:1, high resolution. No text, no labels, no UI, no watermark.
World color rule: the only green light in the world is the emerald relic magic; never green glow on anything else.
```

## Personnages : 2 images par personnage

**Image A, pour Tripo** (la plus importante) :

```
Full-body character, front view, perfect symmetric T-pose: arms straight out horizontally, palms down, fingers
together, legs straight and slightly apart, feet flat, looking at the camera, neutral relaxed expression with the
character's usual face. Hands EMPTY: no weapon, no prop, no bag in hands. Orthographic-like front camera at chest
height. Head about 40-45 % of total height (chibi), thick limbs, simple readable silhouette.
```

**Image B, pour le design** : la planche comme celle de la Bavaroise (3/4 face avec l'arme, face, profil, dos, T-pose). Elle sert de référence pour les couleurs, le dos et les détails cachés ; Tripo ne la prend pas en entrée.

**Arme ou accessoire tenu, à part** (chope, luth, platines de DJ…) : une image dédiée, l'objet seul, de 3/4, même bloc commun, « single prop, not held ».

À éviter pour les personnages : cheveux ou vêtements qui flottent loin du corps, capes très longues, objets qui traversent le corps, poses dynamiques, plusieurs personnages, vue en plongée, **grandes facettes plates visibles, aspect taillé façon pierre précieuse, silhouette en blocs ou à angles droits, motif à carreaux rendu en facettes géométriques dures** (01/10/2026).

## Bâtiments et décor : 1 image par objet

```
Single building asset, three-quarter view from slightly above (about 30 degrees), whole building visible including
roof and base slab. The building stands alone: no terrain, no grass, no path, no fence, no trees, no props around
it, no characters. Doors and windows clearly readable. Scale cues: the plank door is about 2.6 m tall, a
single-storey wall is less than twice the height of a 2.3 m chunky character.
```

Puis le style maison du bloc de style du jeu (`_style-jeu.md`) : colombages, enduit crème, tuiles, porte cintrée encadrée de grosses pierres, socle de pierre lisse avec deux marches.

- **Accessoires de décor** (puits, potager, enseigne, tonneaux, socle de Nyxessa…) : **chacun dans sa propre image**, jamais collés au bâtiment, pour pouvoir les placer et les réutiliser.
- **Socle de Nyxessa** : le plateau de pierre seul, sans le cristal ni la lumière verte (le cristal existe déjà en effet de gemmes dans le jeu).
- **Bâtiment avec un intérieur à visiter** : une image fermée pour l'extérieur ; l'intérieur se fait à part.

## Vérifications avant d'envoyer à Tripo

- Sujet seul, entier, centré, fond uni, aucune ombre au sol.
- Personnage en T-pose de face, mains vides.
- Lumière égale (pas de face dans l'ombre).
- Couleurs proches de la palette voulue (Tripo les délave un peu ; on les recale ensuite sur la planche).

## Réglages Tripo qui ont marché (Bavaroise)

Image to 3D, **Modèle HD**, IA H3.1, **Générer par parties : Équilibré** pour un personnage (aide les couleurs et les poids), **Qualité de maillage Ultra**, **Complétion IA** activée, **Texture 2K**, **Supprimer l'éclairage** activé, PBR désactivé, **Triangle**, **100 000 polygones**, Confidentialité privée. Pas d'auto-rig ni de retopo Tripo. Puis **Texture** sur le modèle par parties, export **FBX avec texture** (archive) dans `ArtSources/References/Personnages/` (ou `ArtSources/References/Decor/` pour le décor).
