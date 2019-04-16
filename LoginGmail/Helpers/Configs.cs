using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginGmail.Helpers
{
    class Configs
    {
        public int Page_Load { get; set; }
        public int Wait_Enter { get; set; }
        public int Login_Type { get; set; }
        public int Fake_IP { get; set; }
        public string Location { get; set; }
        public int IP_Alive_Interval { get; set; }
        public int IP_Timeout { get; set; }
        public string Bin_Location { get; set; }
    }
}
