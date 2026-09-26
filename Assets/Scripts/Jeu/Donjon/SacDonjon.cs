using UnityEngine;

namespace Deathless.Jeu
{
    /// Effet discret sur un sac de butin au sol (DonjonJeu.CreerVisuelSac) : quelques pièces qui scintillent au-dessus,
    /// avec la palette de l'or (PieceOr, jamais vert), pour qu'on le repère. Le ramassage se fait en marchant dessus
    /// (DonjonJeu.Update) ; ce composant n'est que visuel.
    public class SacDonjon : MonoBehaviour
    {
        public float intervalleMin = 1.0f, intervalleMax = 1.8f;
        float m_Prochain;

        void Update()
        {
            m_Prochain -= Time.deltaTime;
            if (m_Prochain > 0f) return;
            m_Prochain = Random.Range(intervalleMin, intervalleMax);
            PieceOr.Jouer(transform.position + Vector3.up * 0.55f + Random.insideUnitSphere * 0.15f);
        }
    }
}
