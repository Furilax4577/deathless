using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Règles de combat partagées par les classes : cibles d'une frappe de mêlée, coup dans le dos, tir à la tête,
    /// critique (effet et son), visée depuis le réticule.
    public static class Combat
    {
        static readonly Collider[] s_Tampon = new Collider[128];

        /// Ennemis vivants devant `origine` (portée horizontale, demi-angle autour de `avant`), du plus proche au plus loin.
        public static List<Sante> Ennemis(Vector3 origine, Vector3 avant, float portee, float demiAngle)
        {
            var res = new List<Sante>();
            var dist = new List<float>();
            avant.y = 0f;
            int n = Physics.OverlapSphereNonAlloc(origine + Vector3.up, portee + 1.2f, s_Tampon, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var s = s_Tampon[i].GetComponentInParent<Sante>();
                if (s == null || s.equipe != Equipe.Ennemis || s.Mort || res.Contains(s)) continue;
                Vector3 d = s.transform.position - origine; d.y = 0f;
                float r = RayonDe(s);
                float dd = Mathf.Max(0f, d.magnitude - r);
                if (dd > portee) continue;
                if (demiAngle < 180f && d.sqrMagnitude > 0.04f && Vector3.Angle(avant, d) > demiAngle) continue;
                int k = 0;
                while (k < dist.Count && dist[k] < dd) k++;
                res.Insert(k, s);
                dist.Insert(k, dd);
            }
            return res;
        }

        static float RayonDe(Sante s)
        {
            var sq = s.GetComponent<Squelette>();
            return sq != null && sq.Agent != null ? sq.Agent.radius : 0.4f;
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
