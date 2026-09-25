namespace Deathless.UI.Donnees
{
    /// Donjon, pour le HUD du joueur local : or porté (ramassé au donjon, versé à la caisse au retour par le portail),
    /// présence au donjon, alerte avant le rappel par Nyxessa et dernier message (rappel, dépôt).
    public interface IEtatDonjon
    {
        bool AuDonjon { get; }
        int OrPorte { get; }
        /// Secondes avant le rappel par Nyxessa (alerte), ou -1 hors alerte.
        float AvantRappel { get; }
        /// Message du moment (« Rappelé par Nyxessa… », « 120 or versés à la caisse »), ou vide.
        string Message { get; }
    }
}
