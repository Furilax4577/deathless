# Direction artistique

{dev} Les images qui fixent le cap visuel de Deathless. Depuis le 28/09/2026, Quentin passe par **Grok Imagine** pour la direction artistique : il génère, choisit, et les images retenues deviennent la cible des générateurs du jeu (maisons, arbres, carte). Une image n'est jamais copiée telle quelle : on en tire des règles.

## Comment ça marche

1. **Générer** : dans Grok, ou par l'API avec `python Docs/outils/grok_image.py <prompt>` (prompts dans `Docs/da/prompts/`, clé dans la variable d'environnement `XAI_API_KEY`, jamais dans le dépôt ; chaque image est facturée). Les images arrivent dans `Docs/references/`, le journal dans `Docs/da/journal.md`.
2. **Choisir** : Quentin retient une image. Elle entre sur cette page avec ce qu'on en garde et ce qu'on écarte.
3. **Traduire** : les règles chiffrées vont dans le guide de style (`Docs/style-kaykit.md`) et dans les fiches des générateurs ; chaque agent compare sa capture à l'image avant de conclure.

## Partage des rôles {décidé}

Grok donne la **composition et les idées** (où sont les choses, les volumes, l'ambiance) ; le **style de surface reste celui du jeu** : facettes plates, une couleur par facette avec le dégradé de l'atlas, aucune texture, formes trapues. Les images de Grok sont belles mais texturées (briques et planches peintes, herbe détaillée) : on n'en copie jamais le rendu (Quentin, 28/09/2026). Pour rester au plus près du jeu, on part d'une **capture du prototype** (`grok_image.py --depuis`) et on demande la variante.

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
