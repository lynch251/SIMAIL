using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SIMAIL.Classes.Utilisateur
{

    public class CompteMessagerie
    {
        public string Identifiant { get; set; }
        public string Mdp { get; set; }
        public ImapClient IMAPclient { get; set; }

        public CompteServeur compteServeur { get; set; }
        public string compteServDefaultDirectory { get; set; }

        public int nbMailToLoad { get; set; }
        /// Temps en minutes
        public int timerInboxReload { get; set; }

        bool asyncProcessRunning = false;

        // Singleton
        private static CompteMessagerie instance;
        /// <summary>
        /// Récupère le compte ou initialise un nouveau compte
        /// </summary>
        /// <returns></returns>
        public static CompteMessagerie Instance()
        {
            if (instance == null)
            {
                instance = new CompteMessagerie();    
            }
            return instance;
        }

        /// Retourne une connexion IMAP ouverte
        public async Task<ImapClient> Authenticate()
        {
            ImapClient client = null;
            switch (compteServeur.methodeConnexion)
            {
                case CompteServeur.MethConnexion.Identifiants:
                    if (isValid())
                    {
                        IMAPclient = new ImapClient();

                        // For demo-purposes, accept all SSL certificates
                        IMAPclient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        await Task.Run(() =>
                        {
                            try
                            {
                                if (compteServeur.ChiffrementIMAP == CompteServeur.Chiffrement.SSL)
                                {
                                    IMAPclient.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, MailKit.Security.SecureSocketOptions.SslOnConnect);
                                }
                                else if (compteServeur.ChiffrementIMAP == CompteServeur.Chiffrement.TLS)
                                {
                                    IMAPclient.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, MailKit.Security.SecureSocketOptions.StartTls);
                                }
                                else
                                {
                                    IMAPclient.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, false);
                                }
                                asyncProcessRunning = true;
                                IMAPclient.Authenticate(Identifiant, Mdp);
                                this.nbMailToLoad = 25; // rendre paramétrable
                            }
                            catch (System.Exception e)
                            {
                                MessageBox.Show(e.Message);
                            }
                        });
                        asyncProcessRunning = false;
                        if (IMAPclient.IsAuthenticated == true)
                        {
                            client = IMAPclient;
                            return client;
                        }
                    }
                    break;

                case CompteServeur.MethConnexion.OAuth2:

                    ImapClient ImapClient = new ImapClient();
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
                    CancellationToken cancellationToken = new CancellationToken();
                    var credential = authCode.AuthorizeAsync(Identifiant, cancellationToken).Result;

                    cancellationToken.ThrowIfCancellationRequested();

                    await credential.RefreshTokenAsync(cancellationToken);

                    var oauth2 = new SaslMechanismOAuth2(credential.UserId, credential.Token.AccessToken);

                    await ImapClient.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect); // non paramètrable
                    await ImapClient.AuthenticateAsync(oauth2);
                                
                    if (ImapClient.IsAuthenticated)
                    {
                        client = ImapClient;
                    }
                    asyncProcessRunning = false;

                    break;
            }
            return client;
        }

        /// Connexion de type Smtp via accès non hashé pour envoi de mail
        public SmtpClient getCnxSMTP()
        {
            if (isValid() & this.isAuthenticated())
            {
                //var vSMTPCnx = new SmtpClient();

                //vSMTPCnx.Host = this.compteServeur.AdresseSMTP;
                //vSMTPCnx.Port = this.compteServeur.PortSMTP;
                //if (this.compteServeur.ChiffrementSMTP == CompteServeur.Chiffrement.SSL)
                //{
                //    vSMTPCnx.EnableSsl = true;
                //}
                //if (this.compteServeur.ChiffrementSMTP == CompteServeur.Chiffrement.TLS)
                //{                   
                //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //}
                //vSMTPCnx.DeliveryMethod = SmtpDeliveryMethod.Network;
                //System.Net.NetworkCredential SMTPUserInfo = new System.Net.NetworkCredential(this.Identifiant, this.Mdp);
                //vSMTPCnx.Credentials = SMTPUserInfo;

                //return vSMTPCnx;
            }
            return null;
        }

        /// Le compte de messagerie est théoriquement correctement renseigné pour une connexion
        public bool isValid()
        {
            if (compteServeur != null)
            {
                if (compteServeur.AdresseIMAP != "" & compteServeur.PortIMAP != 0 & Mdp != "" & Identifiant != "")
                {
                    return true;
                } 
            }
            return false;
        }

        // Vérifie que la connexion au serveur de messagerie est opérationnelle
        public bool isAuthenticated()
        {
            bool vValue = false;
            if (IMAPclient != null)
            {
                if (IMAPclient.IsConnected & IMAPclient.IsAuthenticated)
                {
                    try
                    {
                        IMAPclient.Inbox.Open(MailKit.FolderAccess.ReadWrite);
                        vValue = true;
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\n\n Vous avez été déconnecté ! Redémarrage de l'application.", "SIMAIL", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        try
                        {
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                    }
                }
            }
            return vValue;
        }

        public override string ToString()
        {
            return Identifiant;
        }






}
}