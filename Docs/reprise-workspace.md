# Note de reprise — Deathless Workspace (`C:/Dev/Unity/Deathless-Workspace`) (25 septembre 2026, fin de soirée)

Vue d'ensemble pour reprendre sans relire les sessions. Le détail de chaque bac à sable est dans son `CLAUDE.md` (section « État »).

## Câblage du workspace

- Quatre projets Unity 6000.3.24f1 / URP : `main` (Deathless, git) et trois bacs à sable sans git : `sandbox-level`, `sandbox-rig`, `sandbox-vfx`.
- Les quatre éditeurs tournent en parallèle sur **un seul serveur MCP for Unity** (`http://127.0.0.1:8080/mcp`) qui route par instance. Client en ligne de commande pour les agents : `python tools/umcp.py <instance> tool <outil> '<json>'` (`tools/umcp.py --help`).
- `C:/Dev/Unity/Relic` = ancien projet, **lecture seule**, référence pour les assets et le code. Il est synchronisé entre deux PC par le NAS UGREEN : **vérifier la synchro avant de s'en servir** (working tree plus récent que `git log`, `Assets/Scenes/Game.unity` en YAML texte). Le 24/09 au soir la synchro manquait, ce qui a fait reproduire des versions obsolètes de la gemme et du portail.
- Méthode de travail : un agent par bac à sable, chacun cantonné à son dossier et à son instance MCP.

## sandbox-rig — armes et attaches : **tout validé, reporté dans `main`**

- Sept styles validés le 24-25/09 : épée + bouclier, bâton, dague + arbalète dans le dos (switch `AltWeaponSwitch`), hache + bouclier, hache à deux mains, arc + carquois (bascule repos/visée `BowStance`), visière du casque (`HelmetVisor`, 41°).
- Sockets `handslot.r` / `handslot.l` animés par les clips KayKit ; eulers, clips et scripts par style dans `Assets/WeaponStyles/*.md` + `.asset`.
- **Reporté dans `main`** : assets KayKit (15 Mo), scripts, `Assets/WeaponStyles/`, scène `Assets/Scenes/WeaponBench.unity`, `Docs/styles-d-armes.md`, `CLAUDE.md`. **15 entrées non committées dans `main`** (dernier commit : 4c09c6e).
- Points ouverts : IK main gauche (ou second socket) pour la hache à deux mains ; livre du mage ; variantes futures (griffes, épées de feu).

## sandbox-vfx — effets : **tout validé, rien encore transféré dans `main`**

- Repris de Relic à l'identique (mêmes GUID, scripts « Visual only » sans FishNet) : gemme Nyxessa + ceinture en orbite, portail voxel, téléportation, boule de feu (version Relic retenue), missile crâne, bouclier de relique, désintégration, sortie de terre.
- Créés ici et validés : flammèches du burn, cône de flammes, aura de soin (croix Lit menthe + paillettes en gemmes), rugissement du viking (crâne en gemmes, heaume à cornes, onde concentrique aller-retour), ondes de choc saut percutant et charge bélier (rendu gemmes).
- Langage visuel commun : gemmes low poly à couleurs par sommet, shader `Relic/VertexColorUnlit` (`Assets/VFX/_RelicCommun/`). Palette dans le `CLAUDE.md`.
- Scène `Assets/Scenes/VfxLab.unity` : ambiance de Relic reproduite (ciel, brouillard, cycle jour/nuit, post-traitement), pupitre `VfxLab` qui rejoue chaque effet en Play. 15 prefabs sous `Assets/VFX/`.
- Prochaine étape : transfert vers `main` (copie des dossiers `Assets/VFX/` avec `.meta`, sans les scripts de démo `*Demo.cs` / `_Lab/`).

## sandbox-level — village : **v4 en cours d'habillage, non validé en Play**

- Décisions du 24-25/09 : plaine entourée de forêt praticable (823 arbres, 7 m d'espacement, colliders de tronc), trois sentiers de sortie vers des clairières de spawn, **pas d'enceinte, pas de bâtiments de production, pas de poste RTS**, six maisons Hexagon hors des sentiers (40°, 95°, 165°, 195°, 270°, 315°, r 17 m), relique validée (prefab de sandbox-vfx) au centre avec rayon dégagé de 8 m, portail voxel validé à 11 m face à la relique, aucun rocher à collider dans le village.
- Générateur : `Assets/Editor/VillageBuilder.cs`, menu **Deathless > Village > Générer / Vérifier la circulation / Nettoyer**, paramètres en tête de fichier. Contrôleur de marche `Assets/Scripts/SandboxWalker.cs` (ZQSD/WASD, souris, Shift, Échap).
- **Interrompue le 25/09/2026 à la demande de Quentin** : la passe « sols en tuiles KayKit, chemins pavés de pierre, plateau de pierre à trois marches sous la relique » était en cours ; l'agent a reçu l'ordre de sauvegarder et de noter fait / pas fait dans la section État du `CLAUDE.md` de sandbox-level. À reprendre en premier côté level.
- Points ouverts : portes de maison à 1,5 m pour un joueur de 2 m (échelle x6-6,6), validation à pied en Play, donjon pas encore commencé.

## Prochaines étapes proposées

1. Committer le report des armes dans `main`.
2. Transférer les VFX validés dans `main` et brancher le premier effet sur le banc WeaponBench.
3. Valider le village v4 à pied (distances, lisibilité des sentiers, échelle des maisons), puis décider de l'échelle finale et du donjon.
4. Décider du mécanisme IK/second socket pour la hache à deux mains avant d'intégrer les classes.
