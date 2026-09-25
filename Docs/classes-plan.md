# Classes jouables : plan (phase A)

Rédigé le 25/09/2026 par l'agent gameplay, **sans toucher à `Assets/` ni à l'éditeur** (l'agent ui pilote main). La
construction (phase B) attend le signal du coordinateur.

Sources lues : `Wiki/pages/classes.md` (version du 25/09 après-midi : pas d'ultime, mana, rage, roulade arrière, passifs
et détection de l'assassin, valeurs de départ du rôdeur et de l'assassin), `Wiki/pages/commandes.md` (plus d'ultime ni
d'accroupissement, R3 libre, LB + RB = compétence 3), `Docs/vfx.md` (fiches et API des effets de classe),
`Docs/styles-d-armes.md`, `Assets/WeaponStyles/*` (assets, contrôleurs), `Assets/Scripts/Dev/VfxBench.cs` (instants clés
mesurés des gestes), le code de la 0.1 (`Assets/Scripts/Jeu/`, commit b53f854) et les changements en cours de l'agent ui
(retrait de `Ultimate` / `Crouch` et de l'accord L3 + R3 dans `DeathlessControls` et `InputChordResolver`).

---

## 1. Architecture commune

### 1.1 Ce qui ne change pas

La 0.1 sépare déjà l'état de la partie (`Partie`, `EtatPartie`) de l'affichage, et les entrées passent toutes par
`InputChordResolver.Triggered` (`HerosEntrees`). On garde tel quel : `Partie`, `DirecteurVagues`, `DefenseNyxessa`,
`MissileCrane`, `Sante` / `InfoDegats`, `CameraEpaule`, `VueCycle`, `AudioBank` / `SonsDuJeu`, `HudPresenter` (étendu),
`MortAllie`, les outils de test (`EntreesSimulees`, `ScenariosTest`, `DevPartie`).

### 1.2 Le héros commun et la classe

`Heros` (aujourd'hui tout le Paladin dans une classe) est coupé en deux :

| Composant | Rôle |
|---|---|
| `Heros` (commun à toutes les classes) | vie, endurance, déplacement relatif à la caméra, sprint, saut, **esquive** (B / Ctrl, commune), gravité, pas, mort (dissolution `MortAllie`) et réapparition, invulnérabilité, rotation, visière (si le modèle en a une), paramètres d'animation communs (`Speed`, `Grounded`, `Dead`, `Jump`, `Dodge`, `Hit`, `Respawn`), couche « haut du corps » pilotée par poids, recopie dans `EtatJoueur`. Il reçoit les actions de `HerosEntrees` et **délègue** tout le reste à la classe. |
| `ClasseHeros` (abstraite) | ce qu'une classe ajoute : `SurAction(nom)` (AttackPrimary, AttackSecondary, Skill1, Skill2, Skill3), `SurMaintien(garde, attaque)` (les maintiens lus chaque image : bander l'arc, cône, garde, tournante, visée), `Mettre à jour(dt)`, `Intercepter(coup)` (garde du Paladin), `ModificateurVitesse`, `DeplacementBloque`, `OrientationVisee` (le héros se tourne vers la caméra), `Jauge` (aucune, mana ou rage) et les **emplacements du HUD**. Une sous-classe par classe : `ClassePaladin`, `ClasseMage`, `ClasseRodeur`, `ClasseAssassin`, `ClasseViking`. |

Le code du Paladin de la 0.1 (garde, parade, charge bélier, soin) passe tel quel dans `ClassePaladin`, sans changement
de comportement (le scénario de test de la 0.1 doit donner les mêmes résultats).

### 1.3 Données d'une classe : `ClasseDef` + `GameBalance`

- **`ClasseDef`** (ScriptableObject, `Assets/Jeu/Classes/<Classe>.asset`) : identifiant (`paladin`, `mage`, `rodeur`,
  `assassin`, `viking`), nom affiché, teinte du portrait, modèle KayKit, `WeaponStyle`, contrôleur d'animation de jeu,
  prefab du héros, type de jauge, liste des emplacements du HUD (action, nom, abréviation, icône). Identité et
  apparence seulement.
- **`GameBalance`** : toutes les valeurs chiffrées (une section par classe, § 3), comme demandé. Les valeurs communes
  (endurance, esquive, saut, sprint) restent partagées ; chaque classe a ses PV et sa vitesse.
- Registre `ClassesJeu` (liste des cinq `ClasseDef`, dans `Resources`) : `Partie.LancerSolo(classeId)` y trouve le
  prefab ; la classe choisie est gardée pour « Rejouer » (drapeau statique, comme `LancerAuChargement`).

### 1.4 Briques partagées (nouvelles)

| Brique | Utilisée par | Rôle |
|---|---|---|
| `Frappe` (statique) | toutes | cibles d'une frappe de mêlée (arc devant, portée, demi-angle, une ou toutes les cibles), application des dégâts avec `sourceId`, score (`CompterDegats`), son d'impact, critique. |
| `Visee` | mage, rôdeur, assassin | rayon depuis le centre de l'écran (réticule de l'agent ui) jusqu'au décor ou à un ennemi, en ignorant le héros ; donne le point visé et la direction de tir depuis la main ou l'arme. |
| `ProjectileJeu` | rôdeur (flèches, salve), assassin (repli), mage (boule) | projectile rectiligne (flèches, carreaux) ou en cloche légère (boule de feu : +1,5 m/s, chute 5, comme Relic) ; balayage par `SphereCast` image par image ; à l'impact, dégâts, **tir à la tête** (voir ci-dessous), rappel pour l'effet. Les flèches utilisent le modèle `arrow_bow` et la **traînée d'air** `TraineeAir` (non magique, sans lueur) ; la boule utilise `FireballVisual` puis `ExplosionFeu`. |
| `Critique` (règle) | rôdeur, assassin | calcul du multiplicateur et du type (normal, critique, meilleur), `InfoDegats.critique`, compteur `coupsCritiques` du score, effet `Critique.Signaler(point, direction, meilleur)` et son `coup_critique` / `coup_critique_meilleur`. |
| `Brulure` | mage | composant posé sur l'ennemi touché : dégâts par seconde pendant la durée (rafraîchie par un nouveau coup), effet `BurnFlammeches` parenté au centre du squelette, son `brulure` en boucle ; arrêt à la mort ou à la désintégration. |
| `JaugeClasse` | mage (mana), viking (rage) | valeur, maximum, gain et perte par seconde, coût ; exposée au HUD. |

### 1.5 Ce qui change chez les ennemis

- **Tête** : `Squelette.Tete` (os `head` du modèle, rayon selon l'échelle) pour les tirs à la tête (rôdeur, arbalète).
  Un projectile qui touche la capsule d'un squelette est un tir à la tête si son rayon passe à moins de ~0,3 m (× échelle)
  du centre de la tête.
- **Dos** : un coup est « dans le dos » si l'attaquant est derrière le squelette (angle > 120° entre l'avant du
  squelette et la direction squelette → attaquant).
- **Détection de l'assassin furtif** (wiki) : `JoueurProche` ignore un assassin furtif sauf s'il est dans le **cône de
  vue** (±60°, 6 m) ou à moins de **1,5 m** dans le dos. Un squelette qui le repère le fait sortir du mode furtif (son
  `assassin_repere`). Personne ne voit un héros qui est **dans la fumée** d'une grenade (`Fumigene.Contient`).
- **Provocation** (rugissement du viking) : `Squelette.Provoquer(heros, durée)` : cible forcée, priorité sur Nyxessa.
- **Brûlure** (mage) : via le composant `Brulure` ; aucune autre modification de l'IA.
- **Étourdissement** : déjà là (saut percutant du viking).
- Le Golem et le Nécromancien (boss, noms du wiki : Morgrim et Nyxar) suivent les mêmes règles (tête plus haute pour
  le Golem).

### 1.6 Entrées (table du wiki, `DeathlessControls`)

| Action | Manette | Clavier-souris | Paladin | Mage de feu | Rôdeur | Assassin | Viking |
|---|---|---|---|---|---|---|---|
| Attaque principale | RT | clic gauche | épée | boule de feu | **maintenir : bander**, relâcher : tirer | dague (ou carreau si l'arbalète est en main) | hache |
| Attaque secondaire | LT | clic droit | garde, parade | **cône de flammes (maintenu)** | visée (zoom) | **arbalète en main et visée (maintenu)** | **attaque tournante (maintenue)** |
| Compétence 1 | LB | A | charge bélier | — | nuée de flèches | grenade fumigène | rugissement |
| Compétence 2 | RB | R | soin sur soi | — | roulade arrière + salve | — | saut percutant |
| Compétence 3 | LB + RB | F | — | — | — | — | — |
| Esquive | B | Ctrl | commune à toutes les classes | | | | |
| Sprinter | L3 | Maj | commun (l'assassin sort du mode furtif) | | | | |

- Le wiki ne donne que la boule, le cône et la brûlure au mage : ses compétences 1 et 2 restent vides dans cette
  version. La brûlure est l'effet de ses coups, pas une touche. C'est une **question pour Quentin** (voir § 5).
- Pas d'ultime, pas d'accroupissement : `HerosEntrees` n'écoute plus `Ultimate` ni `Crouch` (l'agent ui les retire de
  l'asset). R3 reste libre.
- Les maintiens (bander, cône, tournante, visée, garde, arbalète) sont lus par `resolver.IsHeld(action)`, jamais par les
  actions brutes.

### 1.7 Personnages et animation

- **Modèles** (pack KayKit Adventurers 2.0, squelette Rig_Medium, sockets natifs) : Paladin = `Knight` (déjà dans main),
  Rôdeur = `Ranger` (déjà dans main), Mage = `Mage`, Assassin = `Rogue_Hooded` (capuche : lecture « furtif »),
  Viking = `Barbarian`. Les trois derniers sont à copier de Relic avec leurs `.meta` (`Characters/fbx/`, textures
  comprises), après vérification de la synchronisation NAS de Relic et un scan des GUID. Les matériaux partagés
  `KayKit_Mage/Rogue/Barbarian.mat` existent déjà dans `Assets/Art/Materials/`.
- **Équipement** : `MannequinEquip.Equiper(modèle, style)` avec l'asset `WeaponStyle` de la classe (`Staff`,
  `BowQuiver`, `DaggerCrossbow`, `Axe2H`). Bascules du style : `BowStance` (arc repos / visée, flèche encochée,
  blendshape `Draw`) et `AltWeaponSwitch` (dague / arbalète).
- **Contrôleurs de jeu** construits par `JeuBuilder`, un par classe, sur le modèle de `Paladin_Jeu` : les clips viennent
  du `WeaponStyle` (règle « style → animation »), les états communs (saut, esquive, touché, mort, réapparition) des clips
  génériques. Les contrôleurs de référence de `Assets/WeaponStyles/` ne sont pas modifiés. Les noms d'états et de
  paramètres que `BowStance` et `AltWeaponSwitch` attendent (`Draw`, `Aiming_Idle`, `Aiming`, `Crossbow`) sont repris.
- **Instants clés** : ceux mesurés par `VfxBench` (boule `TempsTir` ≈ 0,31 s, poussée du cône ≈ 1,57 s, estoc 0,5 s,
  lâcher de grenade 0,75 s, tir d'arbalète 0,35 s, décocher 0,05 s, saut percutant décollage 0,19 s / atterrissage 0,69 s /
  impact 0,79 s, cri 1,63 s, tournante `Commencer` 0,55 s). En jeu, les clips d'attaque sont accélérés et l'instant est
  mis à l'échelle.

### 1.8 Interface (HUD et choix de classe)

- **Choix de classe** : l'agent ui fournit l'appel (`LancerSolo(classeId)` ou équivalent) ; `HudPresenter` le relaie à
  `Partie.LancerSolo(classeId)`.
- **Emplacements** : `HudPresenter` construit la liste `ICompetenceHud` depuis la `ClasseDef` du héros (liste stable
  pendant la partie) ; état et recharge lus dans `ClasseHeros`.
- **Jauge de classe** : mana (mage) ou rage (viking), exposée par l'interface que l'agent ui ajoutera. Rien pour les
  autres classes.
- **À demander à l'agent ui** : un indicateur « furtif » pour l'assassin (état d'un emplacement ou petit pictogramme),
  la jauge de charge de l'arc (le cercle de charge 3D suffit peut-être), la teinte des portraits (proposée d'après les
  palettes : Paladin or `#d9b264`, Mage feu `#ff610a`, Rôdeur ocre de la chasse `#d9b45a`, Assassin lilas de l'ombre
  `#a58ad6`, Viking rouge de la rage `#b3261e`).

---

## 2. Les classes

Toutes les valeurs sont **à équilibrer** sauf mention « wiki ». Les sons sont des ids du catalogue
(`Wiki/data/sons.json`) avec un repli s'il manque.

### 2.1 Paladin (inchangé)

Épée (RT), garde et parade (LT), charge bélier (LB), soin sur soi (RB). Code déplacé dans `ClassePaladin`, sans autre
changement. Emplacements du HUD : Ép, Ga, Ch, So.

### 2.2 Mage de feu (bâton, `Staff`, modèle `Mage`)

| Action | Comportement | Effet | Son |
|---|---|---|---|
| Boule de feu (RT) | lancée vers le point visé ; explose à l'impact (dégâts directs + zone) ; allume la **brûlure** sur chaque ennemi touché ; chaque ennemi touché rend du mana (wiki) | `FireballVisual.Attach(…, cœur vif)` en vol, `ExplosionFeu.Jouer(point, 2.5, PortalVoxel)` à l'impact ; clip `Ranged_Magic_Shoot`, boule créée au bout du bâton à t ≈ 0,31 s | `fireball_cast`, `fireball_flight_loop` (boucle attachée), `fireball_explosion` |
| Cône de flammes (LT maintenu) | tant que la touche est tenue **et** qu'il reste du mana (wiki) : dégâts par seconde dans un cône devant le bâton, allume ou rafraîchit la brûlure ; le mage se déplace lentement et se tourne vers la visée | `ConeDeFlammes` parenté à la pointe du bâton (0 ; 1,2 ; 0 dans `staff`), `Stop()` au relâchement ; clip `Ranged_Magic_Spellcasting_Long` jusqu'à la poussée puis pose tenue (copie bouclée) sur la couche du haut du corps | `mage_flame_cone_loop` (boucle) |
| Brûlure (passif des coups) | dégâts par seconde, durée rafraîchie à chaque coup | `BurnFlammeches` sur le squelette | `brulure` (boucle, à écouter) |
| Mana (jauge, wiki) | 100 ; +1 par seconde ; bonus par ennemi touché par la boule ; le cône consomme en continu | jauge du HUD | — |

Valeurs de départ :

| Réglage | Valeur |
|---|---|
| Vie, vitesse | 100, 5 m/s |
| Boule de feu | 25 dégâts directs + 15 dans un rayon de 2 m, une toutes les 0,9 s, 18 m/s, portée 30 m |
| Mana | 100, +1/s (wiki « environ 1 »), +4 par ennemi touché par la boule |
| Cône | 14 mana/s, 22 dégâts/s, portée 6 m (celle de l'effet), demi-angle 20°, vitesse du mage ×0,4 |
| Brûlure | 5 dégâts/s pendant 3 s, rafraîchie à chaque coup (pas de cumul) |

Emplacements du HUD : Bo (RT), Cô (LT, « Active » tant qu'il brûle, « Indisponible » sans mana).

### 2.3 Rôdeur (arc et carquois, `BowQuiver`, modèle `Ranger`)

| Action | Comportement | Effet | Son |
|---|---|---|---|
| Bander et tirer (RT maintenu, puis relâché) | charge 0 → 1 en **1,2 s** (wiki) ; au relâchement, flèche vers le point visé ; dégâts **10 → 40** selon la charge (wiki) ; **tir à la tête ×2** et critique (wiki) ; le rôdeur avance lentement en bandant | `ArcBande.Bander(flèche encochée)`, `Charge` chaque image (cercle de charge qui se resserre, verrouillage et éclat à 100 %), `Relacher(départ, cible, impact)` (flèche avec traînée d'air) ; `BowStance` (arc levé, blendshape `Draw`) ; clips `Ranged_Bow_Draw` → `Ranged_Bow_Aiming_Idle` → `Ranged_Bow_Release` (lâcher t = 0,05 s) ; `Critique.Signaler` au tir à la tête | `arc_bander` au début, `arc_charge_complete` à 100 % (ou `sonPret` de `ArcBande`, pas les deux), `bow_shot_v3` / `bow_shot_v3_charged` au tir, `arrow_impact` |
| Visée (LT maintenu) | caméra serrée (champ de vision réduit, épaule plus proche), vitesse ×0,6 | `CameraEpaule` (nouveau réglage de visée) | — |
| Compétence 1 : Nuée de flèches (LB) | la zone suit la visée (point au sol à 25 m au plus) ; au lâcher, pluie sur la zone ; dégâts répartis sur la durée de la pluie | `NueeDeFleches.Jouer(centre, rayon)` (marqueur Chasse, flèches non magiques) ; clips `Ranged_Bow_Draw_Up` → `Ranged_Bow_Release_Up` | `nuee_marqueur` puis `arrow_rain` |
| Compétence 2 : Roulade arrière (RB) | roule en arrière (4 m) pour reprendre ses distances, invulnérable au début, et tire **en même temps** une salve de flèches en éventail devant lui | pas d'effet dédié (consigne) : flèches `arrow_bow` normales avec leur traînée d'air (`ProjectileJeu`) ; clip `Dodge_Backward` (pack MovementAdvanced) | `dodge`, `bow_shot_v3` ×N, `arrow_impact` |

Valeurs de départ :

| Réglage | Valeur |
|---|---|
| Vie, vitesse | 110, 5 m/s (×0,5 en bandant, ×0,6 en visée) |
| Arc | charge **1,2 s**, **10 → 40** dégâts, tête **×2** (wiki) ; 30 m/s ; 0,3 s au moins entre deux tirs |
| Nuée | rayon 3 m, 5 salves de 10 dégâts sur 1,2 s (50 au total par ennemi resté dedans), portée 25 m, recharge 12 s |
| Roulade arrière | 4 m en 0,45 s, invulnérable 0,3 s, salve de 5 flèches sur ±20°, 15 dégâts chacune (tête ×2), recharge 8 s, 20 d'endurance |

Emplacements du HUD : Ar (RT, « Active » en bandant), Vi (LT), Nu (LB), Ro (RB).

### 2.4 Assassin (dague, arbalète dans le dos, `DaggerCrossbow`, modèle `Rogue_Hooded`)

| Action | Comportement | Effet | Son |
|---|---|---|---|
| Marche discrète et furtif (passif, wiki) | **hors combat** (aucun coup donné, reçu ni repéré depuis 4 s), se déplacer sans sprinter fait passer en marche discrète : vitesse réduite, **furtif** ; sprinter, attaquer ou être repéré fait sortir du mode furtif ; dans la fumée d'une grenade, il redevient furtif même en combat | `ModeFurtif.Entrer()` / `Sortir()` ; paramètre `Sneaking` et état `Sneak` (clip `Sneaking`) | `assassin_furtif`, `assassin_furtif_sortie`, `assassin_repere` |
| Dague (RT) | estoc rapide, une cible ; multiplicateurs du wiki : furtif non détecté ×2, dans le dos ×3, les deux ×5 (meilleur critique) | clip `Melee_1H_Attack_Stab` (coup à t = 0,5 s, accéléré) ; `Critique.Signaler(point, direction, meilleur)` | `kenney_rpg_knifeslice`, `coup_critique` / `coup_critique_meilleur` |
| Arbalète (LT maintenu, RT pour tirer) | LT fait passer l'arbalète en main et vise (la dague va dans le dos), RT tire un carreau ; **seul critique : la tête** (les passifs ne s'appliquent pas aux carreaux, wiki) ; **recharge 6 s** (wiki) ; relâcher LT remet la dague en main | `AltWeaponSwitch` (bool `Crossbow`), `Carreau.Tirer(départ, cible, impact)` (traînée d'air courte) ; clips `Ranged_1H_Aiming`, `Ranged_1H_Shoot` (t = 0,35 s), `Ranged_1H_Reload` | `crossbow_shot_v3`, `arbalete_recharge`, `arrow_impact`, `coup_critique` |
| Grenade fumigène (LB) | lancée vers le point visé (8 m au plus) ; nuage d'environ 5 s ; les ennemis perdent de vue qui est dedans ; l'assassin y redevient furtif ; **une grenade, qui revient 20 s après** (wiki) | `Fumigene.Tenir(main)` puis `Lancer(départ, cible, 0,6)` au lâcher (clip `Throw`, grenade en main à 0,1 s, lâchée à 0,75 s) ; `Contient(point)` | `grenade_lancer`, `smoke_bomb` |

Valeurs de départ :

| Réglage | Valeur |
|---|---|
| Vie, vitesse | 100, 5 m/s ; marche discrète 3,2 m/s |
| Dague | 20 dégâts, un coup toutes les 0,55 s, portée 1,8 m, demi-angle 45°, une cible |
| Multiplicateurs | ×2 furtif, ×3 dos, ×5 les deux (wiki) ; « dans le dos » : angle > 120° |
| Détection | cône ±60° jusqu'à **6 m**, **1,5 m** dans le dos (wiki) |
| Hors combat | 4 s sans coup donné, reçu ni repérage |
| Arbalète | 45 dégâts, tête ×2 (critique), 45 m/s, portée 40 m, **recharge 6 s** (wiki) |
| Grenade | **une**, **recharge 20 s**, nuage **5 s** (wiki), portée 8 m, rayon 2,4 m (celui de l'effet) |

Emplacements du HUD : Da (RT), Ar (LT, recharge 6 s), Fu (LB, recharge 20 s). L'indicateur « furtif » est à convenir avec
l'agent ui (§ 1.8).

### 2.5 Viking (hache à deux mains, `Axe2H`, modèle `Barbarian`)

| Action | Comportement | Effet | Son |
|---|---|---|---|
| Coup de hache (RT) | alternance `Melee_2H_Attack_Chop` / `Melee_2H_Attack_Slice`, frappe **toutes** les cibles dans l'arc (rôle mêlée, zone) ; chaque ennemi touché donne de la rage | — (physique, pas de gemmes) | `kenney_rpg_chop`, `skeleton_hit` |
| Attaque tournante (LT maintenu, wiki) | tant que la touche est tenue, le viking tourne et frappe tout autour de lui à intervalle régulier ; consomme de la rage en continu, s'arrête quand la rage est vide (wiki) ; il peut se déplacer lentement | `AttaqueTournante.Commencer(porteur, TeteHache)` / `Arreter()` ; clips `Melee_2H_Attack_Spin` (début, `Commencer` à t = 0,55 s), `Melee_2H_Attack_Spinning` en boucle tant que c'est tenu, fin du Spin (`Arreter` à t = 1,4 s) | `whirlwind_loop` (boucle) |
| Compétence 1 : Rugissement (LB) | provoque les squelettes proches : ils se tournent vers le viking et le prennent pour cible pendant quelques secondes (« venez », wiki) | `Rugissement.Jouer()` (crâne de barbare en gemmes, onde aller-retour), placé 0,32 s avant la tête rejetée ; clip `Skeletons_Taunt_Longer` (cri à t ≈ 1,63 s) | `viking_roar` |
| Compétence 2 : Saut percutant (RB) | bond d'environ 5 m vers la visée (wiki), frappe au sol : dégâts de zone et bref étourdissement | `OndeDeChoc_SautPercutant` au point d'impact ; clip `Melee_1H_Attack_Jump_Chop` (décollage 0,19 s, atterrissage 0,69 s, impact 0,79 s), racine translatée par script (5 m + arc de 0,6 m), comme au banc | `viking_leap_land` |
| Rage (jauge, wiki) | 100 ; monte quand il frappe ; redescend lentement hors combat | jauge du HUD | — |

Valeurs de départ :

| Réglage | Valeur |
|---|---|
| Vie, vitesse | 140, 5 m/s |
| Hache | 38 dégâts, un coup toutes les 1,1 s, portée 2,4 m, demi-angle 70°, toutes les cibles |
| Rage | 100 ; +8 par ennemi touché (hache, tournante, saut) ; −6/s après 4 s sans frapper |
| Tournante | 20 rage/s, un coup toutes les 0,3 s dans un rayon de 2,3 m, 12 dégâts, vitesse ×0,6 ; il faut au moins 15 de rage pour la lancer |
| Rugissement | 25 de rage, recharge 12 s, rayon 10 m, provocation 5 s |
| Saut percutant | 35 de rage, recharge 8 s, 5 m, rayon d'impact 3,5 m, 45 dégâts, étourdissement 1 s |

Emplacements du HUD : Ha (RT), To (LT, « Active » en tournant, « Indisponible » sans rage), Ru (LB), Sa (RB).

---

## 3. `GameBalance` : sections ajoutées

Une section par classe avec les valeurs du § 2 (PV et vitesse compris). Les réglages communs restent : endurance,
esquive, saut, sprint, réapparition, caméra (plus un réglage de visée : champ de vision, épaule, distance). Toutes ces
valeurs seront listées dans le rapport de la phase B pour le wiki (« à équilibrer »).

---

## 4. Ordre de construction (phase B, sur le signal)

0. Relire les interfaces de l'agent ui (choix de classe, jauge) et l'état de `DeathlessControls` (Ultimate et Crouch
   retirés) ; vérifier la synchronisation de Relic ; scanner les GUID des personnages à copier.
1. Copier `Mage`, `Rogue_Hooded` (et `Rogue`), `Barbarian` depuis Relic avec leurs `.meta`.
2. Découper `Heros` / `ClassePaladin` ; relancer les scénarios de la 0.1 (combat, charge, parade, mort) : mêmes
   résultats attendus.
3. Briques communes : `ClasseDef`, registre, `Frappe`, `Visee`, `ProjectileJeu`, règle `Critique`, `JaugeClasse`,
   `Brulure` ; tête, dos, détection, provocation dans `Squelette`.
4. Une classe à la fois, dans cet ordre : **Viking** (le plus proche du Paladin, mêlée), **Mage** (projectile, jauge,
   brûlure), **Rôdeur** (charge de l'arc, visée, nuée, roulade), **Assassin** (furtif et détection, arbalète, fumée).
   Pour chacune : contrôleur, prefab, `ClasseDef`, compétences, sons, emplacements du HUD, puis un scénario de test.
5. `Partie.LancerSolo(classeId)`, branchement du choix de classe et de la jauge de l'agent ui, Rejouer avec la même
   classe.
6. Vérification en Play par la manette virtuelle, **captures `Assets/Screenshots/classes_*.png`** :
   - mage : boule qui explose et brûle, cône maintenu jusqu'à épuisement du mana ;
   - rôdeur : arc bandé (cercle de charge, puis verrouillé), tir à la tête critique, nuée, roulade avec salve ;
   - assassin : approche furtive puis meilleur critique dans le dos, repérage dans le cône de vue, arbalète à la tête,
     grenade puis retour furtif dans la fumée ;
   - viking : coup de zone, tournante tenue jusqu'à la rage vide, rugissement qui attire, saut percutant ;
   - pour chaque classe : une nuit accélérée, la mort et la réapparition, le HUD avec ses emplacements et sa jauge.
7. Rapport : jouable, contrôles par classe, valeurs choisies, manques, captures, `git status`. Pas de commit.

---

## 5. Risques et questions

1. **Recompilation partagée** : rien n'est écrit dans `Assets/` avant le signal. Pendant la phase B, un seul agent dans
   l'éditeur.
2. **Découpage du Paladin** : c'est le plus gros changement de code ; les scénarios de la 0.1 servent de garde-fou.
3. **Visée à la troisième personne** : le tir part de l'arme vers le point visé par la caméra ; de près, l'écart
   caméra / arme peut faire toucher un obstacle proche. Repli : si le point visé est à moins de 2 m, tir droit devant
   le héros.
4. **Tir à la tête** : estimé par la distance du rayon au centre de la tête (pas de collider par os). À régler sur
   chaque modèle (sbire, guerrier, Golem, Nécromancien).
5. **Furtif dans une horde** : avec 40 à 60 squelettes, l'assassin sera repéré souvent. Les distances du wiki (6 m,
   1,5 m) sont à juger en jeu.
6. **Animations en mouvement** : bander, cône, tournante et arbalète se jouent sur la couche du haut du corps pendant
   que les jambes marchent. Le masque `HautDuCorps` existe, à valider pour l'arc (bras gauche tendu).
7. **Modèles de Relic** : synchronisation NAS à vérifier avant la copie (note de reprise).
8. **Question pour Quentin (une seule)** : le Mage de feu n'a, d'après le wiki, que la boule (attaque principale) et le
   cône (attaque secondaire) : ses compétences 1 et 2 restent-elles vides dans cette version ?

## Visée des armes de tir et balistique (25/09/2026)

- **Alignement sur la visée** : les poses KayKit de tir tournent l'arme par rapport au corps (arc bandé : la ligne de tir partait ~63° à gauche du réticule ; arbalète : ~25° à droite). `ClasseHeros.AxeDeTir` donne la ligne de tir posée par l'animation (rôdeur : main de la corde → main de l'arc, en bandant ; assassin : axe avant de `crossbow_1handed`, arbalète en main) ; `Heros.LateUpdate` en mesure le décalage de lacet et tourne le corps d'autant (le rôdeur se met de profil, l'arc et la flèche dans l'axe du réticule), puis penche le buste (`chest`, ±40°) pour suivre le tangage de la visée. Le décalage est gardé 0,45 s après le lâcher. Bâton du mage : écart de 10 à 15° pendant le lancer, jugé acceptable (non corrigé). Captures : `visee_rodeur_avant*.png`, `visee_rodeur_apres*.png`, `visee_assassin_*.png`, `visee_mage_baton.png`.
- **Balistique** (wiki classes.md, « Projectiles ») : flèches et carreaux partent à une vitesse donnée et subissent la pesanteur (`GameBalance.projectileGravite` 9,81 m/s²), orientés le long de leur trajectoire (traînée d'air conservée). Arc : `arcVitesseMin` 18 → `arcVitesseMax` 55 m/s selon la charge ; salve de la roulade `salveVitesse` 35 m/s ; arbalète `arbaleteVitesse` 60 m/s ; nuée : chute balistique dans l'effet `NueeDeFleches` (`gravite` 9,81). Aide à la visée : relèvement plafonné à `aideChuteMax` 3° vers l'angle qui touche le point visé (`ProjectileJeu.DirectionBalistique`) ; au-delà, le joueur vise au-dessus. Vol maximal `projectileVolMax` 200 m.
- Mesures (`ScenariosClasses.Lancer("balistique")`, guerrier figé, visée au centre de la tête) : arc chargé à fond → tête à 15 m et 30 m (écart 0,01 et 0,04 m, flèche au plus 0,6 m au-dessus de la ligne), 45 m → corps (0,92 m sous la tête) ; tir rapide (0,25 s) → 0,72 m sous la tête à 15 m, la flèche retombe vers 17 m ; arbalète → tête à 15, 30 et 45 m (0,26 m sous le centre à 45 m). Capture : `balistique_tir_rapide.png`.
