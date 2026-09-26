# Mage

{video media/classes/mage/rotation.mp4} **Rendu 3D** | Rotation en attente, bâton en main | {dev} modèle `Mage`, style `Staff`, clip `Idle_A` ; rendu par `RendusClasses` (`sandbox-rig`)
{image media/classes/mage/portrait.png} **Portrait** | De 3/4 face

{icone-grande classe_mage_feu}

Il frappe de loin et en zone. Pour l'instant, il maîtrise le **feu** : ses boules de feu explosent et laissent les ennemis en flammes, son cône de flammes balaie ceux qui approchent. Tout lui coûte du mana.

**Styles de magie** {décidé} : la classe s'appelle simplement « Mage », sans élément dans son nom. Le feu est son premier style ; plus tard, le mage pourra **changer de style**. Autres styles et façon d'en changer : {à confirmer}.

| Rôle | Arme |
|---|---|
| Distance, zone | Bâton |

{dev} Modèle : le mage KayKit (`Mage`), style d'arme bâton.

## Actions

| | Touche | Action |
|---|---|---|
| {icone mage_boule_de_feu} | RT | Boule de feu |
| {icone mage_cone_de_flammes} | LT | Cône de flammes, maintenu |
|  | LB | Vide pour l'instant |
|  | RB | Vide pour l'instant |
| {icone mage_brulure} |  | Brûlure : les ennemis touchés brûlent un moment |
| {icone jauge_mana} |  | Jauge de mana |

Actions communes à toutes les classes : voir [Classes](classes.md#actions-communes).

## Clips des compétences

Les gestes joués en jeu pour chaque action, dans l'ordre où ils s'enchaînent. Vidéos sur le vrai modèle du Mage (`Mage`, style `Staff`), équipé comme en jeu, caméra fixe de 3/4, sol quadrillé tous les mètres (même cadrage que la page [Animations](animations.md)).

{dev} Source : contrôleur `Assets/Jeu/Animation/Mage_Jeu.controller` (généré par `ClassesBuilder.ControleurMage`), déclenché par `ClasseMage`. Toutes les animations : [Animations](animations.md).

### Boule de feu

{video media/classes/mage/clips/Ranged_Magic_Shoot.mp4} **Boule de feu : lancer** | {dev} `Ranged_Magic_Shoot` | Arme : bâton | une fois · 0,93 s | {dev} haut du corps, vitesse ×1,3 : on marche en lançant

### Cône de flammes

{video media/classes/mage/clips/Ranged_Magic_Spellcasting.mp4} **Cône de flammes : incantation maintenue** | {dev} `Ranged_Magic_Spellcasting` | Arme : bâton | boucle · 0,67 s | {dev} haut du corps, copie bouclante `Ranged_Magic_Spellcasting_Loop`

{dev} Brûlure : pas de geste (effet posé sur les ennemis touchés). LB et RB : vides.

## Règles

- Bâton. L'attaque de base est une **boule de feu** qui explose à l'impact, puis laisse une fumée à facettes qui se dissipe {effet validé}.
- **Cône de flammes** maintenu devant le mage {effet validé}.
- **Brûlure** : les ennemis touchés brûlent pendant un moment {effet validé}. C'est un [statut](statuts.md) : son icône s'affiche au-dessus de l'ennemi, et chaque nouveau coup de feu relance sa durée.
- **Mana** {décidé} : jauge de 100. Elle remonte d'environ 1 par seconde, plus un bonus à chaque ennemi touché par la boule de feu. Les compétences coûtent du mana ; le cône de flammes en consomme tant qu'il est maintenu. Valeurs {à équilibrer}.
- **Pas d'ultime** {décidé} : le mage garde la boule de feu, le cône de flammes et la brûlure.
- **Compétences LB et RB** : vides pour l'instant {décidé}. Le mage joue avec la boule de feu (attaque principale) et le cône de flammes (attaque secondaire maintenue). Ses compétences : {à confirmer}.
- **Valeurs de départ** de la version 0.2 {à équilibrer} {{dev: (réglées dans `Assets/Jeu/Resources/GameBalance.asset`)}} :

| Sujet | Valeur |
|---|---|
| Vie | 100 |
| Boule de feu | 25 dégâts à l'impact, plus 15 en zone sur 2 m ; une toutes les 0,9 s |
| Mana | +4 par ennemi touché par la boule de feu |
| Cône de flammes | 22 dégâts par seconde, 14 mana par seconde |
| Brûlure | 5 dégâts par seconde pendant 3 s |

