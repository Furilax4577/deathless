using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Point d'interaction du village (achats à la relique, taverne…) : touche Interagir (E, X, Carré) du joueur local.
    /// Le plus proche des points disponibles donne l'invite du HUD (« Améliorer Nyxessa ») ; Interagir l'ouvre.
    public abstract class PointInteraction : MonoBehaviour
    {
        static readonly List<PointInteraction> s_Tous = new List<PointInteraction>();

        protected virtual void OnEnable() { s_Tous.Add(this); }
        protected virtual void OnDisable() { s_Tous.Remove(this); }

        /// Invite du HUD si ce point est disponible pour ce héros (null sinon) et sa distance (pour choisir le plus proche).
        public abstract string Invite(Heros h, out float distance);
        /// Le héros interagit (ouvre le menu).
        public abstract void Interagir(Heros h);

        /// Le point disponible le plus proche du héros, ou null.
        public static PointInteraction Courant(Heros h, out string invite)
        {
            invite = null;
            if (h == null || !h.Vivant) return null;
            PointInteraction meilleur = null; float dmin = float.MaxValue;
            foreach (var p in s_Tous)
            {
                string i = p.Invite(h, out float d);
                if (string.IsNullOrEmpty(i) || d >= dmin) continue;
                dmin = d; meilleur = p; invite = i;
            }
            return meilleur;
        }

        /// Touche Interagir du héros local.
        public static bool InteragirIci(Heros h)
        {
            var p = Courant(h, out _);
            if (p == null) return false;
            p.Interagir(h);
            return true;
        }
    }
}
