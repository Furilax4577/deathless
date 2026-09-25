using UnityEngine;

// Gerbe de gemmes low poly vertes (même style que la soupe du portail), en un seul maillage. Remplace l'ancienne
// explosion verte (LowPolyBlast.Ghost) : ouverture et fermeture du portail ; Shatter fait éclater une forme déjà en
// gemmes (le crâne du nécromancien à l'impact, SkullMissileVisual).
// - Explosion : les gemmes jaillissent du point d'impact dans toutes les directions, tournent, ralentissent,
//   retombent un peu et rétrécissent jusqu'à disparaître ; un bref éclair vert.
// - Implosion : elles partent d'une sphère autour du point et sont aspirées vers lui en rétrécissant (fermeture du
//   portail).
// Purement visuel et local ; shader Relic/VertexColorUnlit (matériau PortalVoxel).
public class GemBurst : MonoBehaviour
{
    private static Color[] Palette => VfxPalette.Cache("GemBurst.Nyxessa", () => new[]
    {
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.07f, 0.38f, 0.05f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.7f, 0.08f)),
        VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.55f, 0.95f, 0.2f)),
        VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.76f, 1f, 0.44f)) * 1.25f,
    });

    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] positions;
    private Vector3[] velocities;
    private Vector3[] stretch;
    private Quaternion[] rotation;
    private Vector3[] spinAxis;
    private float[] spinSpeed;
    private float[] size;
    private Color[] color;
    private float life;
    private float age;
    private bool implode;
    private Light flash;
    private float flashIntensity;

    // Explosion de `radius` m (portée des gemmes) au point `center`.
    public static GemBurst Explode(Vector3 center, float radius, Material material)
    {
        return Spawn(center, radius, material, false);
    }

    // Implosion : les gemmes convergent vers `center` depuis une sphère de `radius` m.
    public static GemBurst Implode(Vector3 center, float radius, Material material)
    {
        return Spawn(center, radius, material, true);
    }

    // Eclatement d'une forme déjà faite de gemmes (crâne du nécromancien) : chaque gemme part de sa place, poussée
    // vers l'extérieur depuis `center` (`speed` m/s), plus la vitesse `inherited` du projectile, avec sa couleur.
    public static GemBurst Shatter(Vector3 center, Vector3[] worldPositions, Color[] gemColors, float gemSize, float speed,
        Vector3 inherited, Material material)
    {
        if (material == null || worldPositions == null || worldPositions.Length == 0)
            return null;
        GameObject go = new GameObject("GemShatter");
        go.transform.position = center;
        GemBurst burst = go.AddComponent<GemBurst>();
        int count = worldPositions.Length;
        burst.Allocate(count);
        burst.life = 0.7f;
        for (int i = 0; i < count; i++)
        {
            Vector3 local = worldPositions[i] - center;
            Vector3 dir = local.sqrMagnitude > 1e-6f ? local.normalized : Random.onUnitSphere;
            burst.positions[i] = local;
            burst.velocities[i] = (dir + Random.insideUnitSphere * 0.5f) * speed * Random.Range(0.5f, 1.3f) + inherited;
            burst.stretch[i] = new Vector3(0.8f, 1.3f, 0.8f);
            burst.rotation[i] = Random.rotation;
            burst.spinAxis[i] = Random.onUnitSphere;
            burst.spinSpeed[i] = Random.Range(200f, 600f);
            burst.size[i] = gemSize * Random.Range(0.9f, 1.4f);
            burst.color[i] = gemColors != null && i < gemColors.Length ? gemColors[i] : Palette[Random.Range(0, Palette.Length)];
        }
        burst.shattered = true;
        burst.Finish(material, speed * burst.life + 1f, 3f);
        return burst;
    }

    private bool shattered;
    private bool rising;

    // Couleurs de la poussière d'os (vaporisation à l'aube) : os pâle, gris, et quelques éclats verts (la magie qui s'en va).
    private static Color[] BonePalette => VfxPalette.Cache("GemBurst.Os", () => new[]
    {
        VfxPalette.Couleur(VfxTheme.Os, VfxRole.Coeur, new Color(0.92f, 0.9f, 0.8f)),
        VfxPalette.Couleur(VfxTheme.Os, VfxRole.Base, new Color(0.78f, 0.75f, 0.66f)),
        VfxPalette.Couleur(VfxTheme.Os, VfxRole.Ombre, new Color(0.6f, 0.58f, 0.52f)),
        VfxPalette.Accent(VfxTheme.Os, "Magie", new Color(0.55f, 1f, 0.45f)),
    });

    // Vaporisation d'un squelette à l'aube (étape 69) : des gemmes couleur os remplissent le volume `bounds` (une colonne
    // plus large en bas), puis montent en tournoyant, poussées par un léger vent, et rétrécissent jusqu'à disparaître.
    public static GemBurst Rise(Bounds bounds, Material material)
    {
        if (material == null)
            return null;
        GameObject go = new GameObject("GemRise");
        go.transform.position = bounds.center;
        GemBurst burst = go.AddComponent<GemBurst>();
        int count = Mathf.Clamp(Mathf.RoundToInt(bounds.size.y * 65f), 60, 150);
        burst.Allocate(count);
        burst.life = 1.7f;
        burst.rising = true;
        burst.shattered = true;   // gemmes déjà formées : pas d'éclosion
        Vector3 wind = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * 0.6f;
        for (int i = 0; i < count; i++)
        {
            float h = Random.value;
            Vector2 disc = Random.insideUnitCircle * Mathf.Lerp(0.5f, 0.25f, h);
            burst.positions[i] = new Vector3(disc.x * bounds.size.x, (h - 0.5f) * bounds.size.y, disc.y * bounds.size.z);
            // Le haut part un peu avant le bas : la silhouette se défait par la tête.
            burst.velocities[i] = Vector3.up * Random.Range(0.4f, 1.2f) * (0.6f + h) + wind + Random.insideUnitSphere * 0.3f;
            burst.stretch[i] = new Vector3(Random.Range(0.6f, 1f), Random.Range(0.9f, 1.4f), Random.Range(0.6f, 1f));
            burst.rotation[i] = Random.rotation;
            burst.spinAxis[i] = Random.onUnitSphere;
            burst.spinSpeed[i] = Random.Range(120f, 420f);
            burst.size[i] = Random.Range(0.045f, 0.085f);
            burst.color[i] = BonePalette[Random.value < 0.12f ? 3 : Random.Range(0, 3)];
        }
        burst.Finish(material, bounds.size.y * 3f + 3f, 0.9f);
        // Eclair couleur soleil levant (l'aube), pas le vert de la magie.
        if (burst.flash != null)
            burst.flash.color = new Color(1f, 0.85f, 0.6f);
        return burst;
    }

    private void Allocate(int count)
    {
        positions = new Vector3[count];
        velocities = new Vector3[count];
        stretch = new Vector3[count];
        rotation = new Quaternion[count];
        spinAxis = new Vector3[count];
        spinSpeed = new float[count];
        size = new float[count];
        color = new Color[count];
    }

    private static GemBurst Spawn(Vector3 center, float radius, Material material, bool implode)
    {
        if (material == null)
            return null;
        GameObject go = new GameObject(implode ? "GemImplode" : "GemBurst");
        go.transform.position = center;
        GemBurst burst = go.AddComponent<GemBurst>();
        burst.Build(radius, material, implode);
        return burst;
    }

    private void Build(float radius, Material material, bool inward)
    {
        implode = inward;
        life = implode ? 0.55f : 0.75f;
        int count = Mathf.Clamp(Mathf.RoundToInt(40 + radius * 30f), 30, 90);
        Allocate(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            float speed = Random.Range(0.6f, 1.3f) * radius / life * 1.6f;
            if (implode)
            {
                positions[i] = dir * radius * Random.Range(0.8f, 1.2f);
                velocities[i] = -positions[i] / life;
            }
            else
            {
                positions[i] = dir * radius * 0.08f;
                velocities[i] = dir * speed;
            }
            stretch[i] = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.8f, 1.5f), Random.Range(0.6f, 1.2f));
            rotation[i] = Random.rotation;
            spinAxis[i] = Random.onUnitSphere;
            spinSpeed[i] = Random.Range(200f, 600f);
            size[i] = Random.Range(0.05f, 0.11f) * Mathf.Clamp(radius, 0.5f, 1.5f);
            color[i] = Palette[Random.Range(0, Palette.Length)];
        }
        Finish(material, radius * 4f, implode ? 2.5f : 4f);
    }

    // Maillage, rendu et éclair communs aux trois formes de gerbe.
    private void Finish(Material material, float extent, float flashPower)
    {
        int count = positions.Length;
        vertices = new Vector3[count * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "GemBurst" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(count);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * extent);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        flash = new GameObject("Flash").AddComponent<Light>();
        flash.transform.SetParent(transform, false);
        flash.type = LightType.Point;
        flash.color = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.45f, 1f, 0.35f));
        flashIntensity = flashPower;
        flash.range = extent * 0.75f + 2f;
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
        if (age >= life)
            Destroy(gameObject);
    }

    // Aperçu en édition : avance la gerbe de `seconds` s.
    public void Simulate(float seconds)
    {
        const float dt = 1f / 60f;
        for (float t = 0f; t < seconds; t += dt)
            Step(dt);
    }

    private void Step(float dt)
    {
        age += dt;
        float k = Mathf.Clamp01(age / life);
        for (int i = 0; i < positions.Length; i++)
        {
            if (implode)
                positions[i] += velocities[i] * dt;
            else if (rising)
            {
                // Poussière qui s'élève : elle accélère un peu vers le haut, sans retomber.
                velocities[i] += Vector3.up * 1.6f * dt;
                velocities[i] *= Mathf.Max(0f, 1f - 0.6f * dt);
                positions[i] += velocities[i] * dt;
            }
            else
            {
                // Freinage et légère chute : la gerbe s'ouvre vite puis flotte.
                velocities[i] *= Mathf.Max(0f, 1f - 3.2f * dt);
                velocities[i] += Vector3.down * 2.5f * dt;
                positions[i] += velocities[i] * dt;
            }
            // Taille : éclosion très rapide, puis la gemme rétrécit jusqu'à disparaître.
            // (Un éclatement part de gemmes déjà formées : pas d'éclosion.)
            float grow = implode || shattered ? 1f - k * k : Mathf.Clamp01(age / 0.06f) * (1f - k * k);
            Quaternion spin = Quaternion.AngleAxis(age * spinSpeed[i], spinAxis[i]) * rotation[i];
            LowPolyGem.Write(vertices, colors, i, positions[i], size[i] * grow, stretch[i], spin, color[i], LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        if (flash != null)
            flash.intensity = flashIntensity * (implode ? k : 1f - k);
    }
}
