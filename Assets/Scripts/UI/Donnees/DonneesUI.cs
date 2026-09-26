using System;

namespace Deathless.UI.Donnees
{
    /// Point de rendez-vous entre le jeu et les écrans. Le jeu enregistre ses sources (Enregistrer) quand une partie
    /// existe, et les retire (Retirer) à la fin ; les écrans lisent ces propriétés sans connaître le jeu.
    /// Une source peut implémenter plusieurs interfaces (EtatFactice les implémente toutes).
    public static class DonneesUI
    {
        public static IEtatPartie Partie { get; private set; }
        public static IEtatJoueur Joueur { get; private set; }
        public static IScoreFin Score { get; private set; }
        public static ICommandesPartie Commandes { get; private set; }

        /// Aperçu 3D des classes pour l'écran de choix (facultatif, posé par le jeu ; null : pas d'aperçu).
        public static IApercuClasse ApercuClasse { get; set; }

        /// Lobby multijoueur (posé par le jeu ou le futur module réseau ; à défaut, le menu crée un LobbyFactice).
        public static ILobby Lobby { get; set; }

        /// Profil du joueur local (pseudo), toujours présent.
        public static IProfilJoueur Profil => ProfilJoueur.Local;

        /// Le jeu demande l'ouverture d'un menu d'achat (interaction : relique, taverne) ; le navigateur d'écrans l'ouvre
        /// par-dessus le HUD.
        public static event Action<IMenuAchat> MenuAchatDemande;
        public static void OuvrirMenuAchat(IMenuAchat menu) => MenuAchatDemande?.Invoke(menu);

        /// Donjon du joueur local (or porté, alerte, rappel) ; null sans donjon.
        public static IEtatDonjon Donjon { get; set; }

        /// Menu du personnage (posé par le jeu en partie ; null sinon).
        public static IMenuPersonnage Personnage { get; set; }

        /// Le jeu demande l'ouverture du menu du personnage (touche Tab, Y, Triangle).
        public static event Action MenuPersonnageDemande;
        public static void OuvrirMenuPersonnage() => MenuPersonnageDemande?.Invoke();

        /// Roue à emotes du joueur local (posée par le jeu quand le héros local existe ; null sinon). Affichée par le
        /// navigateur d'écrans en calque du HUD tant qu'elle est ouverte.
        public static IRoueEmotes RoueEmotes { get; set; }

        /// Statuts du joueur local et des ennemis affectés (posé par le jeu ; null : rien n'est affiché).
        public static IEtatStatuts Statuts { get; set; }

        /// Jauge de parade du paladin local (posée par le jeu ; null : pas de jauge).
        public static IJaugeParade Parade { get; set; }

        /// Jauge de relevé du Renversé du héros local (posée par le jeu ; null : pas de jauge).
        public static IJaugeRelevage Relevage { get; set; }

        /// Appelé à chaque enregistrement ou retrait (les écrans se réabonnent aux événements de IEtatPartie).
        public static event Action Changees;

        public static void Enregistrer(IEtatPartie partie, IEtatJoueur joueur, IScoreFin score, ICommandesPartie commandes)
        {
            Partie = partie;
            Joueur = joueur;
            Score = score;
            Commandes = commandes;
            Changees?.Invoke();
        }

        public static void Retirer()
        {
            Partie = null;
            Joueur = null;
            Score = null;
            Changees?.Invoke();
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Partie = null;
            Joueur = null;
            Score = null;
            Commandes = null;
            ApercuClasse = null;
            Lobby = null;
            RoueEmotes = null;
            Parade = null;
            Relevage = null;
            Changees = null;
        }
    }
}
