# Animations

Toutes les animations KayKit du projet, jouées sur le mannequin du pack Character Animations 1.1 (rig Medium, celui des héros et des squelettes) ou sur Morgrim, le Golem squelette (rig Large). Quand un clip est fait pour un type d'arme, le mannequin porte l'arme KayKit correspondante, posée par le style d'arme validé quand il existe (voir `Docs/styles-d-armes.md`). 161 animations en 15 familles, plus 5 ouvertures de coffre, 28,7 Mo de vidéos.

> Chaque carte donne le nom en français, le nom technique du clip (celui du code et des contrôleurs), l'arme portée, et si le clip est une boucle ou se joue une fois (la vidéo marque alors une courte pause au début et à la fin). Caméra fixe de 3/4 ; le sol est quadrillé tous les mètres pour juger des déplacements. Les vidéos se chargent quand elles arrivent à l'écran.

## Emotes possibles

Clips utilisables pour une roue à emotes : 11 candidats, de quoi remplir une roue de 8 (10 sur le rig Medium). Ils portent l'étiquette {emote possible} dans les familles plus bas. Les packs n'ont ni danse, ni rire, ni applaudissement, ni geste pour pointer : il faudrait les créer.

| Emote proposée | Clip | Remarque |
|---|---|---|
| **Salut** | `Waving` | boucle possible |
| **Acclamation** | `Cheering` |  |
| **Provocation** | `Skeletons_Taunt` | fait pour les squelettes, lisible sur un héros |
| **Grande provocation** | `Skeletons_Taunt_Longer` | version longue de la précédente |
| **S'asseoir** | `Sit_Floor_Down` | enchaîner avec `Sit_Floor_Idle` (boucle) puis `Sit_Floor_StandUp` |
| **Se reposer** | `Lie_Down` | enchaîner avec `Lie_Idle` (boucle) puis `Lie_StandUp` |
| **Pompes** | `Push_Ups` | boucle |
| **Abdos** | `Sit_Ups` | boucle |
| **Boire un coup** | `Use_Item` | avec une chope ou une potion en main |
| **Faire le mort** | `Death_B` | détournement de la mort lente ; se relever avec `Lie_StandUp` |
| **Gonflette** | `Large_Flexing` | rig Large seulement : clip à refaire pour le rig Medium |

**Roue à emotes** {décidé} (26/09/2026, validée par Quentin) : 8 emotes retenues. Salut (`Waving`), Acclamation (`Cheering`), Provocation (`Skeletons_Taunt`), S'asseoir (`Sit_Floor_Down`, puis `Sit_Floor_Idle` en boucle, puis `Sit_Floor_StandUp`), Se reposer (`Lie_Down`, `Lie_Idle` en boucle, `Lie_StandUp`), Pompes (`Push_Ups` en boucle), Boire un coup (`Use_Item`, avec une chope KayKit pleine puis vide) et Faire le mort (`Death_B`, puis `Lie_StandUp`). Écartées : Grande provocation (doublon), Abdos (doublon des pompes), Gonflette (rig Large seulement). Commande et roue : voir Interface.

## Déplacements (13)

{video media/animations/Idle_A.mp4} **Attente** | `Idle_A` | Arme : aucune | boucle · 1,07 s
{video media/animations/Idle_B.mp4} **Attente, variante** | `Idle_B` | Arme : aucune | boucle · 2,13 s
{video media/animations/Walking_A.mp4} **Marche** | `Walking_A` | Arme : aucune | boucle · 1,07 s
{video media/animations/Walking_B.mp4} **Marche, variante** | `Walking_B` | Arme : aucune | boucle · 1,07 s
{video media/animations/Walking_C.mp4} **Marche lente** | `Walking_C` | Arme : aucune | boucle · 1,60 s
{video media/animations/Walking_Backwards.mp4} **Marche arrière** | `Walking_Backwards` | Arme : aucune | boucle · 1,07 s
{video media/animations/Running_A.mp4} **Course** | `Running_A` | Arme : aucune | boucle · 0,80 s
{video media/animations/Running_B.mp4} **Course, variante** | `Running_B` | Arme : aucune | boucle · 0,80 s
{video media/animations/Running_Strafe_Left.mp4} **Course latérale vers la gauche** | `Running_Strafe_Left` | Arme : aucune | boucle · 0,80 s
{video media/animations/Running_Strafe_Right.mp4} **Course latérale vers la droite** | `Running_Strafe_Right` | Arme : aucune | boucle · 0,80 s
{video media/animations/Sneaking.mp4} **Marche furtive** | `Sneaking` | Arme : aucune | boucle · 2,13 s
{video media/animations/Crouching.mp4} **Marche accroupie** | `Crouching` | Arme : aucune | boucle · 1,07 s
{video media/animations/Crawling.mp4} **Ramper** | `Crawling` | Arme : aucune | boucle · 1,07 s

## Sauts et esquives (9)

{video media/animations/Jump_Start.mp4} **Saut, impulsion** | `Jump_Start` | Arme : aucune | une fois · 0,60 s
{video media/animations/Jump_Idle.mp4} **Saut, en l'air** | `Jump_Idle` | Arme : aucune | boucle · 1,07 s
{video media/animations/Jump_Land.mp4} **Saut, réception** | `Jump_Land` | Arme : aucune | une fois · 0,67 s
{video media/animations/Jump_Full_Short.mp4} **Saut complet, court** | `Jump_Full_Short` | Arme : aucune | une fois · 1,17 s
{video media/animations/Jump_Full_Long.mp4} **Saut complet, long** | `Jump_Full_Long` | Arme : aucune | une fois · 2,33 s
{video media/animations/Dodge_Forward.mp4} **Esquive en avant** | `Dodge_Forward` | Arme : aucune | une fois · 0,40 s
{video media/animations/Dodge_Backward.mp4} **Esquive en arrière** | `Dodge_Backward` | Arme : aucune | une fois · 0,40 s
{video media/animations/Dodge_Left.mp4} **Esquive à gauche** | `Dodge_Left` | Arme : aucune | une fois · 0,40 s
{video media/animations/Dodge_Right.mp4} **Esquive à droite** | `Dodge_Right` | Arme : aucune | une fois · 0,40 s

## Mêlée à une main (5)

{video media/animations/Melee_1H_Attack_Chop.mp4} **Coup de haut en bas, arme à une main** | `Melee_1H_Attack_Chop` | Arme : hache à une main et bouclier (style AxeShield) | une fois · 1,07 s
{video media/animations/Melee_1H_Attack_Jump_Chop.mp4} **Coup sauté de haut en bas, arme à une main** | `Melee_1H_Attack_Jump_Chop` | Arme : hache à une main et bouclier (style AxeShield) | une fois · 1,33 s
{video media/animations/Melee_1H_Attack_Slice_Diagonal.mp4} **Taille en diagonale, arme à une main** | `Melee_1H_Attack_Slice_Diagonal` | Arme : épée et bouclier (style SwordShield) | une fois · 1,00 s
{video media/animations/Melee_1H_Attack_Slice_Horizontal.mp4} **Taille horizontale, arme à une main** | `Melee_1H_Attack_Slice_Horizontal` | Arme : épée et bouclier (style SwordShield) | une fois · 1,37 s
{video media/animations/Melee_1H_Attack_Stab.mp4} **Estoc, arme à une main** | `Melee_1H_Attack_Stab` | Arme : épée et bouclier (style SwordShield) | une fois · 1,60 s

## Mêlée à deux mains (6)

{video media/animations/Melee_2H_Idle.mp4} **Garde, arme à deux mains** | `Melee_2H_Idle` | Arme : hache à deux mains (style Axe2H) | boucle · 1,07 s
{video media/animations/Melee_2H_Attack_Chop.mp4} **Coup de haut en bas, arme à deux mains** | `Melee_2H_Attack_Chop` | Arme : hache à deux mains (style Axe2H) | une fois · 1,63 s
{video media/animations/Melee_2H_Attack_Slice.mp4} **Taille, arme à deux mains** | `Melee_2H_Attack_Slice` | Arme : hache à deux mains (style Axe2H) | une fois · 1,10 s
{video media/animations/Melee_2H_Attack_Stab.mp4} **Estoc, arme à deux mains** | `Melee_2H_Attack_Stab` | Arme : hache à deux mains (style Axe2H) | une fois · 1,60 s
{video media/animations/Melee_2H_Attack_Spin.mp4} **Attaque tournante, arme à deux mains** | `Melee_2H_Attack_Spin` | Arme : hache à deux mains (style Axe2H) | une fois · 2,40 s
{video media/animations/Melee_2H_Attack_Spinning.mp4} **Tourbillon continu, arme à deux mains** | `Melee_2H_Attack_Spinning` | Arme : hache à deux mains (style Axe2H) | boucle · 0,67 s

## Deux armes (3)

{video media/animations/Melee_Dualwield_Attack_Chop.mp4} **Coup de haut en bas, deux armes** | `Melee_Dualwield_Attack_Chop` | Arme : deux dagues (dagger dans handslot.r et handslot.l) | une fois · 1,27 s
{video media/animations/Melee_Dualwield_Attack_Slice.mp4} **Taille, deux armes** | `Melee_Dualwield_Attack_Slice` | Arme : deux dagues (dagger dans handslot.r et handslot.l) | une fois · 1,17 s
{video media/animations/Melee_Dualwield_Attack_Stab.mp4} **Estoc, deux armes** | `Melee_Dualwield_Attack_Stab` | Arme : deux dagues (dagger dans handslot.r et handslot.l) | une fois · 1,60 s

## Mains nues (3)

{video media/animations/Melee_Unarmed_Idle.mp4} **Garde, mains nues** | `Melee_Unarmed_Idle` | Arme : aucune | boucle · 1,07 s
{video media/animations/Melee_Unarmed_Attack_Punch_A.mp4} **Coup de poing, mains nues** | `Melee_Unarmed_Attack_Punch_A` | Arme : aucune | une fois · 1,17 s
{video media/animations/Melee_Unarmed_Attack_Kick.mp4} **Coup de pied, mains nues** | `Melee_Unarmed_Attack_Kick` | Arme : aucune | une fois · 0,93 s

## Bouclier (4)

{video media/animations/Melee_Block.mp4} **Lever le bouclier** | `Melee_Block` | Arme : épée et bouclier (style SwordShield) | une fois · 1,07 s
{video media/animations/Melee_Blocking.mp4} **Garde au bouclier, maintenue** | `Melee_Blocking` | Arme : épée et bouclier (style SwordShield) | boucle · 1,07 s
{video media/animations/Melee_Block_Hit.mp4} **Coup encaissé au bouclier** | `Melee_Block_Hit` | Arme : épée et bouclier (style SwordShield) | une fois · 1,07 s
{video media/animations/Melee_Block_Attack.mp4} **Coup de bouclier** | `Melee_Block_Attack` | Arme : épée et bouclier (style SwordShield) | une fois · 1,07 s

## Arc (7)

{video media/animations/Ranged_Bow_Idle.mp4} **Arc, attente arme prête** | `Ranged_Bow_Idle` | Arme : arc et carquois (style BowQuiver, pose BowStance) | boucle · 1,57 s
{video media/animations/Ranged_Bow_Draw.mp4} **Arc, bander** | `Ranged_Bow_Draw` | Arme : arc et carquois (style BowQuiver, pose BowStance) | une fois · 1,33 s
{video media/animations/Ranged_Bow_Aiming_Idle.mp4} **Arc, viser (maintenu)** | `Ranged_Bow_Aiming_Idle` | Arme : arc et carquois (style BowQuiver, pose BowStance) | boucle · 1,83 s
{video media/animations/Ranged_Bow_Release.mp4} **Arc, décocher** | `Ranged_Bow_Release` | Arme : arc et carquois (style BowQuiver, pose BowStance) | une fois · 1,33 s
{video media/animations/Ranged_Bow_Draw_Up.mp4} **Arc, bander vers le ciel** | `Ranged_Bow_Draw_Up` | Arme : arc et carquois (style BowQuiver, pose BowStance) | une fois · 1,33 s
{video media/animations/Ranged_Bow_Release_Up.mp4} **Arc, décocher vers le ciel** | `Ranged_Bow_Release_Up` | Arme : arc et carquois (style BowQuiver, pose BowStance) | une fois · 1,37 s
{video media/animations/Running_HoldingBow.mp4} **Course, arc en main** | `Running_HoldingBow` | Arme : arc et carquois (style BowQuiver, pose BowStance) | boucle · 0,80 s

## Arbalète (9)

{video media/animations/Ranged_1H_Aiming.mp4} **Arbalète à une main, épauler** | `Ranged_1H_Aiming` | Arme : arbalète à une main (style DaggerCrossbow, pose arbalète en main) | une fois · 1,07 s
{video media/animations/Ranged_1H_Shoot.mp4} **Arbalète à une main, tirer** | `Ranged_1H_Shoot` | Arme : arbalète à une main (style DaggerCrossbow, pose arbalète en main) | une fois · 1,07 s
{video media/animations/Ranged_1H_Shooting.mp4} **Arbalète à une main, tir continu** | `Ranged_1H_Shooting` | Arme : arbalète à une main (style DaggerCrossbow, pose arbalète en main) | boucle · 1,60 s
{video media/animations/Ranged_1H_Reload.mp4} **Arbalète à une main, recharger** | `Ranged_1H_Reload` | Arme : arbalète à une main (style DaggerCrossbow, pose arbalète en main) | une fois · 1,17 s
{video media/animations/Ranged_2H_Aiming.mp4} **Arbalète à deux mains, épauler** | `Ranged_2H_Aiming` | Arme : arbalète à deux mains (crossbow_2handed dans handslot.r) | une fois · 1,60 s
{video media/animations/Ranged_2H_Shoot.mp4} **Arbalète à deux mains, tirer** | `Ranged_2H_Shoot` | Arme : arbalète à deux mains (crossbow_2handed dans handslot.r) | une fois · 1,07 s
{video media/animations/Ranged_2H_Shooting.mp4} **Arbalète à deux mains, tir continu** | `Ranged_2H_Shooting` | Arme : arbalète à deux mains (crossbow_2handed dans handslot.r) | boucle · 1,07 s
{video media/animations/Ranged_2H_Reload.mp4} **Arbalète à deux mains, recharger** | `Ranged_2H_Reload` | Arme : arbalète à deux mains (crossbow_2handed dans handslot.r) | une fois · 1,60 s
{video media/animations/Running_HoldingRifle.mp4} **Course, arbalète à deux mains en main** | `Running_HoldingRifle` | Arme : arbalète à deux mains (crossbow_2handed dans handslot.r) | boucle · 0,80 s

## Magie (5)

{video media/animations/Ranged_Magic_Shoot.mp4} **Magie, lancer un projectile** | `Ranged_Magic_Shoot` | Arme : bâton (style Staff) | une fois · 0,93 s
{video media/animations/Ranged_Magic_Spellcasting.mp4} **Magie, incantation continue** | `Ranged_Magic_Spellcasting` | Arme : bâton (style Staff) | boucle · 0,67 s
{video media/animations/Ranged_Magic_Spellcasting_Long.mp4} **Magie, longue incantation** | `Ranged_Magic_Spellcasting_Long` | Arme : bâton (style Staff) | une fois · 2,53 s
{video media/animations/Ranged_Magic_Raise.mp4} **Magie, élever le bâton** | `Ranged_Magic_Raise` | Arme : bâton (style Staff) | une fois · 2,10 s
{video media/animations/Ranged_Magic_Summon.mp4} **Magie, invocation** | `Ranged_Magic_Summon` | Arme : bâton (style Staff) | une fois · 4,30 s

## Réactions et morts (6)

{video media/animations/Hit_A.mp4} **Touché, recul léger** | `Hit_A` | Arme : aucune | une fois · 0,67 s
{video media/animations/Hit_B.mp4} **Touché, recul marqué** | `Hit_B` | Arme : aucune | une fois · 0,87 s
{video media/animations/Death_A.mp4} **Mort, chute rapide en arrière** | `Death_A` | Arme : aucune | une fois · 0,80 s
{video media/animations/Death_A_Pose.mp4} **Mort A, pose finale au sol** | `Death_A_Pose` | Arme : aucune | une fois · 0,03 s
{video media/animations/Death_B.mp4} **Mort, effondrement lent vers l'avant** | `Death_B` | Arme : aucune | une fois · 2,63 s {emote possible}
{video media/animations/Death_B_Pose.mp4} **Mort B, pose finale au sol** | `Death_B_Pose` | Arme : aucune | une fois · 0,03 s

## Interactions et gestes (17)

{video media/animations/Interact.mp4} **Interagir (actionner devant soi)** | `Interact` | Arme : aucune | une fois · 1,30 s
{video media/animations/PickUp.mp4} **Ramasser** | `PickUp` | Arme : aucune | une fois · 1,30 s
{video media/animations/Throw.mp4} **Lancer un objet** | `Throw` | Arme : fiole à lancer (potion_small_red dans handslot.r) | une fois · 1,37 s
{video media/animations/Use_Item.mp4} **Boire une potion** | `Use_Item` | Arme : potion (potion_medium_red dans handslot.r) | boucle · 1,60 s {emote possible}
{video media/animations/Waving.mp4} **Saluer de la main** | `Waving` | Arme : aucune | une fois · 2,13 s {emote possible}
{video media/animations/Cheering.mp4} **Acclamer, poing levé** | `Cheering` | Arme : aucune | une fois · 1,67 s {emote possible}
{video media/animations/Sit_Floor_Down.mp4} **S'asseoir par terre** | `Sit_Floor_Down` | Arme : aucune | une fois · 1,00 s {emote possible}
{video media/animations/Sit_Floor_Idle.mp4} **Assis par terre** | `Sit_Floor_Idle` | Arme : aucune | boucle · 4,00 s
{video media/animations/Sit_Floor_StandUp.mp4} **Se relever du sol** | `Sit_Floor_StandUp` | Arme : aucune | une fois · 1,13 s
{video media/animations/Sit_Chair_Down.mp4} **S'asseoir sur une chaise** | `Sit_Chair_Down` | Arme : aucune | une fois · 0,80 s
{video media/animations/Sit_Chair_Idle.mp4} **Assis sur une chaise** | `Sit_Chair_Idle` | Arme : aucune | boucle · 3,60 s
{video media/animations/Sit_Chair_StandUp.mp4} **Se lever d'une chaise** | `Sit_Chair_StandUp` | Arme : aucune | une fois · 0,80 s
{video media/animations/Lie_Down.mp4} **S'allonger** | `Lie_Down` | Arme : aucune | une fois · 3,00 s {emote possible}
{video media/animations/Lie_Idle.mp4} **Allongé** | `Lie_Idle` | Arme : aucune | boucle · 2,67 s
{video media/animations/Lie_StandUp.mp4} **Se relever (depuis allongé)** | `Lie_StandUp` | Arme : aucune | une fois · 2,33 s
{video media/animations/Push_Ups.mp4} **Pompes** | `Push_Ups` | Arme : aucune | boucle · 1,03 s {emote possible}
{video media/animations/Sit_Ups.mp4} **Abdominaux** | `Sit_Ups` | Arme : aucune | boucle · 1,73 s {emote possible}

## Outils et métiers (28)

Outils du pack RPG Tools Bits dans `handslot.r`, chacun avec sa prise (position et rotation vérifiées sur plusieurs images de chaque clip) : la hache frappe par le fer, tenue au bout du manche ; la pelle se tient à deux mains (la main gauche sur la ligature, le fer entre de 5 cm en terre puis rejette la terre à gauche) ; la scie va et vient lame en avant, dents en bas, la main gauche tenant la pièce. Valeurs dans `PlancheAccessoires.cs` (`PriseHache`, `PrisePelle`, `PriseScie`).

Pêche : **canne provisoire**, générée par script (bâton facetté aux couleurs de l'atlas des outils KayKit, moulinet dont la manivelle suit la main gauche, ligne, bouchon rouge et blanc, flaque et poisson provisoires). Aucune canne dans les packs KayKit FREE ni dans les archives de `Relic/ArtSources` (elle est peut-être dans RPG Tools Bits EXTRA, que nous n'avons pas). Les clips de l'établi et du port d'objet restent joués à mains vides.

{video media/animations/Chopping.mp4} **Bûcheronner, coups répétés** | `Chopping` | Arme : hache de bûcheron (RPG Tools axe dans handslot.r, tenue au bout du manche, tranchant vers l'avant) | boucle · 1,33 s
{video media/animations/Chop.mp4} **Bûcheronner, séquence complète** | `Chop` | Arme : hache de bûcheron (RPG Tools axe dans handslot.r, tenue au bout du manche, tranchant vers l'avant) | une fois · 4,33 s
{video media/animations/Digging.mp4} **Creuser, pelletées répétées** | `Digging` | Arme : pelle (RPG Tools shovel à deux mains, handslot.r en haut du manche, fer en bas, main gauche sur le manche) | boucle · 1,40 s
{video media/animations/Dig.mp4} **Creuser, séquence complète** | `Dig` | Arme : pelle (RPG Tools shovel à deux mains, handslot.r en haut du manche, fer en bas, main gauche sur le manche) | une fois · 4,67 s
{video media/animations/Hammering.mp4} **Marteler, coups répétés** | `Hammering` | Arme : marteau (RPG Tools hammer) | boucle · 2,67 s
{video media/animations/Hammer.mp4} **Marteler, séquence complète** | `Hammer` | Arme : marteau (RPG Tools hammer) | une fois · 4,33 s
{video media/animations/Pickaxing.mp4} **Piocher, coups répétés** | `Pickaxing` | Arme : pioche (RPG Tools pickaxe) | boucle · 3,73 s
{video media/animations/Pickaxe.mp4} **Piocher, séquence complète** | `Pickaxe` | Arme : pioche (RPG Tools pickaxe) | une fois · 6,03 s
{video media/animations/Sawing.mp4} **Scier, va-et-vient** | `Sawing` | Arme : scie (RPG Tools saw, poignée dans handslot.r, lame en avant, dents vers le bas) | boucle · 0,67 s
{video media/animations/Saw.mp4} **Scier, séquence complète** | `Saw` | Arme : scie (RPG Tools saw, poignée dans handslot.r, lame en avant, dents vers le bas) | une fois · 2,40 s
{video media/animations/Lockpicking.mp4} **Crocheter une serrure, en continu** | `Lockpicking` | Arme : crochet (RPG Tools screwdriver_A_short) | boucle · 2,33 s
{video media/animations/Lockpick.mp4} **Crocheter une serrure, séquence complète** | `Lockpick` | Arme : crochet (RPG Tools screwdriver_A_short) | une fois · 3,00 s
{video media/animations/Fishing_Cast.mp4} **Pêche, lancer la ligne** | `Fishing_Cast` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | une fois · 1,93 s
{video media/animations/Fishing_Idle.mp4} **Pêche, attente** | `Fishing_Idle` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | boucle · 2,33 s
{video media/animations/Fishing_Bite.mp4} **Pêche, ça mord** | `Fishing_Bite` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | une fois · 2,23 s
{video media/animations/Fishing_Tug.mp4} **Pêche, ferrer** | `Fishing_Tug` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | une fois · 1,90 s
{video media/animations/Fishing_Reeling.mp4} **Pêche, mouliner** | `Fishing_Reeling` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | boucle · 1,60 s
{video media/animations/Fishing_Struggling.mp4} **Pêche, lutter avec la prise** | `Fishing_Struggling` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | boucle · 3,43 s
{video media/animations/Fishing_Catch.mp4} **Pêche, sortir la prise** | `Fishing_Catch` | Arme : canne à pêche **provisoire** (générée par script : bâton facetté, moulinet, ligne, bouchon) | une fois · 3,23 s
{video media/animations/Holding_A.mp4} **Tenir un objet d'une main** | `Holding_A` | Arme : aucune | boucle · 1,00 s
{video media/animations/Holding_B.mp4} **Tenir un objet à deux mains** | `Holding_B` | Arme : aucune | boucle · 1,00 s
{video media/animations/Holding_C.mp4} **Tenir un objet contre soi** | `Holding_C` | Arme : aucune | boucle · 1,00 s
{video media/animations/Working_A.mp4} **Travail à l'établi A, en continu** | `Working_A` | Arme : aucune | boucle · 1,50 s
{video media/animations/Work_A.mp4} **Travail à l'établi A, séquence complète** | `Work_A` | Arme : aucune | une fois · 2,53 s
{video media/animations/Working_B.mp4} **Travail à l'établi B, en continu** | `Working_B` | Arme : aucune | boucle · 2,53 s
{video media/animations/Work_B.mp4} **Travail à l'établi B, séquence complète** | `Work_B` | Arme : aucune | une fois · 3,10 s
{video media/animations/Working_C.mp4} **Travail à l'établi C, en continu** | `Working_C` | Arme : aucune | boucle · 2,00 s
{video media/animations/Work_C.mp4} **Travail à l'établi C, séquence complète** | `Work_C` | Arme : aucune | une fois · 3,67 s

## Coffres (5)

Ouverture du coffre du donjon, rejouée comme dans le jeu (`DonjonJeu.Ouvrir`) : le couvercle bascule de 105° en 0,45 s, puis 2 pièces d'or montent en tournant (4 pour le grand coffre). Ouvrir un coffre ne coûte jamais d'or {décidé} (Quentin, 26/09/2026 ; plus tard, peut-être une clé). Plus de cadenas (retour de Quentin, 26/09/2026) :

- **coffres sans serrure** (`Coffre`, `GrandCoffre`) : ceux du jeu pour l'instant, ouverture gratuite ; la gâche et le moraillon à trou de serrure du modèle KayKit sont retirés (copies des maillages) ;
- **coffres à clé**, déclinaisons prêtes pour plus tard (`Coffre_Acier`, `_Cuivre`, `_Or`, et `GrandCoffre_*`) : le coffre garde sa serrure et tout son métal (ferrures, coins, clous, serrure) prend la couleur de la clé ; la clé entre dans la serrure, tourne d'un quart de tour et reste en place pendant que le couvercle s'ouvre.

Assets dans le bac à sable `sandbox-level`, sous `Assets/Art/Coffres/` (générés par `CoffresBuilder`), prêts à être copiés dans le jeu.

{video media/animations/Coffre_SansSerrure.mp4} **Coffre sans serrure, ouverture gratuite** | Objet : prefab `Coffre` (KayKit `chest` sans serrure) | Clé : aucune (pas de serrure) | une fois · 0,45 s (couvercle 0,45 s, puis les pièces)
{video media/animations/GrandCoffre_SansSerrure.mp4} **Grand coffre plein d'or sans serrure, ouverture gratuite** | Objet : prefab `GrandCoffre` (KayKit `chest_gold`, plein d'or, sans serrure) | Clé : aucune (pas de serrure) | une fois · 0,45 s (couvercle 0,45 s, puis les pièces)
{video media/animations/Coffre_Cle_Acier.mp4} **Coffre à clé d'acier (déclinaison pour plus tard)** | Objet : prefab `Coffre_Acier` (KayKit `chest`, métal teinté acier) | Clé : `Cle_Acier` | une fois · 1,10 s (clé 0,65 s puis couvercle 0,45 s, puis les pièces)
{video media/animations/Coffre_Cle_Cuivre.mp4} **Coffre à clé de cuivre (déclinaison pour plus tard)** | Objet : prefab `Coffre_Cuivre` (KayKit `chest`, métal teinté cuivre) | Clé : `Cle_Cuivre` | une fois · 1,10 s (clé 0,65 s puis couvercle 0,45 s, puis les pièces)
{video media/animations/Coffre_Cle_Or.mp4} **Coffre à clé d'or (déclinaison pour plus tard)** | Objet : prefab `Coffre_Or` (KayKit `chest`, métal teinté or) | Clé : `Cle_Or` | une fois · 1,10 s (clé 0,65 s puis couvercle 0,45 s, puis les pièces)

## Squelettes, apparitions et références (17)

Clips du fichier `Rig_Medium_Special` (pensés pour les squelettes, joués ici sur le mannequin), apparitions et pose de référence.

{video media/animations/Skeletons_Idle.mp4} **Squelette, attente** | `Skeletons_Idle` | Arme : aucune | boucle · 4,27 s
{video media/animations/Skeletons_Walking.mp4} **Squelette, marche traînante** | `Skeletons_Walking` | Arme : aucune | boucle · 1,60 s
{video media/animations/Skeletons_Taunt.mp4} **Squelette, provocation** | `Skeletons_Taunt` | Arme : aucune | une fois · 1,03 s {emote possible}
{video media/animations/Skeletons_Taunt_Longer.mp4} **Squelette, provocation longue** | `Skeletons_Taunt_Longer` | Arme : aucune | une fois · 3,00 s {emote possible}
{video media/animations/Skeletons_Spawn_Ground.mp4} **Squelette, sortie de terre** | `Skeletons_Spawn_Ground` | Arme : aucune | une fois · 3,57 s
{video media/animations/Skeletons_Awaken_Standing.mp4} **Squelette, réveil debout** | `Skeletons_Awaken_Standing` | Arme : aucune | une fois · 1,00 s
{video media/animations/Skeletons_Awaken_Floor.mp4} **Squelette, réveil au sol** | `Skeletons_Awaken_Floor` | Arme : aucune | une fois · 2,30 s
{video media/animations/Skeletons_Awaken_Floor_Long.mp4} **Squelette, réveil au sol (long)** | `Skeletons_Awaken_Floor_Long` | Arme : aucune | une fois · 3,83 s
{video media/animations/Skeletons_Death.mp4} **Squelette, effondrement en tas d'os** | `Skeletons_Death` | Arme : aucune | une fois · 2,00 s
{video media/animations/Skeletons_Death_Resurrect.mp4} **Squelette, se reconstituer** | `Skeletons_Death_Resurrect` | Arme : aucune | une fois · 2,70 s
{video media/animations/Skeletons_Death_Pose.mp4} **Squelette, tas d'os (pose)** | `Skeletons_Death_Pose` | Arme : aucune | une fois · 0,03 s
{video media/animations/Skeletons_Inactive_Floor_Pose.mp4} **Squelette inactif au sol (pose)** | `Skeletons_Inactive_Floor_Pose` | Arme : aucune | une fois · 0,03 s
{video media/animations/Skeletons_Inactive_Standing_Pose.mp4} **Squelette inactif debout (pose)** | `Skeletons_Inactive_Standing_Pose` | Arme : aucune | une fois · 0,03 s
{video media/animations/Spawn_Ground.mp4} **Apparition, sortie du sol** | `Spawn_Ground` | Arme : aucune | une fois · 1,30 s
{video media/animations/Spawn_Air.mp4} **Apparition, chute du ciel** | `Spawn_Air` | Arme : aucune | une fois · 1,30 s
{video media/animations/EXPERIMENTAL_Medium_Transform.mp4} **Transformation (expérimental)** | `EXPERIMENTAL_Medium_Transform` | Arme : aucune | une fois · 1,00 s
{video media/animations/T-Pose.mp4} **Pose en T (référence du squelette)** | `T-Pose` | Arme : aucune | une fois · 0,03 s

## Rig_Large : Morgrim, le Golem squelette (29)

Aucun mannequin Large dans les packs : ces clips sont joués sur `Skeleton_Golem` (Morgrim, mini-boss de la nuit 10), avec les armes Large des packs (`Skeleton_Golem_Axe_Large`, `axe_1handed_Large`, `shield_round_barbarian_Large`).

{video media/animations/Large_Idle_A.mp4} **Attente** | `Idle_A` · rig Large | Arme : aucune | boucle · 1,97 s
{video media/animations/Large_Idle_B.mp4} **Attente, variante longue** | `Idle_B` · rig Large | Arme : aucune | boucle · 6,00 s
{video media/animations/Large_Walking_A.mp4} **Marche** | `Walking_A` · rig Large | Arme : aucune | boucle · 1,07 s
{video media/animations/Large_Running_A.mp4} **Course** | `Running_A` · rig Large | Arme : aucune | boucle · 1,07 s
{video media/animations/Large_Dodge_Forward.mp4} **Esquive en avant** | `Dodge_Forward` · rig Large | Arme : aucune | une fois · 0,33 s
{video media/animations/Large_Dodge_Backwards.mp4} **Esquive en arrière** | `Dodge_Backwards` · rig Large | Arme : aucune | une fois · 0,33 s
{video media/animations/Large_Dodge_Left.mp4} **Esquive à gauche** | `Dodge_Left` · rig Large | Arme : aucune | une fois · 0,33 s
{video media/animations/Large_Dodge_Right.mp4} **Esquive à droite** | `Dodge_Right` · rig Large | Arme : aucune | une fois · 0,33 s
{video media/animations/Large_Melee_1H_Slash.mp4} **Taille, arme à une main** | `Melee_1H_Slash` · rig Large | Arme : hache à une main Large (axe_1handed_Large) | une fois · 1,57 s
{video media/animations/Large_Melee_1H_Stab.mp4} **Estoc, arme à une main** | `Melee_1H_Stab` · rig Large | Arme : hache à une main Large (axe_1handed_Large) | une fois · 1,40 s
{video media/animations/Large_Melee_2H_Idle.mp4} **Garde, arme à deux mains** | `Melee_2H_Idle` · rig Large | Arme : hache du Golem (Skeleton_Golem_Axe_Large) | boucle · 1,57 s
{video media/animations/Large_Melee_2H_Attack.mp4} **Coup, arme à deux mains** | `Melee_2H_Attack` · rig Large | Arme : hache du Golem (Skeleton_Golem_Axe_Large) | une fois · 1,33 s
{video media/animations/Large_Melee_2H_Slam.mp4} **Frappe au sol, arme à deux mains** | `Melee_2H_Slam` · rig Large | Arme : hache du Golem (Skeleton_Golem_Axe_Large) | une fois · 2,83 s
{video media/animations/Large_Melee_Dualwield_Slash.mp4} **Taille, deux armes** | `Melee_Dualwield_Slash` · rig Large | Arme : deux haches Large (axe_1handed_Large x 2) | une fois · 1,03 s
{video media/animations/Large_Melee_Dualwield_SlashCombo.mp4} **Enchaînement de tailles, deux armes** | `Melee_Dualwield_SlashCombo` · rig Large | Arme : deux haches Large (axe_1handed_Large x 2) | une fois · 1,60 s
{video media/animations/Large_Melee_Block.mp4} **Lever le bouclier** | `Melee_Block` · rig Large | Arme : hache et bouclier Large (axe_1handed_Large, shield_round_barbarian_Large) | une fois · 1,07 s
{video media/animations/Large_Melee_Blocking.mp4} **Garde au bouclier, maintenue** | `Melee_Blocking` · rig Large | Arme : hache et bouclier Large (axe_1handed_Large, shield_round_barbarian_Large) | boucle · 1,07 s
{video media/animations/Large_Melee_Block_Hit.mp4} **Coup encaissé au bouclier** | `Melee_Block_Hit` · rig Large | Arme : hache et bouclier Large (axe_1handed_Large, shield_round_barbarian_Large) | une fois · 0,67 s
{video media/animations/Large_Melee_Block_Attack.mp4} **Coup de bouclier** | `Melee_Block_Attack` · rig Large | Arme : hache et bouclier Large (axe_1handed_Large, shield_round_barbarian_Large) | une fois · 1,03 s
{video media/animations/Large_Melee_Unarmed_Idle.mp4} **Garde, mains nues** | `Melee_Unarmed_Idle` · rig Large | Arme : aucune | boucle · 1,07 s
{video media/animations/Large_Melee_Unarmed_Punch.mp4} **Coup de poing, mains nues** | `Melee_Unarmed_Punch` · rig Large | Arme : aucune | une fois · 1,23 s
{video media/animations/Large_Melee_Unarmed_Kick.mp4} **Coup de pied, mains nues** | `Melee_Unarmed_Kick` · rig Large | Arme : aucune | une fois · 1,70 s
{video media/animations/Large_Melee_Unarmed_Smash.mp4} **Plongeon écrasant, mains nues** | `Melee_Unarmed_Smash` · rig Large | Arme : aucune | une fois · 3,47 s
{video media/animations/Large_Hit_A.mp4} **Touché** | `Hit_A` · rig Large | Arme : aucune | une fois · 0,70 s
{video media/animations/Large_Death_A.mp4} **Mort, effondrement** | `Death_A` · rig Large | Arme : aucune | une fois · 1,67 s
{video media/animations/Large_Death_A_Pose.mp4} **Mort, pose finale au sol** | `Death_A_Pose` · rig Large | Arme : aucune | une fois · 0,03 s
{video media/animations/Large_Flexing.mp4} **Montrer ses muscles** | `Flexing` · rig Large | Arme : aucune | une fois · 4,30 s {emote possible}
{video media/animations/Large_EXPERIMENTAL_Large_Transform.mp4} **Transformation (expérimental)** | `EXPERIMENTAL_Large_Transform` · rig Large | Arme : aucune | une fois · 1,67 s
{video media/animations/Large_T-Pose.mp4} **Pose en T (référence du squelette)** | `T-Pose` · rig Large | Arme : aucune | une fois · 0,03 s

## Produire les vidéos

Bac à sable `sandbox-level` : scène `Assets/Scenes/Animations.unity` (décor et mannequins équipés), outil `Assets/Animations_Planche/Editor/PlancheAnimations.cs`. `Planche.Capturer(debut, nombre)` rejoue chaque clip image par image (30 i/s, pas fixe, en édition, sans Play) dans une scène de prévisualisation isolée, encode en MP4 H.264 480 × 480 muet par `UnityEditor.Media.MediaEncoder` et copie dans `Wiki/media/animations/` ; `Assets/Animations_Planche/Outils~/generer_page.py` réécrit cette page à partir du manifeste `animations.json`. Prises des outils, canne provisoire et coffres : `Assets/Animations_Planche/Editor/PlancheAccessoires.cs` (`Planche.CapturerCoffres()` pour les coffres, prefabs de `Assets/Art/Coffres/`, clés `Cle_*` de `Assets/Art/Cadenas/`).
