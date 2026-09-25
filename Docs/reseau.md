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
- **Tirs des joueurs** (flèches, carreaux, boules de feu) : rejoués chez les autres (`ProjectileJeu.TirerVisuel`, même balistique, sans dégâts).
- **Effets de compétence** (26/09/2026) : chaque classe diffuse ses effets par `ClasseHeros.Diffuser(effet, a, b, v)` → `HerosReseau.EffetRpc` (propriétaire → autres postes) → `ClasseHeros.EffetDistant` sur la marionnette, qui rejoue visuel et son à la même position et dans la même orientation, sans dégâts (ils restent décidés comme avant). Paladin : élan et impact de l'épée, charge bélier et son impact, soin et aura, garde et parade ; Viking : élan, coup de hache, attaque tournante (début, effet, coups, fin), rugissement (effet et cri), saut percutant (onde) ; Mage : lancer de boule, cône de flammes (allumé, suit l'orientation du héros, éteint) ; Rôdeur : bander, tir, nuée de flèches (effet et sons), roulade, salve ; Assassin : dague, arbalète (et son rechargement), fumigène (`FumeeRpc`, avec le son du nuage) ; toutes les classes : marque de critique, esquive, saut.
- **Sorcier et bouclier** : l'hôte pilote ; les clients suivent (marionnette du sorcier, effet du bouclier levé, frappé, brisé, baissé).
- Préfabs : `Deathless > Jeu > 8. Réseau` équipe aussi les 4 squelettes et crée `Resources/Reseau/PartieReseau.prefab`.

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

## Limites connues

- Pose de l'arc des autres rôdeurs (flèche encochée, corde tendue) et cercle de charge : visibles seulement par le tireur.
- Le penché du buste en visée (arc, arbalète) n'est pas recopié chez les autres ; l'orientation du corps l'est.
- Chaque coup d'un client sur un squelette est un message ; les dégâts continus (cône, brûlure) en envoient beaucoup (sans gêne constatée à deux).
- Pas d'arrivée en cours de partie ; pas de reconnexion.
- Coupure brutale : l'hôte ne s'en aperçoit qu'au bout du délai du transport (~30 s).
- Après une déconnexion, le message d'erreur est dans le lobby ; le menu principal s'affiche d'abord.
- Le secours IP direct suppose le port 7777 ouvert chez l'hôte (réseau local, ou redirection de port sur la box).
- Le premier usage du multijoueur écrit le jeton du joueur anonyme dans les PlayerPrefs (par profil).
- Filet de sécurité ajouté au héros (tous modes) : un héros passé sous le sol (y < −25 m) revient au point de réapparition le plus proche ; le héros réseau est recalé sur son point d'apparition avant son premier pas (une chute à travers le sol a été vue deux fois sur le client avant ce correctif).
