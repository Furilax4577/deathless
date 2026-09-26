# Viking

{video media/classes/viking/rotation.mp4} **Rendu 3D** | Rotation en garde, hache à deux mains | {dev} modèle `Barbarian`, style `Axe2H`, clip `Melee_2H_Idle_Loop` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/viking/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_viking}

La force brute. Sa hache à deux mains frappe tout autour de lui, et plus il frappe, plus sa rage monte. Il attire les squelettes d'un rugissement et bondit dans la mêlée.

| Rôle | Arme |
|---|---|
| Mêlée, zone | Hache à deux mains |

{dev} Modèle : le barbare KayKit (`Barbarian`), style d'arme hache à deux mains.

## Actions

| | Touche | Action |
|---|---|---|
| {icone viking_hache} | RT | Hache |
| {icone viking_attaque_tournante} | LT | Attaque tournante, maintenue |
| {icone viking_rugissement} | LB | Rugissement |
| {icone viking_saut_percutant} | RB | Saut percutant |
| {icone jauge_rage} |  | Jauge de rage |
Répartition du rugissement et du saut sur LB et RB : celle de la version 0.1 {à confirmer}.

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Viking (`Barbarian`, style `Axe2H`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Viking_Jeu.controller` (généré par `ClassesBuilder.ControleurViking`), déclenché par `ClasseViking`. Toutes les animations : [Animations](animations.md).

### Hache

Les deux coups alternent.

{video media/classes/viking/clips/Melee_2H_Attack_Slice.mp4} **Hache : taille (1er coup)** | {dev} `Melee_2H_Attack_Slice` | Arme : hache à deux mains | une fois · 1,10 s | {dev} vitesse calée sur `hacheIntervalle`
{video media/classes/viking/clips/Melee_2H_Attack_Chop.mp4} **Hache : coup de haut en bas (2e coup)** | {dev} `Melee_2H_Attack_Chop` | Arme : hache à deux mains | une fois · 1,63 s | {dev} vitesse calée sur `hacheIntervalle`

### Attaque tournante

{video media/classes/viking/clips/Melee_2H_Attack_Spin.mp4} **Attaque tournante : élan** | {dev} `Melee_2H_Attack_Spin` | Arme : hache à deux mains | une fois · 2,40 s | {dev} début du clip, jusqu'à 1,2 s
{video media/classes/viking/clips/Melee_2H_Attack_Spinning.mp4} **Attaque tournante : tourbillon, tant que la touche est tenue** | {dev} `Melee_2H_Attack_Spinning` | Arme : hache à deux mains | boucle · 0,67 s | {dev} copie bouclante `Melee_2H_Attack_Spinning_Loop`
{video media/classes/viking/clips/Melee_2H_Attack_Spin.mp4} **Attaque tournante : fin** | {dev} `Melee_2H_Attack_Spin` | Arme : hache à deux mains | une fois · 2,40 s | {dev} fin du même clip, à partir de 1,25 s

### Rugissement

{video media/classes/viking/clips/Skeletons_Taunt_Longer.mp4} **Rugissement** | {dev} `Skeletons_Taunt_Longer` | Arme : hache à deux mains | une fois · 3,00 s | {dev} emprunté au pack Skeletons, vitesse ×1,6 ; geste gardé (V2 écartée par Quentin, 26/09/2026)

### Saut percutant

{video media/classes/viking/clips/Melee_1H_Attack_Jump_Chop.mp4} **Saut percutant** | {dev} `Melee_1H_Attack_Jump_Chop` | Arme : hache à deux mains (clip emprunté à une prise à une main) | une fois · 1,33 s | {dev} vitesse ×1,2 ; bond de 5 m translaté par script

## Règles

- Hache à deux mains **uniquement** {décidé}. Pas de bouclier.
- **Attaque tournante** {décidé} : le viking tourne sur lui-même, hache tendue, et frappe tout autour de lui {{dev: (animations KayKit `Melee_2H_Attack_Spin` et `Melee_2H_Attack_Spinning`)}}.
  - **Maintenue** {décidé} : tant que la touche est tenue, le viking tourne. Elle consomme de la rage en continu et s'arrête quand la rage est vide. Consommation {à équilibrer}.
- **Rugissement** : un crâne de barbare casqué en gemmes rouges surgit au-dessus du viking, rugit, et une onde part de lui puis revient comme pour dire « venez » {effet validé}.
- **Saut percutant** : le viking bondit d'environ 5 m vers l'avant et frappe le sol, une onde de terre part du point d'impact {effet validé}.
- **Rage** {décidé} : jauge de 100. Elle monte quand le viking frappe et redescend lentement hors combat. Les compétences du viking coûtent de la rage. Valeurs {à équilibrer}.
- **Valeurs de départ** de la version 0.2 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 140 |
| Hache | 38 dégâts, touche tous les ennemis de l'arc ; +8 rage par ennemi touché |
| Attaque tournante | 20 rage par seconde |
| Rugissement | 25 rage, recharge 12 s |
| Saut percutant | 35 rage, 45 dégâts, recharge 8 s |

