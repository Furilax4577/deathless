using UnityEngine;

// Cycle jour / nuit du village (bac à sable, Visual only, rien sur le réseau). Règles du jeu (main/Wiki/pages/deroule.md) :
// jour 120 s, crépuscule 5 s, nuit 120 s, aube 5 s ; la nuit le portail est absent (seul son socle reste) et Nyxessa
// reprend son énergie : au crépuscule le portail se referme et la charge retourne au cristal, à l'aube Nyxessa renvoie sa
// charge et le portail se rouvre (PortalVisual, alimenteParNyxessa).
//
// Le rendu de l'ambiance réutilise DayCycle (copie Visual only de Relic) : CycleJourNuit lui donne à chaque image le
// fondu (`nuit`, 0 jour -> 1 nuit) et la course du soleil (`heure`), DayCycle interpole soleil / lune, lumière ambiante,
// brouillard et ciel entre les deux préréglages de l'asset Ambiance (AmbianceVillage : jour, nuit). Par-dessus, pendant
// le crépuscule et l'aube, une teinte chaude sur le soleil, le brouillard et le ciel. Le reste suit le fondu : lumière
// de nuit de Nyxessa, lanternes des maisons, fenêtres (émission), lucioles. La brume au sol (GroundMist) lit DayCycle.Night.
//
// Nuit violette (demande de Quentin, 25/09/2026) : préréglage « nuit » d'AmbianceVillage tiré vers un bleu-violet plus
// sombre (lune, ambiante, brouillard, ciel), sol du ciel violet nuit (solCielNuit), lumière verte de Nyxessa plus forte
// pour ressortir sur ce fond ; lanternes et fenêtres restent chaudes. Brume basse GroundMist violet-gris, plus dense
// hors de la place (clearRadius). Valeurs dans Docs/vfx.md, section « Ambiance de nuit ».
public class CycleJourNuit : MonoBehaviour
{
    public enum Phase { Jour, Crepuscule, Nuit, Aube }

    [Header("Durées (s)")]
    public float dureeJour = 120f;
    public float dureeCrepuscule = 5f;
    public float dureeNuit = 120f;
    public float dureeAube = 5f;
    [Tooltip("Multiplicateur du temps du cycle (banc : 10 = un cycle en 25 s).")]
    public float vitesse = 1f;
    [Tooltip("Phase au lancement.")]
    public Phase phaseDepart = Phase.Jour;
    [Tooltip("Fige la phase courante (menus Deathless > Village > Ambiance) : crépuscule et aube sont figés à mi-course.")]
    public bool fige;

    [Header("Rendu")]
    public DayCycle dayCycle;
    public PortalVisual portail;
    [Tooltip("Lumière de nuit de Nyxessa (ponctuelle, grande portée) : la relique devient la principale source de lumière.")]
    public Light lumiereNyxessa;
    public float intensiteNyxessa = 14f;
    public Light[] lanternes;
    public float intensiteLanterne = 2.2f;
    public GameObject[] flammes;
    [Tooltip("Rendus des maisons (matériau à émission : fenêtres).")]
    public Renderer[] maisons;
    // Réduit de ×1,8 à ×1,3 (26/09/2026, luminance de nuit) : les fenêtres restent chaudes mais discrètes, sous Nyxessa.
    [ColorUsage(false, true)] public Color fenetres = new Color(1f, 0.55f, 0.22f) * 1.3f;
    public ParticleSystem lucioles;
    public float luciolesParSeconde = 5f;
    [Header("Crépuscule et aube (teinte chaude au milieu de la transition)")]
    public Color soleilChaud = new Color(1f, 0.55f, 0.32f);
    public Color brouillardChaud = new Color(0.42f, 0.28f, 0.32f);
    public Color cielChaud = new Color(0.95f, 0.45f, 0.35f);
    [Tooltip("Couleur du sol du ciel (sous l'horizon) en pleine nuit.")]
    public Color solCielNuit = new Color(0.03f, 0.018f, 0.07f);
    private Color solCielJour = new Color(0.37f, 0.35f, 0.34f);
    private bool solCielLu;

    public Phase PhaseCourante { get; private set; }
    public float TempsPhase { get; private set; }
    // Fondu courant (0 jour, 1 nuit).
    public float Nuit { get; private set; }

    private MaterialPropertyBlock bloc;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    // Lanternes réglées par LanterneLumiere (couleur, scintillement, vitres) : le cycle ne leur donne que l'allumage.
    private LanterneLumiere[] pilotes;

    private void Start()
    {
        bloc = new MaterialPropertyBlock();
        // Fenêtres : le matériau des maisons garde son mot-clé _EMISSION (drapeau GI RealtimeEmissive, VillageBuilder
        // .HouseNightMaterial) ; plus de copie du matériau par maison, l'émission passe par MaterialPropertyBlock.
        if (lanternes != null)
        {
            pilotes = new LanterneLumiere[lanternes.Length];
            for (int i = 0; i < lanternes.Length; i++) if (lanternes[i] != null) pilotes[i] = lanternes[i].GetComponentInParent<LanterneLumiere>();
        }
        PhaseCourante = phaseDepart;
        TempsPhase = fige ? Duree(phaseDepart) * 0.5f : 0f;
        Appliquer();
    }

    public float Duree(Phase p)
    {
        return p == Phase.Jour ? dureeJour : p == Phase.Crepuscule ? dureeCrepuscule : p == Phase.Nuit ? dureeNuit : dureeAube;
    }

    // Fige une phase (crépuscule et aube à mi-course).
    public void Figer(Phase p)
    {
        fige = true;
        PhaseCourante = p;
        TempsPhase = Duree(p) * 0.5f;   // jour : soleil à midi ; nuit : milieu ; crépuscule et aube : mi-course
        Appliquer();
    }

    public void Reprendre() { fige = false; }

    // Pilotage par l'horloge de la partie (main, 25/09/2026, agent gameplay) : quand `pilote` est vrai, le cycle ne compte
    // plus son temps lui-même ; Deathless.Jeu.VueCycle lui donne chaque image la phase et le temps écoulé dans la phase
    // (et règle les durées, qui peuvent être accélérées en mode test). Sans pilote, comportement du bac à sable.
    [System.NonSerialized] public bool pilote;
    // Portail ouvert quelle que soit la phase (plan de nuit du menu principal, posé par VueCycle ; jamais en partie).
    [System.NonSerialized] public bool portailForceOuvert;
    public void Piloter(Phase p, float tempsDansPhase)
    {
        pilote = true;
        fige = false;
        PhaseCourante = p;
        TempsPhase = Mathf.Max(0f, tempsDansPhase);
    }

    private void Update()
    {
        if (!fige && !pilote)
        {
            TempsPhase += Time.deltaTime * Mathf.Max(0f, vitesse);
            while (TempsPhase >= Duree(PhaseCourante))
            {
                TempsPhase -= Duree(PhaseCourante);
                PhaseCourante = (Phase)(((int)PhaseCourante + 1) % 4);
            }
        }
    }

    // Après DayCycle (Update) : on impose le fondu exact puis la teinte chaude.
    private void LateUpdate() { Appliquer(); }

    private void Appliquer()
    {
        float k = Mathf.Clamp01(TempsPhase / Mathf.Max(0.01f, Duree(PhaseCourante)));
        float n, heure;
        switch (PhaseCourante)
        {
            case Phase.Jour: n = 0f; heure = k; break;              // le soleil traverse le ciel pendant le jour
            case Phase.Crepuscule: n = k; heure = 1f; break;        // il est bas à l'ouest, la lune le remplace
            case Phase.Nuit: n = 1f; heure = 1f; break;
            default: n = 1f - k; heure = 0f; break;                 // aube : le soleil se lève à l'est
        }
        Nuit = n;
        if (dayCycle != null)
        {
            dayCycle.nuit = n;
            dayCycle.heure = heure;
            dayCycle.AppliquerImmediat();
            // teinte chaude au milieu du crépuscule et de l'aube
            Material cielN = RenderSettings.skybox;
            if (cielN != null && cielN.HasProperty("_GroundColor"))
            {
                if (!solCielLu) { solCielJour = cielN.GetColor("_GroundColor"); solCielLu = true; }
                cielN.SetColor("_GroundColor", Color.Lerp(solCielJour, solCielNuit, n));
            }
            float w = PhaseCourante == Phase.Crepuscule || PhaseCourante == Phase.Aube ? Mathf.Sin(Mathf.PI * k) : 0f;
            if (w > 0f)
            {
                Light soleil = dayCycle.GetComponent<Light>();
                soleil.color = Color.Lerp(soleil.color, soleilChaud, 0.75f * w);
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, brouillardChaud, 0.6f * w);
                Material ciel = RenderSettings.skybox;
                if (ciel != null && ciel.HasProperty("_SkyTint")) ciel.SetColor("_SkyTint", Color.Lerp(ciel.GetColor("_SkyTint"), cielChaud, 0.7f * w));
            }
        }
        // Portail présent le jour ; il se referme au crépuscule (charge rendue à Nyxessa) et se rouvre à l'aube (charge).
        if (portail != null) portail.ouvert = portailForceOuvert || PhaseCourante == Phase.Jour || PhaseCourante == Phase.Aube;

        // Lumières qui suivent la nuit : Nyxessa, lanternes (allumées dès la première moitié du crépuscule), fenêtres.
        float allume = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.7f, n));
        if (lumiereNyxessa != null)
        {
            lumiereNyxessa.enabled = n > 0.01f;
            lumiereNyxessa.intensity = intensiteNyxessa * Mathf.SmoothStep(0f, 1f, n);
            lumiereNyxessa.color = VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.62f, 0.91f, 0.44f));
        }
        if (lanternes != null)
            for (int i = 0; i < lanternes.Length; i++)
            {
                Light l = lanternes[i];
                if (l == null) continue;
                LanterneLumiere ll = pilotes != null ? pilotes[i] : null;
                if (ll != null) { ll.allumage = allume; ll.intensite = intensiteLanterne; continue; }   // couleur, scintillement ±10 %, vitres
                l.enabled = allume > 0.01f;
                l.intensity = intensiteLanterne * allume * (0.92f + 0.08f * Mathf.PerlinNoise(Time.time * 3f, l.GetInstanceID() * 0.01f));
            }
        if (flammes != null) foreach (GameObject f in flammes) if (f != null && f.activeSelf != allume > 0.3f) f.SetActive(allume > 0.3f);
        if (maisons != null && bloc != null)
            foreach (Renderer r in maisons)
            {
                if (r == null) continue;
                r.GetPropertyBlock(bloc);
                bloc.SetColor(EmissionId, fenetres * allume);
                r.SetPropertyBlock(bloc);
            }
        if (lucioles != null)
        {
            var em = lucioles.emission;
            em.rateOverTime = luciolesParSeconde * Mathf.Clamp01((n - 0.5f) * 2f);
        }
    }
}
