# Barde

> **Candidat au [personnage du mois](classes.md)** {décidé} : cette classe est une proposition ; si elle est retenue, elle arrivera comme personnage du mois.

{video media/classes/barde/rotation.mp4} **Rendu 3D** {à confirmer} | Proposition en attente de validation, luth au dos | {dev} prefab `Barde.prefab` (`BardeBuilder`), clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/barde/portrait.png} **Portrait** {à confirmer} | De 3/4 face (le luth est au dos)

{icone-grande classe_barde}

**Bientôt.** Une classe jouable à venir. Le kit ci-dessous est une **proposition** à valider {à confirmer}.

**Soutien joyeux** : il frappe avec son luth, entraîne ses alliés en musique et déstabilise les squelettes d'un accord bien faux.

| Rôle | Arme |
|---|---|
| Soutien, zone courte | Luth (tenu par le manche comme une massue pour frapper) |

## Actions proposées {à confirmer}

| Touche | Action | Idée |
|---|---|---|
| RT | {icone barde_coup_de_luth} **Coup de luth** | Il tient le luth par le manche et frappe comme avec une massue. Le 3e coup d'une série fait « bwoiing » et étourdit brièvement. |
| LT | **Jouer** (maintenu) | Il joue une mélodie : les alliés à moins de 8 m régénèrent un peu de vie tant qu'il joue. Il avance lentement. |
| LB | {icone barde_ballade_entrainante} **Ballade entraînante** | Pendant 6 s, les alliés proches attaquent et se déplacent 20 % plus vite. |
| RB | {icone barde_accord_dissonant} **Accord dissonant** | Une onde sonore en cône repousse les squelettes devant lui et les étourdit un court instant. |

- **Jauge : Inspiration** : elle monte quand il frappe et quand il joue ; la Ballade et l'Accord la dépensent.
- **Effets** : notes de musique en gemmes crème et moutarde, ondes sonores en anneaux à facettes ; pas de vert.

{dev} Modèle : créé de zéro dans le style KayKit (`sandbox-rig/Assets/Art/Barde/`). L'animation « jouer du luth » reste à créer.

## Clips proposés {à confirmer}

Gestes KayKit proposés pour ce kit, repris des bacs à sable : rien n'est encore branché en jeu. Vidéos sur le prefab `Barde.prefab` (lissé), luth tenu par le manche (`BardeLuth`, prise « massue » ou « jouer » selon le geste), caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Sources : `sandbox-rig` (contrôleur `Barde.controller`, enchaînement `BardeCombo.cs`) et banc `NcBanc` de `sandbox-vfx` (scène `NouvellesClasses`).

### Coup de luth

{video media/classes/barde/clips/Melee_1H_Attack_Chop.mp4} **Coup de luth : 1er coup** {à confirmer} | {dev} `Melee_1H_Attack_Chop` | Arme : luth (tenu par le manche, prise « massue ») | une fois · 1,07 s
{video media/classes/barde/clips/Melee_1H_Attack_Slice_Diagonal.mp4} **Coup de luth : 2e coup** {à confirmer} | {dev} `Melee_1H_Attack_Slice_Diagonal` | Arme : luth (tenu par le manche, prise « massue ») | une fois · 1,00 s
{video media/classes/barde/clips/Melee_1H_Attack_Jump_Chop.mp4} **Coup de luth : 3e coup, « bwoiing »** {à confirmer} | {dev} `Melee_1H_Attack_Jump_Chop` | Arme : luth (tenu par le manche, prise « massue ») | une fois · 1,33 s

### Jouer

{video media/classes/barde/clips/Ranged_Magic_Spellcasting_Long.mp4} **Jouer : en attendant l'animation de luth** {à confirmer} | {dev} `Ranged_Magic_Spellcasting_Long` | Arme : luth (en main, prise « jouer ») | une fois · 2,53 s

Luth en main (prise « jouer », `BardeLuth`) sur le clip générique emprunté ; une animation « jouer du luth » propre reste à créer.

### Ballade entraînante

{video media/classes/barde/clips/Ranged_Magic_Raise.mp4} **Ballade entraînante : luth levé** {à confirmer} | {dev} `Ranged_Magic_Raise` | Arme : luth (en main, prise « jouer ») | une fois · 2,10 s

### Accord dissonant

{video media/classes/barde/clips/Ranged_Magic_Shoot.mp4} **Accord dissonant : onde lancée devant** {à confirmer} | {dev} `Ranged_Magic_Shoot` | Arme : luth (en main, prise « jouer ») | une fois · 0,93 s
