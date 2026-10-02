#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Palette « Biere » et matériau des effets de la taverne (02/10/2026) : créés une fois, enregistrés dans le registre des
// palettes (Assets/VFX/_Palettes/Resources/VfxPalettes.asset). Appelé par MaisonTripo.Construire("Taverne") ; aussi au menu.
public static class BiereFuiteOutil
{
    public const string Palette = "Assets/VFX/_Palettes/Biere.asset";
    public const string Materiau = "Assets/VFX/BiereFuite/BiereFuite.mat";
    const string MateriauSource = "Assets/VFX/_RelicCommun/PortalVoxel.mat";

    [MenuItem("Deathless/VFX/Bière : palette et matériau (taverne)")]
    public static void Menu() { Debug.Log(Assurer()); }

    static VfxPalette.Teinte T(string nom, VfxRole role, string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return new VfxPalette.Teinte { nom = nom, role = role, couleur = c };
    }

    public static string Assurer()
    {
        string r = "";
        var p = AssetDatabase.LoadAssetAtPath<VfxPalette>(Palette);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<VfxPalette>();
            p.theme = VfxTheme.Biere;
            p.usage = "Bière de la taverne « Le Tonneau Percé » : fuite du tonneau du toit (BiereFuite), du jaune ambré au blanc de la mousse. Jamais vert.";
            p.teintes = new[] {
                T("Ambre sombre", VfxRole.Ombre, "#b8741a"), T("Ambre", VfxRole.Base, "#e3a31a"), T("Dorée", VfxRole.Vif, "#f5c542"),
                T("Mousse", VfxRole.Coeur, "#fff6df"), T("MousseOmbre", VfxRole.Accent, "#e8d9b0") };
            p.materiaux = new VfxPalette.CibleMateriau[0];
            p.intensiteEmission = 1f;
            AssetDatabase.CreateAsset(p, Palette);
            r += "palette créée ; ";
        }
        var reg = AssetDatabase.LoadAssetAtPath<VfxPalettes>("Assets/VFX/_Palettes/Resources/VfxPalettes.asset");
        if (reg != null && System.Array.IndexOf(reg.palettes, p) < 0)
        {
            var l = new System.Collections.Generic.List<VfxPalette>(reg.palettes) { p };
            reg.palettes = l.ToArray();
            EditorUtility.SetDirty(reg);
            r += "ajoutée au registre ; ";
        }
        if (AssetDatabase.LoadAssetAtPath<Material>(Materiau) == null)
        {
            AssetDatabase.CopyAsset(MateriauSource, Materiau);
            r += "matériau copié de PortalVoxel ; ";
        }
        AssetDatabase.SaveAssets();
        return "Bière : " + (r == "" ? "rien à faire" : r);
    }
}
#endif
