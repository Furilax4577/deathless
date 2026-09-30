using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Règles de combat partagées par les classes : cibles d'une frappe de mêlée, coup dans le dos, tir à la tête,
    /// critique (effet et son), visée depuis le réticule.
    public static class Combat
    {
        static readonly Collider[] s_Tampon = new Collider[128];
        // Tampons internes d'Ennemis (vidés à chaque appel, jamais rendus à l'appelant).
        static readonly List<float> s_Distances = new List<float>();
        static readonly HashSet<Sante> s_Vus = new HashSet<Sante>();

        /// Écart de hauteur maximal entre le héros et sa cible de mêlée (30/09/2026, retour de test : au donjon, on frappait
        /// un ennemi de l'étage au-dessus ou au-dessous à travers le plafond) : un peu plus qu'une marche d'escalier.
        public const float EcartHauteurMax = 1.8f;

        /// Ennemis vivants devant `origine` (portée horizontale, demi-angle autour de `avant`), du plus proche au plus loin.
        /// Depuis le 30/09/2026 : même étage (EcartHauteurMax) et ligne de vue dégagée (Degage : pas de mur ni de plafond
        /// entre le torse du héros et celui de l'ennemi).
        /// Alloue une liste neuve : hors des chemins chauds, préférer la surcharge à tampon fourni.
        public static List<Sante> Ennemis(Vector3 origine, Vector3 avant, float portee, float demiAngle)
            => Ennemis(origine, avant, portee, demiAngle, new List<Sante>());

        /// Même chose dans `res` (vidée puis remplie, et renvoyée) : sans allocation. Chaque appelant garde son propre
        /// tampon (ClasseHeros.Cibles) et le parcourt entièrement avant de le redemander.
        public static List<Sante> Ennemis(Vector3 origine, Vector3 avant, float portee, float demiAngle, List<Sante> res)
        {
            res.Clear();
            var dist = s_Distances;
            dist.Clear();
            s_Vus.Clear();
            avant.y = 0f;
            int n = Physics.OverlapSphereNonAlloc(origine + Vector3.up, portee + 1.2f, s_Tampon, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var s = s_Tampon[i].GetComponentInParent<Sante>();
                if (s == null || s.equipe != Equipe.Ennemis || s.Mort || !s_Vus.Add(s)) continue;
                Vector3 d = s.transform.position - origine; d.y = 0f;
                float r = RayonDe(s);
                float dd = Mathf.Max(0f, d.magnitude - r);
                if (dd > portee) continue;
                if (demiAngle < 180f && d.sqrMagnitude > 0.04f && Vector3.Angle(avant, d) > demiAngle) continue;
                if (Mathf.Abs(s.transform.position.y - origine.y) > EcartHauteurMax) continue;
                if (!Degage(origine + Vector3.up * 1.2f, s.transform.position + Vector3.up * 1f)) continue;
                int k = 0;
                while (k < dist.Count && dist[k] < dd) k++;
                res.Insert(k, s);
                dist.Insert(k, dd);
            }
            s_Vus.Clear();
            return res;
        }

        static readonly RaycastHit[] s_Vue = new RaycastHit[16];

        /// Rien de solide entre `a` et `b` : les personnages (héros, ennemis, Nyxessa) et le feuillage de la forêt ne
        /// bloquent pas ; murs, sols, plafonds (masqués ou non) et gros décors bloquent.
        public static bool Degage(Vector3 a, Vector3 b)
        {
            Vector3 v = b - a;
            float l = v.magnitude;
            if (l < 0.05f) return true;
            int n = Physics.RaycastNonAlloc(a, v / l, s_Vue, l, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = s_Vue[i].collider;
                if (c.GetComponentInParent<Sante>() != null || FeuillageMasquage.ColliderForet(c)) continue;
                return false;
            }
            return true;
        }

        static float RayonDe(Sante s)
        {
            var sq = s.GetComponent<Squelette>();
            if (sq != null) return sq.Agent != null ? sq.Agent.radius : 0.4f;
            // Éclat de Nyx de Nyxar (EclatNyx, 30/09/2026) : posé aux pieds de Nyxar, compté un peu plus près que lui
            // pour qu'une frappe de mêlée à cible unique vise d'abord ce point faible.
            var e = s.GetComponent<EclatNyx>();
            if (e != null && e.Proprietaire != null && e.Proprietaire.Agent != null) return e.Proprietaire.Agent.radius + 0.15f;
            return 0.4f;
        }

        /// L'attaquant est dans le dos de la cible (angle > `angleDos` entre l'avant de la cible et la direction vers lui).
        public static bool DansLeDos(Transform cible, Vector3 attaquant, float angleDos)
        {
            Vector3 d = attaquant - cible.position; d.y = 0f;
            Vector3 f = cible.forward; f.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return false;
            return Vector3.Angle(f, d) > angleDos;
        }

        /// Un projectile qui arrive de `origine` dans la direction `dir` touche-t-il la tête du squelette ?
        public static bool ALaTete(Squelette s, Vector3 point, Vector3 dir)
        {
            if (s == null) return false;
            Vector3 c = s.CentreTete;
            // Distance du centre de la tête à la trajectoire (droite passant par le point d'impact).
            Vector3 v = c - point;
            float le = Vector3.Dot(v, dir.normalized);
            float d = (v - dir.normalized * le).magnitude;
            return d <= s.RayonTete && Mathf.Abs(le) < 1.5f;
        }

        /// Effet et son d'un coup critique (le compte du score passe par InfoDegats.critique).
        public static void Critique(Vector3 point, Vector3 direction, bool meilleur)
        {
            var g = EffetsJeu.Gemmes;
            if (global::Critique.Instance != null) global::Critique.Instance.Jouer(point, direction, meilleur);
            else if (g != null) global::Critique.Eclat(point, direction, meilleur, g);
            AudioBank.Jouer(meilleur ? SonsDuJeu.CritiqueMeilleur : SonsDuJeu.Critique, point, 1f);
        }

        static readonly RaycastHit[] s_Hits = new RaycastHit[32];

        /// Point visé par le réticule (centre de l'écran), en ignorant `ignorer` (le héros) : premier obstacle ou ennemi
        /// jusqu'à `portee`, sinon le point à la portée. `sante` : l'ennemi visé (ou null).
        public static Vector3 PointVise(Camera cam, Transform ignorer, float portee, out Sante sante)
        {
            sante = null;
            if (cam == null) return ignorer.position + ignorer.forward * portee + Vector3.up;
            Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // Départ du rayon au niveau du héros (la caméra est derrière lui : on ne vise pas ce qui est entre les deux).
            float avance = Vector3.Dot(ignorer.position - r.origin, r.direction);
            if (avance > 0f) r.origin += r.direction * avance;
            int n = Physics.RaycastNonAlloc(r, s_Hits, portee, ~0, QueryTriggerInteraction.Ignore);
            float best = portee;
            Vector3 p = r.origin + r.direction * portee;
            for (int i = 0; i < n; i++)
            {
                var h = s_Hits[i];
                if (h.collider.transform.IsChildOf(ignorer)) continue;
                if (h.distance < best) { best = h.distance; p = h.point; sante = h.collider.GetComponentInParent<Sante>(); }
            }
            if (sante != null && sante.equipe != Equipe.Ennemis) sante = null;
            // Petite aide à la visée : un ennemi frôlé par le réticule (0,45 m) avant l'obstacle est visé au point le plus
            // proche du rayon sur sa capsule.
            if (sante == null)
            {
                n = Physics.SphereCastNonAlloc(r, 0.45f, s_Hits, best, ~0, QueryTriggerInteraction.Ignore);
                float bestE = best;
                for (int i = 0; i < n; i++)
                {
                    var h = s_Hits[i];
                    if (h.collider.transform.IsChildOf(ignorer)) continue;
                    var s = h.collider.GetComponentInParent<Sante>();
                    if (s == null || s.equipe != Equipe.Ennemis || s.Mort || h.distance >= bestE) continue;
                    bestE = h.distance;
                    sante = s;
                    Vector3 c = h.collider.bounds.center;
                    Vector3 surRayon = r.origin + r.direction * Vector3.Dot(c - r.origin, r.direction);
                    p = h.collider.ClosestPoint(surRayon);
                }
            }
            return p;
        }
    }
}
