using System;

namespace Deathless.UI.Donnees
{
    /// Boss de la nuit (30/09/2026), pour le HUD. Facultative : à implémenter par l'objet enregistré comme IEtatPartie
    /// (trouvé par DonneesUI.Partie as IEtatBoss) ; sans elle, ni bannière de boss ni message d'aube retenue.
    public interface IEtatBoss
    {
        /// Nom du boss dont l'aube attend la chute (temps de la nuit écoulé, boss vivant), null sinon. Le HUD affiche
        /// alors « Nuit 10 · l'aube attend la chute de Morgrim » à la place du compte à rebours.
        string AubeAttend { get; }

        /// Un boss sort de terre (son nom) : bannière en grand, comme « NUIT N ».
        event Action<string> BossSurgit;
    }
}
