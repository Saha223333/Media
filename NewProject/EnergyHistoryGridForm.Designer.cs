namespace NewProject
{
    partial class EnergyHistoryGridForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnergyHistoryGridForm));
            this.OnlyExportToExcelToolStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OnlyExport = new System.Windows.Forms.ToolStripMenuItem();
            this.OnlyExportToExcelToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // OnlyExportToExcelToolStrip
            // 
            this.OnlyExportToExcelToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OnlyExport});
            this.OnlyExportToExcelToolStrip.Name = "OnlyExportToExcelToolStrip";
            this.OnlyExportToExcelToolStrip.Size = new System.Drawing.Size(158, 26);
            // 
            // OnlyExport
            // 
            this.OnlyExport.Name = "OnlyExport";
            this.OnlyExport.Size = new System.Drawing.Size(157, 22);
            this.OnlyExport.Text = "Экспорт в Excel";
            this.OnlyExport.Click += new System.EventHandler(this.OnlyExport_Click);
            // 
            // EnergyHistoryGridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1021, 591);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EnergyHistoryGridForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "История энергии";
            this.OnlyExportToExcelToolStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip OnlyExportToExcelToolStrip;
        private System.Windows.Forms.ToolStripMenuItem OnlyExport;
    }
}