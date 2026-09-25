using UnityEngine;

namespace Deathless.Donjon
{
    /// Parent d'un groupe (bloc, niveau) de collisions : permet de savoir si un collider appartient à un étage masqué
    /// (la caméra le traverse alors, voir DonjonMasquage.ColliderMasque).
    public class DonjonGroupe : MonoBehaviour
    {
        public int index;
        public DonjonMasquage masquage;
    }
}
