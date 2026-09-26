using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Coups parables annoncés aux héros de ce poste (jauge de parade du paladin, parade parfaite ; Docs/reseau.md,
    /// « Parade parfaite »). Un ennemi qui commence à préparer un coup contre un héros l'annonce ici : chez l'hôte (ou en
    /// solo) directement (Squelette.CommencerAttaque) ; chez un client, par le message HerosReseau.CoupAnnonceRpc que
    /// l'hôte envoie au propriétaire du héros visé. L'instant d'impact est noté dans l'horloge locale : réception + durée
    /// de la préparation. Chez un client, l'annonce et le coup partent de l'hôte par le même chemin, avec le même retard :
    /// l'impact prévu tombe donc là où le coup arrive vraiment (et où le client voit le geste, répliqué avec ce retard).
    /// Une annonce s'efface quand l'ennemi meurt ou est étourdi pendant sa préparation.
    public static class TelegraphieCoups
    {
        public struct Coup
        {
            public Squelette source;
            public Heros cible;
            /// Début de la préparation et impact prévu (Time.time, horloge de ce poste).
            public float debut, impact;
            public float Duree => impact - debut;
        }

        static readonly List<Coup> s_Coups = new List<Coup>();

        /// Une annonce reste utilisable jusqu'à cette durée après son impact prévu (gigue, coup reçu en retard).
        public const float Retenue = 0.5f;

        /// Annonce d'un coup de `source` contre `cible`, qui porte dans `duree` secondes (remplace l'annonce précédente
        /// de la même source).
        public static void Annoncer(Squelette source, Heros cible, float duree)
        {
            if (source == null || cible == null) return;
            float t = Time.time;
            for (int i = s_Coups.Count - 1; i >= 0; i--) if (s_Coups[i].source == source) s_Coups.RemoveAt(i);
            s_Coups.Add(new Coup { source = source, cible = cible, debut = t, impact = t + Mathf.Max(0f, duree) });
            AnnoncesRecues++;
        }

        /// Tests : annonces reçues depuis le lancement.
        public static int AnnoncesRecues { get; private set; }

        /// Le coup est-il toujours d'actualité (ennemi vivant, pas étourdi depuis le début de sa préparation) ?
        public static bool Actif(Coup c)
        {
            if (c.source == null || !c.source.Vivant || c.cible == null) return false;
            float t = Time.time;
            if (c.source.enabled)
            {
                // L'IA tourne ici (hôte, solo) : son état fait foi.
                if (t < c.impact - 0.02f && c.source.EtatCourant != Squelette.Etat.Preparation) return false;
                return true;
            }
            // Marionnette (client) : l'IA est chez l'hôte ; un étourdissement posé depuis le début de la préparation
            // (liste des statuts, répliquée) annule le coup.
            var st = c.source.Statuts;
            if (st == null) return true;
            var liste = st.Liste;
            for (int i = 0; i < liste.Count; i++)
            {
                var s = liste[i];
                if (s.type == TypeStatut.Etourdi && s.fin > t && s.fin - s.duree >= c.debut - 0.05f && t < c.impact) return false;
            }
            return true;
        }

        /// Coup à venir le plus proche de son impact contre `cible` (impact prévu au plus `apres` s dans le passé).
        public static bool Prochain(Heros cible, out Coup coup, float apres = 0.1f)
        {
            coup = default;
            Nettoyer();
            float t = Time.time, meilleur = float.MaxValue;
            bool trouve = false;
            for (int i = 0; i < s_Coups.Count; i++)
            {
                var c = s_Coups[i];
                if (c.cible != cible || c.impact < t - apres || !Actif(c)) continue;
                if (c.impact < meilleur) { meilleur = c.impact; coup = c; trouve = true; }
            }
            return trouve;
        }

        /// Impact prévu du dernier coup annoncé de `source` contre `cible`, s'il est proche (±Retenue s).
        public static bool ImpactPrevu(GameObject source, Heros cible, out float impact)
        {
            impact = 0f;
            if (source == null) return false;
            float t = Time.time;
            for (int i = 0; i < s_Coups.Count; i++)
            {
                var c = s_Coups[i];
                if (c.source == null || c.source.gameObject != source || c.cible != cible) continue;
                if (Mathf.Abs(c.impact - t) > Retenue) continue;
                impact = c.impact;
                return true;
            }
            return false;
        }

        static void Nettoyer()
        {
            float t = Time.time;
            for (int i = s_Coups.Count - 1; i >= 0; i--)
            {
                var c = s_Coups[i];
                if (c.source == null || c.cible == null || c.impact < t - 2f) s_Coups.RemoveAt(i);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reinitialiser() { s_Coups.Clear(); AnnoncesRecues = 0; }
    }
}
