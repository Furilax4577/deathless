using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Lueur légère (bloom URP) du village, plus présente la nuit que le jour (Quentin, 26/09/2026) : seuil haut, pour que
// seules les vraies sources brillent (cristal de Nyxessa, anneaux du socle, portail, lanternes, fenêtres), intensité
// modeste, sans halo qui mange l'image ni cristal blanchi. Suit CycleJourNuit.Nuit (0 jour, 1 nuit) ; aucune allocation
// par image (le profil est copié une fois au réveil, puis seules deux valeurs changent). Coupable : LueurNuit.Active
// (PlayerPrefs, clé Deathless.Lueur ; à brancher dans les options d'affichage).
[RequireComponent(typeof(Volume))]
public class LueurNuit : MonoBehaviour
{
    public const string ClePrefs = "Deathless.Lueur";

    public CycleJourNuit cycle;
    [Header("Jour")]
    public float intensiteJour = 0.12f;
    public float seuilJour = 1.25f;
    [Header("Nuit")]
    public float intensiteNuit = 0.7f;
    public float seuilNuit = 0.9f;

    private Volume volume;
    private Bloom bloom;

    // Réglage du joueur (vrai par défaut). Relu à chaque image : le changer dans les options s'applique aussitôt.
    public static bool Active
    {
        get { return PlayerPrefs.GetInt(ClePrefs, 1) == 1; }
        set { PlayerPrefs.SetInt(ClePrefs, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        // Copie du profil pour ce Volume (l'asset n'est jamais modifié en jeu).
        if (volume.sharedProfile != null && volume.profile.TryGet(out Bloom b)) bloom = b;
        if (cycle == null) cycle = FindAnyObjectByType<CycleJourNuit>();
    }

    private void Update()
    {
        if (bloom == null) return;
        bool active = Active;
        if (volume.weight != (active ? 1f : 0f)) volume.weight = active ? 1f : 0f;
        if (!active) return;
        float n = cycle != null ? cycle.Nuit : 0f;
        bloom.intensity.value = Mathf.Lerp(intensiteJour, intensiteNuit, n);
        bloom.threshold.value = Mathf.Lerp(seuilJour, seuilNuit, n);
    }
}
