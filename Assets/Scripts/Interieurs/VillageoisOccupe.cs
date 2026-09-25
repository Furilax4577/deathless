using UnityEngine;

// Villageois à son poste (tavernier…) : au repos (état « Repos »), il fait de temps en temps un geste (état « Geste » :
// essuyer le comptoir, servir), puis revient au repos. Purement visuel, sans réseau (chaque poste le joue pour lui).
// Posé par TavernierBuilder (Deathless > Niveau > Intérieurs).
public class VillageoisOccupe : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Pause entre deux gestes (s).")]
    public Vector2 entreGestes = new Vector2(4f, 9f);

    static readonly int S_Repos = Animator.StringToHash("Repos");
    static readonly int S_Geste = Animator.StringToHash("Geste");
    float m_Prochain;
    bool m_Geste;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        m_Prochain = Time.time + Random.Range(1f, entreGestes.y);
    }

    void Update()
    {
        if (animator == null) return;
        if (!m_Geste)
        {
            if (Time.time < m_Prochain) return;
            m_Geste = true;
            animator.CrossFadeInFixedTime(S_Geste, 0.25f, 0, 0f);
            return;
        }
        var e = animator.GetCurrentAnimatorStateInfo(0);
        if (!animator.IsInTransition(0) && e.shortNameHash == S_Geste && e.normalizedTime >= 0.95f)
        {
            m_Geste = false;
            animator.CrossFadeInFixedTime(S_Repos, 0.3f, 0, 0f);
            m_Prochain = Time.time + Random.Range(entreGestes.x, entreGestes.y);
        }
    }
}
