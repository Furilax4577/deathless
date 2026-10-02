# Mage

{video media/classes/mage/rotation.mp4} **Rendu 3D** | Rotation en attente, bâton en main | {dev} modèle `Mage`, style `Staff`, clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/mage/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_mage_feu}

Il frappe de loin et **tient une zone**. Pour l'instant, il maîtrise le **feu** : ses boules de feu explosent et laissent les ennemis en flammes, son cône de flammes freine ceux qui approchent, sa grande boule embrase un groupe entier et son mur de flammes ferme un passage. Ses sorts coûtent du mana.

**Styles de magie** {décidé} : la classe s'appelle simplement « Mage », sans élément dans son nom. Le feu est son premier style ; plus tard, le mage pourra **changer de style**. Autres styles et façon d'en changer : {à confirmer}.

| Rôle | Arme |
|---|---|
| Distance, zone | Bâton |

{dev} Modèle : le mage KayKit (`Mage`), style d'arme bâton.

## Actions

Kit décidé par Quentin le 01/10/2026 {décidé, 01/10/2026} : le mage tient une zone (refonte (c) de l'audit d'équilibrage, avec la grande boule de feu à la place du brasier et le mur de flammes à la place de la déflagration).

| | Touche | Action |
|---|---|---|
| {icone mage_boule_de_feu} | RT | Boule de feu |
| {icone mage_cone_de_flammes} | LT | Cône de flammes, maintenu : brûle et ralentit |
| {icone mage_grande_boule} | LB, puis RT | Grande boule de feu : lente, explosion de 5 m, un palier de brûlure de plus à tous les touchés. **On vise d'abord** (cercle au sol), RT confirme, LT annule |
| {icone mage_mur_de_flammes} | RB, puis RT | Mur de flammes : ligne de feu de 8 m posée là où on vise, qui brûle et ralentit ce qui la traverse. **On vise d'abord**, RT confirme, LT annule |
| {icone mage_brulure} |  | Brûlure : les ennemis touchés brûlent un moment |
| {icone jauge_mana} |  | Jauge de mana |

Au clavier et à la souris : A et R ouvrent la visée, le **clic gauche** confirme, le **clic droit** annule (voir [Commandes](commandes.md#viser-une-zone-au-sol)).

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Mage (`Mage`, style `Staff`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Mage_Jeu.controller` (généré par `ClassesBuilder.ControleurMage` ; grande boule et mur ajoutés par `ClassesBuilder.AjouterSortsMage`, menu Deathless > Jeu > 7b), déclenché par `ClasseMage`. Toutes les animations : [Animations](animations.md).

### Boule de feu

{video media/classes/mage/clips/Ranged_Magic_Shoot.mp4} **Boule de feu : lancer** | {dev} `Ranged_Magic_Shoot` | Arme : bâton | une fois · 0,93 s | {dev} haut du corps, vitesse ×1,3 : on marche en lançant

### Cône de flammes

{video media/classes/mage/clips/Ranged_Magic_Spellcasting.mp4} **Cône de flammes : incantation maintenue** | {dev} `Ranged_Magic_Spellcasting` | Arme : bâton | boucle · 0,67 s | {dev} haut du corps, copie bouclante `Ranged_Magic_Spellcasting_Loop`

### Grande boule de feu

Après la confirmation de la visée, le mage lève le bâton et ramasse le feu (environ 0,6 s), puis lance la boule (elle part vers 0,8 s). Pendant la visée elle-même, il garde la pose d'incantation du cône (haut du corps), bâton levé. {dev} Haut du corps : `Ranged_Magic_Raise` (2,1 s) accéléré pour durer 0,58 s, puis `Ranged_Magic_Shoot` à ×1,3 (déclencheur `GrandeBoule`). Vidéo à tourner.

### Mur de flammes

Après la confirmation de la visée, le mage frappe vers le sol avec son bâton ; le mur prend à 0,5 s, au point visé. {dev} Haut du corps : `Ranged_Magic_Summon` (4,3 s) accéléré pour durer 1 s (déclencheur `Mur`). Vidéo à tourner.

{dev} Brûlure : pas de geste (effet posé sur les ennemis touchés).

## Règles

- Bâton. L'attaque de base est une **boule de feu** qui explose à l'impact, puis laisse une fumée à facettes qui se dissipe {effet validé}.
- **Cône de flammes** maintenu devant le mage {effet validé}. Il **ralentit de 40 %** les ennemis qui sont dedans {décidé, 01/10/2026} : statut [Ralenti](statuts.md), renouvelé tant qu'ils y restent.
- **Grande boule de feu** (LB) {décidé, 01/10/2026} : une boule de feu plus grosse et plus lente, lancée en 0,8 s environ après la confirmation de la visée (le mage avance au ralenti pendant le geste). À l'impact, une explosion de **5 m** : gros dégâts à l'ennemi du cœur, un peu moins autour, et tous les touchés **montent d'un palier de brûlure d'un coup** (un ennemi qui ne brûlait pas prend le palier 1). Depuis la visée au sol (02/10/2026) elle **tombe en cloche sur le point visé** et explose au centre du cercle, quoi qu'il y ait sur son chemin ; le « cœur » est l'ennemi le plus proche du centre, à 1,5 m au plus {à confirmer}. Elle coûte du mana et a une recharge. Pas de bonus de mana par ennemi touché (plus aucune boule n'en rend depuis le 01/10/2026). Effet : la boule de feu en plus gros, explosion plus large dans le même langage de gemmes {à confirmer}.
- **Mur de flammes** (RB) {décidé, 01/10/2026} : une **ligne de feu de 8 m** posée au sol **là où on la vise** (avant le 02/10/2026 : à 4 m devant le mage), en travers de la ligne qui va du mage au point visé, qui brûle **5 s**. Un ennemi qui le **traverse ou s'y tient** brûle (**+1 palier de brûlure** en y entrant, puis un autre toutes les 1,5 s s'il y reste) et est **ralenti de 40 %**. Il ne bloque pas le passage : il le rend coûteux. Sert à **fermer un passage** (pont, gué) devant une vague. Effet : rangée de flammes en gemmes de feu sur un lit de braises, qui court du milieu vers les bouts en apparaissant et s'éteint par la taille {à confirmer}.
- **Viser une zone au sol** {décidé, 02/10/2026} (« le mage doit voir où le sort va tomber ; clic droit annule, clic gauche confirme ; mécanique à reprendre avec l'archer ») : la grande boule et le mur ne partent plus à l'appui sur LB / RB. L'appui **ouvre la visée** : un **indicateur de zone** apparaît au sol, à la taille réelle de la zone d'effet (cercle de 5 m pour la grande boule, ligne de 8 m × 1,4 m pour le mur), en gemmes aux couleurs du feu, et **suit le point que le réticule vise**, posé sur le relief. **RT / clic gauche confirme** et lance le sort sur ce point ; **LT / clic droit annule**. C'est la même mécanique que la nuée de flèches du [Rôdeur](classe-rodeur.md), construite une seule fois (voir [Commandes](commandes.md#viser-une-zone-au-sol)). Détails, tous {à confirmer} :
  - **Portée** : le cercle ne dépasse pas **20 m** du mage (grande boule) ou **14 m** (mur) ; il reste à plus de 1,5 m de lui. Si le réticule vise le ciel ou l'horizon, le cercle se pose à la portée maximale. Si le point n'a pas de sol dessous (vide), l'indicateur se tamise et la confirmation est refusée.
  - **Coût** : **ni mana, ni recharge** pendant la visée. Ils partent à la **confirmation**, pas avant ; annuler ne coûte rien.
  - **Ralenti** : le mage marche à **50 % de sa vitesse** pendant la visée (comme l'archer qui bande), sans sprint ni saut ; il fait face à la caméra et garde le bâton levé.
  - **Mur** : la ligne est posée **en travers** de la direction mage → point visé, pour fermer un passage devant le mage.
  - **Changer de sort** : appuyer sur l'autre compétence (LB ↔ RB) pendant la visée bascule l'indicateur sur l'autre sort, sans rien coûter, s'il est prêt (mana, recharge).
  - **Coupée sans coût** : l'esquive, un étourdissement, la mort, un portail, l'ouverture du menu du personnage ou de la roue à emotes ferment la visée.
  - **Annuler avec LT** : LT n'ouvre pas le cône de flammes tant qu'il n'a pas été relâché après l'annulation.
  - **Pendant la visée**, la boule de feu (RT) ne part pas : RT confirme. Hors visée, rien ne change : RT lance la boule, LT maintenu fait le cône.
  - **Réseau** : l'indicateur n'est visible **que du joueur qui vise**. À la confirmation, le tir (grande boule) ou l'effet (mur) part avec le point visé vers les autres postes ; les dégâts et statuts suivent le chemin habituel (poste du mage, puis hôte).
  - **Bandeau** : pendant la visée, le HUD affiche le nom du sort et les invites des deux boutons (Confirmer, Annuler), qui suivent la manette ou le clavier-souris.
- **Brûlure** : les ennemis touchés brûlent pendant un moment {effet validé}. C'est un [statut](statuts.md) : son icône s'affiche au-dessus de l'ennemi. Elle **se cumule en paliers** {décidé, 30/09/2026} : chaque coup de feu remplit la jauge de brûlure de l'ennemi, pleine elle monte d'un palier (**3 au plus** {décidé, 01/10/2026}), et sans feu elle redescend palier par palier (voir [Brûlure en paliers](statuts.md#brûlure-en-paliers)). La grande boule et le mur montent la brûlure d'un palier entier d'un coup.
- **Mana** {décidé} : jauge de 100. Elle remonte de **3 par seconde** {décidé, 01/10/2026}, et c'est **la seule source de mana** : la boule de feu **ne rend plus de mana** quand elle touche un ennemi {décidé, 01/10/2026} (avant : +4 par ennemi touché). Le cône de flammes en consomme tant qu'il est maintenu ; la grande boule et le mur ont un coût fixe et une recharge. Valeurs {à équilibrer}. {{dev: Fait le 01/10/2026 : `GameBalance.manaParTouche` à 0 (réglage gardé, `ClasseMage.Toucher` ne l'applique que s'il est positif).}}
  - **Pas de régénération pendant le cône** : le mana ne remonte pas tant que le cône est maintenu.
  - **Démarrage du cône** : il faut au moins 25 % de son coût par seconde, soit 2,5 de mana, pour le lancer.
  - **Sort coupé** : si une esquive, un étourdissement ou la mort coupe la grande boule ou le mur avant qu'ils partent, le mana et la recharge sont rendus.
  - Dans le HUD, l'emplacement d'une compétence est grisé tant qu'il manque du mana pour la lancer.
- **Coup critique** {décidé, 01/10/2026} : **5 %** de chance qu'une boule de feu ou une grande boule de feu fasse un **critique ×2** {à équilibrer} (même effet et même son que les autres critiques) ; la brûlure ne critique pas. Un seul tirage par boule : un critique double les dégâts de toute l'explosion (coup direct et zone). {{dev: Fait le 01/10/2026 : `GameBalance.mageCritiqueChance` (0,05) et `mageCritiqueMultiplicateur` (2), `ClasseMage.TirerCritique` ; tiré par le poste du mage comme les autres critiques, marque `Combat.Critique` rejouée chez les autres, `InfoDegats.critique` pour le score et les chiffres de dégâts.}}
- **Pas d'ultime** {décidé} : le mage garde la boule de feu, le cône de flammes, la grande boule, le mur et la brûlure.
- **Valeurs de départ** {à équilibrer} (01/10/2026) {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset` : `bouleDegats…`, `cone…`, `grandeBoule…`, `mur…`, `mana…`, `brulure…`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 100 |
| Boule de feu (RT) | 25 dégâts à la cible touchée, 15 aux autres ennemis dans un rayon de 2 m (la cible touchée ne prend pas les 15 en plus) ; une toutes les 0,9 s ; gratuite |
| Cône de flammes (LT) | **30 dégâts par seconde, 10 mana par seconde** ; 6 m, ±20° ; **Ralenti −40 %** (0,5 s, renouvelé à chaque tic) |
| Grande boule de feu (LB) | **60 dégâts à l'ennemi du cœur (à 1,5 m du centre au plus), 35 aux autres** dans un rayon de **5 m** ; **+1 palier de brûlure** à tous les touchés ; portée de visée **20 m** ; lancer 0,8 s après la confirmation, vol à 12 m/s ; **35 mana, recharge 10 s** (partent à la confirmation) |
| Mur de flammes (RB) | ligne de **8 m** posée au point visé (au plus à **14 m**), 1,4 m d'épaisseur, **5 s** ; **+1 palier de brûlure** en y entrant (puis toutes les 1,5 s dedans), **Ralenti −40 %** ; **30 mana, recharge 14 s** (partent à la confirmation) |
| Visée d'une zone | marche à **50 %** ; zone à 1,5 m du mage au moins {à confirmer} |
| Mana | 100 ; **+3 par seconde**, rien d'autre (plus de regain par ennemi touché depuis le 01/10/2026) |
| Brûlure | 5, 8 puis 12 dégâts par seconde selon le palier (1 à 3, 3 au plus) ; jauge : +15 % par tic du cône, +40 % par boule, un palier entier par la grande boule et le mur ; redescente après 1 s sans feu, 75 % de jauge par seconde ; au moins 3 s après le dernier coup de feu |

{dev} Visée : `VisiereZone` (état et indicateur) et ses règles d'entrée et d'interruption dans `ClasseHeros` (`CommencerVisee`, `ViseeSurAction`, `ViseeMaj`), communes au mage et au rôdeur ; point visé par `Combat.PointViseSol` ; indicateur `ZoneVisee` (`Assets/VFX/ZoneVisee/`, fiche dans `Docs/vfx.md`) ; bandeau `HudVisee` (`IViseeZone`, `DonneesUI.Visee`) ; réglages `GameBalance` (`grandeBoulePortee`, `grandeBouleCoeur`, `murPortee`, `viseeZoneVitesse`, `viseeZoneDistanceMin`) ; la grande boule est un tir en cloche sur point (`ProjectileJeu`, genre `GrandeBouleDeFeu`). Banc : `ScenariosClasses.Lancer("mage_visee")`.

{dev} Code : `ClasseMage` (actions `Skill1` = LB, `Skill2` = RB, lues par `InputChordResolver`), `Brulure.MonterPalier` / `Statuts.MonterBrulure` (un palier d'un coup ; un client l'envoie à l'hôte avec le marqueur `Brulure.MarqueurPalier`), effet `MurDeFlammes` (`Assets/VFX/MurDeFlammes/`, fiche dans `Docs/vfx.md`), projectile `ProjectileJeu.Genre.GrandeBouleDeFeu`. Multijoueur : l'hôte applique dégâts et statuts ; les autres joueurs voient la grande boule (même tir rejoué, `HerosReseau.Tir`) et le mur (effet 5 de `ClasseMage`). Banc : `ScenariosClasses.Lancer("mage_kit")`.
