using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeathlessLauncher
{
    // --test-video <vidéo.mp4> [--images 90] [--secondes 12] [--naif]
    // Banc du fond animé, sans fenêtre. La vidéo de test doit coder le numéro de chaque image dans sa couleur :
    // R = 20 + (i % 10) × 23, V = 20 + (i / 10) × 23, B = 128 (fabriquée par l'outil décrit dans Docs/launcher.md),
    // relue en tenant compte de la plage limitée du décodeur.
    // Le fond est rendu hors écran (RenderTargetBitmap) toutes les 2 ms environ ; on relit le numéro de l'image
    // réellement affichée, puis on mesure combien de temps chaque image reste à l'écran, les sauts et les retours en
    // arrière. Au raccord, la dernière image doit être suivie de la première sans image tenue plus longtemps que les
    // autres. --naif mesure la boucle simple (un seul lecteur remis à zéro) pour comparaison.
    static class TestVideo
    {
        [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint ms);
        [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint ms);

        struct Mesure { public double T; public int Image; public int Lecteur; }

        public static void Demarrer(string[] args, Application app)
        {
            Sortie.Attacher();
            string chemin = Arguments.Valeur(args, "--test-video");
            int n = int.TryParse(Arguments.Valeur(args, "--images"), out int ni) ? ni : 90;
            double secondes = double.TryParse(Arguments.Valeur(args, "--secondes"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s) ? s : 12;
            bool naif = Arguments.Present(args, "--naif");
            int pas = int.TryParse(Arguments.Valeur(args, "--pas"), out int pa) ? pa : 1; // 2 si le fichier ne garde qu'une image sur deux
            if (Arguments.Present(args, "--inventaire")) { Inventaire(chemin, n, app); return; }
            timeBeginPeriod(1);

            var hote = new Grid { Width = 32, Height = 18, Background = Brushes.Black };
            var mesures = new List<Mesure>();
            var horloge = Stopwatch.StartNew();
            int rendus = 0, echanges = 0, attenteSurZero = 0, attenteAutre = 0;
            int attente = -2; // image montrée par le lecteur caché à la dernière mesure
            double debut = -1;
            bool manuel = false;
            CompositionTarget.Rendering += (o, e) => rendus++;

            FondVideo fond = null;
            MediaPlayer seul = null;
            Action avancer = () => { };
            if (naif)
            {
                // Boucle simple : un seul lecteur, remis à zéro et relancé à la fin (ce que ferait un MediaElement).
                seul = new MediaPlayer { IsMuted = true, ScrubbingEnabled = true };
                var dessin = new VideoDrawing { Player = seul, Rect = new Rect(0, 0, 32, 18) };
                hote.Children.Add(new Image { Source = new DrawingImage(dessin), Stretch = Stretch.Fill });
                seul.MediaOpened += (o, e) => { seul.Play(); debut = horloge.Elapsed.TotalSeconds; };
                seul.MediaEnded += (o, e) => { echanges++; seul.Position = TimeSpan.Zero; seul.Play(); };
                seul.MediaFailed += (o, e) => { Console.WriteLine("ÉCHEC : " + e.ErrorException?.Message); app.Shutdown(1); };
                seul.Open(new Uri(System.IO.Path.GetFullPath(chemin)));
            }
            else
            {
                fond = new FondVideo();
                hote.Children.Add(fond);
                fond.Echec += r => { Console.WriteLine("RÉSULTAT : ÉCHEC, repli sur l'image fixe (" + r + ")"); app.Shutdown(1); };
                fond.Demarre += () =>
                {
                    debut = horloge.Elapsed.TotalSeconds;
                    Console.WriteLine("Vidéo ouverte : durée annoncée " + fond.Duree.TotalSeconds.ToString("0.000") + " s, durée exacte (en-tête MP4) "
                        + (fond.DureeExacte is TimeSpan x ? x.TotalSeconds.ToString("0.000") + " s" : "?") + ", deux lecteurs prêts ; échange "
                        + (fond.DureeFiable ? "par la position (5 ms avant la fin)" : "sur MediaEnded (durée annoncée tronquée)"));
                };
                fond.Echange += (i, pos) =>
                {
                    echanges++;
                    if (attente == 0) attenteSurZero++; else attenteAutre++;
                    Console.WriteLine("  échange à " + (horloge.Elapsed.TotalSeconds - debut).ToString("0.000") + " s : lecteur " + i + " visible (l'autre était à "
                        + pos.TotalSeconds.ToString("0.000") + " s ; juste avant, le lecteur caché montrait l'image " + (attente >= 0 ? attente.ToString() : "?") + ")"
                        );
                };
                avancer = () => fond.Avancer();
                fond.Ouvrir(chemin);
            }
            Console.WriteLine("Mode : " + (naif ? "boucle simple (un lecteur)" : "deux lecteurs en alternance") + ", " + n + " images par tour");

            var image = new RenderTargetBitmap(32, 18, 96, 96, PixelFormats.Pbgra32);
            var pixels = new int[32 * 18];
            var minuteur = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1) };
            minuteur.Tick += (o, e) =>
            {
                if (debut < 0) return;
                double t = horloge.Elapsed.TotalSeconds - debut;
                // Sans fenêtre, CompositionTarget.Rendering peut ne pas battre : on fait alors avancer le fond à la main.
                if (!manuel && t > 1 && rendus == 0) { manuel = true; Console.WriteLine("(CompositionTarget.Rendering muet sans fenêtre : Avancer() appelé par le banc)"); }
                if (manuel) avancer();
                hote.Measure(new Size(32, 18));
                hote.Arrange(new Rect(0, 0, 32, 18));
                hote.UpdateLayout();
                image.Clear();
                image.Render(hote);
                image.CopyPixels(pixels, 32 * 4, 0);
                int lu = Lire(pixels[14 * 32 + 28]); // numéro de l'image affichée, -1 si illisible
                if (fond != null && fond.EnCours)
                {
                    // Ce que montre le lecteur caché (rendu à part : son Image est masquée).
                    var visuel = new DrawingVisual();
                    using (DrawingContext dc = visuel.RenderOpen())
                    {
                        Rect bornes = fond.DessinEnAttente.Bounds;
                        dc.PushTransform(new ScaleTransform(32 / bornes.Width, 18 / bornes.Height));
                        dc.DrawDrawing(fond.DessinEnAttente);
                    }
                    var cache = new RenderTargetBitmap(32, 18, 96, 96, PixelFormats.Pbgra32);
                    cache.Render(visuel);
                    var px = new int[32 * 18];
                    cache.CopyPixels(px, 32 * 4, 0);
                    attente = Lire(px[14 * 32 + 28]);
                }
                mesures.Add(new Mesure { T = t, Image = lu, Lecteur = fond?.Actif ?? 0 });
                if (t >= secondes)
                {
                    minuteur.Stop();
                    timeEndPeriod(1);
                    fond?.Fermer();
                    seul?.Close();
                    Console.WriteLine("Rendering : " + (rendus / Math.Max(0.001, t)).ToString("0") + " battements/s" + (manuel ? " (fond avancé à la main)" : ""));
                    if (!naif) Console.WriteLine("Lecteur caché sur l'image 0 juste avant l'échange : " + attenteSurZero + " fois sur " + (attenteSurZero + attenteAutre));
                    bool ok = Analyser(mesures, n, echanges, pas) && (naif || attenteAutre == 0);
                    app.Shutdown(ok ? 0 : 1);
                }
            };
            minuteur.Start();
        }

        /// --inventaire : parcourt la vidéo à l'arrêt, image par image (recherche au milieu de chaque image), et liste les
        /// numéros lus : vérifie ce que contient le fichier, indépendamment de la lecture en continu.
        static void Inventaire(string chemin, int n, Application app)
        {
            var lecteur = new MediaPlayer { IsMuted = true, ScrubbingEnabled = true };
            var dessin = new VideoDrawing { Player = lecteur, Rect = new Rect(0, 0, 32, 18) };
            var lus = new List<int>();
            int k = -1;
            var minuteur = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            minuteur.Tick += (o, e) =>
            {
                if (k >= 0)
                {
                    var visuel = new DrawingVisual();
                    using (DrawingContext dc = visuel.RenderOpen()) dc.DrawDrawing(dessin);
                    var rtb = new RenderTargetBitmap(32, 18, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(visuel);
                    var px = new int[32 * 18];
                    rtb.CopyPixels(px, 32 * 4, 0);
                    lus.Add(Lire(px[14 * 32 + 28]));
                }
                k++;
                if (k >= n)
                {
                    minuteur.Stop();
                    Console.WriteLine("Durée annoncée : " + (lecteur.NaturalDuration.HasTimeSpan ? lecteur.NaturalDuration.TimeSpan.TotalSeconds.ToString("0.000") + " s" : "?"));
                    Console.WriteLine("Images lues à l'arrêt (0 à " + (n - 1) + ") : " + string.Join(" ", lus));
                    lecteur.Close();
                    app.Shutdown(0);
                    return;
                }
                lecteur.Position = TimeSpan.FromSeconds((k + 0.5) / 30.0);
            };
            lecteur.MediaOpened += (o, e) => { lecteur.Pause(); minuteur.Start(); };
            lecteur.MediaFailed += (o, e) => { Console.WriteLine("ÉCHEC : " + e.ErrorException?.Message); app.Shutdown(1); };
            lecteur.Open(new Uri(System.IO.Path.GetFullPath(chemin)));
        }

        static int Lire(int p)
        {
            int r = (p >> 16) & 0xff, g = (p >> 8) & 0xff, b = p & 0xff;
            // Le décodeur rend la vidéo en plage limitée (16 à 235) : v' = 16 + v × 219 / 255.
            double pas = 23 * 219 / 255.0, zero = 16 + 20 * 219 / 255.0;
            int u = (int)Math.Round((r - zero) / pas), d = (int)Math.Round((g - zero) / pas);
            bool lisible = Math.Abs(b - (16 + 128 * 219 / 255.0)) < 16 && u >= 0 && u <= 9 && d >= 0 && Math.Abs(r - (zero + u * pas)) < 7 && Math.Abs(g - (zero + d * pas)) < 7;
            return lisible ? d * 10 + u : -1;
        }

        /// Découpe les mesures en « tenues » (une même image affichée) et vérifie l'enchaînement.
        static bool Analyser(List<Mesure> mesures, int n, int echanges, int pas)
        {
            // Première seconde écartée (mise en route du décodeur).
            var m = mesures.Where(x => x.T >= 1.0).ToList();
            int illisibles = m.Count(x => x.Image < 0);
            var tenues = new List<(int image, double debut, double fin)>();
            foreach (Mesure x in m)
            {
                if (x.Image < 0) continue;
                if (tenues.Count > 0 && tenues[tenues.Count - 1].image == x.Image)
                    tenues[tenues.Count - 1] = (x.Image, tenues[tenues.Count - 1].debut, x.T);
                else
                    tenues.Add((x.Image, x.T, x.T));
            }
            int suites = 0, sauts = 0, reculs = 0, raccords = 0, raccordsParfaits = 0;
            var durees = new List<double>();
            var dureesRaccord = new List<double>();
            for (int i = 1; i < tenues.Count - 1; i++)
            {
                int avant = tenues[i - 1].image, ici = tenues[i].image;
                double duree = tenues[i + 1].debut - tenues[i].debut; // du début de cette image au début de la suivante
                int ecart = ((ici - avant) % n + n) % n;
                bool raccord = avant > ici;
                if (raccord) raccords++;
                // Au raccord, la dernière image présente est n - pas (avec pas = 2, la 89 n'existe pas : 88 puis 0).
                if (ecart == pas || (raccord && ici == 0 && avant == n - pas)) { suites++; if (raccord) raccordsParfaits++; }
                else if (ecart > pas && ecart < n / 2) sauts++;
                else reculs++;
                bool presDuRaccord = ici < 2 * pas || ici >= n - 2 * pas;
                (presDuRaccord ? dureesRaccord : durees).Add(duree * 1000);
            }
            // Détail de chaque raccord : les trois images d'avant et d'après, avec leur durée d'affichage.
            for (int i = 1; i < tenues.Count; i++)
            {
                if (tenues[i - 1].image <= tenues[i].image) continue;
                var morceaux = new List<string>();
                for (int k = Math.Max(0, i - 3); k < Math.Min(tenues.Count - 1, i + 3); k++)
                    morceaux.Add(tenues[k].image + " (" + ((tenues[k + 1].debut - tenues[k].debut) * 1000).ToString("0") + " ms)");
                Console.WriteLine("  raccord à " + tenues[i].debut.ToString("0.000") + " s : " + string.Join(" > ", morceaux));
            }
            durees.Sort();
            double mediane = durees.Count > 0 ? durees[durees.Count / 2] : 0;
            double maxAilleurs = durees.Count > 0 ? durees.Max() : 0;
            double maxRaccord = dureesRaccord.Count > 0 ? dureesRaccord.Max() : 0;
            Console.WriteLine("Mesures : " + m.Count + " (" + illisibles + " illisibles), " + tenues.Count + " images vues, " + echanges + " bouclages");
            Console.WriteLine("Enchaînements : " + suites + " image suivante, " + sauts + " saut(s) en avant, " + reculs + " retour(s) en arrière");
            Console.WriteLine("Raccords (dernière image -> première) : " + raccords + " vus, " + raccordsParfaits + " sans image perdue");
            Console.WriteLine("Durée d'affichage d'une image : médiane " + mediane.ToString("0.0") + " ms, maximum hors raccord " + maxAilleurs.ToString("0.0")
                + " ms, maximum autour du raccord " + maxRaccord.ToString("0.0") + " ms");
            // Autour du raccord, pas d'image tenue plus d'une image et demie de plus que le pire du reste de la vidéo.
            double image = pas * 1000.0 / 30;
            bool ok = illisibles == 0 && reculs == 0 && raccords > 0 && raccordsParfaits == raccords && maxRaccord <= Math.Max(maxAilleurs, 2 * image) + 0.5 * image;
            Console.WriteLine("RÉSULTAT : " + (ok ? "BOUCLE SANS SACCADE" : "SACCADE OU DÉFAUT AU RACCORD"));
            return ok;
        }
    }
}
