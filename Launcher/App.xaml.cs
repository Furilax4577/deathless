using System;
using System.Windows;

namespace DeathlessLauncher
{
    // Point d'entrée. Sans argument : la fenêtre du launcher. Options cachées, sans aucune fenêtre (outils de
    // développement, voir Docs/launcher.md) :
    //   --capture <fichier.png> [--etat accueil|telechargement|verification|pret|horsligne|erreur] [--manette]
    //             [--changelog <changelog.json>] [--largeur 1280 --hauteur 720]
    //   --test-maj [--racine <dossier>]   déroulé complet de la mise à jour, compte rendu sur la sortie standard
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
                if (Array.IndexOf(args, "--test-maj") >= 0)
                {
                    Shutdown(TestMaj.Executer(args));
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
