using UnityEngine;

// Fuite de bière du tonneau posé de travers sur le toit de la taverne « Le Tonneau Percé » (02/10/2026, ancre Tonneau_Fuite
// du prefab Assets/Art/Decor/Maisons/Taverne.prefab). Langage visuel du projet : gemmes low poly à couleurs par sommet
// (GemmesVolantes, shader Relic/VertexColorUnlit, pas d'alpha), palette du thème Biere (jaune ambré, blanc mousse).
// L'ancre est au point bas du tonneau, +Z local = sens de la descente le long des tuiles (le prefab la tourne ainsi) :
//  - un filet de gouttes ambrées glisse sur les tuiles jusqu'au bord du toit (distanceBord) où elles s'éteignent par la taille ;
//  - de la mousse blanche enfle près de la fuite et le long du filet, puis retombe ;
//  - au bord du toit, des gouttes tombent (gravité) en quelques éclats ambrés.
// Rien n'est émis quand la caméra est plus loin que distanceMax (le village se lit de loin sans cette dépense).
public class BiereFuite : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Gouttes du filet par seconde.")] public float debit = 8f;
    [Tooltip("Mousse près de la fuite, par seconde.")] public float debitMousse = 3.5f;
    [Tooltip("Longueur de la descente sur les tuiles, de l'ancre au bord du toit (m).")] public float distanceBord = 1.8f;
    [Tooltip("Vitesse de glissement sur les tuiles (m/s).")] public float vitesse = 0.7f;
    [Tooltip("Gouttes qui tombent du bord du toit, par seconde.")] public float debitGouttes = 3f;
    [Tooltip("Au-delà de cette distance à la caméra, rien n'est émis (m).")] public float distanceMax = 70f;

    private GemmesVolantes gemmes;
    private float resteFilet, resteMousse, resteGouttes;

    private static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Biere, r, d); }

    private void Start()
    {
        if (materiau == null) return;
        gemmes = GemmesVolantes.Creer("BiereFuite_Gemmes", materiau, 260);
    }

    private void OnDestroy()
    {
        if (gemmes != null) Destroy(gemmes.gameObject);
    }

    private void Update()
    {
        if (gemmes == null) return;
        Camera cam = Camera.main;
        if (cam != null && (cam.transform.position - transform.position).sqrMagnitude > distanceMax * distanceMax) return;
        float dt = Time.deltaTime;
        Vector3 desc = transform.forward, droite = transform.right, normale = transform.up;
        Vector3 origine = transform.position;
        Color ombre = C(VfxRole.Ombre, new Color(0.72f, 0.45f, 0.1f));
        Color baseC = C(VfxRole.Base, new Color(0.89f, 0.64f, 0.1f));
        Color vif = C(VfxRole.Vif, new Color(0.96f, 0.77f, 0.26f));
        Color mousse = C(VfxRole.Coeur, new Color(1f, 0.96f, 0.87f));
        Color mousseOmbre = VfxPalette.Accent(VfxTheme.Biere, "MousseOmbre", new Color(0.91f, 0.85f, 0.69f));

        // filet : gouttes ambrées qui glissent jusqu'au bord, éteintes par la taille à l'arrivée
        resteFilet += debit * dt;
        while (resteFilet >= 1f)
        {
            resteFilet -= 1f;
            float v = vitesse * Random.Range(0.8f, 1.25f);
            Vector3 p = origine + droite * Random.Range(-0.14f, 0.14f) + normale * 0.06f;
            Color c = Random.value < 0.5f ? baseC : (Random.value < 0.5f ? vif : ombre);
            gemmes.Emettre(p, desc * v, Random.Range(0.05f, 0.085f), distanceBord / v, c, 0f, 0f, 0.2f, 0.7f,
                new Vector3(0.9f, 0.9f, Random.Range(1.4f, 2.2f)));
        }
        // mousse : bulles blanches qui gonflent près de la fuite puis rétrécissent, quelques-unes le long du filet
        resteMousse += debitMousse * dt;
        while (resteMousse >= 1f)
        {
            resteMousse -= 1f;
            float t = Random.value * Random.value;   // plus dense près de la fuite
            Vector3 p = origine + desc * (t * distanceBord * 0.8f) + droite * Random.Range(-0.3f, 0.3f) + normale * Random.Range(0.06f, 0.14f);
            gemmes.Emettre(p, desc * 0.08f + normale * 0.05f, Random.Range(0.09f, 0.17f), Random.Range(1.4f, 2.2f),
                Random.value < 0.6f ? mousse : mousseOmbre, 0f, 0.5f, 0.8f, 0.45f, new Vector3(1f, 0.8f, 1f));
        }
        // gouttes au bord du toit : elles tombent
        resteGouttes += debitGouttes * dt;
        while (resteGouttes >= 1f)
        {
            resteGouttes -= 1f;
            Vector3 p = origine + desc * distanceBord + droite * Random.Range(-0.12f, 0.12f);
            gemmes.Emettre(p, desc * 0.3f, Random.Range(0.045f, 0.07f), 0.75f, Random.value < 0.5f ? baseC : vif, 9.8f, 0f, 0.05f, 0.7f,
                new Vector3(0.8f, 1.5f, 0.8f));
        }
    }
}
