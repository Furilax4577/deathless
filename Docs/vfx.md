# Effets visuels (VFX) — récapitulatif (transférés le 25/09/2026)

Source : bac à sable `sandbox-vfx` (scène `VfxLab.unity`, fiche complète dans son `CLAUDE.md`, section « État »). Reporté ici le 25/09/2026 : **mêmes chemins et mêmes GUID** sous `Assets/VFX/`, aucune collision de GUID avec ce projet. Banc de vérification : `Assets/Scenes/VfxBench.unity` + `Assets/Scripts/Dev/VfxBench.cs`, capture `Assets/Screenshots/VfxBench.png`.

Deux origines :
- **Relic** (validés dans Relic, rapatriés à l'identique dans le bac à sable) : scripts copiés tels quels ou en version « Visual only » (sans FishNet, sans son, sans gameplay : un champ public remplace l'état réseau, signalé en tête de chaque fichier).
- **Bac à sable** (créés et validés par l'utilisateur dans `sandbox-vfx` les 24 et 25/09/2026).

Non transférés : tous les `*Demo.cs` et `Assets/VFX/_Lab/` (`VfxLabDemo`, `IEffetDemo`, `VfxRejoueur`, matériaux factices, `RelicVolumeProfile`), scènes et captures du labo, `_Ambiance/` sauf `DayCycle.cs`. Aucun script runtime ne dépendait d'`IEffetDemo`.

## Langage visuel commun

- **Gemmes low poly à couleurs par sommet** : chaque effet « matière » est un seul maillage dynamique de petites gemmes (`LowPolyGem`, 8 facettes, 24 sommets, ombrage peint dans la couleur de sommet), rendu par le shader **`Relic/VertexColorUnlit`** (`_RelicCommun/VertexColorUnlit.shader`) via `PortalVoxel.mat` ou une copie (`AuraSoin_Gemmes`, `Rugissement_Gemmes`, `OndeDeChoc_Gemmes`). Pas d'alpha : apparition et disparition par la taille.
- Effets de particules du bac à sable (flammèches, cône, croix du soin) : URP Lit standard, flat shading, maillages facettés (tétraèdre, croix), sans émission.
- Feu de Relic (boule, explosion, gerbe de terre) : `FireBurst.mat` émissif + particules additives (`FlameParticle`, `SmokeParticle`).
- Palette réduite par effet (2 à 5 teintes), voir chaque fiche.

## Dossiers communs

- `Assets/VFX/_RelicCommun/` : `LowPolyGem` (écriture d'une gemme dans un maillage partagé), `GemShape` (ScriptableObject : nuage de points cuit), `GemBurst` (`Explode`, `Implode`, `Shatter`, `Rise`), `GemTrail` (`Follow`), `FireballVisual` (`Attach`, `SpawnEmber`), `FireballEmber`, `LowPolyBlast` (`Fire`, `Spawn`), `DirtBurst` (`Spawn`), `AreaBurst` (`Spawn`), `FireEffect` (`Create`, `SetEmitting`), `WavyTrail` (copié, plus utilisé par la boule actuelle), `Ambiance` (presets jour/crépuscule), shader `VertexColorUnlit`, `PortalVoxel.mat`, `FireBurst.mat`.
- `Assets/VFX/_Ambiance/DayCycle.cs` : seul fichier d'ambiance repris, parce que `RelicGem` et `RelicGlow` lisent `DayCycle.Night` (statique). Copie « Visual only » (`nuit` 0/1 et `heure` 0-1 à la place de `RunProgress`). Sans `DayCycle` dans la scène, `Night` vaut 0 (jour). L'asset `Ambiance.asset`, le ciel, le sol et la brume `GroundMist` du labo ne sont pas repris.

## Fiches

### Gemme Nyxessa + ceinture (Relic)
- **Prefab** : `Assets/VFX/GemmeNyxessa/GemmeNyxessa.prefab` (rocher Forest `Rock_1_O_Color1` au sol, `Crystal` à 5,68 m au-dessus de la racine, échelle 1,3 × 2 × 1,3, lumière ponctuelle).
- **Scripts** : `RelicGem` (gemme à 7 pans irréguliers, remplace `Crystal.asset` au démarrage), `RelicGlow`, `RelicBelt` (650 gemmes en bande horizontale, rayon 1,5 m, largeur 0,45, épaisseur 0,14), `CrystalSpin`.
- **API** : `RelicBelt.Pulse()` (impulsion de l'anneau, à l'ouverture du portail à l'aube) ; `RelicBelt.portailOuvert` (l'impulsion part au passage faux → vrai). Nuit : `DayCycle.Night >= 0.5`.
- **Dépendances** : `LowPolyGem`, `DayCycle`, `PortalVoxel.mat`, `RelicMaterial.mat`, `KayKit_Forest.mat` + `forest_texture.png` (copiés dans le dossier, pas dans `Assets/Art/`).
- **Palette** : vert Nyxessa `#145032` (matériau), dégradé de verts de la ceinture (0.04, 0.22, 0.05) → (0.85, 1.15, 0.6).

### Portail de donjon (Relic)
- **Prefab** : `Assets/VFX/PortailDonjon/PortailDonjon.prefab` (racine à y = 1,5, tournée de 90° ; anciens quads `Glow` / `SwirlBack` / `SwirlFront` masqués au démarrage, gardés pour la fidélité).
- **Script** : `PortalVisual` (Visual only) : disque de 1 700 gemmes, rayon 1,35 m, cellule 0,1 m ; ouverture 1,3 s (gerbe), fermeture 0,9 s (implosion).
- **API** : `PortalVisual.ouvert` (remplace `Portal.IsOpen`), `Ripple()` (onde concentrique au passage d'un joueur), `Center` (centre de la soupe).
- **Dépendances** : `LowPolyGem`, `GemBurst`, `PortalVoxel.mat` (+ `FireBurst`, `TrailGlow`, `TrailSmoke` référencés mais plus utilisés).
- **Palette** : verts (0.07, 0.38, 0.05), (0.25, 0.7, 0.08), (0.55, 0.95, 0.2), pâle (0.95, 1.25, 0.55).

### Téléportation par le portail (Relic)
- **Prefab** : `Assets/VFX/Teleportation/Teleportation.prefab` (contient un `PortailDonjon` imbriqué ; le corps factice du labo a été retiré).
- **Script** : `PortalTransit` (copie telle quelle).
- **API** : `PortalTransit.Depart(Bounds corps, Vector3 centrePortail, Material gemmes, 1.1f)` puis, à l'arrivée, `PortalTransit.Arrive(corps, centrePortailSortie, gemmes, 1.0f)` ; `PortalVisual.Ripple()` sur chaque portail ; masquer le corps pendant le transit (séquence de `PlayerZone.Transit` dans Relic). Volume du joueur : 0,8 × 1,9 × 0,8.
- **Dépendances** : `LowPolyGem`, `PortalVisual`, `PortalVoxel.mat`.

### Boule de feu (Relic)
- **Prefab** : `Assets/VFX/BouleDeFeuRelic/BouleDeFeuRelic.prefab` : racine vide (le composant de démo portait la séquence). L'effet est entièrement dans les API statiques.
- **API** : en vol, `FireballVisual.Attach(projectile, FireBurst)` (corps à facettes, noyau, lumière, braises `FireballEmber`) ; à l'impact, `LowPolyBlast.Fire(point, 2.5f, FireBurst)` + fumée `FireEffect.Create(null, point, 1f, 0.8f, FlameParticle, SmokeParticle, false, true, false)`, émission 0,4 s, détruite à 3 s (= `SkillEffects.Burst` de Relic).
- **Paramètres (VfxRecorder de Relic)** : 18 m/s, chute 5, +1,5 m/s vers le haut, départ 2,5 m, 1,3 s ou jusqu'au sol.
- **Dépendances** : `FireballVisual`, `FireballEmber`, `LowPolyBlast`, `FireEffect`, `FireBurst.mat`, `FlameParticle.mat` + `flame_soft.png`, `SmokeParticle.mat` + `smoke_soft.png` (`TrailGlow`, `TrailSmoke` copiés pour le portail).
- **Palette** : jaune (1, 0.9, 0.4), orange (1, 0.38, 0.04), rouge (0.8, 0.12, 0.03), en émissif.
- Retenue par l'utilisateur le 25/09/2026 (la version du bac à sable est abandonnée).

### Missile magique, crâne en gemmes (Relic)
- **Prefab** : `Assets/VFX/MissileMagique/MissileMagique.prefab` : racine vide (idem boule de feu).
- **Script** : `SkullMissileVisual` (Visual only, sans `GameAudio`).
- **API** : au tir, `GemBurst.Explode(bouche + dir * 0.3f, 0.5f, PortalVoxel)` (éclat de `RelicTurret.FireRpc`) ; `SkullMissileVisual.Attach(projectile, SkullGemShape, Vector3.zero, 0.55f, PortalVoxel)` ; à l'arrivée, `Shatter()`.
- **Paramètres** : 12 m/s, 1,6 s ; forme cuite `SkullGemShape.asset` (1 100 points).
- **Dépendances** : `GemShape`, `GemTrail`, `GemBurst`, `LowPolyGem`, `PortalVoxel.mat` (`GhostSkull.mat` copié pour mémoire).

### Bouclier de la relique (Relic)
- **Prefab** : `Assets/VFX/BouclierRelique/BouclierRelique.prefab` (racine à y = 0,8 : `BasePosition` = racine − 0,8 m).
- **Scripts** : `RelicShieldVisual` (900 gemmes en cylindre), `RelicShieldEtat` (remplace le `RelicShield` réseau, même API).
- **API** : `RelicShieldEtat.Lever()` (incantation 3 s, les gemmes montent du sol), `Frapper(dégâts, point)` (onde depuis l'impact, couleur selon la vie), `Baisser()` ; rupture automatique à 0 PV (`RelicShieldVisual.Shatter()`). Dans Deathless, brancher ces appels sur l'état serveur.
- **Paramètres (GameBalance de Relic)** : rayon 5,5 m, hauteur 6 m, 300 PV, seuils orange 0,4 et rouge 0,15.
- **Palette** : bleu (0.1, 0.4, 0.85) → orange (0.9, 0.45, 0.08) → rouge (0.85, 0.12, 0.1).

### Désintégration des ennemis à l'aube (Relic)
- **Prefab** : `Assets/VFX/Desintegration/Desintegration.prefab` : racine vide.
- **API** : couper les rendus de l'ennemi, puis `GemBurst.Rise(boundsDesRendus, PortalVoxel)` (gemmes couleur os qui montent en tournoyant, ~2,6 s) (= `EnemyVisual.Vaporize`).
- **Dépendances** : `GemBurst`, `LowPolyGem`, `PortalVoxel.mat`. Squelette de test : `SortieDeTerre/Skeleton_Warrior.fbx` + `KayKit_Skeleton_Enemy.mat` (teinte rouge des ennemis de Relic), échelle 0,75.

### Sortie de terre des squelettes (Relic)
- **Prefab** : `Assets/VFX/SortieDeTerre/SortieDeTerre.prefab` : racine vide.
- **API** : clip `Skeletons_Spawn_Ground` (de `SortieDeTerre/Rig_Medium_Special.fbx`) joué à la vitesse 1,5 ; le modèle part 1,9 m sous terre et remonte pendant 75 % du clip (montée en `1 − (1 − k)²`) ; `DirtBurst.Spawn(sol, FireBurst, 1f)` au départ et `DirtBurst.Spawn(sol, FireBurst, 0.55f)` à 35 % (= `EnemyVisual.UpdateIntro` / `ApplyRise`).
- **Dépendances** : `DirtBurst`, `FireballVisual`, `FireBurst.mat`, `Skeleton_Warrior.fbx`, `skeleton_texture.png`, `KayKit_Skeleton_Enemy.mat`, `Rig_Medium_Special.fbx` (copiés dans le dossier, pas dans `Assets/Art/KayKit/`).

### Flammèches du burn (bac à sable)
- **Prefab** : `Assets/VFX/BurnFlammeches/BurnFlammeches.prefab`, à parenter au centre de l'ennemi.
- **API** : trois ParticleSystem bouclés (`Sombre`, `Vif`, `Clair`, `playOnAwake`) : actif = ça brûle ; `Stop()` sur les trois (ou désactiver) à la fin du burn.
- **Paramètres** : mesh `Flammeche.asset` (tétraèdre, pointe en haut), émission sur un ellipsoïde 1,3 × 2,2 × 1,3 (capsule de 2 m, à adapter à l'ennemi), montée 1 à 2,6 m/s, bruit léger.
- **Palette** : `#145032`, `#1e5a32`, `#3fae5a` (URP Lit).

### Cône de flammes (bac à sable)
- **Prefab** : `Assets/VFX/ConeDeFlammes/ConeDeFlammes.prefab`, à parenter à la main du mage, émission le long de +Z local.
- **API** : trois ParticleSystem bouclés : actif pendant le sort, `Stop()` au relâchement.
- **Paramètres** : mesh `Flamme.asset` (tétraèdre), cône de 30° d'ouverture, rayon 6 cm, 5 à 7,4 m/s × 0,7 à 1 s ≈ 6 m de portée, taille 0,25 → 1 → 0.
- **Palette** : `#145032` (gros, lent), `#1e5a32`, `#3fae5a` (petit, rapide) (URP Lit).

### Aura de soin (bac à sable)
- **Prefab** : `Assets/VFX/AuraSoin/AuraSoin.prefab`, au sol sous l'allié (racine à y = −1 : prévue pour être enfant du centre d'une capsule de 2 m).
- **Scripts** : `AuraSoin` (déclencheur écrit pour Deathless, reprend `AuraSoinDemo` sans la boucle), `AuraGemmes` (paillettes en gemmes).
- **API** : `AuraSoin.Jouer()` au soin (croix + paillettes, ~1 s).
- **Paramètres** : 6 à 8 croix `Croix.asset` (grille 3 × 3 de cellules de 0,14 m), disque de 1,1 m parcouru en 0,3 s, montée 1,3-2 m/s ; 20 à 40 gemmes de 0,05-0,09 m, montée 0,9-1,4 m/s, durée 1 s. `Fragment.asset` copié mais inutilisé.
- **Palette** : menthe `#4fcf9a`, menthe sombre `#2e9e72` (croix en URP Lit, paillettes en couleurs par sommet).

### Rugissement du viking (bac à sable)
- **Prefab** : `Assets/VFX/Rugissement/Rugissement.prefab`, racine au centre de la capsule du personnage (le crâne `Crane` est à 1,65 m au-dessus), visage vers +Z.
- **Scripts** : `Rugissement` (déclencheur écrit pour Deathless, séquence reprise de `RugissementDemo` sans la boucle), `RugissementCrane` (crâne barbare en gemmes : heaume, cornes, mandibule aux condyles), `RugissementOnde` (anneau de 72 gemmes).
- **API** : `Rugissement.Jouer()` ; `Appliquer(t)` pose un instant donné ; `DureeTotale` ≈ 1,52 s. Bas niveau : `RugissementCrane.Ouverture` (0..1), `Bascule` (0..1), `RugissementOnde.Jouer()`.
- **Paramètres** : pop 0,12 s, ouverture 0,2 s (28°, tremblement 0,3 s, tête rejetée de 13°), onde 0,9 s jusqu'à 4,5 m puis retour, fermeture 0,15 s, rétraction 0,15 s.
- **Dépendances** : `SkullGemShape.asset` (dossier `MissileMagique`), `GemShape`, `LowPolyGem`, `Rugissement_Gemmes.mat`.
- **Palette** : rouge vif `#b3261e`, rouge sombre `#6e1410`, rouge très sombre `#3a0a08`, ivoire `#e8dcc0`, fer `#5a5f66` / `#3f444a`, fer clair `#7a8088`.

### Ondes de choc (bac à sable)
- **Prefabs** : `Assets/VFX/OndeDeChoc/OndeDeChoc.prefab` (base), variantes `OndeDeChoc_SautPercutant.prefab` (cercle, rayon 4,5 m, 0,75 s) et `OndeDeChoc_ChargeBelier.prefab` (arc de 100° devant, +Z local, rayon 5 m, 0,6 s, anneau plus large au départ). Racine des variantes à y = −1 (enfant du centre de la capsule).
- **Scripts** : `OndeDeChoc` (paramètres `rayonMax`, `duree`, `angleOuverture`, `rayonDepart`, `largeurDepart/Fin`), `OndeGemmes` (rendu).
- **API** : `OndeDeChoc.Jouer()` ; `Appliquer(t)` pour une capture.
- **Paramètres** : anneau de gemmes (14 par mètre de périmètre, plafond 420, taille 0,13 → 0,06 m), 26 gros éclats + 16 petits, coup de pied vertical 3,8 m/s, gravité 0,7 g.
- **Palette** : terre sombre `#5b3f2a`, terre claire `#8a6a48`.
- Pas encore d'onde spécifique pour la charge bélier du chevalier/paladin (case ouverte dans le bac à sable).

## Banc `VfxBench`

Scène `Assets/Scenes/VfxBench.unity` : sol (`RigTest_Ground.mat`), lumière directionnelle, caméra en surplomb, deux rangées de postes avec un label TextMesh au sol (fond : effets de Relic ; devant : effets du bac à sable + squelettes). Figurants : `Knight` (téléportation, soin, rugissement) en `Idle_A` échantillonné, `Skeleton_Warrior` (burn, désintégration, sortie de terre). Script unique `Assets/Scripts/Dev/VfxBench.cs` : en Play, chaque poste boucle à son intervalle par l'API publique ci-dessus ; `ToutJouer()` relance tout au même instant (utile pour une capture ~0,5 s après). Les durées de vol et intervalles du banc sont des réglages de banc, pas de l'effet.

## Points ouverts

- Boule de feu retenue (Relic) **orange**, alors que le feu du mage du bac à sable (flammèches, cône) est **vert Nyxessa** : à harmoniser ou à assumer.
- Les prefabs `BouleDeFeuRelic`, `MissileMagique`, `Desintegration`, `SortieDeTerre` sont des racines vides : l'effet vit dans les API statiques ; le code de jeu (sorts, ennemis) devra porter la séquence (voir `VfxBench.cs`).
- `RelicGem` / `RelicGlow` / `RelicBelt` / `PortalVisual` / `RelicShieldEtat` gardent les champs « Visual only » du bac à sable (`DayCycle.Night`, `portailOuvert`, `ouvert`, `Lever/Frapper/Baisser`) : à brancher sur l'état de partie de Deathless.
