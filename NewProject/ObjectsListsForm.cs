using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;

namespace NewProject
{
    public partial class ObjectsListsForm : Form
    {
        private SearchNodesDelegate SearchNodesDelegate;//делегат метода поиска подключений по идентификатору в главном дереве, который реализован в главной форме  
        protected override CreateParams CreateParams
        {//помогает ускорить прорисовку формы
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | 0x2000000;
                return cp;
            }
        }

        public ObjectsListsForm(SearchNodesDelegate del)
        {
            InitializeComponent();
            SearchNodesDelegate = del;
            Utils.DoubleBufferGrid(CountersPLCListGrid, true);
            Utils.DoubleBufferGrid(ConnectionsListGrid, true);
            Utils.DoubleBufferGrid(CountersRSListGrid, true);
            
        }

        private void RefreshListsButt_Click(object sender, EventArgs e)
        {
            RefreshGrid(ConnectionsListGrid);
            RefreshGrid(CountersPLCListGrid);
            RefreshGrid(CountersRSListGrid);
        }

        private void RefreshGrid(DataGridView dgv)
        {
            if (dgv == ConnectionsListGrid)
            {
                ConnectionsListGrid.DataSource = DataBaseManagerMSSQL.Return_Connections_List();
                Utils.CreateFilterTextBoxes(ConnectionsListGrid);
            }

            if (dgv == CountersPLCListGrid)
            {
                CountersPLCListGrid.DataSource = DataBaseManagerMSSQL.Return_CounterPLC_List();
                Utils.CreateFilterTextBoxes(CountersPLCListGrid);
            }

            if (dgv == CountersRSListGrid)
            {
                CountersRSListGrid.DataSource = DataBaseManagerMSSQL.Return_CounterRS_List();
                Utils.CreateFilterTextBoxes(CountersRSListGrid);
            }
            //при возврате гридов в исходное состояние опустошаем поля для ввода
            foreach (Control control in dgv.Controls)
            {
                if (control.GetType() == typeof(TextBox))
                {
                    TextBox textbox = (TextBox)control;
                    textbox.Text = String.Empty;
                }
            }
        }

        private void ConnectionsListGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int object_id = Convert.ToInt16(Convert.ToInt16(ConnectionsListGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
                SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
            }
            catch
            {
                return;
            }
        }

        private void CountersPLCListGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int object_id = Convert.ToInt16(Convert.ToInt16(CountersPLCListGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
                SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
            }
            catch
            {
                return;
            }
        }

        private void CountersRSListGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int object_id = Convert.ToInt16(Convert.ToInt16(CountersRSListGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
                SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
            }
            catch
            {
                return;
            }
        }

        private void GoToTaskAll_Click(object sender, EventArgs e)
        {
            ProgressBar.Value = 0;
            DataGridView grid = null;
            if (ObjectListsTabControl.SelectedTab == ConnectionsListTab) grid = ConnectionsListGrid;
            if (ObjectListsTabControl.SelectedTab == CountersPLCListTab) grid = CountersPLCListGrid;
            if (ObjectListsTabControl.SelectedTab == CountersRSListTab) grid = CountersRSListGrid;
            ProgressBar.Maximum = grid.Rows.Count;
            GoToTaskAll(grid);
        }

        private void GoToTaskAll(DataGridView dgv)
        {
            foreach (DataGridViewRow dgvr in dgv.Rows)
            {
                int object_id = Convert.ToInt16(Convert.ToInt16(dgvr.Cells[0].Value));//запоминаем идентификатор 
                SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
                ProgressBar.Value += 1;
            }
        }

        private void ConnectionsListTab_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = ConnectionsListGrid;
        }

        private void CountersPLCListTab_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = CountersPLCListGrid;
        }

        private void CountersRSListTab_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = CountersRSListGrid;
        }

        private void ConnectionsListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                RefreshGrid(ConnectionsListGrid);
            }
        }

        private void CountersPLCListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                RefreshGrid(CountersPLCListGrid);
            }
        }

        private void CountersRSListGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                RefreshGrid(CountersRSListGrid);
            }
        }

        private void CountersPLCListGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = false;
        }

        private void ConnectionsListGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ConnectionsListGrid.Columns[0].Visible = false;
            ConnectionsListGrid.Columns[1].HeaderText = "Район";
            ConnectionsListGrid.Columns[2].HeaderText = "Наименование";
            ConnectionsListGrid.Columns[3].HeaderText = "Номер";
            ConnectionsListGrid.Columns[4].HeaderText = "Улица";
            ConnectionsListGrid.Columns[5].HeaderText = "Комментарий";
        }

        private void CountersPLCListGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            CountersPLCListGrid.Columns[0].Visible = false;
            CountersPLCListGrid.Columns[1].HeaderText = "Район";
            CountersPLCListGrid.Columns[2].HeaderText = "ТП";
            CountersPLCListGrid.Columns[3].HeaderText = "Объект";
            CountersPLCListGrid.Columns[4].HeaderText = "Улица";
            CountersPLCListGrid.Columns[5].HeaderText = "Дом";
            CountersPLCListGrid.Columns[6].HeaderText = "Серийный номер";
            CountersPLCListGrid.Columns[7].HeaderText = "Сетевой";
            CountersPLCListGrid.Columns[8].HeaderText = "П\\П, сумма";
            CountersPLCListGrid.Columns[9].HeaderText = "Дата П\\П, сумма";
            CountersPLCListGrid.Columns[10].HeaderText = "Комментарий";
        }

        private void CountersRSListGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            CountersRSListGrid.Columns[0].Visible = false;
            CountersRSListGrid.Columns[1].HeaderText = "Район";
            CountersRSListGrid.Columns[2].HeaderText = "ТП";
            CountersRSListGrid.Columns[3].HeaderText = "Объект";
            CountersRSListGrid.Columns[4].HeaderText = "Улица";
            CountersRSListGrid.Columns[5].HeaderText = "Дом";
            CountersRSListGrid.Columns[6].HeaderText = "Серийный номер";
            CountersRSListGrid.Columns[7].HeaderText = "Сетевой";
            CountersRSListGrid.Columns[8].HeaderText = "Дата П\\П, сумма";
            CountersRSListGrid.Columns[9].HeaderText = "Комментарий";
        }

        private void CountersRSListGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = false;
        }

        private void ObjectsListsForm_Load(object sender, EventArgs e)
        {
            {
                ConnectionsListGrid.DataSource = DataBaseManagerMSSQL.Return_Connections_List();//привязываем данные к гриду           
                //DataBaseManagerMSSQL.Return_Connections_List(ConnectionsListGrid);
                Utils.CreateFilterTextBoxes(ConnectionsListGrid);
            }

            {
                CountersPLCListGrid.DataSource = DataBaseManagerMSSQL.Return_CounterPLC_List();//привязываем данные к гриду
                Utils.CreateFilterTextBoxes(CountersPLCListGrid);
            }

            {
                CountersRSListGrid.DataSource = DataBaseManagerMSSQL.Return_CounterRS_List();//привязываем данные к гриду
                Utils.CreateFilterTextBoxes(CountersRSListGrid);
            }

            this.Location = Properties.Settings.Default.ObjectsListFormLocation;
            this.Size = Properties.Settings.Default.ObjectsListFormSize;

            RefreshListsButt.PerformClick();
        }

        private void CountersPLCListGrid_ColumnWidthChanged_1(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void ConnectionsListGrid_ColumnWidthChanged_1(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void CountersRSListGrid_ColumnWidthChanged_1(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void ConnectionsListGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)ConnectionsListGrid.Controls[2];
            bn.SetBounds(0, 20, ConnectionsListGrid.Width, 20);
        }

        private void CountersPLCListGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)CountersPLCListGrid.Controls[2];
            bn.SetBounds(0, 20, CountersPLCListGrid.Width, 20);
        }

        private void CountersRSListGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)CountersRSListGrid.Controls[2];
            bn.SetBounds(0, 20, CountersRSListGrid.Width, 20);
        }

        private void ExportToExcelButt_Click_1(object sender, EventArgs e)
        {
            if (this.ActiveControl.GetType() == typeof(DataGridView))
            {
                Cursor = Cursors.WaitCursor;
                Utils.ExportDataGrid((DataGridView)this.ActiveControl, "Список объектов", 1);
                Cursor = Cursors.Default;
            }
            else
            {
                MessageBox.Show("Перед экспортом нажмите на любую ячейку таблицы и попробуйте снова", "Информация",
                           MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void ConnectionsListGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //using (SqlConnection connection = new SqlConnection(DataBaseManagerMSSQL.connStr))
            //{
            //    SqlDataAdapter da = (SqlDataAdapter)ConnectionsListGrid.Tag;
            //    da.UpdateCommand.Connection = connection;
            //    BindingSource bs = (BindingSource)ConnectionsListGrid.DataSource;
            //    da.Update((DataTable)bs.DataSource);
            //}
        }

        private void ObjectsListsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.ObjectsListFormLocation = this.Location;
            Properties.Settings.Default.ObjectsListFormSize = this.Size;
            Properties.Settings.Default.Save();
        }
    }
}
