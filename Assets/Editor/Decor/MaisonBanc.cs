using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Banc de contrôle des maisons Tripo (02/10/2026) : scène Assets/Scenes/Dev/MaisonBanc.unity, créée par l'API de l'éditeur
// (jamais par écriture disque). Contenu : sol plat, lumière et ambiance de la carte (soleil (55, 325, 0) 1,3, ambiance à trois
// couleurs, brouillard linéaire, volume global de la carte), sur une rangée : la maison standard actuelle de la carte v5
// (building_home_A_blue x 7,5), la maison de base Tripo et sa variante de palette, puis la forge, le sorcier, le druide et le
// mécano, chacun suivi de sa variante de palette ; un héros (Modele de Heros_Paladin, 2,18 m) devant la porte de chaque
// maison pour l'échelle (et aux trois postes du forgeron). Captures 1920 x 1080 dans Assets/Screenshots/ (maison_base_*,
// forge_*, sorcier_*, druide_*, mecano_*, taverne_*), Camera.Render en édition, sans Play.
// Taverne (02/10/2026) : AjouterAuBanc("Taverne") l'ajoute à la scène SANS la régénérer (ouverture additive de MaisonBanc.unity,
// jamais écrite si elle est déjà ouverte) ; CapturerTaverneAdditif() photographie le prefab dans une scène temporaire additive
// loin de l'origine (la scène active, par exemple CarteV5, n'est ni remplacée ni modifiée ; son éclairage s'applique).
public static class MaisonBanc
{
    public const string Scene = "Assets/Scenes/Dev/MaisonBanc.unity";
    const string Standard = "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/buildings/blue/building_home_A_blue.fbx";
    const string MatStandard = "Assets/Art/Materials/KayKit_Hexagons_medieval_Maisons.mat";
    const string Heros = "Assets/Jeu/Prefabs/Heros_Paladin.prefab";
    const float EspaceX = 17f;
    static readonly string[] Pieces = { "Maison_Base", "Forge", "Sorcier", "Druide", "Mecano", "Taverne" };
    static readonly float[] PosX = { 0f, 70f, 140f, 205f, 270f, 345f };
    static readonly float[] PosVariante = { 17f, 92f, 156f, 224f, 286f, 380f };

    [System.Serializable] class PlanJson { public float[] origine_plan; }

    [MenuItem("Deathless/Village/Maisons Tripo - banc (scène)")]
    public static void Menu() { Debug.Log(Generer()); }

    static string Prefixe(string piece) { return piece == "Maison_Base" ? "maison_base_" : piece.ToLowerInvariant() + "_"; }

    public static string Generer()
    {
        if (EditorApplication.isPlaying) return "refusé : éditeur en Play";
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) return "refusé : la scène « " + SceneManager.GetSceneAt(i).name + " » a des modifications non enregistrées (l'enregistrer d'abord)";
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // lumière et ambiance de la carte
        var sun = new GameObject("Soleil"); sun.transform.rotation = Quaternion.Euler(55f, 325f, 0f);
        var l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.3f; l.color = Color.white;
        l.shadows = LightShadows.Soft; l.shadowStrength = 1f; l.shadowBias = 0.05f;
        RenderSettings.sun = l;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f); RenderSettings.ambientEquatorColor = new Color(0.46f, 0.49f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.24f, 0.24f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(0.74f, 0.8f, 0.88f);
        RenderSettings.fogStartDistance = 60f; RenderSettings.fogEndDistance = 260f;
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        var vol = new GameObject("Volume"); var v = vol.AddComponent<Volume>(); v.isGlobal = true;
        v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/VFX/_Ambiance/Lueur.asset");

        var sol = GameObject.CreatePrimitive(PrimitiveType.Plane); sol.name = "Sol"; sol.transform.localScale = new Vector3(70f, 1f, 30f);
        sol.transform.position = new Vector3(150f, 0f, 0f);
        sol.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Ground.mat");

        var g = new GameObject("Maisons").transform;
        var std = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Standard), g);
        PrefabUtility.UnpackPrefabInstance(std, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        std.name = "Maison_Standard_KayKit"; std.transform.position = new Vector3(-EspaceX, -0.03f, 0.39f); std.transform.localScale = Vector3.one * 7.5f;
        foreach (var r in std.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatStandard);

        var h = new GameObject("Heros").transform;
        Heroes(h, "Heros_Standard", new Vector3(-EspaceX + 0.4f, 0f, 6.0f), 180f);
        var sb = new StringBuilder("Banc : " + Scene + " : ");
        for (int i = 0; i < Pieces.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaisonTripo.CheminPrefab(Pieces[i]));
            if (prefab == null) { sb.Append(Pieces[i] + " absente ; "); continue; }
            var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g); m.name = Pieces[i] + "_Tripo"; m.transform.position = new Vector3(PosX[i], 0f, 0f);
            var mv = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g); mv.name = Pieces[i] + "_Tripo_Variante"; mv.transform.position = new Vector3(PosVariante[i], 0f, 0f);
            mv.transform.Find("Rendu").GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaisonTripo.Dossier + "/" + Pieces[i] + "_Variante.mat");
            Vector3 pe = m.transform.Find("Entree").position;
            Heroes(h, "Heros_" + Pieces[i], pe + new Vector3(0f, 0f, 2.3f), 180f);
            Heroes(h, "Heros_" + Pieces[i] + "_Variante", mv.transform.Find("Entree").position + new Vector3(0f, 0f, 2.3f), 180f);
            sb.Append(Pieces[i] + " x=" + PosX[i] + " ; ");
            if (Pieces[i] == "Forge")
            {
                // pièces séparées à venir : repères gris à l'échelle (enclume sur billot 1,0 m, bac 1,6 x 0,8 x 0,85 m) et un héros à chaque poste
                Transform en = m.transform.Find("Enclume_Ancre"), bac = m.transform.Find("Bac_Ancre");
                var billot = GameObject.CreatePrimitive(PrimitiveType.Cylinder); billot.name = "Repere_Billot"; Object.DestroyImmediate(billot.GetComponent<Collider>());
                billot.transform.position = en.position + Vector3.up * 0.225f; billot.transform.localScale = new Vector3(0.8f, 0.225f, 0.8f); billot.transform.SetParent(g, true);
                var enc = GameObject.CreatePrimitive(PrimitiveType.Cube); enc.name = "Repere_Enclume"; Object.DestroyImmediate(enc.GetComponent<Collider>());
                enc.transform.position = en.position + Vector3.up * 0.725f; enc.transform.localScale = new Vector3(1.1f, 0.55f, 0.5f); enc.transform.SetParent(g, true);
                var tub = GameObject.CreatePrimitive(PrimitiveType.Cube); tub.name = "Repere_Bac"; Object.DestroyImmediate(tub.GetComponent<Collider>());
                tub.transform.position = bac.position + Vector3.up * 0.425f; tub.transform.rotation = bac.rotation; tub.transform.localScale = new Vector3(0.8f, 0.85f, 1.6f); tub.transform.SetParent(g, true);
                foreach (string poste in new[] { "Poste_Chauffe", "Poste_Frappe", "Poste_Trempe" })
                {
                    Transform t = m.transform.Find(poste);
                    Heroes(h, "Heros_" + poste, t.position, t.eulerAngles.y);
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(sc);
        System.IO.Directory.CreateDirectory("Assets/Scenes/Dev");
        EditorSceneManager.SaveScene(sc, Scene);
        return sb.ToString();
    }


    // ------------------------------------------------------------------------------------------------ taverne (sans remplacer la scène active)
    [MenuItem("Deathless/Village/Taverne Tripo - ajouter au banc")]
    public static void MenuAjouterTaverne() { Debug.Log(AjouterAuBanc("Taverne")); }

    [MenuItem("Deathless/Village/Taverne Tripo - captures")]
    public static void MenuCapturesTaverne() { Debug.Log(CapturerTaverneAdditif()); }

    /// Ajoute une pièce (Pieces) et sa variante de palette à MaisonBanc.unity, sans régénérer le reste : la scène est ouverte en
    /// additif (la scène active n'est pas touchée), complétée, enregistrée puis refermée. Refus si le banc est déjà ouvert (écrire
    /// une scène ouverte déclenche le dialogue modal « modified externally » dans l'éditeur qui l'a ouverte).
    public static string AjouterAuBanc(string piece)
    {
        if (EditorApplication.isPlaying) return "refusé : éditeur en Play";
        int k = System.Array.IndexOf(Pieces, piece);
        if (k < 0) return "pièce inconnue : " + piece;
        if (SceneManager.GetSceneByPath(Scene).isLoaded) return "refusé : " + Scene + " est déjà ouverte";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaisonTripo.CheminPrefab(piece));
        if (prefab == null) return piece + " : prefab absent";
        var active = SceneManager.GetActiveScene();
        var sc = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
        try
        {
            Transform g = null, h = null;
            foreach (var r in sc.GetRootGameObjects()) { if (r.name == "Maisons") g = r.transform; else if (r.name == "Heros") h = r.transform; }
            if (g == null || h == null) return "banc inattendu (Maisons / Heros introuvables)";
            if (g.Find(piece + "_Tripo") != null) return piece + " déjà dans le banc";
            SceneManager.SetActiveScene(sc);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g); m.name = piece + "_Tripo"; m.transform.position = new Vector3(PosX[k], 0f, 0f);
            var mv = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g); mv.name = piece + "_Tripo_Variante"; mv.transform.position = new Vector3(PosVariante[k], 0f, 0f);
            mv.transform.Find("Rendu").GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaisonTripo.Dossier + "/" + piece + "_Variante.mat");
            Heroes(h, "Heros_" + piece, m.transform.Find("Entree").position + new Vector3(0f, 0f, 2.3f), 180f);
            Heroes(h, "Heros_" + piece + "_Variante", mv.transform.Find("Entree").position + new Vector3(0f, 0f, 2.3f), 180f);
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            return piece + " ajoutée au banc (x = " + PosX[k] + " et " + PosVariante[k] + ")";
        }
        finally
        {
            SceneManager.SetActiveScene(active);
            EditorSceneManager.CloseScene(sc, true);
        }
    }

    /// Captures de la taverne dans une scène additive temporaire (5 km de l'origine), éclairage de la scène active (aucune scène
    /// remplacée ni écrite). taverne_trois_quarts, _face, _dos, _profil_droit, _arche, _jeu_3e_personne (héros devant la porte,
    /// caméra d'épaule 5,5 m, 22°), _enseigne (héros sous la planche, caméra d'épaule à 6,5 m : le nom doit être lisible),
    /// _enseigne_gros_plan, _tonneau, _palettes_0_1.
    public static string CapturerTaverneAdditif()
    {
        if (EditorApplication.isPlaying) return "refusé : éditeur en Play";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaisonTripo.CheminPrefab("Taverne"));
        if (prefab == null) return "Taverne : prefab absent";
        var active = SceneManager.GetActiveScene();
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var sb = new StringBuilder("Taverne (captures) : ");
        try
        {
            SceneManager.SetActiveScene(sc);
            Vector3 o = new Vector3(5000f, 0f, 5000f);
            var sol = GameObject.CreatePrimitive(PrimitiveType.Plane); sol.name = "Sol"; sol.transform.localScale = new Vector3(14f, 1f, 10f); sol.transform.position = o + new Vector3(15f, 0f, 0f);
            sol.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Ground.mat");
            var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab); m.name = "Taverne_Tripo"; m.transform.position = o;
            var mv = (GameObject)PrefabUtility.InstantiatePrefab(prefab); mv.name = "Taverne_Tripo_Variante"; mv.transform.position = o + new Vector3(38f, 0f, 0f);
            mv.transform.Find("Rendu").GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaisonTripo.Dossier + "/Taverne_Variante.mat");
            var h = new GameObject("Heros").transform;
            Transform e = m.transform.Find("Entree"), ens = m.transform.Find("Enseigne"), ton = m.transform.Find("Tonneau_Fuite");
            Heroes(h, "Heros_Porte", e.position + new Vector3(0f, 0f, 2.3f), 180f);
            Vector3 pe = ens.position; pe.y = 0f;
            Heroes(h, "Heros_Enseigne", pe + new Vector3(0.9f, 0f, 1.6f), 180f);
            Bounds b = m.transform.Find("Rendu").GetComponent<MeshRenderer>().bounds;
            Vector3 c = b.center - Vector3.up * (0.07f * b.size.y);
            float dist = 1.7f * Mathf.Max(b.size.y * 1.1f, b.size.x * 0.75f, 12f);
            Photo("taverne_trois_quarts", c + new Vector3(-0.55f, 0.28f, 0.78f).normalized * dist, c, 36f, sb);
            Photo("taverne_face", c + new Vector3(0f, 0.12f, 1f).normalized * dist, c, 36f, sb);
            Photo("taverne_dos", c + new Vector3(0.25f, 0.2f, -1f).normalized * dist, c, 36f, sb);
            Photo("taverne_profil_droit", c + new Vector3(-1f, 0.15f, 0.1f).normalized * dist, c, 36f, sb);
            Photo("taverne_arche", e.position + new Vector3(-1.2f, 1.9f, 6.2f), e.position + new Vector3(0f, 1.6f, -0.5f), 40f, sb);
            // caméra de jeu : pivot à 1,6 m au-dessus du héros, tangage 22°, épaule 0,6 m à droite, recul 5,5 m, champ 60°
            Quaternion rot = Quaternion.Euler(22f, 180f, 0f);
            foreach (var cas in new[] { new[] { "taverne_jeu_3e_personne", "Heros_Porte" }, new[] { "taverne_enseigne", "Heros_Enseigne" } })
            {
                var hero = GameObject.Find(cas[1]).transform;
                Vector3 pivot = hero.position + Vector3.up * 1.6f; Vector3 epaule = pivot + rot * Vector3.right * 0.6f;
                Photo(cas[0], epaule - rot * Vector3.forward * 5.5f, epaule, 60f, sb, rot);
            }
            Photo("taverne_enseigne_gros_plan", ens.position + new Vector3(-1.2f, 0.3f, 7.5f), ens.position, 30f, sb);
            Photo("taverne_tonneau", ton.position + new Vector3(-3.5f, 3.2f, 9.5f), ton.position + Vector3.up * 0.8f, 40f, sb);
            Vector3 mid = (c + (mv.transform.Find("Rendu").GetComponent<MeshRenderer>().bounds.center - Vector3.up * (0.07f * b.size.y))) / 2f;
            Photo("taverne_palettes_0_1", mid + new Vector3(0f, 0.12f, 1f).normalized * Mathf.Max((38f + b.size.x) * 0.5f / 0.577f * 1.12f, b.size.y * 1.6f), mid, 36f, sb);
        }
        finally
        {
            SceneManager.SetActiveScene(active);
            EditorSceneManager.CloseScene(sc, true);
        }
        return sb.ToString();
    }

    /// À lancer EN PLAY (l'éditeur doit déjà y être) : pose le prefab Taverne à 5 km de l'origine dans la scène active, laisse tourner
    /// les effets (BiereFuite, FumeeCheminee) quelques secondes, photographie la fuite et la fumée (taverne_effet_fuite,
    /// taverne_effet_fumee) puis retire le tout. Rien n'est enregistré dans une scène.
    [MenuItem("Deathless/Village/Taverne Tripo - effets (en Play)")]
    public static void MenuEffetsPlay()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Taverne effets : à lancer en Play"); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaisonTripo.CheminPrefab("Taverne"));
        if (prefab == null) { Debug.Log("Taverne effets : prefab absent"); return; }
        Vector3 o = new Vector3(5000f, 0f, 5000f);
        var m = (GameObject)Object.Instantiate(prefab, o, Quaternion.identity);
        var sol = GameObject.CreatePrimitive(PrimitiveType.Plane); sol.transform.localScale = new Vector3(8f, 1f, 8f); sol.transform.position = o;
        sol.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Ground.mat");
        var bf = m.GetComponentInChildren<BiereFuite>(); var fc = m.GetComponentInChildren<FumeeCheminee>();
        if (bf != null) bf.distanceMax = 1e9f;
        if (fc != null) fc.distanceMax = 1e9f;
        float t0 = Time.realtimeSinceStartup; bool fait = false;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (fait || Time.realtimeSinceStartup - t0 < 6f) return;
            fait = true; EditorApplication.update -= cb;
            var sb = new StringBuilder("Taverne effets (Play) : ");
            if (bf != null) Photo("taverne_effet_fuite", bf.transform.position + new Vector3(-2.2f, 1.6f, 4.2f), bf.transform.position + bf.transform.forward * 0.9f, 40f, sb);
            if (fc != null) Photo("taverne_effet_fumee", fc.transform.position + new Vector3(-6f, -2f, 16f), fc.transform.position + Vector3.up * 2.5f, 40f, sb);
            Debug.Log(sb + (bf == null ? "BiereFuite absente " : "") + (fc == null ? "FumeeCheminee absente" : ""));
            Object.Destroy(m); Object.Destroy(sol);
            foreach (var g in Object.FindObjectsByType<GemmesVolantes>(FindObjectsSortMode.None))
                if (g.name == "BiereFuite_Gemmes" || g.name == "FumeeCheminee_Gemmes") Object.Destroy(g.gameObject);
        };
        EditorApplication.update += cb;
    }

    static void Heroes(Transform parent, string nom, Vector3 pos, float lacet)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Heros);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var modele = inst.transform.Find("Modele");
        var h = Object.Instantiate(modele.gameObject);
        Object.DestroyImmediate(inst);
        h.name = nom; h.transform.SetParent(parent, false);
        h.transform.position = pos; h.transform.rotation = Quaternion.Euler(0f, lacet, 0f);
        foreach (var a in h.GetComponentsInChildren<Animator>()) a.enabled = false;
    }

    // ------------------------------------------------------------------------------------------------ captures
    const int W = 1920, H = 1080;

    static Transform Piece(string piece, bool variante) { return GameObject.Find(piece + (variante ? "_Tripo_Variante" : "_Tripo")).transform; }

    static Bounds Bornes(Transform p) { return p.Find("Rendu").GetComponent<MeshRenderer>().bounds; }

    /// Captures d'une pièce : trois-quarts avant-droit, face, dos, profil droit, gros plan de l'entrée, caméra de jeu devant la porte,
    /// palettes (retenue à gauche, variante à droite), nuit ; la forge a en plus l'atelier (vue du dessus avec les zones libres).
    public static string Capturer(string piece)
    {
        if (SceneManager.GetActiveScene().path != Scene) return "ouvrir " + Scene + " d'abord";
        var sb = new StringBuilder(piece + " : ");
        string pre = Prefixe(piece);
        Transform p = Piece(piece, false);
        Bounds b = Bornes(p);
        Vector3 c = b.center - Vector3.up * (0.07f * b.size.y);
        float dist = 1.7f * Mathf.Max(b.size.y * 1.1f, b.size.x * 0.75f, 12f);
        Photo(pre + "trois_quarts", c + new Vector3(-0.55f, 0.28f, 0.78f).normalized * dist, c, 36f, sb);
        Photo(pre + "face", c + new Vector3(0f, 0.12f, 1f).normalized * dist, c, 36f, sb);
        Photo(pre + "dos", c + new Vector3(0.25f, 0.2f, -1f).normalized * dist, c, 36f, sb);
        Photo(pre + "profil_droit", c + new Vector3(-1f, 0.15f, 0.1f).normalized * dist, c, 36f, sb);
        Transform e = p.Find("Entree");
        Photo(pre + "arche", e.position + new Vector3(-1.2f, 1.9f, 6.2f), e.position + new Vector3(0f, 1.6f, -0.5f), 40f, sb);
        // caméra de jeu : pivot à 1,6 m au-dessus du héros, tangage 22°, épaule 0,6 m à droite, recul 5,5 m, champ 60°
        {
            var hero = GameObject.Find("Heros_" + piece).transform;
            Quaternion rot = Quaternion.Euler(22f, 180f, 0f);
            Vector3 pivot = hero.position + Vector3.up * 1.6f;
            Vector3 epaule = pivot + rot * Vector3.right * 0.6f;
            Photo(pre + "jeu_3e_personne", epaule - rot * Vector3.forward * 5.5f, epaule, 60f, sb, rot);
        }
        // palettes : retenue (à gauche dans l'image) et variante côte à côte
        Transform v = Piece(piece, true);
        Vector3 mid = (c + Bornes(v).center) / 2f;
        float sep = Mathf.Abs(Bornes(v).center.x - b.center.x);
        float dp = Mathf.Max((sep + b.size.x) * 0.5f / 0.577f * 1.12f, b.size.y * 1.6f);
        Photo(pre + "palettes_0_1", mid + new Vector3(0f, 0.12f, 1f).normalized * dp, mid - Vector3.up * (0.05f * b.size.y), 36f, sb);
        if (piece == "Forge") sb.Append(CapturerForge());
        sb.Append(CapturerNuit(piece));
        return sb.ToString();
    }

    // Forge : vue du dessus de l'atelier (toit coupé par le plan proche) avec la bande libre du brief (rouge), les zones libres
    // vérifiées par le pipeline (vert) et les trois postes ; caméra de jeu devant l'atelier ; vue rapprochée du foyer.
    static string CapturerForge()
    {
        var sb = new StringBuilder();
        Transform p = Piece("Forge", false);
        PlanJson pj = JsonUtility.FromJson<PlanJson>(File.ReadAllText(MaisonTripo.Dossier + "/Forge_Ancres.json"));
        Vector3 Plan(float xp, float yp, float z) { return p.TransformPoint(new Vector3(-(xp + pj.origine_plan[0]), z, -(yp + pj.origine_plan[1]))); }
        var temp = new List<GameObject>();
        Material Transp(Color col)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f); mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetColor("_BaseColor", col);
            return mat;
        }
        void Rect(float x0, float x1, float y0, float y1, float zc, float ep, Color col)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(q.GetComponent<Collider>());
            Vector3 a = Plan(x0, y0, zc), d = Plan(x1, y1, zc);
            q.transform.position = (a + d) / 2f; q.transform.rotation = p.rotation;
            q.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), ep, Mathf.Abs(y1 - y0));
            q.GetComponent<MeshRenderer>().sharedMaterial = Transp(col); temp.Add(q);
        }
        Rect(11.6f, 14.4f, 0f, 9.6f, 0.06f, 0.03f, new Color(1f, 0.1f, 0.1f, 0.30f));          // bande libre du brief (plan)
        Rect(10.6f, 15.8f, 0.6f, 3.7f, 0.10f, 0.03f, new Color(0.1f, 1f, 0.2f, 0.35f));        // zones libres vérifiées
        Rect(14.1f, 15.8f, 3.7f, 9.0f, 0.10f, 0.03f, new Color(0.1f, 1f, 0.2f, 0.35f));
        foreach (string n in new[] { "Poste_Chauffe", "Poste_Frappe", "Poste_Trempe", "Enclume_Ancre", "Bac_Ancre", "Foyer_Feu" })
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.position = p.Find(n).position + Vector3.up * 0.3f; s.transform.localScale = Vector3.one * 0.5f;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", n.StartsWith("Poste") ? new Color(1f, 0.9f, 0.1f) : n == "Foyer_Feu" ? new Color(1f, 0.4f, 0f) : Color.cyan);
            s.GetComponent<MeshRenderer>().sharedMaterial = m; temp.Add(s);
        }
        // dessus : caméra orthographique, plan proche à 3,8 m (le toit de l'atelier est à 4,4 m au plus bas), façade en bas de l'image
        {
            var go = new GameObject("CamCapture"); var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 8.2f; cam.nearClipPlane = 40f - 3.8f; cam.farClipPlane = 80f; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.4f);
            Vector3 ctr = Plan(13.5f, 4.8f, 0f);
            cam.transform.position = new Vector3(ctr.x, 40f, ctr.z); cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.back);
            Render(cam, "forge_dessus_atelier", sb);
            Object.DestroyImmediate(go);
        }
        // vue du dessus de tout le bâtiment, sans les repères
        {
            var go = new GameObject("CamCapture"); var cam = go.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 8.2f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 80f; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.55f, 0.4f);
            Vector3 ctr = Plan(8.7f, 4.8f, 0f);
            cam.transform.position = new Vector3(ctr.x, 40f, ctr.z); cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.back);
            cam.orthographicSize = 11f;
            Render(cam, "forge_dessus_toit", sb);
            Object.DestroyImmediate(go);
        }
        foreach (var t in temp) Object.DestroyImmediate(t);
        // caméra de jeu devant l'atelier : héros au poste de frappe, vu du sud
        {
            Transform front = Piece("Forge", false).Find("Poste_Frappe");
            var hero = GameObject.Find("Heros_Poste_Frappe").transform;
            Vector3 old = hero.position; Quaternion oldRot = hero.rotation;
            hero.position = front.position + new Vector3(0f, 0f, 2.6f); hero.rotation = Quaternion.Euler(0f, 180f, 0f);
            Quaternion rot = Quaternion.Euler(22f, 180f, 0f);
            Vector3 pivot = hero.position + Vector3.up * 1.6f; Vector3 epaule = pivot + rot * Vector3.right * 0.6f;
            Photo("forge_jeu_atelier", epaule - rot * Vector3.forward * 5.5f, epaule, 60f, sb, rot);
            hero.position = old; hero.rotation = oldRot;
        }
        Transform fo = Piece("Forge", false).Find("Foyer_Feu");
        Photo("forge_foyer", fo.position + new Vector3(2.5f, 1.2f, 6.5f), fo.position + new Vector3(0f, 0.6f, 0f), 40f, sb);
        return sb.ToString();
    }

    // Nuit simulée comme CycleJourNuit : ambiance sombre, lanterne allumée si la pièce en a une, vitres émissives par bloc de propriétés.
    static string CapturerNuit(string piece)
    {
        var sb = new StringBuilder();
        string pre = Prefixe(piece);
        var sun = RenderSettings.sun; var sr = sun.transform.rotation; float si = sun.intensity; Color sc = sun.color;
        var amb = new[] { RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor };
        Color fog = RenderSettings.fogColor;
        sun.intensity = 0.32f; sun.color = new Color(0.62f, 0.72f, 1f); sun.transform.rotation = Quaternion.Euler(40f, 235f, 0f);
        RenderSettings.ambientSkyColor = new Color(0.07f, 0.09f, 0.18f); RenderSettings.ambientEquatorColor = new Color(0.05f, 0.06f, 0.12f); RenderSettings.ambientGroundColor = new Color(0.03f, 0.03f, 0.05f);
        RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
        Transform m = Piece(piece, false);
        Transform lan = m.Find("Lanterne");
        Light lum = lan != null ? lan.GetComponentInChildren<Light>(true) : null;
        bool en = lum != null && lum.enabled; float li = lum != null ? lum.intensity : 0f;
        LanterneLumiere ll = lan != null ? lan.GetComponent<LanterneLumiere>() : null;
        if (ll != null) { ll.allumage = 1f; ll.Rafraichir(0f); }
        var bloc = new MaterialPropertyBlock();
        var mr = m.Find("Rendu").GetComponent<MeshRenderer>();
        mr.GetPropertyBlock(bloc); bloc.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.22f) * 1.3f); mr.SetPropertyBlock(bloc);
        Bounds b = Bornes(m);
        float dist = 1.4f * Mathf.Max(b.size.y * 1.1f, b.size.x * 0.75f, 12f);
        Photo(pre + "tripo_nuit", b.center + new Vector3(-0.55f, 0.1f, 0.8f).normalized * dist, b.center, 36f, sb);
        if (piece == "Forge")
        {
            Transform fo = m.Find("Foyer_Feu");
            Photo("forge_nuit_foyer", fo.position + new Vector3(2.5f, 1.2f, 6.5f), fo.position + new Vector3(0f, 0.6f, 0f), 40f, sb);
        }
        if (ll != null) ll.allumage = 0f;
        if (lum != null) { lum.enabled = en; lum.intensity = li; }
        mr.SetPropertyBlock(null);
        sun.intensity = si; sun.color = sc; sun.transform.rotation = sr;
        RenderSettings.ambientSkyColor = amb[0]; RenderSettings.ambientEquatorColor = amb[1]; RenderSettings.ambientGroundColor = amb[2]; RenderSettings.fogColor = fog;
        return sb.ToString();
    }

    /// Captures propres à la maison de base : comparaison avec la maison standard de la carte v5, trois maisons côte à côte, repères d'ancres.
    public static string CapturerBase()
    {
        if (SceneManager.GetActiveScene().path != Scene) return "ouvrir " + Scene + " d'abord";
        var sb = new StringBuilder("Base : ");
        Photo("maison_base_comparaison_standard", new Vector3(-8.5f, 5.5f, 33f), new Vector3(-8.5f, 4.0f, 0f), 36f, sb);
        Photo("maison_base_trois_maisons", new Vector3(0f, 7f, 52f), new Vector3(0f, 3.5f, 0f), 36f, sb);
        var maison = Piece("Maison_Base", false);
        var temp = new List<GameObject>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f); mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f, 0.35f));
        var e = maison.Find("Entree"); var bc = e.GetComponent<BoxCollider>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.position = e.TransformPoint(bc.center); cube.transform.rotation = e.rotation; cube.transform.localScale = bc.size;
        cube.GetComponent<MeshRenderer>().sharedMaterial = mat; temp.Add(cube);
        foreach (string n in new[] { "Porte_Pivot", "Volet_G", "Volet_D", "Lanterne" })
        {
            var t = maison.Find(n);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.position = t.position; s.transform.localScale = Vector3.one * 0.16f;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", n == "Porte_Pivot" ? Color.cyan : n == "Lanterne" ? Color.yellow : Color.magenta);
            s.GetComponent<MeshRenderer>().sharedMaterial = m; temp.Add(s);
        }
        Photo("maison_base_ancres", new Vector3(-0.5f, 2.6f, 15f), new Vector3(0.2f, 2.0f, 3f), 30f, sb);
        foreach (var gg in temp) Object.DestroyImmediate(gg);
        Object.DestroyImmediate(mat);
        return sb.ToString();
    }

    /// Repères visibles de l'entrée et des ancres d'une pièce, puis retirés (vérification des cotes).
    public static string CapturerAncres(string piece)
    {
        if (SceneManager.GetActiveScene().path != Scene) return "ouvrir " + Scene + " d'abord";
        var sb = new StringBuilder(piece + " ancres : ");
        var maison = Piece(piece, false);
        var temp = new List<GameObject>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f); mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f, 0.35f));
        var e = maison.Find("Entree"); var bc = e.GetComponent<BoxCollider>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.position = e.TransformPoint(bc.center); cube.transform.rotation = e.rotation; cube.transform.localScale = bc.size;
        cube.GetComponent<MeshRenderer>().sharedMaterial = mat; temp.Add(cube);
        foreach (Transform t in maison)
        {
            if (t.name == "Rendu" || t.name == "Collision" || t.name == "Entree") continue;
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(s.GetComponent<Collider>());
            s.transform.position = t.position; s.transform.localScale = Vector3.one * 0.2f;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", t.name == "Porte_Pivot" ? Color.cyan : t.name == "Lanterne" ? Color.yellow : Color.magenta);
            s.GetComponent<MeshRenderer>().sharedMaterial = m; temp.Add(s);
        }
        Photo(Prefixe(piece) + "ancres", e.position + new Vector3(-0.8f, 2.4f, 12f), e.position + new Vector3(0.2f, 1.8f, 0f), 30f, sb);
        foreach (var gg in temp) Object.DestroyImmediate(gg);
        Object.DestroyImmediate(mat);
        return sb.ToString();
    }

    public static void Photo(string nom, Vector3 pos, Vector3 cible, float fov, StringBuilder sb, Quaternion? rot = null)
    {
        var go = new GameObject("CamCapture");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 500f; cam.clearFlags = CameraClearFlags.Skybox;
        cam.transform.position = pos;
        if (rot.HasValue) cam.transform.rotation = rot.Value; else cam.transform.LookAt(cible);
        Render(cam, nom, sb);
        Object.DestroyImmediate(go);
    }

    static void Render(Camera cam, string nom, StringBuilder sb)
    {
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null;
        File.WriteAllBytes("Assets/Screenshots/" + nom + ".png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        sb.Append(nom).Append(' ');
    }
}
