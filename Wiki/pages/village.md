# Le village

Le village est une clairière entourée de forêt. Nyxessa est au centre, les maisons l'entourent, et les ennemis arrivent par la forêt.

## Disposition {décidé}

| Élément | Règle |
|---|---|
| Nyxessa | Au centre, sur un plateau de pierre à trois marches. Un rayon de 8 m reste dégagé autour d'elle. |
| Maisons | Six maisons en couronne à environ 18,5 m du centre, façade tournée vers Nyxessa, reliées à la place par des allées pavées. |
| Portail vers le donjon | Tout près de Nyxessa, à environ 11 m, sur un petit socle de pierre, face à la relique. |
| Prairie | Herbe, buissons et rochers entre les maisons et la forêt. |
| Forêt | Ouverte et praticable : on peut marcher entre les troncs partout. |
| Apparition des ennemis | Trois clairières dans la forêt, au nord, au sud-est et au sud-ouest, à environ 70 m du centre. |

Aucune maison n'est posée dans l'axe d'une clairière d'apparition : les ennemis ont toujours un passage dégagé vers le centre.

## Retiré {décidé} {dev}

Ces éléments ont été essayés puis retirés du village le 25/09/2026 : l'enceinte et ses portes, la scierie, la mine, le marchand, les tours, les garnisons, le poste de construction, les couloirs fermés, les sentiers de terre dans la forêt.

## Sentiers vers la forêt {décidé}

Un **sentier de pierre** part du village vers **chacune des trois zones** d'où sortent les squelettes. Entretenu à la sortie du village, il se **dégrade** à mesure qu'on s'enfonce dans la forêt : dalles espacées, cassées, envahies d'herbe, puis quelques pierres éparses. Une ou deux **lanternes**, au sol ou sur poteau, jalonnent chaque sentier.

## Taille des maisons {décidé} {dev}

Les portes des maisons sont à l'échelle du joueur : 2,1 m pour un personnage de 2 m. Les maisons font donc 6 à 6,5 m de large.

## Villageois

- **Sorcier** {décidé} : un villageois sorcier invoque le bouclier de Nyxessa. **Le jour**, il reste dans **sa maison** ; **la nuit**, il se tient près de Nyx et la protège. Pour le moment, on ne lui parle pas. Voir [Nyxessa](nyxessa.md). Il porte un **bâton à cornes dorées**, dont le **cristal est vert**, couleur de Nyxessa {décidé}.
- **Druide** {décidé} : il vend les potions de soin le jour. {{dev: Modèle : le druide du pack KayKit Adventurers 2.0 EXTRA (`Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Druid.fbx`, bâton `druid_staff`).}}
- **Mécano** {décidé} : le vendeur traditionnel. Il tiendra plus tard la **boutique** où l'on achète des **armes et des améliorations**. {{dev: Modèle : l'ingénieur (`Engineer.fbx`, clé `engineer_Wrench`) du pack KayKit Adventurers 2.0 EXTRA. Pas dans la version 0.1.}}
- **Forgeron** {décidé} : il améliore l'arme de chaque héros ; le mécano garde la vente des armes neuves. Modèle : le **barbare, sans son chapeau d'ours ni son écharpe** {décidé}. Il **forge dans sa forge** avec un marteau KayKit, jour et nuit : coups réguliers sur l'enclume, étincelles {décidé}. {{dev: Barbare du pack KayKit Adventurers 2.0 FREE (`Barbarian.fbx`, présent dans Relic) ; le chapeau est une pièce séparée, `Barbarian_BearHat`, à masquer.}}
  - **L'enclume** est à sa taille : plus petite qu'avant, sur un billot bas, sa table à hauteur de la main du forgeron, devant lui. Le marteau **frappe la table de l'enclume sans y entrer**, ni pendant le geste ni au repos (entre deux coups, le marteau reste posé sur la table) {décidé}. {{dev: Enclume KayKit `anvil` à l'échelle 0,42 (0,7 × l'ancienne), billot de 10 cm, table à 0,44 m du plancher (`InterieursBuilder.EnclumeEchelle`). `ForgeronBuilder` mesure l'instant du contact (tête du marteau à la hauteur de la table) et place le forgeron pour qu'aucun sommet du marteau ni du corps n'entre dans l'enclume sur tout le geste ; mesure en jeu : 0,0 mm de pénétration. Captures `Assets/Screenshots/forge_contact_*.png`.}}
  - **Le feu de la forge est vivant** : flammes en gemmes qui dansent, étincelles, braises qui palpitent, lumière qui vacille ; couleurs du feu, jamais de vert {décidé}. {{dev: `ForgeFeu` (palette Feu, 54 gemmes au plus, lumière `Feu_Forge` ± 10 %), visuel seulement, chaque poste le joue pour lui. Fiche dans `Docs/vfx.md`.}}
  - **Sons** : à chaque coup, **un des 3 sons de marteau sur l'enclume**, tiré au hasard, et rien d'autre {décidé}. {{dev: Son `forge_enclume` du catalogue (voir [Sons](sons.md)), synthétisé pour Deathless : `Assets/Audio/Forge/enclume_1..3.wav`, générés par `Assets/Audio/Forge/synth_enclume.py`.}}
- Pas d'autre villageois {décidé}.

## Potions de soin {décidé}

- Les héros **achètent des potions de soin en or, le jour**, au village.
- Chacun en porte **3 au maximum**. On boit avec la croix directionnelle haut, ou la touche 1 au clavier (voir [Commandes](commandes.md)).
- Prix et soin rendu : {à équilibrer}.
- **Le druide** les vend {décidé}. Voir Villageois.

## Intérieurs {décidé}

Les maisons des villageois ont un **intérieur** où l'on entre par la porte : la boutique du druide (fioles, herbes, chaudron), celle du mécano (établi, engrenages, armes exposées), la forge du forgeron (enclume, braises ; le forgeron y bat le fer, jour et nuit), la maison du sorcier (pupitre et carte du village, éclat de Nyx, croquis de la relique, grimoires ; on le voit à sa place le jour) et la taverne (comptoir, tonneaux, tables, tavernier). Les portes restent ouvertes, le battant presque contre le mur, sur de beaux gonds. Les squelettes n'entrent pas {décidé}.

## Rôle des maisons {décidé}

Le druide, le mécano et le forgeron ont **chacun leur maison**, qui leur sert de boutique : on y entre le jour pour acheter. Le sorcier a aussi sa maison, où il passe la journée ; il ne vend rien. Une maison est la **taverne** (ci-dessous). La dernière reste du décor.

## Taverne {décidé}

Une des maisons (celle du nord-est de la place) est la **taverne** : comptoir, tonneaux en perce, tables et tabourets, âtre, et le **tavernier** derrière son comptoir. **De jour uniquement**, au comptoir, la touche **Interagir** (E, X, Carré) ouvre son menu ; l'or est pris dans la **caisse commune** :

- **Se restaurer** : un bol de ragoût, un peu de vie (+40 points de vie pour 15 or) ;
- **Boire une bière** : la tête tourne quelques secondes (8 s, 5 or) ;
- **Payer une tournée** : **tous les joueurs** sont ivres quelques secondes (15 s, 30 or).

**Ivresse** {décidé} (un [statut](statuts.md), affiché dans le HUD) : la caméra tangue doucement et la démarche hésite, sans rien de handicapant pour le combat (la visée, les attaques et les compétences ne changent pas). Prix, soin et durées : {à équilibrer}. Des **breuvages** viendront plus tard {à confirmer}. En multijoueur, l'hôte décide des achats. {{dev: (`Taverne`, `Ivresse`, `Partie.PayerTaverne` ; intérieur par `InterieursBuilder`, tavernier par `TavernierBuilder`)}}
