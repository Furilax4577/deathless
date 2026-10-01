using UnityEngine;

namespace Deathless.Jeu
{
    /// Cascade du village (carte v5, 01/10/2026) : elle sort de la ravine de la montagne, au nord, et tombe dans le
    /// bassin d'où part la rivière. Langage commun des effets : gemmes low poly à couleurs par sommet (GemmesVolantes,
    /// shader Relic/VertexColorUnlit), couleurs du thème Eau (VfxPalette, jamais de vert : le vert est à Nyxessa),
    /// sur un voile d'eau (maillage posé par VillageBuilder, matériau EauLowPoly). Trois familles de gemmes : filets
    /// étirés qui tombent le long du voile, écume qui rejaillit au pied, embrun lent au-dessus du bassin. La nuit, les
    /// teintes s'assombrissent avec DayCycle.Night (l'eau ne luit pas). Son : boucle « village_cascade » (catalogue).
    public class CascadeVillage : MonoBehaviour
    {
        [Tooltip("Lèvre de la chute (centre) ; la chute part dans son axe avant (forward) et tombe jusqu'à `pied`.")]
        public Transform levre;
        [Tooltip("Coude (facultatif) : bas de la chute libre, sur l'éboulis ; de là l'eau dévale jusqu'au pied.")]
        public Transform coude;
        [Tooltip("Point d'impact au pied de la chute, à la surface du bassin.")]
        public Transform pied;
        [Tooltip("Largeur de la chute (m), le long de l'axe droit de la lèvre.")]
        public float largeur = 2.4f;
        [Tooltip("Matériau des gemmes (Relic/VertexColorUnlit : PortalVoxel.mat ou une copie).")]
        public Material materiauGemmes;
        public int capacite = 900;
        [Tooltip("Filets par seconde.")] public float debitFilets = 110f;
        [Tooltip("Gemmes d'écume par seconde.")] public float debitEcume = 70f;
        [Tooltip("Gemmes d'embrun par seconde.")] public float debitEmbrun = 9f;
        [Tooltip("Assombrissement à la pleine nuit (facteur des teintes).")] public float nuitFacteur = 0.42f;
        [Tooltip("Volume de la boucle sonore.")] public float volume = 0.8f;

        public static readonly string[] SonCascade = { "village_cascade" };

        GemmesVolantes m_Gemmes;
        float m_Filets, m_Ecume, m_Embrun;
        Color[] m_Teintes;
        int m_Version = -1;
        AudioSource m_Son;

        void OnEnable()
        {
            if (materiauGemmes == null || levre == null || pied == null) return;
            if (m_Gemmes == null)
            {
                m_Gemmes = GemmesVolantes.Creer("Cascade_Gemmes", materiauGemmes, capacite);
                m_Gemmes.transform.SetParent(transform, false);
            }
            m_Gemmes.gameObject.SetActive(true);
        }

        void Start()
        {
            if (m_Son == null && levre != null)
                m_Son = AudioBank.Boucle(SonCascade, pied != null ? pied : levre, volume);
        }

        void OnDisable() { if (m_Gemmes != null) m_Gemmes.gameObject.SetActive(false); }

        Color Teinte(int k)
        {
            if (m_Teintes == null || m_Version != VfxPalette.Version)
            {
                m_Version = VfxPalette.Version;
                m_Teintes = new[]
                {
                    VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Ombre, new Color(0.11f, 0.24f, 0.4f)),
                    VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Base, new Color(0.2f, 0.45f, 0.7f)),
                    VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Vif, new Color(0.45f, 0.72f, 0.9f)),
                    VfxPalette.Couleur(VfxTheme.Eau, VfxRole.Coeur, new Color(0.8f, 0.93f, 1f)),
                    VfxPalette.Accent(VfxTheme.Eau, "Ecume", new Color(0.95f, 0.98f, 1f)),
                };
            }
            float n = Mathf.Lerp(1f, nuitFacteur, DayCycle.Night);
            Color c = m_Teintes[Mathf.Clamp(k, 0, m_Teintes.Length - 1)] * n;
            c.a = 1f;
            return c;
        }

        void Update()
        {
            if (m_Gemmes == null || levre == null || pied == null) return;
            float dt = Time.deltaTime;
            Vector3 l = levre.position, droite = levre.right, avant = levre.forward;
            Vector3 b = pied.position;
            Vector3 k = coude != null ? coude.position : b;
            const float g = 9.81f;
            // 1. chute libre de la lèvre au coude : vitesse horizontale qui amène la gemme au-dessus du coude
            float h = Mathf.Max(0.5f, l.y - k.y);
            float tChute = Mathf.Sqrt(2f * h / g);
            Vector3 horiz = k - l; horiz.y = 0f;
            Vector3 vH = horiz / tChute;
            // 2. dévalée du coude au pied (nappe sur l'éboulis), vitesse constante
            Vector3 pente = b - k; float lPente = pente.magnitude;
            const float vitessePente = 4.5f;

            m_Filets += debitFilets * dt;
            while (m_Filets >= 1f)
            {
                m_Filets -= 1f;
                float s = Random.Range(-0.5f, 0.5f) * largeur;
                Color teinte = Teinte(Random.value < 0.55f ? 2 : Random.value < 0.6f ? 1 : 3);
                float taille = Random.Range(0.16f, 0.3f);
                Vector3 p = l + droite * s + avant * Random.Range(-0.1f, 0.15f);
                Vector3 v = vH + droite * Random.Range(-0.15f, 0.15f) + avant * Random.Range(-0.1f, 0.25f);
                float vie = tChute * Random.Range(0.95f, 1.03f);
                m_Gemmes.Emettre(p, v, taille, vie, teinte, g, 0f, 0.08f, 0.92f, new Vector3(0.75f, Random.Range(1.4f, 1.9f), 0.75f));
                if (coude != null && lPente > 0.3f)
                {
                    // la même eau reprend au coude (avec un retard égal à la chute) et dévale jusqu'au pied
                    float s2 = s * Random.Range(1.05f, 1.35f);
                    Vector3 p2 = k + droite * s2 + Vector3.up * 0.1f;
                    Vector3 v2 = pente / lPente * vitessePente * Random.Range(0.85f, 1.15f) + droite * s2 * 0.05f;
                    m_Gemmes.Emettre(p2, v2, taille * 0.9f, lPente / vitessePente, teinte, 0f, 0f, 0.05f, 0.9f,
                        new Vector3(0.9f, 0.7f, Random.Range(1.2f, 1.6f)), vie);
                }
            }
            m_Ecume += debitEcume * dt;
            while (m_Ecume >= 1f)
            {
                m_Ecume -= 1f;
                Vector3 p = b + droite * Random.Range(-0.5f, 0.5f) * largeur * 1.2f + Vector3.up * 0.05f;
                Vector2 o = Random.insideUnitCircle * 2.2f;
                Vector3 v = new Vector3(o.x, Random.Range(1.6f, 3.4f), o.y);
                m_Gemmes.Emettre(p, v, Random.Range(0.12f, 0.24f), Random.Range(0.45f, 0.75f), Teinte(Random.value < 0.6f ? 4 : 3), g, 0.6f,
                    0.04f, 0.5f);
            }
            m_Embrun += debitEmbrun * dt;
            while (m_Embrun >= 1f)
            {
                m_Embrun -= 1f;
                Vector2 o = Random.insideUnitCircle * 2.6f;
                Vector3 p = b + new Vector3(o.x, Random.Range(0.3f, 1.2f), o.y);
                Vector3 v = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.25f, 0.6f), Random.Range(-0.3f, 0.3f)) - avant * 0.3f;
                m_Gemmes.Emettre(p, v, Random.Range(0.35f, 0.6f), Random.Range(1.6f, 2.6f), Teinte(4) * 0.92f, -0.05f, 0.4f,
                    0.5f, 0.35f);
            }
        }
    }
}
