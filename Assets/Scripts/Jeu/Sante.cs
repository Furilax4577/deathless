using System;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Un coup porté. `sourceId` : id du joueur qui frappe (≥ 1) ou 0 pour un ennemi / Nyxessa.
    public struct InfoDegats
    {
        public float montant;
        public int sourceId;
        public Equipe equipeSource;
        public GameObject source;
        public Vector3 point;
        public Vector3 direction;
        public bool parable;
        public bool critique;
    }

    /// Réponse d'un intercepteur (garde du héros) : le coup passe, est bloqué ou paré.
    public enum Interception { Passe, Bloque, Pare }

    /// Points de vie communs (héros, squelettes, Nyxessa). Le composant ne décide rien : il applique les dégâts, prévient
    /// ses abonnés (Partie, IA, effets) et laisse un intercepteur (la garde) annuler un coup.
    public class Sante : MonoBehaviour
    {
        public Equipe equipe;
        public float pvMax = 100f;
        [SerializeField] float pv = 100f;
        [Tooltip("Invulnérable (esquive, réapparition, mode test).")]
        public bool invulnerable;
        public float Pv => pv;
        public bool Mort => pv <= 0f;
        public float Ratio => pvMax > 0f ? Mathf.Clamp01(pv / pvMax) : 0f;

        /// Garde du héros : appelé avant d'appliquer un coup parable.
        public Func<InfoDegats, Interception> intercepteur;

        public event Action<InfoDegats, float> Touche;           // coup appliqué, dégâts réels
        public event Action<InfoDegats, Interception> Intercepte; // coup bloqué ou paré
        public event Action<InfoDegats> Tue;
        public event Action<float> Soigne;                        // PV réellement rendus

        /// Dernier coup reçu (pour l'attribution et la priorité des missiles).
        public float DernierCoup { get; private set; } = -99f;

        public void Initialiser(float max)
        {
            pvMax = max;
            pv = max;
        }

        public void Remplir() { pv = pvMax; }

        /// Applique un coup ; renvoie les dégâts réellement appliqués.
        public float Encaisser(InfoDegats info)
        {
            if (Mort || !isActiveAndEnabled) return 0f;
            if (info.parable && intercepteur != null)
            {
                var r = intercepteur(info);
                if (r != Interception.Passe)
                {
                    Intercepte?.Invoke(info, r);
                    return 0f;
                }
            }
            if (invulnerable) return 0f;
            float avant = pv;
            pv = Mathf.Max(0f, pv - Mathf.Max(0f, info.montant));
            float reel = avant - pv;
            DernierCoup = Time.time;
            Touche?.Invoke(info, reel);
            if (pv <= 0f) Tue?.Invoke(info);
            return reel;
        }

        public float Soigner(float montant)
        {
            if (Mort) return 0f;
            float avant = pv;
            pv = Mathf.Min(pvMax, pv + Mathf.Max(0f, montant));
            float reel = pv - avant;
            if (reel > 0f) Soigne?.Invoke(reel);
            return reel;
        }

        /// Ajoute des PV (plafond des squelettes : PV d'un squelette non posé répartis sur les vivants).
        public void Renforcer(float bonus)
        {
            if (Mort || bonus <= 0f) return;
            pvMax += bonus;
            pv += bonus;
        }

        /// Remet en vie avec tous ses PV (réapparition).
        public void Ranimer() { pv = pvMax; }
    }
}
