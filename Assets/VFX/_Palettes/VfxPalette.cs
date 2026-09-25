using System.Collections.Generic;
using UnityEngine;

// Thèmes de couleur des effets visuels (source unique, 25/09/2026). Règle : le feu est couleur feu ; le vert Nyxessa est
// réservé à la relique et à son énergie. Voir Docs/vfx.md (table des thèmes).
// Nouveaux thèmes ajoutés en fin de liste (valeurs sérialisées dans les assets).
public enum VfxTheme { Feu, Nyxessa, Terre, Rage, Sacre, Soin, Os, BouclierPlein, BouclierEntame, BouclierCritique, Critique, Chasse, Ombre, SoinCroix }

// Rôle d'une teinte dans son thème, du plus sombre au plus clair ; Accent = teinte à part, retrouvée par son nom.
public enum VfxRole { Ombre, Base, Vif, Coeur, Accent }

// Palette d'un thème : 3 à 5 teintes principales ordonnées (ombre → cœur) et des accents nommés. Les scripts d'effets
// lisent leurs couleurs ici à l'exécution (VfxPalette.Couleur / Rampe, via le registre Resources/VfxPalettes) ; les
// matériaux Lit listés dans `materiaux` reçoivent leur teinte par l'outil Deathless > VFX > Appliquer les palettes
// (et automatiquement en éditeur quand on modifie la palette). Changer une teinte ici recolore tous les effets du thème.
[CreateAssetMenu(menuName = "Deathless/VFX/Palette", fileName = "Palette")]
public class VfxPalette : ScriptableObject
{
    [System.Serializable]
    public class Teinte
    {
        public string nom;
        public VfxRole role;
        public Color couleur = Color.white;
    }

    [System.Serializable]
    public class CibleMateriau
    {
        public Material materiau;
        public VfxRole role = VfxRole.Base;
        [Tooltip("Nom de l'accent si role = Accent.")]
        public string accent;
        public string propriete = "_BaseColor";
    }

    public VfxTheme theme;
    [TextArea] public string usage;
    public Teinte[] teintes;
    [Tooltip("Matériaux Lit (particules, croix...) recolorés par l'outil Appliquer les palettes.")]
    public CibleMateriau[] materiaux;

    // Version : incrémentée à chaque modification (les caches des scripts se reconstruisent).
    public static int Version { get; private set; }

    public Color Get(VfxRole role, string accent, Color defaut)
    {
        if (teintes != null)
            foreach (Teinte t in teintes)
                if (t.role == role && (role != VfxRole.Accent || string.IsNullOrEmpty(accent) || t.nom == accent))
                    return t.couleur;
        return defaut;
    }

    // ---------------------------------------------------------------- accès statique (registre)

    public static VfxPalette De(VfxTheme theme)
    {
        VfxPalettes registre = VfxPalettes.Instance;
        return registre != null ? registre.Trouver(theme) : null;
    }

    // Teinte `role` du thème (ou l'accent `accent`), `defaut` si le registre ou la teinte manque.
    public static Color Couleur(VfxTheme theme, VfxRole role, Color defaut, string accent = null)
    {
        VfxPalette p = De(theme);
        return p != null ? p.Get(role, accent, defaut) : defaut;
    }

    // Émission (HDR) tirée d'une teinte : plus saturée que la teinte (puissance 1,6) et multipliée par `intensite`.
    public static Color Lueur(Color c, float intensite)
    {
        return new Color(Mathf.Pow(c.r, 1.6f) * intensite, Mathf.Pow(c.g, 1.6f) * intensite, Mathf.Pow(c.b, 1.6f) * intensite, 1f);
    }

    public static Color Accent(VfxTheme theme, string nom, Color defaut)
    {
        return Couleur(theme, VfxRole.Accent, defaut, nom);
    }

    private static readonly Dictionary<string, Color[]> caches = new Dictionary<string, Color[]>();
    private static int versionCache = -1;

    // Tableau de couleurs construit une fois par version de palette (pour les rampes lues dans des boucles).
    public static Color[] Cache(string cle, System.Func<Color[]> construire)
    {
        if (versionCache != Version) { caches.Clear(); versionCache = Version; }
        Color[] c;
        if (!caches.TryGetValue(cle, out c))
        {
            c = construire();
            caches[cle] = c;
        }
        return c;
    }

    private void OnValidate()
    {
        Version++;
#if UNITY_EDITOR
        VfxPalette moi = this;
        UnityEditor.EditorApplication.delayCall += () => { if (moi != null) moi.AppliquerMateriaux(); };
#endif
    }

    private void OnEnable() { Version++; }

    // Pousse les teintes dans les matériaux ciblés (éditeur : l'outil sauvegarde les assets).
    public int AppliquerMateriaux()
    {
        int n = 0;
        if (materiaux == null) return 0;
        foreach (CibleMateriau c in materiaux)
        {
            if (c == null || c.materiau == null) continue;
            string prop = string.IsNullOrEmpty(c.propriete) ? "_BaseColor" : c.propriete;
            if (!c.materiau.HasProperty(prop)) continue;
            Color col = Get(c.role, c.accent, c.materiau.GetColor(prop));
            col.a = c.materiau.GetColor(prop).a;
            if (c.materiau.GetColor(prop) != col)
            {
                c.materiau.SetColor(prop, col);
                if (prop == "_BaseColor" && c.materiau.HasProperty("_Color")) c.materiau.SetColor("_Color", col);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(c.materiau);
#endif
                n++;
            }
        }
        return n;
    }
}
