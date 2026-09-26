using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Petites formes vectorielles du HUD, dessinées par Painter2D (nettes à toutes les tailles d'interface).
    /// La taille vient de l'USS (width, height) ; les couleurs sont celles des maquettes.
    public abstract class FormeHud : VisualElement
    {
        protected FormeHud()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += ctx => Dessiner(ctx.painter2D, contentRect);
        }

        protected abstract void Dessiner(Painter2D p, Rect r);

        protected static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        protected static void Polygone(Painter2D p, Color couleur, params Vector2[] points)
        {
            p.fillColor = couleur;
            p.BeginPath();
            p.MoveTo(points[0]);
            for (var i = 1; i < points.Length; i++) p.LineTo(points[i]);
            p.ClosePath();
            p.Fill();
        }
    }

    /// Gemme de Nyxessa (losange vert, facette claire).
    [UxmlElement]
    public partial class GemmeNyxessa : FormeHud
    {
        protected override void Dessiner(Painter2D p, Rect r)
        {
            var c = r.center;
            Vector2 haut = new Vector2(c.x, r.yMin), droite = new Vector2(r.xMax, c.y), bas = new Vector2(c.x, r.yMax), gauche = new Vector2(r.xMin, c.y);
            Polygone(p, Hex("#3fae5a"), haut, droite, bas, gauche);
            Polygone(p, Hex("#9fe870"), haut, droite, new Vector2(c.x, c.y + r.height * 0.07f));
        }
    }

    /// Icône du cycle : soleil à moitié plein le jour, croissant la nuit. Propriété « nuit ».
    [UxmlElement]
    public partial class IconeJourNuit : FormeHud
    {
        bool m_Nuit;

        [UxmlAttribute("nuit")]
        public bool nuit
        {
            get => m_Nuit;
            set { m_Nuit = value; MarkDirtyRepaint(); }
        }

        protected override void Dessiner(Painter2D p, Rect r)
        {
            var c = r.center;
            var rayon = Mathf.Min(r.width, r.height) * 0.45f;
            if (!m_Nuit)
            {
                var or = Hex("#f2c14e");
                p.strokeColor = or;
                p.lineWidth = Mathf.Max(1.5f, rayon * 0.22f);
                p.BeginPath();
                p.Arc(c, rayon - p.lineWidth * 0.5f, 0f, 360f);
                p.Stroke();
                p.fillColor = or;
                p.BeginPath();
                p.MoveTo(c);
                p.Arc(c, rayon, -90f, 0f);
                p.ClosePath();
                p.Fill();
            }
            else
            {
                // Croissant : disque clair moins un disque décalé (dessiné avec la couleur de la pastille).
                p.fillColor = Hex("#c9d4f0");
                p.BeginPath();
                p.Arc(c, rayon, 0f, 360f);
                p.Fill();
                p.fillColor = Hex("#1a1e28");
                p.BeginPath();
                p.Arc(c + new Vector2(rayon * 0.55f, -rayon * 0.35f), rayon * 0.85f, 0f, 360f);
                p.Fill();
            }
        }
    }

    /// Pièce d'or (caisse commune).
    [UxmlElement]
    public partial class PieceOr : FormeHud
    {
        protected override void Dessiner(Painter2D p, Rect r)
        {
            var c = r.center;
            var rayon = Mathf.Min(r.width, r.height) * 0.5f;
            p.fillColor = Hex("#e8c872");
            p.BeginPath();
            p.Arc(c, rayon, 0f, 360f);
            p.Fill();
            p.strokeColor = Hex("#b8903a");
            p.lineWidth = rayon * 0.18f;
            p.BeginPath();
            p.Arc(c, rayon * 0.6f, 0f, 360f);
            p.Stroke();
        }
    }

    /// Réticule de visée (point et cercle blancs).
    [UxmlElement]
    public partial class Reticule : FormeHud
    {
        protected override void Dessiner(Painter2D p, Rect r)
        {
            var c = r.center;
            var rayon = Mathf.Min(r.width, r.height) * 0.5f;
            p.fillColor = Color.white;
            p.BeginPath();
            p.Arc(c, rayon * 0.25f, 0f, 360f);
            p.Fill();
            p.strokeColor = new Color(1f, 1f, 1f, 0.7f);
            p.lineWidth = rayon * 0.17f;
            p.BeginPath();
            p.Arc(c, rayon * 0.75f, 0f, 360f);
            p.Stroke();
        }
    }

    /// Couronne or : « meilleur » d'une catégorie de l'écran de score.
    [UxmlElement]
    public partial class Couronne : FormeHud
    {
        protected override void Dessiner(Painter2D p, Rect r)
        {
            float w = r.width, h = r.height, x = r.xMin, y = r.yMin;
            Polygone(p, Hex("#d9b264"),
                new Vector2(x, y + h), new Vector2(x + w * 0.12f, y + h * 0.17f), new Vector2(x + w * 0.38f, y + h * 0.58f),
                new Vector2(x + w * 0.5f, y), new Vector2(x + w * 0.62f, y + h * 0.58f), new Vector2(x + w * 0.88f, y + h * 0.17f),
                new Vector2(x + w, y + h));
        }
    }

    /// Anneau de jauge de classe (mana ou rage) dessiné autour du portrait du joueur (maquette B, 26/09/2026 ;
    /// porté du bac à sable `sandbox-ui`, `Dev/FormesMaquetteHud.cs`). Départ en haut (12 h), balayage horaire
    /// jusqu'à `value` (0 à 1), comme IconeJourNuit. `couleur` : hex ARGB/RGB (« #4a8fe0 » mana, « #ff8c1a » rage).
    [UxmlElement]
    public partial class AnneauJauge : FormeHud
    {
        float m_Value = 1f;
        string m_Couleur = "#4a8fe0";
        float m_Epaisseur = 6f;

        [UxmlAttribute("value")]
        public float value
        {
            get => m_Value;
            set { m_Value = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("couleur")]
        public string couleur
        {
            get => m_Couleur;
            set { m_Couleur = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute("epaisseur")]
        public float epaisseur
        {
            get => m_Epaisseur;
            set { m_Epaisseur = value; MarkDirtyRepaint(); }
        }

        protected override void Dessiner(Painter2D p, Rect r)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            var centre = r.center;
            var rayon = Mathf.Min(r.width, r.height) * 0.5f - m_Epaisseur * 0.5f;
            p.lineWidth = m_Epaisseur;
            p.lineCap = LineCap.Round;

            // Piste vide (fond de l'anneau).
            p.strokeColor = new Color(1f, 1f, 1f, 0.14f);
            p.BeginPath();
            p.Arc(centre, rayon, -90f, 270f);
            p.Stroke();

            // Portion remplie, du haut vers la droite (sens horaire).
            var v = Mathf.Clamp01(m_Value);
            if (v > 0.003f)
            {
                p.strokeColor = Hex(m_Couleur);
                p.BeginPath();
                p.Arc(centre, rayon, -90f, -90f + v * 360f);
                p.Stroke();
            }
        }
    }

    /// Flèche pleine (triangle) pointant dans la direction « angle », en degrés à l'écran :
    /// 0 = droite, 90 = bas, 180 = gauche, 270 = haut.
    [UxmlElement]
    public partial class FlecheHud : FormeHud
    {
        float m_Angle;

        [UxmlAttribute("angle")]
        public float angle
        {
            get => m_Angle;
            set { m_Angle = value; MarkDirtyRepaint(); }
        }

        protected override void Dessiner(Painter2D p, Rect r)
        {
            var c = r.center;
            var s = Mathf.Min(r.width, r.height) * 0.5f;
            var a = m_Angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var perp = new Vector2(-dir.y, dir.x);
            Polygone(p, Color.white, c + dir * s, c - dir * s * 0.6f + perp * s * 0.85f, c - dir * s * 0.6f - perp * s * 0.85f);
        }
    }
}
