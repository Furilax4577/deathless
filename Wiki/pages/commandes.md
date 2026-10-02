# Commandes

Le jeu se joue à la manette Xbox ou PlayStation, ou au clavier et à la souris. Les icônes de boutons affichées changent automatiquement selon le dernier appareil utilisé.

{dev} Cette table est la seule référence des commandes : dans Unity, elle correspond au fichier d'actions `DeathlessControls` {décidé}.

## Table de correspondance

| Action | Xbox | PlayStation | Clavier et souris | Statut {dev} |
|---|---|---|---|---|
| Sauter | A | Croix | Espace | {décidé} |
| Esquive, roulade | B | Rond | Ctrl | {décidé} |
| Interagir, parler | X | Carré | E | {décidé} |
| Menu du personnage (inventaire, compétences) | Y | Triangle | Tab | {décidé} |
| Attaque principale (pendant une visée de zone : **confirmer**) | RT | R2 | Clic gauche | {décidé} |
| Attaque secondaire, garde, parade, visée (pendant une visée de zone : **annuler**) | LT | L2 | Clic droit | {décidé} |
| Compétence 1 | LB | L1 | A | {décidé} |
| Compétence 2 | RB | R1 | R | {décidé} |
| Compétence 3 | LB + RB | L1 + R1 | F | {décidé} |
| **Furie** (ultime du Viking, rage pleine ; sans effet avant 100 et pour les autres classes) | R3 (clic du stick droit) | R3 | G | {décidé, Quentin 03/10/2026} |
| Sprinter | L3 | L3 | Maj | {décidé} |
| Boire une potion de **santé** | Croix directionnelle haut | Croix directionnelle haut | 1 | {décidé} |
| Boire une potion de **mana** (Mage seulement) | Croix directionnelle gauche | Croix directionnelle gauche | 2 | {décidé, Quentin 03/10/2026} |
| Boire une potion d'**endurance** | Croix directionnelle droite | Croix directionnelle droite | 3 | {décidé, Quentin 03/10/2026} |
| Roue à emotes (maintenir, pointer, relâcher ; voir [Interface](interface.md)) | Croix directionnelle bas | Croix directionnelle bas | B | {décidé} |
| Se déclarer prêt (jour, voir [Déroulé d'une partie](deroule.md)) | Vue | Pavé tactile ou Create | F1 | {décidé} |
| Pause | Menu | Options | Échap | {décidé} |

Au clavier, les touches sont données sur une disposition AZERTY. Le déplacement se fait avec ZQSD et la caméra avec la souris.

Les trois potions occupent les trois directions de la croix qui étaient libres (la croix bas reste la roue à emotes) : **aucune direction n'était déjà prise** en dehors du haut, qui était déjà la potion. Au clavier, les touches 1, 2 et 3 sont celles de la rangée des chiffres (disposition AZERTY : & é "). Détail des potions dans [Le village](village.md#potions-du-druide-décidé). {dev} Actions `DrinkPotion` (santé), `DrinkPotionMana`, `DrinkPotionStamina` de `DeathlessControls`, relayées par `InputChordResolver` comme les autres boutons, lues par `Heros.OnAction` → `UsagePotions.Boire`.

Touche de la Furie (R3, G) {à confirmer} : proposée faute de mieux, R3 étant libre depuis le retrait de l'accroupissement.

## Combinaisons à la manette {décidé}

La combinaison LB + RB utilise un **court délai** : quand on appuie sur LB, le jeu attend environ 0,1 s {à équilibrer}. Si RB arrive dans ce délai, c'est la compétence 3 ; sinon, la compétence 1 part. Aucune compétence ne part par erreur, et le délai reste imperceptible.

{dev} Plus d'accroupissement {décidé} : l'action `Crouch` et l'accord L3 + R3 ont été retirés de `DeathlessControls` et de `InputChordResolver`. **L'ultime revient pour le Viking seulement** (03/10/2026) : une action `Ultimate` simple (pas d'accord, pas de délai) lie R3 et G ; le résolveur la relaie comme les autres boutons, le code de jeu la lit par `Triggered` (jamais par `performed`). Le Viking la lit dans `ClasseViking.SurAction("Ultimate")` ; les autres classes l'ignorent.

## Viser une zone au sol {décidé, 02/10/2026}

Les sorts qui tombent sur un endroit précis (grande boule de feu et mur de flammes du [Mage](classe-mage.md), nuée de flèches du [Rôdeur](classe-rodeur.md)) ne partent plus à l'appui sur la compétence : l'appui **ouvre une visée**. Un cercle (ou une ligne) à la taille réelle de la zone suit le point que le réticule vise, limité à la portée du sort ; le HUD affiche le nom du sort et les deux invites. On confirme ou on annule :

| Pendant la visée | Xbox | PlayStation | Clavier et souris |
|---|---|---|---|
| Ouvrir la visée (compétence 1 ou 2) | LB, RB | L1, R1 | A, R |
| **Confirmer** : le sort part sur le point visé, le mana et la recharge sont dépensés | RT | R2 | Clic gauche |
| **Annuler** : rien n'est dépensé | LT | L2 | Clic droit |
| Annuler en esquivant (l'esquive ferme aussi la visée) | B | Rond | Ctrl |
| Changer de sort visé (mage) | LB ↔ RB | L1 ↔ R1 | A ↔ R |

Règles communes {à confirmer} : le héros marche à 50 % pendant la visée, sans sprint ni saut ; ni mana ni recharge avant la confirmation ; un étourdissement, la mort, un portail, l'ouverture d'un menu ou de la roue à emotes ferment la visée sans coût ; LT, après avoir annulé, ne déclenche pas l'action maintenue de la classe (cône de flammes du mage, visée zoomée du rôdeur) tant qu'il n'est pas relâché ; l'indicateur n'est vu que du joueur qui vise.

{dev} Code commun : `VisiereZone`, `ClasseHeros.ViseeSurAction` (confirmation par `AttackPrimary`, annulation par `AttackSecondary`, résolus par `InputChordResolver`), invites `HudVisee` (`InputPrompt` sur `Gameplay/AttackPrimary` et `Gameplay/AttackSecondary`, qui suivent le dernier appareil).

## Touches de dev de l'aperçu « Nouvelle carte » {décidé, 02/10/2026} {dev}

Trois touches de clavier réservées au mode « Nouvelle carte (aperçu) » : outils de dev, liés à aucune action de `DeathlessControls`, **sans effet dans le jeu normal** (ancienne carte, parties à vagues). Chaque appui affiche un court message à l'écran.

| Touche | Effet {décidé} | Message |
|---|---|---|
| **F8** | Donjon en terrasses : graine suivante ; **Maj + F8** : graine au hasard | « Donjon : graine N (...) » |
| **F9** | Bascule **jour / nuit** : fondu d'une seconde vers l'ambiance de nuit (lanternes, fenêtres, brume violet-gris, lune, musique de nuit) et retour. Ambiance seulement : la phase reste le jour figé, **jamais de vague** ; le HUD écrit « Jour · aperçu » ou « Nuit · aperçu » ; le donjon garde son éclairage | « Nuit » / « Jour » |
| **F10** | Bascule **sans mob / avec mob** : sans, tous les ennemis vivants disparaissent (gardiens du donjon, squelettes d'essai) et rien n'apparaît tant que c'est désactivé ; avec, les gardiens reviennent à leurs 14 points et, si le héros est dehors, quatre squelettes d'essai se posent devant lui | « Mobs : désactivés » / « Mobs : activés » |

L'état de F9 et de F10 tient pendant les allers-retours par les portails et s'applique aux donjons que F8 construit ensuite ; il est remis à zéro au chargement de la scène.

{dev} Code : `DonjonJeu.ToucheApercu` (lecture directe du clavier, comme F8 ; `Partie.Exploration` seulement), état dans `Partie.NuitApercu` et `Partie.SansMobApercu`, fondu de nuit dans `VueCycle` (`CycleJourNuit.nuitForcee`), garde « sans mob » dans `DirecteurVagues.Poser`. Détails dans `Docs/donjon-generateur.md`, section « Branchement dans le jeu ».

## Dans les menus {décidé}

| Action | Xbox | PlayStation | Clavier et souris |
|---|---|---|---|
| Valider | A | Croix | Entrée ou clic |
| Retour | B | Rond | Échap |
| Onglet précédent, suivant | LB, RB | L1, R1 | Q, E |
| Réinitialiser | Y | Triangle | Suppr |
