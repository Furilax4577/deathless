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
| Succès | Trier la liste proposée le 01/10/2026 (garder, renommer, retirer), puis décider quand coder le module (local d'abord, Steam plus tard). | [Succès](succes.md) |
| Mage (banc en jeu, 01/10/2026) | Avec son nouveau kit, le Mage est au-dessus de la moyenne des classes partout : +14 à +38 % en vague (nuits 6 et 12), +16 à +30 % sur une cible (boule et brûlure en paliers). Le réduire avant ou après la 0.8.0, et par quoi (dégâts de la boule, paliers de brûlure, mana) ? Détail : `Docs/equilibrage-classes.md`, « Banc en jeu ». | [Mage](classe-mage.md) |

## Clés, crochetage et potions du village (03/10/2026, proposition à valider)

Demande de Quentin : savoir **qui vend les clés des pièces fermées, à quel prix chacune, et le kit de crochetage** ; et peut-être **des potions de santé, de mana et d'endurance chez le druide**. Repères d'économie : un sbire rapporte 5 or, un guerrier ou un voleur 8, un mage 10 ; à la taverne, une bière coûte 5 or, un repas 15, une tournée 30 ; tout se paie dans la **caisse commune**, l'hôte décide en multijoueur.

**Le mécano vend les clés et le kit** {à confirmer}. Une clé est **à usage unique**, on peut en porter **2 de chaque sorte au plus** ; elle ne se trouve jamais dans le donjon.

| Article | Prix | Ouvre | Contenu visé de la pièce |
|---|---|---|---|
| Clé de bronze | 40 or | les serrures de bronze | environ 100 or |
| Clé d'argent | 120 or | les serrures d'argent | environ 300 or |
| Clé d'or | 320 or | les serrures d'or | environ 800 or, et plus tard un objet rare |
| Kit de crochetage | 60 or, 5 crochets | serrures simples et de bronze (facile), d'argent (difficile) ; **jamais l'or** | selon la serrure |

Idée : la clé coûte à peu près 40 % de ce qu'elle rapporte, donc elle est rentable sans être gratuite ; le kit est moins cher à l'unité mais peut échouer.

**Crochetage** {à confirmer} : un petit mini-jeu à l'ouverture (une aiguille à garder dans une zone qui bouge, 3 essais, un crochet casse à chaque échec). **L'assassin est favorisé** : sa **perception** (ou son agilité, à choisir) agrandit la zone et ralentit l'aiguille, et il casse moins de crochets ; les autres classes y arrivent mais avec une zone étroite. Le kit se vend à tout le monde.

**Potions du druide** {à confirmer} (la potion de soin est déjà décidée, voir [Le village](village.md#potions-de-soin-décidé) ; santé et mana et endurance sont la nouveauté) :

| Potion | Prix | Effet |
|---|---|---|
| Santé | 20 or | rend 40 % des points de vie |
| Mana | 20 or | rend 50 % de la jauge de classe du mage (autres classes : à décider, une potion de jauge ou rien) |
| Endurance | 15 or | rend toute l'endurance, puis 10 s de récupération doublée |

On en porte **3 au maximum de chaque sorte**, on les boit à la **croix directionnelle** (haut santé, gauche mana, droite endurance) ou aux touches 1, 2, 3 au clavier. Elles s'achètent le jour, au druide, qui se tient devant sa maison.

**À trancher** : les prix et les effets ci-dessus sont des points de départ ; la potion de mana pour les classes sans mana ; la perception ou l'agilité pour le crochetage ; le contenu d'un coffre fermé.

## Visée au sol (02/10/2026)

Quentin a demandé que le mage voie où son sort va tomber (clic gauche confirme, clic droit annule, même mécanique que la nuée du rôdeur). Construit et décrit dans [Mage](classe-mage.md#règles), [Rôdeur](classe-rodeur.md#règles) et [Commandes](commandes.md#viser-une-zone-au-sol). Restent {à confirmer} : le ralenti à 50 % pendant la visée, les portées (20 m grande boule, 14 m mur, 25 m nuée), le mur posé en travers de la ligne mage → point, l'ennemi du « cœur » de la grande boule (1,5 m), LT (et non B) comme bouton d'annulation à la manette (B reste l'esquive, qui ferme aussi la visée), la visée aussi appliquée à la nuée du rôdeur.

## Équilibrage des classes (27/09/2026)

Audit chiffré des cinq classes et propositions en trois niveaux (lissage, retouches de kit, refontes) dans `Docs/equilibrage-classes.md` (tableau regénérable par `Docs/outils/equilibrage.py`). Les trois déséquilibres les plus nets : le coup dans le dos de l'Assassin (×3 permanent sur tout squelette qui frappe Nyxessa : 109 dégâts par seconde contre 32 à 44 pour les autres), le Mage sans kit (deux boutons vides, cône moins bon que la boule, aucun contrôle), et la zone du simple au quadruple (Viking 201, Rôdeur 55, Assassin 44) alors que la nuit 12 demande environ 64 dégâts par seconde par joueur. Quentin tranche classe par classe :

| Classe | Recommandation | À trancher |
|---|---|---|
| [Paladin](classe-paladin.md) | **Tranché le 27/09/2026** {décidé} : épée 30 → 27, soin toutes les 20 s, garde 0,8 endurance par dégât, et le soin devient un **soin d'aura** (le paladin et les alliés à 4 m). Règles dans [Paladin](classe-paladin.md#lissage-du-27092026). | — |
| [Viking](classe-viking.md) | **Tranché le 27/09/2026** {décidé} : plancher de rage à 30, saut 25 et rugissement 15 de rage, tournante 10 ; le rugissement pose Peau de fer (−35 % de dégâts subis, 6 s) et se joue sur le haut du corps. Règles dans [Viking](classe-viking.md#lissage-du-27092026). | — |
| [Mage](classe-mage.md) | **Tranché le 01/10/2026** {décidé} : refonte (c) adoptée, avec la **Grande boule de feu** (LB) à la place du Brasier et le **Mur de flammes** (RB) à la place de la Déflagration ; cône 30 DPS à 10 mana/s qui ralentit de 40 %, mana 3/s. Règles dans [Mage](classe-mage.md#règles). | Chiffres à équilibrer en jeu. |
| [Rôdeur](classe-rodeur.md) | **Tranché le 27/09/2026** {décidé} : arc 50 à pleine charge (cadence du tir rapide 0,15 s), nuée 14 par salve, salve 18, 120 PV ; la flèche chargée à fond étourdit 1 s (toute flèche à pleine charge, tête comprise), la nuée ralentit. Règles dans [Rôdeur](classe-rodeur.md#lissage-du-27092026). | — |
| [Assassin](classe-assassin.md) | **Tranché le 27/09/2026** {décidé} (« bonne idée ; être invisible sans bouger reste cool pour ouvrir un angle d'attaque » : la furtivité est gardée telle quelle). Règles dans [Assassin](classe-assassin.md#pas-de-lombre-et-exécution). Retour de Quentin : « ce n'est pas le sujet, il est trop lent pour attraper un ennemi dans le dos ; il faudrait presque qu'il puisse dash / exécuter ». Kit retenu : le dos ×3 reste (c'est la récompense), on lui donne le moyen d'y arriver. **Pas de l'ombre** (RB, aujourd'hui vide) : bond de 7 m en 0,15 s dans la direction visée, invulnérable pendant le bond, traverse les ennemis ; si le réticule est sur un ennemi, le bond s'arrête **1 m derrière lui, face à son dos** ; recharge 6 s. **Exécution** (passif de la dague) : un coup de dague sur un ennemi sous **30 % de vie** l'achève net (sbire, guerrier, voleur, mage) ; sur un élite ou un boss, coup ×3 sans achever ; chaque exécution **recharge le Pas de l'ombre**, ce qui enchaîne bond → dos → exécution → bond. Chiffres restants : dague 22, arbalète 60 / 5 s, grenade 15 s. Coût : moyen (ClasseAssassin, GameBalance, un déclencheur d'animation de bond réutilisant la roulade, effet Ombre existant ; réseau : le bond passe par le propriétaire comme l'esquive, l'exécution par le chemin des coups). | Seuil d'exécution (25 / 30 / 35 %) ; le bond traverse-t-il les murs du donjon (non recommandé) ; recharge 6 ou 8 s. |
| [Ennemis](ennemis.md) | **Tranché le 27/09/2026** {décidé} : un squelette au contact de Nyxessa se retourne vers un héros qui l'a frappé deux fois de suite à moins de 3 m, puis revient à Nyxessa (règle dans [Ennemis](ennemis.md#comportement-et-détection)). | À juger en jeu. |
