namespace NewProject
{
    partial class CalendarForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CalendarForm));
            this.CalendarFormMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.CalendarDaysCaption = new System.Windows.Forms.Label();
            this.CalendarDaysGrid = new System.Windows.Forms.DataGridView();
            this.CalendarFlowLayout1 = new System.Windows.Forms.FlowLayoutPanel();
            this.monthCalendar = new System.Windows.Forms.MonthCalendar();
            this.MonthPeakHoursLabel = new System.Windows.Forms.Label();
            this.CalendarFlowLayout2 = new System.Windows.Forms.FlowLayoutPanel();
            this.LowerPeakHourNumeric = new System.Windows.Forms.NumericUpDown();
            this.PeakHourLabel = new System.Windows.Forms.Label();
            this.UpperPeakHourNumeric = new System.Windows.Forms.NumericUpDown();
            this.AddPeakHourButt = new System.Windows.Forms.Button();
            this.CalendarFormMainLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CalendarDaysGrid)).BeginInit();
            this.CalendarFlowLayout1.SuspendLayout();
            this.CalendarFlowLayout2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowerPeakHourNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.UpperPeakHourNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // CalendarFormMainLayout
            // 
            this.CalendarFormMainLayout.ColumnCount = 2;
            this.CalendarFormMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.CalendarFormMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.CalendarFormMainLayout.Controls.Add(this.CalendarDaysCaption, 0, 0);
            this.CalendarFormMainLayout.Controls.Add(this.CalendarDaysGrid, 1, 2);
            this.CalendarFormMainLayout.Controls.Add(this.CalendarFlowLayout1, 0, 1);
            this.CalendarFormMainLayout.Controls.Add(this.CalendarFlowLayout2, 0, 2);
            this.CalendarFormMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CalendarFormMainLayout.Location = new System.Drawing.Point(0, 0);
            this.CalendarFormMainLayout.Name = "CalendarFormMainLayout";
            this.CalendarFormMainLayout.RowCount = 3;
            this.CalendarFormMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.CalendarFormMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.CalendarFormMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.CalendarFormMainLayout.Size = new System.Drawing.Size(668, 298);
            this.CalendarFormMainLayout.TabIndex = 0;
            // 
            // CalendarDaysCaption
            // 
            this.CalendarDaysCaption.AutoSize = true;
            this.CalendarDaysCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CalendarDaysCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.CalendarDaysCaption.Location = new System.Drawing.Point(3, 0);
            this.CalendarDaysCaption.Name = "CalendarDaysCaption";
            this.CalendarDaysCaption.Size = new System.Drawing.Size(328, 29);
            this.CalendarDaysCaption.TabIndex = 0;
            this.CalendarDaysCaption.Text = "* - жирным обозначены выходные дни. Клик на день для изменения этого параметра";
            this.CalendarDaysCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // CalendarDaysGrid
            // 
            this.CalendarDaysGrid.AllowUserToAddRows = false;
            this.CalendarDaysGrid.AllowUserToDeleteRows = false;
            this.CalendarDaysGrid.AllowUserToResizeRows = false;
            this.CalendarDaysGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.CalendarDaysGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CalendarDaysGrid.Location = new System.Drawing.Point(337, 255);
            this.CalendarDaysGrid.Name = "CalendarDaysGrid";
            this.CalendarDaysGrid.RowHeadersVisible = false;
            this.CalendarDaysGrid.Size = new System.Drawing.Size(328, 40);
            this.CalendarDaysGrid.TabIndex = 1;
            this.CalendarDaysGrid.Visible = false;
            this.CalendarDaysGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.CalendarDaysGrid_CurrentCellDirtyStateChanged);
            // 
            // CalendarFlowLayout1
            // 
            this.CalendarFlowLayout1.Controls.Add(this.monthCalendar);
            this.CalendarFlowLayout1.Controls.Add(this.MonthPeakHoursLabel);
            this.CalendarFlowLayout1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CalendarFlowLayout1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.CalendarFlowLayout1.Location = new System.Drawing.Point(3, 32);
            this.CalendarFlowLayout1.Name = "CalendarFlowLayout1";
            this.CalendarFlowLayout1.Size = new System.Drawing.Size(328, 217);
            this.CalendarFlowLayout1.TabIndex = 4;
            // 
            // monthCalendar
            // 
            this.monthCalendar.BackColor = System.Drawing.SystemColors.HotTrack;
            this.monthCalendar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.monthCalendar.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.monthCalendar.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.monthCalendar.Location = new System.Drawing.Point(9, 9);
            this.monthCalendar.MaxSelectionCount = 1;
            this.monthCalendar.Name = "monthCalendar";
            this.monthCalendar.ScrollChange = 1;
            this.monthCalendar.TabIndex = 4;
            this.monthCalendar.DateChanged += new System.Windows.Forms.DateRangeEventHandler(this.monthCalendar_DateChanged);
            this.monthCalendar.DateSelected += new System.Windows.Forms.DateRangeEventHandler(this.monthCalendar_DateSelected);
            // 
            // MonthPeakHoursLabel
            // 
            this.MonthPeakHoursLabel.AutoSize = true;
            this.MonthPeakHoursLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MonthPeakHoursLabel.Location = new System.Drawing.Point(7, 180);
            this.MonthPeakHoursLabel.Margin = new System.Windows.Forms.Padding(7, 0, 3, 0);
            this.MonthPeakHoursLabel.Name = "MonthPeakHoursLabel";
            this.MonthPeakHoursLabel.Size = new System.Drawing.Size(0, 17);
            this.MonthPeakHoursLabel.TabIndex = 5;
            // 
            // CalendarFlowLayout2
            // 
            this.CalendarFlowLayout2.Controls.Add(this.LowerPeakHourNumeric);
            this.CalendarFlowLayout2.Controls.Add(this.PeakHourLabel);
            this.CalendarFlowLayout2.Controls.Add(this.UpperPeakHourNumeric);
            this.CalendarFlowLayout2.Controls.Add(this.AddPeakHourButt);
            this.CalendarFlowLayout2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CalendarFlowLayout2.Location = new System.Drawing.Point(3, 255);
            this.CalendarFlowLayout2.Name = "CalendarFlowLayout2";
            this.CalendarFlowLayout2.Size = new System.Drawing.Size(328, 40);
            this.CalendarFlowLayout2.TabIndex = 5;
            // 
            // LowerPeakHourNumeric
            // 
            this.LowerPeakHourNumeric.Location = new System.Drawing.Point(3, 3);
            this.LowerPeakHourNumeric.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.LowerPeakHourNumeric.Name = "LowerPeakHourNumeric";
            this.LowerPeakHourNumeric.Size = new System.Drawing.Size(46, 20);
            this.LowerPeakHourNumeric.TabIndex = 2;
            // 
            // PeakHourLabel
            // 
            this.PeakHourLabel.AutoSize = true;
            this.PeakHourLabel.Location = new System.Drawing.Point(55, 0);
            this.PeakHourLabel.Name = "PeakHourLabel";
            this.PeakHourLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.PeakHourLabel.Size = new System.Drawing.Size(19, 17);
            this.PeakHourLabel.TabIndex = 1;
            this.PeakHourLabel.Text = "до";
            // 
            // UpperPeakHourNumeric
            // 
            this.UpperPeakHourNumeric.Location = new System.Drawing.Point(80, 3);
            this.UpperPeakHourNumeric.Maximum = new decimal(new int[] {
            24,
            0,
            0,
            0});
            this.UpperPeakHourNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.UpperPeakHourNumeric.Name = "UpperPeakHourNumeric";
            this.UpperPeakHourNumeric.Size = new System.Drawing.Size(45, 20);
            this.UpperPeakHourNumeric.TabIndex = 0;
            this.UpperPeakHourNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // AddPeakHourButt
            // 
            this.AddPeakHourButt.Location = new System.Drawing.Point(131, 3);
            this.AddPeakHourButt.Name = "AddPeakHourButt";
            this.AddPeakHourButt.Size = new System.Drawing.Size(163, 23);
            this.AddPeakHourButt.TabIndex = 3;
            this.AddPeakHourButt.Text = "Добавить период часов-пик";
            this.AddPeakHourButt.UseVisualStyleBackColor = true;
            this.AddPeakHourButt.Click += new System.EventHandler(this.AddPeakHourButt_Click);
            // 
            // CalendarForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(668, 298);
            this.Controls.Add(this.CalendarFormMainLayout);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CalendarForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Календарь";
            this.Load += new System.EventHandler(this.CalendarForm_Load);
            this.CalendarFormMainLayout.ResumeLayout(false);
            this.CalendarFormMainLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.CalendarDaysGrid)).EndInit();
            this.CalendarFlowLayout1.ResumeLayout(false);
            this.CalendarFlowLayout1.PerformLayout();
            this.CalendarFlowLayout2.ResumeLayout(false);
            this.CalendarFlowLayout2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LowerPeakHourNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.UpperPeakHourNumeric)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel CalendarFormMainLayout;
        private System.Windows.Forms.Label CalendarDaysCaption;
        private System.Windows.Forms.DataGridView CalendarDaysGrid;
        private System.Windows.Forms.FlowLayoutPanel CalendarFlowLayout1;
        private System.Windows.Forms.MonthCalendar monthCalendar;
        private System.Windows.Forms.Label MonthPeakHoursLabel;
        private System.Windows.Forms.FlowLayoutPanel CalendarFlowLayout2;
        private System.Windows.Forms.NumericUpDown UpperPeakHourNumeric;
        private System.Windows.Forms.Label PeakHourLabel;
        private System.Windows.Forms.NumericUpDown LowerPeakHourNumeric;
        private System.Windows.Forms.Button AddPeakHourButt;
    }
}