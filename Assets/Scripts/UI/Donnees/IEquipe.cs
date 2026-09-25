using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Un autre joueur de la partie (multijoueur), pour la colonne de gauche du HUD (Wiki : interface.md, « Vie des
    /// autres joueurs »). Lu à chaque image.
    public interface IAllie
    {
        string Pseudo { get; }
        /// Id de la classe (catalogue ClassesJouables) ; Nom, teinte et emblème s'en déduisent.
        string ClasseId { get; }
        float Vie { get; }
        float VieMax { get; }
        bool EstMort { get; }
        float TempsAvantReapparition { get; }
        /// Point au-dessus de la tête (monde) où le HUD écrit le pseudo ; null : pas d'étiquette (hors scène, banc).
        Vector3? PositionTete { get; }
    }

    /// Équipe vue par le joueur local : les autres joueurs de la partie (vide en solo). Facultatif : l'objet enregistré
    /// comme IEtatPartie l'implémente en multijoueur (rétrocompatible : sans lui, la colonne reste vide).
    public interface IEtatEquipe
    {
        /// Autres joueurs, dans l'ordre des emplacements du salon (liste stable ; le HUD la relit à chaque image).
        IReadOnlyList<IAllie> Allies { get; }
    }
}
