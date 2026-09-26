# Sons

Tous les sons du projet s'écoutent dans le wiki, page **Sons** (`Wiki/pages/sons.md`, générée depuis `Wiki/data/sons.json`). Ils ont été copiés de Relic le 25/09/2026 (mêmes noms de fichiers, mêmes `.meta`, donc mêmes GUID), puis complétés le même jour par 35 fichiers créés pour Deathless (arc et arbalète refaits par modélisation physique, et tous les sons qui étaient « à créer »). Aucun n'est encore branché dans le jeu : le statut « utilisé » du catalogue veut dire « a un usage clair dans Deathless ».

## Identité sonore de Deathless (depuis le 26/09/2026)

Tous les sons et musiques doivent être **régénérés avec une identité propre** : direction artistique, palette de timbres (gemme de Nyxessa, bois sec de l'interface, os creux des squelettes…), inventaire complet des sons nécessaires, musiques à couches, niveaux cibles, nommage et plan de production en lots dans **`Docs/son-cahier-des-charges.md`**. Les nouveaux sons vivent dans `Assets/Audio/Deathless/<Famille>/` (un script `synth_<famille>.py` par famille, briques communes dans `Assets/Audio/Deathless/deathless_audio.py`, crête -1,4 dBFS, niveau perçu réglé sur une cible par son), entrent au catalogue sous un id `dl_*` en `a_ecouter`, et sont mesurés par `python -B Assets/Audio/Deathless/controle.py` (une planche par lot, `Docs/son-lotN-controle.md`). Lots faits : 1 (Nyxessa, interface) et 2 (Nyxessa suite, bouclier et sorcier, portail). Ce qui suit décrit les sons existants, qu'ils remplaceront peu à peu.

## Organisation de `Assets/Audio/`

| Dossier | Contenu | Licence |
|---|---|---|
| `Relic/` | 90 effets sonores générés en Python pur pour Relic, et 35 créés pour Deathless le 25/09/2026 (même dossier, mêmes conventions) ; `LISEZMOI.md` donne l'usage, le script et la graine de chaque fichier | Aucune : créés pour le projet |
| `Relic/Musique/` | 3 musiques générées, en boucle sans raccord (jour, nuit, donjon), WAV stéréo | Aucune |
| `Relic/Sources/` | Scripts de synthèse : ceux de Relic (`synth_sounds2.py` à `synth_sounds7.py`, `synth_b_fix2.py`, `synth_music.py`) et ceux de Deathless (`synth_physique.py`, `synth_deathless.py`) ; Unity ignore les `.py` | Aucune |
| `Kenney/RPGAudio/` | 51 sons d'objets (pas, pièces, portes, livres, cuir, lames, métal) | CC0, `License.txt` |
| `Kenney/InterfaceSounds/` | 100 sons d'interface (clics, tics, confirmations, erreurs, gong…) | CC0, `License.txt` |

Conventions héritées de Relic : noms de fichiers en anglais, en minuscules ; variantes d'un même son numérotées `_1`, `_2`… (tirées au hasard en jeu) ; boucles suffixées `_loop`. Les `.wav`, `.ogg` et `.mp3` sont suivis par Git LFS (`.gitattributes`).

Les références de `Relic/LISEZMOI.md` à l'éditeur de Relic (menu **Relic > Sons > Installer**, `SoundSetup.cs`, `GameAudio`, `SonsEssais/ecoute.html`) n'existent pas dans Deathless : le fichier est gardé tel quel comme description des sons.

## Ajouter un son au catalogue

1. Déposer le fichier sous `Assets/Audio/` (un nouveau son généré pour Deathless : dans `Relic/` pour l'instant, ou un nouveau dossier `Deathless/` ; un pack tiers : son propre dossier avec sa licence, et une ligne dans `Docs/assets-tiers.md`). Unity crée le `.meta` à l'import.
2. Ajouter une entrée dans `Wiki/data/sons.json` (une ligne par son) :
   - `id` : identifiant unique, sans espace (sert d'ancre `#son-<id>` dans la page) ;
   - `nom` : nom explicite en français, « Famille — détail » (« Arc — tir », « Squelette — sortie de terre ») ;
   - `categorie` : une des catégories existantes (Interface, Joueurs, Armes, Sorts du mage, Paladin, Viking, Assassin et rôdeur, Nyxessa et portail, Ennemis, Jour et nuit, Musique, Donjon et défenses, Kenney divers…) ; l'ordre des catégories dans la page est celui de leur première apparition dans le fichier ;
   - `usage` : quand il joue dans Deathless (ou « Piste : … » s'il n'a pas d'usage décidé) ;
   - `fichier` : chemin sous `Assets/Audio/` ; `variantes` : liste de tous les fichiers si plusieurs forment le même son (le premier est aussi dans `fichier`) ;
   - `source`, `licence` ;
   - `statut` : `a_ecouter` (créé, pas encore validé à l'écoute par Quentin), `utilise`, `disponible` ou `a_creer` (pour un son à créer : `fichier` à `null`).
3. Quand un son « à créer » existe enfin, remplir `fichier`, `source`, `licence` et passer son statut à `a_ecouter` ; une fois validé à l'écoute, à `utilise`. Une ancienne version remplacée reste au catalogue en `disponible`, nommée « … (ancienne version) », pour comparer. L'encart en haut de la page Sons (balise `{sons à écouter}`, ancre `#a-ecouter`) liste tous les sons `a_ecouter`.
4. `python Wiki/build.py` : la page Sons se régénère et le fichier est copié dans `Wiki/site/sons/`. Un fichier absent est signalé dans la console et dans la page (« fichier absent »).

## Générer un nouveau son

Les sons sont synthétisés en Python pur (bibliothèque standard seulement, WAV 44,1 kHz 16 bits), sans échantillon, avec une graine fixe : relancer un script redonne exactement le même fichier (vérifié le 25/09/2026 sur `parry.wav` et sur les 35 fichiers de Deathless). Direction sonore de Relic : sombre et organique (graves, souffles, craquements, réverbération de pierre), jamais de clochette ni de mélodie enfantine. Exception voulue pour Deathless : Nyxessa et le portail (gemmes vertes) sonnent cristallins (verre, carillons, résonances claires), l'interface en cloches douces.

- `synth_sounds2.py` : les briques de base (`white`, `bandpass`, `lowpass`, `envelope`, `tone`, `reverb`, `grains`, `mix`, écriture WAV) ; `synth_sounds3.py` à `synth_sounds6.py` : briques ajoutées au fil des essais (voix, feu, bruit brun, boucles) ; les scripts s'importent entre eux, ils doivent rester ensemble.
- `synth_sounds7.py` : le catalogue principal. Chaque son est une fonction (`parry()`, `viking_roar()`…) et une ligne de `CATALOG` : `"nom": (fonction, boucle, nombre de variantes, graine)`. Il fournit aussi des briques prêtes : `noise`, `rumble`, `thump` (coup sourd), `knock`, `click`, `whoosh`, `pluck` (corde d'arc), `creak`, `bones`, `gravel`, `fire_roar`.
- `synth_music.py` : les trois musiques (`jour`, `nuit`, `donjon`).
- `synth_physique.py` (Deathless) : armes de trait par **modélisation physique**, sans réverbération : corde en guide d'onde (`string` : forme triangulaire de la corde tirée, T60, hauteur qui retombe, claquement dépendant de l'amplitude), corps par synthèse modale (`modal`, `resonator` : modes fréquence / T60 / amplitude, excités par `strike`, un choc dont la largeur fixe la dureté), projectile qui s'éloigne (`recede` : Doppler, 1 / distance, absorption de l'air), frottement colle-glisse (`stick_slip`) pour les grincements, `rustle` (tissu), mastering `finish`. Sons : `bow_shot_v3`, `bow_shot_v3_charged`, `bow_draw`, `crossbow_shot_v3`, `crossbow_reload` (graines 772 à 776). L'entête explique pourquoi les anciens arcs sonnaient synthétiques.
- `synth_deathless.py` (Deathless) : les sons qui étaient « à créer » (Nyxessa, portail, interface, critiques, furtif, grenade, brûlure ; graines 780 à 803), avec les briques `ping` (tintement à partiels : `GLASS`, `BELL`, `GOLD`), `sparkle` (scintillement de gemmes), `air`, `sub`, `bubble` (goutte), `master` et `loop_master`. Les sons « retour » et « sortie » sont les sons d'aller retournés.

**Niveau** : tous les fichiers sont écrits avec une crête à 0,8 (-1,9 dB) comme ceux de Relic ; les scripts de Deathless abaissent en plus la crête d'un son trop dense pour que son volume perçu (RMS maximal sur 50 ms, `synth_physique.loudness_db`) ne dépasse pas **-14 dB**, la médiane des sons du projet (-15 dB pour les boucles, -12,5 à -13 dB pour les événements majeurs : tir chargé, meilleur critique, rappel, destruction de Nyxessa, victoire ; table `LOUD` de chaque script).

Pour créer un son :

1. Écrire la fonction dans `synth_deathless.py` (ou `synth_physique.py` pour un son d'objet physique), en partant du son le plus proche, puis l'ajouter à `CATALOG` avec une graine neuve (après 803).
2. Générer **hors de `Assets/`** pour écouter, avec `-B` pour ne pas créer de `__pycache__` (Unity lui ferait des `.meta`) :
   `cd Assets/Audio/Relic/Sources` puis `python -B synth_deathless.py <dossier_d_essai> <nom> [nom ...]` (idem `synth_physique.py`, `synth_sounds7.py` ; sans nom : tout le catalogue ; variantes écrites `<nom>_1.wav`, `<nom>_2.wav`…).
   Musiques : `python -B synth_music.py <dossier> jour|nuit|donjon` (plusieurs minutes de calcul).
3. Copier le fichier dans `Assets/Audio/Relic/` (ou générer directement avec `..` comme dossier de sortie), ajouter sa ligne au `LISEZMOI.md` du dossier et au catalogue `Wiki/data/sons.json` en `a_ecouter` ; Quentin l'écoute depuis l'encart de la page Sons, puis il passe en `utilise`.
