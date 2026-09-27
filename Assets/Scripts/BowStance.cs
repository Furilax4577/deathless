using UnityEngine;

// Bascule repos <-> visée de l'arc (style arc + carquois). Contrairement à l'épée, l'arc a besoin de DEUX rotations
// locales dans handslot.l : au repos (branches le long du corps, corde vers le ciel) et en visée (corde côté archer).
// Piloté par le bool `Aiming` de l'Animator : interpolation en `blendSeconds` ; la flèche (handslot.r) n'est visible
// qu'en visée ; le blendshape `Draw` de l'arc suit l'état courant (Draw : monte avec le clip, Aiming_Idle : tendu, sinon 0).
public class BowStance : MonoBehaviour
{
    public Animator animator;
    public Transform bow;
    public GameObject arrow;
    public SkinnedMeshRenderer bowRenderer;
    [Tooltip("Rotation locale de l'arc dans handslot.l au repos (Idle_A, marche, course).")]
    public Vector3 restEuler = new Vector3(286f, 183f, 357f);
    [Tooltip("Rotation locale de l'arc dans handslot.l en visée (Ranged_Bow_Aiming_Idle / Draw / Release).")]
    public Vector3 aimEuler = new Vector3(0f, 0f, 180f);
    public float blendSeconds = 0.15f;
    public string aimingParam = "Aiming";
    public string drawState = "Draw";
    public string aimIdleState = "Aiming_Idle";

    private float amount; // 0 = repos, 1 = visée
    // Hachages des noms ci-dessus (Animator.StringToHash), calculés une fois au lieu de hacher les chaînes à chaque image.
    private int aimingHash, drawHash, aimIdleHash;

    private void Hacher()
    {
        aimingHash = Animator.StringToHash(aimingParam);
        drawHash = Animator.StringToHash(drawState);
        aimIdleHash = Animator.StringToHash(aimIdleState);
    }

    private void OnValidate() { Hacher(); }

    // Même règle que AnimatorStateInfo.IsName : nom court ou chemin complet (« Base Layer.Draw »).
    private static bool EstEtat(AnimatorStateInfo state, int hash)
    {
        return state.shortNameHash == hash || state.fullPathHash == hash;
    }

    private void Awake()
    {
        Hacher();
        if (animator == null) animator = GetComponent<Animator>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (bow == null && t.name == "bow_withString") bow = t;
            if (arrow == null && t.name == "arrow_bow") arrow = t.gameObject;
        }
        if (bowRenderer == null && bow != null) bowRenderer = bow.GetComponent<SkinnedMeshRenderer>();
        amount = animator != null && animator.GetBool(aimingHash) ? 1f : 0f;
        Apply();
    }

    private void Update()
    {
        if (animator == null || bow == null) return;
        bool aiming = animator.GetBool(aimingHash);
        amount = Mathf.MoveTowards(amount, aiming ? 1f : 0f, blendSeconds > 0f ? Time.deltaTime / blendSeconds : 1f);
        Apply();
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float draw = 0f;
        if (EstEtat(state, drawHash)) draw = Mathf.Clamp01(state.normalizedTime) * 100f;
        else if (EstEtat(state, aimIdleHash)) draw = 100f;
        if (bowRenderer != null) bowRenderer.SetBlendShapeWeight(0, draw);
    }

    private void Apply()
    {
        float k = Mathf.SmoothStep(0f, 1f, amount);
        if (bow != null) bow.localRotation = Quaternion.Slerp(Quaternion.Euler(restEuler), Quaternion.Euler(aimEuler), k);
        if (arrow != null && arrow.activeSelf != amount > 0.5f) arrow.SetActive(amount > 0.5f);
    }
}
