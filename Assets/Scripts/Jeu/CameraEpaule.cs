using UnityEngine;

namespace Deathless.Jeu
{
    /// Caméra orbitale derrière l'épaule droite du héros (troisième personne ; la vue FPS est proscrite). Lacet et
    /// tangage pilotés par la souris ou le stick droit (HerosEntrees.Regard), collision par SphereCast (les personnages
    /// sont ignorés). Sans cible (menu principal) : plan fixe du village de nuit, Nyxessa et le portail dans la moitié
    /// droite de l'image (le panneau du menu couvre la gauche), avec un très lent balancement latéral.
    [DefaultExecutionOrder(100)]
    public class CameraEpaule : MonoBehaviour
    {
        public Transform cible;
        public float lacet;
        public float tangage = 12f;
        [Header("Plan du menu principal (sans cible)")]
        public Vector3 menuPosition = new Vector3(-8f, 13f, -22f);
        [Tooltip("Rotation de la caméra (angles d'Euler, degrés).")]
        public Vector3 menuRotation = new Vector3(23.8f, 13.4f, 0f);
        [Tooltip("Champ de vision vertical du plan du menu (degrés). En jeu, celui de la caméra est rendu.")]
        public float menuChamp = 44f;
        [Tooltip("Balancement latéral lent (m, de part et d'autre) ; 0 : plan fixe.")]
        public float menuBalancement = 0.8f;
        [Tooltip("Période du balancement (s).")]
        public float menuPeriode = 50f;

        float m_Distance;
        readonly RaycastHit[] m_Hits = new RaycastHit[16];
        Camera m_Camera;
        float m_ChampJeu;

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            if (m_Camera != null) m_ChampJeu = m_Camera.fieldOfView;
        }

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
                Quaternion r = Quaternion.Euler(menuRotation);
                float s = menuPeriode > 0f ? Mathf.Sin(Time.time * 2f * Mathf.PI / menuPeriode) : 0f;
                transform.position = menuPosition + r * Vector3.right * (menuBalancement * s);
                transform.rotation = r;
                if (m_Camera != null) m_Camera.fieldOfView = menuChamp;
                return;
            }
            if (m_Camera != null) m_Camera.fieldOfView = m_ChampJeu;
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
