using System.Collections;
using UnityEngine;

// Bac à sable : remplace Relic/Assets/Scripts/RelicShield.cs (NetworkBehaviour à SyncVar, non copiable) pour le visuel.
// Même API lue par RelicShieldVisual (IsUp, IsCasting, Health, MaxHealth, Radius, Height, CastSeconds, BasePosition,
// LifeRatio, LastHitTime, LastHitPoint, LifeTint) et mêmes valeurs que GameBalance.asset de Relic : rayon 5,5 m, hauteur
// 6 m, incantation 3 s, vie 300 (niveau 1), seuils orange 0,4 et rouge 0,15, GroundOffset 0,8 (l'objet RelicShield est à
// y = 0,8 dans la scène). Lever(), Frapper() et Baisser() remplacent le serveur.
public class RelicShieldEtat : MonoBehaviour
{
    private const float GroundOffset = 0.8f;
    private const float TintFadeWidth = 0.05f;

    public float radius = 5.5f;
    public float height = 6f;
    public float castSeconds = 3f;
    public float maxHealth = 300f;
    [Range(0f, 1f)] public float warnRatio = 0.4f;
    [Range(0f, 1f)] public float criticalRatio = 0.15f;

    public bool IsCasting { get; private set; }
    public bool IsUp { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public float Radius => radius;
    public float Height => height;
    public float CastSeconds => Mathf.Max(0.3f, castSeconds);
    public Vector3 BasePosition => transform.position - Vector3.up * GroundOffset;
    public float LifeRatio => IsUp && MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 1f;
    public float LastHitTime { get; private set; } = float.NegativeInfinity;
    public Vector3 LastHitPoint { get; private set; }

    // Couleur selon la vie restante : bleu au-dessus de `warnRatio`, orange jusqu'à `criticalRatio`, rouge en dessous,
    // avec un fondu doux autour de chaque seuil (copie de RelicShield.LifeTint).
    public Color LifeTint(Color blue, Color orange, Color red)
    {
        float ratio = LifeRatio;
        float toOrange = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(warnRatio + TintFadeWidth, warnRatio - TintFadeWidth, ratio));
        float toRed = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(criticalRatio + TintFadeWidth, criticalRatio - TintFadeWidth, ratio));
        return Color.Lerp(Color.Lerp(blue, orange, toOrange), red, toRed);
    }

    // Incantation (le mage lève le bouclier) : `castSeconds` puis le bouclier est levé à pleine vie.
    public void Lever()
    {
        if (IsUp || IsCasting)
            return;
        StartCoroutine(Incantation());
    }

    private IEnumerator Incantation()
    {
        IsCasting = true;
        yield return new WaitForSeconds(CastSeconds);
        IsCasting = false;
        IsUp = true;
        Health = maxHealth;
    }

    // Coup reçu (RelicShield.TakeDamage + HitRpc) : à zéro, le bouclier se brise (BreakRpc → RelicShieldVisual.Shatter).
    public void Frapper(float amount, Vector3 point)
    {
        if (!IsUp || amount <= 0f)
            return;
        Health = Mathf.Max(0f, Health - amount);
        LastHitTime = Time.time;
        LastHitPoint = point;
        if (Health <= 0f)
        {
            IsUp = false;
            RelicShieldVisual visual = GetComponent<RelicShieldVisual>();
            if (visual != null)
                visual.Shatter();
        }
    }

    // L'aube : le bouclier redescend dans le sol.
    public void Baisser()
    {
        StopAllCoroutines();
        IsCasting = false;
        IsUp = false;
    }
}
