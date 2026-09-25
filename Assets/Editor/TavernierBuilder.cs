using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tavernier de la taverne (Maison_1_A, Interieur_Taverne) : modèle KayKit Rogue (aucune classe jouable ne le porte),
// texture alternative A, debout derrière le comptoir à Ancre_Villageois_Taverne, face à la salle. Au repos (Idle_A),
// il fait de temps en temps un geste (Interact : il essuie le comptoir, sert). Runtime : VillageoisOccupe. Relancé par
// InterieursBuilder.Construire ; menu seul : Deathless > Niveau > Tavernier.
public static class TavernierBuilder
{
    const string Modele = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Rogue.fbx";
    const string Texture = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Textures/rogue_texture_alt_A.png";
    const string Anims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
    const string Dossier = "Assets/Jeu/Tavernier";

    [MenuItem("Deathless/Niveau/Tavernier")]
    public static void Menu()
    {
        Debug.Log(Poser());
        var v = GameObject.Find("VillageBlockout");
        if (v != null) { EditorSceneManager.MarkSceneDirty(v.scene); EditorSceneManager.SaveScene(v.scene); }
    }

    public static string Poser()
    {
        var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Taverne");
        if (it == null) return "Tavernier : Interieur_Taverne introuvable";
        Transform ancre = null;
        foreach (var t in it.GetComponentsInChildren<Transform>(true)) if (t.name == "Ancre_Villageois_Taverne") ancre = t;
        if (ancre == null) return "Tavernier : ancre introuvable";
        var vieux = it.transform.Find("Tavernier");
        if (vieux != null) Object.DestroyImmediate(vieux.gameObject);
        System.IO.Directory.CreateDirectory(Dossier);

        var racine = new GameObject("Tavernier");
        racine.transform.SetParent(it.transform, false);
        racine.transform.SetPositionAndRotation(ancre.position, ancre.rotation);
        var col = racine.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.9f, 0f); col.height = 1.8f; col.radius = 0.3f;
        var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modele), racine.transform);
        modele.name = "Modele";
        modele.transform.localPosition = Vector3.zero; modele.transform.localRotation = Quaternion.identity; modele.transform.localScale = Vector3.one * 0.8f;
        var mat = Materiau();
        foreach (var smr in modele.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.sharedMaterial = mat;

        AnimationClip repos = null, geste = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Anims))
            if (o is AnimationClip c) { if (c.name == "Idle_A") repos = c; else if (c.name == "Interact") geste = c; }
        string chemin = Dossier + "/Tavernier.controller";
        AssetDatabase.DeleteAsset(chemin);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(chemin);
        var sm = ctrl.layers[0].stateMachine;
        var sRepos = sm.AddState("Repos"); sRepos.motion = repos;
        var sGeste = sm.AddState("Geste"); sGeste.motion = geste;
        sm.defaultState = sRepos;
        var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        if (repos != null) repos.SampleAnimation(modele, 0f);
        var occ = racine.AddComponent<VillageoisOccupe>(); occ.animator = anim;
        EditorUtility.SetDirty(occ);
        return "Tavernier posé derrière le comptoir (Rogue, texture alt A ; repos " + (repos != null) + ", geste " + (geste != null) + ")";
    }

    static Material Materiau()
    {
        string chemin = Dossier + "/Tavernier_Rogue.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null)
        {
            Material src = null;
            foreach (var r in AssetDatabase.LoadAssetAtPath<GameObject>(Modele).GetComponentsInChildren<Renderer>(true)) { src = r.sharedMaterial; break; }
            m = new Material(src);
            AssetDatabase.CreateAsset(m, chemin);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Texture);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        EditorUtility.SetDirty(m);
        return m;
    }
}
