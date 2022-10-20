using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Office.Interop.Excel;


namespace NewProject
{
    public class CommonReports
    {
        private Microsoft.Office.Interop.Excel.Application excel;
        private Workbook workbook;
        private Worksheet worksheet;
        private DataGridView datagrid;
        private RichTextBox richText;
        private System.Data.DataTable dt;

        public CommonReports(System.Data.DataTable pdt)
        {   //в этом конструкторе будет таблица из БД
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Add(Type.Missing);
            this.worksheet = null;
            this.dt = pdt;
        }

        public CommonReports(DataGridView pdatagrid)
        {   //создаем объекты Excel для методов выгрузки.
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Add(Type.Missing);
            this.worksheet = null;
            this.datagrid = pdatagrid; 
        }

        public CommonReports(DataGridView pdatagrid, RichTextBox prichText)
        {   //создаем объекты Excel для методов выгрузки. Создаются в конструкторе чтобы в цикле вызывать процедуру выгрузки и все выгруженные объекты были в одной книге
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Add(Type.Missing);
            this.worksheet = null; datagrid = pdatagrid;
            this.richText = prichText;
        }

        public CommonReports(string PathToTemplate, RichTextBox prichText)
        {   //этот конструктор принимает в качестве аргумента путь к существующей книге для подгрузки шаблона
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Open(PathToTemplate);
            this.worksheet = workbook.Worksheets[1];
            this.richText = prichText;
        }

        public CommonReports(RichTextBox prichText)
        {   //создаем объекты Excel для методов выгрузки. Создаются в конструкторе чтобы в цикле вызывать процедуру выгрузки и все выгруженные объекты были в одной книге
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Add(Type.Missing);
            this.worksheet = null; richText = prichText;
        }

        public CommonReports(string workbookname)
        {//здесь содаётся пустая книга с именем workbookname
            this.excel = new Microsoft.Office.Interop.Excel.Application();
            this.workbook = excel.Workbooks.Add(Type.Missing);
            this.worksheet = null;
            this.workbook.Sheets.Add();
            this.worksheet = workbook.ActiveSheet;
            this.worksheet.Name = workbookname; 
        }

        public CommonReports()
        {   
            
        }

        public void ExportEnergyPLC(int rowcount, DataGridView dgvr, string additional_info)
        {//здесь кидаем все последние показания на один лист                 
            try
            {//рисуем заголовок (тянем из грида)
                for (int i = 3; i < dgvr.Columns.Count; i++)
                {
                    if (dgvr.Columns[i].Visible == false) continue;//если колонка грида не видима, то игнорируем её при выгрузке
                    worksheet.Cells[1, i - 2] = dgvr.Columns[i].HeaderText; worksheet.Cells[1, i - 2].Interior.Color = Color.Yellow;
                    worksheet.Cells[1, i - 2].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                }
                //заполняем данными из грида 
                for (int i = 0; i < dgvr.Rows.Count; i++)
                    {
                        for (int j = 3; j < dgvr.Columns.Count; j++)
                        {
                            worksheet.Cells[i + 2 + rowcount, dgvr.Columns.Count - 2] = additional_info;
                            //если колонка грида не видима, то игнорируем её значения при выгрузке
                            if (dgvr.Columns[j].Visible == false) continue;
                            //если в ячейке пусто, то пишем 0
                            if (dgvr.Rows[i].Cells[j].Value == null)
                            {
                                worksheet.Cells[i + 2 + rowcount, j - 2] = "0";
                            }
                            else
                            {   //иначе заносим значение
                                worksheet.Cells[i + 2 + rowcount, j - 2] = dgvr.Rows[i].Cells[j].Value;
                            }                           
                        }                      
                    }
                    worksheet.Columns.AutoFit();               
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
        }

        public void OpenAfterExport()
        {
            excel.Visible = true;
        }

        public void ExportToExcel()
        {//здесь выгружаем на лист таблицу из БД
            try
            {
                workbook.Sheets.Add();
                worksheet = workbook.ActiveSheet;
                //заголовки
                worksheet.Cells[1, 1] = "Район"; worksheet.Cells[1, 1].Interior.Color = Color.Yellow; 
                worksheet.Cells[1, 2] = "Номер"; worksheet.Cells[1, 2].Interior.Color = Color.Yellow;
                worksheet.Cells[1, 3] = "Сообщение"; worksheet.Cells[1, 3].Interior.Color = Color.Yellow;
                worksheet.Cells[1, 4] = "Комментарий"; worksheet.Cells[1, 4].Interior.Color = Color.Yellow;

                for (int i = 0; i < this.dt.Rows.Count; i++)
                {
                    for (int j = 0; j < this.dt.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 2, j + 1] = this.dt.Rows[i][j];                        
                    }
                }
                worksheet.Columns.AutoFit();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
        }
    
        public void ExportToExcel(string text, int firstColumn)
        {//в этой процедуре на листе книги рисуется произвольный грид 
            try
            {
                workbook.Sheets.Add();
                worksheet = workbook.ActiveSheet;
                string textBuffer = text; //буфферная текстовая переменная для анализа запрещенных символов в названии листа
                char[] forbiddenChars = { (char)0x005c, '/', '*', '[', ']', ':', '?', ';' };//запрещенные в названии листа символов              
                foreach (char c in forbiddenChars) { textBuffer = textBuffer.Replace(c, ' '); }//в цикле заменяем все запрещенные символы пробелами
                //если длина строки превышает 31 символ, то отрезаем лишние, т.к. в имени листа книги 
                //не должно быть более 31 символа
                if (textBuffer.Length > 31) { textBuffer = textBuffer.Remove(31); }
                //именуем лист по имени узла после удаления запрещенных символов
                worksheet.Name = textBuffer;
                //сначала создаём заголовки
                for (int i = firstColumn; i < datagrid.Columns.Count; i++)
                {
                    if (datagrid.Columns[i].Visible == false) continue;//если колонка грида не видима, то игнорируем её при выгрузке
                    worksheet.Cells[1, i] = datagrid.Columns[i].HeaderText;
                    worksheet.Cells[1, i].Interior.Color = Color.Yellow;
                    worksheet.Cells[1, i].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                }
                //заполняем данными из сетки 
                for (int i = 0; i < datagrid.Rows.Count; i++)
                {
                    for (int j = firstColumn; j < datagrid.Columns.Count; j++)
                    {
                        //если колонка грида не видима, то игнорируем её значения при выгрузке
                        if (datagrid.Columns[j].Visible == false) continue;
                        //если в ячейке пусто, то пишем 0
                        if (datagrid.Rows[i].Cells[j].Value == null)
                        {
                            worksheet.Cells[i + 2, j] = "0";
                        }
                        else
                        {   //иначе заносим значение
                            worksheet.Cells[i + 2, j] = datagrid.Rows[i].Cells[j].Value;
                        }
                    }
                }
                worksheet.Columns.AutoFit();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
        }

        public void ExportToExcel(TreeNode treenode)
        {//в этой процедуре на листе книги формируется грид для узла treenode          
            try
            {
                workbook.Sheets.Add(); worksheet = workbook.ActiveSheet;
                string textBuffer = treenode.Text; //буфферная текстовая переменная для анализа запрещенных символов в названии листа
                char[] forbiddenChars = { (char)0x005c, '/', '*', '[', ']', ':', '?', ';' };//запрещенные в названии листа символов              
                foreach (char c in forbiddenChars) { textBuffer = textBuffer.Replace(c, ' '); }//в цикле заменяем все запрещенные символы пробелами
                //если длина строки превышает 31 символ, то отрезаем лишние, т.к. в имени листа книги 
                //не должно быть более 31 символа
                if (textBuffer.Length > 31) { textBuffer = textBuffer.Remove(31); }
                //именуем лист по имени узла после удаления запрещенных символов
                worksheet.Name = textBuffer;
                //сначала создаём заголовки
                for (int i = 3; i < datagrid.Columns.Count; i++)
                {
                    if (datagrid.Columns[i].Visible == false) continue;//если колонка грида не видима, то игнорируем её при выгрузке
                    worksheet.Cells[1, i - 2] = datagrid.Columns[i].HeaderText; worksheet.Cells[1, i - 2].Interior.Color = Color.Yellow;
                    worksheet.Cells[1, i - 2].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                }
                //заполняем данными из сетки 
                for (int i = 0; i < datagrid.Rows.Count; i++)
                {
                    for (int j = 3; j < datagrid.Columns.Count; j++)
                    {
                        //если колонка грида не видима, то игнорируем её значения при выгрузке
                        if (datagrid.Columns[j].Visible == false) continue;
                        //если в ячейке пусто, то пишем 0
                        if (datagrid.Rows[i].Cells[j].Value == null)
                        {
                            worksheet.Cells[i + 2, j - 2] = "0";
                        }
                        else
                        {   //иначе заносим значение
                            worksheet.Cells[i + 2, j - 2] = datagrid.Rows[i].Cells[j].Value;
                        }
                    }
                }
                worksheet.Columns.AutoFit();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return; }
        }

        public void ExportToExcel(List<TreeNode> treenodes)
        {   //процедура выгрузки выбранных параметров по группе точек                  
            //выгрузка параметров
            {
                workbook.Sheets.Add();
                worksheet = workbook.ActiveSheet; worksheet.Name = "Параметры";
                worksheet.Cells[1, 1].Interior.Color = Color.Yellow;
                worksheet.Cells[1, 2].Interior.Color = Color.Yellow;
                //создаём заголовки на листе
                worksheet.Cells[2, 1] = "Наименование"; worksheet.Cells[2, 2] = "Серийный номер"; //строка, столбец 
                worksheet.Cells[2, 1].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 2].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                int i = 3; int j = 3;
                //циклимся по коллекции поданых в процедуру узлов
                foreach (TreeNode node in treenodes)
                {
                    ICounter counter = (ICounter)node.Tag;
                    worksheet.Cells[i, 1] = counter.Name; worksheet.Cells[i, 2] = counter.SerialNumber;
                    worksheet.Cells[i, 1].Interior.Color = Color.GreenYellow; worksheet.Cells[i, 2].Interior.Color = Color.GreenYellow;
                    //цикл по параметрам энергии
                    //делаем запрос только на те параметры, которые отмечены
                    //var checkedParameters = from param in counter.ParametersToRead where param.check == true select param;
                    foreach (CounterParameterToRead param in counter.ParametersToRead)
                    {
                        if (param.check == true)
                        {//выгружаем помеченные параметры
                            worksheet.Cells[1, j] = param.name;//создание заголовка для параметра попадает в цикл потому что он (параметр) создаётся по условию check
                            worksheet.Cells[1, j].HorizontalAlignment = XlHAlign.xlHAlignCenter; worksheet.Cells[1, j].Interior.Color = Color.Yellow;
                            worksheet.Cells[2, j] = "Значение"; worksheet.Cells[i, j] = param.value; worksheet.Cells[2, j].Interior.Color = Color.Yellow; worksheet.Cells[2, j].HorizontalAlignment = XlHAlign.xlHAlignCenter;//сумма 
                        }
                        j += 1;
                    }
                    worksheet.Cells[2, j + 4] = "EOF";//метка конца параметров счётчика (End Of File). Нужна для удаления пустых столбцов вплоть до этой метки
                    i += 1; j = 3;
                }
                //пробежимся и удалим пустые столбцы.Если столбец пустой это значит что параметр не был выбран на выгрузку. Если заголовка нет - удаляем столбец
                int k = 1; string txt = "";
                while (txt != "EOF")//цикл пока не дошли до метки конца
                {
                    txt = worksheet.Cells[2, k].Text;
                    if (txt == "")//как только наткнулись на первую пустую ячейку начинаем удалять вплоть до ячейки с текстом
                    {
                        while (txt == "")
                        { worksheet.Columns[k].Delete(); txt = worksheet.Cells[2, k].Text; }
                    }
                    k += 1;
                }
                worksheet.Columns.AutoFit();
            }
            //вкладка журнала
            {
                workbook.Sheets.Add();
                worksheet = workbook.ActiveSheet; worksheet.Name = "Журнал.";
                worksheet.Cells[1, 1].Interior.Color = Color.Yellow; worksheet.Cells[1, 2].Interior.Color = Color.Yellow;
                //создаём заголовки на листе
                worksheet.Cells[2, 1] = "Наименование"; worksheet.Cells[2, 2] = "Серийный номер"; //строка, столбец 
                worksheet.Cells[2, 1].Interior.Color = Color.Yellow; worksheet.Cells[2, 2].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                int i = 3; int j = 3;
                //циклимся по коллекции поданных в процедуру узлов
                foreach (TreeNode node in treenodes)
                {
                    ICounter counter = (ICounter)node.Tag;
                    worksheet.Cells[i, 1] = counter.Name; worksheet.Cells[i, 2] = counter.SerialNumber; worksheet.Cells[i, 2].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                    worksheet.Cells[i, 1].Interior.Color = Color.GreenYellow; worksheet.Cells[i, 2].Interior.Color = Color.GreenYellow;
                    //делаем запрос только на те параметры, которые отмечены
                    //var checkedJournals = from journal in counter.JournalToRead where journal.check == true select journal;
                    //цикл по параметрам энергии
                    foreach (CounterJournalToRead journal in counter.JournalToRead)
                    {
                        if (journal.check == true)
                        {//выгружаем помеченные параметры
                            worksheet.Cells[1, j] = journal.name;//создание заголовка для параметра попадает в цикл потому что он (параметр) создаётся по условию check
                            worksheet.Cells[1, j].HorizontalAlignment = XlHAlign.xlHAlignCenter; worksheet.Cells[1, j].Interior.Color = Color.Yellow;
                            worksheet.Range[worksheet.Cells[1, j], worksheet.Cells[1, j + 9]].Merge();//объединяем ячейки в единый заголовок
                            worksheet.Cells[2, j] = "Запись 1"; worksheet.Cells[i, j] = journal.record1; worksheet.Cells[2, j].Interior.Color = Color.Yellow; worksheet.Cells[2, j].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 1] = "Запись 2"; worksheet.Cells[i, j + 1] = journal.record2; worksheet.Cells[2, j + 1].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;//запись 1 
                            worksheet.Cells[2, j + 2] = "Запись 3"; worksheet.Cells[i, j + 2] = journal.record3; worksheet.Cells[2, j + 2].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 2].HorizontalAlignment = XlHAlign.xlHAlignCenter;//..
                            worksheet.Cells[2, j + 3] = "Запись 4"; worksheet.Cells[i, j + 3] = journal.record4; worksheet.Cells[2, j + 3].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 3].HorizontalAlignment = XlHAlign.xlHAlignCenter;//..
                            worksheet.Cells[2, j + 4] = "Запись 5"; worksheet.Cells[i, j + 4] = journal.record5; worksheet.Cells[2, j + 4].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 4].HorizontalAlignment = XlHAlign.xlHAlignCenter;//..                                        
                            worksheet.Cells[2, j + 5] = "Запись 6"; worksheet.Cells[i, j + 5] = journal.record6; worksheet.Cells[2, j + 5].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 5].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 6] = "Запись 7"; worksheet.Cells[i, j + 6] = journal.record7; worksheet.Cells[2, j + 6].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 6].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 7] = "Запись 8"; worksheet.Cells[i, j + 7] = journal.record8; worksheet.Cells[2, j + 7].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 7].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 8] = "Запись 9"; worksheet.Cells[i, j + 8] = journal.record9; worksheet.Cells[2, j + 8].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 8].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 9] = "Запись 10"; worksheet.Cells[i, j + 9] = journal.record10; worksheet.Cells[2, j + 9].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 9].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            //заголовки в рамки
                            worksheet.Cells[i, j + 9].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[i, j + 9].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                            worksheet.Cells[1, j].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[1, j].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                            worksheet.Cells[2, j].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[2, j].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                            worksheet.Cells[1, j + 9].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[1, j + 9].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                            worksheet.Cells[2, j + 9].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[2, j + 9].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                        }
                        j += 10;//наращиваем номер столбца на 5 чтобы заполнять следующий параметр
                    }
                    worksheet.Cells[2, j + 9] = "EOF";//метка конца параметров счётчика (End Of File). Нужна для удаления пустых столбцов вплоть до этой метки
                    i += 1; j = 3;
                }
                //пробежимся и удалим пустые столбцы.Если столбец пустой это значит что параметр не был выбран на выгрузку. Если заголовка нет - удаляем столбец
                int k = 1; string txt = "";
                while (txt != "EOF")//цикл пока не дошли до метки конца
                {
                    txt = worksheet.Cells[2, k].Text;
                    if (txt == "")//как только наткнулись на первую пустую ячейку начинаем удалять вплоть до ячейки с текстом
                    {
                        while (txt == "")
                        { worksheet.Columns[k].Delete(); txt = worksheet.Cells[2, k].Text; }
                    }
                    k += 1;
                }
                worksheet.Columns.AutoFit();
            }
            //далее монитор
            {
                workbook.Sheets.Add(); worksheet = workbook.ActiveSheet; worksheet.Name = "Монитор";
                worksheet.Cells[1, 1].Interior.Color = Color.Yellow; worksheet.Cells[1, 2].Interior.Color = Color.Yellow;
                //создаём заголовки на листе
                worksheet.Cells[2, 1] = "Наименование"; worksheet.Cells[2, 2] = "Серийный номер"; //строка, столбец 
                worksheet.Cells[2, 1].Interior.Color = Color.Yellow; worksheet.Cells[2, 2].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                int i = 3; int j = 3;
                //циклимся по коллекции поданных в процедуру узлов
                foreach (TreeNode node in treenodes)
                {
                    ICounter counter = (ICounter)node.Tag;
                    worksheet.Cells[i, 1] = counter.Name; worksheet.Cells[i, 2] = counter.SerialNumber;
                    worksheet.Cells[i, 1].Interior.Color = Color.GreenYellow; worksheet.Cells[i, 2].Interior.Color = Color.GreenYellow;
                    //цикл по параметрам энергии
                    foreach (CounterMonitorParameterToRead monitor in counter.MonitorToRead)
                    {
                        {//выгружаем помеченные параметры
                            worksheet.Cells[1, j] = monitor.name;//создание заголовка для параметра попадает в цикл потому что он (параметр) создаётся по условию check
                            worksheet.Cells[1, j].HorizontalAlignment = XlHAlign.xlHAlignCenter; worksheet.Cells[1, j].Interior.Color = Color.Yellow;
                            worksheet.Range[worksheet.Cells[1, j], worksheet.Cells[1, j + 3]].Merge();//объединяем ячейки в единый заголовок

                            worksheet.Cells[2, j] = "Сумма"; worksheet.Cells[i, j] = monitor.phase0; worksheet.Cells[2, j].Interior.Color = Color.Yellow; worksheet.Cells[2, j].HorizontalAlignment = XlHAlign.xlHAlignCenter;//сумма 
                            worksheet.Cells[2, j + 1] = "Фвза 1"; worksheet.Cells[i, j + 1] = monitor.phase1; worksheet.Cells[2, j + 1].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;//фаза 1 
                            worksheet.Cells[2, j + 2] = "Фвза 2"; worksheet.Cells[i, j + 2] = monitor.phase2; worksheet.Cells[2, j + 2].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 2].HorizontalAlignment = XlHAlign.xlHAlignCenter;//фаза 2
                            worksheet.Cells[2, j + 3] = "Фвза 3"; worksheet.Cells[i, j + 3] = monitor.phase3; worksheet.Cells[2, j + 3].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 3].HorizontalAlignment = XlHAlign.xlHAlignCenter;//фаза 3                         
                        }
                        j += 4;//наращиваем номер столбца на 5 чтобы заполнять следующий параметр
                    }
                    worksheet.Cells[2, j + 4] = "EOF";//метка конца параметров счётчика (End Of File). Нужна для удаления пустых столбцов вплоть до этой метки
                    i += 1; j = 3;
                }

                //пробежимся и удалим пустые столбцы.Если столбец пустой это значит что параметр не был выбран на выгрузку. Если заголовка нет - удаляем столбец
                int k = 1; string txt = "";
                while (txt != "EOF")//цикл пока не дошли до метки конца
                {
                    txt = worksheet.Cells[2, k].Text;
                    if (txt == "")//как только наткнулись на первую пустую ячейку начинаем удалять вплоть до ячейки с текстом
                    {
                        while (txt == "")
                        { worksheet.Columns[k].Delete(); txt = worksheet.Cells[2, k].Text; }
                    }
                    k += 1;
                }
                worksheet.Columns.AutoFit();
            }
            //вкладка энергии
            {
                workbook.Sheets.Add();
                worksheet = workbook.ActiveSheet; worksheet.Name = "Энергия";
                worksheet.Cells[1, 1+1].Interior.Color = Color.Yellow; worksheet.Cells[1, 2+1].Interior.Color = Color.Yellow;
                //создаём заголовки на листе
                worksheet.Cells[2, 0+1] = "Объект"; worksheet.Cells[2, 0+1].Interior.Color = Color.Yellow; worksheet.Cells[1, 0+1].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 1+1] = "Наименование"; worksheet.Cells[2, 2+1] = "Серийный номер"; //строка, столбец 
                worksheet.Cells[2, 1+1].Interior.Color = Color.Yellow; worksheet.Cells[2, 2+1].Interior.Color = Color.Yellow;
                worksheet.Cells[2, 1+1].HorizontalAlignment = XlHAlign.xlHAlignCenter; worksheet.Cells[2, 0 + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                int i = 3; int j = 4;
                //циклимся по коллекции поданных в процедуру узлов
                foreach (TreeNode node in treenodes)
                {
                    ICounter counter = (ICounter)node.Tag; 
                    System.Data.DataTable connection_dt = DataBaseManagerMSSQL.Return_Connection_Row(counter.ParentID);//нужно получить наименование ТП
                    worksheet.Cells[i, 1+1] = counter.Name; worksheet.Cells[i, 2+1] = counter.SerialNumber; worksheet.Cells[i, 2+1].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                    worksheet.Cells[i, 1+1].Interior.Color = Color.GreenYellow; worksheet.Cells[i, 2+1].Interior.Color = Color.GreenYellow;
                    worksheet.Cells[i, 1] = connection_dt.Rows[0][1].ToString(); worksheet.Cells[i, 1].Interior.Color = Color.GreenYellow;//наименование объекта (ТП)
                    //цикл по параметрам энергии
                    foreach (CounterEnergyToRead energy in counter.EnergyToRead)
                    {
                        if (energy.check == true)
                        {//выгружаем помеченные параметры
                            worksheet.Cells[1, j] = energy.name;//создание заголовка для параметра попадает в цикл потому что он (параметр) создаётся по условию check
                            worksheet.Cells[1, j].HorizontalAlignment = XlHAlign.xlHAlignCenter; worksheet.Cells[1, j].Interior.Color = Color.Yellow;
                            worksheet.Range[worksheet.Cells[1, j], worksheet.Cells[1, j + 5]].Merge();//объединяем ячейки в единый заголовок
                            worksheet.Cells[2, j] = "Сумма"; worksheet.Cells[i, j] = energy.lastValueZone0; worksheet.Cells[2, j].Interior.Color = Color.Yellow; worksheet.Cells[2, j].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                            worksheet.Cells[2, j + 1] = "Тариф 1"; worksheet.Cells[i, j + 1] = energy.lastValueZone1; worksheet.Cells[2, j + 1].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 1].HorizontalAlignment = XlHAlign.xlHAlignCenter;//тариф 1 
                            worksheet.Cells[2, j + 2] = "Тариф 2"; worksheet.Cells[i, j + 2] = energy.lastValueZone2; worksheet.Cells[2, j + 2].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 2].HorizontalAlignment = XlHAlign.xlHAlignCenter;//тариф 2
                            worksheet.Cells[2, j + 3] = "Тариф 3"; worksheet.Cells[i, j + 3] = energy.lastValueZone3; worksheet.Cells[2, j + 3].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 3].HorizontalAlignment = XlHAlign.xlHAlignCenter;//тариф 3
                            worksheet.Cells[2, j + 4] = "Тариф 4"; worksheet.Cells[i, j + 4] = energy.lastValueZone4; worksheet.Cells[2, j + 4].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 4].HorizontalAlignment = XlHAlign.xlHAlignCenter;//тариф 4                                         
                            worksheet.Cells[2, j + 5] = "Дата\\время последнего считывания";
                            worksheet.Cells[i, j + 5] = energy.lastTime; worksheet.Cells[2, j + 5].Interior.Color = Color.Yellow; worksheet.Cells[2, j + 5].HorizontalAlignment = XlHAlign.xlHAlignCenter;//время
                            worksheet.Cells[i, j + 5].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[i, j + 5].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                            //рамки
                            worksheet.Cells[1, j + 1].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[1, j + 1].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                            worksheet.Cells[2, j + 1].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[2, j + 1].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                            worksheet.Cells[1, j + 6].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[1, j + 6].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                            worksheet.Cells[2, j + 6].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[2, j + 6].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                        }
                        j += 6;//наращиваем номер столбца чтобы заполнять следующий параметр
                    }
                    worksheet.Cells[2, j + 5] = "EOF";//метка конца параметров счётчика (End Of File). Нужна для удаления пустых столбцов вплоть до этой метки
                    i += 1; j = 4;//обновляем счётчики чтобы начать по-новой для следующего счётчика
                }
                //пробежимся и удалим пустые столбцы. Если столбец пустой это значит что параметр не был выбран на выгрузку. Если заголовка нет - удаляем столбец
                int k = 1; string txt = "";
                while (txt != "EOF")//цикл пока не дошли до метки конца
                {
                    txt = worksheet.Cells[2, k].Text;
                    if (txt == "")//как только наткнулись на первую пустую ячейку начинаем удалять вплоть до ячейки с текстом
                    {
                        while (txt == "")
                        { worksheet.Columns[k].Delete(); txt = worksheet.Cells[2, k].Text; }
                    }
                    k += 1;
                }
                worksheet.Columns.AutoFit();
            }
        }

        public void DisposeExcel()
        {
            excel.Quit(); workbook = null; excel = null;
        }

        public void SaveWorkBook(string bookName)
        {
            //сохраняем книгу. Версия процедуры с вызовом диалога с ручным указанием пути
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.OverwritePrompt = false;
            sfd.Filter = "Файлы Excel (*.xlsx)|*.xlsx|Файлы Excel (*.xls)|*.xls|All files (*.*)|*.*";
            sfd.FilterIndex = 2;
            sfd.FileName = bookName;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                workbook.SaveAs(sfd.FileName);
                MessageBox.Show("Выгрузка завершена", "Выгрузка в Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            DisposeExcel();
        }

        public string SaveWorkBook(string bookName, string path)
        {
            //сохраняем книгу. Версия процедуры без вызова диалога с автоматическим указанием пути           
            workbook.SaveAs(path + bookName);//сохраняем книгу на жёстком диске
            string fullname = workbook.FullName;
            DisposeExcel();//избавляемся от экземпляра экселя
            return fullname;//возвращаем полный путь к сохранённой книге
        }

        public void FormActualPowerReport(List<TreeNode> treenodes, string month, DateTime DateN, DateTime DateK, int hoursshift)
        {   //строка, столбец
          
            //возвращаем количество рабочих дней в текущем месяце
            System.Data.DataTable dt = DataBaseManagerMSSQL.Return_Calendar(DateN, DateK);
           
            //создаём отдельный лист для каждого счётчика
            foreach (TreeNode treenode in treenodes)
            {
                ICounter counter = (ICounter)treenode.Tag;
                string textBuffer = treenode.Text; //буфферная текстовая переменная для анализа запрещенных символов в названии листа
                char[] forbiddenChars = { (char)0x005c, '/', '*', '[', ']', ':', '?', ';' };//запрещенные в названии листа символов              
                foreach (char c in forbiddenChars) { textBuffer = textBuffer.Replace(c, ' '); }//в цикле заменяем все запрещенные символы пробелами
                //если длина строки превышает 31 символ, то отрезаем лишние, т.к. в имени листа книги 
                //не должно быть более 31 символа
                if (textBuffer.Length > 31) { textBuffer = textBuffer.Remove(31); }
                workbook.Sheets.Add(); worksheet = workbook.ActiveSheet; worksheet.Name = textBuffer;

                worksheet.Cells[1, 1] = "Акт учёта перетоков";
                worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[1, 31]].Merge();

                worksheet.Cells[4, 1] = "Транзит СКЭК";
                worksheet.Range[worksheet.Cells[4, 1], worksheet.Cells[4, dt.Rows.Count]].Merge();

                worksheet.Cells[5, 2] = "Почасовое сальдо перетоков в сечении";
                worksheet.Range[worksheet.Cells[5, 2], worksheet.Cells[5, dt.Rows.Count]].Merge();

                worksheet.Cells[5, 1] = "Дата\\время";
                worksheet.Range[worksheet.Cells[5, 1], worksheet.Cells[6, 1]].Merge();              
                
                //инициализируем список часов-пик для последующего использования
                //List<byte> peak_hours_list = new List<byte>();
                //нужно пройтись по строке с часами-пик, чтобы составить список
                int pos_separator = 0;//позиция символа-разделителя
              
                //рисуем заголовки часов
                for (int i = 1; i <= 24; i++)
                {
                    worksheet.Cells[6 + i, 1] = i-1 +"-"+ i;
                    worksheet.Range[worksheet.Cells[7, 1], worksheet.Cells[30, 1]].NumberFormat = "@";
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[6 + i, 1].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                }
                //заголовок строки с итогами по дням (суммой часовых расходов)
                worksheet.Cells[31, 1] = "Итого";
                //возвращаем строку часов-пик для выбранного месяца
                
                //идём по таблице дней месяца (календарю)
                for (int k = 0; k < dt.Rows.Count - 1; k++)
                {
                    string month_peak_hours = DataBaseManagerMSSQL.Return_Month_Peak_Hours(DateN.Month);
                    DateTime datatimeN = Convert.ToDateTime(dt.Rows[k]["day"]);
                    if (hoursshift < 0) datatimeN = datatimeN.AddHours(24 + hoursshift);//если сдвигаем часы, то нужно это учесть в счётчике
                    if (hoursshift > 0) datatimeN = datatimeN.AddHours(hoursshift);//если сдвигаем часы, то нужно это учесть в счётчике
                    DateTime datatimeK = datatimeN.AddDays(1);
                    //фильтруем таблицу профиля по дню (начало суток и конец)            
                    DataRow[] day_profile = counter.ProfileDataTable.Select("date_time >'" + datatimeN.ToString() + "' and date_time <= '" + datatimeK.ToString() + "'");
                    //переносим выборку в новую таблицу чтобы учесть типы данных (сортировка по дате в массиве строк DataRow[] не работает)
                    System.Data.DataTable newdt = new System.Data.DataTable();
                    DataColumn
                    dc = new DataColumn("dummy1");    dc.AllowDBNull = true;                                                newdt.Columns.Add(dc);
                    dc = new DataColumn("dummy2");    dc.AllowDBNull = true;                                                newdt.Columns.Add(dc);
                    dc = new DataColumn("dummy3");    dc.AllowDBNull = true;                                                newdt.Columns.Add(dc);
                    dc = new DataColumn("e_a_plus");  dc.AllowDBNull = true; dc.DataType = Type.GetType("System.Double");   newdt.Columns.Add(dc);
                    dc = new DataColumn("e_a_minus"); dc.AllowDBNull = true; dc.DataType = Type.GetType("System.Double");   newdt.Columns.Add(dc);
                    dc = new DataColumn("e_r_plus");  dc.AllowDBNull = true; dc.DataType = Type.GetType("System.Double");   newdt.Columns.Add(dc);
                    dc = new DataColumn("e_r_minus"); dc.AllowDBNull = true; dc.DataType = Type.GetType("System.Double");   newdt.Columns.Add(dc);
                    dc = new DataColumn("date_time"); dc.AllowDBNull = true; dc.DataType = Type.GetType("System.DateTime"); newdt.Columns.Add(dc);
                    dc = new DataColumn("period");    dc.AllowDBNull = true;                                                newdt.Columns.Add(dc);
                    foreach (DataRow row in day_profile) newdt.ImportRow(row);
                    //теперь нужно нормально отсортировать таблицу по дате
                    DataView dtview = new DataView(newdt);
                    dtview.Sort = "date_time";
                    newdt = dtview.ToTable();
                    //рисуем номер дня
                   //DateTime day = Convert.ToDateTime(dt.Rows[k]["day"]);
                    worksheet.Cells[6, k + 2] = k + 1;
                    //далее цикл по часовкам дня                       
                    for (int j = 0; j < newdt.Rows.Count; j++)
                    {
                       worksheet.Cells[7 + j, k + 2] = Convert.ToDouble(newdt.Rows[j]["e_a_plus"]);
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                       worksheet.Cells[7 + j, k + 2].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;                                         
                    }
                    //нужно подбить сумму расходов за день
                    string formula = "=SUM(" + worksheet.Cells[7, k + 2].Address + ":" + worksheet.Cells[30, k + 2].Address + ")";
                    worksheet.Cells[31, k + 2].Formula = formula;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[31, k + 2].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                    //нужно сформировать формулу выбирающую максимальное значение энергии во время часов-пик
                    formula = "=MAX(";                    
                    while (month_peak_hours.Length > 0)
                    {
                        pos_separator = month_peak_hours.IndexOf(",", 0);//находим первое вхождение разделяющего символа в строке
                        string hours_range = month_peak_hours.Substring(0, pos_separator).Trim();//выхватываем диапазон часов до точки с запятой
                        byte low_hour = Convert.ToByte(hours_range.Substring(0, hours_range.IndexOf('-')));//смотрим нижний час диапазона                                                                                                      
                        byte hi_hour = Convert.ToByte(hours_range.Substring(hours_range.IndexOf('-') + 1));//смотрим верхний час диапазона  
                        Range range = worksheet.Range[worksheet.Cells[7 + low_hour, k + 2], worksheet.Cells[6 + hi_hour, k + 2]];
                        range.Interior.Color = Color.Yellow;
                        formula += range.Address + ",";                       
                        //отрезаем от начала строки использованные диапазоны часов
                        month_peak_hours = month_peak_hours.Substring(pos_separator + 1).Trim();
                    }
                    formula += ")*"+ Convert.ToInt16(dt.Rows[k]["work_day"]);
                    worksheet.Cells[32, k + 2].Formula = formula;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
                    worksheet.Cells[32, k + 2].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                }
                //посчитаем кол-во рабочих дней
                DataRow[] work_days = dt.Select("work_day = 1");
                int work_days_count = work_days.Length - 1;//отнимаем единичку т.к. в этот массив попадает первое число следующего месяца
                Range range2 = worksheet.Range[worksheet.Cells[31, 2], worksheet.Cells[31, dt.Rows.Count]];
                worksheet.Cells[33, 1] = "Итого за месяц (с учётом к-та тр-ции " + counter.TransformationRate.ToString() + ")";
                worksheet.Cells[33, 2].Formula = "=SUM("+ range2.Address + ")*"+counter.TransformationRate.ToString();
                worksheet.Cells[33, 2].NumberFormat = "0,000";

                range2 = worksheet.Range[worksheet.Cells[32, 2], worksheet.Cells[32, dt.Rows.Count]];
                worksheet.Cells[35, 1] = "Фактическая мощность";
                worksheet.Cells[35, 2].Formula = "=(SUM(" + range2.Address + ")/"+ work_days_count.ToString()+")*" + counter.TransformationRate.ToString();
                worksheet.Cells[35, 2].NumberFormat = "0,000";
                worksheet.Columns.AutoFit();
            }      
        }

        public void FormIntegralReport(List<TreeNode> treenodes, string month, Workbook ext_workbook = null)
        {
            //здесь будет формирование отчёта "Интегральный акт" используя ранее составленный в Excel шаблон
            this.worksheet = this.workbook.ActiveSheet;
            this.worksheet.Name = "1";
            //Сначала выгружаем энергию на начало и конец периода.
            //Перед этим нужно проанализировать какой период (месяц) нам нужен.
            //Каждому виду энергии в счётчике назначен свой номер
            //Следовательно, анализируя выбранный месяц, мы выбираем соответствующие параметры из списка энергии счётчика

            //Открываем цикл по коллекции счётчиков с целью получить необходимые данные
            int pp = 1;//порядковый номер счётчика (строки)
            foreach (TreeNode node in treenodes)
            {
                try
                {
                ICounter counter = (ICounter)node.Tag;
                //нужно вытащить адрес
                System.Data.DataTable dt = DataBaseManagerMSSQL.Return_CounterRS_Row(counter.ID);
                string address = dt.Rows[1][1].ToString() + " " + dt.Rows[2][1].ToString();//получаем адрес
                System.Data.DataTable dt_params = DataBaseManagerMSSQL.Return_Integral_Parameters_Row(counter.ID);
                double calculated_addendum_percent = Convert.ToDouble(dt_params.Rows[0][1]) / 100;//процент для вычисляемой надбавки
                string misc_id = dt_params.Rows[0][2].ToString();//идентификатор для сбытовой компании
                string type_voltage = dt_params.Rows[0][3].ToString();//тип напряжения??
                string current_direction = "Приём";//направление перетока
                if (Convert.ToByte(dt_params.Rows[0][4]) == 0) current_direction = "Отдача";//если в таблице 0, то меняем направление
                string comment = dt_params.Rows[0][5].ToString();//примечания
                //выбираем виды энрегии в зависимости от выбранного месяца
                double energy_n = 0;//энрегия на начало месяца
                double energy_k = 0;//энрегия на конец месяца (на начало следующего)
                byte qty_day = 0;//количество дней в месяце

                switch (month)
                {
                    case "1.Январь":
                        {
                            energy_n = counter.EnergyToRead[21].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[22].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 31;
                        }
                        break;

                    case "2.Февраль":
                        {
                            energy_n = counter.EnergyToRead[22].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[23].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 28;
                        }
                        break;

                    case "3.Март":
                        {
                            energy_n = counter.EnergyToRead[23].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[24].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 31;
                        }
                        break;

                    case "4.Апрель":
                        {
                            energy_n = counter.EnergyToRead[24].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[25].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 30;
                        }
                        break;

                    case "5.Май":
                            {
                                energy_n = counter.EnergyToRead[25].lastValueZone0;//берем сумму тарифов на начало месяца
                                energy_k = counter.EnergyToRead[26].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                                qty_day = 31;
                            }
                        break;

                    case "6.Июнь":
                        {
                            energy_n = counter.EnergyToRead[26].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[27].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 30;
                        }
                        break;

                    case "7.Июль":
                        {
                            energy_n = counter.EnergyToRead[27].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[28].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 31;
                        }
                        break;

                    case "8.Август":
                        {
                            energy_n = counter.EnergyToRead[28].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[29].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                             qty_day = 31;
                        }
                        break;

                    case "9.Сентябрь":
                        {
                            energy_n = counter.EnergyToRead[29].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[30].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 30;
                        }
                        break;

                    case "10.Октябрь":
                        {
                            energy_n = counter.EnergyToRead[30].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[31].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 31;
                        }
                        break;

                    case "11.Ноябрь":
                        {
                            energy_n = counter.EnergyToRead[31].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[32].lastValueZone0;//берём суииу тарифов на начало следующего месяца
                            qty_day = 30;
                        }
                        break;

                    case "12.Декабрь":
                        {
                            energy_n = counter.EnergyToRead[32].lastValueZone0;//берем сумму тарифов на начало месяца
                            energy_k = counter.EnergyToRead[21].lastValueZone0;//берём суииу тарифов на начало следующего месяца (январь нумеруется по кругу)
                            qty_day = 31;
                        }
                        break;
                }
                //после того, как получили значения энергии, приступаем к заполнению ячеек
                int col_no = 1;
                worksheet.Cells[6 + pp, col_no] = pp; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//заполняем поле порядкового номера
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 2;
                worksheet.Cells[6 + pp, col_no] = counter.Name; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//наименование счётчика
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 3;
                worksheet.Cells[6 + pp, col_no] = address; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//вдрес
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 4;
                worksheet.Cells[6 + pp, col_no] = misc_id; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//идентификатор для сбытовой компании
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 5;
                worksheet.Cells[6 + pp, col_no] = type_voltage; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//тип напряжения???
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 6;
                worksheet.Cells[6 + pp, col_no] = counter.SerialNumber; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//серийный номер
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 7;
                worksheet.Cells[6 + pp, col_no] = current_direction; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//направление перетока
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 8;
                worksheet.Cells[6 + pp, col_no] = energy_k; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;//показания на конец периода (на начало следующего)
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 9;
                worksheet.Cells[6 + pp, col_no] = energy_n; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //показания на начало периода
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                    
                col_no = 10;
                string f = "=IF(OR(" + worksheet.Cells[6 + pp, 8].Address + "<=0," + worksheet.Cells[6 + pp, 9].Address + "<=0),0,IF((" + worksheet.Cells[6 + pp, 8].Address + "-" + worksheet.Cells[6 + pp, 9].Address + ")<0," + worksheet.Cells[6 + pp, 8].Address + "-" + worksheet.Cells[6 + pp, 9].Address + "+POWER(10,LEN(TRUNC(" + worksheet.Cells[6 + pp, 9].Address + "))),"
                        + worksheet.Cells[6 + pp, 8].Address + "-" + worksheet.Cells[6 + pp, 9].Address + "))";

                string a = String.Join("", f.Split('$'));
                worksheet.Cells[6 + pp, col_no] = a;
                worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //разность показаний счётчиков (там где в шаблоне сложная формула)
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;

                col_no = 11;//столбец К
                worksheet.Cells[6 + pp, col_no] = counter.TransformationRate; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //коэффициент трансформации
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 12;//столбец L
                worksheet.Cells[6 + pp, col_no] = "="+ String.Join("", worksheet.Cells[6 + pp, 10].Address.Split('$')) + "*" + String.Join("", worksheet.Cells[6 + pp, 11].Address.Split('$'));
                worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //разность показаний * коэффициент трансформации
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                col_no = 13;//столбец М
                worksheet.Cells[6 + pp, col_no].Formula = "="+ String.Join("", worksheet.Cells[6 + pp, 12].Address.Split('$')) + "*" + calculated_addendum_percent.ToString().Replace(',','.');  worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //столбец L7*вычисляемая надбавка
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;     
                col_no = 14;//столбец N. Итоговое число по счётчику
                worksheet.Cells[6 + pp, col_no].Formula = "=ROUND(" + String.Join("", worksheet.Cells[6 + pp, 12].Address.Split('$')) + "+" +String.Join("", worksheet.Cells[6 + pp, 13].Address.Split('$')) + ",0)";
                worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter; //столбец L7*вычисляемая надбавка
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                //нужно в расшифровке найти первую строку по текущему счётчику (искать по идентификатору) и посмотреть какая получилась по его расшифровке сумма. В случае несовпадения подкрасить ячейку в текущей книге
                if (ext_workbook != null)//если книгу с расшифровкой подали сюда
                {
                        Worksheet ext_worksheet = ext_workbook.ActiveSheet;
                        Range r = ext_worksheet.UsedRange.Find(misc_id);
                        //double val1 = ext_worksheet.Cells[r.Row, 5].Value;
                        //val1 = Math.Round(val1, 3) * 1000;
                        double val2 = worksheet.Cells[6 + pp, col_no].Value;

                        //if (val1 != val2)
                        //{
                        //    ext_worksheet.Cells[r.Row, 5].Interior.Color = Color.Yellow;
                        //    //ext_worksheet.Cells[r.Row, 6].Interior.Color = Color.LightGreen;
                        //    //ext_worksheet.Cells[r.Row, 6] = val2 / 1000;
                        //    //ext_worksheet.Cells[r.Row, 7] = "=" + ext_worksheet.Cells[r.Row, 5].Address + "/" + ext_worksheet.Cells[r.Row, 6].Address;
                        //}

                        ext_worksheet.Cells[r.Row, 6].Interior.Color = Color.LightGreen;
                        ext_worksheet.Cells[r.Row, 6] = val2 / 1000;
                        ext_worksheet.Cells[r.Row, 7] = "=" + ext_worksheet.Cells[r.Row, 6].Address + "/" + ext_worksheet.Cells[r.Row, 5].Address;
                        //количество часов в месяце = 24*количество дней в месяце
                        for (int i = r.Row; i <= qty_day*24 + r.Row - 1; i++)
                        {
                            ext_worksheet.Cells[i, 8] = "=" + ext_worksheet.Cells[i, 4].Address + "*" + ext_worksheet.Cells[r.Row, 7].Address;
                        }
                }

                col_no = 15;//столбец N. Комментарий
                worksheet.Cells[6 + pp, col_no] = comment; worksheet.Cells[6 + pp, col_no].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
                worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, col_no].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;
                pp += 1; //наращиваем порядковый номер строки
            }
                catch (Exception ex)
            {
                    MessageBox.Show(ex.Message, "Ошибка выгрузки интегрального акта для счётчика " + node.Text);
                    continue;
            }
        }

            worksheet.Cells[6 + pp, 12] = "ВСЕГО"; worksheet.Cells[6 + pp, 14].HorizontalAlignment = XlHAlign.xlHAlignRight; 
            worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
            worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
            worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
            worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 12].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;

            worksheet.Cells[6 + pp, 14].Formula = "=SUM(N7:N" + (pp + 6) + ")"; worksheet.Cells[6 + pp, 14].HorizontalAlignment = XlHAlign.xlHAlignRight; //итого по столбцу N
            worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeRight].Weight = 2;
            worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeLeft].Weight = 2;
            worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeTop].Weight = 2;
            worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous; worksheet.Cells[6 + pp, 14].Borders[XlBordersIndex.xlEdgeBottom].Weight = 2;

            worksheet.Cells[1, 14] = month + ' ' + DateTime.Now.Year;
            worksheet.Rows.AutoFit();
        }

        public Workbook FormIntegralReport(List<TreeNode> treenodes, string month, int year)
        {//в этой процедуре выгружаем только расшифровку (сами профили)           
            worksheet = workbook.ActiveSheet;
            worksheet.Name = "Расшифровка";
            //Выгружаем на него профили всех счётчиков подряд один за другим
            //Открываем цикл по счётчикам для выгрузки профиля
            int stringNum = 1;//номер строки в листе куда писать значения. Поскольку профили счётчиков идут один за другим сверху-вниз, этот номер должен наращиваться по мере выгрузки
            //заголовок 
            worksheet.Cells[stringNum, 1] = "День";
            worksheet.Cells[stringNum, 2] = "Час";
            worksheet.Cells[stringNum, 3] = "ID точки учёта";
            worksheet.Cells[stringNum, 4] = "Объем электроэнергии, МВтч";
            foreach (TreeNode node in treenodes)
            {
                try
                {
                    ICounter counter = (ICounter)node.Tag;
                    System.Data.DataTable dt_params = DataBaseManagerMSSQL.Return_Integral_Parameters_Row(counter.ID);//подгружаем параметры выгрузки интегрального акта из базы               
                    string misc_id = dt_params.Rows[0][2].ToString();//идентификатор для сбытовой компании
                    double calculated_addendum_percent = (Convert.ToDouble(dt_params.Rows[0][1]) / 100) + 1;//процент для вычисляемой надбавки
                    //если в счётчике нет ни одной записи профиля (профиль не сниимался) и при загрузке профиля (в LoadProfileCounter) не были нарисованы нули, то нужно нарисовать их здесь, потому что отчёту без разницы
                    if (counter.ProfileDataTable.Rows.Count == 0)
                    {
                        //рисуем дни. Сначала нужно получить количество дней в выбранном месяце
                        string month_no = month.Substring(0, month.IndexOf('.'));//вытаскиваем номер месяца
                        int days = DateTime.DaysInMonth(year, Convert.ToInt16(month_no));
                        //здесь рисуем пустые профили (т.е. в следующий цикл for счётчик с пустым профилем не войдёт, а создавать в профиле счётчика насильно нули - это некорректно)
                        int first_row = stringNum;//запоминаем номер первой строки пустого счётчика (чтобы в правильно месте нарисовать сумму)
                        for (int i = 1; i <= days; i += 1)
                        {
                            for (int j = 1; j <= 24; j++)
                            {
                                stringNum += 1;
                                worksheet.Cells[stringNum, 1] = i.ToString().PadLeft(2, '0') + '.' + month_no.PadLeft(2, '0') + '.' + year;//день
                                worksheet.Cells[stringNum, 2] = j.ToString();//час
                                worksheet.Cells[stringNum, 3] = misc_id;//идентификатор для сбытовой организации
                                worksheet.Cells[stringNum, 4] = 0;//объем потреблённой мощности
                                worksheet.Cells[stringNum, 4].NumberFormat = "0,000";                                
                            }
                        }
                        worksheet.Cells[first_row + 1, 5] = 0;
                        worksheet.Cells[first_row + 1, 5].NumberFormat = "0,000";
                        continue;//после того, как нарисовали пустой счётчик, идём на следующий
                    }
                        //циклимся по строкам таблицы профиля текущего счётчика (у которого есть профиль)
                        for (int i = 0; i < counter.ProfileDataTable.Rows.Count; i += 24)
                        {
                            for (int j = 1; j <= 24; j++)
                            {
                                stringNum += 1;
                                DateTime dt = Convert.ToDateTime(counter.ProfileDataTable.Rows[i + j - 1]["date_time"].ToString().Substring(0, 10));
                                if (j == 24)
                                //если пошёл 24-ый час, то нужно отнять день от даты чтобы рисовался 24-ый час текущего дня (а не предыдущего)
                                {
                                    dt = dt.AddDays(-1);
                                    //в случае кривого профиля (меток времени со сдвигом) и не полностью корректной загрузки его из базы (запрос округляет метки времени, но where идёт без округления) нужно сделать проверку, а не отняли ли мы лишний день?
                                    //если полученная дата после вычитания дня меньше предыдущей, то нужно вернуть день обратно. Это временная заплатка до исправления запроса на загрузку профиля из базы
                                    if (dt < Convert.ToDateTime(counter.ProfileDataTable.Rows[i + j - 2]["date_time"].ToString().Substring(0, 10)))
                                    {
                                        dt = dt.AddDays(1);
                                    }
                                }
                                worksheet.Cells[stringNum, 1] = dt.ToString().Substring(0, 10);//день
                                worksheet.Cells[stringNum, 2] = j.ToString();//час
                                double val = (Convert.ToDouble(counter.ProfileDataTable.Rows[i + j - 1]["e_a_plus"]) / 1000) * (counter.TransformationRate * calculated_addendum_percent);
                                //val = Math.Round(val, 3);
                                worksheet.Cells[stringNum, 4] = val;//объем потреблённой мощности
                                worksheet.Cells[stringNum, 4].NumberFormat = "0,000";
                                worksheet.Cells[stringNum, 3] = misc_id;//идентификатор для сбытовой организации                                                                                     
                            }
                        }
                        //для каждого счётчика нужно посчитать общую сумму
                        string f = "=SUM(D" + (stringNum - counter.ProfileDataTable.Rows.Count + 1).ToString() + ":" + "D" + stringNum.ToString() + ")";
                        worksheet.Cells[stringNum - counter.ProfileDataTable.Rows.Count + 1, 5] = f;
                }
                catch
                {
                    continue;
                }                 
            }
            worksheet.Columns.AutoFit();
            worksheet.Rows.AutoFit();
            return workbook;
        }

        public void ShowStatistics(System.Data.DataTable districts, DateTime energyLastDate, DateTime errorLastDate)
        {
            worksheet = workbook.ActiveSheet; worksheet.Name = "Статистика";
            int stringNum = 2;
            int totalOverall = 0;
            int totalShare = 0;
            worksheet.Cells[1, 3] = "В АИИС"; worksheet.Cells[1, 3].BorderAround(XlLineStyle.xlContinuous);
            worksheet.Cells[1, 4] = "В опросе"; worksheet.Cells[1, 4].BorderAround(XlLineStyle.xlContinuous);
            worksheet.Cells[1, 5] = "%"; worksheet.Cells[1, 5].BorderAround(XlLineStyle.xlContinuous);
            foreach (DataRow district in districts.Rows)
            {//для каждого района из списка районов создаём сводный блок
                System.Data.DataTable dt = DataBaseManagerMSSQL.Return_District_Statistics(district["name"].ToString(), energyLastDate, errorLastDate);
                worksheet.Cells[stringNum, 2] = "Всего объектов"; worksheet.Cells[stringNum, 2].BorderAround(XlLineStyle.xlContinuous); 
                worksheet.Cells[stringNum, 5] = "=(D" + stringNum + "/C" + stringNum + ")*100"; worksheet.Cells[stringNum, 5].BorderAround(XlLineStyle.xlContinuous); worksheet.Cells[stringNum, 5].NumberFormat = "0,0";
                string formulae = "=SUM(C" + (stringNum + 1) + ":C" + (stringNum + dt.Rows.Count) + ")";
                worksheet.Cells[stringNum, 3] = formulae; worksheet.Cells[stringNum, 3].BorderAround(XlLineStyle.xlContinuous);
                formulae = "=SUM(D" + (stringNum + 1) + ":D" + (stringNum + dt.Rows.Count) + ")"; 
                worksheet.Cells[stringNum, 4] = formulae; worksheet.Cells[stringNum, 4].BorderAround(XlLineStyle.xlContinuous);
                worksheet.Range[worksheet.Cells[stringNum, 1], worksheet.Cells[stringNum + dt.Rows.Count, 1]].Merge();
                worksheet.Range[worksheet.Cells[stringNum, 1], worksheet.Cells[stringNum + dt.Rows.Count, 1]].HorizontalAlignment = XlHAlign.xlHAlignCenter;
                worksheet.Range[worksheet.Cells[stringNum, 1], worksheet.Cells[stringNum + dt.Rows.Count, 1]].VerticalAlignment = XlVAlign.xlVAlignCenter;
                worksheet.Range[worksheet.Cells[stringNum, 1], worksheet.Cells[stringNum + dt.Rows.Count, 1]].BorderAround(XlLineStyle.xlContinuous);
                stringNum += 1;
                foreach (DataRow item in dt.Rows)
                {
                    worksheet.Cells[stringNum - 1, 1] = item["district"].ToString(); worksheet.Cells[stringNum - 1, 1].BorderAround(XlLineStyle.xlContinuous);
                    worksheet.Cells[stringNum, 2] = item["title"].ToString(); worksheet.Cells[stringNum, 2].BorderAround(XlLineStyle.xlContinuous);
                    worksheet.Cells[stringNum, 3] = item["overall"].ToString(); worksheet.Cells[stringNum, 3].BorderAround(XlLineStyle.xlContinuous);
                    worksheet.Cells[stringNum, 4] = item["share"].ToString(); worksheet.Cells[stringNum, 4].BorderAround(XlLineStyle.xlContinuous);
                    formulae = "=(D" + stringNum + "/C" + stringNum + ")*100";
                    worksheet.Cells[stringNum, 5] = formulae; worksheet.Cells[stringNum, 5].NumberFormat = "0,0";
                    worksheet.Cells[stringNum, 5].BorderAround(XlLineStyle.xlContinuous);
                    totalOverall += Convert.ToInt16(item["overall"]);
                    totalShare += Convert.ToInt16(item["share"]);
                    stringNum += 1;
                }              
            }
            worksheet.Cells[2, 3] = totalOverall; worksheet.Cells[2, 3].BorderAround(XlLineStyle.xlContinuous); worksheet.Cells[2, 3].NumberFormat = "0,0";
            worksheet.Cells[2, 4] = totalShare; worksheet.Cells[2, 4].BorderAround(XlLineStyle.xlContinuous);
            worksheet.Columns.AutoFit();
        }

        public void ImportXLSIntoASKUE(string workbookname, ToolStripProgressBar pb, DataBaseManagerOracle dbmo, RichTextBox richText, int id_source, string city)
        {//в этой процедуре происходит загрузка существующей книги из РИМа в схему АСКУЭ на оракле
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();//создаём экземпляр книги
                workbook = excel.Workbooks.Open(workbookname);//загружаем книгу в память
                worksheet = workbook.Worksheets[1];//выбираем самый первый лист с таблицей показаний
                //вставляем новый реестр в АСКУЭ
                int roll_id = dbmo.InsertNewRoll(id_source);
                if (roll_id == -1)
                {
                    return;
                }
                //после того, как вставили новый реестр, нужно идти циклом по книге и вставлять записи в Sch_Val
                //сначала посчитаем количество записей в книге чтобы отображать прогресс в прогресс-баре
                pb.Minimum = 0;
                pb.Value = 0;
                pb.Maximum = worksheet.UsedRange.Rows.Count - 4;
                //циклимся по строкам книги. Начинаем работать с пятой строки, т.к. есть ещё заголовок   
                for (int i = 5; i <= worksheet.UsedRange.Rows.Count; i++)
                {//нужно определить тип счётчика и задать inter_type (для правильной записи в sch_val)
                    try
                    {
                        string type = worksheet.Cells[i, 16].Value;
                        int inter_type = 0;
                        //Эти названия берутся из поля "тип для отчёта" в программе РМС-2150
                        switch (type)
                        {
                            case "РиМ 129.01":
                                    { 
                                        switch (city)
                                        {//ВЕТВЛЕНИЕ ПО ГОРОДАМ ИЗ-ЗА ПРОБЛЕМ С ТИПАМИ В ТАБЛИЦЕ SCH_TYPE_EXT
                                            case "Kem": { inter_type = 9; break; }//В кемерово РиМ-129 без деления на 01, 02 и 03, поэтому inter_type указывается один
                                            case "Ber": { inter_type = 9; break; }
                                            case "Len": { inter_type = 5; break; }//В ленинске Рим-129 делится на 01, 02 и 03, поэтому inter_type указывается разный
                                    }
                                        break;
                                }

                            case "РиМ 129.02":
                                {
                                    switch (city)
                                    {//ВЕТВЛЕНИЕ ПО ГОРОДАМ ИЗ-ЗА ПРОБЛЕМ С ТИПАМИ В ТАБЛИЦЕ SCH_TYPE_EXT
                                        case "Kem": { inter_type = 9; break; }//В кемерово РиМ-129 без деления на 01, 02 и 03, поэтому inter_type указывается один
                                        case "Ber": { inter_type = 9; break; }
                                        case "Len": { inter_type = 6; break; }//В ленинске Рим-129 делится на 01, 02 и 03, поэтому inter_type указывается разный
                                    }
                                    break;
                                }

                            case "РиМ 129.03":
                                {
                                    switch (city)
                                    {//ВЕТВЛЕНИЕ ПО ГОРОДАМ ИЗ-ЗА ПРОБЛЕМ С ТИПАМИ В ТАБЛИЦЕ SCH_TYPE_EXT                                       
                                        case "Ber": { inter_type = 9; break; }
                                        case "Kem": { inter_type = 9; break; }
                                    }
                                    break;
                                }

                            case "ДДM 129.01": inter_type = 9; break;
                            case "РиМ 614.01": inter_type = 5; break;//то же, что 109 трехфазный
                            case "РиМ 614": inter_type = 5; break;//то же, что 109 трехфазный
                            case "РиМ 489.19": inter_type = 10; break;
                            case "РиМ 489.11": inter_type = 10; break;
                            case "РиМ 109.01": inter_type = 7; break;
                            case "РиМ 114.01": inter_type = 7; break;//то же, что однофазный РИМ-109
                            case "РиМ 114": inter_type = 7; break;//то же, что однофазный РИМ-109                            
                            case "РиМ 189.02": inter_type = 190; break;
                            //case "РиМ 189.01": inter_type = ; break;
                            case "РиМ 189.11":
                                {
                                    switch (city)
                                    {
                                        case "Kem": { inter_type = 64; break; }
                                        case "Ber": { inter_type = 65; break; }
                                    }
                                    break;
                                }

                            case "РиМ 489.01": inter_type = 10; break;
                            case "РиМ 289.02": inter_type = 9; break;
                            case "РиМ 189.26": inter_type = 11; break;
                            case "РиМ 489.13": inter_type = 915; break;
                        }

                        string serial_number = worksheet.Cells[i, 15].Value.ToString();//читаем серийный номер
                        string val_date = worksheet.Cells[i, 14].Value.ToString();//дата показаний
                        string sch_val1 = worksheet.Cells[i, 12].Value.ToString();//показания по сумме тарифов
                        sch_val1 = sch_val1.Replace(',', '.');//в показаниях заменяем запятую на точку чтобы не было ошибки при выполнении запроса на вставку

                        dbmo.InsertIntoSchVal(roll_id.ToString(), id_source.ToString(), inter_type.ToString(), serial_number, val_date, sch_val1, 0);//пытаемся вставить показания в таблицу Sch_Val
                        pb.PerformStep();
                    }
                    catch (Exception ex)
                    {                       
                        DateTime currentDate = DateTime.Now;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка вставки записи в таблицу Sch_Val: " + ex.Message + "\r");
                        richText.ScrollToCaret();
                        pb.PerformStep();
                        
                        continue;
                    }
                }
                //проверяем, пуст ли реестр?
                System.Data.DataTable dt = dbmo.SelectRoll(roll_id.ToString());//тащмм реестр
                //есть ли в нём хотя бы одна запись?
                if (dt.Rows.Count == 0)
                {//если в реестре нет ни одной записи - его нужно автоматически удалить
                    dbmo.DeleteRoll(roll_id.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при загрузке файла в базу данных Oracle: " + ex.Message, "Ошибка загрузки файла", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                DisposeExcel();                
            }         
        }      
    }
}


