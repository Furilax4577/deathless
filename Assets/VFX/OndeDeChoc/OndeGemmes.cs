using UnityEngine;

// Rendu en gemmes de l'onde de choc (langage gemmes de Relic, 25/09/2026) : un seul maillage LowPolyGem qui porte
// l'anneau (ou l'arc) et les éclats de terre. Couleurs par sommet terre sombre / terre claire, shader
// Relic/VertexColorUnlit (matériau dérivé de PortalVoxel). Piloté par OndeDeChoc (rayon max, durée, angle, largeurs).
// - Anneau : des gemmes réparties sur l'arc par suite dorée (chaque gemme a sa fraction d'arc fixe ; on en montre
//   autant que le périmètre le demande, `densite` par mètre, plafonné) : la densité suit le périmètre sans que les
//   gemmes déjà visibles ne bougent. Rayon dans la largeur de l'anneau, hauteur au ras du sol, taille qui suit la
//   largeur (l'anneau s'amincit en s'élargissant) puis s'éteint.
// - Éclats : deux salves (comme les anciens émetteurs : 26 gros puis 16 petits 0,03 s après) projetées en couronne
//   avec une vitesse radiale liée à rayonMax / durée, un coup de pied vertical qui s'amortit, la gravité (0,7 g),
//   vie 0,7 à 1 s, rotation libre, taille qui s'éteint sur les 30 derniers pour cent.
// Positions écrites dans le repère de l'objet (posé sans rotation ni échelle). Purement visuel et local.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OndeGemmes : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Gemmes de l'anneau par mètre de périmètre, et plafond.")]
    [SerializeField] private float densite = 14f;
    [SerializeField] private int capaciteAnneau = 420;
    [Tooltip("Taille des gemmes de l'anneau (m) au départ (anneau large) et à la fin (anneau fin).")]
    [SerializeField] private Vector2 tailleAnneau = new Vector2(0.13f, 0.06f);
    [SerializeField] private int eclatsGros = 26;
    [SerializeField] private int eclatsPetits = 16;
    [SerializeField] private Vector2 tailleEclatsGros = new Vector2(0.09f, 0.15f);
    [SerializeField] private Vector2 tailleEclatsPetits = new Vector2(0.06f, 0.1f);
    [SerializeField] private float gravite = 0.7f;

    private static Color TerreSombre => VfxPalette.Couleur(VfxTheme.Terre, VfxRole.Base, new Color(0.357f, 0.247f, 0.165f));  // #5b3f2a
    private static Color TerreClaire => VfxPalette.Couleur(VfxTheme.Terre, VfxRole.Vif, new Color(0.541f, 0.416f, 0.282f));  // #8a6a48

    // Paramètres posés par OndeDeChoc.
    [System.NonSerialized] public float rayonDepart = 0.5f;
    [System.NonSerialized] public float rayonMax = 4f;
    [System.NonSerialized] public float largeurDepart = 0.6f;
    [System.NonSerialized] public float largeurFin = 0.12f;
    [System.NonSerialized] public float duree = 0.8f;
    [System.NonSerialized] public float angleOuverture = 360f;

    private Mesh mesh;
    private MeshRenderer rendu;
    private Vector3[] vertices;
    private Color[] colors;
    // Anneau
    private float[] fraction, radial, hauteur, tailleFacteur, phase;
    private Color[] teinte;
    private Quaternion[] rot;
    private Vector3[] axe;
    // Éclats
    private Vector3[] eDepart, eVitesse;
    private float[] eVie, eRetard, eTaille;
    private Color[] eTeinte;
    private Quaternion[] eRot;
    private Vector3[] eAxe;
    private float[] eSpin;
    private float debut = -100f;
    private int total;

    public bool EnCours { get { return debut >= 0f; } }

    private void Awake()
    {
        rendu = GetComponent<MeshRenderer>();
        if (materiau != null) rendu.sharedMaterial = materiau;
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;
        rendu.enabled = false;
        int eclats = eclatsGros + eclatsPetits;
        total = capaciteAnneau + eclats;
        vertices = new Vector3[total * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "OndeGemmes" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(total);
        GetComponent<MeshFilter>().sharedMesh = mesh;
        fraction = new float[capaciteAnneau]; radial = new float[capaciteAnneau]; hauteur = new float[capaciteAnneau];
        tailleFacteur = new float[capaciteAnneau]; phase = new float[capaciteAnneau]; teinte = new Color[capaciteAnneau];
        rot = new Quaternion[capaciteAnneau]; axe = new Vector3[capaciteAnneau];
        System.Random random = new System.Random(2626);
        for (int i = 0; i < capaciteAnneau; i++)
        {
            // Suite dorée : les i premières gemmes couvrent l'arc uniformément, quel que soit i.
            fraction[i] = (float)((i * 0.6180339887) % 1.0);
            radial[i] = (float)random.NextDouble() - 0.5f;
            hauteur[i] = (float)random.NextDouble() * 0.06f;
            tailleFacteur[i] = 0.75f + 0.5f * (float)random.NextDouble();
            phase[i] = (float)random.NextDouble() * 10f;
            teinte[i] = random.NextDouble() < 0.35 ? TerreClaire : TerreSombre;
            rot[i] = Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f);
            axe[i] = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f).normalized;
        }
        eDepart = new Vector3[eclats]; eVitesse = new Vector3[eclats]; eVie = new float[eclats]; eRetard = new float[eclats]; eTaille = new float[eclats];
        eTeinte = new Color[eclats]; eRot = new Quaternion[eclats]; eAxe = new Vector3[eclats]; eSpin = new float[eclats];
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
        float ouverture = Mathf.Clamp(angleOuverture, 10f, 360f) * Mathf.Deg2Rad;
        float debutArc = Mathf.PI / 2f - ouverture / 2f;
        float vRadiale = rayonMax / Mathf.Max(0.1f, duree);
        int eclats = eclatsGros + eclatsPetits;
        for (int i = 0; i < eclats; i++)
        {
            bool gros = i < eclatsGros;
            float a = debutArc + Random.value * ouverture;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            eDepart[i] = dir * 0.5f + Vector3.up * 0.05f;
            eVitesse[i] = dir * vRadiale * Random.Range(0.55f, 0.95f) + Vector3.up * 3.8f;
            eVie[i] = Random.Range(0.7f, 1.0f);
            eRetard[i] = gros ? 0f : 0.03f;
            Vector2 t = gros ? tailleEclatsGros : tailleEclatsPetits;
            eTaille[i] = Random.Range(t.x, t.y);
            eTeinte[i] = gros ? TerreSombre : TerreClaire;
            eRot[i] = Random.rotation;
            eAxe[i] = Random.onUnitSphere;
            eSpin[i] = Random.Range(-230f, 230f);
        }
        debut = Time.time;
        rendu.enabled = true;
        Appliquer(0f);
    }

    public void Cacher()
    {
        debut = -100f;
        if (rendu != null) rendu.enabled = false;
    }

    private void Update()
    {
        if (debut < 0f) return;
        float t = Time.time - debut;
        if (t >= Mathf.Max(duree, 1.05f)) { Cacher(); return; }
        Appliquer(t);
    }

    // Pose l'onde `t` secondes après le départ (Update et captures).
    public void Appliquer(float t)
    {
        if (mesh == null) return;
        float ouverture = Mathf.Clamp(angleOuverture, 10f, 360f) * Mathf.Deg2Rad;
        float debutArc = Mathf.PI / 2f - ouverture / 2f;
        float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duree));
        float rExt = Mathf.Lerp(rayonDepart, rayonMax, p);
        float largeur = Mathf.Lerp(largeurDepart, largeurFin, p);
        float taille = Mathf.Lerp(tailleAnneau.x, tailleAnneau.y, p);
        float extinction = t < duree ? 1f : 0f;
        float perimetre = rExt * ouverture;
        int visibles = t < duree ? Mathf.Min(capaciteAnneau, Mathf.CeilToInt(perimetre * densite)) : 0;
        float lim = rExt + 1f;
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(lim * 2f, 3f, lim * 2f));
        for (int i = 0; i < capaciteAnneau; i++)
        {
            if (i >= visibles)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            float a = debutArc + fraction[i] * ouverture;
            float r = rExt - largeur * 0.5f + radial[i] * largeur;
            Vector3 position = new Vector3(Mathf.Cos(a) * r, hauteur[i] + taille * 0.5f, Mathf.Sin(a) * r);
            Quaternion spin = Quaternion.AngleAxis(t * 90f + phase[i] * 36f, axe[i]) * rot[i];
            LowPolyGem.Write(vertices, colors, i, position, taille * tailleFacteur[i] * extinction, new Vector3(0.8f, 1.2f, 0.8f), spin, teinte[i], LowPolyGem.DefaultLight);
        }
        int eclats = eclatsGros + eclatsPetits;
        for (int j = 0; j < eclats; j++)
        {
            int i = capaciteAnneau + j;
            float age = t - eRetard[j];
            float k = age / eVie[j];
            if (age < 0f || k >= 1f)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            // Coup de pied vertical de 3,8 m/s qui s'amortit, gravité, vitesse radiale constante.
            Vector3 v = eVitesse[j];
            float y = eDepart[j].y + v.y * age * Mathf.Exp(-1.2f * age) - 0.5f * gravite * 9.81f * age * age;
            Vector3 position = new Vector3(eDepart[j].x + v.x * age, Mathf.Max(0.04f, y), eDepart[j].z + v.z * age);
            float size = eTaille[j] * (k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            Quaternion spin = Quaternion.AngleAxis(age * eSpin[j], eAxe[j]) * eRot[j];
            LowPolyGem.Write(vertices, colors, i, position, size, new Vector3(1f, 0.8f, 1.15f), spin, eTeinte[j], LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.bounds = bounds;
    }
}
