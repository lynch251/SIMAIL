using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace SIMAIL.Classes.Fichiers
{
    class saveToXml
    {
        // Sérialise un objet en XML
        public static void saveData(object pObj, string pFilename)
        {
            try
            {
                XmlSerializer vSerializer = new XmlSerializer(pObj.GetType());
                TextWriter vWriter = new StreamWriter(pFilename);
                vSerializer.Serialize(vWriter, pObj);
                vWriter.Close();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message + "\n\r" + " Fichier non sauvegardé !");             
            }
        }
    }
}
