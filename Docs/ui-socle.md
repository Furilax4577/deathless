# Socle d'interface (UI Toolkit et manette)

Construit le 25/09/2026 dans `sandbox-ui` et **reporté dans `main` le même jour** (mêmes chemins, mêmes GUID). Règles : `main/Wiki/pages/commandes.md` et `interface.md`. Style : maquettes « Deathless — Menus et HUD » (canevas 1280×720).

Écrans de la version 0.1 construits sur ce socle : voir `ui-v01.md`.

Démo : scène `Assets/Scenes/UISocle.unity`. Captures : `Assets/Screenshots/UISocle_xbox.png`, `UISocle_playstation.png`, `UISocle_clavier.png`, `UISocle_focus.png`, `UISocle_x3.png` (taille ×3).

## Structure des dossiers

| Chemin | Contenu |
|---|---|
| `Assets/UI/Fonts/` | `Fredoka-{Regular,Medium,SemiBold,Bold}-SDF.asset` : FontAsset TextCore, dynamiques, SDFAA, échantillonnage 64 pt, marge 6, atlas 1024², plusieurs atlas permis. ASCII, lettres accentuées françaises, `« » ‘ ’ “ ” – — × · … € ° ²` préchargés. Source : `Assets/Art/Fonts/Fredoka/*.ttf`. |
| `Assets/UI/Theme/DeathlessTheme.tss` | Thème : `unity-theme://default` puis `Deathless.uss`. |
| `Assets/UI/Theme/Deathless.uss` | Jetons (variables) et styles de base. |
| `Assets/UI/Theme/DeathlessTextSettings.asset` | PanelTextSettings : police par défaut Fredoka Regular. |
| `Assets/UI/PanelSettings/DeathlessPanel.asset` | Échelle selon l'écran, référence 1920×1080, correspondance largeur/hauteur 0,5, thème et réglages de texte ci-dessus. |
| `Assets/UI/Resources/DeathlessInputGlyphs.asset` | Table des icônes (`InputGlyphs`), chargée par `Resources`. Référence `DeathlessControls`. |
| `Assets/UI/Screens/<Écran>/` | Un dossier par écran : `.uxml` et `.uss` propres à l'écran (ex. `UISocle/`). |
| `Assets/Input/DeathlessControls.inputactions` | Actions (cartes Gameplay et UI, schémas Gamepad et KeyboardMouse) et classe générée `DeathlessControls.cs`. |
| `Assets/Scripts/UI/` | `InputDeviceWatcher`, `InputGlyphs`, `InputPrompt`, `Gauge`, `UIScale`, `UINavigation`, `IconesUI` (espace de noms `Deathless.UI`). |
| `Assets/UI/Icones/`, `Assets/UI/Resources/DeathlessIcones.asset` | Icônes vectorielles (SVG → VectorImage) et leur table : voir « Icônes vectorielles ». |
| `Assets/Scripts/Input/InputChordResolver.cs` | Résolveur de l'accord de la manette (LB + RB), espace de noms `Deathless.Controls`. |
| `Assets/Scripts/UI/Dev/UISocleDemo.cs` | Script de la scène de démonstration. |
| `Assets/Editor/UI/DeathlessUISetup.cs` | Menu **Deathless > UI** : régénère polices, PanelSettings, table d'icônes et scène de démo (rejouable, garde les GUID). |

## Jetons (`Deathless.uss`, `:root`)

Les valeurs des maquettes (1280×720) sont multipliées par **1,5** (référence 1920×1080).

| Jeton | Valeur | Usage |
|---|---|---|
| `--dl-color-ink` | `#161a24` | fond |
| `--dl-color-well` | `#10131b` | creux des jauges |
| `--dl-color-panel` / `--dl-color-panel-95` | `#232a3a` / 95 % | panneau (95 % posé sur la scène) |
| `--dl-color-panel-active` | `#2b3348` | panneau actif, fond du focus |
| `--dl-color-border` / `--dl-color-border-strong` | `#3d4660` / `#56607c` | bordure, bordure au survol et des champs |
| `--dl-color-line` | `#333b52` | ligne fine (tables) |
| `--dl-color-text` / `--dl-color-text-muted` / `--dl-color-text-disabled` | `#f4ecd8` / `#c3bca9` / `#8a8578` | ivoire, secondaire, désactivé |
| `--dl-color-gold` | `#d9b264` | focus, sélection, libellés de section |
| `--dl-color-key` / `--dl-color-key-text` / `--dl-color-key-edge` | `#f4ecd8` / `#1f2433` / `#9a8f78` | touche dessinée |
| `--dl-color-life`, `-stamina`, `-mana`, `-rage`, `-nyxessa`, `-shield`, `-shield-warn` | `#e0483e`, `#f2c14e`, `#4a8fe0`, `#f07b2a`, `#3fae5a`, `#4a8fe0`, `#ff9a3c` | jauges |
| `--dl-radius-sm` / `-md` / `-lg` | 9 / 15 / 21 px | touches / boutons / panneaux (6, 10, 14 px maquette) |
| `--dl-radius-pill` | 999 px | ronds parfaits seulement (voir pièges) |
| `--dl-border` / `--dl-border-focus` | 2 / 3 px | bordure / bordure or du focus |
| `--dl-text-caption`, `-small`, `-body`, `-large`, `-heading`, `-title`, `-display` | 18, 20, 24, 30, 36, 51, 102 px | 12, 13-14, 16, 20, 24, 34, 68 px maquette |
| `--dl-space-1` à `--dl-space-6` | 6, 12, 18, 24, 36, 60 px | espacements |
| `--dl-control-height` / `--dl-menu-item-height` / `--dl-bar-height` | 60 / 87 / 84 px | bouton et champ / entrée de menu / barre d'invites |
| classes `dl-scale-1`, `dl-scale-2`, `dl-scale-3` | – | posées sur la racine de chaque écran selon la taille de l'interface (`UIScale`) |
| `--dl-prompt-size` / `--dl-key-height` / `--dl-key-text` | 48 / 34 / 18 px | boîte d'icône d'invite / touche dessinée |

## Classes USS

Les contrôles Unity sont restylés globalement : un `Button`, `Toggle`, `Slider`, `DropdownField` (et son menu) ou `TextField` a l'allure du jeu sans classe.

| Classe | Rôle |
|---|---|
| `dl-screen` | racine d'un écran (fond `ink`, pleine taille) |
| `dl-panel` (+ `--solid`, `--active`, `--scrim`) | panneau arrondi, bordure fine |
| `dl-display`, `dl-title`, `dl-heading`, `dl-subtitle` | DEATHLESS (Bold), titre (SemiBold), intertitre, sous-titre or |
| `dl-section-label` | libellé de section or, espacé ; **texte à écrire en capitales** (USS n'a pas de `text-transform`) |
| `dl-text` (+ `--muted`, `--small`, `--large`), `dl-separator` | texte courant, ligne fine |
| `dl-font-regular`, `-medium`, `-semibold`, `-bold` | graisse de Fredoka ; `-unity-font-style: bold` donne aussi Fredoka Bold |
| Button : `:hover`, `:focus`, `:active`, `:disabled` | survol bordure claire ; **focus manette : bordure or 3 px + fond `#2b3348`** ; pressé : texte or ; désactivé : texte grisé |
| `dl-button--selected` | bouton choisi dans un groupe (×2) |
| `dl-menu-item` (+ `__texts`, `__label`, `__desc`, `__prompt`) | entrée de menu principal ou de pause ; l'invite `__prompt` n'apparaît qu'au focus |
| `dl-tabs`, `dl-tab`, `dl-tab--selected` | barre d'onglets en pastilles |
| `dl-field` | ligne de réglage (Toggle, Slider, DropdownField, TextField) : le focus surligne toute la ligne |
| `dl-field--stacked` | libellé au-dessus du champ |
| `dl-gauge` (+ `--life`, `--stamina`, `--mana`, `--rage`, `--nyxessa`, `--shield`, `--shield-warn`, `--thin`) | jauge (`Deathless.UI.Gauge`) |
| `dl-pill` (+ `--gold`, `__icon`) | pastille |
| `dl-prompt` (+ `__icons`, `__icon`, `__key`, `__plus`, `__label`, `--small`, `--missing`, `--joined`) | invite de bouton (`Deathless.UI.InputPrompt`) |
| `dl-prompt-bar`, `dl-prompt-bar__group` | barre d'invites en bas d'écran |
| `dl-table-header`, `dl-table-row`, `dl-table-cell` | tables |

## Ajouter un écran

1. Créer `Assets/UI/Screens/MonEcran/MonEcran.uxml` (et un `.uss` pour sa seule mise en page). Racine : `<ui:VisualElement class="dl-screen">`. Déclarer `xmlns:dl="Deathless.UI"` pour les éléments du socle.
2. Dans la scène : un GameObject avec `UIDocument` (PanelSettings **DeathlessPanel**, source MonEcran.uxml). Une seule `EventSystem` + `InputSystemUIInputModule` par scène, branchée sur la carte **UI** de DeathlessControls (`DeathlessUISetup.AssignUIModule(module, actions)` depuis l'éditeur, ou copier l'objet `EventSystem` de `UISocle.unity`). Un `UIScale` (panneau DeathlessPanel) par scène ou sur un objet persistant.
3. Dans le script de l'écran, à l'activation :
   - `UINavigation.SetupScreen(root)`, qui fait deux choses. D'abord la sélection du panneau dans l'EventSystem à chaque focus : sans elle, un `Focus()` fait par le code laisse la manette inerte ; le ScrollView parent défile aussi jusqu'à l'élément focus. Ensuite, la sortie des TextField, qui sinon gardent la navigation pour leur curseur ;
   - `UIScale.TagRoot(root)` (et à chaque `UIScale.Changed`) pour recevoir la classe `dl-scale-N` ;
   - table ou liste de lignes focusables dans un ScrollView : `UINavigation.ChainerVerticalement(lignes)` (la navigation spatiale ne passe pas toujours d'une ligne à la suivante) ;
   - focus initial : `UINavigation.Focus(premierBouton)` (idéalement après un court délai, `schedule.Execute(...).StartingIn(50)`) ;
   - au passage à la manette sans focus (`InputDeviceWatcher.Changed`), redonner le focus au premier bouton.
4. Actions propres à l'écran (onglets, réinitialiser…) : `actions.FindAction("UI/TabNext").performed += …` et activer la carte UI.
5. Vérifier l'écran aux trois tailles (règle ci-dessous).

### Règle des écrans denses (taille ×3)

Tailles : ×1 = 0,8, ×2 = 1 (taille des maquettes, défaut), ×3 = 1,35 (décision du 25/09/2026, confort de lecture sur télévision). À ×3, un écran 16:9 n'offre plus qu'environ **1422 × 800 unités logiques** (1920 × 1080 / 1,35).

- Concevoir l'écran pour ×2 (1920 × 1080), en le laissant **tenir sans défilement à ×1 et ×2**.
- Mettre le contenu principal dans un **`ScrollView` vertical** (barre d'invites et en-tête hors du ScrollView) : il ne défile que si c'est nécessaire, et `UINavigation.SetupScreen` le fait suivre le focus manette. Pour qu'à ×2 les colonnes remplissent la hauteur, donner au conteneur `min-height: 100%` (`.x > .unity-scroll-view__content-viewport > .unity-scroll-view__content-container`).
- Sous `.dl-scale-3`, **réorganiser** plutôt que rétrécir : passer de trois colonnes à deux (`flex-wrap: wrap`, la dernière colonne en `width: 100%` en dessous), réduire les titres d'affichage, serrer la barre d'invites. Ne jamais réduire la taille du texte courant à ×3 (c'est le but du réglage).
- Exemple : `Assets/UI/Screens/UISocle/UISocle.uss` (fin du fichier) et la capture `UISocle_x3.png`.

## Afficher une invite de bouton

UXML :

```xml
<dl:InputPrompt action="UI/Submit" label="Valider" />
<dl:InputPrompt action="Gameplay/Skill3" />            <!-- LB + RB, L1 + R1 ou F selon l'appareil -->
<dl:InputPrompt action="Jump" family="PlayStation" />  <!-- famille forcée (ex. colonne d'une table des commandes) -->
```

C# : `parent.Add(new InputPrompt("UI/Cancel", "Retour"));`

- `action` : « Carte/Action » ou nom seul, cherché dans `InputGlyphs.Default.actions` (DeathlessControls).
- L'invite choisit la liaison du schéma de la famille active (KeyboardMouse ou Gamepad), en préférant un chemin propre à la famille (`<DualShockGamepad>/touchpadButton` sur PlayStation) au chemin générique `<Gamepad>/…`.
- Combinaison (composite `ButtonWithOneModifier`) : deux icônes et « + ». Composite 2D : quatre touches (Z Q S D) ou une icône « flèches ».
- Lettres du clavier : icône de la lettre **affichée par la disposition active** (`GetBindingDisplayString`), donc `<Keyboard>/q` montre « A » en AZERTY. Chiffres, F1-F12 et touches spéciales : icône par chemin.
- Touches dont l'icône Kenney porte un mot anglais (ESC, DEL, CTRL, ALT, HOME…) : volontairement retirées de la table, l'invite les **dessine en français** (Échap, Suppr, Ctrl, Alt, Début…) dans une pastille ivoire. Même repli pour toute touche sans icône.
- Mise à jour automatique à chaque `InputDeviceWatcher.Changed`. Hors mode Play (UI Builder), l'aperçu est en Xbox.

## Accord de la manette (LB + RB)

Décision du 25/09/2026 : **court délai**. `Deathless.Controls.InputChordResolver` (C#, sans composant) :

- Au premier appui sur LB (ou RB), l'action seule attend `chordWindow` = **0,1 s** (`InputChordResolver.DefaultChordWindow`, à équilibrer ; dans la démo : champ `chordWindow` de `UISocleDemo`).
- Si l'autre bouton arrive dans ce délai, **Compétence 3** part, et ni la 1 ni la 2. Sinon **Compétence 1** (ou 2) part à la fin du délai, même si le bouton a déjà été relâché. L'ordre des deux boutons est libre.
- **Pas d'accord L3 + R3** (décision de Quentin, 25/09/2026 : aucune classe n'a d'ultime, personne ne s'accroupit ; actions `Ultimate` et `Crouch` retirées). L3 est Sprinter seul, relayé sans délai ; R3 est libre.
- Au clavier (A, R, F), tout part immédiatement.
- Le délai est mesuré sur l'horodatage des événements (`CallbackContext.time`) et vérifié après chaque mise à jour de l'Input System (`InputSystem.onAfterUpdate`) : le résultat ne dépend pas de la fréquence d'images.

Utilisation (code de jeu) :

```csharp
var controls = new DeathlessControls();          // ou l'asset DeathlessControls
var chords = InputChordResolver.ForGameplay(controls.asset);
chords.Triggered += action => { /* action résolue : Skill1, Skill3, Jump… */ };
controls.Gameplay.Enable();
// Sprinter maintenu : chords.IsHeld(controls.Gameplay.Sprint)
// À la destruction : chords.Dispose();
```

`Triggered` relaie aussi, sans délai, toutes les autres actions « Button » de la carte Gameplay : c'est le seul point d'entrée à utiliser. S'abonner directement à `Skill1.performed` contournerait la résolution. Le composite LB + RB reste dans l'asset pour les invites et le clavier ; à la manette, le résolveur l'ignore et détecte l'accord lui-même.

Vérifié en Play (événements simulés et horodatés sur une manette XInput ; résultat affiché dans la démo, bloc « Dernière action en jeu ») :

| Séquence | Résultat |
|---|---|
| LB seul | Compétence 1 |
| RB seul | Compétence 2 |
| LB puis RB à 50 ms | Compétence 3 |
| LB puis RB à 200 ms | Compétence 1, puis Compétence 2 |
| RB puis LB à 50 ms | Compétence 3 |
| L3 seul | Sprinter (sans délai depuis le retrait de L3 + R3) |

### Appareil actif

`InputDeviceWatcher` (statique, installé au lancement par `RuntimeInitializeOnLoadMethod`) écoute `InputSystem.onEvent` ; un événement d'état ne compte que si un contrôle change d'au moins 0,35 (pas de bascule sur la dérive d'un stick), **ou**, pour une manette ou le clavier, si l'événement porte un bouton enfoncé (au-delà de son point d'appui) ou un stick poussé au-delà de 0,35 (correctif du 25/09/2026 : la comparaison avec l'état courant ne voyait plus l'appui quand cet état était déjà à jour, et le premier A après la souris ne ramenait pas la manette ni ses invites ; vérifié : trois fois de suite, un seul appui A après la souris repasse sur Xbox et valide le bouton sous le focus). Classement : `Keyboard`/`Mouse` → KeyboardMouse ; `DualShockGamepad` (DualShock 4, DualSense) ou fabricant Sony → PlayStation ; `XInputController` et **toute autre manette** → Xbox. `Current`, `CurrentDevice`, `CurrentControlScheme`, `Changed`, `SetCurrent()` (forçage).

### Icônes de boutons

`InputGlyphs` (ScriptableObject) : trois listes `contrôle → Texture2D` (chemin sans appareil, en minuscules : `buttonsouth`, `dpad/up`, `leftshoulder`, `space`, `f1`, `leftbutton`…) et une icône d'appareil par famille. Remplie par **Deathless > UI > 3. Table des icônes** depuis `Assets/Art/UI/KenneyInputPrompts/*/Double/`. Choix : boutons de façade en couleur (`xbox_button_color_*`, `playstation_button_color_*`), gâchettes et épaules blanches, PlayStation en icônes PS5 (`playstation5_button_options`, `playstation5_touchpad_press`), clavier en icônes pleines (`keyboard_*`, variantes à symbole pour Espace, Tab, Maj, Retour arrière), souris `mouse_left`, `mouse_right`, `mouse_move`.

## Icônes vectorielles (classes, compétences, HUD)

Sources : `ArtSources/Icones/` (SVG générés par `generer_icones.py`, conventions et table icône → bouton dans `LISEZMOI.md`, planche `Docs/icones/planche.html`). Copies dans le projet : `Assets/UI/Icones/Classes/` (emblèmes hexagonaux `classe_*`, sans les variantes `classe_druide_a/b/c`) et `Assets/UI/Icones/Competences/` (actions, jauges, `commun_*` ; pas les icônes provisoires du druide).

- **Import** : module Vector Graphics intégré à Unity 6 (`com.unity.modules.vectorgraphics`), type **VectorImage** (UI Toolkit), **PreserveViewport** : chaque icône garde le cadre 128 × 128 de son SVG, donc toutes ont la même échelle et restent nettes à ×1, ×2 et ×3. Réglages posés automatiquement à l'import de tout SVG du dossier (`Assets/Editor/UI/IconesUIOutil.cs`, `OnPreprocessAsset`). Rendu vérifié de 33 à 74 px à l’écran (liste des actions, HUD ×1 à ×3) : facettes nettes, pas de flou. Pas besoin de PNG rastérisés.
- **Table** : `Assets/UI/Resources/DeathlessIcones.asset` (`Deathless.UI.IconesUI`) : identifiant (nom du SVG sans extension) → VectorImage. Reconstruite toute seule quand un SVG est ajouté, supprimé ou déplacé dans `Assets/UI/Icones/`, ou par **Deathless > UI > 6. Table des icônes (SVG)**. API : `IconesUI.Trouver(id)`, `IconesUI.Poser(element, id)` (fond de l'élément, masqué si l'icône manque), `IconesUI.Creer(id, classeUss)` ; classe USS `dl-icone` (image ajustée à la boîte, sans déformation). Constantes des icônes du HUD : `IconesUI.Mana`, `Rage`, `Furtif`, `Potion`, `Esquive`, `CoupCritique`.
- **Liens action → icône** : dans les données, jamais dans le code des écrans. Actions des classes : `IActionClasse.Icone` et emblème `IClasseJouable.Embleme` (catalogue `ClassesJouables.Catalogue`, `Donnees/IClasses.cs`), lus par le HUD (`ClassesJouables.IconeAction(classe, action)`), l'écran de choix et la carte du menu principal.
- **Ajouter une icône** : générer le SVG dans `ArtSources/Icones/` ; il est copié tout seul dans `Assets/UI/Icones/Classes/` ou `Competences/` au chargement de l'éditeur ou par le menu 6 (`IconesUIOutil.Synchroniser` : SVG nouveaux ou modifiés ; ni les variantes `classe_x_a/b/c`, ni les icônes provisoires `druide_*`) ; le nom devient l'identifiant. Renseigner ensuite cet identifiant dans la donnée qui l'utilise (`ClassesJouables.Catalogue` pour une action ou un emblème, constante `IconesUI` pour un élément du HUD).
- **Emblème manquant** : un identifiant `classe_*` sans SVG prend l'hexagone vide `repli_classe` (`Assets/UI/Icones/Repli/`, écrit à la main) jusqu'à l'arrivée du SVG (`IconesUI.RepliClasse`).

## Actions et liaisons

Clavier : l'Input System lie des **positions physiques** (disposition US). La table du wiki est en AZERTY ; on lie la position physique de la touche AZERTY, et l'affichage suit la disposition du joueur.

### Carte Gameplay

| Action | Manette (`<Gamepad>/…`) | Clavier-souris (chemin physique) | Affiché en AZERTY |
|---|---|---|---|
| Move | `leftStick` | composite 2DVector `w` / `a` / `s` / `d` | Z Q S D |
| Look | `rightStick` | `<Mouse>/delta` | souris |
| Jump | `buttonSouth` (A, Croix) | `space` | Espace |
| Dodge | `buttonEast` (B, Rond) | `leftCtrl` | Ctrl |
| Interact | `buttonWest` (X, Carré) | `e` | E |
| CharacterMenu | `buttonNorth` (Y, Triangle) | `tab` | Tab |
| AttackPrimary | `rightTrigger` (RT, R2) | `<Mouse>/leftButton` | clic gauche |
| AttackSecondary | `leftTrigger` (LT, L2) | `<Mouse>/rightButton` | clic droit |
| Skill1 | `leftShoulder` (LB, L1) | `q` | **A** |
| Skill2 | `rightShoulder` (RB, R1) | `r` | R |
| Skill3 | ButtonWithOneModifier `leftShoulder` + `rightShoulder`, ordre libre | `f` | F |
| Sprint | `leftStickPress` (L3) | `leftShift` | Maj |
| DrinkPotion | `dpad/up` | `1` (rangée des chiffres) | 1 (icône ; la touche porte « & » en AZERTY) |
| Ready | `<DualShockGamepad>/touchpadButton` (pavé tactile) et `select` (Vue ; Create sur DualSense, voulu) | `f1` | F1 |
| Pause | `start` (Menu, Options) | `escape` | Échap |

### Carte UI

| Action | Manette | Clavier-souris | Affiché en AZERTY |
|---|---|---|---|
| Navigate | `leftStick`, `dpad` | composite 2DVector flèches | flèches |
| Submit | `buttonSouth` | `enter`, `numpadEnter` (+ clic via Click) | Entrée |
| Cancel | `buttonEast` | `escape` | Échap |
| TabPrevious | `leftShoulder` | `a` | **Q** |
| TabNext | `rightShoulder` | `e` | E |
| Reset | `buttonNorth` | `delete` | Suppr |
| Pause | `start` | `escape` | Échap |
| Point, Click, RightClick, MiddleClick, ScrollWheel | – | `<Mouse>/position`, `leftButton`, `rightButton`, `middleButton`, `scroll` | (pour l'EventSystem) |

Schémas : **Gamepad** (`<Gamepad>`) et **KeyboardMouse** (`<Keyboard>` + `<Mouse>`). Le module `InputSystemUIInputModule` de l'EventSystem utilise Point, Click, RightClick, MiddleClick, ScrollWheel, Navigate, Submit et Cancel de la carte UI.

## Pièges UI Toolkit rencontrés

- **Rayon 999 px** : sur un élément plus large que haut, UI Toolkit réduit le rayon et déforme la forme en lentille. Pour une pastille ou une barre, rayon = moitié de la hauteur (jauges 11/8 px, onglets 30 px, pastilles 24 px).
- **Spécificité du thème par défaut** : ses règles d'état (focus, survol, case cochée) passent devant un simple `.unity-button:focus`. Écrire `:focus:enabled`, `:hover:enabled`, et `.unity-toggle > .unity-toggle__input > .unity-toggle__checkmark`.
- `:first-child` et `text-transform` n'existent pas en USS.
- **EventSystem** : la navigation manette n'est envoyée qu'à l'objet sélectionné (`UINavigation.SyncEventSystemSelection`).
- **TextField** : garde la navigation (`UINavigation.ReleaseTextFields`).
- Les espaces insécables `U+00A0` et `U+202F` n'existent pas dans Fredoka : éviter la typographie française à espace fine (`U+202F`) dans les textes.
- Capture : rendre le PanelSettings dans une RenderTexture (`targetTexture`, sRGB) recrée le panneau : le focus est perdu et doit être rendu.
- Simulation d'appareils en Play (éditeur sans focus) : `InputSystem.AddDevice<XInputController>()` / `DualSenseGamepadHID`, puis `QueueStateEvent` / `StateEvent.From` + `WriteValueIntoEvent` **sans** appeler `InputSystem.Update()` (l'événement est traité à l'image suivante). `PlayerSettings.runInBackground` a été activé dans ce bac à sable.

## Décisions prises (25/09/2026)

- Accord de la manette : court délai (section « Accord de la manette »).
- **Pas d'ultime ni d'accroupissement** : actions `Ultimate` (L3 + R3, G) et `Crouch` (R3, C) retirées de DeathlessControls et du résolveur ; R3, G et C sont libres.
- **DualSense : le bouton Create déclare aussi « prêt »**, en plus du pavé tactile (liaison `<Gamepad>/select` gardée volontairement ; l'invite affiche le pavé tactile).
- Taille de l'interface : ×1 = 0,8, ×2 = 1, ×3 = 1,35.
- **DeathlessControls est l'asset d'actions du projet** (Project Settings > Input System > Project-wide Actions) dans main ; `Assets/InputSystem_Actions.inputactions` du gabarit a été retiré de main (aucune référence). Le bac à sable garde l'ancien réglage.

## À trancher

- Délai d'accord de 0,1 s à équilibrer en jeu (`InputChordResolver.chordWindow`).
- **Touche 1 (potion)** : liée à la position de la touche « 1 » de la rangée des chiffres (« & » sans Maj en AZERTY). L'icône affiche 1.
- Ordre de navigation spatiale d'UI Toolkit : aux bords, le focus reboucle (bas depuis le dernier réglage → onglets). À garder ou à bloquer écran par écran.

## Report dans main

Fait le 25/09/2026 (copie avec les `.meta`, aucune collision de GUID, dépendances identiques vérifiées ; capture `Assets/Screenshots/UISocle_main.png`). Pour un report ultérieur, même liste : copier avec les `.meta` (mêmes GUID). Les polices `Assets/Art/Fonts/Fredoka/` et les icônes `Assets/Art/UI/KenneyInputPrompts/` sont déjà dans main avec les mêmes GUID (vérifié).

| Fichier | Dépend de |
|---|---|
| `Assets/Input/DeathlessControls.inputactions`, `DeathlessControls.cs` | Input System |
| `Assets/Scripts/UI/InputDeviceWatcher.cs` | Input System |
| `Assets/Scripts/UI/InputGlyphs.cs` | InputDeviceWatcher |
| `Assets/Scripts/UI/InputPrompt.cs` | InputGlyphs, InputDeviceWatcher |
| `Assets/Scripts/UI/Gauge.cs`, `UIScale.cs`, `UINavigation.cs` | UINavigation dépend d'InputDeviceWatcher |
| `Assets/Scripts/Input/InputChordResolver.cs` | Input System (utilise la carte Gameplay de DeathlessControls) |
| `Assets/UI/Fonts/Fredoka-*-SDF.asset` (4) | `Assets/Art/Fonts/Fredoka/*.ttf` |
| `Assets/UI/Theme/Deathless.uss`, `DeathlessTheme.tss`, `DeathlessTextSettings.asset` | polices SDF |
| `Assets/UI/PanelSettings/DeathlessPanel.asset` | thème, réglages de texte |
| `Assets/UI/Resources/DeathlessInputGlyphs.asset` | InputGlyphs.cs, icônes Kenney, DeathlessControls |
| `Assets/Editor/UI/DeathlessUISetup.cs` (facultatif, régénération) | tout ce qui précède, UISocleDemo |
| Démo (facultatif) : `Assets/Scenes/UISocle.unity`, `Assets/UI/Screens/UISocle/`, `Assets/Scripts/UI/Dev/UISocleDemo.cs`, `Assets/Screenshots/UISocle_*.png` | tout ce qui précède, InputChordResolver |

Réglages de projet du bac à sable à ne reporter que si voulu : `PlayerSettings.runInBackground = true`, UISocle en tête des scènes du build.
