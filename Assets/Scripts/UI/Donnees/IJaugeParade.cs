namespace Deathless.UI.Donnees
{
    /// Issue du coup suivi par la jauge de parade.
    public enum ResultatParade { Aucun, Bloque, Parade, Parfaite, Touche }

    /// Jauge de parade du paladin local (Docs/ui-v01.md, « Jauge de parade » ; Wiki interface.md). Le jeu la pose dans
    /// DonneesUI.Parade (ClassePaladin du héros local) ; le HUD la lit à chaque image. Absente : rien n'est affiché.
    /// Temps en secondes ; l'impact prévu est à droite de la jauge, le curseur avance vers lui.
    public interface IJaugeParade
    {
        /// Un coup parable vise le joueur local (ou son issue est encore montrée) : la jauge est affichée.
        bool Visible { get; }
        /// Temps restant avant l'impact prévu ; négatif juste après.
        float AvantImpact { get; }
        /// Durée représentée par toute la largeur de la jauge : le curseur part de la gauche quand il reste Duree s.
        float Duree { get; }
        /// Fenêtre de parade (large) et fenêtre de la parade parfaite (étroite), comptées avant l'impact.
        float FenetreParade { get; }
        float FenetreParfaite { get; }
        /// Temps avant l'impact auquel la garde a été levée pour ce coup ; négatif : pas encore d'appui.
        float Appui { get; }
        /// Issue du coup (Aucun tant qu'il n'a pas porté) et temps écoulé depuis qu'elle est connue.
        ResultatParade Resultat { get; }
        float DepuisResultat { get; }
    }
}
