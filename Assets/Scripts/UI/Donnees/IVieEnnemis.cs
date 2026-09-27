using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Un ennemi commun ou élite dont la vie peut s'afficher au-dessus de la tête (interface.md, « Barres de vie des
    /// ennemis »). Jamais un boss (Golem, Nécromancien) : eux passent par IBossVie, pas de barre au-dessus d'eux.
    public interface IEnnemiVie
    {
        /// Point au-dessus de la tête où poser la barre, juste sous la rangée de statuts (même ancre : IEnnemiAffecte
        /// et IEnnemiVie visent le même point, StatutsUI.MargeTete, pour que le petit bloc bouge d'un seul tenant).
        Vector3 PositionTete { get; }
        /// Faux : rien à montrer (rendus coupés, ou à pleine vie depuis plus de 2 s sans être un élite). Le HUD
        /// applique en plus sa propre limite de champ et de distance (comme les statuts).
        bool Visible { get; }
        /// Vie courante, 0 à 1.
        float Vie { get; }
        /// Élite : barre toujours visible, plus large, liseré rouge (posé par le HUD via une classe USS).
        bool Elite { get; }
    }

    /// Un boss (Morgrim, Nyxar) : méga barre du HUD, en haut au centre sous la barre de Nyxessa (deux au plus, empilées).
    public interface IBossVie
    {
        /// Identifiant stable le temps que le boss vit (et le court instant après sa mort, le temps du repli) : permet
        /// au HUD de garder le même emplacement (boss1 ou boss2) et d'animer déploiement et repli d'une image à l'autre.
        int Cle { get; }
        string Nom { get; }
        /// Vie courante, 0 à 1.
        float Vie { get; }
        /// Nombre de segments de la piste (paliers de comportement à venir ; constante provisoire pour l'instant).
        int Segments { get; }
        /// Vrai de l'apparition à la mort (pilote le déploiement) ; faux après la mort (pilote le repli, puis le boss
        /// disparaît de la liste une fois replié).
        bool Vivant { get; }
        /// Étourdi, ralenti… accrochés à la barre plutôt qu'au-dessus du boss.
        IReadOnlyList<IStatutAffiche> Statuts { get; }
    }

    /// Vie des ennemis (barres au-dessus de la tête) et des boss (méga barre du HUD, interface.md § « Barres de vie
    /// des ennemis »). Posée par le jeu dans DonneesUI.VieEnnemis (Deathless.Jeu.VieEnnemisUI, créée par
    /// HudPresenter), lue à chaque image par Ecrans/HudVieEnnemis.cs. Absente : rien n'est affiché. Aucun message
    /// réseau à part : les PV des squelettes sont déjà répliqués chez les clients (EnnemiReseau, Docs/reseau.md).
    public interface IEtatVieEnnemis
    {
        IReadOnlyList<IEnnemiVie> Ennemis { get; }
        /// Deux au plus (Quentin, 26/09/2026).
        IReadOnlyList<IBossVie> Boss { get; }
    }
}
