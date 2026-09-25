using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Menu du personnage (touche Tab, Y, Triangle ; IMenuPersonnage) : fiche du personnage, inventaire (vide pour
    /// l'instant) et amélioration des compétences avec les points de compétence. Une ligne focalisable par amélioration
    /// (icône, nom, rangs, effet) ; Valider améliore, Retour ferme. Superposé au HUD : la partie continue.
    public class EcranPersonnage : Ecran
    {
        /// Cases de l'inventaire (vides pour l'instant).
        public const int CasesInventaire = 9;

        sealed class Ligne
        {
            public Button bouton;
            public VisualElement icone;
            public Label nom, desc;
            public readonly List<VisualElement> rangs = new List<VisualElement>();
            public string iconePosee;
        }

        IMenuPersonnage m_Menu;
        VisualElement m_Embleme, m_Carac, m_Ameliorations;
        Label m_Nom, m_Classe, m_Points, m_Message;
        readonly List<Ligne> m_Lignes = new List<Ligne>();
        readonly List<(Label libelle, Label valeur)> m_LignesCarac = new List<(Label, Label)>();
        string m_EmblemePose;

        protected override void Construire()
        {
            m_Embleme = Racine.Q("perso-embleme");
            m_Carac = Racine.Q("perso-carac");
            m_Ameliorations = Racine.Q("perso-ameliorations");
            m_Nom = Racine.Q<Label>("perso-nom");
            m_Classe = Racine.Q<Label>("perso-classe");
            m_Points = Racine.Q<Label>("perso-points");
            m_Message = Racine.Q<Label>("perso-message");
            var cases = Racine.Q("perso-cases");
            for (int i = 0; i < CasesInventaire; i++)
            {
                var c = new VisualElement(); c.AddToClassList("perso__case");
                cases.Add(c);
            }
        }

        /// Prépare l'écran pour ce menu (avant Navigateur.Ouvrir).
        public void Afficher(IMenuPersonnage menu)
        {
            m_Menu = menu;
            m_Ameliorations.Clear();
            m_Lignes.Clear();
            var am = menu.Ameliorations;
            for (int i = 0; i < am.Count; i++)
            {
                int index = i;
                var l = new Ligne { bouton = new Button { name = "perso-amelioration-" + i } };
                l.bouton.AddToClassList("dl-menu-item");
                l.bouton.AddToClassList("perso__ligne");
                l.icone = new VisualElement(); l.icone.AddToClassList("dl-icone"); l.icone.AddToClassList("perso__ligne-icone");
                var textes = new VisualElement(); textes.AddToClassList("perso__ligne-textes");
                var haut = new VisualElement(); haut.AddToClassList("perso__ligne-haut");
                l.nom = new Label(); l.nom.AddToClassList("dl-menu-item__label"); l.nom.AddToClassList("perso__ligne-nom");
                var rangs = new VisualElement(); rangs.AddToClassList("perso__rangs");
                for (int r = 0; r < am[i].RangMax; r++)
                {
                    var p = new VisualElement(); p.AddToClassList("perso__rang");
                    rangs.Add(p); l.rangs.Add(p);
                }
                haut.Add(l.nom); haut.Add(rangs);
                l.desc = new Label(); l.desc.AddToClassList("dl-menu-item__desc"); l.desc.AddToClassList("perso__ligne-desc");
                textes.Add(haut); textes.Add(l.desc);
                var invite = new InputPrompt("UI/Submit", ""); invite.AddToClassList("dl-menu-item__prompt");
                l.bouton.Add(l.icone); l.bouton.Add(textes); l.bouton.Add(invite);
                l.bouton.clicked += () => m_Menu?.Ameliorer(index);
                SonDeClic(l.bouton);
                m_Ameliorations.Add(l.bouton);
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
            m_Nom.text = m.Nom;
            m_Classe.text = m.Classe;
            m_Classe.style.color = m.Teinte;
            if (m_EmblemePose != m.Embleme) { m_EmblemePose = m.Embleme; IconesUI.Poser(m_Embleme, m.Embleme); }

            var carac = m.Caracteristiques;
            while (m_LignesCarac.Count < carac.Count)
            {
                var ligne = new VisualElement(); ligne.AddToClassList("perso__carac-ligne");
                var lib = new Label(); lib.AddToClassList("dl-text"); lib.AddToClassList("perso__carac-libelle");
                var val = new Label(); val.AddToClassList("dl-text"); val.AddToClassList("perso__carac-valeur");
                ligne.Add(lib); ligne.Add(val);
                m_Carac.Add(ligne);
                m_LignesCarac.Add((lib, val));
            }
            for (int i = 0; i < m_LignesCarac.Count; i++)
            {
                bool vu = i < carac.Count;
                m_LignesCarac[i].libelle.parent.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None;
                if (!vu) continue;
                m_LignesCarac[i].libelle.text = carac[i].Key;
                m_LignesCarac[i].valeur.text = carac[i].Value;
            }

            int points = m.Points;
            m_Points.text = points == 0 ? "Aucun point à dépenser" : points == 1 ? "1 point à dépenser" : points + " points à dépenser";
            m_Points.EnableInClassList("perso__points--dispo", points > 0);

            var am = m.Ameliorations;
            for (int i = 0; i < m_Lignes.Count && i < am.Count; i++)
            {
                var a = am[i]; var l = m_Lignes[i];
                l.nom.text = a.Nom;
                l.desc.text = a.Description;
                if (l.iconePosee != a.Icone) { l.iconePosee = a.Icone; IconesUI.Poser(l.icone, a.Icone); }
                for (int r = 0; r < l.rangs.Count; r++) l.rangs[r].EnableInClassList("perso__rang--plein", r < a.Rang);
                l.bouton.EnableInClassList("perso__ligne--impossible", !a.Possible);
            }

            bool vide = string.IsNullOrEmpty(m.Message);
            m_Message.text = vide ? "Message" : m.Message;
            m_Message.EnableInClassList("perso__message--vide", vide);
            m_Message.EnableInClassList("perso__message--refus", !vide && m.MessageRefus);
            m_Message.EnableInClassList("perso__message--ok", !vide && !m.MessageRefus);
        }
    }
}
