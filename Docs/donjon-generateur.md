# Générateur procédural du donjon « terrasses étagées »

État au 02/10/2026. Générateur écrit d'après le croquis de Quentin (`Docs/da/brief-donjon.md`, section « Version 2 ») et les dix plans de `Docs/outils/donjon_plans.py` (images `Docs/da/gabarits/donjon-plans/`), qui fixent la plage de variation attendue. **Il tourne dans un banc à part ; le jeu utilise encore l'ancien donjon** (`Assets/Scripts/Donjon/DonjonGenerateur.cs`, appelé par `DonjonJeu`) : le branchement est listé en fin de fiche.

## Ce qu'il produit

Un seul grand volume fermé sous une voûte commune, en **2 ou 3 niveaux de sol plein** (rez compris) : la grande salle au rez (Lvl 0, arrivée et portail de retour au sud), des terrasses à + 3 m (Lvl 1) et + 6 m (Lvl 2) adossées au fond ou aux côtés, tenues par de **gros murs de soutènement** (jamais de balcon, de galerie ni de mezzanine), de **larges escaliers pleins** (4 à 6 m), des **arches sans vantail** vers de **petites pièces cachées**, des **piliers** sur le rez et des pilastres le long des murs, des coffres, des torches, les points d'arrivée des joueurs et les points d'apparition des monstres.

Même graine = même donjon sur toutes les machines : le plan est du C# pur, tiré par un générateur entier maison (xorshift32), sans aucune décision prise sur un flottant calculé. Contrôlé : l'empreinte du plan (`PlanTerrasses.Empreinte()`) des graines 1 à 50 est identique dans Unity (Mono) et sous .NET 10. En réseau (NGO), seule la graine circule : chaque poste appelle `Generer(graine)`.

## Fichiers

| Fichier | Rôle |
|---|---|
| `Assets/Scripts/Jeu/Donjon/Terrasses/PlanTerrasses.cs` | Plan pur (sans UnityEngine) : variantes, grille de colonnes, pièces cachées, faces de murs, parapets, piliers, mécanismes, coffres, torches, apparitions, empreinte |
| `Assets/Scripts/Jeu/Donjon/Terrasses/PlanTerrassesValidation.cs` | Contrôles (connexité, hauteurs, escaliers, surplombs, zone d'arrivée, piliers, coffres, apparitions) |
| `Assets/Scripts/Jeu/Donjon/Terrasses/ConstructeurTerrasses.cs` | Géométrie, collisions, lumières, repères (`DonjonRepere`), NavMesh, contrôle NavMesh |
| `Assets/Scripts/Jeu/Donjon/Terrasses/MaillageTampon.cs` | Blocs aux arêtes abattues et normales lissées, fûts, quads, à couleurs par sommet |
| `Assets/Scripts/Jeu/Donjon/Terrasses/MecanismesDonjon.cs` | `PorteDonjon` (porte à serrure, paroi secrète), `DeclencheurDonjon` (plaque, bouton), `TorcheDonjon` (vacillement) |
| `Assets/Scripts/Jeu/Dev/BancDonjonTerrasses.cs` | Banc : régénération par graine, vues joueur / dessus / iso / libre, panneau en jeu |
| `Assets/Editor/Donjon/DonjonTerrassesMenu.cs` | Menus `Deathless > Donjon > Terrasses`, captures, contrôles en masse, bouton « Générer » de l'inspecteur |
| `Assets/Scenes/Dev/DonjonBanc.unity` | Scène de banc |
| `Assets/Art/Donjon/Terrasses/DonjonTerrasses_Pierre.mat`, `DonjonTerrasses_Flamme.mat` | `Deathless/VertexColorLit` (pierre, bois, métal), `Relic/VertexColorUnlit` (flammes, portail, repères) |

## Utilisation

```csharp
// Plan seul (serveur, outils, tests) : aucun objet Unity
var plan = new PlanTerrasses();
bool conforme = plan.Generer(graine);            // ParametresTerrasses facultatifs
uint empreinte = plan.Empreinte();              // à comparer entre hôte et clients si besoin

// Donjon construit (tous les postes)
var ct = GetComponent<ConstructeurTerrasses>();   // matériaux assignés
ct.Generer(graine);                              // plan + géométrie + collisions + NavMesh
// ct.Arrivee, ct.PortailRetour, ct.Butins (coffres), ct.Apparitions, ct.Joueurs, ct.Portes, ct.Declencheurs
```

Repère du plan : mètres, x vers l'est, z vers le nord, y vers le haut ; origine au coin sud-ouest intérieur de l'enceinte ; le constructeur place tout sous son transform (`ct.Monde(x, y, z)`).

## Étapes du plan

1. **Niveaux et variante** : 2 niveaux avec une chance sur trois (`probaDeuxNiveaux`), sinon 3 ; variante tirée parmi celles qui conviennent (3 niveaux : v1, v2, v3, v4, v6, v7 ; 2 niveaux : v1, v4, v5, v6). Volume de 32 à 40 m × 30 à 36 m, bande du fond de 12 à 15 m.
2. **Terrasses, massifs, escaliers** (variante, cotes de `donjon_plans.py`).
3. **Grille** de colonnes de 1 m avec 10 m de marge (enceinte, pièces derrière elle) : chaque colonne est pleine jusqu'à son sol ; une cavité (pièce, passage d'arche) a un plein haut (dalle, linteau).
4. **Arrivée** : dalle de bois de 6 × 2 m devant le portail (milieu du mur sud), zone d'arrivée libre de 6 × 5 m, quatre points de joueurs.
5. **Pièces cachées** (2, parfois 3) cherchées partout où elles tiennent : **sous une terrasse** qui domine de 5,1 m au moins le sol de devant (arche dans le mur de soutènement, priorité du croquis), **dans un massif**, ou **derrière l'enceinte** (jamais au sud) ; 6 à 8 × 5 à 6 m, murs de 1 m au moins, 12 m entre deux pièces.
6. **Faces de murs** (enceinte, soutènement, massif, parois des pièces, linteaux) et **parapets** (0,9 m, sur tout bord qui domine de 1,5 m au moins, ouverts en haut des escaliers).
7. **Voûte** : naissance à 5 m au-dessus du plus haut sol (8 ou 11 m), clé 3 à 3,5 m plus haut ; arcs doubleaux tous les 8 m environ sur des pilastres des murs latéraux ; pilastres aussi le long des murs de soutènement de plus de 10 m.
8. **Piliers libres** sur le rez : une ou deux rangées (en quinconce si elles sont à moins de 8 m), sur chaque tronçon dégagé, à 3 m au moins de tout mur, escalier ou terrasse, jamais dans la zone d'arrivée ni sur l'allée arrivée → pied d'escalier, espacés de 8 m au moins.
9. **Pièces fermées** : une pièce verrouillée (une fois sur deux), une pièce secrète (deux fois sur cinq) et son déclencheur.
10. **Coffres** contre les murs (grand coffre sur la plus haute terrasse, deux coffres par terrasse, contenu des pièces selon leur genre), **torches** (une tous les 7 m sur les murs, deux qui encadrent le portail, une au fond de chaque pièce ; 24 lumières au plus, réparties au plus loin les unes des autres), **apparitions** (gardiens sur les terrasses, dont un mage au bord pour tirer d'en haut, le reste au rez).
11. **Contrôles** ; un plan refusé est refait avec l'essai suivant de la même graine (déterministe).

## Variantes (v1 à v7 de `donjon_plans.py`, corrigées)

| | Disposition | Corrections par rapport au script Python |
|---|---|---|
| v1 | Deux terrasses côte à côte (Lvl 1 et Lvl 2, ou deux Lvl 1), couloir central fermé par un massif, escaliers le long des murs | Escalier du Lvl 2 en deux volées et un palier (12 m), encastré dans la terrasse s'il mangerait l'avant de la salle |
| v2 | Chaîne : Lvl 1 d'un côté, Lvl 2 de l'autre, atteint par un escalier latéral posé sur une bande Lvl 1 le long du fond | L'escalier 1 → 2 du script traversait le couloir (pont) ou montait 3 m sur 4 m |
| v3 | Deux Lvl 1 aux extrémités, Lvl 2 au centre atteint depuis les deux (boucle) | Terrasses jointives (le script laissait 2 m de vide), escaliers 1 → 2 de 5 m contre le fond |
| v4 | Grande terrasse Lvl 1 sur tout le fond, estrade Lvl 2 dans un angle, escalier central de 6 m | Escalier 1 → 2 de 5 m contre le fond ; en 2 niveaux, sans estrade |
| v5 | Deux niveaux : deux terrasses Lvl 1 inégales, massif entre elles | (inchangé) |
| v6 | Terrasses le long des murs est et ouest, en vis-à-vis ; le fond reste au rez | Escalier du Lvl 2 encastré avec palier ; en 2 niveaux, deux Lvl 1 |
| v7 | Grande terrasse Lvl 1, estrade centrale Lvl 2, grand escalier central en deux volées | Estrade calée sur la place réelle des deux volées et du palier de 2 m |

## Paramètres (`ParametresTerrasses`, sérialisés sur le constructeur)

| Champ | Défaut | Rôle |
|---|---|---|
| `variante` | 0 | 0 : tirée ; 1 à 7 : imposée |
| `niveaux` | 0 | 0 : tiré ; 2 ou 3 : imposé (v2, v3, v7 font toujours 3, v5 toujours 2) |
| `probaDeuxNiveaux` | 0,3 | Part des donjons à 2 niveaux |
| `nbApparitions` | 14 | Points d'apparition des monstres |
| `coffresParTerrasse` | 2 | Coffres par terrasse (1 sur une petite terrasse) |
| `probaTroisiemePiece` | 0,35 | Troisième pièce cachée |
| `probaPieceVerrouillee`, `probaPieceSecrete` | 0,5, 0,4 | Mécanismes (voir plus bas) |
| `hauteurLibreMin` | 4,5 m | Hauteur libre contrôlée partout où l'on marche |
| `espacementPiliersMin` | 8 m | Entre deux piliers libres |
| `espacementApparitionsMin`, `distanceArriveeApparitions` | 5 m, 10 m | Apparitions |
| `espacementTorches`, `maxTorchesAllumees` | 7 m, 24 | Torches et lumières |
| `maxEssais` | 40 | Essais avant d'abandonner (jamais atteint : 5 000 graines conformes au premier essai) |

Constantes (`PlanTerrasses`) : niveau 3 m ; marche 0,30 × 0,50 m, volée de 5 m (31°), palier 2 m ; escalier de 4 m (6 m au centre de v4 et v7) ; arche de 3 × 4,5 m en plein cintre (naissance à 3 m) ; dalle de 0,6 m au-dessus d'une pièce ; pièce derrière l'enceinte haute de 5 m ; parapet de 0,9 m ; pilier de 0,85 m de rayon, pilastre de 0,7 m.

## Contrôles (`Valider`)

Niveaux 2 ou 3 et chaque terrasse atteinte ; zone d'arrivée de 6 × 5 m libre (ni pilier, ni coffre, ni apparition, ni déclencheur) ; **connexité** : toute case de sol atteinte depuis l'arrivée (portes ouvertes), et portes fermées tout sauf l'intérieur des pièces fermées ; **hauteur libre ≥ 4,5 m** sur toute case praticable (voûte, plafond des pièces, linteaux) ; escaliers : pente ≤ 31°, longueur exacte, pied et palier d'arrivée praticables ; **aucun surplomb** (tout plein haut est le plafond d'une pièce fermée sur ses côtés) ; pièces assez hautes, arches assez grandes, pièce fermée inatteignable sans l'ouvrir, déclencheur atteignable sans la pièce ; piliers à 8 m les uns des autres ; coffres et apparitions atteignables, apparitions espacées et loin de l'arrivée. Le banc vérifie en plus le **NavMesh construit** (`VerifierNavMesh` : chaque coffre, chaque apparition et le portail atteints depuis l'arrivée, portes ouvertes).

Mesures : 5 000 graines sous .NET, toutes conformes au premier essai, 0,8 ms par plan (2,8 ms dans l'éditeur, contrôles compris) ; NavMesh sans défaut sur les graines 1 à 12 et sur les six graines des captures. Piliers libres : 1 à 6 (sept plans sur 5 000 n'en ont aucun, quand le rez est trop encombré).

## Pièces fermées {à confirmer}

Les petites pièces cachées sont **libres** (arche sans vantail) par défaut. Deux mécanismes demandés par Quentin le 02/10/2026 :

- **Pièce verrouillée** (`GenrePiece.Verrouillee`, `Serrure` : `Bronze`, `Argent`, `Or` ou `Crochetable`) : porte de bois à bandes et serrure de la couleur du métal, dans l'arche. Les clés s'achètent au village (mécano) : **aucune clé dans le donjon**, donc aucune dépendance de connexité interne. Toutes les serrures se crochètent (`PorteDonjon.DifficulteCrochetage` : 1 simple ou bronze, 2 argent, 3 or) ; la serrure « crochetable » n'a pas de clé vendue. Contenu proportionné : bronze ou crochetable 1 coffre, argent 2 coffres, or 1 grand coffre et 1 coffre.
- **Pièce secrète** (`GenrePiece.Secrete`, `Declencheur`) : pan de mur aux mêmes assises que le mur autour, de préférence dans un mur de soutènement ou un massif, qui s'enfonce dans le sol quand on actionne le **déclencheur** posé ailleurs, sur un lieu atteignable sans la pièce : **plaque de pression** au sol (dalle de 1,2 m un peu plus sombre, entre 8 et 26 m de la pièce) ou **bouton mural** discret (pierre claire dans un cadre sombre à 1,1 m du sol, entre 6 et 26 m). Le plan donne pour chaque déclencheur son genre, sa pose et sa pièce cible (`Declencheur.cible`, `PieceCachee.declencheur`). Contenu : 2 coffres.

Côté objets : `PorteDonjon.Ouvrir()` (porte qui pivote de 100° vers l'intérieur, paroi qui descend de 4,7 m), coupe sa collision et son obstacle NavMesh ; le NavMesh est construit portes ouvertes et un `NavMeshObstacle` (découpe, en jeu seulement) ferme chaque porte. `DeclencheurDonjon` ouvre sa cible quand un `CharacterController` entre dans sa zone (plaque) ou par `Activer()` (bouton, en attendant l'interaction). Restent à décider et à faire : prix des clés, mini-jeu de crochetage, avantage de l'assassin par ses points d'attribut, et la réplication réseau (l'autorité ouvre, les clients suivent).

## Géométrie

Style jouet : **blocs de pierre arrondis** (arêtes abattues, normales lissées) en assises de 0,75 m décalées, sur un fond de mortier sombre ; dalles de 2 m ; marches pleines ; parapets en gros blocs ; piliers en tambours de 1,1 m sur socle et chapiteau ; voûte segmentaire à bandes décalées et arcs doubleaux ; corniche à la naissance ; arches à claveaux et clé. Albédo plat par couleurs de sommet (palette dans `ConstructeurTerrasses`, une teinte par niveau de sol : le Lvl 2 est le plus clair), légères variations par bloc tirées d'un hachage des cotes ; flammes et portail non éclairés. Lumières ponctuelles de 9 m, sans ombre, vacillement léger en jeu.

Rendu combiné par carrés de 16 m (un seul matériau), murs d'enceinte groupés par côté (la vue en coupe cache le sud et l'ouest), voûte à part (cachée en vue de dessus). Collisions : boîtes des colonnes de la grille fusionnées en rectangles, rampes des volées, seuils, paliers, parapets, capsules des piliers, boîtes des coffres et des portes. NavMesh : `NavMeshSurface` (colliders physiques, agent Humanoid), portes et déclencheurs ignorés. Ordre de grandeur (graines des captures) : 200 000 à 250 000 sommets en ~40 objets rendus, 24 lumières, 45 à 115 collisions ; plan 3 ms, géométrie 40 ms, NavMesh 6 ms dans l'éditeur. Les objets créés dans l'éditeur ne sont jamais enregistrés dans la scène.

## Banc et captures

`Assets/Scenes/Dev/DonjonBanc.unity` (le donjon y est placé en (3000, 0, 3000) pour pouvoir ouvrir la scène à côté de la carte) : en jeu, panneau en haut à gauche (graine, Générer, ←/→, Au hasard, vues Joueur / Dessus / Iso / Libre), touches N (graine suivante) et V (vue suivante), vue libre au clavier (ZQSD ou WASD, A/E ou C pour monter et descendre, clic droit pour tourner) ; en édition, boutons « Générer », « Graine précédente / suivante » de l'inspecteur du constructeur. Menus `Deathless > Donjon > Terrasses` : créer le banc, captures, contrôle de 2 000 graines. Les outils ouvrent le banc **en additif** et le referment : ils ne remplacent jamais la scène ouverte dans l'éditeur.

Captures (1920 × 1080, `Assets/Screenshots/donjon_terrasses_g<graine>_<vue>.png`) des graines **9** (v1, 3 niveaux, serrure d'or), **12** (v2, pièce secrète à bouton, serrure de bronze), **16** (v7, serrure crochetable), **17** (v3, serrure de bronze dans le mur de soutènement, pièce secrète à bouton), **21** (v4, 2 niveaux, serrure d'or), **30** (v5, 2 niveaux, pièce secrète à plaque, serrure crochetable) : `dessus` (sans voûte, repères), `joueur` (caméra à l'épaule depuis l'arrivée), `iso` (coupe), `terrasse` (caméra à l'épaule sur la plus haute terrasse), et pour les pièces fermées `porte_<serrure>`, `paroi_secrete`, `paroi_secrete_ouverte`, `declencheur_plaque` ou `declencheur_bouton`.

## Reste à faire

- **Brancher le jeu** : remplacer `DonjonGenerateur` par `ConstructeurTerrasses` dans `DonjonJeu` (mêmes repères `DonjonRepere` : arrivée, portail, butins, apparitions ; la graine passe déjà par `PartieReseau.GraineDonjon`), revoir `DonjonMasquage` (inutile ici : pas d'étages superposés), la caméra (`CameraEpaule.Enceinte` : boîte intérieure L × P × naissance) et les scénarios `ScenariosDonjon`.
- **Découpe autour du héros** : le shader `Deathless/VertexColorLit` n'a pas la découpe d'occlusion de `Deathless/DonjonDecoupe` ; lui ajouter (ou faire une variante à couleurs de sommet).
- **Gameplay des pièces fermées** (clés au mécano, crochetage, réplication réseau des ouvertures) et vrais modèles de coffres du jeu sur les repères de butin.
- Allègement possible si besoin : faces arrière et dessous des blocs déjà retirés ; on peut encore réduire les assises au-dessus de 6 m (vues de loin).
