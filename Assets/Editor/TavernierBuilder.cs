using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tavernière de la taverne (Maison_1_A, Interieur_Taverne) : la Bavaroise (prefab Assets/Art/Bavaroise/Bavaroise.prefab,
// modèle Tripo v4 du 01/10/2026, mains vides depuis le 01/10/2026 ; avant : le Rogue KayKit, texture alternative A), debout derrière le comptoir à Ancre_Villageois_Taverne, face à la salle. Au repos (Idle_A),
// il fait de temps en temps un geste (Interact : il essuie le comptoir, sert). Runtime : VillageoisOccupe. Relancé par
// InterieursBuilder.Construire ; menu seul : Deathless > Niveau > Tavernier.
public static class TavernierBuilder
{
    const string Modele = "Assets/Art/Bavaroise/Bavaroise.prefab";
    const string Anims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/Rig_Medium_General.fbx";
    const string Dossier = "Assets/Jeu/Tavernier";

    [MenuItem("Deathless/Niveau/Tavernier")]
    public static void Menu()
    {
        Debug.Log(Poser());
        var v = GameObject.Find("VillageBlockout");
        if (v != null) { EditorSceneManager.MarkSceneDirty(v.scene); EditorSceneManager.SaveScene(v.scene); }
    }

    /// Tavernière de l'intérieur de l'ancienne carte (Interieur_Taverne, ancre Ancre_Villageois_Taverne).
    public static string Poser()
    {
        var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Taverne");
        if (it == null) return "Tavernier : Interieur_Taverne introuvable";
        Transform ancre = null;
        foreach (var t in it.GetComponentsInChildren<Transform>(true)) if (t.name == "Ancre_Villageois_Taverne") ancre = t;
        if (ancre == null) return "Tavernier : ancre introuvable";
        return Poser(it.transform, ancre);
    }

    /// Tavernière sous `parent` (enfant « Tavernier ») : debout à `ancre` (position et regard), au repos avec un geste de temps en temps.
    /// Sert l'intérieur de l'ancienne carte et le poste extérieur de la carte v5 (VillageBuilder.V5Villageois : devant la porte, derrière
    /// son comptoir de service).
    public static string Poser(Transform parent, Transform ancre)
    {
        var vieux = parent.Find("Tavernier");
        if (vieux != null) Object.DestroyImmediate(vieux.gameObject);
        System.IO.Directory.CreateDirectory(Dossier);

        var racine = new GameObject("Tavernier");
        racine.transform.SetParent(parent, false);
        racine.transform.SetPositionAndRotation(ancre.position, ancre.rotation);
        var col = racine.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.9f, 0f); col.height = 1.8f; col.radius = 0.3f;
        var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modele), racine.transform);
        modele.name = "Modele";
        modele.transform.localPosition = Vector3.zero; modele.transform.localRotation = Quaternion.identity; modele.transform.localScale = Vector3.one * 1.18f;   // Bavaroise v4 1,82 m → ~2,15 m, la taille d'un héros (Paladin 2,18 m) ; la v3 (1,95 m) était à 1,1, le Rogue à 0,8
        // La Bavaroise garde ses matériaux (texture Tripo cuite) : plus de matériau du Rogue.

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
        var anim = modele.GetComponentInChildren<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        if (repos != null) repos.SampleAnimation(anim.gameObject, 0f);
        var occ = racine.AddComponent<VillageoisOccupe>(); occ.animator = anim;
        EditorUtility.SetDirty(occ);
        return "Tavernière posée (Bavaroise ; repos " + (repos != null) + ", geste " + (geste != null) + ")";
    }
}
