using System;

namespace Deathless.UI.Donnees
{
    /// Phase du cycle (voir main/Wiki/pages/deroule.md) : jour 120 s, crépuscule 5 s, nuit 120 s, aube 5 s.
    public enum PhasePartie
    {
        Jour,
        Crepuscule,
        Nuit,
        Aube,
        /// Partie finie (victoire ou Nyxessa détruite) : l'écran de score s'affiche.
        Terminee,
    }

    /// État de la partie lu par le HUD. Les propriétés sont lues à chaque image (valeurs courantes, pas de cache côté
    /// UI) ; les événements signalent les instants à mettre en scène (bannière de nuit, coup sur Nyxessa, fin).
    /// Implémenté par le jeu (dans main), et par EtatFactice pour la démo.
    public interface IEtatPartie
    {
        /// Phase en cours.
        PhasePartie Phase { get; }

        /// Numéro de la nuit : la nuit en cours pendant Nuit et Aube, la prochaine nuit pendant Jour et Crépuscule
        /// (1 au premier jour). Sert à « Nuit 3 · 2:10 avant l'aube » et à la bannière « NUIT 3 ».
        int NumeroNuit { get; }

        /// Secondes restantes dans la phase en cours (≥ 0). Le HUD affiche « Jour · 1:42 avant la nuit ».
        float TempsRestantPhase { get; }

        /// Points de vie de Nyxessa et maximum (> 0).
        float VieNyxessa { get; }
        float VieMaxNyxessa { get; }

        /// Bouclier de Nyxessa : points restants et maximum. BouclierMax = 0 quand il n'y a pas de bouclier
        /// (non invoqué) ; la barre du bouclier est alors masquée. Couleur : bleu, orange, puis rouge quand il faiblit.
        float Bouclier { get; }
        float BouclierMax { get; }

        /// Or de la caisse commune de l'équipe.
        int OrEquipe { get; }

        /// Vote « prêt » (jour seulement). VoteActif est faux si un joueur est au donjon (ou hors du jour) :
        /// le HUD cache alors l'invite. JoueursPrets / JoueursTotal : « Prêts 1 / 1 ».
        bool VoteActif { get; }
        int JoueursPrets { get; }
        int JoueursTotal { get; }

        /// Angle horizontal de Nyxessa vu de la caméra du joueur local, en degrés (-180 à 180 ; 0 = droit devant,
        /// positif = à droite). Sert à placer l'indicateur de bord d'écran « Nyxessa attaquée ».
        float AngleNyxessa { get; }

        /// Une nuit commence (numéro de la nuit) : bannière « NUIT N ».
        event Action<int> NuitCommencee;

        /// Nyxessa ou son bouclier vient d'être frappé : indicateur « Nyxessa attaquée » (le HUD le garde
        /// quelques secondes après le dernier coup, inutile d'appeler à chaque image).
        event Action NyxessaFrappee;

        /// La partie est finie : les données de l'écran de score sont prêtes (DonneesUI.Score).
        event Action PartieTerminee;
    }
}
