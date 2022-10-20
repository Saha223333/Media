namespace NewProject
{
    partial class ObjectsListsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectsListsForm));
            this.ObjectListFormLayout = new System.Windows.Forms.TableLayoutPanel();
            this.ObjectListsTabControl = new System.Windows.Forms.TabControl();
            this.ConnectionsListTab = new System.Windows.Forms.TabPage();
            this.ConnectionsListGrid = new System.Windows.Forms.DataGridView();
            this.ListsGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ExportToExcelButt = new System.Windows.Forms.ToolStripMenuItem();
            this.CountersPLCListTab = new System.Windows.Forms.TabPage();
            this.CountersPLCListGrid = new System.Windows.Forms.DataGridView();
            this.CountersRSListTab = new System.Windows.Forms.TabPage();
            this.CountersRSListGrid = new System.Windows.Forms.DataGridView();
            this.ListsControlFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.RefreshListsButt = new System.Windows.Forms.Button();
            this.GoToTaskAllButt = new System.Windows.Forms.Button();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.QuantityLabel = new System.Windows.Forms.Label();
            this.ObjectListFormLayout.SuspendLayout();
            this.ObjectListsTabControl.SuspendLayout();
            this.ConnectionsListTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ConnectionsListGrid)).BeginInit();
            this.ListsGridContextMenu.SuspendLayout();
            this.CountersPLCListTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CountersPLCListGrid)).BeginInit();
            this.CountersRSListTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CountersRSListGrid)).BeginInit();
            this.ListsControlFlow.SuspendLayout();
            this.SuspendLayout();
            // 
            // ObjectListFormLayout
            // 
            this.ObjectListFormLayout.ColumnCount = 2;
            this.ObjectListFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ObjectListFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ObjectListFormLayout.Controls.Add(this.ObjectListsTabControl, 0, 0);
            this.ObjectListFormLayout.Controls.Add(this.ListsControlFlow, 0, 1);
            this.ObjectListFormLayout.Controls.Add(this.ProgressBar, 0, 2);
            this.ObjectListFormLayout.Controls.Add(this.QuantityLabel, 1, 1);
            this.ObjectListFormLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectListFormLayout.Location = new System.Drawing.Point(0, 0);
            this.ObjectListFormLayout.Name = "ObjectListFormLayout";
            this.ObjectListFormLayout.RowCount = 3;
            this.ObjectListFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.ObjectListFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.ObjectListFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.ObjectListFormLayout.Size = new System.Drawing.Size(1268, 683);
            this.ObjectListFormLayout.TabIndex = 0;
            // 
            // ObjectListsTabControl
            // 
            this.ObjectListFormLayout.SetColumnSpan(this.ObjectListsTabControl, 2);
            this.ObjectListsTabControl.Controls.Add(this.ConnectionsListTab);
            this.ObjectListsTabControl.Controls.Add(this.CountersPLCListTab);
            this.ObjectListsTabControl.Controls.Add(this.CountersRSListTab);
            this.ObjectListsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectListsTabControl.Location = new System.Drawing.Point(3, 3);
            this.ObjectListsTabControl.Name = "ObjectListsTabControl";
            this.ObjectListsTabControl.SelectedIndex = 0;
            this.ObjectListsTabControl.Size = new System.Drawing.Size(1262, 540);
            this.ObjectListsTabControl.TabIndex = 0;
            // 
            // ConnectionsListTab
            // 
            this.ConnectionsListTab.Controls.Add(this.ConnectionsListGrid);
            this.ConnectionsListTab.Location = new System.Drawing.Point(4, 22);
            this.ConnectionsListTab.Name = "ConnectionsListTab";
            this.ConnectionsListTab.Padding = new System.Windows.Forms.Padding(3);
            this.ConnectionsListTab.Size = new System.Drawing.Size(1254, 514);
            this.ConnectionsListTab.TabIndex = 0;
            this.ConnectionsListTab.Text = "Подключения";
            this.ConnectionsListTab.UseVisualStyleBackColor = true;
            this.ConnectionsListTab.Enter += new System.EventHandler(this.ConnectionsListTab_Enter);
            // 
            // ConnectionsListGrid
            // 
            this.ConnectionsListGrid.AllowUserToAddRows = false;
            this.ConnectionsListGrid.AllowUserToDeleteRows = false;
            this.ConnectionsListGrid.AllowUserToResizeRows = false;
            this.ConnectionsListGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ConnectionsListGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ConnectionsListGrid.ColumnHeadersHeight = 45;
            this.ConnectionsListGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.ConnectionsListGrid.ContextMenuStrip = this.ListsGridContextMenu;
            this.ConnectionsListGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConnectionsListGrid.EnableHeadersVisualStyles = false;
            this.ConnectionsListGrid.Location = new System.Drawing.Point(3, 3);
            this.ConnectionsListGrid.MultiSelect = false;
            this.ConnectionsListGrid.Name = "ConnectionsListGrid";
            this.ConnectionsListGrid.RowHeadersVisible = false;
            this.ConnectionsListGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ConnectionsListGrid.Size = new System.Drawing.Size(1248, 508);
            this.ConnectionsListGrid.TabIndex = 0;
            this.ConnectionsListGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ConnectionsListGrid_CellDoubleClick);
            this.ConnectionsListGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.ConnectionsListGrid_CellEndEdit);
            this.ConnectionsListGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.ConnectionsListGrid_ColumnWidthChanged_1);
            this.ConnectionsListGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.ConnectionsListGrid_DataBindingComplete);
            this.ConnectionsListGrid.SizeChanged += new System.EventHandler(this.ConnectionsListGrid_SizeChanged);
            this.ConnectionsListGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ConnectionsListGrid_KeyDown);
            // 
            // ListsGridContextMenu
            // 
            this.ListsGridContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExportToExcelButt});
            this.ListsGridContextMenu.Name = "ErrorsGridContextMenu";
            this.ListsGridContextMenu.ShowImageMargin = false;
            this.ListsGridContextMenu.Size = new System.Drawing.Size(133, 26);
            // 
            // ExportToExcelButt
            // 
            this.ExportToExcelButt.Name = "ExportToExcelButt";
            this.ExportToExcelButt.Size = new System.Drawing.Size(132, 22);
            this.ExportToExcelButt.Text = "Экспорт в Excel";
            this.ExportToExcelButt.Click += new System.EventHandler(this.ExportToExcelButt_Click_1);
            // 
            // CountersPLCListTab
            // 
            this.CountersPLCListTab.Controls.Add(this.CountersPLCListGrid);
            this.CountersPLCListTab.Location = new System.Drawing.Point(4, 22);
            this.CountersPLCListTab.Name = "CountersPLCListTab";
            this.CountersPLCListTab.Padding = new System.Windows.Forms.Padding(3);
            this.CountersPLCListTab.Size = new System.Drawing.Size(1254, 514);
            this.CountersPLCListTab.TabIndex = 1;
            this.CountersPLCListTab.Text = "Счётчики PLC";
            this.CountersPLCListTab.UseVisualStyleBackColor = true;
            this.CountersPLCListTab.Enter += new System.EventHandler(this.CountersPLCListTab_Enter);
            // 
            // CountersPLCListGrid
            // 
            this.CountersPLCListGrid.AllowUserToAddRows = false;
            this.CountersPLCListGrid.AllowUserToDeleteRows = false;
            this.CountersPLCListGrid.AllowUserToResizeRows = false;
            this.CountersPLCListGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.CountersPLCListGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.CountersPLCListGrid.ColumnHeadersHeight = 45;
            this.CountersPLCListGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.CountersPLCListGrid.ContextMenuStrip = this.ListsGridContextMenu;
            this.CountersPLCListGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountersPLCListGrid.EnableHeadersVisualStyles = false;
            this.CountersPLCListGrid.Location = new System.Drawing.Point(3, 3);
            this.CountersPLCListGrid.MultiSelect = false;
            this.CountersPLCListGrid.Name = "CountersPLCListGrid";
            this.CountersPLCListGrid.ReadOnly = true;
            this.CountersPLCListGrid.RowHeadersVisible = false;
            this.CountersPLCListGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.CountersPLCListGrid.Size = new System.Drawing.Size(1248, 508);
            this.CountersPLCListGrid.TabIndex = 0;
            this.CountersPLCListGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CountersPLCListGrid_CellDoubleClick);
            this.CountersPLCListGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.CountersPLCListGrid_ColumnWidthChanged_1);
            this.CountersPLCListGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.CountersPLCListGrid_DataBindingComplete);
            this.CountersPLCListGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.CountersPLCListGrid_DataError);
            this.CountersPLCListGrid.SizeChanged += new System.EventHandler(this.CountersPLCListGrid_SizeChanged);
            this.CountersPLCListGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CountersPLCListGrid_KeyDown);
            // 
            // CountersRSListTab
            // 
            this.CountersRSListTab.Controls.Add(this.CountersRSListGrid);
            this.CountersRSListTab.Location = new System.Drawing.Point(4, 22);
            this.CountersRSListTab.Name = "CountersRSListTab";
            this.CountersRSListTab.Padding = new System.Windows.Forms.Padding(3);
            this.CountersRSListTab.Size = new System.Drawing.Size(1254, 514);
            this.CountersRSListTab.TabIndex = 2;
            this.CountersRSListTab.Text = "Счётчики RS485";
            this.CountersRSListTab.UseVisualStyleBackColor = true;
            this.CountersRSListTab.Enter += new System.EventHandler(this.CountersRSListTab_Enter);
            // 
            // CountersRSListGrid
            // 
            this.CountersRSListGrid.AllowUserToAddRows = false;
            this.CountersRSListGrid.AllowUserToDeleteRows = false;
            this.CountersRSListGrid.AllowUserToResizeRows = false;
            this.CountersRSListGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.CountersRSListGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.CountersRSListGrid.ColumnHeadersHeight = 45;
            this.CountersRSListGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.CountersRSListGrid.ContextMenuStrip = this.ListsGridContextMenu;
            this.CountersRSListGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountersRSListGrid.EnableHeadersVisualStyles = false;
            this.CountersRSListGrid.Location = new System.Drawing.Point(3, 3);
            this.CountersRSListGrid.MultiSelect = false;
            this.CountersRSListGrid.Name = "CountersRSListGrid";
            this.CountersRSListGrid.ReadOnly = true;
            this.CountersRSListGrid.RowHeadersVisible = false;
            this.CountersRSListGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.CountersRSListGrid.Size = new System.Drawing.Size(1248, 508);
            this.CountersRSListGrid.TabIndex = 0;
            this.CountersRSListGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.CountersRSListGrid_CellDoubleClick);
            this.CountersRSListGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.CountersRSListGrid_ColumnWidthChanged_1);
            this.CountersRSListGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.CountersRSListGrid_DataBindingComplete);
            this.CountersRSListGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.CountersRSListGrid_DataError);
            this.CountersRSListGrid.SizeChanged += new System.EventHandler(this.CountersRSListGrid_SizeChanged);
            this.CountersRSListGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CountersRSListGrid_KeyDown);
            // 
            // ListsControlFlow
            // 
            this.ListsControlFlow.Controls.Add(this.RefreshListsButt);
            this.ListsControlFlow.Controls.Add(this.GoToTaskAllButt);
            this.ListsControlFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListsControlFlow.Location = new System.Drawing.Point(3, 549);
            this.ListsControlFlow.Name = "ListsControlFlow";
            this.ListsControlFlow.Size = new System.Drawing.Size(628, 96);
            this.ListsControlFlow.TabIndex = 1;
            // 
            // RefreshListsButt
            // 
            this.RefreshListsButt.Location = new System.Drawing.Point(3, 3);
            this.RefreshListsButt.Name = "RefreshListsButt";
            this.RefreshListsButt.Size = new System.Drawing.Size(75, 23);
            this.RefreshListsButt.TabIndex = 0;
            this.RefreshListsButt.Text = "Обновить";
            this.RefreshListsButt.UseVisualStyleBackColor = true;
            this.RefreshListsButt.Click += new System.EventHandler(this.RefreshListsButt_Click);
            // 
            // GoToTaskAllButt
            // 
            this.GoToTaskAllButt.Location = new System.Drawing.Point(84, 3);
            this.GoToTaskAllButt.Name = "GoToTaskAllButt";
            this.GoToTaskAllButt.Size = new System.Drawing.Size(180, 23);
            this.GoToTaskAllButt.TabIndex = 1;
            this.GoToTaskAllButt.Text = "Поместить список в задание";
            this.GoToTaskAllButt.UseVisualStyleBackColor = true;
            this.GoToTaskAllButt.Click += new System.EventHandler(this.GoToTaskAll_Click);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgressBar.ForeColor = System.Drawing.Color.ForestGreen;
            this.ProgressBar.Location = new System.Drawing.Point(3, 651);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(628, 29);
            this.ProgressBar.Step = 1;
            this.ProgressBar.TabIndex = 2;
            // 
            // QuantityLabel
            // 
            this.QuantityLabel.AutoSize = true;
            this.QuantityLabel.Location = new System.Drawing.Point(637, 546);
            this.QuantityLabel.Name = "QuantityLabel";
            this.QuantityLabel.Size = new System.Drawing.Size(0, 13);
            this.QuantityLabel.TabIndex = 3;
            // 
            // ObjectsListsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1268, 683);
            this.Controls.Add(this.ObjectListFormLayout);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ObjectsListsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Списки объектов";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ObjectsListsForm_FormClosed);
            this.Load += new System.EventHandler(this.ObjectsListsForm_Load);
            this.ObjectListFormLayout.ResumeLayout(false);
            this.ObjectListFormLayout.PerformLayout();
            this.ObjectListsTabControl.ResumeLayout(false);
            this.ConnectionsListTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConnectionsListGrid)).EndInit();
            this.ListsGridContextMenu.ResumeLayout(false);
            this.CountersPLCListTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CountersPLCListGrid)).EndInit();
            this.CountersRSListTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.CountersRSListGrid)).EndInit();
            this.ListsControlFlow.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel ObjectListFormLayout;
        private System.Windows.Forms.TabControl ObjectListsTabControl;
        private System.Windows.Forms.TabPage ConnectionsListTab;
        private System.Windows.Forms.TabPage CountersPLCListTab;
        private System.Windows.Forms.TabPage CountersRSListTab;
        private System.Windows.Forms.DataGridView ConnectionsListGrid;
        private System.Windows.Forms.ContextMenuStrip ListsGridContextMenu;
        private System.Windows.Forms.ToolStripMenuItem ExportToExcelButt;
        private System.Windows.Forms.FlowLayoutPanel ListsControlFlow;
        private System.Windows.Forms.Button RefreshListsButt;
        private System.Windows.Forms.DataGridView CountersPLCListGrid;
        private System.Windows.Forms.DataGridView CountersRSListGrid;
        private System.Windows.Forms.Button GoToTaskAllButt;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Label QuantityLabel;
    }
}