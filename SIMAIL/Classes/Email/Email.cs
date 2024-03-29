﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SIMAIL.Classes.Utilisateur;
using MessageBox = System.Windows.Forms.MessageBox;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
using System.IO;

namespace SIMAIL.Classes.Email
{
    public class Email 
    {
        const string gAddressSeparator = ";";

        public String From { get; set; }
        public List<cEmail_Contact> To { get; set; }
        public List<cEmail_Contact> Cc { get; set; }
        public string Object { get; set; }
        public c_EmailBody Body { get; set; }
        public List<System.IO.FileInfo> Pj { get; set; }
        public cEmail_Signature Signature { get; set; }
        public CompteMessagerie CompteMessagerie { get; set; }
        

        public Email()
        {
            this.From = "";
            this.To = new List<cEmail_Contact>();
            this.Cc = new List<cEmail_Contact>();
            this.Body = new c_EmailBody();
            this.Pj = new List<System.IO.FileInfo>();
            this.Signature = new cEmail_Signature();
            this.CompteMessagerie = new CompteMessagerie();
        }


        public MimeMessage getMailMessage()
        {
            MimeMessage vMailMsg = new MimeMessage(); // V1.2 migration vers MimeMessage (librairie Mimekit)
            MimeMessage msg = new MimeMessage();
            BodyBuilder bb = new BodyBuilder();

            //vMailMsg.Body.ContentType.Format =  .IsBodyHtml = true;

            // From         
            vMailMsg.From.Add(new MailboxAddress(From));
            // From
            vMailMsg.Sender = new MailboxAddress(From);
            // Objet
            vMailMsg.Subject = this.Object;
            // To
            foreach (var vDestinataire in this.To)
                vMailMsg.To.Add(new MailboxAddress(vDestinataire.Address));
            // Cc
            foreach (var destinataireCc in this.Cc)
                vMailMsg.Cc.Add(new MailboxAddress(destinataireCc.Address));

            // Body in html and in plain text
            bb.HtmlBody = Body.ToHTML();
            bb.TextBody = Body.Text;
            //vMailMsg.Body = new TextPart(TextFormat.Html)
            //{
            //    Text = Body.ToHTML()
            //   // Body.Text // Me.Body.ToHTML() '
            //};

            // Body et Signature
            //if (Body.ToHTML() != "")
            //{
            //    System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage();
            //    System.Net.Mail.AlternateView htmlView;
            //    string MailContent;
            //    // Si il y a une signature, l'incorporer la signature dans le body
            //    if (this.Signature.ficSignatureHTMLFullName != null)
            //    {
            //        this.Signature.HTMLLoad();
            //        // Ajouter au html de la signature les identifiants cid
            //        this.Signature.html = this.Signature.remplacerOldSrcByCidSrc();
            //        this.Body.Signature = this.Signature;
            //    }
            //    MailContent = this.Body.ToHTML();
            //    // ajouter le mailContent en tant que "alternateView" au mail
            //    htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(MailContent, null, "text/html");
            //    // Encodage en base64 ! important ! sinon les images ressources ne seront pas intégrées (embedded) au corps de mail mais insérée en tant que pj

            //    // ajouter les ressources de la template (images) à la vue
            //    foreach (var vLinkedResource in this.Signature.getLinkedResources())
            //    {
            //        vLinkedResource.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
            //        htmlView.LinkedResources.Add(vLinkedResource);
            //    }
            //    m.AlternateViews.Add(htmlView);

            //    // Conversion du mailmessage en MimeMessage
            //    if (m.AlternateViews.Count > 0)
            //    {
            //        var alternative = new MultipartAlternative();
            //        var body = new TextPart(TextFormat.Html)
            //        {
            //            Text = Body.ToHTML() 
            //        };
            //        alternative.Add(body);
            //        foreach (var view in m.AlternateViews)
            //        {
            //            var part = GetMimePart(view);
            //        }
            //    }
            //}

            // Pièces jointes
            foreach (var piece in this.Pj)
            {
                var attachment = new MimePart("file", piece.Extension)
                {
                    Content = new MimeContent (File.OpenRead(piece.FullName), ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = piece.Name
                };
                bb.Attachments.Add(attachment);
            }

            // building the body of the current mail (HTML/text content, attachments, resources)
            vMailMsg.Body = bb.ToMessageBody();

            return vMailMsg;
        }


        public async Task<bool> EnvoyerEmail()
        {
            bool returnVal = false;

            // Récupérer la connexion Smtp
            SmtpClient vSMTPClient = new SmtpClient(); // CHANGER vers MailKit.Smtp.SmtpCLient
            vSMTPClient = await CompteMessagerie.getSMTPConnection();

            await Task.Run(() =>
            {
                // Envoi du mail
                try
                {
                    MimeMessage vMailMsg = new MimeMessage();
                    vMailMsg = this.getMailMessage();
                    // Me.HtmlSplitPJ(vMailMsg.Body, vMailMsg.Attachments)
                    vSMTPClient.Send(vMailMsg);
                    // Libère les ressources                
                    // vMailMsg.AlternatViews.Dispose()                                   
                    returnVal = true;
                    vSMTPClient.Dispose(); // Permet de clore la connexion. 
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    returnVal = false;
                }
            });          

            return returnVal;
        }
        /// <summary>
        ///     ''' Retourne les adresses mail de tous les Cc en chaine de caractère 
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public string CCToString()
        {
            string VEmails = "";

            foreach (var vContact in this.Cc)
            {
                if (vContact.Address != "")
                {
                    VEmails = VEmails + vContact.Address + gAddressSeparator; 
                }
            }
            return VEmails;
        }

        /// <summary>
        ///     ''' Récupère une chaine de caractères de Cc et ajoute un nouveau contact en liste Cc
        ///     ''' </summary>
        ///     ''' <param name="pCCStr"></param>
        public bool StringToCC(string pCCStr)
        {
            if(pCCStr.Contains(gAddressSeparator) == false) // L'utilisateur doit utiliser le séparateur ("; ") entre les adresses mails
            {
                return false;
            }
            else
            {
                this.Cc.Clear();
                foreach (var vEmail in pCCStr.Split(char.Parse(gAddressSeparator)))
                {
                    if (vEmail != "")
                    {
                        this.Cc.Add(new cEmail_Contact(vEmail));
                    }
                }
                return true;
            }   
        }

        /// <summary>
        ///     ''' Retourne les adresses mail de tous les To en chaine de caractères
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public string ToToString()
        {
            string vEmails = "";
            foreach (var vContact in this.To)
            {
                if (vContact.Address != "")
                {
                    vEmails = vEmails + vContact.Address + gAddressSeparator;
                }
            }
            return vEmails;
        }

        /// <summary>
        ///     ''' Récupère une chaine de caractères de To et ajoute un nouveau contact en liste To 
        ///     ''' </summary>
        ///     ''' <param name="pToStr"></param>
        public bool StringToTo(string pToStr)
        {
            if (pToStr.Contains(gAddressSeparator) == false) // aucun des séparateurs est présent
            {
                return false;
            }
            else
            {
                this.To.Clear();
                foreach (var vEmail in pToStr.Split(char.Parse(gAddressSeparator)))
                {
                    if (vEmail != "")
                    {
                        this.To.Add(new cEmail_Contact(vEmail));
                    }
                }
                return true;
            }   
        }

        /// <summary>
        ///     ''' Initialise  l'email à blanc
        ///     ''' </summary>
        public void clear()
        {
            Object = "";
            Body.Text = "";
        }


        /// <summary>
        ///     ''' Charge un email depuis un fichier au format HTML
        ///     ''' </summary>
        ///     ''' <param name="pFile"></param>
        ///     ''' <param name="pLangue"></param>
        public void Load(string pFile, string pLangue = "FRA")
        {
            try
            {
                XmlDocument vXmlDoc = new XmlDocument();

                this.clear();
                vXmlDoc.Load(pFile);

                foreach (XmlNode vXmlNode in vXmlDoc.DocumentElement)
                {
                    if (vXmlNode.Attributes["LANGUAGE"].Value == pLangue)
                    {
                        foreach (XmlNode vXmlNode2 in vXmlNode.ChildNodes)
                        {
                            switch (vXmlNode2.LocalName)
                            {
                                case "OBJECT":
                                    {
                                        Object = vXmlNode2.InnerText;
                                        break;
                                    }

                                case "BODY":
                                    {
                                        Body.Text = vXmlNode2.InnerText;
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public void showosifjso()
        {

        }

        public void ShowFenMail(SIMAIL.Views.Email pfenMail)
        {

            pfenMail.email = this;
            pfenMail.Show();


        }

        // Retourne la template préremplie pour le mail de retour
        public string getReponseHeader(MimeKit.MimeMessage ms)
        {
            string str = "";
            if (ms != null & From != "" & Body != null)
            {
                str += "\r\n\r\n\r\n\r\n -------------------------------------------------------------------------------------------- \r";
                str += "De :"+ ms.From + "\r";
                str += "Envoyé : " + ms.Date.DateTime.ToLocalTime().ToLongDateString() + "\r";
                str += "A : " + ms.To.ToString() + "\r";
                str += "Objet : " + ms.Subject + "\r";
                str += "\r" + Body.convertTextFromHtml(ms.HtmlBody);
            }
            return str;
        }

    }
}