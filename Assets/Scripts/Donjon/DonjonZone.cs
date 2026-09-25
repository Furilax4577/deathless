using UnityEngine;

namespace Deathless.Donjon
{
    /// Volume déclencheur d'une zone du donjon : un bloc de 12 x 12 m à un niveau (4 m de haut). Posé une fois pour
    /// toutes par le générateur (la grille est la même pour toutes les graines), sur la couche « Ignore Raycast » :
    /// les caméras et les tirs ne le touchent pas, le NavMesh l'ignore.
    public class DonjonZone : MonoBehaviour
    {
        public int bloc, niveau;
        public DonjonMasquage masquage;
    }
}
