using UnityEngine;

// Paillettes du soin dans le langage gemmes de Relic (même technique que GemBurst.Rise, la montée de la désintégration,
// réécrite avec teinte, densité et rythme paramétrés) : un seul maillage en repère monde, une petite gemme low poly
// irrégulière par paillette (LowPolyGem : octaèdre à 8 facettes, ombrage peint dans les couleurs par sommet, shader
// Relic/VertexColorUnlit), qui apparaît sur un disque au sol, monte en dérivant un peu et en tournant lentement, puis
// rétrécit jusqu'à disparaître. Apparition étalée sur `etalement` s. Maillage en repère local (l'objet est posé au sol,
// sans rotation ni échelle). Purement visuel et local.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AuraGemmes : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Nombre de gemmes par déclenchement (mini, maxi).")]
    [SerializeField] private Vector2Int nombre = new Vector2Int(20, 40);
    [Tooltip("Taille d'une gemme (distance centre-pointe, m), mini et maxi.")]
    [SerializeField] private Vector2 taille = new Vector2(0.05f, 0.09f);
    [Tooltip("Rayon du disque d'apparition au sol (m).")]
    [SerializeField] private float rayon = 1.0f;
    [Tooltip("Vitesse de montée (m/s), mini et maxi : un peu plus lente que les croix.")]
    [SerializeField] private Vector2 montee = new Vector2(0.9f, 1.4f);
    [Tooltip("Dérive latérale (m/s).")]
    [SerializeField] private float derive = 0.3f;
    [SerializeField] private float duree = 1.0f;
    [Tooltip("Les apparitions s'étalent sur ce temps (s).")]
    [SerializeField] private float etalement = 0.3f;

    // Lumière sacrée du soin (Quentin, 26/09/2026) : or (deux tiers) et or sombre (un tiers), palette Soin blanc chaud et
    // or (le blanc chaud vient de la lumière de l'effet) ; les croix qui montent restent vertes (palette SoinCroix).
    private static Color Menthe => VfxPalette.Couleur(VfxTheme.Soin, VfxRole.Base, new Color(0.91f, 0.784f, 0.447f));      // #e8c872
    private static Color MentheSombre => VfxPalette.Couleur(VfxTheme.Soin, VfxRole.Ombre, new Color(0.722f, 0.565f, 0.227f)); // #b8903a

    private Mesh mesh;
    private MeshRenderer rendu;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] depart;
    private Vector3[] vitesse;
    private Vector3[] etirement;
    private Quaternion[] rotation;
    private Vector3[] axe;
    private float[] vitesseRotation;
    private float[] retard;
    private float[] tailles;
    private Color[] teintes;
    private int actifs;
    private float debut = -100f;

    private void Awake()
    {
        rendu = GetComponent<MeshRenderer>();
        if (materiau != null)
            rendu.sharedMaterial = materiau;
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;
        rendu.enabled = false;
        int max = Mathf.Max(1, nombre.y);
        vertices = new Vector3[max * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "AuraGemmes" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(max);
        GetComponent<MeshFilter>().sharedMesh = mesh;
        depart = new Vector3[max]; vitesse = new Vector3[max]; etirement = new Vector3[max]; rotation = new Quaternion[max];
        axe = new Vector3[max]; vitesseRotation = new float[max]; retard = new float[max]; tailles = new float[max]; teintes = new Color[max];
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
        actifs = Random.Range(nombre.x, nombre.y + 1);
        for (int i = 0; i < actifs; i++)
        {
            Vector2 disque = Random.insideUnitCircle.normalized * Mathf.Sqrt(Random.value) * rayon;
            depart[i] = new Vector3(disque.x, 0.03f, disque.y);   // repère local de l'objet (le maillage est dessiné par son transform)
            Vector2 d = Random.insideUnitCircle * derive;
            vitesse[i] = new Vector3(d.x, Random.Range(montee.x, montee.y), d.y);
            etirement[i] = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.8f, 1.5f), Random.Range(0.6f, 1.2f));
            rotation[i] = Random.rotation;
            axe[i] = Random.onUnitSphere;
            vitesseRotation[i] = Random.Range(50f, 110f);
            retard[i] = Random.value * etalement;
            tailles[i] = Random.Range(taille.x, taille.y);
            Color c = Random.value < 0.33f ? MentheSombre : Menthe;
            teintes[i] = c * Random.Range(0.9f, 1.1f);
            teintes[i].a = 1f;
        }
        debut = Time.time;
        rendu.enabled = true;
        Dessiner(0f);
    }

    private void Update()
    {
        if (debut < 0f || mesh == null)
            return;
        float t = Time.time - debut;
        if (t > duree + etalement)
        {
            rendu.enabled = false;
            debut = -100f;
            return;
        }
        Dessiner(t);
    }

    private void Dessiner(float t)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        for (int i = 0; i < vertices.Length / LowPolyGem.VerticesPerGem; i++)
        {
            float age = i < actifs ? t - retard[i] : -1f;
            float k = age / duree;
            if (age < 0f || k >= 1f)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            // Même courbe que les croix : éclosion rapide, pleine taille, puis extinction.
            float grow = k < 0.2f ? Mathf.Lerp(0.5f, 1f, k / 0.2f) : k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            Vector3 position = depart[i] + vitesse[i] * age;
            Quaternion spin = Quaternion.AngleAxis(age * vitesseRotation[i], axe[i]) * rotation[i];
            LowPolyGem.Write(vertices, colors, i, position, tailles[i] * grow, etirement[i], spin, teintes[i], LowPolyGem.DefaultLight);
            bounds.Encapsulate(position);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        bounds.Expand(0.3f);
        mesh.bounds = bounds;
    }
}
