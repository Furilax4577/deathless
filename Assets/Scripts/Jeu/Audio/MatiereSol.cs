using UnityEngine;

namespace Deathless.Jeu
{
    /// Matière d'un sol (bruits de pas, 02/10/2026) : l'ordre est celui de SonsDuJeu.PasParMatiere.
    public enum Matiere { Herbe, Terre, Pierre, Bois, Metal, Sable, Eau }

    /// Marqueur posé sur un collider (ou sur un de ses parents : le plus proche gagne) : la matière sous les pieds de
    /// qui marche dessus. Posé par Deathless > Village > Poser les matières du sol (MatieresSolBuilder) à partir de la
    /// hiérarchie de la carte. `parPalette` : le sol de la carte (un seul maillage dont chaque triangle prend une teinte
    /// d'une palette, SolVillage_Palette*.png) : la matière vient de la teinte sous le point, voir PasMatiere.
    /// Un collider sans marqueur compte comme de la pierre (décor dur : rochers, murs, sols du donjon).
    [DisallowMultipleComponent]
    public class MatiereSol : MonoBehaviour
    {
        public Matiere matiere = Matiere.Pierre;
        [Tooltip("Sol de la carte (maillage à palette) : la matière vient de la teinte de la palette sous le point.")]
        public bool parPalette;
        [Tooltip("Nombre de teintes de la palette du sol (VillageBuilder.GroundPalette) : lu avec la coordonnée u du triangle touché.")]
        public int casesPalette = 12;
    }
}
