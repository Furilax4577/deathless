using System.Collections.Generic;
using UnityEngine;

// Ambiance des intérieurs du village (visuel seulement, rien sur le réseau). Posé par Deathless > Niveau > Intérieurs
// sur VillageBlockout/Interieurs. Lit le fondu jour / nuit de CycleJourNuit (sans le modifier) :
// - foyers (forge, âtre, chaudron) : scintillement marqué, un peu plus forts la nuit ;
// - lanternes et bougies : léger scintillement, plus fortes la nuit (l'intérieur reste lisible) ;
// - vitres vues de l'intérieur : claires le jour (la lumière entre), bleu nuit la nuit ;
// - braises : pulsation lente de l'émission, teinte vive du thème Feu (palette, jamais en dur ; comme ForgeFeu).
// La lumière Feu_Forge et les braises de la forge n'y sont plus : ForgeFeu (feu vivant) les pilote (ForgeronBuilder).
// Toutes les lumières sont ponctuelles, chaudes, sans ombre ; aucune allocation par image.
public class InterieursAmbiance : MonoBehaviour
{
    public CycleJourNuit cycle;
    public Light[] feux;
    public float[] feuxIntensite;
    public Light[] lampes;
    public float[] lampesIntensite;
    [Tooltip("Part de l'intensité gardée le jour (1 la nuit).")]
    [Range(0f, 1f)] public float partJour = 0.75f;
    public Renderer[] vitres;
    [ColorUsage(false, true)] public Color vitreJour = new Color(0.62f, 0.74f, 0.9f) * 0.9f;
    [ColorUsage(false, true)] public Color vitreNuit = new Color(0.03f, 0.04f, 0.1f);
    public Renderer[] braises;
    [Tooltip("Éclat (HDR) de l'émission des braises, tiré de la teinte vive du thème Feu.")]
    public float eclatBraises = 2.2f;

    MaterialPropertyBlock m_Bloc;
    // Copies des matériaux créées par Activer (une par rendu), avec le rendu et son matériau d'origine : détruites par
    // OnDestroy (sinon elles restent en mémoire après la scène).
    readonly List<Renderer> m_Rendus = new List<Renderer>();
    readonly List<Material> m_Copies = new List<Material>();
    readonly List<Material> m_Origines = new List<Material>();
    Color m_Braises;
    int m_Version = -1;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        m_Bloc = new MaterialPropertyBlock();
        if (cycle == null) cycle = FindAnyObjectByType<CycleJourNuit>();
        // URP peut retirer le mot-clé _EMISSION des assets à la réimportation : on l'active sur une instance par rendu
        // (vitres, braises : quelques rendus par maison), comme CycleJourNuit pour les fenêtres des maisons.
        Activer(vitres); Activer(braises);
    }

    void Activer(Renderer[] rs)
    {
        if (rs == null) return;
        foreach (Renderer r in rs)
        {
            if (r == null) continue;
            Material origine = r.sharedMaterial;
            if (m_Copies.Contains(origine)) continue;   // rendu déjà traité (listé deux fois)
            Material copie = r.material;
            copie.EnableKeyword("_EMISSION");
            m_Rendus.Add(r); m_Copies.Add(copie); m_Origines.Add(origine);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < m_Copies.Count; i++)
        {
            Material copie = m_Copies[i];
            if (copie == null) continue;
            // Le rendu survit au composant (composant retiré seul) : il reprend son matériau d'origine.
            Renderer r = m_Rendus[i];
            if (r != null && r.sharedMaterial == copie) r.sharedMaterial = m_Origines[i];
            Destroy(copie);
        }
        m_Rendus.Clear(); m_Copies.Clear(); m_Origines.Clear();
    }

    void LateUpdate()
    {
        float nuit = cycle != null ? cycle.Nuit : DayCycle.Night;
        float part = Mathf.Lerp(partJour, 1f, nuit);
        float t = Time.time;
        if (feux != null)
            for (int i = 0; i < feux.Length; i++)
            {
                Light l = feux[i]; if (l == null) continue;
                float f = 0.78f + 0.14f * Mathf.PerlinNoise(t * 4.2f, i * 3.1f) + 0.08f * Mathf.PerlinNoise(t * 11f, i * 7.3f + 5f);
                l.intensity = feuxIntensite[i] * part * f;
            }
        if (lampes != null)
            for (int i = 0; i < lampes.Length; i++)
            {
                Light l = lampes[i]; if (l == null) continue;
                l.intensity = lampesIntensite[i] * part * (0.93f + 0.07f * Mathf.PerlinNoise(t * 2.5f, i * 1.7f + 20f));
            }
        if (m_Bloc == null) return;
        Poser(vitres, Color.Lerp(vitreJour, vitreNuit, nuit));
        if (m_Version != VfxPalette.Version)
        {
            m_Version = VfxPalette.Version;
            m_Braises = VfxPalette.Lueur(VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, new Color(1f, 0.38f, 0.04f)), eclatBraises);
        }
        Poser(braises, m_Braises * (0.8f + 0.2f * Mathf.PerlinNoise(t * 1.3f, 2f)));
    }

    void Poser(Renderer[] rs, Color c)
    {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i]; if (r == null) continue;
            r.GetPropertyBlock(m_Bloc);
            m_Bloc.SetColor(EmissionId, c);
            r.SetPropertyBlock(m_Bloc);
        }
    }
}
