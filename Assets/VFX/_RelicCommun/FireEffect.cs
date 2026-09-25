using UnityEngine;

// Feu stylisé (étape 48) : trois systèmes de particules créés par script, partagés par les pièges à flammes, les
// torches et le four de la forge. Les flammes montent toujours vers le haut du monde (le système est tourné pour
// que son axe d'émission soit vertical, et simule dans l'espace monde), avec des braises qui s'envolent et une
// fumée grise qui s'élargit. Purement visuel et local ; utilisable à l'exécution comme dans l'éditeur.
// Matériaux : `FlameParticle` (additif, sprite doux) et `SmokeParticle` (fondu alpha), dans Assets/Art/Materials.
public class FireEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem flames;
    [SerializeField] private ParticleSystem embers;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField] private Light fireLight;
    [SerializeField] private float lightIntensity = 2.5f;

    private bool emitting = true;

    // radius : rayon du foyer au sol (0,1 pour une torche, 1,6 pour une grille de piège) ; scale : hauteur et taille
    // des flammes (0,35 torche, 1 piège, 1,3 four).
    // withFlames : faux pour une cheminée (braises et fumée seulement, sans flamme visible).
    public static FireEffect Create(Transform parent, Vector3 localPosition, float radius, float scale,
        Material flameMaterial, Material smokeMaterial, bool withLight, bool withSmoke, bool withFlames = true)
    {
        GameObject root = new GameObject("Fire");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        // Un ParticleSystem émet le long de son axe Z : Z vers le haut.
        root.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        FireEffect fire = root.AddComponent<FireEffect>();

        // Hauteur des flammes : plus que proportionnelle à l'échelle (une grille de piège doit cracher haut, une torche
        // garde une petite flamme).
        float rise = Mathf.Pow(scale, 1.3f);
        if (withFlames)
            fire.flames = Make(root, "Flames", flameMaterial, radius, scale, 0.45f * (1f + scale * 0.3f), 0.7f * (1f + scale * 0.3f),
            2.2f * rise, 3.6f * rise, 0.5f * scale, 0.9f * scale, 20f + 35f * radius,
            Gradient(Color.Lerp(Feu(VfxRole.Coeur), Feu(VfxRole.Vif), 0.15f), Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.2f), Color.Lerp(Feu(VfxRole.Base), Feu(VfxRole.Vif), 0.2f), 0.9f, 0.7f, 0f),
            0.35f * scale, 0f);
        fire.embers = Make(root, "Embers", flameMaterial, radius, scale, 1.0f, 1.8f, 1.5f * scale, 3f * scale, 0.05f * scale, 0.1f * scale, 4f + 8f * radius,
            Gradient(Feu(VfxRole.Coeur), Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.25f), Color.Lerp(Feu(VfxRole.Base), Feu(VfxRole.Vif), 0.6f), 1f, 1f, 0f),
            0.6f * scale, -0.05f);
        if (withSmoke && smokeMaterial != null)
        {
            fire.smoke = Make(root, "Smoke", smokeMaterial, radius * 0.7f, scale, 1.6f, 2.6f, 0.7f * scale, 1.3f * scale, 0.5f * scale, 0.9f * scale, 5f + 8f * radius,
                Gradient(new Color(0.5f, 0.5f, 0.5f), new Color(0.32f, 0.32f, 0.34f), new Color(0.25f, 0.25f, 0.28f), 0.05f, 0.55f, 0f),
                0.25f * scale, -0.02f);
            fire.smoke.transform.localPosition = new Vector3(0f, 0f, 0.5f * scale);
            var size = fire.smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
        }
        if (withLight)
        {
            GameObject lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.6f * scale);
            fire.fireLight = lightGo.AddComponent<Light>();
            fire.fireLight.type = LightType.Point;
            fire.fireLight.color = Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.4f);
            fire.lightIntensity = 2.5f * scale;
            fire.fireLight.intensity = fire.lightIntensity;
            fire.fireLight.range = 5f + 5f * scale;
            fire.fireLight.shadows = LightShadows.None;
        }
        return fire;
    }

    // Lanterne suspendue (poteau du village, cimetière) : une lumière chaude au centre du corps de verre, un halo doux
    // autour (une seule particule fixe, additive, qui rayonne avec le Bloom) et le même vacillement que les flammes.
    // `localCenter` : centre du verre dans le repère de `parent`. Le halo est plus grand que la lanterne : il reste
    // visible au-delà de son maillage, qui masque le reste.
    public static FireEffect CreateLantern(Transform parent, Vector3 localCenter, Material glowMaterial, float intensity, float range)
    {
        GameObject root = new GameObject("LanternFire");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localCenter;
        FireEffect fire = root.AddComponent<FireEffect>();

        GameObject lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform, false);
        fire.fireLight = lightGo.AddComponent<Light>();
        fire.fireLight.type = LightType.Point;
        fire.fireLight.color = Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.55f);
        fire.lightIntensity = intensity;
        fire.fireLight.intensity = intensity;
        fire.fireLight.range = range;
        fire.fireLight.shadows = LightShadows.None;

        if (glowMaterial != null)
        {
            GameObject glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            ParticleSystem glow = glowGo.AddComponent<ParticleSystem>();
            var main = glow.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = 1000000f;
            main.startSpeed = 0f;
            main.startSize = 1.5f;
            Color lueur = Color.Lerp(Feu(VfxRole.Vif), Feu(VfxRole.Coeur), 0.45f);
            lueur.a = 0.55f;
            main.startColor = lueur;
            main.maxParticles = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = glow.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var shape = glow.shape;
            shape.enabled = false;
            var glowRenderer = glowGo.GetComponent<ParticleSystemRenderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            fire.flames = glow;   // pour que SetEmitting éteigne aussi le halo
        }
        return fire;
    }

    private static ParticleSystem Make(GameObject root, string name, Material material, float radius, float scale,
        float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, float rate,
        Gradient over, float noise, float gravity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        ParticleSystem system = go.AddComponent<ParticleSystem>();
        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = gravity;
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        var emission = system.emission;
        emission.rateOverTime = rate;
        // Cône étroit : les particules partent de la surface du foyer et montent droit (axe Z du système, tourné vers le
        // haut du monde). Un cercle, lui, émet en anneau vers l'extérieur : c'était le défaut des anciennes flammes.
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = Mathf.Max(0.02f, radius);
        shape.radiusThickness = 1f;
        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = over;
        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));
        var noiseModule = system.noise;
        noiseModule.enabled = noise > 0f;
        noiseModule.strength = noise;
        noiseModule.frequency = 0.8f;
        noiseModule.scrollSpeed = 0.6f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = -10f;
        return system;
    }

    // Teintes du thème Feu (palette de référence : boule de feu de Relic).
    private static Color Feu(VfxRole role)
    {
        switch (role)
        {
            case VfxRole.Coeur: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(1f, 0.9f, 0.4f));
            case VfxRole.Vif: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(1f, 0.38f, 0.04f));
            default: return VfxPalette.Couleur(VfxTheme.Feu, role, new Color(0.8f, 0.12f, 0.03f));
        }
    }

    private static Gradient Gradient(Color a, Color b, Color c, float alphaStart, float alphaMid, float alphaEnd)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.4f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaMid, 0.5f), new GradientAlphaKey(alphaEnd, 1f) });
        return gradient;
    }

    // Allumé ou éteint (les pièges alternent) : l'émission s'arrête, les particules en vol finissent leur vie.
    public void SetEmitting(bool on)
    {
        if (emitting == on)
            return;
        emitting = on;
        foreach (ParticleSystem system in new[] { flames, embers, smoke })
        {
            if (system == null) continue;
            var emission = system.emission;
            emission.enabled = on;
        }
        if (fireLight != null)
            fireLight.enabled = on;
    }

    private void Update()
    {
        if (fireLight != null && emitting)
            fireLight.intensity = lightIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 9f, transform.position.x));
    }
}
