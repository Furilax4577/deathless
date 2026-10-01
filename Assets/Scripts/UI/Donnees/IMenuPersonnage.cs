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

    /// Un attribut du menu du personnage (Force, Endurance, Agilité, Perception, Esprit, Chance ; 01/10/2026).
    public interface IAttributPersonnage
    {
        string Nom { get; }
        /// Valeur actuelle (départ de la classe + points gagnés) et plafond (10).
        int Valeur { get; }
        int Depart { get; }
        int Plafond { get; }
        /// Effet d'un point gagné, en clair, et total déjà gagné (« +6 % dégâts… », vide sans point gagné).
        string Effet { get; }
        string Actuel { get; }
        /// Un point peut y aller maintenant (point disponible, plafond non atteint).
        bool Possible { get; }
    }

    /// Section « Attributs » du menu du personnage, facultative (le menu qui l'implémente la montre) : 1 point d'attribut
    /// par aube survécue, en plus du point de compétence ; plafond 10, pas de réattribution.
    public interface IAttributsPersonnage
    {
        /// Points d'attribut à dépenser.
        int PointsAttribut { get; }
        IReadOnlyList<IAttributPersonnage> Attributs { get; }
        void AmeliorerAttribut(int index);
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
