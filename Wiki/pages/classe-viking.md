# Viking

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

