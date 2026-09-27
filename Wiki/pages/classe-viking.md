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
  - **Baisse** : 6 par seconde, après 4 s sans toucher d'ennemi. Recevoir des coups n'empêche pas la baisse. Pas de baisse pendant l'attaque tournante. {{dev: (`rageBaisse`, `rageDelaiBaisse`)}}
- **Valeurs de départ** de la version 0.2 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 140 |
| Hache | 38 dégâts, touche tous les ennemis de l'arc ; +8 rage par ennemi touché |
| Rage | jauge de 100, plancher 30 (27/09/2026) : rage de départ, et hors combat la jauge revient vers 30 (baisse si au-dessus, remonte si une compétence l'a fait passer dessous, 6 par seconde) |
| Attaque tournante | 15 rage au moins pour la lancer ; 10 dégâts toutes les 0,3 s (12 avant le 27/09/2026), rayon 2,3 m ; vitesse ×0,6 ; 20 rage par seconde ; chaque tic rend +2 rage par ennemi touché {à équilibrer} : elle ne se paie qu’à partir de 3 ennemis (décidé le 26/09/2026) |
| Rugissement | 15 rage (25 avant le 27/09/2026), recharge 12 s ; Peau de fer 6 s sur le viking |
| Saut percutant | 25 rage (35 avant le 27/09/2026), 45 dégâts, recharge 8 s |

## Lissage du 27/09/2026 {décidé}

Suite à l'audit d'équilibrage (`Docs/equilibrage-classes.md`) : le viking partait à zéro rage à chaque vague (rage vide en 20 s, vagues à 40 s d'écart), donc sans saut ni rugissement au début de chaque assaut, et son rugissement l'immobilisait 1,5 s sans protection. Décidé par Quentin :

- **Plancher de rage : 30**. La rage ne descend jamais sous 30 hors combat ; il a toujours de quoi ouvrir une vague.
- **Coûts** : saut percutant 35 → **25**, rugissement 25 → **15**, attaque tournante 12 → **10** de rage.
- **Rugissement** : il pose **Peau de fer** sur le viking (statut : **−35 % de dégâts subis pendant 6 s**, bienfait, liseré or dans le HUD) et se joue sur le **haut du corps** : il continue de marcher pendant le cri.

{dev} `GameBalance` (rageMin, coûts), `ClasseViking` (plancher, Peau de fer par le chemin des statuts, couche haute de l'Animator comme la charge du paladin), nouveau statut `PeauDeFer` dans le catalogue (icône à générer), réseau par `HerosReseau.StatutRpc` existant.

{{dev: Fait le 27/09/2026. Plancher : hors combat (4 s sans toucher), `m_Rage` va vers `rageMin` dans les deux sens (`Mathf.MoveTowards`, `rageBaisse`/s) : après un rugissement à 30 (reste 15), la jauge remonte à 30 en 2,5 s ; la tournante seule peut vider la jauge. Peau de fer : posée **au moment du cri** (1 s après l'appui, quand la provocation part), `Statuts.Ajouter(PeauDeFer, peauDeFerDuree 6 s, peauDeFerReduction 0,35, Joueur)` ; effet dans `Sante.absorbeur` du héros (`Heros.Absorber`), donc sur tout coup ennemi, parable ou non, chez le propriétaire (prédit puis confirmé ; `StatutsReseau.Valider` impose les valeurs de l'hôte). Cri sur la couche haute : état `Rugissement` dans `HautDuCorps` du contrôleur `Viking_Jeu` (`ClassesBuilder.ControleurViking`), `ClasseViking.HautDuCorps` vrai pendant le cri, déplacement libre à vitesse normale (plus de `DeplacementImpose` immobile).}}

