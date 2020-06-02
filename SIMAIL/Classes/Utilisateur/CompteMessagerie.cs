using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SIMAIL.Classes.Utilisateur
{

    public class CompteMessagerie
    {
        #region "Attributs"
        public string Login
        {
            get => _Login;
            set
            {
                if (IMAPConnexion == null) 
                {
                    // if the user is not connected
                    _Login = value;
                }
            }
        }
        public string Pass
        {
            get
            {
                    return _Pass;
            }
            set
            {
                if (IMAPConnexion == null)
                {
                    // if the user is not connected
                    _Pass = value;
                }
            }
        }
        /// <summary> Instance unique de connexion au serveur IMAP </summary>
        private ImapClient IMAPConnexion;
        /// <summary> Instance unique de connexion au serveur SMTP </summary>
        private  SmtpClient SMTPConnexion;
        /// <summary> Objet contenant les paramètres de connexion SMTP et IMAP définis par l'utilisateur </summary>
        public CompteServeur compteServeur { get; set; }
        public string compteServDefaultDirectory { get; set; }
        public int nbMailToLoad { get; set; }
        /// Temps en minutes
        public int timerInboxReload { get; set; }
        #endregion

        #region "Variables"
        bool asyncProcessRunning = false;
        private string _Login;
        private string _Pass;
        #endregion

        public async Task<ImapClient> getIMAPConnection()
        {
            if (IMAPConnexion == null) { IMAPConnexion = new ImapClient(); }
            if (IMAPConnexion.IsAuthenticated == false)
            {
               if(await IMAPAuthenticate() == null)
                {
                    IMAPConnexion = null;
                }
                else
                {
                    IMAPConnexion = await IMAPAuthenticate();
                }
            }
            return IMAPConnexion;
        }

        public async Task<SmtpClient> getSMTPConnection()
        {
            if (SMTPConnexion == null) { SMTPConnexion = new SmtpClient(); }
            if (SMTPConnexion.IsAuthenticated == false)
            {
                if (await SMTPAuthenticate() == null)
                {
                    SMTPConnexion = null;
                }
                else
                {
                    SMTPConnexion = await SMTPAuthenticate();
                }              
            }
            return SMTPConnexion;
        }

        /// <summary>Retourne une connexion IMAP ouverte</summary>
        private async Task<ImapClient> IMAPAuthenticate()
        {
            ImapClient ic;
            switch (compteServeur.methodeConnexion)
            {
                case CompteServeur.MethConnexion.Identifiants:
                    if (isValid())
                    {

                        ic = new ImapClient();
                        // For demo-purposes, accept all SSL certificates
                        ic.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        await Task.Run(() =>
                        {
                            try
                            {
                                if (compteServeur.ChiffrementIMAP == CompteServeur.Chiffrement.SSL)
                                {
                                    ic.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, MailKit.Security.SecureSocketOptions.SslOnConnect);
                                }
                                else if (compteServeur.ChiffrementIMAP == CompteServeur.Chiffrement.TLS)
                                {
                                    ic.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, MailKit.Security.SecureSocketOptions.StartTls);
                                }
                                else
                                {
                                    ic.Connect(compteServeur.AdresseIMAP, compteServeur.PortIMAP, false);
                                }
                                asyncProcessRunning = true;
                                ic.Authenticate(Login, Pass);
                                this.nbMailToLoad = 25; // rendre paramétrable
                            }
                            catch (System.Exception e)
                            {
                                MessageBox.Show(e.Message);
                            }
                        });
                        asyncProcessRunning = false;
                        if (ic.IsAuthenticated == true)
                        {
                            return ic;
                        }
                    }
                    break;

                case CompteServeur.MethConnexion.OAuth2:

                    ic = new ImapClient();
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
                    var credential = authCode.AuthorizeAsync(Login, cancellationToken).Result;

                    cancellationToken.ThrowIfCancellationRequested();

                    await credential.RefreshTokenAsync(cancellationToken);

                    var oauth2 = new SaslMechanismOAuth2(credential.UserId, credential.Token.AccessToken);

                        await ic.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect); // non paramètrable
                    await ic.AuthenticateAsync(oauth2);
                                
                    if (ic.IsAuthenticated)
                    {
                        return ic;
                    }
                    asyncProcessRunning = false;

                    break;
            }
            return null;
        }

        private async Task<SmtpClient> SMTPAuthenticate()
        {
            if (isValid() & this.isAuthenticated())
            {
                var sc = new SmtpClient();
                SecureSocketOptions chiffrement = SecureSocketOptions.None;
                if (this.compteServeur.ChiffrementSMTP == CompteServeur.Chiffrement.SSL)
                {
                    chiffrement = SecureSocketOptions.Auto ;
                }
                else if (this.compteServeur.ChiffrementSMTP == CompteServeur.Chiffrement.TLS)
                {
                    chiffrement = SecureSocketOptions.StartTlsWhenAvailable;
                }

                await sc.ConnectAsync(compteServeur.AdresseSMTP, compteServeur.PortSMTP, chiffrement);
                switch (compteServeur.methodeConnexion)
                {
                    case CompteServeur.MethConnexion.Identifiants:
                         sc.Authenticate(this.Login, this.Pass);
                        if (sc.IsAuthenticated)
                        {
                            return sc;
                        }
                            break;
                    case CompteServeur.MethConnexion.OAuth2:
                        break;
                }                
            }
            return null;
        }

        /// Le compte de messagerie est théoriquement correctement renseigné pour une connexion
        public bool isValid()
        {
            if (compteServeur != null)
            {
                if (compteServeur.AdresseIMAP != "" & compteServeur.PortIMAP != 0 & Pass != "" & Login != "")
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
            if (IMAPConnexion != null)
            {
                if (IMAPConnexion.IsConnected & IMAPConnexion.IsAuthenticated)
                {
                    try
                    {
                        IMAPConnexion.Inbox.Open(MailKit.FolderAccess.ReadWrite);
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
            return Login;
        }






}
}