namespace Deathless.UI.Donnees
{
    /// Aperçu de la nouvelle carte (02/10/2026), pour le HUD. Facultative : à implémenter par l'objet enregistré comme
    /// IEtatPartie (trouvé par DonneesUI.Partie as IEtatApercu) ; sans elle, le HUD garde son affichage de partie.
    public interface IEtatApercu
    {
        /// Vrai dans le mode « Nouvelle carte (aperçu) » : le jour (ou la nuit forcée par F9) est figé, il n'y a ni
        /// compte à rebours ni vagues. Le HUD écrit alors « Jour · aperçu » ou « Nuit · aperçu » à la place du chrono.
        bool EnApercu { get; }
    }
}
