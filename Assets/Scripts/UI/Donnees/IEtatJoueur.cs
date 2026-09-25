using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// État d'un emplacement de la barre de compétences.
    public enum EtatCompetence
    {
        /// Utilisable.
        Prete,
        /// En recharge : le HUD grise l'emplacement et affiche les secondes restantes.
        Recharge,
        /// En cours (garde levée, charge en cours…) : bordure or.
        Active,
        /// Impossible pour une autre raison (pas assez d'endurance, joueur mort…).
        Indisponible,
    }

    /// Un emplacement de la barre de compétences (attaque, garde et parade, charge bélier, soin sur soi…).
    public interface ICompetenceHud
    {
        /// Action de DeathlessControls qui la déclenche, « Carte/Action » (ex. « Gameplay/Skill1 »). Le HUD en tire
        /// l'invite de bouton du dernier appareil utilisé.
        string Action { get; }

        /// Nom affiché au survol et dans la doc (ex. « Charge bélier »).
        string Nom { get; }

        /// Icône (peut être null : le HUD affiche alors Abreviation).
        Texture2D Icone { get; }

        /// Deux lettres affichées sans icône (ex. « Ch »).
        string Abreviation { get; }

        EtatCompetence Etat { get; }

        /// Secondes de recharge restantes et durée totale (0 si pas de recharge). Lu à chaque image.
        float RechargeRestante { get; }
        float RechargeTotale { get; }
    }

    /// État du joueur local lu par le HUD (lu à chaque image).
    public interface IEtatJoueur
    {
        /// Pseudo et classe (ex. « Quentin », « Paladin »).
        string Nom { get; }
        string Classe { get; }

        /// Teinte du portrait (Paladin : or #d9b264) et initiale affichée dedans.
        Color TeinteClasse { get; }

        float Vie { get; }
        float VieMax { get; }
        float Endurance { get; }
        float EnduranceMax { get; }

        /// Mort : le HUD affiche « Mort, réapparition dans N s » tant que EstMort est vrai.
        bool EstMort { get; }
        float TempsAvantReapparition { get; }

        /// Emplacements de la barre de compétences, de gauche à droite (liste stable pendant la partie).
        IReadOnlyList<ICompetenceHud> Competences { get; }

        /// Action possible devant le joueur (ex. « Parler au sorcier »), ou null / vide si aucune.
        /// Le HUD l'affiche au centre avec l'invite de Gameplay/Interact.
        string InviteInteraction { get; }

        /// Vrai si le joueur local s'est déclaré prêt (vote du jour).
        bool EstPret { get; }
    }
}
