using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Deathless.Donjon.Terrasses
{
    /// Construit le donjon « terrasses étagées » d'une graine à partir de PlanTerrasses : blocs de pierre arrondis
    /// (murs d'enceinte, murs de soutènement, massifs, parois des pièces cachées), dalles, escaliers pleins, parapets,
    /// piliers et pilastres, voûte segmentaire à arcs doubleaux, arches de pierre, portes à serrure et parois secrètes,
    /// ponton suspendu de bois (variante 8 : tablier, garde-corps, chaînes pendues à la voûte, corbeaux),
    /// portail de retour, torches (lumières légères), coffres, repères (DonjonRepere), collisions (boîtes) et NavMesh.
    ///
    /// Tout est déterministe (aucun tirage hors du plan ; les variations de blocs viennent d'un hachage des cotes) : en
    /// réseau, l'hôte tire la graine et la transmet, chaque poste appelle Generer(graine) et obtient le même donjon.
    /// Rendu : un seul matériau éclairé à couleurs par sommet (Deathless/VertexColorLit) pour la pierre, le bois et le
    /// métal, combiné par carrés de 16 m ; flammes et portail en non éclairé (Relic/VertexColorUnlit).
    /// En édition (banc), les objets créés ne sont jamais enregistrés dans la scène (HideFlags.DontSave).
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ConstructeurTerrasses : MonoBehaviour
    {
        [Tooltip("Graine du donjon (en jeu : tirée par l'hôte et transmise à tous).")]
        public int graine = 1;
        public ParametresTerrasses parametres = new ParametresTerrasses();
        [Tooltip("Deathless/VertexColorLit : pierre, bois, métal.")]
        public Material materiauPierre;
        [Tooltip("Relic/VertexColorUnlit : flammes, portail, repères.")]
        public Material materiauFlamme;
        public bool genererAuDemarrage = true;
        public bool construireNavMesh = true;
        [Tooltip("Voûte, arcs doubleaux et corniches (à couper pour les vues de dessus).")]
        public bool voute = true;
        [Tooltip("Pastilles des points d'apparition (rouge sbire, rouge vif guerrier, bleu voleur, violet mage), des joueurs (bleu clair) et contour de la zone d'arrivée.")]
        public bool reperesVisibles = false;
        public Color couleurTorche = new Color(1f, 0.6f, 0.3f);
        public float intensiteTorche = 2.6f;
        public float porteeTorche = 9f;

        // ------------------------------------------------------------------ Résultat
        public PlanTerrasses Plan => m_Plan;
        public bool Conforme { get; private set; }
        public Transform Racine { get; private set; }
        public DonjonRepere Arrivee { get; private set; }
        public DonjonRepere PortailRetour { get; private set; }
        public readonly List<DonjonRepere> Butins = new List<DonjonRepere>();
        public readonly List<DonjonRepere> Apparitions = new List<DonjonRepere>();
        public readonly List<Transform> Joueurs = new List<Transform>();
        public readonly List<PorteDonjon> Portes = new List<PorteDonjon>();
        public readonly List<DeclencheurDonjon> Declencheurs = new List<DeclencheurDonjon>();
        public float TempsPlanMs { get; private set; }
        public float TempsConstructionMs { get; private set; }
        public float TempsNavMeshMs { get; private set; }
        public int NbSommets { get; private set; }
        public int NbLumieres { get; private set; }
        public int NbCollisions { get; private set; }
        public event Action<ConstructeurTerrasses> Genere;

        [NonSerialized] readonly PlanTerrasses m_Plan = new PlanTerrasses();
        [NonSerialized] readonly List<UnityEngine.Object> m_Crees = new List<UnityEngine.Object>();
        [NonSerialized] readonly Dictionary<int, MaillageTampon> m_Pierre = new Dictionary<int, MaillageTampon>();
        [NonSerialized] readonly Dictionary<int, MaillageTampon> m_Voute = new Dictionary<int, MaillageTampon>();
        [NonSerialized] readonly MaillageTampon m_Flammes = new MaillageTampon();
        [NonSerialized] readonly MaillageTampon m_Marques = new MaillageTampon();
        [NonSerialized] Transform m_Collisions, m_Objets, m_Lumieres, m_Reperes, m_GroupeVoute, m_GroupeMarques;
        [NonSerialized] NavMeshSurface m_Surface;
        [NonSerialized] Mesh m_MeshCoffre, m_MeshGrandCoffre, m_MeshCouvercle, m_MeshGrandCouvercle;
        // murs d'enceinte par côté (0 nord, 1 est, 2 sud, 3 ouest) : la vue en coupe cache le sud et l'ouest
        [NonSerialized] readonly MaillageTampon[] m_Cotes = { new MaillageTampon(), new MaillageTampon(), new MaillageTampon(), new MaillageTampon() };
        [NonSerialized] readonly MaillageTampon[] m_FlammesCotes = { new MaillageTampon(), new MaillageTampon(), new MaillageTampon(), new MaillageTampon() };
        [NonSerialized] readonly Transform[] m_GroupesCotes = new Transform[4];
        static readonly string[] NomsCotes = { "Enceinte_Nord", "Enceinte_Est", "Enceinte_Sud", "Enceinte_Ouest" };

        // ------------------------------------------------------------------ Palette (albédo plat)
        static readonly Color32 Mortier = new Color32(34, 38, 48, 255);
        static readonly Color32 CEnceinte = new Color32(84, 92, 112, 255);
        static readonly Color32 CSoutenement = new Color32(96, 104, 124, 255);
        static readonly Color32 CMassif = new Color32(78, 86, 104, 255);
        static readonly Color32 CPiece = new Color32(92, 85, 82, 255);
        static readonly Color32 CSol0 = new Color32(66, 73, 89, 255);
        static readonly Color32 CSol1 = new Color32(80, 88, 107, 255);
        static readonly Color32 CSol2 = new Color32(95, 104, 125, 255);
        static readonly Color32 CSolPiece = new Color32(84, 77, 74, 255);
        static readonly Color32 CEscalier = new Color32(106, 114, 134, 255);
        static readonly Color32 CPilier = new Color32(100, 109, 131, 255);
        static readonly Color32 CChapiteau = new Color32(112, 121, 143, 255);
        static readonly Color32 CVoute = new Color32(60, 66, 82, 255);
        static readonly Color32 CNervure = new Color32(86, 95, 115, 255);
        static readonly Color32 CParapet = new Color32(102, 110, 130, 255);
        static readonly Color32 CCape = new Color32(24, 27, 34, 255);
        static readonly Color32 CPlafond = new Color32(46, 44, 46, 255);
        static readonly Color32 CBois = new Color32(110, 76, 46, 255);
        static readonly Color32 CBoisSombre = new Color32(74, 51, 32, 255);
        static readonly Color32 CFer = new Color32(58, 61, 68, 255);
        static readonly Color32 CPlanche = new Color32(124, 88, 54, 255);
        static readonly Color32 CCorde = new Color32(150, 122, 82, 255);
        static readonly Color32 CBronze = new Color32(176, 118, 58, 255);
        static readonly Color32 CArgent = new Color32(196, 202, 212, 255);
        static readonly Color32 COr = new Color32(228, 178, 60, 255);
        static readonly Color32 CFlamme = new Color32(255, 122, 26, 255);
        static readonly Color32 CFlammeCoeur = new Color32(255, 214, 90, 255);
        static readonly Color32 CPortail = new Color32(48, 196, 96, 255);
        static readonly Color32 CPortailCoeur = new Color32(170, 250, 190, 255);
        static readonly Color32 CPlaque = new Color32(80, 86, 101, 255);

        const float Course = 0.75f, Saillie = 0.12f, Joint = 0.07f, ProfBloc = 0.42f, Carre = 16f;

        void Awake()
        {
            if (Application.isPlaying && genererAuDemarrage) Generer(graine);
        }

        void OnDestroy()
        {
            foreach (var o in m_Crees) if (o != null) Detruire(o);
            m_Crees.Clear();
        }

        // ================================================================== Génération
        /// Construit le donjon de la graine. Renvoie true si le plan passe tous les contrôles.
        public bool Generer(int g)
        {
            graine = g;
            var chrono = System.Diagnostics.Stopwatch.StartNew();
            Conforme = m_Plan.Generer(g, parametres);
            TempsPlanMs = (float)chrono.Elapsed.TotalMilliseconds;
            if (!Conforme) Debug.LogWarning("Donjon terrasses : " + m_Plan.Resume());
            chrono.Restart();
            Vider();
            NbCollisions = 0;
            CreerRacine();
            Sols(); Murs(); Chapeaux(); Plafonds(); Escaliers(); Parapets(); Pontons(); Piliers(); Voute(); Arches(); Portail(); Torches(); Coffres(); Mecanismes(); Reperes();
            Finaliser();
            Collisions();
            TempsConstructionMs = (float)chrono.Elapsed.TotalMilliseconds;
            chrono.Restart();
            if (construireNavMesh) ConstruireNavMesh();
            TempsNavMeshMs = (float)chrono.Elapsed.TotalMilliseconds;
            if (!Application.isPlaying) foreach (var t in Racine.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            Genere?.Invoke(this);
            return Conforme;
        }

        public void Vider()
        {
            if (m_Surface != null) m_Surface.RemoveData();
            if (Racine != null)
            {
                // hors de la hiérarchie et inactif tout de suite : le NavMesh reconstruit dans la foulée ne le voit plus
                Racine.gameObject.SetActive(false);
                if (Application.isPlaying) Racine.SetParent(null, false);
                Detruire(Racine.gameObject);
            }
            Racine = null;
            foreach (var o in m_Crees) if (o != null) Detruire(o);
            m_Crees.Clear();
            Butins.Clear(); Apparitions.Clear(); Joueurs.Clear(); Portes.Clear(); Declencheurs.Clear();
            m_Pierre.Clear(); m_Voute.Clear(); m_Flammes.Vider(); m_Marques.Vider();
            for (int k = 0; k < 4; k++) { m_Cotes[k].Vider(); m_FlammesCotes[k].Vider(); }
            Arrivee = null; PortailRetour = null; m_MeshCoffre = null; m_MeshGrandCoffre = null; m_MeshCouvercle = null; m_MeshGrandCouvercle = null;
            // restes d'une génération faite avant un rechargement de domaine
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name.StartsWith("Donjon_g")) Detruire(c.gameObject);
            }
        }

        static void Detruire(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        /// Montre ou cache la voûte (vues de dessus) sans reconstruire.
        public void AfficherVoute(bool oui) { voute = oui; if (m_GroupeVoute != null) m_GroupeVoute.gameObject.SetActive(oui); }
        public void AfficherReperes(bool oui) { reperesVisibles = oui; if (m_GroupeMarques != null) m_GroupeMarques.gameObject.SetActive(oui); }
        /// Vue en coupe : cache les murs d'enceinte sud et ouest (portail, torches et pilastres compris).
        public void AfficherCoupe(bool oui)
        {
            for (int k = 2; k < 4; k++) if (m_GroupesCotes[k] != null) m_GroupesCotes[k].gameObject.SetActive(!oui);
        }

        /// Position monde d'un point du plan.
        public Vector3 Monde(float x, float y, float z) { return transform.TransformPoint(new Vector3(x, y, z)); }

        void CreerRacine()
        {
            Racine = new GameObject("Donjon_g" + graine).transform;
            Racine.SetParent(transform, false);
            m_Collisions = Enfant("Collisions");
            m_Objets = Enfant("Objets");
            m_Lumieres = Enfant("Lumieres");
            m_Reperes = Enfant("Reperes");
            m_GroupeVoute = Enfant("Voute");
            m_GroupeMarques = Enfant("Marques");
            for (int k = 0; k < 4; k++) m_GroupesCotes[k] = Enfant(NomsCotes[k]);
        }

        Transform Enfant(string nom, Transform parent = null)
        {
            var t = new GameObject(nom).transform;
            t.SetParent(parent != null ? parent : Racine, false);
            return t;
        }

        MaillageTampon Tampon(Dictionary<int, MaillageTampon> d, Vector3 p)
        {
            int k = Mathf.FloorToInt((p.x + PlanTerrasses.Marge) / Carre) * 64 + Mathf.FloorToInt((p.z + PlanTerrasses.Marge) / Carre);
            MaillageTampon m;
            if (!d.TryGetValue(k, out m)) d[k] = m = new MaillageTampon();
            return m;
        }

        MaillageTampon Pierre(Vector3 p) { return Tampon(m_Pierre, p); }

        /// Côté de l'enceinte (0 nord, 1 est, 2 sud, 3 ouest) d'un point posé contre l'intérieur d'un mur d'enceinte, en
        /// regardant vers `n` (vers la salle) ; -1 s'il n'est pas contre l'enceinte.
        int Cote(float x, float z, Vector3 n)
        {
            var P = m_Plan;
            int i = P.CelluleI(x - n.x * 0.5f), j = P.CelluleJ(z - n.z * 0.5f);
            if (!P.DansGrille(i, j) || P.DansVolume(i, j)) return -1;
            var c = P.cellules[P.Index(i, j)];
            if (c.genre != GenreCellule.Hors && c.genre != GenreCellule.Passage) return -1;
            if (n.z > 0.5f) return 2;
            if (n.z < -0.5f) return 0;
            if (n.x > 0.5f) return 3;
            return 1;
        }

        static Vector3 N(Dir d) { return new Vector3(PlanTerrasses.Dx(d), 0f, PlanTerrasses.Dz(d)); }

        // ================================================================== Sols
        void Sols()
        {
            var P = m_Plan;
            var h = new float[8]; var col = new Color32[8]; var ci = new int[8]; var cj = new int[8];
            for (int bz = 0; bz < P.NZ; bz += 2)
                for (int bx = 0; bx < P.NX; bx += 2)
                {
                    int n = 0;
                    for (int dz = 0; dz < 2; dz++)
                        for (int dx = 0; dx < 2; dx++)
                        {
                            int i = bx + dx, j = bz + dz;
                            if (!P.DansGrille(i, j)) continue;
                            var c = P.cellules[P.Index(i, j)];
                            if (c.genre == GenreCellule.Sol || c.genre == GenreCellule.Cavite || c.genre == GenreCellule.Passage || c.genre == GenreCellule.Ponton)
                            {
                                h[n] = c.sol; ci[n] = i; cj[n] = j;
                                col[n] = c.genre == GenreCellule.Sol || c.genre == GenreCellule.Ponton ? CouleurSol(c.sol) : CSolPiece; n++;
                            }
                            // le dessus d'un ponton est un tablier de planches (Pontons), pas une dalle
                            if (c.AUnHaut && c.sommet < P.HautMur - 0.5f && c.genre != GenreCellule.Ponton)
                            {
                                h[n] = c.sommet; ci[n] = i; cj[n] = j; col[n] = CouleurSol(c.sommet); n++;
                            }
                        }
                    // regroupe par hauteur et teinte : dalle de 2 m si les quatre cellules y sont, sinon dalles de 1 m
                    var fait = new bool[n];
                    for (int a = 0; a < n; a++)
                    {
                        if (fait[a]) continue;
                        int cnt = 0;
                        for (int b = a; b < n; b++) if (!fait[b] && Mathf.Abs(h[b] - h[a]) < 0.01f && col[b].Equals(col[a])) cnt++;
                        if (cnt == 4)
                        {
                            for (int b = a; b < n; b++) if (Mathf.Abs(h[b] - h[a]) < 0.01f && col[b].Equals(col[a])) fait[b] = true;
                            float x = bx - PlanTerrasses.Marge + 1f, z = bz - PlanTerrasses.Marge + 1f;
                            Dalle(x, z, 2f, h[a], col[a]);
                        }
                        else
                        {
                            fait[a] = true;
                            Dalle(P.CentreX(ci[a]), P.CentreZ(cj[a]), 1f, h[a], col[a]);
                        }
                    }
                }
        }

        Color32 CouleurSol(float h) { return h < PlanTerrasses.HauteurNiveau * 0.5f ? CSol0 : h < PlanTerrasses.HauteurNiveau * 1.5f ? CSol1 : CSol2; }

        void Dalle(float x, float z, float t, float h, Color32 col)
        {
            var m = Pierre(new Vector3(x, h, z));
            uint hh = MaillageTampon.Hache(x, h, z);
            m.Bloc(new Vector3(x, h - 0.15f, z), new Vector3(t - Joint, 0.3f, t - Joint), Quaternion.identity, 0.06f, MaillageTampon.Teinte(col, hh, 5), MaillageTampon.SansDessous);
            float d = t * 0.5f;
            m.Quad(new Vector3(x - d, h - 0.06f, z - d), new Vector3(x - d, h - 0.06f, z + d), new Vector3(x + d, h - 0.06f, z + d), new Vector3(x + d, h - 0.06f, z - d), Vector3.up, Mortier);
        }

        // ================================================================== Murs (faces de la grille)
        void Murs()
        {
            var P = m_Plan;
            foreach (var f in P.faces)
            {
                Color32 col = f.genre == GenreFace.Enceinte || f.genre == GenreFace.Linteau ? CEnceinte : f.genre == GenreFace.Soutenement ? CSoutenement
                    : f.genre == GenreFace.Massif ? CMassif : CPiece;
                Vector3 n = N(f.dir);
                Vector3 mid = new Vector3((f.x0 + f.x1) * 0.5f, 0f, (f.z0 + f.z1) * 0.5f);
                var front = P.CelluleEn(mid.x + n.x * 0.5f, mid.z + n.z * 0.5f);
                bool clip = P.DansVolume(P.CelluleI(mid.x + n.x * 0.5f), P.CelluleJ(mid.z + n.z * 0.5f)) && front.genre != GenreCellule.Cavite && front.genre != GenreCellule.Passage;
                int cote = clip ? Cote(mid.x, mid.z, n) : -1;
                Func<Vector3, MaillageTampon> tampon = Pierre;
                if (cote >= 0) { var mc = m_Cotes[cote]; tampon = q => mc; }
                PoserMur(tampon, new Vector3(f.x0, 0f, f.z0), new Vector3(f.x1, 0f, f.z1), n, f.y0, f.y1, col, clip);
            }
        }

        /// Pan de mur en assises de blocs arrondis (0,75 m), décalées d'une assise à l'autre, devant un fond de mortier.
        /// cible null : tampons de pierre par carré ; sinon, ce tampon (paroi secrète).
        void PoserMur(Func<Vector3, MaillageTampon> tampon, Vector3 a, Vector3 b, Vector3 n, float y0, float y1, Color32 col, bool clipVoute)
        {
            var P = m_Plan;
            Vector3 tg = b - a;
            float lg = tg.magnitude;
            if (lg < 0.05f || y1 - y0 < 0.05f) return;
            tg /= lg;
            Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
            float yHautMortier = y1;
            if (clipVoute) yHautMortier = Mathf.Min(y1, Mathf.Max(P.Voute(a.x), P.Voute(b.x)) + 0.5f);
            if (yHautMortier > y0)
            {
                var mm = tampon((a + b) * 0.5f);
                mm.Quad(a + Vector3.up * y0, b + Vector3.up * y0, b + Vector3.up * yHautMortier, a + Vector3.up * yHautMortier, n, Mortier);
            }
            int idFace = (int)MaillageTampon.Hache(a.x * 3f + n.x, a.z * 3f + n.z, b.x + b.z);
            int k0 = Mathf.FloorToInt(y0 / Course + 0.001f), k1 = Mathf.CeilToInt(y1 / Course - 0.001f);
            for (int k = k0; k < k1; k++)
            {
                float cy0 = Mathf.Max(y0, k * Course), cy1 = Mathf.Min(y1, (k + 1) * Course);
                if (cy1 - cy0 < 0.12f) continue;
                // décalage d'assise lié à la position absolue (les pans voisins se raccordent)
                float origine = Vector3.Dot(a, tg);
                float decal = ((k & 1) == 0 ? 0f : 0.6f) + 0.25f * (MaillageTampon.Hache(k, idFace, 3) % 3u);
                float s = -Mod(origine + decal, 1.9f);
                int nb = 0;
                while (s < lg)
                {
                    uint hb = MaillageTampon.Hache(k, Mathf.RoundToInt((origine + s) * 10f), idFace);
                    float l = 1.0f + 0.3f * (hb % 4u);
                    float s0 = Mathf.Max(s, 0f), s1 = Mathf.Min(s + l, lg);
                    s += l; nb++;
                    if (s1 - s0 < 0.22f) continue;
                    Vector3 c = a + tg * ((s0 + s1) * 0.5f);
                    float by0 = cy0, by1 = cy1;
                    if (clipVoute)
                    {
                        float lim = P.Voute(c.x) + 0.45f;
                        if (by0 > lim) continue;
                        by1 = Mathf.Min(by1, lim);
                        if (by1 - by0 < 0.12f) continue;
                    }
                    Vector3 centre = c + Vector3.up * ((by0 + by1) * 0.5f) + n * (Saillie - ProfBloc * 0.5f);
                    var mm = tampon(centre);
                    mm.Bloc(centre, new Vector3(s1 - s0 - Joint, by1 - by0 - Joint, ProfBloc), rot, 0.09f, MaillageTampon.Teinte(col, hb, 7), MaillageTampon.SansArriere);
                }
            }
        }

        static float Mod(float a, float m) { float r = a % m; return r < 0f ? r + m : r; }

        /// Dessus des murs et des massifs (vus seulement sans la voûte) : un chapeau sombre qui encadre le plan.
        void Chapeaux()
        {
            var P = m_Plan;
            for (int j = 0; j < P.NZ; j++)
                for (int i = 0; i < P.NX; i++)
                {
                    var c = P.cellules[P.Index(i, j)];
                    // les pièces cachées derrière l'enceinte restent découvertes : la vue de dessus montre leur intérieur
                    if (c.genre != GenreCellule.Hors && c.genre != GenreCellule.Massif) continue;
                    bool bord = false;
                    for (int dz = -2; dz <= 2 && !bord; dz++)
                        for (int dx = -2; dx <= 2 && !bord; dx++)
                        {
                            if (!P.DansGrille(i + dx, j + dz)) continue;
                            var b = P.cellules[P.Index(i + dx, j + dz)];
                            if (b.genre != GenreCellule.Hors && b.genre != GenreCellule.Massif) bord = true;
                        }
                    if (!bord) continue;
                    float y = c.genre == GenreCellule.Massif || c.genre == GenreCellule.Hors ? c.sol : c.sommet;
                    float x0 = i - PlanTerrasses.Marge, z0 = j - PlanTerrasses.Marge;
                    int cote = z0 < 0f ? 2 : z0 >= P.P ? 0 : x0 < 0f ? 3 : x0 >= P.L ? 1 : -1;
                    var mc = cote >= 0 ? m_Cotes[cote] : Pierre(new Vector3(x0, y, z0));
                    mc.Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z0 + 1f), new Vector3(x0 + 1f, y, z0 + 1f), new Vector3(x0 + 1f, y, z0), Vector3.up, CCape);
                }
        }

        /// Dessous des pleins hauts : plafond des pièces cachées (avec poutres) et intrados des passages.
        void Plafonds()
        {
            var P = m_Plan;
            for (int j = 0; j < P.NZ; j++)
                for (int i = 0; i < P.NX; i++)
                {
                    var c = P.cellules[P.Index(i, j)];
                    if (!c.AUnHaut || c.genre == GenreCellule.Ponton) continue;
                    float x0 = i - PlanTerrasses.Marge, z0 = j - PlanTerrasses.Marge, y = c.plafond;
                    var col = MaillageTampon.Teinte(CPlafond, MaillageTampon.Hache(i, j, 11), 4);
                    Pierre(new Vector3(x0, y, z0)).Quad(new Vector3(x0, y, z0), new Vector3(x0 + 1f, y, z0), new Vector3(x0 + 1f, y, z0 + 1f), new Vector3(x0, y, z0 + 1f), Vector3.down, col);
                }
            foreach (var p in P.pieces)
            {
                // poutres de bois dans le sens court, tous les 2 m
                bool selonX = p.r.Largeur <= p.r.Profondeur;
                float lg = selonX ? p.r.Largeur : p.r.Profondeur, autre = selonX ? p.r.Profondeur : p.r.Largeur;
                for (float s = 1.5f; s < autre - 0.5f; s += 2f)
                {
                    Vector3 c = selonX ? new Vector3(p.r.CentreX, p.plafond - 0.2f, p.r.z0 + s) : new Vector3(p.r.x0 + s, p.plafond - 0.2f, p.r.CentreZ);
                    Vector3 t = selonX ? new Vector3(lg + 0.4f, 0.4f, 0.35f) : new Vector3(0.35f, 0.4f, lg + 0.4f);
                    Pierre(c).Bloc(c, t, Quaternion.identity, 0.07f, MaillageTampon.Teinte(CBoisSombre, MaillageTampon.Hache(c.x, c.y, c.z), 5));
                }
            }
        }

        // ================================================================== Escaliers
        void Escaliers()
        {
            foreach (var e in m_Plan.escaliers)
            {
                Vector3 ax = N(e.dir), lat = Vector3.Cross(Vector3.up, ax);
                float w = e.Largeur;
                // pied de l'escalier, au milieu de sa largeur
                Vector3 o;
                switch (e.dir)
                {
                    case Dir.Nord: o = new Vector3(e.r.CentreX, 0f, e.r.z0); break;
                    case Dir.Sud: o = new Vector3(e.r.CentreX, 0f, e.r.z1); break;
                    case Dir.Est: o = new Vector3(e.r.x0, 0f, e.r.CentreZ); break;
                    default: o = new Vector3(e.r.x1, 0f, e.r.CentreZ); break;
                }
                Quaternion rot = Quaternion.LookRotation(ax, Vector3.up);
                int nm = Mathf.RoundToInt(PlanTerrasses.HauteurNiveau / PlanTerrasses.Marche);
                for (int v = 0; v < e.Volees; v++)
                {
                    float s0 = v * (PlanTerrasses.Volee + PlanTerrasses.Palier), yb = e.Base + v * PlanTerrasses.HauteurNiveau;
                    for (int k = 1; k <= nm; k++)
                    {
                        float top = yb + k * PlanTerrasses.Marche, sm = s0 + (k - 0.5f) * PlanTerrasses.Giron;
                        Vector3 c = o + ax * sm + Vector3.up * ((top + e.Base) * 0.5f);
                        Pierre(c).Bloc(c, new Vector3(w - 0.06f, top - e.Base, PlanTerrasses.Giron - 0.03f), rot, 0.07f,
                            MaillageTampon.Teinte(CEscalier, MaillageTampon.Hache(c.x, top, c.z), 6), MaillageTampon.SansDessous);
                    }
                    Rampe(o + ax * s0, ax, lat, w, yb);
                    float haut = yb + PlanTerrasses.HauteurNiveau;
                    if (v < e.Volees - 1)
                    {
                        float sp = s0 + PlanTerrasses.Volee + PlanTerrasses.Palier * 0.5f;
                        Vector3 c = o + ax * sp + Vector3.up * ((haut + e.Base) * 0.5f);
                        Pierre(c).Bloc(c, new Vector3(w - 0.06f, haut - e.Base, PlanTerrasses.Palier - 0.03f), rot, 0.07f, MaillageTampon.Teinte(CEscalier, MaillageTampon.Hache(c.x, haut, c.z), 6), MaillageTampon.SansDessous);
                        // palier : recouvre le haut de la rampe et le pied de la suivante (pas de fente où le NavMesh se coupe)
                        Boite(o + ax * sp + Vector3.up * (haut - 0.15f), new Vector3(w, 0.3f, PlanTerrasses.Palier + 0.8f), rot);
                    }
                    else
                    {
                        // seuil : du haut de la rampe (s = 4,75) jusque sur la terrasse
                        Boite(o + ax * (s0 + PlanTerrasses.Volee + 0.05f) + Vector3.up * (haut - 0.15f), new Vector3(w, 0.3f, 0.9f), rot);
                    }
                }
            }
        }

        /// Collision d'une volée : une rampe droite qui passe à mi-hauteur des marches (marche après marche, ±0,15 m).
        void Rampe(Vector3 pied, Vector3 ax, Vector3 lat, float w, float yb)
        {
            float run = PlanTerrasses.Volee, rise = PlanTerrasses.HauteurNiveau;
            Vector3 a = pied + ax * -0.25f + Vector3.up * yb;
            Vector3 b = pied + ax * (run - 0.25f) + Vector3.up * (yb + rise);
            Vector3 d = (b - a);
            float lg = d.magnitude;
            Vector3 nrm = Vector3.Cross(d, lat).normalized;
            if (nrm.y < 0f) nrm = -nrm;
            Quaternion rot = Quaternion.LookRotation(d / lg, nrm);
            Boite((a + b) * 0.5f - nrm * 0.2f - d / lg * 0.15f, new Vector3(w, 0.4f, lg + 0.3f), rot);
        }

        // ================================================================== Parapets
        void Parapets()
        {
            foreach (var p in m_Plan.parapets)
            {
                if (p.bois) { GardeCorps(p); continue; }
                Vector3 n = N(p.dir);
                Vector3 a = new Vector3(p.x0, 0f, p.z0) - n * 0.27f, b = new Vector3(p.x1, 0f, p.z1) - n * 0.27f;
                Vector3 tg = b - a; float lg = tg.magnitude; tg /= lg;
                // prolonge d'un demi-pas aux deux bouts : les angles se ferment
                a -= tg * 0.25f; lg += 0.5f;
                float y0 = p.y - 0.4f, y1 = p.y + PlanTerrasses.HauteurParapet;
                Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
                int nb = Mathf.Max(1, Mathf.RoundToInt(lg / 1.3f));
                for (int k = 0; k < nb; k++)
                {
                    float s0 = lg * k / nb, s1 = lg * (k + 1) / nb;
                    Vector3 c = a + tg * ((s0 + s1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f);
                    Pierre(c).Bloc(c, new Vector3(s1 - s0 - 0.06f, y1 - y0, 0.52f), rot, 0.12f, MaillageTampon.Teinte(CParapet, MaillageTampon.Hache(c.x, c.y, c.z), 6), MaillageTampon.SansDessous);
                }
                var bp = Boite(a + tg * (lg * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f + 0.15f), new Vector3(lg, y1 - y0 + 0.3f, 0.52f), rot);
                bp.name = "Parapet";
                bp.AddComponent<VueLibre>();   // la caméra le traverse (découpé autour du héros)
            }
        }

        // ================================================================== Ponton suspendu
        const float HautGardeCorps = 1.1f;

        /// Garde-corps de bois d'un ponton : collision seule (le visuel est posé par Pontons), mince, traversée par la caméra.
        void GardeCorps(Parapet p)
        {
            Vector3 n = N(p.dir);
            Vector3 a = new Vector3(p.x0, 0f, p.z0) - n * 0.12f, b = new Vector3(p.x1, 0f, p.z1) - n * 0.12f;
            Vector3 tg = b - a; float lg = tg.magnitude;
            var bp = Boite((a + b) * 0.5f + Vector3.up * (p.y + HautGardeCorps * 0.5f), new Vector3(lg, HautGardeCorps, 0.2f), Quaternion.LookRotation(n, Vector3.up));
            bp.name = "GardeCorps";
            bp.AddComponent<VueLibre>();
        }

        /// Ponton : tablier de planches sur deux longerons et des traverses, poteaux et deux lisses (main courante et
        /// corde) sur toute la longueur des deux côtés, chaînes de fer des poteaux jusqu'à la voûte, corbeaux de pierre
        /// sous les bouts. Collision du tablier : boîte de la grille (Collisions), marquée VueLibre.
        void Pontons()
        {
            var P = m_Plan;
            foreach (var po in P.pontons)
            {
                bool selonX = po.axe == Dir.Est || po.axe == Dir.Ouest;
                Vector3 u = selonX ? Vector3.right : Vector3.forward, w = selonX ? Vector3.forward : Vector3.right;
                float lg = po.Longueur, larg = selonX ? po.r.Profondeur : po.r.Largeur, H = po.Hauteur, bas = po.Dessous;
                Vector3 o = selonX ? new Vector3(po.r.x0, 0f, po.r.CentreZ) : new Vector3(po.r.CentreX, 0f, po.r.z0);   // milieu du bout de départ
                Quaternion rot = Quaternion.LookRotation(u, Vector3.up);
                Vector3 mid = o + u * (lg * 0.5f);
                // longerons (prolongés de 0,3 m dans les terrasses) et planches en travers
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 c = mid + w * (s * (larg * 0.5f - 0.45f)) + Vector3.up * (bas + 0.17f);
                    Pierre(c).Bloc(c, new Vector3(0.32f, 0.34f, lg + 0.6f), rot, 0.06f, MaillageTampon.Teinte(CBoisSombre, MaillageTampon.Hache(c.x, c.z, 31), 5));
                }
                int np = Mathf.Max(1, Mathf.RoundToInt(lg / 0.42f));
                float pas = lg / np;
                for (int k = 0; k < np; k++)
                {
                    uint hh = MaillageTampon.Hache(k, po.r.x0 * 7 + po.r.z0, 41);
                    float dl = ((hh >> 4) % 5u) * 0.03f - 0.06f;             // planches de longueurs un peu inégales
                    float dy = ((hh >> 8) % 3u) * 0.012f;
                    Vector3 c = o + u * (pas * (k + 0.5f)) + w * (dl * 0.5f) + Vector3.up * (H - 0.06f + dy * 0.5f - 0.006f);
                    Pierre(c).Bloc(c, new Vector3(larg - 0.04f + dl, 0.12f + dy, pas - 0.05f), rot, 0.025f,
                        MaillageTampon.Teinte((hh & 3u) == 0u ? CBois : CPlanche, hh, 9), MaillageTampon.SansDessous);
                }
                // poteaux, traverses dessous, lisses, chaînes
                int nPot = Mathf.Max(2, Mathf.RoundToInt(lg / 2.4f) + 1);
                for (int k = 0; k < nPot; k++)
                {
                    float sk = Mathf.Lerp(0.25f, lg - 0.25f, k / (float)(nPot - 1));
                    Vector3 t = o + u * sk + Vector3.up * (bas - 0.12f);
                    Pierre(t).Bloc(t, new Vector3(0.24f, 0.24f, larg + 0.3f), Quaternion.LookRotation(w, Vector3.up), 0.04f, MaillageTampon.Teinte(CBoisSombre, MaillageTampon.Hache(k, t.x, t.z), 5));
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 pb = o + u * sk + w * (s * (larg * 0.5f - 0.1f));
                        Vector3 c = pb + Vector3.up * (H + HautGardeCorps * 0.5f - 0.1f);
                        Pierre(c).Bloc(c, new Vector3(0.17f, HautGardeCorps + 0.2f, 0.17f), rot, 0.04f, MaillageTampon.Teinte(CBois, MaillageTampon.Hache(k, s, 43), 6));
                        // chaîne : maillons alternés jusqu'à la voûte, anneau scellé
                        float y0 = H + HautGardeCorps + 0.1f, y1 = P.Voute(pb.x) - 0.1f;
                        int nm = Mathf.Max(1, Mathf.FloorToInt((y1 - y0) / 0.28f));
                        for (int m = 0; m < nm; m++)
                        {
                            Vector3 cm = pb + Vector3.up * (y0 + (m + 0.5f) * (y1 - y0) / nm);
                            Quaternion rm = Quaternion.LookRotation((m & 1) == 0 ? u : w, Vector3.up);
                            Pierre(cm).Bloc(cm, new Vector3(0.06f, 0.3f, 0.16f), rm, 0.02f, CFer);
                        }
                        Vector3 an = pb + Vector3.up * (y1 + 0.02f);
                        Pierre(an).Bloc(an, new Vector3(0.36f, 0.12f, 0.36f), Quaternion.identity, 0.03f, CFer);
                    }
                }
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 cote = mid + w * (s * (larg * 0.5f - 0.1f));
                    Vector3 c = cote + Vector3.up * (H + HautGardeCorps - 0.02f);
                    Pierre(c).Bloc(c, new Vector3(0.14f, 0.12f, lg + 0.1f), rot, 0.04f, MaillageTampon.Teinte(CBois, MaillageTampon.Hache(c.x, c.z, 47), 5));
                    Vector3 cc = cote + Vector3.up * (H + 0.55f);
                    Pierre(cc).Bloc(cc, new Vector3(0.07f, 0.07f, lg - 0.1f), rot, 0.03f, CCorde);
                    Vector3 cp = cote + Vector3.up * (H + 0.08f);
                    Pierre(cp).Bloc(cp, new Vector3(0.1f, 0.16f, lg), rot, 0.03f, MaillageTampon.Teinte(CBoisSombre, MaillageTampon.Hache(c.x, c.z, 53), 5));
                }
                // corbeaux de pierre sous les longerons, contre les murs des terrasses
                for (int bout = 0; bout < 2; bout++)
                {
                    Vector3 e = o + u * (bout == 0 ? 0.3f : lg - 0.3f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 c = e + w * (s * (larg * 0.5f - 0.45f)) + Vector3.up * (bas - 0.3f);
                        Pierre(c).Bloc(c, new Vector3(0.5f, 0.6f, 0.6f), rot, 0.1f, MaillageTampon.Teinte(CChapiteau, MaillageTampon.Hache(c.x, c.z, 59), 5));
                    }
                }
            }
        }

        // ================================================================== Piliers
        void Piliers()
        {
            var P = m_Plan;
            foreach (var p in P.piliers)
            {
                Vector3 b = new Vector3(p.x, p.bas, p.z);
                uint hh = MaillageTampon.Hache(p.x, p.bas, p.z);
                float haut = p.adosse && Mathf.Abs(p.haut - P.Naissance) < 0.01f ? P.Naissance : P.Voute(p.x) + 0.15f;
                bool corbeau = p.adosse && p.bas >= P.Naissance - 1.3f;
                Quaternion rot = p.adosse ? Quaternion.LookRotation(N(p.dir), Vector3.up) : Quaternion.identity;
                float r = p.rayon;
                int cote = p.adosse ? Cote(p.x - N(p.dir).x * 0.3f, p.z - N(p.dir).z * 0.3f, N(p.dir)) : -1;
                Func<Vector3, MaillageTampon> Pierre = q => cote >= 0 ? m_Cotes[cote] : this.Pierre(q);
                if (!corbeau)
                {
                    float socle = 0.5f;
                    Pierre(b).Bloc(b + Vector3.up * (socle * 0.5f), new Vector3(r * 2.4f, socle, r * 2.4f), rot, 0.12f, MaillageTampon.Teinte(CChapiteau, hh, 5), MaillageTampon.SansDessous);
                    float y = p.bas + socle, yTop = haut - 0.7f;
                    int nt = Mathf.Max(1, Mathf.RoundToInt((yTop - y) / 1.1f));
                    float dh = (yTop - y) / nt;
                    for (int k = 0; k < nt; k++)
                    {
                        var col = MaillageTampon.Teinte(CPilier, MaillageTampon.Hache(p.x, k, p.z), 6);
                        Pierre(b).Tour(new Vector3(p.x, y + k * dh, p.z), r, dh - 0.03f, 0.08f, 16, col, false, false);
                    }
                    // collision
                    var go = new GameObject("Pilier");
                    go.transform.SetParent(m_Collisions, false);
                    go.transform.localPosition = new Vector3(p.x, (p.bas + haut) * 0.5f, p.z);
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.radius = r; cap.height = haut - p.bas; cap.direction = 1;
                    go.AddComponent<VueLibre>();   // la caméra le traverse (découpé autour du héros)
                    NbCollisions++;
                }
                else
                {
                    Vector3 c = new Vector3(p.x, haut - 1.0f, p.z);
                    Pierre(c).Bloc(c, new Vector3(1.1f, 0.7f, 1.0f), rot, 0.12f, MaillageTampon.Teinte(CChapiteau, hh, 5));
                }
                Vector3 cc = new Vector3(p.x, haut - 0.35f, p.z);
                Pierre(cc).Bloc(cc, new Vector3(r * 2.6f, 0.7f, r * 2.6f), rot, 0.14f, MaillageTampon.Teinte(CChapiteau, hh >> 3, 5));
            }
        }

        // ================================================================== Voûte
        void Voute()
        {
            var P = m_Plan;
            float L = P.L, PP = P.P, f = P.Cle - P.Naissance, demi = L * 0.5f;
            float R = (demi * demi + f * f) / (2f * f), cy = P.Cle - R;
            int nx = Mathf.CeilToInt(L / 1.0f), nz = Mathf.CeilToInt((PP + 1f) / 2f);
            for (int a = 0; a < nx; a++)
                for (int b = 0; b < nz; b++)
                {
                    float x0 = L * a / nx, x1 = L * (a + 1) / nx, z0 = -0.5f + b * 2f, z1 = Mathf.Min(PP + 0.5f, z0 + 2f);
                    float y0 = P.Voute(x0), y1 = P.Voute(x1);
                    Vector3 n0 = new Vector3(demi - x0, cy - y0, 0f).normalized, n1 = new Vector3(demi - x1, cy - y1, 0f).normalized;
                    // rangs décalés d'une bande à l'autre
                    var col = MaillageTampon.Teinte(CVoute, MaillageTampon.Hache(a, (b * 2 + (a & 1)) / 2, 17), 5);
                    var m = Tampon(m_Voute, new Vector3((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f));
                    m.Quad(new Vector3(x0, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y0, z1), n0, n1, n1, n0, col);
                }
            // arcs doubleaux
            float tmax = Mathf.Asin(Mathf.Min(1f, demi / R));
            int nv = Mathf.Max(8, Mathf.CeilToInt(2f * R * tmax / 1.3f));
            foreach (float z in P.nervures)
                for (int k = 0; k < nv; k++)
                {
                    float th = -tmax + 2f * tmax * (k + 0.5f) / nv;
                    Vector3 u = new Vector3(Mathf.Sin(th), Mathf.Cos(th), 0f);
                    Vector3 p = new Vector3(demi, cy, z) + u * R;
                    Vector3 c = p - u * 0.28f;
                    float lg = 2f * R * tmax / nv;
                    var m = Tampon(m_Voute, c);
                    m.Bloc(c, new Vector3(lg - 0.05f, 0.62f, 0.9f), Quaternion.LookRotation(Vector3.forward, u), 0.1f, MaillageTampon.Teinte(CNervure, MaillageTampon.Hache(k, z, 5), 6));
                }
            // corniches à la naissance, le long des murs latéraux
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? 0.2f : L - 0.2f;
                for (float z = 0f; z < PP; z += 1.6f)
                {
                    float z1 = Mathf.Min(PP, z + 1.6f);
                    Vector3 c = new Vector3(x, P.Naissance - 0.25f, (z + z1) * 0.5f);
                    Tampon(m_Voute, c).Bloc(c, new Vector3(0.8f, 0.5f, z1 - z - 0.06f), Quaternion.identity, 0.1f, MaillageTampon.Teinte(CNervure, MaillageTampon.Hache(x, z, 9), 5));
                }
            }
        }

        // ================================================================== Arches, portes, parois secrètes
        void Arches()
        {
            foreach (var p in m_Plan.pieces)
            {
                Vector3 n = N(p.arche.dir), lat = Vector3.Cross(Vector3.up, n);
                Vector3 o = new Vector3(p.arche.x, p.sol, p.arche.z);
                Color32 colMur = p.emplacement == Emplacement.SousTerrasse ? CSoutenement : p.emplacement == Emplacement.DansMassif ? CMassif : CEnceinte;
                if (p.genre == GenrePiece.Secrete) { ParoiSecrete(p, o, n, lat, colMur); continue; }
                Encadrement(o, n, lat, PlanTerrasses.ArcheLargeur * 0.5f, PlanTerrasses.ArcheNaissance, 1.0f, CChapiteau, Pierre(o));
                if (p.genre == GenrePiece.Verrouillee) Porte(p, o, n, lat);
            }
        }

        /// Encadrement d'une arche en plein cintre : piédroits et claveaux, en saillie de 0,25 m sur le mur, sur toute
        /// l'épaisseur du passage (ils ferment aussi les écoinçons de l'ouverture rectangulaire).
        void Encadrement(Vector3 o, Vector3 n, Vector3 lat, float ri, float naissance, float prof, Color32 col, MaillageTampon m)
        {
            Quaternion rotN = Quaternion.LookRotation(n, Vector3.up);
            float ep = 0.6f, dz = 0.25f - (prof + 0.25f) * 0.5f;
            for (int s = -1; s <= 1; s += 2)
                for (float y = 0f; y < naissance - 0.01f; y += Course)
                {
                    float y1 = Mathf.Min(naissance, y + Course);
                    Vector3 c = o + lat * (s * (ri + ep * 0.5f)) + Vector3.up * ((y + y1) * 0.5f) + n * dz;
                    m.Bloc(c, new Vector3(ep - 0.05f, y1 - y - 0.05f, prof + 0.25f), rotN, 0.1f, MaillageTampon.Teinte(col, MaillageTampon.Hache(c.x, c.y, c.z), 6));
                }
            int nv = 9;
            float rc = ri + ep * 0.5f;
            for (int k = 0; k < nv; k++)
            {
                float th = Mathf.PI * (k + 0.5f) / nv;
                Vector3 u = lat * Mathf.Cos(th) + Vector3.up * Mathf.Sin(th);
                Vector3 c = o + Vector3.up * naissance + u * rc + n * dz;
                float lg = Mathf.PI * rc / nv;
                float ext = k == nv / 2 ? 0.15f : 0f;   // clé
                m.Bloc(c + u * ext * 0.5f, new Vector3(lg - 0.05f, ep + ext, prof + 0.25f + ext * 0.5f), Quaternion.LookRotation(n, u), 0.1f, MaillageTampon.Teinte(col, MaillageTampon.Hache(k, c.y, c.x + c.z), 6));
            }
        }

        void Porte(PieceCachee p, Vector3 o, Vector3 n, Vector3 lat)
        {
            float demi = PlanTerrasses.ArcheLargeur * 0.5f;
            var go = new GameObject("Porte_" + p.serrure + "_piece" + p.index);
            go.transform.SetParent(m_Objets, false);
            // charnière à gauche (vue de la salle), en retrait de 0,45 m dans le passage
            go.transform.localPosition = o - lat * demi - n * 0.45f;
            go.transform.localRotation = Quaternion.LookRotation(n, Vector3.up);
            var m = new MaillageTampon();
            Color32 metal = p.serrure == Serrure.Bronze ? CBronze : p.serrure == Serrure.Argent ? CArgent : p.serrure == Serrure.Or ? COr : CFer;
            int np = 5;
            float lp = PlanTerrasses.ArcheLargeur / np;
            for (int k = 0; k < np; k++)
            {
                float u = -demi + (k + 0.5f) * lp, ue = Mathf.Abs(u) + lp * 0.5f;
                float top = PlanTerrasses.ArcheNaissance + Mathf.Sqrt(Mathf.Max(0f, demi * demi - ue * ue)) + (ue > demi ? 0f : 0f);
                top = Mathf.Max(top, PlanTerrasses.ArcheNaissance) - 0.04f;
                // repère local : x de la charnière vers l'autre montant (droite de LookRotation(n) = lat)
                var c = new Vector3(u + demi, top * 0.5f, 0f);
                m.Bloc(c, new Vector3(lp - 0.04f, top, 0.16f), Quaternion.identity, 0.05f, MaillageTampon.Teinte(CBois, MaillageTampon.Hache(k, p.index, 21), 8));
            }
            for (int b = 0; b < 2; b++)
                m.Bloc(new Vector3(demi, 0.8f + b * 1.7f, 0.1f), new Vector3(PlanTerrasses.ArcheLargeur - 0.1f, 0.18f, 0.06f), Quaternion.identity, 0.03f, metal);
            m.Bloc(new Vector3(PlanTerrasses.ArcheLargeur - 0.45f, 1.2f, 0.14f), new Vector3(0.36f, 0.5f, 0.08f), Quaternion.identity, 0.05f, metal);
            m.Bloc(new Vector3(PlanTerrasses.ArcheLargeur - 0.45f, 1.12f, 0.19f), new Vector3(0.08f, 0.16f, 0.04f), Quaternion.identity, 0.02f, CMortierFonce);
            Visuel(go, m.VersMesh("Porte"), materiauPierre);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(demi, PlanTerrasses.ArcheHauteur * 0.5f, 0f); bc.size = new Vector3(PlanTerrasses.ArcheLargeur, PlanTerrasses.ArcheHauteur, 0.3f);
            Obstacle(go, bc.center, bc.size);
            var porte = go.AddComponent<PorteDonjon>();
            porte.genre = GenrePiece.Verrouillee; porte.serrure = p.serrure; porte.piece = p.index; porte.course = 100f; porte.duree = 1.2f;
            Portes.Add(porte);
        }

        static readonly Color32 CMortierFonce = new Color32(14, 14, 16, 255);

        void ParoiSecrete(PieceCachee p, Vector3 o, Vector3 n, Vector3 lat, Color32 colMur)
        {
            float demi = PlanTerrasses.ArcheLargeur * 0.5f;
            var go = new GameObject("ParoiSecrete_piece" + p.index);
            go.transform.SetParent(m_Objets, false);
            var m = new MaillageTampon();
            // mêmes assises que le mur autour (cotes absolues), puis un bouchon sombre dans l'épaisseur du passage
            PoserMur(q => m, o - lat * demi - Vector3.up * p.sol, o + lat * demi - Vector3.up * p.sol, n, p.sol, p.sol + PlanTerrasses.ArcheHauteur, colMur, false);
            m.Bloc(o + Vector3.up * (PlanTerrasses.ArcheHauteur * 0.5f) - n * 0.5f, new Vector3(Mathf.Abs(lat.x) * PlanTerrasses.ArcheLargeur + Mathf.Abs(n.x) * 0.9f, PlanTerrasses.ArcheHauteur, Mathf.Abs(lat.z) * PlanTerrasses.ArcheLargeur + Mathf.Abs(n.z) * 0.9f), Quaternion.identity, 0.02f, Mortier, MaillageTampon.SansDessous);
            Visuel(go, m.VersMesh("ParoiSecrete"), materiauPierre);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = o + Vector3.up * (PlanTerrasses.ArcheHauteur * 0.5f) - n * 0.45f;
            bc.size = new Vector3(Mathf.Abs(lat.x) * PlanTerrasses.ArcheLargeur + Mathf.Abs(n.x) * 1.0f, PlanTerrasses.ArcheHauteur, Mathf.Abs(lat.z) * PlanTerrasses.ArcheLargeur + Mathf.Abs(n.z) * 1.0f);
            Obstacle(go, bc.center, bc.size);
            var porte = go.AddComponent<PorteDonjon>();
            porte.genre = GenrePiece.Secrete; porte.piece = p.index; porte.course = PlanTerrasses.ArcheHauteur + 0.2f; porte.duree = 2.2f;
            Portes.Add(porte);
        }

        /// Le NavMesh se construit porte ouverte (la porte est ignorée) ; un obstacle qui découpe le NavMesh la ferme.
        void Obstacle(GameObject go, Vector3 centre, Vector3 taille)
        {
            var mod = go.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            var ob = go.AddComponent<NavMeshObstacle>();
            ob.shape = NavMeshObstacleShape.Box; ob.center = centre; ob.size = taille; ob.carving = true; ob.carveOnlyStationary = false;
            // en édition (banc, contrôles) : NavMesh « portes ouvertes » ; la découpe ne vaut qu'en jeu
            ob.enabled = Application.isPlaying;
        }

        // ================================================================== Portail et arrivée
        void Portail()
        {
            var P = m_Plan;
            Vector3 o = new Vector3(P.portail.x, 0f, P.portail.z);
            Vector3 n = Vector3.forward, lat = Vector3.right;
            float ri = 1.8f, nais = 3.2f;
            Encadrement(o, n, lat, ri, nais, 0.6f, CChapiteau, m_Cotes[2]);
            // nappe verte (non éclairée) dans l'arche
            var contour = new List<Vector3>();
            float zp = 0.16f;
            contour.Add(new Vector3(o.x - ri, 0.05f, zp)); contour.Add(new Vector3(o.x + ri, 0.05f, zp));
            for (int k = 0; k <= 12; k++)
            {
                float th = Mathf.PI * k / 12f;
                contour.Add(new Vector3(o.x + Mathf.Cos(th) * ri, nais + Mathf.Sin(th) * ri, zp));
            }
            m_FlammesCotes[2].Polygone(new Vector3(o.x, 2.4f, zp), contour, Vector3.forward, CPortail, CPortailCoeur);
            var l = Lumiere(new Vector3(o.x, 2.4f, 1.0f), new Color(0.3f, 0.9f, 0.5f), 1.2f, 5f, m_GroupesCotes[2]);
            l.name = "LumierePortail";
            // dalle d'arrivée en bois sombre (6 × 2 m), légèrement en relief
            for (int k = 0; k < 6; k++)
            {
                Vector3 c = new Vector3(P.arrivee.x - 2.5f + k, 0.05f, P.arrivee.z);
                Pierre(c).Bloc(c, new Vector3(0.94f, 0.14f, 2f), Quaternion.identity, 0.05f, MaillageTampon.Teinte(CBoisSombre, MaillageTampon.Hache(k, 3, 7), 6), MaillageTampon.SansDessous);
            }
        }

        // ================================================================== Torches
        void Torches()
        {
            NbLumieres = 0;
            foreach (var t in m_Plan.torches)
            {
                Quaternion rot = Quaternion.Euler(0f, t.pose.rotY, 0f);
                Vector3 n = rot * Vector3.forward;
                Vector3 p = new Vector3(t.pose.x, t.pose.y, t.pose.z);
                int cote = Cote(p.x, p.z, n);
                var m = cote >= 0 ? m_Cotes[cote] : Pierre(p);
                var fl = cote >= 0 ? m_FlammesCotes[cote] : m_Flammes;
                m.Bloc(p + n * (Saillie + 0.08f) + Vector3.down * 0.25f, new Vector3(0.22f, 0.36f, 0.16f), rot, 0.05f, CFer);
                Vector3 manche = p + n * (Saillie + 0.22f) + Vector3.down * 0.05f;
                m.Bloc(manche, new Vector3(0.13f, 0.55f, 0.13f), rot * Quaternion.Euler(25f, 0f, 0f), 0.05f, CBois);
                Vector3 f = manche + Vector3.up * 0.32f + n * 0.12f;
                fl.Bloc(f, new Vector3(0.26f, 0.3f, 0.26f), Quaternion.Euler(0f, 45f, 0f), 0.08f, CFlamme);
                fl.Bloc(f + Vector3.up * 0.2f, new Vector3(0.16f, 0.22f, 0.16f), Quaternion.Euler(0f, 20f, 0f), 0.05f, CFlammeCoeur);
                if (!t.allumee) continue;
                var l = Lumiere(f + n * 0.3f + Vector3.up * 0.1f, couleurTorche, intensiteTorche, porteeTorche, cote >= 0 ? m_GroupesCotes[cote] : null);
                var tt = l.gameObject.AddComponent<TorcheDonjon>();
                tt.lumiere = l; tt.intensite = intensiteTorche;
            }
        }

        Light Lumiere(Vector3 p, Color col, float intensite, float portee, Transform parent = null)
        {
            var go = new GameObject("Torche");
            go.transform.SetParent(parent != null ? parent : m_Lumieres, false);
            go.transform.localPosition = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = col; l.intensity = intensite; l.range = portee; l.shadows = LightShadows.None;
            NbLumieres++;
            return l;
        }

        // ================================================================== Coffres
        void Coffres()
        {
            m_MeshCoffre = MeshCoffre(false);
            m_MeshGrandCoffre = MeshCoffre(true);
            m_MeshCouvercle = MeshCouvercle(false);
            m_MeshGrandCouvercle = MeshCouvercle(true);
            int i = 0;
            foreach (var c in m_Plan.coffres)
            {
                bool grand = c.type == TypeCoffre.GrandCoffre;
                var go = new GameObject((grand ? "GrandCoffre_" : "Coffre_") + i);
                go.transform.SetParent(m_Objets, false);
                go.transform.localPosition = new Vector3(c.pose.x, c.pose.y, c.pose.z);
                go.transform.localRotation = Quaternion.Euler(0f, c.pose.rotY, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = grand ? m_MeshGrandCoffre : m_MeshCoffre;
                go.AddComponent<MeshRenderer>().sharedMaterial = materiauPierre;
                float s = grand ? 1.45f : 1f;
                // couvercle à part, charnière à l'arrière (côté mur) : DonjonJeu le fait basculer à l'ouverture (enfant « _lid »)
                var lid = new GameObject("Couvercle_lid");
                lid.transform.SetParent(go.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.6f, -0.45f) * s;
                lid.AddComponent<MeshFilter>().sharedMesh = grand ? m_MeshGrandCouvercle : m_MeshCouvercle;
                lid.AddComponent<MeshRenderer>().sharedMaterial = materiauPierre;
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 0.45f * s, 0f); bc.size = new Vector3(1.36f, 0.9f, 0.9f) * s;
                var r = go.AddComponent<DonjonRepere>();
                r.genre = DonjonRepere.Genre.Butin; r.index = i; r.niveau = c.pose.niveau; r.butin = grand ? TypeButin.GrandCoffre : TypeButin.Coffre; r.visuel = go;
                Butins.Add(r);
                i++;
            }
        }

        Mesh MeshCoffre(bool grand)
        {
            var m = new MaillageTampon();
            float s = grand ? 1.45f : 1f;
            Color32 bande = grand ? COr : CFer;
            // caisse seule ; le couvercle est un maillage à part (MeshCouvercle), posé sur sa charnière
            m.Bloc(new Vector3(0f, 0.31f, 0f) * s, new Vector3(1.3f, 0.62f, 0.84f) * s, Quaternion.identity, 0.07f * s, CBois);
            for (int k = -1; k <= 1; k += 2)
                m.Bloc(new Vector3(0.42f * k, 0.3f, 0f) * s, new Vector3(0.13f, 0.62f, 0.94f) * s, Quaternion.identity, 0.05f * s, bande);
            var mesh = m.VersMesh(grand ? "GrandCoffre" : "Coffre");
            m_Crees.Add(mesh);
            if (!Application.isPlaying) mesh.hideFlags = HideFlags.DontSave;
            return mesh;
        }

        /// Couvercle d'un coffre dans le repère de sa charnière (arrière de la caisse, à 0,6 m) : il bascule autour de x.
        Mesh MeshCouvercle(bool grand)
        {
            var m = new MaillageTampon();
            float s = grand ? 1.45f : 1f;
            Color32 bande = grand ? COr : CFer;
            m.Bloc(new Vector3(0f, 0.14f, 0.45f) * s, new Vector3(1.36f, 0.28f, 0.9f) * s, Quaternion.identity, 0.12f * s, MaillageTampon.Teinte(CBois, 7u, 10));
            for (int k = -1; k <= 1; k += 2)
                m.Bloc(new Vector3(0.42f * k, 0.16f, 0.45f) * s, new Vector3(0.13f, 0.32f, 0.94f) * s, Quaternion.identity, 0.05f * s, bande);
            // moraillon de la serrure, au bord avant du couvercle
            m.Bloc(new Vector3(0f, -0.02f, 0.89f) * s, new Vector3(0.24f, 0.3f, 0.07f) * s, Quaternion.identity, 0.04f * s, grand ? COr : CBronze);
            var mesh = m.VersMesh(grand ? "GrandCouvercle" : "Couvercle");
            m_Crees.Add(mesh);
            if (!Application.isPlaying) mesh.hideFlags = HideFlags.DontSave;
            return mesh;
        }

        // ================================================================== Déclencheurs
        void Mecanismes()
        {
            foreach (var d in m_Plan.declencheurs)
            {
                var porte = Portes.Find(x => x.piece == d.cible);
                var go = new GameObject((d.genre == GenreDeclencheur.PlaqueSol ? "Plaque_piece" : "Bouton_piece") + d.cible);
                go.transform.SetParent(m_Objets, false);
                go.transform.localPosition = new Vector3(d.pose.x, d.pose.y, d.pose.z);
                go.transform.localRotation = Quaternion.Euler(0f, d.pose.rotY, 0f);
                var vis = new GameObject("Visuel");
                vis.transform.SetParent(go.transform, false);
                var m = new MaillageTampon();
                var bc = go.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                if (d.genre == GenreDeclencheur.PlaqueSol)
                {
                    // dalle de 1,2 m un peu plus sombre, à peine en relief : il faut la remarquer
                    m.Bloc(new Vector3(0f, 0.025f, 0f), new Vector3(1.2f, 0.12f, 1.2f), Quaternion.identity, 0.05f, CPlaque, MaillageTampon.SansDessous);
                    bc.center = new Vector3(0f, 0.4f, 0f); bc.size = new Vector3(1.1f, 0.8f, 1.1f);
                }
                else
                {
                    // pierre en saillie de la taille d'une main, dans une assise
                    m.Bloc(new Vector3(0f, 0f, Saillie + 0.02f), new Vector3(0.46f, 0.38f, 0.08f), Quaternion.identity, 0.03f, Mortier);
                    m.Bloc(new Vector3(0f, 0f, Saillie + 0.1f), new Vector3(0.28f, 0.22f, 0.16f), Quaternion.identity, 0.07f, new Color32(128, 132, 146, 255));
                    bc.center = new Vector3(0f, -0.1f, 0.9f); bc.size = new Vector3(1.4f, 2f, 1.4f);
                }
                Visuel(vis, m.VersMesh("Declencheur"), materiauPierre);
                go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                var dec = go.AddComponent<DeclencheurDonjon>();
                dec.genre = d.genre; dec.cible = porte; dec.visuel = vis.transform;
                Declencheurs.Add(dec);
            }
        }

        // ================================================================== Repères
        void Reperes()
        {
            var P = m_Plan;
            Arrivee = Repere("Arrivee", DonjonRepere.Genre.Arrivee, 0, P.arrivee);
            PortailRetour = Repere("PortailRetour", DonjonRepere.Genre.PortailRetour, 0, P.portail);
            for (int k = 0; k < P.joueurs.Length; k++)
            {
                var t = Enfant("Joueur_" + k, m_Reperes);
                t.localPosition = new Vector3(P.joueurs[k].x, P.joueurs[k].y, P.joueurs[k].z);
                Joueurs.Add(t);
                m_Marques.Disque(t.localPosition + Vector3.up * 0.08f, 0.38f, 12, Vector3.up, new Color32(120, 190, 255, 255));
            }
            for (int k = 0; k < P.apparitions.Count; k++)
            {
                var a = P.apparitions[k];
                var r = Repere("Apparition_" + k + "_" + a.type, DonjonRepere.Genre.Apparition, k, a.pose);
                r.apparition = (TypeApparition)(int)a.type;
                Apparitions.Add(r);
                Color32 col = a.type == TypeMonstre.Guerrier ? new Color32(255, 50, 40, 255) : a.type == TypeMonstre.Voleur ? new Color32(70, 140, 255, 255)
                    : a.type == TypeMonstre.Mage ? new Color32(175, 100, 255, 255) : new Color32(215, 90, 70, 255);
                m_Marques.Disque(new Vector3(a.pose.x, a.pose.y + 0.08f, a.pose.z), 0.6f, 14, Vector3.up, col, new Color32(255, 240, 210, 255));
            }
            // contour de la zone d'arrivée (6 × 5 m)
            var z = P.zoneArrivee;
            Color32 v = new Color32(90, 230, 130, 255);
            float y = 0.09f, e = 0.08f;
            m_Marques.Quad(new Vector3(z.x0, y, z.z0 - e), new Vector3(z.x0, y, z.z0 + e), new Vector3(z.x1, y, z.z0 + e), new Vector3(z.x1, y, z.z0 - e), Vector3.up, v);
            m_Marques.Quad(new Vector3(z.x0, y, z.z1 - e), new Vector3(z.x0, y, z.z1 + e), new Vector3(z.x1, y, z.z1 + e), new Vector3(z.x1, y, z.z1 - e), Vector3.up, v);
            m_Marques.Quad(new Vector3(z.x0 - e, y, z.z0), new Vector3(z.x0 - e, y, z.z1), new Vector3(z.x0 + e, y, z.z1), new Vector3(z.x0 + e, y, z.z0), Vector3.up, v);
            m_Marques.Quad(new Vector3(z.x1 - e, y, z.z0), new Vector3(z.x1 - e, y, z.z1), new Vector3(z.x1 + e, y, z.z1), new Vector3(z.x1 + e, y, z.z0), Vector3.up, v);
        }

        DonjonRepere Repere(string nom, DonjonRepere.Genre g, int index, Pose p)
        {
            var t = Enfant(nom, m_Reperes);
            t.localPosition = new Vector3(p.x, p.y, p.z);
            t.localRotation = Quaternion.Euler(0f, p.rotY, 0f);
            var r = t.gameObject.AddComponent<DonjonRepere>();
            r.genre = g; r.index = index; r.niveau = p.niveau;
            return r;
        }

        // ================================================================== Maillages
        void Finaliser()
        {
            int sommets = 0;
            var pierre = Enfant("Pierre");
            foreach (var kv in m_Pierre) sommets += Poser(pierre, "Pierre_" + kv.Key, kv.Value, materiauPierre);
            foreach (var kv in m_Voute) sommets += Poser(m_GroupeVoute, "Voute_" + kv.Key, kv.Value, materiauPierre);
            sommets += Poser(Racine, "Flammes", m_Flammes, materiauFlamme != null ? materiauFlamme : materiauPierre);
            for (int k = 0; k < 4; k++)
            {
                sommets += Poser(m_GroupesCotes[k], "Pierre", m_Cotes[k], materiauPierre);
                sommets += Poser(m_GroupesCotes[k], "Flammes", m_FlammesCotes[k], materiauFlamme != null ? materiauFlamme : materiauPierre);
            }
            Poser(m_GroupeMarques, "Marques", m_Marques, materiauFlamme != null ? materiauFlamme : materiauPierre);
            NbSommets = sommets;
            m_GroupeVoute.gameObject.SetActive(voute);
            m_GroupeMarques.gameObject.SetActive(reperesVisibles);
        }

        int Poser(Transform parent, string nom, MaillageTampon m, Material mat)
        {
            if (m.NbSommets == 0) return 0;
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            Visuel(go, m.VersMesh(nom), mat);
            return m.NbSommets;
        }

        void Visuel(GameObject go, Mesh mesh, Material mat)
        {
            m_Crees.Add(mesh);
            if (!Application.isPlaying) mesh.hideFlags = HideFlags.DontSave;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;
        }

        // ================================================================== Collisions
        GameObject Boite(Vector3 centre, Vector3 taille, Quaternion rot)
        {
            var go = new GameObject("Boite");
            go.transform.SetParent(m_Collisions, false);
            go.transform.localPosition = centre;
            go.transform.localRotation = rot;
            go.AddComponent<BoxCollider>().size = taille;
            NbCollisions++;
            return go;
        }

        /// Boîtes des colonnes de la grille, fusionnées en rectangles de colonnes identiques (plein bas, plein haut).
        void Collisions()
        {
            var P = m_Plan;
            int nx = P.NX, nz = P.NZ;
            // colonnes utiles : tout ce qui n'est pas plein, et 2 m de plein autour
            var utile = new bool[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    var c = P.cellules[P.Index(i, j)];
                    if (c.genre == GenreCellule.Hors || c.genre == GenreCellule.Massif) continue;
                    for (int dz = -2; dz <= 2; dz++)
                        for (int dx = -2; dx <= 2; dx++)
                            if (P.DansGrille(i + dx, j + dz)) utile[P.Index(i + dx, j + dz)] = true;
                }
            var vu = new bool[nx * nz];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    int k = P.Index(i, j);
                    if (vu[k] || !utile[k]) continue;
                    long cle = Cle(P.cellules[k]);
                    int w = 1;
                    while (i + w < nx && !vu[k + w] && utile[k + w] && Cle(P.cellules[k + w]) == cle) w++;
                    int h = 1;
                    while (j + h < nz)
                    {
                        bool ok = true;
                        for (int a = 0; a < w && ok; a++) { int kk = P.Index(i + a, j + h); ok = !vu[kk] && utile[kk] && Cle(P.cellules[kk]) == cle; }
                        if (!ok) break;
                        h++;
                    }
                    for (int b = 0; b < h; b++) for (int a = 0; a < w; a++) vu[P.Index(i + a, j + b)] = true;
                    var c = P.cellules[k];
                    float x0 = i - PlanTerrasses.Marge, z0 = j - PlanTerrasses.Marge;
                    float sol = c.sol;
                    if (sol > PlanTerrasses.FondSol + 0.01f)
                        Boite(new Vector3(x0 + w * 0.5f, (PlanTerrasses.FondSol + sol) * 0.5f, z0 + h * 0.5f), new Vector3(w, sol - PlanTerrasses.FondSol, h), Quaternion.identity);
                    if (c.AUnHaut)
                    {
                        var bh = Boite(new Vector3(x0 + w * 0.5f, (c.plafond + c.sommet) * 0.5f, z0 + h * 0.5f), new Vector3(w, c.sommet - c.plafond, h), Quaternion.identity);
                        // tablier du ponton : la caméra le traverse (vue sous le ponton), il est découpé autour du héros
                        if (c.genre == GenreCellule.Ponton) { bh.name = "Ponton"; bh.AddComponent<VueLibre>(); }
                    }
                }
        }

        static long Cle(Cellule c)
        {
            long a = Mathf.RoundToInt(c.sol * 100f) + 100000;
            long b = c.AUnHaut ? Mathf.RoundToInt(c.plafond * 100f) + 1 : 0;
            long d = c.AUnHaut ? Mathf.RoundToInt(c.sommet * 100f) + 1 : 0;
            return (a << 40) | (b << 20) | d;
        }

        void ConstruireNavMesh()
        {
            if (m_Surface == null) m_Surface = GetComponent<NavMeshSurface>();
            if (m_Surface == null) m_Surface = gameObject.AddComponent<NavMeshSurface>();
            m_Surface.collectObjects = CollectObjects.Children;
            m_Surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            m_Surface.layerMask = ~(1 << 2);
            m_Surface.agentTypeID = 0;
            Physics.SyncTransforms();
            m_Surface.BuildNavMesh();
        }

        /// Longueur du plus court chemin NavMesh entre deux points du plan (-1 : pas de chemin complet).
        public float Chemin(Vector3 a, Vector3 b)
        {
            NavMeshHit ha, hb;
            if (!NavMesh.SamplePosition(Monde(a.x, a.y, a.z), out ha, 1.5f, NavMesh.AllAreas)) return -1f;
            if (!NavMesh.SamplePosition(Monde(b.x, b.y, b.z), out hb, 1.5f, NavMesh.AllAreas)) return -1f;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return -1f;
            float l = 0f;
            for (int i = 1; i < path.corners.Length; i++) l += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return l;
        }

        /// Contrôle NavMesh : chaque coffre (portes ouvertes : le NavMesh est construit ainsi), chaque apparition et le
        /// portail sont atteignables depuis l'arrivée. Renvoie la liste des défauts (vide si tout va bien).
        public List<string> VerifierNavMesh()
        {
            var d = new List<string>();
            var P = m_Plan;
            Vector3 a = new Vector3(P.arrivee.x, P.arrivee.y, P.arrivee.z);
            // les obstacles des portes coupent le NavMesh : on les coupe le temps du contrôle
            var obs = Racine != null ? Racine.GetComponentsInChildren<NavMeshObstacle>() : new NavMeshObstacle[0];
            foreach (var o in obs) o.enabled = false;
            foreach (var c in P.coffres) if (Chemin(a, new Vector3(c.pose.x, c.pose.y, c.pose.z) + Quaternion.Euler(0f, c.pose.rotY, 0f) * Vector3.forward * 1.1f) < 0f) d.Add("coffre " + c.pose + " hors d'atteinte");
            foreach (var s in P.apparitions) if (Chemin(a, new Vector3(s.pose.x, s.pose.y, s.pose.z)) < 0f) d.Add("apparition " + s.pose + " hors d'atteinte");
            if (Chemin(a, new Vector3(P.portail.x, 0f, 1.2f)) < 0f) d.Add("portail hors d'atteinte");
            // ponton : le tablier (au milieu) et le rez dessous, depuis l'arrivée
            foreach (var po in P.pontons)
            {
                if (Chemin(a, new Vector3(po.r.CentreX, po.Hauteur, po.r.CentreZ)) < 0f) d.Add("ponton hors d'atteinte");
                if (Chemin(a, new Vector3(po.r.CentreX, 0f, po.r.CentreZ)) < 0f) d.Add("dessous du ponton hors d'atteinte");
            }
            // chaque terrasse (son centre ou la case libre la plus proche)
            foreach (var t in P.terrasses)
            {
                bool ok = false;
                for (int z = t.r.z0; z < t.r.z1 && !ok; z++)
                    for (int x = t.r.x0; x < t.r.x1 && !ok; x++)
                    {
                        var c = P.CelluleEn(x + 0.5f, z + 0.5f);
                        if (c.genre != GenreCellule.Sol || c.obstacle || Mathf.Abs(c.sol - t.Hauteur) > 0.01f) continue;
                        if (Mathf.Abs(x + 0.5f - t.r.CentreX) > 1.5f && Mathf.Abs(z + 0.5f - t.r.CentreZ) > 1.5f) continue;
                        ok = Chemin(a, new Vector3(x + 0.5f, t.Hauteur, z + 0.5f)) >= 0f;
                    }
                if (!ok) d.Add("terrasse " + t.index + " (Lvl " + t.niveau + ") hors d'atteinte");
            }
            foreach (var o in obs) o.enabled = Application.isPlaying;
            return d;
        }
    }
}
