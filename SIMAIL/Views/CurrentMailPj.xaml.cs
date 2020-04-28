using MimeKit;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SIMAIL.Views
{
    /// <summary>
    /// Logique d'interaction pour CurrentMailPj.xaml
    /// </summary>
    public partial class CurrentMailPj : Window
    {
        public List<MimePart> attachmentsParts;

        public CurrentMailPj()
        {
            InitializeComponent();
            attachmentsParts = new List<MimePart>();
        }

        // ouvrir avec le fichier sélectionné avec le logiciel par défaut sans l'enregistrer sous format fichier
        private void openPj()
        {
            if (I_CurrentMailPjList.SelectedIndex != -1)
            {
                if (I_CurrentMailPjList.SelectedItem != null)
                {
                    foreach (MimePart mp in attachmentsParts)
                    {
                        if(I_CurrentMailPjList.SelectedItem.Equals(mp.FileName))
                        {
                            var filename = mp.ContentDisposition?.FileName ?? mp.ContentType.Name;
                            string FolderDownloadPath = new KnownFolder(KnownFolderType.Downloads).Path;
                            try
                            {
                                var stream = File.Create(FolderDownloadPath + "\\" + filename);
                                mp.Content.DecodeTo(stream); // écriture dans le fichier généré
                                stream.Dispose();
                                Process.Start(FolderDownloadPath + "\\" + filename);
                            }
                            catch(Exception e)
                            {
                                MessageBox.Show(e.Message);
                            }           
                        }
                    }

                }
            }
        }

        // enregistre la sélection dans le répertoire utilisateur "Téléchargements"
        private void enregistrer()
        {
            if (I_CurrentMailPjList.SelectedIndex != -1)
            {
                if (I_CurrentMailPjList.SelectedItem != null)
                {
                    foreach (MimePart mp in attachmentsParts)
                    {
                        if (I_CurrentMailPjList.SelectedItem.Equals(mp.FileName))
                        {
                            var filename = mp.ContentDisposition?.FileName ?? mp.ContentType.Name;
                            string FolderDownloadPath = new KnownFolder(KnownFolderType.Downloads).Path;
                            try
                            {
                                var stream = File.Create(FolderDownloadPath + "\\" + filename);
                                mp.Content.DecodeTo(stream); // écriture dans le fichier généré
                                stream.Dispose();
                                Process.Start("explorer.exe", "/select, \"" + FolderDownloadPath + "\\" + filename + "\"");
                            }
                            catch(Exception e)
                            {
                                MessageBox.Show(e.Message);
                            }
                        }
                    }
                }
            }
        }

        // enregistre toutes les pièces jointes dans le répertoire utilisateur "Téléchargements"
        private void enregistrerTout()
        {
            if (attachmentsParts.Count > 0)
            {
                string lastFileSaved = "";
                foreach (MimePart mp in attachmentsParts)
                {
                    var filename = mp.ContentDisposition?.FileName ?? mp.ContentType.Name;
                    string FolderDownloadPath = new KnownFolder(KnownFolderType.Downloads).Path;
                    try
                    {
                        var stream = File.Create(FolderDownloadPath + "\\"+ filename); // Création du fichier dans le répertoire
                        mp.Content.DecodeTo(stream); // écriture dans le fichier généré
                        stream.Dispose();
                        lastFileSaved = FolderDownloadPath + "\\" + filename;
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }                
                }
                try
                {
                    Process.Start("explorer.exe", "/select, \"" + lastFileSaved + "\""); // ouverture du répertoire où ont été enregistrés les fichiers
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }  
            }  
        }


        private void I_CurrentMailPjList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            openPj();
        }

        private void BT_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           enregistrer();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            enregistrerTout();
        }

        private void I_CurrentMailPjList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                openPj();
            }
        }
    }
}
