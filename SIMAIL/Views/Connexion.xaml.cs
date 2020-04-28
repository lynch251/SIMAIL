using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Security;
using SIMAIL.Classes.Fichiers;
using SIMAIL.Classes.Utilisateur;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SIMAIL.Classes.Utilisateur.CompteServeur;

namespace SIMAIL.Views
{
    /// <summary>
    /// Logique d'interaction pour Connexion.xaml
    /// </summary>
    public partial class Connexion : Window
    {
        /// <summary>
        /// Liste des fournisseurs à charger dans la combobox
        /// </summary>
        private List<CompteServeur> ListeFournisseurs;
        public CompteMessagerie currentCompteMessagerie;
        private CompteServeur currentCompteServeur;
        private Collection<TextBox> _ChampsObligatoiresFournisseur;

        String gLoginText = "Adresse-Email";
        String gFournisseurText = "Fournisseur";
        String gAdresseIMAPText = "Adresse IMAP Serveur";
        String gPortIMAPText = "Port IMAP";
        String gAdresseSMTPText = "Adresse SMTP Serveur";
        String gPortSMTPText = "Port SMTP";
        CompteServeur gNouveauCptServ = new CompteServeur("Nouveau");
        bool asyncProcessRunning = false;


        public Connexion()
        {
            InitializeComponent();
            SIMAIL.Classes.Utilisateur.CompteMessagerie c = new Classes.Utilisateur.CompteMessagerie();

            //Affichage
            IG_Connexion_Param.Visibility = Visibility.Hidden;
            I_Connexion_Login.Text = gLoginText;
            I_Connexion_Login.Foreground = Brushes.Gray;
            I_CptMessagerie_PortIMAP.Text = gPortIMAPText;
            I_CptMessagerie_PortIMAP.Foreground = Brushes.Gray;
            I_CptMessagerie_AdresseIMAP.Text = gAdresseIMAPText;
            I_CptMessagerie_AdresseIMAP.Foreground = Brushes.Gray;
            I_CptMessagerie_Fournisseur.Text = gFournisseurText;
            I_CptMessagerie_Fournisseur.Foreground = Brushes.Gray;
            I_CptMessagerie_AdresseSMTP.Text = gAdresseSMTPText;
            I_CptMessagerie_AdresseSMTP.Foreground = Brushes.Gray;
            I_CptMessagerie_PortSMTP.Text = gPortSMTPText;
            I_CptMessagerie_PortSMTP.Foreground = Brushes.Gray;

            List<MethConnexion> methConnexionList = new List<MethConnexion>();
            I_CptMessagerie_ModeCnx.ItemsSource = methConnexionList;
            methConnexionList.Add(MethConnexion.Identifiants);
            methConnexionList.Add(MethConnexion.OAuth2);
            methConnexionList.Add(MethConnexion.OpenID);            

            // Fournisseurs
            ListeFournisseurs = new List<CompteServeur>();
            currentCompteServeur = new CompteServeur();

            // Instance du compte messagerie
            currentCompteMessagerie = CompteMessagerie.Instance();

            I_Connexion_Fournisseur.SelectedIndex = -1;
            I_Connexion_Fournisseur.Items.Clear();
            I_Connexion_Fournisseur.ItemsSource = ListeFournisseurs;

            // Champs obligatoires
            _ChampsObligatoiresFournisseur = new Collection<TextBox>();
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_Fournisseur);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_AdresseIMAP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_PortIMAP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_AdresseSMTP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_PortSMTP);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                currentCompteMessagerie.compteServDefaultDirectory = this.FindResource("pathDefaultCompteServ").ToString();
            }
            catch(Exception e2)
            {
                MessageBox.Show(e2.Message);
            }
            getFournisseurs();
        }

        #region "Gestion Fenetre"
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        #region "Compte Serveur"
        private void clearCompteServeurSaisie()
        {
            I_CptMessagerie_Fournisseur.Text = "";
            I_CptMessagerie_PortIMAP.Text = "";
            I_CptMessagerie_AdresseIMAP.Text = "";
            I_CptMessagerie_AdresseSMTP.Text = "";
            I_CptMessagerie_PortSMTP.Text = "";
            I_CptMessagerie_Fournisseur_LostFocus(null,null);          
            I_CptMessagerie_PortIMAP_LostFocus(null, null);
            I_CptMessagerie_PortSMTP_LostFocus(null, null);
            I_CptMessagerie_AdresseIMAP_LostFocus(null, null);
            I_CptMessagerie_AdresseSMTP_LostFocus(null, null);
            I_ChiffrementIMAPAucun.IsChecked = true;
            I_ChiffrementSMTPAucun.IsChecked = true;
            I_CptMessagerie_ModeCnx.SelectedIndex = -1;
        }

        /// <summary>
        /// Récupère les fournisseurs sauvegardés dans un fichier sur le disque (dossier SIMAIL dans le répertoire paramètré)
        /// </summary>
        public void getFournisseurs()
        {
            int nbFic = 0;
            if (currentCompteMessagerie.compteServDefaultDirectory != "")
            {
                if (Directory.Exists(currentCompteMessagerie.compteServDefaultDirectory))
                {
                    if (Directory.EnumerateFiles(currentCompteMessagerie.compteServDefaultDirectory).Count() > 0)
                    {
                        foreach (string vFile in Directory.EnumerateFiles(currentCompteMessagerie.compteServDefaultDirectory))
                        {
                            if (System.IO.Path.GetExtension(vFile) == ".xml")
                            {
                                nbFic = nbFic +1;
                                CompteServeur vCs = new CompteServeur();
                                try
                                {
                                    vCs = (CompteServeur)deserializeFromXml.getData(vCs, vFile);
                                    ListeFournisseurs.Add(vCs);
                                }
                                catch(Exception e)
                                {
                                    MessageBox.Show(e.Message);
                                }
                            }                            
                        } 
                    }
                    if (nbFic == 0)
                    {
                        MessageBox.Show("Aucune configuration de serveur présente dans " + currentCompteMessagerie.compteServDefaultDirectory + "\n\r\r\n" + "Vous pouvez ajouter un serveur de messagerie via le menu déroulant.", "SIMAIL - Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                }
                else
                {
                    MessageBox.Show("Répertoire des comptes serveurs introuvable sur votre disque.");
                }
            }
            else
            {
                MessageBox.Show("Répertoire des comptes serveurs introuvable sur votre disque.");
            }
        }

        private void enregistrerFournisseur()
        {
            if (checkMandatoryFieldsFournisseur())
            {
                currentCompteServeur = new CompteServeur();
                currentCompteServeur.Fournisseur = I_CptMessagerie_Fournisseur.Text;
                currentCompteServeur.methodeConnexion = (CompteServeur.MethConnexion)I_CptMessagerie_ModeCnx.SelectedItem;
                // IMAP
                currentCompteServeur.AdresseIMAP = I_CptMessagerie_AdresseIMAP.Text;
                currentCompteServeur.PortIMAP = int.Parse(I_CptMessagerie_PortIMAP.Text);
                if (I_ChiffrementIMAPAucun.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementIMAP = CompteServeur.Chiffrement.Aucun;
                }
                if (I_ChiffrementIMAPSSL.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementIMAP = CompteServeur.Chiffrement.SSL;
                }
                if (I_ChiffrementIMAPTLS.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementIMAP = CompteServeur.Chiffrement.TLS;
                }                
                // SMTP
                currentCompteServeur.AdresseSMTP = I_CptMessagerie_AdresseSMTP.Text;
                currentCompteServeur.PortSMTP = int.Parse(I_CptMessagerie_PortSMTP.Text);
                if (I_ChiffrementSMTPAucun.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementSMTP = CompteServeur.Chiffrement.Aucun;
                }
                if (I_ChiffrementSMTPSSL.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementSMTP = CompteServeur.Chiffrement.SSL;
                }
                if (I_ChiffrementSMTPTLS.IsChecked ?? false)
                {
                    currentCompteServeur.ChiffrementSMTP = CompteServeur.Chiffrement.TLS;
                }
                // Ajout du serveur de messagerie à la liste 
                ListeFournisseurs.Add(currentCompteServeur);
                if (ListeFournisseurs.Contains(gNouveauCptServ)) { ListeFournisseurs.Remove(gNouveauCptServ); }
                I_Connexion_Fournisseur.ItemsSource = ListeFournisseurs;
                I_Connexion_Fournisseur.Items.Refresh();
                I_Connexion_Fournisseur.SelectedItem = currentCompteServeur;

                // Dans un fichier
                currentCompteServeur.save();
            }
            else
            {
                MessageBox.Show("Veuillez renseigner les informations du serveur de messagerie correctement.");
            }
        }

        private void supprimerFournisseur()
        {
            if (currentCompteServeur != null)
            {
                MessageBoxResult res = MessageBox.Show("Voulez vous vraiment supprimer ce compte de connexion ? " + currentCompteServeur.Fournisseur, "SIMAIL", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res.Equals(MessageBoxResult.Yes))
                {
                    // Suppression du compte serveur courant
                    ListeFournisseurs.Remove(currentCompteServeur);
                    majListeFournisseurs();
                    if (ListeFournisseurs.Count == 0)
                    {
                        clearCompteServeurSaisie();
                        currentCompteServeur = null;
                        ListeFournisseurs.Add(gNouveauCptServ);
                        majListeFournisseurs();
                    }
                    else
                    {
                        currentCompteServeur = ListeFournisseurs.Last();
                        chargerCompteServeur(currentCompteServeur);
                        I_Connexion_Fournisseur.SelectedItem = currentCompteServeur;
                    }
                }  
            }
            else
            {
                MessageBox.Show("Aucun compte de connexion sélectionné !", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void chargerCompteServeur(CompteServeur cs)
        {
            clearCompteServeurSaisie();
            if (cs != null & ListeFournisseurs.Contains(cs))
            {
                I_CptMessagerie_Fournisseur.Text = cs.Fournisseur;
                I_CptMessagerie_ModeCnx.SelectedItem = cs.methodeConnexion;
                I_CptMessagerie_PortIMAP.Text = cs.PortIMAP.ToString();
                I_CptMessagerie_AdresseIMAP.Text = cs.AdresseIMAP;
                I_CptMessagerie_PortSMTP.Text = cs.PortSMTP.ToString();
                I_CptMessagerie_AdresseSMTP.Text = cs.AdresseSMTP;
                if (cs.ChiffrementSMTP == CompteServeur.Chiffrement.Aucun)
                {
                    I_ChiffrementSMTPAucun.IsChecked = true;
                }
                if (cs.ChiffrementSMTP == CompteServeur.Chiffrement.SSL)
                {
                    I_ChiffrementSMTPSSL.IsChecked = true;
                }
                if (cs.ChiffrementSMTP == CompteServeur.Chiffrement.TLS)
                {
                    I_ChiffrementSMTPTLS.IsChecked = true;
                }
                if(cs.ChiffrementIMAP == CompteServeur.Chiffrement.SSL)
                {
                    I_ChiffrementIMAPSSL.IsChecked = true;
                }
                if (cs.ChiffrementIMAP == CompteServeur.Chiffrement.TLS)
                {
                    I_ChiffrementIMAPTLS.IsChecked = true;
                }
                if (cs.ChiffrementIMAP == CompteServeur.Chiffrement.Aucun)
                {
                    I_ChiffrementIMAPAucun.IsChecked = true;
                }
                
                // Le compte serveur devient le compte serveur courant.
                currentCompteServeur = cs;
            }
            else
            {
                MessageBox.Show("Erreur de chargement...");
            }
        }

        private void majListeFournisseurs()
        {
            I_Connexion_Fournisseur.ItemsSource = ListeFournisseurs;
            I_Connexion_Fournisseur.Items.Refresh();
        }

        //Renvoie vrai si tous les champs obligatoires sont renseignés
        private bool checkMandatoryFieldsFournisseur()
        {
            bool vFieldsOK = true;
            foreach (TextBox vChamp in _ChampsObligatoiresFournisseur)
            {
                if (vChamp.Text == gAdresseIMAPText || vChamp.Text == gAdresseSMTPText || vChamp.Text == gFournisseurText || vChamp.Text == gLoginText || vChamp.Text == gPortIMAPText || vChamp.Text == gPortSMTPText)
                {
                    vChamp.BorderBrush = Brushes.Red;
                    vFieldsOK = false;
                }
            }
            if (I_CptMessagerie_ModeCnx.SelectedIndex == -1)
            {
                vFieldsOK = false;
                I_CptMessagerie_ModeCnx.BorderBrush = Brushes.Red;
            }
            return vFieldsOK;
        }

        // Connexion OAuth2 vers une messagerie gmail
        private async void connexionGmail()
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = "294403304718-bptp75nbk64qi2klfvjidqvklmcqp9vm.apps.googleusercontent.com",
                ClientSecret = "0PuD1zj5uq62HvWZx1t9RrKc"
            };

            var codeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                DataStore = new FileDataStore("CredentialCacheFolder", false),
                Scopes = new[] { "https://mail.google.com/" },
                ClientSecrets = clientSecrets
            });

            var codeReceiver = new LocalServerCodeReceiver();
            var authCode = new AuthorizationCodeInstalledApp(codeFlow, codeReceiver);
            var credential = await authCode.AuthorizeAsync(I_Connexion_Login.Text, CancellationToken.None);

            if (authCode.ShouldRequestAuthorizationCode(credential.Token))
                await credential.RefreshTokenAsync(CancellationToken.None);

            var oauth2 = new SaslMechanismOAuth2(credential.UserId, credential.Token.AccessToken);

            using (var client = new ImapClient())
            {
                await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect); // non paramètrable
                await client.AuthenticateAsync(oauth2);
                await client.DisconnectAsync(true);
            }
            
        }

        #endregion

        #region "Compte Messagerie"
        private async void creerCompteMessagerie()
        {
            // Création du compte messagerie (tentative)
            if (currentCompteServeur != null)
            {
                switch (currentCompteServeur.methodeConnexion)
                {
                    case MethConnexion.Identifiants:
                        if (currentCompteServeur.isValid() & I_Connexion_Login.Text != "" & I_Connexion_Pass.Password != "")
                        {
                            currentCompteMessagerie.compteServeur = currentCompteServeur;
                            currentCompteMessagerie.Identifiant = I_Connexion_Login.Text;
                            currentCompteMessagerie.Mdp = I_Connexion_Pass.Password;
                            this.Cursor = Cursors.Wait;
                            IG_Connexion_Param.Visibility = Visibility.Hidden;
                            PG_Mailbox.Visibility = Visibility.Visible;
                            PG_Mailbox.IsIndeterminate = true;
                            asyncProcessRunning = true;
                            if (await currentCompteMessagerie.Authenticate() != null)
                            {
                                PG_Mailbox.Visibility = Visibility.Hidden;
                                PG_Mailbox.IsIndeterminate = false;
                                asyncProcessRunning = false;

                                Inbox fenInbox = new Inbox();
                                Application.Current.MainWindow = fenInbox;
                                fenInbox.currentCompteMessagerie = currentCompteMessagerie;
                                fenInbox.Show();
                                this.Cursor = Cursors.Arrow;
                                this.Close();
                            }
                            else 
                            {  
                                this.Cursor = Cursors.Arrow;
                                PG_Mailbox.Visibility = Visibility.Hidden;
                                IG_Connexion_Param.Visibility = Visibility.Visible;
                                PG_Mailbox.IsIndeterminate = false;
                                asyncProcessRunning = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Il manque des informations de connexion !");
                            this.Cursor = Cursors.Arrow;
                        }
                        break;

                    case MethConnexion.OAuth2:
                        if (I_Connexion_Login.Text != gLoginText & I_Connexion_Login.Text != "")
                        {
                            currentCompteMessagerie.compteServeur = currentCompteServeur;
                            currentCompteMessagerie.Identifiant = I_Connexion_Login.Text;
                            this.Cursor = Cursors.Wait;
                            PG_Mailbox.Visibility = Visibility.Visible;
                            PG_Mailbox.IsIndeterminate = true;
                            asyncProcessRunning = true;

                            if (await currentCompteMessagerie.Authenticate() != null)
                            {
                                PG_Mailbox.Visibility = Visibility.Hidden;
                                PG_Mailbox.IsIndeterminate = false;
                                asyncProcessRunning = false;

                                Inbox fenInbox = new Inbox();
                                Application.Current.MainWindow = fenInbox;
                                fenInbox.currentCompteMessagerie = currentCompteMessagerie;
                                fenInbox.Show();
                                this.Cursor = Cursors.Arrow;
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Erreur de connexion.");
                                this.Cursor = Cursors.Arrow;
                                PG_Mailbox.Visibility = Visibility.Hidden;
                                PG_Mailbox.IsIndeterminate = false;
                                asyncProcessRunning = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Veuillez saisir votre adresse email.");
                            this.Cursor = Cursors.Arrow;
                        }
                        break;
                }                
            }
            else
            {
                MessageBox.Show("Aucun compte serveur sélectionné");
                this.Cursor = Cursors.Arrow;
            }
        }

        
        #endregion
      
        #region "Evenements fenetre"
        private void BT_FournisseurEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            enregistrerFournisseur();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IG_Connexion_Param.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Afficher le volet configuration du compte de serveur
        /// </summary>
        private void I_Connexion_Fournisseur_GotFocus(object sender, RoutedEventArgs e)
        {
            IG_Connexion_Param.Visibility = Visibility.Visible;
            if (ListeFournisseurs.Contains(gNouveauCptServ) == false)
            {
                CompteServeur cs = (CompteServeur)I_Connexion_Fournisseur.SelectedItem;
                if (cs != null) { chargerCompteServeur(cs); }
            }
        }

        private void I_CptMessagerie_ModeCnx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (I_CptMessagerie_ModeCnx.SelectedIndex != -1)
            {
                I_CptMessagerie_ModeCnx.BorderBrush = Brushes.LightGray;
                // TO DO
            }
        }

        private void I_Connexion_Login_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_Connexion_Login.Text == gLoginText)
            {
                I_Connexion_Login.Text = "";
                I_Connexion_Login.Foreground = Brushes.Black;
            }
        }

        private void I_Connexion_Login_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_Connexion_Login.Text == "")
            {
                I_Connexion_Login.Text = gLoginText;
                I_Connexion_Login.Foreground = Brushes.Gray;
            }
            else
            {
                I_Connexion_Login.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_Connexion_Pass_LostFocus(object sender, RoutedEventArgs e)
        {
            // TODO
        }

        private void I_CptMessagerie_Fournisseur_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_Fournisseur.Text == "")
            {
                I_CptMessagerie_Fournisseur.Text = gFournisseurText;
                I_CptMessagerie_Fournisseur.Foreground = Brushes.Gray;
            }
            else
            {
                I_CptMessagerie_Fournisseur.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_CptMessagerie_Fournisseur_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_Fournisseur.Text == gFournisseurText)
            {
                I_CptMessagerie_Fournisseur.Text = "";
                I_CptMessagerie_Fournisseur.Foreground = Brushes.Black;
            }
        }

        private void I_CptMessagerie_AdresseIMAP_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_AdresseIMAP.Text == "")
            {
                I_CptMessagerie_AdresseIMAP.Text = gAdresseIMAPText;
                I_CptMessagerie_AdresseIMAP.Foreground = Brushes.Gray;
            }
            else
            {
                I_CptMessagerie_AdresseIMAP.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_CptMessagerie_AdresseIMAP_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_AdresseIMAP.Text == gAdresseIMAPText)
            {
                I_CptMessagerie_AdresseIMAP.Text = "";
                I_CptMessagerie_AdresseIMAP.Foreground = Brushes.Black;
            }
        }

        private void I_CptMessagerie_PortIMAP_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_PortIMAP.Text == "")
            {
                I_CptMessagerie_PortIMAP.Text = gPortIMAPText;
                I_CptMessagerie_PortIMAP.Foreground = Brushes.Gray;
            }
            else
            {
                I_CptMessagerie_PortIMAP.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_CptMessagerie_PortIMAP_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_PortIMAP.Text == gPortIMAPText)
            {
                I_CptMessagerie_PortIMAP.Text = "";
                I_CptMessagerie_PortIMAP.Foreground = Brushes.Black;
            }
        }

        private void I_CptMessagerie_AdresseSMTP_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_AdresseSMTP.Text == "")
            {
                I_CptMessagerie_AdresseSMTP.Text = gAdresseSMTPText;
                I_CptMessagerie_AdresseSMTP.Foreground = Brushes.Gray;
            }
            else
            {
                I_CptMessagerie_AdresseSMTP.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_CptMessagerie_AdresseSMTP_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_AdresseSMTP.Text == gAdresseSMTPText)
            {
                I_CptMessagerie_AdresseSMTP.Text = "";
                I_CptMessagerie_AdresseSMTP.Foreground = Brushes.Black;
            }
        }

        private void I_CptMessagerie_PortSMTP_LostFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_PortSMTP.Text == "")
            {
                I_CptMessagerie_PortSMTP.Text = gPortSMTPText;
                I_CptMessagerie_PortSMTP.Foreground = Brushes.Gray;
            }
            else
            {
                I_CptMessagerie_PortSMTP.BorderBrush = Brushes.LightGray;
            }
        }

        private void I_CptMessagerie_PortSMTP_GotFocus(object sender, RoutedEventArgs e)
        {
            if (I_CptMessagerie_PortSMTP.Text == gPortSMTPText)
            {
                I_CptMessagerie_PortSMTP.Text = "";
                I_CptMessagerie_PortSMTP.Foreground = Brushes.Black;
            }
        }

        private void BT_Connexion_Click(object sender, RoutedEventArgs e)
        {
            if (currentCompteServeur != null)
            {
                currentCompteServeur.methodeConnexion = MethConnexion.Identifiants;
                creerCompteMessagerie();
            }
            else
            {
                MessageBox.Show("Aucun compte serveur sélectionné");
                this.Cursor = Cursors.Arrow;
            }           
        }

        private void BT_FournisseurSupprimer_Click(object sender, RoutedEventArgs e)
        {
            supprimerFournisseur();
        }

        // Appuyer sur entrée une fois le mot de passe saisi déclenche le bouton de connexion
        private void I_Connexion_Pass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter & I_Connexion_Pass.Password != "")
            {
                BT_Connexion.Focus();
                this.Cursor = Cursors.Wait;
                BT_Connexion_Click(null, null);
            }
        }

        private void BT_Connexion_Gmail_Click(object sender, RoutedEventArgs e)
        {
            currentCompteServeur = new CompteServeur();
            currentCompteServeur.methodeConnexion = MethConnexion.OAuth2;
            creerCompteMessagerie();
        }

        #endregion


    }
}
