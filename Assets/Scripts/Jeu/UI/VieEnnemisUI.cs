using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Source de la vie des ennemis pour l'interface (IEtatVieEnnemis, DonneesUI.VieEnnemis, posée par HudPresenter) :
    /// barre fine des ennemis communs et élites (au-dessus de la tête) et méga barre des boss (Morgrim, Nyxar). Aucun
    /// message réseau : les PV des squelettes sont déjà répliqués chez les clients (EnnemiReseau, Docs/reseau.md).
    /// Recalculée une fois par image au plus, objets réutilisés (pas d'allocation à chaque image).
    public class VieEnnemisUI : IEtatVieEnnemis
    {
        /// Après le dernier coup, la barre d'un ennemi commun reste visible ce temps (interface.md).
        public const float DureeApresCoup = 2f;
        /// Paliers de comportement du boss : constante provisoire (les vrais paliers viendront plus tard).
        public const int SegmentsBoss = 4;
        /// Deux boss au plus à la fois (Quentin, 26/09/2026).
        public const int BossMax = 2;
        /// Même point d'ancrage que la rangée de statuts au-dessus des ennemis (StatutsUI.MargeTete) : la barre de vie
        /// vient juste en dessous de cette même ancre (StatutsUI l'utilise pour le bas de sa rangée).
        public static float MargeTete => StatutsUI.MargeTete;

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

        sealed class EnnemiVie : IEnnemiVie
        {
            public Vector3 PositionTete { get; set; }
            public bool Visible { get; set; }
            public float Vie { get; set; }
            public bool Elite { get; set; }
        }

        sealed class BossVie : IBossVie
        {
            public int Cle { get; set; }
            public string Nom { get; set; }
            public float Vie { get; set; }
            public int Segments => SegmentsBoss;
            public bool Vivant { get; set; }
            public readonly List<Affiche> pool = new List<Affiche>();
            public readonly List<IStatutAffiche> liste = new List<IStatutAffiche>();
            public IReadOnlyList<IStatutAffiche> Statuts => liste;
        }

        readonly List<EnnemiVie> m_PoolEnnemis = new List<EnnemiVie>();
        readonly List<IEnnemiVie> m_Ennemis = new List<IEnnemiVie>();
        readonly List<BossVie> m_PoolBoss = new List<BossVie>();
        readonly List<IBossVie> m_Boss = new List<IBossVie>();
        int m_ImageEnnemis = -1, m_ImageBoss = -1;

        public IReadOnlyList<IEnnemiVie> Ennemis
        {
            get
            {
                if (m_ImageEnnemis == Time.frameCount) return m_Ennemis;
                m_ImageEnnemis = Time.frameCount;
                m_Ennemis.Clear();
                var vivants = DirecteurVagues.Instance != null ? DirecteurVagues.Instance.Vivants : null;
                if (vivants == null) return m_Ennemis;
                int n = 0;
                for (int i = 0; i < vivants.Count; i++)
                {
                    var sq = vivants[i];
                    if (sq == null || !sq.Vivant || EstBoss(sq.type)) continue;
                    if (n >= m_PoolEnnemis.Count) m_PoolEnnemis.Add(new EnnemiVie());
                    var e = m_PoolEnnemis[n++];
                    var sante = sq.Sante;
                    bool barre = sq.elite || sante.Ratio < 0.999f || Time.time - sante.DernierCoup < DureeApresCoup;
                    e.Visible = barre && RendusVisibles(sq);
                    e.Vie = sante.Ratio;
                    e.Elite = sq.elite;
                    e.PositionTete = sq.CentreTete + Vector3.up * (sq.RayonTete + MargeTete);
                    m_Ennemis.Add(e);
                }
                return m_Ennemis;
            }
        }

        public IReadOnlyList<IBossVie> Boss
        {
            get
            {
                if (m_ImageBoss == Time.frameCount) return m_Boss;
                m_ImageBoss = Time.frameCount;
                m_Boss.Clear();
                var vivants = DirecteurVagues.Instance != null ? DirecteurVagues.Instance.Vivants : null;
                if (vivants == null) return m_Boss;
                int n = 0;
                for (int i = 0; i < vivants.Count && n < BossMax; i++)
                {
                    var sq = vivants[i];
                    if (sq == null || !EstBoss(sq.type)) continue;
                    if (n >= m_PoolBoss.Count) m_PoolBoss.Add(new BossVie());
                    var b = m_PoolBoss[n++];
                    b.Cle = sq.Id;
                    b.Nom = NomBoss(sq);
                    b.Vie = sq.Sante.Ratio;
                    b.Vivant = sq.Vivant;
                    b.liste.Clear();
                    int k = 0;
                    var st = sq.Statuts;
                    if (st != null)
                        for (int j = 0; j < st.Liste.Count; j++)
                        {
                            if (k >= b.pool.Count) b.pool.Add(new Affiche());
                            var a = b.pool[k++];
                            a.Poser(st.Liste[j]);
                            b.liste.Add(a);
                        }
                    m_Boss.Add(b);
                }
                return m_Boss;
            }
        }

        static bool EstBoss(TypeEnnemi type) => type == TypeEnnemi.Golem || type == TypeEnnemi.Necromancien;

        /// Morgrim, le Roi des os (Golem) ; Nyxar, le Nécromancien (wiki : ennemis.md, univers.md).
        /// Nyxar : éclats de Nyx restants, puis « enragé » (wiki : ennemis.md, trois phases ; 30/09/2026).
        static string NomBoss(Squelette sq)
        {
            if (!(sq is Necromancien n)) return "MORGRIM";
            int restants = (n.CouronneBrisee ? 0 : 1) + (n.GrimoireBrise ? 0 : 1);
            if (restants == 0) return "NYXAR · ENRAGÉ";
            return "NYXAR · " + restants + (restants > 1 ? " ÉCLATS" : " ÉCLAT");
        }

        /// Rendus du squelette actifs (désintégration, étage masqué du donjon : non) — comme StatutsUI.
        static bool RendusVisibles(Squelette sq)
        {
            var r = sq.RenduTete;
            return r == null || (r.enabled && r.gameObject.activeInHierarchy);
        }
    }
}
