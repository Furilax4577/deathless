using UnityEngine;

// Filet d'énergie de la canalisation (26/09/2026, Wiki nyxessa.md « Canalisation », palier 4 du bouclier) : lien continu
// en gemmes vertes (thème Nyxessa, l'exception « vert autorisé » de la relique) entre le cristal du bâton levé du
// sorcier et le cristal de Nyxessa, tant qu'il canalise (bouclier levé, palier >= 4, la nuit). Câble qui ondule
// légèrement et le long duquel des gemmes voyagent en boucle (effet de bandeau défilant sur la couleur/taille). `Pulse()` illumine brièvement le lien
// quand un coup encaissé fait avancer la recharge d'un missile (BouclierNyxessa.AvancerRechargeMissiles).
// Cordon souple (30/09/2026, retour de test : « trop figé, vois ça comme un cordon plus fluide ») : le lien est une
// corde simulée (intégration de Verlet, `noeuds` points, extrémités tenues par le bâton et le cristal) avec du mou, une
// légère pesanteur, un souffle lent qui la balance et de l'inertie : elle traîne quand le sorcier bouge, se tend et
// se détend ; un Pulse() la fait claquer. Les gemmes suivent la courbe lissée (Catmull-Rom) passant par les nœuds.
// API : FiletEnergie.Instance.Activer(depart, arrivee) / Desactiver() / Pulse() ; Actif.
public class FiletEnergie : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [SerializeField] private int gemmes = 140;
    [Tooltip("Amplitude de l'ondulation latérale du lien (m).")]
    [SerializeField] private float ondulation = 0.18f;
    [Tooltip("Vitesse de défilement des gemmes qui voyagent le long du lien (tours/s).")]
    [SerializeField] private float vitesseDefilement = 0.6f;
    [Header("Cordon (Verlet)")]
    [SerializeField] private int noeuds = 22;
    [Tooltip("Longueur de la corde / distance entre les extrémités : le mou qui la fait pendre.")]
    [SerializeField] private float mou = 1.08f;
    [Tooltip("Pesanteur appliquée à la corde (m/s²) : faible, c'est de l'énergie.")]
    [SerializeField] private float pesanteur = 2.5f;
    [Tooltip("Amortissement par image à 60 i/s (1 = aucun) : plus bas, la corde se calme plus vite.")]
    [SerializeField] private float amortissement = 0.94f;
    [Tooltip("Souffle lent qui balance la corde (m/s²).")]
    [SerializeField] private float souffle = 3f;
    [Tooltip("Coup de fouet d'un Pulse() (m/s), transversal, qui parcourt la corde.")]
    [SerializeField] private float fouet = 2.5f;

    public static FiletEnergie Instance { get; private set; }
    public bool Actif { get; private set; }

    /// Matériau des gemmes (PortalVoxel) : à définir avant Activer() si le composant est créé par code plutôt que
    /// posé depuis le prefab (celui-ci l'a déjà assigné dans l'inspecteur).
    public void DefinirMateriau(Material m) { materiau = m; }

    Transform m_Depart, m_Arrivee;
    Mesh m_Mesh;
    Vector3[] m_Vertices;
    Color[] m_Colors;
    float m_Presence;      // 0..1, lissé à l'activation/désactivation
    float m_DernierPulse = -99f;
    VfxLumiere m_Lumiere;
    Vector3[] m_Noeuds, m_Avant;
    bool m_CordePrete;
    float m_Pas;

    void Awake() { if (Instance == null) Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; if (m_Mesh != null) Destroy(m_Mesh); }

    static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Nyxessa, r, d); }

    public void Activer(Transform depart, Transform arrivee)
    {
        m_Depart = depart;
        m_Arrivee = arrivee;
        Actif = depart != null && arrivee != null;
        if (!Actif) return;
        Assurer();
        if (m_Lumiere == null)
        {
            m_Lumiere = VfxLumiere.Creer(null, Vector3.zero, VfxTheme.Nyxessa, VfxTailleLumiere.Petite, -1f);
            m_Lumiere.gameObject.name = "FiletEnergie_Lumiere";
        }
        m_Lumiere.Allumer();
    }

    public void Desactiver()
    {
        Actif = false;
        m_CordePrete = false;
        if (m_Lumiere != null) m_Lumiere.Eteindre();
    }

    // Brille brièvement (recharge d'un missile avancée par la canalisation).
    public void Pulse()
    {
        m_DernierPulse = Time.time;
        if (!m_CordePrete) return;
        // Coup de fouet : on recule la position précédente des nœuds du milieu, la corde claque puis se calme.
        Vector3 axe = m_Noeuds[m_Noeuds.Length - 1] - m_Noeuds[0];
        Vector3 lat = Vector3.Cross(Vector3.up, axe); if (lat.sqrMagnitude < 1e-4f) lat = Vector3.right; lat.Normalize();
        float sens = Random.value < 0.5f ? -1f : 1f;
        for (int i = 1; i < m_Noeuds.Length - 1; i++)
        {
            float k = (float)i / (m_Noeuds.Length - 1);
            m_Avant[i] -= (lat * sens + Vector3.up * 0.5f) * fouet * Mathf.Sin(k * Mathf.PI) / 60f;
        }
    }

    /// Simulation de la corde : Verlet à pas fixe (1/60 s), extrémités épinglées, contraintes de longueur.
    void SimulerCorde(Vector3 a, Vector3 b, float dt)
    {
        int n = Mathf.Max(4, noeuds);
        if (!m_CordePrete || m_Noeuds == null || m_Noeuds.Length != n)
        {
            m_Noeuds = new Vector3[n]; m_Avant = new Vector3[n];
            for (int i = 0; i < n; i++) m_Noeuds[i] = m_Avant[i] = Vector3.Lerp(a, b, (float)i / (n - 1));
            m_CordePrete = true;
        }
        float repos = Vector3.Distance(a, b) * mou / (n - 1);
        const float h = 1f / 60f;
        m_Pas = Mathf.Min(m_Pas + dt, 0.1f);
        float t = Time.time;
        while (m_Pas >= h)
        {
            m_Pas -= h;
            for (int i = 1; i < n - 1; i++)
            {
                Vector3 p = m_Noeuds[i];
                float k = (float)i / (n - 1);
                // Souffle : bruit lent, différent le long de la corde, qui la balance latéralement et verticalement.
                Vector3 vent = new Vector3(Mathf.PerlinNoise(t * 0.35f, k * 1.7f) - 0.5f, Mathf.PerlinNoise(k * 1.3f + 7f, t * 0.3f) - 0.5f,
                    Mathf.PerlinNoise(t * 0.33f + 3f, k * 1.9f + 11f) - 0.5f) * (2f * souffle);
                Vector3 acc = Vector3.down * pesanteur + vent;
                Vector3 v = (p - m_Avant[i]) * amortissement;
                m_Avant[i] = p;
                m_Noeuds[i] = p + v + acc * (h * h);
            }
            m_Noeuds[0] = a; m_Noeuds[n - 1] = b;
            for (int it = 0; it < 10; it++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 d = m_Noeuds[i + 1] - m_Noeuds[i];
                    float l = d.magnitude;
                    if (l < 1e-5f) continue;
                    Vector3 corr = d * ((l - repos) / l);
                    bool fixeA = i == 0, fixeB = i + 1 == n - 1;
                    if (fixeA && fixeB) continue;
                    if (fixeA) m_Noeuds[i + 1] -= corr;
                    else if (fixeB) m_Noeuds[i] += corr;
                    else { m_Noeuds[i] += corr * 0.5f; m_Noeuds[i + 1] -= corr * 0.5f; }
                }
            }
        }
        // Téléportation du sorcier ou de la caméra de tournage : on ne laisse pas la corde s'étirer sur des dizaines de mètres.
        if ((m_Noeuds[1] - a).sqrMagnitude > 25f * repos * repos + 4f) m_CordePrete = false;
    }

    /// Point de la corde lissée (Catmull-Rom par les nœuds), k dans 0..1 ; tangente en sortie.
    Vector3 PointCorde(float k, out Vector3 tangente)
    {
        int n = m_Noeuds.Length;
        float f = Mathf.Clamp01(k) * (n - 1);
        int i = Mathf.Min((int)f, n - 2);
        float u = f - i;
        Vector3 p0 = m_Noeuds[Mathf.Max(i - 1, 0)], p1 = m_Noeuds[i], p2 = m_Noeuds[i + 1], p3 = m_Noeuds[Mathf.Min(i + 2, n - 1)];
        float u2 = u * u, u3 = u2 * u;
        tangente = 0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * u + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * u2);
        return 0.5f * ((2f * p1) + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
    }

    void Assurer()
    {
        if (m_Mesh != null) return;
        m_Vertices = new Vector3[gemmes * LowPolyGem.VerticesPerGem];
        m_Colors = new Color[m_Vertices.Length];
        m_Mesh = new Mesh { name = "FiletEnergie" };
        m_Mesh.MarkDynamic();
        m_Mesh.vertices = m_Vertices;
        m_Mesh.colors = m_Colors;
        m_Mesh.triangles = LowPolyGem.Triangles(gemmes);
        gameObject.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void LateUpdate()
    {
        m_Presence = Mathf.MoveTowards(m_Presence, Actif && m_Depart != null && m_Arrivee != null ? 1f : 0f, Time.deltaTime / 0.4f);
        if (m_Mesh == null)
        {
            if (m_Presence <= 0f) return;
            Assurer();
        }
        if (m_Presence <= 0.001f)
        {
            for (int i = 0; i < gemmes; i++)
                LowPolyGem.Write(m_Vertices, m_Colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
            m_Mesh.vertices = m_Vertices;
            m_Mesh.colors = m_Colors;
            return;
        }
        Vector3 a = m_Depart != null ? m_Depart.position : transform.position;
        Vector3 b = m_Arrivee != null ? m_Arrivee.position : transform.position;
        SimulerCorde(a, b, Time.deltaTime);
        Vector3 milieu = PointCorde(0.5f, out _);
        Vector3 axe = (b - a);
        float longueur = axe.magnitude;
        Vector3 dirAxe = longueur > 0.01f ? axe / longueur : Vector3.forward;
        // Repère perpendiculaire (latéral horizontal, puis vertical) pour l'ondulation.
        Vector3 lateral = Vector3.Cross(Vector3.up, dirAxe); if (lateral.sqrMagnitude < 1e-3f) lateral = Vector3.right; lateral.Normalize();
        Vector3 vertical = Vector3.Cross(dirAxe, lateral).normalized;
        float t = Time.time;
        Color coeur = C(VfxRole.Coeur, new Color(0.643f, 0.925f, 0.565f));
        Color vif = C(VfxRole.Vif, new Color(0.094f, 0.541f, 0.212f));
        Color baseC = C(VfxRole.Base, new Color(0.043f, 0.322f, 0.153f));
        float eclat = Mathf.Clamp01(1f - (t - m_DernierPulse) / 0.35f);
        for (int i = 0; i < gemmes; i++)
        {
            float k = (i + 0.5f) / gemmes;   // 0..1 le long du lien
            // Position sur la corde simulée, plus un léger frémissement qui parcourt le câble (l'énergie vit).
            Vector3 position = PointCorde(k, out Vector3 tangente);
            float onde = Mathf.Sin(k * Mathf.PI * 5f + t * 3.1f) * ondulation * 0.35f * Mathf.Sin(k * Mathf.PI);
            float onde2 = Mathf.Cos(k * Mathf.PI * 4f - t * 2.3f) * ondulation * 0.2f * Mathf.Sin(k * Mathf.PI);
            position += lateral * onde + vertical * onde2;
            // Défilement : une gemme sur trois porte la vague qui voyage du bâton vers le cristal, plus brillante.
            float phase = Mathf.Repeat(k - t * vitesseDefilement, 1f);
            float voyage = Mathf.Pow(1f - Mathf.Abs(phase - 0.5f) * 2f, 6f);   // pic étroit qui parcourt 0..1 en boucle
            float taille = (0.028f + 0.02f * Mathf.Sin(k * 37f + i)) * (0.55f + 0.45f * voyage) * m_Presence;
            Color c = Color.Lerp(baseC, vif, 0.4f + 0.6f * (i % 3) / 2f);
            // HDR (26/09/2026, luminance de nuit) : le pic qui voyage sur le lien rayonne, intensité du thème Nyxessa
            // (remplace l'ancien ×1,6 en dur).
            c = Color.Lerp(c, coeur * VfxPalette.Intensite(VfxTheme.Nyxessa, 2.5f), voyage) * (1f + eclat * 1.4f);
            Quaternion r = Quaternion.LookRotation(tangente.sqrMagnitude > 1e-6f ? tangente.normalized : dirAxe, vertical);
            LowPolyGem.Write(m_Vertices, m_Colors, i, position, taille, new Vector3(0.6f, 0.6f, 1.4f), r, c, LowPolyGem.DefaultLight);
        }
        m_Mesh.vertices = m_Vertices;
        m_Mesh.colors = m_Colors;
        m_Mesh.RecalculateBounds();
        if (m_Lumiere != null)
        {
            m_Lumiere.transform.position = milieu;
            m_Lumiere.facteur = m_Presence * (0.5f + eclat * 1.5f);
        }
    }
}
