namespace NewProject
{
    partial class ReadingLogForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReadingLogForm));
            this.MainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.richText = new System.Windows.Forms.RichTextBox();
            this.CancelReadingThread = new System.Windows.Forms.Button();
            this.ReadingLogStrip = new System.Windows.Forms.ToolStrip();
            this.readingLogStripBar = new System.Windows.Forms.ToolStripProgressBar();
            this.CurrentProfileRecord = new System.Windows.Forms.ToolStripLabel();
            this.LastProfileRecord = new System.Windows.Forms.ToolStripLabel();
            this.MinimizeWindowsButton = new System.Windows.Forms.Button();
            this.MainLayout.SuspendLayout();
            this.ReadingLogStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 
            this.MainLayout.ColumnCount = 3;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.MainLayout.Controls.Add(this.richText, 0, 0);
            this.MainLayout.Controls.Add(this.CancelReadingThread, 1, 1);
            this.MainLayout.Controls.Add(this.ReadingLogStrip, 0, 1);
            this.MainLayout.Controls.Add(this.MinimizeWindowsButton, 2, 1);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.RowCount = 2;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.MainLayout.Size = new System.Drawing.Size(605, 740);
            this.MainLayout.TabIndex = 0;
            this.MainLayout.Paint += new System.Windows.Forms.PaintEventHandler(this.MainLayout_Paint);
            // 
            // richText
            // 
            this.MainLayout.SetColumnSpan(this.richText, 3);
            this.richText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richText.Location = new System.Drawing.Point(3, 3);
            this.richText.Name = "richText";
            this.richText.Size = new System.Drawing.Size(599, 697);
            this.richText.TabIndex = 1;
            this.richText.Text = "";
            // 
            // CancelReadingThread
            // 
            this.CancelReadingThread.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelReadingThread.Location = new System.Drawing.Point(487, 706);
            this.CancelReadingThread.Name = "CancelReadingThread";
            this.CancelReadingThread.Size = new System.Drawing.Size(50, 23);
            this.CancelReadingThread.TabIndex = 2;
            this.CancelReadingThread.Text = "Стоп";
            this.CancelReadingThread.UseVisualStyleBackColor = true;
            this.CancelReadingThread.Click += new System.EventHandler(this.button1_Click);
            // 
            // ReadingLogStrip
            // 
            this.ReadingLogStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.readingLogStripBar,
            this.CurrentProfileRecord,
            this.LastProfileRecord});
            this.ReadingLogStrip.Location = new System.Drawing.Point(0, 703);
            this.ReadingLogStrip.Name = "ReadingLogStrip";
            this.ReadingLogStrip.Size = new System.Drawing.Size(484, 25);
            this.ReadingLogStrip.TabIndex = 3;
            this.ReadingLogStrip.Text = "toolStrip1";
            // 
            // readingLogStripBar
            // 
            this.readingLogStripBar.ForeColor = System.Drawing.Color.ForestGreen;
            this.readingLogStripBar.Name = "readingLogStripBar";
            this.readingLogStripBar.Size = new System.Drawing.Size(400, 22);
            // 
            // CurrentProfileRecord
            // 
            this.CurrentProfileRecord.Name = "CurrentProfileRecord";
            this.CurrentProfileRecord.Size = new System.Drawing.Size(13, 22);
            this.CurrentProfileRecord.Text = "0";
            // 
            // LastProfileRecord
            // 
            this.LastProfileRecord.Name = "LastProfileRecord";
            this.LastProfileRecord.Size = new System.Drawing.Size(13, 22);
            this.LastProfileRecord.Text = "0";
            // 
            // MinimizeWindowsButton
            // 
            this.MinimizeWindowsButton.Location = new System.Drawing.Point(547, 706);
            this.MinimizeWindowsButton.Name = "MinimizeWindowsButton";
            this.MinimizeWindowsButton.Size = new System.Drawing.Size(54, 23);
            this.MinimizeWindowsButton.TabIndex = 4;
            this.MinimizeWindowsButton.Text = "Скрыть";
            this.MinimizeWindowsButton.UseVisualStyleBackColor = true;
            this.MinimizeWindowsButton.Visible = false;
            this.MinimizeWindowsButton.Click += new System.EventHandler(this.SkipCounterButton_Click);
            // 
            // ReadingLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelReadingThread;
            this.ClientSize = new System.Drawing.Size(605, 740);
            this.Controls.Add(this.MainLayout);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReadingLogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Лог опроса";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ReadingLogForm_FormClosing);
            this.Shown += new System.EventHandler(this.ReadingLogForm_Shown);
            this.MainLayout.ResumeLayout(false);
            this.MainLayout.PerformLayout();
            this.ReadingLogStrip.ResumeLayout(false);
            this.ReadingLogStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel MainLayout;
        public System.Windows.Forms.RichTextBox richText;
        private System.Windows.Forms.Button CancelReadingThread;
        private System.Windows.Forms.ToolStrip ReadingLogStrip;
        public System.Windows.Forms.ToolStripProgressBar readingLogStripBar;
        public System.Windows.Forms.ToolStripLabel CurrentProfileRecord;
        public System.Windows.Forms.ToolStripLabel LastProfileRecord;
        private System.Windows.Forms.Button MinimizeWindowsButton;
    }
}