using UnityEngine;

// Éclat de la parade réussie du paladin (26/09/2026) : bref éclat de gemmes au point de contact sur le bouclier, quand
// la garde (LT maintenu) intercepte le coup dans sa fenêtre de parade (ClassePaladin.SurIntercepte, Interception.Pare,
// dans main). Distinct de l'étourdissement de l'ennemi (Etourdissement : étoiles qui tournent au-dessus de sa tête,
// couleurs de l'effet qui étourdit) : ici l'éclat naît sur le bouclier du paladin lui-même, toujours or et blanc
// (thème Sacre), et n'étourdit personne. Langage gemmes (LowPolyGem, Relic/VertexColorUnlit, pas d'alpha) : apparition
// et disparition par la taille. Petit flash de lumière (VfxLumiere). 0,3 à 0,4 s en tout, lisible sans masquer le combat.
// API : ParadeEclat.Instance.Jouer(point, normale) si une instance existe dans la scène (comme Critique.prefab),
// sinon la version statique ParadeEclat.Eclat(point, normale, matériau) (voir Combat.Critique dans main pour l'exemple
// de repli sur EffetsJeu.Gemmes).
public class ParadeEclat : MonoBehaviour
{
    [SerializeField] private Material materiau;

    public static ParadeEclat Instance { get; private set; }

    private void Awake() { if (Instance == null) Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Jouer(Vector3 point, Vector3 normale)
    {
        Eclat(point, normale, materiau);
    }

    private static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Sacre, r, d); }

    // `normale` : direction vers l'extérieur du bouclier (vers l'attaquant), sens dans lequel l'éclat jaillit.
    public static void Eclat(Vector3 point, Vector3 normale, Material materiau)
    {
        if (materiau == null) return;
        Color baseC = C(VfxRole.Base, new Color(0.722f, 0.565f, 0.227f));
        Color vif = C(VfxRole.Vif, new Color(0.910f, 0.784f, 0.447f));
        Color coeur = C(VfxRole.Coeur, new Color(0.957f, 0.886f, 0.659f));
        Vector3 n = normale.sqrMagnitude > 1e-4f ? normale.normalized : Vector3.up;
        // L'éclat naît à la surface du bouclier, pas dedans.
        point += n * 0.08f;
        Vector3 u = Vector3.Cross(n, Vector3.up); if (u.sqrMagnitude < 1e-3f) u = Vector3.right; u.Normalize();
        Vector3 v = Vector3.Cross(u, n).normalized;

        GemmesVolantes g = GemmesVolantes.Creer("ParadeEclat", materiau, 90, true);

        // Étoile compacte : six courts rayons qui jaillissent vers l'avant du bouclier et s'éteignent vite.
        const int rayons = 6;
        for (int r = 0; r < rayons; r++)
        {
            float a = r * Mathf.PI * 2f / rayons + Random.Range(-0.08f, 0.08f);
            Vector3 dirPlan = u * Mathf.Cos(a) + v * Mathf.Sin(a);
            Vector3 dir = (dirPlan * 0.8f + n * 0.6f).normalized;
            float vitesse = Random.Range(3.5f, 5f);
            for (int k = 0; k < 3; k++)
            {
                float f = 1f - k * 0.22f;
                Color c = k == 0 ? coeur * 1.5f : Color.Lerp(vif * 1.2f, baseC, k / 2f);
                g.Emettre(point, dir * vitesse * f, 0.05f * (1.15f - k * 0.15f), 0.3f, c, 0f, 6f, 0.015f, 0.22f,
                    new Vector3(0.5f, 0.5f, 1.6f));
            }
        }
        // Cœur : quelques gemmes blanc chaud qui gonflent sur le point de contact.
        for (int k = 0; k < 6; k++)
            g.Emettre(point + Random.insideUnitSphere * 0.03f, n * Random.Range(0.3f, 0.8f) + Random.insideUnitSphere * 0.2f,
                Random.Range(0.05f, 0.08f), 0.24f, coeur * 1.7f, 0f, 4f, 0.02f, 0.3f);
        // Fin cercle sur la surface (ping du bouclier), qui s'élargit puis s'éteint.
        const int anneau = 14;
        for (int k = 0; k < anneau; k++)
        {
            float a = k * Mathf.PI * 2f / anneau;
            Vector3 dir = u * Mathf.Cos(a) + v * Mathf.Sin(a);
            g.Emettre(point + dir * 0.05f, dir * 2.2f, 0.03f, 0.34f, vif * 1.1f, 0f, 5f, 0.02f, 0.35f);
        }
        VfxLumiere.Eclat(point, VfxTheme.Sacre, VfxTailleLumiere.Petite, 0.03f);
    }
}
