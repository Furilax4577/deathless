using UnityEngine;

// Boule de feu low poly du mage (étape 65) : maillages facettés générés par script, dans l'esprit des modèles KayKit
// (aucune particule floue). Un corps orange à facettes irrégulières, un noyau jaune qui perce à l'avant, une lumière
// chaude et une traînée de débris tétraédriques jaunes, orange et rouges (retour de Quentin : pas de pointes vers
// l'arrière, pas de ruban de fumée). L'avant de la boule est l'axe +z de son parent (le projectile vole selon son +z).
// Un seul matériau émissif partagé (FireBurst), teinté par MaterialPropertyBlock. Purement visuel et local.
public class FireballVisual : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // Emission modérée : trop forte, le tonemapping ramène tout au jaune et les trois teintes se confondent.
    private static Color Yellow => VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Coeur, new Color(1f, 0.9f, 0.4f));
    private static Color YellowGlow => VfxPalette.Lueur(Yellow, 2.2f);
    private static Color Orange => VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, new Color(1f, 0.38f, 0.04f));
    private static Color OrangeGlow => VfxPalette.Lueur(Orange, 1.1f);
    private static Color Red => VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Base, new Color(0.8f, 0.12f, 0.03f));
    private static Color RedGlow => VfxPalette.Lueur(Red, 1f);

    private static Mesh bodyMesh;
    private static Mesh coreMesh;
    private static Mesh tongueMesh;
    private static Mesh emberMesh;

    private Material material;
    private Transform body;
    private float nextEmber;

    // Construit la boule de feu sous `parent`. `size` : 1 = environ 45 cm de diamètre.
    public static FireballVisual Attach(Transform parent, Material material, float size = 1f)
    {
        EnsureMeshes();
        GameObject root = new GameObject("Fireball");
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * size;
        FireballVisual fireball = root.AddComponent<FireballVisual>();
        fireball.material = material;
        fireball.Build();
        return fireball;
    }

    private void Build()
    {
        body = Part("Body", bodyMesh, transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.44f, Orange, OrangeGlow).transform;
        Part("Core", coreMesh, body, new Vector3(0f, 0f, 0.22f), Quaternion.identity, Vector3.one * 0.62f, Yellow, YellowGlow);

        Light glow = new GameObject("Light").AddComponent<Light>();
        glow.transform.SetParent(transform, false);
        glow.type = LightType.Point;
        glow.color = Color.Lerp(Orange, Yellow, 0.3f);
        glow.intensity = 3f;
        glow.range = 6f;
        glow.shadows = LightShadows.None;
    }

    private GameObject Part(string name, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Color color, Color emission)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Tint(renderer, color, emission);
        return go;
    }

    // Maillages facettés partagés, réutilisés par l'explosion (LowPolyBlast) et le crâne du nécromancien.
    public static Mesh BodyMesh { get { EnsureMeshes(); return bodyMesh; } }
    public static Mesh CoreMesh { get { EnsureMeshes(); return coreMesh; } }
    public static Mesh TongueMesh { get { EnsureMeshes(); return tongueMesh; } }
    public static Mesh EmberMesh { get { EnsureMeshes(); return emberMesh; } }

    public static void Tint(Renderer renderer, Color color, Color emission)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, color);
        block.SetColor(EmissionColorId, emission);
        renderer.SetPropertyBlock(block);
    }

    private void Update()
    {
        // Le corps roule sur lui-même ; la traînée, ce sont les débris facettés laissés derrière.
        body.Rotate(0f, 0f, 300f * Time.deltaTime, Space.Self);

        // Débris semés le long du trajet parcouru depuis l'image précédente (un tous les `EmberSpacing` m), pour une traînée
        // continue quelle que soit la cadence d'affichage ; immobile, la boule en lâche quand même quelques-uns.
        float scale = transform.lossyScale.x;
        Vector3 position = transform.position;
        if (!hasLastPosition)
        {
            lastPosition = position;
            hasLastPosition = true;
        }
        emberDistance += Vector3.Distance(lastPosition, position);
        if (Time.time >= nextEmber)
        {
            nextEmber = Time.time + 0.05f;
            emberDistance = Mathf.Max(emberDistance, EmberSpacing);
        }
        int count = Mathf.Min(12, Mathf.FloorToInt(emberDistance / EmberSpacing));
        for (int i = 0; i < count; i++)
        {
            Vector3 along = Vector3.Lerp(lastPosition, position, (i + 1f) / count);
            SpawnEmber(along + Random.insideUnitSphere * 0.2f * scale, material, 1.7f * scale);
        }
        emberDistance -= count * EmberSpacing;
        lastPosition = position;
    }

    private const float EmberSpacing = 0.3f;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private float emberDistance;

    // Braise laissée derrière la boule : tétraèdre jaune, orange ou rouge qui rétrécit (FireballEmber).
    public static GameObject SpawnEmber(Vector3 position, Material material, float size = 1f)
    {
        float pick = Random.value;
        if (pick < 0.3f) return SpawnEmber(position, material, size, Yellow, YellowGlow);
        if (pick < 0.75f) return SpawnEmber(position, material, size, Orange, OrangeGlow);
        return SpawnEmber(position, material, size, Red, RedGlow);
    }

    // Même braise, couleur imposée (traînée verte du crâne du nécromancien).
    public static GameObject SpawnEmber(Vector3 position, Material material, float size, Color color, Color glow)
    {
        EnsureMeshes();
        GameObject ember = new GameObject("Ember");
        ember.transform.position = position;
        ember.transform.rotation = Random.rotation;
        ember.transform.localScale = Vector3.one * Random.Range(0.07f, 0.13f) * size;
        ember.AddComponent<MeshFilter>().sharedMesh = emberMesh;
        MeshRenderer renderer = ember.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Tint(renderer, color, glow);
        ember.AddComponent<FireballEmber>().Init(Random.Range(0.25f, 0.45f));
        return ember;
    }

    // ---------------------------------------------------------------- maillages facettés

    private static void EnsureMeshes()
    {
        if (bodyMesh != null)
            return;
        bodyMesh = Icosphere(1, 0.14f, 7);
        coreMesh = Icosphere(0, 0.1f, 3);
        tongueMesh = Pyramid();
        emberMesh = Tetrahedron();
    }

    // Icosaèdre (subdivisé `subdivisions` fois), rayon 0,5, sommets décalés au hasard de ±`jitter` pour un contour
    // irrégulier ; chaque face a ses propres sommets, donc une normale unique : rendu à facettes.
    private static Mesh Icosphere(int subdivisions, float jitter, int seed)
    {
        float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
        System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i].normalized;
        int[] faces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        for (int s = 0; s < subdivisions; s++)
        {
            System.Collections.Generic.Dictionary<long, int> middles = new System.Collections.Generic.Dictionary<long, int>();
            int[] next = new int[faces.Length * 4];
            int n = 0;
            for (int f = 0; f < faces.Length; f += 3)
            {
                int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                int ab = Middle(a, b, vertices, middles), bc = Middle(b, c, vertices, middles), ca = Middle(c, a, vertices, middles);
                int[] tris = { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca };
                System.Array.Copy(tris, 0, next, n, 12);
                n += 12;
            }
            faces = next;
        }
        System.Random random = new System.Random(seed);
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i] * 0.5f * (1f + ((float)random.NextDouble() * 2f - 1f) * jitter);
        return Flat(vertices, faces, Vector3.zero);
    }

    private static int Middle(int a, int b, System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.Dictionary<long, int> cache)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out int index))
            return index;
        vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
        cache[key] = vertices.Count - 1;
        return vertices.Count - 1;
    }

    // Pyramide à base carrée (côté 1, en z = 0), pointe en z = 1.
    private static Mesh Pyramid()
    {
        System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0f, 0f, 1f),
        };
        int[] faces = { 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4, 0, 2, 1, 0, 3, 2 };
        return Flat(v, faces, new Vector3(0f, 0f, 0.25f));
    }

    private static Mesh Tetrahedron()
    {
        System.Collections.Generic.List<Vector3> v = new System.Collections.Generic.List<Vector3>
        {
            new Vector3(1, 1, 1) * 0.5f, new Vector3(-1, -1, 1) * 0.5f, new Vector3(-1, 1, -1) * 0.5f, new Vector3(1, -1, -1) * 0.5f,
        };
        int[] faces = { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
        return Flat(v, faces, Vector3.zero);
    }

    // Un sommet par coin de face (normales plates) ; chaque face est retournée au besoin pour regarder vers l'extérieur
    // (à l'opposé de `center`), quel que soit l'ordre donné.
    private static Mesh Flat(System.Collections.Generic.List<Vector3> vertices, int[] faces, Vector3 center)
    {
        Vector3[] positions = new Vector3[faces.Length];
        int[] triangles = new int[faces.Length];
        for (int f = 0; f < faces.Length; f += 3)
        {
            Vector3 a = vertices[faces[f]], b = vertices[faces[f + 1]], c = vertices[faces[f + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, (a + b + c) / 3f - center) < 0f)
            {
                Vector3 swap = b;
                b = c;
                c = swap;
            }
            positions[f] = a;
            positions[f + 1] = b;
            positions[f + 2] = c;
            triangles[f] = f;
            triangles[f + 1] = f + 1;
            triangles[f + 2] = f + 2;
        }
        Mesh mesh = new Mesh { name = "LowPolyFire" };
        mesh.vertices = positions;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
