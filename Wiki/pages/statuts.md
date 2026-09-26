# Statuts

Un **statut** est un état passager posé sur un héros ou sur un ennemi : il brûle, il est ralenti, étourdi, ivre ou provoqué. Chaque statut a une **durée**, une **intensité** (des dégâts par seconde, une part de vitesse perdue…), une **source** (le joueur qui l'a posé, une chute, la taverne…) et une **règle** qui dit ce qui se passe quand on le reçoit une deuxième fois {décidé}.

On voit les statuts **au-dessus des ennemis affectés**, **dans le HUD** près du portrait, et dans le **menu du personnage**, où l'on survole chaque statut pour lire ce qu'il fait (voir [Interface](interface.md#statuts)).

## Liste

| | Statut | Effet | Durée | Posé par | Nouveau coup |
|---|---|---|---|---|---|
| {icone statut_brulure} | **Brûlure** | 5 dégâts par seconde | 3 s | Boule de feu et cône de flammes du [Mage](classe-mage.md) | La durée repart de zéro, pas de cumul |
| {icone statut_ralenti} | **Ralenti** | Déplacements ralentis de 40 % | 3 s après une chute ; tant qu'on est dans l'eau du donjon | Une chute de haut (voir plus bas), l'eau du donjon | La fin la plus lointaine l'emporte ; le ralentissement le plus fort compte |
| {icone statut_etourdi} | **Étourdi** | Ni déplacement ni attaque | Charge bélier : 2,5 s pour la cible, 0,6 s pour les ennemis repoussés ; parade : 1 s ; saut percutant : 1 s ; garde brisée (héros) : 0,8 s ; moitié moins pour le mini-boss | [Paladin](classe-paladin.md), [Viking](classe-viking.md), la garde brisée d'un héros | La fin la plus lointaine l'emporte |
| {icone statut_ivresse} | **Ivresse** | La vue tangue et la démarche hésite ; attaques et visée inchangées | Bière : 8 s ; tournée : 15 s | La [taverne](village.md#taverne) | La fin la plus lointaine l'emporte |
| {icone statut_provoque} | **Provoqué** | L'ennemi s'acharne sur le héros qui l'a provoqué, avant Nyxessa | 5 s | Rugissement du [Viking](classe-viking.md) | Le dernier provocateur prend la place |

Les durées et les intensités sont {à équilibrer}. L'ivresse n'est pas une affliction : sa case a un liseré or, les autres un liseré rouge.

{dev} Code : `Assets/Scripts/Jeu/Statuts/` (`Statuts` : un composant par personnage ; `CatalogueStatuts` : nom, icône, règle et effet de chaque type). Valeurs dans `GameBalance` (`brulureDegats`, `brulureDuree`, `chute*`, `chargeEtourdi*`, `paradeEtourdi`, `sautEtourdi`, `gardeBriseeEtourdi`, `ivresseBiere`, `ivresseTournee`, `rugissementProvocation`). Icônes `statut_*` générées par `ArtSources/Icones/generer_statuts.py`.

## Chute {décidé}

Tomber de haut fait mal, puis ralentit quelques secondes.

- **Hauteur** : du point le plus haut atteint en l'air jusqu'au sol. Un saut sur place (1,2 m) ne blesse jamais ; un niveau du donjon fait 4 m.
- **Seuil** : 3,5 m. En dessous, rien.
- **Dégâts** : 12 par mètre au-delà du seuil, 80 au plus. Une chute de 4 m fait 6 dégâts ; sauter depuis un balcon de 4 m, environ 20.
- **Ralenti** : ensuite, déplacements ralentis de 40 % pendant 3 s.
- **Pas de dégâts** pour une téléportation (portail, réapparition, rappel par Nyxessa) ni pendant un déplacement de compétence (charge bélier, saut percutant).

Seuil, dégâts par mètre, maximum, durée et force du ralenti : {à équilibrer}. {{dev: (`GameBalance` : `chuteSeuil`, `chuteDegatsParMetre`, `chuteDegatsMax`, `chuteRalentiDuree`, `chuteRalentiForce` ; mesure dans `Heros.SuivreChute`)}}

## Où les voir

- **Au-dessus des ennemis** : une rangée de petites icônes au-dessus de la tête de chaque ennemi affecté, avec une jauge de durée discrète sous chaque icône. Elle n'apparaît que tant qu'il est affecté, s'il est à l'écran et à moins de 30 m.
- **HUD du joueur** : en bas à gauche, au-dessus du portrait et des barres, une case par statut : icône, jauge de durée et secondes restantes.
- **Menu du personnage** (Tab, Y, Triangle) : section « Afflictions ». Survoler un statut à la souris, ou le sélectionner à la manette, affiche son nom, son effet, sa durée restante et sa source.

## Multijoueur

- **L'hôte décide** des statuts de tous, héros comme ennemis. Il les envoie aux autres joueurs seulement quand ils changent, pas en continu.
- Le joueur qui chute voit son ralentissement tout de suite, sans attendre l'hôte.

{{dev: Détails dans `Docs/reseau.md`, « Statuts ».}}
