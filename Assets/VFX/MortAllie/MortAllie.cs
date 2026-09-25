using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mort et réapparition d'un allié (joueur ou PNJ du camp des joueurs ; 25/09/2026), mêmes briques que la téléportation
// et la charge de Nyxessa :
// - Mourir : le corps se dissout comme au départ d'une téléportation (PortalTransit.Depart : les gemmes quittent le
//   corps en tourbillonnant vers un point au-dessus de lui), puis son énergie retourne à Nyxessa : un flux de gemmes
//   vertes (ChargeNyxessa) part de ce point et rejoint le cristal en arc (durée selon la distance) ; petite réaction de
//   la relique à l'arrivée (PassageJoueur : onde sur la ceinture).
// - Reapparaitre : l'inverse. Petite réaction de la relique au départ, flux du cristal vers le point de réapparition,
//   puis le personnage se recompose comme à l'arrivée d'une téléportation (PortalTransit.Arrive).
// Les rendus du personnage sont coupés pendant la dissolution et rendus à la fin de la recomposition ; l'objet reste
// actif (le jeu garde la main sur l'état). Palette Nyxessa ; lumières : celles de PortalTransit et de ChargeNyxessa.
// API : Mourir(personnage, nyxessa, fin) ; Reapparaitre(personnage, point, nyxessa, fin, recomposition) ; `fin` est appelé quand
// l'énergie est arrivée (mort) ou quand le corps est recomposé (réapparition).
public class MortAllie : MonoBehaviour
{
    [Tooltip("Matériau à couleurs par sommet (PortalVoxel).")]
    [SerializeField] private Material materiau;
    [SerializeField] private float dureeDissolution = 1.1f;
    [SerializeField] private float dureeRecomposition = 1.0f;
    [Tooltip("Vitesse du flux d'énergie (m/s) : durée = distance / vitesse, bornée.")]
    [SerializeField] private float vitesseFlux = 12f;
    [SerializeField] private Vector2 dureeFlux = new Vector2(0.6f, 2.2f);

    public static MortAllie Instance { get; private set; }

    private void Awake() { Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Mourir(GameObject personnage, Nyxessa nyxessa, System.Action fin = null)
    {
        if (personnage != null) StartCoroutine(SequenceMort(personnage, nyxessa, fin));
    }

    // `recomposition` : appelé quand le flux arrive et que le corps commence à se recomposer.
    public void Reapparaitre(GameObject personnage, Vector3 point, Nyxessa nyxessa, System.Action fin = null, System.Action recomposition = null)
    {
        if (personnage != null) StartCoroutine(SequenceReapparition(personnage, point, nyxessa, fin, recomposition));
    }

    private IEnumerator SequenceMort(GameObject personnage, Nyxessa nyxessa, System.Action fin)
    {
        Bounds corps = Volume(personnage);
        Vector3 ame = corps.center + Vector3.up * (corps.extents.y + 0.3f);
        Visible(personnage, false);
        PortalTransit.Depart(corps, ame, materiau, dureeDissolution);
        yield return new WaitForSeconds(dureeDissolution * 0.85f);
        if (nyxessa == null) { if (fin != null) fin(); yield break; }
        Vector3 cristal = nyxessa.CentreCristal;
        float d = Vector3.Distance(ame, cristal);
        ChargeNyxessa.Lancer(ame, cristal, Mathf.Clamp(d / vitesseFlux, dureeFlux.x, dureeFlux.y), materiau,
            () => { if (nyxessa != null) nyxessa.Reagir(ReactionNyxessa.PassageJoueur); if (fin != null) fin(); },
            Mathf.Clamp(d * 0.2f, 1.2f, 5f));
    }

    private IEnumerator SequenceReapparition(GameObject personnage, Vector3 point, Nyxessa nyxessa, System.Action fin, System.Action recomposition)
    {
        Visible(personnage, false);
        personnage.transform.position = point;
        yield return null;
        Bounds corps = new Bounds(point + Vector3.up * 0.95f, new Vector3(0.8f, 1.9f, 0.8f));   // capsule de joueur
        Vector3 ame = corps.center + Vector3.up * (corps.extents.y + 0.3f);
        if (nyxessa != null)
        {
            nyxessa.Reagir(ReactionNyxessa.PassageJoueur);
            Vector3 cristal = nyxessa.CentreCristal;
            float d = Vector3.Distance(ame, cristal);
            bool arrive = false;
            ChargeNyxessa.Lancer(cristal, ame, Mathf.Clamp(d / vitesseFlux, dureeFlux.x, dureeFlux.y), materiau, () => arrive = true,
                Mathf.Clamp(d * 0.2f, 1.2f, 5f));
            while (!arrive) yield return null;
        }
        PortalTransit.Arrive(corps, ame, materiau, dureeRecomposition);
        if (recomposition != null) recomposition();
        yield return new WaitForSeconds(dureeRecomposition);
        Visible(personnage, true);
        if (fin != null) fin();
    }

    // Volume du corps : rendus du personnage (armes comprises), ou capsule de joueur de 1,9 m.
    private static Bounds Volume(GameObject personnage)
    {
        Bounds b = new Bounds(personnage.transform.position + Vector3.up * 0.95f, new Vector3(0.8f, 1.9f, 0.8f));
        bool premier = true;
        foreach (Renderer r in personnage.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || !r.enabled) continue;
            if (premier) { b = r.bounds; premier = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void Visible(GameObject personnage, bool visible)
    {
        foreach (Renderer r in personnage.GetComponentsInChildren<Renderer>(true))
            if (!(r is ParticleSystemRenderer)) r.enabled = visible;
    }
}
