using System.Collections.Generic;
using Deathless.Audio;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Roue à emotes : calque du HUD (pas un écran de la pile : la carte Gameplay reste active, car la touche est
    /// maintenue). Créée par NavigateurEcrans depuis Assets/UI/Screens/RoueEmotes/RoueEmotes.uxml, affichée tant que le HUD
    /// est au sommet et que DonneesUI.RoueEmotes est ouverte. Huit secteurs autour du centre (icône et nom), le secteur
    /// pointé en or ; au centre, le nom de l'emote pointée (ou « Annuler ») et l'invite de Gameplay/Emote.
    public class CalqueRoueEmotes
    {
        public VisualElement Racine { get; }
        readonly FondRoueEmotes m_Fond;
        readonly VisualElement m_Secteurs;
        readonly Label m_Nom, m_Aide;
        readonly List<VisualElement> m_Cases = new List<VisualElement>();
        IReadOnlyList<IEmoteRoue> m_Emotes;
        int m_Pointee = -2;
        bool m_Visible;

        /// Rayon (fraction du rayon de la roue) où sont centrés l'icône et le nom de chaque secteur.
        const float RayonCases = 0.74f;

        public CalqueRoueEmotes(VisualTreeAsset uxml, VisualElement conteneur)
        {
            Racine = uxml != null ? uxml.Instantiate() : new VisualElement();
            Racine.name = "CalqueRoueEmotes";
            Racine.pickingMode = PickingMode.Ignore;
            Racine.style.position = Position.Absolute;
            Racine.style.left = Racine.style.top = Racine.style.right = Racine.style.bottom = 0;
            conteneur.Add(Racine);
            m_Fond = Racine.Q<FondRoueEmotes>("roue-fond");
            m_Secteurs = Racine.Q("roue-secteurs");
            m_Nom = Racine.Q<Label>("roue-nom");
            m_Aide = Racine.Q<Label>("roue-aide");
            if (uxml == null) Debug.LogWarning("[Emotes] NavigateurEcrans.roueEmotes n'est pas renseigné (Assets/UI/Screens/RoueEmotes/RoueEmotes.uxml)");
            Racine.style.display = DisplayStyle.None;
        }

        /// Chaque image : `roue` null (HUD pas au sommet, pas de héros local) ou fermée → calque caché.
        public void MiseAJour(IRoueEmotes roue)
        {
            bool voir = roue != null && roue.Ouverte && m_Secteurs != null;
            if (voir != m_Visible)
            {
                m_Visible = voir;
                Racine.style.display = voir ? DisplayStyle.Flex : DisplayStyle.None;
                if (voir) { Racine.BringToFront(); m_Pointee = -2; }
            }
            if (!voir) return;
            if (!ReferenceEquals(m_Emotes, roue.Emotes)) Construire(roue.Emotes);
            int pointee = roue.Pointee;
            if (pointee == m_Pointee) return;
            if (m_Pointee != -2 && pointee >= 0) VolumesAudio.JouerInterface(SonInterface.Survol, 0.6f);
            m_Pointee = pointee;
            for (int i = 0; i < m_Cases.Count; i++) m_Cases[i].EnableInClassList("roue__case--pointee", i == pointee);
            if (m_Fond != null) m_Fond.Pointe = pointee;
            bool choix = pointee >= 0 && pointee < m_Emotes.Count;
            if (m_Nom != null)
            {
                m_Nom.text = choix ? m_Emotes[pointee].Nom : "Annuler";
                m_Nom.EnableInClassList("roue__nom--annuler", !choix);
            }
            if (m_Aide != null) m_Aide.text = choix ? "Relâcher pour lancer" : "Pointer une emote";
        }

        void Construire(IReadOnlyList<IEmoteRoue> emotes)
        {
            m_Emotes = emotes;
            m_Secteurs.Clear();
            m_Cases.Clear();
            int n = emotes != null ? emotes.Count : 0;
            if (m_Fond != null) m_Fond.Secteurs = Mathf.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;   // 0 en haut, sens horaire
                var c = new VisualElement { pickingMode = PickingMode.Ignore };
                c.AddToClassList("roue__case");
                c.style.left = Length.Percent(50f + 50f * RayonCases * Mathf.Sin(a));
                c.style.top = Length.Percent(50f - 50f * RayonCases * Mathf.Cos(a));
                var icone = new VisualElement { pickingMode = PickingMode.Ignore };
                icone.AddToClassList("roue__icone");
                icone.AddToClassList("dl-icone");
                if (!IconesUI.Poser(icone, emotes[i].Icone)) icone.style.display = DisplayStyle.None;
                c.Add(icone);
                var nom = new Label(emotes[i].Nom) { pickingMode = PickingMode.Ignore };
                nom.AddToClassList("roue__case-nom");
                c.Add(nom);
                m_Secteurs.Add(c);
                m_Cases.Add(c);
            }
            m_Pointee = -2;
        }
    }

}

namespace Deathless.UI
{
    /// Fond de la roue à emotes (Ecrans.CalqueRoueEmotes) : un anneau de secteurs (Painter2D, net à toutes les tailles), le secteur pointé mis en évidence.
    /// Couleurs tirées de l'USS (propriétés --roue-*, qui reprennent les jetons du thème).
    [UxmlElement]
    public partial class FondRoueEmotes : VisualElement
    {
        static readonly CustomStyleProperty<Color> s_Fond = new CustomStyleProperty<Color>("--roue-fond");
        static readonly CustomStyleProperty<Color> s_FondPointe = new CustomStyleProperty<Color>("--roue-fond-pointe");
        static readonly CustomStyleProperty<Color> s_Bord = new CustomStyleProperty<Color>("--roue-bord");
        static readonly CustomStyleProperty<Color> s_BordPointe = new CustomStyleProperty<Color>("--roue-bord-pointe");
        static readonly CustomStyleProperty<float> s_Trou = new CustomStyleProperty<float>("--roue-trou");

        Color m_Fond = new Color(0.137f, 0.165f, 0.227f, 0.95f);        // --dl-color-panel-95
        Color m_FondPointe = new Color(0.169f, 0.2f, 0.282f, 0.97f);   // --dl-color-panel-active
        Color m_Bord = new Color(0.239f, 0.275f, 0.376f, 1f);          // --dl-color-border
        Color m_BordPointe = new Color(0.851f, 0.698f, 0.392f, 1f);    // --dl-color-gold
        float m_Trou = 0.36f;
        int m_Secteurs = 8, m_Pointe = -1;

        public int Secteurs { get => m_Secteurs; set { if (m_Secteurs != value) { m_Secteurs = value; MarkDirtyRepaint(); } } }
        public int Pointe { get => m_Pointe; set { if (m_Pointe != value) { m_Pointe = value; MarkDirtyRepaint(); } } }

        public FondRoueEmotes()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Dessiner;
            RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                var s = e.customStyle;
                if (s.TryGetValue(s_Fond, out var c)) m_Fond = c;
                if (s.TryGetValue(s_FondPointe, out c)) m_FondPointe = c;
                if (s.TryGetValue(s_Bord, out c)) m_Bord = c;
                if (s.TryGetValue(s_BordPointe, out c)) m_BordPointe = c;
                if (s.TryGetValue(s_Trou, out float f)) m_Trou = Mathf.Clamp(f, 0.05f, 0.9f);
                MarkDirtyRepaint();
            });
        }

        void Dessiner(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float rext = Mathf.Min(r.width, r.height) * 0.5f - 3f;
            float rint = rext * m_Trou;
            float pas = 360f / m_Secteurs;
            float jeu = 1.6f;   // demi-écart entre deux secteurs (degrés)
            for (int i = 0; i < m_Secteurs; i++)
            {
                if (i == m_Pointe) continue;
                Secteur(p, c, rint, rext, i * pas - pas * 0.5f + jeu, i * pas + pas * 0.5f - jeu, m_Fond, m_Bord, 2f);
            }
            if (m_Pointe >= 0 && m_Pointe < m_Secteurs)
                Secteur(p, c, rint, rext + 2f, m_Pointe * pas - pas * 0.5f + jeu, m_Pointe * pas + pas * 0.5f - jeu, m_FondPointe, m_BordPointe, 3f);
        }

        /// Secteur d'anneau entre deux angles « de la roue » (0 en haut, sens horaire).
        static void Secteur(Painter2D p, Vector2 c, float rint, float rext, float de, float a, Color fond, Color bord, float trait)
        {
            // Painter2D : 0° à droite, sens horaire (y vers le bas) → angle de la roue - 90°.
            float d0 = de - 90f, d1 = a - 90f;
            p.BeginPath();
            p.Arc(c, rext, d0, d1, ArcDirection.Clockwise);
            p.Arc(c, rint, d1, d0, ArcDirection.CounterClockwise);
            p.ClosePath();
            p.fillColor = fond;
            p.Fill();
            p.strokeColor = bord;
            p.lineWidth = trait;
            p.lineJoin = LineJoin.Round;
            p.Stroke();
        }
    }
}
