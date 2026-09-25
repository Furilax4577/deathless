using System.Collections.Generic;
using UnityEngine;

// Mode furtif de l'assassin (25/09/2026), à poser sur le personnage : le corps (et ses armes) s'assombrit vers le
// violet sombre du thème Ombre et devient partiellement transparent, avec un léger miroitement (quelques gemmes
// sombres qui scintillent à la surface du corps et une variation lente de l'opacité). Transition d'entrée et de
// sortie en `transition` s. Pas de lumière : l'assassin ne doit pas éclairer.
// API : Entrer(), Sortir(), EstFurtif, Niveau (0..1). Les matériaux d'origine sont rendus à la sortie.
// Règles (Wiki/pages/classes.md) : marche discrète hors combat, fumigène, coup non détecté = critique.
public class ModeFurtif : MonoBehaviour
{
    [SerializeField] private Material materiauGemmes;
    [SerializeField] private float transition = 0.35f;
    [Tooltip("Opacité du corps en mode furtif.")]
    [Range(0.1f, 1f)] [SerializeField] private float opacite = 0.4f;
    [Tooltip("Assombrissement vers la teinte Ombre (0 aucun, 1 teinte pleine).")]
    [Range(0f, 1f)] [SerializeField] private float assombrissement = 0.85f;

    private readonly List<Renderer> rendus = new List<Renderer>();
    private readonly List<Material[]> originaux = new List<Material[]>();
    private readonly List<Material[]> copies = new List<Material[]>();
    private readonly List<Color[]> couleursOrigine = new List<Color[]>();
    private float niveau;
    private bool cible;
    private bool copie;
    private GemmesVolantes miroitement;
    private float semis;

    public bool EstFurtif { get { return cible; } }
    public float Niveau { get { return niveau; } }

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public void Entrer() { cible = true; }
    public void Sortir() { cible = false; }

    private void Awake()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                if (r.GetComponent<GemmesVolantes>() == null)
                    rendus.Add(r);
        if (materiauGemmes != null)
        {
            miroitement = GemmesVolantes.Creer("Furtif_Miroitement", materiauGemmes, 120);
            miroitement.transform.SetParent(transform, false);
        }
    }

    private void OnDestroy()
    {
        Restaurer();
    }

    private void Copier()
    {
        if (copie) return;
        copie = true;
        originaux.Clear(); copies.Clear(); couleursOrigine.Clear();
        foreach (Renderer r in rendus)
        {
            Material[] o = r.sharedMaterials;
            Material[] c = new Material[o.Length];
            Color[] col = new Color[o.Length];
            for (int i = 0; i < o.Length; i++)
            {
                c[i] = new Material(o[i]);
                col[i] = c[i].HasProperty(BaseColor) ? c[i].GetColor(BaseColor) : Color.white;
                RendreTransparent(c[i]);
            }
            originaux.Add(o); copies.Add(c); couleursOrigine.Add(col);
            r.sharedMaterials = c;
        }
    }

    // URP Lit : surface transparente, mélange alpha, sans écriture de profondeur.
    private static void RendreTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void Restaurer()
    {
        if (!copie) return;
        for (int i = 0; i < rendus.Count && i < originaux.Count; i++)
            if (rendus[i] != null) rendus[i].sharedMaterials = originaux[i];
        foreach (Material[] c in copies) foreach (Material m in c) if (m != null) Destroy(m);
        copies.Clear(); originaux.Clear(); couleursOrigine.Clear();
        copie = false;
    }

    private void LateUpdate()
    {
        niveau = Mathf.MoveTowards(niveau, cible ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, transition));
        if (niveau <= 0f) { Restaurer(); return; }
        Copier();
        float k = Mathf.SmoothStep(0f, 1f, niveau);
        Color ombre = Color.Lerp(VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Base, new Color(0.17f, 0.09f, 0.25f)),
            VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Vif, new Color(0.36f, 0.23f, 0.54f)), 0.45f);
        // Miroitement : l'opacité ondule doucement.
        float alpha = Mathf.Lerp(1f, opacite * (0.9f + 0.1f * Mathf.Sin(Time.time * 5f)), k);
        for (int i = 0; i < copies.Count; i++)
            for (int j = 0; j < copies[i].Length; j++)
            {
                Material m = copies[i][j];
                if (m == null || !m.HasProperty(BaseColor)) continue;
                Color c = Color.Lerp(couleursOrigine[i][j], ombre, assombrissement * k);
                c.a = alpha;
                m.SetColor(BaseColor, c);
            }
        // Gemmes sombres et reflets violets qui scintillent à la surface du corps.
        if (miroitement != null && rendus.Count > 0)
        {
            semis += Time.deltaTime * 40f * k;
            Color vif = VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Vif, new Color(0.36f, 0.23f, 0.54f));
            Color coeur = VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Coeur, new Color(0.65f, 0.54f, 0.84f));
            while (semis >= 1f)
            {
                semis -= 1f;
                Renderer r = rendus[Random.Range(0, rendus.Count)];
                Bounds b = r.bounds;
                Vector3 p = b.center + Vector3.Scale(Random.insideUnitSphere, b.extents * 0.9f);
                miroitement.Emettre(p, Vector3.up * 0.15f, Random.Range(0.015f, 0.03f), Random.Range(0.3f, 0.5f),
                    Random.value < 0.3f ? coeur : vif, 0f, 1f, 0.08f, 0.3f);
            }
        }
    }
}
