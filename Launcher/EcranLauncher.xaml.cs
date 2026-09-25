using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace DeathlessLauncher
{
    /// L'écran du launcher. Il ne connaît pas le réseau : la fenêtre (MainWindow) lui dit où en est la mise à jour par
    /// les méthodes Montrer…, et il renvoie les choix du joueur par JouerDemande, ReessayerDemande et QuitterDemande.
    /// Le mode capture (Capture.cs) appelle les mêmes méthodes, sans fenêtre.
    public partial class EcranLauncher : UserControl
    {
        public const double LargeurBase = 1280, HauteurBase = 720;
        const double LargeurUtile = 1180; // en dessous, on réduit pour que le volet et les notes ne se chevauchent pas

        public event Action JouerDemande, ReessayerDemande, QuitterDemande, WikiDemande;
        /// Avancement pour le bouton de la barre des tâches (état, valeur de 0 à 1).
        public Action<TaskbarItemProgressState, double> Tache = (s, v) => { };

        readonly List<EntreeMenu> entrees;
        int selection;
        bool manette;
        double cibleDefilement;
        bool defilementAbonne;
        bool notesDepliees;
        double hauteurRepliee = 120;

        static readonly ImageSource IconeEntree = Icone("keyboard_return.png");
        static readonly ImageSource IconeA = Icone("xbox_button_color_a.png");
        static readonly ImageSource IconeFleches = Icone("keyboard_arrows_vertical.png");
        static readonly ImageSource IconeCroix = Icone("xbox_dpad_vertical.png");
        static readonly ImageSource IconeMolette = Icone("mouse_scroll_vertical.png");
        static readonly ImageSource IconeStickDroit = Icone("xbox_stick_r_vertical.png");
        static readonly ImageSource IconeN = Icone("keyboard_n.png");
        static readonly ImageSource IconeY = Icone("xbox_button_color_y.png");

        public EcranLauncher()
        {
            InitializeComponent();
            Fond.Source = ChargerFond(AppDomain.CurrentDomain.BaseDirectory);
            entrees = new List<EntreeMenu> { EntreeJouer, EntreeReessayer, EntreeWiki, EntreeQuitter };
            EntreeJouer.Clic += () => JouerDemande?.Invoke();
            EntreeReessayer.Clic += () => ReessayerDemande?.Invoke();
            EntreeWiki.Clic += () => WikiDemande?.Invoke();
            EntreeQuitter.Clic += () => QuitterDemande?.Invoke();
            foreach (EntreeMenu e in entrees)
            {
                EntreeMenu entree = e;
                entree.Survol += () => { if (entree.Active) Selectionner(entrees.IndexOf(entree)); };
            }
            EntreeJouer.Active = false;
            Selectionner(0);
            Appareil(false);

            SizeChanged += (s, e) => Echelle();
            Defilement.ScrollChanged += (s, e) =>
            {
                if (!defilementAbonne) cibleDefilement = Defilement.VerticalOffset;
                Fondu();
            };
            Defilement.SizeChanged += (s, e) => Fondu();
            Defilement.PreviewMouseWheel += (s, e) =>
            {
                e.Handled = true;
                if (notesDepliees) Defiler(-e.Delta * 0.75);
            };
            // Clic : sur la carte repliée, la déplie ; déplié, un clic sur l'en-tête la replie.
            PanneauNotes.MouseLeftButtonUp += (s, e) =>
            {
                if (!notesDepliees || EnteteNotes.IsMouseOver) { BasculerNotes(); e.Handled = true; }
            };
            ListeNotes.SizeChanged += (s, e) => { if (!notesDepliees) Replier(false); };
            Replier(false);
            Unloaded += (s, e) => AbonnerDefilement(false);
            NotesEnChargement();
        }

        // ---------- Fond, icônes, échelle ----------

        /// Image de fond : avec une vidéo (fond.mp4), son image 0 (fond0.png) ; sinon fond.png (ou .jpg) posé à côté de
        /// l'exe s'il existe ; sinon celle compilée dans l'exe.
        public static ImageSource ChargerFond(string dossier)
        {
            foreach (string nom in NomsDuFond(dossier))
            {
                string chemin = System.IO.Path.Combine(dossier, nom);
                if (!File.Exists(chemin)) continue;
                try { return Bitmap(new Uri(chemin, UriKind.Absolute)); }
                catch { /* image illisible : on garde celle de l'exe */ }
            }
            return Bitmap(new Uri("pack://application:,,,/Ressources/fond.png"));
        }

        /// Fond animé : fond.mp4 du dossier, lu en boucle par-dessus l'image fixe (qui reste en cas d'échec). Pas appelé
        /// en mode capture : les captures gardent l'image fixe.
        public void DemarrerVideo(string dossier)
        {
            string chemin = FondVideo.Chercher(dossier);
            if (chemin == null) return;
            // L'image fixe (image 0 de la vidéo) n'est retirée qu'une fois l'image 0 de la vidéo rendue dessous.
            Video.PremiereImage += () => Fond.Visibility = Visibility.Hidden;
            Video.Echec += raison =>
            {
                Fond.Visibility = Visibility.Visible;
                System.Diagnostics.Trace.WriteLine("Fond animé abandonné, image fixe gardée : " + raison);
            };
            Video.Ouvrir(chemin);
        }

        static string[] NomsDuFond(string dossier) =>
            FondVideo.Chercher(dossier) != null ? new[] { "fond0.png", "fond.png", "fond.jpg", "fond.jpeg" } : new[] { "fond.png", "fond.jpg", "fond.jpeg" };

        /// D'où vient l'image de fond (pour les bancs).
        public static string SourceDuFond(string dossier)
        {
            foreach (string nom in NomsDuFond(dossier))
                if (File.Exists(System.IO.Path.Combine(dossier, nom))) return nom + " (à côté de l'exe)";
            return "image compilée dans l'exe";
        }

        static ImageSource Icone(string nom) => Bitmap(new Uri("pack://application:,,,/Invites/" + nom));

        static ImageSource Bitmap(Uri uri)
        {
            var b = new BitmapImage();
            b.BeginInit();
            b.UriSource = uri;
            b.CacheOption = BitmapCacheOption.OnLoad;
            b.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            if (!uri.Scheme.StartsWith("pack")) b.DecodePixelWidth = 2560;
            b.EndInit();
            b.Freeze();
            return b;
        }

        /// Comme le panneau du jeu : tout est dessiné à l'échelle 1280 × 720 puis agrandi selon la hauteur de la fenêtre.
        void Echelle()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            double s = Math.Max(0.5, Math.Min(ActualHeight / HauteurBase, ActualWidth / LargeurUtile));
            if (Calque.LayoutTransform is ScaleTransform t && Math.Abs(t.ScaleX - s) < 0.0001) return;
            Calque.LayoutTransform = new ScaleTransform(s, s);
        }

        /// Invites selon le dernier appareil utilisé, comme Deathless.UI.InputPrompt dans le jeu.
        public void Appareil(bool estManette)
        {
            manette = estManette;
            InviteValider.Source = estManette ? IconeA : IconeEntree;
            InviteChoisir.Source = estManette ? IconeCroix : IconeFleches;
            NomAppareil.Text = estManette ? "Manette Xbox" : "Clavier et souris";
            InviteNotes.Source = estManette ? IconeY : IconeN;
            InviteNotesCarte.Source = InviteNotes.Source;
            InviteDefiler.Source = estManette ? IconeStickDroit : IconeMolette;
            foreach (EntreeMenu e in entrees) e.Invite = InviteValider.Source;
        }

        public bool Manette => manette;

        // ---------- Menu ----------

        void Selectionner(int index)
        {
            selection = Math.Max(0, Math.Min(entrees.Count - 1, index));
            for (int i = 0; i < entrees.Count; i++) entrees[i].Selectionnee = i == selection;
        }

        /// Haut / bas : entrée précédente ou suivante (les entrées cachées ou inactives sont sautées).
        public void Deplacer(int sens)
        {
            int i = selection;
            for (int n = 0; n < entrees.Count; n++)
            {
                i = (i + sens + entrees.Count) % entrees.Count;
                if (entrees[i].Visibility == Visibility.Visible && entrees[i].Active)
                {
                    Selectionner(i);
                    return;
                }
            }
        }

        /// Entrée, Espace ou A : l'entrée sélectionnée.
        public void Valider()
        {
            EntreeMenu e = entrees[selection];
            if (!e.Active || e.Visibility != Visibility.Visible) return;
            if (e == EntreeJouer) JouerDemande?.Invoke();
            else if (e == EntreeReessayer) ReessayerDemande?.Invoke();
            else if (e == EntreeWiki) WikiDemande?.Invoke();
            else QuitterDemande?.Invoke();
        }

        void Jouer(bool active, string description)
        {
            bool devientActive = active && !EntreeJouer.Active;
            EntreeJouer.Active = active;
            EntreeJouer.Description = description;
            if (devientActive)
            {
                Selectionner(0);
                EntreeJouer.Eclat();
            }
            if (!active && entrees[selection] == EntreeJouer && EntreeReessayer.Visibility == Visibility.Visible) Selectionner(entrees.IndexOf(EntreeReessayer));
        }

        void Reessayer(bool visible)
        {
            EntreeReessayer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (visible && !EntreeJouer.Active) Selectionner(entrees.IndexOf(EntreeReessayer));
            if (!visible && entrees[selection] == EntreeReessayer) Selectionner(0);
        }

        void Avertir(string texte)
        {
            Message.Text = texte ?? "";
            Message.Visibility = string.IsNullOrEmpty(texte) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// Entrée Wiki : visible si une adresse est donnée (launcher.json, wikiUrl).
        public void Wiki(bool visible)
        {
            EntreeWiki.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible && entrees[selection] == EntreeWiki) Selectionner(0);
        }

        // ---------- Notes de version : repliées ou dépliées ----------

        public bool NotesDepliees => notesDepliees;

        /// N, Y ou un clic : déplie ou replie le panneau des notes.
        public void BasculerNotes()
        {
            if (notesDepliees) Replier(true); else Deplier(true);
        }

        /// Déplié : exactement le panneau complet (toute la hauteur entre le haut et la barre d'invites).
        public void Deplier(bool anime)
        {
            notesDepliees = true;
            Defilement.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            InviteDefilerGroupe.Visibility = Visibility.Visible;
            PanneauNotes.Cursor = null;
            EnteteNotes.Cursor = Cursors.Hand;
            RotationChevron.Angle = 180;
            double parent = ((FrameworkElement)PanneauNotes.Parent).ActualHeight;
            double cible = parent - PanneauNotes.Margin.Top - PanneauNotes.Margin.Bottom;
            Animer(cible, anime && parent > 0, () =>
            {
                // Une fois déplié, le panneau reprend sa mise en page souple (il suit la taille de la fenêtre).
                PanneauNotes.Height = double.NaN;
                PanneauNotes.VerticalAlignment = VerticalAlignment.Stretch;
                Fondu();
            });
        }

        /// Replié : une carte compacte en haut à droite (titre, version, étiquette, titre de la version).
        public void Replier(bool anime)
        {
            notesDepliees = false;
            Defilement.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            InviteDefilerGroupe.Visibility = Visibility.Collapsed;
            PanneauNotes.Cursor = Cursors.Hand;
            EnteteNotes.Cursor = null;
            RotationChevron.Angle = 0;
            AbonnerDefilement(false);
            cibleDefilement = 0;
            Defilement.ScrollToVerticalOffset(0);
            hauteurRepliee = HauteurRepliee();
            Animer(hauteurRepliee, anime, () => Fondu());
        }

        /// Hauteur de la carte : en-tête, puis la liste jusqu'au bas du résumé de la première version (son numéro, son
        /// étiquette et son titre ; les puces restent cachées).
        double HauteurRepliee()
        {
            double resume = 22;
            if (ListeNotes.Children.Count > 0)
            {
                // Entrée : en-tête (DockPanel), titre (TextBlock) éventuel, puis les puces (StackPanel).
                FrameworkElement fin = null;
                foreach (UIElement enfant in ListeNotes.Children)
                {
                    if (enfant is StackPanel) break;
                    fin = enfant as FrameworkElement;
                }
                if (fin != null && fin.ActualHeight > 0)
                    resume = fin.TranslatePoint(new Point(0, fin.ActualHeight), ListeNotes).Y + 2;
                else if (fin != null)
                    resume = 58;
            }
            Thickness p = PanneauNotes.Padding, b = PanneauNotes.BorderThickness, m = EnteteNotes.Margin;
            double entete = EnteteNotes.ActualHeight > 0 ? EnteteNotes.ActualHeight : 15;
            return Math.Ceiling(p.Top + p.Bottom + b.Top + b.Bottom + m.Top + m.Bottom + entete + resume);
        }

        /// Animation par la taille (hauteur), courte, sans fondu.
        void Animer(double cible, bool anime, Action fin)
        {
            double depart = PanneauNotes.ActualHeight;
            PanneauNotes.BeginAnimation(HeightProperty, null);
            PanneauNotes.VerticalAlignment = VerticalAlignment.Top;
            if (!anime || !JaugeVie.Animer || depart <= 0 || Math.Abs(depart - cible) < 1)
            {
                PanneauNotes.Height = cible;
                fin();
                return;
            }
            PanneauNotes.Height = depart;
            var animation = new System.Windows.Media.Animation.DoubleAnimation(depart, cible, TimeSpan.FromSeconds(0.2))
            {
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut },
                FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop,
            };
            animation.Completed += (s, e) => { PanneauNotes.Height = cible; fin(); };
            PanneauNotes.BeginAnimation(HeightProperty, animation);
        }

        /// Pour les captures : recalcule la carte repliée une fois la mise en page faite.
        public void ActualiserNotes()
        {
            if (notesDepliees) Deplier(false); else Replier(false);
        }

        // ---------- États de la mise à jour ----------

        public void MontrerRecherche()
        {
            Reessayer(false);
            Avertir(null);
            Jauge.Libelle = "Recherche de mise à jour";
            Jauge.Valeur = "";
            Jauge.Placer(0);
            Jauge.Indetermine = true;
            Jouer(false, "Recherche de mise à jour…");
            Tache(TaskbarItemProgressState.Indeterminate, 0);
        }

        public void MontrerAvancement(Avancement a)
        {
            switch (a.Phase)
            {
                case Phase.Telechargement: MontrerTelechargement(a); break;
                case Phase.Verification: MontrerVerification(a.Version); break;
                case Phase.Installation: MontrerInstallation(a.Version); break;
            }
        }

        public void MontrerTelechargement(Avancement a)
        {
            Jauge.Indetermine = false;
            Jauge.Libelle = a.Reprise ? "Téléchargement repris" : "Téléchargement";
            string valeur = a.Total > 0 ? Format.Mo(a.Fait) + " / " + Format.Mo(a.Total) + " Mo" : Format.Mo(a.Fait) + " Mo";
            string debit = Format.Debit(a.Debit);
            if (debit.Length > 0) valeur += "  ·  " + debit;
            Jauge.Valeur = valeur;
            double f = a.Total > 0 ? (double)a.Fait / a.Total : 0;
            Jauge.Fraction = f;
            string description = "Version " + a.Version;
            if (a.Total > 0 && a.Debit > 0) description += " · encore " + Format.Duree((a.Total - a.Fait) / a.Debit);
            Jouer(false, description);
            Tache(TaskbarItemProgressState.Normal, f);
        }

        public void MontrerVerification(string version)
        {
            Jauge.Libelle = "Vérification du fichier";
            Jauge.Valeur = "SHA-256";
            Jauge.Indetermine = true;
            Jouer(false, "Vérification de la version " + version + "…");
            Tache(TaskbarItemProgressState.Indeterminate, 0);
        }

        public void MontrerInstallation(string version)
        {
            Jauge.Libelle = "Installation";
            Jauge.Valeur = "";
            Jauge.Indetermine = true;
            Jouer(false, "Installation de la version " + version + "…");
            Tache(TaskbarItemProgressState.Indeterminate, 0);
        }

        public void MontrerPret(string version, bool vientDEtreInstallee)
        {
            Reessayer(false);
            Avertir(null);
            Jauge.Indetermine = false;
            Jauge.Fraction = 1;
            Jauge.Libelle = vientDEtreInstallee ? "Installation terminée" : "À jour";
            Jauge.Valeur = "Version " + version;
            Jouer(true, "Version " + version + (vientDEtreInstallee ? " installée, prête" : " à jour"));
            Tache(TaskbarItemProgressState.None, 0);
        }

        public void MontrerHorsLigne(string versionInstallee, string erreur)
        {
            Reessayer(true);
            Jauge.Indetermine = false;
            Jauge.Placer(1);
            Jauge.Libelle = "Hors ligne";
            Jauge.Valeur = "Version " + versionInstallee + " installée";
            Avertir("Impossible de vérifier les mises à jour (" + erreur + "). Vous pouvez lancer la version installée.");
            Jouer(true, "Serveur injoignable · version " + versionInstallee + " installée");
            Tache(TaskbarItemProgressState.None, 0);
        }

        /// Échec sans retour possible au jeu à jour : versionJouable est la version installée (null s'il n'y en a pas).
        public void MontrerEchec(string titre, string detail, string versionJouable)
        {
            Reessayer(true);
            Jauge.Indetermine = false;
            Jauge.Placer(versionJouable != null ? 1 : 0);
            Jauge.Libelle = titre;
            Jauge.Valeur = versionJouable != null ? "Version " + versionJouable + " installée" : "";
            Avertir(detail);
            Jouer(versionJouable != null, versionJouable != null ? "Lancer la version " + versionJouable + " installée" : "Aucune version installée");
            Tache(TaskbarItemProgressState.Error, 1);
        }

        public void MontrerNonConfigure(string versionJouable)
        {
            Reessayer(false);
            Jauge.Indetermine = false;
            Jauge.Placer(versionJouable != null ? 1 : 0);
            Jauge.Libelle = "Adresse du serveur non renseignée";
            Jauge.Valeur = versionJouable != null ? "Version " + versionJouable + " installée" : "";
            Avertir("Le fichier launcher.json, à côté de DeathlessLauncher.exe, doit contenir l'adresse (baseUrl) du dossier où sont publiés version.json et le jeu.");
            Jouer(versionJouable != null, versionJouable != null ? "Version " + versionJouable + " installée" : "Aucune version installée");
            Tache(TaskbarItemProgressState.None, 0);
        }

        public void MontrerErreurLancement(string erreur)
        {
            Avertir(erreur.StartsWith("Impossible") ? erreur : "Impossible de lancer le jeu : " + erreur);
            Tache(TaskbarItemProgressState.Error, 1);
        }

        // ---------- Notes de version ----------

        public void NotesEnChargement()
        {
            ListeNotes.Children.Clear();
            ListeNotes.Children.Add(new TextBlock { Text = "Chargement des notes de version…", Foreground = Teintes.Pinceau(Teintes.TexteSecondaire), FontSize = 16 });
        }

        /// Affiche les versions (déjà triées de la plus récente à la plus ancienne). La version installée porte la
        /// pastille « Installée », la version publiée pas encore installée la pastille « Nouvelle ».
        public void AfficherNotes(IList<EntreeChangelog> versions, string installee, string enLigne)
        {
            ListeNotes.Children.Clear();
            if (versions == null || versions.Count == 0)
            {
                ListeNotes.Children.Add(new TextBlock { Text = "Aucune note de version pour l'instant.", Foreground = Teintes.Pinceau(Teintes.TexteSecondaire), FontSize = 16 });
                return;
            }
            for (int i = 0; i < versions.Count; i++)
            {
                EntreeChangelog v = versions[i];
                if (i > 0) ListeNotes.Children.Add(new Border { Height = 1, Background = Teintes.Pinceau(Teintes.Ligne), Margin = new Thickness(0, 16, 0, 16) });

                var entete = new DockPanel { LastChildFill = false };
                var titre = new TextBlock { Text = "Version " + v.Version, FontSize = 22, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                entete.Children.Add(titre);
                if (installee != null && Changelog.MemeVersion(v.Version, installee))
                    entete.Children.Add(Pastille("Installée", false));
                else if (enLigne != null && Changelog.MemeVersion(v.Version, enLigne))
                    entete.Children.Add(Pastille("Nouvelle", true));
                if (v.DateAffichee.Length > 0)
                {
                    var date = new TextBlock { Text = v.DateAffichee, FontSize = 14, Foreground = Teintes.Pinceau(Teintes.TexteSecondaire), VerticalAlignment = VerticalAlignment.Center };
                    DockPanel.SetDock(date, Dock.Right);
                    entete.Children.Add(date);
                }
                ListeNotes.Children.Add(entete);

                if (v.Titre.Length > 0)
                    ListeNotes.Children.Add(new TextBlock { Text = v.Titre, FontSize = 16, FontWeight = FontWeights.Medium, Foreground = Teintes.Pinceau(Teintes.Or), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

                var puces = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                foreach (string note in v.Notes)
                {
                    var ligne = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
                    var puce = new Ellipse { Width = 6, Height = 6, Fill = Teintes.Pinceau(Teintes.Or), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(2, 8, 12, 0) };
                    ligne.Children.Add(puce);
                    ligne.Children.Add(new TextBlock { Text = note, FontSize = 15, TextWrapping = TextWrapping.Wrap, LineHeight = 21, Foreground = Teintes.Pinceau(Teintes.Texte) });
                    puces.Children.Add(ligne);
                }
                if (v.Notes.Count == 0)
                    puces.Children.Add(new TextBlock { Text = "Pas de note pour cette version.", FontSize = 15, Foreground = Teintes.Pinceau(Teintes.TexteSecondaire) });
                ListeNotes.Children.Add(puces);
            }
            Defilement.ScrollToVerticalOffset(0);
            cibleDefilement = 0;
        }

        /// Pastille dl-pill : « Installée » en or au trait, « Nouvelle » en or plein (score-valeur--meilleur).
        static Border Pastille(string texte, bool pleine)
        {
            return new Border
            {
                Height = 24,
                Padding = new Thickness(10, 0, 10, 1),
                Margin = new Thickness(10, 0, 0, 0),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = Teintes.Pinceau(Teintes.Or),
                Background = Teintes.Pinceau(pleine ? Teintes.Meilleur : Teintes.Creux, pleine ? 1 : 0.78),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = texte,
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = Teintes.Pinceau(pleine ? Colors.White : Teintes.Or),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
        }

        // ---------- Défilement doux des notes ----------

        public void Defiler(double delta)
        {
            if (!notesDepliees) return;
            cibleDefilement = Math.Max(0, Math.Min(Defilement.ScrollableHeight, cibleDefilement + delta));
            if (!JaugeVie.Animer) { Defilement.ScrollToVerticalOffset(cibleDefilement); return; }
            AbonnerDefilement(true);
        }

        public void DefilerPage(int sens) => Defiler(sens * Math.Max(60, Defilement.ViewportHeight * 0.85));
        public void DefilerDebut() => Defiler(-Defilement.ScrollableHeight - 1);
        public void DefilerFin() => Defiler(Defilement.ScrollableHeight + 1);

        void AbonnerDefilement(bool oui)
        {
            if (oui == defilementAbonne) return;
            defilementAbonne = oui;
            if (oui) CompositionTarget.Rendering += ImageDefilement;
            else CompositionTarget.Rendering -= ImageDefilement;
        }

        void ImageDefilement(object sender, EventArgs e)
        {
            double actuel = Defilement.VerticalOffset;
            double ecart = cibleDefilement - actuel;
            if (Math.Abs(ecart) < 0.5)
            {
                Defilement.ScrollToVerticalOffset(cibleDefilement);
                AbonnerDefilement(false);
                return;
            }
            Defilement.ScrollToVerticalOffset(actuel + ecart * 0.22);
        }

        /// Fondu en haut et en bas de la liste quand il reste du texte caché de ce côté.
        public void ActualiserFondu() => Fondu();

        void Fondu()
        {
            // Sur le contenu seulement : la barre de défilement ne s'estompe pas.
            if (!(Defilement.Template?.FindName("PART_ScrollContentPresenter", Defilement) is UIElement contenu)) return;
            double h = Defilement.ActualHeight;
            // Replié : coupe franche sous le résumé, pas de fondu.
            if (!notesDepliees || h <= 0 || Defilement.ScrollableHeight <= 0.5) { contenu.OpacityMask = null; return; }
            double f = Math.Min(0.2, 28 / h);
            bool haut = Defilement.VerticalOffset > 0.5;
            bool bas = Defilement.VerticalOffset < Defilement.ScrollableHeight - 0.5;
            var masque = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            masque.GradientStops.Add(new GradientStop(haut ? Colors.Transparent : Colors.Black, 0));
            masque.GradientStops.Add(new GradientStop(Colors.Black, f));
            masque.GradientStops.Add(new GradientStop(Colors.Black, 1 - f));
            masque.GradientStops.Add(new GradientStop(bas ? Colors.Transparent : Colors.Black, 1));
            masque.Freeze();
            contenu.OpacityMask = masque;
        }
    }
}
