using Deathless.Jeu;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Jauge de relevé du Renversé (statut Renversé, 26/09/2026 ; wiki : statuts.md) : petite jauge en espace écran
    /// sous le héros local le temps qu'il est à terre, avec l'invite du bouton Saut (InputPrompt) à marteler pour
    /// accélérer le relevé (ou à maintenir, en accessibilité ; OptionsJoueur.RelevageMaintenir). Se remplit à chaque
    /// martelage, léger tremblement. Placée chaque image en espace écran comme les statuts (HudStatuts) ; sans durée
    /// (Heros.RenverseProgression/RenverseMartelementRatio, purement local : rien à lire du réseau ici, chaque poste
    /// affiche seulement sa propre jauge, sur son propre héros).
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

        /// Chaque image : masquée hors Renversé (ou sans héros local, marionnette comprise — Heros.Distant n'a pas
        /// cette jauge, seul le propriétaire martèle).
        public void Maj()
        {
            var h = Partie.Instance != null ? Partie.Instance.HerosLocal : null;
            var cam = Camera.main;
            var panel = m_Racine.panel;
            if (h == null || h.Distant || !h.EnRenverse || cam == null || panel == null)
            {
                m_Racine.style.display = DisplayStyle.None;
                return;
            }
            m_Racine.style.display = DisplayStyle.Flex;
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, h.transform.position + Vector3.up * 0.15f, cam);
            // Tremblement bref à chaque martelage (0,12 s), amorti.
            float depuis = Time.time - h.RenverseDernierMartelement;
            if (!Mathf.Approximately(h.RenverseDernierMartelement, m_DernierMartelementVu)) m_DernierMartelementVu = h.RenverseDernierMartelement;
            float secousse = depuis >= 0f && depuis < 0.12f ? (1f - depuis / 0.12f) : 0f;
            float dx = secousse > 0f ? (Mathf.PerlinNoise(Time.time * 45f, 0.5f) - 0.5f) * 8f * secousse : 0f;
            m_Racine.style.left = p.x - 30f + dx;
            m_Racine.style.top = p.y - 6f;
            m_Remplissage.style.width = Length.Percent(Mathf.Clamp01(h.RenverseMartelementRatio) * 100f);
        }
    }
}
