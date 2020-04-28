using MailKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMAIL.Classes.Email
{
    class MailShort
    {
        public String MailShortDate { get; set; }
        public String MailShortText { get; set; }
        public String MailShortHour { get; set; }
        public UniqueId uid { get; set; }
        public int MailShortId { get; set; }
        public MessageFlags MailShortFlag { get; set; }

        public MailShort ()
        {

        }

        public MailShort(UniqueId pUid)
        {
            this.uid = pUid;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
