# Paladin

{video media/classes/paladin/rotation.mp4} **Rendu 3D** | Rotation en attente, épée et bouclier en main | {dev} modèle `Knight`, style `SwordShield`, clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/paladin/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_paladin}

Le rempart du village. Il tient la ligne au bouclier, charge pour ouvrir un passage et se soigne seul. Son épée frappe devant lui, jusqu'à trois ennemis en face.

| Rôle | Arme |
|---|---|
| Tank, mêlée vers l'avant | Épée et bouclier |

{dev} Modèle : le chevalier KayKit (`Knight`), style d'arme `SwordShield`.

## Actions

| | Touche | Action |
|---|---|---|
| {icone paladin_epee} | RT | Épée |
| {icone paladin_garde} | LT | Garde, et parade au bon moment |
| {icone paladin_charge_belier} | LB | Charge bélier |
| {icone paladin_soin} | RB | Soin sur soi |
Répartition de la charge et du soin sur LB et RB : celle de la version 0.1 {à confirmer}.

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Paladin (`Knight`, style `SwordShield`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Paladin_Jeu.controller` (généré par `JeuBuilder.ControleurPaladin`), déclenché par `ClassePaladin`. Toutes les animations : [Animations](animations.md).

### Épée

Les deux tailles alternent à chaque coup.

{video media/classes/paladin/clips/Melee_1H_Attack_Slice_Horizontal.mp4} **Épée : taille horizontale (1er coup)** | {dev} `Melee_1H_Attack_Slice_Horizontal` | Arme : épée et bouclier | une fois · 1,37 s | {dev} vitesse `epeeVitesseClip` ×1,25
{video media/classes/paladin/clips/Melee_1H_Attack_Slice_Diagonal.mp4} **Épée : taille en diagonale (2e coup)** | {dev} `Melee_1H_Attack_Slice_Diagonal` | Arme : épée et bouclier | une fois · 1,00 s | {dev} vitesse `epeeVitesseClip`

### Garde et parade

{video media/classes/paladin/clips/Melee_Blocking.mp4} **Garde : bouclier levé** | {dev} `Melee_Blocking` | Arme : épée et bouclier | boucle · 1,07 s | {dev} haut du corps, copie bouclante `Melee_Blocking_Loop`
{video media/classes/paladin/clips/Melee_Block_Hit.mp4} **Garde : coup bloqué ou paré** | {dev} `Melee_Block_Hit` | Arme : épée et bouclier | une fois · 1,07 s | {dev} haut du corps, vitesse ×1,6

### Charge bélier

Le paladin court derrière son bouclier, penché en avant : les jambes courent, le haut du corps tient la garde, puis le coup de bouclier porte à l'arrivée.

{video media/classes/paladin/clips/Running_A.mp4} **Charge bélier : course (jambes)** | {dev} `Running_A` | Arme : épée et bouclier | boucle · 0,80 s | {dev} couche de base, figée sur sa première image pendant l'anticipation ; cadence = vitesse de la ruée ÷ vitesse des pieds du clip (mesurée par le builder), bornée de ×0,8 à ×3 (`chargeCadenceMin` / `chargeCadenceMax`)
{video media/classes/paladin/clips/Melee_Blocking.mp4} **Charge bélier : garde (haut du corps)** | {dev} `Melee_Blocking` | Arme : épée et bouclier | boucle · 1,07 s | {dev} haut du corps, copie bouclante `Melee_Blocking_Loop`, pendant l'anticipation et la ruée
{video media/classes/paladin/clips/Melee_Block_Attack.mp4} **Charge bélier : coup de bouclier** | {dev} `Melee_Block_Attack` | Arme : épée et bouclier | une fois · 1,07 s | {dev} haut du corps, lancé pour que l'impact (main gauche la plus en avant) tombe à l'arrivée ; à l'arrivée, le corps entier finit le geste depuis l'impact

### Soin sur soi

{video media/classes/paladin/clips/Ranged_Magic_Raise.mp4} **Soin sur soi : épée levée** | {dev} `Ranged_Magic_Raise` | Arme : épée et bouclier | une fois · 2,10 s | {dev} soin donné à `soinIncantation`

## Règles

- Épée et bouclier. La visière du casque s'abaisse et se relève.
- **Épée plus mobile** {décidé} (26/09/2026) : un peu plus de portée, et un **angle d'attaque vers l'avant** qui touche plusieurs ennemis en face, **moins large que la hache du Viking**. Chaque attaque **avance d'un pas** (sauf s'il y a déjà un ennemi au contact), et le paladin **se déplace plus vite en garde**.
- **Garde et parade** : l'attaque secondaire lève le bouclier. Déclenchée au bon moment face à un coup, la garde devient une parade {décidé}.
- **Jauge de parade et parade parfaite** {décidé} (26/09/2026, principe ; valeurs {à équilibrer}) :
  - **Jauge** : quand un ennemi prépare un coup qui vise le paladin, une petite jauge apparaît sous le réticule, du début de la préparation jusqu'à l'impact. Un curseur avance vers l'instant de l'impact, au bord droit. Plusieurs coups à la fois : la jauge suit celui qui porte le plus tôt. Voir [Interface](interface.md#jauge-de-parade).
  - **Deux fenêtres** sont marquées avant l'impact : la **fenêtre de parade**, large (les 0,35 s avant l’impact), et la **fenêtre parfaite**, beaucoup plus serrée (les 0,1 s juste avant), en or.
  - **Parade** : lever la garde (LT) dans la fenêtre de parade pare le coup, comme avant : aucun dégât, aucune endurance dépensée, l'attaquant est étourdi 1 s.
  - **Parade parfaite** : lever la garde dans la fenêtre parfaite pare le coup et lance aussitôt un **coup de bouclier en avance** : un court bond avant (0,7 m) et le coup de bouclier de la charge bélier. Il **repousse** les ennemis dans un cône devant lui (2,5 m, 60° de part et d'autre de l'avant ; l'attaquant aussi, s'il est tout près) de 2 m et les **étourdit** 0,8 s (statut [Étourdi](statuts.md)). Pendant le coup de bouclier, les autres coups venus de devant sont parés aussi. Éclat de parade renforcé en gemmes or et blanc, son plus marqué, léger tremblement de la caméra.
  - Trop tôt (avant la fenêtre de parade) : le coup est seulement bloqué par la garde et coûte de l'endurance.
  - {dev} Code : `ParadeParfaite.cs`, `TelegraphieCoups.cs`, `ClassePaladin` ; valeurs `paradeFenetre`, `paradeParfaite*`, `paradeJaugeDuree` dans `GameBalance`. Multijoueur : le joueur juge sa parade sur l'impact qu'il voit, l'hôte la valide et applique la repousse (`Docs/reseau.md`, « Parade parfaite »).
- **Charge bélier** : le paladin s'élance d'environ 7 m, enveloppé d'une tête de bélier en gemmes dorées qui le précède, et percute à l'arrivée {effet validé}.
  - **Il court derrière son bouclier** {décidé} (26/09/2026) : jambes en course, bouclier levé, corps penché en avant, coup de bouclier à l'arrivée (comme le banc des effets). Les autres joueurs voient le même geste.
  - **Au bout de la trajectoire** : la cible percutée est **étourdie longuement** {décidé}.
  - **Sur le chemin** : les ennemis traversés sont **repoussés sur les côtés** et brièvement étourdis {décidé}.
  - **Dégâts à l'impact** : **proportionnels à la distance parcourue** : une charge courte fait peu de dégâts, une charge complète fait le maximum {décidé}.
  - Valeurs de départ : étourdissement final 2,5 s, repoussés 0,6 s et 2,5 m sur le côté, dégâts de 15 à 60 selon la distance {à équilibrer}.
- **Soin sur soi** : aura de croix qui montent autour du paladin {effet validé}. Couleur : aura **blanc chaud et or**, mais **les croix restent vertes** {décidé} (exception voulue par Quentin) ; l'effet est à recolorer {{dev: (palette `Soin`)}}.
- **Valeurs de départ** de la version 0.1 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 150 |
| Endurance | 100, +15 par seconde après 1 s sans effort |
| Vitesse | 5 m/s, sprint ×1,6 ; ×0,7 en garde ; ×0,4 pendant l'attaque |
| Esquive | 4 m, 25 d'endurance, invulnérable 0,3 s |
| Épée | 30 dégâts toutes les 0,75 s, portée 2,6 m, 40° de part et d'autre de l'avant (hache du Viking : 70°), 3 ennemis au plus par coup, pas en avant de 0,6 m |
| Garde | un coup bloqué coûte de l'endurance |
| Parade | fenêtre de 0,35 s, l'attaquant est étourdi 1 s |
| Parade parfaite | fenêtre de 0,1 s avant l'impact ; coup de bouclier : bond de 0,7 m, cône de 2,5 m et 60°, repousse de 2 m, étourdit 0,8 s |
| Charge bélier | recharge 14 s |
| Soin | +25 % de la vie, recharge 30 s |
- La poussée au bouclier et les valeurs chiffrées sont {à confirmer}.
