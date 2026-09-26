#if UNITY_EDITOR
using System.Collections.Generic;
using Deathless.UI.Donnees;

namespace Deathless.Dev.Tournage
{
    /// Salon scripté pour le plan « coop » de la bande-annonce : le vrai écran de lobby (EcranLobby) lit ce salon au lieu
    /// du réseau. Quatre joueurs, un code de salon, les joueurs passent « prêt » un par un aux instants donnés
    /// (MettreA, appelée par le scénario avec le temps du plan). Repli du storyboard (§ 5) : réunir quatre postes réels
    /// n'est pas possible en solo.
    public class LobbyTournage : ILobby
    {
        sealed class Joueur : IJoueurLobby
        {
            public string Pseudo { get; set; }
            public string ClasseId { get; set; }
            public bool Pret { get; set; }
            public bool EstLocal { get; set; }
            public bool EstHote { get; set; }
            public float arrivee, pret;
        }

        readonly List<Joueur> m_Tous = new List<Joueur>();
        readonly List<IJoueurLobby> m_Vue = new List<IJoueurLobby>();

        public EtatLobby Etat { get; set; } = EtatLobby.Aucun;
        public string CodeSalon { get; set; } = "";
        public bool EstHote => false;
        public IReadOnlyList<IJoueurLobby> Joueurs => m_Vue;
        public int JoueursMax => 4;
        public float CompteARebours { get; set; }
        public string Message { get; set; } = "";

        public void Ajouter(string pseudo, string classe, bool local, bool hote, float arrivee, float pret)
            => m_Tous.Add(new Joueur { Pseudo = pseudo, ClasseId = classe, EstLocal = local, EstHote = hote, arrivee = arrivee, pret = pret });

        /// État du salon à l'instant `t` du plan. Renvoie le nombre de joueurs prêts (pour les sons d'interface).
        public int MettreA(float t)
        {
            m_Vue.Clear();
            int prets = 0;
            foreach (var j in m_Tous)
            {
                if (t < j.arrivee) continue;
                j.Pret = t >= j.pret;
                if (j.Pret) prets++;
                m_Vue.Add(j);
            }
            return prets;
        }

        public void CreerSalon() { }
        public void Rejoindre(string code) { }
        public void RejoindreParAdresse(string adresse) { }
        public bool ChoisirClasse(string classeId) => false;
        public void BasculerPret() { }
        public void LancerMaintenant() { }
        public void Quitter() { }
    }
}
#endif
