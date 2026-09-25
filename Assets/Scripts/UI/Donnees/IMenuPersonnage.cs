using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Une amélioration de compétence du menu du personnage (rang, ce qu'elle apporte).
    public interface IAmeliorationCompetence
    {
        string Nom { get; }
        /// Effet par rang (« +10 % de dégâts à l'épée ») et effet actuel.
        string Description { get; }
        /// Icône de la compétence (catalogue IconesUI, ex. « paladin_epee »).
        string Icone { get; }
        int Rang { get; }
        int RangMax { get; }
        /// Améliorable maintenant (point disponible, rang non maximal).
        bool Possible { get; }
    }

    /// Menu du personnage (touche Tab, Y, Triangle) : le personnage, l'inventaire (vide pour l'instant) et l'amélioration
    /// des compétences avec les points de compétence (1 par jour survécu). Le jeu l'implémente et le pose dans
    /// DonneesUI.Personnage ; l'écran EcranPersonnage le lit à chaque image.
    public interface IMenuPersonnage
    {
        string Nom { get; }
        string Classe { get; }
        string Embleme { get; }
        Color Teinte { get; }
        /// Caractéristiques affichées (libellé, valeur) : vie, endurance, vitesse, jauge, nuits survécues…
        IReadOnlyList<KeyValuePair<string, string>> Caracteristiques { get; }
        /// Points de compétence à dépenser.
        int Points { get; }
        IReadOnlyList<IAmeliorationCompetence> Ameliorations { get; }
        /// Faux quand le menu n'a plus lieu d'être (partie finie) : l'écran se ferme.
        bool Ouvert { get; }
        string Message { get; }
        bool MessageRefus { get; }
        void Ameliorer(int index);
    }
}
