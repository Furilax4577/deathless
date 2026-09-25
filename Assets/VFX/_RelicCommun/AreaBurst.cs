using UnityEngine;

// Onde du sort de zone du mage : un disque plat cyan qui s'étend jusqu'au rayon du sort puis s'efface.
// Purement visuel et local, créé chez chaque client par un ObserversRpc (PlayerWeapon.AreaEffectRpc).
public class AreaBurst : MonoBehaviour
{
    private float radius;
    private float duration = 0.6f;
    private float elapsed;
    private Material material;

    public static void Spawn(Vector3 position, float radius, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(go.GetComponent<Collider>());
        go.name = "AreaBurst";
        go.transform.position = position + Vector3.up * 0.1f;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AreaBurst burst = go.AddComponent<AreaBurst>();
        burst.radius = radius;
        burst.material = material;
        go.transform.localScale = new Vector3(0.2f, 0.05f, 0.2f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(elapsed / duration);
        float r = Mathf.Lerp(0.2f, radius, 1f - (1f - k) * (1f - k));
        transform.localScale = new Vector3(r * 2f, 0.05f, r * 2f);
        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
