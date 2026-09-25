using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DeathlessLauncher
{
    // --test-demarrage [--visuel] [--plein] [--secondes 3]
    // Banc du passage de l'image fixe à la vidéo, sans fenêtre, avec le fond.mp4 (et fond0.png) posés à côté de l'exe.
    // L'écran complet est construit hors écran ; la vidéo démarre à 0,5 s, comme au chargement de la fenêtre.
    //  - Fil de l'interface : durée de chaque opération qu'il exécute (Dispatcher.Hooks), hors passes de rendu. Sans
    //    fenêtre, les passes de rendu attendent le fil de rendu à ~30 Hz et ne mesurent rien d'utile ; une opération de
    //    notre code qui dépasse ~20 ms, elle, gèlerait l'interface à l'écran.
    //  - --visuel : le fond est rendu hors écran toutes les 16 ms (96 × 54, ou 1920 × 1080 avec --plein, où la vidéo
    //    n'est pas mise à l'échelle) et comparé à l'image de départ : écart moyen de 0 à 255 par canal. Un saut
    //    d'image ou une image noire au passage s'y voit. Ce rendu charge le fil de l'interface : les durées
    //    d'opérations se lisent dans la mesure sans --visuel.
    static class TestDemarrage
    {
        public static void Demarrer(string[] args, Application app)
        {
            Sortie.Attacher();
            bool visuel = Arguments.Present(args, "--visuel"), plein = Arguments.Present(args, "--plein");
            double secondes = double.TryParse(Arguments.Valeur(args, "--secondes"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s) ? s : 3;
            string dossier = AppDomain.CurrentDomain.BaseDirectory;
            if (FondVideo.Chercher(dossier) == null) { Console.WriteLine("Pas de fond.mp4 à côté de l'exe."); app.Shutdown(2); return; }

            var horloge = Stopwatch.StartNew();
            var ecran = new EcranLauncher();
            int w = plein ? 1920 : 1280, h = plein ? 1080 : 720;
            ecran.Measure(new Size(w, h));
            ecran.Arrange(new Rect(0, 0, w, h));
            ecran.UpdateLayout();
            Console.WriteLine("Image de départ : " + EcranLauncher.SourceDuFond(dossier));

            // Durée de chaque opération du fil de l'interface.
            var debuts = new Dictionary<DispatcherOperation, double>();
            var operations = new List<(double t, double ms, DispatcherPriority p, string nom)>();
            var nomOperation = typeof(DispatcherOperation).GetProperty("Name", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Dispatcher.CurrentDispatcher.Hooks.OperationStarted += (o, e) => debuts[e.Operation] = horloge.Elapsed.TotalMilliseconds;
            Dispatcher.CurrentDispatcher.Hooks.OperationCompleted += (o, e) =>
            {
                if (!debuts.TryGetValue(e.Operation, out double d)) return;
                debuts.Remove(e.Operation);
                double ms = horloge.Elapsed.TotalMilliseconds - d;
                if (ms >= 2) operations.Add((d / 1000, ms, e.Operation.Priority, nomOperation?.GetValue(e.Operation) as string ?? "?"));
            };

            var evenements = new List<(double t, string quoi)>();
            double tDemarrage = 0.5, tRetrait = -1;
            ecran.Video.Demarre += () => evenements.Add((horloge.Elapsed.TotalSeconds, "lecture lancée, sous l'image fixe"));
            ecran.Video.PremiereImage += () => { tRetrait = horloge.Elapsed.TotalSeconds; evenements.Add((tRetrait, "la vidéo livre une vraie image : image fixe retirée")); };
            ecran.Video.Echec += r => evenements.Add((horloge.Elapsed.TotalSeconds, "échec : " + r));

            // Écart visuel au fond de départ.
            var ecarts = new List<(double t, double ecart)>();
            int[] reference = null;
            var rendu = plein ? new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32) : new RenderTargetBitmap(96, 54, 96 * 96 / 1280.0, 96 * 54 / 720.0, PixelFormats.Pbgra32);
            var minuteurVisuel = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(16) };
            minuteurVisuel.Tick += (o, e) =>
            {
                ecran.UpdateLayout();
                rendu.Clear();
                rendu.Render(ecran.Racine);
                var px = new int[rendu.PixelWidth * rendu.PixelHeight];
                rendu.CopyPixels(px, rendu.PixelWidth * 4, 0);
                if (reference == null) { reference = px; return; }
                double somme = 0;
                for (int i = 0; i < px.Length; i++)
                    for (int c = 0; c < 24; c += 8)
                        somme += Math.Abs(((px[i] >> c) & 0xff) - ((reference[i] >> c) & 0xff));
                ecarts.Add((horloge.Elapsed.TotalSeconds, somme / (px.Length * 3)));
            };
            if (visuel) minuteurVisuel.Start();

            bool lance = false;
            var pilote = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            pilote.Tick += (o, e) =>
            {
                double t = horloge.Elapsed.TotalSeconds;
                if (!lance && t >= tDemarrage)
                {
                    lance = true;
                    double avant = horloge.Elapsed.TotalMilliseconds;
                    ecran.DemarrerVideo(dossier);
                    evenements.Add((t, "DemarrerVideo (" + (horloge.Elapsed.TotalMilliseconds - avant).ToString("0.0") + " ms)"));
                }
                if (t < secondes) return;
                pilote.Stop();
                minuteurVisuel.Stop();
                ecran.Video.Fermer();
                foreach (var ev in evenements.OrderBy(v => v.t)) Console.WriteLine("  " + ev.t.ToString("0.000") + " s : " + ev.quoi);

                double fin = (tRetrait > 0 ? tRetrait : tDemarrage) + 1.0;
                var hors = operations.Where(v => v.p != DispatcherPriority.Render && v.t > 0.1).ToList();
                Func<double, double, string> max = (a, b) =>
                {
                    var x = hors.Where(v => v.t >= a && v.t < b).ToList();
                    return x.Count == 0 ? "moins de 2 ms" : x.Max(v => v.ms).ToString("0.0") + " ms";
                };
                Console.WriteLine("Fil de l'interface, opération la plus longue : avant " + max(0.1, tDemarrage) + ", pendant le passage (" + tDemarrage.ToString("0.0")
                    + " à " + fin.ToString("0.00") + " s) " + max(tDemarrage, fin) + ", après " + max(fin, 1e9));
                foreach (var op in hors.Where(v => v.ms >= 4).OrderBy(v => v.t))
                    Console.WriteLine("  " + op.t.ToString("0.000") + " s : " + op.ms.ToString("0.0") + " ms, " + op.nom);
                if (visuel)
                {
                    var avant = ecarts.Where(v => v.t < tRetrait).ToList();
                    var apres = ecarts.Where(v => tRetrait > 0 && v.t >= tRetrait).ToList();
                    Console.WriteLine("Écart au fond de départ (0 à 255 par canal) : juste avant le passage " + (avant.Count > 0 ? avant.Last().ecart.ToString("0.00") : "?")
                        + ", 3 premières mesures après " + string.Join(" / ", apres.Take(3).Select(v => v.ecart.ToString("0.00")))
                        + ", plus grand écart dans la seconde qui suit " + (apres.Any(v => v.t < tRetrait + 1) ? apres.Where(v => v.t < tRetrait + 1).Max(v => v.ecart).ToString("0.00") : "?"));
                }
                app.Shutdown(0);
            };
            pilote.Start();
        }
    }
}
