using System.Collections;
using UnityEngine;

// Grenade fumigène de l'assassin (25/09/2026) : la grenade (modèle KayKit smokebomb.fbx) apparaît dans la main juste
// avant le lancer (Tenir), part en arc de `depart` à `cible` quand la main se détend (Lancer), puis un nuage de fumée low poly en gemmes gris-violet gonfle (0,5 s), reste `tenue` s en tournant lentement
// et se dissipe par la taille (0,8 s). Pas de lumière (fumée), sauf un bref éclat sombre à l'éclosion.
// API : Tenir(main) ; Lancer(depart, cible, dureeVol) ; Contient(point) (le personnage y redevient furtif : ModeFurtif) ; Actif.
public class Fumigene : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Modèle de la grenade (KayKit smokebomb.fbx).")]
    [SerializeField] private GameObject modeleGrenade;
    [SerializeField] private float rayon = 2.4f;
    [SerializeField] private float hauteur = 2.2f;
    [SerializeField] private float tenue = 4.5f;
    [SerializeField] private int bouffees = 260;

    private GemmesVolantes gemmes;
    private Vector3 centre;
    private float debut = -100f, fin = -100f;
    private GameObject grenade;

    public bool Actif { get { return Time.time >= debut && Time.time < fin; } }

    private void Awake()
    {
        gemmes = GemmesVolantes.Creer("Fumigene_Gemmes", materiau, bouffees * 3 + 40);
        gemmes.transform.SetParent(transform, false);
    }

    // Le point est-il dans le nuage (dôme de rayon `rayon`, hauteur `hauteur`) ?
    public bool Contient(Vector3 p)
    {
        if (!Actif) return false;
        Vector3 d = p - centre;
        d.y = d.y / Mathf.Max(0.1f, hauteur) * rayon;
        return d.magnitude < rayon * 0.9f;
    }

    // La grenade apparaît dans `main` (os de la main ou socket), juste avant le lancer.
    public void Tenir(Transform main)
    {
        if (grenade != null) Destroy(grenade);
        if (modeleGrenade == null || main == null) return;
        grenade = Instantiate(modeleGrenade, main, false);
        grenade.name = "Fumigene_Grenade";
        grenade.transform.localPosition = Vector3.zero;
        grenade.transform.localRotation = Quaternion.identity;
    }

    public void Lancer(Vector3 depart, Vector3 cible, float dureeVol = 0.6f)
    {
        StartCoroutine(Sequence(depart, cible, Mathf.Max(0.1f, dureeVol)));
    }

    private IEnumerator Sequence(Vector3 depart, Vector3 cible, float vol)
    {
        Color ombre = VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Ombre, new Color(0.08f, 0.04f, 0.12f));
        Color baseC = VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Base, new Color(0.17f, 0.09f, 0.25f));
        Color vif = VfxPalette.Couleur(VfxTheme.Ombre, VfxRole.Vif, new Color(0.36f, 0.23f, 0.54f));
        Color gris = VfxPalette.Accent(VfxTheme.Ombre, "Fumée", new Color(0.42f, 0.39f, 0.47f));
        // Grenade : le modèle quitte la main et suit une cloche en tournant ; il disparaît à l'impact.
        GameObject g = grenade;
        grenade = null;
        if (g == null && modeleGrenade != null) g = Instantiate(modeleGrenade);
        if (g != null) g.transform.SetParent(null, true);
        float h = Mathf.Max(1.2f, Vector3.Distance(depart, cible) * 0.35f);
        for (float t = 0f; t < vol; t += Time.deltaTime)
        {
            float k = t / vol;
            Vector3 p = Vector3.Lerp(depart, cible, k) + Vector3.up * h * 4f * k * (1f - k);
            if (g != null)
            {
                g.transform.position = p;
                g.transform.Rotate(540f * Time.deltaTime, 200f * Time.deltaTime, 0f, Space.Self);
            }
            yield return null;
        }
        if (g != null) Destroy(g);
        // Nuage : bouffées de gemmes réparties dans un dôme, qui gonflent et restent en tournant.
        centre = cible;
        debut = Time.time;
        fin = debut + 0.5f + tenue;
        float vie = 0.5f + tenue + 0.8f;
        float tenueFraction = (0.5f + tenue) / vie;
        for (int i = 0; i < bouffees; i++)
        {
            Vector3 d = Random.insideUnitSphere;
            d.y = Mathf.Abs(d.y) * hauteur / rayon;
            Vector3 p = cible + d * rayon * Random.Range(0.35f, 1f);
            Vector3 derive = new Vector3(-d.z, 0f, d.x) * Random.Range(0.05f, 0.18f) + Vector3.up * Random.Range(0f, 0.08f);
            float r = Random.value;
            // Dominante grise (fumée), teintée de violet ; le plus sombre au pied du nuage.
            Color c = r < 0.6f ? gris * Random.Range(0.85f, 1.1f) : r < 0.85f ? Color.Lerp(gris, vif, 0.35f) : r < 0.93f ? baseC : ombre;
            if (d.y < 0.25f && Random.value < 0.5f) c = Color.Lerp(c, ombre, 0.5f);
            // Bouffées aplaties (0,18-0,36 m), éclosion en 0,5 s depuis le centre (retard selon la distance).
            gemmes.Emettre(p, derive, Random.Range(0.18f, 0.36f), vie, c, 0f, 0.2f, 0.5f, tenueFraction,
                new Vector3(Random.Range(1f, 1.4f), Random.Range(0.55f, 0.8f), Random.Range(1f, 1.4f)), d.magnitude * 0.15f);
        }
        VfxLumiere.Eclat(cible + Vector3.up, VfxTheme.Ombre, VfxTailleLumiere.Petite, 0.05f);
    }
}
