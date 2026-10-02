using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

// Maisons Tripo (02/10/2026) : montage Unity des pièces produites par la chaîne Blender
// ArtSources/Decor/Maisons/maison_pipeline.py (une seule image Grok -> Tripo -> décimation, atlas 2048² cuit, recoloration en
// palette, collision simplifiée). Ce script ne fait que le montage, pour chaque pièce (Maison_Base, Forge, Sorcier, Druide, Mecano, Taverne) :
// - réglages d'import (comme la montagne : normales du fichier, pas de matériaux importés) du FBX et des textures ;
// - matériaux URP Lit mats sans spéculaire (<Pièce>.mat = variante de palette retenue, <Pièce>_Variante.mat = l'autre), avec la
//   carte d'émission des vitres et du verre de la lanterne (allumés la nuit par CycleJourNuit.maisons, comme les maisons KayKit) ;
// - <Pièce>.prefab (GUID gardé à chaque régénération) :
//     <Pièce>                racine au sol, au centre de l'emprise, façade vers +Z (comme Batiment_* de la v5)
//       Rendu                maillage rendu (rotation d'import du FBX : Z de Blender vers le haut)
//       Collision            MeshCollider du maillage de collision + NavMeshModifier (jamais marchable, comme la montagne)
//       Entree               marqueur de l'entrée (1,8 x 2,6 m, déclencheur sur Ignore Raycast, axe avant = façade)
//       Lanterne             (si la pièce en a une) LanterneLumiere + Lumiere (point chaud, éteinte le jour, allumée la nuit)
//       Porte_Pivot, Volet_G, Volet_D, Foyer_Feu, Poste_*, ... ancres Transform (pièces animées, effets, postes à venir) :
//                            +Z local = direction de la façade ou du regard
//       Taverne : Enseigne (nom écrit en deux quads, avant et dos), Tonneau_Fuite (BiereFuite), Cheminee (FumeeCheminee)
// Les coordonnées viennent de <Pièce>_Ancres.json (repère Blender : x vers la droite vue de la façade, y vers le fond, z vers
// le haut) ; l'import FBX donne Unity = (-x, z, -y) (façade vers +Z).
public static class MaisonTripo
{
    public const string Dossier = "Assets/Art/Decor/Maisons";
    public const float EntreeLargeur = 1.8f, EntreeHauteur = 2.6f, EntreeProfondeur = 1.6f;
    public static readonly string[] Pieces = { "Maison_Base", "Forge", "Sorcier", "Druide", "Mecano", "Taverne" };

    [System.Serializable] class Ancre { public string nom; public float[] pos; public float[] dir; }
    [System.Serializable] class EntreeJson { public float[] centre; public float largeur, haut; }
    [System.Serializable] class EnseigneJson { public float[] centre; public float largeur, haut, epaisseur; }
    [System.Serializable] class Donnees { public float largeur, facade_y; public EnseigneJson enseigne; public EntreeJson entree; public float[] lanterne; public Ancre[] ancres; public string convention; }

    [MenuItem("Deathless/Village/Maison Tripo (base)")]
    public static void Menu() { Debug.Log(Construire("Maison_Base")); }
    [MenuItem("Deathless/Village/Forge Tripo")]
    public static void MenuForge() { Debug.Log(Construire("Forge")); }
    [MenuItem("Deathless/Village/Sorcier Tripo")]
    public static void MenuSorcier() { Debug.Log(Construire("Sorcier")); }
    [MenuItem("Deathless/Village/Druide Tripo")]
    public static void MenuDruide() { Debug.Log(Construire("Druide")); }
    [MenuItem("Deathless/Village/Mecano Tripo")]
    public static void MenuMecano() { Debug.Log(Construire("Mecano")); }

    [MenuItem("Deathless/Village/Taverne Tripo")]
    public static void MenuTaverne() { Debug.Log(Construire("Taverne")); }

    public static string Construire() { return Construire("Maison_Base"); }

    // Blender (x droite vue de la façade, y fond, z haut) -> Unity local (façade vers +Z) ; vaut aussi pour les directions
    static Vector3 U(float[] b) { return new Vector3(-b[0], b[2], -b[1]); }

    public static string CheminPrefab(string nom) { return Dossier + "/" + nom + ".prefab"; }

    public static string Construire(string nom)
    {
        var sb = new StringBuilder(nom + " (Tripo) : ");
        string fbx = Dossier + "/" + nom + ".fbx";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(fbx) == null) return "FBX absent (" + fbx + ") : lancer maison_pipeline.py --piece";
        ReglerModele(fbx);
        foreach (string t in new[] { "_Texture", "_Texture_Variante", "_Emission" }) ReglerTexture(Dossier + "/" + nom + t + ".png");
        AssetDatabase.Refresh();
        Material mat = Materiau(nom, nom + "_Texture", nom);
        Materiau(nom + "_Variante", nom + "_Texture_Variante", nom);   // l'autre variante de palette (comparaison du banc)
        Donnees d = JsonUtility.FromJson<Donnees>(System.IO.File.ReadAllText(Dossier + "/" + nom + "_Ancres.json"));

        GameObject modele = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        Mesh rendu = null, coll = null; Quaternion axeFbx = Quaternion.identity;
        foreach (var mf in modele.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh.name.Contains("Collision")) coll = mf.sharedMesh;
            else { rendu = mf.sharedMesh; axeFbx = mf.transform.localRotation; }
        }
        if (rendu == null || coll == null) return "maillages introuvables dans le FBX";

        var racine = new GameObject(nom);
        var r = new GameObject("Rendu"); r.transform.SetParent(racine.transform, false); r.transform.localRotation = axeFbx;
        r.AddComponent<MeshFilter>().sharedMesh = rendu;
        var mr = r.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        GameObjectUtility.SetStaticEditorFlags(r, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

        var c = new GameObject("Collision"); c.transform.SetParent(racine.transform, false); c.transform.localRotation = axeFbx;
        c.AddComponent<MeshCollider>().sharedMesh = coll;
        var mod = c.AddComponent<NavMeshModifier>(); mod.overrideArea = true; mod.area = 1;   // non marchable (zone « Not Walkable »)

        // entrée : repère au sol dans le plan de façade (ou au bord des marches) ; l'axe avant (+Z) sort du bâtiment
        Vector3 pe = U(d.entree.centre);
        var e = new GameObject("Entree"); e.layer = 2; e.transform.SetParent(racine.transform, false); e.transform.localPosition = pe;
        var bc = e.AddComponent<BoxCollider>(); bc.isTrigger = true;
        bc.size = new Vector3(EntreeLargeur, EntreeHauteur, EntreeProfondeur); bc.center = new Vector3(0f, EntreeHauteur / 2f, EntreeProfondeur / 2f);

        // lanterne (si la pièce en a une) : lumière chaude au centre du verre, un peu vers l'extérieur ; éteinte le jour (CycleJourNuit)
        string lan = "";
        if (d.lanterne != null && d.lanterne.Length == 3)
        {
            Vector3 pl = U(d.lanterne);
            var l = new GameObject("Lanterne"); l.transform.SetParent(racine.transform, false); l.transform.localPosition = pl;
            var lg = new GameObject("Lumiere"); lg.transform.SetParent(l.transform, false); lg.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            var lum = lg.AddComponent<Light>(); lum.type = LightType.Point; lum.range = VillageBuilder.LanternRange;
            lum.color = LanterneLumiere.CouleurDefaut; lum.intensity = 0f; lum.shadows = LightShadows.None; lum.enabled = false;
            var ll = l.AddComponent<LanterneLumiere>();
            ll.reglages = LanterneAssets.Reglages(); ll.lumiere = lum; ll.intensite = 2.2f; ll.allumage = 0f; ll.rendus = new Renderer[0];
            ll.couleur = LanterneLumiere.CouleurDefaut;
            lan = " ; lanterne " + pl.ToString("F2");
        }

        // ancres (Transform seuls, +Z local = direction du regard ou de la façade)
        foreach (var a in d.ancres)
        {
            var g = new GameObject(a.nom); g.transform.SetParent(racine.transform, false); g.transform.localPosition = U(a.pos);
            Vector3 dir = U(a.dir);
            if (dir.sqrMagnitude > 1e-6f) g.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
        if (nom == "Taverne") sb.Append(MonterTaverne(racine, d));

        PrefabUtility.SaveAsPrefabAsset(racine, CheminPrefab(nom));
        Object.DestroyImmediate(racine);
        AssetDatabase.SaveAssets();
        sb.Append("prefab ").Append(CheminPrefab(nom)).Append(" ; rendu ").Append(rendu.triangles.Length / 3).Append(" triangles, collision ")
          .Append(coll.triangles.Length / 3).Append(" ; emprise rendue ").Append(rendu.bounds.size.ToString("F2"))
          .Append(" ; entrée ").Append(pe.ToString("F2")).Append(lan).Append(" ; ").Append(d.ancres.Length).Append(" ancres");
        return sb.ToString();
    }

    /// Pose le prefab sous un objet racine de bâtiment (Maisons/Batiment_<Rôle> de la carte v5 : pivot au sol au centre de l'emprise,
    /// axe avant +Z tourné vers Nyxessa) et le branche sur le cycle jour / nuit : lumière de la lanterne dans CycleJourNuit.lanternes,
    /// rendu dans CycleJourNuit.maisons (vitres et verre de la lanterne émissifs la nuit). Ne touche à rien d'autre : l'ancien modèle,
    /// sa lanterne et sa zone « Porte » restent à retirer ou recaler par l'appelant (voir Docs/da/brief-maisons.md, § Réception).
    public static GameObject Poser(Transform batiment, CycleJourNuit cycle) { return Poser(batiment, cycle, "Maison_Base"); }

    public static GameObject Poser(Transform batiment, CycleJourNuit cycle, string nom)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheminPrefab(nom));
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, batiment);
        m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
        if (cycle != null)
        {
            var lum = m.GetComponentInChildren<Light>(true);
            if (lum != null)
            {
                var ls = new System.Collections.Generic.List<Light>(cycle.lanternes ?? new Light[0]); ls.RemoveAll(x => x == null);
                ls.Add(lum); cycle.lanternes = ls.ToArray();
            }
            var rs = new System.Collections.Generic.List<Renderer>(cycle.maisons ?? new Renderer[0]); rs.RemoveAll(x => x == null);
            rs.Add(m.transform.Find("Rendu").GetComponent<MeshRenderer>()); cycle.maisons = rs.ToArray();
            EditorUtility.SetDirty(cycle);
        }
        return m;
    }

    // ---------------------------------------------------------------------------------------------------- taverne
    public const string EnseigneTexture = Dossier + "/Taverne_Enseigne_Nom.png", EnseigneMateriau = Dossier + "/Taverne_Enseigne.mat";
    const float EnseigneRemplissage = 0.84f;   // le quad fait 84 % de la planche (le panneau plat à l intérieur du cadre en fait 82 %)

    /// Ce qui est propre à la taverne « Le Tonneau Percé » : le nom écrit sur l'enseigne (deux quads texturés, avant et dos, fixés à
    /// l'ancre Enseigne ; texture générée par ArtSources/Decor/Maisons/taverne_enseigne.py, police Fredoka Bold du projet), l'effet de
    /// fuite de bière sur Tonneau_Fuite (BiereFuite) et la fumée sur Cheminee (FumeeCheminee).
    static string MonterTaverne(GameObject racine, Donnees d)
    {
        var sb = new StringBuilder(" ; taverne :");
        sb.Append(" ").Append(BiereFuiteOutil.Assurer());
        ReglerTextureTexte(EnseigneTexture);
        Material mat = MateriauEnseigne();
        Transform ens = racine.transform.Find("Enseigne");
        if (ens != null && d.enseigne != null && mat != null)
        {
            Mesh quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            float l = d.enseigne.largeur * EnseigneRemplissage, h = d.enseigne.haut * EnseigneRemplissage;
            // l'ancre est sur la face avant de la planche, +Z local sort vers la façade ; Quad : face visible vers -Z
            Quad("Nom_Avant", ens, quad, mat, new Vector3(0f, 0f, 0.03f), Quaternion.Euler(0f, 180f, 0f), l, h);
            Quad("Nom_Dos", ens, quad, mat, new Vector3(0f, 0f, -(d.enseigne.epaisseur + 0.03f)), Quaternion.identity, l, h);
            sb.Append(" enseigne ").Append(l.ToString("F2")).Append(" x ").Append(h.ToString("F2")).Append(" m ;");
        }
        Material mv = AssetDatabase.LoadAssetAtPath<Material>(BiereFuiteOutil.Materiau);
        Transform fuite = racine.transform.Find("Tonneau_Fuite"), chem = racine.transform.Find("Cheminee");
        if (fuite != null) { var c = fuite.gameObject.AddComponent<BiereFuite>(); LierMateriau(c, mv); }
        if (chem != null) { var c = chem.gameObject.AddComponent<FumeeCheminee>(); LierMateriau(c, mv); }
        return sb.ToString();
    }

    static void LierMateriau(Component c, Material m)
    {
        var so = new SerializedObject(c);
        so.FindProperty("materiau").objectReferenceValue = m;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Quad(string nom, Transform parent, Mesh quad, Material mat, Vector3 pos, Quaternion rot, float l, float h)
    {
        var g = new GameObject(nom); g.transform.SetParent(parent, false);
        g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = new Vector3(l, h, 1f);
        g.AddComponent<MeshFilter>().sharedMesh = quad;
        var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Lettres crème sur fond transparent : découpe alpha (pas de transparence triée), couverture conservée dans les mips
    static void ReglerTextureTexte(string chemin)
    {
        var ti = AssetImporter.GetAtPath(chemin) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.maxTextureSize = 2048;
        ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true;
        ti.mipmapEnabled = true; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.4f;
        ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 8;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
    }

    static Material MateriauEnseigne()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(EnseigneMateriau);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, EnseigneMateriau); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(EnseigneTexture));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        // lettres un peu lumineuses : lisibles à l'ombre du toit (le texte crème sortait gris) et la nuit
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(EnseigneTexture)); m.SetColor("_EmissionColor", new Color(0.75f, 0.75f, 0.75f)); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f); m.EnableKeyword("_ALPHATEST_ON");
        m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Comme la montagne : pas de matériau importé, normales du fichier (lissées par Blender, arêtes dures gardées), tangentes
    // recalculées, maillage non lisible.
    static void ReglerModele(string chemin)
    {
        var imp = AssetImporter.GetAtPath(chemin) as ModelImporter;
        if (imp == null) return;
        bool change = false;
        if (imp.materialImportMode != ModelImporterMaterialImportMode.None) { imp.materialImportMode = ModelImporterMaterialImportMode.None; change = true; }
        if (imp.importNormals != ModelImporterNormals.Import) { imp.importNormals = ModelImporterNormals.Import; change = true; }
        if (imp.importTangents != ModelImporterTangents.CalculateMikk) { imp.importTangents = ModelImporterTangents.CalculateMikk; change = true; }
        if (imp.importAnimation) { imp.importAnimation = false; change = true; }
        if (imp.animationType != ModelImporterAnimationType.None) { imp.animationType = ModelImporterAnimationType.None; change = true; }
        if (imp.importCameras || imp.importLights || imp.importBlendShapes) { imp.importCameras = false; imp.importLights = false; imp.importBlendShapes = false; change = true; }
        if (imp.meshCompression != ModelImporterMeshCompression.Off) { imp.meshCompression = ModelImporterMeshCompression.Off; change = true; }
        if (imp.isReadable) { imp.isReadable = false; change = true; }
        if (imp.generateSecondaryUV) { imp.generateSecondaryUV = false; change = true; }
        if (change) imp.SaveAndReimport();
    }

    static void ReglerTexture(string chemin)
    {
        var ti = AssetImporter.GetAtPath(chemin) as TextureImporter;
        if (ti == null) return;
        if (ti.maxTextureSize == 2048 && ti.sRGBTexture && ti.wrapMode == TextureWrapMode.Clamp && ti.mipmapEnabled
            && ti.textureCompression == TextureImporterCompression.CompressedHQ) return;
        ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.maxTextureSize = 2048;
        ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.filterMode = FilterMode.Bilinear; ti.anisoLevel = 4;
        ti.alphaSource = TextureImporterAlphaSource.None;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
    }

    // URP Lit mat sans spéculaire ni reflets (comme Bavaroise_Texture.mat) ; émission : couleur noire dans l'asset, la nuit
    // CycleJourNuit écrit _EmissionColor par bloc de propriétés sur les rendus de sa liste « maisons » (drapeau GI
    // RealtimeEmissive et mot-clé _EMISSION gardés, voir LanterneAssets.Materiau).
    static Material Materiau(string nom, string texture, string piece)
    {
        string chemin = Dossier + "/" + nom + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, chemin); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dossier + "/" + texture + ".png"));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Dossier + "/" + piece + "_Emission.png"));
        m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.EnableKeyword("_EMISSION");
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }
}
