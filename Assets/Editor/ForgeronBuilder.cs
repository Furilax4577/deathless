using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Forgeron de la forge (Maison_4_B, Interieur_Forgeron) : modèle KayKit Barbarian sans le bonnet d'ours
// (Barbarian_BearHat masqué) ni l'écharpe (triangles retirés d'une copie du maillage du corps, voir RetirerEcharpe),
// texture alternative (il ne ressemble pas au Viking des joueurs), marteau RPG Tools dans handslot.r, debout à
// Ancre_Villageois_Forgeron face à l'enclume. Contrôleur Repos (Idle_A) / Frappe (Melee_1H_Attack_Chop) ; l'instant
// d'impact est mesuré ici en échantillonnant le clip (la tête du marteau redescend au dessus de l'enclume), puis le
// forgeron est recalé pour que le marteau tombe au milieu de l'enclume. Runtime : ForgeronForge. Relancé par
// InterieursBuilder.Construire ; menu seul : Deathless > Niveau > Forgeron.
public static class ForgeronBuilder
{
    const string Modele = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Characters/fbx/Barbarian.fbx";
    const string Texture = "Assets/Art/KayKit/KayKit_Adventurers_2.0_EXTRA/Textures/barbarian_texture_alt_B.png";
    const string Marteau = "Assets/Art/KayKit/KayKit_RPGToolsBits_1.0_FREE/Assets/fbx(unity)/hammer.fbx";
    const string Anims = "Assets/Art/KayKit/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";
    const string Dossier = "Assets/Jeu/Forgeron";
    const float Echelle = 0.8f;   // comme les héros (Modele à 0,8)

    [MenuItem("Deathless/Niveau/Forgeron")]
    public static void Menu()
    {
        Debug.Log(Poser());
        var v = GameObject.Find("VillageBlockout");
        if (v != null) { EditorSceneManager.MarkSceneDirty(v.scene); EditorSceneManager.SaveScene(v.scene); }
    }

    public static string Poser()
    {
        var it = GameObject.Find("VillageBlockout/Interieurs/Interieur_Forgeron");
        if (it == null) return "Forgeron : Interieur_Forgeron introuvable (lancer Deathless > Niveau > Intérieurs)";
        Transform ancre = null, enclume = null;
        foreach (var t in it.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Ancre_Villageois_Forgeron") ancre = t;
            else if (enclume == null && t.name.ToLower().StartsWith("anvil")) enclume = t;
        }
        if (ancre == null || enclume == null) return "Forgeron : ancre ou enclume introuvable";
        var vieux = it.transform.Find("Forgeron");
        if (vieux != null) Object.DestroyImmediate(vieux.gameObject);
        System.IO.Directory.CreateDirectory(Dossier);

        // Enclume : dessus (monde) et centre.
        Bounds be = new Bounds(enclume.position, Vector3.zero); bool premier = true;
        foreach (var r in enclume.GetComponentsInChildren<Renderer>()) { if (premier) { be = r.bounds; premier = false; } else be.Encapsulate(r.bounds); }
        Vector3 dessus = new Vector3(be.center.x, be.max.y, be.center.z);
        // Le marteau posé sur l'enclume par InterieursBuilder : c'est maintenant celui du forgeron, dans sa main.
        var large = be; large.Expand(0.3f);
        var poses = new List<GameObject>();
        foreach (var t in it.GetComponentsInChildren<Transform>(true)) if (t.name == "hammer" && large.Contains(t.position)) poses.Add(t.gameObject);
        foreach (var g in poses) Object.DestroyImmediate(g);
        Vector3 face = dessus - ancre.position; face.y = 0f; face.Normalize();

        // Racine (collider, script) et modèle.
        var racine = new GameObject("Forgeron");
        racine.transform.SetParent(it.transform, false);
        racine.transform.SetPositionAndRotation(ancre.position, Quaternion.LookRotation(face));
        var col = racine.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.9f, 0f); col.height = 1.8f; col.radius = 0.32f;
        var modele = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Modele), racine.transform);
        modele.name = "Modele";
        modele.transform.localPosition = Vector3.zero; modele.transform.localRotation = Quaternion.identity; modele.transform.localScale = Vector3.one * Echelle;

        // Sans bonnet d'ours ; sans écharpe ; texture alternative.
        var mat = Materiau();
        string echarpe = "";
        foreach (var smr in modele.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.name == "Barbarian_BearHat") { smr.gameObject.SetActive(false); continue; }
            if (smr.name == "Barbarian_Body") echarpe = RetirerEcharpe(smr);
            smr.sharedMaterial = mat;
        }

        // Marteau dans la main droite ; point de la tête.
        Transform main = null;
        foreach (var t in modele.GetComponentsInChildren<Transform>(true)) if (t.name == "handslot.r") main = t;
        var marteau = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Marteau), main);
        marteau.name = "Marteau";
        marteau.transform.localPosition = Vector3.zero; marteau.transform.localRotation = Quaternion.identity; marteau.transform.localScale = Vector3.one * 0.8f;
        foreach (var c in marteau.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var tete = new GameObject("Tete").transform; tete.SetParent(marteau.transform, false);
        var mf = marteau.GetComponentInChildren<MeshFilter>();
        tete.position = mf.transform.TransformPoint(TeteLocale(mf.sharedMesh));

        // Contrôleur, clips.
        var repos = Clip(Anims + "Rig_Medium_General.fbx", "Idle_A");
        var frappe = Clip(Anims + "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Chop");
        var ctrl = Controleur(repos, frappe);
        var anim = modele.GetComponent<Animator>(); if (anim == null) anim = modele.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        // Impact : premier instant, après le haut de l'élan, où la tête du marteau redescend au dessus de l'enclume.
        float duree = frappe.length, tHaut = 0f, yHaut = float.MinValue;
        var ys = new List<float>(); var ps = new List<Vector3>();
        for (float t = 0f; t <= duree; t += 1f / 120f)
        {
            frappe.SampleAnimation(modele, t);
            Vector3 p = tete.position; ys.Add(p.y); ps.Add(p);
            if (p.y > yHaut) { yHaut = p.y; tHaut = t; }
        }
        int iImpact = -1; float yMin = float.MaxValue; int iMin = 0;
        for (int i = 0; i < ys.Count; i++)
        {
            float t = i / 120f; if (t < tHaut) continue;
            if (ys[i] < yMin) { yMin = ys[i]; iMin = i; }
            if (iImpact < 0 && ys[i] <= dessus.y + 0.04f) iImpact = i;
        }
        if (iImpact < 0) iImpact = iMin;
        float tImpact = iImpact / 120f;
        // Placement : la tête du marteau tombe sur le dessus de l'enclume à l'impact (au milieu, ou un peu vers le forgeron) ;
        // on cherche l'orientation et le point de chute pour lesquels, au repos, le marteau (sommets du maillage) et le
        // corps restent hors de la boîte de l'enclume (collider Meuble_Enclume).
        Vector3 hLocal = racine.transform.InverseTransformPoint(ps[iImpact]); hLocal.y = 0f;
        BoxCollider boite = null;
        foreach (var bc in it.GetComponentsInChildren<BoxCollider>(true)) if (bc.name == "Meuble_Enclume") boite = bc;
        var mfm = marteau.GetComponentInChildren<MeshFilter>(); var vm = mfm.sharedMesh.vertices;
        var enveloppes = new List<MeshCollider>();
        int Dedans()
        {
            int k = 0;
            for (int i = 0; i < vm.Length; i += 2)
            {
                Vector3 w = mfm.transform.TransformPoint(vm[i]);
                foreach (var mc in enveloppes) if ((mc.ClosestPoint(w) - w).sqrMagnitude < 1e-6f) { k++; break; }
            }
            return k;
        }
        float lacet0 = Quaternion.LookRotation(face).eulerAngles.y;
        // Points de chute possibles : seulement sur la table plate de l'enclume (lancer de rayon sur son maillage).
        var mcs = new List<MeshCollider>();
        foreach (var mfe in enclume.GetComponentsInChildren<MeshFilter>())
        {
            var mc = mfe.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mfe.sharedMesh; mcs.Add(mc);
            var env = mfe.gameObject.AddComponent<MeshCollider>(); env.sharedMesh = mfe.sharedMesh; env.convex = true; enveloppes.Add(env);   // enveloppe de l'enclume
        }
        bool SurLaTable(Vector3 pt)
        {
            foreach (var mc in mcs) if (mc.Raycast(new Ray(new Vector3(pt.x, be.max.y + 1f, pt.z), Vector3.down), out var h, 2f) && h.point.y >= be.max.y - 0.025f) return true;
            return false;
        }
        float meilleurNote = float.MaxValue; Vector3 meilleurPos = racine.transform.position; Quaternion meilleurRot = racine.transform.rotation;
        int meilleurDedans = 0; float meilleurRecul = 0f, meilleurD = 0f;
        for (float d = -15f; d <= 15f; d += 5f)
            for (float u = 0.15f; u <= 0.851f; u += 0.1f)
            for (float w = 0.2f; w <= 0.81f; w += 0.2f)
            {
                Quaternion q = Quaternion.Euler(0f, lacet0 + d, 0f);
                Vector3 chute = new Vector3(Mathf.Lerp(be.min.x, be.max.x, u), 0f, Mathf.Lerp(be.min.z, be.max.z, w));
                if (!SurLaTable(chute)) continue;
                float recul = Vector2.Distance(new Vector2(chute.x, chute.z), new Vector2(dessus.x, dessus.z));
                Vector3 pos = new Vector3(chute.x, ancre.position.y, chute.z) - q * hLocal;
                racine.transform.SetPositionAndRotation(pos, q);
                // Tout le geste, du départ jusqu'au contact (le repos est la pose de contact : marteau posé sur l'enclume).
                int dedans = 0;
                for (int k = 0; k < 16; k++) { frappe.SampleAnimation(modele, tImpact * k / 16f - 0.001f); dedans += Dedans(); }
                float corps = 9f;
                foreach (var env in enveloppes) { Vector3 tronc = pos + Vector3.up * (be.max.y - pos.y - 0.1f); Vector3 cp = env.ClosestPoint(tronc); corps = Mathf.Min(corps, Vector2.Distance(new Vector2(cp.x, cp.z), new Vector2(pos.x, pos.z))); }
                float note = dedans * 3f + (corps < 0.25f ? 1000f : 0f) + Mathf.Abs(d) * 1f + Vector3.Distance(pos, ancre.position) * 30f + recul * 30f;
                if (note < meilleurNote) { meilleurNote = note; meilleurPos = pos; meilleurRot = q; meilleurDedans = dedans; meilleurRecul = recul; meilleurD = d; }
            }
        foreach (var mc in mcs) Object.DestroyImmediate(mc);
        foreach (var mc in enveloppes) Object.DestroyImmediate(mc);
        racine.transform.SetPositionAndRotation(meilleurPos, meilleurRot);
        string essais = "lacet " + meilleurD.ToString("+0;-0") + "°, chute à " + meilleurRecul.ToString("0.00") + " m du milieu, geste : "
            + meilleurDedans + " sommets dans l'enclume (16 poses)" + (boite == null ? " (boîte introuvable)" : "");
        float avance = Vector3.Distance(racine.transform.position, ancre.position);
        frappe.SampleAnimation(modele, tImpact);
        Vector3 teteImpact = tete.position;
        repos.SampleAnimation(modele, 0f);

        var forge = racine.AddComponent<ForgeronForge>();
        forge.animator = anim; forge.teteMarteau = tete; forge.pointEnclume = dessus; forge.impact = tImpact / duree;
        EditorUtility.SetDirty(forge);
        return "Forgeron posé : impact à " + tImpact.ToString("F2") + " s / " + duree.ToString("F2") + " s (" + (tImpact / duree).ToString("P0")
            + "), tête du marteau à " + (teteImpact.y - dessus.y).ToString("+0.00;-0.00") + " m du dessus de l'enclume, à "
            + Vector2.Distance(new Vector2(teteImpact.x, teteImpact.z), new Vector2(dessus.x, dessus.z)).ToString("F2") + " m de son milieu ; recalé de "
            + avance.ToString("0.00") + " m de l'ancre, " + essais + " ; " + echarpe;
    }

    static Material Materiau()
    {
        string chemin = Dossier + "/Forgeron_Barbarian.mat";
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

    // Écharpe du Barbarian : dans Barbarian_Body, c'est l'îlot de triangles (composante connexe) qui entoure le cou,
    // au-dessus des épaules. On la retire d'une copie du maillage (Assets/Jeu/Forgeron/Forgeron_Corps.asset) : le
    // maillage KayKit d'origine n'est pas touché. Critère : composante dont le centre est dans la bande du cou
    // (entre 78 % et 92 % de la hauteur du corps, dans l'espace du maillage) et qui fait le tour du cou (large en x et z).
    static string RetirerEcharpe(SkinnedMeshRenderer smr)
    {
        Mesh src = smr.sharedMesh;
        var tris = src.triangles; var v = src.vertices;
        int n = v.Length;
        // Composantes connexes par sommets partagés (positions soudées).
        var parent = new int[n]; for (int i = 0; i < n; i++) parent[i] = i;
        int Racine(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Unir(int a, int b) { a = Racine(a); b = Racine(b); if (a != b) parent[a] = b; }
        var soude = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < n; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
            if (soude.TryGetValue(k, out int j)) Unir(i, j); else soude[k] = i;
        }
        for (int t = 0; t < tris.Length; t += 3) { Unir(tris[t], tris[t + 1]); Unir(tris[t + 1], tris[t + 2]); }
        var boites = new Dictionary<int, Bounds>(); var nbTris = new Dictionary<int, int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int r = Racine(tris[t]);
            Vector3 c = (v[tris[t]] + v[tris[t + 1]] + v[tris[t + 2]]) / 3f;
            if (boites.TryGetValue(r, out var b)) { b.Encapsulate(c); boites[r] = b; nbTris[r]++; } else { boites[r] = new Bounds(c, Vector3.zero); nbTris[r] = 1; }
        }
        Bounds tout = src.bounds;
        var infos = new System.Text.StringBuilder();
        int echarpe = -1; float meilleur = 0f;
        foreach (var kv in boites)
        {
            var b = kv.Value;
            float h = (b.center.y - tout.min.y) / Mathf.Max(0.001f, tout.size.y);
            infos.Append("[" + nbTris[kv.Key] + " tri, h " + h.ToString("F2") + ", l " + b.size.x.ToString("F2") + "x" + b.size.z.ToString("F2") + "] ");
            if (h < 0.7f || h > 0.97f) continue;
            float tour = Mathf.Min(b.size.x / tout.size.x, b.size.z / tout.size.z);
            if (tour > meilleur && nbTris[kv.Key] < tris.Length / 3 * 0.6f) { meilleur = tour; echarpe = kv.Key; }
        }
        if (echarpe < 0) return "écharpe : aucune composante au cou (" + infos + ")";
        var garde = new List<int>(tris.Length);
        for (int t = 0; t < tris.Length; t += 3) if (Racine(tris[t]) != echarpe) { garde.Add(tris[t]); garde.Add(tris[t + 1]); garde.Add(tris[t + 2]); }
        string chemin = Dossier + "/Forgeron_Corps.asset";
        var copie = AssetDatabase.LoadAssetAtPath<Mesh>(chemin);
        if (copie == null) { copie = Object.Instantiate(src); AssetDatabase.CreateAsset(copie, chemin); }
        else { EditorUtility.CopySerialized(src, copie); }
        copie.name = "Forgeron_Corps";
        copie.subMeshCount = 1;
        copie.SetTriangles(garde, 0);
        copie.RecalculateBounds();
        EditorUtility.SetDirty(copie);
        smr.sharedMesh = copie;
        return "écharpe retirée : " + nbTris[echarpe] + " triangles sur " + tris.Length / 3 + " (composantes : " + infos + ")";
    }

    // Tête du marteau : moyenne des sommets les plus éloignés de la poignée (le quart haut le long de l'axe principal).
    static Vector3 TeteLocale(Mesh m)
    {
        var v = m.vertices; Bounds b = m.bounds;
        int axe = b.size.y >= b.size.x && b.size.y >= b.size.z ? 1 : b.size.x >= b.size.z ? 0 : 2;
        float haut = b.max[axe], seuil = b.max[axe] - b.size[axe] * 0.2f;
        Vector3 s = Vector3.zero; int k = 0;
        foreach (var p in v) if (p[axe] >= seuil) { s += p; k++; }
        return k > 0 ? s / k : b.center;
    }

    static AnimationClip Clip(string fbx, string nom)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx)) if (o is AnimationClip c && c.name == nom) return c;
        Debug.LogError("Forgeron : clip introuvable " + nom);
        return null;
    }

    static AnimatorController Controleur(AnimationClip repos, AnimationClip frappe)
    {
        string chemin = Dossier + "/Forgeron.controller";
        AssetDatabase.DeleteAsset(chemin);
        var c = AnimatorController.CreateAnimatorControllerAtPath(chemin);
        var sm = c.layers[0].stateMachine;
        var sRepos = sm.AddState("Repos"); sRepos.motion = repos;
        var sFrappe = sm.AddState("Frappe"); sFrappe.motion = frappe;
        sm.defaultState = sRepos;
        return c;
    }
}
