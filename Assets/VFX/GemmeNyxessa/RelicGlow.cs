using UnityEngine;

// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/RelicGlow.cs : IsWave() lit DayCycle.Night (copie locale)
// au lieu de RunProgress. Le reste est identique. (Désactivé au démarrage par RelicGem, comme dans Relic.)
//
// Lueur intérieure de la gemme selon l'état du jeu (demande de Quentin, 22 septembre 2026) : discrète de jour et
// au répit, plus marquée pendant les vagues, en plus de la lumière déjà projetée par le point Light du cristal.
[RequireComponent(typeof(Renderer))]
public class RelicGlow : MonoBehaviour
{
    [SerializeField] private Color dayEmission = new Color(0.05f, 0.22f, 0.04f);
    [SerializeField] private Color nightEmission = new Color(0.09f, 0.35f, 0.07f);
    [SerializeField] private float fadeSeconds = 2.5f;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Renderer target;
    private MaterialPropertyBlock block;
    private float blend;

    private void Awake()
    {
        target = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        float wanted = IsWave() ? 1f : 0f;
        float speed = fadeSeconds > 0f ? 1f / fadeSeconds : 100f;
        blend = Mathf.MoveTowards(blend, wanted, speed * Time.deltaTime);

        target.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, Color.Lerp(dayEmission, nightEmission, blend));
        target.SetPropertyBlock(block);
    }

    private static bool IsWave()
    {
        return DayCycle.Night >= 0.5f;
    }
}
