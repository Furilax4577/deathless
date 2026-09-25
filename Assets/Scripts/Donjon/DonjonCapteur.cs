using UnityEngine;

namespace Deathless.Donjon
{
    /// Capteur de zone à poser sur le héros LOCAL et sur SA caméra (jamais sur les autres joueurs : le masquage est
    /// propre à chaque joueur). Petite sphère déclencheuse + Rigidbody cinématique, couche « Ignore Raycast » :
    /// les entrées et sorties de zones (DonjonZone) arrivent par OnTriggerEnter / OnTriggerExit, sans calcul par image.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class DonjonCapteur : MonoBehaviour
    {
        public enum Role { Heros, Camera }

        public Role role = Role.Heros;
        [Tooltip("Transform suivi par la vague de masquage (le héros). Vide : ce transform.")]
        public Transform reference;

        readonly DonjonZone[] m_Zones = new DonjonZone[8];
        int m_Nb;

        void Reset() { Configurer(); }
        void Awake() { Configurer(); }

        void Configurer()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var sc = GetComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.2f;
            gameObject.layer = 2; // Ignore Raycast
        }

        void OnTriggerEnter(Collider autre)
        {
            DonjonZone z;
            if (!autre.TryGetComponent(out z)) return;
            for (int i = 0; i < m_Nb; i++) if (m_Zones[i] == z) return;
            if (m_Nb < m_Zones.Length) m_Zones[m_Nb++] = z;
            Signaler();
        }

        void OnTriggerExit(Collider autre)
        {
            DonjonZone z;
            if (!autre.TryGetComponent(out z)) return;
            for (int i = 0; i < m_Nb; i++)
            {
                if (m_Zones[i] != z) continue;
                for (int j = i; j < m_Nb - 1; j++) m_Zones[j] = m_Zones[j + 1];
                m_Nb--; m_Zones[m_Nb] = null;
                break;
            }
            if (m_Nb > 0) Signaler();
        }

        void OnDisable() { m_Nb = 0; }

        /// Zone courante = la dernière où l'on est entré et qu'on n'a pas quittée.
        void Signaler()
        {
            if (m_Nb == 0) return;
            DonjonZone z = m_Zones[m_Nb - 1];
            if (z.masquage == null) return;
            if (role == Role.Heros) z.masquage.Heros(z.bloc, z.niveau, reference != null ? reference : transform);
            else z.masquage.Camera(z.bloc);
        }
    }
}
