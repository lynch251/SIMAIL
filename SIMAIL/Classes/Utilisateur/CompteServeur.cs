using SIMAIL.Classes.Fichiers;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMAIL.Classes.Utilisateur
{
    public class CompteServeur
    {
        public enum Chiffrement
        {
            Aucun,
            SSL,
            TLS            
        }

        public enum MethConnexion
        {
            Identifiants,
            OpenID,
            OAuth2
        }

        public MethConnexion methodeConnexion { get; set; }
        public string Fournisseur { get; set; }

        public string AdresseIMAP { get; set; }
        public int PortIMAP { get; set; }
        public Chiffrement ChiffrementIMAP { get; set; }
        
        public string AdresseSMTP { get; set; }
        public int PortSMTP { get; set; }
        public Chiffrement ChiffrementSMTP { get; set; }

        public string saveDefaultDirectory { get; set; }
        public string saveDirectory { get; set; }

        public CompteServeur()
        {
            // Valeurs par défaut
            saveDefaultDirectory = new KnownFolder(KnownFolderType.Documents).Path + "\\" + "SIMAIL";
            saveDirectory = "";
            methodeConnexion = MethConnexion.Identifiants;
        }

        public CompteServeur(String pFournisseur)
        {
            Fournisseur = pFournisseur;
        }

        public Boolean isValid()
        {
            if (AdresseIMAP != "" & PortIMAP != 0 & AdresseSMTP != "" & PortSMTP != 0)
            {
                return true;
            }
            return false;
        }

        // Sauvegarde le compte serveur sur la machine de l'utilisateur
        public void save()
        {
            if (this.isValid())
            {
                if (this.saveDirectory != "")
                {
                    saveToXml.saveData(this, saveDirectory + "\\" + this.Fournisseur + ".xml");
                }
                else
                {
                    saveToXml.saveData(this, saveDefaultDirectory + "\\" + this.Fournisseur + ".xml");
                }           
            }
        }

        public override string ToString()
        {
            return Fournisseur ;
        }
    }
}
