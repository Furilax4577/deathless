using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Jauge de relevé du Renversé (statut Renversé, 26/09/2026 ; wiki : statuts.md) : petite jauge en espace écran
    /// sous le héros local le temps qu'il est à terre, avec l'invite du bouton Saut (InputPrompt) à marteler pour
    /// accélérer le relevé (ou à maintenir, en accessibilité ; OptionsJoueur.RelevageMaintenir). Se remplit à chaque
    /// martelage, léger tremblement. Placée chaque image en espace écran comme les statuts (HudStatuts) ; sans durée
    /// (lit DonneesUI.Relevage, IJaugeRelevage, comme les autres écrans : l'UI ne connaît pas le jeu ; purement local,
    /// chaque poste affiche seulement sa propre jauge, sur son propre héros).
    public sealed class HudRelevage
    {
        readonly VisualElement m_Racine, m_Jauge, m_Remplissage;

        public HudRelevage(VisualElement racine)
        {
            var hud = racine.Q("hud") ?? racine;
            m_Racine = new VisualElement { name = "relevage", pickingMode = PickingMode.Ignore };
            m_Racine.AddToClassList("hud-relevage");
            m_Racine.style.display = DisplayStyle.None;
            var prompt = new InputPrompt("Gameplay/Jump") { pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("hud-relevage__touche");
            m_Racine.Add(prompt);
            m_Jauge = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Jauge.AddToClassList("hud-relevage__jauge");
            m_Remplissage = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Remplissage.AddToClassList("hud-relevage__remplissage");
            m_Jauge.Add(m_Remplissage);
            m_Racine.Add(m_Jauge);
            hud.Add(m_Racine);
        }

        float m_DernierMartelementVu = -99f;

        /// Chaque image : masquée hors Renversé (ou sans source : seul le héros local du propriétaire a cette jauge).
        public void Maj()
        {
            var j = DonneesUI.Relevage;
            var cam = Camera.main;
            var panel = m_Racine.panel;
            if (j == null || !j.Visible || cam == null || panel == null)
            {
                m_Racine.style.display = DisplayStyle.None;
                return;
            }
            m_Racine.style.display = DisplayStyle.Flex;
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, j.Position + Vector3.up * 0.15f, cam);
            // Tremblement bref à chaque martelage (0,12 s), amorti.
            float depuis = Time.time - j.DernierMartelement;
            if (!Mathf.Approximately(j.DernierMartelement, m_DernierMartelementVu)) m_DernierMartelementVu = j.DernierMartelement;
            float secousse = depuis >= 0f && depuis < 0.12f ? (1f - depuis / 0.12f) : 0f;
            float dx = secousse > 0f ? (Mathf.PerlinNoise(Time.time * 45f, 0.5f) - 0.5f) * 8f * secousse : 0f;
            m_Racine.style.left = p.x - 30f + dx;
            m_Racine.style.top = p.y - 6f;
            m_Remplissage.style.width = Length.Percent(Mathf.Clamp01(j.Martelement) * 100f);
        }
    }
}
