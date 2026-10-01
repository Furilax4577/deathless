namespace Deathless.UI.Donnees
{
    /// Vagues de la nuit, pour le repère permanent du HUD à côté de l'horloge (demande de Quentin du 01/10/2026 : le
    /// numéro de vague toujours visible en jeu). Facultative : à implémenter par l'objet enregistré comme IEtatPartie
    /// (trouvé par DonneesUI.Partie as IEtatVagues) ; sans elle, le repère ne montre que le jour ou la nuit.
    public interface IEtatVagues
    {
        /// Numéro de la dernière vague lancée cette nuit (1 à VaguesTotal) ; 0 avant la première (ou hors de la nuit).
        int VagueEnCours { get; }
        /// Nombre de vagues prévues pour la nuit (connu dès le crépuscule) ; 0 si inconnu.
        int VaguesTotal { get; }
    }
}
