using UnityEngine;

// Passage d'un joueur par le portail (étape 72, demande de Quentin) : le personnage se désintègre en gemmes vertes,
// comme un squelette qui se vaporise, mais les gemmes filent vers le centre du portail en tourbillonnant et y
// disparaissent ; à la sortie, l'animation inverse : les gemmes jaillissent du centre du portail de sortie et
// reconstituent le corps. Même famille que le portail (LowPolyGem, matériau PortalVoxel). Un seul maillage en repère
// monde, purement visuel et local ; le serveur décide du passage (PlayerZone).
public class PortalTransit : MonoBehaviour
{
    private static Color[] Palette => VfxPalette.Cache("PortalTransit.Nyxessa", () => new[]
    {
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.07f, 0.38f, 0.05f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.7f, 0.08f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.55f, 0.95f, 0.2f)),
        VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.76f, 1f, 0.44f)) * 1.25f,
    });

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] bodyPoints;    // place de chaque gemme dans le corps
    private float[] sizes;
    private float[] phases;
    private Quaternion[] rotations;
    private Vector3[] spinAxes;
    private Color[] tints;
    private Vector3 portalCenter;
    private float seconds;
    private bool arriving;
    private float age;
    private Light flash;

    // Départ : les gemmes partent du corps (volume `body`) vers `portalCenter` en `seconds` s.
    public static PortalTransit Depart(Bounds body, Vector3 portalCenter, Material material, float seconds)
    {
        return Spawn(body, portalCenter, material, seconds, false);
    }

    // Arrivée : les gemmes jaillissent de `portalCenter` et se posent dans le corps en `seconds` s.
    public static PortalTransit Arrive(Bounds body, Vector3 portalCenter, Material material, float seconds)
    {
        return Spawn(body, portalCenter, material, seconds, true);
    }

    private static PortalTransit Spawn(Bounds body, Vector3 portalCenter, Material material, float seconds, bool arriving)
    {
        if (material == null)
            return null;
        GameObject go = new GameObject(arriving ? "PortalArrive" : "PortalDepart");
        PortalTransit transit = go.AddComponent<PortalTransit>();
        transit.Build(body, portalCenter, material, seconds, arriving);
        return transit;
    }

    private void Build(Bounds body, Vector3 center, Material material, float duration, bool inward)
    {
        portalCenter = center;
        seconds = Mathf.Max(0.2f, duration);
        arriving = inward;
        int count = 160;
        bodyPoints = new Vector3[count];
        sizes = new float[count];
        phases = new float[count];
        rotations = new Quaternion[count];
        spinAxes = new Vector3[count];
        tints = new Color[count];
        for (int i = 0; i < count; i++)
        {
            // Une colonne plus large en bas, comme la vaporisation des squelettes.
            float h = Random.value;
            Vector2 disc = Random.insideUnitCircle * Mathf.Lerp(0.5f, 0.3f, h);
            bodyPoints[i] = body.center + new Vector3(disc.x * body.size.x, (h - 0.5f) * body.size.y, disc.y * body.size.z);
            sizes[i] = Random.Range(0.04f, 0.085f);
            phases[i] = Random.value;
            rotations[i] = Random.rotation;
            spinAxes[i] = Random.onUnitSphere;
            tints[i] = Palette[Random.Range(0, Palette.Length)];
        }
        vertices = new Vector3[count * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "PortalTransit" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(count);
        Bounds extent = body;
        extent.Encapsulate(center);
        extent.Expand(1.5f);
        mesh.bounds = extent;
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        flash = new GameObject("Flash").AddComponent<Light>();
        flash.transform.SetParent(transform, false);
        flash.transform.position = center;
        flash.type = LightType.Point;
        flash.color = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.45f, 1f, 0.35f));
        flash.range = 5f;
        flash.shadows = LightShadows.None;
        Step(0f);
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }

    private void Update()
    {
        Step(Time.deltaTime);
        if (age >= seconds + 0.1f)
            Destroy(gameObject);
    }

    private void Step(float dt)
    {
        age += dt;
        float k = Mathf.Clamp01(age / seconds);
        Vector3 axis = Vector3.up;
        for (int i = 0; i < bodyPoints.Length; i++)
        {
            // Chaque gemme part avec un léger retard (le corps se défait de haut en bas, se refait de bas en haut).
            float delay = phases[i] * 0.35f;
            float local = Mathf.Clamp01((k - delay) / (1f - 0.35f));
            // Départ : du corps vers le portail, en accélérant ; arrivée : l'inverse, en freinant.
            float travel = arriving ? 1f - Mathf.Pow(1f - local, 2.2f) : Mathf.Pow(local, 2.2f);
            Vector3 from = bodyPoints[i], to = portalCenter;
            if (arriving) { from = portalCenter; to = bodyPoints[i]; }
            Vector3 position = Vector3.Lerp(from, to, travel);
            // Tourbillon autour de l'axe corps -> portail : les gemmes s'enroulent en s'approchant du centre.
            Vector3 line = (to - from);
            Vector3 side = Vector3.Cross(line.normalized, axis).normalized;
            Vector3 lift = Vector3.Cross(side, line.normalized);
            float swirl = Mathf.Sin(travel * Mathf.PI) * 0.45f;
            float angle = travel * Mathf.PI * 2f + phases[i] * Mathf.PI * 2f;
            position += (side * Mathf.Cos(angle) + lift * Mathf.Sin(angle)) * swirl;
            // Taille : pleine dans le corps, nulle dans le portail.
            float presence = arriving ? travel : 1f - travel;
            float size = sizes[i] * Mathf.Clamp01(presence * 1.2f);
            Quaternion spin = Quaternion.AngleAxis(age * (240f + phases[i] * 200f), spinAxes[i]) * rotations[i];
            Color color = Color.Lerp(tints[i], Palette[3], (1f - presence) * 0.7f);
            LowPolyGem.Write(vertices, colors, i, position, size, new Vector3(0.7f, 1.3f, 0.7f), spin, color, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        if (flash != null)
            flash.intensity = 3f * Mathf.Sin(Mathf.PI * k);
    }
}
