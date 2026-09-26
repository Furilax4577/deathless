using UnityEngine;

namespace Deathless.Jeu
{
    /// Brûlure du Mage, style feu (wiki : les ennemis touchés brûlent pendant un moment) : c'est un statut (Statuts,
    /// TypeStatut.Brulure) posé par la boule de feu et le cône ; dégâts par seconde pendant la durée, rafraîchie par chaque
    /// nouveau coup (pas de cumul), décidés par l'hôte (Statuts.Update). Ce composant n'est plus que le visuel, sur tous les
    /// postes tant que le statut est là : flammèches `BurnFlammeches` au centre du squelette et crépitement en boucle.
    public class Brulure : MonoBehaviour
    {
        GameObject m_Flammes;
        AudioSource m_Son;

        /// Mage : pose ou rafraîchit la brûlure d'un ennemi touché (demande à l'hôte chez un client).
        public static void Allumer(Sante cible, Heros source)
        {
            if (cible == null || cible.Mort) return;
            var b = GameBalance.Courant;
            Statuts.De(cible).Ajouter(TypeStatut.Brulure, b.brulureDuree, b.brulureDegats, OrigineStatut.Joueur, source != null ? source.Id : 0);
        }

        /// Flammèches et crépitement du personnage, allumés ou éteints (appelé par Statuts sur chaque poste).
        public static void Montrer(Component cible, bool actif)
        {
            if (cible == null) return;
            var b = cible.GetComponent<Brulure>();
            if (!actif)
            {
                if (b != null) b.Eteindre();
                return;
            }
            if (b == null) b = cible.gameObject.AddComponent<Brulure>();
            b.Allumer();
        }

        void Allumer()
        {
            if (m_Flammes == null && EffetsJeu.Instance != null && EffetsJeu.Instance.prefabBrulure != null)
            {
                var sq = GetComponent<Squelette>();
                float h = sq != null && sq.type == TypeEnnemi.Golem ? 2f : 1f;
                m_Flammes = Instantiate(EffetsJeu.Instance.prefabBrulure, transform);
                m_Flammes.transform.localPosition = Vector3.up * h;
                m_Flammes.transform.localScale = Vector3.one * h;
            }
            if (m_Flammes != null && !m_Flammes.activeSelf) m_Flammes.SetActive(true);
            if (m_Son == null) m_Son = AudioBank.Boucle(SonsDuJeu.Brulure, transform, 0.45f);
            else if (!m_Son.isPlaying) m_Son.Play();
        }

        void Eteindre()
        {
            if (m_Flammes != null) m_Flammes.SetActive(false);
            if (m_Son != null) m_Son.Stop();
        }
    }
}
