using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DeathlessLauncher
{
    // La fenêtre : au démarrage, lecture de launcher.json, vérification de la version en ligne, mise à jour automatique
    // si besoin, puis Jouer s'active. Sans réseau, on peut lancer la version installée. Même déroulé que le launcher de
    // Relic (MainForm), l'affichage passe par EcranLauncher.
    public partial class MainWindow : Window
    {
        private Updater updater;
        private CancellationTokenSource cancel;
        private bool ready;
        private bool verificationEnCours;           // Rafraîchir ou revérification automatique
        private readonly DispatcherTimer revérification;
        /// Revérification automatique discrète quand le jeu est à jour et la fenêtre ouverte.
        public static readonly TimeSpan IntervalleRevérification = TimeSpan.FromMinutes(10);

        private readonly Manette manette = new Manette();
        private readonly DispatcherTimer sondeManette;
        private double repetition;          // délai avant la prochaine répétition du stick gauche ou de la croix
        private int sensTenu;               // -1, 0 ou 1
        private DateTime derniereSonde = DateTime.UtcNow;
        private Point derniereSouris = new Point(double.NaN, double.NaN);

        public MainWindow()
        {
            InitializeComponent();

            // Environ 1280 × 720 de zone utile, sans dépasser l'écran ; taille minimale 960 × 540.
            Rect travail = SystemParameters.WorkArea;
            double largeur = Math.Min(EcranLauncher.LargeurBase, (travail.Width - 40));
            double hauteur = Math.Min(largeur * 9 / 16, travail.Height - 80);
            largeur = Math.Min(largeur, hauteur * 16 / 9);
            Ecran.Width = largeur;
            Ecran.Height = hauteur;
            MinWidth = Math.Min(960, travail.Width);
            MinHeight = Math.Min(540, travail.Height);
            ContentRendered += (s, e) =>
            {
                // Taille fixée au premier affichage, puis libre (fenêtre redimensionnable).
                SizeToContent = SizeToContent.Manual;
                Ecran.Width = double.NaN;
                Ecran.Height = double.NaN;
            };

            Ecran.JouerDemande += OnPlay;
            Ecran.ReessayerDemande += BeginCheck;
            Ecran.QuitterDemande += Close;
            Ecran.WikiDemande += OuvrirWiki;
            Ecran.RafraichirDemande += () => { var _ = RafraichirAsync(false); };
            revérification = new DispatcherTimer { Interval = IntervalleRevérification };
            revérification.Tick += (s, e) => { var _ = RafraichirAsync(true); };
            Ecran.Tache = (etat, valeur) => { Tache.ProgressState = etat; Tache.ProgressValue = valeur; };

            PreviewKeyDown += OnKey;
            PreviewMouseMove += (s, e) =>
            {
                Point p = PointToScreen(e.GetPosition(this));
                if (!double.IsNaN(derniereSouris.X) && (Math.Abs(p.X - derniereSouris.X) > 2 || Math.Abs(p.Y - derniereSouris.Y) > 2))
                    Ecran.Appareil(false);
                derniereSouris = p;
            };
            PreviewMouseDown += (s, e) => Ecran.Appareil(false);
            PreviewMouseWheel += (s, e) => Ecran.Appareil(false);
            Activated += (s, e) => manette.Oublier();

            sondeManette = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(33) };
            sondeManette.Tick += (s, e) => SonderManette();

            SourceInitialized += (s, e) => BarreDeTitreSombre();
            Loaded += (s, e) => { Ecran.DemarrerVideo(AppDomain.CurrentDomain.BaseDirectory); BeginCheck(); sondeManette.Start(); revérification.Start(); };
            StateChanged += (s, e) => Ecran.Video.Suspendre(WindowState == WindowState.Minimized);
            Closing += (s, e) => { if (cancel != null) cancel.Cancel(); sondeManette.Stop(); revérification.Stop(); Ecran.Video.Fermer(); };
        }

        // ---------- Mise à jour ----------

        private void BeginCheck()
        {
            ready = false;
            if (cancel != null) cancel.Cancel();
            string root = AppDomain.CurrentDomain.BaseDirectory;
            LauncherConfig config = LauncherConfig.Load(Path.Combine(root, "launcher.json"));
            Ecran.Wiki(AdresseWeb(config.WikiUrl));
            Updater courant = new Updater(config, root);
            courant.NettoyerAuDemarrage(); // restes d'une installation interrompue
            updater = courant;
            // Les rapports d'une vérification abandonnée (Réessayer) sont ignorés.
            courant.Rapport = a => Dispatcher.BeginInvoke((Action)(() => { if (updater == courant && !ready) Ecran.MontrerAvancement(a); }));
            cancel = new CancellationTokenSource();
            var _ = CheckAsync(updater, cancel.Token);
        }

        private async Task CheckAsync(Updater updater, CancellationToken token)
        {
            Manifest installed = updater.Installed;
            string installedName = updater.IsInstalled && installed != null ? installed.Affichage : null;
            // Notes de la dernière visite en attendant le serveur (et en mode hors ligne).
            string cached = updater.CachedChangelog;
            if (cached != null) Ecran.AfficherNotes(Changelog.Construire(cached, null), installedName, null);

            if (!updater.IsConfigured)
            {
                Ecran.MontrerNonConfigure(installedName);
                if (cached == null) Ecran.AfficherNotes(null, installedName, null);
                ready = installedName != null;
                return;
            }

            Ecran.MontrerRecherche();
            Task<string> changelogTask = updater.FetchChangelogAsync(token);
            Manifest remote;
            try
            {
                remote = await updater.FetchRemoteAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            catch (Exception e)
            {
                if (token.IsCancellationRequested) return;
                if (cached == null) Ecran.AfficherNotes(null, installedName, null);
                if (installedName != null)
                {
                    ready = true;
                    Ecran.MontrerHorsLigne(installedName, Explication(e));
                }
                else
                    Ecran.MontrerEchec("Serveur injoignable", "Aucune version installée, et le serveur ne répond pas (" + Explication(e) + "). Vérifiez la connexion, puis Réessayer.", null);
                return;
            }

            string changelog = null;
            try { changelog = await changelogTask; } catch { }
            if (token.IsCancellationRequested) return;
            bool upToDate = Updater.IsUpToDate(installed, remote, updater.IsInstalled);
            Ecran.AfficherNotes(Changelog.Construire(changelog ?? cached, remote), upToDate ? remote.Affichage : installedName, remote.Affichage);

            if (upToDate)
            {
                updater.NettoyerTemporaires();
                ready = true;
                Ecran.MontrerPret(remote.Affichage, false);
                return;
            }

            try
            {
                await updater.InstallAsync(remote, token);
                ready = true;
                Ecran.AfficherNotes(Changelog.Construire(changelog ?? cached, remote), remote.Affichage, remote.Affichage);
                Ecran.MontrerPret(remote.Affichage, true);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                if (token.IsCancellationRequested) return;
                // L'ancienne installation a pu être retirée si l'échec est survenu pendant l'extraction.
                string playable = updater.IsInstalled && updater.Installed != null ? updater.Installed.Affichage : null;
                ready = playable != null;
                Ecran.MontrerEchec("La mise à jour a échoué", Phrase(e) + " Vérifiez la connexion, puis Réessayer.", playable);
            }
        }

        /// Rafraîchir (F5, X, clic) ou revérification automatique (automatique = vrai, toutes les 10 minutes). Seulement
        /// quand le jeu est à jour et que rien n'est en cours. Manuel : la jauge montre le scan ; si une nouvelle version
        /// est publiée, la mise à jour se lance comme au démarrage ; sinon « Déjà à jour ». Automatique : rien ne bouge
        /// à l'écran, et une nouvelle version est seulement signalée (Jouer, pastille « Nouvelle », bouton en or).
        private async Task RafraichirAsync(bool automatique)
        {
            if (!ready || verificationEnCours || updater == null || !Ecran.RafraichirDisponible) return;
            Updater courant = updater;
            verificationEnCours = true;
            try
            {
                if (!automatique) Ecran.MontrerRafraichissement();
                VerificationEnLigne v = await courant.VerifierAsync(cancel?.Token ?? CancellationToken.None);
                if (courant != updater) return;
                Manifest installed = courant.Installed;
                string installedName = courant.IsInstalled && installed != null ? installed.Affichage : "?";
                switch (v.Etat)
                {
                    case EtatEnLigne.AJour:
                        Ecran.AfficherNotes(Changelog.Construire(v.Changelog ?? courant.CachedChangelog, v.Remote), v.Remote.Affichage, v.Remote.Affichage);
                        if (!automatique) Ecran.MontrerDejaAJour(v.Remote.Affichage);
                        break;
                    case EtatEnLigne.Nouvelle:
                        if (automatique)
                        {
                            Ecran.AfficherNotes(Changelog.Construire(v.Changelog ?? courant.CachedChangelog, v.Remote), installedName, v.Remote.Affichage);
                            Ecran.SignalerNouvelleVersion(installedName, v.Remote.Affichage);
                        }
                        else
                        {
                            verificationEnCours = false;
                            BeginCheck(); // mise à jour comme au démarrage
                        }
                        break;
                    case EtatEnLigne.Injoignable:
                        if (!automatique) Ecran.MontrerRafraichissementImpossible(installedName, Explication(v.Erreur));
                        break;
                }
            }
            catch (OperationCanceledException) { }
            finally { verificationEnCours = false; }
        }

        /// Message d'erreur lisible (HttpClient enveloppe souvent la vraie cause).
        public static string Explication(Exception e)
        {
            if (e is TaskCanceledException) return "le serveur n'a pas répondu à temps";
            Exception inner = e;
            while (inner.InnerException != null && (inner is System.Net.Http.HttpRequestException || inner is AggregateException))
                inner = inner.InnerException;
            string text = inner.Message.Trim();
            if (text.EndsWith(".")) text = text.Substring(0, text.Length - 1);
            // Le message est inséré dans une phrase : « (impossible de se connecter…) ».
            // (sauf si le premier mot est un nom de fichier ou d'hôte : « Deathless.exe est absent… »).
            string first = text.Split(' ')[0];
            if (text.Length > 1 && char.IsUpper(text[0]) && char.IsLower(text[1]) && first.IndexOf('.') < 0 && first.IndexOf('-') < 0)
                text = char.ToLowerInvariant(text[0]) + text.Substring(1);
            return text;
        }

        /// L'explication en phrase autonome : « Le fichier téléchargé est corrompu (…). »
        public static string Phrase(Exception e)
        {
            string text = Explication(e);
            return text.Length == 0 ? "" : char.ToUpperInvariant(text[0]) + text.Substring(1) + ".";
        }

        static bool AdresseWeb(string url) =>
            !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

        /// Wiki : la version joueur, dans le navigateur par défaut.
        private void OuvrirWiki()
        {
            string url = updater?.Config.WikiUrl;
            if (!AdresseWeb(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Ecran.MontrerErreurLancement("Impossible d'ouvrir le wiki (" + Explication(e) + ").");
            }
        }

        private void OnPlay()
        {
            if (!ready) return;
            try
            {
                updater.Launch();
                Close();
            }
            catch (Exception e)
            {
                Ecran.MontrerErreurLancement(Explication(e));
            }
        }

        // ---------- Clavier et manette ----------

        private void OnKey(object sender, KeyEventArgs e)
        {
            Ecran.Appareil(false);
            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    Ecran.Valider(); break;
                case Key.Up: Ecran.Deplacer(-1); break;
                case Key.Down: Ecran.Deplacer(1); break;
                case Key.Tab: Ecran.Deplacer(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1); break;
                case Key.PageUp: Ecran.DefilerPage(-1); break;
                case Key.PageDown: Ecran.DefilerPage(1); break;
                case Key.Home: Ecran.DefilerDebut(); break;
                case Key.End: Ecran.DefilerFin(); break;
                case Key.N: Ecran.BasculerNotes(); break;
                case Key.F5: { var _ = RafraichirAsync(false); } break;
                case Key.Escape:
                    if (!Ecran.NotesDepliees) return;
                    Ecran.Replier(true);
                    break;
                default: return;
            }
            e.Handled = true;
        }

        private void SonderManette()
        {
            DateTime now = DateTime.UtcNow;
            double dt = Math.Min(0.1, (now - derniereSonde).TotalSeconds);
            derniereSonde = now;
            manette.Lire();
            // Seulement quand le launcher est au premier plan : A dans une autre fenêtre ne lance rien.
            if (!IsActive || !manette.Disponible) return;
            if (manette.Activite) Ecran.Appareil(true);

            if ((manette.Appuis & Manette.A) != 0) { Ecran.Valider(); return; }
            if ((manette.Appuis & Manette.Y) != 0) Ecran.BasculerNotes();
            if ((manette.Appuis & Manette.X) != 0) { var _ = RafraichirAsync(false); }
            if ((manette.Appuis & Manette.B) != 0 && Ecran.NotesDepliees) Ecran.Replier(true);

            // Croix ou stick gauche : entrée précédente ou suivante, avec répétition si on garde la direction.
            int sens = 0;
            if ((manette.Boutons & Manette.DpadHaut) != 0 || manette.StickGaucheY > 0.5) sens = -1;
            else if ((manette.Boutons & Manette.DpadBas) != 0 || manette.StickGaucheY < -0.5) sens = 1;
            if (sens != 0 && sens != sensTenu) { Ecran.Deplacer(sens); repetition = 0.4; }
            else if (sens != 0) { repetition -= dt; if (repetition <= 0) { Ecran.Deplacer(sens); repetition = 0.15; } }
            sensTenu = sens;

            // Stick droit : défilement continu des notes ; LB et RB : page précédente ou suivante.
            if (Math.Abs(manette.StickDroitY) > 0) Ecran.Defiler(-manette.StickDroitY * 900 * dt);
            if ((manette.Appuis & Manette.LB) != 0) Ecran.DefilerPage(-1);
            if ((manette.Appuis & Manette.RB) != 0) Ecran.DefilerPage(1);
        }

        // ---------- Barre de titre sombre (Windows 10 20H1 et plus, couleur ardoise sur Windows 11) ----------

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private void BarreDeTitreSombre()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int oui = 1;
                if (DwmSetWindowAttribute(hwnd, 20, ref oui, 4) != 0) DwmSetWindowAttribute(hwnd, 19, ref oui, 4);
                int encre = 0x00241a16; // #161a24 en 0x00BBGGRR (DWMWA_CAPTION_COLOR, Windows 11 seulement)
                DwmSetWindowAttribute(hwnd, 35, ref encre, 4);
            }
            catch { /* dwmapi absent ou ancien : barre de titre par défaut */ }
        }
    }
}
