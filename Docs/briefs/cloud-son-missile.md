# Brief pour une session cloud : refaire les sons du missile crâne de Nyxessa

À coller comme premier message d'une session cloud Claude Code sur le dépôt `Furilax4577/deathless`. Retour de Quentin du 26/09/2026 sur le lot 2 : tout est validé **sauf le missile crâne** (vol et éclat).

---

Tu travailles sur **Deathless** (jeu Unity coop à 4). Tu es en session cloud : pas d'éditeur Unity, pas de serveur ; seulement des fichiers texte, des scripts Python (bibliothèque standard, aucun paquet, `python` jamais `python3`) et des WAV générés. Réponds et rédige en français. Branche `cloud/son-missile-crane`, pull request à la fin. Ne modifie ni `Assets/Scripts/`, ni les `.asset` et `.meta` Unity.

## Lis d'abord
- `Docs/son-cahier-des-charges.md` (§ 1, § 2, § 3.2 Nyxessa, § 5, § 8.2 lot 2) et `Assets/Audio/Deathless/deathless_audio.py`, `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py`, `controle.py`.
- Le missile de Nyxessa en jeu : un **crâne en gemmes vertes** qui file vers un squelette, en léger guidage, puis éclate au contact (`Assets/VFX/MissileMagique/SkullMissileVisual.cs`, `Assets/Scripts/Jeu/DefenseNyxessa.cs`). Vol de 0,5 à 2 s selon la distance.

## Retour de Quentin sur la version actuelle du lot 2
Les sons `dl_nyxessa_missile_*` (vol en boucle, éclat) ne conviennent pas : **« pas assez crâne »**, et surtout **pas assez vocal**. L'ancien son (Relic, id `skull_*` dans `Wiki/data/sons.json`, fichiers `Assets/Audio/Relic/`) faisait des **complaintes des limbes** pendant le vol, puis **explosait dans un cri**. Quentin adore ce caractère : le missile est une âme captive qui hurle, pas une gemme qui siffle. Écoute-le mentalement en lisant ses paramètres si tu peux, ou analyse ses WAV (durée, enveloppe, spectre) avec la bibliothèque standard pour t'en rapprocher.

## À produire
- **Vol** (`dl_nyxessa_missile_vol`, boucle sans raccord, 1,5 à 2 s) : un chœur de **plaintes fantomatiques**, 2 ou 3 voix synthétiques par formants (voyelles sombres « ô », « ou », glissandos lents descendants et remontées plaintives, léger vibrato, détunage entre les voix), sur un fond d'os creux qui siffle (brique os du § 2) et une pointe de gemme (thème Nyxessa, tintement discret). Doppler et intensité seront gérés par le jeu : garde le son stable.
- **Éclat** (`dl_nyxessa_missile_eclat_1..3`, 0,6 à 0,9 s, 3 variantes) : un **cri** bref et déchirant, formant vocal ouvert qui monte puis se brise, suivi de l'éclat de gemmes (brique gemme, plus aigu) et d'un souffle court. Pas de bruit blanc plat : le cri doit rester lisible comme une voix.
- Niveaux : vol à −20 dBFS de niveau perçu (il est en boucle et proche de Nyxessa), éclat à −13.
- Ajoute à `deathless_audio.py` une brique **voix fantôme** (synthèse par formants : 2 ou 3 résonances filtrées sur une source à impulsions, vibrato, glissando) réutilisable pour le Nécromancien et les âmes du donjon plus tard, avec un commentaire qui décrit le modèle.
- Entrées de `Wiki/data/sons.json` : mêmes ids, statut `a_ecouter`, `usage` mis à jour ; planche `Docs/son-lot2b-controle.md` par `controle.py` ; § 8.2b dans le cahier des charges avec la table de branchement (constantes `SonsDuJeu.MissileVol`/`MissileEclat` ou équivalentes, à vérifier dans `Assets/Scripts/Jeu/Audio/SonsDuJeu.cs`).

## Rapport
Dans la pull request : comment la voix est synthétisée, les paramètres des plaintes et du cri, et ce qu'il faudrait pour aller plus loin (voix enregistrées, banque).
