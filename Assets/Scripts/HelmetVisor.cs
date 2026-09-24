using UnityEngine;

// Visière articulée du casque du chevalier (Knight.fbx). Dans le FBX, `Knight_HelmetVisor` est un SkinnedMeshRenderer
// séparé, entièrement lié à l'os `head`, modélisé visière RELEVÉE. On ne découpe rien : on remplace l'os `head` par une
// charnière `VisorHinge` (enfant de `head`) dans `SkinnedMeshRenderer.bones`, et on fait pivoter la charnière autour de
// l'axe des tempes pour abaisser la visière (même mécanisme que HeadGear.cs dans Relic).
// `open` = relevée (répit), sinon abaissée (combat) ; interpolation en `seconds`.
public class HelmetVisor : MonoBehaviour
{
    [Tooltip("Vrai = visière relevée (répit), faux = abaissée (combat).")]
    public bool open = true;
    public float seconds = 0.2f;
    [Tooltip("Suffixe du renderer de la visière et nom de l'os de la tête.")]
    public string visorSuffix = "HelmetVisor";
    public string headBone = "head";
    [Tooltip("Charnière dans le repère de l'os head (m) : rivets des tempes.")]
    public Vector3 hingeInHead = new Vector3(0f, 0.548f, 0.07f);
    [Tooltip("Axe de rotation dans le repère de l'os head (axe des tempes).")]
    public Vector3 axisInHead = Vector3.right;
    [Tooltip("Angle de la visière abaissée (relevée = 0, comme modélisée).")]
    public float closedAngle = 41f;

    private Transform hinge;
    private float amount; // 0 = relevée, 1 = abaissée

    private void Awake() { Build(); amount = open ? 0f : 1f; Apply(); }

    private void Update()
    {
        if (hinge == null) return;
        amount = Mathf.MoveTowards(amount, open ? 0f : 1f, seconds > 0f ? Time.deltaTime / seconds : 1f);
        Apply();
    }

    // Substitue la charnière à l'os head dans le skin de la visière (réexécutable).
    public void Build()
    {
        SkinnedMeshRenderer visor = null; Transform head = null;
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.name.EndsWith(visorSuffix)) visor = smr;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == headBone) head = t;
        if (visor == null || head == null) return;
        Transform existing = head.Find("VisorHinge");
        hinge = existing != null ? existing : new GameObject("VisorHinge").transform;
        hinge.SetParent(head, false);
        Transform[] bones = visor.bones;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] == head) bones[i] = hinge;
        visor.bones = bones;
    }

    // Pose immédiate (0 = relevée, 1 = abaissée), utilisable hors Play mode.
    public void SetAmount(float value)
    {
        if (hinge == null) Build();
        amount = Mathf.Clamp01(value);
        Apply();
    }

    private void Apply()
    {
        if (hinge == null) return;
        Quaternion rotation = Quaternion.AngleAxis(closedAngle * Mathf.SmoothStep(0f, 1f, amount), axisInHead.normalized);
        hinge.localRotation = rotation;
        hinge.localPosition = hingeInHead - rotation * hingeInHead;
    }
}
