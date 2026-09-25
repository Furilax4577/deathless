using System.Collections.Generic;

namespace Deathless.UI.Donnees
{
    /// Une ligne d'un menu d'achat (palier de Nyxessa, plat de la taverne…).
    public interface IArticleAchat
    {
        string Nom { get; }
        /// Ce que l'achat apporte (valeurs du niveau suivant).
        string Description { get; }
        /// Niveau atteint (« Palier 2 / 5 ») ou vide.
        string Niveau { get; }
        /// Prix en or, ou -1 si l'article n'est plus achetable (niveau maximal).
        int Prix { get; }
        /// Achetable maintenant (or suffisant, moment permis).
        bool Achetable { get; }
    }

    /// Menu d'achat ouvert par une interaction du jeu (touche Interagir : E, X, Carré) : achats à la relique, taverne.
    /// Le jeu l'implémente ; l'écran EcranAchat le lit à chaque image et appelle Acheter. Le jeu décide (et, en
    /// multijoueur, l'hôte) : l'écran ne connaît que ces textes et ces prix.
    public interface IMenuAchat
    {
        string Titre { get; }
        string SousTitre { get; }
        /// Or disponible (caisse commune) et sa légende.
        int Or { get; }
        string LegendeOr { get; }
        IReadOnlyList<IArticleAchat> Articles { get; }
        /// Faux dès que le menu n'a plus lieu d'être (nuit tombée, joueur éloigné ou mort) : l'écran se ferme.
        bool Ouvert { get; }
        /// Dernier message (achat fait, refus), ou vide.
        string Message { get; }
        /// Le message est un refus (or insuffisant…) : affiché en rouge ; sinon en or.
        bool MessageRefus { get; }
        void Acheter(int index);
    }
}
