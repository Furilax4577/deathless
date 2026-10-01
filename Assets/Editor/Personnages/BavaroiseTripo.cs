using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Bavaroise, version 4 (01/10/2026) : modèle Tripo (image Grok v2 « facettage dense », vues de face et de dos, 8 pièces
// texturées, ArtSources/References/Personnages/bavaroise_v4_tripo/) passé par la chaîne Blender
// ArtSources/Personnages/Bavaroise/bavaroise_v4_pipeline.py : décimation par pièce (~11 800 triangles), normales lissées
// (arêtes dures au-delà de 60°, normales pondérées par l'aire), atlas de texture cuit depuis les 8 textures Tripo sur les UV
// Tripo (une tuile par pièce, 2048²), nœud du tablier présent dans le maillage, armature Rig_Medium du Knight
// (Adventurers 2.0) gardée telle quelle et poids automatiques corrigés par règles géométriques, chope modélisée (v3).
// La v3 (bavaroise_pipeline.py, facettes plates, ~6 500 triangles) reste reproductible.
//
// Ce script ne fait que le montage Unity :
// - réglages d'import des deux FBX (Assets/Art/Bavaroise/Tripo/) : comme le Knight (Generic, sans Avatar), sans clips,
//   sans matériaux importés, normales du fichier (lissées par Blender ; le journal vérifie qu'elles le sont) ;
// - Chope_Bavaroise.prefab (GUID gardé) : racine = repère du socket (anse à l'origine, haut selon +X, corps selon +Z),
//   enfant « Modele » = Chope_Tripo ;
// - Bavaroise.prefab (GUID gardé) : Bavaroise_Tripo dépaqueté, matériau Bavaroise_Texture.mat (URP Lit mat, texture cuite
//   Tripo/Bavaroise_Texture.png ; repli Bavaroise.mat, couleurs de sommet), chopes en Bavaroise.mat (Deathless/VertexColorLit),
//   Animator + Bavaroise.controller (vérification), une chope sous chaque handslot (style DeuxChopes) ;
// - vérification : chemins, positions et rotations de repos des os comparés à ceux du Knight (les clips KayKit se lient
//   par chemin et contiennent des courbes de position).
public static class BavaroiseTripo
{
    public const string Dossier = "Assets/Art/Bavaroise";
    public const string CheminPrefab = Dossier + "/Bavaroise.prefab";
    public const string CheminChope = Dossier + "/Chope_Bavaroise.prefab";
    public const string FbxCorps = Dossier + "/Tripo/Bavaroise_Tripo.fbx";
    public const string FbxChope = Dossier + "/Tripo/Chope_Tripo.fbx";
    const string Knight = "Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Characters/fbx/Knight.fbx";
    public const string Anims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";

    // Rotations des chopes dans les sockets (reprises de la v2) : corps de la chope vers l'avant-extérieur, anse vers le
    // corps ; le socket gauche est en miroir (demi-tour autour de Z pour garder la mousse en haut).
    public static readonly Vector3 RotationChopeDroite = new Vector3(-50f, 0f, 0f);
    public static readonly Vector3 RotationChopeGauche = (Quaternion.Euler(0f, 0f, 180f) * Quaternion.Euler(50f, 0f, 0f)).eulerAngles;

    [MenuItem("Deathless/Personnages/Bavaroise (Tripo)")]
    public static void Menu() { Debug.Log(Construire()); }

    public static string Construire()
    {
        var journal = new System.Text.StringBuilder();
        if (!ReglerImport(FbxCorps, true) | !ReglerImport(FbxChope, false))
            journal.Append("réglages d'import appliqués ; ");
        var modele = AssetDatabase.LoadAssetAtPath<GameObject>(FbxCorps);
        var modeleChope = AssetDatabase.LoadAssetAtPath<GameObject>(FbxChope);
        if (modele == null || modeleChope == null) return "Bavaroise (Tripo) : FBX introuvables (lancer bavaroise_pipeline.py)";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Dossier + "/Bavaroise.mat");
        if (mat == null)
        {
            mat = new Material(Shader.Find("Deathless/VertexColorLit"));
            AssetDatabase.CreateAsset(mat, Dossier + "/Bavaroise.mat");
        }

        // Chope : racine au repère du socket, modèle en enfant (sa transformation d'import est gardée).
        var chope = new GameObject("Chope_Bavaroise");
        var mc = (GameObject)PrefabUtility.InstantiatePrefab(modeleChope);
        PrefabUtility.UnpackPrefabInstance(mc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        mc.name = "Modele";
        mc.transform.SetParent(chope.transform, false);
        foreach (var r in chope.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = mat;
        var bc = Bornes(chope);
        var prefabChope = PrefabUtility.SaveAsPrefabAsset(chope, CheminChope);
        Object.DestroyImmediate(chope);
        journal.Append("chope : centre ").Append(bc.center.ToString("F3")).Append(", taille ").Append(bc.size.ToString("F3")).Append(" ; ");

        // Personnage.
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(modele);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        inst.name = "Bavaroise";
        inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        int tris = 0;
        var matCorps = MateriauTexture();
        int sommets = 0, lisses = 0, nSmr = 0;
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.sharedMaterial = matCorps != null ? matCorps : mat;
            smr.updateWhenOffscreen = false;
            tris += smr.sharedMesh.triangles.Length / 3;
            sommets += smr.sharedMesh.vertexCount;
            lisses += SommetsLisses(smr.sharedMesh);
            nSmr++;
        }
        journal.Append(nSmr).Append(" SkinnedMeshRenderer, ").Append(sommets).Append(" sommets dont ").Append(lisses)
            .Append(" lissés (normale à plus de 5° d'une face voisine ; plats = facettes) ; ");
        var droite = Trouver(inst.transform, "handslot.r");
        var gauche = Trouver(inst.transform, "handslot.l");
        if (droite == null || gauche == null) { Object.DestroyImmediate(inst); return "Bavaroise (Tripo) : sockets handslot introuvables"; }
        var cd = (GameObject)PrefabUtility.InstantiatePrefab(prefabChope, droite);
        cd.name = "Chope_Droite";
        cd.transform.localPosition = Vector3.zero; cd.transform.localRotation = Quaternion.Euler(RotationChopeDroite); cd.transform.localScale = Vector3.one;
        var cg = (GameObject)PrefabUtility.InstantiatePrefab(prefabChope, gauche);
        cg.name = "Chope_Gauche";
        cg.transform.localPosition = Vector3.zero; cg.transform.localRotation = Quaternion.Euler(RotationChopeGauche); cg.transform.localScale = Vector3.one;

        var ctrl = Controleur();
        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.avatar = null;
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        Style(prefabChope, ctrl);

        journal.Append(VerifierOs(inst)).Append(" ; ");
        PrefabUtility.SaveAsPrefabAsset(inst, CheminPrefab);
        Object.DestroyImmediate(inst);
        AssetDatabase.SaveAssets();
        int tch = 0;
        foreach (var mf in prefabChope.GetComponentsInChildren<MeshFilter>(true)) tch += mf.sharedMesh.triangles.Length / 3;
        journal.Append("corps ").Append(tris).Append(" triangles, chope ").Append(tch).Append(" (x2)");
        return "Bavaroise (Tripo) : " + journal;
    }

    // Normales importées : un sommet est « lissé » si sa normale s'écarte de plus de 5° de la normale géométrique d'une de ses
    // faces (sur un maillage à facettes plates, toutes les normales sont celles des faces : 0 lissé).
    static int SommetsLisses(Mesh m)
    {
        var v = m.vertices; var n = m.normals; var t = m.triangles;
        if (n == null || n.Length != v.Length) return 0;
        var lisse = new bool[v.Length];
        for (int i = 0; i < t.Length; i += 3)
        {
            var fn = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).normalized;
            for (int k = 0; k < 3; k++) if (Vector3.Angle(n[t[i + k]], fn) > 5f) lisse[t[i + k]] = true;
        }
        int c = 0; foreach (var b in lisse) if (b) c++;
        return c;
    }

    // Texture cuite par la chaîne Blender (Tripo/Bavaroise_Texture.png) : matériau URP Lit sans brillance ni reflets
    // (aspect mat de la planche) ; le volume vient des normales lissées du FBX. Sans texture : null (couleurs de
    // sommet et Bavaroise.mat).
    public const string Texture = Dossier + "/Tripo/Bavaroise_Texture.png";
    static Material MateriauTexture()
    {
        var ti = AssetImporter.GetAtPath(Texture) as TextureImporter;
        if (ti == null) return null;
        if (ti.maxTextureSize != 2048 || !ti.sRGBTexture || ti.wrapMode != TextureWrapMode.Clamp)
        {
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.maxTextureSize = 2048;
            ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Texture);
        string chemin = Dossier + "/Bavaroise_Texture.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, chemin); }
        m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
        m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Comme le Knight : Generic sans Avatar ; pas de clip ni de matériau importé ; normales du fichier (lissées par Blender,
    // arêtes dures gardées : « Calculate » les recalculerait sans les coutures choisies).
    static bool ReglerImport(string chemin, bool squelette)
    {
        var imp = AssetImporter.GetAtPath(chemin) as ModelImporter;
        if (imp == null) return true;
        bool change = false;
        System.Action<bool> marquer = c => { if (c) change = true; };
        var type = squelette ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        marquer(imp.animationType != type); imp.animationType = type;
        if (squelette) { marquer(imp.avatarSetup != ModelImporterAvatarSetup.NoAvatar); imp.avatarSetup = ModelImporterAvatarSetup.NoAvatar; }
        marquer(imp.importAnimation); imp.importAnimation = false;
        marquer(imp.materialImportMode != ModelImporterMaterialImportMode.None); imp.materialImportMode = ModelImporterMaterialImportMode.None;
        marquer(imp.importNormals != ModelImporterNormals.Import); imp.importNormals = ModelImporterNormals.Import;
        marquer(imp.importBlendShapes); imp.importBlendShapes = false;
        marquer(imp.importCameras); imp.importCameras = false;
        marquer(imp.importLights); imp.importLights = false;
        marquer(imp.importTangents != ModelImporterTangents.None); imp.importTangents = ModelImporterTangents.None;
        if (change) imp.SaveAndReimport();
        return !change;
    }

    // Os de la Bavaroise comparés à ceux du Knight : même chemin, même position et rotation de repos.
    static string VerifierOs(GameObject bav)
    {
        var knight = AssetDatabase.LoadAssetAtPath<GameObject>(Knight);
        var ref_ = new Dictionary<string, Transform>();
        var racineK = knight.transform.Find("Rig_Medium");
        foreach (var t in racineK.GetComponentsInChildren<Transform>(true)) ref_[Chemin(t, knight.transform)] = t;
        var racineB = bav.transform.Find("Rig_Medium");
        if (racineB == null) return "os : Rig_Medium absent !";
        int n = 0, manquants = 0;
        float dp = 0f, dr = 0f;
        string pire = "";
        var vus = new HashSet<string>();
        foreach (var t in racineB.GetComponentsInChildren<Transform>(true))
        {
            string c = Chemin(t, bav.transform);
            vus.Add(c);
            Transform k;
            if (!ref_.TryGetValue(c, out k)) { if (t.GetComponent<Renderer>() == null && !t.name.StartsWith("Chope")) manquants++; continue; }
            n++;
            float p = (t.localPosition - k.localPosition).magnitude, r = Quaternion.Angle(t.localRotation, k.localRotation);
            if (p > dp) dp = p;
            if (r > dr) { dr = r; pire = c; }
        }
        int absents = 0;
        foreach (var c in ref_.Keys) if (!vus.Contains(c)) absents++;
        return "os : " + n + " comparés au Knight, écart max " + (dp * 1000f).ToString("F2") + " mm / " + dr.ToString("F3") + "°"
            + (pire != "" && dr > 0.01f ? " (" + pire + ")" : "") + ", en trop " + manquants + ", absents " + absents
            + ", Rig_Medium " + racineB.localPosition.ToString("F3") + " " + racineB.localEulerAngles.ToString("F1") + " " + racineB.localScale.ToString("F2");
    }

    static string Chemin(Transform t, Transform racine)
    {
        string c = t.name;
        for (var p = t.parent; p != null && p != racine; p = p.parent) c = p.name + "/" + c;
        return c;
    }

    public static Transform Trouver(Transform t, string nom)
    {
        if (t.name == nom) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Trouver(t.GetChild(i), nom);
            if (r != null) return r;
        }
        return null;
    }

    static Bounds Bornes(GameObject racine)
    {
        var b = new Bounds();
        bool premier = true;
        foreach (var mf in racine.GetComponentsInChildren<MeshFilter>(true))
            foreach (var v in mf.sharedMesh.vertices)
            {
                var p = racine.transform.InverseTransformPoint(mf.transform.TransformPoint(v));
                if (premier) { b = new Bounds(p, Vector3.zero); premier = false; } else b.Encapsulate(p);
            }
        return b;
    }

    public static AnimationClip Clip(string fichier, string nom)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Anims + fichier))
        {
            var c = o as AnimationClip;
            if (c != null && c.name == nom) return c;
        }
        return null;
    }

    // Contrôleur minimal de vérification (gardé s'il existe déjà, pour ne pas changer son GUID) : locomotion sur Speed,
    // attaque à deux armes sur le déclencheur Attack.
    static AnimatorController Controleur()
    {
        string chemin = Dossier + "/Bavaroise.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(chemin);
        if (ctrl != null) return ctrl;
        ctrl = AnimatorController.CreateAnimatorControllerAtPath(chemin);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        BlendTree arbre;
        var loco = ctrl.CreateBlendTreeInController("Locomotion", out arbre, 0);
        arbre.blendParameter = "Speed";
        arbre.AddChild(Clip("Rig_Medium_General.fbx", "Idle_A"), 0f);
        arbre.AddChild(Clip("Rig_Medium_MovementBasic.fbx", "Walking_A"), 0.5f);
        arbre.AddChild(Clip("Rig_Medium_MovementBasic.fbx", "Running_A"), 1f);
        sm.defaultState = loco;
        var att = sm.AddState("Attack");
        att.motion = Clip("Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Slice");
        var tr = sm.AddAnyStateTransition(att);
        tr.AddCondition(AnimatorConditionMode.If, 0f, "Attack"); tr.duration = 0.1f; tr.canTransitionToSelf = false;
        var ret = att.AddTransition(loco); ret.hasExitTime = true; ret.exitTime = 0.9f; ret.duration = 0.15f;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static void Style(GameObject chope, RuntimeAnimatorController ctrl)
    {
        string chemin = Dossier + "/DeuxChopes.asset";
        var st = AssetDatabase.LoadAssetAtPath<WeaponStyle>(chemin);
        if (st == null) { st = ScriptableObject.CreateInstance<WeaponStyle>(); AssetDatabase.CreateAsset(st, chemin); }
        st.styleName = "Deux chopes (Bavaroise)";
        st.notes = "Chope_Bavaroise (modèle Chope_Tripo) dans les deux handslots : anse au socket, mousse vers le haut, corps vers "
            + "l'avant-extérieur en Idle_A ; rotation gauche = demi-tour autour de Z (socket en miroir). Modèle à valider par Quentin (pas encore jouable).";
        st.attachments = new[] {
            new WeaponStyle.Attachment { prefab = chope, boneName = "handslot.r", localPosition = Vector3.zero, localEuler = RotationChopeDroite, localScale = 1f },
            new WeaponStyle.Attachment { prefab = chope, boneName = "handslot.l", localPosition = Vector3.zero, localEuler = RotationChopeGauche, localScale = 1f },
        };
        st.idle = Clip("Rig_Medium_General.fbx", "Idle_A");
        st.walk = Clip("Rig_Medium_MovementBasic.fbx", "Walking_A");
        st.run = Clip("Rig_Medium_MovementBasic.fbx", "Running_A");
        st.attacks = new[] { Clip("Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Slice"), Clip("Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Chop") };
        st.controller = ctrl;
        EditorUtility.SetDirty(st);
    }
}
