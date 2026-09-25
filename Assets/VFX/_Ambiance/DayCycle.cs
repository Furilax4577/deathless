using UnityEngine;

// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/DayCycle.cs : RunProgress (répit / vague, temps restant)
// est remplacé par deux réglages à la main : `nuit` (0 jour, 1 crépuscule, cible du fondu) et `heure` (course du soleil
// pendant le jour, 0 aube, 0,5 midi, 1 soir). Le fondu, les presets Ambiance et l'application sont identiques.
//
// Jour pendant le répit, crépuscule pendant les vagues (étape 40) ; la nuit tombe juste avant la vague (40 bis).
// A poser sur la lumière directionnelle : soleil (orientation, couleur, intensité), lumière ambiante, brouillard, ciel.
[RequireComponent(typeof(Light))]
public class DayCycle : MonoBehaviour
{
    [SerializeField] private Ambiance ambiance;
    [Tooltip("Bac à sable : cible du fondu (0 = jour / répit, 1 = crépuscule / vague).")]
    [Range(0f, 1f)] public float nuit;
    [Tooltip("Bac à sable : avancement du jour (0 aube, 0,5 midi, 1 soir).")]
    [Range(0f, 1f)] public float heure = 0.5f;

    private Light sun;
    private Material sky;
    private Material sharedSky;
    // 0 = jour, 1 = crépuscule.
    private float blend;
    private float dayProgress = 0.5f;

    // Fondu jour/nuit courant (0 = jour, 1 = nuit), lu par les effets qui suivent la nuit (brume au sol, GroundMist).
    public static float Night { get; private set; }

    // Aperçu (outil d'enregistrement, mise au point) : force la nuit quel que soit l'état de la partie.
    public static bool PreviewNight;

    private void Awake()
    {
        sun = GetComponent<Light>();
        // Copie du skybox : on ne modifie jamais l'asset du projet.
        sharedSky = RenderSettings.skybox;
        if (sharedSky != null)
        {
            sky = new Material(sharedSky);
            RenderSettings.skybox = sky;
        }
    }

    private void OnDestroy()
    {
        if (sharedSky != null)
            RenderSettings.skybox = sharedSky;
        if (sky != null)
            Destroy(sky);
    }

    private void Start()
    {
        blend = TargetBlend();
        dayProgress = heure;
        Apply();
    }

    private void Update()
    {
        if (ambiance == null)
            return;
        float target = TargetBlend();
        float speed = ambiance.transitionSeconds > 0f ? 1f / ambiance.transitionSeconds : 100f;
        blend = Mathf.MoveTowards(blend, target, speed * Time.deltaTime);
        float gap = heure - dayProgress;
        float rate = Mathf.Abs(gap) > 0.1f ? 2f : 0.3f;
        dayProgress = Mathf.Clamp01(dayProgress + gap * (1f - Mathf.Exp(-rate * Time.deltaTime)));
        Apply();
    }

    // Bac à sable : pose l'état sans fondu (captures).
    public void AppliquerImmediat()
    {
        blend = TargetBlend();
        dayProgress = heure;
        Apply();
    }

    private float TargetBlend()
    {
        return PreviewNight ? 1f : nuit;
    }

    private void Apply()
    {
        Night = blend;
        if (ambiance == null)
            return;
        Ambiance.Preset a = ambiance.day, b = ambiance.dusk;
        float t = Mathf.SmoothStep(0f, 1f, blend);

        // Le soleil du jour traverse le ciel : bas à l'est à l'aube, au réglage « jour » à midi, bas à l'ouest le soir.
        float arc = Mathf.Sin(Mathf.PI * dayProgress);
        Quaternion daySun = Quaternion.Euler(Mathf.Lerp(18f, a.sunEuler.x, arc), a.sunEuler.y + Mathf.Lerp(-70f, 70f, dayProgress), 0f);
        sun.transform.rotation = Quaternion.Slerp(daySun, Quaternion.Euler(b.sunEuler.x, b.sunEuler.y, 0f), t);
        sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
        sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);

        RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
        RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);

        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
        RenderSettings.fogStartDistance = Mathf.Lerp(a.fogStart, b.fogStart, t);
        RenderSettings.fogEndDistance = Mathf.Lerp(a.fogEnd, b.fogEnd, t);

        if (sky != null)
        {
            sky.SetColor("_SkyTint", Color.Lerp(a.skyTint, b.skyTint, t));
            sky.SetFloat("_Exposure", Mathf.Lerp(a.skyExposure, b.skyExposure, t));
            sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(a.atmosphereThickness, b.atmosphereThickness, t));
        }
    }
}
