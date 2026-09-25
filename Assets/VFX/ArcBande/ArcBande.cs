using System.Collections;
using UnityEngine;

// Arc bandé du rôdeur (25/09/2026, corrigé le même jour : « les flèches ne sont pas magiques »). La charge se lit par la
// tension de l'arc (blendshape Draw de bow_withString, piloté par le jeu ou le banc) et par la pose ; aucune gemme,
// aucune lueur, aucune lumière, aucune traînée. Seule exception : quand la charge atteint 100 %, la flèche encochée
// brille brièvement (`dureeFlash` s, émission du matériau, puis retour à la normale) pour annoncer le tir chargé.
// Au relâchement, la flèche (modèle KayKit arrow_bow tel quel) part tout droit ; `impact(point, direction)` est appelé
// à l'arrivée (le jeu décide du critique : tir à la tête → Critique).
// API : Bander(flecheEncochee), Charge (0..1 ; le flash part au passage à 1), Palier (0..3), Relacher(départ, cible,
// impact), Annuler().
public class ArcBande : MonoBehaviour
{
    [Tooltip("Modèle de la flèche tirée (arrow_bow).")]
    [SerializeField] private GameObject modeleFleche;
    [SerializeField] private float vitesse = 30f;
    [Tooltip("Flash de pleine charge : durée (s) et intensité de l'émission (couleur : cœur du thème Chasse).")]
    [SerializeField] private float dureeFlash = 0.25f;
    [SerializeField] private float intensiteFlash = 2.5f;

    private GameObject encochee;
    private float charge;
    private bool pleine;

    public float Charge
    {
        get { return charge; }
        set
        {
            charge = Mathf.Clamp01(value);
            if (!pleine && charge >= 0.999f)
            {
                pleine = true;
                if (encochee != null) StartCoroutine(Flash(encochee));
            }
        }
    }

    public int Palier { get { return charge >= 0.999f ? 3 : charge >= 0.66f ? 2 : charge >= 0.33f ? 1 : 0; } }

    // Commence à bander : `flecheEncochee` est la flèche tenue (celle qui brillera à pleine charge).
    public void Bander(GameObject flecheEncochee)
    {
        encochee = flecheEncochee;
        charge = 0f;
        pleine = false;
    }

    public void Annuler()
    {
        charge = 0f;
        pleine = false;
        encochee = null;
    }

    // Brille brièvement : copie du matériau avec émission, rendue au bout de `dureeFlash`.
    private IEnumerator Flash(GameObject fleche)
    {
        Renderer[] rendus = fleche.GetComponentsInChildren<Renderer>();
        Material[][] origines = new Material[rendus.Length][];
        Material[][] copies = new Material[rendus.Length][];
        Color lueur = VfxPalette.Couleur(VfxTheme.Chasse, VfxRole.Coeur, new Color(0.85f, 0.7f, 0.35f));
        for (int i = 0; i < rendus.Length; i++)
        {
            origines[i] = rendus[i].sharedMaterials;
            copies[i] = new Material[origines[i].Length];
            for (int j = 0; j < origines[i].Length; j++)
            {
                copies[i][j] = new Material(origines[i][j]);
                copies[i][j].EnableKeyword("_EMISSION");
            }
            rendus[i].sharedMaterials = copies[i];
        }
        for (float t = 0f; t < dureeFlash; t += Time.deltaTime)
        {
            float k = Mathf.Sin(Mathf.PI * t / dureeFlash);
            for (int i = 0; i < copies.Length; i++)
                foreach (Material m in copies[i]) m.SetColor("_EmissionColor", lueur * intensiteFlash * k);
            yield return null;
        }
        for (int i = 0; i < rendus.Length; i++)
        {
            if (rendus[i] != null) rendus[i].sharedMaterials = origines[i];
            foreach (Material m in copies[i]) Destroy(m);
        }
    }

    // Relâche : une flèche neuve part de `depart` vers `cible` ; `impact(point, direction)` à l'arrivée.
    public GameObject Relacher(Vector3 depart, Vector3 cible, System.Action<Vector3, Vector3> impact)
    {
        Annuler();
        GameObject fleche = modeleFleche != null ? Instantiate(modeleFleche) : new GameObject("Fleche");
        fleche.name = "ArcBande_Fleche";
        fleche.transform.SetPositionAndRotation(depart, Quaternion.LookRotation(cible - depart));
        StartCoroutine(Vol(fleche, depart, cible, impact));
        return fleche;
    }

    private IEnumerator Vol(GameObject fleche, Vector3 depart, Vector3 cible, System.Action<Vector3, Vector3> impact)
    {
        Vector3 dir = (cible - depart).normalized;
        // La pointe suit la trajectoire (pivot du modèle au milieu) et se fiche à `cible` (surface de la cible).
        MeshFilter mf = fleche.GetComponentInChildren<MeshFilter>();
        float demi = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents.z * fleche.transform.lossyScale.z : 0f;
        float duree = Vector3.Distance(depart, cible) / vitesse;
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            fleche.transform.position = Vector3.Lerp(depart, cible, t / duree) - dir * demi;
            yield return null;
        }
        fleche.transform.position = cible + dir * (0.1f - demi);
        if (impact != null) impact(cible, dir);
        Destroy(fleche, 2f);   // plantée un instant dans la cible
    }
}
