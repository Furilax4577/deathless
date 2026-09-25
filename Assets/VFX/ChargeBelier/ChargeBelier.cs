using System.Collections.Generic;
using UnityEngine;

// Charge bélier (chevalier / paladin), créé le 25/09/2026 dans Deathless, langage gemmes de Relic (LowPolyGem, couleurs
// par sommet, shader Relic/VertexColorUnlit) :
// - Bulle : pendant la ruée, le porteur est enveloppé d'une tête de bélier en gemmes clairsemées (crâne arrondi derrière,
//   museau allongé vers l'avant, orbites, deux grandes cornes enroulées en spirale sur les côtés). Densité faible sur
//   les flancs, plus forte sur les lignes de contour (arête dorsale, flancs, mâchoire, bord arrière, museau) et les
//   cornes, pour qu'on voie le personnage au travers. Apparition en pop (0,1 s), suit le porteur, orientée sur la
//   direction de la charge (horizontale).
// - Traînée : derrière la bulle, des gemmes semées le long du trajet restent en place et s'éteignent par la taille.
// - Impact : quand le porteur a parcouru `distance` le long de `direction` (ou sur Impact()), la bulle éclate vers
//   l'avant (GemBurst.Shatter, chaque gemme part de sa place) et l'onde au sol `onde` (OndeDeChoc_ChargeBelier) part
//   devant lui.
// - (25/09/2026, retour de l'utilisateur) La tête est avancée : museau et cornes devant le porteur, le porteur dans la
//   partie arrière de la bulle (`avance`). Règles montrées : ennemis sur le chemin repoussés sur les côtés avec un court
//   étourdissement (Repousser : gerbe latérale de gemmes Sacré du côté où il part + Etourdissement court) ; à l'impact
//   final, gros étourdissement de la cible (le jeu appelle Etourdissement.Jouer, ou EtourdirCible) et éclatement + onde
//   dont l'intensité grandit avec la distance parcourue (force = parcouru / distanceMax ; dégâts proportionnels côté jeu).
// API : Jouer(porteur, direction, distance, distanceMax) ; Repousser(ennemi, duréeÉtourdi) → côté (vecteur latéral) ;
//   Impact() / Impact(force) ; EtourdirCible(cible, durée) ; Parcouru ; Force ; événement AImpact(force). La translation
//   du porteur (et des ennemis repoussés) est faite par le gameplay (ou le banc).
public class ChargeBelier : MonoBehaviour
{
    [SerializeField] private Material materiau;
    [Tooltip("Onde au sol déclenchée à l'impact (OndeDeChoc_ChargeBelier), placée devant le porteur.")]
    [SerializeField] private OndeDeChoc onde;
    [Tooltip("Dimensions de la tête (m) : longueur (arrière du crâne → bout du museau), hauteur, largeur.")]
    [SerializeField] private float longueur = 2.2f;
    [SerializeField] private float hauteur = 1.6f;
    [SerializeField] private float largeur = 1.3f;
    [SerializeField] private float tailleGemme = 0.05f;
    [Tooltip("Avancée de la tête devant le porteur (fraction de la longueur) : 0 = porteur au tiers arrière (première version), 0,22 = porteur dans la partie arrière, museau et cornes devant lui.")]
    [Range(0f, 0.4f)] [SerializeField] private float avance = 0.22f;
    [Tooltip("Étourdissement : court pour les ennemis repoussés, long pour la cible de l'impact (s).")]
    [SerializeField] private float etourdiCourt = 1f;
    [SerializeField] private float etourdiLong = 3f;
    [Tooltip("Durée du pop d'apparition (s).")]
    [SerializeField] private float pop = 0.1f;
    [Tooltip("Traînée : une salve de gemmes tous les `pasTrainee` m parcourus, durée de vie (s).")]
    [SerializeField] private float pasTrainee = 0.08f;
    [SerializeField] private int gemmesParPas = 4;
    [SerializeField] private float vieTrainee = 0.5f;
    [Tooltip("Éclatement : vitesse radiale (m/s) et vitesse vers l'avant héritée (m/s).")]
    [SerializeField] private float vitesseEclat = 3.5f;
    [SerializeField] private float elanEclat = 6f;
    [Tooltip("Sécurité : impact forcé après cette durée (s).")]
    [SerializeField] private float dureeMax = 3f;

    // Palette du chevalier / paladin : or, or sombre, bleu acier (minorité), fond d'orbite, pointe des cornes.
    public static Color Or => VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Vif, new Color(0.91f, 0.784f, 0.447f));         // #e8c872
    public static Color OrSombre => VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Base, new Color(0.722f, 0.565f, 0.227f));  // #b8903a
    public static Color Acier => VfxPalette.Accent(VfxTheme.Sacre, "Acier", new Color(0.353f, 0.478f, 0.627f));     // #5a7aa0
    public static Color Orbite => VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Ombre, new Color(0.118f, 0.165f, 0.227f));    // #1e2a3a
    public static Color OrClair => VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Coeur, new Color(0.957f, 0.886f, 0.659f));   // #f4e2a8

    private struct Gemme
    {
        public Vector3 p;
        public Color c;
        public float s;
        public Vector3 etirement;
        public Quaternion r;
        public Vector3 axe;
        public float phase;
    }

    private readonly List<Gemme> gemmes = new List<Gemme>();
    private Transform bulle;
    private Mesh maillage;
    private Vector3[] vertices;
    private Color[] colors;
    private MeshRenderer rendu;

    // Traînée (repère monde, objet à part pour rester en place).
    private const int Capacite = 600;
    private GameObject traineeObjet;
    private Mesh traineeMaillage;
    private Vector3[] tv;
    private Color[] tc;
    private readonly Vector3[] tp = new Vector3[Capacite];
    private readonly Quaternion[] tr = new Quaternion[Capacite];
    private readonly Color[] tcol = new Color[Capacite];
    private readonly float[] tne = new float[Capacite];
    private readonly float[] ts = new float[Capacite];
    private int prochain;

    private Transform porteur;
    private Vector3 direction;
    private Vector3 depart;
    private float distance;
    private float debut = -1f;
    private float parcouruEmis;
    private bool actif;
    private VfxLumiere lumiere;
    private float distanceMax;
    private float rayonOndeBase = -1f;
    private GemmesVolantes gerbes;

    public bool EnCours { get { return actif; } }
    // Distance parcourue le long de la direction depuis Jouer (m) et force d'impact (0..1) qui en découle.
    public float Parcouru { get { return actif || debut >= 0f ? Vector3.Dot(transform.position - depart, direction) : 0f; } }
    public float Force { get { return distanceMax > 0f ? Mathf.Clamp01(Parcouru / distanceMax) : 1f; } }
    public event System.Action<float> AImpact;

    private void Awake()
    {
        Construire();
    }

    private void OnDestroy()
    {
        if (maillage != null) Destroy(maillage);
        if (traineeMaillage != null) Destroy(traineeMaillage);
        if (traineeObjet != null) Destroy(traineeObjet);
    }

    // Lance la charge : la bulle apparaît autour de `porteur` (pieds), orientée sur `direction` ; l'impact part quand le
    // porteur a parcouru `distance` m le long de `direction`.
    // `distanceMax` : distance de charge qui donne la force d'impact maximale (0 : toujours pleine force).
    public void Jouer(Transform porteur, Vector3 direction, float distance, float distanceMax = 0f)
    {
        this.distanceMax = distanceMax;
        if (maillage == null) Construire();
        this.porteur = porteur;
        direction.y = 0f;
        this.direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        this.distance = distance;
        depart = porteur != null ? porteur.position : transform.position;
        debut = Time.time;
        parcouruEmis = 0f;
        actif = true;
        Suivre();
        rendu.enabled = true;
        Dessiner(0f);
        // Lumière commune des effets (thème Sacre, classe moyenne) portée par la bulle pendant la ruée.
        if (lumiere == null) lumiere = VfxLumiere.Creer(transform, new Vector3(0f, hauteur * 0.5f, 0f), VfxTheme.Sacre, VfxTailleLumiere.Moyenne, -1f);
        lumiere.Allumer();
    }

    // Impact immédiat (charge interrompue par un obstacle, par exemple) ; la force suit la distance parcourue.
    public void Impact()
    {
        Impact(Force);
    }

    // Impact de force donnée (0..1) : éclatement, onde et éclat plus forts avec la force.
    public void Impact(float force)
    {
        if (!actif) return;
        actif = false;
        force = Mathf.Clamp01(force);
        float k = 0.65f + 0.7f * force;
        Suivre();
        Vector3[] positions = new Vector3[gemmes.Count];
        Color[] couleurs = new Color[gemmes.Count];
        for (int i = 0; i < gemmes.Count; i++)
        {
            positions[i] = bulle.TransformPoint(gemmes[i].p);
            couleurs[i] = gemmes[i].c;
        }
        Vector3 centre = bulle.TransformPoint(new Vector3(0f, hauteur * 0.5f, -longueur * 0.15f));
        GemBurst eclat = GemBurst.Shatter(centre, positions, couleurs, tailleGemme, vitesseEclat * k, direction * elanEclat * k, materiau);
        // Éclat de la gerbe au thème Sacre (GemBurst est Nyxessa par défaut).
        if (eclat != null && eclat.Lumiere != null)
            eclat.Lumiere.theme = VfxTheme.Sacre;
        if (lumiere != null) lumiere.Eteindre();
        rendu.enabled = false;
        VfxLumiere.Eclat(bulle.TransformPoint(new Vector3(0f, hauteur * 0.5f, Z(0.85f))), VfxTheme.Sacre,
            force >= 0.66f ? VfxTailleLumiere.Grande : VfxTailleLumiere.Moyenne, 0.06f + 0.12f * force);
        if (onde != null)
        {
            if (rayonOndeBase < 0f) rayonOndeBase = onde.rayonMax;
            onde.rayonMax = rayonOndeBase * (0.7f + 0.5f * force);
            onde.transform.SetPositionAndRotation(transform.position + direction * Mathf.Max(0.5f, Z(0.8f)), Quaternion.LookRotation(direction));
            onde.Jouer();
        }
        if (AImpact != null) AImpact(force);
    }

    // Un ennemi traversé par la bulle : gerbe latérale de gemmes Sacré qui part du côté où il est repoussé, court
    // étourdissement. Renvoie la direction latérale (le jeu pousse l'ennemi de ce côté).
    public Vector3 Repousser(Transform ennemi, float dureeEtourdi = -1f)
    {
        if (ennemi == null) return Vector3.zero;
        Vector3 droite = Vector3.Cross(Vector3.up, direction);
        Vector3 rel = ennemi.position - (porteur != null ? porteur.position : transform.position);
        Vector3 cote = Vector3.Dot(rel, droite) >= 0f ? droite : -droite;
        if (gerbes == null)
        {
            gerbes = GemmesVolantes.Creer("ChargeBelier_Gerbes", materiau, 240);
            gerbes.transform.SetParent(transform, false);
        }
        Vector3 p = ennemi.position + Vector3.up * 0.9f - cote * 0.15f;
        for (int i = 0; i < 24; i++)
        {
            float r = Random.value;
            Color c = r < 0.4f ? Or : r < 0.7f ? OrClair * 1.2f : r < 0.9f ? OrSombre : Acier;
            Vector3 vit = cote * Random.Range(2.5f, 5f) + direction * Random.Range(0.8f, 2.5f) + Vector3.up * Random.Range(0.4f, 2.4f)
                + Random.insideUnitSphere * 0.6f;
            gerbes.Emettre(p + Random.insideUnitSphere * 0.2f, vit, Random.Range(0.045f, 0.08f), Random.Range(0.45f, 0.7f), c, 6f, 1.5f, 0.03f, 0.45f);
        }
        VfxLumiere.Eclat(p, VfxTheme.Sacre, VfxTailleLumiere.Petite, 0.04f);
        Etourdissement.Jouer(ennemi, dureeEtourdi > 0f ? dureeEtourdi : etourdiCourt, true, materiau, VfxTheme.Sacre);
        return cote;
    }

    // Gros étourdissement de la cible au bout de la trajectoire.
    public void EtourdirCible(Transform cible, float duree = -1f)
    {
        Etourdissement.Jouer(cible, duree > 0f ? duree : etourdiLong, false, materiau, VfxTheme.Sacre);
    }

    private void Suivre()
    {
        if (porteur != null)
            transform.SetPositionAndRotation(new Vector3(porteur.position.x, depart.y, porteur.position.z), Quaternion.LookRotation(direction));
    }

    private void LateUpdate()
    {
        if (actif)
        {
            Suivre();
            float age = Time.time - debut;
            Dessiner(age);
            float parcouru = Vector3.Dot(transform.position - depart, direction);
            while (parcouruEmis + pasTrainee <= parcouru)
            {
                parcouruEmis += pasTrainee;
                Semer();
            }
            if (parcouru >= distance - 0.02f || age >= dureeMax)
                Impact();
        }
        DessinerTrainee();
    }

    // ---------------------------------------------------------------- bulle

    private void Dessiner(float age)
    {
        float echelle;
        if (age < pop) echelle = Mathf.SmoothStep(0f, 1.12f, age / pop);
        else if (age < pop + 0.06f) echelle = Mathf.Lerp(1.12f, 1f, (age - pop) / 0.06f);
        else echelle = 1f;
        bulle.localScale = Vector3.one * Mathf.Max(0.001f, echelle);
        float t = Time.time;
        Vector3 lumiere = Quaternion.Inverse(bulle.rotation) * LowPolyGem.DefaultLight;
        for (int i = 0; i < gemmes.Count; i++)
        {
            Gemme g = gemmes[i];
            float frisson = 1f + 0.12f * Mathf.Sin(t * 9f + g.phase);
            Quaternion r = Quaternion.AngleAxis(t * 70f + g.phase * 40f, g.axe) * g.r;
            LowPolyGem.Write(vertices, colors, i, g.p, g.s * frisson, g.etirement, r, g.c, lumiere);
        }
        maillage.vertices = vertices;
        maillage.colors = colors;
    }

    private void Construire()
    {
        if (bulle == null)
        {
            bulle = new GameObject("Bulle").transform;
            bulle.SetParent(transform, false);
            maillage = new Mesh { name = "ChargeBelier" };
            maillage.MarkDynamic();
            bulle.gameObject.AddComponent<MeshFilter>().sharedMesh = maillage;
            rendu = bulle.gameObject.AddComponent<MeshRenderer>();
            rendu.sharedMaterial = materiau;
            rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rendu.receiveShadows = false;
            rendu.enabled = false;
        }
        GenererTete();
        vertices = new Vector3[gemmes.Count * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        maillage.Clear();
        maillage.vertices = vertices;
        maillage.colors = colors;
        maillage.triangles = LowPolyGem.Triangles(gemmes.Count);
        maillage.bounds = new Bounds(new Vector3(0f, hauteur * 0.5f, longueur * avance), new Vector3(largeur + 2f, hauteur + 1.5f, longueur + 1.5f));
        ConstruireTrainee();
    }

    // Profil de la tête le long de u (0 = arrière du crâne, 1 = bout du museau) : facteur de rayon et hauteur du centre.
    private float Rayon(float u)
    {
        float f = u < 0.35f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.35f - u) / 0.35f, 2f))) : Mathf.Lerp(1f, 0.42f, (u - 0.35f) / 0.65f);
        if (u > 0.88f) f *= Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.88f) / 0.12f, 2f)));
        return Mathf.Max(0.06f, f);
    }

    private float Z(float u) { return Mathf.Lerp(-longueur * (0.36f - avance), longueur * (0.64f + avance), u); }
    private float CentreY(float u) { return hauteur * 0.53f - hauteur * 0.16f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, u)); }

    private Vector3 Surface(float u, float theta, out Vector3 normale)
    {
        float f = Rayon(u);
        float rx = largeur * 0.5f * f, ry = hauteur * 0.48f * f;
        float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
        normale = new Vector3(c / Mathf.Max(0.01f, rx), s / Mathf.Max(0.01f, ry), 0f).normalized;
        return new Vector3(rx * c, CentreY(u) + ry * s, Z(u));
    }

    private void Ajouter(System.Random rnd, Vector3 p, Vector3 n, Color c, float taille, Vector3 etirement)
    {
        float R() { return (float)rnd.NextDouble(); }
        gemmes.Add(new Gemme
        {
            p = p,
            c = c * (0.92f + 0.16f * R()),
            s = taille,
            etirement = etirement,
            r = n.sqrMagnitude > 0.5f ? Quaternion.LookRotation(n) * Quaternion.Euler(0f, 0f, R() * 360f) : Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f),
            axe = new Vector3(R() - 0.5f, R() - 0.5f, R() - 0.5f).normalized,
            phase = R() * 10f,
        });
    }

    private Color Teinte(System.Random rnd)
    {
        double v = rnd.NextDouble();
        return v < 0.12 ? Acier : v < 0.34 ? OrSombre : Or;
    }

    private void GenererTete()
    {
        gemmes.Clear();
        System.Random rnd = new System.Random(4242);
        float R() { return (float)rnd.NextDouble(); }
        Vector3 n;
        Vector3 ecaille = new Vector3(1.2f, 1.2f, 0.5f);
        float s = tailleGemme;
        const float Deg = Mathf.Deg2Rad;
        // Orbites : sur le haut des flancs, un peu avant le milieu.
        float uOeil = 0.47f;
        Vector3 oeilD = Surface(uOeil, 28f * Deg, out n), oeilG = Surface(uOeil, 152f * Deg, out n);

        // Remplissage clairsemé des flancs (le personnage reste visible).
        for (int i = 0; i < 150; i++)
        {
            float u = 0.04f + 0.92f * R(), th = R() * Mathf.PI * 2f;
            Vector3 p = Surface(u, th, out n);
            if ((p - oeilD).sqrMagnitude < 0.06f || (p - oeilG).sqrMagnitude < 0.06f) continue;
            Ajouter(rnd, p, n, Teinte(rnd), s * (0.8f + 0.3f * R()), ecaille);
        }
        // Lignes de contour : arête dorsale, flancs (silhouette vue de face), mâchoire, dessous.
        foreach (float thDeg in new[] { 90f, 0f, 180f, -35f, -145f, -90f })
        {
            float u0 = thDeg < 0f ? 0.38f : 0.04f, u1 = 0.96f;
            int nb = thDeg == 90f ? 42 : 26;
            for (int i = 0; i < nb; i++)
            {
                float u = Mathf.Lerp(u0, u1, (i + R() * 0.6f) / nb);
                Vector3 p = Surface(u, thDeg * Deg + (R() - 0.5f) * 0.12f, out n);
                Ajouter(rnd, p, n, thDeg == 90f ? OrClair : (R() < 0.2f ? Acier : Or), s * 1.05f, ecaille);
            }
        }
        // Bord arrière du crâne et anneau du museau.
        foreach (float u in new[] { 0.07f, 0.9f })
        {
            int nb = u < 0.5f ? 30 : 22;
            for (int i = 0; i < nb; i++)
            {
                Vector3 p = Surface(u, (i + R() * 0.3f) / nb * Mathf.PI * 2f, out n);
                Ajouter(rnd, p, n, u < 0.5f ? OrSombre : Or, s, ecaille);
            }
        }
        // Naseaux : deux petits cercles sombres au bout du museau.
        float uN = 0.95f;
        for (int cote = -1; cote <= 1; cote += 2)
        {
            Vector3 centre = new Vector3(cote * largeur * 0.07f, CentreY(uN) - hauteur * 0.02f, Z(uN) + 0.02f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Ajouter(rnd, centre + new Vector3(Mathf.Cos(a) * 0.05f, Mathf.Sin(a) * 0.05f, 0f), Vector3.forward, Orbite, s * 0.7f, ecaille);
            }
        }
        // Orbites : anneau sombre enfoncé, fond presque noir, arcade sourcilière acier au-dessus.
        foreach (Vector3 oeil in new[] { oeilD, oeilG })
        {
            Vector3 no = new Vector3(Mathf.Sign(oeil.x) * 0.8f, 0.5f, 0.15f).normalized;
            Vector3 t1 = Vector3.Cross(no, Vector3.up).normalized, t2 = Vector3.Cross(t1, no).normalized;
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                Vector3 p = oeil + (t1 * Mathf.Cos(a) * 0.16f + t2 * Mathf.Sin(a) * 0.12f) - no * 0.02f;
                Ajouter(rnd, p, no, Orbite, s * 0.9f, ecaille);
            }
            for (int i = 0; i < 4; i++)
                Ajouter(rnd, oeil - no * 0.06f + (t1 * (R() - 0.5f) + t2 * (R() - 0.5f)) * 0.08f, no, Orbite, s * 0.8f, ecaille);
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.Lerp(0.15f, 0.85f, i / 8f) * Mathf.PI;
                Vector3 p = oeil + (t1 * Mathf.Cos(a) * 0.2f + t2 * (Mathf.Sin(a) * 0.17f + 0.04f)) + no * 0.04f;
                Ajouter(rnd, p, no, Acier, s * 1.1f, ecaille);
            }
        }
        // Cornes : spirales dans le plan vertical (y, z) de chaque côté, qui partent du haut du crâne vers l'arrière,
        // descendent, reviennent vers l'avant autour de l'oreille et remontent en s'affinant, en s'écartant de la tête.
        for (int cote = -1; cote <= 1; cote += 2)
        {
            Vector3 centre = new Vector3(cote * largeur * 0.35f, hauteur * 0.55f, Z(0.3f));
            const int Stations = 34, Anneau = 8;
            float r0 = hauteur * 0.3f, r1 = hauteur * 0.08f;
            float e0 = 0.14f, e1 = 0.03f;
            Vector3 prec = Vector3.zero;
            for (int k = 0; k < Stations; k++)
            {
                float sK = k / (float)(Stations - 1);
                float phi = Mathf.Lerp(80f, 520f, sK) * Deg;   // 80° : en haut ; tourne vers l'arrière, le bas, l'avant
                float r = Mathf.Lerp(r0, r1, Mathf.Pow(sK, 0.8f));
                // y = r sin(phi), z = r cos(phi) : phi 90° en haut, 180° en arrière, 270° en bas, 360° en avant.
                Vector3 c = centre + new Vector3(cote * (0.02f + 0.55f * Mathf.Sqrt(sK)), r * Mathf.Sin(phi), r * Mathf.Cos(phi));
                Vector3 tangente = k > 0 ? (c - prec).normalized : new Vector3(0f, 0f, -1f);
                prec = c;
                Vector3 b1 = Vector3.Cross(tangente, Vector3.right).normalized;
                if (b1.sqrMagnitude < 0.5f) b1 = Vector3.up;
                Vector3 b2 = Vector3.Cross(tangente, b1).normalized;
                float ep = Mathf.Lerp(e0, e1, sK);
                Color bande = sK > 0.85f ? OrClair : (k % 3 == 0 ? OrSombre : Or);
                for (int j = 0; j < Anneau; j++)
                {
                    float a = (j + (k % 2) * 0.5f) / Anneau * Mathf.PI * 2f;
                    Vector3 radial = b1 * Mathf.Cos(a) + b2 * Mathf.Sin(a);
                    Ajouter(rnd, c + radial * ep, radial, bande, s * Mathf.Lerp(1.15f, 0.7f, sK), ecaille);
                }
            }
        }
    }

    // ---------------------------------------------------------------- traînée

    private void ConstruireTrainee()
    {
        if (traineeObjet != null) return;
        traineeObjet = new GameObject("ChargeBelier_Trainee");
        traineeMaillage = new Mesh { name = "ChargeBelier_Trainee" };
        traineeMaillage.MarkDynamic();
        tv = new Vector3[Capacite * LowPolyGem.VerticesPerGem];
        tc = new Color[tv.Length];
        traineeMaillage.vertices = tv;
        traineeMaillage.colors = tc;
        traineeMaillage.triangles = LowPolyGem.Triangles(Capacite);
        traineeObjet.AddComponent<MeshFilter>().sharedMesh = traineeMaillage;
        MeshRenderer mr = traineeObjet.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        for (int i = 0; i < Capacite; i++) tne[i] = -100f;
    }

    // Sème `gemmesParPas` gemmes prises sur la bulle (surtout l'arrière et les cornes, la traînée part de là).
    private void Semer()
    {
        for (int k = 0; k < gemmesParPas; k++)
        {
            Gemme g = gemmes[Random.Range(0, gemmes.Count)];
            Vector3 p = bulle.TransformPoint(g.p) - direction * Random.Range(0f, 0.3f);
            tp[prochain] = p;
            tr[prochain] = Random.rotation;
            tcol[prochain] = g.c;
            ts[prochain] = tailleGemme * Random.Range(0.7f, 1.1f);
            tne[prochain] = Time.time;
            prochain = (prochain + 1) % Capacite;
        }
    }

    private void DessinerTrainee()
    {
        if (traineeMaillage == null) return;
        float t = Time.time;
        bool vivant = false;
        for (int i = 0; i < Capacite; i++)
        {
            float a = (t - tne[i]) / vieTrainee;
            if (a < 0f || a >= 1f)
            {
                LowPolyGem.Write(tv, tc, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            vivant = true;
            Quaternion r = Quaternion.AngleAxis(a * 180f, Vector3.up) * tr[i];
            LowPolyGem.Write(tv, tc, i, tp[i] + Vector3.up * 0.15f * a, ts[i] * (1f - a), Vector3.one, r, tcol[i], LowPolyGem.DefaultLight);
        }
        traineeMaillage.vertices = tv;
        traineeMaillage.colors = tc;
        if (vivant) traineeMaillage.RecalculateBounds();
    }
}
