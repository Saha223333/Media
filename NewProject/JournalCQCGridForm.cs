using System;
using System.Data;
using System.Drawing;

using System.Windows.Forms;

namespace NewProject
{
    public partial class JournalCQCGridForm : Form
    {
        public JournalCQCGridForm(TreeNode globalSelectedNode, string journalname)
        {
            InitializeComponent();

            ICounter counter = (ICounter)globalSelectedNode.Tag;

            //нужно получить таблицу конкретного журнала по имени
            //var journal = from JournalCQCToRead in counter.JournalCQCToRead where JournalCQCToRead.name == journalname select JournalCQCToRead;
            //ЧТОБЫ ИСПОЛЬЗОВАТЬ LINQ НУЖНО ЧТОБЫ КЛАСС CounterJournalCQCToRead РЕАЛИЗОВАЛ ИНТЕРФЕЙС IEnumerable, НО МНЕ ЛЕНЬ

            //проциклимся по списку журналов чтобы найти нам нужный
            CounterJournalCQCToRead journal = null;

            foreach (CounterJournalCQCToRead j in counter.JournalCQCToRead)
            {
                if (j.name != journalname) continue;
                else
                {
                    journal = j;
                    break;
                }
            }

            this.Text += ": " + journal.name;
            DataTable dt = journal.ValuesTable;

            if (dt.Rows.Count == 0) return;

            DataGridView journalGrid = new DataGridView();
            this.Controls.Add(journalGrid);

            journalGrid.DataSource = dt;

            journalGrid.Dock = DockStyle.Fill;
            journalGrid.ContextMenuStrip = OnlyExportToExcelContext;
            Utils.DoubleBufferGrid(journalGrid, true);
            journalGrid.ReadOnly = true;
            journalGrid.MultiSelect = false;
            journalGrid.BorderStyle = BorderStyle.FixedSingle;
            journalGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            journalGrid.RowHeadersVisible = false;
            journalGrid.AllowUserToAddRows = false;
            journalGrid.AllowUserToDeleteRows = false;
            journalGrid.AllowUserToOrderColumns = false;
            journalGrid.AllowUserToResizeColumns = false;
            journalGrid.AllowUserToResizeRows = false;
            journalGrid.ScrollBars = ScrollBars.Vertical;
            journalGrid.Columns[0].Visible = false;
            journalGrid.Columns[1].Visible = false;
            journalGrid.Columns[2].Visible = false;
            journalGrid.Columns[3].HeaderText = "Дата и время выхода за пределы";
            journalGrid.Columns[4].HeaderText = "Дата и время возврата в пределы";
            journalGrid.EnableHeadersVisualStyles = false;
            journalGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
        }

        private void ExportToExcelButt_Click(object sender, EventArgs e)
        {
            if (this.ActiveControl.GetType() == typeof(DataGridView))
            {
                Cursor = Cursors.WaitCursor;
                Utils.ExportDataGrid((DataGridView)this.ActiveControl, "Журнал ПКЭ", 1);
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
