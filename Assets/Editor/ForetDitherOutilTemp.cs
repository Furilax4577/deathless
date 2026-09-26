using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Outil ponctuel (Temps 2, 26/09/2026) : applique le matériau du feuillage estompé (Deathless/ForetDither) aux
// arbres/buissons déjà posés dans la scène ouverte (VillageBlockout/Foret/Arbres et /Lisiere), sans toucher aux
// rochers ni à l'herbe du même dossier (matériau KayKit_Forest.mat inchangé) ni au reste du village. Pas destiné à
// durer comme menu : si VillageBuilder régénère la forêt plus tard, prévoir le même geste (ou le brancher dans
// VillageBuilder/KayKitImportSettings directement).
public static class ForetDitherOutilTemp
{
    const string Racine = "VillageBlockout/Foret";
    const string MateriauPath = "Assets/VFX/GemmeNyxessa/KayKit_Forest_Dither.mat";

    [MenuItem("Deathless/Village/Feuillage dithered (appliquer, Temps 2)")]
    public static void Appliquer()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MateriauPath);
        if (mat == null) { Debug.LogError("Matériau introuvable : " + MateriauPath); return; }
        GameObject foret = GameObject.Find(Racine);
        if (foret == null) { Debug.LogError("Racine introuvable : " + Racine); return; }
        int nb = 0;
        foreach (var r in foret.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.gameObject.name;
            if (!(n.StartsWith("Tree_") || n.StartsWith("Bush_"))) continue;
            Undo.RecordObject(r, "Feuillage dithered");
            r.sharedMaterial = mat;
            nb++;
        }
        EditorUtility.SetDirty(foret);
        EditorSceneManager.MarkSceneDirty(foret.scene);
        Debug.Log("[ForetDither] " + nb + " rendus (arbres/buissons) passés sur " + MateriauPath);
    }
}
