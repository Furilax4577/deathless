using UnityEngine;

// Fumée de la haute cheminée de pierre de la taverne (02/10/2026, ancre Cheminee du prefab Taverne) : gemmes grises (accents
// Charbon et Cendre du thème Feu, comme la fumée à facettes de la boule de feu) qui montent lentement, enflent puis
// s'éteignent par la taille ; une brise légère les incline. Sans alpha, sans lumière. Rien n'est émis au-delà de distanceMax.
public class FumeeCheminee : MonoBehaviour
{
    public Material materiau;
    [Header("Couleurs (palette du thème : deux teintes nommées, tirage entre les deux)")]
    public VfxTheme theme = VfxTheme.Feu;
    public string teinteA = "Charbon", teinteB = "Cendre";
    [Header("Forme")]
    [Tooltip("Multiplicateur de la taille des volutes.")] public float taille = 1f;
    [Tooltip("Multiplicateur de la vitesse de montée.")] public float montee = 1f;
    [Tooltip("Demi-côté de la zone d'émission au sommet de la cheminée (m).")] public float rayon = 0.35f;
    [Tooltip("Multiplicateur de la durée de vie (la volute monte plus haut et se dissipe plus lentement).")] public float duree = 1f;
    [Tooltip("Nombre de volutes vivantes au plus.")] public int capacite = 120;
    [Tooltip("Volutes par seconde.")] public float debit = 2.2f;
    public float distanceMax = 80f;
    [Tooltip("Brise : vitesse horizontale ajoutée (m/s, repère monde).")] public Vector3 brise = new Vector3(0.35f, 0f, 0.15f);

    private GemmesVolantes gemmes;
    private float reste;

    private void Start()
    {
        if (materiau == null) return;
        gemmes = GemmesVolantes.Creer("FumeeCheminee_Gemmes", materiau, Mathf.Max(8, capacite));
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
        Color charbon = VfxPalette.Accent(theme, teinteA, new Color(0.18f, 0.16f, 0.157f));
        Color cendre = VfxPalette.Accent(theme, teinteB, new Color(0.42f, 0.38f, 0.353f));
        reste += debit * Time.deltaTime;
        while (reste >= 1f)
        {
            reste -= 1f;
            Vector3 p = transform.position + new Vector3(Random.Range(-rayon, rayon), 0f, Random.Range(-rayon, rayon));
            gemmes.Emettre(p, Vector3.up * Random.Range(0.9f, 1.4f) * montee + brise, Random.Range(0.28f, 0.45f) * taille, Random.Range(3.5f, 5f) * duree,
                Color.Lerp(charbon, cendre, Random.value), 0f, 0.25f, 1.6f, 0.35f, new Vector3(1f, 0.85f, 1f));
        }
    }
}
