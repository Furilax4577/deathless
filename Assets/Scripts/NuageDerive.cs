using UnityEngine;

// Nuages du ciel du village (modèles KayKit cloud_big / cloud_small, opaques, low poly) : dérive lente au vent et couleur
// jour / nuit. Purement visuel et local : rien sur le réseau, chaque machine anime ses nuages.
//
// Un seul composant, posé sur le groupe des nuages (VillageBlockout/Ciel_Nuages, généré par Deathless > Niveau > Nuages),
// anime tous les nuages de la liste :
// - Dérive : chaque nuage avance dans la direction du vent (ventAzimut) à `vitesse` x son facteur propre. La zone est une
//   bande de 2 x demiLongueur le long du vent, centrée sur le groupe (le Nexus) : un nuage qui sort d'un côté revient de
//   l'autre. Pas de fondu d'alpha : dans les `bordFondu` derniers mètres de chaque bout, le nuage rétrécit jusqu'à
//   disparaître, puis regrandit en entrant de l'autre côté (à plus de 200 m, dans le brouillard).
// - Couleur : un seul matériau partagé (copie d'exécution de `materiau`, instanciée une fois au lancement, pour ne pas
//   modifier l'asset en Play dans l'éditeur ; même matériau pour tous les nuages, compatible SRP Batcher). Blanc le jour,
//   plus sombre et bleu-violet la nuit, teinte chaude au milieu du crépuscule et de l'aube.
//
// Point d'accroche au cycle jour / nuit : on lit CycleJourNuit (propriétés publiques `Nuit`, `PhaseCourante`, `TempsPhase`
// et `Duree()`, présentes dans le bac à sable comme dans main), sans rien modifier dans CycleJourNuit. Sans cycle dans la
// scène : DayCycle.Night. Pour un autre pilote : appeler Teinter(nuit, chaud) chaque image avec `cycle` à null et
// `suivreCycle` à faux.
//
// Aucune allocation par image : tableaux remplis par le générateur, matériau instancié une seule fois.
[DefaultExecutionOrder(50)]   // après CycleJourNuit (LateUpdate, ordre 0) : on lit le fondu de l'image courante
public class NuageDerive : MonoBehaviour
{
    [Header("Dérive")]
    [Tooltip("Direction vers laquelle souffle le vent (degrés, horaire depuis +Z : 90 = vers l'est).")]
    public float ventAzimut = 70f;
    [Tooltip("Vitesse moyenne du vent (m/s). Chaque nuage a son facteur (facteursVitesse).")]
    public float vitesse = 0.8f;
    [Tooltip("Demi-longueur de la zone le long du vent (m), depuis le centre du groupe.")]
    public float demiLongueur = 210f;
    [Tooltip("Distance au bout de la zone sur laquelle le nuage rétrécit jusqu'à disparaître (m).")]
    public float bordFondu = 40f;

    [Header("Nuages (remplis par le générateur)")]
    public Transform[] nuages;
    public float[] facteursVitesse;
    public Vector3[] echelles;

    [Header("Couleur jour / nuit")]
    public Material materiau;
    public bool suivreCycle = true;
    public CycleJourNuit cycle;
    [Tooltip("Test : fondu forcé (0 jour, 1 nuit) ; négatif = suit le cycle.")]
    [Range(-1f, 1f)] public float nuitForcee = -1f;
    public Color couleurJour = new Color(1f, 1f, 1f);
    [ColorUsage(false, true)] public Color emissionJour = new Color(0.20f, 0.21f, 0.24f);
    public Color couleurNuit = new Color(0.36f, 0.31f, 0.58f);
    [ColorUsage(false, true)] public Color emissionNuit = new Color(0.050f, 0.038f, 0.105f);
    [Tooltip("Teinte chaude au milieu du crépuscule et de l'aube (le soleil est déjà teinté par CycleJourNuit).")]
    public Color couleurChaude = new Color(1f, 0.70f, 0.62f);
    [ColorUsage(false, true)] public Color emissionChaude = new Color(0.20f, 0.10f, 0.10f);
    [Range(0f, 1f)] public float forceChaude = 0.55f;

    private Material m_Instance;
    private float m_Nuit = -1f, m_Chaud = -1f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (nuages == null) nuages = new Transform[0];
        int n = nuages.Length;
        if (facteursVitesse == null || facteursVitesse.Length != n)
        {
            facteursVitesse = new float[n];
            for (int i = 0; i < n; i++) facteursVitesse[i] = 1f;
        }
        if (echelles == null || echelles.Length != n)
        {
            echelles = new Vector3[n];
            for (int i = 0; i < n; i++) echelles[i] = nuages[i] != null ? nuages[i].localScale : Vector3.one;
        }
        if (materiau != null)
        {
            m_Instance = new Material(materiau);
            m_Instance.name = materiau.name + " (exécution)";
            m_Instance.EnableKeyword("_EMISSION");
            for (int i = 0; i < n; i++)
            {
                if (nuages[i] == null) continue;
                Renderer[] rs = nuages[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < rs.Length; j++) rs[j].sharedMaterial = m_Instance;
            }
        }
        if (cycle == null && suivreCycle) cycle = FindAnyObjectByType<CycleJourNuit>();
    }

    private void OnDestroy()
    {
        if (m_Instance != null) Destroy(m_Instance);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 c = transform.position;
        float a = ventAzimut * Mathf.Deg2Rad;
        Vector3 d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        float L = Mathf.Max(1f, demiLongueur);
        float bord = Mathf.Max(0.01f, bordFondu);
        for (int i = 0; i < nuages.Length; i++)
        {
            Transform t = nuages[i];
            if (t == null) continue;
            Vector3 p = t.position + d * (vitesse * facteursVitesse[i] * dt);
            float u = (p.x - c.x) * d.x + (p.z - c.z) * d.z;
            if (u > L) { p -= d * (2f * L); u -= 2f * L; }
            else if (u < -L) { p += d * (2f * L); u += 2f * L; }
            t.position = p;
            float f = Mathf.Clamp01((L - Mathf.Abs(u)) / bord);
            f = f * f * (3f - 2f * f);
            t.localScale = echelles[i] * Mathf.Max(0.01f, f);
        }
    }

    private void LateUpdate()
    {
        float n, chaud = 0f;
        if (nuitForcee >= 0f) n = nuitForcee;
        else if (suivreCycle && cycle != null)
        {
            n = cycle.Nuit;
            CycleJourNuit.Phase ph = cycle.PhaseCourante;
            if (ph == CycleJourNuit.Phase.Crepuscule || ph == CycleJourNuit.Phase.Aube)
                chaud = Mathf.Sin(Mathf.PI * Mathf.Clamp01(cycle.TempsPhase / Mathf.Max(0.01f, cycle.Duree(ph))));
        }
        else if (suivreCycle) n = DayCycle.Night;
        else return;   // piloté de l'extérieur par Teinter()
        Teinter(n, chaud);
    }

    // Couleur des nuages : `nuit` 0 jour -> 1 nuit ; `chaud` 0 -> 1 au milieu du crépuscule et de l'aube.
    public void Teinter(float nuit, float chaud)
    {
        if (m_Instance == null) return;
        nuit = Mathf.Clamp01(nuit);
        chaud = Mathf.Clamp01(chaud) * forceChaude;
        if (Mathf.Abs(nuit - m_Nuit) < 0.0005f && Mathf.Abs(chaud - m_Chaud) < 0.0005f) return;
        m_Nuit = nuit; m_Chaud = chaud;
        float s = Mathf.SmoothStep(0f, 1f, nuit);
        Color col = Color.Lerp(couleurJour, couleurNuit, s);
        Color em = Color.Lerp(emissionJour, emissionNuit, s);
        m_Instance.SetColor(BaseColorId, Color.Lerp(col, couleurChaude * col.maxColorComponent, chaud));
        m_Instance.SetColor(EmissionId, Color.Lerp(em, emissionChaude, chaud));
    }
}
