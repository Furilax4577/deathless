# Guide de style KayKit, chiffré

Étude du 26/09/2026 (demande de Quentin : « les arbres et les sols ne sont pas assez KayKit friendly ; la maison est cheap, on peut faire largement mieux »). Le but est de remplacer peu à peu les assets KayKit par les nôtres sans que la différence se voie à l'œil, puis de faire mieux. Ce guide donne des règles **directement applicables par un générateur**. Il a été appliqué à une maison générée (dernière partie).

Tout a été mesuré dans `sandbox-level` (scène `Assets/Scenes/StyleKayKit.unity`). Les chemins d'images ci-dessous sont relatifs à ce fichier.

## 1. Ce qui a été mesuré

| Modèle | Pack | Taille (échelle d'import) | Triangles | Pièces (îlots) |
|---|---|---|---|---|
| `building_home_A` | Medieval Hexagon | 0,79 x 0,93 x 0,85 | 1 011 | 20 (médiane 24 tri) |
| `building_home_B` | Medieval Hexagon | 0,88 x 1,28 x 1,10 | 1 393 | 44 (médiane 10 tri) |
| `building_barracks` | Medieval Hexagon | 1,44 x 1,64 x 1,57 | 4 007 | 96 (médiane 12 tri) |
| `building_market` | Medieval Hexagon | 1,80 x 0,98 x 1,32 | 3 125 | 97 |
| `building_tower_A` | Medieval Hexagon | 0,99 x 2,19 x 1,15 | 2 138 | 67 |
| `barrel`, `crate_A_big` | Medieval Hexagon | 0,2 | 240, 132 | 2, 1 |
| `house`, `mill`, `watchtower` | Medieval Builder 1.0 (archive de Relic) | 1,7 (tuile de carte) | 1 666, 1 906, 1 871 | 5 matériaux plats |
| `Tree_1_A` .. `Tree_4_A` | Forest Nature | 2,0-3,2 de large, 3,5-5,3 de haut | 336-978 | 5-7 |
| `Bush_1_A`, `Bush_2_A`, `Rock_1_A` | Forest Nature | 0,2-0,6 | 72, 44, 48 | 1 |
| `floor_tile_large`, `floor_wood_large`, `floor_dirt_large` | Dungeon | 4 x 4 x 0,15 | 188, 272, 104 | 1, 1, 9 |
| `wall_doorway` (+ porte) | Dungeon | 4 x 4 x 1 | 448 + 620 | 13 + 6 |
| `Knight`, `Rogue` | Adventurers 2.0 | 2,3-2,4 de haut (pose de repos) | 5 800, 7 562 | 1 par membre |

Dans le village, les maisons sont posées à **x7,5** (porte de 2,1-2,2 m pour un personnage de 2,3 m). Toutes les cotes « au village » ci-dessous sont à cette échelle.

Outils (dans `sandbox-level/Assets/StyleKayKit/Editor/`) :
- `AnalyseKayKit.Rapport(chemins)` : triangles, îlots, ombrage (coins plats, arêtes vives ou lissées et leur angle dièdre), UV (cases de l'atlas, plage de v, corrélation hauteur/v, dessus contre côtés), couleurs haut et bas de chaque case. `RapportObjet(nom)` fait la même chose sur un objet de la scène.
- `Mesures.Profil(...)` (lancers de rayons) et `Mesures.Coupe(...)` (coupe plane du maillage, dessinée en PNG).
- `EtudeStyle.Construire()` (scène d'étude), `EtudeCaptures.Plans()` et `Finales()` (toutes les captures), filaire et rendu « atlas seul » (sans lumière).

![Ensemble](../../sandbox-level/Assets/Screenshots/etude_vue_ensemble.png)

## 2. Ce qui fait le style KayKit, en chiffres

### 2.1 Proportions (maisons)

Coupes de `building_home_A` à l'échelle du village :

![Coupes verticales de home_A](../../sandbox-level/Assets/Screenshots/etude_coupe_home_A_verticale.png)
![Coupes horizontales à 1,6 m](../../sandbox-level/Assets/Screenshots/etude_coupe_horizontale.png)

| Cote (home_A, x7,5) | Valeur | Rapport |
|---|---|---|
| Emprise des murs (nu de l'enduit) | 3,7 x 4,7 m | |
| Haut des murs (sablière) | 3,15 m | |
| Égout du toit | 2,9 m | |
| Faîtage | 5,5 m | le toit fait **47 %** de la hauteur (hors cheminée) |
| Pente du toit | **45°** (1 pour 1) | home_B : 45° aussi |
| Débord à l'égout | **0,72 m** | 13 % de la largeur hors tout |
| Débord au pignon (planche de rive) | **0,53 m** | |
| Largeur hors tout / hauteur | 5,94 / 6,98 = **0,85** (0,97 sans cheminée) | la maison est un cube coiffé |
| Porte (baie) | **1,16 x 2,2 m** | 0,95 fois la taille du personnage, 0,5 fois en largeur ; **70 %** de la hauteur du mur |
| Cadre de porte | 1,58 x 2,36 m, **saillie 0,26 m** | le vantail est 0,15 m en retrait du cadre, pas du mur |
| Fenêtres (cadre) | 1,05 x 1,31 m, **saillie 0,26 m**, cintrées, appui en bois | |
| Colombages, sablières, bandeaux | **saillie 0,26-0,27 m**, raccords au mur en **chanfrein à 45°** | |
| Pierres d'angle du socle | 0,5-0,6 m, saillie 0,27 m | |
| Personnage (Knight) | 2,3-2,4 m ; **la tête fait 46 %** de la hauteur | |

À retenir : **gros volumes, peu nombreux, en fort relief** (0,26 m, soit 1/12 de la hauteur du mur), toit à 45° qui fait la moitié de la silhouette, porte et fenêtres surdimensionnées pour des personnages à grosse tête. Les murs sont des volumes pleins : pas d'épaisseur de mur, pas d'embrasure creusée ; la profondeur vient des cadres en saillie.

### 2.2 Biseaux

- **Un seul chanfrein à 45°** sur les arêtes vues, de largeur à peu près constante au village : **0,08-0,10 m** (0,011-0,013 à l'échelle d'import), soit **6-10 % de la plus petite dimension** d'une grosse pièce (cheminée de home_A : 0,1 m sur 1,58 m ; cadre de fenêtre : 0,08 m sur 1,05 m).
- Planchers du Dungeon Pack : chanfrein de 0,03 m sur des lames de 0,5 m (6 %), rainure en V de 0,04-0,05 m entre les dalles.
- Les petites pièces répétées (pierres du mur `wall`, 10 triangles) n'ont **pas** de biseau : leurs faces sont bombées (éventail de 4 triangles vers un centre légèrement sorti), ce qui suffit à la lecture.
- Arêtes des planches de toit, des rives, des poutres : **vives**, mais chaque face d'une même poutre a un ton différent (voir 2.5) : c'est la couleur qui « arrondit ».

### 2.3 Densité

- Maison : **1 000-1 400 triangles** (caserne, marché : 3 000-4 000). **20 à 100 pièces**, 10 à 25 triangles par pièce en médiane.
- Le détail va **à la silhouette** : planches de toit de longueurs différentes qui dépassent en escalier à l'égout, rives qui se croisent au faîtage, lucarne, cheminée à chapeau, pierres d'angle, cadres en saillie. Les grandes faces (enduit, pans de toit) sont **d'un seul tenant, en longs triangles fins** (voir le filaire).
- Les arrondis (arcs de porte et de fenêtre) ont **7 segments** pour un demi-cercle.
- Arbres : 336-978 triangles ; buissons : 44-72 ; rochers : 48. Personnages : 5 800-7 600 (c'est là que va la densité).

![Filaire home_A](../../sandbox-level/Assets/Screenshots/etude_A_trois_quarts_filaire.png)
![Filaire home_B](../../sandbox-level/Assets/Screenshots/etude_B_trois_quarts_filaire.png)

### 2.4 Ombrage (normales)

| | Coins plats | Arêtes lissées | Arêtes vives |
|---|---|---|---|
| Maisons | 56-75 % | angle médian 22-30°, **max 45°** | angle min 33-45°, médian 75-90° |
| Arbres, buissons | 0-4 % | médian 14-45°, max 50-64° | rares |
| Rochers | 92 % | | min 44° |
| Personnages | 5-15 % | médian 10-20°, max 45-60° | |

Règle : **lissage jusqu'à 45°**, arête vive au-delà. Les biseaux sont lissés avec les faces voisines ; les coins d'une boîte restent vifs. Végétation entièrement lissée ; rochers entièrement facettés.

### 2.5 Couleur : l'atlas en dégradé

![Atlas Hexagon](../../sandbox-level/Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/buildings/blue/hexagons_medieval.png)

- Un atlas par pack, **1024 x 1024 en 8 x 4 cases de 128 x 256 px**. Chaque case est **un dégradé vertical** : clair en haut, sombre en bas (Forest Nature : 4 cases utiles seulement, feuillage, tronc, pierre et une réserve).
- Écarts mesurés entre le haut et le bas d'une case : la valeur baisse de **20 à 45 points** (sur 100), la saturation monte de **5 à 20 points**, et la **teinte tourne** : les tons chauds vers le rouge (bois T19 -> T13, tronc T22 -> T9), les bleus vers le violet (toit T198 -> T239), les verts vers le bleu-vert (feuillage T87 -> T150). Les neutres restent neutres mais froids en bas (enduit #FDFDFD -> #ADBBC0).
- Valeurs et saturations (haut -> bas) : bois S54 V70 -> S65 V49 ; toit bleu S82 V89 -> S74 V42 ; enduit S0 V99 -> S10 V75 ; pierre S11 V75 -> V39 ; feuillage S68 V77 -> S96 V42.
- Une maison utilise **8 cases** : 4 principales (bois 43 %, toit 26 %, pierre 15 %, enduit 12 % de l'aire) et 3-4 accents (< 4 %). **Une seule couleur saturée par bâtiment** (le toit, couleur d'équipe), le reste est brun chaud ou neutre.
- **Les UV ne sont pas des points** : 86-96 % des triangles couvrent une partie du dégradé (étalement médian 24 px sur 256, soit 10 % de la case). Trois effets sont peints par les UV :
  - **dessus plus clair que les côtés** : v moyen du dessus 0,59-0,80 contre 0,34-0,52 pour les côtés, soit **+0,15 à +0,28** ;
  - **dégradé en hauteur** : corrélation hauteur/v de 0,2 à 0,9 sur les pièces (1,0 sur les troncs : pied sombre) ;
  - **ton propre à chaque pièce** : les planches voisines, les pierres d'un même tas ont des v de base différents (0,41 / 0,48 / 0,51 pour les pierres de home_B).
- Le Builder Pack 1.0 (2020) colore encore par matériau (5 couleurs plates : Beige, Brown, BrownDark, Stone, White). La méthode de l'atlas en dégradé est celle des packs récents.

![Atlas seul, home_A](../../sandbox-level/Assets/Screenshots/etude_A_albedo.png)

### 2.6 Décor

- **Toit** : 3-4 grandes planches par pan, dans le sens de la pente, séparées par des couvre-joints en saillie de 0,17 m (0,5 m de large), bouts d'égout en escalier. Les planches ont chacune un ton.
- **Soubassement** : bloc de pierre continu (home_B : 1,6 m de haut, arête haute chanfreinée) ou **pierres d'angle isolées** (home_A), plus des « plaques » de 3 pierres dans l'enduit (pierres de 0,63 x 0,42 m, 10 triangles chacune).
- **Colombages** : **en relief** (0,26 m), jamais peints ; croix de Saint-André en « K » sur l'étage de home_B.
- **Fenêtres** : pas d'embrasure creusée ; cadre cintré en saillie de 0,26 m, vitre en retrait de 0,15 m dans le cadre, appui ou jardinière en bois.
- **Porte** : planches verticales séparées par des rainures, 2 traverses, bouton rond ; cadre cintré à 7 segments. Porte du donjon : anneau en tore, claveaux en saillie irrégulière.
- **Cheminée** : prisme chanfreiné, chapeau plus large (débord 0,2 m), couleur sombre.

![Toit de home_A, filaire](../../sandbox-level/Assets/Screenshots/etude_A_angle_toit_filaire.png)
![Pierres d'angle de home_A](../../sandbox-level/Assets/Screenshots/etude_A_soubassement_filaire.png)

### 2.7 Arbres et buissons

![Forest Nature Pack, filaire](../../sandbox-level/Assets/Screenshots/etude_arbres_filaire.png)
![Forest Nature Pack, atlas seul](../../sandbox-level/Assets/Screenshots/etude_arbres_albedo.png)

- **1 à 3 gros volumes de feuillage**, plus 1-2 petites touffes au bout des branches. Formes simples et reconnaissables : cônes empilés (sapin, 2-3 étages), ellipsoïde aplati en parasol, cube aux arêtes très arrondies, boule bosselée. 100-270 triangles par volume, **lissés** (angle de pli 50-64°).
- **Tronc court, épais, conique, tordu**, 1-2 branches seulement ; hauteur totale **1,5 à 2,3 fois** le personnage.
- Couleurs : feuillage **#88C33F -> #056B37** (vert-jaune saturé vers vert-bleu), tronc **#F17C36 -> #813223** (orange vif vers brun rouge). Le tronc n'utilise que le bas de sa case (v 0,13-0,40) ; le feuillage toute la case, du bas du volume au sommet.
- Buissons : un volume lissé de 44-72 triangles (cube arrondi, boule). Rochers : volumes facettés (48 triangles), arêtes vives.

### 2.8 Sols (Dungeon Pack)

![Sols du Dungeon Pack, filaire](../../sandbox-level/Assets/Screenshots/etude_sols_donjon_filaire.png)
![Dalles, gros plan](../../sandbox-level/Assets/Screenshots/etude_sol_dalle_gros_plan_filaire.png)
![Planches, gros plan](../../sandbox-level/Assets/Screenshots/etude_sol_planches_gros_plan.png)

Ce qui fait « KayKit » : **tout est en géométrie, rien en texture**.
- Une dalle de 4 x 4 m est **un bloc épais (0,15 m)** à bord chanfreiné, découpé en **8-12 pierres irrégulières** (polygones de 5-7 côtés) séparées par des **rainures en V de 0,05 m** ; le dessus de chaque pierre est très légèrement facetté (éventail). 188 triangles.
- Plancher : lames de 0,5 m, décalées, longueurs variées, **chanfrein de 0,03 m**, légère inclinaison propre à chaque lame, dégradé le long de la lame. 272 triangles, arêtes vives.
- Terre : plaque plate + quelques cailloux en géométrie (104 triangles).
- **Une seule case de couleur par sol**, utilisée sur une plage courte (v 0,23-0,61 pour la pierre) : les sols sont calmes, les variations viennent des rainures et des normales. Aucun bruit, aucun moucheté.

### 2.9 Personnages (pour mémoire)

![Rogue et Knight, filaire](../../sandbox-level/Assets/Screenshots/etude_knight_rogue_filaire.png)

Tête = 46 % de la hauteur, membres courts et ronds, 5 800-7 600 triangles, lissés (85-95 % de coins lissés), un atlas par personnage dans la même logique de dégradé (corrélation hauteur/v de 0,7 à 1,0 par partie).

## 3. Règles pour un générateur

Échelle : celle du village (porte de 2,2 m, personnage de 2,3 m).

**Proportions**
1. Pente de toit **45°** (50° au plus) ; pour un bâtiment d'un niveau, le toit fait **45-55 %** de la hauteur jusqu'au faîte (home_A : 47 %).
2. Débord à l'égout **0,65-0,75 m**, au pignon **0,5 m** ; planches de rive au-dessus des tuiles de 0,15 m au moins.
3. Égout à **2,9-3,0 m** pour un rez-de-chaussée (mur jusqu'à la sablière : 3,2-3,6 m).
4. Porte : baie de **1,15-1,2 x 2,2 m** (0,95 fois le personnage), cintrée en plein cintre ou en arc à 7 segments ; fenêtres de **0,85-1,05 m** de large, appui à 1,2-1,3 m.
5. Largeur hors tout / hauteur hors cheminée **0,85-1,1** : des volumes ramassés.
6. **Relief** : tout ce qui habille le mur sort de **0,10 à 0,26 m** (colombages, sablières, cadres, pierres) ; les raccords au mur se font en chanfrein à 45°.

**Biseaux**
7. Biseau = **8-20 % de la plus petite dimension de la face, plafonné à 0,08 m** ; quart de cercle en **2 segments** pour les pièces de plus de 0,15 m, **1 segment lissé** en dessous (pièces fines, tuiles, clous).
8. Biseau sur les arêtes **vues** seulement : une arête couverte par la pièce voisine (haut d'une tuile, dos d'une pierre contre le mur) n'en a pas.
9. Coins des pièces (rencontre de deux flancs) : **arête vive**.

**Densité**
10. **10-40 triangles par petite pièce** (pierre : 28, tuile : 16, clou : 20), 30-110 pour une poutre ou une boîte arrondie. Les grandes faces planes restent en **1 ou 2 quadrilatères**.
11. Le détail va d'abord à la **silhouette** (égout en escalier, rives, faîtage, cheminée, saillies), ensuite aux pièces individuelles (tuiles, pierres, planches), jamais à des subdivisions de faces planes.
12. Arcs : **7 segments** pour un demi-cercle (claveaux : un par segment) ; cylindres : 8-10 côtés.

**Ombrage**
13. Normales écrites par le générateur, jamais recalculées : **lissage jusqu'à 45°** (biseaux, cylindres, végétation), arête vive au-delà.
14. **Chaque triangle est tourné vers ses normales** (face avant d'Unity du côté de cross(b - a, c - a)) : à contrôler par `AnalyseKayKit.RapportObjet` (« faces dans le sens des normales : 100 % »).

**Couleur et UV**
15. **Un atlas en dégradé** par famille d'objets, cases de 64-128 x 256 px, **un dégradé vertical par matière** : en bas, valeur **-25 à -45 points**, saturation **+5 à +20**, teinte tournée (chauds vers le rouge, bleus vers le violet, verts vers le bleu-vert). Courbe légèrement convexe (les clairs tiennent).
16. u au **milieu de la colonne** de la case (aucun débord au filtrage) ; v de chaque sommet = **ton de la pièce + 0,2 x normale.y + 0,2 à 0,3 x (hauteur relative dans la pièce - 0,5)**, borné à 4-96 % de la case.
17. **Ton propre à chaque pièce** tiré dans une plage de ±0,12-0,18 autour du ton de la matière (tuiles, pierres, planches) ; les grands murs suivent un dégradé commun à tout le bâtiment.
18. **4 matières principales + 3-4 accents** par bâtiment ; **une seule matière saturée** (le toit ou les volets) ; enduit presque blanc, pierre gris froid, bois brun chaud.
19. Un seul matériau opaque (URP Lit, lissé 0,1, sans métal) + un matériau des vitres (émission la nuit).

**Décor**
20. **Toit** : tuiles (ou planches) **individuelles en rangées décalées d'une demi-tuile**, chaque tuile basculée de l'épaisseur de la tuile (5-7°) pour marquer la rangée, ±1,5° de lacet et ±0,05 m de largeur au hasard ; faîtage en tuiles faîtières arrondies ; chevrons apparents sous l'égout.
21. **Soubassement** : pierres individuelles sur **2 assises irrégulières**, joints de 0,03 m décalés, **une pierre sur cinq sur deux assises**, harpes alternées aux angles, fond de mortier sombre derrière. Une pierre sur cinq dans une teinte plus claire.
22. **Colombages** en relief (pas peints) : poteaux d'angle, sablières qui dépassent aux angles, lisse, écharpes.
23. **Fenêtres** : embrasure de 0,22-0,26 m, vitre au fond, croisillon, chambranle en saillie, **appui qui déborde** de 0,2 m ; volets ouverts si la façade le permet.
24. **Porte** : planches séparées (rainures), traverses et écharpe en Z, **pentures en fer à bout arrondi avec clous**, anneau ; encadrement de **claveaux** avec clé plus claire et plus saillante ; seuil de pierre.
25. **Cheminée** en pierres (assises de 0,4 m, joints croisés aux angles), couronnement débordant de 0,15 m, mitron.

**Arbres, sols** (à appliquer aux prochains POC)
26. Arbres : 1-3 volumes lissés de 100-270 triangles + 1-2 touffes, tronc court tordu, feuillage #88C33F -> #056B37, tronc #F17C36 -> #813223, 350-1 000 triangles par arbre.
27. Sols : géométrie seulement (dalles irrégulières à rainures en V de 0,05 m, lames chanfreinées de 0,03 m), bloc de 0,15 m d'épaisseur, une case de couleur par sol sur une plage courte, **pas de texture de bruit**.

## 4. Ce qu'il faut éviter (défauts des POC)

**Maison du POC** (`sandbox-level/Assets/PocBatiments/`) :

![POC maison](../../sandbox-level/Assets/Screenshots/etude_poc_maison.png)
![POC maison, filaire : on voit les murs du fond à travers ceux de devant](../../sandbox-level/Assets/Screenshots/etude_poc_maison_filaire.png)

- **Faces retournées** : la plupart des faces des boîtes, prismes et pignons de `MeshBasPoly` pointaient vers l'intérieur. On voyait les faces intérieures des murs du fond à travers ceux de devant (filaire ci-dessus), l'éclairage et les colliders maillés étaient faux. Corrigé le 26/09/2026 (voir 5), ce qui a révélé un second défaut : les murs étaient centrés sur la ligne de leur face extérieure, les fenêtres et colombages se retrouvaient dans l'épaisseur du mur.
- **Une couleur plate par matériau** (14 matériaux) : aucun dégradé, aucune variation d'une pièce à l'autre, rendu « plastique ».
- **Pas de biseau** sur la plupart des pièces (chanfrein seulement sur les arêtes verticales des murs).
- **Relief trop faible** : lisses de 0,14 m sur 0,01 m de saillie, cadres de 0,05-0,06 m ; de loin, la façade est plate.
- **Toit en une dalle** (ni tuiles, ni planches, ni rangées), pente de 32°, débord de 0,45 m : le toit ne fait que 39 % de la silhouette.
- **Soubassement en un bloc**, fenêtres collées au mur (pas d'embrasure ni d'appui), porte en une boîte.
- Grandes surfaces d'enduit vides (6 x 5,4 m pour un seul niveau).

![POC maison, corrigée (orientation des faces et axe des murs)](../../sandbox-level/Assets/Screenshots/etude_poc_maison_corrige.png)

**Arbre du POC** (`sandbox-ui/Assets/PocArbre/`) :

![POC arbre](../../sandbox-ui/Assets/Screenshots/poc_arbre_gros_plan.png)

- Feuillage fait de **nombreuses petites boules facettées** (icosphères plates) au lieu de 1-3 gros volumes lissés.
- **Vert olive sombre et terne** (au lieu du vert-jaune saturé qui tourne au vert-bleu).
- **Branches longues et fines, réalistes** ; KayKit : tronc court, épais, tordu, 1-2 branches.

**Sols du POC** (`sandbox-ui/Assets/PocArbre/Sol/`) :

![POC sols](../../sandbox-ui/Assets/Screenshots/poc_sol_motifs.png)

- **Textures de bruit**, mouchetures, rayures peintes, dalles dessinées dans la texture : tout ce que KayKit n'a pas. Chez KayKit, le relief et les rainures sont en géométrie, la couleur est un dégradé calme.

## 5. La maison générée

Générateur `sandbox-level/Assets/StyleKayKit/Editor/MaisonStyleBuilder.cs` (menu **Deathless > Style KayKit > Générer la maison**), outils `MeshBasPoly.cs` (réécrit) et `AtlasDegrade.cs` (atlas `Assets/StyleKayKit/Maison/AtlasMaison.png`, 512 x 512, 8 x 2 cases de 64 x 256 px). Extérieur seulement.

![Comparaison de jour](../../sandbox-level/Assets/Screenshots/style_maison_comparaison_jour.png)
![Comparaison de nuit](../../sandbox-level/Assets/Screenshots/style_maison_comparaison_nuit.png)
![Hauteur d'homme](../../sandbox-level/Assets/Screenshots/style_maison_hauteur_homme.png)
![Gros plan](../../sandbox-level/Assets/Screenshots/style_maison_gros_plan.png)

De gauche à droite : `building_home_A`, la maison générée, `building_home_B` (x7,5, même lumière : préréglages jour et nuit de `AmbianceVillage`, fenêtres KayKit allumées par la carte d'émission du village).

| | home_A | home_B | Maison générée |
|---|---|---|---|
| Triangles | 1 011 | 1 393 | **11 280** + 132 (vitres) |
| Pièces | 20 | 44 | 415 (médiane 28 triangles) |
| Faces dans le sens des normales | 100 % | 100 % | 100 % |
| Lissage (angle max des arêtes lissées) | 45° | 45° | 45° (73° sur le tore de l'anneau) |
| Cases d'atlas | 8 | 8 | 10 + 2 (vitres, lanterne) |

Répartition : toit 3 432 (134 tuiles à 16 triangles, faîtage, rives, chevrons, platelage), façades 3 016 (porte, 4 fenêtres, volets), soubassement 1 400 (pierres à 28 triangles), cheminée 1 144, colombages 992, pignons 792, lanterne et pierres apparentes 504.

Ce qui applique le guide : emprise 5,6 x 4,6 m, égout à 3,0 m, haut du faîtage à 6,5 m (toit : 54 % de la hauteur), pente 45°, largeur hors tout / hauteur 1,06, débords 0,65 + 0,14 m à l'égout et 0,5 m au pignon ; porte cintrée de 1,2 x 2,2 m en embrasure de 0,26 m dans 7 claveaux et 8 piédroits ; fenêtres de 0,85 x 1,0 m en embrasure de 0,24 m ; colombages à 0,10-0,19 m de saillie ; biseaux arrondis de 0,014 à 0,075 m ; atlas en dégradé avec tons par pièce.

![Filaire](../../sandbox-level/Assets/Screenshots/style_maison_filaire.png)
![Atlas seul (sans lumière)](../../sandbox-level/Assets/Screenshots/style_maison_albedo.png)
![Arrière et cheminée](../../sandbox-level/Assets/Screenshots/style_maison_arriere.png)

Limites connues :
- **Densité** : 8 à 11 fois celle des maisons KayKit, voulue (tuiles et pierres individuelles). Pour six maisons, 68 000 triangles, sans enjeu sur PC. Leviers si besoin : tuiles plus grandes (0,9 x 0,6 m), pierres du soubassement en 1 segment (20 triangles au lieu de 28), volets seulement en façade.
- Le relief des colombages (0,10-0,19 m) reste en dessous des 0,26 m de KayKit : plus fin, un peu moins « jouet ».
- Pas d'intérieur ; la porte est fixe.
