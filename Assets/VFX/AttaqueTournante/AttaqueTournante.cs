using UnityEngine;

// Attaque tournante du viking (25/09/2026), hache à deux mains : pendant la rotation, la tête de la hache sème une
// traînée circulaire de gemmes (thème Rage) qui restent en place et s'éteignent par la taille ; à chaque tour complet
// de la hache autour du viking, un petit anneau de gemmes part au sol. Lumière VfxLumiere (Rage, moyenne) portée par la
// hache pendant l'attaque.
// API : Commencer(porteur, teteDeHache), Arreter() ; Tours (nombre de tours faits).
public class AttaqueTournante : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Espacement des gemmes de la traînée (m) et leur durée de vie (s).")]
    [SerializeField] private float pas = 0.05f;
    [SerializeField] private float vieTrainee = 0.35f;
    [SerializeField] private float rayonAnneau = 1.9f;

    private GemmesVolantes gemmes;
    private VfxLumiere lumiere;
    private Transform porteur, tete;
    private Vector3 dernier;
    private float angleCumule, anglePrecedent, reste;
    private int tours;
    private bool actif;

    public int Tours { get { return tours; } }

    private static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Rage, r, d); }

    private void Awake()
    {
        gemmes = GemmesVolantes.Creer("Tournante_Gemmes", materiau, 1200);
        gemmes.transform.SetParent(transform, false);
    }

    public void Commencer(Transform porteurViking, Transform teteDeHache)
    {
        porteur = porteurViking;
        tete = teteDeHache;
        actif = porteur != null && tete != null;
        if (!actif) return;
        dernier = tete.position;
        anglePrecedent = Angle();
        angleCumule = 0f;
        reste = 0f;
        tours = 0;
        if (lumiere == null) lumiere = VfxLumiere.Creer(tete, Vector3.zero, VfxTheme.Rage, VfxTailleLumiere.Moyenne, -1f);
        lumiere.transform.SetParent(tete, false);
        lumiere.Allumer();
    }

    public void Arreter()
    {
        actif = false;
        if (lumiere != null) lumiere.Eteindre();
    }

    private float Angle()
    {
        Vector3 d = tete.position - porteur.position;
        return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }

    private void LateUpdate()
    {
        if (!actif) return;
        Vector3 p = tete.position;
        Color vif = C(VfxRole.Vif, new Color(0.7f, 0.15f, 0.12f)), coeur = C(VfxRole.Coeur, new Color(1f, 0.45f, 0.35f)), baseC = C(VfxRole.Base, new Color(0.43f, 0.08f, 0.06f));
        float d = Vector3.Distance(dernier, p);
        // Traînée : gemmes semées le long du trajet de la tête, plus claires au bord extérieur, plus grosses si la hache va vite.
        float vitesseTete = d / Mathf.Max(0.001f, Time.deltaTime);
        reste += d;
        int n = 0;
        while (reste >= pas && n < 60)
        {
            reste -= pas;
            n++;
            Vector3 q = Vector3.Lerp(dernier, p, 1f - reste / Mathf.Max(0.0001f, d));
            Vector3 dehors = q - porteur.position; dehors.y = 0f; dehors.Normalize();
            float taille = Mathf.Clamp(0.03f + vitesseTete * 0.004f, 0.03f, 0.08f);
            gemmes.Emettre(q + dehors * Random.Range(-0.1f, 0.15f) + Random.insideUnitSphere * 0.04f, dehors * 0.3f, taille, vieTrainee,
                Random.value < 0.35f ? coeur * 1.2f : Random.value < 0.7f ? vif : baseC, 0f, 1.5f, 0.02f, 0.15f, new Vector3(0.6f, 0.6f, 1.6f));
        }
        dernier = p;
        // Tours : angle cumulé de la tête autour du viking (en valeur absolue, dans un sens ou l'autre).
        float a = Angle();
        angleCumule += Mathf.DeltaAngle(anglePrecedent, a);
        anglePrecedent = a;
        if (Mathf.Abs(angleCumule) >= 360f * (tours + 1))
        {
            tours++;
            Anneau();
        }
    }

    // Petit anneau de gemmes au sol, qui s'élargit jusqu'à `rayonAnneau` et s'éteint.
    private void Anneau()
    {
        Vector3 c = porteur.position + Vector3.up * 0.06f;
        Color vif = C(VfxRole.Vif, new Color(0.7f, 0.15f, 0.12f)), sombre = C(VfxRole.Base, new Color(0.43f, 0.08f, 0.06f));
        for (int i = 0; i < 40; i++)
        {
            float a = i * Mathf.PI * 2f / 40f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            gemmes.Emettre(c + dir * 0.5f, dir * (rayonAnneau - 0.5f) / 0.4f, i % 2 == 0 ? 0.06f : 0.045f, 0.45f, i % 3 == 0 ? sombre : vif,
                0f, 2.5f, 0.03f, 0.3f, new Vector3(1.2f, 0.5f, 0.8f));
        }
    }
}
