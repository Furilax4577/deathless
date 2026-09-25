using System.Collections.Generic;
using Deathless.Donjon;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Menus Deathless > Donjon : génération avec graine réglable, vérification de la consigne sur 1 000 graines,
/// création du kit de pièces KayKit et préparation de la scène de test.
public static class DonjonMenu
{
    public const string CheminKit = "Assets/Donjon/DonjonKit.asset";
    public const string CheminAnneau = "Assets/Donjon/Donjon_PortailRetour.mat";
    const string Dungeon = "Assets/Art/KayKit/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string Halloween = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";

    [MenuItem("Deathless/Donjon/Générer", false, 1)]
    public static void Generer()
    {
        var g = Generateur(true);
        Generer(g, g.graine);
    }

    [MenuItem("Deathless/Donjon/Graine suivante", false, 2)]
    public static void GraineSuivante() { var g = Generateur(true); Generer(g, g.graine + 1); }

    [MenuItem("Deathless/Donjon/Graine précédente", false, 3)]
    public static void GrainePrecedente() { var g = Generateur(true); Generer(g, g.graine - 1); }

    [MenuItem("Deathless/Donjon/Graine au hasard", false, 4)]
    public static void GraineHasard() { var g = Generateur(true); Generer(g, Random.Range(1, 1000000)); }

    public static void Generer(DonjonGenerateur g, int graine)
    {
        Undo.RecordObject(g, "Graine du donjon");
        bool ok = g.Generer(graine);
        EditorUtility.SetDirty(g);
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
        DonjonPlan p = g.Plan;
        Debug.Log(string.Format("Donjon graine {0} : {1}, {2} essai(s), chemin critique {3:F1} m (consigne {4:F0} à {5:F0} m) = {6:F1} s à 5 m/s, {7:F1} s à 8 m/s ; tournée complète {8:F1} m ; plan {9:F2} ms, pièces {10:F1} ms, NavMesh {11:F1} ms, {12} objets, {13} lumières.",
            graine, ok ? "valide" : "HORS CONSIGNE", p.essais, p.cheminCritiqueDm / 10f, DonjonPlan.CheminMinDm / 10f, DonjonPlan.CheminMaxDm / 10f,
            p.cheminCritiqueDm / 50f, p.cheminCritiqueDm / 80f, p.CalculerTourneeDm() / 10f,
            g.DernierTempsPlanMs, g.DernierTempsConstructionMs, g.DernierTempsNavMeshMs, g.NbObjets, g.NbLumieres));
    }

    /// Générateur de la scène active ; en crée un (avec le kit) si besoin.
    public static DonjonGenerateur Generateur(bool creer)
    {
        var g = Object.FindFirstObjectByType<DonjonGenerateur>();
        if (g != null || !creer) { if (g != null && g.kit == null) g.kit = Kit(); return g; }
        var go = new GameObject("Donjon");
        Undo.RegisterCreatedObjectUndo(go, "Donjon");
        go.AddComponent<DonjonMasquage>();
        go.AddComponent<NavMeshSurface>();
        g = go.AddComponent<DonjonGenerateur>();
        g.kit = Kit();
        return g;
    }

    // ------------------------------------------------------------------ Vérification
    [MenuItem("Deathless/Donjon/Vérifier 1000 graines", false, 20)]
    public static void Verifier()
    {
        var plan = new DonjonPlan();
        int n = 1000, ok = 0, essais = 0, maxEssais = 0, rs = 0, rc = 0, rl = 0;
        int cmin = int.MaxValue, cmax = 0; long somme = 0;
        var liste = new List<int>(n);
        var chrono = System.Diagnostics.Stopwatch.StartNew();
        for (int s = 1; s <= n; s++)
        {
            bool v = plan.Generer(s);
            if (v) ok++;
            essais += plan.essais; if (plan.essais > maxEssais) maxEssais = plan.essais;
            rs += plan.rejetsStructure; rc += plan.rejetsTropCourt; rl += plan.rejetsTropLong;
            int c = plan.cheminCritiqueDm;
            if (c < cmin) cmin = c; if (c > cmax) cmax = c; somme += c; liste.Add(c);
        }
        chrono.Stop();
        liste.Sort();
        // Déterminisme : deux plans de la même graine ont la même empreinte.
        var a = new DonjonPlan(); var b = new DonjonPlan();
        int differents = 0;
        for (int s = 1; s <= 200; s++) { a.Generer(s); b.Generer(s); if (a.empreinte != b.empreinte) differents++; }
        Debug.Log(string.Format("Donjon, {0} graines : {1} valides ; chemin critique min {2:F1} m, médiane {3:F1} m, max {4:F1} m, moyenne {5:F1} m ; essais moyens {6:F2}, max {7} (rejets : structure {8}, trop court {9}, trop long {10}) ; {11:F2} ms par plan ; déterminisme : {12} différence(s) sur 200.",
            n, ok, cmin / 10f, liste[n / 2] / 10f, cmax / 10f, somme / (10f * n), essais / (float)n, maxEssais, rs, rc, rl, chrono.Elapsed.TotalMilliseconds / n, differents));
    }

    // ------------------------------------------------------------------ Kit
    [MenuItem("Deathless/Donjon/Créer ou mettre à jour le kit", false, 40)]
    public static void CreerKit() { Kit(true); }

    public static DonjonKit Kit(bool maj = false)
    {
        var kit = AssetDatabase.LoadAssetAtPath<DonjonKit>(CheminKit);
        if (kit != null && !maj) return kit;
        if (!AssetDatabase.IsValidFolder("Assets/Donjon")) AssetDatabase.CreateFolder("Assets", "Donjon");
        bool nouveau = kit == null;
        if (nouveau) kit = ScriptableObject.CreateInstance<DonjonKit>();
        // Sols en dalles hexagonales (floor_tile_large), quelques grilles ; balcons de pierre ou de bois.
        kit.solsRez = M(Dungeon, "floor_tile_large", "floor_tile_large", "floor_tile_large", "floor_tile_large", "floor_tile_large", "floor_tile_large", "floor_tile_large", "floor_tile_big_grate");
        kit.solsPierre = M(Dungeon, "floor_tile_large");
        kit.solsBois = M(Dungeon, "floor_wood_large", "floor_wood_large", "floor_wood_large_dark");
        kit.plafond = null;
        // Variantes de murs mélangées pour casser la répétition (planche 23) : briques différentes, arc, charpente
        // de bois, portes de bois. Pas de wall_cracked (mousse verte) ni de wall_shelves (pot vert).
        kit.murs = M(Dungeon, "wall", "wall", "wall", "wall_broken", "wall_arched", "wall_scaffold", "wall_scaffold", "wall_doorway", "wall_doorway_scaffold", "wall_pillar");
        kit.mursHauts = M(Dungeon, "wall_window_closed", "wall_archedwindow_gated", "wall_window_closed");
        kit.gardeCorps = M(Dungeon, "barrier")[0];
        kit.pilier = M(Dungeon, "pillar")[0];
        kit.poteau = M(Dungeon, "column")[0];
        kit.escalier = M(Dungeon, "stairs_long")[0];
        kit.escalierBassin = M(Dungeon, "stairs")[0];
        kit.arcade = M(Dungeon, "wall_open_scaffold")[0];
        kit.fondationBassin = M(Dungeon, "floor_foundation_front")[0];
        kit.eau = Eau();
        kit.tonneauFlottant = M(Dungeon, "barrel_large")[0];
        kit.grandCoffre = M(Dungeon, "chest_gold")[0];
        kit.coffre = M(Dungeon, "chest")[0];
        kit.tasOr = M(Dungeon, "coin_stack_large", "coin_stack_medium");
        // Mobilier des planches (tonneaux, caisses, tables) ; jamais rien qui ressemble à du butin (retour de Quentin) :
        // ni or, ni coffre, ni sac, ni malle. Pas de box_stacked (bouteilles vertes).
        kit.decorsCoin = M(Dungeon, "barrel_large", "barrel_small_stack", "barrel_large", "box_large", "crates_stacked", "table_small", "table_medium");   // pas de keg : couché sur son support, il ressemble à un coffre
        kit.tableLongue = M(Dungeon, "table_long")[0];
        kit.bannieres = M(Dungeon, "banner_red", "banner_patternA_red", "banner_shield_red", "banner_blue", "banner_patternB_blue", "banner_brown", "banner_triple_red");
        kit.os = M(Halloween, "bone_A", "bone_B", "bone_C", "skull", "ribcage");
        kit.dalleArrivee = null;
        kit.torcheMurale = M(Dungeon, "torch_mounted")[0];
        kit.torcheSurPied = M(Dungeon, "torch_lit")[0];
        kit.colonneTorchere = M(Dungeon, "column")[0];
        kit.socle = M(Dungeon, "floor_foundation_allsides")[0];
        kit.anneau = Anneau();
        if (nouveau) AssetDatabase.CreateAsset(kit, CheminKit);
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();
        return kit;
    }

    static GameObject[] M(string dossier, params string[] noms)
    {
        var r = new GameObject[noms.Length];
        for (int i = 0; i < noms.Length; i++)
        {
            r[i] = AssetDatabase.LoadAssetAtPath<GameObject>(dossier + noms[i] + ".fbx");
            if (r[i] == null) Debug.LogError("Donjon : pièce introuvable " + dossier + noms[i] + ".fbx");
        }
        return r;
    }

    /// Matériau de l'anneau du portail de retour : bronze chaud légèrement émissif, jamais vert (le vert est
    /// l'énergie de Nyxessa, et ce portail n'est pas alimenté par elle).
    static Material Anneau()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(CheminAnneau);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, CheminAnneau);
        }
        m.SetColor("_BaseColor", new Color(0.62f, 0.42f, 0.22f));
        m.SetFloat("_Smoothness", 0.35f);
        m.SetFloat("_Metallic", 0.6f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.2f) * 0.9f);
        m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    public const string CheminEau = "Assets/Donjon/Donjon_Eau.mat";

    /// Matériau de l'eau : shader EauLowPoly, bleu profond semi-opaque (jamais vert : réservé à Nyxessa).
    static Material Eau()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(CheminEau);
        if (m == null)
        {
            m = new Material(Shader.Find("Deathless/Donjon/EauLowPoly"));
            AssetDatabase.CreateAsset(m, CheminEau);
        }
        m.SetColor("_Couleur", new Color(0.11f, 0.22f, 0.38f, 0.74f));
        m.SetColor("_Reflet", new Color(0.42f, 0.56f, 0.74f, 1f));
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ Scène
    [MenuItem("Deathless/Donjon/Préparer la scène", false, 41)]
    public static void PreparerScene()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.27f, 0.27f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.07f, 0.055f, 0.06f);
        RenderSettings.fogDensity = 0.012f;
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.055f, 0.06f);
        cam.fieldOfView = 60f;
        cam.farClipPlane = 300f;
        cam.transform.position = new Vector3(30f, 55f, -25f);
        cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        Generateur(true);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
    }
}

[CustomEditor(typeof(DonjonGenerateur))]
public class DonjonGenerateurEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var g = (DonjonGenerateur)target;
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Générer")) DonjonMenu.Generer(g, g.graine);
            if (GUILayout.Button("◀")) DonjonMenu.Generer(g, g.graine - 1);
            if (GUILayout.Button("▶")) DonjonMenu.Generer(g, g.graine + 1);
            if (GUILayout.Button("Hasard")) DonjonMenu.Generer(g, Random.Range(1, 1000000));
        }
        DonjonPlan p = g.Plan;
        if (p != null && p.cheminCritiqueDm > 0)
            EditorGUILayout.HelpBox(string.Format("Graine {0} : chemin critique {1:F1} m ({2:F1} s à 5 m/s, {3:F1} s à 8 m/s), consigne {4:F0}–{5:F0} m. {6} objets, {7} lumières.",
                p.graine, p.cheminCritiqueDm / 10f, p.cheminCritiqueDm / 50f, p.cheminCritiqueDm / 80f, DonjonPlan.CheminMinDm / 10f, DonjonPlan.CheminMaxDm / 10f, g.NbObjets, g.NbLumieres), MessageType.Info);
    }
}
