using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Achats à la relique (wiki : nyxessa, Paliers ; décision du 26/09/2026) : de jour, près de Nyxessa, la touche
    /// Interagir ouvre le menu d'achat des paliers (missiles de Nyxessa, bouclier du sorcier), payés par la caisse
    /// commune (100, 200, 350 et 550 or pour les paliers 2 à 5). L'autorité décide (Partie.Acheter ; en multijoueur,
    /// l'hôte). Posé sur Nyxessa par Partie au démarrage.
    public class AchatRelique : PointInteraction, IMenuAchat
    {
        sealed class Article : IArticleAchat
        {
            public Partie.Amelioration a;
            public string Nom { get; set; }
            public string Description { get; set; }
            public string Niveau { get; set; }
            public int Prix { get; set; }
            public bool Achetable { get; set; }
        }

        readonly List<IArticleAchat> m_Articles = new List<IArticleAchat>
        {
            new Article { a = Partie.Amelioration.Missiles, Nom = Partie.NomAmelioration(Partie.Amelioration.Missiles) },
            new Article { a = Partie.Amelioration.Bouclier, Nom = Partie.NomAmelioration(Partie.Amelioration.Bouclier) },
        };
        Heros m_Heros;
        string m_Message = "";
        bool m_Refus;

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;

        protected override void OnEnable()
        {
            base.OnEnable();
            Deathless.Reseau.PartieReseau.ReponseAchat += OnReponse;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Deathless.Reseau.PartieReseau.ReponseAchat -= OnReponse;
        }

        void OnReponse(string message) { m_Message = message; m_Refus = !message.Contains("acheté"); }

        float Distance(Heros h)
        {
            Vector3 d = h.transform.position - transform.position; d.y = 0f;
            return d.magnitude;
        }

        bool Disponible(Heros h) => h != null && h.Vivant && P != null && P.EnCours && P.Etat.phase == Phase.Jour && !P.Etat.nyxessa.detruite;

        public override string Invite(Heros h, out float distance)
        {
            distance = h != null ? Distance(h) : float.MaxValue;
            return Disponible(h) && distance <= B.achatDistance ? "Améliorer Nyxessa" : null;
        }

        public override void Interagir(Heros h)
        {
            m_Heros = h;
            m_Message = "";
            DonneesUI.OuvrirMenuAchat(this);
        }

        // ----------------------------------------------------------------- IMenuAchat

        public string Titre => "Nyxessa";
        public string SousTitre => "Améliorations de la relique, payées par la caisse commune.";
        public int Or => P != null ? P.Etat.orEquipe : 0;
        public string LegendeOr => "caisse commune";
        public string Message => m_Message;
        public bool MessageRefus => m_Refus;
        public bool Ouvert => Disponible(m_Heros) && Distance(m_Heros) <= B.achatDistance + 2f;

        public IReadOnlyList<IArticleAchat> Articles
        {
            get
            {
                var p = P; var b = B;
                foreach (Article art in m_Articles)
                {
                    int palier = p != null ? p.PalierDe(art.a) : 1;
                    int suivant = Mathf.Min(GameBalance.PalierMax, palier + 1);
                    art.Niveau = "Palier " + palier + " / " + GameBalance.PalierMax;
                    art.Prix = b.PrixPalierSuivant(palier);
                    art.Achetable = p != null && p.RefusAchat(art.a, out _) == null;
                    if (art.Prix < 0) art.Description = "Palier maximal atteint.";
                    else if (art.a == Partie.Amelioration.Missiles)
                        art.Description = "Palier " + suivant + " : " + GameBalance.AuPalier(b.missilesStockPaliers, suivant) + " missiles en stock, un de plus toutes les "
                            + GameBalance.AuPalier(b.missileRegenerationPaliers, suivant).ToString("0.#") + " s, " + GameBalance.AuPalier(b.missileDegatsPaliers, suivant).ToString("0") + " dégâts.";
                    else
                        art.Description = "Palier " + suivant + " : encaisse " + b.Palier(b.bouclierEncaissement, suivant).ToString("0") + " dégâts, en renvoie "
                            + b.Palier(b.bouclierRenvoi, suivant).ToString("0") + " à chaque coup reçu.";
                }
                return m_Articles;
            }
        }

        public void Acheter(int index)
        {
            if (P == null || index < 0 || index >= m_Articles.Count) return;
            var art = (Article)m_Articles[index];
            string refus = P.RefusAchat(art.a, out _);
            if (refus != null)
            {
                m_Message = refus;
                m_Refus = true;
                AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.7f);
                return;
            }
            m_Message = P.Acheter(art.a, m_Heros != null ? m_Heros.Id : 1);
            m_Refus = false;
        }
    }
}
