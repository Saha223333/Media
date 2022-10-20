namespace NewProject
{
    partial class ErrorsForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorsForm));
            this.MainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ErrorsTabControl = new System.Windows.Forms.TabControl();
            this.ErrorsPageConnections = new System.Windows.Forms.TabPage();
            this.ConnectionsErrorsFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.ConnectionsErrorsGrid = new System.Windows.Forms.DataGridView();
            this.ErrorsGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ExportToExcelButt = new System.Windows.Forms.ToolStripMenuItem();
            this.ErrorsPageCountersPLC = new System.Windows.Forms.TabPage();
            this.CountersPLCErrorsGrid = new System.Windows.Forms.DataGridView();
            this.ErrorsCounterRSTab = new System.Windows.Forms.TabPage();
            this.CountersRSErrorsGrid = new System.Windows.Forms.DataGridView();
            this.ErrorsFlowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.RefreshErrorsButt = new System.Windows.Forms.Button();
            this.DateFromLabel = new System.Windows.Forms.Label();
            this.DateFromPicker = new System.Windows.Forms.DateTimePicker();
            this.DateToLabel = new System.Windows.Forms.Label();
            this.DateToPicker = new System.Windows.Forms.DateTimePicker();
            this.GoToTaskAll = new System.Windows.Forms.Button();
            this.FastErrorsReportButt = new System.Windows.Forms.Button();
            this.MainLayout.SuspendLayout();
            this.ErrorsTabControl.SuspendLayout();
            this.ErrorsPageConnections.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ConnectionsErrorsGrid)).BeginInit();
            this.ErrorsGridContextMenu.SuspendLayout();
            this.ErrorsPageCountersPLC.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CountersPLCErrorsGrid)).BeginInit();
            this.ErrorsCounterRSTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CountersRSErrorsGrid)).BeginInit();
            this.ErrorsFlowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 
            this.MainLayout.ColumnCount = 2;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.MainLayout.Controls.Add(this.ErrorsTabControl, 0, 0);
            this.MainLayout.Controls.Add(this.ErrorsFlowLayoutPanel1, 0, 1);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.RowCount = 2;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.MainLayout.Size = new System.Drawing.Size(1268, 683);
            this.MainLayout.TabIndex = 0;
            // 
            // ErrorsTabControl
            // 
            this.MainLayout.SetColumnSpan(this.ErrorsTabControl, 2);
            this.ErrorsTabControl.Controls.Add(this.ErrorsPageConnections);
            this.ErrorsTabControl.Controls.Add(this.ErrorsPageCountersPLC);
            this.ErrorsTabControl.Controls.Add(this.ErrorsCounterRSTab);
            this.ErrorsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ErrorsTabControl.Location = new System.Drawing.Point(3, 3);
            this.ErrorsTabControl.Name = "ErrorsTabControl";
            this.ErrorsTabControl.SelectedIndex = 0;
            this.ErrorsTabControl.Size = new System.Drawing.Size(1262, 540);
            this.ErrorsTabControl.TabIndex = 0;
            // 
            // ErrorsPageConnections
            // 
            this.ErrorsPageConnections.Controls.Add(this.ConnectionsErrorsFlow);
            this.ErrorsPageConnections.Controls.Add(this.ConnectionsErrorsGrid);
            this.ErrorsPageConnections.Location = new System.Drawing.Point(4, 22);
            this.ErrorsPageConnections.Name = "ErrorsPageConnections";
            this.ErrorsPageConnections.Padding = new System.Windows.Forms.Padding(3);
            this.ErrorsPageConnections.Size = new System.Drawing.Size(1254, 514);
            this.ErrorsPageConnections.TabIndex = 0;
            this.ErrorsPageConnections.Text = "Подключения";
            this.ErrorsPageConnections.UseVisualStyleBackColor = true;
            this.ErrorsPageConnections.Enter += new System.EventHandler(this.ErrorsPageConnections_Enter);
            // 
            // ConnectionsErrorsFlow
            // 
            this.ConnectionsErrorsFlow.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ConnectionsErrorsFlow.Location = new System.Drawing.Point(3, 479);
            this.ConnectionsErrorsFlow.Name = "ConnectionsErrorsFlow";
            this.ConnectionsErrorsFlow.Size = new System.Drawing.Size(1248, 32);
            this.ConnectionsErrorsFlow.TabIndex = 2;
            this.ConnectionsErrorsFlow.Visible = false;
            // 
            // ConnectionsErrorsGrid
            // 
            this.ConnectionsErrorsGrid.AllowUserToAddRows = false;
            this.ConnectionsErrorsGrid.AllowUserToDeleteRows = false;
            this.ConnectionsErrorsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ConnectionsErrorsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ConnectionsErrorsGrid.ColumnHeadersHeight = 45;
            this.ConnectionsErrorsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ConnectionsErrorsGrid.ContextMenuStrip = this.ErrorsGridContextMenu;
            this.ConnectionsErrorsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConnectionsErrorsGrid.EnableHeadersVisualStyles = false;
            this.ConnectionsErrorsGrid.Location = new System.Drawing.Point(3, 3);
            this.ConnectionsErrorsGrid.MultiSelect = false;
            this.ConnectionsErrorsGrid.Name = "ConnectionsErrorsGrid";
            this.ConnectionsErrorsGrid.ReadOnly = true;
            this.ConnectionsErrorsGrid.RowHeadersVisible = false;
            this.ConnectionsErrorsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ConnectionsErrorsGrid.Size = new System.Drawing.Size(1248, 508);
            this.ConnectionsErrorsGrid.TabIndex = 0;
            this.ConnectionsErrorsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ConnectionsErrorsGrid_CellDoubleClick);
            this.ConnectionsErrorsGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.ConnectionsErrorsGrid_ColumnWidthChanged);
            this.ConnectionsErrorsGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.ConnectionsErrorsGrid_DataBindingComplete);
            this.ConnectionsErrorsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.ConnectionsErrorsGrid_DataError);
            this.ConnectionsErrorsGrid.SizeChanged += new System.EventHandler(this.ConnectionsErrorsGrid_SizeChanged);
            // 
            // ErrorsGridContextMenu
            // 
            this.ErrorsGridContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExportToExcelButt});
            this.ErrorsGridContextMenu.Name = "ErrorsGridContextMenu";
            this.ErrorsGridContextMenu.ShowImageMargin = false;
            this.ErrorsGridContextMenu.Size = new System.Drawing.Size(133, 26);
            // 
            // ExportToExcelButt
            // 
            this.ExportToExcelButt.Name = "ExportToExcelButt";
            this.ExportToExcelButt.Size = new System.Drawing.Size(132, 22);
            this.ExportToExcelButt.Text = "Экспорт в Excel";
            this.ExportToExcelButt.Click += new System.EventHandler(this.ExportToExcelButt_Click);
            // 
            // ErrorsPageCountersPLC
            // 
            this.ErrorsPageCountersPLC.Controls.Add(this.CountersPLCErrorsGrid);
            this.ErrorsPageCountersPLC.Location = new System.Drawing.Point(4, 22);
            this.ErrorsPageCountersPLC.Name = "ErrorsPageCountersPLC";
            this.ErrorsPageCountersPLC.Padding = new System.Windows.Forms.Padding(3);
            this.ErrorsPageCountersPLC.Size = new System.Drawing.Size(1254, 514);
            this.ErrorsPageCountersPLC.TabIndex = 1;
            this.ErrorsPageCountersPLC.Text = "Счётчики PLC";
            this.ErrorsPageCountersPLC.UseVisualStyleBackColor = true;
            this.ErrorsPageCountersPLC.Enter += new System.EventHandler(this.ErrorsPageCountersPLC_Enter);
            // 
            // CountersPLCErrorsGrid
            // 
            this.CountersPLCErrorsGrid.AllowUserToAddRows = false;
            this.CountersPLCErrorsGrid.AllowUserToDeleteRows = false;
            this.CountersPLCErrorsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.CountersPLCErrorsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.CountersPLCErrorsGrid.ColumnHeadersHeight = 45;
            this.CountersPLCErrorsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.CountersPLCErrorsGrid.ContextMenuStrip = this.ErrorsGridContextMenu;
            this.CountersPLCErrorsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountersPLCErrorsGrid.EnableHeadersVisualStyles = false;
            this.CountersPLCErrorsGrid.Location = new System.Drawing.Point(3, 3);
            this.CountersPLCErrorsGrid.MultiSelect = false;
            this.CountersPLCErrorsGrid.Name = "CountersPLCErrorsGrid";
            this.CountersPLCErrorsGrid.ReadOnly = true;
            this.CountersPLCErrorsGrid.RowHeadersVisible = false;
            this.CountersPLCErrorsGrid.RowTemplate.ReadOnly = true;
            this.CountersPLCErrorsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.CountersPLCErrorsGrid.Size = new System.Drawing.Size(1248, 508);
            this.CountersPLCErrorsGrid.TabIndex = 0;
            this.CountersPLCErrorsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CountersPLCErrorsGrid_CellDoubleClick);
            this.CountersPLCErrorsGrid.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.CountersPLCErrorsGrid_CellEnter);
            this.CountersPLCErrorsGrid.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.CountersPLCErrorsGrid_CellMouseClick);
            this.CountersPLCErrorsGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.CountersPLCErrorsGrid_ColumnWidthChanged);
            this.CountersPLCErrorsGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.CountersPLCErrorsGrid_DataBindingComplete);
            this.CountersPLCErrorsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.CountersPLCErrorsGrid_DataError);
            this.CountersPLCErrorsGrid.SizeChanged += new System.EventHandler(this.CountersPLCErrorsGrid_SizeChanged);
            // 
            // ErrorsCounterRSTab
            // 
            this.ErrorsCounterRSTab.Controls.Add(this.CountersRSErrorsGrid);
            this.ErrorsCounterRSTab.Location = new System.Drawing.Point(4, 22);
            this.ErrorsCounterRSTab.Name = "ErrorsCounterRSTab";
            this.ErrorsCounterRSTab.Padding = new System.Windows.Forms.Padding(3);
            this.ErrorsCounterRSTab.Size = new System.Drawing.Size(1254, 514);
            this.ErrorsCounterRSTab.TabIndex = 2;
            this.ErrorsCounterRSTab.Text = "Счётчики RS485";
            this.ErrorsCounterRSTab.UseVisualStyleBackColor = true;
            this.ErrorsCounterRSTab.Enter += new System.EventHandler(this.ErrorsCounterRSTab_Enter);
            // 
            // CountersRSErrorsGrid
            // 
            this.CountersRSErrorsGrid.AllowUserToAddRows = false;
            this.CountersRSErrorsGrid.AllowUserToDeleteRows = false;
            this.CountersRSErrorsGrid.AllowUserToResizeRows = false;
            this.CountersRSErrorsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.CountersRSErrorsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.CountersRSErrorsGrid.ColumnHeadersHeight = 45;
            this.CountersRSErrorsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.CountersRSErrorsGrid.ContextMenuStrip = this.ErrorsGridContextMenu;
            this.CountersRSErrorsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountersRSErrorsGrid.EnableHeadersVisualStyles = false;
            this.CountersRSErrorsGrid.Location = new System.Drawing.Point(3, 3);
            this.CountersRSErrorsGrid.MultiSelect = false;
            this.CountersRSErrorsGrid.Name = "CountersRSErrorsGrid";
            this.CountersRSErrorsGrid.ReadOnly = true;
            this.CountersRSErrorsGrid.RowHeadersVisible = false;
            this.CountersRSErrorsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.CountersRSErrorsGrid.Size = new System.Drawing.Size(1248, 508);
            this.CountersRSErrorsGrid.TabIndex = 0;
            this.CountersRSErrorsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CountersRSErrorsGrid_CellDoubleClick);
            this.CountersRSErrorsGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.CountersRSErrorsGrid_ColumnWidthChanged);
            this.CountersRSErrorsGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.CountersRSErrorsGrid_DataBindingComplete);
            this.CountersRSErrorsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.CountersRSErrorsGrid_DataError);
            this.CountersRSErrorsGrid.SizeChanged += new System.EventHandler(this.CountersRSErrorsGrid_SizeChanged);
            // 
            // ErrorsFlowLayoutPanel1
            // 
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.RefreshErrorsButt);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.DateFromLabel);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.DateFromPicker);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.DateToLabel);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.DateToPicker);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.GoToTaskAll);
            this.ErrorsFlowLayoutPanel1.Controls.Add(this.FastErrorsReportButt);
            this.ErrorsFlowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ErrorsFlowLayoutPanel1.Location = new System.Drawing.Point(3, 549);
            this.ErrorsFlowLayoutPanel1.Name = "ErrorsFlowLayoutPanel1";
            this.ErrorsFlowLayoutPanel1.Size = new System.Drawing.Size(628, 131);
            this.ErrorsFlowLayoutPanel1.TabIndex = 1;
            // 
            // RefreshErrorsButt
            // 
            this.RefreshErrorsButt.Location = new System.Drawing.Point(3, 3);
            this.RefreshErrorsButt.Name = "RefreshErrorsButt";
            this.RefreshErrorsButt.Size = new System.Drawing.Size(75, 23);
            this.RefreshErrorsButt.TabIndex = 0;
            this.RefreshErrorsButt.Text = "Обновить";
            this.RefreshErrorsButt.UseVisualStyleBackColor = true;
            this.RefreshErrorsButt.Click += new System.EventHandler(this.RefreshErrorsButt_Click);
            // 
            // DateFromLabel
            // 
            this.DateFromLabel.AutoSize = true;
            this.DateFromLabel.Location = new System.Drawing.Point(84, 7);
            this.DateFromLabel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.DateFromLabel.Name = "DateFromLabel";
            this.DateFromLabel.Size = new System.Drawing.Size(20, 13);
            this.DateFromLabel.TabIndex = 1;
            this.DateFromLabel.Text = "От";
            // 
            // DateFromPicker
            // 
            this.DateFromPicker.Location = new System.Drawing.Point(110, 3);
            this.DateFromPicker.Name = "DateFromPicker";
            this.DateFromPicker.Size = new System.Drawing.Size(200, 20);
            this.DateFromPicker.TabIndex = 2;
            this.DateFromPicker.Value = new System.DateTime(2018, 5, 28, 0, 0, 0, 0);
            // 
            // DateToLabel
            // 
            this.DateToLabel.AutoSize = true;
            this.DateToLabel.Location = new System.Drawing.Point(316, 7);
            this.DateToLabel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.DateToLabel.Name = "DateToLabel";
            this.DateToLabel.Size = new System.Drawing.Size(19, 13);
            this.DateToLabel.TabIndex = 3;
            this.DateToLabel.Text = "до";
            // 
            // DateToPicker
            // 
            this.DateToPicker.Location = new System.Drawing.Point(341, 3);
            this.DateToPicker.Name = "DateToPicker";
            this.DateToPicker.Size = new System.Drawing.Size(200, 20);
            this.DateToPicker.TabIndex = 4;
            // 
            // GoToTaskAll
            // 
            this.GoToTaskAll.Location = new System.Drawing.Point(3, 32);
            this.GoToTaskAll.Name = "GoToTaskAll";
            this.GoToTaskAll.Size = new System.Drawing.Size(172, 23);
            this.GoToTaskAll.TabIndex = 5;
            this.GoToTaskAll.Text = "Поместить список в задание";
            this.GoToTaskAll.UseVisualStyleBackColor = true;
            this.GoToTaskAll.Click += new System.EventHandler(this.button1_Click);
            // 
            // FastErrorsReportButt
            // 
            this.FastErrorsReportButt.Location = new System.Drawing.Point(181, 32);
            this.FastErrorsReportButt.Name = "FastErrorsReportButt";
            this.FastErrorsReportButt.Size = new System.Drawing.Size(114, 23);
            this.FastErrorsReportButt.TabIndex = 6;
            this.FastErrorsReportButt.Text = "Быстрый отчёт";
            this.FastErrorsReportButt.UseVisualStyleBackColor = true;
            this.FastErrorsReportButt.Click += new System.EventHandler(this.FastErrorsReport_Click);
            // 
            // ErrorsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1268, 683);
            this.Controls.Add(this.MainLayout);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ErrorsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Ошибки и предупреждения";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ErrorsForm_FormClosed);
            this.Load += new System.EventHandler(this.ErrorsForm_Load);
            this.Shown += new System.EventHandler(this.ErrorsForm_Shown);
            this.MainLayout.ResumeLayout(false);
            this.ErrorsTabControl.ResumeLayout(false);
            this.ErrorsPageConnections.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConnectionsErrorsGrid)).EndInit();
            this.ErrorsGridContextMenu.ResumeLayout(false);
            this.ErrorsPageCountersPLC.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CountersPLCErrorsGrid)).EndInit();
            this.ErrorsCounterRSTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CountersRSErrorsGrid)).EndInit();
            this.ErrorsFlowLayoutPanel1.ResumeLayout(false);
            this.ErrorsFlowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel MainLayout;
        private System.Windows.Forms.TabControl ErrorsTabControl;
        private System.Windows.Forms.TabPage ErrorsPageConnections;
        private System.Windows.Forms.DataGridView ConnectionsErrorsGrid;
        private System.Windows.Forms.TabPage ErrorsPageCountersPLC;
        private System.Windows.Forms.FlowLayoutPanel ConnectionsErrorsFlow;
        private System.Windows.Forms.FlowLayoutPanel ErrorsFlowLayoutPanel1;
        private System.Windows.Forms.Button RefreshErrorsButt;
        private System.Windows.Forms.Label DateFromLabel;
        private System.Windows.Forms.DateTimePicker DateFromPicker;
        private System.Windows.Forms.Label DateToLabel;
        private System.Windows.Forms.DateTimePicker DateToPicker;
        private System.Windows.Forms.DataGridView CountersPLCErrorsGrid;
        private System.Windows.Forms.TabPage ErrorsCounterRSTab;
        private System.Windows.Forms.DataGridView CountersRSErrorsGrid;
        private System.Windows.Forms.ContextMenuStrip ErrorsGridContextMenu;
        private System.Windows.Forms.ToolStripMenuItem ExportToExcelButt;
        private System.Windows.Forms.Button GoToTaskAll;
        private System.Windows.Forms.Button FastErrorsReportButt;
    }
}