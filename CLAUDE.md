# Deathless — projet principal

Jeu Unity 6000.3.24f1 (URP, Input System). Successeur de Relic (`C:/Dev/Unity/Relic`, lecture seule : référence, jamais modifié).

**Règle d'équipement** : le lien est **arme / style de jeu → animation**, jamais personnage → animation. Un style (épée + bouclier, bâton, dague + arbalète, hache + bouclier, hache à deux mains, arc + carquois) définit ses sockets, ses offsets et son set de clips ; tout personnage au squelette KayKit Rig_Medium avec les sockets `handslot.r` / `handslot.l` le porte sans réglage.

Styles d'armes validés le 24/09/2026 dans le bac à sable `sandbox-rig` et reportés ici : récapitulatif dans `Docs/styles-d-armes.md`, données dans `Assets/WeaponStyles/` (assets `WeaponStyle`, contrôleurs, clips bouclés, fiches), scripts dans `Assets/Scripts/` (`WeaponStyle`, `BowStance`, `AltWeaponSwitch`, `HelmetVisor`), banc de vérification `Assets/Scenes/WeaponBench.unity`.
