namespace Deathless.UI.Donnees
{
    /// Visée d'une zone au sol du héros local (grande boule de feu et mur de flammes du mage, nuée de flèches du rôdeur ;
    /// 02/10/2026, wiki : commandes.md « Viser une zone ») : le jeu la pose dans DonneesUI.Visee ; le HUD la lit à chaque
    /// image et montre, tant qu'elle est ouverte, le nom du sort et les invites « Confirmer » (RT, clic gauche) et
    /// « Annuler » (LT, clic droit). Absente : rien n'est affiché. Locale au joueur qui vise.
    public interface IViseeZone
    {
        /// Une visée de zone est ouverte chez le héros local.
        bool Visible { get; }
        /// Nom du sort visé (« Grande boule de feu », « Mur de flammes », « Nuée de flèches »).
        string Sort { get; }
        /// Le point visé est sur du sol : faux (vide), confirmer est refusé.
        bool Valide { get; }
    }
}
