using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Jauge de relevé du Renversé (statut Renversé ; wiki : statuts.md) : le jeu la pose dans DonneesUI.Relevage
    /// (héros local seulement : chaque poste montre sa propre jauge) ; le HUD la lit à chaque image. Absente : rien
    /// n'est affiché.
    public interface IJaugeRelevage
    {
        /// Le héros local est à terre (Renversé) : la jauge est affichée.
        bool Visible { get; }
        /// Point du monde sous lequel placer la jauge (pieds du héros local).
        Vector3 Position { get; }
        /// Part du relevé gagnée en martelant Saut, de 0 à 1.
        float Martelement { get; }
        /// Instant (Time.time) du dernier martelage, pour la petite secousse ; très négatif s'il n'y en a pas eu.
        float DernierMartelement { get; }
    }
}
