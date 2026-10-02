using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Deathless.Donjon.Terrasses
{
    /// Fermeture d'une pièce cachée : porte de bois à serrure (bronze, argent, or ou crochetable) ou paroi secrète (pan de mur
    /// qui s'enfonce dans le sol). Le NavMesh est construit porte ouverte ; l'obstacle (carving) la ferme jusqu'à l'ouverture.
    /// Le gameplay complet (clés achetées au mécano, crochetage, réseau) reste à brancher : Ouvrir() est l'unique entrée,
    /// à appeler par l'autorité puis à répliquer {à confirmer}.
    public class PorteDonjon : MonoBehaviour
    {
        public GenrePiece genre;
        public Serrure serrure;
        public int piece;
        [Tooltip("Paroi secrète : descente (m) ; porte : angle d'ouverture (°).")]
        public float course = 100f;
        public float duree = 1.2f;
        public bool Ouverte { get; private set; }
        public event Action<PorteDonjon> Ouverture;

        /// Difficulté de crochetage : 1 (simple ou bronze), 2 (argent), 3 (or) {à confirmer}.
        public int DifficulteCrochetage => serrure == Serrure.Or ? 3 : serrure == Serrure.Argent ? 2 : 1;

        public void Ouvrir()
        {
            if (Ouverte) return;
            Ouverte = true;
            if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(Animer());
            else Terminer(1f);
            Ouverture?.Invoke(this);
        }

        IEnumerator Animer()
        {
            for (float t = 0f; t < duree; t += Time.deltaTime)
            {
                Poser(Mathf.SmoothStep(0f, 1f, t / duree));
                yield return null;
            }
            Terminer(1f);
        }

        Vector3 m_Pos0; Quaternion m_Rot0; bool m_Init;

        void Poser(float k)
        {
            if (!m_Init) { m_Pos0 = transform.localPosition; m_Rot0 = transform.localRotation; m_Init = true; }
            if (genre == GenrePiece.Secrete) transform.localPosition = m_Pos0 + Vector3.down * (course * k);
            else transform.localRotation = m_Rot0 * Quaternion.Euler(0f, course * k, 0f);   // s'ouvre vers l'intérieur de la pièce
        }

        void Terminer(float k)
        {
            Poser(k);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var o in GetComponentsInChildren<NavMeshObstacle>()) o.enabled = false;
        }
    }

    /// Déclencheur d'une pièce secrète : plaque de pression (un héros marche dessus) ou bouton mural discret (un héros s'en
    /// approche ; l'interaction par touche viendra avec le gameplay).
    public class DeclencheurDonjon : MonoBehaviour
    {
        public GenreDeclencheur genre;
        public PorteDonjon cible;
        public Transform visuel;
        public bool Actif { get; private set; }

        void OnTriggerEnter(Collider c)
        {
            if (Actif || c.isTrigger) return;
            if (c.GetComponentInParent<CharacterController>() == null) return;
            Activer();
        }

        public void Activer()
        {
            if (Actif) return;
            Actif = true;
            if (visuel != null) visuel.localPosition += genre == GenreDeclencheur.PlaqueSol ? Vector3.down * 0.06f : Vector3.back * 0.08f;
            if (cible != null) cible.Ouvrir();
        }
    }

    /// Flamme de torche : léger vacillement de la lumière (en jeu seulement).
    public class TorcheDonjon : MonoBehaviour
    {
        public Light lumiere;
        public float intensite = 2.4f;
        float m_Phase;

        void Start() { m_Phase = (transform.position.x * 1.7f + transform.position.z * 3.1f) % 10f; }

        void Update()
        {
            if (lumiere == null) return;
            float t = Time.time * 7f + m_Phase;
            lumiere.intensity = intensite * (0.9f + 0.1f * Mathf.PerlinNoise(t, m_Phase));
        }
    }
}
