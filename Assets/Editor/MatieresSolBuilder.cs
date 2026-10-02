#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Deathless.Jeu;

/// Matières du sol et dalles de la carte (02/10/2026) : pose, sur la scène ouverte (VillageBlockout : Village.unity ou
/// CarteV5.unity), les marqueurs MatiereSol qui disent aux bruits de pas (PasMatiere) sur quoi on marche, et des
/// BoxCollider plats sur les dalles `floor_tile*` (anneau, grotte) et `path_*` (sentiers, route), qui n'avaient aucun
/// collider : le héros marchait au niveau du sol, les pieds 5 à 12 cm sous la face des dalles, ce que masquait le
/// flottement de 6 cm du contrôleur (skinWidth) ; depuis qu'il se pose pieds au sol, il doit se tenir sur la face
/// des dalles. Retire aussi l'ancienne rampe invisible des marches du plateau (Nexus/Plateau/Marches_Rampe), remplacée
/// par le déplacement en sous-pas (DeplacementSousPas). Idempotent : à relancer après chaque génération de la carte
/// (VillageBuilder.Build et la carte v5 l'appellent à la fin).
///
/// Règles (chemin relatif à VillageBlockout) :
///   Sol/Sol_Village → palette (teinte du triangle : herbe, terre, sable, galets) ; Sol/Sol_Plane → herbe
///   Nexus/** (plateau, gemme), Maisons/**, Portail/**, Montagne/** (grotte, falaises) → pierre
///   Riviere/Ponts/** → bois ; Riviere/Cascade/** → eau (les gués : ZoneEau, voir PasMatiere)
///   Interieurs/Interieur_Forgeron → pierre (dallage), sauf enclume, seau et barres de fer → métal
///   Interieurs/** → bois (planchers), sauf Perron et Rampe_Perron → pierre
///   Foret/Arbres/** → bois ; Foret/Lande/**, Foret/Pierrier/** → pierre ; dalles → pierre
public static class MatieresSolBuilder
{
    const float EpaisseurDalle = 0.1f;

    [MenuItem("Deathless/Village/Poser les matières du sol et les dalles")]
    public static string Appliquer()
    {
        var rootGo = GameObject.Find("VillageBlockout");
        if (rootGo == null) return "VillageBlockout introuvable dans la scène ouverte";
        Transform root = rootGo.transform;
        int marques = 0, dalles = 0, rampes = 0;

        // 1. Rampe invisible des marches du plateau (01/10/2026) : retirée (déplacement en sous-pas).
        var plateau = root.Find("Nexus/Plateau");
        if (plateau != null)
        {
            var rampe = plateau.Find("Marches_Rampe");
            while (rampe != null) { Object.DestroyImmediate(rampe.gameObject); rampes++; rampe = plateau.Find("Marches_Rampe"); }
        }

        // 2. Dalles : un collider plat à la face de chaque dalle.
        var vus = new HashSet<Transform>();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            string n = mf.name;
            if (!(n.StartsWith("floor_tile") || n.StartsWith("path_")) || mf.sharedMesh == null || !vus.Add(mf.transform)) continue;
            PoserDalle(mf);
            dalles++;
        }

        // 3. Marqueurs de matière sur tous les colliders pleins.
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (c.isTrigger) continue;
            var m = Regle(Chemin(c.transform, root), c.name);
            if (m == null) continue;
            var ms = c.GetComponent<MatiereSol>();
            if (ms == null) ms = c.gameObject.AddComponent<MatiereSol>();
            bool palette = c.name == "Sol_Village";
            int cases = VillageBuilder.GroundPalette.Length;
            if (ms.matiere != m.Value || ms.parPalette != palette || (palette && ms.casesPalette != cases)) { ms.matiere = m.Value; ms.parPalette = palette; ms.casesPalette = cases; EditorUtility.SetDirty(ms); }
            marques++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rootGo.scene);
        string r = "Matières du sol : " + marques + " colliders marqués, " + dalles + " dalles avec collider, " + rampes + " rampe(s) des marches retirée(s) (" + rootGo.scene.name + ")";
        Debug.Log(r);
        return r;
    }

    static string Chemin(Transform t, Transform racine)
    {
        string p = t.name;
        for (var q = t.parent; q != null && q != racine; q = q.parent) p = q.name + "/" + p;
        return p;
    }

    /// Matière d'un collider d'après son chemin sous VillageBlockout (null : sans marqueur, compte comme pierre).
    public static Matiere? Regle(string chemin, string nom)
    {
        if (chemin.StartsWith("Sol/")) return Matiere.Herbe;
        if (chemin.StartsWith("Nexus/") || chemin.StartsWith("Maisons/") || chemin.StartsWith("Portail/") || chemin.StartsWith("Montagne/")) return Matiere.Pierre;
        if (chemin.StartsWith("Riviere/Ponts/")) return Matiere.Bois;
        if (chemin.StartsWith("Riviere/Cascade")) return Matiere.Eau;
        if (chemin.StartsWith("Interieurs/"))
        {
            string l = nom.ToLowerInvariant();
            if (chemin.StartsWith("Interieurs/Interieur_Forgeron/"))
                return l.Contains("enclume") || l.Contains("metal") || l.Contains("iron") || l.Contains("fer") ? Matiere.Metal : Matiere.Pierre;
            return nom == "Perron" || nom == "Rampe_Perron" ? Matiere.Pierre : Matiere.Bois;
        }
        if (chemin.StartsWith("Foret/Arbres/")) return Matiere.Bois;
        if (chemin.StartsWith("Foret/Lande/") || chemin.StartsWith("Foret/Pierrier/")) return Matiere.Pierre;
        return null;
    }

    // ------------------------------------------------------------------ dalles

    static readonly Dictionary<Mesh, Vector4> s_Boites = new Dictionary<Mesh, Vector4>();   // xmin, xmax, zmin, zmax de la face plate (repère du maillage)
    static readonly Dictionary<Mesh, float> s_Faces = new Dictionary<Mesh, float>();

    static void PoserDalle(MeshFilter mf)
    {
        var mesh = mf.sharedMesh;
        if (!s_Faces.ContainsKey(mesh)) AnalyserDalle(mesh);
        float haut = s_Faces[mesh];
        Vector4 b = s_Boites[mesh];
        var bc = mf.GetComponent<BoxCollider>();
        if (bc == null) bc = mf.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = false;
        bc.size = new Vector3(b.y - b.x, EpaisseurDalle, b.w - b.z);
        bc.center = new Vector3((b.x + b.y) * 0.5f, haut - EpaisseurDalle * 0.5f, (b.z + b.w) * 0.5f);
        if (mf.GetComponent<MatiereSol>() == null) mf.gameObject.AddComponent<MatiereSol>().matiere = Matiere.Pierre;
    }

    /// Hauteur de la face plate d'une dalle : l'étage le plus haut (à 1 cm près) qui porte au moins 35 % de la surface
    /// des faces tournées vers le haut (les herbes et fissures qui dépassent en sont écartées) ; étendue de ces faces.
    static void AnalyserDalle(Mesh mesh)
    {
        var v = mesh.vertices; var t = mesh.triangles;
        var aire = new Dictionary<int, float>(); float total = 0f;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a); float s = n.magnitude * 0.5f;
            if (s < 1e-9f || n.normalized.y < 0.9f) continue;
            int cle = Mathf.RoundToInt((a.y + b.y + c.y) / 3f * 100f);
            aire.TryGetValue(cle, out float x); aire[cle] = x + s; total += s;
        }
        int meilleur = int.MinValue;
        foreach (var kv in aire) if (kv.Value >= 0.35f * total && kv.Key > meilleur) meilleur = kv.Key;
        if (meilleur == int.MinValue) meilleur = Mathf.RoundToInt(mesh.bounds.max.y * 100f);
        float haut = meilleur / 100f;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.magnitude < 1e-9f || n.normalized.y < 0.9f || Mathf.Abs((a.y + b.y + c.y) / 3f - haut) > 0.02f) continue;
            foreach (var p in new[] { a, b, c }) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z); }
        }
        if (x0 > x1) { x0 = mesh.bounds.min.x; x1 = mesh.bounds.max.x; z0 = mesh.bounds.min.z; z1 = mesh.bounds.max.z; }
        s_Faces[mesh] = haut;
        s_Boites[mesh] = new Vector4(x0, x1, z0, z1);
    }
}
#endif
