using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class Mercury200RS485 : IDevice
    {//здесь будет описан счётчик Меркурий 200
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }

        public bool Search(string textToFind, StringComparison compare, string add = "")
        {
            // if ((Utils.Contains(this.Name, textToFind, StringComparison.CurrentCultureIgnoreCase))
            //|| (Utils.Contains(this.SerialNumber, textToFind, StringComparison.CurrentCultureIgnoreCase))
            //|| (Utils.Contains(add, textToFind, StringComparison.CurrentCultureIgnoreCase)))
            // {
            //     return true;
            // }

            return false;
        }
    }
}
