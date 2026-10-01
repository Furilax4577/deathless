using System.Collections.Generic;
using UnityEngine;

namespace Deathless.UI.Donnees
{
    /// Un statut tel que l'interface l'affiche (HUD du joueur, au-dessus des ennemis, menu du personnage).
    public interface IStatutAffiche
    {
        /// Nom (« Brûlure », « Ralenti »).
        string Nom { get; }
        /// Identifiant de l'icône (table IconesUI, ex. « statut_brulure »).
        string Icone { get; }
        /// Effet en clair, avec ses valeurs (« 5 dégâts par seconde »).
        string Effet { get; }
        /// Source (« Morgane (Mage) », « Chute », « Taverne »).
        string Source { get; }
        /// Secondes restantes ; négatif : sans durée (tant que la cause dure, par exemple dans l'eau).
        float Restant { get; }
        /// Durée totale du statut (jauge : Restant / Duree).
        float Duree { get; }
        /// Affliction (liseré rouge) ou bienfait (liseré or).
        bool Nefaste { get; }
        /// Palier d'un statut qui se cumule (Brûlure : 1 au plafond, chiffre sur l'icône) ; 0 : sans palier.
        int Palier { get; }
        /// Jauge de cumul vers le palier suivant (0 à 1, Brûlure), montrée à la place de la jauge de durée ; négatif : aucune.
        float Jauge { get; }
    }

    /// Un ennemi affecté par au moins un statut.
    public interface IEnnemiAffecte
    {
        /// Point au-dessus de la tête où poser la rangée d'icônes (monde).
        Vector3 PositionTete { get; }
        /// Faux s'il ne doit pas être montré (rendus coupés : désintégration, étage masqué du donjon).
        bool Visible { get; }
        IReadOnlyList<IStatutAffiche> Statuts { get; }
    }

    /// Statuts du joueur local et des ennemis (Docs/ui-v01.md, « Statuts »). Le jeu l'implémente et le pose dans
    /// DonneesUI.Statuts ; le HUD et le menu du personnage le lisent à chaque image. Absent : rien n'est affiché.
    public interface IEtatStatuts
    {
        IReadOnlyList<IStatutAffiche> StatutsJoueur { get; }
        IReadOnlyList<IEnnemiAffecte> EnnemisAffectes { get; }
    }
}
