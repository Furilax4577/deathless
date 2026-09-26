using UnityEngine;

namespace Deathless.Jeu
{
    /// Données des emotes qui viennent des assets : la chope de « Boire un coup » (modèles KayKit pleine et vide, pose dans
    /// la main) et les instants mesurés dans le clip Use_Item. Asset Assets/Jeu/Resources/Emotes.asset, écrit par le menu
    /// Deathless > Jeu > 11. Emotes (JeuBuilder.CatalogueEmotes). La liste des emotes elle-même est dans EmotesHeros.
    public class CatalogueEmotes : ScriptableObject
    {
        [Header("Chope (Boire un coup)")]
        [Tooltip("Chope pleine, tenue du début du geste jusqu'à ce qu'elle quitte la bouche (KayKit mug_full).")]
        public GameObject chopePleine;
        [Tooltip("Chope vide, tenue ensuite jusqu'à la fin du geste (KayKit mug_empty).")]
        public GameObject chopeVide;
        [Tooltip("Main qui tient la chope dans Use_Item (mesurée par le builder) : la chope y remplace l'arme le temps du geste.")]
        public bool mainGauche;
        [Tooltip("Pose de la chope dans le socket de la main (handslot.r ou handslot.l).")]
        public Vector3 position;
        public Vector3 rotation;
        public float echelle = 1f;
        [Tooltip("Instant (temps normalisé de Use_Item, 0 à 1) où la chope quitte la bouche : pleine avant, vide après.")]
        [Range(0f, 1f)] public float instantVide = 0.6f;

        static CatalogueEmotes s_Courant;
        public static CatalogueEmotes Courant => s_Courant != null ? s_Courant : (s_Courant = Resources.Load<CatalogueEmotes>("Emotes"));
    }
}
