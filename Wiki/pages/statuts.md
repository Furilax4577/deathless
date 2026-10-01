# Statuts

Un **statut** est un état passager posé sur un héros ou sur un ennemi : il brûle, il est ralenti, étourdi, ivre ou provoqué. Chaque statut a une **durée**, une **intensité** (des dégâts par seconde, une part de vitesse perdue…), une **source** (le joueur qui l'a posé, une chute, la taverne…) et une **règle** qui dit ce qui se passe quand on le reçoit une deuxième fois {décidé}.

On voit les statuts **au-dessus des ennemis affectés**, **dans le HUD** près du portrait, et dans le **menu du personnage**, où l'on survole chaque statut pour lire ce qu'il fait (voir [Interface](interface.md#statuts)).

## Liste

| | Statut | Effet | Durée | Posé par | Nouveau coup |
|---|---|---|---|---|---|
| {icone statut_brulure} | **Brûlure** (en paliers, {décidé, 30/09/2026}) | 5, 8 puis 12 dégâts par seconde selon le palier (1 à 3, **3 au plus** {décidé, 01/10/2026}) {à équilibrer} | Au moins 3 s après le dernier coup de feu, plus longue aux paliers hauts (le temps de redescendre) | Boule de feu, cône de flammes, grande boule de feu et mur de flammes du [Mage](classe-mage.md) | Remplit la jauge de brûlure ; pleine, la brûlure monte d'un palier ; la grande boule et le mur la montent d'un palier entier d'un coup (voir [Brûlure en paliers](#brûlure-en-paliers)) |
| {icone statut_ralenti} | **Ralenti** | Déplacements ralentis de 40 % | 3 s après une chute ; tant qu'on est dans l'eau du donjon | Une chute de haut (voir plus bas), l'eau du donjon, le Fend-sol de Morgrim martache (voir [Ennemis](ennemis.md)) ; le cône et le mur de flammes du [Mage](classe-mage.md) (40 % tant qu'on y est, 01/10/2026) | La fin la plus lointaine l'emporte ; le ralentissement le plus fort compte |
| {icone statut_etourdi} | **Étourdi** | Ni déplacement ni attaque | Charge bélier : 2,5 s pour la cible, 0,6 s pour les ennemis repoussés ; parade : 1 s ; parade parfaite : 0,8 s pour les ennemis repoussés par le coup de bouclier ; saut percutant : 1 s ; garde brisée (héros) : 0,8 s ; moitié moins pour le mini-boss | [Paladin](classe-paladin.md), [Viking](classe-viking.md), la garde brisée d'un héros | La fin la plus lointaine l'emporte |
| {icone statut_ivresse} | **Ivresse** | La vue tangue et la démarche hésite ; attaques et visée inchangées | Bière : 8 s ; tournée : 15 s | La [taverne](village.md#taverne) | La fin la plus lointaine l'emporte |
| {icone statut_provoque} | **Provoqué** | L'ennemi s'acharne sur le héros qui l'a provoqué, avant Nyxessa | 5 s | Rugissement du [Viking](classe-viking.md) | Le dernier provocateur prend la place |
| {icone statut_renverse} | **Renversé** ({décidé}, 26/09/2026) | Tombe à la renverse et se relève, **sans contrôle** pendant tout le déroulé | Chute à la renverse + un instant au sol + relevé, voir plus bas | Charge écrasante de [Morgrim massue](ennemis.md#morgrim-le-roi-des-os), son onde de choc non sautée, une grosse chute | Remplace le Renversé en cours (redémarre) |
| {icone statut_peau_de_fer} | **Peau de fer** ({décidé}, 27/09/2026) | Dégâts subis réduits de 35 % (tout coup ennemi, parable ou non) | 6 s | Rugissement du [Viking](classe-viking.md), sur lui-même, au moment du cri | La fin la plus lointaine l'emporte |
| {icone statut_galvanise} | **Galvanisé** (30/09/2026) | Un squelette frappe 30 % plus fort et va 25 % plus vite {à équilibrer} | 8 s {à équilibrer} | Cri de [Morgrim](ennemis.md#morgrim-le-roi-des-os), sur les squelettes ordinaires proches | La fin la plus lointaine l'emporte |

Les durées et les intensités sont {à équilibrer}. L'ivresse, la Peau de fer et Galvanisé ne sont pas des afflictions : leur case a un liseré or, les autres un liseré rouge.

{dev} Code : `Assets/Scripts/Jeu/Statuts/` (`Statuts` : un composant par personnage ; `CatalogueStatuts` : nom, icône, règle et effet de chaque type). Valeurs dans `GameBalance` (`brulureDegatsPaliers`, `brulureRemplissage*`, `brulureDelaiDescente`, `brulureVitesseDescente`, `brulureDuree`, `chute*`, `chargeEtourdi*`, `paradeEtourdi`, `paradeParfaiteEtourdi`, `sautEtourdi`, `gardeBriseeEtourdi`, `ivresseBiere`, `ivresseTournee`, `rugissementProvocation`, `renverse*`, `peauDeFerReduction`, `peauDeFerDuree`, `arcEtourdiPleineCharge`, `nueeRalenti*`, `morgrimCri*`). Icônes `statut_*` générées par `ArtSources/Icones/generer_statuts.py`. Galvanisé agit dans `Sante.Encaisser` (d'après le squelette source du coup) et dans `Statuts.FacteurVitesse`. Peau de fer agit dans `Sante.absorbeur` du héros (`Heros.Absorber`) ; en réseau, `StatutsReseau.Valider` l'accepte sur son propre héros avec les valeurs de l'hôte. Depuis le 27/09/2026, la flèche à pleine charge du [Rôdeur](classe-rodeur.md) pose Étourdi (1 s) et la zone de sa nuée pose Ralenti (40 % tant qu'on y reste).

## Brûlure en paliers {décidé, 30/09/2026}

La brûlure se cumule : plus on brûle un ennemi, plus il brûle fort.

- **Jauge de brûlure** : chaque coup de feu la remplit. Un tic du cône de flammes (4 par seconde) ajoute **15 %** ; la boule de feu ajoute **40 %** à chaque ennemi touché (coup direct ou explosion) {à équilibrer}.
- **Un palier d'un coup** {décidé, 01/10/2026} : la grande boule de feu (tous les ennemis touchés) et le mur de flammes (en y entrant, puis toutes les 1,5 s dedans) montent la brûlure d'un palier entier, jauge gardée ; un ennemi qui ne brûlait pas prend le palier 1, jauge pleine ; au plafond, la jauge se remplit.
- **Monter d'un palier** : au-delà de 100 %, la brûlure passe au palier suivant et la jauge repart du dépassement (110 % → palier suivant, jauge à 10 %), ainsi de suite jusqu'au **palier 3**, le plafond {décidé, 01/10/2026}. Au plafond, la jauge reste pleine.
- **Dégâts par palier** : 5, 8 puis 12 dégâts par seconde {à équilibrer} ; **3 paliers au plus** {décidé, 01/10/2026}.
- **Redescendre** : sans feu reçu pendant **1 s**, la jauge baisse de **75 % par seconde** (un palier en 1,3 s environ) {à équilibrer} ; arrivée à 0, la brûlure descend d'un palier et la jauge repart pleine, ainsi de suite jusqu'au palier 1. La brûlure s'éteint au palier 1 vide, et jamais moins de **3 s** après le dernier coup de feu.
- Au cône seul, il faut environ 1,7 s pour passer au palier 2 et environ 3 s pour atteindre le palier 3 ; depuis le palier 3, la brûlure s'éteint environ 5 s après le dernier coup de feu.
- **Lisibilité** : sur l'icône de Brûlure (au-dessus de l'ennemi, dans le HUD, sur la barre d'un boss), une pastille donne le **chiffre du palier** et la petite jauge du bas devient la **jauge de brûlure**, orange feu. Les flammèches sur l'ennemi sont plus fournies et plus grosses à chaque palier, et le crépitement plus fort.
- **Multijoueur** : l'hôte décide (dégâts, paliers) ; les autres joueurs voient le même palier et la même jauge.

{{dev: `Brulure.Attiser` (montée) et `Brulure.Etat` (redescente calculée à partir de l'instantané du dernier coup de feu : champs `palier`, `jauge`, `descente` du `Statut`), `Statuts.AttiserBrulure` (un client cumule ses remplissages et les envoie à l'hôte au plus toutes les 0,4 s), `Brulure.MonterPalier` et `Statuts.MonterBrulure` (un palier d'un coup ; demande d'un client envoyée aussitôt, remplissage = `Brulure.MarqueurPalier`), `StatutReseau` (palier, jauge et début de la redescente en temps serveur : rien n'est envoyé pendant la redescente). Pastille et jauge : `CaseStatut` (`HudStatuts.cs`, classes `statut__palier`, `statut__jauge--cumul` de `Hud.uss`). Valeurs : `GameBalance.brulureDegatsPaliers` (le nombre d'entrées fixe le plafond), `brulureRemplissageCone`, `brulureRemplissageBoule`, `brulureDelaiDescente`, `brulureVitesseDescente`, `brulureDuree`.}}

## Chute {décidé}

Tomber de haut fait mal, puis ralentit quelques secondes.

- **Hauteur** : du point le plus haut atteint en l'air jusqu'au sol. Un saut sur place (1,2 m) ne blesse jamais ; un niveau du donjon fait 4 m.
- **Seuil** : 3,5 m. En dessous, rien.
- **Dégâts** : 12 par mètre au-delà du seuil, 80 au plus. Une chute de 4 m fait 6 dégâts ; sauter depuis un balcon de 4 m, environ 20.
- **Ralenti** : ensuite, déplacements ralentis de 40 % pendant 3 s.
- **Pas de dégâts** pour une téléportation (portail, réapparition, rappel par Nyxessa) ni pendant un déplacement de compétence (charge bélier, saut percutant).

Seuil, dégâts par mètre, maximum, durée et force du ralenti : {à équilibrer}. {{dev: (`GameBalance` : `chuteSeuil`, `chuteDegatsParMetre`, `chuteDegatsMax`, `chuteRalentiDuree`, `chuteRalentiForce` ; mesure dans `Heros.SuivreChute`)}}

**Grosse chute** {décidé, 26/09/2026} : au-delà d'un second seuil, 7 m {à équilibrer} (`GameBalance.chuteRenverseSeuil`), le héros est en plus **Renversé** avant le Ralenti habituel (voir plus bas).

## Renversé {décidé}

Le héros tombe à la renverse et reste **sans contrôle** (ni déplacement, ni attaque, ni saut) pendant tout le déroulé :

- **Chute** : `Death_A` (rig Medium General), 0,8 s.
- **Au sol** : tenu sur la pose finale de la chute, 0,4 s.
- **Relevé** : `Lie_StandUp`, 2,33 s à vitesse normale, accéléré ×1,5 (environ 1,55 s).

Durées {à équilibrer} (`GameBalance` : `renverseChuteDuree`, `renverseAuSolDuree`, `renverseReleveDuree`, `renverseReleveVitesse`). Il reste vulnérable : être touché en étant à terre reste possible (pas d'invulnérabilité pendant le Renversé, comme l'Étourdi — à revoir si Quentin préfère l'inverse).

**Marteler pour se relever plus vite** {décidé, 26/09/2026} : chaque appui sur **Saut** pendant le Renversé raccourcit le temps au sol puis le relevé, au lieu de faire sauter.

- Chaque appui retire un peu de temps (0,08 s, {à équilibrer} : `renverseMartelementReduction`), plafonné à **50 % au plus** de la durée totale (`renverseMartelementPlafond`) : jamais d'annulation complète.
- **Accessibilité** : « marteler » (par défaut) ou « maintenir » Saut enfoncé, au même rythme maximal (`renverseMartelementIntervalleMaintenir`). Interrupteur local (`OptionsJoueur.RelevageMaintenir`, PlayerPrefs ; pas encore de case dans l'écran Options).
- Une **petite jauge** apparaît sous le héros pendant le Renversé, avec l'invite du bouton Saut et un léger tremblement à chaque appui (`HudRelevage.cs`), locale au joueur renversé.
- **Réseau** : local au joueur renversé, qui accélère son propre relevé ; l'hôte accepte une fin anticipée dans la limite du plafond (`Docs/reseau.md`).

**Utilisé pour** : la charge écrasante de [Morgrim massue](ennemis.md#morgrim-le-roi-des-os) (au lieu de l'Étourdi court d'avant), son onde de choc du Fracas non sautée (en plus des dégâts), et une grosse chute (ci-dessus).

## Où les voir

- **Au-dessus des ennemis** : une rangée de petites icônes au-dessus de la tête de chaque ennemi affecté, avec une jauge de durée discrète sous chaque icône (pour la Brûlure : le chiffre du palier et la jauge de brûlure). Elle n'apparaît que tant qu'il est affecté, s'il est à l'écran et à moins de 30 m.
- **HUD du joueur** : en bas à gauche, au-dessus du portrait et des barres, une case par statut : icône, jauge de durée et secondes restantes.
- **Menu du personnage** (Tab, Y, Triangle) : section « Afflictions ». Survoler un statut à la souris, ou le sélectionner à la manette, affiche son nom, son effet, sa durée restante et sa source.

## Multijoueur

- **L'hôte décide** des statuts de tous, héros comme ennemis. Il les envoie aux autres joueurs seulement quand ils changent, pas en continu.
- Le joueur qui chute voit son ralentissement tout de suite, sans attendre l'hôte.

{{dev: Détails dans `Docs/reseau.md`, « Statuts ».}}
