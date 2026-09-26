# Brief pour une session cloud : cahier des charges son et musique de Deathless

À coller tel quel comme premier message d'une session cloud Claude Code sur le dépôt `Furilax4577/deathless`. La session travaille sur une branche et ouvre une pull request ; rien n'est publié depuis le cloud (le wiki et le jeu sont publiés depuis le poste de Quentin, après vérification dans Unity).

---

Tu travailles sur **Deathless**, un jeu Unity coopératif à 4 joueurs : le jour, on pille un donjon ; la nuit, on défend Nyxessa (la relique verte qui nous ressuscite) contre des vagues de squelettes, jusqu'à Morgrim, le Roi des os, et au Nécromancien. Style low poly lisse, ton léger (un clochard pétomane et un DJ sont candidats comme personnages jouables). Réponds et rédige en **français**.

Tu es dans une session cloud : **pas d'éditeur Unity, pas de serveur**. Tu ne touches qu'à des fichiers texte, des scripts Python et des WAV générés. Travaille sur une branche `cloud/son-cahier-des-charges` et ouvre une pull request à la fin. Ne modifie pas `Assets/Scripts/`, ni les `.asset` et `.meta` Unity : Quentin et l'agent local les brancheront.

## Ce qui existe déjà (lis-le d'abord)

- `Wiki/data/sons.json` : le catalogue, 210 sons. Champs : `id`, `nom`, `categorie`, `usage`, `fichier`, `variantes`, `source`, `licence`, `statut` (`utilise`, `disponible`, `a_ecouter`, `a_creer`). Catégories actuelles : Interface, Joueurs, Paladin, Viking, Assassin et rôdeur, Sorts du mage, Armes, Ennemis, Nyxessa et portail, Donjon et défenses, Jour et nuit, Village, Musique, et deux fourre-tout « Kenney divers ».
- Origines : bruitages **Kenney RPG Audio** (CC0, 111 sons), extraits **Sonniss GDC** (libres de droits, sélectionnés à la main : souffle de hache, habillage du trailer), sons **synthétisés par script** (`Assets/Audio/Forge/synth_enclume.py` : Python standard, `wave`/`math`/`random`/`struct`, WAV 44,1 kHz mono 16 bits, graines fixes, normalisation à −1,4 dBFS, pas de réverbération : l'espace vient du jeu). Deux **musiques** héritées de Relic (`musique_dehors_jour`, `musique_dehors_nuit`).
- `Assets/Scripts/Jeu/Audio/SonsDuJeu.cs` : les identifiants que le code appelle (par exemple `SonsDuJeu.Parade`, `ForgeEnclume`, `TournanteVent`, `MusiqueJour`). Lis-le pour connaître **chaque événement sonore du jeu**.
- `Assets/Scripts/Jeu/Audio/AudioBank.cs` : lecture 3D par point, tirage au hasard des variantes, anti-répétition.
- `Wiki/pages/sons.md`, `credits.md`, `effets.md`, `classes.md`, `classe-*.md`, `ennemis.md`, `nyxessa.md`, `deroule.md`, `village.md`, `statuts.md` : les règles du jeu, à lire pour savoir ce que chaque son accompagne.
- `Docs/vfx.md` : le langage visuel (gemmes low poly, palettes) que le son doit épouser.

## Ce que Quentin veut

**Regénérer tous les sons et les musiques** avec une identité propre à Deathless, à la place du mélange actuel. Il veut d'abord un **cahier des charges**, puis la production.

### Livrable 1 : `Docs/son-cahier-des-charges.md`

1. **Direction artistique sonore** en une page : le caractère (chaleureux, matière bois-pierre-os, gemmes qui tintent pour tout ce qui vient de Nyxessa, humour assumé sur les classes comiques), ce qu'on évite (réalisme militaire, nappes cinéma), la place du silence, le rapport jour/nuit.
2. **Palette** : quelques timbres récurrents qui font la signature (par exemple un tintement de gemme pour Nyxessa, un bois sec pour l'interface, un os creux pour les squelettes), avec pour chacun une description acoustique précise (spectre, enveloppe, durée) qu'un script peut suivre.
3. **Inventaire complet des sons nécessaires**, en un tableau par famille, à partir de `SonsDuJeu.cs`, du catalogue et des règles du wiki : identifiant, événement déclencheur, durée cible, nombre de variantes, 2D ou 3D, portée, priorité (vital / confort / bonus), et une description acoustique. Couvre tout, y compris ce qui manque aujourd'hui : statuts (brûlure, ralenti, étourdi, renversé, ivresse), parade parfaite, onde de Morgrim, sac d'or, portails (départ, arrivée par le ciel, sortie du sol), emotes, DJ Bob et les autres candidats, Nyxessa (missiles, bouclier par palier, canalisation), ambiances (village jour/nuit, forêt, donjon, intérieurs, forge, taverne).
4. **Musiques** : liste des morceaux (menu, village de jour, crépuscule, nuit par intensité, Morgrim, Nécromancien, victoire, défaite, taverne, donjon), pour chacun : tempo, tonalité, instruments, durée, boucle ou couches superposables selon l'intensité de la nuit, transitions. Propose un système à **couches** (une base calme, des couches qui s'ajoutent quand les vagues arrivent) plutôt que des morceaux séparés.
5. **Contraintes techniques** : formats (WAV 44,1 kHz mono 16 bits pour les effets, stéréo pour les musiques), niveaux (crête −1,4 dBFS, cibles de niveau moyen par famille pour que tout soit à l'échelle), pas de réverbération dans les fichiers, nommage `famille_evenement_N.wav`, graines fixes, dossier `Assets/Audio/Deathless/<Famille>/`, un script par famille, une entrée par son dans `Wiki/data/sons.json` avec `source: "Deathless (synthèse)"` et `licence: "propre"`.
6. **Méthode de production, avec ses limites** : la synthèse par script Python standard (seule méthode possible dans le dépôt, sans téléchargement) fait très bien les impacts, tintements, souffles, interface, os, gemmes, pluie de pièces, portails, et convenablement des nappes et des percussions ; elle fait mal les voix et les instruments réalistes. Dis clairement ce qui restera moins bon en synthèse et ce que Quentin devra décider (musique par synthèse, bibliothèque libre ou outil externe).
7. **Plan de production** en lots de 15 à 25 sons, par priorité, chacun réalisable par une session cloud indépendante.

### Livrable 2 : le premier lot, pour prouver la méthode

Produis le **lot 1 (sons vitaux de Nyxessa et de l'interface)** : le script `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py` et `Assets/Audio/Deathless/Interface/synth_interface.py` sur le modèle de `synth_enclume.py`, avec 15 à 25 WAV générés, les entrées ajoutées dans `Wiki/data/sons.json` (statut `a_ecouter`), et une planche de contrôle par script (`Docs/son-lot1-controle.md` : durée, crête, niveau moyen, spectre en quelques bandes, pour chaque fichier). Ne touche pas aux `.meta` : Unity les créera.

Contraintes :
- Python 3 standard seulement, aucun paquet, aucun téléchargement. Appelle toujours `python`, jamais `python3`.
- Chaque script est relançable et déterministe (graines fixes).
- Pas de saturation, jamais de silence de plus de 20 ms en tête de fichier (le son doit partir dès le déclenchement).
- Respecte la règle du projet : le **vert** et l'énergie de Nyxessa ont leur signature (le tintement de gemme) ; les squelettes sont **os et poussière** ; le feu du mage est **feu**.

### Rapport

À la fin, dans la description de la pull request : les grandes lignes de la direction artistique, le nombre de sons inventoriés, le lot 1 produit, ce qui reste moins bon en synthèse, et les décisions attendues de Quentin.
