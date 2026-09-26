# Brief pour une session cloud : direction sonore plus sombre, regénération des lots 1 et 2

À coller comme premier message d'une session cloud Claude Code sur le dépôt `Furilax4577/deathless`. Il remplace le brief du missile crâne (`cloud-son-missile.md`), qui est absorbé ici.

---

Tu travailles sur **Deathless** (jeu Unity coop à 4 : la nuit, on défend Nyxessa, la relique verte qui nous ressuscite, contre des vagues de squelettes ; le jour, on pille un donjon). Tu es en session cloud : pas d'éditeur Unity, pas de serveur ; seulement des fichiers texte, des scripts Python (bibliothèque standard, aucun paquet, `python` jamais `python3`) et des WAV générés. Réponds et rédige en français. Branche `cloud/son-direction-dark`, pull request à la fin. Ne modifie ni `Assets/Scripts/`, ni les `.asset` et `.meta` Unity.

## Lis d'abord
- `Docs/son-cahier-des-charges.md`, et d'abord l'encadré **« Correction de direction, 26/09/2026 au soir »** en tête du § 1, puis § 2, § 3.1, § 3.2, § 3.3, § 3.4, § 5, § 8 et § 8.2.
- `Assets/Audio/Deathless/deathless_audio.py`, les scripts `synth_*.py` des lots 1 et 2 (`Nyxessa/`, `Interface/`, `Bouclier/`, `Portail/`), `controle.py`.
- L'ancien missile magique de Relic (id `skull_*` dans `Wiki/data/sons.json`, fichiers `Assets/Audio/Relic/`) : analyse ses WAV (durée, enveloppe, spectre par bandes) avec la bibliothèque standard. Quentin adore ce son : des **plaintes des limbes** pendant le vol, puis un **cri** à l'éclat. C'est la référence de caractère.

## Le retour de Quentin
Après écoute des lots 1 et 2 : « trop cristallins, enfantins ; je veux une vraie dimension plus dark ». Il veut :
- des **vocalises pour Nyxessa** dans l'esprit du missile magique : une âme captive qui gémit, appelle, hurle ;
- **plus d'instruments et de variété** : bourdons graves, métal frotté, os creux, peaux tendues, souffles, chœurs sourds, cordes grattées, et non des tintements partout ;
- un caractère **sombre et organique** de base, l'humour restant réservé aux classes comiques.

## Ta tâche
1. **Réécrire le § 1 et le § 2 du cahier des charges** pour cette direction : caractère, ce qu'on évite (revoir la liste : les chœurs sourds et le bourdon deviennent voulus), palette de timbres avec les **nouvelles briques** : voix fantôme (synthèse par formants : 2 ou 3 résonances filtrées sur une source à impulsions, vibrato lent, glissandos, souffle), chœur sourd (3 à 5 voix fantômes détunées), bourdon grave (oscillateurs détunés, battements lents), métal frotté (partiels inharmoniques excités lentement, comme un archet sur une plaque), os creux (résonance tubulaire courte), peau tendue (percussion à tonalité descendante), souffle (bruit filtré modulé). Garde le tintement de gemme pour la **signature de Nyxessa**, mais plus grave, plus sombre, et toujours mêlé à une voix ou un bourdon, jamais seul.
2. **Ajouter ces briques à `deathless_audio.py`**, chacune avec un commentaire qui décrit le modèle acoustique et ses paramètres.
3. **Regénérer les lots 1 et 2** sous la nouvelle direction, **mêmes identifiants et mêmes noms de fichiers** (le jeu les charge déjà) :
   - **Nyxessa** : chaque son porte une vocalise. Le tir est un appel bref ; frappée, une plainte courte ; l'alerte, un cri d'appel ; le palier, un chœur qui s'ouvre ; la charge du portail et le retour d'énergie, un glissando de voix avec le bourdon ; la destruction, un long cri qui s'éteint dans le bourdon ; le rappel et la réapparition, des voix qui s'éloignent puis reviennent.
   - **Missile crâne** : vol = chœur de plaintes des limbes en boucle sans raccord (1,5 à 2 s), sur un fond d'os creux qui siffle ; éclat = cri bref et déchirant puis gemmes sombres et souffle, 3 variantes.
   - **Bouclier et sorcier** : bourdon et métal frotté à la levée, coups sourds d'os et de peau, bris en chœur qui se déchire, incantation en voix sourde continue, canalisation en bourdon avec battements.
   - **Portail** : bourdon grave permanent, ouverture et fermeture en souffle et chœur, départ et arrivée en voix qui glissent, chute du ciel et sortie du sol en peaux et souffles.
   - **Interface** : reste sobre et lisible, mais sombre : bois sombre et os plutôt que cristal ; le décompte et « tous prêts » prennent une peau tendue ou un bourdon court, pas de carillon.
4. **Contrôle** : `Docs/son-lots1-2-dark-controle.md` par `controle.py`, tous conformes (crêtes, niveaux cibles du § 5, pas de silence en tête).
5. **Catalogue** : `Wiki/data/sons.json`, mêmes ids, `usage` mis à jour, statut `a_ecouter`.
6. § 8 et § 8.2 du cahier : une note « regénérés sous la direction sombre le <date> ».

## Contraintes
- Les sons doivent rester **lisibles en mêlée** et **courts** (§ 5) : sombre ne veut pas dire long ni boueux. Attaques franches, fins nettes.
- Ne pas modifier les scripts du jeu ni les `.meta`.
- Dans la pull request : comment les voix sont synthétisées, les paramètres des plaintes et des cris, et ce que la synthèse ne pourra pas donner (vraies voix, instruments enregistrés), pour la décision de Quentin sur la musique.
