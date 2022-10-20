namespace NewProject
{
    partial class JournalCQCGridForm
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
            this.OnlyExportToExcelContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ExportToExcelButt = new System.Windows.Forms.ToolStripMenuItem();
            this.OnlyExportToExcelContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // OnlyExportToExcelContext
            // 
            this.OnlyExportToExcelContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExportToExcelButt});
            this.OnlyExportToExcelContext.Name = "OnlyExportToExcelContext";
            this.OnlyExportToExcelContext.Size = new System.Drawing.Size(158, 48);
            // 
            // ExportToExcelButt
            // 
            this.ExportToExcelButt.Name = "ExportToExcelButt";
            this.ExportToExcelButt.Size = new System.Drawing.Size(157, 22);
            this.ExportToExcelButt.Text = "Экспорт в Excel";
            this.ExportToExcelButt.Click += new System.EventHandler(this.ExportToExcelButt_Click);
            // 
            // JournalCQCGridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 657);
            this.Name = "JournalCQCGridForm";
            this.Text = "Журнал ПКЭ";
            this.OnlyExportToExcelContext.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip OnlyExportToExcelContext;
        private System.Windows.Forms.ToolStripMenuItem ExportToExcelButt;
    }
}