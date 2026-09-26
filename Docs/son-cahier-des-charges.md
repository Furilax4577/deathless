# Cahier des charges son et musique de Deathless

Rédigé le 26/09/2026 (session cloud, brief `Docs/briefs/cloud-son.md`). But : **régénérer tous les sons et toutes les musiques** avec une identité propre à Deathless, à la place du mélange actuel (Kenney, Relic, Sonniss, synthèses de passage). Ce document fixe la direction artistique, la palette de timbres, l'inventaire complet des sons nécessaires, les musiques, les contraintes techniques, la méthode de production et le plan en lots. Le lot 1 (Nyxessa et interface) est produit avec ce document pour prouver la méthode : § 8.

Sources lues : `Assets/Scripts/Jeu/Audio/SonsDuJeu.cs` (les événements que le code appelle), `AudioBank.cs`, `Assets/Scripts/Audio/VolumesAudio.cs`, le catalogue `Wiki/data/sons.json` (210 sons avant ce lot), les pages du wiki (classes, ennemis, Nyxessa, portail, déroulé, village, statuts, interface, effets), `Docs/sons.md` et `Docs/vfx.md`.

Tout ce qui suit est une **proposition** à valider par Quentin, sauf ce qui reprend une règle déjà écrite au wiki (le vert réservé à Nyxessa, le feu couleur feu, les flèches non magiques, les squelettes os et poussière).

---

## 1. Direction artistique sonore

**Le caractère.** Deathless sonne comme il se voit : **low poly lisse, chaleureux, lisible**. Les sons sont nets, courts, avec peu de couches. Chacun dit une seule chose, et on le reconnaît du premier coup, même à 60 squelettes. Les matières sont celles du décor : **bois** (maisons, interface, tavernes), **pierre** (plateau de Nyxessa, donjon), **os** (squelettes), **terre** (sorties de terre, sauts, charges), et, au-dessus de tout, le **cristal de Nyxessa**, qui tinte.

**Les gemmes tintent, et seulement elles.** Le tintement de gemme est la signature de Nyxessa : la relique, sa ceinture, ses missiles, le portail, la téléportation, la réapparition, le bouclier du sorcier et les éclats de Nyxar. C'est la règle du vert, transposée à l'oreille : on n'entend ce timbre nulle part ailleurs. Le feu du mage **crépite et gronde**, il ne tinte pas. Les critiques ont un **éclat d'or bref**, métallique et chaud, pas cristallin. Les squelettes sont **os et poussière** : cliquetis creux, mâchoires, sable qui retombe, jamais de voix humaine ni de métal lourd.

**L'humour est assumé, mais cadré.** Les classes comiques (Clochard pétomane, DJ Bob, Bavaroise, Barde) ont des sons franchement drôles : pets en plusieurs tailles, scratch, « bwoiing » du luth, chopes qui trinquent. L'humour vient du **timbre** et du **rythme**, pas du volume ni de la vulgarité, et il ne déborde pas sur le reste : un squelette qui meurt n'est pas drôle, il s'effondre en poussière.

**Ce qu'on évite.**
- Le **réalisme militaire** : pas de métal épais qui racle, pas de cris de douleur, pas d'éclaboussures de sang, pas d'armures lourdes.
- Les **nappes de cinéma** : pas de bruits de fond saturés, pas de « braaam », pas de chœurs épiques, pas de réverbération de cathédrale.
- Les **bips électroniques** et les sons de synthé datés dans l'interface : un menu fait « toc », pas « bip ».
- Les **clochettes enfantines** et les mélodies mièvres. Les gemmes chantent, mais dans une gamme sombre.

**La place du silence.** Le silence est un outil :
- avant une vague, les ambiances s'amincissent un instant, et le premier son de sortie de terre part sur un fond calme ;
- les longues préparations de Morgrim se lisent à l'oreille : un grondement qui monte, puis **un creux d'un quart de seconde**, puis l'impact ;
- la mort d'un joueur coupe presque tout pendant une demi-seconde autour de lui, puis son énergie file vers Nyxessa ;
- la musique de jour laisse des mesures vides : on doit entendre la forge, le vent et les oiseaux.

**Le jour et la nuit.**
- **Le jour est chaud et ouvert** : sol majeur, bois et cordes pincées, oiseaux, vent léger dans les feuilles, forge au loin. Le rythme est posé, on prépare.
- **La nuit est froide et resserrée** : mi mineur, bourdon grave, percussions de bois et d'os, grillons, souffle de brume. Le rythme monte par **couches** à mesure que les vagues arrivent (§ 4).
- **Le crépuscule et l'aube** (5 s chacun) sont des charnières sonores : un seul geste, lisible, qui accompagne le portail et l'énergie de Nyxessa.

**Une gamme commune.** Tout ce qui a une hauteur (gemmes, notes de l'interface, fanfares, musiques) est pris dans **mi mineur pentatonique** (mi, sol, la, si, ré), relatif de sol majeur pour le jour. Un tintement de Nyxessa ou un « toc » de menu ne sonne jamais faux par-dessus la musique.

**Épouser le langage visuel** (`Docs/vfx.md`). Les effets apparaissent et disparaissent **par la taille**, jamais par transparence : les sons aussi ont des **attaques franches** et des fins **nettes** (décroissance naturelle d'un mode, fondu court), jamais de longs fondus flous. Une gerbe de gemmes à l'écran, c'est une gerbe de tintements à l'oreille, avec le même nombre d'éléments à peu près et la même palette réduite. Une onde de gemmes au sol, c'est un souffle qui balaie.

---

## 2. Palette de timbres

Sept timbres récurrents font la signature. Chacun est décrit pour qu'un script le reproduise ; les briques correspondantes sont dans `Assets/Audio/Deathless/deathless_audio.py` (lot 1) ou à ajouter au fil des lots.

| Timbre | Où | Spectre | Enveloppe | Durée | Brique |
|---|---|---|---|---|---|
| **Gemme** (cristal de Nyxessa) | Nyxessa, portail, bouclier, téléportation, réapparition, éclats de Nyxar | Barre de cristal libre : partiels aux rapports **1 : 2,756 : 5,404 : 8,933**, amplitudes 1 / 0,42 / 0,2 / 0,09 ; fondamental de 330 Hz (mi4, gemme lourde) à 2,6 kHz (mi7, étincelle), sur la gamme commune ; chaque partiel doublé d'un **jumeau désaccordé de 0,6 à 3 Hz** (le frisson de la gemme) | Attaque 0,4 ms avec un petit choc de verre (bruit filtré autour de 3 × f0, 1,2 ms) ; décroissance exponentielle, T60 du fondamental de 0,3 à 2,6 s, les partiels aigus 2 à 6 fois plus brefs | 0,3 à 2,6 s | `gemme`, `scintillement` |
| **Bois sec** (interface, maisons) | Menus, votes, décompte, portes, tables, tonneaux | Lame de marimba accordée **1 : 3,93 : 9,24** (amplitudes 1 / 0,3 / 0,1) sur la gamme commune (mi5 à ré7) ; clic de bois : bruit en bande 2,6 à 4,2 kHz, 0,9 ms | Attaque 0,6 ms ; T60 de **25 à 90 ms** pour un « tic » ou un « toc », de **0,3 à 0,7 s** pour une note qui chante | 0,07 à 1 s | `bois`, `tambour_bois` |
| **Os creux** (squelettes) | Pas, coups reçus, mort, sortie de terre, préparation | Tube court fermé : modes impairs autour de **450 à 900 Hz** (rapports 1 : 3 : 5 légèrement désaccordés), plus un cliquetis de 3 à 8 petits chocs (1,5 à 4 kHz, 1 ms chacun) espacés de 8 à 25 ms | Attaque 0,5 ms ; T60 **40 à 80 ms** (sec, sans résonance) ; le cliquetis retombe en grappe irrégulière | 0,1 à 0,6 s | à créer (lot 3) |
| **Poussière et terre** | Mort et aube des squelettes, sortie de terre, saut percutant, chute de Morgrim | Bruit brun (bruit blanc intégré) passé en bas à 300-600 Hz pour la masse, grains de gravier (bruit en bande 1 à 3 kHz, impulsions aléatoires de 2 ms à densité décroissante) | Coup sourd de 5 à 15 ms, puis ruissellement qui s'éclaircit et s'éteint en 0,3 à 1,2 s | 0,3 à 1,5 s | `sub`, `gravier`, `pas_pierre` (lot 2) ; bruit brun à ajouter (lot 3) |
| **Souffle** (air, énergie, mouvement) | Charge et retour de l'énergie, élans d'armes, esquives, traînée des flèches, onde de choc | Bruit blanc dans un **passe-bande** (Q 1 à 1,6) dont la fréquence centrale **balaie** : montée 300 Hz → 4-7 kHz pour ce qui part, descente pour ce qui revient ; élan d'arme : 600 Hz → 2,5 kHz en 80 à 150 ms | Attaque de 5 ms (élan) à 1,2 s (charge) ; forme de décroissance en puissance 1,2 à 2 | 0,08 à 2 s | `souffle` |
| **Feu** | Mage, brûlure, forge, torches, pièges | Grondement : bruit brun passé en bas à 250-500 Hz, modulé lentement (2 à 6 Hz) ; crépitements : impulsions de 0,5 à 2 ms en bande 2-6 kHz, densité 8 à 40 par seconde selon l'intensité | Allumage en 30 à 80 ms, maintien en boucle, extinction en 0,2 à 0,5 s | 0,3 s à boucle | à créer (lot 7) |
| **Éclat d'or** (critiques, or, sacré) | Coups critiques, pièces, charge bélier dorée, soin | Métal fin et clair : 3 à 5 partiels inharmoniques de **2,5 à 7 kHz** (plaque mince, rapports 1 : 1,59 : 2,14 : 2,65), plus un corps de coup (« toc » grave de l'impact) ; pièces : grappe de 4 à 20 tintements de 3 à 9 kHz, T60 de 60 à 200 ms | Attaque 0,3 ms, T60 **0,15 à 0,5 s**, jamais plus : l'or brille un instant | 0,15 à 0,6 s | `plaque` (lot 2, fer de Morgrim sur le bouclier) ; pièces à ajouter (lots 5 et 11) |

**Poussée grave** (complément transversal) : sinus de 35 à 120 Hz qui glisse vers le bas en 0,15 à 1,7 s. Elle donne la masse (Nyxessa qui pulse, impacts lourds, boss) sans remplir le médium. Brique `sub`. On la dose avec prudence, car beaucoup d'enceintes de télévision ne la rendent pas : le son doit rester lisible sans elle.

---

## 3. Inventaire des sons nécessaires

Légende :
- **Identifiant** : nom de fichier sans le numéro de variante (`famille_evenement_N.wav`, § 5) ; l'id du catalogue est le même, préfixé de `dl_` tant que l'ancien son est encore branché. Entre parenthèses, la constante de `SonsDuJeu.cs` qui l'appellera, ou *nouveau* si le code ne l'appelle pas encore.
- **Var.** : nombre de variantes tirées au hasard (`AudioBank` ajoute déjà ± 5 % de hauteur).
- **Espace** : **2D** (joué pour le joueur seul, non spatialisé) ou **3D** (au point de l'événement).
- **Portée** : distance au-delà de laquelle on n'entend plus rien (atténuation linéaire depuis 3 m, comme `AudioBank`) : **C** = 20 m, **M** = 40 m, **L** = 60 m. Aujourd'hui toutes les sources 3D sont à 60 m et toutes les boucles à 45 m : adapter la portée par son est une petite évolution de `AudioBank` (§ 7, décision 5).
- **Prio.** : **V** vital (sans lui, le jeu se lit mal), **C** confort (le jeu se lit, mais il manque quelque chose), **B** bonus.
- **Durée** : durée cible du fichier ; « boucle » = boucle sans raccord, de la durée indiquée.

### 3.1 Interface (groupe Interface, 2D, bois sec)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `interface_survol` (ReglagesAudio.survol) | La sélection passe sur un bouton | 70 ms | 2 | 2D | — | V | Tic de bois aigu (si6, ré7), T60 35 ms, clic léger ; niveau le plus bas de l'interface |
| `interface_clic` (ReglagesAudio.clic) | Valider (A, Croix, Entrée, clic) | 160 ms | 2 | 2D | — | V | Toc de bois clair (la5, si5), T60 90 ms, clic net, un peu de corps grave (240 Hz, 35 ms) |
| `interface_retour` (ReglagesAudio.retour, PretAnnule en repli) | Revenir (B, Rond, Échap) | 220 ms | 1 | 2D | — | V | Deux tocs qui descendent (la5 → mi5), 65 ms d'écart |
| `interface_refus` (AchatRefuse, ReglagesAudio.refus) | Action impossible, or insuffisant | 300 ms | 1 | 2D | — | V | Double toc sourd (mi4 + fa4, un demi-ton de trop : « non »), clarté réduite, souffle étouffé |
| `interface_confirmation` (PointDepense, repli de PalierAchete) | Action acceptée | 600 ms | 1 | 2D | — | V | Toc, puis quinte de marimba qui monte (mi5 → si5), T60 0,45 s |
| `interface_decompte` (repli d'AlerteNuit) | Chaque seconde d'un compte à rebours | 250 ms | 1 | 2D | — | V | Bloc de bois net (la5 et un second mode non accordé × 2,31) |
| `interface_decompte_fin` | Dernière seconde, « c'est parti » | 0,5 s | 1 | 2D | — | C | Même bloc une octave plus bas, doublé d'une note de marimba mi5 tenue |
| `interface_onglet` | Onglet précédent / suivant (LB / RB, Q / E) | 130 ms | 1 | 2D | — | C | Frottement de papier bref (bande 2,5 → 5 kHz, 60 ms) et tic de bois aigu |
| `interface_curseur` | Un curseur de réglage bouge d'un cran | 40 ms | 1 | 2D | — | C | Micro-tic de bois dont la hauteur suit la valeur (mi5 à mi7, rejouée avec une hauteur donnée par le code) |
| `interface_case` | Case à cocher, interrupteur | 120 ms | 2 | 2D | — | C | Activée : toc qui monte (mi5 → sol5) ; désactivée : toc qui descend |
| `interface_menu_ouvre` | Ouverture de la pause, du menu du personnage, de la roue | 250 ms | 1 | 2D | — | C | Froissement de tissu court (bande 800 Hz → 2 kHz) et toc grave de bois (mi4) |
| `interface_menu_ferme` | Fermeture de ces menus | 200 ms | 1 | 2D | — | C | L'inverse : toc, puis froissement qui retombe |
| `interface_pret` (Pret) | Un joueur se déclare prêt | 450 ms | 1 | 2D | — | V | Toc et quinte de marimba ensemble (mi5 + si5) |
| `interface_pret_annule` (PretAnnule) | Vote annulé | 350 ms | 1 | 2D | — | V | Deux notes qui retombent (si5 → sol5), plus sombres |
| `interface_tous_prets` (TousPrets) | Tout le monde est prêt | 1 s | 1 | 2D | — | V | Tambour de bois (110 Hz) et arpège montant de marimba (mi5 sol5 si5 mi6) |
| `interface_point_competence` (PointGagne) | Un point de compétence est gagné à l'aube | 0,8 s | 1 | 2D | — | C | Deux notes de marimba en tierce, puis un éclat d'or léger (rappel : c'est une récompense, pas Nyxessa) |
| `interface_notification` | Message au HUD (sac ramassé, rappelé par Nyxessa, palier acheté par un autre) | 300 ms | 1 | 2D | — | C | Toc double rapide aigu (ré6, mi6), discret |
| `interface_score_revele` | Écran de score : une catégorie et son meilleur joueur s'affichent | 0,4 s | 3 | 2D | — | C | Tambour de bois grave et note de marimba, la note monte d'une variante à l'autre |
| `interface_salon_arrivee` | Un joueur rejoint le salon | 0,5 s | 1 | 2D | — | C | Deux tocs qui montent (mi5, si5), clarté pleine |
| `interface_salon_depart` | Un joueur quitte le salon | 0,5 s | 1 | 2D | — | C | Deux tocs qui descendent (si5, mi5), étouffés |
| `interface_classe_choisie` | Une classe est choisie au lobby | 0,6 s | 1 | 2D | — | C | Toc de bois, puis l'accroche sonore de la classe (son d'arme le plus caractéristique, raccourci à 0,4 s) |
| `interface_emote_secteur` | La roue à emotes change de secteur pointé | 40 ms | 1 | 2D | — | B | Micro-tic de bois, hauteur selon le secteur (8 notes de la gamme) |
| `interface_saisie` | Touche tapée dans un champ (pseudo, code de salon) | 30 ms | 3 | 2D | — | B | Clic de bois minuscule, sans note |

### 3.2 Nyxessa (gemme, 3D sur la relique sauf mention)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `nyxessa_tir` (NyxessaTir) | Le cristal pulse et tire un missile | 0,85 s | 3 | 3D | L | V | Poussée grave 110 → 52 Hz (0,2 s), souffle qui part (700 Hz → 5,2 kHz), accord de deux gemmes à la quinte ou à la quarte (mi6 + si6, sol6 + ré7, la6 + mi7), cinq étincelles |
| `nyxessa_missile_vol` (MissileVol, joué par Nyxessa) | Vol du missile en crâne de gemmes | boucle 1,5 s | 1 | 3D | M | V | Souffle continu bande 1,5-3 kHz modulé à 7 Hz, frisson de gemme tenu (deux sinus à 1,5 Hz d'écart vers mi6), sans attaque ; version de Nyxessa 5 demi-tons plus grave que celle du mage squelette (crâne 1,5 fois plus gros) |
| `nyxessa_missile_eclat` (MissileEclat) | Le missile éclate à l'impact | 0,6 s | 3 | 3D | M | V | Choc de verre, grappe de 12 à 16 gemmes courtes (T60 0,1 à 0,25 s) qui partent vers l'aigu, petite poussée grave |
| `nyxessa_frappee` (NyxessaFrappee) | Un ennemi frappe la relique | 0,75 s | 3 | 3D | M | V | Coup sourd (modes 170 et 320 Hz, 50 à 90 ms, bruit passé en bas), gemme grave (mi5, ré5, sol5) doublée d'une seconde un demi-ton trop haut : elle a mal ; quatre éclats |
| `nyxessa_alerte` (NyxessaAlerte) | Relique attaquée (HUD), rappel imminent au donjon | 1,05 s | 1 | 2D | — | V | Deux fois deux notes de gemme qui descendent (si6 → mi6), la seconde paire plus forte, battement grave à 82 Hz sous chacune |
| `nyxessa_palier` (PalierAchete) | Palier des missiles ou du bouclier acheté | 2,5 s | 1 | 3D | L | V | Arpège montant (mi6 sol6 si6 mi7), accord ouvert tenu (mi6 si6 mi7, T60 1,7 s), souffle clair qui s'ouvre, gerbe de 30 étincelles, poussée grave qui monte (55 → 82 Hz) |
| `nyxessa_charge_portail` (ChargePortail) | À l'aube, la charge part vers le portail | 1,95 s | 1 | 3D | L | V | Souffle qui monte (300 Hz → 4,2 kHz, attaque 1,25 s), 42 gemmes qui montent et se densifient, sinus 58 → 118 Hz |
| `nyxessa_retour_energie` (RetourEnergie) | Au crépuscule, l'énergie revient du portail | 1,95 s | 1 | 3D | L | V | Souffle qui retombe (4,2 kHz → 350 Hz), 36 gemmes qui descendent et s'espacent, la relique absorbe : gemme mi5 et poussée 110 → 55 Hz |
| `nyxessa_onde` (EnergieMort, onde du passage au portail) | Une onde fait le tour de la ceinture | 1,15 s | 1 | 3D | M | V | Course de dix gemmes (la5 à mi7) en 0,45 s, amplitude en arche, souffle léger 1,8 → 4,2 kHz |
| `nyxessa_rappel` (NyxessaRappel) | Un joueur resté au donjon est ramené de force | 1,8 s | 1 | 2D | — | V | Accord de gemmes tendu (mi6 + fa6, un demi-ton : c'est une punition), souffle aspiré qui monte vite, puis l'arrivée : gemme grave et poussée ; pour le joueur rappelé seulement |
| `nyxessa_destruction` (NyxessaDestruction) | Nyxessa est détruite, défaite | 3,9 s | 1 | 3D | L | V | Fêlure (bruit en haut, 80 ms), poussée qui s'effondre (92 → 34 Hz, 1,7 s), bris en cascade de 95 éclats hors gamme, dernier soupir grave (mi4 + si4, T60 2,6 s), trois notes qui retombent (si5 sol5 mi5), poussière |
| `nyxessa_reapparition` (Reapparition) | Un joueur renaît près de Nyxessa | 1,3 s | 1 | 3D | M | V | Gemmes qui convergent (registre qui descend de l'aigu vers mi6), souffle aspiré, puis un accord posé (mi5 + si5) et un petit pas sur la pierre |
| `nyxessa_missile_pret` | Un missile de plus dans le stock (icône qui grossit) | 0,25 s | 1 | 2D | — | B | Une seule gemme courte et douce (mi7, T60 0,15 s), très basse |
| `nyxessa_presence` | Présence de la relique au repos (boucle, près d'elle seulement) | boucle 12 s | 1 | 3D | C | C | Bourdon de verre très doux (mi3 + si3 avec battements de 0,3 Hz), une gemme de la ceinture tinte au hasard toutes les 2 à 4 s ; ouverte quand le portail est présent, resserrée la nuit |

### 3.3 Bouclier de Nyxessa et sorcier (gemme plus grave, 3D)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `bouclier_leve` (BouclierLeve) | Le bouclier de gemmes se lève | 1,6 s | 1 | 3D | L | V | Cent gemmes graves (mi4 à mi5) qui montent en spirale et se serrent, souffle circulaire, accord final tenu (mi4 si4 mi5) |
| `bouclier_touche` (BouclierTouche) | Un coup frappe le bouclier | 0,5 s | 4 | 3D | M | V | Choc sourd absorbé (modes 250 et 400 Hz, 40 ms), mur de gemmes qui frémit (six gemmes mi4 à si5, T60 0,2 s), petit retour sec de la riposte |
| `bouclier_etat_entame` | Le bouclier passe au orange (moins de 40 %) | 0,8 s | 1 | 3D | M | C | Accord de gemmes qui glisse d'un demi-ton vers le bas, frisson accéléré (jumeaux à 6 Hz) |
| `bouclier_etat_critique` | Le bouclier passe au rouge (moins de 15 %) | 0,8 s | 1 | 3D | M | C | Même geste, plus bas, avec un craquement de verre |
| `bouclier_brise` (BouclierBrise) | Le bouclier cède | 2,2 s | 1 | 3D | L | V | Craquement de verre, bris en cascade des gemmes graves, souffle qui s'effondre, poussée grave |
| `bouclier_breche` | Coup de brèche de Morgrim martache sur la paroi | 0,9 s | 2 | 3D | L | C | Choc de fer sur verre (éclat d'or grave, 800 à 2 kHz) et gemmes qui crient faux (demi-tons superposés) |
| `sorcier_incantation` (SorcierIncantation) | Le sorcier invoque le bouclier (boucle) | boucle 3 s | 1 | 3D | M | V | Souffle rythmé comme une respiration (bande 600 Hz-1,5 kHz, 0,75 Hz), gemmes qui tintent en motif lent (mi5, si5, mi6) ; pas de voix |
| `sorcier_canalisation` | Lien d'énergie entre le bâton et la relique (boucle, palier 4) | boucle 4 s | 1 | 3D | C | C | Bourdon de gemmes ondulant (si4 + mi5, vibrato lent 0,5 Hz), gemmes qui voyagent le long du lien (petits tintements réguliers) |
| `sorcier_canalisation_eclat` | Un coup fait avancer la recharge d'un missile | 0,4 s | 2 | 3D | C | C | Gemme brève (si6) et petit souffle montant |
| `bouclier_palier` | Le bouclier gagne un palier (plus dense) | 1,2 s | 1 | 3D | L | C | Mur de gemmes qui se densifie : grappe de gemmes graves en crescendo, accord tenu |

### 3.4 Portail et téléportation (gemme, 3D)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `portail_ouverture` (PortailOuverture) | À l'arrivée de la charge, le portail s'ouvre | 1,6 s | 1 | 3D | L | V | Anneau de gemmes qui s'ouvre (gemmes du grave vers l'aigu en cercle, 0,6 s), souffle qui s'étale, puis le bourdon qui s'installe |
| `portail_fermeture` (PortailFermeture) | Au crépuscule, le portail se referme | 1,4 s | 1 | 3D | L | V | Le geste inverse : l'anneau se referme vers le centre, souffle aspiré, une gemme grave qui s'éteint |
| `portail_bourdon` (PortailBourdon) | Portail ouvert (village et retour du donjon) | boucle 6 s | 1 | 3D | C | V | Bourdon de verre (mi3 + si3 + mi4, battements lents), eau qui tourne (bruit en bande 300-800 Hz, modulé à 0,4 Hz), rares gemmes |
| `portail_depart` (PortailPassage) | Le corps d'un joueur part en gemmes vers le centre | 1,1 s | 1 | 3D | M | V | Goutte grave (sinus 400 → 140 Hz, 60 ms), trois anneaux (souffles concentriques de plus en plus aigus), gemmes aspirées qui montent |
| `portail_arrivee` | Les gemmes jaillissent du portail d'arrivée et reforment le corps | 1,1 s | 1 | 3D | M | V | Anneaux du bord vers le centre (souffles qui descendent), gerbe de gemmes qui retombent et s'assemblent en accord (mi5 + si5) |
| `portail_chute_ciel` | Arrivée au donjon : le héros tombe du plafond (`Spawn_Air`) | 1,3 s | 1 | 3D | M | V | Souffle qui descend (3 kHz → 500 Hz) pendant la chute, réception sur la pierre (coup sourd et petit gravier) à l'instant du contact |
| `portail_sortie_sol` | Retour au village : le héros sort du sol (`Spawn_Ground`) | 1,3 s | 1 | 3D | M | V | Terre qui s'ouvre (grondement bref, gravier qui ruisselle vers le haut), deux gemmes au moment où le corps est entier |
| `portail_ferme_refus` | Interagir près du portail fermé la nuit | 0,4 s | 1 | 2D | — | C | Gemme étouffée, sans éclat (clarté 0,2), et toc de pierre |

### 3.5 Joueurs, actions communes

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `pas_herbe` (Pas) | Pas sur l'herbe et la terre du village | 0,15 s | 6 | 3D | C | V | Froissement doux (bruit en bande 1,5-4 kHz, 40 ms) sur un coup sourd (80-120 Hz) |
| `pas_pierre` | Pas sur les dalles, le plateau, le donjon | 0,12 s | 6 | 3D | C | V | Toc de semelle plus net (modes 300-600 Hz, 25 ms), grain de gravier |
| `pas_bois` | Pas dans les intérieurs, escaliers de bois | 0,15 s | 6 | 3D | C | C | Toc de bois grave (mi3 à la3, T60 60 ms), léger craquement (colle-glisse) une fois sur trois |
| `pas_eau` | Pas dans l'eau du donjon (jusqu'aux genoux) | 0,35 s | 4 | 3D | C | V | Clapotis : deux ou trois gouttes (sinus qui glissent vers le haut, 600 Hz → 1,5 kHz, 20 ms) sur un remous grave filtré |
| `eau_entree` | On entre dans l'eau | 0,6 s | 2 | 3D | C | C | Plongeon de jambe : remous large et grappe de gouttes |
| `saut` (Saut) | Saut | 0,25 s | 3 | 3D | C | C | Froissement de vêtement et d'équipement (bruit 1-3 kHz, 60 ms), petit souffle d'effort sans voix |
| `reception` (Reception) | Réception après un saut | 0,25 s | 3 | 3D | C | C | Coup sourd (60-100 Hz), gravier bref |
| `chute_lourde` | Chute de haut (dégâts), grosse chute (Renversé) | 0,6 s | 2 | 3D | M | V | Impact grave plus long (40-70 Hz, 0,2 s), équipement qui cliquette, souffle coupé |
| `esquive` (Esquive) | Esquive, roulade (4 directions) | 0,45 s | 3 | 3D | C | V | Souffle rapide (800 Hz → 2 kHz, 0,15 s) puis roulement sur le sol (bruit brun modulé) et cliquetis d'équipement |
| `sprint_debut` | Le sprint démarre | 0,3 s | 1 | 3D | C | B | Élan : souffle bref et pas appuyé |
| `endurance_vide` | L'endurance tombe à zéro | 0,5 s | 1 | 2D | — | C | Souffle expiré (bruit 400-900 Hz, sans voix) et petit toc de bois grave de l'interface |
| `joueur_touche` (JoueurTouche) | Un héros prend un coup | 0,3 s | 4 | 3D | M | V | Choc mat sur cuir et tissu (modes 150-300 Hz, 40 ms, bruit passé en bas), cliquetis d'équipement ; **pas de cri** |
| `joueur_mort` (JoueurMort) | Un héros meurt et se dissout | 1,6 s | 1 | 3D | L | V | Coup sourd, silence d'une demi-seconde, puis le corps part en gemmes : scintillement qui monte et s'éloigne vers Nyxessa |
| `potion_boire` | Boire une potion de soin | 0,9 s | 2 | 3D | C | V | Bouchon (pop : sinus 900 → 300 Hz, 15 ms), trois gorgées (glou : sinus qui montent 200 → 500 Hz sur bruit filtré), petit soupir d'aise sans voix (souffle doux) |
| `potion_vide` | Plus de potion | 0,3 s | 1 | 2D | — | C | Fiole de verre vide qu'on secoue (deux tintements de verre non accordés, pas de gemme) |
| `relevage_appui` | Chaque appui sur Saut pendant le Renversé | 0,1 s | 2 | 2D | — | B | Froissement court et petit effort, pitch qui monte avec la jauge |
| `emote_salut` | Emote « Salut » | 0,6 s | 1 | 3D | C | B | Froissement d'une manche levée et petit sifflement de deux notes (sinus pur, sol6 → mi6) |
| `emote_acclamation` | Emote « Acclamation » | 1 s | 1 | 3D | C | B | Trois frappes de mains (bruit 1-3 kHz, 8 ms) et froissement |
| `emote_provocation` | Emote « Provocation » | 1 s | 1 | 3D | C | B | Arme frappée contre le bouclier ou le sol, deux fois (selon la classe, son d'arme raccourci) |
| `emote_assis` | Emotes « S'asseoir », « Se reposer » | 0,6 s | 1 | 3D | C | B | Froissement long et petit coup sourd sur le sol |
| `emote_pompes` | Emote « Pompes » (à chaque pompe) | 0,3 s | 2 | 3D | C | B | Petit coup sourd des mains et souffle d'effort sans voix |
| `emote_boire` | Emote « Boire un coup » | 1,2 s | 1 | 3D | C | B | Chope de bois posée, trois gorgées, chope reposée vide (toc plus clair) |
| `emote_mort` | Emote « Faire le mort » | 0,8 s | 1 | 3D | C | B | Chute molle comique : froissement, « plouf » grave (sinus 180 → 60 Hz) |

### 3.6 Coups critiques (commun à toutes les classes, éclat d'or)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `critique` (Critique) | Coup critique (tête, dos, furtif) | 0,45 s | 3 | 3D | M | V | Éclat d'or bref (plaque mince, 2,5 à 7 kHz, T60 0,3 s) sur le son d'impact normal, avec une poussée sèche |
| `critique_meilleur` (CritiqueMeilleur) | Meilleur critique de l'assassin (furtif et dans le dos, × 5) | 0,8 s | 1 | 3D | L | V | Deux éclats d'or en quinte, un souffle qui tranche (5 → 1,5 kHz), poussée grave ; plus fort et plus long |
| `tir_tete` | Flèche ou carreau dans la tête | 0,5 s | 2 | 3D | M | C | Os creux sec et éclat d'or court : on entend l'os |

### 3.7 Paladin (épée et bouclier, thème Sacré)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `epee_elan` (EpeeElan) | Coup d'épée (deux tailles alternées) | 0,3 s | 4 | 3D | C | V | Souffle de lame court et fin (600 Hz → 3 kHz, 0,12 s), léger chant de métal (T60 0,1 s) |
| `epee_impact` (EpeeImpact) | L'épée touche un squelette | 0,3 s | 4 | 3D | M | V | Os creux frappé (modes 450-900 Hz) et tranchant bref (bruit 3-6 kHz, 10 ms) |
| `garde_levee` | Le bouclier se lève (LT) | 0,3 s | 2 | 3D | C | C | Cuir qui se tend, bois du bouclier qui cogne légèrement le bras |
| `blocage` (Blocage) | Coup bloqué par la garde | 0,4 s | 4 | 3D | M | V | Toc profond de bois cerclé (modes 180, 420 Hz, T60 0,12 s), cerclage métallique bref |
| `parade` (Parade) | Parade (fenêtre de 0,35 s) | 0,6 s | 2 | 3D | M | V | Blocage plus un éclat métallique clair qui sonne (T60 0,4 s, 1,5 à 4 kHz), repousse (souffle) |
| `parade_parfaite` | Parade parfaite et coup de bouclier | 0,9 s | 1 | 3D | L | V | Parade, puis éclat d'or doré (thème Sacré, quinte mi6 si6 en plaque mince, pas de gemme), bond (souffle) et coup de bouclier (toc grave et poussée) |
| `garde_brisee` | Garde brisée (endurance vide) | 0,6 s | 1 | 3D | M | C | Bois qui craque, souffle coupé, cliquetis |
| `charge_belier_elan` (Charge) | La charge bélier démarre (7 m) | 1 s | 1 | 3D | M | V | Pas de course précipités sur un souffle qui monte, bélier doré : bourdon d'or (plaques minces en accord) qui enfle |
| `charge_belier_impact` (ChargeImpact) | Percussion à l'arrivée | 0,7 s | 2 | 3D | L | V | Coup de bouclier (bois cerclé, grave), poussée 90 → 40 Hz, éclat d'or, terre qui gicle |
| `soin` (Soin) | Soin sur soi (aura blanc et or, croix vertes) | 1,5 s | 1 | 3D | M | V | Accord doux et chaud de plaques d'or (mi5 sol5 si5), souffle clair qui monte ; les croix vertes restent muettes (pas de gemme : le soin n'est pas Nyxessa) |
| `visiere` | La visière du casque s'abaisse ou se relève | 0,2 s | 2 | 3D | C | B | Petit claquement de charnière métallique |

### 3.8 Viking (hache à deux mains, thème Rage)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `hache_elan` (Hache) | Coup de hache (deux coups alternés) | 0,4 s | 4 | 3D | M | V | Souffle large et grave (400 Hz → 1,8 kHz, 0,2 s), masse qui passe |
| `hache_impact` | La hache touche un ou plusieurs squelettes | 0,4 s | 4 | 3D | M | V | Os brisé (plusieurs os creux simultanés), coup sourd de la masse |
| `tournante` (Tournante) | Attaque tournante maintenue (boucle) | boucle 1,2 s | 1 | 3D | M | V | Souffle cyclique de la hache (un passage par tour, raccord sans couture), grondement sous-jacent |
| `tournante_vent` (TournanteVent) | À chaque tour complet | 0,4 s | 3 | 3D | M | V | Souffle de lame qui passe (existe en extrait Sonniss, à resynthétiser pour l'identité) |
| `rugissement` (Rugissement) | Rugissement, crâne de barbare en gemmes rouges | 1,6 s | 2 | 3D | L | V | Grondement grave (sinus 70-110 Hz modulé irrégulièrement, bruit de gorge filtré en formants 300 et 700 Hz), onde qui part puis revient (souffle aller-retour) ; limite de la synthèse : § 6 |
| `saut_percutant_elan` | Bond de 5 m | 0,5 s | 1 | 3D | M | C | Souffle d'élan, cliquetis d'équipement |
| `saut_percutant_impact` (SautPercutant) | Frappe au sol, onde de terre | 1 s | 2 | 3D | L | V | Poussée 80 → 35 Hz, terre qui gicle, onde de terre (souffle grave qui s'éloigne 600 → 200 Hz) |
| `rage_pleine` | La jauge de rage est pleine | 0,6 s | 1 | 2D | — | B | Grondement court et tambour grave (tambour de bois à 70 Hz) |

### 3.9 Mage (bâton, thème Feu)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `boule_lancer` (BouleLancer) | Boule de feu lancée | 0,5 s | 3 | 3D | M | V | Allumage (« fwoup » : bruit brun qui s'ouvre de 200 à 1,2 kHz en 50 ms), souffle qui part |
| `boule_vol` (BouleVol) | Vol de la boule (boucle) | boucle 1 s | 1 | 3D | M | V | Grondement de feu serré et crépitements denses |
| `boule_explosion` (BouleExplosion) | Explosion à l'impact, fumée à facettes | 1,2 s | 3 | 3D | L | V | Coup grave (poussée 90 → 40 Hz), souffle d'embrasement, crépitements qui retombent, fumée (bruit passé en bas qui s'éteint) |
| `cone` (Cone) | Cône de flammes maintenu (boucle) | boucle 2 s | 1 | 3D | M | V | Rugissement de chalumeau (bruit brun 150-900 Hz modulé), crépitements serrés |
| `cone_debut` | Le cône s'allume | 0,3 s | 1 | 3D | M | C | Souffle d'allumage, flamme qui prend |
| `cone_fin` | Le cône s'éteint | 0,4 s | 1 | 3D | M | C | Flamme qui retombe, braises |
| `mana_vide` | Mana insuffisant | 0,4 s | 1 | 2D | — | C | Flamme qui s'étouffe (« pff » grave), sans le refus de l'interface |

### 3.10 Rôdeur (arc, thème Chasse, flèches non magiques)

Les flèches ne sont pas magiques : bois, corde, plume et air, **jamais de gemme ni de lueur sonore**. Base : les modèles physiques de `Relic/Sources/synth_physique.py` (corde en guide d'onde, corps modal), à reprendre dans la chaîne de Deathless.

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `arc_bander` (ArcBander) | L'arc se bande | 1,2 s | 1 | 3D | C | V | Grincement de bois et de corde qui monte (colle-glisse), synchronisé sur la charge de 1,2 s |
| `arc_pret` (ArcPret) | Charge complète (cercle verrouillé, bref éclat) | 0,3 s | 1 | 3D | C | V | Petit « tic » de corde tendue et une note de bois sèche ; seul signe de la pleine charge |
| `arc_tir` (ArcTir) | Tir peu chargé | 0,5 s | 3 | 3D | M | V | Corde qui claque (guide d'onde), vibration de l'arc, flèche qui part |
| `arc_tir_charge` (ArcTirCharge) | Tir chargé à fond | 0,7 s | 2 | 3D | M | V | Claquement plus sec et plus grave, flèche qui file (souffle qui s'éloigne, Doppler) |
| `fleche_vol` | Traînée d'air de la flèche ou du carreau (boucle) | boucle 0,5 s | 1 | 3D | C | C | Souffle fin et clair (bande 2-5 kHz), sans lueur, sans tonalité |
| `fleche_impact_os` (FlecheImpact) | Flèche dans un squelette | 0,3 s | 3 | 3D | M | V | Os creux piqué, vibration du fût (mode 180 Hz, T60 0,15 s) |
| `fleche_impact_decor` | Flèche dans le sol, le bois, la pierre | 0,3 s | 3 | 3D | C | C | Terre : coup mat ; bois : toc et fût qui vibre ; pierre : toc sec et ricochet |
| `nuee_marqueur` (NueeMarqueur) | Marqueur au sol de la nuée | 0,5 s | 1 | 3D | M | V | Froissement de plumes et note de bois grave (thème Chasse, pas de gemme) |
| `nuee` (Nuee) | Pluie de flèches sur la zone (5 salves) | 2,5 s | 1 | 3D | L | V | Sifflements qui tombent (souffles qui descendent, 30 à 40), impacts dispersés dans la terre et les os |
| `roulade_salve` | Roulade arrière et salve de 5 flèches | 0,7 s | 1 | 3D | M | V | Roulade (esquive) et cinq claquements de corde serrés |
| `visee_zoom` | La visée (LT) resserre la caméra | 0,2 s | 1 | 2D | — | B | Souffle très bref et cuir de la main gantée |

### 3.11 Assassin (dague et arbalète, thème Ombre)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `dague_elan` (Dague) | Coup de dague | 0,2 s | 4 | 3D | C | V | Souffle court et très aigu (1,5 → 5 kHz, 60 ms), rapide |
| `dague_impact` | La dague touche | 0,25 s | 4 | 3D | M | V | Os creux piqué, petit tranchant |
| `arme_echange` | Dague rangée, arbalète en main (et l'inverse) | 0,4 s | 2 | 3D | C | C | Cuir, sangle, déclic de bois |
| `arbalete_tir` (ArbaleteTir) | Tir d'un carreau | 0,5 s | 3 | 3D | M | V | Déclic de noix, arc court qui claque très sec, carreau qui file |
| `arbalete_recharge` (ArbaleteRecharge) | Rechargement (6 s de recharge) | 1 s | 1 | 3D | C | V | Cliquet de bois qui tend la corde (trois crans), cliquet qui se verrouille |
| `carreau_impact` | Carreau dans un squelette | 0,3 s | 3 | 3D | M | V | Comme la flèche, plus lourd et plus sec |
| `furtif_entree` (FurtifEntree) | Passage en mode furtif | 0,6 s | 1 | 2D | — | V | Souffle qui s'étouffe (1,5 kHz → 400 Hz), le monde s'éloigne : légère coupure des aigus (à faire par le mixer, § 7) |
| `furtif_sortie` (FurtifSortie) | Sortie du mode furtif | 0,4 s | 1 | 2D | — | V | Le geste inverse, plus bref |
| `repere` (Repere) | L'assassin est repéré | 0,5 s | 1 | 2D | — | V | Deux os creux secs rapprochés (le squelette se retourne), souffle bref qui monte |
| `grenade_lancer` (GrenadeLancer) | Lancer de la grenade fumigène | 0,4 s | 1 | 3D | C | V | Souffle de lancer, petite fiole qui tournoie |
| `fumee` (Fumee) | La grenade éclate, fumée violette | 1,5 s | 1 | 3D | M | V | Verre qui éclate (petit, pas de gemme), chuintement de fumée (bruit 1-4 kHz qui s'étale), bouffées graves |

### 3.12 Candidats du mois (bonus tant qu'ils ne sont pas décidés)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `clochard_bouteille` | Coup de bouteille | 0,35 s | 3 | 3D | C | B | Souffle court, verre épais qui cogne l'os (toc de verre sourd 500-900 Hz, non accordé) |
| `clochard_bouteille_eclat` | 3e coup : la bouteille éclate | 0,6 s | 1 | 3D | M | B | Verre brisé (grappe de chocs 2-6 kHz, pas de gemme), goulot qui reste en main |
| `clochard_pet_defense` | Pet de défense | 0,7 s | 4 | 3D | M | B | Train d'impulsions de 60 à 120 Hz à hauteur instable (gigue ± 15 %), filtré en bas avec deux formants mous (250 et 600 Hz), fin en « pfft » ; 4 tailles |
| `clochard_nuage` | Nuage pestilentiel (boucle) | boucle 3 s | 1 | 3D | M | B | Bulles lentes et graves (gloups 80-200 Hz), sifflement de gaz faible |
| `clochard_propulsion` | Pet-propulsion (bond) | 0,9 s | 1 | 3D | M | B | Pet long qui monte en hauteur, souffle d'élan, réception |
| `clochard_boire` | Il boit (jauge de gaz) | 0,8 s | 1 | 3D | C | B | Goulot, glouglou, petit rot synthétique (formants 300 / 900 Hz) |
| `djbob_vinyle_lancer` | Lancer de vinyle | 0,4 s | 2 | 3D | M | B | Scratch avant rapide (bruit filtré dont la hauteur suit une vitesse de disque), souffle |
| `djbob_vinyle_vol` | Vol du vinyle (boucle) | boucle 0,6 s | 1 | 3D | M | B | Vrombissement de disque qui tourne (bourdon 110 Hz modulé à 9 Hz), souffle |
| `djbob_vinyle_rattrape` | Le vinyle revient dans la main | 0,3 s | 1 | 3D | C | B | Claquement de plastique, arrêt de disque (hauteur qui plonge) |
| `djbob_scratch` | Scratch en cône | 0,6 s | 3 | 3D | M | B | Deux scratchs aller-retour (bruit et note filtrés dont la hauteur suit la main), onde sonore (souffle) |
| `djbob_drop` | Drop : la piste de danse s'allume | 1,2 s | 1 | 3D | L | B | Montée (« riser » : souffle qui monte, 1 s), coup de grosse caisse synthétique (sinus 150 → 45 Hz) |
| `djbob_piste` | Piste de danse (boucle, 120 BPM) | boucle 8 s | 1 | 3D | M | B | Petit motif disco : grosse caisse à la noire, charleston synthétique (bruit en haut, 20 ms), basse en octaves ; se synthétise bien |
| `djbob_boule` | Boule à facettes lancée puis flottante (boucle) | boucle 3 s | 1 | 3D | M | B | Scintillement métallique fin (pas de gemme de Nyxessa : plaques minces, 5-9 kHz), rotation lente |
| `barde_luth_coup` | Coup de luth | 0,4 s | 3 | 3D | C | B | Caisse de bois creuse frappée (modes 200-500 Hz), cordes qui résonnent au hasard (guide d'onde) |
| `barde_bwoiing` | 3e coup : « bwoiing » | 0,8 s | 1 | 3D | M | B | Corde grave pincée dont la hauteur plonge (guide d'onde avec tension qui baisse) |
| `barde_jouer` | Jouer, maintenu (boucle) | boucle 8 s | 1 | 3D | M | B | Mélodie pincée en mi mineur (guide d'onde) ; limite de la synthèse : § 6 |
| `barde_ballade` | Ballade entraînante | 1,2 s | 1 | 3D | M | B | Trois accords pincés qui montent |
| `barde_accord_dissonant` | Accord dissonant en cône | 0,8 s | 1 | 3D | M | B | Cordes grattées en clusters (demi-tons), souffle qui part |
| `bavaroise_chope` | Coups de chopes | 0,3 s | 3 | 3D | C | B | Chope de verre épais et d'étain qui cogne (toc 400-900 Hz et mousse qui clapote) |
| `bavaroise_trinquer` | Trinquer (gorgée, soin) | 1 s | 1 | 3D | C | B | Deux chopes qui trinquent, gorgées, « ah » sans voix (souffle) |
| `bavaroise_tournee` | Tournée générale : la chope éclate en mousse | 1,2 s | 1 | 3D | M | B | Chope qui éclate, mousse qui gicle et crépite (bulles fines 2-6 kHz) |
| `bavaroise_tonneau` | Charge du tonneau | 1,2 s | 1 | 3D | M | B | Tonneau qui roule (grondement de bois creux périodique), impacts de squelettes renversés |

### 3.13 Squelettes (os et poussière, jamais de voix)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `squelette_sortie` (SqueletteSortie) | Sortie de terre, gerbe de mottes | 1,2 s | 3 | 3D | L | V | Terre qui s'ouvre (coup sourd 60-90 Hz), mottes qui retombent (gravier dense puis clairsemé), os qui s'assemblent (cliquetis) |
| `squelette_pas` | Pas d'un squelette (anti-répétition : un sur trois) | 0,12 s | 4 | 3D | C | C | Os creux léger (T60 30 ms) sur terre, très bas en niveau (ils sont 60) |
| `squelette_preparation` (SquelettePreparation) | Il prépare son coup (0,7 à 0,8 s, yeux qui s'intensifient) | 0,7 s | 3 | 3D | M | V | Crécelle d'os qui s'accélère (chocs de plus en plus serrés), léger souffle qui monte ; se termine juste avant l'impact |
| `squelette_coup` | Un squelette frappe (sbire, voleur) | 0,3 s | 3 | 3D | M | V | Souffle de lame rouillée court, sans chant de métal |
| `guerrier_coup` | Un guerrier frappe (lourd) | 0,4 s | 3 | 3D | M | V | Souffle plus grave, masse de l'arme, os qui craquent à l'effort |
| `squelette_touche` (SqueletteTouche) | Un squelette est touché | 0,25 s | 4 | 3D | M | V | Os creux frappé (modes 450-900 Hz, T60 50 ms), petits éclats d'os |
| `squelette_mort` (SqueletteMort) | Il se désintègre en gemmes couleur os | 1 s | 3 | 3D | M | V | Os qui s'effondrent en grappe (10 à 20 chocs creux), poussière qui s'échappe (bruit brun qui s'éteint), un soupir de sable ; l'accent vert (la magie de Nyxessa qui quitte le squelette) : une seule gemme lointaine, très douce |
| `squelette_aube` (SqueletteAube) | À l'aube, les squelettes restants se désintègrent | 1,5 s | 2 | 3D | L | V | Poussière qui monte et se disperse (souffle qui monte 400 Hz → 3 kHz), os qui tombent en pluie légère |
| `squelette_etourdi` | Un squelette est étourdi | 0,6 s | 2 | 3D | M | C | Mâchoire qui claque deux fois, os qui vacillent (cliquetis lent) |
| `squelette_repousse` | Un squelette est repoussé (charge, parade parfaite) | 0,4 s | 3 | 3D | M | C | Glissement sur la terre (bruit brun qui frotte), cliquetis |
| `mage_squelette_incantation` | Le mage squelette prépare son crâne | 0,8 s | 2 | 3D | M | C | Os creux en roulement lent, souffle qui se charge ; pas de gemme (la gemme est dans le missile) |
| `mage_squelette_tir` | Le mage squelette tire son missile | 0,5 s | 2 | 3D | M | V | Souffle qui part et claquement de mâchoire ; le vol et l'éclat sont ceux du missile de Nyxessa (§ 3.2), 5 demi-tons plus aigus |
| `voleur_elan` | Le voleur se rue sur un joueur isolé | 0,5 s | 2 | 3D | M | C | Pas d'os rapides et souffle de deux lames courtes |
| `elite_aura` | Aura rouge d'un élite (boucle, près de lui) | boucle 3 s | 1 | 3D | C | C | Grondement grave pulsé (70 Hz, 1,5 Hz), cliquetis plus lourd |
| `squelette_danse` | Squelettes qui dansent (Drop de DJ Bob) | boucle 2 s | 1 | 3D | M | B | Cliquetis d'os en rythme à 120 BPM |

### 3.14 Morgrim, le Roi des os (thèmes Terre et Rage)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `morgrim_arrivee` | Morgrim sort de terre (nuit 10) | 3 s | 1 | 2D | — | V | Grondement qui monte de la terre (sub 30-50 Hz), terre qui s'ouvre, grands os qui s'assemblent, puis un creux, puis le premier pas |
| `morgrim_pas` | Pas du colosse | 0,5 s | 3 | 3D | L | V | Poussée 45 → 30 Hz, terre, gros os qui s'entrechoquent |
| `morgrim_preparation` | Préparation longue d'un coup (1,6 s) | 1,6 s | 2 | 3D | L | V | Grondement qui monte, crécelle d'os lente puis rapide, **creux d'un quart de seconde** avant l'impact |
| `morgrim_cri` | Cri qui renforce les squelettes proches | 2 s | 1 | 3D | L | V | Grondement de gorge d'os (formants graves, voir limite § 6), cliquetis de tous les os, onde qui part |
| `morgrim_mort` | Morgrim s'effondre | 3 s | 1 | 3D | L | V | Effondrement en poussière massif : grappe de gros os, sub qui retombe, poussière longue |
| `massue_fracas` (GolemCoup) | Fracas : impact de la boule à pointes | 1,2 s | 2 | 3D | L | V | Impact énorme (sub 60 → 25 Hz, bruit brun), terre, pointes qui mordent le sol |
| `massue_onde` | L'onde de choc s'étend (6 m/s jusqu'à 14 m) | 2,4 s | 1 | 3D | L | V | Souffle grave qui roule (bande 150-500 Hz), gravier qui tremble ; son joué sur le front de l'onde (source qui se déplace avec lui) pour qu'on **entende quand sauter** |
| `massue_onde_sautee` | Le joueur saute l'onde à temps | 0,4 s | 1 | 2D | — | C | Souffle qui passe sous les pieds, petite note d'or (réussite) |
| `massue_tourbillon` | Tourbillon de la boule (boucle) | boucle 1,6 s | 1 | 3D | L | V | Grand souffle cyclique grave, chaîne qui tinte (métal sombre) |
| `massue_charge` | Charge écrasante (course) | 1,5 s | 1 | 3D | L | V | Pas lourds accélérés, grondement qui enfle |
| `massue_charge_impact` | La charge renverse un joueur | 0,8 s | 1 | 3D | L | V | Choc massif et chute du joueur |
| `martache_fauche` | Fauche en cône | 1 s | 2 | 3D | L | V | Souffle de lame géante (300 Hz → 1,5 kHz), fer qui chante sombre (thème Rage) |
| `martache_fend_sol` | Fend-sol : saut, retombée, fissure | 1,6 s | 1 | 3D | L | V | Élan, impact, terre qui se fend en ligne (craquement qui s'éloigne) |
| `martache_breche` | Coup de brèche sur le bouclier | 1 s | 1 | 3D | L | V | Marteau sur verre (voir `bouclier_breche`, § 3.3) |

### 3.15 Nyxar, le Nécromancien (os, violet et éclats de Nyx)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `nyxar_arrivee` | Nyxar apparaît (nuit 12) | 3,5 s | 1 | 2D | — | V | Bourdon grave dissonant, deux gemmes corrompues (ses éclats : gemme de Nyxessa désaccordée d'un quart de ton, frisson rapide), poussière qui tourbillonne |
| `nyxar_teleport_depart` | Il se téléporte (départ) | 0,8 s | 1 | 3D | L | V | Souffle aspiré, os qui s'effondrent en poussière |
| `nyxar_teleport_arrivee` | Il réapparaît | 0,8 s | 1 | 3D | L | V | Poussière qui se reforme, gemme corrompue brève |
| `nyxar_salve` | Salve de crânes | 1 s | 2 | 3D | L | V | Trois ou quatre tirs de missile (mage squelette) serrés, précédés d'un souffle chargé |
| `nyxar_releve` (Invocation) | Il relève des squelettes du sol | 2 s | 1 | 3D | L | V | Grondement de terre et trois sorties de terre (`squelette_sortie`) superposées, décalées |
| `nyxar_faux` | Coup de faux | 0,6 s | 3 | 3D | L | V | Grand souffle fin et très long (lame courbe), sifflement aigu |
| `nyxar_eclat_touche` | Un éclat de Nyx est touché | 0,5 s | 4 | 3D | L | V | Gemme corrompue frappée (quart de ton), craquement de verre qui grandit d'un coup à l'autre |
| `nyxar_eclat_brise` | Un éclat se brise (couronne, grimoire) | 2 s | 2 | 3D | L | V | Bris de gemme, puis l'énergie libérée file (souffle qui monte, gemmes **justes** de Nyxessa) : l'éclat revient à Nyx |
| `nyxar_enrage` | Phase 3 : les deux éclats brisés, il enrage | 2 s | 1 | 2D | — | V | Grondement d'os massif, cliquetis frénétique |
| `nyxar_mort` | Nyxar est vaincu | 4 s | 1 | 2D | — | V | Effondrement, silence, puis une seule gemme juste et claire (mi6) : la victoire commence |

### 3.16 Statuts (sur la cible, 3D ; sur soi, 2D)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `brulure` (Brulure) | Brûlure (boucle tant qu'elle dure, 3 s) | boucle 1 s | 1 | 3D | C | V | Petit feu : crépitements épars (8 par seconde), grondement très léger ; niveau bas (beaucoup de cibles) |
| `brulure_debut` | La brûlure prend | 0,3 s | 2 | 3D | C | C | Allumage bref (« fwip ») |
| `ralenti_debut` | Le héros est ralenti (chute) | 0,5 s | 1 | 2D | — | C | Souffle qui s'alourdit et glisse vers le grave, pas qui traîne |
| `etourdi` | Étourdi (boucle sur la cible) | boucle 1 s | 1 | 3D | C | V | Petites étoiles qui tournent : trois tintements métalliques fins (plaque mince, 4-7 kHz, pas de gemme) en rotation régulière (2 tours par seconde) |
| `renverse_chute` | Renversé : le héros tombe à la renverse | 0,8 s | 1 | 3D | M | V | Chute sur le dos (coup sourd long, équipement), souffle coupé |
| `renverse_releve` | Il se relève | 0,6 s | 1 | 3D | C | C | Froissement, appui, pas qui se replace |
| `ivresse_debut` | Ivresse (bière, tournée) | 0,8 s | 2 | 2D | — | C | Hoquet synthétique (sinus 300 → 600 Hz, 40 ms, sur un souffle), une note de marimba qui glisse (tangage) |
| `ivresse` | Ivresse (boucle douce tant qu'elle dure) | boucle 4 s | 1 | 2D | — | B | Bourdon très doux dont la hauteur tangue au rythme de la caméra (± 30 cents, 0,25 Hz) |
| `provoque` | Un squelette est provoqué par le rugissement | 0,4 s | 2 | 3D | M | C | Mâchoire qui claque et se retourne (deux os creux) |

### 3.17 Village, taverne, forge, achats

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `or_caisse` (Or) | De l'or entre dans la caisse commune ou en sort | 0,6 s | 3 | 2D | — | V | Grappe de pièces (éclat d'or : 6 à 12 tintements 3-9 kHz, T60 0,1 s) qui tombent dans une bourse de cuir (coup mat) |
| `or_ramasse` | On passe sur un tas d'or au donjon | 0,5 s | 3 | 3D | C | V | Pièces qui glissent et s'entrechoquent (4 à 8 tintements), plus vif |
| `sac_or_tombe` | Un joueur meurt au donjon, son sac tombe | 0,6 s | 1 | 3D | M | V | Sac de toile qui tombe lourd (coup sourd), pièces étouffées à l'intérieur |
| `sac_or_ramasse` | Un joueur ramasse le sac d'un autre | 0,8 s | 1 | 3D | C | V | Toile soulevée, grosse grappe de pièces, éclat d'or court |
| `coffre_ouvre` (CoffreOuvert) | Un coffre s'ouvre (le couvercle bascule) | 1 s | 2 | 3D | M | V | Charnière de bois qui grince (colle-glisse), couvercle qui tombe en arrière (toc grave), petit éclat d'or |
| `grand_coffre_ouvre` | Le grand coffre du 2e étage s'ouvre | 1,4 s | 1 | 3D | M | V | Plus lourd, grincement plus long, éclat d'or en accord |
| `taverne_ragout` (Repas) | Se restaurer : un bol de ragoût | 1,2 s | 1 | 2D | — | C | Bol de bois posé, cuillère, deux bouchées (bruit mouillé bref), soupir sans voix |
| `taverne_biere` (Biere) | Boire une bière | 1,4 s | 1 | 2D | — | C | Robinet de tonneau, chope qui se remplit (glouglou qui monte), gorgées, chope reposée |
| `taverne_tournee` | Payer une tournée (tous ivres) | 1,6 s | 1 | 2D | — | C | Quatre chopes qui trinquent, cris de joie remplacés par un accord de marimba festif (mi5 sol5 si5) |
| `potion_achat` | Acheter une potion au druide | 0,8 s | 1 | 2D | — | C | Fiole posée sur le bois, bouchon qui grince, pièces |
| `forge_enclume` (ForgeEnclume) | Le forgeron frappe l'enclume | 1,25 s | 3 | 3D | M | V | Existe (`Assets/Audio/Forge/`, déjà synthétisé pour Deathless) : à garder tel quel, seulement ré-étalonné au niveau de la famille |
| `forge_feu` | Le feu de la forge (boucle) | boucle 8 s | 1 | 3D | C | C | Feu moyen, braises qui palpitent (grondement lent), crépitements épars |
| `forge_soufflet` | Soufflet (toutes les 10 à 20 s) | 1,2 s | 1 | 3D | C | B | Souffle de soufflet de cuir (bande 200-800 Hz), le feu qui ronfle plus fort |
| `sorcier_sortie` | Le sorcier sort de chez lui au crépuscule, rentre à l'aube | 0,6 s | 1 | 3D | M | B | Pas sur les dalles et bâton qui touche la pierre (toc de bois), une gemme douce |

### 3.18 Jour, nuit et partie (annonces 2D)

| Identifiant | Événement | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `crepuscule` (TombeeNuit) | Tombée de la nuit, clairières annoncées | 4 s | 1 | 2D | — | V | Grand geste de 5 s qui accompagne la fermeture du portail : cloche grave de bois (tambour de bois à 55 Hz, deux coups), bourdon de mi qui descend d'une quinte, grillons qui arrivent |
| `aube` (Aube) | Retour du jour | 4 s | 1 | 2D | — | V | Bourdon qui s'éclaire (mi → sol), tambour de bois léger, premiers oiseaux (sifflets de sinus glissés, 2-4 kHz) |
| `vague` (Vague) | Une vague de squelettes est lancée | 2 s | 1 | 2D | — | V | Trois coups de tambour grave de bois et d'os (70 Hz), grondement de terre lointain ; chaque vague un peu plus grave que la précédente |
| `clairiere_active` | Une clairière s'active (3D, dans la clairière) | 2 s | 1 | 3D | L | C | Terre qui gronde et souffle vert de Nyxessa bas et diffus (gemmes très graves, sans éclat) |
| `alerte_nuit_donjon` (AlerteNuit) | 15 s avant le crépuscule, au donjon | 1,5 s | 1 | 2D | — | V | `nyxessa_alerte` en plus lointain et plus grave : Nyxessa appelle depuis le village |
| `boss_annonce` | Annonce du boss de la nuit (10, 12) | 3 s | 1 | 2D | — | V | Coup de tambour grave, bourdon dissonant, silence |
| `victoire` (Victoire) | Aube après la nuit 12 : la partie est gagnée | 6 s | 1 | 2D | — | V | Fanfare de marimba et de gemmes en sol majeur (arpèges montants, accord final tenu), tambour de bois ; précède la musique de victoire |
| `defaite` | Nyxessa détruite, l'écran de score arrive | 4 s | 1 | 2D | — | V | Après `nyxessa_destruction` : bourdon grave de mi, trois notes de marimba sourde qui retombent, silence |

### 3.19 Ambiances (boucles, stéréo 2D sauf mention)

| Identifiant | Lieu, moment | Durée | Var. | Espace | Portée | Prio. | Description acoustique |
|---|---|---|---|---|---|---|---|
| `ambiance_village_jour` | Place du village, de jour | boucle 60 s | 1 | 2D | — | V | Vent léger dans les feuilles (bruit rose passé en bas, lentement modulé), oiseaux épars (sifflets glissés, 2 à 5 par 10 s), quelques bruits de village lointains (bois) |
| `ambiance_village_nuit` | Place du village, la nuit | boucle 60 s | 1 | 2D | — | V | Grillons (impulsions à 4-5 kHz en trilles de 30 Hz, groupes irréguliers), vent froid plus grave, brume (souffle très bas) |
| `ambiance_foret` | Lisière et forêt (zones 3D sous les arbres) | boucle 30 s | 1 | 3D | M | C | Feuillages plus proches, craquements de branches rares, oiseau de nuit (hibou : deux sinus graves à 400 Hz) la nuit |
| `ambiance_clairiere` | Clairières d'apparition (zone verte) | boucle 20 s | 1 | 3D | M | C | Souffle bas et diffus, gemmes très graves qui tintent rarement (Nyxessa), terre qui frémit |
| `ambiance_donjon` | Donjon (sombre, torches, sans ciel) | boucle 60 s | 1 | 2D | — | V | Souffle creux grave (bruit brun 80-250 Hz), gouttes rares (sinus glissés), grincements lointains, pas d'oiseaux ; une « pièce » : c'est la réverbération du mixer qui fait la taille (§ 5) |
| `ambiance_donjon_eau` | Demi-niveau d'eau du donjon | boucle 20 s | 1 | 3D | M | C | Clapotis doux, gouttes plus fréquentes |
| `torche` | Torche du donjon, lanterne (3D, courte portée) | boucle 6 s | 1 | 3D | C | C | Petit feu : grondement doux, deux crépitements par seconde |
| `ambiance_interieur` | Boutiques (druide, mécano), maison du sorcier | boucle 30 s | 1 | 2D | — | C | Pièce calme : bois qui craque, chaudron qui bulle (druide), extérieur étouffé (ambiance du village passée en bas) |
| `ambiance_taverne` | Taverne (jour) | boucle 45 s | 1 | 2D | — | C | Âtre qui crépite, chopes et couverts, murmure de salle **abstrait** (bruit filtré en formants lents, sans mots ; limite § 6) |
| `ambiance_forge` | Intérieur de la forge | boucle 30 s | 1 | 2D | — | C | Feu de forge proche, soufflet, sans les coups d'enclume (joués à part, synchronisés) |

**Nombre de sons inventoriés : 231 identifiants** (hors musiques), soit 386 fichiers en comptant les variantes. Le lot 1 en produit 18 (24 fichiers). Détail par priorité : § 7.

---

## 4. Musiques

### 4.1 Principe : des couches plutôt que des morceaux

La nuit ne change pas de morceau à chaque vague : **une base calme joue toujours, des couches s'ajoutent** selon l'intensité, toutes au même tempo, dans la même tonalité et de la **même longueur**, lues ensemble depuis le même instant (synchronisées à l'échantillon près). Monter d'un cran, c'est ouvrir une couche en fondu de 2 s ; redescendre, la refermer en 4 s. Aucune transition ne coupe la phrase en cours.

**Intensité de la nuit** (calculée par le jeu, proposition) :

| Niveau | Quand | Couches ouvertes |
|---|---|---|
| 0 | Crépuscule, répit entre deux vagues, aucun squelette à moins de 40 m de Nyxessa | Base |
| 1 | Une vague est en marche (les squelettes marchent sur le village) | Base + Tambours |
| 2 | Combat : au moins un squelette à moins de 15 m de Nyxessa ou d'un joueur | Base + Tambours + Pulsation |
| 3 | Danger : Nyxessa sous 40 %, bouclier brisé, ou plus de 25 squelettes au village | Base + Tambours + Pulsation + Tension |

Le jour suit le même principe avec deux couches (village calme, puis « on se prépare » quand un vote « prêt » est lancé). Les boss remplacent les couches de la nuit par les leurs (même tempo pour que le passage se fasse en fondu sur une mesure).

**Code** : `AudioBank.Musique` ne joue qu'un clip à la fois, en fondu enchaîné. Les couches demandent un petit lecteur `MusiqueCouches` (N sources lancées ensemble par `AudioSource.PlayScheduled`, un volume par couche, fondus par couche). C'est une décision de code (§ 7, décision 4), hors de cette session.

### 4.2 Liste des morceaux

Tous en **mi mineur** (nuit, combat, donjon) ou en **sol majeur**, son relatif (jour, menu, victoire), pour que les tintements de Nyxessa et les sons d'interface restent dans la gamme. Stéréo, 44,1 kHz, 16 bits, crête -1,4 dBFS.

| Morceau | Tempo | Tonalité | Instruments | Durée | Forme | Transitions |
|---|---|---|---|---|---|---|
| **Menu principal** | 84 BPM | sol majeur | Marimba grave, cordes pincées (guide d'onde), gemmes rares (Nyxessa, au loin), bourdon doux | boucle 64 mesures en 4/4 = 3 min 03 | Boucle simple, une phrase de 8 mesures qui revient avec variations | Fondu de 1,5 s vers le lobby (même morceau, filtré) |
| **Village, jour** (MusiqueJour) | 96 BPM | sol majeur | Cordes pincées, flûte de bois (sinus avec souffle), tambour de bois léger, marimba | boucle 48 mesures = 2 min | 2 couches : **Calme** (flûte, cordes) ; **Préparation** (tambour, marimba, s'ouvre au premier vote « prêt ») | Au crépuscule, les couches se referment en 3 s sur le stinger du crépuscule |
| **Crépuscule** (stinger) | libre | mi mineur | Tambour de bois grave, bourdon qui descend, gemmes graves | 5 s | Une fois, synchronisé sur les 5 s de transition | Enchaîne sur la couche Base de la nuit |
| **Nuit** (MusiqueNuit) | 110 BPM | mi mineur | **Base** : bourdon de mi et de si, pad de souffle filtré, cloche de bois toutes les 4 mesures. **Tambours** : caisses de bois et d'os (cliquetis) en motif de 2 mesures. **Pulsation** : basse pincée en ostinato de croches (mi, sol, la, si), marimba à contretemps. **Tension** : gemmes rapides en arpège (double croches, mi6-mi7), cordes graves trémolo, grosse caisse à la noire | boucle 32 mesures = 1 min 10 | 4 couches synchronisées | Montée 2 s, descente 4 s ; à l'aube, tout se referme en 3 s sous le stinger de l'aube |
| **Aube** (stinger) | libre | sol majeur | Bourdon qui s'éclaire, marimba, gemmes justes | 5 s | Une fois | Enchaîne sur la couche Calme du jour |
| **Morgrim** (nuit 10) | 110 BPM | mi phrygien (fa naturel) | Tambours graves lourds, basse distordue douce (saturation par fonction tangente), os en crécelle, cuivres synthétiques graves (dents de scie filtrées) | boucle 32 mesures = 1 min 10 | 2 couches : **Morgrim** et **Morgrim enragé** (sous 30 % de vie) | Remplace les couches de la nuit en fondu sur une mesure ; à sa mort, stinger de 3 s puis retour à la nuit |
| **Nyxar** (nuit 12) | 120 BPM | mi mineur harmonique (ré dièse) | Orgue de gemmes corrompues (gemmes désaccordées d'un quart de ton), chœur remplacé par un pad de formants (limite § 6), tambours, basse | boucle 32 mesures = 1 min 04 | 3 couches, une par **phase** (éclats intacts, un éclat brisé, les deux brisés) | À chaque éclat brisé, la couche suivante entre sur le temps fort suivant |
| **Victoire** | 96 BPM | sol majeur | Fanfare de marimba et de gemmes justes, cordes pincées, tambour de bois | 30 s puis boucle douce de 16 mesures | Une fois, puis boucle sous l'écran de score | Après le son `victoire` |
| **Défaite** | 72 BPM | mi mineur | Bourdon, marimba sourde, une gemme qui s'éteint | 20 s puis boucle de 8 mesures | Une fois, puis boucle sous l'écran de score | Après `defaite` |
| **Taverne** | 150 BPM | ré majeur (sol majeur en passage) | Accordéon synthétique (paires de dents de scie désaccordées de 3 à 6 cents, filtrées), tuba (sinus grave et souffle), caisse claire de bois ; polka en 2/4, humour assumé | boucle 64 mesures = 51 s | Boucle simple, entendue dans la taverne seulement (source 3D dans la salle, et 2D atténuée à l'intérieur) | Se mêle à la musique du village par la distance |
| **Donjon** | 72 BPM | mi dorien (do dièse) | Bourdon creux, gouttes accordées, marimba grave très espacée, souffle | boucle 32 mesures = 1 min 47 | 2 couches : **Exploration** ; **Alerte** (s'ouvre aux 15 s de l'alerte de nuit, pulsation de tambour grave) | En sortant par le portail, fondu de 2 s vers le village |

---

## 5. Contraintes techniques

**Formats.**
- Effets : **WAV 44,1 kHz, mono, 16 bits** (le moteur spatialise).
- Ambiances 2D et musiques : **WAV 44,1 kHz, stéréo, 16 bits**. Ambiances 3D (forêt, clairière, torche, eau) : mono.
- Unity convertira à l'import (Vorbis pour les musiques et ambiances, qualité 70 ; ADPCM ou PCM pour les effets courts) : réglages d'import à poser par l'agent local, pas ici.

**Niveaux.**
- Crête de chaque fichier : **-1,4 dBFS au plus** (0,85).
- Le **niveau perçu** d'un effet est son **RMS maximal sur 50 ms** (mesure déjà utilisée par `synth_physique.loudness_db`, reprise par `deathless_audio.niveau_percu`). Chaque son vise une **cible** ; le mastering applique le gain qui l'atteint, sans dépasser la crête (un son très transitoire peut rester un peu sous sa cible : c'est voulu).
- Cibles par famille (dB), pour que tout soit à l'échelle **avant** le mixer :

| Famille | Plage | Référence | Remarques |
|---|---|---|---|
| Interface | -24 à -14 | -18 | Survol le plus bas ; « tous prêts » le plus haut |
| Nyxessa, portail, bouclier | -17 à -12,5 | -14 | Destruction, palier : -12,5 (événements majeurs) ; onde, missile prêt : -17 et plus bas |
| Joueurs (actions communes) | -28 à -13 | -16 | Pas -26 à -28 ; mort -13 |
| Armes et compétences | -17 à -12,5 | -14 | Boucles -17 ; tir chargé, meilleur critique, parade parfaite -12,5 |
| Squelettes | -28 à -14 | -16 | Ils sont jusqu'à 60 : pas -28, mort -15, sortie de terre -14 |
| Boss | -14 à -11 | -12 | Les sons les plus forts du jeu, hors musique |
| Statuts | -22 à -15 | -18 | Boucles de statut -22 (beaucoup de cibles) |
| Village, donjon, objets | -20 à -13 | -16 | Or, coffres -14 ; forge -16 |
| Jour, nuit, partie (annonces) | -15 à -11 | -13 | Victoire -11 |
| Ambiances | RMS moyen -32 à -26 dBFS | -30 | Mesurées en RMS moyen sur toute la boucle : ce sont des tapis |
| Musiques | RMS moyen -22 à -18 dBFS | -20 | Mesurées sur la boucle entière toutes couches ouvertes ; chaque couche seule vers -26 |

**Pas de réverbération dans les fichiers** : l'espace vient du jeu. Le donjon et les intérieurs demandent une réverbération de pièce dans le mixer (effet `SFX Reverb` sur un groupe, ou `AudioReverbZone`) : décision de code, § 7.

**Attaque et fin.** Jamais plus de **20 ms de silence en tête** (un son part au déclenchement ; mesuré : premier échantillon au-dessus de -40 dBFS). Fin sans clic (fondu en cosinus de 12 à 60 ms). Boucles : raccord sans couture (fondu enchaîné de la fin sur le début dans le script, 50 à 200 ms), longueur exacte d'un nombre entier de mesures pour les musiques.

**Nommage.**
- Fichiers : `famille_evenement_N.wav` (N = 1, 2… pour les variantes ; pas de numéro s'il n'y en a qu'une), en minuscules, sans accent, en français (`nyxessa_frappee_2.wav`, `interface_clic_1.wav`). Boucles : `famille_evenement_boucle.wav`. L'importeur du catalogue (`JeuBuilder`) ne reconnaît aujourd'hui une boucle qu'à `_loop` dans le nom du fichier : il faudra qu'il reconnaisse aussi `_boucle` (le drapeau n'est pas lu par `AudioBank`, qui boucle déjà tout ce qu'on lui donne par `AudioBank.Boucle`). Musiques : `musique_<morceau>_<couche>.wav` (`musique_nuit_tambours.wav`).
- Ids du catalogue : le nom sans numéro. Tant que l'ancien son est branché sous un autre id, le nouveau prend le préfixe **`dl_`** (identité Deathless) : `dl_nyxessa_tir`. Le branchement se fait en mettant cet id en tête de la liste de `SonsDuJeu` (la liste est déjà un ordre de préférence, et l'ancien son y reste en repli).

**Dossiers et scripts.**
- `Assets/Audio/Deathless/<Famille>/` : les WAV et **un script par famille** (`synth_<famille>.py`), qui écrit tous les fichiers de la famille.
- `Assets/Audio/Deathless/deathless_audio.py` : briques communes (timbres de la palette, filtres, mastering, analyse), importées par chaque script.
- `Assets/Audio/Deathless/controle.py` : planches de contrôle, une par lot (`Docs/son-lotN-controle.md`, un tableau par script). Chaque script déclare ses sons dans une liste `SONS` de (nom, lot, cible en dB, fondu de fin ou `BOUCLE`, fabrique) : `controle.py` y lit le lot et la cible de chaque fichier, et `deathless_audio.produire` écrit les fichiers.
- **Boucles** : rendues sans raccord par `deathless_audio` (`plier` replie la traîne des événements sur le début, `fondre_boucle` fond le continu à puissance constante, les sinus tenus ont un nombre entier de périodes sur la boucle) ; `controle.py` vérifie qu'il n'y a pas de saut à la jointure.
- Familles prévues : Interface, Nyxessa, Bouclier, Portail, Joueurs, Critiques, Paladin, Viking, Mage, Rodeur, Assassin, Candidats, Squelettes, Morgrim, Nyxar, Statuts, Village, JourNuit, Ambiances, Musique.

**Graines fixes**, une plage par famille (un script relancé réécrit exactement les mêmes fichiers) : Nyxessa 1100-1199, Interface 1200-1299, Portail 1300, Bouclier 1350, Joueurs 1400, Critiques 1450, Paladin 1500, Viking 1600, Mage 1700, Rodeur 1800, Assassin 1900, Squelettes 2000, Morgrim 2100, Nyxar 2150, Statuts 2200, Village 2300, JourNuit 2500, Ambiances 2600, Candidats 2700, Musique 3000-3999.

**Catalogue.** Une entrée par son dans `Wiki/data/sons.json` : `source: "Deathless (synthèse)"`, `licence: "propre"`, `statut: "a_ecouter"` jusqu'à l'écoute de Quentin, puis `utilise`, et pour un son 3D `portee` en mètres (C = 20, M = 40, L = 60, colonne Portée du § 3 ; absent pour un son 2D). L'usage dit quel ancien son il remplacera. Ne pas toucher aux `.meta` : Unity les crée à l'import.

**Commandes.** Toujours `python -B` (pas de `__pycache__` dans `Assets/`) :
```
python -B Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py
python -B Assets/Audio/Deathless/Interface/synth_interface.py
python -B Assets/Audio/Deathless/controle.py        # toutes les planches ; controle.py 2 : le lot 2 seulement
```
Un script accepte un dossier de sortie (pour écouter hors de `Assets/`) et une liste de noms : `python -B synth_nyxessa.py <dossier> nyxessa_tir_1 nyxessa_palier`.

---

## 6. Méthode de production et ses limites

**La méthode possible dans le dépôt** : la synthèse par script, en **Python standard seulement** (math, random, struct, wave, cmath), sans paquet ni téléchargement, sans échantillon. Les briques : modes amortis (résonances d'objets), filtres de bruit (souffles, poussière, feu), guides d'onde (cordes), glissements de hauteur (poussées, gouttes), grappes aléatoires (gemmes, gravier, pièces). Tout est déterministe, léger (le lot 1 se génère en 2 s) et relu en Markdown. C'est ce qui garantit une identité cohérente : les mêmes briques, la même gamme, les mêmes niveaux partout.

**Ce que la synthèse fait très bien** : impacts, tintements, gemmes, bois, os, souffles, élans d'armes, interface, pluie de pièces, portails, téléportation, poussière, pets (réellement : un train d'impulsions à hauteur instable, filtré, est le modèle classique). **Convenablement** : feu, eau, nappes et bourdons, percussions (tambours de bois, grosses caisses), grillons et oiseaux simples, cordes pincées (guide d'onde), marimba.

**Ce qui restera moins bon en synthèse** (à dire clairement) :
1. **Les voix** : le rugissement du viking, le cri de Morgrim, un rire de Nyxar, le brouhaha de la taverne, les soupirs, un « hic ». Des formants filtrés donnent un grondement ou un murmure abstrait, jamais une vraie voix. Proposition par défaut : **pas de voix humaine** dans Deathless (les héros sont muets, les squelettes aussi, les effets parlent à leur place), avec des « presque voix » synthétiques pour le rugissement et le cri, assumées comme des sons de créature.
2. **Les instruments réalistes** : luth du barde, accordéon de la taverne, cordes frottées, cuivres, chœurs. On obtient des versions « jouet » (guide d'onde, dents de scie filtrées) : charmantes, cohérentes avec le low poly, mais pas réalistes.
3. **Les musiques** : la synthèse maison donne une musique de type **boîte à musique et percussions de bois**, simple et lisible, qui peut devenir le style du jeu (« low poly sonore »), mais elle ne rivalisera pas avec une composition jouée ou produite dans un séquenceur. Le calcul est long en Python pur (quelques minutes par couche de 1 min en stéréo), sans être bloquant.
4. **Les espaces** : pas de réverbération dans les fichiers ; tout le rendu de salle dépend du mixer d'Unity, à régler en jeu.
5. **L'écoute** : une session cloud ne peut pas écouter. Elle contrôle par la mesure (planche : niveaux, spectre, attaque) et par des spectrogrammes ; seule l'oreille de Quentin valide un son.

**Décisions attendues de Quentin** (reprises au § 7 et dans le rapport) :
- **Musique** : (a) **synthèse maison** dans le style boîte à musique et bois (gratuit, cohérent, limité) ; (b) **bibliothèque libre** (CC0 ou CC-BY, attribution aux crédits : cohérence de style plus difficile) ; (c) **outil externe** (séquenceur et instruments virtuels, compositeur, ou outil génératif dont la licence permet un jeu commercial), les fichiers étant ensuite déposés dans `Assets/Audio/Deathless/Musique/` au même format et aux mêmes niveaux.
- **Voix** : aucune voix (proposition), voix enregistrées (Quentin et des amis), ou banque libre de droits (comme l'extrait Sonniss déjà utilisé).

---

## 7. Plan de production

### 7.1 Décompte

| Priorité | Identifiants | Fichiers (variantes comprises) |
|---|---|---|
| Vital | 129 | 236 |
| Confort | 61 | 92 |
| Bonus | 41 | 58 |
| **Total** | **231** | **386** |

À quoi s'ajoutent les **11 morceaux** du § 4 (22 fichiers de couches et stingers).

### 7.2 Lots

Chaque lot fait 15 à 25 sons (identifiants ou fichiers), tient dans une session cloud indépendante, et se termine comme le lot 1 : script de la famille, WAV générés, entrées `a_ecouter` dans le catalogue, planche de contrôle régénérée (`controle.py` : renommer la planche `son-lotN-controle.md` ou la garder cumulée), pull request. Ordre : les sons vitaux d'abord, en commençant par ce qui s'entend le plus souvent.

| Lot | Contenu | Priorité | Sons |
|---|---|---|---|
| **1** (fait) | Nyxessa (tir, frappée, alerte, palier, charge, retour, onde, destruction) et interface (survol, clic, retour, refus, confirmation, décompte, onglet, votes) | V | 18 ids, 24 fichiers |
| **2** (fait) | Nyxessa (suite : missile vol et éclat, rappel, réapparition), bouclier et sorcier, portail et téléportation | V et C | 22 ids, 29 fichiers |
| 3 | Squelettes : sortie, préparation, coups, touché, mort, aube, pas, mage squelette ; nouvelle brique os creux et poussière | V | 15 ids, environ 45 fichiers |
| 4 | Joueurs : pas (4 sols), saut, réception, chutes, esquive, touché, mort, potion | V | 13 ids, environ 45 fichiers |
| 5 | Paladin et critiques ; nouvelle brique éclat d'or | V | 15 ids |
| 6 | Viking et mage ; nouvelle brique feu | V | 15 ids |
| 7 | Rôdeur et assassin (repris des modèles physiques de `synth_physique.py`) | V | 22 ids |
| 8 | Statuts ; village (or, sac, coffres, taverne, forge) | V et C | 23 ids |
| 9 | Jour, nuit, partie (crépuscule, aube, vagues, alertes, victoire, défaite) et Morgrim (première moitié) | V | 18 ids |
| 10 | Morgrim (seconde moitié) et Nyxar | V | 18 ids |
| 11 | Ambiances (10 boucles stéréo et 3D) | V et C | 10 ids |
| 12 | Musique, preuve de méthode : la nuit en 4 couches et les deux stingers (si Quentin choisit la synthèse) | V | 6 fichiers |
| 13 | Musique : village jour (2 couches), menu, donjon (2 couches) | V | 5 fichiers |
| 14 | Musique : Morgrim, Nyxar (couches), victoire, défaite, taverne | C | 9 fichiers |
| 15 | Interface (confort : curseur, cases, menus, notification, score, salon, point de compétence) et sons de confort restants | C | 20 ids |
| 16 | Emotes et bonus des classes jouables | B | 15 ids |
| 17 | Candidats du mois, un personnage par lot quand il est décidé (Clochard, DJ Bob, Barde, Bavaroise) | B | 5 à 7 ids chacun |

### 7.3 Décisions et travaux hors session cloud

1. **Écouter le lot 1** (encart « à écouter » de la page Sons du wiki) et valider ou corriger la direction : c'est lui qui fixe la suite.
2. **Musique** : synthèse maison, bibliothèque libre ou outil externe (§ 6).
3. **Voix** : aucune voix, voix enregistrées, ou banque (§ 6).
4. **Code, par l'agent local** : brancher les ids `dl_*` en tête des listes de `SonsDuJeu` (et les clips de `ReglagesAudio` pour l'interface) ; lecteur de musique à couches `MusiqueCouches` ; intensité de la nuit ; groupe **Ambiances** dans le mixer (aujourd'hui : Principal, Musique, Effets, Interface) et son curseur dans les options ; réverbération de pièce pour le donjon et les intérieurs ; coupure des aigus pendant le mode furtif.
5. **Portée par son** dans `AudioBank` (aujourd'hui 60 m pour tous les effets 3D, 45 m pour les boucles) : les colonnes Portée du § 3 la donnent.
6. **Sort des anciens sons** : une fois une famille validée, les sons Kenney, Relic et Sonniss qu'elle remplace passent en `disponible` (« ancienne version »), puis peuvent quitter le jeu. L'enclume de la forge, déjà synthétisée pour Deathless, reste.

---

## 8. Lots produits

### 8.1 Lot 1 produit

24 fichiers, 18 identifiants, générés le 26/09/2026 :

- `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py` : `nyxessa_tir_1..3`, `nyxessa_frappee_1..3`, `nyxessa_alerte`, `nyxessa_palier`, `nyxessa_charge_portail`, `nyxessa_retour_energie`, `nyxessa_onde`, `nyxessa_destruction` (12 fichiers, 8 ids).
- `Assets/Audio/Deathless/Interface/synth_interface.py` : `interface_survol_1..2`, `interface_clic_1..2`, `interface_retour`, `interface_refus`, `interface_confirmation`, `interface_decompte`, `interface_onglet`, `interface_pret`, `interface_pret_annule`, `interface_tous_prets` (12 fichiers, 10 ids).
- Catalogue : ids `dl_nyxessa_*` et `dl_interface_*` dans `Wiki/data/sons.json`, statut `a_ecouter`.
- Planche de contrôle : [`son-lot1-controle.md`](son-lot1-controle.md) (24 fichiers conformes : 44,1 kHz mono, crête sous -1,4 dBFS, tête sous 20 ms, niveau à la cible).

**Branchement proposé** (après écoute, par l'agent local ; aucun script de jeu n'a été modifié) :

| Constante de `SonsDuJeu` ou réglage | Nouvel id en tête |
|---|---|
| `NyxessaTir` | `dl_nyxessa_tir` |
| `NyxessaFrappee` | `dl_nyxessa_frappee` |
| `NyxessaAlerte`, et `AlerteNuit` au donjon | `dl_nyxessa_alerte` |
| `PalierAchete` | `dl_nyxessa_palier` |
| `ChargePortail` | `dl_nyxessa_charge_portail` |
| `RetourEnergie` | `dl_nyxessa_retour_energie` |
| `EnergieMort`, et `NyxessaRappel` en repli | `dl_nyxessa_onde` |
| `NyxessaDestruction` | `dl_nyxessa_destruction` |
| `Pret` | `dl_interface_pret` |
| `PretAnnule` | `dl_interface_pret_annule` |
| `TousPrets`, `PointGagne` | `dl_interface_tous_prets` |
| `PointDepense` | `dl_interface_confirmation` |
| `AchatRefuse` | `dl_interface_refus` |
| `ReglagesAudio` : survol, clic, retour, refus | `interface_survol_1`, `interface_clic_1`, `interface_retour`, `interface_refus` (clips) |
| Décompte (`ui_decompte`) | `dl_interface_decompte` |

### 8.2 Lot 2 produit

29 fichiers, 22 identifiants, générés le 26/09/2026 (Nyxessa, suite ; bouclier et sorcier ; portail et téléportation) :

- `Assets/Audio/Deathless/Nyxessa/synth_nyxessa.py` (complété) : `nyxessa_missile_vol_boucle`, `nyxessa_missile_eclat_1..3`, `nyxessa_rappel`, `nyxessa_reapparition` (6 fichiers, 4 ids).
- `Assets/Audio/Deathless/Bouclier/synth_bouclier.py` : `bouclier_leve`, `bouclier_touche_1..4`, `bouclier_etat_entame`, `bouclier_etat_critique`, `bouclier_brise`, `bouclier_breche_1..2`, `bouclier_palier`, `sorcier_incantation_boucle`, `sorcier_canalisation_boucle`, `sorcier_canalisation_eclat_1..2` (15 fichiers, 10 ids).
- `Assets/Audio/Deathless/Portail/synth_portail.py` : `portail_ouverture`, `portail_fermeture`, `portail_bourdon_boucle`, `portail_depart`, `portail_arrivee`, `portail_chute_ciel`, `portail_sortie_sol`, `portail_ferme_refus` (8 fichiers, 8 ids).
- Briques ajoutées à `deathless_audio.py` : `plaque` (éclat d'or et fer, § 2), `gravier` et `pas_pierre` (poussière et terre, § 2), boucles sans raccord (`plier`, `fondre_boucle`, `master_boucle`) et `produire` (écriture d'une famille). Les scripts du lot 1 déclarent désormais leurs sons dans la même liste `SONS` ; leurs 24 fichiers sont inchangés, à l'octet près.
- Catalogue : ids `dl_nyxessa_*` (4), `dl_bouclier_*` (7), `dl_sorcier_*` (3) et `dl_portail_*` (8) dans `Wiki/data/sons.json`, statut `a_ecouter`, avec `portee` pour les sons 3D (ajoutée aussi aux sons 3D du lot 1).
- Planche de contrôle : [`son-lot2-controle.md`](son-lot2-controle.md) (29 fichiers conformes, dont 4 boucles vérifiées à la jointure).

**Hypothèses prises** (à confirmer en jeu) :
- **Chute du ciel** et **sortie du sol** : le clip dure 1,3 s ; le contact avec le sol (`Spawn_Air`) et le corps entier (`Spawn_Ground`) sont placés vers **1,0 s**, faute de mesure de l'instant dans le clip. Si l'instant diffère, décaler le déclenchement du son plutôt que le fichier.
- **Missile en vol** : la boucle est celle du missile de **Nyxessa** (gemme tenue sur si5). Celle du mage squelette, 5 demi-tons plus aiguë (§ 3.2), viendra avec le lot 3 (même fonction, `base="E6"`).
- **Rappel** : son 2D pour le joueur rappelé seulement ; les autres joueurs entendent l'arrivée (`portail_sortie_sol`) au village.
- **Réapparition** : un seul fichier, joué au point de réapparition près de Nyxessa.
- **Boucles** : `nyxessa_missile_vol_boucle` 1,5 s, `sorcier_incantation_boucle` 3 s, `sorcier_canalisation_boucle` 4 s, `portail_bourdon_boucle` 6 s ; cibles -17 à -20 dB, sous la plage des événements de la famille (ce sont des sons de présence, entendus de près).

**Branchement proposé** (après écoute, par l'agent local ; aucun script de jeu n'a été modifié) :

| Constante de `SonsDuJeu` ou réglage | Nouvel id en tête |
|---|---|
| `MissileVol` | `dl_nyxessa_missile_vol` (pour Nyxessa ; le mage squelette garde `skull_flight_loop` jusqu'au lot 3) |
| `MissileEclat` | `dl_nyxessa_missile_eclat` |
| `NyxessaRappel` | `dl_nyxessa_rappel` |
| `Reapparition` | `dl_nyxessa_reapparition` |
| `BouclierLeve` | `dl_bouclier_leve` |
| `BouclierTouche` | `dl_bouclier_touche` |
| `BouclierBrise` | `dl_bouclier_brise` |
| `SorcierIncantation` | `dl_sorcier_incantation` |
| `PortailOuverture` | `dl_portail_ouverture` |
| `PortailFermeture` | `dl_portail_fermeture` |
| `PortailBourdon` | `dl_portail_bourdon` |
| `PortailPassage` | `dl_portail_depart` |
| *nouveau* : arrivée par le portail (`PortalTransit.Arrive`) | `dl_portail_arrivee` |
| *nouveau* : clip `Spawn_Air` (arrivée au donjon) | `dl_portail_chute_ciel` |
| *nouveau* : clip `Spawn_Ground` (retour au village, rappel) | `dl_portail_sortie_sol` |
| *nouveau* : Interagir au portail fermé | `dl_portail_ferme_refus` |
| *nouveau* : bouclier sous 40 % et sous 15 % (changement de thème de `RelicShieldVisual`) | `dl_bouclier_etat_entame`, `dl_bouclier_etat_critique` |
| *nouveau* : coup de brèche de Morgrim martache sur le bouclier | `dl_bouclier_breche` |
| *nouveau* : palier du bouclier acheté (en plus de `dl_nyxessa_palier`) | `dl_bouclier_palier` |
| *nouveau* : canalisation du sorcier (boucle tant qu'elle dure) et son éclat (`AvancerRechargeMissiles`) | `dl_sorcier_canalisation`, `dl_sorcier_canalisation_eclat` |
| Importeur du catalogue (`JeuBuilder`) | reconnaître `_boucle` comme `_loop` |
