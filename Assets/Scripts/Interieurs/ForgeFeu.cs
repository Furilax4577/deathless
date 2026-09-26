using UnityEngine;

// Feu vivant de la forge du village (Quentin, 26/09/2026 : « un feu vivant »), sur le lit de braises de la forge de
// l'intérieur Interieur_Forgeron. Remplace les quatre flammes fixes (pyramides) et la lueur constante des braises.
// Langage visuel des effets du jeu : gemmes low poly à couleurs par sommet (LowPolyGem, un seul maillage dynamique,
// shader Relic/VertexColorUnlit, pas d'alpha, apparition et disparition par la taille), comme GemmesVolantes :
// - flammes : des langues de gemmes étirées qui naissent pleines sur le lit, montent en dansant (lentement au pied,
//   de plus en plus vite ; balancement latéral qui grandit avec la hauteur, léger resserrement vers le centre), jaune
//   au pied, orange puis rouge à la pointe, et rétrécissent jusqu'à s'éteindre ; leur hauteur suit le souffle du feu ;
// - étincelles : quelques points blancs chauds qui s'échappent vers la hotte ;
// - braises vives : éclats posés sur le lit, chacun palpite à son rythme (rouge → orange → jaune) ;
// - lit de braises (maillage « Braises » de l'intérieur, matériau Interieur_Braises) : émission qui respire ;
// - lumière ponctuelle Feu_Forge : vacillement de ±10 % (comme les lanternes, mais plus vif), plus forte la nuit
//   (fondu jour / nuit de CycleJourNuit, comme InterieursAmbiance, qui ne pilote plus cette lumière ni ces braises).
// Couleurs : palette Feu (Assets/VFX/_Palettes/Feu), relues quand la palette change ; jamais de vert.
// Discret : 54 gemmes au plus (1296 sommets), aucune allocation par image, maillage mis à jour seulement s'il est vu
// et à moins de `distanceMax` de la caméra ; lumière sans ombre. Visuel seulement : chaque poste le joue pour lui,
// rien sur le réseau. Posé par ForgeronBuilder (Deathless > Niveau > Forgeron, relancé par Intérieurs).
[DisallowMultipleComponent]
public class ForgeFeu : MonoBehaviour
{
    [Tooltip("PortalVoxel.mat (Relic/VertexColorUnlit) : gemmes à couleurs par sommet.")]
    public Material materiau;
    [Tooltip("Demi-tailles du lit de braises (x, z) dans le repère de l'objet, centré sur le lit, y au dessus des braises.")]
    public Vector2 demiLit = new Vector2(0.42f, 0.3f);
    [Tooltip("Hauteur moyenne des flammes (m).")]
    public float hauteur = 0.36f;
    [Range(4, 48)] public int flammes = 34;
    [Range(0, 12)] public int etincelles = 6;
    [Range(0, 24)] public int braisesVives = 14;

    [Header("Lumière")]
    public Light lumiere;
    public float intensite = 3f;
    [Tooltip("Vacillement de l'intensité (0,1 : ±10 %).")]
    [Range(0f, 0.3f)] public float amplitude = 0.1f;
    [Tooltip("Part de l'intensité gardée le jour (1 la nuit).")]
    [Range(0f, 1f)] public float partJour = 0.75f;
    public CycleJourNuit cycle;

    [Header("Lit de braises")]
    public Renderer braises;
    [Tooltip("Éclat (HDR) de l'émission des braises, tiré de la teinte vive du thème Feu.")]
    public float eclatBraises = 2.4f;

    [Tooltip("Au-delà, le maillage n'est plus mis à jour (m).")]
    public float distanceMax = 30f;

    // Gemmes : [0, flammes) langues de feu, puis étincelles, puis braises vives.
    Mesh m_Mesh;
    MeshRenderer m_Rendu;
    Vector3[] m_Sommets;
    Color[] m_Couleurs;
    int m_N;
    float[] m_Ne, m_Vie, m_Taille, m_Phase, m_Freq, m_Spin;
    Vector3[] m_Base, m_Vit;
    Quaternion[] m_Rot;
    MaterialPropertyBlock m_Bloc;
    Color m_Braise, m_Rouge, m_Orange, m_Jaune, m_Blanc, m_Lumiere;
    int m_Version = -1;
    float m_Graine;
    Transform m_Camera;
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (cycle == null) cycle = FindAnyObjectByType<CycleJourNuit>();
        m_Graine = Hash(transform.position.x * 12.9898f + transform.position.z * 78.233f) * 100f;
        Couleurs();
        Construire();
        if (braises != null)
        {
            // URP peut retirer _EMISSION de l'asset à la réimportation : on l'active sur l'instance (un seul rendu).
            braises.material.EnableKeyword("_EMISSION");
            m_Bloc = new MaterialPropertyBlock();
        }
    }

    void OnDestroy() { if (m_Mesh != null) Destroy(m_Mesh); }

    static float Hash(float x) { float s = Mathf.Sin(x) * 43758.5453f; return s - Mathf.Floor(s); }

    // Teintes de la palette Feu (repli : valeurs de l'asset Feu, si le registre manque).
    void Couleurs()
    {
        m_Version = VfxPalette.Version;
        m_Braise = VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Ombre, new Color(0.29f, 0.07f, 0.024f));
        m_Rouge = VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Base, new Color(0.8f, 0.12f, 0.03f));
        m_Orange = VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Vif, new Color(1f, 0.38f, 0.04f));
        m_Jaune = VfxPalette.Couleur(VfxTheme.Feu, VfxRole.Coeur, new Color(1f, 0.9f, 0.4f));
        m_Blanc = VfxPalette.Accent(VfxTheme.Feu, "Blanc chaud", new Color(1f, 0.96f, 0.84f));
        m_Lumiere = Color.Lerp(m_Orange, m_Jaune, 0.3f);
    }

    void Construire()
    {
        m_N = flammes + etincelles + braisesVives;
        m_Sommets = new Vector3[m_N * LowPolyGem.VerticesPerGem];
        m_Couleurs = new Color[m_Sommets.Length];
        m_Ne = new float[m_N]; m_Vie = new float[m_N]; m_Taille = new float[m_N]; m_Phase = new float[m_N];
        m_Freq = new float[m_N]; m_Spin = new float[m_N];
        m_Base = new Vector3[m_N]; m_Vit = new Vector3[m_N]; m_Rot = new Quaternion[m_N];
        float t = Time.time;
        for (int i = 0; i < m_N; i++)
        {
            if (i < flammes) { NouvelleFlamme(i, t); m_Ne[i] = t - Random.value * m_Vie[i]; }   // déjà allumé
            else if (i < flammes + etincelles) { NouvelleEtincelle(i, t); m_Ne[i] = t + Random.Range(0f, 2f); }
            else
            {
                // braise vive posée sur le lit, à plat, palpitation propre (0,25 à 0,6 Hz)
                Vector2 p = PointDuLit(0.95f);
                m_Base[i] = new Vector3(p.x, Random.Range(-0.01f, 0.015f), p.y);
                m_Taille[i] = Random.Range(0.03f, 0.055f);
                m_Phase[i] = Random.value * 6.2832f; m_Freq[i] = Random.Range(0.25f, 0.6f);
                m_Rot[i] = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            }
        }
        m_Mesh = new Mesh { name = "Forge_Feu" };
        m_Mesh.MarkDynamic();
        m_Mesh.vertices = m_Sommets;
        m_Mesh.colors = m_Couleurs;
        m_Mesh.triangles = LowPolyGem.Triangles(m_N);
        // Bornes fixes (lit + flammes + étincelles) : pas de RecalculateBounds par image.
        m_Mesh.bounds = new Bounds(new Vector3(0f, hauteur * 1.4f, 0f), new Vector3(demiLit.x * 2f + 0.4f, hauteur * 3f + 0.4f, demiLit.y * 2f + 0.4f));
        var mf = gameObject.GetComponent<MeshFilter>(); if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = m_Mesh;
        m_Rendu = gameObject.GetComponent<MeshRenderer>(); if (m_Rendu == null) m_Rendu = gameObject.AddComponent<MeshRenderer>();
        m_Rendu.sharedMaterial = materiau;
        m_Rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_Rendu.receiveShadows = false;
        m_Rendu.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    }

    // Point tiré dans l'ellipse du lit, plus dense au centre (le feu est plus haut au milieu).
    Vector2 PointDuLit(float etendue)
    {
        float a = Random.value * 6.2832f, r = Mathf.Sqrt(Random.value) * etendue;
        return new Vector2(Mathf.Cos(a) * r * demiLit.x, Mathf.Sin(a) * r * demiLit.y);
    }

    void NouvelleFlamme(int i, float t)
    {
        Vector2 p = PointDuLit(0.85f);
        float centre = 1f - Mathf.Clamp01(p.magnitude / Mathf.Max(demiLit.x, demiLit.y));   // 1 au centre
        m_Base[i] = new Vector3(p.x, 0f, p.y);
        m_Ne[i] = t;
        m_Vie[i] = Random.Range(0.45f, 0.8f) * (0.75f + 0.35f * centre);
        m_Taille[i] = Random.Range(0.06f, 0.1f) * (0.8f + 0.35f * centre);
        m_Phase[i] = Random.value * 6.2832f; m_Freq[i] = Random.Range(1.6f, 3.2f);
        m_Spin[i] = Random.Range(-160f, 160f);
        m_Rot[i] = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
        m_Vit[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1.25f), Random.Range(-1f, 1f));   // x, z : sens du balancement ; y : élan
    }

    void NouvelleEtincelle(int i, float t)
    {
        Vector2 p = PointDuLit(0.6f);
        m_Base[i] = new Vector3(p.x, 0.02f, p.y);
        m_Ne[i] = t;
        m_Vie[i] = Random.Range(0.7f, 1.3f);
        m_Taille[i] = Random.Range(0.012f, 0.02f);
        m_Vit[i] = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.9f, 1.6f), Random.Range(-0.25f, 0.25f));
        m_Phase[i] = Random.value * 6.2832f; m_Freq[i] = Random.Range(2f, 4f);
        m_Rot[i] = Random.rotation; m_Spin[i] = Random.Range(200f, 500f);
    }

    // Souffle du feu dans [-1, 1] : deux ondulations lentes et un bruit plus vif (la lumière et les flammes le suivent).
    float Souffle(float t)
    {
        return 0.5f * Mathf.Sin(6.2832f * 0.7f * t + m_Graine) + 0.3f * Mathf.Sin(6.2832f * 1.9f * t + m_Graine * 1.7f)
             + 0.2f * (2f * Mathf.PerlinNoise(t * 6f, m_Graine) - 1f);
    }

    void LateUpdate()
    {
        if (m_Mesh == null) return;
        if (m_Version != VfxPalette.Version) Couleurs();
        float t = Time.time, s = Souffle(t);

        if (lumiere != null)
        {
            float nuit = cycle != null ? cycle.Nuit : DayCycle.Night;
            lumiere.color = m_Lumiere;
            lumiere.intensity = intensite * Mathf.Lerp(partJour, 1f, nuit) * (1f + amplitude * s);
        }
        if (braises != null && m_Bloc != null)
        {
            // Respiration lente du lit (0,35 Hz) et un peu du souffle : de 0,7 à 1,15 × l'éclat.
            float k = 0.92f + 0.16f * Mathf.Sin(6.2832f * 0.35f * t + m_Graine) + 0.07f * s;
            braises.GetPropertyBlock(m_Bloc);
            m_Bloc.SetColor(EmissionId, VfxPalette.Lueur(m_Orange, eclatBraises * k));
            braises.SetPropertyBlock(m_Bloc);
        }

        // Maillage : seulement s'il est vu, et d'assez près.
        if (m_Rendu == null || !m_Rendu.isVisible) return;
        if (m_Camera == null && Camera.main != null) m_Camera = Camera.main.transform;
        if (m_Camera != null && (m_Camera.position - transform.position).sqrMagnitude > distanceMax * distanceMax) return;

        Vector3 lumierePeinte = LowPolyGem.DefaultLight;
        float souffleHaut = 1f + 0.25f * s;
        int e0 = flammes, b0 = flammes + etincelles;
        for (int i = 0; i < m_N; i++)
        {
            if (i < e0)
            {
                float age = t - m_Ne[i];
                if (age >= m_Vie[i]) { NouvelleFlamme(i, t); age = 0f; }
                float a = age / m_Vie[i];
                // montée lente au pied puis de plus en plus vive, balancement qui grandit avec la hauteur, léger resserrement
                float h = hauteur * souffleHaut * m_Vit[i].y * Mathf.Pow(a, 1.35f);
                float danse = 0.06f * a * Mathf.Sin(6.2832f * m_Freq[i] * t + m_Phase[i]);
                Vector3 p = new Vector3(m_Base[i].x * (1f - 0.35f * a) + m_Vit[i].x * danse, h, m_Base[i].z * (1f - 0.35f * a) + m_Vit[i].z * danse);
                // taille : pleine au pied (éclosion en 0,06 s), puis la langue s'effile jusqu'à s'éteindre à la pointe
                float taille = m_Taille[i] * Mathf.Clamp01(age / 0.06f) * (1f - a) * (0.85f + 0.15f * souffleHaut);
                Color c = a < 0.35f ? Color.Lerp(m_Jaune, m_Orange, a / 0.35f)
                        : a < 0.75f ? Color.Lerp(m_Orange, m_Rouge, (a - 0.35f) / 0.4f)
                        : Color.Lerp(m_Rouge, m_Braise, (a - 0.75f) / 0.25f);
                c *= 1.3f - 0.4f * a;   // plus lumineux au pied (léger HDR : le Bloom le prend sans blanchir la teinte)
                Quaternion r = Quaternion.AngleAxis(age * m_Spin[i], Vector3.up) * m_Rot[i];
                LowPolyGem.Write(m_Sommets, m_Couleurs, i, p, taille, new Vector3(0.8f, 1.5f + 0.4f * a, 0.8f), r, c, lumierePeinte);
            }
            else if (i < b0)
            {
                float age = t - m_Ne[i];
                if (age >= m_Vie[i]) { NouvelleEtincelle(i, t + Random.Range(0f, 1.2f)); age = t - m_Ne[i]; }
                if (age < 0f) { LowPolyGem.Write(m_Sommets, m_Couleurs, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, lumierePeinte); continue; }
                float a = age / m_Vie[i];
                Vector3 v = m_Vit[i];
                Vector3 p = m_Base[i] + new Vector3(v.x * age + 0.04f * Mathf.Sin(6.2832f * m_Freq[i] * age + m_Phase[i]), v.y * age * (1f - 0.3f * a), v.z * age);
                float taille = m_Taille[i] * Mathf.Clamp01(a / 0.08f) * (1f - a);
                Color c = Color.Lerp(m_Blanc, m_Orange, a) * 1.5f;
                Quaternion r = Quaternion.AngleAxis(age * m_Spin[i], Vector3.up) * m_Rot[i];
                LowPolyGem.Write(m_Sommets, m_Couleurs, i, p, taille, Vector3.one, r, c, lumierePeinte);
            }
            else
            {
                // braise vive : palpite entre rouge et jaune, gonfle un peu quand elle rougeoie
                float k = 0.5f + 0.5f * Mathf.Sin(6.2832f * m_Freq[i] * t + m_Phase[i]);
                k = Mathf.Clamp01(k * (0.9f + 0.2f * s));
                Color c = k < 0.6f ? Color.Lerp(m_Rouge, m_Orange, k / 0.6f) : Color.Lerp(m_Orange, m_Jaune, (k - 0.6f) / 0.4f);
                c *= 0.9f + 0.8f * k;
                LowPolyGem.Write(m_Sommets, m_Couleurs, i, m_Base[i], m_Taille[i] * (0.9f + 0.15f * k), new Vector3(1.2f, 0.55f, 1f), m_Rot[i], c, lumierePeinte);
            }
        }
        m_Mesh.vertices = m_Sommets;
        m_Mesh.colors = m_Couleurs;
    }
}
