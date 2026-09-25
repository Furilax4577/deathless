using UnityEditor;
using UnityEngine;

// Deathless > VFX > Appliquer les palettes : pousse les teintes de chaque palette de thème dans les matériaux Lit qu'elle
// liste (flammèches, cône, croix du soin, cristal de la relique...). Les effets en gemmes lisent leurs couleurs à
// l'exécution et n'ont pas besoin de cet outil.
public static class VfxPaletteOutil
{
    [MenuItem("Deathless/VFX/Appliquer les palettes")]
    public static void Appliquer()
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:VfxPalette"))
        {
            VfxPalette p = AssetDatabase.LoadAssetAtPath<VfxPalette>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null) n += p.AppliquerMateriaux();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Palettes VFX appliquées : " + n + " matériau(x) modifié(s).");
    }
}
