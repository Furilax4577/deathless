# Paladin

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

## Règles

- Épée et bouclier. La visière du casque s'abaisse et se relève.
- **Épée plus mobile** {décidé} (26/09/2026) : un peu plus de portée, et un **angle d'attaque vers l'avant** qui touche plusieurs ennemis en face, **moins large que la hache du Viking**. Chaque attaque **avance d'un pas** (sauf s'il y a déjà un ennemi au contact), et le paladin **se déplace plus vite en garde**.
- **Garde et parade** : l'attaque secondaire lève le bouclier. Déclenchée au bon moment face à un coup, la garde devient une parade {décidé}.
- **Charge bélier** : le paladin s'élance d'environ 7 m, enveloppé d'une tête de bélier en gemmes dorées qui le précède, et percute à l'arrivée {effet validé}.
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
| Parade | fenêtre de 0,25 s, l'attaquant est étourdi 1 s |
| Charge bélier | recharge 14 s |
| Soin | +25 % de la vie, recharge 30 s |
- La poussée au bouclier et les valeurs chiffrées sont {à confirmer}.
