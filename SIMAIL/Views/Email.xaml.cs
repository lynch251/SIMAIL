
using Microsoft.Win32;
using SIMAIL.Classes.Email;
using SIMAIL.Classes.Utilisateur;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SIMAIL.Views
{
    public partial class Email : Window, IfPGProgress
    {
        public enum Action
        {
            Reponse,
            Transfert,
            Nouveau
        }

        #region Propriétés
        public Action action;
        public CompteMessagerie currentCompteMessagerie;
        public SIMAIL.Classes.Email.Email email { get; set; }
        private System.Windows.Forms.ErrorProvider gErrorProvider;
        #endregion

        #region Variables
        private Collection<TextBox> _ChampObligatoire;
        private Collection<TextBox> _DestChampObligatoire;
        const string gAddressSeparator = ";";
        int timeTimer = 3;
        DispatcherTimer timerPG = new DispatcherTimer();
        BackgroundWorker worker = new BackgroundWorker();
        bool asyncProcessRunning = false;
        #endregion

        public Email()
        {
            InitializeComponent();
            this.DataContext = this;

            // Action de la fenetre par défaut
            this.action = Action.Nouveau;

            // Initialisation de l'email
            email = new Classes.Email.Email();

            // Initialisation du compte utilisateur
            CompteMessagerie cm = new CompteMessagerie();
            email.CompteMessagerie = cm;

            // Initialisation editeur de texte
            CB_BodyFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            CB_BodyFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28 };

            // Initialisation des pj
            L_Pj.ItemsSource = null;
            L_Pj.ItemsSource = email.Pj;
            L_Pj.AllowDrop = true;

            // Champs obligatoires
            _ChampObligatoire = new Collection<TextBox>();
            _DestChampObligatoire = new Collection<TextBox>();
            _ChampObligatoire.Add(this.I_Objet);
            _ChampObligatoire.Add(this.I_De);
            _ChampObligatoire.Add(this.I_A);
            _DestChampObligatoire.Add(this.I_A);
            _DestChampObligatoire.Add(this.I_Cc);
            // Champs de saisie
            I_De.IsReadOnly = true;                      
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialisation de l'email
            if (email == null)
            {
                email = new Classes.Email.Email();
            }
            if (currentCompteMessagerie != null)
            {
                if (currentCompteMessagerie.isAuthenticated())
                {
                    // Chargement des paramètres utilisateur
                    email.CompteMessagerie = currentCompteMessagerie;
                    email.From = currentCompteMessagerie.Identifiant;
                }
                else
                {
                    this.Close();
                }
            }

            if (this.action == Action.Reponse)
            {
                // Préchargement d'un mail
                I_A.Text = email.ToToString();
                I_Cc.Text = email.CCToString();
                I_Objet.Text = email.Object;
                I_Body.AppendText(email.Body.TextReponse);
            }
            if(this.action == Action.Transfert)
            {
                I_Objet.Text = email.Object;
                I_Body.AppendText(email.Body.TextReponse);
            }
            // Préchargement du mail utilisateur
            I_De.Text = email.From;
        }
        
        private async Task envoyer()
        {
            if (currentCompteMessagerie.isAuthenticated())
            {
                if (asyncProcessRunning == false)
                {
                    try
                    {
                        if (checkMandatoryFields())
                        {
                            setInfoMail();
                            // Envoi du mail
                            PG_ProgressBar.Visibility = Visibility.Visible;
                            PG_ProgressBar.IsIndeterminate = true;
                            this.Cursor = Cursors.Wait;
                            if (await email.EnvoyerEmail() == true)
                            {
                                MessageBox.Show("Envoi effectué.");
                                email = null;
                                _ChampObligatoire = null;
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Une erreur est survenue, réessayer.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Arrow;
                        PG_ProgressBar.Visibility = Visibility.Hidden;
                        PG_ProgressBar.IsIndeterminate = false;
                    }
                }
            }          
        }

        //Renvoie vrai si tous les champs obligatoires sont renseignés
        private bool checkMandatoryFields()
        {
            bool vOneEmpty = true;
            foreach (TextBox vChamp in _ChampObligatoire)
            {
                if (vChamp.Text == "")
                {
                    vChamp.BorderBrush = Brushes.Red;               
                    vOneEmpty = false;
                }
                else
                {
                    vChamp.BorderBrush = Brushes.LightGray;
                }
            }
            foreach (TextBox vChamp in _DestChampObligatoire)
            {
                if (vChamp.Text != "")
                {
                    if (vChamp.Text.Contains(gAddressSeparator) == false)
                    {
                        vChamp.BorderBrush = Brushes.Red;
                        vOneEmpty = false;
                        MessageBox.Show("Veuillez séparer les adresses destinataires par \";\" ! ");
                    }    
                    else
                    {
                        vChamp.BorderBrush = Brushes.LightGray;
                    }
                }
                else if (_ChampObligatoire.Contains(vChamp) == false)
                {
                    vChamp.BorderBrush = Brushes.LightGray;
                }
            }
            if (getBodyText() == "\r\n")
            {
                vOneEmpty = false;
                I_Body.BorderBrush = Brushes.Red;
            }
            else
            {
                I_Body.BorderBrush = Brushes.LightGray;
            }
            return vOneEmpty;
        }

        // renvoie le contenu texte de l'éditeur
        private string getBodyText()
        {
            string text;
            TextRange textRange = new TextRange(I_Body.Document.ContentStart, I_Body.Document.ContentEnd);
            return text = textRange.Text;
        }

        /// Méthode de l'interface IfPGProgress permettant de lancer la barre de progression
        public void PGProgressRun(int pTimerSleep)
        {
            timeTimer = pTimerSleep;
            worker.RunWorkerAsync();
            worker.WorkerReportsProgress = true;
            worker.DoWork += PG_PjProgress;
            worker.ProgressChanged += PG_PjProgressChanged;
        }


        #region "Gestion PJ"
        // Aperçu de la pj ajoutée à la liste
        private void I_Email_Pj_List_DoubleClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(L_Pj.SelectedValue.ToString());
        }

        /// Supprimer une PJ
        private void I_Email_Pj_List_KeyDown(object sender, KeyEventArgs e)
        {
            if(L_Pj.SelectedIndex != -1)
            {
                if (e.Key == Key.Delete || e.Key == Key.Back)
                {
                    try
                    {
                        FileInfo fi = (FileInfo)L_Pj.SelectedItem;
                        email.Pj.Remove(fi);
                        // Mettre à jour la listBox
                        L_Pj.ClearValue(ItemsControl.ItemsSourceProperty);
                        L_Pj.ItemsSource = email.Pj;
                    }
                    catch(Exception e2)
                    {
                        MessageBox.Show(e2.Message);
                    }  
                }
            }            
        }


        private async Task ajoutPJ()
        {
            // Ouverture d'une fenêtre de saisie
            OpenFileDialog vOpenFileDialog = new OpenFileDialog();
            vOpenFileDialog.RestoreDirectory = true;
            vOpenFileDialog.FilterIndex = 1;
            if (asyncProcessRunning == false)
            {
                PG_ProgressBar.Visibility = Visibility.Visible;
                PG_ProgressBar.IsIndeterminate = true;
                if (vOpenFileDialog.ShowDialog() == true)
                {         
                    await Task.Run(() =>
                    {
                        asyncProcessRunning = true;
                        FileInfo vNouvellePj = new FileInfo(vOpenFileDialog.FileName);
                        if (email.Pj.Contains(vNouvellePj) == false)
                        {
                        // Enregistrement du fichier dans l'objet Email
                        email.Pj.Add(vNouvellePj);
                        //processPG();                      
                    }
                    });
                    PG_ProgressBar.Visibility = Visibility.Hidden;
                    PG_ProgressBar.IsIndeterminate = false;
                    asyncProcessRunning = false;
                    // Rafraîchir la listBox
                    L_Pj.ItemsSource = null;
                    L_Pj.ItemsSource = email.Pj;
                }
                else
                {
                    PG_ProgressBar.Visibility = Visibility.Hidden;
                    PG_ProgressBar.IsIndeterminate = false;
                }
            }
        }
        #endregion
        #region "Gestion Signature"

        private void ajoutSignature()
        {
            // Ouverture d'une fenêtre de saisie
            OpenFileDialog vOpenFileDialog = new OpenFileDialog();
            vOpenFileDialog.RestoreDirectory = true;
            vOpenFileDialog.FilterIndex = 1;
            vOpenFileDialog.Filter = "html files (*.html)|*.html";

            if (vOpenFileDialog.ShowDialog() == true)
            {
                FileInfo vSignatureFic = new FileInfo(vOpenFileDialog.FileName);
                // Enregistrement du fichier dans l'objet Email
                cEmail_Signature vSignature = new cEmail_Signature(vSignatureFic.FullName);// TO DO : gérer l'enregistrement du fichier de signature dans un répertoire donné en fonction du compte utilisateur
                email.Signature = vSignature;
                // TO DO : gérer l'affichage de la signature 
                //processPG();

            }
        }
        #endregion
        #region "Gestion Auto complétion"

        #endregion
        #region "Gestion éditeur texte"

        private void CB_BodyFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CB_BodyFontFamily.SelectedItem != null)
                I_Body.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, CB_BodyFontFamily.SelectedItem);
        }


        private void CB_BodyFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CB_BodyFontSize.Text != "")
                I_Body.BorderBrush = Brushes.LightGray;
                I_Body.Selection.ApplyPropertyValue(Inline.FontSizeProperty, double.Parse(CB_BodyFontSize.Text));
        }
        /// Récupérer la sélection de l'utilisateur et mettre à jour les controles editeur txt
        private void I_Body_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Récupérer la largeur de police de la sélection
            Object obj = I_Body.Selection.GetPropertyValue(Inline.FontWeightProperty);
            if (obj != DependencyProperty.UnsetValue && obj.Equals(FontWeights.Bold))
                BT_BodyBold.IsChecked = true;


            // Si la sélection est en italique
            obj = I_Body.Selection.GetPropertyValue(Inline.FontStyleProperty);
            if (obj != DependencyProperty.UnsetValue && obj.Equals(FontStyles.Italic))
                BT_BodyItalic.IsChecked = true;
           // I_Body.Selection.ApplyPropertyValue(FontWeightProperty, FontWeights.Bold);


            // Si la sélection est soulignée
            obj = I_Body.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            if (obj != DependencyProperty.UnsetValue && obj.Equals(TextDecorations.Underline))
                BT_BodyUnderline.IsChecked = true;


            obj = I_Body.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            CB_BodyFontFamily.SelectedItem = obj;

            obj = I_Body.Selection.GetPropertyValue(Inline.FontSizeProperty);
            CB_BodyFontSize.Text = obj.ToString();
        }

        // Récupère les données saisies pour l'envoi du mail
        private void setInfoMail()
        {
            if (email != null)
            {
                if (I_A.Text != "")
                {
                    email.StringToTo(I_A.Text);
                }
                if (I_De.Text != "")
                {
                    email.From = I_De.Text;
                }
                if (I_Cc.Text != "")
                {
                    email.StringToCC(I_Cc.Text);
                }
                if (I_Objet.Text != "")
                {
                    email.Object = I_Objet.Text;
                }
                if (getBodyText() != "\r\n")
                {
                    email.Body.Text = getBodyText();
                }
            }
        }

        // Affiche une fenêtre d'aide et d'information sur le logiciel
        private void showHelp()
        {
            MessageBox.Show("Version actuelle de l'application : " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\n\n Documentation : " + Application.Current.FindResource("helpHttpAddress").ToString(), Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Ouvre une fenetre d'administration des comptes serveur
        private void showComptesServeur()
        {
            if (currentCompteMessagerie.isAuthenticated())
            {
                Comptes fenComptes = new Comptes();
                fenComptes.currentCompteMessagerie = currentCompteMessagerie;
                fenComptes.Show();
            }
        }

        #endregion
        #region "Evènements"

        private async void M_EmailEnvoyer_Click(object sender, RoutedEventArgs e)
        {
            await envoyer();
        }

        private void BT_Quitter_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult = false;
            this.Close();
        }

        private void M_EmailSignature_Click(object sender, RoutedEventArgs e)
        {
            ajoutSignature();
        }
        private async void M_EmailPj_Click(object sender, RoutedEventArgs e)
        {
            await ajoutPJ();
        }
        // Déclenche un évènement de progression à intervalle régulier
        void PG_PjProgress(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                // Toutes les 100 ms déclencher l'évènement ReportProgress de la classe BackgroundWork, à partir du moment où cet évènement courant est lancé
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(this.timeTimer);
            }               
        }

        // Met à jour une progress bar ou un autre controle de progression
        void PG_PjProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PG_ProgressBar.Maximum = 100;
            PG_ProgressBar.Value = e.ProgressPercentage;
            if (e.ProgressPercentage == 99)
            {
                PG_ProgressBar.Value = 0;
            }
            
        }

        private void I_Body_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Ajouter le texte dans le mail
            email.Body.Text = getBodyText();
        }

        private void BT_CurrentEmailA_Click(object sender, RoutedEventArgs e)
        {
            // TO DO
        }

        private void BT_CurrentEmailCc_Click(object sender, RoutedEventArgs e)
        {
            // TO DO
        }

        private void M_Aide_Click(object sender, RoutedEventArgs e)
        {
            showHelp();
        }

        private void M_Compte_Click(object sender, RoutedEventArgs e)
        {
            showComptesServeur();
        }

        private void M_BoiteMail_Click(object sender, RoutedEventArgs e)
        {
            int vCount = 0;
            foreach (Window vFen in Application.Current.Windows)
            {
                if (vFen.GetType().Equals(typeof(Inbox)))
                {
                    vFen.Focus();
                    Application.Current.MainWindow = vFen;
                    vCount = vCount + 1;
                }
            }
            if (vCount == 0)
            {
                if (currentCompteMessagerie.isAuthenticated())
                {
                    Inbox fenInbox = new Inbox();
                    fenInbox.currentCompteMessagerie = currentCompteMessagerie;
                    Application.Current.MainWindow = fenInbox;
                    fenInbox.Show();
                }
            }
        }

        #endregion
    }

}


