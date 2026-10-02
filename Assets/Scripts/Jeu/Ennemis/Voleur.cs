using UnityEngine;

namespace Deathless.Jeu
{
    /// Voleur (wiki : ennemis.md ; 28/09/2026) : squelette rapide, moins solide qu'un guerrier, coup court à préparer.
    /// Il **chasse les joueurs isolés** : pendant sa marche, il préfère un joueur vu qu'aucun autre joueur vivant
    /// n'accompagne à moins de GameBalance.voleurDistanceIsolement m, repéré jusqu'à voleurDistanceChasse m (plus loin
    /// que la détection ordinaire). Sans joueur isolé, il fait comme les autres : Nyxessa au contact, sinon le joueur
    /// le plus proche à detectionJoueur m, sinon il marche. Le reste (poursuite, abandon, riposte, coup parable) est
    /// celui de Squelette ; stats dans GameBalance.voleur.
    public class Voleur : Squelette
    {
        /// Dernier joueur isolé que ce voleur a pris en chasse (succès « Le voleur volé »).
        public Heros Proie { get; private set; }

        protected override Heros ChoisirCible(float rayon)
        {
            // Voleur gardien du donjon (02/10/2026) : il repère comme les autres gardiens (gardienDetection, ligne de vue, laisse) ;
            // sa portée de chasse plus longue le faisait se ruer à travers les murs sur le héros arrivé à 14 m.
            var isole = JoueurIsole(Gardien ? Mathf.Min(B.voleurDistanceChasse, B.gardienDetection) : B.voleurDistanceChasse);
            if (isole != null)
            {
                Proie = isole;
                AudioBank.Jouer(SonsDuJeu.VoleurElan, transform.position + Vector3.up, 0.7f, 0.1f);
                if (P != null) P.Journal("Voleur " + Id + " se rue sur le joueur isolé " + isole.Id);
                return isole;
            }
            return base.ChoisirCible(rayon);
        }

        /// Joueur vivant, vu, à moins de `rayon` m, sans autre joueur vivant à moins de voleurDistanceIsolement m ; le plus
        /// proche s'il y en a plusieurs. En solo, le joueur est toujours isolé.
        Heros JoueurIsole(float rayon)
        {
            if (P == null) return null;
            var tous = P.TousLesHeros;
            float iso2 = B.voleurDistanceIsolement * B.voleurDistanceIsolement;
            Heros meilleur = null;
            float d = rayon * rayon;
            for (int i = 0; i < tous.Count; i++)
            {
                var h = tous[i];
                if (h == null || !h.Vivant) continue;
                float dd = (h.transform.position - transform.position).sqrMagnitude;
                if (dd >= d) continue;
                bool seul = true;
                for (int k = 0; k < tous.Count && seul; k++)
                {
                    var a = tous[k];
                    if (a == null || a == h || !a.Vivant) continue;
                    if ((a.transform.position - h.transform.position).sqrMagnitude < iso2) seul = false;
                }
                if (Gardien && (!DansLaLaisse(h) || !LigneDeVue(h))) continue;
                if (seul && Voit(h)) { d = dd; meilleur = h; }
            }
            return meilleur;
        }
    }
}
