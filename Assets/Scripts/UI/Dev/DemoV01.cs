using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Dev
{
    /// Décor de la démo 0.1 (remplace la scène 3D) : paysage des maquettes, de jour ou de nuit, avec Nyxessa.
    [UxmlElement]
    public partial class FondDemo : FormeHud
    {
        bool m_Nuit;

        [UxmlAttribute("nuit")]
        public bool nuit
        {
            get => m_Nuit;
            set { if (m_Nuit == value) return; m_Nuit = value; MarkDirtyRepaint(); }
        }

        protected override void Dessiner(Painter2D p, Rect r)
        {
            // Coordonnées des maquettes (1280 × 720), mises à l'échelle du panneau.
            var k = new Vector2(r.width / 1280f, r.height / 720f);
            Vector2 P(float x, float y) => new Vector2(r.x + x * k.x, r.y + y * k.y);
            void Rect(float x, float y, float w, float h, string c) => Polygone(p, Hex(c), P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h));

            Rect(0, 0, 1280, 720, m_Nuit ? "#1c2340" : "#8fc1e3");
            if (m_Nuit)
            {
                p.fillColor = Hex("#e8e4d8");
                foreach (var s in new[] { new Vector2(120, 60), new Vector2(300, 110), new Vector2(520, 40), new Vector2(760, 90), new Vector2(980, 50), new Vector2(1150, 120), new Vector2(860, 170), new Vector2(210, 190) })
                {
                    p.BeginPath();
                    p.Arc(P(s.x, s.y), 2f * k.x, 0f, 360f);
                    p.Fill();
                }
            }
            Polygone(p, Hex(m_Nuit ? "#24382c" : "#7fae6a"), P(0, 400), P(180, 330), P(360, 380), P(560, 310), P(760, 360), P(980, 300), P(1180, 350), P(1280, 330), P(1280, 460), P(0, 460));
            Rect(0, 430, 1280, 290, m_Nuit ? "#2c4436" : "#6f9f57");
            Polygone(p, Hex(m_Nuit ? "#5d5a52" : "#b9b2a2"), P(560, 720), P(720, 720), P(668, 440), P(612, 440));
            var arbre = m_Nuit ? "#15301f" : "#2f6b3f";
            var arbre2 = m_Nuit ? "#1b3a26" : "#3d7d4a";
            foreach (var t in new[] { new Vector2(40, 190), new Vector2(130, 150), new Vector2(230, 210), new Vector2(1060, 200), new Vector2(1150, 170), new Vector2(1230, 220), new Vector2(330, 120), new Vector2(950, 130) })
            {
                Rect(t.x - 5, 446, 10, 26, m_Nuit ? "#3a2a1e" : "#6b4a2e");
                Polygone(p, Hex(arbre), P(t.x - 40, 450), P(t.x + 40, 450), P(t.x, 450 - t.y));
                Polygone(p, Hex(arbre2), P(t.x - 28, 450 - t.y * 0.45f), P(t.x + 28, 450 - t.y * 0.45f), P(t.x, 450 - t.y));
            }
            foreach (var m in new[] { new Vector2(420, 70), new Vector2(820, 80) })
            {
                Rect(m.x, 395, m.y, 50, m_Nuit ? "#8e8a80" : "#efe6d2");
                Polygone(p, Hex(m_Nuit ? "#2b3f6e" : "#3b5fa8"), P(m.x - 8, 397), P(m.x + m.y + 8, 397), P(m.x + m.y / 2, 362));
            }
            // Nyxessa sur son rocher.
            Rect(610, 420, 60, 12, "#8c877f");
            if (m_Nuit)
            {
                foreach (var (rayon, a) in new[] { (70f, 0.10f), (48f, 0.16f), (30f, 0.24f) })
                {
                    var c = Hex("#3fae5a");
                    c.a = a;
                    p.fillColor = c;
                    p.BeginPath();
                    p.Arc(P(640, 385), rayon * k.x, 0f, 360f);
                    p.Fill();
                }
            }
            Polygone(p, Hex("#3fae5a"), P(640, 352), P(656, 386), P(640, 420), P(624, 386));
            Polygone(p, Hex("#9fe870"), P(640, 352), P(656, 386), P(640, 392));
            // Le Paladin, de dos.
            Rect(606, 550, 68, 104, "#4a5570");
            p.fillColor = Hex("#e2c9a4");
            p.BeginPath();
            p.Arc(P(640, 528), 24f * k.x, 0f, 360f);
            p.Fill();
            Rect(614, 500, 52, 22, "#3a4258");
        }
    }

    /// Démo des écrans 0.1 : le décor suit le jour et la nuit de la partie factice.
    [RequireComponent(typeof(UIDocument))]
    public class DemoV01 : MonoBehaviour
    {
        FondDemo m_Fond;

        void OnEnable() => m_Fond = GetComponent<UIDocument>().rootVisualElement.Q<FondDemo>();

        void Update()
        {
            if (m_Fond == null) return;
            var partie = DonneesUI.Partie;
            m_Fond.nuit = partie != null && (partie.Phase == PhasePartie.Nuit || partie.Phase == PhasePartie.Crepuscule
                                             || (partie.Phase == PhasePartie.Terminee));
        }
    }
}
