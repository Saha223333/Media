using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace NewProject
{
    public partial class TasksForm : Form
    {
        private DataTable SchedulesTable;//таблица расписаний
        private DataTable EMailRecieversTable;//таблица получателей почты
        private List<TreeNode> tree = new List<TreeNode>();//дерево для сохранения в классе taskmanager
        private RichTextBox richText = new RichTextBox();//лог хода работы программы
        private LoadTaskDelegate LoadDelegate;//делегат метода загрузки задания, который реализован в главной форме       
        private int taskrowindex;//номер выбранной в данный момент строки в гриде заданий
        private int schedrowindex;//номер выбранной в данный момент строки в гриде расписаний
        private int mailrowindex;//номер выбранной в данный момент строки в гриде получателей почты
        private bool isBindingMode = false;//хранит включен ли режим привязки
        private string tv_type;//тип дерева заданий
        private int profile_export_sum;//определяет сколько периодов интегрирования должно быть суммировано при выгрузке профиля мощности. Значение берётся из поля PeriodNumEdit с вкладки профиля мощности на главной форме

        public TasksForm(List<TreeNode> tree, RichTextBox richText, LoadTaskDelegate del, string ptv_type, int pprofile_export_sum)
        {
            InitializeComponent();
            this.tv_type = ptv_type;
            this.LoadDelegate = del;
            this.richText = richText;
            this.tree = tree;
            this.profile_export_sum = pprofile_export_sum;
            Utils.DoubleBufferGrid(TasksDataGridView, true);
            Utils.DoubleBufferGrid(SchedulesGrid, true);
            Utils.DoubleBufferGrid(ReportsGrid, true);          
        }

        private void RefreshTasksGrid()
        {
            DataBaseManagerMSSQL.Return_Tasks(TasksDataGridView);
        }

        private void RefreshEMailRecieversGrid()
        {
            this.EMailRecieversTable = DataBaseManagerMSSQL.Return_EMail_Recievers();
            //------------------------------------------------------------------------------------------------------------------          
            //сделал ручное заполнение (без привязки к источнику данных) чтобы не было мерцаний по нажатию на строку грида во время режима привязки заданий к получателям почты
            //------------------------------------------------------------------------------------------------------------------
            EMailGirdView.Columns.Clear(); EMailGirdView.Rows.Clear();
            //оформляем грид     
            //добавляем столбцы                  
            DataGridViewColumn dc = new DataGridViewTextBoxColumn(); dc.Name = "id"; dc.ValueType = typeof(int); EMailGirdView.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "email"; dc.ValueType = typeof(string); EMailGirdView.Columns.Add(dc);
            EMailGirdView.Columns[0].Visible = false;
            EMailGirdView.Columns[1].HeaderText = "Получатели почты";
            EMailGirdView.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            EMailGirdView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            EMailGirdView.EnableHeadersVisualStyles = false;
            //помещаем в грид данные из таблицы
            foreach (DataRow row in EMailRecieversTable.Rows)
            {
                DataGridViewRow dg = new DataGridViewRow(); dg.CreateCells(EMailGirdView);

                dg.Cells[0].Value = row["id"];
                dg.Cells[1].Value = row["email"];

                EMailGirdView.Rows.Add(dg);
            }

            if (EMailGirdView.Rows.Count == 0)
            {
                DeleteScheduleButt.Enabled = false;
            }
            else
            {
                DeleteScheduleButt.Enabled = true;
            }
        }

        private void RefreshSchedulesGrid()
        {
            this.SchedulesTable = DataBaseManagerMSSQL.Return_Schedules();//вытаскиваеми список расписаний
            //------------------------------------------------------------------------------------------------------------------          
            //сделал ручное заполнение (без привязки к источнику данных) чтобы не было мерцаний по нажатию на строку грида во время режима привязки заданий к расписаниям
            //------------------------------------------------------------------------------------------------------------------
            SchedulesGrid.Columns.Clear(); SchedulesGrid.Rows.Clear();
            //оформляем грид     
            //добавляем столбцы                  
            DataGridViewColumn dc = new DataGridViewTextBoxColumn(); dc.Name = "id"; dc.ValueType = typeof(int); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "day_of_month"; dc.ValueType = typeof(int); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "day_of_week"; dc.ValueType = typeof(int); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "time"; dc.ValueType = typeof(TimeSpan); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "start_date "; dc.ValueType = typeof(DateTime); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "times_repeat"; dc.ValueType = typeof(int); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewTextBoxColumn(); dc.Name = "name"; dc.ValueType = typeof(string); SchedulesGrid.Columns.Add(dc);
            dc = new DataGridViewCheckBoxColumn(); dc.Name = "active"; dc.ValueType = typeof(bool); SchedulesGrid.Columns.Add(dc);

            SchedulesGrid.Columns[0].Visible = false;
            SchedulesGrid.Columns[1].HeaderText = "День месяца";
            SchedulesGrid.Columns[2].HeaderText = "День недели";
            SchedulesGrid.Columns[3].HeaderText = "Время";
            SchedulesGrid.Columns[4].HeaderText = "Старт";
            SchedulesGrid.Columns[5].HeaderText = "Повторы";
            SchedulesGrid.Columns[6].HeaderText = "Наименование";
            SchedulesGrid.Columns[7].HeaderText = "Активно";
            SchedulesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            SchedulesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SchedulesGrid.EnableHeadersVisualStyles = false;
            //помещаем в грид данные из таблицы
            foreach (DataRow row in SchedulesTable.Rows)
            {
                DataGridViewRow dg = new DataGridViewRow(); dg.CreateCells(SchedulesGrid);

                dg.Cells[0].Value = row["id"]; dg.Cells[1].Value = row["day_of_month"]; dg.Cells[2].Value = row["day_of_week"];
                dg.Cells[3].Value = row["time"]; dg.Cells[4].Value = row["start_date"];
                dg.Cells[5].Value = row["times_repeat"]; dg.Cells[6].Value = row["name"]; dg.Cells[7].Value = row["active"];

                SchedulesGrid.Rows.Add(dg);
            }

            if (SchedulesTable.Rows.Count == 0)
            {
                UpdateScheduleButt.Enabled = false;
                DeleteScheduleButt.Enabled = false;
            }
            else
            {
                UpdateScheduleButt.Enabled = true;
                DeleteScheduleButt.Enabled = true;
            }
        }

        private void CloseFormButt_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateTask_Click(object sender, EventArgs e)
        {//процедура  сохранения сетки задания
            if ((tree == null) || (tree.Count == 0)) return;
            
            if (taskrowindex < 0) return;//если не выбрано ни одно задание
            int task_id = Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value);//запоминаем идентификатор задания        
            //далее идёт обновление сетки задания, если входное дерево не пустое
            //сначала сформируем коллекцию из всех счётчиков в дереве задания (шлюзы и концентраторы в задание не идут)
            List<TreeNode> list = new List<TreeNode>();
            foreach (TreeNode parentNode in tree)
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    if (tv_type == "rs485") { list.Add(childNode); continue; }//если витая пара, то добавляем счётчик в список и идём дальше
                    if (tv_type == "plc")//если PLC, то идём на дочерние узлы концентраторов
                        foreach (TreeNode grandChildNode in childNode.Nodes)
                        {
                            list.Add(grandChildNode);
                        }
                }
            }

            try
            {
                TaskManager.richText = richText;
                TaskManager tm = new TaskManager();
                TaskManager.tree = list;
                TaskManager.OverwriteTree(task_id, this.tv_type);//процедура перезаписи (сохранения) сетки задания                              
            }
            catch (Exception ex2)
            {
                MessageBox.Show(ex2.Message, "Ошибка сохранения задания", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime currentDate = DateTime.Now;
            //пишем действие в лог
            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Задание " + TasksDataGridView.Rows[taskrowindex].Cells[1].Value.ToString() + " сохранено" + "\r");
            richText.ScrollToCaret();
            this.Close();
        }

        private void LoadTaskButt_Click(object sender, EventArgs e)
        {
            if (LoadDelegate == null) return;

            if (taskrowindex < 0) return;//если не выбрано ни одно задание
            int task_id = Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value);//запоминаем идентификатор задания
            LoadDelegate(task_id, ProgressBar);//вызываем делегат загрузки (метод реализован в главной форме)
            this.Close();
        }

        private void DeleteTaskButt_Click(object sender, EventArgs e)
        {
            if (taskrowindex < 0) return;//если не выбрано ни одно задание

            DialogResult result = MessageBox.Show("Точно удалить?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;

            int task_id = Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value);//запоминаем идентификатор задания
            DataBaseManagerMSSQL.Delete_Task_Row(task_id);
            RefreshTasksGrid();
        }

        private void Return_Binded_Schedules()
        {
            if (isBindingMode == true)
            //если режим привязки
            //возвращаем набор привязанных к текущему заданий расписания и подсвечиваем их в гриде расписаний
            {
                //запоминаем текущий список расписаний в гриде
                //убираем подкраску строк
                foreach (DataGridViewRow row in SchedulesGrid.Rows)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }

                List<DataGridViewRow> rows = new List<DataGridViewRow>(SchedulesGrid.Rows.Cast<DataGridViewRow>());//запоминаем список               
                SchedulesGrid.Rows.Clear();//очищаем грид расписаний
                //возвращаем набор привязанных к текущему заданию расписаний
                DataTable dt = DataBaseManagerMSSQL.Return_Binded_Schedules(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value));
                //циклимся по полученной таблице
                foreach (DataRow table_row in dt.Rows)
                    //циклимся по ранее запомненному списку из грида расписаний
                    foreach (DataGridViewRow schedule_row in rows)//если находим свзяь, строку подкрашиваем
                        if (schedule_row.Cells[0].Value.ToString().Equals(table_row["id_schedule"].ToString()))
                        {
                            schedule_row.DefaultCellStyle.BackColor = Color.Green;
                            schedule_row.DefaultCellStyle.ForeColor = Color.White;
                        }
                //возвращаем отработанный список в грид расписаний (с подкрашенными строками)
                SchedulesGrid.Rows.AddRange(rows.ToArray());
            }
        }

        private void Return_Binded_Mail_Recievers()
        {
            if (isBindingMode == true)
            //если режим привязки
            //возвращаем набор привязанных к текущему заданию получателей почты и подсвечиваем их в гриде
            {
                //запоминаем текущий список получателей почты в гриде
                //убираем подкраску строк
                foreach (DataGridViewRow row in EMailGirdView.Rows)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }

                List<DataGridViewRow> rows = new List<DataGridViewRow>(EMailGirdView.Rows.Cast<DataGridViewRow>());//запоминаем список               
                EMailGirdView.Rows.Clear();//очищаем грид получателей почты
                //возвращаем набор привязанных к текущему заданию получателей почты
                DataTable dt = DataBaseManagerMSSQL.Return_Binded_Mail_Recievers(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value));
                //циклимся по полученной таблице
                foreach (DataRow table_row in dt.Rows)
                    //циклимся по ранее запомненному списку из грида получателей почты
                    foreach (DataGridViewRow mail_row in rows)//если находим свзяь, строку подкрашиваем
                        if (mail_row.Cells[0].Value.ToString().Equals(table_row["id_reciever"].ToString()))
                        {
                            mail_row.DefaultCellStyle.BackColor = Color.Green;
                            mail_row.DefaultCellStyle.ForeColor = Color.White;
                        }
                //возвращаем отработанный список в грид расписаний (с подкрашенными строками)
                EMailGirdView.Rows.AddRange(rows.ToArray());
            }
        }

        private void Return_Binded_Reports()
        {//возвращаем список привязанных к заданию отчётов
            DataTable dt = DataBaseManagerMSSQL.Return_Task_Reports(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value));
            ReportsGrid.DataSource = dt;

            ReportsGrid.Columns[0].Visible = false;
            ReportsGrid.Columns[1].HeaderText = "Наименование";
            ReportsGrid.Columns[2].HeaderText = "Суммирование периодов интегрирования при выгрузке профиля";
            ReportsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            ReportsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ReportsGrid.EnableHeadersVisualStyles = false;
        }

        private void CreateScheduleButt_Click(object sender, EventArgs e)
        {
            try
            {
                int sched_id = DataBaseManagerMSSQL.Create_Schedule_Row("Новое расписание");
                RefreshSchedulesGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка создания расписания", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void SchedulesGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            this.schedrowindex = e.RowIndex;//запоминаем номер строки в гриде расписаний

            if (isBindingMode == true)// && taskrowindex > -1)
            {
                //если режим привязки
                //то по клику на строку привязываем или отвязываем кликнутое расписание к текущему заданию из грида заданий
                DataBaseManagerMSSQL.Bind_Schedule(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value),
                                                   Convert.ToInt16(SchedulesGrid.Rows[schedrowindex].Cells[0].Value));

                Return_Binded_Schedules();
            }

            SchedulesGrid.Controls.Clear();//очищаем грид от всех контролов                     
            if (e.ColumnIndex == 4)//добавляем датапикер в ячейку грида
            {//убеждаемся что ячейки те, которые нам нужны
                if (e.RowIndex > -1)//исключаем заголовок
                {
                    SchedulesGrid.AllowUserToResizeColumns = false;//запрещаем менять размер столбцов пока календарь активен   
                    SchedulesGrid.AllowUserToResizeRows = false;//запрещаем менять размер строк пока календарь активен  
                    DateTimePicker oDateTimePicker = new DateTimePicker();//создаём новый календарь
                    SchedulesGrid.Controls.Add(oDateTimePicker);//добавляем календарь к гриду
                    oDateTimePicker.CustomFormat = "dd.MM.yyyy HH:mm";
                    oDateTimePicker.Format = DateTimePickerFormat.Custom;
                    Rectangle oRectangle = SchedulesGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);//область контрола
                    oDateTimePicker.Size = new Size(oRectangle.Width, oRectangle.Height);//размеры
                    oDateTimePicker.Location = new Point(oRectangle.X, oRectangle.Y);//координаты
                    oDateTimePicker.CloseUp += new EventHandler(oDateTimePicker_CloseUp);//событие сворачивания
                    oDateTimePicker.VisibleChanged += new EventHandler(oDateTimePicker_VisibleChanged);//событие изменения видимости
                    oDateTimePicker.TextChanged += new EventHandler(oDateTimePicker_OnTextChange);//событие изменения текста
                    oDateTimePicker.Visible = true;//делаем календарь видимым
                }
            }
        }

        private void oDateTimePicker_OnTextChange(object sender, EventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            SchedulesGrid.CurrentCell.Value = dtp.Text.ToString();
        }

        private void oDateTimePicker_VisibleChanged(object sender, EventArgs e)
        {//если календарь скрылся, то разрешаем пользователю менять размер столбцов
            DateTimePicker dtp = (DateTimePicker)sender;
            if (dtp.Visible == false)
            {
                SchedulesGrid.AllowUserToResizeColumns = true;
                SchedulesGrid.AllowUserToResizeRows = true;
            }
        }

        private void oDateTimePicker_CloseUp(object sender, EventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            dtp.Visible = false;//свернули календарь, значит отработали и скрыли его            
        }

        private void UpdateScheduleButt_Click(object sender, EventArgs e)
        {
            //в цикле сохраняем весь грид расписаний
            foreach (DataGridViewRow dgvr in SchedulesGrid.Rows) UpdateSchedule(Convert.ToInt16(dgvr.Cells[0].Value), dgvr.Index);

            RefreshSchedulesGrid();
        }

        private void UpdateSchedule(int schedule_id, int rowidex)
        {
            //вызываем процедуру обновления
            try
            {
                Exception ex = DataBaseManagerMSSQL.Update_Schedule(SchedulesGrid.Rows[rowidex].Cells[6].Value.ToString(),
                                                    Convert.ToInt16(SchedulesGrid.Rows[rowidex].Cells[1].Value),
                                                    Convert.ToInt16(SchedulesGrid.Rows[rowidex].Cells[2].Value),
                                                    Convert.ToDateTime(SchedulesGrid.Rows[rowidex].Cells[3].Value.ToString()),
                                                    Convert.ToDateTime(SchedulesGrid.Rows[rowidex].Cells[4].Value.ToString()),
                                                    Convert.ToInt16(SchedulesGrid.Rows[rowidex].Cells[5].Value),
                                                    Convert.ToByte(SchedulesGrid.Rows[rowidex].Cells[7].Value),
                                                    schedule_id);

                if (ex != null) RefreshSchedulesGrid();
            }
            catch
            {
                RefreshSchedulesGrid();
            }
        }

        private void SchedulesGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            SchedulesGrid.CancelEdit();
        }

        private void TasksDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            TasksDataGridView.CancelEdit();
        }

        private void DeleteScheduleButt_Click(object sender, EventArgs e)
        {
            DeleteSchedule();
        }

        private void DeleteSchedule()
        {
            if (User.Role == "reader") return;//пользователю с этой ролью не положено удалять расписания

            DialogResult result = MessageBox.Show("Точно удалить?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;

            int schedule_id = Convert.ToInt16(SchedulesGrid.Rows[schedrowindex].Cells[0].Value);//запоминаем идентификатор расписания
            DataBaseManagerMSSQL.Delete_Schedule_Row(schedule_id);//вызываем процедуру удаления
            RefreshSchedulesGrid();//обновляем грид
        }

        private void TasksDataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.Handled = true;
        }

        private void TasksForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt)
            {
                EnterBindingMode();
            }
        }

        private void TasksForm_KeyUp(object sender, KeyEventArgs e)
        {
            EnterEditingMode();
        }

        private void EnterEditingMode()
        {//ввод форму в режим редактирования
            isBindingMode = false;

            TasksDataGridView.ReadOnly = false;
            SchedulesGrid.ReadOnly = false;
            EMailGirdView.ReadOnly = false;

            TasksDataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            SchedulesGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            EMailGirdView.SelectionMode = DataGridViewSelectionMode.CellSelect;

            TasksDataGridView.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;

            SchedulesGrid.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            SchedulesGrid.DefaultCellStyle.ForeColor = Color.Black;

            EMailGirdView.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
            EMailGirdView.DefaultCellStyle.ForeColor = Color.Black;
            //после того как вышли из режима привязки убираем подкраску строк
            foreach (DataGridViewRow row in SchedulesGrid.Rows)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;
            }
            //после того как вышли из режима привязки убираем подкраску строк
            foreach (DataGridViewRow row in EMailGirdView.Rows)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.ForeColor = Color.Black;
            }

            LoadTaskButt.Enabled = true; //UpdateTaskButt.Enabled = true;
            DeleteTaskButt.Enabled = true;
            CloseFormButt.Enabled = true;

            CreateEMailRecieverButt.Enabled = true;
            DeleteEMailRecieverButt.Enabled = true;

            CreateScheduleButt.Enabled = true;
            UpdateScheduleButt.Enabled = true;
            DeleteScheduleButt.Enabled = true;
        }

        private void EnterBindingMode()
        {//вводит форму в режим привязки расписаний
            if (TasksDataGridView.CurrentRow == null) return;
            TasksDataGridView.CurrentRow.Selected = true;

            isBindingMode = true;

            TasksDataGridView.ReadOnly = true;
            SchedulesGrid.ReadOnly = true;
            EMailGirdView.ReadOnly = true;

            TasksDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            TasksDataGridView.DefaultCellStyle.SelectionBackColor = Color.Green;

            LoadTaskButt.Enabled = false; UpdateTaskButt.Enabled = false;
            DeleteTaskButt.Enabled = false; CloseFormButt.Enabled = false;

            CreateScheduleButt.Enabled = false;
            UpdateScheduleButt.Enabled = false;
            DeleteScheduleButt.Enabled = false;

            CreateEMailRecieverButt.Enabled = false;
            DeleteEMailRecieverButt.Enabled = false;
        }

        private void TasksDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (TasksDataGridView.CurrentRow == null) return;

            this.taskrowindex = TasksDataGridView.CurrentRow.Index;//запоминаем номер строки в гриде заданий
            Return_Binded_Schedules();//возвращаем набор привязанных к текущему заданию расписаний и подсвечиваем их в гриде расписаний
            Return_Binded_Reports();//возвращаем набор привязанных к текущему заданию отчётов
            Return_Binded_Mail_Recievers();//возвращаем набор привязанных к текущему заданию получателей почты
        }

        private void SchedulesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (isBindingMode == true) SchedulesGrid.ClearSelection();
        }

        private void SchedulesGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.Handled = true;
            if (e.KeyCode == Keys.Delete) DeleteSchedule();
        }

        private void SchedulesGrid_Leave(object sender, EventArgs e)
        {
            foreach (Control cntrl in SchedulesGrid.Controls)
            {//когда выходим из грида скрываем все имеющиеся контролы
                if (cntrl.GetType() == typeof(DateTimePicker))
                {
                    SchedulesGrid.CurrentCell.Value = cntrl.Text.ToString();
                    cntrl.Visible = false;
                }
            }
        }

        private void TasksForm_Load(object sender, EventArgs e)
        {
            switch (User.Role)
            {             
                case "reader"://запрещаем низкоранговым пользователям видеть кнопки
                    {
                        SchedulesButtLayout.Visible = false;
                        EMailButtsLayout.Visible = false;
                        break;
                    }                            
            }
            RefreshTasksGrid();//обновление грида заданий
            RefreshSchedulesGrid();//обновление грида расписаний    
            RefreshEMailRecieversGrid();//обновление грида получателей почты
        }

        private void AddReportToTaskButt_Click(object sender, EventArgs e)
        {
            if (TasksDataGridView.CurrentRow == null) return;
            if (ReportTypesComboBox.Text == String.Empty) return;

            DataBaseManagerMSSQL.Add_Task_Reports(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value), ReportTypesComboBox.Text, this.profile_export_sum);
            Return_Binded_Reports();
        }

        private void DeleteReportFromTasKbutt_Click(object sender, EventArgs e)
        {
            if (ReportsGrid.CurrentRow == null) return;

            DataBaseManagerMSSQL.Delete_Task_Report(Convert.ToInt16(ReportsGrid.Rows[ReportsGrid.CurrentCell.RowIndex].Cells[0].Value), 
                                                                    ReportsGrid.Rows[ReportsGrid.CurrentCell.RowIndex].Cells[1].Value.ToString());
            Return_Binded_Reports();
        }

        private void TasksDataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            TasksDataGridView.Columns[0].Visible = false;
            TasksDataGridView.Columns[1].HeaderText = "Наименование";
            TasksDataGridView.Columns[2].HeaderText = "Комментарий";
            TasksDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            TasksDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            TasksDataGridView.EnableHeadersVisualStyles = false;
        }

        private void TasksForm_Shown(object sender, EventArgs e)
        {
            if ((tree == null) || (tree.Count == 0))
            {
                UpdateTaskButt.Enabled = false;
            }
        }

        private void TasksDataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                SqlDataAdapter da = (SqlDataAdapter)TasksDataGridView.Tag;//возвращаем SqlDataAdapter
                da.UpdateCommand.Connection = connection;//воскрешаем Connection т.к. оно ранее было убито by Using
                BindingSource bs = (BindingSource)TasksDataGridView.DataSource;//возвращаем BindingSource
                da.Update((DataTable)bs.DataSource);//вызываем метод обновления (он актуализирует данные из грида в базу: возьмёт строки со статусом Modified)
            }
        }

        private void CreateEMailRecieverButt_Click(object sender, EventArgs e)
        {
            try
            {
                DataBaseManagerMSSQL.Create_MailReciever_Row(EMailRecieverTextBox.Text);
                EMailRecieverTextBox.Text = String.Empty;
                RefreshEMailRecieversGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка создания получателя почты", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void EMailGirdView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            this.mailrowindex = e.RowIndex;//запоминаем номер строки в гриде получателей почты

            if (isBindingMode == true)
            {
                //если режим привязки
                //то по клику на строку привязываем или отвязываем кликнутого получателя почты к текущему заданию из грида заданий
                DataBaseManagerMSSQL.Bind_Mail_Recievers(Convert.ToInt16(TasksDataGridView.Rows[taskrowindex].Cells[0].Value),
                                                   Convert.ToInt16(EMailGirdView.Rows[mailrowindex].Cells[0].Value));

                Return_Binded_Mail_Recievers();
            }
        }

        private void EMailGirdView_SelectionChanged(object sender, EventArgs e)
        {
            if (isBindingMode == true) EMailGirdView.ClearSelection();
        }

        private void TasksDataGridView_CellValidated(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
