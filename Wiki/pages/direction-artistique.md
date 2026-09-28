# Direction artistique

{dev} Les images qui fixent le cap visuel de Deathless. Depuis le 28/09/2026, Quentin passe par **Grok Imagine** pour la direction artistique : il génère, choisit, et les images retenues deviennent la cible des générateurs du jeu (maisons, arbres, carte). Une image n'est jamais copiée telle quelle : on en tire des règles.

## Comment ça marche

1. **Générer** : dans Grok, ou par l'API avec `python Docs/outils/grok_image.py <prompt>` (prompts dans `Docs/da/prompts/`, clé dans la variable d'environnement `XAI_API_KEY`, jamais dans le dépôt ; chaque image est facturée). Les images arrivent dans `Docs/references/`, le journal dans `Docs/da/journal.md`.
2. **Choisir** : Quentin retient une image. Elle entre sur cette page avec ce qu'on en garde et ce qu'on écarte.
3. **Traduire** : les règles chiffrées vont dans le guide de style (`Docs/style-kaykit.md`) et dans les fiches des générateurs ; chaque agent compare sa capture à l'image avant de conclure.

## Partage des rôles {décidé}

Grok donne la **composition et les idées** (où sont les choses, les volumes, l'ambiance) ; le **style de surface reste celui du jeu** : facettes plates, une couleur par facette avec le dégradé de l'atlas, aucune texture, formes trapues. Les images de Grok sont belles mais texturées (briques et planches peintes, herbe détaillée) : on n'en copie jamais le rendu (Quentin, 28/09/2026). Pour rester au plus près du jeu, on part d'une **capture du prototype** (`grok_image.py --depuis`) et on demande la variante.

## Le jeu de prompts {dev}

Tous les prompts sont dans `Docs/da/prompts/` et reprennent le même bloc de style (`_style-jeu.md` : facettes plates, aucune texture, formes trapues, vert réservé à la relique, maisons sur dalle lisse) par l'en-tête `base: _style-jeu`.

| Prompt | Ce qu'il produit |
|---|---|
| `plan-village` | Le plan vu du dessus, nord en haut (carte du niveau) |
| `village-jour` | Le village vu du sud, cadrage du menu principal |
| `village-nuit` | Même cadrage, de nuit : la relique d'abord, fenêtres et lanternes ensuite |
| `village-joueur` | À hauteur de joueur, depuis le pont sud |
| `maisons-planche` | Les six maisons côte à côte, avec un personnage pour l'échelle |
| `grotte-portail` | Retouche d'une capture du prototype : la grotte dans la falaise (`--depuis`) |

## Premier jeu d'images (28/09/2026)

**Le plan n° 3 est la cible** {décidé, Quentin, 28/09/2026}. Les maisons sont « un bon début », à reprendre une fois leurs intérieurs dimensionnés.

{image media/da/plan-village-20260928-03.jpg} **Plan n° 3** | Le plus fidèle à nos décisions
{image media/da/plan-village-20260928-01.jpg} **Plan n° 1** | Sept maisons, une dans l'axe nord
{image media/da/plan-village-20260928-04.jpg} **Plan n° 4** | La rivière serre le plateau
{image media/da/plan-village-20260928-02.jpg} **Plan n° 2** | Le plateau dans l'eau, forêt clairsemée
{image media/da/village-jour-20260928-03.jpg} **Village de jour** | Cadrage du menu
{image media/da/village-jour-20260928-02.jpg} **Village de jour, variante** | Falaise en arc
{image media/da/village-nuit-20260928-01.jpg} **Village de nuit** | La relique domine, braise verte dans la grotte
{image media/da/village-joueur-20260928-01.jpg} **Vue du joueur** | À refaire : le plateau est dans l'eau
{image media/da/maisons-planche-20260928-01.jpg} **Planche des maisons** | Les six métiers

- **Plan n° 3** : falaise au nord, cascade plein nord, grotte verte à sa gauche, rivière à l'est du plateau qui sort au sud-ouest, deux ponts (est et sud), gué sous le pont sud, six maisons trois par rive, axe nord dégagé, lande et souches à l'est, forêt au sud et à l'ouest, trois clairières. Écarts : forêt trop dense, maisons à une quinzaine de mètres de la relique (notre prototype : 30 m).
- **Ce que les images proposent de neuf** : un **anneau pavé** autour du plateau d'où partent les allées ; un village **plus ramassé** ; des maisons **à étage** (taverne, mécano) ; la tour du sorcier à **toit conique** ; le **potager clôturé et le puits** de la maison de décor ; les **lanternes aux ponts**.
- **Ce qu'on écarte toujours** : les textures (pierres maçonnées, planches peintes), l'enseigne de la fiole en vert (le vert est à Nyxessa), le plateau posé dans l'eau.

## Taverne (28/09/2026)

{image media/da/taverne-coupe-20260928-01.jpg} **Coupe, toit retiré** | Comptoir, galerie, estrade, âtre
{image media/da/taverne-coupe-20260928-02.jpg} **Coupe, variante** | Charpente apparente
{image media/da/taverne-ambiance-20260928-01.jpg} **Depuis l'entrée** | À hauteur de joueur

- **On garde** : la salle à double hauteur avec poutres, lustres à bougies et guirlandes de fanions ; le comptoir sous une galerie à balustrade ; l'estrade du barde dans l'angle ; l'âtre massif ; le grand sol nu au milieu ; la Bavaroise au comptoir, le clochard sur son banc près du feu.
- **On corrige** : **l'échelle**. Grok dessine des personnages de 1,2 m dans un mobilier d'adultes ; les nôtres font 2,3 m, avec une grosse tête. Le plan chiffré (`Docs/da/taverne-plan.md`) fait foi : salle de 16 × 11 m, allée de 3 m, comptoir de 6 m à 1,3 m de haut, 5 m sous poutres pour la caméra. Le bloc de style des prompts porte désormais la règle d'échelle.
- **On écarte** : la seconde porte latérale, les textures de pierre.

{image media/da/taverne-interieur-grok-cible.webp} **Intérieur cible, donné par Quentin** | Une seule salle, sans galerie ni escalier

**Cible de l'aménagement** (image de Quentin, 28/09/2026, après le premier volume gris) : une seule grande salle ; âtre sur le mur ouest ; estrade à pan coupé dans le coin nord-ouest ; comptoir droit le long du mur nord ; râtelier de six tonneaux dans le coin nord-est ; tables rondes groupées au sud-est, une table seule près du feu ; grand centre vide. La **galerie et l'escalier disparaissent** : dans le premier volume gris ils gênaient la caméra (poteaux, dessous de galerie à 3,2 m, escalier collé à la place des joueurs). On n'en reprend pas les textures (bois veiné, enduit martelé) : facettes plates, une couleur par facette. **Volume gris à l'échelle validé par Quentin** {décidé, 28/09/2026}, avec trois tables rondes au sud-est au lieu de quatre, disposées régulièrement. Toit allégé : 40° à demi-croupes, faîtage à 10,8 m (11,75 m à 45°). Plan chiffré : `Docs/da/taverne-plan.md`. 

{image media/da/taverne-volume-v2.png} **Volume gris validé** | Même angle que l'image cible, personnages de 2,3 m
{image media/da/taverne-ext-facade.png} **Extérieur habillé** | À côté d'un pin et de la maison standard
{image media/da/taverne-ext-nuit.png} **La nuit** | Lanternes et fenêtres chaudes

**Extérieur habillé validé par Quentin** {décidé, 28/09/2026} (`TaverneExterieurBuilder`, scène `Assets/Scenes/TaverneExterieur.unity` de `sandbox-level`) : dalle de pierre lisse et basse, enduit crème, colombage, porte double sous un arc surbaissé et un auvent de tuiles, volets bruns, toit à 40° à demi-croupes, cheminée de pierre sur le pignon ouest, enseigne, lanternes, tonneaux et banc. 23 916 triangles, faîtage à 10,66 m. Même technique que la maison standard (biseaux, atlas en dégradé). À reprendre plus tard : tuiles plus grosses que celles de la maison standard (1,15 m contre 0,86 m), enseigne petite, façade arrière sans fenêtre.

{image media/da/taverne-int-comme_cible.png} **Intérieur meublé** | Même angle que l'image cible
{image media/da/taverne-int-estrade.png} **Vers l'âtre et l'estrade** | Caméra à l'épaule
{image media/da/taverne-int-nuit.png} **La nuit** | Feu, lustres et bougies

**Intérieur meublé validé par Quentin** {décidé, 28/09/2026} (`TaverneInterieurBuilder`, scène `Assets/Scenes/Taverne.unity` de `sandbox-level` : la taverne complète). Plancher à larges lames, colombage et charpente apparents, âtre de pierre, estrade à pan coupé, comptoir à panneaux et étagères à chopes, râtelier de six tonneaux, trois tables rondes et celle du clochard, appliques, trois lustres à 4,6 m, fanions rouge brique, bleu ardoise et ocre. **Mobilier à l'échelle des personnages** : tabouret 0,6 m, plateau de table 0,8 m, comptoir 1,3 m. Cinq lumières temps réel à l'intérieur. **Allégé le 28/09/2026 de 52 131 à 34 549 triangles** (objectif 35 000 tenu), sans perte visible aux distances de jeu : 12 lumières temps réel (5 intérieures dont 4 vacillantes, 7 de nuit inactives de jour), 29 maillages, 4 matériaux. Le clochard, mal assis, est rassis d'aplomb (tabouret à 0,5 m et non 0,6 m, mesuré sur son maillage). Prefab prêt : `Assets/StyleKayKit/Taverne/Taverne.prefab`. Bavaroise, barde et clochard sont encore des chevaliers en substitut.

## Les cinq autres intérieurs (28/09/2026)

{image media/da/sorcier-coupe-20260928-01.jpg} **Maison du sorcier** | Carte du village, pupitre, éclat de Nyx, tour
{image media/da/forge-coupe-20260928-01.jpg} **Forge** | Enclume au centre, foyer, appentis
{image media/da/druide-coupe-20260928-01.jpg} **Boutique du druide** | Chaudron, comptoir aux fioles, herbes séchées
{image media/da/mecano-coupe-20260928-01.jpg} **Boutique du mécano** | Comptoir, râteliers d'armes, machine à engrenages
{image media/da/maison-base-coupe-20260928-01.jpg} **Maison de base** | Puits, potager, étendoir

Plans chiffrés à notre échelle : `Docs/da/maisons-plans.md`. Les boutiques font 11 à 12 m sur 8 à 9 m à l'intérieur (une fois et demie la maison standard actuelle), la maison de base 8 × 6,5 m sur une parcelle de 16 × 10 m avec son puits et son jardin. Partout : point d'intérêt au fond, sol nu d'au moins 6 × 5 m devant lui pour les quatre joueurs, 4,5 m sous poutres.

- **On garde** : les dispositions, les accessoires de métier, la tour ouverte sur la salle du sorcier, l'appentis de la forge.
- **On corrige** : l'échelle des personnages (toujours trop petits chez Grok), les soubassements maçonnés, le cristal vert au bâton du mage joueur (notre mage est de feu ; le vert est à Nyxessa).

## Plan et façades à l'échelle (28/09/2026)

{image media/da/plan-village.png} **Plan à l'échelle** | Dessiné d'après les mesures, fait foi
{image media/da/facades.png} **Façades à l'échelle** | Personnage de 2,3 m devant chacune
{image media/da/facades-gabarit-20260928-01.jpg} **Façades habillées par Grok** | Fidèles aux gabarits
{image media/da/plan-gabarit-20260928-01.jpg} **Rendu général par Grok** | Ambiance seulement : tailles fausses

`Docs/outils/plan_village.py` dessine le plan et les façades au mètre près à partir des mesures des intérieurs, et vérifie les écarts (voisins, rivière, couloirs des vagues, axe nord, falaise). Avec les vraies emprises, la couronne passe à **29 à 36 m** de Nyxessa : taverne et sorcier au nord-ouest (le sorcier près de la grotte), druide et forge au nord-est, mécano au sud-est, maison de base au sud-ouest ; trois bâtiments par rive.

- **Façades** : Grok respecte largeurs et hauteurs quand on lui donne le gabarit. À corriger : il ajoute un étage de fenêtres partout (seuls la taverne et le mécano en ont un), et garde les soubassements maçonnés.
- **Rendu général** : Grok a basculé la vue, resserré le village et donné la même taille à toutes les maisons. Il sert pour l'ambiance ; pour un rendu général exact, c'est le prototype dans le moteur qui fait foi.

## Pins (28/09/2026)

{image media/da/pins-planche-20260928-01.jpg} **Pins, planche 1** | Élancé, parasol, battu par le vent
{image media/da/pins-planche-20260928-02.jpg} **Pins, planche 2** | Variante

Proposition de Quentin : partir sur du **pin**, plus esthétique et à sa place au pied d'une montagne. Trois silhouettes : **pin élancé** à cinq étages (environ 10 m), **pin parasol** à tronc fourchu et couronne plate (8 à 9 m), **vieux pin battu par le vent**, penché, touffes d'un seul côté (7 à 8 m). Tronc brun-rouge nu sur 3 m et plus (une fois et demie un personnage), moignons de branches, masses d'aiguilles en gros volumes facettés à dessous sombre. {à confirmer} : remplacent-ils les six essences validées le 27/09/2026, ou s'y ajoutent-ils ?

{image media/da/pins-moteur-rangee.png} **Les trois pins dans le moteur** | Chevalier de 2,3 m et maison standard pour l'échelle
{image media/da/pins-moteur-bosquet.png} **Bosquet** | Les trois silhouettes mélangées

**Construits et validés** {décidé, 28/09/2026} dans `sandbox-level` (`ArbreStyleBuilderPins`, prefabs `Assets/StyleKayKit/Arbres/Arbre_Pin{Elance,Parasol,Vent}.prefab`). Quentin les a voulus **plus petits, comme sur les planches** : élancé **5,8 m**, parasol **5,0 m**, pin du vent **4,6 m** (2,2, 1,9 et 1,8 fois un personnage), troncs de 0,7 m nus sur 2,6 m, 340 à 436 triangles. Ils restent plus bas que la maison standard (7,9 m).

## Rendu 3D low poly à l'échelle, jour et nuit (28/09/2026)

{image media/da/village-3d-lowpoly-jour-20260928-03.jpg} **Village, jour, avec les gués** | Trois passages en eau basse sur la rive est
{image media/da/village-3d-lowpoly-nuit-20260928-03.jpg} **Village, nuit, avec les gués** | Même scène

{image media/da/village-3d-lowpoly-jour-20260928-02.jpg} **Village, jour, maquette corrigée** | Trois sentiers d'attaque, route de la grotte
{image media/da/village-3d-lowpoly-nuit-20260928-02.jpg} **Village, nuit, maquette corrigée** | Même scène

**Corrections demandées par Quentin** : les points d'attaque des ennemis doivent se lire, et le chemin de la grotte doit être clair. La maquette porte maintenant **trois sentiers de terre battue de 7 m** (est par le pont, sud par le pont, ouest par la terre ferme), de chaque clairière à l'anneau pavé, sans maison ni arbre dessus ; une **route pavée de 4 m**, bordée de lanternes, du plateau à la grotte ; le sorcier est à gauche de cette route, la taverne au sud-ouest, la maison de base à l'ouest. Versions précédentes ci-dessous.

{image media/da/village-3d-lowpoly-jour-20260928-01.jpg} **Village, jour** | Grok sur le gabarit 3D à l'échelle
{image media/da/village-3d-lowpoly-nuit-20260928-01.jpg} **Village, nuit** | Même scène, la relique domine
{image media/da/village-3d.png} **Gabarit 3D à l'échelle** | Volumes aux cotes, projection parallèle

**Méthode retenue pour les vues d'ensemble** : `Docs/outils/plan_village_3d.py` dessine les volumes aux vraies cotes (projection parallèle : un mètre vaut le même nombre de pixels partout), avec quatre personnages de 2,3 m près du plateau ; Grok habille ce gabarit (`village-3d-lowpoly-jour`, puis `-nuit` sur l'image de jour). Positions et tailles relatives sont tenues : la taverne est le grand bâtiment, la maison de base le petit, les joueurs restent à leur taille. Restent approximatifs : la grotte, en partie cachée par la maison du sorcier ; les soubassements maçonnés.

## Rendu réaliste 3D, jour et nuit (28/09/2026)

{image media/da/village-3d-jour-20260928-01.jpg} **Village, rendu réaliste, jour** | D'après le plan à l'échelle
{image media/da/village-3d-nuit-20260928-01.jpg} **Village, rendu réaliste, nuit** | Même scène, la relique domine

Images d'ambiance, **hors style du jeu** (matières réalistes) : elles servent à juger le lieu et la lumière, et pourront servir à la communication. Elles ne sont pas une cible de modélisation. Écarts avec le plan : un troisième pont, deux toits d'ardoise, des bâtiments de tailles voisines.

## Images retenues

{image media/da/village-vision-grok-01.webp} **Village, vision du 28/09/2026** | Grok Imagine, par Quentin
{image media/da/grotte-portail-20260928-01.jpg} **Grotte du portail, 28/09/2026** | Grok, à partir d'une capture du prototype
{image media/da/arbres-lowpoly-reference.webp} **Arbres, planche du 27/09/2026** | Référence choisie par Quentin

### Village, vision du 28/09/2026

- **On garde** : la falaise au nord avec la cascade et son bassin ; la relique sur son plateau à marches ; les métiers lisibles de loin (tonneaux, forge qui rougeoie, lierre, tour du sorcier) ; le village **sur une terrasse** au-dessus de l'eau ; la rivière qui **entoure le village** au sud, les ponts comme entrées ; des **maisons à étage**, volumes en L, lucarnes.
- **On écarte** : le soubassement en pierres maçonnées (décision du 27/09/2026 : dalle basse et lisse).
- **À trancher** : le village serré (maisons à une dizaine de mètres de Nyxessa) contre la place de combat autour de la relique. Quentin retravaille la carte ; le plan validé du prototype v5 reste la base tant que la nouvelle carte n'est pas choisie.

### Arbres, planche du 27/09/2026

- **On garde** : conifères en étages facettés, feuillus en grappes de boules, arbres morts, souches, rochers, plusieurs verts par famille.
- **Adapté** : troncs dégagés jusqu'à 1,5 fois la hauteur des personnages, verts plus profonds, couronne et tronc séparés. Fait et validé (`ArbreStyleBuilder`).

### Grotte du portail, 28/09/2026

- Générée à partir de `v5f_01_menu_jour.png` (capture du prototype v5) : Grok a gardé nos maisons, nos dalles et nos couleurs, et n'a changé que la falaise.
- **On garde** : la bouche de grotte large encadrée de gros blocs, le portail vert au fond visible du dehors, la lueur verte qui déborde sur le sol et les rochers, l'allée pavée qui y mène.
- **On écarte** : la maison du nord-ouest que l'image a fait disparaître (la grotte se loge entre les maisons, dans la falaise) ; l'ancien portail resté sur sa place à droite (il déménage dans la grotte).
- Règles : [Village](village.md#refonte-de-la-carte).
