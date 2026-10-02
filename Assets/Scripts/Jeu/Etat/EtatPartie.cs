using System;
using System.Collections.Generic;

namespace Deathless.Jeu
{
    /// Phases d'une partie. Attente : scène chargée, menu principal, aucune partie lancée.
    public enum Phase { Attente, Jour, Crepuscule, Nuit, Aube, Terminee }

    public enum Resultat { Aucun, Victoire, Defaite }

    public enum Equipe { Heros, Ennemis, Relique }

    /// Types de squelettes (wiki : ennemis.md). Voleur et Mage ajoutés le 28/09/2026 en fin de liste : la valeur passe en
    /// octet sur le réseau (EnnemiReseau) et dans les prefabs, l'ordre des anciens ne doit pas bouger.
    public enum TypeEnnemi { Sbire, Guerrier, Golem, Necromancien, Voleur, Mage }

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
        public string classeId = "paladin";
        public float jauge, jaugeMax;    // mana du mage, rage du viking (0 : pas de jauge)
        public bool furtif;              // assassin en mode furtif
        public int nuitsSurvecues;
        public int orPorte;              // or ramassé au donjon, versé à la caisse au retour par le portail
        public int pointsCompetence;     // 1 par jour survécu (crédité à l'aube), dépensés dans le menu du personnage
        public int[] rangs = new int[4]; // rangs des améliorations de compétence (ArbreCompetences), par index
        public int pointsAttribut;       // 1 par jour survécu (crédité à l'aube, en plus du point de compétence), dépensés dans le menu
        public int[] attributs = new int[Attributs.Nombre]; // points d'attribut GAGNÉS, par Attribut (la répartition de départ est celle de la classe)
        // Inventaire (03/10/2026, mécano et druide du village ; voir Inventaire) : potions et clés par sorte, crochets du kit.
        public int[] potions = new int[Inventaire.NbPotions];
        public int[] cles = new int[Inventaire.NbCles];
        public int crochets;
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
        public int palierMissiles = 1;   // paliers achetés à la relique (1 à 5)
        public int palierBouclier = 1;
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
        public bool aubeRetenue;         // temps de la nuit écoulé, l'aube attend la chute du boss (30/09/2026)
        public TypeEnnemi bossAttendu;   // ce boss (Golem : Morgrim, Necromancien : Nyxar), si aubeRetenue
        public Resultat resultat;
        public int nuitAtteinte;
        public int orEquipe;             // caisse commune (or rapporté du donjon : 0 tant qu'il n'y a pas de donjon)
        public EtatNyxessa nyxessa = new EtatNyxessa();
        public EtatVagues vagues = new EtatVagues();
        public List<EtatJoueur> joueurs = new List<EtatJoueur>();

        public float TempsRestant => Math.Max(0f, dureePhase - tempsPhase);
    }
}
