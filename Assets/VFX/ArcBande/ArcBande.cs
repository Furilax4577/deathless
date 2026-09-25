using System.Collections;
using UnityEngine;

// Arc bandé du rôdeur (25/09/2026, refait le même jour à la demande de l'utilisateur).
// - Cercle de charge : pendant qu'on bande l'arc, un anneau fin de petites gemmes (thème Chasse) apparaît à la pointe de
//   la flèche encochée, perpendiculaire à l'axe de tir, et se resserre en accélérant avec la charge (rayon =
//   max − (max − min) × charge² : lent au début, rapide à la fin) ; on lit la tension à l'œil.
// - Coup prêt : quand la charge atteint 100 %, l'anneau se verrouille sur la pointe (petit « clic » : il se contracte
//   d'un coup, éclat bref, puis reste serré), la flèche brille brièvement (émission du matériau, `dureeFlash`), le son
//   `sonPret` (bow_full_charge.wav) part au même instant et l'événement `Pret` est levé.
// - Flèche non magique : modèle KayKit arrow_bow tel quel ; en vol, seulement une traînée d'air fine et claire
//   (TraineeAir, non émissive, disparaît par la taille). Pas de lumière.
// Au relâchement, la flèche part tout droit ; sa pointe se fiche à `cible` ; `impact(point, direction)` est appelé à
// l'arrivée (le jeu décide du critique : tir à la tête → Critique).
// API : Bander(flecheEncochee), Charge (0..1), Palier (0..3), EstPret, événement Pret, Relacher(départ, cible, impact),
// Annuler().
public class ArcBande : MonoBehaviour
{
    [Tooltip("Modèle de la flèche tirée (arrow_bow).")]
    [SerializeField] private GameObject modeleFleche;
    [Tooltip("Matériau à couleurs par sommet (PortalVoxel) : anneau de charge et traînée d'air.")]
    [SerializeField] private Material materiauGemmes;
    [SerializeField] private float vitesse = 30f;
    [Tooltip("Flash de pleine charge de la flèche : durée (s) et intensité de l'émission (couleur : cœur du thème Chasse).")]
    [SerializeField] private float dureeFlash = 0.25f;
    [SerializeField] private float intensiteFlash = 2.5f;
    [Header("Cercle de charge")]
    [SerializeField] private float rayonMax = 0.42f;
    [SerializeField] private float rayonMin = 0.06f;
    [SerializeField] private int gemmesAnneau = 26;
    [SerializeField] private float tailleGemme = 0.02f;
    [Tooltip("Son joué au coup prêt (bow_full_charge.wav), au même instant que le verrouillage.")]
    [SerializeField] private AudioClip sonPret;
    [Header("Traînée d'air")]
    [SerializeField] private float longueurTrainee = 2.4f;
    [SerializeField] private float epaisseurTrainee = 0.012f;

    public event System.Action Pret;

    private GameObject encochee;
    private float charge;
    private bool pleine;
    private float instantPret = -100f;
    private float apparition;          // 0..1 : l'anneau éclot au début de la tension, se referme à l'annulation
    private bool actif;

    private Mesh mesh;
    private GameObject porteur;
    private Vector3[] v;
    private Color[] c;

    public float Charge
    {
        get { return charge; }
        set
        {
            charge = Mathf.Clamp01(value);
            if (!pleine && charge >= 0.999f)
            {
                pleine = true;
                instantPret = Time.time;
                if (encochee != null) StartCoroutine(Flash(encochee));
                if (sonPret != null) AudioSource.PlayClipAtPoint(sonPret, Pointe());
                if (Pret != null) Pret();
            }
        }
    }

    public int Palier { get { return charge >= 0.999f ? 3 : charge >= 0.66f ? 2 : charge >= 0.33f ? 1 : 0; } }
    public bool EstPret { get { return pleine; } }

    private void Awake()
    {
        if (materiauGemmes == null) return;
        porteur = new GameObject("ArcBande_Anneau");
        porteur.transform.SetParent(transform, false);
        mesh = new Mesh { name = "ArcBande_Anneau" };
        mesh.MarkDynamic();
        int n = gemmesAnneau + 4;
        v = new Vector3[n * LowPolyGem.VerticesPerGem];
        c = new Color[v.Length];
        mesh.vertices = v;
        mesh.colors = c;
        mesh.triangles = LowPolyGem.Triangles(n);
        porteur.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = porteur.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiauGemmes;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    // Commence à bander : `flecheEncochee` est la flèche tenue (l'anneau se pose à sa pointe, elle brille à 100 %).
    public void Bander(GameObject flecheEncochee)
    {
        encochee = flecheEncochee;
        charge = 0f;
        pleine = false;
        actif = true;
    }

    public void Annuler()
    {
        charge = 0f;
        pleine = false;
        actif = false;
        encochee = null;
    }

    // Pointe de la flèche encochée (le modèle arrow_bow a son axe sur +Z, pointe à +Z) et axe de tir.
    private Vector3 Pointe() { Vector3 a; return Pointe(out a); }

    private Vector3 Pointe(out Vector3 axe)
    {
        axe = transform.forward;
        if (encochee == null) return transform.position;
        MeshFilter mf = encochee.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { axe = encochee.transform.forward; return encochee.transform.position; }
        Bounds b = mf.sharedMesh.bounds;
        axe = mf.transform.forward;
        return mf.transform.TransformPoint(b.center + new Vector3(0f, 0f, b.extents.z));
    }

    private void LateUpdate()
    {
        if (mesh == null) return;
        porteur.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        apparition = Mathf.MoveTowards(apparition, actif && encochee != null ? 1f : 0f, Time.deltaTime / (actif ? 0.12f : 0.08f));
        int n = gemmesAnneau + 4;
        if (apparition <= 0f)
        {
            for (int i = 0; i < n; i++) LowPolyGem.Write(v, c, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
            mesh.vertices = v;
            mesh.colors = c;
            return;
        }
        Vector3 axe;
        Vector3 centre = Pointe(out axe) + axe * 0.03f;
        Quaternion repere = Quaternion.LookRotation(axe, Mathf.Abs(Vector3.Dot(axe, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up);
        // Rayon : se resserre en accélérant (charge²) ; au coup prêt, « clic » : contraction d'un coup puis léger rebond.
        float rayon = Mathf.Lerp(rayonMax, rayonMin, charge * charge);
        float depuis = Time.time - instantPret;
        float eclat = 0f;
        if (pleine)
        {
            float k = Mathf.Clamp01(depuis / 0.14f);
            rayon = rayonMin * (1f - 0.35f * Mathf.Sin(Mathf.PI * k));
            eclat = depuis < 0.3f ? 1f - depuis / 0.3f : 0f;
        }
        Color vif = VfxPalette.Couleur(VfxTheme.Chasse, VfxRole.Vif, new Color(0.48f, 0.55f, 0.23f));
        Color coeur = VfxPalette.Couleur(VfxTheme.Chasse, VfxRole.Coeur, new Color(0.85f, 0.7f, 0.35f));
        Color teinte = Color.Lerp(Color.Lerp(vif, coeur, 0.5f), coeur, charge) * (1f + 1.2f * eclat);
        float rotation = Time.time * (40f + 200f * charge * charge);   // tourne plus vite en se resserrant
        float taille = tailleGemme * apparition * (pleine ? 1.15f + 0.6f * eclat : 1f);
        for (int i = 0; i < gemmesAnneau; i++)
        {
            float a = (i / (float)gemmesAnneau) * 360f + rotation;
            Quaternion q = repere * Quaternion.Euler(0f, 0f, a);
            Vector3 p = centre + q * Vector3.right * rayon;
            // Gemme fine, allongée le long du cercle.
            Quaternion o = q * Quaternion.Euler(90f, 0f, 0f);
            LowPolyGem.Write(v, c, i, p, taille, new Vector3(0.45f, 0.45f, 2.2f * Mathf.Clamp(rayon / rayonMax * 1.6f, 0.6f, 1.6f)), o,
                i % 4 == 0 ? teinte * 1.15f : teinte, LowPolyGem.DefaultLight);
        }
        // Quatre crans (haut, bas, gauche, droite) : un viseur ; ils se referment avec l'anneau.
        for (int j = 0; j < 4; j++)
        {
            Quaternion q = repere * Quaternion.Euler(0f, 0f, j * 90f);
            Vector3 p = centre + q * Vector3.right * (rayon + 0.025f);
            LowPolyGem.Write(v, c, gemmesAnneau + j, p, taille * 1.3f, new Vector3(0.5f, 0.5f, 1.6f), q * Quaternion.Euler(0f, 90f, 0f),
                coeur * (1f + 1.2f * eclat), LowPolyGem.DefaultLight);
        }
        mesh.vertices = v;
        mesh.colors = c;
        mesh.RecalculateBounds();
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

    // Relâche : une flèche neuve part de `depart` vers `cible` (pointe fichée à `cible`) ; `impact(point, direction)`.
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
        TraineeAir trainee = TraineeAir.Attacher(fleche.transform, new Vector3(0f, 0f, -demi / Mathf.Max(0.001f, fleche.transform.lossyScale.z)),
            materiauGemmes, longueurTrainee, epaisseurTrainee);
        float duree = Vector3.Distance(depart, cible) / vitesse;
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            fleche.transform.position = Vector3.Lerp(depart, cible, t / duree) - dir * demi;
            yield return null;
        }
        fleche.transform.position = cible + dir * (0.1f - demi);
        if (trainee != null) trainee.Detacher();
        if (impact != null) impact(cible, dir);
        Destroy(fleche, 2f);   // plantée un instant dans la cible
    }
}
