using UnityEngine;

// Braise de la traînée d'une boule de feu (FireballVisual) : un petit tétraèdre facetté, laissé sur place, qui monte
// un peu, tourne et rétrécit jusqu'à disparaître. Purement visuel et local.
public class FireballEmber : MonoBehaviour
{
    private float lifetime;
    private float age;
    private Vector3 startScale;
    private Vector3 spin;

    public void Init(float seconds)
    {
        lifetime = Mathf.Max(0.05f, seconds);
        startScale = transform.localScale;
        spin = Random.insideUnitSphere * 540f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float k = age / lifetime;
        if (k >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.localScale = startScale * (1f - k);
        transform.position += Vector3.up * (0.6f * Time.deltaTime);
        transform.Rotate(spin * Time.deltaTime, Space.Self);
    }
}
