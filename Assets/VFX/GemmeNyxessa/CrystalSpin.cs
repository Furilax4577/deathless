using UnityEngine;

// Cristal d'énergie flottant (relique, poste de construction) : rotation lente et léger balancement vertical.
// Purement visuel et local : rien n'est envoyé sur le réseau, chaque machine anime le sien.
public class CrystalSpin : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 40f;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobPeriod = 3f;

    private Vector3 restPosition;
    [Tooltip("Multiplicateur de la vitesse de rotation (réactions de la relique : Nyxessa).")]
    public float multiplicateur = 1f;
    [Tooltip("Décalage local ajouté à la position (recul du cristal au tir : Nyxessa).")]
    public Vector3 decalage;

    private void Awake()
    {
        restPosition = transform.localPosition;
    }

    private void Update()
    {
        transform.Rotate(0f, degreesPerSecond * multiplicateur * Time.deltaTime, 0f, Space.Self);
        float bob = Mathf.Sin(Time.time * Mathf.PI * 2f / bobPeriod) * bobAmplitude;
        transform.localPosition = restPosition + Vector3.up * bob + decalage;
    }
}
