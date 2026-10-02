using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Mini-jeu de crochetage (03/10/2026 ; wiki : donjon.md, {à confirmer}) : un panneau au centre, au-dessus de la barre de
    /// compétences, tant que le joueur local crochète une serrure. Une piste horizontale où la zone dorée glisse et où l'aiguille
    /// ivoire monte (Interagir maintenu) ou retombe ; sous la piste, la jauge de réussite (or) et le temps de l'essai ; en tête, le
    /// nom de la serrure, l'essai et les crochets restants ; en pied, un message (crochet cassé, réussite) et les deux invites
    /// (maintenir Interagir, annuler par Esquive). Lit DonneesUI.Crochetage (IEtatCrochetage) ; absent : masqué.
    public sealed class HudCrochetage
    {
        readonly VisualElement m_Racine, m_Zone, m_Aiguille, m_Progres, m_Temps;
        readonly Label m_Titre, m_Essais, m_Message;
        bool m_Vu;
        string m_TitreVu, m_EssaisVu, m_MessageVu;

        public HudCrochetage(VisualElement racine)
        {
            var hud = racine.Q("hud") ?? racine;
            m_Racine = new VisualElement { name = "crochetage", pickingMode = PickingMode.Ignore };
            m_Racine.AddToClassList("hud-crochetage");
            m_Racine.style.display = DisplayStyle.None;
            var entete = new VisualElement { pickingMode = PickingMode.Ignore };
            entete.AddToClassList("hud-crochetage__entete");
            m_Titre = new Label("Serrure") { pickingMode = PickingMode.Ignore };
            m_Titre.AddToClassList("hud-crochetage__titre");
            m_Essais = new Label("") { pickingMode = PickingMode.Ignore };
            m_Essais.AddToClassList("hud-crochetage__essais");
            entete.Add(m_Titre); entete.Add(m_Essais);
            m_Racine.Add(entete);
            var piste = new VisualElement { pickingMode = PickingMode.Ignore };
            piste.AddToClassList("hud-crochetage__piste");
            m_Zone = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Zone.AddToClassList("hud-crochetage__zone");
            m_Aiguille = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Aiguille.AddToClassList("hud-crochetage__aiguille");
            piste.Add(m_Zone); piste.Add(m_Aiguille);
            m_Racine.Add(piste);
            var jauge = new VisualElement { pickingMode = PickingMode.Ignore };
            jauge.AddToClassList("hud-crochetage__jauge");
            m_Progres = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Progres.AddToClassList("hud-crochetage__progres");
            jauge.Add(m_Progres);
            m_Racine.Add(jauge);
            var temps = new VisualElement { pickingMode = PickingMode.Ignore };
            temps.AddToClassList("hud-crochetage__temps");
            m_Temps = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Temps.AddToClassList("hud-crochetage__temps-reste");
            temps.Add(m_Temps);
            m_Racine.Add(temps);
            m_Message = new Label("Message") { pickingMode = PickingMode.Ignore };
            m_Message.AddToClassList("hud-crochetage__message");
            m_Racine.Add(m_Message);
            var invites = new VisualElement { pickingMode = PickingMode.Ignore };
            invites.AddToClassList("hud-crochetage__invites");
            var tenir = new InputPrompt("Gameplay/Interact", "Maintenir : monter") { pickingMode = PickingMode.Ignore };
            tenir.AddToClassList("dl-prompt--small");
            var annuler = new InputPrompt("Gameplay/Dodge", "Abandonner") { pickingMode = PickingMode.Ignore };
            annuler.AddToClassList("dl-prompt--small");
            annuler.style.marginLeft = 24;
            invites.Add(tenir); invites.Add(annuler);
            m_Racine.Add(invites);
            hud.Add(m_Racine);
        }

        public void Maj()
        {
            var c = DonneesUI.Crochetage;
            bool vu = c != null && c.Actif;
            if (vu != m_Vu) { m_Vu = vu; m_Racine.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None; }
            if (!vu) return;
            if (m_TitreVu != c.Serrure) { m_TitreVu = c.Serrure; m_Titre.text = c.Serrure; }
            string essais = "Essai " + c.Essai + " / " + c.EssaisMax + "   ·   Crochets : " + c.Crochets;
            if (essais != m_EssaisVu) { m_EssaisVu = essais; m_Essais.text = essais; }
            float demi = c.ZoneLargeur * 0.5f;
            m_Zone.style.left = Length.Percent(Mathf.Clamp01(c.ZoneCentre - demi) * 100f);
            m_Zone.style.width = Length.Percent(Mathf.Clamp01(c.ZoneLargeur) * 100f);
            m_Aiguille.style.left = Length.Percent(Mathf.Clamp01(c.Aiguille) * 100f);
            m_Progres.style.width = Length.Percent(Mathf.Clamp01(c.Progression) * 100f);
            m_Temps.style.width = Length.Percent(Mathf.Clamp01(c.TempsRestant) * 100f);
            m_Racine.EnableInClassList("hud-crochetage--dans", c.Dans);
            string msg = string.IsNullOrEmpty(c.Message) ? "Message" : c.Message;
            if (msg != m_MessageVu) { m_MessageVu = msg; m_Message.text = msg; }
            m_Message.EnableInClassList("hud-crochetage__message--vide", string.IsNullOrEmpty(c.Message));
            m_Message.EnableInClassList("hud-crochetage__message--echec", !string.IsNullOrEmpty(c.Message) && c.MessageEchec);
        }
    }
}
