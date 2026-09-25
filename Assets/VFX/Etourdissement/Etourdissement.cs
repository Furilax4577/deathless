using UnityEngine;

// Indicateur d'étourdissement (25/09/2026) : quelques étoiles en gemmes (deux gemmes plates croisées : une étoile à
// quatre branches) qui tournent au-dessus de la tête de la cible pendant `duree` s, avec un léger balancement. Pop
// d'apparition 0,15 s, disparition par la taille les 0,25 dernières secondes. Suit la cible. Pas de lumière.
// Version courte (`court`) : 3 étoiles plus petites, cercle plus serré (ennemis repoussés par la charge bélier) ;
// version pleine : 5 étoiles (cible de l'impact). Couleurs : thème de l'effet qui étourdit (Sacre par défaut).
// API : Etourdissement.Jouer(cible, durée, court, matériau, thème) ; le composant se détruit seul.
public class Etourdissement : MonoBehaviour
{
    private Transform cible;
    private float duree, debut, hauteur, rayon, taille;
    private int etoiles;
    private Color clair, vif;
    private Mesh mesh;
    private Vector3[] v;
    private Color[] c;

    public static Etourdissement Jouer(Transform cible, float duree, bool court, Material materiau, VfxTheme theme = VfxTheme.Sacre)
    {
        if (cible == null || materiau == null) return null;
        // Un seul indicateur par cible : le nouveau remplace l'ancien.
        foreach (Etourdissement e in FindObjectsByType<Etourdissement>(FindObjectsSortMode.None))
            if (e.cible == cible) Destroy(e.gameObject);
        GameObject go = new GameObject("Etourdissement");
        Etourdissement t = go.AddComponent<Etourdissement>();
        t.cible = cible;
        t.duree = Mathf.Max(0.3f, duree);
        t.debut = Time.time;
        t.etoiles = court ? 3 : 5;
        t.rayon = court ? 0.28f : 0.4f;
        t.taille = court ? 0.09f : 0.12f;
        // Hauteur : juste au-dessus du sommet des rendus de la cible (casque compris).
        float haut = cible.position.y + 1.8f;
        bool premier = true;
        foreach (Renderer r in cible.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (premier) { haut = r.bounds.max.y; premier = false; }
            else haut = Mathf.Max(haut, r.bounds.max.y);
        }
        t.hauteur = haut - cible.position.y + 0.18f;
        t.clair = VfxPalette.Couleur(theme, VfxRole.Coeur, new Color(0.957f, 0.886f, 0.659f));
        t.vif = VfxPalette.Couleur(theme, VfxRole.Vif, new Color(0.91f, 0.784f, 0.447f));
        t.Construire(materiau);
        return t;
    }

    private void Construire(Material materiau)
    {
        mesh = new Mesh { name = "Etourdissement" };
        mesh.MarkDynamic();
        int n = etoiles * 2;
        v = new Vector3[n * LowPolyGem.VerticesPerGem];
        c = new Color[v.Length];
        mesh.vertices = v;
        mesh.colors = c;
        mesh.triangles = LowPolyGem.Triangles(n);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    private void LateUpdate()
    {
        float age = Time.time - debut;
        if (cible == null || age >= duree)
        {
            Destroy(gameObject);
            return;
        }
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.15f)) * Mathf.Clamp01((duree - age) / 0.25f);
        Vector3 centre = cible.position + Vector3.up * hauteur;
        for (int i = 0; i < etoiles; i++)
        {
            float a = (age * 220f + i * 360f / etoiles) * Mathf.Deg2Rad;
            Vector3 p = centre + new Vector3(Mathf.Cos(a) * rayon, Mathf.Sin(age * 5f + i * 1.7f) * 0.05f, Mathf.Sin(a) * rayon);
            // Étoile à quatre branches : deux gemmes plates, allongées, croisées dans un plan vertical, qui tournent sur elles-mêmes.
            Quaternion r = Quaternion.Euler(0f, age * 300f + i * 40f, 0f);
            Color col = i % 2 == 0 ? clair * 1.2f : vif * 1.15f;
            LowPolyGem.Write(v, c, i * 2, p, taille * s, new Vector3(0.35f, 1.6f, 0.25f), r, col, LowPolyGem.DefaultLight);
            LowPolyGem.Write(v, c, i * 2 + 1, p, taille * s, new Vector3(1.6f, 0.35f, 0.25f), r, col, LowPolyGem.DefaultLight);
        }
        mesh.vertices = v;
        mesh.colors = c;
        mesh.RecalculateBounds();
    }
}
