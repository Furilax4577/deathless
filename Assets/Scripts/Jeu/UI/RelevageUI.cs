using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Source de la jauge de relevé pour l'interface (IJaugeRelevage, DonneesUI.Relevage, posée par HudPresenter) :
    /// lit le héros local de la partie (jamais une marionnette : seul le propriétaire martèle).
    public class RelevageUI : IJaugeRelevage
    {
        static Heros H
        {
            get
            {
                var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
                return h != null && !h.Distant ? h : null;
            }
        }

        public bool Visible { get { var h = H; return h != null && h.EnRenverse; } }
        public Vector3 Position { get { var h = H; return h != null ? h.transform.position : Vector3.zero; } }
        public float Martelement { get { var h = H; return h != null ? h.RenverseMartelementRatio : 0f; } }
        public float DernierMartelement { get { var h = H; return h != null ? h.RenverseDernierMartelement : -99f; } }
    }
}
