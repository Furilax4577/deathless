# Style « Épée + bouclier » (chevalier / paladin)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur le mannequin neutre KayKit (`Mannequin_Medium.fbx`, pack Character Animations 1.1) et recoupé sur `Knight.fbx` (pack Adventurers 2.0). Squelette **Rig_Medium**. Asset équivalent : `Assets/WeaponStyles/SwordShield.asset` (ScriptableObject `WeaponStyle`).

## Points d'attache (sockets KayKit)

Les rigs Adventurers ont deux os terminaux prévus pour les armes, enfants de `hand.r` / `hand.l` :

| Socket | Parent | Position locale | Rotation locale (Euler) |
|---|---|---|---|
| `handslot.r` | `hand.r` | (0, 0.096, −0.057) | (0, 0, 270) |
| `handslot.l` | `hand.l` | (0, 0.096, −0.057) | (0, 0, 90) |

- **Les clips KayKit animent ces sockets** (courbes sur `Rig_Medium/root/hips/spine/chest/upperarm.r/lowerarm.r/wrist.r/hand.r/handslot.r`, idem à gauche) : c'est l'animation qui oriente l'arme (pendante en attente, levée en marche, devant en garde). Il ne faut donc **aucune rotation de correction** sur l'arme.
- `Mannequin_Medium.fbx` (pack Animations) **n'a pas** ces sockets : ils ont été ajoutés à la main sous `hand.r` / `hand.l` avec les valeurs ci-dessus. L'Animator les lie par chemin et les anime comme les natifs. Vérification numérique en pose Idle_A (repère du personnage) : écart 0.0000 m / 0.00° entre les sockets ajoutés du mannequin et les sockets natifs du Knight.
- Règle de compatibilité pour Deathless : un personnage est compatible s'il a le squelette Rig_Medium **et** les deux `handslot.*` (natifs sur tous les Adventurers et Skeletons ; à créer avec ces valeurs sur un rig qui n'en a pas).

## Pièces attachées

| Pièce | Modèle | Os | Position | Rotation | Échelle |
|---|---|---|---|---|---|
| Épée | `KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/sword_1handed.fbx` | `handslot.r` | (0, 0, 0) | (0, 0, 0) | 1 |
| Bouclier | `.../shield_round.fbx` (idem `shield_square`, `shield_badge`, `shield_spikes` : même pivot) | `handslot.l` | (0, 0, 0) | (0, 0, 0) | 1 |

Repères des modèles : la lame de l'épée est selon +Y du modèle (pivot à la garde, manche vers −Y) ; le bouclier est un disque dans le plan XY, normale +Z, pivot au centre (poignée). Avec la rotation identité dans le socket, la prise est correcte dans toutes les poses testées. Ces valeurs sont **identiques à celles de `Relic/Assets/Settings/Classes/Knight.asset`** (`rightHandEuler` = `leftHandEuler` = 0), ce qui confirme la mécanique de Relic (`ClassGear.AttachTo` : instanciation sous l'os, position 0, rotation Euler, échelle).

## Animations (clips KayKit Rig_Medium, `KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/`)

| Rôle | Clip | Fichier | Boucle | Remarque |
|---|---|---|---|---|
| Idle | `Idle_A` | Rig_Medium_General | oui (postprocesseur) | épée pendante pointe vers l'avant-bas, bouclier sur l'avant-bras gauche |
| Marche | `Walking_A` | Rig_Medium_MovementBasic | oui | épée levée devant le torse |
| Course | `Running_A` | Rig_Medium_MovementBasic | oui | |
| Attaque principale | `Melee_1H_Attack_Slice_Diagonal` (1.00 s) | Rig_Medium_CombatMelee | non | armé au-dessus de la tête vers t≈0.3 s, frappe diagonale vers t≈0.55 s (c'est le clip du chevalier dans Relic) |
| Attaques alternatives | `Melee_1H_Attack_Chop` (1.07 s), `Melee_1H_Attack_Slice_Horizontal` (1.37 s), `Melee_1H_Attack_Stab` (1.60 s) | Rig_Medium_CombatMelee | non | variantes possibles pour un combo |
| Garde | `Melee_Blocking` (1.07 s) | Rig_Medium_CombatMelee | **non dans le pack** → copie bouclante `Assets/WeaponStyles/Clips/Melee_Blocking_Loop.anim` | bouclier levé devant, épée horizontale à la ceinture |
| Coup encaissé | `Melee_Block_Hit` (1.07 s) | Rig_Medium_CombatMelee | non | |
| Autres du même fichier, non retenus | `Melee_Block`, `Melee_Block_Attack`, `Melee_1H_Attack_Jump_Chop`, `Melee_2H_*`, `Melee_Dualwield_*`, `Melee_Unarmed_*` | | | pas d'idle « 1H » dans le pack (seulement `Melee_2H_Idle`, `Melee_Unarmed_Idle`) : Idle_A est le bon idle |

Contrôleur minimal : `Assets/WeaponStyles/SwordShield.controller` — état par défaut `Locomotion` (arbre de mélange 1D sur `Speed` : Idle_A à 0, Walking_A à 0.5, Running_A à 1), `Attack` (déclencheur `Attack`, depuis Any State, retour à Locomotion à 90 %), `Guard` (booléen `Guard`, Melee_Blocking_Loop), `BlockHit` (déclencheur `BlockHit` depuis Guard, retour à Guard). Posé sur `Mannequin` et `Knight_Ref` dans la scène.

## Captures (Assets/Screenshots/)

- Idle : `SwordShield_idle_front-1.png` (face), `SwordShield_idle_3q.png` (trois-quarts), `SwordShield_idle_compare_knight.png` (mannequin + Knight côte à côte, même pose)
- Marche : `SwordShield_walk_front.png`, `SwordShield_walk_3q.png` ; course : `SwordShield_run_3q.png`
- Attaque : `SwordShield_attack_windup_3q.png` (armé, t = 0.3 s), `SwordShield_attack_strike_3q.png` et `SwordShield_attack_strike_front.png` (frappe, t = 0.55 s)
- Garde : `SwordShield_guard_front.png`, `SwordShield_guard_3q.png`
- `bindpose_front.png` : pose de bind (T-pose) avant animation, pour référence

Poses obtenues par `AnimationClip.SampleAnimation` en mode édition (pas de Play mode), caméra à 4.5 m, champ 35°.

## À reporter dans Deathless

- Style = { socket main droite `handslot.r` + `sword_1handed` (0,0,0) ; socket main gauche `handslot.l` + `shield_*` (0,0,0) ; clips ci-dessus }.
- Le personnage n'apporte rien d'autre que son squelette : aucun réglage par personnage n'a été nécessaire (Knight et mannequin donnent la même pose au millimètre).
- Reste à valider par l'utilisateur : la lecture des captures (prise « correcte » au sens artistique), et la convergence avec les miniatures/GIF de l'outil de catalogage (méthode, point 3).
