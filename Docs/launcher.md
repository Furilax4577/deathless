# Launcher de Deathless

Exe Windows pour jouer entre amis sans manipuler de zip. Il vérifie la version publiée sur le serveur de Quentin,
télécharge et installe le jeu si besoin, affiche les notes de version, puis lance le jeu. Le code est dans
`Launcher/`, hors du projet Unity. Il reprend le launcher de Relic (`C:/Dev/Unity/Relic/Launcher`) avec la même
logique, et l'habille comme le menu principal du jeu.

![Launcher prêt](../Launcher/captures/launcher_pret.png)

## Côté joueur

1. Télécharger `http://srv617344.hstgr.cloud/deathless/DeathlessLauncher.zip` et le dézipper dans un dossier à soi,
   par exemple `Documents\Deathless\`. Le zip contient `DeathlessLauncher.exe`, `launcher.json` (déjà réglé sur le
   serveur), `OFL.txt` (licence de la police), `fond.mp4` (le fond animé) et `fond0.png` (sa première image). Il n'y a
   rien à installer et aucun droit administrateur n'est demandé.
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
  est animée en boucle si `fond.mp4` est à côté de l'exe, et fixe sinon (voir « Changer le fond »). Au lancement,
  l'image fixe est la première image de la vidéo, et la vidéo prend sa place sans que cela se voie.
- **Volet gauche** : titre « DEATHLESS » et sous-titre, puis les entrées « Jouer », « Réessayer » (après un échec ou
  hors ligne), « Wiki » (règles, classes et commandes : la version joueur du wiki, dans le navigateur) et « Quitter ». L'entrée sélectionnée a le fond actif et la bordure or, comme au menu du jeu. Pendant la
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
- **Notes de version** : panneau `dl-panel` à droite.
  - **Replié, par défaut** : une petite carte en haut à droite, posée sur les arbres, qui laisse voir Nyxessa et le
    portail. Elle montre « NOTES DE VERSION », la dernière version, son étiquette et son titre (par exemple
    « Première défense du village »), avec l'invite N ou Y et un chevron.
  - **Déplié** : toute la hauteur jusqu'à la barre d'invites, de la version la plus récente à la plus ancienne.
    Chaque version a son numéro, sa date et ses notes en puces. La liste défile en douceur, avec un fondu en haut et
    en bas quand il reste du texte caché.
  - **Bascule** : N ou Y, un clic sur la carte, ou un clic sur l'en-tête quand le panneau est déplié. Échap ou B le
    replie. Le passage d'un état à l'autre est une animation de hauteur de 0,2 s, sans fondu.
  - **Étiquettes** : « Installée » marque la version installée, « Nouvelle » la version publiée qui n'est pas
    encore installée.
- **Barre d'invites** en bas : Valider, Choisir, Notes de version, et Défiler quand les notes sont dépliées.
  L'appareil actif est affiché à droite. Les icônes Kenney suivent le dernier appareil utilisé, comme dans le jeu.

![Notes dépliées](../Launcher/captures/launcher_accueil_notes.png)

### Commandes

| Action | Clavier et souris | Manette Xbox |
|---|---|---|
| Valider l'entrée sélectionnée | Entrée, Espace, clic | A |
| Entrée précédente ou suivante | Flèches haut et bas, Tab, survol | Croix, stick gauche |
| Déplier ou replier les notes | N, clic sur la carte (Échap replie) | Y (B replie) |
| Défiler les notes dépliées | Molette, Page préc. et Page suiv., Début et Fin | Stick droit, LB et RB |

La manette est lue par XInput (`xinput1_4.dll`, avec repli sur `xinput9_1_0.dll`, sans rien installer), seulement
quand le launcher est au premier plan. Les manettes PlayStation ne passent pas par XInput : avec elles, il faut le
clavier et la souris.

## Côté serveur (en place depuis le 25/09/2026, version 0.1 publiée)

Le serveur est le VPS de Quentin (`srv617344.hstgr.cloud`, déjà utilisé par Relic : Ubuntu, Nginx, accès SSH
`root` par clé, jamais de mot de passe dans le dépôt ni dans une conversation). Quentin a lancé le script
`Deathless-Workspace/tools/serveur-deathless.sh` le 25/09/2026, qui a :

1. sauvegardé la configuration Nginx (`/root/nginx-relic.bak-<date>`) ;
2. créé `/var/www/deathless` (propriétaire `www-data`) et `/root/deathless-staging/current` pour `publish-diff.ps1` ;
3. ajouté au site `relic` (`/etc/nginx/sites-available/relic`) un bloc `location /deathless/`, servi à l'adresse
   `http://srv617344.hstgr.cloud/deathless/`, avec `Cache-Control: no-store` sur `version.json` et `changelog.json` ;
4. déposé un `changelog.json` vide (`{ "versions": [] }`), vérifié en ligne le même jour.

Nginx accepte les requêtes `Range` par défaut : la reprise des téléchargements fonctionne sans réglage (vérifié :
réponse 206).

**Wiki** : le bloc `/deathless/` redéfinit ses types MIME (`types { application/json json; application/zip zip; }`),
ce qui remplace toute la table. Sans réglage, les pages `.html` et les icônes `.svg` du wiki partiraient en
`application/octet-stream`, et le navigateur les téléchargerait. Le 25/09/2026, après sauvegarde
(`/root/nginx-relic.bak-20260925-142147`) et `nginx -t`, une ligne a donc été ajoutée dans ce bloc :

```nginx
location /deathless/wiki/ { root /var/www; include /etc/nginx/mime.types; charset utf-8; }
```

`index.html` est l'index par défaut de Nginx : `/deathless/wiki/` sert la page d'accueil, et `/deathless/wiki`
redirige vers `/deathless/wiki/` (301). `/relic/` répond toujours.

**Première publication** : la 0.1 a été publiée le 25/09/2026 (publication 1, build `Builds\Deathless-0.1`, Paladin
seul jouable), avec le launcher et le wiki. Vérifié en HTTP le même jour :

- `http://srv617344.hstgr.cloud/deathless/version.json` et `changelog.json` : 200, `application/json`,
  `Cache-Control: no-store` ;
- `deathless-v1.zip` : 68 771 224 octets, SHA-256 identique à `version.json`, `Deathless.exe` à la racine, sans
  `DoNotShip` ;
- `DeathlessLauncher.zip` : l'exe, `launcher.json`, `OFL.txt`, `fond.mp4` et `fond0.png` ;
- `/deathless/wiki/` et `/deathless/wiki/classe-paladin.html` : 200, `text/html; charset=utf-8`, icônes en
  `image/svg+xml`.

Le launcher publié, dézippé dans un dossier de test, a installé la 0.1 depuis le serveur avec `--test-maj`
(téléchargement de 65,6 Mo, SHA-256, extraction), puis l'a vue à jour à la relance.

Le VPS expire le 10 octobre 2026 : il faudra le prolonger ou changer `baseUrl`.

Contenu du dossier publié :

- `version.json` : la version à installer (format ci-dessous).
- `changelog.json` : les notes de toutes les versions (format ci-dessous).
- `deathless-v<N>.zip` : le build Windows, avec son contenu à la racine (`Deathless.exe`, `Deathless_Data\…`). Les
  anciens zips peuvent rester ou être supprimés : seul celui de `version.json` compte.
- `DeathlessLauncher.zip` : le launcher à distribuer.
- `wiki/` : la version joueur du wiki (`Wiki/public`), envoyée par `publish.ps1 -AvecWiki`.

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
{
  "baseUrl": "http://srv617344.hstgr.cloud/deathless/",
  "gameExe": "Deathless.exe",
  "wikiUrl": "http://srv617344.hstgr.cloud/deathless/wiki/"
}
```

`wikiUrl` est l'adresse ouverte par l'entrée « Wiki », dans le navigateur par défaut. Si le champ est absent,
c'est cette adresse par défaut ; s'il est vide, l'entrée est masquée.

## Numéros de version (décidé le 25/09/2026)

Format **majeur.mineur.correctif**, comme npm (versionnement sémantique) :

- **majeur** : changement cassant (sauvegardes ou parties en réseau incompatibles, règles refondues). Reste à 0 tant que le jeu est en développement ;
- **mineur** : nouveautés (classes, écrans, contenu) ;
- **correctif** : corrections et équilibrage seulement.

Le numéro affiché est le `-Nom` de `publish.ps1` (par exemple `0.3.0`), le même que `version` dans `changelog.json` et que
la version du jeu (`PlayerSettings.bundleVersion`, posée par le script de build). `-Version` reste un simple compteur
entier de publications (1, 2, 3…). Le launcher considère `0.2` et `0.2.0` comme la même version.

## Côté Quentin : publier une version

Prérequis, une seule fois : une clé SSH (`ssh-keygen -t ed25519`) dont la clé publique est déposée sur le serveur
(`~/.ssh/authorized_keys`), pour que `scp` fonctionne sans mot de passe.

1. Faire le build Windows depuis Unity (File > Build), par exemple dans `Builds\Deathless-v1-2026-09-25\` (dossier
   ignoré par git).
2. Mettre à jour `Launcher\changelog.json`.
3. Depuis la racine du dépôt (`main`) :

```powershell
.\Launcher\publish.ps1 -BuildDir "Builds\Deathless-0.1" -Version 1 -Nom "0.1" -AvecLauncher -AvecWiki `
    -Remote "root@srv617344.hstgr.cloud:/var/www/deathless/" -BaseUrl "http://srv617344.hstgr.cloud/deathless/"
```

Le script zippe le build (sans les `.log` ni le dossier `*_DoNotShip`), calcule l'empreinte, écrit `version.json`,
vérifie et copie `changelog.json`. Avec `-AvecLauncher`, il zippe le launcher construit, avec `fond.mp4` et son
image 0 (`fond0.png`, extraite par le launcher lui-même). Avec `-AvecWiki`, il régénère le wiki
(`python Wiki\build.py`) et zippe sa version joueur (`Wiki\public`).

Il envoie ensuite le tout par `scp`, `version.json` en dernier. Le wiki est décompressé sur le serveur dans
`wiki.nouveau` (par le module `zipfile` de python3, rien à installer), puis échangé d'un coup avec `wiki/`. Enfin,
le script relit en ligne `version.json`, `changelog.json` et le wiki pour confirmer. Sans `-Remote`, rien ne part :
les fichiers restent dans `Builds\publish\` pour contrôle.

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
`DeathlessLauncher.exe.config`, `fond.mp4` (copie de `Assets/Screenshots/menu_nuit_boucle.mp4`, quand elle existe) et
`fond0.png`. Cette dernière est la première image de la vidéo, extraite après la compilation par l'exe lui-même
(`--image0`, cible `ImageZeroDuFond` du `.csproj`, refaite quand la vidéo change).
Ces fichiers forment le launcher à distribuer, que `publish.ps1 -AvecLauncher` zippe. Le script laisse de côté ce
qu'un lancement depuis `bin\` a pu y ajouter (`changelog.cache.json`, `Game\`), et reprend toujours `fond.mp4` du
dépôt. Le SDK `dotnet` récent suffit : les assemblies de référence .NET Framework 4.8 viennent du paquet NuGet
`Microsoft.NETFramework.ReferenceAssemblies`, utilisé à la compilation seulement.

Tout est compilé dans l'exe (environ 1,6 Mo) :

- la police Fredoka (4 graisses), lue dans `Assets/Art/Fonts/Fredoka/` ;
- les invites Kenney (Entrée, flèches, molette, N ; A, croix, stick droit, Y), lues dans
  `Assets/Art/UI/KenneyInputPrompts/*/Double/` ;
- l'image de fond (voir ci-dessous) ;
- l'icône, `Launcher/Ressources/deathless.ico` : la gemme de Nyxessa du HUD, régénérée par
  `python Launcher/make_icon.py`.

Le launcher ne se met pas à jour lui-même : une nouvelle version se distribue à la main, en republiant
`DeathlessLauncher.zip`.

## Changer le fond

### Fond animé (vidéo)

Au démarrage, le launcher cherche `fond.mp4` à côté de `DeathlessLauncher.exe` et le lit en boucle, muet. La vidéo
n'est jamais compilée dans l'exe.

- **Démarrage invisible** : l'image fixe de départ est `fond0.png`, la première image exacte de la vidéo. Elle est
  extraite par le launcher lui-même, par le même chemin de décodage que la lecture : couleurs et cadrage sont
  identiques. La vidéo est posée sous cette image, et sa lecture démarre dessous, hors de vue. L'image fixe n'est
  retirée qu'une fois que la vidéo livre une vraie image. Juste après le démarrage, le lecteur de Windows montre un
  instant une surface noire, qui ne doit jamais être vue. Le passage se fait donc de l'image 0 à l'image 1 ou 2,
  quasi identique.

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
- Sans `fond0.png` (par exemple une vidéo posée à la main à côté de l'exe), l'image fixe est l'image ci-dessous, et
  le passage peut se voir. Pour la créer : `DeathlessLauncher.exe --image0 fond.mp4 fond0.png`.

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

L'image est étirée pour couvrir la fenêtre sans être déformée (les bords sont rognés), et centrée. Nyxessa et le
portail doivent donc se trouver entre le volet gauche (42 % de la largeur) et le bord droit, sous la carte des notes
repliées (en haut à droite, sur 20 % de la hauteur). Le format conseillé est 16:9, 1920 × 1080 ou plus.

## Outils de développement (options cachées, sans fenêtre)

- `DeathlessLauncher.exe --capture <fichier.png> [--etat accueil|telechargement|verification|pret|horsligne|erreur]
  [--notes deplie] [--manette] [--changelog Launcher\changelog.json] [--largeur 1280 --hauteur 720]` rend l'écran
  hors écran (`RenderTargetBitmap`, aucune fenêtre créée) avec des données d'exemple, puis quitte. Les notes sont
  repliées, sauf avec `--notes deplie`. Les captures sont dans `Launcher/captures/` :
  - `launcher_accueil.png` et `launcher_accueil_notes.png` (notes dépliées) ;
  - `launcher_telechargement.png`, `launcher_verification.png`, `launcher_pret.png`, `launcher_horsligne.png`,
    `launcher_erreur.png` ;
  - `launcher_pret_manette.png` (invites Xbox) et `launcher_petite_fenetre.png` (960 × 600).
- `DeathlessLauncher.exe --image0 <vidéo.mp4> <image.png>` extrait la première image de la vidéo, à sa taille
  native (c'est ainsi que `fond0.png` est produite).
- `DeathlessLauncher.exe --test-demarrage [--visuel] [--plein]` est le banc du passage de l'image fixe à la vidéo,
  avec le `fond.mp4` posé à côté de l'exe. Il mesure la durée de chaque opération du fil de l'interface
  (`Dispatcher.Hooks`), hors passes de rendu. Avec `--visuel`, il mesure aussi l'écart entre le fond affiché et
  l'image de départ. Avec `--plein`, ce rendu se fait en 1920 × 1080, où la vidéo n'est pas mise à l'échelle.
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

Passage de l'image fixe à la vidéo, mesuré le 25/09/2026 avec `--test-demarrage`, sur la vraie vidéo :

| | Avant correction | Après correction |
|---|---|---|
| Image de départ | `menu_nuit_fond.png`, un autre cadrage | `fond0.png`, l'image 0 de la vidéo |
| Écart moyen au passage (0 à 255) | 4,6 puis 11,1 : saut visible | 0,27 à 0,45 en 1:1, soit une ou deux images de mouvement |
| Image noire au passage | oui, par intermittence (surface noire du lecteur à `Play()`) | aucune sur 8 passages |
| Plus longue opération du fil de l'interface | 7,4 ms (ouverture des lecteurs) | 4,5 à 6,1 ms (9,2 ms à la première exécution d'un exe neuf) |

Le micro-gel ressenti venait du passage lui-même, pas d'un fil de l'interface bloqué. Deux causes :

- un changement d'image, l'image fixe n'étant pas la première image de la vidéo ;
- un éclair noir, la vidéo étant rendue visible au moment de `Play()`.

Sans fenêtre, le banc ne peut pas mesurer le fil de rendu ni la présentation à l'écran. La vidéo y est cependant
composée cachée, sous l'image fixe, bien avant le passage.

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
- les notes de version repliables et l'entrée Wiki ;
- la progression dans la barre des tâches ;
- les modes capture et test ;
- `publish.ps1 -AvecLauncher` et `-AvecWiki`, qui publient aussi le launcher et le wiki.
