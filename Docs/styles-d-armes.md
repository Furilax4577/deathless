# Styles d'armes — récapitulatif (validés le 24/09/2026)

Source : bac à sable `sandbox-rig` (scène `RigTest.unity`, fiches détaillées `Assets/WeaponStyles/*.md`, captures). Reporté dans ce projet le 24/09/2026 : mêmes chemins et mêmes GUID, banc de vérification `Assets/Scenes/WeaponBench.unity` (sept personnages en idle, x = −5 → 10).

## Règles communes

- **Squelette** : KayKit Rig_Medium (pack Adventurers 2.0 et Character Animations 1.1). Clips génériques (Generic, sans avatar), liés par chemin d'os.
- **Sockets** : `handslot.r` / `handslot.l`, enfants de `hand.r` / `hand.l`, position (0, 0.096, −0.057), rotation (0, 0, 270) / (0, 0, 90). Natifs sur tous les personnages Adventurers et Skeletons ; **animés par les clips** (c'est l'animation qui oriente l'arme). Le `Mannequin_Medium` du pack Animations n'en a pas : les recréer avec ces valeurs (écart mesuré 0 avec le Knight).
- **Attache** : instancier le FBX de l'arme sous l'os, position/rotation/échelle locales = valeurs du style (mécanisme `ClassGear.AttachTo` de Relic). Rotation identité sauf indication.
- **Données** : un asset `WeaponStyle` par style (`Assets/WeaponStyles/*.asset`, script `Assets/Scripts/WeaponStyle.cs`) : pièces (os, position, euler, pose alternative), sockets requis, clips idle/marche/course/attaques/garde/extra, contrôleur, script de bascule.
- **Clips bouclés** : le pack n'est jamais modifié ; les clips qui doivent boucler sont copiés dans `Assets/WeaponStyles/Clips/*_Loop.anim` (`Melee_Blocking`, `Ranged_Bow_Aiming_Idle`, `Ranged_1H_Aiming`, `Melee_2H_Idle`). `Assets/Editor/KayKitImportSettings.cs` boucle déjà `Idle_*`, `Walking_*`, `Running_*`.
- **Contrôleurs minimaux** : `Locomotion` = arbre 1D sur `Speed` (idle 0, `Walking_A` 0.5, course 1), puis états d'attaque / garde / visée propres au style.

## Tableau des styles

| Style | Pièces (os, position, euler) | Idle / marche / course | Attaques | Garde / états | Script | Contrôleur / asset |
|---|---|---|---|---|---|---|
| **Épée + bouclier** (chevalier / paladin) | `sword_1handed` → `handslot.r` (0,0,0)/(0,0,0) ; `shield_round` (ou `_square`) → `handslot.l` (0,0,0)/(0,0,0) | `Idle_A` / `Walking_A` / `Running_A` | `Melee_1H_Attack_Slice_Diagonal` (+ `_Chop`, `_Slice_Horizontal`, `_Stab`) | `Melee_Blocking_Loop` (bool `Guard`), `Melee_Block_Hit` | — | `SwordShield.controller` / `.asset` |
| **Bâton** (mage de feu) | `staff` → `handslot.r` (0,0,0)/(0,0,0) | `Idle_A` / `Walking_A` / `Running_A` | `Ranged_Magic_Shoot` (trigger `Attack`), `Ranged_Magic_Spellcasting_Long` (trigger `Cast`) ; dispo : `Spellcasting`, `Raise`, `Summon` | — | — | `Staff.controller` / `.asset` |
| **Dague + arbalète dans le dos** (rogue / assassin) | dague → `handslot.r` (0,0,0)/**(0,180,0)** (horizontale, pointe avant, tranchant vers le sol) ; arbalète rangée → `chest` (0.05,−0.323,−0.5)/(305,270,90) ; **switch** : arbalète → `handslot.r` (0,0,0)/**(0,271,0)**, dague rangée → `chest` (0.12,0.15,−0.35)/(0,0,135) | `Idle_A` / `Walking_A` / `Running_A` | dague `Melee_1H_Attack_Stab` (la pointe mène l'estoc) ; arbalète `Ranged_1H_Shoot` puis `Ranged_1H_Reload` | `CrossbowAim` = `Ranged_1H_Aiming_Loop` (bool `Crossbow`) | `AltWeaponSwitch` (bool `Crossbow`, reparentage + 0.15 s) | `DaggerCrossbow.controller` / `.asset` |
| **Hache à une main + bouclier** (viking) | `axe_1handed` → `handslot.r` (0,0,0)/**(0,180,0)** (manche horizontal devant, fer à l'avant, tranchant vers le sol) ; `shield_round` → `handslot.l` (0,0,0)/(0,0,0) | `Idle_A` / `Walking_A` / `Running_A` | `Melee_1H_Attack_Chop` (impact t ≈ 0.58 s avec le tranchant, cos +0.75…+1.00 ; identité frapperait du dos) | `Melee_Blocking_Loop`, `Melee_Block_Hit` | — | `AxeShield.controller` / `.asset` |
| **Hache à deux mains** (viking) | `axe_2handed` → `handslot.r` (0,0,0)/**(0,0,0)** (double fer ±X ; la direction du manche est dictée par l'idle 2H) | `Melee_2H_Idle_Loop` / `Walking_A` / `Running_A` | `Melee_2H_Attack_Chop` (trigger `Attack`), `Melee_2H_Attack_Slice` (trigger `Attack2`) ; dispo : `_Stab`, `_Spin`, `_Spinning` | `Melee_Blocking_Loop` (geste de bouclier, faute de garde 2H) | — | `Axe2H.controller` / `.asset` |
| **Arc + carquois** (archer) | `bow_withString` → `handslot.l` (0,0,0)/**(286,183,357)** au repos, **(0,0,180)** en visée ; `arrow_bow` → `handslot.r` (0.163,0.523,−0.047)/(288,106,75), visible seulement en visée ; `quiver` → `chest` (0.06,0,−0.33)/(0,0,−45) | `Idle_A` / `Walking_A` / `Running_HoldingBow` | `Ranged_Bow_Draw` → `Ranged_Bow_Release` (trigger `Shoot`) | `Aiming_Idle` = `Ranged_Bow_Aiming_Idle_Loop` (bool `Aiming`) ; blendshape `Draw` de l'arc : monte pendant `Draw`, 100 en `Aiming_Idle`, 0 sinon | `BowStance` (bool `Aiming`, 0.15 s) | `BowQuiver.controller` / `.asset` |

## Pièce articulée : visière du casque (Knight)

`Knight_HelmetVisor` est un SkinnedMeshRenderer séparé, skinné à 100 % sur `head`, modélisé relevé. Script `HelmetVisor` (bool `open`, 0.2 s) : charnière `VisorHinge` (enfant de `head`) substituée à `head` dans `bones`, pivot **(0, 0.566, 0.068)** dans `head` (= (0, 1.79, 0.07) modèle, rivets des tempes), axe X, relevée 0°, **abaissée 41°**. Même mécanisme que `HeadGear.cs` de Relic.

## Points ouverts

- **Hache 2H** : les clips `Melee_2H_*` posent la main gauche à 12–15 cm du manche sans le saisir : prévoir un **IK main gauche** (cible à 0.4–0.6 m de la poignée) ou un second socket sur le manche. Garde empruntée au bouclier.
- **Arc** : **bascule repos ↔ visée obligatoire** (`BowStance`) : deux eulers, flèche visible seulement en visée, blendshape `Draw` à synchroniser. Le bug d'orientation de Relic venait de son euler de repos (0, 270, 285.2).
- **Dague / arbalète** : switch par reparentage (`AltWeaponSwitch`) ; le cooldown du switch reste à faire côté jeu. Relic portait la dague lame vers le bas (285.2, 180, 270) : remplacé par (0, 180, 0).
- **Écarts avec Relic à reporter dans ses assets si Relic est repris** : hache 1H (0,0,65) → (0,180,0) ; dague (285.2,180,270) → (0,180,0) ; arbalète (0,273,0) → (0,271,0) ; fourreau `hips` → dos `chest` ; arc repos (0,270,285.2) → (286,183,357) ; flèche avec position locale non nulle.
- Non traités : livre du mage, variantes futures (griffes, épées de feu), armures, cooldowns, réseau.
