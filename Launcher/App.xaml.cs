using System;
using System.Windows;

namespace DeathlessLauncher
{
    // Point d'entrée. Sans argument : la fenêtre du launcher. Options cachées, sans aucune fenêtre (outils de
    // développement, voir Docs/launcher.md) :
    //   --capture <fichier.png> [--etat accueil|telechargement|verification|pret|horsligne|erreur] [--notes deplie] [--manette]
    //             [--changelog <changelog.json>] [--largeur 1280 --hauteur 720]
    //   --test-maj [--racine <dossier>]   déroulé complet de la mise à jour, compte rendu sur la sortie standard
    //   --test-rafraichir [--racine <dossier>]   la vérification de Rafraîchir (à jour, nouvelle version, injoignable)
    //   --image0 <vidéo.mp4> <image.png>   extrait l'image 0 de la vidéo (fond0.png, au build)
    //   --test-video, --test-demarrage     bancs du fond animé (TestVideo.cs, TestDemarrage.cs)
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string[] args = e.Args;
            try
            {
                if (Array.IndexOf(args, "--capture") >= 0)
                {
                    Shutdown(Capture.Executer(args));
                    return;
                }
                if (Array.IndexOf(args, "--test-rafraichir") >= 0)
                {
                    Shutdown(TestRafraichir.Executer(args));
                    return;
                }
                if (Array.IndexOf(args, "--test-maj") >= 0)
                {
                    Shutdown(TestMaj.Executer(args));
                    return;
                }
                if (Array.IndexOf(args, "--image0") >= 0)
                {
                    ImageZero.Extraire(args, this); // quitte de lui-même une fois l'image écrite
                    return;
                }
                if (Array.IndexOf(args, "--test-demarrage") >= 0)
                {
                    TestDemarrage.Demarrer(args, this); // quitte de lui-même à la fin de la mesure
                    return;
                }
                if (Array.IndexOf(args, "--test-video") >= 0)
                {
                    TestVideo.Demarrer(args, this); // quitte de lui-même à la fin de la mesure
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Shutdown(3);
                return;
            }

            var window = new MainWindow();
            MainWindow = window;
            window.Closed += (s, a) => Shutdown(0);
            window.Show();
        }
    }
}
