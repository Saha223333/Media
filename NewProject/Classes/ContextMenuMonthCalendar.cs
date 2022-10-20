using System.Windows.Forms;

namespace NewProject
{
    public class ContextMenuMonthCalendar : ToolStripControlHost
    {
        public ContextMenuMonthCalendar() : base(new MonthCalendar()) { }//используем конструктор базового класса, подаём туда новый календарь
        //нужно объявить и описать всё то, чего нет в базовом классе
        protected override void OnSubscribeControlEvents(Control control)
        {
            base.OnSubscribeControlEvents(control);
            //-------//расширяем метод 
            MonthCalendar mc = (MonthCalendar)control;//берём стандартный календарь
            //привязываем к его событию изменения даты метод, вызывающий событие, описанный в данном классе
            mc.DateSelected += new DateRangeEventHandler(OnDateSelected);
        }

        protected override void OnUnsubscribeControlEvents(Control control)
        {
            base.OnUnsubscribeControlEvents(control);
            //------//расширяем метод 
            MonthCalendar mc = (MonthCalendar)control;
            mc.DateSelected -= new DateRangeEventHandler(OnDateSelected);
        }

        public event DateRangeEventHandler DateSelected;
        public void OnDateSelected(object sender, DateRangeEventArgs e)
        {
            if (DateSelected != null)
            {
                DateSelected(this, e);
            }

        }
    }
}
