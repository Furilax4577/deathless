using UnityEngine;

namespace Deathless.Jeu
{
    /// Écume de la rivière du village (carte v5, 02/10/2026) : petites gemmes blanches aplaties qui naissent contre les
    /// pierres des gués et les piles des ponts et filent vers l'aval, puis s'éteignent par la taille. Une seule réserve
    /// de gemmes (GemmesVolantes, Relic/VertexColorUnlit) pour toute la rivière ; couleurs du thème Eau, assombries la
    /// nuit (DayCycle.Night). Points et sens du courant écrits par VillageBuilder (menu Deathless > Village > v5).
    public class EcumeRiviere : MonoBehaviour
    {
        [Tooltip("Points d'écume (repère monde) : pierres des gués, piles des ponts.")]
        public Vector3[] points;
        [Tooltip("Sens du courant en chaque point (horizontal, normé).")]
        public Vector3[] aval;
        [Tooltip("Rayon de l'obstacle (m) : l'écume naît sur son pourtour, côté amont et sur les flancs.")]
        public float[] rayons;
        public Material materiauGemmes;
        [Tooltip("Gemmes par seconde et par point.")] public float debitParPoint = 2.4f;
        public float vitesseCourant = 0.9f;
        public float nuitFacteur = 0.45f;

        GemmesVolantes m_Gemmes;
        float m_Accu;
        Color m_Ecume, m_Clair;
        int m_Version = -1;

        void OnEnable()
        {
            if (materiauGemmes == null || points == null || points.Length == 0) return;
            if (m_Gemmes == null)
            {
                m_Gemmes = GemmesVolantes.Creer("Ecume_Gemmes", materiauGemmes, Mathf.Clamp(Mathf.CeilToInt(points.Length * debitParPoint * 1.8f), 64, 600));
                m_Gemmes.transform.SetParent(transform, false);
            }
            m_Gemmes.gameObject.SetActive(true);
        }

        void OnDisable() { if (m_Gemmes != null) m_Gemmes.gameObject.SetActive(false); }

        void Update()
        {
            if (m_Gemmes == null || points == null || points.Length == 0) return;
            if (m_Version != VfxPalette.Version)
            {
                m_Version = VfxPalette.Version;
                m_Ecume = VfxPalette.Accent(VfxTheme.Eau, "Ecume", new Color(0.95f, 0.98f, 1f));
                m_Clair = VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Coeur, new Color(0.8f, 0.93f, 1f));
            }
            float n = Mathf.Lerp(1f, nuitFacteur, DayCycle.Night);
            m_Accu += debitParPoint * points.Length * Time.deltaTime;
            while (m_Accu >= 1f)
            {
                m_Accu -= 1f;
                int k = Random.Range(0, points.Length);
                Vector3 a = aval != null && k < aval.Length ? aval[k] : Vector3.forward;
                float r = rayons != null && k < rayons.Length ? rayons[k] : 0.4f;
                Vector3 cote = Vector3.Cross(Vector3.up, a);
                float ang = Random.Range(-100f, 100f) * Mathf.Deg2Rad;      // pourtour, côté amont et flancs
                Vector3 p = points[k] + (-a * Mathf.Cos(ang) + cote * Mathf.Sin(ang)) * r + Vector3.up * 0.03f;
                Vector3 v = a * vitesseCourant * Random.Range(0.8f, 1.3f) + cote * Mathf.Sin(ang) * 0.35f;
                Color c = (Random.value < 0.7f ? m_Ecume : m_Clair) * n; c.a = 1f;
                m_Gemmes.Emettre(p, v, Random.Range(0.1f, 0.2f), Random.Range(0.9f, 1.6f), c, 0f, 0.5f, 0.12f, 0.45f,
                    new Vector3(Random.Range(1.1f, 1.6f), 0.28f, Random.Range(1.1f, 1.6f)));
            }
        }
    }
}
