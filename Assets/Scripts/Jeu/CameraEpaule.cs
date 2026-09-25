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
        [Tooltip("Visée demandée par la classe (rôdeur : LT) : 0 normale, 1 serrée (épaule plus proche, champ réduit).")]
        public float viseeVoulue;
        float m_Visee;
        [Header("Plan du menu principal (sans cible)")]
        public Vector3 menuPosition = new Vector3(-8f, 13f, -22f);
        [Tooltip("Rotation de la caméra (angles d'Euler, degrés).")]
        public Vector3 menuRotation = new Vector3(11.8f, 13.4f, 0f);   // relevée de 12° (Quentin, 26/09/2026) : un bout de ciel et des nuages
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

        // Garde-fou (0.4.2) : une caméra restée dans la scène (outil de capture) et qui rend à l'écran passe par-dessus
        // celle-ci dans le build ; c'était le « plan du menu figé » de la 0.4.1. Seule la caméra de jeu rend à l'écran :
        // les autres caméras de la scène qui visent l'écran sont coupées au démarrage (les aperçus sur texture restent).
        void Start()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == m_Camera || !c.enabled || c.targetTexture != null || c.gameObject.scene != gameObject.scene) continue;
                Debug.LogWarning("[Caméra] caméra de trop dans la scène, coupée : " + c.name);
                c.enabled = false;
            }
        }

        /// État pour les journaux de test : cible, mode (menu ou jeu), position, caméras qui rendent à l'écran.
        public string Diagnostic()
        {
            string ecran = "";
            foreach (var c in Camera.allCameras) if (c.targetTexture == null) ecran += (ecran.Length > 0 ? "," : "") + c.name;
            return "caméra " + (cible == null ? "MENU (sans cible)" : "jeu, cible " + cible.name) + " à " + transform.position.ToString("F1")
                + ", à l'écran : " + ecran;
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

        /// Recul minimal derrière l'épaule (m) : petit, pour les pièces étroites (intérieurs des maisons).
        public const float ReculMin = 0.3f;
        const float Rayon = 0.25f;

        /// Place l'épaule puis la caméra, sans traverser les murs (personnages ignorés). 1) L'épaule est recalée par un
        /// SphereCast du pivot (centre du héros, à hauteur des yeux) vers la droite : collée à un mur, elle revient vers le
        /// héros, et le second test ne part plus de l'intérieur d'un collider (PhysX l'ignorerait et la caméra passerait
        /// à travers le mur). 2) SphereCast de l'épaule vers l'arrière. 3) Tête -> caméra dégagée. Rend le recul libre
        /// (au moins ReculMin).
        public static float Recul(Vector3 pivot, Quaternion rot, float epauleVoulue, float distance, RaycastHit[] hits, out Vector3 epaule)
        {
            Vector3 droite = rot * Vector3.right;
            float e = Premier(pivot, droite, epauleVoulue, hits);
            epaule = pivot + droite * (e < epauleVoulue ? Mathf.Max(0f, e - 0.02f) : epauleVoulue);
            float d = Premier(epaule, rot * Vector3.back, distance, hits);
            // 3) Le héros doit rester visible : un obstacle entre sa tête et la caméra (bord d'un battant de porte, d'un
            // montant, que le test depuis l'épaule longe sans le toucher) rapproche la caméra d'autant.
            Vector3 cam = epaule + rot * Vector3.back * d;
            Vector3 vers = cam - pivot; float l = vers.magnitude;
            if (l > 0.01f)
            {
                float t = Premier(pivot, vers / l, l, hits, 0.12f);
                if (t < l) d = d * (t / l) - 0.05f;
            }
            return Mathf.Max(ReculMin, d);
        }

        // Distance du premier obstacle (hors personnages) sur `longueur`, ou `longueur`.
        static float Premier(Vector3 depart, Vector3 dir, float longueur, RaycastHit[] hits, float rayon = Rayon)
        {
            float d = longueur;
            int n = Physics.SphereCastNonAlloc(depart, rayon, dir, hits, longueur, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider.GetComponentInParent<Sante>() != null) continue;   // personnages ignorés (Nyxessa comprise)
                if (h.distance > 0f && h.distance < d) d = h.distance;
            }
            return d;
        }

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
            m_Visee = Mathf.MoveTowards(m_Visee, viseeVoulue, Time.deltaTime * 5f);
            if (m_Camera != null) m_Camera.fieldOfView = m_ChampJeu * (1f - 0.25f * m_Visee);
            Quaternion rot = Quaternion.Euler(tangage, lacet, 0f);
            Vector3 pivot = cible.position + Vector3.up * b.cameraHauteur;
            float d = Recul(pivot, rot, b.cameraEpaule * (1f + 0.3f * m_Visee), b.cameraDistance * (1f - 0.3f * m_Visee), m_Hits, out Vector3 epaule);
            Vector3 dir = rot * Vector3.back;
            m_Distance = m_Distance <= 0f ? d : (d < m_Distance ? d : Mathf.Lerp(m_Distance, d, 1f - Mathf.Exp(-6f * Time.deltaTime)));
            transform.position = epaule + dir * Mathf.Max(ReculMin, m_Distance);
            transform.rotation = rot;
        }
    }
}
