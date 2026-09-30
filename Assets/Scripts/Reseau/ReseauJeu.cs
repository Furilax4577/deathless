using System;
using System.Collections.Generic;
using System.Linq;
using Deathless.Jeu;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deathless.Reseau
{
    /// Module réseau du jeu (Netcode for GameObjects + Unity Transport ; Relay et Lobby par Multiplayer Services) :
    /// un objet persistant (DontDestroyOnLoad) qui porte le NetworkManager, son transport, le lobby réel (LobbyReseau) et
    /// les règles de connexion (4 joueurs au plus ; en cours de partie, retour d'un joueur parti et arrivée d'un nouveau
    /// s'il reste place et classe). Perte du réseau : l'hôte continue seul, un client revient au lobby (Docs/reseau.md).
    /// Créé par Partie au premier chargement du village. En solo, rien ne démarre : le jeu tourne exactement comme avant.
    [DefaultExecutionOrder(-100)]
    public class ReseauJeu : MonoBehaviour
    {
        public static ReseauJeu Instance { get; private set; }

        /// Port de l'adresse IP directe (secours sans Relay). Voir Docs/reseau.md.
        public const ushort PortDirect = 7777;
        public const int JoueursMax = 4;
        /// Délai de détection d'une coupure (ms) : un poste sans nouvelles de l'autre (battements du transport toutes les
        /// 0,5 s) pendant ce temps est déconnecté. 10 s au lieu des 30 s par défaut d'UnityTransport (décision du
        /// 27/09/2026) ; vaut aussi par Relay (même transport).
        public const int DelaiCoupureMs = 10000;

        public NetworkManager Reseau { get; private set; }
        public UnityTransport Transport { get; private set; }
        public LobbyReseau Lobby { get; private set; }

        /// Une partie réseau a été lancée depuis le salon (le village est chargé en mode réseau).
        public static bool EnPartie => Actif && SalonReseau.Instance != null && SalonReseau.Instance.Lance.Value;
        /// Joueurs de la partie (clientId, pseudo, classe) : la liste du salon, figée au lancement (plus de changement de
        /// classe ; un départ en retire le joueur, un retour ou une arrivée en cours de partie l'y ajoute).
        public static IEnumerable<JoueurSalon> JoueursPartie
        {
            get { if (SalonReseau.Instance != null) foreach (var j in SalonReseau.Instance.Joueurs) yield return j; }
        }

        /// Réseau actif (hôte ou client connecté).
        public static bool Actif => Instance != null && Instance.Reseau != null && Instance.Reseau.IsListening;
        /// Ce poste fait autorité sur le monde : solo, ou hôte d'une partie réseau.
        public static bool Autorite => !Actif || Instance.Reseau.IsServer;
        public static ulong IdLocal => Actif ? Instance.Reseau.LocalClientId : 0;

        public static void Journal(string t) => Debug.Log("[Réseau] " + t);

        /// Crée le module s'il n'existe pas (appelé par Partie).
        public static ReseauJeu Assurer()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Reseau");
            DontDestroyOnLoad(go);
            go.SetActive(false);
            var r = go.AddComponent<ReseauJeu>();
            r.Transport = go.AddComponent<UnityTransport>();
            r.Transport.DisconnectTimeoutMS = DelaiCoupureMs;
            r.Reseau = go.AddComponent<NetworkManager>();
            r.Reseau.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = r.Transport,
                ConnectionApproval = true,
                EnableSceneManagement = true,
                TickRate = 30,
                ClientConnectionBufferTimeout = 15,
            };
            r.Lobby = go.AddComponent<LobbyReseau>();
            go.SetActive(true);
            return r;
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // Préfabs réseau : le salon et les héros des classes (mêmes listes sur tous les postes).
            var salon = Resources.Load<GameObject>("Reseau/SalonReseau");
            if (salon != null) Reseau.AddNetworkPrefab(salon);
            var monde = Resources.Load<GameObject>("Reseau/PartieReseau");
            if (monde != null) Reseau.AddNetworkPrefab(monde);
            // Squelettes (étape 2) et les deux variantes de Morgrim, d'après le directeur des vagues du village.
            var dv = FindAnyObjectByType<DirecteurVagues>();
            if (dv != null)
                foreach (var pf in new[] { dv.prefabSbire, dv.prefabGuerrier, dv.prefabGolem, dv.prefabNecromancien, dv.prefabMorgrimMassue, dv.prefabMorgrimMartache })
                    if (pf != null && pf.GetComponent<NetworkObject>() != null) Reseau.AddNetworkPrefab(pf);
            var classes = ClassesJeu.Courant;
            if (classes != null) foreach (var c in classes.classes) if (c.prefab != null && c.prefab.GetComponent<NetworkObject>() != null) Reseau.AddNetworkPrefab(c.prefab);
            Reseau.ConnectionApprovalCallback = Approuver;
            Reseau.OnClientConnectedCallback += OnConnexion;
            Reseau.OnClientDisconnectCallback += OnDeconnexion;
            Reseau.OnServerStarted += () => { Journal("hôte démarré"); m_ArretVoulu = false; };
            Reseau.OnClientStarted += () => m_ArretVoulu = false;
            Reseau.OnTransportFailure += () => Journal("échec du transport");
            Reseau.OnPreShutdown += AvantArret;
            Reseau.OnClientStopped += ApresArret;
            Reseau.OnServerStopped += ApresArret;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            // Hôte dont le réseau est tombé en partie : une image après l'arrêt de Netcode (les objets réseau détruits
            // avec lui ont alors disparu), la partie reprend en solo.
            if (m_RepriseSolo >= 0 && Time.frameCount > m_RepriseSolo)
            {
                m_RepriseSolo = -1;
                if (Partie.Instance != null) Partie.Instance.ContinuerSeul();
            }
        }

        // ----------------------------------------------------------------- Identité des postes (retour en cours de partie)

        /// Identité stable de ce poste, envoyée à l'hôte dans la demande de connexion (NetworkConfig.ConnectionData) :
        /// le joueur anonyme des services (AuthenticationService.PlayerId) quand ils sont prêts, sinon un identifiant tiré
        /// une fois et gardé dans les PlayerPrefs (par profil : deux postes de test sur une machine restent distincts).
        /// C'est par elle que l'hôte reconnaît un joueur qui revient dans la partie après une coupure.
        public static string IdentiteLocale()
        {
            try
            {
                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized
                    && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                    return Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            }
            catch (Exception) { }
            string cle = "deathless.identite" + (string.IsNullOrEmpty(LobbyReseau.ProfilServices) ? "" : "." + LobbyReseau.ProfilServices);
            string id = PlayerPrefs.GetString(cle, "");
            if (string.IsNullOrEmpty(id)) { id = "local-" + Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(cle, id); PlayerPrefs.Save(); }
            return id;
        }

        /// Avant de démarrer (hôte ou client) : l'identité part avec la demande de connexion.
        public void PreparerConnexion()
        {
            m_ArretVoulu = false;
            Reseau.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(IdentiteLocale());
        }

        /// Hôte : identité de chaque client connecté (payload de la demande de connexion).
        readonly Dictionary<ulong, string> m_Identites = new Dictionary<ulong, string>();

        /// Hôte : joueur parti en cours de partie, gardé pour son retour (même identité) : sa place et sa classe dans le
        /// salon, son état (score, or porté) dans Etat.joueurs.
        sealed class Absent { public JoueurSalon salon; public EtatJoueur etat; }
        readonly Dictionary<string, Absent> m_Absents = new Dictionary<string, Absent>();
        /// Hôte : arrivants en cours de partie entre leur présentation (salon) et l'apparition de leur héros.
        readonly Dictionary<ulong, Absent> m_Retours = new Dictionary<ulong, Absent>();
        /// Hôte : clients synchronisés (scène et objets) en cours de partie, dont le héros reste à faire apparaître.
        readonly HashSet<ulong> m_Synchronises = new HashSet<ulong>();

        public string IdentiteDe(ulong clientId) => m_Identites.TryGetValue(clientId, out var id) ? id : "";

        /// Joueurs partis en cours de partie et attendus (tests, journal).
        public int Absents => m_Absents.Count;

        void Approuver(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse rep)
        {
            string ident = req.Payload != null && req.Payload.Length > 0 ? System.Text.Encoding.UTF8.GetString(req.Payload) : "";
            if (ident.Length > 128) ident = ident.Substring(0, 128);
            int n = Reseau.ConnectedClientsIds.Count;
            bool lance = SalonReseau.Instance != null && SalonReseau.Instance.Lance.Value;
            rep.CreatePlayerObject = false;
            string refus = !lance ? (n < JoueursMax ? null : "Le salon est complet (4 joueurs).") : RefusEnCours(ident, n);
            rep.Approved = refus == null;
            if (!rep.Approved) { rep.Reason = refus; Journal("connexion refusée : " + refus); return; }
            m_Identites[req.ClientNetworkId] = ident;
        }

        /// Hôte, partie lancée : un joueur parti revient (même identité) tant que la partie est en cours ; un nouveau
        /// joueur entre s'il reste une place (absents compris) et une classe libre. Sinon la raison du refus.
        string RefusEnCours(string ident, int connectes)
        {
            var p = Partie.Instance;
            if (m_Lancement || p == null || !p.EnCours) return "La partie a déjà commencé.";
            if (ident.Length > 0)
            {
                if (m_Absents.ContainsKey(ident)) return connectes < JoueursMax ? null : "Le salon est complet (4 joueurs).";
                // Retour avant que l'hôte ait vu tomber l'ancienne connexion (délai du transport) : elle est fermée, le
                // joueur passe par les absents et revient aussitôt.
                ulong ancienne = ulong.MaxValue;
                foreach (var kv in m_Identites)
                    if (kv.Value == ident && kv.Key != NetworkManager.ServerClientId && Reseau.ConnectedClients.ContainsKey(kv.Key)) ancienne = kv.Key;
                if (ancienne != ulong.MaxValue)
                {
                    Journal("retour de " + ident + " : ancienne connexion " + ancienne + " fermée");
                    Reseau.DisconnectClient(ancienne, "Reconnecté depuis un autre poste.");
                    return null;
                }
            }
            if (connectes + m_Absents.Count >= JoueursMax) return "Le salon est complet (4 joueurs).";
            if (string.IsNullOrEmpty(ClasseLibreEnCours(null))) return "Plus de classe libre dans cette partie.";
            return null;
        }

        /// Classe libre pour un arrivant en cours de partie : ni prise dans le salon, ni gardée pour un absent.
        string ClasseLibreEnCours(string preferee)
        {
            bool Prise(string c)
            {
                if (SalonReseau.Instance != null) foreach (var j in SalonReseau.Instance.Joueurs) if (j.classeId.ToString() == c) return true;
                foreach (var a in m_Absents.Values) if (a.salon.classeId.ToString() == c) return true;
                return false;
            }
            if (!string.IsNullOrEmpty(preferee) && Deathless.UI.Donnees.ClassesJouables.Jouable(preferee) && !Prise(preferee)) return preferee;
            foreach (var c in Deathless.UI.Donnees.ClassesJouables.Catalogue) if (!c.Verrouillee && !Prise(c.Id)) return c.Id;
            return "";
        }

        /// Hôte (SalonReseau.PresenterRpc, partie lancée) : un client arrivé en cours de partie se présente. Il retrouve sa
        /// place (classe, état) s'il revient, sinon il prend une classe libre ; son héros apparaît quand il a fini de
        /// se synchroniser (OnConnexion).
        public void PresentationEnCours(ulong clientId, string pseudo, string classePreferee)
        {
            var s = SalonReseau.Instance;
            if (s == null || !Reseau.IsServer) return;
            foreach (var j in s.Joueurs) if (j.clientId == clientId) return;
            string ident = IdentiteDe(clientId);
            Absent a = null;
            if (ident.Length > 0 && m_Absents.TryGetValue(ident, out a)) m_Absents.Remove(ident);
            string classe = a != null ? a.salon.classeId.ToString() : ClasseLibreEnCours(classePreferee);
            if (string.IsNullOrEmpty(classe))
            {
                Reseau.DisconnectClient(clientId, "Plus de classe libre dans cette partie.");
                return;
            }
            s.Joueurs.Add(new JoueurSalon { clientId = clientId, pseudo = new FixedString64Bytes(pseudo ?? ""), classeId = new FixedString32Bytes(classe), pret = true });
            m_Retours[clientId] = a ?? new Absent();
            Journal((a != null ? "retour de " : "arrivée en cours de partie : ") + pseudo + " (client " + clientId + ", " + classe + ")");
            EssayerArrivee(clientId);
        }

        void OnConnexion(ulong id)
        {
            Journal("client connecté : " + id);
            if (!Reseau.IsServer || id == NetworkManager.ServerClientId || !EnPartie) return;
            m_Synchronises.Add(id);
            EssayerArrivee(id);
        }

        /// Hôte : le client s'est présenté et a fini de se synchroniser : son héros apparaît près de Nyxessa.
        void EssayerArrivee(ulong id)
        {
            if (!m_Synchronises.Contains(id) || !m_Retours.TryGetValue(id, out var a)) return;
            JoueurSalon js = default; bool trouve = false;
            if (SalonReseau.Instance != null) foreach (var j in SalonReseau.Instance.Joueurs) if (j.clientId == id) { js = j; trouve = true; }
            if (!trouve) return;
            m_Synchronises.Remove(id);
            m_Retours.Remove(id);
            if (Partie.Instance != null) Partie.Instance.ArriveeEnCours(js, a.etat);
        }

        void OnDeconnexion(ulong id)
        {
            if (Reseau.IsServer)
            {
                if (id != Reseau.LocalClientId)
                {
                    Journal("client parti : " + id);
                    string ident = IdentiteDe(id);
                    m_Identites.Remove(id);
                    m_Synchronises.Remove(id);
                    m_Retours.Remove(id);
                    bool enPartie = EnPartie;
                    JoueurSalon js = default;
                    bool present = SalonReseau.Instance != null && SalonReseau.Instance.Retirer(id, out js);
                    EtatJoueur etat = enPartie && Partie.Instance != null ? Partie.Instance.JoueurParti(id) : null;
                    // Partie en cours : sa place, sa classe et son état sont gardés pour son retour (même identité).
                    if (enPartie && present && ident.Length > 0)
                    {
                        m_Absents[ident] = new Absent { salon = js, etat = etat };
                        Journal("place gardée pour " + js.pseudo + " (" + js.classeId + ")");
                    }
                    Lobby.RetirerDeLaSession(ident);
                }
            }
            else if (id == Reseau.LocalClientId || id == NetworkManager.ServerClientId)
            {
                string raison = Reseau.DisconnectReason;
                Journal("déconnecté de l'hôte" + (string.IsNullOrEmpty(raison) ? "" : " : " + raison));
                Lobby.SurDeconnexion(Traduire(raison));
            }
        }

        /// Raison de déconnexion affichée au joueur (celles de Netcode sont en anglais ; les nôtres, d'Approuver, en français).
        static string Traduire(string raison)
        {
            if (string.IsNullOrEmpty(raison)) return "Connexion à l’hôte perdue.";
            if (raison.Contains("host shutting down") || raison.Contains("shutdown")) return "L’hôte a fermé le salon.";
            if (raison.Contains("timed out") || raison.Contains("Timeout")) return "L’hôte ne répond plus.";
            if (raison.StartsWith("[") || raison.Contains("Disconnect")) return "Connexion à l’hôte perdue.";
            return raison;
        }

        // ----------------------------------------------------------------- Arrêt du réseau (voulu ou non)

        /// Arrêt demandé par le jeu (quitter le salon ou la partie, erreur de connexion) : pas une perte.
        bool m_ArretVoulu;
        bool m_HoteAvantArret, m_EnPartieAvantArret, m_ArretTraite;
        int m_RepriseSolo = -1;

        /// Juste avant que Netcode ne détruise ses objets (NetworkManager.OnPreShutdown) : si l'arrêt n'est pas voulu et
        /// que ce poste héberge une partie, ce qui va disparaître est noté (héros local, squelettes vivants).
        void AvantArret()
        {
            m_ArretTraite = false;
            m_HoteAvantArret = Reseau.IsServer;
            m_EnPartieAvantArret = EnPartie && Partie.Instance != null && Partie.Instance.Etat.phase != Phase.Attente;
            if (m_ArretVoulu) return;
            Journal("arrêt du réseau non voulu (" + (m_HoteAvantArret ? "hôte" : "client") + (m_EnPartieAvantArret ? ", en partie" : "") + ")");
            if (m_HoteAvantArret && m_EnPartieAvantArret) Partie.Instance.NoterAvantPerteReseau();
        }

        /// Netcode arrêté (OnClientStopped / OnServerStopped ; l'hôte reçoit les deux). Arrêt non voulu : l'hôte d'une
        /// partie la continue seul (reprise à l'image suivante) ; un client, ou un hôte encore au salon, revient à
        /// l'écran d'entrée du lobby avec un message.
        void ApresArret(bool _)
        {
            if (m_ArretTraite) return;
            m_ArretTraite = true;
            m_Lancement = false;
            m_Identites.Clear(); m_Absents.Clear(); m_Retours.Clear(); m_Synchronises.Clear();
            if (m_ArretVoulu) return;
            if (m_HoteAvantArret && m_EnPartieAvantArret)
            {
                Journal("hôte : réseau perdu, la partie continue en solo");
                m_RepriseSolo = Time.frameCount;
                Lobby.SurPerteReseauHote();
            }
            else Lobby.SurDeconnexion(m_HoteAvantArret ? "Connexion au réseau perdue : le salon est fermé." : Traduire(Reseau.DisconnectReason));
        }

        // ----------------------------------------------------------------- Salon → partie

        /// Hôte : tous sont prêts, le compte à rebours est fini. Tous les postes chargent le village en mode réseau.
        /// Un seul lancement à la fois : un vote rebasculé pendant le chargement (Rejouer) rappelait LancerPartie, et le
        /// gestionnaire abonné deux fois faisait apparaître les héros en double.
        public void LancerPartie()
        {
            if (!Reseau.IsServer || m_Lancement) return;
            Journal("lancement de la partie : " + string.Join(", ", JoueursPartie.Select(j => j.pseudo + " (" + j.classeId + ")")));
            // Nouvelle partie (lancement ou Rejouer) : les places gardées de la précédente sont oubliées.
            m_Absents.Clear(); m_Retours.Clear(); m_Synchronises.Clear();
            Reseau.SceneManager.OnLoadEventCompleted += ChargementTermine;
            var statut = Reseau.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            if (statut != SceneEventProgressStatus.Started)
            {
                Reseau.SceneManager.OnLoadEventCompleted -= ChargementTermine;
                Journal("lancement refusé : " + statut);
                return;
            }
            m_Lancement = true;
        }

        bool m_Lancement;

        void ChargementTermine(string scene, LoadSceneMode mode, List<ulong> ok, List<ulong> expires)
        {
            Reseau.SceneManager.OnLoadEventCompleted -= ChargementTermine;
            m_Lancement = false;
            Journal("village chargé par " + ok.Count + " poste(s)" + (expires.Count > 0 ? ", " + expires.Count + " en retard" : ""));
            if (Partie.Instance != null) Partie.Instance.ApparaitreHerosReseau();
        }

        /// Fin du réseau (quitter le salon ou la partie) : arrêt du NetworkManager, partie réseau oubliée.
        public void Arreter()
        {
            m_ArretVoulu = true;
            m_Lancement = false;
            if (Reseau != null && Reseau.IsListening) Reseau.Shutdown();
        }

        /// Adresse IPv4 locale (affichée comme « code » d'un salon direct).
        public static string AdresseLocale()
        {
            try
            {
                foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip)) return ip.ToString();
            }
            catch (Exception) { }
            return "127.0.0.1";
        }
    }
}
