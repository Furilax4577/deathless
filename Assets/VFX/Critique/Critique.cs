using UnityEngine;

// Coup critique (25/09/2026), retour visuel commun à toutes les classes, au point d'impact : éclat étoilé de gemmes
// claires et bref (rayons qui jaillissent puis s'éteignent par la taille), cœur blanc chaud, petit flash de lumière
// (VfxLumiere, thème Critique). Variante « meilleur critique » (assassin furtif + dans le dos) : plus de rayons, plus
// longs, deux couronnes, un anneau qui s'élargit, accent rouge-orangé, flash plus fort.
// API : Critique.Jouer(point, direction, meilleur) sur le composant (prefab Critique.prefab), ou la version statique
// Critique.Eclat(point, direction, meilleur, matériau). `direction` : sens du coup (les rayons partent autour).
public class Critique : MonoBehaviour
{
    [SerializeField] private Material materiau;

    public static Critique Instance { get; private set; }

    private void Awake() { if (Instance == null) Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Jouer(Vector3 point, Vector3 direction, bool meilleur = false)
    {
        Eclat(point, direction, meilleur, materiau);
    }

    // Critique au point, sans dépendance : ne fait rien si aucun composant Critique n'est dans la scène.
    public static void Signaler(Vector3 point, Vector3 direction, bool meilleur = false)
    {
        if (Instance != null) Instance.Jouer(point, direction, meilleur);
    }

    private static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Critique, r, d); }

    public static void Eclat(Vector3 point, Vector3 direction, bool meilleur, Material materiau)
    {
        if (materiau == null) return;
        Color vif = C(VfxRole.Vif, new Color(1f, 0.82f, 0.4f));
        Color coeur = C(VfxRole.Coeur, new Color(1f, 0.95f, 0.82f));
        Color baseC = C(VfxRole.Base, new Color(0.91f, 0.65f, 0.23f));
        Color accent = VfxPalette.Accent(VfxTheme.Critique, "Meilleur", new Color(1f, 0.35f, 0.24f));
        GemmesVolantes g = GemmesVolantes.Creer(meilleur ? "MeilleurCritique" : "Critique", materiau, meilleur ? 260 : 120, true);
        Vector3 d = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
        // L'éclat part devant la surface touchée (côté tireur), sinon la tête ou l'armure le cachent.
        point -= d * 0.35f;
        Vector3 u = Vector3.Cross(d, Vector3.up); if (u.sqrMagnitude < 1e-3f) u = Vector3.right; u.Normalize();
        Vector3 v = Vector3.Cross(u, d).normalized;
        float echelle = meilleur ? 1.6f : 1f;
        int rayons = meilleur ? 12 : 7;
        for (int couronne = 0; couronne < (meilleur ? 2 : 1); couronne++)
        {
            float decal = couronne * Mathf.PI / rayons;
            for (int r = 0; r < rayons; r++)
            {
                // Rayons dans le plan perpendiculaire au coup, un peu vers l'arrière (on voit l'étoile de face).
                float a = decal + r * Mathf.PI * 2f / rayons + Random.Range(-0.1f, 0.1f);
                Vector3 dir = (u * Mathf.Cos(a) + v * Mathf.Sin(a) - d * 0.25f).normalized;
                float vitesse = Random.Range(5f, 7f) * echelle * (couronne == 0 ? 1f : 0.7f);
                for (int k = 0; k < 5; k++)
                {
                    // Chaque rayon : gemmes étirées le long de sa direction, la pointe part la première.
                    float f = 1f - k * 0.17f;
                    Color c = k == 0 ? coeur * 1.6f : Color.Lerp(vif * 1.3f, meilleur && couronne == 1 ? accent * 1.3f : baseC, k / 4f);
                    g.Emettre(point, dir * vitesse * f, 0.075f * echelle * (1.2f - k * 0.12f), 0.34f * (meilleur ? 1.35f : 1f), c,
                        0f, 7f, 0.02f, 0.25f, new Vector3(0.45f, 0.45f, 1.8f));
                }
            }
        }
        // Cœur : quelques gemmes blanc chaud qui gonflent et s'éteignent sur place.
        for (int k = 0; k < (meilleur ? 14 : 8); k++)
            g.Emettre(point + Random.insideUnitSphere * 0.05f, Random.insideUnitSphere * 0.4f, Random.Range(0.08f, 0.13f) * echelle, 0.28f,
                coeur * 1.8f, 0f, 3f, 0.03f, 0.3f);
        // Meilleur critique : anneau d'accent qui s'élargit dans le plan de l'étoile.
        if (meilleur)
            for (int k = 0; k < 36; k++)
            {
                float a = k * Mathf.PI * 2f / 36f;
                Vector3 dir = u * Mathf.Cos(a) + v * Mathf.Sin(a);
                g.Emettre(point + dir * 0.1f, dir * 4.5f, 0.045f, 0.4f, accent * 1.4f, 0f, 5f, 0.02f, 0.35f);
            }
        VfxLumiere.Eclat(point, VfxTheme.Critique, meilleur ? VfxTailleLumiere.Moyenne : VfxTailleLumiere.Petite, meilleur ? 0.12f : 0.04f);
    }
}
