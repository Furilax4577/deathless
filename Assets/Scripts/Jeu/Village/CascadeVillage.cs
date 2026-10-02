using UnityEngine;

namespace Deathless.Jeu
{
    /// Cascade du village (carte v5, 01/10/2026 ; refaite le 02/10/2026 après le retour de Quentin : « elle lévite », « l'eau
    /// tombe sur une dalle et l'écume est ailleurs »). Elle jaillit d'un rebord de roche (la lèvre, posée en haut de la ravine
    /// de la montagne, au nord), épouse la paroi (VillageBuilder la plaque sur la roche par lancers de rayons : aucun vide
    /// entre l'eau et la pierre), ruisselle sur les éboulis et tombe dans le bassin d'où part la rivière. Langage commun des
    /// effets : gemmes low poly à couleurs par sommet (GemmesVolantes, shader Relic/VertexColorUnlit), couleurs du thème Eau
    /// (VfxPalette, jamais de vert : le vert est à Nyxessa), sur un voile d'eau (maillage posé par VillageBuilder, matériau
    /// EauLowPoly). Familles de gemmes : filets qui glissent le long du chemin (toute la chute) ; écume FINE (nombreuses petites
    /// gemmes claires qui montent puis retombent) sur les deux bords de la nappe, sur les éboulis, à la lèvre et surtout au
    /// point d'impact dans le bassin (sur toute la largeur de la nappe, plus large de moitié) ; embrun lent au-dessus du
    /// bassin ; anneaux plats qui s'élargissent à la surface. La nuit, les teintes s'assombrissent avec DayCycle.Night (l'eau ne
    /// luit pas). Son : boucle « village_cascade » (catalogue).
    public class CascadeVillage : MonoBehaviour
    {
        [Tooltip("Lèvre de la chute (centre du rebord, là où l'eau quitte la roche).")]
        public Transform levre;
        [Tooltip("Point d'impact au pied de la chute, à la surface du bassin.")]
        public Transform pied;
        [Tooltip("Axe de la nappe, de la lèvre au bassin (repère monde, posé sur la roche).")]
        public Vector3[] chemin;
        [Tooltip("Normale de la roche en chaque point du chemin (vers le spectateur).")]
        public Vector3[] normales;
        [Tooltip("Largeur de la nappe (m) en chaque point du chemin.")]
        public float[] largeurs;
        [Tooltip("Axe latéral de la nappe (horizontal, normé).")]
        public Vector3 droite = Vector3.right;
        [Tooltip("Matériau des gemmes (Relic/VertexColorUnlit : PortalVoxel.mat ou une copie).")]
        public Material materiauGemmes;
        public int capacite = 1600;
        [Tooltip("Filets par seconde (toute la chute).")] public float debitFilets = 240f;
        [Tooltip("Écume fine par seconde sur les deux bords de la nappe.")] public float debitBords = 55f;
        [Tooltip("Écume fine par seconde au point d'impact dans le bassin.")] public float debitImpact = 170f;
        [Tooltip("Écume fine par seconde là où la nappe rebondit sur les éboulis (dernier quart du chemin).")] public float debitEboulis = 30f;
        [Tooltip("Éclaboussures par seconde à la lèvre.")] public float debitLevre = 14f;
        [Tooltip("Embrun léger au-dessus du bassin (par seconde).")] public float debitEmbrun = 9f;
        [Tooltip("Anneaux dans le bassin : un anneau toutes les `periodeAnneaux` s.")] public float periodeAnneaux = 0.75f;
        [Tooltip("Assombrissement à la pleine nuit (facteur des teintes).")] public float nuitFacteur = 0.42f;
        [Tooltip("Volume de la boucle sonore.")] public float volume = 0.8f;

        public static readonly string[] SonCascade = { "village_cascade" };

        GemmesVolantes m_Gemmes;
        float m_Filets, m_Bords, m_Impact, m_Eboulis, m_Levre, m_Embrun, m_Anneau;
        static readonly int s_Nuit = Shader.PropertyToID("_DeathlessNuit");
        Color[] m_Teintes;
        int m_Version = -1;
        AudioSource m_Son;
        float[] m_Cumul;      // longueur cumulée du chemin
        float m_Longueur;

        void OnEnable()
        {
            if (materiauGemmes == null || chemin == null || chemin.Length < 2 || pied == null) return;
            if (m_Gemmes == null)
            {
                m_Gemmes = GemmesVolantes.Creer("Cascade_Gemmes", materiauGemmes, capacite);
                m_Gemmes.transform.SetParent(transform, false);
            }
            m_Gemmes.gameObject.SetActive(true);
            m_Cumul = new float[chemin.Length];
            for (int i = 1; i < chemin.Length; i++) m_Cumul[i] = m_Cumul[i - 1] + Vector3.Distance(chemin[i - 1], chemin[i]);
            m_Longueur = Mathf.Max(0.1f, m_Cumul[chemin.Length - 1]);
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

        /// Point du chemin à l'abscisse curviligne `s` : position, tangente (sens de l'écoulement), normale de roche, largeur.
        void Point(float s, out Vector3 p, out Vector3 tan, out Vector3 nor, out float larg)
        {
            int i = 1;
            while (i < chemin.Length - 1 && m_Cumul[i] < s) i++;
            float l0 = m_Cumul[i - 1], l1 = m_Cumul[i];
            float u = l1 > l0 ? Mathf.Clamp01((s - l0) / (l1 - l0)) : 0f;
            p = Vector3.Lerp(chemin[i - 1], chemin[i], u);
            tan = (chemin[i] - chemin[i - 1]).normalized;
            nor = normales != null && normales.Length == chemin.Length ? Vector3.Lerp(normales[i - 1], normales[i], u).normalized : Vector3.back;
            larg = largeurs != null && largeurs.Length == chemin.Length ? Mathf.Lerp(largeurs[i - 1], largeurs[i], u) : 2.4f;
        }

        void Update()
        {
            Shader.SetGlobalFloat(s_Nuit, DayCycle.Night);   // eau de la rivière (EauRiviere) : assombrie la nuit
            if (m_Gemmes == null || chemin == null || chemin.Length < 2 || pied == null || m_Cumul == null) return;
            float dt = Time.deltaTime;
            Vector3 b = pied.position;
            Vector3 avant = Vector3.Cross(droite, Vector3.up);   // horizontal, vers le spectateur ou l'inverse : seulement pour l'embrun
            const float g = 9.81f;

            // 1. filets : gemmes étirées qui glissent sur la nappe, partout le long du chemin, de plus en plus vite
            m_Filets += debitFilets * dt;
            while (m_Filets >= 1f)
            {
                m_Filets -= 1f;
                float s = Random.value * m_Longueur;
                Point(s, out Vector3 p, out Vector3 tan, out Vector3 nor, out float larg);
                float u = Random.Range(-0.5f, 0.5f) * larg;
                float vit = Mathf.Lerp(2.5f, 8.5f, Mathf.Clamp01(s / (m_Longueur * 0.7f))) * Random.Range(0.85f, 1.15f);
                Color teinte = Teinte(Random.value < 0.55f ? 2 : Random.value < 0.6f ? 1 : 3);
                m_Gemmes.Emettre(p + droite * u + nor * 0.1f, tan * vit, Random.Range(0.12f, 0.24f), Random.Range(0.3f, 0.5f), teinte, 0f, 0f, 0.08f, 0.9f,
                    new Vector3(0.75f, Random.Range(1.4f, 2.0f), 0.75f));
            }
            // 2. écume fine sur les deux bords de la nappe (jets de petites gemmes claires qui retombent)
            m_Bords += debitBords * dt;
            while (m_Bords >= 1f)
            {
                m_Bords -= 1f;
                float s = Random.value * m_Longueur * 0.92f;
                Point(s, out Vector3 p, out Vector3 tan, out Vector3 nor, out float larg);
                float cote = Random.value < 0.5f ? -1f : 1f;
                Vector3 pos = p + droite * (cote * (larg * 0.5f + Random.Range(-0.1f, 0.12f))) + nor * Random.Range(0.08f, 0.22f);
                Vector3 v = droite * cote * Random.Range(0.2f, 0.8f) + nor * Random.Range(0.2f, 0.7f) + tan * Random.Range(0.5f, 2.5f) + Vector3.up * Random.Range(0.1f, 0.6f);
                m_Gemmes.Emettre(pos, v, Random.Range(0.045f, 0.1f), Random.Range(0.5f, 0.9f), Teinte(Random.value < 0.7f ? 4 : 3), 1.6f, 0.4f, 0.05f, 0.5f);
            }
            // 3. écume sur les éboulis (la nappe rebondit sur les blocs du bas)
            m_Eboulis += debitEboulis * dt;
            while (m_Eboulis >= 1f)
            {
                m_Eboulis -= 1f;
                float s = m_Longueur * Random.Range(0.74f, 0.97f);
                Point(s, out Vector3 p, out Vector3 tan, out Vector3 nor, out float larg);
                Vector3 pos = p + droite * Random.Range(-0.5f, 0.5f) * larg + nor * 0.12f;
                Vector3 v = Vector3.up * Random.Range(0.8f, 2.0f) + nor * Random.Range(0.1f, 0.6f) + droite * Random.Range(-0.5f, 0.5f);
                m_Gemmes.Emettre(pos, v, Random.Range(0.05f, 0.11f), Random.Range(0.5f, 0.85f), Teinte(4), 5f, 0.3f, 0.04f, 0.45f);
            }
            // 4. impact dans le bassin : beaucoup de petites gemmes qui montent puis retombent, sur toute la largeur de la nappe
            //    (et un peu au-delà), plus serrées au centre
            float demi = LargeurPied() * 0.5f * 1.15f;
            m_Impact += debitImpact * dt;
            while (m_Impact >= 1f)
            {
                m_Impact -= 1f;
                float u = (Random.value + Random.value - 1f) * demi;
                Vector3 p = b + droite * u + Vector3.up * 0.04f;
                Vector2 o = Random.insideUnitCircle;
                Vector3 v = new Vector3(o.x * 1.1f, Random.Range(1.4f, 3.6f), o.y * 1.1f) + droite * (u / Mathf.Max(0.1f, demi)) * 0.7f;
                m_Gemmes.Emettre(p, v, Random.Range(0.05f, 0.13f), Random.Range(0.5f, 0.95f), Teinte(Random.value < 0.65f ? 4 : 3), 6.5f, 0.4f, 0.04f, 0.45f);
            }
            // 5. lèvre : quelques éclaboussures à l'endroit où l'eau quitte le rebord
            if (levre != null)
            {
                m_Levre += debitLevre * dt;
                while (m_Levre >= 1f)
                {
                    m_Levre -= 1f;
                    Vector3 p = levre.position + droite * Random.Range(-0.5f, 0.5f) * (largeurs != null && largeurs.Length > 0 ? largeurs[0] : 2.4f) + Vector3.up * 0.05f;
                    Vector3 v = levre.forward * Random.Range(0.5f, 1.6f) + Vector3.up * Random.Range(0.6f, 1.8f) + droite * Random.Range(-0.4f, 0.4f);
                    m_Gemmes.Emettre(p, v, Random.Range(0.05f, 0.1f), Random.Range(0.45f, 0.8f), Teinte(4), g * 0.5f, 0.3f, 0.04f, 0.45f);
                }
            }
            // 6. anneaux : couronne de gemmes plates qui s'élargit à la surface du bassin, au point d'impact
            m_Anneau += dt;
            if (m_Anneau >= periodeAnneaux)
            {
                m_Anneau -= periodeAnneaux;
                int nb = 22; float a0 = Random.value * Mathf.PI * 2f;
                for (int i = 0; i < nb; i++)
                {
                    float a = a0 + i * Mathf.PI * 2f / nb;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    m_Gemmes.Emettre(b + d * 0.9f + Vector3.up * 0.02f, d * 1.5f, 0.12f, 1.7f, Teinte(i % 3 == 0 ? 3 : 4), 0f, 0.9f,
                        0.15f, 0.4f, new Vector3(1.6f, 0.2f, 1.6f));
                }
            }
            // 7. embrun léger, lent, au-dessus du bassin
            m_Embrun += debitEmbrun * dt;
            while (m_Embrun >= 1f)
            {
                m_Embrun -= 1f;
                Vector2 o = Random.insideUnitCircle * 2.2f;
                Vector3 p = b + new Vector3(o.x, Random.Range(0.3f, 1.2f), o.y);
                Vector3 v = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.25f, 0.6f), Random.Range(-0.3f, 0.3f)) - avant * 0.2f;
                m_Gemmes.Emettre(p, v, Random.Range(0.12f, 0.2f), Random.Range(1.6f, 2.6f), Teinte(4) * 0.92f, -0.05f, 0.4f, 0.5f, 0.35f);
            }
        }

        float LargeurPied() { return largeurs != null && largeurs.Length > 0 ? largeurs[largeurs.Length - 1] : 2.4f; }
    }
}
