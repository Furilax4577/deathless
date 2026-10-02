# Brief Grok → Tripo des maisons (02/10/2026)

Six bâtiments, **quatre vues chacun** (face, côté gauche, côté droit, dos) pour le mode **multivue de Tripo** (onglet **Modèle** : image principale = face ; emplacements **Gauche**, **Droite**, **Dos**), comme pour la Bavaroise v4 (`Docs/da/brief-grok-tripo.md`). Dimensions et intérieurs : `Docs/da/maisons-plans.md` et `taverne-plan.md`. Les extérieurs sont fermés (l'intérieur est une zone à part, porte et fondu au noir) ; les accessoires vont **chacun dans leur propre image** (jamais collés au bâtiment). **Décidé par Quentin le 02/10/2026 : la porte fait un léger mouvement d'ouverture avec un son quand on s'en sert, et les volets se ferment la nuit.** Donc **la porte (battants) et les volets sont des pièces à part, animées dans Unity** : les quatre vues de la maison se demandent **sans battant de porte et sans volets** (embrasure vide, fenêtres nues), et les battants et volets se produisent séparément (section « Portes et volets » plus bas).

**Ordre conseillé** : maison de base (la plus simple, pour tester la chaîne) → mécano → forge → druide → sorcier → taverne (déjà faite par code et validée ; la version Tripo est facultative, à comparer).

## Règles pour les quatre vues

- **Même conversation Grok pour les quatre vues.** Génère d'abord la **face**, valide-la, puis redemande en joignant cette image : « *Same building, same colors, same proportions, same scale and camera height; now the LEFT side elevation* » (puis droite, puis dos). C'est ce qui garde le même bâtiment sous quatre angles.
- **Vues orthographiques (sans perspective), caméra à 15° au-dessus de l'horizontale, identique dans les quatre images** : la légère plongée montre les pans du toit (sans elle, Tripo ne voit pas le toit et le laisse creux, comme à l'essai du 02/10/2026). Bâtiment **entier et centré avec de la marge** (du socle au sommet de la cheminée), **même échelle dans les quatre images** (même hauteur de bâtiment en pixels).
- **Un toit entièrement fermé** : « the roof is a solid closed roof, both slopes completely covered with clay tiles, no gaps, no holes, nothing see-through ».
- **Compte exact** : une seule cheminée (sauf mention), le nombre exact de fenêtres de chaque mur. Chaque vue doit montrer **exactement ce que dit la ligne « Vues » de la maison** : si Grok invente un détail ou une seconde cheminée, on refait la vue (le premier essai a donné deux cheminées et un toit creux parce que les vues ne se recoupaient pas).
- **Repères** : « face » = la façade à la porte, tournée vers Nyxessa. « Gauche » = le côté qui est à votre **gauche quand vous faites face à la porte, vu de l'extérieur** : sur cette vue, la **façade est au bord DROIT** de l'image. « Droite » = l'autre côté : la **façade est au bord GAUCHE**. « Dos » = le mur opposé à la porte (la gauche du bâtiment est alors à droite de l'image).
- **Sans battant de porte et sans volets, sur les quatre vues** : la porte est une **embrasure arrondie vide** (renfoncement sombre d'au moins 30 cm de profondeur, cadre de gros blocs de pierre), les fenêtres sont des ouvertures à cadre de bois simple et carreaux, **sans volets**. Les battants et les volets viendront s'y poser, articulés.
- Format **1:1**, fond uni gris foncé, un seul bâtiment, rien autour.

## Bloc commun (en anglais, à coller en tête de chaque demande)

```
Low-poly 3D game building, SMOOTH dense faceting: many small soft-beveled facets blending into rounded, sculpted
forms, chunky toy-like proportions with softly beveled edges, like a hand-made cozy low-poly game. NOT large flat
polygon planes, NOT a crystal or gem-cut look, no hard visible triangle edges. Matte, clean simple surfaces: no wood
grain, no stone masonry texture, no photographic detail, no noise, no painted grime.
Half-timbered cottage: smooth cream plaster walls, dark red-brown timber frame, clay-tile roof, stone chimney, an
arched doorway framed by large plain light-grey stone blocks, EMPTY: a dark recessed doorway with NO door leaf and NO door, windows with simple wooden frames and glass panes and NO shutters, standing on a LOW light-grey stone slab with a couple of steps in front of the door, with a plinth of exactly TWO low
courses of grey stone blocks at the foot of the walls (never a high masonry base).
Scale cues: the doorway is 1.8 m wide and 2.6 m tall, the walls are 4.4 m high, a chunky big-headed 2.3 m
character would be almost as tall as the door.
Single isolated building centered in frame, entire building visible with margin, nothing cropped. Plain flat dark grey
background (#2b2b2e), no ground, no grass, no path, no fence, no trees, no props around it, no characters, no cast
shadow. Soft even studio lighting from the front, no strong rim light, no dramatic shadows. Square image 1:1. No text,
no labels, no UI, no watermark. The only green light in the world is the emerald relic magic: no green glow anywhere.
```

Phrase de vue à ajouter à la fin (la **description de la maison** vient avant) :

- **Face** : « Straight-on orthographic FRONT elevation of the facade with the door, camera 15 degrees above the horizontal, no perspective distortion. »
- **Gauche** : « Straight-on orthographic LEFT SIDE elevation (the wall on your left when you face the front door from outside), same scale and same 15-degree camera elevation as the front view; the front facade with the door is at the RIGHT edge of the picture. »
- **Droite** : « Straight-on orthographic RIGHT SIDE elevation (the wall on your right when you face the front door from outside), same scale and same 15-degree camera elevation as the front view; the front facade with the door is at the LEFT edge of the picture. »
- **Dos** : « Straight-on orthographic BACK elevation, the wall opposite the front door, same scale and same 15-degree camera elevation as the front view; no door on this wall. »

---

## 1. Maison de base (8,6 × 7,1 m, murs 4,4 m, faîtage environ 8 m)

La maison standard du jeu : la plus simple, **elle sert de gabarit aux maisons d'habitants anonymes**. Puits, potager clôturé, étendoir et tas de bois sont des **accessoires à part** (voir plus bas).

```
A small single-storey half-timbered cottage, 8.6 m wide and 7.1 m deep, steep 45-degree gabled roof of big smooth
clay tiles in warm brick red, a solid closed roof with a visible ridge, and exactly ONE short stone chimney standing against
the LEFT gable wall and rising above the ridge; a single empty arched doorway on the front (no door leaf),
set a little to the left, with two steps; one window with a plain wooden frame to the right of the door; a small round
window under the roof peak in the RIGHT gable only (the left gable has the chimney instead); one window on the back wall;
one window on each side wall; a small hanging lantern between the doorway and the window.
```

**Vues** : *face* : embrasure à gauche, une fenêtre à droite, lanterne entre les deux, pan avant du toit ; la cheminée de pierre monte contre le pignon GAUCHE. *Gauche* : pignon avec la cheminée au centre qui dépasse le faîte, une fenêtre en bas à côté. *Droite* : pignon avec une fenêtre et un petit hublot rond sous le faîte, pas de cheminée. *Dos* : une fenêtre au centre, pan arrière du toit, la cheminée visible à l'extrémité gauche du faîte (donc à DROITE de l'image).

**Phrases à ajouter à la phrase de vue (maison de base, en anglais)** :
- Face : « The single stone chimney is seen at the far left, rising behind the front roof slope; the round window of the right gable is not visible from here. »
- Gauche : « On this wall we see the gable end with the single stone chimney rising up its center above the ridge, and one window at the bottom beside it; no round window on this gable. »
- Droite : « On this wall we see the gable end with one window and a small round window under the roof peak; no chimney on this gable. »
- Dos : « One window in the middle of the back wall, the back roof slope, and the single chimney at the end of the ridge (it appears on the RIGHT side of the picture since the building's left is on the right here); no door on this wall. »

## 2. Boutique du mécano (12,6 × 9,6 m, un étage en façade, faîtage environ 9,5 m)

```
A shop with a street-facing gable, 12.6 m wide and 9.6 m deep, two storeys at the front: a tall front gable with a
second floor with two small windows, a steep 45-degree gabled roof of clay tiles in warm brick red, cream plaster
and dark timber frame; on the ground floor an empty arched doorway (no door leaf) on the left (1.8 m wide) and, to the right of it, a very
large shop window 4 m wide with many small square panes and a wooden sill; a bent grey metal stove pipe goes
through the roof near the back, with a small cog-shaped weathervane on the ridge; no shutters; one
window on the back wall, one on each side wall at ground level, an upper window in each gable.
```

**Vues** : *face* : haut pignon à deux petites fenêtres, embrasure à gauche, grande vitrine à droite, girouette en engrenage au faîte. *Gauche* : une fenêtre au rez-de-chaussée, une fenêtre haute au pignon, le tuyau de poêle coudé dépasse du toit vers l'arrière. *Droite* : idem côté droit, sans tuyau visible. *Dos* : une fenêtre, pan arrière du toit, le tuyau de poêle.
Accessoires à part : enseigne en forme d'engrenage et de clé croisés sur potence de fer, caisses de bois empilées.

## 3. Forge (11,6 × 9,6 m + appentis ouvert 5 × 7 m côté droit)

```
A blacksmith workshop, 11.6 m wide and 9.6 m deep, half-timbered with cream plaster darkened with a little soot
near the chimney, a roof of clay tiles in dark brown, a very massive tall stone chimney on the back right of the
roof; a plain empty arched doorway (no door leaf) on the front, left of center, and one barred window with iron bars and no shutters
to its right; on the RIGHT side of the building an open lean-to shed, 5 m wide and 7 m deep, with no walls, a low
sloping brown roof held by thick wooden posts, sheltering a stone hearth with an orange fire, a stack of logs and a
low wall; one barred window on the back wall and one on the left side wall.
```

**Vues** : *face* : embrasure à gauche du centre, une fenêtre à barreaux à droite, l'appentis ouvert à l'extrême droite, la grosse cheminée de pierre au fond à droite. *Gauche* : mur plein avec une fenêtre à barreaux, pas d'appentis. *Droite* : l'appentis ouvert (poteaux épais, toit bas, foyer de pierre au feu orange, tas de bûches) devant le mur. *Dos* : une fenêtre à barreaux, la grosse cheminée.
Accessoires à part : enclume sur billot, tas de bûches, soufflet, roue de meule, outils accrochés. (Le foyer est dans l'appentis : orange chaud, jamais vert.)

## 4. Boutique du druide (11,6 × 8,6 m + auvent 2 × 4 m côté droit, toit à 50°)

```
An herbalist cottage, 11.6 m wide and 8.6 m deep, half-timbered with cream plaster, a very steep 50-degree gabled
roof covered with thick soft olive-green moss over brown tiles, a short crooked stone chimney, green ivy climbing
over parts of the timber frame and the front gable (matte leaves, no glow); an empty arched doorway (no door leaf) on the front, left
of center, and two windows with flower boxes full of small yellow and purple flowers under them; on the RIGHT side of
the building a small open drying porch, 2 m by 4 m, with a lean-to roof on two posts and bunches of dried herbs
hanging from the beams; one window on the back wall and one on the left wall.
```

**Vues** : *face* : embrasure à gauche du centre, deux fenêtres à jardinières fleuries, lierre sur le colombage, auvent de séchage à l'extrême droite. *Gauche* : une fenêtre, lierre, petite cheminée tordue. *Droite* : l'auvent ouvert avec les bouquets d'herbes suspendus. *Dos* : une fenêtre, lierre sur le pan arrière.
Accessoires à part : enseigne en forme de fiole ambre, jardinières, claie de séchage, bacs de champignons.

## 5. Maison du sorcier (11,6 × 9,6 m + tour ronde Ø 4,5 m)

```
A wizard's house, 11.6 m wide and 9.6 m deep, half-timbered with cream plaster, a gabled roof of dark blue slate
shingles, and an attached round stone-and-plaster tower of 4.5 m diameter standing at the BACK RIGHT corner of the
house, taller than the roof, with a tall conical dark blue slate roof topped by a crescent-moon weathervane, a round
porthole window and a small slit window; an empty arched doorway (no door leaf) on the front, left of center, and two windows
with plain frames and no shutters; one window on the back
wall, a window on the left wall.
```

**Vues** : *face* : embrasure à gauche du centre, deux fenêtres, la tour ronde visible derrière à droite, toit conique bleu avec croissant de lune. *Gauche* : une fenêtre ; la tour (au coin arrière droit, donc de l'autre côté) dépasse du toit à l'arrière, au bord GAUCHE de l'image. *Droite* : la tour accolée au coin arrière, au bord DROIT de l'image (la façade est au bord gauche), avec son hublot rond et sa fente. *Dos* : la tour à GAUCHE de l'image (coin arrière droit du bâtiment), une fenêtre.
Accessoires à part : lanterne à vitre bleutée, cloche de verre (l'éclat de Nyx est à l'intérieur : jamais dessiné ici). Le hublot de la tour est pâle et « qui luit » seulement en blanc-bleu, **pas de vert**.

## 6. Taverne (16,6 × 11,6 m, murs 5,4 m, faîtage 10,7 m ; déjà construite par code)

```
A big tavern: a very long single-storey great hall, 16.6 m wide and 11.6 m deep, with walls 5.4 m high and a very tall
hipped roof with half-hips (the gable ends are cut by a small hip) at about 40 degrees, clay tiles in warm brick red; a
huge stone chimney rising from the LEFT end wall; on the front, in the middle, a wide empty arched double doorway
(3.2 m wide, no door leaves) in a frame of large grey stone blocks, under a small tiled porch roof on two timber brackets, two steps;
on the front a large window with a plain frame and no shutters on each side of the doorway; one window on each side wall on the
left and right ends; plenty of dark timber framing with diagonal braces; the back wall has timber framing and no
door.
```

**Vues** : *face* : double embrasure au centre sous le petit auvent de tuiles, une grande fenêtre de chaque côté, chaînage de colombages. *Gauche* : la très grande cheminée de pierre contre le mur d'extrémité, une fenêtre. *Droite* : mur d'extrémité avec une fenêtre. *Dos* : colombages et diagonales, aucune porte, pas de fenêtre.
Accessoires à part : enseigne en forme de chope sur potence de fer, deux tonneaux et un banc, lanternes d'applique. Référence visuelle : `sandbox-level/Assets/Screenshots/taverne_ext_*.png` (hors dépôt).

---

## Mode recommandé depuis le 02/10/2026 : UNE SEULE IMAGE (trois-quarts)

**Soubassement (décidé par Quentin, 02/10/2026)** : pour les maisons Tripo, on garde **un soubassement de deux rangées de pierres** au pied des murs et **deux marches** devant la porte (comme sur l'image de Grok de la maison de base). **Précision du 02/10 (soir) : pas de grande plateforme dallée autour de la maison** (Tripo la garde et la modélise : 4 m² de dalle inutile) ; la fondation est **le soubassement de deux rangées seul**, avec deux marches devant la porte. Cela remplace la règle « dalle lisse sans maçonnerie » de la maison générée par code ; limite : deux rangées, pas de soubassement haut.

Essai du 02/10/2026 sur la maison de base : avec **la seule vue de face**, Tripo a donné une géométrie très bonne (tuiles en relief, arche en pierres appareillées, colombages, marches, lanterne, toit fermé, une cheminée) ; avec quatre vues peu différentes, un toit creux et deux cheminées. **On passe donc à une image unique par maison**, plus riche : un **trois-quarts** qui montre en même temps la façade, un pignon et le toit.

Règles de l'image unique :
- **Trois-quarts avant-droit, caméra à environ 30° au-dessus de l'horizontale** (on voit la façade avec l'embrasure, le pignon droit, et le dessus du toit), bâtiment **entier, centré, avec de la marge**, rien d'autre dans l'image.
- **Couleurs plates et franches, sans ombres peintes** : la couleur de la texture Tripo vient de l'image. Demander « flat unlit albedo colors, no cast shadows, no baked ambient occlusion, no gradients of light » donne des zones propres (tuiles, plâtre, colombage, pierre) que la recoloration en palette sépare sans bavure.
- **Relief visible** : tuiles individuelles, claveaux de l'arche, poutres épaisses : Tripo ne crée pas de relief qu'il ne voit pas.
- **Sans battant de porte ni volets** (pièces à part), **toit entièrement fermé**, **une seule cheminée** à la place indiquée.
- Tripo : onglet **Image** (une image, pas multivue), mêmes réglages ; la **texture se fait ensuite** (outil Texture) ; côté Blender, je recolore en **palette de 6 à 8 couleurs** (la texture Tripo est floue : tuiles en tache, arêtes qui bavent).

### Bloc commun « une image » (en anglais, à coller en tête)

```
Low-poly 3D game building asset, SMOOTH dense faceting: many small soft-beveled facets blending into rounded, sculpted
forms, chunky toy-like proportions with softly beveled edges, like a hand-made cozy low-poly game, NOT clay or putty-like,
NOT realistic or architectural-model-like. NOT large flat polygon planes, NOT a crystal or gem-cut look, no hard visible
triangle edges. Oversized chunky relief: big puffy roof tiles (each about 40 cm, few and large), big rounded stone blocks
(about 40 cm, few joints) in the arch, the chimney and the plinth, thick rounded timber beams, smooth plaster without
mottling.
FLAT UNLIT ALBEDO COLORS: solid clean color zones with crisp boundaries (brick-red tiles, cream plaster, dark brown
timber, light-grey stone, dark glass), NO cast shadows, NO baked ambient occlusion, NO light gradients, NO glossy
highlights, no photographic detail, no noise, no wood grain, no painted grime.
Half-timbered cottage whose walls stand directly on a stone plinth of exactly TWO low courses of rounded grey stone
blocks, the plinth following the footprint of the walls and only a few centimeters wider than them. NO wide platform,
NO paved terrace and NO large slab around the house: the plinth is the only foundation. In front of the doorway only
two small stone steps leading up to the threshold. The doorway is EMPTY: a dark recessed arched opening framed by large stone blocks, with NO door
leaf and NO door. Windows have simple wooden frames and glass panes and NO shutters. The roof is SOLID and CLOSED:
both slopes completely covered with tiles, no gaps, no holes, nothing see-through.
Three-quarter view from the front-right, camera about 30 degrees above the horizontal, no extreme perspective, the
facade with the doorway on the left of the picture and the right gable wall visible on its right. Single isolated
building centered in frame, entire building visible with margin, nothing cropped. Plain flat dark grey background
(#2b2b2e), no ground, no grass, no path, no fence, no trees, no props, no characters, no cast shadow on the
background. Soft even studio lighting from the front. Square image 1:1. No text, no labels, no UI, no watermark. The
only green light in the world is the emerald relic magic: no green anywhere.
```

### Descriptions par maison (à ajouter après le bloc commun)

- **Maison de base** : « A small single-storey cottage, 8.6 m wide and 7.1 m deep, steep 45-degree gabled roof of brick-red clay tiles, exactly ONE short stone chimney standing against the LEFT gable wall and rising above the ridge; the empty arched doorway on the front, set a little to the left; one window with a plain wooden frame to the right of the doorway; a small hanging lantern between them; a small round window under the roof peak in the right gable. »
- **Mécano** : « A shop with a tall front gable, 12.6 m wide and 9.6 m deep, two storeys at the front with two small windows in the front gable, brick-red clay tile roof; an empty arched doorway on the left of the ground floor and, to the right, a very large shop window 4 m wide with many small square panes; a bent grey metal stove pipe through the roof near the back and a small cog-shaped weathervane on the ridge; no shutters. »
- **Forge** : « A blacksmith workshop, 11.6 m wide and 9.6 m deep, dark brown clay tile roof, a very massive tall stone chimney on the back right of the roof; an empty arched doorway on the front left of center, a window with iron bars to its right; on the RIGHT side an open lean-to shed 5 m wide with no walls, a low sloping brown roof on thick wooden posts, sheltering a stone hearth with an orange fire, stacked logs. »
- **Druide** : « An herbalist cottage, 11.6 m wide and 8.6 m deep, very steep 50-degree gabled roof thickly covered with dull olive-green moss over brown tiles, a short crooked stone chimney, ivy (matte leaves, no glow) climbing the timber frame; an empty arched doorway on the front left of center, two windows with flower boxes of small yellow and purple flowers; on the RIGHT side a small open drying porch 2 m by 4 m with a lean-to roof on two posts and bunches of dried herbs hanging from the beams. »
- **Sorcier** : « A wizard's house made of a small half-timbered house and a round tower. The house is 11.6 m wide and 9.6 m deep, single storey, with a steep 45-degree solid closed roof of dark blue slate shingles (big chunky rounded shingles) and walls standing directly on a stone plinth of exactly TWO low courses of rounded grey stone blocks; an EMPTY arched doorway on the front, left of center, framed by large stone blocks with two small steps (NO door leaf); two windows with plain thick wooden frames to its right (NO shutters). Attached at the BACK RIGHT corner of the house stands a round tower of 4.5 m diameter, cream plaster with a stone plinth, taller than the roof (about 9 m high at the cone's base), topped by a tall conical roof of dark blue slate shingles ending in a crescent-moon weathervane, with a round porthole window with a thick stone frame and a narrow slit window. A short crooked chimney of chunky stone blocks on the house roof, on the LEFT gable. A small hanging lantern beside the doorway. The overall silhouette is asymmetrical: low wide house, tall slim tower rising behind it on the right. »
- **Taverne** (« Le Tonneau Percé », nom choisi par Quentin le 02/10/2026) : « A big tavern: a long single-storey great hall 16.6 m wide and 11.6 m deep, walls 5.4 m high, a very tall hipped roof with half-hips at about 40 degrees, big puffy brick-red clay tiles; a huge stone chimney of chunky blocks rising from the LEFT end wall; a wide EMPTY arched double doorway 3.2 m wide in the middle under a small tiled porch roof on two timber brackets, two steps (NO door leaves); a large window with a plain thick frame on each side of the doorway (NO shutters); dark timber framing with thick diagonal braces; walls on a plinth of exactly TWO low courses of rounded grey stone blocks, no platform. TWO wooden barrels: ONE barrel standing on the ground against the front wall to the LEFT of the doorway, about 1.2 m wide and 1.2 m high with two dark iron hoops; and ONE barrel sitting tilted on top of the small tiled porch roof above the doorway, wedged against the slope, same size, with a hole in its side facing the front and a short wooden tap stub, a small split stave, and a little amber-colored beer already trickling out of the hole and running down the tiles in a thin streak with a few round white foam blobs clinging to the barrel and the roof edge (modeled as small chunky white rounded bumps). A hanging tavern sign on a curved wrought-iron bracket fixed to the front wall to the right of the doorway at about 4.5 m height: a rounded wooden board of about 1.6 m by 1.0 m with a thick carved frame, and on it, painted as a simple flat picture, a barrel with a hole spouting a thin jet of beer; the board carries NO letters and NO text (the name is added in the engine). »

**Cohérence de style (02/10/2026)** : la première forge de Grok était trop fine et réaliste (petites tuiles plates, poutres minces, plâtre moucheté). **Toujours joindre l'image de la maison de base comme référence de style** dans Grok (« same visual style as the reference cottage ») : tuiles bombées de 40 cm, colombages épais, grosses pierres arrondies, plâtre lisse.

## Portes et volets (pièces animées, une image chacune, vue de face droite sans perspective)

À ajouter à la fin du bloc commun adapté : « Single door leaf / single pair of shutters, flat straight-on elevation, centered, nothing else; same smooth dense low-poly style, plain dark grey background ». Une image de face suffit (le dos se déduit ; demander si besoin une vue de dos pour la ferrure).

- **Battant de porte simple, 1,8 × 2,6 m, arrondi en haut** : planches verticales lisses brun chaud, deux bandes de fer horizontales avec rivets, anneau de fer en guise de poignée, sans cadre de pierre. Variantes : **sorcier** (croissant de lune et quelques symboles peints en bleu nuit), **druide** (feuillage sculpté en relief, un peu plus clair), **mécano** (petite plaque à engrenage), **forge** (planches renforcées de plaques de fer, plus sombre).
- **Double porte de la taverne, 3,2 × 2,8 m** : deux battants arrondis symétriques à planches et bandes de fer, un anneau chacun (une image du couple).
- **Paire de volets de fenêtre** (deux battants de 0,6 × 1,4 m, vus **ouverts à plat côte à côte** ou en deux images) : planches avec une barre en Z, charnières de fer apparentes ; teinte par maison (taverne brun, maison de base bleu-gris, druide vert sauge, sorcier bleu nuit ; forge et mécano sans volets).

**Côté Unity** : pivot sur le bord de la charnière, ouverture légère (environ 15 à 25° vers l'intérieur) quand le joueur utilise la porte, **son d'ouverture** ; **volets ouverts le jour, fermés la nuit** (rotation liée au cycle jour/nuit, avec la lumière chaude des vitres derrière). L'embrasure du modèle de maison doit être **plus profonde que le battant** (au moins 30 cm) pour qu'il tienne dedans en s'ouvrant.

## Accessoires (une image chacun, vue de trois quarts, bloc commun adapté « single prop »)

Puits à toit de bois (Ø 2 m), potager clôturé (6 × 5 m : choux, carottes, citrouilles, palissade basse), étendoir, tas de bois, enseignes (chope, engrenage et clé, fiole), caisses, tonneaux, banc, enclume sur billot, soufflet, bûches. Pour chacun : « single prop, not held, three-quarter view from slightly above, same smooth dense low-poly style ».

## Réglages Tripo (bâtiments)

- Onglet **Modèle** (multivue) : face en image principale, puis **Gauche**, **Droite**, **Dos**.
- **Modèle HD**, IA H3.1 ; **Générer par parties : à tester en « Équilibré » sur la maison de base** (Tripo sépare alors toit, murs, cheminée, vitres… : nombre et nature des pièces imprévisibles, à relever avant de généraliser ; la texture se fait ensuite avec l'outil Texture) ; à défaut **désactivé** (maillage d'un bloc, toit et vitres séparés ensuite dans Blender par leurs cotes). Les battants de porte et les volets **ne dépendent pas de ce réglage** : ce sont des pièces à part ; Qualité de maillage **Ultra** ; **Texture activée, 4K** ; **PBR désactivé** ; **Supprimer l'éclairage activé** ; **Triangle** ; **200 000 à 300 000 polygones** ; confidentialité privée ; export FBX avec texture dans `ArtSources/References/Decor/maison_<nom>_tripo/`.

## Réception côté Blender puis Unity

1. **Échelle** d'après la boîte englobante et les emprises du tableau de `maisons-plans.md` (pas de dimension métrique dans Tripo) ; origine au centre du dessous, façade vers -Y.
2. **Décimation** : maison de base 8 000 à 11 000 triangles, boutiques 12 000 à 16 000, taverne 20 000 à 25 000 ; normales lissées (faces lisses, arêtes dures aux vrais angles, Weighted Normal) ; **atlas 2048²** cuit depuis la haute définition ; collision simplifiée (boîtes ou maillage convexe).
3. **Porte et volets** : l'embrasure (1,8 × 2,6 m, ou 3,2 × 2,8 m pour la taverne) reste à la cote du plan ; le **battant** s'y pose, pivot à la charnière, ouverture légère et son à l'usage, puis fondu au noir vers la zone d'intérieur ; les **volets** se posent de part et d'autre de chaque fenêtre, fermés la nuit. Un vide d'au moins 30 cm derrière l'embrasure (fond sombre simple) évite de voir à travers.
4. Les toits et les fenêtres éclairées la nuit (émissif chaud, jamais vert) se règlent dans Unity ; prévoir de séparer le toit si on veut le masquer en intérieur.

## Vérifications avant d'envoyer à Tripo

Un seul bâtiment par image, entier, centré, fond uni, aucune ombre au sol ; **mêmes proportions, couleurs et détails sur les quatre vues** (même nombre de fenêtres, même cheminée, mêmes teintes) ; **aucun battant de porte ni volet** sur les maisons ; élévations sans perspective ; socle bas : dalle + soubassement de deux rangées de pierres au pied des murs, pas plus haut ; pas de vert luisant ; la façade est bien au bord droit de la vue « gauche » et au bord gauche de la vue « droite ».
