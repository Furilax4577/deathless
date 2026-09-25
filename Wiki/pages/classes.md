# Classes

Cinq classes jouables. Chaque classe a un style d'arme, et ses compétences sont réparties sur trois emplacements au plus, plus une ultime pour certaines.

| Classe | Style d'arme | Rôle |
|---|---|---|
| Paladin | Épée et bouclier | Tank, une cible à la fois |
| Mage de feu | Bâton | Distance, zone |
| Rôdeur | Arc et carquois | Distance, précision |
| Assassin | Dague, arbalète dans le dos | Furtif, coups critiques |
| Viking | Hache à deux mains | Mêlée, zone |

Les rôles sont {à confirmer}. Les armes et leurs animations sont {décidé}.

## Paladin {décidé}

- Épée et bouclier. La visière du casque s'abaisse et se relève.
- **Garde et parade** : l'attaque secondaire lève le bouclier. Déclenchée au bon moment face à un coup, la garde devient une parade {décidé}.
- **Charge bélier** : le paladin s'élance d'environ 7 m, enveloppé d'une tête de bélier en gemmes dorées, et percute à l'arrivée {effet validé}.
- **Soin sur soi** : aura de croix vertes qui montent autour du paladin {effet validé}.
- La poussée au bouclier et les valeurs chiffrées sont {à confirmer}.

## Mage de feu

- Bâton. L'attaque de base est une **boule de feu** qui explose à l'impact {effet validé}.
- **Cône de flammes** maintenu devant le mage {effet validé}.
- **Brûlure** : les ennemis touchés brûlent pendant un moment {effet validé}.
- Jauge de mana, ultime et valeurs chiffrées : {à confirmer}.

## Rôdeur

- Arc et carquois {décidé} : au repos, l'arc est tenu le long du corps ; il se lève et se bande pour viser.
- **Compétence 1 : Nuée de flèches** {décidé} : une pluie de flèches sur une zone ciblée.
- **Visée récompensée** {décidé} : un tir plus précis doit rapporter davantage. La façon de le mesurer est {à confirmer}.
- Autres compétences et valeurs chiffrées : {à confirmer}.

## Assassin

- Dague en main, arbalète rangée dans le dos {décidé}. Changer d'arme fait passer l'arbalète en main et range la dague dans le dos.

### Passifs {décidé}

- **Marche discrète** : l'assassin ne s'accroupit pas. Il marche discrètement (animation KayKit `Sneaking`) et passe en **mode furtif**. Un coup porté sans avoir été détecté est un **coup critique**.
- **Coups dans le dos** : tout coup porté dans le dos d'un ennemi est un **coup critique**.
- **Furtif et dans le dos** : les deux se cumulent et donnent le **meilleur critique** du jeu.

| Situation | Coup |
|---|---|
| Ni furtif, ni dans le dos | Normal |
| Furtif, non détecté | Critique |
| Dans le dos | Critique |
| Furtif et dans le dos | Meilleur critique |

- Multiplicateurs : {à confirmer}. Pour mémoire, Relic utilisait ×3 dans le dos et ×2 en furtivité, cumulables.
- **Déclenchement** {décidé} : automatique. Hors combat, dès que l'assassin marche sans courir ni sprinter, il passe en marche discrète et devient furtif. Courir, attaquer ou être repéré le fait sortir du mode furtif.
- Portée de détection des ennemis : {à confirmer}.
### Style de jeu {décidé}

L'assassin joue **principalement à la dague**. L'arbalète et la grenade sont des outils ponctuels.

### Arbalète {décidé}

- Un carreau dans la **tête** est un **coup critique**.
- **Gros temps de recharge** : l'arbalète ne remplace pas la dague.
- Durée exacte du temps de recharge : {à confirmer}.

### Grenade fumigène {décidé}

- L'assassin la lance ; elle crée un nuage de fumée.
- Elle sert à **s'extraire d'un combat**.
- **Furtif dans la fumée** {décidé} : tant qu'il est dans le nuage, les ennemis le perdent de vue et il redevient furtif, même en combat. En sortant, il reste furtif s'il marche, ce qui lui ouvre un coup critique au retour.
- Nombre de grenades et recharge : {à confirmer}.

## Viking

- Hache à deux mains **uniquement** {décidé}. Pas de bouclier.
- **Attaque tournante** {décidé} : le viking tourne sur lui-même, hache tendue, et frappe tout autour de lui (animations KayKit `Melee_2H_Attack_Spin` et `Melee_2H_Attack_Spinning`). Son emplacement et sa durée sont {à confirmer}.
- **Rugissement** : un crâne de barbare casqué en gemmes rouges surgit au-dessus du viking, rugit, et une onde part de lui puis revient comme pour dire « venez » {effet validé}.
- **Saut percutant** : le viking bondit d'environ 5 m vers l'avant et frappe le sol, une onde de terre part du point d'impact {effet validé}.
- Jauge de rage et valeurs chiffrées : {à confirmer}.
