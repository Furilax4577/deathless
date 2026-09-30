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
    /// Le dernier code (ou la dernière adresse) utilisé est gardé : après une coupure, le joueur revient dans la partie en
    /// cours par le même code (Docs/reseau.md, « Coupures et retour en partie »).
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
            get { var s = SalonReseau.Instance; return s != null && s.IsSpawned ? s.Restant : 0f; }
        }
        public string Message => m_Message;
        NetworkManager NM => ReseauJeu.Instance != null ? ReseauJeu.Instance.Reseau : null;

        /// Dernier code de salon rejoint et dernière adresse IP rejointe (PlayerPrefs, par profil) : pré-remplis
        /// dans le lobby pour revenir dans une partie après une coupure.
        public string DernierCode => PlayerPrefs.GetString(Cle("code"), "");
        public string DerniereAdresse => PlayerPrefs.GetString(Cle("adresse"), "");
        static string Cle(string nom) => "deathless.dernier." + nom + (string.IsNullOrEmpty(ProfilServices) ? "" : "." + ProfilServices);
        static void Retenir(string nom, string valeur) { PlayerPrefs.SetString(Cle(nom), valeur ?? ""); PlayerPrefs.Save(); }

        void Awake()
        {
            DonneesUI.Lobby = this;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("-deathless-profil=")) ProfilServices = a.Substring("-deathless-profil=".Length);
                if (a == "-deathless-direct") ForcerDirect = true;
            }
        }

        void OnDestroy()
        {
            Suivre((SalonReseau)null);
            Suivre((ISession)null);
            if (ReferenceEquals(DonneesUI.Lobby, this)) DonneesUI.Lobby = null;
        }

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
                    ReseauJeu.Instance.PreparerConnexion();
                    var options = new SessionOptions { MaxPlayers = ReseauJeu.JoueursMax, IsPrivate = true }.WithRelayNetwork();
                    var s = await MultiplayerService.Instance.CreateSessionAsync(options);
                    m_Session = s;
                    m_Code = s.Code;
                    ReseauJeu.Journal("salon Relay créé : code " + m_Code);
                    Suivre(s);
                    ApresConnexion();
                    return;
                }
                catch (Exception e)
                {
                    ReseauJeu.Journal("création du salon Relay impossible : " + e.Message);
                    ReseauJeu.Instance.Arreter();
                    m_Message = "Services en ligne indisponibles : salon local, à rejoindre par adresse IP.";
                }
            }
            // Secours : salon direct sur le port 7777 (réseau local ou redirection de port).
            ReseauJeu.Instance.Transport.SetConnectionData("127.0.0.1", ReseauJeu.PortDirect, "0.0.0.0");
            ReseauJeu.Instance.PreparerConnexion();
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
            Retenir("code", c);
            // Retour après une coupure : ce joueur peut encore figurer dans l'ancienne session (départ non vu par le
            // service) ; il s'en retire avant de la rejoindre, sinon le service le refuse (« déjà membre »).
            await QuitterAnciennesSessions();
            try
            {
                ReseauJeu.Instance.PreparerConnexion();
                m_Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(c);
                m_Code = c;
                ReseauJeu.Journal("salon rejoint par code " + c);
                Suivre(m_Session);
                ApresConnexion();
            }
            catch (Exception e)
            {
                ReseauJeu.Journal("échec pour rejoindre " + c + " : " + e.Message);
                ReseauJeu.Instance.Arreter();
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
            Retenir("adresse", a);
            ReseauJeu.Instance.Transport.SetConnectionData(ip, port);
            ReseauJeu.Instance.PreparerConnexion();
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
            Suivre((ISession)null);
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

        /// Le lien avec l'hôte est perdu (client) : retour à l'écran d'entrée avec un message. En partie, ce que ce poste
        /// est seul à tenir (rangs et points de compétence) est gardé pour un retour par le même code, pré-rempli.
        public void SurDeconnexion(string raison)
        {
            // En partie : retour au menu (la scène est rechargée en solo), puis le message reste lisible dans le lobby.
            var partie = Deathless.Jeu.Partie.Instance;
            bool enPartie = partie != null && (partie.Etat.phase != Deathless.Jeu.Phase.Attente || ReseauJeu.EnPartie);
            string code = m_Code;
            if (enPartie && !m_Hote && partie.Etat.phase != Deathless.Jeu.Phase.Terminee)
            {
                partie.GarderPourRetour(code);
                if (!string.IsNullOrEmpty(code)) raison += code.Contains(":") ? " Tu peux revenir dans la partie par la même adresse." : " Tu peux revenir dans la partie avec le même code.";
            }
            if (enPartie) partie.QuitterPartie();
            else Quitter();
            m_Etat = EtatLobby.Erreur;
            m_Message = raison;
        }

        /// Hôte d'une partie dont le réseau vient de tomber (transport, session perdue) : la partie continue en solo
        /// (Partie.ContinuerSeul) ; le lobby revient à l'état hors salon et la session en ligne est fermée si elle
        /// répond encore.
        public async void SurPerteReseauHote()
        {
            var s = m_Session;
            m_Session = null;
            Suivre((ISession)null);
            m_Etat = EtatLobby.Aucun;
            m_Code = m_Message = "";
            m_Vue.Clear();
            if (s == null) return;
            try { await s.AsHost().DeleteAsync(); }
            catch (Exception e) { ReseauJeu.Journal("session perdue non fermée : " + e.Message); }
        }

        /// Hôte : un joueur a quitté la partie (Netcode) ; il est aussi retiré de la session en ligne, pour que sa place
        /// s'y libère et qu'il puisse la rejoindre à nouveau par le code.
        public async void RetirerDeLaSession(string joueurId)
        {
            var s = m_Session;
            if (s == null || string.IsNullOrEmpty(joueurId) || joueurId.StartsWith("local-") || !s.IsHost) return;
            bool membre = false;
            foreach (var j in s.Players) if (j.Id == joueurId) membre = true;
            if (!membre) return;
            try { await s.AsHost().RemovePlayerAsync(joueurId); ReseauJeu.Journal("session : joueur " + joueurId + " retiré"); }
            catch (Exception e) { ReseauJeu.Journal("session : retrait de " + joueurId + " impossible : " + e.Message); }
        }

        /// Client, avant de rejoindre par code : ce joueur se retire des sessions dont il est encore membre (départ
        /// brutal non vu par le service).
        async Task QuitterAnciennesSessions()
        {
            try
            {
                var ids = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
                if (ids == null) return;
                foreach (var id in ids)
                {
                    try
                    {
                        await Unity.Services.Lobbies.LobbyService.Instance.RemovePlayerAsync(id, AuthenticationService.Instance.PlayerId);
                        ReseauJeu.Journal("ancienne session quittée : " + id);
                    }
                    catch (Exception e) { ReseauJeu.Journal("ancienne session " + id + " : " + e.Message); }
                }
            }
            catch (Exception e) { ReseauJeu.Journal("sessions rejointes illisibles : " + e.Message); }
        }

        /// Session suivie (journal) : suppression, exclusion, changement d'état. Si elle entraîne l'arrêt de Netcode chez
        /// l'hôte d'une partie, ReseauJeu le traite comme une perte du réseau (la partie continue en solo).
        ISession m_SessionSuivie;

        void Suivre(ISession s)
        {
            if (ReferenceEquals(m_SessionSuivie, s)) return;
            if (m_SessionSuivie != null)
            {
                m_SessionSuivie.Deleted -= SessionSupprimee;
                m_SessionSuivie.RemovedFromSession -= SessionQuittee;
                m_SessionSuivie.StateChanged -= SessionEtat;
            }
            m_SessionSuivie = s;
            if (s != null)
            {
                s.Deleted += SessionSupprimee;
                s.RemovedFromSession += SessionQuittee;
                s.StateChanged += SessionEtat;
            }
        }

        void SessionSupprimee() => ReseauJeu.Journal("session supprimée");
        void SessionQuittee() => ReseauJeu.Journal("session : retiré de la session");
        void SessionEtat(SessionState e) => ReseauJeu.Journal("session : état " + e);

        void Erreur(string message)
        {
            m_Etat = EtatLobby.Erreur;
            m_Message = message;
            m_Vue.Clear();
            if (ReseauJeu.Instance != null) ReseauJeu.Instance.Arreter();
            ReseauJeu.Journal("erreur : " + message);
        }

        // ----------------------------------------------------------------- Vue (reconstruite sur changement)

        /// Salon dont on écoute la liste des joueurs, et vue à reconstruire : la vue (une Vue et deux chaînes par joueur)
        /// n'est refaite que quand la liste change (NetworkList.OnListChanged), à l'arrivée d'un nouveau salon ou au
        /// retour dans le salon ; avant, elle l'était à chaque image, partie comprise.
        SalonReseau m_SalonSuivi;
        bool m_VueAJour;

        void Suivre(SalonReseau s)
        {
            if (ReferenceEquals(m_SalonSuivi, s)) return;
            if (!ReferenceEquals(m_SalonSuivi, null) && m_SalonSuivi.Joueurs != null) m_SalonSuivi.Joueurs.OnListChanged -= JoueursChanges;
            m_SalonSuivi = s;
            if (!ReferenceEquals(s, null) && s.Joueurs != null) s.Joueurs.OnListChanged += JoueursChanges;
            m_VueAJour = false;
        }

        void JoueursChanges(NetworkListEvent<JoueurSalon> e) => m_VueAJour = false;

        void Update()
        {
            var s = SalonReseau.Instance;
            Suivre(s);
            if (m_Etat == EtatLobby.Connexion)
            {
                if (s != null && s.IsSpawned && Contient(s, ReseauJeu.IdLocal)) { m_Etat = EtatLobby.Salon; m_Message = ""; }
                else if (m_Attente >= 0f)
                {
                    m_Attente -= Time.unscaledDeltaTime;
                    if (m_Attente < 0f) Erreur("L’hôte ne répond pas.");
                }
            }
            if (s == null || !s.IsSpawned || m_Etat == EtatLobby.Aucun || m_Etat == EtatLobby.Erreur || m_Etat == EtatLobby.Connexion)
            {
                m_VueAJour = false;   // la vue a pu être vidée (Entrer, Quitter, Erreur) : refaite au retour dans le salon
                return;
            }
            if (!m_VueAJour)
            {
                m_VueAJour = true;
                m_Vue.Clear();
                foreach (var j in s.Joueurs)
                    m_Vue.Add(new Vue
                    {
                        Pseudo = j.pseudo.ToString(), ClasseId = j.classeId.ToString(), Pret = j.pret,
                        EstLocal = j.clientId == ReseauJeu.IdLocal, EstHote = j.clientId == NetworkManager.ServerClientId,
                    });
            }
            if (s.Lance.Value) m_Etat = EtatLobby.Lancement;
            else if (s.EnCompte) m_Etat = EtatLobby.CompteARebours;
            else m_Etat = EtatLobby.Salon;
        }

        static bool Contient(SalonReseau s, ulong id)
        {
            foreach (var j in s.Joueurs) if (j.clientId == id) return true;
            return false;
        }
    }
}
