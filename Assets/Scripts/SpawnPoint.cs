using UnityEngine;

// Point d'apparition (clairière de spawn) : Transform vide, visible seulement dans l'éditeur (gizmo), rien en jeu.
// Posé par VillageBuilder à la place des anciennes dalles et capsules rouges (retirées le 25/09/2026, décision utilisateur).
public class SpawnPoint : MonoBehaviour
{
    public Color couleur = new Color(1f, 0.25f, 0.2f);
    public float rayon = 3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = couleur;
        Vector3 p = transform.position;
        const int n = 24;
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            Gizmos.DrawLine(p + new Vector3(Mathf.Cos(a0), 0.05f, Mathf.Sin(a0)) * rayon, p + new Vector3(Mathf.Cos(a1), 0.05f, Mathf.Sin(a1)) * rayon);
        }
        Gizmos.DrawWireSphere(p + Vector3.up * 1f, 0.4f);
        Gizmos.DrawLine(p + Vector3.up * 1f, p + Vector3.up * 1f + transform.forward * 2f);   // direction du village
    }
}
