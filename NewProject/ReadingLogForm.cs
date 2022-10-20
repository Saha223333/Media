using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace NewProject
{
    public partial class ReadingLogForm : Form
    {
        public BackgroundWorker bgrw;
        public bool closeAfterWork;
       // private const int CP_NOCLOSE_BUTTON = 0x200;
        public ReadingLogForm(ref BackgroundWorker pbgrw, bool close)
        {
            InitializeComponent();
            this.bgrw = pbgrw;
            this.closeAfterWork = close;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.bgrw.IsBusy == true)
            {
                DateTime currentDate = DateTime.Now;
                this.Invoke(new Action(delegate
                {
                    this.richText.SelectionColor = Color.DarkOrange;
                    this.richText.AppendText(currentDate + "." + currentDate.Millisecond + " Пользователь вручную остановил опрос\r");
                    this.richText.ScrollToCaret();
                }));
                bgrw.CancelAsync();
            }
        }

        private void MainLayout_Paint(object sender, PaintEventArgs e)
        {

        }

        private void SkipCounterButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void ReadingLogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.bgrw.IsBusy == true) e.Cancel = true;//если поток чтения не прекратил работу, то форму нельзя закрывать (по крестику)
        }

        private void ReadingLogForm_Shown(object sender, EventArgs e)
        {
            FormCollection fc = Application.OpenForms;//получаем текущую коллекцию форм

            foreach (Form f in fc)//циклимся по коллекции форм программы
            {
                if (f.GetType() == typeof(ReadingLogForm))
                {
                    this.Left += 30;
                    this.Top += 35;
                }
            }
        }
    }
}
