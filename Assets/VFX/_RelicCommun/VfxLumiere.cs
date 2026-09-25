using UnityEngine;

// Classe de taille d'une lumière d'effet (intensité et portée) : même éclairage pour des effets de même taille.
public enum VfxTailleLumiere { Petite, Moyenne, Grande }

// Lumière commune des effets visuels (25/09/2026) : une lumière ponctuelle sans ombre dont
// - la couleur vient du thème de l'effet (VfxPalette : mi-chemin entre les rôles vif et cœur, ou cœur seul s'il n'y a
//   pas de vif), sauf couleur imposée (états du bouclier) ;
// - l'intensité et la portée viennent de la classe de taille (préréglages ci-dessous) ;
// - l'enveloppe temporelle est la même pour tous : montée rapide (Montee), maintien, extinction douce (Extinction) ;
//   scintillement léger pour le thème Feu seulement.
// `facteur` module l'intensité de l'extérieur (ouverture du portail, réactions de la relique, coups sur le bouclier).
// Usage : VfxLumiere.Creer(parent, position, thème, taille, maintien) pour une lumière qui suit un objet,
// VfxLumiere.Eclat(position, thème, taille, maintien) pour un éclat d'impact, ou le composant posé sur un prefab
// (avec `suivreParticules` : allumée tant que les ParticleSystem enfants émettent).
[DisallowMultipleComponent]
public class VfxLumiere : MonoBehaviour
{
    public const float Montee = 0.08f;
    public const float Extinction = 0.4f;

    // Préréglages (intensité, portée en m) par classe de taille.
    public static float Intensite(VfxTailleLumiere t) { return t == VfxTailleLumiere.Petite ? 1.5f : t == VfxTailleLumiere.Moyenne ? 3f : 6f; }
    public static float Portee(VfxTailleLumiere t) { return t == VfxTailleLumiere.Petite ? 3.5f : t == VfxTailleLumiere.Moyenne ? 6f : 11f; }

    public VfxTheme theme = VfxTheme.Nyxessa;
    public VfxTailleLumiere taille = VfxTailleLumiere.Moyenne;
    [Tooltip("Allumée au démarrage et tenue (lumière permanente : relique, portail).")]
    public bool allumerAuDemarrage;
    [Tooltip("Allumée tant qu'un ParticleSystem enfant émet (cône, flammèches).")]
    public bool suivreParticules;
    [Tooltip("Modulation externe de l'intensité.")]
    public float facteur = 1f;
    [Tooltip("Couleur imposée au lieu de celle du thème.")]
    public bool couleurImposee;
    public Color couleur = Color.white;

    private Light lum;
    private float debut = -100f;
    private float finMaintien = float.PositiveInfinity;
    private float debutExtinction = float.PositiveInfinity;
    private bool detruireApres;
    private bool allumee;
    private float graine;
    private ParticleSystem[] particules;

    public Light Lumiere { get { Assurer(); return lum; } }
    public bool Allumee { get { return allumee; } }

    // Lumière qui suit `parent` ; `maintien` < 0 : tenue jusqu'à Eteindre().
    public static VfxLumiere Creer(Transform parent, Vector3 positionLocale, VfxTheme theme, VfxTailleLumiere taille, float maintien = -1f)
    {
        GameObject go = new GameObject("VfxLumiere");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = positionLocale;
        VfxLumiere l = go.AddComponent<VfxLumiere>();
        l.theme = theme;
        l.taille = taille;
        l.detruireApres = maintien >= 0f;
        l.Allumer(maintien);
        return l;
    }

    // Éclat ponctuel en `position` (objet à part, détruit après l'extinction).
    public static VfxLumiere Eclat(Vector3 position, VfxTheme theme, VfxTailleLumiere taille, float maintien = 0.05f)
    {
        VfxLumiere l = Creer(null, position, theme, taille, Mathf.Max(0f, maintien));
        l.gameObject.name = "VfxEclat";
        return l;
    }

    private void Assurer()
    {
        if (lum != null) return;
        lum = GetComponent<Light>();
        if (lum == null) lum = gameObject.AddComponent<Light>();
        lum.type = LightType.Point;
        lum.shadows = LightShadows.None;
        if (!allumee) lum.intensity = 0f;
        graine = Random.value * 50f;
    }

    private void Awake()
    {
        Assurer();
        if (suivreParticules) particules = GetComponentsInChildren<ParticleSystem>();
    }

    private void Start()
    {
        if (allumerAuDemarrage && !allumee) Allumer();
    }

    // Allume (montée depuis l'intensité actuelle) ; `maintien` < 0 : tenue jusqu'à Eteindre().
    public void Allumer(float maintien = -1f)
    {
        Assurer();
        if (!allumee || Time.time >= debutExtinction) debut = Time.time;
        allumee = true;
        finMaintien = maintien < 0f ? float.PositiveInfinity : Time.time + Montee + maintien;
        debutExtinction = finMaintien;
        lum.enabled = true;
        Appliquer();
    }

    public void Eteindre()
    {
        if (!allumee) return;
        debutExtinction = Mathf.Min(debutExtinction, Time.time);
    }

    public Color CouleurTheme()
    {
        if (couleurImposee) return couleur;
        Color coeur = VfxPalette.Couleur(theme, VfxRole.Coeur, Color.white);
        VfxPalette p = VfxPalette.De(theme);
        bool aVif = false;
        if (p != null && p.teintes != null)
            foreach (VfxPalette.Teinte t in p.teintes) if (t.role == VfxRole.Vif) aVif = true;
        return aVif ? Color.Lerp(VfxPalette.Couleur(theme, VfxRole.Vif, coeur), coeur, 0.5f) : coeur;
    }

    private float Enveloppe()
    {
        if (!allumee) return 0f;
        float t = Time.time;
        float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - debut) / Montee));
        if (t >= debutExtinction)
        {
            float k = Mathf.Clamp01((t - debutExtinction) / Extinction);
            e *= 1f - k * k * (3f - 2f * k);
            if (k >= 1f) return -1f;
        }
        return e;
    }

    private void Appliquer()
    {
        float e = Enveloppe();
        if (e < 0f)
        {
            allumee = false;
            lum.intensity = 0f;
            lum.enabled = false;
            if (detruireApres) Destroy(gameObject);
            return;
        }
        float scintille = 1f;
        if (theme == VfxTheme.Feu && !couleurImposee)
            scintille = 0.88f + 0.24f * Mathf.PerlinNoise(Time.time * 9f, graine);
        lum.color = CouleurTheme();
        lum.range = Portee(taille);
        lum.intensity = Intensite(taille) * e * Mathf.Max(0f, facteur) * scintille;
    }

    private void LateUpdate()
    {
        if (suivreParticules && particules != null)
        {
            bool emet = false;
            foreach (ParticleSystem ps in particules) if (ps != null && ps.isEmitting) { emet = true; break; }
            if (emet && (!allumee || Time.time >= debutExtinction)) Allumer();
            else if (!emet && allumee) Eteindre();
        }
        if (lum != null && (allumee || lum.enabled)) Appliquer();
    }
}
