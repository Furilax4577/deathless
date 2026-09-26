# À faire

Travaux prévus ou notés, pas encore faits. Une ligne quitte cette page quand le travail est livré. Les règles encore à trancher sont dans [À décider](a-decider.md).

## Noté par Quentin

- **Cap : sortir de KayKit** {décidé} (26/09/2026) : à terme, remplacer tout ce qui vient de KayKit par des assets propres à Deathless, pour se différencier et avoir plus de liberté. Premières étapes : arbres, sol, bâtiments, personnages générés. Les animations (squelette Rig_Medium) viendront en dernier. Méthode retenue : **étudier KayKit d'abord** (guide chiffré `Docs/style-kaykit.md`, 26/09/2026), puis refaire chaque famille au niveau.

{jauge-kaykit}

- **Revoir le menu des options de jeu.** (noté le 25/09/2026) ; y ajouter la case « Se relever : marteler / maintenir » (aujourd'hui un simple réglage enregistré).
- **Bande-annonce Steam** : v1 tournée le 26/09/2026 (`Docs/trailer-storyboard.md`, outil `Assets/Scripts/Dev/Tournage/`), 39 s, à **écouter et valider** par Quentin ; à retourner quand le décor aura quitté KayKit.

## En attente de validation (Quentin)

- **Personnage du mois** : candidats Barde, Bavaroise, Clochard pétomane et **DJ Bob Douville** (26/09/2026, modèles lissés, pages avec rendus et clips). Choisir le premier ; alléger celui qui est retenu (Barde et Bavaroise un peu au-dessus de 8 000 triangles). Barde : l'attaque de base frappe avec le luth comme une massue, animation de jeu du luth à créer.
- **Clochard pétomane** : pet de défense provisoire (roulade avant et grosse bouffée), nuage à 2,5 s dans la vidéo au lieu de 5 s, geste pour boire à retoucher.
- **Effets des nouvelles classes** (bac à sable des effets) : nuage pestilentiel un peu opaque, flaque de la tournée en mosaïque, Trinquer discret.
- **Maison générée selon le guide de style** (`sandbox-level`, scène `StyleKayKit`) : 11 280 triangles contre 1 000 à 1 400 pour KayKit ; leviers d'allègement notés dans le guide. Si validée : arbre et sol avec le même guide, puis intégration au village (code des intérieurs agrandis et de la porte qui claque mis de côté dans git, tag `parc-maisons-generees`).
- **Arbre et sol** (preuves de concept dans `sandbox-ui`) : jugés pas assez KayKit, à refaire avec le guide.
- **Maisons la nuit** : entrer seulement le jour ? Reconduire dehors au crépuscule ? (voir [À décider](a-decider.md)).
- **Renversé** : pas d'invulnérabilité pendant la chute (choix par défaut, à confirmer).

## Plus tard

- Forêt : arbres plus riches et moins nombreux, troncs dégagés jusqu'à 1,5 fois la hauteur des personnages ; **les squelettes traversent la forêt entre les troncs** au lieu de toujours suivre le même chemin (variété des trajets : points de passage au hasard ou coût de chemin bruité). Idée de Quentin, 26/09/2026.
- Donjon, à équilibrer : montants d'or (120 / 50 / 20, +10 % par nuit), nombre de gardiens (6, dont 35 % de guerriers), ralentissement dans l'eau (×0,6), délai d'alerte (15 s), parts gardées au rappel (0 à 75 %).
- Morgrim : onde du Fracas réglée en réutilisant l'onde du Golem (un prefab dédié serait plus propre) ; effets au sol du Golem d'origine pas diffusés aux clients.
- Parade parfaite : la gerbe de gemmes envoyée devant se voit peu depuis la caméra ; chemin à deux joueurs pas testé.
- À tester à deux joueurs : emotes, charge bélier, statuts, esquive directionnelle, parade parfaite, onde de Morgrim.
- Taverne : breuvages (plus tard).
- Réseau : penché du buste en visée, arrivée en cours de partie.
- Mettre à jour la copie de `CycleJourNuit` du bac à sable du village.
- Wiki : canne à pêche provisoire, faute de modèle KayKit.
