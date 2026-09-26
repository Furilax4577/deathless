# Brief pour une session cloud : un lot de sons de Deathless

À coller comme premier message d'une session cloud Claude Code sur le dépôt `Furilax4577/deathless`, en remplaçant `N` par le numéro du lot. Une session par lot ; les lots sont indépendants. Le lot 1 (Nyxessa, interface) a été produit et validé le 26/09/2026 : c'est le modèle à suivre.

---

Tu travailles sur **Deathless** (jeu Unity coop à 4, défense de Nyxessa la nuit, donjon le jour). Tu es en session cloud : pas d'éditeur Unity, pas de serveur. Tu ne touches qu'à des fichiers texte, des scripts Python (bibliothèque standard seulement, aucun paquet, `python` jamais `python3`) et des WAV générés. Réponds et rédige en français.

Branche `cloud/son-lot-N`, pull request à la fin. Ne modifie ni `Assets/Scripts/`, ni les `.asset` et `.meta` Unity.

## Lis d'abord
- `Docs/son-cahier-des-charges.md` : la direction artistique (§ 1), la palette de timbres (§ 2), l'inventaire par famille (§ 3), les contraintes (§ 5), le plan de production (§ 7.2) et le lot 1 (§ 8).
- `Assets/Audio/Deathless/deathless_audio.py` : la bibliothèque commune de synthèse (briques de timbres, enveloppes, normalisation, écriture WAV) ; `synth_nyxessa.py` et `synth_interface.py` comme modèles ; `controle.py` pour la planche de contrôle.
- `Wiki/data/sons.json` : le catalogue (une entrée par id, statut `a_ecouter` pour un nouveau son).

## Ta tâche : produire le **lot N** du § 7.2
- Un script par famille, `Assets/Audio/Deathless/<Famille>/synth_<famille>.py`, relançable et déterministe (graines fixes), qui écrit les WAV de la famille (44,1 kHz mono 16 bits, crête sous −1,4 dBFS, pas de silence en tête, niveau à la cible du § 5, pas de réverbération).
- Si le lot demande une **nouvelle brique** de timbre (os creux, éclat d'or, feu…), ajoute-la à `deathless_audio.py` en suivant le § 2, avec un commentaire qui décrit le modèle acoustique.
- Les entrées du catalogue dans `Wiki/data/sons.json` (ids `dl_<famille>_<evenement>`, `source: "Deathless (synthèse)"`, `licence: "propre"`, `statut: "a_ecouter"`, `variantes`, `usage`, `portee` en mètres selon le § 3).
- La planche de contrôle `Docs/son-lotN-controle.md` par `python -B Assets/Audio/Deathless/controle.py` (tous les fichiers doivent être conformes).
- Dans `Docs/son-cahier-des-charges.md`, un § 8.N « Lot N produit » sur le modèle du § 8 : fichiers, ids, et la table « Branchement proposé » (constante de `SonsDuJeu` → nouvel id), sans toucher au code.

Si un son du lot a besoin d'une information de jeu que tu ne trouves pas (durée d'un geste, nombre de variantes), prends le cahier des charges comme référence et note l'hypothèse dans le § 8.N.

## Rapport
Dans la description de la pull request : la famille, le nombre de sons et de fichiers, les briques ajoutées, ce qui reste moins bon en synthèse, et les hypothèses prises.
