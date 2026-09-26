# DJ Bob Douville

> **Candidat au [personnage du mois](classes.md)** {décidé} : cette classe est une proposition ; si elle est retenue, elle arrivera comme personnage du mois.

{video media/classes/dj-bob/rotation.mp4} **Rendu 3D** {à confirmer} | Proposition en attente de validation, platines en bandoulière, casque autour du cou | {dev} prefab `DjBob.prefab` (`DjBobBuilder`), clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/dj-bob/portrait.png} **Portrait** {à confirmer} | De 3/4 face

{icone-grande classe_dj_bob}

**Bientôt.** Une classe jouable à venir. Le kit ci-dessous est une **proposition** à valider {à confirmer}.

**Le DJ des années 80** : DJ Bob Douville, crâne chauve et luisant, lunettes de soleil, chaîne en or et casque autour du cou, se bat avec ses platines. Il lance ses vinyles comme des boomerangs, scratche les squelettes à bout portant et transforme le champ de bataille en piste de danse.

| Rôle | Arme |
|---|---|
| Distance moyenne, contrôle et soutien | Platines portables (mallette ouverte en bandoulière, deux plateaux et une table de mixage) ; il y prend ses vinyles |

**Look** {à confirmer} : blouson satiné violet ouvert (revers roses, côtes noires à liserés or, épaulettes), tee-shirt noir, chaîne en or et médaillon « 45 tours », lunettes de soleil dégradées violet et rose, grosse moustache, **pantalon à pattes d'éléphant en camouflage Centre-Europe de l'armée française** (taches vert foncé, brun et noir sur fond kaki ; mat, sans lueur : c'est du camouflage, pas l'énergie de Nyxessa), baskets montantes blanches. Écusson disque d'or dans le dos.

## Actions proposées {à confirmer}

| Touche | Action | Idée |
|---|---|---|
| RT | {icone dj_bob_lancer_vinyle} **Lancer de vinyle** | Coup principal. Il sort un disque du plateau et le lance : le vinyle part en ligne, **traverse et fend** les squelettes sur son passage, fait une boucle et **revient dans sa main** comme un boomerang (il le touche aussi au retour). |
| LT | {icone dj_bob_scratch} **Scratch** | Un coup de platine au corps à corps : une onde sonore en **cône** qui grince et **repousse** les squelettes devant lui. |
| LB | {icone dj_bob_drop} **Drop** | Il **pose la mallette au sol** : une piste de danse s'allume autour d'elle pendant quelques secondes. Les squelettes dedans se mettent à danser (**Étourdi**), les alliés dedans **attaquent plus vite**. Il reprend ses platines à la fin. Coûte du Tempo. |
| RB | {icone dj_bob_boule_a_facettes} **Boule à facettes** | Il lance une boule à facettes qui **flotte** au-dessus d'un point et balaie le sol de ses reflets : les squelettes dans son rayon, éblouis, sont **Ralentis**. Coûte du Tempo. |

- **Jauge : le Tempo** {icone dj_bob_jauge_tempo} : elle monte quand il enchaîne ses coups en rythme et retombe dès qu'il s'arrête ; le Drop et la Boule à facettes la dépensent.
- **Emote** : il remonte son casque sur les oreilles et hoche la tête en rythme.
- **Effets** : gemmes de la palette **Disco** (violet, rose, blanc, or) ; **jamais de vert** (réservé à Nyxessa). Le seul vert du personnage est le camouflage mat de son pantalon.
- Durées, portées, dégâts, coût en Tempo et vitesse du lancer : {à confirmer}.

{{dev: Modèle créé de zéro dans le style KayKit (`sandbox-rig/Assets/Art/DjBob/`, générateur `Assets/Editor/DjBob/DjBobBuilder.cs`, 6 780 triangles + casque 700, platines 1 838). Équipement `DjBobEquipement` (vinyle en main, mallette au sol, casque sur les oreilles). Effets prototypes `Assets/VFX/DjBob/` (`VinyleVol`, `ScratchOnde`, `DropZone`, `BouleFacettes`, palette `Disco.asset`), banc `DjBobBanc` (scène `DjBob.unity`).}}

## Effets proposés {à confirmer}

Prototypes en gemmes low poly sur le banc de `sandbox-rig`, squelettes KayKit en cibles.

{image media/classes/dj-bob/effets/lancer_vinyle.png} **Lancer de vinyle** {à confirmer} | Le disque tourne en boucle, traînée violette et rose, entaille rose sur les squelettes traversés | {dev} `VinyleVol`
{image media/classes/dj-bob/effets/scratch.png} **Scratch** {à confirmer} | Fronts d'onde en arcs qui avancent en dents de scie, crête blanche | {dev} `ScratchOnde`
{image media/classes/dj-bob/effets/drop.png} **Drop** {à confirmer} | Piste de danse en damier qui change de couleur à chaque temps, étoiles dorées au-dessus des squelettes étourdis | {dev} `DropZone`
{image media/classes/dj-bob/effets/boule_facettes.png} **Boule à facettes** {à confirmer} | Boule argentée qui flotte, faisceaux et taches de lumière qui balaient le sol, cercle lilas du rayon | {dev} `BouleFacettes`

## Clips proposés {à confirmer}

Gestes KayKit proposés pour ce kit : rien n'est encore branché en jeu. Vidéos sur le prefab `DjBob.prefab`, accessoires posés par `DjBobEquipement`, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Sources : `sandbox-rig` (contrôleur `DjBob.controller`, banc `DjBobBanc`).

### Lancer de vinyle

{video media/classes/dj-bob/clips/Throw.mp4} **Lancer de vinyle** {à confirmer} | {dev} `Throw` | Arme : vinyle sorti du plateau droit, lâché à mi-geste | une fois · 1,37 s

### Scratch

{video media/classes/dj-bob/clips/Melee_1H_Attack_Slice_Horizontal.mp4} **Scratch : la main balaie le plateau** {à confirmer} | {dev} `Melee_1H_Attack_Slice_Horizontal` | Arme : platines | une fois · 1,37 s

### Drop

{video media/classes/dj-bob/clips/PickUp.mp4} **Drop : la mallette posée au sol** {à confirmer} | {dev} `PickUp` | Arme : platines, posées à 46 % du geste | une fois · 1,30 s

### Boule à facettes

{video media/classes/dj-bob/clips/Ranged_Magic_Shoot.mp4} **Boule à facettes : lancée devant lui** {à confirmer} | {dev} `Ranged_Magic_Shoot` | Arme : boule lâchée à 0,31 s | une fois · 0,93 s

### Mixer et emote

{video media/classes/dj-bob/clips/Ranged_Magic_Spellcasting.mp4} **Mixer : les deux mains sur les platines** {à confirmer} | {dev} `Ranged_Magic_Spellcasting` | Arme : platines | en boucle · 0,67 s
{video media/classes/dj-bob/clips/Idle_B.mp4} **Emote : casque sur les oreilles** {à confirmer} | {dev} `Idle_B` + hochement de tête en rythme (`DjBobEquipement.Hocher`, 112 bpm) | en boucle · 2,13 s
