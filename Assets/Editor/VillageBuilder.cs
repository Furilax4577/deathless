using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Village v4 (25/09/2026) : Nexus dégagé, six maisons KayKit en couronne, place du portail près du Nexus, prairie,
// forêt ouverte et praticable avec trois sentiers de terre vers les clairières de spawn. Plus d'enceinte ni de bâtiments.
// Outil d'éditeur ré-exécutable : menu Deathless > Village > Générer / Vérifier la circulation / Nettoyer.
// Tout est mesuré depuis le Nexus (origine). Azimuts horaires depuis +Z : nord = 0°, sud-est = 135°, sud-ouest = 225°.
// Aléa à graine fixe : deux exécutions donnent la même forêt.
public static class VillageBuilder
{
    // ---------------- Paramètres ----------------
    public const int Seed = 42;
    public const float TerrainHalf = 100f;       // plaine 200 x 200 m
    public const float NexusClear = 8f;          // rayon dégagé autour du Nexus (CLAUDE.md)
    // Maisons : 6, façade vers le Nexus, jamais dans les couloirs d'arrivée (secteur de ±TrailExclusion autour de chaque
    // sentier 0° / 135° / 225°), écart angulaire >= HouseMinGap entre maisons. Répartition 2 / 2 / 2 dans les trois arcs
    // libres (25-110°, 160-200°, 250-335°) : la place du portail tient entre les deux premières sans façade devant.
    public static readonly float[] HouseAzimuths = { 40f, 95f, 165f, 195f, 270f, 315f };
    public const float TrailExclusion = 25f;     // demi-secteur interdit autour de l'axe d'un sentier (degrés)
    public const float HouseMinGap = 30f;        // écart angulaire minimal entre deux maisons (degrés) ; à r = 17 m, 30° ≈ 9 m
    // r = 18,5 m depuis l'agrandissement du 25/09/2026 (17 m avant) : à 17 m, la maison 2 (B) mordait sur la place du portail
    // et les maisons 3 et 4 (30° d'écart) se touchaient presque.
    public const float HouseR = 18.5f;
    // Échelle réglée sur la porte (décision utilisateur du 25/09/2026 : échelle de Relic, porte d'environ 2,1 m pour un joueur de
    // 2 m). Porte mesurée dans les maillages (seuil -> haut de l'arc intérieur, unités du maillage) : 0,28 pour home_A (seuil au
    // pied du maillage) et pour home_B (seuil à 0,21, en haut du perron de pierre). Échelle 2,1 / 0,28 = x7,5 :
    // home_A 5,9 m de large, home_B 6,6 m.
    public const float HouseDoorTarget = 2.1f;
    public static readonly float[] HouseDoorLocal = { 0.28f, 0.28f };      // home_A, home_B
    public static readonly float[] HouseDoorSillLocal = { 0f, 0.21f };     // hauteur du seuil dans le maillage
    public static readonly float[] HouseSillHeight = { 0f, 0.6f };         // hauteur du seuil au-dessus du sol (m) : home_B garde 2-3 marches de perron
    public const float HouseBury = 0.03f;                                  // enfoncement supplémentaire (m) : pas de jour sous la base
    public const float GroundEarthHouse = 0.9f;                            // liseré de terre au pied des maisons (m)
    public const float HouseYawJitter = 8f;      // ±8°
    public const float HousePathWidth = 2f;      // sentier de terre maison -> anneau du Nexus
    // Portail de donjon : place réduite près de Nyxessa, entre les maisons 1 (40°) et 2 (95°)
    public const float PortalAz = 67.5f, PortalR = 11f, PortalPlaceDiameter = 6f;
    // Nexus : autel + relique validés (prefab repris de sandbox-vfx), anneau de pierres au rayon dégagé
    public const string RelicPrefabPath = "Assets/VFX/GemmeNyxessa/GemmeNyxessa.prefab";
    public const string PortalPrefabPath = "Assets/VFX/PortailDonjon/PortailDonjon.prefab"; // portail voxel validé (sandbox-vfx), `ouvert` = vrai dans le prefab
    // Sols (technique Relic, DefenseDressing) : plan de sol en `Ground.mat` uni, dalles Dungeon Pack (2 m / 4 m, atlas
    // KayKit_Dungeon) posées à y = -0,06 (dessus à ~0,05 m), allées en tuiles Halloween `path_A..D` (1,9 m, deux rangées
    // à ±0,95 m pour 4 m de large, une rangée pour 2 m) à y = +0,02, en alternant ±6 mm d'une tuile à l'autre (pas de z-fighting
    // sur les chevauchements des coudes). Dans la forêt : terre battue + tuiles éparses (le pavé continu ferait trop urbain).
    public const float PaversRadius = 8.6f;      // pavés `floor_tile_small` sur tout l'anneau autour de la relique (jusqu'aux pierres)
    public const float TileY = 0f, PathY = 0.02f, PathAlt = 0.006f;   // floor_tile_* : dessous à -0,10, dessus à +0,05 (au-dessus du plan de sol)
    public const float SparseTileStep = 3.5f;    // dans la forêt : une tuile de chemin tous les 3,5 m environ
    // Étiquettes TextMesh au sol (sentiers, spawns, maisons, portail, prairie, forêt, Nexus) : retirées le 25/09/2026 (décision
    // utilisateur). Le code reste ; à vrai, elles sont regénérées (matériau BK_Label supprimé : police intégrée par défaut).
    public static readonly bool GenerateLabels = false;   // readonly plutôt que const : pas d'avertissement « code inaccessible »
    public const float LabelY = 0.15f;           // étiquettes au sol : au-dessus du dessus des tuiles path_* (0,12-0,126), sinon z-fighting
    // Plateau de la relique : 3 marches octogonales concentriques de 0,25 m (Ø 6 m au sommet, bord à r = 4,2 m au sol).
    // Chaque marche = un prisme (MeshCollider convexe exact : volumes et circulation inchangés) + un habillage sans collider.
    public const int PlateauSides = 8, PlateauSteps = 3;
    public const float PlateauTopRadius = 3f, StepHeight = 0.25f, StepWidth = 0.6f;
    // Habillage des marches et des assises (style KayKit : le détail vient de la géométrie, UV fixes sur l'atlas KayKit_Dungeon).
    // Dessus : dalles floor_tile_small* réduites, découpées au contour octogonal (maillage recalculé), 2 mm au-dessus du dessus.
    // Contremarches : une rangée de blocs chanfreinés par côté, en saillie, dont le dessus déborde sur le giron (nez de marche).
    // Le prisme rendu dessous sert de fond sombre (joints) ; son dessus est abaissé pour laisser voir les joints des dalles.
    public const float PlateauTileScale = 0.5f;      // dalles de 1 m sur les marches (2 m sur l'anneau)
    public const float SocleTileScale = 0.3f;        // dalles de 0,6 m sur le socle du portail
    public const float DressTileLift = 0.002f;       // dessus des dalles au-dessus du dessus du prisme
    public const float SupportDrop = 0.015f;         // dessus du prisme rendu abaissé (collider inchangé), sous le fond des joints des dalles
    public const float BlockProud = 0.008f;          // saillie des blocs devant la face du prisme = retrait des joints
    public const float BlockJoint = 0.008f;          // largeur des joints entre blocs
    public const float BlockChamfer = 0.015f;        // chanfrein des arêtes des blocs
    public const float BlockDepth = 0.10f;           // profondeur des blocs : leur dessus couvre le bord du giron (pierre de chant)
    public const float BlockTopLift = 0.008f;        // dessus des blocs au-dessus des dalles (nez de marche)
    public static readonly Vector2 PlateauBlockLength = new Vector2(0.45f, 0.75f);  // longueur des blocs (min, max), une rangée de 0,25 m
    public static readonly Vector2 SocleBlockLength = new Vector2(0.20f, 0.36f);    // blocs plus petits sur le socle
    public const int PlateauBlockRows = 1, SocleBlockRows = 1;   // 0,25 m et moins : une rangée ; > 1 = rangs à joints décalés
    // Teintes (UV fixes) prises sur les pierres des murs `wall` du Dungeon Pack : gris-bleu foncé, moyen, clair ; joints très foncés.
    public static readonly Vector2[] BlockUvs = { new Vector2(0.162f, 0.825f), new Vector2(0.152f, 0.861f), new Vector2(0.143f, 0.892f) };   // #6E767B, #7C868B, #899499
    public static readonly Vector2 MortarUv = new Vector2(0.058f, 0.884f);
    public const int DressSeed = 4242;               // aléa propre à l'habillage (System.Random) : ne décale pas l'aléa de la forêt
    private const string DungeonRoot = "Assets/Art/KayKit/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    private const string HalloweenRoot = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";
    private static readonly string[] Pavers = { "floor_tile_small", "floor_tile_small", "floor_tile_small", "floor_tile_small_broken_A", "floor_tile_small_broken_B", "floor_tile_small_weeds_A", "floor_tile_small_weeds_B" };
    private static readonly string[] PathTiles = { "path_A", "path_B", "path_C", "path_D" };
    public const int RingStones = 24;            // petites pierres (< 0,6 m, sans collider) : l'anneau se lit sans gêner le passage
    public const float RingStoneSize = 0.5f;     // extension max (m) d'une pierre de l'anneau
    // Socle du portail (même langage que le plateau : prismes octogonaux KayKit_Dungeon habillés), petit pour ne pas
    // concurrencer le plateau de la relique. Assises du bas vers le haut : rayon circonscrit et hauteur du dessus (une seule entrée
    // = une seule assise). Chaque assise < 0,3 m au-dessus de la précédente. MeshCollider convexe par assise.
    public static readonly float[] PortalPedestalRadii = { 0.95f, 0.65f };   // Ø 1,9 m puis Ø 1,3 m (hors tout, pointe à pointe)
    public static readonly float[] PortalPedestalTops = { 0.18f, 0.30f };
    public const float PortalVisualRadius = 1.6f;   // rayon du disque de voxels (PortalVisual.radius : 1,6 depuis le 25/09/2026 dans main, 1,35 avant)
    public const float PortalPedestalGap = 0.15f;   // écart visuel entre le bas du disque et le dessus du socle
    // Racine du prefab : dessus du socle + écart + rayon = 2,05 m (le prefab de main la met à 1,75 m sans socle, disque à 0,15 m
    // du sol ; ici le disque est à 0,15 m au-dessus du socle). Le cube racine (collider de 3,55 m de haut) descend à 0,27 m.
    public static float PortalHeight { get { return PortalPedestalTops[PortalPedestalTops.Length - 1] + PortalPedestalGap + PortalVisualRadius; } }
    public const float RockMinSize = 0.6f, RockMaxSize = 2.2f; // rochers de lisière : taille monde hors tout ; collider seulement si >= 1 m
    public const float RockTrailClear = 3f;      // aucun rocher à moins de 3 m des sentiers
    public const float RelicColliderRadius = 1.3f;
    // Prairie et forêt
    public const float ForestInner = 30f;        // lisière
    public const float TreeSpacing = 7f;         // grille hexagonale : ~42 m² par arbre (1 arbre / 40-50 m²)
    public const float TreeJitter = 1.5f;        // décalage aléatoire max (m) : deux voisins restent à >= ~4 m
    public const float TreeScale = 1.6f;         // échelle Relic (1,4-1,9) ; ±15 %
    public const float TreeScaleVar = 0.15f;
    public const float TreeColliderRadius = 0.5f; // tronc (monde) : la forêt est praticable, seuls les troncs bloquent
    public const int EdgeBushCount = 90;
    public const int EdgeRockCount = 40;
    public const int GrassCount = 400;           // touffes de la prairie (220 avant le sol texturé du 25/09/2026)
    // Sentiers de terre (nord, SE, SO) : de l'anneau du Nexus à la clairière, coude de 5° dans la forêt. Retirés le 25/09/2026
    // (décision utilisateur) avec le sol des clairières : le code reste derrière GenerateTrails, les tirages aléatoires sont
    // faits quand même (la forêt ne bouge pas). Les couloirs et les clairières restent libres d'arbres, sans sol marqué.
    public static readonly bool GenerateTrails = false;
    public static readonly bool KeepTrailCorridors = true;   // couloirs sans arbres ni rochers le long des anciens sentiers
    public static readonly bool KeepSpawnClearings = true;   // clairières de spawn libres d'arbres
    public const float TrailWidth = 4f;
    public const float TrailClear = 3f;          // demi-largeur libre de troncs de part et d'autre de l'axe
    public const float TrailBendStart = 30f, TrailBendEnd = 64f, TrailBend = 5f;
    public const float ClearingR = 70f, ClearingRadius = 7f;
    public static readonly float[] TrailAz = { 0f, 135f, 225f };
    public static readonly string[] TrailName = { "Nord", "SudEst", "SudOuest" };
    public static readonly string[] TrailLabel = { "NORD", "SUD-EST", "SUD-OUEST" };
    // Sol (remplace le plan Ground.mat vert uni le 25/09/2026). Technique du terrain de défense de Relic (DefenseDressing :
    // grille de 2 m, diagonales alternées, sommets non partagés donc facettes) + couleur par facette : chaque triangle pointe
    // un texel d'une petite palette (texture générée, filtrage point) rendue par URP Lit, qui garde lumière et ombres.
    // Relief très léger hors du village ; plat (y = 0) sous le village, le plateau, la place du portail et les allées.
    public const float GroundStep = 2f;
    public const float GroundFlatRadius = 22f, GroundBlendRadius = 28f;   // plat jusqu'à 22 m, relief complet au-delà de 28 m
    public const float GroundRelief = 0.12f;                             // amplitude du relief (m, ±)
    public const float GroundEarthTrunk = 1.3f;                          // terre au pied des arbres (rayon, m)
    public const float GroundEarthPaved = 0.8f;                          // liseré de terre autour des pavés et des allées (m)
    public const string GroundMeshPath = "Assets/Art/Meshes/SolVillage.asset";
    public const string GroundTexPath = "Assets/Art/Textures/SolVillage_Palette.png";
    public const string GroundMatPath = "Assets/Art/Materials/SolVillage.mat";
    // Palette (sRGB) : 0-3 herbes du plus sombre au plus jaune, 4-5 taches claire / sombre, 6-7 terres
    public static readonly Color[] GroundPalette = {
        new Color(0.33f, 0.43f, 0.27f), new Color(0.40f, 0.50f, 0.32f), new Color(0.46f, 0.56f, 0.35f), new Color(0.52f, 0.58f, 0.36f),
        new Color(0.58f, 0.64f, 0.40f), new Color(0.28f, 0.37f, 0.24f), new Color(0.47f, 0.41f, 0.31f), new Color(0.39f, 0.33f, 0.26f) };

    private const string ForestRoot = "Assets/Art/KayKit/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)/";
    private const string HexBuildings = "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/buildings/blue/";
    private static readonly string[] Houses = { "building_home_A_blue", "building_home_B_blue" };
    private static readonly string[] Trees = { "Tree_1_A_Color1", "Tree_1_B_Color1", "Tree_1_C_Color1", "Tree_2_A_Color1", "Tree_2_B_Color1", "Tree_2_C_Color1", "Tree_2_D_Color1", "Tree_2_E_Color1", "Tree_3_A_Color1", "Tree_3_B_Color1", "Tree_3_C_Color1", "Tree_4_A_Color1", "Tree_4_B_Color1", "Tree_4_C_Color1" };
    private static readonly string[] BareTrees = { "Tree_Bare_1_A_Color1", "Tree_Bare_2_A_Color1" };
    private static readonly string[] Bushes = { "Bush_1_A_Color1", "Bush_1_C_Color1", "Bush_2_B_Color1", "Bush_2_D_Color1", "Bush_3_A_Color1", "Bush_4_A_Color1", "Bush_4_D_Color1" };
    private static readonly string[] Rocks = { "Rock_1_A_Color1", "Rock_1_D_Color1", "Rock_2_A_Color1", "Rock_2_C_Color1", "Rock_3_B_Color1", "Rock_3_G_Color1" };
    private static readonly string[] Grass = { "Grass_1_A_Color1", "Grass_1_C_Color1", "Grass_2_B_Color1", "Grass_2_D_Color1" };

    // ---------------- Géométrie ----------------
    public static Vector3 Polar(float az, float r) { return new Vector3(Mathf.Sin(az * Mathf.Deg2Rad) * r, 0f, Mathf.Cos(az * Mathf.Deg2Rad) * r); }
    public static Vector3 Right(float az) { return new Vector3(Mathf.Cos(az * Mathf.Deg2Rad), 0f, -Mathf.Sin(az * Mathf.Deg2Rad)); }
    public static float AzOf(Vector3 p) { return (Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg + 360f) % 360f; }
    public static float RadOf(Vector3 p) { return new Vector2(p.x, p.z).magnitude; }
    public static float TrailAxis(int g, float r)
    {
        float t = Mathf.Clamp01((r - TrailBendStart) / (TrailBendEnd - TrailBendStart));
        return TrailAz[g] + TrailBend * Mathf.Sin(Mathf.PI * t);
    }
    public static float Lateral(int g, Vector3 p) { float r = RadOf(p); return Mathf.Abs(Mathf.DeltaAngle(AzOf(p), TrailAxis(g, r))) * Mathf.Deg2Rad * r; }
    // Sur (ou à `margin` m de) une surface pavée : anneau, allées maison -> anneau, place et chemin du portail.
    public static bool NearPaved(Vector3 p, float margin)
    {
        float r = RadOf(p), az = AzOf(p);
        if (r < PaversRadius + 1f + margin) return true;
        foreach (float haz in HouseAzimuths)
            if (r < HouseR && Mathf.Abs(Mathf.DeltaAngle(az, haz)) * Mathf.Deg2Rad * r < HousePathWidth / 2f + margin) return true;
        Vector3 pp = Polar(PortalAz, PortalR);
        if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pp.x, pp.z)) < PortalPlaceDiameter / 2f + 1f + margin) return true;
        if (r < PortalR && Mathf.Abs(Mathf.DeltaAngle(az, PortalAz)) * Mathf.Deg2Rad * r < 1.25f + margin) return true;
        foreach (Vector4[] f in HouseFootprints) if (FootprintDistance(f, new Vector2(p.x, p.z)) < margin) return true;   // pas dans les maisons
        return false;
    }

    // Emprise au sol d'une maison (base du maillage : sommets à moins de 0,1 u du pied), en rectangle orienté :
    // { centre (x, z), axe droit (x, z), axe avant (x, z), demi-tailles (x, z) } rangés dans deux Vector4.
    public static readonly List<Vector4[]> HouseFootprints = new List<Vector4[]>();
    private static Vector4[] Footprint(Transform t, Mesh m)
    {
        Vector3 mn = Vector3.one * 999f, mx = -Vector3.one * 999f;
        foreach (Vector3 v in m.vertices) if (v.y < m.bounds.min.y + 0.1f) { mn = Vector3.Min(mn, v); mx = Vector3.Max(mx, v); }
        Vector3 c = t.TransformPoint((mn + mx) / 2f);
        Vector3 r = t.right, f = t.forward; float s = t.lossyScale.x;
        return new Vector4[] { new Vector4(c.x, c.z, r.x, r.z), new Vector4(f.x, f.z, (mx.x - mn.x) / 2f * s, (mx.z - mn.z) / 2f * s) };
    }
    // Distance d'un point au rectangle orienté (0 à l'intérieur).
    public static float FootprintDistance(Vector4[] f, Vector2 p)
    {
        Vector2 d = p - new Vector2(f[0].x, f[0].y);
        float lx = Mathf.Abs(Vector2.Dot(d, new Vector2(f[0].z, f[0].w))) - f[1].z, lz = Mathf.Abs(Vector2.Dot(d, new Vector2(f[1].x, f[1].y))) - f[1].w;
        return new Vector2(Mathf.Max(lx, 0f), Mathf.Max(lz, 0f)).magnitude;
    }
    public static Vector3 ClearingCenter(int g) { return Polar(TrailAz[g], ClearingR); }
    public static bool OnTrail(Vector3 p, float margin)
    {
        float r = RadOf(p);
        for (int g = 0; g < 3; g++)
        {
            if (KeepTrailCorridors && r <= TrailBendEnd + 2f && Lateral(g, p) <= TrailWidth / 2f + margin) return true;
            Vector3 c = ClearingCenter(g);
            if (KeepSpawnClearings && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)) <= ClearingRadius + margin) return true;
        }
        return false;
    }

    // ---------------- Menus ----------------
    [MenuItem("Deathless/Village/Générer")]
    public static void Build()
    {
        Random.InitState(Seed);
        DressPieces = 0;
        Transform root = Find("VillageBlockout");
        if (root == null) { Debug.LogError("VillageBlockout introuvable dans la scène."); return; }
        Clean(root);
        Material terre = GenerateTrails ? Mat("BK_Terre") : null, labelMat = Mat("BK_Label");

        // 1. Plaine : l'ancien plan (Ground.mat vert uni) est désactivé, le sol généré (étape 9) le remplace
        Transform sol = root.Find("Sol/Sol_Plane");
        if (sol != null) sol.gameObject.SetActive(false);

        // 1b. Nexus : autel + relique validés (prefab de sandbox-vfx) à l'origine, anneau de pierres Forest au rayon dégagé
        Transform nexus = root.Find("Nexus"); if (nexus == null) nexus = Group(root, "Nexus");
        {
            GameObject relicModel = AssetDatabase.LoadAssetAtPath<GameObject>(RelicPrefabPath);
            if (relicModel == null) Debug.LogWarning("Prefab de la relique introuvable : " + RelicPrefabPath);
            else
            {
                GameObject relic = (GameObject)PrefabUtility.InstantiatePrefab(relicModel, nexus);
                relic.name = "GemmeNyxessa"; relic.transform.position = Vector3.up * (PlateauSteps * StepHeight); relic.transform.rotation = Quaternion.identity;
                CapsuleCollider cc = relic.AddComponent<CapsuleCollider>(); cc.radius = RelicColliderRadius; cc.height = 5f; cc.center = new Vector3(0f, 2.5f, 0f);
                HideLabel(relic);   // étiquette 3D du labo VFX, inutile ici
            }
            // plateau de pierre à trois marches (octogones concentriques), du bas vers le haut
            Transform plateau = Group(nexus, "Plateau");
            Material dungeonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/KayKit_Dungeon.mat");
            var dressRng = new System.Random(DressSeed);
            for (int k = 0; k < PlateauSteps; k++)
            {
                float radius = PlateauTopRadius + StepWidth * (PlateauSteps - 1 - k);
                StoneTier(plateau, "Marche_" + (k + 1), radius, k + 1 < PlateauSteps ? radius - StepWidth : 0f, StepHeight * k, StepHeight * (k + 1),
                    PlateauTileScale, PlateauBlockLength, PlateauBlockRows, dungeonMat, dressRng);
            }
            // pavés Dungeon sur tout l'anneau autour du plateau (grille de 2 m, cellules dont le centre est sous PaversRadius)
            Transform pavage = Group(nexus, "Pavage_Anneau");
            for (float z = -10f; z <= 10f; z += 2f)
                for (float x = -10f; x <= 10f; x += 2f)
                {
                    float r = Mathf.Sqrt(x * x + z * z);
                    if (r > PaversRadius || r < PlateauTopRadius + StepWidth * (PlateauSteps - 1) - 1.2f) continue;
                    Dungeon(pavage, Pavers[Random.Range(0, Pavers.Length)], new Vector3(x, TileY, z), 90f * Random.Range(0, 4));
                }
            Transform ring = Group(nexus, "Anneau_Pierres");
            for (int i = 0; i < RingStones; i++)
            {
                float az = 360f * i / RingStones + Random.Range(-2f, 2f);
                string rockPiece = Rocks[Random.Range(0, Rocks.Length)];
                GameObject stone = PlaceRock(ring, rockPiece, Polar(az, NexusClear + Random.Range(-0.2f, 0.2f)), RingStoneSize * Random.Range(0.7f, 1f), false);
                if (stone != null) stone.name = "Pierre_" + (i + 1).ToString("00");
            }
        }

        // 2. Maisons en couronne, façade vers le Nexus, sentier vers l'anneau
        Transform maisons = Group(root, "Maisons");
        Transform sentiers = Group(root, "Sentiers");
        float houseScale = 0f;
        HouseFootprints.Clear();
        int HouseCount = HouseAzimuths.Length;
        for (int i = 0; i < HouseCount; i++)
        {
            float az = HouseAzimuths[i];
            for (int g = 0; g < 3; g++) if (Mathf.Abs(Mathf.DeltaAngle(az, TrailAz[g])) < TrailExclusion) Debug.LogWarning("Maison " + (i + 1) + " (az " + az + ") dans le couloir du sentier " + TrailName[g]);
            for (int k = 0; k < i; k++) if (Mathf.Abs(Mathf.DeltaAngle(az, HouseAzimuths[k])) < HouseMinGap) Debug.LogWarning("Maisons " + (k + 1) + " et " + (i + 1) + " a moins de " + HouseMinGap + " deg");
            string piece = Houses[i % Houses.Length];
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HexBuildings + piece + ".fbx");
            if (model == null) { Debug.LogWarning("Maison introuvable : " + piece); continue; }
            Mesh houseMesh = model.GetComponent<MeshFilter>().sharedMesh;
            Bounds b = houseMesh.bounds;
            int type = i % Houses.Length;
            float scale = HouseDoorTarget / HouseDoorLocal[type];   // porte de 2,1 m
            houseScale = scale;
            Vector3 pos = Polar(az, HouseR) + Vector3.up * (HouseSillHeight[type] - HouseDoorSillLocal[type] * scale - HouseBury);   // socle enfoncé : seuil à la hauteur voulue
            float yaw = YawToward(pos, Vector3.zero) + Random.Range(-HouseYawJitter, HouseYawJitter);
            GameObject h = Place(maisons, HexBuildings + piece, pos, yaw, scale);
            h.name = "Maison_" + (i + 1) + "_" + (piece.Contains("_A_") ? "A" : "B");
            BoxFromMesh(h);
            HouseFootprints.Add(Footprint(h.transform, houseMesh));
            // allée maison -> anneau : de l'anneau jusqu'à 0,6 m devant la façade (bord avant du maillage, perron compris)
            float front = HouseR - b.max.z * scale;
            PavedPath(Group(sentiers, "Sentier_Maison_" + (i + 1)), Polar(az, PaversRadius - 0.5f), Polar(az, front + 0.6f), 1);
        }

        // 3. Place du portail près du Nexus
        Transform por = Group(root, "Portail");
        {
            Vector3 pp = Polar(PortalAz, PortalR);
            Transform place = Group(por, "Portail_Place");
            // dalles recalées sur la grille de 2 m du pavage de l'anneau, sans celles que l'anneau pave déjà : aucune dalle
            // superposée à une autre (z-fighting à y = 0,05). Mêmes tirages aléatoires qu'avant : la forêt ne change pas.
            for (float dz = -4f; dz <= 4f; dz += 2f)
                for (float dx = -4f; dx <= 4f; dx += 2f)
                    if (Mathf.Sqrt(dx * dx + dz * dz) <= PortalPlaceDiameter / 2f + 0.3f)
                    {
                        string piece = Pavers[Random.Range(0, Pavers.Length)]; float yaw = 90f * Random.Range(0, 4);
                        Vector3 cell = new Vector3(Mathf.Round((pp.x + dx) / 2f) * 2f, TileY, Mathf.Round((pp.z + dz) / 2f) * 2f);
                        if (RadOf(cell) <= PaversRadius) continue;   // déjà pavé par l'anneau
                        Dungeon(place, piece, cell, yaw);
                    }
            // socle de pierre sous le disque de voxels
            {
                Transform socle = Group(por, "Portail_Socle"); socle.position = pp;
                socle.rotation = Quaternion.Euler(0f, Mathf.Repeat(PortalAz, 360f / PlateauSides), 0f);   // un côté (pas une pointe) face au Nexus
                Material dungeonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/KayKit_Dungeon.mat");
                var dressRng = new System.Random(DressSeed + 1);
                for (int k = 0; k < PortalPedestalRadii.Length; k++)
                    StoneTier(socle, "Assise_" + (k + 1), PortalPedestalRadii[k], k + 1 < PortalPedestalRadii.Length ? PortalPedestalRadii[k + 1] : 0f,
                        k > 0 ? PortalPedestalTops[k - 1] : 0f, PortalPedestalTops[k], SocleTileScale, SocleBlockLength, SocleBlockRows, dungeonMat, dressRng);
            }
            PavedPath(Group(por, "Portail_Chemin"), Polar(PortalAz, PaversRadius - 0.5f), Polar(PortalAz, PortalR - PortalPlaceDiameter / 2f + 0.8f), 1);
            // portail voxel validé (sandbox-vfx) au centre de la place, face au Nexus
            GameObject portalModel = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (portalModel == null) Debug.LogWarning("Prefab du portail introuvable : " + PortalPrefabPath);
            else
            {
                GameObject portal = (GameObject)PrefabUtility.InstantiatePrefab(portalModel, por);
                portal.name = "PortailDonjon";
                portal.transform.position = pp + Vector3.up * PortalHeight;
                portal.transform.rotation = Quaternion.Euler(0f, PortalAz + 90f, 0f);   // comme Portal_ToDungeon dans Relic (az 0, yaw 90) : la face fine du cube racine, donc le disque de voxels, regarde le Nexus
                HideLabel(portal);
                // portail alimenté par Nyxessa : la relique envoie sa charge en arc, le portail s'ouvre à son arrivée
                var so = new SerializedObject(portal.GetComponent<PortalVisual>());
                so.FindProperty("alimenteParNyxessa").boolValue = true;
                so.FindProperty("ouvert").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 4. Sentiers de terre nord / SE / SO : de l'anneau du Nexus aux clairières de spawn
        Transform foret = Group(root, "Foret");
        Transform arbres = Group(foret, "Arbres");
        Transform lisiere = Group(foret, "Lisiere");
        Transform herbe = Group(foret, "Herbe");
        Transform spawns = Group(foret, "Spawns");
        for (int g = 0; g < 3; g++)
        {
            // Sentiers retirés (GenerateTrails = faux) : les tirages sont faits quand même, dans le même ordre, pour que la forêt
            // qui suit reste identique ; rien n'est posé.
            Transform ts = GenerateTrails ? Group(sentiers, "Sentier_" + TrailName[g]) : null;
            // pavé (deux rangées) de l'anneau à la lisière
            Transform pave = GenerateTrails ? Group(ts, "Pave") : null;
            int rowIndex = 0;
            for (float r = PaversRadius - 0.5f; r < ForestInner; r += 1.9f, rowIndex++)
            {
                float az = TrailAxis(g, r); Vector3 pc = Polar(az, r); Vector3 rt = Right(az);
                for (int s = 0; s < 2; s++)   // damier de hauteurs : deux tuiles voisines (en long comme en travers) diffèrent de PathAlt
                {
                    string piece = PathTiles[Random.Range(0, PathTiles.Length)]; float yaw = 90f * Random.Range(0, 4);
                    if (GenerateTrails) Halloween(pave, piece, pc + rt * (s == 0 ? -0.95f : 0.95f) + Vector3.up * (PathY + ((rowIndex + s) % 2) * PathAlt), yaw);
                }
            }
            // forêt : terre battue + tuiles éparses jusqu'à la clairière
            float step = 3f;
            if (GenerateTrails)
                for (float r = ForestInner - 1f; r < TrailBendEnd; r += step)
                    Strip(ts, "Terre_" + Mathf.RoundToInt(r), Polar(TrailAxis(g, r), r), Polar(TrailAxis(g, r + step), Mathf.Min(r + step, TrailBendEnd)), TrailWidth, terre);
            Transform epars = GenerateTrails ? Group(ts, "Dalles_Eparses") : null;
            for (float r = ForestInner + 1.5f; r < TrailBendEnd; r += SparseTileStep * Random.Range(0.8f, 1.2f))
            {
                float az = TrailAxis(g, r);
                string piece = PathTiles[Random.Range(0, PathTiles.Length)]; float off = Random.Range(-1f, 1f); float yaw = 90f * Random.Range(0, 4);
                if (GenerateTrails) Halloween(epars, piece, Polar(az, r) + Right(az) * off + Vector3.up * PathY, yaw);
            }
            Vector3 c = ClearingCenter(g);
            if (GenerateTrails) Flat(ts, "Clairiere_Sol", PrimitiveType.Cylinder, c, new Vector3(ClearingRadius * 2f - 1f, 0.02f, ClearingRadius * 2f - 1f), terre);
            // point d'apparition : Transform vide + gizmo éditeur (dalle et capsule rouges retirées le 25/09/2026)
            GameObject spawn = new GameObject("Spawn_" + TrailName[g]);
            spawn.transform.SetParent(spawns, false);
            spawn.transform.position = c + Vector3.up * GroundHeight(c.x, c.z);
            spawn.transform.rotation = Quaternion.LookRotation(-new Vector3(c.x, 0f, c.z), Vector3.up);
            spawn.AddComponent<SpawnPoint>().rayon = ClearingRadius * 0.5f;
        }

        // 5. Forêt ouverte : grille hexagonale espacée, jitter, troncs à collider ; rien sur les sentiers ni les clairières
        int trees = 0;
        float rowStep = TreeSpacing * 0.866f; int row = 0;
        for (float z = -TerrainHalf + 1f; z <= TerrainHalf - 1f; z += rowStep, row++)
        {
            float x0 = -TerrainHalf + 1f + ((row & 1) == 1 ? TreeSpacing / 2f : 0f);
            for (float x = x0; x <= TerrainHalf - 1f; x += TreeSpacing)
            {
                Vector3 p = new Vector3(x + Random.Range(-TreeJitter, TreeJitter), 0f, z + Random.Range(-TreeJitter, TreeJitter));
                if (Mathf.Abs(p.x) > TerrainHalf - 1f || Mathf.Abs(p.z) > TerrainHalf - 1f) continue;
                if (RadOf(p) < ForestInner) continue;
                if (OnTrail(p, TrailClear - TrailWidth / 2f + TreeColliderRadius)) continue;
                p.y = GroundHeight(p.x, p.z);
                PlaceTree(arbres, p, Random.value < 0.06f);
                trees++;
            }
        }
        // 6. Lisière (buissons, rochers avec collider) et herbe de prairie
        for (int i = 0; i < EdgeBushCount; i++)
        {
            Vector3 p = Polar(Random.Range(0f, 360f), Random.Range(ForestInner - 2f, ForestInner + 2f));
            if (OnTrail(p, 1.5f)) continue;
            p.y = GroundHeight(p.x, p.z);
            Place(lisiere, ForestRoot + Bushes[Random.Range(0, Bushes.Length)], p, Random.Range(0f, 360f), Random.Range(3f, 5f));
        }
        for (int i = 0; i < EdgeRockCount; i++)
        {
            // rochers de lisière : jamais à moins de 3 m d'un sentier ; collider seulement pour les gros (>= 1 m)
            Vector3 p = Polar(Random.Range(0f, 360f), Random.Range(ForestInner - 2.5f, ForestInner + 1.5f));
            float size = Random.Range(RockMinSize, RockMaxSize);
            if (OnTrail(p, RockTrailClear - TrailWidth / 2f + size / 2f)) continue;
            p.y = GroundHeight(p.x, p.z);
            PlaceRock(lisiere, Rocks[Random.Range(0, Rocks.Length)], p, size, size >= 1f);
        }
        for (int i = 0; i < GrassCount; i++)
        {
            Vector3 p = Polar(Random.Range(0f, 360f), Random.Range(NexusClear + 1.5f, ForestInner - 1f));
            if (OnTrail(p, 0.5f) || NearPaved(p, 0.6f)) continue;   // pas de touffe sur les allées, la place du portail ni l'anneau
            p.y = GroundHeight(p.x, p.z);
            Place(herbe, ForestRoot + Grass[Random.Range(0, Grass.Length)], p, Random.Range(0f, 360f), Random.Range(1.5f, 2.2f));
        }

        // 7. Labels (au sol) : désactivés depuis le 25/09/2026 (décision utilisateur), voir GenerateLabels
        if (GenerateLabels)
        {
            Transform labels = root.Find("Labels"); if (labels == null) labels = Group(root, "Labels");
            Label(labels, "Nexus", "NEXUS (Nyxessa)\nrayon degage 8 m", new Vector3(4.76f, LabelY, -2.75f), 0.5f, labelMat);
            for (int g = 0; g < 3; g++)
            {
                Label(labels, "V4_Sentier_" + TrailName[g], "SENTIER\n" + TrailLabel[g], Polar(TrailAxis(g, 45f), 45f) + Vector3.up * LabelY, 0.3f, labelMat);
                Label(labels, "V4_Spawn_" + TrailName[g], "SPAWN\n" + TrailLabel[g], ClearingCenter(g) + Polar(TrailAz[g], 4f) + Vector3.up * LabelY, 0.3f, labelMat);
            }
            for (int i = 0; i < HouseCount; i++) Label(labels, "V4_Maison_" + (i + 1), "MAISON " + (i + 1), Polar(HouseAzimuths[i], HouseR + 4f) + Vector3.up * LabelY, 0.22f, labelMat);
            Label(labels, "V4_Portail", "PORTAIL", Polar(PortalAz, PortalR) + Polar(PortalAz, 2.2f) + Vector3.up * LabelY, 0.2f, labelMat);
            Label(labels, "V4_Prairie", "PRAIRIE", Polar(300f, 25f) + Vector3.up * LabelY, 0.3f, labelMat);
            Label(labels, "V4_Foret", "FORET OUVERTE", Polar(80f, 45f) + Vector3.up * LabelY, 0.35f, labelMat);
        }

        // 8. Player au spawn nord, face au village
        Transform player = Find("Player");
        if (player != null)
        {
            Vector3 c = ClearingCenter(0);
            Vector3 pp0 = c + Polar(TrailAz[0], -2f);
            player.position = pp0 + Vector3.up * (1f + GroundHeight(pp0.x, pp0.z));
            player.rotation = Quaternion.LookRotation(-c, Vector3.up);
            Transform cam = player.Find("PlayerCamera"); if (cam != null) cam.localRotation = Quaternion.identity;
        }

        // 9. Sol à facettes colorées (après la forêt et les pavés : terre au pied des troncs et autour des dalles)
        BuildGround(sol != null ? sol.parent : root, arbres, root);
        // 10. Ambiance jour / nuit (DayCycle + CycleJourNuit, lumière de nuit de Nyxessa, lanternes, fenêtres, brume, lucioles)
        BuildAmbiance(root);

        MarkStatic(foret.gameObject); MarkStatic(maisons.gameObject); MarkStatic(nexus.Find("Anneau_Pierres").gameObject);
        Physics.SyncTransforms();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log("Village v4 généré : " + trees + " arbres (espacement " + TreeSpacing + " m ± " + TreeJitter + "), " + HouseCount + " maisons à r = " + HouseR + " (échelle x" + houseScale.ToString("F2") + ", porte " + HouseDoorTarget + " m), habillage plateau + socle : " + DressPieces + " pièces, lumières ponctuelles d'ambiance : " + AmbianceLights + ", sol : " + GroundStats + ", objets sous VillageBlockout : " + root.GetComponentsInChildren<Transform>().Length);
    }

    [MenuItem("Deathless/Village/Nettoyer")]
    public static void CleanMenu() { Transform root = Find("VillageBlockout"); if (root != null) Clean(root); }

    // Retire tout ce que le générateur produit et les restes des versions précédentes (v1 enceinte/bâtiments, v2 terrain, v3 palissade).
    private static void Clean(Transform root)
    {
        foreach (string n in new string[] { "Terrain", "Couloirs", "Foret", "Portail", "Enceinte", "Entrees", "Batiments", "Maisons", "Sentiers", "Ambiance" }) Kill(root.Find(n));
        Kill(root.Find("Sol/Routes"));
        Kill(root.Find("Sol/Sol_Village"));
        foreach (string n in new string[] { "Nexus/GemmeNyxessa", "Nexus/Anneau_Pierres", "Nexus/Nexus_Nyxessa", "Nexus/Nexus_Disque_R8", "Nexus/Anneau_R8", "Nexus/Plateau", "Nexus/Pavage_Anneau" })
            for (Transform t = root.Find(n); t != null; t = root.Find(n)) Kill(t);   // en boucle : une génération antérieure a pu laisser des doublons
        Kill(root.Find("Labels"));   // toutes les étiquettes, Label_Nexus compris (recréé par l'étape 7 si GenerateLabels)
    }

    // ---------------- Vérification de circulation ----------------
    // Grille au mètre sur toute la plaine : cellule bloquée si une sphère de 0,45 m (joueur) y touche un collider.
    // Flood-fill depuis le spawn nord : doit atteindre les 3 clairières, et un point à chaque azimut (pas de 5°) dans la
    // prairie (r = 25) et dans la forêt (r = 45 et 60). Puis largeur minimale de passage entre troncs sur les 3 sentiers.
    [MenuItem("Deathless/Village/Vérifier la circulation")]
    public static void VerifyMenu() { Debug.Log(Verify()); }

    public static string Verify()
    {
        Physics.SyncTransforms();
        int N = Mathf.RoundToInt(TerrainHalf * 2f);
        bool[,] ok = new bool[N, N];
        for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                Vector3 p = new Vector3(-TerrainHalf + i + 0.5f, 0f, -TerrainHalf + j + 0.5f);
                ok[i, j] = !Physics.CheckSphere(p + Vector3.up * 1.0f, 0.45f, ~0, QueryTriggerInteraction.Ignore);
            }
        var sb = new System.Text.StringBuilder();
        HashSet<int> seen = Flood(ok, N, ClearingCenter(0) + Polar(TrailAz[0], -5f));
        for (int g = 0; g < 3; g++) sb.Append("Joueur -> clairiere " + TrailName[g] + " : " + seen.Contains(Cell(ClearingCenter(g) + Polar(TrailAz[g], -5f), N)) + "\n");
        sb.Append("Joueur -> interieur de l'anneau du Nexus (r = 6) : " + seen.Contains(Cell(Polar(45f, 6f), N)) + " ; place du portail : " + seen.Contains(Cell(Polar(PortalAz, PortalR - 2f), N)) + "\n");
        foreach (float ring in new float[] { 25f, 45f, 60f, 85f })
        {
            int miss = 0; string missAz = "";
            for (int a = 0; a < 360; a += 5)
            {
                bool any = false;
                for (float dr = -2f; dr <= 2f && !any; dr += 1f) any = seen.Contains(Cell(Polar(a, ring + dr), N));
                if (!any) { miss++; missAz += a + " "; }
            }
            sb.Append("Joueur : tour a r = " + ring + " m (pas de 5 deg, tolerance ±2 m) : " + (72 - miss) + "/72 azimuts" + (miss == 0 ? " -> OK" : " -> BLOQUE en " + missAz) + "\n");
        }
        int total = 0, reach = seen.Count; for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) if (ok[i, j]) total++;
        sb.Append("Joueur : cellules praticables atteintes " + reach + " / " + total + "\n");
        // troncs (CapsuleCollider des arbres) pour la statistique d'espacement ; le test de largeur des sentiers est retiré avec eux
        var trunks = new List<Vector3>(); var radii = new List<float>();
        foreach (var cap in Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None))
        {
            if (cap.GetComponent<CharacterController>() != null || cap.transform.parent == null || cap.transform.parent.name != "Arbres") continue;
            trunks.Add(cap.transform.position); radii.Add(cap.radius * cap.transform.lossyScale.x);
        }
        // plateau : le joueur monte sur le plateau. CharacterController temporaire (rayon 0,45 m, hauteur 2 m, stepOffset 0,3 m,
        // pente 45°, skin 0,08 m) qui part de chaque sentier (r = 9 m, sur l'axe) et marche vers la relique à 4 m/s avec gravité ;
        // réussite s'il arrive au sommet (r <= 2,1 m, soit contre le collider de la relique) avec les pieds à +0,75 m.
        {
            float topY = PlateauSteps * StepHeight;
            GameObject probe = new GameObject("_Test_Montee_Plateau"); probe.hideFlags = HideFlags.HideAndDontSave;
            CharacterController cc = probe.AddComponent<CharacterController>();
            cc.radius = 0.45f; cc.height = 2f; cc.stepOffset = 0.3f; cc.slopeLimit = 45f; cc.skinWidth = 0.08f; cc.center = Vector3.up;
            int okCount = 0;
            for (int g = 0; g < 3; g++)
            {
                cc.enabled = false; probe.transform.position = Polar(TrailAz[g], 9f) + Vector3.up * 0.1f; cc.enabled = true;
                Physics.SyncTransforms();
                float vy = 0f, dt = 0.02f, rEnd = 9f; string prof = ""; float[] marks = { 3.6f, 3.05f, 2.5f }; int mi = 0;   // milieux des marches 1 et 2, sommet
                for (int it = 0; it < 1000; it++)
                {
                    Vector3 p = probe.transform.position; rEnd = RadOf(p);
                    if (rEnd <= 2.1f) break;
                    if (mi < marks.Length && rEnd <= marks[mi]) { prof += "r " + rEnd.ToString("F2") + " : " + (p.y - cc.skinWidth).ToString("F2") + " ; "; mi++; }
                    Vector3 walk = -new Vector3(p.x, 0f, p.z).normalized * 4f;
                    vy = cc.isGrounded ? -1f : vy - 9.81f * dt;
                    cc.Move((walk + Vector3.up * vy) * dt);
                }
                float feet = probe.transform.position.y - cc.skinWidth;
                bool up = rEnd <= 2.1f && Mathf.Abs(feet - topY) < 0.05f;
                if (up) okCount++;
                sb.Append("Plateau depuis l'axe " + TrailName[g] + " : " + prof + "arrivee r = " + rEnd.ToString("F2") + " m, pieds a " + feet.ToString("F2") + " m" + (up ? " -> OK (sommet atteint)" : " -> BLOQUE") + "\n");
            }
            Object.DestroyImmediate(probe);
            Transform relic = Find("VillageBlockout/Nexus/GemmeNyxessa");
            float relicFoot = -1f;
            if (relic != null) { relicFoot = 999f; foreach (Renderer rd in relic.GetComponentsInChildren<Renderer>()) if (rd.gameObject.activeInHierarchy && rd is MeshRenderer && rd.GetComponent<MeshFilter>() != null) relicFoot = Mathf.Min(relicFoot, rd.bounds.min.y); }
            sb.Append("Plateau : le joueur monte depuis " + okCount + "/3 axes (N, SE, SO) ; pied du rocher-piedestal de la relique a " + relicFoot.ToString("F2") + " m (sommet " + topY.ToString("F2") + " m)" + (okCount == 3 && Mathf.Abs(relicFoot - topY) < 0.02f ? " -> OK" : " -> A CORRIGER") + "\n");
        }
        // portail : le joueur contourne le socle. Même CharacterController, qui fait le tour du portail sur un cercle de 2,2 m
        // (demi-largeur du collider du portail 1,25 m + rayon du joueur 0,45 m + 0,5 m), point de passage tous les 10°.
        {
            Vector3 pp = Polar(PortalAz, PortalR);
            GameObject probe = new GameObject("_Test_Contour_Portail"); probe.hideFlags = HideFlags.HideAndDontSave;
            CharacterController cc = probe.AddComponent<CharacterController>();
            cc.radius = 0.45f; cc.height = 2f; cc.stepOffset = 0.3f; cc.slopeLimit = 45f; cc.skinWidth = 0.08f; cc.center = Vector3.up;
            const float ring = 2.2f;
            cc.enabled = false; probe.transform.position = pp + Polar(PortalAz + 180f, ring) + Vector3.up * 0.1f; cc.enabled = true;
            Physics.SyncTransforms();
            int reached = 0; float worst = 0f, maxFeet = 0f, vy = 0f, dt = 0.02f;
            for (int a = 10; a <= 360; a += 10)
            {
                Vector3 target = pp + Polar(PortalAz + 180f + a, ring);
                float d = 99f;
                for (int it = 0; it < 150; it++)
                {
                    Vector3 delta = target - probe.transform.position; delta.y = 0f; d = delta.magnitude;
                    if (d < 0.15f) break;
                    vy = cc.isGrounded ? -1f : vy - 9.81f * dt;
                    cc.Move((delta.normalized * Mathf.Min(4f, d / dt) + Vector3.up * vy) * dt);
                    maxFeet = Mathf.Max(maxFeet, probe.transform.position.y - cc.skinWidth);
                }
                worst = Mathf.Max(worst, d);
                if (d < 0.3f) reached++;
            }
            Object.DestroyImmediate(probe);
            Transform socle = Find("VillageBlockout/Portail/Portail_Socle");
            string dims = socle == null ? "socle absent" : "socle " + PortalPedestalRadii.Length + " assise(s), dessus a " + PortalPedestalTops[PortalPedestalTops.Length - 1].ToString("F2") + " m, bas du disque a " + (PortalHeight - PortalVisualRadius).ToString("F2") + " m";
            sb.Append("Portail : contour a " + ring + " m du centre, " + reached + "/36 points de passage atteints (ecart max " + worst.ToString("F2") + " m, pieds au plus haut " + maxFeet.ToString("F2") + " m) ; " + dims + (reached == 36 && socle != null ? " -> OK" : " -> BLOQUE") + "\n");
        }
        // maisons : écart entre boîtes de collision (toit compris) et entre emprises au sol et surfaces pavées (anneau, place du portail)
        {
            var boxes = new List<Vector4[]>(); var feet = new List<Vector4[]>(); var names = new List<string>();
            Transform ms = Find("VillageBlockout/Maisons");
            if (ms != null)
                foreach (Transform h in ms)
                {
                    BoxCollider bc = h.GetComponent<BoxCollider>(); if (bc == null) continue;
                    Vector3 c = h.TransformPoint(bc.center); float s = h.lossyScale.x;
                    boxes.Add(new Vector4[] { new Vector4(c.x, c.z, h.right.x, h.right.z), new Vector4(h.forward.x, h.forward.z, bc.size.x / 2f * s, bc.size.z / 2f * s) });
                    feet.Add(Footprint(h, h.GetComponent<MeshFilter>().sharedMesh)); names.Add(h.name);
                }
            float minHouse = 999f; string pair = "";
            for (int a = 0; a < boxes.Count; a++)
                for (int b = a + 1; b < boxes.Count; b++)
                {
                    float d = RectGap(boxes[a], boxes[b]);
                    if (d < minHouse) { minHouse = d; pair = names[a] + " / " + names[b]; }
                }
            float minPaved = 999f; string what = "";
            var tiles = new List<Transform>();
            foreach (string g in new string[] { "VillageBlockout/Nexus/Pavage_Anneau", "VillageBlockout/Portail/Portail_Place" })
            { Transform t = Find(g); if (t != null) foreach (Transform c in t) tiles.Add(c); }
            for (int a = 0; a < feet.Count; a++)
                foreach (Transform t in tiles)
                {
                    Vector4[] sq = { new Vector4(t.position.x, t.position.z, 1f, 0f), new Vector4(0f, 1f, 1f, 1f) };
                    float d = RectGap(feet[a], sq);
                    if (d < minPaved) { minPaved = d; what = names[a] + " / " + t.parent.name; }
                }
            sb.Append("Maisons : ecart minimal entre maisons (toits compris) = " + minHouse.ToString("F2") + " m (" + pair + ") ; entre emprise au sol et pavage anneau / place du portail = " + minPaved.ToString("F2") + " m (" + what + ")" + (minHouse > 0.5f && minPaved > 0.5f ? " -> OK" : " -> TROP PRES") + "\n");
        }
        // aucun collider de rocher à moins de 3 m des sentiers (sentiers de sortie et sentiers maison -> anneau)
        {
            int near = 0; string names = "";
            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!col.gameObject.name.StartsWith("Rock_") && !col.gameObject.name.StartsWith("Pierre_")) continue;
                Vector3 p = col.bounds.center; p.y = 0f; float r = RadOf(p);
                float best = 999f;
                for (int g = 0; g < 3; g++) if (r <= TrailBendEnd + 2f) best = Mathf.Min(best, Lateral(g, p));
                foreach (float haz in HouseAzimuths) { if (r > HouseR) continue; best = Mathf.Min(best, Mathf.Abs(Mathf.DeltaAngle(AzOf(p), haz)) * Mathf.Deg2Rad * r); }
                float halfSize = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
                if (best - halfSize < RockTrailClear) { near++; names += col.gameObject.name + " "; }
            }
            sb.Append("Rochers : colliders a moins de " + RockTrailClear + " m d'un couloir ou d'une allee = " + near + (near == 0 ? " -> OK" : " -> " + names) + "\n");
        }
        // espacement moyen réel des arbres (distance au plus proche voisin)
        if (trunks.Count > 1)
        {
            float sum = 0f; int n = 0;
            for (int a = 0; a < trunks.Count; a += 7)
            {
                float best = 999f;
                for (int b = 0; b < trunks.Count; b++) if (b != a) best = Mathf.Min(best, Vector3.Distance(trunks[a], trunks[b]));
                sum += best; n++;
            }
            sb.Append("Foret : " + trunks.Count + " troncs, distance moyenne au plus proche voisin = " + (sum / n).ToString("F1") + " m\n");
        }
        return sb.ToString();
    }

    // Écart entre deux rectangles orientés (0 s'ils se chevauchent) : échantillonnage des bords de chacun.
    private static float RectGap(Vector4[] a, Vector4[] b)
    {
        float best = 999f;
        foreach (Vector4[][] pr in new Vector4[][][] { new[] { a, b }, new[] { b, a } })
        {
            Vector4[] f = pr[0], g = pr[1];
            Vector2 c = new Vector2(f[0].x, f[0].y), r = new Vector2(f[0].z, f[0].w), fw = new Vector2(f[1].x, f[1].y);
            for (int k = 0; k <= 80; k++)
            {
                float t = k / 80f * 2f - 1f;
                foreach (Vector2 q in new Vector2[] { c + r * f[1].z * t + fw * f[1].w, c + r * f[1].z * t - fw * f[1].w, c + fw * f[1].w * t + r * f[1].z, c + fw * f[1].w * t - r * f[1].z })
                    best = Mathf.Min(best, FootprintDistance(g, q));
            }
        }
        return best;
    }

    private static int Cell(Vector3 p, int N) { int i = Mathf.Clamp(Mathf.FloorToInt(p.x + TerrainHalf), 0, N - 1); int j = Mathf.Clamp(Mathf.FloorToInt(p.z + TerrainHalf), 0, N - 1); return j * N + i; }
    private static HashSet<int> Flood(bool[,] ok, int N, Vector3 start)
    {
        var seen = new HashSet<int>(); var q = new Queue<int>();
        int s = Cell(start, N); seen.Add(s); q.Enqueue(s);
        int[] di = { 1, -1, 0, 0 }, dj = { 0, 0, 1, -1 };
        while (q.Count > 0)
        {
            int k = q.Dequeue(); int i = k % N, j = k / N;
            for (int d = 0; d < 4; d++)
            {
                int ii = i + di[d], jj = j + dj[d];
                if (ii < 0 || jj < 0 || ii >= N || jj >= N) continue;
                int kk = jj * N + ii;
                if (seen.Contains(kk) || !ok[ii, jj]) continue;
                seen.Add(kk); q.Enqueue(kk);
            }
        }
        return seen;
    }

    // ---------------- Sol ----------------
    // Hauteur du sol : 0 jusqu'à GroundFlatRadius (village, plateau, place du portail, allées), puis relief de Perlin très léger.
    public static float GroundHeight(float x, float z)
    {
        float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(GroundFlatRadius, GroundBlendRadius, Mathf.Sqrt(x * x + z * z)));
        if (k <= 0f) return 0f;
        float n = (Mathf.PerlinNoise(x * 0.07f + 31.7f, z * 0.07f + 12.3f) * 2f - 1f) * 0.75f
                + (Mathf.PerlinNoise(x * 0.23f + 5.1f, z * 0.23f + 77.9f) * 2f - 1f) * 0.25f;
        return GroundRelief * k * n;
    }

    // Maillage du sol (technique Relic DefenseTerrain) : grille de GroundStep, diagonales alternées, sommets non partagés
    // (facettes, normales dures). Chaque triangle prend une teinte de GroundPalette : herbes mêlées par un bruit de Perlin basse
    // fréquence + tirage, taches claires et sombres, terre au pied des troncs et en liseré autour des pavés et des allées.
    private static void BuildGround(Transform parent, Transform arbres, Transform root)
    {
        Texture2D palette = GroundPaletteTexture();
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(GroundMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SolVillage" };
            AssetDatabase.CreateAsset(mat, GroundMatPath);
        }
        mat.SetTexture("_BaseMap", palette); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(mat);

        // troncs (grille de hachage de 4 m) et surfaces pavées (dalles Dungeon 2 m, tuiles path_* 1,9 m, plateau, socle)
        var trunkCells = new Dictionary<long, List<Vector2>>();
        foreach (Transform t in arbres)
        {
            Vector2 p = new Vector2(t.position.x, t.position.z);
            long key = CellKey(p);
            List<Vector2> list; if (!trunkCells.TryGetValue(key, out list)) { list = new List<Vector2>(); trunkCells[key] = list; }
            list.Add(p);
        }
        var paved = new List<Vector4>();   // xmin, zmin, xmax, zmax
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            float half = t.name.StartsWith("floor_tile") ? 1f : t.name.StartsWith("path_") ? 0.95f : 0f;
            if (half > 0f) paved.Add(new Vector4(t.position.x - half, t.position.z - half, t.position.x + half, t.position.z + half));
        }
        Vector3 portal = Polar(PortalAz, PortalR);
        float plateauRadius = PlateauTopRadius + StepWidth * (PlateauSteps - 1);

        var rng = new System.Random(DressSeed + 7);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        int cells = Mathf.RoundToInt(TerrainHalf * 2f / GroundStep);
        int[] hist = new int[GroundPalette.Length];
        System.Action<Vector3, Vector3, Vector3> add = (a, b, c) =>
        {
            Vector2 m = new Vector2((a.x + b.x + c.x) / 3f, (a.z + b.z + c.z) / 3f);
            float r1 = (float)rng.NextDouble(), r2 = (float)rng.NextDouble();
            float low = Mathf.PerlinNoise(m.x * 0.035f + 3.1f, m.y * 0.035f + 7.7f);
            int idx = Mathf.Clamp(Mathf.FloorToInt(low * 4.6f - 0.3f + (r1 - 0.5f) * 0.55f), 0, 3);   // tirage : bords de zones en dents de scie
            float spot = Mathf.PerlinNoise(m.x * 0.12f + 11f, m.y * 0.12f + 5f);
            if (spot > 0.74f && r1 < 0.8f) idx = 4; else if (spot < 0.24f && r1 < 0.8f) idx = 5;
            // terre autour des pavés (près du village seulement) et au pied des troncs
            if (m.magnitude < GroundFlatRadius)
            {
                float dp = 99f;
                if (m.magnitude < plateauRadius + 0.2f || Vector2.Distance(m, new Vector2(portal.x, portal.z)) < 1f) dp = 0f;
                foreach (Vector4 q in paved)
                {
                    float dx = Mathf.Max(q.x - m.x, 0f, m.x - q.z), dz = Mathf.Max(q.y - m.y, 0f, m.y - q.w);
                    dp = Mathf.Min(dp, Mathf.Sqrt(dx * dx + dz * dz));
                    if (dp <= 0f) break;
                }
                if (dp > 0f && dp < GroundEarthPaved * (0.5f + 0.7f * r2)) idx = r2 < 0.3f ? 7 : 6;
            }
            float dt = 99f;
            for (int ox = -1; ox <= 1; ox++)
                for (int oz = -1; oz <= 1; oz++)
                {
                    List<Vector2> list;
                    if (trunkCells.TryGetValue(CellKey(m + new Vector2(ox * 4f, oz * 4f)), out list))
                        foreach (Vector2 p in list) dt = Mathf.Min(dt, Vector2.Distance(m, p));
                }
            if (dt < GroundEarthTrunk * (0.7f + 0.5f * r2)) idx = dt < GroundEarthTrunk * 0.5f ? 7 : 6;
            foreach (Vector4[] f in HouseFootprints)
            {
                float dh = FootprintDistance(f, m);
                if (dh < GroundEarthHouse * (0.5f + 0.7f * r2)) { idx = dh < 0.3f && r2 < 0.5f ? 7 : 6; break; }
            }
            hist[idx]++;
            Vector2 u = new Vector2((idx + 0.5f) / GroundPalette.Length, 0.5f);
            Vector3 nrm = Vector3.Cross(b - a, c - a).normalized;
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(c); n.Add(nrm); n.Add(nrm); n.Add(nrm); uv.Add(u); uv.Add(u); uv.Add(u);
            tri.Add(i0); tri.Add(i0 + 1); tri.Add(i0 + 2);
        };
        for (int j = 0; j < cells; j++)
            for (int i = 0; i < cells; i++)
            {
                float x0 = -TerrainHalf + i * GroundStep, x1 = x0 + GroundStep, z0 = -TerrainHalf + j * GroundStep, z1 = z0 + GroundStep;
                Vector3 a = new Vector3(x0, GroundHeight(x0, z0), z0), b = new Vector3(x0, GroundHeight(x0, z1), z1);
                Vector3 c = new Vector3(x1, GroundHeight(x1, z1), z1), d = new Vector3(x1, GroundHeight(x1, z0), z0);
                if ((i + j) % 2 == 0) { add(a, b, c); add(a, c, d); }   // diagonale alternée (comme Relic)
                else { add(a, b, d); add(b, c, d); }
            }

        if (!AssetDatabase.IsValidFolder("Assets/Art/Meshes")) AssetDatabase.CreateFolder("Assets/Art", "Meshes");
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh();
        mesh.Clear();
        mesh.name = "SolVillage";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, GroundMeshPath); else EditorUtility.SetDirty(mesh);

        GameObject ground = new GameObject("Sol_Village");
        ground.transform.SetParent(parent, false);
        ground.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = ground.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        AssetDatabase.SaveAssets();
        GroundStats = tri.Count / 3 + " facettes ; teintes " + string.Join("/", System.Array.ConvertAll(hist, x => x.ToString()));
    }

    public static string GroundStats = "";
    private static long CellKey(Vector2 p) { return ((long)Mathf.FloorToInt(p.x / 4f) << 32) ^ (uint)Mathf.FloorToInt(p.y / 4f); }

    // Palette du sol : une case de 4 x 4 texels par teinte, filtrage point, sans mipmaps (écrite seulement si elle change).
    private static Texture2D GroundPaletteTexture()
    {
        int w = GroundPalette.Length * 4;
        Texture2D t = new Texture2D(w, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < w; x++) for (int y = 0; y < 4; y++) t.SetPixel(x, y, GroundPalette[x / 4]);
        t.Apply();
        byte[] png = t.EncodeToPNG();
        Object.DestroyImmediate(t);
        if (!AssetDatabase.IsValidFolder("Assets/Art/Textures")) AssetDatabase.CreateFolder("Assets/Art", "Textures");
        string full = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), GroundTexPath);
        bool changed = !System.IO.File.Exists(full) || !System.Linq.Enumerable.SequenceEqual(System.IO.File.ReadAllBytes(full), png);
        if (changed) { System.IO.File.WriteAllBytes(full, png); AssetDatabase.ImportAsset(GroundTexPath, ImportAssetOptions.ForceUpdate); }
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(GroundTexPath);
        if (imp.filterMode != FilterMode.Point || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed || imp.wrapMode != TextureWrapMode.Clamp)
        {
            imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.sRGBTexture = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexPath);
    }

    // ---------------- Ambiance jour / nuit ----------------
    // Rendu : DayCycle (copie Visual only de Relic) sur la lumière directionnelle, préréglages jour / nuit dans
    // AmbianceVillage.asset (réécrit à chaque génération d'après les paramètres ci-dessous) ; horloge : CycleJourNuit.
    private static Ambiance.Preset AmbianceJour()
    {
        return new Ambiance.Preset
        {
            sunEuler = new Vector2(48f, 335f), sunColor = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.3f,
            ambientSky = new Color(0.62f, 0.68f, 0.78f), ambientEquator = new Color(0.46f, 0.49f, 0.52f), ambientGround = new Color(0.24f, 0.24f, 0.24f),
            fogColor = new Color(0.74f, 0.8f, 0.88f), fogStart = 60f, fogEnd = 260f,   // voile léger au loin
            skyTint = new Color(0.42f, 0.6f, 0.9f), skyExposure = 1.25f, atmosphereThickness = 0.85f,
        };
    }
    private static Ambiance.Preset AmbianceNuit()
    {
        return new Ambiance.Preset
        {
            // la lune : lumière directionnelle froide et faible, haute au sud-ouest
            sunEuler = new Vector2(38f, 200f), sunColor = new Color(0.55f, 0.66f, 0.95f), sunIntensity = 0.32f,
            ambientSky = new Color(0.11f, 0.14f, 0.26f), ambientEquator = new Color(0.07f, 0.09f, 0.17f), ambientGround = new Color(0.02f, 0.025f, 0.05f),
            fogColor = new Color(0.035f, 0.05f, 0.1f), fogStart = 15f, fogEnd = 95f,
            skyTint = new Color(0.05f, 0.08f, 0.22f), skyExposure = 0.18f, atmosphereThickness = 0.5f,
        };
    }
    public const string AmbianceVillagePath = "Assets/VFX/_Ambiance/AmbianceVillage.asset";
    public const float LanternScale = 1.3f;      // lantern_standing (Halloween Bits) : 0,93 m -> 1,2 m
    public const float LanternRange = 7f;
    // Portes (m, repère de la maison : x à droite, z vers le Nexus) : home_A porte au milieu, home_B porte décalée à -1,05 m.
    public static readonly float[] HouseDoorX = { 0f, -1.05f };
    public static readonly Vector2[] LanternLocal = { new Vector2(1.5f, 3.3f), new Vector2(-2.45f, 4.55f) };

    private static void BuildAmbiance(Transform root)
    {
        Transform amb = Group(root, "Ambiance");

        // préréglages et lumière directionnelle
        Ambiance asset = AssetDatabase.LoadAssetAtPath<Ambiance>(AmbianceVillagePath);
        if (asset == null) { asset = ScriptableObject.CreateInstance<Ambiance>(); AssetDatabase.CreateAsset(asset, AmbianceVillagePath); }
        asset.day = AmbianceJour(); asset.dusk = AmbianceNuit(); asset.transitionSeconds = 5f; asset.duskLeadSeconds = 0f;
        EditorUtility.SetDirty(asset);
        Light sun = RenderSettings.sun;
        if (sun == null) { GameObject dl = GameObject.Find("VillageBlockout/Environnement/Directional Light"); if (dl != null) sun = dl.GetComponent<Light>(); }
        DayCycle dc = sun.GetComponent<DayCycle>(); if (dc == null) dc = sun.gameObject.AddComponent<DayCycle>();
        var so = new SerializedObject(dc); so.FindProperty("ambiance").objectReferenceValue = asset; so.ApplyModifiedPropertiesWithoutUndo();
        dc.nuit = 0f; dc.heure = 0.5f;
        sun.shadows = LightShadows.Soft;
        // réglages de la scène (vue de l'éditeur = jour) : brouillard linéaire, lumière ambiante à trois couleurs
        Ambiance.Preset j = asset.day;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = j.fogColor; RenderSettings.fogStartDistance = j.fogStart; RenderSettings.fogEndDistance = j.fogEnd;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = j.ambientSky; RenderSettings.ambientEquatorColor = j.ambientEquator; RenderSettings.ambientGroundColor = j.ambientGround;

        // lumière de nuit de Nyxessa : la relique éclaire la place, le plateau et les maisons proches
        Transform relic = root.Find("Nexus/GemmeNyxessa");
        Light nyx = null;
        if (relic != null)
        {
            GameObject ln = new GameObject("Lumiere_Nuit_Nyxessa");
            ln.transform.SetParent(relic, false); ln.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            nyx = ln.AddComponent<Light>(); nyx.type = LightType.Point; nyx.range = 30f; nyx.intensity = 0f; nyx.shadows = LightShadows.None; nyx.enabled = false;
        }

        // maisons : matériau à fenêtres émissives, lanterne allumée près de chaque porte
        Material houseMat = HouseNightMaterial();
        Material flameMat = FlatMaterial("Assets/Art/Materials/Lanterne_Flamme.mat", "Universal Render Pipeline/Unlit", new Color(1f, 0.62f, 0.28f) * 2.2f);
        var lights = new List<Light>(); var flames = new List<GameObject>(); var houses = new List<Renderer>();
        Transform lanternes = Group(amb, "Lanternes");
        Transform ms = root.Find("Maisons");
        if (ms != null)
            foreach (Transform h in ms)
            {
                MeshRenderer mr = h.GetComponent<MeshRenderer>(); if (mr == null) continue;
                if (houseMat != null) mr.sharedMaterial = houseMat;
                houses.Add(mr);
                int type = h.name.EndsWith("_A") ? 0 : 1;
                Vector3 f = h.forward; f.y = 0f; f.Normalize(); Vector3 rt = Vector3.Cross(Vector3.up, f);
                Vector3 p = h.position + rt * LanternLocal[type].x + f * LanternLocal[type].y; p.y = GroundHeight(p.x, p.z);
                GameObject lan = Place(lanternes, HalloweenRoot + "lantern_standing", p, Quaternion.LookRotation(f).eulerAngles.y, LanternScale);
                if (lan == null) continue;
                lan.name = "Lanterne_" + h.name;
                Vector3 glass = p + Vector3.up * 0.62f * LanternScale;
                GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Sphere); fl.name = "Flamme";
                Object.DestroyImmediate(fl.GetComponent<Collider>());
                fl.transform.SetParent(lan.transform, false); fl.transform.position = glass; fl.transform.localScale = Vector3.one * 0.14f / LanternScale;
                MeshRenderer fr = fl.GetComponent<MeshRenderer>(); fr.sharedMaterial = flameMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fl.SetActive(false); flames.Add(fl);
                GameObject lg = new GameObject("Lumiere"); lg.transform.SetParent(lan.transform, false); lg.transform.position = glass + f * 0.15f;
                Light l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = LanternRange; l.color = new Color(1f, 0.64f, 0.34f); l.intensity = 0f; l.shadows = LightShadows.None; l.enabled = false;
                lights.Add(l);
            }

        // brume au sol (GroundMist, copie de sandbox-vfx) autour du village
        GameObject brume = new GameObject("Brume_Sol"); brume.transform.SetParent(amb, false);
        GroundMist gm = brume.AddComponent<GroundMist>();
        var sm = new SerializedObject(gm);
        sm.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_Ambiance/GroundMist.mat");
        sm.FindProperty("centre").objectReferenceValue = root.Find("Nexus");
        sm.FindProperty("clearRadius").floatValue = 6.5f;
        sm.FindProperty("color").colorValue = new Color(0.36f, 0.42f, 0.62f);
        sm.FindProperty("glow").colorValue = new Color(0.04f, 0.05f, 0.1f);
        sm.FindProperty("nightAlpha").floatValue = 0.16f;                   // brume discrète : on doit voir arriver les squelettes
        sm.FindProperty("height").vector2Value = new Vector2(0.7f, 1.2f);
        sm.ApplyModifiedPropertiesWithoutUndo();

        // lucioles discrètes dans la prairie (particules émissives, sans lumière)
        GameObject lg2 = new GameObject("Lucioles"); lg2.transform.SetParent(amb, false); lg2.transform.position = new Vector3(0f, 0.9f, 0f);
        ParticleSystem ps = lg2.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 6f; main.startSpeed = 0.15f; main.startSize = 0.07f; main.maxParticles = 60;
        main.startColor = new Color(0.85f, 1f, 0.45f) * 2.5f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = true;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Donut; sh.radius = 20f; sh.donutRadius = 6f; sh.rotation = new Vector3(90f, 0f, 0f);
        var no = ps.noise; no.enabled = true; no.strength = 0.5f; no.frequency = 0.25f;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));
        var pr = lg2.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = FlatMaterial("Assets/Art/Materials/Lucioles.mat", "Universal Render Pipeline/Particles/Unlit", Color.white);
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // horloge du cycle
        CycleJourNuit cycle = amb.gameObject.AddComponent<CycleJourNuit>();
        cycle.dayCycle = dc;
        Transform portal = root.Find("Portail/PortailDonjon");
        cycle.portail = portal != null ? portal.GetComponent<PortalVisual>() : null;
        cycle.lumiereNyxessa = nyx; cycle.intensiteNyxessa = 9f;
        cycle.lanternes = lights.ToArray(); cycle.flammes = flames.ToArray(); cycle.maisons = houses.ToArray();
        cycle.lucioles = ps;
        AmbianceLights = lights.Count + (nyx != null ? 1 : 0);
        AssetDatabase.SaveAssets();
    }

    public static int AmbianceLights;

    // Matériau simple (créé s'il manque), couleur de base imposée.
    private static Material FlatMaterial(string path, string shader, Color color)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Matériau des maisons pour la nuit : copie de KayKit_Hexagons_medieval avec une carte d'émission (texture générée,
    // orange sur les texels du verre des fenêtres, noir ailleurs). L'intensité est pilotée par CycleJourNuit (MaterialPropertyBlock).
    public static readonly Rect WindowGlassUv = new Rect(0.79f, 0.54f, 0.052f, 0.18f);   // verre des fenêtres : éventail u 0,795-0,838, v 0,549-0,708 (centre)
    private static Material HouseNightMaterial()
    {
        Material src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/KayKit_Hexagons_medieval.mat");
        if (src == null) return null;
        const string texPath = "Assets/Art/Textures/Maisons_Fenetres_Emission.png", matPath = "Assets/Art/Materials/KayKit_Hexagons_medieval_Maisons.mat";
        const int size = 256;
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color on = new Color(1f, 0.62f, 0.3f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, WindowGlassUv.Contains(new Vector2((x + 0.5f) / size, (y + 0.5f) / size)) ? on : Color.black);
        t.Apply();
        byte[] png = t.EncodeToPNG(); Object.DestroyImmediate(t);
        string full = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), texPath);
        if (!System.IO.File.Exists(full) || !System.Linq.Enumerable.SequenceEqual(System.IO.File.ReadAllBytes(full), png))
        { System.IO.File.WriteAllBytes(full, png); AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate); }
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (imp.filterMode != FilterMode.Point || imp.mipmapEnabled)
        { imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport(); }
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, matPath); }
        else m.CopyPropertiesFromMaterial(src);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        // presque noir le jour (pas tout à fait : URP couperait le mot-clé _EMISSION) ; CycleJourNuit allume les fenêtres la nuit
        m.SetColor("_EmissionColor", Color.white * 0.001f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Menus : figer une phase (en Play) ou choisir la phase de départ (hors Play).
    [MenuItem("Deathless/Village/Ambiance/Jour")] public static void AmbJour() { FigerPhase(CycleJourNuit.Phase.Jour); }
    [MenuItem("Deathless/Village/Ambiance/Crépuscule")] public static void AmbCrep() { FigerPhase(CycleJourNuit.Phase.Crepuscule); }
    [MenuItem("Deathless/Village/Ambiance/Nuit")] public static void AmbNuit() { FigerPhase(CycleJourNuit.Phase.Nuit); }
    [MenuItem("Deathless/Village/Ambiance/Aube")] public static void AmbAube() { FigerPhase(CycleJourNuit.Phase.Aube); }
    [MenuItem("Deathless/Village/Ambiance/Reprendre le cycle")]
    public static void AmbReprendre()
    {
        CycleJourNuit c = Object.FindFirstObjectByType<CycleJourNuit>(); if (c == null) return;
        if (Application.isPlaying) c.Reprendre();
        else { c.fige = false; EditorUtility.SetDirty(c); }
    }
    public static void FigerPhase(CycleJourNuit.Phase p)
    {
        CycleJourNuit c = Object.FindFirstObjectByType<CycleJourNuit>();
        if (c == null) { Debug.LogWarning("CycleJourNuit introuvable (Deathless > Village > Générer)."); return; }
        if (Application.isPlaying) c.Figer(p);
        else { c.phaseDepart = p; c.fige = true; EditorUtility.SetDirty(c); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(c.gameObject.scene); }
    }

    // ---------------- Placement ----------------
    private static Material Mat(string name) { return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat"); }

    private static GameObject Place(Transform parent, string assetPath, Vector3 position, float yaw, float scale)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath + ".fbx");
        if (model == null) { Debug.LogWarning("Pièce KayKit introuvable : " + assetPath); return null; }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        instance.name = System.IO.Path.GetFileName(assetPath);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static void PlaceTree(Transform parent, Vector3 position, bool bare)
    {
        string[] pool = bare ? BareTrees : Trees;
        float scale = TreeScale * Random.Range(1f - TreeScaleVar, 1f + TreeScaleVar);
        GameObject tree = Place(parent, ForestRoot + pool[Random.Range(0, pool.Length)], position, Random.Range(0f, 360f), scale);
        if (tree == null) return;
        CapsuleCollider capsule = tree.AddComponent<CapsuleCollider>();
        capsule.radius = TreeColliderRadius / scale;
        capsule.height = 7f / scale;
        capsule.center = new Vector3(0f, 3.5f / scale, 0f);
    }

    private static GameObject Dungeon(Transform parent, string piece, Vector3 position, float yaw) { return Place(parent, DungeonRoot + piece, position, yaw, 1f); }
    private static GameObject Halloween(Transform parent, string piece, Vector3 position, float yaw) { return Place(parent, HalloweenRoot + piece, position, yaw, 1f); }

    // Allée pavée droite de a à b : tuiles path_* tous les 1,9 m, `rows` rangées (1 = 2 m de large, 2 = 4 m), sans collider.
    private static void PavedPath(Transform parent, Vector3 a, Vector3 b, int rows)
    {
        Vector3 d = b - a; d.y = 0f; float len = d.magnitude; Vector3 dir = d / Mathf.Max(0.001f, len);
        Vector3 side = new Vector3(dir.z, 0f, -dir.x);
        int i = 0;
        for (float t = 0.95f; t < len + 0.5f; t += 1.9f)
        {
            Vector3 c = a + dir * Mathf.Min(t, len - 0.5f);
            if (rows == 1) Halloween(parent, PathTiles[Random.Range(0, PathTiles.Length)], c + Vector3.up * (PathY + (i++ % 2) * PathAlt), 90f * Random.Range(0, 4));
            else foreach (float off in new float[] { -0.95f, 0.95f })
                Halloween(parent, PathTiles[Random.Range(0, PathTiles.Length)], c + side * off + Vector3.up * (PathY + (i++ % 2) * PathAlt), 90f * Random.Range(0, 4));
        }
    }

    // Assise de pierre : prisme octogonal (rayon circonscrit, dessus à `top`) avec MeshCollider convexe exact, rendu comme fond
    // sombre (dessus abaissé de SupportDrop), habillé de dalles découpées au contour (dessus) et de blocs chanfreinés (contremarche
    // visible entre `bottom` et `top`). `inner` = rayon de l'assise du dessus (0 s'il n'y en a pas) : les dalles entièrement
    // dessous sont omises.
    private static GameObject StoneTier(Transform parent, string name, float radius, float inner, float bottom, float top,
        float tileScale, Vector2 blockLength, int rows, Material mat, System.Random rng)
    {
        GameObject tier = new GameObject(name);
        tier.transform.SetParent(parent, false);
        Mesh collider = Prism(PlateauSides, radius, top, MortarUv);
        Mesh support = Prism(PlateauSides, radius, top - SupportDrop, MortarUv);
        tier.AddComponent<MeshFilter>().sharedMesh = support;
        tier.AddComponent<MeshRenderer>().sharedMaterial = mat;
        MeshCollider mc = tier.AddComponent<MeshCollider>(); mc.sharedMesh = collider; mc.convex = true;

        var tiles = new MeshBuild();
        float apothem = radius * Mathf.Cos(Mathf.PI / PlateauSides);
        float innerApothem = inner * Mathf.Cos(Mathf.PI / PlateauSides);
        float step = 2f * tileScale;
        int n = Mathf.CeilToInt(radius / step) + 1;
        int tileCount = 0;
        for (int iz = -n; iz < n; iz++)
            for (int ix = -n; ix < n; ix++)
            {
                float cx = (ix + 0.5f) * step, cz = (iz + 0.5f) * step;
                string piece = Pavers[rng.Next(Pavers.Length)]; int quarter = rng.Next(4);
                // dalle entièrement sous l'assise du dessus, ou entièrement hors du contour : omise
                bool allInner = inner > 0f, anyInside = false;
                for (int c = 0; c < 4; c++)
                {
                    Vector2 corner = new Vector2(cx + ((c & 1) == 0 ? -tileScale : tileScale), cz + ((c & 2) == 0 ? -tileScale : tileScale));
                    if (OctagonDistance(corner) > innerApothem - 0.02f) allInner = false;
                    if (OctagonDistance(corner) < apothem) anyInside = true;
                }
                if (allInner || (!anyInside && OctagonDistance(new Vector2(cx, cz)) > apothem + step)) continue;
                if (AddClippedTile(tiles, piece, new Vector3(cx, top + DressTileLift - 0.05f * tileScale, cz), 90f * quarter, tileScale, apothem)) tileCount++;
            }
        AddChild(tier, "Dalles", tiles.ToMesh("Dalles_" + name), mat);

        var blocks = new MeshBuild();
        float front = apothem + BlockProud;
        float sideLength = 2f * front * Mathf.Tan(Mathf.PI / PlateauSides);
        float y0 = bottom - 0.02f, y1 = top + DressTileLift + BlockTopLift, h = top - bottom;
        int blockCount = 0;
        for (int side = 0; side < PlateauSides; side++)
        {
            float theta = (side + 1) * 2f * Mathf.PI / PlateauSides;
            Vector3 fwd = new Vector3(Mathf.Sin(theta), 0f, Mathf.Cos(theta)), right = new Vector3(Mathf.Cos(theta), 0f, -Mathf.Sin(theta));
            for (int r = 0; r < rows; r++)
            {
                float rb = r == 0 ? y0 : bottom + h * r / rows + BlockJoint / 2f;
                float rt = r == rows - 1 ? y1 : bottom + h * (r + 1) / rows - BlockJoint / 2f;
                // joints décalés d'un rang à l'autre : les rangs impairs commencent par un demi-bloc
                float t = -sideLength / 2f;
                bool first = true;
                while (t < sideLength / 2f - 0.01f)
                {
                    float len = Mathf.Lerp(blockLength.x, blockLength.y, (float)rng.NextDouble());
                    if (first && (r & 1) == 1) len *= 0.5f;
                    first = false;
                    float rest = sideLength / 2f - t;
                    if (rest - len < blockLength.x * 0.6f) len = rest;   // pas de chute trop courte en bout de côté
                    float a = t + BlockJoint / 2f, b = t + len - BlockJoint / 2f;
                    Vector3 center = fwd * (front - BlockDepth / 2f) + right * ((a + b) / 2f) + Vector3.up * ((rb + rt) / 2f);
                    ChamferBox(blocks, center, right, Vector3.up, fwd, new Vector3((b - a) / 2f, (rt - rb) / 2f, BlockDepth / 2f), BlockChamfer, BlockUvs[rng.Next(BlockUvs.Length)]);
                    blockCount++;
                    t += len;
                }
            }
        }
        AddChild(tier, "Blocs", blocks.ToMesh("Blocs_" + name), mat);
        DressPieces += tileCount + blockCount;
        return tier;
    }

    public static int DressPieces;   // dalles + blocs posés par la dernière génération (rapport)

    // Distance « octogonale » : max des projections sur les normales des côtés (= apothème du contour qui passe par p).
    private static float OctagonDistance(Vector2 p)
    {
        float d = -999f;
        for (int i = 0; i < PlateauSides; i++)
        {
            float th = i * 2f * Mathf.PI / PlateauSides;
            d = Mathf.Max(d, p.x * Mathf.Sin(th) + p.y * Mathf.Cos(th));
        }
        return d;
    }

    private static void AddChild(GameObject parent, string name, Mesh mesh, Material mat)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // Accumulateur de maillage (sommets non partagés, normales dures).
    private class MeshBuild
    {
        public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> U = new List<Vector2>(); public readonly List<int> T = new List<int>();
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = V.Count; V.Add(a); V.Add(b); V.Add(c); N.Add(na); N.Add(nb); N.Add(nc); U.Add(ua); U.Add(ub); U.Add(uc);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }
        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(V); m.SetNormals(N); m.SetUVs(0, U); m.SetTriangles(T, 0); m.RecalculateBounds();
            return m;
        }
    }

    private static readonly Dictionary<string, Mesh> tileMeshCache = new Dictionary<string, Mesh>();

    // Dalle Dungeon (pièce, centre du bas, quart de tour, échelle) découpée au contour octogonal d'apothème `apothem`
    // (Sutherland-Hodgman triangle par triangle, UV et normales interpolées). Vrai si quelque chose reste.
    private static bool AddClippedTile(MeshBuild mb, string piece, Vector3 origin, float yaw, float scale, float apothem)
    {
        Mesh src;
        if (!tileMeshCache.TryGetValue(piece, out src) || src == null)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonRoot + piece + ".fbx");
            if (model == null) return false;
            src = model.GetComponentInChildren<MeshFilter>().sharedMesh; tileMeshCache[piece] = src;
        }
        Vector3[] v = src.vertices; Vector3[] nr = src.normals; Vector2[] uv = src.uv; int[] tr = src.triangles;
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        int before = mb.T.Count;
        var poly = new List<Vector3>(8); var pn = new List<Vector3>(8); var pu = new List<Vector2>(8);
        var np = new List<Vector3>(8); var nn = new List<Vector3>(8); var nu = new List<Vector2>(8);
        for (int k = 0; k < tr.Length; k += 3)
        {
            poly.Clear(); pn.Clear(); pu.Clear();
            for (int j = 0; j < 3; j++) { int id = tr[k + j]; poly.Add(origin + q * (v[id] * scale)); pn.Add(q * nr[id]); pu.Add(uv[id]); }
            for (int s = 0; s < PlateauSides && poly.Count >= 3; s++)
            {
                float th = s * 2f * Mathf.PI / PlateauSides; float sx = Mathf.Sin(th), sz = Mathf.Cos(th);
                np.Clear(); nn.Clear(); nu.Clear();
                for (int j = 0; j < poly.Count; j++)
                {
                    int j2 = (j + 1) % poly.Count;
                    float da = poly[j].x * sx + poly[j].z * sz - apothem, db = poly[j2].x * sx + poly[j2].z * sz - apothem;
                    if (da <= 0f) { np.Add(poly[j]); nn.Add(pn[j]); nu.Add(pu[j]); }
                    if ((da <= 0f) != (db <= 0f))
                    {
                        float f = da / (da - db);
                        np.Add(Vector3.Lerp(poly[j], poly[j2], f)); nn.Add(Vector3.Lerp(pn[j], pn[j2], f).normalized); nu.Add(Vector2.Lerp(pu[j], pu[j2], f));
                    }
                }
                poly.Clear(); poly.AddRange(np); pn.Clear(); pn.AddRange(nn); pu.Clear(); pu.AddRange(nu);
            }
            for (int j = 1; j + 1 < poly.Count; j++) mb.Tri(poly[0], poly[j], poly[j + 1], pn[0], pn[j], pn[j + 1], pu[0], pu[j], pu[j + 1]);
        }
        return mb.T.Count > before;
    }

    // Bloc chanfreiné (6 faces, 12 biseaux, 8 coins) centré en `c`, repère (r, u, f), demi-tailles h, UV fixe.
    // Chaque face est orientée vers l'extérieur (le bloc est convexe et centré sur l'origine de son repère).
    private static void ChamferBox(MeshBuild mb, Vector3 c, Vector3 r, Vector3 u, Vector3 f, Vector3 h, float ch, Vector2 uv)
    {
        ch = Mathf.Min(ch, 0.45f * Mathf.Min(h.x, Mathf.Min(h.y, h.z)));
        Vector3 inner = h - Vector3.one * ch;
        System.Func<Vector3, Vector3> W = p => c + r * p.x + u * p.y + f * p.z;
        System.Action<Vector3, Vector3, Vector3> tri = (a, b, d) =>
        {
            Vector3 nl = Vector3.Cross(b - a, d - a);
            if (Vector3.Dot(nl, a + b + d) < 0f) { Vector3 tmp = b; b = d; d = tmp; nl = -nl; }
            Vector3 nw = (r * nl.x + u * nl.y + f * nl.z).normalized;
            mb.Tri(W(a), W(b), W(d), nw, nw, nw, uv, uv, uv);
        };
        System.Action<Vector3, Vector3, Vector3, Vector3> quad = (a, b, d, e) => { tri(a, b, d); tri(a, d, e); };
        for (int ax = 0; ax < 3; ax++)
            for (int sg = -1; sg <= 1; sg += 2)
            {
                // face principale
                int a1 = (ax + 1) % 3, a2 = (ax + 2) % 3;
                Vector3[] p = new Vector3[4];
                for (int k = 0; k < 4; k++)
                {
                    Vector3 q = Vector3.zero; q[ax] = sg * h[ax];
                    q[a1] = (k == 0 || k == 3 ? -1 : 1) * inner[a1]; q[a2] = (k < 2 ? -1 : 1) * inner[a2];
                    p[k] = q;
                }
                quad(p[0], p[1], p[2], p[3]);
            }
        for (int ax = 0; ax < 3; ax++)
        {
            int a1 = (ax + 1) % 3, a2 = (ax + 2) % 3;   // biseau entre les faces a1 et a2, le long de l'axe ax
            for (int s1 = -1; s1 <= 1; s1 += 2)
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    Vector3 pa = Vector3.zero, pb = Vector3.zero;
                    pa[a1] = s1 * h[a1]; pa[a2] = s2 * inner[a2];
                    pb[a1] = s1 * inner[a1]; pb[a2] = s2 * h[a2];
                    Vector3 pa0 = pa, pa1 = pa, pb0 = pb, pb1 = pb;
                    pa0[ax] = -inner[ax]; pa1[ax] = inner[ax]; pb0[ax] = -inner[ax]; pb1[ax] = inner[ax];
                    quad(pa0, pa1, pb1, pb0);
                }
        }
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    tri(new Vector3(sx * h.x, sy * inner.y, sz * inner.z), new Vector3(sx * inner.x, sy * h.y, sz * inner.z), new Vector3(sx * inner.x, sy * inner.y, sz * h.z));
    }

    // Prisme régulier (n côtés, rayon circonscrit, hauteur) posé au sol, UV fixe (teinte de l'atlas), normales dures.
    private static Mesh Prism(int sides, float radius, float height, Vector2 uv)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>(); var u = new List<Vector2>();
        System.Action<Vector3, Vector3, Vector3, Vector3> quad = (p0, p1, p2, p3) => {
            // p0..p3 = haut a0, haut a1, bas a1, bas a0 : vu de l'extérieur, a1 est à gauche -> triangles (0, 2, 1), (0, 3, 2)
            // en sens horaire (face avant Unity) et normale vers l'extérieur
            Vector3 nn = Vector3.Cross(p2 - p0, p1 - p0).normalized; int b = v.Count;
            v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3); for (int k = 0; k < 4; k++) { n.Add(nn); u.Add(uv); }
            t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
        };
        for (int i = 0; i < sides; i++)
        {
            float a0 = (i + 0.5f) * 2f * Mathf.PI / sides, a1 = (i + 1.5f) * 2f * Mathf.PI / sides;
            Vector3 p0 = new Vector3(Mathf.Sin(a0) * radius, 0f, Mathf.Cos(a0) * radius), p1 = new Vector3(Mathf.Sin(a1) * radius, 0f, Mathf.Cos(a1) * radius);
            quad(p0 + Vector3.up * height, p1 + Vector3.up * height, p1, p0);               // face latérale (vers l'extérieur)
            int b = v.Count; v.Add(new Vector3(0f, height, 0f)); v.Add(p1 + Vector3.up * height); v.Add(p0 + Vector3.up * height);
            for (int k = 0; k < 3; k++) { n.Add(Vector3.up); u.Add(uv); }
            t.Add(b); t.Add(b + 2); t.Add(b + 1);                                          // dessus (centre, p0, p1 : sens horaire vu d'en haut, face avant)
        }
        Mesh m = new Mesh { name = "Prisme_" + sides + "_" + radius + "_" + height };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, u); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    // Rocher Forest à une taille monde donnée (extension max en m), avec ou sans collider boîte.
    private static GameObject PlaceRock(Transform parent, string piece, Vector3 position, float worldSize, bool collider)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ForestRoot + piece + ".fbx");
        if (model == null) return null;
        Vector3 b = model.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.size;
        float scale = worldSize / Mathf.Max(b.x, b.y, b.z);
        GameObject rock = Place(parent, ForestRoot + piece, position, Random.Range(0f, 360f), scale);
        if (rock != null && collider) BoxFromMesh(rock);
        return rock;
    }

    private static void BoxFromMesh(GameObject go)
    {
        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        Bounds b = mf.sharedMesh.bounds;
        BoxCollider box = go.AddComponent<BoxCollider>();
        if (mf.gameObject != go) { box.center = go.transform.InverseTransformPoint(mf.transform.TransformPoint(b.center)); box.size = Vector3.Scale(b.size, mf.transform.localScale); }
        else { box.center = b.center; box.size = b.size; }
    }

    private static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat, bool meshCollider)
    {
        GameObject g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
        g.transform.position = pos; g.transform.eulerAngles = euler; g.transform.localScale = scale;
        if (mat != null) g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (meshCollider && type == PrimitiveType.Cylinder) { Object.DestroyImmediate(g.GetComponent<CapsuleCollider>()); g.AddComponent<MeshCollider>(); }
        return g;
    }

    // dalle plate sans collider (sol de place, clairière)
    private static GameObject Flat(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject g = Prim(parent, name, type, pos + Vector3.up * scale.y, Vector3.zero, scale, mat, false);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    // bande de terre plate entre deux points (sans collider)
    private static GameObject Strip(Transform parent, string name, Vector3 a, Vector3 b, float width, Material mat)
    {
        Vector3 d = b - a; d.y = 0f;
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
        g.transform.position = (a + b) / 2f + Vector3.up * 0.02f;
        g.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        g.transform.localScale = new Vector3(width, 0.04f, d.magnitude + 0.6f);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        if (mat != null) g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    private static float YawToward(Vector3 from, Vector3 to) { Vector3 d = to - from; d.y = 0f; return d.sqrMagnitude < 0.001f ? 0f : Quaternion.LookRotation(d, Vector3.up).eulerAngles.y; }

    private static void Label(Transform parent, string name, string text, Vector3 pos, float size, Material mat)
    {
        GameObject g = new GameObject("Label_" + name); g.transform.SetParent(parent, false);
        g.transform.position = pos; g.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        TextMesh tm = g.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.text = text; tm.font = font; tm.fontSize = 48; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.black; tm.fontStyle = FontStyle.Bold;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : font.material;
    }

    private static void HideLabel(GameObject go) { foreach (Transform c in go.transform) if (c.name == "Label") c.gameObject.SetActive(false); }

    private static Transform Group(Transform parent, string name) { Transform t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    private static Transform Find(string path) { GameObject g = GameObject.Find(path); return g != null ? g.transform : null; }
    private static void Kill(Transform t) { if (t != null) Object.DestroyImmediate(t.gameObject); }
    private static void MarkStatic(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
    }
}
