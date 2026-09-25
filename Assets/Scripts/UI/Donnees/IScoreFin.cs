using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    public enum ResultatPartie
    {
        /// Survie à la nuit 12 et à son boss, à l'aube.
        Victoire,
        /// Nyxessa détruite.
        Defaite,
    }

    /// Une ligne de l'écran de score (un joueur).
    public interface ILigneScore
    {
        string Nom { get; }
        string Classe { get; }
        Color TeinteClasse { get; }
        /// Vrai pour le joueur local (ligne soulignée).
        bool EstLocal { get; }

        /// Catégories décidées (deroule.md). Meilleur : le plus élevé, sauf Morts (le plus bas).
        int OrRapporte { get; }
        int DegatsInfliges { get; }
        int EnnemisTues { get; }
        int Morts { get; }
    }

    /// Données de l'écran de score, valables après IEtatPartie.PartieTerminee.
    public interface IScoreFin
    {
        ResultatPartie Resultat { get; }

        /// Nuit atteinte (défaite : la nuit où Nyxessa est tombée ; victoire : 12).
        int NuitAtteinte { get; }

        /// Durée de la partie en secondes.
        float DureeSecondes { get; }

        /// Or total de l'équipe.
        int OrTotal { get; }

        IReadOnlyList<ILigneScore> Joueurs { get; }

        /// Vote « Rejouer » : prêts / total. Quand tout le monde est prêt, le jeu relance une partie.
        int JoueursPrets { get; }
        int JoueursTotal { get; }
        bool EstPretLocal { get; }
    }
}
