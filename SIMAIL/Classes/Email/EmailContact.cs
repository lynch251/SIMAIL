
namespace SIMAIL.Classes.Email
{

    public class cEmail_Contact
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Address { get; set; }
        public string NomPrenom { get; set; }

        public cEmail_Contact()
        {
        }


        public cEmail_Contact(string pEmailAdr)
        {
            this.Address = pEmailAdr;
        }

        public cEmail_Contact(string pEmailAdr, string pNomPrenom)
        {
            this.Address = pEmailAdr;
            this.NomPrenom = pNomPrenom;
        }

        public cEmail_Contact(string pEmailAdr, string pNom, string pPrenom)
        {
            this.Prenom = pPrenom;
            this.Nom = pNom;
            this.Address = pEmailAdr;
        }

        public override string ToString()
        {
            return this.Address + "(" + this.Nom + " " + this.Prenom + ")";
        }
    }
}