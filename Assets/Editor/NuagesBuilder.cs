using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Nuages du ciel du village : modèles KayKit cloud_big / cloud_small (pack Medieval Hexagon), opaques, sans ombre.
// Menu Deathless > Niveau > Nuages : ré-exécutable, retire d'abord le groupe existant (pas de doublon), graine fixe
// (deux exécutions donnent le même ciel). Deathless > Niveau > Nuages - Retirer : enlève le groupe.
//
// - Groupe `Ciel_Nuages` sous `VillageBlockout` (à la racine de la scène s'il n'existe pas), centré sur le Nexus.
// - Répartition stationnaire sous la dérive : le long du vent, les nuages sont étalés uniformément (tirage stratifié) sur
//   toute la bande de dérive (2 x DemiLongueur) ; en travers du vent, sur ±DemiLargeur, plus serrés vers l'axe du village.
//   Une répartition dense au centre ne tiendrait pas : le vent l'emporterait en bloc et le ciel du village se viderait
//   pendant plusieurs minutes. Rien à moins de RayonLibre du Nexus au départ, écart minimal EcartMin entre nuages.
//   Hauteurs HauteurPetit / HauteurGrand : le bas du plus bas nuage reste au-dessus de 20 m, loin au-dessus de la caméra
//   de jeu (au plus 8 m environ) et de celle du menu (13 m), donc aucun nuage ne peut passer entre une caméra du village
//   et Nyxessa. Vitesse liée à l'altitude (plus haut, plus vite) : deux nuages à la même hauteur vont presque à la même
//   vitesse et ne se traversent pas.
// - Matériau `Assets/Art/Materials/KayKit_Nuages.mat` : URP Lit sur l'atlas du pack Hexagon (les UV des nuages pointent un
//   dégradé blanc -> gris-bleu, qui donne déjà le dessous ombré), mat, un peu d'émission pour que les flancs restent
//   blancs. Couleur jour / nuit pilotée à l'exécution par NuageDerive.
// - Ombres : aucune (ni portée ni reçue). Voir NuageDerive pour la dérive et la couleur.
public static class NuagesBuilder
{
    // ---------------- Paramètres ----------------
    public const int Seed = 1717;
    public const int Nombre = 32;
    public const float PartGrands = 0.45f;                                 // part de cloud_big
    public const float RayonLibre = 16f;                                   // aucun nuage à moins de 16 m du Nexus (à plat) au départ
    public const float DemiLargeur = 150f;                                 // en travers du vent
    public const float ExposantLargeur = 1.3f;                             // > 1 : plus serrés vers l'axe du village
    public const float EcartMin = 24f;                                     // entre centres de nuages (à plat)
    public static readonly Vector2 HauteurPetit = new Vector2(34f, 50f);   // centre du nuage (m)
    public static readonly Vector2 HauteurGrand = new Vector2(40f, 60f);
    public static readonly Vector2 EchellePetit = new Vector2(5f, 8f);     // cloud_small : 2,3 x 1,8 x 1,8 u -> 12 à 19 m
    public static readonly Vector2 EchelleGrand = new Vector2(6.5f, 10f);  // cloud_big : 3,6 x 1,9 x 2,2 u -> 23 à 36 m
    public static readonly Vector2 AplatVertical = new Vector2(0.8f, 1.05f);
    // Dérive (NuageDerive)
    public const float VentAzimut = 70f;          // vers l'est-nord-est
    public const float Vitesse = 0.8f;            // m/s
    public static readonly Vector2 FacteurVitesse = new Vector2(0.85f, 1.15f);   // du plus bas au plus haut
    public const float DemiLongueur = 210f;
    public const float BordFondu = 40f;

    public const string NomGroupe = "Ciel_Nuages";
    private const string Dossier = "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/decoration/nature/";
    private const string MateriauSource = "Assets/Art/Materials/KayKit_Hexagons_medieval.mat";
    public const string MateriauNuages = "Assets/Art/Materials/KayKit_Nuages.mat";

    [MenuItem("Deathless/Niveau/Nuages")]
    public static void Generer()
    {
        GameObject grand = AssetDatabase.LoadAssetAtPath<GameObject>(Dossier + "cloud_big.fbx");
        GameObject petit = AssetDatabase.LoadAssetAtPath<GameObject>(Dossier + "cloud_small.fbx");
        if (grand == null || petit == null) { Debug.LogError("Nuages : modèles KayKit introuvables dans " + Dossier); return; }
        Material mat = Materiau();
        if (mat == null) return;

        Retirer();
        GameObject village = GameObject.Find("VillageBlockout");
        Transform groupe = new GameObject(NomGroupe).transform;
        if (village != null) groupe.SetParent(village.transform, false);
        groupe.localPosition = Vector3.zero;
        groupe.localRotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(groupe.gameObject, "Nuages");

        System.Random rnd = new System.Random(Seed);
        float az = VentAzimut * Mathf.Deg2Rad;
        Vector3 axeU = new Vector3(Mathf.Sin(az), 0f, Mathf.Cos(az));      // le long du vent
        Vector3 axeV = new Vector3(axeU.z, 0f, -axeU.x);                    // en travers
        float hMin = Mathf.Min(HauteurPetit.x, HauteurGrand.x), hMax = Mathf.Max(HauteurPetit.y, HauteurGrand.y);
        var positions = new List<Vector3>();
        var nuages = new List<Transform>();
        var facteurs = new List<float>();
        var echelles = new List<Vector3>();
        float basMin = float.MaxValue;
        int tentatives = 0;
        for (int i = 0; i < Nombre; i++)
        {
            Vector3 p = Vector3.zero;
            bool libre = false;
            for (int essai = 0; essai < 200 && !libre; essai++)
            {
                tentatives++;
                float u = -DemiLongueur + (i + 0.1f + 0.8f * (float)rnd.NextDouble()) * (2f * DemiLongueur / Nombre);
                float v = DemiLargeur * Mathf.Pow((float)rnd.NextDouble(), ExposantLargeur) * (rnd.NextDouble() < 0.5 ? -1f : 1f);
                p = axeU * u + axeV * v;
                libre = p.magnitude >= RayonLibre;
                if (libre) foreach (Vector3 q in positions) if (new Vector2(q.x - p.x, q.z - p.z).sqrMagnitude < EcartMin * EcartMin) { libre = false; break; }
            }
            if (!libre) continue;

            bool estGrand = rnd.NextDouble() < PartGrands;
            Vector2 h = estGrand ? HauteurGrand : HauteurPetit;
            Vector2 e = estGrand ? EchelleGrand : EchellePetit;
            p.y = Mathf.Lerp(h.x, h.y, (float)rnd.NextDouble());
            float s = Mathf.Lerp(e.x, e.y, (float)rnd.NextDouble());
            Vector3 echelle = new Vector3(s, s * Mathf.Lerp(AplatVertical.x, AplatVertical.y, (float)rnd.NextDouble()), s);
            float lacet = (float)rnd.NextDouble() * 360f;
            float facteur = Mathf.Lerp(FacteurVitesse.x, FacteurVitesse.y, Mathf.InverseLerp(hMin, hMax, p.y));

            GameObject n = (GameObject)PrefabUtility.InstantiatePrefab(estGrand ? grand : petit, groupe);
            n.name = (estGrand ? "Nuage_Grand_" : "Nuage_Petit_") + nuages.Count.ToString("00");
            n.transform.localPosition = p;
            n.transform.localRotation = Quaternion.Euler(0f, lacet, 0f);
            n.transform.localScale = echelle;
            foreach (Collider c in n.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (MeshRenderer mr in n.GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
                basMin = Mathf.Min(basMin, mr.bounds.min.y);
            }
            GameObjectUtility.SetStaticEditorFlags(n, 0);
            positions.Add(p);
            nuages.Add(n.transform);
            facteurs.Add(facteur);
            echelles.Add(echelle);
        }

        NuageDerive derive = groupe.gameObject.AddComponent<NuageDerive>();
        derive.ventAzimut = VentAzimut;
        derive.vitesse = Vitesse;
        derive.demiLongueur = DemiLongueur;
        derive.bordFondu = BordFondu;
        derive.nuages = nuages.ToArray();
        derive.facteursVitesse = facteurs.ToArray();
        derive.echelles = echelles.ToArray();
        derive.materiau = mat;
        derive.cycle = Object.FindFirstObjectByType<CycleJourNuit>();

        int proches = 0;
        foreach (Vector3 p in positions) if (new Vector2(p.x, p.z).magnitude < 60f) proches++;
        EditorSceneManager.MarkSceneDirty(groupe.gameObject.scene);
        Debug.Log("Nuages : " + nuages.Count + " (" + tentatives + " tirages), dont " + proches + " à moins de 60 m du Nexus (à plat) ; bas du plus bas nuage à "
            + basMin.ToString("0.0") + " m ; cycle jour / nuit " + (derive.cycle != null ? "relié" : "introuvable (DayCycle.Night)") + ".");
    }

    [MenuItem("Deathless/Niveau/Nuages - Retirer")]
    public static void Retirer()
    {
        // En boucle, partout dans la scène : une génération antérieure a pu laisser un groupe ailleurs.
        var aRetirer = new List<GameObject>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name == NomGroupe && t.GetComponent<NuageDerive>() != null) aRetirer.Add(t.gameObject);
        foreach (GameObject g in aRetirer) if (g != null) Undo.DestroyObjectImmediate(g);
    }

    // Copie URP Lit du matériau du pack Hexagon (même atlas), créée au premier passage, réglée à chaque passage.
    private static Material Materiau()
    {
        Material source = AssetDatabase.LoadAssetAtPath<Material>(MateriauSource);
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MateriauNuages);
        if (m == null)
        {
            if (source == null) { Debug.LogError("Nuages : matériau source introuvable " + MateriauSource); return null; }
            m = new Material(source);
            AssetDatabase.CreateAsset(m, MateriauNuages);
        }
        if (source != null && source.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetFloat("_EnvironmentReflections", 0f);
        m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        m.SetFloat("_ReceiveShadows", 0f);
        m.EnableKeyword("_RECEIVE_SHADOWS_OFF");
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.20f, 0.21f, 0.24f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.enableInstancing = false;   // SRP Batcher
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }
}
