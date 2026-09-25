using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Sentiers de pierre du village vers les 3 zones d'apparition des squelettes (Quentin, 26/09/2026) : dalles jointives et
// entières à la sortie du village (comme les allées pavées), puis de plus en plus espacées, cassées, de travers,
// enfoncées et envahies d'herbe en s'enfonçant dans la forêt, jusqu'à quelques pierres éparses près de la clairière.
// Tracé légèrement sinueux dans le couloir déjà libre d'arbres (VillageBuilder.TrailAxis), qui s'écarte des troncs et des
// rochers ; 2 lanternes par sentier dans la partie entretenue (au sol, puis sur poteau), réglées par LanterneLumiere et
// allumées la nuit par CycleJourNuit. Aucune pièce n'a de collider : le NavMesh (colliders physiques) et le héros ne les
// voient pas. Relançable : Deathless > Village > Sentiers (retire puis refait, puis recuit le NavMesh).
public static class SentiersBuilder
{
    const string Racine = "VillageBlockout";
    const string NomGroupe = "Sentiers_Pierre";
    const string Halloween = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";
    const string Foret = "Assets/Art/KayKit/KayKit_Forest_Nature_Pack_1.0_FREE/Assets/fbx(unity)/";
    const int Graine = 7310;

    static readonly string[] Dalles = { "path_A", "path_B", "path_C", "path_D" };                       // allées du village
    static readonly string[] Cailloux = { "Rock_1_A_Color1", "Rock_1_D_Color1", "Rock_2_A_Color1", "Rock_3_B_Color1" };
    static readonly string[] Herbes = { "Grass_1_A_Color1", "Grass_1_C_Color1", "Grass_2_B_Color1", "Grass_2_D_Color1" };

    [MenuItem("Deathless/Village/Sentiers")]
    public static string Generer()
    {
        GameObject root = GameObject.Find(Racine);
        if (root == null) return "VillageBlockout introuvable";
        Retirer(root);
        var rnd = new System.Random(Graine);
        float R() => (float)rnd.NextDouble();
        Transform groupe = new GameObject(NomGroupe).transform;
        groupe.SetParent(root.transform, false);
        Collider sol = GameObject.Find(Racine + "/Sol/Sol_Village")?.GetComponent<Collider>();
        var dv = Object.FindAnyObjectByType<Deathless.Jeu.DirecteurVagues>();
        var lumieres = new List<Light>(); var flammes = new List<GameObject>();
        Material lanterneMat = LanterneAssets.Materiau();
        LanternesReglages reglages = LanterneAssets.Reglages();
        Material flammeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Lanterne_Flamme.mat");
        int pieces = 0, buissons = 0;
        for (int g = 0; g < 3; g++)
        {
            Transform s = new GameObject("Sentier_" + VillageBuilder.TrailName[g]).transform;
            s.SetParent(groupe, false);
            Vector3 clairiere = dv != null && dv.clairieres != null && g < dv.clairieres.Length ? dv.clairieres[g].position : VillageBuilder.ClearingCenter(g);
            float r0 = VillageBuilder.PaversRadius - 0.3f;
            float r1 = VillageBuilder.RadOf(clairiere) - VillageBuilder.ClearingRadius * 0.55f;
            float phase = R() * 6.28f;
            int rang = 0;
            float r = r0;
            while (r < r1)
            {
                float u = Mathf.Clamp01((r - r0) / (r1 - r0));
                float usure = Mathf.Clamp01((u - 0.22f) / 0.78f);                      // 0 dans la partie entretenue
                // Sinuosité douce dans le couloir libre (± 1,1 m autour de l'axe du couloir), nulle à la sortie du village.
                float lat = Mathf.Sin(r * 0.11f + phase) * 1.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r0, r0 + 8f, r));
                float az = VillageBuilder.TrailAxis(g, r);
                Vector3 droite = VillageBuilder.Right(az);
                Vector3 axe = Ecarter(VillageBuilder.Polar(az, r) + droite * lat, droite, 1.2f);
                // Deux rangées jointives (comme les allées du village), puis une seule en s'enfonçant dans la forêt.
                int rangees = u < 0.3f ? 2 : (u < 0.55f && R() < 0.55f - u) ? 2 : 1;
                for (int k = 0; k < rangees; k++)
                {
                    if (R() < Mathf.Pow(usure, 1.6f) * 0.45f) continue;                  // dalles manquantes plus loin
                    float decal = rangees == 2 ? (k == 0 ? -0.95f : 0.95f) : (R() - 0.5f) * 1.4f * usure;
                    Vector3 p = axe + droite * decal + new Vector3(R() - 0.5f, 0f, R() - 0.5f) * 0.5f * usure;
                    bool caillou = u > 0.8f ? R() < 0.55f : u > 0.55f && R() < 0.15f;
                    string chemin; float echelle, enfonce;
                    if (!caillou)
                    {
                        chemin = Halloween + Dalles[rnd.Next(Dalles.Length)];
                        echelle = Mathf.Lerp(1f, 0.55f, usure) * (1f - 0.12f * R() * usure);
                        enfonce = usure * (0.02f + 0.05f * R());                        // de plus en plus enfoncées
                    }
                    else
                    {
                        chemin = Foret + Cailloux[rnd.Next(Cailloux.Length)];
                        echelle = Mathf.Lerp(0.3f, 0.5f, R());
                        enfonce = 0.04f;
                    }
                    // De travers : quarts de tour réguliers dans la partie entretenue, puis lacet libre et léger roulis.
                    float lacet = usure <= 0f ? 90f * rnd.Next(4) : 90f * rnd.Next(4) + (R() - 0.5f) * 70f * usure;
                    float roulis = (R() - 0.5f) * 10f * usure, tangage = (R() - 0.5f) * 10f * usure;
                    Vector3 pos = PoserSurSol(p, sol) + Vector3.up * (VillageBuilder.PathY + ((rang + k) % 2) * VillageBuilder.PathAlt - enfonce);
                    if (Placer(s, chemin, pos, Quaternion.Euler(tangage, lacet, roulis), echelle) != null) pieces++;
                    // Herbe qui envahit les pierres.
                    int herbes = Mathf.FloorToInt(usure * 2.2f + R() * usure * 1.5f);
                    for (int h = 0; h < herbes; h++)
                    {
                        Vector3 ph = p + new Vector3((R() - 0.5f) * 1.8f, 0f, (R() - 0.5f) * 1.8f);
                        Placer(s, Foret + Herbes[rnd.Next(Herbes.Length)], PoserSurSol(ph, sol), Quaternion.Euler(0f, R() * 360f, 0f), 0.8f + R() * 0.6f);
                    }
                }
                // Pas : 1,9 m (jointives) à la sortie du village, puis de plus en plus espacées.
                r += 1.9f * Mathf.Lerp(1f, 2.2f, Mathf.Pow(usure, 1.2f)) * (1f + 0.25f * (R() - 0.5f) * usure);
                rang++;
            }
            buissons += DegagerBuissons(root.transform, s, g);
            // Lanternes dans la partie entretenue : au sol, puis sur poteau, de part et d'autre du sentier.
            float[] places = { 0.13f, 0.34f };
            for (int k = 0; k < places.Length; k++)
            {
                float rl = Mathf.Lerp(r0, r1, places[k]);
                float az = VillageBuilder.TrailAxis(g, rl);
                float lat = Mathf.Sin(rl * 0.11f + phase) * 1.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r0, r0 + 8f, rl));
                float cote = (k % 2 == 0 ? 1f : -1f) * (g % 2 == 0 ? 1f : -1f);
                Vector3 p = VillageBuilder.Polar(az, rl) + VillageBuilder.Right(az) * (lat + cote * (k == 0 ? 2.5f : 1.9f));
                p = PoserSurSol(Ecarter(p, VillageBuilder.Right(az), 0.8f), sol);
                bool poteau = k == 1;
                float ech = poteau ? 1.1f : VillageBuilder.LanternScale;
                GameObject lan = Placer(s, Halloween + (poteau ? "post_lantern" : "lantern_standing"), p, Quaternion.Euler(0f, az + 180f, 0f), ech);
                if (lan == null) continue;
                lan.name = "Lanterne_" + VillageBuilder.TrailName[g] + "_" + (k + 1);
                GameObject fl = GameObject.CreatePrimitive(PrimitiveType.Sphere); fl.name = "Flamme";
                Object.DestroyImmediate(fl.GetComponent<Collider>());
                fl.transform.SetParent(lan.transform, false); fl.transform.localScale = Vector3.one * 0.14f / ech;
                var fr = fl.GetComponent<MeshRenderer>(); fr.sharedMaterial = flammeMat; fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fl.SetActive(false); flammes.Add(fl);
                GameObject lg = new GameObject("Lumiere"); lg.transform.SetParent(lan.transform, false);
                Light l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = VillageBuilder.LanternRange; l.color = LanterneLumiere.CouleurDefaut; l.intensity = 0f; l.shadows = LightShadows.None; l.enabled = false;
                LanterneLumiere.Configurer(lan, l, fl.transform, lanterneMat, reglages, 2.2f, false, false);
                lumieres.Add(l);
            }
        }
        // Le cycle jour / nuit allume aussi ces lanternes.
        var cycle = root.GetComponentInChildren<CycleJourNuit>();
        if (cycle != null)
        {
            var ls = new List<Light>(); if (cycle.lanternes != null) ls.AddRange(cycle.lanternes); ls.RemoveAll(x => x == null); ls.AddRange(lumieres);
            var fs = new List<GameObject>(); if (cycle.flammes != null) fs.AddRange(cycle.flammes); fs.RemoveAll(x => x == null); fs.AddRange(flammes);
            cycle.lanternes = ls.ToArray(); cycle.flammes = fs.ToArray();
            EditorUtility.SetDirty(cycle);
        }
        string nav = Deathless.EditorTools.JeuBuilder.CuireNavMesh();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        string res = "Sentiers de pierre : 3 sentiers, " + pieces + " pierres, " + lumieres.Count + " lanternes, " + buissons + " buissons écartés ; " + nav;
        Debug.Log(res);
        return res;
    }

    [MenuItem("Deathless/Village/Sentiers - Retirer")]
    public static void RetirerMenu()
    {
        GameObject root = GameObject.Find(Racine);
        if (root == null) return;
        Retirer(root);
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    static void Retirer(GameObject root)
    {
        Transform t = root.transform.Find(NomGroupe);
        if (t == null) return;
        Object.DestroyImmediate(t.gameObject);
        var cycle = root.GetComponentInChildren<CycleJourNuit>();
        if (cycle != null)
        {
            if (cycle.lanternes != null) { var l = new List<Light>(cycle.lanternes); l.RemoveAll(x => x == null); cycle.lanternes = l.ToArray(); }
            if (cycle.flammes != null) { var f = new List<GameObject>(cycle.flammes); f.RemoveAll(x => x == null); cycle.flammes = f.ToArray(); }
            EditorUtility.SetDirty(cycle);
        }
    }

    // Les buissons de lisière (sans collider, on les traverse) qui débordent sur les pierres sont écartés du sentier, par pas de
    // 0,5 m sur le côté où ils se trouvent déjà (idempotent : un buisson déjà dégagé ne bouge plus).
    static int DegagerBuissons(Transform root, Transform sentier, int g)
    {
        Transform lisiere = root.Find("Foret/Lisiere");
        if (lisiere == null) return 0;
        var pierres = new List<Vector3>();
        foreach (Transform c in sentier) if (c.name.StartsWith("path_") || c.name.StartsWith("Rock")) pierres.Add(c.position);
        int n = 0;
        foreach (Transform b in lisiere)
        {
            if (b.GetComponentInChildren<Collider>() != null) continue;
            bool bouge = false;
            for (int essai = 0; essai < 12; essai++)
            {
                Bounds bb = new Bounds(b.position, Vector3.zero);
                foreach (var mr in b.GetComponentsInChildren<Renderer>()) bb.Encapsulate(mr.bounds);
                bb.Expand(new Vector3(0.9f, 0f, 0.9f));
                Vector3? gene = null;
                foreach (var p in pierres) if (p.x > bb.min.x && p.x < bb.max.x && p.z > bb.min.z && p.z < bb.max.z) { gene = p; break; }
                if (gene == null) break;
                Vector3 droite = VillageBuilder.Right(VillageBuilder.TrailAxis(g, VillageBuilder.RadOf(gene.Value)));
                float signe = Vector3.Dot(b.position - gene.Value, droite) >= 0f ? 1f : -1f;
                b.position += droite * signe * 0.5f;
                bouge = true;
            }
            if (bouge) n++;
        }
        return n;
    }

    // Hauteur exacte du sol (maillage à facettes) sous le point.
    static Vector3 PoserSurSol(Vector3 p, Collider sol)
    {
        if (sol != null && sol.Raycast(new Ray(new Vector3(p.x, 50f, p.z), Vector3.down), out RaycastHit h, 100f)) return h.point;
        return new Vector3(p.x, VillageBuilder.GroundHeight(p.x, p.z), p.z);
    }

    // S'écarte latéralement d'un tronc ou d'un rocher trop proche (colliders du décor), sinon garde la place.
    static Vector3 Ecarter(Vector3 p, Vector3 cote, float rayon)
    {
        for (int i = 0; i < 6; i++)
        {
            bool gene = false;
            foreach (var c in Physics.OverlapSphere(new Vector3(p.x, 1f, p.z), rayon, ~0, QueryTriggerInteraction.Ignore))
                if (c.name != "Sol_Village" && !c.isTrigger) { gene = true; break; }
            if (!gene) return p;
            p += cote * ((i % 2 == 0 ? 1f : -1f) * (0.5f + 0.5f * i));
        }
        return p;
    }

    static GameObject Placer(Transform parent, string chemin, Vector3 pos, Quaternion rot, float echelle)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(chemin + ".fbx");
        if (model == null) { Debug.LogWarning("Sentiers : pièce introuvable " + chemin); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        go.name = System.IO.Path.GetFileName(chemin);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one * echelle;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);   // rien pour la physique ni le NavMesh
        return go;
    }
}
