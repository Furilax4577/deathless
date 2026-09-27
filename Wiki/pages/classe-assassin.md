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
| {icone assassin_pas_ombre} | RB | Pas de l'ombre (bond derrière l'ennemi visé) {décidé, 27/09/2026} |
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

### Pas de l'ombre

{dev} `Dodge_Forward` (roulade avant commune) accéléré sur la durée du bond (état `PasOmbre`, déclencheur du même nom, corps entier), traînée de gemmes du thème Ombre semée le long du trajet (`ClasseAssassin.EffetPasOmbre`), petit éclat Ombre à chaque exécution.

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
| Pas de l'ombre | 7 m en 0,15 s, arrêt 1 m derrière la cible visée, invulnérable pendant, recharge 6 s (remise à zéro par une exécution) |
| Exécution | ennemi commun sous 30 % de vie achevé ; élite ou boss : ×3 |

## Style de jeu {décidé}

L'assassin joue **principalement à la dague**. L'arbalète et la grenade sont des outils ponctuels. Depuis le 27/09/2026, une compétence active de plus, le **Pas de l'ombre**, et un passif, l'**Exécution** (ci-dessous) : retour de Quentin, « il est trop lent pour attraper un ennemi dans le dos ». La furtivité reste son ouverture d'angle : rester furtif sans bouger continue de préparer le premier coup.

## Pas de l'ombre et Exécution {décidé}

Décidés le 27/09/2026 (audit d'équilibrage, `Docs/equilibrage-classes.md`) : le coup dans le dos ×3 reste la récompense, on donne à l'assassin le moyen d'y arriver.

- **Pas de l'ombre** (RB) : un bond de **7 m** en 0,15 s dans la direction visée, **invulnérable pendant le bond**, qui traverse les ennemis mais pas les murs. Si le réticule est sur un ennemi, le bond s'arrête **1 m derrière lui, face à son dos** : le coup suivant est un coup dans le dos. Recharge **6 s** {à équilibrer}. Le bond ne fait pas sortir du mode furtif ; attaquer, oui.
- **Exécution** (passif de la dague) : un coup de dague sur un ennemi commun (sbire, guerrier, voleur, mage) sous **30 % de vie** l'**achève net** ; sur un élite ou un boss, le coup fait **×3** sans achever. Chaque exécution **recharge le Pas de l'ombre** aussitôt : bond → dos → exécution → bond, tant qu'il y a des blessés. Seuil {à équilibrer}.
- {dev} Le bond passe par le propriétaire comme l'esquive (chemin `Diffuser` pour le visuel chez les autres postes, effet du thème Ombre) ; l'exécution passe par le chemin des coups (l'hôte fait foi sur la vie). Animation du bond : la roulade accélérée ou `Dodge_Forward`, avec une traînée de gemmes Ombre.
- {{dev: Fait le 27/09/2026 (`ClasseAssassin.PasDeLOmbre`, `GameBalance.pasOmbre*`, `execution*`). Cible : l'ennemi sous le réticule (`Combat.PointVise`, portée `pasOmbrePorteeCible` 9 m) s'il est à portée du bond ; sinon bond libre de 7 m vers la visée. Murs : `SphereCast` (rayon 0,35 m, ennemis et héros ignorés), arrêt 0,5 m avant. À l'arrivée derrière une cible, le corps **et la caméra** se tournent vers elle (sinon le coup suivant partirait vers l'ancienne visée). L'exécution passe dans `InfoDegats.execution` : l'hôte revérifie le seuil sur sa propre vie (`EnnemiReseau.FrapperRpc`) et achève net un ennemi commun ; sur un élite ou un boss, le coup vaut le **meilleur** des deux facteurs (dos ×3 ou exécution ×3, pas le produit). Mot « Exécuté » au-dessus du chiffre (`DegatsUI`). Icône `assassin_pas_ombre` (`generer_icones.py`), catalogue `ClassesJouables`, 4e amélioration de l'arbre « Pas léger » (−12 % de recharge par rang).}}

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
