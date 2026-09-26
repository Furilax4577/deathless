using UnityEngine;

// Forgeron du village (Quentin, 26/09/2026) : debout à sa place dans la forge (Ancre_Villageois_Forgeron), face à
// l'enclume, il bat le fer à un rythme calme : une série de 2 à 4 coups de marteau (attaque à une main
// Melee_1H_Attack_Chop des KayKit Character Animations), puis une pause, le marteau posé sur l'enclume. À chaque impact : courte gerbe
// d'étincelles (gemmes low poly de la palette Feu, sans lumière) et un coup de marteau sur l'enclume (son 3D forge_enclume :
// une des 3 variantes synthétisées, tirée au hasard ; aucun autre son). Jour et nuit, sans interaction ni réseau
// (chaque poste le joue pour lui). Posé par ForgeronBuilder (Deathless > Niveau > Forgeron, relancé par Intérieurs).
// Contact (retour de Quentin, 26/09/2026 : « le marteau doit frapper l'enclume et pas rentrer dedans ») : le geste
// s'arrête à l'instant où la tête du marteau touche la table de l'enclume (mesuré par ForgeronBuilder) ; le test se
// fait en LateUpdate, après l'évaluation de l'Animator, et la pose est recalée sur le contact avant le rendu : aucune
// image ne montre le marteau plus bas. La pose posée est l'état Contact du contrôleur (même clip, vitesse 0) : le
// fondu vers le geste part d'une pose figée, pas d'un geste qui continuerait à descendre, et rejoint le clip à
// `depart` (marteau déjà relevé) : partir du tout début ramènerait la tête à travers la table.
public class ForgeronForge : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Tête du marteau (point d'impact).")]
    public Transform teteMarteau;
    [Tooltip("Dessus de l'enclume, au point frappé (monde).")]
    public Vector3 pointEnclume;
    [Tooltip("Instant de l'impact dans le clip de frappe (0-1), mesuré par ForgeronBuilder.")]
    [Range(0f, 1f)] public float impact = 0.45f;
    [Tooltip("Instant du clip (s) où repart le geste après la pose posée, mesuré par ForgeronBuilder : le marteau quitte la table en montant.")]
    public float depart = 0f;
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
    static readonly int S_Contact = Animator.StringToHash("Contact");

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

    // Pose de repos : marteau posé sur l'enclume (pose de contact du geste, figée : état Contact, vitesse 0).
    void Poser()
    {
        if (animator == null) return;
        animator.speed = vitesse;
        animator.Play(S_Contact, 0, impact);
        animator.Update(0f);
    }

    void Update()
    {
        if (animator == null || m_Frappe || Time.time < m_Prochain) return;
        if (m_Restants <= 0) m_Restants = Random.Range(coupsParSerie.x, coupsParSerie.y + 1);
        // Il relève le marteau (fondu de la pose posée vers le début du geste), puis frappe.
        m_Frappe = true; m_Depart = Time.time;
        animator.speed = vitesse;
        animator.CrossFadeInFixedTime(S_Frappe, 0.3f, 0, depart);
    }

    // Après l'Animator : dès que le geste atteint (ou dépasse) l'instant du contact, la pose est recalée exactement sur
    // le contact, dans la même image ; il la garde jusqu'au coup suivant.
    void LateUpdate()
    {
        if (animator == null || !m_Frappe) return;
        var e = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
        bool contact = e.shortNameHash == S_Frappe && e.normalizedTime >= impact;
        if (!contact && Time.time - m_Depart < 4f) return;
        m_Frappe = false;
        Poser();
        if (!contact) return;   // sécurité : geste perdu, pose posée sans coup
        m_Restants--;
        m_Prochain = Time.time + (m_Restants > 0 ? Random.Range(entreCoups.x, entreCoups.y) : Random.Range(entreSeries.x, entreSeries.y));
        Frapper();
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
        Deathless.Jeu.AudioBank.Jouer(Deathless.Jeu.SonsDuJeu.ForgeEnclume, p, volume, 0.08f);
    }
}
