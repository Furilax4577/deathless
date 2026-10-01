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

**Coup en préparation** {décidé, 26/09/2026} : pendant qu'un squelette prépare son coup, ses **yeux s'intensifient** (émission ×1 à ×3,2), avec une légère **pulsation** dans le dernier tiers pour marquer l'instant de l'impact — pour tous les squelettes, **Morgrim compris**. La teinte des yeux ne change pas (jaune-orangé, rouge élite, bleu glacé Morgrim martache) : c'est l'intensité seule qui monte, jamais de vert. {dev} `PreparationLisible.cs`, posé automatiquement par `Squelette.Awake` sur chaque squelette ; lu depuis `Squelette.PreparationProgress`.

## Élites {décidé}

Un élite est un squelette ordinaire **plus fort**, sans éclat de Nyx (décision du 25/09/2026) :

- environ **1,3 fois plus grand** ;
- **trois fois plus de points de vie** et des dégâts plus forts ;
- une portée d'attaque un peu plus longue (+0,3 m) {à équilibrer} ; {{dev: (codé en dur dans `DirecteurVagues.Poser`, pas dans `GameBalance`)}}
- **yeux rouges** et légère **aura rouge**, pour les distinguer ;
- une **barre de vie toujours visible** au-dessus de la tête, là où les squelettes ordinaires ne la montrent que blessés (voir [Interface](interface.md#barres-de-vie-des-ennemis), 26/09/2026).

Valeurs exactes : {à équilibrer}.

## Boss {décidé}

| Nuit | Boss | Allure |
|---|---|---|
| 10 | **Morgrim, le Roi des os**, mini-boss (le Golem) | Grand squelette massif, hache géante |
| 12 | **Nyxar, le Nécromancien**, boss final, ancien possesseur de Nyxessa (voir [L'univers](univers.md)) | Couronne à crâne, robe violette, grimoire, grande faux et faucille, **yeux verts** qui brillent de la force de Nyxessa |

**Apparition** {décidé, 30/09/2026} : le boss sort de terre dans une clairière active **au début de la dernière vague** de sa nuit, annoncé par une bannière à son nom et son cri ; **le jour ne se lève qu'à sa mort** (la nuit se prolonge tant qu'il vit, voir [Déroulé d'une partie](deroule.md)).

Un boss a sa **méga barre de vie dans le HUD**, sous celle de Nyxessa, avec son nom et ses statuts ; elle se déploie à son entrée en scène (voir [Interface](interface.md#barres-de-vie-des-ennemis), 26/09/2026).

### Morgrim, le Roi des os {décidé}

Colosse très résistant, **offensif** {décidé, 01/10/2026} : il **chasse les joueurs** (celui qui vient de le frapper, sinon le plus proche) et ne marche sur Nyxessa que si personne n'est à portée. Chaque attaque reste télégraphiée, pour laisser le temps de parer ou d'esquiver. Les deux versions (plus bas) ont ce kit commun, en plus de leurs trois compétences propres :

{spoil son kit et ses deux versions}

- **Balayage** de hache en arc devant lui (200°, 3,8 m ; recharge 5,5 s {à équilibrer}) : touche tous les joueurs dans l'arc et les repousse un peu ; **parable**. Couleurs de la version (Terre pour la massue, Rage pour la martache).
- **Coup écrasé** au sol (frappe par-dessus), qui fait une **onde de choc autour de lui** : même règle que le Fracas de la massue (front lent à **sauter**, ni parable ni esquivable ; touché au sol : dégâts et [Renversé](statuts.md)), mais plus courte (7 m/s jusqu'à 8 m ; recharge 9 s {à équilibrer}). Thème **Terre**.
- **Cri** qui renforce les squelettes proches : dès qu'au moins 3 squelettes ordinaires sont à moins de 12 m, il crie et les **galvanise** (statut [Galvanisé](statuts.md) : dégâts +30 %, vitesse +25 %, 8 s ; recharge 15 s ; tout {à équilibrer}). Thème **Rage**.

Choix des coups : une compétence commune prête passe avant celle de la version avec une chance sur deux environ {à équilibrer} ; le cri part de lui-même, cible ou pas.

**Offensif** {décidé, 01/10/2026, retour de Quentin après une partie à deux : « trop simple, pas assez offensif »} :

- il **poursuit les joueurs** : sa proie est le joueur qui l'a frappé dans les 4 dernières secondes, sinon le plus proche qu'il voit à moins de 22 m ; il ne va frapper Nyxessa que si aucun joueur n'est à portée ; il **court** (3,4 m/s au lieu de 2,4 m/s) quand sa proie est à plus de 6 m ;
- il **attaque plus souvent** : un coup toutes les 2,8 s au lieu de 4 s, recharges des compétences réduites d'environ 35 %, préparations un peu plus courtes (environ −15 %, toujours télégraphiées) ;
- il **enchaîne** : après son coup simple (Fracas de la massue, Fauche de la martache), si une compétence est prête, elle part presque aussitôt (0,45 s) ;
- tous ces chiffres {à équilibrer}. Mesure au banc (solo, héros immobile au contact) : environ 10 attaques par minute avant, et il finissait par repartir vers Nyxessa ; voir ci-dessous pour après.

{{dev: `MorgrimVariant.cs` (kit commun, `CommencerAttaque` / `Frapper` scellés qui passent la main à `CommencerVariante` / `FrapperVariante` des deux versions) ; réglages `GameBalance.morgrimCommunChance`, `morgrimBalayage*`, `morgrimEcrase*`, `morgrimCri*`. Pas de clip propre au cri : il joue le clip d'attaque du Golem. Statut `TypeStatut.Galvanise` (icône `statut_galvanise`). Offensif (01/10/2026) : `MorgrimVariant.ChoisirProie`, `MajMarche` / `MajPoursuite` (chasse), enchaînement (`CoupSimple`, `CompetenceVariantePrete`, `ForcerCompetence`, `RecuperationDuree`) ; réglages `morgrimDetection`, `morgrimAgressionDuree`, `morgrimIntervalle`, `morgrimVitesse`, `morgrimVitesseCourse`, `morgrimCourseDistance`, `morgrimEnchainementDelai`. Scénario `ScenariosBoss.Lancer("morgrim_massue")` / `("morgrim_martache")`.}}

**Déclinaisons** {décidé, 26/09/2026} : le mini-boss existe en **deux versions**, l'une armée d'une **massue** (boule à pointes), l'autre d'une **martache** (hache-marteau), avec des **comportements et des compétences différents**. Les yeux **bleu glacé** de la martache la distinguent de la massue (yeux jaune-orangé habituels) au premier coup d'œil. **Une des deux versions apparaît au hasard à la nuit 10** ; la graine du tirage vient de l'hôte (voir Multijoueur ci-dessous et `Docs/reseau.md`).

{dev} Les deux armes existent telles quelles dans le pack KayKit Skeletons EXTRA, à l'échelle du Golem (rig Large) : `Skeleton_Mace_Large` (massue) et `Skeleton_Golem_Axe_Large` (martache — c'est déjà la hache géante du Morgrim actuel : elle porte une tête de marteau au dos de la lame en croissant, donc une vraie hache-marteau sans rien à fabriquer). Aucune arme générée. La distinction des yeux n'est pas une texture alternative du corps (les deux versions gardent `skeleton_texture_A`) mais un second matériau émissif bleu glacé pour les yeux, sur le modèle du rouge des élites (`Yeux_Elite.mat`).

#### Massue : colosse qui contrôle la zone {décidé}

Frappes larges et lentes, pense en zone plutôt qu'en cible : elle punit les groupes serrés autour de Nyxessa et les joueurs qui restent au contact.

- **Fracas** : frappe la boule à pointes au sol devant lui ; une **onde de choc part de l'impact et s'étend lentement** en cercle (6 m/s jusqu'à 14 m {à équilibrer}, décidé le 26/09/2026), assez lentement pour qu'on **saute par-dessus**. Elle n'est **pas parable** (garde et parade sans effet) et **pas esquivable** (la roulade et ses frames d'invulnérabilité n'y font rien) : **seul un saut** au bon instant, au passage du front sous les pieds, évite les dégâts. Un joueur touché au sol encaisse des dégâts et le statut [Renversé](statuts.md). Visuel : anneau épais de gemmes au ras du sol (langage `OndeGemmes`), palette Terre de Morgrim, un peu de poussière — « ça se lit comme à sauter », pas de vert. Thème **Terre**.
- **Tourbillon** : fait tournoyer la boule autour de lui sur 360°, dégâts continus et léger recul pour qui reste dans le rayon ; oblige à sortir de la mêlée le temps du tour. Thème **Terre**.
- **Charge écrasante** : fonce en ligne droite sur sa cible et **renverse** (statut [Renversé](statuts.md), décidé le 26/09/2026) le premier joueur touché, comme la charge bélier du [Paladin](classe-paladin.md) mais sans parade possible en cours de charge. Thème **Terre**.

{video media/ennemis/morgrim/Fracas_massue.mp4} **Fracas** {à confirmer} | {dev} `Melee_2H_Slam` | Arme : massue (`Skeleton_Mace_Large`) | une fois · 2,83 s | {dev} approximation, aucun clip n'est écrit spécifiquement pour ce coup
{video media/ennemis/morgrim/Tourbillon_massue.mp4} **Tourbillon** {à confirmer} | {dev} `Melee_1H_Slash` | Arme : massue (`Skeleton_Mace_Large`) | une fois · 1,57 s | {dev} approximation
{video media/ennemis/morgrim/Charge_Ecrasante_massue.mp4} **Charge écrasante** {à confirmer} | {dev} `Melee_2H_Attack` | Arme : massue (`Skeleton_Mace_Large`) | une fois · 1,33 s | {dev} approximation

#### Martache : colosse qui tranche et vise juste {décidé}

Coups plus rapides et plus précis que la massue, avec une compétence dédiée à percer la défense de Nyxessa plutôt qu'à contrôler la zone.

- **Fauche** : coup en cône devant lui avec le tranchant de la hache, touche tous les joueurs dans l'arc. Thème **Rage** (accents fer, comme le rugissement du viking).
- **Fend-sol** : saut court suivi d'une retombée qui plante l'arme droit devant lui et fend le sol en ligne ; la fissure **ralentit** (statut [Ralenti](statuts.md)) les joueurs qui restent dedans. Il est **parable** (garde levée au bon moment), comme la Fauche. Thème **Terre** pour la fissure (c'est le sol qui casse, pas l'arme), portée réduite par rapport au Fracas de la massue (une ligne, pas un cercle).
- **Coup de brèche** : frappe du côté marteau de l'arme, tournée vers le bouclier de [Nyxessa](vfx.md) plutôt que vers les joueurs : inflige des dégâts renforcés à la paroi du bouclier quand il est levé (voir `Docs/vfx.md`, Bouclier de la relique). Thème **Rage**.

{video media/ennemis/morgrim/Fauche_martache.mp4} **Fauche** {à confirmer} | {dev} `Melee_Dualwield_SlashCombo` | Arme : martache (`Skeleton_Golem_Axe_Large`) | une fois · 1,60 s | {dev} approximation
{video media/ennemis/morgrim/Fend_Sol_martache.mp4} **Fend-sol** {à confirmer} | {dev} `Melee_1H_Stab` | Arme : martache (`Skeleton_Golem_Axe_Large`) | une fois · 1,40 s | {dev} approximation
{video media/ennemis/morgrim/Coup_De_Breche_martache.mp4} **Coup de brèche** {à confirmer} | {dev} `Melee_Block_Attack` | Arme : martache (`Skeleton_Golem_Axe_Large`) | une fois · 1,03 s | {dev} approximation

{dev} Clips filmés sur Morgrim (`Skeleton_Golem`, rig Large, les deux variantes) et son arme, caméra fixe, cadrage commun aux 6 clips : `Assets/Editor/ClipsWiki/ClipsWiki.cs` (`ClipsWiki.Morgrim()`, `sandbox-rig`, 26/09/2026), même méthode que la page [Animations](animations.md) (maillages skinnés cuits, MP4 480 × 480 30 i/s par `MediaEncoder`). Les clips génériques du rig Large qui ne sont pas propres à Morgrim (locomotion, garde, coups non listés ci-dessus) restent sur cette page, filmés sur le mannequin Rig_Large. Prototypé dans le bac à sable `sandbox-rig` (scène `Assets/Scenes/Morgrim.unity`, outils `Assets/Editor/Morgrim/MorgrimBuilder.cs` et `MorgrimCaptures.cs`) : les deux mannequins posés et équipés, poses d'attente et pose clé de chaque compétence échantillonnées sur les clips du rig Large (`Melee_2H_Slam`, `Melee_1H_Slash`, `Melee_2H_Attack` pour la massue ; `Melee_Dualwield_SlashCombo`, `Melee_1H_Stab`, `Melee_Block_Attack` pour la martache — approximations, aucun clip n'est écrit spécifiquement pour ces coups). Effets principaux prototypés dans le langage gemmes (`Assets/VFX/Morgrim/MorgrimEffets.cs`, gemmes `LowPolyGem` / shader `Relic/VertexColorUnlit`, palettes Terre et Rage). Captures du bac à sable : `Assets/Screenshots/morgrim_massue_*.png`, `morgrim_martache_*.png`, planche `morgrim_planche.png`.

{dev} **Reporté dans `main` le 26/09/2026** (mêmes chemins et GUID) : `Assets/VFX/Morgrim/` (`MorgrimEffets.cs`, `MorgrimGemmes.mat`), `Assets/Jeu/Materiaux/Yeux_Glace.mat` ; les armes `Skeleton_Mace_Large.fbx` et `Skeleton_Golem_Axe_Large.fbx` étaient déjà présentes dans `main` (pack KayKit Skeletons EXTRA), rien à copier. Comportement en jeu : `Assets/Scripts/Jeu/Ennemis/MorgrimVariant.cs` (base commune : joueurs proches, télégraphie et impact en gemmes), `MorgrimMassue.cs` (Fracas, Tourbillon, Charge écrasante) et `MorgrimMartache.cs` (Fauche, Fend-sol, Coup de brèche), dérivées de `Golem.cs` ; valeurs dans `GameBalance` (préfixes `morgrimMassue*` / `morgrimMartache*`, {à équilibrer}). Deux prefabs `Assets/Jeu/Prefabs/Morgrim_Massue.prefab` et `Morgrim_Martache.prefab`, construits à partir de `Squelette_Golem.prefab` par l'outil relançable `Assets/Editor/Morgrim/MorgrimPrefabBuilder.cs` (menu **Deathless > Jeu > Morgrim**). `DirecteurVagues` tire l'une des deux versions au hasard à la nuit 10 (`prefabMorgrimMassue` / `prefabMorgrimMartache`), tirage fait par l'hôte seul (Docs/reseau.md). « Renversé » (charge écrasante, et l'onde du Fracas non sautée) est le statut [Renversé](statuts.md) (26/09/2026, knockdown complet : chute, au sol, relevé, sans contrôle), plus l'ancien étourdissement court. Le Coup de brèche inflige des dégâts renforcés au [bouclier de Nyxessa](vfx.md) quand il est levé. **Onde du Fracas** (26/09/2026) : `OndeChocLente.cs`, front lent (`GameBalance.morgrimMassueFracasOnde*`), jugement « au sol ou en l'air » fait côté client propriétaire (`Docs/reseau.md`). Depuis le 30/09/2026, l'onde a son propre visuel (anneau de gemmes Terre `AnneauGemmesComp` à front linéaire, calé sur l'heure réseau du départ) au lieu du prefab d'onde du Golem, et sert aussi au Coup écrasé commun. Sons, terre projetée et coups sourds de Morgrim (et du Golem de repli) sont rejoués chez les clients par `EffetsBoss` (`EnnemiReseau.DiffuserEffetBoss`). Prefabs `Morgrim_Massue` et `Morgrim_Martache` générés et câblés dans `Assets/Scenes/Village.unity` (champs `prefabMorgrimMassue` / `prefabMorgrimMartache` de `DirecteurVagues`) ; vérification en Play : à confirmer par Quentin.

{/spoil}

Points de vie et dégâts : {à équilibrer}.

### Nyxar, le Nécromancien {décidé}

Invocateur qui combat à distance :

{spoil ses points faibles et ses phases}

- il **garde ses distances** (entre 12 et 18 m) et **se téléporte** quand on l'approche (un joueur à moins de 5 m ; il réapparaît de 11 à 16 m plus loin, loin des joueurs, sans s'éloigner à plus de 26 m de Nyxessa ; recharge 9 s ; tout {à équilibrer}) : gemmes vertes aspirées à son départ, jaillissantes à son arrivée ;
- il tire des **salves de crânes** : 3 crânes à 0,6 s d'écart, toutes les 3 s {à équilibrer} ; les crânes sont **esquivables** {décidé, 01/10/2026} : plus lents (9 m/s au lieu de 12), guidage faible qui s'arrête dans la dernière partie de la course (à 7 m de la cible, ou si elle sort du cône devant le crâne), si bien qu'**un pas de côté ou une roulade au bon moment** les fait rater {à équilibrer} ;
- il **relève des squelettes** du sol autour de lui ;
- il **fauche à la faux** ceux qui le serrent de près quand il ne peut pas se téléporter (2,6 m, 30 dégâts, parable {à équilibrer}) ;
- ses **deux éclats de Nyx brillent**, dans le crâne de sa couronne et dans celui de son grimoire à la ceinture : ce sont ses **points faibles** {décidé}. Grappes de gemmes vertes (énergie de Nyxessa) qui pulsent et rétrécissent en s'usant.
  - Son **corps prend des dégâts** dès le début du combat {décidé, 01/10/2026} (Nyxessa tire aussi sur lui).
  - Il **perd un éclat par tranche de vie** {décidé, 01/10/2026} : le premier quand il passe sous les 2/3 de ses PV, le second sous 1/3, avec le changement de phase (tableau ci-dessous). Un éclat peut aussi se **briser** sous les coups (200 PV chacun, multipliés comme ceux des ennemis selon la nuit {à équilibrer}) ; une frappe de mêlée à cible unique vise l'éclat le plus proche avant son corps. À chacun de trouver comment tirer parti de ces points faibles.
  - Un éclat brisé lui retire une partie de son kit (tableau ci-dessous), plus de PV en moins {décidé, 01/10/2026}.
  - Sa méga barre de vie affiche les éclats restants (« NYXAR · 2 ÉCLATS »), puis « ENRAGÉ ».
  - PV : 1 800 (× nuit) au lieu de 1 200, pour un combat à deux d'environ 2 à 3 minutes {à équilibrer}.

**Trois phases**, selon les éclats brisés {décidé} :

| Phase | Éclats | Combat |
|---|---|---|
| 1 | Les deux intacts | Kit complet : distance, téléportation, salves de crânes, squelettes relevés, faux de près |
| 2 | Couronne brisée | Plus de téléportation ni de squelettes relevés |
| 2 | Grimoire brisé | Plus de salves de crânes (un seul crâne par tir) |
| 3 | Les deux brisés | Enragé, il se bat au corps à corps à la faux |

En phase 3, il fonce sur les joueurs et sur Nyxessa comme un squelette de mêlée, plus vite (4,2 m/s) et plus fort (38 dégâts aux joueurs, 40 à Nyxessa, un coup toutes les 1,7 s) {à équilibrer}.

{/spoil}

Points de vie, dégâts et cadence : {à équilibrer}.

{{dev: `Necromancien.cs` (phases, téléportation, salves, faux, enragé) et `EclatNyx.cs` (éclat : sa propre `Sante` sur un enfant posé à ses pieds, sphère de collision et grappe de gemmes qui suivent l'os de la tête ou du bassin) ; coup sur un éclat : `Sante.renvoi` → `Necromancien.CoupSurEclat` (un même geste ne frappe Nyxar qu'une fois), usure `EclatNyx.Abimer` ; rupture par tranche de vie : `Necromancien.VerifierTranches` ; `Combat.RayonDe` fait passer l'éclat avant le corps en mêlée. Crânes esquivables : `MissileCrane` (paramètres de coupure passés par le tireur, aussi au visuel des clients par `PartieReseau.Missile`). Réglages `GameBalance.necro*` (PV, distance, tir, vitesse des crânes, invocation) et `nyxar*` (salve, crânes, téléportation, faux, éclats, enragé). Grimoire à la ceinture : modèle KayKit `spellbook_closed`, posé sur le prefab par le menu **Deathless > Jeu > 14. Boss (grimoire de Nyxar)** (à relancer après le menu 4). Réseau : PV des éclats dans `EnnemiReseau` (variable `m_Eclats`), coups des clients sur un éclat : le corps par le relais ordinaire, l'usure de l'éclat et l'effet vu de tous relayés à l'hôte (`RelayerEclat` → `AbimerEclat`, effet `EffetBoss.Critique`), téléportation par `NetworkTransform.Teleport`, effets par `EffetsBoss` ; les crânes passent déjà par `PartieReseau.Missile`. Scénarios de vérification : `ScenariosBoss.Lancer("nyxar")`, `ScenariosBoss.Lancer("nyxar_esquive")`.}}

## Mage

- Le mage squelette tire un **missile en forme de crâne** fait de gemmes {effet validé}. {{dev: Il s'appelait « nécromancien » avant que ce nom ne soit réservé au boss final.}}
- C'est le même missile que celui de Nyxessa, à taille normale ; celui de Nyxessa est une fois et demie plus gros {décidé}.
- Modèle : le squelette mage KayKit {décidé}.
- **Comportement** {décidé, 28/09/2026} : tireur fragile, il garde ses distances (il avance au-delà de 7 m, recule en deçà de 5,5 m, donc hors du bouclier de Nyxessa) et tire sur le joueur le plus proche à portée, sinon sur Nyxessa. Pas de coup au corps à corps, ni invocation ni téléportation. Chiffres {à équilibrer} dans le tableau plus bas. {{dev: `Mage.cs` ; `GameBalance.mage`, `mageDistanceTir`, `mageVitesseMissile` 11 m/s ; prefab `Squelette_Mage`. `DefenseNyxessa` le traite en cible lourde.}}

## Voleur

- **Comportement** {décidé, 28/09/2026} : rapide et moins solide qu'un guerrier, il **chasse les joueurs isolés** : un joueur sans autre joueur vivant à moins de 10 m, repéré jusqu'à 14 m (plus loin que la détection ordinaire de 8 m). En solo, le joueur est toujours isolé. Sans joueur isolé, il fait comme les autres squelettes ; il riposte comme eux au contact de Nyxessa. Chiffres {à équilibrer} dans le tableau plus bas. {{dev: `Voleur.cs` ; `GameBalance.voleur`, `voleurDistanceIsolement`, `voleurDistanceChasse` ; prefab `Squelette_Voleur`.}}

## Comportement et détection

- **Riposte au contact de Nyxessa** {décidé, 27/09/2026} : un squelette qui frappe Nyxessa ne l'ignore plus tout à fait : s'il est **frappé deux fois de suite par le même héros à moins de 3 m**, il se retourne vers lui (quelques coups), puis revient à Nyxessa. Fini les coups dans le dos gratuits ; la garde du paladin sert aussi autour de la relique. {{dev: `Squelette.OnTouche` (compteur par héros, `DistanceNyxessa() <= RayonContact` ne bloque plus la riposte), IA de l'hôte seulement.}} {{dev: Fait le 27/09/2026 : `Squelette.CompterRiposte` ; `GameBalance.riposteCoups` 2, `riposteDistance` 3 m, `riposteDuree` 3 s, `riposteAttaques` 2. Le compteur se remet à zéro si le héros change ou après 4 s sans coup ; sbires et guerriers seulement (élites compris ; Morgrim et le Nécromancien gardent leur comportement). La riposte prend la cible tout de suite s'il marche ou récupère, et écourte la récupération d'un coup sur Nyxessa (0,25 s) ; elle s'arrête au bout de 3 s ou après 2 coups portés, ou si le héros entre dans la fumée, puis il revient frapper Nyxessa. Un rugissement (Provoqué) la remplace.}}

- **Course** {décidé, 30/09/2026} : les squelettes ordinaires (sbire, guerrier, voleur, élites compris) **courent un peu** quand ils poursuivent un joueur loin d'eux, et repassent au pas de charge près de lui. Ils ne courent jamais en marchant vers Nyxessa : les vagues gardent leur rythme d'arrivée. Le mage (qui garde ses distances), Morgrim et Nyxar ne courent pas. Chiffres {à équilibrer} : course au-delà de 6 m de la cible, retour au pas en deçà de 4 m ; vitesse × 1,45, plafonnée à 5 m/s (pas plus vite qu'un joueur qui court : le sprint le distance toujours). {{dev: `Squelette.Court`, `MajCourse`, `VitesseCourse` ; `GameBalance.courseFacteur`, `courseVitesseMax`, `courseDistance`, `courseDistanceArret` ; blend tree `Squelette_Jeu` : Skeletons_Idle 0, Walking_A 0,5, Running_A (copie bouclée `Running_A_Loop`) 1 (`JeuBuilder.SqueletteCourseEsquive`). Sbire 4,9 m/s, guerrier 4,35 m/s, voleur 5 m/s en course.}}
- **Esquive** {décidé, 30/09/2026} : comme les joueurs, les squelettes ordinaires peuvent **esquiver** : quand un joueur proche, tourné vers eux, arme une attaque (coup, tir, boule), ils ont une chance de faire un bond sur le côté ou en arrière, brièvement intouchables, puis reprennent (le joueur qui les a menacés devient leur cible). Jamais pendant leur propre coup, ni étourdis ; un temps de recharge par squelette. Chiffres {à équilibrer} : joueur à moins de 3,5 m et à moins de 60° de face ; chance par attaque 35 % (voleur), 10 % (sbire), 5 % (guerrier) ; recharge 4 à 6 s ; bond de 3 m en 0,4 s, intouchable 0,3 s ; 70 % sur le côté. Un joueur furtif n'est pas esquivé. {{dev: `Squelette.GuetterAttaques`, `Esquiver`, `MajEsquive`, état `Etat.Esquive` ; `Heros.DerniereAttaque` posé à l'appui de RT et envoyé aux autres postes par l'effet commun `ClasseHeros.EffetAttaque` (205) : l'hôte, qui tient les squelettes, juge l'esquive ; clips Dodge_Forward/Right/Backward/Left (trigger `Dodge`, `DodgeDir`) répliqués par le NetworkAnimator. `GameBalance.esquiveEnnemi*`, `esquiveChanceSbire/Guerrier/Voleur`.}}
- **Cible prioritaire** {décidé} : les squelettes marchent vers Nyxessa. Un joueur qui les frappe, ou qui passe à moins de 4 m, devient leur cible pendant quelques secondes, puis ils reprennent leur route. Le voleur fait exception : il chasse les joueurs isolés. Distance et durée {à équilibrer}.
- **Trajets variés** {décidé, 30/09/2026} : les squelettes d'une même clairière ne suivent plus tous le même chemin vers Nyxessa, ce qui rendait les attaques de zone trop faciles. À sa sortie de terre, chaque sbire, guerrier ou voleur (élites compris) prend l'un des **couloirs** de sa clairière : il passe par un point à mi-chemin, décalé sur le côté, puis rejoint une place autour de Nyxessa du même côté ; son pas varie aussi un peu d'un squelette à l'autre, ce qui étire la file. Le rythme des vagues ne change pas (détour de 12 % au plus, sur les chemins praticables : jamais dans la rivière). Le mage, les boss et les gardiens du donjon ne sont pas concernés. Chiffres {à équilibrer} : 3 couloirs par clairière, 22 m entre les couloirs extrêmes à mi-chemin (± 3 m de flou par squelette), point de passage entre 40 et 60 % du trajet ; pas de couloir à moins de 25 m de Nyxessa (invocations de Nyxar) ; pas de marche ± 8 % ; places d'arrivée jusqu'à 55° de part et d'autre de la direction d'approche (avant : 35°). {{dev: Fait le 01/10/2026 : `Squelette.Initialiser` (tirage du couloir, de la place et du pas), `ChoisirPassage` (point posé sur le NavMesh par `NavMesh.SamplePosition`, refusé si le chemin par lui dépasse le chemin direct de plus de `trajetDetourMax` : décalage réduit de moitié, puis abandonné), `DestinationMarche` (le point de passage est oublié une fois atteint ou dépassé, par exemple après une poursuite), `PasDeBase` ; `GameBalance.trajet*`. IA de l'hôte seulement, positions répliquées : rien à faire en réseau. Mesure en Play (18 squelettes depuis la clairière nord, 70 m) : écart latéral moyen à mi-chemin 1,0 à 1,3 m avant, 6,3 à 6,7 m après (écart-type 1,2 à 2,5 m avant, 7,5 m après) ; trajet moyen 22,8 à 23,2 s avant, 23,4 à 23,7 s après (+2 %).}}
- **Détection de l'assassin furtif** {décidé} : cône de vue d'environ 6 m devant le squelette, 1,5 m dans son dos. Voir [Classes](classes.md).
- **Valeurs de départ** de la version 0.1 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Ennemi | Vie | Vitesse | Dégâts | Autre |
|---|---|---|---|---|
| Sbire | 100 | 3,4 m/s | 8 | prépare son coup 0,7 s |
| Guerrier | 160 | 3,0 m/s | 14 | prépare son coup 0,8 s |
| Voleur | 115 | 4,4 m/s | 12 (10 sur Nyxessa) | prépare son coup 0,5 s, un coup toutes les 1,4 s |
| Mage | 70 | 3,2 m/s | 12 par crâne | tire à 7,5 m au plus, incantation 0,45 s, un tir toutes les 2,6 s |
| Élite | ×3 | | ×1,5 | portée +0,3 m ; 1 par nuit aux nuits 5 et 6, 2 dès la nuit 7 |
| Morgrim | 1 500 | 2 m/s | 45 en zone, 60 sur Nyxessa | rayon 3 m, prépare son coup 1,6 s |
| Nyxar | 1 200 | | 18 par crâne, toutes les 3 s | reste entre 12 et 18 m ; relève 3 sbires toutes les 15 s, 12 au plus |

- **Composition des vagues** {à équilibrer} : vagues de 30, 35 et 35 % des squelettes de la nuit ; avec quatre vagues, 22, 24, 26 et 28 %. Chaque sortie est tirée selon les parts de la nuit : mages, puis voleurs, puis guerriers, le reste en sbires (au moins 30 %). Guerriers : 0 % la nuit 1, 25 % la nuit 2, puis de 30 à 40 % (40 % dès la nuit 9). Voleurs : 12 % la nuit 3, puis 14 à 16 %. Mages : 10 % la nuit 5, 12 % dès la nuit 6, 14 % dès la nuit 9. {{dev: `GameBalance.partGuerriers`, `partVoleurs`, `partMages` ; `DirecteurVagues.TirerType`.}}
- **Or rapporté** par squelette tué, versé à la caisse commune (règle provisoire en attendant le donjon, voir [Déroulé d'une partie](deroule.md)) {à équilibrer} :

| Ennemi | Or |
|---|---|
| Sbire (y compris ceux relevés par Nyxar) | 5 |
| Guerrier, voleur | 8 |
| Mage | 10 |
| Élite | 25 |
| Morgrim | 150 |
| Nyxar | 300 |
- {dev} Les élites sont des guerriers renforcés. Voleurs et mages ont leur propre prefab depuis le 28/09/2026 (avant, ils étaient joués comme des guerriers) ; sans prefab, un voleur ou un mage est posé comme un sbire.
