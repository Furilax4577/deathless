using UnityEngine;

// Traînée low poly (étape 68) : de petites gemmes vertes semées derrière un projectile (crâne du nécromancien), qui
// restent sur place, tournent, dérivent un peu et rétrécissent jusqu'à disparaître. Un seul maillage en repère monde,
// indépendant du projectile : à l'impact, la traînée finit de s'éteindre toute seule. Purement visuel et local.
public class GemTrail : MonoBehaviour
{
    private const int Capacity = 180;
    private const float Life = 0.55f;
    private const float Spacing = 0.06f;   // une gemme tous les 6 cm parcourus

    private static readonly Color[] Palette =
    {
        new Color(0.07f, 0.38f, 0.05f), new Color(0.25f, 0.7f, 0.08f), new Color(0.55f, 0.95f, 0.2f), new Color(0.95f, 1.25f, 0.55f),
    };

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private readonly Vector3[] positions = new Vector3[Capacity];
    private readonly Vector3[] velocities = new Vector3[Capacity];
    private readonly Quaternion[] rotations = new Quaternion[Capacity];
    private readonly Vector3[] spinAxes = new Vector3[Capacity];
    private readonly float[] born = new float[Capacity];
    private readonly float[] sizes = new float[Capacity];
    private readonly Color[] tints = new Color[Capacity];
    private int next;
    private Transform source;
    private Vector3 lastSource;
    private float carried;
    private bool stopped;
    private float stoppedAt;

    // Traînée qui suit `source` (son arrière est son -z) ; `size` : taille moyenne d'une gemme (m).
    public static GemTrail Follow(Transform source, Material material, float size)
    {
        GameObject go = new GameObject("GemTrail");
        GemTrail trail = go.AddComponent<GemTrail>();
        trail.source = source;
        trail.lastSource = source.position;
        trail.Build(material, size);
        return trail;
    }

    // Le projectile a disparu : plus de nouvelles gemmes, la traînée s'éteint puis se détruit.
    public void Stop()
    {
        if (stopped)
            return;
        stopped = true;
        stoppedAt = Time.time;
        source = null;
    }

    private float baseSize;

    private void Build(Material material, float size)
    {
        baseSize = size;
        for (int i = 0; i < Capacity; i++)
            born[i] = -100f;
        vertices = new Vector3[Capacity * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "GemTrail" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(Capacity);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    private void Spawn(Vector3 at, Vector3 back)
    {
        int i = next;
        next = (next + 1) % Capacity;
        positions[i] = at + Random.insideUnitSphere * baseSize * 2.5f;
        velocities[i] = back * Random.Range(0.3f, 0.9f) + Random.insideUnitSphere * 0.4f;
        rotations[i] = Random.rotation;
        spinAxes[i] = Random.onUnitSphere;
        born[i] = Time.time;
        sizes[i] = baseSize * Random.Range(0.6f, 1.2f);
        tints[i] = Palette[Random.Range(0, Palette.Length)];
    }

    private void Update()
    {
        // Semis le long du trajet parcouru depuis l'image précédente (traînée continue quelle que soit la cadence).
        if (!stopped && source != null)
        {
            Vector3 now = source.position;
            Vector3 back = -source.forward;
            carried += Vector3.Distance(lastSource, now);
            int count = Mathf.Min(12, Mathf.FloorToInt(carried / Spacing));
            for (int k = 0; k < count; k++)
                Spawn(Vector3.Lerp(lastSource, now, (k + 1f) / count) + back * 0.18f, back);
            carried -= count * Spacing;
            lastSource = now;
        }
        else if (source == null && !stopped)
            Stop();

        Bounds bounds = new Bounds();
        bool any = false;
        for (int i = 0; i < Capacity; i++)
        {
            float age = Time.time - born[i];
            float k = age / Life;
            if (k >= 1f)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            positions[i] += velocities[i] * Time.deltaTime;
            velocities[i] *= Mathf.Max(0f, 1f - 2.5f * Time.deltaTime);
            Quaternion spin = Quaternion.AngleAxis(age * 360f, spinAxes[i]) * rotations[i];
            // Les plus récentes sont claires, puis la gemme fonce en rétrécissant.
            Color tint = Color.Lerp(tints[i], Palette[0], k * 0.6f);
            LowPolyGem.Write(vertices, colors, i, positions[i], sizes[i] * (1f - k), new Vector3(0.8f, 1.3f, 0.8f), spin, tint, LowPolyGem.DefaultLight);
            if (!any) { bounds = new Bounds(positions[i], Vector3.one * 0.3f); any = true; }
            else bounds.Encapsulate(positions[i]);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        if (any)
        {
            bounds.Expand(0.3f);
            mesh.bounds = bounds;
        }
        if (stopped && Time.time - stoppedAt > Life + 0.1f)
            Destroy(gameObject);
    }
}
