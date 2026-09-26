using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Source des statuts pour l'interface (IEtatStatuts, DonneesUI.Statuts, posée par HudPresenter) : statuts du héros
    /// local (plus l'eau du donjon, affichée comme un ralenti sans durée) et ennemis affectés. Recalculée une fois par
    /// image au plus, objets réutilisés (pas d'allocation à chaque image).
    public class StatutsUI : IEtatStatuts
    {
        sealed class Affiche : IStatutAffiche
        {
            public string Nom { get; set; }
            public string Icone { get; set; }
            public string Effet { get; set; }
            public string Source { get; set; }
            public float Restant { get; set; }
            public float Duree { get; set; }
            public bool Nefaste { get; set; }

            public void Poser(Statut s)
            {
                Nom = CatalogueStatuts.Nom(s.type);
                Icone = CatalogueStatuts.Icone(s.type);
                Effet = CatalogueStatuts.Effet(s);
                Source = CatalogueStatuts.Source(s);
                Restant = s.Restant;
                Duree = s.duree;
                Nefaste = CatalogueStatuts.Nefaste(s.type);
            }
        }

        sealed class Ennemi : IEnnemiAffecte
        {
            public readonly List<Affiche> pool = new List<Affiche>();
            public readonly List<IStatutAffiche> liste = new List<IStatutAffiche>();
            public Vector3 PositionTete { get; set; }
            public bool Visible { get; set; }
            public IReadOnlyList<IStatutAffiche> Statuts => liste;
        }

        readonly List<Affiche> m_PoolJoueur = new List<Affiche>();
        readonly List<IStatutAffiche> m_Joueur = new List<IStatutAffiche>();
        readonly List<Ennemi> m_PoolEnnemis = new List<Ennemi>();
        readonly List<IEnnemiAffecte> m_Ennemis = new List<IEnnemiAffecte>();
        int m_ImageJoueur = -1, m_ImageEnnemis = -1;

        /// Au-dessus de la tête : marge au-dessus du crâne (m).
        public static float MargeTete = 0.35f;

        public IReadOnlyList<IStatutAffiche> StatutsJoueur
        {
            get
            {
                if (m_ImageJoueur == Time.frameCount) return m_Joueur;
                m_ImageJoueur = Time.frameCount;
                m_Joueur.Clear();
                var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
                if (h == null || !h.Vivant) return m_Joueur;
                var st = h.Statuts;
                int n = 0;
                if (st != null)
                    for (int i = 0; i < st.Liste.Count; i++) Remplir(m_PoolJoueur, m_Joueur, ref n).Poser(st.Liste[i]);
                // Eau du donjon : ralenti sans durée tant que le héros y marche.
                float eau = st != null ? st.FacteurEau : Deathless.Donjon.ZoneEau.FacteurEn(h.transform.position + Vector3.up * 0.2f);
                if (eau < 0.999f)
                    Remplir(m_PoolJoueur, m_Joueur, ref n).Poser(new Statut
                    {
                        type = TypeStatut.Ralenti, intensite = 1f - eau, duree = 0f, fin = float.PositiveInfinity, origine = OrigineStatut.Eau,
                    });
                return m_Joueur;
            }
        }

        public IReadOnlyList<IEnnemiAffecte> EnnemisAffectes
        {
            get
            {
                if (m_ImageEnnemis == Time.frameCount) return m_Ennemis;
                m_ImageEnnemis = Time.frameCount;
                m_Ennemis.Clear();
                var actifs = Statuts.Actifs;
                int k = 0;
                for (int i = 0; i < actifs.Count; i++)
                {
                    var st = actifs[i];
                    if (st == null || st.Nombre == 0) continue;
                    var sq = st.GetComponent<Squelette>();
                    if (sq == null || sq.Sante == null || sq.Sante.Mort) continue;
                    if (k >= m_PoolEnnemis.Count) m_PoolEnnemis.Add(new Ennemi());
                    var e = m_PoolEnnemis[k++];
                    e.liste.Clear();
                    int n = 0;
                    for (int j = 0; j < st.Liste.Count; j++) Remplir(e.pool, e.liste, ref n).Poser(st.Liste[j]);
                    e.PositionTete = sq.CentreTete + Vector3.up * (sq.RayonTete + MargeTete);
                    e.Visible = RendusVisibles(sq);
                    m_Ennemis.Add(e);
                }
                return m_Ennemis;
            }
        }

        static Affiche Remplir(List<Affiche> pool, List<IStatutAffiche> liste, ref int n)
        {
            if (n >= pool.Count) pool.Add(new Affiche());
            var a = pool[n++];
            liste.Add(a);
            return a;
        }

        /// Rendus du squelette actifs (désintégration, étage masqué du donjon : non).
        static bool RendusVisibles(Squelette sq)
        {
            var r = sq.RenduTete;
            return r == null || (r.enabled && r.gameObject.activeInHierarchy);
        }
    }
}
