# Wiki de Deathless

Le wiki fixe les règles du jeu : les pages sources sont dans `pages/` (Markdown). Le générateur produit deux versions, non versionnées :

- `site/` : **version développeur**, complète (pistes, pages À décider, Effets, Sons, notes techniques) ;
- `public/` : **version joueur**, seulement les règles décidées, sans étiquettes de statut ni détails techniques.

- **Consulter** : double-clic sur `ouvrir-wiki.cmd` (version développeur) ou `ouvrir-wiki-joueur.cmd` (version joueur) ; chacun génère les deux versions puis ouvre la sienne. Python 3 suffit, aucune dépendance.
- **Marquer pour la version joueur** : `{dev}` sur une ligne, une ligne de tableau, un titre de section ou une cellule d'en-tête (colonne entière) = réservé aux développeurs ; `{{dev: texte}}` = morceau de phrase réservé aux développeurs ; `{public}` = ligne réservée à la version joueur. Tout ce qui porte `{à confirmer}` est retiré de la version joueur, `{à équilibrer}` y devient « valeurs provisoires ». Les pages réservées aux développeurs sont marquées `"dev"` dans `MENU` (`build.py`).
- **Modifier** : éditer un fichier de `pages/`, puis relancer `ouvrir-wiki.cmd`. L'ordre du menu est dans `build.py` (`MENU`).
- **Étiquettes** : `{décidé}`, `{à confirmer}`, `{effet validé}` ; pastille de couleur : `{couleur #rrggbb}`.
- **Catalogue des sons** : deux balises, chacune seule sur sa ligne, remplacées à la génération à partir de `data/sons.json` : `{catalogue sons}` (barre de recherche et de filtres, puis un tableau par catégorie avec un lecteur audio par son et par variante) et `{sons à créer}` (les sons nécessaires qui n'existent pas encore). Elles sont utilisées par `pages/sons.md`. Les fichiers audio référencés sont copiés de `Assets/Audio/` dans `site/sons/` (seulement ceux qui ont changé ; les fichiers qui ne sont plus au catalogue en sont retirés), car le site ne peut pas lire `Assets/`. Format du catalogue et ajout d'un son : `Docs/sons.md`.
- Publication : en local pour l'instant (décision du 25/09/2026).
