using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Vérification des intérieurs (menu Deathless > Niveau > Intérieurs - Vérifier), hors Play ou en Play :
// 1. Visite : un CharacterController à la taille du héros (rayon 0,4 m, hauteur 2 m, pas 0,35 m, pente 45°) part du
//    dehors, passe la porte (le perron et sa rampe pour les maisons B), va devant l'ancre d'échange, puis ressort.
// 2. Murs : depuis le milieu de la pièce, il marche 6 m vers chaque mur ; il doit rester dedans.
// 3. Caméra : réglages de CameraEpaule (main) : pivot à 1,6 m, épaule à 0,6 m, 4,5 m de recul, SphereCast de 0,25 m ;
//    12 lacets x 3 tangages depuis le point client et depuis l'entrée : distance obtenue, caméra restée dans la pièce.
// 4. NavMesh : cuisson temporaire (colliders physiques, comme main) autour de chaque maison : pas de NavMesh dans la
//    pièce ni sur le perron, NavMesh devant la porte ; rien n'est enregistré.
public static class InterieursVerif
{
    public const float Rayon = 0.4f, Hauteur = 2f, Pas = 0.35f, Vitesse = 3.5f;

    [MenuItem("Deathless/Niveau/Intérieurs - Vérifier")]
    public static void Menu() { Debug.Log(Verifier()); }

    public static string Verifier()
    {
        var sb = new System.Text.StringBuilder("Intérieurs - vérification\n");
        Transform racine = GameObject.Find("VillageBlockout/" + InterieursBuilder.Racine) != null ? GameObject.Find("VillageBlockout/" + InterieursBuilder.Racine).transform : null;
        if (racine == null) return "Intérieurs absents : lancer Deathless > Niveau > Intérieurs.";
        Physics.SyncTransforms();
        int ok = 0;
        foreach (Transform it in racine)
        {
            string nom = it.name.Replace("Interieur_", "");
            int i = System.Array.IndexOf(InterieursBuilder.Noms, nom);
            Transform maison = GameObject.Find("VillageBlockout/Maisons/" + InterieursBuilder.Maisons[i]).transform;
            var g = InterieursBuilder.Maisons[i].EndsWith("_A") ? InterieursBuilder.A : InterieursBuilder.B;
            var p = InterieursBuilder.Mesurer(g, maison.localScale.x);
            sb.Append("== " + nom + " (" + InterieursBuilder.Maisons[i] + ")\n");
            bool bon = true;
            // ancres
            Transform av = it.Find("Ancre_Villageois_" + nom), ae = it.Find("Ancre_Echange_" + nom);
            sb.Append("  ancres : villageois " + (av != null) + ", échange " + (ae != null) + "\n");
            bon &= av != null && ae != null;
            // lumières
            int nl = 0, ombres = 0; foreach (Light l in it.GetComponentsInChildren<Light>()) { nl++; if (l.shadows != LightShadows.None) ombres++; }
            sb.Append("  lumières : " + nl + " ponctuelles, " + ombres + " avec ombres\n");
            // visite
            float dx = g.porteX * p.s;
            Vector3 client = it.InverseTransformPoint(ae.position + ae.forward * (nom == "Sorcier" ? 1.2f : nom == "Forgeron" ? 1.1f : 1.0f)); client.y = p.yF;
            var chemin = new List<Vector3>();
            if (g.rampants) { chemin.Add(new Vector3(dx, 0, p.cadreZ + 3f)); chemin.Add(new Vector3(dx, 0, p.cadreZ + 0.6f)); }
            else { chemin.Add(new Vector3(dx, 0, 0.35f * p.s + 2.6f)); chemin.Add(new Vector3(dx, 0, 0.35f * p.s + 0.9f)); chemin.Add(new Vector3(dx, 0, 0.35f * p.s - 0.4f)); }
            chemin.Add(new Vector3(dx, 0, p.zfi - 0.7f));
            chemin.Add(client);
            chemin.Add(new Vector3(dx, 0, p.zfi - 0.7f));
            for (int k = (g.rampants ? 1 : 2); k >= 0; k--) chemin.Add(chemin[k]);
            string detail; float tps;
            int atteints = Visite(it, chemin, out detail, out tps);
            sb.Append("  visite dehors -> porte -> devant l'échange -> dehors : " + atteints + "/" + chemin.Count + " points en " + tps.ToString("F1") + " s" + detail + "\n");
            bon &= atteints == chemin.Count;
            // seuil : traversée de la porte à -8, 0 et +8 cm de l'axe, aller et retour, sans ralentir
            string seuil = Seuil(it, p, g);
            sb.Append("  seuil : " + seuil + "\n");
            bon &= !seuil.Contains("ACCROCHE");
            // battant : ne touche ni mur ni meuble (colliders et objets KayKit)
            string battant = Battant(it);
            sb.Append("  battant : " + battant + "\n");
            bon &= !battant.Contains("TOUCHE");
            // murs
            Vector3 milieu = new Vector3(0, p.yF, (p.zbi + p.zfi) * 0.5f);
            if (nom == "Druide") milieu = new Vector3(0.9f, p.yF, 0.25f);
            if (nom == "Mecano") milieu = new Vector3(0.2f, p.yF, -0.2f);
            if (nom == "Forgeron") milieu = new Vector3(0.4f, p.yF, 0.2f);
            if (nom == "Sorcier") milieu = new Vector3(-0.1f, p.yF, 0.5f);
            int dedans = 0; string sorties = "";
            foreach (Vector3 d in new[] { Vector3.right, Vector3.left, Vector3.back, new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized })
            {
                Vector3 fin = Marche(it, milieu, milieu + d * 6f, 4f);
                bool in_ = fin.x > -p.xi - 0.05f && fin.x < p.xi + 0.05f && fin.z > p.zbi - 0.05f && fin.z < p.zfi + 0.05f;
                if (in_) dedans++; else sorties += " " + d.ToString("F1") + "->" + fin.ToString("F2");
            }
            sb.Append("  murs : " + dedans + "/5 marches contre les murs restent dans la pièce" + sorties + "\n");
            bon &= dedans == 5;
            // caméra
            string cam = Camera3P(it, p, new[] { client, new Vector3(dx, p.yF, p.zfi - 0.7f) });
            sb.Append("  caméra : " + cam + "\n");
            bon &= !cam.Contains("SORTIE");
            // NavMesh
            string nav = Nav(it, p, g);
            sb.Append("  NavMesh : " + nav + "\n");
            bon &= !nav.Contains("ECHEC");
            if (bon) ok++;
        }
        sb.Append("Bilan : " + ok + "/" + racine.childCount + " intérieurs conformes.");
        return sb.ToString();
    }

    static CharacterController Capsule(Transform it, Vector3 local)
    {
        GameObject go = new GameObject("VerifHeros");
        var cc = go.AddComponent<CharacterController>();
        cc.radius = Rayon; cc.height = Hauteur; cc.center = Vector3.up * Hauteur * 0.5f; cc.stepOffset = Pas; cc.slopeLimit = 45f; cc.skinWidth = 0.06f;
        Vector3 w = it.TransformPoint(local);
        RaycastHit h;
        if (Physics.Raycast(w + Vector3.up * 3f, Vector3.down, out h, 10f, ~0, QueryTriggerInteraction.Ignore)) w.y = h.point.y + 0.02f;
        go.transform.position = w;
        Physics.SyncTransforms();
        return cc;
    }

    // Marche d'un point à l'autre (repère de l'intérieur, y ignoré), gravité, jusqu'à `duree` s ; renvoie la position finale (locale).
    static Vector3 Marche(Transform it, Vector3 a, Vector3 b, float duree)
    {
        var cc = Capsule(it, a); float vy = 0f;
        for (float t = 0; t < duree; t += 0.02f)
        {
            Vector3 cible = it.TransformPoint(b); Vector3 d = cible - cc.transform.position; d.y = 0f;
            if (d.magnitude < 0.2f) break;
            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy -= 20f * 0.02f;
            cc.Move(d.normalized * Vitesse * 0.02f + Vector3.up * vy * 0.02f);
        }
        Vector3 fin = it.InverseTransformPoint(cc.transform.position);
        Object.DestroyImmediate(cc.gameObject);
        return fin;
    }

    // Battant ouvert : chevauchements avec les autres colliders (hors plancher et seuil) et avec les objets KayKit.
    static string Battant(Transform it)
    {
        Transform t = it.Find("Collisions/Vantail"); if (t == null) return "absent TOUCHE";
        BoxCollider bc = t.GetComponent<BoxCollider>();
        Vector3 demi = bc.size * 0.5f - Vector3.one * 0.005f;
        string touche = "";
        foreach (Collider col in Physics.OverlapBox(t.TransformPoint(bc.center), demi, t.rotation, ~0, QueryTriggerInteraction.Ignore))
            if (col != bc && col.name != "Sol" && col.name != "Seuil") touche += " " + col.name;
        Transform props = it.Find("Mobilier/KayKit");
        if (props != null)
            foreach (Renderer r in props.GetComponentsInChildren<Renderer>())
            {
                Bounds b = r.bounds; bool dedans = false;
                for (int k = 0; k < 8 && !dedans; k++)
                {
                    Vector3 q = t.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)));
                    dedans = Mathf.Abs(q.x) < demi.x && Mathf.Abs(q.y) < demi.y && Mathf.Abs(q.z) < demi.z;
                }
                if (dedans) touche += " " + r.name;
            }
        Vector3 ang = t.localEulerAngles;
        return "ouvert à " + InterieursBuilder.VantailAngle.ToString("F0") + "°, " + (touche.Length == 0 ? "ne touche rien" : "TOUCHE" + touche);
    }

    // Traversée du seuil : progression minimale par pas (1 = pleine vitesse) en marchant droit à travers la porte.
    static string Seuil(Transform it, InterieursBuilder.Piece p, InterieursBuilder.Gabarit g)
    {
        float dx = g.porteX * p.s, zDehors = g.rampants ? p.cadreZ + 1.4f : 0.35f * p.s - 0.25f, zDedans = p.zfi - 0.8f;
        float pire = 1f; string ou = "";
        foreach (float off in new[] { -0.08f, 0f, 0.08f })
            foreach (bool entre in new[] { true, false })
            {
                Vector3 a = new Vector3(dx + off, 0, entre ? zDehors : zDedans), b = new Vector3(dx + off, 0, entre ? zDedans : zDehors);
                var cc = Capsule(it, a); float vy = 0f;
                var prog = new List<float>();   // progression par pas ; on juge des moyennes sur 0,2 s (une marche coûte un pas)
                for (int n = 0; n < 400; n++)
                {
                    Vector3 cible = it.TransformPoint(b); Vector3 d = cible - cc.transform.position; d.y = 0f;
                    if (d.magnitude < 0.3f) break;
                    if (cc.isGrounded && vy < 0f) vy = -2f;
                    vy -= 20f * 0.02f;
                    Vector3 avant = cc.transform.position;
                    float pasVoulu = Vitesse * 0.02f;
                    cc.Move(d.normalized * pasVoulu + Vector3.up * vy * 0.02f);
                    Vector3 fait = cc.transform.position - avant; fait.y = 0f;
                    prog.Add(Vector3.Dot(fait, d.normalized) / pasVoulu);
                    if (prog.Count >= 10)
                    {
                        float moy = 0f; for (int k = prog.Count - 10; k < prog.Count; k++) moy += prog[k]; moy /= 10f;
                        if (n > 12 && moy < pire) { pire = moy; ou = (entre ? "entrée" : "sortie") + " à " + (off * 100f).ToString("F0") + " cm, z = " + it.InverseTransformPoint(cc.transform.position).z.ToString("F2"); }
                    }
                }
                Object.DestroyImmediate(cc.gameObject);
            }
        return "6 traversées, vitesse minimale sur 0,2 s : " + (pire * 100f).ToString("F0") + " %" + (pire < 0.6f ? " ACCROCHE (" + ou + ")" : " (" + ou + ")");
    }

    public static int Visite(Transform it, List<Vector3> chemin, out string detail, out float temps)
    {
        var cc = Capsule(it, chemin[0]); float vy = 0f; int atteints = 1; temps = 0f; detail = "";
        float yMax = float.MinValue;
        for (int k = 1; k < chemin.Count; k++)
        {
            bool ok = false; Vector3 dernier = cc.transform.position; float bloque = 0f;
            for (float t = 0; t < 12f; t += 0.02f)
            {
                temps += 0.02f;
                Vector3 cible = it.TransformPoint(chemin[k]); Vector3 d = cible - cc.transform.position; d.y = 0f;
                if (d.magnitude < 0.25f) { ok = true; break; }
                if (cc.isGrounded && vy < 0f) vy = -2f;
                vy -= 20f * 0.02f;
                cc.Move(Vector3.ClampMagnitude(d.normalized * Vitesse * 0.02f, d.magnitude) + Vector3.up * vy * 0.02f);
                yMax = Mathf.Max(yMax, cc.transform.position.y);
                if ((cc.transform.position - dernier).magnitude < 0.002f) { bloque += 0.02f; if (bloque > 1f) break; } else bloque = 0f;
                dernier = cc.transform.position;
            }
            if (!ok) { detail = " (bloqué vers le point " + k + " " + chemin[k].ToString("F2") + ", à " + it.InverseTransformPoint(cc.transform.position).ToString("F2") + ")"; break; }
            atteints++;
        }
        Object.DestroyImmediate(cc.gameObject);
        return atteints;
    }

    // Caméra à l'épaule, logique de CameraEpaule (main) : pivot à 1,6 m, épaule à 0,6 m à droite, SphereCast de 0,25 m
    // vers l'arrière sur 4,5 m, recul minimal 0,6 m. `corrigee` : variante proposée pour main (épaule recalée par un
    // SphereCast du pivot vers l'épaule, recul minimal 0,3 m) ; sans elle, une épaule collée à un mur fait partir le
    // SphereCast de l'intérieur du collider (ignoré par PhysX) et la caméra traverse le mur.
    public static Vector3 PositionCamera(Vector3 pieds, float lacet, float tangage, out float dist, bool corrigee = false)
    {
        Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);
        Vector3 pivot = pieds + Vector3.up * 1.6f;
        Vector3 droite = rot * Vector3.right; float epauleD = 0.6f; RaycastHit h;
        if (corrigee && Physics.SphereCast(pivot, 0.25f, droite, out h, 0.6f, ~0, QueryTriggerInteraction.Ignore)) epauleD = Mathf.Max(0f, h.distance - 0.02f);
        Vector3 epaule = pivot + droite * epauleD;
        Vector3 dir = rot * Vector3.back;
        float d = 4.5f;
        if (Physics.SphereCast(epaule, 0.25f, dir, out h, 4.5f, ~0, QueryTriggerInteraction.Ignore) && h.distance > 0f) d = h.distance;
        dist = Mathf.Max(corrigee ? 0.3f : 0.6f, d);
        return epaule + dir * dist;
    }

    static string Camera3P(Transform it, InterieursBuilder.Piece p, Vector3[] points)
    {
        string r = "";
        foreach (bool corrigee in new[] { false, true })
        {
            int n = 0, dehors = 0, proches = 0; float somme = 0f, mini = 99f;
            foreach (Vector3 pt in points)
                foreach (float tang in new[] { -10f, 12f, 40f })
                    for (int k = 0; k < 12; k++)
                    {
                        float lacet = it.eulerAngles.y + k * 30f; float d;
                        Vector3 c = it.InverseTransformPoint(PositionCamera(it.TransformPoint(pt), lacet, tang, out d, corrigee));
                        bool in_ = c.x > -p.xi && c.x < p.xi && c.z > p.zbi && c.z < p.zfi && c.y > p.yF && c.y < p.Plafond(c.x);
                        // sortie par la porte (légitime) : le segment tête -> caméra passe dans l'arc
                        if (!in_ && c.z >= p.zfi)
                        {
                            Vector3 e = it.InverseTransformPoint(it.TransformPoint(pt) + Vector3.up * 1.6f);
                            Vector3 m = Vector3.Lerp(e, c, (p.zfi - e.z) / Mathf.Max(0.001f, c.z - e.z));
                            if (Mathf.Abs(m.x - p.g.porteX * p.s) < 0.55f && m.y < p.yF + 2.3f) in_ = true;
                        }
                        n++; somme += d; mini = Mathf.Min(mini, d); if (!in_) dehors++; if (d < 1.2f) proches++;
                    }
            r += (corrigee ? " | épaule recalée (proposition) : " : "CameraEpaule actuelle : ") + n + " positions, recul moyen " + (somme / n).ToString("F2") + " m (min " + mini.ToString("F2") + "), " + proches + " à moins de 1,2 m, " + (dehors == 0 ? "toujours dans la pièce" : dehors + " derrière un mur" + (corrigee ? " SORTIE" : ""));
        }
        return r;
    }

    static string Nav(Transform it, InterieursBuilder.Piece p, InterieursBuilder.Gabarit g)
    {
        GameObject go = new GameObject("VerifNavMesh");
        go.transform.position = it.position;
        var surf = go.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.Volume; surf.center = new Vector3(0, 2f, 0); surf.size = new Vector3(22f, 12f, 22f);
        surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surf.layerMask = ~0;
        surf.overrideVoxelSize = true; surf.voxelSize = 0.13f;
        string r;
        try
        {
            surf.BuildNavMesh();
            NavMeshHit h;
            float dx = g.porteX * p.s;
            var dedans = new List<Vector3> { new Vector3(0, p.yF, (p.zbi + p.zfi) * 0.5f), new Vector3(dx, p.yF, p.zfi - 0.5f), new Vector3(p.xi - 0.6f, p.yF, p.zbi + 0.6f), new Vector3(-p.xi + 0.6f, p.yF, p.zfi - 0.6f) };
            if (!g.rampants) { dedans.Add(new Vector3(1.5f, p.seuil, 0.28f * p.s)); dedans.Add(new Vector3(dx, p.seuil, 0.3f * p.s)); }
            int trouves = 0; string ou = "";
            foreach (Vector3 q in dedans)
                if (NavMesh.SamplePosition(it.TransformPoint(q), out h, 0.35f, NavMesh.AllAreas)) { trouves++; ou += " " + q.ToString("F1"); }
            Vector3 devant = it.TransformPoint(new Vector3(dx, 0, (g.rampants ? p.cadreZ : 0.35f * p.s + 1.1f) + 1.2f));
            RaycastHit sol; if (Physics.Raycast(devant + Vector3.up * 5f, Vector3.down, out sol, 10f, ~0, QueryTriggerInteraction.Ignore)) devant = sol.point;
            bool dehors = NavMesh.SamplePosition(devant, out h, 0.6f, NavMesh.AllAreas);
            // chemin d'un point devant la porte vers le milieu de la pièce : il ne doit pas y entrer
            bool entre = false;
            if (dehors)
            {
                var path = new NavMeshPath();
                NavMesh.CalculatePath(h.position, it.TransformPoint(dedans[0]), NavMesh.AllAreas, path);
                if (path.corners.Length > 0)
                {
                    Vector3 fin = it.InverseTransformPoint(path.corners[path.corners.Length - 1]);
                    entre = fin.z < p.zfi && fin.x > -p.xi && fin.x < p.xi && fin.z > p.zbi;
                }
            }
            r = "pièce" + (g.rampants ? "" : " et perron") + " : " + trouves + "/" + dedans.Count + " points sur le NavMesh" + ou + " ; devant la porte : " + (dehors ? "oui" : "non") + " ; un chemin entre : " + (entre ? "oui" : "non")
                + (trouves == 0 && dehors && !entre ? " -> OK" : " -> ECHEC");
        }
        finally { surf.RemoveData(); Object.DestroyImmediate(go); }
        return r;
    }

    // ---------------- Captures en Play (menu Deathless > Niveau > Intérieurs - Captures (en Play)) ----------------
    // Un mannequin de 2 m (capsule, CharacterController du héros) fait la visite de chaque maison ; la caméra suit la
    // logique de CameraEpaule (troisième personne). Captures : Assets/Screenshots/interieur_<maison>_entree / _vue (jour)
    // ou _nuit (vue de nuit). Figer la phase avant (Deathless > Village > Ambiance), une image au moins avant la capture.
    [MenuItem("Deathless/Niveau/Intérieurs - Captures (en Play)")]
    public static void CapturesMenu() { Debug.Log(Captures(false)); }

    public static string Captures(bool nuit)
    {
        if (!Application.isPlaying) return "Captures : lancer le mode Play d'abord.";
        var sb = new System.Text.StringBuilder("Captures des intérieurs (" + (nuit ? "nuit" : "jour") + ")\n");
        Transform racine = GameObject.Find("VillageBlockout/" + InterieursBuilder.Racine).transform;
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", new Color(0.72f, 0.69f, 0.64f));
        foreach (Transform it in racine)
        {
            string nom = it.name.Replace("Interieur_", "");
            int i = System.Array.IndexOf(InterieursBuilder.Noms, nom);
            Transform maison = GameObject.Find("VillageBlockout/Maisons/" + InterieursBuilder.Maisons[i]).transform;
            var g = InterieursBuilder.Maisons[i].EndsWith("_A") ? InterieursBuilder.A : InterieursBuilder.B;
            var p = InterieursBuilder.Mesurer(g, maison.localScale.x);
            Transform ae = it.Find("Ancre_Echange_" + nom);
            float dx = g.porteX * p.s;
            Vector3 client = it.InverseTransformPoint(ae.position + ae.forward * (nom == "Sorcier" ? 1.2f : nom == "Forgeron" ? 1.1f : 1.0f)); client.y = p.yF;
            // mannequin
            GameObject man = new GameObject("Mannequin_2m");
            var cc = man.AddComponent<CharacterController>(); cc.radius = Rayon; cc.height = Hauteur; cc.center = Vector3.up; cc.stepOffset = Pas; cc.slopeLimit = 45f; cc.skinWidth = 0.06f;
            GameObject corps = GameObject.CreatePrimitive(PrimitiveType.Capsule); Object.DestroyImmediate(corps.GetComponent<Collider>());
            corps.transform.SetParent(man.transform, false); corps.transform.localPosition = Vector3.up; corps.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            corps.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObject visiere = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(visiere.GetComponent<Collider>());
            visiere.transform.SetParent(man.transform, false); visiere.transform.localPosition = new Vector3(0, 1.65f, 0.33f); visiere.transform.localScale = new Vector3(0.45f, 0.12f, 0.14f);
            visiere.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // visite complète (dehors -> dedans -> dehors)
            var chemin = new List<Vector3>();
            if (g.rampants) { chemin.Add(new Vector3(dx, 0, p.cadreZ + 3f)); chemin.Add(new Vector3(dx, 0, p.cadreZ + 0.6f)); }
            else { chemin.Add(new Vector3(dx, 0, 0.35f * p.s + 2.6f)); chemin.Add(new Vector3(dx, 0, 0.35f * p.s + 0.9f)); chemin.Add(new Vector3(dx, 0, 0.35f * p.s - 0.4f)); }
            chemin.Add(new Vector3(dx, 0, p.zfi - 0.7f)); chemin.Add(client); chemin.Add(new Vector3(dx, 0, p.zfi - 0.7f));
            for (int k = (g.rampants ? 1 : 2); k >= 0; k--) chemin.Add(chemin[k]);
            string detail; float tps;
            int ok = Visite(it, chemin, out detail, out tps);
            sb.Append(nom + " : visite " + ok + "/" + chemin.Count + detail + "\n");
            float lacetMaison = it.eulerAngles.y;
            if (!nuit)
            {
                // porte vue de l'intérieur et du dessus : battant rabattu contre le mur, seuil, allée
                cc.gameObject.SetActive(false);
                sb.Append("  porte : " + CaptureLibre(it.TransformPoint(new Vector3(dx - 0.9f, p.yF + 2.7f, p.zfi - 2.1f)), it.TransformPoint(new Vector3(dx + 0.45f, p.yF + 0.5f, p.zfi - 0.1f)), 62f, "Assets/Screenshots/interieur_" + nom.ToLower() + "_porte.png") + "\n");
                // porte vue de dehors (allée, ou pied de la rampe du perron)
                // de biais depuis la gauche : on voit dans l'embrasure le battant rabattu à droite et ses gonds
                Vector3 ext = g.rampants ? new Vector3(dx - 1.1f, 1.6f, p.cadreZ + 2.4f) : new Vector3(dx - 1.3f, 2.2f, 0.35f * p.s + 2.0f);
                sb.Append("  porte (dehors) : " + CaptureLibre(it.TransformPoint(ext), it.TransformPoint(new Vector3(dx, p.yF + 1.0f, p.zfi)), 55f, "Assets/Screenshots/interieur_" + nom.ToLower() + "_porte_ext.png") + "\n");
                cc.gameObject.SetActive(true);
                // entrée : le mannequin passe le seuil vers l'intérieur, caméra derrière lui (dehors)
                Poser(cc, it, new Vector3(dx, 0, g.rampants ? p.zfi + 0.35f : p.zfi + 0.25f), lacetMaison + 180f);
                sb.Append("  entrée : " + Capture(it, cc, lacetMaison + 180f, 14f, "Assets/Screenshots/interieur_" + nom.ToLower() + "_entree.png") + "\n");
            }
            // vue : devant l'ancre d'échange, face au villageois ; caméra à l'épaule dans la pièce
            float lacetVue = Quaternion.LookRotation(-ae.forward).eulerAngles.y;
            // sorcier : le pupitre est près de la façade ; vue prise du milieu de la pièce, face à l'âtre
            if (nom == "Sorcier") { client = new Vector3(0.15f, p.yF, 1.25f); lacetVue = it.eulerAngles.y + 180f; }
            Poser(cc, it, client, lacetVue);
            // lacet de caméra (le joueur tourne la caméra librement) : le plus grand recul resté dans la pièce, à ±80° du regard
            cc.enabled = false; float meilleur = lacetVue, dMax = -1f;
            for (float o = 0f; o <= 80f; o += 5f)
                foreach (float sg in new[] { 1f, -1f })
                {
                    float l = lacetVue + sg * o, d;
                    Vector3 c = it.InverseTransformPoint(PositionCamera(cc.transform.position, l, 18f, out d));
                    bool dedans = c.x > -p.xi + 0.1f && c.x < p.xi - 0.1f && c.z > p.zbi + 0.1f && c.z < p.zfi - 0.1f && c.y < p.Plafond(c.x) - 0.1f;
                    if (dedans && d > dMax + 0.15f) { dMax = d; meilleur = l; }
                }
            cc.enabled = true;
            sb.Append("  vue : " + Capture(it, cc, meilleur, 18f, "Assets/Screenshots/interieur_" + nom.ToLower() + (nuit ? "_nuit" : "_vue") + ".png") + "\n");
            Object.DestroyImmediate(man);
        }
        Object.DestroyImmediate(mat);
        if (!nuit)
            // portes fermées des maisons de décor (maillage KayKit d'origine)
            foreach (Transform h in GameObject.Find("VillageBlockout/Maisons").transform)
            {
                if (System.Array.IndexOf(InterieursBuilder.Maisons, h.name) >= 0) continue;
                var g = h.name.EndsWith("_A") ? InterieursBuilder.A : InterieursBuilder.B;
                Vector3 porte = h.TransformPoint(new Vector3(g.porteX, g.sol + 0.15f, g.cadreZ));
                Vector3 cam = h.TransformPoint(new Vector3(g.porteX + 0.1f, g.sol + 0.3f, g.cadreZ + 0.55f));
                sb.Append("  décor " + h.name + " : " + CaptureLibre(cam, porte, 55f, "Assets/Screenshots/interieur_decor_" + h.name.ToLower() + "_porte.png") + "\n");
            }
        AssetDatabase.Refresh();
        return sb.ToString();
    }

    static void Poser(CharacterController cc, Transform it, Vector3 local, float lacet)
    {
        cc.enabled = false;
        Vector3 w = it.TransformPoint(local); RaycastHit h;
        if (Physics.Raycast(w + Vector3.up * 3f, Vector3.down, out h, 10f, ~0, QueryTriggerInteraction.Ignore)) w.y = h.point.y;
        cc.transform.SetPositionAndRotation(w, Quaternion.Euler(0, lacet, 0));
        Physics.SyncTransforms();
    }

    public static string CaptureLibre(Vector3 pos, Vector3 vise, float champ, string chemin)
    {
        GameObject go = new GameObject("CaptureInterieur"); Camera cam = go.AddComponent<Camera>();
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cam.fieldOfView = champ; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f;
        go.transform.position = pos; go.transform.LookAt(vise);
        var rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), chemin), tex.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        return chemin;
    }

    static string Capture(Transform it, CharacterController cc, float lacet, float tangage, string chemin)
    {
        cc.enabled = false;   // la caméra de main ignore les personnages
        float d; Vector3 pos = PositionCamera(cc.transform.position, lacet, tangage, out d);
        GameObject go = new GameObject("CaptureInterieur"); Camera cam = go.AddComponent<Camera>();
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(tangage, lacet, 0f));
        var rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), chemin), tex.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        cc.enabled = true;
        return chemin + " (recul caméra " + d.ToString("F2") + " m)";
    }
}
