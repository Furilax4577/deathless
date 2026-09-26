using UnityEngine;

namespace Deathless.Jeu
{
    /// Ivresse du joueur local (taverne : une bière, ou une tournée payée par un joueur) : pendant quelques secondes, la
    /// caméra tangue doucement (roulis, léger balancement) et la démarche hésite (la direction de marche ondule un peu).
    /// Rien de handicapant pour le combat : la visée, les attaques et les compétences ne changent pas, et la démarche
    /// reste droite pendant une action de classe. Montée et descente en 1 s. Purement local (chaque poste son joueur).
    public static class Ivresse
    {
        static float s_Debut = -99f, s_Fin = -99f;

        /// Enivre le joueur local pendant `duree` secondes (prolonge une ivresse en cours).
        public static void Commencer(float duree)
        {
            float t = Time.time;
            if (t > s_Fin) s_Debut = t;
            s_Fin = Mathf.Max(s_Fin, t + duree);
            // Statut Ivresse du héros local (HUD, menu du personnage ; chez un client, demandé à l'hôte qui le diffuse).
            var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
            if (h != null && h.Statuts != null) h.Statuts.Ajouter(TypeStatut.Ivresse, duree, 1f, OrigineStatut.Taverne);
            Partie.Instance?.Journal("Ivresse : " + duree.ToString("0") + " s");
        }

        /// Arrêt immédiat (mort, fin de partie).
        public static void Arreter()
        {
            s_Fin = Mathf.Min(s_Fin, Time.time);
            var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
            if (h != null && h.Statuts != null) h.Statuts.Retirer(TypeStatut.Ivresse);
        }

        /// Nouvelle partie (Partie.Awake) : l'état est statique et Time.time continue d'une scène à l'autre, donc une
        /// tournée bue juste avant « Rejouer » se prolongeait dans la partie suivante.
        public static void Reinitialiser() { s_Debut = s_Fin = -99f; }

        /// 0 à 1 : force de l'ivresse maintenant.
        public static float Force
        {
            get
            {
                float t = Time.time;
                if (t >= s_Fin) return 0f;
                return Mathf.Clamp01(Mathf.Min(t - s_Debut, s_Fin - t));
            }
        }

        public static bool Active => Force > 0f;

        static GameBalance B => GameBalance.Courant;

        /// Roulis de la caméra (degrés).
        public static float Roulis => Mathf.Sin(Time.time * 1.3f) * B.ivresseRoulis * Force;
        /// Balancement latéral de la caméra (m).
        public static float Balancement => Mathf.Sin(Time.time * 0.9f + 1f) * 0.18f * Force;
        /// Déviation de la direction de marche (degrés).
        public static float Deviation => (Mathf.Sin(Time.time * 1.7f) * 0.7f + Mathf.Sin(Time.time * 0.63f + 2f) * 0.3f) * B.ivresseDeviation * Force;
    }
}
