using UnityEngine;

namespace Deathless.Jeu
{
    /// Bref tremblement de la caméra de jeu (parade parfaite) : décalage de position qui s'amortit, ajouté après
    /// CameraEpaule (ordre 101), qui repose la caméra à chaque image. Local au poste : rien n'est envoyé. La visée n'est
    /// pas touchée (seule la position bouge, de quelques centimètres).
    [DefaultExecutionOrder(101)]
    public class SecousseCamera : MonoBehaviour
    {
        float m_Amplitude, m_Duree, m_Fin = -1f;

        /// Tremble la caméra `camera` pendant `duree` s, d'au plus `amplitude` m.
        public static void Jouer(Component camera, float amplitude, float duree)
        {
            if (camera == null || amplitude <= 0f || duree <= 0f) return;
            var s = camera.GetComponent<SecousseCamera>();
            if (s == null) s = camera.gameObject.AddComponent<SecousseCamera>();
            s.m_Amplitude = Mathf.Max(amplitude, s.Actif ? s.m_Amplitude : 0f);
            s.m_Duree = duree;
            s.m_Fin = Time.unscaledTime + duree;
        }

        bool Actif => Time.unscaledTime < m_Fin;

        void LateUpdate()
        {
            if (!Actif) return;
            float k = (m_Fin - Time.unscaledTime) / Mathf.Max(0.01f, m_Duree);   // 1 → 0
            float t = Time.unscaledTime * 55f;
            var d = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 2f;
            transform.position += transform.rotation * d * (m_Amplitude * k * k);
        }
    }
}
