using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using HtmlAgilityPack;
using System.Windows;

/// <summary>
/// ''' Cette classe récupère dans un fichier .html, la partie HTML d'une signature, et la partie data(base64) des iamges associées
/// ''' Elle renvoie ensuite la signature complète en tant que type "AlternateView" à ajouter à un objet de type MailMessage
/// ''' Etape 0 : Création de l'objet(chemin complet du fichier de template signature .html nécessaire)
/// ''' Etape 1 : Lancer la méthode HTMLLoad()
/// ''' Etape 2 : Lancer la méthode remplacerOldSrcByCidSrc()
/// ''' Etape 3 : Lancer la méthode getLinkedResources()
/// ''' </summary>
namespace SIMAIL.Classes.Email
{
    public class cEmail_Signature
    {
        public string htmlEntier { get; set; } // contient l'ensemble du code (initial, non traité) du fichier .html
        public string html { get; set; } // contient le code html contenu dans les balises body du fichier .html
        public List<cEmail_Signature_Ressource_Img> imagesRessources { get; set; }
        public string ficSignatureHTMLFullName { get; set; }

        public cEmail_Signature()
        {
            this.imagesRessources = new List<cEmail_Signature_Ressource_Img>();
        }

        /// <summary>
        ///     ''' Construire un objet signature à partir d'une template HTML en vue d'en extraire 1) la structure HTML 2) les ressources images présentées sous base64
        ///     ''' </summary>
        ///     ''' <param name="pFicSignatureHTMLFullName">Chemin complet du fichier</param>
        public cEmail_Signature(string pFicSignatureHTMLFullName)
        {
            this.ficSignatureHTMLFullName = pFicSignatureHTMLFullName;
            this.imagesRessources = new List<cEmail_Signature_Ressource_Img>();
        }

        #region "Récupération de données brutes"
        /// <summary>
        ///     ''' Récupère le contenu du fichier HTML (structure HTML et les images chiffrées en base64 et alimente les propriétés html et imagesRessources de cet objet)
        ///     ''' </summary>
        public void HTMLLoad()
        {
            if (this.ficSignatureHTMLFullName != "")
            {
                try
                {
                    // Chargement du document HTML
                    HtmlDocument vDoc = new HtmlDocument();
                    vDoc.Load(this.ficSignatureHTMLFullName);

                    // ======================================Partie relative à l'attribut "htmlEntier"==========================================================
                    // Récupère tout le contenu HTML du fichier spécifié
                    string vHTMLEntierTemplateSignature = "";
                    vHTMLEntierTemplateSignature = this.getContentFicHTML(this.ficSignatureHTMLFullName);
                    this.htmlEntier = vHTMLEntierTemplateSignature;

                    // ======================================Partie relative à l'attribut "html"================================================================
                    // Récupère UNIQUEMENT le contenu HTML entre les balises "body" du fichier template .html
                    if (this.htmlEntier != "")
                    {
                        string vHTMLTemplateSignature = "";
                        HtmlDocument vDoc2 = new HtmlDocument();
                        vDoc2.LoadHtml(this.htmlEntier);
                        var vBody = vDoc2.DocumentNode.Descendants("body");
                        foreach (var vContenu in vBody)
                            this.html = vContenu.InnerHtml;
                    }

                    // ======================================Partie relative à l'attribut "imagesRessources"====================================================
                    // Décomposer chaque chaine de caractère de  l'attribut src de chaque balise img pour en tirer : le nom de l'image et la chaine base64
                    List<string> vListOfChaineBase64 = new List<string>();
                    List<string> listOfAllBalisesImgAttributSrc;
                    listOfAllBalisesImgAttributSrc = getContenuDeBalisesImgAttributSrc(vDoc);
                    foreach (var vChaineImage in listOfAllBalisesImgAttributSrc)
                    {
                        string vChaineInfosImageBase64;   // la chaine base 64
                        string vChaineInfosImage;         // la chaine contenant les infos de l'image
                        string[] vTabSplit = new[] { ";ba", "se64," }; // Exemple de chaine : (date:image/png;filename=image.png;base64,987dfsdf845d5f49sd8f7sdf5s64fqsdqsd1.....)

                        // Chaine base64
                        vChaineInfosImageBase64 = vChaineImage.Split(vTabSplit, StringSplitOptions.RemoveEmptyEntries).Last();

                        // Chaine contenant les infos de l'image
                        vChaineInfosImage = vChaineImage.Split(vTabSplit, StringSplitOptions.RemoveEmptyEntries).First();
                        // Décomposer la chaine d'informations de l'image Exemple de chaine : (date:image/png;filename=image.png)
                        string vFileNameImage;
                        string vExtensionImage;
                        vFileNameImage = vChaineInfosImage.Split('=').Last();
                        vExtensionImage = vFileNameImage.Split('.').Last();

                        // Création d'une Ressource Image
                        cEmail_Signature_Ressource_Img vRessourceImage = new cEmail_Signature_Ressource_Img();
                        vRessourceImage.Extension = vExtensionImage;
                        vRessourceImage.NomFichier = vFileNameImage;
                        vRessourceImage.ChaineBase64 = vChaineInfosImageBase64;
                        vRessourceImage.ChaineInfosImage = vChaineInfosImage;

                        // Ajout à la liste de la propriété de l'objet signature
                        this.imagesRessources.Add(vRessourceImage);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("Aucun fichier de signature de template .html spécifié !");
        }

        /// <summary>
        ///     ''' Récupérer le contenu de l'attribut "src" de toutes les balises "img" d'un fichier HTML
        ///     ''' </summary>
        ///     ''' <param name="pHTMLDocument">Un objet de type HTMLDocument</param>
        ///     ''' <returns>Une liste de chaines de caractères</returns>
        public List<string> getContenuDeBalisesImgAttributSrc(HtmlDocument pHTMLDocument)
        {
            try
            {
                List<string> vListOfContentOfAllImgSrc = new List<string>();
                string vContentOfOneImgSrc = "";

                // parcours du fichier html
                List<HtmlNode> links = new List<HtmlNode>();
                foreach (var link in pHTMLDocument.DocumentNode.Descendants("img"))
                    vListOfContentOfAllImgSrc.Add(link.GetAttributeValue("src", "nothing"));
                return vListOfContentOfAllImgSrc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     ''' Retourne une chaine de caractères contenant la totalité du contenu d'un fichier HTML
        ///     ''' </summary>
        ///     ''' <param name="pHTMLDocument">Fichier de type .html</param>
        ///     ''' <returns>Une chaine de caractères</returns>
        public string getContentFicHTML(string pHTMLDocument)
        {
            try
            {
                string vContentOfHTMLFic = "";
                StreamReader reader = new StreamReader(pHTMLDocument);
                vContentOfHTMLFic = reader.ReadToEnd();
                reader.Close();
                return vContentOfHTMLFic;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
        #endregion
        #region  "Traitement des données"

        /// <summary>
        ///     ''' Dans la chaine html de la signature, remplace l'ancienne chaine par une nouvelle chaine incluant le cid de chaque ressource
        ///     ''' </summary>
        ///     ''' <returns>Nouvelle chaine (String)</returns>
        public string remplacerOldSrcByCidSrc()
        {

            // ==============================Gestion de la structure HTML de la signature : toutes les balises img devront etre identifiées par un identifiant cid associé à une image de type "linkedResource" ===============================
            // Exemple de chaine de structure HTML pour la signature : <html><body><p>Texte de signature</p><img src='cid:IDENTIFIANTIMAGE'></body></html>
            if (this.html != "" & this.imagesRessources.Count != 0)
            {
                string vHtmlSignature = "";
                vHtmlSignature = this.html;
                // Pour chaque image ressource présente, on cherche la chaine src correspondante au nom de fichier 
                foreach (var vBaliseImg in this.imagesRessources)
                {
                    var vCid = vBaliseImg.NomFichier;
                    // on remplace la chaine src par une nouvelle : cid:nomfichier.extension
                    vHtmlSignature = vHtmlSignature.Replace(vBaliseImg.ChaineInfosImage + ";base64," + vBaliseImg.ChaineBase64, "cid:" + vBaliseImg.NomFichier);
                }
                return vHtmlSignature;
            }
            return null;
        }

        public List<System.Net.Mail.LinkedResource> getLinkedResources()
        {
            List<System.Net.Mail.LinkedResource> vLinkedResourcesColl = new List<System.Net.Mail.LinkedResource>();
            // ==============================Gestion des images ressources ===================================================================================================================================================================
            if (this.imagesRessources.Count != 0)
            {
                // Ajout de toutes les imagesRessources en tant que "LinkedResource" nécessaires à la vue de la signature (collection à ajouter à une alternateview)
                foreach (var vImageRessource in this.imagesRessources)
                {
                    // Conversion de la base64 à image et conversion de l'imageRessource en "linkedResource"
                    System.Net.Mail.LinkedResource vImageLinkedResource;
                    vImageLinkedResource = new System.Net.Mail.LinkedResource(vImageRessource.base64ToImage(), "image/" + vImageRessource.Extension + "");
                    // Indication de l'identifiant cid obligatoire !!
                    vImageLinkedResource.ContentId = vImageRessource.NomFichier;
                    vLinkedResourcesColl.Add(vImageLinkedResource);
                }
            }
            return vLinkedResourcesColl;
        }
        #endregion
    }
}
