using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Threading;

namespace NewProject
{
    public class TaskManager
    {
        public static List<TreeNode> tree = new List<TreeNode>();
        public static RichTextBox richText;
        public static bool GetProfile;//эта переменная хранит, будет ли снимать выбранное задание профиль при сохранении
        public static DateTime LowerDate;//нижняя дата снятия профиля при сохранении задания
        public static DateTime UpperDate;//верхняя дата снятия профиля при сохранении задания  
        public static int PeriodTemplatesComboBoxSelectedIndex = 0;//индекс выбранный в выпадающем списке шаблона автоматизации для съема профиля

        public static void OverwriteTree(int task_id, string tv_type)
        {//процедура перезаписи сетки выбранного задания
            //сначала очищаем сетку задания
            DataBaseManagerMSSQL.Delete_Task_Grid(task_id);
            //теперь цикл по дереву. Добавляем узлы в базу
            foreach (TreeNode node in tree)
            {
                var counter = (ICounter)node.Tag;
                //сохраняем сетку задания
                Exception ex = DataBaseManagerMSSQL.Add_Counter_To_Task(counter.ID, task_id);
                if (ex != null)
                {
                    DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                       {
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка сохранения счётчика в задание: " + ex.Message + "\r");
                           richText.ScrollToCaret();
                       }));
                    continue;
                }

                if (tv_type != "rs485") continue;//если счётчяик по витой паре, то идём дальше и сохраняем его параметры, если нет - то цикл продолжаем
                
                    //делаем запрос на те параметры, которые отмечены
                    var checkedEnergy = from energy in counter.EnergyToRead where energy.check == true select energy;
                    var checkedJournal = from journal in counter.JournalToRead where journal.check == true select journal;
                    var checkedParameter = from parameter in counter.ParametersToRead where parameter.check == true select parameter;
                    //далее циклимся по ним чтобы добавить в сетку опроса
                    foreach (CounterEnergyToRead energy in checkedEnergy)
                    {
                        DataBaseManagerMSSQL.Add_Parameter_To_Task(task_id, counter.ID, energy.name);
                    }

                    foreach (CounterJournalToRead journal in checkedJournal)
                    {
                        DataBaseManagerMSSQL.Add_Parameter_To_Task(task_id, counter.ID, journal.name);
                    }

                    foreach (CounterParameterToRead parameter in checkedParameter)
                    {
                        DataBaseManagerMSSQL.Add_Parameter_To_Task(task_id, counter.ID, parameter.name);
                    }
                
                //удаляем строку из сетки задание-профиль (на случай если период изменится). Если такой строки не было, то эффекта не будет
                DataBaseManagerMSSQL.Delete_Task_Profile_Row(task_id);
                //также поступаем с автоматизацией съема профиля (шаблон периода - за предыдущие сутки, за предыдущий месяц...). На случай если она изменться или отмениться, нам нужно её предварительно удалить
                DataBaseManagerMSSQL.Delete_Task_Profile_Automation(task_id);

                if ((TaskManager.GetProfile == true) && (PeriodTemplatesComboBoxSelectedIndex == 0))
                    //если стоит галочка "считывать при групповом опросе" и НЕ назначена автоматизация (стоит опция "указать") съёма профиля (за предыдущие сутки, за предыдущий месяц...), 
                    //то добавляем.            
                    DataBaseManagerMSSQL.Add_Task_Profile(task_id, LowerDate, UpperDate);
                //если стоит галочка "считывать при групповом опросе" и НАЗНАЧЕНА автоматизация съёма профиля (за предыдущие сутки, за предыдущий месяц...), 
                //то добавляем эту автоматизацию, а значения полей даты игнорируем, т.к. они выставятся при загрузке задания согласно условиям автоматизации
                if ((TaskManager.GetProfile == true) && (PeriodTemplatesComboBoxSelectedIndex > 0)) DataBaseManagerMSSQL.Add_Task_Profile_Automation(task_id, PeriodTemplatesComboBoxSelectedIndex);
            }
        }
    }
}
