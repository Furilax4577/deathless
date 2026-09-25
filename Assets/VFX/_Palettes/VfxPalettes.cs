using UnityEngine;

// Registre des palettes de thème (un seul asset : Assets/VFX/_Palettes/Resources/VfxPalettes.asset), chargé par
// Resources pour que les API statiques des effets (GemBurst, FireballVisual, DirtBurst...) y accèdent sans référence.
[CreateAssetMenu(menuName = "Deathless/VFX/Registre des palettes", fileName = "VfxPalettes")]
public class VfxPalettes : ScriptableObject
{
    public VfxPalette[] palettes;

    private static VfxPalettes instance;
    private static bool charge;

    public static VfxPalettes Instance
    {
        get
        {
            if (instance == null && (!charge || !Application.isPlaying))
            {
                instance = Resources.Load<VfxPalettes>("VfxPalettes");
                charge = true;
            }
            return instance;
        }
    }

    public VfxPalette Trouver(VfxTheme theme)
    {
        if (palettes != null)
            foreach (VfxPalette p in palettes)
                if (p != null && p.theme == theme)
                    return p;
        return null;
    }
}
