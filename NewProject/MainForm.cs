using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Data;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using System.IO.Ports;
using System.IO;
using System.Xml.Linq;
using System.Drawing.Imaging;
using System.Net.Mail;
using System.Text;

namespace NewProject
{
    public delegate void LoadTaskDelegate(int task_id, ProgressBar tspb);//делегат для передачи метода загрузки задания в форму заданий. Метод реализован в главной форме
    public delegate void SearchNodesDelegate(int id);//делегат для передачи метода поиска подключений в главном дереве на форму просмотра ошибок. Метод реализован в главной форме

    public partial class MainForm : Form
    {
        List<TreeNode> selectedNodesListGlobal = new List<TreeNode>(); //этот список отводится для мультивыбора в дереве (глобальный на всю форму. Влияет на ручную загрузку задания. На загрузку по расписанию влиять никак не должен)
        TreeNode firstNode;//"первый" узел для мультвыбора в дереве с шифтом       
        TreeNode globalSelectedNode;//узел, выбранный в любом из деревьев.        
        TabPage selectedTabPage; //храним здесь выбранную вкладку для быстрого переключения
        System.Timers.Timer TaskReadingTimer = new System.Timers.Timer();//таймер для опроса по расписаниям
        BackgroundWorker globalWorkerExportToExcel = null;//глобальный экспортер в эксель. Нужен чтобы выполнять его в отдельном потоке с возможностью остановить 
        DataTable DistrictsList;//справочник районов
        DataTable StreetsList;//справочник улиц
        //DataGridViewComboBoxCell CBSTComboBox;//список строк инициализации модема
        DataGridViewComboBoxCell DistrictComboBox;//выпадающий список районов из справочника
        DataGridViewComboBoxCell StreetComboBox;//выпадающий список улиц из справочника        

        SerialPort global_sp = null;//глобальный серийный порт, используемый для ручной работы с дозвоном
      
        private struct TaskStruct
        {//структура с параметрами задания, которую будем передавать в BackgroundWorker для выполнения задания в отдельном потоке
            public List<TreeNode> tree;//дерево
            public int task_id;//номер задания
            public bool get_profile;//здесь храним, снимается ли профиль в задании
            public DateTime lower_datetime; //здесь храним нижнюю границу профиля для опроса (в т.ч. для определения месяца интегрального акта)
            public DateTime upper_datetime; //здесь храним верхнюю границу профиля для опроса
            public int periodsCount;//здесь хранится количество периодов для суммирования после снятия профиля
            public string name;//наименование
            public bool closeAfterDoWork;//определяет закрываем ли окно лога после опроса (нужно чтобы не плодить их во время опроса по расписанию)         
            public bool ReReadAbsentRecords;//определяет, работает ли чтение профиля в режиме дочитывания недостающих записей

            public TaskStruct(List<TreeNode> ptree, int ptask_id, bool pget_profile, DateTime plower_datetime, DateTime pupper_datetime, int pperiods_count, string pname, bool pclose, bool prereadprof)
            {
                this.tree = ptree;
                this.task_id = ptask_id;
                this.get_profile = pget_profile;
                this.lower_datetime = plower_datetime;
                this.upper_datetime = pupper_datetime;
                this.periodsCount = pperiods_count;
                this.name = pname;
                this.closeAfterDoWork = pclose;
                this.ReReadAbsentRecords = prereadprof;
            }           
        }

        public MainForm()
        {
            InitializeComponent();
            //включаем двойную буферизацию гридов
            Utils.DoubleBufferGrid(DevicePropertiesGrid, true);
            Utils.DoubleBufferGrid(DeviceEnergyGrid, true);
            Utils.DoubleBufferGrid(DeviceParametersGrid, true);
            Utils.DoubleBufferGrid(DeviceJournalGrid, true);
            Utils.DoubleBufferGrid(PowerProfileGrid, true);
            Utils.DoubleBufferGrid(DeviceMonitorGrid, true);
            Utils.DoubleBufferGrid(WriteParametersGrid, true);

            StopButton.Click += new EventHandler(StopExportingToExcel);//к нажатию кнопки Stop привязываем отмену экспорта в Excel
            FullTree.DragOver += new DragEventHandler(DragScroll);//к событию перетаскивания узла привязываем метод, позволяющий скроллить во время перетаскивания
            UserNameTextBox.Text = Environment.UserName;//подкидываем имя текущего доменного пользователя в поле для ввода с именем для захода на сервер
            //открываем пожключение к базе(по сути просто задаём строку подключения)
            if (DataBaseManagerMSSQL.ConnectToDB(ServNameTextBox.Text, DbNameTextBox.Text, UserNameTextBox.Text, PwdTextBox.Text) == false)
            {
                MessageBox.Show("Подключение к базе данных не удалось! Выходим из приложения.", "Ошибка подключения к базе данных",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(Environment.ExitCode);//выходим из приложения
            }

            ////в самом начале смотрим, есть ли хоть один логин с правами администратора программы
            DataBaseManagerMSSQL.Whether_Admin_Exists();
            User.Identify_User();//определяем пользователя
            this.Text = User.Login + " / " + User.Role;//пишем имя и роль пользователя в заголовок формы

            TaskReadingTimer.Interval = 1000;//таймер отрабатывает каждую секунду
            TaskReadingTimer.Elapsed += TaskReadingTimer_Elapsed;
            TaskReadingTimer.Enabled = false;

            DateTimeKEnergy.Value = DateTime.Now;
            DateTimeNEnergy.Value = DateTimeKEnergy.Value.AddDays(-7);

            DistrictsList = DataBaseManagerMSSQL.Return_Districts();//получаем справочник районов
            StreetsList = DataBaseManagerMSSQL.Return_Streets();//получаем справочник улиц

            HideTabPage(ReadJournalCQCPage);//не нашёл где вкладки скрываются при загрузке, поэтому запихнул эту строку сюда
        }

        private void TaskReadingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Invoke(new Action(delegate 
            {
                SystemDateTimeLabel.Text = DateTime.Now.ToString();
            }));
            //проверяем текущее время на предмет начала суток. Если сутки начались - пришло время перезагрузить главное дерево для того, чтобы обновить экземпляры счётчиков в оперативной памяти
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 10 && DateTime.Now.Second == 1)
            {
                if (this.ConnectToDataBase() == false)
                {
                    //отправляем письмо с сообщением об ошибке
                    //string[] recips = new string[1];
                    //recips[0] = "gtr54@yandex.ru";
                    //ReportsSender rs = new ReportsSender();
                    //rs.SendMail(recips, "Ошибка подключения к базе в начале суток", "123");
                    return;
                }
            }
            DateTime currentDate = DateTime.Now;//запоминаем текущую дату и время
            //как только таймер отработал, идёт проверка расписаний   
            DataTable SchedulesTable = DataBaseManagerMSSQL.Return_Schedules_Tasks_Table();//вытаскиваеми сетку активных расписаний для текущего пользователя на текущее время
            //если процедура вернула null - это значит что соединение с сервером было разорвано
            if (SchedulesTable == null)
            {
                this.Invoke(new Action(delegate 
                {
                    ProgrammLogEdit.SelectionColor = Color.Red;
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " СОЕДИНЕНИЕ С СЕРВЕРОМ БЫЛО РАЗОРВАНО!\r");
                }));
                return;
            }
            currentDate = DateTime.Now;//запоминаем текущую дату и время
            //циклимся по сетке полученных выше заданий. Запускаем все полученные задания одно за другим
            foreach (DataRow row in SchedulesTable.Rows)
            {
                BackgroundWorker worker_task_firer = new BackgroundWorker();
                worker_task_firer.DoWork += new DoWorkEventHandler(fireScheduledTask);
                worker_task_firer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_task_firer_RunWorkerCompleted);
                worker_task_firer.WorkerSupportsCancellation = false;
                worker_task_firer.RunWorkerAsync(row);//передаём параметр и запускаем операцию    

                ProgrammLogEdit.Invoke(new Action(delegate 
                {
                    ProgrammLogEdit.SelectionColor = Color.DarkOrange;
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Задание " + row["task_name"].ToString() + " вызвано согласно расписанию " + row["name"].ToString() + "\r");
                }));
            }           
        }

        private void fireScheduledTask(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker_task_firer = sender as BackgroundWorker;
            DataRow row = (DataRow)e.Argument;

            DateTime currentDate = DateTime.Now;//запоминаем текущую дату и время
            TreeView tv = new TreeView();//создаём "виртуальное" дерево (которое будет временно, без привязки к видимому интерфейсу) для опроса по расписанию
            bool read_profile = false;//определяет, будет ли считываться профиль в групповом задании. По-умолчанию не будет
            LoadTask(Convert.ToInt16(row["task_id"]), tv, out read_profile);//в этой процедуре помещаем в новое виртуальное дерево все необходимые узлы для задания

            List<TreeNode> tree = new List<TreeNode>(tv.Nodes.Cast<TreeNode>());//преобразуем дерево в список
            //создаём структуру для передачи в BackgroundWorker
            TaskStruct task = new TaskStruct(tree, Convert.ToInt16(row["task_id"]), read_profile, DateTimeEditN.Value, DateTimeEditK.Value, Convert.ToInt16(PeriodNumEdit.Value), row["task_name"].ToString(), true, ReReadAbsentRecords.Checked);           
            tv = null;//уничтожаем виртуальное дерево за ненадобностью

            BackgroundWorker worker_task_executor = null;
            this.Invoke(new Action(delegate
            {//пробрасываем этот код в основной поток чтобы было обработано событие RunWorkerCompleted (которое тоже реализовано в основном потоке)
                worker_task_executor = new BackgroundWorker();  
                worker_task_executor.WorkerSupportsCancellation = true;
                worker_task_executor.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker_task_executor.RunWorkerCompleted += worker_RunWorkerCompleted;               
                worker_task_executor.RunWorkerAsync(task);//передаём структуру и запускаем операцию
            }));            
            
            DataBaseManagerMSSQL.Decrease_Schedule_Times_Repeat(Convert.ToInt16(row["id"].ToString()));//сокращаем количество повторов выполнения расписания
            
            //string[] recips = new string[1]; recips[0] = "gtr54@yandex.ru";
            //ReportsSender rs = new ReportsSender();
            //rs.SendMail(recips, "Задание успешно запущено. Время: " + DateTime.Now, task.name);
        }

        private void worker_task_firer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker worker_task_firer = sender as BackgroundWorker;
            worker_task_firer.Dispose();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {//этот метод запускается одинаково как из нажатия кнопок чтения деревьев заданий, так и при опросе задания по расписанию. Разница только в том, закрывается форма лога выполнения задания или нет
            BackgroundWorker worker = sender as BackgroundWorker;
            TaskStruct task = (TaskStruct)e.Argument;
            ReadingLogForm rlf = new ReadingLogForm(ref worker, task.closeAfterDoWork);

            this.Invoke(new Action(delegate { rlf.StartPosition = FormStartPosition.CenterScreen; }));
            this.Invoke(new Action(delegate { rlf.Text = "Лог опроса задания " + task.name + " Количество объектов: " + task.tree.Count.ToString(); }));
            this.Invoke(new Action(delegate { rlf.Show(this); }));
            //включаем опрос. Возвращаем результат отработки 
            BackgroundWorkerDoWorkResult bwdwr = TaskReading(task.tree, task.get_profile, task.lower_datetime, task.upper_datetime, task.periodsCount, rlf.richText, worker, rlf.readingLogStripBar, rlf.CurrentProfileRecord, rlf.LastProfileRecord, task.ReReadAbsentRecords, rlf, task.name);
            e.Result = bwdwr;//результат выполнения задания для передачи его в work complete
            //после опроса выгружаем отчёты
            DataTable dt = DataBaseManagerMSSQL.Return_Task_Reports(task.task_id);
            if (dt.Rows.Count != 0)
            {
                DateTime currentDate = DateTime.Now;
                //циклимся по отчётам задания
                foreach (DataRow dr in dt.Rows)
                {
                    switch (dr["report_name"].ToString())
                    {//смотрим какие отчёты выгружать
                        case "Выбранные параметры":
                            {
                                string filename = "Выбранные параметры для группы счётчиков_" + task.name + '_' + currentDate.ToString().Replace(' ', '_').Replace(':', '_').Replace('.', '_') + "";
                                string path = Application.StartupPath + "\\" + Properties.Settings.Default.ReportsDirectory + "\\";
                                filename = ExportSelectedParamsForSetOfCounters(task.tree, path, filename);//выгрузка выбранных параметров на жёсткий диск

                                DataTable dt_mail_recievers = DataBaseManagerMSSQL.Return_Binded_Mail_Recievers(task.task_id);
                                if (dt_mail_recievers.Rows.Count != 0)//если к заданию привязан хотя бы один получатель почты
                                {
                                    ReportsSender rs = new ReportsSender();
                                    rs.SendMail(filename, dt_mail_recievers, "Автоматическая отправка отчётов");
                                }
                            }
                            break;
                   
                        case "Профили мощности":
                            {//убедимся, что профиль для текущего задания снимался во время выполнения, прежде чем выгружать
                                if (task.get_profile == true)
                                {
                                    string filename = "Профили мощности_" + task.name + '_' + currentDate.ToString().Replace(' ', '_').Replace(':', '_').Replace('.', '_') + "";
                                    string path = Application.StartupPath + "\\" + Properties.Settings.Default.ReportsDirectory + "\\";                                
                                    filename = ExportPowerProfileDataGridAfterScheduledTask(task.tree, PowerProfileGrid, filename, path, task.lower_datetime, task.upper_datetime, Convert.ToInt16(dr["profile_export_sum"]));//при выгрузке профиля нужно брать дату-время не те, которые выставлены в контролах на форме, а те, которые были сняты по заданию

                                    //нужно вернуть всех получателей почты, привязанных к текущему заданию
                                    DataTable dt_mail_recievers = DataBaseManagerMSSQL.Return_Binded_Mail_Recievers(task.task_id);
                                    if (dt_mail_recievers.Rows.Count != 0)//если к заданию привязан хотя бы один получатель почты
                                    {
                                        ReportsSender rs = new ReportsSender();
                                        rs.SendMail(filename, dt_mail_recievers, "Автоматическая отправка отчётов");
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void GetProfileButt_Click(object sender, EventArgs e)
        {//опрос профиля вручную с ручным дозвоном
            PowerProfileGrid.DataSource = null;//чтобы не блокировать визуальный компонент
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(ProfileWorker_doWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void ProfileWorker_doWork(object sender, DoWorkEventArgs e)
        {//опрос профиля вручную с ручным дозвоном
            BackgroundWorker worker = sender as BackgroundWorker;
            ReadingLogForm rlf = new ReadingLogForm(ref worker, false);
            this.Invoke(new Action(delegate { rlf.StartPosition = FormStartPosition.CenterScreen; }));
            this.Invoke(new Action(delegate { rlf.Text = "Лог опроса профиля "; }));
            this.Invoke(new Action(delegate { rlf.Show(this); }));
            e.Result = GetPowerProfile(global_sp, worker, rlf.richText, rlf.readingLogStripBar, rlf.CurrentProfileRecord, rlf.LastProfileRecord, rlf, String.Empty);
        }

        private void WriteParametersButt_Click(object sender, EventArgs e)
        {//процедура записи настроек в выбранное устройство по нажатию кнопки на форме (не групповая операция) с ипользованием значений полей на форме
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(WriteParameters_doWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void WriteParameters_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = WriteParameters(global_sp, worker);
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {//все окончательные закрытия и завершения группвого опроса должны быть здесь
            this.Invoke(new Action(delegate 
            {
                FullTree.Enabled = true;
                TVTaskPLC.Enabled = true;
            }));
            ReadingLogForm rlf = null;
            BackgroundWorker worker = sender as BackgroundWorker;
            if (e.Result != null)//этот объект может быть пустым в том случае, если порт не был найден
            {//если порт был найден, значит так или иначе групповое задание было выполнено либо остановлено во время работы и мы получили результат выполнения задания
                BackgroundWorkerDoWorkResult bwdwr = (BackgroundWorkerDoWorkResult)e.Result;//экземпляр результата выполнения задания
                if (bwdwr.rlf != null) rlf = bwdwr.rlf;//rlf не будет если занимаемся ручным дозвоном, и логи пишутся на главную форму
                DataProcessing dp = bwdwr.dp;//экземпляр обработчика данных который посылает и принимает данные через порт
                if (bwdwr.dp.sp != null && bwdwr.dp.sp.IsOpen && bwdwr.autoClosePort == true)//если порт не пустой (был найден при выполнении задания) и он открыт, и указано что он должен закрываться автоматически (при опросе группы или по расписанию) 
                {
                    DateTime currentDate = DateTime.Now;
                    try
                    {
                        dp.ClosePort();//если TaskReading вернула не пустой порт (т.е. он был найден и отработал) и этот порт не закрыт, то нужно его закрыть после работы
                        
                        rlf.Invoke(new Action(delegate { rlf.richText.SelectionColor = Color.DarkGreen; }));
                        rlf.Invoke(new Action(delegate { rlf.richText.AppendText(currentDate + "." + currentDate.Millisecond + " Порт успешно закрыт\r"); }));
                    }
                    catch
                    {
                        rlf.Invoke(new Action(delegate { rlf.richText.SelectionColor = Color.Red; }));
                        rlf.Invoke(new Action(delegate { rlf.richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка закрытия порта\r"); }));
                    }
                }

                if (rlf != null)
                {
                    DateTime currentDate1 = DateTime.Now;
                    rlf.Invoke(new Action(delegate { rlf.richText.SelectionColor = Color.DarkViolet; }));
                    rlf.Invoke(new Action(delegate { rlf.richText.AppendText(currentDate1 + "." + currentDate1.Millisecond + " Опрос завершён\r"); }));
                }

                if (rlf != null)//если форма лога работы задания была создана, значит прошёл опрос группы (в том числе опрос профиля после ручного дозвона) по команде пользователя или опрос по расписанию. 
                    //Во время чтения монитора и записи параметров вручную эта форма не создаётся (они доступны только при ручном дозвоне)
                {                                                   
                    if (rlf.closeAfterWork)//если логу суждено закрыться после выполнения задания, значит задание было выполнено по расписанию
                    {
                        if (bwdwr.taskname != String.Empty)//если имя задания не пустое - значит оно было выполнено по расписанию, а значит нужно выгрузить лог его выполнения
                        {
                            DateTime currentDate = DateTime.Now;
                            string pathToSaveLog = Application.StartupPath + "\\" + Properties.Settings.Default.LogsDirectory + "\\_" + bwdwr.taskname + "_"
                            + DateTime.Now.ToString().Replace('.', '_').Replace(' ', '_').Replace(':', '_') + ".rtf";
                            rlf.Invoke(new Action(delegate
                            {
                                rlf.richText.SelectionColor = Color.DarkViolet; 
                                rlf.richText.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка лога\r");
                                rlf.richText.SaveFile(pathToSaveLog, RichTextBoxStreamType.RichText);                              
                            }));
                        }
                        rlf.Invoke(new Action(delegate { rlf.Close(); }));//после выполнения задания по расписанию форму нужно закрыть                       
                    }
                    rlf = null;
                }
            }
                      
            worker.Dispose();
            GC.Collect();
        }

        private void HideTabPage(TabPage tb)
        {
            tb.Parent = null;
        }

        private void ShowTabPage(TabPage tb, TabControl tc)
        {
            tb.Parent = tc;
        }

        private void LoadNodeData(TreeNode selectedNode)
        {  
             selectedTabPage.Select();
             DeviceEnergyGrid.Controls.Clear();
             DeviceEnergyGrid.Refresh();
             NodeNameLabel.Text = selectedNode.Name;        
             //переменная, хранящая данные отфильтрованной строки
             DataTable deviceRow;
             //выбор узла и отображение его содержимого (смотрим по классу)      
             ShowTabPage(ConfigurePage, DeviceTabControl);//эта вкладка доступна для любых объектов
             HideTabPage(ImagePage);
     
            //счётчик с цифровым интерфейсом RS-485
            if (selectedNode.Tag is ICounter && selectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                    ICounter counter = (ICounter)selectedNode.Tag; //экземпляр класса счётчика            
                    //При выборе объекта настраиваем набор доступных вкладок на панели                
                    ShowTabPage(ReadEnergyPage, DeviceTabControl);
                    ShowTabPage(ReadParamsPage, DeviceTabControl);
                    ShowTabPage(ReadJournalPage, DeviceTabControl);
                    ShowTabPage(PowerProfilePage, DeviceTabControl);
                    ShowTabPage(MonitorPage, DeviceTabControl);
                    ShowTabPage(WriteParamsPage, DeviceTabControl);
                    ShowTabPage(ReadJournalCQCPage, DeviceTabControl);
                    //выводим ID-шники в метки для отладки
                    NodeIDLabel.Text = counter.ID.ToString();
                    ParentNodeIDLabel.Text = counter.ParentID.ToString();
                    EnergyDatesFlowLayout.Visible = true;
                    LoadEnergyButt.Visible = false;
                    ShowChartButt.Visible = false;

                    deviceRow = DataBaseManagerMSSQL.Return_CounterRS_Row(counter.ID);//получаем данные по RS-счётчику
                    if (deviceRow.Rows.Count == 0)
                    {//если ничего не вернулось, то удаляем несуществующий узел и выходим                   
                        selectedNode.Remove();                   
                        return;
                    }
                    //выводим полученную таблицу свойств объекта в грид               
                    DevicePropertiesGrid.DataSource = deviceRow;
                    //убираем ненужные поля
                    DevicePropertiesGrid.Rows[6].ReadOnly = true;
                    DevicePropertiesGrid.Rows[8].Visible = false;
                    DevicePropertiesGrid.Rows[9].Visible = false;
                    DevicePropertiesGrid.Rows[10].Visible = false;
                    DevicePropertiesGrid.Rows[11].Visible = false;
                    DevicePropertiesGrid.Rows[12].Visible = false;
                    //поля с логическим значением превращаем в чекбоксы
                    DevicePropertiesGrid[1, 8] = new DataGridViewCheckBoxCell(false);
                    DevicePropertiesGrid[1, 9] = new DataGridViewCheckBoxCell(false);
                    //настраиваем стиль
                    DevicePropertiesGrid.Columns[0].DefaultCellStyle.BackColor = Color.Yellow;
                
                    //в экземпляр класса счётчика загружаем данные из базы (из строки deviceRow)
                    try
                    {
                        counter.Name = deviceRow.Rows[0][1].ToString();
                        counter.NetAddress = Convert.ToInt16(deviceRow.Rows[4][1].ToString());
                        counter.SerialNumber = deviceRow.Rows[3][1].ToString();
                        counter.TransformationRate = Convert.ToInt16(deviceRow.Rows[13][1].ToString());
                    }
                    catch
                    {
                        return;
                    }
               
                    selectedNode.Text = deviceRow.Rows[0][1].ToString();//текст узла в дереве тоже подгружаем из базы

                    //улицы - выпадающий список
                    StreetComboBox = new DataGridViewComboBoxCell();
                    StreetComboBox.FlatStyle = FlatStyle.Flat;
                    //нужно его наполнить        
                    //foreach (DataRow dr in StreetsList.Rows) StreetComboBox.Items.Add(dr["street"].ToString());
                    //DevicePropertiesGrid[1, 1] = StreetComboBox;//список улиц помещаем в грид свойств прибора

                    DeviceEnergyGrid.Columns.Clear();                
                    //гриду энергии присваиваем в качестве источника список энергии
                    counter.LoadLastEnergyIntoEnergyList();//вытаскиваем последние значения энергии из базы для каждого вида в список параметров
              
                    DeviceEnergyGrid.DataSource = null;
                    DeviceEnergyGrid.DataSource = counter.EnergyToRead;
                    //форматируем таблицу               
                    DeviceEnergyGrid.Columns[0].Visible = false;
                    DeviceEnergyGrid.Columns[1].Visible = false;
                    DeviceEnergyGrid.Columns[2].HeaderText = counter.Name; DeviceEnergyGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceEnergyGrid.Columns[3].HeaderText = "Энергия"; DeviceEnergyGrid.Columns[3].ReadOnly = true;
                    DeviceEnergyGrid.Columns[4].HeaderText = "Дата и время последнего считывания"; DeviceEnergyGrid.Columns[4].ReadOnly = true;
                    DeviceEnergyGrid.Columns[5].HeaderText = "Сумма"; DeviceEnergyGrid.Columns[4].ReadOnly = true;
                    DeviceEnergyGrid.Columns[6].HeaderText = "Тариф 1"; DeviceEnergyGrid.Columns[5].ReadOnly = true;
                    DeviceEnergyGrid.Columns[7].HeaderText = "Тариф 2"; DeviceEnergyGrid.Columns[6].ReadOnly = true;
                    DeviceEnergyGrid.Columns[8].HeaderText = "Тариф 3"; DeviceEnergyGrid.Columns[7].ReadOnly = true;
                    DeviceEnergyGrid.Columns[9].HeaderText = "Тариф 4"; DeviceEnergyGrid.Columns[8].ReadOnly = true;
                    DeviceEnergyGrid.ReadOnly = false;

                    DeviceEnergyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;//для других устройств этот параметр оставляем в значении fill, но здесь ширину столбцов выставляем вручную
                    DeviceEnergyGrid.AllowUserToResizeColumns = false;
                    DeviceEnergyGrid.Columns[2].Width = 90;
                    DeviceEnergyGrid.Columns[3].Width = 220;
                    int w = (DeviceEnergyGrid.Width - DeviceEnergyGrid.Columns[2].Width - DeviceEnergyGrid.Columns[3].Width) / 6; //ширина оставшихся колонок
                    DeviceEnergyGrid.Columns[4].Width = w;
                    DeviceEnergyGrid.Columns[5].Width = w;
                    DeviceEnergyGrid.Columns[6].Width = w;
                    DeviceEnergyGrid.Columns[7].Width = w;
                    DeviceEnergyGrid.Columns[8].Width = w;
                    DeviceEnergyGrid.Columns[9].Width = w;

                    try
                    {                       
                        //гриду записи параметров присваиваем список параметров счётчика на запись
                        WriteParametersGrid.DataSource = counter.ParametersToWrite;
                        //форматируем таблицу
                        WriteParametersGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
                        WriteParametersGrid.Columns[0].Visible = false;
                        WriteParametersGrid.Columns[1].Visible = false;
                        WriteParametersGrid.Columns[2].HeaderText = counter.Name;
                        WriteParametersGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                        WriteParametersGrid.Columns[3].HeaderText = "Параметр";
                        WriteParametersGrid.Columns[3].ReadOnly = true;                        
                    }
                    catch
                    {

                    }               
                    //гриду параметров присваиваем в качестве источника список параметров счётчика
                    DeviceParametersGrid.DataSource = counter.ParametersToRead;
                    //форматируем таблицу              
                    DeviceParametersGrid.Columns[0].Visible = false;
                    DeviceParametersGrid.Columns[1].Visible = true;
                    DeviceParametersGrid.Columns[1].HeaderText = counter.Name; DeviceParametersGrid.Columns[1].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceParametersGrid.Columns[2].HeaderText = "Параметр"; DeviceParametersGrid.Columns[3].ReadOnly = true;
                    DeviceParametersGrid.Columns[3].HeaderText = "Значение"; DeviceParametersGrid.Columns[4].ReadOnly = true;
                    DeviceParametersGrid.Columns[4].Visible = false;
                    //гриду журнала присваиваем в качестве источника список параметров журнала
                    DeviceJournalGrid.DataSource = counter.JournalToRead;
                    //форматируем таблицу               
                    DeviceJournalGrid.Columns[0].Visible = false;
                    DeviceJournalGrid.Columns[1].Visible = false; DeviceJournalGrid.Columns[1].DefaultCellStyle.BackColor = Color.Yellow;
                    DeviceJournalGrid.Columns[2].HeaderText = counter.Name; DeviceJournalGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceJournalGrid.Columns[3].HeaderText = "Журнал"; DeviceJournalGrid.Columns[3].ReadOnly = true;
                    DeviceJournalGrid.Columns[4].HeaderText = "Запись 1"; DeviceJournalGrid.Columns[4].ReadOnly = true;
                    DeviceJournalGrid.Columns[5].HeaderText = "Запись 2"; DeviceJournalGrid.Columns[5].ReadOnly = true;
                    DeviceJournalGrid.Columns[6].HeaderText = "Запись 3"; DeviceJournalGrid.Columns[6].ReadOnly = true;
                    DeviceJournalGrid.Columns[7].HeaderText = "Запись 4"; DeviceJournalGrid.Columns[7].ReadOnly = true;
                    DeviceJournalGrid.Columns[8].HeaderText = "Запись 5"; DeviceJournalGrid.Columns[8].ReadOnly = true;
                    DeviceJournalGrid.Columns[9].HeaderText = "Запись 6"; DeviceJournalGrid.Columns[9].ReadOnly = true;
                    DeviceJournalGrid.Columns[10].HeaderText = "Запись 7"; DeviceJournalGrid.Columns[10].ReadOnly = true;
                    DeviceJournalGrid.Columns[11].HeaderText = "Запись 8"; DeviceJournalGrid.Columns[11].ReadOnly = true;
                    DeviceJournalGrid.Columns[12].HeaderText = "Запись 9"; DeviceJournalGrid.Columns[12].ReadOnly = true;
                    DeviceJournalGrid.Columns[13].HeaderText = "Запись 10"; DeviceJournalGrid.Columns[13].ReadOnly = true; ;
                    //гриду монитора присваиваем в качестве источника список параметров монитора
                    DeviceMonitorGrid.DataSource = counter.MonitorToRead;
                    //форматируем таблицу               
                    DeviceMonitorGrid.Columns[0].Visible = false;//скрываем пустышку
                    DeviceMonitorGrid.Columns[1].Visible = false;//скрываем пустышку
                    DeviceMonitorGrid.Columns[2].Visible = false;//скрываем пустышку
                    DeviceMonitorGrid.Columns[3].HeaderText = "Параметр"; DeviceJournalGrid.Columns[3].ReadOnly = true;
                    DeviceMonitorGrid.Columns[4].HeaderText = "Сумма"; DeviceJournalGrid.Columns[4].ReadOnly = true;
                    DeviceMonitorGrid.Columns[5].HeaderText = "Фаза 1"; DeviceJournalGrid.Columns[5].ReadOnly = true;
                    DeviceMonitorGrid.Columns[6].HeaderText = "Фаза 2"; DeviceJournalGrid.Columns[6].ReadOnly = true;
                    DeviceMonitorGrid.Columns[7].HeaderText = "Фаза 3"; DeviceJournalGrid.Columns[7].ReadOnly = true;                   
                    //гриду журнала ПКЭ присваиваем в качестве источника список параметров журнала ПКЭ                   
                    DeviceJournalCQCGrid.DataSource = counter.JournalCQCToRead;
                    //форматируем таблицу
                    DeviceJournalCQCGrid.Columns[0].Visible = false;
                    DeviceJournalCQCGrid.Columns[1].Visible = false; DeviceJournalCQCGrid.Columns[1].DefaultCellStyle.BackColor = Color.Yellow;
                    DeviceJournalCQCGrid.Columns[2].HeaderText = counter.Name; DeviceJournalCQCGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceJournalCQCGrid.Columns[3].HeaderText = "Журнал ПКЭ"; DeviceJournalCQCGrid.Columns[3].ReadOnly = true;
                    DeviceJournalCQCGrid.Refresh();                
                    //загружаем профиль из базы в экземпляр счётчика  
                    LoadProfileIntoCounter(counter, (int)PeriodNumEdit.Value, true, DateTimeEditN.Value, DateTimeEditK.Value);//при простом выборе счётчика достаточно использовать значения контролов с датами на форме
                    ByPhaseVectorDiagramPictureBox.Image = counter.DrawVectorDiagramm(ByPhaseVectorDiagramPictureBox, ProgrammLogEdit);//рисуем векторную диаграмму                
                }
                //PLC-счётчик
                if (selectedNode.Tag.GetType() == typeof(MercuryPLC1))
                {
                    LoadEnergyButt.Visible = true;
                    ShowChartButt.Visible = true;
                    DeviceEnergyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    DeviceEnergyGrid.AllowUserToResizeColumns = true;
                    MercuryPLC1 counter = (MercuryPLC1)selectedNode.Tag;//экземпляр PLC-счётчика
                    //если счётчик виртуальный, то выходим из процедуры
                    if (counter.virtualmode == true) return;
                    EnergyDatesFlowLayout.Visible = true;
                    //При выборе объекта настраиваем набор доступных вкладок на панели
                    HideTabPage(ReadParamsPage);
                    HideTabPage(ReadJournalPage);
                    HideTabPage(PowerProfilePage);
                    HideTabPage(MonitorPage);
                    HideTabPage(WriteParamsPage);
                    HideTabPage(ReadJournalCQCPage);

                    ShowTabPage(ReadEnergyPage, DeviceTabControl);
                    //выводим ID-шники в метки для отладки
                    NodeIDLabel.Text = counter.ID.ToString();
                    ParentNodeIDLabel.Text = counter.ParentID.ToString();
                    //получаем строку из базы с данными по PLC-счётчику
                    deviceRow = DataBaseManagerMSSQL.Return_CounterPLC_Row(counter.ID);
                    if (deviceRow.Rows.Count == 0)
                    {//если ничего не выернулось, то удаляем несуществующий узел и выходим
                        selectedNode.Remove();
                        return;
                    }
                    //текст узла в дереве тоже подгружаем из базы
                    selectedNode.Text = deviceRow.Rows[1][1].ToString() + " " + deviceRow.Rows[2][1].ToString()
                        + " (" + deviceRow.Rows[4][1].ToString() + ")";
                    //выводим полученную таблицу свойств объекта в грид
                    DevicePropertiesGrid.DataSource = deviceRow;
                    //столбца с последним средним расходом скрываем
                    DevicePropertiesGrid.Rows[6].ReadOnly = true;
                    DevicePropertiesGrid.Rows[18].Visible = false;
                    DevicePropertiesGrid.Rows[19].Visible = false;
                    DevicePropertiesGrid.Rows[20].Visible = false;
                    DevicePropertiesGrid.Rows[21].Visible = false;
                    DevicePropertiesGrid.Rows[22].Visible = false;
                    //настраиваем стиль
                    DevicePropertiesGrid.Columns[0].DefaultCellStyle.BackColor = Color.Yellow;
                    //в экземпляр класса счётчика загружаем данные из базы
                    try
                    {
                        counter.Name = deviceRow.Rows[0][1].ToString();
                        counter.NetAddress = Convert.ToInt16(deviceRow.Rows[4][1]);
                        counter.SerialNumber = deviceRow.Rows[3][1].ToString();

                        if (!Convert.IsDBNull(deviceRow.Rows[13][1])) counter.lastDateZone0 = Convert.ToDateTime(deviceRow.Rows[13][1]);
                        if (!Convert.IsDBNull(deviceRow.Rows[14][1])) counter.lastDateZone1 = Convert.ToDateTime(deviceRow.Rows[14][1]);
                        if (!Convert.IsDBNull(deviceRow.Rows[15][1])) counter.lastDateZone2 = Convert.ToDateTime(deviceRow.Rows[15][1]);
                        if (!Convert.IsDBNull(deviceRow.Rows[16][1])) counter.lastDateZone3 = Convert.ToDateTime(deviceRow.Rows[16][1]);
                        if (!Convert.IsDBNull(deviceRow.Rows[17][1])) counter.lastDateZone4 = Convert.ToDateTime(deviceRow.Rows[17][1]);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    //улицы - выпадающий список
                    StreetComboBox = new DataGridViewComboBoxCell();
                    StreetComboBox.FlatStyle = FlatStyle.Flat;
                    //нужно его наполнить        
                    foreach (DataRow dr in StreetsList.Rows) StreetComboBox.Items.Add(dr["street"].ToString());
                    //DevicePropertiesGrid[1, 1] = StreetComboBox;//список улиц помещаем в грид свойств прибора

                    Cursor = Cursors.WaitCursor;
                    DeviceEnergyGrid.Columns.Clear();//очищаем грид
                    //в грид на вкладке энергии выкидываем историю показаний по выбранному счётчику              
                    DeviceEnergyGrid.DataSource = DataBaseManagerMSSQL.Return_Counter_Energy_PLC_History(counter.SerialNumber, 2, DateTimeNEnergy.Value, DateTimeKEnergy.Value, "'Энергия, текущее потребление', 'Энергия, суточный срез'");
                    //создаём столбцы-пустышки для того чтобы корректно отработал алгоритм выгрузки грида
                    DataGridViewColumn dummy1 = new DataGridViewColumn(); dummy1.CellTemplate = new DataGridViewTextBoxCell();
                    DataGridViewColumn dummy2 = new DataGridViewColumn(); dummy2.CellTemplate = new DataGridViewTextBoxCell();
                    DataGridViewColumn dummy3 = new DataGridViewColumn(); dummy3.CellTemplate = new DataGridViewTextBoxCell();
                    //вставляем столбцы-пустышки
                    DeviceEnergyGrid.Columns.Insert(0, dummy1);
                    DeviceEnergyGrid.Columns.Insert(1, dummy2);
                    DeviceEnergyGrid.Columns.Insert(2, dummy3);
                    //скрываем столбы-пустышки
                    DeviceEnergyGrid.Columns[0].Visible = false;
                    DeviceEnergyGrid.Columns[1].Visible = false;
                    DeviceEnergyGrid.Columns[2].Visible = false;
                    //////форматируем таблицу               
                    DeviceEnergyGrid.Columns[3].HeaderText = "Сумма, значение"; DeviceEnergyGrid.Columns[3].ReadOnly = true;
                    DeviceEnergyGrid.Columns[4].HeaderText = "Сумма, дата\\время"; DeviceEnergyGrid.Columns[4].ReadOnly = true;
                    DeviceEnergyGrid.Columns[5].HeaderText = "Тариф 1, значение"; DeviceEnergyGrid.Columns[5].ReadOnly = true;
                    DeviceEnergyGrid.Columns[6].HeaderText = "Тариф 1, дата\\время"; DeviceEnergyGrid.Columns[6].ReadOnly = true;
                    DeviceEnergyGrid.Columns[7].HeaderText = "Тариф 2, значение"; DeviceEnergyGrid.Columns[7].ReadOnly = true;
                    DeviceEnergyGrid.Columns[8].HeaderText = "Тариф 2, дата\\время"; DeviceEnergyGrid.Columns[8].ReadOnly = true;
                    DeviceEnergyGrid.Columns[9].HeaderText = "Тариф 3, значение"; DeviceEnergyGrid.Columns[9].ReadOnly = true;
                    DeviceEnergyGrid.Columns[10].HeaderText = "Тариф 3, дата\\время"; DeviceEnergyGrid.Columns[10].ReadOnly = true;
                    DeviceEnergyGrid.Columns[11].HeaderText = "Тариф 4, значение"; DeviceEnergyGrid.Columns[11].ReadOnly = true;
                    DeviceEnergyGrid.Columns[12].HeaderText = "Тариф 4, дата\\время"; DeviceEnergyGrid.Columns[12].ReadOnly = true;
                    DeviceEnergyGrid.Columns[13].HeaderText = "Наименование"; DeviceEnergyGrid.Columns[13].ReadOnly = true;
                    DeviceEnergyGrid.ReadOnly = true;
                    Cursor = Cursors.Default;
                    VirtualModeHintLabel.Text = counter.clonemode.ToString();
                }
                if (selectedNode.Tag is IConnection)
                {
                //выпадающий список со значениями строки инициализации модема
                    //CBSTComboBox = new DataGridViewComboBoxCell();
                    //CBSTComboBox.FlatStyle = FlatStyle.Flat;
                    //CBSTComboBox.Items.Add("7,0,1"); CBSTComboBox.Items.Add("71,0,1");
                    //районы - выпадающий список
                    DistrictComboBox = new DataGridViewComboBoxCell();
                    DistrictComboBox.FlatStyle = FlatStyle.Flat;
                    //нужно его наполнить        
                    foreach (DataRow dr in DistrictsList.Rows) DistrictComboBox.Items.Add(dr["name"].ToString());
                    //DistrictComboBox.ReadOnly = true;
                    //DistrictComboBox.DropDownWidth = 1;
                }
                //модем
                if (selectedNode.Tag.GetType() == typeof(Modem))
                {
                    //При выборе объекта настраиваем набор доступных вкладок на панели        
                    HideTabPage(ReadEnergyPage);
                    HideTabPage(ReadParamsPage);
                    HideTabPage(ReadJournalPage);
                    HideTabPage(PowerProfilePage);
                    HideTabPage(MonitorPage);
                    HideTabPage(WriteParamsPage);
                    HideTabPage(ReadJournalCQCPage);

                    Modem modem = (Modem)selectedNode.Tag;//экземпляр модема
                                                          //выводим ID-шники в метки для отладки
                    NodeIDLabel.Text = modem.ID.ToString();
                    ParentNodeIDLabel.Text = "0";
                    deviceRow = DataBaseManagerMSSQL.Return_Connection_Row(modem.ID);//получаем строку из базы с данными по модему
                    if (deviceRow.Rows.Count == 0)
                    {//если ничего не выернулось, то удаляем несуществующий узел и выходим
                        selectedNode.Remove();
                        return;
                    }
                    selectedNode.Text = deviceRow.Rows[0][1].ToString() + "\\" + deviceRow.Rows[8][1].ToString();//текст узла в дереве тоже подгружаем из базы
                                                                                                                 //в экземпляр класса модема загружаем данные из базы из строки deviceRow
                    modem.Name = deviceRow.Rows[0][1].ToString(); modem.IP = deviceRow.Rows[2][1].ToString();
                    modem.Phone = deviceRow.Rows[1][1].ToString(); modem.CBST = deviceRow.Rows[5][1].ToString();
                    //выводим полученную таблицу свойств объекта в грид (задаём гриду источних данных)
                    DevicePropertiesGrid.DataSource = deviceRow;
                    //оформляем грид свойств объекта
                    DevicePropertiesGrid.Columns[0].ReadOnly = true;
                    DevicePropertiesGrid.Columns[0].DefaultCellStyle.BackColor = Color.Yellow;
                    DevicePropertiesGrid.Columns[0].HeaderText = "Свойство";
                    DevicePropertiesGrid.Columns[1].HeaderText = "Значение";
                    DevicePropertiesGrid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
                    DevicePropertiesGrid.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
                    //убираем ненужные столбцы                
                    DevicePropertiesGrid.Rows[4].Visible = false;
                    DevicePropertiesGrid.Rows[6].Visible = false;
                    DevicePropertiesGrid.Rows[7].Visible = false;

                    //DevicePropertiesGrid[1, 5] = CBSTComboBox;//строка инициализации - выпадающий список                                                     
                    DevicePropertiesGrid[1, 8] = DistrictComboBox;//список районов - выпадающий список   
                }
                //шлюз
                if (selectedNode.Tag.GetType() == typeof(Mercury228))
                {
                    //При выборе объекта настраиваем набор доступных вкладок на панели
                    HideTabPage(ReadEnergyPage);
                    HideTabPage(ReadJournalPage); HideTabPage(PowerProfilePage);
                    HideTabPage(MonitorPage);// HideTabPage(WriteParamsPage);
                    HideTabPage(ReadJournalCQCPage);

                    ShowTabPage(ReadParamsPage, DeviceTabControl);
                    ShowTabPage(WriteParamsPage, DeviceTabControl);
                    Mercury228 gate = (Mercury228)selectedNode.Tag;//экземпляр класса шлюза
                    //выводим ID-шники в метки для отладки
                    NodeIDLabel.Text = gate.ID.ToString();
                    ParentNodeIDLabel.Text = "0";
                    deviceRow = DataBaseManagerMSSQL.Return_Connection_Row(gate.ID);//получаем данные по шлюзу (реальная таблица транспонирована)
                    if (deviceRow.Rows.Count == 0)
                    {//если ничего не выернулось, то удаляем несуществующий узел и выходим
                        selectedNode.Remove();
                        return;
                    }
                    selectedNode.Text = deviceRow.Rows[0][1].ToString() + "\\" + deviceRow.Rows[8][1].ToString();//текст узла в дереве тоже подгружаем из базы
                                                                                                                 //выводим полученную таблицу свойств объекта в грид (задаём гриду источних данных)
                    DevicePropertiesGrid.DataSource = deviceRow;
                    //----------оформляем грид свойств объекта----------------------------     
                    DevicePropertiesGrid.Columns[0].ReadOnly = true;
                    DevicePropertiesGrid.Columns[0].DefaultCellStyle.BackColor = Color.Yellow;
                    DevicePropertiesGrid.Columns[0].HeaderText = "Свойство";
                    DevicePropertiesGrid.Columns[1].HeaderText = "Значение";
                    DevicePropertiesGrid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
                    DevicePropertiesGrid.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
                    //задаём нужные типы в ячейках
                    DevicePropertiesGrid[1, 6] = new DataGridViewCheckBoxCell(false);
                    //убираем ненужные строки (заголовки в реальной таблице)
                    DevicePropertiesGrid.Rows[4].Visible = false;
                    DevicePropertiesGrid.Rows[6].Visible = false;
                    DevicePropertiesGrid.Rows[7].Visible = false;

                    //DevicePropertiesGrid[1, 5] = CBSTComboBox;//строка инициализации - выпадающий список    
                    DevicePropertiesGrid[1, 8] = DistrictComboBox;//список районов
                    try
                    {
                        //в экземпляр класса загружаем данные из базы
                        gate.Name = deviceRow.Rows[0][1].ToString(); gate.Phone = deviceRow.Rows[1][1].ToString();
                        gate.IP = deviceRow.Rows[2][1].ToString(); gate.Port = deviceRow.Rows[3][1].ToString();
                        gate.CBST = deviceRow.Rows[5][1].ToString(); gate.AutoConfig = Convert.ToInt16(Convert.ToBoolean(deviceRow.Rows[6][1].ToString()));
                        gate.ConfigStr = deviceRow.Rows[7][1].ToString();
                    }
                    catch
                    {
                        return;
                    }

                    if (gate.ConfigStr != null && gate.ConfigStr != String.Empty && gate.ConfigStr != "0" && gate.ConfigStr.Length == 6)
                    {
                        //теперь нужно расшифровать строку автоконфигурации, хранящуюся в базе, и поместить в контролы на вкладке записи параметров устройства
                        //эта процедура обратна процедуре Mercury228.FormConfigString
                        string DecodedConfigString = Mercury228.DecodeConfigString(gate.ConfigStr);
                        //разбираем полученную расшифрованную строку автоконфигурации и помещаем значения на контролы на вкладке записи параметров 
                        int pos = 0;//позиция символа-разделителя
                        pos = DecodedConfigString.IndexOf("/", 0);//находим первое вхождение разделяющего символа в строке
                        PortRateGateCombo.Text = DecodedConfigString.Substring(0, pos);//поле UART - скорость
                        DataBytesGateCombo.Text = DecodedConfigString.Substring(pos + 1, 6); pos += 7; //поле UART - кол-во бит данных
                        StopBytesGateCombo.Text = DecodedConfigString.Substring(pos + 1, 7); pos += 8; //поле UART - кол-во стоповых бит          
                        int pos2 = DecodedConfigString.IndexOf("/", pos + 1);//находим вхождение разделяющего символа в строке для проверки чётности
                        EvenOddCheckGateCombo.Text = DecodedConfigString.Substring(pos + 1, pos2 - pos - 1); pos += pos2 - pos; //поле UART - проверка чётности
                        pos2 = DecodedConfigString.IndexOf("/", pos + 1);//находим вхождение разделяющего символа в строке для чёт\нечет
                        EvenOddGateCombo.Text = DecodedConfigString.Substring(pos + 1, pos2 - pos - 1); pos += pos2 - pos; //поле UART - чёт\нечет
                        pos2 = DecodedConfigString.IndexOf("/", pos + 1);//находим вхождение разделяющего символа в строке для значения таймаута пропуска пакета
                        TimeoutPackageGateCombo.Text = DecodedConfigString.Substring(pos + 1, pos2 - pos - 1); pos += pos2 - pos; //поле WAIT - таймаут пропуска пакета
                        PauseGateNum.Value = Convert.ToInt16(DecodedConfigString.Substring(pos + 1, 1));//поле PAUSE
                    }
                    else
                    {
                        PortRateGateCombo.SelectedIndex = -1; DataBytesGateCombo.SelectedIndex = -1;
                        StopBytesGateCombo.SelectedIndex = -1; EvenOddCheckGateCombo.SelectedIndex = -1;
                        EvenOddGateCombo.SelectedIndex = -1; TimeoutPackageGateCombo.SelectedIndex = -1;
                        PauseGateNum.Value = 3;
                    }
                    //гриду параметров присваиваем в качестве источника список параметров шлюза
                    DeviceParametersGrid.DataSource = gate.ParametersToRead;
                    DeviceParametersGrid.Columns[0].Visible = false;
                    DeviceParametersGrid.Columns[1].Visible = true;
                    DeviceParametersGrid.Columns[1].HeaderText = gate.Name; DeviceParametersGrid.Columns[1].DefaultCellStyle.BackColor = Color.Yellow;
                    DeviceParametersGrid.Columns[2].HeaderText = "Параметр"; DeviceParametersGrid.Columns[3].ReadOnly = true; DeviceParametersGrid.Columns[1].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceParametersGrid.Columns[3].HeaderText = "Значение"; DeviceParametersGrid.Columns[4].ReadOnly = true;
                    DeviceParametersGrid.Columns[4].Visible = false; DeviceParametersGrid.Columns[5].Visible = false;
                    //гриаду записи параметров присваимвыем список параметров концентратора на запись
                    WriteParametersGrid.DataSource = gate.ParametersToWrite;
                    WriteParametersGrid.Columns[0].Visible = false;
                    WriteParametersGrid.Columns[1].Visible = false;
                    WriteParametersGrid.Columns[2].HeaderText = gate.Name; WriteParametersGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    WriteParametersGrid.Columns[3].HeaderText = "Параметр"; WriteParametersGrid.Columns[3].ReadOnly = true;
                }
                //концентратор
                if (selectedNode.Tag.GetType() == typeof(Mercury225PLC1))
                {
                    DeviceEnergyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    DeviceEnergyGrid.AllowUserToResizeColumns = true;
                    //если концентратор виртуальный, то выходим из процедуры
                    if (selectedNode.Text.Contains("K")) return;

                    EnergyDatesFlowLayout.Visible = false;
                    //При выборе объекта настраиваем набор доступных вкладок на панели
                    HideTabPage(ReadJournalPage);
                    HideTabPage(PowerProfilePage);
                    HideTabPage(MonitorPage);
                    HideTabPage(ReadJournalCQCPage);

                    ShowTabPage(WriteParamsPage, DeviceTabControl);
                    ShowTabPage(ReadEnergyPage, DeviceTabControl);
                    ShowTabPage(ReadParamsPage, DeviceTabControl);

                    Mercury225PLC1 concentrator = (Mercury225PLC1)selectedNode.Tag;//экземпляр класса концентратора
                                                                                   //выводим ID-шники в метки для отладки
                    NodeIDLabel.Text = concentrator.ID.ToString();
                    ParentNodeIDLabel.Text = concentrator.ParentID.ToString();
                    deviceRow = DataBaseManagerMSSQL.Return_Concentrator_Row(concentrator.ID);//получаем строку с данными по концентратору
                    if (deviceRow.Rows.Count == 0)
                    {//если ничего не вернулось, то удаляем несуществующий узел и выходим
                        selectedNode.Remove();
                        return;
                    }
                    selectedNode.Text = deviceRow.Rows[1][1].ToString();//текст узла в дереве тоже подгружаем из базы
                    //выводим полученную таблицу свойств объекта в грид (задаём гриду источних данных)
                    DevicePropertiesGrid.DataSource = deviceRow;
                    //оформляем грид свойств объекта
                    DevicePropertiesGrid.Columns[0].ReadOnly = true;
                    DevicePropertiesGrid.Columns[0].DefaultCellStyle.BackColor = Color.Yellow;
                    DevicePropertiesGrid.Columns[0].HeaderText = "Свойство";
                    DevicePropertiesGrid.Columns[1].HeaderText = "Значение";
                    DevicePropertiesGrid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
                    DevicePropertiesGrid.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
                    //в экземпляр класса концентратора загружаем данные из базы
                    try
                    {
                        concentrator.Name = deviceRow.Rows[0][1].ToString();
                        concentrator.NetAddress = deviceRow.Rows[1][1].ToString();
                    }
                    catch
                    {
                        return;
                    }
                    WriteConcAddressTextConc.Text = concentrator.NetAddress;
                    //гриду параметров присваиваем в качестве источника список параметров концентратора
                    DeviceParametersGrid.DataSource = concentrator.ParametersToRead;
                    DeviceParametersGrid.Columns[0].Visible = false;
                    DeviceParametersGrid.Columns[1].Visible = true;
                    DeviceParametersGrid.Columns[1].HeaderText = concentrator.Name; DeviceParametersGrid.Columns[1].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    DeviceParametersGrid.Columns[2].HeaderText = "Параметр"; DeviceParametersGrid.Columns[2].ReadOnly = true;
                    DeviceParametersGrid.Columns[3].HeaderText = "Значение"; DeviceParametersGrid.Columns[3].ReadOnly = true;
                    //гриаду записи параметров присваиваем список параметров концентратора на запись
                    WriteParametersGrid.DataSource = concentrator.ParametersToWrite;
                    //форматируем таблицу
                    WriteParametersGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
                    WriteParametersGrid.Columns[0].Visible = false;
                    WriteParametersGrid.Columns[1].Visible = false;
                    WriteParametersGrid.Columns[2].HeaderText = concentrator.Name; WriteParametersGrid.Columns[2].DefaultCellStyle.BackColor = Color.LemonChiffon;
                    WriteParametersGrid.Columns[3].HeaderText = "Параметр"; WriteParametersGrid.Columns[3].ReadOnly = true;
                    //далее идёт грид энергии---------------------------------------------------------------------------------------------------------------------
                    DeviceEnergyGrid.Columns.Clear();//очищаем грид
                    //в грид на вкладке энергии выкидываем последние показания для всех подключенных к концентратору счётчиков
                    DeviceEnergyGrid.DataSource = DataBaseManagerMSSQL.Return_Concentrator_Last_Energy(selectedNode);
                    DataTable dt = (DataTable)DeviceEnergyGrid.DataSource;
                    if (dt.Rows.Count == 0) return;
                    //создаём столбцы-пустышки для того чтобы корректно отработал алгоритм выгрузки грида
                    DataGridViewColumn dummy1 = new DataGridViewColumn(); dummy1.CellTemplate = new DataGridViewTextBoxCell();
                    DataGridViewColumn dummy2 = new DataGridViewColumn(); dummy2.CellTemplate = new DataGridViewTextBoxCell();
                    DataGridViewColumn dummy3 = new DataGridViewColumn(); dummy3.CellTemplate = new DataGridViewTextBoxCell();
                    //вставляем столбцы-пустышки
                    DeviceEnergyGrid.Columns.Insert(0, dummy1);
                    DeviceEnergyGrid.Columns.Insert(1, dummy2);
                    DeviceEnergyGrid.Columns.Insert(2, dummy3);
                    //скрываем столбы-пустышки
                    DeviceEnergyGrid.Columns[0].Visible = false;
                    DeviceEnergyGrid.Columns[1].Visible = false;
                    DeviceEnergyGrid.Columns[2].Visible = false;
                    //форматируем таблицу               
                    DeviceEnergyGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
                    DeviceEnergyGrid.Columns[3].HeaderText = "Объект"; DeviceEnergyGrid.Columns[3].ReadOnly = true;
                    DeviceEnergyGrid.Columns[4].HeaderText = "Серийный №"; DeviceEnergyGrid.Columns[4].ReadOnly = true;
                    DeviceEnergyGrid.Columns[5].HeaderText = "Район"; DeviceEnergyGrid.Columns[5].ReadOnly = true;
                    DeviceEnergyGrid.Columns[6].HeaderText = "Сумма, значение"; DeviceEnergyGrid.Columns[6].ReadOnly = true;
                    DeviceEnergyGrid.Columns[7].HeaderText = "Сумма, дата\\время"; DeviceEnergyGrid.Columns[7].ReadOnly = true;
                    DeviceEnergyGrid.Columns[8].HeaderText = "Тариф 1, значение"; DeviceEnergyGrid.Columns[8].ReadOnly = true;
                    DeviceEnergyGrid.Columns[9].HeaderText = "Тариф 1, дата\\время"; DeviceEnergyGrid.Columns[9].ReadOnly = true;
                    DeviceEnergyGrid.Columns[10].HeaderText = "Тариф 2, значение"; DeviceEnergyGrid.Columns[10].ReadOnly = true;
                    DeviceEnergyGrid.Columns[11].HeaderText = "Тариф 2, дата\\время"; DeviceEnergyGrid.Columns[11].ReadOnly = true;
                    DeviceEnergyGrid.Columns[12].HeaderText = "Тариф 3, значение"; DeviceEnergyGrid.Columns[12].ReadOnly = true;
                    DeviceEnergyGrid.Columns[13].HeaderText = "Тариф 3, дата\\время"; DeviceEnergyGrid.Columns[13].ReadOnly = true;
                    DeviceEnergyGrid.Columns[14].HeaderText = "Тариф 4, значение"; DeviceEnergyGrid.Columns[14].ReadOnly = true;
                    DeviceEnergyGrid.Columns[15].HeaderText = "Тариф 4, дата\\время"; DeviceEnergyGrid.Columns[15].ReadOnly = true;
                    DeviceEnergyGrid.Columns[16].HeaderText = "Комментарий"; DeviceEnergyGrid.Columns[16].ReadOnly = true;
                    DeviceEnergyGrid.ReadOnly = true;
                }
                ShowTabPage(ImagePage, DeviceTabControl);//эта вкладка доступна для любых объектов
                                                         
                ImageFlow.Controls.Clear();//очищаем фотки
        }

        public void LoadAllObjects(object obj)
        {
            DataSet ds = (DataSet)obj;//приводим тип

            try
                {                                          
                    Mercury228 newGate;
                    Modem newModem;
                    TreeNode newConnectionNode, newConcentratorNode;

                    //считаем общее кол-во объектов
                    this.Invoke(new Action(delegate
                    {
                        ProgressBar.Value = 0;
                        ProgressBar.Maximum =
                              ds.Tables["CONNECTION_POINTS"].Rows.Count
                            + ds.Tables["CONCENTRATOR_POINTS"].Rows.Count
                            + ds.Tables["COUNTERS_PLC"].Rows.Count 
                            + ds.Tables["COUNTERS_RS"].Rows.Count;
                    }));

                    //циклимся по таблице подключений
                    foreach (DataRow rowConnection in ds.Tables["CONNECTION_POINTS"].Rows)
                    {   //создаем узел дерева
                        newConnectionNode = new TreeNode();
                        switch ((byte)rowConnection["type_id"])
                        {
                            case 2://шлюз
                                {
                                    //создаем экземпляр класса
                                    newGate = new Mercury228((int)rowConnection["id"], rowConnection["name"].ToString(),
                                                                  rowConnection["phone_number"].ToString(), "127.0.0.1",
                                                                  rowConnection["config_string"].ToString(), rowConnection["gsm_cbst"].ToString());
                                    //привязываем экзепляр класса к узлу в дереве
                                    newConnectionNode.Tag = newGate;
                                    //пиктограммы
                                    this.Invoke(new Action(delegate
                                    {
                                        newConnectionNode.ImageIndex = 0; newConnectionNode.SelectedImageIndex = 1;
                                        //даем узлу имя
                                        newConnectionNode.Name = rowConnection["id"].ToString() + "_" + rowConnection["type_id"].ToString();
                                    }));
                                }
                                break;

                            case 1://модем
                                {
                                    //создаем экземпляр класса
                                    newModem = new Modem((int)rowConnection["id"], rowConnection["name"].ToString(),
                                                 rowConnection["phone_number"].ToString(), "127.0.0.1", rowConnection["gsm_cbst"].ToString());
                                    //привязываем экзепляр класса к узлу в дереве
                                    newConnectionNode.Tag = newModem;
                                    //пиктограммы
                                    this.Invoke(new Action(delegate
                                    {
                                        newConnectionNode.ImageIndex = 8; newConnectionNode.SelectedImageIndex = 9;
                                        newConnectionNode.Name = rowConnection["id"].ToString() + "_" + rowConnection["type_id"].ToString();
                                    }));
                                }
                                break;
                        }

                        this.Invoke(new Action(delegate
                        {
                            ProgressBar.PerformStep();
                            newConnectionNode.Text = rowConnection["name"].ToString() + "\\" + rowConnection["district"].ToString();
                            FullTree.Nodes.Add(newConnectionNode);
                        }));
                        //циклимся по таблице концентраторов. Фильтруем её по ID подключения
                        foreach (DataRow rowConcentrator in ds.Tables["CONCENTRATOR_POINTS"].Select("id_connection=" + rowConnection["id"].ToString()))
                        {
                            //создаем узел дерева
                            newConcentratorNode = new TreeNode();
                            //создаем экземпляр класса
                            Mercury225PLC1 newConcentrator = new Mercury225PLC1((int)rowConcentrator["id"], (int)rowConnection["id"],
                                           rowConcentrator["name"].ToString(), rowConcentrator["net_address"].ToString(), //(int)rowConnection["type_id"],
                                           String.Empty);
                            //привязываем экземпляр класса к узлу в дереве
                            newConcentratorNode.Tag = newConcentrator;
                            //запоминаем в целом типе чтобы потом перевести в 16-ричное представление
                            string netAdr = rowConcentrator["net_address"].ToString();
                            //переводим в 16-ричное текстовове представление
                            newConcentratorNode.Text = netAdr;
                            //пиктограмми
                            newConcentratorNode.ImageIndex = 2;
                            newConcentratorNode.SelectedImageIndex = 3;
                            //даем узлу имя, чтобы потом по нему искать в дереве
                            this.Invoke(new Action(delegate
                            {
                                ProgressBar.PerformStep();
                                newConcentratorNode.Name = rowConcentrator["id"].ToString() + "_" + rowConcentrator["net_address"].ToString();
                                //к узлу подключения добавляем вновь полученный узел концентратора
                                newConnectionNode.Nodes.Add(newConcentratorNode);

                            }));
                            //циклимся по таблице PLC-счётчиков. Фильтруем её по ID концентратора
                            foreach (DataRow rowCounterPLC in ds.Tables["COUNTERS_PLC"].Select("id_concentrator=" + rowConcentrator["id"].ToString()))
                            {
                                //создаем узел дерева
                                TreeNode newCounterNode = new TreeNode();
                                //создаем экземпляр класса
                                MercuryPLC1 newCounter =
                                new MercuryPLC1((int)rowCounterPLC["id"], (int)rowConcentrator["id"],
                                                        rowCounterPLC["name"].ToString(), Convert.ToInt16(rowCounterPLC["net_address"]),
                                                        (string)rowCounterPLC["serial_number"] //Convert.ToInt16(rowConcentrator["net_address"]), 
                                                        , false, false);

                                //грузим из базы даты последних показаний счётчика 
                                DataTable deviceRow = DataBaseManagerMSSQL.Return_CounterPLC_Row(newCounter.ID);
                                //помещаем даты последних показаний в поля класса для последующего анализа в попытке записать новые показания в базу
                                if (!Convert.IsDBNull(deviceRow.Rows[13][1])) newCounter.lastDateZone0 = Convert.ToDateTime(deviceRow.Rows[13][1]);
                                if (!Convert.IsDBNull(deviceRow.Rows[14][1])) newCounter.lastDateZone1 = Convert.ToDateTime(deviceRow.Rows[14][1]);
                                if (!Convert.IsDBNull(deviceRow.Rows[15][1])) newCounter.lastDateZone2 = Convert.ToDateTime(deviceRow.Rows[15][1]);
                                if (!Convert.IsDBNull(deviceRow.Rows[16][1])) newCounter.lastDateZone3 = Convert.ToDateTime(deviceRow.Rows[16][1]);
                                if (!Convert.IsDBNull(deviceRow.Rows[17][1])) newCounter.lastDateZone4 = Convert.ToDateTime(deviceRow.Rows[17][1]);

                                //привязываем экзепляр класса к узлу в дереве
                                newCounterNode.Tag = newCounter;
                                newCounterNode.Text = rowCounterPLC["street"].ToString() + " " + rowCounterPLC["house"].ToString()
                                                                                                            + " (" + rowCounterPLC["net_address"].ToString() + ")";
                                //пиктограммы
                                newCounterNode.ImageIndex = 6; newCounterNode.SelectedImageIndex = 7;
                                //даем узлу имя, чтобы потом по нему искать в дереве
                                this.Invoke(new Action(delegate
                                {
                                    ProgressBar.PerformStep();
                                    newCounterNode.Name = rowCounterPLC["id"].ToString() + "_" + rowCounterPLC["name"].ToString();
                                    //к узлу подключения добавляем вновь полученный узел счётчика
                                    newConcentratorNode.Nodes.Add(newCounterNode);

                                }));
                            }
                        }
                        //циклимся по таблице счётчиков RS-485.Фильтруем её по ID подключения   
                        foreach (DataRow rowCounter in ds.Tables["COUNTERS_RS"].Select("id_connection=" + rowConnection["id"].ToString()))
                        {
                            //создаем узел в дереве
                            TreeNode newCounterNode = new TreeNode();
                            //var weakNode = new WeakReference(newCounterNode);
                        
                            //создаем экземпляр класса
                            byte type_id = Convert.ToByte(rowCounter["type_id"]);//смотрим какой тип счётчика в справочнике
                            if (type_id == 1) // в зависимости от типа счётчика создаём соответствующий экзмепляр
                            {
                                MercuryRS485 newCounter = new MercuryRS485((int)rowCounter["id"], (int)rowConnection["id"],
                                                rowCounter["name"].ToString(), (int)rowCounter["net_address"], true,// (int)rowConnection["type_id"], 
                                                //ProgrammLogEdit,
                                                (string)rowCounter["serial_number"], Convert.ToInt16(rowCounter["transformation_rate"])
                                                , newCounterNode
                                                );
                                //подгружаем профиль за текущий месяц
                                //пиктограммы
                                newCounterNode.ImageIndex = 4;
                                newCounterNode.SelectedImageIndex = 5;
                                newCounterNode.Tag = newCounter;
                            }
                            //если счётчик - СЭТ
                            if (type_id == 3)
                            {
                                MicronSET newCounter = new MicronSET((int)rowCounter["id"], (int)rowConnection["id"],
                                          rowCounter["name"].ToString(), Convert.ToByte(rowCounter["net_address"]), true,// (int)rowConnection["type_id"],
                                          //ProgrammLogEdit,
                                          (string)rowCounter["serial_number"], Convert.ToInt16(rowCounter["transformation_rate"])
                                          , newCounterNode
                                          );
                                //пиктограммы
                                newCounterNode.ImageIndex = 11;
                                newCounterNode.SelectedImageIndex = 11;
                                newCounterNode.Tag = newCounter;
                            }
                            //если счётчик - ПСЧ
                            if (type_id == 4)
                            {
                                PSCH newCounter = new PSCH();
                                //пиктограммы
                                newCounterNode.ImageIndex = 12;
                                newCounterNode.SelectedImageIndex = 12;
                                newCounterNode.Tag = newCounter;
                            }
                            //если счётчик - Меркурий однофазный
                            if (type_id == 5)
                            {
                                Mercury200RS485 newCounter = new Mercury200RS485();
                                //пиктограммы
                                newCounterNode.ImageIndex = 13;
                                newCounterNode.SelectedImageIndex = 13;
                                newCounterNode.Tag = newCounter;
                           }
                        
                        newCounterNode.Text = rowCounter["name"].ToString();//даем узлу имя, чтобы потом по нему искать в дереве
                        newCounterNode.Name = rowCounter["id"].ToString() + "_" + rowCounter["name"].ToString();
                        
                        this.Invoke(new Action(delegate
                        {//к узлу подключения добавляем вновь полученный узел счётчика
                            newConnectionNode.Nodes.Add(newCounterNode);
                            ProgressBar.PerformStep();                          
                        }));
                    } // конец цикла по таблице RS
                    } //конец цикла по подключениям
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка вида: " + ex.Message, "Ошибка загрузки дерева",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Invoke(new Action(delegate
                    {
                        Cursor = Cursors.Default;
                    }));
                    System.Diagnostics.EventLog.WriteEntry("Media", ex.StackTrace, System.Diagnostics.EventLogEntryType.Error);
            }

            ds.Clear();
            ds = null;
            GC.Collect();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            //
        }

        private void CollapseTWButt_Click(object sender, EventArgs e)
        {
            FullTree.CollapseAll();
        }

        private void GoToTask(TreeView tv, TreeNode SelectedNode)
        {
            //процедура переноса конкретного узла из главного дерева в конкретное дерево заданий
            //поэтому этот вариант с параметрами: определенными узлом и деревом
            TreeNode clonedNode = (TreeNode)SelectedNode.Clone(); clonedNode.BackColor = FullTree.BackColor;
            TreeNode parentNode = SelectedNode.Parent; TreeNode copyParentNode = null;
            TreeNode[] nodeToFind = tv.Nodes.Find(SelectedNode.Name, true);//если узла с таким именем в дереве заданий нет, то добавляем          

            if (nodeToFind.Length == 0)//т.е. массив пустой
            {
                while (parentNode != null)
                {
                    TreeNode[] parentToFind = tv.Nodes.Find(parentNode.Name, true);
                    if (parentToFind.Length == 0)//если родитель не найден в дереве заданий
                    {   //приходится вручную копировать св-ва родительского узла, т.к. клонирование копирует также все дочерние
                        //а мне нужно вставлять по одному дочернему
                        copyParentNode = new TreeNode(parentNode.Text);
                        copyParentNode.Name = parentNode.Name;
                        copyParentNode.Tag = parentNode.Tag;
                        copyParentNode.ImageIndex = parentNode.ImageIndex;
                        copyParentNode.SelectedImageIndex = parentNode.SelectedImageIndex;
                        copyParentNode.Nodes.Add(clonedNode);
                        clonedNode = copyParentNode;
                    }
                    else
                    {
                        parentToFind[0].Nodes.Add(clonedNode);//если родитель был найден в дереве заданий, то сразу добавляем узел в его массив дочерних
                        return;
                    }
                    parentNode = parentNode.Parent;
                }
                tv.Nodes.Add(clonedNode);
            }
        }

        public void GoToTask(TreeNode SelectedNode)
        {
            //эта процедура переносит выделенный узел SelectedNode, по его типу определяя в какое дерево пойдет
            if (SelectedNode != null)
            {
                //если закидываем по одному
                //PLC
                if (SelectedNode.Tag.GetType() == typeof(Mercury225PLC1) || SelectedNode.Tag.GetType() == typeof(MercuryPLC1))
                {
                    GoToTask(TVTaskPLC, SelectedNode);
                    TWPageControl.SelectedTab = TWTaskPLCPage;
                }
                //RS485
                if (SelectedNode.Tag is ICounter && SelectedNode.Tag.GetType() != typeof(MercuryPLC1))
                {
                    ICounter counter = (ICounter)SelectedNode.Tag;
                    counter.LoadLastEnergyIntoEnergyList();
                    GoToTask(TVTask485, SelectedNode);
                    TWPageControl.SelectedTab = TWTask485Page;
                }

                //если закидываем весь шлюз или модем программа должна сама разбить их на деревья
                if (SelectedNode.Tag is IConnection)
                {
                    //циклимся по узлам подключения
                    foreach (TreeNode CurNode in SelectedNode.Nodes)
                    {   //по типу узла смотрим в какое дерево его поместить
                        //PLC
                        if (CurNode.Tag.GetType() == typeof(Mercury225PLC1) || CurNode.Tag.GetType() == typeof(MercuryPLC1))
                        {
                            GoToTask(TVTaskPLC, CurNode);
                        }
                        //RS485
                        if (CurNode.Tag is ICounter && CurNode.Tag.GetType() != typeof(MercuryPLC1))
                        {
                            ICounter counter = (ICounter)CurNode.Tag;
                            counter.LoadLastEnergyIntoEnergyList();
                            GoToTask(TVTask485, CurNode);
                        }
                    }
                }
            }
        }

        public void TransferAllTree()
        {
            ProgressBar.Value = 0;
            ProgressBar.Maximum = FullTree.Nodes.Count;

            foreach (TreeNode CurNode in (FullTree.Nodes))
            {
                GoToTask(CurNode);
                ProgressBar.Value += 1;
            }
        }

        private void GoToTaskButt_Click(object sender, EventArgs e)
        {
            GoToTask();
        }

        private void GoToTask()
        {
            DisableControls();
            if (selectedNodesListGlobal.Count > 0)
            {
                ProgressBar.Value = 0;
                ProgressBar.Maximum = selectedNodesListGlobal.Count;

                foreach (TreeNode CurNode in selectedNodesListGlobal)
                {
                    GoToTask(CurNode);
                    ProgressBar.Value += 1;
                }
            }
            selectedNodesListClear(selectedNodesListGlobal);
            EnableControls();
            this.ActiveControl = FullTree;
        }

        private void ExitButt_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AboutFormButt_Click(object sender, EventArgs e)
        {
            AboutBox AboutForm = new AboutBox();
            AboutForm.ShowDialog();
        }

        private void selectedNodesListClear(List<TreeNode> list)
        {
            //для каждого узла из коллекции отменяем подсветку
            foreach (TreeNode node in list)
            {
                node.BackColor = FullTree.BackColor;
            }
            //очищаем коллекцию
            list.Clear();
        }

        private void selectedNodesListAppend(TreeNode node, List<TreeNode> list)
        {   //добавление указанного узла в коллекцию
            try
            {
                list.Add(node);
                node.BackColor = Color.LightGreen;
            }
            catch
            {
                selectedNodesListClear(selectedNodesListGlobal);
                return;
            }
        }

        private void selectNodesViaShift(TreeNode firstNode, TreeNode lastNode, TreeView tv)
        {
            try
            {
                //если уровни узлов неравны, то выходим
                if (firstNode.Level != lastNode.Level) { return; }
                TreeNode curNode;//текущий узел          
                curNode = firstNode;//текущий узел приравниваем первому
                                    //пока текущий не станет равен последнему, добавляем все видимые в коллекцию
                while (!curNode.Equals(lastNode))
                {
                    if (curNode.Level == firstNode.Level) { selectedNodesListAppend(curNode, selectedNodesListGlobal); }//в коллекцию встают только узлы одного уровня     
                    int firstnodeabsindex = Utils.GetIndex(firstNode);
                    int lastnodeabsindex = Utils.GetIndex(lastNode);
                    if (firstnodeabsindex < lastnodeabsindex) { curNode = curNode.NextVisibleNode; }//если первый узел стоит раньше последнего, то двигаем сверху-вниз               
                    if (firstnodeabsindex > lastnodeabsindex) { curNode = curNode.PrevVisibleNode; }//если последний узел стоит раньше первого, то двигаем снизу-вверх
                    //if (firstNode.Index == lastNode.Index) { return; }
                }
                //безусловно добавляем последний узел в коллекцию
                selectedNodesListAppend(lastNode, selectedNodesListGlobal);
            }
            catch
            {

            }
        }


        private void SelectNode(TreeViewEventArgs e, TreeView tv)
        {
            if (tv == FullTree)
            {
                TVTask485.SelectedNode = null;
                TVTaskPLC.SelectedNode = null;
            }

            if (tv == TVTask485)
            {
                FullTree.SelectedNode = null;
                TVTaskPLC.SelectedNode = null;
            }

            if (tv == TVTaskPLC)
            {
                FullTree.SelectedNode = null;
                TVTask485.SelectedNode = null;
            }
            //блок мультивыбора - при зажатом контроле
            if (TreeView.ModifierKeys == Keys.Control)
            {
                selectedNodesListAppend(e.Node, selectedNodesListGlobal);
            }
            else
            {
                if (TreeView.ModifierKeys != Keys.Shift)
                {
                    //запоминаем "первый" узел для операции с шифтом 
                    firstNode = e.Node;
                }
                selectedNodesListClear(selectedNodesListGlobal);//очищаем коллекцию при нажатии без зажатой клавиши, т.е. нет никакого мультивыбора             
                if (TreeView.ModifierKeys == Keys.Shift && firstNode != null)//если зажат шифт
                {
                    selectNodesViaShift(firstNode, e.Node, tv);
                    return;
                }
                selectedNodesListAppend(e.Node, selectedNodesListGlobal);
            }
            globalSelectedNode = tv.SelectedNode;
            LoadNodeData(globalSelectedNode);
            GC.Collect();
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectNode(e, sender as TreeView);
        }

        private void TWTaskCollapseButt_Click(object sender, EventArgs e)
        {
            switch (TWPageControl.SelectedIndex)
            {
                case 0:
                    { CollapseTree(TVTask485); }
                    break;
                case 1:
                    { CollapseTree(TVTaskPLC); }
                    break;
            }
        }

        private void CollapseTree(TreeView TW)
        {
            TW.CollapseAll();
        }

        private void ExpandTree(TreeView TW)
        {
            TW.ExpandAll();
        }

        private void ExportDataGrid(TreeNode selectedNode, DataGridView dg)
        {
            //процедура одиночной выгрузки данных для определённого грида выбранного узла
            CommonReports cr = new CommonReports(dg, ProgrammLogEdit);
            //запоминаем текущее время для отображения в логе
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт выбранного грида в Excel";
            ProgressBar.Value = 0;

            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "\r");
                ProgrammLogEdit.ScrollToCaret();
                ProgressBar.Maximum = 2;
            }));

            cr.ExportToExcel(selectedNode);
            Invoke(new Action(delegate
            {
                ProgressBar.Value += 1;
            }));
            //cr.SaveWorkBook("123");
            cr.OpenAfterExport();
            //запоминаем время после отработки для отображения в логе
            currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgressBar.Value += 1;
            }));
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }

        private void ExportDataGrid(TreeView TV, DataGridView dg, string bookName, string path_to_save)
        {
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт текущего задания в Excel";
            //запоминаем текущее время для отображения в логе
            //процедура групповой выгрузки данных для указанного дерева заданий

            this.Invoke(new Action(delegate
            {
                ProgressBar.Value = 0;
                ProgressBar.Maximum = TV.Nodes.Count;
            }));

            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            CommonReports cr = new CommonReports(dg, ProgrammLogEdit);
            //циклимся по подключениям
            foreach (TreeNode connectionNode in TV.Nodes) //всё-таки коллекция узлов дерева это чисто верхний уровень
            {//циклимся по счётчикам\концентраторам
                foreach (TreeNode childNode in connectionNode.Nodes)
                {
                    LoadNodeData(childNode);//грузим данные                   
                    cr.ExportToExcel(childNode);//выводим грид на выгрузку
                }
                Invoke(new Action(delegate { ProgressBar.Value += 1; }));
            }
            //запоминаем время после отработки для отображения в логе
            currentDate = DateTime.Now;
            if (path_to_save != String.Empty)
            {//если путь не пустой, то сохраняем книгу на винте
                cr.SaveWorkBook(bookName, path_to_save);
            }
            else
            {//иначе просто открываем 
                cr.OpenAfterExport();
            }

            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }      

        private string ExportPowerProfileDataGridAfterScheduledTask(List<TreeNode> TV, DataGridView dg, string bookName, string path_to_save, DateTime daten, DateTime datek, int count_sum)
        {//здесь выгружаем профиль мощности для отправки отчёта по почте после задания, выполненного по расписанию
            DateTime currentDate = DateTime.Now;//запоминаем текущее время для отображения в логе
            string CommandText = " Экспорт текущего задания в Excel";

            this.Invoke(new Action(delegate
            {
                ProgressBar.Value = 0;
                ProgressBar.Maximum = TV.Count;          
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            CommonReports cr = new CommonReports(dg, ProgrammLogEdit);
            //циклимся по подключениям
            foreach (TreeNode connectionNode in TV) //всё-таки коллекция узлов дерева это чисто верхний уровень
            {//циклимся по счётчикам
                foreach (TreeNode childNode in connectionNode.Nodes)
                {
                    ICounter counter = (ICounter)childNode.Tag;

                    if (this.InvokeRequired)
                    {//если вызов пошёл из другого потока
                        this.Invoke(new Action(delegate ()
                        {
                            ShowTabPage(PowerProfilePage, DeviceTabControl);//нужно вызвать это чтобы вкладка с таблицей профиля мощности не исчезла, а вместе с ней все колонки в таблице 
                        }));
                    }
                    else
                    {
                        ShowTabPage(PowerProfilePage, DeviceTabControl);//нужно вызвать это чтобы вкладка с таблицей профиля мощности не исчезла, а вместе с ней все колонки в таблице 
                    }
                    LoadProfileIntoCounter(counter, count_sum, true, daten, datek);//грузим данные. Здесь для загрузки профиля берём даты из задания, а не из контролов на форме (как было раньше)
                    cr.ExportToExcel(childNode);//выводим грид на выгрузку
                }
                this.Invoke(new Action(delegate 
                {
                    ProgressBar.Value += 1;
                }));
            }
            //запоминаем время после отработки для отображения в логе
            currentDate = DateTime.Now;
            string fullbookname = String.Empty;//полное имя сохраненной книги
            if (path_to_save != String.Empty)
            {//если путь не пустой, то сохраняем книгу
                fullbookname = cr.SaveWorkBook(bookName, path_to_save);
            }
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнено" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            return fullbookname;// возвращаем полное имя сохраненной книги
        }

        private void DataGridContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void TWMainRefreshButt_Click(object sender, EventArgs e)
        {
            if (this.ConnectToDataBase() == false)
            {
                MessageBox.Show("Подключение к базе данных не удалось!", "Ошибка подключения к базе данных", MessageBoxButtons.OK, MessageBoxIcon.Error);//перезагружаем рабочий датасет          
            }
        }

        private void TWTask485_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void TWtaskExpandButt_Click(object sender, EventArgs e)
        {
            switch (TWPageControl.SelectedIndex)
            {
                case 0:
                    { ExpandTree(TVTask485); }
                    break;
                case 1:
                    { ExpandTree(TVTaskPLC); }
                    break;
            }
        }

        private void TransferAllTreeButt_Click(object sender, EventArgs e)
        {
            DisableControls();
            TransferAllTree();
            EnableControls();
        }

        private void TWMain_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left) return; //левой кнопкой перетаскивать нельзя

            FullTree.SelectedNode = (TreeNode)e.Item;
            FullTree.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void TWMain_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void TWMain_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;

            Bitmap bmp = drawNodeText(FullTree.SelectedNode.Bounds, FullTree.SelectedNode.Text, FullTree.Font);
            Clipboard.SetImage(bmp);
            System.Windows.Forms.Cursor newCur = new System.Windows.Forms.Cursor(bmp.GetHicon());

            FullTree.Cursor = newCur;
        }

        private static Bitmap drawNodeText(Rectangle r, string txt, Font NodeFont)
        {
            Bitmap nb = new Bitmap(r.Width, r.Height);
            Graphics g = Graphics.FromImage(nb);
            StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            };

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(txt, NodeFont, Brushes.ForestGreen, 1, 1);
            g.Flush();

            return nb;
        }

        private void TWMain_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Point targetPoint = FullTree.PointToClient(new Point(e.X, e.Y)); // Получаем точку сброса узла (в координатах) в рамках клиента (TVMain)
                TreeNode targetNode = FullTree.GetNodeAt(targetPoint); // Запоминаем целевой узел в точке сброса targetPoint
                TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));// запоминаем узел, который перетаскиваем.
                 //блок запретов на перетаскивание по типам узлов
                if (draggedNode.Equals(targetNode) && targetNode != null)//сам на себя или в никуда
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag.GetType() == draggedNode.Tag.GetType())//проверка по ПОЛНОМУ совпадению типов (не по интерфейсу)
                {//одинаковые узлы перетаскивать мы не можем, но делаем исключение для счётчиков в целях копирования
                    if (targetNode.Tag is ICounter && targetNode.Tag.GetType() != typeof(MercuryPLC1))//для RS-счётчиков
                    {
                        DialogResult result = MessageBox.Show("Копировать счётчик?", "Подтверждение копирования", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.No)
                        {
                            FullTree.Cursor = Cursors.Default;
                            return;
                        }

                        IDevice draggedNodeDevice = (IDevice)draggedNode.Tag;
                        IDevice targetNodeDevice = (IDevice)targetNode.Tag;
                        DataTable deviceRow = DataBaseManagerMSSQL.Return_CounterRS_Row(draggedNodeDevice.ID);//возвращаем данные счётчика-источника           

                        double ca = 0;
                        string id = DevicePropertiesGrid.Rows[15].Cells[1].Value.ToString();

                        if (DevicePropertiesGrid.Rows[14].Cells[1].Value.ToString() == String.Empty)
                            ca = 0;
                        else
                            ca = Convert.ToDouble(DevicePropertiesGrid.Rows[14].Cells[1].Value.ToString());

                        DataBaseManagerMSSQL.Update_CounterRS_Row(targetNodeDevice.ID,
                                                   deviceRow.Rows[0][1].ToString(),  //name
                                                   deviceRow.Rows[1][1].ToString(),  //street
                                                   deviceRow.Rows[2][1].ToString(),  //house
                                                   targetNodeDevice.ID.ToString(),   //serial_number
                                   Convert.ToInt16(deviceRow.Rows[4][1].ToString()), //net_adress
                                                   deviceRow.Rows[5][1].ToString(),  //comments
                                                   deviceRow.Rows[6][1].ToString(),  //district
                 Convert.ToInt16(Convert.ToBoolean(deviceRow.Rows[8][1].ToString())),//ppexist
                 Convert.ToInt16(Convert.ToBoolean(deviceRow.Rows[9][1].ToString())),//intfeed
                                                   deviceRow.Rows[10][1].ToString(), //pwd1
                                                   deviceRow.Rows[11][1].ToString(),//pwd2 
                                   Convert.ToInt16(deviceRow.Rows[13][1].ToString()),
                                   ca,
                                   id
                                   );//transformation_rate
                        targetNode.Text = draggedNode.Text;
                    }

                    if (targetNode.Tag.GetType() == typeof(MercuryPLC1))//PLC-счётчики
                    {
                        DialogResult result = MessageBox.Show("Копировать счётчик?", "Подтверждение копирования", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.No)
                        {
                            FullTree.Cursor = Cursors.Default;
                            return;
                        }
                        IDevice sourceNodeDevice = (IDevice)draggedNode.Tag;//счётчик-источник
                        IDevice targetNodeDevice = (IDevice)targetNode.Tag;//счётчик-мишень
                        DataTable deviceRow = DataBaseManagerMSSQL.Return_CounterPLC_Row(sourceNodeDevice.ID);//возвращаем данные счётчика-источника  
                       //обновим счётчик-источник чтобы забрать у него серийный номер и избежать исключения, возникающего при конфликте серийных номеров (одинаковых серийных номеров не может быть)
                               DataBaseManagerMSSQL.Update_CounterPLC_Row(sourceNodeDevice.ID,
                                                   deviceRow.Rows[0][1].ToString(), //name
                                                   deviceRow.Rows[1][1].ToString(), //street
                                                   deviceRow.Rows[2][1].ToString(), //house
                                                   sourceNodeDevice.ID.ToString(),  //serial_number. Сюда пишем идентификатор чтобы избежать конфликта серийных номеров
                                   Convert.ToInt16(deviceRow.Rows[4][1].ToString()),//net_asdress
                                                   deviceRow.Rows[5][1].ToString(), //comments
                                                   deviceRow.Rows[6][1].ToString(), //district
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   null,
                                                   null,
                                                   null,
                                                   null,
                                                   null
                                                   ); 
                       //обновляем целевой счётчик по данным счётчика-источника                                                                                                                                                                                          
                        DataBaseManagerMSSQL.Update_CounterPLC_Row(targetNodeDevice.ID,
                                                   deviceRow.Rows[0][1].ToString(), //name
                                                   deviceRow.Rows[1][1].ToString(), //street
                                                   deviceRow.Rows[2][1].ToString(), //house
                                                   deviceRow.Rows[3][1].ToString(),  //serial_number
                                   Convert.ToInt16(deviceRow.Rows[4][1].ToString()),//net_asdress
                                                   deviceRow.Rows[5][1].ToString(), //comments
                                                   deviceRow.Rows[6][1].ToString(), //district
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   null,
                                                   null,
                                                   null,
                                                   null,
                                                   null
                                                   );

                        targetNode.Text = draggedNode.Text;
                    }
                    FullTree.Cursor = Cursors.Default;
                    return;
                }
                //перетягиваем шлюз на модем и наоборот
                if ((draggedNode.Tag.GetType() == typeof(Mercury228) || draggedNode.Tag.GetType() == typeof(Modem))
                    && (targetNode.Tag.GetType() == typeof(Mercury228) || targetNode.Tag.GetType() == typeof(Modem)))
                {
                    DialogResult result = MessageBox.Show("Копировать подключение?", "Подтверждение копирования", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        FullTree.Cursor = Cursors.Default;
                        return;
                    }

                    IDevice draggedNodeDevice = (IDevice)draggedNode.Tag;
                    IDevice targetNodeDevice = (IDevice)targetNode.Tag;
                    DataTable deviceRow = DataBaseManagerMSSQL.Return_Connection_Row(draggedNodeDevice.ID);//возвращаем данные подключения

                    DataBaseManagerMSSQL.Update_Connection_Row(targetNodeDevice.ID,
                                                                deviceRow.Rows[0][1].ToString(),//name
                                                                deviceRow.Rows[1][1].ToString(),//phone_number
                                                                deviceRow.Rows[2][1].ToString(),//ip
                                                                deviceRow.Rows[3][1].ToString(),//port
                                                                deviceRow.Rows[5][1].ToString(),//init_string
                                                                false,//autoconf_bool
                                                                String.Empty,//autoconf_str
                                                                deviceRow.Rows[8][1].ToString(),//district
                                                                deviceRow.Rows[9][1].ToString(),//street
                                                                deviceRow.Rows[10][1].ToString(),//house
                                                                deviceRow.Rows[11][1].ToString()//comments
                                                                );

                    targetNode.Text = draggedNode.Text;
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag.GetType() == typeof(Mercury225PLC1)
                && (//на концентратор можно перетаскивать PLC-счётчики и другие концентраторы
                   (draggedNode.Tag is ICounter && draggedNode.Tag.GetType() != typeof(MercuryPLC1))//убедимся, что перетаскиваемый узел - RSсчётчик (реализует ICounter, но при этом не MercuryPLC1)
                 || draggedNode.Tag is IConnection//убедимся что перетаскиваемый узел - не подключение
                   )
                   )
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag is ICounter && targetNode.Tag.GetType() != typeof(MercuryPLC1)//для RS-счётчиков
                && (//на RS-счётчик можно перетаскивать только другой RS-счётчик ТАКОГО ЖЕ ТИПА (обработка этого варианта происходить ранее - проверка полного совпадения типов - см выше)
                   draggedNode.Tag is IConnection
                || draggedNode.Tag is IConcentrator
                || draggedNode.Tag is ICounter
                  ))
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag.GetType() == typeof(MercuryPLC1)
                && (//на PLC-счётчик можно перетаскивать только другой PLC-счётчик (обработка этого варианта происходить ранее)
                   draggedNode.Tag is IConnection
                || draggedNode.Tag is IConcentrator
                || draggedNode.Tag is ICounter
                   ))                 
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag.GetType() == typeof(Mercury228)
                && (
                        draggedNode.Tag.GetType() == typeof(MercuryPLC1)
                   )
                   )
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }

                if (targetNode.Tag.GetType() == typeof(Modem)
                && (
                        draggedNode.Tag.GetType() == typeof(MercuryPLC1)
                   )
                   )
                {
                    FullTree.Cursor = Cursors.Default;
                    return;
                }
                //подтверждаем, что целевой узел (в точке сброса) не равен исходному узлу
                //и что целевой узел не равен пустоте
                if (!draggedNode.Equals(targetNode) && targetNode != null)
                {
                    draggedNode.Remove();//удаляем исходный узел 
                    targetNode.Nodes.Add(draggedNode);//добавляем его в дочерние к целевому              
                    targetNode.Expand();//разворачиваем целевой узел
                    //теперь нужно сделать соответствующие изменения в базе данных
                    IDevice draggedNodeDevice = (IDevice)draggedNode.Tag;
                    IDevice targetNodeDevice = (IDevice)targetNode.Tag;
                    DataBaseManagerMSSQL.Reassign_Parent(draggedNodeDevice.ID, targetNodeDevice.ID, draggedNode.Tag);
                }
                FullTree.SelectedNode = null;
                FullTree.Cursor = Cursors.Default;
            }
            catch
            {
                FullTree.Cursor = Cursors.Default;
                return;
            }
        }

        private void ExpandSelectedNodesButt_Click(object sender, EventArgs e)
        {
            DisableControls();
            if (selectedNodesListGlobal.Count > 0)
            {
                foreach (TreeNode node in selectedNodesListGlobal)
                {
                    node.Expand();
                }
            }
            else
            {
                FullTree.ExpandAll();
            }
            EnableControls();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            //Utils.DoubleBufferToolStrip(ProgressBar, true);

            DeviceTabControl.TabPages.Remove(ReadEnergyPage); DeviceTabControl.TabPages.Remove(ReadParamsPage);
            DeviceTabControl.TabPages.Remove(ReadJournalPage); DeviceTabControl.TabPages.Remove(PowerProfilePage);
            DeviceTabControl.TabPages.Remove(WriteParamsPage); DeviceTabControl.TabPages.Remove(MonitorPage);
            DeviceTabControl.TabPages.Remove(ImagePage); DeviceTabControl.TabPages.Remove(ConfigurePage);

            DateTimeEditN.Value = DateTime.Now; DateTimeEditK.Value = DateTime.Now;
            DefaultBackupPathTextBox.Text = Properties.Settings.Default.DefaultBackupPath;//читаем путь к резервным копиям базы из класса настроек
            //согласно роли пользователя определяем, какие контролы надо скрыть
            switch (User.Role)
            {
                case "editor":
                    {
                        UsersFormButt.Visible = false;
                        BackupDbFlowLayout.Visible = false;
                        FixOverconsumeButt.Visible = false;
                        break;
                    }

                case "reader":
                    {
                        UsersFormButt.Visible = false;
                        BackupDbFlowLayout.Visible = false;
                        ObjectsAndSearchFormButt.Visible = false;
                        SettingsFormButt.Visible = false;
                        ApplyChangesButt.Visible = false;
                        WriteParametersButt.Visible = false;
                        AddImageButt.Visible = false;
                        FixOverconsumeButt.Visible = false;
                        MessageBox.Show("Вы входите в программу с ограниченными правами: только просмотр и опрос. Нажмите 'ОК' для продолжения.", "Ограничение доступа",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    }

                case "observer":
                    {
                        UsersFormButt.Visible = false;
                        BackupDbFlowLayout.Visible = false;
                        ObjectsAndSearchFormButt.Visible = false;
                        SettingsFormButt.Visible = false;
                        ApplyChangesButt.Visible = false;
                        TaskFormButt.Visible = false;
                        ChangesLogButt.Visible = false;
                        ProgrammLogEdit.Visible = false;
                        StatusToolStrip.Visible = false;
                        MonitorToolStrip.Visible = false;
                        Read485Butt.Visible = false;
                        TasksTreeRS.Visible = false;
                        ReadPLCButt.Visible = false;
                        WriteParametersButt.Visible = false;
                        AddImageButt.Visible = false;
                        FixOverconsumeButt.Visible = false;
                        MessageBox.Show("Вы входите в программу с ограниченными правами: только просмотр. Нажмите 'ОК' для продолжения.", "Ограничение доступа",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                    }
            }

            if (this.ConnectToDataBase() == false) //после удачного подключения подгружаем главное дерево
            {
                MessageBox.Show("Подключение к базе данных не удалось!", "Ошибка подключения к базе данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //this.Location = Properties.Settings.Default.MainFormLocation;
            //this.Size = Properties.Settings.Default.MainFormSize;
        }

        private void PortsComboBox_MouseClick_1(object sender, MouseEventArgs e)
        {

        }

        private void ATTextBox_KeyDown(object sender, KeyEventArgs e)
        {

        }

        public bool SendTextToPort(string txt, string termTxt, string port, bool autoClosePort, DataProcessing dp, int timeout, RichTextBox rtb)
        {
            if (port == String.Empty)
            {
                MessageBox.Show("Не выбран порт!", "Ошибка порта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            //запоминаем текущее время для отображения в логе
            DateTime currentDateA = DateTime.Now;

            rtb.Invoke(new Action(delegate
            {
                rtb.AppendText(currentDateA + "." + currentDateA.Millisecond + " Выполнение команды " + txt + "\r");
                rtb.ScrollToCaret();
            }));

            Exception ex = dp.SendData(txt + "\r");
            if (ex != null)
            {
                rtb.Invoke(new Action(delegate
                {
                    currentDateA = DateTime.Now;
                    rtb.SelectionColor = Color.Red;
                    rtb.AppendText(currentDateA + "." + currentDateA.Millisecond +
                     " Ошибка выполнения коиманды: " + txt + " " + ex.Message + "\r");
                    rtb.ScrollToCaret();
                }));
                return false;
            }

            string rezStr = dp.Read(termTxt, timeout);

            //если вернулась ошибка, то говорим что процедура не отработала            
            if (rezStr.Contains("ERROR"))
            {
                return false;
            }

            //запоминаем время после отработки для отображения в логе
            DateTime currentDateB = DateTime.Now;
            //для вывода прошедшего времени заводим отдельную переменную чтобы отформатировать миллисекунды
            string ElapsedTime = currentDateB.Subtract(currentDateA).ToString();
            //ElapsedTime = ElapsedTime.Remove(12);
            rtb.Invoke(new Action(delegate
            {
                rtb.AppendText(currentDateB + "." + currentDateB.Millisecond +
                 " Команда " + txt + " выполнена. Время выполнения: " + ElapsedTime + "\r");
                rtb.ScrollToCaret();
            }));
            //-----------------------------------------------------------------------
            Thread.Sleep(500);
            //-----------------------------------------------------------------------
            //смотрим если хотим закрыть порт. Например закрытие порта нам не нужно если мы висим на звонке и готовимся читать данные
            if (autoClosePort == true)
            {
                dp.ClosePort();
                Thread.Sleep(300);
            }
            return true;
        }

        private void TWMain_KeyDown(object sender, KeyEventArgs e)
        {//перенос узлов по нажатию стрелки влево       
            if (e.KeyCode == Keys.Left) GoToTask();
        }

        private void TWTaskPLC_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //selectedNodesListClear(selectedNodesListGlobal);
            //TVTask485.SelectedNode = null;
            //FullTree.SelectedNode = null;
            //globalSelectedNode = TVTaskPLC.SelectedNode;
            //LoadNodeData(globalSelectedNode);
        }

        private bool ConnectToDataBase()
        {
            DataSet DataBaseSet = null;

            try
            {
                if (DataBaseManagerMSSQL.ConnectToDB(ServNameTextBox.Text, DbNameTextBox.Text, UserNameTextBox.Text, PwdTextBox.Text) == false)
                {//попытка подключиться к базе данных
                    this.Invoke(new Action(delegate ()
                    {
                        Cursor = Cursors.Default;
                    }));
                    return false;
                }
            }
            catch (Exception ex)
            {
               this.Invoke(new Action(delegate ()
                {
                    Cursor = Cursors.Default;
                }));
                System.Diagnostics.EventLog.WriteEntry("Media", ex.StackTrace, System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }

            DateTime currentDateA = DateTime.Now;

            string CommandText = " Загрузка главного дерева";

            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDateA + "." + currentDateA.Millisecond + CommandText + "\r");
                ProgrammLogEdit.ScrollToCaret();

                FullTree.Nodes.Clear();
                FullTree.BeginUpdate();            
                FullTree.Enabled = false;
                DisableControls();                                               
            }));

            DataBaseManagerMSSQL cm = new DataBaseManagerMSSQL();          
            
            int count = cm.count();//сначала нужно посчитать, сколько их вообще (подключений)
            int a = count / 100;//сколько порций по 100 подключений (целое количество раз)

            for (int i = 0; i <= a; i++)
            {//циклимся по "сотням" подключений
                try
                {
                    DataBaseSet = cm.CreateWorkDataSet(i);//создаём датасет с очередной порцией подключений
                    LoadAllObjects(DataBaseSet);//грузим в дерево очеердную порцию подключений
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(delegate ()
                    {
                        Cursor = Cursors.Default;
                    }));
                    System.Diagnostics.EventLog.WriteEntry("Media", ex.StackTrace, System.Diagnostics.EventLogEntryType.Warning);
                    return false;
                }                
            }

            DateTime currentDateB = DateTime.Now;

            this.Invoke(new Action(delegate
            {
                EnableControls();
                FullTree.Enabled = true;
                ProgrammLogEdit.AppendText(currentDateB + "." + currentDateB.Millisecond + CommandText + " завершена\r");
                ProgrammLogEdit.ScrollToCaret();
                TWMainRefreshButt.Visible = true;
                FullTree.EndUpdate();
                
                ActiveControl = FullTree;
                if (!DisableMonitoringButt.Enabled)
                {
                    EnableMonitoringButt.Enabled = true;
                }
            }));

            this.Invoke(new Action(delegate
            {
                this.Location = Properties.Settings.Default.MainFormLocation;
                this.Size = Properties.Settings.Default.MainFormSize;
            }));

            return true;
        }

        private void ProgrammLogContextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name)
            {
                case "ProgContItem1":
                    ProgrammLogEdit.SelectAll();
                    ProgrammLogEdit.Copy();
                    ProgrammLogEdit.DeselectAll();
                    break;
            }
        }

        private void InitModemButt_Click(object sender, EventArgs e)
        {

        }

        private SerialPort InitModem(DataProcessing dp, int rate, SerialPort sp, RichTextBox rtb)
        {//процедура поиска и инициализации модема перед опросом
            try
            {
                Thread.Sleep(100);
                DateTime currentDate = DateTime.Now;
                rtb.Invoke(new Action(delegate
                {
                    rtb.AppendText(currentDate + "." + currentDate.Millisecond + " ...\r");
                    rtb.ScrollToCaret();
                }));
                int qports = DataProcessing.ReturnAvailablePorts(rtb).Count;
                if (qports == 0) { return null; }
                rtb.Invoke(new Action(delegate
                {
                    currentDate = DateTime.Now;
                    rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Доступных портов: " + qports.ToString() + " ...\r");
                    rtb.ScrollToCaret();
                }));
                //ищем свободный порт и подключенный к нему свободный модем
                foreach (string port in DataProcessing.ReturnAvailablePorts(rtb))
                {
                    rtb.Invoke(new Action(delegate
                    {
                        currentDate = DateTime.Now;
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " ...\r");
                        rtb.ScrollToCaret();
                    }));
                    sp = dp.createSerialPort(port, rate);
                    rtb.Invoke(new Action(delegate
                    {
                        currentDate = DateTime.Now;
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " ...\r");
                        rtb.ScrollToCaret();
                    }));
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Пробуем " + port.ToString() + "...\r");
                        rtb.ScrollToCaret();
                    }));
                    if (SendTextToPort("at", "OK", port, false, dp, 200, rtb) == false) { sp.Close(); continue; }
                    if (SendTextToPort("at+creg?", "1", port, false, dp, 200, rtb) == false) { sp.Close(); continue; }
                    else
                    {
                        DateTime currentDateC = DateTime.Now;
                        rtb.Invoke(new Action(delegate
                        {
                            rtb.SelectionColor = Color.Green;
                            rtb.AppendText(currentDateC + "." + currentDateC.Millisecond + " Найден модем " + port + "\r");
                            rtb.ScrollToCaret();
                        }));
                        return sp; //если свободный порт с модемом найден, то запоминаем его и выходим из процедруы
                    }
                }
                //если нет модема ни на одном из свободных портов, то выходим
                if (sp.IsOpen == false)
                {
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " false. Return null\r");
                        rtb.ScrollToCaret();
                    }));
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка инциализации порта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            rtb.Invoke(new Action(delegate
            {
                DateTime currentDate = DateTime.Now;
                rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Return " + sp.PortName + "\r");
                rtb.ScrollToCaret();
            }));
            return sp;
        }

        private void DisableControls()
        {   //процедура, отключающая визуальные компоненты чтобы нельзя было внести изменения в структуру или свойства объектов во время опроса
            this.Invoke(new Action(delegate
             {
                 TVTask485.Enabled = false;
                 TVTaskPLC.Enabled = false;
                 FullTree.Enabled = false;
                 DeviceTabControl.Enabled = false;
                 TVMainToolStrip.Enabled = false;
                 TVTaskCollapseButt.Enabled = false;
                 TVTaskExpandButt.Enabled = false;
                 //StopButton.Enabled = true;
                 MainMenu.Enabled = false;
                 Cursor = Cursors.WaitCursor;
             }));
        }

        private void EnableControls()
        {   //процедура, включающая визуальные компоненты. См. DisableControls()
            this.Invoke(new Action(delegate
            {
                {
                    TVTask485.Enabled = true;
                    TVTaskPLC.Enabled = true;
                    FullTree.Enabled = true;
                    DeviceTabControl.Enabled = true;
                    TVMainToolStrip.Enabled = true; ;
                    TVTaskCollapseButt.Enabled = true;
                    TVTaskExpandButt.Enabled = true;
                    MainMenu.Enabled = true;
                    //StopButton.Enabled = false;
                    Cursor = Cursors.Default;
                }
            }));
        }

        private BackgroundWorkerDoWorkResult TaskReading(List<TreeNode> Task, bool GetPowerProfileBool, DateTime DateTimeN, DateTime DateTimeK,
            int PeriodsCount, RichTextBox rtb, BackgroundWorker worker, ToolStripProgressBar pb, ToolStripLabel crl, ToolStripLabel lrl, bool ReReadAbsentRecordsBool, ReadingLogForm rlf, string taskname)
        {         
            DateTime currentDate = DateTime.Now;
            rtb.Invoke(new Action(delegate
            {
                rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Ищем свободный модем...\r");
                rtb.ScrollToCaret();
            }));
            if (worker.CancellationPending == true) { return null; } //проверяем, был ли запрос на отмену работы
            SerialPort sp = null;
            DataProcessing dp = new DataProcessing(sp, notifyicon);
            //результат выполнения задания
            BackgroundWorkerDoWorkResult bwdwr = new BackgroundWorkerDoWorkResult(dp, rlf, taskname, true);
            //инициализируем модем и возвращаем рабочий порт               
            while (sp == null && worker.CancellationPending == false)//ждём пока найдётся свободный порт с модемом или пока пользователь не отменит задание
            {
                sp = InitModem(dp, 9600, sp, rtb);
                if (sp == null)
                {
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.SelectionColor = Color.Red;
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка инициализации модема\r");
                        rtb.ScrollToCaret();
                    }));
                }
            }

            if (sp == null) return null;
           
            string WorkingPortName = sp.PortName;

            DateTime currentDateStart = DateTime.Now;
            rtb.Invoke(new Action(delegate
                {
                    rlf.Text += " " + WorkingPortName; 
                    rtb.SelectionColor = Color.DarkViolet;
                    rtb.AppendText(currentDateStart + "." + currentDateStart.Millisecond + " Начинаем опрос\r");
                    rtb.ScrollToCaret();
                }));
            //цикл по соединениям (шлюзы и модемы)           
            foreach (TreeNode connectionNode in Task)
            {//если пользователь нажал кнопочку стоп, то выходим из цикла по подключениям и выходим на остановку опроса
                if (worker.CancellationPending == true) { break; } //проверяем, был ли запрос на отмену работы
                string cbst_str = String.Empty;
                string phone_number = String.Empty;
                string node_name = String.Empty;
               
                var selectedDevice = (IConnection)connectionNode.Tag; cbst_str = selectedDevice.CBST;
                phone_number = selectedDevice.Phone; node_name = selectedDevice.Name;
                //строка инициализации модема берётся из настроек подключения
                if (SendTextToPort("at+cbst=" + cbst_str, "OK", WorkingPortName, false, dp, 1000, rtb) == false)
                {
                    DateTime currentDateD = DateTime.Now;
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.SelectionColor = Color.Red;
                        rtb.AppendText(currentDateD + "." + currentDateD.Millisecond + " Ошибка команды at+cbst во время опроса. Останавливаем опрос\r");
                        rtb.ScrollToCaret();
                    }));
                    //dp.ClosePort();//теперь порт закрывается по полному окончанию работы потока в событии RunWorkerCompleted
                    return bwdwr;
                }

                if (ReReadAbsentRecordsBool && GetPowerProfileBool)//если отмечены обе эти галочки, значит мы в групповом опросе хотим доснимать неснятые получасовки
                {                
                    foreach (TreeNode counter in connectionNode.Nodes)
                    {//для этого пройдёмся в цикле по счётчикам, загружая их профиль, т.к процедура считывания профиля требует загрузки профиля в экземпляр счётчика
                        try
                        {
                            DateTime currentDateE = DateTime.Now;
                            ICounter cntr = (ICounter)counter.Tag;
                            LoadProfileIntoCounter(cntr, 0, false, DateTimeN, DateTimeK);
                        }
                        catch (Exception ex)
                        {
                            rtb.Invoke(new Action(delegate
                            {
                                DateTime currentDateE = DateTime.Now;
                                rtb.SelectionColor = Color.Red;
                                rtb.AppendText(currentDateE + "." + currentDateE.Millisecond + " Ошибка загрузки профиля при групповом чтении: " + ex.Message + "\r");
                                rtb.ScrollToCaret();
                            }));
                            continue;
                        }
                    }
                }
                //делаем несколько попыток дозвона
                bool connect = false;
                DateTime currentDateC = DateTime.Now;
                for (int i = 1; i <= 3; i++)
                {                   
                    if (SendTextToPort("atd" + phone_number, "CONNECT", WorkingPortName, false, dp, 60000, rtb) == false)
                    {  //дозвонились или нет?                      
                        rtb.Invoke(new Action(delegate
                        {
                            rtb.SelectionColor = Color.Red;
                            rtb.AppendText(currentDateC + "." + currentDateC.Millisecond + " Соединение с " + node_name + " не установлено\r");
                            rtb.ScrollToCaret();
                        }));

                        Thread.Sleep(3000);//ждём перед следующей попыткой дозвона
                        if (worker.CancellationPending == true) { break; } //проверяем, был ли запрос на отмену работы
                        continue;
                    }
                    else
                    {//если дозвонились, то выходим из цикла                       
                        connect = true;
                        break;
                    }
                }
                //если не дозвонились, то идём на следующий шлюз\модем
                if (connect == false)
                {                   
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.SelectionColor = Color.Red;
                        rtb.AppendText(currentDateC + "." + currentDateC.Millisecond + " Попытки соединения с " + node_name + " исчерпаны\r");
                        rtb.ScrollToCaret();
                    }));

                    Thread.Sleep(500);
                    //дальше надо бы записать в БД ошибку, что не дозвонились
                    var device = (IDevice)connectionNode.Tag; //нужно получить айдишник объекта
                    DataBaseManagerMSSQL.Create_Error_Row(1, selectedDevice.Name, device.ID);
                    continue;//идём на следующее подключение
                }

                rtb.Invoke(new Action(delegate
                {
                     currentDateC = DateTime.Now;
                     rtb.SelectionColor = Color.DarkGreen;
                     rtb.AppendText(currentDateC + "." + currentDateC.Millisecond + " Соединение с " + node_name + " установлено\r");
                     rtb.ScrollToCaret();
                 }));
                //если дозвонились, начинаем посылать запросы и читать ответы--------------------------------------------------------------------------
                selectedDevice.GatherDevicesData(WorkingPortName, connectionNode.Nodes, dp, pb, DateTimeN, DateTimeK, PeriodsCount,
                                                                GetPowerProfileBool, crl, lrl, rtb, worker, ReReadAbsentRecordsBool);
                //-------------------------------------------------------------------------------------------------------------------------------------
                StopCall(dp, WorkingPortName, rtb);
            }//конец foreach (TreeNode connectionNode in TWTask.Nodes)      

            DateTime currentDateFinish = DateTime.Now;
            string ElapsedTime = currentDateFinish.Subtract(currentDateStart).ToString();

            rtb.Invoke(new Action(delegate
            {
                DeviceParametersGrid.Refresh(); DeviceParametersGrid.AutoResizeColumns();
                DeviceEnergyGrid.Refresh(); DeviceEnergyGrid.AutoResizeColumns();
                DeviceJournalGrid.Refresh(); DeviceJournalGrid.AutoResizeColumns();
            }));
           
            return bwdwr;//если всё прошло нормально, то возвращаем результат работы 
        }

        private void StopCall(DataProcessing dp, string workingPort, RichTextBox rtb)
        {//вешаем в этой процедуре трубку (порт всё ещё открыт)
            Thread.Sleep(1100);
            //переводим модем в режим команд 
            byte[] m = { 0x002B, 0x002B, 0x002B }; //+++
            DateTime currentDate = DateTime.Now;
            rtb.Invoke(new Action(delegate
            {
                rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Выполнение команды +++\r");
                rtb.ScrollToCaret(); Thread.Sleep(300);
            }));
            Exception ex = dp.SendData(m, 0);
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                rtb.Invoke(new Action(delegate
                {
                    rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    rtb.ScrollToCaret();
                }));
                return;
            }
            //бросаем трубку
            Thread.Sleep(2000);

            if (SendTextToPort("ath", "OK", workingPort, false, dp, 10000, rtb) == false)
            {
                currentDate = DateTime.Now;
                rtb.Invoke(new Action(delegate
                {
                    rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка команды ath\r");
                    rtb.ScrollToCaret();
                }));
            }
        }

        private void StopExportingToExcel(object sender, EventArgs e)
        {
            if (globalWorkerExportToExcel != null)
            {
                DateTime currentDate = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    FullTree.Enabled = true;
                    TVTaskPLC.Enabled = true;
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Пользователь прервал выгрузку в Excel\r");
                    ProgrammLogEdit.ScrollToCaret();
                }));
                globalWorkerExportToExcel.CancelAsync();
            }
        }

        private void ReadRSButt_Click(object sender, EventArgs e)
        {
            PowerProfileGrid.DataSource = null; //чтобы не блокировать визуальный компонент
            DateTime currentDate = DateTime.Now;
            ProgrammLogEdit.Invoke(new Action(delegate
            {
                ProgrammLogEdit.SelectionColor = Color.DarkOrange;
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Опрос дерева RS-485\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            List<TreeNode> tree = new List<TreeNode>(TVTask485.Nodes.Cast<TreeNode>());//преобразуем дерево в список
            //создаём структуру для передачи в BackgroundWorker
            TaskStruct task = new TaskStruct(tree, -1, GetPowerProfileCheck.Checked, DateTimeEditN.Value, DateTimeEditK.Value, Convert.ToInt16(PeriodNumEdit.Value), String.Empty, false, ReReadAbsentRecords.Checked);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync(task);//передаём структуру задания
        }

        private void TWTaskClearButt_Click(object sender, EventArgs e)
        {//здесь очищаетмся всё дерево заданий rs485
            TVTask485.Nodes.Clear();
        }

        private void GroupExportButt_Click(object sender, EventArgs e)
        {
            //
        }

        private string ExportSelectedParamsForSetOfCounters(List<TreeNode> tree, string path_to_save, string name_to_save)
        {   //процедура выгрузки выбранных параметров для группы счётчиков
            string fullname = String.Empty;
            if (tree.Count == 0) { return fullname; }

            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт выбранных параметров в Excel...";
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();
            }));

            DisableControls();
            //сначала сформируем коллекцию из всех счётчиков в дереве задания
            List<TreeNode> collection = new List<TreeNode>();
            foreach (TreeNode parentNode in tree)
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    collection.Add(childNode);
                    //WeakReference wr = new WeakReference(childNode);
                }
            }
            
            CommonReports cr = new CommonReports(ProgrammLogEdit);
            cr.ExportToExcel(collection);

            if (path_to_save == String.Empty)
            {//если путь пустой, то просто показываем книгу         
                cr.OpenAfterExport();
            }
            else
            {//если путь не пустой, то сохраняем на диск автоматически
                fullname = cr.SaveWorkBook(name_to_save, path_to_save);
            }

            EnableControls();

            currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            return fullname;
        }

        private void ExportActualPowerReport(string month, int hoursshift)
        {
            if (TVTask485.Nodes.Count == 0) { return; }//если нет счётчиков в расписании - выходим
            if (month == String.Empty) { return; }//если месяц не выбран - выходим
            //процедура выгрузки интегрального акта
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт фактической мощности в Excel...";
            //автоматически задаём нижнюю и верхнюю дату на случай если пользователь забудет
            int monint_low = Convert.ToInt16(month.Substring(0, month.IndexOf('.')));//номер первого месяца
            int monint_hi = monint_low + 1;//номер второго месяца
            int yearint_hi = DateTime.Now.Year;//в этой переменной храним номер года верхней границы периода
            int yearint_low = DateTime.Now.Year;//в этой переменной храним номер года нижней границы периода

            if (monint_hi == 13) //если нижний месяц был декабрь, то верхний должен быть январь и нижний год меньше
            {
                monint_hi = 1;
                yearint_low -= 1;
            }

            DateTimeEditN.Value = Convert.ToDateTime("01." + monint_low.ToString() + "." + yearint_low + " 00:00:00");
            DateTimeEditK.Value = Convert.ToDateTime("01." + monint_hi.ToString() + "." + yearint_hi + " 00:00:00");
            DateTimeEditN.Value = DateTimeEditN.Value.AddHours(hoursshift);
            DateTimeEditK.Value = DateTimeEditK.Value.AddHours(hoursshift);

            currentDate = DateTime.Now;

            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            DisableControls();
            //сформируем коллекцию из всех счётчиков в дереве задания
            List<TreeNode> collection = new List<TreeNode>();
            foreach (TreeNode parentNode in TVTask485.Nodes)
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    LoadProfileIntoCounter((ICounter)childNode.Tag, 2, false, DateTimeEditN.Value, DateTimeEditK.Value);//вытаскиваем профиль из базы с одновременным суммированием периодов (по 2 для 30 минут чтобы получился час)          
                    collection.Add(childNode);//добавляем счётчик в коллекцию для формирования отчёта
                }
            }

            CommonReports cr = new CommonReports(ProgrammLogEdit);
            cr.FormActualPowerReport(collection, month, DateTimeEditN.Value, DateTimeEditK.Value, hoursshift);//в этой процедуре заполним необходимые ячейки

            EnableControls();
            currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));

            cr.OpenAfterExport();
        }

        private string ExportIntegralReport(string month, string path_to_save)
        {   //процедура выгрузки интегрального акта
            string fullname = String.Empty;
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт интегрального акта в Excel...";

            if (!File.Exists(Application.StartupPath + "\\ExcelTemplates\\IntegralReportTemplate.xlsx"))
            {
                currentDate = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    //EnableControls();
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " Ошибка. Файл-шаблон не существует" + "\r");
                    ProgrammLogEdit.ScrollToCaret();
                }));
                return fullname;
            }

            if (TVTask485.Nodes.Count == 0) { return fullname; }//если нет счётчиков в расписании - выходим
            if (month == String.Empty) { return fullname; }//если месяц не выбран - выходим
            //автоматически задаём нижнюю и верхнюю дату на случай если пользователь забудет
            int monint_low = Convert.ToInt16(month.Substring(0, month.IndexOf('.')));//номер первого месяца
            int monint_hi = monint_low + 1;//номер второго месяца
            int yearint_hi = DateTime.Now.Year;//в этой переменной храним номер года верхней границы периода
            int yearint_low = DateTime.Now.Year;//в этой переменной храним номер года нижней границы периода

            if (monint_hi == 13) //если нижний месяц был декабрь, то верхний должен быть январь и нижний год меньше
            {
                monint_hi = 1;
                yearint_low -= 1;
            }

            this.Invoke(new Action(delegate
            {
                DateTimeEditN.Value = Convert.ToDateTime("01." + monint_low.ToString() + "." + yearint_low + " 00:00:00");
                DateTimeEditK.Value = Convert.ToDateTime("01." + monint_hi.ToString() + "." + yearint_hi + " 00:00:00");
            }));

            currentDate = DateTime.Now;

            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();
                DisableControls();
            }));
            
            //сформируем коллекцию из всех счётчиков в дереве задания
            List<TreeNode> collection = new List<TreeNode>();
            foreach (TreeNode parentNode in TVTask485.Nodes)
            {
                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    LoadProfileIntoCounter((ICounter)childNode.Tag, 2, false, DateTimeEditN.Value, DateTimeEditK.Value);//вытаскиваем профиль из базы с одновременным суммированием периодов (по 2 для 30 минут чтобы получился час)       
                    collection.Add(childNode);//добавляем счётчик в коллекцию для формирования отчёта
                }
            }

            Microsoft.Office.Interop.Excel.Workbook workbook = null;
            CommonReports cr = new CommonReports(ProgrammLogEdit);
            if (!OnlyEnergyCheck.Checked)
            {
                workbook = cr.FormIntegralReport(collection, month, yearint_low);//в этой процедуре рисуем расшифровку (часы)
                cr.OpenAfterExport();
            }

            //формирование книги с основным листом по внешнему шаблону
            cr = new CommonReports(Application.StartupPath + "\\ExcelTemplates\\IntegralReportTemplate.xlsx", ProgrammLogEdit);
            if (OnlyEnergyCheck.Checked)
            {//если чекаем рисование только основого листа
                cr.FormIntegralReport(collection, month);//в этой процедуре заполним необходимые ячейки внешнего шаблона
            }
            else
            {//если нужно нарисовать расшифровку
                cr.FormIntegralReport(collection, month, workbook);
            }

            cr.OpenAfterExport();
                     
            currentDate = DateTime.Now;
            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
                EnableControls();
            }));
            return fullname;
        }

        private void StopReadButt_Click(object sender, EventArgs e)
        {

        }

        private UInt16 ModRTU_CRC(byte[] buf, int len)
        {//вычисляем контрольную сумму
            UInt16 crc = 0xFFFF;

            for (int pos = 0; pos < len; pos++)
            {
                crc ^= (UInt16)buf[pos];          // XOR byte into least sig. byte of crc

                for (int i = 8; i != 0; i--)
                {    // Loop over each bit
                    if ((crc & 0x0001) != 0)
                    {      // If the LSB is set
                        crc >>= 1;                    // Shift right and XOR 0xA001
                        crc ^= 0xA001;
                    }
                    else                            // Else LSB is not set
                        crc >>= 1;                    // Just shift right
                }
            }
            // Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes)
            return crc;
        }

        private void DeviceParamsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void DeviceParametersGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void selectAllParams(DataGridView dgv, bool select, TreeNode selectedNode)
        {//процедура, ставящая или убирающая галочки на всех параметрах грида
            string dgvName = dgv.Name;

            switch (dgvName)
            {
                case "DeviceParametersGrid": //грид определяет класс параметров
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (selectedNode.Tag is ICounter && selectedNode.Tag.GetType() != typeof(MercuryPLC1))
                            {
                                CounterParameterToRead param = (CounterParameterToRead)row.DataBoundItem;
                                param.check = select;
                            }
                            if (selectedNode.Tag.GetType() == typeof(Mercury228))
                            {
                                Mercury228ParametersToRead param = (Mercury228ParametersToRead)row.DataBoundItem;
                                param.check = select;
                            }
                            if (selectedNode.Tag.GetType() == typeof(Mercury225PLC1))
                            {
                                Mercury225PLC1ParametersToRead param = (Mercury225PLC1ParametersToRead)row.DataBoundItem;
                                param.check = select;
                            }

                        }
                    }
                    break;

                case "DeviceEnergyGrid":
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            CounterEnergyToRead energy = (CounterEnergyToRead)row.DataBoundItem;
                            energy.check = select;
                        }
                    }
                    break;

                case "DeviceJournalGrid":
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            CounterJournalToRead journal = (CounterJournalToRead)row.DataBoundItem;
                            journal.check = select;
                        }
                    }
                    break;

                case "DeviceJournalCQCGrid":
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            CounterJournalCQCToRead journal = (CounterJournalCQCToRead)row.DataBoundItem;
                            journal.check = select;
                        }
                    }
                    break;
            }
            dgv.Refresh();
        }

        private void selectAllParams(ICounter counter, bool select)
        {//процедура, ставящая или убирающая галочки на всех параметрах счётчика
            foreach (CounterParameterToRead param in counter.ParametersToRead)
            {
                param.check = select;
            }
        }

        private void selectAllEnergy(ICounter counter, bool select)
        {//процедура, ставящая или убирающая галочки на всех видах энергии  счётчика
            foreach (CounterEnergyToRead energy in counter.EnergyToRead)
            {
                energy.check = select;
            }
        }

        private void selectAllJournal(ICounter counter, bool select)
        {
            //процедура, ставящая или убирающая галочки на всех параметрах журнала
            foreach (CounterJournalToRead journal in counter.JournalToRead)
            {
                journal.check = select;
            }
        }

        private void selectAllCQCJournal(ICounter counter, bool select)
        {
            //процедура, ставящая или убирающая галочки на всех параметрах журнала
            foreach (CounterJournalCQCToRead journal in counter.JournalCQCToRead)
            {
                journal.check = select;
            }
        }

        private void SelectAllTVParamsButt_Click(object sender, EventArgs e)
        {//процедура, ставящая галочки на всех параметрах всех счётчиков  дерева задания TVTask485       
            foreach (TreeNode node in TVTask485.Nodes)
            {
                foreach (TreeNode nodeChild in node.Nodes)
                {
                    ICounter counter = (ICounter)nodeChild.Tag;
                    selectAllParams(counter, true);
                    selectAllEnergy(counter, true);
                    selectAllJournal(counter, true);
                    selectAllCQCJournal(counter, true);
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
            DeviceJournalCQCGrid.Refresh();
        }

        private void DeselectAllTVParamsButt_Click(object sender, EventArgs e)
        {//процедура, убирающая галочки на всех параметрах всех счётчиков дерева задания TVTask485   
            foreach (TreeNode node in TVTask485.Nodes)
            {
                foreach (TreeNode nodeChild in node.Nodes)
                {
                    ICounter counter = (ICounter)nodeChild.Tag;
                    selectAllParams(counter, false);
                    selectAllEnergy(counter, false);
                    selectAllJournal(counter, false);
                    selectAllCQCJournal(counter, false);
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
            DeviceJournalCQCGrid.Refresh();
        }

        private void cloneParameters()
        {//процедура, распространяющая отмеченные параметры выбранного прибора на все аналогичные приборы в дереве
            if (TVTask485.Nodes.Count == 0)
            {
                return;
            }

            if (globalSelectedNode.Tag is ICounter)
            {   //счётчик-источник    
                ICounter sourceCounter = (ICounter)globalSelectedNode.Tag;
                foreach (TreeNode node in TVTask485.Nodes) //цикл по верхним узлам (подключениям)
                {   //цикл по принимающим счётчикам
                    foreach (TreeNode nodeChild in node.Nodes)
                    {   //принимающий счётчик 
                        ICounter destCounter = (ICounter)nodeChild.Tag;
                        //цикл по параметрам счётчика-источника
                        foreach (CounterParameterToRead sourceParam in sourceCounter.ParametersToRead)
                        {   //цикл по параметрам принимающего счётчика
                            foreach (CounterParameterToRead destParam in destCounter.ParametersToRead)
                            {//если параметр счётчика-источника равен параметру принимающего счётчика, и первый отмечен, то отмечаем и второй 
                                if (sourceParam.name == destParam.name)
                                {
                                    destParam.check = sourceParam.check;
                                }
                            }
                        }
                    }
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
        }


        private void cloneEnergy()
        {//процедура, распространяющая отмеченные параметры выбранного прибора на все аналогичные приборы в дереве
            if (TVTask485.Nodes.Count == 0)
            {
                return;
            }

            if (globalSelectedNode.Tag is ICounter)
            {   //счётчик-источник    
                ICounter sourceCounter = (ICounter)globalSelectedNode.Tag;
                foreach (TreeNode node in TVTask485.Nodes) //цикл по верхним узлам (подключениям)
                {   //цикл по принимающим счётчикам
                    foreach (TreeNode nodeChild in node.Nodes)
                    {   //принимающий счётчик 
                        ICounter destCounter = (ICounter)nodeChild.Tag;
                        //цикл по параметрам счётчика-источника
                        foreach (CounterEnergyToRead sourceEnergy in sourceCounter.EnergyToRead)
                        {   //цикл по параметрам принимающего счётчика
                            foreach (CounterEnergyToRead destEnergy in destCounter.EnergyToRead)
                            {//если параметр счётчика-источника равен параметру принимающего счётчика, и первый отмечен, то отмечаем и второй 
                                if (sourceEnergy.name == destEnergy.name)
                                {
                                    destEnergy.check = sourceEnergy.check;
                                }
                            }
                        }
                    }
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
        }

        private void cloneJournal()
        {//процедура, распространяющая отмеченные параметры журнала выбранного прибора на все аналогичные приборы в дереве
            if (TVTask485.Nodes.Count == 0) return;

            if (globalSelectedNode.Tag is ICounter)
            {   //счётчик-источник    
                ICounter sourceCounter = (ICounter)globalSelectedNode.Tag;
                foreach (TreeNode node in TVTask485.Nodes) //цикл по верхним узлам (подключениям)
                {   //цикл по принимающим счётчикам
                    foreach (TreeNode nodeChild in node.Nodes)
                    {   //принимающий счётчик 
                        ICounter destCounter = (ICounter)nodeChild.Tag;
                        //цикл по параметрам счётчика-источника
                        foreach (CounterJournalToRead sourceJournal in sourceCounter.JournalToRead)
                        {   //цикл по параметрам принимающего счётчика
                            foreach (CounterJournalToRead destJournal in destCounter.JournalToRead)
                            {//если параметр счётчика-источника равен параметру принимающего счётчика, и первый отмечен, то отмечаем и второй 
                                if (sourceJournal.name == destJournal.name)
                                {
                                    destJournal.check = sourceJournal.check;
                                }
                            }
                        }
                    }
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
        }

        private void cloneCQCJournal()
        {//процедура, распространяющая отмеченные параметры журнала ПКЭ выбранного прибора на все аналогичные приборы в дереве
            if (TVTask485.Nodes.Count == 0) return;

            if (globalSelectedNode.Tag is ICounter)
            {   //счётчик-источник    
                ICounter sourceCounter = (ICounter)globalSelectedNode.Tag;
                foreach (TreeNode node in TVTask485.Nodes) //цикл по верхним узлам (подключениям)
                {   //цикл по принимающим счётчикам
                    foreach (TreeNode nodeChild in node.Nodes)
                    {   //принимающий счётчик 
                        ICounter destCounter = (ICounter)nodeChild.Tag;
                        //цикл по параметрам счётчика-источника
                        foreach (CounterJournalCQCToRead sourceJournal in sourceCounter.JournalCQCToRead)
                        {   //цикл по параметрам принимающего счётчика
                            foreach (CounterJournalCQCToRead destJournal in destCounter.JournalCQCToRead)
                            {//если параметр счётчика-источника равен параметру принимающего счётчика, и первый отмечен, то отмечаем и второй 
                                if (sourceJournal.name == destJournal.name)
                                {
                                    destJournal.check = sourceJournal.check;
                                }
                            }
                        }
                    }
                }
            }
            DeviceEnergyGrid.Refresh();
            DeviceParametersGrid.Refresh();
            DeviceJournalGrid.Refresh();
            DeviceJournalCQCGrid.Refresh();
        }

        private void SelectAllParamsButt_Click(object sender, EventArgs e)
        {//процедура ставящая все галки в гриде параметров для конкретного счётчика
            selectAllParams(DeviceParametersGrid, true, globalSelectedNode);
        }

        private void DeselectAllParamsButt_Click(object sender, EventArgs e)
        {//процедура убирающая все галки в гриде параметров для конкретного счётчика
            selectAllParams(DeviceParametersGrid, false, globalSelectedNode);
        }

        private void ExportDataGridButt_Click(object sender, EventArgs e)
        {
            ExportDataGrid(globalSelectedNode, DeviceParametersGrid);
        }

        private void CloneParamsButt_Click(object sender, EventArgs e)
        {//клонируем все параметры в дереве заданий
            cloneParameters();
            cloneEnergy();
            cloneJournal();
            cloneCQCJournal();
        }

        private void DeviceJournalGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void SelectAllJournalButt_Click(object sender, EventArgs e)
        {  //процедура ставящая все галки в гриде журнала для конкретного счётчика
            selectAllParams(DeviceJournalGrid, true, globalSelectedNode);
        }

        private void DeselectAllJournalButt_Click(object sender, EventArgs e)
        {  //процедура убирающая все галки в гриде журнала для конкретного счётчика
            selectAllParams(DeviceJournalGrid, false, globalSelectedNode);
        }

        private void SelectAllEnergyButt_Click(object sender, EventArgs e)
        {  //процедура ставящая все галки в гриде энергии для конкретного счётчика
            selectAllParams(DeviceEnergyGrid, true, globalSelectedNode);
        }

        private void DeselectAllEnergyButt_Click(object sender, EventArgs e)
        {  //процедура убирающая все галки в гриде энергии для конкретного счётчика
            selectAllParams(DeviceEnergyGrid, false, globalSelectedNode);
        }

        private void ExportEnergyGridButt_Click(object sender, EventArgs e)
        {  //выгружаем грид энергии выбранного узла в Excel
            ExportDataGrid(globalSelectedNode, DeviceEnergyGrid);
        }

        private void ExportJournalGridButt_Click(object sender, EventArgs e)
        {  //выгружаем грид журнала выбранного узла в Excel
            ExportDataGrid(globalSelectedNode, DeviceJournalGrid);
        }

        private BackgroundWorkerDoWorkResult GetPowerProfile(SerialPort sp, BackgroundWorker worker, RichTextBox rtb, ToolStripProgressBar pb, ToolStripLabel crl, ToolStripLabel lrl, ReadingLogForm rlf, string taskname)
        {//процедура чтения профиля мощности, котрая запускается вручную с ручным дозвоном
            DataProcessing dp = new DataProcessing(sp, notifyicon);
            BackgroundWorkerDoWorkResult bwdwr = new BackgroundWorkerDoWorkResult(dp, rlf, taskname, false);//закрывать порт автоматически нельзя. Имя задания - пустое. Форма есть
            for (int k = 0; k < IterationQty.Value; k++)//выполняем некоторое количество итераций
            {               
                    if (!(globalSelectedNode.Tag is ICounter))
                    {
                        return null;
                    }

                    if (DateTimeEditN.Value >= DateTimeEditK.Value)
                    {
                        MessageBox.Show("Начальная дата не может быть больше или равна конечной!", "Ошибка выбора дат", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }

                    var parentDevice = (IConnection)globalSelectedNode.Parent.Tag;
                    ICounter counter = (ICounter)globalSelectedNode.Tag;

                    if (ReReadAbsentRecords.Checked)//если отмечена эта галочка, значит мы в ручном опросе хотим доснимать только неснятые получасовки для выбранного счётчика
                        LoadProfileIntoCounter(counter, 0, true, DateTimeEditN.Value, DateTimeEditK.Value);//поэтому тащим профиль из базы в экземпляр счётчика, для анализа во время опроса

                    DateTime currentDateA = DateTime.Now;                   

                    if (sp == null)
                    {
                        DateTime currentDateQ = DateTime.Now;
                        Invoke(new Action(delegate
                        {
                            rtb.AppendText(currentDateQ + "." + currentDateQ.Millisecond + " Ошибка инициализации модема. Прекращаем опрос\r");
                            rtb.ScrollToCaret();
                            //EnableControls();
                        }));
                        
                        return null;
                    }
                    string workingPort = sp.PortName; //инициализируем модем и возвращаем рабочий порт

                    Invoke(new Action(delegate
                    {
                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        rtb.AppendText(currentDateA + "." + currentDateA.Millisecond + " Получаем профиль мощности для " + counter.Name + "\r");
                        rtb.ScrollToCaret();

                        GetProfileButt.Enabled = false;
                        WriteParametersButt.Enabled = false;
                        GetMonitorButt.Enabled = false;
                    }));
                    //получаем набор данных. Подаём: порт, счётчик, обработчик инфы порт, прогресс бар, начальную дату\время, конечную дату\время, кол-во периодов для суммирования после опроса                 
                    parentDevice.GetPowerProfileForCounter(workingPort, counter, dp, pb, DateTimeEditN.Value, DateTimeEditK.Value, Convert.ToInt16(PeriodNumEdit.Value), crl, lrl, rtb, worker, this.ReReadAbsentRecords.Checked);
                    //после снятия профиля загружаем в экземпляр класса профиль из базы
                    LoadProfileIntoCounter(counter, 0, true, DateTimeEditN.Value, DateTimeEditK.Value);      
            }

            Invoke(new Action(delegate
            {
                GetProfileButt.Enabled = true;
                WriteParametersButt.Enabled = true;
                GetMonitorButt.Enabled = true;
            }));
            return bwdwr;
        }

        private void LoadProfileIntoCounter(ICounter counter, int countSum, bool visualize, DateTime daten, DateTime datek)
        {//процедура загрузки профиля из базы в экземпляр счётчика
            if (visualize == true)
            {
                this.Invoke(new Action(delegate ()
                {
                    PowerProfileGrid.DataSource = null;
                    PowerProfileChart.Series.Clear();
                }));
            }
            //тащим профиль из базы
            counter.ProfileDataTable = DataBaseManagerMSSQL.Return_Profile(counter.ID, daten, datek);
            //если записей в профиле нет, то мы должны это показать и выйти
            if (counter.ProfileDataTable.Rows.Count == 0)
            {
                TimeSpan ts = datek.Subtract(daten); //разница между датами (начальной и конечной)
                int MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до конечной даты
                int EstimatedPeriodsCount = MinutesElapsed / 30; //расчётное количество периодов интегрирования от начальной даты до конечной даты   
                if (visualize == true)
                {
                    this.Invoke(new Action(delegate ()
                    {
                        PeriodsPresenceLabel.Text = counter.ProfileDataTable.Rows.Count.ToString() + " из " + EstimatedPeriodsCount.ToString();//выводим количество имеющихся периодов интегрирования и расчётное
                        PeriodsPresenceLabel.ForeColor = Color.Red;
                    }));
                }
                return;
            }
            int period = Convert.ToInt16(counter.ProfileDataTable.Rows[0]["period"]);//период интегрирования
            //далее смотрим профиль за указанный период на отсутствие записей
            if (IntegrityCheckBox.Checked)
            {
                    TimeSpan ts = datek.Subtract(daten); //разница между датами (начальной и конечной)
                    int MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до конечной даты
                    int EstimatedPeriodsCount = MinutesElapsed / period; //расчётное количество периодов интегрирования от начальной даты до конечной даты  
                    if (visualize == true)
                    {
                        this.Invoke(new Action(delegate ()
                        {
                            PeriodsPresenceLabel.Text = counter.ProfileDataTable.Rows.Count.ToString() + " из " + EstimatedPeriodsCount.ToString();//выводим количество имеющихся периодов интегрирования и расчётное
                        }));
                    }
                    //смотрим совпадают ли расчётное количество периодов и фактическое количество периодов и соответствующе подкрашиваем метку
                    if (counter.ProfileDataTable.Rows.Count == EstimatedPeriodsCount)
                    {
                        if (visualize == true)
                        {
                            this.Invoke(new Action(delegate ()  { PeriodsPresenceLabel.ForeColor = Color.Green; }));
                        }
                    }
                    //если цифры не совпадают, то нам нужно пройтись по периодам чтобы отобразить недостающие 
                    else
                    {
                        if (visualize == true)
                        {
                            this.Invoke(new Action(delegate () { PeriodsPresenceLabel.ForeColor = Color.Red; }));
                        }
                       
                        //сначала нужно добавить пустые периоды в начало таблицы (по факту одну запись чтобы от неё дальше добавлять при необходимости другие)
                        if (Convert.ToDateTime(counter.ProfileDataTable.Rows[0]["date_time"]) > daten.AddMinutes(period))
                        {
                            DateTime IncreasingDate = daten.AddMinutes(period);//дата нужная для того чтобы заполнить пустоты в начале
                            DataRow dr = counter.ProfileDataTable.NewRow(); //добавляем виртуальную строку в таблицу

                            counter.ProfileDataTable.Columns[3].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[4].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[5].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[6].AllowDBNull = true;//разрешаем столбцам нулевые значения

                            dr["column1"] = 0; dr["column2"] = 0; dr["column3"] = 0; dr["period"] = period;
                            dr["e_a_plus"] = DBNull.Value; dr["e_a_minus"] = DBNull.Value;
                            dr["e_r_plus"] = DBNull.Value; dr["e_r_minus"] = DBNull.Value;
                            dr["date_time"] = IncreasingDate;

                            counter.ProfileDataTable.Rows.InsertAt(dr, 0);
                        }

                        //циклимся по таблице профиля в счётчике с целью выявления недостающих записей профиля
                        for (int i = 0; i < EstimatedPeriodsCount - 1; i++)
                        {
                            //к очередной записи добавляем период интегрирования чтобы сравнить со следующей записью в таблице
                            DateTime dt = Convert.ToDateTime(counter.ProfileDataTable.Rows[i]["date_time"]).AddMinutes(period);
                            DataRow dr = counter.ProfileDataTable.NewRow(); //добавляем виртуальную строку в таблицу

                            counter.ProfileDataTable.Columns[3].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[4].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[5].AllowDBNull = true;//разрешаем столбцам нулевые значения
                            counter.ProfileDataTable.Columns[6].AllowDBNull = true;//разрешаем столбцам нулевые значения

                            dr["column1"] = 0; dr["column2"] = 0; dr["column3"] = 0; dr["period"] = period;

                            dr["e_a_plus"] = DBNull.Value; dr["e_a_minus"] = DBNull.Value;
                            dr["e_r_plus"] = DBNull.Value; dr["e_r_minus"] = DBNull.Value;
                            //если время не подходит, то добавляем виртуальную строку в таблицу
                            try
                            {
                                if (dt != Convert.ToDateTime(counter.ProfileDataTable.Rows[i + 1]["date_time"]))//если нарощенная текущая дата не равна следующей дате в профиле
                                {//то добавляем её в качестве "виртуальной" т.е. заполняем пробел                                
                                    dr["date_time"] = dt;
                                    counter.ProfileDataTable.Rows.InsertAt(dr, i + 1);
                                }
                            }
                            catch //если наткнулись на исключение - это значит что вышли за рамки таблицы профиля счётчика и нужно дорисовать строки за их пределами
                            {
                                dr["date_time"] = dt;
                                counter.ProfileDataTable.Rows.Add(dr);
                            }
                        }
                    }
            }


            if (countSum > 1)
            {//если есть суммирование, то формируем новый набор данных
                DataTable dtNew = DataProcessing.SumUpProfile(counter.ProfileDataTable, countSum);
                counter.ProfileDataTable = dtNew;
            }

            if (visualize)
            {//выводим результат по желанию
                this.Invoke(new Action(delegate ()
                {
                    PowerProfileGrid.DataSource = counter.ProfileDataTable; //привязываем набор данных к гриду и графику
                    //======================================ГРИД ПРОФИЛЯ=============================================================
                    PowerProfileGrid.Columns[0].Visible = false; //скрываем столбцы-пустышки
                    PowerProfileGrid.Columns[1].Visible = false; //скрываем столбцы-пустышки
                    PowerProfileGrid.Columns[2].Visible = false; //скрываем столбцы-пустышки
                    PowerProfileGrid.Columns[8].Visible = false; //скрываем столбец с периодом интегрирования
                                                                 //задаём заголовки
                    PowerProfileGrid.Columns[3].HeaderText = "Значение A+"; PowerProfileGrid.Columns[4].HeaderText = "Значение A-";
                    PowerProfileGrid.Columns[5].HeaderText = "Значение R+"; PowerProfileGrid.Columns[6].HeaderText = "Значение R-";
                    PowerProfileGrid.Columns[7].HeaderText = "Дата\\время";
                    //настраиваем стиль
                    PowerProfileGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
                    PowerProfileGrid.EnableHeadersVisualStyles = false;

                    counter.ProfileDataTable.Columns[0].AllowDBNull = true;//столбец-пустышка dummy1
                    counter.ProfileDataTable.Columns[1].AllowDBNull = true;//столбец-пустышка dummy2
                    counter.ProfileDataTable.Columns[2].AllowDBNull = true;//столбец-пустышка dummy3

                    //======================================ГРАФИК ПРОФИЛЯ=============================================================
                    System.Windows.Forms.DataVisualization.Charting.SeriesChartType charttype;
                    charttype = charttype = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

                    if (ChartViewCombo.SelectedIndex == 0) charttype = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    if (ChartViewCombo.SelectedIndex == 1) charttype = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Area;

                    PowerProfileChart.Series.Clear(); //очищаем все существующие серии
                                                      //создаём серии
                    PowerProfileChart.Series.Add("A+"); //активная прямая

                    PowerProfileChart.Series["A+"].ChartType = charttype;
                    PowerProfileChart.Series["A+"].XValueMember = "date_time"; PowerProfileChart.Series["A+"].YValueMembers = "e_a_plus";
                    PowerProfileChart.Series["A+"].BorderWidth = 2;
                    PowerProfileChart.Series["A+"].EmptyPointStyle.Color = Color.Yellow;
                    PowerProfileChart.Series["A+"].EmptyPointStyle.BorderWidth = 2;
                    PowerProfileChart.Series["A+"].EmptyPointStyle.BorderColor = Color.Yellow;

                    PowerProfileChart.Series.Add("A-"); //активная обратная
                    PowerProfileChart.Series["A-"].ChartType = charttype;
                    PowerProfileChart.Series["A-"].XValueMember = "date_time"; PowerProfileChart.Series["A-"].YValueMembers = "e_a_minus";

                    PowerProfileChart.Series.Add("R+"); //реактивная прямая
                    PowerProfileChart.Series["R+"].ChartType = charttype;
                    PowerProfileChart.Series["R+"].XValueMember = "date_time"; PowerProfileChart.Series["R+"].YValueMembers = "e_r_plus";

                    PowerProfileChart.Series.Add("R-"); //реактивная обратная
                    PowerProfileChart.Series["R-"].ChartType = charttype;
                    PowerProfileChart.Series["R-"].XValueMember = "date_time"; PowerProfileChart.Series["R-"].YValueMembers = "e_r_minus";

                    PowerProfileChart.DataSource = counter.ProfileDataTable; PowerProfileChart.DataBind();

                    PowerProfileChart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                    PowerProfileChart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;

                    PowerProfileChart.MouseWheel += PowerProfileChart_MouseWheel;
                }));

                //теперь нужно подкрасить недостающие записи грида
                //циклимся по ячейкам грида
                for (int i = 0; i < PowerProfileGrid.Rows.Count; i++)
                {
                    if (Convert.IsDBNull(PowerProfileGrid[3, i].Value))
                    {
                        this.Invoke(new Action(delegate ()
                        {
                            DataGridViewCellStyle dgvcs = new DataGridViewCellStyle();//стиль ячейки
                            dgvcs.BackColor = Color.Yellow;
                            PowerProfileGrid[3, i].Style = dgvcs;
                            PowerProfileGrid[4, i].Style = dgvcs;
                            PowerProfileGrid[5, i].Style = dgvcs;
                            PowerProfileGrid[6, i].Style = dgvcs;
                            PowerProfileGrid[7, i].Style = dgvcs;
                        }));
                    }
                }
            }
        }

        private void PowerProfileChart_MouseWheel(object sender, MouseEventArgs e)
        {

        }

        private void GetCounterMonitor_doWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = GetCounterMonitor(global_sp, worker);
            //GetCounterMonitor(global_sp, worker);
        }

        private BackgroundWorkerDoWorkResult GetCounterMonitor(SerialPort sp, BackgroundWorker worker)
        {
            //BackgroundWorker worker = sender as BackgroundWorker;
            //процедура чтения монитора со счётчика (только вручнуюп ппосле ручного дозвона)
            if (!(globalSelectedNode.Tag is ICounter)) return null;

            DateTime currentDateA = DateTime.Now;
            DataProcessing dp = new DataProcessing(sp, notifyicon);
            BackgroundWorkerDoWorkResult bwdwr = new BackgroundWorkerDoWorkResult(dp, null, String.Empty, false);//формы нет, имя задания пустое, закрывать порт автоматически нельзя
            if (sp == null)
            {
                DateTime currentDateQ = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDateQ + "." + currentDateQ.Millisecond + " Ошибка инициализации модема. Прекращаем опрос\r");
                    ProgrammLogEdit.ScrollToCaret();               
                    //EnableControls();
                }));
                return null;
            }
            string workingPort = sp.PortName; //инициализируем модем и возвращаем рабочий порт

            ICounter counter = (ICounter)globalSelectedNode.Tag;
            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDateA + "." + currentDateA.Millisecond + " Получаем монитор для..." + counter.Name + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));

            var parentDevice = (IConnection)globalSelectedNode.Parent.Tag;
            this.Invoke(new Action(delegate
            {
                DisableControls(); //отключаем визуальные компоненты чтобы нельзя было внести изменения в структуру или свойства объектов во время опроса
                WriteParametersButt.Enabled = false;
                GetProfileButt.Enabled = false;
            }));
            //------------------непосредственно процедура чтения монитора------------
            parentDevice.GetMonitorForCounter(workingPort, counter, dp, ProgressBar, ByPhaseVectorDiagramPictureBox, ProgrammLogEdit, ref worker);
            //-----------------------------------------------------------------------
            this.Invoke(new Action(delegate
            {
                EnableControls();
                WriteParametersButt.Enabled = true;
                GetProfileButt.Enabled = true;
            }));
            return bwdwr;
        }

        private void ExportProdileGridButt_Click(object sender, EventArgs e)
        {
            ExportDataGrid(globalSelectedNode, PowerProfileGrid);
        }

        private void GetMonitorButt_Click(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(GetCounterMonitor_doWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void ExportAllGridsButt_Click(object sender, EventArgs e)
        {//кнопка выгрузки всех гридов всего дерева
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт всех параметров в Excel...";
            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();
                DisableControls();
            }));
            
            if (TVTask485.Nodes.Count > 0)
            {
                ExportDataGrid(TVTask485, DeviceEnergyGrid, "Энергия ", String.Empty); ExportDataGrid(TVTask485, DeviceParametersGrid, "Параметры ", String.Empty);
                ExportDataGrid(TVTask485, DeviceJournalGrid, "Журнал ", String.Empty); ExportDataGrid(TVTask485, DeviceMonitorGrid, "Монитор ", String.Empty);
            }
           
            currentDate = DateTime.Now;
            this.Invoke(new Action(delegate
            {
                EnableControls();
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }

        private void ExportSelectedParamsButt_Click(object sender, EventArgs e)
        {
            //экспорт выбранных параметров     
            this.Cursor = Cursors.WaitCursor;
            List<TreeNode> tree = new List<TreeNode>(TVTask485.Nodes.Cast<TreeNode>());
            ExportSelectedParamsForSetOfCounters(tree, String.Empty, String.Empty);
            this.Cursor = Cursors.Default;
        }

        private void TaskFormButt_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            LoadTaskDelegate del = new LoadTaskDelegate(LoadTask);//в форму загрузки заданий передаём делегат загрузки заданий (метод реализован в данной форме)
            TasksForm tf = new TasksForm(null, ProgrammLogEdit, del, null, (int)PeriodNumEdit.Value);//подгружаем в форму таблицу с заданиями.
            //Второй параметр null т.к. неизвестно какое будет дерево. С подачей дерева эта форма создаётся из контекстных меню деревьев 
            tf.ShowDialog();
            Cursor.Current = Cursors.Default;
        }

        private void SettingsFormButt_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Раздел в разработке", "Ничего не выйдет", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HelpFormButt_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Раздел в разработке", "Ничего не выйдет", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SearchNodes(string text, bool clear_global_list)
        {   //процедура поиска узлов в главном дереве по заданному текстовому критерию            
            if (text == String.Empty) { return; }//если поле поиска пусто, то выходим (неизвестно, что искать)

            if (clear_global_list)
            {
                selectedNodesListClear(selectedNodesListGlobal); //очищаем текущий список выбранных узлов перед поиском
            }

            foreach (TreeNode connectionNode in FullTree.Nodes)
            {   //цикл по верхним узлам (шлюзам и модемам)
                var connectionDevice = (IDevice)connectionNode.Tag;
                if (connectionDevice.Search(text, StringComparison.CurrentCultureIgnoreCase, connectionNode.Text))
                {//если одно из полей узла удовлетворяет критерию поиска, то добавляем узел в коллекцию
                    selectedNodesListAppend(connectionNode, selectedNodesListGlobal);
                    connectionNode.EnsureVisible();
                }
                //цикл по дочерним узлам подключения (шлюза или модема)
                foreach (TreeNode childNode in connectionNode.Nodes)
                {//проверяем тип дочернего узла
                    if (childNode.Tag is ICounter)
                    {//делаем проверку по определённым полям
                        var counterDevice = (IDevice)childNode.Tag;
                        if (counterDevice.Search(text, StringComparison.CurrentCultureIgnoreCase, childNode.Text))                         
                        {//если одно из полей узла удовлетворяет критерию поиска, то добавляем узел в коллекцию
                            selectedNodesListAppend(childNode, selectedNodesListGlobal);
                            connectionNode.Expand(); //раскрываем родительский узел
                            childNode.EnsureVisible();
                        }
                    }
                    if (childNode.Tag is IConcentrator)
                    {//если концентратор, то открываем цикл по дочерним PLC-счётчикам
                        foreach (TreeNode childNodePLC in childNode.Nodes)
                        {//делаем проверку по определённым полям
                            var counterDevice = (IDevice)childNodePLC.Tag;
                            if (counterDevice.Search(text, StringComparison.CurrentCultureIgnoreCase, childNodePLC.Text))
                            {//если одно из полей узла удовлетворяет критерию поиска, то добавляем узел в коллекцию
                                selectedNodesListAppend(childNodePLC, selectedNodesListGlobal);
                                childNode.Expand(); //раскрываем родительский узел концентратор
                                connectionNode.Expand(); //раскрываем родительский узел подключение
                                childNodePLC.EnsureVisible();
                            }
                        }
                    }
                }
            }
        }

        private bool SearchNodes(int id, TreeNode pnode)
        {   //процедура РЕКУРСИВНОГО поиска узлов в главном дереве по ИДЕНТИФИКАТОРУ
            bool node_has_been_found = false;//по-умолчанию узел не найден
            foreach (TreeNode node in pnode.Nodes)
            {
                var device = (IDevice)node.Tag;
                if (device.ID == id)
                {
                    selectedNodesListAppend(node, selectedNodesListGlobal);
                    node_has_been_found = true;//нашли узел по идентификатору
                }
                else
                {
                    SearchNodes(id, node);//не нашли - углубляемся в иерархию
                }
            }
            return node_has_been_found;
        }

        private void SearchNodes(int id)
        {   //процедура поиска подключений в главном дереве по ИДЕНТИФИКАТОРУ
            foreach (TreeNode node in FullTree.Nodes)
            {
                var device = (IDevice)node.Tag;
                if (device.ID == id) selectedNodesListAppend(node, selectedNodesListGlobal);
                else SearchNodes(id, node);
            }
            GoToTaskButt.PerformClick();
        }

        private bool SearchNodes(int id, TreeNode pnode, List<TreeNode> list)
        {   //процедура РЕКУРСИВНОГО поиска узлов в главном дереве по ИДЕНТИФИКАТОРУ с ИСПОЛЬЗОВАНИЕМ СПИСКОВ
            bool node_has_been_found = false;//по-умолчанию узел не найден
            foreach (TreeNode node in pnode.Nodes)
            {
                var device = (IDevice)node.Tag;
                if (device.ID == id)
                {
                    selectedNodesListAppend(node, list);
                    node_has_been_found = true;//нашли узел по идентификатору
                }
                else
                {
                    SearchNodes(id, node, list);//не нашли - углубляемся в иерархию
                }
            }
            return node_has_been_found;
        }

        private void SearchNodes(int id, List<TreeNode> list)
        {   //процедура поиска подключений в главном дереве по ИДЕНТИФИКАТОРУ с ИСПОЛЬЗОВАНИЕМ СПИСКОВ
            foreach (TreeNode node in FullTree.Nodes)
            {
                var device = (IDevice)node.Tag;
                if (device.ID == id) selectedNodesListAppend(node, list);
                else SearchNodes(id, node, list);
            }
            GoToTaskButt.PerformClick();
        }

        public void LoadTask(int task_id, TreeView tv, out bool read_profile)//версия процедуры для загрузки задания по расписанию (использует маленький GoToTask и не использует глобальную коллекцию выбранных узлов)
        {//ЗАГРУЗКА ЗАДАНИЯ РЕАЛИЗОВАНА ЗДЕСЬ Т.К. МЕТОДЫ SearchNodes И GoToTask ТОЖЕ НА ЭТОЙ ФОРМЕ, И ОНИ УДОБНЫ ДЛЯ ЭТОГО ДЕЙСТВИЯ             
            try
            {
                read_profile = false;//определяет, будет ли считываться профиль при выполнении группового задания. По-умолчанию снятие профиля отключено
                this.Invoke(new Action(delegate
                {
                    FullTree.BeginUpdate();
                }));
                DataTable dt = DataBaseManagerMSSQL.Return_Task_Grid(task_id);//грузим задание из сетки задания
                if ((dt == null) || (dt.Rows.Count == 0)) return;//если в задании пусто            

                //грузим параметры профиля, если для загружаемого задания такие имеются
                DataTable dt_profile = DataBaseManagerMSSQL.Return_Task_Profile(task_id);            

                if (dt_profile.Rows.Count > 0)
                {//если есть, то тянем их и помещаем на контролы формы
                    this.Invoke(new Action(delegate
                    {
                        DateTimeEditN.Value = Convert.ToDateTime(dt_profile.Rows[0]["lower_datetime"]);
                        DateTimeEditK.Value = Convert.ToDateTime(dt_profile.Rows[0]["upper_datetime"]);                        
                    }));
                    read_profile = true;//будем снимать профиль
                }
                //грузим автоматизацию для снятия профиля, если она есть
                DataTable dt_profile_automation = DataBaseManagerMSSQL.Return_Task_Profile_Automation(task_id);
                if (dt_profile_automation.Rows.Count > 0)
                {//если есть, то тянем её помещаем на контролы формы
                    this.Invoke(new Action(delegate
                    {
                        PeriodTemplatesComboBox.SelectedIndex = -1;
                        PeriodTemplatesComboBox.SelectedIndex = Convert.ToInt16(dt_profile_automation.Rows[0]["automation_id"]);                       
                    }));
                    read_profile = true;//будем снимать профиль
                }
                List<TreeNode> list = new List<TreeNode>();//частный список узлов для виртуального дерева при опросе по расписанию (без использования глобального списка)
                foreach (DataRow row in dt.Rows)
                {//цикл по таблице сетки задания                          
                    foreach (TreeNode node in FullTree.Nodes)//циклимся по верхним узлам главного дерева
                    {//цикл по узлам. Если метод SearchNodes находит текущий узел в главном дереве, он добавляет его в коллекцию выделенных узлов
                        if (SearchNodes(Convert.ToInt16(row["id_node"].ToString()), node, list) == true) break;
                    }
                }

                //переносим выделенные узлы в дерево задания (здесь это дерево "виртуальное" для опроса по расписанию в обход интерфейсных деревьев)
                foreach (TreeNode CurNode in list)
                {
                    GoToTask(tv, CurNode);//сам перенос. Маленькая версия переноса по одному
                    //заполняем параметры счётчика из сетки параметров задания
                    ICounter counter = (ICounter)CurNode.Tag;//счётчик
                    DataTable dt_params = DataBaseManagerMSSQL.Return_Task_Parameters_Grid(task_id, counter.ID);//сетка пераметров задания для счётчика
                    //циклимся по таблице, помечая параметры, сохранённые в базе
                    foreach (DataRow row in dt_params.Rows)
                    {
                        var checkedEnergy = from energy in counter.EnergyToRead where energy.name == row["param_name"].ToString() select energy;
                        foreach (var energy in checkedEnergy) energy.check = true;

                        var checkedJournal = from journal in counter.JournalToRead where journal.name == row["param_name"].ToString() select journal;
                        foreach (var journal in checkedJournal) journal.check = true;

                        var checkedParameter = from parameter in counter.ParametersToRead where parameter.name == row["param_name"].ToString() select parameter;
                        foreach (var parameter in checkedParameter) parameter.check = true;
                    }
                }
                this.Invoke(new Action(delegate
                {
                    selectedNodesListClear(list);
                }));
            }
            catch (Exception ex)
            {//если загрузка не удалась, попробуем ещё раз
                DateTime currentDate = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Не получилось загрузить задание: " + ex.Message + "\r");
                    ProgrammLogEdit.ScrollToCaret();
                    Cursor = Cursors.Default;
                }));
                read_profile = false;
                return;
            }
            this.Invoke(new Action(delegate
            {
                FullTree.EndUpdate();
            }));
        }

        public void LoadTask(int task_id, ProgressBar tspb)//версия процедуры для загрузки задания вручную (использует расширенный GoToTask и использует глобальную коллекцию выбранных узлов для загрузки задания)
        {//ЗАГРУЗКА ЗАДАНИЯ РЕАЛИЗОВАНА ЗДЕСЬ Т.К. МЕТОДЫ SearchNodes И GoToTask ТОЖЕ НА ЭТОЙ ФОРМЕ, И ОНИ УДОБНЫ ДЛЯ ЭТОГО ДЕЙСТВИЯ  
            try
            {
                Cursor = Cursors.WaitCursor;
                Invoke(new Action(delegate
                {
                    PeriodTemplatesComboBox.SelectedIndex = -1;
                }));
                DataTable dt = DataBaseManagerMSSQL.Return_Task_Grid(task_id);//грузим задание из сетки задания
                if ((dt == null) || (dt.Rows.Count == 0)) return;//если в задании пусто            
                selectedNodesListClear(selectedNodesListGlobal);//очищаем список выбранных узлов перел тем как загрузить задание
                Invoke(new Action(delegate
                {
                    TVTask485.Nodes.Clear();
                    TVTaskPLC.Nodes.Clear();//очищаем деревья заданий
                }));
                //грузим параметры профиля, если для загружаемого задания такие имеются
                DataTable dt_profile = DataBaseManagerMSSQL.Return_Task_Profile(task_id);
                Invoke(new Action(delegate
                {
                    GetPowerProfileCheck.Checked = false;//по-умолчанию снятие профиля отключено
                }));
                if (dt_profile.Rows.Count > 0)
                {//если есть, то тянем их и помещаем на контролы формы
                    Invoke(new Action(delegate
                    {
                        DateTimeEditN.Value = Convert.ToDateTime(dt_profile.Rows[0]["lower_datetime"]);
                        DateTimeEditK.Value = Convert.ToDateTime(dt_profile.Rows[0]["upper_datetime"]);
                        GetPowerProfileCheck.Checked = true;//снятие профиля при групповом опросе
                    }));
                }
                //грузим автоматизацию для снятия профиля, если она есть
                DataTable dt_profile_automation = DataBaseManagerMSSQL.Return_Task_Profile_Automation(task_id);
                if (dt_profile_automation.Rows.Count > 0)
                {//если есть, то тянем её помещаем на контролы формы
                    Invoke(new Action(delegate
                    {
                        PeriodTemplatesComboBox.SelectedIndex = -1;
                        PeriodTemplatesComboBox.SelectedIndex = Convert.ToInt16(dt_profile_automation.Rows[0]["automation_id"]);
                        GetPowerProfileCheck.Checked = true;//снятие профиля при групповом опросе
                    }));
                }

                foreach (DataRow row in dt.Rows)
                {//цикл по таблице сетки задания                          
                    foreach (TreeNode node in FullTree.Nodes)//циклимся по верхним узлам главного дерева
                    {//цикл по узлам. Если метод SearchNodes находит текущий узел в главном дереве, он добавляет его в коллекцию выделенных узлов
                        if (SearchNodes(Convert.ToInt16(row["id_node"].ToString()), node) == true) break;
                    }
                }

                if (tspb != null)
                {
                    tspb.Value = 0;
                    tspb.Maximum = selectedNodesListGlobal.Count;
                }
                //переносим выделенные узлы в деревья заданий
                foreach (TreeNode CurNode in selectedNodesListGlobal)
                {
                    GoToTask(CurNode);//сам перенос
                    //заполняем параметры счётчика из сетки параметров задания
                    ICounter counter = (ICounter)CurNode.Tag;//счётчик
                    DataTable dt_params = DataBaseManagerMSSQL.Return_Task_Parameters_Grid(task_id, counter.ID);//сетка пераметров задания для счётчика
                    //циклимся по таблице, помечая параметры, сохранённые в базе
                    foreach (DataRow row in dt_params.Rows)
                    {
                        var checkedEnergy = from energy in counter.EnergyToRead where energy.name == row["param_name"].ToString() select energy;
                        foreach (var energy in checkedEnergy) energy.check = true;

                        var checkedJournal = from journal in counter.JournalToRead where journal.name == row["param_name"].ToString() select journal;
                        foreach (var journal in checkedJournal) journal.check = true;

                        var checkedParameter = from parameter in counter.ParametersToRead where parameter.name == row["param_name"].ToString() select parameter;
                        foreach (var parameter in checkedParameter) parameter.check = true;
                    }
                    if (tspb != null) { tspb.PerformStep(); }
                }
                selectedNodesListClear(selectedNodesListGlobal);//очищаем список выбранных узлов в главном дереве после того как загрузили задание
            }
            catch (Exception ex)
            {//если загрузка не удалась, попробуем ещё раз
                DateTime currentDate = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Не получилось загрузить задание: " + ex.Message + "\r");
                    ProgrammLogEdit.ScrollToCaret();
                }));
                Cursor = Cursors.Default;
                return;
            }
            Cursor = Cursors.Default;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DisableControls();
                SearchNodes(SearchTextBox.Text, true);
                EnableControls();
            }
        }

        private void ExtSourcesFormButt_Click(object sender, EventArgs e)
        {

        }

        private void ClearTVtaskPLCButt_Click(object sender, EventArgs e)
        {
            TVTaskPLC.Nodes.Clear();
        }

        private void ReadPLCButt_Click(object sender, EventArgs e)
        {
            DateTime currentDate = DateTime.Now;
            ProgrammLogEdit.Invoke(new Action(delegate
            {
                ProgrammLogEdit.SelectionColor = Color.DarkOrange;
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Опрос дерева PLC\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            List<TreeNode> tree = new List<TreeNode>(TVTaskPLC.Nodes.Cast<TreeNode>());//преобразуем дерево в список
            //создаём структуру для передачи в BackgroundWorker
            TaskStruct task = new TaskStruct(tree, -1, GetPowerProfileCheck.Checked, DateTimeEditN.Value, DateTimeEditK.Value, Convert.ToInt16(PeriodNumEdit.Value), String.Empty, false, false);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync(task);//передаём структуру задания
        }

        private BackgroundWorkerDoWorkResult WriteParameters(SerialPort sp, BackgroundWorker worker)
        {
                //процедура записи настроек в выбранное устройство по нажатию кнопки на форме (не групповая операция) с использованием значений полей на форме
                if (!(globalSelectedNode.Tag is IWritable))
                {
                    return null;
                }

                DateTime currentDateA = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDateA + "." + currentDateA.Millisecond + " Пытаемся записать настройки в устройство...\r");
                    ProgrammLogEdit.ScrollToCaret();
                }));

                //--------------------------------------------------------------
                ///этот список возможно будет убран, а возможно нет-------------
                ///список значений параметров из полей на форме ----------------------------------
                List<FieldsValuesToWrite> fieldsToWriteList = new List<FieldsValuesToWrite>();
                //--------------------------------------------------------------

                //-----это новый список значений полей из главной формы---------
                List<FieldsValuesToWrite> formFieldsValuesToWrite = new List<FieldsValuesToWrite>();
                //заполняем список значениями полей из главной формы чтобы потом подать его в метод записи параметров устройства
                formFieldsValuesToWrite.Add(new FieldsValuesToWrite("DTPicker", DTPicker.Text));
                //--------------------------------------------------------------

                this.Invoke(new Action(delegate
                {
                    if ((globalSelectedNode.Tag.GetType() == typeof(Mercury225PLC1)))
                    {                
                        //создаём список значений полей с формы и заполняем его, чтобы потом подать на разбор и запись
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Конфигурация", WriteNetSizeComboConc.Text + "/" + WriteConcModeComboConc.Text));
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Дата и время", DateTime.Now.ToString()));//текущие дата и время
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Сетевой адрес", WriteConcAddressTextConc.Text));
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Скорость порта", PortRateComboConc.Text));
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Расчётный день", WriteBDayNumConc.Text));
                    }
                }));

                this.Invoke(new Action(delegate
                {
                    if ((globalSelectedNode.Tag.GetType() == typeof(Mercury228)))
                    {
                        //создаём список значений полей с формы и заполняем его, чтобы потом подать на разбор и запись 
                        //скорость в поле UART подаётся последней для правильного составления двоичной строки в процедуре разбора т.к. читаем слева-направо
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Настройки порта 1", EvenOddGateCombo.Text + "/" + EvenOddCheckGateCombo.Text
                                                                           + "/" + StopBytesGateCombo.Text + "/" +
                                                                           DataBytesGateCombo.Text + "/" +
                                                                           PortRateGateCombo.Text + "/" + TimeoutPackageGateCombo.Text
                                                                            + "/" + PauseGateNum.Value + " симв/"));
                        fieldsToWriteList.Add(new FieldsValuesToWrite("Настройки порта 2", EvenOddCheckGateCombo.Text + "/" + EvenOddGateCombo.Text
                                                                           + "/" + StopBytesGateCombo.Text + "/" +
                                                                           DataBytesGateCombo.Text + "/" +
                                                                           PortRateGateCombo.Text + "/" + TimeoutPackageGateCombo.Text
                                                                         + "/" + PauseGateNum.Value + " симв/"));
                    }
                }));

                DataProcessing dp = new DataProcessing(sp, notifyicon);
                if (sp == null)
                {
                    DateTime currentDateQ = DateTime.Now;
                    this.Invoke(new Action(delegate
                    {
                        ProgrammLogEdit.AppendText(currentDateQ + "." + currentDateQ.Millisecond + " Ошибка инициализации модема. Прекращаем опрос\r");
                        ProgrammLogEdit.ScrollToCaret();
                        EnableControls();
                    }));
                    
                    return null;
                }
                string workingPort = sp.PortName; //инициализируем модем и возвращаем рабочий порт   

                IConnection device = null;
                //если у выбранного узла нет родителя
                if (globalSelectedNode.Parent == null)
                { device = (IConnection)globalSelectedNode.Tag; }
                else //если есть родитель
                { device = (IConnection)globalSelectedNode.Parent.Tag; }

                this.Invoke(new Action(delegate
                {
                    DisableControls(); //отключаем визуальные компоненты чтобы нельзя было внести изменения в структуру или свойства объектов во время опроса
                    GetProfileButt.Enabled = false;
                    GetMonitorButt.Enabled = false;
                //------------------непосредственно процедура записи настроек------------
                    device.WriteParametersToDevice(globalSelectedNode, dp,  ProgrammLogEdit, worker, formFieldsValuesToWrite, fieldsToWriteList);
                //-----------------------------------------------------------------------
                }));

                DateTime currentDateB = DateTime.Now;
                string ElapsedTime = currentDateB.Subtract(currentDateA).ToString();
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDateB + "." + currentDateB.Millisecond + " Запись завершёна. Время выполнения: " + ElapsedTime + "\r");
                    ProgrammLogEdit.ScrollToCaret();  
                    EnableControls();
                    GetProfileButt.Enabled = true;
                    GetMonitorButt.Enabled = true;
                    Cursor = Cursors.Default;
                }));

            BackgroundWorkerDoWorkResult bwdwr = new BackgroundWorkerDoWorkResult(dp, null, String.Empty, false);//формы лога нет, закрывать порт автоматически нельзя, имя задания пустое
            return bwdwr;
        }

        private void WriteParametersGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //нет никакой обработки. Только чтобы избежать окна исключений
        }

        private void FormIntegralReportButt_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() => 
            {
                string rmctext = String.Empty;
                this.Invoke(new Action(delegate
                {
                    rmctext = ReportMonthCombo.Text;
                }));

                ExportIntegralReport(rmctext, String.Empty);
            });
            thread.Start();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

            string a = Application.StartupPath + "\\ExcelTemplates\\IntegralReportTemplate.xlsx";
        }

        bool MouseDoubleClickOnNode;//хранит значение, был лы двойной щелчок по узлу дерева. Нужна, чтобы не давать узлу раскрыться при двойном нажатии
        private void FullTree_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {//запрещаем узлам раскрываться при двойном клике чтобы переносить их в задания без раскрывания
            if (MouseDoubleClickOnNode && (e.Action == TreeViewAction.Collapse)) { e.Cancel = true; }
        }

        private void FullTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {//запрещаем узлам закрываться при двойном клике чтобы переносить их в задания без закрывания
            if (MouseDoubleClickOnNode && (e.Action == TreeViewAction.Expand)) { e.Cancel = true; }
        }

        private void FullTree_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Clicks > 1) { MouseDoubleClickOnNode = true; } else { MouseDoubleClickOnNode = false; }
        }

        private void FullTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {//по двойному нажатию переносим узел в задание
            DisableControls();
            GoToTask(e.Node);
            EnableControls();
        }

        private void DeleteNodeButt_Click(object sender, EventArgs e)
        {
            RemoveNodesFromTask(TVTask485);
        }

        private void RemoveNodesFromTask(TreeView tv)
        {//циклимся по глобальной коллекции выделенных узлов с целью убрать их
            try
            {
                foreach (TreeNode node in selectedNodesListGlobal)
                {
                    if (node.TreeView == tv) RemoveNodeFromTask(node);
                }
            }
            catch
            {

            }
        }

        private void RemoveNodeFromTask(TreeNode node)
        {//процедура удаления узла из дерева заданий  
            TreeNode pnode = null;
            if (node == null) return;
            //запоминаем родителя если есть 
            if (node.Parent != null) { pnode = node.Parent; }
            //удаляем узел
            node.Remove();
            //если у удаляемого узла есть родитель, и у этого родителя больше не осталось детей, то грохаем родителя
            if ((pnode != null) && (pnode.Nodes.Count == 0)) { pnode.Remove(); }
        }

        //private void DeleteNodeFromTask(TreeNode node, TreeView tv)
        //{//процедура удаления коллекции узлов из дерева заданий
        //    TreeNode pnode = null;
        //    if (node == null) return;
        //    //запоминаем родителя если есть 
        //    if (node.Parent != null) { pnode = node.Parent; }
        //    //удаляем узел
        //    node.Remove();
        //    //если у удаляемого узла есть родитель, и у этого родителя больше не осталось детей, то грохаем родителя
        //    if ((pnode != null) && (pnode.Nodes.Count == 0)) { pnode.Remove(); }
        //}

        private void DeleteAllDoneCounters()
        {//в этой процедре удаляются все счётчики, в которых не осталось неопрошенных видов энергии (отмечены и больше 0)
         //для быстрого переопроса неопрошенных
            for (int i = 0; i < TVTask485.Nodes.Count; i++)
            {
                try
                {
                    for (int j = 0; j < TVTask485.Nodes[i].Nodes.Count; j++)
                    {
                        ICounter counter = (ICounter)TVTask485.Nodes[i].Nodes[j].Tag;
                        //список отмеченных и непрочитанных видов энергии в текущем счётчике (смотрим по сумме тарифов)
                        var doneEnergy = from energy in counter.EnergyToRead where energy.check == true && energy.lastValueZone0 == 0 select energy;
                        if (doneEnergy.Count<CounterEnergyToRead>() == 0)
                        {//если список пустой, то удаляем счётчик из задания, т.к. все отмеченные в нём виды энергии были успешно считаны ранее
                            RemoveNodeFromTask(TVTask485.Nodes[i].Nodes[j]); j -= 1;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private void DeleteAllDoneCountersButt_Click(object sender, EventArgs e)
        {
            DeleteAllDoneCounters();
        }

        private void TVTask485ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (TVTask485.Nodes.Count == 0) e.Cancel = true;
        }

        private void StopButton_MouseEnter(object sender, EventArgs e)
        {

        }

        private void StopButton_MouseLeave(object sender, EventArgs e)
        {

        }

        private void LoadProfileFromDB_Click(object sender, EventArgs e)
        {
            DisableControls();
            if (globalSelectedNode.Tag is ICounter && globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                ICounter counter = (ICounter)globalSelectedNode.Tag;
                LoadProfileIntoCounter(counter, (int)PeriodNumEdit.Value, true, DateTimeEditN.Value, DateTimeEditK.Value);//загружаем профиль из базы в экземпляр счётчика     
            }
            EnableControls();
        }

        private void PowerProfileGrid_RowEnter(object sender, DataGridViewCellEventArgs e)
        {//здесь устанавливаем пикеры дат в соответствии с выделенными строками грида профиля для удобного переопроса неопрошенных получасовок
            Invoke(new Action(delegate
            {
                PeriodTemplatesComboBox.SelectedIndex = 0;
            }));
            try
            {
                if (PowerProfileGrid.SelectedRows.Count == 1)
                {
                    DateTimeEditN.Value = Convert.ToDateTime(PowerProfileGrid.Rows[e.RowIndex].Cells[7].Value).AddMinutes(-Convert.ToInt16(PowerProfileGrid.Rows[e.RowIndex].Cells[8].Value));
                    DateTimeEditK.Value = Convert.ToDateTime(PowerProfileGrid.Rows[e.RowIndex].Cells[7].Value).AddMinutes(Convert.ToInt16(PowerProfileGrid.Rows[e.RowIndex].Cells[8].Value));
                }
                if (PowerProfileGrid.SelectedRows.Count > 1)
                {
                    DateTimeEditK.Value = Convert.ToDateTime(PowerProfileGrid.SelectedRows[0].Cells[7].Value).AddMinutes(-Convert.ToInt16(PowerProfileGrid.SelectedRows[0].Cells[8].Value));//переключаем нижнюю дату согласно дате получасовки
                    DateTimeEditN.Value = Convert.ToDateTime(PowerProfileGrid.SelectedRows[PowerProfileGrid.SelectedRows.Count - 1].Cells[7].Value).AddMinutes(-Convert.ToInt16(PowerProfileGrid.Rows[PowerProfileGrid.SelectedRows.Count - 1].Cells[8].Value));//переключаем нижнюю дату согласно дате получасовки
                }
            }
            catch
            {

            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyicon.Visible = true;
                this.Hide();
            }
        }

        private void notifyicon_MouseClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
            notifyicon.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void AddCounterButt_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode == null) return;

            //вызов процедуры создания объекта
            IDevice device = (IDevice)globalSelectedNode.Tag;

            if (globalSelectedNode.Tag.GetType() == typeof(Mercury225PLC1))//смотрим какой тип у родительского узла 
                CreateObject("counter_plc", device.ID);//если родительский узел - концентратор, то создаём объект в таблице PLC-счётчиков

            if (globalSelectedNode.Tag is IConnection)//смотрим какой тип у родительского узла 
                CreateObject("counter_rs", device.ID);//если родительский узел - модем или шлюз, то создаём объект в таблице счётчиков с витой парой
        }

        private void AddGateButt_Click(object sender, EventArgs e)
        {
            CreateObject("gate", 0);//создаём шлюз. Родитель = 0 
        }

        private void UpdateObject()
        {//процедура сохранения конфигурации объектов
            if (globalSelectedNode == null) return;//если не выбран ни один узел, то выходим      
            IDevice device = (IDevice)globalSelectedNode.Tag;//интерфейса IDevice чтобы вытащит идентификатор узла
            //далее смотрим какой класс у объекта в узле: если шлюз
            if (globalSelectedNode.Tag.GetType() == typeof(Mercury228))
            {

                //строка автоконфигурации, взятая с контролов формы.  Получаем её через процедуру, реализованную в классе шлюза
                string autoconfstr = Mercury228.FormConfigString(EvenOddGateCombo.Text + "/" + EvenOddCheckGateCombo.Text + "/" + StopBytesGateCombo.Text + "/" +
                                     DataBytesGateCombo.Text + "/" + PortRateGateCombo.Text + "/" + TimeoutPackageGateCombo.Text + "/" + PauseGateNum.Value + " симв/");
                bool autoconf = true;//переменная флага автоконфигурации. По-умолчанию истина
                if (DevicePropertiesGrid.Rows[6].Cells[1].Value.ToString() == "False") autoconf = false;
                try
                {
                    //вызываем процедуру обновления подключений
                    DataBaseManagerMSSQL.Update_Connection_Row(device.ID, DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[3].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[5].Cells[1].Value.ToString(),
                                         autoconf, Convert.ToInt32(autoconfstr, 2).ToString("X"),
                                         DevicePropertiesGrid.Rows[8].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[9].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[10].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[11].Cells[1].Value.ToString());
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }

            }
            //смотрим какой класс у объекта в узле: если модем
            if (globalSelectedNode.Tag.GetType() == typeof(Modem))
            {
                try
                {
                    //вызываем процедуру обновления подключений
                    DataBaseManagerMSSQL.Update_Connection_Row(device.ID, DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[3].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[5].Cells[1].Value.ToString(),
                                         false, "0",
                                         DevicePropertiesGrid.Rows[8].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[9].Cells[1].Value.ToString(),
                                         DevicePropertiesGrid.Rows[10].Cells[1].Value.ToString(), DevicePropertiesGrid.Rows[11].Cells[1].Value.ToString());
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }
            }
            //смотрим какой класс у объекта в узле: если концентратор
            if (globalSelectedNode.Tag.GetType() == typeof(Mercury225PLC1))
                try
                {
                    DataBaseManagerMSSQL.Update_Concentrator_Row(device.ID, DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString()
                                                                       , Convert.ToInt16(DevicePropertiesGrid.Rows[1].Cells[1].Value)
                                                                       , DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString());
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }
            //смотрим какой класс у объекта в узле: если витая пара
            if (globalSelectedNode.Tag is ICounter && globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
                try
                {
                    double ca = 0;
                    string id = DevicePropertiesGrid.Rows[15].Cells[1].Value.ToString();

                    if (DevicePropertiesGrid.Rows[14].Cells[1].Value.ToString() == String.Empty)
                        ca = 0;
                    else
                        ca = Convert.ToDouble(DevicePropertiesGrid.Rows[14].Cells[1].Value.ToString());

                    DataBaseManagerMSSQL.Update_CounterRS_Row(device.ID, DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString(), //name
                                                                    DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString(), //street
                                                                    DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString(), //house
                                                                    DevicePropertiesGrid.Rows[3].Cells[1].Value.ToString(), //serial_number
                                                    Convert.ToInt32(DevicePropertiesGrid.Rows[4].Cells[1].Value),           //net_address
                                                                    DevicePropertiesGrid.Rows[5].Cells[1].Value.ToString(), //comments
                                                                    DevicePropertiesGrid.Rows[6].Cells[1].Value.ToString(), //district
                                  Convert.ToInt16(Convert.ToBoolean(DevicePropertiesGrid.Rows[8].Cells[1].Value.ToString())), //power_profile_exists
                                  Convert.ToInt16(Convert.ToBoolean(DevicePropertiesGrid.Rows[9].Cells[1].Value.ToString())), //integrated_feed
                                                                    DevicePropertiesGrid.Rows[10].Cells[1].Value.ToString(),//pwd1
                                                                    DevicePropertiesGrid.Rows[11].Cells[1].Value.ToString(),//pwd2
                                                    Convert.ToInt16(DevicePropertiesGrid.Rows[13].Cells[1].Value.ToString()),
                                                    ca,
                                                    id
                                                    );
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }
            //смотрим какой класс у объекта в узле: если PLC-счётчик
            if (globalSelectedNode.Tag.GetType() == typeof(MercuryPLC1))
            {//DateTime не принимает значение NULL по-умолчанию, поэтому столько возни
                DateTime? e_t0_last_date = null; DateTime? e_t1_last_date = null;
                DateTime? e_t2_last_date = null; DateTime? e_t3_last_date = null;
                DateTime? e_t4_last_date = null;

                try
                {
                    if (Convert.IsDBNull(DevicePropertiesGrid.Rows[13].Cells[1].Value)) e_t0_last_date = null; else e_t0_last_date = Convert.ToDateTime(DevicePropertiesGrid.Rows[13].Cells[1].Value);
                    if (Convert.IsDBNull(DevicePropertiesGrid.Rows[14].Cells[1].Value)) e_t1_last_date = null; else e_t1_last_date = Convert.ToDateTime(DevicePropertiesGrid.Rows[14].Cells[1].Value);
                    if (Convert.IsDBNull(DevicePropertiesGrid.Rows[15].Cells[1].Value)) e_t2_last_date = null; else e_t2_last_date = Convert.ToDateTime(DevicePropertiesGrid.Rows[15].Cells[1].Value);
                    if (Convert.IsDBNull(DevicePropertiesGrid.Rows[16].Cells[1].Value)) e_t3_last_date = null; else e_t3_last_date = Convert.ToDateTime(DevicePropertiesGrid.Rows[16].Cells[1].Value);
                    if (Convert.IsDBNull(DevicePropertiesGrid.Rows[17].Cells[1].Value)) e_t4_last_date = null; else e_t4_last_date = Convert.ToDateTime(DevicePropertiesGrid.Rows[17].Cells[1].Value);
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }

                try
                {
                    DataBaseManagerMSSQL.Update_CounterPLC_Row(device.ID, DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString(), //name
                                                                     DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString(), //street
                                                                     DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString(), //house
                                                                     DevicePropertiesGrid.Rows[3].Cells[1].Value.ToString(), //serial_number
                                                     Convert.ToInt16(DevicePropertiesGrid.Rows[4].Cells[1].Value),           //net_address
                                                                     DevicePropertiesGrid.Rows[5].Cells[1].Value.ToString(), //comments
                                                                     DevicePropertiesGrid.Rows[6].Cells[1].Value.ToString(), //district

                                                     Convert.ToDouble(DevicePropertiesGrid.Rows[8].Cells[1].Value), //e_t0_last
                                                     Convert.ToDouble(DevicePropertiesGrid.Rows[9].Cells[1].Value), //e_t1_last
                                                     Convert.ToDouble(DevicePropertiesGrid.Rows[10].Cells[1].Value),//e_t2_last
                                                     Convert.ToDouble(DevicePropertiesGrid.Rows[11].Cells[1].Value),//e_t3_last
                                                     Convert.ToDouble(DevicePropertiesGrid.Rows[12].Cells[1].Value),//e_t4_last

                                                     e_t0_last_date,
                                                     e_t1_last_date,
                                                     e_t2_last_date,
                                                     e_t3_last_date,
                                                     e_t4_last_date

                                                                );
                }
                catch
                {
                    LoadNodeData(globalSelectedNode);//обновим грид свойств (криво введённые данные отменяться)
                    return;
                }
            }

            LoadNodeData(globalSelectedNode);//подгружаем изменения в экземпляр класса (чтобы в экземплярах зафиксировать новую информацию)
            DateTime currentDate = DateTime.Now;
            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Объект сохранён: " + globalSelectedNode.Text + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }

        private void DeleteObject(TreeNode SelectedNode)
        {
            if (SelectedNode.Tag is IConnection && User.Role != "admin")
            {
                MessageBox.Show("ТП удалять нельзя", "Ошибка удаоения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DisableControls();
            if (SelectedNode == null) return;
            DialogResult result = MessageBox.Show("Точно удалить?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                EnableControls();
                return;
            }

            IDevice device = (IDevice)globalSelectedNode.Tag;

            if (SelectedNode.Tag is ICounter && SelectedNode.Tag.GetType() != typeof(MercuryPLC1)) DataBaseManagerMSSQL.Delete_CounterRS_Row(device.ID);
            if (SelectedNode.Tag.GetType() == typeof(MercuryPLC1)) DataBaseManagerMSSQL.Delete_CounterPLC_Row(device.ID);
            if (SelectedNode.Tag.GetType() == typeof(Mercury225PLC1)) DataBaseManagerMSSQL.Delete_Concentrator_Row(device.ID);
            if (SelectedNode.Tag is IConnection) DataBaseManagerMSSQL.Delete_Connection_Row(device.ID);

            SelectedNode.Remove();//непосредственно сам узел дерева
            EnableControls();
        }

        private void CreateObject(string objectType, int parent_id)
        {
            switch (objectType)
            {
                case "gate"://шлюз
                    {
                        int new_id = DataBaseManagerMSSQL.Create_Connection_Row(objectType);//вызываем процедуру создания объекта
                        TreeNode newConnectionNode = new TreeNode();
                        //создаем экземпляр класса
                        Mercury228 newGate = new Mercury228(new_id, "Новый шлюз", "+7", "127.0.0.1", "0", "0");
                        //привязываем экзепляр класса к узлу в дереве
                        newConnectionNode.Tag = newGate;
                        newConnectionNode.ImageIndex = 0; newConnectionNode.SelectedImageIndex = 1;//пиктограммы                                                                                                       
                        newConnectionNode.Name = new_id.ToString() + "_" + objectType;//даем узлу имя (хз зачем)
                        newConnectionNode.Text = "Новый шлюз";//текст узла
                        FullTree.Nodes.Insert(globalSelectedNode.Index + 1, newConnectionNode);
                        break;
                    }

                case "modem"://шлюз
                    {
                        int new_id = DataBaseManagerMSSQL.Create_Connection_Row(objectType);//вызываем процедуру создания объекта
                        TreeNode newConnectionNode = new TreeNode();
                        //создаем экземпляр класса
                        Modem newModem = new Modem(new_id, "Новый модем", "+7", "127.0.0.1", "0");
                        //привязываем экзепляр класса к узлу в дереве
                        newConnectionNode.Tag = newModem;
                        newConnectionNode.ImageIndex = 8; newConnectionNode.SelectedImageIndex = 9;//пиктограммы                                                                                                       
                        newConnectionNode.Name = new_id.ToString() + "_" + objectType;//даем узлу имя (хз зачем)
                        newConnectionNode.Text = "Новый модем";//текст узла
                        FullTree.Nodes.Insert(globalSelectedNode.Index + 1, newConnectionNode);
                        break;
                    }

                case "concentrator_1"://концентратор
                    {
                        int new_id = DataBaseManagerMSSQL.Create_Concentrator_Row(objectType, parent_id);//вызываем процедуру создания объекта
                        string netAdr = "2001";
                        //создаем узел дерева
                        TreeNode newConcentratorNode = new TreeNode();
                        //создаем экземпляр класса
                        Mercury225PLC1 newConcentrator = new Mercury225PLC1(new_id, parent_id, "Новый концентратор", netAdr, String.Empty);
                        //привязываем экземпляр класса к узлу в дереве
                        newConcentratorNode.Tag = newConcentrator;
                        newConcentratorNode.Text = netAdr.ToString();
                        //пиктограмми
                        newConcentratorNode.ImageIndex = 2;
                        newConcentratorNode.SelectedImageIndex = 3;
                        //даем узлу имя
                        newConcentratorNode.Name = new_id.ToString() + "_" + netAdr.ToString();
                        //к узлу подключения добавляем вновь полученный узел концентратора
                        globalSelectedNode.Nodes.Add(newConcentratorNode);
                        globalSelectedNode.Expand();
                        break;
                    }

                case "counter_plc":
                    {
                        int new_id = DataBaseManagerMSSQL.Create_CounterPLC_Row(parent_id);//вызываем процедуру создания объекта
                         //пытаемся добавить новый счётчик в разрешённое задание общего опроса
                        int allowedTaskID = DataBaseManagerMSSQL.Return_Allowed_Task("plc");//Номер разрешённого задания общего опроса
                        //если есть разрешённое задание общего опроса, то кидаем туда созданный счётчик  
                        if (allowedTaskID > -1)
                        {
                            DataBaseManagerMSSQL.Add_Counter_To_Task(new_id, allowedTaskID);
                        }
                        //создаем узел дерева
                        TreeNode newCounterNode = new TreeNode();
                        //создаем экземпляр класса
                        Mercury225PLC1 parent = (Mercury225PLC1)globalSelectedNode.Tag;
                        //в серийный номер помещаем полученный ранее идентификатор из последовательности чтобы серийные номера не дублировались. Это нужно для того чтобы совпадало сразу с данными в базе
                        MercuryPLC1 newCounter = new MercuryPLC1(new_id, parent_id, "Новый счётчик PLC", 0, new_id.ToString(), false, false);
                        //привязываем экзепляр класса к узлу в дереве
                        newCounterNode.Tag = newCounter;
                        newCounterNode.Text = "Новый счётчик PLC";
                        //пиктограммы
                        newCounterNode.ImageIndex = 6;
                        newCounterNode.SelectedImageIndex = 7;
                        //даем узлу имя
                        newCounterNode.Name = newCounter.ID + '_' + newCounter.Name;
                        //к узлу подключения добавляем вновь полученный узел счётчика
                        globalSelectedNode.Nodes.Add(newCounterNode);
                        globalSelectedNode.Expand();
                        break;
                    }

                case "counter_rs"://счётчик на витой паре
                    {
                        if (CounterTypeCombo.Text == String.Empty) return; //если никакой тип счётчика не выбран                     
                        int new_type_id = 0;//идентификатор типа счётчика
                        switch (CounterTypeCombo.SelectedIndex)
                        {//смотрим в выпадающем списке какой счётчик создаём
                            //case "Меркурий 233\\234\\236": new_type_id = 1; break;
                            //case "СЭТ": new_type_id = 3; break;
                            //case "Меркурий 200": new_type_id = 5; break;
                            //case "ПСЧ": new_type_id = 4; break;
                            case 0: new_type_id = 1; break;//Меркурии трёхфазные
                            case 1: new_type_id = 3; break;//СЭТы
                            case 2: new_type_id = 5; break;//Меркурии однофазные
                        }
                        //вызываем процедуру создания объекта
                        int new_id = DataBaseManagerMSSQL.Create_CounterRS_Row(new_type_id, parent_id);//возвращаем идентификатор нового счётчика для того чтобы занести его в экземпляр класса    
                        //пытаемся добавить новый счётчик в разрешённое задание
                        int allowedTaskID = DataBaseManagerMSSQL.Return_Allowed_Task("rs485");//Номер разрешённого задания
                        //если есть разрешённое задание, то кидаем туда счётчик  
                        if (allowedTaskID > -1) DataBaseManagerMSSQL.Add_Counter_To_Task(new_id, allowedTaskID);
                        TreeNode newCounterNode = new TreeNode();//создаем узел в дереве
                        //создаем экземпляр класса                     
                        if (new_type_id == 1) // в зависимости от типа счётчика создаём соответствующий экзмепляр класса
                        {//Меркурий трёхфазный
                            MercuryRS485 newCounter = new MercuryRS485(new_id, parent_id, "Новый счётчик", 0, true,// (int)rowConnection["type_id"], 
                                //ProgrammLogEdit,
                                new_id.ToString(), 1
                                , newCounterNode
                                );//в серийный номер помещаем полученный ранее идентификатор из последовательности чтобы серийные номера не дублировались. 
                            //пиктограммы                             Здесь это нужно для того чтобы совпадало сразу с данными в базе
                            newCounterNode.ImageIndex = 4;
                            newCounterNode.SelectedImageIndex = 5;
                            newCounterNode.Tag = newCounter;//помещаем экземпляр класса счётчика в узел дерева
                            newCounterNode.Text = newCounter.Name;
                            newCounterNode.Name = newCounter.ID + '_' + newCounter.Name; //даем узлу имя (хз уже зачем)
                            //к узлу подключения добавляем вновь полученный узел счётчика
                            globalSelectedNode.Nodes.Add(newCounterNode);
                            globalSelectedNode.Expand();
                        }
                        //если счётчик - СЭТ
                        if (new_type_id == 3)
                        {
                            MicronSET newCounter = new MicronSET(new_id, parent_id, "Новый счётчик", 0, true, //(int)rowConnection["type_id"], 
                                //ProgrammLogEdit, 
                                new_id.ToString(), 1
                                , newCounterNode
                                ); //в серийный номер помещаем полученный ранее идентификатор из последовательности чтобы серийные номера не дублировались. 
                            //пиктограммы                              Здесь это нужно для того чтобы совпадало сразу с данными в базе
                            newCounterNode.ImageIndex = 11;
                            newCounterNode.SelectedImageIndex = 11;
                            newCounterNode.Tag = newCounter;//помещаем экземпляр класса счётчика в узел дерева
                            newCounterNode.Text = newCounter.Name;
                            newCounterNode.Name = newCounter.ID + '_' + newCounter.Name; //даем узлу имя (хз уже зачем)
                            //к узлу подключения добавляем вновь полученный узел счётчика
                            globalSelectedNode.Nodes.Add(newCounterNode);
                            globalSelectedNode.Expand();
                        }
                        //Меркурий однофазный
                        if (new_type_id == 5)
                        {
                            Mercury200RS485 newCounter = new Mercury200RS485(); 
                            newCounterNode.ImageIndex = 13;
                            newCounterNode.SelectedImageIndex = 13;
                            newCounterNode.Tag = newCounter;//помещаем экземпляр класса счётчика в узел дерева
                            newCounterNode.Text = newCounter.Name;
                            newCounterNode.Name = newCounter.ID + '_' + newCounter.Name; //даем узлу имя (хз уже зачем)
                            //к узлу подключения добавляем вновь полученный узел счётчика
                            globalSelectedNode.Nodes.Add(newCounterNode);
                            globalSelectedNode.Expand();
                        }
                        break;
                    }
            }

            DateTime currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Создан объект\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }

        private void AddConcentratorButt_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode == null)
            {
                return;
            }

            if (globalSelectedNode.Tag is IConnection)//смотрим какой тип у родительского узла 
            {
                IDevice device = (IDevice)globalSelectedNode.Tag;
                CreateObject("concentrator_1", device.ID);//если родительский узел - модем или шлюз, то создаём объект в таблице концентраторов
            }
        }

        private void AddModemButt_Click(object sender, EventArgs e)
        {
            CreateObject("modem", 0);//создаём модоем. Родитель = 0 
        }

        private void ApplyChangesButt_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateObject();
            }
            catch
            {
                return;
            }
        }

        private void DefaultSetsButt_Click(object sender, EventArgs e)
        {
            SetDefaultPort();
            //UpdateObject();
        }

        private void SetDefaultPort()
        {//значения контрлов на вкладки записи параметров в шлюз по-умолчанию
            PortRateGateCombo.SelectedIndex = 9;
            DataBytesGateCombo.SelectedIndex = 1;
            StopBytesGateCombo.SelectedIndex = 0;
            EvenOddGateCombo.SelectedIndex = 0;
            EvenOddCheckGateCombo.SelectedIndex = 0;
            PauseGateNum.Value = 3;
            TimeoutPackageGateCombo.SelectedIndex = 1;
        }

        private void EnergyGridContextMenu_Opening(object sender, CancelEventArgs e)
        {//по-умолчанию все пункты меню видимы
            EnergyGridContextMenu.Items[0].Visible = true;
            EnergyGridContextMenu.Items[1].Visible = true;
            EnergyGridContextMenu.Items[2].Visible = true;

            if (globalSelectedNode.Tag.GetType() == typeof(Mercury225PLC1) || globalSelectedNode.Tag.GetType() == typeof(MercuryPLC1))//смотрим какой тип у выбранного узла 
            {//если выбрали PLC, то пункты с пометкой параметров делаем невидимыми
                EnergyGridContextMenu.Items[1].Visible = false;
                EnergyGridContextMenu.Items[2].Visible = false;
            }
        }

        private void DevicePropertiesGrid_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // DevicePropertiesGrid.Columns[e.ColumnIndex].Visible = false;
        }

        private void TVMainContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (globalSelectedNode == null)
            {
                CreateObjectButt.Visible = true;//кнопка создания объектов
                DeleteObjectButt.Visible = false;//кнопка удаления объектов
            }
            else
            {
                CreateObjectButt.Visible = true;//кнопка создания объектов
                DeleteObjectButt.Visible = true;//кнопка удаления объектов                            

                if (globalSelectedNode == null)
                {
                    AddModemButt.Visible = true;//создание модема
                    AddGateButt.Visible = true;//создание шлюза
                    AddConcentratorButt.Visible = false;//создание концентратора
                    AddCounterButt.Visible = false;//создания счётчика
                    CounterTypeCombo.Visible = false;//тип счётчика

                }
                //если выбрано пожключение, то доступны все кнопки
                if (globalSelectedNode.Tag is IConnection || globalSelectedNode == null)
                {
                    AddModemButt.Visible = true;
                    AddGateButt.Visible = true;
                    AddConcentratorButt.Visible = true;
                    AddCounterButt.Visible = true;
                    CounterTypeCombo.Visible = true;
                }
                //если выбран концентратор, то доступна кнопка с добавлением счётчика, но без выпадающего меню с выбором типа
                if (globalSelectedNode.Tag.GetType() == typeof(Mercury225PLC1))
                {
                    AddModemButt.Visible = false;
                    AddGateButt.Visible = false;
                    AddConcentratorButt.Visible = false;
                    AddCounterButt.Visible = true;
                    CounterTypeCombo.Visible = false;
                }
                //если выбран счётчик
                if (globalSelectedNode.Tag is ICounter)
                {
                    CreateObjectButt.Visible = false;
                }
            }
            //если роль с ограниченными правами, то убираем кнопки создания и удаления
            if (User.Role == "reader" || User.Role == "observer")
            {
                CreateObjectButt.Visible = false;//кнопка создания объектов
                DeleteObjectButt.Visible = false;//кнопка удаления объектов     
            }
        }

        private void DeviceTabControl_Selected(object sender, TabControlEventArgs e)
        {


        }

        private void DeleteObjectButt_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode != null)
            DeleteObject(globalSelectedNode);
        }

        private void DeleteTaskPLCNode_Click(object sender, EventArgs e)
        {
            RemoveNodesFromTask(TVTaskPLC);
        }

        private void TVTaskPLCMenu_Opening(object sender, CancelEventArgs e)
        {
            if (TVTaskPLC.Nodes.Count == 0) e.Cancel = true;
        }

        private void TVTask485_MouseDown(object sender, MouseEventArgs e)
        {
            //  if (e.Button == MouseButtons.Right) TVTask485.SelectedNode = null;
        }

        private void CreateTask(TreeNodeCollection tv)
        {//создаём новое задание
            try
            {
                if (tv.Count == 0)
                {
                    MessageBox.Show("В дереве заданий пусто", "Ошибка создания задания", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string name = TaskNameTextBox.Text;
                if (TaskNameTextBox.Text == String.Empty) name = "Новое задание";
                string tv_type = String.Empty;//типа дерева
                List<TreeNode> collection = new List<TreeNode>();
                if (tv == TVTask485.Nodes)
                {
                    tv_type = "rs485";
                    int task_id = DataBaseManagerMSSQL.Create_Task_Row(name, tv_type);//создаём задание (строку в таблице)
                    //если стоит галочка "считывать при групповом опросе" на вкладке профиля и если не выбран шаблон дат (автоматизация снятия профиля)
                    //то добавляем строку в сетку задание\профиль (будет ли сниматься профиль для текущего задания или нет)
                    //Подаём в процедуру создания идентификатор задания и даты          
                    if ((TaskManager.GetProfile == true) && (PeriodTemplatesComboBox.SelectedIndex == 0))
                        DataBaseManagerMSSQL.Add_Task_Profile(task_id, DateTimeEditN.Value, DateTimeEditK.Value);
                    //формируем коллекцию из всех счётчиков в дереве задания                  
                    foreach (TreeNode parentNode in tv)
                    {
                        foreach (TreeNode childNode in parentNode.Nodes)
                        {
                            collection.Add(childNode);
                        }
                    }
                    TaskManager.LowerDate = DateTimeEditN.Value;//нижняя дата снятия профиля при создании задания
                    TaskManager.UpperDate = DateTimeEditK.Value;//верхняя дата снятия профиля при создании задания

                    TaskManager.richText = ProgrammLogEdit;
                    TaskManager.tree = collection;
                    TaskManager.OverwriteTree(task_id, tv_type);//процедура перезаписи (сохранения) структуры объектов
                }

                if (tv == TVTaskPLC.Nodes)
                {
                    tv_type = "plc";
                    int task_id = DataBaseManagerMSSQL.Create_Task_Row(name, tv_type);//создаём задание (строку в таблице)
                    //формируем коллекцию из всех счётчиков в дереве задания (шлюзы и концентраторы не учитываются)                 
                    foreach (TreeNode parentNode in tv)
                    {
                        foreach (TreeNode concentratorNode in parentNode.Nodes)
                        {
                            foreach (TreeNode ccounterNode in concentratorNode.Nodes)
                            {
                                collection.Add(ccounterNode);
                            }
                        }
                    }
                    TaskManager.richText = ProgrammLogEdit;
                    TaskManager.tree = collection;
                    TaskManager.OverwriteTree(task_id, tv_type);//процедура перезаписи (сохранения) структуры объектов
                }

                DateTime currentDate = DateTime.Now;
                //пишем действие в лог
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Задание " + name + " создано" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка создания задания", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void CreateTaskButt_Click(object sender, EventArgs e)
        {//создаём новое задание RS485
            CreateTask(TVTask485.Nodes);
        }

        private void SaveTaskButt_Click(object sender, EventArgs e)
        {
            List<TreeNode> tree = new List<TreeNode>(TVTask485.Nodes.Cast<TreeNode>());//преобразуем коллекцию узлов в список т.к. метод принимает списки, а не коллекции             
            TasksForm tf = new TasksForm(tree, ProgrammLogEdit, null, "rs485", (int)PeriodNumEdit.Value);//подгружаем в форму таблицу с заданиями.
            TaskManager.LowerDate = DateTimeEditN.Value;//нижняя дата снятия профиля при сохранении задания
            TaskManager.UpperDate = DateTimeEditK.Value;//верхняя дата снятия профиля при сохранении задания
            tf.Show();
        }

        private void MainMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private bool DeleteExcept(TreeNode pnode)
        {
            if (pnode.Level == globalSelectedNode.Level && pnode != globalSelectedNode)
            {
                pnode.Remove();
                return true;
            }

            if (pnode.Level != globalSelectedNode.Level)
            {
                for (int i = 0; i < pnode.Nodes.Count; i++)
                {
                    if (DeleteExcept(pnode.Nodes[i]))
                    {
                        i -= 1;
                    }
                }
            }
            return false;
        }

        private void DevicePropertiesGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DevicePropertiesGrid.CancelEdit();
        }

        private void EnableMonitoringButt_Click(object sender, EventArgs e)
        {

        }

        private void DisableMonitoringButt_Click(object sender, EventArgs e)
        {

        }

        private void GetPowerProfileCheck_CheckedChanged(object sender, EventArgs e)
        {
            TaskManager.GetProfile = GetPowerProfileCheck.Checked;
        }

        private void MonitoringControlsPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void EnableMonitoringButt_Click_1(object sender, EventArgs e)
        {
            TaskReadingTimer.Enabled = true;
            EnableMonitoringButt.Enabled = false;
            DisableMonitoringButt.Enabled = true;
        }

        private void DisableMonitoringButt_Click_1(object sender, EventArgs e)
        {
            TaskReadingTimer.Enabled = false;
            EnableMonitoringButt.Enabled = true;
            DisableMonitoringButt.Enabled = false;
        }

        private void DevicePropertiesGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {//если выбранный узел PLC-счётчик, то определённые ячейки делаем календарями на время редактирования
            DevicePropertiesGrid.Controls.Clear();//очищаем грид от всех контролов
            if (globalSelectedNode.Tag.GetType() == typeof(MercuryPLC1))
            {
                if (e.ColumnIndex == 1)
                {//убеждаемся что ячейки те, которые нам нужны
                    if (e.RowIndex == 13 || e.RowIndex == 14 || e.RowIndex == 15 || e.RowIndex == 16 || e.RowIndex == 17)
                    {
                        DevicePropertiesGrid.AllowUserToResizeColumns = false;//запрещаем менять размер столбцов пока календарь активен   
                        DevicePropertiesGrid.AllowUserToResizeRows = false;//запрещаем менять размер строк пока календарь активен  
                        DateTimePicker oDateTimePicker = new DateTimePicker();//создаём новый календарь
                        DevicePropertiesGrid.Controls.Add(oDateTimePicker);//добавляем календарь к гриду
                        oDateTimePicker.CustomFormat = "dd.MM.yyyy HH:mm:ss";
                        oDateTimePicker.Format = DateTimePickerFormat.Custom;
                        Rectangle oRectangle = DevicePropertiesGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);//область контрола
                        oDateTimePicker.Size = new Size(oRectangle.Width, oRectangle.Height);//размеры
                        oDateTimePicker.Location = new Point(oRectangle.X, oRectangle.Y);//координаты
                        oDateTimePicker.CloseUp += new EventHandler(oDateTimePicker_CloseUp);//событие сворачивания
                        oDateTimePicker.VisibleChanged += new EventHandler(oDateTimePicker_VisibleChanged);//событие изменения видимости
                        oDateTimePicker.TextChanged += new EventHandler(oDateTimePicker_OnTextChange);//событие изменения текста
                        oDateTimePicker.Visible = true;//делаем календарь видимым
                    }
                }
            }
            //заставляем выпадающие списки раскрыться с первого клика (изначально это не так)
            if (DevicePropertiesGrid[e.ColumnIndex, e.RowIndex].GetType() == typeof(DataGridViewComboBoxCell))
            {
                DevicePropertiesGrid.BeginEdit(true);//вводим нажатую ячейку в режим редактирования
                ComboBox comboBox = (ComboBox)DevicePropertiesGrid.EditingControl;//получаем контрол, находящийся в ячейке, если он в режиме редактирования
                comboBox.DroppedDown = true;//раскрываем выпадающий список
            }
        }

        private void oDateTimePicker_OnTextChange(object sender, EventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            DevicePropertiesGrid.CurrentCell.Value = dtp.Text.ToString();
        }

        private void oDateTimePicker_VisibleChanged(object sender, EventArgs e)
        {//если календарь скрылся, то разрешаем пользователю менять размер столбцов
            DateTimePicker dtp = (DateTimePicker)sender;
            if (dtp.Visible == false)
            {
                DevicePropertiesGrid.AllowUserToResizeColumns = true;
                DevicePropertiesGrid.AllowUserToResizeRows = true;
            }
        }

        private void oDateTimePicker_CloseUp(object sender, EventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            dtp.Visible = false;//свернули календарь, значит отработали и скрыли его            
        }

        private void DevicePropertiesGrid_Leave(object sender, EventArgs e)
        {//когда выходим из грида скрываем все имеющиеся контролы
            foreach (Control cntrl in DevicePropertiesGrid.Controls)
            {
                if (cntrl.GetType() == typeof(DateTimePicker))
                {
                    DevicePropertiesGrid.CurrentCell.Value = cntrl.Text.ToString();
                    cntrl.Visible = false;
                }
            }
        }

        private void DevicePropertiesGrid_CellEnter(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void TVMainToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void DevicePropertiesGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.Handled = true;
        }

        private void DevicePropertiesGrid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

        private void AddImageButt_Click(object sender, EventArgs e)
        {
            AddImage();
        }

        private void AddImage()
        {//здесь добавляем картинку в базу
            try
            {
                if (globalSelectedNode.Tag == null) return;
                OpenFileDialog opfdlg = new OpenFileDialog();
                opfdlg.Multiselect = true;//можно выделить несколько файлов одновременно
                opfdlg.Filter = "Изображения (*.jpg)|*.jpg|Изображения (*.jpeg)|*.jpeg";
                if (opfdlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    IDevice device = (IDevice)globalSelectedNode.Tag;

                    foreach (string filename in opfdlg.FileNames)
                    {//циклимся по именам выбранных файлов
                        int imageID = DataBaseManagerMSSQL.Add_Image(device.ID, filename);
                        Image image = Image.FromFile(filename);//грузим картинку из файла
                        byte[] bArr = imgToByteArray(image);//преобразуем в массив
                        MemoryStream stream = new MemoryStream(bArr);//помещаем в поток в оперативной памяти
                        CreatePictureBox(imageID, stream);//создаём картинку на панели
                    }
                    Cursor = Cursors.Default;
                }
            }
            catch
            { }
        }

        private byte[] imgToByteArray(Image img)
        {
            MemoryStream mStream = new MemoryStream();
            img.Save(mStream, img.RawFormat);
            return mStream.ToArray();
        }

        private void ShowImages()
        {//здесь грузим картинки из базы по указанному объекту     
            DisableControls();
            ImageFlow.Controls.Clear();//очищаем существующие контролы на панели
            if (globalSelectedNode.Tag == null) return;
            IDevice device = (IDevice)globalSelectedNode.Tag;
            //грузим картинки
            DataTable dt = DataBaseManagerMSSQL.Return_Images(device.ID);
            if (dt.Rows.Count == 0)
            {
                EnableControls();
                return;
            }
            //циклимся по картинкам
            foreach (DataRow dr in dt.Rows)
            {
                Byte[] image = (Byte[])dr["image"];//берём данные из поля таблицы (массив байт)
                int imageID = (int)dr["id"];
                MemoryStream stream = new MemoryStream(image);//помещаем в поток (хранящийся в оперативной памяти)
                CreatePictureBox(imageID, stream);
            }
            EnableControls();
        }

        private void CreatePictureBox(object imageID, MemoryStream stream)
        {
            try
            {
                PictureBox picBox = new PictureBox();//создаём новый контрол для картинки               
                picBox.SizeMode = PictureBoxSizeMode.Zoom;
                picBox.Image = Image.FromStream(stream);//грузим картинку из потока                
                picBox.Size = new Size(300, 300);//задаём размеры  
                picBox.DoubleClick += new EventHandler(ShowBigPicture); //привязываем процедуру ShowBigPicture к событию двойного щелчка 
                                                                        //через системный делегат EventHandler
                if (User.Role == "editor" || User.Role == "admin")//если роль позволяет удалять картинки
                {
                    ContextMenuStrip cm = new ContextMenuStrip();//создаём контекстное меню с кнопкой удаления изображения
                    cm.ShowImageMargin = false;
                    ToolStripMenuItem tsmi = new ToolStripMenuItem();//создаём кнопку удаления изображения
                    tsmi.Tag = imageID;//помещаем идентификатор картинки в это поле, чтобы потом можно было удалить картинку
                    tsmi.Text = "Удалить";
                    tsmi.Click += new EventHandler(DeleteImageButtonClick);//привязываем процедуру удаления изображения к событию нажатия кнопки
                                                                           //через системный делегат EventHandler
                    cm.Items.Add(tsmi);//добавляем кнопку удаления в контекстное меню
                    picBox.ContextMenuStrip = cm;
                }
                ImageFlow.Controls.Add(picBox);//добавляем картинку на панель 
            }
            catch
            {
                return;
            }
        }

        private void DeleteImageButtonClick(object sender, EventArgs e)
        {//здесь удаляем картинку из базы
            Cursor = Cursors.WaitCursor;
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            foreach (Control ctrl in ImageFlow.Controls)
            {//чтобы удалить картинку без последующего моргания (из-за полной перезагрузки через процедуру ShowImages)
             //, нужно проциклиться по текущим контролам и убедиться, что удаляем необходимый контрол из панели
                if (ctrl.ContextMenuStrip == tsmi.GetCurrentParent())//если контекстное меню принадлежит картинке, значит эта картинка нам нужна
                {
                    DataBaseManagerMSSQL.Delete_Image((int)tsmi.Tag);
                    ImageFlow.Controls.Remove(ctrl);
                }
            }
            Cursor = Cursors.Default;
        }

        private void ShowBigPicture(object sender, EventArgs e)
        {//здесь увеличиваем картинку по клику
            PictureBox pb = (PictureBox)sender;
            PhotoForm pf = new PhotoForm();
            pf.BackgroundImage = pb.Image;
            pf.Size = new Size(1000, 1000);
            pf.StartPosition = FormStartPosition.CenterScreen;
            pf.ShowDialog();
        }

        private void UsersFormButt_Click(object sender, EventArgs e)
        {
            UsersForm uf = new UsersForm();
            uf.StartPosition = FormStartPosition.CenterScreen;
            uf.ShowDialog();
        }

        private void DeviceEnergyGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void DeviceEnergyGrid_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {

        }

        private void DevicePropertiesGrid_MouseLeave(object sender, EventArgs e)
        {

        }

        private void PeriodTemplatesComboBox_SelectedValueChanged(object sender, EventArgs e)
        {

        }

        private void PeriodTemplatesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TaskManager.PeriodTemplatesComboBoxSelectedIndex = PeriodTemplatesComboBox.SelectedIndex;

            switch (PeriodTemplatesComboBox.SelectedIndex) //применяем шаблоны дат профиля
            {
                case 0://указать
                    {
                        DateTimeEditN.Enabled = true;
                        DateTimeEditK.Enabled = true;
                    }
                    break;

                case 1://предыдущие сутки
                    {
                        DateTimeEditN.Value = DateTime.Today.AddDays(-1);
                        DateTimeEditK.Value = DateTime.Today;
                        DateTimeEditN.Enabled = false;
                        DateTimeEditK.Enabled = false;
                    }
                    break;

                case 2://предыдущий месяц
                    {
                        DateTimeEditN.Value = DateTime.Now.AddMonths(-1);
                        DateTimeEditN.Value = new DateTime(DateTimeEditN.Value.Year, DateTimeEditN.Value.Month, 1);
                        DateTimeEditK.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        DateTimeEditN.Enabled = false;
                        DateTimeEditK.Enabled = false;
                    }
                    break;

                case 3://текущий месяц
                    {
                        DateTimeEditK.Value = DateTime.Now;
                        DateTimeEditN.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        DateTimeEditK.Value = new DateTime(DateTimeEditK.Value.Year, DateTimeEditK.Value.Month, DateTimeEditK.Value.Day);
                        DateTimeEditN.Enabled = false;
                        DateTimeEditK.Enabled = false;
                    }
                    break;
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            PeriodTemplatesComboBox.SelectedIndex = -1;
        }

        private void IntegrityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DisableControls();
            if (globalSelectedNode.Tag is ICounter && globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                ICounter counter = (ICounter)globalSelectedNode.Tag;
                LoadProfileIntoCounter(counter, (int)PeriodNumEdit.Value, true, DateTimeEditN.Value, DateTimeEditK.Value);//загружаем профиль из базы в экземпляр счётчика     
            }
            EnableControls();
        }

        private void ActualPowerButt_Click(object sender, EventArgs e)
        {
            string rmc_text = ReportMonthCombo.Text;
            int hsn_value = Convert.ToInt16(HoursShiftNumeric.Value);
            Thread thread = new Thread(() => 
            {
                ExportActualPowerReport(rmc_text, hsn_value);
            });
            thread.Start();
        }

        private void ChangesLogButt_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            ChangesForm cf = new ChangesForm();
            cf.ShowDialog();
            Cursor.Current = Cursors.Arrow;

        }

        private void ExportPowerProfileGrids_Click(object sender, EventArgs e)
        {
            ExportPowerProfileGridsProc();
        }

        private void ExportPowerProfileGridsProc()
        {
            DateTime currentDate = DateTime.Now;
            string CommandText = " Экспорт профилей мощности в Excel...";
            this.Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();
                DisableControls();
            }));
            
            if (TVTask485.Nodes.Count > 0)
            {
                ExportDataGrid(TVTask485, PowerProfileGrid, "Профиль мощности ", String.Empty);
            }
            
            currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                EnableControls();
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + " выполнен" + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
        }

        private void CreateBackupButt_Click(object sender, EventArgs e)
        {//создаём резервную копию базы данных
            DateTime currentDate = DateTime.Now;
            string CommandText = " Создание резервной копии базы данных...";

            DisableControls();

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.OverwritePrompt = false;
            saveDialog.Filter = "Файлы резервных копий (*.bak)|*.bak|All files (*.*)|*.*";
            saveDialog.FilterIndex = 2;
            saveDialog.FileName = DefaultBackupPathTextBox.Text;

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + CommandText + "...\r");
                ProgrammLogEdit.ScrollToCaret();

                Exception ex = DataBaseManagerMSSQL.Create_Backup(saveDialog.FileName);
                //если создание копии прошло без ошибок
                if (ex == null)
                {
                    MessageBox.Show("Резервная копия успешно создана", "Создание резервной копии", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Создание резервной копии базы данных прошло успешно. Путь: " + saveDialog.FileName + "\r");
                }
                else ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Создание резервной копии базы данных не получилось. Причина: " + ex.Message + "\r");
                ProgrammLogEdit.ScrollToCaret();
            }

            EnableControls();
        }

        private void ProgrammLogContextMenu_Opening(object sender, CancelEventArgs e)
        {

        }

        private void ProgContItem1_Click(object sender, EventArgs e)
        {

        }

        private void ProgrammLogEdit_TextChanged(object sender, EventArgs e)
        {
            //if (ProgrammLogEdit.Lines.Count() == 10000)
            //{
            //    ProgrammLogEdit.Clear();
            //}
        }

        private void button1_Click_2(object sender, EventArgs e)
        {

        }

        private void ExportTaskToXML(TreeNodeCollection collection)
        {//здесь выгружаем задание в XML-файл
            DateTime currentDate = DateTime.Now;
            ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка дерева в XML-файл\r");
            DisableControls();
            XDocument xdoc = new XDocument();

            XElement task = new XElement("task");
            xdoc.Add(task);

            foreach (TreeNode connectionNode in collection)
            {
                try
                {//цикл по подключениям
                    IConnection connectionNodeTag = (IConnection)connectionNode.Tag;
                    XElement connectionElement = new XElement(connectionNode.Tag.GetType().ToString().Substring(11));
                    connectionElement.Add(new XAttribute("channel", connectionNodeTag.Channel));
                    connectionElement.Add(new XAttribute("ip", connectionNodeTag.IP));
                    connectionElement.Add(new XAttribute("port", connectionNodeTag.Port));
                    connectionElement.Add(new XAttribute("cbst", connectionNodeTag.CBST));
                    connectionElement.Add(new XAttribute("phone", connectionNodeTag.Phone));
                    connectionElement.Add(new XAttribute("name", connectionNodeTag.Name));
                    task.Add(connectionElement);
                    //цикл по дочерним узлам (счётчики или концентраторы)
                    foreach (TreeNode childNode in connectionNode.Nodes)
                    {
                        //проверяем тип дочернего узла
                        if (childNode.Tag is ICounter && childNode.Tag.GetType() != typeof(MercuryPLC1))
                        {
                            ICounter childNodeTag = (ICounter)childNode.Tag;
                            XElement counterElement = new XElement(childNode.Tag.GetType().ToString().Substring(11));
                            counterElement.Add(new XAttribute("transrate", childNodeTag.TransformationRate));
                            counterElement.Add(new XAttribute("divider", childNodeTag.Divider));
                            counterElement.Add(new XAttribute("const", childNodeTag.CounterConst));
                            counterElement.Add(new XAttribute("net", childNodeTag.NetAddress));
                            counterElement.Add(new XAttribute("serial", childNodeTag.SerialNumber));
                            counterElement.Add(new XAttribute("name", childNodeTag.Name));
                            connectionElement.Add(counterElement);
                        }

                        if (childNode.Tag.GetType() == typeof(Mercury225PLC1) || childNode.Tag.GetType() == typeof(Mercury225PLC2))
                        {
                            IConcentrator concentratorNodeTag = (IConcentrator)childNode.Tag;
                            XElement concentratorElement = new XElement(childNode.Tag.GetType().ToString().Substring(11));
                            concentratorElement.Add(new XAttribute("net", concentratorNodeTag.NetAddress));
                            connectionElement.Add(concentratorElement);

                            foreach (TreeNode counterPLCNode in childNode.Nodes)
                            {
                                ICounter counterPLCTag = (ICounter)counterPLCNode.Tag;
                                XElement counterPLCElement = new XElement(counterPLCNode.Tag.GetType().ToString().Substring(11));
                                counterPLCElement.Add(new XAttribute("net", counterPLCTag.NetAddress));
                                counterPLCElement.Add(new XAttribute("serial", counterPLCTag.SerialNumber));
                                counterPLCElement.Add(new XAttribute("name", counterPLCTag.Name));
                                concentratorElement.Add(counterPLCElement);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    currentDate = DateTime.Now;
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка выгрузки дерева в XML-файл: " + ex.Message + "\r");
                    continue;
                }
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.OverwritePrompt = false;
            saveDialog.Filter = "XML (*.xml)|*.xml|All files (*.*)|*.*";
            saveDialog.FilterIndex = 1;

            if (saveDialog.ShowDialog() == DialogResult.OK) xdoc.Save(saveDialog.FileName);
            EnableControls();
        }

        private void ExportToXMLButt_Click(object sender, EventArgs e)
        {
            ExportTaskToXML(TVTask485.Nodes);

        }

        private void выгрузитьВXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportTaskToXML(FullTree.Nodes);
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            //this.SuspendLayout();
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            //this.ResumeLayout(true);
        }

        private void CreatePLCTask_Click(object sender, EventArgs e)
        {
            CreateTask(TVTaskPLC.Nodes);
        }

        private void SaveTaskPLC_Click(object sender, EventArgs e)
        {
            List<TreeNode> tree = new List<TreeNode>(TVTaskPLC.Nodes.Cast<TreeNode>());//преобразуем коллекцию узлов в список т.к. метод принимает списки, а не коллекции             
            TasksForm tf = new TasksForm(tree, ProgrammLogEdit, null, "plc", (int)PeriodNumEdit.Value);//подгружаем в форму таблицу с заданиями.    
            tf.Show();
        }

        private void DevicePropertiesGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void PowerProfileChart_DoubleClick(object sender, EventArgs e)
        {

        }

        private void ProgContItem2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Точно очистить лог?", "Подтверждение очистки лога", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;
            ProgrammLogEdit.Clear();
        }

        private void SearchNodeButt_Click(object sender, EventArgs e)
        {

        }

        private void LoadEnergyButt_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                return;
            }

            Cursor = Cursors.WaitCursor;

            MercuryPLC1 counter = (MercuryPLC1)globalSelectedNode.Tag;//экземпляр PLC-счётчика
            DataTable dt = DataBaseManagerMSSQL.Return_Counter_Energy_PLC_History(counter.SerialNumber, 2, DateTimeNEnergy.Value, DateTimeKEnergy.Value, "'Энергия, текущее потребление', 'Энергия, суточный срез'");

            if (dt.Rows.Count == 0)
            {
                Cursor = Cursors.Default;
                return;
            }

            DeviceEnergyGrid.DataSource = dt;
            Cursor = Cursors.Default;
        }

        private void ClonePLCCounters()
        {//здесь клонируем счётчики в дереве задания PLC
         //сначала пройдём по дереву задания собрав все счётчики в общую коллекцию
            DisableControls();
            List<TreeNode> collection = new List<TreeNode>();
            foreach (TreeNode parentNode in TVTaskPLC.Nodes)//цикл по присоединениям
            {
                for (int j = 0; j < parentNode.Nodes.Count; j++)
                {
                    foreach (TreeNode counterNode in parentNode.Nodes[j].Nodes)
                    {//добавляем счётчик в коллекцию  
                        collection.Add(counterNode);
                    }
                    //удаляем реальный концентратор
                    parentNode.Nodes[j].Remove();
                    j -= 1;
                }
                //теперь создаём виртуальные концентраторы
                for (int i = 1; i <= 6; i++)
                {
                    Mercury225PLC1 virtual_concentrator_tag = new Mercury225PLC1(0, 0, "0", "200" + i.ToString(), String.Empty);
                    TreeNode virtual_concentrator = new TreeNode("200" + i.ToString() + "K");
                    virtual_concentrator.ImageIndex = 2;
                    virtual_concentrator.SelectedImageIndex = 3;
                    virtual_concentrator.Tag = virtual_concentrator_tag;
                    parentNode.Nodes.Add(virtual_concentrator);
                }
                //далее нужно опять пройтись по концентраторам добавив в каждый концентратор сформированную в предыдщущем цикле коллекцию счётчиков
                foreach (TreeNode concentratorNode in parentNode.Nodes)//цикл по концентраторам
                {
                    try
                    {
                        List<TreeNode> collection_clone = new List<TreeNode>();
                        foreach (TreeNode node in collection)
                        {
                            TreeNode clonedNode = (TreeNode)node.Clone();//нужно клонировать коллекцию чтобы избежать ошибки дублирования узлов в дереве   
                            MercuryPLC1 counter = (MercuryPLC1)clonedNode.Tag;
                            //counter.virtualmode = true;//включаем виртуальный режим для того чтобы не записывать показания в базу
                            collection_clone.Add(clonedNode);//добавляем клон во временную коллекцию
                        }
                        concentratorNode.Nodes.AddRange(collection_clone.ToArray());//добавляем в концентратор клонированную коллекцию счётчиков                    
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка при клонировании", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                collection.Clear();//очищаем коллекцию для использования в следующем соединении
            }
            EnableControls();
        }

        private void Create1024PLCCounters(TreeNode targetConcentratorNode)
        {//здесь создаём 1024 виртуальных счётчика
            DisableControls();
            targetConcentratorNode.Nodes.Clear();//убираем дочерние узлы у целевого концентратора
            for (int i = 1; i <= 1024; i++)
            {
                TreeNode newCounterNode = new TreeNode();//создаём узел дерева               
                MercuryPLC1 newCounter = new MercuryPLC1(-1, -1, "(В" + i.ToString() + ")", i, "-1", false, true);//создаём экземпляр виртуального счётчика              
                newCounterNode.Tag = newCounter;//привязываем экземпляр класса к узлу в дереве
                newCounterNode.Text = "(В" + i.ToString() + ")";//имя - только сетевой адрес + буква указывающая на то, что он виртуальный
                //пиктограммы
                newCounterNode.ImageIndex = 6;
                newCounterNode.SelectedImageIndex = 7;
                newCounterNode.Name = "(В" + i.ToString() + " )";//имя - только сетевой адрес

                targetConcentratorNode.Nodes.Add(newCounterNode);//добавляем новый виртуальный счётчик к концентратору
            }
            EnableControls();
        }

        private void ClonePLCCountersButt_Click(object sender, EventArgs e)
        {
            ClonePLCCounters();
        }

        private void ExpandField_Click(object sender, EventArgs e)
        {
            //  ;
            // MainLayoutPanel.Ro
            //MainLayoutPanel.RowStyles[MainLayoutPanel.GetRow(LogTabControl)].Height += 100;
            //MainLayoutPanel.RowStyles[MainLayoutPanel.GetRow(MainMenu)].Height += 10;
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void DeviceTabControl_Selected_1(object sender, TabControlEventArgs e)
        {
            selectedTabPage = e.TabPage;
            if (selectedTabPage == ImagePage) ShowImages();
        }

        private void LoadProfileFromDBButt_Click(object sender, EventArgs e)
        {
            DisableControls();
            if (globalSelectedNode.Tag is ICounter && globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                ICounter counter = (ICounter)globalSelectedNode.Tag;
                LoadProfileIntoCounter(counter, (int)PeriodNumEdit.Value, true, DateTimeEditN.Value, DateTimeEditK.Value);//загружаем профиль из базы в экземпляр счётчика     
            }
            EnableControls();
        }

        private void ConnectDbButt_Click(object sender, EventArgs e)
        {
            if (this.ConnectToDataBase() == false)
            {
                MessageBox.Show("Подключение к базе данных не удалось!", "Ошибка подключения к базе данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void splitContainer1_Panel2_DoubleClick(object sender, EventArgs e)
        {
            MainSplit.SplitterDistance = 0;
        }

        private void DevicePropertiesGrid_DataError_1(object sender, DataGridViewDataErrorEventArgs e)
        {
            DevicePropertiesGrid.CancelEdit();
        }

        private void DeviceEnergyGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            ///
        }

        private void DeviceParametersGrid_DataError_1(object sender, DataGridViewDataErrorEventArgs e)
        {
            ///
        }

        private void DeviceJournalGrid_DataError_1(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void PowerProfileGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void DeviceMonitorGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void WriteParametersGrid_DataError_1(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void ShowErrorsFormButt_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            selectedNodesListClear(selectedNodesListGlobal);
            SearchNodesDelegate del = new SearchNodesDelegate(SearchNodes);
            ErrorsForm ef = new ErrorsForm(del);
            ef.Show();
        }

        private void TVMainContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            //if (e.CloseReason == ToolStripDropDownCloseReason.Keyboard)
            //{
            //    e.Cancel = true;
            //}
        }

        private void TasksTreeRS_Click(object sender, EventArgs e)
        {

        }

        private void ExportPLCLastEnergyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ExportToExcelLastEnergyPLC();
        }

        private void ExportToExcelLastEnergyPLCButt_Click(object sender, EventArgs e)
        {
            FullTree.Enabled = false;
            TVTaskPLC.Enabled = false;
            globalWorkerExportToExcel = new BackgroundWorker();
            globalWorkerExportToExcel.WorkerSupportsCancellation = true;
            globalWorkerExportToExcel.DoWork += new DoWorkEventHandler(ExportPLCLastEnergyWorker_DoWork);
            globalWorkerExportToExcel.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            globalWorkerExportToExcel.RunWorkerAsync();
        }

        private void ExportToExcelLastEnergyPLC()
        {//выгрузка последних показаний дерева PLC
            //пройдёмся по концентраторам в цикле и поместим каждый грид энергии концентратора в Excel
            DateTime currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка последних показаний PLC\r");
                ProgrammLogEdit.ScrollToCaret();
                ProgressBar.Value = 0;
                ProgressBar.Maximum = TVTaskPLC.Nodes.Count;
            }));
            CommonReports cr = new CommonReports("Последние показания PLC");
            int rowcount = 0;//номер последней строки на листе. Нужен чтобы не было перезаписи при следующей итерации по концентраторам
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                IConnection connectionTag = (IConnection)connection.Tag;
                foreach (TreeNode concentrator in connection.Nodes)
                {
                    if (globalWorkerExportToExcel.CancellationPending == true) { cr.DisposeExcel(); return; }
                    this.Invoke(new Action(delegate
                    {
                        LoadNodeData(concentrator);//грузим концентратор чтобы получить грид энергии
                    }));
                    cr.ExportEnergyPLC(rowcount, DeviceEnergyGrid, connectionTag.Name);//передаём в процедуру номер последней строки и грид энергии
                    rowcount += DeviceEnergyGrid.Rows.Count;//наращиваем номер последней строки 
                }
                Invoke(new Action(delegate
                {
                    ProgressBar.Value += 1;
                }));
            }
            Invoke(new Action(delegate
            {
                currentDate = DateTime.Now;
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка последних показаний PLC завершена\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            cr.OpenAfterExport();
        }

        private void StatisticsFormbutt_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            CommonReports cr = new CommonReports("Статистика");
            DateTime energyLastDate = DateTime.Now.AddDays(-3);
            DateTime errorLastDate = DateTime.Now.AddDays(-1);
            cr.ShowStatistics(DistrictsList, energyLastDate, errorLastDate);
            cr.OpenAfterExport();
            Cursor = Cursors.Default;
        }

        private void UsersFormButt2_Click(object sender, EventArgs e)
        {
            UsersForm uf = new UsersForm();
            uf.StartPosition = FormStartPosition.CenterScreen;
            uf.ShowDialog();
        }

        private void SettingsFormButt_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Раздел в разработке", "Ничего не выйдет", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CalendarFormButt2_Click(object sender, EventArgs e)
        {
            //if (DataBaseSet == null)
            //{
            //    MessageBox.Show("База данных не подключена", "Ничего не выйдет", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            CalendarForm cf = new CalendarForm();
            cf.ShowDialog();
        }

        private void ExportToExcelPLCButt_Click(object sender, EventArgs e)
        {

        }

        private void FixOverconsume_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("ВНИМАНИЕ! Корректируются только правые четыре разряда последней суммы показаний согласно последней ошибке перерасхода для каждого счётчика из задания." +
                " Эта функция подходит только для тех счётчиков, которые вернулись на главную последовательность показаний после сбоя. Для тех счётчиков, которые сбились с главной последовательности"
                + " на момент применения этой функции, она может быть вредна, т.к. утвердит сбой последовательности показаний. В случае утверждения сбоя, придётся ждать когда такие счётчики"
                + " вернутся на правильную последовательность и вновь применить эту функцию. Продолжить?", "Подтверждение исправления ошибок", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No) return;

            DateTime currentDate = DateTime.Now;
            ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Корректировка ошибок PLC\r");
            ProgrammLogEdit.ScrollToCaret();

            ProgressBar.Value = 0;
            ProgressBar.Maximum = TVTaskPLC.Nodes.Count;

            DisableControls();
            FixOverconsume();
            EnableControls();

            ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Корректировка ошибок PLC завершена\r");
            ProgrammLogEdit.ScrollToCaret();
        }

        private void FixOverconsume()
        {//здесь пытаемся исправить перерасходы в дереве ПОКА ТОЛЬКО ПО СУММЕ ПОКАЗАНИЙ
         //циклимся по PLC-счётчикам
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                foreach (TreeNode concentrator in connection.Nodes)
                {
                    foreach (TreeNode counter in concentrator.Nodes)
                    {
                        MercuryPLC1 device = (MercuryPLC1)counter.Tag;
                        //тащим из базы последнюю ошибку перерасхода по счётчику
                        DataTable dt = DataBaseManagerMSSQL.Return_Last_PLC_Error(device.ID, 4);
                        if (dt.Rows.Count == 0) continue;
                        //смотрим строку сообщения
                        string message = dt.Rows[0]["message"].ToString();
                        //вытаскиваем из этой строки значение Т0 (показания которые содержатся в ошибке)
                        int pos = message.IndexOf(".", 0);//находим первое вхождение символа в строке
                        string t0 = message.Substring(0, pos);//выделяем подстроку со значением Т0
                        t0 = t0.Substring(t0.Length - 4);//отрезаем последние 4 разряда
                        int t0_int = Convert.ToInt16(t0);//запоминаем в виде числа
                        dt = DataBaseManagerMSSQL.Return_CounterPLC_Row(device.ID);//тянем данные по счётчику чтобы узнать последние показания
                        string last_t0_str = dt.Rows[8][1].ToString();//запоминаем последние показания (те, что ещё хранятся в базе)
                        double last_t0_dbl = Convert.ToDouble(dt.Rows[8][1]);
                        last_t0_str = last_t0_str.PadLeft(9, '0');
                        last_t0_str = last_t0_str.Substring(last_t0_str.Length - 9);//отрезаем последние 4 разряда
                        double last_t0_dbl_temp = Convert.ToDouble(last_t0_str);//запоминаем в виде числа
                        double delta = last_t0_dbl_temp - t0_int; //считаем разницу мкежду показаниями
                        last_t0_dbl -= delta;//из последних показаний, которые хранятся в базе, вычитаем полученную разницу
                        DataBaseManagerMSSQL.Update_Last_PLC_Energy(last_t0_dbl, 0, device.ID, null);//обноваляем последние показания
                    }
                }
                ProgressBar.Value += 1;
            }
        }

        private void FixOldDate()
        {//здесь пытаемся исправить ошибки большого интервала времени в дереве ПОКА ТОЛЬКО ПО СУММЕ ПОКАЗАНИЙ
         //циклимся по PLC-счётчикам
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                foreach (TreeNode concentrator in connection.Nodes)
                {
                    foreach (TreeNode counter in concentrator.Nodes)
                    {
                        MercuryPLC1 device = (MercuryPLC1)counter.Tag;
                        //тащим из базы последнюю ошибку большого интервала времени по счётчику
                        DataTable dt = DataBaseManagerMSSQL.Return_Last_PLC_Error(device.ID, 5);
                        if (dt.Rows.Count == 0) continue;
                        //смотрим строку сообщения
                        string message = dt.Rows[0]["message"].ToString();
                        //вытаскиваем из этой строки значение Т0 (показания которые содержатся в ошибке)
                        //показания и дату берём из сообщения об ошибке. Показания можно брать как есть, а от даты отнимаем один день и подаём в счётчик
                        //показания находятся между = и /
                        int pos1 = message.IndexOf("=", 0);//находим первое вхождение подстроки в строке
                        int pos2 = message.IndexOf("/", 0);//находим первое вхождение подстроки в строке
                        int delta_pos = pos2 - pos1;//количество символов между = и /
                        string t0 = message.Substring(pos1 + 1, delta_pos - 1);//выделяем подстроку со значением Т0
                        int pos3 = t0.IndexOf(".", 0);//ищем точку в строке с числом
                        t0 = t0.Substring(0, pos3);//от начала строки до точки
                        double t0_int = Convert.ToInt32(t0);//запоминаем в виде числа

                        DateTime last_dt = Convert.ToDateTime(dt.Rows[0][0]);
                        last_dt = last_dt.AddDays(-1);//отнимаем один день

                        DataBaseManagerMSSQL.Update_Last_PLC_Energy(t0_int, 0, device.ID, last_dt);//обноваляем последние показания
                    }
                }
                ProgressBar.Value += 1;
            }
        }

        private void FixPLCLastDate()
        {//здесь исправляем дату ПП для группы счётчиков чтобы не приходилось вручную работать после долгого неопроса
         //циклимся по PLC-счётчикам
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                foreach (TreeNode concentrator in connection.Nodes)
                {
                    foreach (TreeNode counter in concentrator.Nodes)
                    {
                        MercuryPLC1 device = (MercuryPLC1)counter.Tag;

                        DateTime new_last_dt = new DateTime();
                        new_last_dt = DateTime.Now.AddDays(-1);//отнимаем один день

                        DataBaseManagerMSSQL.Update_Last_PLC_Energy(0, device.ID, new_last_dt);//обновляем последние показания
                    }
                }
                ProgressBar.Value += 1;
            }
        }

        private void ExportToExcelHistoryEnergyPLCButt_Click(object sender, EventArgs e)
        {
            FullTree.Enabled = false;
            TVTaskPLC.Enabled = false;
            globalWorkerExportToExcel = new BackgroundWorker();
            globalWorkerExportToExcel.WorkerSupportsCancellation = true;
            globalWorkerExportToExcel.DoWork += new DoWorkEventHandler(ExportPLCHistoryEnergyWorker_DoWork);
            globalWorkerExportToExcel.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            globalWorkerExportToExcel.RunWorkerAsync();
        }

        private void ExportPLCHistoryEnergyWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ExportToExcelHistoryEnergyPLC();
        }

        private void ExportToExcelHistoryEnergyPLC()
        {//выгрузка истории показаний дерева PLC
            //пройдёмся по счётчикам в цикле и поместим каждый грид энергии счётчика в Excel
            if (DateTimeNEnergy.Value >= DateTimeKEnergy.Value)
            {
                MessageBox.Show("Начальная дата не может быть больше или равна конечной! Выберите произвольный PLC-счётчик и на вкладке 'Энергия' поставьте допустимый диапазон дат",
                    "Ошибка выбора дат", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DateTime currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка истории показаний PLC\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            CommonReports cr = new CommonReports("История показаний PLC");
            int rowcount = 0;//номер последней строки на листе. Нужен чтобы не было перезаписи при следующей итерации по концентраторам
            Invoke(new Action(delegate { ProgressBar.Value = 0; ProgressBar.Maximum = TVTaskPLC.Nodes.Count; }));
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                foreach (TreeNode concentrator in connection.Nodes)
                {
                    IConcentrator concentratorTag = (IConcentrator)concentrator.Tag;
                    foreach (TreeNode counter in concentrator.Nodes)
                    {
                        if (globalWorkerExportToExcel.CancellationPending == true) { cr.DisposeExcel(); return; }
                        Invoke(new Action(delegate {
                            LoadNodeData(counter);//грузим счётчик чтобы получить грид энергии
                        }));
                        cr.ExportEnergyPLC(rowcount, DeviceEnergyGrid, counter.Text + " (" + concentratorTag.NetAddress + ")");//передаём в процедуру номер последней строки и грид энергии
                        rowcount += DeviceEnergyGrid.Rows.Count;//наращиваем номер последней строки 
                    }
                }
                Invoke(new Action(delegate { ProgressBar.Value += 1; }));
            }
            Invoke(new Action(delegate
            {
                currentDate = DateTime.Now;
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Выгрузка истории показаний PLC завершена\r");
                ProgrammLogEdit.ScrollToCaret();
            }));
            cr.OpenAfterExport();
        }

        private void DeleteEnergyRow_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Раздел в разработке", "Ничего не выйдет", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LeftCountersWithIncompleteProfileButt_Click(object sender, EventArgs e)
        {
            LeftCountersWithIncompleteProfile();
        }

        private void LeftCountersWithIncompleteProfile()
        {//в этой процедуре из задания удаляются счётчики в которых профиль за указанный период полный
            DisableControls();
            for (int i = 0; i < TVTask485.Nodes.Count; i++)
            {
                try
                {
                    for (int j = 0; j < TVTask485.Nodes[i].Nodes.Count; j++)
                    {
                        LoadNodeData(TVTask485.Nodes[i].Nodes[j]);
                        if (PeriodsPresenceLabel.ForeColor == Color.Green)
                        {//если список пустой, то удаляем счётчик из задания, т.к. все отмеченные в нём виды энергии были успешно считаны ранее
                            RemoveNodeFromTask(TVTask485.Nodes[i].Nodes[j]); j -= 1;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            EnableControls();
        }

        private void показатьКоличествоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int quantityConnections = 0;
            int quantityConcentrators = 0;
            int quantityCounters = 0;

            quantityConnections += TVTaskPLC.Nodes.Count;
            foreach (TreeNode connection in TVTaskPLC.Nodes)
            {
                quantityConcentrators += connection.Nodes.Count;
                foreach (TreeNode concetrator in connection.Nodes)
                {
                    quantityCounters += concetrator.Nodes.Count;
                }
            }

            MessageBox.Show("Количество подключений: " + quantityConnections.ToString() +
                    "; Количество концентраторов: " + quantityConcentrators.ToString() +
                    "; Количество счётчиков: " + quantityCounters.ToString(), "Количество объектов в задании", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReadEnergyPage_Enter(object sender, EventArgs e)
        {

        }

        private void DeviceEnergyGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {//раскрываем историю показаний для счётчика RS-485 с учётом дат в контролах и виду энергии
            if (!(globalSelectedNode.Tag is ICounter) || globalSelectedNode.Tag.GetType() == typeof(MercuryPLC1)) { return; }

            try
            {
                EnergyHistoryGridForm ehgf = new EnergyHistoryGridForm(globalSelectedNode, DateTimeNEnergy, DateTimeKEnergy, DeviceEnergyGrid.Rows[e.RowIndex].Cells[3].Value.ToString());
                ehgf.Show();
            }
            catch
            {
                return;
            }
        }

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DisableControls();
                SearchNodes(SearchText.Text, true);
                EnableControls();
                this.ActiveControl = FullTree;
            }
        }

        private void ShowImagesButt_Click(object sender, EventArgs e)
        {
            ShowImages();//грузим картинки выбранного узла
        }

        private void FullTree_MouseEnter(object sender, EventArgs e)
        {
            if (FullTree.Cursor != Cursors.Default)
                FullTree.Cursor = Cursors.Default;
        }

        private void DragScroll(object sender, DragEventArgs e)
        {
            Utils.DragScroll(FullTree);
        }

        private void ChartViewCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadProfileFromDBButt.PerformClick();
        }

        private void FullTree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //приравниваем правый щелчок мыши к левому
            if (e.Button == MouseButtons.Right) { FullTree.SelectedNode = FullTree.GetNodeAt(e.X, e.Y); }
        }

        private void TVTask485_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //приравниваем правый щелчок мыши к левому
            if (e.Button == MouseButtons.Right) { TVTask485.SelectedNode = TVTask485.GetNodeAt(e.X, e.Y); }
        }

        private void TVTaskPLC_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //приравниваем правый щелчок мыши к левому
            if (e.Button == MouseButtons.Right) { TVTaskPLC.SelectedNode = TVTaskPLC.GetNodeAt(e.X, e.Y); }
        }

        private void MailButt_Click(object sender, EventArgs e)
        {
            //ReportsSender rs = new ReportsSender();
            //rs.SendMail(String.Empty, "gtr54", "yandex.ru","123");
        }

        private void ReReadAbsentRecords_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void GenerateForBQuarkButt_Click(object sender, EventArgs e)
        {
            GenerateForBQuark();
        }

        private void GenerateForBQuark()
        {
            DisableControls();

            ProgrammLogEdit.Clear();
            ProgrammLogEdit.AppendText("OBJECTS\r");
            ProgrammLogEdit.AppendText("\tTYPE=GSM_TERMINAL; BAUDRATE=9600\r");

            string connection_name = String.Empty;
            string connection_type = String.Empty;
            string concentrator_addr = String.Empty;

            foreach (TreeNode connection in TVTaskPLC.Nodes)//подключения
            {
                LoadNodeData(connection);
                connection_name = DevicePropertiesGrid.Rows[0].Cells[1].Value.ToString();
                if (connection.Tag.GetType() == typeof(Mercury228)) connection_type = "GSM_GATE";
                if (connection.Tag.GetType() == typeof(Modem)) connection_type = "GSM_MODEM";
                ProgrammLogEdit.AppendText("\t\tTYPE=" + connection_type + "; NUMBER=" + DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString() + "\r");
                foreach (TreeNode concentrator in connection.Nodes)//концентраторы
                {
                    LoadNodeData(concentrator);
                    concentrator_addr = DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString();
                    ProgrammLogEdit.AppendText("\t\t\tTYPE=PLC_I_CONCENTRATOR" + "; ADDR=" + concentrator_addr + "\r");
                    ProgrammLogEdit.AppendText("\t\t\t\tTYPE=COMMAND; RUN=SET_TIMEDATE\r");
                    foreach (TreeNode counter in concentrator.Nodes)//счётчики
                    {
                        LoadNodeData(counter);
                        MercuryPLC1 c = (MercuryPLC1)counter.Tag;
                        DataTable dt = DataBaseManagerMSSQL.Return_CounterPLC_Row(c.ID);//тащим данные счётчика из базы чтобы получить последние показания и дату последних показаний
                        //нужно сформировать строку с последними показаниями и датой последних показаний согласно формату бикварка
                        StringBuilder last_energy_string = new StringBuilder();
                        DateTime d = new DateTime();
                        try
                        {
                           d = DateTime.Parse(dt.Rows[13][1].ToString());
                        }
                        catch
                        {
                            continue;
                        }

                        string sum = dt.Rows[8][1].ToString().TrimEnd('0').TrimEnd(',').PadLeft(8, '0');

                        last_energy_string.Append("SUM-" + d.ToString("yyyy.MM.dd-HHmm"));                       
                        last_energy_string.Append("=" + sum);

                        ProgrammLogEdit.AppendText("\t\t\t\tTYPE=PLC_I_METER" + "; ADDR=" + DevicePropertiesGrid.Rows[4].Cells[1].Value.ToString() + "; ТП=" + connection_name + "; Улица="
                            + DevicePropertiesGrid.Rows[1].Cells[1].Value.ToString() + "; Дом=" + DevicePropertiesGrid.Rows[2].Cells[1].Value.ToString() +
                            "; №счетчика=" + DevicePropertiesGrid.Rows[3].Cells[1].Value.ToString() + "; Конц.=" + concentrator_addr + "; DEXT=YES; " + last_energy_string + "\r");
                    }
                }
            }

            ProgrammLogEdit.AppendText("INTERFACE\r");
            ProgrammLogEdit.AppendText("\tTYPE=TABLE\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=20; TITLE=№; VALUE=NUM\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=40; TITLE=PLC-адрес; VALUE=PROPERTY; FILTER=ADDR\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=50; TITLE=ТП; VALUE=PROPERTY; FILTER=ТП\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=100; TITLE=Улица; VALUE=PROPERTY; FILTER=Улица\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=40; TITLE=Дом; VALUE=PROPERTY; FILTER=Дом\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=100; TITLE=№счетчика; VALUE=PROPERTY; FILTER=№счетчика\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=40; TITLE=Конц.; VALUE=PROPERTY; FILTER=Конц.\r");

            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=0\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-1\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-2\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-3\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-4\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-5\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-6\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-7\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-8\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-9\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-10\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-11\r");
            ProgrammLogEdit.AppendText("\t\tTYPE=COLUMN; WIDTH=150; TITLE=Показания счетчика; VALUE=BINDATA; FILTER=SUM; M=-12\r");

            EnableControls();
        }

        private void ReLoadPhotos()
        {
            //цикл по ТП

            foreach (TreeNode node in FullTree.Nodes)
            {
                try
                {
                    globalSelectedNode = node;
                    ShowImages();//выводим изображения
                    if (ImageFlow.Controls.Count != 0)//если есть фотки
                    {
                        //цикл по фоткам
                        int p = 1;
                        foreach (Control ctrl in ImageFlow.Controls)
                        {
                            Thread.Sleep(100);
                            ToolStripMenuItem tsmi = (ToolStripMenuItem)ctrl.ContextMenuStrip.Items[0];
                            //здесь увеличиваем картинку по клику
                            PictureBox pb = (PictureBox)ctrl;
                            PhotoForm pf = new PhotoForm();
                            Image bp = pb.Image;
                            IDevice device = (IDevice)globalSelectedNode.Tag;
                            string filename = "Foto\\" + node.Text.Substring(0, node.Text.IndexOf('\\') - 1) + "_" + p.ToString() + ".jpg";
                            bp.Save(filename, ImageFormat.Jpeg);
                            DataBaseManagerMSSQL.Delete_Image((int)tsmi.Tag);//грохаем старую картинку из базы
                            DataBaseManagerMSSQL.Add_Image(device.ID, filename);//добавляем по-новой
                            p += 1;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private void ReLoadPhotos2()
        {
            //цикл по ТП
            foreach (TreeNode node in FullTree.Nodes)
            {
                try
                {
                    globalSelectedNode = node;
                    ShowImages();//выводим изображения
                    if (ImageFlow.Controls.Count != 0)//если есть фотки
                    {
                        //цикл по фоткам
                        int p = 1;
                        foreach (Control ctrl in ImageFlow.Controls)
                        {
                            Thread.Sleep(100);
                            ToolStripMenuItem tsmi = (ToolStripMenuItem)ctrl.ContextMenuStrip.Items[0];
                            //здесь увеличиваем картинку по клику
                            PictureBox pb = (PictureBox)ctrl;
                            PhotoForm pf = new PhotoForm();
                            Image bp = pb.Image;
                            IDevice device = (IDevice)globalSelectedNode.Tag;
                            DataBaseManagerMSSQL.Delete_Image((int)tsmi.Tag);//грохаем старую картинку из базы
                            DataBaseManagerMSSQL.Add_Image(device.ID, bp);//добавляем по-новой
                            p += 1;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private void ReLoadPhotosButt_Click(object sender, EventArgs e)
        {
            ReLoadPhotos2();
        }

        private void DevicePropertiesGrid_KeyDown_1(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
            //{
            //    DataGridViewCell cell = DevicePropertiesGrid.SelectedCells[1];
            //    cell.
            //    ApplyChangesButt.PerformClick();
            //}
        }

        private void DevicePropertiesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            //
        }

        private void SaveConfToDB_Click(object sender, EventArgs e)
        {
            UpdateObject();
        }

        private void CreateObjectButt_Click(object sender, EventArgs e)
        {

        }

        private void TWMainRefreshButt_CheckStateChanged(object sender, EventArgs e)
        {

        }

        private void WriteDateTimeBoxConc_ValueChanged(object sender, EventArgs e)
        {
            //при изменений этой даты откатываем день и месяц на 1. Это связано с тем, что при считывании показаний с концентратора день и месяц в дате показаний приходят на 1 меньше и 
            //это обрабатывается программой. Таким образом чтобы при дистанционной записи даты в концетратор показания от счётчика зафиксировались правильно, нужно записать дату на 1 меньше
            //WriteDateTimeBoxConc.Value.AddDays(-1);
            //WriteDateTimeBoxConc.Value.AddMonths(-1);
        }

        private void ObjectsAndSearchFormButt_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            selectedNodesListClear(selectedNodesListGlobal);
            SearchNodesDelegate del = new SearchNodesDelegate(SearchNodes);
            ObjectsListsForm olf = new ObjectsListsForm(del);
            olf.Show();
            Cursor.Current = Cursors.Arrow;
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

        private void ImportDataButt_Click(object sender, EventArgs e)
        {

        }

        private void ImportXLSIntoASKUE(string service_name, string username, string password, int id_source, string city)
        {//эта подпрограмма заливает экселевский файл в схему АСКУЭ на девятом оракле
            Cursor = Cursors.WaitCursor;
            DataBaseManagerOracle dbmo = new DataBaseManagerOracle("Data Source = " + service_name + ";  Password = " + password + "; User ID = " + username, ProgrammLogEdit);
            bool connect = dbmo.ConnectToDB();

            if (connect == false)
            {
                MessageBox.Show("Подключение к Oracle не удалось. Загрузка прервана.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cursor = Cursors.Default;
                return;
            }

            MessageBox.Show("Подключение к Oracle удалось. Выберите папку.", "Удачно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Cursor = Cursors.Default;

            FolderBrowserDialog fbd = new FolderBrowserDialog();
          
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("Папка выбрана. Начинаем загрузку файлов в базу данных", "Удачно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                string[] files = Directory.GetFiles(fbd.SelectedPath);//получаем список путей к файлам

                foreach (string file in files)
                {//циклимся по списку путей файлов и заливаем по-одному
                    DateTime currentDate = DateTime.Now;
                    ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Загружаем файл "+ Path.GetFileName(file) + "\r");                    
                    ProgrammLogEdit.ScrollToCaret();

                    Cursor = Cursors.WaitCursor;
                    CommonReports cr = new CommonReports();
                    cr.ImportXLSIntoASKUE(file, this.ProgressBar, dbmo, ProgrammLogEdit, id_source, city);
                    
                    Cursor = Cursors.Default;
                }               
            }
            MessageBox.Show("Загрузка файлов завершена", "Загрузка файлов завершена", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ServNameTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_3(object sender, EventArgs e)
        {

        }

        private void button1_Click_4(object sender, EventArgs e)
        {
            try
            {
                using (SmtpClient smtp = new SmtpClient("lxmail.skek.ru", 465))
                {
                    using (MailMessage mail = new MailMessage())
                    {
                        mail.To.Add("gtr54@yandex.ru");
                        mail.From = new MailAddress("protasov@skek.ru");
                        mail.Subject = "School Name";
                        mail.Body = "12345";

                        smtp.Timeout = 2000;
                        smtp.Credentials = new System.Net.NetworkCredential("protasov@skek.ru", "y8Fr_I9uU");
                        //////smtp.UseDefaultCredentials = true;
                        //smtp.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.EnableSsl = true;
                        smtp.SendCompleted += new SendCompletedEventHandler(Smtp_SendCompleted);

                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Smtp_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            MessageBox.Show("123456");
        }

        private void Create1024Counters_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode.Tag.GetType() != typeof(Mercury225PLC1))
            {
                return;
            }

            this.Create1024PLCCounters(globalSelectedNode);
        }

        private void RMSKemButt_Click(object sender, EventArgs e)
        {
            ImportXLSIntoASKUE("orcl", "askue", "askue", 4, "Kem");
        }

        private void RMSBerButt_Click(object sender, EventArgs e)
        {
            ImportXLSIntoASKUE("orclber", "askue", "askue", 5, "Ber");
        }

        private void ManualDialBegin(string text)
        {//здесь включаем кнопки, которые должны быть доступны после ручного дозвона
            this.Invoke(new Action(delegate
            {
                GetProfileButt.Enabled = true;
                GetMonitorButt.Enabled = true;
                WriteParametersButt.Enabled = true;
                ManualConnectionLabel.ForeColor = Color.Red;
                ManualConnectionLabel.Text = text;
            }));
        }

        private void ManualDialEnded(string text)
        {//здесь отключаем кнопки, которые должны быть недоступны после окончания ручного дозвона
            this.Invoke(new Action(delegate
            {
                GetProfileButt.Enabled = false;
                GetMonitorButt.Enabled = false;
                WriteParametersButt.Enabled = false;
                ManualConnectionLabel.ForeColor = this.ForeColor;
                ManualConnectionLabel.Text = text;
            }));
        }

        private void ManualDial()
        {
            //здесь просто звоним на соединение (модем  или шлюз)
            if (!(globalSelectedNode.Tag is IConnection))
            {
                return;
            }
            this.Invoke(new Action(delegate
            {
                DisableControls();
            }));
            var connection = (IConnection)globalSelectedNode.Tag;//
            this.Invoke(new Action(delegate
            {
                DialButt.Enabled = false;
                StopDialButt.Enabled = true;
                DialButt.BackColor = Color.Red;
                StopDialButt.BackColor = Color.LightGreen;
            }));
            DateTime currentDateA = DateTime.Now;
            DataProcessing dp = new DataProcessing(this.global_sp, notifyicon);
            this.global_sp = InitModem(dp, 9600, this.global_sp, ProgrammLogEdit);
            if (this.global_sp == null)
            {
                DateTime currentDateQ = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDateQ + "." + currentDateQ.Millisecond + " Ошибка инициализации модема. Прекращаем опрос\r");
                    ProgrammLogEdit.ScrollToCaret();
                    EnableControls();
                }));
                //если вылетела ошибка инициализации модема, то нужно вернуть контролы к исходному состоянию
                DialButt.Enabled = true;
                StopDialButt.Enabled = false;
                DialButt.BackColor = Color.LightGreen;
                StopDialButt.BackColor = Color.Red;

                return;
            }
            string workingPort = this.global_sp.PortName; //инициализируем модем и возвращаем рабочий порт
            string cbst_str = String.Empty; string phone_number = String.Empty; string node_name = String.Empty;

            cbst_str = connection.CBST;
            phone_number = connection.Phone;
            node_name = connection.Name;
            //строка инициализации модема берётся из настроек подключения
            if (SendTextToPort("at+cbst=" + cbst_str, "OK", workingPort, false, dp, 200, ProgrammLogEdit) == false)
            {
                EnableControls();
                return;
            }
            //делаем несколько попыток дозвона
            bool connect = false;
            for (int i = 1; i <= 3; i++)
            {
                if (SendTextToPort("atd" + phone_number, "CONNECT", workingPort, false, dp, 60000, ProgrammLogEdit) == false)
                {  //дозвонились или нет?
                    DateTime currentDateC = DateTime.Now;
                    Invoke(new Action(delegate
                    {
                        ProgrammLogEdit.AppendText(currentDateC + "." + currentDateC.Millisecond + " Соединение с " + node_name + " не установлено\r");
                        ProgrammLogEdit.ScrollToCaret();
                    }));
                    Thread.Sleep(5000);//ждём перед следующей попыткой дозвона
                    continue;
                }
                else
                {//если дозвонились, то выходим из цикла
                    connect = true;
                    break;
                }
            }
            //если не дозвонились, то возвращаем управление программе
            if (connect == false)
            {
                DateTime currentDateC = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    ProgrammLogEdit.AppendText(currentDateC + "." + currentDateC.Millisecond + " Попытки соединения с " + node_name + " исчерпаны\r");
                    ProgrammLogEdit.ScrollToCaret();

                    DialButt.Enabled = true;
                    StopDialButt.Enabled = false;
                    DialButt.BackColor = Color.LightGreen;
                    StopDialButt.BackColor = Color.Red;
                    Thread.Sleep(1000);
                    EnableControls();
                }));
                
                return;
            }
            DateTime currentDate = DateTime.Now;
            Invoke(new Action(delegate
            {
                ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Соединение с " + node_name + " установлено\r");
                ProgrammLogEdit.ScrollToCaret();
                Thread.Sleep(1000);
                EnableControls();
            }));
            
            ManualDialBegin("Связь с " + globalSelectedNode.Text);
        }

        private void DialButt_Click(object sender, EventArgs e)
        {
            ManualDial();         
        }

        private void StopDial()
        {
            //здесь кладём трубку
            DataProcessing dp = new DataProcessing(global_sp, notifyicon);
            StopCall(dp, global_sp.PortName, ProgrammLogEdit);

            DialButt.Enabled = true;
            StopDialButt.Enabled = false;
            DialButt.BackColor = Color.LightGreen;
            StopDialButt.BackColor = Color.Red;

            dp.ClosePort();
            dp = null;
            ManualDialEnded("Ручной опрос ВЫКЛ");
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //GetScreenFormInfo();
            //this.Location = Properties.Settings.Default.MainFormLocation;
            //this.Size = Properties.Settings.Default.MainFormSize;
        }

        private void StopDialButt_Click(object sender, EventArgs e)
        {
            StopDial();
        }

        private void MonitorButtLayout_Paint(object sender, PaintEventArgs e)
        {

        }

        private void DeleteExceptRSButt_Click(object sender, EventArgs e)
        {
            TVTask485.Nodes.Clear();
            GoToTask();
            //TVTask485.ExpandAll();
        }

        private void DeleteAllExceptPLCButt_Click(object sender, EventArgs e)
        {
            TVTaskPLC.Nodes.Clear();
            GoToTask();
            //TVTaskPLC.ExpandAll();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {//при попытке закрыть главную форму (т.е. всё приложение) нужно проверить, есть ли незавершённые потоки чтения или незакрытый глобальный порт.
            if (global_sp != null)//если глобальный порт существует (ссылка на него) 
            {
                if (global_sp.IsOpen) { e.Cancel = true; }
            }

            FormCollection fc = Application.OpenForms;//получаем текущую коллекцию форм

            foreach (Form f in fc)//циклимся по коллекции форм программы
            {
                if (f.GetType() == typeof(ReadingLogForm))
                {
                    e.Cancel = true;
                    //ReadingLogForm rlf = (ReadingLogForm)f;//получаем экземпляр формы
                    //BackgroundWorker bgw = rlf.bgrw;//получаем экземпляр потока чтения
                    //смотрим, занят ли ещё поток чтения или нет
                    //if (bgw.IsBusy == true) { }//если поток чтения ещё работает, то закрывать глауню форму нельзя
                }
            }
        }

        private void DevicePropertiesGrid_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
            //{
            //    ApplyChangesButt.PerformClick();
            //}
        }

        private void DevicePropertiesGrid_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            //MessageBox.Show("1234");
        }

        private void DevicePropertiesGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
           
            ApplyChangesButt.PerformClick();
        }

        private void SelectAllJournalCQCButt_Click(object sender, EventArgs e)
        {
            selectAllParams(DeviceJournalCQCGrid, true, globalSelectedNode);
        }

        private void DeselectAllJournalCQCButt_Click(object sender, EventArgs e)
        {
            selectAllParams(DeviceJournalCQCGrid, false, globalSelectedNode);
        }

        private void DeviceJournalCQCGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!(globalSelectedNode.Tag is ICounter) || globalSelectedNode.Tag.GetType() == typeof(MercuryPLC1)) { return; }

            try
            {
                JournalCQCGridForm jcgf = new JournalCQCGridForm(globalSelectedNode, DeviceJournalCQCGrid.Rows[e.RowIndex].Cells[3].Value.ToString());
                jcgf.Show();
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private void ленинскToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportXLSIntoASKUE("orcllen", "askue", "askue", 4, "Len");
        }

        private void ReadBQuarkFileButt_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();           
            //открываем файл задания для бикварка. Нужно его проанализировать
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            DisableControls();//отключаем контролы
            
            ofd.Filter = "Файлы задания (*.dat)|*.dat";
            Cursor = Cursors.WaitCursor;

            ProgrammLogEdit.Clear();
            ProgrammLogEdit.LoadFile(ofd.FileName, RichTextBoxStreamType.PlainText);

            selectedNodesListClear(selectedNodesListGlobal); //очищаем текущий список выбранных узлов перед поиском

            //циклимся по строкам файла          
            for (int i = 0; i < ProgrammLogEdit.Lines.Count(); i++)
            {
                char[] ser_num = new char[8];
                string line = ProgrammLogEdit.Lines[i];

                int pos = line.IndexOf('№');
              
                if (pos == -1)
                {
                    continue;
                }
                
                int abvgd = line.IndexOf(';', pos);
                try//на случай если возникнет исключение ArgumentOutOfRangeException (серийный номер будет длиннее 8 символов)
                {
                    line.CopyTo(pos + 11, ser_num, 1, abvgd - pos - 11);
                }
                catch
                {
                    continue;
                }
                string s = new string(ser_num);
                s = s.Substring(1);
                SearchNodes(s, false);
            }

            EnableControls();//включаем контролы
            TVTaskPLC.Nodes.Clear();//очищаем дерево задания
            GoToTaskButt.PerformClick();//переносим узлы в задание
            selectedNodesListClear(selectedNodesListGlobal); //очищаем текущий список выбранных узлов после поиска
            Cursor = Cursors.Default;
        }

        private void исправитьОшибкуБИВToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("У текущей группы счётчиков будет изменена дата последних показаний = текущая дата - 1 день.", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No) return;

            DateTime currentDate = DateTime.Now;
            ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Корректировка даты ПП\r");
            ProgrammLogEdit.ScrollToCaret();

            ProgressBar.Value = 0;
            ProgressBar.Maximum = TVTaskPLC.Nodes.Count;

            DisableControls();
            FixPLCLastDate();
            EnableControls();

            ProgrammLogEdit.AppendText(currentDate + "." + currentDate.Millisecond + " Корректировка даты ПП завершена\r");
            ProgrammLogEdit.ScrollToCaret();
           
        }

        private void ShowChartButt_Click(object sender, EventArgs e)
        {
            if (globalSelectedNode.Tag.GetType() != typeof(MercuryPLC1))
            {
                return;
            }

            Cursor = Cursors.WaitCursor;

            MercuryPLC1 counter = (MercuryPLC1)globalSelectedNode.Tag;//экземпляр PLC-счётчика
            DataTable dt = DataBaseManagerMSSQL.Return_Counter_Energy_PLC_History(counter.SerialNumber, 2, DateTimeNEnergy.Value, DateTimeKEnergy.Value, "'Энергия, текущее потребление', 'Энергия, суточный срез'");

            if (dt.Rows.Count == 0)
            {
                Cursor = Cursors.Default;
                return;
            }

            DeviceEnergyGrid.DataSource = dt;
            Cursor = Cursors.Default;
            //выводим график показаний
            PLCChartForm pcf = new PLCChartForm(dt);
            pcf.Show();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.MainFormLocation = this.Location;
            Properties.Settings.Default.MainFormSize = this.Size;
            Properties.Settings.Default.Save();
        }
    }
}
