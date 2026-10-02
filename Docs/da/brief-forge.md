# Brief Grok → Tripo de la forge (02/10/2026)

La forge = **une maison (2/3) + un atelier extérieur ouvert (1/3)** où le forgeron travaille : un **foyer** contre le mur de la maison, une **enclume** et un **bac de trempe**. **L'enclume et le bac sont des pièces séparées, repositionnables** (accessoires à part, posés sur des ancres) ; le **feu du foyer est un effet moteur** (`ForgeFeu`), pas du modèle. Plan à l'échelle : `Docs/da/gabarits/forge-plan.png` (`python Docs/outils/forge_plan.py`). Méthode générale : `Docs/da/brief-maisons.md` § « Mode une seule image » (image trois-quarts unique par pièce, couleurs plates sans ombres, soubassement de deux rangées de pierres, pas de plateforme, embrasure vide, pas de volets, toit fermé) et `Docs/da/brief-grok-tripo.md`.

## Plan (mètres ; x vers l'est, y vers le nord, origine au coin sud-ouest de la maison ; façade au sud)

| Élément | Emprise | Note |
|---|---|---|
| Maison | 11,6 × 9,6 m (les 2/3) | porte 1,8 m, intérieur = zone à part (fondu au noir), toit à 45° brun sombre, murs 4,4 m |
| Atelier ouvert | 5,8 × 9,6 m, à l'est (le tiers) | **sans murs**, toit bas à une seule pente (sablières à 3,2 m côté ouvert, 4,0 m côté maison) sur quatre poteaux d'angle ; sol de dalles plates à même le sol (pas de plateforme) |
| Foyer | 2,2 × 2,4 m, contre le mur est de la maison, y 6,4 à 8,8 | pierre massive, hotte, **cheminée qui monte le long du mur de la maison** ; **bouche du foyer vide et sombre : pas de feu, pas de braise** (le feu est un effet) |
| Enclume sur billot | centre (15,2 ; 4,6) | **pièce séparée** ; sommet à 1,0 m |
| Bac de trempe | centre (15,2 ; 1,6), 1,6 × 0,8 m, grand côté nord-sud | **pièce séparée** ; bord à 0,85 m |
| Bande libre du forgeron | x de 11,6 à 14,4 (2,8 m de large) | aucun meuble dedans ; **poteaux d'angle à l'extérieur des postes** (x ≥ 16,8 côté est, x = 14,2 côté nord et sud au milieu : jamais dans la bande) |
| Place des 4 joueurs | devant l'atelier, côté sud | hors de l'atelier |

**Circulation** : le forgeron se tient dans la bande libre, aux **trois postes alignés**, séparés d'environ 3 m : **Chauffe** (14,7 ; 7,6) face au foyer, **Frappe** (14,0 ; 4,6) face à l'enclume, **Trempe** (14,0 ; 1,6) face au bac ; passage libre d'au moins 1,5 m autour de chaque poste, aucun obstacle entre eux. **Animations à venir** (chauffe, tape, trempe, puis retour) : chaque poste est un `Transform` d'ancre (`Poste_Chauffe`, `Poste_Frappe`, `Poste_Trempe`) calé **relativement à son accessoire** (la pièce enclume ou bac porte sa propre ancre « poste du forgeron » : si Quentin la déplace, le poste suit) ; sorties de gestes : enclume (étincelles), bac (vapeur), foyer (lueur) : effets moteur.

## Bloc commun (en anglais, identique à `brief-maisons.md` § Mode une seule image ; à coller en tête de chaque demande)

Reprendre le bloc « une image » de `Docs/da/brief-maisons.md` tel quel, **sauf** pour la forge (voir ci-dessous) où l'on remplace le paragraphe du soubassement par celui qui suit.

## 1. Prompt de la forge (maison + atelier, à vide)

```
Low-poly 3D game building asset, SMOOTH dense faceting but crisp: many small soft-beveled facets, chunky toy-like
proportions, like a hand-made cozy low-poly game, NOT clay or putty-like, NOT large flat polygon planes, no hard
visible triangle edges. Clearly modeled relief: individual dark-brown clay roof tiles, individual stone blocks in the
chimney, the hearth and the arch, thick timber beams.
FLAT UNLIT ALBEDO COLORS: solid clean color zones with crisp boundaries (dark-brown tiles, cream plaster slightly
darkened with soot near the chimney, dark brown timber, light-grey stone, dark iron), NO cast shadows, NO baked ambient
occlusion, NO light gradients, no glossy highlights, no photographic detail, no noise, no wood grain, no grime.

A blacksmith forge made of two parts side by side. LEFT two thirds: a small half-timbered house, 11.6 m wide and
9.6 m deep, single storey, steep 45-degree solid closed roof of dark-brown clay tiles, walls standing directly on a
stone plinth of exactly TWO low courses of rounded grey stone blocks, an EMPTY arched doorway on the front, left of
center, framed by large stone blocks with two small steps (NO door leaf), one window with a plain wooden frame and iron
bars to its right (NO shutters). RIGHT third: an OPEN workshop, 5.8 m wide and 9.6 m deep, attached to the east side of
the house, with NO walls at all, a low single-slope brown tile roof held by four thick wooden posts at its four outer
corners, and a flat floor of grey flagstones lying directly on the ground (NO raised platform, NO terrace around the
building). Against the house's east wall, inside the workshop, stands a massive stone hearth 2.2 m wide with a wide
stone hood, and a very tall massive stone chimney rises from it along the outside of the house wall above the roof; the
hearth opening is EMPTY and dark inside: NO fire, NO flames, NO embers, NO coals, NO smoke. The rest of the workshop
floor is completely EMPTY and clear: NO anvil, NO tub, NO barrel, NO logs, NO tools, NO racks, NO furniture, NO props of
any kind. The only chimney of the building is the one of the hearth.

Three-quarter view from the front-right, camera about 30 degrees above the horizontal, no extreme perspective: the house
facade with the doorway on the left of the picture and the open workshop on the right, fully visible with its floor and
the hearth. Single isolated building centered in frame, entire building visible with margin, nothing cropped. Plain flat
dark grey background (#2b2b2e), no ground, no grass, no path, no fence, no trees, no props, no characters, no cast
shadow on the background. Soft even studio lighting from the front. Square image 1:1. No text, no labels, no UI, no
watermark. The only green light in the world is the emerald relic magic: no green anywhere.
```

## 2. Prompt de l'enclume (pièce séparée)

```
Low-poly 3D game prop, crisp smooth-faceted, chunky toy-like, NOT clay. FLAT UNLIT ALBEDO COLORS: solid clean color
zones (dark iron grey for the anvil, warm brown for the stump), NO cast shadows, NO baked ambient occlusion, NO light
gradients, no glossy highlights, no noise.
A blacksmith's anvil standing on a thick round oak stump: the stump is 0.8 m in diameter and 0.45 m high with a flat
top and visible rim, the anvil is 1.1 m long and 0.55 m high with a flat top, a tapered horn on the left and a small
square step on the right. The total height is 1.0 m. NO hammer, NO tongs, NO sparks, NO glowing metal, NOTHING on the
anvil. Single isolated prop centered in frame, entire prop visible with margin, three-quarter view from slightly above
(about 25 degrees). Plain flat dark grey background (#2b2b2e), no ground, no shadow. Soft even studio lighting.
Square image 1:1. No text.
```

## 3. Prompt du bac de trempe (pièce séparée)

```
Low-poly 3D game prop, crisp smooth-faceted, chunky toy-like, NOT clay. FLAT UNLIT ALBEDO COLORS: solid clean color
zones (warm brown wood, dark iron bands, dark blue water), NO cast shadows, NO baked ambient occlusion, NO light
gradients, no glossy highlights, no noise.
A blacksmith's quenching trough: a low rectangular wooden tub made of thick vertical planks bound by two dark iron
bands, 1.6 m long, 0.8 m wide and 0.85 m high, filled with dark blue water whose flat surface lies about 10 cm below
the rim. NO steam, NO bubbles, NO ripples, NO sword or object in it, NOTHING on top. Single isolated prop centered in
frame, entire prop visible with margin, three-quarter view from slightly above (about 25 degrees). Plain flat dark
grey background (#2b2b2e), no ground, no shadow. Soft even studio lighting. Square image 1:1. No text.
```

## 4. Accessoires facultatifs (une image chacun, même gabarit que ci-dessus)

Soufflet à main, tas de bûches (1,2 × 0,8 m), râtelier à épées et haches (2 m), établi à outils (1,6 m), meule à aiguiser (0,8 m), seau, panneau-enseigne en forme d'enclume sur potence de fer, tonneau d'eau. Chacun : « single prop, not held, three-quarter view from slightly above, crisp smooth-faceted, flat unlit colors, no shadows ».

## Réglages Tripo et réception Blender/Unity

- **Forge** : onglet **Image** (une seule image), Modèle HD, IA H3.1, « Générer par parties » **désactivé**, Qualité Ultra, Triangle, 200 000 à 300 000 polygones, PBR désactivé, **puis l'outil Texture** en 4K ; export FBX avec texture dans `ArtSources/References/Decor/forge_tripo/`. **Enclume et bac** : mêmes réglages, 100 000 polygones, texture 2K, dans `forge_enclume_tripo/` et `forge_bac_tripo/`.
- **Pipeline Blender** : `ArtSources/Decor/Maisons/maison_pipeline.py` (échelle d'après la largeur réelle : forge 17,4 m, enclume 1,1 m, bac 1,6 m ; décimation forge 14 000 à 18 000 triangles, props 1 500 à 3 000 ; atlas 2048² ; palette ; collision simplifiée). **La bande libre de 2,8 m doit rester libre dans le modèle** (collision du toit et des poteaux hors de cette bande).
- **Prefabs** : `Forge.prefab` (maison + atelier + foyer, collision, `Entree` pour la zone d'intérieur, ancre du feu `Foyer_Feu` à la bouche du foyer pour `ForgeFeu`, ancres `Enclume_Ancre` et `Bac_Ancre`, ancres `Poste_Chauffe/Frappe/Trempe`, ancres `Porte_Pivot` et `Volet_*` comme les autres maisons), `Enclume.prefab` et `Bac_Trempe.prefab` (pivot au centre du dessous, chacun avec son ancre « poste du forgeron » à 1,2 m).
- **À vérifier avant d'envoyer à Tripo** : atelier **vide**, aucun feu peint, aucune plateforme, le **hublot, la porte et les volets absents**, une seule cheminée, le toit de l'atelier **ne couvre rien d'autre** que les trois postes.
