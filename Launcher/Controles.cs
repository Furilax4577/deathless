using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace DeathlessLauncher
{
    /// Couleurs du thème du jeu (Assets/UI/Theme/Deathless.uss), pour le code. Les mêmes existent en ressources XAML
    /// (Theme.xaml).
    public static class Teintes
    {
        public static readonly Color Encre = Hex("#161a24");
        public static readonly Color Creux = Hex("#10131b");
        public static readonly Color PanneauActif = Hex("#2b3348");
        public static readonly Color Bord = Hex("#3d4660");
        public static readonly Color BordFort = Hex("#56607c");
        public static readonly Color Ligne = Hex("#333b52");
        public static readonly Color Texte = Hex("#f4ecd8");
        public static readonly Color TexteSecondaire = Hex("#c3bca9");
        public static readonly Color TexteInactif = Hex("#8a8578");
        public static readonly Color Or = Hex("#d9b264");
        public static readonly Color Vie = Hex("#e0483e");
        public static readonly Color Alerte = Hex("#ff9a8a");
        public static readonly Color Meilleur = Hex("#3a3420");

        public static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
        public static SolidColorBrush Pinceau(Color c, double alpha = 1)
        {
            var b = new SolidColorBrush(Color.FromArgb((byte)Math.Round(alpha * c.A), c.R, c.G, c.B));
            b.Freeze();
            return b;
        }
    }

    /// Texte à espacement de lettres (USS letter-spacing, que WPF n'a pas) : titre DEATHLESS, libellés de section.
    public sealed class TexteEspace : FrameworkElement
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(TexteEspace),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty EspacementProperty = DependencyProperty.Register(nameof(Espacement), typeof(double), typeof(TexteEspace),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(TexteEspace),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TexteEspace),
            new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(TexteEspace),
            new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(TexteEspace),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public double Espacement { get => (double)GetValue(EspacementProperty); set => SetValue(EspacementProperty, value); }
        public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
        public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
        public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
        public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

        FormattedText Lettre(string c)
        {
            var typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal);
            double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            return new FormattedText(c, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, FontSize, Foreground, dip);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            string text = Text ?? "";
            if (text.Length == 0) return new Size(0, 0);
            double w = 0, h = 0;
            foreach (char c in text)
            {
                FormattedText f = Lettre(c.ToString());
                w += f.WidthIncludingTrailingWhitespace;
                h = Math.Max(h, f.Height);
            }
            return new Size(w + Espacement * (text.Length - 1), h);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double x = 0;
            foreach (char c in Text ?? "")
            {
                FormattedText f = Lettre(c.ToString());
                dc.DrawText(f, new Point(x, 0));
                x += f.WidthIncludingTrailingWhitespace + Espacement;
            }
        }
    }

    /// Barre de téléchargement à la forme de la barre de vie du HUD (Deathless.UI.Gauge, classe dl-gauge--life) :
    /// libellé à gauche, valeur à droite, piste arrondie au creux sombre bordée d'un trait, remplissage rouge arrondi.
    /// Le remplissage suit la valeur en douceur (comme la transition de 0,15 s de l'USS). En mode indéterminé
    /// (recherche, vérification, installation), le remplissage est masqué et une rangée de petites gemmes or, dans le
    /// langage visuel du jeu (low poly, bords francs, pas de dégradé ni d'alpha), grossissent puis rétrécissent en vague
    /// qui avance de gauche à droite : elles apparaissent et disparaissent par la taille.
    public sealed class JaugeVie : StackPanel
    {
        /// Faux pour les captures : valeurs appliquées tout de suite, vague figée.
        public static bool Animer = true;

        const double Hauteur = 18;       // piste de la jauge du HUD (21 px à 1080p), un peu grandie pour le launcher
        readonly TextBlock libelle = new TextBlock();
        readonly TextBlock valeur = new TextBlock();
        readonly Grid interieur = new Grid { ClipToBounds = true };
        readonly Border remplissage;
        readonly Canvas rangee = new Canvas { Visibility = Visibility.Collapsed };
        readonly System.Collections.Generic.List<Gemme> gemmes = new System.Collections.Generic.List<Gemme>();
        double cible, affiche, phase = 0.35;

        // Vague de gemmes : une période de 1,4 s ; chaque gemme grossit puis rétrécit pendant 20 % de la période, avec un
        // retard proportionnel à sa position (la vague avance de gauche à droite et se raccorde d'elle-même).
        const double Periode = 1.4, Impulsion = 0.2, Retard = 0.75, Pas = 22;
        const double GemmeHauteur = 12, GemmeLargeur = GemmeHauteur * 33 / 42; // proportions de hud-nyx__gemme
        bool indetermine;
        TimeSpan dernier = TimeSpan.Zero;
        bool abonne;

        public JaugeVie()
        {
            var entete = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 5) };
            libelle.FontSize = 14;
            libelle.Foreground = Teintes.Pinceau(Teintes.TexteSecondaire);
            valeur.FontSize = 14;
            valeur.Foreground = Teintes.Pinceau(Teintes.Texte);
            DockPanel.SetDock(libelle, Dock.Left);
            DockPanel.SetDock(valeur, Dock.Right);
            entete.Children.Add(libelle);
            entete.Children.Add(valeur);
            Children.Add(entete);

            remplissage = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius((Hauteur - 2) / 2),
                Background = Teintes.Pinceau(Teintes.Vie),
                Width = 0,
            };
            interieur.Children.Add(remplissage);
            interieur.Children.Add(rangee);
            interieur.SizeChanged += (s, e) => { Decouper(); Disposer(); Appliquer(); };
            var piste = new Border
            {
                Height = Hauteur,
                CornerRadius = new CornerRadius(Hauteur / 2),
                BorderThickness = new Thickness(1),
                BorderBrush = Teintes.Pinceau(Teintes.Bord),
                Background = Teintes.Pinceau(Teintes.Creux),
                Child = interieur,
            };
            Children.Add(piste);

            Loaded += (s, e) => Abonner(true);
            Unloaded += (s, e) => Abonner(false);
        }

        public string Libelle { get => libelle.Text; set => libelle.Text = value ?? ""; }
        public string Valeur { get => valeur.Text; set => valeur.Text = value ?? ""; }

        /// Remplissage visé, de 0 à 1.
        public double Fraction
        {
            get => cible;
            set
            {
                cible = Math.Max(0, Math.Min(1, double.IsNaN(value) ? 0 : value));
                if (!Animer) affiche = cible;
                Appliquer();
            }
        }

        /// Place immédiatement le remplissage (sans l'animation de montée), par exemple au retour à zéro.
        public void Placer(double fraction)
        {
            Fraction = fraction;
            affiche = cible;
            Appliquer();
        }

        public bool Indetermine
        {
            get => indetermine;
            set
            {
                indetermine = value;
                rangee.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                remplissage.Visibility = value ? Visibility.Hidden : Visibility.Visible;
                Appliquer();
            }
        }

        void Abonner(bool oui)
        {
            if (oui == abonne || !Animer) return;
            abonne = oui;
            if (oui) CompositionTarget.Rendering += Image;
            else CompositionTarget.Rendering -= Image;
        }

        void Image(object sender, EventArgs e)
        {
            TimeSpan t = ((RenderingEventArgs)e).RenderingTime;
            double dt = dernier == TimeSpan.Zero ? 0 : Math.Min(0.1, (t - dernier).TotalSeconds);
            dernier = t;
            if (dt <= 0) return;
            bool change = false;
            if (Math.Abs(affiche - cible) > 0.0005)
            {
                affiche += (cible - affiche) * (1 - Math.Exp(-dt / 0.12));
                change = true;
            }
            else if (affiche != cible) { affiche = cible; change = true; }
            if (indetermine)
            {
                phase = (phase + dt / Periode) % 1.0;
                change = true;
            }
            if (change) Appliquer();
        }

        void Decouper()
        {
            double r = (Hauteur - 2) / 2;
            interieur.Clip = new RectangleGeometry(new Rect(0, 0, interieur.ActualWidth, interieur.ActualHeight), r, r);
        }

        void Appliquer()
        {
            double w = interieur.ActualWidth;
            if (w <= 0) return;
            remplissage.Width = w * affiche;
            if (!indetermine) return;
            for (int i = 0; i < gemmes.Count; i++)
            {
                double x = gemmes.Count > 1 ? (double)i / (gemmes.Count - 1) : 0;
                double u = phase - x * Retard;
                u -= Math.Floor(u);
                // Taille : 0 hors de l'impulsion, puis une bosse lisse (sinus) ; à 0, la gemme n'est pas dessinée.
                double t = u < Impulsion ? Math.Sin(Math.PI * u / Impulsion) : 0;
                gemmes[i].Taille(t);
            }
        }

        /// Une gemme toutes les 22 px, centrées dans la piste.
        void Disposer()
        {
            double w = interieur.ActualWidth, h = interieur.ActualHeight;
            if (w <= 0) return;
            int n = Math.Max(3, (int)Math.Floor((w - 12) / Pas) + 1);
            while (gemmes.Count < n) { var g = new Gemme(GemmeLargeur, GemmeHauteur); gemmes.Add(g); rangee.Children.Add(g); }
            while (gemmes.Count > n) { rangee.Children.Remove(gemmes[gemmes.Count - 1]); gemmes.RemoveAt(gemmes.Count - 1); }
            double debut = (w - (n - 1) * Pas) / 2;
            for (int i = 0; i < n; i++)
            {
                Canvas.SetLeft(gemmes[i], Math.Round(debut + i * Pas - GemmeLargeur / 2));
                Canvas.SetTop(gemmes[i], Math.Round((h - GemmeHauteur) / 2));
            }
        }
    }

    /// Petite gemme or de la jauge, taillée comme GemmeNyxessa du HUD (losange et facette claire, couleurs pleines).
    /// Elle grossit et rétrécit par sa taille (échelle autour de son centre), jamais par l'opacité.
    sealed class Gemme : Canvas
    {
        static readonly Brush Corps = Teintes.Pinceau(Teintes.Or);
        static readonly Brush Facette = Teintes.Pinceau(Teintes.Hex("#f2d79a"));
        readonly ScaleTransform echelle = new ScaleTransform(0, 0);

        public Gemme(double w, double h)
        {
            Width = w;
            Height = h;
            IsHitTestVisible = false;
            RenderTransformOrigin = new Point(0.5, 0.5);
            RenderTransform = echelle;
            Point haut = new Point(w / 2, 0), droite = new Point(w, h / 2), bas = new Point(w / 2, h), gauche = new Point(0, h / 2);
            Children.Add(new Polygon { Points = new PointCollection { haut, droite, bas, gauche }, Fill = Corps });
            Children.Add(new Polygon { Points = new PointCollection { haut, droite, new Point(w / 2, h / 2 + h * 0.07) }, Fill = Facette });
            Visibility = Visibility.Hidden;
        }

        public void Taille(double t)
        {
            echelle.ScaleX = t;
            echelle.ScaleY = t;
            Visibility = t > 0.1 ? Visibility.Visible : Visibility.Hidden;
        }
    }

    /// Entrée du menu, comme dl-menu-item du jeu : invisible au repos, fond éclairci au survol, fond actif et bordure
    /// or une fois sélectionnée, invite Valider à droite. Une entrée inactive peut rester sélectionnée (Jouer pendant le
    /// téléchargement) : bordure or estompée, textes grisés, pas d'invite.
    public sealed class EntreeMenu : Border
    {
        readonly TextBlock libelle = new TextBlock { FontSize = 24, FontWeight = FontWeights.SemiBold };
        readonly TextBlock description = new TextBlock { FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis };
        readonly Image invite = new Image { Width = 32, Height = 32, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        bool selectionnee, active = true, survol;

        public event Action Clic;
        public event Action Survol;

        public EntreeMenu()
        {
            MinHeight = 58;
            Margin = new Thickness(0, 0, 0, 8);
            Padding = new Thickness(18, 6, 12, 7);
            CornerRadius = new CornerRadius(10);
            BorderThickness = new Thickness(2);
            Background = Brushes.Transparent;
            Cursor = System.Windows.Input.Cursors.Hand;
            SnapsToDevicePixels = true;

            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textes.Children.Add(libelle);
            textes.Children.Add(description);
            var dock = new DockPanel();
            DockPanel.SetDock(invite, Dock.Right);
            dock.Children.Add(invite);
            dock.Children.Add(textes);
            Child = dock;

            MouseEnter += (s, e) => { survol = true; Actualiser(); Survol?.Invoke(); };
            MouseLeave += (s, e) => { survol = false; Actualiser(); };
            MouseLeftButtonUp += (s, e) => { if (active) Clic?.Invoke(); };
            Actualiser();
        }

        public string Libelle { get => libelle.Text; set => libelle.Text = value; }
        public string Description
        {
            get => description.Text;
            set { description.Text = value ?? ""; description.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible; }
        }
        public ImageSource Invite { get => invite.Source; set => invite.Source = value; }
        public bool Selectionnee { get => selectionnee; set { selectionnee = value; Actualiser(); } }
        public bool Active { get => active; set { active = value; Cursor = value ? System.Windows.Input.Cursors.Hand : null; Actualiser(); } }

        void Actualiser()
        {
            if (selectionnee && active)
            {
                Background = Teintes.Pinceau(Teintes.PanneauActif);
                BorderBrush = Teintes.Pinceau(Teintes.Or);
            }
            else if (selectionnee)
            {
                Background = Teintes.Pinceau(Teintes.PanneauActif, 0.6);
                BorderBrush = Teintes.Pinceau(Teintes.Or, 0.4);
            }
            else if (survol && active)
            {
                Background = Teintes.Pinceau(Teintes.PanneauActif, 0.6);
                BorderBrush = Teintes.Pinceau(Teintes.Bord);
            }
            else
            {
                Background = Brushes.Transparent;
                BorderBrush = Brushes.Transparent;
            }
            libelle.Foreground = Teintes.Pinceau(!active ? Teintes.TexteInactif : selectionnee ? Colors.White : Teintes.Texte);
            description.Foreground = Teintes.Pinceau(active ? Teintes.TexteSecondaire : Teintes.TexteInactif);
            invite.Visibility = selectionnee && active ? Visibility.Visible : Visibility.Hidden;
        }

        /// Éclat or bref quand Jouer devient disponible.
        public void Eclat()
        {
            if (!JaugeVie.Animer) return;
            var ombre = new DropShadowEffect { Color = Teintes.Or, ShadowDepth = 0, BlurRadius = 26, Opacity = 0 };
            Effect = ombre;
            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.18)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.1)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));
            anim.Completed += (s, e) => Effect = null;
            ombre.BeginAnimation(DropShadowEffect.OpacityProperty, anim);
        }
    }
}
