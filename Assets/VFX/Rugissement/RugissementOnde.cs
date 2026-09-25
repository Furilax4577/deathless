using UnityEngine;

// Onde du rugissement, concentrique au personnage puis rappelée (« venez ! »), dans le langage gemmes de Relic : un
// anneau horizontal de petites gemmes LowPolyGem (couleurs par sommet, shader Relic/VertexColorUnlit), centré sur le
// personnage à hauteur `hauteur`, qui part du centre, s'élargit jusqu'à `rayonMax` en `aller` s, puis revient vers le
// personnage en `retour` s en se resserrant et se dissipe au centre ; au point le plus large, quelques gemmes s'en
// détachent vers l'extérieur et reviennent aussi en tourbillonnant (aspiration). Tout par taille et couleur, pas
// d'alpha. Un seul maillage, positions dans le repère de l'objet (posé au centre du personnage, sans rotation ni
// échelle). Purement visuel et local.
public class RugissementOnde : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Hauteur de l'anneau au-dessus de l'objet (m) ; 0 = à hauteur de poitrine si l'objet est au centre de la capsule.")]
    [SerializeField] private float hauteur = 0f;
    [SerializeField] private float rayonMax = 4.5f;
    [SerializeField] private float aller = 0.4f;
    [SerializeField] private float retour = 0.5f;
    [SerializeField] private int gemmesAnneau = 72;
    [SerializeField] private int gemmesDetachees = 16;
    [SerializeField] private Vector2 taille = new Vector2(0.05f, 0.09f);

    private static readonly Color RougeVif = new Color(0.7f, 0.15f, 0.12f);     // #b3261e
    private static readonly Color RougeSombre = new Color(0.43f, 0.08f, 0.06f); // #6e1410
    private static readonly Color RougePale = new Color(1f, 0.45f, 0.35f);      // éclair au point le plus large

    private Mesh mesh;
    private MeshRenderer rendu;
    private Vector3[] vertices;
    private Color[] colors;
    private float[] angle;
    private float[] radial;
    private float[] eleve;
    private float[] tailles;
    private Color[] teintes;
    private Quaternion[] rotations;
    private Vector3[] axes;
    private float[] phases;
    private float debut = -100f;

    public float Duree { get { return aller + retour; } }
    public bool EnCours { get { return debut >= 0f; } }

    private void Awake()
    {
        rendu = GetComponent<MeshRenderer>();
        if (rendu == null) rendu = gameObject.AddComponent<MeshRenderer>();
        MeshFilter filtre = GetComponent<MeshFilter>();
        if (filtre == null) filtre = gameObject.AddComponent<MeshFilter>();
        if (materiau != null) rendu.sharedMaterial = materiau;
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;
        rendu.enabled = false;
        int n = gemmesAnneau + gemmesDetachees;
        vertices = new Vector3[n * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "RugissementOnde" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(n);
        filtre.sharedMesh = mesh;
        angle = new float[n]; radial = new float[n]; eleve = new float[n]; tailles = new float[n]; teintes = new Color[n];
        rotations = new Quaternion[n]; axes = new Vector3[n]; phases = new float[n];
        for (int i = 0; i < n; i++)
        {
            angle[i] = i < gemmesAnneau ? i * Mathf.PI * 2f / gemmesAnneau + Random.Range(-0.04f, 0.04f) : Random.Range(0f, Mathf.PI * 2f);
            radial[i] = Random.Range(-0.15f, 0.15f);
            eleve[i] = Random.Range(-0.12f, 0.12f);
            tailles[i] = Random.Range(taille.x, taille.y);
            teintes[i] = Random.value < 0.33f ? RougeSombre : RougeVif;
            rotations[i] = Random.rotation;
            axes[i] = Random.onUnitSphere;
            phases[i] = Random.value;
        }
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    public void Jouer()
    {
        if (mesh == null)
            return;
        debut = Time.time;
        rendu.enabled = true;
        Appliquer(0f);
    }

    private void Update()
    {
        if (debut < 0f)
            return;
        float t = Time.time - debut;
        if (t >= Duree)
        {
            debut = -100f;
            rendu.enabled = false;
            return;
        }
        Appliquer(t);
    }

    // Pose l'onde `t` secondes après le départ (aussi pour les captures).
    public void Appliquer(float t)
    {
        Vector3 centre = Vector3.up * hauteur;
        bool retourPhase = t >= aller;
        float k = retourPhase ? Mathf.Clamp01((t - aller) / retour) : Mathf.Clamp01(t / aller);
        // Aller : l'anneau s'ouvre vite puis décélère ; retour : il se resserre en accélérant vers le centre et s'éteint.
        float rayon = retourPhase ? Mathf.Lerp(rayonMax, 0.1f, k * k) : Mathf.Lerp(0.3f, rayonMax, 1f - (1f - k) * (1f - k));
        float presence = retourPhase ? 1f - k * k : 1f;
        float eclair = retourPhase ? Mathf.Clamp01(1f - k * 3f) : Mathf.Clamp01((k - 0.7f) / 0.3f);
        Bounds bounds = new Bounds(centre, new Vector3((rayonMax + 1.5f) * 2f, 1.5f, (rayonMax + 1.5f) * 2f));
        for (int i = 0; i < angle.Length; i++)
        {
            Vector3 position;
            float size;
            Color color;
            if (i < gemmesAnneau)
            {
                float a = angle[i] + t * 1.2f;
                float r = rayon + radial[i] * Mathf.Clamp01(rayon / rayonMax);
                position = centre + new Vector3(Mathf.Cos(a) * r, eleve[i] * Mathf.Clamp01(rayon / rayonMax), Mathf.Sin(a) * r);
                size = tailles[i] * presence;
                color = Color.Lerp(teintes[i], RougePale, eclair * 0.7f);
            }
            else
            {
                // Détachées au point le plus large : elles partent un peu plus loin puis reviennent au centre en tourbillonnant.
                if (!retourPhase) { LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight); continue; }
                float delay = phases[i] * 0.35f;
                float local = Mathf.Clamp01((k - delay) / (1f - delay));
                Vector3 dir = new Vector3(Mathf.Cos(angle[i]), 0f, Mathf.Sin(angle[i]));
                Vector3 from = centre + dir * (rayonMax + 0.9f);
                float travel = local * local;
                position = Vector3.Lerp(from, centre, travel);
                // Tourbillon autour de l'axe vertical : l'angle avance en s'approchant du centre.
                float sa = angle[i] + travel * Mathf.PI * 1.5f;
                float sr = Vector3.Distance(position, centre);
                position = centre + new Vector3(Mathf.Cos(sa) * sr, Mathf.Sin(travel * Mathf.PI) * 0.5f + eleve[i], Mathf.Sin(sa) * sr);
                size = tailles[i] * Mathf.Clamp01(local * 6f) * (1f - travel);
                color = Color.Lerp(teintes[i], RougePale, 0.4f * (1f - travel));
            }
            Quaternion spin = Quaternion.AngleAxis(t * (200f + phases[i] * 200f), axes[i]) * rotations[i];
            LowPolyGem.Write(vertices, colors, i, position, size, new Vector3(0.7f, 1.3f, 0.7f), spin, color, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.bounds = bounds;
    }
}
