using UnityEngine;

// Ambiances du jeu (étape 40) : le jour pendant le répit, le crépuscule pendant les vagues. Deux jeux de valeurs
// entre lesquels DayCycle fait un fondu. Un seul asset : Assets/Settings/Ambiance.asset.
[CreateAssetMenu(menuName = "Relic/Ambiance", fileName = "Ambiance")]
public class Ambiance : ScriptableObject
{
    [System.Serializable]
    public class Preset
    {
        [Tooltip("Orientation du soleil (degrés) : x = hauteur, y = azimut.")]
        public Vector2 sunEuler = new Vector2(48f, 335f);
        public Color sunColor = Color.white;
        public float sunIntensity = 1.7f;
        [Header("Lumière ambiante (trois couleurs)")]
        public Color ambientSky = new Color(0.55f, 0.7f, 0.9f);
        public Color ambientEquator = new Color(0.6f, 0.6f, 0.55f);
        public Color ambientGround = new Color(0.35f, 0.3f, 0.25f);
        [Header("Brouillard")]
        public Color fogColor = new Color(0.74f, 0.8f, 0.88f);
        public float fogStart = 25f;
        public float fogEnd = 110f;
        [Header("Ciel (skybox procédural)")]
        public Color skyTint = new Color(0.42f, 0.6f, 0.9f);
        public float skyExposure = 1.25f;
        public float atmosphereThickness = 0.85f;
    }

    [Tooltip("Répit, salle d'attente, menu.")]
    public Preset day = new Preset();
    [Tooltip("Pendant une vague.")]
    public Preset dusk = new Preset
    {
        sunEuler = new Vector2(12f, 300f),
        sunColor = new Color(1f, 0.6f, 0.35f),
        sunIntensity = 1.1f,
        ambientSky = new Color(0.45f, 0.3f, 0.55f),
        ambientEquator = new Color(0.55f, 0.32f, 0.38f),
        ambientGround = new Color(0.18f, 0.12f, 0.16f),
        fogColor = new Color(0.55f, 0.36f, 0.5f),
        fogStart = 15f,
        fogEnd = 85f,
        skyTint = new Color(0.8f, 0.42f, 0.55f),
        skyExposure = 0.95f,
        atmosphereThickness = 1.35f,
    };

    [Tooltip("Durée du fondu entre jour et crépuscule (s).")]
    public float transitionSeconds = 10f;
    [Tooltip("Le crépuscule commence ce nombre de secondes avant la fin du répit : la nuit est tombée quand la vague démarre (demande de Quentin). Un peu plus que la durée du fondu.")]
    public float duskLeadSeconds = 12f;
}
