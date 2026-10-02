using System.Collections.Generic;
using System.IO;
using System.Text;
using Deathless.Donjon.Terrasses;
using Deathless.Jeu.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Outils du générateur de donjon « terrasses » (02/10/2026) : banc Assets/Scenes/Dev/DonjonBanc.unity (créé et ouvert EN
// ADDITIF par l'API de l'éditeur : la scène active d'un autre agent n'est jamais remplacée ni écrite), captures
// (Assets/Screenshots/donjon_terrasses_g<graine>_{dessus,joueur,iso}.png, Camera.Render sans Play, rendu limité à la
// scène du banc), contrôle de N graines (plan pur) et bouton « Générer » dans l'inspecteur du constructeur.
public static class DonjonTerrassesMenu
{
    public const string Scene = "Assets/Scenes/Dev/DonjonBanc.unity";
    const string DossierMat = "Assets/Art/Donjon/Terrasses";
    const string MatPierre = DossierMat + "/DonjonTerrasses_Pierre.mat";
    const string MatFlamme = DossierMat + "/DonjonTerrasses_Flamme.mat";
    const string Heros = "Assets/Jeu/Prefabs/Heros_Paladin.prefab";
    static readonly Vector3 Position = new Vector3(3000f, 0f, 3000f);     // loin de la carte (scènes ouvertes ensemble)
    public static readonly int[] GrainesCaptures = { 9, 12, 16, 17, 21, 30 };

    [MenuItem("Deathless/Donjon/Terrasses/Créer le banc (scène)")]
    public static void MenuCreer() { Debug.Log(CreerBanc()); }

    [MenuItem("Deathless/Donjon/Terrasses/Captures des graines du banc")]
    public static void MenuCaptures() { Debug.Log(Captures(GrainesCaptures)); }

    [MenuItem("Deathless/Donjon/Terrasses/Contrôler 2000 graines (plan)")]
    public static void MenuValider() { Debug.Log(Valider(1, 2000)); }

    public static void Materiaux(out Material pierre, out Material flamme)
    {
        Directory.CreateDirectory(DossierMat);
        pierre = AssetDatabase.LoadAssetAtPath<Material>(MatPierre);
        if (pierre == null)
        {
            pierre = new Material(Shader.Find("Deathless/VertexColorLit")) { name = "DonjonTerrasses_Pierre" };
            pierre.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(pierre, MatPierre);
        }
        flamme = AssetDatabase.LoadAssetAtPath<Material>(MatFlamme);
        if (flamme == null)
        {
            flamme = new Material(Shader.Find("Relic/VertexColorUnlit")) { name = "DonjonTerrasses_Flamme" };
            AssetDatabase.CreateAsset(flamme, MatFlamme);
        }
    }

    static void Ambiance()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.71f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.50f, 0.52f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.26f, 0.31f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
        RenderSettings.fogStartDistance = 30f; RenderSettings.fogEndDistance = 110f;
        RenderSettings.sun = null;
    }

    public static string CreerBanc()
    {
        if (EditorApplication.isPlaying) return "refusé : éditeur en Play";
        Material pierre, flamme;
        Materiaux(out pierre, out flamme);
        var dejaOuverte = SceneManager.GetSceneByPath(Scene);
        if (dejaOuverte.IsValid() && dejaOuverte.isLoaded) EditorSceneManager.CloseScene(dejaOuverte, true);
        var active = SceneManager.GetActiveScene();
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(sc);
        try
        {
            Ambiance();
            var racine = new GameObject("DonjonBanc");
            racine.transform.position = Position;
            var ct = racine.AddComponent<ConstructeurTerrasses>();
            ct.materiauPierre = pierre; ct.materiauFlamme = flamme; ct.graine = 1;
            var camGo = new GameObject("CameraBanc");
            camGo.transform.SetParent(racine.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.04f, 0.05f, 0.06f);
            var donnees = camGo.AddComponent<UniversalAdditionalCameraData>();
            donnees.renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            var vol = new GameObject("Volume").AddComponent<Volume>();
            vol.transform.SetParent(racine.transform, false);
            vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/VFX/_Ambiance/Lueur.asset");
            var heros = Modele(racine.transform);
            var banc = racine.AddComponent<BancDonjonTerrasses>();
            banc.constructeur = ct; banc.cam = cam; banc.heros = heros;
            banc.Regenerer(1);
            EditorSceneManager.MarkSceneDirty(sc);
            Directory.CreateDirectory("Assets/Scenes/Dev");
            EditorSceneManager.SaveScene(sc, Scene);
            return "Banc créé : " + Scene + " ; " + ct.Plan.Resume();
        }
        finally
        {
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            EditorSceneManager.CloseScene(sc, true);
        }
    }

    static Transform Modele(Transform parent)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Heros);
        if (src == null) return null;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var modele = inst.transform.Find("Modele");
        GameObject h = modele != null ? Object.Instantiate(modele.gameObject) : null;
        Object.DestroyImmediate(inst);
        if (h == null) return null;
        h.name = "Heros_Echelle";
        h.transform.SetParent(parent, false);
        foreach (var a in h.GetComponentsInChildren<Animator>()) a.enabled = false;
        return h.transform;
    }

    /// Ouvre le banc en additif (s'il ne l'est pas), l'active le temps de l'action, puis rend la main.
    static string AvecBanc(System.Func<Scene, BancDonjonTerrasses, string> action)
    {
        if (EditorApplication.isPlaying) return "refusé : éditeur en Play";
        if (!File.Exists(Scene)) CreerBanc();
        var active = SceneManager.GetActiveScene();
        var sc = SceneManager.GetSceneByPath(Scene);
        bool ouverte = sc.IsValid() && sc.isLoaded;
        if (!ouverte) sc = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(sc);
        try
        {
            BancDonjonTerrasses banc = null;
            foreach (var go in sc.GetRootGameObjects()) { banc = go.GetComponentInChildren<BancDonjonTerrasses>(); if (banc != null) break; }
            if (banc == null) return "banc introuvable dans " + Scene;
            return action(sc, banc);
        }
        finally
        {
            if (active.IsValid() && active.isLoaded && active != sc) SceneManager.SetActiveScene(active);
            if (!ouverte) EditorSceneManager.CloseScene(sc, true);
        }
    }

    public static string Captures(int[] graines)
    {
        return AvecBanc((sc, banc) =>
        {
            var sb = new StringBuilder();
            foreach (int g in graines)
            {
                banc.Regenerer(g);
                var ct = banc.constructeur;
                var defNav = ct.VerifierNavMesh();
                sb.Append(ct.Plan.Resume()).Append(" | NavMesh : ").Append(defNav.Count == 0 ? "ok" : string.Join(", ", defNav)).Append(" | ");
                foreach (var v in new[] { BancDonjonTerrasses.Vue.Dessus, BancDonjonTerrasses.Vue.Joueur, BancDonjonTerrasses.Vue.Iso })
                {
                    banc.vue = v;
                    banc.PlacerCamera();
                    string nom = "donjon_terrasses_g" + g + "_" + v.ToString().ToLowerInvariant();
                    Rendre(banc.cam, sc, nom);
                    sb.Append(nom).Append(' ');
                }
                // vue du joueur depuis la plus haute terrasse, tournée vers la grande salle
                Terrasse haute = null;
                foreach (var t in ct.Plan.terrasses) if (haute == null || t.niveau > haute.niveau || (t.niveau == haute.niveau && t.r.Largeur * t.r.Profondeur > haute.r.Largeur * haute.r.Profondeur)) haute = t;
                if (haute != null)
                {
                    float hx = haute.r.CentreX, hz = Mathf.Max(haute.r.z0 + 1.6f, Mathf.Min(haute.r.z0 + 3.2f, haute.r.z1 - 6f));   // la caméra (5,1 m derrière) reste dans la salle
                    if (ct.Plan.CelluleEn(hx, hz).genre != GenreCellule.Sol) hz = haute.r.CentreZ;
                    string nom = "donjon_terrasses_g" + g + "_terrasse";
                    PoserVue(banc, sc, hx, haute.Hauteur, hz, 180f, nom);
                    sb.Append(nom).Append(' ');
                }
                sb.Append(CapturesMecanismes(banc, sc, g));
                sb.AppendLine();
            }
            banc.vue = BancDonjonTerrasses.Vue.Joueur;
            banc.Regenerer(graines[0]);
            return sb.ToString();
        });
    }

    /// Vues rapprochées des pièces fermées : la porte à serrure, la paroi secrète fermée puis ouverte, son déclencheur.
    static string CapturesMecanismes(BancDonjonTerrasses banc, Scene sc, int g)
    {
        var sb = new StringBuilder();
        var P = banc.constructeur.Plan;
        foreach (var p in P.pieces)
        {
            if (p.genre == GenrePiece.Libre) continue;
            var n = new Vector3(PlanTerrasses.Dx(p.arche.dir), 0f, PlanTerrasses.Dz(p.arche.dir));
            var cible = new Vector3(p.arche.x, p.sol, p.arche.z);
            string nom = "donjon_terrasses_g" + g + (p.genre == GenrePiece.Verrouillee ? "_porte_" + p.serrure.ToString().ToLowerInvariant() : "_paroi_secrete");
            VueRapprochee(banc, sc, cible, n, 2.0f, nom);
            sb.Append(nom).Append(' ');
            if (p.genre != GenrePiece.Secrete) continue;
            var porte = banc.constructeur.Portes.Find(x => x.piece == p.index);
            if (p.declencheur >= 0)
            {
                var d = P.declencheurs[p.declencheur];
                string nd = "donjon_terrasses_g" + g + "_declencheur_" + (d.genre == GenreDeclencheur.PlaqueSol ? "plaque" : "bouton");
                var pos = new Vector3(d.pose.x, d.pose.y, d.pose.z);
                if (d.genre == GenreDeclencheur.PlaqueSol) VueRapprochee(banc, sc, pos, MeilleureDirection(P, pos), 0.1f, nd);
                else
                {
                    float r = d.pose.rotY * Mathf.Deg2Rad;
                    VueRapprochee(banc, sc, pos - Vector3.up * 1.1f, new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)), 1.1f, nd);
                }
                sb.Append(nd).Append(' ');
            }
            if (porte != null)
            {
                porte.Ouvrir();
                VueRapprochee(banc, sc, cible, n, 2.0f, nom + "_ouverte");
                sb.Append(nom).Append("_ouverte ");
            }
        }
        return sb.ToString();
    }

    /// Hauteur du plein le plus haut d'une colonne du plan qui gênerait la vue (mur : infini).
    static float Obstacle(PlanTerrasses P, float x, float z)
    {
        int i = P.CelluleI(x), j = P.CelluleJ(z);
        if (!P.DansGrille(i, j) || !P.DansVolume(i, j)) return 1e6f;
        var c = P.cellules[P.Index(i, j)];
        if (c.genre == GenreCellule.Massif || c.genre == GenreCellule.Hors) return 1e6f;
        if (c.genre == GenreCellule.Escalier) return c.sol + PlanTerrasses.HauteurNiveau * 2f;
        if (c.obstacle) return 1e6f;
        return c.sol;
    }

    static float Recul(PlanTerrasses P, Vector3 cible, Vector3 n, float sol)
    {
        for (float d = 7f; d >= 2.5f; d -= 0.5f)
        {
            bool ok = true;
            Vector3 lat = Vector3.Cross(Vector3.up, n);
            for (float k = 0.8f; k <= d + 0.01f && ok; k += 0.4f)
                for (int q = -1; q <= 1 && ok; q++)
                    ok = Obstacle(P, cible.x + n.x * k + lat.x * q * 0.9f, cible.z + n.z * k + lat.z * q * 0.9f) < sol + 1.6f;
            if (ok) return d;
        }
        return 2.5f;
    }

    static Vector3 MeilleureDirection(PlanTerrasses P, Vector3 cible)
    {
        Vector3 best = Vector3.back; float bd = -1f;
        foreach (var n in new[] { Vector3.back, Vector3.forward, Vector3.left, Vector3.right })
        {
            float d = Recul(P, cible, n, cible.y);
            if (d > bd + 0.01f) { bd = d; best = n; }
        }
        return best;
    }

    /// Vue rapprochée d'un mécanisme : caméra reculée dans l'espace libre devant lui (7 m au plus), héros sur le côté.
    static void VueRapprochee(BancDonjonTerrasses banc, Scene sc, Vector3 cible, Vector3 n, float hauteurVisee, string nom)
    {
        var ct = banc.constructeur;
        var P = ct.Plan;
        ct.AfficherVoute(true); ct.AfficherReperes(false); ct.AfficherCoupe(false);
        float d = Recul(P, cible, n, cible.y);
        Vector3 lat = Vector3.Cross(Vector3.up, n);
        if (banc.heros != null)
        {
            banc.heros.position = ct.Monde(cible.x + n.x * Mathf.Min(2f, d - 0.5f) + lat.x * 1.5f, cible.y, cible.z + n.z * Mathf.Min(2f, d - 0.5f) + lat.z * 1.5f);
            banc.heros.rotation = Quaternion.LookRotation(-n, Vector3.up);
        }
        var cam = banc.cam;
        cam.orthographic = false; cam.fieldOfView = 55f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
        Vector3 pos = ct.Monde(cible.x + n.x * d, cible.y + 2.6f, cible.z + n.z * d);
        Vector3 vise = ct.Monde(cible.x, cible.y + hauteurVisee, cible.z);
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(vise - pos, Vector3.up));
        Rendre(cam, sc, nom);
        banc.PlacerHeros();
    }

    /// Vue de jeu supplémentaire : héros posé en (x, z) du plan, regard selon `lacet`.
    public static string CaptureVue(int g, float x, float y, float z, float lacet, string nom, bool voute = true)
    {
        return AvecBanc((sc, banc) =>
        {
            if (banc.constructeur.graine != g || banc.constructeur.Racine == null) banc.Regenerer(g);
            PoserVue(banc, sc, x, y, z, lacet, nom, voute);
            banc.PlacerHeros();
            return nom;
        });
    }

    static void PoserVue(BancDonjonTerrasses banc, Scene sc, float x, float y, float z, float lacet, string nom, bool voute = true)
    {
        {
            var ct = banc.constructeur;
            ct.AfficherVoute(voute); ct.AfficherReperes(false); ct.AfficherCoupe(false);
            var hp = ct.Monde(x, y, z);
            if (banc.heros != null) { banc.heros.position = hp; banc.heros.rotation = Quaternion.Euler(0f, lacet, 0f); }
            Quaternion rot = Quaternion.Euler(22f, lacet, 0f);
            Vector3 pivot = hp + Vector3.up * 1.6f, epaule = pivot + rot * Vector3.right * 0.6f;
            var cam = banc.cam;
            cam.orthographic = false; cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
            cam.transform.SetPositionAndRotation(epaule - rot * Vector3.forward * 5.5f, rot);
            Rendre(cam, sc, nom);
            banc.PlacerHeros();
        }
    }

    const int W = 1920, H = 1080;

    static void Rendre(Camera cam, Scene sc, string nom)
    {
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var ancienne = cam.scene;
        cam.scene = sc;                    // n'affiche que le banc (pas la carte ouverte à côté)
        cam.targetTexture = rt;
        cam.aspect = (float)W / H;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null;
        cam.scene = ancienne;
        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllBytes("Assets/Screenshots/" + nom + ".png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
    }

    /// Contrôle du plan pur sur une plage de graines : conformité, essais, variantes, niveaux, pièces, temps.
    public static string Valider(int de, int nb)
    {
        var plan = new PlanTerrasses();
        int ok = 0, relances = 0, verrou = 0, secret = 0, pieces = 0;
        var parVar = new int[8]; var niv = new int[4];
        var defauts = new Dictionary<string, int>();
        var chrono = System.Diagnostics.Stopwatch.StartNew();
        for (int g = de; g < de + nb; g++)
        {
            if (plan.Generer(g)) ok++;
            else foreach (var d in plan.defauts) { string k = d.Split(':')[0]; defauts[k] = defauts.TryGetValue(k, out int c) ? c + 1 : 1; }
            if (plan.Essai > 0) relances++;
            parVar[plan.Variante]++; niv[plan.Niveaux]++;
            foreach (var p in plan.pieces) { pieces++; if (p.genre == GenrePiece.Verrouillee) verrou++; if (p.genre == GenrePiece.Secrete) secret++; }
        }
        var sb = new StringBuilder();
        sb.Append(ok).Append('/').Append(nb).Append(" plans conformes (").Append((chrono.ElapsedMilliseconds / (double)nb).ToString("0.00")).Append(" ms par graine), ")
          .Append(relances).Append(" relancés ; variantes ");
        for (int v = 1; v < 8; v++) sb.Append('v').Append(v).Append(':').Append(parVar[v]).Append(' ');
        sb.Append("; 2 niveaux : ").Append(niv[2]).Append(", 3 niveaux : ").Append(niv[3]).Append(" ; pièces cachées ").Append(pieces).Append(" (verrouillées ").Append(verrou).Append(", secrètes ").Append(secret).Append(')');
        foreach (var kv in defauts) sb.Append(" ; ").Append(kv.Value).Append(" × ").Append(kv.Key);
        return sb.ToString();
    }

    /// Contrôle NavMesh (construit) de quelques graines dans le banc.
    public static string ValiderNavMesh(int de, int nb)
    {
        return AvecBanc((sc, banc) =>
        {
            var sb = new StringBuilder();
            int ok = 0;
            float tp = 0f, tc = 0f, tn = 0f;
            for (int g = de; g < de + nb; g++)
            {
                banc.Regenerer(g);
                var d = banc.constructeur.VerifierNavMesh();
                tp += banc.constructeur.TempsPlanMs; tc += banc.constructeur.TempsConstructionMs; tn += banc.constructeur.TempsNavMeshMs;
                if (d.Count == 0) ok++; else sb.Append("graine ").Append(g).Append(" : ").Append(string.Join(", ", d)).Append(" ; ");
            }
            banc.Regenerer(GrainesCaptures[0]);
            return ok + "/" + nb + " graines sans défaut NavMesh ; moyennes : plan " + (tp / nb).ToString("0.0") + " ms, géométrie " + (tc / nb).ToString("0") + " ms, NavMesh " + (tn / nb).ToString("0") + " ms. " + sb;
        });
    }
}

[CustomEditor(typeof(ConstructeurTerrasses))]
public class ConstructeurTerrassesEditeur : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var ct = (ConstructeurTerrasses)target;
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Générer")) Regenerer(ct, ct.graine);
            if (GUILayout.Button("Graine précédente")) Regenerer(ct, ct.graine - 1);
            if (GUILayout.Button("Graine suivante")) Regenerer(ct, ct.graine + 1);
            if (GUILayout.Button("Vider")) ct.Vider();
        }
        if (ct.Plan != null && ct.Racine != null)
        {
            EditorGUILayout.HelpBox(ct.Plan.Resume() + "\nPlan " + ct.TempsPlanMs.ToString("0.0") + " ms, géométrie " + ct.TempsConstructionMs.ToString("0") + " ms, NavMesh "
                + ct.TempsNavMeshMs.ToString("0") + " ms ; " + ct.NbSommets + " sommets, " + ct.NbLumieres + " lumières, " + ct.NbCollisions + " collisions.", MessageType.None);
        }
    }

    static void Regenerer(ConstructeurTerrasses ct, int g)
    {
        var banc = ct.GetComponent<BancDonjonTerrasses>();
        if (banc != null) banc.Regenerer(g); else ct.Generer(g);
    }
}
