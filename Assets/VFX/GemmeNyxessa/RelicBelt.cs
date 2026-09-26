using UnityEngine;

// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/RelicBelt.cs : sans FishNet ni GameAudio ; l'ouverture du
// portail (Portal.ToDungeon.IsOpen) est remplacée par le champ `portailOuvert` (l'impulsion part quand il passe à vrai).
// Le reste (tirages, orbites, taches, impulsion) est identique.
//
// Anneau de la relique (demande de Quentin, 23 septembre 2026, refait « déstructuré comme le portail ») : autour du
// cristal Nyxessa, une soupe de petites gemmes low poly en bande horizontale (jamais inclinée), chacune sur sa propre
// orbite, à son rayon et à sa hauteur, à sa vitesse (l'intérieur tourne plus vite), qui ondule et tourne sur elle-même ;
// les couleurs forment des taches qui dérivent le long de l'anneau, comme les taches du portail ; quelques éclats plus
// gros flottent dans la bande.
public class RelicBelt : MonoBehaviour
{
    [SerializeField] private Transform crystal;
    [SerializeField] private Material gemMaterial;
    [Tooltip("Rayon moyen de l'anneau autour du centre du cristal (m).")]
    [SerializeField] private float ringRadius = 1.5f;
    [Tooltip("Largeur de la bande (écart des rayons, m) et épaisseur (écart des hauteurs, m).")]
    [SerializeField] private float bandWidth = 0.45f;
    [SerializeField] private float bandThickness = 0.14f;
    [SerializeField] private int gems = 650;
    [Tooltip("Taille des petites gemmes (m), mini et maxi ; une gemme sur vingt est un éclat deux fois plus gros.")]
    [SerializeField] private Vector2 gemSize = new Vector2(0.022f, 0.05f);
    [Tooltip("Vitesse angulaire moyenne (radians par seconde).")]
    [SerializeField] private float spin = 0.35f;
    [Tooltip("Impulsion à l'ouverture du portail : élargissement (m) et durée (s).")]
    [SerializeField] private float pulseDistance = 1.1f;
    [SerializeField] private float pulseSeconds = 2.2f;
    [Tooltip("Bac à sable : remplace Portal.ToDungeon.IsOpen ; l'impulsion part quand il passe de faux à vrai.")]
    public bool portailOuvert = true;

    // Vert Nyxessa, du plus sombre au plus clair (même esprit que le portail).
    private static Color[] Palette => VfxPalette.Cache("RelicBelt.Nyxessa", () => new[]
    {
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Ombre, new Color(0.04f, 0.22f, 0.05f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.1f, 0.45f, 0.1f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.72f, 0.16f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.5f, 0.95f, 0.3f)),
        VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.74f, 1f, 0.52f)) * 1.15f,
    });

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private float[] angle0;
    private float[] radius0;
    private float[] height0;
    private float[] speed;
    private float[] phase;
    private float[] size;
    private Vector3[] stretch;
    private Quaternion[] rotation0;
    private Vector3[] spinAxis;
    private GameObject holder;
    private float pulseStart = -100f;
    private float pulseSigne = 1f;          // +1 : élargissement (Pulse) ; -1 : resserrement (Resserrer)
    private float parcoursStart = -100f;    // onde qui fait le tour de la ceinture (Parcourir)
    private float parcoursAngle;
    private float tempsRotation;            // temps de rotation accumulé (la vitesse peut varier)
    [Tooltip("Multiplicateur de la vitesse de rotation (réactions de la relique : Nyxessa).")]
    public float multiplicateurVitesse = 1f;
    private const float ParcoursSecondes = 1.2f;
    private bool wasOpen = true;

    private void Start()
    {
        if (crystal == null || gemMaterial == null)
        {
            enabled = false;
            return;
        }
        // Tirages fixes : même anneau chez tout le monde, sans rien synchroniser.
        System.Random random = new System.Random(1717);
        float R() => (float)random.NextDouble();
        // Loi en cloche (somme de trois tirages) : la bande est dense au milieu, effilochée sur les bords.
        float Bell() => (R() + R() + R()) / 1.5f - 1f;
        angle0 = new float[gems];
        radius0 = new float[gems];
        height0 = new float[gems];
        speed = new float[gems];
        phase = new float[gems];
        size = new float[gems];
        stretch = new Vector3[gems];
        rotation0 = new Quaternion[gems];
        spinAxis = new Vector3[gems];
        for (int i = 0; i < gems; i++)
        {
            angle0[i] = R() * Mathf.PI * 2f;
            radius0[i] = ringRadius + Bell() * bandWidth;
            height0[i] = Bell() * bandThickness;
            // Plus près du cristal, plus vite (comme un disque qui tourbillonne) ; un peu de désordre en plus.
            speed[i] = spin * Mathf.Pow(ringRadius / Mathf.Max(0.3f, radius0[i]), 1.5f) * (0.8f + 0.4f * R());
            phase[i] = R() * 10f;
            bool shard = R() < 0.05f;
            size[i] = Mathf.Lerp(gemSize.x, gemSize.y, R()) * (shard ? 2.2f : 1f);
            stretch[i] = shard ? new Vector3(0.7f, 1.7f, 0.7f) : new Vector3(0.6f + 0.5f * R(), 0.8f + 0.7f * R(), 0.6f + 0.5f * R());
            rotation0[i] = Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f);
            spinAxis[i] = new Vector3(R() - 0.5f, R() - 0.5f, R() - 0.5f).normalized;
        }

        vertices = new Vector3[gems * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "RelicBelt" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(gems);
        // Maillage en repère monde, sur un objet à part (non enfant : l'échelle et la rotation de Visual n'y touchent pas).
        holder = new GameObject("RelicBelt");
        holder.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = holder.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = gemMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (holder != null)
            Destroy(holder);
        if (mesh != null)
            Destroy(mesh);
    }

    // Déclenche l'impulsion (aussi appelée par l'outil de capture).
    public void Pulse()
    {
        pulseStart = Time.time;
        pulseSigne = 1f;
    }

    // Resserrement (fermeture du portail) : même onde que Pulse, vers l'intérieur et moins ample.
    public void Resserrer()
    {
        pulseStart = Time.time;
        pulseSigne = -1f;
    }

    // Petite onde qui fait le tour de la ceinture en 1,2 s (soulève et éclaire les gemmes au passage), avec un
    // scintillement (passage d'un joueur dans le portail).
    public void Parcourir()
    {
        parcoursStart = Time.time;
        parcoursAngle = Random.Range(0f, Mathf.PI * 2f);
    }

    private void LateUpdate()
    {
        if (mesh == null || crystal == null)
            return;
        // Le portail vers le donjon vient de s'ouvrir (l'aube) : impulsion. (Relic : Portal.ToDungeon.IsOpen + son RelicPulse.)
        bool open = portailOuvert;
        if (open && !wasOpen)
            Pulse();
        wasOpen = open;
        tempsRotation += Time.deltaTime * multiplicateurVitesse;
        Draw(Time.time, Time.time - pulseStart);
    }

    // Aperçu en édition (outil de capture) : construit l'anneau et le pose à l'instant `time`, l'impulsion ayant eu lieu
    // `pulseAge` s plus tôt (grand : pas d'impulsion). Retourne l'objet créé, à détruire après la capture.
    public GameObject Preview(float time, float pulseAge = 100f)
    {
        if (holder != null)
            DestroyImmediate(holder);
        mesh = null;
        Start();
        tempsRotation = time;
        if (mesh != null)
            Draw(time, pulseAge);
        return holder;
    }

    private void Draw(float t, float pulseAge)
    {
        Vector3 center = crystal.position;
        float innerEdge = ringRadius - bandWidth;
        // Luminance de nuit (26/09/2026) : la ceinture rayonne avec le cristal, HDR au-delà de 1 la nuit (Bloom
        // LueurNuit), inchangée le jour (comme RelicGem.Shade).
        float hdr = Mathf.Lerp(1f, VfxPalette.Intensite(VfxTheme.Nyxessa, 2.5f), DayCycle.Night);
        for (int i = 0; i < gems; i++)
        {
            float a = angle0[i] + tempsRotation * speed[i];
            float p = phase[i];
            float r = radius0[i] + Mathf.Sin(t * 0.9f + p) * 0.04f;
            float h = height0[i] + Mathf.Sin(t * 1.3f + p * 1.7f) * 0.035f;
            float bright = 0f;

            if (pulseAge >= 0f && pulseAge < pulseSeconds * 1.5f)
            {
                // Onde qui part de l'intérieur : chaque gemme la reçoit avec un retard selon son rayon, s'écarte d'un
                // coup puis revient en oscillant (ressort amorti) ; la bande s'épaissit et s'éclaire au passage.
                float delay = Mathf.Max(0f, radius0[i] - innerEdge) * 0.25f;
                float local = Mathf.Max(0f, pulseAge - delay) / pulseSeconds;
                if (pulseAge > delay)
                {
                    float wave = Mathf.Exp(-4f * local) * Mathf.Sin(local * Mathf.PI * 3f);
                    r += pulseDistance * pulseSigne * (pulseSigne < 0f ? 0.55f : 1f) * Mathf.Max(wave, -0.3f);
                    h *= 1f + 2.5f * Mathf.Abs(wave);
                    bright = Mathf.Exp(-3f * local);
                }
            }

            // Onde qui fait le tour de l'anneau (Parcourir) et scintillement pendant son passage.
            float parcoursAge = t - parcoursStart;
            if (parcoursAge >= 0f && parcoursAge < ParcoursSecondes)
            {
                float fin = 1f - parcoursAge / ParcoursSecondes;
                float front = parcoursAngle + parcoursAge / ParcoursSecondes * Mathf.PI * 2f;
                float ecart = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, front * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float bosse = Mathf.Exp(-(ecart / 0.35f) * (ecart / 0.35f)) * fin;
                h += 0.12f * bosse;
                bright += bosse;
                if (Mathf.Repeat(Mathf.Sin(i * 12.9898f + Mathf.Floor(t * 14f) * 78.233f) * 43758.55f, 1f) > 0.93f)
                    bright += 0.7f * fin;
            }

            // Taches de couleur qui dérivent le long de l'anneau (même idée que le masque du portail).
            float blot = Mathf.Sin(a * 3f - t * 0.5f) * 0.5f + Mathf.Sin(a * 7f + r * 5f + t * 0.8f) * 0.3f + Mathf.Sin(p * 3f) * 0.25f;
            float shade = Mathf.Clamp01(0.5f + 0.5f * blot + bright * 0.6f);
            float slot = shade * (Palette.Length - 1);
            int k = Mathf.Min(Palette.Length - 2, (int)slot);
            Color color = Color.Lerp(Palette[k], Palette[k + 1], slot - k) * hdr;

            Vector3 position = center + new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
            Quaternion rotation = Quaternion.AngleAxis(t * (90f + 60f * Mathf.Sin(p)) + p * 36f, spinAxis[i]) * rotation0[i];
            LowPolyGem.Write(vertices, colors, i, position, size[i], stretch[i], rotation, color, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        float extent = (ringRadius + bandWidth * 2f + pulseDistance) * 2f;
        mesh.bounds = new Bounds(center, new Vector3(extent, 1.5f, extent));
    }
}
