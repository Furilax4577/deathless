using UnityEngine;

// Traînée qui serpente (étape 65) : un TrailRenderer porté par un point qui tourne autour de l'axe de vol du projectile
// (son +z) et dont le rayon ondule ; le ruban laissé derrière zigzague comme une fumée. Deux traînées déphasées
// forment une hélice. Utilisée par la boule de feu du mage et le crâne du nécromancien. Purement visuel et local.
public class WavyTrail : MonoBehaviour
{
    private float radius;
    private float turnsPerSecond;
    private float phase;
    private float wobble;
    private float seed;
    private float segment = 0.15f;

    // `colors` : couleur de la tête à la queue du ruban (l'alpha de la queue à 0 pour qu'il s'efface) ;
    // `width` : largeur à la tête ; `time` : durée de vie d'un point du ruban (longueur de la traînée) ;
    // `segment` : distance entre deux points du ruban (grande = ruban anguleux, low poly).
    public static WavyTrail Create(Transform parent, Material material, Gradient colors, float width, float time,
        float radius, float turnsPerSecond, float phase, float segment = 0.15f)
    {
        GameObject go = new GameObject("WavyTrail");
        go.transform.SetParent(parent, false);
        WavyTrail trail = go.AddComponent<WavyTrail>();
        trail.radius = radius;
        trail.turnsPerSecond = turnsPerSecond;
        trail.phase = phase;
        trail.wobble = 0.5f;
        trail.seed = Random.value * 50f;
        trail.Place(0f);

        TrailRenderer renderer = go.AddComponent<TrailRenderer>();
        renderer.sharedMaterial = material;
        renderer.colorGradient = colors;
        renderer.time = time;
        renderer.widthMultiplier = width;
        renderer.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.8f), new Keyframe(1f, 0.15f));
        // Des points espacés : le ruban reste anguleux, dans l'esprit low poly.
        renderer.minVertexDistance = segment;
        trail.segment = segment;
        renderer.numCapVertices = 0;
        renderer.numCornerVertices = 0;
        renderer.alignment = LineAlignment.View;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return trail;
    }

    // Dégradé en trois couleurs (tête, milieu, queue) avec l'alpha de chacune.
    public static Gradient Colors(Color head, Color middle, Color tail)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(head, 0f), new GradientColorKey(middle, 0.4f), new GradientColorKey(tail, 1f) },
            new[] { new GradientAlphaKey(head.a, 0f), new GradientAlphaKey(middle.a, 0.4f), new GradientAlphaKey(tail.a, 1f) });
        return gradient;
    }

    private void Update()
    {
        Place(Time.time);
    }

    // Position autour de l'axe : angle qui tourne, rayon qui ondule un peu au hasard.
    private void Place(float time)
    {
        float angle = phase + time * turnsPerSecond * Mathf.PI * 2f;
        float r = radius * (1f - wobble * 0.5f + wobble * Mathf.PerlinNoise(seed, time * 4f));
        transform.localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, -0.15f);
    }

    // Aperçu en édition (pas de mouvement) : dessine le ruban comme si le projectile avait volé `length` m selon son +z.
    public void PreviewPath(float length, float speed)
    {
        TrailRenderer renderer = GetComponent<TrailRenderer>();
        Transform parent = transform.parent;
        int count = Mathf.CeilToInt(length / segment);
        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float back = length * (count - 1 - i) / (count - 1);
            float t = -back / speed;
            float angle = phase + t * turnsPerSecond * Mathf.PI * 2f;
            float r = radius * (1f - wobble * 0.5f + wobble * Mathf.PerlinNoise(seed, t * 4f));
            points[i] = parent.TransformPoint(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, -0.15f - back));
        }
        renderer.Clear();
        renderer.AddPositions(points);
    }
}
