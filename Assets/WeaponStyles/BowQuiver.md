# Style « Arc + carquois » (archer)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre `Mannequin_Bow` (`Mannequin_Medium.fbx` + sockets `handslot.*` recréés, voir `SwordShield.md`). Asset : `Assets/WeaponStyles/BowQuiver.asset`. Contrôleur : `Assets/WeaponStyles/BowQuiver.controller`. Script de bascule : `Assets/Scripts/BowStance.cs`.

**Contrairement à l'épée, ce style a besoin d'une bascule repos ↔ visée** : l'arc n'a pas la même rotation locale dans `handslot.l` au repos et en visée.

## Repères mesurés sur les modèles

- `bow_withString.fbx` (SkinnedMeshRenderer sans os, 1 blendshape `Draw` = corde tendue) : **Z = axe des branches**, **+X = côté corde** (corde à x ≈ 0.35, `Draw` la tire vers +X), **−X = dos en bois**. Pivot à la poignée.
- `arrow_bow.fbx` : axe Z, pivot au milieu, **pointe = +Z**, empennage = −Z.
- `quiver.fbx` : longueur selon Y, ouverture vers +Y ; pas de texture référencée dans le FBX → matériau `KayKit_Ranger` assigné.

## Pourquoi deux rotations

En rotation identité, le socket gauche (réglé pour le bouclier) présente l'arc retourné de 180° autour de ses branches dans tous les clips (corde vers la cible en visée, bois vers le ciel en Idle_A). (0, 0, 180) corrige la visée et donne, en Idle_A, un arc horizontal en travers des jambes : **rejeté** par l'utilisateur. L'idle validé est l'arc **le long du corps, branches selon l'axe avant-arrière, corde vers le ciel**, ce qui demande (286, 183, 357) en Idle_A — et cette rotation casse la visée. D'où la bascule.

## Pièces attachées (valeurs retenues)

| Pièce | Modèle | Os | Repos (Idle_A, Walking_A, Running_HoldingBow) | Visée (Aiming_Idle, Draw, Release) |
|---|---|---|---|---|
| Arc | `bow_withString.fbx` | `handslot.l` | pos (0, 0, 0), rot **(286, 183, 357)** | pos (0, 0, 0), rot **(0, 0, 180)** |
| Flèche | `arrow_bow.fbx` | `handslot.r` | **masquée** | pos **(0.163, 0.523, −0.047)** (encoche dans la main), rot **(288, 106, 75)** (pointe de la main droite vers la poignée de l'arc) |
| Carquois | `quiver.fbx` | `chest` | pos (0.06, 0, −0.33), rot (0, 0, −45) | idem |

`BowStance` : lit le bool `Aiming` de l'Animator, interpole (Slerp, SmoothStep) la rotation locale de l'arc entre repos et visée en **0.15 s**, montre la flèche quand la bascule dépasse 50 %, et pilote le blendshape `Draw` de l'arc : 0 au repos, monte avec le temps normalisé de l'état `Draw`, 100 dans `Aiming_Idle`, 0 dans `Release`.

## Contrôleur `BowQuiver.controller`

| État | Clip | Entrée | Sortie |
|---|---|---|---|
| `Locomotion` (défaut) | arbre 1D `Speed` : `Idle_A` 0, `Walking_A` 0.5, `Running_HoldingBow` 1 | | `Aiming` vrai → `Aiming_Idle` |
| `Aiming_Idle` | `Ranged_Bow_Aiming_Idle` (copie bouclante `Clips/Ranged_Bow_Aiming_Idle_Loop.anim`) | `Aiming` | `Aiming` faux → `Locomotion` ; déclencheur `Shoot` → `Draw` |
| `Draw` | `Ranged_Bow_Draw` (1.33 s) | `Shoot` | fin (90 %) → `Release` |
| `Release` | `Ranged_Bow_Release` (1.33 s) | | fin (90 %) → `Aiming_Idle` si `Aiming`, sinon `Locomotion` |

Paramètres : `Speed` (float), `Aiming` (bool), `Shoot` (trigger). Autres clips disponibles non utilisés : `Ranged_Bow_Idle` (idle « prêt », arc devant), `Ranged_Bow_Draw_Up` / `Release_Up` (tir en cloche).

## Captures (Assets/Screenshots/)

`BowFinal_idle_3q.png` (Idle_A, repos), `BowFinal_walk_3q.png` (Walking_A, repos), `BowFinal_aim_3q.png` (Aiming_Idle, visée, corde tendue), `BowFinal_draw_3q.png` (Draw), `BowFinal_release_3q.png` (Release). Référence validée par l'utilisateur : `BowFix_IdleA_variant_alongBody_3q.png` (idle) et `BowFix_Draw_3q.png` (bandé).

## À reporter dans Deathless

- `Ranger.asset` : `rightHandEuler` (arc, main gauche) → (286, 183, 357) ; `aimWeaponEuler` → (0, 0, 180) (inchangé) ; flèche : `aimOffhandEuler` → (288, 106, 75) **avec** une position locale (0.163, 0.523, −0.047) (`ClassGear`/`PlayerVisual` posent aujourd'hui la position à zéro : ajouter un décalage) ; `aimSeconds` reste nécessaire (bascule).
- Blendshape `Draw` : 100 pendant la visée tenue, à ramener à 0 au tir.
