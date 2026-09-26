# Ennemis

Les ennemis sont des **squelettes**, issus de la force de Nyxessa, qui viennent la récupérer (voir [L'univers](univers.md)). Ils sortent de terre dans les trois clairières de la forêt, au nord, au sud-est et au sud-ouest, puis marchent sur le village et sur Nyxessa.

## Apparition {effet validé}

Un squelette **sort de terre** : il remonte du sol avec une gerbe de mottes de terre. Voir [Le village](village.md) pour la position des clairières.

## Mort {effet validé}

Un squelette vaincu se **désintègre** en gemmes couleur os qui montent et s'éteignent. La même désintégration touche les squelettes encore debout quand la nuit se termine, à l'aube.

## Types de squelettes {décidé}

{dev} Les modèles viennent du pack KayKit Skeletons.

| Modèle | Allure | Rôle |
|---|---|---|
| Guerrier | Heaume à cornes, arme de mêlée | Lent et solide, frappe fort, bloque les joueurs |
| Mage | Chapeau pointu, bâton | Reste à distance et tire le missile crâne |
| Voleur | Capuche, lames | Rapide, contourne et vise les joueurs isolés |
| Sbire | Sans casque, le plus simple | Nombreux et fragiles, foncent sur Nyxessa |

Ordre d'apparition dans la partie : sbires dès la nuit 1, guerriers nuit 2, voleurs nuit 3, mages (lanceurs de crâne) et premier élite nuit 5, mages plus nombreux nuit 6, Morgrim, le Roi des os (mini-boss) nuit 10, Nécromancien (boss final) nuit 12. Voir [Déroulé d'une partie](deroule.md) {décidé}.

{dev} Le casque du squelette guerrier sert aussi de modèle au heaume du rugissement du viking.

## Yeux {décidé}

Les squelettes ordinaires ont les **yeux jaune-orangé lumineux**, comme les modèles KayKit. Les **élites** ont les **yeux rouges**. **Nyxar** a les **yeux verts** : il porte des éclats de Nyx.

## Élites {décidé}

Un élite est un squelette ordinaire **plus fort**, sans éclat de Nyx (décision du 25/09/2026) :

- environ **1,3 fois plus grand** ;
- **trois fois plus de points de vie** et des dégâts plus forts ;
- **yeux rouges** et légère **aura rouge**, pour les distinguer.

Valeurs exactes : {à équilibrer}.

## Boss {décidé}

| Nuit | Boss | Allure |
|---|---|---|
| 10 | **Morgrim, le Roi des os**, mini-boss (le Golem) | Grand squelette massif, hache géante |
| 12 | **Nyxar, le Nécromancien**, boss final, ancien possesseur de Nyxessa (voir [L'univers](univers.md)) | Couronne à crâne, robe violette, grimoire, grande faux et faucille, **yeux verts** qui brillent de la force de Nyxessa |

### Morgrim, le Roi des os {décidé}

Colosse très résistant et lent, il marche droit sur Nyxessa. Chaque attaque se prépare longtemps, pour laisser le temps de parer ou d'esquiver :

- **Balayage** de hache en arc devant lui ;
- **Coup écrasé** au sol, qui fait une onde de choc autour de lui ;
- **Cri** qui renforce les squelettes proches.

**Déclinaisons à créer** {décidé} : le mini-boss existera en **deux versions**, l'une armée d'une **massue** (boule à pointes), l'autre d'une **martache** (hache-marteau), avec des **comportements et des compétences différents**. Les yeux **bleu glacé** de la martache la distinguent de la massue (yeux jaune-orangé habituels) au premier coup d'œil. Détail des deux versions : {à confirmer}.

{dev} Les deux armes existent telles quelles dans le pack KayKit Skeletons EXTRA, à l'échelle du Golem (rig Large) : `Skeleton_Mace_Large` (massue) et `Skeleton_Golem_Axe_Large` (martache — c'est déjà la hache géante du Morgrim actuel : elle porte une tête de marteau au dos de la lame en croissant, donc une vraie hache-marteau sans rien à fabriquer). Aucune arme générée. La distinction des yeux n'est pas une texture alternative du corps (les deux versions gardent `skeleton_texture_A`) mais un second matériau émissif bleu glacé pour les yeux, sur le modèle du rouge des élites (`Yeux_Elite.mat`).

#### Massue : colosse qui contrôle la zone {à confirmer}

Frappes larges et lentes, pense en zone plutôt qu'en cible : elle punit les groupes serrés autour de Nyxessa et les joueurs qui restent au contact.

- **Fracas** : frappe la boule à pointes au sol devant lui ; onde de choc en cercle qui **étourdit** (statut [Étourdi](statuts.md)) tout joueur pris dedans. Reprend le mécanisme du coup de zone actuel du Golem (`Golem.Frapper`, `OndeDeChoc`), thème **Terre**.
- **Tourbillon** : fait tournoyer la boule autour de lui sur 360°, dégâts continus et léger recul pour qui reste dans le rayon ; oblige à sortir de la mêlée le temps du tour. Thème **Terre**.
- **Charge écrasante** : fonce en ligne droite sur sa cible et **renverse** (étourdit) le premier joueur touché, comme la charge bélier du [Paladin](classe-paladin.md) mais sans parade possible en cours de charge. Thème **Terre**.

#### Martache : colosse qui tranche et vise juste {à confirmer}

Coups plus rapides et plus précis que la massue, avec une compétence dédiée à percer la défense de Nyxessa plutôt qu'à contrôler la zone.

- **Fauche** : coup en cône devant lui avec le tranchant de la hache, touche tous les joueurs dans l'arc. Thème **Rage** (accents fer, comme le rugissement du viking).
- **Fend-sol** : saut court suivi d'une retombée qui plante l'arme droit devant lui et fend le sol en ligne ; la fissure **ralentit** (statut [Ralenti](statuts.md)) les joueurs qui restent dedans. Thème **Terre** pour la fissure (c'est le sol qui casse, pas l'arme), portée réduite par rapport au Fracas de la massue (une ligne, pas un cercle).
- **Coup de brèche** : frappe du côté marteau de l'arme, tournée vers le bouclier de [Nyxessa](vfx.md) plutôt que vers les joueurs : inflige des dégâts renforcés à la paroi du bouclier quand il est levé (voir `Docs/vfx.md`, Bouclier de la relique). Thème **Rage**.

{dev} Prototypes dans le bac à sable `sandbox-rig` (scène `Assets/Scenes/Morgrim.unity`, outils `Assets/Editor/Morgrim/MorgrimBuilder.cs` et `MorgrimCaptures.cs`) : les deux mannequins posés et équipés, poses d'attente et pose clé de chaque compétence échantillonnées sur les clips du rig Large (`Melee_2H_Slam`, `Melee_1H_Slash`, `Melee_2H_Attack` pour la massue ; `Melee_Dualwield_SlashCombo`, `Melee_1H_Stab`, `Melee_Block_Attack` pour la martache — approximations, aucun clip n'est écrit spécifiquement pour ces coups). Effets principaux prototypés dans le langage gemmes (`Assets/VFX/Morgrim/MorgrimEffets.cs`, gemmes `LowPolyGem` / shader `Relic/VertexColorUnlit`, palettes Terre et Rage). Captures : `Assets/Screenshots/morgrim_massue_*.png`, `morgrim_martache_*.png`, planche `morgrim_planche.png`. Pas encore reporté dans `main`.

Points de vie et dégâts : {à équilibrer}.

### Nyxar, le Nécromancien {décidé}

Invocateur qui combat à distance :

- il **garde ses distances** et **se téléporte** quand on l'approche ;
- il tire des **salves de crânes** ;
- il **relève des squelettes** du sol autour de lui ;
- il **fauche à la faux** ceux qui le serrent de près ;
- ses **deux éclats de Nyx brillent**, dans le crâne de sa couronne et dans celui de son grimoire à la ceinture : ce sont ses **points faibles** {décidé}.
  - Chaque éclat se **brise** sous les coups. Chaque éclat brisé lui retire **un tiers de sa puissance** {décidé}.
  - Il ne peut être **tué qu'une fois ses deux éclats brisés** {décidé}.
  - Vie des éclats et effet précis de la perte de puissance : {à équilibrer}. {{dev: Pas encore dans la version 0.1.}}

**Trois phases**, selon les éclats brisés {décidé} :

| Phase | Éclats | Combat |
|---|---|---|
| 1 | Les deux intacts | Kit complet : distance, téléportation, salves de crânes, squelettes relevés, faux de près |
| 2 | Couronne brisée | Plus de téléportation ni de squelettes relevés |
| 2 | Grimoire brisé | Plus de salves de crânes |
| 3 | Les deux brisés | Enragé, il se bat au corps à corps à la faux ; il devient tuable |

Points de vie, dégâts et cadence : {à équilibrer}.

## Mage

- Le mage squelette tire un **missile en forme de crâne** fait de gemmes {effet validé}. {{dev: Il s'appelait « nécromancien » avant que ce nom ne soit réservé au boss final.}}
- C'est le même missile que celui de Nyxessa, à taille normale ; celui de Nyxessa est une fois et demie plus gros {décidé}.
- Modèle : le squelette mage KayKit {décidé}. Sa vie et son comportement sont {à confirmer}.

## Comportement et détection

- **Cible prioritaire** {décidé} : les squelettes marchent vers Nyxessa. Un joueur qui les frappe, ou qui passe à moins de 4 m, devient leur cible pendant quelques secondes, puis ils reprennent leur route. Le voleur fait exception : il chasse les joueurs isolés. Distance et durée {à équilibrer}.
- **Détection de l'assassin furtif** {décidé} : cône de vue d'environ 6 m devant le squelette, 1,5 m dans son dos. Voir [Classes](classes.md).
- **Valeurs de départ** de la version 0.1 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Ennemi | Vie | Vitesse | Dégâts | Autre |
|---|---|---|---|---|
| Sbire | 100 | 3,4 m/s | 8 | prépare son coup 0,7 s |
| Guerrier | 160 | 3,0 m/s | 14 | prépare son coup 0,8 s |
| Élite | ×3 | | ×1,5 | 1 par nuit aux nuits 5 et 6, 2 dès la nuit 7 |
| Morgrim | 1 500 | 2 m/s | 45 en zone, 60 sur Nyxessa | rayon 3 m, prépare son coup 1,6 s |
| Nyxar | 1 200 | | 18 par crâne, toutes les 3 s | reste entre 12 et 18 m ; relève 3 sbires toutes les 15 s, 12 au plus |

- **Composition des vagues** {à équilibrer} : vagues de 30, 35 et 35 % des squelettes de la nuit ; avec quatre vagues, 22, 24, 26 et 28 %. La part de guerriers passe de 0 % la nuit 1 à 50 % dès la nuit 5.
- **Or rapporté** par squelette tué, versé à la caisse commune (règle provisoire en attendant le donjon, voir [Déroulé d'une partie](deroule.md)) {à équilibrer} :

| Ennemi | Or |
|---|---|
| Sbire | 5 |
| Guerrier, voleur | 8 |
| Mage | 10 |
| Élite | 25 |
| Morgrim | 150 |
| Nyxar | 300 |
- {dev} Dans la version 0.1, voleurs et mages sont encore joués comme des guerriers, et les élites sont des guerriers renforcés.
