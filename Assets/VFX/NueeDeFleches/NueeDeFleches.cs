using System.Collections;
using UnityEngine;

// Nuée de flèches du rôdeur (25/09/2026) : marqueur de zone au sol (anneau de gemmes Chasse qui apparaît en 0,3 s),
// puis pluie de flèches (modèle KayKit arrow_bow) qui tombent dans la zone en `dureePluie` s, chacune plantée au sol
// avec une petite gerbe de terre en gemmes (thème Terre) ; les flèches restent plantées un instant puis rapetissent.
// Les flèches ne sont pas magiques : modèle KayKit tel quel, sans lueur, sans lumière, sans traînée.
// API : Jouer(centre, rayon) ; `nombre`, `dureePluie`, `hauteurDepart`, `vitesseChute` exposés.
public class NueeDeFleches : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [SerializeField] private GameObject modeleFleche;
    [SerializeField] private int nombre = 26;
    [SerializeField] private float dureePluie = 1.2f;
    [SerializeField] private float hauteurDepart = 9f;
    [SerializeField] private float vitesseChute = 26f;
    [Tooltip("Marqueur : durée d'apparition avant la pluie (s).")]
    [SerializeField] private float delaiMarqueur = 0.35f;

    private GemmesVolantes gemmes;

    private void Awake()
    {
        gemmes = GemmesVolantes.Creer("Nuee_Gemmes", materiau, 900);
        gemmes.transform.SetParent(transform, false);
    }

    public void Jouer(Vector3 centre, float rayon)
    {
        StartCoroutine(Sequence(centre, Mathf.Max(0.5f, rayon)));
    }

    private IEnumerator Sequence(Vector3 centre, float rayon)
    {
        // Marqueur : anneau de gemmes Chasse posé au sol, tenu pendant toute la pluie.
        float tenue = delaiMarqueur + dureePluie + 0.6f;
        Color ch = VfxPalette.Couleur(VfxTheme.Chasse, VfxRole.Vif, new Color(0.48f, 0.55f, 0.23f));
        Color cl = VfxPalette.Couleur(VfxTheme.Chasse, VfxRole.Coeur, new Color(0.85f, 0.7f, 0.35f));
        int n = Mathf.RoundToInt(rayon * 2f * Mathf.PI * 7f);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 p = centre + new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a)) * rayon;
            gemmes.Emettre(p, Vector3.zero, i % 3 == 0 ? 0.07f : 0.05f, tenue, i % 3 == 0 ? cl : ch, 0f, 0f, delaiMarqueur, 0.9f,
                new Vector3(0.8f, 0.5f, 1.3f), i * delaiMarqueur / n * 0.5f);
        }
        yield return new WaitForSeconds(delaiMarqueur);
        for (int i = 0; i < nombre; i++)
        {
            Vector2 d = Random.insideUnitCircle * rayon * 0.95f;
            Vector3 sol = centre + new Vector3(d.x, 0f, d.y);
            StartCoroutine(Fleche(sol, Random.Range(0f, dureePluie * 0.85f)));
        }
        yield return null;
    }

    private IEnumerator Fleche(Vector3 sol, float retard)
    {
        yield return new WaitForSeconds(retard);
        // Chute légèrement inclinée (tir en cloche), pointe vers le bas.
        Vector3 penche = new Vector3(Random.Range(-0.25f, 0.25f), -1f, Random.Range(0.1f, 0.35f)).normalized;
        Vector3 depart = sol - penche * hauteurDepart;
        GameObject f = modeleFleche != null ? Instantiate(modeleFleche) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        f.name = "Nuee_Fleche";
        f.transform.rotation = Quaternion.LookRotation(penche);
        float duree = hauteurDepart / vitesseChute;
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            f.transform.position = Vector3.Lerp(depart, sol, t / duree);
            yield return null;
        }
        // Plantée : la pointe s'enfonce de 15 cm.
        f.transform.position = sol + penche * 0.15f;
        Color t1 = VfxPalette.Couleur(VfxTheme.Terre, VfxRole.Base, new Color(0.36f, 0.25f, 0.16f));
        Color t2 = VfxPalette.Couleur(VfxTheme.Terre, VfxRole.Vif, new Color(0.54f, 0.42f, 0.28f));
        for (int k = 0; k < 7; k++)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            gemmes.Emettre(sol + Vector3.up * 0.03f, new Vector3(r.x * Random.Range(0.8f, 1.8f), Random.Range(1.5f, 2.8f), r.y * Random.Range(0.8f, 1.8f)),
                Random.Range(0.035f, 0.06f), Random.Range(0.4f, 0.6f), k % 3 == 0 ? t2 : t1, 9f, 0.5f, 0.02f, 0.4f);
        }
        yield return new WaitForSeconds(0.9f);
        Vector3 e = f.transform.localScale;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            f.transform.localScale = e * (1f - t / 0.3f);
            yield return null;
        }
        Destroy(f);
    }
}
