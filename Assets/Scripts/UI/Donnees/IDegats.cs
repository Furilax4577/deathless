using System;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Nature d'un chiffre de dégâts flottant (interface.md § « Barres de vie des ennemis », chiffres de dégâts) :
    /// pilote couleur et taille côté HUD (Ecrans/HudDegats.cs), jamais décidée ici.
    public enum TypeChiffreDegat
    {
        Normal,     // coup ordinaire (blanc)
        Critique,   // coup critique (or, plus gros, à-coup)
        Brulure,    // tic continu, cumulé (orange, petit)
        Recu,       // dégâts reçus par le héros local (rouge)
        Nyxessa,    // dégâts subis par Nyxessa, partagés entre tous les postes (vert, seule exception à « ses coups »)
        Soin,       // soin reçu par le héros local (doré, « +N »)
        Mot,        // Paré / Bloqué / Esquivé / Immunisé / Exécuté (pas de chiffre)
    }

    /// Un coup, un soin ou une issue à afficher en chiffre flottant (interface.md). Struct : un événement par coup,
    /// sans allocation.
    public struct EvenementDegat
    {
        /// Point du monde où faire naître le chiffre (point d'impact, ou position du héros pour un soin).
        public Vector3 Point;
        /// Montant à afficher ; ignoré si Mot n'est pas nul.
        public float Montant;
        /// "Paré", "Bloqué", "Esquivé", "Immunisé" ou "Exécuté" (assassin, 27/09/2026) ; null pour un chiffre.
        public string Mot;
        public TypeChiffreDegat Type;
        /// Tic continu (brûlure, tournante) : le HUD le cumule sur le chiffre en cours pour la même cible plutôt que
        /// d'en faire naître un nouveau (interface.md : « les tics continus se cumulent »).
        public bool Continu;
        /// Identifiant de la cible touchée, pour cumuler les tics sur elle (0 si sans objet).
        public int CleCible;
    }

    /// Chiffres de dégâts flottants (option OptionsJoueur.AfficherDegats, activée par défaut, interface.md). Posée par
    /// le jeu dans DonneesUI.Degats (Deathless.Jeu.DegatsUI, créée et actualisée par HudPresenter) ; le HUD
    /// (Ecrans/HudDegats.cs) s'y abonne, sans rien lire image par image : un événement par coup, un pool d'éléments
    /// réutilisés (trente au plus). Chaque poste ne reçoit que ses propres coups portés, ce qu'il reçoit lui-même, et
    /// les dégâts subis par Nyxessa (partagés, jamais les coups des autres joueurs). Option désactivée : plus aucun
    /// événement n'est levé (les barres de vie restent, elles, inchangées).
    public interface IDegatsSource
    {
        event Action<EvenementDegat> Degat;
    }
}
