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
| Attaque principale | RT | R2 | Clic gauche | {décidé} |
| Attaque secondaire, garde, parade, visée | LT | L2 | Clic droit | {décidé} |
| Compétence 1 | LB | L1 | A | {décidé} |
| Compétence 2 | RB | R1 | R | {décidé} |
| Compétence 3 | LB + RB | L1 + R1 | F | {décidé} |
| Sprinter | L3 | L3 | Maj | {décidé} |
| Boire une potion | Croix directionnelle haut | Croix directionnelle haut | 1 | {décidé} |
| Roue à emotes (maintenir, pointer, relâcher ; voir [Interface](interface.md)) | Croix directionnelle bas | Croix directionnelle bas | B | {à confirmer} |
| Se déclarer prêt (jour, voir [Déroulé d'une partie](deroule.md)) | Vue | Pavé tactile ou Create | F1 | {décidé} |
| Pause | Menu | Options | Échap | {décidé} |

Au clavier, les touches sont données sur une disposition AZERTY. Le déplacement se fait avec ZQSD et la caméra avec la souris.

## Combinaisons à la manette {décidé}

La combinaison LB + RB utilise un **court délai** : quand on appuie sur LB, le jeu attend environ 0,1 s {à équilibrer}. Si RB arrive dans ce délai, c'est la compétence 3 ; sinon, la compétence 1 part. Aucune compétence ne part par erreur, et le délai reste imperceptible.

{dev} Plus d'ultime ni d'accroupissement {décidé} : les actions `Ultimate` et `Crouch` et l'accord L3 + R3 sont à retirer de `DeathlessControls` et de `InputChordResolver`. R3 reste libre.

## Dans les menus {décidé}

| Action | Xbox | PlayStation | Clavier et souris |
|---|---|---|---|
| Valider | A | Croix | Entrée ou clic |
| Retour | B | Rond | Échap |
| Onglet précédent, suivant | LB, RB | L1, R1 | Q, E |
| Réinitialiser | Y | Triangle | Suppr |
