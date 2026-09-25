using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeathlessLauncher
{
    // --image0 <vidéo.mp4> <image.png>
    // Extrait l'image 0 de la vidéo, à sa taille native, par le même chemin que la lecture (MediaPlayer de WPF, rendu en
    // RenderTargetBitmap) : l'image fixe de départ (fond0.png) est ainsi exactement ce que la vidéo affichera, couleurs
    // comprises. Appelée au build (cible ImageZeroDuFond du .csproj) et par publish.ps1. Sans fenêtre.
    static class ImageZero
    {
        public static void Extraire(string[] args, Application app)
        {
            Sortie.Attacher();
            int i = Array.IndexOf(args, "--image0");
            if (i < 0 || i + 2 >= args.Length) { Console.WriteLine("--image0 <vidéo.mp4> <image.png>"); app.Shutdown(2); return; }
            string video = Path.GetFullPath(args[i + 1]), sortie = Path.GetFullPath(args[i + 2]);

            var lecteur = new MediaPlayer { IsMuted = true, ScrubbingEnabled = true };
            VideoDrawing dessin = null;
            int[] precedente = null;
            int essais = 0;
            var minuteur = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            Action<int, string> finir = (code, message) =>
            {
                minuteur.Stop();
                Console.WriteLine(message);
                lecteur.Close();
                app.Shutdown(code);
            };
            minuteur.Tick += (o, e) =>
            {
                int w = lecteur.NaturalVideoWidth, h = lecteur.NaturalVideoHeight;
                var visuel = new DrawingVisual();
                using (DrawingContext dc = visuel.RenderOpen()) dc.DrawDrawing(dessin);
                var image = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                image.Render(visuel);
                var pixels = new int[w * h];
                image.CopyPixels(pixels, w * 4, 0);
                bool pleine = ((uint)pixels[0] >> 24) != 0 && ((uint)pixels[pixels.Length / 2] >> 24) != 0 && ((uint)pixels[pixels.Length - 1] >> 24) != 0;
                // On attend une image complète et stable (deux rendus identiques de suite).
                if (pleine && precedente != null && Egales(pixels, precedente))
                {
                    var png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(image));
                    Directory.CreateDirectory(Path.GetDirectoryName(sortie));
                    string temp = sortie + ".part";
                    using (var flux = File.Create(temp)) png.Save(flux);
                    if (File.Exists(sortie)) File.Delete(sortie);
                    File.Move(temp, sortie);
                    finir(0, "Image 0 de " + Path.GetFileName(video) + " : " + sortie + " (" + w + " × " + h + ")");
                    return;
                }
                precedente = pleine ? pixels : null;
                if (++essais > 150) finir(1, "Image 0 introuvable dans " + video);
            };
            lecteur.MediaOpened += (o, e) =>
            {
                dessin = new VideoDrawing { Player = lecteur, Rect = new Rect(0, 0, lecteur.NaturalVideoWidth, lecteur.NaturalVideoHeight) };
                lecteur.Pause();
                lecteur.Position = TimeSpan.Zero;
                minuteur.Start();
            };
            lecteur.MediaFailed += (o, e) => finir(1, "Lecture impossible de " + video + " : " + e.ErrorException?.Message);
            lecteur.Open(new Uri(video));
        }

        static bool Egales(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
