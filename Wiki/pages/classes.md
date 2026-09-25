# Classes

Cinq classes jouables. Chaque classe a un style d'arme, et ses compétences sont réparties sur trois emplacements au plus, plus une ultime pour certaines.

| Classe | Style d'arme | Rôle |
|---|---|---|
| Paladin | Épée et bouclier | Tank, une cible à la fois |
| Mage de feu | Bâton | Distance, zone |
| Rôdeur | Arc et carquois | Distance, précision |
| Assassin | Dague, arbalète dans le dos | Furtif, coups critiques |
| Viking | Hache à deux mains | Mêlée, zone |

Les rôles, les armes et leurs animations sont {décidé}.

## Paladin {décidé}

- Épée et bouclier. La visière du casque s'abaisse et se relève.
- **Garde et parade** : l'attaque secondaire lève le bouclier. Déclenchée au bon moment face à un coup, la garde devient une parade {décidé}.
- **Charge bélier** : le paladin s'élance d'environ 7 m, enveloppé d'une tête de bélier en gemmes dorées qui le précède, et percute à l'arrivée {effet validé}.
  - **Au bout de la trajectoire** : la cible percutée est **étourdie longuement** {décidé}.
  - **Sur le chemin** : les ennemis traversés sont **repoussés sur les côtés** et brièvement étourdis {décidé}.
  - **Dégâts à l'impact** : **proportionnels à la distance parcourue** : une charge courte fait peu de dégâts, une charge complète fait le maximum {décidé}.
  - Valeurs de départ : étourdissement final 2,5 s, repoussés 0,6 s et 2,5 m sur le côté, dégâts de 15 à 60 selon la distance {à équilibrer}.
- **Soin sur soi** : aura de croix vertes qui montent autour du paladin {effet validé}.
- La poussée au bouclier et les valeurs chiffrées sont {à confirmer}.

## Mage de feu

- Bâton. L'attaque de base est une **boule de feu** qui explose à l'impact, puis laisse une fumée à facettes qui se dissipe {effet validé}.
- **Cône de flammes** maintenu devant le mage {effet validé}.
- **Brûlure** : les ennemis touchés brûlent pendant un moment {effet validé}.
- **Mana** {décidé} : jauge de 100. Elle remonte d'environ 1 par seconde, plus un bonus à chaque ennemi touché par la boule de feu. Les compétences coûtent du mana ; le cône de flammes en consomme tant qu'il est maintenu. Valeurs {à équilibrer}.
- **Pas d'ultime** {décidé} : le mage garde la boule de feu, le cône de flammes et la brûlure.
- Valeurs chiffrées : {à confirmer}.

## Rôdeur

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
- Durée de charge, dégâts minimum et maximum, multiplicateur de critique : {à confirmer}.
- Valeurs chiffrées des compétences : {à confirmer}.

## Assassin

- Dague en main, arbalète rangée dans le dos {décidé}. Changer d'arme fait passer l'arbalète en main et range la dague dans le dos.

### Passifs {décidé}

- **Marche discrète** : l'assassin ne s'accroupit pas. Il marche discrètement {{dev: (animation KayKit `Sneaking`)}} et passe en **mode furtif**. Un coup porté sans avoir été détecté est un **coup critique**.
- **Coups dans le dos** : tout coup porté dans le dos d'un ennemi est un **coup critique**.
- **Furtif et dans le dos** : les deux se cumulent et donnent le **meilleur critique** du jeu.

| Situation | Coup |
|---|---|
| Ni furtif, ni dans le dos | Normal |
| Furtif, non détecté | Critique |
| Dans le dos | Critique |
| Furtif et dans le dos | Meilleur critique |

- Multiplicateurs : {à confirmer}. {{dev: Pour mémoire, Relic utilisait ×3 dans le dos et ×2 en furtivité, cumulables.}}
- **Déclenchement** {décidé} : automatique. Hors combat, dès que l'assassin marche sans courir ni sprinter, il passe en marche discrète et devient furtif. Courir, attaquer ou être repéré le fait sortir du mode furtif.
- Portée de détection des ennemis : {à confirmer}.
### Style de jeu {décidé}

L'assassin joue **principalement à la dague**. L'arbalète et la grenade sont des outils ponctuels. **Pas d'autre compétence active** : dague, passifs, arbalète et grenade fumigène forment son kit complet.

### Arbalète {décidé}

- Un carreau dans la **tête** est un **coup critique**. C'est le **seul** critique possible à l'arbalète : les passifs de furtivité et de coup dans le dos ne s'appliquent pas aux carreaux.
- **Gros temps de recharge** : l'arbalète ne remplace pas la dague.
- Durée exacte du temps de recharge : {à confirmer}.

### Grenade fumigène {décidé}

- L'assassin la lance {{dev: (modèle KayKit `smokebomb`, animation `Throw`)}} ; elle crée un nuage de fumée à l'impact.
- Elle sert à **s'extraire d'un combat**.
- **Furtif dans la fumée** {décidé} : tant qu'il est dans le nuage, les ennemis le perdent de vue et il redevient furtif, même en combat. En sortant, il reste furtif s'il marche, ce qui lui ouvre un coup critique au retour.
- Nombre de grenades et recharge : {à confirmer}.

## Viking

- Hache à deux mains **uniquement** {décidé}. Pas de bouclier.
- **Attaque tournante** {décidé} : le viking tourne sur lui-même, hache tendue, et frappe tout autour de lui {{dev: (animations KayKit `Melee_2H_Attack_Spin` et `Melee_2H_Attack_Spinning`)}}. Son emplacement et sa durée sont {à confirmer}.
- **Rugissement** : un crâne de barbare casqué en gemmes rouges surgit au-dessus du viking, rugit, et une onde part de lui puis revient comme pour dire « venez » {effet validé}.
- **Saut percutant** : le viking bondit d'environ 5 m vers l'avant et frappe le sol, une onde de terre part du point d'impact {effet validé}.
- **Rage** {décidé} : jauge de 100. Elle monte quand le viking frappe et redescend lentement hors combat. Les compétences du viking coûtent de la rage. Valeurs {à équilibrer}.
- Valeurs chiffrées des compétences : {à confirmer}.
