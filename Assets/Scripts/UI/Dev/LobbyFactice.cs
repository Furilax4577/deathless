using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.UI.Dev
{
    /// Lobby factice (aucun réseau) : simule un salon de 1 à 3 autres joueurs qui arrivent, choisissent une classe et se
    /// déclarent prêts au bout de quelques secondes. Quand tous sont prêts : compte à rebours, puis partie solo avec la
    /// classe du joueur local (ClassesJouables.Lancer). Posé par EtatFactice (banc) ou créé par le menu principal quand
    /// DonneesUI.Lobby est vide (Village) ; sera remplacé par l'implémentation réseau.
    public class LobbyFactice : MonoBehaviour, ILobby
    {
        [Tooltip("Nombre d'autres joueurs simulés (1 à 3).")]
        [Range(1, 3)] public int autresJoueurs = 3;
        [Tooltip("Délai (s) entre deux arrivées, et avant qu'un joueur simulé passe prêt.")]
        public Vector2 delaiArrivee = new Vector2(0.8f, 1.8f), delaiPret = new Vector2(2f, 6f);
        [Tooltip("Durée du compte à rebours quand tous sont prêts (s).")]
        public float dureeCompte = 3f;
        [Tooltip("Durée de la « connexion » simulée (s).")]
        public float dureeConnexion = 0.8f;

        sealed class Joueur : IJoueurLobby
        {
            public string Pseudo { get; set; }
            public string ClasseId { get; set; }
            public bool Pret { get; set; }
            public bool EstLocal { get; set; }
            public bool EstHote { get; set; }
            public float pretDans = -1f;
        }

        static readonly string[] s_Pseudos = { "Morgane", "Tibo", "Lysa", "Kael" };

        readonly List<Joueur> m_Joueurs = new List<Joueur>();
        readonly List<IJoueurLobby> m_Vue = new List<IJoueurLobby>();
        EtatLobby m_Etat = EtatLobby.Aucun;
        float m_Chrono, m_ProchaineArrivee;
        int m_AArriver;
        bool m_Hote;
        string m_Code = "", m_Message = "";

        public EtatLobby Etat => m_Etat;
        public string CodeSalon => m_Code;
        public bool EstHote => m_Hote;
        public IReadOnlyList<IJoueurLobby> Joueurs => m_Vue;
        public int JoueursMax => 4;
        public float CompteARebours => m_Etat == EtatLobby.CompteARebours ? Mathf.Max(0f, m_Chrono) : 0f;
        public string Message => m_Message;

        /// Crée un lobby factice sur un objet persistant et l'enregistre dans DonneesUI.
        public static LobbyFactice Creer()
        {
            var go = new GameObject("LobbyFactice");
            DontDestroyOnLoad(go);
            var l = go.AddComponent<LobbyFactice>();
            DonneesUI.Lobby = l;
            return l;
        }

        void OnDestroy() { if (ReferenceEquals(DonneesUI.Lobby, this)) DonneesUI.Lobby = null; }

        public void CreerSalon()
        {
            Entrer(true, "");
        }

        public void Rejoindre(string code)
        {
            var c = (code ?? "").Trim().ToUpperInvariant();
            if (c.Length < 4)
            {
                m_Etat = EtatLobby.Erreur;
                m_Message = "Saisis le code du salon (6 caractères).";
                return;
            }
            Entrer(false, c);
        }

        public void RejoindreParAdresse(string adresse)
        {
            var a = (adresse ?? "").Trim();
            if (a.Length == 0)
            {
                m_Etat = EtatLobby.Erreur;
                m_Message = "Saisis l’adresse IP de l’hôte.";
                return;
            }
            Entrer(false, a);
        }

        void Entrer(bool hote, string code)
        {
            m_Hote = hote;
            m_Code = hote ? CodeAuHasard() : code;
            m_Message = hote ? "" : "Connexion à " + m_Code + "…";
            m_Joueurs.Clear();
            m_Etat = EtatLobby.Connexion;
            m_Chrono = dureeConnexion;
            var local = new Joueur { Pseudo = DonneesUI.Profil.Pseudo, EstLocal = true, EstHote = hote };
            if (!hote)
            {
                // Rejoindre : l'hôte est déjà là (avec sa classe).
                m_Joueurs.Add(Simule(0, true));
            }
            // Classe du joueur local : la dernière jouée si elle est libre, sinon une classe libre.
            local.ClasseId = ClasseLibre(ClassesJouables.Derniere != null ? ClassesJouables.Derniere.Id : ClassesJouables.ParDefaut);
            m_Joueurs.Add(local);
            m_AArriver = Mathf.Clamp(autresJoueurs, 1, 3) - (hote ? 0 : 1);
            m_ProchaineArrivee = Random.Range(delaiArrivee.x, delaiArrivee.y) + dureeConnexion;
            MajVue();
        }

        Joueur Simule(int index, bool hote)
        {
            return new Joueur
            {
                Pseudo = s_Pseudos[index % s_Pseudos.Length],
                ClasseId = ClasseLibre(null),
                EstHote = hote,
                pretDans = Random.Range(delaiPret.x, delaiPret.y),
            };
        }

        static string CodeAuHasard()
        {
            const string lettres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var s = "";
            for (var i = 0; i < 6; i++) s += lettres[Random.Range(0, lettres.Length)];
            return s;
        }

        Joueur Local
        {
            get
            {
                foreach (var j in m_Joueurs) if (j.EstLocal) return j;
                return null;
            }
        }

        public bool ChoisirClasse(string classeId)
        {
            var l = Local;
            if (l == null || !ClassesJouables.Jouable(classeId)) return false;
            // Classe unique dans le salon : premier arrivé, premier servi.
            if (LobbyOutils.PrisePar(this, classeId) != null) return false;
            l.ClasseId = classeId;
            l.Pret = false;
            return true;
        }

        bool Prise(string classeId)
        {
            foreach (var j in m_Joueurs) if (j.ClasseId == classeId) return true;
            return false;
        }

        /// Première classe jouable libre (en commençant par `preferee`).
        string ClasseLibre(string preferee)
        {
            if (!string.IsNullOrEmpty(preferee) && ClassesJouables.Jouable(preferee) && !Prise(preferee)) return preferee;
            var libres = new List<string>();
            foreach (var c in ClassesJouables.Catalogue) if (!c.Verrouillee && !Prise(c.Id)) libres.Add(c.Id);
            return libres.Count > 0 ? libres[Random.Range(0, libres.Count)] : null;
        }

        /// Tests (EtatFactice) : le premier autre joueur prend la classe `classeId` (si le joueur local ne l'a pas).
        public bool SimulerPrise(string classeId)
        {
            foreach (var j in m_Joueurs)
            {
                if (j.EstLocal) continue;
                if (Prise(classeId) && j.ClasseId != classeId) return false;
                j.ClasseId = classeId;
                return true;
            }
            return false;
        }

        public void BasculerPret()
        {
            var l = Local;
            if (l == null || string.IsNullOrEmpty(l.ClasseId)) return;
            if (m_Etat != EtatLobby.Salon && m_Etat != EtatLobby.CompteARebours) return;
            l.Pret = !l.Pret;
        }

        public void LancerMaintenant()
        {
            if (m_Hote && TousPrets() && (m_Etat == EtatLobby.Salon || m_Etat == EtatLobby.CompteARebours)) Lancer();
        }

        public void Quitter()
        {
            m_Joueurs.Clear();
            MajVue();
            m_Etat = EtatLobby.Aucun;
            m_Code = "";
            m_Message = "";
        }

        bool TousPrets()
        {
            if (m_Joueurs.Count == 0) return false;
            foreach (var j in m_Joueurs) if (!j.Pret) return false;
            return true;
        }

        void Lancer()
        {
            m_Etat = EtatLobby.Lancement;
            var l = Local;
            var classe = l != null ? l.ClasseId : ClassesJouables.ParDefaut;
            // Pas de réseau : la partie démarre en solo avec la classe du joueur local.
            ClassesJouables.Lancer(classe);
            Quitter();
        }

        void MajVue()
        {
            m_Vue.Clear();
            foreach (var j in m_Joueurs) m_Vue.Add(j);
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            switch (m_Etat)
            {
                case EtatLobby.Connexion:
                    m_Chrono -= dt;
                    if (m_Chrono <= 0f) { m_Etat = EtatLobby.Salon; m_Message = ""; }
                    break;
                case EtatLobby.Salon:
                case EtatLobby.CompteARebours:
                    // Arrivées et prêts des joueurs simulés.
                    if (m_AArriver > 0 && m_Joueurs.Count < JoueursMax)
                    {
                        m_ProchaineArrivee -= dt;
                        if (m_ProchaineArrivee <= 0f)
                        {
                            m_Joueurs.Add(Simule(m_Joueurs.Count, false));
                            m_AArriver--;
                            m_ProchaineArrivee = Random.Range(delaiArrivee.x, delaiArrivee.y);
                            MajVue();
                        }
                    }
                    foreach (var j in m_Joueurs)
                    {
                        if (j.EstLocal || j.Pret || j.pretDans < 0f) continue;
                        j.pretDans -= dt;
                        if (j.pretDans <= 0f) j.Pret = true;
                    }
                    var tous = TousPrets() && m_AArriver == 0;
                    if (m_Etat == EtatLobby.Salon && tous) { m_Etat = EtatLobby.CompteARebours; m_Chrono = dureeCompte; }
                    else if (m_Etat == EtatLobby.CompteARebours && !tous) m_Etat = EtatLobby.Salon;
                    else if (m_Etat == EtatLobby.CompteARebours)
                    {
                        m_Chrono -= dt;
                        if (m_Chrono <= 0f) Lancer();
                    }
                    break;
            }
        }

        /// Tests : tous les joueurs simulés passent prêts tout de suite (et arrivent s'ils manquaient).
        public void ForcerAutresPrets()
        {
            while (m_AArriver > 0 && m_Joueurs.Count < JoueursMax) { m_Joueurs.Add(Simule(m_Joueurs.Count, false)); m_AArriver--; }
            foreach (var j in m_Joueurs) if (!j.EstLocal) j.Pret = true;
            MajVue();
        }

        /// Tests : fige ou relance la simulation (captures).
        public void Figer(bool fige) => enabled = !fige;
    }
}
