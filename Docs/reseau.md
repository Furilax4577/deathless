# Réseau (multijoueur)

Jusqu'à 4 joueurs. Un joueur **héberge** (hôte = serveur + joueur), les autres le **rejoignent**. En solo, le réseau ne démarre pas : le jeu tourne exactement comme avant (aucun `NetworkManager` à l'écoute, `ReseauJeu.Autorite` vrai).

État au 25/09/2026 : **étape 1** faite (lobby réel, lancement, héros de chacun, déplacements et animations vus des autres, pseudos, colonne des alliés). **Étape 2** à venir : monde tenu par l'hôte (squelettes, vagues, dégâts, Nyxessa et missiles, cycle, portail, morts et réapparitions, vote du jour, score, sorcier et bouclier, or ; +60 % d'ennemis par joueur en plus).

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

## Tests (sans fenêtre)

Hôte : l'éditeur `main` en Play. Client : **l'éditeur Unity lui-même en `-batchmode -nographics`**, fenêtre cachée, sur une **copie** du projet (Assets, Packages, ProjectSettings ; sans le paquet MCP), qui entre en Play et est piloté par `ClientAutomatique`. Pas d'exécutable construit : un nouvel .exe qui écoute ferait apparaître la demande d'autorisation du pare-feu Windows, `Unity.exe` a déjà ses règles. La copie doit avoir un **chemin court** (jonction), sinon certains chemins du `PackageCache` dépassent 260 caractères. Aucun script ne doit être modifié dans `main` pendant que l'hôte est en Play (recompilation en Play : scripts perdus).

Arguments de `ClientAutomatique` :

```
-deathless-rejoindre=127.0.0.1[:7777]   ou   -deathless-code=ABC123   ou   -deathless-heberger
-deathless-pseudo=Morgane -deathless-classe=mage -deathless-duree=60
[-deathless-profil=client2] [-deathless-direct] [-deathless-quitter-salon=6]
```

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

## Limites connues (étape 1)

- Les clients ne voient ni squelettes, ni tirs de Nyxessa, ni projectiles et effets des autres ; chaque poste a sa propre horloge de jour et de nuit (lancée au même moment). Tout cela est l'étape 2.
- Pas d'arrivée en cours de partie ; pas de reconnexion.
- Coupure brutale : l'hôte ne s'en aperçoit qu'au bout du délai du transport (~30 s).
- Après une déconnexion, le message d'erreur est dans le lobby ; le menu principal s'affiche d'abord.
- Le secours IP direct suppose le port 7777 ouvert chez l'hôte (réseau local, ou redirection de port sur la box).
- Le premier usage du multijoueur écrit le jeton du joueur anonyme dans les PlayerPrefs (par profil).
- Filet de sécurité ajouté au héros (tous modes) : un héros passé sous le sol (y < −25 m) revient au point de réapparition le plus proche ; le héros réseau est recalé sur son point d'apparition avant son premier pas (une chute à travers le sol a été vue deux fois sur le client avant ce correctif).
