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
            Changees = null;
        }
    }
}
