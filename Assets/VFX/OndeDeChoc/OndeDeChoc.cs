using UnityEngine;

// Onde de choc au sol, paramétrable (mécanique validée le 25/09/2026, rendu passé en gemmes le même jour) : un anneau
// de gemmes low poly qui s'étend (largeur qui s'amincit) et des éclats de terre en gemmes projetés en couronne, le
// tout porté par un OndeGemmes (langage gemmes de Relic : LowPolyGem, couleurs par sommet terre sombre / claire,
// shader Relic/VertexColorUnlit). `angleOuverture` à 360 : onde circulaire (saut percutant) ; plus petit : arc
// centré sur +Z local (charge bélier). Jouer() déclenche l'onde ; Appliquer(t) pose un état donné (captures).
public class OndeDeChoc : MonoBehaviour
{
    [Header("Forme")]
    public float rayonMax = 4f;
    public float duree = 0.8f;
    [Range(10f, 360f)] public float angleOuverture = 360f;
    public float largeurDepart = 0.6f;
    public float largeurFin = 0.12f;
    public float rayonDepart = 0.5f;

    [Header("Rendu en gemmes")]
    public OndeGemmes gemmes;

    private void OnValidate() { Configurer(); }
    private void Awake() { Configurer(); }

    // Reporte les paramètres sur le rendu.
    public void Configurer()
    {
        if (gemmes == null) return;
        gemmes.rayonDepart = rayonDepart;
        gemmes.rayonMax = rayonMax;
        gemmes.duree = duree;
        gemmes.angleOuverture = angleOuverture;
        gemmes.largeurDepart = largeurDepart;
        gemmes.largeurFin = largeurFin;
    }

    public void Jouer()
    {
        // Petit éclat à l'impact (lumière commune des effets, thème Terre).
        VfxLumiere.Eclat(transform.position + Vector3.up * 0.4f, VfxTheme.Terre, VfxTailleLumiere.Petite, 0.05f);
        Configurer();
        if (gemmes != null) gemmes.Jouer();
    }

    // Capture : état à `t` secondes (0 à durée).
    public void Appliquer(float t)
    {
        Configurer();
        if (gemmes != null) { if (!gemmes.EnCours) gemmes.Jouer(); gemmes.Appliquer(t); }
    }
}
