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

        static readonly System.Collections.Generic.List<ZoneEau> s_Toutes = new System.Collections.Generic.List<ZoneEau>();
        BoxCollider m_Boite;

        void OnEnable() { m_Boite = GetComponent<BoxCollider>(); s_Toutes.Add(this); }
        void OnDisable() { s_Toutes.Remove(this); }

        /// Facteur de vitesse au point `p` (pieds d'un personnage) : celui du bassin qui le contient, 1 hors de l'eau.
        /// Sans déclencheur (héros et squelettes l'interrogent à chaque image ; un ou deux bassins au plus).
        public static float FacteurEn(Vector3 p)
        {
            for (int i = 0; i < s_Toutes.Count; i++)
            {
                var z = s_Toutes[i];
                if (z.m_Boite != null && z.m_Boite.enabled && z.m_Boite.bounds.Contains(p)) return z.facteurVitesse;
            }
            return 1f;
        }

        /// Facteur de vitesse d'un collider qui entre dans ce volume (1 hors de l'eau).
        public static float Facteur(Collider c)
        {
            ZoneEau z;
            return c != null && c.TryGetComponent(out z) ? z.facteurVitesse : 1f;
        }
    }
}
