using UnityEngine;

// Rugissement du viking : déclencheur runtime (écrit pour Deathless le 25/09/2026 à partir de la séquence validée dans
// le bac à sable sandbox-vfx, RugissementDemo, sans la boucle ni IEffetDemo).
// Le crâne rouge en gemmes (RugissementCrane) apparaît au-dessus du personnage (pop d'échelle), la mâchoire s'ouvre
// nettement et la tête se rejette en arrière, l'onde de gemmes (RugissementOnde) part à l'ouverture et revient, la
// mâchoire se referme à son arrivée, le crâne se rétracte. Jouer() lance la séquence (relance si elle est en cours).
public class Rugissement : MonoBehaviour
{
    public RugissementCrane crane;
    public RugissementOnde onde;

    // Séquence (s) : pop 0-0,12, ouverture 0,12-0,32, onde pendant sa durée (0,9), fermeture 0,15, rétraction 0,15.
    private const float Pop = 0.12f, Ouverture = 0.2f, Fermeture = 0.15f, Retraction = 0.15f;
    private float temps = -1f;
    private bool ondeLancee;

    public float DureeTotale { get { return Pop + Ouverture + (onde != null ? onde.Duree : 0.9f) + Fermeture + Retraction; } }
    public bool EnCours { get { return temps >= 0f; } }

    private void Awake()
    {
        if (crane != null) crane.transform.localScale = Vector3.zero;
    }

    public void Jouer()
    {
        temps = 0f;
        ondeLancee = false;
        Appliquer(0f);
    }

    private void Update()
    {
        if (temps < 0f) return;
        temps += Time.deltaTime;
        if (temps >= DureeTotale)
        {
            temps = -1f;
            if (crane != null) crane.transform.localScale = Vector3.zero;
            return;
        }
        Appliquer(temps);
    }

    // Pose le crâne à l'instant `t` (s) et lance l'onde à l'ouverture de la gueule (aussi utilisable pour une capture).
    public void Appliquer(float t)
    {
        float dureeOnde = onde != null ? onde.Duree : 0.9f;
        float tOuvert = Pop + Ouverture;
        float tFerme = tOuvert + dureeOnde;
        float tFin = tFerme + Fermeture;
        float echelle, ouverture;
        if (t < Pop) echelle = Mathf.SmoothStep(0f, 1f, t / Pop) * 1.15f;
        else if (t < Pop + 0.08f) echelle = Mathf.Lerp(1.15f, 1f, (t - Pop) / 0.08f);
        else if (t < tFin) echelle = 1f;
        else echelle = 1f - Mathf.SmoothStep(0f, 1f, (t - tFin) / Retraction);
        if (t < Pop) ouverture = 0f;
        else if (t < tOuvert) ouverture = Mathf.SmoothStep(0f, 1f, (t - Pop) / Ouverture);
        else if (t < tFerme) ouverture = 1f;
        else if (t < tFin) ouverture = 1f - Mathf.SmoothStep(0f, 1f, (t - tFerme) / Fermeture);
        else ouverture = 0f;
        // Petit tremblement en fin de course d'ouverture, et tête rejetée en arrière pendant le cri.
        if (t >= tOuvert && t < tOuvert + 0.3f)
            ouverture += 0.07f * Mathf.Sin((t - tOuvert) * 45f) * (1f - (t - tOuvert) / 0.3f);
        float bascule;
        if (t < Pop) bascule = 0f;
        else if (t < tOuvert) bascule = Mathf.SmoothStep(0f, 1f, (t - Pop) / Ouverture);
        else if (t < tFerme) bascule = 1f;
        else if (t < tFin) bascule = 1f - Mathf.SmoothStep(0f, 1f, (t - tFerme) / Fermeture);
        else bascule = 0f;
        if (crane != null)
        {
            crane.transform.localScale = Vector3.one * echelle;
            crane.Ouverture = Mathf.Clamp01(ouverture);
            crane.Bascule = bascule;
        }
        if (!ondeLancee && t >= tOuvert && onde != null)
        {
            ondeLancee = true;
            onde.Jouer();
        }
    }
}
