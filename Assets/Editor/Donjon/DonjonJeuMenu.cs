using Deathless.Donjon;
using Deathless.Jeu;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Donjon dans la scène du village (main) : objet « Donjon » loin du village (DonjonJeu.Origine), générateur sans
/// génération au démarrage (la partie le génère avec la graine du jour), masquage, NavMesh propre, et DonjonJeu
/// (portails, butin, gardiens, rappel). Rien de généré n'est enregistré dans la scène.
public static class DonjonJeuMenu
{
    [MenuItem("Deathless/Donjon/Placer dans le village", false, 60)]
    public static string Placer()
    {
        var jeu = Object.FindFirstObjectByType<DonjonJeu>(FindObjectsInactive.Include);
        GameObject go = jeu != null ? jeu.gameObject : new GameObject("Donjon");
        go.transform.SetPositionAndRotation(DonjonJeu.Origine, Quaternion.identity);
        if (go.GetComponent<DonjonMasquage>() == null) go.AddComponent<DonjonMasquage>();
        if (go.GetComponent<NavMeshSurface>() == null) go.AddComponent<NavMeshSurface>();
        var g = go.GetComponent<DonjonGenerateur>() ?? go.AddComponent<DonjonGenerateur>();
        g.kit = DonjonMenu.Kit();
        g.genererAuDemarrage = false;
        g.construireNavMesh = true;
        if (jeu == null) jeu = go.AddComponent<DonjonJeu>();
        jeu.generateur = g;
        jeu.cadenasGrandCoffre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Cadenas/Prefabs/Cadenas_Or.prefab");
        jeu.cadenasCoffre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Cadenas/Prefabs/Cadenas_Acier.prefab");
        if (jeu.modeleSac == null) jeu.modeleSac = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/decoration/props/sack.fbx");
        Vider(go);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        return "Donjon placé à " + DonjonJeu.Origine + " (kit " + (g.kit != null ? g.kit.name : "absent") + ")";
    }

    /// Retire la construction (générée en édition pour un essai) : la scène n'enregistre jamais le donjon construit.
    [MenuItem("Deathless/Donjon/Vider (avant d'enregistrer)", false, 61)]
    public static void ViderMenu()
    {
        var jeu = Object.FindFirstObjectByType<DonjonJeu>(FindObjectsInactive.Include);
        if (jeu != null) { Vider(jeu.gameObject); EditorSceneManager.MarkSceneDirty(jeu.gameObject.scene); }
    }

    static void Vider(GameObject go)
    {
        var t = go.transform.Find("Genere");
        if (t != null) Object.DestroyImmediate(t.gameObject);
        var s = go.GetComponent<NavMeshSurface>();
        if (s != null) s.RemoveData();
        if (s != null) s.navMeshData = null;
    }
}
