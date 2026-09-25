# Deathless — projet principal

Jeu Unity 6000.3.24f1 (URP, Input System). Successeur de Relic (`C:/Dev/Unity/Relic`, lecture seule : référence, jamais modifié).

**Règle d'équipement** : le lien est **arme / style de jeu → animation**, jamais personnage → animation. Un style (épée + bouclier, bâton, dague + arbalète, hache + bouclier, hache à deux mains, arc + carquois) définit ses sockets, ses offsets et son set de clips ; tout personnage au squelette KayKit Rig_Medium avec les sockets `handslot.r` / `handslot.l` le porte sans réglage.

Styles d'armes validés le 24/09/2026 dans le bac à sable `sandbox-rig` et reportés ici : récapitulatif dans `Docs/styles-d-armes.md`, données dans `Assets/WeaponStyles/` (assets `WeaponStyle`, contrôleurs, clips bouclés, fiches), scripts dans `Assets/Scripts/` (`WeaponStyle`, `BowStance`, `AltWeaponSwitch`, `HelmetVisor`), banc de vérification `Assets/Scenes/WeaponBench.unity`.

**Effets visuels** transférés du bac à sable `sandbox-vfx` le 25/09/2026 : récapitulatif par effet (prefab, scripts, API de déclenchement, dépendances, palette) dans `Docs/vfx.md`, assets sous `Assets/VFX/` (mêmes chemins et GUID que le bac à sable ; communs dans `Assets/VFX/_RelicCommun/`), banc `Assets/Scenes/VfxBench.unity` (script `Assets/Scripts/Dev/VfxBench.cs`). Langage visuel commun : **gemmes low poly à couleurs par sommet** (`LowPolyGem`, un seul maillage dynamique par effet, pas d'alpha, apparition et disparition par la taille) rendues par le shader **`Relic/VertexColorUnlit`** (`PortalVoxel.mat` ou une copie) ; palette réduite par effet (2 à 5 teintes) ; les particules du bac à sable restent en URP Lit flat shading.
