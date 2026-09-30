# Le donjon

Le donjon est décrit dans le [Déroulé d'une partie](deroule.md) (portails, butin, or porté, rappel par Nyxessa). Cette page fixe les **règles de construction** du lieu lui-même : ce qu'un joueur doit voir, et ce qu'il ne doit plus voir. Les balcons et les niveaux (« des balcons plus que des étages ») restent une décision de conception à venir {à confirmer}.

## Ce qu'on voit {décidé}

- **Un lieu clos, sans ciel** : sombre, éclairé aux torches, un fond de brouillard. Les murs d'enceinte font toute la hauteur et un toit ferme l'enceinte : on ne voit jamais le vide à travers un mur, une fenêtre ou un plancher.
- **Des murs pleins** : un mur est plein par défaut. Une porte ou une arche n'existe que sur un **passage** ; les passages du donjon sont les **arcades de bois** sous les balcons et les mezzanines. Il n'y a donc **aucune porte fermée** et aucune arche bouchée dans un mur.
- **Des fenêtres fermées, rares** : seulement sur les murs d'enceinte des étages (rien derrière), volets clos, au plus une par pan de trois cellules, jamais sous une torche.
- **Des murs cassés, rares** : un pan sur quinze, jamais deux côte à côte ; sur l'enceinte, un mur plein derrière le trou, pour qu'on voie de la pierre et non le noir.
- **Des murets** : entre deux halls du rez, quelques murets pleins d'un mètre (bloc de pierre), jamais un mur écrasé.
- **Le dessous des étages est un plafond** : sous chaque plancher d'étage, une dalle de plafond ; sous une mezzanine ou la tour, une lumière d'appoint (torchère).
- **Les ossements décorent, ils n'accueillent pas** : un ossement est posé près de chaque point d'apparition, à 1,6 m au moins ; un squelette ne sort plus de terre à travers un crâne. Les tonneaux et caisses des coins ne sont jamais sur un point d'apparition.
- **Un coffre ouvert est vide** : à l'ouverture, une fois l'or crédité, le tas de pièces s'envole en gemmes d'or ; il ne reste rien « à ramasser ». Il n'y a plus de tas d'or au sol (30/09/2026, voir [Déroulé d'une partie](deroule.md)).
- **La dalle d'arrivée** : une dalle de bois sombre marque le point d'arrivée. Les grilles d'égout ne sont plus tirées au hasard : une par bloc de hall au plus, jamais sous un point d'apparition ni à l'arrivée.
- **La caméra reste dans l'enceinte** : jamais par-dessus le mur d'enceinte.
- **Découpe autour du héros** {décidé, 30/09/2026} (procédé dit « see-through » ou découpe d'occlusion) : au donjon, la caméra garde sa distance au lieu de se coller aux murs ; les murs, poutres et plafonds qui passent entre elle et le héros sont découpés dans un disque centré sur lui (bord en damier, sans transparence). Le sol qu'il foule et tout ce qui est derrière lui restent pleins. Rayon {à équilibrer} (un quart de la hauteur de l’écran). {{dev: shader `Deathless/DonjonDecoupe` (`Assets/Art/Shaders/DonjonDecoupe.shader` + `DonjonDecoupeCommun.hlsl`, variante d'URP Lit sur le modèle de `ForetDither` ; passes couleur, profondeur et normales découpées, ombre pleine), matériau `KayKit_Dungeon_Decoupe.mat` (`DonjonKit.materiauDecoupe`) et copies à la volée des autres matériaux Lit dans `DonjonGenerateur.Decoupe` ; globales `_DecoupeCentre` / `_DecoupeRayon` posées par `CameraEpaule.PoserDecoupe` ; `GameBalance.cameraDecoupeRayon` 0,24.}}
- **On ne frappe pas à travers les murs** {décidé, 30/09/2026} : un coup de mêlée ou de zone ne touche qu'un ennemi du même étage (1,8 m d'écart au plus) et sans mur, sol ni plafond entre le torse du héros et le sien. {{dev: `Combat.Ennemis` : `EcartHauteurMax` et `Combat.Degage` (rayon, personnages et feuillage ignorés) ; vaut partout, pas seulement au donjon.}}
- **Le HUD** : le message du donjon (« Rappelé par Nyxessa… », « N or versés… ») s'affiche sous la pastille de phase et sous la bannière de nuit, jamais dessus ; l'invite « Prêts » du vote n'apparaît pas au donjon.

## Gardiens {décidé, 30/09/2026}

Les gardiens du butin (voir [Déroulé d'une partie](deroule.md)) sont **agressifs** : retour de test de Quentin du 30/09/2026, « les ennemis dans le donjon ne sont pas assez agressifs ».

- **Ils voient plus loin, mais pas à travers les murs** : un joueur en ligne de vue à moins de 12 m est poursuivi {à équilibrer}.
- **Ils ne lâchent pas** : plus d'abandon au bout de quelques secondes sans frapper ; un gardien poursuit tant que sa cible reste à moins de 20 m de son poste (sa « laisse »), puis y retourne {à équilibrer}.
- **Ils donnent l'alerte** : un gardien qui repère un joueur ou qui est frappé (de n'importe où dans sa laisse, flèche comprise) lance aussi les gardiens à moins de 8 m {à équilibrer}.
- **Ils courent et esquivent** comme les squelettes des vagues (voir [Ennemis](ennemis.md), Comportement).

{{dev: `Squelette.Gardien` : `JoueurProche` (ligne de vue `LigneDeVue`, laisse `DansLaLaisse`), `MajPoursuite` (abandon à la laisse seulement), `OnTouche` et `AlerterGardiens` (une seule vague d'alerte, pas de relais). `GameBalance.gardienDetection` 12, `gardienLaisse` 20, `gardienAlerte` 8.}}

## Notes de développement {dev}

{{dev: Corrections de l'audit du 27/09/2026 (`Docs/audit-donjon.md`), version 0.5.10.}}

- {{dev: **Murs** : `DonjonGenerateur.PoserBord` ; `DonjonKit.murs` = `wall` ×5, `wall_pillar` ×2, `wall_scaffold` ×2 (pleins) ; `mursHauts` = `wall_window_closed` seul (fenêtre fermée, `k ≥ 1`, cellule du milieu du pan, une chance sur deux, `DonjonPlan.TorcheSur` exclu) ; `murCasse` = `wall_broken` (1 sur 15 par hachage du bord, le bord précédent du même alignement jamais cassé, mur plein posé 0,95 m derrière sur l'enceinte). Comptes sur 5 graines : portes 20-33 → 0, arches 12-20 → 0, fenêtres (grillagées ou fermées) 22-29 → 14-22 (fermées), cassés 10-15 → 8-15 (doublés), pleins 54-61 → 140-148.}}
- {{dev: **Murets** : `Bord.MurBas` pose `DonjonKit.muret` (`floor_foundation_front` mis à l'échelle (2 ; 0,5 ; 0,48) : 4 × 1 × 1 m) avec une boîte de 1 m ; sans modèle, bloc facetté généré (`DonjonGenerateur.MeshMuret`). `DonjonPlan.MaxMursBas` reste 3.}}
- {{dev: **Ossements** : `DonjonPlan.PlacerDecor`, `OsDistance = 1,6 m`, décalage du côté opposé au jeu du point dans sa cellule, aucun os si aucun côté ne tient dans la cellule (23-24 os pour 24 apparitions). Décors de coin exclus des cellules d'apparition (`m_Occupe & 16`).}}
- {{dev: **Plafonds et toit** : `DonjonKit.plafond` = `ceiling_tile`, posé sous chaque plancher d'étage (pivot au plan du plancher, pend de 0,25 m) et en toit à 12 m sur toute l'emprise (groupe du dernier niveau). Deuxième torche (torchère dans la cellule libre la plus loin de la première) au rez des blocs Mezzanine et Tour ; torchère au bord du bassin, près de l'arrivée de son escalier (`TorchereBassin`).}}
- {{dev: **Coffres** : `DonjonJeu.SansPieces` retire du corps du coffre les triangles dont l'UV tombe dans la zone dorée de l'atlas KayKit (u ≥ 0,84, v ≤ 0,5 ; grand coffre : 1 084 → 242 triangles) ; `EnvolOr` émet 24 ou 44 gemmes (`GemmesVolantes`, thème Sacré : le seul doré des palettes) ; le maillage plein est rendu à `PreparerButins`. Les maillages `Assets/Art/Coffres/Maillages/*` sont en lecture (`m_IsReadable`).}}
- {{dev: **Caméra** (`CameraEpaule`) : deux lancers sans rayon (pivot → caméra, épaule → caméra) en plus des balayages, `Enceinte` (boîte intérieure des murs, sous leur sommet, posée par `DonjonJeu.LateUpdate` quand le héros local est au donjon), puis `DansUnCollider` : la caméra se rapproche par pas de 0,2 m tant qu'une sphère de 0,18 m touche un collider non ignoré. Boîtes des poteaux d'arcade portées à 0,6 × 4 m, boîte du poteau de balustrade prise sur son modèle.}}
- {{dev: **Grilles et arrivée** : `DonjonPlan.grille[]` (une cellule libre par bloc de hall, une chance sur deux), `DonjonKit.solGrille` ; `solsRez` = dalles pleines ; `dalleArrivee` = `floor_wood_large_dark`, posée par `DecorDalleArrivee` (jamais posée jusque-là).}}
- {{dev: **Rendu** : `DonjonGenerateur.Combiner` combine les mailles immobiles de chaque groupe (bloc, niveau) par matériau après la génération (`Mesh.CombineMeshes`, modèles du kit en lecture) ; coffres, tas d'or, portail et eau restent à part. Pendant une transition de masquage, les pièces se rendent elles-mêmes (`DonjonMasquage.Rendus`). Hall d'arrivée, graine 111 : 2 148 → 498 lots, 43 → 41 SetPass, 30,4 → 23,8 ms dans l'éditeur ; plate-forme du 2e étage : 2 322 → 582 lots.}}
- {{dev: **HUD** : `Hud.uss` `.hud-donjon-message` top 386 px (312 px à ×3) ; `EcranHud` cache « Prêts » quand `DonneesUI.Donjon.AuDonjon`.}}
- {{dev: **Contrôles** : `ScenariosDonjon.Ouvertures / OsApparitions / BordsOuverts / CasCamera / CoffreVide / TasOr / VoirPiece / Hud` ; captures `Assets/Screenshots/donjon_fix_*.png`.}}
