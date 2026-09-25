# Sons

Tous les sons du projet s'écoutent dans le wiki, page **Sons** (`Wiki/pages/sons.md`, générée depuis `Wiki/data/sons.json`). Ils ont été copiés de Relic le 25/09/2026 (mêmes noms de fichiers, mêmes `.meta`, donc mêmes GUID). Aucun n'est encore branché dans le jeu : le statut « utilisé » du catalogue veut dire « a un usage clair dans Deathless ».

## Organisation de `Assets/Audio/`

| Dossier | Contenu | Licence |
|---|---|---|
| `Relic/` | 90 effets sonores générés en Python pur pour Relic ; `LISEZMOI.md` donne l'usage, le script et la graine de chaque fichier | Aucune : créés pour le projet |
| `Relic/Musique/` | 3 musiques générées, en boucle sans raccord (jour, nuit, donjon), WAV stéréo | Aucune |
| `Relic/Sources/` | Scripts de synthèse (`synth_sounds2.py` à `synth_sounds7.py`, `synth_b_fix2.py`, `synth_music.py`) ; Unity ignore les `.py` | Aucune |
| `Kenney/RPGAudio/` | 51 sons d'objets (pas, pièces, portes, livres, cuir, lames, métal) | CC0, `License.txt` |
| `Kenney/InterfaceSounds/` | 100 sons d'interface (clics, tics, confirmations, erreurs, gong…) | CC0, `License.txt` |
| `Incompetech/` | 3 musiques de Kevin MacLeod, non jouées | CC BY 4.0 : **crédit obligatoire** si un morceau est joué (texte dans `Wiki/pages/credits.md`) |

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
   - `statut` : `utilise`, `disponible` ou `a_creer` (pour un son à créer : `fichier` à `null`).
3. Quand un son « à créer » existe enfin, remplir `fichier`, `source`, `licence` et passer son statut à `utilise`.
4. `python Wiki/build.py` : la page Sons se régénère et le fichier est copié dans `Wiki/site/sons/`. Un fichier absent est signalé dans la console et dans la page (« fichier absent »).

## Générer un nouveau son

Les sons de Relic sont synthétisés en Python pur (bibliothèque standard seulement, WAV 44,1 kHz 16 bits), sans échantillon, avec une graine fixe : relancer un script redonne exactement le même fichier (vérifié le 25/09/2026 sur `parry.wav`). Direction sonore de Relic : sombre et organique (graves, souffles, craquements, réverbération de pierre), jamais de clochette ni de mélodie enfantine.

- `synth_sounds2.py` : les briques de base (`white`, `bandpass`, `lowpass`, `envelope`, `tone`, `reverb`, `grains`, `mix`, écriture WAV) ; `synth_sounds3.py` à `synth_sounds6.py` : briques ajoutées au fil des essais (voix, feu, bruit brun, boucles) ; les scripts s'importent entre eux, ils doivent rester ensemble.
- `synth_sounds7.py` : le catalogue principal. Chaque son est une fonction (`parry()`, `viking_roar()`…) et une ligne de `CATALOG` : `"nom": (fonction, boucle, nombre de variantes, graine)`. Il fournit aussi des briques prêtes : `noise`, `rumble`, `thump` (coup sourd), `knock`, `click`, `whoosh`, `pluck` (corde d'arc), `creak`, `bones`, `gravel`, `fire_roar`.
- `synth_music.py` : les trois musiques (`jour`, `nuit`, `donjon`).

Pour créer un son :

1. Écrire la fonction dans `synth_sounds7.py` (en partant du son le plus proche : par exemple `ranger_focus` pour « Arc — chargé à fond », `portal_open` pour la charge de Nyxessa), puis l'ajouter à `CATALOG` avec une graine neuve (après 771).
2. Générer **hors de `Assets/`** pour écouter, avec `-B` pour ne pas créer de `__pycache__` (Unity lui ferait des `.meta`) :
   `cd Assets/Audio/Relic/Sources` puis `python -B synth_sounds7.py <dossier_d_essai> <nom> [nom ...]` (sans nom : tout le catalogue ; variantes écrites `<nom>_1.wav`, `<nom>_2.wav`…).
   Musiques : `python -B synth_music.py <dossier> jour|nuit|donjon` (plusieurs minutes de calcul).
3. Faire valider à l'écoute par Quentin, puis copier le fichier retenu dans `Assets/Audio/Relic/` (ou `Deathless/`), ajouter sa ligne au `LISEZMOI.md` du dossier et au catalogue `Wiki/data/sons.json`.
