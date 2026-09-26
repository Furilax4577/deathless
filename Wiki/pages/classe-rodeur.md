# Rôdeur

{video media/classes/rodeur/rotation.mp4} **Rendu 3D** | Rotation en attente, arc en main, carquois au dos | {dev} modèle `Ranger`, style `BowQuiver` (flèche encochée masquée au repos), clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/rodeur/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_rodeur}

L'archer. Plus il vise juste et bande fort, plus il fait mal : une flèche chargée dans la tête est un coup critique. Il couvre une zone de sa nuée de flèches et roule en arrière pour garder ses distances.

| Rôle | Arme |
|---|---|
| Distance, précision | Arc et carquois |

{dev} Modèle : le rôdeur KayKit (`Ranger`), style d'arme arc et carquois.

## Actions

| | Touche | Action |
|---|---|---|
| {icone rodeur_visee} | LT / clic droit maintenu | Viser (sans zoom) |
| {icone rodeur_tir} | RT / clic gauche | Bander l'arc pendant la visée, relâcher pour tirer |
| {icone rodeur_nuee_de_fleches} | LB | Nuée de flèches |
| {icone rodeur_roulade_salve} | RB | Roulade arrière avec salve |

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Rôdeur (`Ranger`, style `BowQuiver`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Rodeur_Jeu.controller` (généré par `ClassesBuilder.ControleurRodeur`), déclenché par `ClasseRodeur`. Toutes les animations : [Animations](animations.md).

### Viser, bander et tirer

{video media/classes/rodeur/clips/Ranged_Bow_Draw.mp4} **Tir : bander l'arc** | {dev} `Ranged_Bow_Draw` | Arme : arc et carquois | une fois · 1,33 s | {dev} haut du corps, durée calée sur `arcCharge`
{video media/classes/rodeur/clips/Ranged_Bow_Aiming_Idle.mp4} **Tir : arc bandé, tenu** | {dev} `Ranged_Bow_Aiming_Idle` | Arme : arc et carquois | boucle · 1,83 s | {dev} copie bouclante `Ranged_Bow_Aiming_Idle_Loop`
{video media/classes/rodeur/clips/Ranged_Bow_Release.mp4} **Tir : décocher** | {dev} `Ranged_Bow_Release` | Arme : arc et carquois | une fois · 1,33 s | {dev} vitesse ×1,3

{dev} Viser seul (LT) ne joue pas de clip : les gestes partent quand le rôdeur bande l'arc.

### Nuée de flèches

{video media/classes/rodeur/clips/Ranged_Bow_Draw_Up.mp4} **Nuée : bander vers le ciel** | {dev} `Ranged_Bow_Draw_Up` | Arme : arc et carquois | une fois · 1,33 s | {dev} corps entier, vitesse ×2,2
{video media/classes/rodeur/clips/Ranged_Bow_Release_Up.mp4} **Nuée : décocher vers le ciel** | {dev} `Ranged_Bow_Release_Up` | Arme : arc et carquois | une fois · 1,37 s | {dev} vitesse ×1,2

### Roulade arrière et salve

{video media/classes/rodeur/clips/Dodge_Backward.mp4} **Roulade arrière** | {dev} `Dodge_Backward` | Arme : arc et carquois | une fois · 0,40 s | {dev} esquive arrière commune (`DodgeBack`)

{dev} La salve n'a pas de geste propre : les flèches partent pendant la roulade, 0,08 s après son début.

## Règles

- Arc et carquois {décidé} : au repos, l'arc est tenu le long du corps ; il se lève et se bande pour viser.
- **Compétence 1 : Nuée de flèches** {décidé} : un marqueur apparaît au sol, puis une pluie de flèches tombe sur la zone ciblée.
- **Compétence 2 : Roulade arrière** {décidé} : le rôdeur roule en arrière pour reprendre ses distances et tire en même temps une **salve de flèches devant lui**. Nombre de flèches, écart et dégâts {à équilibrer}.
- **Visée récompensée** {décidé} : un tir plus précis rapporte davantage.
  - **Tir à la tête** : une flèche dans la tête est un **coup critique**.
  - **Viser puis bander** {décidé} : on **maintient le clic droit** (LT) pour viser, **sans zoom** ; le **clic gauche** (RT) bande l'arc, avec une jauge de charge. Plus l'arc est tendu, plus les dégâts sont élevés.
  - **Cercle de charge** : pendant qu'on bande l'arc, un cercle apparaît au bout de la flèche et se réduit en accélérant. Il indique la tension.
  - **Coup prêt** : quand le cercle atteint sa taille minimale, il se verrouille sur la pointe et la flèche brille brièvement : le tir est chargé à fond.
  - **Traînée** : en vol, la flèche laisse une traînée d'air fine, sans lueur, non magique.
- **Flèches non magiques** {décidé} : les flèches, du rôdeur comme toutes les autres, ne brillent pas. Elles laissent une traînée d'air, jamais lumineuse. Seule exception, le bref éclat de la charge complète.
- **Vitesse et portée** {décidé} : les flèches volent en **cloche**, tirées par la pesanteur. Plus une flèche part vite, plus elle va loin et droit. La vitesse dépend de **la force avec laquelle le rôdeur bande son arc** : un tir rapide retombe vite, un tir chargé à fond file loin. Valeurs de départ {à équilibrer} : de **18 m/s** (tir rapide) à **55 m/s** (charge complète) ; salve de la roulade 35 m/s ; pesanteur réelle. Chargée à fond, une flèche reste quasi tendue jusqu'à 30 m (0,6 m au-dessus de la ligne de visée) ; un tir rapide retombe vers 16 à 17 m. Une légère aide relève le tir de 3° au plus ; au-delà, on vise au-dessus.
- **Face à la visée** {décidé} : quand il bande son arc, le rôdeur se tourne vers le point visé, le corps de profil comme un archer, et la flèche part vers le réticule.
- Valeurs de départ : charge complète en **1,2 s** ; **10 dégâts** sans charge, **40** chargé à fond ; tir à la tête **×2** {à équilibrer}.
- **Valeurs de départ** des compétences, version 0.2 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 110 |
| Nuée de flèches | 5 salves de 10 dégâts, recharge 12 s |
| Roulade arrière | recul d'environ 3,7 m, salve de 5 flèches de 15 dégâts, recharge 8 s |

