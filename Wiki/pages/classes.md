# Classes

Cinq classes jouables, et bientôt le **druide** et le **mécanicien**. Chaque classe a un style d'arme, et ses compétences sont réparties sur trois emplacements au plus. Aucune classe n'a d'ultime {décidé}. Les rôles, les armes et leurs animations sont fixés {décidé}. En multijoueur, **chaque classe est unique** : jamais deux joueurs sur la même classe {décidé}.

| | Classe | Style d'arme | Rôle |
|---|---|---|---|
| {icone classe_paladin} | [Paladin](classe-paladin.md) | Épée et bouclier | Tank, mêlée vers l'avant |
| {icone classe_mage_feu} | [Mage](classe-mage.md) | Bâton | Distance, zone ; style feu pour l'instant |
| {icone classe_rodeur} | [Rôdeur](classe-rodeur.md) | Arc et carquois | Distance, précision |
| {icone classe_assassin} | [Assassin](classe-assassin.md) | Dague, arbalète dans le dos | Furtif, coups critiques |
| {icone classe_viking} | [Viking](classe-viking.md) | Hache à deux mains | Mêlée, zone |
| {icone classe_druide} | [Druide](classe-druide.md) | **Bientôt** | Prochaine version |
| {icone classe_mecanicien} | [Mécanicien](classe-mecanicien.md) | **Bientôt** | Prochaine version |
| {icone classe_barde} | [Barde](classe-barde.md) | **Bientôt** | Soutien, luth |
| {icone classe_bavaroise} | [Bavaroise](classe-bavaroise.md) | **Bientôt** | Mêlée, chopes de bière |
| {icone classe_clochard} | [Clochard](classe-clochard.md) | **Bientôt** | Contrôle, bouteille et gaz |

Chaque classe a sa page : présentation, actions et touches, règles détaillées.

## Projectiles {décidé}

Flèches et carreaux ont une **vitesse** et subissent la **pesanteur** : plus un projectile part vite, plus il va loin. La vitesse de l'arc dépend de la charge ; celle de l'arbalète est fixe. Voir [Rôdeur](classe-rodeur.md) et [Assassin](classe-assassin.md).

## Actions communes

| | Touche | Action |
|---|---|---|
| {icone commun_esquive} | B | Esquive, roulade |
| {icone commun_potion_soin} | Croix directionnelle haut | Boire une potion de soin (vendue par le druide, voir [Le village](village.md)) |
| {icone commun_coup_critique} | | Coup critique : tête, dos, furtivité selon la classe |

Toutes les touches : voir [Commandes](commandes.md).

## Menu du personnage et points de compétence

- **Menu du personnage** {décidé} : la touche **Tab** (Y à la manette, Triangle) ouvre un menu avec **le personnage** (vie, endurance, jauge, vitesse, nuits survécues, ennemis tués), **l'inventaire** (vide pour l'instant) et **l'amélioration des compétences**. La partie continue pendant qu'il est ouvert.
- **Points de compétence** {décidé} : **1 point par jour survécu**, crédité à l'aube. Le HUD rappelle les points à dépenser au-dessus du portrait.
- **Premier arbre** {à confirmer} (proposition du 26/09/2026) : une amélioration par action, **3 rangs**, **1 point par rang**.

| Classe | Amélioration | Par rang |
|---|---|---|
| Paladin | Épée affûtée | +10 % de dégâts à l'épée |
| | Garde solide | −15 % d'endurance par coup bloqué |
| | Bélier infatigable | −12 % de recharge de la charge bélier |
| | Soin fervent | +20 % de vie rendue par le soin |
| Viking | Hache lourde | +10 % de dégâts à la hache |
| | Tourbillon | −15 % de rage consommée par l'attaque tournante |
| | Cri de guerre | −12 % de recharge du rugissement |
| | Chute brutale | +15 % de dégâts du saut percutant |
| Mage | Brasier | +10 % de dégâts de la boule de feu |
| | Souffle économe | −12 % de mana consommé par le cône de flammes |
| | Source de mana | +20 % de régénération du mana |
| Rôdeur | Pointes d'acier | +10 % de dégâts des flèches |
| | Main sûre | −10 % de temps pour bander l'arc à fond |
| | Nuée drue | −12 % de recharge de la nuée de flèches |
| | Salve fournie | +1 flèche dans la salve de la roulade |
| Assassin | Lame empoisonnée | +10 % de dégâts à la dague |
| | Rechargement vif | −12 % de recharge de l'arbalète |
| | Fumée épaisse | −12 % de recharge de la grenade fumigène |

{{dev: Arbre dans `Assets/Scripts/Jeu/Heros/ArbreCompetences.cs` ; points et rangs dans `EtatJoueur` ; écran `EcranPersonnage`.}}

## Icônes {décidé}

Gemmes low poly à facettes, validées le 25/09/2026 : un emblème dans un hexagone coupé en diagonale pour chaque classe, un pictogramme par action. Le soin est blanc et or, le vert reste à Nyxessa. {{dev: Sources et script de génération : `ArtSources/Icones/` ; planche de revue : `Docs/icones/planche.html`.}}
