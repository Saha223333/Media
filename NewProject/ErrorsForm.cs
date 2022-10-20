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
    public partial class ErrorsForm : Form
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

        public ErrorsForm(SearchNodesDelegate del)
        {
            InitializeComponent();
            Utils.DoubleBufferGrid(ConnectionsErrorsGrid, true);
            Utils.DoubleBufferGrid(CountersPLCErrorsGrid, true);
            Utils.DoubleBufferGrid(CountersRSErrorsGrid, true);
            SearchNodesDelegate = del;
        }
       
        private void Redial(DataGridView dgv)
        {//переопрашиваем ошибки                    
           foreach (DataGridViewRow dgvr in dgv.Rows)
           {
              int object_id = Convert.ToInt16(Convert.ToInt16(dgvr.Cells[0].Value));//запоминаем идентификатор 
              SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
           }            
        }

        private void ExportDataGrid(DataGridView dg)
        {        
            CommonReports cr = new CommonReports(dg);//процедура одиночной выгрузки данных для определённого грида            
            DateTime currentDate = DateTime.Now;//запоминаем текущее время для отображения в логе
            cr.ExportToExcel("Ошибки", 1);
            cr.OpenAfterExport();           
            currentDate = DateTime.Now;//запоминаем время после отработки для отображения в логе
        }

        private void RefreshErrorsButt_Click(object sender, EventArgs e)
        {
            RefreshGrid(ConnectionsErrorsGrid);
            RefreshGrid(CountersPLCErrorsGrid);
            RefreshGrid(CountersRSErrorsGrid);
        }

        private void RefreshGrid(DataGridView dgv)
        {
            if (dgv == ConnectionsErrorsGrid)
            {
                ConnectionsErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Connections_Errors(DateFromPicker.Value, DateToPicker.Value);
                Utils.CreateFilterTextBoxes(ConnectionsErrorsGrid);
            }

            if (dgv == CountersPLCErrorsGrid)
            {
                CountersPLCErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Counters_PLC_Errors(DateFromPicker.Value, DateToPicker.Value);
                Utils.CreateFilterTextBoxes(CountersPLCErrorsGrid);
            }

            if (dgv == CountersRSErrorsGrid)
            {
                CountersRSErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Counters_RS_Errors(DateFromPicker.Value, DateToPicker.Value);
                Utils.CreateFilterTextBoxes(CountersRSErrorsGrid);
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

        private void ExportToExcelButt_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl.GetType() == typeof(DataGridView))
            {
                Cursor = Cursors.WaitCursor;
                Utils.ExportDataGrid((DataGridView)this.ActiveControl, "Список ошибок", 1);
                Cursor = Cursors.Default;
            }
            else
            {
                MessageBox.Show("Перед экспортом нажмите на произвольную ячейку таблицы и попробуйте снова", "Информация",
                           MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void ErrorsCounterRSTab_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = CountersRSErrorsGrid;
        }

        private void ErrorsPageConnections_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = ConnectionsErrorsGrid;
        }

        private void ErrorsPageCountersPLC_Enter(object sender, EventArgs e)
        {
            this.ActiveControl = CountersPLCErrorsGrid;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DataGridView grid = null;
            if (ErrorsTabControl.SelectedTab == ErrorsCounterRSTab) grid = CountersRSErrorsGrid;
            if (ErrorsTabControl.SelectedTab == ErrorsPageConnections) grid = ConnectionsErrorsGrid;
            if (ErrorsTabControl.SelectedTab == ErrorsPageCountersPLC) grid = CountersPLCErrorsGrid;
            Redial(grid);
        }

        private void CountersPLCErrorsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                int object_id = Convert.ToInt16(Convert.ToInt16(CountersPLCErrorsGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
                SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
            }
            catch
            {
                return;
            }
        }

        private void ConnectionsErrorsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int object_id = Convert.ToInt16(Convert.ToInt16(ConnectionsErrorsGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
            SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
        }

        private void CountersRSErrorsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int object_id = Convert.ToInt16(Convert.ToInt16(CountersRSErrorsGrid.Rows[e.RowIndex].Cells[0].Value));//запоминаем идентификатор 
            SearchNodesDelegate(object_id);//вызываем делегат поиска по идентификатору в главном дереве
        }

        private void CountersPLCErrorsGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void CountersPLCErrorsGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {//здесь управляем увеличением высоты ячейки по клику
            if (e.RowIndex != -1)//если щелкаем не на заголовок
            {
                if (CountersPLCErrorsGrid.Rows[e.RowIndex].Height < 70)
                    CountersPLCErrorsGrid.Rows[e.RowIndex].Height = 70;
                else
                    CountersPLCErrorsGrid.Rows[e.RowIndex].Height = 22;
            }
        }

        private void ConnectionsErrorsGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ConnectionsErrorsGrid.Columns[0].Visible = false;
            ConnectionsErrorsGrid.Columns[1].HeaderText = "Район";
            ConnectionsErrorsGrid.Columns[2].HeaderText = "Объект";
            ConnectionsErrorsGrid.Columns[3].HeaderText = "Номер";
            ConnectionsErrorsGrid.Columns[4].HeaderText = "Ошибка";
            ConnectionsErrorsGrid.Columns[5].HeaderText = "Комментарий";
            ConnectionsErrorsGrid.Columns[6].HeaderText = "Дата\\время ошибки";
        }

        private void CountersPLCErrorsGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            CountersPLCErrorsGrid.Columns[0].Visible = false;
            CountersPLCErrorsGrid.Columns[1].HeaderText = "Район";
            CountersPLCErrorsGrid.Columns[2].HeaderText = "Объект";
            CountersPLCErrorsGrid.Columns[3].HeaderText = "Точка учёта";
            CountersPLCErrorsGrid.Columns[4].HeaderText = "Адрес";
            CountersPLCErrorsGrid.Columns[5].HeaderText = "Серийный номер";
            CountersPLCErrorsGrid.Columns[6].HeaderText = "Ошибка";
            CountersPLCErrorsGrid.Columns[7].HeaderText = "Сообщение";
            CountersPLCErrorsGrid.Columns[8].HeaderText = "Комментарий";
            CountersPLCErrorsGrid.Columns[9].HeaderText = "Дата\\время ошибки";
            CountersPLCErrorsGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        private void CountersRSErrorsGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            CountersRSErrorsGrid.Columns[0].Visible = false;
            CountersRSErrorsGrid.Columns[1].HeaderText = "Район";
            CountersRSErrorsGrid.Columns[2].HeaderText = "Объект";
            CountersRSErrorsGrid.Columns[3].HeaderText = "Точка учёта";
            CountersRSErrorsGrid.Columns[4].HeaderText = "Адрес";
            CountersRSErrorsGrid.Columns[5].HeaderText = "Серийный номер";
            CountersRSErrorsGrid.Columns[6].HeaderText = "Ошибка";
            CountersRSErrorsGrid.Columns[7].HeaderText = "Комментарий";
            CountersRSErrorsGrid.Columns[8].HeaderText = "Дата\\время ошибки";
        }

        private void ErrorsForm_Shown(object sender, EventArgs e)
        {
            DateToPicker.Value = DateTime.Now;
            DateFromPicker.Value = DateToPicker.Value.AddDays(-7);
            Cursor.Current = Cursors.Arrow;
        }

        private void ErrorsForm_Load(object sender, EventArgs e)
        {
            ConnectionsErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Connections_Errors(DateFromPicker.Value, DateToPicker.Value);
            Utils.CreateFilterTextBoxes(ConnectionsErrorsGrid);
            
            CountersPLCErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Counters_PLC_Errors(DateFromPicker.Value, DateToPicker.Value);
            Utils.CreateFilterTextBoxes(CountersPLCErrorsGrid);
            
            CountersRSErrorsGrid.DataSource = DataBaseManagerMSSQL.Return_Counters_RS_Errors(DateFromPicker.Value, DateToPicker.Value);
            Utils.CreateFilterTextBoxes(CountersRSErrorsGrid);

            this.Location = Properties.Settings.Default.ErrorsFormLocation;
            this.Size = Properties.Settings.Default.ErrorsFormSize;
        }

        private void ConnectionsErrorsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void CountersPLCErrorsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void CountersRSErrorsGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.Index != 0 & e.Column.Tag != null)
            {
                ToolStripTextBox tb = (ToolStripTextBox)e.Column.Tag;//получаем экземпляр поля для ввода
                tb.Width = e.Column.Width;
            }
            else
                return;
        }

        private void ConnectionsErrorsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = false;
        }

        private void CountersPLCErrorsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = false;
        }

        private void CountersRSErrorsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            e.Cancel = false;
        }

        private void CountersRSErrorsGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)CountersRSErrorsGrid.Controls[2];
            bn.SetBounds(0, 20, CountersRSErrorsGrid.Width, 20);
        }

        private void ConnectionsErrorsGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)ConnectionsErrorsGrid.Controls[2];
            bn.SetBounds(0, 20, ConnectionsErrorsGrid.Width, 20);
        }

        private void CountersPLCErrorsGrid_SizeChanged(object sender, EventArgs e)
        {
            BindingNavigator bn = (BindingNavigator)CountersPLCErrorsGrid.Controls[2];
            bn.SetBounds(0, 20, CountersPLCErrorsGrid.Width, 20);
        }

        private void ErrorsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.ErrorsFormLocation = this.Location;
            Properties.Settings.Default.ErrorsFormSize = this.Size;
            Properties.Settings.Default.Save();
        }

        private void FastErrorsReport_Click(object sender, EventArgs e)
        {
            DataTable dt = DataBaseManagerMSSQL.Return_FastErrors_List(3);
            CommonReports cr = new CommonReports(dt);

            cr.ExportToExcel();
            cr.OpenAfterExport();
        }
    }
}
