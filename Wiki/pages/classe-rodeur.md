# Rôdeur

{icone-grande classe_rodeur}

L'archer. Plus il vise juste et bande fort, plus il fait mal : une flèche chargée dans la tête est un coup critique. Il couvre une zone de sa nuée de flèches et roule en arrière pour garder ses distances.

| Rôle | Arme |
|---|---|
| Distance, précision | Arc et carquois |

{dev} Modèle : le rôdeur KayKit (`Ranger`), style d'arme arc et carquois.

## Actions

| | Touche | Action |
|---|---|---|
| {icone rodeur_tir} | RT | Bander l'arc, relâcher pour tirer |
| {icone rodeur_visee} | LT | Viser |
| {icone rodeur_nuee_de_fleches} | LB | Nuée de flèches |
| {icone rodeur_roulade_salve} | RB | Roulade arrière avec salve |

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Règles

- Arc et carquois {décidé} : au repos, l'arc est tenu le long du corps ; il se lève et se bande pour viser.
- **Compétence 1 : Nuée de flèches** {décidé} : un marqueur apparaît au sol, puis une pluie de flèches tombe sur la zone ciblée.
- **Compétence 2 : Roulade arrière** {décidé} : le rôdeur roule en arrière pour reprendre ses distances et tire en même temps une **salve de flèches devant lui**. Nombre de flèches, écart et dégâts {à équilibrer}.
- **Visée récompensée** {décidé} : un tir plus précis rapporte davantage.
  - **Tir à la tête** : une flèche dans la tête est un **coup critique**.
  - **Arc bandé** : maintenir l'attaque bande l'arc, avec une jauge de charge. Plus l'arc est tendu, plus les dégâts sont élevés.
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

