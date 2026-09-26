using UnityEngine;

// Filet d'énergie de la canalisation (26/09/2026, Wiki nyxessa.md « Canalisation », palier 4 du bouclier) : lien continu
// en gemmes vertes (thème Nyxessa, l'exception « vert autorisé » de la relique) entre le cristal du bâton levé du
// sorcier et le cristal de Nyxessa, tant qu'il canalise (bouclier levé, palier >= 4, la nuit). Câble qui ondule
// légèrement (une seule courbe de Bézier recalculée chaque image entre les deux points suivis) et le long duquel des
// gemmes voyagent en boucle (effet de bandeau défilant sur la couleur/taille). `Pulse()` illumine brièvement le lien
// quand un coup encaissé fait avancer la recharge d'un missile (BouclierNyxessa.AvancerRechargeMissiles).
// API : FiletEnergie.Instance.Activer(depart, arrivee) / Desactiver() / Pulse() ; Actif.
public class FiletEnergie : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [SerializeField] private int gemmes = 140;
    [Tooltip("Amplitude de l'ondulation latérale du lien (m).")]
    [SerializeField] private float ondulation = 0.18f;
    [Tooltip("Vitesse de défilement des gemmes qui voyagent le long du lien (tours/s).")]
    [SerializeField] private float vitesseDefilement = 0.6f;

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
        if (m_Lumiere != null) m_Lumiere.Eteindre();
    }

    // Brille brièvement (recharge d'un missile avancée par la canalisation).
    public void Pulse() { m_DernierPulse = Time.time; }

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
        Vector3 milieu = (a + b) * 0.5f;
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
            // Ondulation légère : une onde lente qui parcourt le câble (indépendante du défilement des gemmes).
            float onde = Mathf.Sin(k * Mathf.PI * 3f + t * 1.4f) * ondulation * Mathf.Sin(k * Mathf.PI);
            float onde2 = Mathf.Cos(k * Mathf.PI * 2f - t * 1.1f) * ondulation * 0.4f * Mathf.Sin(k * Mathf.PI);
            Vector3 position = Vector3.Lerp(a, b, k) + lateral * onde + vertical * onde2;
            // Défilement : une gemme sur trois porte la vague qui voyage du bâton vers le cristal, plus brillante.
            float phase = Mathf.Repeat(k - t * vitesseDefilement, 1f);
            float voyage = Mathf.Pow(1f - Mathf.Abs(phase - 0.5f) * 2f, 6f);   // pic étroit qui parcourt 0..1 en boucle
            float taille = (0.028f + 0.02f * Mathf.Sin(k * 37f + i)) * (0.55f + 0.45f * voyage) * m_Presence;
            Color c = Color.Lerp(baseC, vif, 0.4f + 0.6f * (i % 3) / 2f);
            c = Color.Lerp(c, coeur * 1.6f, voyage) * (1f + eclat * 1.4f);
            Quaternion r = Quaternion.LookRotation(dirAxe, vertical);
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
