using System.Collections;
using System.Collections.Generic;
using Deathless.Accessoires;
using Deathless.Donjon;
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
    ///   le construit, pose les gardiens ; la graine part aux clients (PartieReseau.GraineDonjon), qui construisent le
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
        static readonly Bounds Emprise = new Bounds(Origine + new Vector3(30f, 5f, 24f), new Vector3(66f, 34f, 54f));

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
        /// Butins déjà pris (un bit par emplacement).
        public int Pris { get; private set; }
        public bool Pret => GraineCourante != 0 && generateur != null && generateur.Arrivee != null;

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;

        /// Durées du passage (Wiki : portail.md, {décidé} 26/09/2026). Départ : l'effet en gemmes (PortalTransit.Depart)
        /// se joue en entier, immobile, avant la téléportation. Arrivée : les gemmes se reforment (PortalTransit.Arrive),
        /// puis le clip d'arrivée prend le relais dès sa première image (PortailAnim.DureeClip, Heros.DeclencherPortail),
        /// en entier, avant de rendre le contrôle.
        const float DureeTransitDepart = 1.1f, DureeTransitArriveeGemmes = 0.5f;

        readonly List<Squelette> m_Gardiens = new List<Squelette>();
        readonly float[] m_Demande = new float[DonjonPlan.NbButins];
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

        sealed class Coffre
        {
            public Transform couvercle;
            public Quaternion ferme;
            public CadenasOuverture cadenas;
        }
        readonly Dictionary<GameObject, Coffre> m_Coffres = new Dictionary<GameObject, Coffre>();

        /// Sac d'un joueur mort au donjon (26/09/2026) : voir les méthodes « Sacs des joueurs morts » plus bas.
        struct Sac { public int id; public Vector3 position; public int montant; public string nom; }
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
        }

        void OnDestroy()
        {
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

        public static bool Contient(Vector3 p) => Emprise.Contains(p);
        public static bool AuDonjon(Heros h) => h != null && Contient(h.transform.position);

        /// Un joueur au moins est au donjon (vote « prêt » bloqué).
        public static bool QuelquunAuDonjon
        {
            get
            {
                var p = Partie.Instance;
                if (p == null) return false;
                foreach (var h in p.TousLesHeros) if (AuDonjon(h)) return true;
                return false;
            }
        }

        PortalVisual PortailVillage
        {
            get
            {
                if (m_VueCycle == null) m_VueCycle = FindAnyObjectByType<VueCycle>();
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
                Transform a = generateur.Arrivee.transform;
                Vector3 p = a.position;
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

        /// Tous les postes : construit le donjon de cette graine (butins remis en place).
        void Construire(int graine)
        {
            if (generateur == null || graine == GraineCourante) return;
            RetirerGardiens();
            ViderSacsLocal();
            if (ReseauJeu.Autorite) PartieReseau.Instance?.ViderSacs();
            float t0 = Time.realtimeSinceStartup;
            bool ok = generateur.Generer(graine);
            GraineCourante = graine;
            Pris = 0; m_PrisVus = 0;
            for (int i = 0; i < m_Demande.Length; i++) m_Demande[i] = 0f;
            PreparerButins();
            P?.Journal("Donjon : graine " + graine + (ok ? "" : " (hors consigne)") + ", chemin critique " + (generateur.Plan.cheminCritiqueDm / 10f).ToString("F0") + " m, construit en "
                + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("F0") + " ms");
        }

        void PreparerButins()
        {
            for (int i = 0; i < generateur.Butins.Length; i++)
            {
                var r = generateur.Butins[i];
                if (r == null) continue;
                if (r.visuel != null) r.visuel.SetActive(true);
                var coffre = r.butin != TypeButin.TasOr ? CoffreDe(r.visuel, r.butin == TypeButin.GrandCoffre) : null;
                if (coffre != null)
                {
                    if (coffre.couvercle != null) coffre.couvercle.localRotation = coffre.ferme;
                    if (coffre.cadenas != null) { coffre.cadenas.gameObject.SetActive(true); coffre.cadenas.Reinitialiser(); }
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

        public int MontantButin(int index) => generateur != null && index >= 0 && index < generateur.Butins.Length && generateur.Butins[index] != null ? Montant(generateur.Butins[index].butin) : 0;
        public bool ButinPris(int index) => (Pris & (1 << index)) != 0;

        // ================================================================== Gardiens

        void PoserGardiens()
        {
            var dv = DirecteurVagues.Instance;
            if (dv == null || !Pret) return;
            var b = B;
            // Butins du plus riche au plus modeste ; un gardien par butin, puis un deuxième sur les plus riches.
            var ordre = new List<int>();
            for (int i = 0; i < generateur.Butins.Length; i++) if (generateur.Butins[i] != null) ordre.Add(i);
            ordre.Sort((x, y) => ((int)generateur.Butins[x].butin).CompareTo((int)generateur.Butins[y].butin));
            var pris = new HashSet<DonjonRepere>();
            int nb = Mathf.Max(0, b.gardiensDonjon), guerriers = Mathf.RoundToInt(nb * b.partGuerriersDonjon);
            for (int k = 0; k < nb && ordre.Count > 0; k++)
            {
                var butin = generateur.Butins[ordre[k % ordre.Count]];
                DonjonRepere meilleur = null; float dmin = float.MaxValue;
                foreach (var a in generateur.Apparitions)
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
            if (!Pret || index < 0 || index >= generateur.Butins.Length || ButinPris(index) || P == null) return;
            var r = generateur.Butins[index];
            var h = P.HerosDe(joueurId);
            var j = P.Joueur(joueurId);
            if (r == null || h == null || j == null || !h.Vivant) return;
            if (Vector3.Distance(h.transform.position, r.transform.position) > B.distanceCoffre + 1.5f) return;
            Pris |= 1 << index;
            int or = Montant(r.butin);
            j.orPorte += or;
            P.Journal("Donjon : " + j.nom + " prend " + (r.butin == TypeButin.GrandCoffre ? "le grand coffre" : r.butin == TypeButin.Coffre ? "un coffre" : "un tas d'or") + " (" + or + " or ; porté : " + j.orPorte + ")");
        }

        /// Tous les postes : les butins pris depuis la dernière image s'ouvrent (coffre) ou disparaissent (tas d'or).
        void SuivreButins()
        {
            int nouveaux = Pris & ~m_PrisVus;
            if (nouveaux == 0) return;
            m_PrisVus |= nouveaux;
            for (int i = 0; i < generateur.Butins.Length; i++)
                if ((nouveaux & (1 << i)) != 0) StartCoroutine(Ouvrir(i));
        }

        IEnumerator Ouvrir(int i)
        {
            var r = generateur.Butins[i];
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
            for (int k = 0; k < (r.butin == TypeButin.GrandCoffre ? 4 : 2); k++) { PieceOr.Jouer(p + Random.insideUnitSphere * 0.3f); yield return new WaitForSeconds(0.08f); }
            AudioBank.Jouer(SonsDuJeu.Or, p, 0.7f);
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

        // ================================================================== Sacs des joueurs morts au donjon

        /// Autorité, à la mort d'un joueur au donjon (Update) : un sac tombe à `position` avec tout l'or qu'il portait.
        /// N'importe quel joueur peut le ramasser tant que le donjon est ouvert (Wiki : deroule.md, Mort au donjon).
        void CreerSac(EtatJoueur j, Vector3 position)
        {
            if (j == null || j.orPorte <= 0) return;
            int id = ++m_ProchainSacId;
            int montant = j.orPorte;
            string nom = j.nom;
            j.orPorte = 0;
            m_Sacs.Add(new Sac { id = id, position = position, montant = montant, nom = nom });
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.CreerSacReseau(id, position, montant, nom);
            P?.Journal("Donjon : " + nom + " meurt au donjon, un sac tombe (" + montant + " or)");
        }

        /// Autorité : le sac `id` va au joueur `joueurId`, s'il est encore là et que le joueur est à côté (marché dessus).
        public void RamasserSac(int id, int joueurId)
        {
            if (P == null) return;
            int idx = m_Sacs.FindIndex(s => s.id == id);
            if (idx < 0) return;
            var sac = m_Sacs[idx];
            var h = P.HerosDe(joueurId);
            var j = P.Joueur(joueurId);
            if (h == null || j == null || !h.Vivant || Horizontal(h.transform.position, sac.position) > B.rayonTasOr + 1f) return;
            m_Sacs.RemoveAt(idx);
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.RetirerSacReseau(id);
            j.orPorte += sac.montant;
            P.Journal("Donjon : " + j.nom + " ramasse le sac de " + sac.nom + " (" + sac.montant + " or ; porté : " + j.orPorte + ")");
        }

        /// Ce poste demande le sac `id` pour son joueur (marché dessus) ; message local optimiste, comme le dépôt à la caisse.
        void DemanderSac(int id)
        {
            if (m_DemandeSac.TryGetValue(id, out float t) && Time.time < t) return;
            m_DemandeSac[id] = Time.time + 1f;
            var h = P != null ? P.HerosLocal : null;
            int idx = m_Sacs.FindIndex(s => s.id == id);
            if (h == null || idx < 0) return;
            var sac = m_Sacs[idx];
            Dire("Tu ramasses le sac de " + sac.nom + " : " + sac.montant + " or", 5f);
            if (Partie.ClientReseau) PartieReseau.Instance?.DemanderSac(id);
            else RamasserSac(id, h.Id);
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
            for (int i = m_Sacs.Count - 1; i >= 0; i--)
            {
                int id = m_Sacs[i].id;
                bool present = false;
                foreach (var s in distants) if (s.id == id) { present = true; break; }
                if (!present) m_Sacs.RemoveAt(i);
            }
            foreach (var s in distants)
            {
                if (m_Sacs.Exists(x => x.id == s.id)) continue;
                m_Sacs.Add(new Sac { id = s.id, position = s.position, montant = s.montant, nom = s.nom.ToString() });
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
                if (!m_Sacs.Exists(x => x.id == kv.Key)) (disparus ??= new List<int>()).Add(kv.Key);
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
            if (h == null || h.Distant || h.EnTransit || !h.Vivant || m_Transit || P == null || !P.EnCours || generateur == null) return null;
            Vector3 ph = h.transform.position;
            if (!retour)
            {
                var pv = PortailVillage;
                if (!PortailVillageOuvert || !pv.ouvert || AuDonjon(h) || Mathf.Abs(ph.y - pv.Center.y) > 3f) return null;
                distance = Horizontal(ph, pv.Center);
                return distance <= B.distancePortail ? "Entrer dans le donjon" : null;
            }
            var ret = Pret ? generateur.PortailRetour : null;
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
                StartCoroutine(Transit(h, PointArrivee, true, PortailVillage, PortailRetourVisuel));
                m_AlerteJouee = false;
                return;
            }
            var p = P;
            int porte = p.JoueurLocal != null ? p.JoueurLocal.orPorte : 0;
            StartCoroutine(Transit(h, SortieVillage, false, PortailRetourVisuel, PortailVillage));
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
        IEnumerator Transit(Heros h, Vector3 destination, bool versDonjon, PortalVisual portailDepart, PortalVisual portailArrivee)
        {
            m_Transit = true;
            h.EnTransit = true;
            m_HerosTransit = h;
            try
            {
                var gemmes = EffetsJeu.Gemmes;
                Bounds corps = EffetsJeu.Volume(h.gameObject);
                if (portailDepart != null) PortalTransit.Depart(corps, portailDepart, gemmes, DureeTransitDepart);
                else PortalTransit.Depart(corps, corps.center + Vector3.up * 0.3f, gemmes, DureeTransitDepart);
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
                Vector3 v = versDonjon ? generateur.Arrivee.transform.forward
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
                else PortalTransit.Arrive(arrivee, arrivee.center + h.transform.forward * 1.2f, gemmes, DureeTransitArriveeGemmes);
                h.Classe?.DiffuserTransit(ClasseHeros.EffetTransitArrivee, h.transform.position, CodeDe(portailArrivee));
                yield return new WaitForSeconds(DureeTransitArriveeGemmes);
                if (h == null) yield break;
                // Corps reformé : le clip d'arrivée prend le relais dès sa première image (Spawn_Air commence en l'air),
                // joué en entier, toujours sans contrôle.
                Visible(h, true);
                h.DeclencherPortail(versDonjon);
                yield return new WaitForSeconds(PortailAnim.DureeClip);
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
            var ret = generateur != null ? generateur.PortailRetour : null;
            if (m_PassageRetour == null && ret != null)
            {
                m_PassageRetour = ret.GetComponent<PassagePortail>();
                if (m_PassageRetour == null) m_PassageRetour = ret.gameObject.AddComponent<PassagePortail>();
                m_PassageRetour.retour = true;
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
            if (p == null || generateur == null) return;
            // Client : le donjon et ses butins suivent l'hôte.
            if (Partie.ClientReseau)
            {
                var r = PartieReseau.Instance;
                if (r != null)
                {
                    if (r.GraineDonjon.Value != 0 && r.GraineDonjon.Value != GraineCourante) Construire(r.GraineDonjon.Value);
                    if (r.GraineDonjon.Value == GraineCourante) Pris = r.ButinsPris.Value;
                    SuivreSacsReseau(r.Sacs);
                }
            }
            // Autorité : premier jour déjà commencé avant que ce composant écoute les phases.
            else if (ReseauJeu.Autorite && p.EnCours && p.Etat.phase == Phase.Jour && GraineCourante == 0) NouveauDonjon();
            if (Pret) { SuivreButins(); SuivreSacsVisuels(); }

            // Autorité : mort au donjon avec de l'or porté → un sac tombe (perdu ailleurs, comme avant) ; vote « prêt »
            // réévalué au retour de l'équipe.
            if (ReseauJeu.Autorite && p.EnCours)
            {
                foreach (var j in p.Etat.joueurs)
                {
                    if (!j.mort || j.orPorte <= 0) continue;
                    var hj = p.HerosDe(j.id);
                    if (AuDonjon(hj)) CreerSac(j, hj.transform.position); else Perdre(j, out _, out _, "mort au donjon");
                }
                if (p.Etat.phase == Phase.Jour) p.EvaluerPrets();
            }

            // Portails : touche Interagir (PassagePortail), plus de passage en marchant dedans (Quentin, 26/09/2026).
            AssurerPortails();

            var h = p.HerosLocal;
            if (h == null) return;
            AssurerCapteurs(h);
            if (m_Transit || !h.Vivant || !p.EnCours) return;
            if (!AuDonjon(h) || !Pret) return;
            // Tas d'or : on passe dessus.
            for (int i = 0; i < generateur.Butins.Length; i++)
            {
                var r = generateur.Butins[i];
                if (r == null || r.butin != TypeButin.TasOr || ButinPris(i)) continue;
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

        /// Ambiance au donjon pour le joueur local : sombre, aux torches, sans ciel (après le cycle jour / nuit).
        void LateUpdate()
        {
            var h = P != null ? P.HerosLocal : null;
            var cam = P != null && P.cameraJeu != null ? P.cameraJeu.GetComponent<Camera>() : Camera.main;
            bool dedans = h != null && (AuDonjon(h) || (cam != null && Contient(cam.transform.position)));
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
