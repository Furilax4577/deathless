using System;
using System.Collections.Generic;
using System.Linq;
using Deathless.Jeu;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deathless.Reseau
{
    /// Module réseau du jeu (Netcode for GameObjects + Unity Transport ; Relay et Lobby par Multiplayer Services) :
    /// un objet persistant (DontDestroyOnLoad) qui porte le NetworkManager, son transport, le lobby réel (LobbyReseau) et
    /// les règles de connexion (4 joueurs au plus, pas d'arrivée en cours de partie). Créé par Partie au premier chargement
    /// du village. En solo, rien ne démarre : le jeu tourne exactement comme avant.
    [DefaultExecutionOrder(-100)]
    public class ReseauJeu : MonoBehaviour
    {
        public static ReseauJeu Instance { get; private set; }

        /// Port de l'adresse IP directe (secours sans Relay). Voir Docs/reseau.md.
        public const ushort PortDirect = 7777;
        public const int JoueursMax = 4;

        public NetworkManager Reseau { get; private set; }
        public UnityTransport Transport { get; private set; }
        public LobbyReseau Lobby { get; private set; }

        /// Une partie réseau a été lancée depuis le salon (le village est chargé en mode réseau).
        public static bool EnPartie => Actif && SalonReseau.Instance != null && SalonReseau.Instance.Lance.Value;
        /// Joueurs de la partie (clientId, pseudo, classe) : la liste du salon, figée au lancement (plus de changement de
        /// classe ni d'arrivée ; un départ en retire le joueur).
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
            // Squelettes (étape 2), d'après le directeur des vagues du village.
            var dv = FindAnyObjectByType<DirecteurVagues>();
            if (dv != null)
                foreach (var pf in new[] { dv.prefabSbire, dv.prefabGuerrier, dv.prefabGolem, dv.prefabNecromancien })
                    if (pf != null && pf.GetComponent<NetworkObject>() != null) Reseau.AddNetworkPrefab(pf);
            var classes = ClassesJeu.Courant;
            if (classes != null) foreach (var c in classes.classes) if (c.prefab != null && c.prefab.GetComponent<NetworkObject>() != null) Reseau.AddNetworkPrefab(c.prefab);
            Reseau.ConnectionApprovalCallback = Approuver;
            Reseau.OnClientConnectedCallback += id => Journal("client connecté : " + id);
            Reseau.OnClientDisconnectCallback += OnDeconnexion;
            Reseau.OnServerStarted += () => Journal("hôte démarré");
            Reseau.OnTransportFailure += () => Journal("échec du transport");
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Approuver(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse rep)
        {
            int n = Reseau.ConnectedClientsIds.Count;
            bool lance = SalonReseau.Instance != null && SalonReseau.Instance.Lance.Value;
            rep.Approved = n < JoueursMax && !lance;
            rep.CreatePlayerObject = false;
            if (!rep.Approved) rep.Reason = lance ? "La partie a déjà commencé." : "Le salon est complet (4 joueurs).";
        }

        void OnDeconnexion(ulong id)
        {
            if (Reseau.IsServer)
            {
                if (id != Reseau.LocalClientId)
                {
                    Journal("client parti : " + id);
                    if (SalonReseau.Instance != null) SalonReseau.Instance.Retirer(id);
                    if (EnPartie && Partie.Instance != null) Partie.Instance.JoueurParti(id);
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

        // ----------------------------------------------------------------- Salon → partie

        /// Hôte : tous sont prêts, le compte à rebours est fini. Tous les postes chargent le village en mode réseau.
        /// Un seul lancement à la fois : un vote rebasculé pendant le chargement (Rejouer) rappelait LancerPartie, et le
        /// gestionnaire abonné deux fois faisait apparaître les héros en double.
        public void LancerPartie()
        {
            if (!Reseau.IsServer || m_Lancement) return;
            Journal("lancement de la partie : " + string.Join(", ", JoueursPartie.Select(j => j.pseudo + " (" + j.classeId + ")")));
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
