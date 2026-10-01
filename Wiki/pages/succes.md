# Succès

Proposition du 01/10/2026, à trier par Quentin : rien n'est encore décidé ni codé {à confirmer}. Les succès servent à la fois en jeu (écran « Succès » et petite bannière au déblocage) et, plus tard, sur **Steam**. Ils sont donc pensés dès maintenant avec les contraintes de Steam : un identifiant stable par succès, des statistiques pour les succès à progression, des succès cachés pour les surprises.

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

{{dev: Notes techniques pour plus tard, rien n'est fait.}}

- **Identifiants** : chaque succès a un identifiant `ACH_…` (colonne ci-dessus) déclaré à l'identique dans Steamworks (partenaire Steam, « Stats & Achievements »). Il ne change plus après la publication ; le nom et la description affichés sont traduisibles.
- **Statistiques** : les succès à compteur (`ACH_TETES_1000`, `ACH_COFFRES_50`…) s'appuient sur une **statistique Steam** entière (`STAT_…`) ; Steam affiche alors la progression (« 412 / 1000 »). Pousser la stat périodiquement (fin de nuit, retour au menu), pas à chaque tir.
- **Icônes** : deux icônes 64 × 64 par succès (débloqué en couleur, verrouillé en gris), dans le style des icônes du jeu (`ArtSources/Icones/`) ; un succès caché montre une icône et un texte génériques tant qu'il n'est pas débloqué.
- **Multijoueur** : l'hôte fait autorité sur les événements (boss tué, nuit tenue) et envoie aux clients un message « succès d'équipe débloqué » ; chaque poste débloque ensuite **ses propres** succès auprès de Steam (Steam n'accepte un succès que du compte qui joue). Les succès personnels sont comptés sur le poste du joueur.
- **Hors ligne** : le jeu garde les succès et les stats en local (comme les options) et les renvoie à Steam à la connexion suivante ; Steam lui-même met en cache les déblocages hors ligne.
- **Code** : un module `Deathless.Succes` (catalogue en ScriptableObject : identifiant, nom, description, caché, stat et seuil), qui écoute les événements du jeu déjà présents (`Partie` : nuit tenue, mort, boss ; dégâts et critiques ; donjon : coffres, sacs ; statuts : brûlure ; emotes) et passe par une interface de plateforme (`ISuccesPlateforme`) : **locale** dès maintenant, **Steam** plus tard (Steamworks.NET), sans toucher au reste. Les scénarios de dev et le mode test ne débloquent rien.
- **Écran en jeu** : un onglet « Succès » (menu principal et pause) avec la liste, la progression et la date de déblocage ; une petite bannière en haut à droite au déblocage (même style que les messages du HUD).
