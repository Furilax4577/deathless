using UnityEngine;

namespace Deathless.Jeu
{
    /// Brûlure du Mage, style feu : c'est un statut (Statuts, TypeStatut.Brulure) posé par la boule de feu et le cône,
    /// dont les dégâts sont décidés par l'hôte (Statuts.Update).
    ///
    /// Paliers (Quentin, 30/09/2026 ; chiffres du 01/10/2026 à équilibrer, GameBalance « brulure… ») : chaque coup de feu
    /// remplit la jauge de brûlure de l'ennemi ; au-delà de 100 %, le palier monte de 1 (jusqu'au plafond) et la jauge
    /// repart du dépassement. Sans feu reçu pendant `brulureDelaiDescente`, la jauge baisse ; arrivée à 0, le palier
    /// descend de 1 et la jauge repart pleine, ainsi de suite jusqu'au palier 1 vide. Chaque palier brûle plus fort.
    /// L'état est un instantané pris au dernier coup de feu (palier, jauge, instant de début de la redescente, champs du
    /// Statut) : la redescente se calcule (Etat) sur chaque poste, sans message réseau tant que personne ne brûle l'ennemi.
    ///
    /// Ce composant est aussi le visuel, sur tous les postes tant que le statut est là : flammèches `BurnFlammeches` au
    /// centre du squelette (plus fournies et plus grosses à chaque palier) et crépitement en boucle.
    public class Brulure : MonoBehaviour
    {
        /// Flammèches par palier : débit des particules (× palier 1) et taille (+ par palier au-dessus du 1).
        static readonly float[] DebitParPalier = { 1f, 1.7f, 2.5f, 3.4f };
        const float TailleParPalier = 0.14f;

        GameObject m_Flammes;
        ParticleSystem[] m_Systemes;
        float[] m_DebitBase, m_TailleBase;
        AudioSource m_Son;
        int m_Palier;

        // ----------------------------------------------------------------- Règles (tous postes)

        /// Plafond des paliers (nombre d'entrées de GameBalance.brulureDegatsPaliers, 1 si la liste est vide).
        public static int PalierMax
        {
            get
            {
                var l = GameBalance.Courant.brulureDegatsPaliers;
                return l != null && l.Length > 0 ? l.Length : 1;
            }
        }

        /// Dégâts par seconde d'un palier (1 au plafond).
        public static float Degats(int palier)
        {
            var b = GameBalance.Courant;
            var l = b.brulureDegatsPaliers;
            if (l == null || l.Length == 0) return b.brulureDegats;
            return l[Mathf.Clamp(palier, 1, l.Length) - 1];
        }

        static float Vitesse => Mathf.Max(0.01f, GameBalance.Courant.brulureVitesseDescente);

        /// Palier (1 au plafond) et jauge (0 à 1) d'une brûlure à l'instant `t` (Time.time de ce poste) : l'instantané du
        /// dernier coup de feu, moins la redescente écoulée depuis `descente`.
        public static void Etat(Statut s, float t, out int palier, out float jauge)
        {
            float total = Mathf.Max(1, (int)s.palier) - 1 + s.jauge - Mathf.Max(0f, t - s.descente) * Vitesse;
            if (total <= 0f) { palier = 1; jauge = 0f; return; }
            palier = Mathf.Clamp(Mathf.CeilToInt(total - 0.0001f), 1, PalierMax);
            jauge = Mathf.Clamp01(total - (palier - 1));
        }

        /// Autorité : un coup de feu de `remplissage` (part de jauge) sur la brûlure `e` (existe : déjà sur l'ennemi).
        /// Renvoie le nouvel instantané (palier, jauge, début de la redescente, durée, dégâts du palier).
        public static Statut Attiser(Statut e, bool existe, float remplissage, float t)
        {
            var b = GameBalance.Courant;
            int p = 1;
            float j = 0f;
            if (existe) Etat(e, t, out p, out j);
            j += Mathf.Max(0f, remplissage);
            int max = PalierMax;
            while (j > 1f && p < max) { j -= 1f; p++; }
            j = Mathf.Min(j, 1f);
            e.type = TypeStatut.Brulure;
            e.palier = (byte)p;
            e.jauge = j;
            e.descente = t + b.brulureDelaiDescente;
            // Fin : au moins brulureDuree après le dernier coup de feu, sinon le temps de redescendre jusqu'au palier 1 vide.
            e.duree = Mathf.Max(b.brulureDuree, b.brulureDelaiDescente + (p - 1 + j) / Vitesse);
            e.fin = t + e.duree;
            e.intensite = Degats(p);
            return e;
        }

        /// Mage : un coup de feu (boule : brulureRemplissageBoule, tic du cône : brulureRemplissageCone) pose la brûlure
        /// ou remplit sa jauge (demande à l'hôte chez un client).
        public static void Allumer(Sante cible, Heros source, float remplissage)
        {
            if (cible == null || cible.Mort) return;
            Statuts.De(cible).AttiserBrulure(remplissage, OrigineStatut.Joueur, source != null ? source.Id : 0);
        }

        // ----------------------------------------------------------------- Visuel

        /// Flammèches et crépitement du personnage, allumés (au palier donné) ou éteints (appelé par Statuts sur chaque poste).
        public static void Montrer(Component cible, bool actif, int palier = 1)
        {
            if (cible == null) return;
            var b = cible.GetComponent<Brulure>();
            if (!actif)
            {
                if (b != null) b.Eteindre();
                return;
            }
            if (b == null) b = cible.gameObject.AddComponent<Brulure>();
            b.Allumer(palier);
        }

        void Allumer(int palier)
        {
            if (m_Flammes == null && EffetsJeu.Instance != null && EffetsJeu.Instance.prefabBrulure != null)
            {
                var sq = GetComponent<Squelette>();
                float h = sq != null && sq.type == TypeEnnemi.Golem ? 2f : 1f;
                m_Flammes = Instantiate(EffetsJeu.Instance.prefabBrulure, transform);
                m_Flammes.transform.localPosition = Vector3.up * h;
                m_Flammes.transform.localScale = Vector3.one * h;
                m_Systemes = m_Flammes.GetComponentsInChildren<ParticleSystem>(true);
                m_DebitBase = new float[m_Systemes.Length];
                m_TailleBase = new float[m_Systemes.Length];
                for (int i = 0; i < m_Systemes.Length; i++)
                {
                    m_DebitBase[i] = m_Systemes[i].emission.rateOverTimeMultiplier;
                    m_TailleBase[i] = m_Systemes[i].main.startSizeMultiplier;
                }
                m_Palier = 0;
            }
            if (m_Flammes != null)
            {
                if (!m_Flammes.activeSelf) m_Flammes.SetActive(true);
                if (palier != m_Palier) Intensifier(palier);
            }
            if (m_Son == null) m_Son = AudioBank.Boucle(SonsDuJeu.Brulure, transform, 0.45f);
            else if (!m_Son.isPlaying) m_Son.Play();
            if (m_Son != null) m_Son.volume = Mathf.Min(1f, 0.45f + 0.1f * (Mathf.Max(1, palier) - 1));
        }

        /// Flammèches du palier : plus de particules et un peu plus grosses (palette Feu inchangée).
        void Intensifier(int palier)
        {
            m_Palier = palier;
            if (m_Systemes == null) return;
            int k = Mathf.Clamp(palier, 1, DebitParPalier.Length) - 1;
            float debit = DebitParPalier[k] * (palier > DebitParPalier.Length ? 1f + 0.5f * (palier - DebitParPalier.Length) : 1f);
            float taille = 1f + TailleParPalier * (Mathf.Max(1, palier) - 1);
            for (int i = 0; i < m_Systemes.Length; i++)
            {
                var ps = m_Systemes[i];
                if (ps == null) continue;
                var em = ps.emission;
                em.rateOverTimeMultiplier = m_DebitBase[i] * debit;
                var main = ps.main;
                main.startSizeMultiplier = m_TailleBase[i] * taille;
            }
        }

        void Eteindre()
        {
            if (m_Flammes != null) m_Flammes.SetActive(false);
            if (m_Son != null) m_Son.Stop();
        }
    }
}
