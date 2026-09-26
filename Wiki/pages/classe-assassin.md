# Assassin

{video media/classes/assassin/rotation.mp4} **Rendu 3D** | Rotation en attente, dague en main, arbalète dans le dos | {dev} modèle `Rogue_Hooded`, style `DaggerCrossbow`, clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/assassin/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_assassin}

Il frappe fort quand on ne le voit pas. Furtif en marchant, il porte ses meilleurs coups dans le dos d'un ennemi qui ne l'a pas repéré, puis disparaît dans la fumée.

| Rôle | Arme |
|---|---|
| Furtif, coups critiques | Dague, arbalète dans le dos |

{dev} Modèle : le voleur à capuche KayKit (`Rogue_Hooded`), style d'arme dague et arbalète.

## Actions

| | Touche | Action |
|---|---|---|
| {icone assassin_dague} | RT | Dague |
| {icone assassin_arbalete} | LT | Arbalète en main et visée ; RT tire |
| {icone assassin_fumigene} | LB | Grenade fumigène |
|  | RB | Vide |
| {icone assassin_furtif} |  | Indicateur du mode furtif |

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle de l'Assassin (`Rogue_Hooded`, style `DaggerCrossbow`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Assassin_Jeu.controller` (généré par `ClassesBuilder.ControleurAssassin`), déclenché par `ClasseAssassin`. Toutes les animations : [Animations](animations.md).

### Dague

{video media/classes/assassin/clips/Melee_1H_Attack_Stab.mp4} **Dague : estoc** | {dev} `Melee_1H_Attack_Stab` | Arme : dague (arbalète au dos) | une fois · 1,60 s | {dev} vitesse calée sur `dagueInstant` (coup à 0,5 s du clip)

### Arbalète en main

{video media/classes/assassin/clips/Ranged_1H_Aiming.mp4} **Arbalète : en main, visée** | {dev} `Ranged_1H_Aiming` | Arme : arbalète à une main | une fois · 1,07 s (tenue en boucle en jeu) | {dev} haut du corps, copie bouclante `Ranged_1H_Aiming_Loop`
{video media/classes/assassin/clips/Ranged_1H_Shoot.mp4} **Arbalète : tir** | {dev} `Ranged_1H_Shoot` | Arme : arbalète à une main | une fois · 1,07 s | {dev} vitesse ×1,2

### Lancer de la grenade

{video media/classes/assassin/clips/Throw.mp4} **Grenade fumigène : lancer** | {dev} `Throw` | Arme : dague et arbalète (en jeu : grenade fumigène) | une fois · 1,37 s

### Marche discrète (passif)

{video media/classes/assassin/clips/Sneaking.mp4} **Marche discrète** | {dev} `Sneaking` | Arme : dague (arbalète au dos) | boucle · 2,13 s | {dev} seconde locomotion, copie bouclante `Sneaking_Loop`

{dev} RB : vide.

## Règles

- Dague en main, arbalète rangée dans le dos {décidé}. Changer d'arme fait passer l'arbalète en main et range la dague dans le dos.

## Passifs {décidé}

- **Marche discrète** : l'assassin ne s'accroupit pas. Il marche discrètement {{dev: (animation KayKit `Sneaking`)}} et passe en **mode furtif**. Un coup porté sans avoir été détecté est un **coup critique**.
- **Coups dans le dos** : tout coup porté dans le dos d'un ennemi est un **coup critique**.
- **Furtif et dans le dos** : les deux se cumulent et donnent le **meilleur critique** du jeu.

| Situation | Coup | Dégâts |
|---|---|---|
| Ni furtif, ni dans le dos | Normal | ×1 |
| Furtif, non détecté | Critique | ×2 |
| Dans le dos | Critique | ×3 |
| Furtif et dans le dos | Meilleur critique | ×5 |

- Multiplicateurs {décidé} : ×2 en furtif, ×3 dans le dos, ×5 pour les deux ensemble. Valeurs {à équilibrer}.
- **Déclenchement** {décidé} : automatique. Hors combat, dès que l'assassin marche sans sprinter, il passe en marche discrète et devient furtif. Il n'y a pas de course distincte : en furtif, il marche à 3,2 m/s. Il faut bouger pour entrer en furtif ; s'arrêter ne le fait pas sortir. Sprinter, attaquer ou être repéré le fait sortir du mode furtif.
- **Détection** {décidé} : un squelette repère l'assassin furtif dans un **cône de vue** devant lui, jusqu'à environ 6 m ; dans son dos, seulement à moins de 1,5 m. Il faut contourner pour frapper. Distances {à équilibrer}.
## Valeurs de départ {à équilibrer}

Version 0.2 {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 100 |
| Dague | 20 dégâts (×2 furtif, ×3 dans le dos, ×5 les deux) ; une seule cible, portée 1,8 m, 45° de part et d'autre de l'avant |
| Marche discrète | 3,2 m/s |
| Retour hors combat | 4 s sans combat |
| Carreau | 45 dégâts, ×2 à la tête, recharge 6 s |

## Style de jeu {décidé}

L'assassin joue **principalement à la dague**. L'arbalète et la grenade sont des outils ponctuels. **Pas d'autre compétence active** : dague, passifs, arbalète et grenade fumigène forment son kit complet.

## Arbalète {décidé}

- Un carreau dans la **tête** est un **coup critique**. C'est le **seul** critique possible à l'arbalète : les passifs de furtivité et de coup dans le dos ne s'appliquent pas aux carreaux.
- **Gros temps de recharge** : l'arbalète ne remplace pas la dague.
- **Vitesse fixe** {décidé} : le carreau vole en cloche comme une flèche, mais sa vitesse est **toujours la même**, rapide : pas de charge. Valeur de départ : **60 m/s** {à équilibrer} ; il touche la tête visée jusqu'à 45 m au moins.
- Temps de recharge : **6 secondes** entre deux carreaux {à équilibrer}.

## Grenade fumigène {décidé}

- L'assassin la lance {{dev: (modèle KayKit `smokebomb`, animation `Throw`)}} ; elle crée un nuage de fumée à l'impact.
- Elle sert à **s'extraire d'un combat**.
- **Furtif dans la fumée** {décidé} : tant qu'il est dans le nuage, les ennemis le perdent de vue et il redevient furtif, même en combat. En sortant, il reste furtif s'il marche, ce qui lui ouvre un coup critique au retour.
- **Une grenade**, qui revient **20 s** après usage ; le nuage dure environ 5 s {à équilibrer}.
