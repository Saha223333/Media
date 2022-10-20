using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NewProject
{
    public partial class CalendarForm : Form
    {
        private DataTable CalendarTable;//таблица дней

        public CalendarForm()
        {
            InitializeComponent();
            RefreshCalendarGrid(Convert.ToDateTime("01.01."+DateTime.Now.Year), Convert.ToDateTime("31.12." + DateTime.Now.Year));
        }

        private void RefreshCalendarGrid(DateTime DateN, DateTime DateK)
        {          
            CalendarTable = DataBaseManagerMSSQL.Return_Calendar(DateN, DateK);//вытаскиваем календарь
            //нужно отсортировать таблицу по дате
            DataView dtview = new DataView(CalendarTable);
            dtview.Sort = "day";
            DataTable newdt = dtview.ToTable();
            CalendarDaysGrid.DataSource = newdt;

            CalendarDaysGrid.Columns[0].HeaderText = "Дата";
            CalendarDaysGrid.Columns[1].HeaderText = "День недели";
            CalendarDaysGrid.Columns[2].HeaderText = "Рабочий";
            CalendarDaysGrid.Columns[0].ReadOnly = true;
            CalendarDaysGrid.Columns[1].ReadOnly = true;
            CalendarDaysGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            CalendarDaysGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            CalendarDaysGrid.EnableHeadersVisualStyles = false;
        }

        private void UpdateDay(DateTime datetime, bool workday)
        {
            DataBaseManagerMSSQL.Update_Calendar_Day(datetime, workday);
        }

        private void DataGridViewCheckBoxCell_ChangeState(object sender, EventArgs e)
        {
          
        }

        private void CalendarDaysGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (CalendarDaysGrid.IsCurrentCellDirty)
            {
                CalendarDaysGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                UpdateDay(Convert.ToDateTime(CalendarDaysGrid.Rows[CalendarDaysGrid.CurrentRow.Index].Cells[0].Value),
                                                              Convert.ToBoolean(CalendarDaysGrid.Rows[CalendarDaysGrid.CurrentRow.Index].Cells[2].Value));
            }
        }

        private void monthCalendar1_DateChanged(object sender, DateRangeEventArgs e)
        {

        }

        private void monthCalendar_DateSelected(object sender, DateRangeEventArgs e)
        {               
            //сначала надо проверить, есть ли выбранный день в списке жирных (т.е. он выходной и в базе отмечен 0). Если да, то убрать его из списка, если нет, то добавить
            if (monthCalendar.BoldedDates.Contains(e.Start))
            {
                monthCalendar.RemoveBoldedDate(e.Start);
                UpdateDay(e.Start, true);//вносим изменение в базу (выходной день становится рабочим (истина в поле work_day)
            }
            else
            {//здесь добавляем дату в массив выходных дней (в календаре он подкрасится жирным, а в базу флажок work_day будет отмечен в ложь)              
                monthCalendar.AddBoldedDate(e.Start);
                UpdateDay(e.Start, false);//вносим изменение в базу (рабочий день становится выходным (ложь в поле work_day)
            }
            monthCalendar.UpdateBoldedDates();
            //нажатием на день на календаре помечаем этот день как выходной
            RefreshCalendarGrid(Convert.ToDateTime("01.01.2018"), Convert.ToDateTime("31.12.2018"));

            string peakhours = DataBaseManagerMSSQL.Return_Month_Peak_Hours(monthCalendar.SelectionStart.Month);
            MonthPeakHoursLabel.Text = peakhours;
        }

        private void CalendarForm_Load(object sender, EventArgs e)
        {
            //пытаемся загрузить в визуальный календарь все выходные даты из таблицы в базе (в список жирных)
            foreach (DataRow dr in CalendarTable.Rows)
            {
                if (Convert.ToBoolean(dr["work_day"]) == false)//если день выходной - делаем его жирным
                {
                    monthCalendar.AddBoldedDate(Convert.ToDateTime(dr["day"]));
                }               
            }
            monthCalendar.UpdateBoldedDates();
            //возвращаем пиковые часы месяцев
            string peakhours = DataBaseManagerMSSQL.Return_Month_Peak_Hours(monthCalendar.SelectionStart.Month);
            MonthPeakHoursLabel.Text = peakhours;
        }

        private void monthCalendar_DateChanged(object sender, DateRangeEventArgs e)
        {
            string peakhours = DataBaseManagerMSSQL.Return_Month_Peak_Hours(monthCalendar.SelectionStart.Month);
            MonthPeakHoursLabel.Text = peakhours;
        }

        private void AddPeakHourButt_Click(object sender, EventArgs e)
        {//добавляем часы-пик для указанного месяца
            if (LowerPeakHourNumeric.Value >= UpperPeakHourNumeric.Value) return;
                       
            DataBaseManagerMSSQL.Add_Peak_Hours_Period(LowerPeakHourNumeric.Value.ToString() + "-" + UpperPeakHourNumeric.Value.ToString() + ",", monthCalendar.SelectionStart.Month);
            string peakhours = DataBaseManagerMSSQL.Return_Month_Peak_Hours(monthCalendar.SelectionStart.Month);
            MonthPeakHoursLabel.Text = peakhours;
        }
    }
}
