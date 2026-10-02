using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Bandeau de la visée d'une zone au sol (02/10/2026 ; wiki : commandes.md) : tant que le héros local vise un sort de
    /// zone (mage : grande boule, mur ; rôdeur : nuée), une pastille au-dessus de la barre de compétences donne le nom du
    /// sort et les deux invites de boutons du moment (InputPrompt : « Confirmer » = attaque principale, RT ou clic gauche ;
    /// « Annuler » = attaque secondaire, LT ou clic droit), qui suivent le dernier appareil utilisé. Lit DonneesUI.Visee
    /// (IViseeZone, posée par le jeu) ; masquée sans source.
    public sealed class HudVisee
    {
        readonly VisualElement m_Racine;
        readonly Label m_Sort;
        bool m_Vu;

        public HudVisee(VisualElement racine)
        {
            var hud = racine.Q("hud") ?? racine;
            m_Racine = new VisualElement { name = "visee", pickingMode = PickingMode.Ignore };
            m_Racine.AddToClassList("hud-pastille");
            m_Racine.AddToClassList("hud-visee");
            m_Racine.style.display = DisplayStyle.None;
            m_Sort = new Label("Grande boule de feu") { pickingMode = PickingMode.Ignore };
            m_Sort.AddToClassList("hud-visee__sort");
            m_Racine.Add(m_Sort);
            var confirmer = new InputPrompt("Gameplay/AttackPrimary", "Confirmer") { pickingMode = PickingMode.Ignore };
            confirmer.AddToClassList("dl-prompt--small");
            confirmer.AddToClassList("hud-visee__invite");
            m_Racine.Add(confirmer);
            var annuler = new InputPrompt("Gameplay/AttackSecondary", "Annuler") { pickingMode = PickingMode.Ignore };
            annuler.AddToClassList("dl-prompt--small");
            annuler.AddToClassList("hud-visee__invite");
            m_Racine.Add(annuler);
            hud.Add(m_Racine);
        }

        /// Chaque image : visible tant que la visée est ouverte ; sans sol sous le point visé, le bandeau le dit et vire au rouge.
        public void Maj()
        {
            var v = DonneesUI.Visee;
            bool visible = v != null && v.Visible;
            if (visible != m_Vu)
            {
                m_Vu = visible;
                m_Racine.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (!visible) return;
            string texte = v.Valide ? v.Sort : v.Sort + " : pas de sol ici";
            if (m_Sort.text != texte) m_Sort.text = texte;
            m_Racine.EnableInClassList("hud-visee--refus", !v.Valide);
        }
    }
}
