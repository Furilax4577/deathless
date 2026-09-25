# Assassin

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
- **Déclenchement** {décidé} : automatique. Hors combat, dès que l'assassin marche sans courir ni sprinter, il passe en marche discrète et devient furtif. Courir, attaquer ou être repéré le fait sortir du mode furtif.
- **Détection** {décidé} : un squelette repère l'assassin furtif dans un **cône de vue** devant lui, jusqu'à environ 6 m ; dans son dos, seulement à moins de 1,5 m. Il faut contourner pour frapper. Distances {à équilibrer}.
## Valeurs de départ {à équilibrer}

Version 0.2 {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 100 |
| Dague | 20 dégâts (×2 furtif, ×3 dans le dos, ×5 les deux) |
| Marche discrète | 3,2 m/s |
| Retour hors combat | 4 s sans combat |
| Carreau | 45 dégâts, ×2 à la tête, recharge 6 s |

## Style de jeu {décidé}

L'assassin joue **principalement à la dague**. L'arbalète et la grenade sont des outils ponctuels. **Pas d'autre compétence active** : dague, passifs, arbalète et grenade fumigène forment son kit complet.

## Arbalète {décidé}

- Un carreau dans la **tête** est un **coup critique**. C'est le **seul** critique possible à l'arbalète : les passifs de furtivité et de coup dans le dos ne s'appliquent pas aux carreaux.
- **Gros temps de recharge** : l'arbalète ne remplace pas la dague.
- **Vitesse fixe** {décidé} : le carreau vole en cloche comme une flèche, mais sa vitesse est **toujours la même**, rapide : pas de charge. Vitesse {à équilibrer}.
- Temps de recharge : **6 secondes** entre deux carreaux {à équilibrer}.

## Grenade fumigène {décidé}

- L'assassin la lance {{dev: (modèle KayKit `smokebomb`, animation `Throw`)}} ; elle crée un nuage de fumée à l'impact.
- Elle sert à **s'extraire d'un combat**.
- **Furtif dans la fumée** {décidé} : tant qu'il est dans le nuage, les ennemis le perdent de vue et il redevient furtif, même en combat. En sortant, il reste furtif s'il marche, ce qui lui ouvre un coup critique au retour.
- **Une grenade**, qui revient **20 s** après usage ; le nuage dure environ 5 s {à équilibrer}.
