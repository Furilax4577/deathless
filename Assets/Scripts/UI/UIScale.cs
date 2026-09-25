using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Taille de l'interface (Options > Jeu > Taille de l'interface : ×1, ×2, ×3), mémorisée dans les PlayerPrefs.
    /// Le niveau multiplie PanelSettings.scale, par-dessus la mise à l'échelle 1920×1080 du panneau.
    /// ×1 = 0,8 ; ×2 = 1 (taille des maquettes, défaut) ; ×3 = 1,35 (confort de lecture sur télévision).
    /// À ×3, l'espace logique d'un écran 16:9 n'est plus que d'environ 1422×800 : les écrans denses doivent
    /// défiler ou se réorganiser. La racine de chaque UIDocument reçoit la classe dl-scale-1, -2 ou -3.
    ///
    /// À poser une fois par scène (ou sur un objet persistant) avec le ou les PanelSettings du jeu.
    /// Dans l'éditeur, l'échelle d'origine de l'asset est rétablie à la sortie du mode Play.
    [DefaultExecutionOrder(-100)]
    public class UIScale : MonoBehaviour
    {
        public const string PrefsKey = "deathless.ui.scale";
        public const int MinLevel = 1;
        public const int MaxLevel = 3;
        public const int DefaultLevel = 2;

        /// Facteur appliqué pour chaque niveau (index 0 = ×1). Décision utilisateur du 25/09/2026.
        static readonly float[] s_Factors = { 0.8f, 1f, 1.35f };

        /// Classe USS posée sur la racine des écrans selon le niveau (dl-scale-1, dl-scale-2, dl-scale-3).
        public static string ClassFor(int level) => "dl-scale-" + Mathf.Clamp(level, MinLevel, MaxLevel);

        /// Pose la classe du niveau actuel sur une racine d'écran (à appeler à l'activation d'un écran).
        public static void TagRoot(VisualElement root)
        {
            if (root == null) return;
            for (var l = MinLevel; l <= MaxLevel; l++) root.EnableInClassList(ClassFor(l), l == Level);
        }

        [Tooltip("Panneaux dont l'échelle suit le réglage.")]
        public PanelSettings[] panels = Array.Empty<PanelSettings>();

        float[] m_BaseScales;

        /// Niveau actuel (1, 2 ou 3).
        public static int Level
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, DefaultLevel), MinLevel, MaxLevel);
            set
            {
                var clamped = Mathf.Clamp(value, MinLevel, MaxLevel);
                if (clamped == Level && PlayerPrefs.HasKey(PrefsKey)) return;
                PlayerPrefs.SetInt(PrefsKey, clamped);
                PlayerPrefs.Save();
                Changed?.Invoke(clamped);
            }
        }

        /// Facteur de l'échelle actuelle.
        public static float Factor => FactorFor(Level);

        public static float FactorFor(int level) => s_Factors[Mathf.Clamp(level, MinLevel, MaxLevel) - 1];

        /// Appelé quand le niveau change (le composant s'en sert pour appliquer l'échelle).
        public static event Action<int> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;

        void OnEnable()
        {
            m_BaseScales = new float[panels.Length];
            for (var i = 0; i < panels.Length; i++)
                m_BaseScales[i] = panels[i] != null ? panels[i].scale : 1f;
            Changed += Apply;
            Apply(Level);
        }

        void OnDisable()
        {
            Changed -= Apply;
            if (m_BaseScales == null) return;
            for (var i = 0; i < panels.Length && i < m_BaseScales.Length; i++)
                if (panels[i] != null) panels[i].scale = m_BaseScales[i];
        }

        void Apply(int level)
        {
            var factor = FactorFor(level);
            for (var i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].scale = m_BaseScales[i] * factor;
            foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                if (Array.IndexOf(panels, document.panelSettings) >= 0) TagRoot(document.rootVisualElement);
        }
    }
}
