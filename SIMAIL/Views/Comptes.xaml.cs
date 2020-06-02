using SIMAIL.Classes.Fichiers;
using SIMAIL.Classes.Utilisateur;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
    /// Logique d'interaction pour Comptes.xaml
    /// </summary>
    public partial class Comptes : Window
    {
        public CompteMessagerie currentCompteMessagerie { get; set; }
        private Collection<TextBox> _ChampsObligatoiresFournisseur;

        List<Object> gListComptesServ = new List<Object>();

        String gMsgAucunCpt = "Aucun compte serveur actuellement sur votre machine!";


        public Comptes()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (currentCompteMessagerie.Equals(null))
            {
                MessageBox.Show("Connexion perdue !");
                Connexion fenConnexion = new Connexion();
                this.Close();
                Application.Current.MainWindow.Close();
                Application.Current.MainWindow = fenConnexion;
                fenConnexion.Show();
            }
            loadComptesServeur();
            loadCompteUtilisateurInfo();

            // Champs obligatoires
            _ChampsObligatoiresFournisseur = new Collection<TextBox>();
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_Fournisseur);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_AdresseIMAP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_PortIMAP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_AdresseSMTP);
            _ChampsObligatoiresFournisseur.Add(this.I_CptMessagerie_PortSMTP);

            // Méthodes de connexion
            List<MethConnexion> methConnexionList = new List<MethConnexion>();
            I_CptMessagerie_ModeCnx.ItemsSource = methConnexionList;
            methConnexionList.Add(MethConnexion.Identifiants);
            //methConnexionList.Add(MethConnexion.OAuth2);
            //methConnexionList.Add(MethConnexion.OpenID);

        }

        private void loadCompteUtilisateurInfo()
        {
            if (currentCompteMessagerie != null & currentCompteMessagerie.isAuthenticated())
            {
                L_compteEmail.Content = currentCompteMessagerie.Login;
            }
        }

        private void loadComptesServeur()
        {
            if (currentCompteMessagerie != null)
            {
                if (currentCompteMessagerie.isAuthenticated())
                {
                    try
                    {
                        if (Directory.Exists(getPathFolder()))
                        {
                            clearListe();
                            String vPath = getPathFolder();
                            foreach (string vFile in Directory.GetFiles(vPath)) {
                                CompteServeur cs = new CompteServeur();
                                cs = (CompteServeur)deserializeFromXml.getData(cs, vFile);
                                gListComptesServ.Add(cs);
                                if (gListComptesServ.Contains(gMsgAucunCpt))
                                {
                                    gListComptesServ.Remove(gMsgAucunCpt);
                                }
                            }                              
                            if (gListComptesServ.Count == 0)
                            {
                                gListComptesServ.Add(gMsgAucunCpt);
                            }
                            IT_CompteServeur.ItemsSource = gListComptesServ;
                        }
                        else
                        {
                            MessageBox.Show("Répertoire des comptes serveur introuvable.");
                        }
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }
            }
        }

        private void supprimerCompteServeur(CompteServeur pCptServ)
        {
            MessageBoxResult res =  MessageBox.Show("Etes vous sûr de vouloir supprimer le compte " + pCptServ.Fournisseur + " ?", "SIMAIL", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(res.Equals(MessageBoxResult.Yes)) {
                if (currentCompteMessagerie != null & currentCompteMessagerie.isAuthenticated())
                {
                    if (IT_CompteServeur.SelectedIndex != -1)
                    {
                        if (IT_CompteServeur.SelectedItem.GetType().Equals(typeof(CompteServeur)))
                        {
                            if (IT_CompteServeur.SelectedItem.Equals(pCptServ))
                            {
                                if (Directory.Exists(getPathFolder()))
                                {
                                    String vPath = getPathFolder();
                                    foreach (String vFile in Directory.GetFiles(vPath))
                                    {
                                        FileInfo f = new FileInfo(vFile);
                                        if (f.Name.Equals(pCptServ.Fournisseur + ".xml"))
                                        {
                                            f.Delete();
                                            gListComptesServ.Remove(pCptServ);
                                            IT_CompteServeur.ItemsSource = null;
                                            IT_CompteServeur.ItemsSource = gListComptesServ;
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Répertoire des comptes serveur introuvable.");
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Veuillez sélectionner un compte de connexion dans la liste ci dessus ! ");
                    }
                }
            }
          
        }

        // Retourne le chemin du répertoire choisi pour les comptes serveurs
        private string getPathFolder()
        {
            String vPathCompteServ = "";
            if (Application.Current.TryFindResource("pathCompteServ") != null)
            {
                vPathCompteServ = Application.Current.FindResource("pathCompteServ").ToString();
            }
            else
            {
                vPathCompteServ = Application.Current.FindResource("pathDefaultCompteServ").ToString();
            }
            return vPathCompteServ;
        }

        private void showSelectedCompteServeur()
        {
            if (currentCompteMessagerie != null & currentCompteMessagerie.isAuthenticated())
            {
                if(IT_CompteServeur.SelectedIndex != -1)
                {
                    if (IT_CompteServeur.SelectedItem.GetType().Equals(typeof(CompteServeur)))
                    {
                        CompteServeur cs = (CompteServeur)IT_CompteServeur.SelectedItem;
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
                        //if (cs.ChiffrementSMTP == CompteServeur.Chiffrement.TLS)
                        //{
                        //    I_ChiffrementSMTPTLS.IsChecked = true;
                        //}
                        if (cs.ChiffrementIMAP == CompteServeur.Chiffrement.SSL)
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

                    }
                }
            }
        }

        private void enregistrerFournisseur()
        {
            if (checkMandatoryFieldsFournisseur())
            {
                CompteServeur cs = new CompteServeur();
                cs.Fournisseur = I_CptMessagerie_Fournisseur.Text;
                cs.methodeConnexion = (CompteServeur.MethConnexion)I_CptMessagerie_ModeCnx.SelectedItem;
                // IMAP
                cs.AdresseIMAP = I_CptMessagerie_AdresseIMAP.Text;
                cs.PortIMAP = int.Parse(I_CptMessagerie_PortIMAP.Text);
                if (I_ChiffrementIMAPAucun.IsChecked ?? false)
                {
                    cs.ChiffrementIMAP = CompteServeur.Chiffrement.Aucun;
                }
                if (I_ChiffrementIMAPSSL.IsChecked ?? false)
                {
                    cs.ChiffrementIMAP = CompteServeur.Chiffrement.SSL;
                }
                if (I_ChiffrementIMAPTLS.IsChecked ?? false)
                {
                    cs.ChiffrementIMAP = CompteServeur.Chiffrement.TLS;
                }
                // SMTP
                cs.AdresseSMTP = I_CptMessagerie_AdresseSMTP.Text;
                cs.PortSMTP = int.Parse(I_CptMessagerie_PortSMTP.Text);
                if (I_ChiffrementSMTPAucun.IsChecked ?? false)
                {
                    cs.ChiffrementSMTP = CompteServeur.Chiffrement.Aucun;
                }
                if (I_ChiffrementSMTPSSL.IsChecked ?? false)
                {
                    cs.ChiffrementSMTP = CompteServeur.Chiffrement.SSL;
                }
                //if (I_ChiffrementSMTPTLS.IsChecked ?? false)
                //{
                //    cs.ChiffrementSMTP = CompteServeur.Chiffrement.TLS;
                //}

                // Sauvegarde dans un fichier
                cs.saveDefaultDirectory = getPathFolder();
                cs.save();
                loadComptesServeur();

                MessageBoxResult res = MessageBox.Show("Enregistrement effectué. Un redémarrage est nécessaire. \n\n Souhaitez vous redémarrer l'application ?", Assembly.GetExecutingAssembly().GetName().Name, MessageBoxButton.YesNo);
                switch(res)
                {
                    case MessageBoxResult.Yes:
                        this.Close();
                        try
                        {
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                        break;
                }
            }
            else
            {
                MessageBox.Show("Veuillez renseigner les informations du serveur de messagerie correctement.");
            }
        }

        //Renvoie vrai si tous les champs obligatoires sont renseignés
        private bool checkMandatoryFieldsFournisseur()
        {
            bool vFieldsOK = true;
            foreach (TextBox vChamp in _ChampsObligatoiresFournisseur)
            {
                if (vChamp.Text == "")
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


        private void clearCompteServeurSaisie()
        {
            I_CptMessagerie_Fournisseur.Text = "";
            I_CptMessagerie_PortIMAP.Text = "";
            I_CptMessagerie_AdresseIMAP.Text = "";
            I_CptMessagerie_AdresseSMTP.Text = "";
            I_CptMessagerie_PortSMTP.Text = "";
            I_CptMessagerie_Fournisseur_LostFocus(null, null);
            I_CptMessagerie_PortIMAP_LostFocus(null, null);
            I_CptMessagerie_PortSMTP_LostFocus(null, null);
            I_CptMessagerie_AdresseIMAP_LostFocus(null, null);
            I_CptMessagerie_AdresseSMTP_LostFocus(null, null);
            I_ChiffrementIMAPAucun.IsChecked = true;
            I_ChiffrementSMTPAucun.IsChecked = true;
            I_CptMessagerie_ModeCnx.SelectedIndex = -1;
        }

        private void clearListe()
        {
            IT_CompteServeur.ItemsSource = null;
            IT_CompteServeur.Items.Clear();
            IT_CompteServeur.ClearValue(ItemsControl.ItemsSourceProperty);
            gListComptesServ.Clear();
            IT_CompteServeur.ItemsSource = null;
        }

        #region "Evènements"


        private void I_CptMessagerie_Fournisseur_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_Fournisseur_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_AdresseIMAP_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_AdresseIMAP_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_PortIMAP_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_PortIMAP_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_AdresseSMTP_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_AdresseSMTP_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_PortSMTP_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_PortSMTP_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void I_CptMessagerie_ModeCnx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BT_FournisseurSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (IT_CompteServeur.SelectedIndex != -1)
            {

                try
                {
                    CompteServeur cs = (CompteServeur)IT_CompteServeur.SelectedItem;
                    supprimerCompteServeur(cs);
                }
                catch (Exception e2)
                {
                    MessageBox.Show(e2.Message);
                }
            }
        }

        private void I_CptMessagerie_PortIMAP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void I_CptMessagerie_PortSMTP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void IT_CompteServeur_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Delete))
            {
                if (IT_CompteServeur.SelectedIndex != -1)
                {
                    try
                    {
                        CompteServeur cs = (CompteServeur)IT_CompteServeur.SelectedItem;
                        supprimerCompteServeur(cs);
                    }
                    catch (Exception e2)
                    {
                        MessageBox.Show(e2.Message);
                    }
                }
            }
        }

        private void BT_Rafraîchir_Click(object sender, RoutedEventArgs e)
        {
            clearCompteServeurSaisie();
            loadComptesServeur();
        }

        private void IT_CompteServeur_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            clearCompteServeurSaisie();
            showSelectedCompteServeur();
        }
        private void BT_FournisseurEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            enregistrerFournisseur();
        }


        #endregion


    }
}
