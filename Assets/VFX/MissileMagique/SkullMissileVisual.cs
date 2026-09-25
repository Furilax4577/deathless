// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/SkullMissileVisual.cs : sans GameAudio (plainte en vol, cri à
// l'éclatement). Le reste est identique.
using UnityEngine;

// Missile magique du nécromancien (étape 68) : le crâne KayKit (Halloween Bits) refait en « soupe de gemmes » low poly,
// comme le portail. Les points de la forme viennent d'un GemShape calculé dans l'éditeur (GemShapeBaker) ; chaque gemme
// est un petit octaèdre vert, sombre dans les orbites et les dents, pâle sur l'os, couchée à plat sur la surface comme une écaille, qui frémit sur place.
// Derrière lui, une traînée de gemmes qui rétrécissent (GemTrail). A l'impact, le crâne lui-même éclate : ses gemmes
// partent de leur place (GemBurst.Shatter). L'avant est l'axe +z du parent. Purement visuel et local.
public class SkullMissileVisual : MonoBehaviour
{
    // Du plus sombre (orbites, dents) au plus pâle (os), vert Nyxessa.
    private static Color Dark => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Ombre, new Color(0.03f, 0.23f, 0.07f)) * 0.3f;
    private static Color Mid => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.18f, 0.6f, 0.1f));
    private static Color Pale => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.5f, 0.95f, 0.38f));   // sous 1 : le Bloom ne le délave pas en blanc

    private const float GemSize = 0.021f;
    private float echelle = 1f;
    private float gemSize = GemSize;
    private VfxLumiere lumiere;

    private Transform skull;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] points;         // place de chaque gemme dans le repère du crâne
    private Color[] tints;
    private Quaternion[] rotations;
    private Vector3[] spinAxes;
    private float[] phases;
    private Material material;
    private GemTrail trail;
    private float seed;
    private bool shattered;
    private Vector3 lastPosition;
    private Vector3 velocity;

    // `shape` : forme en gemmes du crâne ; `euler` : rotation qui tourne le visage vers l'avant (+z) ; `size` : taille du
    // crâne (m) ; `gemMaterial` : matériau à couleurs par sommet (PortalVoxel).
    // `echelle` (défaut 1) : échelle d'ensemble du missile (crâne, gemmes, traînée, éclats) ; 1,5 pour le missile tiré
    // par la relique (Nyxessa.TirerMissile), 1 pour le missile du nécromancien (inchangé).
    public static SkullMissileVisual Attach(Transform parent, GemShape shape, Vector3 euler, float size, Material gemMaterial, float echelle = 1f)
    {
        if (shape == null || gemMaterial == null || shape.points.Length == 0)
            return null;
        GameObject root = new GameObject("SkullMissile");
        root.transform.SetParent(parent, false);
        SkullMissileVisual visual = root.AddComponent<SkullMissileVisual>();
        visual.material = gemMaterial;
        visual.seed = Random.value * 100f;
        visual.echelle = Mathf.Max(0.1f, echelle);
        visual.gemSize = GemSize * visual.echelle;
        visual.Build(shape, euler, size * visual.echelle);
        visual.trail = GemTrail.Follow(root.transform, gemMaterial, 0.07f * visual.echelle);
        visual.lastPosition = root.transform.position;
        return visual;
    }

    private void Build(GemShape shape, Vector3 euler, float size)
    {
        skull = new GameObject("Skull").transform;
        skull.SetParent(transform, false);

        int count = shape.points.Length;
        Quaternion turn = Quaternion.Euler(euler);
        points = new Vector3[count];
        tints = new Color[count];
        rotations = new Quaternion[count];
        spinAxes = new Vector3[count];
        phases = new float[count];
        // Contraste appuyé : la texture du crâne varie peu (os clair, creux à peine plus sombres), on étire son écart.
        float low = 1f, high = 0f;
        foreach (float value in shape.brightness) { low = Mathf.Min(low, value); high = Mathf.Max(high, value); }
        for (int i = 0; i < count; i++)
        {
            points[i] = turn * shape.points[i] * size;
            float b = i < shape.brightness.Length ? shape.brightness[i] : high;
            float k = high > low + 0.01f ? Mathf.Clamp01((b - low) / (high - low)) : 0.8f;
            // Seuil adouci : l'os reste franchement clair, les creux franchement sombres (le crâne doit se lire de loin).
            k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.35f) / 0.4f));
            tints[i] = k < 0.5f ? Color.Lerp(Dark, Mid, k * 2f) : Color.Lerp(Mid, Pale, (k - 0.5f) * 2f);
            // Gemme couchée à plat sur la surface (écaille), tournée au hasard autour de la normale : un crâne en mosaïque
            // net, sans trous vers l'arrière de la tête. Sans normale (ancienne forme), orientation au hasard.
            Vector3 normal = i < shape.normals.Length ? turn * shape.normals[i] : Vector3.zero;
            rotations[i] = normal.sqrMagnitude > 0.5f
                ? Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                : Random.rotation;
            spinAxes[i] = Random.onUnitSphere;
            phases[i] = Random.value * 10f;
        }

        vertices = new Vector3[count * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "SkullGems" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(count);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * size * 1.6f);
        skull.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = skull.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Lumière commune des effets (thème Nyxessa ; petite, moyenne pour un gros missile).
        lumiere = VfxLumiere.Creer(transform, Vector3.zero, VfxTheme.Nyxessa, echelle >= 1.25f ? VfxTailleLumiere.Moyenne : VfxTailleLumiere.Petite);
        Refresh(0f);
    }

    private void OnDestroy()
    {
        // Pas d'éclatement ici (fin de scène possible) : le projectile appelle Shatter à sa disparition.
        if (trail != null)
            trail.Stop();
        if (mesh != null)
            Destroy(mesh);
    }

    // Le crâne éclate : ses gemmes partent de leur place, la traînée s'éteint. Appelé à la disparition du projectile.
    public void Shatter()
    {
        if (shattered || points == null || skull == null)
            return;
        shattered = true;
        if (trail != null)
            trail.Stop();
        Vector3[] world = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
            world[i] = skull.TransformPoint(Jittered(i, Time.time));
        GemBurst.Shatter(skull.position, world, tints, gemSize, 3.5f * Mathf.Sqrt(echelle), velocity * 0.15f, material);
        skull.gameObject.SetActive(false);
        if (lumiere != null)
            lumiere.Eteindre();
    }

    private void Update()
    {
        if (shattered)
            return;
        if (Time.deltaTime > 0f)
            velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        Refresh(Time.time);
    }

    // Aperçu en édition : pose le crâne à l'instant `time`.
    public void Preview(float time)
    {
        Refresh(time);
    }

    // Frémissement : chaque gemme bouge un peu autour de sa place.
    private Vector3 Jittered(int i, float time)
    {
        float p = phases[i];
        return points[i] + new Vector3(Mathf.Sin(time * 9f + p), Mathf.Sin(time * 11f + p * 1.7f), Mathf.Sin(time * 7f + p * 2.3f)) * 0.005f * echelle;
    }

    private void Refresh(float time)
    {
        // Le crâne oscille un peu (lacet et roulis), comme l'ancien modèle.
        skull.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 5f + seed) * 10f, Mathf.Sin(time * 3.3f + seed) * 14f);
        for (int i = 0; i < points.Length; i++)
        {
            // Frémissement : la gemme bascule un peu autour de sa pose (±12°) sans quitter la surface.
            Quaternion spin = Quaternion.AngleAxis(Mathf.Sin(time * 8f + phases[i] * 5f) * 12f, spinAxes[i]) * rotations[i];
            // Scintillement : chaque gemme s'éclaire un peu à son rythme.
            float flicker = 0.85f + 0.25f * Mathf.Sin(time * 6f + phases[i] * 3f);
            LowPolyGem.Write(vertices, colors, i, Jittered(i, time), gemSize, new Vector3(1.25f, 1.25f, 0.45f), spin, tints[i] * flicker, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
    }
}
