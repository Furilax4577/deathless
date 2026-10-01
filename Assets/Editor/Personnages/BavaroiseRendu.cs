using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Captures de vérification de la Bavaroise (BavaroiseTripo, modèle Tripo v3) aux angles de la planche de référence
// (ArtSources/References/Personnages/bavaroise_planche.jpg) : 3/4 face, face, profil, dos, T-pose ; fond gris sombre,
// lumière douce fixe par rapport à la caméra (c'est le personnage qui tourne, comme sur la planche).
// Scène de rendu Assets/Scenes/Dev/BavaroiseRendu.unity (recréée à chaque fois), puis retour à la scène ouverte avant.
// Sorties (Assets/Screenshots/) : <préfixe>_comparaison.png (planche en haut, rendu en bas, vues de 654 x 986 réduites),
// <préfixe>_animation.png (Idle_A, Running_A, Melee_Dualwield_Attack_Slice), <préfixe>_portrait.png (720 x 720, 3/4 face,
// cadrage des portraits du wiki).
public static class BavaroiseRendu
{
    const string CheminScene = "Assets/Scenes/Dev/BavaroiseRendu.unity";
    const string Planche = "ArtSources/References/Personnages/bavaroise_planche.jpg";
    const string Sortie = "Assets/Screenshots/";
    const int L = 654, H = 986;

    // Vues de la planche : x, y (depuis le haut), largeur, hauteur en pixels de la planche (1792 x 1008).
    static readonly int[,] Vues = { { 42, 190, 335, 493 }, { 392, 190, 327, 493 }, { 735, 190, 324, 493 }, { 1075, 190, 322, 493 }, { 1412, 190, 334, 493 } };
    public static readonly string[] Noms = { "trois_quarts", "face", "profil", "dos", "tpose" };
    static readonly float[] Lacets = { 38f, 0f, 90f, 180f, 0f };

    [MenuItem("Deathless/Personnages/Bavaroise : captures")]
    public static void Menu() { Debug.Log(Captures("bavaroise_v3")); }

    public static string Captures(string prefixe)
    {
        var avant = EditorSceneManager.GetActiveScene();
        if (avant.isDirty) return "Bavaroise : la scène ouverte (" + avant.path + ") a des modifications non enregistrées, captures annulées";
        string cheminAvant = avant.path;
        var journal = new System.Text.StringBuilder();
        // Compilation des shaders synchrone pendant les captures (sinon la première vue peut sortir sans le personnage).
        bool async = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        try
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes/Dev");
            System.IO.Directory.CreateDirectory(Sortie);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Ambiance(new Color(0.19f, 0.19f, 0.19f));
            EditorSceneManager.SaveScene(scene, CheminScene);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BavaroiseTripo.CheminPrefab);
            if (prefab == null) return "Bavaroise : prefab introuvable (lancer Deathless > Personnages > Bavaroise)";
            var perso = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var idle = BavaroiseTripo.Clip("Rig_Medium_General.fbx", "Idle_A");
            var course = BavaroiseTripo.Clip("Rig_Medium_MovementBasic.fbx", "Running_A");
            var coup = BavaroiseTripo.Clip("Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Slice");

            var cam = Camera();
            var vues = new Texture2D[Noms.Length];
            for (int v = 0; v < Noms.Length; v++)
            {
                Pose(perso, v == 4 ? null : idle, 0f);
                // T-pose de la planche : mains vides.
                foreach (var t in perso.GetComponentsInChildren<MeshRenderer>(true)) if (t.name.StartsWith("Chope") || (t.transform.parent != null && t.transform.parent.name.StartsWith("Chope"))) t.enabled = v != 4;
                perso.transform.rotation = Quaternion.Euler(0f, Lacets[v], 0f);
                vues[v] = Rendre(cam, L, H, new Vector3(0f, 0.9f, 0f), 6f, 6.3f, 20f);
            }
            foreach (var t in perso.GetComponentsInChildren<MeshRenderer>(true)) t.enabled = true;
            journal.Append("vues ").Append(Noms.Length);

            // Comparaison : planche en haut, rendu en bas (cases de 327 x 493).
            var planche = new Texture2D(2, 2); planche.LoadImage(System.IO.File.ReadAllBytes(Planche));
            int cl = 327, ch = 493;
            var comp = new Texture2D(cl * Noms.Length, ch * 2, TextureFormat.RGB24, false);
            for (int v = 0; v < Noms.Length; v++)
            {
                int x0 = Vues[v, 0], y0 = Vues[v, 1], w = Vues[v, 2], h = Vues[v, 3];
                for (int y = 0; y < ch; y++)
                    for (int x = 0; x < cl; x++)
                    {
                        // Recentrée : on prend une fenêtre de 327 x 493 au milieu de la vue de la planche.
                        int px = x0 + (w - cl) / 2 + x, py = planche.height - 1 - (y0 + (h - ch) / 2 + (ch - 1 - y));
                        comp.SetPixel(v * cl + x, ch + y, planche.GetPixel(px, py));
                        comp.SetPixel(v * cl + x, y, vues[v].GetPixelBilinear((x + 0.5f) / cl, (y + 0.5f) / ch));
                    }
            }
            comp.Apply();
            Ecrire(comp, Sortie + prefixe + "_comparaison.png");

            // Gros plans : visage (face) et tablier (face), en idle, côte à côte.
            Pose(perso, idle, 0f);
            perso.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var visage = Rendre(cam, 600, 600, new Vector3(0f, 1.45f, 0f), 3f, 2.2f, 20f);
            var tablier = Rendre(cam, 600, 600, new Vector3(0f, 0.72f, 0f), 3f, 2.2f, 20f);
            var gros = new Texture2D(1200, 600, TextureFormat.RGB24, false);
            gros.SetPixels(0, 0, 600, 600, visage.GetPixels()); gros.SetPixels(600, 0, 600, 600, tablier.GetPixels()); gros.Apply();
            Ecrire(gros, Sortie + prefixe + "_gros_plan.png");

            // Animation : Idle_A, Running_A (2 instants), Melee_Dualwield_Attack_Slice (3 instants), de 3/4.
            var instants = new List<KeyValuePair<AnimationClip, float>> {
                new KeyValuePair<AnimationClip, float>(idle, 0.6f), new KeyValuePair<AnimationClip, float>(course, 0.15f),
                new KeyValuePair<AnimationClip, float>(course, 0.55f), new KeyValuePair<AnimationClip, float>(coup, 0.3f),
                new KeyValuePair<AnimationClip, float>(coup, 0.5f), new KeyValuePair<AnimationClip, float>(coup, 0.75f) };
            int al = 436, ah = 657;
            var anim = new Texture2D(al * instants.Count, ah, TextureFormat.RGB24, false);
            for (int k = 0; k < instants.Count; k++)
            {
                Pose(perso, instants[k].Key, instants[k].Value);
                perso.transform.rotation = Quaternion.Euler(0f, 38f, 0f);
                var t = Rendre(cam, al, ah, new Vector3(0f, 0.92f, 0f), 8f, 6.4f, 20f);
                anim.SetPixels(k * al, 0, al, ah, t.GetPixels());
                Object.DestroyImmediate(t);
            }
            anim.Apply();
            Ecrire(anim, Sortie + prefixe + "_animation.png");

            // Portrait du wiki : 720 x 720, 3/4 face, fond bleu nuit et disque au sol comme les autres portraits.
            Ambiance(new Color(0.11f, 0.12f, 0.15f));
            var disque = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disque.GetComponent<Collider>());
            disque.transform.localScale = new Vector3(1.9f, 0.005f, 1.9f);
            var matDisque = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            matDisque.SetColor("_BaseColor", new Color(0.22f, 0.23f, 0.28f));
            disque.GetComponent<MeshRenderer>().sharedMaterial = matDisque;
            Pose(perso, idle, 0f);
            perso.transform.rotation = Quaternion.Euler(0f, 32f, 0f);
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.15f);
            var portrait = Rendre(cam, 720, 720, new Vector3(0f, 0.95f, 0f), 17f, 4.7f, 30f);
            Ecrire(portrait, Sortie + prefixe + "_portrait.png");
            Object.DestroyImmediate(matDisque);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CheminScene);
            journal.Append(", comparaison, gros plan, animation, portrait -> ").Append(Sortie).Append(prefixe).Append("_*.png");
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = async;
            if (!string.IsNullOrEmpty(cheminAvant)) EditorSceneManager.OpenScene(cheminAvant, OpenSceneMode.Single);
        }
        return "Bavaroise : captures faites (" + journal + ")";
    }

    static void Pose(GameObject perso, AnimationClip clip, float t)
    {
        // Retour à la pose de liaison, puis échantillonnage du clip.
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(BavaroiseTripo.CheminPrefab);
        var dst = perso.GetComponentsInChildren<Transform>(true);
        var map = new Dictionary<string, Transform>();
        foreach (var x in src.GetComponentsInChildren<Transform>(true)) map[x.name] = x;
        foreach (var x in dst)
        {
            Transform s;
            if (x == perso.transform || !map.TryGetValue(x.name, out s)) continue;
            x.localPosition = s.localPosition; x.localRotation = s.localRotation; x.localScale = s.localScale;
        }
        perso.transform.position = Vector3.zero;
        if (clip != null)
        {
            clip.SampleAnimation(perso, t);
            // Recentre sur les hanches (certains coups avancent le personnage).
            Transform hanches = null;
            foreach (var x in dst) if (x.name == "hips") hanches = x;
            if (hanches != null) perso.transform.position = -new Vector3(hanches.position.x, 0f, hanches.position.z);
        }
    }

    static void Ambiance(Color fond)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.44f, 0.43f, 0.45f);
        RenderSettings.fog = false;
        RenderSettings.skybox = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) Object.DestroyImmediate(l.gameObject);
        Lumiere("Cle", new Vector3(24f, 215f, 0f), 0.85f, new Color(1f, 0.94f, 0.88f), LightShadows.Soft);
        Lumiere("Remplissage", new Vector3(14f, 145f, 0f), 0.38f, new Color(0.9f, 0.93f, 1f), LightShadows.None);
        Lumiere("Contre", new Vector3(28f, 10f, 0f), 0.35f, Color.white, LightShadows.None);
        var c = Object.FindFirstObjectByType<Camera>();
        if (c != null) c.backgroundColor = fond;
    }

    static void Lumiere(string nom, Vector3 euler, float intensite, Color couleur, LightShadows ombres)
    {
        var go = new GameObject(nom);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional; l.intensity = intensite; l.color = couleur; l.shadows = ombres; l.shadowStrength = 0.6f;
        go.transform.rotation = Quaternion.Euler(euler);
    }

    static Camera Camera()
    {
        var go = new GameObject("CameraRendu");
        var cam = go.AddComponent<Camera>();
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.19f, 0.19f, 0.19f);
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 50f;
        return cam;
    }

    static Texture2D Rendre(Camera cam, int w, int h, Vector3 vise, float tangage, float distance, float fov)
    {
        cam.fieldOfView = fov;
        var rot = Quaternion.Euler(tangage, 180f, 0f);
        cam.transform.SetPositionAndRotation(vise - rot * Vector3.forward * distance, rot);
        // Hors du mode Play, le skinning n'est pas recalculé par Camera.Render : maillages cuits (BakeMesh) dans la pose courante.
        var figes = new List<GameObject>();
        var smrs = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
        foreach (var s in smrs)
        {
            if (!s.enabled) continue;
            var cuit = new Mesh();
            s.BakeMesh(cuit, true);
            var go = new GameObject("Fige_" + s.name);
            go.transform.SetPositionAndRotation(s.transform.position, s.transform.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = cuit;
            go.AddComponent<MeshRenderer>().sharedMaterials = s.sharedMaterials;
            s.enabled = false;
            figes.Add(go);
        }
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt; cam.Render();
        foreach (var s in smrs) s.enabled = true;
        foreach (var go in figes) { Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(go); }
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt);
        return tex;
    }

    static void Ecrire(Texture2D t, string chemin)
    {
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), chemin), t.EncodeToPNG());
    }
}
