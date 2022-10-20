namespace NewProject
{
    partial class ChangesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangesForm));
            this.ChangesMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.DatesFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.FromLabel = new System.Windows.Forms.Label();
            this.FromDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.ToLabel = new System.Windows.Forms.Label();
            this.ToDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.RefreshChangesGridButt = new System.Windows.Forms.Button();
            this.ChangesTabControl = new System.Windows.Forms.TabControl();
            this.ChangesTabConnections = new System.Windows.Forms.TabPage();
            this.ChangesGridConnections = new System.Windows.Forms.DataGridView();
            this.ChangesGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.FilterByValueButt = new System.Windows.Forms.ToolStripMenuItem();
            this.FilterValueTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.ChangesTabCountersPLC = new System.Windows.Forms.TabPage();
            this.ChangesGridCountersPLC = new System.Windows.Forms.DataGridView();
            this.ChangesTabCountersRS = new System.Windows.Forms.TabPage();
            this.ChangesGridCountersRS = new System.Windows.Forms.DataGridView();
            this.ChangesMainLayout.SuspendLayout();
            this.DatesFlow.SuspendLayout();
            this.ChangesTabControl.SuspendLayout();
            this.ChangesTabConnections.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridConnections)).BeginInit();
            this.ChangesGridContextMenu.SuspendLayout();
            this.ChangesTabCountersPLC.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridCountersPLC)).BeginInit();
            this.ChangesTabCountersRS.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridCountersRS)).BeginInit();
            this.SuspendLayout();
            // 
            // ChangesMainLayout
            // 
            this.ChangesMainLayout.ColumnCount = 2;
            this.ChangesMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChangesMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChangesMainLayout.Controls.Add(this.DatesFlow, 0, 1);
            this.ChangesMainLayout.Controls.Add(this.ChangesTabControl, 0, 0);
            this.ChangesMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangesMainLayout.Location = new System.Drawing.Point(0, 0);
            this.ChangesMainLayout.Name = "ChangesMainLayout";
            this.ChangesMainLayout.RowCount = 2;
            this.ChangesMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.ChangesMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.ChangesMainLayout.Size = new System.Drawing.Size(1079, 594);
            this.ChangesMainLayout.TabIndex = 0;
            // 
            // DatesFlow
            // 
            this.DatesFlow.Controls.Add(this.FromLabel);
            this.DatesFlow.Controls.Add(this.FromDateTimePicker);
            this.DatesFlow.Controls.Add(this.ToLabel);
            this.DatesFlow.Controls.Add(this.ToDateTimePicker);
            this.DatesFlow.Controls.Add(this.RefreshChangesGridButt);
            this.DatesFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DatesFlow.Location = new System.Drawing.Point(3, 478);
            this.DatesFlow.Name = "DatesFlow";
            this.DatesFlow.Size = new System.Drawing.Size(533, 113);
            this.DatesFlow.TabIndex = 1;
            // 
            // FromLabel
            // 
            this.FromLabel.AutoSize = true;
            this.FromLabel.Location = new System.Drawing.Point(3, 0);
            this.FromLabel.Name = "FromLabel";
            this.FromLabel.Size = new System.Drawing.Size(20, 13);
            this.FromLabel.TabIndex = 0;
            this.FromLabel.Text = "От";
            // 
            // FromDateTimePicker
            // 
            this.FromDateTimePicker.Location = new System.Drawing.Point(29, 3);
            this.FromDateTimePicker.Name = "FromDateTimePicker";
            this.FromDateTimePicker.Size = new System.Drawing.Size(200, 20);
            this.FromDateTimePicker.TabIndex = 1;
            this.FromDateTimePicker.Value = new System.DateTime(2018, 5, 7, 0, 0, 0, 0);
            // 
            // ToLabel
            // 
            this.ToLabel.AutoSize = true;
            this.ToLabel.Location = new System.Drawing.Point(235, 0);
            this.ToLabel.Name = "ToLabel";
            this.ToLabel.Size = new System.Drawing.Size(19, 13);
            this.ToLabel.TabIndex = 2;
            this.ToLabel.Text = "до";
            // 
            // ToDateTimePicker
            // 
            this.ToDateTimePicker.Location = new System.Drawing.Point(260, 3);
            this.ToDateTimePicker.Name = "ToDateTimePicker";
            this.ToDateTimePicker.Size = new System.Drawing.Size(159, 20);
            this.ToDateTimePicker.TabIndex = 3;
            this.ToDateTimePicker.Value = new System.DateTime(2019, 1, 31, 0, 0, 0, 0);
            // 
            // RefreshChangesGridButt
            // 
            this.RefreshChangesGridButt.Location = new System.Drawing.Point(425, 3);
            this.RefreshChangesGridButt.Name = "RefreshChangesGridButt";
            this.RefreshChangesGridButt.Size = new System.Drawing.Size(75, 23);
            this.RefreshChangesGridButt.TabIndex = 4;
            this.RefreshChangesGridButt.Text = "Обновить";
            this.RefreshChangesGridButt.UseVisualStyleBackColor = true;
            this.RefreshChangesGridButt.Click += new System.EventHandler(this.RefreshChangesGridButt_Click);
            // 
            // ChangesTabControl
            // 
            this.ChangesMainLayout.SetColumnSpan(this.ChangesTabControl, 2);
            this.ChangesTabControl.Controls.Add(this.ChangesTabConnections);
            this.ChangesTabControl.Controls.Add(this.ChangesTabCountersPLC);
            this.ChangesTabControl.Controls.Add(this.ChangesTabCountersRS);
            this.ChangesTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangesTabControl.Location = new System.Drawing.Point(3, 3);
            this.ChangesTabControl.Name = "ChangesTabControl";
            this.ChangesTabControl.SelectedIndex = 0;
            this.ChangesTabControl.Size = new System.Drawing.Size(1073, 469);
            this.ChangesTabControl.TabIndex = 2;
            // 
            // ChangesTabConnections
            // 
            this.ChangesTabConnections.Controls.Add(this.ChangesGridConnections);
            this.ChangesTabConnections.Location = new System.Drawing.Point(4, 22);
            this.ChangesTabConnections.Name = "ChangesTabConnections";
            this.ChangesTabConnections.Padding = new System.Windows.Forms.Padding(3);
            this.ChangesTabConnections.Size = new System.Drawing.Size(1065, 443);
            this.ChangesTabConnections.TabIndex = 0;
            this.ChangesTabConnections.Text = "Подключения";
            this.ChangesTabConnections.UseVisualStyleBackColor = true;
            // 
            // ChangesGridConnections
            // 
            this.ChangesGridConnections.AllowUserToAddRows = false;
            this.ChangesGridConnections.AllowUserToDeleteRows = false;
            this.ChangesGridConnections.AllowUserToOrderColumns = true;
            this.ChangesGridConnections.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ChangesGridConnections.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ChangesGridConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ChangesGridConnections.ContextMenuStrip = this.ChangesGridContextMenu;
            this.ChangesGridConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangesGridConnections.EnableHeadersVisualStyles = false;
            this.ChangesGridConnections.Location = new System.Drawing.Point(3, 3);
            this.ChangesGridConnections.MultiSelect = false;
            this.ChangesGridConnections.Name = "ChangesGridConnections";
            this.ChangesGridConnections.ReadOnly = true;
            this.ChangesGridConnections.RowHeadersVisible = false;
            this.ChangesGridConnections.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ChangesGridConnections.Size = new System.Drawing.Size(1059, 437);
            this.ChangesGridConnections.TabIndex = 0;
            // 
            // ChangesGridContextMenu
            // 
            this.ChangesGridContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FilterByValueButt});
            this.ChangesGridContextMenu.Name = "ErrorsGridContextMenu";
            this.ChangesGridContextMenu.ShowImageMargin = false;
            this.ChangesGridContextMenu.Size = new System.Drawing.Size(166, 26);
            // 
            // FilterByValueButt
            // 
            this.FilterByValueButt.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FilterValueTextBox});
            this.FilterByValueButt.Name = "FilterByValueButt";
            this.FilterByValueButt.Size = new System.Drawing.Size(165, 22);
            this.FilterByValueButt.Text = "Фильтр по значению";
            this.FilterByValueButt.Click += new System.EventHandler(this.FilterByValueButt_Click);
            // 
            // FilterValueTextBox
            // 
            this.FilterValueTextBox.Name = "FilterValueTextBox";
            this.FilterValueTextBox.Size = new System.Drawing.Size(150, 23);
            this.FilterValueTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FilterValueTextBox_KeyDown);
            // 
            // ChangesTabCountersPLC
            // 
            this.ChangesTabCountersPLC.Controls.Add(this.ChangesGridCountersPLC);
            this.ChangesTabCountersPLC.Location = new System.Drawing.Point(4, 22);
            this.ChangesTabCountersPLC.Name = "ChangesTabCountersPLC";
            this.ChangesTabCountersPLC.Padding = new System.Windows.Forms.Padding(3);
            this.ChangesTabCountersPLC.Size = new System.Drawing.Size(1065, 443);
            this.ChangesTabCountersPLC.TabIndex = 1;
            this.ChangesTabCountersPLC.Text = "Счётчики PLC";
            this.ChangesTabCountersPLC.UseVisualStyleBackColor = true;
            // 
            // ChangesGridCountersPLC
            // 
            this.ChangesGridCountersPLC.AllowUserToAddRows = false;
            this.ChangesGridCountersPLC.AllowUserToDeleteRows = false;
            this.ChangesGridCountersPLC.AllowUserToOrderColumns = true;
            this.ChangesGridCountersPLC.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ChangesGridCountersPLC.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.ChangesGridCountersPLC.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ChangesGridCountersPLC.ContextMenuStrip = this.ChangesGridContextMenu;
            this.ChangesGridCountersPLC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangesGridCountersPLC.EnableHeadersVisualStyles = false;
            this.ChangesGridCountersPLC.Location = new System.Drawing.Point(3, 3);
            this.ChangesGridCountersPLC.MultiSelect = false;
            this.ChangesGridCountersPLC.Name = "ChangesGridCountersPLC";
            this.ChangesGridCountersPLC.ReadOnly = true;
            this.ChangesGridCountersPLC.RowHeadersVisible = false;
            this.ChangesGridCountersPLC.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ChangesGridCountersPLC.Size = new System.Drawing.Size(1059, 437);
            this.ChangesGridCountersPLC.TabIndex = 0;
            // 
            // ChangesTabCountersRS
            // 
            this.ChangesTabCountersRS.Controls.Add(this.ChangesGridCountersRS);
            this.ChangesTabCountersRS.Location = new System.Drawing.Point(4, 22);
            this.ChangesTabCountersRS.Name = "ChangesTabCountersRS";
            this.ChangesTabCountersRS.Size = new System.Drawing.Size(1065, 443);
            this.ChangesTabCountersRS.TabIndex = 2;
            this.ChangesTabCountersRS.Text = "Счётчики RS485";
            this.ChangesTabCountersRS.UseVisualStyleBackColor = true;
            // 
            // ChangesGridCountersRS
            // 
            this.ChangesGridCountersRS.AllowUserToAddRows = false;
            this.ChangesGridCountersRS.AllowUserToDeleteRows = false;
            this.ChangesGridCountersRS.AllowUserToOrderColumns = true;
            this.ChangesGridCountersRS.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.Yellow;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ChangesGridCountersRS.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.ChangesGridCountersRS.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ChangesGridCountersRS.ContextMenuStrip = this.ChangesGridContextMenu;
            this.ChangesGridCountersRS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChangesGridCountersRS.EnableHeadersVisualStyles = false;
            this.ChangesGridCountersRS.Location = new System.Drawing.Point(0, 0);
            this.ChangesGridCountersRS.MultiSelect = false;
            this.ChangesGridCountersRS.Name = "ChangesGridCountersRS";
            this.ChangesGridCountersRS.ReadOnly = true;
            this.ChangesGridCountersRS.RowHeadersVisible = false;
            this.ChangesGridCountersRS.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.ChangesGridCountersRS.Size = new System.Drawing.Size(1065, 443);
            this.ChangesGridCountersRS.TabIndex = 0;
            // 
            // ChangesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1079, 594);
            this.Controls.Add(this.ChangesMainLayout);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ChangesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Просмотр изменений";
            this.Load += new System.EventHandler(this.ChangesForm_Load);
            this.ChangesMainLayout.ResumeLayout(false);
            this.DatesFlow.ResumeLayout(false);
            this.DatesFlow.PerformLayout();
            this.ChangesTabControl.ResumeLayout(false);
            this.ChangesTabConnections.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridConnections)).EndInit();
            this.ChangesGridContextMenu.ResumeLayout(false);
            this.ChangesTabCountersPLC.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridCountersPLC)).EndInit();
            this.ChangesTabCountersRS.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ChangesGridCountersRS)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel ChangesMainLayout;
        private System.Windows.Forms.FlowLayoutPanel DatesFlow;
        private System.Windows.Forms.Label FromLabel;
        private System.Windows.Forms.DateTimePicker FromDateTimePicker;
        private System.Windows.Forms.Label ToLabel;
        private System.Windows.Forms.DateTimePicker ToDateTimePicker;
        private System.Windows.Forms.Button RefreshChangesGridButt;
        private System.Windows.Forms.TabControl ChangesTabControl;
        private System.Windows.Forms.TabPage ChangesTabConnections;
        private System.Windows.Forms.TabPage ChangesTabCountersPLC;
        private System.Windows.Forms.DataGridView ChangesGridConnections;
        private System.Windows.Forms.DataGridView ChangesGridCountersPLC;
        private System.Windows.Forms.TabPage ChangesTabCountersRS;
        private System.Windows.Forms.DataGridView ChangesGridCountersRS;
        private System.Windows.Forms.ContextMenuStrip ChangesGridContextMenu;
        private System.Windows.Forms.ToolStripMenuItem FilterByValueButt;
        private System.Windows.Forms.ToolStripTextBox FilterValueTextBox;
    }
}