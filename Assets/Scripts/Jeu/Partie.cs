using System;
using System.Collections.Generic;
using Deathless.Reseau;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deathless.Jeu
{
    /// Autorité de la partie (un par scène) : seule horloge du jeu (jour 120 s, crépuscule 5 s, nuit 120 s, aube 5 s),
    /// vote « prêt », victoire à l'aube de la nuit 12, défaite quand Nyxessa est détruite, morts et réapparitions, score.
    /// Tout l'état qui fait foi est dans `Etat` (données pures) ; les vues (ambiance, sons, HUD) s'abonnent aux événements.
    /// Multijoueur (Docs/reseau.md) : chaque poste simule son propre héros ; l'hôte fait apparaître les héros de tous
    /// (ApparaitreHerosReseau) et fera autorité sur le monde (ReseauJeu.Autorite) ; les héros des autres postes sont des
    /// marionnettes (Heros.Distant) tenues hors de Etat.joueurs.
    [DefaultExecutionOrder(-50)]
    public class Partie : MonoBehaviour
    {
        public static Partie Instance { get; private set; }
        /// Rejouer : la scène rechargée relance aussitôt une partie.
        public static bool LancerAuChargement;

        [Header("Réglages")]
        public GameBalance reglages;
        [Header("Scène")]
        public GameObject prefabHeros;
        public Sante nyxessa;
        public Transform[] pointsReapparition;
        public Transform pointDepart;
        public CameraEpaule cameraJeu;

        public EtatPartie Etat { get; private set; } = new EtatPartie();
        public GameBalance B => reglages != null ? reglages : GameBalance.Courant;
        public bool EnCours => Etat.phase != Phase.Attente && Etat.phase != Phase.Terminee && !m_Chute;

        readonly Dictionary<int, Heros> m_Heros = new Dictionary<int, Heros>();
        /// Mêmes héros que m_Heros, en liste tenue à jour (PoserHeros, RetirerHeros) : parcours par index sans allocation.
        readonly List<Heros> m_ListeHeros = new List<Heros>();
        readonly Dictionary<int, EtatJoueur> m_Distants = new Dictionary<int, EtatJoueur>();
        public Heros HerosLocal { get; private set; }
        EtatJoueur m_Local;
        public EtatJoueur JoueurLocal => m_Local ?? (Etat.joueurs.Count > 0 ? Etat.joueurs[0] : null);
        /// Client d'une partie réseau : l'hôte fait foi (horloge, monde, morts) ; ce poste suit (SuivreHote).
        public static bool ClientReseau => ReseauJeu.EnPartie && !ReseauJeu.Autorite;
        bool m_Chute;          // Nyxessa détruite, écran de score imminent
        float m_ChuteDepuis;
        bool m_AlerteDonnee;

        // ----------------------------------------------------------------- Événements (vues)
        public event Action<Phase, Phase> PhaseChangee;     // ancienne, nouvelle
        public event Action<int> NuitCommencee;
        public event Action AlerteNuit;
        public event Action<float, Vector3> NyxessaTouchee;
        public event Action NyxessaDetruite;
        public event Action PartieLancee;
        public event Action PartieTerminee;
        public event Action<int, bool> PretChange;           // joueur, prêt
        public event Action<int> JoueurMort;
        public event Action<int> JoueurReapparu;
        public event Action<int, Vector3> OrGagne;          // montant, point

        // ----------------------------------------------------------------- Mode exploration (aperçu de la nouvelle carte)
        /// Mode exploration (02/10/2026) : héros solo sur la scène CarteV5, jour figé à midi, ni vagues ni nuit ni défaite,
        /// pas de vote « prêt », pas de multijoueur. Demandé par le menu principal (OuvrirCarteExploration), lu une fois au
        /// chargement de la scène : tout autre chargement (retour au menu) le remet à faux.
        public static bool Exploration { get; private set; }
        static bool s_ExplorationDemandee;
        public const string SceneCarteExploration = "CarteV5";

        /// Touches de dev de l'aperçu (02/10/2026, F9 / F10, DonjonJeu.ToucheApercu) : nuit forcée (visuelle seulement : la
        /// phase reste le jour figé, donc aucune vague) et « sans mob ». Faux hors du mode exploration ; remis à faux à
        /// chaque chargement de scène. Ils survivent aux allers-retours par les portails du donjon et aux graines de F8.
        static bool s_NuitApercu, s_SansMobApercu;
        public static bool NuitApercu => Exploration && s_NuitApercu;
        public static bool SansMobApercu => Exploration && s_SansMobApercu;
        public static void DefinirNuitApercu(bool nuit) { if (Exploration) s_NuitApercu = nuit; }
        public static void DefinirSansMobApercu(bool sansMob) { if (Exploration) s_SansMobApercu = sansMob; }

        /// Menu principal > Nouvelle carte (aperçu) : charge la scène de la carte v5 en mode exploration, avec la dernière
        /// classe choisie (Paladin à défaut).
        public static void OuvrirCarteExploration()
        {
            if (ReseauJeu.Actif) return;
            var c = Deathless.UI.Donnees.ClassesJouables.Derniere;
            ClasseChoisie = c != null && !string.IsNullOrEmpty(c.Id) ? c.Id : "paladin";
            s_ExplorationDemandee = true;
            LancerAuChargement = false;
            SceneManager.LoadScene(SceneCarteExploration);
        }

        void Awake()
        {
            Exploration = s_ExplorationDemandee;
            s_ExplorationDemandee = false;
            s_NuitApercu = false;
            s_SansMobApercu = false;
            Instance = this;
            Ivresse.Reinitialiser();
            if (reglages != null) GameBalance.Courant = reglages;
            ReseauJeu.Assurer();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// Ancre d'échange de la taverne (le composant Taverne s'y pose ; on y commande depuis 2,4 m à plat). Carte v5 (03/10/2026) : au
        /// comptoir de service extérieur de la tavernière, devant la porte (Villageois/Ancre_Echange_Taverne) ; ancienne carte : au comptoir
        /// de l'intérieur. Null si le village n'a pas de taverne.
        public static GameObject AncreTaverne()
        {
            var a = GameObject.Find("VillageBlockout/Villageois/Ancre_Echange_Taverne");
            return a != null ? a : GameObject.Find("VillageBlockout/Interieurs/Interieur_Taverne/Ancre_Echange_Taverne");
        }

        void Start()
        {
            if (nyxessa != null)
            {
                // Achats des paliers à la relique (touche Interagir, de jour).
                if (nyxessa.GetComponent<AchatRelique>() == null) nyxessa.gameObject.AddComponent<AchatRelique>();
                // Taverne : au comptoir (ancre d'échange : celui, extérieur, de la carte v5, ou celui de l'intérieur de l'ancienne carte).
                var comptoir = AncreTaverne();
                if (comptoir != null && comptoir.GetComponent<Taverne>() == null) comptoir.AddComponent<Taverne>();
                nyxessa.equipe = Equipe.Relique;
                nyxessa.Initialiser(B.nyxessaPV);
                nyxessa.Touche += OnNyxessaTouchee;
                nyxessa.Tue += _ => OnNyxessaDetruite();
            }
            Etat.nyxessa.pvMax = B.nyxessaPV;
            Etat.nyxessa.pv = B.nyxessaPV;
            if (Exploration)
            {
                LancerAuChargement = false;
                StartCoroutine(LancerApresUneImage());
            }
            else if (ReseauJeu.EnPartie)
            {
                // Partie réseau : le village vient d'être chargé par l'hôte ; la partie commence quand le héros de ce
                // poste apparaît (HerosReseau → LancerReseau).
                LancerAuChargement = false;
                Journal("Village chargé en mode réseau (" + (ReseauJeu.Autorite ? "hôte" : "client") + "), attente des héros");
            }
            else if (LancerAuChargement || B.lancerDirectement)
            {
                LancerAuChargement = false;
                if (B.lancerDirectement && !string.IsNullOrEmpty(B.classeDeTest)) ClasseChoisie = B.classeDeTest;
                StartCoroutine(LancerApresUneImage());
            }
        }

        // Une image d'attente : toutes les vues (vagues, ambiance, HUD) se sont abonnées dans leur Start.
        System.Collections.IEnumerator LancerApresUneImage()
        {
            yield return null;
            LancerSolo();
        }

        // ----------------------------------------------------------------- Commandes (IPartieCommandes)

        /// Classe du joueur local (choix de classe ; gardée pour « Rejouer »).
        public static string ClasseChoisie = "paladin";

        /// Menu principal > Solo : lance la partie avec la dernière classe choisie.
        public void LancerSolo() => LancerSolo(ClasseChoisie);

        /// Crée le héros de la classe `classeId` et lance la partie au jour qui précède la nuit de départ.
        public void LancerSolo(string classeId)
        {
            if (Etat.phase != Phase.Attente) return;
            var def = Definition(ref classeId);
            var prefab = def != null && def.prefab != null ? def.prefab : prefabHeros;
            Heros h = null;
            if (prefab != null)
            {
                Vector3 p = pointDepart != null ? pointDepart.position : PointReapparition(Vector3.zero);
                Quaternion r = pointDepart != null ? pointDepart.rotation : Quaternion.LookRotation(-new Vector3(p.x, 0f, p.z).normalized);
                var go = Instantiate(prefab, p, r);
                go.name = "Heros_" + classeId;
                h = go.GetComponent<Heros>();
            }
            Commencer(NouveauJoueur(1, "Joueur", classeId, def), h);
        }

        ClassesJeu.ClasseDef Definition(ref string classeId)
        {
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(classeId) : null;
            if (def == null || def.prefab == null) { classeId = "paladin"; def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(classeId) : null; }
            return def;
        }

        EtatJoueur NouveauJoueur(int id, string nom, string classeId, ClassesJeu.ClasseDef def)
        {
            var b = B;
            return new EtatJoueur { id = id, nom = nom, classe = def != null ? def.nom : "Paladin", classeId = classeId, enduranceMax = b.endurance, endurance = b.endurance };
        }

        /// Le joueur local et son héros entrent en jeu : la partie commence (jour qui précède la nuit de départ), ou, pour
        /// un client qui arrive en cours de partie, directement à la phase et à la nuit de l'hôte.
        void Commencer(EtatJoueur j, Heros h, Phase? phaseHote = null, int nuitHote = 0)
        {
            var b = B;
            ClasseChoisie = j.classeId;
            Etat.joueurs.Insert(0, j);
            m_Local = j;
            if (h != null)
            {
                h.Initialiser(this, j);
                h.EcrireEtat(j);
                PoserHeros(j.id, h);
                HerosLocal = h;
                if (cameraJeu != null) cameraJeu.Suivre(h.transform);
            }
            Etat.nuit = nuitHote > 0 ? Mathf.Clamp(nuitHote, 1, b.nuitsPourGagner) : Mathf.Clamp(b.nuitDeDepart, 1, b.nuitsPourGagner);
            Etat.duree = 0f;
            Journal("Partie lancée (" + j.classeId + ", nuit " + (phaseHote.HasValue ? "de l'hôte " : "de départ ") + Etat.nuit + ", vitesse ×" + b.vitesseCycle + ")");
            PartieLancee?.Invoke();
            Passer(Exploration ? Phase.Jour : phaseHote ?? (b.commencerALaNuit ? Phase.Crepuscule : Phase.Jour));
            if (Exploration) { Etat.nuit = 1; Etat.tempsPhase = Etat.dureePhase * 0.5f; Journal("Mode exploration : jour figé, pas de vagues"); }
        }

        // ----------------------------------------------------------------- Multijoueur (Docs/reseau.md)

        /// Identifiant de joueur d'un poste réseau (1 pour l'hôte, comme en solo).
        public static int IdJoueur(ulong clientId) => (int)clientId + 1;

        /// Héros des autres postes (multijoueur ; vide en solo), pour la colonne du HUD.
        public IEnumerable<Heros> HerosDistants { get { foreach (var id in m_Distants.Keys) if (m_Heros.TryGetValue(id, out var h) && h != null) yield return h; } }

        /// Hôte, village chargé chez tous : le héros de chaque joueur du salon (sa classe) apparaît près de Nyxessa,
        /// côte à côte au point de départ ; chaque poste prend le contrôle du sien.
        public void ApparaitreHerosReseau()
        {
            var nm = ReseauJeu.Instance != null ? ReseauJeu.Instance.Reseau : null;
            if (nm == null || !nm.IsServer) return;
            // Le monde de la partie (horloge, Nyxessa, scores, sorcier) : tenu ici, suivi par les clients.
            var mondePrefab = Resources.Load<GameObject>("Reseau/PartieReseau");
            if (mondePrefab != null && PartieReseau.Instance == null) Instantiate(mondePrefab).GetComponent<Unity.Netcode.NetworkObject>().Spawn(true);
            var joueurs = new List<JoueurSalon>(ReseauJeu.JoueursPartie);
            Vector3 p0 = pointDepart != null ? pointDepart.position : PointReapparition(Vector3.zero);
            Quaternion r = pointDepart != null ? pointDepart.rotation : Quaternion.LookRotation(-new Vector3(p0.x, 0f, p0.z).normalized);
            Vector3 cote = r * Vector3.right;
            for (int i = 0; i < joueurs.Count; i++)
            {
                var js = joueurs[i];
                if (!nm.ConnectedClients.ContainsKey(js.clientId)) continue;
                string classeId = js.classeId.ToString();
                var def = Definition(ref classeId);
                if (def == null || def.prefab == null || def.prefab.GetComponent<Unity.Netcode.NetworkObject>() == null)
                {
                    Debug.LogError("[Réseau] préfab réseau manquant pour la classe " + classeId);
                    continue;
                }
                Vector3 p = p0 + cote * ((i - (joueurs.Count - 1) * 0.5f) * 1.8f);
                var go = Instantiate(def.prefab, p, r);
                var hr = go.GetComponent<HerosReseau>();
                hr.Preparer(js.pseudo.ToString(), classeId);
                go.GetComponent<Unity.Netcode.NetworkObject>().SpawnAsPlayerObject(js.clientId, true);
                ReseauJeu.Journal("héros de " + js.pseudo + " (" + classeId + ") apparu pour le client " + js.clientId);
            }
        }

        /// Le héros de ce poste est apparu (réseau) : la partie commence ici. Arrivée en cours de partie (retour après une
        /// coupure, ou nouveau joueur) : ce poste se cale sur l'horloge de l'hôte (phase, nuit ; le temps suit par
        /// SuivreHote) au lieu de repartir du jour de départ, et retrouve ses rangs de compétence s'il revient.
        public void LancerReseau(Heros h, string classeId, string pseudo, ulong clientId)
        {
            if (Etat.phase != Phase.Attente || h == null) return;
            var def = Definition(ref classeId);
            var j = NouveauJoueur(IdJoueur(clientId), pseudo, classeId, def);
            var r = PartieReseau.Instance;
            var ph = r != null ? (Phase)r.Phase.Value : Phase.Attente;
            if (ClientReseau && ph != Phase.Attente && ph != Phase.Terminee)
            {
                bool retour = Rattraper(j, r.Nuit.Value, ph);
                Commencer(j, h, ph, r.Nuit.Value);
                if (retour) Deathless.Succes.ServiceSucces.Reconnexion();   // succès « Je reviens tout de suite »
                string msg = (retour ? "De retour dans la partie" : "Tu rejoins la partie en cours") + " (nuit " + Etat.nuit + ").";
                DonjonJeu.Instance?.Annoncer(msg, 6f);
                ReseauJeu.Journal("arrivée en cours de partie : " + ph + ", nuit " + Etat.nuit + (retour ? ", rangs retrouvés" : "") + ", " + j.pointsCompetence + " point(s)");
                return;
            }
            Commencer(j, h);
        }

        /// Client qui revient après une coupure : ce que ce poste tenait seul (rangs, points, jours survécus), gardé à la
        /// déconnexion (GarderPourRetour) pour le code du salon. Survit au rechargement du village.
        sealed class Retour { public string code, classeId; public int[] rangs, attributs; public int points, pointsAttribut, nuits; }
        static Retour s_Retour;

        /// Client déconnecté en partie (LobbyReseau.SurDeconnexion) : rangs et points de compétence gardés pour un retour
        /// par le même code.
        public void GarderPourRetour(string code)
        {
            var j = m_Local;
            if (j == null || string.IsNullOrEmpty(code)) { s_Retour = null; return; }
            s_Retour = new Retour { code = code, classeId = j.classeId, rangs = (int[])j.rangs?.Clone(), points = j.pointsCompetence, nuits = j.nuitsSurvecues,
                attributs = (int[])j.attributs?.Clone(), pointsAttribut = j.pointsAttribut };
        }

        /// Arrivée en cours de partie : jours survécus d'après l'horloge de l'hôte (1 point par aube passée depuis la nuit
        /// de départ) ; au retour du même joueur (même code, même classe), ses rangs et ses points gardés, plus les aubes
        /// passées pendant son absence. Vrai si c'est un retour.
        bool Rattraper(EtatJoueur j, int nuit, Phase ph)
        {
            var b = B;
            int nuits = Mathf.Max(0, nuit - Mathf.Clamp(b.nuitDeDepart, 1, b.nuitsPourGagner) + (ph == Phase.Aube ? 1 : 0));
            var lobby = ReseauJeu.Instance != null ? ReseauJeu.Instance.Lobby : null;
            var r = s_Retour;
            s_Retour = null;
            if (r != null && lobby != null && r.code == lobby.CodeSalon && r.classeId == j.classeId)
            {
                if (r.rangs != null) j.rangs = r.rangs;
                j.nuitsSurvecues = Mathf.Max(r.nuits, nuits);
                j.pointsCompetence = r.points + Mathf.Max(0, nuits - r.nuits);
                // Attributs (01/10/2026) : gardés comme les rangs, plus un point par aube passée pendant l'absence.
                if (r.attributs != null) j.attributs = r.attributs;
                j.pointsAttribut = r.pointsAttribut + Mathf.Max(0, nuits - r.nuits);
                return true;
            }
            j.nuitsSurvecues = nuits;
            j.pointsCompetence = nuits;
            j.pointsAttribut = nuits;
            return false;
        }

        /// Hôte : un joueur arrive en cours de partie (ReseauJeu, après sa synchronisation) : son état est remis dans
        /// Etat.joueurs s'il revient (score, or porté ; nouvel identifiant de poste), puis son héros de sa classe apparaît
        /// près de Nyxessa ; ce qui n'a été envoyé que par RPC ponctuels (zones, bouclier) lui est rejoué.
        public void ArriveeEnCours(JoueurSalon js, EtatJoueur ancien)
        {
            var nm = ReseauJeu.Instance != null ? ReseauJeu.Instance.Reseau : null;
            if (nm == null || !nm.IsServer || !nm.ConnectedClients.ContainsKey(js.clientId)) return;
            string classeId = js.classeId.ToString();
            var def = Definition(ref classeId);
            if (def == null || def.prefab == null || def.prefab.GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                Debug.LogError("[Réseau] préfab réseau manquant pour la classe " + classeId);
                return;
            }
            int id = IdJoueur(js.clientId);
            if (ancien != null)
            {
                ancien.id = id;
                ancien.nom = js.pseudo.ToString();
                ancien.mort = false;
                ancien.reapparitionRestante = 0f;
                ancien.pret = false;
                if (Joueur(id) == null) Etat.joueurs.Add(ancien);
            }
            Vector3 centre = nyxessa != null ? nyxessa.transform.position : Vector3.zero;
            Vector3 p = PointReapparition(pointDepart != null ? pointDepart.position : centre);
            Vector3 dehors = new Vector3(p.x - centre.x, 0f, p.z - centre.z);
            Quaternion rot = dehors.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dehors.normalized) : Quaternion.identity;
            var go = Instantiate(def.prefab, p, rot);
            go.GetComponent<HerosReseau>().Preparer(js.pseudo.ToString(), classeId);
            go.GetComponent<Unity.Netcode.NetworkObject>().SpawnAsPlayerObject(js.clientId, true);
            PartieReseau.Instance?.Rattraper(js.clientId);
            DonjonJeu.Instance?.Annoncer(js.pseudo + (ancien != null ? " est de retour." : " rejoint la partie."), 5f);
            ReseauJeu.Journal("héros de " + js.pseudo + " (" + classeId + ") apparu en cours de partie pour le client " + js.clientId + (ancien != null ? " (retour : score et or gardés)" : ""));
        }

        /// Héros d'un autre poste : marionnette (position et animations par le réseau), hors de Etat.joueurs.
        public void AttacherHerosDistant(Heros h, string classeId, string pseudo, ulong clientId)
        {
            if (h == null) return;
            var def = Definition(ref classeId);
            // Hôte : un joueur qui revient en cours de partie a déjà son état (score, or porté) remis dans Etat.joueurs.
            var j = (ReseauJeu.Autorite ? Joueur(IdJoueur(clientId)) : null) ?? NouveauJoueur(IdJoueur(clientId), pseudo, classeId, def);
            h.DevenirDistant();
            h.Initialiser(this, j);
            PoserHeros(j.id, h);
            m_Distants[j.id] = j;
            // L'hôte tient l'état de tous les joueurs (vote, morts, score, nombre d'ennemis).
            if (ReseauJeu.Autorite && Joueur(j.id) == null) Etat.joueurs.Add(j);
            Journal("Héros distant : " + pseudo + " (" + classeId + ")");
        }

        public void DetacherHeros(ulong clientId)
        {
            int id = IdJoueur(clientId);
            if (m_Distants.Remove(id)) RetirerHeros(id);
            var j = Joueur(id);
            if (j != null && j != m_Local) { Etat.joueurs.Remove(j); m_Detaches[id] = j; }
        }

        /// Etats retirés avec leur héros (Netcode despawne le héros d'un client avant d'annoncer son départ) : JoueurParti
        /// les retrouve pour les garder au retour du joueur.
        readonly Dictionary<int, EtatJoueur> m_Detaches = new Dictionary<int, EtatJoueur>();

        /// Hôte : un joueur a quitté la partie (son héros disparaît avec lui). Renvoie son état (score, or porté), que
        /// ReseauJeu garde pour son retour.
        public EtatJoueur JoueurParti(ulong clientId)
        {
            Journal("Joueur " + IdJoueur(clientId) + " a quitté la partie");
            int id = IdJoueur(clientId);
            var j = Joueur(id);
            if (j == null) m_Detaches.TryGetValue(id, out j);
            DetacherHeros(clientId);
            m_Detaches.Remove(id);
            return j != null && j != m_Local ? j : null;
        }

        // ----------------------------------------------------------------- Perte du réseau (hôte) : la partie continue seule

        struct EnnemiGarde { public TypeEnnemi type; public bool elite; public Vector3 position; public Quaternion rotation; public float pv, pvMax; }
        readonly List<EnnemiGarde> m_EnnemisGardes = new List<EnnemiGarde>();
        bool m_HerosGarde, m_HerosGardeMort;
        Vector3 m_HerosGardePos;
        Quaternion m_HerosGardeRot;
        float m_HerosGardePv, m_HerosGardePvMax;

        /// Hôte, juste avant l'arrêt non voulu de Netcode (ReseauJeu, OnPreShutdown) : les objets réseau vont être
        /// détruits ; on note le héros local (position, vie) et les squelettes vivants (type, élite, position, vie).
        public void NoterAvantPerteReseau()
        {
            m_HerosGarde = false;
            m_EnnemisGardes.Clear();
            var h = HerosLocal;
            if (h != null)
            {
                m_HerosGarde = true;
                m_HerosGardePos = h.transform.position;
                m_HerosGardeRot = h.transform.rotation;
                m_HerosGardePv = h.Sante != null ? h.Sante.Pv : 0f;
                m_HerosGardePvMax = h.Sante != null ? h.Sante.pvMax : 0f;
                m_HerosGardeMort = m_Local != null && m_Local.mort || !h.Vivant;
            }
            var dv = DirecteurVagues.Instance;
            if (dv != null)
                foreach (var sq in dv.Vivants)
                {
                    // Les gardiens du donjon ne sont pas reposés (le donjon se referme au crépuscule).
                    if (sq == null || !sq.Vivant || sq.Sante == null || sq.Sante.Mort || sq.Gardien) continue;
                    m_EnnemisGardes.Add(new EnnemiGarde { type = sq.type, elite = sq.elite, position = sq.transform.position, rotation = sq.transform.rotation, pv = sq.Sante.Pv, pvMax = sq.Sante.pvMax });
                }
            Journal("Réseau perdu : héros et " + m_EnnemisGardes.Count + " squelettes notés");
        }

        /// Hôte, une image après l'arrêt non voulu de Netcode : la partie continue en solo, sans rechargement. Le héros
        /// local est recréé (objet local, même classe, même état de joueur : score, or, rangs de compétence ; même
        /// position et même vie, ou au point de réapparition s'il était mort) et suivi par la caméra ; les squelettes
        /// détruits avec les objets réseau sont reposés ici ; les vagues à venir, Nyxessa, le sorcier et le cycle
        /// continuent (ce poste faisait déjà autorité).
        public void ContinuerSeul()
        {
            if (ReseauJeu.Actif) return;
            Deathless.Succes.ServiceSucces.HoteSeul();   // succès « Seul contre tous » à la prochaine nuit tenue
            m_Distants.Clear();
            for (int i = Etat.joueurs.Count - 1; i >= 0; i--) if (Etat.joueurs[i] != m_Local) Etat.joueurs.RemoveAt(i);
            for (int i = m_ListeHeros.Count - 1; i >= 0; i--) if (m_ListeHeros[i] == null) m_ListeHeros.RemoveAt(i);
            var morts = new List<int>();
            foreach (var kv in m_Heros) if (kv.Value == null) morts.Add(kv.Key);
            foreach (var k in morts) m_Heros.Remove(k);
            var j = m_Local;
            if (j != null && HerosLocal == null && m_HerosGarde)
            {
                string classeId = j.classeId;
                var def = Definition(ref classeId);
                var prefab = def != null && def.prefab != null ? def.prefab : prefabHeros;
                if (prefab != null)
                {
                    Vector3 p = m_HerosGardeMort ? PointReapparition(m_HerosGardePos) : m_HerosGardePos;
                    var go = Instantiate(prefab, p, m_HerosGardeRot);
                    go.name = "Heros_" + classeId;
                    var h = go.GetComponent<Heros>();
                    h.Initialiser(this, j);
                    if (!m_HerosGardeMort && m_HerosGardePvMax > 0f) h.Sante.Fixer(Mathf.Max(1f, m_HerosGardePv), Mathf.Max(h.Sante.pvMax, m_HerosGardePvMax));
                    j.mort = false;
                    j.reapparitionRestante = 0f;
                    h.EcrireEtat(j);
                    PoserHeros(j.id, h);
                    HerosLocal = h;
                    if (cameraJeu != null) cameraJeu.Suivre(h.transform);
                }
            }
            int reposes = 0;
            var dv = DirecteurVagues.Instance;
            if (dv != null)
            {
                dv.OublierDisparus();
                if (EnCours)
                    foreach (var e in m_EnnemisGardes)
                    {
                        var sq = dv.Poser(e.type, e.position, e.elite, false);
                        if (sq == null) continue;
                        sq.transform.rotation = e.rotation;
                        if (e.pvMax > 0f) sq.Sante.Fixer(e.pv, e.pvMax);
                        reposes++;
                    }
            }
            m_EnnemisGardes.Clear();
            m_HerosGarde = false;
            Etat.comptePret = false;
            if (Etat.phase == Phase.Jour) EvaluerPrets();
            DonjonJeu.Instance?.Annoncer("Connexion perdue : la partie continue en solo.", 8f);
            ReseauJeu.Journal("partie continuée en solo (" + Etat.phase + ", nuit " + Etat.nuit + ", " + reposes + " squelettes reposés)");
        }

        /// Vote « prêt » (jour) ou Rejouer (écran de score).
        public void BasculerPret(int joueurId = 1)
        {
            // Client : le vote part vers l'hôte, qui le compte (son état revient par PartieReseau).
            if (Exploration) return;
            if (ClientReseau) { PartieReseau.Instance?.DemanderPret(); return; }
            var j = Joueur(joueurId);
            if (j == null) return;
            if (Etat.phase == Phase.Terminee)
            {
                j.pret = !j.pret;
                PretChange?.Invoke(joueurId, j.pret);
                if (TousPrets())
                {
                    // Rejouer : en réseau, l'hôte recharge le village pour tous (mêmes joueurs, mêmes classes).
                    if (ReseauJeu.EnPartie) ReseauJeu.Instance.LancerPartie();
                    else Recharger(true);
                }
                return;
            }
            if (Etat.phase != Phase.Jour) return;
            if (!j.pret && DonjonJeu.QuelquunAuDonjon)
            {
                AudioBank.Jouer2D(SonsDuJeu.PretAnnule);
                Journal("Vote refusé : un joueur est au donjon");
                return;
            }
            j.pret = !j.pret;
            AudioBank.Jouer2D(j.pret ? SonsDuJeu.Pret : SonsDuJeu.PretAnnule);
            PretChange?.Invoke(joueurId, j.pret);
            EvaluerPrets();
        }

        /// Tous prêts (et toute l'équipe rentrée) : le jour est écourté ; sinon le compte à rebours est annulé.
        public void EvaluerPrets()
        {
            if (Etat.phase != Phase.Jour || ClientReseau) return;
            if (TousPrets() && !Etat.comptePret)
            {
                Etat.comptePret = true;
                // Le jour est écourté : il reste le compte à rebours (5 s).
                float reste = Mathf.Min(Etat.TempsRestant, B.compteAReboursPret);
                Etat.tempsPhase = Etat.dureePhase - reste;
                AudioBank.Jouer2D(SonsDuJeu.TousPrets);
            }
            else if (!TousPrets() && Etat.comptePret)
            {
                // Vote annulé pendant le compte à rebours : le jour reprend son cours normal (temps restant gardé).
                Etat.comptePret = false;
            }
        }

        public void QuitterPartie()
        {
            // Multijoueur : on quitte d'abord la session (l'hôte la ferme pour tous), puis retour au menu en solo.
            if (ReseauJeu.Actif && ReseauJeu.Instance.Lobby != null) ReseauJeu.Instance.Lobby.Quitter();
            if (Exploration)
            {
                // Retour au menu principal, sur l'ancienne carte (Village.unity, première scène du build).
                Exploration = false;
                LancerAuChargement = false;
                SceneManager.LoadScene(0);
                return;
            }
            Recharger(false);
        }

        public void QuitterJeu()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Recharger(bool relancer)
        {
            LancerAuChargement = relancer;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().path : SceneManager.GetActiveScene().name);
        }

        bool TousPrets()
        {
            if (Etat.joueurs.Count == 0) return false;
            // Wiki : le vote n'est possible que quand toute l'équipe est rentrée du donjon.
            if (Etat.phase == Phase.Jour && DonjonJeu.QuelquunAuDonjon) return false;
            foreach (var j in Etat.joueurs) if (!j.pret) return false;
            return true;
        }

        public int JoueursPrets { get { int n = 0; foreach (var j in Etat.joueurs) if (j.pret) n++; return n; } }

        // ----------------------------------------------------------------- Horloge

        void Update()
        {
            if (ClientReseau && Etat.phase != Phase.Attente) { SuivreHote(Time.deltaTime); return; }
            if (Etat.phase == Phase.Attente || Etat.phase == Phase.Terminee) return;
            float dt = Time.deltaTime;
            if (m_Chute)
            {
                m_ChuteDepuis += dt;
                if (m_ChuteDepuis >= B.delaiScoreDefaite) Terminer(Resultat.Defaite);
                return;
            }
            Etat.duree += dt;
            if (Exploration)
            {
                // Jour figé à midi : ni alerte, ni crépuscule, ni vagues.
                Etat.tempsPhase = Etat.dureePhase * 0.5f;
                MettreAJourJoueurs(dt);
                return;
            }
            Etat.tempsPhase += dt;

            // Alerte avant la nuit (wiki : 15 s avant le crépuscule).
            if (Etat.phase == Phase.Jour && !m_AlerteDonnee && Etat.TempsRestant <= B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle) && Etat.dureePhase > B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle))
            {
                m_AlerteDonnee = true;
                AudioBank.Jouer2D(SonsDuJeu.AlerteNuit);
                AlerteNuit?.Invoke();
            }

            MettreAJourJoueurs(dt);

            if (Etat.tempsPhase >= Etat.dureePhase)
            {
                switch (Etat.phase)
                {
                    case Phase.Jour: Passer(Phase.Crepuscule); break;
                    case Phase.Crepuscule: Passer(Phase.Nuit); break;
                    case Phase.Nuit:
                        // Décision du 30/09/2026 : le jour ne se lève que quand le boss de la nuit (Morgrim nuit 10,
                        // Nyxar nuit 12) est mort ; d'ici là, la nuit se prolonge, chrono figé à 0:00.
                        var dv = DirecteurVagues.Instance;
                        if (dv != null && dv.BossAttendu(out var boss))
                        {
                            if (!Etat.aubeRetenue) Journal("Temps de la nuit écoulé : l'aube attend la chute de " + NomBoss(boss));
                            Etat.aubeRetenue = true;
                            Etat.bossAttendu = boss;
                            Etat.tempsPhase = Etat.dureePhase;
                            dv.DebloquerBoss(Time.deltaTime);
                        }
                        else Passer(Phase.Aube);
                        break;
                    case Phase.Aube:
                        if (Etat.nuit >= B.nuitsPourGagner) Terminer(Resultat.Victoire);
                        else { Etat.nuit++; Passer(Phase.Jour); }
                        break;
                }
            }
        }

        // ----------------------------------------------------------------- Client d'une partie réseau

        float m_TempsHote = -1f;
        float m_MortSignalee = -99f;
        float m_NyxVue = -1f;
        bool m_NyxDetruiteVue;

        /// Client : l'horloge, Nyxessa, la caisse et l'état du joueur local viennent de l'hôte (PartieReseau) ; les
        /// changements de phase rejouent ici les mêmes événements (vues, sons, HUD) sans rien décider.
        void SuivreHote(float dt)
        {
            var r = PartieReseau.Instance;
            if (r == null) return;
            var ph = (Phase)r.Phase.Value;
            Etat.nuit = r.Nuit.Value;
            if (ph != Etat.phase && Etat.phase != Phase.Terminee)
            {
                if (ph == Phase.Terminee)
                {
                    Terminer((Resultat)r.Resultat.Value);
                    Etat.nuitAtteinte = r.NuitAtteinte.Value;
                }
                else if (ph != Phase.Attente) Passer(ph);
            }
            if (Etat.phase == Phase.Terminee) { Etat.nuitAtteinte = r.NuitAtteinte.Value; LireJoueurLocal(r); return; }
            // Temps : la dernière valeur de l'hôte, puis on avance seul jusqu'à la suivante.
            if (r.TempsPhase.Value != m_TempsHote) { m_TempsHote = r.TempsPhase.Value; Etat.tempsPhase = m_TempsHote; Etat.duree = r.Duree.Value; }
            else { Etat.tempsPhase += dt; Etat.duree += dt; }
            Etat.dureePhase = r.DureePhase.Value;
            // L'aube attend la chute du boss (hôte) : même message ici, chrono figé à 0:00.
            byte attente = r.AubeAttend.Value;
            Etat.aubeRetenue = attente != 0 && Etat.phase == Phase.Nuit;
            if (Etat.aubeRetenue) { Etat.bossAttendu = (TypeEnnemi)(attente - 1); Etat.tempsPhase = Mathf.Min(Etat.tempsPhase, Etat.dureePhase); }
            Etat.comptePret = r.ComptePret.Value;
            Etat.orEquipe = r.OrEquipe.Value;
            // Vagues (01/10/2026) : numéro et total de la nuit, pour le repère du HUD (le directeur ne tourne que chez l'hôte).
            Etat.vagues.vague = r.Vague.Value;
            Etat.vagues.total = r.VaguesTotal.Value;
            Etat.nyxessa.palierMissiles = r.PalierMissiles.Value;
            Etat.nyxessa.palierBouclier = r.PalierBouclier.Value;
            SuivreMissiles(r.StockMissiles.Value, r.MissileRegeneration.Value, dt);
            if (nyxessa != null)
            {
                nyxessa.Fixer(r.NyxPv.Value, r.NyxPvMax.Value);
                Etat.nyxessa.pv = nyxessa.Pv;
                Etat.nyxessa.pvMax = nyxessa.pvMax;
                if (m_NyxVue >= 0f && nyxessa.Pv < m_NyxVue - 0.01f)
                {
                    Etat.nyxessa.dernierCoup = Time.time;
                    NyxessaTouchee?.Invoke(m_NyxVue - nyxessa.Pv, nyxessa.transform.position + Vector3.up * 2f);
                }
                m_NyxVue = nyxessa.Pv;
            }
            if (r.NyxDetruite.Value && !m_NyxDetruiteVue)
            {
                m_NyxDetruiteVue = true;
                Etat.nyxessa.detruite = true;
                var d = nyxessa != null ? nyxessa.GetComponent<DefenseNyxessa>() : null;
                if (d != null) d.DetruireVisuel();
                NyxessaDetruite?.Invoke();
            }
            // Alerte avant la nuit (même règle que chez l'hôte).
            if (Etat.phase == Phase.Jour && !m_AlerteDonnee && Etat.TempsRestant <= B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle) && Etat.dureePhase > B.alerteAvantNuit / Mathf.Max(0.01f, B.vitesseCycle))
            {
                m_AlerteDonnee = true;
                AudioBank.Jouer2D(SonsDuJeu.AlerteNuit);
                AlerteNuit?.Invoke();
            }
            LireJoueurLocal(r);
        }

        float m_RegenMissilesHote = -1f;

        /// Client : stock des missiles de Nyxessa et recharge du prochain, tels que l'hôte les tient (PartieReseau,
        /// StockMissiles à chaque changement, MissileRegeneration 5 fois par seconde comme TempsPhase) ; on avance seul de
        /// dt entre deux envois, et on se recale dès qu'une nouvelle valeur arrive (même règle que le temps de phase).
        /// Nécessaire depuis la canalisation du bouclier (27/09/2026) : un gros coup peut faire bondir la recharge sans
        /// faire monter le stock, ce qu'une extrapolation en dt seule ne peut plus suivre.
        void SuivreMissiles(int stock, float regenHote, float dt)
        {
            var n = Etat.nyxessa;
            int max = GameBalance.AuPalier(B.missilesStockPaliers, n.palierMissiles);
            float duree = GameBalance.AuPalier(B.missileRegenerationPaliers, n.palierMissiles);
            if (stock >= max) n.regeneration = 0f;
            else if (regenHote != m_RegenMissilesHote) n.regeneration = regenHote;
            else n.regeneration = Mathf.Min(n.regeneration + dt, duree);
            m_RegenMissilesHote = regenHote;
            n.stock = stock;
        }

        /// Client : vote, mort, délai et score du joueur local tels que l'hôte les tient ; vie et endurance restent locales.
        void LireJoueurLocal(PartieReseau r)
        {
            var j = m_Local;
            if (j == null) return;
            if (HerosLocal != null) HerosLocal.EcrireEtat(j);
            if (!r.ScoreDe((ulong)Mathf.Max(0, j.id - 1), out var s)) return;
            if (j.pret != s.pret) { j.pret = s.pret; PretChange?.Invoke(j.id, j.pret); }
            // Mort : l'hôte fait foi ; juste après la mort locale, on attend qu'il l'ait comptée avant de le suivre.
            if (s.mort) { j.mort = true; j.reapparitionRestante = s.reapparition; }
            else if (j.mort && Time.time - m_MortSignalee > 1.5f) { j.mort = false; j.reapparitionRestante = 0f; }
            j.score.orRapporte = s.or; j.score.degatsInfliges = s.degats; j.score.ennemisTues = s.tues; j.score.morts = s.morts;
            j.score.coupsCritiques = s.critiques; j.score.degatsEvitesNyxessa = s.evites; j.score.soinsProdigues = s.soins;
            j.orPorte = s.orPorte;
        }

        /// Client : l'hôte fait réapparaître le héros de ce poste (délai écoulé ou aube).
        public void ReapparaitreLocal(Vector3 point)
        {
            var j = m_Local;
            if (j != null) { j.mort = false; j.reapparitionRestante = 0f; }
            var h = HerosLocal;
            if (h == null) return;
            h.Reapparaitre(point);
            var nt = h.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt != null && nt.IsSpawned && nt.IsOwner) nt.Teleport(h.transform.position, h.transform.rotation, h.transform.localScale);
            if (j != null) JoueurReapparu?.Invoke(j.id);
        }

        void Passer(Phase nouvelle)
        {
            var ancienne = Etat.phase;
            Etat.phase = nouvelle;
            Etat.tempsPhase = 0f;
            Etat.dureePhase = B.Duree(nouvelle);
            Etat.aubeRetenue = false;
            if (nouvelle == Phase.Jour)
            {
                m_AlerteDonnee = false;
                Etat.comptePret = false;
                foreach (var j in Etat.joueurs) j.pret = false;
            }
            if (nouvelle == Phase.Aube && m_Local != null && ancienne == Phase.Nuit)
            {
                // Wiki : 1 point de compétence par jour survécu, crédité à l'aube (chaque poste crédite son joueur).
                m_Local.pointsCompetence++;
                // Attributs (01/10/2026) : +1 point d'attribut à chaque aube survécue, en plus du point de compétence.
                m_Local.pointsAttribut++;
                m_Local.nuitsSurvecues++;
                PointCompetenceGagne?.Invoke(m_Local.pointsCompetence);
                Journal("Point de compétence : " + m_Local.pointsCompetence + " à dépenser ; point d'attribut : " + m_Local.pointsAttribut);
            }
            if (nouvelle == Phase.Aube && !ClientReseau)
            {
                // Wiki : un joueur mort revient au début de la nouvelle journée, même si son délai n'est pas écoulé.
                foreach (var j in Etat.joueurs) if (j.mort) Reapparaitre(j);
                if (B.nyxessaRegenAube > 0f && nyxessa != null) nyxessa.Soigner(nyxessa.pvMax * B.nyxessaRegenAube);
            }
            Journal("Phase " + nouvelle + " (nuit " + Etat.nuit + ", " + Etat.dureePhase.ToString("F1") + " s)");
            PhaseChangee?.Invoke(ancienne, nouvelle);
            if (nouvelle == Phase.Nuit) NuitCommencee?.Invoke(Etat.nuit);
        }

        void Terminer(Resultat r)
        {
            if (Etat.phase == Phase.Terminee) return;
            Etat.resultat = r;
            Etat.nuitAtteinte = r == Resultat.Victoire ? B.nuitsPourGagner : Etat.nuit;
            foreach (var j in Etat.joueurs) j.pret = false;
            var ancienne = Etat.phase;
            Etat.phase = Phase.Terminee;
            m_Chute = false;
            if (r == Resultat.Victoire) AudioBank.Jouer2D(SonsDuJeu.Victoire);
            Journal("Fin de partie : " + r + " (nuit " + Etat.nuitAtteinte + ", " + Etat.duree.ToString("F0") + " s)");
            PhaseChangee?.Invoke(ancienne, Phase.Terminee);
            PartieTerminee?.Invoke();
        }

        /// Test : fin forcée.
        public void ForcerFin(Resultat r) { Deathless.Succes.ServiceSucces.MarquerPartieTest("fin forcée"); Terminer(r); }

        /// Test : saute à une phase (nuit donnée).
        public void ForcerPhase(Phase p, int nuit)
        {
            Deathless.Succes.ServiceSucces.MarquerPartieTest("phase forcée");   // les succès ne comptent pas
            Etat.nuit = Mathf.Clamp(nuit, 1, B.nuitsPourGagner);
            Passer(p);
        }

        // ----------------------------------------------------------------- Boss de la nuit (30/09/2026)

        /// Un boss de la nuit sort de terre (tous les postes) : bannière du HUD.
        public event Action<TypeEnnemi> BossSurgi;

        public static string NomBoss(TypeEnnemi t) => t == TypeEnnemi.Necromancien ? "Nyxar" : "Morgrim";

        /// Annonce de la sortie du boss de la nuit (DirecteurVagues : sortie chez l'hôte, arrivée de la marionnette chez
        /// un client) : cri du boss, message du HUD et bannière ; une fois par nuit et par poste.
        public void SignalerBoss(TypeEnnemi t)
        {
            if (m_BossAnnonce == Etat.nuit) return;
            m_BossAnnonce = Etat.nuit;
            AudioBank.Jouer2D(t == TypeEnnemi.Necromancien ? SonsDuJeu.NyxarEnrage : SonsDuJeu.MorgrimCri, 0.9f);
            DonjonJeu.Instance?.Annoncer(t == TypeEnnemi.Necromancien
                ? "Nyxar, le Nécromancien, sort de terre : l’aube attendra sa chute."
                : "Morgrim, le Roi des os, sort de terre : l’aube attendra sa chute.", 7f);
            Journal("Boss annoncé : " + NomBoss(t) + " (nuit " + Etat.nuit + ")");
            BossSurgi?.Invoke(t);
        }

        int m_BossAnnonce = -1;

        // ----------------------------------------------------------------- Nyxessa

        void OnNyxessaTouchee(InfoDegats info, float reel)
        {
            Etat.nyxessa.pv = nyxessa.Pv;
            Etat.nyxessa.dernierCoup = Time.time;
            NyxessaTouchee?.Invoke(reel, info.point);
        }

        void OnNyxessaDetruite()
        {
            if (!EnCours) return;
            Etat.nyxessa.pv = 0f;
            Etat.nyxessa.detruite = true;
            m_Chute = true;
            m_ChuteDepuis = 0f;
            Journal("Nyxessa est détruite (nuit " + Etat.nuit + ")");
            NyxessaDetruite?.Invoke();
        }

        // ----------------------------------------------------------------- Achats à la relique (wiki : nyxessa, Paliers)

        public enum Amelioration { Missiles, Bouclier }

        /// Un palier vient d'être acheté (tous les postes) : amélioration, nouveau palier, pseudo de l'acheteur.
        public event Action<Amelioration, int, string> PalierAchete;

        // ----------------------------------------------------------------- Taverne (de jour, caisse commune)

        /// Raison pour laquelle cet achat à la taverne est impossible (null s'il est possible).
        public string RefusTaverne(Taverne.Article a, Heros h)
        {
            if (Etat.phase != Phase.Jour) return "Le tavernier ne sert que de jour.";
            if (Etat.orEquipe < Taverne.Prix(a)) return "Pas assez d’or dans la caisse commune.";
            if (a == Taverne.Article.Repas && h != null && h.Sante.Pv >= h.Sante.pvMax - 0.5f) return "Vous êtes déjà en pleine forme.";
            return null;
        }

        /// Achat à la taverne : l'autorité (hôte, ou ce poste en solo) décide et prend l'or ; un client transmet sa demande.
        /// Repas et bière font effet sur l'acheteur ; la tournée enivre tous les joueurs.
        public string PayerTaverne(Taverne.Article a, int joueurId, out bool ok)
        {
            ok = false;
            if (ClientReseau) { PartieReseau.Instance?.DemanderTaverne((byte)a); return "Commande passée au tavernier…"; }
            var h = HerosDe(joueurId);
            string refus = RefusTaverne(a, h);
            if (refus != null) return refus;
            int prix = Taverne.Prix(a);
            Etat.orEquipe -= prix;
            ok = true;
            var j = Joueur(joueurId);
            string qui = j != null ? j.nom : "Joueur";
            Journal("Taverne : " + a + " (" + prix + " or, par " + qui + ")");
            if (a == Taverne.Article.Tournee)
            {
                Tournee(qui);
                if (ReseauJeu.EnPartie) PartieReseau.Instance?.AnnoncerTournee(qui);
                return "Tournée payée (" + prix + " or) : santé !";
            }
            if (m_Local != null && joueurId == m_Local.id) Taverne.AppliquerLocal(a);
            return (a == Taverne.Article.Repas ? "Ragoût servi" : "Bière servie") + " (" + prix + " or).";
        }

        /// Tous les postes : une tournée vient d'être payée, le joueur local est ivre quelques secondes.
        public void Tournee(string qui)
        {
            Ivresse.Commencer(B.ivresseTournee);
            if (HerosLocal != null) AudioBank.Jouer(SonsDuJeu.Biere, HerosLocal.transform.position + Vector3.up, 0.8f);
            TourneePayee?.Invoke(qui);
        }

        /// Une tournée vient d'être payée (pseudo du généreux).
        public event Action<string> TourneePayee;

        /// Un point de compétence vient d'être gagné (joueur local) : total à dépenser.
        public event Action<int> PointCompetenceGagne;

        /// Menu du personnage : dépense d'un point pour le rang suivant de l'amélioration `index` (joueur local ; les effets
        /// sont simulés par son poste, comme ses attaques).
        public string AmeliorerCompetence(int index, out bool refus)
        {
            refus = true;
            var j = m_Local;
            if (j == null) return "Aucun personnage.";
            var defs = ArbreCompetences.De(j.classeId);
            if (index < 0 || index >= defs.Length) return "Rien à améliorer ici.";
            if (j.rangs == null || j.rangs.Length < defs.Length) System.Array.Resize(ref j.rangs, Mathf.Max(4, defs.Length));
            if (j.rangs[index] >= ArbreCompetences.RangMax) return "Rang maximal atteint.";
            if (j.pointsCompetence < ArbreCompetences.CoutParRang) return "Pas de point de compétence à dépenser.";
            j.pointsCompetence -= ArbreCompetences.CoutParRang;
            j.rangs[index]++;
            refus = false;
            Journal("Compétence améliorée : " + defs[index].nom + " rang " + j.rangs[index]);
            return defs[index].nom + " : rang " + j.rangs[index] + ".";
        }

        /// Menu du personnage : dépense d'un point d'attribut dans l'attribut `index` (Attribut ; joueur local, plafond
        /// Attributs.Plafond, pas de réattribution). Les effets sont simulés par son poste ; l'hôte reçoit les points
        /// gagnés par HerosReseau (recul, or ramassé).
        public string AmeliorerAttribut(int index, out bool refus)
        {
            refus = true;
            var j = m_Local;
            if (j == null) return "Aucun personnage.";
            if (index < 0 || index >= Attributs.Nombre) return "Rien à améliorer ici.";
            if (j.attributs == null || j.attributs.Length < Attributs.Nombre) System.Array.Resize(ref j.attributs, Attributs.Nombre);
            var a = (Attribut)index;
            if (!Attributs.SousPlafond(j, a)) return Attributs.Noms[index] + " est au maximum (" + Attributs.Plafond + ").";
            if (j.pointsAttribut < Attributs.CoutParPoint) return "Pas de point d’attribut à dépenser.";
            j.pointsAttribut -= Attributs.CoutParPoint;
            j.attributs[index]++;
            refus = false;
            HerosLocal?.AppliquerAttributs();
            Journal("Attribut amélioré : " + Attributs.Noms[index] + " " + Attributs.Valeur(j, a) + " (+" + j.attributs[index] + " gagné)");
            return Attributs.Noms[index] + " : " + Attributs.Valeur(j, a) + ".";
        }

        public int PalierDe(Amelioration a) => a == Amelioration.Missiles ? Etat.nyxessa.palierMissiles : Etat.nyxessa.palierBouclier;

        /// Achat du palier suivant de `a`, payé par la caisse commune, de jour. L'autorité (hôte, ou ce poste en solo)
        /// décide ; un client transmet sa demande à l'hôte. Renvoie le message à montrer à l'acheteur.
        public string Acheter(Amelioration a, int joueurId)
        {
            if (ClientReseau) { PartieReseau.Instance?.DemanderAchat((byte)a); return "Achat demandé à l’hôte…"; }
            string refus = RefusAchat(a, out int prix);
            if (refus != null) return refus;
            Etat.orEquipe -= prix;
            int palier = PalierDe(a) + 1;
            if (a == Amelioration.Missiles) Etat.nyxessa.palierMissiles = palier; else Etat.nyxessa.palierBouclier = palier;
            var j = Joueur(joueurId);
            string qui = j != null ? j.nom : "Joueur";
            Journal("Achat à la relique : " + NomAmelioration(a) + " palier " + palier + " (" + prix + " or, par " + qui + ")");
            SignalerPalier(a, palier, qui);
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.AnnoncerPalier((byte)a, palier, qui);
            return NomAmelioration(a) + " : palier " + palier + " acheté (" + prix + " or).";
        }

        /// Raison pour laquelle l'achat est impossible (null s'il est possible) et son prix.
        public string RefusAchat(Amelioration a, out int prix)
        {
            prix = B.PrixPalierSuivant(PalierDe(a));
            if (Etat.phase != Phase.Jour) return "Les achats se font de jour.";
            if (prix < 0) return "Palier maximal atteint.";
            if (Etat.orEquipe < prix) return "Pas assez d’or dans la caisse commune.";
            return null;
        }

        public static string NomAmelioration(Amelioration a) => a == Amelioration.Missiles ? "Missiles de Nyxessa" : "Bouclier du sorcier";

        /// Tous les postes : son et événement d'un palier acheté. Le bouclier a en plus son propre son (lot 2, § 8.2 :
        /// « palier du bouclier acheté, en plus de dl_nyxessa_palier »).
        public void SignalerPalier(Amelioration a, int palier, string qui)
        {
            if (nyxessa != null)
            {
                AudioBank.Jouer(SonsDuJeu.PalierAchete, nyxessa.transform.position + Vector3.up * 3f, 0.9f);
                if (a == Amelioration.Bouclier) AudioBank.Jouer(SonsDuJeu.BouclierPalier, nyxessa.transform.position + Vector3.up * 3f, 0.9f);
            }
            PalierAchete?.Invoke(a, palier, qui);
        }

        // ----------------------------------------------------------------- Joueurs

        public EtatJoueur Joueur(int id)
        {
            foreach (var j in Etat.joueurs) if (j.id == id) return j;
            return null;
        }

        public Heros HerosDe(int id) => m_Heros.TryGetValue(id, out var h) ? h : null;
        /// Tous les héros (local et distants). Parcourir par index (for) aux endroits appelés à chaque image : un foreach
        /// sur l'interface alloue son énumérateur.
        public IReadOnlyList<Heros> TousLesHeros => m_ListeHeros;

        /// Pose le héros d'un joueur (remplace l'ancien à la même place, comme le dictionnaire).
        void PoserHeros(int id, Heros h)
        {
            int i = m_Heros.TryGetValue(id, out var ancien) ? IndexHeros(ancien) : -1;
            m_Heros[id] = h;
            if (i >= 0) m_ListeHeros[i] = h; else m_ListeHeros.Add(h);
        }

        void RetirerHeros(int id)
        {
            if (!m_Heros.TryGetValue(id, out var h)) return;
            m_Heros.Remove(id);
            int i = IndexHeros(h);
            if (i >= 0) m_ListeHeros.RemoveAt(i);
        }

        /// Comparaison par référence : un héros détruit reste retrouvable (l'égalité Unity le confondrait avec null).
        int IndexHeros(Heros h)
        {
            for (int i = 0; i < m_ListeHeros.Count; i++) if (ReferenceEquals(m_ListeHeros[i], h)) return i;
            return -1;
        }

        void MettreAJourJoueurs(float dt)
        {
            foreach (var j in Etat.joueurs)
            {
                var h = HerosDe(j.id);
                if (h != null) h.EcrireEtat(j);
                if (!j.mort) continue;
                j.reapparitionRestante = Mathf.Max(0f, j.reapparitionRestante - dt);
                if (j.reapparitionRestante <= 0f && nyxessa != null && !nyxessa.Mort) Reapparaitre(j);
            }
        }

        /// Appelé par le héros à sa mort.
        public void SignalerMort(int joueurId)
        {
            var j = Joueur(joueurId);
            if (j == null || j.mort) return;
            // Client : l'hôte compte la mort et fixe le délai de réapparition.
            if (ClientReseau)
            {
                j.mort = true;
                m_MortSignalee = Time.time;
                HerosReseau.Local(HerosLocal)?.SignalerMort();
                JoueurMort?.Invoke(joueurId);
                return;
            }
            j.mort = true;
            // Wiki : chaque mort allonge le délai (8 s + 4 s par mort précédente dans la partie).
            j.reapparitionRestante = B.DelaiReapparition(j.score.morts);
            j.score.morts++;
            Journal("Joueur " + joueurId + " mort (" + j.score.morts + "e mort, réapparition dans " + j.reapparitionRestante.ToString("F0") + " s)");
            JoueurMort?.Invoke(joueurId);
        }

        void Reapparaitre(EtatJoueur j)
        {
            if (!j.mort) return;
            j.mort = false;
            j.reapparitionRestante = 0f;
            var h = HerosDe(j.id);
            if (h != null)
            {
                Vector3 point = PointReapparition(h.transform.position);
                // Héros d'un autre poste : son propriétaire le fait réapparaître (RPC) ; sinon ici.
                if (h.Distant) h.GetComponent<HerosReseau>()?.Reapparaitre(point);
                else if (ReseauJeu.EnPartie) ReapparaitreLocal(point);
                else h.Reapparaitre(point);
            }
            Journal("Joueur " + j.id + " réapparaît");
            JoueurReapparu?.Invoke(j.id);
        }

        /// Point de réapparition libre le plus proche de la position donnée (autour de Nyxessa).
        public Vector3 PointReapparition(Vector3 depuis)
        {
            if (pointsReapparition == null || pointsReapparition.Length == 0) return new Vector3(0f, 0f, -10f);
            Transform meilleur = null;
            float d = float.MaxValue;
            foreach (var t in pointsReapparition)
            {
                if (t == null) continue;
                bool occupe = Physics.CheckSphere(t.position + Vector3.up, 0.6f, ~0, QueryTriggerInteraction.Ignore);
                float dd = (t.position - depuis).sqrMagnitude + (occupe ? 10000f : 0f);
                if (dd < d) { d = dd; meilleur = t; }
            }
            return meilleur != null ? meilleur.position : Vector3.zero;
        }

        // ----------------------------------------------------------------- Score

        public void CompterDegats(int joueurId, float montant, bool critique)
        {
            var j = Joueur(joueurId);
            if (j == null) return;
            j.score.degatsInfliges += montant;
            if (critique) j.score.coupsCritiques++;
        }

        public void CompterTue(int joueurId)
        {
            var j = Joueur(joueurId);
            if (j != null) j.score.ennemisTues++;
        }

        public void CompterDegatsEvites(int joueurId, float montant)
        {
            var j = Joueur(joueurId);
            if (j != null && montant > 0f) j.score.degatsEvitesNyxessa += montant;
        }

        public void CompterSoins(int joueurId, float montant)
        {
            var j = Joueur(joueurId);
            if (j != null && montant > 0f) j.score.soinsProdigues += montant;
        }

        /// Or des vagues : versé à la caisse commune, compté dans « Or rapporté » du joueur qui a porté le coup fatal
        /// (joueurId 0 : Nyxessa, bouclier… : caisse seulement). Petite pièce qui monte et son d'or discret.
        public void GagnerOr(int montant, int joueurId, Vector3 point)
        {
            if (montant <= 0 || !EnCours) return;
            Etat.orEquipe += montant;
            var j = Joueur(joueurId);
            if (j != null) j.score.orRapporte += montant;
            PieceOr.Jouer(point);
            if (ReseauJeu.EnPartie) PartieReseau.Instance?.PieceOr(point);
            AudioBank.Jouer(SonsDuJeu.Or, point, 0.3f, 0.1f);
            OrGagne?.Invoke(montant, point);
        }

        /// Journal de partie (GameBalance.journal). Éditeur seulement : dans un build, l'appel disparaît à la compilation,
        /// avec la construction de sa chaîne chez l'appelant (Conditional), sans rien à régler dans l'asset.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void Journal(string texte)
        {
            if (B.journal) Debug.Log("[Partie " + Etat.duree.ToString("F1") + " s] " + texte);
        }
    }
}
