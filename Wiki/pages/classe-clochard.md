# Clochard

{icone-grande classe_clochard}

**Bientôt.** Une classe jouable à venir. Le kit ci-dessous est une **proposition** à valider {à confirmer}.

**Perturbateur malodorant** : un vagabond attachant qui se bat à la bouteille et dont les flatulences font fuir même les morts.

| Rôle | Arme |
|---|---|
| Contrôle de zone, dégâts dans la durée | Bouteille dans du papier kraft |

## Actions proposées {à confirmer}

| Touche | Action | Idée |
|---|---|---|
| RT | {icone clochard_coup_de_bouteille} **Coup de bouteille** | Coups de bouteille ; le 3e coup la fait éclater en éclats qui touchent autour. |
| LT | {icone clochard_pet_de_defense} **Pet de défense** | Un nuage derrière lui repousse et empoisonne les squelettes qui le poursuivent. Recharge courte. |
| LB | {icone clochard_nuage_pestilentiel} **Nuage pestilentiel** | Un grand nuage moutarde : les squelettes dedans ralentissent, perdent de la vie peu à peu et visent mal. |
| RB | {icone clochard_pet_propulsion} **Pet-propulsion** | Un bond en avant propulsé par un pet, qui laisse un petit nuage au point de départ. |

- **Passif : Débrouille** : il ramasse un peu plus d'or sur les squelettes tués (+10 %).
- **Jauge : Gaz** : elle se remplit avec le temps et quand il boit ; les pets la dépensent.
- **Effets** : nuages en gemmes jaune-brun moutarde qui grossissent puis disparaissent par la taille ; **jamais vert** (le vert est réservé à Nyxessa).

{dev} Modèle : créé de zéro dans le style KayKit (`sandbox-ui/Assets/Art/Clochard/`).

## Clips proposés {à confirmer}

Gestes KayKit proposés pour ce kit, repris des bacs à sable : rien n'est encore branché en jeu. L'arme est celle de la vidéo, suivie de l'arme prévue.

{dev} Sources : `sandbox-ui` (contrôleur du clochard, `ClochardBuilder.cs`, état `Frappe`), banc `NcBanc` de `sandbox-vfx` (scène `NouvellesClasses`) et demande de Quentin pour « boire ».

### Coup de bouteille

{video media/animations/Melee_1H_Attack_Chop.mp4} **Coup de bouteille** {à confirmer} | {dev} `Melee_1H_Attack_Chop` | Arme : hache à une main et bouclier (prévu : bouteille) | une fois · 1,07 s

### Pet de défense

Aucun geste proposé pour l'instant.

### Nuage pestilentiel

{video media/animations/Interact.mp4} **Nuage pestilentiel** {à confirmer} | {dev} `Interact` | Arme : aucune (prévu : bouteille) | une fois · 1,30 s

### Pet-propulsion

{video media/animations/Jump_Full_Long.mp4} **Pet-propulsion : bond** {à confirmer} | {dev} `Jump_Full_Long` | Arme : aucune (prévu : bouteille) | une fois · 2,33 s

### Boire (jauge de Gaz)

{video media/animations/Use_Item.mp4} **Boire un coup** {à confirmer} | {dev} `Use_Item` | Arme : potion (prévu : bouteille) | boucle · 1,60 s
