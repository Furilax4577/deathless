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
    /// luit pas). Remous (retour du 02/10/2026, « plus de remous dans la chute ») : aux points où la nappe frôle la roche (foyers
    /// relevés par lancers de rayons à la construction, VillageBuilder.V5FoyersCascade : saillies, replats, bords) naissent de
    /// petits jets de gemmes claires, des tourbillons (spirale de gemmes) et des bandes d'écume le long des deux bords ; au pied,
    /// des plaques d'écume plates et de grands anneaux s'étalent sur le bassin. Tout reste fin (5 à 13 cm) et dans le même
    /// maillage dynamique que le reste : un seul appel de dessin. Son : boucle « village_cascade » (catalogue).
    /// Centre des effets du pied (retour du 02/10/2026 : « le centre de l'animation n'est pas aligné avec la cascade » : tourbillon
    /// et anneaux étaient décalés vers l'avant et la gauche de la nappe) : à l'activation, le pied est MESURÉ sur le maillage de
    /// la nappe (centroïde des sommets les plus bas, ramené à la hauteur de l'eau du bassin, axe du pied par analyse en composantes
    /// principales) ; tous les effets du pied (impact, plaques, anneaux, grands anneaux, embrun, son) et les anneaux du shader de
    /// l'eau (global `_DeathlessCascadePied`, EauRiviere) sont centrés sur ce point, au lieu du dernier point de la colonne du
    /// milieu (devant la nappe, 20 cm au-dessus de l'eau) et du centre du disque du bassin (1 m à l'ouest de l'axe de la nappe).
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
        public int capacite = 2000;
        [Tooltip("Filets par seconde (toute la chute).")] public float debitFilets = 240f;
        [Tooltip("Écume fine par seconde sur les deux bords de la nappe.")] public float debitBords = 55f;
        [Tooltip("Écume fine par seconde au point d'impact dans le bassin.")] public float debitImpact = 170f;
        [Tooltip("Écume fine par seconde là où la nappe rebondit sur les éboulis (dernier quart du chemin).")] public float debitEboulis = 30f;
        [Tooltip("Éclaboussures par seconde à la lèvre.")] public float debitLevre = 14f;
        [Tooltip("Embrun léger au-dessus du bassin (par seconde).")] public float debitEmbrun = 9f;
        [Tooltip("Anneaux dans le bassin : un anneau toutes les `periodeAnneaux` s.")] public float periodeAnneaux = 0.75f;
        [Tooltip("Assombrissement à la pleine nuit (facteur des teintes).")] public float nuitFacteur = 0.42f;
        [Header("Remous (points de contact eau / roche)")]
        [Tooltip("Foyers de saillie ou de replat (repère monde), leur normale de roche et leur force (0 à 1).")]
        public Vector3[] foyers;
        public Vector3[] foyersNormales;
        public float[] foyersForce;
        [Tooltip("Points de bord de la nappe (gauche et droite), leur normale et leur force.")]
        public Vector3[] bords;
        public Vector3[] bordsNormales;
        public float[] bordsForce;
        [Tooltip("Petits jets de gemmes aux foyers (par seconde).")] public float debitJets = 62f;
        [Tooltip("Tourbillons (huit gemmes en spirale) aux foyers (par seconde).")] public float debitTourbillons = 2.4f;
        [Tooltip("Gemmes d'écume le long des deux bords de la nappe (par seconde).")] public float debitBandes = 80f;
        [Tooltip("Plaques d'écume plates qui s'étalent sur le bassin au pied (par seconde).")] public float debitPlaques = 36f;
        [Tooltip("Grands anneaux de remous sur le bassin : un tous les `periodeRemous` s.")] public float periodeRemous = 1.3f;
        [Tooltip("Volume de la boucle sonore.")] public float volume = 0.9f;
        [Header("Pied mesuré sur la nappe")]
        [Tooltip("Maillage de la nappe (le pied est mesuré sur ses sommets les plus bas) ; à défaut, l'enfant « Voile ».")]
        public MeshFilter voile;
        [Tooltip("Hauteur (m) au-dessus du sommet le plus bas où un sommet compte encore pour le pied : poids plein jusqu'à la moitié, nul à cette hauteur.")]
        public float bandePied = 0.5f;
        [Tooltip("Hauteur (m) des gemmes plates (anneaux, plaques, impact) au-dessus de l'eau : plus que les vagues de l'eau (5 cm).")]
        public float hauteurSurface = 0.07f;

        public static readonly string[] SonCascade = { "village_cascade" };
        /// Centre du pied pour le shader de l'eau : (x, z) monde, w = 1 quand il est posé (EauRiviere : anneaux du bassin).
        public static readonly int s_PiedEau = Shader.PropertyToID("_DeathlessCascadePied");

        /// Pied mesuré de la nappe (monde, à la hauteur de l'eau), axe horizontal du pied et largeur de la ligne de contact.
        public Vector3 PiedMesure { get { return m_Pied; } }
        public Vector3 AxePied { get { return m_AxePied; } }
        public float LargeurMesuree { get { return m_LargPied; } }
        Vector3 m_Pied, m_AxePied = Vector3.right;
        float m_LargPied = 2.4f, m_Niveau;
        bool m_Mesure;

        GemmesVolantes m_Gemmes;
        float m_Filets, m_Bords, m_Impact, m_Eboulis, m_Levre, m_Embrun, m_Anneau;
        static readonly int s_Nuit = Shader.PropertyToID("_DeathlessNuit");
        Color[] m_Teintes;
        int m_Version = -1;
        AudioSource m_Son;
        float[] m_Cumul;      // longueur cumulée du chemin
        float m_Longueur;
        float m_Jets, m_Tourb, m_Bandes, m_Plaques, m_Remous;

        /// Indice d'un foyer tiré au hasard, plus souvent parmi les plus forts (rejet) ; -1 s'il n'y en a pas.
        static int Tirer(float[] force)
        {
            if (force == null || force.Length == 0) return -1;
            for (int essai = 0; essai < 8; essai++)
            {
                int i = Random.Range(0, force.Length);
                if (Random.value <= force[i]) return i;
            }
            return Random.Range(0, force.Length);
        }

        /// Remous aux points de contact eau / roche : jets, tourbillons, bandes d'écume des bords (toute la hauteur de la chute).
        void Remous(float dt)
        {
            const float g = 9.81f;
            if (foyers != null && foyersNormales != null && foyers.Length > 0 && foyersNormales.Length == foyers.Length && foyersForce != null && foyersForce.Length == foyers.Length)
            {
                // petits jets : la nappe bute sur la saillie, quelques gemmes claires jaillissent et retombent
                m_Jets += debitJets * dt;
                while (m_Jets >= 1f)
                {
                    m_Jets -= 1f;
                    int i = Tirer(foyersForce); if (i < 0) break;
                    Vector3 n = foyersNormales[i];
                    Vector3 pos = foyers[i] + n * Random.Range(0.04f, 0.14f) + droite * Random.Range(-0.12f, 0.12f);
                    Vector3 v = n * Random.Range(0.4f, 1.3f) + Vector3.up * Random.Range(0.6f, 2.0f) + droite * Random.Range(-0.5f, 0.5f);
                    m_Gemmes.Emettre(pos, v, Random.Range(0.05f, 0.11f), Random.Range(0.45f, 0.8f), Teinte(Random.value < 0.7f ? 4 : 3), g * 0.7f, 0.4f, 0.04f, 0.45f);
                }
                // tourbillons : huit gemmes semées en spirale dans le plan de la roche, qui glissent avec le courant
                m_Tourb += debitTourbillons * dt;
                while (m_Tourb >= 1f)
                {
                    m_Tourb -= 1f;
                    int i = Tirer(foyersForce); if (i < 0) break;
                    Vector3 n = foyersNormales[i];
                    Vector3 t1 = Vector3.Cross(n, Vector3.up); if (t1.sqrMagnitude < 0.01f) t1 = Vector3.right;
                    t1.Normalize(); Vector3 t2 = Vector3.Cross(n, t1);
                    float a0 = Random.value * Mathf.PI * 2f, sens = Random.value < 0.5f ? 1f : -1f;
                    float echelle = Mathf.Lerp(0.7f, 1.2f, foyersForce[i]);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = a0 + sens * k * 0.85f, r = Mathf.Lerp(0.26f, 0.07f, k / 7f) * echelle;
                        Vector3 pos = foyers[i] + n * 0.08f + (t1 * Mathf.Cos(a) + t2 * Mathf.Sin(a)) * r;
                        Vector3 v = Vector3.Cross(n, (pos - foyers[i]).normalized) * sens * 0.2f - Vector3.up * 0.7f;
                        m_Gemmes.Emettre(pos, v, Random.Range(0.05f, 0.085f), 0.34f, Teinte(k % 3 == 0 ? 3 : 4), 0f, 0.3f, 0.05f, 0.35f, default(Vector3), k * 0.035f);
                    }
                }
            }
            // bandes d'écume le long des deux bords : petites gemmes claires qui filent avec le courant contre la roche
            if (bords != null && bordsNormales != null && bords.Length > 0 && bordsNormales.Length == bords.Length && bordsForce != null && bordsForce.Length == bords.Length)
            {
                m_Bandes += debitBandes * dt;
                while (m_Bandes >= 1f)
                {
                    m_Bandes -= 1f;
                    int i = Tirer(bordsForce); if (i < 0) break;
                    float cote = Vector3.Dot(bords[i] - m_Pied, droite) < 0f ? -1f : 1f;   // côté de la nappe, relatif au pied mesuré
                    Vector3 n = bordsNormales[i];
                    Vector3 pos = bords[i] + droite * (cote * Random.Range(-0.05f, 0.12f)) + n * Random.Range(0.04f, 0.14f) + Vector3.down * Random.Range(-0.2f, 0.2f);
                    Vector3 v = Vector3.down * Random.Range(1.2f, 3.0f) + n * Random.Range(0.1f, 0.4f) + droite * (cote * Random.Range(0.0f, 0.4f));
                    m_Gemmes.Emettre(pos, v, Random.Range(0.045f, 0.09f), Random.Range(0.4f, 0.7f), Teinte(Random.value < 0.8f ? 4 : 3), 3f, 0.3f, 0.04f, 0.5f);
                }
            }
            // au pied : plaques d'écume plates qui s'étalent sur le bassin, et grands anneaux de remous
            Vector3 b = m_Pied;
            m_Plaques += debitPlaques * dt;
            while (m_Plaques >= 1f)
            {
                m_Plaques -= 1f;
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(0.4f, 1.6f);
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                m_Gemmes.Emettre(b + d * r + Vector3.up * hauteurSurface, d * Random.Range(0.5f, 1.3f), Random.Range(0.07f, 0.13f), Random.Range(1.2f, 2.0f), Teinte(Random.value < 0.7f ? 4 : 3),
                    0f, 0.9f, 0.15f, 0.4f, new Vector3(1.6f, 0.2f, 1.6f));
            }
            m_Remous += dt;
            if (m_Remous >= periodeRemous)
            {
                m_Remous -= periodeRemous;
                int nb = 26; float a0 = Random.value * Mathf.PI * 2f;
                for (int i = 0; i < nb; i++)
                {
                    float a = a0 + i * Mathf.PI * 2f / nb;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    m_Gemmes.Emettre(b + d * 1.3f + Vector3.up * hauteurSurface, d * 1.9f, 0.11f, 1.9f, Teinte(i % 4 == 0 ? 3 : 4), 0f, 0.5f, 0.2f, 0.35f, new Vector3(1.6f, 0.2f, 1.6f));
                }
            }
        }

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
            MesurerPied();
        }

        /// Niveau de l'eau du bassin (m) : surface du maillage « Eau » de la rivière (frère du groupe « Cascade »), à défaut `defaut`.
        float NiveauEau(float defaut)
        {
            Transform racine = transform.parent;
            Transform e = racine != null ? racine.Find("Eau") : null;
            Renderer r = e != null ? e.GetComponent<Renderer>() : null;
            return r != null ? r.bounds.center.y : defaut;
        }

        /// Mesure le pied de la nappe sur son maillage : centroïde (x, z) des sommets les plus bas (poids 1 jusqu'à `bandePied`/2
        /// au-dessus du plus bas, nul à `bandePied`), posé à la hauteur de l'eau ; axe de la ligne de contact = direction principale
        /// des mêmes sommets (la nappe, plaquée sur les éboulis, touche l'eau en biais) et sa largeur. Sans maillage lisible : le
        /// repère `pied` posé par le constructeur.
        public void MesurerPied()
        {
            m_Pied = pied != null ? pied.position : transform.position;
            m_AxePied = droite.sqrMagnitude > 0.01f ? new Vector3(droite.x, 0f, droite.z).normalized : Vector3.right;
            m_Niveau = NiveauEau(m_Pied.y);
            m_Mesure = false;
            MeshFilter mf = voile;
            if (mf == null) { Transform t = transform.Find("Voile"); if (t != null) mf = t.GetComponent<MeshFilter>(); }
            Mesh m = mf != null ? mf.sharedMesh : null;
            if (m != null && m.isReadable && m.vertexCount > 2)
            {
                Vector3[] vs = m.vertices;
                Matrix4x4 l2w = mf.transform.localToWorldMatrix;
                float minY = float.MaxValue;
                for (int i = 0; i < vs.Length; i++) { vs[i] = l2w.MultiplyPoint3x4(vs[i]); if (vs[i].y < minY) minY = vs[i].y; }
                float demiBande = Mathf.Max(0.02f, bandePied * 0.5f);
                double sw = 0, sx = 0, sz = 0;
                for (int i = 0; i < vs.Length; i++)
                {
                    float w = Mathf.Clamp01((bandePied - (vs[i].y - minY)) / demiBande);
                    if (w <= 0f) continue;
                    sw += w; sx += w * vs[i].x; sz += w * vs[i].z;
                }
                if (sw > 1e-3)
                {
                    float cx = (float)(sx / sw), cz = (float)(sz / sw);
                    double sxx = 0, sxz = 0, szz = 0;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        float w = Mathf.Clamp01((bandePied - (vs[i].y - minY)) / demiBande);
                        if (w <= 0f) continue;
                        double dx = vs[i].x - cx, dz = vs[i].z - cz;
                        sxx += w * dx * dx; sxz += w * dx * dz; szz += w * dz * dz;
                    }
                    if (sxx + szz > 1e-4)
                    {
                        float a = 0.5f * Mathf.Atan2((float)(2 * sxz), (float)(sxx - szz));   // direction principale (plan x, z)
                        m_AxePied = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        if (m_AxePied.x < 0f) m_AxePied = -m_AxePied;
                    }
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        if (vs[i].y - minY > bandePied * 0.5f) continue;
                        float u = (vs[i].x - cx) * m_AxePied.x + (vs[i].z - cz) * m_AxePied.z;
                        if (u < lo) lo = u; if (u > hi) hi = u;
                    }
                    m_LargPied = Mathf.Max(1f, hi - lo);
                    m_Pied = new Vector3(cx, m_Niveau, cz);
                    m_Mesure = true;
                }
            }
            else m_Pied.y = m_Niveau;
            if (pied != null) pied.position = m_Pied;   // le son (Start) et tout lecteur du repère suivent le pied mesuré
            Shader.SetGlobalVector(s_PiedEau, new Vector4(m_Pied.x, m_Pied.z, 0f, 1f));
        }

        void Start()
        {
            if (m_Son == null && levre != null)
            {
                m_Son = AudioBank.Boucle(SonCascade, pied != null ? pied : levre, volume);
                if (m_Son != null) ConfigurerSon(m_Son);
            }
        }

        /// Atténuation propre à la cascade (retour du 02/10/2026 : « un son 3D qui s'entend à 40-50 m ») : pleine force à moins de
        /// 4 m du bassin, puis une décroissance en cloche douce (65 % à 10 m, 38 % à 20 m, 18 % à 35 m, 7 % à 46 m, rien à 50 m)
        /// au lieu de la droite par défaut (qui ne laisse que 40 % à 30 m et s'éteint à 45 m). Distance max = portée du catalogue.
        void ConfigurerSon(AudioSource s)
        {
            float max = Mathf.Max(10f, s.maxDistance);
            var courbe = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(4f / max, 1f), new Keyframe(10f / max, 0.65f), new Keyframe(20f / max, 0.38f),
                new Keyframe(35f / max, 0.18f), new Keyframe(46f / max, 0.07f), new Keyframe(1f, 0f));
            for (int i = 0; i < courbe.length; i++) courbe.SmoothTangents(i, 0.35f);
            s.rolloffMode = AudioRolloffMode.Custom;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, courbe);
            s.spread = 30f;      // un peu large : le rideau d'eau est un son étendu, pas un point
        }

        void OnDisable()
        {
            if (m_Gemmes != null) m_Gemmes.gameObject.SetActive(false);
            Shader.SetGlobalVector(s_PiedEau, Vector4.zero);   // l'eau retrouve les anneaux du disque du bassin
        }

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
            Vector3 b = m_Pied;
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
            float demi = LargeurPied() * 0.5f * 1.15f;   // largeur mesurée de la ligne de contact
            m_Impact += debitImpact * dt;
            while (m_Impact >= 1f)
            {
                m_Impact -= 1f;
                float u = (Random.value + Random.value - 1f) * demi;
                Vector3 p = b + m_AxePied * u + Vector3.up * hauteurSurface;
                Vector2 o = Random.insideUnitCircle;
                Vector3 v = new Vector3(o.x * 1.1f, Random.Range(1.4f, 3.6f), o.y * 1.1f) + m_AxePied * (u / Mathf.Max(0.1f, demi)) * 0.7f;
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
                    m_Gemmes.Emettre(b + d * 0.9f + Vector3.up * hauteurSurface, d * 1.5f, 0.12f, 1.7f, Teinte(i % 3 == 0 ? 3 : 4), 0f, 0.9f,
                        0.15f, 0.4f, new Vector3(1.6f, 0.2f, 1.6f));
                }
            }
            // 7'. remous aux points de contact eau / roche
            Remous(dt);
            // 7. embrun léger, lent, au-dessus du bassin
            m_Embrun += debitEmbrun * dt;
            while (m_Embrun >= 1f)
            {
                m_Embrun -= 1f;
                Vector2 o = Random.insideUnitCircle * 2.2f;
                Vector3 p = b + new Vector3(o.x, Random.Range(0.45f, 1.35f), o.y);
                Vector3 v = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.25f, 0.6f), Random.Range(-0.3f, 0.3f)) - avant * 0.2f;
                m_Gemmes.Emettre(p, v, Random.Range(0.12f, 0.2f), Random.Range(1.6f, 2.6f), Teinte(4) * 0.92f, -0.05f, 0.4f, 0.5f, 0.35f);
            }
        }

        float LargeurPied() { return m_Mesure ? m_LargPied : largeurs != null && largeurs.Length > 0 ? largeurs[largeurs.Length - 1] : 2.4f; }
    }
}
