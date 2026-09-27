using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Chiffres de dégâts flottants (interface.md § « Barres de vie des ennemis », chiffres de dégâts) : s'abonne à
    /// DonneesUI.Degats (IDegatsSource, un événement par coup) plutôt que de lire quoi que ce soit image par image.
    /// Pool de trente éléments réutilisés au plus (les plus anciens s'effacent d'abord, interface.md) ; les tics
    /// continus (brûlure, tournante) se cumulent sur le même chiffre pour une même cible plutôt que d'en faire naître
    /// un nouveau. Classe à part : EcranHud ne fait que la créer et l'appeler.
    public sealed class HudDegats
    {
        public const int Max = 30;
        public const float Duree = 0.8f;
        public const float Amplitude = 60f;

        sealed class Chiffre
        {
            public Label label;
            /// Négatif : libre.
            public float age = -1f;
            public float montant;
            public bool continu;
            public int cleCible;
            public float baseTop;
            public bool critique;
        }

        readonly VisualElement m_Calque;
        readonly List<Chiffre> m_Pool = new List<Chiffre>();
        IDegatsSource m_Source;

        public HudDegats(VisualElement racine)
        {
            m_Calque = racine.Q("degats");
            if (m_Calque == null)
            {
                m_Calque = new VisualElement { name = "degats", pickingMode = PickingMode.Ignore };
                m_Calque.AddToClassList("hud-degats");
                (racine.Q("hud") ?? racine).Add(m_Calque);
            }
            m_Calque.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < Max; i++)
            {
                var c = new Chiffre { label = new Label { pickingMode = PickingMode.Ignore } };
                c.label.AddToClassList("chiffre");
                m_Calque.Add(c.label);
                m_Pool.Add(c);
            }
        }

        /// À appeler quand DonneesUI.Degats peut avoir changé (nouvelle partie) : HudStatuts et les autres composants
        /// du HUD lisent DonneesUI à chaque image, mais celui-ci s'abonne pour de vrai à un événement.
        public void Suivre()
        {
            var source = DonneesUI.Degats;
            if (ReferenceEquals(m_Source, source)) return;
            if (m_Source != null) m_Source.Degat -= OnDegat;
            m_Source = source;
            if (m_Source != null) m_Source.Degat += OnDegat;
        }

        void OnDegat(EvenementDegat e)
        {
            var panel = m_Calque.panel;
            var cam = Camera.main;
            if (panel == null || cam == null) return;
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, e.Point, cam);

            // Cumul des tics continus sur la même cible : grossit le chiffre en cours plutôt que d'en faire naître un
            // nouveau (interface.md : « les tics continus se cumulent »).
            if (e.Continu && e.CleCible != 0)
            {
                for (int i = 0; i < m_Pool.Count; i++)
                {
                    var c = m_Pool[i];
                    if (c.age < 0f || !c.continu || c.cleCible != e.CleCible) continue;
                    c.montant += e.Montant;
                    c.age = 0f;
                    c.baseTop = p.y;
                    c.label.style.left = p.x;
                    c.label.text = Mathf.RoundToInt(c.montant).ToString();
                    return;
                }
            }

            var c2 = Allouer();
            c2.age = 0f;
            c2.continu = e.Continu;
            c2.cleCible = e.CleCible;
            c2.montant = e.Montant;
            c2.critique = e.Type == TypeChiffreDegat.Critique;
            c2.baseTop = p.y;
            c2.label.text = e.Mot ?? (e.Type == TypeChiffreDegat.Soin ? "+" + Mathf.RoundToInt(e.Montant) : Mathf.RoundToInt(e.Montant).ToString());
            c2.label.style.left = p.x;
            c2.label.style.top = p.y;
            c2.label.style.display = DisplayStyle.Flex;
            c2.label.style.opacity = 1f;
            c2.label.style.scale = new StyleScale(new Scale(Vector3.one));
            c2.label.RemoveFromClassList("chiffre--normal");
            c2.label.RemoveFromClassList("chiffre--critique");
            c2.label.RemoveFromClassList("chiffre--brulure");
            c2.label.RemoveFromClassList("chiffre--recu");
            c2.label.RemoveFromClassList("chiffre--nyxessa");
            c2.label.RemoveFromClassList("chiffre--soin");
            c2.label.RemoveFromClassList("chiffre--mot");
            c2.label.AddToClassList(Classe(e.Type));
        }

        static string Classe(TypeChiffreDegat t) => t switch
        {
            TypeChiffreDegat.Critique => "chiffre--critique",
            TypeChiffreDegat.Brulure => "chiffre--brulure",
            TypeChiffreDegat.Recu => "chiffre--recu",
            TypeChiffreDegat.Nyxessa => "chiffre--nyxessa",
            TypeChiffreDegat.Soin => "chiffre--soin",
            TypeChiffreDegat.Mot => "chiffre--mot",
            _ => "chiffre--normal",
        };

        /// Un chiffre libre, sinon le plus ancien (trente au plus, les plus anciens s'effacent d'abord).
        Chiffre Allouer()
        {
            Chiffre plusVieux = null;
            for (int i = 0; i < m_Pool.Count; i++)
            {
                var c = m_Pool[i];
                if (c.age < 0f) return c;
                if (plusVieux == null || c.age > plusVieux.age) plusVieux = c;
            }
            return plusVieux;
        }

        public void Maj(float dt)
        {
            for (int i = 0; i < m_Pool.Count; i++)
            {
                var c = m_Pool[i];
                if (c.age < 0f) continue;
                c.age += dt;
                var progres = Mathf.Clamp01(c.age / Duree);
                if (c.age >= Duree)
                {
                    c.age = -1f;
                    c.label.style.display = DisplayStyle.None;
                    continue;
                }
                c.label.style.opacity = 1f - progres;
                c.label.style.top = c.baseTop - progres * Amplitude;
                if (c.critique)
                {
                    var jolt = progres < 0.15f ? 1f + 0.35f * (1f - progres / 0.15f) : 1f;
                    c.label.style.scale = new StyleScale(new Scale(new Vector3(jolt, jolt, 1f)));
                }
            }
        }
    }
}
