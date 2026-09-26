# Écrans de la version 0.1

Première version jouable (décision de l'utilisateur) : **défense solo, une seule classe (le Paladin), pas de donjon**. Écrans construits le 25/09/2026 dans `sandbox-ui` sur le socle décrit dans `ui-socle.md` (thème, `InputPrompt`, navigation manette, tailles 80 / 100 / 135 %). Règles : `main/Wiki/pages/` (deroule, nyxessa, portail, classes, interface, commandes). Maquettes : Main, Pause, HUD_Jour, HUD_Nuit, Score.

Banc : scène `Assets/Scenes/UIv01.unity` (première scène du build), données factices. Générée par **Deathless > UI > 5. Scène des écrans 0.1**.

Captures (`Assets/Screenshots/`) : `UI01_menu.png`, `UI01_hud_jour.png`, `UI01_hud_alerte.png`, `UI01_hud_nuit.png`, `UI01_mort.png`, `UI01_pause.png`, `UI01_options_commandes.png`, `UI01_score.png`, `UI01_hud_x3.png`. Lobby aux trois tailles, dont un format large (21:9) : `lobby_x1.png`, `lobby_x2.png`, `lobby_x3.png`, `lobby_x3_large.png` (26/09/2026, voir « Lobby multijoueur » ci-dessous).

## Principe : les écrans ne connaissent pas le jeu

```
 jeu (main)                          UI (ce dossier)
 ──────────                          ───────────────
 implémente IEtatPartie ─┐
 implémente IEtatJoueur ─┼─► DonneesUI.Enregistrer(...) ─► NavigateurEcrans ─► écrans (HUD, score…)
 implémente IScoreFin  ──┤                                        │
 implémente ICommandesPartie ◄─────────────────────────────────────┘  (Solo, prêt, quitter…)
```

- Le jeu ne lit rien de l'UI ; l'UI **lit** les interfaces à chaque image et reçoit quelques **événements** pour les instants à mettre en scène.
- Sens UI → jeu : uniquement `ICommandesPartie` (boutons des menus).
- Les **entrées de jeu** (compétences, prêt, sauter…) ne passent pas par l'UI : le jeu les lit lui-même (`InputChordResolver`, voir `ui-socle.md`). Seule exception : `Gameplay/Pause`, qu'écoute le navigateur pour ouvrir la pause.

## Interfaces de données (`Assets/Scripts/UI/Donnees/`, espace de noms `Deathless.UI.Donnees`)

### `DonneesUI` (statique) : le point de rendez-vous

| Membre | Rôle |
|---|---|
| `Enregistrer(partie, joueur, score, commandes)` | Le jeu publie ses sources. Une même classe peut implémenter plusieurs interfaces. |
| `Retirer()` | Retire partie, joueur et score (garde `Commandes`, pour que le menu principal puisse lancer une partie). |
| `Partie`, `Joueur`, `Score`, `Commandes` | Sources courantes (null si absentes). |
| `event Changees` | Après chaque `Enregistrer` / `Retirer`. |

Le navigateur choisit l'écran de base d'après `DonneesUI` : `Partie == null` → **menu principal** ; `Partie.Phase == Terminee` → **score** ; sinon → **HUD**. Le jeu n'a donc qu'à tenir ses sources à jour :

| Moment (jeu) | Appel |
|---|---|
| Au lancement du jeu | `DonneesUI.Enregistrer(null, null, null, commandes)` |
| Partie lancée (`LancerSolo`) | `DonneesUI.Enregistrer(partie, joueur, score, commandes)` |
| Fin de partie | passer `Phase` à `Terminee` puis déclencher `PartieTerminee` |
| Rejouer (tous prêts) | réinitialiser et `Enregistrer` à nouveau (ou garder les mêmes objets, `Phase` repassant à `Jour`, puis `Changees` via un nouvel `Enregistrer`) |
| Quitter la partie | `DonneesUI.Enregistrer(null, null, null, commandes)` |

### `IEtatPartie` (HUD, lu à chaque image)

| Membre | Type | Sens |
|---|---|---|
| `Phase` | `PhasePartie` | `Jour`, `Crepuscule`, `Nuit`, `Aube`, `Terminee`. |
| `NumeroNuit` | int | Nuit en cours (Nuit, Aube) ou prochaine nuit (Jour, Crépuscule). 1 au premier jour. |
| `TempsRestantPhase` | float (s) | Temps restant de la phase. Tous prêts : le jeu le ramène à 5 s. |
| `VieNyxessa`, `VieMaxNyxessa` | float | Barre verte. |
| `Bouclier`, `BouclierMax` | float | Barre du bouclier sous la vie. `BouclierMax = 0` ou `Bouclier = 0` : barre masquée. Couleur selon `Bouclier / BouclierMax` : ≥ 60 % bleu, ≥ 30 % orange, sinon rouge ; sous 50 % : « Bouclier de Nyxessa à N % ». |
| `OrEquipe` | int | Caisse commune (haut, à droite). |
| `VoteActif` | bool | Vote « prêt » possible (jour, toute l'équipe au village). Faux : invite et compteur masqués. |
| `JoueursPrets`, `JoueursTotal` | int | « Prêts 1 / 1 ». |
| `AngleNyxessa` | float (°) | Nyxessa vue de la caméra : -180 à 180, 0 = devant, positif = à droite. Place l'indicateur « Nyxessa attaquée » : ±35° en haut, au-delà de ±145° en bas, sinon au bord gauche ou droit (plus bas quand elle est plus en arrière). |
| `event NuitCommencee(int nuit)` | | Début d'une nuit : bannière « NUIT N » (3,5 s). |
| `event NyxessaFrappee()` | | Nyxessa ou son bouclier touché : indicateur de bord d'écran pendant 3 s après le dernier coup. |
| `event PartieTerminee()` | | Données de score prêtes : l'écran de score s'affiche. |

Réglages du HUD (statiques, `EcranHud`) : `DelaiAlerteNuit = 15` s (alerte « La nuit tombe dans N s » tant que `Phase == Jour` et `TempsRestantPhase ≤ 15`), `DureeAlerteAttaque = 3` s, `DureeBanniere = 3,5` s.

### `IEtatJoueur` (joueur local, lu à chaque image)

| Membre | Type | Sens |
|---|---|---|
| `Nom`, `Classe` | string | « Quentin », « Paladin » (l'initiale de la classe va dans le portrait). |
| `TeinteClasse` | Color | Fond du portrait (Paladin : or `#d9b264`). |
| `Vie`, `VieMax`, `Endurance`, `EnduranceMax` | float | Jauges du bas, à gauche. |
| `EstMort`, `TempsAvantReapparition` | bool, float (s) | Voile « Vous êtes tombé · Réapparition dans N s » ; réticule, interaction masqués, compétences grisées. |
| `Competences` | `IReadOnlyList<ICompetenceHud>` | Barre du bas, de gauche à droite. **Liste stable** (le HUD reconstruit la barre si la référence change). |
| `InviteInteraction` | string | Action possible devant soi (« Parler au sorcier »), null ou vide : masquée. Affichée avec l'invite de `Gameplay/Interact`. |
| `EstPret` | bool | Vote du joueur local. |

`ICompetenceHud` :

| Membre | Sens |
|---|---|
| `Action` | Action de DeathlessControls, « Carte/Action » : l'invite sous l'emplacement en est tirée (suit l'appareil). |
| `Nom` | Infobulle et doc. |
| `Icone` (Texture2D, peut être null) / `Abreviation` | Icône, sinon deux lettres. |
| `Etat` | `Prete`, `Recharge` (voile + secondes, le voile descend avec la recharge), `Active` (bordure or : garde levée…), `Indisponible` (grisé). |
| `RechargeRestante`, `RechargeTotale` | Secondes (1 décimale sous 1 s). |

Barre du Paladin dans la démo : Frappe à l'épée (`Gameplay/AttackPrimary`), Garde et parade (`Gameplay/AttackSecondary`, active tant que la garde est levée), **Charge bélier (`Gameplay/Skill1`, LB / A)**, **Soin sur soi (`Gameplay/Skill2`, RB / R)** : voir « À trancher ».

### `IScoreFin` (écran de score, après `PartieTerminee`)

| Membre | Sens |
|---|---|
| `Resultat` | `Victoire` (« Nyxessa a survécu aux 12 nuits ») ou `Defaite` (« Nyxessa est tombée à la nuit N »). |
| `NuitAtteinte` | Nuit de la chute (12 en cas de victoire). |
| `DureeSecondes`, `OrTotal` | En-tête : « DURÉE 38:40 », « OR DE L'ÉQUIPE 1 840 ». |
| `Joueurs` | `IReadOnlyList<ILigneScore>` : `Nom`, `Classe`, `TeinteClasse`, `EstLocal`, puis les sept catégories dans l'ordre des colonnes : `OrRapporte`, `DegatsInfliges`, `EnnemisTues`, `Morts`, `CoupsCritiques`, `DegatsEvitesNyxessa` (dégâts que le joueur a empêchés d'atteindre Nyxessa), `SoinsProdigues`. Le meilleur de chaque catégorie (le plus élevé ; **le moins de morts**) reçoit une pastille or et une couronne. |
| `JoueursPrets`, `JoueursTotal`, `EstPretLocal` | Bouton Rejouer : « Prêts 0 / 1 ». |

Les trois catégories ajoutées le 25/09/2026 (Coups critiques, Dégâts évités à Nyxessa, Soins prodigués : le plus élevé gagne) sont affichées ; l'écran a donc sept colonnes, sur une ligne à toutes les tailles (à ×3, textes et pastilles resserrés, en-têtes sur deux lignes). Ajouter une catégorie : une propriété dans `ILigneScore`, une entrée dans `EcranScore.s_Categories`, un en-tête dans `Score.uxml`.

### Donjon : `IEtatDonjon` (`Donnees/IEtatDonjon.cs`, 26/09/2026)

Cette interface est enregistrée dans `DonneesUI.Donjon` et implémentée par `DonjonJeu`. Le HUD la lit ainsi :
- **`OrPorte`, `AuDonjon`** : la pastille « or porté · au donjon » s'affiche sous la caisse commune (`or-porte`) quand de l'or est porté ou que le joueur est au donjon.
- **`AvantRappel`** : s'il vaut 0 ou plus et que le joueur est au donjon, une alerte rouge pulsée s'affiche (`donjon-alerte`), avec le texte « Le portail se ferme dans N s : rentrez au village ! ». Elle remplace l'alerte de la tombée de la nuit.
- **`Message`** : une pastille (`donjon-message`) affiche par exemple « 140 or versés à la caisse commune » ou « Rappelé par Nyxessa : 88 or gardés, 132 perdus ».

### Missiles de Nyxessa : `IEtatMissiles` (`Donnees/IEtatMissiles.cs`, 26/09/2026)

Interface facultative, à implémenter par l'objet enregistré comme `IEtatPartie` (le HUD la trouve par `DonneesUI.Partie as IEtatMissiles`). Sans elle, ou avec `MissilesMax = 0`, le compteur est masqué. Implémentée par `HudPresenter` (jeu) et `EtatFactice` (banc).

| Membre | Type | Sens |
|---|---|---|
| `MissilesDisponibles` | int | Missiles prêts à partir (stock de Nyxessa). |
| `MissilesMax` | int | Stock maximal du palier acheté à la relique. |
| `ChargeProchainMissile` | float (0 à 1) | Recharge du prochain missile. Sans objet quand le stock est plein (1 par convention). |

Côté jeu, `HudPresenter` lit `Partie.Etat.nyxessa` (`stock`, `regeneration`, `palierMissiles`) et `GameBalance` (`missilesStockPaliers`, `missileRegenerationPaliers`). Chez un client réseau, le stock vient de l'hôte et la recharge est extrapolée sur place (`reseau.md`).

**Compteur du HUD** (`missiles`, dans la rangée `hud-nyx`, juste à droite de la barre de Nyxessa) : une pastille sombre avec l'icône du missile (crâne en gemmes vertes, `IconesUI.MissileNyxessa`) et « 3 / 5 » (nombre en Fredoka SemiBold ivoire, « / 5 » en petit et atténué).
- **Recharge** : l'icône éteinte (`nyxessa_missile_eteint`, en ardoise) est dessous. L'icône allumée est dessus, dans un conteneur `overflow: hidden` ancré en bas, dont la hauteur suit `ChargeProchainMissile` : elle monte du bas vers le haut (d'abord la traînée, puis le crâne). Un filet vert clair marque le niveau.
- **Stock plein** : l'icône est entièrement allumée, et la pastille prend un liseré vert.
- **Missile gagné** : l'icône, entièrement allumée le temps de l'animation, fait un « pop » d'échelle (×1,3, 0,3 s, `EcranHud.DureePopMissile`). La recharge du suivant reprend ensuite à zéro.
- **Missile tiré** : un anneau vert s'élargit et s'efface, et le nombre passe en vert un instant (0,35 s, `DureeTirMissile`).
- **Stock vide** : le nombre est grisé.

La rangée du haut est élargie pour que la barre de Nyxessa garde sa longueur : `hud-haut` passe de 780 à 900 px (à ×3, de 620 à 720 px).

Vérifié en Play le 26/09/2026. Captures (`Assets/Screenshots/`) : village en solo, palier 3, 2 missiles sur 4, recharge en cours : `hud_missiles_village_x1.png`, `_x2.png`, `_x3.png` ; missile gagné : `hud_missiles_village_pop.png` ; missile tiré : `hud_missiles_village_tir.png` ; banc (3 sur 5) : `hud_missiles_banc_x1.png`, `_x2.png`, `_x3.png`.

### Statuts : `IEtatStatuts`, `IStatutAffiche`, `IEnnemiAffecte` (`Donnees/IStatuts.cs`, 26/09/2026)

Posée par le jeu dans `DonneesUI.Statuts` (`Deathless.Jeu.StatutsUI`, créée par `HudPresenter`), lue à chaque image par le HUD et le menu du personnage. Absente : rien n'est affiché. Règles : Wiki `statuts.md` et `interface.md`, « Statuts ».

| Membre | Sens |
|---|---|
| `StatutsJoueur` | Statuts du joueur local (`IStatutAffiche` : `Nom`, `Icone` « statut_<id> », `Effet` en clair avec ses valeurs, `Source`, `Restant` en s ou négatif sans durée, `Duree`, `Nefaste`). L'eau du donjon y figure comme un « Ralenti » sans durée. |
| `EnnemisAffectes` | Ennemis vivants avec au moins un statut (`IEnnemiAffecte` : `PositionTete` au-dessus du crâne, `Visible` si ses rendus sont affichés, `Statuts`). |

- **HUD** (`Ecrans/HudStatuts.cs`, classe à part : `EcranHud` ne fait que la créer et l'appeler) :
  - Rangée du joueur `statuts` (`.hud-statuts`), en bas à gauche, dans le bloc joueur, **juste au-dessus de la barre de vie** (maquette B, 26/09/2026 : déplacée de son ancienne position au-dessus du portrait). La pastille des points de compétence reste au-dessus de tout le bloc joueur (bottom 189 px, 142 px à ×3).
  - Six cases de 60 px au plus : icône, liseré rouge (affliction) ou or (bienfait), jauge de durée en bas de la case, secondes dans une pastille. Masquée quand le joueur est mort.
  - Pour déplacer la rangée (refonte des barres) : seulement les règles `.hud-statuts` de `Hud.uss`.
- **Au-dessus des ennemis** : calque `statuts-ennemis`, placé chaque image en espace écran comme les pseudos. Une petite rangée par ennemi affecté et visible : quatre cases de 36 px au plus, jauge de 3 px, sans secondes.
  - Seulement dans le champ de la caméra et à moins de 30 m (`HudStatuts.DistanceEnnemis`) ; elle s'estompe sur les 6 derniers mètres. 24 rangées au plus.
  - Aucune allocation d'image en image : cases et rangées réutilisées.
- **Menu du personnage** : section « AFFLICTIONS » sous les caractéristiques (`perso-afflictions`, `Ecrans/AfflictionsPersonnage.cs`).
  - Un bouton par statut : icône, jauge, secondes. « Aucune affliction. » sinon.
  - Le survol à la souris ou le focus à la manette montre une infobulle sous le bouton (au-dessus s'il manque de place, sans cacher les statuts voisins ; `perso__bulle`) : nom, effet, durée restante, source.
  - Navigation : gauche et droite d'un statut à l'autre ; à droite du dernier, la première amélioration ; à gauche d'une amélioration, le dernier statut visité. Si le statut qui a le focus s'achève, le focus revient à la première amélioration.
- **Banc UIv01** : `EtatFactice` pose des statuts factices (`Dev/StatutsFactices.cs`), si le jeu n'en a pas déjà posé. Le joueur est brûlé, ralenti et ivre ; deux ennemis fictifs devant la caméra sont brûlé et ralenti, étourdi et provoqué. Les durées tournent en boucle ; `ForcerStatuts(false)` masque le tout.
- Vérifié en Play le 26/09/2026. Captures (`Assets/Screenshots/`) :
  - `statuts_village_hud_ennemis.png` : Village, mage ralenti et ivre, un squelette brûlé, étourdi et ralenti, un autre étourdi et provoqué ;
  - `statuts_village_hud_x3.png` : même scène à ×3 ;
  - `statuts_menu_afflictions_infobulle.png` : menu du personnage, focus sur « Ralenti » ;
  - `statuts_banc_uiv01.png` : banc UIv01.
  - Chute testée : 8 m donnent 53 dégâts et Ralenti −40 % pendant 3 s ; un saut sur place ne donne rien.
- Icônes : `ArtSources/Icones/generer_statuts.py` → `Statuts/`, copiées dans `Assets/UI/Icones/Statuts/` par la synchronisation des icônes (menu 6).

### Jauge de parade : `IJaugeParade` (`Donnees/IJaugeParade.cs`, 26/09/2026)

Posée par le jeu dans `DonneesUI.Parade` (`Deathless.Jeu.ParadeParfaite`, créée par `ClassePaladin` pour le héros local ; retirée à sa destruction), lue à chaque image par le HUD. Absente (autres classes) : rien n'est affiché. Règles : Wiki `interface.md`, « Jauge de parade », et `classe-paladin.md`.

| Membre | Sens |
|---|---|
| `Visible` | Un coup parable vise le joueur local (du début de sa préparation à l'impact), ou son issue est encore montrée (0,45 s). |
| `AvantImpact` | Secondes avant l'impact prévu (négatif juste après). |
| `Duree` | Durée couverte par toute la largeur (`GameBalance.paradeJaugeDuree`, 0,8 s) : le curseur part de la gauche quand il reste `Duree` s. |
| `FenetreParade`, `FenetreParfaite` | Fenêtres avant l'impact (`paradeFenetre` 0,35 s, `paradeParfaiteFenetre` 0,1 s). |
| `Appui` | Secondes avant l'impact où la garde a été levée pour ce coup ; négatif : pas d'appui. |
| `Resultat`, `DepuisResultat` | `ResultatParade` : `Aucun`, `Bloque`, `Parade`, `Parfaite` (dès l'appui), `Touche` ; temps écoulé depuis. |

- **HUD** (`Ecrans/HudParade.cs`, classe à part : `EcranHud` ne fait que la créer et l'appeler ; allure dans `Hud.uss`, règles `.hud-parade`). Élément `parade` inséré juste après le réticule, centré 40 px sous le centre de l'écran, 240 × 36 px (tailles ×1/×2/×3 par l'échelle du panneau).
  - Piste sombre de 12 px à liseré (`--dl-color-border-strong`), fenêtre de parade en ivoire à 28 %, fenêtre parfaite en or (`--dl-color-gold`) collée au bord droit (l'impact).
  - Curseur ivoire de 6 × 26 px qui avance vers la droite ; repère fin (`--dl-color-text-muted`) à l'instant de l'appui.
  - Issue : classes `hud-parade--parfaite` (liseré et curseur or, « Parfaite » en or au-dessus, rebond ×1,12), `--parade` (liseré ivoire), `--bloque` (liseré grisé), `--touche` (liseré rouge `--dl-color-life`) ; la jauge s'efface ensuite (opacité).
  - Pas de vert, aucune icône. Masquée quand le joueur est mort.
- **Pourquoi sous le réticule** : en mêlée, le regard est au centre ; au-dessus de l'attaquant, la jauge bougerait avec lui et se mêlerait aux rangées de statuts des ennemis.
- Pas encore de version factice dans le banc UIv01 (`EtatFactice`) ; vérification dans le Village avec `Deathless.Jeu.Dev.ScenariosParade`.

### Bloc joueur : maquette B (26/09/2026)

Quentin a choisi la **maquette B** parmi trois essais du bloc joueur (bac à sable `sandbox-ui`, `Assets/UI/Screens/HudMaquettes/`, capture `maquette_hud_B.png`), intégrée dans le vrai HUD :

- **Jauge de classe (mana, rage)** : un **anneau plein autour du portrait**, à la couleur de la jauge (mana `#4a8fe0`, rage `#ff8c1a`), départ en haut (12 h), balayage horaire. Pas d'anneau pour une classe sans jauge (`EcranHud.MajClasse`). Élément `Deathless.UI.AnneauJauge` (Painter2D, `Assets/Scripts/UI/FormesHud.cs`), porté du bac à sable (`Dev/FormesMaquetteHud.cs`) sous ce nom.
- **Vie** : une large barre à embouts de gemme (losanges or), la seule des trois à afficher son chiffre (le nombre courant seul, pas de « / max »). Pas de libellé « Vie ».
- **Endurance** : un filet fin, presque invisible, qui ne s'éclaire vraiment que sous 70 % environ (classe `hud-joueur__endurance-piste--active`, opacité 0,3 → 1). Pas de libellé « Endurance ».
- **Statuts** : la rangée `.hud-statuts` (`HudStatuts.cs`, inchangée) est passée dans le bloc joueur, juste au-dessus de la barre de vie, au lieu d'au-dessus du portrait.
- Aucune icône à côté des barres de vie ou d'endurance.
- Portrait (emblème de classe), furtif de l'assassin, potion, pastille des points de compétence et écran de mort inchangés ; tailles ×1/×2/×3 revues (`.dl-scale-3`).
- Fichiers touchés : `Assets/UI/Screens/Hud/Hud.uxml`, `Hud.uss`, `Assets/Scripts/UI/Ecrans/EcranHud.cs` (`MajJoueur`, `MajClasse`), `Assets/Scripts/UI/FormesHud.cs` (`AnneauJauge`).
- Vérifié en Play le 26/09/2026 : banc UIv01 (statuts factices) et Village avec le Mage (mana) et le Viking (rage), aux tailles ×1/×2/×3, console sans erreur. Captures (`Assets/Screenshots/`) : `hud_b_uiv01_statuts.png`, `hud_b_village_mage.png`, `hud_b_village_viking.png`.

### Roue à emotes : `IRoueEmotes`, `IEmoteRoue` (`Donnees/IRoueEmotes.cs`, 26/09/2026)

Posée par le jeu dans `DonneesUI.RoueEmotes` (le composant `Deathless.Jeu.EmotesHeros` du héros local), lue à chaque image. Le jeu tient tout (ouverture, secteur pointé, lancement) ; l'UI ne fait qu'afficher. Règles : Wiki `interface.md`, « Roue à emotes ».

| Membre | Sens |
|---|---|
| `Ouverte` | Touche `Gameplay/Emote` maintenue : la roue est affichée. |
| `Emotes` | Les emotes (`IEmoteRoue` : `Nom`, `Icone`), la première en haut puis dans le sens horaire. Huit pour l'instant. |
| `Pointee` | Secteur pointé, ou -1 au centre (relâcher n'y lance rien). |

**Calque** `CalqueRoueEmotes` (`Ecrans/CalqueRoueEmotes.cs`, UXML et USS `Assets/UI/Screens/RoueEmotes/`) : ce n'est pas un écran de la pile, car la carte Gameplay doit rester active tant que la touche est tenue. `NavigateurEcrans` le crée dans le conteneur des écrans (champ `roueEmotes`, à renseigner dans les scènes Village et UIv01 et dans les deux générateurs) et l'affiche seulement quand le HUD est au sommet et que la roue est ouverte. Contenu : anneau de huit secteurs (`Deathless.UI.FondRoueEmotes`, Painter2D, couleurs `--roue-*` reprises des jetons), icône (`emote_<id>`, `ArtSources/Icones/generer_emotes.py`, copiées dans `Assets/UI/Icones/Emotes/`) et nom par secteur ; secteur pointé en or ; au centre, le nom de l'emote pointée (ou « Annuler ») et l'invite de `Gameplay/Emote`. Son de survol à chaque changement de secteur. Roue de 720 px à ×2, 640 px à ×3.

### Classes : `IClassesJouables`, `IClasseJouable`, `IEtatJoueurClasse` (`Donnees/IClasses.cs`, 25/09/2026)

Cinq classes jouables (Wiki `classes.md`, `commandes.md`). Tout est **facultatif et rétrocompatible** : un jeu qui n'implémente rien de nouveau garde son comportement (choix affiché depuis le catalogue, `LancerSolo()` appelé).

| Type | Membres | Rôle |
|---|---|---|
| `IClasseJouable` | `Id` (« paladin », « mage », « rodeur », « assassin », « viking »), `Nom`, `Role`, `Arme`, `Description`, `Teinte`, `Embleme` (icône hexagonale, ex. « classe_paladin »), `Jauge` (`JaugeClasse.Aucune` / `Mana` / `Rage`), `Actions` | Une classe, pour l'écran de choix et la carte du menu principal. |
| `IActionClasse` | `Action` (« Gameplay/AttackPrimary »…), `Nom` (null ou vide : emplacement vide, affiché grisé « Vide pour l'instant »), `Icone` (identifiant de l'icône, ex. « paladin_charge_belier ») | Cinq actions dans l'ordre : attaque principale (RT), attaque secondaire (LT), compétences 1 (LB), 2 (RB), 3 (LB + RB). |
| `IEtatJoueurPotions` | `Potions`, `PotionsMax` | Facultatif, sur l'objet enregistré comme `IEtatJoueur` : le HUD affiche l'emplacement de potion (icône `commun_potion_soin`, nombre restant, invite de `Gameplay/DrinkPotion`) à droite des jauges ; grisé à 0. Le jeu ne l'implémente pas encore (pas de potions en 0.1) ; `EtatFactice` oui (2 sur 3). |
| `IApercuClasse` | `Rendu` (RenderTexture), `Montrer(classeId)`, `Cacher()`, `Tourner(degres)` | Facultatif, posé par le jeu dans `DonneesUI.ApercuClasse` : personnage de la classe en 3D dans l'écran de choix (voir « Choix de classe en 3D »). Absent (banc UIv01) : la colonne est masquée. |
| `IClasseJouable.Verrouillee` | bool | Classe à venir (**Druide**, **Mécanicien**, au catalogue depuis la 0.3) : visible dans le choix de classe avec l'étiquette « Bientôt » (pastille ivoire), carte assombrie, fiche réduite (nom, « Bientôt », « Arme, rôle et compétences à venir »), invite Valider grisée ; Valider joue le son de refus doux (`SonInterface.Refus`, Kenney `error_004`) et ne lance rien. `ClassesJouables.Jouable(id)` ; `Lancer` refuse une classe verrouillée ; `Derniere` ne renvoie jamais une classe verrouillée. |

### Pseudo : `IProfilJoueur`, `ProfilJoueur` (`Donnees/IProfil.cs`, 25/09/2026)

| Membre | Rôle |
|---|---|
| `DonneesUI.Profil` (`IProfilJoueur`) | `Pseudo` (« Joueur » tant qu'aucun n'est choisi), `PseudoDefini`, `event PseudoChange`. Pour le jeu et le futur réseau (`HudPresenter.Nom` et `EtatFactice.Nom` le renvoient). |
| `ProfilJoueur` (statique) | `Valider(saisie, out raison)`, `Normaliser` (espaces rognés, espaces multiples réduits), `Definir(saisie)` : enregistré dans les PlayerPrefs (`Deathless.Pseudo`). Règle : 3 à 16 caractères ; lettres (accents compris), chiffres, tirets et espaces. |

**Premier lancement** : sans pseudo enregistré, le navigateur ouvre l'écran de saisie (`EcranSaisie`, obligatoire : B efface un caractère au lieu de revenir) avant le menu principal ; le menu s'affiche dès que le pseudo est valide. **Options > Jeu** : ligne « Pseudo … Modifier » qui rouvre le même écran (B : retour). Capture : `UI01_pseudo.png`.

### Lobby multijoueur : `ILobby` (`Donnees/ILobby.cs`, 25/09/2026)

Transport retenu par Quentin : **Unity Relay et Lobby** (Unity Gaming Services), code court de salon ; l'adresse IP directe reste un secours. **Aucun code réseau pour l'instant** : l'écran lit `DonneesUI.Lobby` ; à défaut, le menu crée un `LobbyFactice` (`Deathless.UI.Dev`), comme `EtatFactice` sur le banc.

| Membre | Rôle |
|---|---|
| `Etat` (`EtatLobby`) | `Aucun` (écran d'entrée), `Connexion`, `Salon`, `CompteARebours`, `Lancement`, `Erreur` (voir `Message`). |
| `CodeSalon`, `EstHote` | Code court (6 caractères) affiché (masqué par défaut, bouton Afficher) et copiable par l'hôte. |
| `Joueurs` (`IJoueurLobby` : `Pseudo`, `ClasseId`, `Pret`, `EstLocal`, `EstHote`), `JoueursMax` (4) | Emplacements du salon. |
| `CompteARebours`, `Message` | Secondes avant le lancement quand tous sont prêts ; information ou erreur. |
| `CreerSalon()`, `Rejoindre(code)`, `RejoindreParAdresse(adresse)` | Entrée ; l'adresse IP (« 192.168.1.20:7777 ») est un lien secondaire « Rejoindre par adresse IP ». |
| `bool ChoisirClasse(id)` | **Chaque classe est unique dans un salon** (décision de Quentin) : faux si un autre joueur l'a déjà ; en cas de demandes simultanées, le premier arrivé l'obtient (arbitré par l'hôte ou le service). `LobbyOutils.PrisePar(lobby, id)` : pseudo de l'autre joueur qui l'a. |
| `BasculerPret()`, `LancerMaintenant()` (hôte, tous prêts), `Quitter()` | Salon. |

`LobbyFactice` : 1 à 3 autres joueurs simulés (Morgane, Tibo, Lysa, Kael) arrivent l'un après l'autre avec une classe libre et passent prêts au bout de 2 à 6 s ; tous prêts : compte à rebours de 3 s, puis **partie solo avec la classe du joueur local** (`ClassesJouables.Lancer`). Rejoindre : l'hôte simulé est déjà là. Tests : `ForcerAutresPrets()`, `SimulerPrise(id)` ; `EtatFactice.ForcerClassePrise(id)` (un faux joueur prend la classe : affichage « prise »).
| `IClassesJouables` | `Classes`, `LancerSolo(string classeId)` | À implémenter par l'objet enregistré comme `ICommandesPartie` (trouvé par `DonneesUI.Commandes as IClassesJouables`). Sans lui, l'écran affiche `ClassesJouables.Catalogue` et appelle `LancerSolo()`. |
| `IEtatJoueurClasse` | `Jauge`, `ValeurJauge`, `JaugeMax`, `Furtif` | À implémenter par l'objet enregistré comme `IEtatJoueur` (trouvé par `DonneesUI.Joueur as IEtatJoueurClasse`). Le HUD affiche alors la jauge de classe (Mana bleue, Rage orange) en **anneau plein autour du portrait** (maquette B, 26/09/2026 ; élément `Deathless.UI.AnneauJauge`, Painter2D, `EcranHud.MajClasse`) ; pas d'anneau pour une classe sans jauge. Si `Furtif`, l'icône `assassin_furtif` dans une pastille violet nuit (thème Ombre) sur le portrait. |
| `ClassesJouables` (statique) | `Catalogue`, `Proposees`, `Trouver(id)`, `DerniereJouee` (PlayerPrefs `Deathless.DerniereClasse`, défaut « paladin »), `Derniere`, `Lancer(id)` | Données des cinq classes tirées du wiki, et mémoire de la dernière classe jouée. |

Catalogue (actions RT / LT / LB / RB ; LB + RB vide pour toutes) : **Paladin** (épée, garde et parade, charge bélier, soin sur soi) ; **Mage** (boule de feu, cône de flammes maintenu, LB et RB vides ; mana) ; **Rôdeur** (bander et tirer, viser, nuée de flèches, roulade arrière et salve) ; **Assassin** (dague, arbalète en main et visée, grenade fumigène, RB vide ; furtif) ; **Viking** (hache, attaque tournante maintenue, rugissement, saut percutant ; rage). Teintes des portraits : Paladin `#d9b264` (inchangée), Mage `#ff610a` (Feu, vif), Rôdeur `#a8742f` (Chasse, accent ocre : l'ocre clair `#d9b45a` proposé était trop proche de l'or du Paladin), Assassin `#a58ad6` (Ombre, cœur), Viking `#b3261e` (Rage, vif).

**Emplacement vide dans le HUD** : un `ICompetenceHud` dont `Nom` est vide s'affiche grisé (case sombre, sans icône, abréviation ni recharge, invite atténuée).

**Icônes du HUD** (25/09/2026) : chaque emplacement affiche l'icône vectorielle de l'action de la classe (retrouvée par `IEtatJoueur.Classe` et `ICompetenceHud.Action` dans le catalogue), à la place de l'abréviation envoyée par le jeu. Une `ICompetenceHud.Icone` (Texture2D) fournie par le jeu passe devant ; sans icône, l'abréviation reste. États inchangés : recharge (voile qui descend et secondes par-dessus l'icône), actif (bordure or), indisponible (icône atténuée), vide. Captures : `UI01_hud_icones_viking.png`, `UI01_hud_icones_mage.png`, `UI01_hud_icones_assassin.png` (Village), `UI01_hud_icones_furtif_potion.png`, `UI01_hud_icones_mana.png` (banc). L'esquive n'a pas d'emplacement dans le HUD : son icône (`IconesUI.Esquive`) attend qu'on en affiche un.

Côté jeu (`HudPresenter`) : `IClassesJouables` est branché sur l'existant ; `Classes` renvoie le catalogue et `LancerSolo(classeId)` lance le Paladin quelle que soit la classe choisie (l'agent jeu branchera les autres). `IEtatJoueurClasse` n'est pas encore implémenté par le jeu (le Paladin n'a ni jauge ni mode furtif).

### `ICommandesPartie` (UI → jeu)

| Méthode | Appelée par |
|---|---|
| `LancerSolo()` | Choix de classe > Jouer, si le jeu n'implémente pas `IClassesJouables` (sinon `LancerSolo(classeId)`). |
| `BasculerPret()` | Écran de score > Rejouer (A). Le vote du jour vient de l'entrée `Gameplay/Ready`, lue par le jeu. |
| `QuitterPartie()` | Pause > Quitter la partie ; score > Arrêter (B). |
| `QuitterJeu()` | Menu principal > Quitter ; pause > Quitter le jeu. |

### Exemple d'implémentation (main)

```csharp
public class PartieUI : MonoBehaviour, IEtatPartie, IEtatJoueur, IScoreFin, ICommandesPartie
{
    // … propriétés lues dans les systèmes du jeu (cycle, Nyxessa, joueur local, statistiques) …
    void OnEnable() => DonneesUI.Enregistrer(null, null, null, this);
    public void LancerSolo() { /* charger le village, créer le Paladin */ DonneesUI.Enregistrer(this, this, this, this); }
    // Cycle.NuitCommence += n => NuitCommencee?.Invoke(n);  Nyxessa.Touchee += () => NyxessaFrappee?.Invoke(); …
}
```

`Assets/Scripts/UI/Dev/EtatFactice.cs` est une implémentation complète à suivre (et à garder comme banc).

## Écrans (`Assets/Scripts/UI/Ecrans/`, `Assets/UI/Screens/`)

| Écran | UXML / USS | Contrôleur | Contenu |
|---|---|---|---|
| Menu principal | `MenuPrincipal/MenuPrincipal.uxml`, `V01.uss` | `EcranMenuPrincipal` | Solo (ouvre le choix de classe), Multijoueur (ouvre le lobby), Options, Crédits, Quitter ; « Version 0.1 ». Plus de carte de classe (la classe se choisit juste avant de lancer ; la dernière jouée reste mémorisée pour présélectionner le choix). Capture `UI01_menu_sans_carte.png`. |
| Saisie | `Saisie/Saisie.uxml`, `V01.uss` | `EcranSaisie` | Saisie d'un texte : champ (clavier physique) et clavier virtuel `ClavierVirtuel` (grille de touches pour la manette et la souris : lettres avec accents courants, chiffres, tiret, Maj, Espace, Effacer, Valider ; majuscule automatique au début du pseudo). A : touche, B : effacer (saisie obligatoire) ou retour, Y : valider. Trois usages : pseudo (premier lancement, options), code de salon (6 caractères), adresse IP (secours). |
| Lobby | `Lobby/Lobby.uxml`, `V01.uss` | `EcranLobby` | Entrée : « Créer un salon » ; « Rejoindre un salon » (champ du code, ouvre la saisie ; bouton Rejoindre ; lien « Rejoindre par adresse IP »). Salon : code (Afficher, Copier), quatre emplacements (emblème de la classe, pseudo, classe, Prêt / Pas prêt, étiquette Hôte ; emplacement libre grisé), « Choisir sa classe » (écran de choix en mode lobby, avec l'aperçu 3D), « Je suis prêt » (Y), « Lancer » (hôte, actif quand tous sont prêts), « Quitter le salon » (B). Bandeau « Tous prêts : la partie commence dans … » **en surimpression par-dessus les 4 cartes**, centré, sur fond sombre (26/09/2026, retour de Quentin) : élément `lobby-compte` positionné en `position: absolute` (USS), toujours enfant de `lobby-emplacements` mais dessiné en dernier pour rester au-dessus des cartes ; jamais dans la mise en page, ne décale donc jamais rien, dans tous les cas (corrigé en 0.4.3, comportement gardé). Contenu du salon dans un `ScrollView` vertical (`lobby-scroll`, règle des écrans denses à ×3 de `ui-socle.md`) : ne défile que si nécessaire, pour que la rangée d'actions du bas tienne toujours au-dessus de la barre d'invites, à ×1/×2/×3 et sur tout format (16:9, 21:9, 16:10) ; marges et hauteurs resserrées à ×3. Captures : `UI01_lobby_entree.png`, `UI01_lobby_salon.png`, `UI01_lobby_prets.png`, `UI01_lobby_classe_prise.png`, `lobby_x1.png`, `lobby_x2.png`, `lobby_x3.png`, `lobby_x3_large.png` (2560×1080). |
| Choix de classe | `ChoixClasse/ChoixClasse.uxml`, `V01.uss` | `EcranChoixClasse` | Trois colonnes : les cinq classes à gauche (emblème, nom, rôle, étiquette « Dernière »), le personnage en 3D au centre, la fiche à droite (emblème, nom, rôle, arme, description, jauge, cinq actions : icône de l'action, invite du bouton de l'appareil actif, nom ; emplacement vide grisé). À ×3 : colonnes resserrées, étiquette « Dernière » et aide masquées. Dernière classe jouée présélectionnée ; la fiche suit le focus (manette, clavier) et le survol (souris). Valider (A, Entrée, clic) : retient la classe et lance la partie ; Retour (B, Échap) : menu principal. Captures : `UI01_choix_classe_3d_<id>.png` (une par classe, Village) ; avant les icônes et la 3D : `UI01_choix_classe*.png`. |
| Options | `Options/Options.uxml` | `EcranOptions` | Onglets Jeu / Commandes / Audio (LB, RB). Jeu : taille ×1 (80 %), ×2 (100 %), ×3 (135 %). Commandes : table en lecture seule (clavier et manette, la colonne manette suit la dernière manette), lignes focusables et défilantes. Audio : volumes principal, musique, effets spéciaux, interface (voir « Audio »). Réinitialiser (Y) : réglages de l'onglet affiché (taille ×2, ou volumes par défaut). |
| Crédits | `Credits/Credits.uxml` | `EcranCredits` | Contenu de `Wiki/pages/credits.md`. |
| HUD | `Hud/Hud.uxml`, `Hud/Hud.uss` | `EcranHud` | Nyxessa (barre verte) et bouclier (bleu → orange → rouge, masqué sans bouclier), compteur de missiles de Nyxessa (`IEtatMissiles`), temps avec icône jour ou nuit (« Jour · 1:42 avant la nuit », « Nuit 3 · 2:10 avant l'aube », « Crépuscule · la nuit 3 tombe », « Aube · le jour se lève »), « Prêts 1 / 1 » et invite de `Gameplay/Ready`, **alerte avant la nuit** (pastille orange qui clignote, 15 s), **bannière « NUIT N »**, indicateur de bord « Nyxessa attaquée », or, **bloc joueur** (maquette B, 26/09/2026 : portrait avec l'anneau de jauge de classe, statuts, vie en large barre à embouts de gemme avec son chiffre, endurance en filet qui ne s'éclaire vraiment que sous 70 % ; ni libellé ni icône sur les barres — voir « Classes » et « Statuts » ci-dessous), barre de compétences avec invites et **temps de recharge**, réticule, invite d'interaction, **écran de mort**. |
| Pause | `Pause/Pause.uxml` | `EcranPause` | « La partie continue » ; Reprendre, Options, Quitter la partie, Quitter le jeu. Le HUD reste visible et vivant dessous. |
| Achat | `Achat/Achat.uxml`, `Achat/Achat.uss` | `EcranAchat` | Menu d'achat ouvert par une interaction du jeu (`DonneesUI.OuvrirMenuAchat(IMenuAchat)`, par-dessus le HUD) : titre, aide, or de la caisse commune ; une ligne focalisable par article (nom, niveau, ce qu'apporte le suivant, prix ; atténuée si l'achat est impossible) ; message réservé en permanence (achat fait en or, refus en rouge). Valider achète, Retour ferme ; se ferme seul quand `IMenuAchat.Ouvert` devient faux. Utilisé pour les achats à la relique (`AchatRelique`). Captures : `achat_relique_menu*.png`. |
| Personnage | `Personnage/Personnage.uxml`, `Personnage/Personnage.uss` | `EcranPersonnage` | Menu du personnage (touche Tab, Y ; `DonneesUI.Personnage` : `IMenuPersonnage`, ouvert par `DonneesUI.OuvrirMenuPersonnage()`, par-dessus le HUD) : carte du personnage (emblème, nom, classe, caractéristiques), carte des compétences (points à dépenser, une ligne focalisable par amélioration : icône, nom, rangs en losanges, effet par rang et actuel ; Valider améliore), carte de l'inventaire (9 cases vides). À ×3 : l'inventaire passe dessous, le contenu défile. Le HUD affiche « N points de compétence » et l'invite de `Gameplay/CharacterMenu` au-dessus du portrait. Captures : `personnage_*.png`. |
| Score | `Score/Score.uxml` | `EcranScore` | Résultat, durée, or ; table par joueur, sept catégories (or rapporté, dégâts infligés, ennemis tués, morts, coups critiques, dégâts évités à Nyxessa, soins prodigués), meilleur mis en avant ; Rejouer (vote prêt, « Prêts 0 / 1 ») / Arrêter. |

Formes vectorielles (`Assets/Scripts/UI/FormesHud.cs`, Painter2D, nettes à toutes les tailles) : `GemmeNyxessa`, `IconeJourNuit` (`nuit`), `PieceOr`, `Reticule`, `Couronne`, `FlecheHud` (`angle`).

### Navigateur (`NavigateurEcrans`, sur l'objet du UIDocument)

- Une **pile** : le sommet reçoit les actions ; on affiche l'écran opaque de base (HUD, menu, score) et le sommet par-dessus (pause, options, crédits). Un seul voile, une seule barre d'invites.
- **Une seule carte d'actions active** : HUD au sommet → `Gameplay` seule ; tout autre écran → `UI` seule (`NavigateurEcrans.CarteActive` pour vérifier). La bascule a lieu à chaque changement de sommet ; le focus est rendu au dernier élément focus de l'écran (ou au premier), et retiré en jeu.
- `Gameplay/Pause` (Menu, Options, Échap) ouvre la pause ; `UI/Cancel` (B, Rond, Échap) ferme le sommet ou appelle `Ecran.Retour()` ; `UI/Pause` (Menu, Options) ferme la pause. Au clavier, Échap est Retour et Pause à la fois : seul Retour agit. Une garde d'une image évite qu'un même appui ouvre et referme.
- `UI/TabPrevious`, `UI/TabNext`, `UI/Reset` sont transmis au sommet.
- Les libellés `dl-device-name` des barres d'invites affichent l'appareil détecté.

Ajouter un écran : une classe `Ecran` (`Construire`, `PremierFocus`, `Opaque`, `CarteUI`, `Retour`…), un UXML dans `Assets/UI/Screens/<Nom>/`, un champ `VisualTreeAsset` et une ligne `Creer(...)` dans `NavigateurEcrans`, puis renseigner ce champ dans les scènes (Village, UIv01) et dans les générateurs `Assets/Editor/Jeu/JeuBuilder.cs` et `Assets/Editor/UI/DeathlessUISetup.cs` (fait pour `choixClasse`).

### Audio : volumes et mixer (25/09/2026)

- **Mixer** `Assets/Audio/Deathless.mixer` : groupe Master et trois sous-groupes, **Musique**, **Effets** (combats, compétences, ennemis, Nyxessa, portail, ambiance) et **Interface** (sons des menus). Paramètres exposés en dB : `VolumePrincipal` (Master), `VolumeMusique`, `VolumeEffets`, `VolumeInterface`.
- **`Deathless.Audio.VolumesAudio`** (`Assets/Scripts/Audio/`) : quatre volumes de 0 à 1 (`CanalAudio.Principal`, `Musique`, `Effets`, `Interface`), conversion en dB (20 log10 v ; 0 = coupé, -80 dB), enregistrés dans les PlayerPrefs (`Deathless.Volume.<Canal>`), chargés et appliqués **avant la première scène** (`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`, puis de nouveau à la première image). Défauts : principal 100, musique 10, effets 15, interface 15. `Groupe(canal)` et `Router(source, canal)` branchent une source ; `JouerInterface(Survol | Clic | Retour)` ; `JouerApercu(canal)` ; `JouerAuPoint(clip, point)` remplace `AudioSource.PlayClipAtPoint` (dont la source ne passe par aucun groupe).
- Réglages : asset `Assets/Audio/Resources/DeathlessAudio.asset` (`ReglagesAudio` : mixer, groupes, sons d'interface Kenney `tick_002` (survol), `click_002` (clic), `back_001` (retour), aperçu des effets `skeleton_hit`).
- **Routage** : `AudioBank` met ses sources 3D, sa source 2D et les boucles (`Boucle` : missile, bourdon du portail) dans Effets, les deux sources de musique (jour et nuit) dans Musique ; `ArcBande` passe par `JouerAuPoint`. Les écrans jouent leurs sons dans Interface : survol quand le focus change (pas à l'ouverture d'un écran ni juste après une validation), clic sur tout bouton (`Ecran.SonDeClic` pour un bouton créé après `Construire`), retour sur UI/Cancel. Vérifié en Play (Village, combat) : 30 sources, toutes dans un groupe ; Effets à 0 : -80 dB sur Effets, la musique continue (-3,1 dB).
- **Onglet Audio** : un `Slider` 0-100 par volume (classe `dl-field`, bordure or au focus), pourcentage à droite. Manette et flèches : gauche / droite ±5 % (`EcranOptions.PasVolume`), haut / bas passent d'un réglage à l'autre ; souris : glisser. L'effet s'entend tout de suite : un son court de la catégorie quand on change Principal, Effets ou Interface. Enregistré sur le disque à la fermeture de l'écran. Capture : `Assets/Screenshots/UI01_options_audio.png`.

### Fond du menu principal (scène Village, 25/09/2026)

Le menu principal s'affiche par-dessus le village **de nuit**, sur un plan fixe où Nyxessa et le portail sont lisibles dans la moitié droite (le panneau du menu couvre environ 44 % à gauche à ×2, la carte de classe est en bas à droite). Ce plan n'existe que dans l'état « menu » (`Partie.Etat.phase == Attente`) : pas de squelettes, pas d'horloge, pas de HUD.

- **Caméra** : `Deathless.Jeu.CameraEpaule` sans cible (aucun héros) se place sur le plan du menu. Champs (inspecteur de la caméra de Village, mêmes valeurs par défaut dans le code) : `menuPosition` (-8, 13, -22), `menuRotation` (23,8 ; 13,4 ; 0), `menuChamp` 44° (champ vertical ; le champ de jeu de la caméra est rendu dès qu'elle suit le héros), `menuBalancement` 0,8 m et `menuPeriode` 50 s (très lent va-et-vient latéral ; 0 pour un plan fixe). L'ancienne orbite autour du village est retirée.
- **Ambiance** : `Deathless.Jeu.VueCycle` pilote le cycle en **milieu de nuit** tant que la partie est en attente (`CycleJourNuit.Piloter(Nuit, dureeNuit / 2)`) : lumière, brume, lanternes et lueurs vertes de Nyxessa sont celles de la nuit en partie.
- **Portail ouvert** : en partie, le portail est absent la nuit (seul son socle reste). Pour ce seul plan, `VueCycle` pose `CycleJourNuit.portailForceOuvert = (phase == Attente)` ; la règle de jeu n'est pas changée (le drapeau retombe dès `LancerSolo`).
- Captures des cadrages essayés (`Assets/Screenshots/`) : `menu_nuit_1_rapproche.png` (position (-10, 7, -13), rotation (18,7 ; 29,4 ; 0), champ 44°), **`menu_nuit_2_retenu.png`** (cadrage en place), `menu_nuit_3_moyen.png` ((-4, 11, -20), (22,3 ; 3,4 ; 0), 50°), `menu_nuit_4_large.png` ((-14, 18, -26), (25,3 ; 19,2 ; 0), 40°). Fond sans interface en 1920 × 1080 (pour le launcher) : `menu_nuit_fond.png`.
- Pour changer de cadrage : Play dans Village, régler les champs `menu*` de la caméra en direct, puis reporter les valeurs hors Play.
- **Boucle vidéo pour le launcher** : `Assets/Screenshots/menu_nuit_boucle.mp4` (H.264, 1920 × 1080, 30 images/s, 12 s, muette, 7,5 Mbit/s, 11,2 Mo), même cadrage et même ambiance que `menu_nuit_fond.png`, sans interface. Outil : `Assets/Scripts/Dev/EnregistreurBoucle.cs` (en Play, `UnityEditor.Media.MediaEncoder`, `Time.captureFramerate = 30`, rendu de la caméra seule dans une RenderTexture). Pour l'enregistrement seulement (valeurs de la scène inchangées) : période du balancement de la caméra 12 s (un aller-retour), rotation du cristal 30°/s au lieu de 40 (un tour ; balancement vertical de 3 s : 4 cycles), anneaux du portail 60 et -60°/s au lieu de 70 et -45 (2 tours), vitesse de chaque gemme de la ceinture arrondie à un nombre entier de tours (au moins un). Le reste (brume, lucioles, voxels du portail, oscillations de la ceinture, flammes) : 13 s capturées, la dernière seconde fondue pixel par pixel dans la première. Raccord mesuré : écart moyen entre la dernière et la première image de 0,65 (sur 255, par canal), contre 0,55 entre deux images consécutives ; côte à côte : `menu_nuit_boucle_raccord.png`. Écrire la vidéo hors de `Assets` (Temp) puis la copier : sinon Unity importe le fichier encore incomplet.

### Choix de classe : classes à venir et mode lobby (25/09/2026)

- Liste de sept cartes (cartes resserrées, à ×3 le rôle est masqué) : les cinq classes, puis Druide et Mécanicien verrouillés (étiquette « Bientôt », sous-titre « Prochaine version »). Capture : `UI01_choix_classe_bientot.png`.
- Aperçu 3D des classes à venir : `Assets/Jeu/Resources/ApercusVerrouilles.asset` (`Deathless.Jeu.ApercusVerrouilles` : Druide = `Druid.fbx` + `druid_staff`, Mécanicien = `Engineer.fbx` + `engineer_Wrench`, sous `handslot.r`, pose de repos `Idle_A` jouée par Playables), matériaux un peu assombris et désaturés par un MaterialPropertyBlock (`ApercuClasse.teinteVerrouillee`), sans alpha. Emblèmes : `classe_druide`, `classe_mecanicien` (variante A retenue).
- **Mode lobby** (`EcranChoixClasse.OuvrirPourLobby(surChoix, classeActuelle, prisePar)`) : invite « Choisir » au lieu de « Jouer », sous-titre du salon ; une classe déjà prise par un autre joueur porte l'étiquette « Prise · pseudo », la fiche dit « Déjà prise par … dans ce salon », Valider est inactif (son de refus) ; elle se libère si ce joueur change de classe ou quitte le salon (rafraîchi à chaque image). Solo : `OuvrirSolo()`, rien ne change.

### Choix de classe en 3D (25/09/2026)

- **Jeu** : `Deathless.Jeu.ApercuClasse` (`Assets/Scripts/Jeu/UI/ApercuClasse.cs`), créé par `HudPresenter` au lancement de la scène et enregistré dans `DonneesUI.ApercuClasse`. Petite scène de présentation à (0, -300, 0), sur la couche 30 (retirée du masque de la caméra de jeu) : socle hexagonal bas en ardoise (maillage à facettes créé par le script), trois lumières ponctuelles proches limitées à la couche 30 (clé blanc chaud, contre-jour bleu-violet de la nuit, rappel vert de Nyxessa ; pas de directionnelle, qui pourrait devenir la lumière principale du village), et une seule caméra (champ 26°, pas de post-traitement pour garder l'alpha, pas d'ombres) qui rend dans une RenderTexture 720 × 920 à fond transparent. La caméra n'est active que pendant `Montrer` … `Cacher` (écran de choix ouvert).
- **Personnage** : copie de l'enfant « Modele » du prefab `Heros_<Classe>` de `ClassesJeu.asset` (modèle, arme équipée par le style, contrôleur d'animation au repos), sans le gameplay (Heros, ClasseHeros, entrées restent sur la racine du prefab, non copiée). Un modèle par classe, créé à la première demande puis réutilisé. Changement de classe immédiat, apparition par la taille (0,28 s, léger dépassement, sans alpha). Rotation lente continue (18°/s), plus la souris glissée sur le personnage (`EcranChoixClasse.RotationSouris`, degrés par pixel) et le stick droit (`RotationStick`, degrés par seconde, lu directement sur la manette : la carte UI n'a pas d'action pour ce stick).
- **UI** : colonne `choix-apercu` ; l'image est la RenderTexture en fond (`Background.FromRenderTexture`), ajustée à la boîte. Le fond transparent laisse voir le village de nuit sous le voile de l'écran.

### Tailles 80 / 100 / 135 %

Chaque écran a ses règles `.dl-scale-3` (fin de `Hud.uss` et `V01.uss`) : HUD resserré (compétences à droite, légende de l'or masquée), menus et score resserrés. Vérifié en Play à ×3 : HUD, menu principal, pause, score tiennent sans débordement ; la table des commandes défile.

## Données factices (`EtatFactice`, banc)

Partie accélérée (×4 par défaut : jour de 120 s en 30 s), bouclier à partir du jour 2, coups sur Nyxessa la nuit (angles variés), ennemis tués et or, dégâts subis, **mort pendant la nuit 2** (réapparition en 10 s), **chute de Nyxessa pendant la nuit 3** (écran de score), Rejouer relance après 1,5 s. Les entrées de jeu passent par `InputChordResolver` : LB (A) lance la charge, RB (R) le soin, LT maintenue lève la garde, Vue / pavé tactile / F1 vote « prêt ».

Classes : `EtatFactice` implémente `IClassesJouables` et `IEtatJoueurClasse` ; `LancerSolo(id)` joue la classe choisie (le Paladin garde sa simulation complète ; les autres classes ont leur barre d'actions, vides comprises, une jauge simulée : mana +3/s, -8 par attaque, -12/s cône maintenu ; rage +12 par attaque, -1,5/s hors combat, compétences à 30 ; assassin furtif hors combat et hors nuit). Captures du HUD : `UI01_hud_mage_mana.png`, `UI01_hud_viking_rage.png`, `UI01_hud_assassin_furtif.png`.

Missiles de Nyxessa (`IEtatMissiles`) : stock de 5 (`missilesMax`), un missile toutes les 8 s de jeu (`missileRecharge`) ; la nuit, un tir toutes les 4 à 12 s en gardant un missile en réserve, et une fois sur quatre une salve de tout le stock sauf un.

Méthodes de test : `ForcerMissiles(disponibles, charge01)`, `Forcer(phase, nuit, reste)`, `ForcerNyxessa(vie01, bouclier01)`, `Frapper(angle)`, `ForcerJoueur(vie, endurance, rechargeCharge, rechargeSoin, garde)`, `ForcerMort(s)`, `ForcerScore(or, dégâts, tués, morts, critiques, évités, soins)`, `ForcerFin(résultat, nuit, durée)`, `ForcerPret(bool)`, `ForcerJoueursDeTest(bool)` (deux joueurs fictifs pour tester la table du score), `LancerSolo(classeId)`, `ForcerJauge(0-100)`, `ForcerFurtif(bool?)`, champ `figer`.

## Vérifié en Play (manette simulée)

Menu → A sur Solo → HUD (carte Gameplay seule, pas de focus) ; Start → pause (carte UI, focus Reprendre, la partie continue : le temps défile) ; croix haut/bas ; B → HUD ; Start / Start ouvre puis ferme ; pause → Options → RB (onglet Commandes) → 14 lignes vers le bas (défilement) → B, B ; fin de partie → score (focus Rejouer) → A → nouvelle partie ; score → B → menu ; menu → Crédits → B. Console sans erreur.

## À trancher

- **Touches du Paladin** : la maquette mettait Poussée au bouclier en compétence 1 (LB), Charge en 2 (RB), Soin en 3 (LB + RB). La poussée étant {à confirmer} et absente de la 0.1, la démo met **Charge bélier en compétence 1 (LB)** et **Soin sur soi en compétence 2 (RB)**. C'est une donnée du jeu (`ICompetenceHud.Action`), pas de l'UI.
- **Délai de réapparition** (a-decider.md) : la démo prend 10 s.
- **Alerte avant la nuit** : le wiki la prévoit pour les joueurs au donjon ; sans donjon en 0.1, elle prévient tout le monde 15 s avant le crépuscule. Son d'alerte non géré (pas de son dans l'UI).
- Icônes des compétences : absentes (abréviations « Ép », « Ga », « Ch », « So »).
- Le curseur de la souris n'est ni caché ni verrouillé en jeu (à faire côté jeu, avec la caméra).

## Hors 0.1 (non construit)

Multijoueur en jeu (réseau Unity Relay et Lobby, vie des autres joueurs à gauche, « Prêts 2 / 3 »), potions, donjon et portail (rappel, butin), options Affichage / Audio / manette, personnalisation des touches.

## Report dans main

Mêmes chemins, avec les `.meta`. Le socle est déjà dans main ; **deux fichiers du socle ont changé** depuis le report et doivent être recopiés : `Assets/Scripts/UI/UINavigation.cs` (`ChainerVerticalement`) et `Assets/Editor/UI/DeathlessUISetup.cs` (menu 5, scène UIv01).

| Fichier | Dépend de |
|---|---|
| `Assets/Scripts/UI/Donnees/*.cs` (5) | rien |
| `Assets/Scripts/UI/FormesHud.cs` | UI Toolkit |
| `Assets/Scripts/UI/Ecrans/*.cs` (4) | Donnees, socle (`InputPrompt`, `Gauge`, `UIScale`, `UINavigation`, `InputDeviceWatcher`, `InputGlyphs`), DeathlessControls |
| `Assets/UI/Screens/{MenuPrincipal,Options,Credits,Hud,Pause,Score}/`, `Assets/UI/Screens/V01.uss` | thème Deathless, `FormesHud`, `InputPrompt`, `Gauge` |
| Banc : `Assets/Scripts/UI/Dev/EtatFactice.cs`, `DemoV01.cs`, `Assets/UI/Screens/UIv01/`, `Assets/Scenes/UIv01.unity`, `Assets/Screenshots/UI01_*.png` | tout ce qui précède, `InputChordResolver` |

Dans main, le vrai jeu remplacera `EtatFactice` par ses propres implémentations ; la scène de jeu aura un UIDocument (DeathlessPanel, racine avec un conteneur `ecrans`) portant `NavigateurEcrans` et `UIScale`, et une EventSystem branchée sur la carte UI (voir `ui-socle.md`).
