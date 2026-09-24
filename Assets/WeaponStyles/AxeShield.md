# Style « Hache à une main + bouclier » (viking)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre `Mannequin_Axe` (`Mannequin_Medium.fbx` + sockets `handslot.*` recréés, voir `SwordShield.md`), précision utilisateur appliquée (repos : hache horizontale devant, tranchant vers le sol). Asset : `Assets/WeaponStyles/AxeShield.asset`. Contrôleur : `Assets/WeaponStyles/AxeShield.controller`. **Pas de bascule** : la prise de repos convient aussi à l'attaque (mesuré ci-dessous).

## Repère mesuré sur le modèle

`axe_1handed.fbx` : manche selon Y (1.24 m), pivot à la prise, fer vers +X à y ≈ 0.66, **tranchant = +X**.

## Pièces attachées

| Pièce | Modèle | Os | Position | Rotation | Remarque |
|---|---|---|---|---|---|
| Hache | `axe_1handed.fbx` | `handslot.r` | (0, 0, 0) | **(0, 180, 0)** | repos (Idle_A) : manche horizontal vers l'avant (direction du fer (0.26, −0.01, 0.96)), fer à l'avant, **tranchant vers le sol** (0, −1, 0) |
| Bouclier | `shield_round.fbx` | `handslot.l` | (0, 0, 0) | (0, 0, 0) | comme l'épée + bouclier |

Autres valeurs mesurées en Idle_A, pour mémoire : identité → même tenue mais tranchant vers le ciel ; (0, 0, 295) → fer sur l'épaule (ancienne valeur retenue, abandonnée) ; (0, 0, 65) (valeur de `Relic/Viking.asset`) → fer au sol sur ce rig.

## Compatibilité repos / attaque (`Melee_1H_Attack_Chop`)

Alignement du tranchant (+X du modèle) avec la vitesse du fer pendant le clip, échantillonné tous les 0.05 s (impact à t ≈ 0.55–0.60 s, vitesse du fer 16–20 m/s) :

| Prise | t = 0.55 | t = 0.60 | t = 0.70–0.80 |
|---|---|---|---|
| **(0, 180, 0)** | **+0.75** | **+1.00** | +0.5 à +0.9 |
| identité | −1.00 | −0.83 | −0.5 |

Avec (0, 180, 0), le fer frappe **avec le tranchant** ; en identité, avec le dos. La prise de repos est donc aussi la bonne prise d'attaque : aucune bascule nécessaire (contrairement à l'arc). Les variantes `Melee_1H_Attack_Slice_Diagonal` / `Slice_Horizontal` sont à re-vérifier de la même façon si elles sont utilisées.

## Animations (clips KayKit Rig_Medium)

| Rôle | Clip | Fichier |
|---|---|---|
| Idle / marche / course | `Idle_A`, `Walking_A`, `Running_A` | General / MovementBasic |
| Attaque | `Melee_1H_Attack_Chop` (1.07 s) : armé vers t ≈ 0.45 s, impact t ≈ 0.58 s | Rig_Medium_CombatMelee |
| Variantes | `Melee_1H_Attack_Slice_Diagonal`, `Melee_1H_Attack_Slice_Horizontal` | Rig_Medium_CombatMelee |
| Garde | `Melee_Blocking` (copie bouclante `Clips/Melee_Blocking_Loop.anim`) ; coup encaissé `Melee_Block_Hit` | Rig_Medium_CombatMelee |

Contrôleur : même structure que `SwordShield.controller` (`Locomotion` / `Attack` (Any State, trigger `Attack`) / `Guard` (bool `Guard`) / `BlockHit` (trigger)).

## Captures (Assets/Screenshots/)

`AxeShield_idle_3q.png`, `AxeShield_idle_front.png`, `AxeShield_walk_3q.png`, `AxeShield_chop_windup_3q.png` (t = 0.45), `AxeShield_chop_strike_3q.png` (t = 0.58, impact), `AxeShield_guard_3q.png`.

## À reporter dans Deathless

`Viking.asset` : `rightHandEuler` (hache) (0, 0, 65) → **(0, 180, 0)** ; bouclier inchangé ; clip `Melee_1H_Attack_Chop` (Relic utilise `Melee_2H_Attack_Chop` pour le viking : à harmoniser avec le style « une main », et à re-mesurer si le clip 2H est conservé).
