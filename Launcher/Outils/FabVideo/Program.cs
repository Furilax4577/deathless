using System;
using System.IO;
using System.Threading;
using Windows.Foundation;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;

// Outil de test du fond animé du launcher (voir Docs/launcher.md, « Outils de développement »). Fabrique une vidéo
// H.264 dont chaque image code son numéro dans sa couleur, avec l'encodeur de Windows (MediaComposition) : rien à
// télécharger ni à installer.
//
//   FabVideo <sortie.mp4> [images=60] [fps=30] [largeur=640] [hauteur=360] [bruit] [tronquee]
//
// Image i : couleur R = 20 + (i % 10) × 23, V = 20 + (i / 10) × 23, B = 128 (au plus 100 images) ; son numéro est aussi
// écrit en blanc dans le coin haut gauche. « bruit » : bruit aléatoire différent à chaque image, le code couleur dans
// un bloc en bas à droite (vidéo lourde à décoder ; en 1080p, l'encodeur ne garde qu'une image sur deux : mesurer avec
// --pas 2). L'encodeur écrit une durée d'un tick trop courte (N × d − 1), que le lecteur de Windows tronque à la
// seconde : la durée est donc réécrite à sa valeur exacte, sauf avec « tronquee » (pour tester l'échange sur
// MediaEnded).
static class Program
{
    static T Attendre<T>(IAsyncOperation<T> op) { while (op.Status == AsyncStatus.Started) Thread.Sleep(10); return op.GetResults(); }
    static T Attendre<T, P>(IAsyncOperationWithProgress<T, P> op) { while (op.Status == AsyncStatus.Started) Thread.Sleep(10); return op.GetResults(); }

    static int Main(string[] a)
    {
        if (a.Length == 0) { Console.WriteLine("FabVideo <sortie.mp4> [images=60] [fps=30] [largeur=640] [hauteur=360] [bruit] [tronquee]"); return 2; }
        string sortie = Path.GetFullPath(a[0]);
        int n = a.Length > 1 ? int.Parse(a[1]) : 60, fps = a.Length > 2 ? int.Parse(a[2]) : 30;
        uint w = a.Length > 3 ? uint.Parse(a[3]) : 640, h = a.Length > 4 ? uint.Parse(a[4]) : 360;
        bool bruit = Array.IndexOf(a, "bruit") >= 0, tronquee = Array.IndexOf(a, "tronquee") >= 0;
        if (n > 100) { Console.WriteLine("100 images au plus (code couleur sur deux chiffres)."); return 2; }

        var composition = new MediaComposition();
        var duree = TimeSpan.FromTicks(10000000L / fps);
        string images = Path.Combine(Path.GetTempPath(), "FabVideo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(images);
        try
        {
            StorageFolder dossierImages = Attendre(StorageFolder.GetFolderFromPathAsync(images));
            for (int i = 0; i < n; i++)
            {
                string png = Path.Combine(images, i.ToString("0000") + ".png");
                using (var bmp = new System.Drawing.Bitmap((int)w, (int)h))
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    var code = System.Drawing.Color.FromArgb(20 + (i % 10) * 23, 20 + (i / 10) * 23, 128);
                    if (bruit)
                    {
                        var zone = new System.Drawing.Rectangle(0, 0, (int)w, (int)h);
                        var bits = bmp.LockBits(zone, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        var octets = new byte[bits.Stride * (int)h];
                        new Random(1000 + i).NextBytes(octets);
                        for (int k = 3; k < octets.Length; k += 4) octets[k] = 255;
                        System.Runtime.InteropServices.Marshal.Copy(octets, 0, bits.Scan0, octets.Length);
                        bmp.UnlockBits(bits);
                        using (var pinceau = new System.Drawing.SolidBrush(code))
                            g.FillRectangle(pinceau, (int)(w * 0.6), (int)(h * 0.55), (int)w, (int)h);
                    }
                    else
                        g.Clear(code);
                    using (var f = new System.Drawing.Font("Segoe UI", h / 6f, System.Drawing.FontStyle.Bold))
                        g.DrawString(i.ToString(), f, System.Drawing.Brushes.White, 10, 10);
                    bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
                }
                StorageFile fichierImage = Attendre(dossierImages.GetFileAsync(Path.GetFileName(png)));
                composition.Clips.Add(Attendre(MediaClip.CreateFromImageFileAsync(fichierImage, duree)));
            }

            var profil = MediaEncodingProfile.CreateMp4(w >= 1920 ? VideoEncodingQuality.HD1080p : VideoEncodingQuality.HD720p);
            profil.Video.Width = w;
            profil.Video.Height = h;
            profil.Video.FrameRate.Numerator = (uint)fps;
            profil.Video.FrameRate.Denominator = 1;
            profil.Audio = null;
            StorageFolder dossier = Attendre(StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(sortie)));
            StorageFile fichier = Attendre(dossier.CreateFileAsync(Path.GetFileName(sortie), CreationCollisionOption.ReplaceExisting));
            var res = Attendre(composition.RenderToFileAsync(fichier, MediaTrimmingPreference.Precise, profil));
            if (res != Windows.Media.Transcoding.TranscodeFailureReason.None) { Console.WriteLine("Échec de l'encodage : " + res); return 1; }
        }
        finally
        {
            try { Directory.Delete(images, true); } catch { }
        }
        if (!tronquee) DureeExacte(sortie);
        Console.WriteLine("Vidéo : " + sortie + " (" + new FileInfo(sortie).Length + " octets, " + n + " images à " + fps + " i/s"
            + (bruit ? ", bruit" : "") + (tronquee ? ", durée d'origine" : ", durée exacte") + ")");
        return 0;
    }

    /// Réécrit les durées mvhd, tkhd et mdhd (version 0) à la somme des durées d'images (table stts).
    static void DureeExacte(string chemin)
    {
        byte[] d = File.ReadAllBytes(chemin);
        long total = 0;
        var boites = new System.Collections.Generic.List<(string type, int pos)>();
        void Parcourir(int debut, int fin)
        {
            int pos = debut;
            while (pos + 8 <= fin)
            {
                int taille = (d[pos] << 24) | (d[pos + 1] << 16) | (d[pos + 2] << 8) | d[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(d, pos + 4, 4);
                if (taille < 8) return;
                boites.Add((type, pos));
                if (type == "moov" || type == "trak" || type == "mdia" || type == "minf" || type == "stbl") Parcourir(pos + 8, pos + taille);
                pos += taille;
            }
        }
        Parcourir(0, d.Length);
        uint Lire(int p) => (uint)((d[p] << 24) | (d[p + 1] << 16) | (d[p + 2] << 8) | d[p + 3]);
        void Ecrire(int p, uint v) { d[p] = (byte)(v >> 24); d[p + 1] = (byte)(v >> 16); d[p + 2] = (byte)(v >> 8); d[p + 3] = (byte)v; }
        foreach (var (type, pos) in boites)
            if (type == "stts")
                for (uint i = 0, nb = Lire(pos + 12); i < nb; i++) total += (long)Lire(pos + 16 + 8 * (int)i) * Lire(pos + 20 + 8 * (int)i);
        if (total <= 0) return;
        foreach (var (type, pos) in boites)
        {
            if (d[pos + 8] != 0) continue; // version 1 : laissée telle quelle
            if (type == "mvhd" || type == "mdhd") Ecrire(pos + 8 + 16, (uint)total); // même échelle de temps ici
            if (type == "tkhd") Ecrire(pos + 8 + 20, (uint)total);
        }
        File.WriteAllBytes(chemin, d);
    }
}
