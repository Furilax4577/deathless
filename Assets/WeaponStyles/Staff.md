# Style « Bâton » (mage de feu)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre `Mannequin_Staff` (`Mannequin_Medium.fbx` + sockets `handslot.*` recréés, voir `SwordShield.md`). Asset : `Assets/WeaponStyles/Staff.asset`. Contrôleur : `Assets/WeaponStyles/Staff.controller`. **Pas de bascule** (une seule pose, comme l'épée).

## Pièce attachée

| Pièce | Modèle | Os | Position | Rotation | Échelle |
|---|---|---|---|---|---|
| Bâton | `KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/staff.fbx` | `handslot.r` | (0, 0, 0) | **(0, 0, 0)** | 1 |

Repère du modèle : manche selon Y (2.15 m de haut, pivot à la prise, cristal vers +Y). Identique à `Relic/Assets/Settings/Classes/Mage.asset` (`rightHandEuler` = 0). Le livre de l'autre main (`spellbook_*`, `leftHandEuler` (270, 90, 0), échelle 0.5 dans Relic) n'a pas été testé ici : hors périmètre « bâton ».

## Animations (clips KayKit Rig_Medium)

| Rôle | Clip | Fichier | Remarque |
|---|---|---|---|
| Idle / marche / course | `Idle_A`, `Walking_A`, `Running_A` | General / MovementBasic | bâton pendant, cristal vers l'avant-bas |
| Attaque (tir) | `Ranged_Magic_Shoot` (0.93 s) | Rig_Medium_CombatRanged | bâton pointé horizontalement vers la cible vers t ≈ 0.45 s |
| Incantation | `Ranged_Magic_Spellcasting_Long` (2.53 s) | Rig_Medium_CombatRanged | bâton levé en diagonale, cristal en haut |
| Autres disponibles | `Ranged_Magic_Spellcasting` (0.67 s), `Ranged_Magic_Raise` (2.10 s), `Ranged_Magic_Summon` (4.30 s) | Rig_Medium_CombatRanged | listés dans `Staff.asset.attacks` |

Contrôleur : `Locomotion` (arbre 1D `Speed` : Idle_A 0, Walking_A 0.5, Running_A 1), `Attack` (déclencheur `Attack`, depuis Any State, retour à 90 %), `Cast` (déclencheur `Cast`, depuis Locomotion, retour à 90 %).

## Captures (Assets/Screenshots/)

`Staff_idle_front.png`, `Staff_idle_3q.png`, `Staff_walk_3q.png`, `Staff_attack_3q.png`, `Staff_attack_front.png`, `Staff_cast_3q.png`.

## À reporter dans Deathless

Style = { `handslot.r` + `staff` (0,0,0) ; clips ci-dessus }. Rien à changer par rapport à `Mage.asset` pour le bâton.
