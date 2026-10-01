using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Deathless.Donjon;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Deathless.Jeu.Dev
{
    /// Outils d'audit du donjon en Play (appelés par execute_code, préfixe [Donjon] dans la console ; audit du
    /// 27/09/2026, Docs/audit-donjon.md). Aucun effet hors Play ; rien n'est sauvegardé.
    ///   ScenariosDonjon.Etat()                    état du donjon, du héros, de l'invite courante
    ///   ScenariosDonjon.Construire(graine)        donjon de cette graine (autorité) + gardiens
    ///   ScenariosDonjon.Analyser()                mesures du plan construit (murs, ouvertures, os, lumières, NavMesh…)
    ///   ScenariosDonjon.Aller("arrivee"|"retour"|"butin0"…|"eau"|"apparition3"|"village")
    ///   ScenariosDonjon.Teleporter(x, y, z) / Regarder(lacet, tangage) / Capturer(nom)
    ///   ScenariosDonjon.Invite() / Interagir()    invite du point d'interaction le plus proche / touche Interagir
    ///   ScenariosDonjon.Horloge(reste)            secondes restantes dans la phase courante
    ///   ScenariosDonjon.Parcourir("butin0", 20)   marche par le NavMesh, avec surveillance de la caméra
    ///   ScenariosDonjon.Perf(120)                 temps d'image, triangles, batches sur N images
    public class ScenariosDonjon : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosDonjon() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosDonjon"); }

        static ScenariosDonjon s_I;
        public static string Dernier = "";

        static ScenariosDonjon I
        {
            get
            {
                if (s_I == null) s_I = new GameObject("ScenariosDonjon").AddComponent<ScenariosDonjon>();
                return s_I;
            }
        }

        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;
        static DonjonJeu DJ => DonjonJeu.Instance;
        static DonjonGenerateur G => DJ != null ? DJ.generateur : null;

        static void Log(string t) { Dernier = t; Debug.Log("[Donjon] " + t); }

        static object Prive(object o, string nom, params object[] args)
        {
            var t = o.GetType();
            var m = t.GetMethod(nom, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (m != null) return m.Invoke(o, args);
            var p = t.GetProperty(nom, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (p != null) return p.GetValue(o);
            var f = t.GetField(nom, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return f != null ? f.GetValue(o) : null;
        }

        // ================================================================== État

        public static string Etat()
        {
            var sb = new StringBuilder();
            var p = P; var h = H; var dj = DJ; var g = G;
            if (p == null) return "pas de partie";
            sb.Append(p.Etat.phase).Append(" nuit ").Append(p.Etat.nuit).Append(" reste ").Append(p.Etat.TempsRestant.ToString("F1")).Append(" s");
            sb.Append(" | caisse ").Append(p.Etat.orEquipe);
            if (dj != null)
            {
                sb.Append(" | donjon graine ").Append(dj.GraineCourante).Append(" essai ").Append(dj.EssaiCourant).Append(" pret ").Append(dj.Pret)
                  .Append(" pris ").Append(System.Convert.ToString(dj.Pris, 2)).Append(" gardiens ").Append(dj.GardiensVivants)
                  .Append(" orPorte ").Append(dj.OrPorte).Append(" auDonjon ").Append(dj.AuDonjonLocal).Append(" avantRappel ").Append(dj.AvantRappel.ToString("F1"))
                  .Append(" msg '").Append(dj.Message).Append("'");
            }
            if (h != null)
            {
                sb.Append(" | héros ").Append(h.EtatCourant).Append(h.EnTransit ? " TRANSIT" : "").Append(h.Vivant ? "" : " MORT").Append(" pos ").Append(h.transform.position.ToString("F1"));
                var pi = PointInteraction.Courant(h, out string inv);
                sb.Append(" invite '").Append(inv ?? "").Append("'").Append(pi != null ? " (" + pi.GetType().Name + ")" : "");
                var st = h.Statuts;
                if (st != null) { sb.Append(" statuts "); foreach (var s in st.Liste) sb.Append(s.type).Append("/").Append(s.origine).Append(" "); }
                sb.Append(" eau×").Append(ZoneEau.FacteurEn(h.transform.position + Vector3.up * 0.2f).ToString("F1"));
            }
            var cam = p.cameraJeu;
            if (cam != null) sb.Append(" | cam ").Append(cam.transform.position.ToString("F1")).Append(" lacet ").Append(cam.lacet.ToString("F0")).Append(" tangage ").Append(cam.tangage.ToString("F0"));
            return sb.ToString();
        }

        // ================================================================== Construction d'une graine

        /// Autorité : construit le donjon de cette graine (comme au lever du jour) et pose les gardiens.
        public static string Construire(int graine)
        {
            var dj = DJ;
            if (dj == null) return "pas de DonjonJeu";
            var t = typeof(DonjonJeu);
            t.GetMethod("Construire", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dj, new object[] { graine, -1 });
            t.GetMethod("PoserGardiens", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dj, null);
            var g = G;
            string r = "graine " + dj.GraineCourante + " essai " + dj.EssaiCourant + " chemin " + (g.Plan.cheminCritiqueDm / 10f).ToString("F0") + " m, plan " + g.DernierTempsPlanMs.ToString("F0") + " ms, pièces " + g.DernierTempsConstructionMs.ToString("F0") + " ms, navmesh " + g.DernierTempsNavMeshMs.ToString("F0") + " ms, relances " + g.Relances + ", objets " + g.NbObjets + ", lumières " + g.NbLumieres + ", gardiens " + dj.GardiensVivants;
            Log(r);
            return r;
        }

        // ================================================================== Analyse du plan construit

        public static string Analyser()
        {
            var g = G;
            if (g == null || g.Plan == null) return "pas de générateur";
            var plan = g.Plan;
            var sb = new StringBuilder();
            sb.Append("graine ").Append(plan.graine).Append(" essai ").Append(plan.essais - 1).Append(" chemin ").Append(plan.cheminCritiqueDm / 10f).Append(" m\n");
            // Escaliers et butins par le NavMesh.
            bool esc = g.VerifierEscaliers(), but = g.VerifierButins();
            sb.Append("escaliers ").Append(g.EscaliersValides).Append("/").Append(plan.nbEscaliers).Append(esc ? " ok" : " DEFAUT " + g.DefautsEscaliers).Append("\n");
            sb.Append("butins ").Append(but ? "tous accessibles" : "DEFAUT " + g.DefautsButins).Append("\n");
            // Cellules par niveau, blocs.
            int[] parNiveau = new int[DonjonPlan.NbNiveaux];
            for (int n = 0; n < DonjonPlan.NbNoeuds; n++) if (plan.plein[n]) parNiveau[n / DonjonPlan.NbCellules]++;
            sb.Append("cellules pleines : rez ").Append(parNiveau[0]).Append(", niveau 1 ").Append(parNiveau[1]).Append(", niveau 2 ").Append(parNiveau[2]).Append("\n");
            var blocs = new Dictionary<TypeBloc, int>();
            foreach (var tb in plan.typeBloc) { blocs.TryGetValue(tb, out int c); blocs[tb] = c + 1; }
            sb.Append("blocs : "); foreach (var kv in blocs) sb.Append(kv.Key).Append(" ").Append(kv.Value).Append(", "); sb.Append("\n");
            // Bords par type.
            var bords = new Dictionary<Bord, int>();
            foreach (var b in plan.bordH) { bords.TryGetValue(b, out int c); bords[b] = c + 1; }
            foreach (var b in plan.bordV) { bords.TryGetValue(b, out int c); bords[b] = c + 1; }
            sb.Append("bords : "); foreach (var kv in bords) if (kv.Key != Bord.Rien) sb.Append(kv.Key).Append(" ").Append(kv.Value).Append(", "); sb.Append("\n");
            // Modèles visibles par nom (murs, portes, fenêtres, os, crânes).
            var noms = new Dictionary<string, int>();
            int mursBas = 0;
            Transform visuels = g.transform.Find("Genere/Visuels");
            if (visuels != null)
                foreach (Transform grp in visuels)
                    foreach (Transform t in grp)
                    {
                        if (!t.gameObject.activeSelf) continue;
                        noms.TryGetValue(t.name, out int c); noms[t.name] = c + 1;
                        if (t.name.StartsWith("wall") && Mathf.Abs(t.localScale.y - 0.5f) < 0.01f) mursBas++;
                    }
            sb.Append("modèles : ");
            var cles = new List<string>(noms.Keys); cles.Sort();
            foreach (var k in cles) sb.Append(k).Append(" ").Append(noms[k]).Append(", ");
            sb.Append("\nmurs bas (échelle 0,5) : ").Append(mursBas).Append("\n");
            // Torches et lumières.
            sb.Append("torches ").Append(plan.nbTorches).Append(", lumières actives ").Append(g.NbLumieres).Append("\n");
            // Cellules sombres : aucune lampe active à moins de sa portée (horizontalement, même niveau ± 3 m).
            var lampes = new List<Light>();
            Transform lum = g.transform.Find("Genere/Lumieres");
            if (lum != null) foreach (Transform t in lum) { var l = t.GetComponent<Light>(); if (l != null && t.gameObject.activeSelf) lampes.Add(l); }
            int sombres = 0; var exemples = new StringBuilder();
            for (int n = 0; n < DonjonPlan.NbNoeuds; n++)
            {
                if (!plan.Praticable(n)) continue;
                int k = n / DonjonPlan.NbCellules, c = n % DonjonPlan.NbCellules;
                Vector3 pos = g.transform.TransformPoint(new Vector3((c % DonjonPlan.Largeur + 0.5f) * DonjonPlan.Cellule, plan.HauteurSol(n) + 1f, (c / DonjonPlan.Largeur + 0.5f) * DonjonPlan.Cellule));
                float best = float.MaxValue;
                foreach (var l in lampes)
                {
                    float d = Vector3.Distance(l.transform.position, pos);
                    if (d < best) best = d;
                }
                if (best > 8f) { sombres++; if (exemples.Length < 200) exemples.Append(pos.ToString("F0")).Append(" (").Append(best.ToString("F0")).Append(" m) "); }
            }
            sb.Append("cellules à plus de 8 m de toute lampe : ").Append(sombres).Append(" ").Append(exemples).Append("\n");
            // Butins et apparitions dans un mur ? (colliders épais contenant le point, hors dalles).
            sb.Append(DansMurs("butin", g.Butins));
            sb.Append(DansMurs("apparition", g.Apparitions));
            // Os / crânes posés sur les points d'apparition.
            int cranes = 0; noms.TryGetValue("skull", out cranes);
            sb.Append("crânes (skull) posés : ").Append(cranes).Append(" sur ").Append(DonjonPlan.NbApparitions).Append(" apparitions\n");
            // Gardiens.
            sb.Append(Gardiens());
            Log(sb.ToString());
            return sb.ToString();
        }

        static string DansMurs(string nom, DonjonRepere[] reperes)
        {
            var sb = new StringBuilder();
            var hits = new Collider[16];
            for (int i = 0; i < reperes.Length; i++)
            {
                var r = reperes[i];
                if (r == null) continue;
                Vector3 p = r.transform.position + Vector3.up * 0.9f;
                int n = Physics.OverlapSphereNonAlloc(p, 0.3f, hits, ~(1 << 2), QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    var c = hits[k];
                    var bc = c as BoxCollider;
                    if (c.GetComponentInParent<Sante>() != null) continue;
                    if (bc != null && bc.size.y <= 0.35f) continue;   // dalle
                    // La boîte du butin lui-même (BoiteModele) : même position que le repère.
                    if (r.genre == DonjonRepere.Genre.Butin && Vector3.Distance(c.transform.position, r.transform.position) < 0.05f) continue;
                    sb.Append(nom).Append(" ").Append(i).Append(" (").Append(r.name).Append(") dans ").Append(c.name).Append(" ").Append(c.bounds.size.ToString("F1")).Append(" à ").Append(c.transform.position.ToString("F1")).Append("\n");
                }
            }
            return sb.Length == 0 ? "aucun " + nom + " dans un mur\n" : sb.ToString();
        }

        public static string Gardiens()
        {
            var dj = DJ;
            if (dj == null) return "";
            var liste = Prive(dj, "m_Gardiens") as List<Squelette>;
            var sb = new StringBuilder();
            if (liste == null) return "gardiens inconnus\n";
            sb.Append("gardiens ").Append(liste.Count).Append(" : ");
            foreach (var s in liste)
            {
                if (s == null) { sb.Append("[détruit] "); continue; }
                var a = s.Agent;
                sb.Append(s.type).Append(" ").Append(s.EtatCourant).Append(" à ").Append(s.transform.position.ToString("F1"))
                  .Append(" navmesh=").Append(a != null && a.enabled && a.isOnNavMesh).Append(" vivant=").Append(s.Vivant).Append(" ; ");
            }
            sb.Append("\n");
            return sb.ToString();
        }

        // ================================================================== Déplacements

        public static Vector3 Cible(string ou)
        {
            var g = G; var dj = DJ;
            if (g == null) return Vector3.zero;
            if (ou == "arrivee") return (Vector3)Prive(dj, "PointArrivee");
            if (ou == "village") return (Vector3)Prive(dj, "SortieVillage");
            if (ou == "retour") return g.PortailRetour.transform.position + g.PortailRetour.transform.forward * 2f;
            if (ou == "eau")
            {
                Transform e = null;
                foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (t.name == "Eau") { e = t; break; }
                if (e != null && NavMesh.SamplePosition(e.position + Vector3.up * 0.5f, out var he, 4f, NavMesh.AllAreas)) return he.position;
                return e != null ? e.position : Vector3.zero;
            }
            if (ou.StartsWith("butin"))
            {
                int i = int.Parse(ou.Substring(5));
                var r = g.Butins[i];
                // Devant le coffre (il tourne le dos au mur) ; jusqu'au 27/09/2026, « - forward » plaçait le héros dans le mur.
                Vector3 p = r.transform.position + r.transform.forward * 1.5f;
                if (NavMesh.SamplePosition(p + Vector3.up * 0.3f, out var hb, 1.2f, NavMesh.AllAreas)) return hb.position;
                return r.transform.position;
            }
            if (ou.StartsWith("apparition"))
            {
                int i = int.Parse(ou.Substring(10));
                return g.Apparitions[i].transform.position;
            }
            if (ou == "portailvillage") { var vc = VueCycle.Instance; return vc != null && vc.portail != null ? vc.portail.Center : Vector3.zero; }
            var parts = ou.Split(',');
            if (parts.Length == 3) return new Vector3(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));
            return Vector3.zero;
        }

        public static string Aller(string ou)
        {
            Vector3 p = Cible(ou);
            var h = H;
            if (h == null) return "pas de héros";
            Vector3 regard = ou == "retour" && G != null ? G.PortailRetour.transform.position : p + h.transform.forward;
            if (ou.StartsWith("butin")) regard = G.Butins[int.Parse(ou.Substring(5))].transform.position;
            if (ou == "portailvillage") { var vc = VueCycle.Instance; p = vc.portail.Center + (P.nyxessa.transform.position - vc.portail.Center).normalized * 2.2f; p.y = 0f; regard = vc.portail.Center; }
            Teleporter(p.x, p.y, p.z);
            Vector3 d = regard - p; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) { h.transform.rotation = Quaternion.LookRotation(d); if (P.cameraJeu != null) P.cameraJeu.lacet = h.transform.eulerAngles.y; }
            return "héros à " + h.transform.position.ToString("F1");
        }

        public static void Teleporter(float x, float y, float z)
        {
            var h = H;
            if (h == null) return;
            h.Teleporter(new Vector3(x, y, z));
            if (P.cameraJeu != null) P.cameraJeu.lacet = h.transform.eulerAngles.y;
        }

        public static void Regarder(float lacet, float tangage)
        {
            var h = H; var cam = P != null ? P.cameraJeu : null;
            if (cam == null) return;
            cam.lacet = lacet; cam.tangage = tangage;
            if (h != null) h.transform.rotation = Quaternion.Euler(0f, lacet, 0f);
        }

        public static void Capturer(string nom) => DevPartie.Capturer(nom);

        public static string Invite()
        {
            var h = H;
            if (h == null) return "pas de héros";
            var pi = PointInteraction.Courant(h, out string inv);
            return (inv ?? "(aucune)") + (pi != null ? " [" + pi.GetType().Name + "]" : "");
        }

        public static string Interagir()
        {
            var h = H;
            if (h == null) return "pas de héros";
            string avant = Invite();
            bool ok = PointInteraction.InteragirIci(h);
            return "interagir sur '" + avant + "' → " + ok;
        }

        /// Secondes restantes dans la phase courante.
        public static string Horloge(float reste)
        {
            var p = P;
            if (p == null) return "pas de partie";
            p.Etat.tempsPhase = Mathf.Max(0f, p.Etat.dureePhase - reste);
            return p.Etat.phase + " reste " + p.Etat.TempsRestant.ToString("F1") + " s";
        }

        public static string Phase(string nom)
        {
            var p = P;
            if (p == null) return "pas de partie";
            p.ForcerPhase((Phase)System.Enum.Parse(typeof(Phase), nom), Mathf.Max(1, p.Etat.nuit));
            return Etat();
        }

        public static void Mourir() => DevPartie.Blesser(100000f);
        public static void DonnerOr(int n) { if (P != null && P.JoueurLocal != null) P.JoueurLocal.orPorte += n; }
        public static void TuerGardiens()
        {
            var liste = DJ != null ? Prive(DJ, "m_Gardiens") as List<Squelette> : null;
            if (liste == null) return;
            foreach (var s in liste) if (s != null && s.Vivant) s.Sante.Encaisser(new InfoDegats { montant = 100000f, sourceId = H != null ? H.Id : 0, equipeSource = Equipe.Heros, point = s.transform.position });
        }

        // ================================================================== Parcours avec surveillance de la caméra

        /// Marche par le NavMesh jusqu'à `ou` (entrée de test, repère de la caméra), `duree` s au plus, en notant les images
        /// où la caméra est dans un mur (sphère de 0,15 m dans un collider non masqué, hors personnages) et celles où
        /// la tête du héros est cachée à la caméra ; sauvegarde des captures aux images fautives (`capture` : préfixe).
        public static void Parcourir(string ou, float duree, string capture = null, bool sprint = false)
        {
            var i = I;
            i.StopAllCoroutines();
            i.StartCoroutine(i.CoParcourir(Cible(ou), duree, capture, sprint));
        }

        public static void Arreter()
        {
            if (s_I != null) s_I.StopAllCoroutines();
            var h = H;
            if (h != null) { h.Entrees.DeplacementTest = null; h.Entrees.SprintTest = false; }
        }

        IEnumerator CoParcourir(Vector3 dest, float duree, string capture, bool sprint)
        {
            var h = H; var cam = P.cameraJeu;
            if (h == null || cam == null) { Log("pas de héros/caméra"); yield break; }
            var camU = cam.GetComponent<Camera>();
            var chemin = new NavMeshPath();
            int coin = 1;
            float t = 0f, dernierCalcul = -9f;
            int imagesMur = 0, imagesCache = 0, images = 0, captures = 0;
            float reculMin = 99f;
            var premMur = new StringBuilder(); var premCache = new StringBuilder();
            var hits = new Collider[16];
            while (t < duree)
            {
                if (Time.time - dernierCalcul > 0.5f)
                {
                    dernierCalcul = Time.time;
                    if (NavMesh.SamplePosition(h.transform.position, out var hs, 2f, NavMesh.AllAreas) && NavMesh.SamplePosition(dest, out var hd, 2f, NavMesh.AllAreas)
                        && NavMesh.CalculatePath(hs.position, hd.position, NavMesh.AllAreas, chemin) && chemin.corners.Length > 1) coin = 1;
                    else { Log("parcours : pas de chemin vers " + dest.ToString("F1")); break; }
                }
                Vector3 cible = chemin.corners[Mathf.Min(coin, chemin.corners.Length - 1)];
                Vector3 d = cible - h.transform.position; d.y = 0f;
                if (d.magnitude < 0.6f) { if (coin < chemin.corners.Length - 1) coin++; else { Log("parcours : arrivé à " + h.transform.position.ToString("F1")); break; } }
                d.Normalize();
                Vector3 avant = cam.AvantPlat; Vector3 droite = Vector3.Cross(Vector3.up, avant);
                h.Entrees.DeplacementTest = new Vector2(Vector3.Dot(d, droite), Vector3.Dot(d, avant));
                h.Entrees.SprintTest = sprint;
                // La caméra suit le lacet du héros (comme un joueur qui tourne la souris vers sa direction de marche).
                float lacetVoulu = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                cam.lacet = Mathf.MoveTowardsAngle(cam.lacet, lacetVoulu, 180f * Time.deltaTime);
                yield return null;   // LateUpdate de la caméra passé : mesure sur la pose rendue
                images++;
                t += Time.deltaTime;
                // Caméra dans un mur ?
                int n = Physics.OverlapSphereNonAlloc(cam.transform.position, 0.15f, hits, ~(1 << 2), QueryTriggerInteraction.Ignore);
                bool mur = false;
                for (int k = 0; k < n; k++)
                {
                    if (hits[k].GetComponentInParent<Sante>() != null || DonjonMasquage.ColliderMasque(hits[k])) continue;
                    mur = true; break;
                }
                // Tête cachée ?
                Vector3 tete = h.transform.position + Vector3.up * 1.6f;
                bool cache = false;
                if (Physics.Linecast(cam.transform.position, tete, out var lh, ~(1 << 2), QueryTriggerInteraction.Ignore))
                    cache = lh.collider.GetComponentInParent<Sante>() == null && !DonjonMasquage.ColliderMasque(lh.collider);
                float recul = Vector3.Distance(cam.transform.position, h.transform.position + Vector3.up * GameBalance.Courant.cameraHauteur);
                if (recul < reculMin) reculMin = recul;
                if (mur) { imagesMur++; if (premMur.Length < 300) premMur.Append(cam.transform.position.ToString("F1")).Append(" "); }
                if (cache) { imagesCache++; if (premCache.Length < 300) premCache.Append(h.transform.position.ToString("F1")).Append("→").Append(lh.collider.name).Append(" "); }
                if ((mur || cache) && capture != null && captures < 3) { captures++; DevPartie.Capturer(capture + "_" + captures); }
            }
            h.Entrees.DeplacementTest = null; h.Entrees.SprintTest = false;
            if (capture != null && captures == 0) DevPartie.Capturer(capture + "_fin");   // aucune image fautive : vue d'arrivée
            Log("parcours " + images + " images : caméra dans un mur " + imagesMur + " (" + premMur + "), héros caché " + imagesCache + " (" + premCache + "), recul min " + reculMin.ToString("F2") + " m, héros " + h.transform.position.ToString("F1"));
        }

        // ================================================================== Performances

        public static void Perf(int images) { var i = I; i.StartCoroutine(i.CoPerf(images)); }

        IEnumerator CoPerf(int images)
        {
            float somme = 0f, max = 0f, min = 99f;
            for (int k = 0; k < images; k++)
            {
                yield return null;
                float ms = Time.unscaledDeltaTime * 1000f;
                somme += ms; if (ms > max) max = ms; if (ms < min) min = ms;
            }
            string stats = "";
#if UNITY_EDITOR
            // UnityStats n'existe que dans l'éditeur : hors de ce bloc, le build du joueur ne compile pas.
            stats = " | triangles " + UnityEditor.UnityStats.triangles + " sommets " + UnityEditor.UnityStats.vertices + " batches " + UnityEditor.UnityStats.batches + " setPass " + UnityEditor.UnityStats.setPassCalls;
#endif
            Log("perf sur " + images + " images : moyenne " + (somme / images).ToString("F1") + " ms, min " + min.ToString("F1") + ", max " + max.ToString("F1")
                + stats + " | héros " + (H != null ? H.transform.position.ToString("F0") : "?"));
        }

        // ================================================================== Contrôles des correctifs du 27/09/2026

        /// Compte des pièces de mur posées, par famille (audit point 4 : avant / après), plus murets, plafonds, grilles, combinés.
        public static string Ouvertures()
        {
            var g = G;
            if (g == null) return "pas de générateur";
            int portes = 0, arches = 0, fenetres = 0, casses = 0, pleins = 0, murets = 0, plafonds = 0, grilles = 0, combines = 0, ecrases = 0;
            Transform visuels = g.transform.Find("Genere/Visuels");
            if (visuels != null)
                foreach (Transform grp in visuels)
                    foreach (Transform t in grp)
                    {
                        if (!t.gameObject.activeSelf) continue;
                        string n = t.name;
                        if (n.StartsWith("Combine_")) combines++;
                        else if (n.StartsWith("wall_doorway")) portes++;
                        else if (n.StartsWith("wall_arched")) arches++;
                        else if (n.Contains("window")) fenetres++;
                        else if (n == "wall_broken") casses++;
                        else if (n == "wall" || n == "wall_pillar" || n == "wall_scaffold") pleins++;
                        else if (n == "ceiling_tile") plafonds++;
                        else if (n == "floor_tile_big_grate") grilles++;
                        if (n.StartsWith("wall") && Mathf.Abs(t.localScale.y - 0.5f) < 0.01f) ecrases++;
                        if ((n == "floor_foundation_front" && Mathf.Abs(t.localScale.y - 0.5f) < 0.01f) || n == "Muret") murets++;
                    }
            string r = "graine " + g.Plan.graine + " : portes " + portes + ", arches " + arches + ", fenêtres " + fenetres + ", cassés " + casses + ", pleins " + pleins
                + ", murs écrasés " + ecrases + ", murets " + murets + ", plafonds " + plafonds + ", grilles " + grilles + ", combinés " + combines + " (" + g.DernierTempsCombinaisonMs.ToString("F0") + " ms)";
            Log(r);
            return r;
        }

        /// Distance de chaque point d'apparition à l'ossement le plus proche (point 3 : ≥ 1,5 m attendu), et point d'apparition dans un décor (G6).
        public static string OsApparitions()
        {
            var g = G;
            if (g == null) return "pas de générateur";
            var os = new List<Transform>();
            Transform visuels = g.transform.Find("Genere/Visuels");
            if (visuels != null)
                foreach (Transform grp in visuels)
                    foreach (Transform t in grp)
                        if (t.gameObject.activeSelf && (t.name.StartsWith("bone_") || t.name == "skull" || t.name == "ribcage")) os.Add(t);
            float min = 99f; int sous = 0;
            foreach (var a in g.Apparitions)
            {
                float best = 99f;
                foreach (var o in os) { float d = Vector3.Distance(o.position, a.transform.position); if (d < best) best = d; }
                if (best < min) min = best;
                if (best < 1.5f) sous++;
            }
            string r = "ossements " + os.Count + ", distance minimale apparition → os " + min.ToString("F2") + " m, apparitions à moins de 1,5 m d'un os : " + sous + "\n" + DansMurs("apparition", g.Apparitions);
            Log(r);
            return r;
        }

        /// Bords de plancher des étages sans garde-corps ni mur (G8) : cellule pleine au niveau k ≥ 1 dont la voisine est vide
        /// et dont le bord est « Rien », hors palier d'escalier.
        public static string BordsOuverts()
        {
            var g = G;
            if (g == null) return "pas de générateur";
            var plan = g.Plan;
            int n = 0; var sb = new StringBuilder();
            for (int k = 1; k < DonjonPlan.NbNiveaux; k++)
                for (int c = 0; c < DonjonPlan.NbCellules; c++)
                {
                    if (!plan.plein[k * DonjonPlan.NbCellules + c]) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        int v = DonjonPlan.Voisine(c, d);
                        if (v < 0 || plan.plein[k * DonjonPlan.NbCellules + v]) continue;
                        if (plan.BordCellule(k, c, d) != Bord.Rien) continue;
                        bool palier = false;
                        for (int i = 0; i < plan.nbEscaliers; i++)
                        {
                            var e = plan.escaliers[i];
                            if (!e.bassin && e.niveau == k - 1 && e.arrivee == c && e.haut == v) palier = true;
                        }
                        if (palier) continue;
                        n++;
                        if (sb.Length < 300) sb.Append("niveau ").Append(k).Append(" cellule ").Append(c).Append(" dir ").Append(d).Append(" ; ");
                    }
                }
            string r = "bords de plancher ouverts sur le vide (hors paliers) : " + n + " " + sb;
            Log(r);
            return r;
        }

        /// Cas de caméra de l'audit : téléporte à `ou`, oriente la caméra, attend quelques images puis vérifie que la caméra n'est
        /// dans aucun collider (sphère 0,15 m) et qu'elle est dans l'enceinte ; capture `capture` (facultative).
        public static void CasCamera(string ou, float lacet, float tangage, string capture = null) { var i = I; i.StartCoroutine(i.CoCasCamera(ou, lacet, tangage, capture)); }

        IEnumerator CoCasCamera(string ou, float lacet, float tangage, string capture)
        {
            var h = H; var cam = P != null ? P.cameraJeu : null;
            if (h == null || cam == null) { Log("pas de héros/caméra"); yield break; }
            Aller(ou);
            yield return null;
            Regarder(lacet, tangage);
            var hits = new Collider[16];
            int dansMur = 0, hors = 0, images = 0; float reculMin = 99f; string premier = "";
            for (int k = 0; k < 20; k++)
            {
                yield return null;
                images++;
                int n = Physics.OverlapSphereNonAlloc(cam.transform.position, 0.15f, hits, ~(1 << 2), QueryTriggerInteraction.Ignore);
                bool mur = false;
                for (int i = 0; i < n; i++) if (!CameraEpaule.Ignore(hits[i])) { mur = true; if (premier.Length == 0) premier = hits[i].name + " " + hits[i].bounds.size.ToString("F1"); }
                if (mur) dansMur++;
                if (!DonjonJeu.Contient(cam.transform.position) || (CameraEpaule.Enceinte.HasValue && !CameraEpaule.Enceinte.Value.Contains(cam.transform.position))) hors++;
                float recul = Vector3.Distance(cam.transform.position, h.transform.position + Vector3.up * GameBalance.Courant.cameraHauteur);
                if (recul < reculMin) reculMin = recul;
            }
            if (capture != null) DevPartie.Capturer(capture);
            Log("cas caméra " + ou + " lacet " + lacet + " tangage " + tangage + " : " + images + " images, dans un collider " + dansMur + (premier.Length > 0 ? " (" + premier + ")" : "")
                + ", hors enceinte " + hors + ", recul " + reculMin.ToString("F2") + " m, héros " + h.transform.position.ToString("F2") + ", caméra " + cam.transform.position.ToString("F2"));
        }

        /// Ouvre le coffre `index` puis compte les triangles du corps (le tas de pièces doit avoir disparu) ; capture après 1 s.
        public static void CoffreVide(int index, string capture = null) { var i = I; i.StartCoroutine(i.CoCoffreVide(index, capture)); }

        IEnumerator CoCoffreVide(int index, string capture)
        {
            var g = G; var dj = DJ;
            if (g == null || dj == null) yield break;
            var r = g.Butins[index];
            int avant = Triangles(r.visuel), apres;
            Aller("butin" + index);
            yield return null;
            // Caméra de trois quarts : le héros ne cache plus le coffre.
            var cam = P.cameraJeu; cam.lacet += 55f; cam.tangage = 32f;
            yield return null;
            int orAvant = dj.OrPorte;
            PointInteraction.InteragirIci(H);
            yield return new WaitForSeconds(0.55f);
            if (capture != null) DevPartie.Capturer(capture + "_0");
            yield return new WaitForSeconds(1.3f);
            apres = Triangles(r.visuel);
            int pieces = 0; foreach (var p in FindObjectsByType<PieceOr>(FindObjectsSortMode.None)) pieces++;
            if (capture != null) DevPartie.Capturer(capture + "_1");
            Log("coffre " + index + " : triangles du modèle " + avant + " → " + apres + ", or porté " + orAvant + " → " + dj.OrPorte + ", PieceOr en vol " + pieces + ", visuel actif " + r.visuel.activeSelf);
        }

        static int Triangles(GameObject go)
        {
            int n = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null && mf.sharedMesh.isReadable) n += mf.sharedMesh.triangles.Length / 3;
            return n;
        }

        /// Place le héros à 3,5 m devant la `index`-ième pièce active nommée `nom` (côté intérieur du donjon), la regarde, capture.
        public static void VoirPiece(string nom, int index, string capture, float tangage = 14f) { var i = I; i.StartCoroutine(i.CoVoirPiece(nom, index, capture, tangage)); }

        IEnumerator CoVoirPiece(string nom, int index, string capture, float tangage)
        {
            var g = G; var h = H;
            if (g == null || h == null) yield break;
            // nom : « wall », « wall@1 » (niveau 1), « muret » (fondation écrasée à 0,5 ou bloc généré).
            int niveau = -1; int at = nom.IndexOf('@');
            if (at >= 0) { niveau = int.Parse(nom.Substring(at + 1)); nom = nom.Substring(0, at); }
            Transform piece = null; int n = 0;
            Transform visuels = g.transform.Find("Genere/Visuels");
            foreach (Transform grp in visuels) foreach (Transform t in grp)
            {
                if (!t.gameObject.activeSelf) continue;
                bool ok = nom == "muret" ? (t.name == "Muret" || (t.name == "floor_foundation_front" && Mathf.Abs(t.localScale.y - 0.5f) < 0.01f)) : t.name == nom;
                if (!ok || (niveau >= 0 && Mathf.RoundToInt(t.localPosition.y / DonjonPlan.HauteurNiveau) != niveau)) continue;
                if (n++ == index) { piece = t; break; }
            }
            if (piece == null) { Log("pièce " + nom + " " + index + " introuvable (" + n + " actives)"); yield break; }
            Vector3 centre = g.transform.TransformPoint(new Vector3(DonjonPlan.Largeur * DonjonPlan.Cellule * 0.5f, 0f, DonjonPlan.Profondeur * DonjonPlan.Cellule * 0.5f));
            Vector3 vers = centre - piece.position; vers.y = 0f;
            // Pièce de bord (mur, muret) : on se place perpendiculairement à elle, côté centre.
            Vector3 normale = piece.right; normale.y = 0f;
            if (Mathf.Abs(Vector3.Dot(normale, piece.forward)) < 0.5f) normale = piece.forward;
            normale.y = 0f; normale.Normalize();
            if (Vector3.Dot(normale, vers) < 0f) normale = -normale;
            Vector3 p = piece.position + normale * 3.5f;
            if (NavMesh.SamplePosition(p + Vector3.up * 0.5f, out var hit, 2.5f, NavMesh.AllAreas)) p = hit.position;
            Teleporter(p.x, p.y, p.z);
            Vector3 d = piece.position - p; d.y = 0f;
            float lacet = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Regarder(lacet, tangage);
            for (int k = 0; k < 15; k++) yield return null;
            if (capture != null) DevPartie.Capturer(capture);
            Log("pièce " + nom + " " + index + " à " + piece.position.ToString("F1") + ", héros à " + p.ToString("F1") + ", lacet " + lacet.ToString("F0"));
        }

        /// Marche sur le tas d'or `index` : crédité et retiré ? Capture 0,4 s après.
        public static void TasOr(int index, string capture = null) { var i = I; i.StartCoroutine(i.CoTasOr(index, capture)); }

        IEnumerator CoTasOr(int index, string capture)
        {
            var g = G; var dj = DJ;
            if (g == null || dj == null) yield break;
            var r = g.Butins[index];
            int orAvant = dj.OrPorte;
            Vector3 p = r.transform.position;
            Teleporter(p.x, p.y, p.z);
            yield return new WaitForSeconds(0.4f);
            if (capture != null) DevPartie.Capturer(capture);
            Log("tas d'or " + index + " : pris " + dj.ButinPris(index) + ", or porté " + orAvant + " → " + dj.OrPorte + ", visuel actif " + r.visuel.activeSelf);
        }

        /// Position des pastilles du HUD (temps et message du donjon) dans le panneau, pour régler Hud.uss (G1).
        public static string Hud()
        {
            var doc = FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            if (doc == null) return "pas d'UIDocument";
            var sb = new StringBuilder();
            foreach (var d in FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None))
            {
                var root = d.rootVisualElement;
                if (root == null) continue;
                var temps = root.Q("temps-texte"); var msg = root.Q("donjon-message"); var al = root.Q("donjon-alerte"); var ban = root.Q("banniere");
                if (temps == null) continue;
                var pastille = temps.parent;
                sb.Append(d.name).Append(" : temps ").Append(pastille.worldBound.ToString()).Append(" message ").Append(msg != null ? msg.worldBound.ToString() : "?")
                  .Append(" alerte ").Append(al != null ? al.worldBound.ToString() : "?").Append(" bannière ").Append(ban != null ? ban.worldBound.ToString() : "?")
                  .Append(" classes ").Append(string.Join(",", root.GetClasses())).Append(" panneau ").Append(root.panel != null ? root.panel.visualTree.worldBound.ToString() : "?").Append("\n");
            }
            return sb.ToString();
        }

        // ================================================================== Scénarios enchaînés

        /// Passage complet par la touche Interagir : village → donjon, chronométré (transit, clip d'arrivée, retour du contrôle).
        public static void Passage(bool retour, string capture = null) { var i = I; i.StartCoroutine(i.CoPassage(retour, capture)); }

        IEnumerator CoPassage(bool retour, string capture)
        {
            var h = H;
            if (h == null) yield break;
            Aller(retour ? "retour" : "portailvillage");
            yield return null;
            string inv = Invite();
            Vector3 p0 = h.transform.position;
            float t0 = Time.time;
            bool ok = PointInteraction.InteragirIci(h);
            var sb = new StringBuilder();
            sb.Append("invite '").Append(inv).Append("' interagir=").Append(ok);
            float tTeleport = -1f, tVisible = -1f, tClip = -1f, tFin = -1f;
            var rend = h.GetComponentInChildren<SkinnedMeshRenderer>();
            var anim = h.animator;
            int n = 0;
            while (Time.time - t0 < 6f)
            {
                yield return null;
                float t = Time.time - t0;
                if (tTeleport < 0f && Vector3.Distance(h.transform.position, p0) > 5f) tTeleport = t;
                if (tTeleport >= 0f && tVisible < 0f && rend != null && rend.enabled) tVisible = t;
                if (tClip < 0f && anim != null && (anim.GetCurrentAnimatorStateInfo(0).IsTag(PortailAnim.TagEtat) || anim.GetNextAnimatorStateInfo(0).IsTag(PortailAnim.TagEtat))) tClip = t;
                if (tTeleport >= 0f && tFin < 0f && !h.EnTransit) { tFin = t; }
                if (capture != null && n < 4 && ((n == 0 && t > 0.5f) || (n == 1 && tTeleport >= 0f && t > tTeleport + 0.2f) || (n == 2 && tVisible >= 0f && t > tVisible + 0.4f) || (n == 3 && tFin >= 0f)))
                { DevPartie.Capturer(capture + "_" + n); n++; }
                if (tFin >= 0f && t > tFin + 0.3f) break;
            }
            sb.Append(" | téléporté à ").Append(tTeleport.ToString("F2")).Append(" s, visible à ").Append(tVisible.ToString("F2")).Append(" s, clip vu à ").Append(tClip.ToString("F2")).Append(" s, contrôle rendu à ").Append(tFin.ToString("F2")).Append(" s | ").Append(Etat());
            Log(sb.ToString());
        }

        /// Ouverture d'un coffre par Interagir : chronométrage et or crédité.
        public static void Coffre(int index, string capture = null) { var i = I; i.StartCoroutine(i.CoCoffre(index, capture)); }

        IEnumerator CoCoffre(int index, string capture)
        {
            var h = H; var dj = DJ;
            if (h == null || dj == null) yield break;
            Aller("butin" + index);
            yield return null;
            int orAvant = dj.OrPorte;
            string inv = Invite();
            bool ok = PointInteraction.InteragirIci(h);
            float t0 = Time.time;
            bool pris = false; float tPris = -1f;
            if (capture != null) DevPartie.Capturer(capture + "_0");
            while (Time.time - t0 < 3f)
            {
                yield return null;
                if (!pris && dj.ButinPris(index)) { pris = true; tPris = Time.time - t0; }
                if (capture != null && pris && Time.time - t0 > tPris + 0.5f) { DevPartie.Capturer(capture + "_1"); capture = null; }
            }
            // Deuxième appui : doit être refusé.
            string inv2 = Invite();
            bool ok2 = PointInteraction.InteragirIci(h);
            yield return new WaitForSeconds(0.3f);
            Log("coffre " + index + " : invite '" + inv + "' interagir=" + ok + " pris=" + pris + " à " + tPris.ToString("F2") + " s, or porté " + orAvant + " → " + dj.OrPorte + " (montant attendu " + dj.MontantButin(index) + ") ; 2e appui : invite '" + inv2 + "' interagir=" + ok2 + ", or " + dj.OrPorte);
        }
    }
}
