using System.Collections.Generic;
using UnityEngine;

// Gerbe de terre low poly : quand un squelette sort du sol, des mottes facettées (petits icosaèdres et tétraèdres,
// bruns et gris pierre) jaillissent en cloche, retombent, roulent un peu sur le sol puis s'enfoncent en rétrécissant.
// Même famille de maillages que la boule de feu (FireballVisual), matériau partagé teinté par MaterialPropertyBlock,
// sans émission. Purement visuel et local (EnemyVisual la crée chez chaque client).
public class DirtBurst : MonoBehaviour
{
    private static readonly Color[] Palette =
    {
        new Color(0.42f, 0.3f, 0.19f), new Color(0.3f, 0.21f, 0.13f), new Color(0.52f, 0.4f, 0.27f), new Color(0.55f, 0.53f, 0.5f),
    };

    private const float Life = 1.6f;
    private const float SinkTime = 0.5f;

    private float groundY;
    private float age;
    private readonly List<Transform> pieces = new List<Transform>();
    private readonly List<Vector3> velocities = new List<Vector3>();
    private readonly List<Vector3> spins = new List<Vector3>();
    private readonly List<float> scales = new List<float>();

    // `strength` : 1 = gerbe normale ; plus petit pour une seconde gerbe plus discrète.
    public static DirtBurst Spawn(Vector3 center, Material material, float strength = 1f)
    {
        if (material == null)
            return null;
        GameObject root = new GameObject("DirtBurst");
        root.transform.position = center;
        DirtBurst burst = root.AddComponent<DirtBurst>();
        burst.groundY = center.y;
        int count = Mathf.RoundToInt(Mathf.Lerp(6f, 16f, Mathf.Clamp01(strength)));
        for (int i = 0; i < count; i++)
        {
            GameObject piece = new GameObject("Clod");
            piece.transform.SetParent(root.transform, false);
            piece.AddComponent<MeshFilter>().sharedMesh = i % 3 == 0 ? FireballVisual.EmberMesh : FireballVisual.CoreMesh;
            MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            FireballVisual.Tint(renderer, Palette[Random.Range(0, Palette.Length)], Color.black);
            Vector2 ring = Random.insideUnitCircle * 0.45f;
            piece.transform.localPosition = new Vector3(ring.x, 0.05f, ring.y);
            piece.transform.rotation = Random.rotation;
            float scale = Random.Range(0.1f, 0.24f) * Mathf.Lerp(0.7f, 1f, strength);
            piece.transform.localScale = Vector3.one * scale;
            Vector3 outward = new Vector3(ring.x, 0f, ring.y).normalized;
            burst.pieces.Add(piece.transform);
            burst.scales.Add(scale);
            burst.velocities.Add(outward * Random.Range(0.8f, 2.2f) + Vector3.up * Random.Range(2.5f, 4.5f) * Mathf.Lerp(0.7f, 1f, strength));
            burst.spins.Add(Random.insideUnitSphere * 500f);
        }
        return burst;
    }

    private void Update()
    {
        Step(Time.deltaTime);
        if (age >= Life)
            Destroy(gameObject);
    }

    // Aperçu en édition : avance la gerbe de `seconds` s.
    public void Simulate(float seconds)
    {
        const float dt = 1f / 60f;
        for (float t = 0f; t < seconds; t += dt)
            Step(dt);
    }

    private void Step(float deltaTime)
    {
        age += deltaTime;
        float sink = Mathf.Clamp01((age - (Life - SinkTime)) / SinkTime);
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform piece = pieces[i];
            Vector3 velocity = velocities[i];
            velocity += Vector3.down * 12f * deltaTime;
            Vector3 position = piece.position + velocity * deltaTime;
            // Au sol : la motte s'arrête (un petit rebond amorti), puis roule à peine.
            float floor = groundY + scales[i] * 0.3f;
            if (position.y < floor)
            {
                position.y = floor;
                velocity = new Vector3(velocity.x * 0.35f, Mathf.Abs(velocity.y) * 0.2f, velocity.z * 0.35f);
                spins[i] *= 0.4f;
            }
            velocities[i] = velocity;
            piece.position = position;
            piece.Rotate(spins[i] * deltaTime, Space.Self);
            piece.localScale = Vector3.one * scales[i] * (1f - sink);
        }
    }
}
