using System;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;


namespace NewProject
{
    public static class Utils
    {
        [DllImport("user32.dll")]//импортируем внешнюю библиотеку
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);//внешняя функция отправки windows message

        public static void DoubleBufferGrid(Control ctrl, bool setting)
        {//двойная буфферизация скрыта, поэтому приходится делать это вручную
            Type dgvType = ctrl.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(ctrl, setting, null);
        }

        public static int GetIndex(TreeNode node)
        {
            int returnValue = 0;

            if (node.Index == 0 && node.Parent == null)
                return returnValue;

            returnValue = 1;

            if (node.Index > 0)
            {
                TreeNode previousSibling = node.PrevNode;
                while (previousSibling != null)
                {
                    returnValue += GetDecendantCount(previousSibling);
                    previousSibling = previousSibling.PrevNode;
                }
            }

            if (node.Parent == null)
                return returnValue;
            else
                return returnValue + GetIndex(node.Parent);
        }

        private static int GetDecendantCount(TreeNode node)
        {
            int returnValue = 0;

            if (node.Index != 0 || node.Parent != null)
                returnValue = 1;

            if (node.Nodes.Count == 0)
                return returnValue;

            foreach (TreeNode childNode in node.Nodes)
            {
                returnValue += GetDecendantCount(childNode);
            }
            return returnValue;
        }

        public static void SuspendDrawing(Control control)
        {
            SendMessage(control.Handle, 11, false, 0);
        }

        public static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, 11, true, 0);
            control.Refresh();
        }

        public static void DragScroll(TreeView tv)
        {
            //процедура, позволяющая скроллить дерево при перетаскивании узлов
            const Single scrollRegion = 20;//определяем область в которой начинается скролл
            Point pt = tv.PointToClient(Cursor.Position);//получаем координаты курсора
            if ((pt.Y + scrollRegion) > tv.Height)//определяем нужно скроллить вниз или вверх
            {
                //посылаем сообщение скроллить вниз
                SendMessage(tv.Handle, (int)277, true, 0);
            }
            else if (pt.Y < (tv.Top + scrollRegion))
            {
                //посылаем сообщение скроллить вверх
                SendMessage(tv.Handle, (int)277, false, 0);
            }
        }

        public static void CreateFilterTextBoxes(DataGridView dgv)
        {//в этой процедуре создаём кастомные поля для ввода фильтра 
            dgv.Controls.Clear();
            BindingNavigator bn = new BindingNavigator(false);
            bn.GripStyle = ToolStripGripStyle.Hidden;//прячем ручку перетаскивания
            bn.CanOverflow = false;//запрещаем крайнему правому контролу прятаться 
            bn.Dock = DockStyle.None;//отвязываем якоря 
            bn.SetBounds(0, 21, dgv.Width, 0);//располагаем под заголовками столбцов
            dgv.Controls.Add(bn);
            //циклимся по столбцам грида
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (column.Index == 0) continue;//игнорируем столбец с идентификаторами
                ToolStripTextBox textbox = new ToolStripTextBox();//создаём текстовое поле
                textbox.Tag = column;//помещаем в него столбец (для процедуры фильтрации)  
                column.Tag = textbox;//в столбец помещаем текстовое поле (для изменения ширины)
                textbox.AutoSize = false;
                textbox.Width = column.Width;
                textbox.TextChanged += new EventHandler(DataBaseManagerMSSQL.FilterApply);
                textbox.BackColor = Color.Beige;
                textbox.BorderStyle = BorderStyle.FixedSingle;
                textbox.Margin = new Padding(0);
                bn.Items.Add(textbox);//добавляем текстовое поле
            }
        }

        public static void ExportDataGrid(DataGridView dg, string name, int firstColumnNumber)
        {   //процедура одиночной выгрузки данных для определённого грида 
            CommonReports cr = new CommonReports(dg);            

            cr.ExportToExcel(name, firstColumnNumber);
            cr.OpenAfterExport();           
        }

        public static DateTime Round(this DateTime date, TimeSpan span)
        {//округление даты
            long ticks = (date.Ticks + (span.Ticks / 2) + 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }
        public static DateTime Floor(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks / span.Ticks);
            return new DateTime(ticks * span.Ticks);
        }
        public static DateTime Ceil(this DateTime date, TimeSpan span)
        {
            long ticks = (date.Ticks + span.Ticks - 1) / span.Ticks;
            return new DateTime(ticks * span.Ticks);
        }

        public static bool Contains(string source, string textToFind, StringComparison compare)
        {//функция поиска подстроки в строке. Является заменой стандартной Contains т.к. та чувствительна к регистру, а здесь можно это отменить
            return source.IndexOf(textToFind, compare) >= 0;
        }
    }
}
