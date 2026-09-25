namespace Deathless.UI.Donnees
{
    /// Ce que les écrans demandent au jeu (sens UI → jeu). Implémenté par le jeu ; les écrans n'appellent rien d'autre.
    public interface ICommandesPartie
    {
        /// Menu principal > Solo : lancer une partie solo (Paladin, en 0.1).
        void LancerSolo();

        /// Vote « prêt » du jour (Gameplay/Ready) ou « Rejouer » de l'écran de score : bascule le vote du joueur local.
        void BasculerPret();

        /// Pause > Quitter la partie, ou score > Arrêter : fin de la partie, retour au menu principal.
        void QuitterPartie();

        /// Menu principal ou pause > Quitter le jeu.
        void QuitterJeu();
    }
}
