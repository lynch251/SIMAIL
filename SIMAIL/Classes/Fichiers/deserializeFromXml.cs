using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace SIMAIL.Classes.Fichiers
{
    class deserializeFromXml
    {
        // désérialise un fichier XML, dont le type est précisé
        public static object getData(object pObj, string pFilename)
        {
            try
            {
                XmlSerializer vSerializer = new XmlSerializer(pObj.GetType());

                FileStream fileStream = new FileStream(@"" + pFilename, FileMode.Open, FileAccess.Read, FileShare.Read);

                object obj = vSerializer.Deserialize(fileStream);
                return obj;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\r" + " Fichier non lisible ou compte serveur incorrect.");
            }
            return null;
        }

    }
}
