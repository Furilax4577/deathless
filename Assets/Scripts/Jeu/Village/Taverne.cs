using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Taverne (Maison_1_A ; décision de Quentin du 26/09/2026) : de jour seulement, au comptoir, la touche Interagir
    /// ouvre le menu du tavernier : se restaurer (un peu de vie pour de l'or), boire une bière (ivre quelques secondes),
    /// payer une tournée (tous les joueurs ivres quelques secondes). L'or est pris dans la caisse commune ; l'autorité
    /// décide (Partie.PayerTaverne ; en multijoueur, l'hôte). Des breuvages viendront plus tard. Posé sur l'ancre
    /// d'échange de l'intérieur de la taverne par Partie au démarrage.
    public class Taverne : PointInteraction, IMenuAchat
    {
        public enum Article { Repas, Biere, Tournee }

        sealed class Ligne : IArticleAchat
        {
            public Article a;
            public string Nom { get; set; }
            public string Description { get; set; }
            public string Niveau => "";
            public int Prix { get; set; }
            public bool Achetable { get; set; }
        }

        readonly List<IArticleAchat> m_Lignes = new List<IArticleAchat>
        {
            new Ligne { a = Article.Repas, Nom = "Se restaurer" },
            new Ligne { a = Article.Biere, Nom = "Boire une bière" },
            new Ligne { a = Article.Tournee, Nom = "Payer une tournée" },
        };
        Heros m_Heros;
        static string s_Message = "";
        static bool s_Refus;

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;

        public static int Prix(Article a)
        {
            var b = GameBalance.Courant;
            return a == Article.Repas ? b.tavernePrixRepas : a == Article.Biere ? b.tavernePrixBiere : b.tavernePrixTournee;
        }

        float Distance(Heros h)
        {
            Vector3 d = h.transform.position - transform.position; d.y = 0f;
            return d.magnitude;
        }

        bool Disponible(Heros h) => h != null && h.Vivant && P != null && P.EnCours && P.Etat.phase == Phase.Jour;

        public override string Invite(Heros h, out float distance)
        {
            distance = h != null ? Distance(h) : float.MaxValue;
            return Disponible(h) && distance <= B.taverneDistance ? "Taverne : se restaurer, boire" : null;
        }

        public override void Interagir(Heros h)
        {
            m_Heros = h;
            s_Message = "";
            DonneesUI.OuvrirMenuAchat(this);
        }

        // ----------------------------------------------------------------- IMenuAchat

        public string Titre => "Taverne";
        public string SousTitre => "Le tavernier sert de jour. L’or est pris dans la caisse commune.";
        public int Or => P != null ? P.Etat.orEquipe : 0;
        public string LegendeOr => "caisse commune";
        public string Message => s_Message;
        public bool MessageRefus => s_Refus;
        public bool Ouvert => Disponible(m_Heros) && Distance(m_Heros) <= B.taverneDistance + 1.5f;

        public IReadOnlyList<IArticleAchat> Articles
        {
            get
            {
                var b = B;
                foreach (Ligne l in m_Lignes)
                {
                    l.Prix = Prix(l.a);
                    l.Achetable = P != null && P.RefusTaverne(l.a, m_Heros) == null;
                    l.Description = l.a == Article.Repas ? "Un bol de ragoût chaud : +" + b.taverneSoinRepas.ToString("0") + " points de vie."
                        : l.a == Article.Biere ? "Une chope bien fraîche. La tête tourne un peu, pendant " + b.ivresseBiere.ToString("0") + " s."
                        : "Une chope pour tout le monde : tous les joueurs sont ivres pendant " + b.ivresseTournee.ToString("0") + " s.";
                }
                return m_Lignes;
            }
        }

        public void Acheter(int index)
        {
            if (P == null || index < 0 || index >= m_Lignes.Count) return;
            var a = ((Ligne)m_Lignes[index]).a;
            string refus = P.RefusTaverne(a, m_Heros);
            if (refus != null) { s_Message = refus; s_Refus = true; AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.7f); return; }
            s_Message = P.PayerTaverne(a, m_Heros != null ? m_Heros.Id : 1, out bool ok);
            s_Refus = !ok;
        }

        /// Réponse de l'hôte (client) : message et, si l'achat est fait, son effet sur le joueur local.
        public static void Reponse(string message, bool ok, Article a)
        {
            s_Message = message; s_Refus = !ok;
            if (ok && a != Article.Tournee) AppliquerLocal(a);   // la tournée arrive chez tous par Partie.Tournee
        }

        /// Effet d'un article sur le joueur local (repas, bière ; la tournée passe par Partie.Tournee chez tous).
        public static void AppliquerLocal(Article a)
        {
            var p = Partie.Instance; var h = p != null ? p.HerosLocal : null;
            var b = GameBalance.Courant;
            if (h == null) return;
            if (a == Article.Repas) { h.Sante.Soigner(b.taverneSoinRepas); AudioBank.Jouer(SonsDuJeu.Repas, h.transform.position + Vector3.up, 0.8f); }
            else if (a == Article.Biere) { Ivresse.Commencer(b.ivresseBiere); AudioBank.Jouer(SonsDuJeu.Biere, h.transform.position + Vector3.up, 0.8f); }
        }
    }
}
