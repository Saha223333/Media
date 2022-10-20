using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.ComponentModel;

namespace NewProject
{
    public interface IConnection
    {           
        string Phone{ get; set; }
        string IP { get; set; }
        string Port { get; set; }
        string Name { get; set; }
        int Channel { get; set; }
        string CBST { get; set; }

        void GatherDevicesData(string portname, TreeNodeCollection childNodes, DataProcessing dp, ToolStripProgressBar pb, DateTime ndate,
            DateTime kdate, int countSumm, bool GetProfile, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox rtb, BackgroundWorker bcw, bool ReReadOnlyAbsent); 
        void GetMonitorForCounter(string workingPort, ICounter counter, DataProcessing dp, 
            ToolStripProgressBar pb, PictureBox pixBox, RichTextBox rtb, ref BackgroundWorker worker);
        DataTable GetPowerProfileForCounter(string workingPort, ICounter counter, DataProcessing dp,
            ToolStripProgressBar pb, DateTime DateN, DateTime DateK, int countSumm, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox rtb, BackgroundWorker worker, bool ReReadOnlyAbsent);
        void WriteParametersToDevice(TreeNode node, DataProcessing dp,
             RichTextBox rtb, BackgroundWorker worker, List<FieldsValuesToWrite> formControlValuesList, List<FieldsValuesToWrite> list = null);      
    }
}
