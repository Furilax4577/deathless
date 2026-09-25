using UnityEngine;

public enum EtatZone { Eteinte, Annonce, Active, Extinction }

// Zone d'apparition des vagues de squelettes (25/09/2026), langage des gemmes low poly (couleurs par sommet, shader
// Relic/VertexColorUnlit, pas de transparence : tout apparaît et disparaît par la taille). Couleurs : thème Nyxessa
// (décision de l'utilisateur : « vertes comme le reste », exception à la règle du vert réservé à la relique).
// - Empreinte au sol (repère local de la zone, 4 cm au-dessus du sol) : anneau extérieur bordé d'un liseré sombre, avec
//   des encoches claires, cercle intérieur, fissures de terre qui rayonnent du cercle intérieur (bord sombre, cœur
//   lumineux), runes entre les fissures, éclats semés sur la surface. Elle respire (lueur et taille) et une onde de
//   lumière la parcourt du centre au bord à chaque Pulse().
// - Aura vers le ciel (repère monde) : colonne aérée de petites gemmes (taille des gemmes du portail) qui montent
//   depuis l'anneau et la surface en tournant ; la colonne se resserre un peu en montant et se dissipe par la taille
//   entre la moitié et le haut de `hauteurAura`.
// - Tout suit le rayon : nombre de gemmes proportionnel au périmètre (anneaux, fissures, runes, flux de l'aura) et à
//   la surface (éclats) ; les tailles de gemmes ne changent pas avec le rayon. `Rayon` se règle en temps réel.
// États : Annoncer() (crépuscule : l'empreinte se trace, l'aura monte à `niveauAnnonce`), Activer() (nuit : pleine
// intensité), Pulse() (à chaque vague), Eteindre() (aube : l'aura s'arrête, l'empreinte se résorbe).
// PointAleatoire() : point au hasard à l'intérieur du cercle (sortie de terre des squelettes).
// Lumière : VfxLumiere commune, thème Nyxessa, grande, modulée par l'état et les pulses.
public class ZoneApparition : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Rayon de la zone (m) ; les clairières d'apparition du village font environ 14 m de diamètre.")]
    [Range(2f, 20f)] [SerializeField] private float rayon = 7f;
    [Tooltip("Hauteur de l'aura (m) : assez pour dépasser la forêt vue du village.")]
    [SerializeField] private float hauteurAura = 36f;
    [SerializeField] private float dureeTrace = 3f;
    [SerializeField] private float dureeResorption = 2.5f;
    [Tooltip("Temps de montée de l'aura jusqu'au niveau d'annonce (s).")]
    [SerializeField] private float monteeAnnonce = 6f;
    [Range(0f, 1f)] [SerializeField] private float niveauAnnonce = 0.45f;
    [Tooltip("Période de la respiration de l'empreinte (s).")]
    [SerializeField] private float respiration = 3.5f;
    [Tooltip("Aura : gemmes émises par seconde et par mètre de périmètre à pleine intensité.")]
    [SerializeField] private float fluxParMetre = 1.6f;
    [Tooltip("Empreinte : gemmes par mètre de cercle.")]
    [SerializeField] private float densiteAnneau = 5f;
    [Tooltip("Empreinte : éclats par m² de surface.")]
    [SerializeField] private float densiteSurface = 0.8f;
    [SerializeField] private int graine = 7;
    [SerializeField] private bool annoncerAuDemarrage;

    private const float Sol = 0.04f;

    private EtatZone etat = EtatZone.Eteinte;
    private float trace, aura, auraCible, vitesseAura, lueur, lueurCible;
    private float dernierPulse = -100f;
    private float accumulateur;

    // Empreinte (repère local).
    private Mesh maillageEmpreinte;
    private Vector3[] vE; private Color[] cE;
    private Vector3[] ePos, eEtir; private Quaternion[] eRot; private Color[] eCol;
    private float[] eTaille, eLueur, eCle, eDist;
    private int nE;
    private float rayonConstruit = -1f;

    // Aura (repère monde).
    private Mesh maillageAura;
    private Transform aurTransform;
    private Vector3[] vA; private Color[] cA;
    private Vector3[] aPos, aVit, aEtir, aAxe; private Quaternion[] aRot; private Color[] aCol;
    private float[] aNe, aTaille, aSpin;
    private bool[] aVivant;
    private int capA, prochainA, vivantsA;

    private GemmesVolantes ondes;
    private VfxLumiere lumiere;

    public EtatZone Etat { get { return etat; } }
    public float Rayon
    {
        get { return rayon; }
        set { rayon = Mathf.Clamp(value, 2f, 20f); }
    }
    public float HauteurAura { get { return hauteurAura; } }

    public const VfxTheme Theme = VfxTheme.Nyxessa;
    private static Color C(VfxRole r, Color d) { return VfxPalette.Couleur(Theme, r, d); }
    private static Color Ombre { get { return C(VfxRole.Ombre, new Color(0.078f, 0.314f, 0.196f)); } }
    private static Color BaseC { get { return C(VfxRole.Base, new Color(0.118f, 0.353f, 0.196f)); } }
    private static Color Vif { get { return C(VfxRole.Vif, new Color(0.247f, 0.682f, 0.353f)); } }
    private static Color Coeur { get { return C(VfxRole.Coeur, new Color(0.624f, 0.91f, 0.439f)); } }
    private static Color Lueur { get { return VfxPalette.Accent(Theme, "Éclat", new Color(0.91f, 1f, 0.784f)); } }

    private void Awake()
    {
        GameObject e = new GameObject("Empreinte");
        e.transform.SetParent(transform, false);
        maillageEmpreinte = new Mesh { name = "ZoneApparition_Empreinte" };
        maillageEmpreinte.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        maillageEmpreinte.MarkDynamic();
        e.AddComponent<MeshFilter>().sharedMesh = maillageEmpreinte;
        MeshRenderer mr = e.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        GameObject a = new GameObject("Aura");
        a.transform.SetParent(transform, false);
        aurTransform = a.transform;
        maillageAura = new Mesh { name = "ZoneApparition_Aura" };
        maillageAura.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        maillageAura.MarkDynamic();
        a.AddComponent<MeshFilter>().sharedMesh = maillageAura;
        MeshRenderer ma = a.AddComponent<MeshRenderer>();
        ma.sharedMaterial = materiau;
        ma.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ma.receiveShadows = false;

        ondes = GemmesVolantes.Creer("ZoneApparition_Ondes", materiau, 600);
        ondes.transform.SetParent(transform, false);

        lumiere = VfxLumiere.Creer(transform, new Vector3(0f, 2f, 0f), Theme, VfxTailleLumiere.Grande, -1f);
        lumiere.facteur = 0f;
        Construire();
    }

    private void Start()
    {
        if (annoncerAuDemarrage) Annoncer();
    }

    private void OnDestroy()
    {
        if (maillageEmpreinte != null) Destroy(maillageEmpreinte);
        if (maillageAura != null) Destroy(maillageAura);
    }

    // ---------------------------------------------------------------- API

    // Crépuscule : l'empreinte se trace (anneau, puis cercle intérieur, fissures, runes) et l'aura monte doucement.
    public void Annoncer()
    {
        if (etat == EtatZone.Active) return;
        etat = EtatZone.Annonce;
        auraCible = niveauAnnonce;
        vitesseAura = niveauAnnonce / Mathf.Max(0.1f, monteeAnnonce);
        lueurCible = 0.15f;
    }

    // Nuit : pleine intensité (trace achevée au besoin en accéléré).
    public void Activer()
    {
        etat = EtatZone.Active;
        auraCible = 1f;
        vitesseAura = 1f / 1.5f;
        lueurCible = 0.55f;
    }

    // Aube : l'aura s'arrête (les gemmes déjà parties finissent de monter), l'empreinte se résorbe.
    public void Eteindre()
    {
        if (etat == EtatZone.Eteinte) return;
        etat = EtatZone.Extinction;
        auraCible = 0f;
        vitesseAura = 1f / 0.8f;
        lueurCible = 0f;
    }

    // À chaque vague : onde lumineuse du centre au bord, anneau de gemmes au sol, bouffée de gemmes dans l'aura, éclat.
    public void Pulse()
    {
        if (trace <= 0.05f) return;
        dernierPulse = Time.time;
        float R = rayon, P = 2f * Mathf.PI * R;
        Vector3 c = transform.position + Vector3.up * 0.15f;
        int n = Mathf.CeilToInt(P * 3f);
        Color lu = Lueur, vi = Vif;
        for (int i = 0; i < n; i++)
        {
            float ang = (i + Random.value * 0.5f) / n * Mathf.PI * 2f;
            Vector3 d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            ondes.Emettre(c + d * R * 0.28f, d * (R * 0.72f / 0.55f) + Vector3.up * Random.Range(0.2f, 0.8f), Random.Range(0.08f, 0.13f),
                0.6f, (i % 3 == 0 ? lu * 1.8f : vi * 1.4f), 0f, 1.2f, 0.04f, 0.55f, new Vector3(0.6f, 0.35f, 1.4f));
        }
        int bouffee = Mathf.CeilToInt(P * 0.8f);
        for (int i = 0; i < bouffee; i++) Naitre(1.5f);
    }

    // Coupe tout sur-le-champ, sans résorption (rechargement de scène, banc relancé).
    public void Couper()
    {
        etat = EtatZone.Eteinte;
        trace = aura = auraCible = lueur = lueurCible = 0f;
        accumulateur = 0f;
        dernierPulse = -100f;
        if (aVivant != null) for (int i = 0; i < aVivant.Length; i++) aVivant[i] = false;
        vivantsA = 0;
    }

    // Point au hasard dans la zone (à l'intérieur de l'anneau), au niveau du sol de la zone.
    public Vector3 PointAleatoire()
    {
        float r = rayon * 0.8f * Mathf.Sqrt(Random.value);
        float a = Random.value * Mathf.PI * 2f;
        return transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    // ---------------------------------------------------------------- empreinte

    private void Construire()
    {
        System.Random rng = new System.Random(graine);
        System.Func<float> rnd = () => (float)rng.NextDouble();
        System.Collections.Generic.List<Vector3> lp = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector3> le = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Quaternion> lr = new System.Collections.Generic.List<Quaternion>();
        System.Collections.Generic.List<Color> lc = new System.Collections.Generic.List<Color>();
        System.Collections.Generic.List<float> lt = new System.Collections.Generic.List<float>(), ll = new System.Collections.Generic.List<float>(), lk = new System.Collections.Generic.List<float>();
        System.Action<Vector3, float, Vector3, float, Color, float, float> ajouter = (p, lacet, etir, taille, col, lu, cle) =>
        {
            lp.Add(new Vector3(p.x, Sol + p.y, p.z));
            lr.Add(Quaternion.Euler(rnd() * 8f - 4f, lacet, rnd() * 8f - 4f));
            le.Add(etir); lt.Add(taille); lc.Add(col); ll.Add(lu); lk.Add(cle);
        };
        float R = rayon, P = 2f * Mathf.PI * R;
        Color om = Ombre, ba = BaseC, vi = Vif, co = Coeur, lu = Lueur;
        System.Func<float, float> lacetTangent = ang => -ang * Mathf.Rad2Deg;   // +Z local de la gemme le long du cercle
        System.Func<float, float> lacetRadial = ang => 90f - ang * Mathf.Rad2Deg;

        // Anneau extérieur (le plus clair : lisible de loin) et bordure intérieure.
        int n = Mathf.CeilToInt(P * densiteAnneau);
        for (int i = 0; i < n; i++)
        {
            float a = (i + rnd() * 0.3f) / n * Mathf.PI * 2f;
            float r = R + (rnd() - 0.5f) * 0.08f;
            ajouter(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), lacetTangent(a), new Vector3(0.8f, 0.22f, 1.5f), 0.16f,
                Color.Lerp(vi, co, rnd() * 0.6f), 1f, (float)i / n * 0.45f);
            float rx = R + 0.2f;   // liseré sombre à l'extérieur : l'anneau se détache du sol
            ajouter(new Vector3(Mathf.Cos(a) * rx, -0.01f, Mathf.Sin(a) * rx), lacetTangent(a), new Vector3(0.7f, 0.2f, 1.5f), 0.14f,
                Color.Lerp(om, ba, rnd() * 0.5f), 0f, (float)i / n * 0.45f);
        }
        float r2 = R - 0.45f;
        int n2 = Mathf.CeilToInt(2f * Mathf.PI * r2 * densiteAnneau * 0.8f);
        for (int i = 0; i < n2; i++)
        {
            float a = (i + rnd() * 0.3f) / n2 * Mathf.PI * 2f;
            ajouter(new Vector3(Mathf.Cos(a) * r2, 0f, Mathf.Sin(a) * r2), lacetTangent(a), new Vector3(0.55f, 0.2f, 1.4f), 0.1f,
                Color.Lerp(ba, vi, 0.3f + rnd() * 0.4f), 0.6f, 0.04f + (float)i / n2 * 0.45f);
        }
        // Encoches claires entre les deux cercles.
        int encoches = Mathf.Max(8, Mathf.RoundToInt(P / 1.1f));
        for (int i = 0; i < encoches; i++)
        {
            float a = (i + 0.5f) / encoches * Mathf.PI * 2f;
            float r = R - 0.22f;
            ajouter(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), lacetRadial(a), new Vector3(0.5f, 0.2f, 1.7f), 0.1f,
                co * 0.9f, 0.3f, 0.06f + (float)i / encoches * 0.45f);
        }
        // Cercle intérieur.
        float r1 = R * 0.28f;
        int n1 = Mathf.CeilToInt(2f * Mathf.PI * r1 * densiteAnneau * 0.8f);
        for (int i = 0; i < n1; i++)
        {
            float a = (i + rnd() * 0.3f) / n1 * Mathf.PI * 2f;
            ajouter(new Vector3(Mathf.Cos(a) * r1, 0f, Mathf.Sin(a) * r1), lacetTangent(a), new Vector3(0.55f, 0.2f, 1.4f), 0.1f,
                Color.Lerp(ba, vi, 0.5f + rnd() * 0.4f), 0.7f, 0.5f + (float)i / n1 * 0.12f);
        }
        // Fissures de terre : du cercle intérieur vers l'anneau, trajet en zigzag, bord sombre et cœur lumineux.
        int nf = Mathf.Max(5, Mathf.RoundToInt(P / 5f));
        System.Action<Vector2, float, float, float, float> fissure = null;
        fissure = (depart, cap, longueur, cle0, largeur) =>
        {
            Vector2 p = depart;
            int pas = Mathf.Max(3, Mathf.CeilToInt(longueur / 0.18f));
            float c = cap;
            for (int s = 0; s < pas; s++)
            {
                float k = (float)s / pas;
                c += (rnd() - 0.5f) * 0.55f;
                Vector2 dir = new Vector2(Mathf.Cos(c), Mathf.Sin(c));
                p += dir * (longueur / pas);
                if (p.magnitude > R - 0.6f) break;
                float lacet = 90f - Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float w = Mathf.Lerp(1.2f, 0.45f, k) * largeur;
                float cle = cle0 + k * 0.3f;
                ajouter(new Vector3(p.x, 0f, p.y), lacet, new Vector3(1f, 0.18f, 1.3f), 0.14f * w, Color.Lerp(om, ba, rnd() * 0.5f), 0f, cle);
                ajouter(new Vector3(p.x, 0.02f, p.y), lacet, new Vector3(0.5f, 0.2f, 1.6f), 0.075f * w, Color.Lerp(vi, lu, rnd() * 0.6f), 1f, cle);
                if (largeur > 0.9f && s == pas / 2 && rnd() < 0.6f)
                    fissure(p, c + (rnd() < 0.5f ? -1f : 1f) * (0.45f + rnd() * 0.3f), longueur * 0.35f, cle, 0.6f);
            }
        };
        for (int f = 0; f < nf; f++)
        {
            float a = (f + (rnd() - 0.5f) * 0.3f) / nf * Mathf.PI * 2f;
            Vector2 d0 = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            fissure(d0 * (r1 + 0.12f), a, (R - 0.6f - r1) * (0.7f + rnd() * 0.25f), 0.58f, 1f);
        }
        // Runes claires entre les fissures, sur un cercle à 62 % du rayon, tournées vers le centre.
        float rr = R * 0.62f;
        for (int f = 0; f < nf; f++)
        {
            float a = (f + 0.5f) / nf * Mathf.PI * 2f;
            Vector3 centre = new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr);
            Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), tang = new Vector3(-radial.z, 0f, radial.x);
            float lr0 = lacetRadial(a), lt0 = lacetTangent(a);
            float cle = 0.78f + (float)f / nf * 0.15f;
            Color os = Color.Lerp(co, lu, 0.4f);
            int forme = (graine + f * 7) % 4;
            // Trait principal radial.
            ajouter(centre, lr0, new Vector3(0.45f, 0.2f, 3.2f), 0.11f, os, 0.8f, cle);
            if (forme == 0) { ajouter(centre + radial * 0.12f, lt0, new Vector3(0.45f, 0.2f, 2.2f), 0.1f, os, 0.8f, cle); }
            else if (forme == 1)
            {
                ajouter(centre + radial * 0.22f + tang * 0.12f, lr0 + 40f, new Vector3(0.45f, 0.2f, 1.6f), 0.1f, os, 0.8f, cle);
                ajouter(centre + radial * 0.22f - tang * 0.12f, lr0 - 40f, new Vector3(0.45f, 0.2f, 1.6f), 0.1f, os, 0.8f, cle);
            }
            else if (forme == 2)
            {
                ajouter(centre + tang * 0.2f, lr0, new Vector3(0.45f, 0.2f, 2.2f), 0.1f, os, 0.8f, cle);
                ajouter(centre - tang * 0.2f, lr0, new Vector3(0.45f, 0.2f, 2.2f), 0.1f, os, 0.8f, cle);
            }
            else
            {
                ajouter(centre - radial * 0.22f, lt0, new Vector3(0.45f, 0.2f, 1.8f), 0.1f, os, 0.8f, cle);
                ajouter(centre + radial * 0.22f, lt0, new Vector3(0.45f, 0.2f, 1.8f), 0.1f, os, 0.8f, cle);
            }
            ajouter(centre + radial * 0.02f, 45f, new Vector3(1f, 0.3f, 1f), 0.06f, lu * 1.2f, 1f, cle + 0.03f);   // point lumineux
        }
        // Éclats semés sur la surface (densité par m²).
        int ne = Mathf.RoundToInt(Mathf.PI * R * R * densiteSurface);
        for (int i = 0; i < ne; i++)
        {
            float r = (R - 0.6f) * Mathf.Sqrt(rnd());
            float a = rnd() * Mathf.PI * 2f;
            ajouter(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), rnd() * 360f, new Vector3(0.8f + rnd() * 0.5f, 0.3f, 0.8f + rnd() * 0.5f),
                0.05f + rnd() * 0.04f, Color.Lerp(om, vi, rnd() * 0.6f), 0f, 0.3f + rnd() * 0.65f);
        }

        nE = lp.Count;
        ePos = lp.ToArray(); eEtir = le.ToArray(); eRot = lr.ToArray(); eCol = lc.ToArray();
        eTaille = lt.ToArray(); eLueur = ll.ToArray(); eCle = lk.ToArray();
        eDist = new float[nE];
        for (int i = 0; i < nE; i++) eDist[i] = new Vector2(ePos[i].x, ePos[i].z).magnitude;
        vE = new Vector3[nE * LowPolyGem.VerticesPerGem];
        cE = new Color[vE.Length];
        maillageEmpreinte.Clear();
        maillageEmpreinte.vertices = vE;
        maillageEmpreinte.colors = cE;
        maillageEmpreinte.triangles = LowPolyGem.Triangles(nE);
        maillageEmpreinte.bounds = new Bounds(Vector3.zero, new Vector3(R * 2f + 1f, 1f, R * 2f + 1f));
        rayonConstruit = rayon;
        ConstruireAura();
    }

    // ---------------------------------------------------------------- aura

    private void ConstruireAura()
    {
        float P = 2f * Mathf.PI * rayon;
        int cap = Mathf.CeilToInt(fluxParMetre * P * 3f * (hauteurAura / 3.5f)) + Mathf.CeilToInt(P * 0.8f * 3f) + 64;
        if (cap == capA) return;
        capA = cap;
        aPos = new Vector3[cap]; aVit = new Vector3[cap]; aEtir = new Vector3[cap]; aAxe = new Vector3[cap];
        aRot = new Quaternion[cap]; aCol = new Color[cap]; aNe = new float[cap]; aTaille = new float[cap]; aSpin = new float[cap];
        aVivant = new bool[cap];
        prochainA = 0; vivantsA = 0;
        vA = new Vector3[cap * LowPolyGem.VerticesPerGem];
        cA = new Color[vA.Length];
        maillageAura.Clear();
        maillageAura.vertices = vA;
        maillageAura.colors = cA;
        maillageAura.triangles = LowPolyGem.Triangles(cap);
    }

    // Une gemme d'aura : petite gemme qui monte en tournant (`elan` : multiplicateur de vitesse, pulses).
    private void Naitre(float elan = 1f)
    {
        int i = prochainA;
        prochainA = (prochainA + 1) % capA;
        if (!aVivant[i]) vivantsA++;
        float R = rayon;
        float ang = Random.value * Mathf.PI * 2f;
        float r = Random.value < 0.75f ? R * Random.Range(0.88f, 1.02f) : R * 0.9f * Mathf.Sqrt(Random.value);
        Vector3 radial = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
        Vector3 tang = new Vector3(-radial.z, 0f, radial.x);
        float montee = Random.Range(4f, 8f) * elan;
        float resserre = r * 0.35f / (hauteurAura / montee);     // la colonne se resserre de 35 % en haut
        aPos[i] = transform.position + radial * r + Vector3.up * 0.1f;
        aVit[i] = Vector3.up * montee - radial * resserre + tang * Random.Range(0.4f, 1f);
        aNe[i] = Time.time;
        aVivant[i] = true;
        aRot[i] = Random.rotation;
        aEtir[i] = new Vector3(Random.Range(0.7f, 1.1f), Random.Range(0.9f, 1.5f), Random.Range(0.7f, 1.1f));
        aTaille[i] = Random.Range(0.05f, 0.1f) * (Random.value < 0.12f ? 1.6f : 1f);   // quelques éclats un peu plus gros
        aSpin[i] = Random.Range(60f, 200f);
        aAxe[i] = Random.onUnitSphere;
        // Dégradé de la relique : base → vif → cœur → éclat (HDR léger sur les plus clairs).
        float k = Random.value;
        aCol[i] = k < 0.25f ? BaseC * 1.3f : k < 0.6f ? Vif * 1.15f : k < 0.88f ? Coeur * 1.2f : Lueur * 1.25f;
    }

    // ---------------------------------------------------------------- mise à jour

    private void Update()
    {
        if (!Mathf.Approximately(rayon, rayonConstruit)) Construire();
        float dt = Time.deltaTime;
        // Trace de l'empreinte.
        if (etat == EtatZone.Annonce) trace = Mathf.MoveTowards(trace, 1f, dt / Mathf.Max(0.1f, dureeTrace));
        else if (etat == EtatZone.Active) trace = Mathf.MoveTowards(trace, 1f, dt * 2f / Mathf.Max(0.1f, dureeTrace));
        else if (etat == EtatZone.Extinction) trace = Mathf.MoveTowards(trace, 0f, dt / Mathf.Max(0.1f, dureeResorption));
        aura = Mathf.MoveTowards(aura, auraCible, vitesseAura * dt);
        lueur = Mathf.MoveTowards(lueur, lueurCible, dt * 0.5f);
        if (etat == EtatZone.Extinction && trace <= 0f && aura <= 0f && vivantsA == 0) etat = EtatZone.Eteinte;

        // Flux de l'aura (proportionnel au périmètre).
        float P = 2f * Mathf.PI * rayon;
        float depuisPulse = Time.time - dernierPulse;
        float bouffee = depuisPulse < 0.8f ? 1f + 2f * (1f - depuisPulse / 0.8f) : 1f;
        accumulateur += fluxParMetre * P * aura * bouffee * dt;
        int n = Mathf.Min(capA / 4, Mathf.FloorToInt(accumulateur));
        accumulateur -= Mathf.FloorToInt(accumulateur);
        for (int k = 0; k < n; k++) Naitre();

        // Lumière : suit l'intensité de l'aura et de l'empreinte, éclat à chaque pulse.
        float eclat = depuisPulse < 1.2f ? 1.5f * Mathf.Exp(-depuisPulse * 4f) : 0f;
        lumiere.facteur = Mathf.Max(aura, trace * 0.35f) * (1f + eclat);
    }

    private void LateUpdate()
    {
        EcrireEmpreinte();
        EcrireAura();
    }

    private void EcrireEmpreinte()
    {
        if (nE == 0) return;
        float t = Time.time;
        float souffle = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(0.5f, respiration));
        float depuisPulse = t - dernierPulse;
        float front = depuisPulse * rayon / 0.55f;         // onde : du centre au bord en 0,55 s
        float onde = depuisPulse < 1f ? 1f - depuisPulse : 0f;
        float progres = trace * 1.15f;
        for (int i = 0; i < nE; i++)
        {
            float s = Mathf.Clamp01((progres - eCle[i]) / 0.12f);
            if (s <= 0f)
            {
                LowPolyGem.Write(vE, cE, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            s = s * s * (3f - 2f * s);
            float l = eLueur[i];
            float vague = onde > 0f ? onde * Mathf.Exp(-(eDist[i] - front) * (eDist[i] - front) * 1.5f) : 0f;
            float boost = 1f + l * (0.6f * lueur + 0.22f * souffle + 1.6f * vague);
            float taille = eTaille[i] * s * (1f + 0.08f * souffle * l + 0.35f * vague);
            LowPolyGem.Write(vE, cE, i, ePos[i], taille, eEtir[i], eRot[i], eCol[i] * boost, LowPolyGem.DefaultLight);
        }
        maillageEmpreinte.vertices = vE;
        maillageEmpreinte.colors = cE;
    }

    private void EcrireAura()
    {
        if (capA == 0) return;
        aurTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        float t = Time.time, dt = Time.deltaTime;
        float base0 = transform.position.y, H = hauteurAura;
        int vivants = 0;
        for (int i = 0; i < capA; i++)
        {
            if (!aVivant[i])
            {
                LowPolyGem.Write(vA, cA, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            aPos[i] += aVit[i] * dt;
            float h = aPos[i].y - base0;
            if (h > H)
            {
                aVivant[i] = false;
                LowPolyGem.Write(vA, cA, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            vivants++;
            float age = t - aNe[i];
            float s = Mathf.Clamp01(age / 0.25f);
            float k = Mathf.Clamp01((h - H * 0.5f) / (H * 0.5f));
            s *= 1f - k * k * (3f - 2f * k);                // dissipation en hauteur, par la taille
            Quaternion r = aSpin[i] > 0f ? Quaternion.AngleAxis(age * aSpin[i], aAxe[i]) * aRot[i] : aRot[i];
            LowPolyGem.Write(vA, cA, i, aPos[i], aTaille[i] * s, aEtir[i], r, aCol[i], LowPolyGem.DefaultLight);
        }
        vivantsA = vivants;
        maillageAura.vertices = vA;
        maillageAura.colors = cA;
        Vector3 c = transform.position;
        maillageAura.bounds = new Bounds(c + Vector3.up * H * 0.5f, new Vector3(rayon * 2f + 4f, H + 4f, rayon * 2f + 4f));
    }
}
