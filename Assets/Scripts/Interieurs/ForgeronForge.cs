using UnityEngine;

// Forgeron du village (Quentin, 26/09/2026) : debout à sa place dans la forge (Ancre_Villageois_Forgeron), face à
// l'enclume, il bat le fer à un rythme calme : une série de 2 à 4 coups de marteau (attaque à une main
// Melee_1H_Attack_Chop des KayKit Character Animations), puis une pause, le marteau posé sur l'enclume. À chaque impact : courte gerbe
// d'étincelles (gemmes low poly de la palette Feu, sans lumière) et un tintement discret spatialisé. Jour et nuit,
// sans interaction ni réseau (chaque poste le joue pour lui). Posé par ForgeronBuilder (Deathless > Niveau > Intérieurs).
public class ForgeronForge : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Tête du marteau (point d'impact).")]
    public Transform teteMarteau;
    [Tooltip("Dessus de l'enclume, au point frappé (monde).")]
    public Vector3 pointEnclume;
    [Tooltip("Instant de l'impact dans le clip de frappe (0-1), mesuré par ForgeronBuilder.")]
    [Range(0f, 1f)] public float impact = 0.45f;
    [Tooltip("Vitesse de la frappe (1 : vitesse du clip).")]
    public float vitesse = 0.85f;
    public Vector2Int coupsParSerie = new Vector2Int(2, 4);
    [Tooltip("Pause entre deux coups d'une série (s).")]
    public Vector2 entreCoups = new Vector2(0.25f, 0.55f);
    [Tooltip("Pause entre deux séries (s).")]
    public Vector2 entreSeries = new Vector2(1.8f, 3.6f);
    public int etincelles = 12;
    public float volume = 0.28f;

    static readonly int S_Frappe = Animator.StringToHash("Frappe");
    static readonly string[] Son = { "kenney_rpg_metalpot" };

    /// Instant du dernier coup (Time.time), pour les tests et captures.
    public float DernierImpact { get; private set; } = -1f;

    GemmesVolantes m_Gemmes;
    float m_Prochain;
    int m_Restants;
    bool m_Frappe;
    float m_Depart;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        m_Prochain = Time.time + Random.Range(0.5f, 2f);
        Poser();
    }

    void OnDestroy() { if (m_Gemmes != null) Destroy(m_Gemmes.gameObject); }

    // Pose de repos : marteau posé sur l'enclume (pose de contact du geste, figée).
    void Poser()
    {
        if (animator == null) return;
        animator.Play(S_Frappe, 0, impact);
        animator.speed = 0f;
        animator.Update(0f);
    }

    void Update()
    {
        if (animator == null) return;
        if (!m_Frappe)
        {
            if (Time.time < m_Prochain) return;
            if (m_Restants <= 0) m_Restants = Random.Range(coupsParSerie.x, coupsParSerie.y + 1);
            // Il relève le marteau (fondu de la pose posée vers le début du geste), puis frappe.
            m_Frappe = true; m_Depart = Time.time;
            animator.speed = vitesse;
            animator.CrossFadeInFixedTime(S_Frappe, 0.3f, 0, 0f);
            return;
        }
        var e = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0) || e.normalizedTime > impact + 0.2f) { if (Time.time - m_Depart > 4f) { m_Frappe = false; Poser(); } return; }
        if (e.normalizedTime >= impact)
        {
            // Contact : la pose est recalée exactement sur l'instant du contact (le geste du clip descend plus bas que
            // l'enclume : sans arrêt, le marteau la traverserait), et il la garde jusqu'au coup suivant.
            m_Frappe = false;
            m_Restants--;
            Poser();
            m_Prochain = Time.time + (m_Restants > 0 ? Random.Range(entreCoups.x, entreCoups.y) : Random.Range(entreSeries.x, entreSeries.y));
            Frapper();
        }
    }

    void Frapper()
    {
        DernierImpact = Time.time;
        Vector3 p = pointEnclume;
        if (teteMarteau != null) { Vector3 t = teteMarteau.position; p = new Vector3(t.x, pointEnclume.y, t.z); }
        var mat = Deathless.Jeu.EffetsJeu.Gemmes;
        if (mat != null)
        {
            if (m_Gemmes == null) m_Gemmes = GemmesVolantes.Creer("Forgeron_Etincelles", mat, 64);
            int n = Random.Range(etincelles - 3, etincelles + 3);
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Vector3 v = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(0.8f, 2.2f) + Vector3.up * Random.Range(1.2f, 2.8f);
                float r = Random.value;
                Color c = r < 0.55f ? VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, new Color(1f, 0.38f, 0.04f))
                        : r < 0.9f ? VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Coeur, new Color(1f, 0.9f, 0.4f))
                        : VfxPalette.Accent(VfxTheme.Feu, "Blanc chaud", new Color(1f, 0.95f, 0.8f));
                m_Gemmes.Emettre(p + Vector3.up * 0.02f, v, Random.Range(0.024f, 0.04f), Random.Range(0.3f, 0.55f), c, 9f, 1.5f, 0.02f, 0.4f);
            }
        }
        if (Deathless.Jeu.AudioBank.Instance != null) Deathless.Jeu.AudioBank.Jouer(Son, p, volume, 0.08f);
    }
}
