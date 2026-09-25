using UnityEngine;

namespace Deathless.Donjon
{
    /// Volume d'eau du donjon (bassin) : l'eau arrive aux genoux (0,5 m) et ralentit les déplacements.
    /// Déclencheur posé par le générateur sur la couche « Ignore Raycast ». À relier dans main : le héros et les
    /// squelettes multiplient leur vitesse par `facteurVitesse` tant qu'ils sont dedans (OnTriggerEnter / Exit).
    /// Le NavMesh en tient compte par la zone « Eau » (coût 1 / facteurVitesse) : les squelettes contournent le
    /// bassin quand c'est plus court en temps.
    [RequireComponent(typeof(BoxCollider))]
    public class ZoneEau : MonoBehaviour
    {
        [Range(0.1f, 1f)] public float facteurVitesse = DonjonPlan.FacteurVitesseEau;
        [Tooltip("Hauteur d'eau au-dessus du fond (m).")]
        public float profondeur = DonjonPlan.ProfondeurEau;

        /// Facteur de vitesse d'un collider qui entre dans ce volume (1 hors de l'eau).
        public static float Facteur(Collider c)
        {
            ZoneEau z;
            return c != null && c.TryGetComponent(out z) ? z.facteurVitesse : 1f;
        }
    }
}
