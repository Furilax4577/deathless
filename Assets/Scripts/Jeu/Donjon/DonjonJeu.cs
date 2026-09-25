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
    /// - **Portails** : le jour, le portail du village (près de Nyxessa) mène à l'arrivée ; le portail de retour ramène
    ///   devant le portail du village. Passage avec l'effet de téléportation (PortalTransit), vu par tous.
    /// - **Butin, de l'or seulement** : coffres (touche Interagir ; cadenas qui s'ouvre, couvercle) et tas d'or (on
    ///   passe dessus). L'or est porté par le joueur (HUD) et versé à la caisse au retour par le portail. L'autorité
    ///   décide (un butin n'est pris qu'une fois).
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
        [Tooltip("Cadenas du grand coffre (Assets/Art/Cadenas/Prefabs).")] public GameObject cadenasGrandCoffre;
        [Tooltip("Cadenas des coffres.")] public GameObject cadenasCoffre;

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

        readonly List<Squelette> m_Gardiens = new List<Squelette>();
        readonly float[] m_Demande = new float[DonjonPlan.NbButins];
        int m_PrisVus;
        bool m_Transit;
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

        sealed class Coffre
        {
            public Transform couvercle;
            public Quaternion ferme;
            public CadenasOuverture cadenas;
        }
        readonly Dictionary<GameObject, Coffre> m_Coffres = new Dictionary<GameObject, Coffre>();

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
            else if (apres == Phase.Crepuscule) { RappelerTous(); RetirerGardiens(); }
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

        /// Couvercle et cadenas d'un coffre (posé une fois, les coffres sont réutilisés d'un donjon à l'autre).
        Coffre CoffreDe(GameObject visuel, bool grand)
        {
            if (visuel == null) return null;
            if (m_Coffres.TryGetValue(visuel, out var c)) return c;
            c = new Coffre();
            foreach (var t in visuel.GetComponentsInChildren<Transform>(true))
                if (t.name.EndsWith("_lid")) { c.couvercle = t; c.ferme = t.localRotation; break; }
            var prefab = grand ? cadenasGrandCoffre : cadenasCoffre;
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
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
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
            if (c != null && c.cadenas != null && c.cadenas.gameObject.activeInHierarchy)
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
            StartCoroutine(Transit(h, dest, false, null));
            Dire(perdu > 0 || garde > 0 ? "Rappelé par Nyxessa : " + garde + " or gardés, " + perdu + " perdus" : "Rappelé par Nyxessa", 6f);
            AudioBank.Jouer2D(SonsDuJeu.NyxessaRappel, 0.9f);
        }

        void Dire(string texte, float duree) { m_Message = texte; m_MessageJusqua = Time.time + duree; }

        // ================================================================== Passage des portails (joueur local)

        IEnumerator Transit(Heros h, Vector3 destination, bool versDonjon, PortalVisual portailArrivee)
        {
            m_Transit = true;
            h.EnTransit = true;
            var gemmes = EffetsJeu.Gemmes;
            Bounds corps = EffetsJeu.Volume(h.gameObject);
            var pv = PortailVillage;
            // Départ : par le portail du village (onde, réaction de Nyxessa), sinon sur place (retour, rappel).
            if (versDonjon && pv != null && Horizontal(h.transform.position, pv.Center) < 4f) PortalTransit.Depart(corps, pv, gemmes, 1.1f);
            else PortalTransit.Depart(corps, corps.center + Vector3.up * 0.3f, gemmes, 1.1f);
            h.Classe?.DiffuserTransit(ClasseHeros.EffetTransitDepart, h.transform.position);
            AudioBank.Jouer(SonsDuJeu.PortailPassage, h.transform.position + Vector3.up, 0.9f);
            Visible(h, false);
            yield return new WaitForSeconds(0.55f);
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
            if (portailArrivee != null) PortalTransit.Arrive(arrivee, portailArrivee, gemmes, 1.0f);
            else PortalTransit.Arrive(arrivee, arrivee.center + h.transform.forward * 1.2f, gemmes, 1.0f);
            h.Classe?.DiffuserTransit(ClasseHeros.EffetTransitArrivee, h.transform.position);
            yield return new WaitForSeconds(0.75f);
            Visible(h, true);
            h.EnTransit = false;
            m_Transit = false;
        }

        static void Visible(Heros h, bool oui)
        {
            foreach (var r in h.GetComponentsInChildren<Renderer>(true)) r.enabled = oui;
        }

        /// Autres postes : passage d'un portail par la marionnette d'un joueur (dissolution, puis arrivée).
        public static void TransitDistant(Heros h, Vector3 position, bool arrivee)
        {
            if (h == null) return;
            var gemmes = EffetsJeu.Gemmes;
            Bounds corps = new Bounds(position + Vector3.up, new Vector3(0.8f, 1.9f, 0.8f));
            if (!arrivee) AudioBank.Jouer(SonsDuJeu.PortailPassage, position + Vector3.up, 0.9f);
            if (!arrivee)
            {
                var pv = Instance != null ? Instance.PortailVillage : null;
                if (pv != null && Horizontal(position, pv.Center) < 4f) PortalTransit.Depart(corps, pv, gemmes, 1.1f);
                else PortalTransit.Depart(corps, corps.center + Vector3.up * 0.3f, gemmes, 1.1f);
                Visible(h, false);
            }
            else
            {
                PortalTransit.Arrive(corps, corps.center + Vector3.forward * 1.2f, gemmes, 1.0f);
                if (Instance != null) Instance.StartCoroutine(Instance.Montrer(h, 0.75f));
                else Visible(h, true);
            }
        }

        IEnumerator Montrer(Heros h, float apres) { yield return new WaitForSeconds(apres); if (h != null) Visible(h, true); }

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
                }
            }
            // Autorité : premier jour déjà commencé avant que ce composant écoute les phases.
            else if (ReseauJeu.Autorite && p.EnCours && p.Etat.phase == Phase.Jour && GraineCourante == 0) NouveauDonjon();
            if (Pret) SuivreButins();

            // Autorité : or porté d'un joueur mort au donjon ; vote « prêt » réévalué au retour de l'équipe.
            if (ReseauJeu.Autorite && p.EnCours)
            {
                foreach (var j in p.Etat.joueurs)
                    if (j.mort && j.orPorte > 0) Perdre(j, out _, out _, "mort au donjon");
                if (p.Etat.phase == Phase.Jour) p.EvaluerPrets();
            }

            var h = p.HerosLocal;
            if (h == null) return;
            AssurerCapteurs(h);
            if (m_Transit || !h.Vivant || !p.EnCours) return;
            bool dedans = AuDonjon(h);
            if (!dedans)
            {
                // Portail du village : le jour, on entre en passant dedans.
                var pv = PortailVillage;
                if (PortailVillageOuvert && pv.ouvert && Horizontal(h.transform.position, pv.Center) < B.rayonPortail && Mathf.Abs(h.transform.position.y - pv.Center.y) < 3f)
                {
                    StartCoroutine(Transit(h, PointArrivee, true, null));
                    m_AlerteJouee = false;
                }
                return;
            }
            if (!Pret) return;
            // Portail de retour.
            var ret = generateur.PortailRetour;
            if (ret != null && Horizontal(h.transform.position, ret.transform.position) < B.rayonPortail && Mathf.Abs(h.transform.position.y - ret.transform.position.y) < 2f)
            {
                int porte = p.JoueurLocal != null ? p.JoueurLocal.orPorte : 0;
                StartCoroutine(Transit(h, SortieVillage, false, PortailVillage));
                if (Partie.ClientReseau) PartieReseau.Instance?.DeposerOr();
                else Deposer(h.Id);
                if (porte > 0) Dire(porte + " or versés à la caisse commune", 5f);
                return;
            }
            // Tas d'or : on passe dessus.
            for (int i = 0; i < generateur.Butins.Length; i++)
            {
                var r = generateur.Butins[i];
                if (r == null || r.butin != TypeButin.TasOr || ButinPris(i)) continue;
                if (Horizontal(h.transform.position, r.transform.position) < B.rayonTasOr && Mathf.Abs(h.transform.position.y - r.transform.position.y) < 1.5f) DemanderButin(i);
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
