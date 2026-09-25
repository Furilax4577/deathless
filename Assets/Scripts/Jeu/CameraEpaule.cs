using UnityEngine;

namespace Deathless.Jeu
{
    /// Caméra orbitale derrière l'épaule droite du héros (troisième personne ; la vue FPS est proscrite). Lacet et
    /// tangage pilotés par la souris ou le stick droit (HerosEntrees.Regard), collision par SphereCast (les personnages
    /// sont ignorés). Sans cible (menu principal), la caméra tourne lentement autour du village.
    [DefaultExecutionOrder(100)]
    public class CameraEpaule : MonoBehaviour
    {
        public Transform cible;
        public float lacet;
        public float tangage = 12f;
        [Tooltip("Vue du menu : orbite lente autour de ce point.")]
        public Vector3 centreMenu = new Vector3(0f, 3f, 0f);
        public float rayonMenu = 26f, hauteurMenu = 11f, vitesseMenu = 3f;

        float m_Distance;
        readonly RaycastHit[] m_Hits = new RaycastHit[16];

        public void Suivre(Transform t)
        {
            cible = t;
            if (t != null) lacet = t.eulerAngles.y;
        }

        /// Rotation demandée par les entrées (degrés).
        public void Tourner(float dLacet, float dTangage)
        {
            var b = GameBalance.Courant;
            lacet += dLacet;
            tangage = Mathf.Clamp(tangage - dTangage, b.cameraTangage.x, b.cameraTangage.y);
        }

        /// Direction « avant » à plat de la caméra (déplacement relatif).
        public Vector3 AvantPlat => Quaternion.Euler(0f, lacet, 0f) * Vector3.forward;

        void LateUpdate()
        {
            var b = GameBalance.Courant;
            if (cible == null)
            {
                float a = Time.time * vitesseMenu * Mathf.Deg2Rad;
                transform.position = centreMenu + new Vector3(Mathf.Sin(a) * rayonMenu, hauteurMenu, Mathf.Cos(a) * rayonMenu);
                transform.rotation = Quaternion.LookRotation(centreMenu + Vector3.up * 2f - transform.position);
                return;
            }
            Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);
            Vector3 pivot = cible.position + Vector3.up * b.cameraHauteur;
            Vector3 epaule = pivot + rot * Vector3.right * b.cameraEpaule;
            Vector3 dir = rot * Vector3.back;
            float voulu = b.cameraDistance;
            float d = voulu;
            int n = Physics.SphereCastNonAlloc(epaule, 0.25f, dir, m_Hits, voulu, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = m_Hits[i];
                if (h.collider.GetComponentInParent<Sante>() != null) continue;   // personnages ignorés (Nyxessa comprise)
                if (h.distance > 0f && h.distance < d) d = h.distance;
            }
            m_Distance = m_Distance <= 0f ? d : (d < m_Distance ? d : Mathf.Lerp(m_Distance, d, 1f - Mathf.Exp(-6f * Time.deltaTime)));
            transform.position = epaule + dir * Mathf.Max(0.6f, m_Distance);
            transform.rotation = rot;
        }
    }
}
