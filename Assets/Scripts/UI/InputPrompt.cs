using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Invite de bouton : l'icône de la touche liée à une action pour le dernier appareil utilisé, et un libellé facultatif.
    /// Se met à jour d'elle-même à chaque changement d'appareil (InputDeviceWatcher.Changed).
    ///
    /// UXML : <Deathless.UI.InputPrompt action="UI/Submit" label="Valider" />
    ///   action : « Carte/Action » ou nom seul (cherché dans InputGlyphs.Default.actions, soit DeathlessControls) ;
    ///   label  : texte à droite de l'icône (facultatif) ;
    ///   family : force une famille (KeyboardMouse, Xbox, PlayStation) au lieu de suivre l'appareil actif (facultatif).
    /// Une combinaison (LB + RB) affiche deux icônes séparées par « + ». Une touche sans icône est dessinée.
    [UxmlElement]
    public partial class InputPrompt : VisualElement
    {
        public const string UssClass = "dl-prompt";
        public const string IconsUssClass = "dl-prompt__icons";
        public const string IconUssClass = "dl-prompt__icon";
        public const string KeyUssClass = "dl-prompt__key";
        public const string PlusUssClass = "dl-prompt__plus";
        public const string LabelUssClass = "dl-prompt__label";
        public const string MissingUssClass = "dl-prompt--missing";

        static readonly List<InputGlyphs.Part> s_Parts = new List<InputGlyphs.Part>();

        readonly VisualElement m_Icons;
        readonly Label m_Label;
        string m_Action;
        string m_Text;
        bool m_HasFixedFamily;
        InputFamily m_FixedFamily;

        [UxmlAttribute("action")]
        public string actionName
        {
            get => m_Action;
            set { m_Action = value; Refresh(); }
        }

        [UxmlAttribute("label")]
        public string text
        {
            get => m_Text;
            set
            {
                m_Text = value;
                m_Label.text = value;
                m_Label.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// Vide = suit l'appareil actif ; sinon KeyboardMouse, Xbox ou PlayStation.
        [UxmlAttribute("family")]
        public string fixedFamily
        {
            get => m_HasFixedFamily ? m_FixedFamily.ToString() : string.Empty;
            set
            {
                m_HasFixedFamily = System.Enum.TryParse(value, true, out m_FixedFamily);
                Refresh();
            }
        }

        /// Famille affichée : la famille forcée, sinon l'appareil actif (Xbox hors mode Play, pour l'aperçu).
        public InputFamily displayedFamily
        {
            get
            {
                if (m_HasFixedFamily) return m_FixedFamily;
                return Application.isPlaying ? InputDeviceWatcher.Current : InputFamily.Xbox;
            }
        }

        public InputPrompt()
        {
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            m_Icons = new VisualElement { name = "icons", pickingMode = PickingMode.Ignore };
            m_Icons.AddToClassList(IconsUssClass);
            Add(m_Icons);
            m_Label = new Label { name = "label", pickingMode = PickingMode.Ignore };
            m_Label.AddToClassList(LabelUssClass);
            m_Label.style.display = DisplayStyle.None;
            Add(m_Label);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                InputDeviceWatcher.Changed -= OnDeviceChanged;
                InputDeviceWatcher.Changed += OnDeviceChanged;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => InputDeviceWatcher.Changed -= OnDeviceChanged);
        }

        public InputPrompt(string action, string label = null) : this()
        {
            text = label;
            actionName = action;
        }

        void OnDeviceChanged(InputFamily family) => Refresh();

        /// Reconstruit les icônes (appelé automatiquement ; utile après un changement de liaisons).
        public void Refresh()
        {
            if (m_Icons == null) return;
            m_Icons.Clear();
            var glyphs = InputGlyphs.Default;
            var family = displayedFamily;
            var action = glyphs != null ? glyphs.FindAction(m_Action) : null;
            var ok = glyphs != null && glyphs.Resolve(action, family, s_Parts);
            EnableInClassList(MissingUssClass, !ok);
            if (!ok)
            {
                if (!string.IsNullOrEmpty(m_Action)) AddKey("?");
                return;
            }

            foreach (var part in s_Parts)
            {
                if (part.isSeparator)
                {
                    var plus = new Label("+") { pickingMode = PickingMode.Ignore };
                    plus.AddToClassList(PlusUssClass);
                    m_Icons.Add(plus);
                }
                else if (part.icon != null)
                {
                    var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList(IconUssClass);
                    icon.style.backgroundImage = new StyleBackground(part.icon);
                    m_Icons.Add(icon);
                }
                else
                {
                    AddKey(part.text);
                }
            }
        }

        void AddKey(string keyText)
        {
            var key = new Label(keyText) { pickingMode = PickingMode.Ignore };
            key.AddToClassList(KeyUssClass);
            m_Icons.Add(key);
        }
    }
}
