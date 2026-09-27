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
