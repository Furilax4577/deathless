# Clochard pétomane

> **Candidat au [personnage du mois](classes.md)** {décidé} : cette classe est une proposition ; si elle est retenue, elle arrivera comme personnage du mois.

{video media/classes/clochard/rotation.mp4} **Rendu 3D** {à confirmer} | Proposition en attente de validation, bouteille en main | {dev} prefab `Clochard.prefab` (`ClochardBuilder`), clip `Idle_A` ; rendu par `ClochardTournage` (`sandbox-ui`)
{image media/classes/clochard/portrait.png} **Portrait** {à confirmer} | De 3/4 face, bouteille en main
{image media/classes/clochard/face.png} **Clochard pétomane** | Aperçu de face : son design est encore en cours

{icone-grande classe_clochard}

**Bientôt.** Une classe jouable à venir. Le kit ci-dessous est une **proposition** à valider {à confirmer}.

**Perturbateur malodorant** : le Clochard pétomane est un vagabond attachant qui se bat à la bouteille et dont les flatulences font fuir même les morts.

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

{dev} Modèle du Clochard pétomane : créé de zéro dans le style KayKit (`sandbox-ui/Assets/Art/Clochard/`, générateur `Assets/Editor/Clochard/ClochardBuilder.cs`).

## Design (en cours) {à confirmer}

Le design du Clochard pétomane est **en devenir** : modèle, couleurs et gestes sont des propositions, pas encore validées. La vidéo en tête de page le montre sur 360°.

{dev} Médias tournés dans `sandbox-ui` par `Assets/Scripts/Dev/ClochardTournage.cs` (menu **Deathless > Personnages > Clochard - médias du wiki**, ou `ClochardWiki.Tourner("")`), en Play, 30 i/s, MP4 H.264 par `MediaEncoder` comme la planche des animations. Rotation et portrait au format commun des rendus de classes.

### Rendu 3D

{image media/classes/clochard/face.png} **De face** | Pose d'attente, bouteille en main droite
{image media/classes/clochard/portrait.png} **De 3/4** | Même vue que le rendu 3D
{image media/classes/clochard/dos.png} **De dos** | Baluchon pendu à la ficelle en bandoulière

### Détails

{image media/classes/clochard/visage.png} **Visage** | Gros nez rouge en boule, joues roses, sourcils bonhommes, moustache tombante, sourire à une seule dent
{image media/classes/clochard/tenue.png} **Tenue** | Long manteau brun ouvert et rapiécé, pull beige, écharpe effilochée, mitaines, pantalon trop court, chaussettes rayées, semelle qui bâille
{image media/classes/clochard/bouteille.png} **Bouteille** | Verre brun, bouchon de liège, dans son sac en papier kraft froissé : son arme
{image media/classes/clochard/baluchon.png} **Baluchon** | Ballot rouge à pois crème, noué, dans le dos

### Palette

Aplats dégradés dans un atlas de palette, comme les personnages KayKit ; chaque pastille donne le haut puis le bas du dégradé.

| Élément | Teintes |
|---|---|
| Peau | {couleur #F6D1AC} {couleur #E0A47C} |
| Nez, joues | {couleur #E4665A} {couleur #F2A294} |
| Barbe, moustache | {couleur #8C7A68} {couleur #5E4E40} {couleur #B3A28C} |
| Chapeau haut-de-forme | {couleur #55504C} {couleur #35302D} |
| Manteau, revers | {couleur #8E5C3A} {couleur #5C3822} {couleur #6A4430} |
| Pièces rapiécées | {couleur #D8A048} {couleur #B24A3C} {couleur #7A8CA0} {couleur #936078} |
| Écharpe, pull, pantalon | {couleur #CF9A44} {couleur #B0A696} {couleur #7E7672} |
| Chaussures, chaussettes | {couleur #5E4231} {couleur #F4E8CE} |
| Bouteille, sac kraft, liège | {couleur #9A5220} {couleur #B98C58} {couleur #C99C6A} |
| Baluchon, ficelle | {couleur #A8453A} {couleur #D9C089} |
| Gaz (pets et nuages) | {couleur #6B5528} {couleur #9C7A2E} {couleur #C49A35} {couleur #DDBF5E} |

### Silhouette

- **Proportions KayKit** : grosse tête, corps trapu, ventre rebondi. Même squelette que les héros, donc toutes les animations KayKit lui vont.
- **Lisible de loin** : le haut-de-forme cabossé qui pique du nez, le nez rouge et la barbe en pointe. Le long manteau s'évase jusqu'aux mollets.
- **Misère joyeuse** : pièces de couleurs vives, écharpe effilochée, orteil qui dépasse de la chaussure, sourire à une dent. Il doit rester attachant, pas sale.
- **Accessoires** : la bouteille en main droite, le baluchon dans le dos. Ils se reconnaissent de face comme de dos.
- **Couleurs** : bruns chauds et gris, avec des touches de rouge et d'ocre. Le gaz moutarde tranche sur le manteau. **Aucun vert.**

{dev} 5 980 triangles, plus 238 pour la bouteille et 314 pour le baluchon. Squelette Rig_Medium du Knight, sockets `handslot.r` (bouteille) et `chest` (baluchon). Teintes lues dans `ClochardBuilder.Teintes` et, pour le gaz, dans `PetClochard.palette` ; le thème `Gaz` de `sandbox-vfx` (nuage, propulsion) est proche : {couleur #5C3F12} {couleur #8A621A} {couleur #B88B26} {couleur #DCBC55}.

## Clips proposés {à confirmer}

Chaque geste est joué par le Clochard pétomane lui-même, bouteille en main, avec son effet quand il existe. Caméra fixe de 3/4 et sol quadrillé tous les mètres, comme sur la page des animations. Pour les pets, la vue est de côté, un peu de dos, pour voir la bouffée. Rien n'est encore branché en jeu. Sous chaque geste, la vidéo du mannequin sert de référence.

{dev} Sources : `sandbox-ui` (contrôleur du clochard, `ClochardBuilder.cs`, état `Frappe` ; `PetClochard`), banc `NcBanc` de `sandbox-vfx` (scène `NouvellesClasses`, effets `NuagePestilentiel` et `PetPropulsion` copiés dans `sandbox-ui` avec leurs dépendances) et demande de Quentin pour « boire ».

### Pet

{video media/classes/clochard/pet.mp4} **Pet** {à confirmer} | {dev} `Idle_A` · `PetClochard.Peter()` | Arme : bouteille | Effet : petite bouffée moutarde vers l'arrière · 1,35 s

### Coup de bouteille

{video media/classes/clochard/coup_de_bouteille.mp4} **Coup de bouteille** {à confirmer} | {dev} `Melee_1H_Attack_Chop` | Arme : bouteille | une fois · 1,07 s · pas encore d'éclats au 3e coup

### Pet de défense

Pas encore de geste ni d'effet dédiés. Proposition : une esquive en avant, pendant qu'une grosse bouffée part vers l'arrière, vers ses poursuivants.

{video media/classes/clochard/pet_defense.mp4} **Pet de défense** {à confirmer} | {dev} `Dodge_Forward` · `PetNuage` (90 gemmes, portée 1,6 m, 1,7 s) | Arme : bouteille | une fois · 0,40 s · effet provisoire : la bouffée du pet, en plus gros

### Nuage pestilentiel

{video media/classes/clochard/nuage_pestilentiel.mp4} **Nuage pestilentiel** {à confirmer} | {dev} `Interact` puis `Idle_A` · `NuagePestilentiel.Jouer(centre, 3,5, 2,5)` | Arme : bouteille | une fois · 1,30 s · Effet : grand nuage de 3,5 m de rayon devant lui, tenu 2,5 s dans la vidéo (5 s prévues)

### Pet-propulsion

{video media/classes/clochard/pet_propulsion.mp4} **Pet-propulsion** {à confirmer} | {dev} `Jump_Full_Long` · `PetPropulsion.Jouer(porteur, avant, vol × 0,75)` | Arme : bouteille | une fois · 2,33 s · Effet : bouffée au départ, traînée de gaz, petit nuage qui reste

{dev} Le clip saute sur place : le bond de 5,5 m en cloche, avec une bascule de 18° vers l'avant, est ajouté pendant la phase aérienne, comme dans `NcBanc`.

### Boire (jauge de Gaz)

{video media/classes/clochard/boire.mp4} **Boire un coup** {à confirmer} | {dev} `Use_Item` | Arme : bouteille | boucle · 1,60 s · le clip lève la bouteille devant la poitrine, pas jusqu'à la bouche : geste à retoucher
