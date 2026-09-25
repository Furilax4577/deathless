using System.Collections.Generic;
using UnityEngine;

// Explosion low poly (étape 65) : une coque facettée qui gonfle d'un coup puis se résorbe, un noyau plus clair, des
// éclats en pyramides projetés en cloche qui rétrécissent, et un éclair de lumière. Même famille de maillages que la
// boule de feu (FireballVisual). Sert à la boule de feu du mage (l'explosion verte du crâne du nécromancien est
// devenue une gerbe de gemmes, GemBurst).
// Purement visuel et local, créé chez chaque client par un ObserversRpc.
public class LowPolyBlast : MonoBehaviour
{
    private const float GrowTime = 0.18f;
    private const float HoldTime = 0.06f;
    private const float ShrinkTime = 0.3f;
    private const float ShardLife = 0.7f;

    private Transform shell;
    private Transform core;
    private Light flash;
    private float size;
    private float age;
    private float lightIntensity;
    private readonly List<Transform> shards = new List<Transform>();
    private readonly List<Vector3> velocities = new List<Vector3>();
    private readonly List<Vector3> spins = new List<Vector3>();
    private readonly List<float> shardScales = new List<float>();

    // Boule de feu : coque orange, noyau jaune, éclats orange et rouges.
    public static void Fire(Vector3 center, float radius, Material material)
    {
        Spawn(center, radius, material,
            Feu(VfxRole.Vif), VfxPalette.Lueur(Feu(VfxRole.Vif), 1.1f),
            Feu(VfxRole.Coeur), VfxPalette.Lueur(Feu(VfxRole.Coeur), 2.2f),
            Feu(VfxRole.Base), VfxPalette.Lueur(Feu(VfxRole.Base), 1f),
            Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.3f));
    }

    // Teintes du thème Feu (palette de référence : boule de feu de Relic).
    private static Color Feu(VfxRole role)
    {
        switch (role)
        {
            case VfxRole.Coeur: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(1f, 0.9f, 0.4f));
            case VfxRole.Vif: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(1f, 0.38f, 0.04f));
            default: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(0.8f, 0.12f, 0.03f));
        }
    }

    public static LowPolyBlast Spawn(Vector3 center, float radius, Material material, Color outer, Color outerGlow,
        Color inner, Color innerGlow, Color shard, Color shardGlow, Color lightColor)
    {
        if (material == null)
            return null;
        GameObject root = new GameObject("LowPolyBlast");
        root.transform.position = center;
        LowPolyBlast blast = root.AddComponent<LowPolyBlast>();
        blast.size = Mathf.Max(0.3f, radius * 1.6f);   // diamètre de la coque au maximum (un peu moins que la zone)

        blast.shell = Part("Shell", FireballVisual.BodyMesh, root.transform, material, outer, outerGlow).transform;
        blast.shell.rotation = Random.rotation;
        blast.core = Part("Core", FireballVisual.CoreMesh, root.transform, material, inner, innerGlow).transform;
        blast.core.rotation = Random.rotation;
        blast.shell.localScale = blast.core.localScale = Vector3.one * 0.05f;

        int count = Mathf.Clamp(Mathf.RoundToInt(8 + radius * 3f), 8, 20);
        for (int i = 0; i < count; i++)
        {
            bool dark = i % 3 == 0;
            Mesh mesh = i % 2 == 0 ? FireballVisual.TongueMesh : FireballVisual.EmberMesh;
            Transform piece = Part("Shard", mesh, root.transform, material, dark ? shard : outer, dark ? shardGlow : outerGlow).transform;
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) * 0.8f + 0.2f;
            piece.localPosition = direction * 0.2f;
            piece.rotation = Quaternion.LookRotation(direction);
            float scale = Random.Range(0.15f, 0.3f) * Mathf.Clamp(radius, 0.6f, 2.5f);
            piece.localScale = Vector3.one * scale;
            blast.shards.Add(piece);
            blast.shardScales.Add(scale);
            blast.velocities.Add(direction.normalized * Random.Range(3f, 6f) * Mathf.Clamp(radius * 0.6f, 0.6f, 2f));
            blast.spins.Add(Random.insideUnitSphere * 600f);
        }

        blast.flash = new GameObject("Flash").AddComponent<Light>();
        blast.flash.transform.SetParent(root.transform, false);
        blast.flash.type = LightType.Point;
        blast.flash.color = lightColor;
        blast.lightIntensity = 6f;
        blast.flash.intensity = blast.lightIntensity;
        blast.flash.range = radius * 4f + 3f;
        blast.flash.shadows = LightShadows.None;
        return blast;
    }

    private static GameObject Part(string name, Mesh mesh, Transform parent, Material material, Color color, Color glow)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        FireballVisual.Tint(renderer, color, glow);
        return go;
    }

    private void Update()
    {
        Step(Time.deltaTime);
        if (age >= Mathf.Max(ShardLife, GrowTime + HoldTime + ShrinkTime))
            Destroy(gameObject);
    }

    // Aperçu en édition (Update ne tourne pas) : avance l'explosion de `seconds` s.
    public void Simulate(float seconds)
    {
        const float dt = 1f / 60f;
        for (float t = 0f; t < seconds; t += dt)
            Step(dt);
    }

    private void Step(float deltaTime)
    {
        age += deltaTime;

        // Coque : gonfle vite (ralentit en fin de course), tient un instant, puis se résorbe.
        float shellScale;
        if (age < GrowTime)
        {
            float k = age / GrowTime;
            shellScale = size * (1f - (1f - k) * (1f - k));
        }
        else if (age < GrowTime + HoldTime)
            shellScale = size;
        else
            shellScale = size * Mathf.Clamp01(1f - (age - GrowTime - HoldTime) / ShrinkTime);
        shell.localScale = Vector3.one * Mathf.Max(0.001f, shellScale);
        core.localScale = Vector3.one * Mathf.Max(0.001f, shellScale * 0.72f);
        shell.Rotate(0f, 90f * deltaTime, 0f, Space.World);
        core.Rotate(0f, -140f * deltaTime, 0f, Space.World);
        bool shellGone = shellScale <= 0.001f;
        shell.gameObject.SetActive(!shellGone);
        core.gameObject.SetActive(!shellGone);

        // Eclats : projetés en cloche, ils tournent et rétrécissent.
        float shardK = Mathf.Clamp01(age / ShardLife);
        for (int i = 0; i < shards.Count; i++)
        {
            velocities[i] += Vector3.down * 12f * deltaTime;
            shards[i].position += velocities[i] * deltaTime;
            shards[i].Rotate(spins[i] * deltaTime, Space.Self);
            shards[i].localScale = Vector3.one * shardScales[i] * (1f - shardK);
        }

        flash.intensity = lightIntensity * Mathf.Clamp01(1f - age / (GrowTime + HoldTime + ShrinkTime));
    }
}
