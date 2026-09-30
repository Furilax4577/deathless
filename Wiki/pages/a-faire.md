# À faire

Travaux prévus ou notés, pas encore faits. Une ligne quitte cette page quand le travail est livré. Les règles encore à trancher sont dans [À décider](a-decider.md).

## Noté par Quentin

- **Cap : sortir de KayKit** {décidé} (26/09/2026) : à terme, remplacer tout ce qui vient de KayKit par des assets propres à Deathless, pour se différencier et avoir plus de liberté. Premières étapes : arbres, sol, bâtiments, personnages générés. Les animations (squelette Rig_Medium) viendront en dernier. Méthode retenue : **étudier KayKit d'abord** (guide chiffré `Docs/style-kaykit.md`, 26/09/2026), puis refaire chaque famille au niveau.

{jauge-kaykit}

- **Barres de vie des ennemis** {décidé} (26/09/2026, règles dans [Interface](interface.md#barres-de-vie-des-ennemis)) : barre fine sous les statuts des ennemis blessés (toujours visible sur un élite), méga barre du boss sous celle de Nyxessa ; **chiffres de dégâts** flottants et option « Afficher les dégâts » (onglet Jeu, `OptionsJoueur`), même page du wiki. Maquette dans `sandbox-ui` d'abord, capture à valider par Quentin, puis intégration au HUD et au jeu.
- **Maisons générées : modèle standard refait et personnalisation** (retours de Quentin du 27/09/2026, règles dans [Village](village.md#taille-des-maisons)) : socle à une assise, murs 7,6 × 6 m, façade à porte décalée et deux fenêtres, fiche de paramètres (teintes, fenêtres, options). Éléments distinctifs, deux ou trois par maison, validés par Quentin le 27/09/2026 {décidé} ; à faire une par une après le standard, capture à chaque fois :
  - **Taverne** : enseigne suspendue (chope) sur potence en fer forgé ; auvent au-dessus d'une porte à deux vantaux, deux tonneaux et un banc dessous ; cheminée plus massive qui fume, fenêtres plus larges et plus chaudes la nuit.
  - **Forgeron** : appentis ouvert sur le côté avec le foyer extérieur et sa cheminée de pierre massive (lueur et fumée jour et nuit) ; tas de bûches et outils accrochés sous l'appentis ; fenêtres à barreaux sans volets, dalles noircies devant.
  - **Mécano** : vitrine à petits carreaux avec armes exposées ; enseigne engrenage et clé ; tuyau de poêle en métal coudé à la place de la cheminée, roue dentée ou girouette mécanique sur le faîte, caisses devant.
  - **Druide** : lierre et fleurs sur les colombages, jardinières sous les fenêtres ; séchoir à herbes sous un auvent ; enseigne fiole, toit un peu plus pentu couvert de mousse.
  - **Sorcier** : pignon rehaussé (ou petite tourelle d'angle) avec fenêtre ronde qui luit la nuit ; toit en ardoise bleu sombre ; croissant de lune en girouette et symboles peints sur la porte.
  - **Maison de décor** : potager clôturé, linge qui sèche, puits ou tas de bois.
- **Refonte de la carte, maquette grise** {décidé} (27/09/2026, règles dans [Village](village.md#refonte-de-la-carte)) : générateur du village en v5 dans `sandbox-level`, volumes gris seulement (falaise et pierrier au nord, cascade, rivière qui longe le plateau, deux ponts et un gué, forêt réduite au sud et à l'ouest, lande à l'est, trois clairières est / sud / ouest), circulation revérifiée (vagues jusqu'aux ponts et au gué), captures depuis le menu, Nyxessa et chaque pont. **Faite et validée le 27/09/2026.** **Habillage complet fait le 27/09/2026** (`sandbox-level`, `VillageV5Habillage`, captures `v5f_*.png`) : décor naturel généré, six maisons personnalisées (options de `MaisonStyleBuilder` construites), relique et portail (place à r = 18 m), dalles facettées, nuit (cycle, lanternes générées, fenêtres émissives, brume, lucioles, nuages), estompage des couronnes ; 438 000 triangles. Reste : **report dans `main`** (générateur, NavMesh et vagues sur trois clairières, portail, intérieurs des maisons personnalisées, sons de la cascade et de la rivière) = version 0.6.0.
- **Bouclier de Nyxessa vu de l'intérieur** {décidé} (27/09/2026) : un joueur qui se tient dans le bouclier doit voir dehors avec aisance ; la paroi s'éclaircit (moins dense, plus transparente) quand la caméra est à l'intérieur, sans changer sa lecture depuis l'extérieur. {{dev: Fait le 27/09/2026 : `RelicShieldVisual`, lecture depuis l'intérieur (page Nyxessa, section Bouclier ; fiche `Docs/vfx.md`).}}
- **Lissage global des compétences** (27/09/2026, demande de Quentin : « les cinq personnages sont déséquilibrés ») : audit chiffré des cinq classes (rôle, dégâts par seconde, survie, utilité en groupe, dépendance à l'équipe) et propositions de refonte, même profondes, dans `Docs/equilibrage-classes.md` puis [À décider](a-decider.md) ; Quentin tranche classe par classe avant toute implémentation.
- **Bande-annonce Steam** : v1 tournée le 26/09/2026 (`Docs/trailer-storyboard.md`, outil `Assets/Scripts/Dev/Tournage/`), 39 s, à **écouter et valider** par Quentin ; à retourner quand le décor aura quitté KayKit.

## Retours du test multi 0.7.0 (30/09/2026)

Notés par Quentin après la partie à deux ; rien n'est encore développé.

- **Boss de fin de nuit** {décidé} : Morgrim (nuit 10) et Nyxar (nuit 12) sortent avec la dernière vague, si tard que le soleil se levait sans combat. Désormais le boss arrive en fin de vague et **le jour ne se lève que quand il est mort** : l'aube attend sa chute. Pages à reprendre au développement : [Déroulé d'une partie](deroule.md) et [Ennemis](ennemis.md).
- **Bug : tirs à travers le bouclier** : les ennemis à distance (mage squelette, crânes) atteignent Nyxessa et les joueurs qui se tiennent dans l'enceinte du bouclier, à travers la paroi.
- **Bug : ennemis qui passent le bouclier** : en prenant pour cible un joueur posté au bord du bouclier, des squelettes sont entrés dans l'enceinte.
- **Brûlure qui se cumule en paliers** {décidé} : chaque tic de flamme remplit la jauge de brûlure de l'ennemi ; au-delà de 100 %, la brûlure monte d'un palier (plus forte) et la jauge repart ; elle redescend de palier en palier de la même façon quand on cesse de brûler l'ennemi. Aujourd'hui la brûlure ne se cumule pas (la durée repart de zéro, voir [Statuts](statuts.md)). Chiffres dans [À décider](a-decider.md).
- **Points faibles des boss cachés sur le wiki** {décidé} : les points faibles et les phases des boss (éclats de Nyxar, etc.) sont masqués par défaut dans le wiki joueur, derrière un avertissement « Attention, spoil » à déplier.
- **Menu du personnage (Tab)** : Tab doit aussi le fermer (bascule), pas seulement l'ouvrir. {{dev: Fait le 30/09/2026 : Tab / Y / Triangle ferme le menu quand il est déjà au sommet.}}
- **Chemins des monstres trop semblables** : les squelettes empruntent tous le même chemin, ce qui rend les attaques de zone trop faciles ; varier les trajets (dispersion, plusieurs couloirs, écarts dans la file).
- **Mage : coup critique ?** et **Rôdeur : petit nerf** (critique ou dégâts de base) : à trancher, voir [À décider](a-decider.md).

## En attente de validation (Quentin)

- **Personnage du mois** : candidats Barde, Bavaroise, Clochard pétomane et **DJ Bob Douville** (26/09/2026, modèles lissés, pages avec rendus et clips). Choisir le premier ; alléger celui qui est retenu (Barde et Bavaroise un peu au-dessus de 8 000 triangles). Barde : l'attaque de base frappe avec le luth comme une massue, animation de jeu du luth à créer.
- **Clochard pétomane** : pet de défense provisoire (roulade avant et grosse bouffée), nuage à 2,5 s dans la vidéo au lieu de 5 s, geste pour boire à retoucher.
- **Effets des nouvelles classes** (bac à sable des effets) : nuage pestilentiel un peu opaque, flaque de la tournée en mosaïque, Trinquer discret.
- **Maison générée selon le guide de style** (`sandbox-level`, scène `StyleKayKit`) : 11 280 triangles contre 1 000 à 1 400 pour KayKit ; leviers d'allègement notés dans le guide. Si validée : arbre et sol avec le même guide, puis intégration au village (code des intérieurs agrandis et de la porte qui claque mis de côté dans git, tag `parc-maisons-generees`).
- **Arbres** : générés et **validés par Quentin le 27/09/2026** (`sandbox-level`, `Assets/StyleKayKit/Editor/ArbreStyleBuilder.cs`, fiches `ArbreParams` : sapin, pin, genévrier, chêne, érable, hêtre, deux morts, souches, rochers ; 178 à 532 triangles ; captures `style_arbres_*.png`). Reste : intégration au village avec la v5 (prefabs posés par le générateur, colliders de tronc, `FeuillageMasquage` sur la couronne, variation de teinte par instance) ; arbres morts à épaissir et tordre.
- **Pins** : trois silhouettes (élancé 5,8 m, parasol 5,0 m, battu par le vent 4,6 m) construites d'après les planches Grok et **validées par Quentin le 28/09/2026** (`ArbreStyleBuilderPins`, prefabs `Arbre_Pin*`, captures `style_pins_*.png`). Reste : décider s'ils remplacent les six essences ou s'y ajoutent, puis les poser dans le village v5.
- **Taverne** : volume à l'échelle, extérieur et intérieur **validés par Quentin le 28/09/2026** (`sandbox-level`, scène `Taverne`, générateurs `TaverneVolumeBuilder`, `TaverneExterieurBuilder`, `TaverneInterieurBuilder` ; plan `Docs/da/taverne-plan.md`). Allégée à 34 549 triangles, prefab prêt (`Assets/StyleKayKit/Taverne/Taverne.prefab`). Reste : pose dans le village v5, vrais personnages (Bavaroise, barde, clochard), musique du barde, menu de la tavernière, caméra dos au mur (elle se colle au joueur), puis les cinq autres bâtiments sur la même méthode (volume gris, extérieur, intérieur).
- **Sol** (preuve de concept dans `sandbox-ui`) : jugé pas assez KayKit, à refaire avec le guide, après les arbres.
- **Maisons la nuit** : entrer seulement le jour ? Reconduire dehors au crépuscule ? (voir [À décider](a-decider.md)).
- **Renversé** : pas d'invulnérabilité pendant la chute (choix par défaut, à confirmer).

## Avant la sortie (performance)

- **Mesurer au profileur** ce que la caméra rend vraiment (triangles, objets, temps par image) sur deux vues : la place de nuit avec les vagues, et la forêt en pleine course. Cible : rester sous environ 1,5 million de triangles rendus par image et 60 images par seconde sur une carte d'entrée de gamme.
- **Ombres** : régler distance et résolution des ombres (les arbres hors champ qui projettent une ombre sont rendus une seconde fois ; c'est souvent le premier poste).
- **Occlusion culling** : à activer seulement là où ça rapporte (intérieurs des maisons, village si la mesure le montre) ; pas pour le donjon, généré à chaque jour donc impossible à cuire ; y étendre plutôt le masquage des étages aux salles hors de vue. Noté par Quentin, 26/09/2026.
- À refaire après l'intégration de la nouvelle forêt (arbres plus riches, moins nombreux).

## Plus tard

- Forêt : arbres plus riches et moins nombreux, troncs dégagés jusqu'à 1,5 fois la hauteur des personnages ; **les squelettes traversent la forêt entre les troncs** au lieu de toujours suivre le même chemin (variété des trajets : points de passage au hasard ou coût de chemin bruité). Idée de Quentin, 26/09/2026.
- Donjon, à équilibrer : montants d'or (120 / 50 / 20, +10 % par nuit), nombre de gardiens (6, dont 35 % de guerriers), ralentissement dans l'eau (×0,6), délai d'alerte (15 s), parts gardées au rappel (0 à 75 %).
- Parade parfaite : la gerbe de gemmes envoyée devant se voit peu depuis la caméra ; chemin à deux joueurs pas testé.
- À tester à deux joueurs : emotes, charge bélier, statuts, esquive directionnelle, parade parfaite, onde de Morgrim (Fracas et Coup écrasé), éclats et téléportation de Nyxar, retour dans une partie par le **code** (Relay) après une vraie coupure.
- Taverne : breuvages (plus tard).
- Réseau : penché du buste en visée ; après une coupure, l'hôte qui continue seul ne peut pas rouvrir sa partie aux autres.
- Mettre à jour la copie de `CycleJourNuit` du bac à sable du village.
- Wiki : canne à pêche provisoire, faute de modèle KayKit.
