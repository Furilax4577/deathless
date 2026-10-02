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

### Réception réalisée (02/10/2026) : `Forge.prefab`

Pipeline : `maison_pipeline.py --piece forge` (méthode générale et autres pièces : `brief-maisons.md` § Réception réalisée). Résultat : **14 192 triangles**, atlas 2048², un matériau (`Forge.mat`), collision de 346 triangles, palette 1 (brun unique tuiles et bois, crème, pierre ; variante 0 gardée), 7 ancres, captures `Assets/Screenshots/forge_*.png`, banc `Assets/Scenes/Dev/MaisonBanc.unity`.

**Échelle (compromis).** Le modèle n'est pas homothétique au plan : plus profond (0,75 contre 0,55), maison plus étroite et atelier plus large, foyer et cheminée beaucoup plus gros. Trois options essayées (rendus `ArtSources/References/Decor/forge_tripo/rendus/option_{A,B,C}_{trois_quarts,dessus}.png`) :
- **A** : uniforme, largeur 17,4 m : profondeur 13,1 m (maison 11,5 m de soubassement au lieu de 9,6 m), hauteur 14 m.
- **B (retenue)** : largeur 17,4 m, **profondeur x 0,87**, hauteur gardée : 17,4 × 11,4 × 14,0 m ; soubassement de la maison 10,0 × 9,9 m (plan 11,6 × 9,6 m), atelier 7 m de large sur 10,4 m de profondeur (plan 5,8 × 9,6 m). Distorsion de 13 % sur la profondeur seulement, invisible sur les pierres et les tuiles.
- **C** : uniforme, profondeur 11 m : largeur 14,6 m (maison 8,3 m de large) : trop étroite.
La hauteur reste celle du modèle (égout de la maison à 6,2 m, faîtage 12,1 m, cheminée 14 m), nettement au-dessus du plan (murs 4,4 m).

**Ce que le modèle contient, repère du plan (origine au coin sud-ouest du soubassement de la maison, x est, y nord, m)** : maison x 0 à 10, y 0 à 9,9 ; porte (cadre de pierre sur le pignon sud) ouverture x 2,65 à 4,82 ; fenêtre à barreaux x 6 à 10 (sur la façade) ; atelier x 10 à 16,9 ; **foyer de pierre massif de 3,75 m (est-ouest) sur 4,9 m (nord-sud), x 10,0 à 13,75, y 4,0 à 8,9, dont la bouche regarde le SUD** (centre x = 12,5, face sud y = 3,8, hauteur de la voûte 1,8 à 4,1 m) ; cheminée qui monte contre le mur de la maison ; **deux poteaux seulement**, côté est : x 16,0 à 16,5, aux y 2,7 à 3,5 et 8,7 à 9,2 ; toit de l'atelier à une pente : sous-face à 4,4 m au plus bas (bord est) et jusqu'à 7 m au contact de la maison, **sans rien en dessous**, couvrant y 2,6 à 10,1 : la bande sud de l'atelier (y 0,6 à 2,6) est découverte ; sol de dalles de 0,36 m de haut.

**Sol de l'atelier** : il est « à même le sol » dans le modèle mais haut de 0,36 m (marche infranchissable pour un `CharacterController` de pas 0,3 m). **Tout le modèle est enfoncé de 0,34 m** (dessus du sol de l'atelier à z = 0,02 m) : le sol de l'atelier est donc au niveau du terrain et reste **marchable sans collision** (aucun collider sous les postes) ; le soubassement de la maison et les marches de la porte sont enfoncés d'autant.

**Écart avec le brief, dit franchement** : le foyer du modèle fait 3,75 × 4,9 m (plan : 2,2 × 2,4 m) et sa bouche regarde le sud (plan : à l'est de la bande, forgeron à l'est du foyer). **La bande libre de 2,8 m (x 11,6 à 14,4, du sud au nord) n'existe donc que pour y < 4,0 m** : plus au nord elle est prise par le foyer (le rectangle rouge de `forge_dessus_atelier.png`). Ce qui reste libre : (a) la **bande sud**, devant le foyer et la bouche : x 10,6 à 15,8, y 0,6 à 3,7 (5,2 × 3,1 m, sans poteau ni volume ni toit) ; (b) la **bande est** : x 14,1 à 15,8, y 3,7 à 9,0, entre la face est du foyer (x 13,75) et les poteaux (x 16,0), 2,25 m de passage entre le foyer et les poteaux (les deux poteaux sont aux extrémités nord et sud de cette bande, pas dans son axe). Le pipeline le **vérifie** (`verifier_zones` : points de la collision dans ces zones de 0,05 à 3 m de haut) : **0 point sur 3 276 (bande sud) et 0 sur 1 848 (bande est)**. La collision (8 enveloppes convexes : corps, toit de la maison, marches, foyer, souche de cheminée, toit de l'atelier, deux poteaux) n'occupe ni le sol de l'atelier ni l'espace en dessous du toit de l'atelier.

**Ancres (repère du prefab : racine au sol au centre de l'emprise maison + atelier, façade vers +Z ; chaque ancre a +Z local = direction du regard ou de la façade)** : conversion du plan (x_p, y_p) vers le prefab : `x_b = x_p - 8,373`, `y_b = y_p - 5,025` (Blender, x est, y nord), puis Unity local = (-x_b, z, -y_b) ; `Forge_Ancres.json` donne `origine_plan` et toutes les positions. Les ancres du brief ont été **déplacées** pour tenir compte du foyer réel (plan, m) :
| Ancre | Brief | Réalisé | Regard |
|---|---|---|---|
| `Foyer_Feu` | bouche | (12,5 ; 4,4 ; z 1,7) dans la bouche, face sud | vers le sud |
| `Poste_Chauffe` | (14,7 ; 7,6) | (12,5 ; 2,7) à 1,1 m devant la bouche | vers le nord (le foyer) |
| `Enclume_Ancre` | (15,2 ; 4,6) | (14,5 ; 1,7), sol | vers le nord |
| `Poste_Frappe` | (14,0 ; 4,6) | (14,5 ; 2,9) | vers l'enclume (sud) |
| `Bac_Ancre` | (15,2 ; 1,6) | (14,8 ; 5,6), grand côté nord-sud | vers le nord |
| `Poste_Trempe` | (14,0 ; 1,6) | (15,8 ; 5,6) | vers le bac (ouest) |
| `Entree` | — | centre de l'embrasure, devant les marches, déclencheur 1,8 × 2,6 × 1,6 m | +Z (sud) |
| `Porte_Pivot` | — | charnière gauche de l'embrasure (vue de l'extérieur), à 0,5 m du seuil | +Z |

Écarts entre postes : Chauffe-Frappe 2,5 m, Frappe-Trempe 3,0 m ; au moins 1,5 m libres autour de chaque poste. Pas de `Volet_*` (fenêtres sans volets). Le banc pose des repères gris à l'échelle (enclume sur billot de 1 m, bac de 1,6 × 0,8 × 0,85 m) et un héros à chaque poste.

**Captures** : `forge_trois_quarts`, `_face`, `_dos`, `_profil_droit`, `_arche`, `_ancres` (zone d'entrée en rouge, charnière en cyan), `_dessus_atelier` (vue du dessus, toit coupé à 3,8 m : bande du brief en rouge, zones libres vérifiées en vert, postes en jaune, enclume et bac en cyan, bouche du foyer en orange), `_dessus_toit`, `_jeu_3e_personne` (devant la porte), `_jeu_atelier` (devant l'atelier, au poste de frappe), `_foyer`, `_palettes_0_1`, `_tripo_nuit`, `_nuit_foyer`. Le feu est un effet moteur (`ForgeFeu`) à poser sur `Foyer_Feu` ; sans lui la bouche est sombre.
