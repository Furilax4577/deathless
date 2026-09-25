using System.Collections.Generic;
using UnityEngine;

// Tête du rugissement du viking dans le langage gemmes de Relic : dérivé « Visual only » de SkullMissileVisual (crâne
// KayKit refait en soupe de gemmes LowPolyGem depuis la forme cuite SkullGemShape, gemmes couchées à plat sur la
// surface, frémissement, scintillement), en rouge et un peu plus gros que le missile, puis « barbarisé » (25/09/2026) :
// - visage marqué : orbites creusées et assombries (rouge sombre puis presque noir au fond), arcades sourcilières
//   saillantes et froncées vers le centre, arête du nez et pommettes en relief, dents ivoire en haut et en bas ;
// - barbe courte en gemmes sombres sous la mâchoire ;
// - casque viking : calotte de gemmes gris fer sur le haut du crâne avec un nasal, deux cornes courbes ivoire qui
//   partent des tempes vers le haut et l'avant (4 segments qui s'affinent).
// La mâchoire inférieure (gemmes sous le plan `planMachoire` de la forme, dents du bas et barbe) est un maillage à part,
// dans un enfant à pivot arrière : `Ouverture` (0..1) la fait basculer de `angleMachoire` degrés. Le casque suit le crâne.
// Shader Relic/VertexColorUnlit (matériau dérivé de PortalVoxel). Purement visuel et local.
public class RugissementCrane : MonoBehaviour
{
    [SerializeField] private GemShape forme;
    [SerializeField] private Material materiau;
    [Tooltip("Taille du crâne (m) ; le missile fait 0,55.")]
    [SerializeField] private float taille = 0.8f;
    [Tooltip("Rotation qui tourne le visage vers +Z (comme le missile : aucune).")]
    [SerializeField] private Vector3 euler;
    [Tooltip("Hauteur (dans la forme normalisée, -0,5 à 0,5) sous laquelle les gemmes appartiennent à la mâchoire.")]
    [SerializeField] private float planMachoire = -0.22f;
    [SerializeField] private float angleMachoire = 28f;
    [Tooltip("Léger recul de la mandibule à l'ouverture (m).")]
    [SerializeField] private float reculMachoire = 0.02f;
    [Tooltip("Tête rejetée en arrière pendant le cri (degrés), pilotée par Bascule.")]
    [SerializeField] private float angleBascule = 13f;

    // Rouges du crâne (creux → os), fond d'orbite presque noir, ivoire (dents, cornes), gris fer (casque).
    private static readonly Color Dark = new Color(0.25f, 0.05f, 0.04f);
    private static readonly Color Mid = new Color(0.43f, 0.08f, 0.06f);      // #6e1410
    private static readonly Color Pale = new Color(0.7f, 0.15f, 0.12f);      // #b3261e
    private static readonly Color Orbite = new Color(0.227f, 0.039f, 0.031f);   // #3a0a08
    private static readonly Color Ivoire = new Color(0.91f, 0.863f, 0.753f);    // #e8dcc0
    private static readonly Color Fer = new Color(0.353f, 0.373f, 0.4f);        // #5a5f66
    private static readonly Color FerSombre = new Color(0.247f, 0.267f, 0.29f); // #3f444a
    private static readonly Color FerClair = new Color(0.478f, 0.502f, 0.533f);  // #7a8088 (arêtes du heaume)
    private static readonly Color IvoireClair = new Color(1f, 0.96f, 0.88f);      // pointe des cornes

    private const float GemSize = 0.028f;

    private class Partie
    {
        public Transform racine;
        public Mesh mesh;
        public Vector3[] vertices;
        public Color[] colors;
        public readonly List<Vector3> points = new List<Vector3>();
        public readonly List<Color> tints = new List<Color>();
        public readonly List<Quaternion> rotations = new List<Quaternion>();
        public readonly List<Vector3> spinAxes = new List<Vector3>();
        public readonly List<float> phases = new List<float>();
        public readonly List<float> sizes = new List<float>();
        public readonly List<Vector3> stretches = new List<Vector3>();
        public Vector3 pivot;

        // Ajoute une gemme (coordonnées du crâne, converties dans le repère de la partie).
        public void Ajouter(Vector3 p, Vector3 normal, Color c, float size, Vector3 stretch)
        {
            points.Add(p - pivot);
            tints.Add(c);
            rotations.Add(normal.sqrMagnitude > 0.5f
                ? Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                : Random.rotation);
            spinAxes.Add(Random.onUnitSphere);
            phases.Add(Random.value * 10f);
            sizes.Add(size);
            stretches.Add(stretch);
        }
    }

    private static readonly Vector3 Ecaille = new Vector3(1.25f, 1.25f, 0.45f);
    private static readonly Vector3 Dent = new Vector3(0.7f, 1.5f, 0.7f);
    private static readonly Vector3 Ronde = new Vector3(1f, 1f, 1f);

    private Partie crane;
    private Partie machoire;
    private float seed;
    private float ouverture;
    private float bascule;

    // 0 tête droite, 1 rejetée en arrière de `angleBascule` (le cri).
    public float Bascule
    {
        get { return bascule; }
        set { bascule = Mathf.Clamp01(value); }
    }

    // 0 fermée, 1 ouverte de `angleMachoire`.
    public float Ouverture
    {
        get { return ouverture; }
        set { ouverture = Mathf.Clamp01(value); }
    }

    private void Awake()
    {
        if (forme == null || materiau == null || forme.points.Length == 0)
            return;
        seed = Random.value * 100f;
        Build();
    }

    private void OnDestroy()
    {
        if (crane != null && crane.mesh != null) Destroy(crane.mesh);
        if (machoire != null && machoire.mesh != null) Destroy(machoire.mesh);
    }

    private void Build()
    {
        Quaternion turn = Quaternion.Euler(euler);
        int count = forme.points.Length;
        float low = 1f, high = 0f;
        foreach (float value in forme.brightness) { low = Mathf.Min(low, value); high = Mathf.Max(high, value); }
        Vector3[] pts = new Vector3[count];
        Vector3[] nrm = new Vector3[count];
        float[] k = new float[count];
        float zAvant = -1f, zArriere = 1f;
        for (int i = 0; i < count; i++)
        {
            pts[i] = turn * forme.points[i];
            nrm[i] = i < forme.normals.Length ? turn * forme.normals[i] : Vector3.zero;
            float b = i < forme.brightness.Length ? forme.brightness[i] : high;
            float t = high > low + 0.01f ? Mathf.Clamp01((b - low) / (high - low)) : 0.8f;
            k[i] = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.4f));
            zAvant = Mathf.Max(zAvant, pts[i].z);
            zArriere = Mathf.Min(zArriere, pts[i].z);
        }

        // Orbites : centre des points les plus sombres de la moitié haute du visage, de chaque côté.
        Vector3 oeilG = Vector3.zero, oeilD = Vector3.zero;
        float poidsG = 0f, poidsD = 0f;
        for (int i = 0; i < count; i++)
        {
            if (pts[i].z < 0.1f || pts[i].y < -0.05f || pts[i].y > 0.35f) continue;
            float w = Mathf.Pow(1f - k[i], 3f);
            if (pts[i].x < 0f) { oeilG += pts[i] * w; poidsG += w; } else { oeilD += pts[i] * w; poidsD += w; }
        }
        oeilG = poidsG > 0f ? oeilG / poidsG : new Vector3(-0.15f, 0.1f, 0.35f);
        oeilD = poidsD > 0f ? oeilD / poidsD : new Vector3(0.15f, 0.1f, 0.35f);
        float yOeil = (oeilG.y + oeilD.y) * 0.5f;
        const float rayonOrbite = 0.12f;

        // Mandibule seule : sous un plan de coupe incliné, à la ligne des dents devant (planMachoire) et qui remonte vers
        // l'arrière jusqu'aux condyles (à hauteur des oreilles, sous les orbites) ; pivot aux condyles.
        float yCondyle = yOeil - 0.12f;
        float zCondyle = zArriere + 0.12f;
        System.Func<Vector3, bool> mandibule = (q) => {
            float u = Mathf.Clamp01((zAvant - 0.05f - q.z) / Mathf.Max(0.01f, zAvant - 0.05f - zCondyle));
            float coupe = Mathf.Lerp(planMachoire, yCondyle, u);
            return q.y < coupe && q.z > zCondyle - 0.03f;
        };
        crane = new Partie { pivot = Vector3.zero };
        machoire = new Partie { pivot = new Vector3(0f, yCondyle, zCondyle) * taille };

        // Gemmes de la forme cuite : rouge sombre dans les creux, rouge vif sur l'os ; orbites creusées et assombries ;
        // le haut du crâne (hors visage) reçoit en plus une écaille de fer par-dessus (calotte du casque).
        for (int i = 0; i < count; i++)
        {
            Vector3 p = pts[i];
            Color c = k[i] < 0.5f ? Color.Lerp(Dark, Mid, k[i] * 2f) : Color.Lerp(Mid, Pale, (k[i] - 0.5f) * 2f);
            float creux = Mathf.Max(Mathf.Clamp01(1f - Vector3.Distance(p, oeilG) / rayonOrbite), Mathf.Clamp01(1f - Vector3.Distance(p, oeilD) / rayonOrbite));
            if (creux > 0f)
            {
                c = Color.Lerp(c, Orbite, Mathf.SmoothStep(0f, 1f, creux));
                p -= (nrm[i].sqrMagnitude > 0.5f ? nrm[i] : Vector3.forward) * 0.04f * creux;   // enfoncées
            }
            bool bas = mandibule(pts[i]);
            Partie partie = bas ? machoire : crane;
            partie.Ajouter(p * taille, nrm[i], c, 1f, Ecaille);
            // Heaume : calotte jusqu'au-dessus des orbites devant, jusqu'à la nuque derrière, couvre-joues courts sur les
            // côtés ; gemmes plus grosses, décollées de 0,055 m, arêtes (bord bas) en fer clair.
            bool visage = pts[i].z > 0.2f && pts[i].y < yOeil + 0.17f;
            bool calotte = pts[i].y > yOeil + 0.08f || (pts[i].z < 0.05f && pts[i].y > yOeil - 0.15f);
            bool joue = Mathf.Abs(pts[i].x) > 0.24f && pts[i].y > yOeil - 0.28f && pts[i].y <= yOeil + 0.08f && pts[i].z > -0.05f && pts[i].z < 0.18f;
            if (!bas && !visage && (calotte || joue) && nrm[i].sqrMagnitude > 0.5f)
            {
                bool arete = pts[i].y < yOeil + 0.13f || joue && pts[i].y < yOeil - 0.2f;
                Color fer = arete ? FerClair : Random.value < 0.35f ? FerSombre : Fer;
                crane.Ajouter((p + nrm[i] * 0.07f) * taille, nrm[i], fer, 1.9f, Ecaille);
            }
            // Barbe courte : sous la mâchoire et sur son avant, gemmes sombres décollées.
            if (bas && nrm[i].sqrMagnitude > 0.5f && (nrm[i].y < -0.3f || pts[i].z > zAvant - 0.12f) && Random.value < 0.7f)
                machoire.Ajouter((p + nrm[i] * 0.035f) * taille, nrm[i], Random.value < 0.5f ? Orbite : Dark, 1.15f, Ronde);
        }

        // Arcades sourcilières froncées : deux bourrelets au-dessus des orbites, plus bas côté nez.
        foreach (int cote in new[] { -1, 1 })
        {
            Vector3 oeil = cote < 0 ? oeilG : oeilD;
            for (int j = 0; j < 10; j++)
            {
                float u = j / 9f;                     // 0 = extérieur, 1 = intérieur (près du nez)
                float x = oeil.x + cote * Mathf.Lerp(0.16f, -0.04f, u);
                float y = oeil.y + Mathf.Lerp(0.16f, 0.09f, u);
                Vector3 p = new Vector3(x, y, oeil.z + 0.05f);
                Vector3 n = new Vector3(cote * 0.3f, 0.4f, 1f).normalized;
                crane.Ajouter(p * taille, n, Dark, 1.7f, Ronde);
                crane.Ajouter((p + new Vector3(0f, 0.035f, -0.01f)) * taille, n, Mid, 1.3f, Ronde);
            }
        }
        // Arête du nez et pommettes en relief.
        for (int j = 0; j < 7; j++)
        {
            float u = j / 6f;
            Vector3 p = new Vector3(0f, Mathf.Lerp(yOeil + 0.02f, yOeil - 0.2f, u), zAvant + 0.03f + 0.04f * u);
            crane.Ajouter(p * taille, Vector3.forward, Pale, 1.5f - 0.3f * u, Ronde);
        }
        foreach (int cote in new[] { -1, 1 })
            for (int j = 0; j < 4; j++)
                crane.Ajouter(new Vector3(cote * (0.2f + 0.03f * j), yOeil - 0.14f - 0.03f * j, zAvant - 0.06f - 0.03f * j) * taille,
                    new Vector3(cote * 0.6f, 0f, 0.8f).normalized, Pale, 1.6f, Ronde);

        // Dents : 7 en haut (crâne) et 7 en bas (mâchoire), le long de la bouche, ivoire.
        float zBouche = zAvant - 0.03f;
        for (int j = 0; j < 7; j++)
        {
            float x = Mathf.Lerp(-0.15f, 0.15f, j / 6f);
            float zz = zBouche - 0.05f * Mathf.Abs(x / 0.15f);   // rangée légèrement arrondie
            crane.Ajouter(new Vector3(x, planMachoire + 0.03f, zz) * taille, Vector3.forward, Ivoire, 1.35f, Dent);
            machoire.Ajouter(new Vector3(x, planMachoire - 0.03f, zz) * taille, Vector3.forward, Ivoire, 1.25f, Dent);
        }

        // Casque : nasal (bande de fer sur l'arête du nez) et cornes courbes ivoire depuis les tempes.
        for (int j = 0; j < 8; j++)
        {
            float u = j / 7f;
            Vector3 p = new Vector3(0f, Mathf.Lerp(yOeil + 0.16f, yOeil - 0.16f, u), zAvant + 0.06f + 0.04f * u);
            crane.Ajouter(p * taille, Vector3.forward, j % 2 == 0 ? FerClair : Fer, 1.7f, Ronde);
            crane.Ajouter((p + new Vector3(0.045f, 0f, -0.01f)) * taille, Vector3.forward, Fer, 1.3f, Ronde);
            crane.Ajouter((p + new Vector3(-0.045f, 0f, -0.01f)) * taille, Vector3.forward, Fer, 1.3f, Ronde);
        }
        foreach (int cote in new[] { -1, 1 })
        {
            // Départ au-dessus des oreilles (latéral), vers l'extérieur et le bas, puis remontée en C ouvert vers le
            // haut et légèrement l'avant (Bézier cubique, ≈ 0,75 m pour un crâne de 0,8 m).
            Vector3 b0 = new Vector3(cote * 0.42f, yOeil + 0.05f, -0.05f);
            Vector3 b1 = b0 + new Vector3(cote * 0.38f, -0.22f, 0.0f);
            Vector3 b2 = b0 + new Vector3(cote * 0.62f, 0.3f, 0.15f);
            Vector3 b3 = b0 + new Vector3(cote * 0.5f, 0.8f, 0.38f);
            // Gemmes semées le long de la courbe (8 anneaux de 8 gemmes, section qui s'affine, écailles couchées) :
            // assez serrées pour lire une corne pleine.
            const int segments = 14;
            for (int s = 0; s <= segments; s++)
            {
                float u = s / (float)segments;
                float w = 1f - u;
                Vector3 c = w * w * w * b0 + 3f * w * w * u * b1 + 3f * w * u * u * b2 + u * u * u * b3;
                Vector3 tangente = (3f * w * w * (b1 - b0) + 6f * w * u * (b2 - b1) + 3f * u * u * (b3 - b2)).normalized;
                Vector3 a1 = Vector3.Cross(tangente, Vector3.forward).normalized;
                if (a1.sqrMagnitude < 0.5f) a1 = Vector3.Cross(tangente, Vector3.up).normalized;
                Vector3 a2 = Vector3.Cross(tangente, a1);
                float rayon = Mathf.Lerp(0.1f, 0.019f, u);
                int autour = s == segments ? 1 : 8;
                for (int q = 0; q < autour; q++)
                {
                    float ang = (q + 0.5f * (s % 2)) * Mathf.PI * 2f / autour;
                    Vector3 n = (a1 * Mathf.Cos(ang) + a2 * Mathf.Sin(ang)).normalized;
                    Vector3 p = s == segments ? c + tangente * 0.03f : c + n * rayon;
                    Color iv = Color.Lerp(q % 4 == 0 ? Ivoire * 0.85f : Ivoire, IvoireClair, Mathf.Clamp01((u - 0.6f) / 0.4f));
                    crane.Ajouter(p * taille, s == segments ? tangente : n, iv, Mathf.Lerp(2.4f, 1.2f, u), Ecaille);
                }
            }
        }

        Materialiser(crane, "Crane");
        Materialiser(machoire, "Machoire");

        Light glow = new GameObject("Light").AddComponent<Light>();
        glow.transform.SetParent(transform, false);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.3f, 0.25f);
        glow.intensity = 2.5f;
        glow.range = 5f;
        glow.shadows = LightShadows.None;
        Refresh(0f);
    }

    private void Materialiser(Partie p, string nom)
    {
        p.racine = new GameObject(nom).transform;
        p.racine.SetParent(transform, false);
        p.racine.localPosition = p.pivot;
        int n = Mathf.Max(1, p.points.Count);
        p.vertices = new Vector3[n * LowPolyGem.VerticesPerGem];
        p.colors = new Color[p.vertices.Length];
        p.mesh = new Mesh { name = "Rugissement" + nom };
        p.mesh.MarkDynamic();
        p.mesh.vertices = p.vertices;
        p.mesh.colors = p.colors;
        p.mesh.triangles = LowPolyGem.Triangles(n);
        p.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * taille * 2.4f);
        p.racine.gameObject.AddComponent<MeshFilter>().sharedMesh = p.mesh;
        MeshRenderer r = p.racine.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterial = materiau;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    private void Update()
    {
        if (crane == null)
            return;
        Refresh(Time.time);
    }

    // Aperçu : pose le crâne à l'instant `time`.
    public void Refresh(float time)
    {
        if (crane == null)
            return;
        // Léger lacet et roulis, comme le missile.
        // Tête rejetée en arrière pendant le cri (l'objet entier bascule), plus le léger lacet et roulis du missile.
        transform.localRotation = Quaternion.Euler(-angleBascule * bascule, 0f, 0f);
        crane.racine.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 5f + seed) * 6f, Mathf.Sin(time * 3.3f + seed) * 5f);
        machoire.racine.localRotation = Quaternion.Euler(ouverture * angleMachoire, 0f, 0f);
        machoire.racine.localPosition = machoire.pivot + new Vector3(0f, 0f, -reculMachoire * ouverture);
        Dessiner(crane, time);
        Dessiner(machoire, time);
    }

    private static void Dessiner(Partie p, float time)
    {
        for (int i = 0; i < p.points.Count; i++)
        {
            float ph = p.phases[i];
            Vector3 jitter = new Vector3(Mathf.Sin(time * 9f + ph), Mathf.Sin(time * 11f + ph * 1.7f), Mathf.Sin(time * 7f + ph * 2.3f)) * 0.006f;
            Quaternion spin = Quaternion.AngleAxis(Mathf.Sin(time * 8f + ph * 5f) * 12f, p.spinAxes[i]) * p.rotations[i];
            float flicker = 0.85f + 0.25f * Mathf.Sin(time * 6f + ph * 3f);
            LowPolyGem.Write(p.vertices, p.colors, i, p.points[i] + jitter, GemSize * p.sizes[i], p.stretches[i], spin, p.tints[i] * flicker, LowPolyGem.DefaultLight);
        }
        p.mesh.vertices = p.vertices;
        p.mesh.colors = p.colors;
    }
}
