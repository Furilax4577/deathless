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
