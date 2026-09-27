using UnityEngine;

namespace Deathless.Jeu
{
    /// Caméra orbitale derrière l'épaule droite du héros (troisième personne ; la vue FPS est proscrite). Lacet et
    /// tangage pilotés par la souris ou le stick droit (HerosEntrees.Regard), collision par SphereCast (les personnages
    /// sont ignorés). Sans cible (menu principal) : plan fixe du village de nuit, Nyxessa et le portail dans la moitié
    /// droite de l'image (le panneau du menu couvre la gauche), avec un très lent balancement latéral.
    ///
    /// En forêt (26/09/2026) : les troncs d'arbres/buissons (FeuillageMasquage.ColliderForet) sont ignorés par le
    /// recul ci-dessous, comme les personnages et les étages masqués du donjon -- le feuillage qui gêne s'estompe à
    /// la place (FeuillageMasquage, shader Deathless/ForetDither) ; la caméra elle-même ne bouge pas pour eux.
    [DefaultExecutionOrder(100)]
    public class CameraEpaule : MonoBehaviour
    {
        public Transform cible;
        public float lacet;
        [Tooltip("Valeur de repli avant que Suivre() applique GameBalance.cameraTangageDefaut (au démarrage de la partie).")]
        public float tangage = 22f;
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
            if (t != null)
            {
                lacet = t.eulerAngles.y;
                // Tangage par défaut au démarrage de la partie (GameBalance.cameraTangageDefaut), plus robuste que la
                // valeur de scène (décision de Quentin, 26/09/2026) : ne touche pas au tangage déjà choisi par le joueur
                // en cours de partie (Tourner), seulement au début, quand la caméra se met à suivre un héros.
                tangage = GameBalance.Courant.cameraTangageDefaut;
            }
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
        /// Rayon de la sphère qui vérifie, une fois la caméra placée, qu'elle n'est dans aucun collider (Degager).
        const float RayonLibre = 0.18f;

        /// Enceinte (monde) que la caméra ne doit pas quitter : au donjon, l'intérieur des murs, sous leur sommet (DonjonJeu
        /// la pose quand le héros local y est). Null : libre. Audit du 27/09/2026 (B1) : au bord de la plate-forme du 2e
        /// étage, la caméra passait par-dessus le mur d'enceinte.
        public static Bounds? Enceinte;

        /// Place l'épaule puis la caméra, sans traverser les murs (personnages ignorés). 1) L'épaule est recalée par un
        /// SphereCast du pivot (centre du héros, à hauteur des yeux) vers la droite : collée à un mur, elle revient vers le
        /// héros, et le second test ne part plus de l'intérieur d'un collider (PhysX l'ignorerait et la caméra passerait
        /// à travers le mur). 2) SphereCast de l'épaule vers l'arrière. 3) Tête -> caméra dégagée (petite sphère).
        /// 4) Deux lancers SANS rayon, pivot -> caméra et épaule -> caméra : un rayon qui part dans un collider l'ignore mais
        /// touche ceux qui suivent, alors qu'un balayage parti en chevauchement (capsule du héros, boîte d'un coffre à
        /// 1,5 m) pouvait ne pas remonter le mur derrière (B1). Rend le recul libre (au moins ReculMin).
        public static float Recul(Vector3 pivot, Quaternion rot, float epauleVoulue, float distance, RaycastHit[] hits, out Vector3 epaule)
        {
            Vector3 droite = rot * Vector3.right, arriere = rot * Vector3.back;
            float e = Premier(pivot, droite, epauleVoulue, hits);
            epaule = pivot + droite * (e < epauleVoulue ? Mathf.Max(0f, e - 0.02f) : epauleVoulue);
            float d = Premier(epaule, arriere, distance, hits);
            // 3) Le héros doit rester visible : un obstacle entre sa tête et la caméra (bord d'un battant de porte, d'un
            // montant, que le test depuis l'épaule longe sans le toucher) rapproche la caméra d'autant.
            Vector3 cam = epaule + arriere * d;
            Vector3 vers = cam - pivot; float l = vers.magnitude;
            if (l > 0.01f)
            {
                float t = Mathf.Min(Premier(pivot, vers / l, l, hits, 0.12f), Rayon0(pivot, vers / l, l, hits));
                if (t < l) d = d * (t / l) - 0.05f;
            }
            // 4) Épaule -> caméra sans rayon (le balayage du 2 peut être parti dans un collider).
            float t2 = Rayon0(epaule, arriere, d, hits);
            if (t2 < d) d = t2 - 0.1f;
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
                if (Ignore(h.collider)) continue;
                if (h.distance > 0f && h.distance < d) d = h.distance;
            }
            return d;
        }

        // Même chose par un rayon sans épaisseur (les colliders qui contiennent le départ sont ignorés par PhysX).
        static float Rayon0(Vector3 depart, Vector3 dir, float longueur, RaycastHit[] hits)
        {
            float d = longueur;
            int n = Physics.RaycastNonAlloc(depart, dir, hits, longueur, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (Ignore(h.collider)) continue;
                if (h.distance < d) d = h.distance;
            }
            return d;
        }

        /// Colliders que la caméra traverse : personnages (Nyxessa comprise), étages masqués du donjon, troncs de la forêt
        /// (le feuillage s'estompe à la place, FeuillageMasquage).
        public static bool Ignore(Collider c)
        {
            if (c.GetComponentInParent<Sante>() != null) return true;
            if (Deathless.Donjon.DonjonMasquage.ColliderMasque(c)) return true;
            return FeuillageMasquage.ColliderForet(c);
        }

        /// Distance, le long de `dir` depuis `depart`, à laquelle on sort de la boîte `b` (0 si le départ est dehors).
        static float Sortie(Vector3 depart, Vector3 dir, Bounds b)
        {
            if (!b.Contains(depart)) return 0f;
            float t = float.MaxValue;
            for (int a = 0; a < 3; a++)
            {
                float v = dir[a];
                if (Mathf.Abs(v) < 1e-5f) continue;
                float bord = v > 0f ? b.max[a] : b.min[a];
                t = Mathf.Min(t, (bord - depart[a]) / v);
            }
            return t;
        }

        readonly Collider[] m_Cols = new Collider[8];

        /// La caméra est-elle dans un collider (hors ceux qu'elle ignore) ?
        bool DansUnCollider(Vector3 p)
        {
            int n = Physics.OverlapSphereNonAlloc(p, RayonLibre, m_Cols, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!Ignore(m_Cols[i])) return true;
            return false;
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
            // Enceinte (donjon) : jamais au-delà des murs ni par-dessus.
            if (Enceinte.HasValue) d = Mathf.Max(ReculMin, Mathf.Min(d, Sortie(epaule, dir, Enceinte.Value) - 0.35f));
            m_Distance = m_Distance <= 0f ? d : (d < m_Distance ? d : Mathf.Lerp(m_Distance, d, 1f - Mathf.Exp(-6f * Time.deltaTime)));
            // Dernier filet : si la position retenue est dans un collider (pilier, bord d'un mur que les lancers ont longé),
            // la caméra se rapproche par pas de 0,2 m jusqu'à être libre.
            float recul = Mathf.Max(ReculMin, m_Distance);
            for (int i = 0; i < 24 && recul > ReculMin && DansUnCollider(epaule + dir * recul); i++) recul = Mathf.Max(ReculMin, recul - 0.2f);
            if (recul < m_Distance) m_Distance = recul;
            transform.position = epaule + dir * recul;
            transform.rotation = rot;
            // Ivresse (taverne) : la caméra tangue doucement, sans toucher à la visée (le centre de l'écran reste le même).
            if (Ivresse.Active)
            {
                transform.position += rot * Vector3.right * Ivresse.Balancement;
                transform.rotation = rot * Quaternion.Euler(0f, 0f, Ivresse.Roulis);
            }
        }
    }
}
