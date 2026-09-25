using UnityEngine;

namespace Deathless.Jeu
{
    /// Références partagées vers les effets et matériaux de Assets/VFX (un par scène, rempli par Deathless > Jeu).
    /// Les effets restent « Visual only » : c'est le code de jeu qui les déclenche au bon instant.
    public class EffetsJeu : MonoBehaviour
    {
        public static EffetsJeu Instance { get; private set; }

        [Tooltip("PortalVoxel.mat : gemmes à couleurs par sommet.")]
        public Material gemmes;
        [Tooltip("FireBurst.mat : mottes de terre de la sortie de terre (DirtBurst).")]
        public Material terre;
        [Tooltip("SkullGemShape.asset : forme du missile crâne.")]
        public GemShape formeCrane;
        public GameObject prefabChargeBelier;
        public GameObject prefabAuraSoin;
        public GameObject prefabOndeGolem;
        [Header("Classes")]
        [Tooltip("Modèle KayKit arrow_bow (flèches et carreaux, non magiques).")]
        public GameObject modeleFleche;
        [Tooltip("Pièce d'or KayKit (coin) : retour de l'or gagné sur un squelette tué.")]
        public GameObject modelePiece;
        public GameObject prefabArcBande;
        public GameObject prefabNuee;
        public GameObject prefabCone;
        public GameObject prefabBrulure;
        public GameObject prefabFumigene;
        public GameObject prefabTournante;
        public GameObject prefabRugissement;
        public GameObject prefabOndeSaut;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public static Material Gemmes => Instance != null ? Instance.gemmes : null;
        public static Material Terre => Instance != null ? Instance.terre : null;

        /// Volume visible d'un personnage (rendus actifs).
        public static Bounds Volume(GameObject go)
        {
            var b = new Bounds(go.transform.position + Vector3.up, new Vector3(0.8f, 1.9f, 0.8f));
            bool premier = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                if (premier) { b = r.bounds; premier = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
