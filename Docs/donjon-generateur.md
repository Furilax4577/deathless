# Générateur procédural du donjon « terrasses étagées »

État au 02/10/2026. Générateur écrit d'après le croquis de Quentin (`Docs/da/brief-donjon.md`, section « Version 2 ») et les dix plans de `Docs/outils/donjon_plans.py` (images `Docs/da/gabarits/donjon-plans/`), qui fixent la plage de variation attendue. **Branché le 02/10/2026 dans le mode « Nouvelle carte (aperçu) »** (scène CarteV5, `Partie.Exploration`) : le portail de la grotte y mène. **Le jeu normal (ancienne carte) garde l'ancien donjon** (`Assets/Scripts/Donjon/DonjonGenerateur.cs`, appelé par `DonjonJeu`). Voir « Branchement dans le jeu » en fin de fiche.

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
| `Assets/Scripts/Jeu/Donjon/Terrasses/VueLibre.cs` | Marque des collisions que la caméra traverse (piliers libres, parapets) |
| `Assets/Scripts/Jeu/Donjon/MecanismeApercu.cs` | Aperçu seulement : touche Interagir sur une porte à serrure ou un bouton mural |
| `Assets/Scripts/Jeu/Donjon/DonjonJeu.cs` | Donjon en partie : construit l'ancien donjon ou celui-ci (`TerrassesVoulues`), portails, butins, gardiens, caméra |
| `Assets/Scripts/Jeu/Dev/BancDonjonTerrasses.cs` | Banc : régénération par graine, vues joueur / dessus / iso / libre, panneau en jeu |
| `Assets/Editor/Donjon/DonjonTerrassesMenu.cs` | Menus `Deathless > Donjon > Terrasses`, captures, contrôles en masse, bouton « Générer » de l'inspecteur |
| `Assets/Scenes/Dev/DonjonBanc.unity` | Scène de banc |
| `Assets/Jeu/Resources/DonjonTerrasses/DonjonTerrasses_Pierre.mat`, `DonjonTerrasses_Flamme.mat` | `Deathless/VertexColorLitDecoupe` (pierre, bois, métal ; découpe autour du héros), `Relic/VertexColorUnlit` (flammes, portail, repères). Dans les Resources : `DonjonJeu` les charge en jeu sans référence de scène |
| `Assets/Art/Shaders/VertexColorLitDecoupe.shader` | `Deathless/VertexColorLit` + la découpe de `DonjonDecoupeCommun.hlsl` (passes couleur, profondeur, normales) |

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
| `espacementApparitionsMin`, `distanceArriveeApparitions` | 5 m, 16 m | Apparitions ; 16 m depuis le 02/10/2026 (10 m avant : les gardiens, qui repèrent à 12 m, tombaient sur le joueur dès l'arrivée). Les points de joueurs étant à 2-3 m du point d'arrivée, le gardien le plus proche est à 13 m au moins d'un héros fraîchement arrivé |
| `espacementTorches`, `maxTorchesAllumees` | 7 m, 24 | Torches et lumières |
| `maxEssais` | 40 | Essais avant d'abandonner (jamais atteint : 5 000 graines conformes, 4 541 au premier essai et 459 au deuxième ou plus, faute de place pour les 14 apparitions à 16 m de l'arrivée) |

Constantes (`PlanTerrasses`) : niveau 3 m ; marche 0,30 × 0,50 m, volée de 5 m (31°), palier 2 m ; escalier de 4 m (6 m au centre de v4 et v7) ; arche de 3 × 4,5 m en plein cintre (naissance à 3 m) ; dalle de 0,6 m au-dessus d'une pièce ; pièce derrière l'enceinte haute de 5 m ; parapet de 0,9 m ; pilier de 0,85 m de rayon, pilastre de 0,7 m.

## Contrôles (`Valider`)

Niveaux 2 ou 3 et chaque terrasse atteinte ; zone d'arrivée de 6 × 5 m libre (ni pilier, ni coffre, ni apparition, ni déclencheur) ; **connexité** : toute case de sol atteinte depuis l'arrivée (portes ouvertes), et portes fermées tout sauf l'intérieur des pièces fermées ; **hauteur libre ≥ 4,5 m** sur toute case praticable (voûte, plafond des pièces, linteaux) ; escaliers : pente ≤ 31°, longueur exacte, pied et palier d'arrivée praticables ; **aucun surplomb** (tout plein haut est le plafond d'une pièce fermée sur ses côtés) ; pièces assez hautes, arches assez grandes, pièce fermée inatteignable sans l'ouvrir, déclencheur atteignable sans la pièce ; piliers à 8 m les uns des autres ; coffres et apparitions atteignables, apparitions espacées et loin de l'arrivée. Le banc vérifie en plus le **NavMesh construit** (`VerifierNavMesh` : chaque coffre, chaque apparition et le portail atteints depuis l'arrivée, portes ouvertes).

Mesures : 5 000 graines sous .NET, toutes conformes (à 16 m d'arrivée : 459 refaites avec l'essai suivant de la même graine, 14 apparitions placées partout ; à 18 m : 1 441 refaites, toujours 5 000 conformes ; à 10 m, valeur d'avant : aucune), 0,8 ms par plan (2,8 ms dans l'éditeur, contrôles compris) ; NavMesh sans défaut sur les graines 1 à 12 et sur les six graines des captures. Piliers libres : 1 à 6 (sept plans sur 5 000 n'en ont aucun, quand le rez est trop encombré).

## Pièces fermées {à confirmer}

Les petites pièces cachées sont **libres** (arche sans vantail) par défaut. Deux mécanismes demandés par Quentin le 02/10/2026 :

- **Pièce verrouillée** (`GenrePiece.Verrouillee`, `Serrure` : `Bronze`, `Argent`, `Or` ou `Crochetable`) : porte de bois à bandes et serrure de la couleur du métal, dans l'arche. Les clés s'achètent au village (mécano) : **aucune clé dans le donjon**, donc aucune dépendance de connexité interne. Toutes les serrures se crochètent (`PorteDonjon.DifficulteCrochetage` : 1 simple ou bronze, 2 argent, 3 or) ; la serrure « crochetable » n'a pas de clé vendue. Contenu proportionné : bronze ou crochetable 1 coffre, argent 2 coffres, or 1 grand coffre et 1 coffre.
- **Pièce secrète** (`GenrePiece.Secrete`, `Declencheur`) : pan de mur aux mêmes assises que le mur autour, de préférence dans un mur de soutènement ou un massif, qui s'enfonce dans le sol quand on actionne le **déclencheur** posé ailleurs, sur un lieu atteignable sans la pièce : **plaque de pression** au sol (dalle de 1,2 m un peu plus sombre, entre 8 et 26 m de la pièce) ou **bouton mural** discret (pierre claire dans un cadre sombre à 1,1 m du sol, entre 6 et 26 m). Le plan donne pour chaque déclencheur son genre, sa pose et sa pièce cible (`Declencheur.cible`, `PieceCachee.declencheur`). Contenu : 2 coffres.

Côté objets : `PorteDonjon.Ouvrir()` (porte qui pivote de 100° vers l'intérieur, paroi qui descend de 4,7 m), coupe sa collision et son obstacle NavMesh ; le NavMesh est construit portes ouvertes et un `NavMeshObstacle` (découpe, en jeu seulement) ferme chaque porte. `DeclencheurDonjon` ouvre sa cible quand un `CharacterController` entre dans sa zone (plaque) ou par `Activer()` (bouton, en attendant l'interaction). Restent à décider et à faire : prix des clés, mini-jeu de crochetage, avantage de l'assassin par ses points d'attribut, et la réplication réseau (l'autorité ouvre, les clients suivent).

## Géométrie

Style jouet : **blocs de pierre arrondis** (arêtes abattues, normales lissées) en assises de 0,75 m décalées, sur un fond de mortier sombre ; dalles de 2 m ; marches pleines ; parapets en gros blocs ; piliers en tambours de 1,1 m sur socle et chapiteau ; voûte segmentaire à bandes décalées et arcs doubleaux ; corniche à la naissance ; arches à claveaux et clé. Albédo plat par couleurs de sommet (palette dans `ConstructeurTerrasses`, une teinte par niveau de sol : le Lvl 2 est le plus clair), légères variations par bloc tirées d'un hachage des cotes ; flammes et portail non éclairés. Lumières ponctuelles de 9 m, sans ombre, vacillement léger en jeu.

Coffres : caisse et couvercle à part (enfant `Couvercle_lid`, charnière à l'arrière, à 0,6 m), que `DonjonJeu` fait basculer à l'ouverture. Piliers libres et parapets portent `VueLibre` (la caméra les traverse, ils sont découpés).

Rendu combiné par carrés de 16 m (un seul matériau), murs d'enceinte groupés par côté (la vue en coupe cache le sud et l'ouest), voûte à part (cachée en vue de dessus). Collisions : boîtes des colonnes de la grille fusionnées en rectangles, rampes des volées, seuils, paliers, parapets, capsules des piliers, boîtes des coffres et des portes. NavMesh : `NavMeshSurface` (colliders physiques, agent Humanoid), portes et déclencheurs ignorés. Ordre de grandeur (graines des captures) : 200 000 à 250 000 sommets en ~40 objets rendus, 24 lumières, 45 à 115 collisions ; plan 3 ms, géométrie 40 ms, NavMesh 6 ms dans l'éditeur. Les objets créés dans l'éditeur ne sont jamais enregistrés dans la scène.

## Banc et captures

`Assets/Scenes/Dev/DonjonBanc.unity` (le donjon y est placé en (3000, 0, 3000) pour pouvoir ouvrir la scène à côté de la carte) : en jeu, panneau en haut à gauche (graine, Générer, ←/→, Au hasard, vues Joueur / Dessus / Iso / Libre), touches N (graine suivante) et V (vue suivante), vue libre au clavier (ZQSD ou WASD, A/E ou C pour monter et descendre, clic droit pour tourner) ; en édition, boutons « Générer », « Graine précédente / suivante » de l'inspecteur du constructeur. Menus `Deathless > Donjon > Terrasses` : créer le banc, captures, contrôle de 2 000 graines. Les outils ouvrent le banc **en additif** et le referment : ils ne remplacent jamais la scène ouverte dans l'éditeur.

Captures (1920 × 1080, `Assets/Screenshots/donjon_terrasses_g<graine>_<vue>.png`) des graines **9** (v1, 3 niveaux, serrure d'or), **12** (v2, pièce secrète à bouton, serrure de bronze), **16** (v7, serrure crochetable), **17** (v3, serrure de bronze dans le mur de soutènement, pièce secrète à bouton), **21** (v4, 2 niveaux, serrure d'or), **30** (v5, 2 niveaux, pièce secrète à plaque, serrure crochetable) : `dessus` (sans voûte, repères), `joueur` (caméra à l'épaule depuis l'arrivée), `iso` (coupe), `terrasse` (caméra à l'épaule sur la plus haute terrasse), et pour les pièces fermées `porte_<serrure>`, `paroi_secrete`, `paroi_secrete_ouverte`, `declencheur_plaque` ou `declencheur_bouton`.

## Branchement dans le jeu (02/10/2026)

**Ce qui est branché** (mode « Nouvelle carte (aperçu) » seulement : `DonjonJeu.TerrassesVoulues` = `Partie.Exploration` ; l'ancienne carte et le jeu normal gardent `DonjonGenerateur`, vérifié en Play : graine 5, arrivée, gardiens, retour) :

- **Construction** : `DonjonJeu.Construire` → `ConstruireTerrasses(graine)`. Le constructeur est créé une fois sous l'objet `Donjon`, en `DonjonJeu.OrigineTerrasses` (1000, 0, 120 : à côté de l'ancien, qui n'est pas construit dans l'aperçu), inactif le temps d'être réglé (pas de génération à son `Awake`), matériaux chargés des Resources. Même chemin que l'ancien : l'autorité tire la graine au premier jour (`NouveauDonjon`), elle part aux clients par `PartieReseau.GraineDonjon` et chaque poste construit le même donjon (l'essai `EssaiDonjon` vaut 0, inutile ici). Construction 30 à 60 ms (plan 3, géométrie 30-60, NavMesh 6-7) dans l'éditeur.
- **Repères** : `DonjonJeu.RepereArrivee`, `RepereRetour`, `Butins`, `Apparitions` lisent l'un ou l'autre donjon ; `Pris` (masque de 32 bits) suit les coffres (5 à 8 par graine). Emprise « au donjon » (`Contient`) : celle du plan, marge des pièces comprise.
- **Portail de la grotte → donjon** : `PassagePortail` du portail de la carte (inchangé) ; arrivée sur un des quatre points de joueurs (`ct.Joueurs`, selon l'identifiant du joueur), regard vers le nord. Effet de passage : gemmes vers le portail de la grotte, puis depuis le centre de l'arche de retour (`CentreRetourTerrasses` : le donjon en terrasses n'a pas de `PortalVisual`, le passage des autres joueurs se joue « sur place »). Bourdonnement du portail sur le repère de retour.
- **Portail de retour → carte** : `PassagePortail` posé sur le repère `PortailRetour` (touche Interagir à 3 m), sortie au **pied de l'escalier de la grotte** (`DonjonJeu.PiedEscalier` : bord de marche de `Grotte_Escalier` le plus loin du portail + 1,5 m, sur le NavMesh ; dos à la grotte), or versé à la caisse comme avant. Sans grotte (ancienne carte), sortie inchangée devant le portail du village.
- **Caméra** : `CameraEpaule.Enceinte` = la pièce cachée où est le héros, sinon la grande salle (faces intérieures des murs, du sol à 0,4 m sous la naissance : la voûte n'a pas de collision), `null` dans le passage d'une arche à travers l'enceinte. `CameraEpaule.DecoupeSansTraverser` (vrai ici) : la découpe autour du héros reste active, mais la caméra **ne traverse plus les murs** (recul contre les collisions, comme au village) ; seuls les piliers libres et les parapets (`VueLibre`) sont traversés et découpés par le shader `Deathless/VertexColorLitDecoupe`. L'ancien donjon garde sa caméra qui passe à travers les murs (`DecoupeSansTraverser` faux). `DonjonMasquage` n'est pas utilisé (pas d'étages superposés) ; ses capteurs restent posés, sans effet.
- **Gardiens** : `PoserGardiensTerrasses` pose un squelette par point d'apparition du plan (14), du type du plan (`TypeApparition` → `TypeEnnemi` sbire, guerrier, voleur, mage), tourné comme le point, en gardien (`Squelette.Garder`). Ils sont sur le NavMesh construit et le suivent escaliers compris : contrôlé en Play (graines 9, 12, 21 : chemin complet vers le héros pour les 14 ; des gardiens du rez montent les deux volées jusqu'au Lvl 2).
- **Coffres** : `CoffreDonjon` (touche Interagir), couvercle qui bascule, or porté, comme les coffres actuels (modèles provisoires du constructeur).
- **Pièces fermées, aperçu seulement** (`MecanismeApercu`) : porte à serrure ouverte par la touche Interagir (« Ouvrir la porte (aperçu, sans clé) », à 3 m), bouton mural par la touche (« Appuyer sur la pierre ») ou en s'en approchant (déclencheur d'origine) ; plaque de pression sous les pas (`CharacterController`). Grondement (`SonsDuJeu.GolemCoup`) quand une paroi secrète descend. `PorteDonjon.Ouvrir()` et `DeclencheurDonjon.Activer()` restent les seules entrées.
- **Graines (outil de dev, aperçu seulement)** : **F8** graine suivante, **Maj + F8** graine au hasard (`DonjonJeu.ChangerGraine`, autorité ; le héros au donjon revient à l'arrivée) ; message du HUD à l'entrée et à chaque changement. Aucune commande du jeu normal n'est touchée (F8 n'est lié à rien dans `DeathlessControls`).
- **Jour / nuit et mobs (outil de dev, aperçu seulement, 02/10/2026)** : **F9** bascule jour / nuit, **F10** bascule sans mob / avec mob (`DonjonJeu.ToucheApercu`, même lecture directe du clavier et même garde `Partie.Exploration` que F8 ; message du HUD à chaque appui : « Nuit », « Jour », « Mobs : désactivés », « Mobs : activés »). États dans `Partie.NuitApercu` et `Partie.SansMobApercu` : ils tiennent pendant les allers-retours par les portails, s'appliquent aux graines de F8 et sont remis à zéro au chargement de la scène. *F9* n'est qu'une ambiance : la phase de jeu reste le jour figé (aucune vague, aucun effet de la nuit sur la partie) ; `VueCycle` fait un fondu d'une seconde par `CycleJourNuit.nuitForcee` (lanternes, fenêtres, brume, lune, Nyxessa, lucioles suivent comme pour une vraie nuit), change la musique, garde le portail ouvert ; la visière du héros s'abaisse, le HUD écrit « Nuit · aperçu » (`IEtatApercu`) ; l'éclairage du donjon (posé après le cycle) reste inchangé. *F10* : « sans » désintègre tous les ennemis vivants (`DirecteurVagues.RetirerTous`) et `DirecteurVagues.Poser` refuse toute apparition (gardiens des nouveaux donjons compris) ; « avec » repose les gardiens aux 14 points et, si le héros est hors du donjon, quatre squelettes d'essai (trois sbires et un guerrier, gardiens de poste) à 8 m devant lui, car la carte extérieure n'a aucun ennemi dans l'aperçu (choix fait ici, à confirmer). Rien n'est lié dans `DeathlessControls`, le jeu normal ne lit pas ces touches.
- **Tests** (`ScenariosDonjon`, Play) : `Construire(graine)` et `Terrasses()` (plan, portes, déclencheurs, gardiens : état, NavMesh, chemin vers le héros), cibles `escalierN`, `hautN`, `terrasseN`, `pieceN`, `porteN`, `archeN`, `declencheurN`, `joueurN` pour `Aller` et `Parcourir`, `Cadrer(ou, vers, tangage, capture)`. Pour lancer l'aperçu depuis l'éditeur : Play puis `Partie.OuvrirCarteExploration()`.

**Vérifié en Play** (02/10/2026) : aller-retour par les portails (trois fois, après changements de graine compris), marche sur les trois niveaux par les escaliers (graines 9, 12, 21, 30, 444951 ; caméra jamais dans un mur ni le héros caché, sauf 12 images où la caméra frôle de 5 cm l'angle d'une terrasse au pied d'un escalier encastré), coffres, porte d'or (graine 9), paroi secrète au bouton (12) et à la plaque (30), F8, gardiens, aucune erreur ni avertissement en console. Captures `Assets/Screenshots/donjon_apercu_*.png`.

**Ce qui reste** :

- **Arrivée** (corrigée le 02/10/2026, à surveiller au jeu) : à 10 m d'arrivée, trois à six gardiens tombaient sur le joueur dans les huit premières secondes (un héros de 150 PV est mort ainsi en test). Deux causes : (1) `distanceArriveeApparitions` passée de 10 à 16 m (valeur retenue, la plus haute essayée qui garde les 5 000 graines conformes avec 14 apparitions ; empreintes des graines 1 à 50 identiques sous Unity (Mono) et .NET 10) ; (2) le **voleur gardien** repérait à 14 m (`voleurDistanceChasse`) à travers les murs, hors laisse, et alertait ses voisins : un gardien voleur n'utilise plus que `gardienDetection` (12 m), la ligne de vue et la laisse, comme les autres gardiens (`Voleur.ChoisirCible`, `JoueurIsole` ; l'ancien donjon n'a pas de voleurs gardiens, aucun effet ailleurs). Vérifié en Play (graines 9, 12, 21, par le portail) : le gardien le plus proche est à 13,5 à 14 m du héros, personne ne bouge pendant 15 s à l'arrivée, puis deux ou trois gardiens (sbire, guerrier, voleur) répondent dès que le héros s'avance à 10 m de l'un d'eux ; aucune erreur en console. Si ce reste trop dur, reculer encore (17 m laisse 5 000 graines conformes ; au-delà, de plus en plus de plans refaits).
- **Réseau** : l'aperçu est solo (`Partie.Exploration` refuse le multijoueur). La graine passe déjà par `PartieReseau.GraineDonjon` et le plan est identique partout, mais l'ouverture des portes et parois (`PorteDonjon.Ouvrir`) n'est pas répliquée, ni F8.
- **Gameplay des pièces fermées** : clés au mécano, crochetage, avantage de l'assassin ; vrais modèles de coffres du jeu sur les repères de butin.
- **Escaliers encastrés** : en haut d'un escalier encastré dans sa terrasse, le NavMesh (agent Humanoid) relie le côté de la volée à la terrasse par une marche de 0,4 m que le héros ne monte pas : un bot qui suit le NavMesh en ligne droite s'y bloque (le joueur, lui, monte par la volée). Sans effet sur les squelettes. Piste : obstacle ou modificateur NavMesh le long des côtés des volées.
- Portail de retour sans `PortalVisual` (nappe verte du constructeur) : le passage des autres joueurs se joue sur place ; un vrai portail de gemmes dans l'arche serait plus lisible.
- Allègement possible si besoin : faces arrière et dessous des blocs déjà retirés ; on peut encore réduire les assises au-dessus de 6 m (vues de loin).
