using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Deathless.UI.Donnees;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Deathless.Reseau
{
    /// Lobby réel (ILobby) : créer un salon (code court par Multiplayer Services, réseau Relay), le rejoindre par ce code,
    /// ou rejoindre un hôte par son adresse IP (Unity Transport direct, port 7777). Identification anonyme aux services au
    /// premier usage. Sans services (hors ligne, service refusé), l'hôte ouvre un salon direct sur son adresse locale.
    /// L'état du salon (pseudos, classes uniques, prêts, compte à rebours) vit dans SalonReseau, possédé par l'hôte.
    public class LobbyReseau : MonoBehaviour, ILobby
    {
        sealed class Vue : IJoueurLobby
        {
            public string Pseudo { get; set; }
            public string ClasseId { get; set; }
            public bool Pret { get; set; }
            public bool EstLocal { get; set; }
            public bool EstHote { get; set; }
        }

        EtatLobby m_Etat = EtatLobby.Aucun;
        string m_Code = "", m_Message = "";
        bool m_Hote;
        ISession m_Session;
        readonly List<IJoueurLobby> m_Vue = new List<IJoueurLobby>();
        bool m_ServicesPrets;

        /// Profil des services (tests : deux postes sur la même machine doivent avoir deux joueurs anonymes différents).
        public static string ProfilServices = "";
        /// Tests et secours : l'hôte ouvre un salon direct (adresse IP) sans essayer Relay.
        public static bool ForcerDirect;
        /// Tests (client automatique) : pseudo et classe imposés sans toucher au profil enregistré (PlayerPrefs).
        public static string PseudoForce, ClasseForcee;
        public static string PseudoLocal => !string.IsNullOrEmpty(PseudoForce) ? PseudoForce : DonneesUI.Profil.Pseudo;
        public static string ClassePreferee => !string.IsNullOrEmpty(ClasseForcee) ? ClasseForcee : ClassesJouables.DerniereJouee;

        public EtatLobby Etat => m_Etat;
        public string CodeSalon => m_Code;
        public bool EstHote => m_Hote;
        public IReadOnlyList<IJoueurLobby> Joueurs => m_Vue;
        public int JoueursMax => ReseauJeu.JoueursMax;
        public float CompteARebours
        {
            get { var s = SalonReseau.Instance; return s != null && s.CompteARebours.Value >= 0f ? s.CompteARebours.Value : 0f; }
        }
        public string Message => m_Message;
        NetworkManager NM => ReseauJeu.Instance != null ? ReseauJeu.Instance.Reseau : null;

        void Awake()
        {
            DonneesUI.Lobby = this;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("-deathless-profil=")) ProfilServices = a.Substring("-deathless-profil=".Length);
                if (a == "-deathless-direct") ForcerDirect = true;
            }
        }

        void OnDestroy() { if (ReferenceEquals(DonneesUI.Lobby, this)) DonneesUI.Lobby = null; }

        // ----------------------------------------------------------------- Services (Relay, Lobby)

        async Task<bool> Services()
        {
            if (m_ServicesPrets && AuthenticationService.Instance.IsSignedIn) return true;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    var opts = new InitializationOptions();
                    if (!string.IsNullOrEmpty(ProfilServices)) opts.SetProfile(ProfilServices);
                    await UnityServices.InitializeAsync(opts);
                }
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                m_ServicesPrets = true;
                ReseauJeu.Journal("services prêts (joueur anonyme " + AuthenticationService.Instance.PlayerId + ")");
                return true;
            }
            catch (Exception e)
            {
                ReseauJeu.Journal("services indisponibles : " + e.Message);
                return false;
            }
        }

        // ----------------------------------------------------------------- ILobby

        public async void CreerSalon()
        {
            if (m_Etat == EtatLobby.Connexion) return;
            Entrer(true, "Création du salon…");
            if (!ForcerDirect && await Services())
            {
                try
                {
                    var options = new SessionOptions { MaxPlayers = ReseauJeu.JoueursMax, IsPrivate = true }.WithRelayNetwork();
                    var s = await MultiplayerService.Instance.CreateSessionAsync(options);
                    m_Session = s;
                    m_Code = s.Code;
                    ReseauJeu.Journal("salon Relay créé : code " + m_Code);
                    ApresConnexion();
                    return;
                }
                catch (Exception e)
                {
                    ReseauJeu.Journal("création du salon Relay impossible : " + e.Message);
                    if (NM.IsListening) NM.Shutdown();
                    m_Message = "Services en ligne indisponibles : salon local, à rejoindre par adresse IP.";
                }
            }
            // Secours : salon direct sur le port 7777 (réseau local ou redirection de port).
            ReseauJeu.Instance.Transport.SetConnectionData("127.0.0.1", ReseauJeu.PortDirect, "0.0.0.0");
            if (!NM.StartHost()) { Erreur("Impossible d’ouvrir le salon (port " + ReseauJeu.PortDirect + " occupé ?)."); return; }
            m_Code = ReseauJeu.AdresseLocale() + ":" + ReseauJeu.PortDirect;
            ReseauJeu.Journal("salon direct ouvert : " + m_Code);
            ApresConnexion();
        }

        public async void Rejoindre(string code)
        {
            var c = (code ?? "").Trim().ToUpperInvariant();
            if (c.Length < 4) { Erreur("Saisis le code du salon (6 caractères)."); return; }
            if (m_Etat == EtatLobby.Connexion) return;
            Entrer(false, "Connexion au salon " + c + "…");
            if (!await Services()) { Erreur("Services en ligne indisponibles : rejoins par adresse IP."); return; }
            try
            {
                m_Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(c);
                m_Code = c;
                ReseauJeu.Journal("salon rejoint par code " + c);
                ApresConnexion();
            }
            catch (Exception e)
            {
                ReseauJeu.Journal("échec pour rejoindre " + c + " : " + e.Message);
                if (NM.IsListening) NM.Shutdown();
                Erreur("Salon introuvable ou complet (" + c + ").");
            }
        }

        public void RejoindreParAdresse(string adresse)
        {
            var a = (adresse ?? "").Trim();
            if (a.Length == 0) { Erreur("Saisis l’adresse IP de l’hôte."); return; }
            if (m_Etat == EtatLobby.Connexion) return;
            string ip = a; ushort port = ReseauJeu.PortDirect;
            int deux = a.LastIndexOf(':');
            if (deux > 0 && ushort.TryParse(a.Substring(deux + 1), out var p)) { ip = a.Substring(0, deux); port = p; }
            if (!System.Net.IPAddress.TryParse(ip, out _)) { Erreur("Adresse invalide : " + a); return; }
            Entrer(false, "Connexion à " + ip + ":" + port + "…");
            ReseauJeu.Instance.Transport.SetConnectionData(ip, port);
            if (!NM.StartClient()) { Erreur("Connexion impossible à " + a + "."); return; }
            m_Code = ip + ":" + port;
            ReseauJeu.Journal("connexion directe à " + m_Code);
            m_Attente = 12f;
        }

        float m_Attente = -1f;

        void Entrer(bool hote, string message)
        {
            m_Hote = hote;
            m_Code = "";
            m_Message = message;
            m_Etat = EtatLobby.Connexion;
            m_Vue.Clear();
        }

        /// Hôte : le salon (objet réseau) naît avec le serveur. Tous : on passe à l'écran du salon quand il est là.
        void ApresConnexion()
        {
            if (NM.IsServer && SalonReseau.Instance == null)
            {
                var prefab = Resources.Load<GameObject>("Reseau/SalonReseau");
                var go = Instantiate(prefab);
                go.GetComponent<NetworkObject>().Spawn(false);
            }
            m_Attente = 12f;
        }

        public bool ChoisirClasse(string classeId)
        {
            var s = SalonReseau.Instance;
            if (s == null || !ClassesJouables.Jouable(classeId)) return false;
            if (LobbyOutils.PrisePar(this, classeId) != null) return false;
            s.DemanderClasse(classeId);
            if (string.IsNullOrEmpty(ClasseForcee)) ClassesJouables.DerniereJouee = classeId;
            return true;
        }

        public void BasculerPret()
        {
            var s = SalonReseau.Instance;
            if (s != null) s.DemanderPret();
        }

        public void LancerMaintenant()
        {
            var s = SalonReseau.Instance;
            if (s != null && m_Hote) s.DemanderLancement();
        }

        public async void Quitter()
        {
            ReseauJeu.Journal("quitter le salon");
            var s = m_Session;
            m_Session = null;
            ReseauJeu.Instance.Arreter();
            m_Etat = EtatLobby.Aucun;
            m_Code = m_Message = "";
            m_Vue.Clear();
            if (s != null)
            {
                try { if (s.IsHost) await s.AsHost().DeleteAsync(); else await s.LeaveAsync(); }
                catch (Exception e) { ReseauJeu.Journal("fin de session : " + e.Message); }
            }
        }

        /// Le lien avec l'hôte est perdu (client) : retour à l'écran d'entrée avec un message.
        public void SurDeconnexion(string raison)
        {
            // En partie : retour au menu (la scène est rechargée en solo), puis le message reste lisible dans le lobby.
            var partie = Deathless.Jeu.Partie.Instance;
            if (partie != null && (partie.Etat.phase != Deathless.Jeu.Phase.Attente || ReseauJeu.EnPartie)) partie.QuitterPartie();
            else Quitter();
            m_Etat = EtatLobby.Erreur;
            m_Message = raison;
        }

        void Erreur(string message)
        {
            m_Etat = EtatLobby.Erreur;
            m_Message = message;
            m_Vue.Clear();
            if (NM != null && NM.IsListening) NM.Shutdown();
            ReseauJeu.Journal("erreur : " + message);
        }

        // ----------------------------------------------------------------- Vue (chaque image)

        void Update()
        {
            var s = SalonReseau.Instance;
            if (m_Etat == EtatLobby.Connexion)
            {
                if (s != null && s.IsSpawned && Contient(s, ReseauJeu.IdLocal)) { m_Etat = EtatLobby.Salon; m_Message = ""; }
                else if (m_Attente >= 0f)
                {
                    m_Attente -= Time.unscaledDeltaTime;
                    if (m_Attente < 0f) Erreur("L’hôte ne répond pas.");
                }
            }
            if (s == null || !s.IsSpawned || m_Etat == EtatLobby.Aucun || m_Etat == EtatLobby.Erreur || m_Etat == EtatLobby.Connexion) return;
            m_Vue.Clear();
            foreach (var j in s.Joueurs)
                m_Vue.Add(new Vue
                {
                    Pseudo = j.pseudo.ToString(), ClasseId = j.classeId.ToString(), Pret = j.pret,
                    EstLocal = j.clientId == ReseauJeu.IdLocal, EstHote = j.clientId == NetworkManager.ServerClientId,
                });
            if (s.Lance.Value) m_Etat = EtatLobby.Lancement;
            else if (s.CompteARebours.Value >= 0f) m_Etat = EtatLobby.CompteARebours;
            else m_Etat = EtatLobby.Salon;
        }

        static bool Contient(SalonReseau s, ulong id)
        {
            foreach (var j in s.Joueurs) if (j.clientId == id) return true;
            return false;
        }
    }
}
