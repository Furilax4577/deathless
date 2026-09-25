using UnityEngine;

namespace Deathless.Jeu
{
    /// Petite pièce d'or (modèle KayKit `coin`, EffetsJeu.modelePiece) qui monte en tournant au-dessus d'un squelette tué :
    /// retour discret de l'or gagné (règle provisoire des vagues). Apparition et disparition par la taille.
    public class PieceOr : MonoBehaviour
    {
        public static float Duree = 0.9f, Hauteur = 1.2f, Taille = 1.4f;

        float m_T;
        Vector3 m_Depart;

        public static void Jouer(Vector3 point)
        {
            var fx = EffetsJeu.Instance;
            if (fx == null || fx.modelePiece == null) return;
            var go = Instantiate(fx.modelePiece, point, Quaternion.Euler(90f, 0f, 0f));
            go.name = "PieceOr";
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.transform.localScale = Vector3.zero;
            go.AddComponent<PieceOr>().m_Depart = point;
        }

        void Update()
        {
            m_T += Time.deltaTime;
            float k = Mathf.Clamp01(m_T / Duree);
            transform.position = m_Depart + Vector3.up * Hauteur * (1f - (1f - k) * (1f - k));
            transform.rotation = Quaternion.Euler(0f, m_T * 540f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            float s = k < 0.15f ? k / 0.15f : k > 0.75f ? (1f - k) / 0.25f : 1f;
            transform.localScale = Vector3.one * Taille * Mathf.Clamp01(s);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
