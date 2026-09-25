using UnityEditor;
using UnityEngine;

// Réglages d'import automatiques pour les packs KayKit (Assets/Art/KayKit).
// AssetPostprocessor : classe d'éditeur que Unity appelle tout seul à chaque import d'asset ; rien à brancher.
// Elle ne touche jamais aux fichiers des packs, seulement à leurs réglages d'import (.meta).
//
// - Textures : chaque pack repose sur un atlas en dégradé (1024 x 1024). On le garde net : pas de compression,
//   pas de mipmaps (ils feraient baver les dégradés voisins), filtrage bilinéaire.
// - Modèles : les matériaux embarqués dans les FBX sont remplacés par le matériau URP Lit partagé du pack
//   (Assets/Art/Materials/KayKit_<Pack>.mat), retrouvé d'après la texture que le FBX référence.
//   Un seul matériau par atlas = un seul "draw call" possible par lot (SRP Batcher, instanciation GPU).
public class KayKitImportSettings : AssetPostprocessor
{
    private const string PacksRoot = "Assets/Art/KayKit/";
    private const string MaterialsRoot = "Assets/Art/Materials/";

    private static bool IsKayKit(string path)
    {
        return path.StartsWith(PacksRoot);
    }

    // Chemin du matériau partagé pour une texture d'atlas : knight_texture -> KayKit_Knight.mat,
    // dungeon_texture -> KayKit_Dungeon.mat, forest_texture -> KayKit_Forest.mat.
    public static string MaterialPathForTexture(string textureName)
    {
        string shortName = textureName.Replace("_texture", "");
        if (shortName.Length > 0)
            shortName = char.ToUpperInvariant(shortName[0]) + shortName.Substring(1);
        return MaterialsRoot + "KayKit_" + shortName + ".mat";
    }

    private void OnPreprocessTexture()
    {
        if (!IsKayKit(assetPath))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
    }

    private void OnPreprocessModel()
    {
        if (!IsKayKit(assetPath))
            return;

        ModelImporter importer = (ModelImporter)assetImporter;
        // Les FBX "(unity)" des packs sont déjà à l'échelle : 1 unité = 1 m.
        importer.useFileScale = true;
        importer.globalScale = 1f;
        // Pas de fichiers .mat générés à côté des FBX : on ne garde que le matériau partagé (OnAssignMaterialModel).
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        // Les FBX d'animation (Animations/) ne servent que pour leurs clips : pas de matériau du tout.
        importer.materialImportMode = IsAnimationFile(assetPath)
            ? ModelImporterMaterialImportMode.None
            : ModelImporterMaterialImportMode.ImportStandard;

    }

    // Appelé pour chaque clip d'un FBX, une fois lu. Les clips de locomotion et d'attente doivent boucler ;
    // les actions ponctuelles (mort, saut, coup...) non.
    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!IsKayKit(assetPath) || !IsAnimationFile(assetPath))
            return;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = ShouldLoop(clip.name);
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static bool ShouldLoop(string clipName)
    {
        return clipName.StartsWith("Idle_") || clipName.StartsWith("Walking_") || clipName.StartsWith("Running_")
            || clipName == "Jump_Idle" || clipName == "Use_Item";
    }

    private static bool IsAnimationFile(string path)
    {
        return path.Contains("/Animations/");
    }

    // Maillages utilitaires sans texture, livrés tels quels par le pack (ex. Grass_1_Mesh) : matériau par défaut, sans bruit.
    private static bool IsUntexturedHelper(string path)
    {
        return path.EndsWith("_Mesh.fbx");
    }

    // Appelé pour chaque matériau embarqué d'un FBX : on renvoie le matériau partagé correspondant à sa texture.
    // Renvoyer null laisse Unity créer son matériau par défaut (cas d'une texture inconnue : visible dans la Console).
    private Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (!IsKayKit(assetPath) || IsAnimationFile(assetPath))
            return null;

        // Medieval Builder Pack : pas de texture, les couleurs sont dans les sommets → matériau partagé à couleurs par
        // sommet (shader Deathless/VertexColorLit), un seul pour tout le pack.
        if (assetPath.Contains("/KayKit_Medieval_Builder_Pack"))
        {
            Material vc = AssetDatabase.LoadAssetAtPath<Material>(MaterialsRoot + "KayKit_Builder.mat");
            if (vc == null) Debug.LogWarning("KayKit : matériau Assets/Art/Materials/KayKit_Builder.mat absent (" + assetPath + ").");
            return vc;
        }

        Texture texture = material.mainTexture;
        // Texture d'atlas introuvable à côté du FBX (ex. forest_texture.png du pack Forest : son GUID est déjà pris par la
        // copie de Assets/VFX/GemmeNyxessa/, elle n'est donc pas recopiée dans le pack) : on retrouve le matériau partagé
        // par le nom du matériau embarqué (« forest » → KayKit_Forest.mat).
        if (texture == null && !string.IsNullOrEmpty(material.name) && !IsUntexturedHelper(assetPath))
        {
            Material parNom = OnAssignMaterialModelParNom(material.name + "_texture");
            if (parNom != null) return parNom;
        }
        if (texture == null)
        {
            if (!IsUntexturedHelper(assetPath))
                Debug.LogWarning("KayKit : " + assetPath + " : le matériau '" + material.name + "' ne référence aucune texture, matériau par défaut conservé.");
            return null;
        }

        Material partage = OnAssignMaterialModelParNom(texture.name);
        return partage != null ? partage : AvertirIntrouvable(texture.name);
    }

    private Material AvertirIntrouvable(string textureName)
    {
        Debug.LogWarning("KayKit : matériau partagé introuvable " + MaterialPathForTexture(textureName) + " (texture '" + textureName + "', modèle " + assetPath + ").");
        return null;
    }

    private static Material OnAssignMaterialModelParNom(string textureName)
    {
        string materialPath = MaterialPathForTexture(textureName);
        Material shared = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        // Matériau partagé rangé ailleurs dans main (ex. KayKit_Forest.mat, copié avec la relique dans
        // Assets/VFX/GemmeNyxessa/ ; même GUID que dans les bacs à sable) : on le cherche par son nom.
        if (shared == null)
        {
            string nom = System.IO.Path.GetFileNameWithoutExtension(materialPath);
            foreach (string guid in AssetDatabase.FindAssets(nom + " t:Material"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == nom) { shared = AssetDatabase.LoadAssetAtPath<Material>(p); break; }
            }
        }
        return shared;
    }
}
