# Effets visuels (VFX) — récapitulatif (transférés le 25/09/2026)

Source : bac à sable `sandbox-vfx` (scène `VfxLab.unity`, fiche complète dans son `CLAUDE.md`, section « État »). Reporté ici le 25/09/2026 : **mêmes chemins et mêmes GUID** sous `Assets/VFX/`, aucune collision de GUID avec ce projet. Banc de vérification : `Assets/Scenes/VfxBench.unity` + `Assets/Scripts/Dev/VfxBench.cs`, capture `Assets/Screenshots/VfxBench.png`. Palettes de thème : `Assets/VFX/_Palettes/` (voir plus bas).

Deux origines :
- **Relic** (validés dans Relic, rapatriés à l'identique dans le bac à sable) : scripts copiés tels quels ou en version « Visual only » (sans FishNet, sans son, sans gameplay : un champ public remplace l'état réseau, signalé en tête de chaque fichier).
- **Bac à sable** (créés et validés par l'utilisateur dans `sandbox-vfx` les 24 et 25/09/2026).

Non transférés : tous les `*Demo.cs` et `Assets/VFX/_Lab/` (`VfxLabDemo`, `IEffetDemo`, `VfxRejoueur`, matériaux factices, `RelicVolumeProfile`), scènes et captures du labo, `_Ambiance/` sauf `DayCycle.cs`. Aucun script runtime ne dépendait d'`IEffetDemo`.

## Langage visuel commun

- **Gemmes low poly à couleurs par sommet** : chaque effet « matière » est un seul maillage dynamique de petites gemmes (`LowPolyGem`, 8 facettes, 24 sommets, ombrage peint dans la couleur de sommet), rendu par le shader **`Relic/VertexColorUnlit`** (`_RelicCommun/VertexColorUnlit.shader`) via `PortalVoxel.mat` ou une copie (`AuraSoin_Gemmes`, `Rugissement_Gemmes`, `OndeDeChoc_Gemmes`). Pas d'alpha : apparition et disparition par la taille.
- Effets de particules du bac à sable (flammèches, cône, croix du soin) : URP Lit standard, flat shading, maillages facettés (tétraèdre, croix), sans émission.
- Feu de Relic (boule, explosion, gerbe de terre) : `FireBurst.mat` émissif + particules additives (`FlameParticle`, `SmokeParticle`).
- **Couleurs par thème** (source unique, voir « Palettes de thème » ci-dessous) : chaque effet prend ses teintes dans la palette de son thème. **Le feu est couleur feu ; le vert Nyxessa est réservé à la relique et à son énergie.**

## Palettes de thème (25/09/2026, validées par l'utilisateur, vert Nyxessa émeraude conservé)

Source unique : `Assets/VFX/_Palettes/` — `VfxPalette.cs` (ScriptableObject : thème, teintes ordonnées de la plus sombre à la plus claire avec un rôle ombre / base / vif / cœur, accents nommés, matériaux Lit ciblés), un asset par thème (`Feu.asset`, `Nyxessa.asset`, …) et le registre `Resources/VfxPalettes.asset` (`VfxPalettes.cs`) qui les rend accessibles aux API statiques. Planche : `Assets/Screenshots/VfxPalettes.png`.

**Application** :
- Effets en gemmes et effets de Relic (scripts) : les couleurs sont lues à l'exécution par `VfxPalette.Couleur(theme, rôle, défaut)` / `VfxPalette.Accent(theme, nom, défaut)` (rampes mises en cache par `VfxPalette.Cache`, reconstruites quand une palette change). Les anciennes constantes restent en valeur de secours si le registre manque. Émission HDR des feux : `VfxPalette.Lueur(teinte, intensité)` (puissance 1,6 × intensité, reproduit les lueurs de Relic).
- Matériaux Lit (particules des flammèches et du cône, croix du soin, cristal de la relique) : listés dans le champ `materiaux` de la palette ; poussés par le menu **Deathless > VFX > Appliquer les palettes** (`_Palettes/Editor/VfxPaletteOutil.cs`), et automatiquement en éditeur quand on modifie un asset de palette.
- Changer une teinte dans un asset de palette recolore donc tous les effets du thème (nouvelles instances en Play, matériaux immédiatement).

| Thème | Teintes (rôle : nom `hex`) | Effets |
|---|---|---|
| **Feu** | ombre : braise `#4a1206` · base : rouge `#cc1f08` · vif : orange `#ff610a` · cœur : jaune `#ffe666` · accent : blanc chaud `#fff4d6` | boule de feu (référence Relic, valeurs identiques), explosion `LowPolyBlast`, flammes et braises `FireEffect`, **cône de flammes** et **flammèches du burn** (recolorés : base → particules « Sombre », vif → « Vif », cœur → « Clair ») |
| **Nyxessa** | ombre : émeraude sombre `#145032` · base : émeraude `#1e5a32` · vif : vert vif `#3fae5a` · cœur : vert clair `#9fe870` · accent : éclat `#e8ffc8` | gemme de la relique (`RelicMaterial.mat` = ombre, `RelicGem`), ceinture `RelicBelt`, portail `PortalVisual`, téléportation `PortalTransit`, missile crâne `SkullMissileVisual` (tiré par la relique), gerbes `GemBurst` (explosion / implosion / éclat) et traînée `GemTrail`. Les dégradés de Relic (4 stops) sont repris sur base → vif → cœur → éclat (× 1,25 en HDR) : le vert est plus émeraude que le vert citron de Relic |
| **Terre** | ombre : terre profonde `#3a281a` · base : terre sombre `#5b3f2a` · vif : terre claire `#8a6a48` · cœur : sable `#b8966c` · accent : pierre `#8c877f` | ondes de choc `OndeGemmes` (saut percutant, charge bélier), gerbe de mottes `DirtBurst` (sortie de terre) |
| **Rage** | ombre : rouge noir `#3a0a08` · base : rouge sombre `#6e1410` · vif : rouge vif `#b3261e` · cœur : rouge pâle `#ff7359` · accents : ivoire `#e8dcc0`, ivoire clair `#fff5e0`, fer `#5a5f66`, fer sombre `#3f444a`, fer clair `#7a8088` | rugissement du viking (`RugissementCrane` : crâne, dents et cornes ivoire, heaume en fer ; `RugissementOnde`) |
| **Sacre** (sacré) | ombre : nuit `#1e2a3a` · base : or sombre `#b8903a` · vif : or `#e8c872` · cœur : or clair `#f4e2a8` · accent : acier `#5a7aa0` | chevalier / paladin : charge bélier (`ChargeBelier` : bulle, traînée, éclat, éclair doré) |
| **Soin** | ombre : menthe profonde `#1b6a4c` · base : menthe sombre `#2e9e72` · vif : menthe `#4fcf9a` · cœur : menthe claire `#b8f5d8` | aura de soin (croix Lit : `AuraSoin_Menthe` = vif, `AuraSoin_MentheSombre` = base ; paillettes `AuraGemmes`) |
| **Os** (ajouté) | ombre : os gris `#999485` · base : os `#c7bfa8` · cœur : os pâle `#ebe6cc` · accent : magie `#8cff73` | désintégration à l'aube (`GemBurst.Rise`). Ni un élément ni la relique : de la poussière d'os ; l'accent vert est la magie Nyxessa qui quitte le squelette |
| **BouclierPlein / BouclierEntame / BouclierCritique** (ajoutés) | bleu `#0d2e73` `#1a66d9` `#4ca6ff` `#95bfff` + lueur `#59a6ff` · orange `#732e08` `#e67314` `#ffad40` `#ffca8a` + lueur `#ff9933` · rouge `#660a0a` `#d91f1a` `#ff594c` `#ff9f95` + lueur `#ff4033` | bouclier de la relique (`RelicShieldVisual`) : les trois thèmes codent la **vie restante** (> 40 %, 15-40 %, < 15 %), pas un élément ; cœur × 1,2 en HDR par le script (valeurs de Relic inchangées) |

Hors thèmes (inchangés) : fumée grise de `FireEffect`, éclair chaud de l'aube de `GemBurst.Rise`, émission de `RelicGlow` (champs du composant), ambiance jour/nuit (`Ambiance`).

## Lumière des effets (VfxLumiere, 25/09/2026)

Tous les sorts émettent de la lumière de la même façon : composant commun `Assets/VFX/_RelicCommun/VfxLumiere.cs` (lumière ponctuelle **sans ombre**). Couleur : thème de l'effet, **mi-chemin entre les rôles vif et cœur** (cœur seul si le thème n'a pas de vif, couleur imposée pour les états du bouclier). Enveloppe commune : **montée 0,08 s** (lissée), maintien, **extinction 0,4 s** (lissée) ; **scintillement léger pour le thème Feu seulement** (± 12 %, bruit de Perlin). `facteur` module l'intensité de l'extérieur (ouverture du portail, réactions de la relique, coups sur le bouclier). API : `VfxLumiere.Creer(parent, position, thème, taille, maintien)` (maintien < 0 : tenue jusqu'à `Eteindre()`), `VfxLumiere.Eclat(position, thème, taille, maintien)` (éclat d'impact, détruit après), `Allumer(maintien)`, `Eteindre()`, composant sur prefab avec `allumerAuDemarrage` ou `suivreParticules` (allumée tant que les ParticleSystem enfants émettent). Les anciennes lumières ad hoc (boule, explosion, gerbes, téléportation, missile, crâne du rugissement, portail, cristal, bouclier, feux de `FireEffect.Create`) sont remplacées ; seule la lanterne décorative `FireEffect.CreateLantern` garde sa lumière propre.

| Classe | Intensité | Portée |
|---|---|---|
| Petite | 1,5 | 3,5 m |
| Moyenne | 3 | 6 m |
| Grande | 6 | 11 m |

| Effet | Thème | Classe | Enveloppe |
|---|---|---|---|
| Boule de feu (en vol) | Feu | moyenne | tenue tant que le projectile existe |
| Explosion de la boule (`LowPolyBlast.Fire`) | Feu | grande | maintien 0,26 s |
| Cône de flammes | Feu | moyenne | suit les particules (`suivreParticules`) |
| Flammèches du burn | Feu | petite | suit les particules |
| Feux `FireEffect.Create` | Feu | petite / moyenne / grande selon l'échelle | tenue, `SetEmitting(false)` éteint |
| Missile magique | Nyxessa | petite (moyenne à l'échelle ≥ 1,25 : missile de la relique) | tenue, éteinte à l'éclatement |
| Gerbes `GemBurst` (explosion, implosion, éclatement, montée) | Nyxessa par défaut ; Os (désintégration), Sacre (charge), BouclierCritique (rupture du bouclier) | selon l'étendue (< 3 m petite, < 8 m moyenne, sinon grande) | maintien 0,1 s |
| Aura de soin | Soin | moyenne | maintien 0,6 s |
| Rugissement | Rage | moyenne | la durée de la séquence |
| Ondes de choc (impact) | Terre | petite | maintien 0,05 s |
| Sortie de terre (`DirtBurst`) | Terre | petite | maintien 0,08 s × force |
| Charge bélier (bulle) | Sacre | moyenne | tenue pendant la ruée, éteinte à l'impact |
| Téléportation (`PortalTransit`) | Nyxessa | moyenne | la durée du passage |
| Portail | Nyxessa | grande | tenue, `facteur` = ouverture / fermeture |
| Relique (cristal) | Nyxessa | grande | tenue, `facteur` = réactions (Nyxessa) |
| Bouclier de la relique | BouclierPlein / Entame / Critique (couleur imposée par la vie) | grande | tenue, `facteur` = présence du mur + éclat aux coups |

Capture de nuit : `Assets/Screenshots/VfxBench_lumieres.png`.

## Réactions de la relique Nyxessa (25/09/2026)

Composant `Nyxessa` (`Assets/VFX/GemmeNyxessa/Nyxessa.cs`) sur la racine du prefab `GemmeNyxessa` ; singleton léger `Nyxessa.Instance`, et `Nyxessa.Signaler(ReactionNyxessa type, Vector3 point)` qui ne fait rien s'il n'y a pas de relique (aucune dépendance dure). API : `Reagir(type, point)`, `TirerMissile(projectile, forme, matériau, échelle = 1,5)`.

| Événement (`ReactionNyxessa`) | Qui le signale | Réponse |
|---|---|---|
| `OuverturePortail` | `PortalVisual` à l'ouverture (`reagirRelique`) | ceinture qui s'élargit (`RelicBelt.Pulse`) et accélère (× 3, retour en 1,5 s), rotation du cristal accélérée, éclat de lumière (× 2,5), gerbe de gemmes au cristal (rayon 1,3 m) |
| `FermeturePortail` | `PortalVisual` à la fermeture | ceinture qui se resserre (`RelicBelt.Resserrer`) et ralentit (× 0,25, retour en 2 s), lumière qui baisse (jusqu'à × 0,35) puis revient, implosion de gemmes au cristal |
| `TirMissile` | `Nyxessa.TirerMissile` (au départ du missile) | cristal qui pulse (× 1,15) et recule de 18 cm à l'opposé du tir (0,3 s), éclat de lumière au point de départ, gerbe au départ |
| `PassageJoueur` | `PortalTransit.Depart` / `Arrive` (surcharges avec le portail) | petite onde qui fait le tour de la ceinture en 1,2 s (`RelicBelt.Parcourir` : gemmes soulevées de 12 cm et éclairées) avec un scintillement |

Ajouts : `RelicBelt.multiplicateurVitesse`, `Resserrer()`, `Parcourir()` ; `CrystalSpin.multiplicateur`, `decalage`. Captures : `VfxBench_nyxessa_ouverture.png`, `VfxBench_nyxessa_missile.png`.

### Charge de Nyxessa vers le portail (25/09/2026)

Nyxessa envoie une charge au portail pour l'ouvrir et la reprend à la fermeture (`ChargeNyxessa.cs`, `Nyxessa.EnvoyerCharge` / `ReprendreCharge`).
- **Flux** (`ChargeNyxessa.Lancer(départ, arrivée, durée, matériau, àLArrivée, hauteur = 2,5)`) : 240 gemmes (0,07-0,12 m, tête × 1,8) le long d'une Bézier cubique qui monte de 2,5 m au-dessus de la corde en son milieu (≈ 13,6 m du cristal au portail du banc) ; tête dense et claire (éclat → cœur), queue sur 38 % du trajet qui passe au vif puis à la base et s'éteint par la taille ; départ franc, léger freinage ; la queue se résorbe dans la cible en 0,35 s après la tête. Une `VfxLumiere` (Nyxessa, moyenne) voyage avec la tête et s'éteint à l'arrivée.
- **Ouverture** : réaction d'ouverture de Nyxessa (ceinture qui s'élargit et accélère, éclat, gerbe), puis flux du cristal au centre du portail en `dureeCharge` (0,7 s) ; **le portail s'ouvre à l'arrivée** (ouverture habituelle + goutte d'eau `Entrer()`).
- **Fermeture** : le portail se referme aussitôt (implosion habituelle) et le flux part du portail vers le cristal le long du même arc ; **la réaction de fermeture (ceinture qui se resserre, lumière qui baisse, implosion) se joue à l'arrivée** au cristal.
- **Réglage sur le portail** : `PortalVisual.alimenteParNyxessa` (vrai sur `PortailDonjon.prefab`, le portail du village vers le donjon ; faux pour le portail de retour du donjon et pour le portail imbriqué de `Teleportation.prefab`) et `dureeCharge`. La charge n'est jouée que si le portail est alimenté **et** qu'une relique existe (`Nyxessa.Instance`) ; sinon ouverture et fermeture immédiates comme avant. Séquence côté portail (rappel à l'arrivée), sans dépendance dure.
- Captures : `VfxBench_charge_nyxessa_ouverture.png` (flux à mi-chemin), `VfxBench_charge_nyxessa_arrivee.png` (portail qui s'ouvre), `VfxBench_charge_nyxessa_fermeture.png` (flux qui revient).

## Dossiers communs

- `Assets/VFX/_RelicCommun/` : `LowPolyGem` (écriture d'une gemme dans un maillage partagé), `GemShape` (ScriptableObject : nuage de points cuit), `GemBurst` (`Explode`, `Implode`, `Shatter`, `Rise`), `GemTrail` (`Follow`), `FireballVisual` (`Attach`, `SpawnEmber`), `FireballEmber`, `LowPolyBlast` (`Fire`, `Spawn`), `DirtBurst` (`Spawn`), `AreaBurst` (`Spawn`), `FireEffect` (`Create`, `SetEmitting`), `WavyTrail` (copié, plus utilisé par la boule actuelle), `Ambiance` (presets jour/crépuscule), shader `VertexColorUnlit`, `PortalVoxel.mat`, `FireBurst.mat`.
- `Assets/VFX/_Ambiance/DayCycle.cs` : seul fichier d'ambiance repris, parce que `RelicGem` et `RelicGlow` lisent `DayCycle.Night` (statique). Copie « Visual only » (`nuit` 0/1 et `heure` 0-1 à la place de `RunProgress`). Sans `DayCycle` dans la scène, `Night` vaut 0 (jour). L'asset `Ambiance.asset`, le ciel, le sol et la brume `GroundMist` du labo ne sont pas repris.

## Fiches

### Gemme Nyxessa + ceinture (Relic)
- **Prefab** : `Assets/VFX/GemmeNyxessa/GemmeNyxessa.prefab` (rocher Forest `Rock_1_O_Color1` au sol, `Crystal` à 5,68 m au-dessus de la racine, échelle 1,3 × 2 × 1,3, lumière ponctuelle).
- **Scripts** : `RelicGem` (gemme à 7 pans irréguliers, remplace `Crystal.asset` au démarrage), `RelicGlow`, `RelicBelt` (650 gemmes en bande horizontale, rayon 1,5 m, largeur 0,45, épaisseur 0,14), `CrystalSpin`.
- **API** : `RelicBelt.Pulse()` (impulsion de l'anneau, à l'ouverture du portail à l'aube) ; `RelicBelt.portailOuvert` (l'impulsion part au passage faux → vrai). Nuit : `DayCycle.Night >= 0.5`.
- **Dépendances** : `LowPolyGem`, `DayCycle`, `PortalVoxel.mat`, `RelicMaterial.mat`, `KayKit_Forest.mat` + `forest_texture.png` (copiés dans le dossier, pas dans `Assets/Art/`).
- **Palette** : thème **Nyxessa** (matériau `RelicMaterial` = ombre ; ceinture : ombre → base → vif → cœur → éclat × 1,15).

### Portail de donjon (Relic)
- **Prefab** : `Assets/VFX/PortailDonjon/PortailDonjon.prefab` (racine à **y = 1,75** depuis le 25/09/2026, tournée de 90° ; `BoxCollider` agrandi à 1 × 1,185 × 1,185 (× 1,185 comme le rayon) ; anciens quads `Glow` / `SwirlBack` / `SwirlFront` masqués au démarrage, gardés pour la fidélité).
- **Script** : `PortalVisual` (Visual only) : disque de 2 400 gemmes, cellule 0,1 m ; ouverture 1,3 s (gerbe), fermeture 0,9 s (implosion). **Paramètres exposés** (25/09/2026) : `radius` = **1,6 m** (1,35 dans Relic, + 18 %) ; `epaisseur` = **0,35** (profondeur visible au centre en fraction du diamètre, gemmes et houle comprises ; profil de disque épais à faces presque plates et bord arrondi, `(1 − r⁴)^0,5`, au lieu de la lentille `1 − r²` de Relic). Mesuré sur le maillage : avant ≈ 1,15 m de profondeur pour 2,7 m de diamètre (≈ 43 %) ; après **1,10 m pour 3,1 m (35 %)**. `reagirRelique` : prévenir Nyxessa à l'ouverture / fermeture.
- **API** : `PortalVisual.ouvert` (remplace `Portal.IsOpen`), **`Entrer()`** (goutte d'eau : trois anneaux bien séparés partent du centre vers le bord sur la surface du disque, crête claire suivie d'un creux sombre, amortis en 1,7 s ; creux au centre qui rebondit ; paramètres `goutteLongueurOnde` = **0,6 m** entre deux anneaux (× 2 par rapport à la première version), `goutteDecalage` = **0,32 s** entre deux départs, vitesse = longueur d'onde / décalage ≈ 1,9 m/s, `goutteAnneaux` = 3), **`Sortir()`** (l'inverse : les anneaux partent du bord, convergent vers le centre et s'y résorbent, petite bosse au centre à la fin), `Ripple()` (alias d'`Entrer()`, compatibilité Relic), `Center` (centre de la soupe). Captures : `VfxBench_portail.png` (3/4), `VfxBench_portail_entree.png`, `VfxBench_portail_sortie.png`.
- **Dépendances** : `LowPolyGem`, `GemBurst`, `PortalVoxel.mat` (+ `FireBurst`, `TrailGlow`, `TrailSmoke` référencés mais plus utilisés).
- **Palette** : thème **Nyxessa** (base, vif, cœur, éclat × 1,25 ; lumière = cœur).

### Téléportation par le portail (Relic)
- **Prefab** : `Assets/VFX/Teleportation/Teleportation.prefab` (contient un `PortailDonjon` imbriqué ; le corps factice du labo a été retiré).
- **Script** : `PortalTransit` (copie telle quelle).
- **API** : **`PortalTransit.Depart(Bounds corps, PortalVisual portail, Material gemmes, 1.1f)`** (déclenche `portail.Entrer()` et `Nyxessa.Signaler(PassageJoueur)`) puis, à l'arrivée, **`PortalTransit.Arrive(corps, portailSortie, gemmes, 1.0f)`** (`portail.Sortir()` + réaction) ; les anciennes surcharges avec un `Vector3` centre restent (sans goutte ni réaction) ; masquer le corps pendant le transit (séquence de `PlayerZone.Transit` dans Relic). Volume du joueur : 0,8 × 1,9 × 0,8.
- **Dépendances** : `LowPolyGem`, `PortalVisual`, `PortalVoxel.mat`.

### Boule de feu (Relic)
- **Prefab** : `Assets/VFX/BouleDeFeuRelic/BouleDeFeuRelic.prefab` : racine vide (le composant de démo portait la séquence). L'effet est entièrement dans les API statiques.
- **API** : en vol, `FireballVisual.Attach(projectile, FireBurst)` (corps à facettes, noyau, lumière, braises `FireballEmber`) ; à l'impact, `LowPolyBlast.Fire(point, 2.5f, FireBurst)` + fumée `FireEffect.Create(null, point, 1f, 0.8f, FlameParticle, SmokeParticle, false, true, false)`, émission 0,4 s, détruite à 3 s (= `SkillEffects.Burst` de Relic).
- **Paramètres (VfxRecorder de Relic)** : 18 m/s, chute 5, +1,5 m/s vers le haut, départ 2,5 m, 1,3 s ou jusqu'au sol.
- **Dépendances** : `FireballVisual`, `FireballEmber`, `LowPolyBlast`, `FireEffect`, `FireBurst.mat`, `FlameParticle.mat` + `flame_soft.png`, `SmokeParticle.mat` + `smoke_soft.png` (`TrailGlow`, `TrailSmoke` copiés pour le portail).
- **Palette** : thème **Feu** (référence : cœur jaune, vif orange, base rouge ; émission par `VfxPalette.Lueur`).
- Retenue par l'utilisateur le 25/09/2026 (la version du bac à sable est abandonnée).

### Missile magique, crâne en gemmes (Relic)
- **Prefab** : `Assets/VFX/MissileMagique/MissileMagique.prefab` : racine vide (idem boule de feu).
- **Script** : `SkullMissileVisual` (Visual only, sans `GameAudio`).
- **API** : tir par la relique : **`Nyxessa.Instance.TirerMissile(projectile, SkullGemShape, PortalVoxel, 1.5f)`** (éclat au départ, réaction de la relique, crâne à l'échelle 1,5) ; bas niveau : `GemBurst.Explode(bouche + dir * 0.3f, 0.5f, PortalVoxel)` puis `SkullMissileVisual.Attach(projectile, SkullGemShape, Vector3.zero, 0.55f, PortalVoxel, echelle)` ; à l'arrivée, `Shatter()`.
- **Paramètre `echelle`** (dernier argument d'`Attach`, défaut **1**) : échelle d'ensemble du missile — taille du crâne, taille des gemmes, frémissement, taille de la traînée `GemTrail`, vitesse d'éclatement (× √échelle), classe de lumière (moyenne à partir de 1,25). **1,5** pour le missile tiré par la relique (banc : `VfxBench.echelleMissile`), **1** pour le missile du nécromancien (inchangé s'il est repris).
- **Paramètres** : 12 m/s, 1,6 s ; forme cuite `SkullGemShape.asset` (1 100 points).
- **Dépendances** : `GemShape`, `GemTrail`, `GemBurst`, `LowPolyGem`, `PortalVoxel.mat` (`GhostSkull.mat` copié pour mémoire).

### Bouclier de la relique (Relic)
- **Prefab** : `Assets/VFX/BouclierRelique/BouclierRelique.prefab` (racine à y = 0,8 : `BasePosition` = racine − 0,8 m).
- **Scripts** : `RelicShieldVisual` (900 gemmes en cylindre), `RelicShieldEtat` (remplace le `RelicShield` réseau, même API).
- **API** : `RelicShieldEtat.Lever()` (incantation 3 s, les gemmes montent du sol), `Frapper(dégâts, point)` (onde depuis l'impact, couleur selon la vie), `Baisser()` ; rupture automatique à 0 PV (`RelicShieldVisual.Shatter()`). Dans Deathless, brancher ces appels sur l'état serveur.
- **Paramètres (GameBalance de Relic)** : rayon 5,5 m, hauteur 6 m, 300 PV, seuils orange 0,4 et rouge 0,15.
- **Palette** : thèmes **BouclierPlein** → **BouclierEntame** → **BouclierCritique** selon la vie.

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
- **Palette** : thème **Feu** (recoloré le 25/09/2026 : sort de feu du mage) — `Sombre` = rouge `#cc1f08`, `Vif` = orange `#ff610a`, `Clair` = jaune `#ffe666` (URP Lit, par l'outil Appliquer les palettes).

### Cône de flammes (bac à sable)
- **Prefab** : `Assets/VFX/ConeDeFlammes/ConeDeFlammes.prefab`, à parenter à la main du mage, émission le long de +Z local.
- **API** : trois ParticleSystem bouclés : actif pendant le sort, `Stop()` au relâchement.
- **Paramètres** : mesh `Flamme.asset` (tétraèdre), cône de 30° d'ouverture, rayon 6 cm, 5 à 7,4 m/s × 0,7 à 1 s ≈ 6 m de portée, taille 0,25 → 1 → 0.
- **Palette** : thème **Feu** (recoloré le 25/09/2026) — `Sombre` (gros, lent) = rouge `#cc1f08`, `Vif` = orange `#ff610a`, `Clair` (petit, rapide) = jaune `#ffe666` (URP Lit).

### Aura de soin (bac à sable)
- **Prefab** : `Assets/VFX/AuraSoin/AuraSoin.prefab`, au sol sous l'allié (racine à y = −1 : prévue pour être enfant du centre d'une capsule de 2 m).
- **Scripts** : `AuraSoin` (déclencheur écrit pour Deathless, reprend `AuraSoinDemo` sans la boucle), `AuraGemmes` (paillettes en gemmes).
- **API** : `AuraSoin.Jouer()` au soin (croix + paillettes, ~1 s).
- **Paramètres** : 6 à 8 croix `Croix.asset` (grille 3 × 3 de cellules de 0,14 m), disque de 1,1 m parcouru en 0,3 s, montée 1,3-2 m/s ; 20 à 40 gemmes de 0,05-0,09 m, montée 0,9-1,4 m/s, durée 1 s. `Fragment.asset` copié mais inutilisé.
- **Palette** : thème **Soin** (menthe `#4fcf9a`, menthe sombre `#2e9e72` ; croix en URP Lit, paillettes en couleurs par sommet).

### Rugissement du viking (bac à sable)
- **Prefab** : `Assets/VFX/Rugissement/Rugissement.prefab`, racine au centre de la capsule du personnage (le crâne `Crane` est à 1,65 m au-dessus), visage vers +Z.
- **Scripts** : `Rugissement` (déclencheur écrit pour Deathless, séquence reprise de `RugissementDemo` sans la boucle), `RugissementCrane` (crâne barbare en gemmes : heaume, cornes, mandibule aux condyles), `RugissementOnde` (anneau de 72 gemmes).
- **API** : `Rugissement.Jouer()` ; `Appliquer(t)` pose un instant donné ; `DureeTotale` ≈ 1,52 s. Bas niveau : `RugissementCrane.Ouverture` (0..1), `Bascule` (0..1), `RugissementOnde.Jouer()`.
- **Paramètres** : pop 0,12 s, ouverture 0,2 s (28°, tremblement 0,3 s, tête rejetée de 13°), onde 0,9 s jusqu'à 4,5 m puis retour, fermeture 0,15 s, rétraction 0,15 s.
- **Dépendances** : `SkullGemShape.asset` (dossier `MissileMagique`), `GemShape`, `LowPolyGem`, `Rugissement_Gemmes.mat`.
- **Heaume (25/09/2026)** : la référence est le **casque du `Skeleton_Warrior` KayKit** (`Assets/VFX/SortieDeTerre/Skeleton_Warrior.fbx`, sous-objet `Skeleton_Warrior_Helmet`, maillage séparé sous l'os `head`). Comparés dans le pack `KayKit_Skeletons_1.1_FREE` de Relic : Mage (chapeau pointu), Rogue (capuche), Minion (pas de casque) : seul le Warrior a un heaume (calotte, bandeau, crête d'épines, cornes courtes), retenu. Sa surface est cuite en **1 800 positions de gemmes** (`HeaumeGemShape.asset`, outil `Assets/VFX/_RelicCommun/Editor/GemShapeBaker.cs`, copie de l'outil de Relic avec filtre par sous-objet, menu **Deathless > VFX > Heaume du rugissement en gemmes** ; luminosité de la texture et occlusion comme pour `SkullGemShape`). `RugissementCrane` pose ce heaume à la place du heaume procédural (calotte en écailles, nasal) et **des cornes Bézier** (le casque KayKit a ses propres cornes, plus courtes) : champs `heaume`, `heaumeEchelle` (1,35), `heaumeDecalage` (0 ; 0,24 ; −0,03 dans le repère normalisé du crâne), `heaumeTailleGemme` (1,3) ; les gemmes du crâne qu'il recouvre (dessus, côtés, nuque, hors visage) sont retirées. Couleurs par la texture : fer sombre / fer / fer clair pour la calotte et le bandeau, ivoire / ivoire clair pour les cornes et les pointes (palette Rage). Crâne, mâchoire, attitude et onde inchangés ; sans `heaume`, le rendu procédural d'origine revient. Captures : `Rugissement_casque_reference.png` (casque KayKit source : face, profil, 3/4), `VfxBench_rugissement.png`, `VfxBench_rugissement_profil.png`.
- **Palette** : thème **Rage** (rouge vif `#b3261e`, rouge sombre `#6e1410`, rouge noir `#3a0a08`, accents ivoire `#e8dcc0` et fer `#5a5f66` / `#3f444a` / `#7a8088`).

### Ondes de choc (bac à sable)
- **Prefabs** : `Assets/VFX/OndeDeChoc/OndeDeChoc.prefab` (base), variantes `OndeDeChoc_SautPercutant.prefab` (cercle, rayon 4,5 m, 0,75 s) et `OndeDeChoc_ChargeBelier.prefab` (arc de 100° devant, +Z local, rayon 5 m, 0,6 s, anneau plus large au départ). Racine des variantes à y = −1 (enfant du centre de la capsule).
- **Scripts** : `OndeDeChoc` (paramètres `rayonMax`, `duree`, `angleOuverture`, `rayonDepart`, `largeurDepart/Fin`), `OndeGemmes` (rendu).
- **API** : `OndeDeChoc.Jouer()` ; `Appliquer(t)` pour une capture.
- **Paramètres** : anneau de gemmes (14 par mètre de périmètre, plafond 420, taille 0,13 → 0,06 m), 26 gros éclats + 16 petits, coup de pied vertical 3,8 m/s, gravité 0,7 g.
- **Palette** : thème **Terre** (terre sombre `#5b3f2a`, terre claire `#8a6a48`).

### Charge bélier (créé dans Deathless, 25/09/2026)
- **Prefab** : `Assets/VFX/ChargeBelier/ChargeBelier.prefab` (script `ChargeBelier` + enfant `Onde` = `OndeDeChoc_ChargeBelier` imbriqué), matériau `ChargeBelier_Gemmes.mat` (copie de `PortalVoxel`, shader `Relic/VertexColorUnlit`).
- **API** : `ChargeBelier.Jouer(Transform porteur, Vector3 direction, float distance)` au début de l'élan ; la bulle suit le porteur (orientée sur `direction`, horizontale) ; quand il a parcouru `distance` m le long de `direction`, **impact** automatique : la bulle éclate vers l'avant (`GemBurst.Shatter`, chaque gemme part de sa place, + 6 m/s vers l'avant, éclair doré) et l'onde part 0,5 m devant lui. `Impact()` force l'impact (obstacle) ; sécurité à 3 s. La translation du porteur est faite par le gameplay (le banc la fait par script).
- **Bulle** : tête de bélier procédurale en gemmes `LowPolyGem` (~2,2 m de long, 1,6 m de haut, 1,3 m de large ; crâne arrondi derrière, museau allongé qui descend vers l'avant, orbites sombres cerclées, arcades acier, naseaux), gemmes clairsemées sur les flancs (le personnage reste visible) et denses sur les lignes de contour (arête dorsale, flancs, mâchoire, bord arrière, anneau du museau) ; deux cornes en spirale dans le plan vertical de chaque côté (1,2 tour : haut → arrière → bas → avant, rayon 0,48 → 0,13 m, section 0,14 → 0,03 m, bandes or / or sombre, pointe or clair), qui s'écartent de la tête en s'enroulant. Pop 0,1 s (× 1,12 puis 1), léger frémissement.
- **Traînée** : toutes les 8 cm parcourus, 4 gemmes prises sur la bulle restent en place, montent de 15 cm, tournent et s'éteignent par la taille en 0,5 s.
- **Palette** : thème **Sacre** (or `#e8c872` majoritaire, or sombre `#b8903a`, acier `#5a7aa0` en minorité, orbites `#1e2a3a`, pointe des cornes et arête `#f4e2a8`).

## Banc `VfxBench`

Scène `Assets/Scenes/VfxBench.unity` : sol (`RigTest_Ground.mat`), lumière directionnelle, caméra en surplomb, deux rangées de postes avec un label TextMesh au sol. Fond : effets de Relic sans personnage (gemme + missile tiré par la relique vers un squelette, portail, téléportation, bouclier, désintégration, sortie de terre). Devant : un geste de personnage par effet, synchronisé avec l'effet.

- **Personnages** : `Mannequin_Medium.fbx` (pack Character Animations 1.1), équipés en édition par `Assets/Scripts/Dev/MannequinEquip.cs` (`MannequinEquip.Equiper(personnage, WeaponStyle)` : crée les sockets `handslot.r/.l` manquants depuis `requiredSockets`, puis instancie les pièces de `attachments` sous leur os ; même méthode que `WeaponBench` et `ClassGear.AttachTo` de Relic). Cibles : `Skeleton_Warrior` (échelle 0,75, `KayKit_Skeleton_Enemy.mat`), en pose de fin de `Skeletons_Spawn_Ground`.
- **Animation** : pas de contrôleur ; `VfxBench.cs` pose chaque mannequin par un `PlayableGraph` en mise à jour manuelle (couche de base à deux clips mélangés, fondu 0,15 s, et couche « haut du corps » masquée sur `chest` et ses descendants). Le temps du clip est piloté par le banc, donc l'effet part exactement à l'instant clé.
- **Instants clés** : mesurés au démarrage en échantillonnant le clip à 120 images/s sur le mannequin équipé (`VfxBench.MesurerInstants`), exposés par `TempsImpactSaut`, `TempsCoupBouclier`, `TempsTir`, `TempsPoussee`, `TempsSoin`, `TempsCri`.
- `ToutJouer()` relance tous les postes au même instant ; `Capturer(chemin, délai, position, visée, fov)` relance tout et enregistre une image 1600 × 1000 après le délai (caméra principale si fov ≤ 0). Les durées de vol et intervalles du banc sont des réglages de banc, pas de l'effet.

| Effet | Personnage (style) | Clip(s) | Instant de l'effet (mesuré) |
|---|---|---|---|
| Saut percutant (**bond vers l'avant**) | mannequin, hache à deux mains (`Axe2H`) | `Melee_2H_Idle` → `Melee_1H_Attack_Jump_Chop` (1,33 s, CombatMelee) | clip « in place » (aucune courbe sur la racine) : la racine est translatée par script pendant la phase aérienne mesurée, **décollage t = 0,19 s → atterrissage t = 0,69 s** (hanches : 15 % de l'amplitude au-dessus de l'accroupi, puis retour à la hauteur de repos), 5 m à vitesse horizontale constante + arc vertical de 0,6 m (4k(1−k)) ajouté au saut du clip (sommet t = 0,43 s) ; `OndeDeChoc_SautPercutant` au point où la lame (fer −X à 0,92 m du manche) touche le sol, devant le mannequin : **t = 0,79 s** ; retour discret au départ à chaque boucle |
| Rugissement | mannequin, hache + bouclier (`AxeShield`) | `Idle_A` → `Skeletons_Taunt_Longer` (3,0 s, `Rig_Medium_Special`, pack Skeletons : accroupi, bond, tête rejetée en arrière) | `Rugissement.Jouer()` 0,32 s avant que la tête soit rejetée (tête au plus en arrière à **t = 1,63 s**), gueule ouverte à cet instant ; racine du prefab à 1,4 m (crâne ~0,5 m au-dessus de la tête du mannequin) |
| Charge bélier (**ruée**) | mannequin, épée + bouclier (`SwordShield`) | anticipation 0,18 s (corps penché de 14°, `Melee_Blocking` sur le haut du corps) ; ruée de **7 m en 0,5 s** (racine translatée : départ franc, léger freinage), jambes `Running_A` × 1,8, haut du corps en garde puis `Melee_Block_Attack` calé pour que le coup porte à l'arrivée ; suite de `Melee_Block_Attack` en corps entier | `ChargeBelier.Jouer(chevalier, axe, 7)` au début de l'anticipation (bulle en tête de bélier + traînée) ; à l'arrivée (coup de bouclier le plus en avant, **t = 0,47 s** du clip), éclatement de la bulle vers l'avant + `OndeDeChoc_ChargeBelier` devant lui ; retour discret au départ |
| Aura de soin | paladin mannequin, épée + bouclier (`SwordShield`) ; allié mannequin sans arme à 1,7 m | `Ranged_Magic_Raise` (2,1 s, épée levée) | `AuraSoin.Jouer()` sous l'allié quand l'épée atteint le haut : **t = 0,60 s** |
| Cône de flammes + burn | mage mannequin, bâton (`Staff`) ; cible squelette à 4,5 m | `Ranged_Magic_Spellcasting_Long` (2,53 s) jusqu'à la poussée du bâton, pose tenue 3 s (léger balancement), fin du clip | cône à la pointe du bâton (0, 1,2, 0 dans `staff`), visant la poitrine de la cible, de la poussée (**t = 1,57 s**, pointe au plus en avant) pendant 3 s ; `BurnFlammeches` sur la cible 0,3 s après le début, arrêt 1,5 s après la fin du cône |
| Boule de feu | mage mannequin, bâton (`Staff`) ; cible squelette à 8 m | `Ranged_Magic_Shoot` (0,93 s) | boule créée à la pointe du bâton quand elle est au plus en avant, **t = 0,31 s**, vol droit à 18 m/s vers la poitrine de la cible, puis `LowPolyBlast.Fire` + fumée |
| Téléportation | mannequin sans arme | `Idle_A`, `Walking_A` (1,2 s vers le portail) | `PortalTransit.Depart(…, portail, …)` en fin de marche (goutte d'entrée, réaction de la relique), `Arrive(…, portail, …)` 1,1 s après de l'autre côté (goutte de sortie) |
| Missile magique | la relique (cristal de la gemme) ; cible squelette | — | `Nyxessa.TirerMissile` (échelle 1,5 : éclat, pulsation et recul du cristal), crâne à 12 m/s vers la poitrine du squelette, `Shatter` à l'arrivée |
| Portail | — | — | cycle : ouvert ; fermeture demandée à 5 s (le portail se ferme, la charge revient au cristal) ; ouverture demandée à 7,5 s (charge envoyée, portail ouvert à son arrivée, ≈ 8,2 s) |
| Gemme, portail, bouclier | — | — | inchangés (voir fiches) |
| Désintégration, sortie de terre | squelettes | `Skeletons_Spawn_Ground` (sortie) | inchangés |

Captures (Play, `Assets/Screenshots/`) : `VfxBench.png` (vue d'ensemble), `VfxBench_saut_percutant_vol.png` (sommet du bond), `VfxBench_saut_percutant.png` (à l'impact), `VfxBench_rugissement.png`, `VfxBench_charge_elan.png` (milieu de la ruée : bulle + traînée), `VfxBench_charge_impact.png` (éclatement + onde), `VfxBench_cone.png`, `VfxBench_burn.png`, `VfxBench_boule.png`, `VfxBench_nyxessa.png` (gemme, portail, téléportation), `VfxBench_missile.png`, `VfxBench_nyxessa_ouverture.png`, `VfxBench_nyxessa_missile.png`, `VfxBench_portail.png`, `VfxBench_portail_entree.png`, `VfxBench_portail_sortie.png`, `VfxBench_rugissement_profil.png`, `VfxBench_lumieres.png` (pénombre), `VfxBench_charge_nyxessa_ouverture.png`, `VfxBench_charge_nyxessa_arrivee.png`, `VfxBench_charge_nyxessa_fermeture.png` ; planche des palettes `VfxPalettes.png` ; casque de référence `Rugissement_casque_reference.png`. Le banc ralentit le temps (× 0,25) pendant l'attente d'une capture (`ralentiCapture`) pour que les à-coups de l'éditeur ne décalent pas l'instant.

Écarts assumés : pas de clip de saut frappé à deux mains dans le pack (le saut percutant utilise le seul saut frappé, `Melee_1H_Attack_Jump_Chop` : la hache 2H y est tenue de la main droite, la gauche ne tient pas le manche) ; pas de clip de cri/rugissement dans les packs Adventurers/Animations (le rugissement emprunte `Skeletons_Taunt_Longer` du pack Skeletons, même squelette Rig_Medium) ; pas de clip de charge ni de sprint (course accélérée + garde en couches, puis coup de bouclier) ; aucun clip de saut n'a de root motion (bond et ruée translatés par script).

## Points ouverts

- Tranché le 25/09/2026 : le feu est couleur feu (cône et flammèches recolorés en thème Feu, aligné sur la boule de Relic) ; le vert Nyxessa est réservé à la relique. À valider : le thème Nyxessa (hex de l'utilisateur) rend le portail, la ceinture et les gerbes plus émeraude que le vert citron de Relic ; ajuster `Nyxessa.asset` si besoin.
- Les prefabs `BouleDeFeuRelic`, `MissileMagique`, `Desintegration`, `SortieDeTerre` sont des racines vides : l'effet vit dans les API statiques ; le code de jeu (sorts, ennemis) devra porter la séquence (voir `VfxBench.cs`).
- `RelicGem` / `RelicGlow` / `RelicBelt` / `PortalVisual` / `RelicShieldEtat` gardent les champs « Visual only » du bac à sable (`DayCycle.Night`, `portailOuvert`, `ouvert`, `Lever/Frapper/Baisser`) : à brancher sur l'état de partie de Deathless.
