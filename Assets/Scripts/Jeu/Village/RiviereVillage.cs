using UnityEngine;

namespace Deathless.Jeu
{
    /// Rivière du village (carte v5, 01/10/2026 ; wiki : village.md, Refonte de la carte) : l'eau ne se franchit qu'aux
    /// trois gués et sur les deux ponts. Les squelettes suivent le NavMesh, où le lit est non praticable (volumes posés
    /// par VillageBuilder, menu Deathless > Village > v5) et les gués en zone « Eau » ; le héros local, lui, est ramené à
    /// la berge s'il entre dans le lit hors d'un passage. Pas de mur invisible : rien n'arrête les tirs ni la caméra.
    /// Tracé, largeur et passages écrits par VillageBuilder (données du plan Docs/outils/plan_village.py --serpente).
    public class RiviereVillage : MonoBehaviour
    {
        public static RiviereVillage Instance { get; private set; }

        [Tooltip("Axe de la rivière (points rapprochés, repère monde ; y ignoré).")]
        public Vector3[] trace;
        [Tooltip("Distance à l'axe en deçà de laquelle le héros est dans l'eau (m) : bord de l'eau + un demi-pas.")]
        public float demiLargeurInterdite = 2.45f;
        [Tooltip("Gués : centres (repère monde) ; on y passe à pied (eau aux chevilles, ralenti par ZoneEau).")]
        public Vector3[] gues;
        public float rayonGue = 2.6f;
        [Tooltip("Ponts : centres ; on y passe sur le tablier (pieds au-dessus de hauteurTablier), jamais dessous.")]
        public Vector3[] ponts;
        public float rayonPont = 4.6f;
        public float hauteurTablier = -0.05f;
        [Tooltip("Bassin au pied de la cascade : centre et rayon de l'eau infranchissable.")]
        public Vector3 centreBassin;
        public float rayonBassin;

        void OnEnable() { Instance = this; }
        void OnDisable() { if (Instance == this) Instance = null; }

        /// Point de l'axe le plus proche de `p` (plan horizontal) et distance à l'axe.
        public float DistanceAxe(Vector3 p, out Vector3 proche)
        {
            proche = p;
            float best = float.MaxValue;
            if (trace == null || trace.Length < 2) return best;
            Vector2 q = new Vector2(p.x, p.z);
            for (int i = 1; i < trace.Length; i++)
            {
                Vector2 a = new Vector2(trace[i - 1].x, trace[i - 1].z), b = new Vector2(trace[i].x, trace[i].z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                Vector2 c = a + ab * t;
                float d = (q - c).sqrMagnitude;
                if (d < best) { best = d; proche = new Vector3(c.x, p.y, c.y); }
            }
            return Mathf.Sqrt(best);
        }

        /// Vrai si `p` (pieds) est dans l'eau infranchissable : dans le lit, hors des gués et hors du tablier des ponts.
        public bool Interdit(Vector3 p, out Vector3 proche, out float distance)
        {
            distance = DistanceAxe(p, out proche);
            if (rayonBassin > 0f)
            {
                // bassin : ramené à une rivière dont l'axe est le cercle de rayon (rayonBassin - demiLargeurInterdite)
                Vector3 d = p - centreBassin; d.y = 0f;
                float db = Mathf.Max(0f, d.magnitude - (rayonBassin - demiLargeurInterdite));
                if (db < distance)
                {
                    distance = db;
                    Vector3 dir = d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.back;
                    proche = centreBassin + dir * Mathf.Max(0f, rayonBassin - demiLargeurInterdite); proche.y = p.y;
                }
            }
            if (distance >= demiLargeurInterdite) return false;
            if (gues != null) foreach (var g in gues) if (Plan(p, g) < rayonGue) return false;
            if (ponts != null && p.y > hauteurTablier) foreach (var q in ponts) if (Plan(p, q) < rayonPont) return false;
            return true;
        }

        static float Plan(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

        void LateUpdate()
        {
            var partie = Partie.Instance;
            if (partie == null) return;
            var tous = partie.TousLesHeros;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || h.Distant || h.CC == null || !h.CC.enabled || h.EnTransit) continue;
                Vector3 p = h.transform.position;
                if (!Interdit(p, out Vector3 proche, out float d)) continue;
                // Vers la berge la plus proche (dehors de l'axe), sans saut de position : déplacement par le contrôleur.
                Vector3 dehors = p - proche; dehors.y = 0f;
                if (dehors.sqrMagnitude < 1e-4f) dehors = h.transform.forward;
                dehors.Normalize();
                Vector3 cible = proche + dehors * (demiLargeurInterdite + 0.02f);
                Vector3 delta = cible - p; delta.y = 0f;
                h.CC.Move(delta + Vector3.up * 0.05f);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (trace == null) return;
            Gizmos.color = new Color(0.3f, 0.6f, 1f);
            for (int i = 1; i < trace.Length; i++) Gizmos.DrawLine(trace[i - 1], trace[i]);
            Gizmos.color = Color.white;
            if (gues != null) foreach (var g in gues) Gizmos.DrawWireSphere(g, rayonGue);
            if (ponts != null) foreach (var q in ponts) Gizmos.DrawWireSphere(q, rayonPont);
        }
#endif
    }
}
