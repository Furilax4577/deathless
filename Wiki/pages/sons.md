# Sons

Tous les sons présents dans le projet, à écouter ici. Ils viennent de Relic, le projet précédent (des sons générés en Python pour le projet, les packs Kenney RPG Audio et Interface Sounds), ou ont été créés pour Deathless de la même façon, en Python pur. Les fichiers sont dans `Assets/Audio/`.

- **à écouter** : le son vient d'être créé pour Deathless et attend d'être validé à l'écoute. L'encart ci-dessous mène à chacun.
- **utilisé** : le jeu joue ce son.
- **disponible** : le son est dans le projet mais le jeu ne le joue pas : sans usage décidé (une piste est parfois proposée), usage prévu pas encore branché (« Prévu : … »), ou ancienne version remplacée, gardée pour comparer.
- **à créer** : Deathless a besoin de ce son et il n'existe pas encore ; en attendant, le jeu joue un son proche, cité dans l'usage. Voir la section [À créer](sons.md#a-creer) en bas de page.

{sons à écouter}

Les boucles (vol d'un projectile, cône de flammes, bourdonnement du portail…) sont marquées « (boucle) » : le lecteur ne les joue qu'une fois. Un son en plusieurs variantes a un lecteur par variante, tirée au hasard en jeu.

> Le catalogue est le fichier `Wiki/data/sons.json`. Pour ajouter un son ou en générer un nouveau avec les scripts de synthèse, voir `Docs/sons.md`.

{catalogue sons}

{sons à créer}

## Musique

Le jeu a un lecteur de musique (`Assets/Scripts/Audio/LecteurMusique.cs`, {effet validé}) prêt à recevoir les morceaux ; il n'y en a pas encore. Cinq fichiers sont attendus dans `Assets/Audio/Deathless/Musique/` : `jour_1.ogg`, `jour_2.ogg`, `nuit_1.ogg`, `nuit_2.ogg`, `taverne.ogg` (le brief de création est dans `Docs/da/brief-musiques.md`). Tant qu'ils manquent, le jeu reste silencieux, sans message d'erreur ; dès qu'ils sont déposés, l'éditeur les branche tout seul sur les réglages audio (menu `Deathless > Audio > Brancher les musiques` pour le refaire à la main).

| Liste | Morceaux | Jouée quand |
|---|---|---|
| Jour | `jour_1`, `jour_2` | jour, crépuscule, aube, et au menu principal |
| Nuit | `nuit_1`, `nuit_2` | nuit (et nuit forcée de l'aperçu, touche F9) |
| Taverne | `taverne` | le héros est à l'intérieur de la taverne (quel que soit le moment) |

- Les morceaux d'une même liste s'enchaînent en alternance, avec un fondu enchaîné de 2,5 s ; la liste de la taverne n'ayant qu'un morceau, il se rejoue sur lui-même.
- Au changement de liste (entrée dans la taverne, tombée de la nuit), la nouvelle liste monte en fondu enchaîné de 2,5 s ; si la liste ne change pas, le morceau en cours continue (pas de redémarrage au changement de scène).
- En réseau, chaque poste joue sa propre musique d'après sa phase de partie et sa zone : rien n'est envoyé.
- Le volume se règle dans les options (réglage Musique, groupe Musique du mixeur, 10 % par défaut).
- Les anciennes boucles `musique_dehors_jour` et `musique_dehors_nuit` ne sont plus jouées par le jeu.

## Sources et licences

| Source | Licence | Crédit |
|---|---|---|
| Sons générés pour Relic et pour Deathless (Python pur, aucun échantillon tiers) | Aucune : créés pour le projet | Aucun |
| Kenney, RPG Audio et Interface Sounds | CC0 | Facultatif, donné dans les [Crédits](credits.md) |
| Sonniss.com, GDC Game Audio Bundle (344 Audio ; David Dumais Audio) | Libre de droits pour les jeux (licence du bundle) | Donné dans les [Crédits](credits.md) |
