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
| {icone mage_grande_boule} | LB | Grande boule de feu : lente, explosion de 5 m, un palier de brûlure de plus à tous les touchés |
| {icone mage_mur_de_flammes} | RB | Mur de flammes : ligne de feu de 8 m posée devant lui, qui brûle et ralentit ce qui la traverse |
| {icone mage_brulure} |  | Brûlure : les ennemis touchés brûlent un moment |
| {icone jauge_mana} |  | Jauge de mana |

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Mage (`Mage`, style `Staff`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Mage_Jeu.controller` (généré par `ClassesBuilder.ControleurMage` ; grande boule et mur ajoutés par `ClassesBuilder.AjouterSortsMage`, menu Deathless > Jeu > 7b), déclenché par `ClasseMage`. Toutes les animations : [Animations](animations.md).

### Boule de feu

{video media/classes/mage/clips/Ranged_Magic_Shoot.mp4} **Boule de feu : lancer** | {dev} `Ranged_Magic_Shoot` | Arme : bâton | une fois · 0,93 s | {dev} haut du corps, vitesse ×1,3 : on marche en lançant

### Cône de flammes

{video media/classes/mage/clips/Ranged_Magic_Spellcasting.mp4} **Cône de flammes : incantation maintenue** | {dev} `Ranged_Magic_Spellcasting` | Arme : bâton | boucle · 0,67 s | {dev} haut du corps, copie bouclante `Ranged_Magic_Spellcasting_Loop`

### Grande boule de feu

Le mage lève le bâton et ramasse le feu (environ 0,6 s), puis lance la boule (elle part vers 0,8 s). {dev} Haut du corps : `Ranged_Magic_Raise` (2,1 s) accéléré pour durer 0,58 s, puis `Ranged_Magic_Shoot` à ×1,3 (déclencheur `GrandeBoule`). Vidéo à tourner.

### Mur de flammes

Le mage frappe vers le sol avec son bâton ; le mur prend à 0,5 s. {dev} Haut du corps : `Ranged_Magic_Summon` (4,3 s) accéléré pour durer 1 s (déclencheur `Mur`). Vidéo à tourner.

{dev} Brûlure : pas de geste (effet posé sur les ennemis touchés).

## Règles

- Bâton. L'attaque de base est une **boule de feu** qui explose à l'impact, puis laisse une fumée à facettes qui se dissipe {effet validé}.
- **Cône de flammes** maintenu devant le mage {effet validé}. Il **ralentit de 40 %** les ennemis qui sont dedans {décidé, 01/10/2026} : statut [Ralenti](statuts.md), renouvelé tant qu'ils y restent.
- **Grande boule de feu** (LB) {décidé, 01/10/2026} : une boule de feu plus grosse et plus lente, lancée en 0,8 s environ (le mage avance au ralenti pendant le geste). À l'impact, une explosion de **5 m** : gros dégâts à la cible touchée, un peu moins autour, et tous les touchés **montent d'un palier de brûlure d'un coup** (un ennemi qui ne brûlait pas prend le palier 1). Elle coûte du mana et a une recharge. Pas de bonus de mana par ennemi touché (c'est celui de la boule de feu). Effet : la boule de feu en plus gros, explosion plus large dans le même langage de gemmes {à confirmer}.
- **Mur de flammes** (RB) {décidé, 01/10/2026} : une **ligne de feu de 8 m** posée au sol à 4 m devant le mage, en travers de sa visée, qui brûle **5 s**. Un ennemi qui le **traverse ou s'y tient** brûle (**+1 palier de brûlure** en y entrant, puis un autre toutes les 1,5 s s'il y reste) et est **ralenti de 40 %**. Il ne bloque pas le passage : il le rend coûteux. Sert à **fermer un passage** (pont, gué) devant une vague. Effet : rangée de flammes en gemmes de feu sur un lit de braises, qui court du milieu vers les bouts en apparaissant et s'éteint par la taille {à confirmer}.
- **Brûlure** : les ennemis touchés brûlent pendant un moment {effet validé}. C'est un [statut](statuts.md) : son icône s'affiche au-dessus de l'ennemi. Elle **se cumule en paliers** {décidé, 30/09/2026} : chaque coup de feu remplit la jauge de brûlure de l'ennemi, pleine elle monte d'un palier (**3 au plus** {décidé, 01/10/2026}), et sans feu elle redescend palier par palier (voir [Brûlure en paliers](statuts.md#brûlure-en-paliers)). La grande boule et le mur montent la brûlure d'un palier entier d'un coup.
- **Mana** {décidé} : jauge de 100. Elle remonte de **3 par seconde** {décidé, 01/10/2026}, plus un bonus à chaque ennemi touché par la boule de feu. Le cône de flammes en consomme tant qu'il est maintenu ; la grande boule et le mur ont un coût fixe et une recharge. Valeurs {à équilibrer}.
  - **Pas de régénération pendant le cône** : le mana ne remonte pas tant que le cône est maintenu.
  - **Démarrage du cône** : il faut au moins 25 % de son coût par seconde, soit 2,5 de mana, pour le lancer.
  - **Sort coupé** : si une esquive, un étourdissement ou la mort coupe la grande boule ou le mur avant qu'ils partent, le mana et la recharge sont rendus.
  - Dans le HUD, l'emplacement d'une compétence est grisé tant qu'il manque du mana pour la lancer.
- **Coup critique** {décidé, 01/10/2026} : **5 %** de chance qu'une boule de feu ou une grande boule de feu fasse un **critique ×2** {à équilibrer} (même effet et même son que les autres critiques) ; la brûlure ne critique pas. Pas encore codé.
- **Pas d'ultime** {décidé} : le mage garde la boule de feu, le cône de flammes, la grande boule, le mur et la brûlure.
- **Valeurs de départ** {à équilibrer} (01/10/2026) {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset` : `bouleDegats…`, `cone…`, `grandeBoule…`, `mur…`, `mana…`, `brulure…`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 100 |
| Boule de feu (RT) | 25 dégâts à la cible touchée, 15 aux autres ennemis dans un rayon de 2 m (la cible touchée ne prend pas les 15 en plus) ; une toutes les 0,9 s ; gratuite |
| Cône de flammes (LT) | **30 dégâts par seconde, 10 mana par seconde** ; 6 m, ±20° ; **Ralenti −40 %** (0,5 s, renouvelé à chaque tic) |
| Grande boule de feu (LB) | **60 dégâts à la cible touchée, 35 aux autres** dans un rayon de **5 m** ; **+1 palier de brûlure** à tous les touchés ; lancer 0,8 s, vol à 12 m/s ; **35 mana, recharge 10 s** |
| Mur de flammes (RB) | ligne de **8 m** à 4 m devant le mage, 1,4 m d'épaisseur, **5 s** ; **+1 palier de brûlure** en y entrant (puis toutes les 1,5 s dedans), **Ralenti −40 %** ; **30 mana, recharge 14 s** |
| Mana | 100 ; **+3 par seconde** ; +4 par ennemi touché par la boule de feu |
| Brûlure | 5, 8 puis 12 dégâts par seconde selon le palier (1 à 3, 3 au plus) ; jauge : +15 % par tic du cône, +40 % par boule, un palier entier par la grande boule et le mur ; redescente après 1 s sans feu, 75 % de jauge par seconde ; au moins 3 s après le dernier coup de feu |

{dev} Code : `ClasseMage` (actions `Skill1` = LB, `Skill2` = RB, lues par `InputChordResolver`), `Brulure.MonterPalier` / `Statuts.MonterBrulure` (un palier d'un coup ; un client l'envoie à l'hôte avec le marqueur `Brulure.MarqueurPalier`), effet `MurDeFlammes` (`Assets/VFX/MurDeFlammes/`, fiche dans `Docs/vfx.md`), projectile `ProjectileJeu.Genre.GrandeBouleDeFeu`. Multijoueur : l'hôte applique dégâts et statuts ; les autres joueurs voient la grande boule (même tir rejoué, `HerosReseau.Tir`) et le mur (effet 5 de `ClasseMage`). Banc : `ScenariosClasses.Lancer("mage_kit")`.
