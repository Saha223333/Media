using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Globalization;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;


namespace NewProject
{
    public partial class EnergyHistoryGridForm : Form
    {
        public EnergyHistoryGridForm(TreeNode globalSelectedNode, DateTimePicker DateTimeNEnergy, DateTimePicker DateTimeKEnergy, string energyname)
        {
            InitializeComponent();

            ICounter counter = (ICounter)globalSelectedNode.Tag;
            DataTable dt = DataBaseManagerMSSQL.Return_Counter_Energy_RS_History(counter.SerialNumber, 1, DateTimeNEnergy.Value, DateTimeKEnergy.Value, "'" + energyname + "'");
            if (dt.Rows.Count == 0) return;

            DataGridView historyGrid = new DataGridView();
            this.Controls.Add(historyGrid);

            historyGrid.DataSource = dt;
            
            historyGrid.Dock = DockStyle.Fill;
            historyGrid.ContextMenuStrip = OnlyExportToExcelToolStrip;
            Utils.DoubleBufferGrid(historyGrid, true);
            historyGrid.ReadOnly = true;
            historyGrid.MultiSelect = false;
            historyGrid.BorderStyle = BorderStyle.FixedSingle;
            historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            historyGrid.RowHeadersVisible = false;
            historyGrid.AllowUserToAddRows = false;
            historyGrid.AllowUserToDeleteRows = false;
            historyGrid.AllowUserToOrderColumns = false;
            historyGrid.AllowUserToResizeColumns = false;
            historyGrid.AllowUserToResizeRows = false;
            historyGrid.ScrollBars = ScrollBars.Vertical;
            historyGrid.Columns[0].Visible = false;
            historyGrid.Columns[1].HeaderText = "Дата и время последнего считывания";
            historyGrid.Columns[2].HeaderText = "Сумма";
            historyGrid.Columns[3].HeaderText = "Тариф 1";
            historyGrid.Columns[4].HeaderText = "Тариф 2";
            historyGrid.Columns[5].HeaderText = "Тариф 3";
            historyGrid.Columns[6].HeaderText = "Тариф 4";
            historyGrid.Columns[7].HeaderText = "Наименование";
            historyGrid.EnableHeadersVisualStyles = false;
            historyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
        }

        private void OnlyExport_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl.GetType() == typeof(DataGridView))
            {
                Cursor = Cursors.WaitCursor;
                Utils.ExportDataGrid((DataGridView)this.ActiveControl, "История энергии", 1);
                Cursor = Cursors.Default;
            }
            else
            {
                MessageBox.Show("Перед экспортом нажмите на любую ячейку таблицы и попробуйте снова", "Информация",
                           MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
