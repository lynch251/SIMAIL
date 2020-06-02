using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using MimeKit.Text;
using SIMAIL.Classes.Email;
using SIMAIL.Classes.Utilisateur;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

namespace SIMAIL.Views
{
    /// <summary>
    /// Logique d'interaction pour Inbox.xaml
    /// </summary>
    public partial class Inbox : Window
    {
        public enum InboxMode
        {
            Reponse,
            Reception,
            Transfert,
            Nouveau
        }
        public CompteMessagerie currentCompteMessagerie;
        public ImapClient currentClient;
        public int NbMailToLoad { get; set; }
        public InboxMode inboxMode { get; set; }
        private System.Windows.Threading.DispatcherTimer timerInboxReload;

                
        List<Object> gEmailList = new List<Object>();
        MailShort gCurrentMailSummary;
        MimeMessage gCurrentMail;
        bool asyncProcessRunning = false;

        public Inbox()
        {
            InitializeComponent();
            
        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            if (currentCompteMessagerie != null & currentCompteMessagerie.isAuthenticated())
            {
                NbMailToLoad = 25;
                hideMailControls();
                await openInbox();

                //lancement du rafraîchissement régulier
                currentCompteMessagerie.timerInboxReload = 1; // une minute -> à paramétrer
                timerInboxReload = new System.Windows.Threading.DispatcherTimer();
                timerInboxReload.Interval = TimeSpan.FromMinutes(currentCompteMessagerie.timerInboxReload);
                timerInboxReload.Tick += timer_Tick;
                timerInboxReload.Start();
            }
            else
            {
                MessageBox.Show("Connexion perdue !");
                Connexion fenConnexion = new Connexion();
                fenConnexion.Show();
                closeInbox(); 
              
            } 
        }

        #region "Gestion Boîte"
        public async Task<Boolean> openInbox()
        {             
            if (currentCompteMessagerie.isAuthenticated())
            {
                // Ouverture de la boite mail
                // Mode réception à l'ouverture de l'inbox
                currentClient = await currentCompteMessagerie.getIMAPConnection();
                inboxMode = InboxMode.Reception;
                currentClient.Inbox.Open(FolderAccess.ReadWrite);

                await getMailInbox();
            }
            else
            {
                MessageBox.Show("Aucune authentification !");
            }
            return false;
        }

        private void closeInbox()
        {
            timerInboxReload.Stop();
            this.Close();
        }

        async void timer_Tick(object sender, EventArgs e)
        {
            await getMailInbox();
        }

        /// <summary>
        /// Récupère les currentInbox.NbMail premiers mails de la boite mail
        /// </summary>
        private async Task getMailInbox()
        {
            if (asyncProcessRunning == false)
            {
                PG_Mailbox.Visibility = Visibility.Visible;
                PG_Mailbox.IsIndeterminate = true;

                if (currentCompteMessagerie.isAuthenticated())
                {
                    clearInboxMessages();
                    await Task.Run(() =>
                    {
                        asyncProcessRunning = true;
                        int nbMailsToLoad = Math.Max(currentClient.Inbox.Count - this.currentCompteMessagerie.nbMailToLoad, 0); // entier le plus grand des deux
                    var InboxMails = currentClient.Inbox.Fetch(nbMailsToLoad, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags);
                        foreach (var item in InboxMails)
                        {
                            // Récupérer l'enveloppe de chaque mail
                            String sujet = item.Envelope.Subject;
                            UniqueId Uid = item.UniqueId;
                            int id = item.Index;
                            String date = item.Envelope.Date.Value.Date.ToShortDateString();
                            if(item.Envelope.Date.Value.Date == DateTime.Now.Date)
                            {
                                date = "Aujourd'hui";
                            }
                            String time = item.Envelope.Date.Value.DateTime.ToLocalTime().ToShortTimeString();
                            String from = "";
                            MessageFlags flags = item.Flags.Value;
                            foreach (InternetAddress vAdress in item.Envelope.From)
                            {
                                if (vAdress.Name != null)
                                {
                                    from = vAdress.Name.ToString();
                                }
                                else
                                {
                                    from = vAdress.ToString();
                                }
                            }
                            gEmailList.Add(new MailShort(Uid) { MailShortDate = date, MailShortText = from + "\r" + sujet, MailShortHour = time, MailShortId = id, MailShortFlag = flags });
                        }
                    });
                    asyncProcessRunning = false;
                    IT_InboxMessages.ItemsSource = gEmailList;
                    I_InboxSearch.Text = "";
                    sortInbox(ListSortDirection.Descending);

                }
                PG_Mailbox.IsIndeterminate = false;
                PG_Mailbox.Visibility = Visibility.Hidden;
            }
        }

        // Rechercher dans la boite de messagerie
        private async Task searchInbox()
        {
            if (asyncProcessRunning == false)
            {
                PG_Mailbox.Visibility = Visibility.Visible;
                PG_Mailbox.IsIndeterminate = true;

                clearInboxMessages();
                I_SearchDate.SelectedDate = null;
                if (currentCompteMessagerie.isAuthenticated() & I_InboxSearch.Text != "")
                {

                    String search = I_InboxSearch.Text;
                    await Task.Run(() =>
                    {
                        asyncProcessRunning = true;
                        var query = SearchQuery.SubjectContains(search);
                        var uids = currentClient.Inbox.Search(query);
                        var InboxMails = currentClient.Inbox.Fetch(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure | MessageSummaryItems.Flags);
                        foreach (var item in InboxMails)
                        {
                            if (item.Envelope.MessageId != null)
                            {
                                String sujet = item.Envelope.Subject;
                                UniqueId Uid = item.UniqueId;
                                int id = item.Index;
                                String date = item.Envelope.Date.Value.Date.ToShortDateString();
                                if (item.Envelope.Date.Value.Date == DateTime.Now.Date)
                                {
                                    date = "Aujourd'hui";
                                }
                                String time = item.Envelope.Date.Value.DateTime.ToLocalTime().ToShortTimeString();
                                String from = "";
                                MessageFlags flags = item.Flags.Value;
                                foreach (InternetAddress vAdress in item.Envelope.From)
                                {
                                    if (vAdress.Name != null)
                                    {
                                        from = vAdress.Name.ToString();
                                    }
                                    else
                                    {
                                        from = vAdress.ToString();
                                    }
                                }
                                gEmailList.Add(new MailShort(Uid) { MailShortDate = date, MailShortText = from + "\r" + sujet, MailShortHour = time, MailShortId = id, MailShortFlag = flags });
                            }
                        }
                    });
                    asyncProcessRunning = false;              
                    IT_InboxMessages.ItemsSource = gEmailList;
                    sortInbox(ListSortDirection.Descending);
                }
                else
                {
                    await getMailInbox(); // on recharge la boite mail par défaut.
                }

                PG_Mailbox.IsIndeterminate = false;
                PG_Mailbox.Visibility = Visibility.Hidden;
            }
        }

        // Rechercher les mails reçus à une date donnée
        private async Task searchInboxByDate()
        {
            if (asyncProcessRunning == false)
            {
                PG_Mailbox.Visibility = Visibility.Visible;
                PG_Mailbox.IsIndeterminate = true;

                clearInboxMessages();
                if (currentCompteMessagerie.isAuthenticated() & I_SearchDate.SelectedDate != null)
                {
                    DateTime search = I_SearchDate.SelectedDate.Value;
                    await Task.Run(() =>
                    {
                        asyncProcessRunning = true;
                        var query = SearchQuery.DeliveredOn(search);
                        var uids = currentClient.Inbox.Search(query);
                        var InboxMails = currentClient.Inbox.Fetch(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure | MessageSummaryItems.Flags);
                        foreach (IMessageSummary item in InboxMails)
                        {
                            if (item.Envelope.MessageId != null)
                            {
                                String sujet = item.Envelope.Subject;
                                UniqueId Uid = item.UniqueId;
                                int id = item.Index;
                                String date = item.Envelope.Date.Value.Date.ToShortDateString();
                                if (item.Envelope.Date.Value.Date == DateTime.Now.Date)
                                {
                                    date = "Aujourd'hui";
                                }
                                String time = item.Envelope.Date.Value.DateTime.ToLocalTime().ToShortTimeString();
                                String from = "";
                                MessageFlags flags = item.Flags.Value;
                                foreach (InternetAddress vAdress in item.Envelope.From)
                                {
                                    if (vAdress.Name != null)
                                    {
                                        from = vAdress.Name.ToString();
                                    }
                                    else
                                    {
                                        from = vAdress.ToString();
                                    }
                                }
                                gEmailList.Add(new MailShort(Uid) { MailShortDate = date, MailShortText = from + "\r" + sujet, MailShortHour = time, MailShortId = id, MailShortFlag = flags });
                            }
                        }
                    });
                    asyncProcessRunning = false;
                    IT_InboxMessages.ItemsSource = gEmailList;
                    sortInbox(ListSortDirection.Descending);
                }
                else
                {
                    await getMailInbox();
                }
                PG_Mailbox.IsIndeterminate = false;
                PG_Mailbox.Visibility = Visibility.Hidden;
            }
        }
        
        // Adapte l'affichage de l'enveloppe d'un mail selon son drapeau passé en paramètre
        private void checkMailFlags(DataGridRowEventArgs e)
        {
            if (e.Row.Item != null)
            {
                MailShort ms = (MailShort)e.Row.Item;
                if (ms.MailShortFlag.HasFlag(MessageFlags.Seen)) // mail déjà lu donc on enlève le gras
                {
                    e.Row.FontWeight = FontWeights.Normal;
                    e.Row.Foreground = Brushes.Black;
                }

            }   
        }

        /// Récupérer un MimeMessage lors que l'utilisateur sélectionne un MailSummary dans le DGV et l'afficher
        private async Task openMail(SelectionChangedEventArgs e)
        {
            if (currentCompteMessagerie.isAuthenticated())
            {
                if (IT_InboxMessages.SelectedIndex != -1)
                {
                    if (e.AddedItems.Count > 0)
                    {
                        //ClearCurrentMail();
                        if (asyncProcessRunning == false)
                        {
                            PG_Mailbox.Visibility = Visibility.Visible;
                            PG_Mailbox.IsIndeterminate = true;
                            gCurrentMailSummary = (MailShort)e.AddedItems[0];
                            await Task.Run(() =>
                            {
                                asyncProcessRunning = true;
                                try
                                {
                                    this.gCurrentMail = currentClient.Inbox.GetMessage(gCurrentMailSummary.uid);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                                asyncProcessRunning = false;
                            });
                            ShowCurrentMail();
                            currentClient.Inbox.AddFlags(gCurrentMailSummary.uid, MessageFlags.Seen, true); // Marquer le mail comme lu
                            PG_Mailbox.Visibility = Visibility.Hidden;
                            PG_Mailbox.IsIndeterminate = false;
                        }
                    }
                }
            }         
        }

        /// Ouverture de la fenêtre de rédaction de mail, préchargée avec les informations en fonction du mail à l'origine de la réponse
        private void loadMailReponse()
        {
            if (currentCompteMessagerie.isAuthenticated() & gCurrentMail != null)
            {
                inboxMode = InboxMode.Reponse;
                SIMAIL.Classes.Email.Email vEmail = new SIMAIL.Classes.Email.Email();

                // Les émetteurs (From) deviennent récepteurs (To)
                foreach (var vDestinataire in gCurrentMail.From.Mailboxes)
                {
                    cEmail_Contact vContact = new cEmail_Contact(vDestinataire.Address, vDestinataire.Name);
                    vEmail.To.Add(vContact);
                }
                vEmail.From = currentCompteMessagerie.Login;
                vEmail.Object = "RE : " + gCurrentMail.Subject;
                vEmail.Body.Text = "";
                vEmail.Body.TextReponse = vEmail.getReponseHeader(gCurrentMail);

                SIMAIL.Views.Email fenEmail = new SIMAIL.Views.Email();
                // Chargement de la fenetre
                fenEmail.action = Email.Action.Reponse;
                fenEmail.currentCompteMessagerie = currentCompteMessagerie;
                vEmail.ShowFenMail(fenEmail);
            }
        }

        // Transférer un mail
        private void loadMailTransfert()
        {
            if (currentCompteMessagerie.isAuthenticated() & gCurrentMail != null)
            {
                inboxMode = InboxMode.Transfert;
                SIMAIL.Classes.Email.Email vEmail = new SIMAIL.Classes.Email.Email();

                vEmail.From = currentCompteMessagerie.Login;
                vEmail.Object = "TR : " + gCurrentMail.Subject;
                vEmail.Body.Text = "";
                vEmail.Body.TextReponse = vEmail.getReponseHeader(gCurrentMail);

                SIMAIL.Views.Email fenEmail = new SIMAIL.Views.Email();
                // Chargement de la fenetre
                fenEmail.action = Email.Action.Transfert ;
                fenEmail.currentCompteMessagerie = currentCompteMessagerie;
                vEmail.ShowFenMail(fenEmail);
            }
        }

        private async void deleteMail()
        {
            if (currentCompteMessagerie.isAuthenticated() & gCurrentMailSummary != null)
            {
                // Demande de confirmation
                MessageBoxResult result = MessageBox.Show("Voulez-vous vraimenter supprimer ce mail du " + gCurrentMailSummary.MailShortDate +" "+ gCurrentMailSummary.MailShortHour + " ?", "SIMAIL", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        currentClient.Inbox.AddFlags(new UniqueId[] { gCurrentMailSummary.uid }, MessageFlags.Deleted, true);
                        currentClient.Inbox.Expunge();
                        await getMailInbox();
                        ClearCurrentMail();
                        break;
                }
                
            }
        }

        private void nouveauMail()
        {
            if (currentCompteMessagerie.isAuthenticated())
            {
                inboxMode = InboxMode.Nouveau;
                SIMAIL.Views.Email vFenEmail = new SIMAIL.Views.Email();

                    vFenEmail.currentCompteMessagerie = currentCompteMessagerie;
                    vFenEmail.Show();
            }
        }

        /// Met à jour l'interface pour afficher le contenu d'un email
        private void ShowCurrentMail()
        {
            if (currentCompteMessagerie.isAuthenticated())
            {
                if (this.gCurrentMail != null)
                {
                    showMailControls();
                    //From 
                    foreach (var vFrom in gCurrentMail.From.Mailboxes)
                    {
                        I_CurrentMailFrom.Text = I_CurrentMailFrom.Text + vFrom.Address + "  ";
                    }
                    I_CurrentMailTo.Text = gCurrentMail.To.ToString();
                    //Cc
                    foreach (InternetAddress vCc in gCurrentMail.Cc.Mailboxes)
                    {
                        I_CurrentMailCc.Text = I_CurrentMailCc.Text + "  " + vCc;
                    }
                    I_CurrentMailSubject.Text = gCurrentMail.Subject.ToString();
                    //Body
                    if (gCurrentMail.HtmlBody != null || gCurrentMail.TextBody != null)
                    {
                        if (gCurrentMail.HtmlBody != null)
                        {
                            WB_CurrentMailBody.NavigateToString(gCurrentMail.HtmlBody);
                        }
                        else
                        {
                            MemoryStream ms = new MemoryStream(UTF8Encoding.Default.GetBytes(gCurrentMail.TextBody));
                            WB_CurrentMailBody.NavigateToStream(ms);
                        }                       
                    }
                    else
                    {
                        //using (var stream = new MemoryStream())
                        //{
                        //    //gCurrentMail.WriteTo(stream);
                        //    //var encoding = Encoding.GetEncoding(28591);
                        //    //var Bytes = stream.GetBuffer();
                        //    //encoding.GetString(Bytes, 0, Bytes.Length);
                        //    //WB_CurrentMailBody.NavigateToStream(stream);

                        //}
                        string MessageText = gCurrentMail.GetTextBody(TextFormat.Html);
                        if (MessageText == null)
                        {
                            MessageText = "No Text.";
                        }
                        MemoryStream ms = new MemoryStream(UTF8Encoding.Default.GetBytes(MessageText));
                        WB_CurrentMailBody.NavigateToStream(ms);

                    }
                    this.L_CurrentMailDateTime.Content = "Reçu le " + gCurrentMail.Date.Date.ToShortDateString() + " à " + gCurrentMail.Date.DateTime.ToShortTimeString();
                    // PJ
                    if (gCurrentMail.Attachments.Count() > 0)
                    {
                        I_CurrentMailPj.Content = gCurrentMail.Attachments.Count() + " pièce(s) jointe(s).";
                    }
                }
            }
        }
#endregion

        #region "Menu"
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

        #region "Contrôles utilisateur"


        private void hideMailControls()
        {
            I_CurrentMailFrom.Visibility = Visibility.Hidden;
            I_CurrentMailTo.Visibility = Visibility.Hidden;
            I_CurrentMailCc.Visibility = Visibility.Hidden;
            I_CurrentMailSubject.Visibility = Visibility.Hidden;
            WB_CurrentMailBody.Visibility = Visibility.Hidden;
            BT_CurrentEmailClose.Visibility = Visibility.Hidden;
            G_CurrentMail.Visibility = Visibility.Hidden;
        }

        private void showMailControls()
        {
            I_CurrentMailFrom.Visibility = Visibility.Visible;
            I_CurrentMailTo.Visibility = Visibility.Visible;
            I_CurrentMailCc.Visibility = Visibility.Visible;
            I_CurrentMailSubject.Visibility = Visibility.Visible;
            WB_CurrentMailBody.Visibility = Visibility.Visible;
            BT_CurrentEmailClose.Visibility = Visibility.Visible;
            G_CurrentMail.Visibility = Visibility.Visible;
        }

        private void clearMenuControls()
        {
            M_MenuControls.Items.Clear();
        }

        /// <summary>
        /// Efface l'affichage du mail courant
        /// </summary>
        private void ClearCurrentMail()
        {
            I_CurrentMailFrom.Text = "";
            I_CurrentMailTo.Text = "";
            I_CurrentMailCc.Text = "";
            I_CurrentMailSubject.Text = "";
            WB_CurrentMailBody.Navigate((Uri)null);
            gCurrentMail = null;
            gCurrentMailSummary = null;
            hideMailControls();
            IT_InboxMessages.SelectedIndex = -1;
        }

        // Efface les messages reçus de l'affichage 
        private void clearInboxMessages()
        {
            // Clear la liste 
            IT_InboxMessages.ItemsSource = null;
            IT_InboxMessages.Items.Clear();
            IT_InboxMessages.ClearValue(ItemsControl.ItemsSourceProperty);
            gEmailList.Clear();
            IT_InboxMessages.ItemsSource = null;
        }

        // Tri par numéro (uid) de mail le DGV Inbox (+grand = +récent)
        private void sortInbox(ListSortDirection pListSort)
        {
            ICollectionView cvTasks = CollectionViewSource.GetDefaultView(IT_InboxMessages.ItemsSource);
            if (cvTasks != null && cvTasks.CanSort == true)
            {
                cvTasks.SortDescriptions.Clear();
                cvTasks.SortDescriptions.Add(new SortDescription("MailShortId", pListSort));

            }
        }

        // Ouvre un dialog contenant tous les destinataires Cc du mail courant
        private void showCurrentMailCc()
        {
            if (gCurrentMail != null & gCurrentMail.Cc.ToString() != "")
            {
                CurrentMailDest fenDiag = new CurrentMailDest();
                fenDiag.L_CurrentMailDest.Content = "Liste des Destinataires Cc :"; 
                foreach (InternetAddress dest in gCurrentMail.Cc)
                {
                    fenDiag.I_CurrentMailDestList.Text = fenDiag.I_CurrentMailDestList.Text + dest.ToString() + "\n";
                }
                fenDiag.Title = "Destinataires - " + gCurrentMail.Subject;
                fenDiag.Show();
            }
        }

        // Ouvre un dialog contenant tous les destinataires du mail courant
        private void showCurrentMailA()
        {
            if (gCurrentMail != null & gCurrentMail.To.ToString() != "")
            {
                CurrentMailDest fenDiag = new CurrentMailDest();
                fenDiag.L_CurrentMailDest.Content = "Liste des Destinataires : ";
                foreach (InternetAddress dest in gCurrentMail.To)
                {
                    fenDiag.I_CurrentMailDestList.Text = fenDiag.I_CurrentMailDestList.Text + dest.ToString() + "\n";
                }
                fenDiag.Title = "Destinataires - " + gCurrentMail.Subject;
                fenDiag.Show();
            }
        }

        // Ouvre un dialog contenant toutes les pièces jointes du mail courant
        private void showCurrentMailPj()
        {
            if(gCurrentMail != null & gCurrentMail.Attachments.Count() > 0)
            {
                CurrentMailPj fenDiag = new CurrentMailPj();
                foreach (MimePart a in gCurrentMail.Attachments.OfType<MimePart>())
                {
                    fenDiag.I_CurrentMailPjList.Items.Add(a.FileName);
                    fenDiag.attachmentsParts.Add(a);
                }
                fenDiag.Title = "Pièces jointes - " + gCurrentMail.Subject;
                fenDiag.Show();
            }
        }

        #endregion

        #region "Evènements"

        private async void I_InboxSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            await searchInbox();
        }

        private async void I_SearchDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (I_SearchDate.SelectedDate != null)
            {
                await searchInboxByDate();
            }          
        }

        //private async void I_SearchDate_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Delete)
        //    {
        //        I_SearchDate.SelectedDate = null;
        //    }
        //    if (e.Key == Key.Tab || e.Key == Key.Enter)
        //    {
        //        if (I_SearchDate.SelectedDate == null)
        //        {
        //            await getMailInbox();
        //        }              
        //    }
        //}

        private async void I_SearchDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (I_SearchDate.Text == "" & BT_Rechercher.IsMouseOver == false)
            {
                await getMailInbox();
            }
        }

        private async void BT_Rechercher_Click(object sender, RoutedEventArgs e)
        {
            await searchInbox();
        }

        private async void I_InboxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                await searchInbox();
            }
        }

        private async void IT_InboxMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(IT_InboxMessages.SelectedIndex != -1)
            {
                await openMail(e);
            }
            
        }

        private void M_EmailNouveau_Click(object sender, RoutedEventArgs e)
        {
            nouveauMail();
        }

        private async void M_EmailRafraichir_Click(object sender, RoutedEventArgs e)
        {           
            await getMailInbox();
        }

        private void M_EmailRepondre_Click(object sender, RoutedEventArgs e)
        {
            loadMailReponse();
        }

        private void M_EmailTransferer_Click(object sender, RoutedEventArgs e)
        {
            loadMailTransfert();
        }

        private void M_EmailSupprimer_Click(object sender, RoutedEventArgs e)
        {
            deleteMail();
        }

        private void BT_CurrentEmailClose_Click(object sender, RoutedEventArgs e)
        {
            ClearCurrentMail();
        }

        private void BT_CurrentEmailA_Click(object sender, RoutedEventArgs e)
        {
            showCurrentMailA();
        }

        private void BT_CurrentEmailCc_Click(object sender, RoutedEventArgs e)
        {
            showCurrentMailCc();
        }

        private void IT_InboxMessages_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            checkMailFlags(e);
        }

        private void BT_CurrentEmailPJ_Click(object sender, RoutedEventArgs e)
        {
            showCurrentMailPj();
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
