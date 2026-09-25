using System.IO;
using UnityEditor;
using UnityEngine;

// Assets des lanternes du village (voir LanterneLumiere), créés s'ils manquent :
//  - Lanterne_Vitres_Emission.png : carte d'émission 256 × 256, noire sauf la case des vitres de l'atlas
//    halloweenbits_texture, où elle reprend le dégradé des vitres en niveaux de gris (luminance normalisée) : la
//    teinte vient de la couleur de la lanterne (LanterneLumiere), la carte ne donne que le modelé ;
//  - Lanterne_HalloweenBits.mat : copie de KayKit_Halloweenbits.mat (matériau partagé du pack, URP Lit, même atlas) avec
//    cette carte d'émission. Seules les vitres peuvent briller ; le matériau partagé des autres pièces n'est pas touché ;
//  - LanternesReglages.asset : couleur commune des lanternes, scintillement, éclat des vitres.
public static class LanterneAssets
{
    public const string FbxPath = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/lantern_standing.fbx";
    public const string SourcePath = "Assets/Art/Materials/KayKit_Halloweenbits.mat";
    public const string AtlasPath = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/halloweenbits_texture.png";
    public const string EmissionPath = "Assets/Art/Textures/Lanterne_Vitres_Emission.png";
    public const string MateriauPath = "Assets/Art/Materials/Lanterne_HalloweenBits.mat";
    public const string ReglagesPath = "Assets/VFX/_Ambiance/LanternesReglages.asset";

    public static LanternesReglages Reglages()
    {
        LanternesReglages r = AssetDatabase.LoadAssetAtPath<LanternesReglages>(ReglagesPath);
        if (r == null) { r = ScriptableObject.CreateInstance<LanternesReglages>(); AssetDatabase.CreateAsset(r, ReglagesPath); }
        return r;
    }

    public static Texture2D CarteEmission()
    {
        Texture2D atlas = new Texture2D(2, 2);
        atlas.LoadImage(File.ReadAllBytes(AtlasPath));
        const int n = 256;
        Color[] px = new Color[n * n];
        float max = 0.001f;
        for (int pass = 0; pass < 2; pass++)
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector2 uv = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                    if (!LanterneLumiere.UvVitres.Contains(uv)) { px[y * n + x] = Color.black; continue; }
                    float l = atlas.GetPixelBilinear(uv.x, uv.y).grayscale;
                    if (pass == 0) max = Mathf.Max(max, l);
                    else { float g = l / max; px[y * n + x] = new Color(g, g, g); }
                }
        Object.DestroyImmediate(atlas);
        Texture2D c = new Texture2D(n, n, TextureFormat.RGB24, false);
        c.SetPixels(px); c.Apply();
        byte[] png = c.EncodeToPNG();
        Object.DestroyImmediate(c);
        if (!File.Exists(EmissionPath) || !System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(EmissionPath), png))
        {
            File.WriteAllBytes(EmissionPath, png);
            AssetDatabase.ImportAsset(EmissionPath, ImportAssetOptions.ForceUpdate);
        }
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(EmissionPath);
        if (ti.mipmapEnabled || ti.wrapMode != TextureWrapMode.Clamp || ti.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Bilinear; ti.sRGBTexture = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath);
    }

    public static Material Materiau() { return Materiau(MateriauPath); }

    public static Material Materiau(string chemin)
    {
        Texture2D carte = CarteEmission();
        // Source : matériau partagé du pack (KayKitImportSettings le met sur les FBX Halloween Bits), sinon celui du FBX.
        Material source = AssetDatabase.LoadAssetAtPath<Material>(SourcePath);
        if (source == null)
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(FbxPath)) if (o is Material) { source = (Material)o; break; }
        Material m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        bool nouveau = m == null;
        if (nouveau)
        {
            m = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = Path.GetFileNameWithoutExtension(chemin);
        }
        else if (source != null) m.CopyPropertiesFromMaterial(source);   // suit les réglages du matériau du pack
        // Carte d'émission ; couleur d'émission noire dans l'asset (vitres éteintes hors jeu) : LanterneLumiere la pose à
        // chaque image (MaterialPropertyBlock). Drapeau GI RealtimeEmissive obligatoire : URP
        // (BaseShaderGUI.SetMaterialKeywords) ne garde le mot-clé _EMISSION que si globalIlluminationFlags contient
        // AnyEmissive ; avec None il le retire à la première validation du matériau (import, inspecteur), même si la
        // couleur n'est pas noire. Sans GI temps réel dans le projet, ce drapeau ne coûte rien.
        m.SetTexture("_EmissionMap", carte);
        m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.EnableKeyword("_EMISSION");
        if (nouveau)
        {
            // Premier import immédiat (le post-traitement d'URP ajoute la sous-ressource AssetVersion et revalide les
            // mots-clés), puis on repart de l'asset importé.
            AssetDatabase.CreateAsset(m, chemin);
            AssetDatabase.ImportAsset(chemin, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        }
        m.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }
}
