# Brief Grok → Tripo des maisons (02/10/2026)

Six bâtiments, **quatre vues chacun** (face, côté gauche, côté droit, dos) pour le mode **multivue de Tripo** (onglet **Modèle** : image principale = face ; emplacements **Gauche**, **Droite**, **Dos**), comme pour la Bavaroise v4 (`Docs/da/brief-grok-tripo.md`). Dimensions et intérieurs : `Docs/da/maisons-plans.md` et `taverne-plan.md`. Les extérieurs sont fermés (l'intérieur est une zone à part, porte et fondu au noir) ; les accessoires vont **chacun dans leur propre image** (jamais collés au bâtiment).

**Ordre conseillé** : maison de base (la plus simple, pour tester la chaîne) → mécano → forge → druide → sorcier → taverne (déjà faite par code et validée ; la version Tripo est facultative, à comparer).

## Règles pour les quatre vues

- **Même conversation Grok pour les quatre vues.** Génère d'abord la **face**, valide-la, puis redemande en joignant cette image : « *Same building, same colors, same proportions, same scale and camera height; now the LEFT side elevation* » (puis droite, puis dos). C'est ce qui garde le même bâtiment sous quatre angles.
- **Élévations droites, sans perspective** (orthographiques), **caméra à mi-hauteur des murs, parfaitement horizontale**, bâtiment **entier et centré avec de la marge** (du socle au sommet de la cheminée), **même échelle dans les quatre images** (même hauteur de bâtiment en pixels).
- **Repères** : « face » = la façade à la porte, tournée vers Nyxessa. « Gauche » = le côté qui est à votre **gauche quand vous faites face à la porte, vu de l'extérieur** : sur cette vue, la **façade est au bord DROIT** de l'image. « Droite » = l'autre côté : la **façade est au bord GAUCHE**. « Dos » = le mur opposé à la porte (la gauche du bâtiment est alors à droite de l'image).
- Format **1:1**, fond uni gris foncé, un seul bâtiment, rien autour.

## Bloc commun (en anglais, à coller en tête de chaque demande)

```
Low-poly 3D game building, SMOOTH dense faceting: many small soft-beveled facets blending into rounded, sculpted
forms, chunky toy-like proportions with softly beveled edges, like a hand-made cozy low-poly game. NOT large flat
polygon planes, NOT a crystal or gem-cut look, no hard visible triangle edges. Matte, clean simple surfaces: no wood
grain, no stone masonry texture, no photographic detail, no noise, no painted grime.
Half-timbered cottage: smooth cream plaster walls, dark red-brown timber frame, clay-tile roof, stone chimney, an
arched plank door framed by large plain light-grey stone blocks, windows with shutters, standing on a LOW, smooth,
plain light-grey stone slab that overhangs the wall slightly, with a couple of steps in front of the door (never on a
high masonry base).
Scale cues: the plank door is 1.8 m wide and 2.6 m tall, the walls are 4.4 m high, a chunky big-headed 2.3 m
character would be almost as tall as the door.
Single isolated building centered in frame, entire building visible with margin, nothing cropped. Plain flat dark grey
background (#2b2b2e), no ground, no grass, no path, no fence, no trees, no props around it, no characters, no cast
shadow. Soft even studio lighting from the front, no strong rim light, no dramatic shadows. Square image 1:1. No text,
no labels, no UI, no watermark. The only green light in the world is the emerald relic magic: no green glow anywhere.
```

Phrase de vue à ajouter à la fin (la **description de la maison** vient avant) :

- **Face** : « Straight-on orthographic FRONT elevation of the facade with the door, camera at mid-wall height looking exactly horizontally, no perspective distortion. »
- **Gauche** : « Straight-on orthographic LEFT SIDE elevation (the wall on your left when you face the front door from outside), same scale and same camera height as the front view; the front facade with the door is at the RIGHT edge of the picture. »
- **Droite** : « Straight-on orthographic RIGHT SIDE elevation (the wall on your right when you face the front door from outside), same scale and same camera height as the front view; the front facade with the door is at the LEFT edge of the picture. »
- **Dos** : « Straight-on orthographic BACK elevation, the wall opposite the front door, same scale and same camera height as the front view; no door on this wall. »

---

## 1. Maison de base (8,6 × 7,1 m, murs 4,4 m, faîtage environ 8 m)

La maison standard du jeu : la plus simple, **elle sert de gabarit aux maisons d'habitants anonymes**. Puits, potager clôturé, étendoir et tas de bois sont des **accessoires à part** (voir plus bas).

```
A small single-storey half-timbered cottage, 8.6 m wide and 7.1 m deep, steep 45-degree gabled roof of big smooth
clay tiles in warm brick red with a visible ridge and a short stone chimney; a single arched plank door on the front,
set a little to the left, with two steps; one window with blue-grey shutters to the right of the door; a small round
window under the roof peak in each gable; one window on the back wall; one window on each side wall; a small
hanging lantern between the door and the window.
```
Dos : une fenêtre, pas de porte. Côtés : pignons avec la petite fenêtre ronde sous le faîte et une fenêtre, cheminée sur le pignon gauche.

## 2. Boutique du mécano (12,6 × 9,6 m, un étage en façade, faîtage environ 9,5 m)

```
A shop with a street-facing gable, 12.6 m wide and 9.6 m deep, two storeys at the front: a tall front gable with a
second floor with two small windows, a steep 45-degree gabled roof of clay tiles in warm brick red, cream plaster
and dark timber frame; on the ground floor a plank door on the left (door 1.8 m wide) and, to the right of it, a very
large shop window 4 m wide with many small square panes and a wooden sill; a bent grey metal stove pipe goes
through the roof near the back, with a small cog-shaped weathervane on the ridge; shutters in slate grey-blue; one
window on the back wall, one on each side wall at ground level, an upper window in each gable.
```
Accessoires à part : enseigne en forme d'engrenage et de clé croisés sur potence de fer, caisses de bois empilées.

## 3. Forge (11,6 × 9,6 m + appentis ouvert 5 × 7 m côté droit)

```
A blacksmith workshop, 11.6 m wide and 9.6 m deep, half-timbered with cream plaster darkened with a little soot
near the chimney, a roof of clay tiles in dark brown, a very massive tall stone chimney on the back right of the
roof; a plain arched plank door on the front, left of center, and one barred window with iron bars and no shutters
to its right; on the RIGHT side of the building an open lean-to shed, 5 m wide and 7 m deep, with no walls, a low
sloping brown roof held by thick wooden posts, sheltering a stone hearth with an orange fire, a stack of logs and a
low wall; one barred window on the back wall and one on the left side wall.
```
Accessoires à part : enclume sur billot, tas de bûches, soufflet, roue de meule, outils accrochés. (Le foyer est dans l'appentis : orange chaud, jamais vert.)

## 4. Boutique du druide (11,6 × 8,6 m + auvent 2 × 4 m côté droit, toit à 50°)

```
An herbalist cottage, 11.6 m wide and 8.6 m deep, half-timbered with cream plaster, a very steep 50-degree gabled
roof covered with thick soft olive-green moss over brown tiles, a short crooked stone chimney, green ivy climbing
over parts of the timber frame and the front gable (matte leaves, no glow); an arched plank door on the front, left
of center, and two windows with flower boxes full of small yellow and purple flowers under them; on the RIGHT side of
the building a small open drying porch, 2 m by 4 m, with a lean-to roof on two posts and bunches of dried herbs
hanging from the beams; one window on the back wall and one on the left wall.
```
Accessoires à part : enseigne en forme de fiole ambre, jardinières, claie de séchage, bacs de champignons.

## 5. Maison du sorcier (11,6 × 9,6 m + tour ronde Ø 4,5 m)

```
A wizard's house, 11.6 m wide and 9.6 m deep, half-timbered with cream plaster, a gabled roof of dark blue slate
shingles, and an attached round stone-and-plaster tower of 4.5 m diameter standing at the BACK RIGHT corner of the
house, taller than the roof, with a tall conical dark blue slate roof topped by a crescent-moon weathervane, a round
porthole window and a small slit window; an arched plank door on the front, left of center, with a crescent moon and
a few simple symbols painted in dark blue on it, and two windows with deep blue shutters; one window on the back
wall, a window on the left wall.
```
Accessoires à part : lanterne à vitre bleutée, cloche de verre (l'éclat de Nyx est à l'intérieur : jamais dessiné ici). Le hublot de la tour est pâle et « qui luit » seulement en blanc-bleu, **pas de vert**.

## 6. Taverne (16,6 × 11,6 m, murs 5,4 m, faîtage 10,7 m ; déjà construite par code)

```
A big tavern: a very long single-storey great hall, 16.6 m wide and 11.6 m deep, with walls 5.4 m high and a very tall
hipped roof with half-hips (the gable ends are cut by a small hip) at about 40 degrees, clay tiles in warm brick red; a
huge stone chimney rising from the LEFT end wall; on the front, in the middle, a pair of wide arched plank double doors
(3.2 m wide) in a frame of large grey stone blocks, under a small tiled porch roof on two timber brackets, two steps;
on the front a large window with brown shutters on each side of the doors; one window on each side wall on the
left and right ends; plenty of dark timber framing with diagonal braces; the back wall has timber framing and no
door.
```
Accessoires à part : enseigne en forme de chope sur potence de fer, deux tonneaux et un banc, lanternes d'applique. Référence visuelle : `sandbox-level/Assets/Screenshots/taverne_ext_*.png` (hors dépôt).

---

## Accessoires (une image chacun, vue de trois quarts, bloc commun adapté « single prop »)

Puits à toit de bois (Ø 2 m), potager clôturé (6 × 5 m : choux, carottes, citrouilles, palissade basse), étendoir, tas de bois, enseignes (chope, engrenage et clé, fiole), caisses, tonneaux, banc, enclume sur billot, soufflet, bûches. Pour chacun : « single prop, not held, three-quarter view from slightly above, same smooth dense low-poly style ».

## Réglages Tripo (bâtiments)

- Onglet **Modèle** (multivue) : face en image principale, puis **Gauche**, **Droite**, **Dos**.
- **Modèle HD**, IA H3.1 ; **Générer par parties : désactivé** (bâtiments d'un bloc ; sinon la texture se fait ensuite avec l'outil Texture) ; Qualité de maillage **Ultra** ; **Texture activée, 4K** ; **PBR désactivé** ; **Supprimer l'éclairage activé** ; **Triangle** ; **200 000 à 300 000 polygones** ; confidentialité privée ; export FBX avec texture dans `ArtSources/References/Decor/maison_<nom>_tripo/`.

## Réception côté Blender puis Unity

1. **Échelle** d'après la boîte englobante et les emprises du tableau de `maisons-plans.md` (pas de dimension métrique dans Tripo) ; origine au centre du dessous, façade vers -Y.
2. **Décimation** : maison de base 8 000 à 11 000 triangles, boutiques 12 000 à 16 000, taverne 20 000 à 25 000 ; normales lissées (faces lisses, arêtes dures aux vrais angles, Weighted Normal) ; **atlas 2048²** cuit depuis la haute définition ; collision simplifiée (boîtes ou maillage convexe).
3. **Porte** : la façade est fermée ; l'emplacement de la porte (1,8 × 2,6 m, ou 3,2 × 2,8 m pour la taverne) doit rester à la cote du plan pour la zone d'intérieur (porte et fondu au noir) : à vérifier au placement.
4. Les toits et les fenêtres éclairées la nuit (émissif chaud, jamais vert) se règlent dans Unity ; prévoir de séparer le toit si on veut le masquer en intérieur.

## Vérifications avant d'envoyer à Tripo

Un seul bâtiment par image, entier, centré, fond uni, aucune ombre au sol ; **mêmes proportions, couleurs et détails sur les quatre vues** (même nombre de fenêtres, même cheminée, mêmes teintes) ; élévations sans perspective ; socle bas et lisse, pas de maçonnerie ; pas de vert luisant ; la façade est bien au bord droit de la vue « gauche » et au bord gauche de la vue « droite ».
