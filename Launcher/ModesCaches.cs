using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeathlessLauncher
{
    static class Arguments
    {
        public static string Valeur(string[] args, string nom)
        {
            int i = Array.IndexOf(args, nom);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[i + 1] : null;
        }

        public static bool Present(string[] args, string nom) => Array.IndexOf(args, nom) >= 0;
    }

    // --capture <fichier.png> [--etat …] [--manette] [--changelog <fichier>] [--largeur L --hauteur H]
    // Rend l'écran hors écran (RenderTargetBitmap, aucune fenêtre n'est créée ni affichée) avec des données d'exemple,
    // écrit le PNG et quitte. Sert aux captures de Launcher/captures/ et à vérifier l'allure sans prendre le bureau.
    static class Capture
    {
        const long Mo = 1048576;

        public static int Executer(string[] args)
        {
            string fichier = Arguments.Valeur(args, "--capture") ?? throw new ArgumentException("--capture <fichier.png> attendu.");
            string etat = (Arguments.Valeur(args, "--etat") ?? "accueil").ToLowerInvariant();
            string cheminNotes = Arguments.Valeur(args, "--changelog");
            int largeur = int.TryParse(Arguments.Valeur(args, "--largeur"), out int l) ? l : (int)EcranLauncher.LargeurBase;
            int hauteur = int.TryParse(Arguments.Valeur(args, "--hauteur"), out int h) ? h : (int)EcranLauncher.HauteurBase;

            JaugeVie.Animer = false;
            var ecran = new EcranLauncher();
            ecran.Appareil(Arguments.Present(args, "--manette"));

            string notes = cheminNotes != null ? File.ReadAllText(cheminNotes, Encoding.UTF8) : Exemple;
            var enLigne = new Manifest { Version = "1", Nom = "0.1", Zip = "deathless-v1.zip" };
            var liste = Changelog.Construire(notes, enLigne);

            switch (etat)
            {
                case "accueil":
                    ecran.AfficherNotes(liste, null, null);
                    ecran.MontrerRecherche();
                    break;
                case "telechargement":
                    ecran.AfficherNotes(liste, null, "0.1");
                    ecran.MontrerTelechargement(new Avancement { Phase = Phase.Telechargement, Version = "0.1", Fait = 212 * Mo + Mo / 3, Total = 487 * Mo, Debit = 4.3 * Mo });
                    break;
                case "verification":
                    ecran.AfficherNotes(liste, null, "0.1");
                    ecran.MontrerTelechargement(new Avancement { Phase = Phase.Telechargement, Version = "0.1", Fait = 487 * Mo, Total = 487 * Mo });
                    ecran.MontrerVerification("0.1");
                    break;
                case "pret":
                    ecran.AfficherNotes(liste, "0.1", "0.1");
                    ecran.MontrerPret("0.1", false);
                    break;
                case "horsligne":
                    ecran.AfficherNotes(liste, "0.1", null);
                    ecran.MontrerHorsLigne("0.1", "Impossible de se connecter au serveur distant");
                    break;
                case "erreur":
                    ecran.AfficherNotes(liste, null, "0.1");
                    ecran.MontrerEchec("La mise à jour a échoué", "Le fichier téléchargé est corrompu (empreinte SHA-256 différente). Vérifiez la connexion, puis Réessayer.", null);
                    break;
                default:
                    throw new ArgumentException("État inconnu : " + etat + " (accueil, telechargement, verification, pret, horsligne, erreur).");
            }

            var taille = new Size(largeur, hauteur);
            for (int i = 0; i < 3; i++)
            {
                ecran.Measure(taille);
                ecran.Arrange(new Rect(taille));
                ecran.UpdateLayout();
                ecran.ActualiserFondu();
            }
            var image = new RenderTargetBitmap(largeur, hauteur, 96, 96, PixelFormats.Pbgra32);
            image.Render(ecran);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            string dossier = Path.GetDirectoryName(Path.GetFullPath(fichier));
            if (!string.IsNullOrEmpty(dossier)) Directory.CreateDirectory(dossier);
            using (var flux = File.Create(fichier)) png.Save(flux);
            Sortie.Attacher();
            Console.WriteLine("Capture " + etat + " : " + Path.GetFullPath(fichier) + " (" + largeur + " × " + hauteur + ")");
            return 0;
        }

        // Utilisé si --changelog n'est pas donné (même contenu que Launcher/changelog.json, en plus court).
        const string Exemple = "{ \"versions\": [ { \"version\": \"0.1\", \"date\": \"2026-09-25\", \"titre\": \"Première défense du village\", \"notes\": ["
            + "\"Partie solo au village avec le Paladin.\", \"Cycle jour-nuit, 12 nuits à tenir.\", \"Nyxessa et ses missiles.\","
            + "\"Nuit 10 : Morgrim, le Roi des os.\", \"Nuit 12 : Nyxar, le Nécromancien.\", \"Écran de score.\" ] } ] }";
    }

    // --test-maj [--racine <dossier>] : déroulé complet sans fenêtre, avec le launcher.json de la racine (par défaut le
    // dossier de l'exe) et installation dans <racine>/Game. Code de sortie : 0 à jour ou installé, 2 hors ligne avec une
    // version jouable, 1 échec.
    static class TestMaj
    {
        public static int Executer(string[] args)
        {
            Sortie.Attacher();
            string racine = Path.GetFullPath(Arguments.Valeur(args, "--racine") ?? AppDomain.CurrentDomain.BaseDirectory);
            LauncherConfig config = LauncherConfig.Load(Path.Combine(racine, "launcher.json"));
            var updater = new Updater(config, racine);
            Manifest installed = updater.Installed;
            string installedName = updater.IsInstalled && installed != null ? installed.Affichage : null;
            Ecrire("Racine    : " + racine);
            Ecrire("Serveur   : " + (config.BaseUrl.Length > 0 ? config.BaseUrl : "(non renseigné)"));
            Ecrire("Installée : " + (installedName != null ? installedName + " (publication " + installed.Version + ")" : "aucune"));

            Phase? derniere = null;
            int palier = -1;
            updater.Rapport = a =>
            {
                if (a.Phase != derniere)
                {
                    derniere = a.Phase;
                    Ecrire("Étape     : " + a.Phase + (a.Reprise ? " (reprise)" : ""));
                }
                if (a.Phase == Phase.Telechargement && a.Total > 0)
                {
                    int p = (int)(100 * a.Fait / a.Total) / 25;
                    if (p != palier)
                    {
                        palier = p;
                        Ecrire("            " + Format.Mo(a.Fait) + " / " + Format.Mo(a.Total) + " Mo " + Format.Debit(a.Debit));
                    }
                }
            };

            if (!updater.IsConfigured)
            {
                Ecrire("RÉSULTAT  : NON CONFIGURÉ" + (installedName != null ? ", version installée jouable" : ""));
                return installedName != null ? 2 : 1;
            }

            Manifest remote;
            try
            {
                remote = updater.FetchRemoteAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                if (installedName != null)
                {
                    Ecrire("RÉSULTAT  : HORS LIGNE, version " + installedName + " jouable (" + MainWindow.Explication(e) + ")");
                    return 2;
                }
                Ecrire("RÉSULTAT  : ÉCHEC, serveur injoignable et aucune version installée (" + MainWindow.Explication(e) + ")");
                return 1;
            }
            Ecrire("En ligne  : " + remote.Affichage + " (publication " + remote.Version + ", " + remote.Zip + ", " + remote.Size + " octets, sha256 " + remote.Sha256 + ")");

            string changelog = updater.FetchChangelogAsync(CancellationToken.None).GetAwaiter().GetResult();
            var entrees = Changelog.Construire(changelog, remote);
            Ecrire("Notes     : " + (changelog != null ? "changelog.json" : "notes de version.json") + ", " + entrees.Count + " version(s)");
            foreach (EntreeChangelog v in entrees)
                Ecrire("            " + v.Version + (v.Date.Length > 0 ? " du " + v.DateAffichee : "") + " : " + v.Notes.Count + " note(s)" + (v.Notes.Count > 0 ? ", « " + v.Notes[0] + " »" : ""));

            if (Updater.IsUpToDate(installed, remote, updater.IsInstalled))
            {
                Ecrire("RÉSULTAT  : À JOUR (" + remote.Affichage + ")");
                return 0;
            }
            try
            {
                updater.InstallAsync(remote, CancellationToken.None).GetAwaiter().GetResult();
                string[] fichiers = Directory.GetFiles(updater.GameDir, "*", SearchOption.AllDirectories);
                Ecrire("Game/     : " + string.Join(", ", fichiers.Select(f => f.Substring(updater.GameDir.Length + 1))));
                Ecrire("RÉSULTAT  : INSTALLÉ (" + remote.Affichage + "), " + config.GameExe + (File.Exists(updater.GameExePath) ? " présent" : " ABSENT"));
                return 0;
            }
            catch (Exception e)
            {
                string playable = updater.IsInstalled && updater.Installed != null ? updater.Installed.Affichage : null;
                Ecrire("RÉSULTAT  : ÉCHEC de la mise à jour (" + MainWindow.Explication(e) + ")" + (playable != null ? ", version " + playable + " toujours jouable" : ""));
                return 1;
            }
        }

        static void Ecrire(string ligne)
        {
            Console.WriteLine(ligne);
            Console.Out.Flush();
        }
    }

    /// Un exe fenêtré n'a pas de console : si la sortie n'est pas redirigée, on s'attache à celle qui l'a lancé.
    static class Sortie
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int processId);

        static bool fait;

        public static void Attacher()
        {
            if (fait) return;
            fait = true;
            try
            {
                if (Console.IsOutputRedirected)
                    Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
                else if (AttachConsole(-1))
                    Console.OutputEncoding = new UTF8Encoding(false);
            }
            catch { }
        }
    }
}
