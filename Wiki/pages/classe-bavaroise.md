# Bavaroise

{icone-grande classe_bavaroise}

**Bientôt.** Une classe jouable à venir. Le kit ci-dessous est une **proposition** à valider {à confirmer}.

**Bagarreuse joviale** : en dirndl, une chope de bière dans chaque main, elle cogne vite, trinque pour se requinquer et arrose tout le monde.

| Rôle | Arme |
|---|---|
| Mêlée rapide, bagarre | Deux chopes de bière (chopes KayKit) |

## Actions proposées {à confirmer}

| Touche | Action | Idée |
|---|---|---|
| RT | {icone bavaroise_coups_de_chopes} **Coups de chopes** | Enchaînement rapide gauche-droite ; le 4e coup est un double coup qui repousse. |
| LT | {icone bavaroise_trinquer} **Trinquer** | Elle boit une gorgée : se soigne un peu et remplit sa jauge d'Ivresse. Recharge courte. |
| LB | {icone bavaroise_tournee_generale} **Tournée générale** | Elle lance une chope qui éclate en zone : la mousse rend le sol glissant, les squelettes ralentissent et glissent. |
| RB | {icone bavaroise_charge_du_tonneau} **Charge du tonneau** | Elle fonce en avant, épaule en avant, et renverse les squelettes sur son passage. |

- **Jauge : Ivresse** : plus elle est haute, plus ses coups font mal, mais elle titube légèrement ; elle redescend avec le temps.
- **Effets** : mousse et éclaboussures en gemmes crème et ambre ; pas de vert.

{dev} Modèle : créé de zéro dans le style KayKit (`sandbox-rig/Assets/Art/Bavaroise/`), chopes KayKit `mug_full_Large`.

## Clips proposés {à confirmer}

Gestes KayKit proposés pour ce kit, repris des bacs à sable : rien n'est encore branché en jeu. L'arme est celle de la vidéo, suivie de l'arme prévue.

{dev} Sources : `sandbox-rig` (contrôleur `Bavaroise.controller`, chope portée à la bouche par `ChopeBoire.cs`) et banc `NcBanc` de `sandbox-vfx` (scène `NouvellesClasses`).

### Coups de chopes

{video media/animations/Melee_Dualwield_Attack_Slice.mp4} **Coups de chopes : enchaînement gauche-droite** {à confirmer} | {dev} `Melee_Dualwield_Attack_Slice` | Arme : deux dagues (prévu : deux chopes) | une fois · 1,17 s
{video media/animations/Melee_Dualwield_Attack_Chop.mp4} **Coups de chopes : double coup (4e coup)** {à confirmer} | {dev} `Melee_Dualwield_Attack_Chop` | Arme : deux dagues (prévu : deux chopes) | une fois · 1,27 s

### Trinquer

{video media/animations/Use_Item.mp4} **Trinquer : boire une gorgée** {à confirmer} | {dev} `Use_Item` | Arme : potion (prévu : chope) | boucle · 1,60 s

### Tournée générale

{video media/animations/Throw.mp4} **Tournée générale : lancer de chope** {à confirmer} | {dev} `Throw` | Arme : fiole (prévu : chope) | une fois · 1,37 s

### Charge du tonneau

{video media/animations/Running_A.mp4} **Charge du tonneau : course** {à confirmer} | {dev} `Running_A` | Arme : aucune (prévu : deux chopes) | boucle · 0,80 s
