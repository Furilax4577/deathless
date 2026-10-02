using Deathless.UI.Donnees;

namespace Deathless.Jeu
{
    /// Source de la visée d'une zone pour l'interface (IViseeZone, DonneesUI.Visee, posée par HudPresenter) : lit le héros
    /// local de la partie (jamais une marionnette : seul le propriétaire voit son indicateur).
    public class ViseeUI : IViseeZone
    {
        static Heros H
        {
            get
            {
                var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
                return h != null && !h.Distant ? h : null;
            }
        }

        public bool Visible { get { var h = H; return h != null && h.Classe != null && h.Classe.EnVisee; } }
        public string Sort { get { var h = H; return h != null && h.Classe != null ? h.Classe.LibelleVisee : ""; } }
        public bool Valide { get { var h = H; return h != null && h.Classe != null && h.Classe.PointViseValide; } }
    }
}
