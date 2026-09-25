using System;
using System.Collections.Generic;

namespace Deathless.Jeu
{
    /// Phases d'une partie. Attente : scène chargée, menu principal, aucune partie lancée.
    public enum Phase { Attente, Jour, Crepuscule, Nuit, Aube, Terminee }

    public enum Resultat { Aucun, Victoire, Defaite }

    public enum Equipe { Heros, Ennemis, Relique }

    public enum TypeEnnemi { Sbire, Guerrier, Golem, Necromancien }

    /// Statistiques de score d'un joueur (écran de score : sept catégories).
    [Serializable]
    public class ScoreJoueur
    {
        public int orRapporte;
        public float degatsInfliges;
        public int ennemisTues;
        public int morts;
        public int coupsCritiques;
        public float degatsEvitesNyxessa;
        public float soinsProdigues;
    }

    /// État d'un joueur tel que l'autorité le connaît (données pures, synchronisables plus tard).
    [Serializable]
    public class EtatJoueur
    {
        public int id;
        public string nom = "Joueur";
        public string classe = "Paladin";
        public float pv, pvMax;
        public float endurance, enduranceMax;
        public bool mort;
        public float reapparitionRestante;
        public bool pret;
        public float rechargeCharge, rechargeSoin;
        public ScoreJoueur score = new ScoreJoueur();
    }

    [Serializable]
    public class EtatNyxessa
    {
        public float pv, pvMax;
        public int stock;
        public float regeneration;       // temps écoulé vers le prochain missile
        public float depuisDernierTir = 99f;
        public float dernierCoup = -99f; // Time.time du dernier coup reçu
        public bool detruite;
    }

    [Serializable]
    public class EtatVagues
    {
        public int vague;                // vague lancée (0 : aucune)
        public int total;
        public int ennemisVivants;
        public int restantsASortir;
        public List<int> clairieresActives = new List<int>();
    }

    /// Tout l'état qui fait foi, possédé par Partie.
    [Serializable]
    public class EtatPartie
    {
        public Phase phase = Phase.Attente;
        public float tempsPhase;         // temps écoulé dans la phase
        public float dureePhase;
        public int nuit = 1;             // nuit en cours (Nuit, Aube) ou prochaine (Jour, Crépuscule)
        public float duree;              // durée de la partie (s)
        public bool comptePret;          // tous prêts : le jour a été ramené au compte à rebours
        public Resultat resultat;
        public int nuitAtteinte;
        public EtatNyxessa nyxessa = new EtatNyxessa();
        public EtatVagues vagues = new EtatVagues();
        public List<EtatJoueur> joueurs = new List<EtatJoueur>();

        public float TempsRestant => Math.Max(0f, dureePhase - tempsPhase);
    }
}
