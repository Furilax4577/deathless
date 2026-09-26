using UnityEngine;

// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/RelicGem.cs : la lecture de RunProgress (nuit pendant la
// vague) est remplacée par DayCycle.Night (copie locale pilotée à la main). Le reste est identique.
//
// Cristal de la relique en gemme low poly (demande de Quentin, 23 septembre 2026 : l'homogénéiser avec les autres
// cristaux) : au lieu de l'octaèdre lisse et émissif, une gemme taillée irrégulière (couronne, ceinture et pointe, 7 pans
// décalés), chaque facette de sa propre teinte de vert Nyxessa et ombrée selon son orientation du moment, comme les
// gemmes du portail et de l'anneau (même shader à couleurs par sommet, matériau PortalVoxel). Un peu plus claire la
// nuit. Remplace le maillage et le matériau du cristal au démarrage ; CrystalSpin continue de le faire tourner et
// flotter. Purement visuel et local. A poser sur l'objet Crystal de la relique.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RelicGem : MonoBehaviour
{
    [SerializeField] private Material gemMaterial;
    [Tooltip("Nombre de pans autour de la gemme.")]
    [SerializeField] private int sides = 7;
    [Tooltip("Eclaircissement la nuit (pendant la vague), en plus de la lumière du cristal.")]
    [SerializeField] private float nightBoost = 0.15f;

    private static Color[] Palette => VfxPalette.Cache("RelicGem.Nyxessa", () => new[]
    {
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.1f, 0.45f, 0.1f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.2f, 0.62f, 0.14f)),
        Color.Lerp(VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.2f, 0.62f, 0.14f)), VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.5f, 0.95f, 0.32f)), 0.5f),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.5f, 0.95f, 0.32f)),
    });

    private Mesh mesh;
    private Vector3[] faceNormals;
    private Color[] faceColors;
    private Color[] colors;
    private float night;

    private void Awake()
    {
        if (gemMaterial == null)
            return;
        Build();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = gemMaterial;
        // L'ancienne lueur émissive n'a plus de sens sur ce shader sans lumière.
        RelicGlow glow = GetComponent<RelicGlow>();
        if (glow != null)
            glow.enabled = false;
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    // Gemme dans le même volume que l'ancien cristal (rayon 0,5, hauteur -0,5 à 0,5 avant l'échelle de l'objet).
    private void Build()
    {
        System.Random random = new System.Random(4242);
        float R() => (float)random.NextDouble();
        int n = Mathf.Max(4, sides);
        Vector3[] girdle = new Vector3[n];   // ceinture, la plus large
        Vector3[] crown = new Vector3[n];    // couronne, au-dessus
        for (int i = 0; i < n; i++)
        {
            float a = (i + (R() - 0.5f) * 0.35f) / n * Mathf.PI * 2f;
            float r = 0.5f * (0.82f + 0.18f * R());
            girdle[i] = new Vector3(Mathf.Cos(a) * r, 0.02f + (R() - 0.5f) * 0.06f, Mathf.Sin(a) * r);
            float c = a + Mathf.PI / n;   // décalée d'un demi-pan : facettes en losange
            float rc = r * (0.55f + 0.1f * R());
            crown[i] = new Vector3(Mathf.Cos(c) * rc, 0.3f + (R() - 0.5f) * 0.05f, Mathf.Sin(c) * rc);
        }
        Vector3 top = new Vector3((R() - 0.5f) * 0.05f, 0.5f, (R() - 0.5f) * 0.05f);
        Vector3 bottom = new Vector3((R() - 0.5f) * 0.05f, -0.5f, (R() - 0.5f) * 0.05f);

        System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>();
        void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            // Sens horaire vu de l'extérieur (face avant de Unity) : on retourne le triangle s'il regarde vers le centre.
            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 middle = (a + b + c) / 3f;
            if (Vector3.Dot(normal, new Vector3(middle.x, middle.y * 0.3f, middle.z)) < 0f) { v.Add(a); v.Add(c); v.Add(b); }
            else { v.Add(a); v.Add(b); v.Add(c); }
        }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Tri(crown[i], crown[j], top);                 // table en pointe
            Tri(girdle[i], girdle[j], crown[i]);          // losanges de la couronne
            Tri(crown[i], girdle[j], crown[j]);
            Tri(girdle[i], bottom, girdle[j]);            // pavillon
        }

        Vector3[] vertices = v.ToArray();
        int faces = vertices.Length / 3;
        faceNormals = new Vector3[faces];
        faceColors = new Color[faces];
        for (int f = 0; f < faces; f++)
        {
            faceNormals[f] = Vector3.Cross(vertices[f * 3 + 1] - vertices[f * 3], vertices[f * 3 + 2] - vertices[f * 3]).normalized;
            faceColors[f] = Palette[random.Next(Palette.Length)];
        }
        int[] triangles = new int[vertices.Length];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = i;
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "RelicGem" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Shade();
    }

    private void Update()
    {
        if (mesh == null)
            return;
        // Bac à sable : « vague » = nuit du DayCycle local (dans Relic : RunProgress.IsStarted && !IsResting).
        bool wave = DayCycle.Night >= 0.5f;
        night = Mathf.MoveTowards(night, wave ? 1f : 0f, Time.deltaTime / 2.5f);
        Shade();
    }

    // Ombrage peint : chaque facette selon son orientation actuelle dans le monde (la gemme tourne), comme LowPolyGem.
    private void Shade()
    {
        Vector3 light = LowPolyGem.DefaultLight;
        float boost = 1f + nightBoost * night;
        // Luminance de nuit (26/09/2026) : Nyxessa doit être la source la plus brillante du village. Émission HDR
        // (au-delà de 1, le Bloom LueurNuit la fait rayonner) qui monte avec la nuit (VfxPalette.intensiteEmission du
        // thème Nyxessa, la plus forte) ; le jour reste inchangé (×1) pour ne pas blanchir le cristal.
        float hdr = Mathf.Lerp(1f, VfxPalette.Intensite(VfxTheme.Nyxessa, 2.5f), night);
        for (int f = 0; f < faceNormals.Length; f++)
        {
            Vector3 world = transform.TransformDirection(faceNormals[f]).normalized;
            // Vert plus profond (Quentin, 26/09/2026) : facettes sombres plus sombres, la plus claire garde sa lecture.
            float shade = 0.36f + 0.66f * Mathf.Abs(Vector3.Dot(world, light));
            Color c = faceColors[f] * shade * boost * hdr;
            c.a = 1f;
            colors[f * 3] = colors[f * 3 + 1] = colors[f * 3 + 2] = c;
        }
        mesh.colors = colors;
    }
}
