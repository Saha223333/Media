namespace NewProject
{
    partial class TasksForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TasksForm));
            this.TaskFormLayout = new System.Windows.Forms.TableLayoutPanel();
            this.TasksLabel = new System.Windows.Forms.Label();
            this.SchedulesLabel = new System.Windows.Forms.Label();
            this.SchedulesLayout = new System.Windows.Forms.TableLayoutPanel();
            this.SchedulesButtLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.CreateScheduleButt = new System.Windows.Forms.Button();
            this.UpdateScheduleButt = new System.Windows.Forms.Button();
            this.DeleteScheduleButt = new System.Windows.Forms.Button();
            this.SchedulesGrid = new System.Windows.Forms.DataGridView();
            this.ReportsLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ReportsLabel = new System.Windows.Forms.Label();
            this.ReportTypesComboBox = new System.Windows.Forms.ComboBox();
            this.ReportsGrid = new System.Windows.Forms.DataGridView();
            this.FlowReportsButtLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.AddReportToTaskButt = new System.Windows.Forms.Button();
            this.DeleteReportFromTasKbutt = new System.Windows.Forms.Button();
            this.TaskTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.TaskButtsLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.LoadTaskButt = new System.Windows.Forms.Button();
            this.UpdateTaskButt = new System.Windows.Forms.Button();
            this.DeleteTaskButt = new System.Windows.Forms.Button();
            this.CloseFormButt = new System.Windows.Forms.Button();
            this.TasksDataGridView = new System.Windows.Forms.DataGridView();
            this.EMailTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.EMailGirdView = new System.Windows.Forms.DataGridView();
            this.EMailLabel = new System.Windows.Forms.Label();
            this.EMailButtsLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.EMailRecieverTextBox = new System.Windows.Forms.TextBox();
            this.CreateEMailRecieverButt = new System.Windows.Forms.Button();
            this.DeleteEMailRecieverButt = new System.Windows.Forms.Button();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.TaskFormLayout.SuspendLayout();
            this.SchedulesLayout.SuspendLayout();
            this.SchedulesButtLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SchedulesGrid)).BeginInit();
            this.ReportsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ReportsGrid)).BeginInit();
            this.FlowReportsButtLayout.SuspendLayout();
            this.TaskTableLayout.SuspendLayout();
            this.TaskButtsLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TasksDataGridView)).BeginInit();
            this.EMailTableLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EMailGirdView)).BeginInit();
            this.EMailButtsLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // TaskFormLayout
            // 
            this.TaskFormLayout.ColumnCount = 2;
            this.TaskFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TaskFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TaskFormLayout.Controls.Add(this.TasksLabel, 0, 0);
            this.TaskFormLayout.Controls.Add(this.SchedulesLabel, 1, 0);
            this.TaskFormLayout.Controls.Add(this.SchedulesLayout, 1, 1);
            this.TaskFormLayout.Controls.Add(this.ReportsLayout, 1, 2);
            this.TaskFormLayout.Controls.Add(this.TaskTableLayout, 0, 1);
            this.TaskFormLayout.Controls.Add(this.EMailTableLayout, 0, 2);
            this.TaskFormLayout.Controls.Add(this.ProgressBar, 0, 3);
            this.TaskFormLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TaskFormLayout.Location = new System.Drawing.Point(0, 0);
            this.TaskFormLayout.Name = "TaskFormLayout";
            this.TaskFormLayout.RowCount = 4;
            this.TaskFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4.999999F));
            this.TaskFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.TaskFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.TaskFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.TaskFormLayout.Size = new System.Drawing.Size(1334, 721);
            this.TaskFormLayout.TabIndex = 0;
            // 
            // TasksLabel
            // 
            this.TasksLabel.AutoSize = true;
            this.TasksLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TasksLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TasksLabel.Location = new System.Drawing.Point(3, 0);
            this.TasksLabel.Name = "TasksLabel";
            this.TasksLabel.Size = new System.Drawing.Size(661, 36);
            this.TasksLabel.TabIndex = 2;
            this.TasksLabel.Text = "Задания";
            this.TasksLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SchedulesLabel
            // 
            this.SchedulesLabel.AutoSize = true;
            this.SchedulesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SchedulesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.SchedulesLabel.Location = new System.Drawing.Point(670, 0);
            this.SchedulesLabel.Name = "SchedulesLabel";
            this.SchedulesLabel.Size = new System.Drawing.Size(661, 36);
            this.SchedulesLabel.TabIndex = 3;
            this.SchedulesLabel.Text = "Расписания";
            this.SchedulesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SchedulesLayout
            // 
            this.SchedulesLayout.ColumnCount = 1;
            this.SchedulesLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.SchedulesLayout.Controls.Add(this.SchedulesButtLayout, 0, 1);
            this.SchedulesLayout.Controls.Add(this.SchedulesGrid, 0, 0);
            this.SchedulesLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SchedulesLayout.Location = new System.Drawing.Point(670, 39);
            this.SchedulesLayout.Name = "SchedulesLayout";
            this.SchedulesLayout.RowCount = 2;
            this.SchedulesLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.SchedulesLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.SchedulesLayout.Size = new System.Drawing.Size(661, 354);
            this.SchedulesLayout.TabIndex = 4;
            // 
            // SchedulesButtLayout
            // 
            this.SchedulesButtLayout.Controls.Add(this.CreateScheduleButt);
            this.SchedulesButtLayout.Controls.Add(this.UpdateScheduleButt);
            this.SchedulesButtLayout.Controls.Add(this.DeleteScheduleButt);
            this.SchedulesButtLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SchedulesButtLayout.Location = new System.Drawing.Point(3, 321);
            this.SchedulesButtLayout.Name = "SchedulesButtLayout";
            this.SchedulesButtLayout.Size = new System.Drawing.Size(655, 30);
            this.SchedulesButtLayout.TabIndex = 10;
            // 
            // CreateScheduleButt
            // 
            this.CreateScheduleButt.Location = new System.Drawing.Point(3, 3);
            this.CreateScheduleButt.Name = "CreateScheduleButt";
            this.CreateScheduleButt.Size = new System.Drawing.Size(75, 23);
            this.CreateScheduleButt.TabIndex = 5;
            this.CreateScheduleButt.Text = "Создать";
            this.CreateScheduleButt.UseVisualStyleBackColor = true;
            this.CreateScheduleButt.Click += new System.EventHandler(this.CreateScheduleButt_Click);
            // 
            // UpdateScheduleButt
            // 
            this.UpdateScheduleButt.Location = new System.Drawing.Point(84, 3);
            this.UpdateScheduleButt.Name = "UpdateScheduleButt";
            this.UpdateScheduleButt.Size = new System.Drawing.Size(75, 23);
            this.UpdateScheduleButt.TabIndex = 6;
            this.UpdateScheduleButt.Text = "Сохранить";
            this.UpdateScheduleButt.UseVisualStyleBackColor = true;
            this.UpdateScheduleButt.Click += new System.EventHandler(this.UpdateScheduleButt_Click);
            // 
            // DeleteScheduleButt
            // 
            this.DeleteScheduleButt.Location = new System.Drawing.Point(165, 3);
            this.DeleteScheduleButt.Name = "DeleteScheduleButt";
            this.DeleteScheduleButt.Size = new System.Drawing.Size(75, 23);
            this.DeleteScheduleButt.TabIndex = 7;
            this.DeleteScheduleButt.Text = "Удалить";
            this.DeleteScheduleButt.UseVisualStyleBackColor = true;
            this.DeleteScheduleButt.Click += new System.EventHandler(this.DeleteScheduleButt_Click);
            // 
            // SchedulesGrid
            // 
            this.SchedulesGrid.AllowUserToAddRows = false;
            this.SchedulesGrid.AllowUserToDeleteRows = false;
            this.SchedulesGrid.AllowUserToResizeRows = false;
            this.SchedulesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.SchedulesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SchedulesGrid.Location = new System.Drawing.Point(0, 0);
            this.SchedulesGrid.Margin = new System.Windows.Forms.Padding(0);
            this.SchedulesGrid.MultiSelect = false;
            this.SchedulesGrid.Name = "SchedulesGrid";
            this.SchedulesGrid.RowHeadersVisible = false;
            this.SchedulesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.SchedulesGrid.Size = new System.Drawing.Size(661, 318);
            this.SchedulesGrid.TabIndex = 1;
            this.SchedulesGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SchedulesGrid_CellClick);
            this.SchedulesGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.SchedulesGrid_DataError);
            this.SchedulesGrid.SelectionChanged += new System.EventHandler(this.SchedulesGrid_SelectionChanged);
            this.SchedulesGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SchedulesGrid_KeyDown);
            this.SchedulesGrid.Leave += new System.EventHandler(this.SchedulesGrid_Leave);
            // 
            // ReportsLayout
            // 
            this.ReportsLayout.ColumnCount = 2;
            this.ReportsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ReportsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ReportsLayout.Controls.Add(this.ReportsLabel, 0, 0);
            this.ReportsLayout.Controls.Add(this.ReportTypesComboBox, 0, 2);
            this.ReportsLayout.Controls.Add(this.ReportsGrid, 0, 1);
            this.ReportsLayout.Controls.Add(this.FlowReportsButtLayout, 1, 2);
            this.ReportsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReportsLayout.Location = new System.Drawing.Point(670, 399);
            this.ReportsLayout.Name = "ReportsLayout";
            this.ReportsLayout.RowCount = 3;
            this.ReportsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.ReportsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 77F));
            this.ReportsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.ReportsLayout.Size = new System.Drawing.Size(661, 282);
            this.ReportsLayout.TabIndex = 8;
            // 
            // ReportsLabel
            // 
            this.ReportsLabel.AutoSize = true;
            this.ReportsLayout.SetColumnSpan(this.ReportsLabel, 2);
            this.ReportsLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReportsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F);
            this.ReportsLabel.Location = new System.Drawing.Point(3, 0);
            this.ReportsLabel.Name = "ReportsLabel";
            this.ReportsLabel.Size = new System.Drawing.Size(655, 28);
            this.ReportsLabel.TabIndex = 0;
            this.ReportsLabel.Text = "Автовыгрузка отчётов";
            this.ReportsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ReportTypesComboBox
            // 
            this.ReportTypesComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReportTypesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ReportTypesComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ReportTypesComboBox.FormattingEnabled = true;
            this.ReportTypesComboBox.Items.AddRange(new object[] {
            "Выбранные параметры",
            "Интегральный акт",
            "Профили мощности"});
            this.ReportTypesComboBox.Location = new System.Drawing.Point(3, 248);
            this.ReportTypesComboBox.Name = "ReportTypesComboBox";
            this.ReportTypesComboBox.Size = new System.Drawing.Size(324, 21);
            this.ReportTypesComboBox.TabIndex = 1;
            // 
            // ReportsGrid
            // 
            this.ReportsGrid.AllowUserToAddRows = false;
            this.ReportsGrid.AllowUserToDeleteRows = false;
            this.ReportsGrid.AllowUserToResizeColumns = false;
            this.ReportsGrid.AllowUserToResizeRows = false;
            this.ReportsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ReportsLayout.SetColumnSpan(this.ReportsGrid, 2);
            this.ReportsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReportsGrid.Location = new System.Drawing.Point(0, 28);
            this.ReportsGrid.Margin = new System.Windows.Forms.Padding(0);
            this.ReportsGrid.MultiSelect = false;
            this.ReportsGrid.Name = "ReportsGrid";
            this.ReportsGrid.ReadOnly = true;
            this.ReportsGrid.RowHeadersVisible = false;
            this.ReportsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.ReportsGrid.Size = new System.Drawing.Size(661, 217);
            this.ReportsGrid.TabIndex = 3;
            // 
            // FlowReportsButtLayout
            // 
            this.FlowReportsButtLayout.Controls.Add(this.AddReportToTaskButt);
            this.FlowReportsButtLayout.Controls.Add(this.DeleteReportFromTasKbutt);
            this.FlowReportsButtLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FlowReportsButtLayout.Location = new System.Drawing.Point(330, 245);
            this.FlowReportsButtLayout.Margin = new System.Windows.Forms.Padding(0);
            this.FlowReportsButtLayout.Name = "FlowReportsButtLayout";
            this.FlowReportsButtLayout.Size = new System.Drawing.Size(331, 37);
            this.FlowReportsButtLayout.TabIndex = 4;
            // 
            // AddReportToTaskButt
            // 
            this.AddReportToTaskButt.Location = new System.Drawing.Point(3, 3);
            this.AddReportToTaskButt.Name = "AddReportToTaskButt";
            this.AddReportToTaskButt.Size = new System.Drawing.Size(75, 23);
            this.AddReportToTaskButt.TabIndex = 2;
            this.AddReportToTaskButt.Text = "Добавить";
            this.AddReportToTaskButt.UseVisualStyleBackColor = true;
            this.AddReportToTaskButt.Click += new System.EventHandler(this.AddReportToTaskButt_Click);
            // 
            // DeleteReportFromTasKbutt
            // 
            this.DeleteReportFromTasKbutt.Location = new System.Drawing.Point(84, 3);
            this.DeleteReportFromTasKbutt.Name = "DeleteReportFromTasKbutt";
            this.DeleteReportFromTasKbutt.Size = new System.Drawing.Size(75, 23);
            this.DeleteReportFromTasKbutt.TabIndex = 3;
            this.DeleteReportFromTasKbutt.Text = "Убрать";
            this.DeleteReportFromTasKbutt.UseVisualStyleBackColor = true;
            this.DeleteReportFromTasKbutt.Click += new System.EventHandler(this.DeleteReportFromTasKbutt_Click);
            // 
            // TaskTableLayout
            // 
            this.TaskTableLayout.ColumnCount = 1;
            this.TaskTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.TaskTableLayout.Controls.Add(this.TaskButtsLayout, 0, 1);
            this.TaskTableLayout.Controls.Add(this.TasksDataGridView, 0, 0);
            this.TaskTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TaskTableLayout.Location = new System.Drawing.Point(3, 39);
            this.TaskTableLayout.Name = "TaskTableLayout";
            this.TaskTableLayout.RowCount = 2;
            this.TaskTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.TaskTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.TaskTableLayout.Size = new System.Drawing.Size(661, 354);
            this.TaskTableLayout.TabIndex = 10;
            // 
            // TaskButtsLayout
            // 
            this.TaskButtsLayout.Controls.Add(this.LoadTaskButt);
            this.TaskButtsLayout.Controls.Add(this.UpdateTaskButt);
            this.TaskButtsLayout.Controls.Add(this.DeleteTaskButt);
            this.TaskButtsLayout.Controls.Add(this.CloseFormButt);
            this.TaskButtsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TaskButtsLayout.Location = new System.Drawing.Point(3, 321);
            this.TaskButtsLayout.Name = "TaskButtsLayout";
            this.TaskButtsLayout.Size = new System.Drawing.Size(661, 30);
            this.TaskButtsLayout.TabIndex = 9;
            // 
            // LoadTaskButt
            // 
            this.LoadTaskButt.Location = new System.Drawing.Point(3, 3);
            this.LoadTaskButt.Name = "LoadTaskButt";
            this.LoadTaskButt.Size = new System.Drawing.Size(76, 23);
            this.LoadTaskButt.TabIndex = 2;
            this.LoadTaskButt.Text = "Загрузить";
            this.LoadTaskButt.UseVisualStyleBackColor = true;
            this.LoadTaskButt.Click += new System.EventHandler(this.LoadTaskButt_Click);
            // 
            // UpdateTaskButt
            // 
            this.UpdateTaskButt.Location = new System.Drawing.Point(85, 3);
            this.UpdateTaskButt.Name = "UpdateTaskButt";
            this.UpdateTaskButt.Size = new System.Drawing.Size(123, 23);
            this.UpdateTaskButt.TabIndex = 5;
            this.UpdateTaskButt.Text = "Сохранить/заменить";
            this.UpdateTaskButt.UseVisualStyleBackColor = true;
            this.UpdateTaskButt.Click += new System.EventHandler(this.UpdateTask_Click);
            // 
            // DeleteTaskButt
            // 
            this.DeleteTaskButt.Location = new System.Drawing.Point(214, 3);
            this.DeleteTaskButt.Name = "DeleteTaskButt";
            this.DeleteTaskButt.Size = new System.Drawing.Size(75, 23);
            this.DeleteTaskButt.TabIndex = 6;
            this.DeleteTaskButt.Text = "Удалить";
            this.DeleteTaskButt.UseVisualStyleBackColor = true;
            this.DeleteTaskButt.Click += new System.EventHandler(this.DeleteTaskButt_Click);
            // 
            // CloseFormButt
            // 
            this.CloseFormButt.Location = new System.Drawing.Point(295, 3);
            this.CloseFormButt.Name = "CloseFormButt";
            this.CloseFormButt.Size = new System.Drawing.Size(81, 23);
            this.CloseFormButt.TabIndex = 4;
            this.CloseFormButt.Text = "Выход";
            this.CloseFormButt.UseVisualStyleBackColor = true;
            this.CloseFormButt.Click += new System.EventHandler(this.CloseFormButt_Click);
            // 
            // TasksDataGridView
            // 
            this.TasksDataGridView.AllowUserToAddRows = false;
            this.TasksDataGridView.AllowUserToDeleteRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.TasksDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.TasksDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.TasksDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
            this.TasksDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TasksDataGridView.Location = new System.Drawing.Point(0, 0);
            this.TasksDataGridView.Margin = new System.Windows.Forms.Padding(0);
            this.TasksDataGridView.MultiSelect = false;
            this.TasksDataGridView.Name = "TasksDataGridView";
            this.TasksDataGridView.RowHeadersWidth = 23;
            this.TasksDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.TasksDataGridView.Size = new System.Drawing.Size(667, 318);
            this.TasksDataGridView.TabIndex = 0;
            this.TasksDataGridView.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.TasksDataGridView_CellEndEdit);
            this.TasksDataGridView.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.TasksDataGridView_CellValidated);
            this.TasksDataGridView.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.TasksDataGridView_DataBindingComplete);
            this.TasksDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.TasksDataGridView_DataError);
            this.TasksDataGridView.SelectionChanged += new System.EventHandler(this.TasksDataGridView_SelectionChanged);
            this.TasksDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TasksDataGridView_KeyDown);
            // 
            // EMailTableLayout
            // 
            this.EMailTableLayout.ColumnCount = 1;
            this.EMailTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.EMailTableLayout.Controls.Add(this.EMailGirdView, 0, 1);
            this.EMailTableLayout.Controls.Add(this.EMailLabel, 0, 0);
            this.EMailTableLayout.Controls.Add(this.EMailButtsLayout, 0, 2);
            this.EMailTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EMailTableLayout.Location = new System.Drawing.Point(3, 399);
            this.EMailTableLayout.Name = "EMailTableLayout";
            this.EMailTableLayout.RowCount = 3;
            this.EMailTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.EMailTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 77F));
            this.EMailTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.EMailTableLayout.Size = new System.Drawing.Size(661, 282);
            this.EMailTableLayout.TabIndex = 11;
            // 
            // EMailGirdView
            // 
            this.EMailGirdView.AllowUserToAddRows = false;
            this.EMailGirdView.AllowUserToDeleteRows = false;
            this.EMailGirdView.AllowUserToResizeRows = false;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.EMailGirdView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.EMailGirdView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.EMailGirdView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EMailGirdView.Location = new System.Drawing.Point(0, 28);
            this.EMailGirdView.Margin = new System.Windows.Forms.Padding(0);
            this.EMailGirdView.Name = "EMailGirdView";
            this.EMailGirdView.RowHeadersVisible = false;
            this.EMailGirdView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.EMailGirdView.Size = new System.Drawing.Size(661, 217);
            this.EMailGirdView.TabIndex = 0;
            this.EMailGirdView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.EMailGirdView_CellClick);
            this.EMailGirdView.SelectionChanged += new System.EventHandler(this.EMailGirdView_SelectionChanged);
            // 
            // EMailLabel
            // 
            this.EMailLabel.AutoSize = true;
            this.EMailLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EMailLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F);
            this.EMailLabel.Location = new System.Drawing.Point(3, 0);
            this.EMailLabel.Name = "EMailLabel";
            this.EMailLabel.Size = new System.Drawing.Size(655, 28);
            this.EMailLabel.TabIndex = 1;
            this.EMailLabel.Text = "Почта";
            this.EMailLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // EMailButtsLayout
            // 
            this.EMailButtsLayout.Controls.Add(this.EMailRecieverTextBox);
            this.EMailButtsLayout.Controls.Add(this.CreateEMailRecieverButt);
            this.EMailButtsLayout.Controls.Add(this.DeleteEMailRecieverButt);
            this.EMailButtsLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EMailButtsLayout.Location = new System.Drawing.Point(3, 248);
            this.EMailButtsLayout.Name = "EMailButtsLayout";
            this.EMailButtsLayout.Size = new System.Drawing.Size(655, 31);
            this.EMailButtsLayout.TabIndex = 2;
            // 
            // EMailRecieverTextBox
            // 
            this.EMailRecieverTextBox.Location = new System.Drawing.Point(3, 3);
            this.EMailRecieverTextBox.Name = "EMailRecieverTextBox";
            this.EMailRecieverTextBox.Size = new System.Drawing.Size(286, 20);
            this.EMailRecieverTextBox.TabIndex = 0;
            // 
            // CreateEMailRecieverButt
            // 
            this.CreateEMailRecieverButt.Location = new System.Drawing.Point(295, 3);
            this.CreateEMailRecieverButt.Name = "CreateEMailRecieverButt";
            this.CreateEMailRecieverButt.Size = new System.Drawing.Size(75, 23);
            this.CreateEMailRecieverButt.TabIndex = 1;
            this.CreateEMailRecieverButt.Text = "Добавить";
            this.CreateEMailRecieverButt.UseVisualStyleBackColor = true;
            this.CreateEMailRecieverButt.Click += new System.EventHandler(this.CreateEMailRecieverButt_Click);
            // 
            // DeleteEMailRecieverButt
            // 
            this.DeleteEMailRecieverButt.Location = new System.Drawing.Point(376, 3);
            this.DeleteEMailRecieverButt.Name = "DeleteEMailRecieverButt";
            this.DeleteEMailRecieverButt.Size = new System.Drawing.Size(75, 23);
            this.DeleteEMailRecieverButt.TabIndex = 2;
            this.DeleteEMailRecieverButt.Text = "Удалить";
            this.DeleteEMailRecieverButt.UseVisualStyleBackColor = true;
            this.DeleteEMailRecieverButt.Visible = false;
            // 
            // ProgressBar
            // 
            this.ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgressBar.Location = new System.Drawing.Point(3, 687);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(661, 31);
            this.ProgressBar.Step = 1;
            this.ProgressBar.TabIndex = 12;
            // 
            // TasksForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1334, 721);
            this.Controls.Add(this.TaskFormLayout);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 600);
            this.Name = "TasksForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Задания";
            this.Load += new System.EventHandler(this.TasksForm_Load);
            this.Shown += new System.EventHandler(this.TasksForm_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TasksForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TasksForm_KeyUp);
            this.TaskFormLayout.ResumeLayout(false);
            this.TaskFormLayout.PerformLayout();
            this.SchedulesLayout.ResumeLayout(false);
            this.SchedulesButtLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SchedulesGrid)).EndInit();
            this.ReportsLayout.ResumeLayout(false);
            this.ReportsLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ReportsGrid)).EndInit();
            this.FlowReportsButtLayout.ResumeLayout(false);
            this.TaskTableLayout.ResumeLayout(false);
            this.TaskButtsLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TasksDataGridView)).EndInit();
            this.EMailTableLayout.ResumeLayout(false);
            this.EMailTableLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.EMailGirdView)).EndInit();
            this.EMailButtsLayout.ResumeLayout(false);
            this.EMailButtsLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TaskFormLayout;
        private System.Windows.Forms.DataGridView TasksDataGridView;
        private System.Windows.Forms.Label TasksLabel;
        private System.Windows.Forms.Label SchedulesLabel;
        private System.Windows.Forms.TableLayoutPanel SchedulesLayout;
        private System.Windows.Forms.DataGridView SchedulesGrid;
        private System.Windows.Forms.TableLayoutPanel ReportsLayout;
        private System.Windows.Forms.Label ReportsLabel;
        private System.Windows.Forms.ComboBox ReportTypesComboBox;
        private System.Windows.Forms.Button AddReportToTaskButt;
        private System.Windows.Forms.DataGridView ReportsGrid;
        private System.Windows.Forms.FlowLayoutPanel FlowReportsButtLayout;
        private System.Windows.Forms.Button DeleteReportFromTasKbutt;
        private System.Windows.Forms.FlowLayoutPanel TaskButtsLayout;
        private System.Windows.Forms.Button LoadTaskButt;
        private System.Windows.Forms.Button UpdateTaskButt;
        private System.Windows.Forms.Button DeleteTaskButt;
        private System.Windows.Forms.Button CloseFormButt;
        private System.Windows.Forms.FlowLayoutPanel SchedulesButtLayout;
        private System.Windows.Forms.Button CreateScheduleButt;
        private System.Windows.Forms.Button UpdateScheduleButt;
        private System.Windows.Forms.Button DeleteScheduleButt;
        private System.Windows.Forms.TableLayoutPanel TaskTableLayout;
        private System.Windows.Forms.TableLayoutPanel EMailTableLayout;
        private System.Windows.Forms.DataGridView EMailGirdView;
        private System.Windows.Forms.Label EMailLabel;
        private System.Windows.Forms.FlowLayoutPanel EMailButtsLayout;
        private System.Windows.Forms.TextBox EMailRecieverTextBox;
        private System.Windows.Forms.Button CreateEMailRecieverButt;
        private System.Windows.Forms.Button DeleteEMailRecieverButt;
        private System.Windows.Forms.ProgressBar ProgressBar;
    }
}