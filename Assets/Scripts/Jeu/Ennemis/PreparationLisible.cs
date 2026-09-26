using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Lisibilité du coup en préparation (retour de Quentin, 0.5.4 : « les ennemis sont durs à lire »), piste retenue au
    /// temps 1 du chantier lisibilité-caméra : les yeux du squelette (jaune-orangé ordinaire, rouge élite, déjà posés par
    /// Squelette/MarquerElite) montent en intensité pendant Squelette.PreparationProgress, avec une pulsation dans le
    /// dernier tiers pour marquer l'instant du coup. Purement visuel et local (chaque poste la joue pour ses squelettes,
    /// comme AuraElite) : par MaterialPropertyBlock, donc sans dupliquer ni modifier le matériau partagé Yeux_Squelette /
    /// Yeux_Elite (les autres squelettes qui portent le même matériau ne sont pas affectés).
    ///
    /// Choisie plutôt que les autres pistes (arme qui brille, trait au sol, contour, préparation plus lente) parce que
    /// l'émission reste lisible même à contre-jour la nuit (l'éclairage de Nyxessa passe derrière l'ennemi) : un contour
    /// dépend de la lumière ambiante, un trait au sol est masqué par l'ennemi lui-même de face, alors qu'une source
    /// émissive plein cadre sur le crâne reste visible quel que soit l'éclairage. Sans vert (réservé à Nyxessa) : la
    /// teinte suit celle déjà posée par le matériau (jaune-orangé, rouge élite, bleu glacé martache).
    public class PreparationLisible : MonoBehaviour
    {
        [Tooltip("Multiplicateur d'émission à l'instant de l'impact (1 = pas de changement).")]
        public float intensiteMax = 3.2f;
        [Tooltip("Fréquence de la pulsation dans le dernier tiers de la préparation (Hz).")]
        public float pulsationHz = 6f;
        [Tooltip("Amplitude de la pulsation (fraction de intensiteMax), montée en rampe sur le dernier tiers.")]
        public float pulsationAmplitude = 0.35f;
        [Tooltip("Fraction de la préparation à partir de laquelle la pulsation démarre.")]
        public float seuilPulsation = 0.7f;

        Squelette m_Squelette;
        Renderer[] m_Yeux;
        MaterialPropertyBlock m_Bloc;
        Color m_BaseEmission;
        bool m_A;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        // En Start (pas Awake) : posé automatiquement par Squelette.Awake (AddComponent), donc son propre Awake tournerait
        // avant que le poste ait fini d'équiper le squelette -- élite (Squelette.MarquerElite, yeux rouges) ou Morgrim
        // Martache (yeux bleu glacé posés sur le prefab) changent le matériau des yeux après l'Instantiate, dans le même
        // appel que celui-ci (DirecteurVagues.Poser : Instantiate -> MarquerElite -> Initialiser), avant que Start ne
        // s'exécute. Lire l'émission de base en Start capture donc la bonne teinte (rouge élite, bleu glacé Martache,
        // jaune-orangé ordinaire) au lieu de celle du matériau posé sur le prefab avant l'échange.
        void Start()
        {
            m_Squelette = GetComponent<Squelette>();
            var liste = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.name.EndsWith("_Eyes")) liste.Add(r);
            m_Yeux = liste.ToArray();
            m_Bloc = new MaterialPropertyBlock();
            if (m_Yeux.Length > 0 && m_Yeux[0].sharedMaterial != null && m_Yeux[0].sharedMaterial.HasProperty(EmissionId))
            {
                m_BaseEmission = m_Yeux[0].sharedMaterial.GetColor(EmissionId);
                m_A = true;
            }
        }

        void Update()
        {
            if (!m_A || m_Squelette == null || m_Yeux.Length == 0) return;
            if (m_Bloc == null) m_Bloc = new MaterialPropertyBlock();   // garde-fou : rechargement de domaine en cours de Play
            float k = m_Squelette.PreparationProgress;
            float facteur = 1f;
            if (k > 0f)
            {
                facteur = Mathf.Lerp(1f, intensiteMax, k);
                if (k > seuilPulsation)
                {
                    float t = (k - seuilPulsation) / Mathf.Max(0.01f, 1f - seuilPulsation);
                    facteur += pulsationAmplitude * intensiteMax * t * Mathf.Max(0f, Mathf.Sin(Time.time * pulsationHz * Mathf.PI * 2f));
                }
            }
            Color c = m_BaseEmission * facteur;
            foreach (var r in m_Yeux)
            {
                if (r == null) continue;
                r.GetPropertyBlock(m_Bloc);
                m_Bloc.SetColor(EmissionId, c);
                r.SetPropertyBlock(m_Bloc);
            }
        }
    }
}
