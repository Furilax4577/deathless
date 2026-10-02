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

## Refonte de la carte {décidé}

**Plan cible** {décidé, 28/09/2026} : le plan n° 3 de la page [Direction artistique](direction-artistique.md) (Grok, d'après nos décisions). Il reprend tout ce qui suit et y ajoute un **anneau pavé** autour du plateau d'où partent les allées, et un village plus ramassé. **L'intérieur dicte l'emprise** : chaque bâtiment est d'abord dimensionné par son plan de circulation intérieur, à l'échelle des personnages (2,3 m) et de la caméra ; l'extérieur en découle. Faits le 28/09/2026 : la taverne, 16 × 11 m (`Docs/da/taverne-plan.md`) ; le mécano 12 × 9 m, la forge 11 × 9 m avec un appentis, le sorcier 11 × 9 m avec sa tour, le druide 11 × 8 m, la maison de base 8 × 6,5 m avec puits et jardin (`Docs/da/maisons-plans.md`). La couronne du plan cible est à recaler sur ces emprises.

Chantier ouvert par Quentin le 27/09/2026. **Plan validé sur la maquette grise le 27/09/2026** (`sandbox-level`, scène `VillageV5`, générateur `VillageV5Builder`, captures `v5_*.png`) : maisons sur une couronne à **r = 30 m** (l'axe nord ±20° est réservé à la vue plateau → cascade, les trois couloirs de vagues ±25° restent libres), cascade **plein nord**, falaise en trois gradins (18, 28, 38 m) à r = 34 m, pierrier r 26-34 m, rivière de 4,5 m, ponts de 3 m, gué de 4,5 m ; place du portail à r = 18 m sur l'azimut 60° (rive est, pont nord) ; le pierrier épargne l'emprise des maisons et le terrain est aplani sous chacune. Habillage complet fait dans le bac à sable le 27/09/2026 ; prochaine étape : le report dans `main` (0.6.0).

- **Moins de forêt** : elle reste au sud et à l'ouest, en deux masses qui gardent les couloirs des vagues ; l'est devient une lande (prairie, rochers, souches, arbres isolés).
- **Une montagne au nord** {décidé} : formation rocheuse dont le premier pic forme une **crête infranchissable** (la géométrie et le NavMesh suffisent, pas de mur invisible), avec un **pierrier** entre la falaise et les maisons. Fond de tableau du menu principal et des plans vers Nyxessa ; les vagues n'arrivent plus que par l'est, le sud et l'ouest.
- **Trois sentiers d'attaque lisibles** {décidé, 28/09/2026} : les vagues arrivent par l'est, le sud et l'ouest sur trois sentiers de terre battue de 7 m, de chaque clairière à l'anneau pavé ; aucun bâtiment, arbre ni rocher dessus. L'est et le sud franchissent la rivière par un pont (ou le gué), l'ouest arrive par la terre ferme. **La route de la grotte** (4 m, pavée, lanternes) va du plateau à la grotte sans rien sur son passage.
- **Trois passages en eau basse** {décidé, 28/09/2026} : les trois maisons de la rive est ne dépendent plus du seul pont. Un gué devant le druide, un devant la forge, un devant le mécano (galets plats, eau claire, ralenti comme l'eau du donjon), en plus des deux ponts. Les squelettes les empruntent aussi.
- **Une petite cascade** sort de la crête : source visuelle et sonore du village.
- **La grotte du portail** {décidé, 28/09/2026} : le portail du donjon quitte sa place au village pour une **grotte au pied de la falaise, à gauche de la cascade** (nord-ouest), d'où sort une **lueur verte** (énergie de Nyxessa). Grotte peu profonde et large (environ 8 m de large, 6 m de profondeur, 5 m de haut), portail visible du dehors, allée pavée depuis le village ; la nuit le portail est fermé et la grotte s'éteint (une braise verte au fond), Nyxessa reste la lumière principale ; les squelettes n'y entrent pas ; le retour du donjon fait sortir les joueurs devant l'entrée. À l'aube, la charge de Nyxessa vole jusqu'à la grotte.
- **La rivière traverse le village** : elle longe le plateau de Nyxessa (à ~10 m) et sort au sud-ouest ; **deux ponts** (nord près du portail, sud) qui font goulets pour les vagues, et **un gué** (eau peu profonde, ralenti comme l'eau du donjon) pour que l'IA ne soit jamais bloquée ; trois maisons par rive ; la nuit, l'eau reflète la lueur de Nyxessa. **Elle longe le plateau** (à ~10 m, entre Nyxessa et trois maisons) {décidé, 27/09/2026}. **Deux ponts et trois gués** {décidé, 28/09/2026 ; un seul gué le 27/09/2026} : pont est, pont sud, et un gué devant chacune des trois maisons de la rive est.
- **Les flancs sont les modèles de Quentin, tels quels** {décidé, 02/10/2026} : les deux pièces Tripo de flanc (mur de colonnes à deux grands pics à l'est, monticule en pente à gros rocher en surplomb à l'ouest) sont posées **entières, sans miroir, sans recadrage, sans étirement** ; seule leur taille est choisie (voir la note de développement).
- **Aucun arbre, souche ni buisson à moins de 3 m d'une paroi rocheuse** {décidé, 02/10/2026} (pièces Tripo, falaise, rochers de falaise) ; les buissons larges, 4 m.
- **L'escalier de la grotte est droit** {décidé, 02/10/2026} : tiré sur l'axe de l'entrée, avec la route pavée (dernier tronçon droit), marches régulières, parapets symétriques.
- **Portail agrandi, sans stèle** {décidé, 02/10/2026} : le portail vert est 1,6 fois plus grand (rayon 2,56 m) et remplit l'entrée de la grotte, visible depuis la route ; la stèle (socle rond) est supprimée ; la dalle repose sur un socle maçonné (plus d'angle en l'air).
- **Herbes de la carte v5 réduites de moitié** {décidé, 02/10/2026} (hauteur et largeur).
- **Plus de rochers posés sur la falaise** {décidé, 02/10/2026} : les blocs gris qui flottaient ou ressemblaient à des cubes sont retirés ; plus aucun rocher qui ne touche pas le sol rocheux.
- **Brume de montagne** {décidé, 02/10/2026} : brume basse et douce sur les gradins, les flancs et les crêtes, à partir du haut de la cascade (14 m) ; le bas de la falaise reste net, la brume s'épaissit vers le haut ; blanc bleuté de jour, violet-gris de nuit, jamais verte.
- **Cascade avec rebord et écume** {décidé, 02/10/2026} : l'eau jaillit d'un rebord de roche arrondi qui avance de 1,45 m en surplomb, la nappe épouse la paroi (aucun vide) et ruisselle sur les éboulis jusqu'au bassin ; l'écume est fine (nombreuses petites gemmes claires), sur les deux bords de la nappe, sur les éboulis et au point d'impact dans le bassin.
- Maquette : `Docs/references/` et le fil de discussion ; générateur du village en v5 dans `sandbox-level`, circulation revérifiée (72 azimuts, remontée aux ponts).

{{dev: **Reportée dans main le 02/10/2026** (plan serpentin `Docs/outils/plan_village.py --serpente`, comparaison `Assets/Screenshots/carte_v5_aerien.png`) : générateur `Assets/Editor/VillageV5Builder.cs`, menu Deathless > Village > v5 (étapes 1 à 7, Tout appliquer, Vérifier).
- **Aperçu de la nouvelle carte (02/10/2026)** : la v5 vit dans sa propre scène `Assets/Scenes/CarteV5.unity` (sol, palette et NavMesh à elle) ; `Village.unity` reste l'ancienne carte du solo et du multijoueur. Le menu principal offre « Nouvelle carte (aperçu) » : héros solo (dernière classe), jour figé à midi, ni vagues, ni nuit, ni défaite ; Pause > Quitter ramène au menu sur l'ancienne carte.
- Maisons aux cotes du plan, une racine par bâtiment `Maisons/Batiment_<Rôle>` (pivot au sol au centre de l'emprise, avant vers Nyxessa, zone `Porte` séparée du modèle) ; intérieurs et lanternes déplacés avec elles.
- Montagne : pièce héros Tripo (`ArtSources/Decor/Montagne/montagne_pipeline.py`, 122 × 40 × 20,5 m) et falaise procédurale en gradins (18, 28, 38 m) derrière et sur les flancs, jamais marchable ; une seule grotte. **Flancs Tripo, intégration fidèle le 02/10/2026** (retour de Quentin : « ce ne sont pas les modèles que je t'ai donnés » ; la première intégration, du matin, les avait retournés en miroir, recadrés et enfouis) : les deux modèles entiers, échelle **uniforme de 62 m par unité Tripo** (`montagne_flancs_pipeline.py --echelle`, 28 000 triangles rendus et 2 800 de collision chacun, atlas 2048²) ; l'ouest mesure 60,7 × 28,7 m et 22,0 m de haut (point haut à l'ouest, rocher en surplomb), l'est 60,8 × 18,7 m et 19,6 m de haut (deux pics à l'est) ; pose `V5FlancsPose` (est (69,5 ; 43), ouest (-69,5 ; 40), lacet 180°, enfoncées de 0,4 m) : emprises x de 39 à 100 m et de -100 à -39 m, donc **dans la limite du sol (±100 m)**, avec un recouvrement de 20 m avec la pièce héros (qui finit à x = ±62 à 6-8 m de haut) ; blocs de 2,7 m environ (5,6 m sur la pièce héros : un compromis entre la taille des blocs et l'emprise). Jamais marchables (collision, NavMesh).
- Grotte : replat du fond à 2,15 m ; **escalier droit** de 10 marches régulières (21,5 × 46 cm, parapets de 0,4 m, largeur 4,6 m) sur l'axe de l'entrée, dalle sur socle maçonné, route pavée dont les 10,9 derniers mètres sont droits ; portail de 2,56 m de rayon (centre à 4,81 m, interaction à 3 m : `GameBalance.distancePortail`, écart de hauteur 2,6 m sur 3 permis), sans stèle ; aller-retour au donjon vérifié en jeu (invite « Entrer dans le donjon » à 2,8 m).
- Rivière (4,5 m), bassin, **cascade refaite le 02/10/2026** (`CascadeVillage` : rebord `Levre_Roche` posé sur le replat naturel de la ravine à 13,6 m ; voile de 7 × 72 sommets plaqué sur la roche par lancers de rayons, 25,6 m de chemin du rebord au bassin ; écume fine sur les bords, les éboulis et au point d'impact ; le shader `EauLowPoly` retourne désormais la normale vers l'œil sur les facettes verticales), **brume de montagne** (`BrumeMontagne` : 130 nappes en trois bandes d'altitude de 14 à 39 m, 3 appels de dessin, positions relevées sur la roche par `V5Brume`),  trois gués (pierres plates, zone « Eau » du NavMesh, ralenti `ZoneEau`), deux ponts de bois (est, sud) ; l'eau est infranchissable ailleurs : lit non praticable pour le NavMesh, héros ramené à la berge (`RiviereVillage`), sans mur invisible. Courant visible (shader `EauRiviere`), écume aux gués et aux piles (`EcumeRiviere`).
- Routes d'attaque de 7 m et clairières est, sud, ouest ; forêt au sud et à l'ouest, lande à l'est, pierrier ; sol à facettes d'1 m.
- Vérifié : chaque clairière rejoint Nyxessa en 18,7 à 18,8 s (sbire, 3,4 m/s), l'est et le sud par leur pont ; aucun chemin dans l'eau hors gué ou pont ; deux nuits jouées en Play sans squelette bloqué ni hors passage ; aucune zone du NavMesh au-delà de la falaise.}}

## Taille des maisons {décidé} {dev}

Les portes des maisons sont à l'échelle du joueur : 2,1 m pour un personnage de 2 m. Les maisons font donc 6 à 6,5 m de large.

Maisons générées (cap « sortir de KayKit », retours de Quentin du 27/09/2026 sur la maison du guide de style) {décidé} :

- **Porte** de 1,8 × 2,6 m au clair (un personnage passe sans toucher les bords).
- **Fondations basses, à la KayKit** : pas d'assise de pierres maçonnées mais une **dalle de pierre basse et lisse** qui déborde du mur, blocs d'angle et perron de deux ou trois marches devant la porte, comme `building_home_B` (retour de Quentin, 27/09/2026).
- **Une fenêtre par façade** : porte et une fenêtre devant, une derrière, une par pignon (plus la petite fenêtre de pignon sous le faîte). Lanterne au-dessus du niveau des claveaux, entre la porte et la fenêtre.
- **Plus grandes et plus larges de façade** : on doit pouvoir y entrer à plusieurs sans être à l'étroit, la taverne surtout. Façade à porte et deux fenêtres, plus une fenêtre de pignon.
- **Des règles communes, une personnalisation par habitant** : même grammaire (proportions, socle, toit, porte, atlas) pour toutes ; chaque maison prend deux ou trois éléments distinctifs selon celui qui l'habite et son activité (taverne, forgeron, mécano, druide, sorcier, maison de décor), et ses propres teintes d'enduit, de volets et de toit. Propositions par maison : dans [À faire](a-faire.md).

## Villageois

- **Sorcier** {décidé} : un villageois sorcier invoque le bouclier de Nyxessa. **Le jour**, il reste dans **sa maison** ; **la nuit**, il se tient près de Nyx et la protège. Pour le moment, on ne lui parle pas. Voir [Nyxessa](nyxessa.md). Il porte un **bâton à cornes dorées**, dont le **cristal est vert**, couleur de Nyxessa {décidé}.
- **Druide** {décidé} : il vend les potions de soin le jour. {{dev: Pas encore dans le jeu.}} {{dev: Modèle : le druide du pack KayKit Adventurers 2.0 EXTRA (`Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Druid.fbx`, bâton `druid_staff`).}}
- **Mécano** {décidé} : le vendeur traditionnel. Il tiendra plus tard la **boutique** où l'on achète des **armes et des améliorations**. {{dev: Modèle : l'ingénieur (`Engineer.fbx`, clé `engineer_Wrench`) du pack KayKit Adventurers 2.0 EXTRA. Pas dans la version 0.1.}}
- **Forgeron** {décidé} : il améliore l'arme de chaque héros ; le mécano garde la vente des armes neuves. {{dev: Amélioration de l'arme : pas encore dans le jeu (seuls le forgeron et sa forge sont en place, en décor).}} Modèle : le **barbare, sans son chapeau d'ours ni son écharpe** {décidé}. Il **forge dans sa forge** avec un marteau KayKit, jour et nuit : coups réguliers sur l'enclume, étincelles {décidé}. {{dev: Barbare du pack KayKit Adventurers 2.0 FREE (`Barbarian.fbx`, présent dans Relic) ; le chapeau est une pièce séparée, `Barbarian_BearHat`, à masquer.}}
  - **L'enclume** est à sa taille : plus petite qu'avant, sur un billot bas, sa table à hauteur de la main du forgeron, devant lui. Le marteau **frappe la table de l'enclume sans y entrer**, ni pendant le geste ni au repos (entre deux coups, le marteau reste posé sur la table) {décidé}. {{dev: Enclume KayKit `anvil` à l'échelle 0,42 (0,7 × l'ancienne), billot de 10 cm, table à 0,44 m du plancher (`InterieursBuilder.EnclumeEchelle`). `ForgeronBuilder` mesure l'instant du contact (tête du marteau à la hauteur de la table) et place le forgeron pour qu'aucun sommet du marteau ni du corps n'entre dans l'enclume sur tout le geste ; mesure en jeu : 0,0 mm de pénétration. Captures `Assets/Screenshots/forge_contact_*.png`.}}
  - **Le feu de la forge est vivant** : flammes en gemmes qui dansent, étincelles, braises qui palpitent, lumière qui vacille ; couleurs du feu, jamais de vert {décidé}. {{dev: `ForgeFeu` (palette Feu, 54 gemmes au plus, lumière `Feu_Forge` ± 10 %), visuel seulement, chaque poste le joue pour lui. Fiche dans `Docs/vfx.md`.}}
  - **Sons** : à chaque coup, **un des 3 sons de marteau sur l'enclume**, tiré au hasard, et rien d'autre {décidé}. {{dev: Son `forge_enclume` du catalogue (voir [Sons](sons.md)), synthétisé pour Deathless : `Assets/Audio/Forge/enclume_1..3.wav`, générés par `Assets/Audio/Forge/synth_enclume.py`.}}
- Pas d'autre villageois {décidé}.

## Potions de soin {décidé}

- Les héros **achètent des potions de soin en or, le jour**, au village.
- Chacun en porte **3 au maximum**. On boit avec la croix directionnelle haut, ou la touche 1 au clavier (voir [Commandes](commandes.md)).
- Prix et soin rendu : {à équilibrer}.
- **Le druide** les vend {décidé}. Voir Villageois.
- {dev} Pas encore dans le jeu : ni achat ni boisson de potion.

## Intérieurs {décidé}

Les maisons des villageois ont un **intérieur** où l'on entre par la porte : la boutique du druide (fioles, herbes, chaudron), celle du mécano (établi, engrenages, armes exposées), la forge du forgeron (enclume, braises ; le forgeron y bat le fer, jour et nuit), la maison du sorcier (pupitre et carte du village, éclat de Nyx, croquis de la relique, grimoires ; on le voit à sa place le jour) et la taverne (comptoir, tonneaux, tables, tavernier). Les portes restent ouvertes, le battant presque contre le mur, sur de beaux gonds. Les squelettes n'entrent pas {décidé}.

## Rôle des maisons {décidé}

Le druide, le mécano et le forgeron ont **chacun leur maison**, qui leur sert de boutique : on y entre le jour pour acheter. Le sorcier a aussi sa maison, où il passe la journée ; il ne vend rien. Une maison est la **taverne** (ci-dessous). La dernière reste du décor.

## Taverne {décidé}

**Refonte du 28/09/2026** {décidé} : la taverne est un lieu convivial, avec de la **musique festive**. La tavernière est **la Bavaroise** ; **le barde** joue sur une estrade ; **le clochard pétomane** est le client récurrent, sur son banc près de l'âtre ; et il y a la place pour les quatre joueurs. Salle de 16 × 11 m à double hauteur, comptoir de 6 m (quatre joueurs de front), galerie au-dessus du service, **décor seulement** (l'escalier est fermé par une corde) {décidé}. Plan chiffré : `Docs/da/taverne-plan.md`. {{dev: Les trois restent des candidats jouables ; à la taverne ce sont des villageois.}}

**Intérieurs en zones à part** {décidé, 01/10/2026} : on entre dans une maison comme dans le donjon : la porte s'ouvre un peu, bruit de porte, fondu au noir, et on arrive dans l'intérieur, une zone séparée du village. Les intérieurs peuvent être plus grands que la maison vue de dehors (cosy) ; dehors, les maisons restent simples pour garder le village lisible pendant les vagues.

Une des maisons (celle du nord-est de la place) est la **taverne** : comptoir, tonneaux en perce, tables et tabourets, âtre, et la **tavernière**, la Bavaroise, derrière son comptoir {décidé, 01/10/2026}. **De jour uniquement**, au comptoir, la touche **Interagir** (E, X, Carré) ouvre son menu ; l'or est pris dans la **caisse commune** :

- **Se restaurer** : un bol de ragoût, un peu de vie (+40 points de vie pour 15 or) ;
- **Boire une bière** : la tête tourne quelques secondes (8 s, 5 or) ;
- **Payer une tournée** : **tous les joueurs** sont ivres quelques secondes (15 s, 30 or).

**Ivresse** {décidé} (un [statut](statuts.md), affiché dans le HUD) : la caméra tangue doucement et la démarche hésite, sans rien de handicapant pour le combat (la visée, les attaques et les compétences ne changent pas). Prix, soin et durées : {à équilibrer}. Des **breuvages** viendront plus tard {à confirmer}. En multijoueur, l'hôte décide des achats. {{dev: (`Taverne`, `Ivresse`, `Partie.PayerTaverne` ; intérieur par `InterieursBuilder`, tavernier par `TavernierBuilder`)}}
