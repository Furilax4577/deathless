using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Menu d'achat (IMenuAchat) ouvert par une interaction du jeu : achats des paliers à la relique, taverne. Une ligne
    /// focalisable par article (nom, niveau, ce qu'il apporte, prix) ; Valider achète, Retour ferme. Superposé au HUD :
    /// la partie continue. Se ferme seul quand le menu n'a plus lieu d'être (nuit, joueur éloigné ou mort).
    public class EcranAchat : Ecran
    {
        sealed class Ligne
        {
            public Button bouton;
            public Label nom, niveau, desc, prix;
        }

        IMenuAchat m_Menu;
        Label m_Titre, m_SousTitre, m_Or, m_Legende, m_Message;
        VisualElement m_Articles;
        readonly List<Ligne> m_Lignes = new List<Ligne>();

        public IMenuAchat Menu => m_Menu;

        protected override void Construire()
        {
            m_Titre = Racine.Q<Label>("achat-titre");
            m_SousTitre = Racine.Q<Label>("achat-sous-titre");
            m_Or = Racine.Q<Label>("achat-or");
            m_Legende = Racine.Q<Label>("achat-legende");
            m_Message = Racine.Q<Label>("achat-message");
            m_Articles = Racine.Q("achat-articles");
        }

        /// Prépare l'écran pour ce menu (avant Navigateur.Ouvrir).
        public void Afficher(IMenuAchat menu)
        {
            m_Menu = menu;
            m_Articles.Clear();
            m_Lignes.Clear();
            var articles = menu.Articles;
            for (int i = 0; i < articles.Count; i++)
            {
                int index = i;
                var l = new Ligne { bouton = new Button { name = "achat-article-" + i } };
                l.bouton.AddToClassList("dl-menu-item");
                l.bouton.AddToClassList("achat__ligne");
                var textes = new VisualElement(); textes.AddToClassList("achat__ligne-textes");
                var haut = new VisualElement(); haut.AddToClassList("achat__ligne-haut");
                l.nom = new Label(); l.nom.AddToClassList("dl-menu-item__label"); l.nom.AddToClassList("achat__nom");
                l.niveau = new Label(); l.niveau.AddToClassList("achat__niveau");
                haut.Add(l.nom); haut.Add(l.niveau);
                l.desc = new Label(); l.desc.AddToClassList("dl-menu-item__desc"); l.desc.AddToClassList("achat__desc");
                textes.Add(haut); textes.Add(l.desc);
                var prix = new VisualElement(); prix.AddToClassList("achat__prix");
                var piece = new PieceOr(); piece.AddToClassList("achat__prix-piece");
                l.prix = new Label(); l.prix.AddToClassList("achat__prix-valeur");
                prix.Add(piece); prix.Add(l.prix);
                var invite = new InputPrompt("UI/Submit", ""); invite.AddToClassList("dl-menu-item__prompt");
                l.bouton.Add(textes); l.bouton.Add(prix); l.bouton.Add(invite);
                l.bouton.clicked += () => m_Menu?.Acheter(index);
                SonDeClic(l.bouton);
                m_Articles.Add(l.bouton);
                m_Lignes.Add(l);
            }
            UINavigation.ChainerVerticalement(m_Lignes.ConvertAll(x => (VisualElement)x.bouton));
            Rafraichir();
        }

        protected override VisualElement PremierFocus => m_Lignes.Count > 0 ? m_Lignes[0].bouton : null;

        public override void MiseAJour(float dt)
        {
            if (m_Menu == null) return;
            if (!m_Menu.Ouvert)
            {
                m_Menu = null;
                if (Navigateur.Sommet == this) Navigateur.Fermer();
                return;
            }
            Rafraichir();
        }

        void Rafraichir()
        {
            var m = m_Menu;
            if (m == null) return;
            m_Titre.text = m.Titre;
            m_SousTitre.text = m.SousTitre;
            m_Or.text = m.Or.ToString();
            m_Legende.text = m.LegendeOr;
            var articles = m.Articles;
            for (int i = 0; i < m_Lignes.Count && i < articles.Count; i++)
            {
                var a = articles[i]; var l = m_Lignes[i];
                l.nom.text = a.Nom;
                l.niveau.text = a.Niveau ?? "";
                l.desc.text = a.Description ?? "";
                l.prix.text = a.Prix >= 0 ? a.Prix.ToString() : "";
                l.bouton.EnableInClassList("achat__ligne--indisponible", !a.Achetable);
                l.bouton.EnableInClassList("achat__ligne--max", a.Prix < 0);
            }
            // Place du message réservée en permanence (texte transparent quand il n'y en a pas).
            bool vide = string.IsNullOrEmpty(m.Message);
            m_Message.text = vide ? "Message" : m.Message;
            m_Message.EnableInClassList("achat__message--vide", vide);
            m_Message.EnableInClassList("achat__message--refus", !vide && m.MessageRefus);
            m_Message.EnableInClassList("achat__message--ok", !vide && !m.MessageRefus);
        }
    }
}
