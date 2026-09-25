using System;
using UnityEngine;
using UnityEngine.Events;

namespace Deathless.Accessoires
{
    /// <summary>
    /// Séquence d'ouverture d'un cadenas (prefabs Cadenas_*) : la clé glisse dans la serrure, tourne d'un quart de tour
    /// avec le verrou, l'anse saute puis pivote autour de son bras long, puis le cadenas tombe et disparaît par la taille.
    /// Environ 1 s (réglable par <see cref="duree"/>). Aucune allocation par image : tout est calculé dans Update.
    ///
    /// Hiérarchie attendue (générée par CadenasBuilder) :
    ///   Cadenas_X (ce composant)
    ///   ├─ Corps   : corps, rivets, trou de serrure
    ///   ├─ Anse    : pivot à la base du bras long ; Y local = axe du bras long
    ///   └─ Verrou  : pivot sur l'axe du trou de serrure, à fleur de la face avant ; tourne autour de son Z local
    ///      └─ Serrure : point d'arrivée de la clé (Z = sens d'insertion, Y = côté du panneton)
    /// Clé (prefabs Cle_*) : pivot à la pointe de la tige, enfant Insertion (Z = sens d'insertion, Y = côté du panneton).
    /// </summary>
    [DisallowMultipleComponent]
    public class CadenasOuverture : MonoBehaviour
    {
        [Header("Pièces")]
        [SerializeField] Transform anse;
        [SerializeField] Transform verrou;
        [SerializeField] Transform serrure;
        [Tooltip("Position locale de l'anse quand le cadenas est fermé.")]
        [SerializeField] Vector3 anseFermee;

        [Header("Clé")]
        [Tooltip("Clé à utiliser (instance de la scène). Si vide, la clé est créée depuis clePrefab.")]
        public Transform cle;
        [SerializeField] GameObject clePrefab;
        [Tooltip("Distance (m) devant la serrure d'où part une clé créée depuis le prefab.")]
        [SerializeField] float approche = 0.15f;
        [Tooltip("Profondeur (m) d'enfoncement de la pointe de la clé.")]
        [SerializeField] float profondeur = 0.09f;

        [Header("Mouvement")]
        [SerializeField] float duree = 1f;
        [SerializeField] float angleVerrou = -90f;
        [SerializeField] float anseLevee = 0.075f;
        [Tooltip("Rotation de l'anse autour de son bras long (négatif : le bras court part vers l'arrière).")]
        [SerializeField] float angleAnse = -90f;
        [SerializeField] float chute = 0.22f;
        [SerializeField] float basculeChute = 18f;
        [Tooltip("Désactive l'objet une fois la séquence terminée.")]
        [SerializeField] bool desactiverALaFin = true;

        [Header("Son (optionnel)")]
        [SerializeField] AudioClip sonTour;
        [SerializeField] AudioClip sonDeverrouillage;
        [SerializeField] AudioSource source;

        [Header("Fin")]
        public UnityEvent quandOuvert = new UnityEvent();
        /// <summary>Levé à la fin de la séquence (cadenas réduit à zéro).</summary>
        public event Action<CadenasOuverture> Ouvert;

        public bool EnCours { get; private set; }
        public bool EstOuvert { get; private set; }

        // phases, en fraction de la durée
        const float CleDebut = 0f, CleFin = 0.25f;
        const float TourDebut = 0.25f, TourFin = 0.45f;
        const float SautDebut = 0.45f, SautFin = 0.60f;
        const float PivotDebut = 0.55f, PivotFin = 0.75f;
        const float ChuteDebut = 0.72f, ChuteFin = 1f;
        const float TailleDebut = 0.80f;

        float t;
        bool sonTourJoue, sonLoquetJoue, cleCreee;
        Vector3 posDepart, echelleDepart, anseDepartPos, clePosDepart, clePosFin;
        Quaternion rotDepart, anseDepartRot, verrouDepart, cleRotDepart, cleRotFin;
        Collider[] colliders;

        void Awake()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            enabled = false; // Update ne tourne que pendant la séquence
        }

        /// <summary>Lance la séquence. Sans effet si elle est déjà en cours ou terminée.</summary>
        public void Ouvrir()
        {
            if (EnCours || EstOuvert) return;
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = false;

            posDepart = transform.localPosition; rotDepart = transform.localRotation; echelleDepart = transform.localScale;
            anseDepartPos = anse.localPosition; anseDepartRot = anse.localRotation;
            verrouDepart = verrou.localRotation;

            // clé : rattachée au verrou pour tourner et tomber avec lui
            cleCreee = false;
            if (cle == null && clePrefab != null)
            {
                cle = Instantiate(clePrefab).transform;
                cleCreee = true;
            }
            if (cle != null)
            {
                var ins = cle.Find("Insertion");
                Quaternion insRot = ins != null ? ins.localRotation : Quaternion.identity;
                Vector3 insPos = ins != null ? ins.localPosition : Vector3.zero;
                // pose finale dans l'espace du verrou : Insertion alignée sur Serrure, pointe enfoncée de « profondeur »
                cleRotFin = serrure.localRotation * Quaternion.Inverse(insRot);
                var echelleCle = cle.lossyScale.x / Mathf.Max(1e-5f, verrou.lossyScale.x);
                clePosFin = serrure.localPosition + serrure.localRotation * new Vector3(0, 0, profondeur / verrou.lossyScale.x)
                            - cleRotFin * (insPos * echelleCle);
                if (cleCreee)
                {
                    cle.SetParent(verrou, false);
                    cle.localRotation = cleRotFin;
                    cle.localPosition = clePosFin - serrure.localRotation * new Vector3(0, 0, (approche + profondeur) / verrou.lossyScale.x);
                }
                else cle.SetParent(verrou, true);
                clePosDepart = cle.localPosition; cleRotDepart = cle.localRotation;
            }

            t = 0f; sonTourJoue = sonLoquetJoue = false;
            EnCours = true; enabled = true;
        }

        /// <summary>Remet le cadenas fermé (pour rejouer, ou pour un cadenas posé ouvert dans son prefab).</summary>
        public void Reinitialiser()
        {
            if (EnCours || EstOuvert)
            {
                transform.localPosition = posDepart; transform.localRotation = rotDepart; transform.localScale = echelleDepart;
            }
            EnCours = false; EstOuvert = false; enabled = false;
            anse.localPosition = anseFermee; anse.localRotation = Quaternion.identity;
            verrou.localRotation = Quaternion.identity;
            if (cle != null && cle.parent == verrou)
            {
                if (cleCreee) { if (Application.isPlaying) Destroy(cle.gameObject); else DestroyImmediate(cle.gameObject); cle = null; }
                else cle.SetParent(null, true);
            }
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = true;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (!EnCours) return;
            t += Time.deltaTime / Mathf.Max(0.01f, duree);
            Appliquer(t);
            if (t >= 1f) Terminer();
        }

        void Appliquer(float u)
        {
            // 1. la clé glisse dans la serrure
            if (cle != null)
            {
                float k = EaseOutCubic(Phase(u, CleDebut, CleFin));
                cle.localPosition = Vector3.LerpUnclamped(clePosDepart, clePosFin, k);
                cle.localRotation = Quaternion.SlerpUnclamped(cleRotDepart, cleRotFin, k);
            }

            // 2. un quart de tour (verrou et clé)
            float tour = EaseInOutCubic(Phase(u, TourDebut, TourFin));
            verrou.localRotation = verrouDepart * Quaternion.AngleAxis(angleVerrou * tour, Vector3.forward);
            if (!sonTourJoue && u >= TourDebut) { sonTourJoue = true; Jouer(sonTour); }

            // 3. l'anse saute, puis 4. pivote autour de son bras long
            float saut = EaseOutBack(Phase(u, SautDebut, SautFin));
            var haut = anseFermee + Vector3.up * anseLevee;
            if (anseDepartPos.y > haut.y - 1e-4f) haut = anseDepartPos; // déjà levée (cadenas posé ouvert)
            anse.localPosition = Vector3.LerpUnclamped(anseDepartPos, haut, saut);
            float piv = EaseInOutCubic(Phase(u, PivotDebut, PivotFin));
            anse.localRotation = anseDepartRot * Quaternion.AngleAxis(angleAnse * piv, Vector3.up);
            if (!sonLoquetJoue && u >= SautDebut) { sonLoquetJoue = true; Jouer(sonDeverrouillage); }

            // 5. chute et disparition par la taille
            float c = EaseInQuad(Phase(u, ChuteDebut, ChuteFin));
            transform.localPosition = posDepart + Vector3.down * (chute * c);
            transform.localRotation = rotDepart * Quaternion.AngleAxis(basculeChute * c, Vector3.forward);
            float s = 1f - EaseInCubic(Phase(u, TailleDebut, ChuteFin));
            transform.localScale = echelleDepart * s;
        }

        void Terminer()
        {
            Appliquer(1f);
            EnCours = false; EstOuvert = true; enabled = false;
            quandOuvert?.Invoke();
            Ouvert?.Invoke(this);
            if (desactiverALaFin) gameObject.SetActive(false);
        }

        void Jouer(AudioClip clip)
        {
            if (clip == null) return;
            if (source != null) source.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        static float Phase(float u, float a, float b) => Mathf.Clamp01((u - a) / (b - a));
        static float EaseOutCubic(float x) { x = 1f - x; return 1f - x * x * x; }
        static float EaseInCubic(float x) => x * x * x;
        static float EaseInQuad(float x) => x * x;
        static float EaseInOutCubic(float x) => x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) * 0.5f;
        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float y = x - 1f;
            return 1f + c3 * y * y * y + c1 * y * y;
        }

#if UNITY_EDITOR
        /// <summary>Pose de la séquence à l'instant u (0..1), pour les captures en édition.</summary>
        public void Echantillonner(float u)
        {
            if (!EnCours) Ouvrir();
            sonTourJoue = sonLoquetJoue = true; // pas de son en édition
            t = u; Appliquer(Mathf.Clamp01(u));
        }
#endif
    }
}
