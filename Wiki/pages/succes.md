# Succès

Proposition du 01/10/2026, à trier par Quentin {à confirmer}. {{dev: Codée telle quelle le 01/10/2026 à la demande de Quentin (module `Deathless.Succes`, voir la section Implémentation plus bas) : renommer, retirer ou changer un seuil ne demande qu'une ligne du catalogue.}} Les succès servent à la fois en jeu (écran « Succès » et petite bannière au déblocage) et, plus tard, sur **Steam**. Ils sont donc pensés dès maintenant avec les contraintes de Steam : un identifiant stable par succès, des statistiques pour les succès à progression, des succès cachés pour les surprises.

## Principes {à confirmer}

- **Pour tout le monde, pas seulement les experts** : un tiers de succès que l'on obtient en jouant normalement (première nuit, premier coffre), un tiers qui demandent de bien jouer une classe, un tiers d'exploits et de blagues.
- **Coopératif d'abord** : dans une partie à plusieurs, un succès d'équipe (« tenir jusqu'à l'aube », « vaincre Nyxar ») est donné à **tous les joueurs présents** à ce moment, y compris celui qui est mort ou revenu après une coupure. Un succès personnel (« 1 000 tirs à la tête ») ne compte que les actions du joueur.
- **Aucun succès ne pousse à saboter** l'équipe ou à rester AFK (pas de « mourir 100 fois », pas de « finir sans rien faire »).
- **Cachés** : les succès qui révèlent une surprise (boss, points faibles de Nyxar, blagues) sont cachés jusqu'au déblocage, comme le spoil du wiki.
- **Tous les modes comptent**, solo et multijoueur, sans triche ni console de dev (les scénarios de test ne débloquent rien).

## Liste proposée {à confirmer}

Identifiant Steam entre crochets : il ne change plus une fois publié (le nom affiché, lui, peut changer).

### Premiers pas

| Succès | Condition | Identifiant |
|---|---|---|
| **Première aube** | Survivre à la première nuit. | `ACH_NUIT_1` |
| **Ça sent le sapin** | Mourir pour la première fois (on revient tant que Nyxessa tient). | `ACH_PREMIERE_MORT` |
| **Pilleur novice** | Ouvrir son premier coffre au donjon. | `ACH_PREMIER_COFFRE` |
| **Mécène de la relique** | Acheter un premier palier de Nyxessa (missiles ou bouclier). | `ACH_PREMIER_PALIER` |
| **Bien entouré** | Jouer une partie à 4 joueurs. | `ACH_QUATUOR` |
| **Touche-à-tout** | Finir une nuit avec chacune des cinq classes. | `ACH_CINQ_CLASSES` |

### Tenir la nuit

| Succès | Condition | Identifiant |
|---|---|---|
| **Mi-chemin** | Atteindre la nuit 6. | `ACH_NUIT_6` |
| **Le Roi des os est tombé** (caché) | Vaincre Morgrim. | `ACH_MORGRIM` |
| **Deathless** (caché) | Vaincre Nyxar et voir l'aube de la nuit 12. | `ACH_VICTOIRE` |
| **Sans une égratignure** | Gagner une partie sans que Nyxessa ne perde un point de vie à la nuit 12. | `ACH_NYXESSA_INTACTE` |
| **Le bouclier tient** | Finir une nuit sans que le bouclier ne soit brisé, à partir de la nuit 8. | `ACH_BOUCLIER_TIENT` |
| **Personne ne tombe** | Gagner une partie à 2 joueurs ou plus sans aucune mort. | `ACH_DEATHLESS_EQUIPE` |
| **Les deux visages** | Vaincre Morgrim à la massue et Morgrim à la martache (deux parties). | `ACH_MORGRIM_DEUX` |

### Nyxar et Morgrim (cachés)

| Succès | Condition | Identifiant |
|---|---|---|
| **Couronne brisée** | Briser l'éclat de la couronne de Nyxar. | `ACH_ECLAT_COURONNE` |
| **Grimoire fermé** | Briser l'éclat du grimoire de Nyxar. | `ACH_ECLAT_GRIMOIRE` |
| **Ordre inverse** | Briser le grimoire avant la couronne. | `ACH_ECLATS_INVERSE` |
| **Pas le temps de crier** | Tuer Morgrim avant qu'il ait poussé son cri. | `ACH_MORGRIM_MUET` |
| **L'aube attendra** | Que la nuit se prolonge plus d'une minute avant la chute du boss. | `ACH_NUIT_LONGUE` |

### Classes

| Succès | Condition | Identifiant |
|---|---|---|
| **Mur de fer** (Paladin) | Réussir 100 parades parfaites (cumul). | `ACH_PARADES_100` (stat `STAT_PARADES_PARFAITES`) |
| **L'épée et le baume** (Paladin) | Soigner 3 alliés d'un seul soin d'aura. | `ACH_SOIN_TRIPLE` |
| **Berserk** (Viking) | Toucher 10 ennemis d'une seule attaque tournante. | `ACH_TOURNANTE_10` |
| **Peau de fer** (Viking) | Encaisser 500 dégâts sous Peau de fer (cumul). | `ACH_PEAU_DE_FER` |
| **Feu de joie** (Mage) | Porter 8 ennemis au palier 3 de brûlure en même temps. | `ACH_BRASIER_8` |
| **Vous ne passerez pas** (Mage) | Faire traverser son mur de flammes à 20 ennemis en une seule pose. | `ACH_MUR_20` |
| **Grande boule, grand ménage** (Mage) | Tuer 6 ennemis d'une seule grande boule de feu. | `ACH_GRANDE_BOULE_6` |
| **Dans le mille** (Rôdeur) | 1 000 tirs à la tête (cumul). | `ACH_TETES_1000` (stat `STAT_TIRS_TETE`) |
| **Cloué sur place** (Rôdeur) | Étourdir un élite avec une flèche à pleine charge puis le tuer avant qu'il ne se reprenne. | `ACH_CLOUE` |
| **Personne ne m'a vu** (Assassin) | Tuer 5 ennemis à la suite sans quitter le mode furtif. | `ACH_FURTIF_5` |
| **Enchaînement** (Assassin) | Enchaîner 4 exécutions en moins de 10 s grâce au Pas de l'ombre rechargé. | `ACH_EXECUTIONS_4` |
| **Dans le dos, toujours** (Assassin) | 500 coups dans le dos (cumul). | `ACH_DOS_500` (stat `STAT_COUPS_DOS`) |

### Village, donjon et taverne

| Succès | Condition | Identifiant |
|---|---|---|
| **Rat de donjon** | Ouvrir 50 coffres (cumul). | `ACH_COFFRES_50` (stat `STAT_COFFRES`) |
| **Juste à temps** | Repasser le portail du donjon moins de 3 s avant sa fermeture. | `ACH_JUSTE_A_TEMPS` |
| **Ce qui est à toi est à moi** | Ramasser le sac d'or d'un allié mort au donjon. | `ACH_SAC_ALLIE` |
| **Caisse pleine** | Avoir 1 000 or dans la caisse commune. | `ACH_CAISSE_1000` |
| **La tournée du patron** (caché) | Boire à la taverne jusqu'à l'ivresse maximale. | `ACH_IVRESSE` |
| **Danse de la victoire** | Faire une emote sur le cadavre encore chaud de Morgrim. | `ACH_EMOTE_MORGRIM` |

### Pour rire (cachés)

| Succès | Condition | Identifiant |
|---|---|---|
| **Je reviens tout de suite** | Revenir dans une partie en cours après une coupure (par le même code). | `ACH_RECONNEXION` |
| **Seul contre tous** | Finir une nuit seul après la perte de connexion de l'hôte… qui était soi. | `ACH_HOTE_SEUL` |
| **Tir ami… presque** | Être sauvé par Nyxessa : un missile tue l'ennemi qui allait vous achever (vous à moins de 10 % de vie). | `ACH_SAUVE_PAR_NYX` |
| **Le voleur volé** | Tuer un voleur squelette qui chassait un joueur isolé, en étant ce joueur isolé. | `ACH_VOLEUR_VOLE` |
| **Les pieds dans l'eau** | Rester 60 s d'affilée dans l'eau du bassin du donjon. | `ACH_BASSIN` |

## Intégration Steam {à confirmer}

{{dev: Le module local est codé (01/10/2026, section suivante) ; la plateforme Steam reste à écrire (`ISuccesPlateforme` avec Steamworks.NET).}}

- **Identifiants** : chaque succès a un identifiant `ACH_…` (colonne ci-dessus) déclaré à l'identique dans Steamworks (partenaire Steam, « Stats & Achievements »). Il ne change plus après la publication ; le nom et la description affichés sont traduisibles.
- **Statistiques** : les succès à compteur (`ACH_TETES_1000`, `ACH_COFFRES_50`…) s'appuient sur une **statistique Steam** entière (`STAT_…`) ; Steam affiche alors la progression (« 412 / 1000 »). Pousser la stat périodiquement (fin de nuit, retour au menu), pas à chaque tir.
- **Icônes** : deux icônes 64 × 64 par succès (débloqué en couleur, verrouillé en gris), dans le style des icônes du jeu (`ArtSources/Icones/`) ; un succès caché montre une icône et un texte génériques tant qu'il n'est pas débloqué.
- **Multijoueur** : l'hôte fait autorité sur les événements (boss tué, nuit tenue) et envoie aux clients un message « succès d'équipe débloqué » ; chaque poste débloque ensuite **ses propres** succès auprès de Steam (Steam n'accepte un succès que du compte qui joue). Les succès personnels sont comptés sur le poste du joueur.
- **Hors ligne** : le jeu garde les succès et les stats en local (comme les options) et les renvoie à Steam à la connexion suivante ; Steam lui-même met en cache les déblocages hors ligne.
- **Code** : un module `Deathless.Succes` (catalogue en ScriptableObject : identifiant, nom, description, caché, stat et seuil), qui écoute les événements du jeu déjà présents (`Partie` : nuit tenue, mort, boss ; dégâts et critiques ; donjon : coffres, sacs ; statuts : brûlure ; emotes) et passe par une interface de plateforme (`ISuccesPlateforme`) : **locale** dès maintenant, **Steam** plus tard (Steamworks.NET), sans toucher au reste. Les scénarios de dev et le mode test ne débloquent rien.
- **Écran en jeu** : un onglet « Succès » (menu principal et pause) avec la liste, la progression et la date de déblocage ; une petite bannière en haut à droite au déblocage (même style que les messages du HUD).

## Implémentation {dev}

Codé le 01/10/2026. Le jeu signale ses faits par des appels d'une ligne ; le module décide et garde l'état.

- **Fichiers** : `Assets/Scripts/Succes/` : `CatalogueSucces.cs` (table en code des 41 succès : identifiant, nom, description, rubrique, caché, équipe, statistique et seuil), `ISuccesPlateforme.cs` (interface : `Debloquer`, `EstDebloque` avec la date, `LireStat`, `ReglerStat`, `Stocker`, `ToutEffacer` ; implémentation `SuccesLocal`), `ServiceSucces.cs` (API appelée par le jeu, routage réseau, garde-fou), `SuiviSucces.cs` (abonnements à `Partie` et sondes). Interface : `Assets/Scripts/UI/Donnees/ISucces.cs` (`DonneesUI.Succes`), `Assets/Scripts/UI/Ecrans/EcranSucces.cs` (écran et `BanniereSucces`), `Assets/UI/Resources/Succes/Succes.uxml` et `Succes.uss`, icône générique `Assets/UI/Icones/Succes/succes_generique.svg`.
- **Sauvegarde locale** : `succes.json` dans `Application.persistentDataPath` (`succes.<profil>.json` sous `-deathless-profil=…`, comme les réglages du lobby) ; écrite à chaque déblocage, à chaque aube, au changement de scène, à la sortie et toutes les 30 s si une statistique a changé.
- **Statistiques** (entiers, prêtes pour Steam) : `STAT_PARADES_PARFAITES`, `STAT_TIRS_TETE`, `STAT_COUPS_DOS` (wiki) ; ajoutées : `STAT_COFFRES` (sert aussi à *Pilleur novice*, seuil 1), `STAT_PEAU_DE_FER` (dégâts encaissés), `STAT_CLASSES_NUIT` et `STAT_MORGRIM_VERSIONS` (champs de bits : classes avec lesquelles une nuit a été tenue, versions de Morgrim vaincues ; la progression compte les bits). À déclarer telles quelles dans Steamworks.
- **Multijoueur** : succès d'équipe (nuits, boss, éclats, Morgrim muet, nuit longue, quatuor, caisse, victoire et ses variantes) décidés par l'hôte et envoyés à tous les postes présents (`PartieReseau.SuccesEquipe`, RPC aux clients ; chaque poste débloque pour lui). Faits personnels vus par l'hôte seul (coffre accordé, sac ramassé, soin d'aura, voleur tué, missile qui sauve) envoyés au poste du joueur (`PartieReseau.FaitPersonnel`). Le reste est compté sur le poste du joueur (ses coups, sa mort, ses emotes, son ivresse, l'eau).
- **Garde-fou** : rien n'est compté pendant les scénarios de dev, le banc des classes et le tournage (chaque classe de `Assets/Scripts/Jeu/Dev/` et `Assets/Scripts/Dev/Tournage/` suspend les succès à sa première utilisation, jusqu'à la fin du Play) ni dans une partie de test (réglages de développement de `GameBalance` : invincibilités, lancement direct, départ à la nuit, nuit de départ autre que 1, cycle accéléré ; `Partie.ForcerPhase` / `ForcerFin`). Pour vérifier les succès eux-mêmes : `ServiceSucces.AutoriserTests = true` en Play (jamais posé par le jeu).
- **Interface** : entrée « Succès » au menu principal et à la pause ; liste par rubrique, compteurs avec barre, date de déblocage, cachés « ??? » tant qu'ils sont verrouillés ; bannière « Succès débloqué » en haut à droite, sous l'or (4 s, file d'attente, au-dessus de tous les écrans). **Une icône générique pour tous** : une icône par succès (couleur et gris, 64 × 64 pour Steam) reste à dessiner.

| Succès | Déclencheur codé |
|---|---|
| Première aube, Touche-à-tout | Aube après une nuit (`SuiviSucces.OnPhase`, hôte) : fait « NUIT » diffusé ; chaque poste ajoute sa classe. |
| Ça sent le sapin | `Partie.JoueurMort` du joueur local. |
| Pilleur novice, Rat de donjon | `DonjonJeu.Accorder` (coffre ou grand coffre, pas les tas d'or) : `STAT_COFFRES` du joueur. |
| Mécène de la relique | `Partie.PalierAchete`, acheteur = pseudo du joueur local. |
| Bien entouré | Hôte : 4 joueurs dans la partie en cours. |
| Mi-chemin | Début de la nuit 6 ou plus. |
| Le Roi des os est tombé, Les deux visages | `Squelette.OnTue` d'un Golem : fait « MORGRIM » avec sa version (massue, martache) et la place du cadavre. |
| Deathless, Sans une égratignure, Personne ne tombe | Fin de partie en victoire ; Nyxessa sans dégâts réels depuis le début de la nuit 12 ; aucune mort et au moins 2 joueurs. |
| Le bouclier tient | Aube d'une nuit 8 ou plus où le bouclier a été levé et jamais brisé (`BouclierNyxessa.Brise`). |
| Couronne brisée, Grimoire fermé, Ordre inverse | `EclatNyx.OnTue` (hôte) ; inverse : grimoire brisé avant la couronne. |
| Pas le temps de crier | Morgrim tué sans avoir crié (`MorgrimVariant.ACrie`). |
| L'aube attendra | Boss tué après plus de 60 s d'aube retenue (`EtatPartie.aubeRetenue`). |
| Mur de fer | `ParadeParfaite.Commencer` (paladin local). |
| L'épée et le baume | `ClassePaladin.SoignerAllies` : au moins 3 alliés soignés (hôte, envoyé au paladin). |
| Berserk | Ennemis différents touchés par une même tournante (`ClasseViking`). |
| Peau de fer | Dégâts réels reçus par le héros local sous le statut Peau de fer (`Sante.AnyTouche`). |
| Feu de joie | Sonde : 8 ennemis vivants avec une brûlure du mage local au palier 3 ou plus. |
| Vous ne passerez pas | Ennemis différents entrés dans un même mur de flammes (`ClasseMage.Brasier`). |
| Grande boule, grand ménage | Ennemis achevés par une même explosion de grande boule. |
| Dans le mille | Flèche du rôdeur à la tête (`ClasseRodeur.TirerFleche`) ; les carreaux de l'assassin ne comptent pas. |
| Cloué sur place | Élite étourdi par une flèche à pleine charge puis achevé par une flèche du même rôdeur avant la fin de l'étourdissement. |
| Personne ne m'a vu | 5 ennemis achevés à la suite par des coups de dague portés en mode furtif ; un coup hors furtif, être repéré ou touché remet la série à zéro. |
| Enchaînement | 4 exécutions de dague en moins de 10 s. |
| Dans le dos, toujours | Coups de dague dans le dos. |
| Juste à temps | Portail de retour passé (`DonjonJeu.Passer`) moins de 3 s avant le crépuscule. |
| Ce qui est à toi est à moi | `DonjonJeu.RamasserSac` d'un sac d'un autre pseudo (hôte, envoyé au joueur). |
| Caisse pleine | Hôte : caisse commune à 1 000 or ou plus. |
| La tournée du patron | Interprétation : trois verres (bière ou tournée) de suite sans dessoûler (`Ivresse.Commencer`) ; l'ivresse n'a pas de niveau maximal dans le jeu, à confirmer par Quentin. |
| Danse de la victoire | Emote lancée à 6 m au plus du cadavre de Morgrim, dans les 30 s qui suivent sa chute (`EmotesHeros.Lancer`). |
| Je reviens tout de suite | `Partie.LancerReseau` : retour reconnu (même code, même classe). |
| Seul contre tous | `Partie.ContinuerSeul` puis une aube tenue sans réseau. |
| Tir ami… presque | Missile de Nyxessa qui tue un squelette en préparation de coup (ou à moins de 3,5 m) contre un joueur à moins de 10 % de vie (hôte, envoyé au joueur). |
| Le voleur volé | Voleur tué par le joueur isolé qu'il a pris en chasse (`Voleur.Proie`). |
| Les pieds dans l'eau | Sonde : héros local 60 s d'affilée dans un `ZoneEau` du donjon. |

Limites connues : chez un client, « achevé » (grande boule, série furtive, Cloué sur place) est estimé sur la vie vue par ce poste (le coup porte au moins les PV restants), l'hôte faisant foi sur la mort ; la parade parfaite est comptée au lancement chez le client, même si l'hôte la refuse ensuite (rare). Tous les succès de la liste ont un déclencheur : aucun n'est resté sans détection.
