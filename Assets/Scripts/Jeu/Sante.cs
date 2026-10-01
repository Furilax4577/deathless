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
        /// Attaque à distance (projectile) : la garde peut la bloquer, jamais la parer (ni étourdir le tireur).
        public bool aDistance;
        public bool critique;
        /// Dégâts continus (brûlure, tournante) : pas de réaction « touché » (son, animation) à chaque tic.
        public bool continu;
        /// Exécution de l'assassin (27/09/2026) : ennemi commun sous le seuil achevé net (l'hôte le vérifie), élite ou boss ×3 ;
        /// mot « Exécuté » dans les chiffres de dégâts.
        public bool execution;
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

        /// Bouclier du sorcier (Nyxessa, sorcier) : reçoit chaque coup et renvoie la part qui le traverse (0 : tout absorbé).
        public Func<InfoDegats, float> absorbeur;

        /// Multijoueur : ce corps est la copie d'un objet tenu par un autre poste (squelette chez un client, héros d'un autre
        /// joueur chez l'hôte). Le coup n'est pas appliqué ici : il part vers le poste qui fait foi, et la fonction renvoie
        /// les dégâts estimés (pour les jauges de classe du tireur). Null : coup appliqué ici (solo, objet local).
        public Func<InfoDegats, float> relais;

        /// Point faible (éclat de Nyx de Nyxar, 01/10/2026) : le coup est remis à cette fonction avant tout traitement ici
        /// (relais, garde, événements) ; elle renvoie les dégâts réels. Elle peut rappeler Encaisser sur ce même Sante :
        /// le coup suit alors le chemin ordinaire (pas de second renvoi).
        public Func<InfoDegats, float> renvoi;
        bool m_EnRenvoi;

        public event Action<InfoDegats, float> Touche;           // coup appliqué, dégâts réels
        public event Action<InfoDegats, Interception> Intercepte; // coup bloqué ou paré
        public event Action<InfoDegats> Tue;
        public event Action<float> Soigne;                        // PV réellement rendus

        /// Chiffres de dégâts flottants (interface.md) : ces événements globaux couvrent tous les Sante du jeu (héros,
        /// squelettes, Nyxessa, sorcier) pour que Deathless.Jeu.DegatsUI s'y abonne une seule fois plutôt que par
        /// personnage. AnyTouche est aussi levé sur un coup relayé (marionnette réseau, ci-dessous) avec les dégâts
        /// estimés localement, pour un affichage immédiat côté client sans attendre la confirmation de l'hôte
        /// (Docs/reseau.md, « Relais des coups »).
        public static event Action<Sante, InfoDegats, float> AnyTouche;
        public static event Action<Sante, InfoDegats, Interception> AnyIntercepte;
        /// Coup entièrement annulé par l'invulnérabilité (réapparition, fin d'esquive, mode test) : aucun autre
        /// événement n'est levé pour ce coup.
        public static event Action<Sante, InfoDegats> AnyImmunise;
        public static event Action<Sante, float> AnySoigne;

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
            if (renvoi != null && !m_EnRenvoi)
            {
                m_EnRenvoi = true;
                try { return renvoi(info); }
                finally { m_EnRenvoi = false; }
            }
            // Squelette galvanisé par le cri de Morgrim (statut Galvanisé, 30/09/2026) : ses coups portent plus fort. Avant
            // le relais : un coup ennemi est calculé chez l'hôte, même s'il vise le héros d'un autre poste.
            if (info.equipeSource == Equipe.Ennemis && info.source != null && info.montant > 0f && info.source.TryGetComponent<Squelette>(out var sq)
                && sq.Statuts != null && sq.Statuts.A(TypeStatut.Galvanise))
                info.montant *= 1f + sq.Statuts.Intensite(TypeStatut.Galvanise);
            if (relais != null)
            {
                if (info.montant <= 0f) return 0f;
                float estime = relais(info);
                // Estimation locale (client, marionnette) : chiffre de dégâts affiché tout de suite, sans attendre le
                // PV répliqué par l'hôte (Docs/reseau.md).
                if (estime > 0f) AnyTouche?.Invoke(this, info, estime);
                return estime;
            }
            if (info.parable && intercepteur != null)
            {
                var r = intercepteur(info);
                if (r != Interception.Passe)
                {
                    Intercepte?.Invoke(info, r);
                    AnyIntercepte?.Invoke(this, info, r);
                    return 0f;
                }
            }
            if (invulnerable)
            {
                AnyImmunise?.Invoke(this, info);
                return 0f;
            }
            if (absorbeur != null && info.equipeSource == Equipe.Ennemis)
            {
                info.montant = absorbeur(info);
                if (info.montant <= 0f) return 0f;
            }
            float avant = pv;
            pv = Mathf.Max(0f, pv - Mathf.Max(0f, info.montant));
            float reel = avant - pv;
            DernierCoup = Time.time;
            Touche?.Invoke(info, reel);
            AnyTouche?.Invoke(this, info, reel);
            if (pv <= 0f) Tue?.Invoke(info);
            return reel;
        }

        /// Joueur à l'origine du dernier soin (0 : soi-même, potion, taverne) : le soin d'aura du paladin (27/09/2026) crédite
        /// le soigneur, pas le soigné (Heros.Initialiser → Partie.CompterSoins).
        public int DernierSoigneur { get; private set; }

        public float Soigner(float montant, int soigneurId = 0)
        {
            if (Mort) return 0f;
            DernierSoigneur = soigneurId;
            float avant = pv;
            pv = Mathf.Min(pvMax, pv + Mathf.Max(0f, montant));
            float reel = pv - avant;
            if (reel > 0f) { Soigne?.Invoke(reel); AnySoigne?.Invoke(this, reel); }
            return reel;
        }

        /// Ajoute des PV (plafond des squelettes : PV d'un squelette non posé répartis sur les vivants).
        public void Renforcer(float bonus)
        {
            if (Mort || bonus <= 0f) return;
            pvMax += bonus;
            pv += bonus;
        }

        /// Multijoueur : PV recopiés du poste qui fait foi (sans événement).
        public void Fixer(float valeur, float max)
        {
            pvMax = max;
            pv = Mathf.Clamp(valeur, 0f, max);
        }

        /// Remet en vie avec tous ses PV (réapparition).
        public void Ranimer() { pv = pvMax; }
    }
}
