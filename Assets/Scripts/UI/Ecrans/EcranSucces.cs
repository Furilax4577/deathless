using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Écran Succès (01/10/2026, Wiki/pages/succes.md), ouvert depuis le menu principal et la pause : les succès par
    /// rubrique, la progression des compteurs, la date de déblocage ; un succès caché reste « ??? » tant qu'il est
    /// verrouillé. Lit DonneesUI.Succes. Une icône générique pour tous (une icône par succès viendra plus tard).
    /// UXML et USS chargés par Resources (Succes/Succes) : le navigateur du village n'a aucun champ à régler.
    public class EcranSucces : Ecran
    {
        public const string Chemin = "Succes/Succes";

        Label m_Compte;
        VisualElement m_CompteBarre;
        ScrollView m_Liste;
        readonly List<VisualElement> m_Lignes = new List<VisualElement>();
        IListeSucces m_Source;

        protected override void Construire()
        {
            m_Compte = Racine.Q<Label>("succes-compte");
            m_CompteBarre = Racine.Q("succes-compte-barre");
            m_Liste = Racine.Q<ScrollView>("succes-liste");
        }

        protected override VisualElement PremierFocus => m_Lignes.Count > 0 ? m_Lignes[0] : null;

        public override void AuSommet() => Remplir();

        /// Reconstruit la liste (ouverture de l'écran, déblocage pendant qu'il est ouvert).
        void Remplir()
        {
            if (m_Liste == null) return;
            var source = DonneesUI.Succes;
            if (m_Source != source)
            {
                if (m_Source != null) m_Source.Debloque -= OnDebloque;
                m_Source = source;
                if (m_Source != null) m_Source.Debloque += OnDebloque;
            }
            m_Liste.Clear();
            m_Lignes.Clear();
            if (source == null) { if (m_Compte != null) m_Compte.text = "—"; return; }
            int total = source.Tous.Count, faits = source.NombreDebloques;
            if (m_Compte != null) m_Compte.text = faits + " / " + total;
            if (m_CompteBarre != null) m_CompteBarre.style.width = Length.Percent(total > 0 ? 100f * faits / total : 0f);
            string rubrique = null;
            foreach (var s in source.Tous)
            {
                if (s.Categorie != rubrique)
                {
                    rubrique = s.Categorie;
                    var titre = new Label((rubrique ?? "").ToUpperInvariant());
                    titre.AddToClassList("dl-section-label");
                    titre.AddToClassList("succes__rubrique");
                    if (m_Lignes.Count == 0) titre.AddToClassList("succes__rubrique--premiere");
                    m_Liste.Add(titre);
                }
                var ligne = Ligne(s);
                m_Liste.Add(ligne);
                m_Lignes.Add(ligne);
            }
            UINavigation.ChainerVerticalement(m_Lignes);
        }

        static VisualElement Ligne(ISuccesVue s)
        {
            bool cache = s.Cache && !s.Debloque;
            var ligne = new VisualElement { name = "succes-" + s.Id, focusable = true, tabIndex = 0 };
            ligne.AddToClassList("succes-ligne");
            ligne.AddToClassList(s.Debloque ? "succes-ligne--debloque" : "succes-ligne--verrouille");
            if (cache) ligne.AddToClassList("succes-ligne--cache");
            ligne.Add(IconesUI.Creer(s.Icone, "succes-ligne__icone"));
            var textes = new VisualElement();
            textes.AddToClassList("succes-ligne__textes");
            var nom = new Label(cache ? "???" : s.Nom);
            nom.AddToClassList("succes-ligne__nom");
            textes.Add(nom);
            var desc = new Label(cache ? "Succès caché : il se révélera quand vous l’aurez débloqué." : s.Description);
            desc.AddToClassList("succes-ligne__description");
            textes.Add(desc);
            ligne.Add(textes);
            var etat = new VisualElement();
            etat.AddToClassList("succes-ligne__etat");
            if (!s.Debloque && s.Seuil > 1)
            {
                var compteur = new Label(Nombre(s.Progression) + " / " + Nombre(s.Seuil));
                compteur.AddToClassList("succes-ligne__compteur");
                etat.Add(compteur);
                var barre = new VisualElement();
                barre.AddToClassList("succes-barre");
                var remplie = new VisualElement();
                remplie.AddToClassList("succes-barre__remplie");
                remplie.style.width = Length.Percent(Mathf.Clamp01((float)s.Progression / s.Seuil) * 100f);
                barre.Add(remplie);
                etat.Add(barre);
            }
            else
            {
                var d = s.Date;
                var statut = new Label(s.Debloque ? (d.HasValue ? "Débloqué le " + d.Value.ToString("dd/MM/yyyy") : "Débloqué") : "Verrouillé");
                statut.AddToClassList("succes-ligne__statut");
                etat.Add(statut);
            }
            ligne.Add(etat);
            return ligne;
        }

        /// 1 000 plutôt que 1000 (espace fine insécable comme séparateur des milliers).
        static string Nombre(int n) => n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");

        void OnDebloque(ISuccesVue _)
        {
            if (Racine.resolvedStyle.display != DisplayStyle.None) Remplir();
        }

        public override void Detruire()
        {
            if (m_Source != null) m_Source.Debloque -= OnDebloque;
            m_Source = null;
        }
    }

    /// Bannière de déblocage d'un succès (haut, à droite ; même pastille que les messages du HUD), au-dessus de tous les
    /// écrans : file d'attente, 4 s chacune. Posée par le NavigateurEcrans.
    public class BanniereSucces
    {
        public const float Duree = 4f;
        public VisualElement Racine { get; }
        readonly VisualElement m_Icone;
        readonly Label m_Nom;
        readonly Queue<ISuccesVue> m_File = new Queue<ISuccesVue>();
        IListeSucces m_Source;
        float m_Temps = -1f;

        public BanniereSucces(VisualElement parent)
        {
            Racine = new VisualElement { name = "succes-banniere", pickingMode = PickingMode.Ignore };
            var feuille = Resources.Load<StyleSheet>(EcranSucces.Chemin);
            if (feuille != null) Racine.styleSheets.Add(feuille);
            Racine.AddToClassList("succes-banniere");
            m_Icone = IconesUI.Creer(IconesUI.Succes, "succes-banniere__icone");
            Racine.Add(m_Icone);
            var textes = new VisualElement { pickingMode = PickingMode.Ignore };
            textes.AddToClassList("succes-banniere__textes");
            var titre = new Label("SUCCÈS DÉBLOQUÉ") { pickingMode = PickingMode.Ignore };
            titre.AddToClassList("succes-banniere__titre");
            textes.Add(titre);
            m_Nom = new Label { pickingMode = PickingMode.Ignore };
            m_Nom.AddToClassList("succes-banniere__nom");
            textes.Add(m_Nom);
            Racine.Add(textes);
            parent.Add(Racine);
        }

        /// Chaque image (temps réel : la bannière avance aussi dans les menus).
        public void MiseAJour(float dt)
        {
            var source = DonneesUI.Succes;
            if (source != m_Source)
            {
                if (m_Source != null) m_Source.Debloque -= Ajouter;
                m_Source = source;
                if (m_Source != null) m_Source.Debloque += Ajouter;
            }
            if (m_Temps < 0f)
            {
                if (m_File.Count == 0) return;
                var s = m_File.Dequeue();
                m_Nom.text = s.Nom;
                IconesUI.Poser(m_Icone, s.Icone);
                m_Temps = 0f;
                Racine.BringToFront();
                Racine.AddToClassList("succes-banniere--visible");
                return;
            }
            m_Temps += dt;
            // Un écran ouvert entre-temps passe devant (NavigateurEcrans.Rafraichir) : la bannière reste au-dessus.
            var parent = Racine.parent;
            if (parent != null && parent.IndexOf(Racine) != parent.childCount - 1) Racine.BringToFront();
            if (m_Temps >= Duree) Racine.RemoveFromClassList("succes-banniere--visible");
            if (m_Temps >= Duree + 0.45f) m_Temps = -1f;   // fondu de sortie, puis la suivante
        }

        public bool Visible => m_Temps >= 0f;
        public string Texte => m_Nom.text;

        void Ajouter(ISuccesVue s) => m_File.Enqueue(s);

        public void Detruire()
        {
            if (m_Source != null) m_Source.Debloque -= Ajouter;
            m_Source = null;
            Racine.RemoveFromHierarchy();
        }
    }
}
