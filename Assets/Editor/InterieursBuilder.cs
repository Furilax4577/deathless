using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Intérieurs des maisons du village (25/09/2026, demande de Quentin : « Crée les intérieurs de maison »).
// Règles : main/Wiki/pages/village.md (druide, mécano, forgeron, sorcier ; les deux autres maisons restent du décor).
// Menu Deathless > Niveau > Intérieurs (relançable sans doublon) et Intérieurs - Retirer.
//
// Les maisons KayKit Hexagon (building_home_A / B) sont des coques fermées. Le générateur les rend creuses sans changer
// leur silhouette : il recalcule leur maillage (Assets/Art/Meshes/Interieurs/Maison_*_Ouverte.asset) en retirant
// - tout ce qui entre dans le volume habitable (plinthes, sablières, cheminée, dessus des murs, sous le toit),
// - le vantail de la porte (panneau, pentures, poignée) et le mur derrière l'arc de la porte.
// L'intérieur est ensuite doublé (murs, pignons, rampants ou plafond, parquet), meublé, éclairé et fermé par des
// colliders simples. Le vantail retiré est reposé ouvert, à plat contre le doublage.
// Repère : groupe VillageBlockout/Interieurs/Interieur_<Nom>, même position et rotation que la maison, échelle 1 :
// x à droite (vu de l'intérieur, face à la porte), z vers la porte et le Nexus, y en haut, en mètres.
// Relancer ce menu après Deathless > Village > Générer (qui recrée les maisons pleines).
public static class InterieursBuilder
{
    // ---------------- Paramètres ----------------
    public static readonly string[] Noms = { "Druide", "Mecano", "Forgeron", "Sorcier", "Taverne" };
    public static readonly string[] Maisons = { "Maison_6_B", "Maison_2_B", "Maison_4_B", "Maison_5_A", "Maison_1_A" };
    public const string Racine = "Interieurs";
    public const float Doublage = 0.12f;      // épaisseur du doublage intérieur (m)
    public const float Jeu = 0.01f;           // écart entre la face extérieure du mur et le doublage (m)
    public const float Parquet = 0.05f;       // épaisseur du parquet (m)
    public const float PassagePorte = 0.04f;  // colliders du chambranle 4 cm plus écartés que l’arc : passage de 1,13 m pour une capsule de 0,92 m (peau comprise)
    public const float HauteurPassage = 2.45f;  // dessous du collider du linteau au-dessus du parquet : 2 m + pas (0,35 m) + peau, sinon le contrôleur accroche le linteau en franchissant la porte (montée de pas de PhysX)
    // Vantail ouvert (retour de Quentin, 25/09/2026) : ramené presque contre le mur de façade intérieur, décollé de 15°
    // (ouvert à 165° depuis la position fermée) : il empiète au minimum sur la pièce et laisse le passage entier.
    // Axe vertical sur l'arête intérieure du chambranle, côté du mur de façade ; deux gonds en métal sombre (nœuds sur
    // l'axe, bras et platine vissée au doublage), pentures forgées sur la face intérieure du battant (côté mur une fois
    // ouvert), pentures et poignée KayKit sur la face extérieure (côté pièce une fois ouvert).
    public const float VantailLargeur = 1.04f, VantailEpaisseur = 0.05f, VantailAngle = 165f;
    public const float AxeReculX = 0.045f;     // axe à 4,5 cm au-delà du chambranle (hors du passage)
    public const float AxeReculZ = 0.08f;      // et à 8 cm du doublage (le battant ouvert à 165° n'y touche pas, pentures comprises)
    public static readonly float[] Gonds = { 0.48f, 1.52f };   // hauteur des gonds au-dessus du plancher
    public const float SeuilPierre = 0.16f;   // A : dessus du seuil de pierre (repère de la maison ; allée pavée : 0,15)

    public const string MeshDir = "Assets/Art/Meshes/Interieurs";
    public const string MatDir = "Assets/Art/Materials";
    const string HexBuildings = "Assets/Art/KayKit/KayKit_Medieval_Hexagon_Pack_1.0_FREE/Assets/fbx(unity)/buildings/blue/";
    const string DungeonDir = "Assets/Art/KayKit/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
    const string ToolsDir = "Assets/Art/KayKit/KayKit_RPGToolsBits_1.0_FREE/Assets/fbx(unity)/";
    const string ResDir = "Assets/Art/KayKit/KayKit_ResourceBits_1.0_FREE/Assets/fbx(unity)/";
    const string WeaponsDir = "Assets/Art/KayKit/KayKit_FantasyWeaponsBits_1.0_FREE/Assets/fbx(unity)/";
    const string HalloweenDir = "Assets/Art/KayKit/KayKit_HalloweenBits_1.0_FREE/Assets/fbx(unity)/";

    // Teintes : texels de l'atlas KayKit_Hexagons_medieval (8 x 4 cases en dégradé vertical), comme les maisons.
    public static readonly Vector2 Enduit = new Vector2(0.6875f, 0.39f);     // crème chaud
    public static readonly Vector2 EnduitClair = new Vector2(0.1875f, 0.9f); // blanc des façades
    public static readonly Vector2 Colombage = new Vector2(0.8125f, 0.86f);  // bois rouge-brun des colombages
    public static readonly Vector2 BoisClair = new Vector2(0.6875f, 0.88f);
    public static readonly Vector2 BoisMoyen = new Vector2(0.8125f, 0.93f);
    public static readonly Vector2 BoisBrun = new Vector2(0.3125f, 0.66f);
    public static readonly Vector2 BoisSombre = new Vector2(0.9375f, 0.56f);
    public static readonly Vector2 BoisNoir = new Vector2(0.9375f, 0.12f);
    public static readonly Vector2 PierreClaire = new Vector2(0.3125f, 0.9f);
    public static readonly Vector2 Pierre = new Vector2(0.3125f, 0.8f);
    public static readonly Vector2 PierreSombre = new Vector2(0.4375f, 0.84f);
    public static readonly Vector2 Suie = new Vector2(0.5625f, 0.8f);
    public static readonly Vector2 Fer = new Vector2(0.4375f, 0.9f);
    public static readonly Vector2 Noir = new Vector2(0.0625f, 0.82f);
    public static readonly Vector2 Olive = new Vector2(0.0625f, 0.32f);   // olive éteint (texels à 0,04 au moins des bords de case : dégradé des boîtes)
    public static readonly Vector2 OliveClair = new Vector2(0.0625f, 0.36f);
    public static readonly Vector2 Paille = new Vector2(0.4375f, 0.68f);
    public static readonly Vector2 Lin = new Vector2(0.6875f, 0.44f);
    public static readonly Vector2 Tan = new Vector2(0.6875f, 0.6f);
    public static readonly Vector2 Taupe = new Vector2(0.8125f, 0.1f);
    public static readonly Vector2 Rouge = new Vector2(0.1875f, 0.12f);
    public static readonly Vector2 RougeClair = new Vector2(0.9375f, 0.42f);
    public static readonly Vector2 Bleu = new Vector2(0.1875f, 0.62f);
    public static readonly Vector2 BleuNuit = new Vector2(0.0625f, 0.06f);
    public static readonly Vector2 Ciel = new Vector2(0.0625f, 0.66f);
    public static readonly Vector2 Or = new Vector2(0.4375f, 0.69f);
    public static readonly Vector2 Ambre = new Vector2(0.3125f, 0.2f);
    public static readonly Vector2 Orange = new Vector2(0.9375f, 0.9f);

    // Gabarit d'un modèle de maison, en unités du maillage (échelle x7,5 dans la scène).
    public class Gabarit
    {
        public string fbx;
        public float murX, murAv, murAr, sol;          // plans extérieurs des murs du rez-de-chaussée, dessus du sol
        public float porteX, panneauZ, cadreZ;         // porte : centre, plan du vantail, devant du chambranle
        public bool rampants; public float faitage;    // A : toit à deux pans (dessous du toit : y = faitage - |x|)
        public float plafond;                          // B : plafond plat (sous l'étage et le toit)
        public float coupeHaut;                        // haut du volume retiré (B)
        public Vector4[] fenetres;                     // (mur 0 -x / 1 +x / 2 avant / 3 arrière, centre le long du mur, bas, haut)
        public Vector3 cheminee0, cheminee1;           // boîte de la cheminée extérieure (collider)
        public bool gondsAGauche;                      // gonds côté -x (faux pour A et B : le battant se rabat vers +x, où la façade a la place)
        public float toitX0, toitY0;                   // A : débord du toit (x, y du dessus à l'égout)
    }

    public static readonly Gabarit A = new Gabarit
    {
        fbx = "building_home_A_blue", murX = 0.245f, murAv = 0.315f, murAr = -0.315f, sol = 0f,
        porteX = 0f, panneauZ = 0.322f, cadreZ = 0.35f, rampants = true, faitage = 0.686f,
        fenetres = new[] { new Vector4(0, -0.095f, 0.119f, 0.294f), new Vector4(0, 0.105f, 0.119f, 0.294f), new Vector4(1, -0.095f, 0.119f, 0.294f), new Vector4(1, 0.105f, 0.119f, 0.294f), new Vector4(2, 0f, 0.449f, 0.594f) },
        cheminee0 = new Vector3(-0.14f, 0f, -0.455f), cheminee1 = new Vector3(0.14f, 0.93f, -0.315f),
    };
    public static readonly Gabarit B = new Gabarit
    {
        fbx = "building_home_B_blue", murX = 0.35f, murAv = 0.21f, murAr = -0.35f, sol = 0.21f,
        porteX = -0.14f, panneauZ = 0.217f, cadreZ = 0.245f, rampants = false, plafond = 0.785f, coupeHaut = 0.785f,
        fenetres = new[] { new Vector4(3, 0.14f, 0.329f, 0.504f), new Vector4(1, -0.07f, 0.329f, 0.504f), new Vector4(2, 0.14f, 0.329f, 0.504f) },
        cheminee0 = new Vector3(-0.28f, 0f, -0.525f), cheminee1 = new Vector3(0f, 1.28f, -0.35f), gondsAGauche = false,
    };
    // Contour arrière de l'arc de la porte (bord du chambranle au plan du vantail), relatif au centre et au seuil.
    public static readonly Vector2[] Arc = { new Vector2(0.07f, 0f), new Vector2(0.07f, 0.238f), new Vector2(0.049f, 0.259f), new Vector2(0.027f, 0.273f), new Vector2(0f, 0.28f), new Vector2(-0.027f, 0.273f), new Vector2(-0.049f, 0.259f), new Vector2(-0.07f, 0.238f), new Vector2(-0.07f, 0f) };

    // ---------------- Géométrie d'une pièce (mètres, repère de l'intérieur) ----------------
    public class Piece
    {
        public Gabarit g; public float s;
        public float xi, zfi, zbi, yF, yC;          // faces intérieures du doublage, dessus du parquet, plafond (B) ou faîtage (A)
        public float porteX0, porteX1, porteH, panneauZ, cadreZ, seuil;
        public float Plafond(float x) { return g.rampants ? (g.faitage * s - 0.16f) - Mathf.Abs(x) : yC; }
        public Vector3 Porte(float dy) { return new Vector3(g.porteX * s, yF + dy, zfi); }
    }
    public static Piece Mesurer(Gabarit g, float s)
    {
        var p = new Piece { g = g, s = s };
        p.xi = g.murX * s - Jeu - Doublage;
        p.zfi = g.murAv * s - Jeu - Doublage;
        p.zbi = g.murAr * s + Jeu + Doublage;
        p.seuil = g.sol * s;
        p.yF = g.sol * s + Parquet;
        p.yC = g.rampants ? g.faitage * s - 0.16f : g.plafond * s - Doublage;
        p.porteX0 = (g.porteX - 0.07f) * s; p.porteX1 = (g.porteX + 0.07f) * s; p.porteH = 0.28f * s;
        p.panneauZ = g.panneauZ * s; p.cadreZ = g.cadreZ * s;
        return p;
    }

    // ---------------- Menus ----------------
    [MenuItem("Deathless/Niveau/Intérieurs")]
    public static void Construire()
    {
        GameObject village = GameObject.Find("VillageBlockout");
        if (village == null) { Debug.LogError("Intérieurs : VillageBlockout introuvable dans la scène active."); return; }
        System.IO.Directory.CreateDirectory(MeshDir);
        Retirer(false);
        Transform racine = new GameObject(Racine).transform; racine.SetParent(village.transform, false);
        var amb = racine.gameObject.AddComponent<InterieursAmbiance>();
        var ctx = new Contexte();
        var rapport = new System.Text.StringBuilder("Intérieurs construits :\n");
        for (int i = 0; i < Noms.Length; i++)
        {
            Transform maison = village.transform.Find("Maisons/" + Maisons[i]);
            if (maison == null) { Debug.LogWarning("Intérieurs : maison introuvable " + Maisons[i]); continue; }
            Gabarit g = Maisons[i].EndsWith("_A") ? A : B;
            float s = maison.localScale.x;
            Mesh ouverte = MaisonOuverte(g, s);
            maison.GetComponent<MeshFilter>().sharedMesh = ouverte;
            BoxCollider bc = maison.GetComponent<BoxCollider>(); if (bc != null) bc.enabled = false;   // gardé (désactivé) pour Vérifier du village
            Transform it = new GameObject("Interieur_" + Noms[i]).transform;
            it.SetParent(racine, false);
            it.SetPositionAndRotation(maison.position, maison.rotation);
            ctx.Debut(it, Noms[i], Mesurer(g, s));
            Coque(ctx);
            Collisions(ctx);
            CouperPaves(ctx, village.transform, Maisons[i]);
            switch (Noms[i])
            {
                case "Druide": MeublerDruide(ctx); break;
                case "Mecano": MeublerMecano(ctx); break;
                case "Forgeron": MeublerForgeron(ctx); break;
                case "Taverne": MeublerTaverne(ctx); break;
                default: MeublerSorcier(ctx); break;
            }
            ctx.Fin();
            rapport.Append("- " + Noms[i] + " dans " + Maisons[i] + " : " + ctx.Props + " objets KayKit, " + ctx.Colliders + " colliders, " + ctx.Lumieres.Count + " lumières\n");
        }
        amb.feux = ctx.Feux.ToArray(); amb.feuxIntensite = ctx.FeuxI.ToArray();
        amb.lampes = ctx.Lampes.ToArray(); amb.lampesIntensite = ctx.LampesI.ToArray();
        amb.vitres = ctx.Vitres.ToArray(); amb.braises = ctx.Braises.ToArray();
        rapport.AppendLine(ForgeronBuilder.Poser());   // forgeron
        rapport.AppendLine(TavernierBuilder.Poser());  // tavernier de la taverne (main, 26/09/2026) de la forge (main, 26/09/2026)
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(village.scene);
        EditorSceneManager.SaveScene(village.scene);
        Debug.Log(rapport.ToString());
    }

    [MenuItem("Deathless/Niveau/Intérieurs - Retirer")]
    public static void RetirerMenu() { Retirer(true); }

    // Retire les intérieurs et rend aux maisons leur maillage plein et leur collider boîte.
    public static void Retirer(bool sauver)
    {
        GameObject village = GameObject.Find("VillageBlockout");
        if (village == null) return;
        for (int k = 0; k < 4; k++) { Transform t = village.transform.Find(Racine); if (t == null) break; Object.DestroyImmediate(t.gameObject); }
        Transform ms = village.transform.Find("Maisons");
        if (ms != null)
            foreach (Transform h in ms)
            {
                MeshFilter mf = h.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                if (!mf.sharedMesh.name.EndsWith("_Ouverte")) continue;
                Gabarit g = h.name.EndsWith("_A") ? A : B;
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HexBuildings + g.fbx + ".fbx");
                if (model != null) mf.sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
                BoxCollider bc = h.GetComponent<BoxCollider>(); if (bc != null) bc.enabled = true;
            }
        // pavés de l'allée coupés au seuil : on rend leur maillage d'origine (surcharge du prefab annulée)
        Transform sentiers = village.transform.Find("Sentiers");
        if (sentiers != null)
            foreach (MeshFilter mf in sentiers.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Interieur_") && PrefabUtility.IsPartOfPrefabInstance(mf))
                    PrefabUtility.RevertObjectOverride(mf, InteractionMode.AutomatedAction);
        if (sauver) { EditorSceneManager.MarkSceneDirty(village.scene); EditorSceneManager.SaveScene(village.scene); }
    }

    // ---------------- Contexte de construction d'un intérieur ----------------
    public class Contexte
    {
        public Transform it, mobilier, collisions, lumieres, props; public string nom; public Piece p;
        public MB coque, meubles, vitres, braises, flammes;
        public int Props, Colliders;
        public List<Light> Lumieres = new List<Light>();
        public List<Light> Feux = new List<Light>(), Lampes = new List<Light>();
        public List<float> FeuxI = new List<float>(), LampesI = new List<float>();
        public List<Renderer> Vitres = new List<Renderer>(), Braises = new List<Renderer>();
        public System.Random rng;
        public void Debut(Transform t, string n, Piece piece)
        {
            it = t; nom = n; p = piece; Props = 0; Colliders = 0; Lumieres.Clear();
            rng = new System.Random(n.GetHashCode() & 0xffff);
            mobilier = Group(it, "Mobilier"); props = Group(mobilier, "KayKit"); collisions = Group(it, "Collisions"); lumieres = Group(it, "Lumieres");
            coque = new MB(); meubles = new MB(); vitres = new MB(); braises = new MB(); flammes = new MB();
        }
        public void Fin()
        {
            Material hex = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/KayKit_Hexagons_medieval.mat");
            Rendu(it, "Coque", coque, hex, true);
            Rendu(mobilier, "Meubles", meubles, hex, false);
            Renderer rv = Rendu(it, "Vitres", vitres, MatVitre(), false); if (rv != null) Vitres.Add(rv);
            Renderer rb = Rendu(mobilier, "Braises", braises, MatBraises(), false); if (rb != null) Braises.Add(rb);
            Rendu(mobilier, "Flammes", flammes, AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Lanterne_Flamme.mat"), false);
            foreach (Transform t in mobilier.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            GameObjectUtility.SetStaticEditorFlags(it.Find("Coque").gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
        Renderer Rendu(Transform parent, string n, MB mb, Material m, bool ombres)
        {
            if (mb.Vide) return null;
            GameObject go = new GameObject(n); go.transform.SetParent(parent, false);
            Mesh mesh = SauverMesh(mb.ToMesh("Interieur_" + nom + "_" + n), MeshDir + "/Interieur_" + nom + "_" + n + ".asset");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = ombres ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;   // doublage fermé : le soleil n'entre que par la porte
            return r;
        }
        // Point du repère de l'intérieur -> monde
        public Vector3 W(Vector3 local) { return it.TransformPoint(local); }
    }

    // ---------------- Maillage de la maison ouverte ----------------
    // Coque KayKit moins le volume habitable, moins le vantail (composantes connexes dans l'arc) et le mur derrière l'arc.
    public static Mesh MaisonOuverte(Gabarit g, float s)
    {
        string path = MeshDir + "/Maison_" + (g == A ? "A" : "B") + "_Ouverte.asset";
        Mesh src = AssetDatabase.LoadAssetAtPath<GameObject>(HexBuildings + g.fbx + ".fbx").GetComponent<MeshFilter>().sharedMesh;
        var tris = LireTriangles(src);
        var vantail = VantailComposantes(src, g);
        var volumes = new List<Plane[]> { VolumeHabitable(g), VolumePorte(g) };
        var outMb = new MB();
        for (int i = 0; i < tris.Count; i++)
        {
            if (vantail.Contains(i)) continue;
            var morceaux = new List<List<V>> { tris[i] };
            foreach (Plane[] vol in volumes)
            {
                var suivants = new List<List<V>>();
                foreach (var m in morceaux) suivants.AddRange(Soustraire(m, vol));
                morceaux = suivants;
            }
            foreach (var m in morceaux) outMb.PolyV(m);
        }
        Mesh mesh = outMb.ToMesh(System.IO.Path.GetFileNameWithoutExtension(path));
        return SauverMesh(mesh, path);
    }

    public static List<List<V>> LireTriangles(Mesh m)
    {
        var res = new List<List<V>>();
        Vector3[] v = m.vertices, n = m.normals; Vector2[] uv = m.uv; int[] t = m.triangles;
        for (int i = 0; i < t.Length; i += 3)
            res.Add(new List<V> { new V(v[t[i]], n[t[i]], uv[t[i]]), new V(v[t[i + 1]], n[t[i + 1]], uv[t[i + 1]]), new V(v[t[i + 2]], n[t[i + 2]], uv[t[i + 2]]) });
        return res;
    }

    // Triangles du vantail : composantes connexes (sommets soudés) entièrement dans l'emprise du panneau de porte
    // (panneau, pentures, poignée) ; le chambranle, plus large, n'en fait pas partie.
    public static HashSet<int> VantailComposantes(Mesh m, Gabarit g)
    {
        Vector3[] v = m.vertices; int[] t = m.triangles;
        var parent = new Dictionary<Vector3Int, Vector3Int>();
        System.Func<Vector3, Vector3Int> key = p => new Vector3Int(Mathf.RoundToInt(p.x * 10000f), Mathf.RoundToInt(p.y * 10000f), Mathf.RoundToInt(p.z * 10000f));
        System.Func<Vector3Int, Vector3Int> find = null;
        find = k => { Vector3Int r; if (!parent.TryGetValue(k, out r)) { parent[k] = k; return k; } if (r == k) return k; Vector3Int f = find(r); parent[k] = f; return f; };
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3Int a = find(key(v[t[i]])), b = find(key(v[t[i + 1]])), c = find(key(v[t[i + 2]]));
            parent[b] = a; parent[find(c)] = a;
        }
        var bornes = new Dictionary<Vector3Int, Bounds>();
        var membres = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3Int r = find(key(v[t[i]]));
            if (!membres.ContainsKey(r)) { membres[r] = new List<int>(); bornes[r] = new Bounds(v[t[i]], Vector3.zero); }
            membres[r].Add(i / 3);
            Bounds b = bornes[r]; b.Encapsulate(v[t[i]]); b.Encapsulate(v[t[i + 1]]); b.Encapsulate(v[t[i + 2]]); bornes[r] = b;
        }
        var res = new HashSet<int>();
        foreach (var kv in membres)
        {
            Bounds b = bornes[kv.Key];
            bool dedans = b.min.x >= g.porteX - 0.0785f && b.max.x <= g.porteX + 0.0785f && b.min.y >= g.sol - 0.001f && b.max.y <= g.sol + 0.3f
                && b.min.z >= g.murAv - 0.002f && b.max.z <= g.cadreZ;
            if (dedans) foreach (int i in kv.Value) res.Add(i);
        }
        return res;
    }

    // Volume habitable (unités du maillage), plans à normale sortante.
    public static Plane[] VolumeHabitable(Gabarit g)
    {
        const float e = 0.0015f;
        var pl = new List<Plane> {
            new Plane(Vector3.right, new Vector3(g.murX - e, 0, 0)), new Plane(Vector3.left, new Vector3(-g.murX + e, 0, 0)),
            new Plane(Vector3.forward, new Vector3(0, 0, g.murAv - e)), new Plane(Vector3.back, new Vector3(0, 0, g.murAr + e)),
            new Plane(Vector3.down, new Vector3(0, g.sol + 0.0005f, 0)) };
        if (g.rampants)
        {
            pl.Add(new Plane(new Vector3(1, 1, 0).normalized, new Vector3(0, g.faitage - e, 0)));
            pl.Add(new Plane(new Vector3(-1, 1, 0).normalized, new Vector3(0, g.faitage - e, 0)));
        }
        else pl.Add(new Plane(Vector3.up, new Vector3(0, g.coupeHaut, 0)));
        return pl.ToArray();
    }

    // Prisme de la porte : arc (réduit de 0,5 mm), du doublage au devant du chambranle.
    public static Plane[] VolumePorte(Gabarit g)
    {
        var pts = new List<Vector2>();
        foreach (Vector2 a in Arc) pts.Add(new Vector2(g.porteX + a.x * 0.993f, g.sol + (a.y <= 0f ? 0.0005f : a.y - 0.0005f)));
        var pl = new List<Plane>();
        Vector2 c = Vector2.zero; foreach (Vector2 q in pts) c += q; c /= pts.Count;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
            Vector2 d = (b - a).normalized; Vector2 nn = new Vector2(d.y, -d.x);
            if (Vector2.Dot(nn, (a + b) / 2f - c) < 0f) nn = -nn;
            pl.Add(new Plane(new Vector3(nn.x, nn.y, 0f), new Vector3(a.x, a.y, 0f)));
        }
        pl.Add(new Plane(Vector3.forward, new Vector3(0, 0, g.cadreZ + 0.002f)));
        pl.Add(new Plane(Vector3.back, new Vector3(0, 0, g.murAv - 0.03f)));
        return pl.ToArray();
    }

    // ---------------- Découpe de polygones ----------------
    public struct V
    {
        public Vector3 p, n; public Vector2 uv;
        public V(Vector3 p, Vector3 n, Vector2 uv) { this.p = p; this.n = n; this.uv = uv; }
        public static V Lerp(V a, V b, float t) { return new V(Vector3.Lerp(a.p, b.p, t), Vector3.Lerp(a.n, b.n, t).normalized, Vector2.Lerp(a.uv, b.uv, t)); }
    }
    // Coupe un polygone convexe par un plan : devant (distance > 0) et derrière.
    public static void Couper(List<V> poly, Plane pl, List<V> devant, List<V> derriere)
    {
        const float eps = 1e-6f;
        for (int i = 0; i < poly.Count; i++)
        {
            V a = poly[i], b = poly[(i + 1) % poly.Count];
            float da = pl.GetDistanceToPoint(a.p), db = pl.GetDistanceToPoint(b.p);
            if (da > eps) devant.Add(a); else if (da < -eps) derriere.Add(a); else { devant.Add(a); derriere.Add(a); }
            if ((da > eps && db < -eps) || (da < -eps && db > eps))
            {
                V x = V.Lerp(a, b, da / (da - db));
                devant.Add(x); derriere.Add(x);
            }
        }
    }
    // Parties d'un polygone convexe hors d'un volume convexe (plans à normale sortante).
    public static List<List<V>> Soustraire(List<V> poly, Plane[] vol)
    {
        var dehors = new List<List<V>>();
        var reste = poly;
        foreach (Plane pl in vol)
        {
            var dv = new List<V>(); var dr = new List<V>();
            Couper(reste, pl, dv, dr);
            if (dv.Count >= 3 && Aire(dv) > 1e-10f) dehors.Add(dv);
            reste = dr;
            if (reste.Count < 3 || Aire(reste) <= 1e-10f) return dehors;
        }
        return dehors;   // le reste est dans le volume : retiré
    }
    static float Aire(List<V> p)
    {
        Vector3 s = Vector3.zero;
        for (int i = 1; i + 1 < p.Count; i++) s += Vector3.Cross(p[i].p - p[0].p, p[i + 1].p - p[0].p);
        return s.magnitude * 0.5f;
    }

    // ---------------- Constructeur de maillage ----------------
    public class MB
    {
        public List<Vector3> v = new List<Vector3>(); public List<Vector3> n = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
        public bool Vide { get { return t.Count == 0; } }
        public float grad = 0.07f;   // dégradé vertical des boîtes (plus clair en haut, comme l'atlas KayKit)

        public void PolyV(List<V> p)
        {
            int b = v.Count;
            foreach (V x in p) { v.Add(x.p); n.Add(x.n); uv.Add(x.uv); }
            for (int i = 1; i + 1 < p.Count; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        }
        // Polygone convexe plan, orienté selon `normale` (ordre des sommets indifférent), une couleur (texel).
        public void Poly(IList<Vector3> p, Vector3 normale, Vector2 texel, float dv = 0f)
        {
            if (p.Count < 3) return;
            Vector3 nn = Vector3.zero;
            for (int i = 1; i + 1 < p.Count; i++) nn += Vector3.Cross(p[i] - p[0], p[i + 1] - p[0]);
            bool inverse = Vector3.Dot(nn, normale) < 0f;
            int b = v.Count;
            float y0 = float.MaxValue, y1 = float.MinValue; foreach (Vector3 q in p) { y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y); }
            for (int i = 0; i < p.Count; i++)
            {
                Vector3 q = p[inverse ? p.Count - 1 - i : i];
                float k = y1 - y0 > 1e-4f ? (q.y - y0) / (y1 - y0) - 0.5f : 0f;
                v.Add(q); n.Add(normale.normalized); uv.Add(new Vector2(texel.x, texel.y + dv + k * grad * Mathf.Abs(1f - Mathf.Abs(normale.normalized.y))));
            }
            for (int i = 1; i + 1 < p.Count; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normale, Vector2 texel) { Poly(new[] { a, b, c, d }, normale, texel); }
        // Boîte (centre, taille, rotation), six faces en dur ; `sans` : masque des faces omises (1 bas, 2 haut, 4 -x, 8 +x, 16 -z, 32 +z).
        public void Box(Vector3 c, Vector3 size, Quaternion r, Vector2 texel, int sans = 0)
        {
            Vector3 h = size * 0.5f;
            Vector3[] q = new Vector3[8];
            for (int i = 0; i < 8; i++) q[i] = c + r * new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            if ((sans & 1) == 0) Poly(new[] { q[0], q[1], q[5], q[4] }, r * Vector3.down, texel, -grad * 0.5f);
            if ((sans & 2) == 0) Poly(new[] { q[2], q[3], q[7], q[6] }, r * Vector3.up, texel, grad * 0.5f);
            if ((sans & 4) == 0) Poly(new[] { q[0], q[2], q[6], q[4] }, r * Vector3.left, texel);
            if ((sans & 8) == 0) Poly(new[] { q[1], q[3], q[7], q[5] }, r * Vector3.right, texel);
            if ((sans & 16) == 0) Poly(new[] { q[0], q[1], q[3], q[2] }, r * Vector3.back, texel);
            if ((sans & 32) == 0) Poly(new[] { q[4], q[5], q[7], q[6] }, r * Vector3.forward, texel);
        }
        public void Box(Vector3 c, Vector3 size, Vector2 texel) { Box(c, size, Quaternion.identity, texel); }
        // Boîte par ses coins (repère de l'intérieur).
        public void BoxMinMax(Vector3 a, Vector3 b, Vector2 texel, int sans = 0) { Box((a + b) * 0.5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z)), Quaternion.identity, texel, sans); }
        // Prisme à `cotes` faces (cylindre low poly), base au centre `c`, rayons bas / haut.
        public void Prisme(Vector3 c, float r0, float r1, float h, int cotes, Vector2 texel, float tour = 0f, bool dessus = true, bool dessous = false)
        {
            var bas = new Vector3[cotes]; var haut = new Vector3[cotes];
            for (int i = 0; i < cotes; i++)
            {
                float a = (tour + 360f * i / cotes) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                bas[i] = c + d * r0; haut[i] = c + d * r1 + Vector3.up * h;
            }
            for (int i = 0; i < cotes; i++)
            {
                int j = (i + 1) % cotes;
                Vector3 mid = (bas[i] + bas[j] + haut[i] + haut[j]) * 0.25f - (c + Vector3.up * h * 0.5f);
                Poly(new[] { bas[i], bas[j], haut[j], haut[i] }, mid, texel);
            }
            if (dessus) Poly(haut, Vector3.up, texel, grad * 0.5f);
            if (dessous) Poly(bas, Vector3.down, texel, -grad * 0.5f);
        }
        // Panneau plan à trous : contour convexe et trous convexes, dans le plan (o, ex, ey), face `normale`.
        public void Panneau(Vector3 o, Vector3 ex, Vector3 ey, IList<Vector2> contour, List<Vector2[]> trous, Vector3 normale, Vector2 texel)
        {
            var poly = new List<V>();
            foreach (Vector2 c in contour) poly.Add(new V(o + ex * c.x + ey * c.y, normale, texel));
            var morceaux = new List<List<V>> { poly };
            if (trous != null)
                foreach (Vector2[] tr in trous)
                {
                    Vector2 ct = Vector2.zero; foreach (Vector2 q in tr) ct += q; ct /= tr.Length;
                    var vol = new Plane[tr.Length];
                    for (int i = 0; i < tr.Length; i++)
                    {
                        Vector2 a = tr[i], b = tr[(i + 1) % tr.Length];
                        Vector2 d = (b - a).normalized; Vector2 nn = new Vector2(d.y, -d.x);
                        if (Vector2.Dot(nn, (a + b) / 2f - ct) < 0f) nn = -nn;
                        vol[i] = new Plane((ex * nn.x + ey * nn.y).normalized, o + ex * a.x + ey * a.y);
                    }
                    var suivants = new List<List<V>>();
                    foreach (var m in morceaux) suivants.AddRange(Soustraire(m, vol));
                    morceaux = suivants;
                }
            float g0 = grad; grad = 0f;   // grands panneaux découpés : teinte unie (un dégradé par morceau ferait des bandes)
            foreach (var m in morceaux)
            {
                var pts = new List<Vector3>(); foreach (V x in m) pts.Add(x.p);
                Poly(pts, normale, texel);
            }
            grad = g0;
        }
        public Mesh ToMesh(string nom)
        {
            var m = new Mesh { name = nom };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
    }

    // Enregistre un maillage à `path` en gardant l'asset (et son GUID) s'il existe déjà.
    public static Mesh SauverMesh(Mesh m, string path)
    {
        Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old == null) { AssetDatabase.CreateAsset(m, path); return m; }
        old.Clear();
        old.indexFormat = m.indexFormat;
        old.SetVertices(m.vertices); old.SetNormals(m.normals); old.SetUVs(0, new List<Vector2>(m.uv)); old.SetTriangles(m.triangles, 0);
        old.RecalculateBounds(); old.name = System.IO.Path.GetFileNameWithoutExtension(path);
        EditorUtility.SetDirty(old);
        Object.DestroyImmediate(m);
        return old;
    }

    // ---------------- Doublage, parquet, porte, fenêtres ----------------
    static void Coque(Contexte c)
    {
        Piece p = c.p; Gabarit g = p.g; MB mb = c.coque; float s = p.s;
        float y0 = p.yF - Parquet;           // bas des panneaux (sous le parquet)
        float yK = p.Plafond(p.xi);          // haut des murs latéraux
        // --- trous des fenêtres (par mur) et de la porte
        var trous = new List<Vector2[]>[4];
        for (int k = 0; k < 4; k++) trous[k] = new List<Vector2[]>();
        foreach (Vector4 f in g.fenetres)
        {
            Vector2 q0, q1; Fenetre(p, f, out q0, out q1);
            trous[(int)f.x].Add(new[] { q0, new Vector2(q1.x, q0.y), q1, new Vector2(q0.x, q1.y) });
        }
        var arc = new Vector2[Arc.Length];
        for (int i = 0; i < Arc.Length; i++) arc[i] = new Vector2((g.porteX + Arc[i].x) * s, p.seuil + Arc[i].y * s - (Arc[i].y <= 0f ? 0.2f : 0f));
        trous[2].Add(arc);
        // --- murs latéraux (repère du panneau : u le long de z, v = y)
        var lat = new[] { new Vector2(p.zbi, y0), new Vector2(p.zfi, y0), new Vector2(p.zfi, yK), new Vector2(p.zbi, yK) };
        mb.Panneau(new Vector3(-p.xi, 0, 0), Vector3.forward, Vector3.up, lat, trous[0], Vector3.right, Enduit);
        mb.Panneau(new Vector3(p.xi, 0, 0), Vector3.forward, Vector3.up, lat, trous[1], Vector3.left, Enduit);
        // --- murs avant et arrière (u = x) : pentagone sous les rampants (A), rectangle (B)
        var face = new List<Vector2> { new Vector2(-p.xi, y0), new Vector2(p.xi, y0), new Vector2(p.xi, yK) };
        if (g.rampants) face.Add(new Vector2(0f, p.Plafond(0f)));
        face.Add(new Vector2(-p.xi, yK));
        mb.Panneau(new Vector3(0, 0, p.zfi), Vector3.right, Vector3.up, face, trous[2], Vector3.back, Enduit);
        mb.Panneau(new Vector3(0, 0, p.zbi), Vector3.right, Vector3.up, face, trous[3], Vector3.forward, Enduit);
        // --- plafond
        if (g.rampants)
        {
            float yR = p.Plafond(0f);
            mb.Quad(new Vector3(p.xi, yK, p.zbi), new Vector3(0, yR, p.zbi), new Vector3(0, yR, p.zfi), new Vector3(p.xi, yK, p.zfi), new Vector3(-1, -1, 0), BoisBrun);
            mb.Quad(new Vector3(-p.xi, yK, p.zbi), new Vector3(0, yR, p.zbi), new Vector3(0, yR, p.zfi), new Vector3(-p.xi, yK, p.zfi), new Vector3(1, -1, 0), BoisBrun);
            // chevrons et faîtière
            Quaternion rd = Quaternion.Euler(0, 0, -45f), rg = Quaternion.Euler(0, 0, 45f);
            float L = p.xi * Mathf.Sqrt(2f);
            for (float z = p.zbi + 0.35f; z < p.zfi - 0.2f; z += 1.05f)
            {
                mb.Box(new Vector3(p.xi * 0.5f, (yK + yR) * 0.5f, z) + new Vector3(-1, -1, 0).normalized * 0.06f, new Vector3(L, 0.12f, 0.12f), rd, Colombage);
                mb.Box(new Vector3(-p.xi * 0.5f, (yK + yR) * 0.5f, z) + new Vector3(1, -1, 0).normalized * 0.06f, new Vector3(L, 0.12f, 0.12f), rg, Colombage);
            }
            mb.BoxMinMax(new Vector3(-0.1f, yR - 0.24f, p.zbi), new Vector3(0.1f, yR + 0.02f, p.zfi), Colombage);
            // sablières le long des murs latéraux
            mb.BoxMinMax(new Vector3(-p.xi, yK - 0.2f, p.zbi), new Vector3(-p.xi + 0.16f, yK + 0.02f, p.zfi), Colombage);
            mb.BoxMinMax(new Vector3(p.xi - 0.16f, yK - 0.2f, p.zbi), new Vector3(p.xi, yK + 0.02f, p.zfi), Colombage);
        }
        else
        {
            mb.Quad(new Vector3(-p.xi, p.yC, p.zbi), new Vector3(p.xi, p.yC, p.zbi), new Vector3(p.xi, p.yC, p.zfi), new Vector3(-p.xi, p.yC, p.zfi), Vector3.down, BoisBrun);
            // solives
            for (float x = -p.xi + 0.55f; x < p.xi - 0.2f; x += 1.0f)
                mb.BoxMinMax(new Vector3(x - 0.08f, p.yC - 0.2f, p.zbi), new Vector3(x + 0.08f, p.yC + 0.01f, p.zfi), Colombage);
            // poutre maîtresse et ceinture de l'étage (hauteur du bandeau extérieur)
            float yb = p.yF + 2.95f;
            mb.BoxMinMax(new Vector3(-p.xi, p.yC - 0.34f, -0.62f), new Vector3(p.xi, p.yC - 0.19f, -0.42f), BoisSombre);
            foreach (int sg in new[] { -1, 1 })
                mb.BoxMinMax(new Vector3(sg * p.xi - (sg > 0 ? 0.1f : 0f), yb, p.zbi), new Vector3(sg * p.xi + (sg < 0 ? 0.1f : 0f), yb + 0.18f, p.zfi), Colombage);
            mb.BoxMinMax(new Vector3(-p.xi, yb, p.zbi), new Vector3(p.xi, yb + 0.18f, p.zbi + 0.1f), Colombage);
            mb.BoxMinMax(new Vector3(-p.xi, yb, p.zfi - 0.1f), new Vector3(p.xi, yb + 0.18f, p.zfi), Colombage);
        }
        // --- poteaux d'angle
        float yTop = yK;
        foreach (int sx in new[] { -1, 1 })
            foreach (float z in new[] { p.zbi + 0.09f, p.zfi - 0.09f })
                mb.BoxMinMax(new Vector3(sx * p.xi - 0.09f, y0, z - 0.09f), new Vector3(sx * p.xi + 0.09f, yTop, z + 0.09f), Colombage);
        // plinthes
        mb.BoxMinMax(new Vector3(-p.xi, p.yF, p.zbi), new Vector3(-p.xi + 0.03f, p.yF + 0.12f, p.zfi), BoisSombre, 1);
        mb.BoxMinMax(new Vector3(p.xi - 0.03f, p.yF, p.zbi), new Vector3(p.xi, p.yF + 0.12f, p.zfi), BoisSombre, 1);
        mb.BoxMinMax(new Vector3(-p.xi, p.yF, p.zbi), new Vector3(p.xi, p.yF + 0.12f, p.zbi + 0.03f), BoisSombre, 1);
        mb.BoxMinMax(new Vector3(-p.xi, p.yF, p.zfi - 0.03f), new Vector3(p.porteX0 - 0.1f, p.yF + 0.12f, p.zfi), BoisSombre, 1);
        mb.BoxMinMax(new Vector3(p.porteX1 + 0.1f, p.yF, p.zfi - 0.03f), new Vector3(p.xi, p.yF + 0.12f, p.zfi), BoisSombre, 1);

        // --- parquet : lames le long de z, joints de 1 cm, trois teintes, fond sombre dessous
        mb.Quad(new Vector3(-p.xi, p.yF - 0.045f, p.zbi), new Vector3(p.xi, p.yF - 0.045f, p.zbi), new Vector3(p.xi, p.yF - 0.045f, p.zfi), new Vector3(-p.xi, p.yF - 0.045f, p.zfi), Vector3.up, BoisNoir);
        Vector2[] lames = { BoisClair, BoisMoyen, BoisBrun, BoisClair };
        const float larg = 0.3f;
        int nl = Mathf.CeilToInt(2f * p.xi / larg);
        float x0 = -p.xi;
        for (int i = 0; i < nl; i++)
        {
            float a = x0 + i * (2f * p.xi / nl), b = x0 + (i + 1) * (2f * p.xi / nl) - 0.012f;
            float coupe = Mathf.Lerp(p.zbi + 0.6f, p.zfi - 0.6f, (float)c.rng.NextDouble());
            float h1 = (float)c.rng.NextDouble() * 0.006f, h2 = (float)c.rng.NextDouble() * 0.006f;
            mb.BoxMinMax(new Vector3(a, p.yF - Parquet, p.zbi), new Vector3(b, p.yF - h1, coupe - 0.006f), lames[c.rng.Next(lames.Length)], 1);
            mb.BoxMinMax(new Vector3(a, p.yF - Parquet, coupe + 0.006f), new Vector3(b, p.yF - h2, p.zfi), lames[c.rng.Next(lames.Length)], 1);
        }
        // seuil : lames jusqu'au devant du chambranle ; maison A (porte de plain-pied) : seuil de pierre sur lequel s'arrête
        // l'allée pavée (les pavés coupés au nu de la façade butent contre lui)
        mb.BoxMinMax(new Vector3(p.porteX0, p.yF - Parquet, p.zfi), new Vector3(p.porteX1, p.yF - 0.004f, p.cadreZ), BoisSombre, 1);
        if (g.rampants)
        {
            // seuil de pierre en biseau : au niveau des pavés dehors, au niveau du plancher au plan du vantail
            float xa = p.porteX0 - 0.05f, xb = p.porteX1 + 0.05f, za = p.panneauZ, zb = p.cadreZ + 0.04f, yb = p.yF - Parquet;
            Vector3 A0 = new Vector3(xa, p.yF + 0.004f, za), A1 = new Vector3(xb, p.yF + 0.004f, za), B0 = new Vector3(xa, SeuilPierre, zb), B1 = new Vector3(xb, SeuilPierre, zb);
            mb.Quad(A0, A1, B1, B0, new Vector3(0, 1, (SeuilPierre - p.yF) / (zb - za)), Pierre);
            mb.Quad(B0, B1, new Vector3(xb, yb, zb), new Vector3(xa, yb, zb), Vector3.forward, Pierre);
            mb.Poly(new[] { A0, B0, new Vector3(xa, yb, zb), new Vector3(xa, yb, za) }, Vector3.left, Pierre);
            mb.Poly(new[] { A1, B1, new Vector3(xb, yb, zb), new Vector3(xb, yb, za) }, Vector3.right, Pierre);
        }

        // --- embrasure de la porte (arc extrudé du plan du vantail au doublage) et vantail ouvert
        for (int i = 0; i + 1 < Arc.Length; i++)
        {
            Vector2 a2 = arc[i], b2 = arc[i + 1];
            if (i == 0) a2.y = p.yF - 0.004f; if (i + 1 == Arc.Length - 1) b2.y = p.yF - 0.004f;
            Vector3 a = new Vector3(a2.x, a2.y, p.zfi), b = new Vector3(b2.x, b2.y, p.zfi);
            Vector3 mid = (a + b) * 0.5f; Vector3 axe = new Vector3(g.porteX * s, p.seuil + 0.12f * s, mid.z);
            Vector3 nrm = axe - mid; nrm.z = 0f;
            mb.Quad(a, b, new Vector3(b.x, b.y, p.panneauZ), new Vector3(a.x, a.y, p.panneauZ), nrm, Colombage);
        }
        Vantail(c);
        // --- fenêtres : embrasure, vitre (maillage à part, émissive), cadre et appui
        foreach (Vector4 f in g.fenetres) FenetreInterieure(c, f);
    }

    // Rectangle de la vitre intérieure dans le repère du panneau du mur (u, y), en mètres.
    static void Fenetre(Piece p, Vector4 f, out Vector2 q0, out Vector2 q1)
    {
        float s = p.s; float largeur = f.w - f.z > 0.12f ? 0.085f * s : 0.075f * s;
        float c = f.y * s;
        float bas = f.z * s + 0.024f * s, haut = f.w * s - 0.026f * s;
        q0 = new Vector2(c - largeur / 2f, bas); q1 = new Vector2(c + largeur / 2f, haut);
    }
    static void FenetreInterieure(Contexte c, Vector4 f)
    {
        Piece p = c.p; MB mb = c.coque; Vector2 q0, q1; Fenetre(p, f, out q0, out q1);
        int mur = (int)f.x;
        // repère : o = point du plan intérieur, u = axe le long du mur, n = normale vers la pièce, d = profondeur (vers l'extérieur)
        Vector3 o, u, n;
        if (mur == 0) { o = new Vector3(-p.xi, 0, 0); u = Vector3.forward; n = Vector3.right; }
        else if (mur == 1) { o = new Vector3(p.xi, 0, 0); u = Vector3.forward; n = Vector3.left; }
        else if (mur == 2) { o = new Vector3(0, 0, p.zfi); u = Vector3.right; n = Vector3.back; }
        else { o = new Vector3(0, 0, p.zbi); u = Vector3.right; n = Vector3.forward; }
        Vector3 d = -n * (Doublage + Jeu * 0.5f);
        System.Func<float, float, Vector3> P = (a, y) => o + u * a + Vector3.up * y;
        Vector3 A0 = P(q0.x, q0.y), A1 = P(q1.x, q0.y), A2 = P(q1.x, q1.y), A3 = P(q0.x, q1.y);
        // embrasure
        mb.Quad(A0, A1, A1 + d, A0 + d, Vector3.up, Enduit);
        mb.Quad(A3, A2, A2 + d, A3 + d, Vector3.down, Enduit);
        mb.Quad(A0, A3, A3 + d, A0 + d, u, Enduit);
        mb.Quad(A1, A2, A2 + d, A1 + d, -u, Enduit);
        // vitre (face vers la pièce) et croisillon
        c.vitres.Quad(A0 + d, A1 + d, A2 + d, A3 + d, n, Ciel);
        Vector3 dm = d * 0.8f;
        float w = q1.x - q0.x, h = q1.y - q0.y;
        mb.Box((A0 + A2) * 0.5f + dm, Vector3.Scale(new Vector3(0.05f, h, 0.05f), Vector3.one) , Quaternion.LookRotation(n), BoisSombre);
        mb.Box((A0 + A2) * 0.5f + dm, new Vector3(w, 0.05f, 0.05f), Quaternion.LookRotation(n) * Quaternion.identity, BoisSombre);
        // cadre sur le doublage et appui
        Quaternion r = Quaternion.LookRotation(n);
        Vector3 cen = (A0 + A2) * 0.5f + n * 0.02f;
        mb.Box(cen + Vector3.up * (h / 2f + 0.05f), new Vector3(w + 0.2f, 0.1f, 0.05f), r, Colombage);
        mb.Box(cen - Vector3.up * (h / 2f + 0.04f) + n * 0.04f, new Vector3(w + 0.24f, 0.06f, 0.13f), r, Colombage);
        mb.Box(cen + u * (w / 2f + 0.05f), new Vector3(0.1f, h + 0.2f, 0.05f), r, Colombage);
        mb.Box(cen - u * (w / 2f + 0.05f), new Vector3(0.1f, h + 0.2f, 0.05f), r, Colombage);
    }

    // Repère du battant ouvert : axe (pied de l'axe, au plancher), u de l'axe vers le bord libre, w épaisseur (de la face
    // extérieure vers la face intérieure), h hauteur au bord (l'arc monte au milieu). Porte fermée : u = -x (gonds à
    // droite) ou +x (gonds à gauche), w = -z. Ouverte de VantailAngle vers l'intérieur.
    public static void Battant(Piece p, out Vector3 axe, out Vector3 u, out Vector3 w, out float h)
    {
        float sg = p.g.gondsAGauche ? -1f : 1f;
        float xj = sg > 0 ? p.porteX1 : p.porteX0;
        axe = new Vector3(xj + sg * AxeReculX, p.yF + 0.01f, p.zfi - AxeReculZ);
        float t = VantailAngle * Mathf.Deg2Rad;
        u = new Vector3(-sg * Mathf.Cos(t), 0f, -Mathf.Sin(t));
        w = new Vector3(sg * Mathf.Sin(t), 0f, -Mathf.Cos(t));
        h = 0.28f * p.s - 0.01f;
    }
    public static readonly Vector2 BoisPorte = new Vector2(0.672f, 0.845f);
    // Vantail : contour de l'arc de la porte (réduit de 1,5 cm en haut), 5 cm d'épaisseur, deux rainures de planches par
    // face ; face extérieure : pentures et poignée KayKit reprises du maillage ; face intérieure : pentures forgées ;
    // gonds (nœuds sur l'axe) et platines fixées au doublage contre le chambranle.
    static void Vantail(Contexte c)
    {
        Piece p = c.p; Gabarit g = p.g; float s = p.s; MB mb = c.coque;
        Vector3 axe, u, w; float hBord; Battant(p, out axe, out u, out w, out hBord);
        float sg = g.gondsAGauche ? -1f : 1f, L = VantailLargeur, e = VantailEpaisseur;
        // (a le long du battant depuis l'axe, b hauteur, cz épaisseur : 0 face extérieure, e face intérieure)
        System.Func<float, float, float, Vector3> P = (a, b, cz) => axe + u * a + Vector3.up * b + w * cz;
        var face = new List<Vector3>(); var dos = new List<Vector3>();
        for (int i = 0; i < Arc.Length; i++)
        {
            float a = (0.07f - Arc[i].x) / 0.14f * L, b = Arc[i].y <= 0f ? 0f : Arc[i].y * s - 0.015f;
            face.Add(P(a, b, 0f)); dos.Add(P(a, b, e));
        }
        mb.grad = 0f;
        mb.Poly(face, -w, BoisPorte); mb.Poly(dos, w, BoisPorte);
        Vector3 cen = P(L * 0.5f, 0.9f, e * 0.5f);
        for (int i = 0; i < face.Count; i++)
        {
            int k = (i + 1) % face.Count;
            Vector3 m = (face[i] + face[k]) * 0.5f; Vector3 nrm = m - cen; nrm -= w * Vector3.Dot(nrm, w);
            mb.Poly(new[] { face[i], face[k], dos[k], dos[i] }, nrm, BoisPorte);
        }
        // rainures de planches (deux par face)
        Quaternion rb = Quaternion.LookRotation(w, Vector3.up);
        foreach (float a in new[] { L / 3f, 2f * L / 3f })
        {
            float hb = 0.27f * s - 0.08f;
            mb.Box(P(a, hb * 0.5f + 0.02f, -0.002f), new Vector3(0.012f, hb, 0.006f), rb, BoisNoir);
            mb.Box(P(a, hb * 0.5f + 0.02f, e + 0.002f), new Vector3(0.012f, hb, 0.006f), rb, BoisNoir);
        }
        mb.grad = 0.07f;
        // face extérieure : pentures et poignée KayKit (composantes du vantail sauf le panneau), largeur ramenée à L
        Mesh src = AssetDatabase.LoadAssetAtPath<GameObject>(HexBuildings + g.fbx + ".fbx").GetComponent<MeshFilter>().sharedMesh;
        var tris = LireTriangles(src);
        float kx = L / (0.154f * s);
        foreach (int i in VantailComposantes(src, g))
        {
            bool panneau = true; foreach (V x in tris[i]) if (Mathf.Abs(x.p.z - g.panneauZ) > 0.0005f) panneau = false;
            if (panneau) continue;
            var poly = new List<V>();
            foreach (V x in tris[i])
            {
                // KayKit : gonds à +x, poignée à -x : a = distance au bord +x (bord côté gonds) ; gonds à gauche : image
                // miroir (la poignée reste du côté du bord libre)
                float a = (g.porteX + 0.077f - x.p.x) * s * kx;
                float b = (x.p.y - g.sol) * s, cz = -(x.p.z - g.panneauZ) * s;
                Vector3 n = -u * x.n.x + Vector3.up * x.n.y - w * x.n.z;
                poly.Add(new V(P(a, b, cz), n.normalized, x.uv));
            }
            if (sg < 0) poly.Reverse();   // la symétrie inverse l'ordre des sommets
            mb.PolyV(poly);
        }
        // face intérieure : pentures forgées (barre clouée, bout en pointe), nœuds des gonds sur l'axe, bras et platines
        Vector2 metal = Suie;
        foreach (float hg in Gonds)
        {
            mb.Box(P(0.36f, hg, e + 0.008f), new Vector3(0.68f, 0.07f, 0.016f), Quaternion.LookRotation(w, Vector3.up) * Quaternion.Euler(0, 0, 0), metal);
            mb.Poly(new[] { P(0.70f, hg - 0.035f, e + 0.016f), P(0.70f, hg + 0.035f, e + 0.016f), P(0.78f, hg, e + 0.016f) }, w, metal);
            for (int k = 0; k < 3; k++) mb.Prisme(P(0.12f + 0.25f * k, hg - 0.012f, e + 0.016f), 0.012f, 0.012f, 0.024f, 4, Fer);   // clous
            // nœud du gond : cylindre vertical centré sur l'axe
            mb.Prisme(new Vector3(axe.x, p.yF + hg - 0.12f, axe.z), 0.036f, 0.036f, 0.24f, 8, metal, 0f, true, true);
            mb.Prisme(new Vector3(axe.x, p.yF + hg + 0.12f, axe.z), 0.014f, 0.01f, 0.035f, 6, metal);
            // bras du gond jusqu'au doublage, platine vissée au doublage contre le chambranle
            Vector3 pl = new Vector3(axe.x, p.yF + hg, p.zfi - 0.006f);
            mb.Box(new Vector3(axe.x, p.yF + hg, (axe.z + pl.z) * 0.5f), new Vector3(0.03f, 0.05f, Mathf.Abs(pl.z - axe.z)), Quaternion.identity, metal);
            mb.Box(pl, new Vector3(0.1f, 0.16f, 0.012f), Quaternion.identity, metal);
            mb.Prisme(pl + new Vector3(-0.03f, 0.04f, -0.006f), 0.01f, 0.01f, 0.012f, 4, Fer);
            mb.Prisme(pl + new Vector3(0.03f, -0.06f, -0.006f), 0.01f, 0.01f, 0.012f, 4, Fer);
        }
    }

    // ---------------- Allée pavée ----------------
    // Les pavés de l'allée maison -> anneau (VillageBuilder) finissaient dans la maison pleine ; porte ouverte, ils
    // dépassaient sur le plancher. On coupe leur maillage au nu extérieur de la façade, et dans l'arc de la porte au devant
    // du chambranle (le seuil de pierre ou le perron prend le relais). Maillage coupé : Interieur_<Nom>_Pave_<k>.asset.
    static void CouperPaves(Contexte c, Transform village, string maison)
    {
        Piece p = c.p; Gabarit g = p.g; float s = p.s;
        string numero = maison.Split('_')[1];
        Transform allee = village.Find("Sentiers/Sentier_Maison_" + numero);
        if (allee == null) return;
        float dx = g.porteX * s, zMur = g.murAv * s + 0.005f;
        var volumes = new List<Plane[]> {
            new[] { new Plane(Vector3.forward, new Vector3(0, 0, zMur)), new Plane(Vector3.right, new Vector3(4f, 0, 0)), new Plane(Vector3.left, new Vector3(-4f, 0, 0)) },
            new[] { new Plane(Vector3.forward, new Vector3(0, 0, p.cadreZ + 0.04f)), new Plane(Vector3.right, new Vector3(dx + 0.62f, 0, 0)), new Plane(Vector3.left, new Vector3(dx - 0.62f, 0, 0)) } };
        int k = 0;
        foreach (MeshFilter mf in allee.GetComponentsInChildren<MeshFilter>(true))
        {
            k++;
            MeshFilter source = PrefabUtility.GetCorrespondingObjectFromSource(mf);
            Mesh m = source != null ? source.sharedMesh : mf.sharedMesh;
            if (m == null || m.subMeshCount != 1) continue;
            Matrix4x4 versIt = c.it.worldToLocalMatrix * mf.transform.localToWorldMatrix, retour = versIt.inverse;
            bool touche = false;
            foreach (Vector3 v in m.vertices) { Vector3 q = versIt.MultiplyPoint3x4(v); if (q.z < p.cadreZ + 0.04f && Mathf.Abs(q.x) < 4f) { touche = true; break; } }
            if (!touche) continue;
            var outMb = new MB();
            foreach (var t in LireTriangles(m))
            {
                var poly = new List<V>();
                foreach (V x in t) poly.Add(new V(versIt.MultiplyPoint3x4(x.p), versIt.MultiplyVector(x.n).normalized, x.uv));
                var morceaux = new List<List<V>> { poly };
                foreach (Plane[] vol in volumes) { var suiv = new List<List<V>>(); foreach (var mm in morceaux) suiv.AddRange(Soustraire(mm, vol)); morceaux = suiv; }
                foreach (var mm in morceaux)
                {
                    for (int i = 0; i < mm.Count; i++) { V x = mm[i]; x.p = retour.MultiplyPoint3x4(x.p); x.n = retour.MultiplyVector(x.n).normalized; mm[i] = x; }
                    outMb.PolyV(mm);
                }
            }
            mf.sharedMesh = SauverMesh(outMb.ToMesh("Interieur_" + c.nom + "_Pave_" + k), MeshDir + "/Interieur_" + c.nom + "_Pave_" + k + ".asset");
        }
    }

    // ---------------- Colliders ----------------
    static void Collisions(Contexte c)
    {
        Piece p = c.p; Gabarit g = p.g; float s = p.s;
        float ext = (g.murX + 0.035f) * s;                       // nu extérieur (colombages compris)
        float zAv = g.cadreZ * s, zAr = (g.murAr - 0.035f) * s;
        float yb = g.rampants ? -0.3f : 0.5f;                    // bas des murs (sous le sol)
        float yh = g.rampants ? 3.45f : 0.84f * s;               // haut des murs (A : sous les rampants ; B : haut de l'étage)
        float pg = p.porteX0 - PassagePorte, pd = p.porteX1 + PassagePorte;
        float zMurAv0 = p.zfi, zMurAv1 = g.murAv * s + 0.03f;
        // sol
        ColBox(c, "Sol", new Vector3(-g.murX * s, p.yF - 0.4f, p.zbi - 0.05f), new Vector3(g.murX * s, p.yF, p.panneauZ));
        if (g.rampants)
        {
            float xa = p.porteX0 - 0.05f, xb = p.porteX1 + 0.05f, za = p.panneauZ, zb = p.cadreZ + 0.04f, ybas = p.yF - 0.4f;
            ColConvexe(c, "Seuil", new List<Vector3> { new Vector3(xa, ybas, za), new Vector3(xb, ybas, za), new Vector3(xa, p.yF, za), new Vector3(xb, p.yF, za),
                new Vector3(xa, ybas, zb), new Vector3(xb, ybas, zb), new Vector3(xa, SeuilPierre, zb), new Vector3(xb, SeuilPierre, zb) }, true);
        }
        else ColBox(c, "Seuil", new Vector3(p.porteX0, p.yF - 0.4f, p.panneauZ), new Vector3(p.porteX1, p.yF, p.cadreZ));
        // battant ouvert
        { Vector3 axe, u, w; float h; Battant(p, out axe, out u, out w, out h);
          ColBoxR(c, "Vantail", axe + u * (VantailLargeur * 0.5f) + w * (VantailEpaisseur * 0.5f) + Vector3.up * (h * 0.5f), new Vector3(VantailLargeur, h, VantailEpaisseur + 0.05f), Quaternion.LookRotation(w, Vector3.up)); }
        // murs
        ColBox(c, "Mur_Gauche", new Vector3(-ext, yb, zAr), new Vector3(-p.xi, yh, zAv));
        ColBox(c, "Mur_Droit", new Vector3(p.xi, yb, zAr), new Vector3(ext, yh, zAv));
        ColBox(c, "Mur_Arriere", new Vector3(-ext, yb, zAr), new Vector3(ext, yh, p.zbi));
        ColBox(c, "Mur_Avant_Gauche", new Vector3(-ext, yb, zMurAv0), new Vector3(pg, yh, zMurAv1));
        ColBox(c, "Mur_Avant_Droit", new Vector3(pd, yb, zMurAv0), new Vector3(ext, yh, zMurAv1));
        ColBox(c, "Linteau", new Vector3(pg, p.yF + HauteurPassage, zMurAv0), new Vector3(pd, yh, zMurAv1));   // un peu au-dessus du haut de l'arc
        // cheminée extérieure
        ColBox(c, "Cheminee", g.cheminee0 * s + new Vector3(0, -0.3f, 0), g.cheminee1 * s + new Vector3(0, 0, -0.02f));
        if (g.rampants)
        {
            // pans du toit : boîtes inclinées (dessous = dessous du doublage), pignons en prismes convexes
            float yR = p.Plafond(0f); float L = 2.85f * Mathf.Sqrt(2f); float ep = 0.55f;
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 dir = new Vector3(sx, -1, 0).normalized, nrm = new Vector3(sx, 1, 0).normalized;
                Vector3 cen = new Vector3(0, yR, 0) + dir * (L * 0.5f) + nrm * (ep * 0.5f - 0.13f);   // 13 cm sous le doublage : chevrons
                ColBoxR(c, sx < 0 ? "Toit_Gauche" : "Toit_Droit", cen, new Vector3(L, ep, 0.385f * s * 2f), Quaternion.Euler(0, 0, sx * -45f));
            }
            ColBox(c, "Faitage", new Vector3(-0.4f, yR - 0.26f, -0.385f * s), new Vector3(0.4f, yR + 0.5f, 0.385f * s));   // sous la faîtière
            float yk = 3.3f;
            foreach (int sz in new[] { -1, 1 })
            {
                float za = sz > 0 ? p.zfi : (g.murAr - 0.03f) * s, zb = sz > 0 ? g.murAv * s + 0.03f : p.zbi;
                var tri = new[] { new Vector3(-ext, yk, 0), new Vector3(ext, yk, 0), new Vector3(0, yk + ext - 0.05f, 0) };
                ColPrisme(c, sz > 0 ? "Pignon_Avant" : "Pignon_Arriere", tri, Mathf.Min(za, zb), Mathf.Max(za, zb));
            }
        }
        else
        {
            // plafond, perron et rampe, pans du toit (faîtage le long de x), pignons
            ColBox(c, "Plafond", new Vector3(-ext, p.yC - 0.36f, zAr), new Vector3(ext, 0.84f * s + 0.05f, zAv));   // sous les solives et la poutre
            ColBox(c, "Perron", new Vector3(-0.42f * s, 0.4f, -0.42f * s), new Vector3(0.42f * s, g.sol * s, 0.35f * s));
            // rampe sur l'escalier du perron (marche visible de 0,39 m : trop haute pour un pas de 0,35 m)
            float zPied = 0.35f * s + 0.925f, yPied = 1.005f, zHaut = 0.35f * s, yHaut = g.sol * s + 0.005f;
            Vector3 a = new Vector3(0, yPied, zPied), b = new Vector3(0, yHaut, zHaut);
            Vector3 dir = (b - a).normalized; Vector3 nrm = Vector3.Cross(dir, Vector3.right).normalized; if (nrm.y < 0) nrm = -nrm;
            float L = (b - a).magnitude + 0.3f, ep = 0.5f;
            Vector3 cen = (a + b) * 0.5f - nrm * (ep * 0.5f) - dir * 0.15f;   // prolongée sous le sol, jamais au-dessus du perron
            ColBoxR(c, "Rampe_Perron", cen + new Vector3(-0.14f * s, 0, 0), new Vector3(0.28f * s, ep, L), Quaternion.LookRotation(dir, nrm));
            float zf = 0.301f * s, zr = -0.441f * s, zc = -0.07f * s, ye = 0.794f * s, yf = 1.19f * s;
            foreach (int sz in new[] { -1, 1 })
            {
                Vector3 pa = new Vector3(0, ye, sz > 0 ? zf : zr), pb = new Vector3(0, yf, zc);
                Vector3 d2 = (pb - pa).normalized; Vector3 n2 = Vector3.Cross(Vector3.right, d2).normalized; if (n2.y < 0) n2 = -n2;
                float L2 = (pb - pa).magnitude + 0.2f; float ep2 = 0.5f;
                ColBoxR(c, sz > 0 ? "Toit_Avant" : "Toit_Arriere", (pa + pb) * 0.5f - n2 * (ep2 * 0.5f), new Vector3(0.42f * s * 2f, ep2, L2), Quaternion.LookRotation(d2, n2));
            }
            foreach (int sx in new[] { -1, 1 })
            {
                var tri = new[] { new Vector3(0, 0.84f * s, zr + 0.3f), new Vector3(0, 0.84f * s, zf - 0.3f), new Vector3(0, yf - 0.35f, zc) };
                ColPrismeX(c, sx < 0 ? "Pignon_Gauche" : "Pignon_Droit", tri, sx * (g.murX * s - 0.3f), sx * g.murX * s);
            }
        }
        // NavMesh : les squelettes n'entrent pas (volume non praticable sur la pièce, le seuil et, en B, le perron)
        NavInterdit(c, "NavMesh_Interdit", new Vector3(-ext - 0.05f, yb - 1f, zAr - 0.05f), new Vector3(ext + 0.05f, (g.rampants ? 3.5f : 0.84f * s), zAv + 0.3f));
        if (!g.rampants)
        {
            NavInterdit(c, "NavMesh_Interdit_Perron", new Vector3(-0.42f * s - 0.05f, 0f, zAv), new Vector3(0.42f * s + 0.05f, 2.4f, 0.35f * s + 0.05f));
            NavInterdit(c, "NavMesh_Interdit_Rampe", new Vector3(-0.28f * s - 0.1f, 0f, 0.35f * s), new Vector3(0.1f, 2.4f, 0.35f * s + 1.05f));
        }
    }

    public static BoxCollider ColBox(Contexte c, string n, Vector3 a, Vector3 b)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(c.collisions, false);
        go.transform.localPosition = (a + b) * 0.5f;
        BoxCollider bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        c.Colliders++;
        return bc;
    }
    public static BoxCollider ColBoxR(Contexte c, string n, Vector3 centre, Vector3 taille, Quaternion r)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(c.collisions, false);
        go.transform.localPosition = centre; go.transform.localRotation = r;
        BoxCollider bc = go.AddComponent<BoxCollider>(); bc.size = taille;
        c.Colliders++;
        return bc;
    }
    // Prisme convexe : triangle (x, y) extrudé en z de z0 à z1.
    static void ColPrisme(Contexte c, string n, Vector3[] tri, float z0, float z1)
    {
        var pts = new List<Vector3>();
        foreach (Vector3 q in tri) { pts.Add(new Vector3(q.x, q.y, z0)); pts.Add(new Vector3(q.x, q.y, z1)); }
        ColConvexe(c, n, pts);
    }
    // Prisme convexe : triangle (z, y) extrudé en x de x0 à x1.
    static void ColPrismeX(Contexte c, string n, Vector3[] tri, float x0, float x1)
    {
        var pts = new List<Vector3>();
        foreach (Vector3 q in tri) { pts.Add(new Vector3(Mathf.Min(x0, x1), q.y, q.z)); pts.Add(new Vector3(Mathf.Max(x0, x1), q.y, q.z)); }
        ColConvexe(c, n, pts);
    }
    static void ColConvexe(Contexte c, string n, List<Vector3> pts, bool boite = false)
    {
        var m = new Mesh { name = "Interieur_" + c.nom + "_" + n };
        m.SetVertices(pts);
        // prisme triangulaire (6 points) ou boîte déformée (8 points : bits x, y, z) ; le collider convexe prend l'enveloppe
        if (!boite) m.SetTriangles(new[] { 0, 2, 4, 1, 5, 3, 0, 1, 3, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 1, 4, 1, 0 }, 0);
        else m.SetTriangles(new[] { 0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5, 0, 4, 5, 0, 5, 1, 2, 3, 7, 2, 7, 6, 0, 2, 6, 0, 6, 4, 1, 5, 7, 1, 7, 3 }, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        m = SauverMesh(m, MeshDir + "/Interieur_" + c.nom + "_Col_" + n + ".asset");
        GameObject go = new GameObject(n); go.transform.SetParent(c.collisions, false);
        MeshCollider mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = m; mc.convex = true;
        c.Colliders++;
    }
    static void NavInterdit(Contexte c, string n, Vector3 a, Vector3 b)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(c.it, false);
        go.transform.localPosition = (a + b) * 0.5f;
        var vol = go.AddComponent<NavMeshModifierVolume>();
        vol.size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        vol.center = Vector3.zero;
        vol.area = 1;   // Not Walkable
    }

    // ---------------- Objets, ancres, lumières ----------------
    // Pose un modèle KayKit (repère de l'intérieur) ; `col` : collider boîte ajusté au maillage.
    public static GameObject Poser(Contexte c, string chemin, Vector3 pos, Vector3 euler, float echelle, bool col)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(chemin + ".fbx");
        if (model == null) { Debug.LogWarning("Intérieurs : modèle introuvable " + chemin); return null; }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, c.props);
        go.name = System.IO.Path.GetFileName(chemin);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = Vector3.one * echelle;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        if (col)
        {
            Bounds b = BornesLocales(go);
            BoxCollider bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = b.size;
            c.Colliders++;
        }
        c.Props++;
        return go;
    }
    public static GameObject Poser(Contexte c, string chemin, Vector3 pos, float lacet, float echelle, bool col) { return Poser(c, chemin, pos, new Vector3(0, lacet, 0), echelle, col); }
    static Bounds BornesLocales(GameObject go)
    {
        Matrix4x4 w2l = go.transform.worldToLocalMatrix; bool first = true; Bounds res = new Bounds();
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            Bounds mb = mf.sharedMesh.bounds; Matrix4x4 m = w2l * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 q = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (first) { res = new Bounds(q, Vector3.zero); first = false; } else res.Encapsulate(q);
            }
        }
        return res;
    }
    public static void ColMeuble(Contexte c, string n, Vector3 a, Vector3 b)
    {
        BoxCollider bc = ColBox(c, n, a, b);
        bc.transform.SetParent(c.mobilier.Find("Colliders") != null ? c.mobilier.Find("Colliders") : Group(c.mobilier, "Colliders"), true);
    }
    public static Transform Ancre(Contexte c, string type, Vector3 pos, float lacet)
    {
        GameObject go = new GameObject("Ancre_" + type + "_" + c.nom);
        go.transform.SetParent(c.it, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, lacet, 0);
        return go.transform;
    }
    // Lumière ponctuelle chaude sans ombre. `feu` : scintillement de foyer (sinon léger, lanterne ou bougie).
    public static Light Lumiere(Contexte c, string n, Vector3 pos, Color col, float intensite, float portee, bool feu)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(c.lumieres, false); go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = col; l.intensity = intensite; l.range = portee;
        l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
        c.Lumieres.Add(l);
        if (feu) { c.Feux.Add(l); c.FeuxI.Add(intensite); } else { c.Lampes.Add(l); c.LampesI.Add(intensite); }
        return l;
    }
    // Flamme : double pyramide effilée (bougies, lanternes, foyers).
    public static void Flamme(MB mb, Vector3 bas, float h, float r)
    {
        Vector3 top = bas + Vector3.up * h, mid = bas + Vector3.up * h * 0.35f;
        var pts = new Vector3[4];
        for (int i = 0; i < 4; i++) { float a = (45f + 90f * i) * Mathf.Deg2Rad; pts[i] = mid + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r; }
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4; Vector3 nn = (pts[i] + pts[j]) * 0.5f - mid;
            mb.Poly(new[] { pts[i], pts[j], top }, nn + Vector3.up * 0.3f, Vector2.zero);
            mb.Poly(new[] { pts[i], pts[j], bas }, nn - Vector3.up * 0.3f, Vector2.zero);
        }
    }
    // Braises : petits éclats en désordre dans un rectangle.
    public static void Braises(Contexte c, Vector3 a, Vector3 b, int n, float taille)
    {
        for (int i = 0; i < n; i++)
        {
            Vector3 q = new Vector3(Mathf.Lerp(a.x, b.x, (float)c.rng.NextDouble()), Mathf.Lerp(a.y, b.y, (float)c.rng.NextDouble()), Mathf.Lerp(a.z, b.z, (float)c.rng.NextDouble()));
            float t = taille * (0.6f + 0.8f * (float)c.rng.NextDouble());
            c.braises.Box(q, new Vector3(t, t * 0.7f, t * 0.9f), Quaternion.Euler(c.rng.Next(360), c.rng.Next(360), c.rng.Next(360)), new Vector2(0.5f, 0.5f));
        }
    }

    // ---------------- Matériaux ----------------
    static Material MatLit(string nom, Color baseCol, Color emission)
    {
        string path = MatDir + "/" + nom + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); m = AssetDatabase.LoadAssetAtPath<Material>(path); }
        m.SetColor("_BaseColor", baseCol); m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Metallic", 0f);
        m.SetColor("_EmissionColor", emission);
        // Drapeau GI RealtimeEmissive (comme VillageBuilder et LanterneAssets) : avec None, URP (BaseShaderGUI.SetMaterialKeywords)
        // retire le mot-clé _EMISSION à la première validation du matériau (import, enregistrement), quelle que soit la couleur.
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.EnableKeyword("_EMISSION");   // après la création de l'asset (sinon le mot-clé est perdu à l'import)
        EditorUtility.SetDirty(m);
        return m;
    }
    // Vitres vues de l'intérieur : claires le jour, bleu nuit la nuit (InterieursAmbiance pilote l'émission).
    public static Material MatVitre() { return MatLit("Interieur_Vitre", new Color(0.1f, 0.13f, 0.2f), new Color(0.62f, 0.74f, 0.9f) * 0.9f); }
    public static Material MatBraises() { return MatLit("Interieur_Braises", new Color(0.35f, 0.08f, 0.03f), new Color(1f, 0.32f, 0.08f) * 2.2f); }

    static Transform Group(Transform parent, string name) { Transform t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

    // ---------------- Petits constructeurs de meubles (maillage « Meubles », atlas des maisons) ----------------
    // Repère d'un meuble adossé : o = coin bas, côté mur, à gauche ; u le long du mur ; n vers la pièce.
    public struct Cadre
    {
        public Vector3 o, u, n; public Quaternion r;
        public Cadre(Vector3 o, Vector3 n) { this.o = o; this.n = n.normalized; u = Vector3.Cross(Vector3.up, this.n); r = Quaternion.LookRotation(this.n); }
        public Vector3 P(float x, float y, float z) { return o + u * x + Vector3.up * y + n * z; }
        public void Box(MB mb, float x0, float y0, float z0, float x1, float y1, float z1, Vector2 texel, int sans = 0)
        { mb.Box((P(x0, y0, z0) + P(x1, y1, z1)) * 0.5f, new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), r, texel, sans); }
        public void Col(Contexte c, string nom, float x0, float y0, float z0, float x1, float y1, float z1) { ColBox(c, "Meuble_" + nom, Vector3.Min(P(x0, y0, z0), P(x1, y1, z1)), Vector3.Max(P(x0, y0, z0), P(x1, y1, z1))); }
    }

    // Étagère : montants, planches, dessus ; renvoie la hauteur (au-dessus de o) du dessus de chaque planche.
    static float[] Etagere(Contexte c, Cadre k, float L, float P, float H, int planches, Vector2 bois, bool col = true)
    {
        MB mb = c.meubles; var hs = new float[planches];
        k.Box(mb, 0, 0, 0, 0.06f, H, P, bois); k.Box(mb, L - 0.06f, 0, 0, L, H, P, bois);
        k.Box(mb, 0, H - 0.05f, 0, L, H, P + 0.02f, bois);
        for (int i = 0; i < planches; i++)
        {
            float y = planches > 1 ? 0.12f + (H - 0.62f) * i / (planches - 1) : 0.12f;
            k.Box(mb, 0.06f, y - 0.04f, 0, L - 0.06f, y, P, BoisMoyen);
            hs[i] = y;
        }
        k.Box(mb, 0.06f, 0, 0.0f, L - 0.06f, 0.08f, 0.03f, BoisSombre);   // plinthe
        if (col) k.Col(c, "Etagere", 0, 0, 0, L, H, P);
        return hs;
    }
    // Rangée de livres (u de x0 à x1, sur la planche à la hauteur y).
    static void Livres(Contexte c, Cadre k, float x0, float x1, float y, float prof, float hMin, float hMax)
    {
        Vector2[] couleurs = { Rouge, BleuNuit, BoisBrun, Tan, Taupe, Bleu, RougeClair, BoisSombre, Lin };
        float x = x0;
        while (x < x1 - 0.05f)
        {
            float w = 0.035f + 0.04f * (float)c.rng.NextDouble(), h = Mathf.Lerp(hMin, hMax, (float)c.rng.NextDouble());
            if (c.rng.NextDouble() < 0.12) { x += 0.06f; continue; }   // trou
            if (x + w > x1) break;
            Vector2 col = couleurs[c.rng.Next(couleurs.Length)];
            float p = prof * (0.75f + 0.25f * (float)c.rng.NextDouble());
            k.Box(c.meubles, x, y, 0.03f, x + w, y + h, 0.03f + p, col);
            x += w + 0.004f;
        }
    }
    // Fiole de potion low poly : panse (liquide), épaule, col clair, bouchon.
    static void Fiole(MB mb, Vector3 b, float h, float r, Vector2 liquide, int cotes = 6)
    {
        mb.Prisme(b, r * 0.85f, r, h * 0.12f, cotes, liquide, 0, false);
        mb.Prisme(b + Vector3.up * h * 0.12f, r, r, h * 0.38f, cotes, liquide, 0, false);
        mb.Prisme(b + Vector3.up * h * 0.5f, r, r * 0.38f, h * 0.18f, cotes, liquide, 0, false);
        mb.Prisme(b + Vector3.up * h * 0.68f, r * 0.34f, r * 0.34f, h * 0.18f, cotes, PierreClaire, 0, false);
        mb.Prisme(b + Vector3.up * h * 0.86f, r * 0.42f, r * 0.36f, h * 0.14f, cotes, BoisBrun);
    }
    // Pot (bocal) : corps et couvercle.
    static void Pot(MB mb, Vector3 b, float h, float r, Vector2 corps, Vector2 couvercle)
    {
        mb.Prisme(b, r, r * 1.05f, h * 0.8f, 7, corps, 0, false);
        mb.Prisme(b + Vector3.up * h * 0.8f, r * 1.05f, r * 0.8f, h * 0.2f, 7, couvercle);
    }
    // Teinte d'herbe séchée : paille, brun, taupe, olive éteint (une fois sur quatre) ; jamais de vert vif.
    static Vector2 Seche(Contexte c) { int k = c.rng.Next(4); return k == 0 ? Olive : k == 1 ? Paille : k == 2 ? Tan : Taupe; }
    // Bouquet d'herbes séchées suspendu (ficelle, tiges, têtes) ; `haut` = point d'attache.
    static void Bouquet(Contexte c, Vector3 haut, float longueur, Vector2 couleur)
    {
        MB mb = c.meubles;
        float corde = 0.12f + 0.1f * (float)c.rng.NextDouble();
        mb.Box(haut - Vector3.up * corde * 0.5f, new Vector3(0.015f, corde, 0.015f), Tan);
        Vector3 lien = haut - Vector3.up * corde;
        mb.Box(lien - Vector3.up * 0.03f, new Vector3(0.07f, 0.05f, 0.07f), Paille);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f + c.rng.Next(30);
            Quaternion r = Quaternion.Euler(180f + 10f * Mathf.Sin(a * Mathf.Deg2Rad), a, 12f * Mathf.Cos(a * Mathf.Deg2Rad));
            float l = longueur * (0.75f + 0.35f * (float)c.rng.NextDouble());
            Vector3 d = r * Vector3.up;
            mb.Box(lien + d * (l * 0.5f), new Vector3(0.035f, l, 0.035f), r, couleur);
            mb.Box(lien + d * (l * 0.9f), new Vector3(0.09f, l * 0.3f, 0.07f), r, couleur == Olive ? OliveClair : couleur);
        }
    }
    // Tapis octogonal plat.
    static void Tapis(Contexte c, Vector3 centre, float r, Vector2 bord, Vector2 milieu)
    {
        c.meubles.Prisme(centre, r, r, 0.012f, 8, bord, 22.5f);
        c.meubles.Prisme(centre + Vector3.up * 0.002f, r * 0.72f, r * 0.72f, 0.012f, 8, milieu, 22.5f);
    }
    // Bougie (modèle Dungeon éteint) + flamme émissive.
    static void Bougie(Contexte c, Vector3 pos, float echelle, bool fine = false)
    {
        Poser(c, DungeonDir + (fine ? "candle_thin" : "candle"), pos, c.rng.Next(360), echelle, false);
        Flamme(c.flammes, pos + Vector3.up * (0.87f * echelle - 0.01f), 0.075f * echelle / 0.35f, 0.022f * echelle / 0.35f);
    }
    // Lanterne suspendue (Halloween) avec sa chaîne jusqu'au plafond et une flamme.
    static void LanterneSuspendue(Contexte c, Vector3 accroche, float plafond, float echelle)
    {
        Poser(c, HalloweenDir + "lantern_hanging", accroche, 0f, echelle, false);
        ColBox(c, "Meuble_Lanterne", accroche + new Vector3(-0.24f * echelle, -1.3f * echelle, -0.24f * echelle), accroche + new Vector3(0.24f * echelle, 0.05f, 0.24f * echelle));
        if (plafond > accroche.y + 0.02f) c.meubles.Box(new Vector3(accroche.x, (plafond + accroche.y) * 0.5f, accroche.z), new Vector3(0.03f, plafond - accroche.y, 0.03f), Fer);
        Flamme(c.flammes, accroche + Vector3.down * 0.95f * echelle, 0.16f * echelle, 0.05f * echelle);
    }
    // Chaudron : pieds, panse, bord, liquide.
    static void Chaudron(Contexte c, Vector3 b, float r, Vector2 liquide)
    {
        MB mb = c.meubles;
        for (int i = 0; i < 3; i++) { float a = i * 120f * Mathf.Deg2Rad; mb.Box(b + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r * 0.6f + Vector3.up * 0.06f, new Vector3(0.06f, 0.14f, 0.06f), Noir); }
        mb.Prisme(b + Vector3.up * 0.1f, r * 0.55f, r, r * 0.55f, 8, Noir, 0, false, true);
        mb.Prisme(b + Vector3.up * (0.1f + r * 0.55f), r, r * 0.86f, r * 0.45f, 8, Noir, 0, false);
        mb.Prisme(b + Vector3.up * (0.1f + r), r * 0.94f, r * 0.94f, 0.05f, 8, Fer, 0, false);
        mb.Prisme(b + Vector3.up * (0.1f + r * 0.95f), r * 0.86f, r * 0.86f, 0.001f, 8, liquide);
    }
    // Prisme orienté (bûche, pilon...) de a à b, bouts d'une autre teinte.
    public static void PrismeAxe(MB mb, Vector3 a, Vector3 b, float r, int cotes, Vector2 texel, Vector2 bout)
    {
        Vector3 d = (b - a).normalized; Vector3 e1 = Vector3.Cross(d, Mathf.Abs(d.y) < 0.9f ? Vector3.up : Vector3.right).normalized; Vector3 e2 = Vector3.Cross(d, e1);
        var pa = new Vector3[cotes]; var pb = new Vector3[cotes];
        for (int i = 0; i < cotes; i++) { float t = 2f * Mathf.PI * i / cotes; Vector3 o = (e1 * Mathf.Cos(t) + e2 * Mathf.Sin(t)) * r; pa[i] = a + o; pb[i] = b + o; }
        for (int i = 0; i < cotes; i++) { int j = (i + 1) % cotes; mb.Poly(new[] { pa[i], pa[j], pb[j], pb[i] }, (pa[i] + pa[j]) * 0.5f - a, texel); }
        mb.Poly(pa, -d, bout); mb.Poly(pb, d, bout);
    }
    // Deux bûches croisées sur un foyer.
    static void Buches(Contexte c, Vector3 centre, float L, float lacet)
    {
        Quaternion q = Quaternion.Euler(0, lacet, 0);
        PrismeAxe(c.meubles, centre + q * new Vector3(-L / 2, 0, 0.06f), centre + q * new Vector3(L / 2, 0, -0.04f), 0.065f, 6, BoisBrun, Tan);
        PrismeAxe(c.meubles, centre + q * new Vector3(-L / 2 + 0.05f, 0.08f, -0.1f), centre + q * new Vector3(L / 2 - 0.05f, 0.05f, 0.12f), 0.06f, 6, BoisSombre, Tan);
    }
    // Souche (billot de l'enclume).
    static void Souche(MB mb, Vector3 b, float r, float h)
    {
        mb.Prisme(b, r * 1.1f, r, h * 0.2f, 9, BoisSombre, 10f, false);
        mb.Prisme(b + Vector3.up * h * 0.2f, r, r * 0.95f, h * 0.8f, 9, BoisBrun, 10f, false);
        mb.Prisme(b + Vector3.up * h, r * 0.95f, r * 0.95f, 0.002f, 9, Tan, 10f);
    }
    // Table simple : plateau, quatre pieds, entretoise (repère du cadre), collider.
    static void Table(Contexte c, Cadre k, float L, float P, float H, Vector2 plateau, Vector2 pieds, string nom)
    {
        k.Box(c.meubles, -0.03f, H - 0.06f, -0.03f, L + 0.03f, H, P + 0.03f, plateau);
        foreach (float x in new[] { 0.04f, L - 0.12f }) foreach (float z in new[] { 0.04f, P - 0.12f }) k.Box(c.meubles, x, 0, z, x + 0.08f, H - 0.06f, z + 0.08f, pieds);
        k.Box(c.meubles, 0.08f, 0.18f, 0.08f, L - 0.08f, 0.22f, P - 0.08f, pieds);
        k.Col(c, nom, -0.03f, 0, -0.03f, L + 0.03f, H, P + 0.03f);
    }

    // ---------------- Druide : boutique de potions de soin ----------------
    // Âtre et chaudron dans l'angle de la cheminée (arrière gauche), comptoir face à la porte, étagères de potions de part
    // et d'autre de la fenêtre arrière, table d'herboriste le long du mur gauche, herbes séchées suspendues. Couleurs de la
    // texture du druide (olive éteint, lin, tan, bruns) et potions ambre (icône de la potion de soin), sans vert vif.
    static void MeublerDruide(Contexte c)
    {
        Piece p = c.p; MB mb = c.meubles; float y = p.yF;
        // âtre, hotte, chaudron
        mb.BoxMinMax(new Vector3(-p.xi, y, p.zbi), new Vector3(-0.95f, y + 0.22f, -1.25f), Pierre);
        ColBox(c, "Meuble_Atre", new Vector3(-p.xi, y, p.zbi), new Vector3(-0.95f, y + 0.22f, -1.25f));
        mb.BoxMinMax(new Vector3(-p.xi, y + 2.35f, p.zbi), new Vector3(-0.98f, y + 2.7f, -1.32f), Pierre);
        mb.BoxMinMax(new Vector3(-2.15f, y + 2.7f, p.zbi), new Vector3(-1.3f, p.yC, -1.8f), PierreClaire);
        mb.BoxMinMax(new Vector3(-p.xi, y + 2.22f, -1.42f), new Vector3(-0.95f, y + 2.36f, -1.27f), Colombage);
        mb.Quad(new Vector3(-2.35f, y + 0.22f, p.zbi + 0.01f), new Vector3(-1.1f, y + 0.22f, p.zbi + 0.01f), new Vector3(-1.1f, y + 2.35f, p.zbi + 0.01f), new Vector3(-2.35f, y + 2.35f, p.zbi + 0.01f), Vector3.forward, Suie);
        Vector3 cb = new Vector3(-1.72f, y + 0.22f, -1.9f);
        Braises(c, cb + new Vector3(-0.35f, 0.01f, -0.3f), cb + new Vector3(0.35f, 0.08f, 0.3f), 28, 0.08f);
        Buches(c, cb + new Vector3(0, 0.06f, 0), 0.55f, 30f);
        Flamme(c.flammes, cb + new Vector3(-0.12f, 0.08f, 0.05f), 0.28f, 0.07f); Flamme(c.flammes, cb + new Vector3(0.1f, 0.08f, -0.06f), 0.22f, 0.06f);
        Chaudron(c, cb + new Vector3(0, 0.08f, 0), 0.4f, Ambre);
        ColBox(c, "Meuble_Chaudron", cb + new Vector3(-0.42f, 0, -0.42f), cb + new Vector3(0.42f, 0.62f, 0.42f));
        Lumiere(c, "Feu_Chaudron", cb + new Vector3(0.15f, 0.5f, 0.35f), new Color(1f, 0.55f, 0.25f), 1.6f, 4.2f, true);
        Poser(c, ToolsDir + "bucket_metal", new Vector3(-0.8f, y, -2.2f), 20f, 0.42f, true);

        // étagères de potions : à gauche de la fenêtre arrière, à sa droite et contre le mur droit
        var kg = new Cadre(new Vector3(-0.85f, y, p.zbi), Vector3.forward);
        float[] hg = Etagere(c, kg, 1.28f, 0.38f, 2.3f, 4, BoisBrun);
        var kd = new Cadre(new Vector3(1.66f, y, p.zbi), Vector3.forward);
        float[] hd = Etagere(c, kd, p.xi - 1.66f, 0.38f, 2.3f, 4, BoisBrun);
        var kr = new Cadre(new Vector3(p.xi, y, -2.1f), Vector3.left);   // u vers +z : de -2,1 à -1,12 (fenêtre droite de -1,05 à 0)
        float[] hr = Etagere(c, kr, 0.98f, 0.38f, 2.3f, 4, BoisBrun);
        Vector2[] potions = { Ambre, Ambre, Ambre, Or, BoisBrun, Lin, RougeClair };
        Cadre[] ks = { kg, kd, kr }; float[] Ls = { 1.28f, p.xi - 1.66f, 0.98f }; float[][] hs = { hg, hd, hr };
        for (int e = 0; e < 3; e++)
            for (int i = 0; i < hs[e].Length; i++)
            {
                float x = 0.14f;
                while (x < Ls[e] - 0.12f)
                {
                    float r = 0.045f + 0.03f * (float)c.rng.NextDouble(), hh = 0.2f + 0.14f * (float)c.rng.NextDouble();
                    int t = c.rng.Next(10);
                    Vector3 b = ks[e].P(x + r, hs[e][i], 0.2f + 0.06f * (float)c.rng.NextDouble());
                    if (t < 6) Fiole(mb, b, hh, r, potions[c.rng.Next(potions.Length)]);
                    else if (t < 8) Pot(mb, b, hh * 0.7f, r * 1.2f, Lin, BoisBrun);
                    else if (t < 9) Poser(c, DungeonDir + "bottle_A_brown", b, c.rng.Next(360), 0.3f, false);
                    else { x += 0.08f; continue; }
                    x += r * 2f + 0.04f + 0.05f * (float)c.rng.NextDouble();
                }
            }
        // comptoir face à la porte : dessus en bois clair, façade en lames, tapis devant
        float cx0 = -0.1f, cx1 = 2.0f, cz0 = -1.2f, cz1 = -0.6f, ch = 1.05f;
        mb.BoxMinMax(new Vector3(cx0, y, cz0 + 0.05f), new Vector3(cx1, y + ch - 0.07f, cz1 - 0.02f), BoisBrun);
        for (float x = cx0; x < cx1 - 0.05f; x += 0.21f) mb.BoxMinMax(new Vector3(x + 0.01f, y + 0.08f, cz1 - 0.03f), new Vector3(Mathf.Min(x + 0.2f, cx1), y + ch - 0.12f, cz1), c.rng.Next(2) == 0 ? BoisMoyen : BoisClair);
        mb.BoxMinMax(new Vector3(cx0 - 0.05f, y + ch - 0.07f, cz0), new Vector3(cx1 + 0.05f, y + ch, cz1 + 0.05f), BoisClair);
        mb.BoxMinMax(new Vector3(cx0, y, cz1 - 0.04f), new Vector3(cx1, y + 0.08f, cz1 + 0.01f), BoisSombre);
        ColBox(c, "Meuble_Comptoir", new Vector3(cx0 - 0.05f, y, cz0), new Vector3(cx1 + 0.05f, y + ch, cz1 + 0.05f));
        float yc = y + ch;
        Fiole(mb, new Vector3(0.35f, yc, -0.82f), 0.3f, 0.07f, Ambre); Fiole(mb, new Vector3(0.52f, yc, -0.95f), 0.26f, 0.06f, Ambre); Fiole(mb, new Vector3(0.62f, yc, -0.78f), 0.24f, 0.055f, Ambre);
        mb.Prisme(new Vector3(1.55f, yc, -0.9f), 0.1f, 0.13f, 0.12f, 7, PierreClaire);                                  // mortier
        mb.Box(new Vector3(1.6f, yc + 0.16f, -0.9f), new Vector3(0.03f, 0.2f, 0.03f), Quaternion.Euler(0, 0, -25f), BoisClair);   // pilon
        Poser(c, ToolsDir + "journal_open", new Vector3(1.05f, yc + 0.02f, -0.95f), new Vector3(0, 180f, 0), 0.4f, false);
        Poser(c, DungeonDir + "coin_stack_small", new Vector3(1.85f, yc, -0.78f), 30f, 0.22f, false);
        Bougie(c, new Vector3(-0.0f, yc, -1.1f), 0.32f);
        Tapis(c, new Vector3(0.9f, y + 0.002f, 0.25f), 0.95f, BoisBrun, Lin);
        // table d'herboriste contre le mur gauche : bocaux, herbes, sac
        var kt = new Cadre(new Vector3(-p.xi, y, 0.55f), Vector3.right);
        Table(c, kt, 1.6f, 0.6f, 0.85f, BoisClair, BoisBrun, "Table_Herbes");
        for (int i = 0; i < 5; i++) Pot(mb, kt.P(0.2f + i * 0.3f, 0.85f, 0.18f + 0.1f * (i % 2)), 0.16f + 0.05f * (i % 3), 0.06f, i % 2 == 0 ? Lin : Tan, BoisBrun);
        for (int i = 0; i < 3; i++) mb.Box(kt.P(0.4f + i * 0.45f, 0.87f, 0.42f), new Vector3(0.35f, 0.04f, 0.1f), Quaternion.Euler(0, 20 * i - 20, 0), i == 1 ? Paille : Olive);   // herbes à sécher
        // herbes séchées suspendues : perche le long du mur gauche et perche au-dessus du comptoir
        mb.BoxMinMax(new Vector3(-2.02f, y + 2.72f, -1.15f), new Vector3(-1.96f, y + 2.78f, 1.3f), BoisSombre);
        for (float z = -1.0f; z < 1.2f; z += 0.3f) Bouquet(c, new Vector3(-1.99f, y + 2.72f, z), 0.32f, Seche(c));
        mb.BoxMinMax(new Vector3(-0.3f, y + 3.02f, -0.33f), new Vector3(2.4f, y + 3.08f, -0.27f), BoisSombre);
        for (float x = -0.1f; x < 2.3f; x += 0.34f) Bouquet(c, new Vector3(x, y + 3.02f, -0.3f), 0.3f, Seche(c));
        ColBox(c, "Meuble_Herbes_Mur", new Vector3(-2.2f, y + 2.2f, -1.15f), new Vector3(-1.8f, y + 2.8f, 1.3f));
        ColBox(c, "Meuble_Herbes_Comptoir", new Vector3(-0.3f, y + 2.45f, -0.5f), new Vector3(2.4f, y + 3.1f, -0.1f));
        mb.BoxMinMax(new Vector3(-0.3f, y + 3.08f, -0.33f), new Vector3(-0.24f, p.yC, -0.27f), BoisSombre);
        mb.BoxMinMax(new Vector3(2.34f, y + 3.08f, -0.33f), new Vector3(2.4f, p.yC, -0.27f), BoisSombre);
        // lanterne au-dessus du comptoir, tonneaux et sacs près de l'entrée
        LanterneSuspendue(c, new Vector3(0.95f, y + 3.0f, -0.62f), p.yC, 0.7f);
        Lumiere(c, "Lampe_Comptoir", new Vector3(0.95f, y + 2.3f, -0.45f), new Color(1f, 0.72f, 0.42f), 1.8f, 5f, false);
        Poser(c, DungeonDir + "barrel_small", new Vector3(-2.13f, y, 0.95f), 15f, 0.68f, true);   // hors du débattement du battant
        Poser(c, ResDir + "Textiles_A", new Vector3(2.0f, y, 1.0f), -20f, 0.7f, true);
        Poser(c, DungeonDir + "barrel_small", new Vector3(2.05f, y, 0.25f), 70f, 0.62f, true);
        Ancre(c, "Villageois", new Vector3(1.05f, y, -1.8f), 0f);
        Ancre(c, "Echange", new Vector3(0.95f, y + ch, -0.9f), 0f);
    }

    // ---------------- Mécano : atelier, futur vendeur d'armes et d'améliorations ----------------
    // Établi le long du mur droit (le mécano derrière, fenêtre dans le dos), armes exposées sur un panneau au mur du fond et
    // dans un tonneau, engrenages, outils au mur gauche, barils et pièces dans l'angle de la cheminée.
    static void MeublerMecano(Contexte c)
    {
        Piece p = c.p; MB mb = c.meubles; float y = p.yF;
        // établi : plateau épais, pieds, étagère basse, étau
        float bx0 = 1.0f, bx1 = 1.75f, bz0 = -1.95f, bz1 = -0.05f, bh = 1.0f;
        mb.BoxMinMax(new Vector3(bx0 - 0.04f, y + bh - 0.1f, bz0 - 0.04f), new Vector3(bx1 + 0.02f, y + bh, bz1 + 0.04f), BoisClair);
        foreach (float x in new[] { bx0 + 0.02f, bx1 - 0.12f }) foreach (float z in new[] { bz0 + 0.02f, bz1 - 0.12f }) mb.BoxMinMax(new Vector3(x, y, z), new Vector3(x + 0.1f, y + bh - 0.1f, z + 0.1f), BoisBrun);
        mb.BoxMinMax(new Vector3(bx0 + 0.04f, y + 0.25f, bz0 + 0.04f), new Vector3(bx1 - 0.04f, y + 0.3f, bz1 - 0.04f), BoisMoyen);
        mb.BoxMinMax(new Vector3(bx0 - 0.06f, y + bh, bz1 - 0.35f), new Vector3(bx0 + 0.12f, y + bh + 0.12f, bz1 - 0.15f), Fer);   // étau
        ColBox(c, "Meuble_Etabli", new Vector3(bx0 - 0.06f, y, bz0 - 0.04f), new Vector3(bx1 + 0.02f, y + bh, bz1 + 0.04f));
        float yb = y + bh;
        Poser(c, ToolsDir + "hammer", new Vector3(1.3f, yb + 0.03f, -1.55f), new Vector3(90, 30, 0), 0.5f, false);
        Poser(c, ToolsDir + "wrench_A", new Vector3(1.25f, yb + 0.02f, -0.5f), new Vector3(90, 70, 0), 0.55f, false);
        Poser(c, ToolsDir + "screwdriver_A_long_color", new Vector3(1.45f, yb + 0.03f, -0.75f), new Vector3(90, 10, 0), 0.5f, false);
        Poser(c, ToolsDir + "handdrill", new Vector3(1.5f, yb + 0.05f, -1.2f), new Vector3(90, -50, 0), 0.5f, false);
        Poser(c, ResDir + "Parts_Cog", new Vector3(1.3f, yb + 0.03f, -1.05f), new Vector3(90, 0, 0), 0.55f, false);
        Poser(c, ResDir + "Parts_Cog", new Vector3(1.55f, yb + 0.03f, -0.95f), new Vector3(90, 20, 0), 0.4f, false);
        Poser(c, ResDir + "Parts_Pile_Small", new Vector3(1.4f, yb, -1.8f), 40f, 0.4f, false);
        Poser(c, ToolsDir + "blueprint", new Vector3(1.4f, yb, -0.3f), -80f, 0.35f, false);
        Poser(c, ToolsDir + "lantern", new Vector3(1.6f, yb, -1.4f), 0f, 0.5f, false);
        Flamme(c.flammes, new Vector3(1.6f, yb + 0.2f, -1.4f), 0.14f, 0.04f);
        Lumiere(c, "Lampe_Etabli", new Vector3(1.5f, yb + 0.45f, -1.3f), new Color(1f, 0.72f, 0.42f), 1.5f, 4.5f, false);
        // panneau d'armes au mur du fond (à gauche de la fenêtre) et boucliers au-dessus
        var kw = new Cadre(new Vector3(-0.9f, y, p.zbi), Vector3.forward);
        kw.Box(mb, 0, 0.75f, 0, 1.32f, 2.35f, 0.04f, BoisSombre);
        kw.Col(c, "Panneau_Armes", -0.05f, 0.67f, 0, 1.37f, 3.3f, 0.2f);
        kw.Box(mb, -0.05f, 2.35f, 0, 1.37f, 2.43f, 0.07f, Colombage); kw.Box(mb, -0.05f, 0.67f, 0, 1.37f, 0.75f, 0.07f, Colombage);
        string[] armes = { "sword_A", "sword_B", "axe_A", "sword_C", "hammer_A" };
        for (int i = 0; i < armes.Length; i++)
        {
            Vector3 q = kw.P(0.16f + i * 0.25f, 1.45f, 0.09f);
            Poser(c, WeaponsDir + armes[i], q, new Vector3(0, 0, 0), 0.52f, false);
            mb.Box(kw.P(0.16f + i * 0.25f, 2.02f, 0.06f), new Vector3(0.05f, 0.04f, 0.05f), kw.r, Fer);   // crochets
            mb.Box(kw.P(0.16f + i * 0.25f, 1.22f, 0.06f), new Vector3(0.05f, 0.04f, 0.05f), kw.r, Fer);
        }
        Poser(c, WeaponsDir + "shield_A", kw.P(0.33f, 2.9f, 0.12f), new Vector3(0, 0, 0), 0.6f, false);
        Poser(c, WeaponsDir + "shield_B", kw.P(1.0f, 2.95f, 0.12f), new Vector3(0, 0, 0), 0.55f, false);
        // tonneau d'armes d'hast près de la fenêtre avant
        Vector3 tb = new Vector3(1.95f, y, 0.85f);
        Poser(c, DungeonDir + "barrel_small", tb, 0f, 0.6f, true);
        Poser(c, WeaponsDir + "spear_A", tb + new Vector3(-0.08f, 1.3f, 0.05f), new Vector3(8, 0, -6), 0.62f, false);
        Poser(c, WeaponsDir + "halberd", tb + new Vector3(0.08f, 1.2f, -0.06f), new Vector3(-6, 40, 7), 0.6f, false);
        Poser(c, WeaponsDir + "staff_A", tb + new Vector3(0.0f, 1.2f, 0.1f), new Vector3(10, 0, 4), 0.6f, false);
        // engrenages et barils dans l'angle de la cheminée
        Poser(c, ResDir + "Parts_Cog", new Vector3(-1.75f, y + 2.35f, p.zbi + 0.1f), new Vector3(0, 0, 10), 4.2f, true);
        Poser(c, ResDir + "Parts_Cog", new Vector3(-1.0f, y + 2.9f, p.zbi + 0.08f), new Vector3(0, 0, 0), 2.4f, true);
        Poser(c, ResDir + "Fuel_A_Barrels", new Vector3(-1.85f, y, -1.85f), 20f, 0.72f, true);
        Poser(c, ResDir + "Parts_Pile_Large", new Vector3(-0.95f, y, -2.0f), 200f, 0.62f, true);
        Poser(c, DungeonDir + "box_small", new Vector3(-2.1f, y, -0.9f), 10f, 0.55f, true);
        Poser(c, DungeonDir + "box_small", new Vector3(-2.12f, y + 0.55f, -0.88f), 35f, 0.42f, false);
        // outils au mur gauche au-dessus d'une desserte
        var ko = new Cadre(new Vector3(-p.xi, y, 0.95f), Vector3.right);
        Table(c, ko, 1.45f, 0.55f, 0.9f, BoisMoyen, BoisBrun, "Desserte");
        ko.Box(mb, 0.0f, 1.25f, 0, 1.45f, 2.2f, 0.03f, BoisBrun);
        string[] outils = { "saw", "hammer", "wrench_B", "screwdriver_B_long", "pickaxe", "mallet" };
        for (int i = 0; i < outils.Length; i++) Poser(c, ToolsDir + outils[i], ko.P(0.18f + i * 0.22f, 1.62f, 0.07f), new Vector3(0, -90f, 0), 0.45f, false);
        Poser(c, ToolsDir + "blueprint_stacked", ko.P(0.5f, 0.9f, 0.28f), new Vector3(0, 90f, 0), 0.3f, false);
        Poser(c, ResDir + "Parts_Pile_Small", ko.P(1.1f, 0.9f, 0.3f), new Vector3(0, 10f, 0), 0.35f, false);
        Poser(c, ToolsDir + "rope_bundle_A", ko.P(0.7f, 0.28f, 0.28f), new Vector3(0, 90f, 0), 0.4f, false);
        // lanterne suspendue au centre
        LanterneSuspendue(c, new Vector3(-0.1f, y + 3.1f, -0.4f), p.yC, 0.7f);
        Lumiere(c, "Lampe_Plafond", new Vector3(-0.1f, y + 2.4f, -0.4f), new Color(1f, 0.72f, 0.42f), 1.7f, 5.5f, false);
        Tapis(c, new Vector3(0.25f, y + 0.002f, -0.2f), 0.85f, BoisSombre, Taupe);
        Ancre(c, "Villageois", new Vector3(2.12f, y, -1.0f), -90f);
        Ancre(c, "Echange", new Vector3(1.35f, yb, -1.0f), -90f);
    }

    // ---------------- Forgeron : forge ----------------
    // Forge de pierre à braises chaudes sous la cheminée (arrière gauche), enclume sur billot face à la porte (le forgeron
    // entre l'enclume et la forge), râtelier d'armes contre le mur droit, boucliers, meule, lingots et bois.
    // Feu de la forge : lit de braises (maillage Braises), lumière Feu_Forge ; les flammes sont vivantes (ForgeFeu, posé
    // par ForgeronBuilder), plus de flammes fixes sur la forge.
    // Enclume (retour de Quentin, 26/09/2026 : « le marteau doit frapper l'enclume et pas rentrer dedans, réduis la taille
    // de celle-ci et replace-la ») : anvil KayKit à 0,42 (0,7 × l'ancienne 0,6) sur un billot bas de 10 cm, table à
    // 0,44 m du plancher, à hauteur de la main du forgeron (barbare KayKit à 0,8 : main à 0,43 m au repos, épaules à
    // 0,95 m) ; placée devant sa main droite quand il se tient à son ancre (Ancre_Villageois_Forgeron). L'ancienne, à 0,96 m,
    // lui arrivait aux épaules. ForgeronBuilder cale ensuite le geste sur la table.
    public const float EnclumeEchelle = 0.42f;     // anvil.fbx : 0,8 unité de haut, table plate de 1,2 × 0,7 unité
    public const float BillotHauteur = 0.10f, BillotRayon = 0.26f;
    public static readonly Vector3 EnclumeBillot = new Vector3(-0.42f, 0f, -0.27f);   // pied du billot (repère de la pièce, au plancher)
    public static float EnclumeDessus { get { return BillotHauteur + 0.8f * EnclumeEchelle; } }
    // Tenailles debout dans le seau de trempe (bucket_metal à (0,05 ; -1,05), échelle 0,5 : 0,36 m de haut, 0,2 m de rayon).
    static Vector3 TenailleSeau(float y) { return new Vector3(0.03f, y + 0.44f, -1.03f); }
    static void MeublerForgeron(Contexte c)
    {
        Piece p = c.p; MB mb = c.meubles; float y = p.yF;
        // forge : bloc de pierre, rebord, lit de braises, hotte
        float fx0 = -p.xi, fx1 = -0.85f, fz0 = p.zbi, fz1 = -1.3f, fh = 0.85f;
        mb.BoxMinMax(new Vector3(fx0, y, fz0), new Vector3(fx1, y + fh, fz1), PierreSombre);
        mb.BoxMinMax(new Vector3(fx0, y + fh, fz0), new Vector3(fx1, y + fh + 0.1f, fz0 + 0.15f), Pierre);
        mb.BoxMinMax(new Vector3(fx0, y + fh, fz1 - 0.15f), new Vector3(fx1, y + fh + 0.1f, fz1), Pierre);
        mb.BoxMinMax(new Vector3(fx0, y + fh, fz0 + 0.15f), new Vector3(fx0 + 0.15f, y + fh + 0.1f, fz1 - 0.15f), Pierre);
        mb.BoxMinMax(new Vector3(fx1 - 0.15f, y + fh, fz0 + 0.15f), new Vector3(fx1, y + fh + 0.1f, fz1 - 0.15f), Pierre);
        mb.BoxMinMax(new Vector3(fx0 + 0.15f, y + fh - 0.01f, fz0 + 0.15f), new Vector3(fx1 - 0.15f, y + fh + 0.01f, fz1 - 0.15f), Suie, 1);
        Braises(c, new Vector3(fx0 + 0.2f, y + fh, fz0 + 0.2f), new Vector3(fx1 - 0.2f, y + fh + 0.1f, fz1 - 0.2f), 60, 0.09f);
        ColBox(c, "Meuble_Forge", new Vector3(fx0, y, fz0), new Vector3(fx1, y + fh + 0.1f, fz1));
        mb.BoxMinMax(new Vector3(fx0, y + 2.2f, fz0), new Vector3(fx1 + 0.05f, y + 2.6f, fz1 + 0.05f), PierreSombre);
        mb.BoxMinMax(new Vector3(fx0, y + 2.1f, fz1 - 0.08f), new Vector3(fx1 + 0.05f, y + 2.22f, fz1 + 0.07f), Fer);
        mb.BoxMinMax(new Vector3(-2.15f, y + 2.6f, fz0), new Vector3(-1.2f, p.yC, -1.75f), Pierre);
        mb.Quad(new Vector3(fx0 + 0.1f, y + fh + 0.1f, fz0 + 0.01f), new Vector3(fx1 - 0.1f, y + fh + 0.1f, fz0 + 0.01f), new Vector3(fx1 - 0.1f, y + 2.2f, fz0 + 0.01f), new Vector3(fx0 + 0.1f, y + 2.2f, fz0 + 0.01f), Vector3.forward, Suie);
        Lumiere(c, "Feu_Forge", new Vector3(-1.65f, y + fh + 0.45f, -1.7f), new Color(1f, 0.45f, 0.18f), 3.0f, 5.5f, true);
        // soufflet et seau de trempe, lingots, bois
        mb.BoxMinMax(new Vector3(-0.8f, y + 0.55f, -2.3f), new Vector3(-0.45f, y + 0.7f, -1.8f), BoisBrun);
        mb.BoxMinMax(new Vector3(-0.78f, y + 0.7f, -2.25f), new Vector3(-0.47f, y + 0.82f, -1.9f), Taupe);
        Poser(c, ToolsDir + "bucket_metal", new Vector3(0.05f, y, -1.05f), 0f, 0.5f, true);
        Poser(c, ResDir + "Iron_Bars_Stack_Small", new Vector3(-2.1f, y, -1.06f), 90f, 0.6f, true);
        Poser(c, ResDir + "Wood_Log_Stack", new Vector3(-2.08f, y, -0.4f), 0f, 0.55f, true);   // hors du débattement du battant
        Poser(c, ResDir + "Stone_Chunks_Small", new Vector3(-1.35f, y, -1.05f), 30f, 0.35f, false);
        // enclume sur billot bas (voir EnclumeEchelle)
        Vector3 ab = EnclumeBillot + Vector3.up * y; float de = EnclumeDessus;
        Souche(mb, ab, BillotRayon, BillotHauteur);
        Poser(c, ToolsDir + "anvil", ab + Vector3.up * BillotHauteur, 0f, EnclumeEchelle, false);
        ColBox(c, "Meuble_Enclume", ab + new Vector3(-0.31f, 0, -0.29f), ab + new Vector3(0.39f, de, 0.29f));
        Poser(c, ToolsDir + "hammer", ab + new Vector3(0.12f, de + 0.01f, 0.04f), new Vector3(90, 60, 0), 0.45f, false);   // repris par ForgeronBuilder (dans sa main)
        Poser(c, ToolsDir + "tongs", TenailleSeau(y), new Vector3(0, 0, 12), 0.55f, false);   // dans le seau de trempe
        // râtelier d'armes contre le mur droit
        var kr = new Cadre(new Vector3(p.xi, y, 0.05f), Vector3.left);   // u vers +z : de 0,05 à 1,45
        kr.Box(mb, 0, 0, 0.0f, 1.4f, 0.12f, 0.45f, BoisBrun);
        kr.Box(mb, 0, 1.0f, 0.12f, 1.4f, 1.08f, 0.2f, BoisBrun);
        kr.Box(mb, 0, 0, 0.12f, 0.08f, 1.12f, 0.2f, BoisSombre); kr.Box(mb, 1.32f, 0, 0.12f, 1.4f, 1.12f, 0.2f, BoisSombre);
        kr.Col(c, "Ratelier", 0, 0, 0, 1.4f, 1.12f, 0.45f);
        string[] armes = { "sword_A", "axe_B", "sword_C", "spear_A", "hammer_C", "sword_D" };
        for (int i = 0; i < armes.Length; i++)
        {
            float hh = armes[i] == "spear_A" ? 1.03f : armes[i].StartsWith("axe") ? 0.41f : armes[i].StartsWith("hammer") ? 0.2f : 0.37f;
            Vector3 q = kr.P(0.18f + i * 0.21f, 0.12f + hh * 0.62f, 0.2f);
            Poser(c, WeaponsDir + armes[i], q, Quaternion.LookRotation(-kr.n).eulerAngles + new Vector3(-8f, 0, 0), 0.62f, false);
        }
        // boucliers et outils au mur
        Poser(c, WeaponsDir + "shield_C", new Vector3(p.xi - 0.12f, y + 1.9f, -1.9f), new Vector3(0, -90f, 0), 0.6f, false);
        Poser(c, WeaponsDir + "shield_A", new Vector3(p.xi - 0.1f, y + 2.0f, 0.1f), new Vector3(0, -90f, 0), 0.6f, false);
        var ko = new Cadre(new Vector3(-0.75f, y, p.zbi), Vector3.forward);
        ko.Box(mb, 0, 1.3f, 0, 1.15f, 1.38f, 0.1f, BoisBrun);
        string[] outils = { "tongs", "hammer", "file", "chisel", "hammer" };
        for (int i = 0; i < outils.Length; i++) Poser(c, ToolsDir + outils[i], ko.P(0.12f + i * 0.22f, 1.05f, 0.12f), new Vector3(0, 0, 0), 0.45f, false);
        Poser(c, ToolsDir + "grindstone", new Vector3(1.95f, y, -1.95f), -30f, 0.55f, true);
        Poser(c, DungeonDir + "box_small", new Vector3(2.05f, y, -0.8f), 5f, 0.55f, true);
        Poser(c, ResDir + "Iron_Bars", new Vector3(2.05f, y + 0.55f, -0.8f), 20f, 0.4f, false);
        Poser(c, ToolsDir + "lantern", new Vector3(p.xi - 0.25f, y + 2.35f, 0.62f), 0f, 0.45f, false);
        mb.BoxMinMax(new Vector3(p.xi - 0.4f, y + 2.35f, 0.58f), new Vector3(p.xi, y + 2.4f, 0.66f), Fer);
        Flamme(c.flammes, new Vector3(p.xi - 0.25f, y + 2.53f, 0.62f), 0.13f, 0.035f);
        Lumiere(c, "Lampe_Ratelier", new Vector3(p.xi - 0.5f, y + 2.5f, 0.62f), new Color(1f, 0.72f, 0.42f), 1.2f, 4.5f, false);
        Tapis(c, new Vector3(-0.9f, y + 0.002f, 0.75f), 0.7f, BoisSombre, Rouge);
        Ancre(c, "Villageois", new Vector3(-0.9f, y, -0.85f), 0f);
        Ancre(c, "Echange", ab + Vector3.up * de, 0f);
    }

    // ---------------- Sorcier : maison du gardien de Nyxessa (il y passe la journée ; la nuit, il protège la relique) ----------------
    // Retour de Quentin (25/09/2026) : pas de pentacle ni de cliché occulte. La pièce raconte son lien avec Nyxessa et son
    // rôle de gardien : pupitre avec la carte du village, éclat vert de Nyxessa sur le bureau (vert autorisé ici, discret,
    // sans émission ni lumière), croquis de la relique et du village épinglés au mur, râtelier de son bâton, grimoires,
    // bougies, âtre, lit. Il ne vend rien : l'ancre « Echange » marque le pupitre (point de rencontre).
    public static readonly Vector2 VertNyxessa = new Vector2(0.5625f, 0.62f);   // vert tendre de l'atlas (palette Nyxessa)
    static void MeublerSorcier(Contexte c)
    {
        Piece p = c.p; MB mb = c.meubles; float y = p.yF;
        // âtre
        mb.BoxMinMax(new Vector3(-0.62f, y, p.zbi), new Vector3(0.62f, y + 0.18f, -1.72f), Pierre);
        mb.BoxMinMax(new Vector3(-0.62f, y + 0.18f, p.zbi), new Vector3(-0.42f, y + 1.05f, -1.8f), PierreClaire);
        mb.BoxMinMax(new Vector3(0.42f, y + 0.18f, p.zbi), new Vector3(0.62f, y + 1.05f, -1.8f), PierreClaire);
        mb.BoxMinMax(new Vector3(-0.72f, y + 1.05f, p.zbi), new Vector3(0.72f, y + 1.2f, -1.7f), Colombage);
        mb.BoxMinMax(new Vector3(-0.5f, y + 1.2f, p.zbi), new Vector3(0.5f, p.Plafond(0.5f) + 0.05f, -1.9f), PierreClaire);
        mb.Quad(new Vector3(-0.42f, y + 0.18f, p.zbi + 0.01f), new Vector3(0.42f, y + 0.18f, p.zbi + 0.01f), new Vector3(0.42f, y + 1.05f, p.zbi + 0.01f), new Vector3(-0.42f, y + 1.05f, p.zbi + 0.01f), Vector3.forward, Suie);
        ColBox(c, "Meuble_Atre", new Vector3(-0.72f, y, p.zbi), new Vector3(0.72f, y + 1.2f, -1.7f));
        Vector3 fb = new Vector3(0f, y + 0.18f, -2.0f);
        Braises(c, fb + new Vector3(-0.3f, 0, -0.15f), fb + new Vector3(0.3f, 0.06f, 0.15f), 22, 0.07f);
        Buches(c, fb + new Vector3(0, 0.06f, 0), 0.6f, 85f);
        Flamme(c.flammes, fb + new Vector3(-0.1f, 0.08f, 0), 0.3f, 0.07f); Flamme(c.flammes, fb + new Vector3(0.12f, 0.08f, 0.02f), 0.24f, 0.06f); Flamme(c.flammes, fb + new Vector3(0.0f, 0.08f, 0.05f), 0.36f, 0.07f);
        Lumiere(c, "Feu_Atre", fb + new Vector3(0, 0.45f, 0.35f), new Color(1f, 0.55f, 0.25f), 1.7f, 4.2f, true);
        // manteau : bougies, grimoire, sablier
        Bougie(c, new Vector3(-0.55f, y + 1.2f, -1.85f), 0.32f); Bougie(c, new Vector3(0.52f, y + 1.2f, -1.88f), 0.28f, true);
        mb.Box(new Vector3(0.12f, y + 1.235f, -1.86f), new Vector3(0.26f, 0.07f, 0.18f), Quaternion.Euler(0, 8, 0), BleuNuit);
        mb.Prisme(new Vector3(-0.22f, y + 1.2f, -1.86f), 0.05f, 0.05f, 0.02f, 6, Or);
        mb.Prisme(new Vector3(-0.22f, y + 1.22f, -1.86f), 0.035f, 0.008f, 0.07f, 6, Tan);
        mb.Prisme(new Vector3(-0.22f, y + 1.29f, -1.86f), 0.008f, 0.035f, 0.07f, 6, Tan);
        mb.Prisme(new Vector3(-0.22f, y + 1.36f, -1.86f), 0.05f, 0.05f, 0.02f, 6, Or);
        // lit contre le mur gauche, coffre au pied
        Poser(c, DungeonDir + "bed_frame", new Vector3(-1.19f, y, -1.12f), 0f, 0.68f, true);
        Poser(c, DungeonDir + "trunk_small_A", new Vector3(-1.2f, y, 0.2f), 90f, 0.7f, true);
        // bibliothèque de grimoires dans l'angle arrière droit
        var kb = new Cadre(new Vector3(0.74f, y, p.zbi), Vector3.forward);
        float[] hb = Etagere(c, kb, p.xi - 0.74f, 0.36f, 2.3f, 5, BoisSombre);
        for (int i = 0; i < hb.Length; i++) Livres(c, kb, 0.08f, p.xi - 0.74f - 0.08f, hb[i], 0.26f, 0.22f, 0.36f);
        // bureau sous la fenêtre avant droite : éclat de Nyxessa sur son support, grimoires, bougies
        var kd = new Cadre(new Vector3(p.xi, y, 0.12f), Vector3.left);   // u vers +z : de 0,12 à 1,42
        Table(c, kd, 1.3f, 0.6f, 0.8f, BoisBrun, BoisSombre, "Bureau");
        Poser(c, ToolsDir + "journal_open", kd.P(0.72f, 0.82f, 0.32f), new Vector3(0, 90f, 0), 0.45f, false);
        Poser(c, ToolsDir + "journal_closed", kd.P(0.2f, 0.8f + 0.12f, 0.25f), new Vector3(90, 90f, 0), 0.38f, false);
        mb.Box(kd.P(1.08f, 0.83f, 0.28f), new Vector3(0.28f, 0.06f, 0.2f), Quaternion.Euler(0, 12, 0), BleuNuit);
        mb.Box(kd.P(1.08f, 0.885f, 0.28f), new Vector3(0.25f, 0.05f, 0.18f), Quaternion.Euler(0, -4, 0), Rouge);
        Bougie(c, kd.P(0.98f, 0.8f, 0.47f), 0.3f); Bougie(c, kd.P(1.16f, 0.8f, 0.5f), 0.24f, true);
        Vector3 socle = kd.P(0.38f, 0.8f, 0.3f);
        mb.Prisme(socle, 0.075f, 0.06f, 0.04f, 6, BoisSombre);
        mb.Prisme(socle + Vector3.up * 0.04f, 0.04f, 0.05f, 0.03f, 6, Or);
        for (int i = 0; i < 3; i++) { float a = i * 120f * Mathf.Deg2Rad; mb.Box(socle + new Vector3(Mathf.Sin(a) * 0.045f, 0.1f, Mathf.Cos(a) * 0.045f), new Vector3(0.012f, 0.07f, 0.012f), Quaternion.Euler(0, i * 120f, 12f), Or); }   // griffes
        Gemme(mb, socle + Vector3.up * 0.065f, 0.17f, 0.055f, VertNyxessa);
        Poser(c, DungeonDir + "chair", new Vector3(0.9f, y, 0.8f), 90f, 0.72f, true);
        Lumiere(c, "Lampe_Bureau", kd.P(0.8f, 1.35f, 0.5f), new Color(1f, 0.74f, 0.45f), 1.0f, 3.6f, false);
        // râtelier du bâton contre le mur droit, entre la fenêtre et la bibliothèque : deux fourches, bâton droit, godet
        float zb = -1.52f, xb = p.xi - 0.11f;
        foreach (float hr in new[] { 0.95f, 1.72f })
        {
            mb.BoxMinMax(new Vector3(p.xi - 0.2f, y + hr - 0.03f, zb - 0.09f), new Vector3(p.xi, y + hr + 0.03f, zb - 0.05f), BoisSombre);
            mb.BoxMinMax(new Vector3(p.xi - 0.2f, y + hr - 0.03f, zb + 0.05f), new Vector3(p.xi, y + hr + 0.03f, zb + 0.09f), BoisSombre);
            mb.BoxMinMax(new Vector3(p.xi - 0.03f, y + hr - 0.1f, zb - 0.12f), new Vector3(p.xi, y + hr + 0.1f, zb + 0.12f), Colombage);
        }
        mb.Prisme(new Vector3(xb, y, zb), 0.07f, 0.06f, 0.07f, 6, Fer);
        Poser(c, WeaponsDir + "staff_B", new Vector3(xb, y + 0.86f * 0.82f + 0.03f, zb), new Vector3(0, 90f, 0), 0.82f, false);
        // pupitre au milieu de la pièce : carte du village posée dessus, face à la porte
        Vector3 pu = new Vector3(-0.38f, y, 0.3f); Quaternion rp = Quaternion.Euler(0, -15f, 0);
        mb.Prisme(pu, 0.22f, 0.2f, 0.05f, 6, BoisSombre, 0f, true);
        mb.Prisme(pu + Vector3.up * 0.05f, 0.05f, 0.05f, 0.85f, 6, BoisBrun, 0f, false);
        mb.Box(pu + Vector3.up * 0.97f, new Vector3(0.62f, 0.035f, 0.44f), rp * Quaternion.Euler(22f, 0, 0), BoisBrun);
        mb.Box(pu + rp * new Vector3(0, 0.88f, 0.22f), new Vector3(0.6f, 0.03f, 0.03f), rp, BoisSombre);   // arrêt
        Poser(c, ToolsDir + "map", pu + Vector3.up * 1.0f, (rp * Quaternion.Euler(22f, 0, 0)).eulerAngles, 0.28f, false);
        ColBox(c, "Meuble_Pupitre", pu + new Vector3(-0.3f, 0, -0.27f), pu + new Vector3(0.3f, 1.1f, 0.27f));
        // croquis épinglés au mur de façade, à gauche de la porte : la relique (éclat vert, ceinture), la carte du village
        var kc = new Cadre(new Vector3(-0.64f, y, p.zfi), Vector3.back);   // u vers -x : de -0,64 à -1,56
        kc.Box(mb, 0f, 1.08f, 0f, 0.92f, 1.92f, 0.03f, BoisBrun);
        kc.Box(mb, 0.06f, 1.2f, 0.03f, 0.42f, 1.7f, 0.035f, Lin);                    // croquis de la relique
        Vector3 cr = kc.P(0.24f, 1.47f, 0.037f);
        mb.Poly(new[] { cr + kc.u * 0.0f + Vector3.up * 0.13f, cr + kc.u * 0.06f, cr - Vector3.up * 0.09f, cr - kc.u * 0.06f }, kc.n, VertNyxessa);
        kc.Box(mb, 0.1f, 1.28f, 0.035f, 0.38f, 1.3f, 0.037f, Taupe);                // rocher et ceinture, au trait
        kc.Box(mb, 0.08f, 1.46f, 0.035f, 0.14f, 1.475f, 0.037f, Taupe); kc.Box(mb, 0.34f, 1.46f, 0.035f, 0.40f, 1.475f, 0.037f, Taupe);
        kc.Box(mb, 0.48f, 1.3f, 0.03f, 0.86f, 1.62f, 0.035f, Tan);                  // plan du village : six maisons autour du centre
        Vector3 cp = kc.P(0.67f, 1.46f, 0.037f);
        for (int i = 0; i < 6; i++) { float a = (40f + 60f * i) * Mathf.Deg2Rad; mb.Box(cp + kc.u * Mathf.Sin(a) * 0.11f + Vector3.up * Mathf.Cos(a) * 0.11f, new Vector3(0.025f, 0.025f, 0.004f), kc.r, BoisSombre); }
        mb.Box(cp, new Vector3(0.03f, 0.03f, 0.004f), kc.r, VertNyxessa);
        foreach (Vector3 q in new[] { kc.P(0.24f, 1.68f, 0.04f), kc.P(0.67f, 1.6f, 0.04f) }) mb.Box(q, new Vector3(0.025f, 0.025f, 0.02f), kc.r, Rouge);   // punaises
        // grimoires empilés au sol près de la bibliothèque
        for (int i = 0; i < 4; i++) mb.Box(new Vector3(0.5f, y + 0.035f + i * 0.07f, -1.65f), new Vector3(0.3f - i * 0.02f, 0.065f, 0.22f), Quaternion.Euler(0, 15f * i - 20f, 0), i % 2 == 0 ? BleuNuit : Rouge);
        Tapis(c, new Vector3(-0.2f, y + 0.002f, 0.55f), 0.85f, BleuNuit, Taupe);
        Ancre(c, "Villageois", new Vector3(1.0f, y, -1.3f), 0f);
        Ancre(c, "Echange", pu + new Vector3(0, 1.0f, 0), -15f);
    }
    // ---------------- Taverne (Maison_1_A ; décision de Quentin du 26/09/2026) ----------------
    // De jour, on s'y restaure et on y boit une bière ; « payer une tournée » enivre tout le monde quelques secondes
    // (runtime : Taverne). Âtre au fond, comptoir le long du mur droit (le tavernier derrière, face à la salle), tonneaux
    // en perce dans l'angle du fond, deux tables et leurs tabourets à gauche, chopes et assiettes posées, bougies.
    const string AventuriersDir = "Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/";
    static void MeublerTaverne(Contexte c)
    {
        Piece p = c.p; MB mb = c.meubles; float y = p.yF;
        // âtre (comme chez le sorcier)
        mb.BoxMinMax(new Vector3(-0.62f, y, p.zbi), new Vector3(0.62f, y + 0.18f, -1.72f), Pierre);
        mb.BoxMinMax(new Vector3(-0.62f, y + 0.18f, p.zbi), new Vector3(-0.42f, y + 1.05f, -1.8f), PierreClaire);
        mb.BoxMinMax(new Vector3(0.42f, y + 0.18f, p.zbi), new Vector3(0.62f, y + 1.05f, -1.8f), PierreClaire);
        mb.BoxMinMax(new Vector3(-0.72f, y + 1.05f, p.zbi), new Vector3(0.72f, y + 1.2f, -1.7f), Colombage);
        mb.BoxMinMax(new Vector3(-0.5f, y + 1.2f, p.zbi), new Vector3(0.5f, p.Plafond(0.5f) + 0.05f, -1.9f), PierreClaire);
        mb.Quad(new Vector3(-0.42f, y + 0.18f, p.zbi + 0.01f), new Vector3(0.42f, y + 0.18f, p.zbi + 0.01f), new Vector3(0.42f, y + 1.05f, p.zbi + 0.01f), new Vector3(-0.42f, y + 1.05f, p.zbi + 0.01f), Vector3.forward, Suie);
        ColBox(c, "Meuble_Atre", new Vector3(-0.72f, y, p.zbi), new Vector3(0.72f, y + 1.2f, -1.7f));
        Vector3 fb = new Vector3(0f, y + 0.18f, -2.0f);
        Braises(c, fb + new Vector3(-0.3f, 0, -0.15f), fb + new Vector3(0.3f, 0.06f, 0.15f), 22, 0.07f);
        Buches(c, fb + new Vector3(0, 0.06f, 0), 0.6f, 95f);
        Flamme(c.flammes, fb + new Vector3(-0.1f, 0.08f, 0), 0.3f, 0.07f); Flamme(c.flammes, fb + new Vector3(0.12f, 0.08f, 0.02f), 0.26f, 0.06f); Flamme(c.flammes, fb + new Vector3(0.0f, 0.08f, 0.05f), 0.36f, 0.07f);
        Lumiere(c, "Feu_Atre", fb + new Vector3(0, 0.45f, 0.35f), new Color(1f, 0.55f, 0.25f), 1.8f, 4.5f, true);
        // manteau : chopes et bougie
        Poser(c, AventuriersDir + "mug_full", new Vector3(-0.5f, y + 1.2f, -1.84f), 20f, 0.3f, false);
        Poser(c, AventuriersDir + "mug_full", new Vector3(0.45f, y + 1.2f, -1.86f), -30f, 0.3f, false);
        Bougie(c, new Vector3(0.05f, y + 1.2f, -1.86f), 0.3f);
        // comptoir le long du mur droit (le tavernier passe derrière)
        float x0 = 0.72f, x1 = 1.02f, z0 = -1.3f, z1 = 0.9f, h = 0.88f;
        mb.BoxMinMax(new Vector3(x0, y, z0), new Vector3(x1, y + h - 0.06f, z1), BoisSombre);
        mb.BoxMinMax(new Vector3(x0 - 0.06f, y + h - 0.06f, z0 - 0.04f), new Vector3(x1 + 0.08f, y + h, z1 + 0.04f), BoisBrun);
        mb.BoxMinMax(new Vector3(x0 - 0.02f, y + 0.02f, z0), new Vector3(x0, y + 0.12f, z1), BoisNoir);   // plinthe
        for (float z = z0 + 0.35f; z < z1; z += 0.55f) mb.BoxMinMax(new Vector3(x0 - 0.02f, y + 0.12f, z - 0.03f), new Vector3(x0, y + h - 0.08f, z + 0.03f), Colombage);   // montants
        ColBox(c, "Meuble_Comptoir", new Vector3(x0 - 0.06f, y, z0 - 0.04f), new Vector3(x1 + 0.08f, y + h, z1 + 0.04f));
        // sur le comptoir : chopes, bouteilles, assiette
        float yc = y + h;
        Poser(c, AventuriersDir + "mug_full", new Vector3(0.84f, yc, 0.55f), 70f, 0.3f, false);
        Poser(c, AventuriersDir + "mug_full", new Vector3(0.9f, yc, 0.3f), -20f, 0.3f, false);
        Poser(c, AventuriersDir + "mug_empty", new Vector3(0.86f, yc, -0.9f), 40f, 0.3f, false);
        Poser(c, DungeonDir + "bottle_A_brown", new Vector3(0.95f, yc, -1.1f), 0f, 0.28f, false);
        Poser(c, DungeonDir + "bottle_B_green", new Vector3(0.93f, yc, -0.98f), 30f, 0.28f, false);
        Poser(c, DungeonDir + "plate_food_A", new Vector3(0.87f, yc, -0.45f), 15f, 0.22f, false);
        Lumiere(c, "Lampe_Comptoir", new Vector3(1.0f, y + 1.9f, -0.2f), new Color(1f, 0.72f, 0.42f), 1.0f, 3.6f, false);
        Bougie(c, new Vector3(0.9f, yc, 0.8f), 0.26f, true);
        // tonneaux en perce dans l'angle du fond, derrière le comptoir ; pile de tonnelets au fond à gauche
        Poser(c, DungeonDir + "keg", new Vector3(1.3f, y, -1.82f), -90f, 0.4f, true);
        Poser(c, DungeonDir + "barrel_small_stack", new Vector3(-1.3f, y, -1.9f), 90f, 0.42f, true);
        Poser(c, DungeonDir + "barrel_small", new Vector3(-1.42f, y, 1.97f), 30f, 0.45f, true);
        // deux tables et leurs tabourets, côté gauche
        // (le passage de la porte au comptoir reste libre : tabourets devant et derrière les tables, pas côté allée)
        foreach (float zt in new[] { -0.6f, 0.85f })
        {
            const float xt = -1.12f;
            Poser(c, DungeonDir + "table_small", new Vector3(xt, y, zt), 0f, 0.74f, true);
            Poser(c, DungeonDir + "stool", new Vector3(xt, y, zt - 0.62f), c.rng.Next(360), 0.85f, true);
            Poser(c, DungeonDir + "stool", new Vector3(xt + 0.1f, y, zt + 0.62f), c.rng.Next(360), 0.85f, true);
            Poser(c, AventuriersDir + "mug_full", new Vector3(xt - 0.12f, y + 0.74f, zt + 0.12f), c.rng.Next(360), 0.3f, false);
            Poser(c, AventuriersDir + "mug_empty", new Vector3(xt + 0.15f, y + 0.74f, zt - 0.18f), c.rng.Next(360), 0.3f, false);
            Poser(c, DungeonDir + "plate_food_B", new Vector3(xt + 0.05f, y + 0.74f, zt + 0.02f), c.rng.Next(360), 0.22f, false);
            Bougie(c, new Vector3(xt - 0.25f, y + 0.74f, zt - 0.2f), 0.22f);
        }
        Tapis(c, new Vector3(0.0f, y + 0.002f, 0.4f), 0.75f, Rouge, Tan);
        Ancre(c, "Villageois", new Vector3(1.38f, y, -0.2f), -90f);
        Ancre(c, "Echange", new Vector3(0.87f, y + 0.9f, -0.2f), -90f);
    }

    // Gemme low poly (éclat de Nyxessa) : couronne de six sommets, pointe haute et pointe basse.
    public static void Gemme(MB mb, Vector3 bas, float h, float r, Vector2 texel)
    {
        Vector3 top = bas + Vector3.up * h, mid = bas + Vector3.up * h * 0.35f;
        var ring = new Vector3[6];
        for (int i = 0; i < 6; i++) { float a = (30f + 60f * i) * Mathf.Deg2Rad; ring[i] = mid + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * r; }
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6; Vector3 nn = (ring[i] + ring[j]) * 0.5f - mid;
            mb.Poly(new[] { ring[i], ring[j], top }, nn + Vector3.up * 0.4f, texel, 0.03f);
            mb.Poly(new[] { ring[i], ring[j], bas }, nn - Vector3.up * 0.6f, texel, -0.03f);
        }
    }
}
