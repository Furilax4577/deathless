# Écrans de la version 0.1

Première version jouable (décision de l'utilisateur) : **défense solo, une seule classe (le Paladin), pas de donjon**. Écrans construits le 25/09/2026 dans `sandbox-ui` sur le socle décrit dans `ui-socle.md` (thème, `InputPrompt`, navigation manette, tailles 80 / 100 / 135 %). Règles : `main/Wiki/pages/` (deroule, nyxessa, portail, classes, interface, commandes). Maquettes : Main, Pause, HUD_Jour, HUD_Nuit, Score.

Banc : scène `Assets/Scenes/UIv01.unity` (première scène du build), données factices. Générée par **Deathless > UI > 5. Scène des écrans 0.1**.

Captures (`Assets/Screenshots/`) : `UI01_menu.png`, `UI01_hud_jour.png`, `UI01_hud_alerte.png`, `UI01_hud_nuit.png`, `UI01_mort.png`, `UI01_pause.png`, `UI01_options_commandes.png`, `UI01_score.png`, `UI01_hud_x3.png`.

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
| `Joueurs` | `IReadOnlyList<ILigneScore>` : `Nom`, `Classe`, `TeinteClasse`, `EstLocal`, `OrRapporte`, `DegatsInfliges`, `EnnemisTues`, `Morts`. Le meilleur de chaque catégorie (le plus élevé ; **le moins de morts**) reçoit une pastille or et une couronne. |
| `JoueursPrets`, `JoueursTotal`, `EstPretLocal` | Bouton Rejouer : « Prêts 0 / 1 ». |

Catégories {à confirmer} du wiki (critiques, dégâts évités à Nyxessa, soins) : non affichées ; ajouter une propriété et une colonne quand elles seront décidées.

### `ICommandesPartie` (UI → jeu)

| Méthode | Appelée par |
|---|---|
| `LancerSolo()` | Menu principal > Solo. |
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
| Menu principal | `MenuPrincipal/MenuPrincipal.uxml`, `V01.uss` | `EcranMenuPrincipal` | Solo, Options, Crédits, Quitter ; carte « Classe : Paladin » ; « Version 0.1 ». |
| Options | `Options/Options.uxml` | `EcranOptions` | Onglets Jeu / Commandes (LB, RB). Jeu : taille ×1 (80 %), ×2 (100 %), ×3 (135 %). Commandes : table en lecture seule (clavier et manette, la colonne manette suit la dernière manette), lignes focusables et défilantes. Réinitialiser (Y) : taille ×2. |
| Crédits | `Credits/Credits.uxml` | `EcranCredits` | Contenu de `Wiki/pages/credits.md`. |
| HUD | `Hud/Hud.uxml`, `Hud/Hud.uss` | `EcranHud` | Nyxessa (barre verte) et bouclier (bleu → orange → rouge, masqué sans bouclier), temps avec icône jour ou nuit (« Jour · 1:42 avant la nuit », « Nuit 3 · 2:10 avant l'aube », « Crépuscule · la nuit 3 tombe », « Aube · le jour se lève »), « Prêts 1 / 1 » et invite de `Gameplay/Ready`, **alerte avant la nuit** (pastille orange qui clignote, 15 s), **bannière « NUIT N »**, indicateur de bord « Nyxessa attaquée », or, portrait + vie + endurance, barre de compétences avec invites et **temps de recharge**, réticule, invite d'interaction, **écran de mort**. |
| Pause | `Pause/Pause.uxml` | `EcranPause` | « La partie continue » ; Reprendre, Options, Quitter la partie, Quitter le jeu. Le HUD reste visible et vivant dessous. |
| Score | `Score/Score.uxml` | `EcranScore` | Résultat, durée, or ; table par joueur avec meilleur mis en avant ; Rejouer (vote prêt, « Prêts 0 / 1 ») / Arrêter. |

Formes vectorielles (`Assets/Scripts/UI/FormesHud.cs`, Painter2D, nettes à toutes les tailles) : `GemmeNyxessa`, `IconeJourNuit` (`nuit`), `PieceOr`, `Reticule`, `Couronne`, `FlecheHud` (`angle`).

### Navigateur (`NavigateurEcrans`, sur l'objet du UIDocument)

- Une **pile** : le sommet reçoit les actions ; on affiche l'écran opaque de base (HUD, menu, score) et le sommet par-dessus (pause, options, crédits). Un seul voile, une seule barre d'invites.
- **Une seule carte d'actions active** : HUD au sommet → `Gameplay` seule ; tout autre écran → `UI` seule (`NavigateurEcrans.CarteActive` pour vérifier). La bascule a lieu à chaque changement de sommet ; le focus est rendu au dernier élément focus de l'écran (ou au premier), et retiré en jeu.
- `Gameplay/Pause` (Menu, Options, Échap) ouvre la pause ; `UI/Cancel` (B, Rond, Échap) ferme le sommet ou appelle `Ecran.Retour()` ; `UI/Pause` (Menu, Options) ferme la pause. Au clavier, Échap est Retour et Pause à la fois : seul Retour agit. Une garde d'une image évite qu'un même appui ouvre et referme.
- `UI/TabPrevious`, `UI/TabNext`, `UI/Reset` sont transmis au sommet.
- Les libellés `dl-device-name` des barres d'invites affichent l'appareil détecté.

Ajouter un écran : une classe `Ecran` (`Construire`, `PremierFocus`, `Opaque`, `CarteUI`, `Retour`…), un UXML dans `Assets/UI/Screens/<Nom>/`, un champ `VisualTreeAsset` et une ligne `Creer(...)` dans `NavigateurEcrans`.

### Tailles 80 / 100 / 135 %

Chaque écran a ses règles `.dl-scale-3` (fin de `Hud.uss` et `V01.uss`) : HUD resserré (compétences à droite, légende de l'or masquée), menus et score resserrés. Vérifié en Play à ×3 : HUD, menu principal, pause, score tiennent sans débordement ; la table des commandes défile.

## Données factices (`EtatFactice`, banc)

Partie accélérée (×4 par défaut : jour de 120 s en 30 s), bouclier à partir du jour 2, coups sur Nyxessa la nuit (angles variés), ennemis tués et or, dégâts subis, **mort pendant la nuit 2** (réapparition en 10 s), **chute de Nyxessa pendant la nuit 3** (écran de score), Rejouer relance après 1,5 s. Les entrées de jeu passent par `InputChordResolver` : LB (A) lance la charge, RB (R) le soin, LT maintenue lève la garde, Vue / pavé tactile / F1 vote « prêt ».

Méthodes de test : `Forcer(phase, nuit, reste)`, `ForcerNyxessa(vie01, bouclier01)`, `Frapper(angle)`, `ForcerJoueur(vie, endurance, rechargeCharge, rechargeSoin, garde)`, `ForcerMort(s)`, `ForcerScore(or, dégâts, tués, morts)`, `ForcerFin(résultat, nuit, durée)`, `ForcerPret(bool)`, champ `figer`.

## Vérifié en Play (manette simulée)

Menu → A sur Solo → HUD (carte Gameplay seule, pas de focus) ; Start → pause (carte UI, focus Reprendre, la partie continue : le temps défile) ; croix haut/bas ; B → HUD ; Start / Start ouvre puis ferme ; pause → Options → RB (onglet Commandes) → 14 lignes vers le bas (défilement) → B, B ; fin de partie → score (focus Rejouer) → A → nouvelle partie ; score → B → menu ; menu → Crédits → B. Console sans erreur.

## À trancher

- **Touches du Paladin** : la maquette mettait Poussée au bouclier en compétence 1 (LB), Charge en 2 (RB), Soin en 3 (LB + RB). La poussée étant {à confirmer} et absente de la 0.1, la démo met **Charge bélier en compétence 1 (LB)** et **Soin sur soi en compétence 2 (RB)**. C'est une donnée du jeu (`ICompetenceHud.Action`), pas de l'UI.
- **Délai de réapparition** (a-decider.md) : la démo prend 10 s.
- **Alerte avant la nuit** : le wiki la prévoit pour les joueurs au donjon ; sans donjon en 0.1, elle prévient tout le monde 15 s avant le crépuscule. Son d'alerte non géré (pas de son dans l'UI).
- Icônes des compétences : absentes (abréviations « Ép », « Ga », « Ch », « So »).
- Le curseur de la souris n'est ni caché ni verrouillé en jeu (à faire côté jeu, avec la caméra).

## Hors 0.1 (non construit)

Multijoueur (vie des autres joueurs à gauche, « Prêts 2 / 3 »), choix de classe, jauge de classe (Paladin n'en a pas), potions, donjon et portail (rappel, butin), catégories de score {à confirmer}, options Affichage / Audio / manette, personnalisation des touches.

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
