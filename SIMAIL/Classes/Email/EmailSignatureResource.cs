using System;
using System.IO;

namespace SIMAIL.Classes.Email
{

    public class cEmail_Signature_Ressource_Img
    {
        public string Fullpath { get; set; } // repTemputilisateur/newlogo.png
        public string ChaineBase64 { get; set; } // iVBORw0KGgoAAAANSUhEUgAABrsAAAGwCAYAAA........
        public string NomFichier { get; set; } // newlogo.png
        public string Extension { get; set; } // png
        public string ChaineInfosImage { get; set; } // data:image/png;filename=newlogo.png

        public cEmail_Signature_Ressource_Img()
        {
        }

        /// <summary>
        ///     ''' Constructeur en vue d'une conversion de la base 64 vers un fichier image ou stream
        ///     ''' </summary>
        ///     ''' <param name="pChaineBase64"></param>
        public cEmail_Signature_Ressource_Img(string pChaineBase64)
        {
            this.ChaineBase64 = pChaineBase64;
        }

        /// <summary>
        ///     ''' Constructeur en vue d'une conversion d'un fichier image (quelque soit l'extension) vers la base 64
        ///     ''' </summary>
        ///     ''' <param name="pNomFichier"></param>
        ///     ''' <param name="pFullPath"></param>
        ///     ''' <param name="pExtension"></param>
        public cEmail_Signature_Ressource_Img(string pNomFichier, string pFullPath, string pExtension)
        {
            this.NomFichier = pNomFichier;
            this.Fullpath = pFullPath;
            this.Extension = pExtension;
        }

        /// <summary>
        ///     ''' Convertit une chaine base64 en fichier image (image stockée dans le dossier temp du l'utilisateur)
        ///     ''' </summary>
        ///     ''' <returns>Retourne le path complet de l'image (enregistrée dans le dossier temp de l'utilisateur courant) </returns>

        // Public Function base64ToImage() As String

        // 'Premiere solution pour convertir une chaine base64 en fichier image
        // If Me.ChaineBase64 <> "" And Me.Extension <> "" And Me.NomFichier <> "" Then
        // ' Conversion de base 64 vers fichier image
        // Dim imageBytes As Byte()
        // imageBytes = Convert.FromBase64String(Me.ChaineBase64)
        // Dim ms As New MemoryStream(imageBytes, 0, imageBytes.Length)
        // ms.Write(imageBytes, 0, imageBytes.Length)
        // Dim image As System.Drawing.Image
        // image = System.Drawing.Image.FromStream(ms, True)
        // 'récupérer le format initial de l'image
        // Dim vImageTempFullPath = Path.GetTempPath
        // image.Save(vImageTempFullPath + Me.NomFichier)

        // Return vImageTempFullPath + Me.NomFichier

        // End If
        // Return Nothing
        // End Function

        /// <summary>
        ///     ''' Convertit une chaine base64 en fichier image (image stockée dans le dossier temp du l'utilisateur)
        ///     ''' </summary>
        ///     ''' <returns>Retourne le path complet de l'image (enregistrée dans le dossier temp de l'utilisateur courant) </returns>

        public MemoryStream base64ToImage()
        {

            // Premiere solution pour convertir une chaine base64 en fichier image
            if (this.ChaineBase64 != "" & this.Extension != "" & this.NomFichier != "")
            {
                // Conversion de base 64 vers fichier image
                byte[] imageBytes;
                imageBytes = Convert.FromBase64String(this.ChaineBase64);
                MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);

                return ms;
            }
            return null;
        }

        /// <summary>
        ///     ''' Convertit une image (tout format accepté) en chaine de caractères base64
        ///     ''' </summary>
        ///     ''' <returns></returns>
        //public string imageToBase64()
        //{
        //    //// Convertir le base 64 en .png : A TERMINER
        //    //string imagePath;
        //    //imagePath = "C:/Users/FR01US_PCN/Pictures/test.png";
        //    //string imgBase64String;
        //    //System.Drawing.Image image;
        //    //image = System.Drawing.Image.FromFile(imagePath);
        //    //MemoryStream m = new MemoryStream();

        //    //image.Save(m, image.RawFormat);
        //    //byte[] imageBytes;
        //    //imageBytes = m.ToArray();
        //    //imgBase64String = Convert.ToBase64String(imageBytes);

        //    //return null;
        //}
    }
}