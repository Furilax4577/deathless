# Style « Hache à deux mains » (viking)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre `Mannequin_Axe2H` (x = 10 ; `Mannequin_Medium.fbx` + sockets `handslot.*` recréés, voir `SwordShield.md`). Asset : `Assets/WeaponStyles/Axe2H.asset`. Contrôleur : `Assets/WeaponStyles/Axe2H.controller`. **Pas de bascule.** Pas de bouclier.

## Repère mesuré sur le modèle

`axe_2handed.fbx` (matériau `KayKit_Barbarian`) : manche selon Y (de −0.43 à 1.29, pivot à la prise), **double fer** symétrique en X (lames à ±0.62, à y ≈ 0.92), épaisseur Z ±0.13. Les deux tranchants sont donc ±X.

## Clips 2H trouvés (`Rig_Medium_CombatMelee`)

`Melee_2H_Idle` (1.07 s), `Melee_2H_Attack_Chop` (1.63 s), `Melee_2H_Attack_Slice` (1.10 s), `Melee_2H_Attack_Stab` (1.60 s), `Melee_2H_Attack_Spin` (2.40 s), `Melee_2H_Attack_Spinning` (0.67 s). Pas de marche/course ni de garde 2H : `Walking_A`, `Running_A`, `Melee_Blocking` (clip de bouclier) sont utilisés.

## Pièce attachée et prise

| Pièce | Modèle | Os | Position | Rotation |
|---|---|---|---|---|
| Hache 2H | `axe_2handed.fbx` | `handslot.r` | (0, 0, 0) | **(0, 0, 0)** |

Pourquoi l'identité et pas une valeur « manche horizontal devant, tranchant vers le sol » comme pour la hache à une main :

- Il existe un idle 2H, `Melee_2H_Idle`, et il **tient l'arme à deux mains** : en identité, le manche part de la main droite vers l'avant-gauche-haut ((−0.83, 0.34, 0.45)) et la **main gauche est à 0.59 m le long du manche, 0.15 m hors de son axe**. Forcer le manche horizontal vers l'avant enverrait la main gauche à 0.8 m du manche. La direction du manche est donc dictée par le clip, pas par un euler.
- Avec les lames à ±X, l'identité met déjà une lame vers le **sol-avant** (−X → (0.17, −0.61, 0.78)) et l'autre vers le ciel-arrière : la règle « tranchant vers le sol » est satisfaite.
- En `Idle_A` (idle à une main, dans `extraClips`), l'identité donne exactement la tenue validée pour la hache à une main : manche horizontal vers l'avant, une lame vers le sol.

**Position de la main gauche** (`handslot.l`) par rapport au manche, en identité : `Melee_2H_Idle` 0.59 m le long / 0.15 m hors axe ; `Melee_2H_Attack_Chop` (t = 0.3) 0.36 m / 0.13 m ; `Melee_2H_Attack_Slice` (t = 0.3) 0.53 m / 0.12 m. La main gauche « accompagne » le manche à 12–15 cm mais ne le saisit pas exactement (les clips KayKit sont faits pour un manche générique) : **pour une prise exacte dans Deathless il faudra un IK main gauche (cible = point du manche à 0.4–0.6 m de la poignée) ou un second socket sur le manche**. En `Walking_A`, `Running_A` et `Melee_Blocking` (clips à une main), la main gauche est loin (0.8–1.0 m) : l'arme est tenue d'une main, c'est attendu.

## Compatibilité prise / attaques (impact avec le tranchant)

Alignement de la lame (±X) et du plat (Z) avec la vitesse du fer, échantillonné tous les 0.1 s :

| Clip | Phase rapide | cos(X, vitesse) | cos(Z, vitesse) | Conclusion |
|---|---|---|---|---|
| `Melee_2H_Attack_Chop` | t = 0.7–0.8 s, 10–19 m/s | −0.89 / −0.83 | −0.03 / +0.48 | la lame −X mène : impact avec le tranchant |
| `Melee_2H_Attack_Slice` | t = 0.4 s, 25 m/s | −0.98 | +0.21 | la lame −X mène : impact avec le tranchant |

## Contrôleur `Axe2H.controller`

`Locomotion` (arbre 1D `Speed` : `Melee_2H_Idle` bouclé (`Clips/Melee_2H_Idle_Loop.anim`) 0, `Walking_A` 0.5, `Running_A` 1) ; `Attack_Chop` (trigger `Attack`, Any State) ; `Attack_Slice` (trigger `Attack2`, Any State) ; `Guard` (bool `Guard`, `Melee_Blocking_Loop`) ; `BlockHit` (trigger `BlockHit`). Autres clips dans l'asset : `Melee_2H_Attack_Stab`, `Melee_2H_Attack_Spin`, `Melee_2H_Attack_Spinning`, `Idle_A`.

## Captures (Assets/Screenshots/)

`Axe2H_idle_3q.png`, `Axe2H_idle_front.png` (`Melee_2H_Idle`), `Axe2H_walk_3q.png`, `Axe2H_run_3q.png`, `Axe2H_attack1_strike_3q.png` (`Chop`, t = 0.75), `Axe2H_attack2_strike_3q.png` (`Slice`, t = 0.4), `Axe2H_guard_3q.png` (`Melee_Blocking`). Vue d'ensemble : `Overview_all_styles.png`.

## À reporter dans Deathless

Nouveau style : `handslot.r` + `axe_2handed` (0, 0, 0), clips ci-dessus ; prévoir un IK main gauche pour la prise à deux mains ; la garde `Melee_Blocking` est un geste de bouclier (à remplacer par une garde 2H si un clip ou une pose dédiée est créée).
