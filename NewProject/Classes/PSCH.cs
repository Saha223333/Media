using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class PSCH : IDevice
    {//здесь будет описан счётчик ПСЧ
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }

        public bool Search(string textToFind, StringComparison compare, string add = "")
        {
            return false;
        }
    }
}
