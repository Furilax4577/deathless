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
        /// Ligne d'un objet de l'inventaire (potions, clés, kit de crochetage ; DonneesUI.Inventaire).
        sealed class LigneObjet
        {
            public VisualElement racine, icone;
            public Label nom, desc, quantite;
            public string iconePosee;
        }

        sealed class Ligne
        {
            public Button bouton;
            public VisualElement icone;
            public Label nom, desc;
            public readonly List<VisualElement> rangs = new List<VisualElement>();
            public string iconePosee;
        }

        /// Ligne d'un attribut (01/10/2026) : valeur, nom, crans (départ, gagnés), effet par point et total gagné.
        sealed class LigneAttribut
        {
            public Button bouton;
            public Label valeur, nom, desc;
            public readonly List<VisualElement> crans = new List<VisualElement>();
        }

        IMenuPersonnage m_Menu;
        IAttributsPersonnage m_MenuAttributs;
        VisualElement m_Embleme, m_Carac, m_Ameliorations, m_Attributs, m_CarteAttributs;
        Label m_Nom, m_Classe, m_Points, m_PointsAttributs, m_Message;
        readonly List<Ligne> m_Lignes = new List<Ligne>();
        readonly List<LigneObjet> m_LignesObjets = new List<LigneObjet>();
        VisualElement m_Objets;
        Label m_Vide;
        readonly List<LigneAttribut> m_LignesAttributs = new List<LigneAttribut>();
        readonly List<(Label libelle, Label valeur)> m_LignesCarac = new List<(Label, Label)>();
        string m_EmblemePose;
        /// Section « Afflictions » (statuts actifs, infobulle ; AfflictionsPersonnage.cs).
        SectionAfflictions m_Afflictions;

        protected override void Construire()
        {
            m_Embleme = Racine.Q("perso-embleme");
            m_Carac = Racine.Q("perso-carac");
            m_Ameliorations = Racine.Q("perso-ameliorations");
            m_Nom = Racine.Q<Label>("perso-nom");
            m_Classe = Racine.Q<Label>("perso-classe");
            m_Points = Racine.Q<Label>("perso-points");
            m_Attributs = Racine.Q("perso-attributs");
            m_CarteAttributs = Racine.Q("perso-attributs-carte");
            m_PointsAttributs = Racine.Q<Label>("perso-points-attributs");
            m_Message = Racine.Q<Label>("perso-message");
            var afflictions = Racine.Q("perso-afflictions");
            if (afflictions != null) m_Afflictions = new SectionAfflictions(afflictions, Racine.Q<Label>("perso-afflictions-vide"), Racine);
            m_Objets = Racine.Q("perso-cases");
            m_Vide = Racine.Q<Label>("perso-vide");
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
            ConstruireAttributs(menu as IAttributsPersonnage);
            // Compétences puis attributs, enchaînés de haut en bas (manette, flèches).
            var focalisables = m_Lignes.ConvertAll(x => (VisualElement)x.bouton);
            foreach (var la in m_LignesAttributs) focalisables.Add(la.bouton);
            UINavigation.ChainerVerticalement(focalisables);
            if (m_Afflictions != null) { m_Afflictions.Reinitialiser(); m_Afflictions.Lier(focalisables); }
            Rafraichir();
        }

        /// Section « Attributs » : une ligne focalisable par attribut ; Valider y place un point. Masquée si le menu ne
        /// fournit pas IAttributsPersonnage.
        void ConstruireAttributs(IAttributsPersonnage menu)
        {
            m_MenuAttributs = menu;
            m_LignesAttributs.Clear();
            if (m_Attributs == null) return;
            m_Attributs.Clear();
            if (m_CarteAttributs != null) m_CarteAttributs.style.display = menu != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (menu == null) return;
            var at = menu.Attributs;
            for (int i = 0; i < at.Count; i++)
            {
                int index = i;
                var l = new LigneAttribut { bouton = new Button { name = "perso-attribut-" + i } };
                l.bouton.AddToClassList("dl-menu-item");
                l.bouton.AddToClassList("perso__ligne");
                l.bouton.AddToClassList("perso__attribut");
                l.valeur = new Label(); l.valeur.AddToClassList("perso__attribut-valeur");
                var textes = new VisualElement(); textes.AddToClassList("perso__ligne-textes");
                var haut = new VisualElement(); haut.AddToClassList("perso__ligne-haut");
                l.nom = new Label(); l.nom.AddToClassList("dl-menu-item__label"); l.nom.AddToClassList("perso__ligne-nom");
                var crans = new VisualElement(); crans.AddToClassList("perso__crans");
                for (int r = 0; r < at[i].Plafond; r++)
                {
                    var c = new VisualElement(); c.AddToClassList("perso__cran");
                    crans.Add(c); l.crans.Add(c);
                }
                haut.Add(l.nom); haut.Add(crans);
                l.desc = new Label(); l.desc.AddToClassList("dl-menu-item__desc"); l.desc.AddToClassList("perso__ligne-desc");
                textes.Add(haut); textes.Add(l.desc);
                var invite = new InputPrompt("UI/Submit", ""); invite.AddToClassList("dl-menu-item__prompt");
                l.bouton.Add(l.valeur); l.bouton.Add(textes); l.bouton.Add(invite);
                l.bouton.clicked += () => m_MenuAttributs?.AmeliorerAttribut(index);
                SonDeClic(l.bouton);
                m_Attributs.Add(l.bouton);
                m_LignesAttributs.Add(l);
            }
        }

        /// Inventaire : une ligne par objet (grisée à quantité nulle) ; les lignes sont bâties à la première lecture.
        void RafraichirInventaire()
        {
            var inv = DonneesUI.Inventaire;
            if (m_Objets == null) return;
            int n = inv != null ? inv.NbObjets : 0;
            while (m_LignesObjets.Count < n)
            {
                var l = new LigneObjet { racine = new VisualElement() };
                l.racine.AddToClassList("perso__objet");
                l.icone = new VisualElement(); l.icone.AddToClassList("dl-icone"); l.icone.AddToClassList("perso__objet-icone");
                var textes = new VisualElement(); textes.AddToClassList("perso__objet-textes");
                l.nom = new Label(); l.nom.AddToClassList("dl-text"); l.nom.AddToClassList("perso__objet-nom");
                l.desc = new Label(); l.desc.AddToClassList("perso__objet-desc");
                textes.Add(l.nom); textes.Add(l.desc);
                l.quantite = new Label(); l.quantite.AddToClassList("perso__objet-quantite");
                l.racine.Add(l.icone); l.racine.Add(textes); l.racine.Add(l.quantite);
                m_Objets.Add(l.racine);
                m_LignesObjets.Add(l);
            }
            int portes = 0;
            for (int i = 0; i < m_LignesObjets.Count; i++)
            {
                var l = m_LignesObjets[i];
                bool vu = inv != null && i < n && inv.ObjetUtilisable(i);
                l.racine.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None;
                if (!vu) continue;
                int q = inv.ObjetQuantite(i);
                if (q > 0) portes++;
                l.nom.text = inv.ObjetNom(i);
                l.desc.text = inv.ObjetDescription(i);
                l.quantite.text = q + " / " + inv.ObjetMax(i);
                string ic = inv.ObjetIcone(i);
                if (l.iconePosee != ic) { l.iconePosee = ic; IconesUI.Poser(l.icone, ic); }
                l.racine.EnableInClassList("perso__objet--vide", q <= 0);
            }
            if (m_Vide != null) m_Vide.style.display = portes == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static string TextePoints(int points) => points == 0 ? "Aucun point à dépenser" : points == 1 ? "1 point à dépenser" : points + " points à dépenser";

        protected override VisualElement PremierFocus => m_Lignes.Count > 0 ? m_Lignes[0].bouton : m_LignesAttributs.Count > 0 ? m_LignesAttributs[0].bouton : null;

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
            m_Points.text = TextePoints(points);
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

            var ma = m_MenuAttributs;
            if (ma != null)
            {
                int pa = ma.PointsAttribut;
                if (m_PointsAttributs != null)
                {
                    m_PointsAttributs.text = TextePoints(pa);
                    m_PointsAttributs.EnableInClassList("perso__points--dispo", pa > 0);
                }
                var at = ma.Attributs;
                for (int i = 0; i < m_LignesAttributs.Count && i < at.Count; i++)
                {
                    var a = at[i]; var l = m_LignesAttributs[i];
                    l.nom.text = a.Nom;
                    l.valeur.text = a.Valeur.ToString();
                    l.desc.text = string.IsNullOrEmpty(a.Actuel) ? a.Effet : a.Effet + " · gagné : " + a.Actuel;
                    for (int r = 0; r < l.crans.Count; r++)
                    {
                        l.crans[r].EnableInClassList("perso__cran--depart", r < a.Depart && r < a.Valeur);
                        l.crans[r].EnableInClassList("perso__cran--gagne", r >= a.Depart && r < a.Valeur);
                    }
                    l.bouton.EnableInClassList("perso__ligne--impossible", !a.Possible);
                }
            }

            RafraichirInventaire();

            bool vide = string.IsNullOrEmpty(m.Message);
            m_Message.text = vide ? "Message" : m.Message;
            m_Message.EnableInClassList("perso__message--vide", vide);
            m_Message.EnableInClassList("perso__message--refus", !vide && m.MessageRefus);
            m_Message.EnableInClassList("perso__message--ok", !vide && !m.MessageRefus);

            if (m_Afflictions != null) m_Afflictions.Maj(DonneesUI.Statuts != null ? DonneesUI.Statuts.StatutsJoueur : null);
        }
    }
}
