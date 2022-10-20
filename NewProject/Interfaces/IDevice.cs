using System;
using System.Windows.Forms;

namespace NewProject
{
    public interface IDevice
    {        
        int ID { get; set; }
        int ParentID { get; set; }
        string Name { get; set; }

        bool Search(string textToFind, StringComparison compare, string add = "");    
    }
}
