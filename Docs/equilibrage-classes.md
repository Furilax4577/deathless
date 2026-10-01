# Équilibrage des classes (audit du 27/09/2026)

Demande de Quentin : « Tu ferais pas un lissage global des compétences ? Quitte à proposer des refontes. Les cinq personnages sont déséquilibrés. »

Document de **conception et d'analyse** : rien n'a été changé dans le code ni dans `GameBalance`. Tous les chiffres viennent de `Assets/Scripts/Jeu/GameBalance.cs` (valeurs par défaut des champs) et du code des classes (`Assets/Scripts/Jeu/Classes/*.cs`) ; les tableaux du § 1 sont **générés** par `Docs/outils/equilibrage.py` (Python, bibliothèque standard) et se rejouent après chaque changement de chiffres :

```
python Docs/outils/equilibrage.py          # tableaux Markdown
python Docs/outils/equilibrage.py --json   # les mêmes chiffres, pour comparer deux versions
```

Sources lues : `CLAUDE.md`, le wiki (`classes.md`, les cinq pages de classe, `statuts.md`, `ennemis.md`, `deroule.md`, `nyxessa.md`, `a-decider.md`), `GameBalance.cs`, `ClassePaladin/Viking/Mage/Rodeur/Assassin.cs`, `Heros.cs`, `ClasseHeros.cs`, `Combat.cs`, `ArbreCompetences.cs`, `Squelette.cs`, `DirecteurVagues.cs`, `Sante.cs`, `Docs/reseau.md` (coups, statuts, effets).

## Hypothèses du modèle

- **Cible mono** : un guerrier de la nuit 5 (160 PV, 14 dégâts, coup toutes les 2,2 s, préparation 0,8 s). **Groupe** : 5 sbires serrés (100 PV chacun, tous dans 2,5 m).
- **DPS soutenu** : cycle répété sans temps mort ; les compétences à recharge comptent au prorata (dégâts ÷ recharge). Le **pic 3 s** part jauge et recharges pleines, en comptant l'instant du coup dans le geste (`epeeInstant`, `hacheInstant`, `bouleInstant`, `dagueInstant`).
- Le tir à la tête (Rôdeur, arbalète) et le coup dans le dos (Assassin) dépendent du joueur et de la situation : ils sont donnés **à part**, pas dans la colonne principale.
- Les améliorations de l'arbre (3 rangs, +10 % à +30 %) ne sont pas comptées : elles s'ajoutent de la même façon à toutes les classes.
- Une nuit compte **90 s de combat utile** sur 120 s (20 s de marche depuis les clairières, répits entre vagues).
- Le script audite **le code** ; si `Assets/Jeu/Resources/GameBalance.asset` a été réglé à la main dans l'éditeur, les valeurs peuvent diverger (non vérifié : pas d'éditeur ouvert pour cet audit).

---

## 1. Modèle commun

### Tableau du modèle commun (généré par `Docs/outils/equilibrage.py`)

Cible mono : guerrier de la nuit 5 (160 PV, 14 dégâts par coup) ; groupe : 5 sbires serrés (100 PV chacun).

| Classe | PV | DPS mono soutenu (base seule) | DPS groupe (5 sbires) | Pic 3 s mono | Pic 3 s groupe | Guerrier N5 tué en | Contrôle | Survie |
|---|---|---|---|---|---|---|---|---|
| Paladin | 150 | **44,3** (40) | 124,3 | 150 | 330 | 4 s | étourdi 2,5 s (charge), 1 s (parade), 0,8 s + repousse (parfaite) | 150 PV ; garde (100 dégâts bloqués par jauge, +15/s) ; parade ; soin 37,5 PV / 30 s ; esquive |
| Viking | 140 | **40,2** (34,5) | 200,9 | 121 | 605 | 4,6 s | provocation 5 s à 10 m ; étourdi 1 s en zone (saut) | 140 PV ; aucune mitigation ; esquive ; saut = 5 m de fuite (8 s, 35 rage) |
| Mage | 100 | **32,8** (27,8) | 119,4 | 115 | 415 | 4,9 s | aucun (brûlure = dégâts seulement) | 100 PV ; portée 30 m ; aucune mitigation ; esquive |
| Rôdeur | 110 | **32,2** (24,2) | 54,5 | 120 | 365 | 6,6 s | aucun | 110 PV ; portée 60 m ; roulade arrière 4 m (+ esquive) ; plus rapide que tout squelette (5 contre 3,4 m/s) |
| Assassin | 100 | **43,9** (36,4) | 43,9 | 340 | 340 | 4,4 s | aucun ; fumée = les squelettes perdent leur cible (et repartent vers Nyxessa) | 100 PV ; aucune mitigation ; furtivité (pas ciblé hors combat) ; fumée = sortie de combat ; esquive |

Lignes à part (dépendent du joueur, pas des chiffres) : Rôdeur tête ×2 → 48,5 DPS soutenu ; Assassin dans le dos ×3 → **109,1 DPS soutenu**, ouverture furtif + dos 100 ; Viking attaque tournante 40 DPS par cible (plus que la hache en mono : 34,5) tant qu'il a de la rage.

### Compétences : coût, recharge, effet

**Paladin** — mobilité : 5 m/s ; charge 7 m / 14 s ; ×0,7 en garde ; dépendance : aucune.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Épée | aucun | 0,75 s entre deux coups | 30 dégâts, 3 cibles, 2,6 m, ±40° |
| Garde | 1 endurance par dégât bloqué | — | ±70° ; parade 0,25 s → étourdi 1 s ; parfaite 0,1 s → repousse 2 m, étourdi 0,8 s |
| Charge bélier | aucun | 14 s | 15 à 60 dégâts, 7 m, étourdi 2,5 s (cible) / 0,6 s (traversés) |
| Soin sur soi | aucun | 30 s | +25 % de la vie (37,5 PV), 1,25 PV/s en moyenne |

**Viking** — mobilité : 5 m/s ; saut 5 m / 8 s ; ×0,6 en tournante, ×0,25 pendant le coup ; dépendance : rage à 0 en début de vague : 4,8 s de hache sur une cible avant le saut, 3,4 s avant le rugissement.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Hache | aucun (+8 rage par cible) | 1,1 s entre deux coups | 38 dégâts, toutes les cibles, 2,4 m, ±70° |
| Attaque tournante | 20 rage/s (−6,7 par cible et par seconde) ; 15 rage pour lancer | — | 12 dégâts / 0,3 s = 40 DPS par cible, 360°, 2,3 m ; se paie à partir de 3 cibles |
| Rugissement | 25 rage | 12 s | provoque 5 s à 10 m ; immobile ~1,5 s |
| Saut percutant | 35 rage | 8 s | 45 dégâts, 5 m de bond, rayon 3,5 m, étourdi 1 s |

**Mage** — mobilité : 5 m/s ; ×0,6 en lançant, ×0,4 pendant le cône ; dépendance : aucune ; mais rien ne le protège au contact.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Boule de feu | aucun (+4 mana par cible touchée) | 0,9 s entre deux | 25 dégâts + 15 en zone (2 m) ; brûlure 5/s pendant 3 s ; portée 30 m |
| Cône de flammes | 14 mana/s (jauge 100 : 7,1 s au plus) ; régénération 1/s hors cône | — | 22 DPS par cible, 6 m, ±20°, vitesse ×0,4 ; brûlure |
| LB | — | — | vide |
| RB | — | — | vide |

**Rôdeur** — mobilité : 5 m/s ; roulade 4 m / 8 s ; ×0,5 en bandant ; dépendance : aucune ; sa valeur dépend de la précision (tête ×2).

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Arc (charge complète) | aucun | 1,2 s de charge + 0,45 s | 40 dégâts (24,2 DPS), tête ×2 (48,5 DPS) ; tir rapide 10 dégâts (20 DPS) |
| Nuée de flèches | aucun | 12 s | 5 salves × 10 = 50 dégâts par cible restée dans 3 m, sur 1,2 s ; portée 25 m ; immobile 1 s |
| Roulade + salve | 20 endurance | 8 s | 4 m en arrière, invulnérable 0,3 s ; 5 flèches × 15 sur ±20° (tête ×2) |
| Visée | aucun | — | zoom ; vitesse ×0,6 |

**Assassin** — mobilité : 5 m/s (3,2 furtif) ; ×0,4 pendant le coup, ×0,5 arbalète en main ; dépendance : forte : le dos ×3 exige un ennemi occupé ailleurs (Nyxessa, tank).

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Dague | aucun | 0,55 s entre deux coups | 20 dégâts, 1 cible, 1,8 m ; furtif ×2 (1 coup), dos ×3 (109,1 DPS), les deux ×5 (100) |
| Arbalète | aucun | 6 s | 45 dégâts (tête ×2 = 90), 60 m/s, portée 40 m ; vitesse ×0,5 en main |
| Grenade fumigène | aucun | 20 s | nuage 5 s, portée 8 m : personne n'est vu dedans (alliés compris) ; l'assassin y redevient furtif |
| Furtif (passif) | — | 4 s hors combat | marche à 3,2 m/s ; repéré à 6 m devant (±60°) ou 1,5 m derrière |

### Ce que demandent les nuits (PV ennemis à abattre, 90 s de combat utile par nuit)

| Nuit | Ennemis (solo) | PV ennemis solo | DPS requis solo | PV ennemis à 4 | DPS requis par joueur à 4 |
|---|---|---|---|---|---|
| 5 | 25 | 3570 | 39,7 | 9996 | 27,8 |
| 8 | 38 | 5580 | 62 | 15624 | 43,4 |
| 10 | 44 | 6996 | 77,7 | 19589 | 54,4 |
| 12 | 48 | 8256 | 91,7 | 23117 | 64,2 |

Morgrim : 1500 PV (étourdissements ×0,5, non repoussable) ; Nyxar : 1200 PV, reste à 12–18 m. Nyxessa au palier 5 ajoute environ 34,7 DPS sur une nuit (4 160 dégâts / 120 s).

Temps pour abattre Morgrim seul, DPS mono soutenu : Paladin 34 s ; Viking 37 s ; Mage 46 s ; Rôdeur 47 s ; Assassin 34 s ; Assassin dans le dos 14 s ; Rôdeur à la tête 31 s.

### Lecture du tableau

- Le DPS mono soutenu est **serré** (32 à 44) : ce n'est pas là que le déséquilibre se voit. Il se voit sur trois autres colonnes : le **DPS de groupe** (Viking 201, Paladin 124, Mage 119, Rôdeur 55, Assassin 44 : du simple au quadruple, dans un jeu de hordes), le **contrôle** (Paladin et Viking en ont, les trois autres aucun) et le **dos de l'Assassin** (109 DPS, deux fois et demie les autres).
- Le combat de la **nuit 12 demande environ 64 DPS par joueur à quatre**, avec des vagues de plus de 130 squelettes (plafond 60 sur le terrain, PV reportés). Aucune classe n'y arrive en mono-cible : c'est la zone qui gagne les nuits tardives, et deux classes n'en ont presque pas.
- **Deux règles des squelettes pèsent sur l'équilibre** (code de `Squelette.cs`, pas des classes) : un squelette **au contact de Nyxessa ne se retourne jamais** vers celui qui le frappe (`OnTouche` exige `DistanceNyxessa() > RayonContact`), et un poursuivant **abandonne après 4 s** sans pouvoir frapper (`abandonApres`). La première offre le dos de tous les squelettes autour de Nyxessa à l'Assassin (×3 permanent) et rend la garde du Paladin inutile là où il se bat le plus ; la seconde limite le kiting du Rôdeur, ce qui est sain.

---

## 2. Diagnostic

### Tableau comparatif

| | Paladin | Viking | Mage | Rôdeur | Assassin |
|---|---|---|---|---|---|
| Rôle affiché (wiki) | Tank, mêlée vers l'avant | Mêlée, zone | Distance, zone | Distance, précision | Furtif, coups critiques |
| Rôle réel en jeu | Le plus complet : tank, contrôle, mobilité, soin, 124 DPS de groupe | Roi de la zone, provocation ; sans rage au début de chaque vague | Attaque de base correcte, cône inutile, deux boutons vides | Le plus faible sur le corps ; sans contrôle ; roi de la survie à distance | Le plus fort sur une cible qui regarde ailleurs ; nul en zone ; furtivité inopérante dans une horde |
| Mêlée ordinaire (sbires, guerriers) | Fort | Très fort | Moyen | Faible | Moyen (fort sur Nyxessa) |
| À distance (mages lanceurs de crâne, Nyxar) | Charge 7 m puis épée ; garde bloque le crâne | Saut 5 m, sinon court | Fort (30 m) | Fort (60 m, tête) | Arbalète 45 / 6 s : faible ; furtif vers le mage : bien |
| Morgrim | Parade des coups parables, étourdi ×0,5 | 37 s ; Tourbillon le punit | 46 s, hors de portée du Tourbillon | 31 s à la tête, hors de portée | 14 s dans le dos, sous le Tourbillon |
| Donjon (6 gardiens à poste) | Bon | Bon | Bon | Kite | Le meilleur (furtif, dos, éclaireur) |
| Raison d'être choisi à 4 | Oui : tank et contrôle | Oui : zone et provocation | Non net : le Viking fait mieux la zone | Non net : dégâts trop faibles pour un « pur dégâts » | Oui pour l'exécution, mais fragile et sans zone |

### Par classe

**Paladin.** Trop complet plutôt que trop fort : 40 DPS mono (le meilleur des bases), 3 cibles par coup, la seule mitigation du jeu (garde, parade, parade parfaite), le meilleur contrôle (2,5 s d'étourdissement), un franchissement de 7 m et un soin. Sa lisibilité est bonne (« le rempart »). Ce qui manque : rien ne le tourne vers l'équipe (le soin est sur lui seul ; la catégorie de score « Soins prodigués » n'a que ça à compter), et sa garde ne sert à rien contre les squelettes qui frappent Nyxessa, puisqu'ils ne se retournent pas. Faiblesse chiffrée : 100 dégâts bloqués vident la jauge ; face à Morgrim (45 par coup) la garde tient deux coups.

**Viking.** Le meilleur DPS de groupe (201 à la hache, jusqu'à 200 en tournante), un contrôle utile à l'équipe (provocation 10 m) et un étourdissement de zone. Trois défauts nets : (1) **la rage part à 0** et retombe à 0 entre deux vagues (4 s de délai puis 6/s : vide en 20 s, et les vagues sont espacées de 40 s) : au début de chaque vague, ni saut ni rugissement pendant 3 à 5 s, précisément quand il faudrait attirer ; (2) **le rugissement est un suicide** : 1,5 s immobile, puis 5 s avec tout ce qui est à 10 m sur lui, avec 140 PV et aucune mitigation (un groupe de 5 sbires + 2 guerriers fait 8 × 5 / 1,8 + 14 × 2 / 2,2 ≈ 35 DPS sur lui : mort en 4 s sans esquive) ; (3) **la tournante bat la hache en mono-cible** (40 contre 34,5), ce qui brouille sa lecture « la tournante, c'est pour la foule ».

**Mage.** Le kit n'est pas fini (LB et RB vides, `{décidé}` « pour l'instant », compétences `{à confirmer}`), et ce qui existe se contredit : le **cône est partout moins bon que la boule** (22 DPS contre 27,8 + zone, ralentit à ×0,4, coûte 14 mana/s alors que la régénération est de 1/s : 7 s de cône puis 100 s d'attente ; la boule, elle, est gratuite). Aucun contrôle : la brûlure n'est que des dégâts. Sa zone (119) est en dessous de celle du Viking et du Paladin. Il n'a donc aucune raison d'être choisi à quatre par rapport au Rôdeur (même portée, meilleure survie) sinon la boule sur les groupes. Rôle à donner : **contrôle de zone à distance** (tenir une place autour de Nyxessa), ce que personne ne fait aujourd'hui.

**Rôdeur.** Le plus faible sur le corps (24 DPS à pleine charge ; 20 en tir rapide : la charge rapporte à peine), 55 de groupe, aucun contrôle, une nuée de 50 dégâts par cible toutes les 12 s. Sa valeur est dans la **tête** (48,5) qui dépend de la précision du joueur (à la manette, sur des squelettes qui marchent), et dans sa **survie** (roulade, vitesse, 60 m) qui ne rapporte rien à l'équipe. Un « pur dégâts » qui fait moins que le tank n'a pas de rôle. Rôle à donner : **il choisit sa cible et la cloue** (contrôle mono à distance : l'élite, le mage lanceur de crâne, le squelette qui arrive sur Nyxessa) et sa nuée couvre Nyxessa de loin.

**Assassin.** Deux extrêmes : 109 DPS soutenus dans le dos (deux fois et demie les autres, sans jauge ni recharge) grâce à la règle « un squelette sur Nyxessa ne se retourne pas », et rien en zone (une cible par coup). La furtivité est belle au donjon et pour aborder un mage, mais dans une vague de 40 squelettes il est repéré tout le temps (cône de 6 m devant chacun) et n'a alors ni mitigation ni fuite hormis la grenade (20 s). L'arbalète (45 / 6 s) ne pèse pas. La grenade est en réalité un **outil d'équipe** (personne n'est vu dedans, alliés compris ; `Squelette.Voit`) : ce n'est écrit nulle part dans le wiki. Rôle à garder mais à cadrer : **l'exécuteur** des cibles qui regardent ailleurs (élites, mages, boss) et l'éclaireur du donjon, sans être le premier DPS du jeu sur les sbires ordinaires.

### Les trois déséquilibres les plus nets

1. **Le dos de l'Assassin** : ×3 permanent sur tout ce qui frappe Nyxessa, soit 109 DPS contre 32 à 44 pour les autres, gratuit, et Morgrim en 14 s.
2. **Le Mage n'a pas de kit** : deux boutons vides, un cône moins bon que l'attaque de base, une jauge qui se remplit en 100 s, aucun contrôle ; il n'apporte rien que le Viking ou le Rôdeur n'apportent mieux.
3. **La zone va du simple au quadruple** (Viking 201, Rôdeur 55, Assassin 44) dans un jeu où la nuit 12 demande 64 DPS par joueur : deux classes sur cinq ne tiennent pas leur part des nuits tardives, et le Rôdeur cumule cela avec le plus faible mono-cible.

---

## 3. Propositions

Trois niveaux : **(a) lissage** (chiffres seulement, `GameBalance` + asset + tableau du wiki), **(b) retouches de kit** (une compétence change ou reçoit un effet), **(c) refonte** (rôle en une phrase, quatre actions). Pour chaque proposition : avant → après dans le modèle, risque, coût (petit / moyen / gros, fichiers), réseau.

Repères réseau (`Docs/reseau.md`) : un changement de chiffre ne touche pas au réseau ; **Étourdi, Ralenti, Provoqué, Brûlure sur un squelette** passent déjà par l'hôte (`EtourdirRpc`, `ProvoquerRpc`, `Statuts.Ajouter` en relais, validé par `StatutsReseau.Valider` : y ajouter un type est une ligne) ; un **statut sur son propre héros** passe par `HerosReseau.StatutRpc` (existe) ; **soigner ou protéger un autre héros** n'existe pas (nouveau RPC hôte → propriétaire, sur le modèle de `EncaisserRpc`) ; toute nouvelle compétence ajoute un numéro d'effet dans `EffetDistant` (visuel et son chez les autres, petit) ; les dégâts d'une zone au sol tenue par un client sont un message par coup, comme le cône (accepté « sans gêne constatée à deux »).

### 3.1 Paladin — garder, recentrer sur l'équipe

**(a) Lissage** — `epeeDegats` 30 → **27**, `soinRecharge` 30 → **20**, `gardeCoutParDegat` 1 → **0,8**.

| | Avant | Après |
|---|---|---|
| DPS mono / groupe | 40 (44,3) / 124 | 36 (40,3) / 112 |
| Soin | 37,5 PV / 30 s = 1,25 PV/s | 37,5 PV / 20 s = 1,9 PV/s |
| Dégâts bloqués par jauge pleine | 100 | 125 (Morgrim : 2 coups → presque 3) |

Il reste le meilleur mono-cible de mêlée, mais moins loin devant le Viking (34,5), et il encaisse plus. Risque : faible. Coût : petit (`GameBalance.cs`, `GameBalance.asset`, `classe-paladin.md`). Réseau : non.

**(b) Retouche : le soin devient un soin d'aura.** RB soigne le paladin **et les alliés à 4 m** de 25 % de leur vie, recharge 25 s. Il devient le seul soutien du jeu (la catégorie « Soins prodigués » du score prend un sens), ce qui lui donne une raison d'être au milieu du groupe et non devant. Modèle : inchangé pour lui ; +37,5 PV par allié à portée toutes les 25 s (un Mage de 100 PV : +25). Risque : le groupe se serre autour de lui, ce que la Massue de Morgrim punit (voulu). Coût : **moyen** (`ClassePaladin.Soigner` + `Maj` : parcours de `Partie.TousLesHeros` ; `HerosReseau` : nouveau RPC hôte → propriétaire « Soigner », ou demande client → hôte → propriétaire ; `AuraSoin` déjà là ; wiki). Réseau : **oui**, nouveau message (petit, un par soin).

**(c) Refonte** : inutile, le rôle est net.

### 3.2 Viking — garder, débloquer le début de vague

**(a) Lissage** — nouveau champ **`ragePlancher` = 30** (la baisse hors combat s'arrête à 30 ; la rage de départ est 30), `sautRage` 35 → **25**, `rugissementRage` 25 → **15**, `tournanteDegats` 12 → **10**.

| | Avant | Après |
|---|---|---|
| Début de vague | 0 rage : saut après 4,8 s de hache, rugissement après 3,4 s | 30 rage : rugissement **tout de suite**, saut tout de suite |
| Tournante mono / groupe | 40 / 200 DPS | 33 / 167 (sous la hache en mono : 34,5 ; au-dessus en groupe : 173) |
| Enchaînement à 100 rage | rugissement + saut = 60 rage, reste 40 (2 s de tournante) | 40 rage, reste 60 (3 s de tournante à 5 cibles, qui se paie) |

Le plancher est un ajout de deux lignes dans `ClasseViking.Temps` (`Mathf.Max(B.ragePlancher, …)`) et `Initialiser`. Risque : faible ; le plancher rend la jauge moins « nerveuse », c'est voulu (la rage récompense encore de frapper : 30 → 100). Coût : petit (`GameBalance.cs`, asset, `ClasseViking.cs`, `classe-viking.md`). Réseau : non (jauge locale au propriétaire).

**(b) Retouche : le rugissement protège celui qui rugit.** En plus de provoquer, il pose sur le viking le statut **Peau de fer** : −35 % de dégâts subis pendant 6 s (le temps de la provocation, plus une seconde). Il devient un vrai tank de provocation : 140 PV × 1/0,65 ≈ 215 PV effectifs pendant 6 s, contre 35 DPS de la meute décrite plus haut : 6 s tenues au lieu de 4. Et le cri se joue **sur le haut du corps** (il peut marcher pendant le cri au lieu d'être cloué 1,5 s). Modèle : survie « aucune mitigation » → « −35 % 6 s / 12 s ». Risque : aucun sur les dégâts infligés. Coût : **moyen** (`CatalogueStatuts` + `TypeStatut.PeauDeFer` ; `Heros` : `Sante.absorbeur` existe déjà pour les coups ennemis, il lit le statut ; `ClasseViking.Rugir` ; contrôleur `Viking_Jeu` : couche haute pour `Skeletons_Taunt_Longer` ; icône `statut_peau_de_fer` ; wiki `statuts.md`, `classe-viking.md`). Réseau : **oui, chemin existant** (`HerosReseau.StatutRpc`, statut sur son propre héros, prédit puis confirmé ; un nouveau type dans `StatutReseau`).

**(c) Refonte** : inutile.

### 3.3 Mage — refonte du kit (recommandée)

Le wiki laisse LB et RB « vides pour l'instant » et ses compétences `{à confirmer}` : c'est ouvert.

**(a) Lissage seul** (si Quentin garde deux boutons) — `manaRegen` 1 → **3**, `coneMana` 14 → **10**, `coneDegats` 22 → **30**, `bouleDegatsZone` 15 → **18**.

| | Avant | Après |
|---|---|---|
| Cône : durée à jauge pleine / jauge pleine en | 7,1 s / 100 s | 10 s / 33 s |
| Cône mono (contre boule 27,8 + 5) | 22 + 5 | 30 + 5 : le cône vaut enfin la boule de près |
| DPS groupe (boule) | 119 | 133 |

Risque : faible. Coût : petit. Réseau : non. Mais il reste sans contrôle et sans raison d'être choisi.

**(b) Retouche : le feu ralentit.** Les ennemis dans le cône reçoivent **Ralenti −40 %** (renouvelé à chaque tic, 1 s), la brûlure ralentit de 20 % tant qu'elle brûle. Contrôle de zone à distance sans nouvelle compétence : le cône devient un « mur » devant Nyxessa. Modèle : contrôle « aucun » → « ralenti 40 % dans le cône, 20 % sous brûlure ». Coût : **petit** (`ClasseMage.Maj` : `Statuts.Ajouter(TypeStatut.Ralenti, …)` sur les cibles du cône ; `CatalogueStatuts` : la brûlure porte aussi un facteur de vitesse, ou deux statuts posés ensemble ; `StatutsReseau.Valider` : accepter Ralenti venant d'un client ; wiki `statuts.md`, `classe-mage.md`). Réseau : **oui, chemin existant** (relais de `Statuts.Ajouter`, au plus une demande par 0,4 s et par type).

**(c) Refonte : « le Mage tient une zone : il pose le feu au sol et repousse ce qui l'approche. »**

| Touche | Action | Chiffres proposés | Ce que ça apporte |
|---|---|---|---|
| RT | Boule de feu | inchangée (25 + 15 zone 2 m, brûlure) ; gratuite | attaque de base, groupe |
| LT | Cône de flammes | 30 DPS, 10 mana/s, 6 m, ±20°, **Ralenti −40 %** | mur de près, dégâts de zone courte |
| LB | **Brasier** | zone au sol de 3 m posée jusqu'à 25 m (comme la nuée), 6 s, **8 DPS + brûlure + Ralenti −40 %** ; 30 mana, recharge 12 s | tenir une place autour de Nyxessa, ralentir une vague au pont |
| RB | **Déflagration** | onde à 4 m autour du mage : 35 dégâts, **repousse 3 m, étourdi 0,5 s** ; 25 mana, recharge 15 s | survie au contact, dégager le sorcier |
| Jauge | Mana | 100 ; **régénération 3/s** ; +4 par cible touchée par la boule | une compétence toutes les 10 s environ |

Modèle après : DPS mono 27,8 + 5 + 4 (brasier au prorata) ≈ **37** ; groupe 133 + 20 (brasier) + 12 (déflagration) ≈ **165** (au niveau du Paladin, sous le Viking) ; contrôle : ralenti de zone, repousse ; survie : une sortie de contact toutes les 15 s. Coût de l'ensemble : **gros** (`GameBalance.cs` + asset : ~12 champs ; `ClasseMage.cs` : deux actions, `Emplacement` 2 et 3 ; `ClassesJeu.asset` : emplacements du HUD ; deux icônes `mage_brasier`, `mage_deflagration` dans `ArtSources/Icones/` ; effets : le brasier peut reprendre `BurnFlammeches` au sol, la déflagration `ExplosionFeu` (existants, `Docs/vfx.md`) ; sons : deux ids ; `ArbreCompetences` : 4 améliorations au lieu de 3 ; wiki `classe-mage.md`, `statuts.md`, `commandes.md`). Réseau : **oui** (deux numéros d'effet pour le visuel ; brasier : coups en relais comme le cône, ou Ralenti posé par l'hôte ; déflagration : `Repousser` et `Etourdir` en relais existants). Risque : le brasier + le cône font beaucoup de messages à quatre (tic du brasier à 0,5 s pour limiter).

### 3.4 Rôdeur — chiffres, puis un contrôle à distance

**(a) Lissage** — `arcDegatsMax` 40 → **50**, `arcIntervalle` 0,3 → **0,15**, `nueeDegatsSalve` 10 → **14**, `salveDegats` 15 → **18**, `rodeurPV` 110 → **120**.

| | Avant | Après |
|---|---|---|
| Arc à pleine charge | 40 / 1,65 s = 24,2 DPS (tête 48,5) | 50 / 1,5 s = **33,3** (tête 66,7) |
| Tir rapide | 20 DPS | 22 DPS (la charge rapporte enfin : ×1,5) |
| Nuée | 50 par cible / 12 s (groupe 21/s) | 70 par cible (groupe 29/s) |
| DPS mono / groupe | 32 / 55 | **44 / 75** |

Risque : faible ; à la tête il passe premier mono-cible (67), ce qui est le contrat de la classe « précision ». Coût : petit. Réseau : non.

**(b) Retouche : la flèche chargée cloue, la nuée ralentit.** Une flèche **chargée à fond** (100 %) qui touche **étourdit 1 s** (0,5 s sur Morgrim, règle existante) : il choisit sa cible et l'arrête, à 60 m ; ça fait aussi de la charge complète un choix (dégâts + contrôle) contre le tir rapide (cadence). La **nuée pose Ralenti −40 % 3 s** sur la zone : une zone de contrôle sur Nyxessa depuis 25 m. Modèle : contrôle « aucun » → « étourdi 1 s toutes les 1,5 s sur une cible ; ralenti de zone / 12 s ». Risque : un rôdeur précis peut enchaîner les étourdissements sur un élite (1 s toutes les 1,5 s : l'élite avance encore 33 % du temps) ; si c'est trop, étourdir seulement à la tête. Coût : **petit** (`ClasseRodeur.TirerFleche` : `Etourdir` si `charge >= 0,999` ; `Pluie` : `Statuts.Ajouter(Ralenti)` ; `StatutsReseau.Valider` ; wiki). Réseau : **oui, chemins existants** (`EtourdirRpc`, relais de statut).

**(c) Refonte** : inutile si (a) + (b).

### 3.5 Assassin — cadrer le dos, donner un rôle d'exécuteur

Le wiki dit `{décidé}` « pas d'autre compétence active » : (a) et (b) respectent cette décision ; (c) demande de la rouvrir.

**(a) Lissage** — `critiqueDos` 3 → **2**, `critiqueFurtifDos` 5 → **4**, `dagueDegats` 20 → **22**, `arbaleteDegats` 45 → **60**, `arbaleteRecharge` 6 → **5**, `grenadeRecharge` 20 → **15**, `assassinPV` 100 → **110**.

| | Avant | Après |
|---|---|---|
| Dague de face / dans le dos | 36,4 / 109 DPS | 40 / **80** DPS (encore le premier mono du jeu, plus ×2,5) |
| Ouverture furtif + dos | 100 | 88 |
| Arbalète (corps / tête) | 45 / 90 par 6 s = 7,5 DPS | 60 / 120 par 5 s = 12 DPS : un vrai outil contre un mage lanceur de crâne ou Nyxar |
| Morgrim dans le dos | 14 s | 19 s |

Risque : faible. Coût : petit. Réseau : non.

**(b) Retouche : l'exécuteur d'élites.** Le dos vaut **×2 sur un squelette ordinaire, ×3 sur un élite ou un boss** (`critiqueDosElite`), et la dague touche **2 cibles** dans son cône de 45° (au lieu d'une). Il garde son pic sur ce qui compte (élites, Morgrim) sans être le premier DPS sur les sbires, et gagne un peu de zone (80 en groupe au lieu de 44, dernier quand même). À écrire dans le wiki : la fumée cache **tous** les héros (déjà vrai en jeu). Coût : **petit** (`ClasseAssassin.PorterDague` : `sq.elite || sq is Golem || sq is Necromancien` ; boucle sur 2 cibles ; `GameBalance` ; wiki `classe-assassin.md`). Réseau : non.

**(c) Refonte « le Traqueur » (si Quentin rouvre le kit)** : RB = **Marque du traqueur** : lancer de dague à 20 m ; la cible marquée subit **+30 % de dégâts de tous** pendant 6 s, recharge 15 s. L'assassin désigne l'élite ou le boss à l'équipe (utilité d'équipe, lisible : « il marque, on tape »). Coût : **moyen** (nouveau `TypeStatut.Marque` ; multiplicateur des dégâts reçus côté squelette : `Sante` n'a qu'un `absorbeur` pour les coups ennemis, il faut un crochet symétrique pour les coups des héros ; icône ; effet gemmes Ombre ; wiki). Réseau : **oui, chemin existant** pour le statut (relais), nouveau crochet de dégâts côté hôte.

### 3.6 Hors classes : deux règles des squelettes à trancher

- **Un squelette sur Nyxessa ne se retourne jamais.** Proposition : il se retourne vers un héros qui l'a frappé **deux fois de suite à moins de 3 m** (guerriers et sbires ; pas Morgrim). Effet : la garde et la parade du Paladin servent autour de Nyxessa, l'Assassin doit relancer son approche, les squelettes peuvent être « décrochés » de la relique par la mêlée. Coût : petit (`Squelette.OnTouche`, un compteur). Réseau : non (IA chez l'hôte). Risque : les squelettes quittent Nyxessa plus souvent, ce qui allège la pression sur elle : à jauger en jeu.
- **Étourdissement de Morgrim ×0,5, non repoussable** : cohérent ; à garder.

---

## 4. Ordre conseillé et recommandation

Ordre : ce qui coûte peu et corrige le plus d'abord, la refonte en dernier.

1. **Assassin (a)** : dos ×2, furtif + dos ×4, dague 22, arbalète 60 / 5 s, grenade 15 s — un quart d'heure de réglage, et le premier déséquilibre disparaît.
2. **Rôdeur (a) + (b)** : arc 50, intervalle 0,15, nuée 14 par salve, salve 18, 120 PV ; flèche chargée qui étourdit 1 s, nuée qui ralentit — il passe de dernier à « précision qui contrôle ».
3. **Viking (a) + (b)** : plancher de rage 30, saut 25, rugissement 15, tournante 10 ; Peau de fer sur le rugissement, cri sur le haut du corps — la classe fonctionne dès le début de chaque vague et le rugissement cesse d'être un suicide.
4. **Mage (c)** : le kit complet (Brasier, Déflagration, mana 3/s, cône 30 DPS à 10 mana/s qui ralentit). C'est le gros morceau ; en attendant, (a) + (b) (chiffres et feu qui ralentit) se font en une heure et donnent déjà un rôle.
5. **Paladin (a) puis (b)** : épée 27, soin 20 s, garde 0,8 ; puis le soin d'aura, qui est le seul point à toucher au réseau de façon neuve.
6. **Squelettes** : la règle « se retourne après deux coups au contact » se juge en jeu après 1 et 5, pas avant.

Chaque étape se vérifie en rejouant `python Docs/outils/equilibrage.py` (le tableau du § 1 doit bouger comme les colonnes « après » ci-dessus) puis en Play avec `ScenariosClasses` (une nuit accélérée par classe).

### Ce que je recommande, classe par classe

| Classe | Recommandation (une phrase) | Alternatives |
|---|---|---|
| **Paladin** | Le garder tel quel dans son rôle, lui retirer 10 % d'épée et lui donner le soin d'aura pour qu'il soit aussi le soutien du groupe. | Ne rien toucher tant que les quatre autres ne sont pas réglés ; ou seulement (a). |
| **Viking** | Plancher de rage à 30 et compétences moins chères, rugissement qui protège (Peau de fer) et ne l'immobilise plus : un tank de provocation qui marche dès la première seconde. | (a) seul ; ou rage de départ 50 sans plancher ; ou le rugissement réduit les dégâts des provoqués plutôt que les siens (même coût). |
| **Mage** | Refondre le kit autour du contrôle de zone : Brasier au sol (LB), Déflagration (RB), cône qui ralentit, mana 3/s. | (a) + (b) seuls si le temps manque ; ou un seul bouton (Brasier) et RB vide encore un temps. |
| **Rôdeur** | Monter l'arc (50 à pleine charge, cadence 0,15) et la nuée (70), et donner du contrôle : la flèche chargée étourdit 1 s, la nuée ralentit. | Étourdir seulement à la tête ; ou marque de chasse (+20 % dégâts pour tous) à la place de l'étourdissement (coût moyen). |
| **Assassin** | Ramener le dos à ×2 (×3 réservé aux élites et aux boss), arbalète 60 / 5 s, grenade 15 s, dague sur 2 cibles : l'exécuteur des cibles qui comptent, plus le premier DPS sur les sbires. | Garder ×3 mais faire retourner les squelettes frappés au contact de Nyxessa (§ 3.6) ; ou la Marque du traqueur si le kit est rouvert. |

Une fois tranché, chaque décision va dans la page de sa classe avec l'étiquette `{décidé}` ou `{à équilibrer}`, les chiffres dans `GameBalance.cs` et l'asset, et le tableau du § 1 est regénéré.

---

## Après le lissage du 27/09/2026

Décisions de Quentin reportées dans `GameBalance.cs`, `Assets/Jeu/Resources/GameBalance.asset` et le code le 27/09/2026 (pages `classe-*.md`, `statuts.md`, `ennemis.md` du wiki, sections « Lissage du 27/09/2026 », « Pas de l'ombre et Exécution », « Riposte au contact de Nyxessa ») :

- **Assassin** : Pas de l'ombre (RB : bond de 7 m en 0,15 s, invulnérable, à travers les ennemis, arrêt 1 m derrière l'ennemi visé face à son dos, recharge 6 s, ne sort pas du furtif) et Exécution (dague sur un ennemi commun sous 30 % : achevé net ; élite ou boss : ×3 ; chaque exécution remet le bond à zéro). Chiffres de la dague, de l'arbalète et de la grenade inchangés (la proposition (a) n'a pas été retenue : le dos ×3 reste la récompense, le bond le moyen d'y arriver).
- **Rôdeur** : arc 50 à pleine charge (étourdit 1 s), tir rapide 0,15 s, nuée 14 par salve (ralentit −40 % qui y reste), salve 18, 120 PV.
- **Viking** : plancher de rage 30 (`rageMin` : rage de départ, et hors combat la jauge revient vers 30 dans les deux sens), saut 25, rugissement 15, tournante 10 ; le rugissement pose Peau de fer (−35 % 6 s) au moment du cri et se crie en marchant (couche haute).
- **Paladin** : épée 27, soin 20 s, garde 0,8 ; le soin devient un soin d'aura (lui et les alliés à moins de 4 m, même montant).
- **Squelettes** : riposte au contact de Nyxessa (deux coups de suite du même héros à moins de 3 m → il se retourne 3 s ou deux coups, puis revient).
- **Mage** : inchangé (sa refonte reste à décider, § 3.3).

Vérifié en Play le 27/09/2026 (`ScenariosClasses.Lancer("lissage_*")`, captures `Assets/Screenshots/lissage_*.png`) : bond de 6,9 m à 0,9 m dans le dos d'un guerrier, exécution à 24 % achevée net et bond rechargé ; flèche à pleine charge → Étourdi, nuée → Ralenti 40 % ; viking à 30 de rage au départ, cri en marchant (7,4 m parcourus), Peau de fer −35 % (20 dégâts → 13), rage revenue à 30 ; soin d'aura +38 sur le paladin et sur un allié à 2 m, 75 soins crédités au paladin ; sbire au contact de Nyxessa retourné après deux coups, revenu à Nyxessa 3 s plus tard.

Ce que le tableau ne montre pas : le contrôle du rôdeur et de l'assassin n'entre pas dans le DPS ; la riposte des squelettes coupe le ×3 « gratuit » de l'assassin autour de Nyxessa (il doit relancer son approche par le bond, 6 s), donc les 109 DPS de dos ne sont plus soutenus qu'en alternance : bond → deux ou trois coups dans le dos (0,55 s chacun) → exécution ou repli.

### Tableau du modèle commun (généré par `Docs/outils/equilibrage.py`)

Cible mono : guerrier de la nuit 5 (160 PV, 14 dégâts par coup) ; groupe : 5 sbires serrés (100 PV chacun).

| Classe | PV | DPS mono soutenu (base seule) | DPS groupe (5 sbires) | Pic 3 s mono | Pic 3 s groupe | Guerrier N5 tué en | Contrôle | Survie |
|---|---|---|---|---|---|---|---|---|
| Paladin | 150 | **40,3** (36) | 112,3 | 141 | 303 | 4,4 s | étourdi 2,5 s (charge), 1 s (parade), 0,8 s + repousse (parfaite) | 150 PV ; garde (125 dégâts bloqués par jauge, +15/s) ; parade ; soin 37,5 PV / 20 s (aura 4 m) ; esquive |
| Viking | 140 | **40,2** (34,5) | 200,9 | 121 | 605 | 4,6 s | provocation 5 s à 10 m ; étourdi 1 s en zone (saut) | 140 PV ; Peau de fer −35 % 6 s / 12 s (rugissement) ; esquive ; saut = 5 m de fuite (8 s, 25 rage) |
| Mage | 100 | **32,8** (27,8) | 119,4 | 115 | 415 | 4,9 s | aucun (brûlure = dégâts seulement) | 100 PV ; portée 30 m ; aucune mitigation ; esquive |
| Rôdeur | 120 | **43,7** (33,3) | 73,8 | 156 | 490 | 4,8 s | étourdi 1 s par flèche à pleine charge (toutes les 1,5 s) ; ralenti −40 % dans la nuée / 12 s | 120 PV ; portée 60 m ; roulade arrière 4 m (+ esquive) ; plus rapide que tout squelette (5 contre 3,4 m/s) |
| Assassin | 100 | **43,9** (36,4) | 43,9 | 340 | 340 | 4,4 s | aucun ; fumée = les squelettes perdent leur cible (et repartent vers Nyxessa) ; exécution = un blessé sous 30 % meurt net | 100 PV ; aucune mitigation ; furtivité (pas ciblé hors combat) ; fumée = sortie de combat ; bond invulnérable 0,15 s / 6 s ; esquive |

Lignes à part (dépendent du joueur, pas des chiffres) : Rôdeur tête ×2 → 66,7 DPS soutenu ; Assassin dans le dos ×3 → 109,1 DPS soutenu, ouverture furtif + dos 100 ; Viking attaque tournante 33,3 DPS par cible (plus que la hache en mono : 34,5) tant qu'il a de la rage.

### Compétences : coût, recharge, effet

**Paladin** — mobilité : 5 m/s ; charge 7 m / 14 s ; ×0,7 en garde ; dépendance : aucune.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Épée | aucun | 0,75 s entre deux coups | 27 dégâts, 3 cibles, 2,6 m, ±40° |
| Garde | 0,8 endurance par dégât bloqué | — | ±70° ; parade 0,25 s → étourdi 1 s ; parfaite 0,1 s → repousse 2 m, étourdi 0,8 s |
| Charge bélier | aucun | 14 s | 15 à 60 dégâts, 7 m, étourdi 2,5 s (cible) / 0,6 s (traversés) |
| Soin d'aura | aucun | 20 s | +25 % de la vie (37,5 PV), 1,88 PV/s en moyenne, pour lui et chaque allié à moins de 4 m |

**Viking** — mobilité : 5 m/s ; saut 5 m / 8 s ; ×0,6 en tournante, ×0,25 pendant le coup ; dépendance : rage à 30 (plancher) en début de vague : rugissement et saut tout de suite.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Hache | aucun (+8 rage par cible) | 1,1 s entre deux coups | 38 dégâts, toutes les cibles, 2,4 m, ±70° |
| Attaque tournante | 20 rage/s (−6,7 par cible et par seconde) ; 15 rage pour lancer | — | 10 dégâts / 0,3 s = 33,3 DPS par cible, 360°, 2,3 m ; se paie à partir de 3 cibles |
| Rugissement | 15 rage | 12 s | provoque 5 s à 10 m ; Peau de fer −35 % pendant 6 s ; crié en marchant |
| Saut percutant | 25 rage | 8 s | 45 dégâts, 5 m de bond, rayon 3,5 m, étourdi 1 s |

**Mage** — mobilité : 5 m/s ; ×0,6 en lançant, ×0,4 pendant le cône ; dépendance : aucune ; mais rien ne le protège au contact.

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Boule de feu | aucun (+4 mana par cible touchée) | 0,9 s entre deux | 25 dégâts + 15 en zone (2 m) ; brûlure 5/s pendant 3 s ; portée 30 m |
| Cône de flammes | 14 mana/s (jauge 100 : 7,1 s au plus) ; régénération 1/s hors cône | — | 22 DPS par cible, 6 m, ±20°, vitesse ×0,4 ; brûlure |
| LB | — | — | vide |
| RB | — | — | vide |

**Rôdeur** — mobilité : 5 m/s ; roulade 4 m / 8 s ; ×0,5 en bandant ; dépendance : aucune ; sa valeur dépend de la précision (tête ×2).

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Arc (charge complète) | aucun | 1,2 s de charge + 0,3 s | 50 dégâts (33,3 DPS), tête ×2 (66,7 DPS), étourdi 1 s ; tir rapide 10 dégâts (28,6 DPS) |
| Nuée de flèches | aucun | 12 s | 5 salves × 14 = 70 dégâts par cible restée dans 3 m, sur 1,2 s, ralenti −40 % tant qu'on y reste ; portée 25 m ; immobile 1 s |
| Roulade + salve | 20 endurance | 8 s | 4 m en arrière, invulnérable 0,3 s ; 5 flèches × 18 sur ±20° (tête ×2) |
| Visée | aucun | — | zoom ; vitesse ×0,6 |

**Assassin** — mobilité : 5 m/s (3,2 furtif) ; bond de 7 m / 6 s ; ×0,4 pendant le coup, ×0,5 arbalète en main ; dépendance : le dos ×3 exige un ennemi occupé ailleurs (Nyxessa, tank) ; le Pas de l'ombre l'y porte ; depuis le 27/09/2026 un squelette sur Nyxessa frappé 2 fois à moins de 3 m se retourne (riposte 3 s).

| Compétence | Coût | Recharge | Effet |
|---|---|---|---|
| Dague | aucun | 0,55 s entre deux coups | 20 dégâts, 1 cible, 1,8 m ; furtif ×2 (1 coup), dos ×3 (109,1 DPS), les deux ×5 (100) |
| Arbalète | aucun | 6 s | 45 dégâts (tête ×2 = 90), 60 m/s, portée 40 m ; vitesse ×0,5 en main |
| Grenade fumigène | aucun | 20 s | nuage 5 s, portée 8 m : personne n'est vu dedans (alliés compris) ; l'assassin y redevient furtif |
| Pas de l'ombre | aucun | 6 s (remise à zéro par une exécution) | bond de 7 m en 0,15 s vers la visée, invulnérable, à travers les ennemis ; arrêt 1 m derrière l'ennemi visé, face à son dos ; ne sort pas du furtif |
| Exécution (passif) | — | — | dague sur un ennemi commun sous 30 % de vie : achevé net ; élite ou boss : ×3 (le meilleur des facteurs, pas le produit) |
| Furtif (passif) | — | 4 s hors combat | marche à 3,2 m/s ; repéré à 6 m devant (±60°) ou 1,5 m derrière |

### Ce que demandent les nuits (PV ennemis à abattre, 90 s de combat utile par nuit)

| Nuit | Ennemis (solo) | PV ennemis solo | DPS requis solo | PV ennemis à 4 | DPS requis par joueur à 4 |
|---|---|---|---|---|---|
| 5 | 25 | 3570 | 39,7 | 9996 | 27,8 |
| 8 | 38 | 5580 | 62 | 15624 | 43,4 |
| 10 | 44 | 6996 | 77,7 | 19589 | 54,4 |
| 12 | 48 | 8256 | 91,7 | 23117 | 64,2 |

Morgrim : 1500 PV (étourdissements ×0,5, non repoussable) ; Nyxar : 1200 PV, reste à 12–18 m. Nyxessa au palier 5 ajoute environ 34,7 DPS sur une nuit (4 160 dégâts / 120 s).

Temps pour abattre Morgrim seul, DPS mono soutenu : Paladin 37 s ; Viking 37 s ; Mage 46 s ; Rôdeur 34 s ; Assassin 34 s ; Assassin dans le dos 14 s ; Rôdeur à la tête 22 s.


---

## Simulation de vagues (01/10/2026)

Demande de Quentin (01/10/2026) : « L'archer (Rôdeur) : je pense qu'on nerf un peu les dégâts de base et les critiques. Il faudrait créer un outil qui compare les dégâts de manière équivalente des personnages soumis à des vagues d'ennemis. » Deux outils sont prévus : ce **simulateur Python** (théorique, ci-dessous) et un banc en jeu dans Unity (à venir), qui doit confirmer ses ordres de grandeur. Le nerf du Rôdeur est **proposé** ici, chiffré après mesure ; rien n'a été changé dans `GameBalance`.

```
python Docs/outils/simulateur_vagues.py                        # nuits 3, 6, 9, 12, profils moyen et bon (Markdown)
python Docs/outils/simulateur_vagues.py --nuit 6 --profil bon  # une nuit, un profil (options répétables)
python Docs/outils/simulateur_vagues.py --classe Rôdeur        # une classe (les indices restent rapportés aux cinq)
python Docs/outils/simulateur_vagues.py --seuil 15             # marque ▲ / ▼ les classes à plus de ±15 % de la moyenne
python Docs/outils/simulateur_vagues.py --json                 # mêmes chiffres en JSON
python Docs/outils/simulateur_vagues.py --surcharge arcDegatsMax=45,arcTete=1.8   # « et si » sans toucher au code
python Docs/outils/simulateur_vagues.py --nerf                 # variantes du nerf du Rôdeur
python Docs/outils/simulateur_vagues.py --tete 0.7             # autre taux de tir à la tête (sensibilité)
python Docs/outils/simulateur_vagues.py --palier 0             # sans les missiles de Nyxessa (défaut : palier 1)
```

### Méthode

- **Valeurs** : lues dans `Assets/Scripts/Jeu/GameBalance.cs` par l'analyseur de `equilibrage.py` (initialiseurs des champs, tableaux par nuit, `StatsSquelette` des sbires, guerriers, voleurs et mages) ; les élites (PV ×3, dégâts ×1,5, portée +0,3, une dès la nuit 5, deux dès la nuit 7) sont lues dans `DirecteurVagues.cs`, le rayon de la fumée dans `Fumigene.cs`. Une valeur absente ou illisible arrête le script avec la liste de ce qui manque ; rien n'est inventé. Les quelques durées qui ne sont écrites que dans le code des classes (état « lâcher » de l'arc 0,3 s, départ de la nuée, tic du cône 0,25 s, impact du saut, cri du rugissement…) sont regroupées dans `CODE`, en tête du script, avec le fichier d'où elles viennent. Brûlure : le modèle **en paliers** de `Brulure.cs` (jauge remplie par la boule et chaque tic du cône, palier qui monte au-delà de 100 %, redescente après 1 s sans feu), avec les chiffres du jour (paliers 5 / 8 / 12 par seconde, trois au plus ; si les champs disparaissaient, le script reprendrait la brûlure d'avant et le dirait).
- **Vagues** : le plan exact de `DirecteurVagues.Preparer` pour **un joueur** : nombre d'ennemis de la nuit, parts de mages, voleurs et guerriers tirées sortie par sortie, élites au milieu, clairières actives de la nuit (1, 1, 2, 2 puis 3), départs à 0 / 40 / 80 s (0 / 30 / 60 / 90 dès la nuit 9), sorties étalées sur 8 s, PV × multiplicateur de la nuit. Les squelettes sortent de terre (1 s), marchent environ 68 m (20 s d'un sbire) par l'un des trois couloirs (22 m de large, flou 3 m, écart de vitesse ±8 %) jusqu'à une place à ±55° autour de Nyxessa. **Mêmes vagues pour les cinq classes** (graine de vague commune), cinq tirages moyennés.
- **Squelettes** (`Squelette.cs`) : ils frappent Nyxessa au contact (3,2 m) ; en route, ils poursuivent le héros qui passe à moins de 8 m (assassin furtif : 6 m devant à ±60°, 1,5 m derrière ; personne dans la fumée), abandonnent à 15 m ou après 4 s sans frapper ; riposte au contact après deux coups de suite à moins de 3 m ; préparation et intervalle de chaque type ; étourdissements et ralentis appliqués ; les mages s'arrêtent à 7 m et tirent sur le héros à 7,5 m, sinon sur Nyxessa ; évitement entre squelettes (capsules de 0,4 m, élites 0,52 m) : pas d'empilement au même point.
- **Zone** : chaque coup prend ses cibles dans sa forme réelle (secteur de l'épée ±40° à 2,6 m et 3 cibles au plus, hache ±70° à 2,4 m, tournante 2,3 m, saut 3,5 m, boule 2 m, cône ±20° à 6 m, nuée 3 m, éventail de la salve ±20°, charge de 1,4 m de large), comme `Combat.Ennemis` (distance au bord de la capsule). La densité n'est donc pas un paramètre : elle vient des positions des squelettes, serrés au contact de Nyxessa ou étirés le long des couloirs.
- **Le héros** est seul devant Nyxessa, ne meurt pas (les dégâts qu'il reçoit sont comptés pour information), ne kite pas. Mêlée : il va chercher ce qui frappe (Nyxessa ou lui) dans une laisse de 10 m autour d'elle. Distance : il tient un poste à 4,5 m de Nyxessa, du côté de la clairière la plus pressée. Missiles de Nyxessa au palier 1 (crédités à part, pas au héros). Pas de temps fixe de 0,05 s, graines fixes : le résultat est déterministe.
- **Rotations** (scriptées, avec les jauges et recharges du code) :
  - Paladin : épée ; charge bélier sur une cible à au moins 2,5 m (moyen) ou 4 m (bon), dégâts selon la distance parcourue, étourdissement de la cible et des traversés ; soin d'aura quand il a perdu 25 % de sa vie. Ni garde ni parade (pas de dégâts).
  - Viking : rage de départ 30 (plancher) et ses gains (+8 par cible de la hache, +2 par cible et par tic de la tournante), baisse hors combat ; rugissement s'il y a 4 (moyen) ou 3 (bon) squelettes à 10 m (provocation, Peau de fer) ; saut sur le groupe le plus dense à portée de bond (3 ou 2 squelettes, ou le seul présent) ; tournante dès 3 squelettes à 2,3 m, arrêtée sous 2 ou à rage vide ; hache sinon.
  - Mage (kit refondu dans `GameBalance` le 01/10/2026 : mana 3/s, cône 30 dégâts/s pour 10 mana/s qui ralentit, grande boule de feu en LB, mur de flammes en RB ; **leur code n'est pas encore écrit** au moment de la simulation : la grande boule et le mur sont modélisés d'après les infobulles de `GameBalance`, et l'on suppose que la grande boule allume la brûlure comme la boule ; si ces champs disparaissent, le script reprend le Mage d'avant) : mur de flammes en travers du groupe qui approche (4 squelettes à 4–14 m pour le joueur moyen, 3 pour le bon) ; grande boule sur le groupe le plus fourni dans ses 5 m (3 ou 2 squelettes) ; boule de feu sur le groupe le plus fourni à 30 m (vol à 18 m/s), +4 mana par cible ; cône dès 3 squelettes dans le cône et 40 (moyen) ou 20 (bon) de mana, arrêté sous 2 cibles ou à mana vide ; brûlure en paliers (le mur monte d'un palier à l'entrée, puis toutes les 1,5 s).
  - Rôdeur : arc (cible : ce qui frappe Nyxessa ou lui, sinon le plus proche de Nyxessa, à 40 m au plus) ; charge 1,2 s puis 0,3 s avant de rebander (RT tenu) ; flèche pleine charge 50, étourdit 1 s ; nuée dès qu'elle couvre 2 squelettes, ou 1 quand il n'y en a qu'un ou deux à 25 m (moyen : centrée sur le premier venu ; bon : sur le groupe le plus dense) ; roulade et salve quand un squelette est à 5 m (moyen) ou dès qu'une cible est à 20 m (bon), la salve tirée en éventail (chaque flèche touche le premier squelette sur sa ligne).
  - Assassin : dague (cible, angle du dos et exécution comme `ClasseAssassin.PorterDague`) ; furtif hors combat (4 s) et dans la fumée ; grenade dans la mêlée (3 squelettes à 5 m) ; Pas de l'ombre derrière une cible à 2–9 m (remis à zéro par une exécution) ; arbalète sur ce qui approche quand rien n'est dans la laisse. Bon joueur : change de cible après un coup sur un squelette qui frappe Nyxessa pour garder le dos (la riposte vient au deuxième), contourne sa cible pour la prendre de dos, achève en priorité ce qui est sous 30 %.
- **Profils** (hypothèses, à confronter au banc) :

| | Moyen | Bon |
|---|---|---|
| Temps perdu entre deux actions | 0,15 s | 0,05 s |
| Flèches qui touchent / dont à la tête | 80 % / 25 % | 92 % / 50 % |
| Tirs lâchés à pleine charge (sinon 50 %) | 70 % | 95 % |
| Salve : flèches qui touchent / tête | 75 % / 10 % | 90 % / 30 % |
| Arbalète : touche / tête | 80 % / 25 % | 92 % / 50 % |
| Boule de feu qui touche la cible visée (sinon explosion à 1,2 m) | 75 % | 92 % |
| Assassin : garde le dos en changeant de cible, contourne | non | oui |
| Assassin : bond et grenade utilisés quand ils sont prêts | une fois sur deux | toujours |

- **Mesures** :
  - **DPS mono** : un guerrier de la nuit aux PV infinis, au contact de Nyxessa (il la frappe, riposte comprise), 180 s de rotation complète ; pas d'exécution (PV infinis). **Indice mono** : rapporté à la moyenne des cinq classes.
  - **DPS en combat** : dégâts utiles ÷ temps où un squelette est à portée de l'arme principale (mêlée : à 12,5 m de Nyxessa ; Mage : 30 m ; Rôdeur : 40 m). Donné pour information : la fenêtre n'est pas la même d'une classe à l'autre.
  - **DPS de nuit** : dégâts utiles du héros avant l'aube ÷ 120 s ; **indice vague** = DPS de nuit ÷ moyenne des cinq. C'est la mesure équivalente pour tous : mêmes squelettes, même durée. Quand toutes les classes vident les vagues (nuits 3 et 6), elle tend vers 1 pour tout le monde (l'offre d'ennemis limite) ; elle départage les classes quand la nuit sature (9 et 12).
  - **Dégâts utiles** : sans le surplus sur un squelette déjà achevé (une exécution compte la vie qui restait). **Part critiques** : tête, dos, furtif, exécution.
  - **Vidage** : de la première sortie d'une vague à la mort de son dernier squelette (la nuit est prolongée de 60 s après l'aube pour le mesurer ; « — » : pas vidée). **Survivants à l'aube** : squelettes encore debout à 120 s (désintégrés en jeu). **Ennemis à Nyxessa** : squelettes qui l'ont frappée au moins une fois.

### Résultats (générés le 01/10/2026, `python Docs/outils/simulateur_vagues.py --seuil 15`)

#### Nuit 3, profil moyen (22 squelettes, PV ×1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 28,1 | **0,8** ▼ | 36,6 | 19,2 | 0,98 | 0 % | 41 / 39 / 38 | 4,2 | 229 | 0,4 | 559 |
| Viking | 31,2 | 0,89 | 43,9 | 19,6 | 1 | 0 % | 37 / 36 / 36 | 3,8 | 173 | 0,2 | 297 |
| Mage | 38,4 | 1,09 | 37,5 | 20 | 1,02 | 0 % | 34 / 34 / 33 | 0 | 0 | 0 | 313 |
| Rôdeur | 32,5 | 0,93 | 27,6 | 20,1 | 1,02 | 28 % | 40 / 39 / 37 | 0,6 | 9 | 0,2 | 383 |
| Assassin | 45,6 | **1,3** ▲ | 34,5 | 19 | 0,97 | 45 % | 40 / 43 / 41 | 8 | 395 | 1,2 | 437 |

Origine des dégâts : Paladin épée 88 %, charge 12 % ; Viking tournante 45 %, hache 35 %, saut 20 % ; Mage boule 54 %, grande boule 22 %, brûlure 19 %, cône 6 % ; Rôdeur arc 69 %, nuée 20 %, salve 11 % ; Assassin dague 84 %, arbalète 16 %.

#### Nuit 6, profil moyen (30 squelettes, PV ×1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 28,1 | **0,8** ▼ | 34,6 | 26,3 | 0,96 | 0 % | 46 / 75 / 52 | 10,2 | 997 | 4,8 | 1222 |
| Viking | 31,2 | 0,89 | 44,7 | 27,7 | 1,01 | 0 % | 41 / 46 / 46 | 11,4 | 706 | 3 | 542 |
| Mage | 38,4 | 1,1 | 43,3 | 29,8 | 1,09 | 0 % | 36 / 43 / 40 | 2,8 | 116 | 0,4 | 697 |
| Rôdeur | 31,5 | 0,9 | 32,1 | 27,4 | 1 | 23 % | 41 / 49 / 48 | 6,8 | 344 | 4,2 | 921 |
| Assassin | 45,6 | **1,3** ▲ | 34,3 | 25,3 | 0,93 | 51 % | 46 / 71 / 55 | 17,8 | 1553 | 5,4 | 809 |

Origine des dégâts : Paladin épée 90 %, charge 10 % ; Viking tournante 46 %, hache 36 %, saut 18 % ; Mage boule 52 %, brûlure 22 %, grande boule 22 %, cône 4 % ; Rôdeur arc 66 %, nuée 26 %, salve 9 % ; Assassin dague 90 %, arbalète 10 %.

#### Nuit 9, profil moyen (42 squelettes, PV ×1,1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 28,1 | **0,79** ▼ | 41,1 | 34,3 | 0,91 | 0 % | 57 / 81 / 105 / — | 13,8 | 2298 | 16 | 3137 |
| Viking | 31,2 | 0,88 | 58,1 | 44,9 | **1,19** ▲ | 0 % | 42 / 44 / 62 / 42 | 11,6 | 872 | 8,2 | 1049 |
| Mage | 38,6 | 1,09 | 54,2 | 45,1 | **1,2** ▲ | 0 % | 38 / 42 / 53 / 42 | 4,2 | 254 | 7,8 | 1610 |
| Rôdeur | 33 | 0,94 | 36,3 | 33,8 | 0,9 | 28 % | 61 / — / — / — | 23,4 | 4342 | 19,2 | 2475 |
| Assassin | 45,6 | **1,29** ▲ | 36,7 | 30 | **0,8** ▼ | 54 % | 60 / — / — / — | 29 | 4636 | 19,4 | 3055 |

Origine des dégâts : Paladin épée 92 %, charge 8 % ; Viking tournante 68 %, hache 21 %, saut 12 % ; Mage boule 46 %, brûlure 25 %, grande boule 18 %, cône 11 % ; Rôdeur arc 60 %, nuée 28 %, salve 12 % ; Assassin dague 96 %, arbalète 4 %.

#### Nuit 12, profil moyen (48 squelettes, PV ×1,2 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 28,1 | **0,8** ▼ | 45 | 39,8 | 0,93 | 0 % | — / — / — / — | 16,6 | 2561 | 20,6 | 5261 |
| Viking | 31,2 | 0,89 | 63,4 | 53,2 | **1,24** ▲ | 0 % | 43 / 64 / 59 / 48 | 14 | 1214 | 9,2 | 1295 |
| Mage | 38,2 | 1,09 | 62,4 | 54,5 | **1,28** ▲ | 0 % | 41 / 45 / 55 / 44 | 8 | 484 | 8,4 | 1966 |
| Rôdeur | 33,1 | 0,94 | 37,7 | 36 | **0,84** ▼ | 22 % | — / — / — / — | 31 | 8101 | 25 | 3093 |
| Assassin | 45,6 | **1,29** ▲ | 35,3 | 30,2 | **0,71** ▼ | 45 % | — / — / — / — | 31,8 | 7264 | 28,6 | 6260 |

Origine des dégâts : Paladin épée 93 %, charge 7 % ; Viking tournante 68 %, hache 19 %, saut 13 % ; Mage boule 41 %, brûlure 26 %, grande boule 18 %, cône 15 % ; Rôdeur arc 55 %, nuée 32 %, salve 13 % ; Assassin dague 98 %, arbalète 2 %.

#### Nuit 3, profil bon (22 squelettes, PV ×1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 31,6 | **0,78** ▼ | 40 | 19,4 | 0,98 | 0 % | 40 / 37 / 37 | 3,8 | 204 | 0,2 | 465 |
| Viking | 33,8 | **0,83** ▼ | 43,2 | 19,3 | 0,97 | 0 % | 37 / 36 / 37 | 3,4 | 164 | 0,2 | 298 |
| Mage | 41,1 | 1,02 | 38,5 | 20,1 | 1,01 | 0 % | 34 / 34 / 32 | 0 | 0 | 0 | 229 |
| Rôdeur | 47,5 | **1,17** ▲ | 37,7 | 21,2 | 1,07 | 58 % | 32 / 34 / 30 | 0,6 | 9 | 0 | 93 |
| Assassin | 48,1 | **1,19** ▲ | 42,6 | 19,3 | 0,97 | 63 % | 38 / 39 / 36 | 9,4 | 228 | 0,2 | 378 |

Origine des dégâts : Paladin épée 89 %, charge 11 % ; Viking tournante 42 %, hache 39 %, saut 19 % ; Mage boule 58 %, grande boule 22 %, brûlure 20 % ; Rôdeur arc 83 %, nuée 10 %, salve 7 % ; Assassin dague 84 %, arbalète 16 %.

#### Nuit 6, profil bon (30 squelettes, PV ×1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 31,6 | **0,79** ▼ | 36,8 | 26,7 | 0,93 | 0 % | 45 / 63 / 48 | 12,6 | 1112 | 3,8 | 813 |
| Viking | 33,8 | **0,84** ▼ | 44,8 | 28,5 | 0,99 | 0 % | 41 / 46 / 44 | 13 | 725 | 2 | 502 |
| Mage | 40,9 | 1,02 | 46,3 | 30 | 1,04 | 0 % | 34 / 42 / 37 | 2 | 84 | 0 | 577 |
| Rôdeur | 45,7 | 1,14 | 40,8 | 30,3 | 1,05 | 50 % | 35 / 43 / 37 | 5,8 | 162 | 0,2 | 389 |
| Assassin | 48,1 | **1,2** ▲ | 45,1 | 28,1 | 0,98 | 76 % | 40 / 47 / 46 | 20 | 1100 | 2,8 | 377 |

Origine des dégâts : Paladin épée 88 %, charge 12 % ; Viking tournante 43 %, hache 38 %, saut 19 % ; Mage boule 51 %, grande boule 22 %, brûlure 22 %, cône 6 % ; Rôdeur arc 76 %, nuée 16 %, salve 8 % ; Assassin dague 86 %, arbalète 14 %.

#### Nuit 9, profil bon (42 squelettes, PV ×1,1 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 31,6 | **0,79** ▼ | 43 | 37,1 | 0,91 | 0 % | 58 / 69 / 96 / 71 | 16,4 | 2307 | 14,2 | 2344 |
| Viking | 33,8 | **0,84** ▼ | 59,1 | 45,8 | 1,12 | 0 % | 41 / 45 / 56 / 41 | 12,2 | 698 | 8 | 1158 |
| Mage | 41,2 | 1,02 | 56,8 | 45,3 | 1,11 | 0 % | 38 / 39 / 49 / 41 | 4 | 278 | 8,4 | 1208 |
| Rôdeur | 46,6 | **1,16** ▲ | 44,6 | 40,9 | 1 | 48 % | 39 / 41 / 81 / 58 | 17,6 | 1411 | 12,4 | 1345 |
| Assassin | 48,1 | **1,19** ▲ | 44,5 | 35,4 | 0,87 | 75 % | 45 / 66 / 95 / 66 | 33 | 3184 | 15,2 | 1608 |

Origine des dégâts : Paladin épée 92 %, charge 8 % ; Viking tournante 65 %, hache 22 %, saut 13 % ; Mage boule 44 %, brûlure 24 %, grande boule 19 %, cône 14 % ; Rôdeur arc 68 %, nuée 23 %, salve 9 % ; Assassin dague 96 %, arbalète 4 %.

#### Nuit 12, profil bon (48 squelettes, PV ×1,2 ; missiles de Nyxessa : palier 1)

| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Paladin | 31,6 | **0,78** ▼ | 48,6 | 44 | 0,92 | 0 % | 85 / 128 / 106 / 77 | 19,8 | 2623 | 18 | 3876 |
| Viking | 33,8 | **0,84** ▼ | 61,3 | 53,8 | 1,13 | 0 % | 43 / 52 / 67 / 52 | 17,8 | 1414 | 8,2 | 1260 |
| Mage | 41,3 | 1,02 | 63,7 | 55,4 | **1,16** ▲ | 0 % | 38 / 43 / 53 / 42 | 5,4 | 339 | 7,8 | 1615 |
| Rôdeur | 47 | **1,17** ▲ | 46,3 | 45,4 | 0,95 | 48 % | 46 / 93 / 102 / 76 | 28,4 | 4083 | 17,2 | 1536 |
| Assassin | 48,1 | **1,19** ▲ | 48,1 | 40,1 | **0,84** ▼ | 78 % | 80 / 130 / 107 / 76 | 40,2 | 4658 | 20,6 | 2625 |

Origine des dégâts : Paladin épée 93 %, charge 7 % ; Viking tournante 66 %, hache 20 %, saut 14 % ; Mage boule 39 %, brûlure 24 %, grande boule 19 %, cône 18 % ; Rôdeur arc 65 %, nuée 24 %, salve 11 % ; Assassin dague 97 %, arbalète 3 %.

▲ / ▼ : plus de ±15 % de la moyenne des cinq classes.


### Lecture

- **Nuits 3 et 6** : toutes les classes vident les vagues (indices vague de 0,93 à 1,09) ; les écarts sont ailleurs. L'Assassin laisse le plus de squelettes frapper Nyxessa (8 à 20, contre 0 à 13 pour les autres) : furtif, il ne retient personne. Le Rôdeur et le Mage la protègent le mieux (ils tuent pendant la marche).
- **Nuits 9 et 12** (la nuit sature, aucune classe ne vide tout avant l'aube) : la **zone** décide. Viking (tournante : 65 à 68 % de ses dégâts) et Mage (grande boule, cône et brûlure : 54 à 61 % des siens) sont 11 à 28 % au-dessus de la moyenne ; Paladin (3 cibles au plus) 7 à 9 % en dessous ; Assassin (une cible par coup) 13 à 29 % en dessous. Le Rôdeur y est à 0,84–0,90 (moyen) et 0,95–1,00 (bon).
- **Mono-cible** : l'Assassin est en tête (+19 à +20 % pour le bon joueur, +29 à +30 % pour le moyen : il a deux coups dans le dos avant chaque riposte, et le bon joueur change de cible pour la garder) ; le **Rôdeur bon joueur est à +14 à +17 %** (moyen : −6 à −10 %) ; le Paladin est à −20 % (sa charge n'est pas lancée sur un mannequin collé à lui, et ses 3 cibles ne comptent pas ici). Avec la brûlure en paliers et son nouveau kit, le Mage est au niveau de la moyenne en mono (38 à 41, contre 32,8 dans le modèle du 27/09).
- **Le Rôdeur, donc** : ce n'est pas en vague qu'il dépasse (il y est dans la moyenne pour un bon joueur, en dessous pour un joueur moyen dès la nuit 9), c'est **sur une cible, entre les mains d'un bon tireur**, et c'est la tête qui fait l'écart : sa part de critiques passe de 22–28 % (moyen) à 48–58 % (bon). À 50 et ×2, une flèche à la tête fait 100 : **un sbire (100 PV) meurt d'une seule flèche** jusqu'à la nuit 8, un mage aussi. Sensibilité : avec 70 % de tirs à la tête (`--tete 0.7`), le Rôdeur bon monte à +25 à +28 % en mono. S'y ajoute ce qu'aucun indice ne compte : entre les mains d'un bon joueur, il reçoit le moins de coups de toutes les classes (93 et 389 dégâts aux nuits 3 et 6, contre 229 à 813 pour les autres), sans même kiter dans le simulateur.

### Proposition de nerf du Rôdeur (à valider par Quentin)

Variantes calculées par `python Docs/outils/simulateur_vagues.py --nerf` (les quatre autres classes ne bougent pas, les moyennes sont recalculées ; 5 tirages ; « < Paladin » : sous le Paladin en dégâts de nuit ; la colonne « Dans ±10 % » exige les deux profils, et aucune variante ne la remplit parce que le joueur moyen est déjà sous la bande aux nuits 9 et 12) :

Indice vague / indice mono du Rôdeur (1 = moyenne des cinq classes ; « < X » : sous X en dégâts de nuit) :

| Variante | N3 moyen | N6 moyen | N9 moyen | N12 moyen | N3 bon | N6 bon | N9 bon | N12 bon | Dans ±10 % | Jamais sous Assassin/Paladin |
|---|---|---|---|---|---|---|---|---|---|---|
| Actuel (50, tête ×2, tir rapide 10) | 1,02 / 0,93 | 1 / 0,9 | 0,9 / 0,94 < Paladin | 0,84 / 0,94 < Paladin | 1,07 / 1,17 | 1,05 / 1,14 | 1 / 1,16 | 0,95 / 1,17 | non | non |
| A : pleine charge 45 | 1,03 / 0,87 | 0,98 / 0,84 | 0,84 / 0,88 < Paladin | 0,79 / 0,88 < Paladin | 1,06 / 1,1 | 1,03 / 1,07 | 0,96 / 1,08 | 0,93 / 1,09 < Paladin | non | non |
| B : tête ×1,75 | 1,02 / 0,89 | 1 / 0,87 | 0,86 / 0,91 < Paladin | 0,81 / 0,91 < Paladin | 1,06 / 1,11 | 1,03 / 1,08 | 0,94 / 1,09 | 0,91 / 1,1 < Paladin | non | non |
| C : pleine charge 45, tête ×1,8 | 1,03 / 0,84 | 0,97 / 0,82 | 0,86 / 0,85 < Paladin | 0,78 / 0,86 < Paladin | 1,04 / 1,05 | 1,03 / 1,02 | 0,99 / 1,03 | 0,91 / 1,04 < Paladin | non | non |
| D : pleine charge 44, tête ×1,75, tir rapide 9 | 1,03 / 0,82 | 0,97 / 0,8 < Paladin | 0,84 / 0,83 < Paladin | 0,79 / 0,84 < Paladin | 1,04 / 1,02 | 1,03 / 1 | 0,95 / 1 | 0,9 / 1,01 < Paladin | non | non |
| E : pleine charge 42, tête ×1,8 | 1,02 / 0,81 | 0,97 / 0,79 < Paladin | 0,83 / 0,82 < Paladin | 0,79 / 0,82 < Paladin | 1,03 / 1 | 1,02 / 0,98 | 0,95 / 0,99 | 0,9 / 0,99 < Paladin | non | non |
| F : pleine charge 48, tête ×1,8 | 1,03 / 0,88 | 1 / 0,86 | 0,86 / 0,89 < Paladin | 0,8 / 0,89 < Paladin | 1,06 / 1,09 | 1,02 / 1,06 | 0,94 / 1,08 | 0,94 / 1,08 | non | non |
| G : pleine charge 47, tête ×1,85 | 1,03 / 0,87 | 0,98 / 0,85 | 0,85 / 0,88 < Paladin | 0,8 / 0,88 < Paladin | 1,06 / 1,09 | 1,02 / 1,06 | 0,94 / 1,07 | 0,93 / 1,08 | non | non |
| H : C + nuée 16 par salve (compensation de zone) | 1,03 / 0,86 | 0,98 / 0,84 | 0,86 / 0,87 < Paladin | 0,79 / 0,87 < Paladin | 1,05 / 1,06 | 1,02 / 1,04 | 0,99 / 1,05 | 0,9 / 1,06 < Paladin | non | non |

DPS mono du Rôdeur (moyen / bon) et flèches à pleine charge pour abattre (corps / tête) :

| Variante | DPS mono moyen | DPS mono bon | Part critiques (bon) | Sbire 100 PV | Sbire nuit 11 (120) | Voleur 115 | Guerrier 160 | Mage 70 |
|---|---|---|---|---|---|---|---|---|
| Actuel (50, tête ×2, tir rapide 10) | 32,5 | 46,7 | 51 % | 2 / 1 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| A : pleine charge 45 | 30 | 42,8 | 49 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| B : tête ×1,75 | 31,2 | 43,3 | 49 % | 2 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| C : pleine charge 45, tête ×1,8 | 29,1 | 40,4 | 46 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| D : pleine charge 44, tête ×1,75, tir rapide 9 | 28,3 | 39 | 45 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 3 | 2 / 1 |
| E : pleine charge 42, tête ×1,8 | 27,6 | 38,2 | 45 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 3 | 2 / 1 |
| F : pleine charge 48, tête ×1,8 | 30,5 | 42,5 | 46 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| G : pleine charge 47, tête ×1,85 | 30,3 | 42,5 | 48 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |
| H : C + nuée 16 par salve (compensation de zone) | 29,9 | 41,2 | 45 % | 3 / 2 | 3 / 2 | 3 / 2 | 4 / 2 | 2 / 1 |

Autres classes, indice vague / mono (Rôdeur actuel) : N3 moyen : Paladin 0,98 / 0,8, Viking 1 / 0,89, Mage 1,02 / 1,09, Assassin 0,97 / 1,3 ; N6 moyen : Paladin 0,96 / 0,8, Viking 1,01 / 0,89, Mage 1,09 / 1,1, Assassin 0,93 / 1,3 ; N9 moyen : Paladin 0,91 / 0,79, Viking 1,19 / 0,88, Mage 1,2 / 1,09, Assassin 0,8 / 1,29 ; N12 moyen : Paladin 0,93 / 0,8, Viking 1,24 / 0,89, Mage 1,28 / 1,09, Assassin 0,71 / 1,29 ; N3 bon : Paladin 0,98 / 0,78, Viking 0,97 / 0,83, Mage 1,01 / 1,02, Assassin 0,97 / 1,19 ; N6 bon : Paladin 0,93 / 0,79, Viking 0,99 / 0,84, Mage 1,04 / 1,02, Assassin 0,98 / 1,2 ; N9 bon : Paladin 0,91 / 0,79, Viking 1,12 / 0,84, Mage 1,11 / 1,02, Assassin 0,87 / 1,19 ; N12 bon : Paladin 0,92 / 0,78, Viking 1,13 / 0,84, Mage 1,16 / 1,02, Assassin 0,84 / 1,19.


Sensibilité, bon joueur à 70 % de tirs à la tête (`--nerf --tete 0.7 --profil bon`) : indice mono 1,25–1,28 aujourd'hui, 1,15–1,18 avec F, 1,10–1,13 avec C, 1,05–1,08 avec E ; indice vague 0,93–1,07 avec C.

**Variante retenue : F — pleine charge `arcDegatsMax` 50 → 48, tête `arcTete` ×2 → ×1,8, tir rapide `arcDegatsMin` inchangé (10).** C'est le « un peu » de la demande : −4 % sur la flèche au corps, −14 % sur la flèche à la tête.

- Bon joueur : indice mono 1,14–1,17 → **1,06–1,09**, indice vague 0,95–1,07 → **0,94–1,06** : tout dans ±10 % de la moyenne ; jamais sous l'Assassin ni le Paladin (nuit 12 : 0,94 contre 0,92 et 0,84). DPS mono 46,7 → 42,5.
- Une flèche à la tête fait 86,4 au lieu de 100 : **le sbire ne tombe plus d'une seule flèche** (deux), le mage (70 PV) toujours. Le corps passe de 2 à 3 flèches sur un sbire (48 × 2 = 96). C'est le changement de ressenti le plus net.
- La salve de la roulade utilise aussi `arcTete` : ses têtes passent de 36 à 32,4. La nuée et la salve au corps ne bougent pas.
- Coût pour le joueur moyen : −6 % en mono (32,5 → 30,5) ; en vague, nuits 9 et 12 : 0,90 → 0,86 et 0,84 → 0,80. Il y était **déjà sous le Paladin** avant tout nerf : le simulateur dit que le Rôdeur moyen est faible tard, le bon fort sur une cible. Le nerf vise le second ; la faiblesse du premier, si le banc la confirme, se traite par la zone ou le contrôle, pas par l'arc (la variante H, nuée 16 par salve en plus de C, ne la rattrape pas : 0,79 à la nuit 12).
- Si le banc mesure un taux de tête plus haut que 50 % pour un bon joueur (à 70 %, F laisse le Rôdeur à +15 à +18 % en mono), passer à **C** (45, ×1,8) : bon joueur mono 1,02–1,05 (1,10–1,13 à 70 % de têtes), vague 0,91–1,04, au niveau du Paladin à la nuit 12 (0,91 contre 0,92, dans le bruit) ; joueur moyen −10 %. G (47, ×1,85) donne presque F. Plus forts : **D** (44, ×1,75, tir rapide 9) et **E** (42, ×1,8) passent sous le Paladin à la nuit 12 et retirent 13 à 15 % au joueur moyen ; **A** (45 seul) ou **B** (×1,75 seul) laissent le bon joueur à 1,07–1,11 en mono.
- À reporter si Quentin valide : `GameBalance.cs` (`arcDegatsMax`, `arcTete`, l'en-tête « 10 à 50 dégâts, tête ×2 » et les infobulles), `Assets/Jeu/Resources/GameBalance.asset`, le commentaire de `ClasseRodeur`, la page `Wiki/pages/classe-rodeur.md` ; puis relancer `simulateur_vagues.py` et `equilibrage.py`.

### Limites du modèle (ce que le banc en jeu devra confirmer)

- **Les mains** : taux de touche, de tête, de dos et choix de cible sont des **hypothèses de profil**, pas des mesures. Le taux de tête du Rôdeur sur un squelette qui marche, à la manette, est le chiffre qui pèse le plus sur la proposition (voir la sensibilité à 70 %) : le banc doit le mesurer.
- **La survie ne compte pas** : le héros ne meurt pas, n'est pas interrompu par les coups, ne garde pas (Paladin : ni garde ni parade parfaite), ne kite pas (le Rôdeur reste à son poste). Les classes de mêlée encaissent beaucoup (Paladin et Assassin jusqu'à 5 000 à 6 000 dégâts à la nuit 12, soit des dizaines de morts réelles) sans perdre de DPS : en jeu, elles feraient moins ; le Rôdeur, qui fuit, ferait un peu moins aussi (temps de repli) mais mourrait bien moins.
- **Le contrôle ne compte qu'indirectement** (étourdissements et ralentis appliqués aux squelettes, provocation du Viking) : il se voit dans les dégâts à Nyxessa, pas dans l'indice.
- **Le Mage est en chantier** : sa grande boule et son mur de flammes sont simulés d'après `GameBalance` avant que leur code existe ; quand `ClasseMage` les aura, relancer le simulateur (la moyenne des cinq, donc les indices des autres, en dépend).
- **Rotations scriptées** : des seuils simples (nombre de squelettes à portée, mana, rage), pas un joueur qui anticipe ; un vrai bon joueur placerait mieux le cône, le mur, la nuée, le saut.
- **Géométrie plane** : pas de NavMesh, d'obstacles, de pente ni de hauteur ; vol des projectiles en ligne droite (la touche est un tirage, pas une balistique) ; évitement entre squelettes simplifié ; place du héros à distance fixe (4,5 m).
- **Hors du modèle** : les boss (Morgrim nuit 10, Nyxar nuit 12), le jeu à plusieurs (+60 % d'ennemis par joueur, plafond de 60), les améliorations de l'arbre, le bouclier de Nyxessa et les paliers de missiles au-delà du 1.
- **Bruit** : cinq tirages ; les indices bougent de ±0,03 d'un jeu de graines à l'autre. Un écart de moins de 0,05 entre deux classes n'est pas significatif.
- **Le banc en jeu** devra confirmer, dans l'ordre : le taux de tête et de touche du Rôdeur (et de l'arbalète), le DPS mono de chaque classe sur un mannequin, le temps de vidage d'une vague de la nuit 9 ou 12 par classe, et les dégâts reçus ; si ses chiffres s'écartent de plus de 10 % de ceux-ci, ajuster les profils du script (`PROFILS`) avant de trancher.
