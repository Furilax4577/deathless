using System.Collections.Generic;

namespace Deathless.UI.Donnees
{
    /// État du lobby multijoueur.
    public enum EtatLobby
    {
        /// Hors salon : écran d'entrée (Créer un salon, Rejoindre).
        Aucun,
        /// Création ou connexion en cours (« Connexion… »).
        Connexion,
        /// Dans un salon : choix de classe, prêt / pas prêt.
        Salon,
        /// Tous prêts : compte à rebours (CompteARebours) avant le lancement.
        CompteARebours,
        /// La partie démarre (l'écran de jeu va remplacer le lobby).
        Lancement,
        /// Échec (salon introuvable, complet…) : Message dit pourquoi ; retour à l'écran d'entrée.
        Erreur,
    }

    /// Un joueur du salon (un emplacement sur quatre).
    public interface IJoueurLobby
    {
        string Pseudo { get; }
        /// Classe choisie (Id du catalogue ClassesJouables), null ou vide si pas encore choisie.
        string ClasseId { get; }
        bool Pret { get; }
        bool EstLocal { get; }
        bool EstHote { get; }
    }

    /// Lobby multijoueur vu par l'interface (Wiki : interface.md). Quatre joueurs au plus ; chacun choisit sa classe puis
    /// se déclare prêt ; quand tous sont prêts, un court compte à rebours démarre puis la partie se lance. Transport
    /// retenu : Unity Relay et Lobby (Unity Gaming Services) : l'hôte reçoit un code court que les amis saisissent ;
    /// l'adresse IP directe reste un secours. Aucune implémentation réseau ici : `LobbyFactice` (Deathless.UI.Dev) simule.
    /// Le jeu (ou le futur module réseau) enregistre la sienne dans DonneesUI.Lobby.
    public static class LobbyOutils
    {
        /// Pseudo de l'autre joueur (pas le joueur local) qui a déjà la classe `classeId`, ou null si elle est libre.
        public static string PrisePar(ILobby lobby, string classeId)
        {
            if (lobby == null || string.IsNullOrEmpty(classeId)) return null;
            foreach (var j in lobby.Joueurs) if (!j.EstLocal && j.ClasseId == classeId) return j.Pseudo;
            return null;
        }
    }

    public interface ILobby
    {
        EtatLobby Etat { get; }

        /// Code court du salon (environ 6 caractères ; affiché et copiable par l'hôte, saisi par ceux qui rejoignent).
        /// Vide hors salon.
        string CodeSalon { get; }

        /// Vrai si le joueur local a créé le salon.
        bool EstHote { get; }

        /// Joueurs présents, dans l'ordre des emplacements (1 à JoueursMax).
        IReadOnlyList<IJoueurLobby> Joueurs { get; }
        int JoueursMax { get; }

        /// Secondes restantes avant le lancement (état CompteARebours).
        float CompteARebours { get; }

        /// Information ou erreur à afficher (peut être vide).
        string Message { get; }

        /// Crée un salon dont le joueur local est l'hôte.
        void CreerSalon();

        /// Rejoint un salon par son code court (Unity Lobby).
        void Rejoindre(string code);

        /// Secours : rejoint un hôte par son adresse IP (et son port éventuel, « 192.168.1.20:7777 »).
        void RejoindreParAdresse(string adresse);

        /// Classe du joueur local (Id du catalogue, classe jouable). Repasse le joueur « pas prêt ».
        /// **Chaque classe est unique dans un salon** (décision de Quentin) : renvoie faux si un autre joueur l'a déjà ;
        /// si deux joueurs la demandent en même temps, le premier arrivé l'obtient (arbitré par l'hôte / le service).
        bool ChoisirClasse(string classeId);

        /// Bascule prêt / pas prêt du joueur local (il faut une classe).
        void BasculerPret();

        /// Hôte : lance tout de suite quand tous sont prêts (sans attendre la fin du compte à rebours).
        void LancerMaintenant();

        /// Quitte le salon (retour à l'écran d'entrée).
        void Quitter();
    }
}
