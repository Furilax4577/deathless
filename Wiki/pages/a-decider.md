# À décider

Ce qui reste ouvert. Une ligne quitte cette page quand la règle est tranchée et écrite dans la page concernée.

| Sujet | Question | Page |
|---|---|---|
| Nyxessa | Équilibrage des valeurs en jeu | [Nyxessa](nyxessa.md) |
| Classes | Valeurs chiffrées, autres styles du mage et façon d'en changer | [Classes](classes.md) |
| Nouvelles classes | Kits proposés du Barde, de la Bavaroise et du Clochard | [Classes](classes.md) |
| Ennemis | Équilibrage de la vie, de la vitesse, des dégâts et de l'or rapporté, y compris des deux versions du mini-boss (massue, martache) | [Ennemis](ennemis.md) |
| Univers | Origine de Nyxessa, comment elle a échappé au Nécromancien, rôle du donjon, éclat du sorcier pendant la canalisation | [L'univers](univers.md) |
| Statuts | Durées et intensités des statuts ; seuil, dégâts et ralenti de la chute | [Statuts](statuts.md) |
| Progression | Premier arbre d'améliorations des compétences (proposition), autres améliorations, économie | [Classes](classes.md#menu-du-personnage-et-points-de-competence) |
| Village | Maisons la nuit : n'y entrer que le jour ? Reconduire les héros dehors au crépuscule ? | [Le village](village.md) |

## Équilibrage des classes (27/09/2026)

Audit chiffré des cinq classes et propositions en trois niveaux (lissage, retouches de kit, refontes) dans `Docs/equilibrage-classes.md` (tableau regénérable par `Docs/outils/equilibrage.py`). Les trois déséquilibres les plus nets : le coup dans le dos de l'Assassin (×3 permanent sur tout squelette qui frappe Nyxessa : 109 dégâts par seconde contre 32 à 44 pour les autres), le Mage sans kit (deux boutons vides, cône moins bon que la boule, aucun contrôle), et la zone du simple au quadruple (Viking 201, Rôdeur 55, Assassin 44) alors que la nuit 12 demande environ 64 dégâts par seconde par joueur. Quentin tranche classe par classe :

| Classe | Recommandation | À trancher |
|---|---|---|
| [Paladin](classe-paladin.md) | Le garder dans son rôle, −10 % à l'épée, soin toutes les 20 s, et le soin devient un soin d'aura (alliés à 4 m) pour en faire aussi le soutien du groupe. | Soin d'aura : oui / non (touche au réseau). |
| [Viking](classe-viking.md) | Plancher de rage à 30 (plus de début de vague à zéro), saut 25 et rugissement 15 de rage, tournante 10 ; le rugissement protège (Peau de fer −35 % 6 s) et se joue sur le haut du corps. | Peau de fer : oui / non ; plancher ou rage de départ. |
| [Mage](classe-mage.md) | Refondre le kit autour du contrôle de zone : Brasier au sol (LB), Déflagration qui repousse (RB), cône 30 DPS à 10 mana/s qui ralentit, mana 3/s. | Refonte complète, ou seulement chiffres + feu qui ralentit. |
| [Rôdeur](classe-rodeur.md) | Arc 50 à pleine charge (cadence 0,15), nuée 70, salve 18, 120 PV ; la flèche chargée à fond étourdit 1 s, la nuée ralentit. | Étourdir sur toute flèche chargée, ou seulement à la tête. |
| [Assassin](classe-assassin.md) | Retour de Quentin (27/09/2026) : « ce n'est pas le sujet, il est trop lent pour attraper un ennemi dans le dos ; il faudrait presque qu'il puisse dash / exécuter ». Proposition {à confirmer} : le dos ×3 reste (c'est la récompense), on lui donne le moyen d'y arriver. **Pas de l'ombre** (RB, aujourd'hui vide) : bond de 7 m en 0,15 s dans la direction visée, invulnérable pendant le bond, traverse les ennemis ; si le réticule est sur un ennemi, le bond s'arrête **1 m derrière lui, face à son dos** ; recharge 6 s. **Exécution** (passif de la dague) : un coup de dague sur un ennemi sous **30 % de vie** l'achève net (sbire, guerrier, voleur, mage) ; sur un élite ou un boss, coup ×3 sans achever ; chaque exécution **recharge le Pas de l'ombre**, ce qui enchaîne bond → dos → exécution → bond. Chiffres restants : dague 22, arbalète 60 / 5 s, grenade 15 s. Coût : moyen (ClasseAssassin, GameBalance, un déclencheur d'animation de bond réutilisant la roulade, effet Ombre existant ; réseau : le bond passe par le propriétaire comme l'esquive, l'exécution par le chemin des coups). | Seuil d'exécution (25 / 30 / 35 %) ; le bond traverse-t-il les murs du donjon (non recommandé) ; recharge 6 ou 8 s. |
| [Ennemis](ennemis.md) | Un squelette au contact de Nyxessa se retourne vers un héros qui l'a frappé deux fois de suite à moins de 3 m (aujourd'hui il ne se retourne jamais). | À juger en jeu après les deux premiers réglages. |
