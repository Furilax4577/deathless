using System.Collections;
using System.Collections.Generic;
using Deathless.Accessoires;
using Deathless.Donjon;
using Deathless.Donjon.Terrasses;
using Deathless.Reseau;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Jeu
{
    /// Le donjon en partie (wiki : deroule.md, Le donjon ; portail.md). Posé dans la scène du village, loin de lui
    /// (Origine), avec le générateur (Deathless > Donjon > Placer dans le village).
    ///
    /// - **Un donjon neuf chaque jour** : au début du jour, l'autorité (hôte, ou ce poste en solo) tire une graine,
    ///   le construit, pose les gardiens ; la graine et l'essai retenu partent aux clients (PartieReseau.GraineDonjon, EssaiDonjon), qui construisent le
    ///   même donjon.
    /// - **Portails** : le jour, le portail du village (près de Nyxessa) mène à l'arrivée ; le portail de retour (le même
    ///   portail de gemmes vertes, toujours ouvert) ramène devant le portail du village. On passe avec la touche
    ///   Interagir, à 3 m au plus du centre (PassagePortail ; Quentin, 26/09/2026 : plus en marchant dedans). Passage
    ///   avec l'effet de téléportation (PortalTransit : corps en gemmes vers le portail de départ, onde d'entrée, puis
    ///   gemmes qui jaillissent du portail d'arrivée, onde de sortie), vu par tous.
    /// - **Butin, de l'or seulement** : coffres (touche Interagir ; le couvercle bascule, sans cadenas ni clé depuis le
    ///   26/09/2026) et tas d'or (on passe dessus). L'or est porté par le joueur (HUD) et versé à la caisse au retour
    ///   par le portail. L'autorité décide (un butin n'est pris qu'une fois).
    /// - **Gardiens** : quelques sbires et guerriers sur les points d'apparition proches du butin.
    /// - **Alerte et rappel** : alerte 15 s avant le crépuscule pour les joueurs au donjon ; au crépuscule, Nyxessa les
    ///   rappelle : ils perdent l'or porté, sauf la part gardée selon son palier (0 / 20 / 40 / 60 / 75 %). Même règle
    ///   pour un joueur mort au donjon ({à confirmer}).
    /// - **Masquage des étages** local à chaque joueur (capteurs sur le héros local et sa caméra) ; **ambiance** sombre
    ///   aux torches pour le joueur local au donjon ; **eau** qui ralentit héros et squelettes (ZoneEau).
    [DefaultExecutionOrder(500)]
    public class DonjonJeu : MonoBehaviour, IEtatDonjon
    {
        /// Coin du donjon (60 x 48 m) : loin du village (le village tient dans ± 240 m, caméra à 400 m).
        public static readonly Vector3 Origine = new Vector3(1000f, 0f, 0f);
        static readonly Bounds EmpriseAncien = new Bounds(Origine + new Vector3(30f, 5f, 24f), new Vector3(66f, 34f, 54f));
        /// Emprise du donjon construit (« au donjon ») : celle de l'ancien donjon, ou celle du donjon en terrasses.
        static Bounds s_Emprise = EmpriseAncien;
        /// Intérieur des murs d'enceinte (faces intérieures à 0,5 m des bords, du fond du bassin au sommet des murs du
        /// dernier étage) : la caméra du joueur local n'en sort pas (CameraEpaule.Enceinte, B1 de l'audit).
        static readonly Bounds EnceinteCamera = EnceinteInterieure();

        // ------------------------------------------------------------------ Donjon en terrasses (aperçu de la nouvelle carte)
        /// Coin du donjon en terrasses (02/10/2026) : à côté de l'ancien (qui n'est pas construit dans l'aperçu), toujours
        /// loin de la carte. Le plan va de -10 à L + 10 m (marge des pièces derrière l'enceinte).
        public static readonly Vector3 OrigineTerrasses = new Vector3(1000f, 0f, 120f);
        /// Vrai : le donjon est le donjon en terrasses (Deathless.Donjon.Terrasses) au lieu de l'ancien (DonjonGenerateur).
        /// Seul le mode « Nouvelle carte (aperçu) » (Partie.Exploration) le prend ; le jeu normal garde l'ancien.
        public static bool TerrassesVoulues => Partie.Exploration;
        bool Terrasses => TerrassesVoulues;
        ConstructeurTerrasses m_Terrasses;
        /// Constructeur du donjon en terrasses (null hors aperçu, ou avant la première construction).
        public ConstructeurTerrasses Constructeur => m_Terrasses;
        /// Boîte intérieure de la grande salle (sous la naissance de la voûte), en monde : enceinte de la caméra.
        Bounds m_SalleTerrasses;
        /// Intérieur des pièces cachées (monde), passage de l'arche compris : enceinte de la caméra quand le héros y est.
        readonly List<Bounds> m_PiecesTerrasses = new List<Bounds>();
        Transform m_BourdonTerrasses;
        Vector3? m_PiedEscalier;
        Vector3 m_SortieEscalier;
        bool m_PiedCherche;

        /// Repères du donjon construit (ancien ou terrasses) : arrivée, portail de retour, butins, apparitions.
        public DonjonRepere RepereArrivee => Terrasses ? (m_Terrasses != null ? m_Terrasses.Arrivee : null) : generateur != null ? generateur.Arrivee : null;
        public DonjonRepere RepereRetour => Terrasses ? (m_Terrasses != null ? m_Terrasses.PortailRetour : null) : generateur != null ? generateur.PortailRetour : null;
        public IList<DonjonRepere> Butins => Terrasses ? (m_Terrasses != null ? (IList<DonjonRepere>)m_Terrasses.Butins : Aucun) : generateur != null ? generateur.Butins : Aucun;
        public IList<DonjonRepere> Apparitions => Terrasses ? (m_Terrasses != null ? (IList<DonjonRepere>)m_Terrasses.Apparitions : Aucun) : generateur != null ? generateur.Apparitions : Aucun;
        static readonly DonjonRepere[] Aucun = new DonjonRepere[0];
        /// Butins suivis par un masque de 32 bits (Pris, PartieReseau.ButinsPris).
        const int MaxButins = 32;

        static Bounds EnceinteInterieure()
        {
            float yMin = -DonjonPlan.ProfondeurBassin - 0.5f, yMax = DonjonPlan.NbNiveaux * DonjonPlan.HauteurNiveau - 0.3f;
            float lx = DonjonPlan.Largeur * DonjonPlan.Cellule, lz = DonjonPlan.Profondeur * DonjonPlan.Cellule;
            return new Bounds(Origine + new Vector3(lx * 0.5f, (yMin + yMax) * 0.5f, lz * 0.5f), new Vector3(lx - 1.1f, yMax - yMin, lz - 1.1f));
        }

        public static DonjonJeu Instance { get; private set; }

        public DonjonGenerateur generateur;
        [Tooltip("Cadenas du grand coffre (Assets/Art/Cadenas/Prefabs). Inutilisé tant que CadenasActifs est faux.")] public GameObject cadenasGrandCoffre;
        [Tooltip("Cadenas des coffres. Inutilisé tant que CadenasActifs est faux.")] public GameObject cadenasCoffre;
        [Tooltip("Sac d'un joueur mort au donjon (KayKit, Assets/Art/KayKit/.../decoration/props/sack.fbx).")] public GameObject modeleSac;

        /// Cadenas et clé sur les coffres : désactivés (Quentin, 26/09/2026 : plus aucun cadenas, tous les coffres du
        /// donjon s'ouvrent sans clé pour l'instant ; le couvercle bascule directement). Le code est gardé pour plus tard
        /// (coffres à clé, {à confirmer}) : remettre à vrai pour retrouver le cadenas posé sur la face avant, qui
        /// s'ouvre avant le couvercle.
        static readonly bool CadenasActifs = false;

        [Header("Ambiance au donjon (joueur local)")]
        public Color ambiance = new Color(0.30f, 0.26f, 0.25f);
        public Color brouillard = new Color(0.07f, 0.055f, 0.06f);
        public float brouillardDebut = 12f, brouillardFin = 70f;

        /// Graine du donjon construit (0 : aucun).
        public int GraineCourante { get; private set; }
        /// Essai du plan retenu par l'autorité pour cette graine (PartieReseau.EssaiDonjon) : les clients le
        /// construisent tel quel, sans refaire la vérification NavMesh qui pourrait trancher autrement chez eux.
        public int EssaiCourant { get; private set; }
        /// Butins déjà pris (un bit par emplacement).
        public int Pris { get; private set; }
        public bool Pret => GraineCourante != 0 && RepereArrivee != null;

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;

        /// Durées du passage (Wiki : portail.md, {décidé} 26/09/2026). Départ : l'effet en gemmes (PortalTransit.Depart)
        /// se joue en entier, immobile, avant la téléportation. Arrivée : les gemmes se reforment (PortalTransit.Arrive),
        /// puis le clip d'arrivée prend le relais dès sa première image (PortailAnim.DureeClip, Heros.DeclencherPortail),
        /// en entier, avant de rendre le contrôle.
        const float DureeTransitDepart = 1.1f, DureeTransitArriveeGemmes = 0.5f;

        readonly List<Squelette> m_Gardiens = new List<Squelette>();
        readonly float[] m_Demande = new float[MaxButins];
        int m_PrisVus;
        bool m_Transit;
        Heros m_HerosTransit;
        Camera m_CamAmbiance;
        CameraClearFlags m_FondCamera;
        Color m_FondCouleur;
        bool m_AmbianceActive;
        Light m_Soleil;
        bool m_AlerteJouee;
        string m_Message = "";
        float m_MessageJusqua;
        Heros m_HerosCapteur;
        VueCycle m_VueCycle;
        PassagePortail m_PassageVillage, m_PassageRetour;
        PortalVisual m_BourdonSur;
        float m_ProchaineEvalPrets;   // prochaine réévaluation périodique du vote « prêt » (Update)
        CameraEpaule m_CameraJeuDe;     // caméra de jeu dont m_CameraJeu est la Camera (LateUpdate)
        Camera m_CameraJeu;

        sealed class Coffre
        {
            public Transform couvercle;
            public Quaternion ferme;
            public CadenasOuverture cadenas;
            /// Corps du coffre et son maillage complet (avec les pièces d'or) : à l'ouverture, le corps prend le maillage
            /// sans pièces (SansPieces) ; PreparerButins le lui rend.
            public MeshFilter corps;
            public Mesh maillagePlein;
        }

        /// Maillages des corps de coffre sans leurs pièces d'or (audit du 27/09/2026, point 1 : le coffre ouvert restait plein
        /// de pièces « à ramasser »). Les pièces sont les triangles dont l'UV tombe dans la zone dorée de l'atlas KayKit
        /// (u ≥ 0,84, v ≤ 0,5) ; un maillage sans triangle doré est rendu tel quel.
        static readonly Dictionary<Mesh, Mesh> s_SansPieces = new Dictionary<Mesh, Mesh>();

        static Mesh SansPieces(Mesh plein)
        {
            if (plein == null) return null;
            if (s_SansPieces.TryGetValue(plein, out var vide)) return vide;
            if (!plein.isReadable) { s_SansPieces[plein] = plein; return plein; }   // build : maillage non lisible, laissé tel quel
            var uv = plein.uv; var tri = plein.triangles;
            var garde = new List<int>(tri.Length);
            int retires = 0;
            for (int t = 0; t + 2 < tri.Length; t += 3)
            {
                bool or = uv.Length > 0;
                for (int k = 0; k < 3 && or; k++) { Vector2 u = uv[tri[t + k]]; or = u.x >= 0.84f && u.y <= 0.5f; }
                if (or) { retires++; continue; }
                garde.Add(tri[t]); garde.Add(tri[t + 1]); garde.Add(tri[t + 2]);
            }
            if (retires == 0) vide = plein;
            else
            {
                vide = Instantiate(plein);
                vide.name = plein.name + "_Vide";
                vide.triangles = garde.ToArray();
                vide.RecalculateBounds();
            }
            s_SansPieces[plein] = vide;
            return vide;
        }
        readonly Dictionary<GameObject, Coffre> m_Coffres = new Dictionary<GameObject, Coffre>();

        /// Sac d'un joueur mort au donjon (26/09/2026) : voir les méthodes « Sacs des joueurs morts » plus bas.
        struct Sac { public int id; public Vector3 position; public int montant; public string nom; public int objets; }   // objets : clés et crochets emballés (Inventaire.Tasser)
        readonly List<Sac> m_Sacs = new List<Sac>();
        readonly Dictionary<int, GameObject> m_VisuelsSacs = new Dictionary<int, GameObject>();
        readonly Dictionary<int, float> m_DemandeSac = new Dictionary<int, float>();
        int m_ProchainSacId;

        void Awake()
        {
            Instance = this;
            DonneesUI.Donjon = this;
            if (generateur == null) generateur = GetComponent<DonjonGenerateur>();
            if (generateur != null) generateur.genererAuDemarrage = false;
            s_Emprise = EmpriseAncien;
        }

        void OnDestroy()
        {
            CameraEpaule.Enceinte = null;
            CameraEpaule.DecoupeSansTraverser = false;
            s_Emprise = EmpriseAncien;
            if (Instance == this) Instance = null;
            if (ReferenceEquals(DonneesUI.Donjon, this)) DonneesUI.Donjon = null;
        }

        void Start()
        {
            if (P != null) P.PhaseChangee += OnPhase;
            var dc = FindAnyObjectByType<DayCycle>();
            if (dc != null) m_Soleil = dc.GetComponent<Light>();
        }

        // ================================================================== Géographie

        public static bool Contient(Vector3 p) => s_Emprise.Contains(p);
        public static bool AuDonjon(Heros h) => h != null && Contient(h.transform.position);

        /// Un joueur au moins est au donjon (vote « prêt » bloqué).
        public static bool QuelquunAuDonjon
        {
            get
            {
                var p = Partie.Instance;
                if (p == null) return false;
                var tous = p.TousLesHeros;
                for (int i = 0; i < tous.Count; i++) if (AuDonjon(tous[i])) return true;
                return false;
            }
        }

        PortalVisual PortailVillage
        {
            get
            {
                if (m_VueCycle == null) m_VueCycle = VueCycle.Instance;   // enregistrée par VueCycle.Awake (pas de recherche par image)
                return m_VueCycle != null ? m_VueCycle.portail : null;
            }
        }

        bool PortailVillageOuvert => P != null && P.EnCours && P.Etat.phase == Phase.Jour && Pret && PortailVillage != null;

        /// Portail de retour du donjon (le même portail de gemmes que celui du village), ou null (anneau hors jeu).
        PortalVisual PortailRetourVisuel => generateur != null ? generateur.VisuelPortailRetour : null;

        /// Code réseau d'un portail (effets de passage 203 et 204) : 0 aucun, 1 village, 2 retour du donjon.
        public const int AucunPortail = 0, CodeVillage = 1, CodeRetour = 2;
        int CodeDe(PortalVisual pv) => pv == null ? AucunPortail : pv == PortailVillage ? CodeVillage : pv == PortailRetourVisuel ? CodeRetour : AucunPortail;
        PortalVisual PortailDe(int code) => code == CodeVillage ? PortailVillage : code == CodeRetour ? PortailRetourVisuel : null;

        /// Sortie du portail du village : 5 m devant lui, du côté de Nyxessa (la caméra reste hors du portail).
        Vector3 SortieVillage
        {
            get
            {
                // Nouvelle carte (grotte) : au pied de l'escalier de la grotte, dos au portail.
                if (PiedEscalier(out var pied, out _)) return pied;
                var pv = PortailVillage;
                Vector3 c = pv != null ? pv.Center : Vector3.zero;
                Vector3 n = P != null && P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero;
                Vector3 d = n - c; d.y = 0f;
                Vector3 p = new Vector3(c.x, 0f, c.z) + (d.sqrMagnitude > 0.01f ? d.normalized : Vector3.back) * 5f;
                if (NavMesh.SamplePosition(p + Vector3.up, out var hit, 3f, NavMesh.AllAreas)) p = hit.position;
                return p;
            }
        }

        /// Point d'arrivée au donjon : devant la dalle d'arrivée (adossée au mur extérieur), pour laisser du recul à la caméra.
        Vector3 PointArrivee
        {
            get
            {
                Transform a = RepereArrivee.transform;
                Vector3 p = a.position;
                // Donjon en terrasses : un des quatre points d'arrivée des joueurs (zone de 6 × 5 m), selon le joueur.
                if (Terrasses && m_Terrasses != null && m_Terrasses.Joueurs.Count > 0)
                {
                    int id = P != null && P.HerosLocal != null ? P.HerosLocal.Id : 1;
                    Vector3 j = m_Terrasses.Joueurs[((id - 1) % m_Terrasses.Joueurs.Count + m_Terrasses.Joueurs.Count) % m_Terrasses.Joueurs.Count].position;
                    return NavMesh.SamplePosition(j + Vector3.up * 0.3f, out var hj, 1.5f, NavMesh.AllAreas) ? hj.position : j;
                }
                if (!NavMesh.SamplePosition(p + Vector3.up * 0.3f, out var h0, 1f, NavMesh.AllAreas)) return p;
                for (float d = 3f; d >= 1f; d -= 1f)
                {
                    if (!NavMesh.SamplePosition(p + a.forward * d + Vector3.up * 0.3f, out var h1, 0.5f, NavMesh.AllAreas)) continue;
                    if (!NavMesh.Raycast(h0.position, h1.position, out _, NavMesh.AllAreas)) return h1.position;
                }
                return h0.position;
            }
        }

        static float Horizontal(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

        // ================================================================== Un donjon neuf chaque jour

        void OnPhase(Phase avant, Phase apres)
        {
            if (!ReseauJeu.Autorite) return;
            if (apres == Phase.Jour) NouveauDonjon();
            else if (apres == Phase.Crepuscule) { RappelerTous(); RetirerGardiens(); FermerSacs(); }
        }

        /// Autorité : nouvelle graine, construction, gardiens.
        public void NouveauDonjon()
        {
            int graine = Random.Range(1, 1000000);
            if (graine == GraineCourante) graine++;
            Construire(graine);
            PoserGardiens();
        }

        /// Tous les postes : construit le donjon de cette graine (butins remis en place). `essai` ≥ 0 : plan imposé
        /// par l'hôte (clients) ; -1 : l'autorité choisit elle-même l'essai.
        void Construire(int graine, int essai = -1)
        {
            if (Terrasses) { ConstruireTerrasses(graine); return; }
            if (generateur == null || graine == GraineCourante) return;
            RetirerGardiens();
            ViderSacsLocal();
            if (ReseauJeu.Autorite) PartieReseau.Instance?.ViderSacs();
            float t0 = Time.realtimeSinceStartup;
            bool ok = generateur.Generer(graine, essai);
            EssaiCourant = generateur.EssaiRetenu;
            GraineCourante = graine;
            Pris = 0; m_PrisVus = 0;
            for (int i = 0; i < m_Demande.Length; i++) m_Demande[i] = 0f;
            PreparerButins();
            P?.Journal("Donjon : graine " + graine + (ok ? "" : " (hors consigne)") + ", chemin critique " + (generateur.Plan.cheminCritiqueDm / 10f).ToString("F0") + " m, construit en "
                + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("F0") + " ms");
        }

        void PreparerButins()
        {
            for (int i = 0; i < Butins.Count; i++)
            {
                var r = Butins[i];
                if (r == null) continue;
                if (r.visuel != null) r.visuel.SetActive(ButinActif(i));
                var coffre = r.butin != TypeButin.TasOr ? CoffreDe(r.visuel, r.butin == TypeButin.GrandCoffre) : null;
                if (coffre != null)
                {
                    if (coffre.couvercle != null) coffre.couvercle.localRotation = coffre.ferme;
                    if (coffre.cadenas != null) { coffre.cadenas.gameObject.SetActive(true); coffre.cadenas.Reinitialiser(); }
                    if (coffre.corps != null && coffre.maillagePlein != null) coffre.corps.sharedMesh = coffre.maillagePlein;
                }
                var c = r.GetComponent<CoffreDonjon>();
                if (r.butin != TypeButin.TasOr) { if (c == null) c = r.gameObject.AddComponent<CoffreDonjon>(); c.enabled = true; }
                else if (c != null) c.enabled = false;
            }
        }

        /// Couvercle (et cadenas, désactivé) d'un coffre, relevé une fois : les coffres sont réutilisés d'un donjon à
        /// l'autre. Le modèle vient du kit (DonjonKit.ModeleGrandCoffre / ModeleCoffre : les modèles sans serrure dès
        /// qu'ils y sont renseignés) ; le couvercle est l'enfant dont le nom finit par DonjonKit.suffixeCouvercle
        /// (« _lid » des coffres KayKit).
        Coffre CoffreDe(GameObject visuel, bool grand)
        {
            if (visuel == null) return null;
            if (m_Coffres.TryGetValue(visuel, out var c)) return c;
            c = new Coffre();
            var kit = generateur != null ? generateur.kit : null;
            string suffixe = kit != null && !string.IsNullOrEmpty(kit.suffixeCouvercle) ? kit.suffixeCouvercle : "_lid";
            foreach (var t in visuel.GetComponentsInChildren<Transform>(true))
                if (t != visuel.transform && (t.name.EndsWith(suffixe, System.StringComparison.OrdinalIgnoreCase) || t.name.EndsWith("_lid")))
                { c.couvercle = t; c.ferme = t.localRotation; break; }
            // Corps : le premier maillage qui n'est pas sous le couvercle et qui contient des pièces d'or.
            foreach (var mf in visuel.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || (c.couvercle != null && mf.transform.IsChildOf(c.couvercle))) continue;
                if (SansPieces(mf.sharedMesh) == mf.sharedMesh) continue;
                c.corps = mf; c.maillagePlein = mf.sharedMesh; break;
            }
            // Cadenas : désactivé (CadenasActifs), code gardé pour les coffres à clé.
            var prefab = !CadenasActifs ? null : grand ? cadenasGrandCoffre : cadenasCoffre;
            if (prefab != null)
            {
                // Sur la face avant (+Z du modèle), accroché au bord du couvercle. Calcul dans le repère du modèle : le
                // masquage des étages peut l'avoir réduit à zéro.
                bool premier = true;
                Bounds b = new Bounds();
                foreach (var mf in visuel.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    Matrix4x4 m = Local(mf.transform, visuel.transform);
                    Bounds mb = mf.sharedMesh.bounds;
                    for (int k = 0; k < 8; k++)
                    {
                        Vector3 coin = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)));
                        if (premier) { b = new Bounds(coin, Vector3.zero); premier = false; } else b.Encapsulate(coin);
                    }
                }
                float bord = c.couvercle != null ? Local(c.couvercle, visuel.transform).MultiplyPoint3x4(Vector3.zero).y : b.min.y + b.size.y * 0.55f;
                var go = Instantiate(prefab, visuel.transform, false);
                go.name = prefab.name;
                go.transform.localPosition = new Vector3(b.center.x, bord - 0.12f, b.max.z + 0.07f);
                // Serrure (+Z du prefab, côté verrou) vers l'extérieur, pour que la clé se voie (tourné de 180° jusqu'au
                // 26/09/2026 : la clé entrait par l'intérieur, invisible ; même pose que CadenasFaceAvant de la planche).
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * (grand ? 1.25f : 1f);
                c.cadenas = go.GetComponent<CadenasOuverture>();
            }
            m_Coffres[visuel] = c;
            return c;
        }

        /// Matrice de `t` dans le repère de `racine` (sans passer par les échelles monde).
        static Matrix4x4 Local(Transform t, Transform racine)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (; t != null && t != racine; t = t.parent) m = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * m;
            return m;
        }

        int Montant(TypeButin t)
        {
            var b = B;
            int baseOr = t == TypeButin.GrandCoffre ? b.orGrandCoffre : t == TypeButin.Coffre ? b.orCoffre : b.orTasOr;
            int nuit = P != null ? P.Etat.nuit : 1;
            return Mathf.RoundToInt(baseOr * (1f + b.orDonjonParNuit * Mathf.Max(0, nuit - 1)));
        }

        public int MontantButin(int index) => index >= 0 && index < Butins.Count && Butins[index] != null ? Montant(Butins[index].butin) : 0;
        public bool ButinPris(int index) => (Pris & (1 << index)) != 0;
        /// Butin présent dans ce donjon : les tas d'or au sol sont retirés depuis le 30/09/2026 (GameBalance.tasOrDonjon).
        public bool ButinActif(int index) => index >= 0 && index < Butins.Count && Butins[index] != null
            && (Butins[index].butin != TypeButin.TasOr || B.tasOrDonjon);

        // ================================================================== Donjon en terrasses (aperçu de la nouvelle carte)

        /// Tous les postes : construit le donjon en terrasses de cette graine. Le plan est déterministe (même graine, même
        /// donjon sur toutes les machines, Docs/donjon-generateur.md) : seule la graine circule (PartieReseau.GraineDonjon),
        /// l'essai retenu ne sert pas. Puis butins (coffres), interactions de l'aperçu, emprise et enceintes de la caméra.
        void ConstruireTerrasses(int graine)
        {
            if (graine == GraineCourante && m_Terrasses != null) return;
            if (m_Terrasses == null && !CreerTerrasses()) return;
            RetirerGardiens();
            ViderSacsLocal();
            if (ReseauJeu.Autorite) PartieReseau.Instance?.ViderSacs();
            float t0 = Time.realtimeSinceStartup;
            bool ok = m_Terrasses.Generer(graine);
            EssaiCourant = 0;
            GraineCourante = graine;
            Pris = 0; m_PrisVus = 0;
            for (int i = 0; i < m_Demande.Length; i++) m_Demande[i] = 0f;
            MesurerTerrasses();
            PreparerButins();
            PoserMecanismesApercu();
            var pl = m_Terrasses.Plan;
            P?.Journal("Donjon en terrasses : graine " + graine + (ok ? "" : " (hors consigne)") + ", " + pl.NomVariante + ", " + pl.Niveaux + " niveaux, "
                + pl.L + " x " + pl.P + " m, " + m_Terrasses.Butins.Count + " coffres, " + m_Terrasses.Apparitions.Count + " apparitions, " + m_Terrasses.Portes.Count + " porte(s), construit en "
                + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("F0") + " ms");
            if (m_Terrasses.Butins.Count > MaxButins) Debug.LogWarning("Donjon en terrasses : " + m_Terrasses.Butins.Count + " coffres, seuls les " + MaxButins + " premiers sont suivis");
        }

        /// Constructeur du donjon en terrasses, sous ce donjon (OrigineTerrasses). Matériaux chargés des Resources
        /// (Assets/Jeu/Resources/DonjonTerrasses/) : ils sont dans le build sans toucher à la scène.
        bool CreerTerrasses()
        {
            var pierre = Resources.Load<Material>("DonjonTerrasses/DonjonTerrasses_Pierre");
            var flamme = Resources.Load<Material>("DonjonTerrasses/DonjonTerrasses_Flamme");
            if (pierre == null) { Debug.LogWarning("Donjon en terrasses : matériau DonjonTerrasses_Pierre introuvable dans les Resources"); return false; }
            var go = new GameObject("DonjonTerrasses");
            go.SetActive(false);   // le constructeur ne génère rien à son Awake : il est réglé avant d'être activé
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(OrigineTerrasses, Quaternion.identity);
            m_Terrasses = go.AddComponent<ConstructeurTerrasses>();
            m_Terrasses.genererAuDemarrage = false;
            m_Terrasses.construireNavMesh = true;
            m_Terrasses.voute = true;
            m_Terrasses.reperesVisibles = false;
            m_Terrasses.materiauPierre = pierre;
            m_Terrasses.materiauFlamme = flamme;
            go.SetActive(true);
            return true;
        }

        /// Emprise (« au donjon »), boîte de la grande salle et des pièces cachées (enceintes de la caméra), en monde.
        void MesurerTerrasses()
        {
            var pl = m_Terrasses.Plan;
            Vector3 o = m_Terrasses.transform.position;
            float m = PlanTerrasses.Marge;
            s_Emprise = new Bounds(o + new Vector3(pl.L * 0.5f, pl.Cle * 0.5f, pl.P * 0.5f), new Vector3(pl.L + 2f * m + 2f, pl.Cle + 6f, pl.P + 2f * m + 2f));
            // Salle : faces intérieures des murs (les blocs saillent de 0,12 m), du sol à 0,4 m sous la naissance de la voûte
            // (la voûte n'a pas de collision : la caméra ne monte pas jusqu'à elle).
            const float bord = 0.2f;
            float y0 = -1f, y1 = pl.Naissance - 0.4f;
            m_SalleTerrasses = new Bounds(o + new Vector3(pl.L * 0.5f, (y0 + y1) * 0.5f, pl.P * 0.5f), new Vector3(pl.L - 2f * bord, y1 - y0, pl.P - 2f * bord));
            m_PiecesTerrasses.Clear();
            foreach (var pc in pl.pieces)
            {
                float py0 = pc.sol - 0.5f, py1 = pc.plafond - 0.3f;
                m_PiecesTerrasses.Add(new Bounds(o + new Vector3(pc.r.CentreX, (py0 + py1) * 0.5f, pc.r.CentreZ),
                    new Vector3(pc.r.Largeur - 2f * bord, py1 - py0, pc.r.Profondeur - 2f * bord)));
            }
        }

        /// Enceinte de la caméra au donjon en terrasses : la pièce cachée où est le héros, sinon la grande salle ; null
        /// dans le passage d'une arche à travers l'enceinte (la caméra s'y règle sur les collisions, comme au village).
        Bounds? EnceinteTerrasses(Vector3 pieds)
        {
            if (m_Terrasses == null) return null;
            Vector3 q = pieds + Vector3.up * 0.6f;
            for (int i = 0; i < m_PiecesTerrasses.Count; i++) if (m_PiecesTerrasses[i].Contains(q)) return m_PiecesTerrasses[i];
            return m_SalleTerrasses.Contains(q) ? m_SalleTerrasses : (Bounds?)null;
        }

        /// Centre de l'arche du portail de retour (gemmes du passage) : le donjon en terrasses n'a pas de PortalVisual.
        Vector3? CentreRetourTerrasses => Terrasses && RepereRetour != null
            ? RepereRetour.transform.position + Vector3.up * 2.4f + RepereRetour.transform.forward * 0.3f : (Vector3?)null;

        /// Aperçu seulement : portes à serrure ouvertes par la touche Interagir (sans clé), bouton mural des pièces secrètes
        /// aussi par la touche (la plaque s'enfonce sous les pas) ; un grondement quand une paroi secrète descend.
        void PoserMecanismesApercu()
        {
            if (!Partie.Exploration || m_Terrasses == null) return;
            foreach (var porte in m_Terrasses.Portes)
            {
                if (porte == null) continue;
                if (porte.genre == GenrePiece.Verrouillee && porte.GetComponent<MecanismeApercu>() == null) porte.gameObject.AddComponent<MecanismeApercu>().porte = porte;
                if (porte.genre == GenrePiece.Secrete) porte.Ouverture += GrondementParoi;
            }
            foreach (var d in m_Terrasses.Declencheurs)
                if (d != null && d.genre == GenreDeclencheur.BoutonMural && d.GetComponent<MecanismeApercu>() == null) d.gameObject.AddComponent<MecanismeApercu>().bouton = d;
        }

        static void GrondementParoi(PorteDonjon pd)
        {
            var bc = pd.GetComponent<BoxCollider>();
            AudioBank.Jouer(SonsDuJeu.GolemCoup, pd.transform.TransformPoint(bc != null ? bc.center : Vector3.zero), 0.6f);
        }

        /// Autorité : gardiens du donjon en terrasses, un par point d'apparition du plan (14), du type du plan (sbire,
        /// guerrier, voleur, mage), tournés vers la salle ; ils gardent leur poste et poursuivent les héros en vue.
        void PoserGardiensTerrasses(DirecteurVagues dv)
        {
            var ap = Apparitions;
            for (int k = 0; k < ap.Count; k++)
            {
                var a = ap[k];
                if (a == null) continue;
                Vector3 p = a.transform.position;
                if (NavMesh.SamplePosition(p + Vector3.up * 0.5f, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
                var type = a.apparition == TypeApparition.Guerrier ? TypeEnnemi.Guerrier : a.apparition == TypeApparition.Voleur ? TypeEnnemi.Voleur
                    : a.apparition == TypeApparition.Mage ? TypeEnnemi.Mage : TypeEnnemi.Sbire;
                var sq = dv.Poser(type, p, false, false);
                if (sq == null) continue;
                sq.transform.rotation = a.transform.rotation;
                sq.Garder(p);
                m_Gardiens.Add(sq);
            }
            P?.Journal("Donjon en terrasses : " + m_Gardiens.Count + " gardiens sur " + ap.Count + " points d'apparition");
        }

        /// Pied de l'escalier de la grotte (nouvelle carte) et direction qui s'éloigne du portail ; faux sans grotte
        /// (ancienne carte). Cherché une fois : le bord de marche le plus éloigné du portail, puis 1,5 m plus loin, sur le
        /// NavMesh.
        bool PiedEscalier(out Vector3 pied, out Vector3 dehors)
        {
            if (!m_PiedCherche)
            {
                var pv = PortailVillage;
                var esc = pv != null ? GameObject.Find("Grotte_Escalier") : null;
                if (pv != null) m_PiedCherche = true;
                if (esc != null)
                {
                    var rs = esc.GetComponentsInChildren<Renderer>();
                    Vector3 c = pv.Center; c.y = 0f;
                    Bounds tout = default; bool premier = true;
                    foreach (var r in rs) { if (premier) { tout = r.bounds; premier = false; } else tout.Encapsulate(r.bounds); }
                    Vector3 d = tout.center - c; d.y = 0f;
                    if (!premier && d.sqrMagnitude > 0.01f)
                    {
                        d.Normalize();
                        float loin = 0f;
                        foreach (var r in rs)
                        {
                            Bounds b = r.bounds;
                            float e = Vector3.Dot(new Vector3(b.center.x, 0f, b.center.z) - c, d) + Mathf.Abs(d.x) * b.extents.x + Mathf.Abs(d.z) * b.extents.z;
                            if (e > loin) loin = e;
                        }
                        Vector3 p = c + d * (loin + 1.5f);
                        p.y = tout.min.y;
                        if (NavMesh.SamplePosition(p + Vector3.up, out var hit, 3f, NavMesh.AllAreas)) p = hit.position;
                        m_PiedEscalier = p;
                        m_SortieEscalier = d;
                    }
                }
            }
            pied = m_PiedEscalier ?? Vector3.zero;
            dehors = m_SortieEscalier;
            return m_PiedEscalier.HasValue;
        }

        /// Aperçu seulement (touche de dev, autorité) : F8 construit la graine suivante, Maj + F8 une graine au hasard ; le
        /// héros local au donjon est reposé à l'arrivée. Les commandes du jeu normal ne changent pas (F8 n'y est lié à rien).
        void ToucheGraine()
        {
            if (m_Transit || !ReseauJeu.Autorite || !AppuiApercu(UnityEngine.InputSystem.Key.F8, m_F8)) return;
            ChangerGraine(MajEnfoncee() ? Random.Range(1, 1000000) : GraineCourante + 1);
        }

        // Touches de dev F8 / F9 / F10 : lues sur TOUS les claviers (InputSystem.devices filtré), pas sur Keyboard.current, qui ne suit que le
        // dernier clavier ayant émis (03/10/2026). Un appui = exactement une action : la touche est lue comme un ÉTAT agrégé (enfoncée sur
        // au moins un clavier), pas comme un front par clavier. Cause du bug « jour, nuit, jour, nuit… avant de se stabiliser » : le front
        // wasPressedThisFrame était relu sur chaque périphérique clavier et chaque image ; un clavier exposant plusieurs interfaces, ou
        // renvoyant l'appui (répétition, resynchronisation de l'état au changement de focus) à plus de 0,2 s d'intervalle, relançait
        // la bascule autant de fois. Désormais l'action part au passage « relâchée → enfoncée » de l'état agrégé, et la touche ne se
        // réarme qu'après 0,3 s continues sans aucun clavier qui la signale enfoncée (les rebonds et répétitions ne comptent pas).
        const float RearmementToucheApercu = 0.3f;
        sealed class ToucheDev { public bool armee = true; public float vue = -10f; }
        readonly ToucheDev m_F8 = new ToucheDev(), m_F9 = new ToucheDev(), m_F10 = new ToucheDev();

        static bool AppuiApercu(UnityEngine.InputSystem.Key touche, ToucheDev e)
        {
            var peripheriques = UnityEngine.InputSystem.InputSystem.devices;
            bool enfoncee = false;
            for (int i = 0; i < peripheriques.Count && !enfoncee; i++)
            {
                if (!(peripheriques[i] is UnityEngine.InputSystem.Keyboard clavier)) continue;
                var c = clavier[touche];
                enfoncee = c.isPressed || c.wasPressedThisFrame;
            }
            float t = Time.unscaledTime;
            if (enfoncee)
            {
                e.vue = t;
                if (!e.armee) return false;
                e.armee = false;
                return true;
            }
            if (!e.armee && t - e.vue >= RearmementToucheApercu) e.armee = true;
            return false;
        }

        static bool MajEnfoncee()
        {
            var peripheriques = UnityEngine.InputSystem.InputSystem.devices;
            for (int i = 0; i < peripheriques.Count; i++)
                if (peripheriques[i] is UnityEngine.InputSystem.Keyboard clavier && clavier.shiftKey.isPressed) return true;
            return false;
        }

        /// Aperçu seulement (touches de dev, comme F8 ; le jeu normal ne les lit pas) : F9 bascule jour / nuit (ambiance
        /// seulement : la phase de jeu reste le jour figé, donc pas de vague ; VueCycle fait le fondu), F10 bascule
        /// « sans mob » / « avec mob ». Les deux états (Partie.NuitApercu, Partie.SansMobApercu) tiennent à travers les
        /// portails et s'appliquent aux donjons que F8 construit ensuite.
        void ToucheApercu()
        {
            if (AppuiApercu(UnityEngine.InputSystem.Key.F9, m_F9))
            {
                Partie.DefinirNuitApercu(!Partie.NuitApercu);
                Dire(Partie.NuitApercu ? "Nuit" : "Jour", 3f);
            }
            if (!m_Transit && ReseauJeu.Autorite && AppuiApercu(UnityEngine.InputSystem.Key.F10, m_F10)) BasculerMobsApercu();
        }

        /// Autorité, aperçu : « sans mob » retire tous les ennemis vivants (gardiens du donjon, squelettes d'essai) et plus
        /// rien n'apparaît (DirecteurVagues.Poser) ; « avec mob » remet les gardiens aux points d'apparition du donjon et,
        /// quand le héros est dehors (aucun ennemi n'existe sur la carte extérieure de l'aperçu), quelques squelettes d'essai
        /// à quelques mètres de lui (des gardiens de poste, qui le poursuivent s'il est en vue).
        void BasculerMobsApercu()
        {
            var dv = DirecteurVagues.Instance;
            bool sans = !Partie.SansMobApercu;
            Partie.DefinirSansMobApercu(sans);
            if (sans)
            {
                RetirerGardiens();
                dv?.RetirerTous();
                Dire("Mobs : désactivés", 4f);
                return;
            }
            Dire("Mobs : activés", 4f);
            if (dv == null) return;
            if (Pret) { RetirerGardiens(); PoserGardiens(); }
            var h = P != null ? P.HerosLocal : null;
            if (h != null && !AuDonjon(h)) PoserEssaisExterieur(dv, h);
        }

        /// Quatre squelettes d'essai (sbires, un guerrier) en arc devant le héros, à 8 m, sur le NavMesh.
        void PoserEssaisExterieur(DirecteurVagues dv, Heros h)
        {
            int poses = 0;
            Vector3 devant = h.transform.forward; devant.y = 0f;
            if (devant.sqrMagnitude < 0.01f) devant = Vector3.forward;
            devant.Normalize();
            for (int k = 0; k < 4; k++)
            {
                Vector3 d = Quaternion.Euler(0f, -45f + 30f * k, 0f) * devant;
                if (!NavMesh.SamplePosition(h.transform.position + d * 8f, out var hit, 4f, NavMesh.AllAreas)) continue;
                var sq = dv.Poser(k == 3 ? TypeEnnemi.Guerrier : TypeEnnemi.Sbire, hit.position, false, false);
                if (sq == null) continue;
                sq.Garder(hit.position);
                poses++;
            }
            P?.Journal("Aperçu : " + poses + " squelettes d'essai près du joueur (F10)");
        }

        /// Autorité, aperçu : reconstruit le donjon de cette graine (gardiens compris) ; le héros local au donjon revient
        /// à l'arrivée. Aussi appelé par les outils de test (ScenariosDonjon.Graine).
        public void ChangerGraine(int graine)
        {
            if (!Terrasses || !ReseauJeu.Autorite) return;
            if (graine <= 0) graine = 1;
            var h = P != null ? P.HerosLocal : null;
            bool dedans = AuDonjon(h);
            if (graine == GraineCourante) GraineCourante = 0;   // même graine : reconstruite (gardiens remis)
            Construire(graine);
            PoserGardiens();
            if (dedans && h != null && Pret)
            {
                h.Teleporter(PointArrivee + Vector3.up * 0.05f);
                Vector3 v = RepereArrivee.transform.forward; v.y = 0f;
                if (v.sqrMagnitude > 0.01f)
                {
                    h.transform.rotation = Quaternion.LookRotation(v);
                    if (P.cameraJeu != null) P.cameraJeu.lacet = h.transform.eulerAngles.y;
                }
            }
            AnnoncerGraine();
        }

        /// Message du HUD : graine du donjon en terrasses et touche de dev (aperçu).
        void AnnoncerGraine()
        {
            if (!Terrasses || m_Terrasses == null) return;
            var pl = m_Terrasses.Plan;
            Dire("Donjon : graine " + GraineCourante + " (" + pl.NomVariante + ", " + pl.Niveaux + " niveaux) - F8 : graine suivante, Maj+F8 : au hasard", 8f);
        }

        // ================================================================== Gardiens

        void PoserGardiens()
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null || !Pret) return;
            if (Terrasses) { PoserGardiensTerrasses(dv); return; }
            var b = B;
            // Butins du plus riche au plus modeste ; un gardien par butin, puis un deuxième sur les plus riches.
            var ordre = new List<int>();
            for (int i = 0; i < Butins.Count; i++) if (ButinActif(i)) ordre.Add(i);
            ordre.Sort((x, y) => ((int)Butins[x].butin).CompareTo((int)Butins[y].butin));
            var pris = new HashSet<DonjonRepere>();
            int nb = Mathf.Max(0, b.gardiensDonjon), guerriers = Mathf.RoundToInt(nb * b.partGuerriersDonjon);
            for (int k = 0; k < nb && ordre.Count > 0; k++)
            {
                var butin = Butins[ordre[k % ordre.Count]];
                DonjonRepere meilleur = null; float dmin = float.MaxValue;
                foreach (var a in Apparitions)
                {
                    if (a == null || pris.Contains(a) || a.niveau != butin.niveau && Mathf.Abs(a.transform.position.y - butin.transform.position.y) > 1.5f) continue;
                    float d = (a.transform.position - butin.transform.position).sqrMagnitude;
                    if (d < dmin) { dmin = d; meilleur = a; }
                }
                if (meilleur == null) continue;
                pris.Add(meilleur);
                Vector3 p = meilleur.transform.position;
                if (NavMesh.SamplePosition(p + Vector3.up * 0.5f, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
                var sq = dv.Poser(k < guerriers ? TypeEnnemi.Guerrier : TypeEnnemi.Sbire, p, false, false);
                if (sq == null) continue;
                sq.Garder(p);
                m_Gardiens.Add(sq);
            }
            P?.Journal("Donjon : " + m_Gardiens.Count + " gardiens (" + guerriers + " guerriers)");
        }

        void RetirerGardiens()
        {
            foreach (var s in m_Gardiens) if (s != null && s.Vivant) s.Desintegrer(true);
            m_Gardiens.Clear();
        }

        public int GardiensVivants { get { int n = 0; foreach (var s in m_Gardiens) if (s != null && s.Vivant) n++; return n; } }

        // ================================================================== Butin (l'autorité décide)

        /// Ce poste demande le butin `index` pour son joueur (coffre ouvert, tas d'or ramassé).
        public void DemanderButin(int index)
        {
            if (index < 0 || index >= m_Demande.Length || ButinPris(index) || Time.time < m_Demande[index]) return;
            m_Demande[index] = Time.time + 1f;
            var h = P != null ? P.HerosLocal : null;
            if (h == null) return;
            if (Partie.ClientReseau) PartieReseau.Instance?.DemanderButin(index);
            else Accorder(index, h.Id);
        }

        /// Autorité : le butin `index` va au joueur `joueurId` (s'il est encore là et que le joueur est à côté).
        public void Accorder(int index, int joueurId)
        {
            if (!Pret || index < 0 || index >= Butins.Count || ButinPris(index) || !ButinActif(index) || P == null) return;
            var r = Butins[index];
            var h = P.HerosDe(joueurId);
            var j = P.Joueur(joueurId);
            if (r == null || h == null || j == null || !h.Vivant) return;
            if (Vector3.Distance(h.transform.position, r.transform.position) > B.distanceCoffre + 1.5f) return;
            Pris |= 1 << index;
            // Attributs (01/10/2026) : Chance gagnée, +or ramassé (points du joueur reçus par HerosReseau chez l'hôte).
            int or = Mathf.RoundToInt(Montant(r.butin) * Attributs.FacteurOr(j));
            j.orPorte += or;
            if (r.butin != TypeButin.TasOr) Deathless.Succes.ServiceSucces.CoffreOuvert(joueurId);   // succès (coffres)
            int trouve = r.butin == TypeButin.TasOr ? 0 : TirerObjets(r.butin == TypeButin.GrandCoffre);
            int recu = Inventaire.AjouterTasse(j, trouve);   // l'hôte tient la copie de ce joueur ; son poste l'applique à la réception
            P.Journal("Donjon : " + j.nom + " prend " + (r.butin == TypeButin.GrandCoffre ? "le grand coffre" : r.butin == TypeButin.Coffre ? "un coffre" : "un tas d'or") + " (" + or + " or ; porté : " + j.orPorte + ")"
                + (trouve != 0 ? ", trouve " + Inventaire.TexteTasse(trouve) + (recu != trouve ? " (reçu : " + (recu != 0 ? Inventaire.TexteTasse(recu) : "rien") + ")" : "") : ""));
            if (trouve != 0)
            {
                if (EstLocal(j)) AnnoncerTrouvaille(trouve, recu);
                else if (ReseauJeu.EnPartie) PartieReseau.Instance?.DonnerObjets((ulong)(joueurId - 1), recu, trouve);
            }
        }

        /// Ce joueur est celui de ce poste (sinon c'est la copie que l'hôte tient d'un autre poste).
        bool EstLocal(EtatJoueur j) => P != null && P.JoueurLocal != null && j != null && P.JoueurLocal.id == j.id;

        /// Autorité : clés et kit trouvés dans un coffre (chances et quantités dans GameBalance), emballés (Inventaire.TasseDe).
        int TirerObjets(bool grand)
        {
            int tasse = 0;
            if (Random.value < (grand ? B.cleGrandCoffreChance : B.cleCoffreChance))
            {
                Vector3 w = grand ? B.cleGrandCoffrePoids : B.cleCoffrePoids;
                float tirage = Random.value * Mathf.Max(0.0001f, w.x + w.y + w.z);
                var cle = tirage < w.x ? ArticleBoutique.CleBronze : tirage < w.x + w.y ? ArticleBoutique.CleArgent : ArticleBoutique.CleOr;
                tasse |= Inventaire.TasseDe(cle, Mathf.Max(1, grand ? B.cleGrandCoffreQuantite : B.cleCoffreQuantite));
            }
            if (Random.value < (grand ? B.kitGrandCoffreChance : B.kitCoffreChance))
                tasse |= Inventaire.TasseDe(ArticleBoutique.KitCrochetage, Mathf.Max(1, B.crochetsParKit));
            return tasse;
        }

        /// Ce poste : annonce du HUD pour ce qui a été trouvé dans un coffre (`recu` : ce qui est vraiment entré dans l'inventaire).
        public void AnnoncerTrouvaille(int trouve, int recu)
        {
            string txt = Inventaire.TexteTasse(trouve);
            if (txt.Length == 0) return;
            Dire(recu == 0 ? "Tu trouves " + txt + ", mais tu n’as plus de place" : "Tu trouves " + txt, 5f);
            if (recu != 0) AudioBank.Jouer2D(SonsDuJeu.AchatCle, 0.8f);
        }

        /// Poste d'un client : l'hôte lui a accordé ces objets (coffre, sac) ; ils entrent dans son inventaire.
        public static void RecevoirObjets(int recu, int trouve)
        {
            var p = Partie.Instance;
            if (p == null) return;
            Inventaire.AjouterTasse(p.JoueurLocal, recu);
            if (Instance != null && trouve != 0) Instance.AnnoncerTrouvaille(trouve, recu);
        }

        /// Tous les postes : les butins pris depuis la dernière image s'ouvrent (coffre) ou disparaissent (tas d'or).
        void SuivreButins()
        {
            int nouveaux = Pris & ~m_PrisVus;
            if (nouveaux == 0) return;
            m_PrisVus |= nouveaux;
            for (int i = 0; i < Butins.Count; i++)
                if ((nouveaux & (1 << i)) != 0) StartCoroutine(Ouvrir(i));
        }

        IEnumerator Ouvrir(int i)
        {
            var r = Butins[i];
            if (r == null) yield break;
            var cd = r.GetComponent<CoffreDonjon>(); if (cd != null) cd.enabled = false;
            Vector3 p = r.transform.position + Vector3.up * 0.8f;
            if (r.butin == TypeButin.TasOr)
            {
                if (r.visuel != null) r.visuel.SetActive(false);
                PieceOr.Jouer(p);
                AudioBank.Jouer(SonsDuJeu.Or, p, 0.6f);
                yield break;
            }
            var c = CoffreDe(r.visuel, r.butin == TypeButin.GrandCoffre);
            // Cadenas (désactivé, CadenasActifs) : il s'ouvrait avant le couvercle. Sans lui, le couvercle bascule aussitôt.
            if (CadenasActifs && c != null && c.cadenas != null && c.cadenas.gameObject.activeInHierarchy)
            {
                c.cadenas.Ouvrir();
                AudioBank.Jouer(SonsDuJeu.CoffreCadenas, c.cadenas.transform.position, 0.8f);
                yield return new WaitForSeconds(0.9f);
            }
            AudioBank.Jouer(SonsDuJeu.CoffreOuvert, p, 0.9f);
            if (c != null && c.couvercle != null)
            {
                Quaternion ouvert = c.ferme * Quaternion.Euler(-105f, 0f, 0f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
                {
                    c.couvercle.localRotation = Quaternion.Slerp(c.ferme, ouvert, 1f - (1f - t) * (1f - t));
                    yield return null;
                }
                c.couvercle.localRotation = ouvert;
            }
            // L'or s'envole : le tas de pièces du modèle disparaît (maillage sans pièces) et part en gemmes d'or (thème Sacré,
            // le seul thème doré des palettes), rien ne reste « à ramasser » (point 1 de l'audit du 27/09/2026).
            if (c != null && c.corps != null && c.maillagePlein != null) c.corps.sharedMesh = SansPieces(c.maillagePlein);
            EnvolOr(r.transform, r.butin == TypeButin.GrandCoffre ? 44 : 24);
            AudioBank.Jouer(SonsDuJeu.Or, p, 0.7f);
        }

        /// Petit tas de gemmes d'or qui s'envole du coffre (GemmesVolantes, langage visuel des effets du projet).
        static void EnvolOr(Transform coffre, int nombre)
        {
            var mat = EffetsJeu.Gemmes;
            if (mat == null) return;
            var gemmes = GemmesVolantes.Creer("Coffre_Or", mat, nombre + 8, true);
            Color sombre = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Base, new Color(0.722f, 0.565f, 0.227f));
            Color vif = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Vif, new Color(0.910f, 0.784f, 0.447f));
            Color coeur = VfxPalette.Couleur(VfxTheme.Sacre, VfxRole.Coeur, new Color(0.957f, 0.886f, 0.659f));
            Vector3 centre = coffre.position + Vector3.up * 0.55f;
            for (int i = 0; i < nombre; i++)
            {
                Vector3 depart = centre + coffre.right * Random.Range(-0.5f, 0.5f) + coffre.forward * Random.Range(-0.3f, 0.3f) + Vector3.up * Random.Range(0f, 0.35f);
                Vector3 v = Vector3.up * Random.Range(1.6f, 3.4f) + coffre.right * Random.Range(-0.7f, 0.7f) + coffre.forward * Random.Range(-0.5f, 0.5f);
                float r = Random.value;
                Color col = r < 0.45f ? sombre : r < 0.9f ? vif : coeur;   // surtout de l'or franc, peu de reflets clairs
                gemmes.Emettre(depart, v, Random.Range(0.045f, 0.085f), Random.Range(0.55f, 0.95f), col, 4.5f, 0.6f, 0.04f, 0.45f, default(Vector3), Random.Range(0f, 0.25f));
            }
        }

        // ================================================================== Retour, dépôt, rappel

        /// Autorité : l'or porté du joueur est versé à la caisse (retour par le portail).
        public void Deposer(int joueurId)
        {
            if (P == null) return;
            var j = P.Joueur(joueurId);
            if (j == null || j.orPorte <= 0) return;
            int or = j.orPorte;
            j.orPorte = 0;
            P.GagnerOr(or, joueurId, SortieVillage + Vector3.up * 1.2f);
            P.Journal("Donjon : " + j.nom + " verse " + or + " or à la caisse commune (caisse : " + P.Etat.orEquipe + ")");
        }

        /// Part gardée de l'or porté selon le palier de Nyxessa.
        float PartGardee => GameBalance.AuPalier(B.partGardeeRappel, P != null ? P.Etat.nyxessa.palierMissiles : 1);

        /// Autorité : l'or porté d'un joueur rappelé (ou mort au donjon) : la part gardée va à la caisse, le reste est perdu.
        void Perdre(EtatJoueur j, out int garde, out int perdu, string raison)
        {
            garde = Mathf.FloorToInt(j.orPorte * PartGardee);
            perdu = j.orPorte - garde;
            j.orPorte = 0;
            if (garde > 0) P.GagnerOr(garde, j.id, (P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero) + Vector3.up * 3f);
            P.Journal("Donjon : " + j.nom + " " + raison + " : " + garde + " or gardés par Nyxessa, " + perdu + " perdus");
        }

        /// Autorité, au crépuscule : Nyxessa rappelle les joueurs encore au donjon.
        void RappelerTous()
        {
            if (P == null) return;
            foreach (var j in P.Etat.joueurs)
            {
                var h = P.HerosDe(j.id);
                if (!AuDonjon(h) || !h.Vivant) continue;
                Perdre(j, out int garde, out int perdu, "rappelé par Nyxessa");
                if (h.Distant) h.GetComponent<HerosReseau>()?.Rappeler(garde, perdu);
                else RappelLocal(garde, perdu);
            }
        }

        /// Propriétaire : Nyxessa ramène le héros local au village (dissolution, arrivée près d'elle).
        public void RappelLocal(int garde, int perdu)
        {
            var h = P != null ? P.HerosLocal : null;
            if (h == null || !AuDonjon(h) || !h.Vivant || m_Transit) return;
            Vector3 dest = P.PointReapparition(P.nyxessa != null ? P.nyxessa.transform.position : Vector3.zero);
            StartCoroutine(Transit(h, dest, false, null, null));
            Dire(perdu > 0 || garde > 0 ? "Rappelé par Nyxessa : " + garde + " or gardés, " + perdu + " perdus" : "Rappelé par Nyxessa", 6f);
            AudioBank.Jouer2D(SonsDuJeu.NyxessaRappel, 0.9f);
        }

        void Dire(string texte, float duree) { m_Message = texte; m_MessageJusqua = Time.time + duree; }

        /// Message court du HUD (même emplacement que les messages du donjon), pour d'autres annonces : réseau perdu et
        /// partie continuée en solo, retour d'un joueur en cours de partie (Docs/reseau.md).
        public void Annoncer(string texte, float duree) => Dire(texte, duree);

        // ================================================================== Sacs des joueurs morts au donjon

        /// Autorité, à la mort d'un joueur au donjon (Update) : un sac tombe à `position` avec tout l'or qu'il portait.
        /// N'importe quel joueur peut le ramasser tant que le donjon est ouvert (Wiki : deroule.md, Mort au donjon).
        void CreerSac(EtatJoueur j, Vector3 position)
        {
            if (j == null || (j.orPorte <= 0 && !Inventaire.ABut(j))) return;
            int id = ++m_ProchainSacId;
            int montant = j.orPorte;
            string nom = j.nom;
            int objets = Inventaire.TasserButin(j);   // le sac contient aussi ses clés et ses crochets (pas ses potions)
            j.orPorte = 0;
            Inventaire.RetirerButin(j);
            if (!EstLocal(j) && ReseauJeu.EnPartie) PartieReseau.Instance?.RetirerButinClient((ulong)(j.id - 1));
            m_Sacs.Add(new Sac { id = id, position = position, montant = montant, nom = nom, objets = objets });
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.CreerSacReseau(id, position, montant, nom, objets);
            P?.Journal("Donjon : " + nom + " meurt au donjon, un sac tombe (" + montant + " or" + (objets != 0 ? ", " + Inventaire.TexteTasse(objets) : "") + ")");
        }

        /// Autorité : le sac `id` va au joueur `joueurId`, s'il est encore là et que le joueur est à côté (marché dessus).
        public void RamasserSac(int id, int joueurId)
        {
            if (P == null) return;
            int idx = IndexSac(id);
            if (idx < 0) return;
            var sac = m_Sacs[idx];
            var h = P.HerosDe(joueurId);
            var j = P.Joueur(joueurId);
            if (h == null || j == null || !h.Vivant || Horizontal(h.transform.position, sac.position) > B.rayonTasOr + 1f) return;
            m_Sacs.RemoveAt(idx);
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.RetirerSacReseau(id);
            j.orPorte += sac.montant;
            int recu = Inventaire.AjouterTasse(j, sac.objets);
            if (recu != 0 && !EstLocal(j) && ReseauJeu.EnPartie) PartieReseau.Instance?.DonnerObjets((ulong)(joueurId - 1), recu, 0);
            if (sac.nom != j.nom) Deathless.Succes.ServiceSucces.SacAllie(joueurId);   // succès « Ce qui est à toi est à moi »
            P.Journal("Donjon : " + j.nom + " ramasse le sac de " + sac.nom + " (" + sac.montant + " or ; porté : " + j.orPorte + ")" + (sac.objets != 0 ? ", " + Inventaire.TexteTasse(sac.objets) : ""));
        }

        /// Ce poste demande le sac `id` pour son joueur (marché dessus) ; message local optimiste, comme le dépôt à la caisse.
        void DemanderSac(int id)
        {
            if (m_DemandeSac.TryGetValue(id, out float t) && Time.time < t) return;
            m_DemandeSac[id] = Time.time + 1f;
            var h = P != null ? P.HerosLocal : null;
            int idx = IndexSac(id);
            if (h == null || idx < 0) return;
            var sac = m_Sacs[idx];
            string objets = Inventaire.TexteTasse(sac.objets);
            Dire("Tu ramasses le sac de " + sac.nom + " : " + sac.montant + " or" + (objets.Length > 0 ? ", " + objets : ""), 5f);
            if (Partie.ClientReseau) PartieReseau.Instance?.DemanderSac(id);
            else RamasserSac(id, h.Id);
        }

        /// Index du sac `id` dans m_Sacs, ou -1 (boucle : pas de lambda allouée, appelé à chaque image).
        int IndexSac(int id)
        {
            for (int i = 0; i < m_Sacs.Count; i++) if (m_Sacs[i].id == id) return i;
            return -1;
        }

        /// Autorité, au crépuscule : les sacs encore au sol sont perdus (un seul événement réseau pour tous).
        void FermerSacs()
        {
            if (m_Sacs.Count == 0) return;
            int n = m_Sacs.Count, total = 0;
            foreach (var s in m_Sacs) total += s.montant;
            m_Sacs.Clear();
            PartieReseau.Instance?.ViderSacs();
            P?.Journal("Donjon : le portail se ferme, " + n + " sac(s) perdu(s) (" + total + " or)");
            if (AuDonjonLocal) Dire(n == 1 ? "Le sac oublié au donjon est perdu" : n + " sacs oubliés au donjon sont perdus", 5f);
        }

        /// Client : la liste des sacs suit celle de l'hôte (comme Pris suit ButinsPris).
        void SuivreSacsReseau(Unity.Netcode.NetworkList<SacReseau> distants)
        {
            // Parcours par index : le foreach de NetworkList passe par IEnumerator<T> et alloue (appelé à chaque image).
            int n = distants.Count;
            for (int i = m_Sacs.Count - 1; i >= 0; i--)
            {
                int id = m_Sacs[i].id;
                bool present = false;
                for (int k = 0; k < n; k++) if (distants[k].id == id) { present = true; break; }
                if (!present) m_Sacs.RemoveAt(i);
            }
            for (int k = 0; k < n; k++)
            {
                var s = distants[k];
                if (IndexSac(s.id) >= 0) continue;
                m_Sacs.Add(new Sac { id = s.id, position = s.position, montant = s.montant, nom = s.nom.ToString(), objets = s.objets });
            }
        }

        /// Tous les postes : le visuel (posé au sol, recalé sur le NavMesh) suit m_Sacs, comme les coffres suivent Pris.
        void SuivreSacsVisuels()
        {
            foreach (var s in m_Sacs)
                if (!m_VisuelsSacs.ContainsKey(s.id)) m_VisuelsSacs[s.id] = CreerVisuelSac(s);
            if (m_VisuelsSacs.Count == 0) return;
            List<int> disparus = null;
            foreach (var kv in m_VisuelsSacs)
                if (IndexSac(kv.Key) < 0) (disparus ??= new List<int>()).Add(kv.Key);
            if (disparus == null) return;
            foreach (int id in disparus)
            {
                if (m_VisuelsSacs.TryGetValue(id, out var go) && go != null)
                {
                    PieceOr.Jouer(go.transform.position + Vector3.up * 0.4f);
                    AudioBank.Jouer(SonsDuJeu.Or, go.transform.position, 0.6f);
                    Destroy(go);
                }
                m_VisuelsSacs.Remove(id);
            }
        }

        GameObject CreerVisuelSac(Sac s)
        {
            Vector3 p = s.position;
            if (NavMesh.SamplePosition(p + Vector3.up * 0.5f, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
            GameObject go = modeleSac != null ? Instantiate(modeleSac, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SacDonjon_" + s.id;
            go.transform.position = p;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c); // ramassage par distance (comme le tas d'or), pas de physique
            go.AddComponent<SacDonjon>();
            return go;
        }

        /// Retire les visuels de sacs sans toucher au réseau (nouveau donjon, ou nettoyage défensif).
        void ViderSacsLocal()
        {
            foreach (var go in m_VisuelsSacs.Values) if (go != null) Destroy(go);
            m_VisuelsSacs.Clear();
            m_Sacs.Clear();
        }

        // ================================================================== Passage des portails (joueur local)

        /// Invite du portail (touche Interagir) pour ce héros, ou null : « Entrer dans le donjon » au portail du village
        /// (le jour, portail ouvert), « Revenir au village » au portail de retour ; à `distancePortail` (3 m) au plus du
        /// centre, horizontalement. `distance` : pour choisir le point d'interaction le plus proche.
        public string InvitePortail(bool retour, Heros h, out float distance)
        {
            distance = float.MaxValue;
            if (h == null || h.Distant || h.EnTransit || !h.Vivant || m_Transit || P == null || !P.EnCours || (generateur == null && !Terrasses)) return null;
            Vector3 ph = h.transform.position;
            if (!retour)
            {
                var pv = PortailVillage;
                if (!PortailVillageOuvert || !pv.ouvert || AuDonjon(h) || Mathf.Abs(ph.y - pv.Center.y) > 3f) return null;
                // Fin du jour : le héros n'arrive au donjon que DureeTransitDepart après l'appui ; si le crépuscule tombe
                // entre-temps, RappelerTous ne le voit pas encore au donjon et il y restait la nuit sans être rappelé.
                if (P.Etat.phase == Phase.Jour && P.Etat.TempsRestant < DureeTransitDepart + 0.4f) return null;
                distance = Horizontal(ph, pv.Center);
                return distance <= B.distancePortail ? "Entrer dans le donjon" : null;
            }
            var ret = Pret ? RepereRetour : null;
            if (ret == null || !AuDonjon(h) || Mathf.Abs(ph.y - ret.transform.position.y) > 2f) return null;
            distance = Horizontal(ph, ret.transform.position);
            return distance <= B.distancePortail ? "Revenir au village" : null;
        }

        /// Touche Interagir au portail : passage (conditions revérifiées). Retour : l'or porté est versé à la caisse
        /// (l'hôte décide en réseau).
        public void Passer(bool retour, Heros h)
        {
            if (InvitePortail(retour, h, out _) == null) return;
            if (!retour)
            {
                StartCoroutine(Transit(h, PointArrivee, true, PortailVillage, PortailRetourVisuel, null, CentreRetourTerrasses));
                m_AlerteJouee = false;
                if (Terrasses && Partie.Exploration) AnnoncerGraine();
                return;
            }
            var p = P;
            int porte = p.JoueurLocal != null ? p.JoueurLocal.orPorte : 0;
            Deathless.Succes.ServiceSucces.PortailRetour();   // succès « Juste à temps » (moins de 3 s avant la fermeture)
            StartCoroutine(Transit(h, SortieVillage, false, PortailRetourVisuel, PortailVillage, CentreRetourTerrasses, null));
            if (Partie.ClientReseau) PartieReseau.Instance?.DeposerOr();
            else Deposer(h.Id);
            if (porte > 0) Dire(porte + " or versés à la caisse commune", 5f);
        }

        /// Passage du héros local (EnTransit : ni déplacement ni action) : le corps part en gemmes vers le centre du
        /// portail de départ (onde d'entrée, réaction de Nyxessa) ou sur place (rappel), immobile jusqu'à la fin de
        /// l'effet ; le héros saute alors à `destination`, les gemmes jaillissent du portail d'arrivée (onde de sortie)
        /// ou de devant lui et se reforment, puis le clip d'arrivée (Spawn_Air au donjon, Spawn_Ground au village et au
        /// rappel ; Heros.DeclencherPortail) se joue en entier avant de rendre le contrôle. Diffusé aux autres postes
        /// (effets 203 et 204, avec le code du portail) ; le clip lui-même passe par le NetworkAnimator, comme les emotes.
        IEnumerator Transit(Heros h, Vector3 destination, bool versDonjon, PortalVisual portailDepart, PortalVisual portailArrivee,
            Vector3? pointDepart = null, Vector3? pointArrivee = null)
        {
            m_Transit = true;
            h.EnTransit = true;
            m_HerosTransit = h;
            try
            {
                var gemmes = EffetsJeu.Gemmes;
                Bounds corps = EffetsJeu.Volume(h.gameObject);
                if (portailDepart != null) PortalTransit.Depart(corps, portailDepart, gemmes, DureeTransitDepart);
                else PortalTransit.Depart(corps, pointDepart ?? corps.center + Vector3.up * 0.3f, gemmes, DureeTransitDepart);
                h.Classe?.DiffuserTransit(ClasseHeros.EffetTransitDepart, h.transform.position, CodeDe(portailDepart));
                AudioBank.Jouer(SonsDuJeu.PortailPassage, h.transform.position + Vector3.up, 0.9f);
                Visible(h, false);
                // Immobile jusqu'à la fin de l'effet de départ (Quentin, 26/09/2026) : la téléportation n'arrive qu'une fois
                // les gemmes parties, jamais avant.
                yield return new WaitForSeconds(DureeTransitDepart);
                if (h == null) yield break;
                Vector3 avant = h.transform.position;
                h.Teleporter(destination + Vector3.up * 0.05f);
                // Regard à l'arrivée : vers l'intérieur du donjon (orientation de l'arrivée), ou vers Nyxessa au village.
                Vector3 v = versDonjon ? RepereArrivee.transform.forward
                    : PiedEscalier(out _, out var dehors) ? dehors   // nouvelle carte : dos à la grotte
                    : (P != null && P.nyxessa != null ? P.nyxessa.transform.position : destination + Vector3.forward) - destination;
                v.y = 0f;
                if (v.sqrMagnitude > 0.01f)
                {
                    h.transform.rotation = Quaternion.LookRotation(v);
                    if (P != null && P.cameraJeu != null && P.cameraJeu.cible == h.transform) P.cameraJeu.lacet = h.transform.eulerAngles.y;
                }
                // Réseau : saut de position sans interpolation chez les autres.
                var nt = h.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (nt != null && nt.IsSpawned && nt.IsOwner) nt.Teleport(h.transform.position, h.transform.rotation, h.transform.localScale);
                Bounds arrivee = corps; arrivee.center += h.transform.position - avant;
                if (portailArrivee != null) PortalTransit.Arrive(arrivee, portailArrivee, gemmes, DureeTransitArriveeGemmes);
                else PortalTransit.Arrive(arrivee, pointArrivee ?? arrivee.center + h.transform.forward * 1.2f, gemmes, DureeTransitArriveeGemmes);
                AudioBank.Jouer(SonsDuJeu.PortailArrivee, arrivee.center, 0.9f);
                h.Classe?.DiffuserTransit(ClasseHeros.EffetTransitArrivee, h.transform.position, CodeDe(portailArrivee));
                yield return new WaitForSeconds(DureeTransitArriveeGemmes);
                if (h == null) yield break;
                // Corps reformé : le clip d'arrivée prend le relais dès sa première image (Spawn_Air commence en l'air),
                // joué en entier, toujours sans contrôle.
                Visible(h, true);
                h.DeclencherPortail(versDonjon);
                // Réception (lot 2, § 8.2 : chute du ciel / sortie du sol), placée vers 1 s dans le clip de 1,3 s faute
                // de mesure exacte de l'instant de contact (hypothèse à confirmer en jeu, cahier des charges son § 8.2).
                float avantReception = Mathf.Min(1.0f, PortailAnim.DureeClip);
                yield return new WaitForSeconds(avantReception);
                if (h != null) AudioBank.Jouer(versDonjon ? SonsDuJeu.PortailChuteCiel : SonsDuJeu.PortailSortieSol, h.transform.position, 0.9f);
                yield return new WaitForSeconds(PortailAnim.DureeClip - avantReception);
            }
            finally { FinTransit(h); }
        }

        /// Fin du passage, même interrompu (exception, héros détruit, coroutine arrêtée) : le héros redevient visible
        /// et reprend le contrôle, et les portails se rouvrent. Sans cela, un passage coupé en route bloquait le héros
        /// (EnTransit) et tous les portails (m_Transit) jusqu'au rechargement de la scène.
        void FinTransit(Heros h)
        {
            if (h != null)
            {
                Visible(h, true);
                h.EnTransit = false;
            }
            m_Transit = false;
            m_HerosTransit = null;
        }

        /// Coroutine arrêtée avec le composant (désactivation) : son bloc finally ne s'exécute pas.
        void OnDisable()
        {
            if (m_Transit) FinTransit(m_HerosTransit);
        }

        static void Visible(Heros h, bool oui)
        {
            foreach (var r in h.GetComponentsInChildren<Renderer>(true)) r.enabled = oui;
        }

        /// Autres postes : passage d'un portail par la marionnette d'un joueur (dissolution vers le portail de départ,
        /// puis arrivée depuis le portail d'arrivée ; `portail` : code de DonjonJeu, 0 = sur place). Le clip d'arrivée
        /// (Spawn_Air / Spawn_Ground) n'est pas déclenché ici : il arrive du propriétaire par le NetworkAnimator, comme
        /// les emotes (Heros.DeclencherPortail) ; on ne fait ici que suivre les gemmes et rendre le corps visible au
        /// même instant que chez le propriétaire.
        public static void TransitDistant(Heros h, Vector3 position, bool arrivee, int portail = AucunPortail)
        {
            if (h == null) return;
            var gemmes = EffetsJeu.Gemmes;
            Bounds corps = new Bounds(position + Vector3.up, new Vector3(0.8f, 1.9f, 0.8f));
            var pv = Instance != null ? Instance.PortailDe(portail) : null;
            if (!arrivee)
            {
                AudioBank.Jouer(SonsDuJeu.PortailPassage, position + Vector3.up, 0.9f);
                if (pv != null) PortalTransit.Depart(corps, pv, gemmes, DureeTransitDepart);
                else PortalTransit.Depart(corps, corps.center + Vector3.up * 0.3f, gemmes, DureeTransitDepart);
                Visible(h, false);
            }
            else
            {
                if (pv != null) PortalTransit.Arrive(corps, pv, gemmes, DureeTransitArriveeGemmes);
                else PortalTransit.Arrive(corps, corps.center + h.transform.forward * 1.2f, gemmes, DureeTransitArriveeGemmes);
                AudioBank.Jouer(SonsDuJeu.PortailArrivee, corps.center, 0.9f);
                if (Instance != null) Instance.StartCoroutine(Instance.Montrer(h, DureeTransitArriveeGemmes));
                else Visible(h, true);
            }
        }

        IEnumerator Montrer(Heros h, float apres) { yield return new WaitForSeconds(apres); if (h != null) Visible(h, true); }

        /// Touche Interagir sur les deux portails (posée une fois) ; bourdonnement du portail de retour, comme au village.
        void AssurerPortails()
        {
            var pv = PortailVillage;
            if (m_PassageVillage == null && pv != null)
            {
                m_PassageVillage = pv.GetComponent<PassagePortail>();
                if (m_PassageVillage == null) m_PassageVillage = pv.gameObject.AddComponent<PassagePortail>();
                m_PassageVillage.retour = false;
            }
            var ret = RepereRetour;
            if (m_PassageRetour == null && ret != null)
            {
                m_PassageRetour = ret.GetComponent<PassagePortail>();
                if (m_PassageRetour == null) m_PassageRetour = ret.gameObject.AddComponent<PassagePortail>();
                m_PassageRetour.retour = true;
            }
            if (Terrasses && ret != null && m_BourdonTerrasses != ret.transform)
            {
                m_BourdonTerrasses = ret.transform;
                AudioBank.Boucle(SonsDuJeu.PortailBourdon, ret.transform, 0.4f);
            }
            var visuel = PortailRetourVisuel;
            if (visuel != null && m_BourdonSur != visuel)
            {
                m_BourdonSur = visuel;
                AudioBank.Boucle(SonsDuJeu.PortailBourdon, visuel.transform, 0.4f);
            }
        }

        // ================================================================== Chaque image

        void Update()
        {
            var p = P;
            if (p == null || (generateur == null && !Terrasses)) return;
            // Client : le donjon et ses butins suivent l'hôte.
            if (Partie.ClientReseau)
            {
                var r = PartieReseau.Instance;
                if (r != null)
                {
                    if (r.GraineDonjon.Value != 0 && r.GraineDonjon.Value != GraineCourante) Construire(r.GraineDonjon.Value, r.EssaiDonjon.Value);
                    if (r.GraineDonjon.Value == GraineCourante) Pris = r.ButinsPris.Value;
                    SuivreSacsReseau(r.Sacs);
                }
            }
            // Autorité : premier jour déjà commencé avant que ce composant écoute les phases.
            else if (ReseauJeu.Autorite && p.EnCours && p.Etat.phase == Phase.Jour && GraineCourante == 0) NouveauDonjon();
            if (Pret) { SuivreButins(); SuivreSacsVisuels(); }

            // Autorité : mort au donjon avec de l'or porté → un sac tombe (perdu ailleurs, comme avant) ; vote « prêt »
            // réévalué au retour de l'équipe : tout de suite après une mort traitée ici, sinon toutes les 0,2 s (le vote
            // lui-même réévalue déjà dans Partie.BasculerPret ; plus de parcours des héros à chaque image).
            if (ReseauJeu.Autorite && p.EnCours)
            {
                bool mort = false;
                foreach (var j in p.Etat.joueurs)
                {
                    if (!j.mort) continue;
                    var hj = p.HerosDe(j.id);
                    bool auDonjon = AuDonjon(hj);
                    // Clés et crochets : dans le sac au donjon ; ailleurs, le mort les garde (seul l'or se perd).
                    if (j.orPorte <= 0 && !(auDonjon && Inventaire.ABut(j))) continue;
                    if (auDonjon) CreerSac(j, hj.transform.position); else Perdre(j, out _, out _, "mort au donjon");
                    mort = true;
                }
                if (p.Etat.phase == Phase.Jour && (mort || Time.time >= m_ProchaineEvalPrets))
                {
                    m_ProchaineEvalPrets = Time.time + 0.2f;
                    p.EvaluerPrets();
                }
            }

            // Portails : touche Interagir (PassagePortail), plus de passage en marchant dedans (Quentin, 26/09/2026).
            AssurerPortails();
            // Aperçu de la nouvelle carte : F8 / Maj + F8 changent la graine du donjon en terrasses (outil de dev).
            if (Terrasses && Partie.Exploration && Pret) ToucheGraine();
            if (Partie.Exploration) ToucheApercu();   // F9 jour / nuit, F10 sans mob / avec mob (hors donjon aussi)

            var h = p.HerosLocal;
            if (h == null) return;
            AssurerCapteurs(h);
            if (m_Transit || !h.Vivant || !p.EnCours) return;
            if (!AuDonjon(h) || !Pret) return;
            // Tas d'or : on passe dessus.
            for (int i = 0; i < Butins.Count; i++)
            {
                var r = Butins[i];
                if (r == null || r.butin != TypeButin.TasOr || ButinPris(i) || !ButinActif(i)) continue;
                if (Horizontal(h.transform.position, r.transform.position) < B.rayonTasOr && Mathf.Abs(h.transform.position.y - r.transform.position.y) < 1.5f) DemanderButin(i);
            }
            // Sacs des joueurs morts au donjon : on passe dessus aussi (à l'envers : DemanderSac peut en retirer un).
            for (int i = m_Sacs.Count - 1; i >= 0; i--)
            {
                var s = m_Sacs[i];
                if (Horizontal(h.transform.position, s.position) < B.rayonTasOr && Mathf.Abs(h.transform.position.y - s.position.y) < 1.5f) DemanderSac(s.id);
            }
            // Alerte avant le rappel (le son de l'alerte de la nuit est joué pour tous ; ici, celui de Nyxessa en plus).
            if (AvantRappel >= 0f && !m_AlerteJouee) { m_AlerteJouee = true; AudioBank.Jouer2D(SonsDuJeu.NyxessaAlerte, 0.8f); }
        }

        /// Capteurs du masquage des étages sur le héros local et sa caméra (jamais sur les autres joueurs).
        void AssurerCapteurs(Heros h)
        {
            if (m_HerosCapteur == h) return;
            m_HerosCapteur = h;
            Capteur(h.transform, DonjonCapteur.Role.Heros, h.transform);
            var cam = P.cameraJeu != null ? P.cameraJeu.transform : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null) Capteur(cam, DonjonCapteur.Role.Camera, h.transform);
        }

        static void Capteur(Transform parent, DonjonCapteur.Role role, Transform reference)
        {
            if (parent.GetComponentInChildren<DonjonCapteur>() != null) return;
            var go = new GameObject("CapteurDonjon_" + role);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = role == DonjonCapteur.Role.Heros ? Vector3.up * 1f : Vector3.zero;
            go.AddComponent<Rigidbody>();
            go.AddComponent<SphereCollider>();
            var c = go.AddComponent<DonjonCapteur>();
            c.role = role;
            c.reference = reference;
        }

        /// Camera de la caméra de jeu, cherchée une fois par CameraEpaule (sinon Camera.main).
        Camera CameraJeu
        {
            get
            {
                var ce = P != null ? P.cameraJeu : null;
                if (ce == null) return Camera.main;
                if (m_CameraJeuDe != ce || m_CameraJeu == null) { m_CameraJeuDe = ce; m_CameraJeu = ce.GetComponent<Camera>(); }
                return m_CameraJeu;
            }
        }

        /// Ambiance au donjon pour le joueur local : sombre, aux torches, sans ciel (après le cycle jour / nuit).
        void LateUpdate()
        {
            var h = P != null ? P.HerosLocal : null;
            var cam = CameraJeu;
            bool dedans = h != null && (AuDonjon(h) || (cam != null && Contient(cam.transform.position)));
            CameraEpaule.Enceinte = h != null && AuDonjon(h) ? (Terrasses ? EnceinteTerrasses(h.transform.position) : EnceinteCamera) : (Bounds?)null;
            // Donjon en terrasses : découpe autour du héros, mais la caméra ne traverse ni les murs ni les terrasses
            // (recul contre les collisions) ; l'ancien donjon garde sa caméra qui passe à travers les murs.
            CameraEpaule.DecoupeSansTraverser = Terrasses;
            if (dedans)
            {
                if (!m_AmbianceActive && cam != null) { m_CamAmbiance = cam; m_FondCamera = cam.clearFlags; m_FondCouleur = cam.backgroundColor; }
                m_AmbianceActive = true;
                RenderSettings.ambientSkyColor = ambiance;
                RenderSettings.ambientEquatorColor = ambiance * 0.85f;
                RenderSettings.ambientGroundColor = ambiance * 0.6f;
                RenderSettings.fog = true;
                RenderSettings.fogColor = brouillard;
                RenderSettings.fogStartDistance = brouillardDebut;
                RenderSettings.fogEndDistance = brouillardFin;
                if (m_Soleil != null) m_Soleil.intensity = 0.08f;
                if (m_CamAmbiance != null) { m_CamAmbiance.clearFlags = CameraClearFlags.SolidColor; m_CamAmbiance.backgroundColor = brouillard; }
            }
            else if (m_AmbianceActive)
            {
                m_AmbianceActive = false;
                if (m_CamAmbiance != null) { m_CamAmbiance.clearFlags = m_FondCamera; m_CamAmbiance.backgroundColor = m_FondCouleur; }
            }
        }

        // ================================================================== IEtatDonjon (HUD)

        public bool AuDonjonLocal => P != null && AuDonjon(P.HerosLocal);
        bool IEtatDonjon.AuDonjon => AuDonjonLocal;
        public int OrPorte => P != null && P.JoueurLocal != null ? P.JoueurLocal.orPorte : 0;

        public float AvantRappel
        {
            get
            {
                if (P == null || P.Etat.phase != Phase.Jour) return -1f;
                float reste = P.Etat.TempsRestant;
                float alerte = B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle);
                return reste <= alerte ? Mathf.Max(0f, reste) : -1f;
            }
        }

        public string Message => Time.time < m_MessageJusqua ? m_Message : "";
    }
}
