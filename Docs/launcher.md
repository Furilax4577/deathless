# Launcher de Deathless

Exe Windows pour jouer entre amis sans manipuler de zip. Il vérifie la version publiée sur le serveur de Quentin,
télécharge et installe le jeu si besoin, affiche les notes de version, puis lance le jeu. Le code est dans
`Launcher/`, hors du projet Unity. Il reprend le launcher de Relic (`C:/Dev/Unity/Relic/Launcher`) avec la même
logique, et l'habille comme le menu principal du jeu.

![Launcher prêt](../Launcher/captures/launcher_pret.png)

## Côté joueur

1. Télécharger `http://srv617344.hstgr.cloud/deathless/DeathlessLauncher.zip` et le dézipper dans un dossier à soi,
   par exemple `Documents\Deathless\`. Le zip contient `DeathlessLauncher.exe`, `launcher.json` (déjà réglé sur le
   serveur), `OFL.txt` (licence de la police) et `fond.mp4` (le fond animé). Il n'y a rien à installer et aucun droit
   administrateur n'est demandé.
   .NET Framework 4.8 est déjà présent sur Windows 10 et 11.
2. Lancer `DeathlessLauncher.exe`. Il lit `version.json` et `changelog.json` sur le serveur. Si la version installée
   diffère, il télécharge `deathless-v<N>.zip` dans `%TEMP%\DeathlessLauncher\`, vérifie son empreinte SHA-256,
   l'extrait dans `Game\` à côté de lui, puis active « Jouer ».
3. « Jouer » lance `Game\Deathless.exe` et ferme le launcher.
4. Sans réseau, le launcher propose de lancer la version déjà installée. Il affiche alors les notes de version lues
   lors de la dernière connexion (copie locale `changelog.cache.json`).
5. Si le téléchargement est coupé, « Réessayer » le reprend là où il s'était arrêté.

### Écran

- **Fond** : la scène du menu principal (village de nuit, Nyxessa et le portail), avec le volet gauche assombri. Elle
  est animée en boucle si `fond.mp4` est à côté de l'exe, et fixe sinon (voir « Changer le fond »).
- **Volet gauche** : titre « DEATHLESS » et sous-titre, puis les entrées « Jouer », « Réessayer » (après un échec ou
  hors ligne) et « Quitter ». L'entrée sélectionnée a le fond actif et la bordure or, comme au menu du jeu. Pendant la
  mise à jour, « Jouer » reste sélectionné mais grisé, et sa description donne la version et le temps restant. Quand
  le jeu est prêt, la bordure or s'éclaire brièvement.
- **Jauge** : elle reprend la barre de vie du HUD (`Gauge`, classe `dl-gauge--life`). Le libellé est à gauche, la
  valeur à droite, la piste est arrondie avec son trait, et le remplissage rouge suit la progression en douceur. Au
  téléchargement, la valeur affiche les Mo téléchargés sur le total et le débit. Pendant la recherche de mise à
  jour, la vérification et l'installation, le remplissage est masqué. À sa place, une rangée de petites gemmes or,
  une toutes les 22 px, grossissent puis rétrécissent en une vague qui avance de gauche à droite (1,4 s par passage).
  Elles suivent le langage visuel du jeu : low poly, bords francs, couleurs pleines, sans dégradé ni transparence,
  et elles apparaissent et disparaissent par la taille. Les erreurs s'affichent sous la jauge, en rouge clair. Le
  bouton de la barre des tâches montre aussi l'avancement.
- **Notes de version** : panneau `dl-panel` à droite, de la version la plus récente à la plus ancienne. Chaque
  version a son numéro, sa date et ses notes en puces. La pastille « Installée » marque la version installée, et
  « Nouvelle » la version publiée qui n'est pas encore installée. La liste défile en douceur, avec un fondu en haut
  et en bas quand il reste du texte caché.
- **Barre d'invites** en bas : Valider, Choisir, Notes de version. L'appareil actif est affiché à droite. Les icônes
  Kenney suivent le dernier appareil utilisé, comme dans le jeu.

### Commandes

| Action | Clavier et souris | Manette Xbox |
|---|---|---|
| Valider l'entrée sélectionnée | Entrée, Espace, clic | A |
| Entrée précédente ou suivante | Flèches haut et bas, Tab, survol | Croix, stick gauche |
| Défiler les notes | Molette, Page préc. et Page suiv., Début et Fin | Stick droit, LB et RB |

La manette est lue par XInput (`xinput1_4.dll`, avec repli sur `xinput9_1_0.dll`, sans rien installer), seulement
quand le launcher est au premier plan. Les manettes PlayStation ne passent pas par XInput : avec elles, il faut le
clavier et la souris.

## Côté serveur (en place depuis le 25/09/2026)

Le serveur est le VPS de Quentin (`srv617344.hstgr.cloud`, déjà utilisé par Relic : Ubuntu, Nginx, accès SSH
`root` par clé, jamais de mot de passe dans le dépôt ni dans une conversation). Quentin a lancé le script
`Deathless-Workspace/tools/serveur-deathless.sh` le 25/09/2026, qui a :

1. sauvegardé la configuration Nginx (`/root/nginx-relic.bak-<date>`) ;
2. créé `/var/www/deathless` (propriétaire `www-data`) et `/root/deathless-staging/current` pour `publish-diff.ps1` ;
3. ajouté au site `relic` (`/etc/nginx/sites-available/relic`) un bloc `location /deathless/`, servi à l'adresse
   `http://srv617344.hstgr.cloud/deathless/`, avec `Cache-Control: no-store` sur `version.json` et `changelog.json` ;
4. déposé un `changelog.json` vide (`{ "versions": [] }`), vérifié en ligne le même jour.

Nginx accepte les requêtes `Range` par défaut : la reprise des téléchargements fonctionne sans réglage. Reste à faire :
la première publication, avec le launcher (`publish.ps1 -AvecLauncher`, qui envoie aussi `DeathlessLauncher.zip`).

Le VPS expire le 10 octobre 2026 : il faudra le prolonger ou changer `baseUrl`.

Contenu du dossier publié :

- `version.json` : la version à installer (format ci-dessous).
- `changelog.json` : les notes de toutes les versions (format ci-dessous).
- `deathless-v<N>.zip` : le build Windows, avec son contenu à la racine (`Deathless.exe`, `Deathless_Data\…`). Les
  anciens zips peuvent rester ou être supprimés : seul celui de `version.json` compte.
- `DeathlessLauncher.zip` : le launcher à distribuer.

Le launcher relit `version.json` et `changelog.json` à chaque démarrage, avec un paramètre anti-cache dans l'URL.

### version.json

```json
{ "version": "1", "nom": "0.1", "zip": "deathless-v1.zip", "sha256": "…", "size": 123456789, "notes": "…" }
```

- `version` : numéro de publication (1, 2, 3…). Il nomme le zip et décide de la mise à jour : le jeu est retéléchargé
  dès que `version` ou `sha256` diffère de `Game\installed.json`.
- `nom` (facultatif) : la version du jeu montrée aux joueurs (« 0.1 »). Sans `nom`, le launcher affiche `version`.
- `sha256`, `size` : empreinte et taille du zip, calculées par les scripts de publication. L'empreinte est
  obligatoire : un téléchargement coupé ou abîmé n'est jamais installé.
- `notes` (facultatif) : texte libre, une puce par ligne. Il sert de repli quand `changelog.json` manque, ou quand
  la version publiée n'y a pas d'entrée. C'est le seul format que connaissait le launcher de Relic.

### changelog.json

Modèle : `Launcher/changelog.json`, la source tenue dans le dépôt, avec la version 0.1.

```json
{
  "versions": [
    {
      "version": "0.1",
      "date": "2026-09-25",
      "titre": "Première défense du village",
      "notes": [
        "Partie solo au village avec le Paladin : épée et bouclier, garde et parade, charge bélier, soin sur soi.",
        "Cycle jour-nuit : on se prépare le jour, on défend Nyxessa la nuit."
      ]
    }
  ]
}
```

- `version` : le nom affiché, le même que `nom` dans `version.json`. C'est par lui que le launcher pose les pastilles
  « Installée » et « Nouvelle ».
- `date` : au format `AAAA-MM-JJ`. Elle est affichée en français (« 25 septembre 2026 »).
- `titre` (facultatif) : une ligne en or sous le numéro.
- `notes` : une liste de lignes, chacune devient une puce. Un seul texte est aussi accepté : chaque ligne devient
  alors une puce, et les tirets « - » en début de ligne sont retirés.
- Ordre : peu importe dans le fichier. Le launcher trie par date décroissante (les versions sans date passent
  après), puis par numéro de version décroissant. La version publiée, si elle n'a pas d'entrée, est ajoutée en tête
  avec les `notes` de `version.json`.
- Encodage : UTF-8. Un `changelog.json` illisible est ignoré (le launcher se rabat sur `notes`) et la copie locale
  n'est pas écrasée.

Pour ajouter une version, ajouter une entrée à `Launcher/changelog.json` avant de publier : les deux scripts de
publication vérifient le fichier et l'envoient.

### launcher.json

Il est posé à côté de l'exe :

```json
{ "baseUrl": "http://srv617344.hstgr.cloud/deathless/", "gameExe": "Deathless.exe" }
```

## Côté Quentin : publier une version

Prérequis, une seule fois : une clé SSH (`ssh-keygen -t ed25519`) dont la clé publique est déposée sur le serveur
(`~/.ssh/authorized_keys`), pour que `scp` fonctionne sans mot de passe.

1. Faire le build Windows depuis Unity (File > Build), par exemple dans `Builds\Deathless-v1-2026-09-25\` (dossier
   ignoré par git).
2. Mettre à jour `Launcher\changelog.json`.
3. Depuis la racine du dépôt (`main`) :

```powershell
.\Launcher\publish.ps1 -BuildDir "Builds\Deathless-v1-2026-09-25" -Version 1 -Nom "0.1" -AvecLauncher `
    -Remote "root@srv617344.hstgr.cloud:/var/www/deathless/" -BaseUrl "http://srv617344.hstgr.cloud/deathless/"
```

Le script zippe le build (sans les `.log` ni le dossier `*_DoNotShip`), calcule l'empreinte, écrit `version.json`,
vérifie et copie `changelog.json`, et, avec `-AvecLauncher`, zippe le launcher construit. Il envoie ensuite le tout
par `scp`, `version.json` en dernier, puis relit `version.json` et `changelog.json` en ligne pour confirmer. Sans
`-Remote`, rien ne part : les fichiers restent dans `Builds\publish\` pour contrôle.

**Connexion lente** : `publish-diff.ps1` n'envoie que les différences, par rsync lancé dans WSL, vers
`/root/deathless-staging/current/`. Le serveur refait ensuite le zip, `changelog.json` et `version.json` avec
`Launcher/make_release.py`. Le launcher ne voit aucune différence.

```powershell
.\Launcher\publish-diff.ps1 -BuildDir "Builds\Deathless-v2-2026-10-02" -Version 2 -Nom "0.2"
```

`make_release.py` peut aussi servir en local pour fabriquer une publication de test :
`python Launcher/make_release.py <build> <dossier_web> <version> <notes> --nom 0.1 --changelog Launcher/changelog.json`.

## Construire le launcher

```powershell
dotnet build .\Launcher\DeathlessLauncher.csproj -c Release
```

L'exe est dans `Launcher\bin\Release\net48\DeathlessLauncher.exe`, avec `launcher.json`, `OFL.txt`,
`DeathlessLauncher.exe.config` et `fond.mp4` (copie de `Assets/Screenshots/menu_nuit_boucle.mp4`, quand elle existe).
Ces fichiers forment le launcher à distribuer, que `publish.ps1 -AvecLauncher` zippe. Le script laisse de côté ce
qu'un lancement depuis `bin\` a pu y ajouter (`changelog.cache.json`, `Game\`), et reprend toujours `fond.mp4` du
dépôt. Le SDK `dotnet` récent suffit : les assemblies de référence .NET Framework 4.8 viennent du paquet NuGet
`Microsoft.NETFramework.ReferenceAssemblies`, utilisé à la compilation seulement.

Tout est compilé dans l'exe (environ 1,6 Mo) :

- la police Fredoka (4 graisses), lue dans `Assets/Art/Fonts/Fredoka/` ;
- les invites Kenney (Entrée, flèches, molette ; A, croix, stick droit), lues dans
  `Assets/Art/UI/KenneyInputPrompts/*/Double/` ;
- l'image de fond (voir ci-dessous) ;
- l'icône, `Launcher/Ressources/deathless.ico` : la gemme de Nyxessa du HUD, régénérée par
  `python Launcher/make_icon.py`.

Le launcher ne se met pas à jour lui-même : une nouvelle version se distribue à la main, en republiant
`DeathlessLauncher.zip`.

## Changer le fond

### Fond animé (vidéo)

Au démarrage, le launcher cherche `fond.mp4` à côté de `DeathlessLauncher.exe` et le lit en boucle, muet, par-dessus
l'image fixe. La vidéo n'est jamais compilée dans l'exe.

- **Source** : `Assets/Screenshots/menu_nuit_boucle.mp4`, la scène du menu de nuit enregistrée dans Unity (H.264,
  1920 × 1080, 30 images/s, environ 12 s). Le raccord doit être dans la vidéo elle-même : la dernière image doit
  enchaîner sur la première. Le build la copie en `bin\Release\net48\fond.mp4`, et `publish.ps1 -AvecLauncher` la
  met dans `DeathlessLauncher.zip` sous ce nom. Pour la changer, il suffit de remplacer ce fichier, puis de
  reconstruire ou de republier le launcher. Pour essayer une vidéo sans rien reconstruire, on pose un `fond.mp4` à
  côté de l'exe.
- **Durée conseillée** : un nombre entier de secondes (12 s = 360 images). Le lecteur de Windows tronque la durée à
  la seconde. Pour une vidéo de 12,000 s, l'échange des lecteurs se fait 5 ms avant la fin, par la position. Pour une
  durée non entière, il se fait sur l'événement de fin, avec au pire une fraction d'image de retard au raccord.
- **Boucle sans saccade** : un lecteur en boucle simple marque un à-coup au retour à zéro (arrêt, recherche de
  l'image 0, redémarrage). `Launcher/FondVideo.cs` utilise donc deux lecteurs sur la même vidéo. Pendant que l'un
  joue, l'autre est caché, déjà ouvert et posé sur l'image 0. À la fin, on démarre l'autre et on échange leur
  visibilité dans la même image de rendu, sans fondu. Le lecteur qui vient de finir est remis sur l'image 0 et
  attend le tour suivant.
- **Repli** : si la vidéo est absente, illisible, trop longue à s'ouvrir (10 s), ou si Windows ne sait pas la lire
  (Windows N sans le Media Feature Pack), le fond vidéo se retire et l'image fixe reste. Quand la fenêtre est
  réduite, la vidéo est mise en pause.
- Le mode `--capture` garde toujours l'image fixe.

### Image fixe

Elle sert de repli et de fond pendant l'ouverture de la vidéo. Il y a trois façons de la changer, de la plus simple
à la plus durable :

1. **Sans recompiler** : poser un `fond.png` (ou `fond.jpg`) à côté de `DeathlessLauncher.exe`. Il remplace l'image
   compilée. C'est pratique pour essayer une image.
2. **Image par défaut** : le projet compile `Assets/Screenshots/menu_nuit_fond.png`, la scène du menu principal sans
   interface, produite dans Unity. Il suffit de remplacer ce fichier et de reconstruire. S'il est absent, le projet
   prend la capture provisoire `Assets/Screenshots/v01_nuit_vague.png`.
3. **Autre fichier** : `dotnet build .\Launcher\DeathlessLauncher.csproj -c Release -p:FondLauncher=chemin\image.png`,
   ou changer la propriété `FondLauncher` dans le `.csproj`.

L'image est étirée pour couvrir la fenêtre sans être déformée (les bords sont rognés), et centrée. Nyxessa doit donc
se trouver vers le centre de l'image, entre le volet gauche (42 % de la largeur) et le panneau des notes (le tiers
droit). Le format conseillé est 16:9, 1920 × 1080 ou plus.

## Outils de développement (options cachées, sans fenêtre)

- `DeathlessLauncher.exe --capture <fichier.png> [--etat accueil|telechargement|verification|pret|horsligne|erreur]
  [--manette] [--changelog Launcher\changelog.json] [--largeur 1280 --hauteur 720]` rend l'écran hors écran
  (`RenderTargetBitmap`, aucune fenêtre créée) avec des données d'exemple, puis quitte. Les captures sont dans
  `Launcher/captures/` :
  `launcher_accueil.png`, `launcher_telechargement.png`, `launcher_verification.png`, `launcher_pret.png`,
  `launcher_horsligne.png`, `launcher_erreur.png`, `launcher_pret_manette.png` (invites Xbox) et
  `launcher_petite_fenetre.png` (960 × 600).
- `DeathlessLauncher.exe --test-maj [--racine <dossier>]` déroule toute la mise à jour sans fenêtre, avec le
  `launcher.json` de la racine, et écrit un compte rendu sur la sortie standard. Le code de sortie vaut 0 si le jeu
  est à jour ou installé, 2 s'il est hors ligne avec une version jouable, 1 en cas d'échec.
- `DeathlessLauncher.exe --test-video <vidéo.mp4> [--images 60] [--pas 1] [--secondes 12] [--naif]` est le banc du
  fond animé, sans fenêtre. Il joue la vidéo avec `FondVideo` et rend le fond hors écran environ 60 fois par seconde.
  Il relit le numéro de l'image réellement affichée, et celui que montre le lecteur caché juste avant chaque
  échange. Il compte ensuite les images perdues ou répétées au raccord, et compare la durée d'affichage des images
  autour du raccord à celle du reste de la vidéo. `--naif` mesure une boucle simple (un seul lecteur remis à zéro),
  pour comparer. `--inventaire` parcourt le fichier image par image, à l'arrêt.
- La vidéo de test est fabriquée par `Launcher/Outils/FabVideo`, avec l'encodeur de Windows : rien à télécharger.
  Chaque image code son numéro dans sa couleur. L'option `bruit` donne une vidéo lourde à décoder, et `tronquee`
  garde la durée d'origine de l'encodeur, pour tester l'échange sur l'événement de fin :

  ```powershell
  dotnet build -c Release Launcher\Outils\FabVideo
  Launcher\Outils\FabVideo\bin\Release\net48\FabVideo.exe $env:TEMP\boucle.mp4 60
  Launcher\bin\Release\net48\DeathlessLauncher.exe --test-video $env:TEMP\boucle.mp4 --images 60 --secondes 30
  ```

Le fond animé a été vérifié le 25/09/2026 sur quatre vidéos de test, pendant 30 s chacune :

- 640 × 360 et 1920 × 1080 avec bruit ;
- chacune avec la durée exacte (échange par la position) et avec la durée tronquée (échange sur l'événement de fin).

Dans les quatre cas :

- aucune image n'est perdue ni répétée au raccord (9 à 15 raccords par cas) ;
- le lecteur caché montrait l'image 0 avant chaque échange ;
- l'image la plus longue autour du raccord reste dans la variation normale du reste de la vidéo (par exemple
  46,6 ms contre 55,9 ms ailleurs).

La boucle simple, mesurée de la même façon, garde une image deux fois plus longtemps au raccord (72 ms). Sur la vidéo
lourde, elle tient une image 185 ms, en saute une et montre des images illisibles pendant la recherche. La mesure lit
l'image que le lecteur fournit au rendu de WPF, hors écran. Elle ne voit pas la présentation à l'écran elle-même, ni
la synchronisation verticale de l'écran.

Deux particularités de Windows, relevées pendant ces essais, sont prises en compte par `FondVideo` :

- la durée annoncée par le lecteur est tronquée à la seconde, et sa position reste bloquée sur cette valeur ;
- juste à la fin du flux, une remise à zéro n'est pas toujours prise en compte : elle est refaite deux fois, 250 ms
  puis 500 ms après l'échange.

Banc vérifié le 25/09/2026 avec `python -m http.server` en local et un faux build (`cmd.exe` renommé
`Deathless.exe`, jamais lancé). Les cas couverts :

- installation neuve, puis relance à jour ;
- mise à jour 0.1 vers 0.2 ;
- zip corrompu : l'empreinte est refusée, le `.part` supprimé, et l'ancienne version reste jouable ;
- zip annoncé mais absent (404) ;
- zip sans `Deathless.exe`, avec une entrée `../` ignorée : l'ancienne version reste intacte ;
- zip avec un dossier racine unique, extrait à plat ;
- `changelog.json` absent : repli sur `notes` ;
- reprise d'un téléchargement coupé (serveur avec `Range`), et départ de zéro si le serveur ne gère pas `Range` ;
- hors ligne avec et sans installation, hôte introuvable ;
- `launcher.json` absent.

## Choix

- **WPF** sur .NET Framework 4.8, au lieu de WinForms : la police est embarquée, les coins sont arrondis, le rendu
  des images est de qualité, les animations sont douces, et la capture hors écran est possible. Il n'y a toujours
  aucune bibliothèque tierce à l'exécution. Le JSON est lu par `JavaScriptSerializer` (System.Web.Extensions, fourni
  avec le framework) au lieu d'expressions régulières, parce que `changelog.json` a des listes.
- **Mise en page** : l'écran est dessiné à l'échelle des maquettes (1280 × 720, soit les valeurs de l'USS divisées
  par 1,5), puis agrandi ou réduit en bloc selon la fenêtre, comme le panneau UI Toolkit du jeu. La fenêtre fait
  environ 1280 × 720 au départ, sans dépasser l'écran. Elle est redimensionnable, avec un minimum de 960 × 540. La
  barre de titre est sombre (et couleur ardoise sur Windows 11).
- **Couleurs et formes** : les jetons de `Assets/UI/Theme/Deathless.uss` sont repris dans `Launcher/Theme.xaml` et
  `Teintes` (`Launcher/Controles.cs`). Si le thème du jeu change, il faut reporter les valeurs à ces deux endroits.
- **Installation sûre** : l'extraction se fait dans `Game.nouveau\`, puis les dossiers sont échangés. Un zip invalide
  ne détruit donc jamais la version jouable. Le jeu s'installe à côté du launcher, jamais dans Program Files : il n'y
  a pas d'élévation de droits.
- **Réseau** : `version.json` et `changelog.json` ont un délai de 20 s, si bien qu'un serveur muet ne bloque pas le
  mode hors ligne. Le téléchargement a un délai de 30 min, reprend là où il s'était arrêté (`Range`), et envoie le
  User-Agent `DeathlessLauncher/1.0`.
- **Fond animé** : la vidéo est à côté de l'exe (`fond.mp4`) plutôt que dedans. L'exe reste léger et la vidéo se
  change sans recompiler. Elle est lue par le `MediaPlayer` de WPF, qui s'appuie sur le lecteur de Windows : aucune
  bibliothèque à ajouter. La boucle utilise deux lecteurs en alternance (voir « Changer le fond »).
- **Indicateur d'attente** : une vague de gemmes or plutôt qu'une lueur. C'est le langage des effets du jeu :
  gemmes low poly, pas d'alpha, apparition par la taille.
- **Sons d'interface** : aucun. Le jeu n'a pas encore de sons de survol ni de validation en `.wav` (seulement les
  `.ogg` Kenney, que `System.Media.SoundPlayer` ne lit pas).

## Différences avec le launcher de Relic

Ce qui est repris sans changement : la lecture de `version.json`, le téléchargement, le contrôle SHA-256,
l'extraction à plat (avec les entrées hors du dossier ignorées), le jeu hors ligne, « Réessayer » et le lancement du
jeu depuis `Game\`.

Ce qui est ajouté :

- `changelog.json`, avec une copie locale pour le mode hors ligne ;
- le champ `nom` dans `version.json` ;
- la reprise du téléchargement ;
- le débit et le temps restant ;
- l'installation par échange de dossiers ;
- des délais courts pour la vérification ;
- la manette Xbox et les invites qui suivent l'appareil ;
- le fond animé en boucle, et la vague de gemmes pendant l'attente ;
- la progression dans la barre des tâches ;
- les modes capture et test ;
- `publish.ps1 -AvecLauncher`, qui publie aussi le launcher.
