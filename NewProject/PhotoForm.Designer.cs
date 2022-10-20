namespace NewProject
{
    partial class PhotoForm
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
            this.PhotoFormContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.SavePictureToDisk = new System.Windows.Forms.ToolStripMenuItem();
            this.PhotoFormContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // PhotoFormContext
            // 
            this.PhotoFormContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SavePictureToDisk});
            this.PhotoFormContext.Name = "PhotoFormContext";
            this.PhotoFormContext.ShowImageMargin = false;
            this.PhotoFormContext.Size = new System.Drawing.Size(152, 48);
            // 
            // SavePictureToDisk
            // 
            this.SavePictureToDisk.Name = "SavePictureToDisk";
            this.SavePictureToDisk.Size = new System.Drawing.Size(151, 22);
            this.SavePictureToDisk.Text = "Сохранить на диск";
            this.SavePictureToDisk.Click += new System.EventHandler(this.SavePictureToDisk_Click);
            // 
            // PhotoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.ContextMenuStrip = this.PhotoFormContext;
            this.Name = "PhotoForm";
            this.Text = "Фотография";
            this.DoubleClick += new System.EventHandler(this.PhotoForm_DoubleClick);
            this.PhotoFormContext.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip PhotoFormContext;
        private System.Windows.Forms.ToolStripMenuItem SavePictureToDisk;
    }
}