using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeathlessLauncher
{
    /// Fond animé : fond.mp4 (à côté de l'exe) lu en boucle, sans saccade au raccord.
    ///
    /// Un MediaElement en boucle simple (retour à zéro sur MediaEnded) marque un à-coup : le lecteur s'arrête, cherche
    /// l'image 0, puis repart. Ici, deux lecteurs (MediaPlayer, affichés chacun par une Image) ouvrent la même vidéo.
    /// Celui qui attend est déjà ouvert, décodé et posé sur l'image 0. Quand le lecteur visible arrive au bout, on
    /// démarre l'autre et on échange leur visibilité dans la même image de rendu : la dernière image de la vidéo est
    /// suivie de la première, sans arrêt ni recherche. Le lecteur qui vient de finir est remis à zéro, caché, et
    /// attend le tour suivant. Pas de fondu : l'échange est franc, le raccord est dans la vidéo elle-même.
    ///
    /// Fin de la vidéo : le lecteur de Windows tronque la durée à la seconde (9,9 s annoncées 9 s) et sa position reste
    /// bloquée sur cette valeur, alors que le flux continue. La durée exacte est donc lue dans l'en-tête MP4 (mvhd).
    /// Si elle égale la durée annoncée (vidéo d'un nombre entier de secondes, comme les 12 s du menu), l'échange se
    /// fait 5 ms avant la fin, par la position. Sinon, il se fait sur MediaEnded (la vraie fin du flux, avec un peu de
    /// retard) ; dans les deux cas, rien n'est cherché au raccord : l'autre lecteur est déjà sur l'image 0.
    ///
    /// Démarrage sans à-coup : le fond vidéo est posé SOUS l'image fixe, qui est exactement l'image 0 de la vidéo
    /// (fond0.png, extraite au build par --image0). Dès l'ouverture, le premier lecteur est affiché (caché par l'image
    /// fixe) : la vidéo est composée à l'avance, hors de vue. La lecture démarre dessous ; quand la vidéo livre une
    /// vraie image (ni vide, ni la surface noire que montre le lecteur juste après Play), PremiereImage prévient
    /// l'écran, qui retire l'image fixe : on passe de l'image 0 à l'image 1 ou 2, quasi identique. En cas d'échec (fichier illisible, Windows N sans Media
    /// Feature Pack, délai dépassé), le fond vidéo se retire et l'image fixe reste.
    public sealed class FondVideo : Grid
    {
        /// Avance sur la fin : l'échange a lieu quand le lecteur visible est à moins de 5 ms du bout.
        static readonly TimeSpan Marge = TimeSpan.FromMilliseconds(5);
        static readonly TimeSpan DelaiOuverture = TimeSpan.FromSeconds(10);

        public event Action<string> Echec;
        /// L'image 0 de la vidéo est rendue sous l'image fixe : l'écran peut retirer celle-ci.
        public event Action PremiereImage;
        public event Action Demarre;
        /// Chaque échange (numéro du lecteur devenu visible, position du lecteur qui finissait).
        public event Action<int, TimeSpan> Echange;

        readonly MediaPlayer[] lecteurs = new MediaPlayer[2];
        readonly Image[] images = new Image[2];
        readonly VideoDrawing[] dessins = new VideoDrawing[2];
        readonly bool[] ouverts = new bool[2];
        DispatcherTimer delai, attenteImage;
        bool imagePrete;
        TimeSpan duree;          // durée annoncée par le lecteur (tronquée à la seconde)
        TimeSpan? dureeExacte;   // lue dans l'en-tête MP4
        bool dureeFiable;        // les deux concordent : l'échange peut se faire par la position
        readonly DispatcherTimer[] preparations = new DispatcherTimer[2];
        readonly int[] passes = new int[2];
        int actif;
        bool enCours, suspendu, abonne, termine;

        public FondVideo()
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
            Visibility = Visibility.Collapsed;
        }

        public bool EnCours => enCours && !termine;
        public TimeSpan Duree => duree;
        public bool DureeFiable => dureeFiable;
        public TimeSpan? DureeExacte => dureeExacte;
        public int Actif => actif;
        /// Dessin du lecteur caché (pour le banc : vérifier qu'il montre bien l'image 0 avant l'échange).
        public Drawing DessinEnAttente => dessins[1 - actif];
        public MediaPlayer Lecteur(int i) => lecteurs[i];

        /// fond.mp4 du dossier s'il existe, sinon null.
        public static string Chercher(string dossier)
        {
            string chemin = Path.Combine(dossier, "fond.mp4");
            return File.Exists(chemin) ? chemin : null;
        }

        public void Ouvrir(string chemin)
        {
            try
            {
                var uri = new Uri(Path.GetFullPath(chemin), UriKind.Absolute);
                dureeExacte = DureeMp4(chemin);
                for (int i = 0; i < 2; i++)
                {
                    int n = i;
                    var lecteur = new MediaPlayer { IsMuted = true, Volume = 0, ScrubbingEnabled = true };
                    lecteur.MediaOpened += (s, e) => Ouvert(n);
                    lecteur.MediaFailed += (s, e) => Abandonner("lecture impossible (" + (e.ErrorException?.Message ?? "erreur inconnue") + ")");
                    lecteur.MediaEnded += (s, e) =>
                    {
                        if (n != actif || termine) return;
                        Echanger();
                    };
                    lecteurs[i] = lecteur;
                    dessins[i] = new VideoDrawing { Player = lecteur, Rect = new Rect(0, 0, 16, 9) };
                    images[i] = new Image
                    {
                        Source = new DrawingImage(dessins[i]),
                        Stretch = Stretch.UniformToFill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        // Le premier lecteur est affiché dès l'ouverture, sous l'image fixe (composé à l'avance).
                        Visibility = i == 0 ? Visibility.Visible : Visibility.Hidden,
                    };
                    RenderOptions.SetBitmapScalingMode(images[i], BitmapScalingMode.HighQuality);
                    Children.Add(images[i]);
                    lecteur.Open(uri);
                }
                Visibility = Visibility.Visible;
                delai = new DispatcherTimer { Interval = DelaiOuverture };
                delai.Tick += (s, e) => { delai.Stop(); if (!enCours) Abandonner("la vidéo ne s'est pas ouverte à temps"); };
                delai.Start();
            }
            catch (Exception e)
            {
                Abandonner(e.Message);
            }
        }

        void Ouvert(int i)
        {
            if (termine) return;
            MediaPlayer lecteur = lecteurs[i];
            if (!lecteur.NaturalDuration.HasTimeSpan || lecteur.NaturalDuration.TimeSpan <= TimeSpan.FromMilliseconds(200) || lecteur.NaturalVideoWidth == 0)
            {
                Abandonner("vidéo sans durée ou sans image");
                return;
            }
            duree = lecteur.NaturalDuration.TimeSpan;
            dureeFiable = dureeExacte is TimeSpan exacte && Math.Abs((exacte - duree).TotalMilliseconds) <= 1;
            dessins[i].Rect = new Rect(0, 0, lecteur.NaturalVideoWidth, lecteur.NaturalVideoHeight);
            ouverts[i] = true;
            // Le lecteur en attente se pose sur l'image 0 (ScrubbingEnabled : l'image est décodée même en pause).
            lecteur.Pause();
            lecteur.Position = TimeSpan.Zero;
            if (ouverts[0] && ouverts[1]) AttendrePremiereImage();
        }

        /// Lance la lecture SOUS l'image fixe et attend que la vidéo en lecture livre une vraie image (dessin non vide et
        /// non noir, position avancée d'au moins une image) : juste après Play(), le lecteur peut montrer un instant
        /// une surface noire, qui ne doit jamais être vue. Alors seulement, PremiereImage prévient l'écran, qui retire
        /// l'image fixe (l'image 0) : la vidéo, à son image 1 ou 2, prend sa place.
        void AttendrePremiereImage()
        {
            delai?.Stop();
            actif = 0;
            lecteurs[0].Play();
            enCours = true;
            Abonner(true);
            Demarre?.Invoke();

            var petit = new RenderTargetBitmap(4, 4, 96, 96, PixelFormats.Pbgra32);
            var pixels = new int[16];
            int essais = 0;
            attenteImage = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(15) };
            attenteImage.Tick += (s, e) =>
            {
                if (termine) { attenteImage.Stop(); return; }
                if (++essais > 300) { Abandonner("la vidéo ne livre pas d'image"); return; }
                if (lecteurs[actif].Position < TimeSpan.FromMilliseconds(34)) return;
                var visuel = new DrawingVisual();
                using (DrawingContext dc = visuel.RenderOpen())
                {
                    Rect r = dessins[actif].Rect;
                    dc.PushTransform(new ScaleTransform(4 / r.Width, 4 / r.Height));
                    dc.DrawDrawing(dessins[actif]);
                }
                petit.Clear();
                petit.Render(visuel);
                petit.CopyPixels(pixels, 16, 0);
                int lumiere = 0;
                foreach (int p in pixels)
                {
                    if (((uint)p >> 24) == 0) return; // pas encore d'image
                    lumiere += ((p >> 16) & 0xff) + ((p >> 8) & 0xff) + (p & 0xff);
                }
                if (lumiere < 16 * 3 * 6) return; // surface noire : pas encore une vraie image
                attenteImage.Stop();
                imagePrete = true;
                PremiereImage?.Invoke();
            };
            attenteImage.Start();
        }

        public bool ImagePrete => imagePrete;

        void Abonner(bool oui)
        {
            if (oui == abonne) return;
            abonne = oui;
            if (oui) CompositionTarget.Rendering += Image;
            else CompositionTarget.Rendering -= Image;
        }

        void Image(object sender, EventArgs e) => Avancer();

        /// Appelé à chaque image de rendu : échange les lecteurs quand le visible arrive au bout.
        public void Avancer()
        {
            if (!enCours || suspendu || termine) return;
            // Durée annoncée tronquée : c'est MediaEnded qui déclenche l'échange.
            if (dureeFiable && lecteurs[actif].Position >= duree - Marge) Echanger();
        }

        void Echanger()
        {
            if (!enCours || termine) return;
            int fini = actif, suivant = 1 - actif;
            TimeSpan position = lecteurs[fini].Position;
            lecteurs[suivant].Play();
            images[suivant].Visibility = Visibility.Visible;
            images[fini].Visibility = Visibility.Hidden;
            actif = suivant;
            // Le lecteur qui a fini attend caché, remis sur l'image 0. La remise à zéro est refaite deux fois un peu
            // plus tard : juste à la fin du flux, elle n'est pas toujours prise en compte, et le lecteur montrerait
            // encore sa dernière image au tour suivant.
            lecteurs[fini].Pause();
            lecteurs[fini].Position = TimeSpan.Zero;
            Preparer(fini);
            Echange?.Invoke(suivant, position);
        }

        void Preparer(int i)
        {
            if (preparations[i] == null)
            {
                int n = i;
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                t.Tick += (s, e) =>
                {
                    if (termine || n == actif) { t.Stop(); return; }
                    lecteurs[n].Pause();
                    lecteurs[n].Position = TimeSpan.Zero;
                    if (++passes[n] >= 2) t.Stop();
                };
                preparations[i] = t;
            }
            passes[i] = 0;
            preparations[i].Stop();
            preparations[i].Start();
        }

        /// Durée exacte d'un MP4 : boîte moov/mvhd (échelle de temps et durée). Null si le fichier n'est pas lisible ainsi.
        public static TimeSpan? DureeMp4(string chemin)
        {
            try
            {
                using (var f = new FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var r = new BinaryReader(f))
                {
                    long fin = f.Length, debut = 0;
                    for (int profondeur = 0; profondeur < 2; profondeur++)
                    {
                        bool trouve = false;
                        long pos = debut;
                        while (pos + 8 <= fin)
                        {
                            f.Position = pos;
                            long taille = GrosBout(r.ReadBytes(4));
                            string type = System.Text.Encoding.ASCII.GetString(r.ReadBytes(4));
                            long entete = 8;
                            if (taille == 1) { taille = GrosBout(r.ReadBytes(8)); entete = 16; }
                            else if (taille == 0) taille = fin - pos;
                            if (taille < entete) return null;
                            if (profondeur == 0 && type == "moov") { debut = pos + entete; fin = pos + taille; trouve = true; break; }
                            if (profondeur == 1 && type == "mvhd")
                            {
                                int version = r.ReadByte();
                                r.ReadBytes(3);
                                long echelle, duree;
                                if (version == 1) { r.ReadBytes(16); echelle = GrosBout(r.ReadBytes(4)); duree = GrosBout(r.ReadBytes(8)); }
                                else { r.ReadBytes(8); echelle = GrosBout(r.ReadBytes(4)); duree = GrosBout(r.ReadBytes(4)); }
                                if (echelle <= 0) return null;
                                return TimeSpan.FromTicks((long)Math.Round(duree * 10000000.0 / echelle));
                            }
                            pos += taille;
                        }
                        if (!trouve) return null;
                    }
                }
            }
            catch { }
            return null;
        }

        static long GrosBout(byte[] octets)
        {
            long v = 0;
            foreach (byte b in octets) v = (v << 8) | b;
            return v;
        }

        /// Fenêtre réduite : la vidéo s'arrête (pas de décodage inutile) et reprend où elle était.
        public void Suspendre(bool oui)
        {
            if (!enCours || termine || oui == suspendu) return;
            suspendu = oui;
            if (oui) lecteurs[actif].Pause();
            else lecteurs[actif].Play();
        }

        void Abandonner(string raison)
        {
            if (termine) return;
            termine = true;
            enCours = false;
            Abonner(false);
            delai?.Stop();
            attenteImage?.Stop();
            Visibility = Visibility.Collapsed;
            foreach (MediaPlayer lecteur in lecteurs)
                try { lecteur?.Close(); } catch { }
            Children.Clear();
            Echec?.Invoke(raison);
        }

        public void Fermer()
        {
            if (termine) return;
            termine = true;
            enCours = false;
            Abonner(false);
            delai?.Stop();
            attenteImage?.Stop();
            foreach (MediaPlayer lecteur in lecteurs)
                try { lecteur?.Close(); } catch { }
        }
    }
}
