using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Écoute et vérification des bruits de pas par matière (02/10/2026), en Play, sur le modèle de ScenariosRetours :
    /// `ScenariosPas.Lancer()` (ou le menu Deathless > Jeu > Pas par matière (Play)) fait marcher le héros local
    /// successivement sur chaque matière — herbe, terre, sable, pierre, bois, eau, métal (enclume) —, quelques foulées
    /// chacune (manette virtuelle EntreesSimulees : le vrai chemin du jeu, détection comprise), puis un saut en cours de
    /// route (réception : impact plus lourd, joué sur la matière de l'atterrissage), en journalisant pour chaque station la matière
    /// attendue, la matière détectée, le collider touché et les clips joués (console, préfixe [Pas]). Les stations sont
    /// cherchées dans la carte ouverte (balayage de 2 m, une ligne de 4 m de même matière et libre) : rien n'est écrit
    /// en dur sauf le métal (l'enclume de la forge, si la scène en a une). `Lancer(duree: 5f)` : plus de temps pour écouter.
    /// Résultats : ScenariosPas.Dernier ; captures : `capture` = préfixe de Assets/Screenshots/<préfixe>_<station>.png.
    public class ScenariosPas : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosPas() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosPas"); }

        static ScenariosPas s_I;
        public static string Dernier = "";
        public static bool EnCours { get; private set; }

        public static string Lancer(float duree = 3f, string capture = null)
        {
            if (!Application.isPlaying) return "hors Play : rien lancé";
            if (H == null) return "pas de héros local (lance une partie)";
            if (s_I == null) s_I = new GameObject("ScenariosPas").AddComponent<ScenariosPas>();
            s_I.StopAllCoroutines();
            s_I.StartCoroutine(s_I.Parcours(duree, capture));
            return "parcours des matières lancé (" + duree.ToString("0.#") + " s par station)";
        }

        static void Log(string t) { Dernier = t; Debug.Log("[Pas] " + t); }
        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;

        struct Station { public string nom; public Matiere attendue; public Vector3 depart; public Vector3 direction; public bool surPlace; }

        // ----------------------------------------------------------------- Recherche des stations

        static bool Libre(Vector3 a, Vector3 b)
        {
            return !Physics.CheckCapsule(a + Vector3.up * 0.5f, a + Vector3.up * 1.6f, 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !Physics.CapsuleCast(a + Vector3.up * 0.5f, a + Vector3.up * 1.6f, 0.35f, (b - a).normalized, (b - a).magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        static bool Sol(float x, float z, out Vector3 point)
        {
            point = default;
            if (!Physics.Raycast(new Vector3(x, 12f, z), Vector3.down, out var h, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            point = h.point;
            return true;
        }

        /// Première ligne de 4 m, dans une direction cardinale, dont les cinq points sont de la matière voulue (et libre).
        static bool Chercher(Matiere voulue, float rMin, float rMax, out Station s)
        {
            s = default;
            Vector3[] dirs = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
            for (float r = rMin; r <= rMax; r += 2f)
                for (int k = 0; k < Mathf.CeilToInt(2f * Mathf.PI * r / 2f); k++)
                {
                    float a = k * 2f / r;
                    float x = Mathf.Round(Mathf.Sin(a) * r), z = Mathf.Round(Mathf.Cos(a) * r);
                    if (!Sol(x, z, out var p)) continue;
                    foreach (var d in dirs)
                    {
                        bool bon = true;
                        Vector3 fin = p;
                        for (int i = 0; i <= 4 && bon; i++)
                        {
                            if (!Sol(p.x + d.x * i, p.z + d.z * i, out fin) || Mathf.Abs(fin.y - p.y) > 0.25f) { bon = false; break; }
                            if (PasMatiere.Sous(fin, null) != voulue) bon = false;
                        }
                        if (!bon || !Libre(p, fin)) continue;
                        s = new Station { nom = voulue.ToString().ToLowerInvariant(), attendue = voulue, depart = p, direction = d };
                        return true;
                    }
                }
            return false;
        }

        List<Station> Stations()
        {
            var l = new List<Station>();
            foreach (var m in new[] { Matiere.Herbe, Matiere.Terre, Matiere.Sable, Matiere.Pierre, Matiere.Bois })
                if (Chercher(m, 4f, 46f, out var s)) l.Add(s); else Log("station " + m + " : aucune ligne de 4 m trouvée");
            // Eau : un gué (ZoneEau), traversé dans sa longueur.
            foreach (var z in FindObjectsByType<Deathless.Donjon.ZoneEau>(FindObjectsSortMode.None))
            {
                var b = z.GetComponent<BoxCollider>().bounds;
                Vector3 d = b.size.x > b.size.z ? Vector3.right : Vector3.forward;
                Vector3 c = new Vector3(b.center.x, 0f, b.center.z);
                if (Sol(c.x - d.x * 1.5f, c.z - d.z * 1.5f, out var p)) { l.Add(new Station { nom = "eau", attendue = Matiere.Eau, depart = p, direction = d }); break; }
            }
            // Métal : l'enclume de la forge (posé dessus, quelques pas sur place).
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (c.name == "Meuble_Enclume") { var b = c.bounds; l.Add(new Station { nom = "metal", attendue = Matiere.Metal, depart = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z), direction = Vector3.forward, surPlace = true }); break; }
            return l;
        }

        // ----------------------------------------------------------------- Parcours

        IEnumerator Parcours(float duree, string capture)
        {
            EnCours = true;
            var h = H;
            var sb = new StringBuilder();
            int ok = 0, total = 0;
            var stations = Stations();
            bool invulnerable = h.Sante.invulnerable;
            h.Sante.invulnerable = true;
            Log(stations.Count + " stations : " + string.Join(", ", stations.ConvertAll(s => s.nom)));
            foreach (var s in stations)
            {
                var pas = new List<string>();
                var matieres = new HashSet<Matiere>();
                int ailleurs = 0;
                Vector3 origine = s.depart;
                // Seuls les pas posés sur la ligne de la station (4 m) comptent : la marche continue au-delà, sur un autre sol.
                System.Action<Matiere, Vector3, string> ecoute = (m, p, clip) =>
                {
                    Vector3 d = p - origine; d.y = 0f;
                    if (d.magnitude > 4.6f) { ailleurs++; return; }
                    matieres.Add(m); pas.Add(m + ":" + clip);
                };
                Vector3 vers = s.depart + s.direction * 4f;
                DevPartie.PlacerHeros(s.depart + Vector3.up * 0.05f, vers);
                yield return null; yield return null;
                float lacet = Mathf.Atan2(s.direction.x, s.direction.z) * Mathf.Rad2Deg;
                Quaternion face = Quaternion.LookRotation(s.direction);
                PasMatiere.Joue += ecoute;
                float t0 = Time.time;
                bool sauta = false;
                if (s.surPlace)
                {
                    // Posé sur l'enclume : trois foulées jouées d'un coup (la surface est trop petite pour marcher).
                    for (int i = 0; i < 3; i++) { PasMatiere.Pas(h.transform.position, h.transform, 1f); yield return new WaitForSeconds(0.45f); }
                }
                else
                {
                    EntreesSimulees.Stick(Vector2.up, Vector2.zero, duree);
                    while (Time.time - t0 < duree)
                    {
                        if (P.cameraJeu != null) P.cameraJeu.lacet = lacet;
                        if (!sauta && Time.time - t0 > 0.7f) { sauta = true; EntreesSimulees.Appui("buttonSouth", 0.1f); }
                        yield return null;
                    }
                    EntreesSimulees.ToutRelacher();
                    yield return new WaitForSeconds(0.9f);   // le saut retombe
                }
                PasMatiere.Joue -= ecoute;
                bool bon = matieres.Count > 0 && matieres.Contains(s.attendue) && matieres.Count == 1;
                total++; if (bon) ok++;
                string ligne = (bon ? "OK " : "ÉCART ") + s.nom + " : attendu " + s.attendue + ", détecté " + string.Join("/", matieres) + ", " + pas.Count + " pas sur la station (+" + ailleurs + " au-delà) ; " + string.Join(" ", pas.ConvertAll(x => x.Substring(x.IndexOf(':') + 1)));
                Log(ligne);
                sb.AppendLine(ligne);
                if (capture != null) DevPartie.Capturer(capture + "_" + s.nom);
                yield return new WaitForSeconds(0.3f);
            }
            h.Sante.invulnerable = invulnerable;
            string fin = ok + "/" + total + " stations conformes";
            Log(fin);
            Dernier = fin + "\n" + sb;
            EnCours = false;
        }
    }
}
