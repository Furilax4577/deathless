using System.Collections.Generic;
using Deathless.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Deathless.UI.Dev
{
    /// Écran de démonstration du socle (scène UISocle) : onglets pilotés par LB/RB (Q/E), réglages, échelle de
    /// l'interface, table de toutes les actions avec leur invite, et indicateur de l'appareil détecté.
    [RequireComponent(typeof(UIDocument))]
    public class UISocleDemo : MonoBehaviour
    {
        public InputActionAsset actions;

        [Tooltip("Délai d'accord de la manette (LB + RB), en secondes.")]
        public float chordWindow = InputChordResolver.DefaultChordWindow;

        /// Dernières actions de jeu résolues (la plus récente en premier), pour la démo et les tests.
        public readonly List<string> history = new List<string>();

        // Libellés français des actions (ordre d'affichage de la table).
        static readonly KeyValuePair<string, string>[] s_GameplayRows =
        {
            Row("Gameplay/Move", "Se déplacer"),
            Row("Gameplay/Look", "Caméra"),
            Row("Gameplay/Jump", "Sauter"),
            Row("Gameplay/Dodge", "Esquive, roulade"),
            Row("Gameplay/Interact", "Interagir, parler"),
            Row("Gameplay/CharacterMenu", "Menu du personnage"),
            Row("Gameplay/AttackPrimary", "Attaque principale"),
            Row("Gameplay/AttackSecondary", "Attaque secondaire, garde, visée"),
            Row("Gameplay/Skill1", "Compétence 1"),
            Row("Gameplay/Skill2", "Compétence 2"),
            Row("Gameplay/Skill3", "Compétence 3"),
            Row("Gameplay/Sprint", "Sprinter"),
            Row("Gameplay/DrinkPotion", "Boire une potion"),
            Row("Gameplay/Ready", "Se déclarer prêt"),
            Row("Gameplay/Pause", "Pause"),
        };

        static readonly KeyValuePair<string, string>[] s_UiRows =
        {
            Row("UI/Navigate", "Naviguer"),
            Row("UI/Submit", "Valider"),
            Row("UI/Cancel", "Retour"),
            Row("UI/TabPrevious", "Onglet précédent"),
            Row("UI/TabNext", "Onglet suivant"),
            Row("UI/Reset", "Réinitialiser"),
            Row("UI/Pause", "Pause"),
        };

        static KeyValuePair<string, string> Row(string action, string label) => new KeyValuePair<string, string>(action, label);

        readonly List<Button> m_Tabs = new List<Button>();
        readonly List<Button> m_ScaleButtons = new List<Button>();
        VisualElement m_Root;
        VisualElement m_PageJeu;
        VisualElement m_PageAutre;
        Label m_PageAutreTitle;
        Label m_DeviceLabel;
        Label m_ActionsDevice;
        Label m_BarDevice;
        VisualElement m_DeviceIcon;
        Button m_FirstMenuItem;
        int m_Tab;
        InputActionMap m_UiMap;
        InputAction m_TabPrevious;
        InputAction m_TabNext;
        InputAction m_Reset;
        InputChordResolver m_Chords;
        Label m_LastAction;
        Label m_LastHistory;
        readonly Dictionary<string, string> m_Labels = new Dictionary<string, string>();

        void OnEnable()
        {
            if (actions == null && InputGlyphs.Default != null) actions = InputGlyphs.Default.actions;
            m_Root = GetComponent<UIDocument>().rootVisualElement;
            if (m_Root == null) return;

            m_PageJeu = m_Root.Q("page-jeu");
            m_PageAutre = m_Root.Q("page-autre");
            m_PageAutreTitle = m_Root.Q<Label>("page-autre-title");
            m_DeviceLabel = m_Root.Q<Label>("device-label");
            m_DeviceIcon = m_Root.Q("device-icon");
            m_ActionsDevice = m_Root.Q<Label>("actions-device");
            m_BarDevice = m_Root.Q<Label>("bar-device");
            m_FirstMenuItem = m_Root.Q<Button>("menu-solo");

            m_Tabs.Clear();
            m_Root.Query<Button>(className: "dl-tab").ForEach(t => m_Tabs.Add(t));
            for (var i = 0; i < m_Tabs.Count; i++)
            {
                var index = i;
                m_Tabs[i].clicked += () => SelectTab(index);
            }
            SelectTab(0);

            m_ScaleButtons.Clear();
            for (var level = UIScale.MinLevel; level <= UIScale.MaxLevel; level++)
            {
                var button = m_Root.Q<Button>("scale-" + level);
                if (button == null) continue;
                var captured = level;
                button.clicked += () => UIScale.Level = captured;
                m_ScaleButtons.Add(button);
            }
            UIScale.Changed += OnScaleChanged;
            OnScaleChanged(UIScale.Level);

            UINavigation.SetupScreen(m_Root);
            UIScale.TagRoot(m_Root);
            BuildActionTable("actions-gameplay", s_GameplayRows);
            BuildActionTable("actions-ui", s_UiRows);

            InputDeviceWatcher.Changed += OnDeviceChanged;
            OnDeviceChanged(InputDeviceWatcher.Current);

            if (actions != null)
            {
                m_UiMap = actions.FindActionMap("UI", throwIfNotFound: false);
                m_TabPrevious = actions.FindAction("UI/TabPrevious");
                m_TabNext = actions.FindAction("UI/TabNext");
                m_Reset = actions.FindAction("UI/Reset");
                if (m_TabPrevious != null) m_TabPrevious.performed += OnTabPrevious;
                if (m_TabNext != null) m_TabNext.performed += OnTabNext;
                if (m_Reset != null) m_Reset.performed += OnReset;
                m_UiMap?.Enable();

                // Carte Gameplay : toutes les actions passent par le résolveur d'accords (affichage « Dernière action »).
                foreach (var row in s_GameplayRows) m_Labels[row.Key] = row.Value;
                m_LastAction = m_Root.Q<Label>("last-action");
                m_LastHistory = m_Root.Q<Label>("last-history");
                m_Chords = InputChordResolver.ForGameplay(actions, chordWindow);
                m_Chords.Triggered += OnGameplayTriggered;
                actions.FindActionMap("Gameplay")?.Enable();
            }

            // Focus initial : indispensable pour naviguer à la manette sans souris.
            m_Root.schedule.Execute(() => UINavigation.Focus(m_FirstMenuItem)).StartingIn(50);
        }

        void OnDisable()
        {
            UIScale.Changed -= OnScaleChanged;
            InputDeviceWatcher.Changed -= OnDeviceChanged;
            if (m_TabPrevious != null) m_TabPrevious.performed -= OnTabPrevious;
            if (m_TabNext != null) m_TabNext.performed -= OnTabNext;
            if (m_Reset != null) m_Reset.performed -= OnReset;
            m_Chords?.Dispose();
            m_Chords = null;
        }

        void OnGameplayTriggered(InputAction action)
        {
            var key = action.actionMap.name + "/" + action.name;
            var label = m_Labels.TryGetValue(key, out var l) ? l : action.name;
            history.Insert(0, label);
            if (history.Count > 12) history.RemoveAt(history.Count - 1);
            if (m_LastAction != null) m_LastAction.text = label;
            if (m_LastHistory != null)
                m_LastHistory.text = history.Count > 1 ? "Avant : " + string.Join(", ", history.GetRange(1, Mathf.Min(4, history.Count - 1))) : "";
        }

        void BuildActionTable(string containerName, KeyValuePair<string, string>[] rows)
        {
            var container = m_Root.Q(containerName);
            if (container == null) return;
            foreach (var row in rows)
            {
                var line = new VisualElement();
                line.AddToClassList("socle-action-row");
                var label = new Label(row.Value);
                label.AddToClassList("socle-action-row__label");
                line.Add(label);
                line.Add(new InputPrompt(row.Key));
                // Les lignes passent avant le bloc « Dernière action » s'il est dans la colonne.
                var box = container.Q("last-action-box");
                if (box != null) container.Insert(container.IndexOf(box), line);
                else container.Add(line);
            }
        }

        void OnTabPrevious(InputAction.CallbackContext _) => SelectTab((m_Tab + m_Tabs.Count - 1) % Mathf.Max(1, m_Tabs.Count));
        void OnTabNext(InputAction.CallbackContext _) => SelectTab((m_Tab + 1) % Mathf.Max(1, m_Tabs.Count));

        void OnReset(InputAction.CallbackContext _)
        {
            var toggle = m_Root.Q<Toggle>("vibrations");
            if (toggle != null) toggle.value = true;
            var invert = m_Root.Q<Toggle>("inversion");
            if (invert != null) invert.value = false;
            var slider = m_Root.Q<Slider>("sensibilite");
            if (slider != null) slider.value = 6f;
            var dropdown = m_Root.Q<DropdownField>("langue");
            if (dropdown != null) dropdown.index = 0;
            UIScale.Level = UIScale.DefaultLevel;
        }

        void SelectTab(int index)
        {
            if (m_Tabs.Count == 0) return;
            m_Tab = Mathf.Clamp(index, 0, m_Tabs.Count - 1);
            for (var i = 0; i < m_Tabs.Count; i++)
                m_Tabs[i].EnableInClassList("dl-tab--selected", i == m_Tab);
            var isJeu = m_Tab == 0;
            if (m_PageJeu != null) m_PageJeu.style.display = isJeu ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_PageAutre != null) m_PageAutre.style.display = isJeu ? DisplayStyle.None : DisplayStyle.Flex;
            if (m_PageAutreTitle != null) m_PageAutreTitle.text = m_Tabs[m_Tab].text.ToUpperInvariant();
        }

        void OnScaleChanged(int level)
        {
            UIScale.TagRoot(m_Root);
            for (var i = 0; i < m_ScaleButtons.Count; i++)
                m_ScaleButtons[i].EnableInClassList("dl-button--selected", i + UIScale.MinLevel == level);
        }

        void OnDeviceChanged(InputFamily family)
        {
            var name = InputDeviceWatcher.DisplayName(family);
            if (m_DeviceLabel != null) m_DeviceLabel.text = family == InputFamily.KeyboardMouse ? name : name + " détectée";
            if (m_ActionsDevice != null) m_ActionsDevice.text = name;
            if (m_BarDevice != null) m_BarDevice.text = family == InputFamily.KeyboardMouse ? name : name + " détectée";
            var glyphs = InputGlyphs.Default;
            if (m_DeviceIcon != null && glyphs != null)
                m_DeviceIcon.style.backgroundImage = new StyleBackground(glyphs.DeviceIcon(family));

            // Passage à la manette sans rien de focus : on redonne le focus au premier bouton, sinon la manette est inerte.
            if (family != InputFamily.KeyboardMouse && m_Root?.panel?.focusController?.focusedElement == null)
                UINavigation.Focus(m_FirstMenuItem);
        }
    }
}
