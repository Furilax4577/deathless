# Icônes : classes et compétences

Sources SVG des icônes du jeu. Hors de `Assets/` : on les copie dans le projet Unity quand l'interface les branche.

## Contenu

- `Classes/classe_<classe>.svg` (5) : emblème dans un cadre hexagonal (les dalles du village), aux couleurs de la classe. Le fond de l'hexagone est coupé en deux zones par une diagonale nette « / », d'un sommet à l'autre (haut droite à bas gauche) : moitié haut gauche claire, moitié bas droite sombre, deux teintes du thème. Même direction pour les cinq classes.
- `Competences/<classe>_<action>.svg`, `jauge_<ressource>.svg`, `commun_<action>.svg` (24) : glyphe seul sur fond transparent, sans cadre. Le HUD dessine déjà la case et le temps de recharge.
- **Mécanicien (classe à venir)** : `Classes/classe_mecanicien_a.svg` (clé et engrenage), `_b` (clé et marteau croisés) ; `classe_mecanicien.svg` est une copie de la variante retenue (constante `MECANICIEN_CHOIX`, « a » par défaut). Sans vert : laiton (ambre et ors de Critique), acier clair (Fer clair de Rage, Os) ; cadre laiton, fond acier (Fer / Fer sombre de Rage). Modèle pressenti : l'ingénieur KayKit, clé `engineer_Wrench`.
- **Druide (classe à venir)** : `Classes/classe_druide_a.svg` (bois de cerf), `_b` (bâton noueux et ambre), `_c` (lune et feuille d'automne) sont trois variantes à choisir ; `classe_druide.svg` est une copie de la variante retenue (constante `DRUIDE_CHOIX` du script, « a » pour l'instant). Sans vert : bois et terre (Terre), ambre (Critique), os (Os), ocres (Chasse) ; cadre en os, fond terre profonde / charbon. `Competences/druide_metamorphose.svg`, `druide_ronces.svg`, `druide_soin_nature.svg` sont **provisoires** : les compétences du druide ne sont pas décidées.
- `generer_icones.py` : génère tous les SVG et la planche de revue `Docs/icones/planche.html`.
- `Historique/v1/` : première version des icônes modifiées depuis (commit fb1b64a). La planche les montre dans le bloc « Modifiées », avant et après côte à côte (liste `MODIFIEES` du script).

## Conventions

- **Noms** : minuscules, sans accent, mots séparés par `_` ; préfixe = classe (`paladin_`, `mage_`, `rodeur_`, `assassin_`, `viking_`), `commun_` pour les actions de toutes les classes, `jauge_` pour les jauges de classe.
- **Format** : `viewBox="0 0 128 128"`, uniquement des `<polygon>` remplis en couleur pleine. Pas de trait, de dégradé, de transparence, de filtre, de masque, de texte ni d'image : le module Vector Graphics de Unity 6 (UI Toolkit) les importe tels quels.
- **Style** : gemmes low poly des effets. Facettes à bords nets, une seule lumière venue du haut à gauche, 2 à 5 teintes par objet. Chaque forme est posée sur un fond de sa teinte la plus sombre, ce qui évite les liserés entre facettes.
- **Couleurs** : lues à chaque génération dans `Assets/VFX/_Palettes/*.asset` (table de secours dans le script). Une icône prend le thème de son effet : Sacré pour le paladin, Feu pour le mage, Chasse (ocres seulement) pour le rôdeur, Ombre pour l'assassin, Rage pour le viking, Critique pour le coup critique. **Soin en blanc chaud et or** (décision de Quentin, 25/09/2026 : lumière sacrée), rampe `SOIN` du script tirée de Sacré et du blanc chaud de Critique ; la palette Soin du jeu (menthe) est interdite dans les icônes. Matières non magiques : bois = Terre, fer = accents Fer de Rage, plumes = ocres de Chasse. **Le vert Nyxessa est interdit** : le script s'arrête si une icône utilise une teinte de Nyxessa, les verts de Chasse, l'accent Magie des Os ou la menthe de Soin. Exceptions : la jauge de mana reprend le bleu de la jauge du HUD (palette BouclierPlein), et l'esquive, sans thème, prend l'ivoire des Os.
- **Taille** : dessinées pour se lire à 40 px (HUD) comme à 128 px. Dans le HUD, l'icône occupe la case moins une marge de 12 px (`.hud-emplacement__icone`) : 72 px dans une case de 96.

## Régénérer

```
python ArtSources/Icones/generer_icones.py
```

Python 3, bibliothèque standard seulement. Le script réécrit tous les SVG et la planche, signale une teinte de palette qui a changé, une icône qui déborde du cadre 128 × 128 et les SVG orphelins, sans rien supprimer. Pour modifier une icône, on édite sa fonction (même nom que le fichier) puis on relance ; les rampes de couleurs sont en tête du script. Toute retouche à la main dans un SVG est perdue à la génération suivante.

## Liste

| Fichier | Icône | Bouton |
|---|---|---|
| `paladin_epee` | Frappe à l'épée | RT |
| `paladin_garde` | Garde et parade | LT |
| `paladin_charge_belier` | Charge bélier | LB |
| `paladin_soin` | Soin sur soi | RB |
| `mage_boule_de_feu` | Boule de feu | RT |
| `mage_cone_de_flammes` | Cône de flammes | LT |
| `mage_brulure` | Brûlure (état sur les ennemis) | aucun |
| `jauge_mana` | Mana | jauge |
| `rodeur_tir` | Bander et tirer | RT |
| `rodeur_visee` | Viser (ajoutée) | LT |
| `rodeur_nuee_de_fleches` | Nuée de flèches | LB |
| `rodeur_roulade_salve` | Roulade arrière et salve | RB |
| `assassin_dague` | Dague | RT |
| `assassin_arbalete` | Arbalète | LT |
| `assassin_fumigene` | Grenade fumigène | LB |
| `assassin_furtif` | Mode furtif (indicateur) | aucun |
| `viking_hache` | Hache | RT |
| `viking_attaque_tournante` | Attaque tournante | LT |
| `viking_rugissement` | Rugissement | LB |
| `viking_saut_percutant` | Saut percutant | RB |
| `jauge_rage` | Rage | jauge |
| `commun_esquive` | Esquive, roulade | B |
| `commun_potion_soin` | Potion de soin | croix haut |
| `commun_coup_critique` | Coup critique (retour visuel) | aucun |
