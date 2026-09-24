using UnityEngine;

// Switch entre l'arme principale (dague, handslot.r) et l'arme alternative (arbalète, rangée dans le dos sur `chest`).
// Piloté par le bool `Crossbow` de l'Animator : vrai = arbalète en main droite et dague rangée à la ceinture (`hips`),
// faux = dague en main et arbalète dans le dos. Chaque pièce est reparentée puis glisse vers sa pose locale cible
// en `blendSeconds`. Le cooldown du switch n'est pas géré ici (projet principal).
public class AltWeaponSwitch : MonoBehaviour
{
    public Animator animator;
    public string crossbowParam = "Crossbow";
    public float blendSeconds = 0.15f;

    [Header("Pièces (retrouvées par nom si vides)")]
    public Transform mainWeapon;   // dagger
    public Transform altWeapon;    // crossbow_1handed
    public string handBone = "handslot.r";
    public string backBone = "chest";
    public string sheathBone = "hips";

    [Header("Dague : en main / à la ceinture")]
    public Vector3 mainHandPosition;
    public Vector3 mainHandEuler;
    public Vector3 sheathPosition;
    public Vector3 sheathEuler;

    [Header("Arbalète : en main / dans le dos")]
    public Vector3 altHandPosition;
    public Vector3 altHandEuler;
    public Vector3 backPosition;
    public Vector3 backEuler;

    private bool crossbowOut;
    private float t = 1f;
    private Vector3 mainFromPos, altFromPos;
    private Quaternion mainFromRot, altFromRot;

    private Transform FindBone(string n)
    {
        foreach (Transform x in GetComponentsInChildren<Transform>(true))
            if (x.name == n) return x;
        return null;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (mainWeapon == null) mainWeapon = FindBone("dagger");
        if (altWeapon == null) altWeapon = FindBone("crossbow_1handed");
        crossbowOut = animator != null && animator.GetBool(crossbowParam);
        Place(crossbowOut, true);
    }

    private void Update()
    {
        if (animator == null) return;
        bool want = animator.GetBool(crossbowParam);
        if (want != crossbowOut) { crossbowOut = want; Place(want, false); }
        if (t < 1f)
        {
            t = blendSeconds > 0f ? Mathf.Min(1f, t + Time.deltaTime / blendSeconds) : 1f;
            Blend(want, Mathf.SmoothStep(0f, 1f, t));
        }
    }

    // Pose immédiate dans l'état voulu (utilisable aussi hors Play mode, depuis l'éditeur).
    public void Place(bool crossbow, bool instant)
    {
        Reparent(mainWeapon, FindBone(crossbow ? sheathBone : handBone), out mainFromPos, out mainFromRot);
        Reparent(altWeapon, FindBone(crossbow ? handBone : backBone), out altFromPos, out altFromRot);
        crossbowOut = crossbow;
        t = instant ? 1f : 0f;
        if (instant) Blend(crossbow, 1f);
    }

    private void Blend(bool crossbow, float k)
    {
        Lerp(mainWeapon, mainFromPos, mainFromRot, crossbow ? sheathPosition : mainHandPosition, crossbow ? sheathEuler : mainHandEuler, k);
        Lerp(altWeapon, altFromPos, altFromRot, crossbow ? altHandPosition : backPosition, crossbow ? altHandEuler : backEuler, k);
    }

    private static void Reparent(Transform item, Transform bone, out Vector3 fromPos, out Quaternion fromRot)
    {
        fromPos = Vector3.zero; fromRot = Quaternion.identity;
        if (item == null || bone == null) return;
        item.SetParent(bone, true);
        fromPos = item.localPosition; fromRot = item.localRotation;
    }

    private static void Lerp(Transform item, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Vector3 toEuler, float k)
    {
        if (item == null) return;
        item.localPosition = Vector3.Lerp(fromPos, toPos, k);
        item.localRotation = Quaternion.Slerp(fromRot, Quaternion.Euler(toEuler), k);
    }
}
