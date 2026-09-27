# Audit du donjon (27/09/2026)

Audit **sans correction** du donjon du projet `main`, en Play dans `Assets/Scenes/Village.unity` (éditeur piloté par MCP, partie solo Paladin), avec le scénario de dev `Assets/Scripts/Jeu/Dev/ScenariosDonjon.cs` (construire une graine, analyser le plan, téléporter, parcourir par le NavMesh avec surveillance de la caméra, horloge, captures, performances). Captures 1920 × 1080 dans `Assets/Screenshots/audit_donjon_NN*.png`. Graines parcourues : 141337, 541640, 288567, 463801, 464927, 457966 (tirées par la partie) et 111, 2222, 33333, 4444, 55555 (imposées, 5 graines analysées). Scène non modifiée (`dirty=False` à la sortie du Play), console sans erreur ni avertissement sur toute la session.

**Non testé** : le réseau à deux postes (voir « Non testé »), les sons (non audibles par l'agent ; les appels et les clips sont listés), les coffres à clé (aucun code de clé dans le projet à cette date : `DonjonJeu.CadenasActifs = false`, aucune classe de clé, tous les coffres s'ouvrent sans clé conformément au wiki).

## Les cinq points de Quentin

Ils passent en tête ; ils sont repris dans la liste par gravité plus bas.

### 1. « Pièce à ramasser » — gênant, correctif petit

- **Ce qui se passe** : après l'ouverture d'un coffre (or crédité, couvercle basculé), le coffre reste ouvert **plein de pièces d'or** : le modèle `Assets/Art/Coffres` (grand coffre et coffre sans serrure) contient un tas de pièces à l'intérieur, qui reste visible une fois l'or pris. Le joueur voit de l'or « encore à ramasser » et revient dessus ; l'invite a disparu et rien ne se passe. C'est la lecture la plus probable du retour. Deux autres candidats, moins probables : les pièces `PieceOr` qui montent 0,9 s au-dessus du coffre puis disparaissent (rien à ramasser, mais elles ressemblent à un objet) ; les tas d'or (`coin_stack_*`) qui se ramassent en marchant dessus et non par E (sans invite : on peut tourner autour sans comprendre).
- **Vérifié** : l'or des coffres est bien crédité (50 / 50 / 120) et celui des gardiens n'existe pas (`Squelette.OrRapporte = 0` pour un gardien, `Partie.GagnerOr` sort à `montant <= 0` : aucune pièce ne s'affiche à leur mort ; mesuré : 0 `PieceOr` en vol après avoir tué les 6 gardiens). Aucune pièce ne tombe au sol, aucune ne reste.
- **Reproduire** : n'importe quelle graine, ouvrir le grand coffre (`ScenariosDonjon.Coffre(0)`), regarder le coffre 2 s après.
- **Capture** : `audit_donjon_05_grandcoffre_1.png` (coffre ouvert, 360 or portés, tas de pièces toujours dans le coffre), `audit_donjon_03_coffre1_1.png`.
- **Cause probable** : `DonjonJeu.Ouvrir` (coroutine) fait basculer le couvercle (`_lid`) et joue `PieceOr`, mais ne masque pas le contenu du modèle ; le modèle de coffre livre ses pièces en dur.
- **Correctif** : petit — modèle de coffre vide (ou enfant « pièces » désactivé à l'ouverture dans `Ouvrir`, réactivé dans `PreparerButins`).

### 2. « Des balcons plus que des niveaux » — gênant, correctif gros (conception)

- **Ce qui se passe** : le niveau 1 est presque entièrement un **balcon d'une cellule de large** (4 m) qui court le long du mur d'enceinte, plus deux mezzanines de 9 cellules ; le niveau 2 est une **plate-forme de 12 cellules** (la « tour ») entourée de balustrades, ouverte sur le hall. Rien ne se lit comme un étage : on est toujours sur une passerelle au-dessus du vide, avec le rez visible en contrebas.
- **Mesures** (5 graines) : rez 180 cellules pleines, niveau 1 62 à 67 cellules (dont ~50 de balcon périphérique), niveau 2 12 cellules ; 56 à 67 garde-corps (`barrier`) par donjon.
- **Captures** : `audit_donjon_43_g33333_vue_etage2.png`, `audit_donjon_49_g4444_vue_etage2.png`, `audit_donjon_55_g55555_vue_etage2.png` (depuis la plate-forme du 2e étage : balustrade, hall en dessous, balcon périphérique en face), `audit_donjon_21_etage2.png`, `audit_donjon_02_passage_1.png` (arrivée : le balcon fait le tour du hall).
- **Cause** : `DonjonPlan` (constantes de la « taille unique ») : `NbMezzanines = 3` dont 1 tour ; niveau 1 rempli par la règle « balcon le long des murs d'enceinte + plancher complet des mezzanines » (`(CellulePourtour(c) && t != Entree) || Mezzanine || Tour`, ~ligne 239) ; niveau 2 = la tour seule. Le wiki (`deroule.md`) demandait « un vrai 2e étage plein ».
- **Correctif** : gros — revoir le plan (étages pleins sur plusieurs blocs, balcons réservés aux halls à double hauteur), puis le masquage des étages et le chemin critique qui en dépendent.

### 3. « Des squelettes qui spawnent dans des crânes » — cosmétique, correctif petit

- **Ce qui se passe** : le plan pose un ossement (`bone_A`, `bone_B`, `bone_C` ou `skull`, au hasard) **exactement sur chaque point d'apparition** ; les gardiens sont posés sur ces points ; un gardien sur quatre sort donc de terre à travers un crâne, puis reste planté dedans à son poste.
- **Mesures** : gardien ↔ ossement à **0,04 m** sur toutes les graines (guerrier sur `bone_C` graine 111, sur `ribcage` 2222, sbire sur `skull` 33333 et 55555, sur `bone_A` 4444) ; 3 à 7 crânes par donjon sur 24 apparitions.
- **Captures** : `audit_donjon_17_eau.png` (sbire dans le bassin, ossement sous lui), `audit_donjon_18_cam_butin1_1.png` (crâne posé sur une grille : point d'apparition). Capture nette du squelette sortant du crâne non obtenue (le modèle est encore sous le sol pendant la première seconde de la sortie de terre).
- **Cause** : `DonjonPlan.Peupler`, « Ossements sur chaque point d'apparition » (~lignes 1078-1085 : `p = apparitions[i]; p.type = DecorOs`) + `DonjonJeu.PoserGardiens` (pose à `Apparitions[i].transform.position`).
- **Correctif** : petit — décaler l'ossement de 0,8 à 1,2 m du point (ou retirer `skull` de `kit.os`, ou poser le gardien à côté).

### 4. « Des portes et des fenêtres partout » — gênant, correctif moyen

- **Ce qui se passe** : chaque bord de mur (intérieur ou d'enceinte, tous niveaux) tire son modèle **au hasard** dans `kit.murs` = `wall ×3, wall_broken, wall_arched, wall_scaffold ×2, wall_doorway, wall_doorway_scaffold, wall_pillar` : 3 modèles sur 10 sont une porte ou une arche, 1 sur 10 un mur cassé (trou). Les murs d'enceinte des étages tirent en plus 1 fois sur 4 dans `kit.mursHauts` = `wall_window_closed ×2, wall_archedwindow_gated` (fenêtres sur le vide noir). Toutes ces ouvertures sont doublées d'une boîte de collision pleine (`Boite` 4 × 4 × 1) : porte fermée qui ne s'ouvre pas, porte sur le mur d'enceinte, arche bouchée, fenêtre sur le noir, trou sur le noir.
- **Comptage** (pièces posées par donjon, 5 graines) :

  | Graine | Portes (`doorway` + `doorway_scaffold`) | Arches | Fenêtres | Murs cassés | Murs pleins (`wall` + `wall_pillar`) |
  |---|---|---|---|---|---|
  | 111 | 22 | 12 | 29 | 15 | 55 |
  | 2222 | 26 | 16 | 22 | 13 | 55 |
  | 33333 | 25 | 20 | 24 | 13 | 54 |
  | 4444 | 20 | 17 | 28 | 14 | 61 |
  | 55555 | 33 | 12 | 28 | 10 | 59 |

  Soit environ **6 segments sur 10** qui montrent une ouverture (les `wall_scaffold`, 23 à 29, sont pleins mais habillés d'échafaudages).
- **Captures** : `audit_donjon_32_g111_porte_rez.png` (porte rouge fermée sur le mur d'enceinte, torche au-dessus), `audit_donjon_33_g111_fenetre.png` (fenêtre grillagée du balcon sur le noir, porte à gauche, trou au-dessus), `audit_donjon_35_g111_mur_casse.png` (trou du mur d'enceinte sur le noir), `audit_donjon_06_tasor.png` (portes sur deux niveaux), `audit_donjon_20_cam_butin0_1.png` (porte intérieure fermée), `audit_donjon_44_g33333_porte_rez.png`, `audit_donjon_50_g4444_porte_rez.png`.
- **Cause** : `DonjonGenerateur.PoserBord` : `GameObject m = b == Bord.MurExterieur && k > 0 && (h & 3) == 0 ? Choisir(kit.mursHauts, h >> 4) : Choisir(kit.murs, h >> 4);` puis `Prendre(m, …)` et `Boite(g, p, r, (0, H/2, 0), (C, H, 1))`, sans lien avec `ouvert[]` du plan ; `Assets/Donjon/DonjonKit.asset` (listes `murs`, `mursHauts`).
- **Correctif** : moyen — `Bord.Mur` / `MurExterieur` : murs pleins seulement (`wall`, `wall_pillar`, `wall_scaffold`) ; `wall_doorway` / `wall_arched` réservés aux passages réellement ouverts du plan (bord `Rien` entre deux cellules pleines, sans boîte) ; fenêtres et murs cassés retirés de l'enceinte tant qu'il n'y a rien derrière (ou décor de fond).

### 5. « Des demi-murs qui ne servent à rien » — cosmétique, correctif petit

- **Ce qui se passe** : 2 à 3 « murs bas » par donjon, entre deux halls voisins du rez, sur une seule cellule : un modèle de mur tiré au hasard dans `kit.murs` (donc parfois `wall_broken`, `wall_arched`, `wall_scaffold`, `wall_doorway`) **écrasé à mi-hauteur** (échelle Y 0,5), avec une boîte de 2 m. Ils ne ferment rien, ne couvrent rien, ne servent pas de couvert de combat, et une arche ou une porte écrasée à 2 m se lit comme un bug.
- **Mesures** : 2 murs bas (graines 141337, 457966), 3 (111, 2222, 33333, 4444, 55555) ; modèles écrasés vus : `wall`, `wall_scaffold`, `wall_arched`, `wall_broken`.
- **Captures** : `audit_donjon_34_g111_mur_bas.png` (bloc sombre au milieu du hall), `audit_donjon_46_g33333_mur_bas.png` (arche écrasée), `audit_donjon_52_g4444_mur_bas.png` (mur cassé écrasé), `audit_donjon_58_g55555_mur_bas.png`.
- **Cause** : `DonjonPlan.ChoisirMursBas` (`MaxMursBas = 3`, « ils découpent l'espace sans fermer les salles ») + `DonjonGenerateur.PoserBord`, `case Bord.MurBas : Prendre(Choisir(kit.murs, h >> 4), …, new Vector3(1f, 0.5f, 1f))`.
- **Correctif** : petit — les supprimer (`MaxMursBas = 0`) ou leur donner un vrai modèle de muret (fondation `floor_foundation_*`, `barrier`) et une fonction (couvert autour d'un butin).

## Défauts par gravité

### Bloquant

**B1. La caméra traverse les murs, entre dans les piliers et sort de l'enceinte** — correctif moyen.
- Au bord de la plate-forme du 2e étage (graine 111, héros à (1058.75, 8.00, 14.00) devant le grand coffre, lacet −71°, tangage 30°), la caméra se place à **(1064.7, 12.8, 13.2), 4,7 m au-delà du mur d'enceinte** (x max de l'emprise : 1063) : on voit la face extérieure de l'enceinte et la tête du héros qui dépasse du mur. Un rayon simple depuis (1058, 10, 14) vers +x touche bien la boîte du mur à x = 1059,5, mais le SphereCast de la caméra (rayon 0,25 m, depuis le pivot vers l'arrière) ne renvoie que deux touches à distance 0 (la boîte du grand coffre à 1,5 m et la capsule du héros) et **jamais le mur**.
- En parcours automatique par le NavMesh (4 trajets, 3 300 images), la caméra est mesurée **dans un collider non masqué** 3 à 4 images par trajet et le héros est caché 1 à 4 images ; recul minimal 0,55 à 0,78 m (caméra collée à l'épaule dans le balcon et sous les mezzanines). Deux captures montrent la caméra dans un pilier de bois (`column`) : le héros disparaît derrière un aplat beige.
- **Captures** : `audit_donjon_63_camera_enceinte.png`, `audit_donjon_31_g111_vue_etage2.png` (hors de l'enceinte), `audit_donjon_18_cam_butin1_1.png`, `audit_donjon_22_cam_eau_2.png` (dans un pilier), `audit_donjon_61_gardien_crane_sortie.png` (mur d'enceinte qui remplit le tiers gauche de l'image sur le balcon).
- **Reproduire** : `ScenariosDonjon.Construire(111); Aller("butin0"); Regarder(-71, 30)` ; parcours : `Parcourir("butin1", 45, "cap")` depuis l'arrivée (graine 464927).
- **Cause probable** : `CameraEpaule.Premier` ignore les touches à distance 0 (`h.distance > 0f`) et les balayages PhysX qui démarrent en chevauchement (boîte d'un décor à 1,5 m, capsule du héros) ne remontent pas de façon fiable les colliders suivants ; les trois tests (épaule → arrière, pivot → droite, pivot → caméra à rayon 0,12) partent tous du pivot (1,6 m au-dessus du héros, à l'intérieur de sa capsule). À confirmer au débogueur ; le fait est reproductible.
- **Correctif** : moyen — compléter les SphereCast par un `Linecast` sans rayon (pivot → caméra, tête → caméra) qui ne souffre pas du chevauchement initial, ignorer explicitement la capsule du héros par masque de couche plutôt que par `GetComponentInParent<Sante>`, et rapprocher la caméra dès qu'un `CheckSphere` la trouve dans un collider ; épaissir les boîtes des piliers (`column` : boîte 0,7 m pour un chapiteau plus large).

### Gênant

**G1. Message du donjon superposé à la bannière de phase** — correctif petit.
- « Rappelé par Nyxessa : 0 or gardés, 100 perdus », « 60 or versés à la caisse commune », « Tu ramasses le sac de X » s'affichent **par-dessus** « Crépuscule · la nuit tombe » / « Jour · 0:23 avant la nuit » : les deux textes se chevauchent et deviennent illisibles.
- **Captures** : `audit_donjon_15_rappel.png`, `audit_donjon_16_rappel_fin.png`, `audit_donjon_12_depot.png`.
- **Cause** : `Assets/UI/Screens/Hud/Hud.uss` ligne 428 : `.hud-donjon-message { position: absolute; top: 222px; left: 50% … }` — même hauteur que la pastille de phase ; `EcranHud` ne décale pas l'un quand l'autre est visible.
- **Correctif** : petit — placer le message sous la pastille de phase (ou sous l'alerte donjon), ou masquer la phase pendant le message.

**G2. Plafond noir dans le champ sous les mezzanines** — correctif moyen.
- Sous un plancher (coffre du rez « sous un plancher », allées couvertes), le tiers supérieur de l'image est un aplat noir : dessous des dalles du niveau 1, non éclairé, jamais masqué quand la caméra et le héros ne sont pas dans le même bloc couvert (masquage par bloc de 3 × 3 cellules : le bloc voisin garde son plancher).
- **Captures** : `audit_donjon_19_masquage_butin1.png`, `audit_donjon_10_sac_visuel.png`, `audit_donjon_30_g111_gardien_os.png`.
- **Cause** : `DonjonKit.plafond` vide (`{fileID: 0}`) : `DonjonGenerateur.PoserSols` ne pose aucune sous-face ; lumières sans ombre au-dessus des dalles, rien en dessous ; `DonjonMasquage.Recalculer` ne masque que le bloc du héros, celui de la caméra et les deux blocs diagonaux, et seulement si le bloc du héros est « couvert » (5 cellules sur 9).
- **Correctif** : moyen — dalle de plafond claire (`kit.plafond`), lumière d'appoint sous les mezzanines, et masquage étendu au bloc devant la caméra (direction de vue) même hors bloc couvert.

**G3. Trous et fenêtres sur le noir** — correctif petit (compris dans le point 4).
- Les `wall_broken` (10 à 21 par donjon, dont sur l'enceinte) et les fenêtres des étages montrent le fond de brouillard noir : le donjon paraît percé. Captures `audit_donjon_35_g111_mur_casse.png`, `audit_donjon_47_g33333_mur_casse.png`, `audit_donjon_53_g4444_mur_casse.png`, `audit_donjon_33_g111_fenetre.png`, `audit_donjon_03_coffre1_0.png`. Cause et correctif : point 4.

**G4. Pas de dalle d'arrivée : on tombe du ciel sur une grille d'égout** — correctif petit.
- `DonjonKit.dalleArrivee` est vide (`{fileID: 0}`) : `PoserDecor` (`DecorDalleArrivee`) ne pose rien, et la cellule d'arrivée reçoit le sol tiré au hasard, souvent `floor_tile_big_grate`. Le héros atterrit sur une grille noire ; rien ne signale le point d'arrivée.
- **Captures** : `audit_donjon_02_passage_2.png`, `audit_donjon_02_passage_3.png`, `audit_donjon_14_alerte.png`.
- **Correctif** : petit — renseigner `dalleArrivee` (et exclure la grille des sols de l'entrée).

**G5. Grilles au sol partout** — correctif petit.
- `kit.solsRez` = 7 dalles pleines + 1 `floor_tile_big_grate` tirée par cellule : 19 à 35 grilles noires par donjon, au hasard, y compris sous les tables, sous les points d'apparition et à l'arrivée. Elles se lisent comme des trous ou des trappes et aplatissent la lecture du sol.
- **Captures** : `audit_donjon_02_passage_1.png`, `audit_donjon_20_cam_butin0_1.png`, `audit_donjon_36_g111_zone_sombre.png`.
- **Cause** : `DonjonGenerateur.PoserSols` (`Choisir(kit.solsRez, h >> 3)`), `Assets/Donjon/DonjonKit.asset`.
- **Correctif** : petit — retirer la grille des sols aléatoires ; la poser en décor voulu (une par bloc au plus).

**G6. Un point d'apparition dans un décor de coin** — correctif petit.
- Sur 3 graines sur 7, un point d'apparition (`Apparition_Voleur_*`, niveau 1, cellule de coin) est **dans la boîte d'un décor de coin** (tonneaux, caisses : boîte 1,6 × 1,5 × 1,6 ou 2,1 × 1,0 × 2,1). Un gardien posé là sort de terre dans les tonneaux et son agent NavMesh se retrouve coincé (constaté en analyse, pas de gardien tiré dessus pendant l'audit : les gardiens vont aux 6 points les plus proches des butins).
- **Mesure** : `ScenariosDonjon.Analyser()` : « apparition 10 (Apparition_Voleur_11) dans Boite (1.6, 1.5, 1.6) à (1001.3, 4.0, 46.7) » (graine 457966), idem 2222 (apparition 13), 111 (apparition 19).
- **Cause** : `DonjonPlan.Peupler` : les décors de coin (`DecorCoin`) marquent la cellule (`m_Occupe` quarts) mais `ApparitionDansRegion` teste `(m_Occupe[no] & 16)` (centre) et `m_Utilise` : un point d'apparition posé avant le décor, ou le décor collé dans le coin de la cellule d'apparition, se chevauchent.
- **Correctif** : petit — exclure des décors de coin les cellules d'apparition (ou tester la distance).

**G7. Performances au donjon (éditeur)** — correctif moyen, à surveiller en build.
- Hall d'arrivée : **30,1 ms par image** (min 21,7), 847 000 triangles, 1,37 M sommets, **2 431 batches**, 44 SetPass ; plate-forme du 2e étage, vue sur le hall : 26,1 ms, 639 000 triangles, 2 174 batches, 40 SetPass. Mesures dans l'éditeur (Game 1920 × 1080), sans comparaison village dans cet audit. 1 330 à 1 370 objets actifs, 36 lumières ponctuelles sans ombre (Forward+ : pas de limite par objet).
- **Cause** : une pièce KayKit par cellule et par bord, aucun batching statique (objets réutilisés d'un donjon à l'autre), pas de LOD, murs à détails saillants.
- **Correctif** : moyen — `StaticBatchingUtility.Combine` par groupe après génération (les groupes ne bougent pas jusqu'au donjon suivant), ou instancing ; réduire les pièces d'habillage (`wall_scaffold`, `wall_open_scaffold` : 47 à 57 par donjon).

**G8. Gardien de la tour posé au bord du vide** — à confirmer, correctif petit.
- Sur la plate-forme du 2e étage (graine 457966), le guerrier gardien est posé à (1025.55, 8.08, 37.70) tourné vers l'arrivée : la cellule devant lui n'a pas de plancher (cellule (5, 9) vide aux niveaux 1 et 2, pas de NavMesh). Un héros qui le contourne par là tombe de 8 m (renversé + ralenti : PV 150 → 96, mesuré en s'y téléportant). Pas de garde-corps constaté à cet endroit sur la capture ; à revérifier graine par graine (`Bord` `GardeCorps` attendu au bord d'un plancher).

### Cosmétique

**C1.** Coffre ouvert plein de pièces (point 1). **C2.** Murs bas écrasés (point 5). **C3.** Ossements sous les gardiens (point 3). **C4.** Balcons (point 2 : gênant, mais la lecture d'ensemble est cosmétique ; le cœur est la conception du plan).

**C5. « Prêts 0 / 1 · F1 » affiché au donjon** — le vote est bien inactif (`BasculerPret` ignoré, `pret` reste faux : `audit_donjon_60_vote_donjon.png`), mais l'invite F1 reste affichée sans retour. Correctif petit (`EcranHud` : cacher « Prêts » quand `DonneesUI.Donjon.AuDonjon`).

**C6. Pénombre du bassin** — la cellule la plus éloignée de toute lampe est dans le bassin (13,3 m, graine 111 ; 47 à 58 cellules à plus de 8 m d'une lampe par donjon). Lisible grâce à l'ambiance (0,30), mais terne : `audit_donjon_36_g111_zone_sombre.png`. Correctif petit (torchère au bord du bassin : `DonjonPlan.PlacerTorches` n'en pose pas dans le bloc bassin).

**C7. Portail de retour posé devant une porte** — le portail de gemmes (contre le mur sud de l'entrée) se retrouve devant une `wall_doorway` de l'enceinte (`audit_donjon_38_g2222_porte_rez.png` a été retirée, capture polluée par un héros mort ; reproduire avec la graine 2222). Correctif : point 4.

**C8. Torches murales dans les fenêtres / au-dessus des portes** — `PlacerTorches` pose une torche par région sur un « mur plein » du plan, mais le modèle tiré peut être une porte ou une fenêtre : torche au-dessus d'une porte fermée (`audit_donjon_32_g111_porte_rez.png`). Correctif : point 4.

**C9. Squelette invisible pendant la première seconde de la sortie de terre** — au lever du jour, les 6 gardiens sont « SortieDeTerre » mais leur modèle est sous la dalle ~1 s (montée sur 1,2 s) : sans effet de terre visible à distance, un joueur qui arrive tôt voit des ossements, puis un squelette planté. Cosmétique, lié au point 3.

**Non constaté** : z-fighting (aucun scintillement sur 63 captures statiques ; les murs se recouvrent aux angles mais un pilier (`pillar`, 16 par donjon) couvre chaque angle), torche sans lumière (35 torches, 36 lampes : une par torche + le portail), coffre ou gardien dans un mur (0 sur 7 graines), salle ou butin inaccessible (0 sur 7 graines), trou dans le sol (aucun ; le seul « trou » est le vide au bord de la tour, G8).

## Ce qui marche (pour ne pas le refaire tester)

| Point | Vérifié | Mesure / capture |
|---|---|---|
| Invite au portail du village | « Entrer dans le donjon » à ≤ 3 m, le jour | `audit_donjon_01_invite_village.png` |
| Refus la nuit | aucune invite, aucun passage | `audit_donjon_13_portail_nuit.png` |
| Refus dans la dernière seconde et demie | 1,3 s restantes : aucune invite ; 1,6 s : invite | `InvitePortail`, marge `DureeTransitDepart + 0,4` |
| Départ en gemmes, téléportation, chute du ciel, contrôle | téléporté à 1,13 s, corps visible à 1,64 s, clip `Spawn_Air` vu à 1,64 s, contrôle rendu à 2,94 s (spec : 1,1 + 0,5 + 1,3) ; identique au retour (`Spawn_Ground`) | `audit_donjon_02_passage_0..3.png`, `audit_donjon_08_retour_0..3.png` |
| Portail de retour | disque de gemmes vertes, toujours ouvert, contre le mur sud de l'entrée, bourdonnement (`AudioBank.Boucle`) | `audit_donjon_14_alerte.png`, `audit_donjon_07_invite_retour.png` |
| Invite du retour | « Revenir au village » (libellé du wiki) | `audit_donjon_07_invite_retour.png` |
| Retour : sortie 5 m devant le portail côté Nyxessa, or versé | caisse 20 → 80 pour 60 portés, message « 60 or versés à la caisse commune » | `audit_donjon_12_depot.png` |
| Coffres sans serrure, gratuits | « Ouvrir le coffre » / « Ouvrir le grand coffre », couvercle basculé en 0,45 s, pris à 0,07-0,10 s | `audit_donjon_03_coffre1_*.png`, `04`, `05` |
| Montants | coffre 50, coffre 50, grand coffre 120, tas d'or 20 (nuit 1) ; or porté 0 → 50 → 100 → 220 → 240 | journal `[Partie] Donjon : Joueur prend …` |
| Un coffre ne s'ouvre qu'une fois | 2e appui : invite absente, `InteragirIci` faux, or inchangé | idem |
| Tas d'or en marchant dessus | pris à 1,4 m, pièces et son | `audit_donjon_06_tasor.png` |
| Or porté au HUD | « N or porté · au donjon » sous la caisse, visible au donjon même à 0 | toutes les captures au donjon |
| Alerte 15 s | pastille rouge « Le portail se ferme dans 14 s : rentrez au village ! », `AvantRappel` 13,5 s, son `NyxessaAlerte` joué une fois | `audit_donjon_14_alerte.png` |
| Rappel au crépuscule | dissolution sur place, arrivée près de Nyxessa avec `Spawn_Ground`, « Rappelé par Nyxessa : 0 or gardés, 100 perdus » (palier 1 : 0 %), or porté 100 → 0 | `audit_donjon_15_rappel.png`, `16` (message illisible : G1) |
| Sac à la mort | créé à la position de mort (0,15 m), modèle `sack` recalé sur le NavMesh, pièces qui scintillent, montant = tout l'or porté (20, 60, 380) | `audit_donjon_09_sac_mort.png` (écran de mort) ; journal |
| Ramassage du sac | en marchant dessus, « Tu ramasses le sac de Joueur : 60 or », or porté +60, sac retiré | `audit_donjon_11_sac_ramasse.png` |
| Sac perdu au crépuscule | « le portail se ferme, 1 sac(s) perdu(s) (380 or) », liste vidée | journal |
| Mort au donjon | « Vous êtes tombé — Réapparition dans N s », retour au village ; le sac reste | `audit_donjon_09_sac_mort.png` |
| Eau | facteur 0,6 (3,1 m/s mesurés contre 5 m/s), icône escargot au HUD (statut Ralenti d'origine Eau, sans durée), sortie sans blocage, NavMesh « Eau » | `audit_donjon_17_eau.png`, `audit_donjon_23_eau_arrivee.png` |
| Gardiens | 6 par donjon (2 guerriers, 4 sbires), sur le NavMesh, aux apparitions les plus proches des butins ; sortie de terre 1,6 s ; poursuite dès 8 m ; dégâts 8 (sbire) / 14 (guerrier) ; retour au poste en < 3 s quand le héros s'éloigne (abandon à 15 m) ; pas d'or ni de pièce à leur mort ; désintégrés au crépuscule ; aucun bloqué ni dans un mur sur 7 graines | `audit_donjon_27_gardien_poursuite.png`, `ScenariosDonjon.Gardiens()` |
| Masquage des étages | 2 groupes masqués sous un bloc couvert (héros et caméra dans le même bloc), rien de masqué à découvert ; capteurs posés une fois | `DonjonMasquage.NbMasques` |
| Ambiance | sombre, torches, sans ciel (fond brouillard), identique de jour et de nuit | toutes les captures |
| Génération | 7 graines : plan valide, 3 escaliers sur 3 vérifiés par le NavMesh, 7 butins et le portail de retour accessibles, chemin critique 205 à 249 m (consigne 180-250), construit en 20 à 60 ms ; l'essai retenu part au client (`EssaiCourant` 0 à 5 ; graine 463801 : essai 2 ; 4444 : essai 5, 1 relance) | `Analyser()`, journal |
| Vote « prêt » | inactif tant qu'un joueur est au donjon (`pret` reste faux) | `audit_donjon_60_vote_donjon.png` |
| Console | 0 erreur, 0 avertissement sur toute la session (deux entrées en Play, ~35 min) | `read_console` |

## Non testé et limites

- **Réseau à deux postes** : non fait. La méthode documentée (`Docs/reseau.md`, « Tests sans fenêtre ») demande une **copie du projet à chemin court** pour lancer un second éditeur en `-batchmode -nographics` ; cette copie n'existe pas sur ce poste (aucun `.exe` de client, `Builds/publish` ne contient que des zips) et sa création (copie d'Assets + import de la bibliothèque) prend trop longtemps pour cet audit. Restent à vérifier à deux postes : donjon identique (graine + essai), coffre ouvert vu par le client (`ButinsPris`), sacs (`NetworkList<SacReseau>`), animations de portail des autres (`TransitDistant` + `NetworkAnimator`), dépôt et rappel côté client. À la lecture du code, un point à surveiller : `DonjonJeu.TransitDistant` cache la marionnette sans délai de sécurité si l'effet 204 n'arrive pas (héros distant invisible jusqu'au prochain passage).
- **Sons** : non audibles par l'agent. Les appels existent et pointent sur des clips listés dans `Assets/Scripts/Jeu/Audio/SonsDuJeu.cs` : `PortailPassage`, `PortailArrivee`, `PortailChuteCiel` / `PortailSortieSol` (joués vers 1 s dans le clip), `PortailBourdon`, `CoffreOuvert`, `Or`, `NyxessaAlerte`, `NyxessaRappel`. `PortailFermeRefus` est défini mais **jamais joué** (aucun retour sonore quand on appuie sur E devant un portail fermé : l'invite est simplement absente).
- **Coffres à clé** : inexistants dans ce projet (voir en-tête).
- **Captures polluées** : pendant les graines 2222 et une partie de 111, le héros était mort (tué par les gardiens) ; ces captures ont été refaites (111) ou supprimées (2222). Les captures `audit_donjon_25/26` (héros tombé de la tour) ont été supprimées.

## Tableau de synthèse par graine

| Graine | Essai | Chemin | Niveau 1 / 2 (cellules) | Portes | Arches | Fenêtres | Cassés | Murs bas | Crânes | Grilles | Apparition dans un décor |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 141337 | 0 | 240 m | 62 / 12 | 35 | 8 | 27 | 10 | 2 | 3 | 24 | non |
| 457966 | 0 | 236 m | 67 / 12 | 16 | 13 | 23 | 21 | 2 | 6 | 19 | 2 |
| 111 | 4 | 205 m | 67 / 12 | 22 | 12 | 29 | 15 | 3 | 3 | 31 | 1 |
| 2222 | 3 | 247 m | 67 / 12 | 26 | 16 | 22 | 13 | 3 | 4 | 35 | 1 |
| 33333 | 1 | 205 m | 65 / 12 | 25 | 20 | 24 | 13 | 3 | 4 | 21 | non |
| 4444 | 5 | 219 m | 65 / 12 | 20 | 17 | 28 | 14 | 3 | 7 | 19 | non |
| 55555 | 2 | 249 m | 66 / 12 | 33 | 12 | 28 | 10 | 3 | 6 | 20 | non |

## Fichiers

- Scénario de dev laissé en place : `Assets/Scripts/Jeu/Dev/ScenariosDonjon.cs` (aucun effet hors Play).
- Captures : `Assets/Screenshots/audit_donjon_01_invite_village.png` à `audit_donjon_63_camera_enceinte.png` (68 fichiers).
- Aucun fichier de jeu modifié ; `Village.unity` non touchée.
