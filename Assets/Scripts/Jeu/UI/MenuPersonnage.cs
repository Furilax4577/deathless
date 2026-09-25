using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Source du menu du personnage (IMenuPersonnage, touche Tab / Y) : fiche du joueur local, points de compétence
    /// (1 par jour survécu, crédité à l'aube par Partie) et arbre d'améliorations de sa classe (ArbreCompetences).
    /// Posé dans DonneesUI.Personnage par HudPresenter.
    public class MenuPersonnage : IMenuPersonnage
    {
        sealed class Ligne : IAmeliorationCompetence
        {
            public string Nom { get; set; }
            public string Description { get; set; }
            public string Icone { get; set; }
            public int Rang { get; set; }
            public int RangMax { get; set; }
            public bool Possible { get; set; }
        }

        readonly List<IAmeliorationCompetence> m_Lignes = new List<IAmeliorationCompetence>();
        readonly List<KeyValuePair<string, string>> m_Carac = new List<KeyValuePair<string, string>>();
        string m_ClasseLignes;
        string m_Message = "";
        bool m_Refus;

        Partie P => Partie.Instance;
        EtatJoueur J => P != null ? P.JoueurLocal : null;
        IClasseJouable C => ClassesJouables.Trouver(J != null ? J.classeId : Partie.ClasseChoisie);

        public string Nom => DonneesUI.Profil.PseudoDefini ? DonneesUI.Profil.Pseudo : J != null ? J.nom : "Joueur";
        public string Classe => C != null ? C.Nom : J != null ? J.classe : "";
        public string Embleme => C != null ? C.Embleme : "";
        public Color Teinte => C != null ? C.Teinte : HudPresenter.TeintePaladin;
        public int Points => J != null ? J.pointsCompetence : 0;
        public bool Ouvert => P != null && P.EnCours && J != null;
        public string Message => m_Message;
        public bool MessageRefus => m_Refus;

        public IReadOnlyList<KeyValuePair<string, string>> Caracteristiques
        {
            get
            {
                m_Carac.Clear();
                var j = J; var h = P != null ? P.HerosLocal : null;
                if (j == null) return m_Carac;
                m_Carac.Add(new KeyValuePair<string, string>("Vie", Mathf.CeilToInt(j.pv) + " / " + Mathf.CeilToInt(j.pvMax)));
                m_Carac.Add(new KeyValuePair<string, string>("Endurance", Mathf.CeilToInt(j.endurance) + " / " + Mathf.CeilToInt(j.enduranceMax)));
                if (j.jaugeMax > 0f && h != null && h.Classe != null)
                    m_Carac.Add(new KeyValuePair<string, string>(h.Classe.Jauge == JaugeClasse.Rage ? "Rage" : "Mana", Mathf.FloorToInt(j.jauge) + " / " + Mathf.CeilToInt(j.jaugeMax)));
                if (h != null && h.Classe != null) m_Carac.Add(new KeyValuePair<string, string>("Vitesse", h.Classe.Vitesse.ToString("0.#") + " m/s"));
                m_Carac.Add(new KeyValuePair<string, string>("Nuits survécues", j.nuitsSurvecues.ToString()));
                m_Carac.Add(new KeyValuePair<string, string>("Ennemis tués", j.score.ennemisTues.ToString()));
                return m_Carac;
            }
        }

        public IReadOnlyList<IAmeliorationCompetence> Ameliorations
        {
            get
            {
                var j = J;
                string id = j != null ? j.classeId : "";
                var defs = ArbreCompetences.De(id);
                if (m_ClasseLignes != id)
                {
                    m_ClasseLignes = id;
                    m_Lignes.Clear();
                    foreach (var d in defs) m_Lignes.Add(new Ligne { Nom = d.nom, Icone = d.icone, RangMax = ArbreCompetences.RangMax });
                }
                for (int i = 0; i < defs.Length && i < m_Lignes.Count; i++)
                {
                    var l = (Ligne)m_Lignes[i];
                    int rang = j != null && j.rangs != null && i < j.rangs.Length ? j.rangs[i] : 0;
                    l.Rang = rang;
                    l.Possible = j != null && rang < ArbreCompetences.RangMax && j.pointsCompetence >= ArbreCompetences.CoutParRang;
                    l.Description = defs[i].texte + " par rang" + (rang > 0 ? " · actuel : " + Actuel(defs[i], rang) : "");
                }
                return m_Lignes;
            }
        }

        static string Actuel(ArbreCompetences.Amelioration d, int rang)
        {
            float v = d.parRang * rang;
            switch (d.sens)
            {
                case ArbreCompetences.Sens.Plus: return "+" + Mathf.RoundToInt(v * 100f) + " %";
                case ArbreCompetences.Sens.Moins: return "−" + Mathf.RoundToInt(v * 100f) + " %";
                default: return "+" + Mathf.RoundToInt(v);
            }
        }

        public void Ameliorer(int index)
        {
            if (P == null) return;
            m_Message = P.AmeliorerCompetence(index, out m_Refus);
            if (m_Refus) AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.7f);
            else AudioBank.Jouer2D(SonsDuJeu.PointDepense, 0.8f);
        }

        /// Ouverture (touche du menu) : le message de la dernière visite est effacé.
        public void Ouvrir()
        {
            m_Message = "";
            DonneesUI.OuvrirMenuPersonnage();
        }
    }
}
