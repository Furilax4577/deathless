using UnityEngine;

// Zone visée (créée dans Deathless le 02/10/2026) : l'indicateur au sol d'un sort de zone en cours de visée (grande boule
// de feu et mur de flammes du mage, nuée de flèches du rôdeur). Il montre à quelle taille réelle et à quel endroit le sort
// va tomber ; il suit le point visé, posé sur le relief. Visible seulement du joueur qui vise (jamais diffusé).
// Dans le langage des gemmes : un seul maillage dynamique (LowPolyGem, shader Relic/VertexColorUnlit via PortalVoxel.mat),
// pas d'alpha, apparition et disparition par la taille.
// - Cercle (rayon r) ou Ligne (longueur × largeur) : bord de petites gemmes allongées le long du contour, quatre crans
//   (cercle) ou deux bouts (ligne) plus gros, un losange au centre et un semis de braises à l'intérieur qui montre la
//   surface couverte. Un éclat tourne le long du bord.
// - Hors de portée ou sans sol : l'indicateur se tamise (Valide = faux) au lieu de disparaître.
// - Fermer(confirme) : il se resserre vers le centre (confirmé, plus vif) ou s'éteint par la taille (annulé), puis se détruit.
// Couleurs : thème donné à la création (Feu pour le mage, Chasse pour le rôdeur), lues dans VfxPalette, jamais en dur.
// API : ZoneVisee.Creer(materiau, theme, forme, rayonOuLongueur, largeur) ; Placer(centre, axe) chaque image ; Valide ; Fermer.
public class ZoneVisee : MonoBehaviour
{
    public enum Forme { Cercle, Ligne }

    private enum Role { Bord, Cran, Centre, Braise }

    private struct Gemme
    {
        public Vector2 local;      // position dans le repère de la zone (x : le long de l'axe, y : en travers), mètres
        public float taille;
        public Vector3 etirement;
        public Role role;
        public float phase;        // 0..1 : place le long du contour (pour l'éclat qui court)
        public float angle;        // orientation de l'allongement dans le plan (radians)
        public bool sombre;        // gemme de contraste (rouge sombre, plus large) entre les gemmes vives du bord
    }

    private const float Apparition = 0.14f;
    private const float Fermeture = 0.16f;

    private VfxTheme theme;
    private Forme forme;
    private float rayon, longueur, largeur;
    private Gemme[] gemmes;
    private float[] hauteurs;       // relief sous chaque gemme (relatif au centre), lissé
    private Mesh mesh;
    private Vector3[] sommets;
    private Color[] couleurs;
    private Vector3 centre, axe = Vector3.forward;
    private bool place;
    private float debut, finDepuis = -1f;
    private bool confirme;
    private float valide = 1f, valideVoulu = 1f;
    private int imageHauteurs;
    private bool proprietaireVu;

    private static readonly RaycastHit[] hits = new RaycastHit[8];

    /// Colliders qui ne comptent pas pour le relief (personnages, troncs de la forêt, étages masqués du donjon) : posé par le
    /// jeu, l'effet reste sans dépendance au gameplay.
    public System.Func<Collider, bool> ignorerCollider;
    /// Qui vise : si ce composant est détruit (changement de scène, mort du héros), la zone disparaît avec lui.
    public Component proprietaire;

    public static ZoneVisee Creer(Material materiau, VfxTheme theme, Forme forme, float rayonOuLongueur, float largeur)
    {
        if (materiau == null) return null;
        GameObject go = new GameObject("ZoneVisee");
        ZoneVisee z = go.AddComponent<ZoneVisee>();
        z.theme = theme;
        z.forme = forme;
        z.rayon = forme == Forme.Cercle ? Mathf.Max(0.5f, rayonOuLongueur) : 0f;
        z.longueur = forme == Forme.Ligne ? Mathf.Max(1f, rayonOuLongueur) : 0f;
        z.largeur = Mathf.Max(0.4f, largeur);
        z.Construire(materiau);
        z.debut = Time.time;
        return z;
    }

    /// Le point visé est bon (dans la portée, sur du sol) : faux, l'indicateur se tamise.
    public bool Valide { set { valideVoulu = value ? 1f : 0f; } }

    /// Place la zone : `c` le centre (au sol), `a` l'axe de la ligne (ignoré pour un cercle).
    public void Placer(Vector3 c, Vector3 a)
    {
        if (finDepuis >= 0f) return;
        a.y = 0f;
        if (a.sqrMagnitude > 0.0001f) axe = a.normalized;
        if (!place) { centre = c; place = true; ActualiserHauteurs(true); }
        else centre = c;
    }

    /// Referme la zone puis la détruit : `confirme` vrai (le sort part : elle se resserre, plus vive), faux (annulée : elle s'éteint).
    public void Fermer(bool confirme)
    {
        if (finDepuis >= 0f) return;
        this.confirme = confirme;
        finDepuis = Time.time;
    }

    private void Construire(Material materiau)
    {
        System.Collections.Generic.List<Gemme> liste = new System.Collections.Generic.List<Gemme>();
        System.Random alea = new System.Random(1702);   // semis fixe (générateur à part : le hasard du jeu n'est pas touché)
        System.Func<float, float, float> tirer = (a, b) => a + (float)alea.NextDouble() * (b - a);
        if (forme == Forme.Cercle)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(rayon * 2f * Mathf.PI * 3f), 24, 130);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, a = t * Mathf.PI * 2f;
                bool cran = i % Mathf.Max(1, n / 4) == 0;
                liste.Add(new Gemme
                {
                    local = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rayon,
                    taille = cran ? 0.2f : (i % 2 == 0 ? 0.13f : 0.15f), sombre = !cran && i % 2 != 0,
                    etirement = cran ? new Vector3(0.6f, 0.5f, 1.9f) : (i % 2 == 0 ? new Vector3(0.55f, 0.45f, 2.3f) : new Vector3(0.8f, 0.3f, 2f)),
                    role = cran ? Role.Cran : Role.Bord, phase = t, angle = a + Mathf.PI * 0.5f
                });
            }
            // Cercle intérieur à mi-rayon (zone pleine) puis semis de braises.
            int ni = Mathf.Clamp(Mathf.RoundToInt(rayon * Mathf.PI * 1.2f), 8, 40);
            for (int i = 0; i < ni; i++)
            {
                float a = i / (float)ni * Mathf.PI * 2f + 0.3f;
                liste.Add(new Gemme { local = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rayon * 0.5f, taille = 0.09f, etirement = new Vector3(0.55f, 0.45f, 2f), role = Role.Braise, phase = i / (float)ni, angle = a + Mathf.PI * 0.5f });
            }
            int nb = Mathf.Clamp(Mathf.RoundToInt(rayon * rayon * Mathf.PI * 0.4f), 8, 64);
            for (int i = 0; i < nb; i++)
            {
                float ang = tirer(0f, Mathf.PI * 2f), dist = Mathf.Sqrt(tirer(0f, 1f)) * rayon * 0.9f;
                Vector2 p = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                liste.Add(new Gemme { local = p, taille = tirer(0.07f, 0.11f), etirement = new Vector3(tirer(0.8f, 1.1f), tirer(0.5f, 0.8f), tirer(0.8f, 1.1f)), role = Role.Braise, phase = tirer(0f, 1f), angle = tirer(0f, Mathf.PI) });
            }
        }
        else
        {
            float dl = longueur * 0.5f, dw = largeur * 0.5f;
            int nl = Mathf.Max(4, Mathf.RoundToInt(longueur / 0.45f));
            for (int i = 0; i <= nl; i++)
            {
                float x = Mathf.Lerp(-dl, dl, i / (float)nl);
                float t = i / (float)nl;
                liste.Add(new Gemme { local = new Vector2(x, dw), taille = i % 2 == 0 ? 0.13f : 0.15f, sombre = i % 2 != 0, etirement = i % 2 == 0 ? new Vector3(0.55f, 0.45f, 2.3f) : new Vector3(0.8f, 0.3f, 2f), role = Role.Bord, phase = t * 0.5f, angle = 0f });
                liste.Add(new Gemme { local = new Vector2(x, -dw), taille = i % 2 == 0 ? 0.13f : 0.15f, sombre = i % 2 != 0, etirement = i % 2 == 0 ? new Vector3(0.55f, 0.45f, 2.3f) : new Vector3(0.8f, 0.3f, 2f), role = Role.Bord, phase = 1f - t * 0.5f, angle = 0f });
            }
            int nw = Mathf.Max(2, Mathf.RoundToInt(largeur / 0.45f));
            for (int j = 1; j < nw; j++)
            {
                float y = Mathf.Lerp(-dw, dw, j / (float)nw);
                liste.Add(new Gemme { local = new Vector2(dl, y), taille = 0.11f, etirement = new Vector3(0.5f, 0.4f, 2.2f), role = Role.Bord, phase = 0.5f, angle = Mathf.PI * 0.5f });
                liste.Add(new Gemme { local = new Vector2(-dl, y), taille = 0.11f, etirement = new Vector3(0.5f, 0.4f, 2.2f), role = Role.Bord, phase = 0f, angle = Mathf.PI * 0.5f });
            }
            // Bouts du mur : deux crans plus gros aux quatre coins.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    liste.Add(new Gemme { local = new Vector2(sx * dl, sy * dw), taille = 0.2f, etirement = new Vector3(0.6f, 0.5f, 1.6f), role = Role.Cran, phase = 0.25f, angle = 0f });
            // Ligne centrale : le trait du mur.
            int nc = Mathf.Max(6, Mathf.RoundToInt(longueur / 0.6f));
            for (int i = 0; i <= nc; i++)
                liste.Add(new Gemme { local = new Vector2(Mathf.Lerp(-dl, dl, i / (float)nc), 0f), taille = 0.09f, etirement = new Vector3(0.55f, 0.45f, 2.2f), role = Role.Braise, phase = i / (float)nc, angle = 0f });
        }
        // Losange au centre.
        liste.Add(new Gemme { local = Vector2.zero, taille = 0.24f, etirement = new Vector3(0.7f, 1.5f, 0.7f), role = Role.Centre, phase = 0f, angle = 0f });

        gemmes = liste.ToArray();
        hauteurs = new float[gemmes.Length];
        sommets = new Vector3[gemmes.Length * LowPolyGem.VerticesPerGem];
        couleurs = new Color[sommets.Length];
        mesh = new Mesh { name = "ZoneVisee" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = sommets;
        mesh.colors = couleurs;
        mesh.triangles = LowPolyGem.Triangles(gemmes.Length);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    private Vector3 Monde(Vector2 l, float spin)
    {
        float c = Mathf.Cos(spin), s = Mathf.Sin(spin);
        Vector2 r = new Vector2(l.x * c - l.y * s, l.x * s + l.y * c);
        Vector3 travers = Vector3.Cross(Vector3.up, axe);
        return centre + axe * r.x + travers * r.y;
    }

    // Relief sous chaque gemme : un rayon vers le bas depuis un peu au-dessus du centre ; les personnages, les troncs de la
    // forêt et les étages masqués du donjon ne comptent pas. Sans sol trouvé, la hauteur du centre.
    private void ActualiserHauteurs(bool tout)
    {
        float spin = forme == Forme.Cercle ? Time.time * 0.25f : 0f;
        for (int i = 0; i < gemmes.Length; i++)
        {
            Vector3 p = Monde(gemmes[i].local, spin);
            float h = 0f;
            int n = Physics.RaycastNonAlloc(new Vector3(p.x, centre.y + 2.2f, p.z), Vector3.down, hits, 9f, ~0, QueryTriggerInteraction.Ignore);
            float meilleur = float.MaxValue;
            for (int k = 0; k < n; k++)
            {
                RaycastHit hit = hits[k];
                if (hit.distance >= meilleur) continue;
                Collider c = hit.collider;
                if (ignorerCollider != null && ignorerCollider(c)) continue;
                meilleur = hit.distance;
                h = hit.point.y - centre.y;
            }
            hauteurs[i] = tout ? h : Mathf.Lerp(hauteurs[i], h, 0.6f);
        }
    }

    private static Color C(VfxTheme t, VfxRole r, Color d) { return VfxPalette.Couleur(t, r, d); }

    private void LateUpdate()
    {
        if (mesh == null) return;
        if (proprietaire != null) proprietaireVu = true;
        if (proprietaireVu && proprietaire == null) { Destroy(gameObject); return; }
        if (!place) return;
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        float t = Time.time - debut;
        float k = Mathf.Clamp01(t / Apparition);                       // éclosion
        float sortie = 0f;                                              // fermeture
        if (finDepuis >= 0f)
        {
            sortie = Mathf.Clamp01((Time.time - finDepuis) / Fermeture);
            if (sortie >= 1f) { Destroy(gameObject); return; }
        }
        valide = Mathf.MoveTowards(valide, valideVoulu, Time.deltaTime * 6f);
        if ((++imageHauteurs & 1) == 0) ActualiserHauteurs(false);

        float intens = Mathf.Clamp(VfxPalette.Intensite(theme, 1.4f), 1f, 1.3f);   // émission du thème, plafonnée : au-delà, les teintes claires se délavent en blanc
        Color ombre = C(theme, VfxRole.Ombre, new Color(0.29f, 0.07f, 0.02f));
        Color baseC = C(theme, VfxRole.Base, new Color(0.8f, 0.12f, 0.03f));
        Color vif = C(theme, VfxRole.Vif, new Color(1f, 0.38f, 0.04f));
        Color coeur = C(theme, VfxRole.Coeur, new Color(1f, 0.9f, 0.4f));
        // Teintes de contraste (gemmes sombres du bord, braises) : le rouge sombre du Feu ; pour la Chasse, dont l'ombre et la base
        // sont des verts de sous-bois qui se fondent dans l'herbe, l'ocre de sa palette (accent).
        if (theme == VfxTheme.Chasse)
        {
            baseC = VfxPalette.Accent(theme, "ocre", new Color(0.66f, 0.45f, 0.18f));
            ombre = baseC * 0.55f;
        }
        Color clair = Color.Lerp(vif, coeur, 0.55f);   // gemmes vives du bord : entre le vif et le cœur du thème (se lit sur l'herbe comme sur la pierre)
        // Zone tamisée hors de portée : retour vers l'ombre du thème (pas d'alpha).
        float spin = forme == Forme.Cercle ? Time.time * 0.25f : 0f;
        float echelle = Mathf.SmoothStep(0f, 1f, k);
        // Fermeture : confirmée, la zone se resserre vers le centre ; annulée, elle s'éteint sur place.
        float serre = confirme ? 1f - 0.35f * sortie : 1f;
        float vivacite = confirme ? 1f + 0.9f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, sortie * 1.4f)) : 1f;
        float fondu = (1f - sortie) * echelle;
        float course = t * 0.7f;   // l'éclat qui court le long du bord (tours par seconde)

        for (int i = 0; i < gemmes.Length; i++)
        {
            Gemme g = gemmes[i];
            Vector2 l = g.local * (serre * (0.85f + 0.15f * echelle));
            Vector3 p = Monde(l, spin);
            p.y = centre.y + hauteurs[i] + 0.06f;
            // Éclat : un point brillant qui court le long du contour (bord et crans), léger frémissement des braises.
            float proche = 0f;
            Color col;
            float taille = g.taille;
            switch (g.role)
            {
                case Role.Bord:
                case Role.Cran:
                {
                    float d = Mathf.Abs(Mathf.Repeat(g.phase - course + 0.5f, 1f) - 0.5f);
                    proche = Mathf.Clamp01(1f - d / 0.09f);
                    col = g.sombre ? Color.Lerp(baseC, vif, 0.2f + 0.6f * proche) : Color.Lerp(g.role == Role.Cran ? coeur : clair, coeur, proche);
                    taille *= 1f + 0.55f * proche;
                    break;
                }
                case Role.Centre:
                    col = coeur;
                    taille *= 1f + 0.12f * Mathf.Sin(t * 5f);
                    break;
                default:
                {
                    float f = 0.5f + 0.5f * Mathf.Sin(t * 3.1f + g.phase * 17f);
                    col = Color.Lerp(ombre, baseC, 0.35f + 0.65f * f);
                    taille *= 0.8f + 0.35f * f;
                    break;
                }
            }
            col = Color.Lerp(Color.Lerp(col, ombre, 0.55f), col, valide) * (intens * vivacite);
            col.a = 1f;
            Quaternion o = Quaternion.LookRotation(Direction(new Vector2(Mathf.Cos(g.angle), Mathf.Sin(g.angle)), spin), Vector3.up);
            LowPolyGem.Write(sommets, couleurs, i, p, taille * fondu * Mathf.Lerp(0.6f, 1f, valide), g.etirement, o, col, LowPolyGem.DefaultLight);
        }
        mesh.vertices = sommets;
        mesh.colors = couleurs;
        mesh.RecalculateBounds();
    }

    // Direction monde d'un vecteur du repère de la zone (tourné de `spin`).
    private Vector3 Direction(Vector2 l, float spin)
    {
        float c = Mathf.Cos(spin), s = Mathf.Sin(spin);
        Vector2 r = new Vector2(l.x * c - l.y * s, l.x * s + l.y * c);
        Vector3 d = axe * r.x + Vector3.Cross(Vector3.up, axe) * r.y;
        return d.sqrMagnitude > 0.0001f ? d.normalized : axe;
    }
}
