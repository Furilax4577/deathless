# Style « Dague + arbalète dans le dos » (rogue / assassin)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre `Mannequin_Dagger` (`Mannequin_Medium.fbx` + sockets `handslot.*` recréés, voir `SwordShield.md`), retours utilisateur appliqués (prise inversée, rangement dans le dos). Asset : `Assets/WeaponStyles/DaggerCrossbow.asset`. Contrôleur : `Assets/WeaponStyles/DaggerCrossbow.controller`. Script de switch : `Assets/Scripts/AltWeaponSwitch.cs`.

**Bascule nécessaire** (switch dague ↔ arbalète), contrairement à l'épée : deux positions par pièce, toutes deux sur le même socket de dos `chest`. Le cooldown du switch n'est pas géré ici.

## Repères mesurés sur les modèles

- `dagger.fbx` : lame selon +Y (1.21 m au total, pivot à la garde), épaisseur selon Z.
- `crossbow_1handed.fbx` : axe de tir selon **+Z** (arc large en X côté +Z, crosse en −Z), épaisseur selon Y, pivot à la poignée.

## Positions (deux états, bool `Crossbow` de l'Animator)

| Pièce | Modèle | `Crossbow` = faux (dague en main) | `Crossbow` = vrai (arbalète en main) |
|---|---|---|---|
| Dague | `dagger.fbx` | `handslot.r`, pos (0, 0, 0), rot **(0, 180, 0)** : horizontale, pointe vers l'avant (direction (0.26, −0.01, 0.96) en Idle_A), tranchant +X vers le sol — même logique que la hache. La lame est symétrique en X (double tranchant, X ∈ [−0.139, 0.118]) : l'identité donne le même résultat visuel. Relic (`Assassin.asset`) utilise (285.2, 180, 270) (lame vers le bas), rejeté par l'utilisateur | `chest`, pos **(0.12, 0.15, −0.35)**, rot **(0, 0, 135)** : en diagonale dans le dos, poignée au-dessus de l'épaule droite, pointe vers la hanche gauche, plat de la lame contre le dos |
| Arbalète | `crossbow_1handed.fbx` | `chest`, pos **(0.05, −0.323, −0.5)**, rot **(305, 270, 90)** : en diagonale dans le dos, arc vers l'épaule gauche | `handslot.r`, pos (0, 0, 0), rot **(0, 271, 0)** (calculée : +Z vers l'avant, Y vers le haut en `Ranged_1H_Aiming` ; Relic (0, 273, 0)) |

- Les deux pièces ne sont jamais dans le dos en même temps : aucun croisement dans les deux états (captures `DaggerXbow_idle_back` = arbalète au dos, `DaggerXbow_aim_back` = dague au dos). Plus de fourreau à la hanche.
- `AltWeaponSwitch` : lit le bool `Crossbow`, reparente les deux pièces (`SetParent(bone, true)`) puis interpole position et rotation locales vers la pose cible en **0.15 s** (SmoothStep). Le champ `sheathBone` vaut désormais `chest` (`sheathPosition` / `sheathEuler` = pose de la dague au dos). `Place(bool, instant)` pose l'état sans Play mode.

## Clip d'attaque avec la prise « pointe vers l'avant »

Alignement de la pointe (+Y du modèle) avec la vitesse de la pointe, échantillonné tous les 0.1 s :

| Clip | Phase rapide | cos(pointe, vitesse) | Décision |
|---|---|---|---|
| `Melee_1H_Attack_Stab` (1.60 s) | t ≈ 0.4 s, 14.7 m/s | **+0.99** : la pointe mène l'estoc | **retenu** (état `Stab`) |
| `Melee_1H_Attack_Slice_Diagonal` (1.00 s) | t ≈ 0.4 s, 36 m/s | −0.15 : la lame balaie de travers (taillade) | alternative (`extraClips`) |
| `Melee_1H_Attack_Chop` | coup descendant | — | alternative (`extraClips`) |

(Avec l'ancienne prise inversée (285.2, 180, 270), `Stab` pointait la lame vers l'arrière ; c'est ce qui avait fait retenir `Slice_Diagonal`.)

## Contrôleur `DaggerCrossbow.controller`

| État | Clip | Entrée | Sortie |
|---|---|---|---|
| `Locomotion` (défaut) | arbre 1D `Speed` : `Idle_A` 0, `Walking_A` 0.5, `Running_A` 1 | | `Attack` et non `Crossbow` → `Stab` ; `Crossbow` → `CrossbowAim` |
| `Stab` | `Melee_1H_Attack_Stab` | | fin (90 %) → `Locomotion` |
| `CrossbowAim` | `Ranged_1H_Aiming` (copie bouclante `Clips/Ranged_1H_Aiming_Loop.anim`) | `Crossbow` | non `Crossbow` → `Locomotion` ; `Attack` → `CrossbowShoot` |
| `CrossbowShoot` | `Ranged_1H_Shoot` (1.07 s) | | fin → `CrossbowReload` |
| `CrossbowReload` | `Ranged_1H_Reload` (1.17 s) | | fin → `CrossbowAim` |

Paramètres : `Speed` (float), `Crossbow` (bool), `Attack` (trigger).

## Captures (Assets/Screenshots/)

Dague en main : `DaggerXbow_idle_3q.png`, `DaggerXbow_idle_front.png`, `DaggerXbow_idle_back.png` (arbalète au dos), `DaggerXbow_walk_3q.png`, `DaggerXbow_stab_3q.png` (estoc `Stab`, t = 0.42 s). Arbalète en main : `DaggerXbow_aim_3q.png`, `DaggerXbow_shoot_3q.png`, `DaggerXbow_aim_back.png` (dague au dos).

## À reporter dans Deathless

`Assassin.asset` : `rightHandEuler` (285.2, 180, 270) → **(0, 180, 0)** ; `alternateWeaponEuler` (0, 273, 0) → (0, 271, 0) ; `back*` inchangés pour l'arbalète ; **`sheathBoneName` `hips` → `chest`, `sheathPosition` → (0.12, 0.15, −0.35), `sheathEuler` → (0, 0, 135)** ; clip d'attaque `Melee_1H_Attack_Stab` inchangé (les `aimSeconds` / bascule « lame vers l'avant pendant le coup » de Relic deviennent inutiles).
