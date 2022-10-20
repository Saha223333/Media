using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace NewProject
{
    public partial class PhotoForm : Form
    {
        public PhotoForm()
        {
            InitializeComponent();
        }

        private void PhotoForm_DoubleClick(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SavePictureToDisk_Click(object sender, EventArgs e)
        {
            SavePictureToDisk2();
        }

        public void SavePictureToDisk2()
        {
            //здесь сохраняем картинку на комп
            SaveFileDialog svfdlg = new SaveFileDialog();
            if (svfdlg.ShowDialog() == DialogResult.OK)
            {
                Image bp = this.BackgroundImage;
                bp.Save(svfdlg.FileName + ".jpg", ImageFormat.Jpeg);
            }
        }
    }
}
