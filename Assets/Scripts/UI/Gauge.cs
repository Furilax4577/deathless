using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Jauge du HUD (vie, endurance, jauge de classe…) : un libellé, une valeur « 78 / 100 » et une barre arrondie.
    /// La couleur vient de la classe USS : dl-gauge--life, --stamina, --mana, --rage, --nyxessa, --shield.
    ///
    /// UXML : <Deathless.UI.Gauge label="Vie" value="78" max="100" class="dl-gauge--life" />
    [UxmlElement]
    public partial class Gauge : VisualElement
    {
        public const string UssClass = "dl-gauge";
        public const string HeaderUssClass = "dl-gauge__header";
        public const string LabelUssClass = "dl-gauge__label";
        public const string ValueUssClass = "dl-gauge__value";
        public const string TrackUssClass = "dl-gauge__track";
        public const string FillUssClass = "dl-gauge__fill";
        public const string ThinUssClass = "dl-gauge--thin";

        readonly VisualElement m_Header;
        readonly Label m_Label;
        readonly Label m_Value;
        readonly VisualElement m_Track;
        readonly VisualElement m_Fill;
        float m_Current = 100f;
        float m_Max = 100f;
        bool m_ShowValue = true;

        [UxmlAttribute("label")]
        public string label
        {
            get => m_Label.text;
            set
            {
                m_Label.text = value;
                UpdateHeader();
            }
        }

        [UxmlAttribute("value")]
        public float value
        {
            get => m_Current;
            set { m_Current = value; UpdateFill(); }
        }

        [UxmlAttribute("max")]
        public float max
        {
            get => m_Max;
            set { m_Max = Mathf.Max(0.0001f, value); UpdateFill(); }
        }

        [UxmlAttribute("show-value")]
        public bool showValue
        {
            get => m_ShowValue;
            set { m_ShowValue = value; UpdateFill(); UpdateHeader(); }
        }

        public float normalized => Mathf.Clamp01(m_Current / m_Max);

        public Gauge()
        {
            AddToClassList(UssClass);
            m_Header = new VisualElement { name = "header", pickingMode = PickingMode.Ignore };
            m_Header.AddToClassList(HeaderUssClass);
            m_Label = new Label { name = "label", pickingMode = PickingMode.Ignore };
            m_Label.AddToClassList(LabelUssClass);
            m_Value = new Label { name = "value", pickingMode = PickingMode.Ignore };
            m_Value.AddToClassList(ValueUssClass);
            m_Header.Add(m_Label);
            m_Header.Add(m_Value);
            Add(m_Header);

            m_Track = new VisualElement { name = "track", pickingMode = PickingMode.Ignore };
            m_Track.AddToClassList(TrackUssClass);
            m_Fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
            m_Fill.AddToClassList(FillUssClass);
            m_Track.Add(m_Fill);
            Add(m_Track);
            UpdateFill();
            UpdateHeader();
        }

        public void SetValue(float current, float maximum)
        {
            m_Max = Mathf.Max(0.0001f, maximum);
            m_Current = current;
            UpdateFill();
        }

        void UpdateFill()
        {
            if (m_Fill == null) return;
            m_Fill.style.width = Length.Percent(normalized * 100f);
            m_Value.text = Mathf.RoundToInt(m_Current) + " / " + Mathf.RoundToInt(m_Max);
            m_Value.style.display = m_ShowValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateHeader()
        {
            if (m_Header == null) return;
            var any = !string.IsNullOrEmpty(m_Label.text) || m_ShowValue;
            m_Header.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
