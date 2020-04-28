using SIMAIL.Classes.Utilisateur;
using SIMAIL.Views;
using Syroot.Windows.IO;
using System;
using System.IO;
using System.Windows;

namespace SIMAIL
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public CompteMessagerie cm = new CompteMessagerie();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create the startup window
            SIMAIL.Views.Connexion FenStartup = new SIMAIL.Views.Connexion();
            FenStartup.currentCompteMessagerie = cm;

            // Paramètres comptes serveur
            string appDirectory = new KnownFolder(KnownFolderType.Documents).Path + "\\" + "SIMAIL";
            if (Directory.Exists(appDirectory) == false)
            {
                try
                {
                    Directory.CreateDirectory(appDirectory);
                }
                catch(Exception e2)
                {
                    MessageBox.Show(e2.Message);
                }
            }
            App.Current.Resources.Add("pathDefaultCompteServ", appDirectory);

            //paramètre de l'application
            string helpHttpAddress = "https://simail.chupin-pierre.fr/documentation";
            App.Current.Resources.Add("helpHttpAddress", helpHttpAddress);

            // Do stuff here, e.g. to the window
            FenStartup.Show();
            // Show the window
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            if (cm.IMAPclient != null)
            {
                if (cm.IMAPclient.IsConnected)
                {
                    await cm.IMAPclient.DisconnectAsync(true);
                }
            }    
        }
    }
}
