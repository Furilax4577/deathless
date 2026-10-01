using UnityEngine;

namespace Deathless.Jeu
{
    /// Données des emotes qui viennent des assets : la chope de « Boire un coup » (modèles KayKit pleine et vide, pose dans
    /// la main, mousse, geste de boire) et les instants mesurés dans le clip Use_Item_Boire. Asset
    /// Assets/Jeu/Resources/Emotes.asset, écrit par le menu Deathless > Jeu > 11. Emotes (JeuBuilder.CatalogueEmotes).
    /// La liste des emotes elle-même est dans EmotesHeros.
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
        [Tooltip("Échelle de la chope sous le socket (calculée par le builder : fraction de la hauteur du personnage).")]
        public float echelle = 1f;
        [Tooltip("Instant (temps normalisé de Use_Item, 0 à 1) où la chope quitte la bouche : pleine avant, vide après.")]
        [Range(0f, 1f)] public float instantVide = 0.6f;

        [Header("Geste de boire (01/10/2026 : la chope doit se voir à 5,5 m et aller à la bouche)")]
        [Tooltip("Instant (0 à 1) où la main est au plus près de la bouche : la chope y est portée et basculée jusqu'à instantVide.")]
        [Range(0f, 1f)] public float instantBouche = 0.45f;
        [Tooltip("Durée (temps normalisé) de la montée vers la bouche avant instantBouche et de la descente après instantVide.")]
        [Range(0.01f, 0.5f)] public float fondu = 0.1f;
        [Tooltip("Inclinaison de la chope vers la tête pendant qu'on boit (degrés).")]
        public float bascule = 40f;
        [Tooltip("Part du chemin qui reste entre le bord de la chope et la bouche que la chope franchit d'elle-même (0 : elle reste dans la main ; 1 : son bord touche la bouche).")]
        [Range(0f, 1f)] public float rapprochement = 1f;
        [Tooltip("Bouche : devant l'os « head » (m, dans le sens du personnage) et au-dessus (négatif : en dessous). L'os « head » de KayKit est à la base du cou : la bouche est devant et un peu plus haut.")]
        public float boucheAvant = 0.34f;
        public float boucheHaut = 0.17f;
        [Tooltip("Hauteur du bord de la chope au-dessus de son pivot (unités du modèle, mesurée par le builder).")]
        public float hauteurBord = 0.3f;

        [Header("Mousse")]
        [Tooltip("Dôme de mousse blanche posé sur la chope pleine (EmotesHeros.PreparerChope).")]
        public bool mousse = true;
        [Tooltip("Hauteur du dôme en fraction de la hauteur de la chope.")]
        [Range(0f, 1f)] public float mousseHauteur = 0.3f;
        [Tooltip("Largeur du dôme en fraction de la largeur de la chope.")]
        [Range(0f, 1.5f)] public float mousseLargeur = 0.95f;
        public Color mousseCouleur = new Color(0.97f, 0.95f, 0.88f);

        static CatalogueEmotes s_Courant;
        public static CatalogueEmotes Courant => s_Courant != null ? s_Courant : (s_Courant = Resources.Load<CatalogueEmotes>("Emotes"));
    }
}
