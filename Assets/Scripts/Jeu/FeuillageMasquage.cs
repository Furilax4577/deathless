using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Feuillage qui gêne la caméra à l'épaule, en forêt (décision de Quentin, 26/09/2026) : quand un arbre ou un
    /// buisson du Forest Nature Pack coupe la ligne caméra -> tête du héros, son rendu s'estompe par un damier
    /// (shader Deathless/ForetDither, une valeur 0..1 par MaterialPropertyBlock : pas de nouveau matériau par arbre),
    /// au lieu de repousser la caméra comme un mur. La caméra elle-même ignore les troncs pour son propre recul
    /// (CameraEpaule.Premier via ColliderForet ci-dessous) : avant cet ajout, leur CapsuleCollider (posé par
    /// VillageBuilder.PlaceTree, pour bloquer la marche) la faisait déjà reculer comme un mur.
    ///
    /// Détection en deux temps, comme DonjonMasquage (Assets/Scripts/Donjon) : une collecte unique à l'activation
    /// (tous les rendus "Tree_*"/"Bush_*" de la scène, nom posé par VillageBuilder.Place et jamais changé ensuite ;
    /// ni les rochers/herbes du même pack -- qui partagent pourtant le même matériau KayKit_Forest -- ni aucun objet
    /// du village ou des maisons), puis, quelques fois par seconde, un test de segment (caméra -> tête, élargi d'un
    /// rayon de capsule) contre la boîte englobante de chaque rendu : aucun Physics, aucune allocation par image. La
    /// transition (vers ~30 % de couverture en ~0,2 s, puis retour) n'anime que les quelques rendus concernés, pas
    /// toute la forêt (des centaines d'arbres sur le terrain de VillageBuilder).
    [DefaultExecutionOrder(150)]
    [RequireComponent(typeof(CameraEpaule))]
    public class FeuillageMasquage : MonoBehaviour
    {
        [Tooltip("Rayon de la capsule caméra -> tête du héros (m).")]
        public float rayon = 0.5f;
        [Tooltip("Vérifications de détection par seconde (le fondu, lui, est mis à jour chaque image).")]
        public float frequence = 8f;
        [Tooltip("Couverture visible pendant qu'un arbre ou un buisson gêne (0 = invisible, 1 = plein).")]
        [Range(0f, 1f)] public float couvertureMin = 0.3f;
        [Tooltip("Durée de la transition pleine <-> couverture minimale (s).")]
        public float duree = 0.2f;
        [Tooltip("Décochez pour tout garder plein (comparaison).")]
        public bool actif = true;

        struct Item
        {
            public Renderer rendu;
            public MaterialPropertyBlock bloc;
            public float valeur;   // couverture actuellement appliquée (1 = plein)
            public bool gene;      // dernière détection : ce rendu coupe la ligne caméra -> tête
        }

        static readonly int CouvertureID = Shader.PropertyToID("_Couverture");

        CameraEpaule m_Camera;
        Item[] m_Items = System.Array.Empty<Item>();
        bool[] m_DansListe = System.Array.Empty<bool>();
        readonly List<int> m_EnCours = new List<int>(16);   // indices en cours de transition (peu nombreux à la fois)
        float m_Prochain;

        /// Nombre de rendus actuellement estompés (au moins en cours de transition) : pour les journaux de test.
        public int NbEnCours { get { return m_EnCours.Count; } }
        /// Nombre total de rendus de forêt suivis (arbres + buissons trouvés à la collecte).
        public int NbSuivis { get { return m_Items.Length; } }

        void Awake() { m_Camera = GetComponent<CameraEpaule>(); }

        void OnEnable() { Collecter(); }

        void OnDisable()
        {
            // Redonne tout de suite leur plein aspect (désactivation en jeu, changement de scène...).
            for (int i = 0; i < m_Items.Length; i++) Restaurer(i);
            m_EnCours.Clear();
        }

        /// Recherche unique des rendus de la forêt qui peuvent gêner la caméra : GameObjects "Tree_*" / "Bush_*"
        /// (nom posé par VillageBuilder.Place). Les rochers et l'herbe du même pack (KayKit_Forest.mat partagé) et
        /// tout le reste du village en sont exclus par construction (préfixe de nom).
        public void Collecter()
        {
            var trouves = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var liste = new List<Item>(trouves.Length);
            foreach (var r in trouves)
            {
                if (!NomForet(r.transform)) continue;
                liste.Add(new Item { rendu = r, bloc = new MaterialPropertyBlock(), valeur = 1f, gene = false });
            }
            m_Items = liste.ToArray();
            m_DansListe = new bool[m_Items.Length];
            m_EnCours.Clear();
            m_Prochain = 0f;
        }

        static bool NomForet(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n.StartsWith("Tree_") || n.StartsWith("Bush_")) return true;
                t = t.parent;
            }
            return false;
        }

        /// Un collider appartient-il à un arbre ou un buisson de la forêt ? Appelé par CameraEpaule.Premier pour que
        /// ses SphereCast de recul ignorent les troncs (le feuillage s'estompe à la place ; la caméra ne recule pas
        /// pour eux), comme elle ignore déjà les personnages (Sante) et les étages masqués du donjon (DonjonMasquage).
        public static bool ColliderForet(Collider c) { return NomForet(c.transform); }

        // En LateUpdate, après que CameraEpaule (ordre 100) a placé la caméra pour cette image : transform.position
        // est déjà la position finale (recul contre les murs compris), pas celle de l'image précédente.
        void LateUpdate()
        {
            if (m_Items.Length == 0) return;
            Transform cible = actif ? m_Camera.cible : null;
            if (cible != null)
            {
                if (Time.unscaledTime >= m_Prochain)
                {
                    m_Prochain = Time.unscaledTime + (frequence > 0f ? 1f / frequence : 0f);
                    Detecter(cible);
                }
            }
            else
            {
                // Désactivé, ou pas de héros (menu principal) : on ne détecte plus rien, et ce qui gênait revient.
                for (int k = 0; k < m_EnCours.Count; k++) m_Items[m_EnCours[k]].gene = false;
            }
            Animer();
        }

        void Detecter(Transform cible)
        {
            var b = GameBalance.Courant;
            Vector3 cam = transform.position;
            Vector3 tete = cible.position + Vector3.up * b.cameraHauteur;
            Vector3 seg = tete - cam;
            float longueur = seg.magnitude;
            if (longueur < 0.001f) return;
            Vector3 dir = seg / longueur;
            for (int i = 0; i < m_Items.Length; i++)
            {
                Renderer r = m_Items[i].rendu;
                bool gene = r != null && SegmentCoupeBoite(cam, dir, longueur, Elargir(r.bounds, rayon));
                if (gene == m_Items[i].gene) continue;
                m_Items[i].gene = gene;
                if (!m_DansListe[i]) { m_DansListe[i] = true; m_EnCours.Add(i); }
            }
        }

        // Boîte englobante agrandie de `rayon` de chaque côté (Bounds.Expand ajoute `amount` à la taille totale,
        // donc amount/2 par face : on double le rayon voulu).
        static Bounds Elargir(Bounds b, float rayon)
        {
            b.Expand(rayon * 2f);
            return b;
        }

        // Segment [a, a + dir*longueur] (dir normalisé) contre une boîte : test de pente (slab test), sans
        // allocation. Sert d'approximation à « capsule de rayon `rayon` contre le maillage » (bounds déjà agrandie).
        static bool SegmentCoupeBoite(Vector3 a, Vector3 dir, float longueur, Bounds boite)
        {
            float tMin = 0f, tMax = longueur;
            Vector3 min = boite.min, max = boite.max;
            for (int axe = 0; axe < 3; axe++)
            {
                float o = a[axe], d = dir[axe];
                if (Mathf.Abs(d) < 1e-6f)
                {
                    if (o < min[axe] || o > max[axe]) return false;
                }
                else
                {
                    float inv = 1f / d;
                    float t1 = (min[axe] - o) * inv;
                    float t2 = (max[axe] - o) * inv;
                    if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                    if (t1 > tMin) tMin = t1;
                    if (t2 < tMax) tMax = t2;
                    if (tMin > tMax) return false;
                }
            }
            return true;
        }

        void Animer()
        {
            if (m_EnCours.Count == 0) return;
            float vitesse = duree > 0f ? (1f - couvertureMin) / duree : 1000f;
            float dt = Time.unscaledDeltaTime;
            for (int k = m_EnCours.Count - 1; k >= 0; k--)
            {
                int i = m_EnCours[k];
                float voulu = m_Items[i].gene ? couvertureMin : 1f;
                float v = Mathf.MoveTowards(m_Items[i].valeur, voulu, vitesse * dt);
                m_Items[i].valeur = v;
                Appliquer(i, v);
                if (v == voulu)
                {
                    m_DansListe[i] = false;
                    m_EnCours.RemoveAt(k);
                    if (voulu >= 1f) Restaurer(i);
                }
            }
        }

        void Appliquer(int i, float valeur)
        {
            Renderer r = m_Items[i].rendu;
            if (r == null) return;
            MaterialPropertyBlock bloc = m_Items[i].bloc;
            r.GetPropertyBlock(bloc);
            bloc.SetFloat(CouvertureID, valeur);
            r.SetPropertyBlock(bloc);
        }

        // Retour au repos : couverture 1 sans bloc de propriétés (le rendu redevient éligible au SRP Batcher avec
        // les autres arbres au repos, comme avant cet ajout).
        void Restaurer(int i)
        {
            m_Items[i].valeur = 1f;
            Renderer r = m_Items[i].rendu;
            if (r != null) r.SetPropertyBlock(null);
        }
    }
}
