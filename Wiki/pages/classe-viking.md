# Viking

{video media/classes/viking/rotation.mp4} **Rendu 3D** | Rotation en garde, hache à deux mains | {dev} modèle `Barbarian`, style `Axe2H`, clip `Melee_2H_Idle_Loop` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/viking/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_viking}

La force brute. Sa hache à deux mains frappe tout autour de lui, et sa rage monte au combat, quand il frappe comme quand il encaisse. Il attire les squelettes d'un rugissement, bondit dans la mêlée, et quand la rage est pleine il déclenche sa **Furie** : plus grand, plus rapide, plus violent.

| Rôle | Arme |
|---|---|
| Mêlée, zone | Hache à deux mains |

{dev} Modèle : le barbare KayKit (`Barbarian`), style d'arme hache à deux mains.

## Actions

| | Touche | Action |
|---|---|---|
| {icone viking_hache} | RT | Hache |
| {icone viking_attaque_tournante} | LT | Attaque tournante, maintenue |
| {icone viking_rugissement} | LB | Rugissement |
| {icone viking_saut_percutant} | RB | Saut percutant |
| {icone jauge_rage} | R3 (rage pleine) | **Furie**, l'ultime, à rage pleine {décidé, Quentin 03/10/2026} |
Répartition du rugissement et du saut sur LB et RB : celle de la version 0.1 {à confirmer}.
Touche de la Furie (R3 à la manette, G au clavier) {à confirmer}. Les compétences sont **gratuites**, limitées par leur **recharge** {décidé, Quentin 03/10/2026} ; la jauge de rage ne sert plus qu'à la Furie (voir [Rage et Furie](#rage-et-furie)).

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Viking (`Barbarian`, style `Axe2H`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Viking_Jeu.controller` (généré par `ClassesBuilder.ControleurViking`), déclenché par `ClasseViking`. Toutes les animations : [Animations](animations.md).

### Hache

Les deux coups alternent.

{video media/classes/viking/clips/Melee_2H_Attack_Slice.mp4} **Hache : taille (1er coup)** | {dev} `Melee_2H_Attack_Slice` | Arme : hache à deux mains | une fois · 1,10 s | {dev} vitesse calée sur `hacheIntervalle`
{video media/classes/viking/clips/Melee_2H_Attack_Chop.mp4} **Hache : coup de haut en bas (2e coup)** | {dev} `Melee_2H_Attack_Chop` | Arme : hache à deux mains | une fois · 1,63 s | {dev} vitesse calée sur `hacheIntervalle`

### Attaque tournante

{video media/classes/viking/clips/Melee_2H_Attack_Spin.mp4} **Attaque tournante : élan** | {dev} `Melee_2H_Attack_Spin` | Arme : hache à deux mains | une fois · 2,40 s | {dev} début du clip, jusqu'à 1,2 s
{video media/classes/viking/clips/Melee_2H_Attack_Spinning.mp4} **Attaque tournante : tourbillon, tant que la touche est tenue** | {dev} `Melee_2H_Attack_Spinning` | Arme : hache à deux mains | boucle · 0,67 s | {dev} copie bouclante `Melee_2H_Attack_Spinning_Loop`
{video media/classes/viking/clips/Melee_2H_Attack_Spin.mp4} **Attaque tournante : fin** | {dev} `Melee_2H_Attack_Spin` | Arme : hache à deux mains | une fois · 2,40 s | {dev} fin du même clip, à partir de 1,25 s

### Rugissement

{video media/classes/viking/clips/Skeletons_Taunt_Longer.mp4} **Rugissement** | {dev} `Skeletons_Taunt_Longer` | Arme : hache à deux mains | une fois · 3,00 s | {dev} emprunté au pack Skeletons, vitesse ×1,6 ; geste gardé (V2 écartée par Quentin, 26/09/2026)

### Saut percutant

{video media/classes/viking/clips/Melee_1H_Attack_Jump_Chop.mp4} **Saut percutant** | {dev} `Melee_1H_Attack_Jump_Chop` | Arme : hache à deux mains (clip emprunté à une prise à une main) | une fois · 1,33 s | {dev} vitesse ×1,2 ; bond de 5 m translaté par script

## Règles

- Hache à deux mains **uniquement** {décidé}. Pas de bouclier.
- **Attaque tournante** {décidé} : le viking tourne sur lui-même, hache tendue, et frappe tout autour de lui {{dev: (animations KayKit `Melee_2H_Attack_Spin` et `Melee_2H_Attack_Spinning`)}}.
  - **Maintenue** {décidé} : tant que la touche est tenue, le viking tourne, **3 secondes au plus**, puis la compétence **recharge 10 secondes** à partir de la fin du tourbillon (relâchée ou non) {à équilibrer}. Elle est gratuite : l'ancienne règle « elle consomme de la rage en continu et s'arrête quand la rage est vide » est supprimée (03/10/2026). Un appui pendant la recharge ne fait rien ; l'icône montre la recharge comme celles des autres classes.
- **Rugissement** : un crâne de barbare casqué en gemmes rouges surgit au-dessus du viking, rugit, et une onde part de lui puis revient comme pour dire « venez » {effet validé}.
- **Saut percutant** : le viking bondit d'environ 5 m vers l'avant et frappe le sol, une onde de terre part du point d'impact {effet validé}.
- **Rage** {décidé, 03/10/2026} : jauge de 100, qui **monte au combat** (il frappe, il encaisse) et redescend lentement hors combat. **Les compétences ne coûtent plus de rage** : elles sont gratuites, limitées par leur recharge. Une jauge pleine ouvre la **Furie**, son ultime. Détail et valeurs : section [Rage et Furie](#rage-et-furie).
- **Valeurs de départ** de la version 0.2 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 140 |
| Hache | 38 dégâts, touche tous les ennemis de l'arc ; +8 rage par ennemi touché ; **recul de 0,4 m** (+5 % par point de Force gagné) {à équilibrer} |
| Rage | jauge de 100 ; +8 par ennemi touché à la hache, +0,5 par point de dégât encaissé (12 au plus par coup reçu), +1 par ennemi touché par un tic de la tournante ; baisse de 6 par seconde après 4 s sans toucher ni être touché ; **plancher 0** (le plancher de 30 du 27/09/2026 est supprimé : plus de coût à payer) {à équilibrer} |
| Attaque tournante | **gratuite** ; 10 dégâts toutes les 0,3 s, rayon 2,3 m ; vitesse ×0,6 ; **3 s de maintien au plus, recharge 10 s** {à équilibrer} |
| Rugissement | **gratuit** ; recharge 12 s ; Peau de fer 6 s sur le viking |
| Saut percutant | **gratuit** ; 45 dégâts ; recharge 8 s |
| **Furie** | rage pleine ; 10 s ; voir [Rage et Furie](#rage-et-furie) {à équilibrer} |

## Rage et Furie

{décidé, Quentin 03/10/2026} « La rage, on la fait monter au combat ; les compétences sont gratuites avec recharge ; et un ultime quand la rage est à fond : le barbare grossit un peu, court plus vite, tape légèrement plus vite, a plus de recul, etc. » Puis : « Déclencher la Furie me semble indispensable » : la Furie **ne part pas toute seule**, le joueur la déclenche.

### La rage monte

- **Il frappe** : +8 par ennemi touché avec la hache (déjà le cas), +1 par ennemi touché par un tic de l'attaque tournante {à équilibrer}.
- **Il encaisse** {décidé} : la rage monte aussi quand il est touché ; valeurs {à équilibrer} : +0,5 de rage par point de dégât subi (après Peau de fer), **12 au plus par coup reçu**. Ni la chute, ni la brûlure ne comptent.
- **Elle redescend toujours lentement hors combat** : 6 par seconde après 4 s sans toucher d'ennemi **ni être touché** : recevoir un coup compte comme du combat, sinon la rage gagnée en encaissant s'évaporerait aussitôt. Plancher 0.
- Règle ajoutée par le code, à valider : la baisse attend aussi qu'on ait cessé d'être touché, et le plancher passe de 30 à 0 {à confirmer}.

### Les compétences sont gratuites

L'attaque tournante (LT maintenu), le rugissement (LB) et le saut percutant (RB) ne coûtent plus de rage : seule leur recharge les limite (tableau ci-dessus). L'amélioration « Tourbillon » ([Classes](classes.md)) raccourcit désormais la recharge de la tournante de 15 % par rang, au lieu de sa consommation de rage.

### La Furie, ultime du Viking {décidé}

- **Prête** : à rage 100, la jauge du HUD pulse, le bandeau « FURIE prête » apparaît sous le portrait avec l'invite du bouton, et un petit son l'annonce. Avant 100, l'appui ne fait rien. Si le joueur ne la déclenche pas, elle reste prête tant que la rage ne retombe pas (la baisse hors combat s'applique comme d'habitude : 4 s sans combat, puis 6 par seconde). La règle de baisse n'a pas changé.
- **Déclenchement** : le joueur appuie sur **R3** (clic du stick droit) à la manette, **G** au clavier (touches {à confirmer}) : action `Ultimate`. L'appui est accepté en marchant, pendant la hache ou l'attaque tournante (l'action continue) ; pas pendant le rugissement ni le saut percutant. Il rugit (cri sur le haut du corps quand il est libre), grossit, une gerbe de gemmes rouges éclate.
- **Pendant la Furie** : la jauge se **vide à vitesse fixe, de 100 à 0 en 10 secondes**, et ne monte plus (les coups donnés et reçus ne donnent plus de rage). À 0, la Furie prend fin : le viking reprend sa taille en douceur, l'aura s'éteint en quelques braises, un souffle marque le retour au calme. Mourir y met fin aussitôt.
- **Bonus** {à équilibrer} :

| Bonus | Valeur |
|---|---|
| Taille | **×1,15** (le modèle seul : ni capsule, ni caméra) |
| Vitesse de déplacement | **+20 %** |
| Cadence de la hache | **+15 %** (coup plus tôt, geste plus rapide) |
| Recul de la hache | **×1,5** (0,4 m → 0,6 m par ennemi touché) |
| Dégâts | **+10 %** sur tous ses coups |
| Recharge des compétences | s'écoule **1,5 fois plus vite** |

- **Multijoueur** : l'état de Furie est répliqué comme le mode furtif de l'Assassin : les autres joueurs voient le viking grossir et son aura rouge ; ses bonus sont simulés par son propre poste, comme le reste de ses statistiques.
- **Visuel** : aura de gemmes low poly du thème **Rage** (rouge sombre, rouge vif, rouge pâle ; jamais de vert), petite lumière rouge ; fiche dans `Docs/vfx.md`. Son : le rugissement existant à l'entrée, un souffle de la hache à la sortie, le son « prêt » de l'interface ; sons dédiés {à confirmer}.

{dev} Code : `ClasseViking` (rage, recharges, `DeclencherFurie`, `EntrerFurie`, `SortirFurie`, échelle lissée du modèle, recul `Reculer`), `AuraFurie` (`Assets/Scripts/Jeu/Classes/`), `ClasseHeros` (`UltimeActif`, `UltimePret`, `UltimeProche`, `FacteurDegats`, `SurMort`), `Squelette.Pousser` (recul sans étourdissement, relayé à l'hôte par `EnnemiReseau`), `HerosReseau` (variable `m_Furie`, comme `m_Furtif`), action `Gameplay/Ultimate` de `DeathlessControls` (R3, G, relayée par `InputChordResolver`), HUD (`IEtatJoueurUltime`, `EcranHud.MajUltime`, bandeau `furie` de `Hud.uxml`). Valeurs : `GameBalance` (`rageParDegatRecu`, `rageRecuMax`, `tournanteDureeMax`, `tournanteRecharge`, `hacheRecul`, `furie*`, `rageMin` = 0). L'animation de la hache suit la cadence par le paramètre `VitesseAttaque` du contrôleur `Viking_Jeu` (états `Attaque1` et `Attaque2`). Banc : `ScenariosClasses.Lancer("viking_furie")`.

## Lissage du 27/09/2026 {décidé}

{{dev: **Remplacé en partie le 03/10/2026** : le plancher de rage à 30 et les coûts en rage (saut 25, rugissement 15, tournante 10 par seconde) n'existent plus (compétences gratuites avec recharge, voir [Rage et Furie](#rage-et-furie)) ; Peau de fer et le cri sur le haut du corps restent.}}

Suite à l'audit d'équilibrage (`Docs/equilibrage-classes.md`) : le viking partait à zéro rage à chaque vague (rage vide en 20 s, vagues à 40 s d'écart), donc sans saut ni rugissement au début de chaque assaut, et son rugissement l'immobilisait 1,5 s sans protection. Décidé par Quentin :

- **Plancher de rage : 30**. La rage ne descend jamais sous 30 hors combat ; il a toujours de quoi ouvrir une vague.
- **Coûts** : saut percutant 35 → **25**, rugissement 25 → **15**, attaque tournante 12 → **10** de rage.
- **Rugissement** : il pose **Peau de fer** sur le viking (statut : **−35 % de dégâts subis pendant 6 s**, bienfait, liseré or dans le HUD) et se joue sur le **haut du corps** : il continue de marcher pendant le cri.

{dev} `GameBalance` (rageMin, coûts), `ClasseViking` (plancher, Peau de fer par le chemin des statuts, couche haute de l'Animator comme la charge du paladin), nouveau statut `PeauDeFer` dans le catalogue (icône à générer), réseau par `HerosReseau.StatutRpc` existant.

{{dev: Fait le 27/09/2026. Plancher : hors combat (4 s sans toucher), `m_Rage` va vers `rageMin` dans les deux sens (`Mathf.MoveTowards`, `rageBaisse`/s) : après un rugissement à 30 (reste 15), la jauge remonte à 30 en 2,5 s ; la tournante seule peut vider la jauge. Peau de fer : posée **au moment du cri** (1 s après l'appui, quand la provocation part), `Statuts.Ajouter(PeauDeFer, peauDeFerDuree 6 s, peauDeFerReduction 0,35, Joueur)` ; effet dans `Sante.absorbeur` du héros (`Heros.Absorber`), donc sur tout coup ennemi, parable ou non, chez le propriétaire (prédit puis confirmé ; `StatutsReseau.Valider` impose les valeurs de l'hôte). Cri sur la couche haute : état `Rugissement` dans `HautDuCorps` du contrôleur `Viking_Jeu` (`ClassesBuilder.ControleurViking`), `ClasseViking.HautDuCorps` vrai pendant le cri, déplacement libre à vitesse normale (plus de `DeplacementImpose` immobile).}}

