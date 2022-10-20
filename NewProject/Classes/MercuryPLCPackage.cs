using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class MercuryPLCPackage
    {
        public int dummy1 { get; set; }//пустышка для экспорта грида в Excel
        public int dummy2 { get; set; }//пустышка для экспорта грида в Excel
        public int type { get; set; }
        public string name { get; set; }
        public double value { get; set; }
        public DateTime datetime { get; set; }

        public MercuryPLCPackage(int ptype, double penergy, DateTime pdatetime, string pname)
        {
            this.type = ptype;
            this.value = penergy;
            this.datetime = pdatetime;
            this.name = pname;
        }
    }
}
