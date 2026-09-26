# Réseau (multijoueur)

Jusqu'à 4 joueurs. Un joueur **héberge** (hôte = serveur + joueur), les autres le **rejoignent**. En solo, le réseau ne démarre pas : le jeu tourne exactement comme avant (aucun `NetworkManager` à l'écoute, `ReseauJeu.Autorite` vrai).

État au 25/09/2026 : **étape 1** faite (lobby réel, lancement, héros de chacun, déplacements et animations vus des autres, pseudos, colonne des alliés) ; **étape 2** faite (monde tenu par l'hôte : squelettes, vagues, dégâts, Nyxessa et missiles, cycle jour / nuit et vues qui en dépendent, morts et réapparitions, vote du jour, écran de score multijoueur, sorcier et bouclier, or ; +60 % d'ennemis par joueur en plus ; tirs des autres joueurs visibles).

## Paquets et services

| Paquet | Version | Rôle |
|---|---|---|
| `com.unity.netcode.gameobjects` | 2.13.3 | objets réseau, variables, RPC, chargement de scène |
| `com.unity.transport` | 2.7.4 | transport UDP (Unity Transport) |
| `com.unity.services.multiplayer` | 2.3.3 | sessions : Lobby (code court) + Relay (pas de port à ouvrir) |

Unity Cloud : projet `ea41a097-e6da-4128-819b-672af32643ab`, **Relay et Lobby activés**. Identification : joueur anonyme (`SignInAnonymouslyAsync`) au premier usage du multijoueur ; aucune saisie. Deux postes sur la même machine doivent utiliser deux **profils** différents (`-deathless-profil=nom`), sinon ils sont le même joueur anonyme.

## Rejoindre une partie

- **Créer un salon** : session privée Multiplayer Services (`CreateSessionAsync`, 4 places, `WithRelayNetwork()`). Le **code court** (6 caractères) s'affiche à l'hôte dans le lobby (Afficher, Copier). Aucun port à ouvrir : tout passe par Relay.
- **Rejoindre par code** : `JoinSessionByCodeAsync(code)`.
- **Rejoindre par adresse IP** (secours, réseau local ou redirection de port) : Unity Transport direct, **port UDP 7777** (`ReseauJeu.PortDirect`). Saisie `192.168.0.34` ou `192.168.0.34:7777`.
- **Secours automatique** : si les services en ligne ne répondent pas à la création, l'hôte ouvre un salon direct sur le port 7777 ; son « code » est alors son adresse locale (`192.168.0.34:7777`) et le lobby l'annonce (« Services en ligne indisponibles : salon local, à rejoindre par adresse IP »).
- Refus à la connexion (message affiché au client) : salon complet (4), partie déjà commencée.

Pare-feu : Relay n'en demande rien. En IP directe, l'hôte doit accepter l'UDP entrant sur 7777 (Windows demande l'autorisation au premier lancement du jeu qui écoute).

## Architecture

```
Scripts/Reseau/
  ReseauJeu.cs        module réseau (DontDestroyOnLoad) : NetworkManager + UnityTransport + LobbyReseau,
                      approbation des connexions, départs, lancement (rechargement du village par Netcode)
  LobbyReseau.cs      ILobby réel (Deathless.UI.Donnees) : services, créer / rejoindre (code, IP), quitter,
                      déconnexion ; enregistré dans DonneesUI.Lobby (remplace LobbyFactice, écran inchangé)
  SalonReseau.cs      état du salon, possédé par l'hôte (NetworkList<JoueurSalon> : clientId, pseudo, classe, prêt ;
                      compte à rebours ; Lance) ; les clients ne font que des demandes (RPC) que l'hôte valide
  HerosReseau.cs      côté réseau d'un héros : pseudo et classe (posés par l'hôte), vie, mort et réapparition
                      (écrits par le propriétaire) ; implémente IAllie (HUD)
  ClientAutomatique.cs  poste piloté par la ligne de commande (tests sans fenêtre)
Resources/Reseau/SalonReseau.prefab   (NetworkObject + SalonReseau)
```

- `ReseauJeu.Assurer()` est appelé par `Partie.Awake` : l'objet `Reseau` naît au premier chargement du village et survit aux rechargements. Préfabs réseau enregistrés au démarrage : le salon et les 5 héros (registre `ClassesJeu`).
- **Salon** : l'hôte fait apparaître `SalonReseau` dès que le serveur écoute. Chaque poste s'y présente (`PresenterRpc` : pseudo du profil, dernière classe jouée). **Classe unique, premier arrivé premier servi** : l'hôte attribue la classe demandée si elle est libre, sinon la première classe libre ; un changement de classe remet « pas prêt ». Prêt seulement avec une classe. Tous prêts → compte à rebours de 3 s (annulé si quelqu'un n'est plus prêt) ; « Lancer » de l'hôte le démarre aussi. Un départ retire le joueur et **libère sa classe**.
- **Lancement** : l'hôte recharge le village pour tous (`NetworkManager.SceneManager.LoadScene`, mode Single). Quand tous l'ont chargé, il fait apparaître un héros par joueur, **de sa classe**, côte à côte au point de départ près de Nyxessa (`Partie.ApparaitreHerosReseau`, `SpawnAsPlayerObject`). Sur chaque poste, le héros dont il est propriétaire lance la partie locale (`Partie.LancerReseau`) ; les autres deviennent des **marionnettes** (`Heros.Distant` : ni entrées, ni caméra, ni logique de classe, ni déplacement).
- **Autorité (étape 1)** : chaque poste simule **son** héros comme en solo ; sa position part de lui (`NetworkTransform`, autorité du propriétaire, lacet seul, interpolé) et ses animations aussi (`NetworkAnimator`, autorité du propriétaire : paramètres, poids des couches, déclencheurs). Les déclencheurs passent par `Heros.Declencher` (jamais `animator.SetTrigger` directement) pour être joués chez tous. Le monde (vagues, Nyxessa et ses missiles) ne tourne que chez l'hôte (`ReseauJeu.Autorite`) ; il sera répliqué à l'étape 2.
- **HUD** : colonne « vie des autres joueurs » à gauche (emblème de classe, pseudo, barre de vie fine ; mort : ligne grisée et compte à rebours), jusqu'à 3 lignes, vide en solo ; pseudos au-dessus des têtes. Contrat `IEtatEquipe` / `IAllie` (`Scripts/UI/Donnees/IEquipe.cs`), implémenté par `HudPresenter` ; banc : `EtatFactice.ForcerAllies(n)`. Capture `Assets/Screenshots/UI01_hud_allies.png`.
- **Quitter** : `Quitter` (lobby) ou « Quitter la partie » : l'hôte ferme la session pour tous, un client la quitte seul ; retour au menu (scène rechargée en solo). Côté client, la perte de l'hôte ramène au menu avec un message dans le lobby (« L’hôte a fermé le salon. », « Connexion à l’hôte perdue. »).

Préfabs : menu **Deathless > Jeu > 8. Réseau** (ajoute `NetworkObject`, `NetworkTransform`, `NetworkAnimator`, `HerosReseau` aux héros sans les reconstruire, crée le préfab du salon, calcule les identifiants réseau) ; `7. Classes` les ajoute aussi quand il reconstruit les héros.

Réglages : `NetworkConfig` créé par code (`ReseauJeu.Assurer`) : approbation des connexions, gestion des scènes, 30 ticks/s, délai de connexion 15 s.

## Étape 2 : le monde tenu par l'hôte

L'hôte fait foi sur tout ce qui n'est pas le héros d'un client ; chaque client simule seulement son héros (déplacement, gestes, visée, jauges) et suit le reste.

```
Scripts/Reseau/
  PartieReseau.cs     monde de la partie (apparu par l'hôte au lancement, détruit avec la scène) : horloge (phase, nuit,
                      temps, envoyé 5 fois par seconde), Nyxessa (PV, détruite), caisse commune, vote (prêts / joueurs),
                      scores et état de chaque joueur (NetworkList<ScoreReseau> : prêt, mort, délai, 7 compteurs),
                      sorcier (état, position, orientation, vitesse) ; RPC : zones d'apparition, pièces d'or, bouclier
                      (levé, touché, baissé), invocation du sorcier, missiles en crâne ; vote des clients (PretRpc)
  EnnemiReseau.cs     squelette réseau : chez un client, marionnette (IA et agent coupés, PV recopiés, sortie de terre et
                      désintégration rejouées) ; coups, étourdissements, poussées et provocations relayés vers l'hôte
  HerosReseau.cs      + mort et délai tenus par l'hôte ; coups des squelettes de l'hôte relayés vers le propriétaire (qui
                      garde, pare, esquive) ; tirs et fumigène du propriétaire rejoués chez les autres ; furtivité recopiée
```

- **Relais des coups** : `Sante.relais`. Sur une marionnette, un coup n'est pas appliqué : il part vers le poste qui fait foi et la fonction renvoie les dégâts estimés (jauges de rage et de mana du tireur). Client → hôte pour les squelettes (`EnnemiReseau.FrapperRpc` ; l'hôte crédite le joueur : dégâts, critiques, ennemis tués, or) ; hôte → propriétaire pour les héros (`HerosReseau.EncaisserRpc`). `Squelette.Etourdir`, `Repousser`, `Provoquer` passent aussi par l'hôte depuis un client.
- **Client** (`Partie.ClientReseau`) : `Partie.SuivreHote` recopie l'horloge et rejoue localement les changements de phase (mêmes événements : ambiance, HUD, sons, portail, alerte avant la nuit), Nyxessa (PV, coup reçu, destruction), la caisse, le vote, la mort et le score du joueur local. `DirecteurVagues`, `DefenseNyxessa`, l'IA des squelettes et celle du sorcier ne tournent que chez l'hôte (`ReseauJeu.Autorite`).
- **Morts et réapparitions** : le client signale sa mort (`MortRpc`) ; l'hôte compte la mort, tient le délai (8 s + 4 s par mort) et fait réapparaître (`ReapparaitreRpc`, téléportation du NetworkTransform), à l'aube aussi. Les autres voient la chute (NetworkAnimator), la dissolution et le retour.
- **Vote** : « Prêt » d'un client → `PretRpc` ; l'hôte compte (le jour s'écourte quand tous sont prêts) ; « Rejouer » sur l'écran de score : quand tous ont voté, l'hôte recharge le village pour tous (mêmes joueurs, mêmes classes).
- **Score** : écran de score avec une ligne par joueur (`HudPresenter.Joueurs` lit `PartieReseau.Scores`), le joueur local marqué.
- **Squelettes** : préfabs réseau (NetworkObject, NetworkTransform et NetworkAnimator en autorité serveur, échelle synchronisée pour les élites, `EnnemiReseau`), apparus par l'hôte (`DirecteurVagues.Poser`) ; nombre par nuit × (1 + 0,6 par joueur en plus) (`GameBalance.ennemisParJoueurEnPlus`).
- **Missiles en crâne** (Nyxessa, Nécromancien) : même vol chez les clients, sans dégâts (`MissileCrane.TirerVisuel`).
- **Stock des missiles de Nyxessa** (26/09/2026, compteur du HUD) : l'hôte écrit le stock dans `PartieReseau.StockMissiles` (`NetworkVariable<int>`, envoyée seulement quand elle change) ; le palier passe déjà par `PalierMissiles`. La recharge du prochain missile voyage dans `PartieReseau.MissileRegeneration`, 5 fois par seconde (comme `TempsPhase`) ; chaque client complète seul entre deux envois et se recale dès qu'une nouvelle valeur arrive (`Partie.SuivreMissiles`).
  - Stock plein : elle reste à 0.
  - Sinon : elle avance de `dt` jusqu'au prochain envoi de l'hôte, qui la recale (borne : durée du palier, `missileRegenerationPaliers`). Un tir ne la remet pas à zéro, comme chez l'hôte.
  - Depuis la canalisation du bouclier (27/09/2026, `BouclierNyxessa.Absorber`), un coup encaissé peut faire **bondir** la recharge sans faire monter le stock : une simple extrapolation en `dt` ne suffisait plus à la suivre, d'où l'envoi périodique de sa valeur (avant le 27/09/2026, seul le stock voyageait et la recharge n'était qu'extrapolée).
  - Vérifié le 26/09/2026 (hôte Paladin dans l'éditeur, client Mage caché ; journal du client par `ClientMissiles.cs`, fichier de test propre à la copie ; hôte forcé au palier 3, stock 1, puis un tir simulé) : le client suit le stock à la seconde près ; sa recharge a environ 0,1 s de retard, et le HUD du client affiche 1/4, 2/4, puis 3/4 avec la même charge que l'hôte.
  - L'écart avec l'hôte ne dépasse pas la latence (au plus l'intervalle d'envoi, 0,2 s) et se résorbe au missile suivant. Seule exception : si le client commence à suivre la partie pendant une recharge, son premier affichage peut être en retard.
- **Tirs des joueurs** (flèches, carreaux, boules de feu) : rejoués chez les autres (`ProjectileJeu.TirerVisuel`, même balistique, sans dégâts).
- **Effets de compétence** (26/09/2026) : chaque classe diffuse ses effets par `ClasseHeros.Diffuser(effet, a, b, v)` → `HerosReseau.EffetRpc` (propriétaire → autres postes) → `ClasseHeros.EffetDistant` sur la marionnette, qui rejoue visuel et son à la même position et dans la même orientation, sans dégâts (ils restent décidés comme avant). Paladin : élan et impact de l'épée, charge bélier et son impact, soin et aura, garde et parade ; Viking : élan, coup de hache, attaque tournante (début, effet, coups, fin), rugissement (effet et cri), saut percutant (onde) ; Mage : lancer de boule, cône de flammes (allumé, suit l'orientation du héros, éteint) ; Rôdeur : bander, tir, nuée de flèches (effet et sons), roulade, salve ; Assassin : dague, arbalète (et son rechargement), fumigène (`FumeeRpc`, avec le son du nuage) ; toutes les classes : marque de critique, esquive, saut. Les gestes passent par le `NetworkAnimator`, pas par ces effets. Exemple : la course de la charge bélier derrière le bouclier (26/09/2026). Elle utilise les déclencheurs `Charge` et `CoupBouclier`, les paramètres `Ruee` et `VitesseRuee`, et le poids de la couche haute. Le penché du corps se déduit de l'état de l'Animator sur la marionnette (`Docs/styles-d-armes.md`).
- **Emotes** (26/09/2026, roue à emotes) : rien de nouveau à diffuser. Le propriétaire écrit l'entier `EmoteNum` et lance le déclencheur `Emote` par `Heros.Declencher` ; son `NetworkAnimator` les transmet, et la marionnette joue la même sous-machine « Emotes ». Si le déclencheur arrive avant l'entier, il reste armé jusqu'à l'arrivée de l'entier ; à la fin d'une emote, le propriétaire remet `EmoteNum` à 0 (fin douce) ou -1 (fin brusque) et annule un déclencheur non consommé (`Heros.AnnulerDeclencheur`). La chope de « Boire un coup » (pleine, puis vide) se déduit de l'état `Boire` de l'Animator sur chaque poste (`EmotesHeros.LateUpdate`), comme le penché de la charge.
- **Sorcier et bouclier** : l'hôte pilote ; les clients suivent (marionnette du sorcier, effet du bouclier levé, frappé, brisé, baissé). Réaction de coup du sorcier (27/09/2026, quand le bouclier encaisse un coup) : `PartieReseau.SorcierTouche` (RPC vers les clients, `Sorcier.ToucherDistant`), fréquence limitée côté hôte.
- Préfabs : `Deathless > Jeu > 8. Réseau` équipe aussi les 4 squelettes et crée `Resources/Reseau/PartieReseau.prefab`.

## Statuts (26/09/2026)

Brûlure, ralenti, étourdi, ivresse, provoqué : liste, règles et icônes dans le wiki (`statuts.md`) ; code dans `Scripts/Jeu/Statuts/` (`Statuts`, un composant par personnage ; `CatalogueStatuts`) et `Scripts/Reseau/StatutsReseau.cs`.

- **L'hôte fait foi.** Il tient la liste de chaque personnage (squelettes et héros), applique les règles de cumul et les fins, et fait les dégâts de la brûlure (crédités au joueur qui l'a posée).
- **Synchronisation économe.** Chaque personnage a une `NetworkList<StatutReseau>` dans `EnnemiReseau` et `HerosReseau` (15 octets par statut : type, origine, joueur source, intensité, durée, fin en temps serveur).
  - L'hôte l'écrit seulement quand sa liste change (événement `Statuts.Change`) : ajout, retrait, fin, ou fin déplacée de plus de 0,5 s (`StatutsReseau.Tolerance`). NGO n'envoie que les éléments modifiés.
  - Rien n'est envoyé à chaque image : chaque client fait défiler les durées lui-même à partir de la fin en temps serveur. Un cône de flammes qui rafraîchit une brûlure 4 fois par seconde produit au plus 2 petits messages par seconde et par ennemi, et plus rien ensuite.
  - Les statuts de zone (eau du donjon) ne sont pas envoyés : chaque poste les calcule d'après la position.
- **Demandes des clients.** `Statuts.Ajouter` chez un client devient une demande à l'hôte (`relais`), au plus une toutes les 0,4 s par type. L'hôte la vérifie (`StatutsReseau.Valider`).
  - Sur un squelette (`EnnemiReseau.StatutRpc`) : la brûlure du mage, avec les valeurs de l'hôte et créditée au joueur qui la demande. Avant, le client faisait lui-même les dégâts de la brûlure et envoyait un coup par tic.
  - L'étourdissement et la provocation passent toujours par leurs relais (`EtourdirRpc`, `ProvoquerRpc`) : l'hôte pose le statut en même temps que l'effet.
  - Sur son propre héros (`HerosReseau.StatutRpc`, propriétaire seulement) : ralenti de la chute, étourdi de la garde brisée, ivresse de la taverne.
- **Prédiction du propriétaire.** Le client qui demande un statut pour son héros l'applique aussitôt chez lui (le ralenti agit sans attendre l'aller-retour). Quand la liste de l'hôte revient, elle remplace la prédiction ; une prédiction absente de la liste de l'hôte est gardée 1,5 s au plus (`Statuts.GracePrediction`), puis elle s'éteint à sa fin.
- **Effets.** L'effet reste là où il agit : l'étourdissement et l'IA des squelettes chez l'hôte ; le déplacement du héros (ralenti) et la caméra (ivresse) chez son propriétaire. Les flammèches de la brûlure s'allument sur tous les postes d'après la liste.
- **Vérifié le 26/09/2026** (hôte Paladin dans l'éditeur, client Mage caché, adresse IP, script `scratchpad/reseau/statuts.sh`) :
  - trois squelettes posés près du mage client et étourdis par l'hôte ; ses boules de feu les brûlent ;
  - chez l'hôte, la brûlure porte la source « joueur 2 » (le client) ; chez le client, 3 ennemis affectés (Brûlure ×1, Étourdi ×3) ;
  - environ une liste reçue et une demande envoyée par seconde ;
  - tournée lancée par l'hôte : l'ivresse du héros client figure chez l'hôte, et chez le client elle est déjà confirmée (plus « prédite ») au premier relevé ;
  - aucune erreur dans les deux consoles.
- **Tests.** `ClientAutomatique` journalise les ennemis affectés vus par le client, les listes reçues (`Statuts.ListesRecues`), les demandes envoyées (`Statuts.DemandesEnvoyees`) et les statuts de son héros (marqués « prédit » tant que l'hôte n'a pas répondu).

## Tests (sans fenêtre)

Hôte : l'éditeur `main` en Play. Client : **l'éditeur Unity lui-même en `-batchmode -nographics`**, fenêtre cachée, sur une **copie** du projet (Assets, Packages, ProjectSettings ; sans le paquet MCP), qui entre en Play et est piloté par `ClientAutomatique`. Pas d'exécutable construit : un nouvel .exe qui écoute ferait apparaître la demande d'autorisation du pare-feu Windows, `Unity.exe` a déjà ses règles. La copie doit avoir un **chemin court** (jonction), sinon certains chemins du `PackageCache` dépassent 260 caractères. Aucun script ne doit être modifié dans `main` pendant que l'hôte est en Play (recompilation en Play : scripts perdus).

Arguments de `ClientAutomatique` :

```
-deathless-rejoindre=127.0.0.1[:7777]   ou   -deathless-code=ABC123   ou   -deathless-heberger
-deathless-pseudo=Morgane -deathless-classe=mage -deathless-duree=60
[-deathless-profil=client2] [-deathless-direct] [-deathless-quitter-salon=6]
[-deathless-competences] [-deathless-attendre=2] [-deathless-solo]
```

`-deathless-competences` : le héros enchaîne toutes ses compétences (saut, esquive, RT, LB, RB, LT maintenu, RT maintenu), jauge remplie, et le journal compte les effets reçus des autres (`HerosReseau.EffetsRecus`). `-deathless-attendre=2` : l'hôte construit ne se déclare prêt qu'à 2 joueurs. `-deathless-solo` : partie solo lancée aussitôt (vérification d'un build : le journal donne aussi l'état de la caméra).

`-deathless-donjon=retour|rester` fait le test du donjon.
- Le héros se place à 1,8 m du portail du village et entre avec Interagir (invite « Entrer dans le donjon »), marche sur un tas d'or, puis ouvre un coffre avec Interagir.
- Il se place ensuite devant le portail de retour. Avec `retour`, il revient avec Interagir (« Revenir au village »), et son or est versé à la caisse. Avec `rester`, il attend le rappel du crépuscule.
- Le journal donne ce que voit le poste : graine, butins pris, or porté, caisse, présence au donjon, invites des portails.
- `-deathless-capture=dossier` (26/09/2026) : images PNG de la caméra du jeu aux étapes (portail du village, passage, arrivée, cadenas du coffre, portail de retour, retour au village), rendues dans une texture. Avec `-batchmode` sans `-nographics`, un build les produit sans fenêtre ni prise de focus. L'interface n'y figure pas.
- Script d'hôte : `scratchpad/reseau/donjon.sh`.

Il rejoint, prend sa classe, se déclare prêt, puis fait marcher son héros en rond (sprint une seconde sur trois ; saut, esquive, attaque toutes les 1,5 s) et journalise toutes les 2 s le salon, sa position, le sol sous lui et les héros des autres (position, vie). Pseudo et classe imposés ne touchent pas au profil enregistré.

Résultats du 25/09/2026 (hôte Paladin « Quentin », client Mage « Morgane ») :

| Test | Résultat |
|---|---|
| Rejoindre par IP (127.0.0.1:7777) | salon, pseudos, classes (Mage pris → l'autre classe libre), prêts, compte à rebours, lancement ; les deux héros apparaissent de leur classe ; chacun voit l'autre bouger (positions dans les deux journaux), l'hôte voit le Mage courir et tirer (Walking_A / Running_A, Ranged_Magic_Shoot sur la couche haute), pseudo au-dessus de la tête, colonne des alliés |
| Rejoindre par code Relay | code BKDTLF créé par l'hôte, rejoint par le client (profil `client2`), même déroulé jusqu'à la partie |
| Départ d'un client dans le salon | retiré de la liste, classe libérée (l'hôte peut la prendre aussitôt) |
| Départ propre d'un client en partie | « client parti », héros retiré chez l'hôte, colonne des alliés vidée |
| Coupure brutale d'un client (processus tué) | détectée par l'hôte en ~30 s (délai du transport), même traitement |
| L'hôte ferme le salon / quitte la partie | le client reçoit « L’hôte a fermé le salon. », revient au menu (scène rechargée) |

Résultats de l'étape 2 (25/09/2026, hôte Paladin, client Mage, adresse IP) : vote des deux joueurs → jour écourté, nuit chez les deux ; squelettes vus par le client, qui en tue (dégâts, tués et or crédités par l'hôte : 4 tués, 320 dégâts, 20 or) ; squelettes qui frappent le héros du client ; coup mortel porté chez l'hôte → mort chez le client, comptée par l'hôte (délai 8 s), réapparition au bout du délai ; sorcier et bouclier suivis ; victoire forcée → écran de score à deux lignes chez les deux ; « Rejouer » → nouvelle partie pour les deux ; Nyxessa détruite chez l'hôte → défaite chez le client. Aucune erreur dans les consoles.

## Donjon (26/09/2026)

`Assets/Scripts/Jeu/Donjon/DonjonJeu.cs` est posé sur l'objet « Donjon » de Village.unity, à (1000, 0, 0), par le menu Deathless > Donjon > Placer dans le village. `CoffreDonjon.cs` complète le dispositif.

- **Graine** : l'hôte (ou le poste solo) tire la graine au début du jour (`NouveauDonjon`) et la publie dans `PartieReseau.GraineDonjon`. Chaque client construit le donjon de cette graine (`DonjonGenerateur.Generer`, déterministe, avec son propre NavMesh).
- **Butin** : les butins pris sont un masque de bits, `PartieReseau.ButinsPris`.
  - Un client demande un butin avec `DemanderButin(index)` (RPC au serveur). L'hôte vérifie la distance et l'accorde (`DonjonJeu.Accorder`) en ajoutant l'or à `EtatJoueur.orPorte`.
  - L'or porté est répliqué dans `ScoreReseau.orPorte`.
- **Portails** (touche Interagir, 26/09/2026) : `PassagePortail` (un `PointInteraction`) est posé par `DonjonJeu` sur le portail du village et sur le repère du portail de retour. Chaque poste décide seul du passage de **son** héros (`DonjonJeu.InvitePortail`, `Passer`) : conditions locales (jour et portail ouvert pour l'entrée, 3 m au plus), rien n'est demandé à l'hôte, sauf le dépôt de l'or.
- **Dépôt** : au portail de retour, un client envoie `DeposerOr()`, et l'hôte verse l'or à la caisse avec `GagnerOr`.
- **Rappel** : au crépuscule, l'hôte calcule la part gardée (`GameBalance.partGardeeRappel` au palier des missiles), puis envoie `HerosReseau.Rappeler(garde, perdu)` au propriétaire, qui se téléporte près de Nyxessa.
- **Passages** : le propriétaire joue la téléportation (`PortalTransit`) et la diffuse avec `ClasseHeros.DiffuserTransit` (effets 203 et 204). La valeur libre `v` de l'effet porte le portail (0 aucun pour le rappel, 1 village, 2 retour du donjon) : les autres postes rejouent la dissolution vers ce portail et l'arrivée depuis lui, avec les anneaux (`DonjonJeu.TransitDistant`). Il saute de position avec `NetworkTransform.Teleport`, sans interpolation. Chez les autres, la marionnette est cachée entre le départ et l'arrivée.
- **Gardiens** : ils sont posés par l'hôte avec `DirecteurVagues.Poser` puis `Squelette.Garder(poste)`, et répliqués comme les autres squelettes.
- **Local à chaque poste** : le masquage des étages (capteurs sur le héros local et sa caméra), l'ambiance sombre et l'animation des coffres (couvercle ; plus de cadenas depuis le 26/09/2026).

## Limites connues

- Pose de l'arc des autres rôdeurs (flèche encochée, corde tendue) et cercle de charge : visibles seulement par le tireur.
- Le penché du buste en visée (arc, arbalète) n'est pas recopié chez les autres ; l'orientation du corps l'est.
- Chaque coup d'un client sur un squelette est un message ; le cône de flammes en envoie beaucoup (sans gêne constatée à deux). La brûlure n'en envoie plus : l'hôte fait ses dégâts (« Statuts »).
- Pas d'arrivée en cours de partie ; pas de reconnexion.
- Coupure brutale : l'hôte ne s'en aperçoit qu'au bout du délai du transport (~30 s).
- Après une déconnexion, le message d'erreur est dans le lobby ; le menu principal s'affiche d'abord.
- Le secours IP direct suppose le port 7777 ouvert chez l'hôte (réseau local, ou redirection de port sur la box).
- Le premier usage du multijoueur écrit le jeton du joueur anonyme dans les PlayerPrefs (par profil).
- Filet de sécurité ajouté au héros (tous modes) : un héros passé sous le sol (y < −25 m) revient au point de réapparition le plus proche ; le héros réseau est recalé sur son point d'apparition avant son premier pas (une chute à travers le sol a été vue deux fois sur le client avant ce correctif).
