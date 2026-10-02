using System.Collections.Generic;
using System.Text;
using Deathless.Jeu;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// ATTENTION (02/10/2026) : la carte v5 vit dans sa propre scène, Assets/Scenes/CarteV5.unity (NavMesh dans Assets/Scenes/CarteV5/,
// sol dans SolVillage_V5.asset / SolVillage_Palette_V5.png / SolVillage_V5.mat). Ce fichier ne doit JAMAIS modifier Village.unity
// ni les assets de l'ancienne carte (solo et multijoueur) : toutes les étapes refusent de s'exécuter hors de CarteV5.unity.
// Carte v5 du village (report dans main du 01/10/2026 ; wiki : village.md, Refonte de la carte). Appliquée à la scène
// ouverte (CarteV5.unity), sans regénérer le reste (Nyxessa, plateau et sa rampe, intérieurs, taverne, cycle) :
//   Deathless > Village > v5 > Tout appliquer (les étapes 1 à 7, puis le NavMesh), ou une étape à la fois.
// Cotes : plan serpentin, Docs/outils/plan_village.py --serpente (x vers l'est, y vers le nord, en m ; Unity (x, 0, y)).
// Montagne : pièce héros Tripo préparée par ArtSources/Decor/Montagne/montagne_pipeline.py (Assets/Art/Decor/Montagne/).
// Les maisons gardent leur modèle et leur intérieur : chacune est déplacée (avec son intérieur et sa lanterne) au centre
// que lui donne le plan, façade vers Nyxessa. Chaque étape retire ce qu'elle a posé avant de le refaire.
public static partial class VillageBuilder
{
    /// Vrai : le sol, les sentiers et la vérification suivent la carte v5 (rivière, lande, routes est / sud / ouest).
    public static bool V5Actif = true;

    // ------------------------------------------------------------------ cotes du plan
    /// Maisons : objet sous Maisons -> centre au plan (habitant entre parenthèses).
    public static readonly string[] V5MaisonsNoms = { "Maison_1_A", "Maison_2_B", "Maison_3_A", "Maison_4_B", "Maison_5_A", "Maison_6_B" };
    public static readonly string[] V5MaisonsRoles = { "Taverne", "Mecano", "Maison", "Forge", "Sorcier", "Druide" };
    public static readonly Vector2[] V5MaisonsCentres = { new Vector2(-31f, -16f), new Vector2(27f, -17f), new Vector2(-37f, 10f), new Vector2(32f, 13f), new Vector2(-27f, 28f), new Vector2(21f, 28f) };

    /// Bassin au pied de la cascade : recalé d'un mètre vers l'ouest et le nord sur la ravine de la pièce Tripo (plan : (0, 38)).
    public static readonly Vector2 V5Bassin = new Vector2(-0.8f, 37.6f);
    public const float V5BassinRayon = 4.4f;
    /// Axe de la rivière (plan serpentin), du bassin vers le sud-ouest, prolongé hors de la carte (le plan s'arrête à 80 m).
    public static readonly Vector2[] V5RiviereTrace = {
        new Vector2(-0.8f, 37.6f),
        new Vector2(0f, 36f), new Vector2(4f, 32f), new Vector2(8f, 27f), new Vector2(10.5f, 22f), new Vector2(15f, 17.5f), new Vector2(18f, 12f), new Vector2(15f, 6.5f), new Vector2(13f, 1f), new Vector2(14.5f, -4.5f), new Vector2(17f, -9f), new Vector2(15f, -14f), new Vector2(10f, -18f), new Vector2(4f, -21.5f), new Vector2(-4f, -21f), new Vector2(-5.5f, -22.6f), new Vector2(-6.6f, -24.6f), new Vector2(-7.7f, -26.7f), new Vector2(-8.9f, -28.6f), new Vector2(-10.5f, -30.1f), new Vector2(-12.6f, -30.8f), new Vector2(-15.4f, -30.9f), new Vector2(-18.6f, -30.4f), new Vector2(-21.9f, -29.7f), new Vector2(-24.5f, -29.9f), new Vector2(-26.4f, -31f), new Vector2(-27.6f, -33f), new Vector2(-28f, -35.8f), new Vector2(-28f, -39.2f), new Vector2(-27.9f, -42.7f), new Vector2(-28f, -46f), new Vector2(-28.6f, -48.7f), new Vector2(-29.9f, -50.5f), new Vector2(-31.9f, -51.4f), new Vector2(-34.7f, -51.5f), new Vector2(-37.9f, -50.9f), new Vector2(-41.4f, -50.1f), new Vector2(-44.7f, -49.4f), new Vector2(-47.6f, -49.3f), new Vector2(-49.8f, -50f), new Vector2(-51.2f, -51.7f), new Vector2(-51.9f, -54.2f), new Vector2(-52.1f, -57.4f), new Vector2(-52f, -60.9f), new Vector2(-51.9f, -64.4f), new Vector2(-52.3f, -67.3f), new Vector2(-53.3f, -69.5f), new Vector2(-55f, -70.8f), new Vector2(-57.5f, -71.1f), new Vector2(-60.6f, -70.8f), new Vector2(-64f, -70f),
        new Vector2(-74f, -68.6f), new Vector2(-87f, -72f), new Vector2(-104f, -77f) };
    public const float V5DemiLargeur = 2.25f;            // rivière de 4,5 m (plan)
    public const float V5NiveauEau = -0.3f;              // surface de l'eau (m)
    public const float V5Lit = -1.0f, V5LitBassin = -1.3f, V5LitGue = -0.42f;   // fond du lit, du bassin, des gués
    public const float V5BergeInt = 1.0f, V5BergeExt = 3.2f;                     // le fond s'arrête à 1 m de l'axe, la berge rejoint le sol à 3,2 m (pente ≤ 34°)
    /// Gués : le point de la rivière le plus proche du druide, de la forge et du mécano (plan --serpente).
    public static readonly Vector2[] V5Gues = { new Vector2(12.75f, 19.75f), new Vector2(18f, 12f), new Vector2(15.6f, -12.5f) };
    public const float V5GueRayon = 3.0f;
    /// Ponts : là où la rivière coupe la route est (y = 0) et la route sud (x = 0) ; lacet Unity de l'axe du tablier.
    public static readonly Vector2[] V5Ponts = { new Vector2(13.27f, 0f), new Vector2(0f, -21.25f) };
    public static readonly float[] V5PontsLacet = { 90f, 0f };
    public const float V5PontLongueur = 8.6f, V5PontLargeur = 3f, V5PontHaut = 0.3f;
    /// Routes d'attaque de 7 m en terre battue, de l'anneau aux clairières (est, sud, ouest) ; clairières de 7 m de rayon.
    public const float V5DemiRoute = 3.5f;
    /// Montagne : pièce héros posée au nord, face au village ; la grotte à l'ouest de la ravine de la cascade.
    public const string V5MontagneFbx = "Assets/Art/Decor/Montagne/Montagne_Heros.fbx";
    public const string V5MontagneTex = "Assets/Art/Decor/Montagne/Montagne_Heros_Texture.png";
    public const string V5MontagneMat = "Assets/Art/Decor/Montagne/Montagne_Heros.mat";
    public static readonly Vector3 V5MontagnePos = new Vector3(1f, 0f, 50f);
    public const float V5MontagneLacet = 180f;           // la face avant de la pièce (Blender -Y) regarde le sud
    /// Grotte (repère de la pièce, m) : entrée et fond, sol ; le portail au fond, face au village.
    public static readonly Vector3 V5GrotteEntreeLocal = new Vector3(-14.5f, 0f, -8f);   // seuil (relevé au rayon dans Unity)
    public static readonly Vector3 V5GrotteFondLocal = new Vector3(-14.5f, 0f, -3.6f);
    public const float V5GrotteSol = 2.15f, V5GrotteMarche = 4.6f;   // replat du fond à 2,15 m, escalier de 4,6 m
    public const int V5GrotteMarches = 10;               // marches régulières (21,5 cm de haut, 46 cm de profondeur)
    public const float V5GrotteLargeur = 4.6f;           // escalier et dalle : même largeur, tirés sur l'axe de l'entrée
    public const float V5PortailRayon = 2.56f;           // portail du village : 1,6 m x 1,6 (retour du 02/10/2026)
    public const float V5PortailGarde = 0.1f;            // bas du disque au-dessus de la dalle
    public const float V5RouteDroite = 10.4f;            // route de la grotte : dernier tronçon droit, dans l'axe de l'escalier
    /// Cascade (repère de la pièce) : lèvre en haut de la ravine, coude sur l'éboulis, pied dans le bassin.
    public static readonly Vector3 V5CascadeLevreLocal = new Vector3(-0.8f, 14f, -2.2f);
    public static readonly Vector3 V5CascadeCoudeLocal = new Vector3(-0.8f, 3.4f, -4.4f);
    /// Pièces de flanc (Tripo, montagne_flancs_pipeline.py, 02/10/2026) : les deux modèles de Quentin TELS QUELS (pièce entière,
    /// sans miroir, sans recadrage, échelle uniforme de 62 m par unité Tripo posée dans le FBX), même traitement que la pièce
    /// héros (maillage rendu + maillage de collision, jamais marchable), posées seulement si leur FBX existe. Pose = centre de
    /// l'emprise au sol (x, y, z) et lacet Unity ; lacet 180 comme la pièce héros (face au sud, x non inversé : l'ouest a son
    /// point haut et son rocher en surplomb à l'ouest, l'est ses deux pics à l'est). Enfoncées de V5FlancsEnfoncement m.
    /// Une seule grotte, celle de la pièce héros.
    public static readonly string[] V5FlancsFbx = { "Assets/Art/Decor/Montagne/Montagne_Flanc_Est.fbx", "Assets/Art/Decor/Montagne/Montagne_Flanc_Ouest.fbx" };
    public static readonly string[] V5FlancsTex = { "Assets/Art/Decor/Montagne/Montagne_Flanc_Est_Texture.png", "Assets/Art/Decor/Montagne/Montagne_Flanc_Ouest_Texture.png" };
    public static readonly string[] V5FlancsMat = { "Assets/Art/Decor/Montagne/Montagne_Flanc_Est.mat", "Assets/Art/Decor/Montagne/Montagne_Flanc_Ouest.mat" };
    public static readonly Vector4[] V5FlancsPose = { new Vector4(69.5f, 0f, 43f, 180f), new Vector4(-69.5f, 0f, 40f, 180f) };   // x, -, z, lacet
    public const float V5FlancsEnfoncement = 0.4f;
    /// Falaise procédurale en gradins (18, 28, 38 m) : bande continue derrière la pièce héros et sur les flancs.
    public static readonly float[] V5GradinsHaut = { 18f, 28f, 38f };

    // ------------------------------------------------------------------ géométrie de la rivière
    static List<Vector2> s_V5Axe;
    /// Axe lissé (Catmull-Rom), un point tous les 0,5 m environ.
    public static List<Vector2> V5Axe()
    {
        if (s_V5Axe != null) return s_V5Axe;
        var p = V5RiviereTrace; var r = new List<Vector2>();
        for (int i = 0; i + 1 < p.Length; i++)
        {
            Vector2 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
            int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(p1, p2) / 0.5f));
            for (int k = 0; k < n; k++)
            {
                float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                r.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        r.Add(p[p.Length - 1]);
        s_V5Axe = r;
        return r;
    }

    /// Distance (plan horizontal) à l'axe de la rivière ; `i` : index du segment le plus proche.
    public static float V5DistanceAxe(float x, float z, out int i)
    {
        var a = V5Axe(); Vector2 q = new Vector2(x, z); float best = float.MaxValue; i = 0;
        for (int k = 1; k < a.Count; k++)
        {
            Vector2 s = a[k - 1], ab = a[k] - s;
            float t = Mathf.Clamp01(Vector2.Dot(q - s, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            float d = (q - (s + ab * t)).sqrMagnitude;
            if (d < best) { best = d; i = k; }
        }
        return Mathf.Sqrt(best);
    }

    /// Distance « d'eau » : à l'axe, ou au bord du bassin ramené à une rivière (0 au centre du bassin).
    public static float V5DistanceEau(float x, float z)
    {
        float d = V5DistanceAxe(x, z, out _);
        float b = Mathf.Max(0f, Vector2.Distance(new Vector2(x, z), V5Bassin) - (V5BassinRayon - V5DemiLargeur));
        return Mathf.Min(d, b);
    }

    /// Vrai si `p` est sur l'eau ou sur la berge mouillée (à `marge` m près du bord de l'eau).
    public static bool V5SurEau(Vector3 p, float marge) { return V5DistanceEau(p.x, p.z) < V5DemiLargeur + 0.2f + marge; }

    static float V5Gue(float x, float z)
    {
        float best = 99f;
        foreach (var g in V5Gues) best = Mathf.Min(best, Vector2.Distance(new Vector2(x, z), g));
        return best;
    }

    /// Creusement du sol (≤ 0) : lit de la rivière et du bassin, relevé aux gués.
    public static float V5Creux(float x, float z)
    {
        if (!V5Actif) return 0f;
        float dr = V5DistanceAxe(x, z, out _);
        float db = Vector2.Distance(new Vector2(x, z), V5Bassin);
        float h = 0f;
        if (dr < V5BergeExt)
        {
            float lit = Mathf.Lerp(V5Lit, V5LitGue, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(V5GueRayon, V5GueRayon + 1.6f, V5Gue(x, z))));
            h = Mathf.Min(h, Mathf.Lerp(lit, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(V5BergeInt, V5BergeExt, dr))));
        }
        float bInt = V5BassinRayon - V5DemiLargeur + V5BergeInt, bExt = V5BassinRayon - V5DemiLargeur + V5BergeExt;
        if (db < bExt) h = Mathf.Min(h, Mathf.Lerp(V5LitBassin, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bInt, bExt, db))));
        return h;
    }

    // ------------------------------------------------------------------ routes, lande, emprises
    /// Distance latérale à l'axe de la route d'attaque la plus proche (est, sud, ouest), clairières comprises (0 dedans).
    public static float V5DistanceRoute(Vector2 m)
    {
        float best = 99f;
        for (int g = 0; g < 3; g++)
        {
            Vector3 c = ClearingCenter(g);
            Vector2 cc = new Vector2(c.x, c.z), dir = cc.normalized;
            float along = Vector2.Dot(m, dir);
            if (along > PaversRadius - 0.5f && along < cc.magnitude) best = Mathf.Min(best, Mathf.Abs(dir.x * m.y - dir.y * m.x));
            best = Mathf.Min(best, Mathf.Max(0f, Vector2.Distance(m, cc) - ClearingRadius + V5DemiRoute));
        }
        return best;
    }

    static readonly Vector2[] s_V5Lande = { new Vector2(16f, 40f), new Vector2(100f, 40f), new Vector2(100f, -36f), new Vector2(40f, -36f), new Vector2(18f, -8f), new Vector2(18f, 8f) };
    /// Lande sableuse de l'est (plan) : 1 dedans, 0 dehors, transition de 5 m bruitée.
    public static float V5Lande(Vector2 m)
    {
        bool dedans = false; float d = 99f;
        for (int i = 0, j = s_V5Lande.Length - 1; i < s_V5Lande.Length; j = i++)
        {
            Vector2 a = s_V5Lande[j], b = s_V5Lande[i];
            if ((a.y > m.y) != (b.y > m.y) && m.x < a.x + (m.y - a.y) * (b.x - a.x) / (b.y - a.y)) dedans = !dedans;
            Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(m - a, ab) / ab.sqrMagnitude);
            d = Mathf.Min(d, Vector2.Distance(m, a + ab * t));
        }
        float s = (dedans ? d : -d) + (Mathf.PerlinNoise(m.x * 0.09f + 4f, m.y * 0.09f + 9f) - 0.5f) * 7f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-2.5f, 2.5f, s));
    }

    /// Allées pavées (segments a, b, demi-largeur), remplies par l'étape des allées : herbe et rochers les évitent.
    static readonly List<Vector4> s_V5Allees = new List<Vector4>();
    static readonly List<float> s_V5AlleesLarg = new List<float>();
    static bool V5PresAllee(Vector2 m, float marge)
    {
        for (int i = 0; i < s_V5Allees.Count; i++)
        {
            Vector4 s = s_V5Allees[i]; Vector2 a = new Vector2(s.x, s.y), ab = new Vector2(s.z, s.w) - a;
            float t = Mathf.Clamp01(Vector2.Dot(m - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            if (Vector2.Distance(m, a + ab * t) < s_V5AlleesLarg[i] + marge) return true;
        }
        return false;
    }

    static float V5DistanceMaisons(Vector2 m)
    {
        float best = 99f;
        foreach (Vector4[] f in HouseFootprints) best = Mathf.Min(best, FootprintDistance(f, m));
        return best;
    }

    /// Teinte du sol v5 (index de GroundPalette) pour une facette de centre `m` à la hauteur moyenne `y`.
    static int V5Teinte(Vector2 m, float y, int idx, float r1, float r2)
    {
        if (y < -0.38f) return r2 < 0.3f ? 5 : 8;                        // lit : galets sous l'eau
        if (y < -0.1f) return r2 < 0.55f ? 11 : 6;                        // berge mouillée
        float route = V5DistanceRoute(m);
        if (route < V5DemiRoute + (r1 - 0.5f) * 1.3f) return r2 < 0.3f ? 7 : 6;
        float lande = V5Lande(m);
        if (lande > 0.5f + (r1 - 0.5f) * 0.3f && idx != 6 && idx != 7) return r2 < 0.12f ? 2 : r2 < 0.5f ? 10 : 9;
        return idx;
    }

    // ------------------------------------------------------------------ scène et assets propres à la carte v5
    public const string V5ScenePath = "Assets/Scenes/CarteV5.unity";
    public const string V5SolMesh = "Assets/Art/Meshes/SolVillage_V5.asset";
    public const string V5SolTex = "Assets/Art/Textures/SolVillage_Palette_V5.png";
    public const string V5SolMat = "Assets/Art/Materials/SolVillage_V5.mat";

    static bool V5SceneOk() { return EditorSceneManager.GetActiveScene().path == V5ScenePath; }

    [MenuItem("Deathless/Village/v5/Ouvrir CarteV5.unity")]
    public static void V5Ouvrir() { EditorSceneManager.OpenScene(V5ScenePath, OpenSceneMode.Single); }

    // ------------------------------------------------------------------ menus
    [MenuItem("Deathless/Village/v5/Tout appliquer")]
    public static string V5ToutAppliquer()
    {
        if (!V5SceneOk()) return "Refusé : ouvrir " + V5ScenePath + " (la carte v5 ne touche jamais Village.unity)";
        var sb = new StringBuilder("Carte v5 :\n");
        Transform root = Find("VillageBlockout");
        if (root == null) return "VillageBlockout introuvable";
        s_V5Axe = null;
        sb.AppendLine(V5Maisons(root));
        sb.AppendLine(V5Montagne(root));
        sb.AppendLine(V5Riviere(root));
        sb.AppendLine(V5Allees(root));
        sb.AppendLine(V5Clairieres(root));
        sb.AppendLine(V5Nature(root));
        sb.AppendLine(V5Sol(root));
        sb.AppendLine(SentiersBuilder.Generer());   // sentiers de pierre vers les clairières, NavMesh cuit, scène enregistrée
        string r = sb.ToString();
        Debug.Log(r);
        return r;
    }

    [MenuItem("Deathless/Village/v5/1. Maisons et intérieurs")] public static string V5MenuMaisons() { return Etape(V5Maisons); }
    [MenuItem("Deathless/Village/v5/2. Montagne, grotte et portail")] public static string V5MenuMontagne() { return Etape(V5Montagne); }
    [MenuItem("Deathless/Village/v5/3. Rivière, bassin, cascade, gués, ponts")] public static string V5MenuRiviere() { return Etape(V5Riviere); }
    [MenuItem("Deathless/Village/v5/4. Allées et route de la grotte")] public static string V5MenuAllees() { return Etape(V5Allees); }
    [MenuItem("Deathless/Village/v5/5. Clairières (est, sud, ouest)")] public static string V5MenuClairieres() { return Etape(V5Clairieres); }
    [MenuItem("Deathless/Village/v5/6. Forêt, lande, pierrier, herbe")] public static string V5MenuNature() { return Etape(V5Nature); }
    [MenuItem("Deathless/Village/v5/7. Sol")] public static string V5MenuSol() { return Etape(V5Sol); }
    [MenuItem("Deathless/Village/v5/Vérifier")] public static string V5MenuVerifier() { string r = V5Verifier(); Debug.Log(r); return r; }

    static string Etape(System.Func<Transform, string> f)
    {
        if (!V5SceneOk()) return "Refusé : ouvrir " + V5ScenePath + " (la carte v5 ne touche jamais Village.unity)";
        Transform root = Find("VillageBlockout");
        if (root == null) return "VillageBlockout introuvable";
        s_V5Axe = null;
        string r = f(root);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log(r);
        return r;
    }

    // ------------------------------------------------------------------ 1. maisons
    /// Contrat d'emplacement (02/10/2026 : les bâtiments seront refaits en Grok + Tripo) : un objet racine par bâtiment,
    /// Maisons/Batiment_<Rôle>, pivot au sol (y = 0) au centre de l'emprise, axe avant (+Z) tourné vers Nyxessa ; dessous,
    /// le modèle actuel (Maison_<n>_<A|B>, nom gardé : Sorcier, intérieurs) et la zone de porte « Porte » (déclencheur
    /// sur la couche Ignore Raycast, axe avant vers l'extérieur), séparée du modèle. Remplacer un bâtiment = remplacer
    /// l'enfant modèle et recaler « Porte ». L'intérieur (Interieurs/Interieur_<Rôle>) et la lanterne de porte suivent.
    public static string V5Maisons(Transform root)
    {
        var sb = new StringBuilder("Maisons : ");
        Transform ms = root.Find("Maisons"), its = root.Find("Interieurs"), lan = root.Find("Ambiance/Lanternes");
        if (ms == null) return "Maisons absentes";
        for (int i = 0; i < V5MaisonsNoms.Length; i++)
        {
            Transform h = TrouverMaison(root, V5MaisonsNoms[i]);
            if (h == null) { sb.Append(V5MaisonsNoms[i] + " absente ; "); continue; }
            // ancienne racine retirée (le modèle revient sous Maisons, pose monde gardée)
            if (h.parent != ms) { Transform vieille = h.parent; h.SetParent(ms, true); if (vieille != null && vieille.name.StartsWith("Batiment_")) Object.DestroyImmediate(vieille.gameObject); }
            Vector3 p0 = h.position; Quaternion q0 = h.rotation;
            Vector3 flat0 = new Vector3(p0.x, 0f, p0.z);
            float ecart = Mathf.DeltaAngle(YawToward(flat0, Vector3.zero), q0.eulerAngles.y);
            Vector3 p1 = new Vector3(V5MaisonsCentres[i].x, p0.y, V5MaisonsCentres[i].y);
            Quaternion q1 = Quaternion.Euler(0f, YawToward(new Vector3(p1.x, 0f, p1.z), Vector3.zero) + ecart, 0f);
            Quaternion dq = q1 * Quaternion.Inverse(q0);
            var lies = new List<Transform> { h };
            int k = System.Array.IndexOf(InterieursBuilder.Maisons, h.name);
            if (k >= 0 && its != null) { Transform it = its.Find("Interieur_" + InterieursBuilder.Noms[k]); if (it != null) lies.Add(it); }
            if (lan != null) { Transform l = lan.Find("Lanterne_" + h.name); if (l != null) lies.Add(l); }
            foreach (Transform t in lies)
            {
                Vector3 p = t.position; Quaternion q = t.rotation;
                t.SetPositionAndRotation(p1 + dq * (p - p0), dq * q);
            }
            // racine : centre de l'emprise au sol, avant vers Nyxessa
            var mf = h.GetComponent<MeshFilter>();
            Vector4[] f = mf != null ? Footprint(h, mf.sharedMesh) : null;
            Vector3 centre = f != null ? new Vector3(f[0].x, 0f, f[0].y) : new Vector3(p1.x, 0f, p1.z);
            Transform racine = new GameObject("Batiment_" + V5MaisonsRoles[i]).transform;
            racine.SetParent(ms, false);
            racine.SetPositionAndRotation(centre, Quaternion.Euler(0f, YawToward(centre, Vector3.zero), 0f));
            racine.SetSiblingIndex(i);
            h.SetParent(racine, true);
            // zone de porte : devant le seuil (porte de home_A au milieu, de home_B décalée), 2 × 2,6 × 1,6 m
            int type = h.name.EndsWith("_A") ? 0 : 1;
            Vector3 avant = h.forward; avant.y = 0f; avant.Normalize(); Vector3 droite = Vector3.Cross(Vector3.up, avant);
            float bord = mf != null ? mf.sharedMesh.bounds.max.z * h.lossyScale.z : 3.5f;
            Vector3 porte = new Vector3(h.position.x, 0f, h.position.z) + droite * HouseDoorX[type] + avant * (bord + 0.8f);
            GameObject zp = new GameObject("Porte"); zp.layer = 2;
            zp.transform.SetParent(racine, false);
            zp.transform.SetPositionAndRotation(porte, Quaternion.LookRotation(avant));
            var bc = zp.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(2f, 2.6f, 1.6f); bc.center = new Vector3(0f, 1.3f, 0f);
            sb.Append(V5MaisonsRoles[i] + " (" + h.name + ", " + (lies.Count - 1) + " lié(s)) ; ");
        }
        Physics.SyncTransforms();
        V5EmprisesMaisons(root);
        return sb.ToString();
    }

    /// Modèle d'une maison (Maison_<n>_<A|B>) où qu'il soit sous Maisons (directement en v4, sous Batiment_* en v5).
    public static Transform TrouverMaison(Transform village, string nom)
    {
        Transform ms = village != null ? village.Find("Maisons") : null;
        if (ms == null) return null;
        foreach (Transform t in ms.GetComponentsInChildren<Transform>(true)) if (t.name == nom) return t;
        return null;
    }

    static void V5EmprisesMaisons(Transform root)
    {
        HouseFootprints.Clear();
        Transform ms = root.Find("Maisons");
        if (ms == null) return;
        foreach (MeshFilter mf in ms.GetComponentsInChildren<MeshFilter>()) if (mf.name.StartsWith("Maison_") && mf.sharedMesh != null) HouseFootprints.Add(Footprint(mf.transform, mf.sharedMesh));
    }

    // ------------------------------------------------------------------ 2. montagne, grotte, portail
    public static Transform V5MontagneRacine(Transform root) { return root.Find("Montagne"); }
    public static Vector3 V5Local(Vector3 local)
    {
        // repères « locaux » = coordonnées Blender de la pièce (x, hauteur, y) : avec la pièce tournée de 180°, la face
        // avant (Blender -Y) regarde le sud et x n'est pas inversé (l'import FBX retourne déjà l'axe x)
        return V5MontagnePos + Quaternion.Euler(0f, V5MontagneLacet - 180f, 0f) * local;
    }

    public static string V5Montagne(Transform root)
    {
        Kill(root.Find("Montagne"));
        Transform mont = Group(root, "Montagne");
        int pieces = 0; int trianglesRendu = 0;
        // une pièce = un maillage rendu + un maillage de collision (deux objets du FBX), posés sur un seul objet
        System.Action<string, string, Material, Vector3, float> poser = (fbx, nom, materiau, pos, lacet) =>
        {
            GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (modele == null) return;
            Mesh rendu = null, collision = null; Quaternion axeFbx = Quaternion.identity;
            foreach (var mf in modele.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh.name.Contains("Collision")) collision = mf.sharedMesh; else { rendu = mf.sharedMesh; axeFbx = mf.transform.localRotation; }
            }
            if (rendu == null) return;
            GameObject go = new GameObject(nom);
            go.transform.SetParent(mont, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, lacet, 0f) * axeFbx);   // axe : Z (Blender) vers le haut
            go.transform.localScale = Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = rendu;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = materiau;
            if (collision != null) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = collision; }
            // jamais marchable : la crête est infranchissable par la géométrie et le NavMesh (pas de mur invisible)
            var mod = go.AddComponent<NavMeshModifier>(); mod.overrideArea = true; mod.area = 1;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            pieces++; trianglesRendu += rendu.triangles.Length / 3;
        };
        if (AssetDatabase.LoadAssetAtPath<GameObject>(V5MontagneFbx) == null) return "Montagne : FBX absent (" + V5MontagneFbx + "), lancer montagne_pipeline.py";
        poser(V5MontagneFbx, "Montagne_Heros", V5MateriauMontagne(V5MontagneMat, V5MontagneTex), V5MontagnePos, V5MontagneLacet);
        // flancs (montagne_flancs_pipeline.py) : même traitement que la pièce héros, posés à V5FlancsPose (centre de l'emprise, au sol)
        for (int i = 0; i < V5FlancsFbx.Length; i++)
            poser(V5FlancsFbx[i], i == 0 ? "Flanc_Est" : "Flanc_Ouest", V5MateriauMontagne(V5FlancsMat[i], V5FlancsTex[i]),
                new Vector3(V5FlancsPose[i].x, -V5FlancsEnfoncement, V5FlancsPose[i].z), V5FlancsPose[i].w);
        V5Falaise(mont);

        // Grotte (retours de Quentin du 02/10/2026) : tout est tiré au cordeau sur l'axe de l'entrée (le +z du repère de la
        // pièce, donc du monde) : la route pavée (V5Allees), l'escalier (marches régulières, parapets symétriques), la dalle et
        // le portail. Le fond de la pièce Tripo est un replat à ~2,1 m derrière un seuil rocheux, avec des vides sur les côtés
        // du seuil : la dalle repose sur un socle maçonné qui descend au sol (aucun angle de dalle en l'air).
        Vector3 entree = V5Local(V5GrotteEntreeLocal), fond = V5Local(V5GrotteFondLocal);
        Transform grotte = Group(mont, "Grotte");
        Material pierre = FlatMaterial("Assets/Art/Materials/Grotte_Pierre.mat", "Universal Render Pipeline/Lit", new Color(0.5f, 0.53f, 0.58f));
        pierre.SetFloat("_Smoothness", 0.05f);
        {
            Vector3 axe = fond - entree; axe.y = 0f; Vector3 dir = axe.normalized;
            float lon = axe.magnitude + 1.6f, larg = V5GrotteLargeur;
            Vector3 centreSol = entree + axe / 2f + dir * 0.5f;
            GameObject sol = new GameObject("Grotte_Sol"); sol.transform.SetParent(grotte, false);
            sol.transform.SetPositionAndRotation(centreSol + Vector3.up * (V5GrotteSol - 0.2f), Quaternion.LookRotation(dir));
            var bs = sol.AddComponent<BoxCollider>(); bs.size = new Vector3(larg, 0.4f, lon);
            // socle maçonné : du sol (un peu enterré) jusque sous les dalles (dessous des dalles à V5GrotteSol - 0,1)
            GameObject socle = GameObject.CreatePrimitive(PrimitiveType.Cube); socle.name = "Grotte_Socle"; socle.transform.SetParent(grotte, false);
            float s0 = -0.3f, s1 = V5GrotteSol - 0.1f;
            socle.transform.SetPositionAndRotation(centreSol + Vector3.up * ((s0 + s1) / 2f), Quaternion.LookRotation(dir));
            socle.transform.localScale = new Vector3(larg, s1 - s0, lon);
            socle.GetComponent<MeshRenderer>().sharedMaterial = pierre;
            // dalles : deux colonnes de carreaux de 2 m, de part et d'autre de l'axe
            Vector3 dr = Vector3.Cross(Vector3.up, dir);
            for (float a = 0.6f; a <= axe.magnitude + 1.01f; a += 2f)
                for (float t = -1f; t <= 1.01f; t += 2f)
                    Dungeon(grotte, Pavers[Random.Range(0, Pavers.Length)], entree + dir * a + dr * t + Vector3.up * (V5GrotteSol + TileY), Quaternion.LookRotation(dir).eulerAngles.y + 90f * Random.Range(0, 4));
            // escalier droit : V5GrotteMarches marches régulières de largeur V5GrotteLargeur (parapets de 0,4 m de chaque côté,
            // symétriques), du sol (V5GrotteMarche m devant le seuil) jusqu'au replat ; chaque marche est un bloc plein
            Vector3 bas = entree - dir * V5GrotteMarche; bas.y = 0f;
            Transform esc = Group(grotte, "Grotte_Escalier");
            int n = V5GrotteMarches; float prof = V5GrotteMarche / n, haut = V5GrotteSol / n, parapet = 0.4f;
            for (int i = 1; i <= n; i++)
            {
                Vector3 c = bas + dir * ((i - 0.5f) * prof);
                GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cube); m.name = "Marche_" + i; m.transform.SetParent(esc, false);
                m.transform.SetPositionAndRotation(c + Vector3.up * (i * haut / 2f), Quaternion.LookRotation(dir));
                m.transform.localScale = new Vector3(larg - 2f * parapet, i * haut, prof);
                m.GetComponent<MeshRenderer>().sharedMaterial = pierre; Object.DestroyImmediate(m.GetComponent<Collider>());
                foreach (float cote in new[] { -1f, 1f })
                {
                    GameObject pr = GameObject.CreatePrimitive(PrimitiveType.Cube); pr.name = "Parapet_" + (cote < 0f ? "G" : "D") + "_" + i; pr.transform.SetParent(esc, false);
                    float hp = i * haut + 0.55f;
                    pr.transform.SetPositionAndRotation(c + dr * (cote * (larg / 2f - parapet / 2f)) + Vector3.up * (hp / 2f), Quaternion.LookRotation(dir));
                    pr.transform.localScale = new Vector3(parapet, hp, prof);
                    pr.GetComponent<MeshRenderer>().sharedMaterial = pierre;
                }
            }
            // rampe invisible pour marcher : légèrement au-dessus de la pente des marches (pieds sur les nez de marche)
            GameObject rampe = new GameObject("Grotte_Rampe"); rampe.transform.SetParent(grotte, false);
            float lr = Mathf.Sqrt(V5GrotteMarche * V5GrotteMarche + V5GrotteSol * V5GrotteSol), ang = Mathf.Atan2(V5GrotteSol, V5GrotteMarche) * Mathf.Rad2Deg;
            rampe.transform.SetPositionAndRotation((bas + new Vector3(entree.x, V5GrotteSol, entree.z)) / 2f + Vector3.up * (haut * 0.5f - 0.1f), Quaternion.LookRotation(dir) * Quaternion.Euler(-ang, 0f, 0f));
            var br = rampe.AddComponent<BoxCollider>(); br.size = new Vector3(larg - 2f * parapet - 0.1f, 0.2f, lr + 0.1f);
        }
        // Portail : disque de gemmes (objet d'origine, déplacé), face à la route, agrandi (V5PortailRayon) pour remplir
        // l'entrée de la grotte ; la stèle (socle rond en pierre) est supprimée. Interaction (GameBalance.distancePortail, 3 m,
        // horizontale, et 3 m en hauteur entre les pieds et le centre) et collision : le cube racine suit la taille du disque.
        Transform por = root.Find("Portail");
        string portail = "portail absent";
        if (por != null)
        {
            Kill(por.Find("Portail_Place")); Kill(por.Find("Portail_Chemin")); Kill(por.Find("Portail_Socle"));
            Transform disque = por.Find("PortailDonjon");
            if (disque != null)
            {
                Vector3 pp = fond; pp.y = 0f;
                float ray = V5PortailRayon;
                disque.position = pp + Vector3.up * (V5GrotteSol + V5PortailGarde + ray);
                disque.rotation = Quaternion.Euler(0f, 90f, 0f);            // face fine du cube racine vers le sud : le disque regarde la route
                disque.localScale = new Vector3(0.3f, 3f, 2.5f) * (ray / 1.6f);
                var pv = disque.GetComponent<PortalVisual>();
                if (pv != null)
                {
                    var so = new SerializedObject(pv);
                    so.FindProperty("radius").floatValue = ray;
                    so.FindProperty("cell").floatValue = 0.1f * Mathf.Sqrt(ray / 1.6f);
                    so.FindProperty("particleCount").intValue = Mathf.RoundToInt(1700f * (ray / 1.6f));
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                var lum = disque.GetComponentInChildren<Light>(true);
                if (lum != null) lum.range = 9f * (ray / 1.6f);
                portail = "portail de rayon " + ray.ToString("F2") + " m au fond de la grotte (" + pp.x.ToString("F1") + ", " + pp.z.ToString("F1") + "), centre à " + disque.position.y.ToString("F2") + " m, sans stèle";
            }
        }
        // Lumière de la grotte : la lueur verte vient du portail (PortalVisual) ; rien d'autre ici.
        return "Montagne : " + pieces + " pièces (" + trianglesRendu + " triangles rendus au total), grotte " + Vector3.Distance(entree, fond).ToString("F1") + " m ; " + portail;
    }

    /// Falaise procédurale : champ de hauteur facetté au nord (x de -112 à 112, z de 34 à 126), trois gradins de 18, 28
    /// et 38 m dont le front suit la pièce héros (derrière elle au centre, à z ≈ 44 sur les flancs), fronts et hauteurs
    /// bruités ; fronts raides (infranchissables, sans mur invisible), jamais marchable (NavMesh), collision par le
    /// maillage lui-même. Teintes de roche (palette de 4 gris) ; gros rochers au pied et sur les replats.
    static void V5Falaise(Transform mont)
    {
        const float x0 = -112f, x1 = 112f, z0 = 34f, z1 = 126f, pas = 2.5f;
        System.Func<float, float> front = x =>
        {
            float ax = Mathf.Abs(x);
            float f = Mathf.Lerp(60f, 44f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(46f, 64f, ax)));
            return f + (Mathf.PerlinNoise(x * 0.05f + 3f, 7.3f) - 0.5f) * 6f;
        };
        System.Func<float, float, float> hauteur = (x, z) =>
        {
            float f = front(x), h = 0f;
            for (int k = 0; k < V5GradinsHaut.Length; k++)
            {
                float fk = f + k * 21f + (Mathf.PerlinNoise(x * 0.07f + k * 11f, 2.1f) - 0.5f) * 5f;
                float dh = V5GradinsHaut[k] - (k > 0 ? V5GradinsHaut[k - 1] : 0f);
                h += dh * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fk, fk + 7f + k * 1.5f, z));
            }
            if (h > 0.5f) h += (Mathf.PerlinNoise(x * 0.21f + 40f, z * 0.21f + 9f) - 0.5f) * 2.4f * Mathf.Clamp01(h / 6f);
            return Mathf.Max(-0.3f, h - 0.2f);
        };
        var gris = new[] { new Color(0.42f, 0.41f, 0.42f), new Color(0.5f, 0.49f, 0.5f), new Color(0.58f, 0.57f, 0.58f), new Color(0.35f, 0.34f, 0.36f) };
        Texture2D tex = new Texture2D(16, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < 16; x++) for (int y = 0; y < 4; y++) tex.SetPixel(x, y, gris[x / 4]);
        tex.Apply();
        const string texPath = "Assets/Art/Textures/Falaise_Palette.png", matPath = "Assets/Art/Materials/Falaise.mat";
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), texPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (imp.filterMode != FilterMode.Point || imp.mipmapEnabled) { imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath)); mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(mat);
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        var rnd = new System.Random(6100);
        int nx = Mathf.CeilToInt((x1 - x0) / pas), nz = Mathf.CeilToInt((z1 - z0) / pas);
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float xa = x0 + i * pas, xb = xa + pas, za = z0 + j * pas, zb = za + pas;
                Vector3 a = new Vector3(xa, hauteur(xa, za), za), b = new Vector3(xa, hauteur(xa, zb), zb), c = new Vector3(xb, hauteur(xb, zb), zb), d = new Vector3(xb, hauteur(xb, za), za);
                if (Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y)) < 0.05f) continue;   // seulement là où la falaise sort du sol
                var tris = ((i + j) % 2 == 0) ? new[] { new[] { a, b, c }, new[] { a, c, d } } : new[] { new[] { a, b, d }, new[] { b, c, d } };
                foreach (var tri in tris)
                {
                    Vector3 nn = Vector3.Cross(tri[1] - tri[0], tri[2] - tri[0]).normalized;
                    int k = nn.y < 0.55f ? (rnd.Next(2) == 0 ? 0 : 3) : rnd.Next(1, 3);
                    Vector2 u = new Vector2((k + 0.5f) / 4f, 0.5f);
                    int i0 = v.Count;
                    for (int q = 0; q < 3; q++) { v.Add(tri[q]); n.Add(nn); uv.Add(u); }
                    t.Add(i0); t.Add(i0 + 1); t.Add(i0 + 2);
                }
            }
        const string meshPath = "Assets/Art/Meshes/Village_Falaise.asset";
        Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        bool neuf = m == null; if (neuf) m = new Mesh();
        m.Clear(); m.name = "Village_Falaise"; m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
        if (neuf) AssetDatabase.CreateAsset(m, meshPath); else EditorUtility.SetDirty(m);
        GameObject go = new GameObject("Falaise_Gradins"); go.transform.SetParent(mont, false);
        go.AddComponent<MeshFilter>().sharedMesh = m; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = m;
        var mod = go.AddComponent<NavMeshModifier>(); mod.overrideArea = true; mod.area = 1;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        Transform rochers = Group(mont, "Falaise_Rochers");
        for (int i = 0; i < 70; i++)
        {
            float x = (float)(rnd.NextDouble() * 2 - 1) * 108f;
            int k = rnd.Next(3);
            float z = front(x) + k * 21f + (k == 0 ? -1.5f : 3f) + (float)rnd.NextDouble() * 6f;
            if (Mathf.Abs(x) < 58f && k == 0) continue;       // derrière la pièce héros : rien à voir
            float y = hauteur(x, z);
            PlaceRock(rochers, Rocks[rnd.Next(Rocks.Length)], new Vector3(x, y - 0.4f, z), 3f + (float)rnd.NextDouble() * 5f, false);
        }
        MarkStatic(rochers.gameObject);
    }

    static Material V5MateriauMontagne(string matPath, string texPath)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, matPath); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.05f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ 3. rivière
    public const string V5EauMat = "Assets/Art/Materials/Village_Eau.mat";
    public const string V5CascadeMat = "Assets/Art/Materials/Village_Cascade.mat";

    public static string V5Riviere(Transform root)
    {
        Kill(root.Find("Riviere"));
        Transform riv = Group(root, "Riviere");
        var axe = V5Axe();
        Material eau = V5MateriauRiviere();
        // a. surface : ruban le long de l'axe (5 bandes en travers, un rang tous les ~0,75 m), disque du bassin
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>(); var uv2 = new List<Vector2>();
            float demi = V5DemiLargeur + 0.9f;
            float dist = 0f; Vector2 prec = axe[0];            // le bord passe sous la berge
            int cols = 6, rangs = 0;
            for (int i = 0; i < axe.Count; i += 1)
            {
                if (i % 2 == 1 && i != axe.Count - 1) continue;
                Vector2 a = axe[Mathf.Max(0, i - 1)], b = axe[Mathf.Min(axe.Count - 1, i + 1)];
                Vector2 dir = (b - a).normalized, n = new Vector2(-dir.y, dir.x);
                dist += Vector2.Distance(prec, axe[i]); prec = axe[i];
                // courant plus vif au pied de la cascade (10 premiers mètres) et aux gués
                float vit = 1f + 1.6f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 12f, dist)))
                              + 1.2f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(V5GueRayon - 0.5f, V5GueRayon + 2.5f, V5Gue(axe[i].x, axe[i].y))));
                for (int c = 0; c <= cols; c++)
                {
                    float s = Mathf.Lerp(-demi, demi, c / (float)cols);
                    Vector2 p = axe[i] + n * s;
                    v.Add(new Vector3(p.x, V5NiveauEau, p.y));
                    uv.Add(new Vector2(c / (float)cols, dist)); uv2.Add(new Vector2(vit, 0f));
                }
                rangs++;
            }
            for (int r = 0; r + 1 < rangs; r++)
                for (int c = 0; c < cols; c++)
                {
                    int a0 = r * (cols + 1) + c, a1 = a0 + 1, b0 = a0 + cols + 1, b1 = b0 + 1;
                    t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);
                }
            // bassin : disque à facettes (anneaux)
            int b0i = v.Count; int secteurs = 28; float rb = V5BassinRayon + 0.9f;
            v.Add(new Vector3(V5Bassin.x, V5NiveauEau, V5Bassin.y)); uv.Add(new Vector2(0f, 0f)); uv2.Add(new Vector2(1.8f, 0f));
            for (int ring = 1; ring <= 3; ring++)
                for (int s = 0; s < secteurs; s++)
                {
                    float ang = (s + (ring % 2) * 0.5f) * Mathf.PI * 2f / secteurs, rr = rb * ring / 3f;
                    v.Add(new Vector3(V5Bassin.x + Mathf.Cos(ang) * rr, V5NiveauEau + 0.002f, V5Bassin.y + Mathf.Sin(ang) * rr));
                    uv.Add(new Vector2(s / (float)secteurs, rr)); uv2.Add(new Vector2(1.8f, 0f));   // anneaux qui s'élargissent
                }
            for (int s = 0; s < secteurs; s++) { t.Add(b0i); t.Add(b0i + 1 + (s + 1) % secteurs); t.Add(b0i + 1 + s); }
            for (int ring = 1; ring < 3; ring++)
                for (int s = 0; s < secteurs; s++)
                {
                    int i0 = b0i + 1 + (ring - 1) * secteurs + s, i1 = b0i + 1 + (ring - 1) * secteurs + (s + 1) % secteurs;
                    int o0 = b0i + 1 + ring * secteurs + s, o1 = b0i + 1 + ring * secteurs + (s + 1) % secteurs;
                    t.Add(i0); t.Add(i1); t.Add(o0); t.Add(i1); t.Add(o1); t.Add(o0);
                }
            Mesh m = V5MeshAsset("Assets/Art/Meshes/Village_Riviere.asset", v, t, uv, uv2);
            GameObject go = new GameObject("Eau"); go.transform.SetParent(riv, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = eau; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // b. lit non praticable (NavMesh) : volumes le long de l'axe, sous le niveau des tabliers, sauf aux gués
        Transform lit = Group(riv, "Lit_NonPraticable");
        int volumes = 0;
        for (int i = 0; i + 3 < axe.Count; i += 3)
        {
            Vector2 a = axe[i], b = axe[i + 3], c = (a + b) / 2f;
            if (V5Gue(c.x, c.y) < V5GueRayon + 0.6f) continue;
            if (Mathf.Abs(c.x) > TerrainHalf + 2f || Mathf.Abs(c.y) > TerrainHalf + 2f) continue;
            GameObject go = new GameObject("Lit_" + i); go.transform.SetParent(lit, false);
            Vector2 d = b - a;
            go.transform.SetPositionAndRotation(new Vector3(c.x, -1.05f, c.y), Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
            var vol = go.AddComponent<NavMeshModifierVolume>(); vol.area = 1; vol.center = Vector3.zero; vol.size = new Vector3((V5DemiLargeur + 0.3f) * 2f, 1.9f, d.magnitude + 0.4f);
            volumes++;
        }
        for (int k = 0; k < 4; k++)
        {
            GameObject go = new GameObject("Lit_Bassin_" + k); go.transform.SetParent(lit, false);
            go.transform.SetPositionAndRotation(new Vector3(V5Bassin.x, -1.05f, V5Bassin.y), Quaternion.Euler(0f, 22.5f * k, 0f));
            var vol = go.AddComponent<NavMeshModifierVolume>(); vol.area = 1; vol.size = new Vector3(V5BassinRayon * 2f + 0.6f, 1.9f, V5BassinRayon * 2f * 0.414f + 0.6f);
            var vol2 = go.AddComponent<NavMeshModifierVolume>(); vol2.area = 1; vol2.size = new Vector3(V5BassinRayon * 2f * 0.414f + 0.6f, 1.9f, V5BassinRayon * 2f + 0.6f);
        }
        // c. gués : pierres plates en travers, zone « Eau » (NavMesh, coût 1/0,6) et ZoneEau (ralenti des personnages)
        Transform gues = Group(riv, "Gues");
        int pierres = 0;
        for (int g = 0; g < V5Gues.Length; g++)
        {
            Vector2 c = V5Gues[g];
            V5DistanceAxe(c.x, c.y, out int si);
            Vector2 d = (axe[Mathf.Min(axe.Count - 1, si + 2)] - axe[Mathf.Max(0, si - 2)]).normalized, n = new Vector2(-d.y, d.x);
            Transform gt = Group(gues, "Gue_" + V5MaisonsRoles[new[] { 5, 3, 1 }[g]]);
            gt.position = new Vector3(c.x, 0f, c.y);
            gt.rotation = Quaternion.LookRotation(new Vector3(n.x, 0f, n.y));
            var rnd = new System.Random(5100 + g);
            for (float s = -2.9f; s <= 2.91f; s += 0.95f)
                for (int rang = -1; rang <= 1; rang += 2)
                {
                    float off = rang * (0.55f + (float)rnd.NextDouble() * 0.15f) + ((float)rnd.NextDouble() - 0.5f) * 0.2f;
                    Vector2 p = c + n * (s + rang * 0.25f) + d * off;
                    float y = GroundHeight(p.x, p.y);
                    GameObject st = PlaceRock(gt, Rocks[rnd.Next(Rocks.Length)], new Vector3(p.x, Mathf.Max(y, V5LitGue) - 0.02f, p.y), 0.85f + (float)rnd.NextDouble() * 0.3f, false);
                    if (st == null) continue;
                    // pierre plate : écrasée, dessus juste au-dessus de l'eau
                    Vector3 sc = st.transform.localScale; st.transform.localScale = new Vector3(sc.x, sc.y * 0.3f, sc.z);
                    st.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    var r = st.GetComponentInChildren<Renderer>();
                    if (r != null) { float top = r.bounds.max.y; st.transform.position += Vector3.up * (V5NiveauEau + 0.2f - top); }
                    st.name = "Pierre_Gue";
                    pierres++;
                }
            GameObject zv = new GameObject("Gue_Zone"); zv.transform.SetParent(gt, false);
            zv.transform.localPosition = new Vector3(0f, -0.6f, 0f);
            var vz = zv.AddComponent<NavMeshModifierVolume>(); vz.area = 3; vz.size = new Vector3(V5GueRayon * 2f, 1.6f, V5DemiLargeur * 2f + 2.4f);
            GameObject ze = new GameObject("Gue_Eau"); ze.transform.SetParent(gt, false); ze.layer = 2;
            ze.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var bc = ze.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(V5GueRayon * 1.6f, 0.9f, V5DemiLargeur * 2f + 0.6f);
            ze.AddComponent<Deathless.Donjon.ZoneEau>();
        }
        // d. ponts : modèle KayKit à l'échelle, tablier et rampes en boîtes (marchables), garde-corps
        Transform ponts = Group(riv, "Ponts");
        var piles = new List<Vector3>();
        for (int k = 0; k < V5Ponts.Length; k++) piles.AddRange(V5Pont(ponts, k == 0 ? "Pont_Est" : "Pont_Sud", new Vector3(V5Ponts[k].x, 0f, V5Ponts[k].y), V5PontsLacet[k]));
        // e. berges : pierres le long des deux rives (sans collider), hors gués, ponts, allées et bassin
        Transform berges = Group(riv, "Berges");
        int nb = 0;
        {
            var rnd = new System.Random(5200);
            for (int i = 2; i < axe.Count - 1; i += 4)
            {
                Vector2 a = axe[i - 1], b = axe[i + 1], dd = (b - a).normalized, n = new Vector2(-dd.y, dd.x);
                foreach (int cote in new[] { -1, 1 })
                {
                    if (rnd.NextDouble() < 0.35) continue;
                    Vector2 p = axe[i] + n * cote * (V5DemiLargeur + 0.35f + (float)rnd.NextDouble() * 0.7f) + dd * ((float)rnd.NextDouble() - 0.5f);
                    if (Mathf.Abs(p.x) > TerrainHalf - 1f || Mathf.Abs(p.y) > TerrainHalf - 1f) continue;
                    if (V5Gue(p.x, p.y) < V5GueRayon + 1.5f || V5PresPont(p, 2.5f) || Vector2.Distance(p, V5Bassin) < V5BassinRayon + 0.5f) continue;
                    if (V5DistanceRoute(p) < V5DemiRoute + 0.5f) continue;
                    float taille = 0.45f + (float)rnd.NextDouble() * 0.55f;
                    GameObject st = PlaceRock(berges, Rocks[rnd.Next(Rocks.Length)], new Vector3(p.x, GroundHeight(p.x, p.y) - 0.08f, p.y), taille, false);
                    if (st != null) nb++;
                }
            }
            // bassin : couronne de pierres plus grosses, sauf côté rivière et côté ravine
            for (int s = 0; s < 18; s++)
            {
                float ang = s * 20f + (float)rnd.NextDouble() * 8f;
                Vector2 p = V5Bassin + new Vector2(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad)) * (V5BassinRayon + 0.6f);
                if (V5DistanceAxe(p.x, p.y, out _) < V5DemiLargeur + 0.8f && Vector2.Distance(p, V5Bassin) > 1f && p.y < V5Bassin.y) continue;
                GameObject st = PlaceRock(berges, Rocks[rnd.Next(Rocks.Length)], new Vector3(p.x, GroundHeight(p.x, p.y) - 0.1f, p.y), 0.7f + (float)rnd.NextDouble() * 0.6f, false);
                if (st != null) nb++;
            }
        }
        // f. cascade : voile d'eau (lèvre -> coude -> bassin), gemmes et son (CascadeVillage)
        string cascade = V5Cascade(riv);
        // h. écume : contre les pierres des gués et les piles des ponts, filant vers l'aval (EcumeRiviere)
        {
            var pts = new List<Vector3>(); var avals = new List<Vector3>(); var rays = new List<float>();
            System.Action<Vector3, float> ajouter = (q, r) =>
            {
                V5DistanceAxe(q.x, q.z, out int si);
                Vector2 dd = (axe[Mathf.Min(axe.Count - 1, si + 1)] - axe[Mathf.Max(0, si - 1)]).normalized;
                pts.Add(new Vector3(q.x, V5NiveauEau, q.z)); avals.Add(new Vector3(dd.x, 0f, dd.y)); rays.Add(r);
            };
            foreach (Transform st in gues.GetComponentsInChildren<Transform>()) if (st.name == "Pierre_Gue") ajouter(st.position, 0.4f);
            foreach (var q in piles) ajouter(q, 0.22f);
            var ec = riv.gameObject.AddComponent<EcumeRiviere>();
            ec.points = pts.ToArray(); ec.aval = avals.ToArray(); ec.rayons = rays.ToArray();
            ec.materiauGemmes = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/PortalVoxel.mat");
            cascade += ", écume sur " + pts.Count + " obstacles";
        }
        // g. comportement : le héros ne traverse l'eau qu'aux gués et sur les ponts
        var rv = riv.gameObject.AddComponent<RiviereVillage>();
        var tr = new Vector3[axe.Count]; for (int i = 0; i < axe.Count; i++) tr[i] = new Vector3(axe[i].x, 0f, axe[i].y);
        // le bassin : l'axe y commence au centre ; la demi-largeur interdite du bassin est couverte par un cercle de points
        var tl = new List<Vector3>(tr);
        rv.trace = tl.ToArray();
        rv.demiLargeurInterdite = 2.45f;
        rv.gues = System.Array.ConvertAll(V5Gues, g => new Vector3(g.x, 0f, g.y)); rv.rayonGue = V5GueRayon;
        rv.ponts = System.Array.ConvertAll(V5Ponts, g => new Vector3(g.x, 0f, g.y)); rv.rayonPont = V5PontLongueur / 2f + 0.3f; rv.hauteurTablier = 0.05f;
        rv.rayonBassin = V5BassinRayon + 0.2f; rv.centreBassin = new Vector3(V5Bassin.x, 0f, V5Bassin.y);
        return "Rivière : " + axe.Count + " points d'axe, " + volumes + " volumes de lit, " + V5Gues.Length + " gués (" + pierres + " pierres), " + V5Ponts.Length + " ponts, " + nb + " pierres de berge ; " + cascade;
    }

    public static bool V5SurPont(Vector3 p, float marge) { return V5PresPont(new Vector2(p.x, p.z), marge); }
    static bool V5PresPont(Vector2 p, float marge)
    {
        for (int k = 0; k < V5Ponts.Length; k++)
        {
            Vector2 d = p - V5Ponts[k];
            float a = V5PontsLacet[k] * Mathf.Deg2Rad; Vector2 ax = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), cr = new Vector2(ax.y, -ax.x);
            if (Mathf.Abs(Vector2.Dot(d, ax)) < V5PontLongueur / 2f + marge && Mathf.Abs(Vector2.Dot(d, cr)) < V5PontLargeur / 2f + marge) return true;
        }
        return false;
    }

    static Mesh V5MeshAsset(string path, List<Vector3> v, List<int> t, List<Vector2> uv = null, List<Vector2> uv2 = null)
    {
        Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool neuf = m == null;
        if (neuf) m = new Mesh();
        m.Clear(); m.name = System.IO.Path.GetFileNameWithoutExtension(path);
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v); if (uv != null) m.SetUVs(0, uv); if (uv2 != null) m.SetUVs(1, uv2);
        m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        if (neuf) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    public const string V5RiviereMat = "Assets/Art/Materials/Village_Riviere.mat";
    /// Eau courante de la rivière (shader EauRiviere) : teintes du thème Eau (palette), traits d'écume du courant.
    static Material V5MateriauRiviere()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(V5RiviereMat);
        if (m == null) { m = new Material(Shader.Find("Deathless/Village/EauRiviere")); AssetDatabase.CreateAsset(m, V5RiviereMat); }
        Color fond = VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Ombre, new Color(0.11f, 0.24f, 0.4f)); fond.a = 0.8f;
        m.SetColor("_Couleur", fond);
        m.SetColor("_Reflet", VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Vif, new Color(0.45f, 0.72f, 0.9f)));
        m.SetColor("_Ecume", VfxPalette.Accent(VfxTheme.Eau, "Ecume", new Color(0.95f, 0.98f, 1f)));
        m.SetFloat("_EcumeForce", 0.55f); m.SetFloat("_Densite", 0.68f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material V5MateriauEau(string path, Color couleur, Color reflet, float amplitude, float vitesse)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Donjon/Donjon_Eau.mat");
            m = src != null ? new Material(src) : new Material(Shader.Find("Deathless/Donjon/EauLowPoly"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_Couleur", couleur); m.SetColor("_Reflet", reflet);
        m.SetFloat("_Amplitude", amplitude); m.SetFloat("_Vitesse", vitesse);
        EditorUtility.SetDirty(m);
        return m;
    }

    const string V5PontModele = "Assets/Art/KayKit/KayKit_Medieval_Builder_Pack_1.0/Models/objects/fbx/bridge.fbx";
    static List<Vector3> V5Pont(Transform parent, string nom, Vector3 c, float lacet)
    {
        var piles = new List<Vector3>();
        Transform p = Group(parent, nom);
        p.SetPositionAndRotation(c, Quaternion.Euler(0f, lacet, 0f));
        // visuel : planches, longerons, poteaux et main courante en bois (le pont KayKit du Builder Pack n'a pas de matériau)
        Material bois = FlatMaterial("Assets/Art/Materials/Pont_Bois.mat", "Universal Render Pipeline/Lit", new Color(0.55f, 0.36f, 0.22f));
        Material boisSombre = FlatMaterial("Assets/Art/Materials/Pont_Bois_Sombre.mat", "Universal Render Pipeline/Lit", new Color(0.36f, 0.23f, 0.14f));
        var rnd = new System.Random(nom.GetHashCode() & 0xffff);
        System.Action<string, Vector3, Vector3, Vector3, Material> boite = (n, lp, le, ls, m) =>
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(p, false); g.transform.localPosition = lp; g.transform.localEulerAngles = le; g.transform.localScale = ls;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
        };
        float Lv = V5PontLongueur, haut = V5PontHaut;
        // planches en travers, qui suivent le profil (rampes aux bouts)
        for (float z = -Lv / 2f + 0.22f; z <= Lv / 2f - 0.2f; z += 0.46f)
        {
            float bout = Mathf.Abs(z) - (Lv / 2f - 1.6f);
            float y = bout > 0f ? haut * (1f - bout / 1.6f) : haut;
            float pente = bout > 0f ? Mathf.Sign(z) * Mathf.Atan2(haut, 1.6f) * Mathf.Rad2Deg : 0f;
            boite("Planche", new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.08f, y - 0.05f, z), new Vector3(pente, ((float)rnd.NextDouble() - 0.5f) * 3f, 0f), new Vector3(V5PontLargeur, 0.1f, 0.42f), bois);
        }
        foreach (int sgn in new[] { -1, 1 })
        {
            float x = sgn * (V5PontLargeur / 2f - 0.2f);
            boite("Longeron", new Vector3(x, haut - 0.22f, 0f), Vector3.zero, new Vector3(0.22f, 0.25f, Lv - 3f), boisSombre);
            for (float z = -(Lv / 2f - 1.6f); z <= Lv / 2f - 1.6f + 0.01f; z += (Lv - 3.2f) / 2f)
                boite("Poteau", new Vector3(sgn * (V5PontLargeur / 2f + 0.1f), haut + 0.2f, z), Vector3.zero, new Vector3(0.18f, 1.4f, 0.18f), boisSombre);
            boite("Main_Courante", new Vector3(sgn * (V5PontLargeur / 2f + 0.1f), haut + 0.95f, 0f), Vector3.zero, new Vector3(0.14f, 0.12f, Lv - 3.0f), bois);
            // piles dans l'eau (l'écume s'y forme), sous les longerons
            foreach (float pz in new[] { -1.15f, 1.15f })
            {
                boite("Pile", new Vector3(sgn * (V5PontLargeur / 2f - 0.2f), -0.45f + haut / 2f - 0.15f, pz), Vector3.zero, new Vector3(0.3f, 1.2f + haut, 0.3f), boisSombre);
                piles.Add(p.TransformPoint(new Vector3(sgn * (V5PontLargeur / 2f - 0.2f), 0f, pz)));
            }
        }
        // tablier marchable : plat au centre, rampes aux deux bouts (pente ≈ 11°), garde-corps de 1 m
        float L = V5PontLongueur, rampe = 1.6f, plat = L - 2f * rampe;
        GameObject t = new GameObject("Tablier"); t.transform.SetParent(p, false);
        t.transform.localPosition = new Vector3(0f, V5PontHaut - 0.1f, 0f);
        var bt = t.AddComponent<BoxCollider>(); bt.size = new Vector3(V5PontLargeur, 0.2f, plat);
        foreach (int sgn in new[] { -1, 1 })
        {
            GameObject r = new GameObject(sgn < 0 ? "Rampe_A" : "Rampe_B"); r.transform.SetParent(p, false);
            float ang = Mathf.Atan2(V5PontHaut, rampe) * Mathf.Rad2Deg, longR = Mathf.Sqrt(rampe * rampe + V5PontHaut * V5PontHaut);
            r.transform.localPosition = new Vector3(0f, V5PontHaut / 2f - 0.1f * Mathf.Cos(ang * Mathf.Deg2Rad), sgn * (plat / 2f + rampe / 2f));
            r.transform.localRotation = Quaternion.Euler(sgn * ang, 0f, 0f);
            var br = r.AddComponent<BoxCollider>(); br.size = new Vector3(V5PontLargeur, 0.2f, longR + 0.05f);
            GameObject g0 = new GameObject("Garde_Corps_" + (sgn < 0 ? "G" : "D")); g0.transform.SetParent(p, false);
            g0.transform.localPosition = new Vector3(sgn * (V5PontLargeur / 2f + 0.1f), V5PontHaut + 0.5f, 0f);
            var bg = g0.AddComponent<BoxCollider>(); bg.size = new Vector3(0.2f, 1.0f, plat + 0.4f);
        }
        return piles;
    }

    static string V5Cascade(Transform riv)
    {
        Vector3 levre = V5Local(V5CascadeLevreLocal), coude = V5Local(V5CascadeCoudeLocal);
        Vector3 pied = new Vector3(V5Bassin.x, V5NiveauEau, V5Bassin.y) + (new Vector3(coude.x, 0f, coude.z) - new Vector3(V5Bassin.x, 0f, V5Bassin.y)).normalized * 1.2f;
        Transform cas = Group(riv, "Cascade");
        Material voile = V5MateriauEau(V5CascadeMat, new Color(0.62f, 0.8f, 0.94f, 0.72f), new Color(0.9f, 0.97f, 1f, 1f), 0.08f, 3.2f);
        // voile : ruban vertical de la lèvre au coude, puis nappe sur l'éboulis jusqu'au pied (4 colonnes, rangs serrés)
        var pts = new List<Vector3>(); var largeurs = new List<float>();
        for (int i = 0; i <= 12; i++) { float u = i / 12f; pts.Add(Vector3.Lerp(levre, coude, u) + Vector3.forward * 0f); largeurs.Add(Mathf.Lerp(2.2f, 2.8f, u)); }
        for (int i = 1; i <= 8; i++) { float u = i / 8f; Vector3 q = Vector3.Lerp(coude, pied, u); q.y = Mathf.Lerp(coude.y, pied.y, Mathf.Pow(u, 0.8f)) + 0.08f; pts.Add(q); largeurs.Add(Mathf.Lerp(2.8f, 3.6f, u)); }
        var v = new List<Vector3>(); var t = new List<int>(); int cols = 4;
        Vector3 droite = Vector3.Cross(Vector3.up, (new Vector3(pied.x, 0f, pied.z) - new Vector3(levre.x, 0f, levre.z)).normalized);
        if (droite.sqrMagnitude < 0.01f) droite = Vector3.right;
        for (int i = 0; i < pts.Count; i++)
            for (int c = 0; c <= cols; c++) v.Add(pts[i] + droite * Mathf.Lerp(-largeurs[i] / 2f, largeurs[i] / 2f, c / (float)cols));
        for (int i = 0; i + 1 < pts.Count; i++)
            for (int c = 0; c < cols; c++)
            {
                int a0 = i * (cols + 1) + c, a1 = a0 + 1, b0 = a0 + cols + 1, b1 = b0 + 1;
                t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);
            }
        GameObject go = new GameObject("Voile"); go.transform.SetParent(cas, false);
        go.AddComponent<MeshFilter>().sharedMesh = V5MeshAsset("Assets/Art/Meshes/Village_Cascade.asset", v, t);
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = voile; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // repères de l'effet
        Transform tl = new GameObject("Levre").transform; tl.SetParent(cas, false); tl.position = levre;
        Vector3 av = new Vector3(coude.x - levre.x, 0f, coude.z - levre.z); if (av.sqrMagnitude < 0.01f) av = Vector3.back;
        tl.rotation = Quaternion.LookRotation(av.normalized);
        Transform tc = new GameObject("Coude").transform; tc.SetParent(cas, false); tc.position = coude;
        Transform tp = new GameObject("Pied").transform; tp.SetParent(cas, false); tp.position = pied;
        var cv = cas.gameObject.AddComponent<CascadeVillage>();
        cv.levre = tl; cv.coude = tc; cv.pied = tp;
        cv.materiauGemmes = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/_RelicCommun/PortalVoxel.mat");
        return "cascade de " + (levre.y - V5NiveauEau).ToString("F1") + " m";
    }

    // ------------------------------------------------------------------ 4. allées
    /// Allées pavées anneau -> maison (par le gué pour les maisons de la rive est) et route de la grotte (4 m, lanternes).
    public static string V5Allees(Transform root)
    {
        Random.InitState(Seed + 41);
        s_V5Allees.Clear(); s_V5AlleesLarg.Clear();
        Transform sentiers = root.Find("Sentiers");
        if (sentiers != null) Kill(sentiers);
        sentiers = Group(root, "Sentiers");
        Transform ms = root.Find("Maisons");
        int n = 0;
        if (ms != null)
            for (int i = 0; i < V5MaisonsNoms.Length; i++)
            {
                Transform h = TrouverMaison(root, V5MaisonsNoms[i]); if (h == null) continue;
                var mf = h.GetComponent<MeshFilter>(); if (mf == null) continue;
                Vector3 avant = h.forward; avant.y = 0f; avant.Normalize();
                Vector3 front = new Vector3(h.position.x, 0f, h.position.z) + avant * (mf.sharedMesh.bounds.max.z * h.lossyScale.z + 0.6f);
                var pts = new List<Vector3>();
                int gue = V5MaisonsRoles[i] == "Druide" ? 0 : V5MaisonsRoles[i] == "Forge" ? 1 : V5MaisonsRoles[i] == "Mecano" ? 2 : -1;
                Vector3 premier = gue >= 0 ? new Vector3(V5Gues[gue].x, 0f, V5Gues[gue].y) : front;
                pts.Add(Polar(AzOf(premier), PaversRadius - 0.5f));
                Transform grp = Group(sentiers, "Sentier_Maison_" + (i + 1));
                if (gue >= 0)
                {
                    Vector2 g = V5Gues[gue];
                    V5DistanceAxe(g.x, g.y, out int si);
                    var axe = V5Axe();
                    Vector2 d = (axe[Mathf.Min(axe.Count - 1, si + 2)] - axe[Mathf.Max(0, si - 2)]).normalized, nn = new Vector2(-d.y, d.x);
                    if (Vector2.Dot(nn, g) < 0f) nn = -nn;     // vers l'extérieur du village
                    float bord = V5BergeExt + 0.2f;
                    Vector2 e = g - nn * bord, s = g + nn * bord;
                    PavedPath(grp, pts[0], new Vector3(e.x, 0f, e.y), 1); V5NoterAllee(pts[0], new Vector3(e.x, 0f, e.y), 1f);
                    PavedPath(grp, new Vector3(s.x, 0f, s.y), front, 1); V5NoterAllee(new Vector3(s.x, 0f, s.y), front, 1f);
                }
                else { PavedPath(grp, pts[0], front, 1); V5NoterAllee(pts[0], front, 1f); }
                n++;
            }
        // route de la grotte : 4 m pavés, de l'anneau au pied de l'escalier ; dernier tronçon (V5RouteDroite m) strictement droit,
        // dans l'axe de l'entrée et de l'escalier (retour du 02/10/2026 : la route arrivait de biais) ; lanternes de part et d'autre
        Vector3 entree = V5Local(V5GrotteEntreeLocal); entree.y = 0f;
        Vector3 axeG = V5Local(V5GrotteFondLocal) - entree; axeG.y = 0f; axeG.Normalize();
        Vector3 pied = entree - axeG * V5GrotteMarche;
        Vector3 coude = pied - axeG * V5RouteDroite;
        Vector3 depart = Polar(AzOf(coude), PaversRadius - 0.5f);
        Vector3 arrivee = pied + axeG * 0.5f;
        Transform route = Group(sentiers, "Route_Grotte");
        PavedPath(route, depart, coude, 2); V5NoterAllee(depart, coude, 2f);
        PavedPath(route, coude, arrivee, 2); V5NoterAllee(coude, arrivee, 2f);
        int lanternes = V5LanternesRoute(root, route, coude, arrivee);
        return "Allées : " + n + " maisons (trois par un gué), route de la grotte " + (Vector3.Distance(depart, coude) + Vector3.Distance(coude, arrivee)).ToString("F1") + " m dont " + Vector3.Distance(coude, arrivee).ToString("F1") + " m droits dans l'axe, et " + lanternes + " lanternes";
    }

    static void V5NoterAllee(Vector3 a, Vector3 b, float demi) { s_V5Allees.Add(new Vector4(a.x, a.z, b.x, b.z)); s_V5AlleesLarg.Add(demi); }

    static int V5LanternesRoute(Transform root, Transform parent, Vector3 a, Vector3 b)
    {
        Material lanterneMat = LanterneAssets.Materiau();
        LanternesReglages reglages = LanterneAssets.Reglages();
        Material flammeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Lanterne_Flamme.mat");
        var lumieres = new List<Light>(); var flammes = new List<GameObject>();
        Vector3 d = (b - a); d.y = 0f; Vector3 dir = d.normalized, droite = Vector3.Cross(Vector3.up, dir);
        float[] places = { 0.3f, 0.55f, 0.8f, 0.97f };
        for (int k = 0; k < places.Length; k++)
        {
            float cote = k % 2 == 0 ? 1f : -1f;
            Vector3 p = a + d * places[k] + droite * cote * 2.7f;
            p.y = GroundHeight(p.x, p.z);
            bool poteau = k >= 2;
            float ech = poteau ? 1.1f : LanternScale;
            GameObject lan = Place(parent, HalloweenRoot + (poteau ? "post_lantern" : "lantern_standing"), p, Quaternion.LookRotation(-droite * cote).eulerAngles.y, ech);
            if (lan == null) continue;
            lan.name = "Lanterne_Grotte_" + (k + 1);
            GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Sphere); fl.name = "Flamme";
            Object.DestroyImmediate(fl.GetComponent<Collider>());
            fl.transform.SetParent(lan.transform, false); fl.transform.localScale = Vector3.one * 0.14f / ech;
            var fr = fl.GetComponent<MeshRenderer>(); fr.sharedMaterial = flammeMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fl.SetActive(false); flammes.Add(fl);
            GameObject lg = new GameObject("Lumiere"); lg.transform.SetParent(lan.transform, false);
            Light l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = LanternRange; l.color = LanterneLumiere.CouleurDefaut; l.intensity = 0f; l.shadows = LightShadows.None; l.enabled = false;
            LanterneLumiere.Configurer(lan, l, fl.transform, lanterneMat, reglages, 2.2f, false, false);
            lumieres.Add(l);
        }
        var cycle = root.GetComponentInChildren<CycleJourNuit>();
        if (cycle != null)
        {
            var ls = new List<Light>(); if (cycle.lanternes != null) ls.AddRange(cycle.lanternes); ls.RemoveAll(x => x == null); ls.AddRange(lumieres);
            var fs = new List<GameObject>(); if (cycle.flammes != null) fs.AddRange(cycle.flammes); fs.RemoveAll(x => x == null); fs.AddRange(flammes);
            cycle.lanternes = ls.ToArray(); cycle.flammes = fs.ToArray();
            EditorUtility.SetDirty(cycle);
        }
        return lumieres.Count;
    }

    // ------------------------------------------------------------------ 5. clairières
    /// Points d'apparition et zones d'apparition sur les trois clairières du plan (est, sud, ouest), dans l'ordre des
    /// tableaux de DirecteurVagues (les références de la scène ne changent pas, seuls les objets bougent et sont renommés).
    public static string V5Clairieres(Transform root)
    {
        var sb = new StringBuilder("Clairières : ");
        var dv = Object.FindAnyObjectByType<DirecteurVagues>();
        for (int g = 0; g < 3; g++)
        {
            Vector3 c = ClearingCenter(g); c.y = GroundHeight(c.x, c.z);
            Quaternion q = Quaternion.LookRotation(-new Vector3(c.x, 0f, c.z), Vector3.up);
            Transform sp = dv != null && dv.clairieres != null && g < dv.clairieres.Length ? dv.clairieres[g] : null;
            if (sp != null) { sp.SetPositionAndRotation(c, q); sp.name = "Spawn_" + TrailName[g]; }
            var z = dv != null && dv.zones != null && g < dv.zones.Length ? dv.zones[g] : null;
            if (z != null) { z.transform.SetPositionAndRotation(c, q); z.name = "Zone_" + TrailName[g]; EditorUtility.SetDirty(z); }
            sb.Append(TrailName[g] + " (" + c.x.ToString("F0") + ", " + c.z.ToString("F0") + ") ; ");
        }
        if (dv != null) EditorUtility.SetDirty(dv);
        return sb.ToString();
    }

    // ------------------------------------------------------------------ 6. nature
    public static string V5Nature(Transform root)
    {
        Random.InitState(Seed + 500);
        V5EmprisesMaisons(root);
        if (s_V5Allees.Count == 0) V5AlleesDepuisScene(root);
        Transform foret = root.Find("Foret"); if (foret == null) foret = Group(root, "Foret");
        Kill(foret.Find("Arbres")); Kill(foret.Find("Lisiere")); Kill(foret.Find("Herbe")); Kill(foret.Find("Lande")); Kill(foret.Find("Pierrier"));
        Transform arbres = Group(foret, "Arbres"), lisiere = Group(foret, "Lisiere"), herbe = Group(foret, "Herbe"), lande = Group(foret, "Lande"), pierrier = Group(foret, "Pierrier");
        int nArbres = 0, nLande = 0, nPierrier = 0, nBuissons = 0, nHerbe = 0;
        // a. forêt au sud et à l'ouest (grille hexagonale espacée, troncs à collider), arbres isolés sur la lande
        float rowStep = TreeSpacing * 0.866f; int row = 0;
        for (float z = -TerrainHalf + 1f; z <= TerrainHalf - 1f; z += rowStep, row++)
        {
            float x0 = -TerrainHalf + 1f + ((row & 1) == 1 ? TreeSpacing / 2f : 0f);
            for (float x = x0; x <= TerrainHalf - 1f; x += TreeSpacing)
            {
                Vector3 p = new Vector3(x + Random.Range(-TreeJitter, TreeJitter), 0f, z + Random.Range(-TreeJitter, TreeJitter));
                float tirage = Random.value;
                if (!V5ArbrePossible(p)) continue;
                float l = V5Lande(new Vector2(p.x, p.z));
                if (l > 0.35f && tirage > 0.07f) continue;                 // lande : arbres isolés
                p.y = GroundHeight(p.x, p.z);
                PlaceTree(arbres, p, l > 0.35f ? tirage < 0.035f : tirage < 0.06f);
                nArbres++;
            }
        }
        // b. lisière : buissons et rochers à la limite de la prairie (hors lande, routes, rivière, maisons)
        for (int i = 0; i < 140; i++)
        {
            Vector3 p = Polar(Random.Range(0f, 360f), Random.Range(41f, 48f));
            float taille = Random.Range(RockMinSize, RockMaxSize); bool buisson = Random.value < 0.65f;
            if (!V5Libre(p, 2.5f) || V5Lande(new Vector2(p.x, p.z)) > 0.4f || p.z > 30f) continue;
            p.y = GroundHeight(p.x, p.z);
            if (buisson) { Place(lisiere, ForestRoot + Bushes[Random.Range(0, Bushes.Length)], p, Random.Range(0f, 360f), Random.Range(3f, 5f)); nBuissons++; }
            else PlaceRock(lisiere, Rocks[Random.Range(0, Rocks.Length)], p, taille, taille >= 1f);
        }
        // c. lande : rochers épars (gros = collider), quelques arbres morts
        for (int i = 0; i < 220 && nLande < 70; i++)
        {
            Vector3 p = new Vector3(Random.Range(16f, 99f), 0f, Random.Range(-36f, 34f));
            if (V5Lande(new Vector2(p.x, p.z)) < 0.6f || !V5Libre(p, 3f)) continue;
            p.y = GroundHeight(p.x, p.z);
            if (Random.value < 0.12f) PlaceTree(lande, p, true);
            else { float s = Random.Range(0.5f, 2.4f); PlaceRock(lande, Rocks[Random.Range(0, Rocks.Length)], p, s, s >= 1.2f); }
            nLande++;
        }
        // d. pierrier au pied de la falaise : éboulis entre la falaise et les maisons (gros blocs à collider)
        for (int i = 0; i < 260 && nPierrier < 60; i++)
        {
            Vector3 p = new Vector3(Random.Range(-60f, 60f), 0f, Random.Range(29f, 40f));
            if (!V5Libre(p, 2.2f)) continue;
            if (V5DansMontagne(p)) continue;
            p.y = GroundHeight(p.x, p.z);
            float s = Random.Range(0.6f, 2.6f) * (p.z > 35f ? 1.2f : 0.8f);
            PlaceRock(pierrier, Rocks[Random.Range(0, Rocks.Length)], p, s, s >= 1.2f);
            nPierrier++;
        }
        // e. herbe de la prairie
        for (int i = 0; i < 900 && nHerbe < 520; i++)
        {
            Vector3 p = Polar(Random.Range(0f, 360f), Random.Range(NexusClear + 1.5f, 52f));
            if (!V5Libre(p, 0.6f) || p.z > 31f) continue;
            if (V5Lande(new Vector2(p.x, p.z)) > 0.55f && Random.value < 0.7f) continue;
            p.y = GroundHeight(p.x, p.z);
            Place(herbe, ForestRoot + Grass[Random.Range(0, Grass.Length)], p, Random.Range(0f, 360f), Random.Range(1.5f, 2.2f));
            nHerbe++;
        }
        MarkStatic(foret.gameObject);
        return "Nature : " + nArbres + " arbres, " + nBuissons + " buissons de lisière, " + nLande + " pièces de lande, " + nPierrier + " blocs de pierrier, " + nHerbe + " touffes";
    }

    /// Reconstruit la liste des allées d'après les tuiles posées (étape 6 lancée seule).
    static void V5AlleesDepuisScene(Transform root)
    {
        s_V5Allees.Clear(); s_V5AlleesLarg.Clear();
        Transform s = root.Find("Sentiers"); if (s == null) return;
        foreach (Transform t in s.GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("path_")) V5NoterAllee(t.position, t.position + Vector3.forward * 0.01f, 1.0f);
    }

    /// Libre pour un décor posé au sol : hors de l'anneau, des allées, des routes et clairières, de l'eau, des maisons, des ponts.
    static bool V5Libre(Vector3 p, float marge)
    {
        Vector2 m = new Vector2(p.x, p.z);
        if (Mathf.Abs(p.x) > TerrainHalf - 1f || Mathf.Abs(p.z) > TerrainHalf - 1f) return false;
        if (m.magnitude < PaversRadius + 1.5f + marge) return false;
        if (V5PresAllee(m, marge)) return false;
        if (V5DistanceRoute(m) < V5DemiRoute + marge) return false;
        if (V5DistanceEau(p.x, p.z) < V5BergeExt + marge * 0.5f) return false;
        if (V5DistanceMaisons(m) < 1.5f + marge) return false;
        if (V5PresPont(m, marge)) return false;
        Vector3 e = V5Local(V5GrotteEntreeLocal);
        if (Vector2.Distance(m, new Vector2(e.x, e.z)) < 6f + marge) return false;
        return true;
    }

    static bool V5ArbrePossible(Vector3 p)
    {
        Vector2 m = new Vector2(p.x, p.z);
        if (m.magnitude < 44f) return false;
        if (p.z > (Mathf.Abs(p.x) > 62f ? 42f : 30f)) return false;     // montagne et flancs
        return V5Libre(p, TrailClear);
    }

    /// Approximation de l'emprise au sol de la pièce héros (relevé du profil avant, repère de la pièce).
    static bool V5DansMontagne(Vector3 p)
    {
        Vector3 l = Quaternion.Inverse(Quaternion.Euler(0f, V5MontagneLacet - 180f, 0f)) * (p - V5MontagnePos);
        // l.z < 0 = devant ; profil avant au sol (m) relevé à 0,3 m de haut, tous les 4 m de -60 à +60
        float[] av = { -3.0f, -4.7f, -7.8f, -9.2f, -7.6f, -8.8f, -10.3f, -12.5f, -13.5f, -13.5f, -12.7f, -11.1f, -6.9f, -13.1f, -9.3f, -12.4f, -16.0f, -17.5f, -18.7f, -16.8f, -15.4f, -16.7f, -16.1f, -14.2f, -19.6f, -17.1f, -11.2f, -9.7f, -6.3f, -1.3f, 1.8f };
        float u = (l.x + 60f) / 4f;
        if (u < 0f || u > av.Length - 1) return false;
        int i = Mathf.FloorToInt(u); float f = u - i;
        float front = Mathf.Lerp(av[i], av[Mathf.Min(av.Length - 1, i + 1)], f);
        return -l.z > front - 0.8f && -l.z < 20f;
    }

    // ------------------------------------------------------------------ 7. sol
    public static string V5Sol(Transform root)
    {
        if (!V5SceneOk()) return "Refusé : ouvrir " + V5ScenePath;
        V5EmprisesMaisons(root);
        Transform sol = root.Find("Sol");
        Kill(root.Find("Sol/Sol_Village"));
        Transform arbres = root.Find("Foret/Arbres");
        string m0 = GroundMeshPath, t0 = GroundTexPath, x0 = GroundMatPath;
        GroundMeshPath = V5SolMesh; GroundTexPath = V5SolTex; GroundMatPath = V5SolMat;   // assets propres à la v5
        try { BuildGround(sol != null ? sol : root, arbres != null ? arbres : Group(root, "_vide"), root); }
        finally { GroundMeshPath = m0; GroundTexPath = t0; GroundMatPath = x0; }
        Kill(root.Find("_vide"));
        Physics.SyncTransforms();
        return "Sol : " + GroundStats;
    }

    // ------------------------------------------------------------------ vérification
    /// Chemins NavMesh de chaque clairière à Nyxessa (longueur, temps au pas d'un sbire, passage par un pont ou un gué),
    /// accès au portail, au fond de la grotte et à chaque maison depuis Nyxessa.
    public static string V5Verifier()
    {
        var sb = new StringBuilder("Vérification v5 :\n");
        var b = AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/Jeu/Resources/GameBalance.asset");
        float vitesse = 0f;
        if (b != null)
        {
            var so = new SerializedObject(b);
            var sp = so.FindProperty("sbire.vitesse");
            if (sp == null) { var it = so.GetIterator(); while (it.NextVisible(true)) if (it.name == "vitesse" && it.propertyPath.ToLower().Contains("sbire")) { sp = it.Copy(); break; } }
            if (sp != null) vitesse = sp.floatValue;
        }
        if (vitesse <= 0f) vitesse = 3f;
        var path = new NavMeshPath();
        Vector3 nyx = Vector3.zero;
        NavMesh.SamplePosition(Polar(0f, 6.5f), out var cible, 3f, NavMesh.AllAreas);
        for (int g = 0; g < 3; g++)
        {
            Vector3 c = ClearingCenter(g);
            if (!NavMesh.SamplePosition(c, out var hit, 4f, NavMesh.AllAreas)) { sb.AppendLine("Clairière " + TrailName[g] + " : hors NavMesh"); continue; }
            foreach (float ang in new[] { 0f, 120f, 240f })
            {
                Vector3 place = Quaternion.Euler(0f, ang, 0f) * (c.normalized * -0.0f) + Polar(AzOf(c) + ang * 0.15f - 18f, 6.5f);
                if (!NavMesh.SamplePosition(place, out var pl, 3f, NavMesh.AllAreas)) continue;
                bool ok = NavMesh.CalculatePath(hit.position, pl.position, NavMesh.AllAreas, path);
                float L = 0f; var co = path.corners; string via = "";
                for (int i = 1; i < co.Length; i++)
                {
                    L += Vector3.Distance(co[i - 1], co[i]);
                    for (int k = 0; k < V5Ponts.Length; k++) if (Vector2.Distance(new Vector2(co[i].x, co[i].z), V5Ponts[k]) < V5PontLongueur / 2f + 1f && !via.Contains(k == 0 ? "pont est" : "pont sud")) via += k == 0 ? " pont est" : " pont sud";
                    for (int k = 0; k < V5Gues.Length; k++) if (Vector2.Distance(new Vector2(co[i].x, co[i].z), V5Gues[k]) < V5GueRayon + 1f && !via.Contains("gué " + k)) via += " gué " + k;
                }
                // passage de l'eau : le chemin traverse-t-il la rivière ? (échantillons tous les 0,5 m)
                int traverse = 0; float ancien = 99f;
                for (int i = 1; i < co.Length; i++)
                    for (float s = 0f; s <= 1f; s += 0.5f / Mathf.Max(0.5f, Vector3.Distance(co[i - 1], co[i])))
                    {
                        Vector3 q = Vector3.Lerp(co[i - 1], co[i], s);
                        float d = V5DistanceEau(q.x, q.z);
                        if (d < V5DemiLargeur && ancien >= V5DemiLargeur)
                        {
                            bool permis = V5Gue(q.x, q.z) < V5GueRayon + 0.5f || V5PresPont(new Vector2(q.x, q.z), 0.3f);
                            traverse += permis ? 1 : 100;
                            if (permis) { for (int k = 0; k < V5Ponts.Length; k++) if (V5PresPont(new Vector2(q.x, q.z), 0.3f) && Vector2.Distance(new Vector2(q.x, q.z), V5Ponts[k]) < 6f && !via.Contains(k == 0 ? "pont est" : "pont sud")) via += k == 0 ? " pont est" : " pont sud";
                                          for (int k = 0; k < V5Gues.Length; k++) if (Vector2.Distance(new Vector2(q.x, q.z), V5Gues[k]) < V5GueRayon + 0.5f && !via.Contains("gué " + k)) via += " gué " + k; }
                        }
                        ancien = d;
                    }
                sb.AppendLine("Clairière " + TrailName[g] + " -> Nyxessa (place " + ang + ") : " + path.status + ", " + L.ToString("F1") + " m, " + (L / vitesse).ToString("F1") + " s à " + vitesse.ToString("F2") + " m/s" + (traverse >= 100 ? " ; TRAVERSE L'EAU HORS PASSAGE" : traverse > 0 ? " ; franchit la rivière par" + via : ""));
            }
        }
        Transform por = Find("VillageBlockout/Portail/PortailDonjon");
        if (por != null)
        {
            Vector3 dev = por.position; dev.y = 0f; Vector3 vers = (-dev).normalized;
            NavMesh.SamplePosition(dev + vers * 2.5f, out var pp, 3f, NavMesh.AllAreas);
            bool ok = NavMesh.CalculatePath(cible.position, pp.position, NavMesh.AllAreas, path);
            float L = 0f; for (int i = 1; i < path.corners.Length; i++) L += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            sb.AppendLine("Nyxessa -> portail (grotte) : " + path.status + ", " + L.ToString("F1") + " m, arrivée à " + Vector3.Distance(pp.position, new Vector3(por.position.x, pp.position.y, por.position.z)).ToString("F1") + " m du portail");
        }
        Transform ms = Find("VillageBlockout/Maisons");
        if (ms != null)
            foreach (Transform racine in ms)
            {
                Transform h = racine.Find("Porte") != null ? racine.Find("Porte") : racine;
                if (!NavMesh.SamplePosition(h.position, out var hp, 2f, NavMesh.AllAreas)) { sb.AppendLine(racine.name + " : porte hors NavMesh"); continue; }
                NavMesh.CalculatePath(cible.position, hp.position, NavMesh.AllAreas, path);
                float L = 0f; for (int i = 1; i < path.corners.Length; i++) L += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                sb.AppendLine("Nyxessa -> porte de " + racine.name + " : " + path.status + ", " + L.ToString("F1") + " m");
            }
        return sb.ToString();
    }
}
