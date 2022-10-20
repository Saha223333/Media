using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NewProject
{
    public partial class ChangesForm : Form
    {
        public ChangesForm()
        {
            InitializeComponent();
            Utils.DoubleBufferGrid(ChangesGridConnections, true);
            Utils.DoubleBufferGrid(ChangesGridCountersRS, true);
            Utils.DoubleBufferGrid(ChangesGridCountersPLC, true);            
        }

        public void RefreshChangesGrid()
        {
            ChangesGridConnections.DataSource = DataBaseManagerMSSQL.Return_Changes(FromDateTimePicker.Value, ToDateTimePicker.Value, "Connection_points");
            ChangesGridCountersRS.DataSource = DataBaseManagerMSSQL.Return_Changes(FromDateTimePicker.Value, ToDateTimePicker.Value, "Counters_RS");
            ChangesGridCountersPLC.DataSource = DataBaseManagerMSSQL.Return_Changes(FromDateTimePicker.Value, ToDateTimePicker.Value, "Counters_PLC");
        }

        private void RefreshChangesGridButt_Click(object sender, EventArgs e)
        {
            RefreshChangesGrid();
        }

        private void FilterByValueButt_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl.GetType() == typeof(DataGridView))
            {
                DataBaseManagerMSSQL.FilterByValue((DataGridView)this.ActiveControl, this.FilterValueTextBox);
                ChangesGridContextMenu.Close();
                FilterValueTextBox.Text = String.Empty;
            }
        }

        private void FilterValueTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.ActiveControl.GetType() == typeof(DataGridView))
                {
                    DataBaseManagerMSSQL.FilterByValue((DataGridView)this.ActiveControl, this.FilterValueTextBox);
                    ChangesGridContextMenu.Close();
                    FilterValueTextBox.Text = String.Empty;
                }
            }
        }

        private void ChangesForm_Load(object sender, EventArgs e)
        {

        }
    }
}
