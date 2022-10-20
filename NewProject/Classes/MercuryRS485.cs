using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Linq;

namespace NewProject
{
    public class MercuryRS485 : IDevice, ICounter, IReadable, IWritable
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public int NetAddress { get; set; }
        public bool PowerProfile { get; set; }
        public int CounterConst { get; set; }// постоянная сч-ка. Запоминается при считывании этого параметра
        //в эти поля будут загружаться пароли к счётчикам из базы данных, а потом подставляться в запрос на открытие канала связи
        //public int Pwd1;
        //public int Pwd2; 
        public string SerialNumber { get; set; }
        public DataTable ProfileDataTable { get; set; } //таблица, хранящая снятый профиль
        public int Divider { get; set; }//хранит значение делителя для вычисления энергии (1000 или с учётом постоянной счётчика)
        public int TransformationRate { get; set; }//коэффициент трансформации (берётся из БД)

        public BindingList<CounterEnergyToRead> EnergyToRead { get; set; } //перечень энергии для опроса счётчиков с цифровым интерфейсом       
        public BindingList<CounterParameterToRead> ParametersToRead { get; set; } //перечень параметров для опроса счётчиков с цифровым интерфейсом      
        public BindingList<CounterMonitorParameterToRead> MonitorToRead { get; set; } //перечень параметров тока (монитор) для счётчиков с цифровым интерфейсом      
        public BindingList<CounterJournalToRead> JournalToRead { get; set; } //перечень журнала для счётчиков с цифровым интерфейсом
        public BindingList<CounterJournalCQCToRead> JournalCQCToRead { get; set; } //перечень журнала ПКЭ для счётчиков с цифровым интерфейсом
        public BindingList<CounterParameterToWrite> ParametersToWrite { get; set; } //перечень параметров для записи в счётчик с цифровым интерфейсом     
        private TreeNode tn;

        public MercuryRS485(int pid, int pparentid, string pname, int pnetadr, bool ppowerprofile, string psernum, int ptransformationrate, TreeNode ptn)
        {
            this.tn = ptn;//нужен узел дерева, который является носителем этого объекта;
            //создаём таблицу, которая будет хранить записи профиля
            ProfileDataTable = new DataTable("PowerProfileTable"); ProfileDataTable.Clear();

            //добавляем столбцы
            DataColumn dc = new DataColumn("dummy1"); dc.AllowDBNull = true;
            ProfileDataTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково

            dc = new DataColumn("dummy2"); dc.AllowDBNull = true;
            ProfileDataTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково

            dc = new DataColumn("dummy3"); dc.AllowDBNull = true;
            ProfileDataTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково          

            dc = new DataColumn("e_a_plus"); ProfileDataTable.Columns.Add(dc); dc = new DataColumn("e_a_minus"); ProfileDataTable.Columns.Add(dc);
            dc = new DataColumn("e_r_plus"); ProfileDataTable.Columns.Add(dc); dc = new DataColumn("e_r_minus"); ProfileDataTable.Columns.Add(dc);
            dc = new DataColumn("date_time"); ProfileDataTable.Columns.Add(dc); dc = new DataColumn("period"); ProfileDataTable.Columns.Add(dc);
            //создаём списки параметров
            EnergyToRead = new BindingList<CounterEnergyToRead>();
            ParametersToRead = new BindingList<CounterParameterToRead>();
            JournalToRead = new BindingList<CounterJournalToRead>();
            MonitorToRead = new BindingList<CounterMonitorParameterToRead>();
            ParametersToWrite = new BindingList<CounterParameterToWrite>();
            JournalCQCToRead = new BindingList<CounterJournalCQCToRead>();

            Divider = 1000;//значение делителя энергии по-умолчанию (например при съёме энергии от сброса просто делим на 1000, а при съёме профиля 
                           //это значение будет равно "постоянная счётчика * 2"
            ID = pid; Name = pname; PowerProfile = ppowerprofile;
            ParentID = pparentid; NetAddress = pnetadr; SerialNumber = psernum;
            TransformationRate = ptransformationrate;
            //заполняем списки параметров
            //список параметров на запись
            {
                byte[] b = new byte[2];
                byte[] newValue = new byte[0];
                b[0] = 0x0003; b[1] = 0x000C;
                ParametersToWrite.Add(new CounterParameterToWrite("Дата и время", b, false, newValue));
            }

            {
                byte[] b = new byte[2];
                byte[] newValue = new byte[0];
                b[0] = 0x0003; b[1] = 0x000D;
                ParametersToWrite.Add(new CounterParameterToWrite("Коррекция времени в пределах 4 мин", b, false, newValue));
            }

            {
                byte[] b = new byte[2];
                byte[] newValue = new byte[0];
                b[0] = 0x0003; b[1] = 0x0000;
                ParametersToWrite.Add(new CounterParameterToWrite("Инициализация основного профиля", b, false, newValue));
            }

            {
                byte[] b = new byte[3];
                byte[] newValue = new byte[0];
                b[0] = 0x0003; b[1] = 0x0018; b[2] = 0x0000;
                ParametersToWrite.Add(new CounterParameterToWrite("Разрешить автоматический переход на зимнее\\летнее время", b, false, newValue));
            }

            {
                byte[] b = new byte[3];
                byte[] newValue = new byte[0];
                b[0] = 0x0003; b[1] = 0x0018; b[2] = 0x0001;
                ParametersToWrite.Add(new CounterParameterToWrite("Запретить автоматический переход на зимнее\\летнее время", b, false, newValue));
            }
            //список параметров для чтения
            //запрос на чтение текущего времени
            {
                byte[] b = new byte[2];
                b[0] = 0x0004;//запрос на чтение массива времён
                b[1] = 0x0000;//параметр чтения текущего времени
                ParametersToRead.Add(new CounterParameterToRead("Дата и время", b, 11, true));
            }
            //запрос на серийный номер
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0000;
                ParametersToRead.Add(new CounterParameterToRead("Серийный номер и дата выпуска", b, 10, false));
            }
            //запрос на коэффициенты трансформации
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0002;
                ParametersToRead.Add(new CounterParameterToRead("Коэфф. тр-ции по напряжению", b, 7, false));
            }
            //в ответе эти параметры идут вместе, но здесь они будут читаться и выводиться отдельно
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0002;
                ParametersToRead.Add(new CounterParameterToRead("Коэфф. тр-ции по току", b, 7, false));
            }
            //запросы на вариант исполнения. Посылается всегда один запрос, но в ответе анализируются разные байты
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Класс точности А+", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Номинальный ток", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Номинальное напряжение", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Профиль мощности", b, 9, false)); //да или нет
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Вариант исполнения", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Модем PLM", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Модем GSM", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Постоянная сч-ка", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Интерфейс 2", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Встроенное питание интерфейса 1", b, 9, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0003;
                ParametersToRead.Add(new CounterParameterToRead("Версия ПО", b, 6, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008;
                b[1] = 0x0007;
                ParametersToRead.Add(new CounterParameterToRead("Значение времён перехода на летнее\\зимнее время", b, 9, false));
            }
            ////журнал
            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0001; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время включения\\выключения прибора", b, 15, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0002; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время до\\после коррекции часов", b, 16, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0003; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время включения\\выключения фазы 1", b, 15, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0004; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время включения\\выключения фазы 2", b, 15, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0005; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время включения\\выключения фазы 3", b, 15, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0007; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время коррекции тарифного расписания", b, 9, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0009; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время сброса регистров накопленной энергии", b, 9, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x000A; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время инициализации массива средних мощностей", b, 9, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0012; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время вскрытия\\закрытия прибора", b, 15, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0016; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время сброса массива значений максимумов мощности", b, 9, false));
            }
            //журнал ПКЭ        
            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0020; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. предельно допустимое значение напряжения в фазе 1", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            { 
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0021; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. нормально допустимое значение напряжения в фазе 1", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0022; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. нормально допустимое значение напряжения в фазе 1", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0023; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. предельно допустимое значение напряжения в фазе 1", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0024; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. предельно допустимое значение напряжения в фазе 2", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0025; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. нормально допустимое значение напряжения в фазе 2", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0026; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. нормально допустимое значение напряжения в фазе 2", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0027; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. предельно допустимое значение напряжения в фазе 2", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0028; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. предельно допустимое значение напряжения в фазе 3", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x0029; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. нормально допустимое значение напряжения в фазе 3", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002A; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. нормально допустимое значение напряжения в фазе 3", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002B; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. предельно допустимое значение напряжения в фазе 3", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002C; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. предельно допустимое значение частоты сети", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002D; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за мин. нормально допустимое значение частоты сети", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002E; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. нормально допустимое значение частоты сети", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            {
                //время выхода/ возврата за максимальное предельно допустимое значение частоты сети.
                byte[] b = new byte[3];
                b[0] = 0x0004; b[1] = 0x002F; b[2] = 0x00FE; //ускоренное чтение: 20 записей по 12 байт = 240 байт
                JournalCQCToRead.Add(new CounterJournalCQCToRead("Время выхода/возврата за макс. предельно допустимое значение частоты сети", b, 243, false));//количество ожидаемых байт состоит из самого ответа + сетевой адрес + контрольная сумма
            }

            //параметры тока (монитор, векторная диаграмма)
            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0000;//байт BWRI = 0000|00|00 мощность P по сумме и фазам
                MonitorToRead.Add(new CounterMonitorParameterToRead("Мощность P", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0004;//байт BWRI = 0000|01|00 мощность Q по сумме и фазам
                MonitorToRead.Add(new CounterMonitorParameterToRead("Мощность Q", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0008;//байт BWRI = 0000|10|00 мощность S по сумме и фазам
                MonitorToRead.Add(new CounterMonitorParameterToRead("Мощность S", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0030;//байт BWRI = 0011|00|00 
                MonitorToRead.Add(new CounterMonitorParameterToRead("Коэффициент мощности", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0011;//байт BWRI =
                MonitorToRead.Add(new CounterMonitorParameterToRead("Напряжение", b, 12, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0021;//байт BWRI =
                MonitorToRead.Add(new CounterMonitorParameterToRead("Ток", b, 12, true));
            }


            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0040;//байт BWRI = 0100 0000
                MonitorToRead.Add(new CounterMonitorParameterToRead("Частота", b, 6, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0011; b[2] = 0x0051;
                MonitorToRead.Add(new CounterMonitorParameterToRead("Угол между фазными напряжениями 1 и 2", b, 6, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0011; b[2] = 0x0052;
                MonitorToRead.Add(new CounterMonitorParameterToRead("Угол между фазными напряжениями 1 и 3", b, 6, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0011; b[2] = 0x0053;
                MonitorToRead.Add(new CounterMonitorParameterToRead("Угол между фазными напряжениями 2 и 3", b, 6, true));
            }


            ////энергия           
            {//0              
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0000; b[2] = 0x0000;

                EnergyToRead.Add(new CounterEnergyToRead("Энергия от сброса", b, 19, true, 0, 0, 0, 0, 0, String.Empty));
            }

            {//1           
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0010; b[2] = 0x0000;

                EnergyToRead.Add(new CounterEnergyToRead("Энергия за текущий год", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//2
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0020; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за предыдущий год", b, 19, false, 0, 0, 0, 0, 0, String.Empty));

            }

            {//3            
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0040; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за текущие сутки", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//4
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0050; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за предыдущие сутки", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }
            //------------------энергия за месяц
            {//5
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0031; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за январь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//6
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0032; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за февраль", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//7
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0033; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за март", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//8
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0034; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за апрель", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//9
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0035; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за май", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//10
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0036; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за июнь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//11
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0037; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за июль", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//12
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0038; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за август", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//13
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0039; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за сентябрь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//14
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003A; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за октябрь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//15
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003B; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за ноябрь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//16
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003C; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за декабрь", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            //-----------------------энергия на начало периода
            {//17
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0002; b[3] = 0x0000; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало текущего года", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//18
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0002; b[3] = 0x0055; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало предыдущего года", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//19
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0006; b[3] = 0x00A6; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало текущих суток", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//20
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0006; b[3] = 0x00FB; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало предыдущих суток", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }
            //-----------------------энергия на начало месяца
            {//21
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0002; b[3] = 0x00AA; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало января", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//22
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0002; b[3] = 0x00FF; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало февраля", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//23
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0003; b[3] = 0x0054; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало марта", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//24
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0003; b[3] = 0x00A9; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало апреля", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//25
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0003; b[3] = 0x00FE; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало мая", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//26
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0004; b[3] = 0x0053; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало июня", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//27
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0004; b[3] = 0x00A8; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало июля", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//28
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0004; b[3] = 0x00FD; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало августа", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//29
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0005; b[3] = 0x0052; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало сентября", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//30
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0005; b[3] = 0x00A7; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало октября", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//31
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0005; b[3] = 0x00FC; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало ноября", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }

            {//32
                byte[] b = new byte[5];
                b[0] = 0x0006; b[1] = 0x0002; b[2] = 0x0006; b[3] = 0x0051; b[4] = 0x0010;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало декабря", b, 19, false, 0, 0, 0, 0, 0, String.Empty));
            }
        }

        public void LoadLastEnergyIntoEnergyList()
        {//помещаем последние значения энергии из базы в параметры счётчика для каждого вида энергии
            foreach (CounterEnergyToRead energy in this.EnergyToRead)
            {
                double e_t0_last; double e_t1_last;
                double e_t2_last; double e_t3_last;
                double e_t4_last; string e_last_date;

                DataTable dt = DataBaseManagerMSSQL.Return_CounterRS_Last_Energy(this.SerialNumber, energy.name);

                e_t0_last = Convert.ToDouble(dt.Rows[0]["e_t0_last"]); e_t1_last = Convert.ToDouble(dt.Rows[0]["e_t1_last"]);
                e_t2_last = Convert.ToDouble(dt.Rows[0]["e_t2_last"]); e_t3_last = Convert.ToDouble(dt.Rows[0]["e_t3_last"]);
                e_t4_last = Convert.ToDouble(dt.Rows[0]["e_t4_last"]); e_last_date = dt.Rows[0]["e_last_date"].ToString();

                energy.lastValueZone0 = e_t0_last; energy.lastValueZone1 = e_t1_last;
                energy.lastValueZone2 = e_t2_last; energy.lastValueZone3 = e_t3_last;
                energy.lastValueZone4 = e_t4_last; energy.lastTime = e_last_date;
            }
        }

        private void ParseJournalCQCAnswer(byte[] array, DataTable dt, int offset)
        {//Эта процедура разбирает ответ на запрос чтения журнала ПКЭ и помещает его в таблицу значений параметра
            for (int i = 0; i < 240; i = i + 12)//шагаем по 12 байт
            {
                string time_off_limits = String.Empty;//дата и время когда параметр тока вышел за допустимые границы
                string time_in_limits = String.Empty;//дата и время когда параметр тока вернулся в допустимые границы

                time_off_limits = array[4 + offset + i].ToString("X").PadLeft(2, '0') + "." + array[5 + offset + i].ToString("X").PadLeft(2, '0') + "." + array[6 + offset + i].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset + i].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset + i].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset + i].ToString("X").PadLeft(2, '0');

                time_in_limits = array[10 + offset + i].ToString("X").PadLeft(2, '0') + "." + array[11 + offset + i].ToString("X").PadLeft(2, '0') + "." + array[12 + offset + i].ToString("X").PadLeft(2, '0')
                            + " " + array[9 + offset + i].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset + i].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset + i].ToString("X").PadLeft(2, '0');

                if (time_off_limits == "00.00.00 00:00:00") { continue; }//такая строка нам не нужна т.к. параметр не выходил за границы нормы

                DataRow dr = dt.NewRow(); //добавляем строку в таблицу
                dr["time_off_limits"] = time_off_limits;
                dr["time_in_limits"] = time_in_limits;

                dt.Rows.Add(dr);
            }
        }

        public string ParseParameterValue(byte[] array, string pname, int offset)
        {//процедура разбора ответа и приведения в читаемый вид. Возвращает строку
            switch (pname)
            {
                case "Значение времён перехода на летнее\\зимнее время":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[1 + offset].ToString("X").PadLeft(2, '0') + " ч. " + array[2 + offset].ToString("X").PadLeft(2, '0')
                           + '.' + array[3 + offset].ToString("X").PadLeft(2, '0') + '\\'
                           + array[4 + offset].ToString("X").PadLeft(2, '0') + " ч. " + array[5 + offset].ToString("X").PadLeft(2, '0')
                           + '.' + array[6 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Дата и время":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + ".20" + array[7 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Версия ПО":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[1 + offset].ToString() + array[2 + offset].ToString().PadLeft(2, '0') + array[3 + offset].ToString().PadLeft(2, '0');
                        return valueStr;
                    }

                case "Серийный номер и дата выпуска":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[1 + offset].ToString().PadLeft(2, '0') + array[2 + offset].ToString().PadLeft(2, '0')
                            + array[3 + offset].ToString().PadLeft(2, '0') + array[4 + offset].ToString().PadLeft(2, '0') + " Дата выпуска: "
                            + array[5 + offset].ToString().PadLeft(2, '0') + "." + array[6 + offset].ToString().PadLeft(2, '0')
                            + ".20" + array[7 + offset].ToString().PadLeft(2, '0');
                        return valueStr;
                    }

                case "Коэфф. тр-ции по напряжению":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[1 + offset].ToString() + array[2 + offset].ToString();
                        return valueStr;
                    }

                case "Коэфф. тр-ции по току":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[3 + offset].ToString() + array[4 + offset].ToString();
                        return valueStr;
                    }


                case "Класс точности А+":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(0, 2)) //смотрим что там в двух битах
                        {
                            case "00": { b = "0,2%"; } break;
                            case "01": { b = "0,5%"; } break;
                            case "10": { b = "1,0%"; } break;
                            case "11": { b = "2,0%"; } break;
                        }
                        return b;
                    }

                case "Номинальный ток":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(6, 2)) //смотрим что там в двух битах
                        {
                            case "00": { b = "5А"; } break;
                            case "01": { b = "1А"; } break;
                            case "10": { b = "10А"; } break;
                        }
                        return b;
                    }

                case "Номинальное напряжение":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(4, 2)) //смотрим что там в двух битах
                        {
                            case "00": { b = "57,7В"; } break;
                            case "01": { b = "230В"; } break;
                        }
                        return b;
                    }

                case "Профиль мощности":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(2, 1)) //смотрим что там в бите
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }

                case "Вариант исполнения":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[3 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(4, 4)) //смотрим что там в итах
                        {
                            case "0001": { b = "№1"; } break;
                            case "0010": { b = "№2"; } break;
                            case "0011": { b = "№3"; } break;
                            case "0100": { b = "№4"; } break;
                        }
                        return b;
                    }

                case "Модем PLM":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[4 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(1, 1)) //смотрим что там в итах
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }

                case "Модем GSM":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[4 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(2, 1)) //смотрим что там в итах
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }

                case "Постоянная сч-ка":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(4, 4)) //смотрим что там в итах
                        {
                            case "0000": { b = "5000"; } break;
                            case "0001": { b = "25000"; } break;
                            case "0010": { b = "1250"; } break;
                            case "0011": { b = "500"; } break;
                            case "0100": { b = "1000"; } break;
                            case "0101": { b = "250"; } break;
                        }
                        try
                        {
                            CounterConst = Convert.ToInt16(b);//при считывании этого параметра, запоминаем постоянную в поле счётчика (для снятия профиля)
                        }
                        catch
                        {
                            b = "Ошибка";
                        }
                        return b;
                    }

                case "Интерфейс 2":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[5 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(4, 1)) //смотрим что там в итах
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }

                case "Встроенное питание интерфейса 1":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[5 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = String.Empty; //значение параметра

                        switch (a.Substring(5, 1)) //смотрим что там в итах
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }

                case "Время включения\\выключения прибора":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время до\\после коррекции часов":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                           + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                           + "\\"
                           + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                           + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время включения\\выключения фазы 1":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                           + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                           + "\\"
                           + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                           + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время включения\\выключения фазы 2":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время включения\\выключения фазы 3":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время коррекции тарифного расписания":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время сброса регистров накопленной энергии":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время инициализации массива средних мощностей":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время вскрытия\\закрытия прибора":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[10 + offset].ToString("X").PadLeft(2, '0') + "." + array[11 + offset].ToString("X").PadLeft(2, '0') + "." + array[12 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0') + ":" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время сброса массива значений максимумов мощности":
                    {
                        string valueStr = String.Empty;
                        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }
                //журнал ПКЭ
                //case "Время выхода/возврата за максимальное предельно допустимое значение частоты сети":
                //    {
                //        string valueStr = String.Empty;
                //        valueStr = array[4 + offset].ToString("X").PadLeft(2, '0') + "." + array[5 + offset].ToString("X").PadLeft(2, '0')
                //            + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                //            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                //        return valueStr;
                //    }
            }
            return String.Empty;
        }

        public double ParseEnergyValue(DataProcessing dp, byte[] array, int offset)
        {//процедура чтения данных, возвращающая энергию
            try
            {
                string valueStr = String.Empty;
                valueStr = array[2 + offset].ToString("X").PadLeft(2, '0') + array[1 + offset].ToString("X").PadLeft(2, '0')
                         + array[4 + offset].ToString("X").PadLeft(2, '0') + array[3 + offset].ToString("X").PadLeft(2, '0');
                double valueDbl = Convert.ToInt64(valueStr, 16);
                valueDbl = valueDbl / this.Divider;
                return valueDbl;
            }
            catch
            {
                return -1;
            }
        }

        public double ParseMonitorValue(DataProcessing dp, byte[] array, string pname, int phaseNo, int offset)
        {//процедура чтения данных, возвращающая параметры монитора
         //phaseNo: сюда подаётся номер фазы в цикле. Содержит сдвиг чтения байтов в ответе для того чтобы получить значения по каждой фазе
            try
            {
                switch (pname)
                {
                    case "Мощность P":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[(3 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0')
                                      + array[(2 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0');

                            string firstbyte = Convert.ToString(array[(1 + phaseNo * 3) + offset], 2).PadLeft(8, '0').Substring(0, 1); //переводим первый байт в двоичный вид чтобы посмотреть значение первого бита                        
                            double valueDbl = Convert.ToInt64(valueStr, 16);

                            if (firstbyte == "1") valueDbl = 0 - valueDbl;//если направление энергии обратное

                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Мощность S":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Мощность Q":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

                            string firstbyte = Convert.ToString(array[(1 + phaseNo * 3) + offset], 2).PadLeft(8, '0').Substring(0, 1); //переводим первый байт в двоичный вид чтобы посмотреть значение первого бита   
                            double valueDbl = Convert.ToInt64(valueStr, 16);

                            if (firstbyte == "1") valueDbl = 0 - valueDbl;//если направление энергии обратное

                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Напряжение":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[(1 + phaseNo * 3) - 3 + offset].ToString("X".PadLeft(2, '0'))
                                     + array[(3 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[(2 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Коэффициент мощности":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');
                            //направление энергии   
                            string firstbyte = Convert.ToString(array[(1 + phaseNo * 3) + offset], 2).PadLeft(8, '0').Substring(0, 1); //переводим первый байт в двоичный вид чтобы посмотреть значение первого битас                            
                            double valueDbl = Convert.ToInt64(valueStr, 16);

                            if (firstbyte == "1") valueDbl = 0 - valueDbl;//если направление энергии обратное

                            valueDbl = valueDbl / 1000;

                            return valueDbl;
                        }

                    case "Ток":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[(1 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[(3 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[(2 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 1000;
                            return valueDbl;
                        }

                    case "Частота":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Угол между фазными напряжениями 1 и 2":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Угол между фазными напряжениями 1 и 3":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Угол между фазными напряжениями 2 и 3":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }
                }
            }
            catch
            {
                return -1;
            }

            return -1;
        }

        public void ReadJournalOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {         
            //цикл по отмеченным журналам
            foreach (var journal in this.JournalToRead)
            {
                if (journal.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                for (int i = 0; i < 10; i++)// цикл 10 итераций (от 0 до 9) по журналу т.к. 10 записей в каждом журнале
                {
                    DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + journal.name + " " + this.Name + "\r");
                        richText.ScrollToCaret();
                    }));
                    //посылаем текущий журнал и ждём ответа
                    byte[] OutBuf = this.FormJournalArray(dp, journal, i);//вызываем процедуру формирования запроса журнала
                    Exception ex = dp.SendData(OutBuf, 0); //посылаем запрос                               
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }

                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    byte[] answerArray = dp.Read(journal.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа

                    //читаем ответ
                    bool success = this.ValidateReadParameterAnswer(answerArray);//проверяем ответ на успешность
                    if (!success)
                    {
                        journal.spreadValueByName("record" + i.ToString(), "Ошибка");
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись " + (i + 1).ToString() + " неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
                    //эта процедура распределяет ответы по свойствам класса по имени свойства (в данном случае номер записи)
                    journal.spreadValueByName("record" + i.ToString(), this.ParseParameterValue(answerArray, journal.name, 0));
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись " + (i + 1).ToString() + " удачно\r");
                        richText.ScrollToCaret();
                    }));
                }
            }
        }

        public void ReadJournalCQCOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {
            //цикл по отмеченным журналам
            foreach (var journalCQC in this.JournalCQCToRead)
            {
                if (journalCQC.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                
                    DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + journalCQC.name + " " + this.Name + "\r");
                        richText.ScrollToCaret();
                    }));

                journalCQC.ValuesTable.Clear();//очистим таблицу паеред чтением
                //посылаем текущий байт запроса и ждём ответ
                byte jb = journalCQC.bytes[2];//байт - номер записи (в данном случае один байт запроса снимает 20 записей)

                for (int i = 0; i < 5; i++)//цикл по 5 значениям (FE,FD,FC,FB,FA)
                {                  
                    byte[] OutBuf = this.FormJournalArray(dp, journalCQC, jb);//вызываем процедуру формирования запроса журнала ПКЭ
                    Exception ex = dp.SendData(OutBuf, 0); //посылаем запрос  
                                                 
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }

                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    byte[] answerArray = dp.Read(journalCQC.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа
                    //читаем ответ
                    bool success = this.ValidateReadParameterAnswer(answerArray);//проверяем ответ на успешность
                    if (!success)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записи №" + ((((i + 1) * 20) + 1) - 20).ToString() + "-" + ((i + 1) * 20).ToString() + " неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    //после того как получили ответ вызываем процедуру разбора ответа
                    this.ParseJournalCQCAnswer(answerArray, journalCQC.ValuesTable, 0);

                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записи №" + ((((i + 1) * 20) + 1) - 20).ToString() + "-" + ((i + 1) * 20).ToString() +" удачно\r");
                        richText.ScrollToCaret();
                    }));
                    jb -= 1;//отнимаем 1 чтобы получить следующую порцию записей (FD,FC,FB,FA)
                }
            }
        }

        public void ReadJournalCQCOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {
            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            byte[] Package = null;
            byte[] answerArray = null;

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем журнал ПКЭ...\r");
                richText.ScrollToCaret();
            }));
        
            //циклимся по параметрам журнала
            foreach (var journalCQC in this.JournalCQCToRead)
            {
                if (journalCQC.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы

                byte jb = journalCQC.bytes[2];//байт - номер записи (в данном случае один байт запроса снимает 20 записей) 

                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + journalCQC.name + "...\r");
                    richText.ScrollToCaret();
                }));

                for (int i = 0; i < 5; i++)
                {//ЗДЕСЬ НЕ ПОЛУЧАЕТСЯ РЕАЛИЗОВАТЬ НОРМАЛЬНЫЙ ПАКЕТНЫЙ РЕЖИМ. ВОЗМОЖНО ШЛЮЗЫ НЕ УСПЕВАЮТ ПЕРЕВАРИТЬ ТАКОЙ ОБЪЁМ СРАЗУ? ХОТЯ ПРОФИЛЬ НОРМАЛЬНО СНИМАЕТСЯ ПО 50 ЗАПИСЕЙ ЗА РАЗ
                    List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)  
                    int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза

                    //int numberOfPackages = 1;

                    //for (int i = 0; i < numberOfPackages; i++)//цикл по 5 значениям (FE,FD,FC,FB,FA)
                    {
                        packnum += 1;//наращиваем номер пакета
                        Package = gate.FormPackage(this.FormJournalArray(dp, journalCQC, jb), 1, dp, packnum);
                        PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                        CountToWait += journalCQC.bytesToWait + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок пакета + хвост пакета)
                        jb -= 1;//на 1 уменьшаем этот байт, чтобы запросить следующую порцию записей журнала
                    }

                    if (PackagesBuffer.Count > 0)
                    {
                        
                        //если пакеты набрались, нужно их послать и принять ответ
                        byte[] OutBufJournal = new byte[0];
                        //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                        //составляем его из окна пакетов       
                        foreach (byte[] pack in PackagesBuffer)
                        {
                            if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                            Array.Resize(ref OutBufJournal, OutBufJournal.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                            Array.Copy(pack, 0, OutBufJournal, OutBufJournal.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                        }
                        //после того, как сформировали серию пакетов, отправляем её на порт и ждём ответов
                        Exception ex = dp.SendData(OutBufJournal, 0);//посылаем запрос
                        if (ex != null)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                                richText.ScrollToCaret();
                            }));
                            return;
                        }

                        if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                        answerArray = dp.Read(CountToWait, 10000, true);//ждём ответ. Ожидаемая длина складывается из поля BytesToWait всех параметров (суммы длин всех пакетов в ответе)

                        if (answerArray.Length == 5)//проверяем на длину общий ответа от шлюза
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записи №" + ((((i + 1) * 20) + 1) - 20).ToString() + "-" + ((i + 1) * 20).ToString() + " неудачно\r");
                                richText.ScrollToCaret();
                            }));
                            continue;
                        }
                        //теперь нужно разобрать ответ и оформить
                        //опять циклимся по параметрам, чтобы выделить те ответы из общего потока, которых мы отметили ранее и ждали
                        int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета) 
                        //анализируем ответ
                        //for (int i = 0; i < numberOfPackages; i++)
                        {
                            byte[] curAnswer = new byte[journalCQC.bytesToWait];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                            Array.Copy(answerArray, offset, curAnswer, 0, journalCQC.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа
                                                                                                  //читаем ответ. Проверяем выполнение запроса на успешность
                            bool success = this.ValidateReadParameterAnswer(curAnswer);
                            if (!success)
                            {
                                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                richText.Invoke(new Action(delegate
                                {
                                    richText.SelectionColor = Color.Red;
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записи №" + ((((i + 1) * 20) + 1) - 20).ToString() + "-" + ((i + 1) * 20).ToString() + " неудачно\r");
                                    richText.ScrollToCaret();
                                }));
                                offset += 8 + journalCQC.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                                continue;
                            }
                            //после того как получили ответ вызываем процедуру разбора ответа
                            this.ParseJournalCQCAnswer(curAnswer, journalCQC.ValuesTable, 0);

                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.DarkGreen;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записи №" + ((((i + 1) * 20) + 1) - 20).ToString() + "-" + ((i + 1) * 20).ToString() + " удачно\r");
                                richText.ScrollToCaret();
                            }));
                            offset += 8 + journalCQC.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)                                
                        }
                    }
                }
            }
        }

        public void ReadEnergyOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {           
                //Thread.Sleep(100);
                //для начала нужно получить постоянную счётчика прежде чем снммать энергию
                CounterParameterToRead param = this.ParametersToRead[11];
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] OutBufConst = this.FormParameterArray(dp, param); //вызываем процедуру формирования запроса             
                Exception ex = dp.SendData(OutBufConst, 0); //посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                byte[] answerArray = dp.Read(param.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа

                //читаем ответ. Проверяем на успешность согласно правилам конкретного типа счётчика
                bool success = this.ValidateReadParameterAnswer(answerArray);
                if (!success)
                {
                    param.value = "Ошибка";
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения постоянной счётчика\r");
                        richText.ScrollToCaret();
                    }));

                    //Thread.Sleep(100);
                    return;
                }
                param.value = this.ParseParameterValue(answerArray, param.name, 0);//разбираем и запоминаем значение постоянной счётчика    
                if (param.value == "Ошибка")
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения постоянной счётчика\r");
                        richText.ScrollToCaret();
                    }));
                    //Thread.Sleep(100);
                    return;
                }

                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.DarkGreen;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                    richText.ScrollToCaret();
                }));
            
            //----------------цикл по энергии----------------------------------------------
            foreach (var energy in this.EnergyToRead)
            {
                if (energy.check == false) continue;//если параметр не был отмечен галочкой, то идём на следующмй параметр и не пытаемся считать текущий
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы                    

                for (int i = 0; i <= 4; i++)//цикл 5 итераций по сумме и тарифам
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    byte[] OutBuf = null;
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + energy.name + " " + this.Name + "\r");
                        richText.ScrollToCaret();
                    }));

                    OutBuf = this.FormEnergyArray(dp, energy, i);
                    ex = dp.SendData(OutBuf, 0); //посылаем запрос

                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }

                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    answerArray = dp.Read(energy.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа        

                    success = this.ValidateReadParameterAnswer(answerArray);//проверяем выполнение запроса на успешность
                    double value = this.ParseEnergyValue(dp, answerArray, 0);//вычисляем значение энергии с учётом постоянной счётчика
                                                                                //проверяем корректность. Если первый байт ответа не сетевой адрес, или значение энергии посчиталось с ошибкой, то следующая итерация                           
                    if (!success || value == -1)//значение -1 может быть возвращено из ReadEnergy в случае возникновения исключения при расчёте (например выход за границы массива)
                    {
                        energy.spreadValueByName("lastValueZone" + i.ToString(), -1);//процедура распределения полученных значений энергии по тарифным зонам
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тариф " + i.ToString() + " неудачно\r");
                            richText.ScrollToCaret(); //Thread.Sleep(300);
                        }));
                        continue;
                    }
                    //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
                    //эта процедура распределяет ответы по свойствам класса по имени свойства (в данном случае тариф)
                    energy.spreadValueByName("lastValueZone" + i.ToString(), value);
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тариф " + (i).ToString() + " удачно: " + value.ToString() + "\r");
                        richText.ScrollToCaret();
                    }));
                }
                //после того, как прошлись по тарифам, нужно сохранить значения энергии в базу
                energy.saveToDataBase(this.SerialNumber, richText);
            }//конец цикла по энергии
        }

        public void ReadJournalOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {
            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            byte[] Package = null;
            byte[] answerArray = null;

            richText.Invoke(new Action(delegate
              {
                  richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем журнал...\r");
                  richText.ScrollToCaret();
              }));
            
            //циклимся по параметрам журнала
            foreach (var journal in this.JournalToRead)
            {
                if (journal.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы

                List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)  
                int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
                for (int i = 0; i < 10; i++)
                {
                    packnum += 1;//наращиваем номер пакета
                    Package = gate.FormPackage(this.FormJournalArray(dp, journal, i), 1, dp, packnum);
                    PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                    CountToWait += journal.bytesToWait + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок + хвост пакета
                }
                if (PackagesBuffer.Count > 0)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + journal.name + "...\r");
                        richText.ScrollToCaret();
                    }));
                    //если пакеты набрались, нужно их послать и принять ответ
                    byte[] OutBufJournal = new byte[0];
                    //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                    //составляем его из окна пакетов       
                    foreach (byte[] pack in PackagesBuffer)
                    {
                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        Array.Resize(ref OutBufJournal, OutBufJournal.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                        Array.Copy(pack, 0, OutBufJournal, OutBufJournal.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                    }
                    //после того, как сформировали серию пакетов, отправляем её на порт и ждём ответов
                    Exception ex = dp.SendData(OutBufJournal, 0);//посылаем запрос
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }

                    if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                    answerArray = dp.Read(CountToWait, 20000, true);//ждём ответ. Ожидаемая длина складывается из поля BytesToWait всех параметров (суммы длин всех пакетов в ответе)

                    if (answerArray.Length == 5)//проверяем на длину общий ответа от шлюза
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    //теперь нужно разобрать ответ и оформить
                    //опять циклимся по параметрам, чтобы выделить те ответы из общего потока, которых мы отметили ранее и ждали
                    int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета) 
                                   //анализируем ответ
                    for (int i = 0; i < 10; i++)
                    {
                        byte[] curAnswer = new byte[journal.bytesToWait];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                        Array.Copy(answerArray, offset, curAnswer, 0, journal.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа
                                                                                           //читаем ответ. Проверяем выполнение запроса на успешность
                        bool success = this.ValidateReadParameterAnswer(curAnswer);
                        if (!success)
                        {
                            journal.spreadValueByName("record" + i.ToString(), "Ошибка");
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись " + (i + 1).ToString() + " неудачно\r");
                                richText.ScrollToCaret();
                            }));
                            offset += 8 + journal.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                            continue;
                        }
                        journal.spreadValueByName("record" + i.ToString(), this.ParseParameterValue(curAnswer, journal.name, 0));
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.DarkGreen;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись " + (i + 1).ToString() + " удачно\r");
                            richText.ScrollToCaret();
                        }));
                        offset += 8 + journal.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)                                
                    }
                }
            }
        }//конец журнала------------------------------------------------------------------------------------------------------------------------------------------------------
    
        public void ReadEnergyOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {
            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
            byte[] answerArray = null;
            byte[] Package = null;

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем энергию...\r");
                richText.ScrollToCaret();
            }));
            
            {
                CounterParameterToRead param = this.ParametersToRead[11];//для начала нужно получить постоянную счётчика
                                                                           
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] OutBufConst = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
                Exception ex = dp.SendData(OutBufConst, 0); //посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                answerArray = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

                bool success = this.ValidateReadParameterAnswer(answerArray, 0, 8);
                if (!success)
                {
                    param.value = "Ошибка";
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения постоянной счётчика\r");
                        richText.ScrollToCaret();
                    }));
                    
                    return;
                }
                param.value = this.ParseParameterValue(answerArray, param.name, 8);//разбираем и запоминаем значение постоянной счётчика
            }
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                richText.ScrollToCaret();
            }));
            //циклимся по энергии                                             
            foreach (var energy in this.EnergyToRead)
            {
                if (energy.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы

                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + energy.name + "...\r");
                    richText.ScrollToCaret();
                }));

                int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
                List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)                          

                for (int i = 0; i <= 4; i++)// цикл 5 итераций по сумме и тарифам
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    packnum += 1;//наращиваем номер пакета
                    CountToWait += energy.bytesToWait + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок + хвост пакета
                    Package = new byte[energy.bytesToWait + 9];
                    Package = gate.FormPackage(this.FormEnergyArray(dp, energy, i), 1, dp, packnum);
                    PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                                                    
                }

                if (PackagesBuffer.Count > 0)
                {//если пакеты набрались, нужно их послать и принят ответ
                    byte[] OutBufEnergy = new byte[0];
                    //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                    //составляем его из окна пакетов       
                    foreach (byte[] pack in PackagesBuffer)
                    {
                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        Array.Resize(ref OutBufEnergy, OutBufEnergy.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                        Array.Copy(pack, 0, OutBufEnergy, OutBufEnergy.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                    }
                    //после того, как сформировали серию пакетов, отправляем её на порт и ждём ответов
                    Exception ex = dp.SendData(OutBufEnergy, 0);//посылаем запрос
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }

                    if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                    answerArray = dp.Read(CountToWait, 10000, true);//ждём ответ. Ожидаемая длина складывается из поля BytesToWait всех параметров (суммы длин всех пакетов в ответе)

                    if (answerArray.Length == 5)//проверяем общую длину ответа от шлюза
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    //теперь нужно разобрать ответ и оформить
                    //опять циклимся по параметрам, чтобы выделить те ответы из общего потока, которых мы отметили ранее и ждали
                    int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета) 
                    {
                        if (energy.check == true)//если параметр был отмечен галочкой, значит мы ищем ответ на него в общем потоке
                        {
                            for (int i = 0; i <= 4; i++)//цикл 5 итераций по сумме и тарифам
                            {
                                //Т.к. ответы приходят в том же порядке, в котором уходят запросы, то можно смотреть подряд
                                byte[] curAnswer = new byte[energy.bytesToWait];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                                Array.Copy(answerArray, offset, curAnswer, 0, energy.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа

                                bool success = this.ValidateReadParameterAnswer(curAnswer);//проверяем выполнение запроса на успешность
                                double value = this.ParseEnergyValue(dp, answerArray, offset);//процедура вычисления значения энергии
                                if (!success || value == -1)//значение -1 может быть возвращено из ReadEnergy в случае возникновения исключения при расчёте (например выход за границы массива)
                                {
                                    energy.spreadValueByName("lastValueZone" + i.ToString(), -1);
                                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.SelectionColor = Color.Red;
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тариф " + (i).ToString() + " неудачно\r");
                                        richText.ScrollToCaret();
                                    }));
                                    offset += 8 + energy.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                                    continue;
                                }
                                energy.spreadValueByName("lastValueZone" + i.ToString(), value);//процедура распределения полученных значений энергии по тарифным зонам
                                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                richText.Invoke(new Action(delegate
                                {
                                    richText.SelectionColor = Color.DarkGreen;
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тариф " + (i).ToString() + " удачно: " + value.ToString() + "\r");
                                    richText.ScrollToCaret();
                                }));
                                offset += 8 + energy.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                            }
                            //после того, как прошлись по тарифам, нужно сохранить значения энергии в базу
                            energy.saveToDataBase(this.SerialNumber, richText);
                        }
                    }
                }
            }//конец текущей энергии
        }//конец всей энергии---------------

        public void ReadParametersOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Modem modem)
        {
            //----------------цикл по параметрам RS485-------------------------
            //делаем запрос только на те параметры, которые отмечены
            //цикл по отмеченным параметрам
            foreach (var param in ParametersToRead)
            {
                if (param.check == false) continue;
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] OutBuf = this.FormParameterArray(dp, param); //вызываем процедуру формирования запроса параметра             
                Exception ex = dp.SendData(OutBuf, 0); //посылаем запрос                                                                                      
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; }//проверяем, был ли запрос на отмену работы
                byte[] answerArray = dp.Read(param.bytesToWait, 5000, true);//ждём определённое кол-ва байт ответа

                //читаем ответ
                bool success = this.ValidateReadParameterAnswer(answerArray);
                if (!success)
                {
                    param.value = "Ошибка";
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    continue;
                }
                //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
                param.value = this.ParseParameterValue(answerArray, param.name, 0);

                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.DarkGreen;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                    richText.ScrollToCaret();
                }));                 
                //проверяем, сходится ли время на счётчике со временем на компе. Эта проверка должна быть В ЦИКЛЕ чтобы наглядно для пользователя сразу после считывания проводить проверку и править в случае необходимости
                if (param.name == "Дата и время")
                {
                    DateTime dt = DateTime.Now;//значение чисто для инициализации экземпляра

                    try
                    {
                         dt = Convert.ToDateTime(param.value);//значение параметра времени счётчика, которое считали ранее
                    }
                    catch
                    {//если выпала ошибка преобразования
                        continue;//идём на следующий параметр, не пытаясь перепрошивать время
                    }

                    double hours_shift = 0;//сдвиг по часовому поясу (по-умолчанию 0)
                    //проверяем, есть ли счётчик в интегральниках чтобы учесть сдвиг по часовому поясу (нам нужно чтобы в счётчике было зашито московское время)
                    DataTable t = DataBaseManagerMSSQL.Return_Integral_Parameters_Row(this.ID);
                    if (t.Rows.Count == 1)
                    {
                        hours_shift = -4;
                    }

                    double delta_minutes = DateTime.Now.Subtract(dt).TotalMinutes;//разница в минутах между текущим временем на компе и временем в счётчике         
                    
                    if (hours_shift != 0)
                    {//если есть сдвиг часов (другой часовой пояс на счётчике) то это надо учесть и текущее время компьютера модифицировать с учётом сдвига
                        delta_minutes = DateTime.Now.AddHours(hours_shift).Subtract(dt).TotalMinutes;
                    }
                            
                    //пробуем: если время между часами счётчика и часами компа расходятся больше чем на 4 минуты в абсолютном выражении, то либо добавляем к часам счётчика 4 минуты, либо отнимаем от часов счётчика 4 минуты
                    //Почему так, а не просто попытка прошивки времени с компа:
                    //потому что команда коррекции в пределах 4 минут может быть выполнена один раз в сутки и этот процесс может быть итеративным изо дня в день, постепенно приближать часы счётчика к нужному времени.
                    if (Math.Abs(delta_minutes) > 4)//если сдвиг 0, то будет проверка на разницу в 4 минуты.
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Orange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Разница во времени составляет " + Math.Abs(delta_minutes).ToString() + " минут с учётом сдвига часов " + hours_shift.ToString() + ". Пытаюсь провести коррекцию часов счётчика в пределах 4 минут... " + "\r");
                            richText.ScrollToCaret();
                        }));

                        if (delta_minutes < 0)//если разница во времени отрицательная, значит время на счётчике больше и его надо уменьшить
                        {
                            dt = dt.AddMinutes(-3);//корректируем часы счётчика на 3 минуты назад чтобы приблизить ко времени на компе. 
                        }
                        else
                        {//если разница во времени положительная, значит время на счётчике меньше и его надо увеличить
                            dt = dt.AddMinutes(3);//корректируем часы счётчика на 3 минуты вперёд чтобы приблизить ко времени на компе. 
                        }
                      
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Orange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записываю в счётчик следующие дату и время: " + dt.ToString() + "\r");
                            richText.ScrollToCaret();
                        }));

                        //Т.к. счётчику нужны только часы, минуты и секунды (для формирования нового значения), то здесь можно придать дате (строке, её представляющей) необходимый формат, чтобы она вписалась в алгоритм
                        string fakeDateTime = dt.ToString();
                        fakeDateTime = fakeDateTime.Remove(0, 11);//удаляем всё, что левее от времен (дату)
                        fakeDateTime = fakeDateTime.PadLeft(20, '0');//нам нужны нули слева (чтобы придать строке необходимую длину для алгоритма разбора и формирования нового значения для записи)
                        //список значений полей
                        List<FieldsValuesToWrite> FieldsValuesToWrite = new List<FieldsValuesToWrite>();
                        //заполняем список значениями полей
                        FieldsValuesToWrite.Add(new FieldsValuesToWrite("DTPicker", fakeDateTime));
                        //проводим коррекцию времени в пределах 4 минут
                        this.ParametersToWrite[1].check = true;//помечаем параметр на запись         
                        modem.WriteParametersToDevice(this.tn, dp, richText, worker, FieldsValuesToWrite);//пишем новое значение параметра в счётчик
                        this.ParametersToWrite[1].check = false;//снимаем пометку параметра на запись
                    }
                }         
            }          
        }

        public bool ValidateReadParameterAnswer(byte[] answerArray, byte commandCode = 0, int offset = 0)//здесь счётчик будет сам анализировать успешность выполнения запросов и вернёт ответ в вызывающее устройство (модем, шлюз и т.д.)
        {//в счётчиках типа Меркурий 485 и СЭТ первый байт ответа - всегда сетевой номер, поэтому здесь достаточно проверить первый байт ответа на соответствие сетевому номеру счётчика (для любой команды чтения)
            if ((answerArray.Length == 5) || (answerArray[0 + offset] != this.NetAddress))
                return false;
            else
                return true;
        }

        public byte[] FormTestArray(DataProcessing dp)
        {
            //процедура  формирующая массив проверки связи со счётчиком
            byte[] testOutBuf = new byte[2];
            testOutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            testOutBuf[1] = 0; //запрос на тест связи
            UInt16 crc = dp.ComputeCrc(testOutBuf);
            Array.Resize(ref testOutBuf, 4);
            testOutBuf[2] = Convert.ToByte(crc % 256); //контрольная сумма
            testOutBuf[3] = Convert.ToByte(crc / 256);
            System.Threading.Thread.Sleep(100);
            return testOutBuf;
        }

        public byte[] FormGainAccessArray(DataProcessing dp, byte lvl, byte pwd)
        {//процедура формирования массива открытия канала
            byte[] OutBuf = new byte[9];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0001; //запрос на открытие канала
            OutBuf[2] = lvl; //в будущем в этом запросе должны участовать поля Pwd1 и Pwd2, которые будут тянуться из базы
            OutBuf[3] = pwd; //пароль по-умолчанию
            OutBuf[4] = pwd;
            OutBuf[5] = pwd;
            OutBuf[6] = pwd;
            OutBuf[7] = pwd;
            OutBuf[8] = pwd;
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, 11);
            OutBuf[9] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[10] = Convert.ToByte(crc / 256);
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormParameterArray(DataProcessing dp, CounterParameterToRead param)
        {//процедура, формирующая массив для чтения параметра согласно правилам формирования запросов в данном типе счётчиков
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, param.bytes.Length + 1);
            for (int i = 1; i <= param.bytes.Length; i++) { OutBuf[i] = param.bytes[i - 1]; } //формируем пакет запроса для счётчика.                                                                    
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                        
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormParameterArray(DataProcessing dp, CounterParameterToWrite param)
        {
            //процедура, формирующая массив для записи параметра согласно правилам формирования запросов в данном типе счётчиков
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, param.bytes.Length + 1);
            for (int i = 1; i <= param.bytes.Length; i++) { OutBuf[i] = param.bytes[i - 1]; } //формируем пакет запроса для счётчика
            Array.Resize(ref OutBuf, OutBuf.Length + param.value.Length);
            Array.Copy(param.value, 0, OutBuf, OutBuf.Length - param.value.Length, param.value.Length);//присовокупляем массив нового значения к массиву полезной нагрузки                                                                    
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                        
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormJournalArray(DataProcessing dp, CounterJournalToRead journal, int recNo) //recNo - Номер записи журнала
        {//процедура, формирующая массив для чтения журнала
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, journal.bytes.Length + 1);
            for (int j = 1; j <= journal.bytes.Length; j++) { OutBuf[j] = journal.bytes[j - 1]; } //формируем пакет           
            OutBuf[3] = Convert.ToByte(recNo); //корректируем номер записи журнала по счётчику цикла (для журнала событий)
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                                                                                                                                                                       
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormJournalArray(DataProcessing dp, CounterJournalCQCToRead journal, int recNo) //recNo - Номер записи журнала
        {//процедура, формирующая массив для чтения журнала
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, journal.bytes.Length + 1);
            for (int j = 1; j <= journal.bytes.Length; j++) { OutBuf[j] = journal.bytes[j - 1]; } //формируем пакет           
            OutBuf[3] = Convert.ToByte(recNo); //корректируем номер записи журнала по счётчику цикла (для журнала событий)
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                                                                                                                                                                       
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormEnergyArray(DataProcessing dp, CounterEnergyToRead energy, int zoneNo = 0) //zoneNo - Номер тарифа
        {//процедура, формирующая массив для чтения энергии
            byte[] OutBuf = new byte[1];
            
            switch (energy.bytes[0])
            {
                case 0x0005://если обычный запрос (энергия с нарастающим итогом)
                    {
                        this.Divider = 1000;//делитель по-умолчанию
                        OutBuf[0] = Convert.ToByte(this.NetAddress);
                        Array.Resize(ref OutBuf, energy.bytes.Length + 1);
                        for (int j = 1; j <= energy.bytes.Length; j++) { OutBuf[j] = energy.bytes[j - 1]; } //формируем пакет            
                        OutBuf[3] = Convert.ToByte(zoneNo);
                        UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
                        Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
                        OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
                        OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                               
                        System.Threading.Thread.Sleep(100);
                        break;
                    }
                case 0x0006://если байт запроса среза энергии на начало месяца (лезем в профиль)
                    {
                        this.Divider = Convert.ToInt16(this.CounterConst) * 2;//делитель равен постоянноя сч-ка * 2 если на начало месяца или профиль    
                        string addressStr = String.Empty;
                        addressStr = energy.bytes[2].ToString("X").PadLeft(2, '0') + energy.bytes[3].ToString("X").PadLeft(2, '0');
                                           
                        if (zoneNo > 0)//если считываем тарифы
                        {
                            int addressInt = Convert.ToInt32(addressStr, 16);//переводим полученную строку в целое число
                            addressInt += 0x0011 * zoneNo; //наращиваем адрес записи для того чтобы вытащить тарифы
                            addressStr = addressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора   
                        }

                        byte RecordAddessHi = Convert.ToByte(addressStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки адреса записи
                        byte RecordAddessLow = Convert.ToByte(addressStr.Substring(2, 2), 16); //получаем младший байт адреса из строки адреса записи    
                        OutBuf = this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, 0x0002, 0x0010);//формируем запрос     
                        break;
                    }
            }
            
            
            return OutBuf;
        }

        public byte[] FormMonitorArray(DataProcessing dp, CounterMonitorParameterToRead monitor) //faseNo - номер фазы
        {//процедура, формирующая массив для чтения монитора
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, monitor.bytes.Length + 1);
            for (int i = 1; i <= monitor.bytes.Length; i++) { OutBuf[i] = monitor.bytes[i - 1]; } //формируем пакет для счётчика                                                              
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы                                          
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormLastProfileRecordArray(DataProcessing dp)
        {   //процедура формирующая массив для получения адреса последней записи профиля
            byte[] OutBuf = new byte[3];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0008; //запрос
            OutBuf[2] = 0x0013;
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, OutBuf.Length + 2);
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256);                                         
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormPowerProfileArray (DataProcessing dp, byte RecordAddressHi, byte RecordAddressLow, byte MemoryNumber, byte BytesInfo)
        {   //функция возвращает массив для запроса на чтение профиля мощности              
            //RecordAddress - адрес очередной записи
            byte[] OutBuf = new byte[6];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0006; //запрос чтения памяти по физ адресам
            OutBuf[2] = MemoryNumber; //номер памяти                      
            OutBuf[3] = RecordAddressHi; //старший байт адреса записи
            OutBuf[4] = RecordAddressLow; //младший байт адреса записи
            OutBuf[5] = BytesInfo; //кол-во байт для считывания
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, 8);
            OutBuf[6] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[7] = Convert.ToByte(crc / 256);                                           
            System.Threading.Thread.Sleep(100);   
            return OutBuf;
        }



        public Bitmap DrawVectorDiagramm(PictureBox pixBox,RichTextBox richText)
        {
            //рисуем векторную диаграмму           
            Bitmap map = new Bitmap(pixBox.Width, pixBox.Height); //создаём точечное изображение         
            Graphics g = Graphics.FromImage(map);//создаём графический объект на основе точечного изображения
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//сглаживание
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; //сглаживание текста       
            float pen_width = (float)5.5;//ширина пера для рисования линий
            Pen p1 = new Pen(Color.Red, pen_width);//перо для рисования фазы 1 (вектор напряжения)
            p1.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
            Font f = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);

            float line_length = 150;//длина линии
            float x_centre = pixBox.Width / 2;//центральные координаты канвы 
            float y_centre = pixBox.Height / 2;//центральные координаты канвы
            //рисуем первую фазу (от неё под углом идут все остальные)
            g.DrawString("Пофазная векторная диаграмма", f, Brushes.Black, 40, 2);
            g.DrawLine(p1, x_centre, y_centre, x_centre + line_length, y_centre);//рисуем вектор напряжения для фалы 1
            g.DrawString("Фаза 1", richText.Font, Brushes.Red, x_centre + line_length, y_centre);
            //вектор тока фазы 1
            {
                Pen p = new Pen(Color.Pink, pen_width);//перо для рисования фазы 1 (вектор тока)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase1)*180)/Math.PI);//получаем угол вектора тока

                if (this.MonitorToRead[3].phase1 < 0) { angle_current = 360 - angle_current; }

                float x_current, y_current; line_length = 100;//длина линии
                x_current = x_centre + line_length * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_current / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                y_current = y_centre + line_length * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_current / 360));//вычисляем координату У второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                p.Dispose();
            }
            //Угол между фазами 1 и 2
            {
                Pen p = new Pen(Color.Blue, pen_width);//перо для рисования фазы 2 (вектор напряжения)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                double angle_voltage = this.MonitorToRead[7].phase0; //углы между фазами
                float x, y; line_length = 150;//длина линии
                x = x_centre + line_length * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_voltage / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                y = y_centre + line_length * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_voltage / 360));//вычисляем координату У второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x, y);//рисуем фазу           
                g.DrawString("Фаза 2", richText.Font, Brushes.Blue, x, y);
                p.Dispose();
                //вектор тока фазы 2
                {
                    Pen p2 = new Pen(Color.LightBlue, pen_width);//перо для рисования фазы 2 (вектор тока)
                    p2.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                    double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase2) * 180) / Math.PI);//получаем угол вектора тока

                    if (this.MonitorToRead[3].phase2 < 0) { angle_current = 360 - angle_current; }

                    float x_current, y_current; line_length = 100;//длина линии
                    x_current = x_centre + line_length * Convert.ToSingle(Math.Cos(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                    y_current = y_centre + line_length * Convert.ToSingle(Math.Sin(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату У второго конца линии в зависимости от угла
                    g.DrawLine(p2, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                    p2.Dispose();
                }
            }
            //Угол между фазами 1 и 3
            {
                Pen p = new Pen(Color.Green, pen_width);//перо для рисования фазы 3 (вектор напряжения)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
                double angle_voltage = this.MonitorToRead[8].phase0; //углы между фазами

                float x, y; line_length = 150;//длина линии
                x = x_centre + line_length * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_voltage / 360));//вычиляем координаты второго конца линии в зависимости от угла
                y = y_centre + line_length * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_voltage / 360));//вычиляем координаты второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x, y);//рисуем фазу              
                g.DrawString("Фаза 3", richText.Font, Brushes.Green, x, y);
                p.Dispose();
                //вектор тока фазы 3
                {
                    Pen p2 = new Pen(Color.PaleGreen, pen_width);//перо для рисования фазы 3 (вектор тока)
                    p2.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
                    double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase3) * 180) / Math.PI);//получаем угол вектора тока

                    if (this.MonitorToRead[3].phase3 < 0) { angle_current = 360 - angle_current; }

                    float x_current, y_current; line_length = 100;//длина линии
                    x_current = x_centre + line_length * Convert.ToSingle(Math.Cos(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                    y_current = y_centre + line_length * Convert.ToSingle(Math.Sin(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату У второго конца линии в зависимости от угла
                    g.DrawLine(p2, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                    p2.Dispose();
                }
            }
            p1.Dispose();
            g.Dispose();
            return map;
        }

        public string ValidateWriteParameterAnswer(byte[] InArray, byte offset)
        {
            //процедура, которая разбирает ответ от счётчика на попытку записи параметра
            try
            {
                if (InArray[1 + offset] != 0)
                {
                    switch (InArray[1 + offset])
                    {
                        case 1: return "Недопустимая команда или параметр ";
                        case 2: return "Внутренняя ошибка счётчика ";
                        case 3: return "Недостаточен уровень доступа ";
                        case 4: return "Внутренние часы счетчика уже корректировались в течение текущих суток ";
                        case 5: return "Не открыт канал связи ";
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return String.Empty;//есои первый байт ответа был 0, то ошибок нет
        }

        public byte[] FormParamNewValueArray(List<FieldsValuesToWrite> ValuesList, string paramName, byte[] additional = null,  char stringDivider = '/')
        {//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки (формируется в интерфейсе)   
            byte[] result = new byte[0];
            string newValueStr = String.Empty;
            switch (paramName)
            {
                case "Дата и время"://установка времени
                    {
                        FieldsValuesToWrite field = ValuesList.Find(x => x.name == "DTPicker");//ищем в списке значение поля по названию контрола
                        newValueStr = field.value;//будущее значение параметра берётся из контрола, значение поля которого сохранено в списке
                        Array.Resize(ref result, 8);

                        result[0] = Convert.ToByte(newValueStr.Substring(18, 2).PadLeft(2, '0'), 16);//секунды
                        result[1] = Convert.ToByte(newValueStr.Substring(15, 2).PadLeft(2, '0'), 16);//минуты
                        result[2] = Convert.ToByte(newValueStr.Substring(12, 2).PadLeft(2, '0'), 16);//часы

                        string day_of_week = newValueStr.Substring(9, 2);//день недели
                        switch (day_of_week)
                        {
                            case "Пн":
                                result[3] = 1;
                                break;
                            case "Вт":
                                result[3] = 2;
                                break;
                            case "Ср":
                                result[3] = 3;
                                break;
                            case "Чт":
                                result[3] = 4;
                                break;
                            case "Пт":
                                result[3] = 5;
                                break;
                            case "Сб":
                                result[3] = 6;
                                break;
                            case "Вс":
                                result[3] = 7;
                                break;
                        }

                        result[4] = Convert.ToByte(newValueStr.Substring(0, 2).PadLeft(2, '0'), 16);//день месяца
                        result[5] = Convert.ToByte(newValueStr.Substring(3, 2).PadLeft(2, '0'), 16);//месяц
                        result[6] = Convert.ToByte(newValueStr.Substring(6, 2).PadLeft(2, '0'), 16);//год
                        result[7] = (byte)(1);//зима\лето
                        break;
                    }

                case "Коррекция времени в пределах 4 мин"://коррекция времени в пределах 4 мин
                    {
                        FieldsValuesToWrite field = ValuesList.Find(x => x.name == "DTPicker");//ищем в списке значение поля по названию контрола
                        newValueStr = field.value;//будущее значение параметра берётся из контрола, значение поля которого сохранено в списке
                        Array.Resize(ref result, 3);
                        result[0] = Convert.ToByte(newValueStr.Substring(18, 2).PadLeft(2, '0'), 16);//секунды
                        result[1] = Convert.ToByte(newValueStr.Substring(15, 2).PadLeft(2, '0'), 16);//минуты
                        result[2] = Convert.ToByte(newValueStr.Substring(12, 2).PadLeft(2, '0'), 16);//часы
                        break;
                    }

                case "Инициализация основного профиля"://инициализация профиля
                    {
                        Array.Resize(ref result, 2);
                        result[0] = (Convert.ToByte("30"));//период интегрирования
                        result[1] = 1;//дополнительный параметр стирать память или нет
                        break;
                    }                    
            }
            return result;
        }

        public bool Search(string textToFind, StringComparison compare, string add = "")
        {
            if ((Utils.Contains(this.Name, textToFind, StringComparison.CurrentCultureIgnoreCase))
            || (Utils.Contains(this.SerialNumber, textToFind, StringComparison.CurrentCultureIgnoreCase))
            || (Utils.Contains(add, textToFind, StringComparison.CurrentCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        public bool GainAccessOnModem(DataProcessing dp, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait)
        {   //процедура получения доступа к счётчику (открытие канала)            
            try
            {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Открытие канала к " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));
                byte[] gainOutBuf = this.FormGainAccessArray(dp, lvl, pwd); //вызываем процедуру формирования тестового массива
                Exception ex = dp.SendData(gainOutBuf, 0); //посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return false;
                }

                if (worker.CancellationPending == true) { return false; } //проверяем, был ли запрос на отмену работы
                byte[] answerArray = dp.Read(bytestowait, 5000, true);
                bool success = this.ValidateReadParameterAnswer(answerArray, 0);//пусть счётчик сам оценит успешность выполнения запроса согласно своему протоколу. Здесь 0 - открытие канала связи
                if (!success) //если попытка доступа не удалась
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    //пишем ошибку в базу
                    SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(2, this.Name, this.ID);
                    if (sqlex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return false;
                    }
                    return false;
                }
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.DarkGreen;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                    richText.ScrollToCaret();
                }));
                return true;
            }
            catch
            {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                    richText.ScrollToCaret();
                }));
                //пишем ошибку в базу
                SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(2, this.Name, this.ID);
                if (sqlex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return false;
                }
                return false;
            }
        }

        public bool GainAccessOnGate(Mercury228 gate, DataProcessing dp, int packnum, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait)
        {
            //процедура получения доступа к счётчику (открытие канала)    
            try
            {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Открытие канала к " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] gainOutBuf = gate.FormPackage(this.FormGainAccessArray(dp, lvl, pwd), 1, dp, packnum); //вызываем процедуру формирования тестового массива        
                Exception ex = dp.SendData(gainOutBuf, 0); //посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return false;
                }

                if (worker.CancellationPending == true) { return false; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                byte[] answerArray = dp.Read(bytestowait + 9, 5000, true);

                if (answerArray.Length < 8)//в корректном ответе от шлюза всегда не меньше 8 байт
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    //пишем ошибку в базу
                    SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(2, this.Name, this.ID);
                    if (sqlex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return false;
                    }
                    return false;
                }

                bool success = this.ValidateReadParameterAnswer(answerArray, 0, 8);//пусть счётчик сам оценит успешность выполнения запроса согласно своему протоколу. Здесь 0 - открытие канала связи. Третий параметр - отступ заголовка пакета шлюза
                if (!success) //если попытка доступа не удалась
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    //пишем ошибку в базу
                    SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(2, this.Name, this.ID);
                    if (sqlex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return false;
                    };

                    return false;
                }
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.DarkGreen;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                    richText.ScrollToCaret();
                }));
                return true;
            }
            catch
            {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                    richText.ScrollToCaret();
                }));
                //пишем ошибку в базу
                SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(2, this.Name, this.ID);
                if (sqlex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return false;
                }
                return false;
            }
        }

        public void ReadMonitorOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ МОДЕМ
        {
            if (this.GainAccessOnModem(dp, richText, 1, 1, ref worker, 4) == false) { return; }//получение доступа (открытие канала)

            richText.Invoke(new Action(delegate
             {
                 pb.Value = 0;//обнуляем прогресс бар
                 pb.Maximum = this.MonitorToRead.Count; //количество сегментов прогресс бара = количество параметров
             }));

            foreach (var monitor in this.MonitorToRead)
            {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + monitor.name + " " + this.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] OutBuf = this.FormMonitorArray(dp, monitor);//посылаем запрос на монитор (прилетят сумма + фазы)
                Exception ex = dp.SendData(OutBuf, 0); //посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                byte[] answerArray = dp.Read(monitor.bytesToWait, 20000, true); //ждём определённое кол-ва байт ответа   

                int phasestart = 0;//номер начальной фазы (определяет считать с суммой или только по фазам или только сумму)
                int phaseend = 3;//номер последней фазы (определяет считать с суммой или только по фазам или только сумму)
                if ((monitor.name == "Мощность P") || (monitor.name == "Мощность S") || (monitor.name == "Мощность Q")) { phasestart = 0; phaseend = 3; }//вытаскиваем сумму и фазы
                if ((monitor.name == "Ток") || (monitor.name == "Коэффициент мощности") || (monitor.name == "Напряжение, усреднённое") || (monitor.name == "Напряжение")) { phasestart = 1; phaseend = 3; }//вытаскиваем только фазы
                if ((monitor.name == "Частота") || (monitor.name == "Частота, усреднённая") || (monitor.name == "Угол между фазными напряжениями 1 и 2") || (monitor.name == "Угол между фазными напряжениями 1 и 3")
                || (monitor.name == "Угол между фазными напряжениями 2 и 3"))
                { phasestart = 0; phaseend = 0; }//будем вытаскивать только сумму
                                                 //в цикле разбираем ответы чтобы раскидать по фазам
                for (int i = phasestart; i <= phaseend; i++)
                {
                    double value = this.ParseMonitorValue(dp, answerArray, monitor.name, i, 0);//читаем значение монитора из принятого ответа (в ответе все фазы)
                                                                                                  //проверяем корректность. Если первый байт ответа не сетевой адрес, или значение монитора посчиталось с ошибкой, то следующая итерация
                    if ((answerArray[0] != this.NetAddress) || (value == -1))
                    {
                        monitor.spreadValueByName("phase" + i.ToString(), -1);
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
                    //эта процедура распределяет ответы по свойствам класса по имени свойства (в данном случае номер фазы)
                    monitor.spreadValueByName("phase" + i.ToString(), value);
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " удачно\r");
                        richText.ScrollToCaret();
                    }));
                }
            }
            pixBox.Image = this.DrawVectorDiagramm(pixBox, richText);//рисуем векторную диаграмму
        }

        public void ReadParametersOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)//этот метод полностью реализует чтение параметров счётчика по его правилам ЧЕРЕЗ ШЛЮЗ (в пакетном режиме)
        {
            byte[] Package = null;
            byte[] answerArray = null;
            DateTime currentDate;
            //цикл по отмеченным параметрам
            int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
            List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)  
            foreach (var param in this.ParametersToRead)
            {
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                if (param.check == false) continue;
                packnum += 1;//наращиваем номер пакета
                Package = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
                PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                CountToWait += param.bytesToWait + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок + хвост пакета)
            }

            if (PackagesBuffer.Count > 0)
            {//если пакеты набрались, нужно их послать и принят ответ
                currentDate = DateTime.Now;
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем параметры...\r");
                    richText.ScrollToCaret();
                }));
                byte[] OutBufParam = new byte[0];
                //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                //составляем его из окна пакетов       
                foreach (byte[] pack in PackagesBuffer)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    Array.Resize(ref OutBufParam, OutBufParam.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                    Array.Copy(pack, 0, OutBufParam, OutBufParam.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                }
                //после того, как сформировали серию пакетов, отправляем её на порт и ждём ответов
                Exception ex = dp.SendData(OutBufParam, 0);//посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                answerArray = dp.Read(CountToWait, 15000, true);//ждём ответ. Ожидаемая длина складывается из поля BytesToWait всех параметров (суммы длин всех пакетов в ответе)

                if (answerArray.Length == 5)//если ответ от шлюза слишком короткий
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }
                //теперь нужно разобрать ответ и оформить
                //опять циклимся по параметрам, чтобы выделить те ответы из общего потока, которых мы отметили ранее и ждали
                int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета)
                foreach (var param in this.ParametersToRead)
                {                   
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    if (param.check == false) continue;
                    //если параметр был отмечен галочкой, значит мы ищем ответ на него в общем пакете
                    //Т.к. ответы приходят в том же порядке, в котором уходят запросы, то можно смотреть подряд
                    byte[] curAnswer = new byte[param.bytesToWait];//отсчитываем в общем пакете то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                    Array.Copy(answerArray, offset, curAnswer, 0, param.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответ

                    bool success = this.ValidateReadParameterAnswer(curAnswer);//проверим запрос на успешность выполнения с учётом правил счётчика
                    if (!success)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        offset += 8 + param.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                        continue;
                    }

                    param.value = this.ParseParameterValue(curAnswer, param.name, 0);//отправляем ответ на разбор и возвращаем ответ

                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " удачно: " + param.value + "\r");
                        richText.ScrollToCaret();
                    }));
                    offset += 8 + param.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)      

                    //проверяем, сходится ли время на счётчике со временем на компе. Эта проверка должна быть В ЦИКЛЕ чтобы наглядно для пользователя сразу после считывания проводить проверку и править в случае необходимости
                    if (param.name == "Дата и время")
                    {
                        DateTime dt = DateTime.Now;//значение чисто для инициализации экземпляра

                        try
                        {
                            dt = Convert.ToDateTime(param.value);//значение параметра времени счётчика, которое считали ранее
                        }
                        catch
                        {//если выпала ошибка преобразования
                            continue;//идём на следующий параметр, не пытаясь перепрошивать время
                        }

                        double hours_shift = 0;//сдвиг по часовому поясу (по-умолчанию 0)
                        //проверяем, есть ли счётчик в интегральниках чтобы учесть сдвиг по часовому поясу (нам нужно чтобы в счётчике было зашито московское время)
                        DataTable t = DataBaseManagerMSSQL.Return_Integral_Parameters_Row(this.ID);
                        if (t.Rows.Count == 1)
                        {
                            hours_shift = -4;
                        }

                        double delta_minutes = DateTime.Now.Subtract(dt).TotalMinutes;//разница в минутах между текущим временем на компе и временем в счётчике         

                        if (hours_shift != 0)
                        {//если есть сдвиг часов (другой часовой пояс на счётчике) то это надо учесть и текущее время компьютера модифицировать с учётом сдвига
                            delta_minutes = DateTime.Now.AddHours(hours_shift).Subtract(dt).TotalMinutes;
                        }

                        //пробуем: если время между часами счётчика и часами компа расходятся больше чем на 4 минуты в абсолютном выражении, то либо добавляем к часам счётчика 4 минуты, либо отнимаем от часов счётчика 4 минуты
                        //Почему так, а не просто попытка прошивки времени с компа:
                        //потому что команда коррекции в пределах 4 минут может быть выполнена один раз в сутки и этот процесс может быть итеративным изо дня в день, постепенно приближать часы счётчика к нужному времени.
                        if (Math.Abs(delta_minutes) > 4)//если сдвиг 0, то будет проверка на разницу в 4 минуты.
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Orange;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Разница во времени составляет " + Math.Abs(delta_minutes).ToString() + " минут с учётом сдвига часов " + hours_shift.ToString() + ". Пытаюсь провести коррекцию часов счётчика в пределах 4 минут... " + "\r");
                                richText.ScrollToCaret();
                            }));

                            if (delta_minutes < 0)//если разница во времени отрицательная, значит время на счётчике больше и его надо уменьшить
                            {
                                dt = dt.AddMinutes(-3);//корректируем часы счётчика на 3 минуты назад чтобы приблизить ко времени на компе. 
                            }
                            else
                            {//если разница во времени положительная, значит время на счётчике меньше и его надо увеличить
                                dt = dt.AddMinutes(3);//корректируем часы счётчика на 3 минуты вперёд чтобы приблизить ко времени на компе. 
                            }

                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Orange;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Записываю в счётчик следующие дату и время: " + dt.ToString() + "\r");
                                richText.ScrollToCaret();
                            }));

                            //Т.к. счётчику нужны только часы, минуты и секунды (для формирования нового значения), то здесь можно придать дате (строке, её представляющей) необходимый формат, чтобы она вписалась в алгоритм
                            string fakeDateTime = dt.ToString();
                            fakeDateTime = fakeDateTime.Remove(0, 11);//удаляем всё, что левее от времен (дату)
                            fakeDateTime = fakeDateTime.PadLeft(20, '0');//нам нужны нули слева (чтобы придать строке необходимую длину для алгоритма разбора и формирования нового значения для записи)
                                                                         //список значений полей
                            List<FieldsValuesToWrite> FieldsValuesToWrite = new List<FieldsValuesToWrite>();
                            //заполняем список значениями полей
                            FieldsValuesToWrite.Add(new FieldsValuesToWrite("DTPicker", fakeDateTime));
                            //проводим коррекцию времени в пределах 4 минут
                            this.ParametersToWrite[1].check = true;//помечаем параметр на запись         
                            gate.WriteParametersToDevice(this.tn, dp, richText, worker, FieldsValuesToWrite);//пишем новое значение параметра в счётчик
                            this.ParametersToWrite[1].check = false;//снимаем пометку параметра на запись
                        }
                    }
                }
            }
        }

        public void ReadMonitorOnGate(Mercury228 gate, string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ ШЛЮЗ
        {
            //процедура получения монитора по фазам и сумме
            int packnum = 0;
            //============================================================================================================================================
            //сначала нужно записать настройки порта№1 в шлюз (автоконфигурация)--------------------------------------------------------------------------
            DateTime currentDate = DateTime.Now;

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Автоконфигурация порта №1...\r");
                richText.ScrollToCaret();
            }));
            //формируем пакет для записи настроек в порт шлюза
            byte[] Package = gate.FormPackage(gate.PortConfigArray(1), 0, dp, packnum);
            Exception ex = dp.SendData(Package, 0);//посылаем запрос
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return;
            }

            if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            byte[] answerArray = dp.Read(ParametersToWrite[0].bytes.Length + ParametersToWrite[0].value.Length + 9, 3000, true);//ждём ответ

            if ((answerArray.Length == 5) && (answerArray[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                    richText.ScrollToCaret();
                }));
                return;
            }
            currentDate = DateTime.Now;

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                richText.ScrollToCaret();
            }));
            //--------------------------------------------------------------------------------------------------------------------------------------------
            //==============================================================================================================================================           
            packnum += 1;
            if (this.GainAccessOnGate(gate, dp, packnum, richText, 1, 1, ref worker, 4) == false) { return; }//получение доступа (открытие канала)

            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;//обнуляем прогресс бар
                pb.Maximum = this.MonitorToRead.Count; //количество сегментов прогресс бара = количество параметров
            }));
            List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)  
            int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем монитор...\r");
                richText.ScrollToCaret();
            }));
            //берём только отмеченные параметры
            //var checkedMonitor = from monitor in this.MonitorToRead where monitor.check == true select monitor;
            //цикл по монитору
            foreach (var monitor in this.MonitorToRead)
            {
                if (monitor.check == false) continue;
                packnum += 1;//наращиваем номер пакета
                Package = gate.FormPackage(this.FormMonitorArray(dp, monitor), 1, dp, packnum);
                PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                CountToWait += monitor.bytesToWait + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок + хвост пакета              
            }
            byte[] OutBufMonitor = new byte[0];
            //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
            //составляем его из окна пакетов       
            foreach (byte[] pack in PackagesBuffer)
            {
                Array.Resize(ref OutBufMonitor, OutBufMonitor.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                Array.Copy(pack, 0, OutBufMonitor, OutBufMonitor.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
            }
            ex = dp.SendData(OutBufMonitor, 0); //посылаем запрос
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return;
            }

            if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            answerArray = dp.Read(CountToWait, 5000, true); //ждём определённое кол-ва байт ответа 

            //если первая буква E в ответе, то ошибка
            if (answerArray[0] == 0x0045)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                    richText.ScrollToCaret();
                }));
                return;
            }
            int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок пакета)
            //эти ответы нужно разбирать в цикле пофазно + сумма
            foreach (var monitor in this.MonitorToRead)
            {
                if (monitor.check == false) continue;
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + monitor.name + "...\r");
                    richText.ScrollToCaret();
                }));
                int phasestart = 0;//номер начальной фазы (определяет считать с суммой или только по фазам или только сумму)
                int phaseend = 3;//номер последней фазы (определяет считать с суммой или только по фазам или только сумму)
                                 //следующие несколько условий определяют, нужна ли сумма или по фазам
                if ((monitor.name == "Мощность P") || (monitor.name == "Мощность S") || (monitor.name == "Мощность Q")) { phasestart = 0; phaseend = 3; }//вытаскиваем сумму и фазы
                if ((monitor.name == "Ток") || (monitor.name == "Коэффициент мощности") || (monitor.name == "Напряжение, усреднённое") || (monitor.name == "Напряжение")) { phasestart = 1; phaseend = 3; }//вытаскиваем только фазы
                if ((monitor.name == "Частота") || (monitor.name == "Частота, усреднённая") || (monitor.name == "Угол между фазными напряжениями 1 и 2") || (monitor.name == "Угол между фазными напряжениями 1 и 3")
                || (monitor.name == "Угол между фазными напряжениями 2 и 3"))
                { phasestart = 0; phaseend = 0; }//будем вытаскивать только сумму

                byte[] curAnswer = new byte[monitor.bytesToWait];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                Array.Copy(answerArray, offset, curAnswer, 0, monitor.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа
                                                                                   //в цикле разбираем ответы чтобы раскидать по фазам
                for (int i = phasestart; i <= phaseend; i++)
                {
                    //читаем ответ. Первый байт ответа всегда - сетевой адрес. Если это не так - ошибка
                    double value = this.ParseMonitorValue(dp, curAnswer, monitor.name, i, 0);//читаем значение монитора из принятого ответа (в ответе все фазы)
                                                                                                //проверяем корректность. Если первый байт ответа не сетевой адрес, или значение монитора посчиталось с ошибкой, то следующая итерация
                    if ((curAnswer[0] != this.NetAddress) || (value == -1))
                    {
                        monitor.spreadValueByName("phase" + i.ToString(), -1);
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " неудачно\r");
                            richText.ScrollToCaret();
                        }));
                        //offset += 8 + monitor.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)
                        continue;
                    }
                    //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
                    //эта процедура распределяет ответы по свойствам класса по имени свойства (в данном случае номер фазы)
                    monitor.spreadValueByName("phase" + i.ToString(), value);
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " удачно\r");
                        richText.ScrollToCaret();
                    }));
                }
                offset += 8 + monitor.bytesToWait + 1;//наращиваем отступ вглубь общего массива ответа (заголовок + кол-во байт полезной части + 1 хвост пакета)                          
                //pb.Value += 1;
            }
            pixBox.Image = this.DrawVectorDiagramm(pixBox, richText);//рисуем векторную диаграмму
        }

        public DataTable GetPowerProfileForCounterOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent)
        {   //процедура получения профиля мощности для выбранного счётчика Mercury230RS485
            //берём уже ранее созданную заготовку для хранения записей профиля
            DataTable dt = this.ProfileDataTable;
            if (!ReReadOnlyAbsent) dt.Clear();//если пытаемся дочитать недостающие записи, то очищать таблицу профиля нельзя
            //
            dt.Columns[0].AllowDBNull = true;
            dt.Columns[1].AllowDBNull = true;
            dt.Columns[2].AllowDBNull = true;

            if (DateN >= DateK)
            {
                DateTime currentDateZ = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDateZ + "." + currentDateZ.Millisecond + " Ошибка чтения профиля: начальная дата не может быть равна или больше конечной!\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //----получение доступа (открытие канала) к счётчику
            if (this.GainAccessOnModem(dp, richText, 1, 1, ref worker, 4) == false) { return dt; }
            //---получение постоянной счётчика
            CounterParameterToRead param = this.ParametersToRead[11];
            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));

            byte[] OutBufConst = this.FormParameterArray(dp, param); //вызываем процедуру формирования запроса постоянной счётчика             
            Exception ex = dp.SendData(OutBufConst, 0); //посылаем запрос
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            byte[] answerArray = dp.Read(param.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа
            //читаем ответ. Первый байт ответа всегда - сетевой адрес. Если это не так - ошибка
            if ((answerArray[0] != this.NetAddress) || (answerArray.Length == 5))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения постоянной счётчика\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                richText.ScrollToCaret();
            }));

            param.value = this.ParseParameterValue(answerArray, param.name, 0);//разбираем и запоминаем значение постоянной счётчика 
            //---конец получения постоянной счётчика

            int multiplier = 1;//множитель адреса начальной записи профиля мощности. Зависит от версии ПО счётчика
            int MaxRecordAddressInt = 0xFFFF; //максимальный адрес памяти счётчика. 
            byte MemoryNumberAnd17thBit = 0x0003;//номер памяти и значение 17го бита адреса по-умолчанию (в данном случае номер памяти 3 и значение 17го бита адреса = 0)

            DateTime CurFiguredDateTime = DateN;//расчётные дата и время. Начальное значение

            param = this.ParametersToRead[14];//нужно получить версию ПО. От неё записит нужно ли будет умножить адрес последней записи на 10h или нет
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));
            //---получаем версию ПО счётчика
            byte[] OutBufVer = this.FormParameterArray(dp, param); //вызываем процедуру формирования запроса версии ПО            
            ex = dp.SendData(OutBufVer, 0); //посылаем запрос
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            answerArray = dp.Read(param.bytesToWait, 10000, true); //ждём определённое кол-ва байт ответа
            //читаем ответ. Первый байт ответа всегда - сетевой адрес. Если это не так - ошибка
            if ((answerArray[0] != this.NetAddress) || (answerArray.Length == 5))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения версии ПО\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            try
            {
                param.value = this.ParseParameterValue(answerArray, param.name, 0);//разбираем и запоминаем значение версии ПО счётчика      
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
            }
            catch
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения версии ПО\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            
            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
            }));
            //---конец получения версии ПО счётчика

            if (Convert.ToInt32(param.value) >= 70100) { multiplier = 0x0010; } //смотрим если версия ПО больше или равно 7.1.0 (70100 в виде числа), то устанавливаем множитель адреса последней записи профиля в 10h
            //Для счетчиков с версией ПО 7.1.0 и более поздних значение поля адреса последней записи должно быть умножено на 0x0010h
            byte[] OutBufLastRec = this.FormLastProfileRecordArray(dp);//формируем запрос на получение адреса последней записи профиля
            ex = dp.SendData(OutBufLastRec, 0); //посылаем запрос      
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
            byte[] LastRecord = dp.Read(12, 10000, true);//ждём определённое кол-ва байт ответа
            //читаем ответ
            if (LastRecord.Length == 5)
            {
                currentDate = DateTime.Now;
                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения адреса последней записи профиля\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            string State_Str = "";
            //AnalyzeStateByte(LastRecord[3]);//анализируем байт состояния последней записи профиля мощности           

            //формируем строку из старшего и младшего байтов адреса последней записи
            string LastRecordAddressStr = LastRecord[1].ToString("X").PadLeft(2, '0') + LastRecord[2].ToString("X").PadLeft(2, '0');
            //формируем строку даты\времени последней записи чтобы от неё потом вести отсчёт
            string LastRecordDateTimeStr = LastRecord[6].ToString("X").PadLeft(2, '0') + "." + LastRecord[7].ToString("X").PadLeft(2, '0')
                               + "." + LastRecord[8].ToString("X").PadLeft(2, '0') + " " + LastRecord[4].ToString("X").PadLeft(2, '0')
                               + ":" + LastRecord[5].ToString("X").PadLeft(2, '0');
            int LastRecordAddressInt = Convert.ToInt32(LastRecordAddressStr, 16);//переводим строку адреса последней записи в целое число для расчёта адресов записей с искомыми датами
            LastRecordAddressInt *= multiplier; //Для счетчиков с версией ПО 7.1.0 и более поздних значение поля адреса последней записи должно быть умножено на 0x0010h
            DateTime LastRecordDateTime = Convert.ToDateTime(LastRecordDateTimeStr);//переводим сформированную строку даты последней записи в тип DateTime 

            TimeSpan ts = LastRecordDateTime.Subtract(DateN); //разница между датами (начальной датой и датой последней записи)
            int MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до даты последней записи
            int PeriodsCount = MinutesElapsed / LastRecord[9]; //количество периодов интегрирования от начальной даты до даты последней записи (количество прошедших минут / период интегрирования)          
            int DateNRecordAddressInt = LastRecordAddressInt - 0x0010 * (PeriodsCount - 1); //высчитываем адрес записи с начальной датой
            if (DateNRecordAddressInt > 0xFFFF) { DateNRecordAddressInt = Convert.ToInt16(DateNRecordAddressInt.ToString("X").Substring(1, 4), 16); }//если число слишком велико (больше 2 байт), то отрезаем первую цифру   
            ts = DateK.Subtract(DateN); //разница между датами (начальной и конечной)

            MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до конечной даты
            PeriodsCount = MinutesElapsed / LastRecord[9]; //расчётное количество периодов интегрирования от начальной даты до конечной даты                    
            string CurRecordAddressStr = String.Empty;
            CurRecordAddressStr = DateNRecordAddressInt.ToString("X").PadLeft(4, '0'); //конвертируем полученный адрес для начальной даты для последующего разбора на байты 
            //если число отрицательное, то видимо адресное пространство начало заполняться по кругу. Тогда убираем первые FFFF
            if (DateNRecordAddressInt < 0) CurRecordAddressStr = CurRecordAddressStr.Substring(4, 4);

            currentDate = DateTime.Now;

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем профиль...\r");
                richText.ScrollToCaret();

                pb.Value = 0;
                pb.Maximum = PeriodsCount;//прогресс бар начальные значения - количество периодов интегрирования между начальной и конечной датами         
                crl.Text = "0";
                lrl.Text = " из " + PeriodsCount.ToString();//устанавливаем значение текстовых меток для отображения прогресса считывания
            }));

            int attempts = 2;//кол-во попыток съема получасовки (проблема 17го бита для счётчика версии больше 70100)
            byte t = 30;//период интегрирования (по-умолчанию)
            int CurRecordAddressInt = 0;//адрес текущей записи в целом виде для наращивания
            int rollback = 0;//нужна в случае, когда возникают дополнительные итерации для переопроса спорного адреса

            if (this.ProfileDataTable.Rows.Count == 0)
                ReReadOnlyAbsent = false;//если профиль в счётчике отсутствует (не загрузился из базы), то полагаем, что это опрос с нуля и не учитываем значение галочки
            else ReReadOnlyAbsent = true;

            for (int i = 0; i < PeriodsCount; i++)//самый верхний цикл
            {
                if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                if (ReReadOnlyAbsent)//если поставили галочку на проверку недостающих записей
                {
                    if (!Convert.IsDBNull(this.ProfileDataTable.Rows[i - rollback][3]))//если запись в счётчике не пустая
                    {
                        //если значение поля не null, значит такая запись уже есть и можно шагать дальше (игнорируем)
                        CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                        CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                        if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой 
                        CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                        CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);//наращиваем расчётную дату при помощи периода интегрирования
                        attempts = 2;//в случае успешного съема получасовки восстанавливаем её попытки

                        richText.Invoke(new Action(delegate
                        {
                            try
                            {
                                pb.Value += 1; //прогресс бар - успех получения очередной записи профиля  
                                crl.Text = pb.Value.ToString();//в позиции ХХХ строка отсутствует??? Пока непонятно почему стала появляться эта ошибка
                            }
                            catch (Exception ex777)
                            {
                                currentDate = DateTime.Now;
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + ex777.Message + "\r");
                                richText.ScrollToCaret();
                            }
                        }));
                        continue;
                    }
                }

                if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                                                                       //проверяем кол-во попыток съема получасовки
                                                                       //если попыток съема получасовки не осталось из-за того что попалась пустышка (для версии ПО больше 70100), то идём на следующую получасовку
                if (attempts == 0 && Convert.ToInt32(param.value) >= 70100)
                {
                    t = 30;//возвращаем период интегрирования
                    currentDate = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        try
                        {
                            pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля т.к. там пустышка по этому адресу
                            crl.Text = pb.Value.ToString();
                        }
                        catch (Exception ex777)
                        {
                            currentDate = DateTime.Now;
                            richText.SelectionColor = Color.DarkOrange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + ex777.Message + "\r");
                            richText.ScrollToCaret();
                        }
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: не осталось попыток для считывания текущего расчётного времени. Расчётное время: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Версия ПО счётчика: " + param.value + ". " + State_Str + "\r");
                        richText.ScrollToCaret();
                    }));

                    DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                    drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01");
                    dt.Rows.Add(drE);

                    CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                    CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                    if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой 
                    CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                    CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);//наращиваем расчётную дату при помощи периода интегрирования  
                    attempts = 2;

                    continue;
                }

                try
                {//самый верхний try внутри верхнего цикла
                    if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                    byte RecordAddessHi = Convert.ToByte(CurRecordAddressStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущей записи
                    byte RecordAddessLow = Convert.ToByte(CurRecordAddressStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущей записи
                    byte BytesInfo = 0x000F;//количество байт информации, которое мы хотим считать
                    byte[] OutBufPowerProf = this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, MemoryNumberAnd17thBit, BytesInfo);//формируем запрос на получение профиля
                    Exception ex2 = dp.SendData(OutBufPowerProf, 0); //посылаем запрос               
                    if (ex2 != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex2.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return dt;
                    }

                    if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                    byte[] ProfileRecord = dp.Read(18, 3000, true);//ждём определённое кол-ва байт ответа   

                    if (ProfileRecord.Length == 5)//если ответ слишком короткий (данных нет)
                    {
                        currentDate = DateTime.Now;
                        richText.Invoke(new Action(delegate
                        {
                            pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля
                            crl.Text = pb.Value.ToString(); richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: короткий ответ (5 байт). Предположительно нет данных на следующий период интегрирования: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Версия ПО счётчика: " + param.value + "\r");
                            richText.ScrollToCaret();
                        }));
                        //добавляем строку с ошибкой
                        DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                        drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01");
                        dt.Rows.Add(drE);

                        CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                        CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                        if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                        CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                        CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);
                        attempts = 2;//обнуляем кол-во попыток съема получасовки для следующей итерации                         

                        continue;
                    }
                    //если данные есть (не 5 байт), то продолжаем                        
                    State_Str = AnalyzeStateByte(ProfileRecord[1]);//анализируем байт состояния для каждой считанной записи профиля мощности

                    DataRow dr = dt.NewRow(); //добавляем строку в таблицу
                    t = ProfileRecord[7];//длительность периода интегрирования - нужна при расчёте                     
                    if (t == 0) t = 1;//чтобы не делить на 0 при расчёте
                    int a = Convert.ToInt16(this.CounterConst);//постоянная сч-ка - нужна при расчёте

                    //разбираем полученную запись
                    string valueStr = ProfileRecord[9].ToString("X").PadLeft(2, '0') + ProfileRecord[8].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (A+)
                    if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                    double N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                    double value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                    dr["e_a_plus"] = value / 2;//заносим значение в строку

                    valueStr = ProfileRecord[11].ToString("X").PadLeft(2, '0') + ProfileRecord[10].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (A-)
                    if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                    N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                    value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                    dr["e_a_minus"] = value / 2;//заносим значение в строку

                    valueStr = ProfileRecord[13].ToString("X").PadLeft(2, '0') + ProfileRecord[12].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (R+)
                    if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                    N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                    value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                    dr["e_r_plus"] = value / 2;//заносим значение в строку

                    valueStr = ProfileRecord[15].ToString("X").PadLeft(2, '0') + ProfileRecord[14].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (R-)
                    if (valueStr == "FFFF") { valueStr = "0"; }//если получили такие байты, это значит что данных нет
                    N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                    value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                    dr["e_r_minus"] = value / 2;//заносим значение в строку
                                                //собираем дату из байт массива
                    valueStr = ProfileRecord[4].ToString("X").PadLeft(2, '0') + "." + ProfileRecord[5].ToString("X").PadLeft(2, '0')
                               + "." + ProfileRecord[6].ToString("X") + " " + ProfileRecord[2].ToString("X").PadLeft(2, '0')
                               + ":" + ProfileRecord[3].ToString("X").PadLeft(2, '0');

                    if (valueStr == "00.00.0 00:00") //если получили такую строку, то это значит, что информации за желаемый период в ответе нет (данные есть, но они 0).
                    {
                        //подкидываем заведомо ложную дату чтобы перейти на блок исправления 17го бита и избежать исключения при конвертации (которое должно вызываться при настоящей ошибке или некорректных данных)
                        valueStr = "01.01.2000 00:30";
                    }
                    //видим несоответствие дат расчётной и фактической
                    if (Convert.ToDateTime(valueStr).AddMinutes(-t) != CurFiguredDateTime)
                    {  //здесь идёт проверка на правильность 17го бита адреса памяти №3 для версии ПО старше 7.1.0. СПОСОБА РАСЧЁТА ЗАРАНЕЕ ПОКА НЕ НАШЁЛ 
                        if (Convert.ToInt32(param.value) >= 70100)
                        {
                            attempts -= 1;//уменьшаем кол-во попыток съема получасовки т.к. дата не соответствует расчётной
                                          //теперь решаем вопрос: это реально пустая получасовка или 17ый бит нужно изменить и перечитать получасовку                            
                            {
                                if (MemoryNumberAnd17thBit == 0x0003)
                                {
                                    {
                                        PeriodsCount += 1;//добавим ещё одну итерацию, т.к. текущая оказалась неверной из-за неправильного 17го бита адреса
                                        MemoryNumberAnd17thBit = 0x00083; //меняем 17ый бит и пытаемся перечитать получасовку
                                        rollback += 1;
                                        continue;
                                    }
                                }
                                if (MemoryNumberAnd17thBit == 0x0083)
                                {
                                    {
                                        PeriodsCount += 1;//добавим ещё одну итерацию, т.к. текущая оказалась неверной из-за неправильного 17го бита адреса
                                        MemoryNumberAnd17thBit = 0x00003; //меняем 17ый бит и пытаемся перечитать получасовку
                                        rollback += 1;
                                        continue;
                                    }
                                }
                            }

                        }//конец ветки по версии больше 70100
                        else
                        {//если версия меньше 70100
                            t = 30;//возвращаем период интегрирования на случай если он стал 1 при данных 00.00.00
                            currentDate = DateTime.Now;
                            if (valueStr == "01.01.2000 00:30") { valueStr = "нет данных"; } //если мы ранее подкинули левую дату, то по этому ветвлению пишем "нет данных"
                            richText.Invoke(new Action(delegate
                            {
                                pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля т.к. там пустышка по этому адресу
                                crl.Text = pb.Value.ToString(); richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: предположительно нет данных на следующий период интегрирования либо время в счётчике не совпадает с расчётным: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Фактическая метка времени по этому адресу памяти: " + valueStr + ". Версия ПО счётчика: " + param.value + ". " + State_Str + "\r");
                                richText.ScrollToCaret();
                            }));

                            CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                            CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                            if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                            CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                            CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);

                            if (valueStr != "нет данных")//если данные были (не 5 байт) и не нули (не 00.00.00) то пытаемся записать в базу
                            {
                                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                richText.Invoke(new Action(delegate
                                {
                                    richText.SelectionColor = Color.Black;
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Пытаемся записать в базу...\r");
                                    richText.ScrollToCaret();
                                }));

                                dr["date_time"] = valueStr;
                                dr["period"] = t;
                                dt.Rows.Add(dr);

                                Exception ex20 = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                                dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);

                                if (ex20 != null)
                                {
                                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.SelectionColor = Color.Red;
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи профиля в базу: " + ex20.Message + "\r");
                                        richText.ScrollToCaret();
                                    }));
                                }
                            }
                            continue;
                        }//конец ветки по версии меньше 70100
                    }//конец ветки по несоответствию дат

                    dr["date_time"] = valueStr;
                    dr["period"] = t;
                    dt.Rows.Add(dr);

                    CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                    CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                    if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой 
                    CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                    CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);//наращиваем расчётную дату при помощи периода интегрирования
                    attempts = 2;//в случае успешного съема получасовки восстанавливаем её попытки

                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 1; //прогресс бар - успех получения очередной записи профиля  
                        crl.Text = pb.Value.ToString();
                    }));
                    //=========записываем полученную строку в таблицу для хранения профилей=================
                    ex = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                        dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Orange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи профиля в базу: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                    }
                }  //============================================================================================================================
                catch (Exception ex2)
                {
                    //добавляем строку с ошибкой если нарвались на исключение
                    DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                    drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01");
                    dt.Rows.Add(drE);

                    currentDate = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля
                        crl.Text = pb.Value.ToString(); richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: исключение '" + ex2.Message + "'. Расчётное время периода интегрирования: " + CurFiguredDateTime.ToString() + ". Версия ПО счётчика: " + param.value + "\r");
                        richText.ScrollToCaret();
                    }));

                    CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                    CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                    if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                    CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                    CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);
                    attempts = 2;

                    continue;
                }//конец верхнего try
            }//конец самого верхнего цикла
    
            return dt;
        }

        public DataTable GetPowerProfileForCounterOnGate(string workingPort, Mercury228 gate, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent, int packnum)
        {   //процедура получения профиля мощности для выбранного счётчика Mercury230RS485
            //берём уже ранее созданную заготовку для хранения записей профиля
            DataTable dt = this.ProfileDataTable;
            if (!ReReadOnlyAbsent) dt.Clear();//если пытаемся дочитать недостающие записи, то очищать таблицу профиля нельзя

            dt.Columns[0].AllowDBNull = true;
            dt.Columns[1].AllowDBNull = true;
            dt.Columns[2].AllowDBNull = true;

            packnum += 1;
            //---получение доступа (открытие канала)
            if (this.GainAccessOnGate(gate, dp, packnum, richText, 1, 1, ref worker, 4) == false) { return dt; }
            //---нужно получить постоянную счётчика                                                 
            CounterParameterToRead param = this.ParametersToRead[11];

            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "...\r");
                richText.ScrollToCaret();
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения  
            }));

            packnum += 1;
            byte[] OutBufConst = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
            Exception ex = dp.SendData(OutBufConst, 0); //посылаем запрос
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            byte[] answerArray = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

            if (answerArray.Length == 5)
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения постоянной счётчика\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));

            param.value = this.ParseParameterValue(answerArray, param.name, 8);//разбираем и запоминаем значение постоянной счётчика 
            //---конец получения постоянной счётчика

            //читаем версию ПО счётчика, чтобы определить множитель последней записи профиля мощности            
            int multiplier = 1;//множитель адреса начальной записи профиля мощности. Зависит от версии ПО счётчика
            int MaxRecordAddressInt = 0xFFFF;
            byte MemoryNumberAnd17thBit = 0x0003;//номер памяти и значение 17го бита адреса по-умолчанию (в данном случае номер памяти 3 и значение 17го бита адреса = 0)
            DateTime CurFiguredDateTime = DateN;//расчётные дата и время. Начальное значение

            param = this.ParametersToRead[14];//нужно получить версию ПО. От неё записит нужно ли будет умножить адрес последней записи на 10h или нет     
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));
            packnum += 1;
            byte[] OutBufVer = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);  //вызываем процедуру формирования запроса версии ПО            
            ex = dp.SendData(OutBufVer, 0); //посылаем запрос    
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            answerArray = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

            if ((answerArray.Length == 5) && (answerArray[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения версии ПО\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            try
            {
                param.value = this.ParseParameterValue(answerArray, param.name, 8);//разбираем и запоминаем значение версии ПО счётчика      
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
            }
            catch
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения версии ПО\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + " \r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));

            //смотрим если версия ПО больше или равно 7.1.0 (70100 в виде числа), то устанавливаем множитель адреса последней записи профиля в 10h
            if (Convert.ToInt32(param.value) >= 70100) { multiplier = 0x0010; }

            //получаем адрес последней записи профиля  
            packnum += 1;
            byte[] OutBufLastRec = gate.FormPackage(this.FormLastProfileRecordArray(dp), 1, dp, packnum);//формируем запрос на получение адреса последней записи профиля
            ex = dp.SendData(OutBufLastRec, 0); //посылаем запрос      
            if (ex != null)
            {
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            byte[] LastRecord = dp.Read(12 + 9, 10000, true);//ждём определённое кол-ва байт ответа

            //читаем ответ. Первый байт ответа всегда - сетевой адрес
            if (LastRecord.Length == 5)
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения адреса последней записи профиля\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы

            //----------работаем с последней записью профиля--------------------------------------------------------
            //разбираем байт состояния последней записи профиля мощности
            string State_Str = "";// AnalyzeStateByte(LastRecord[3 + 8]);         

            //currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            //richText.Invoke(new Action(delegate
            //{
            //    richText.SelectionColor = Color.DarkGreen;
            //    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + State_Str + "\r");
            //    richText.ScrollToCaret();
            //}));
            //формируем строку из старшего и младшего байтов адреса последней записи
            string LastRecordAddressStr = LastRecord[1 + 8].ToString("X").PadLeft(2, '0') + LastRecord[2 + 8].ToString("X").PadLeft(2, '0');
            //формируем строку даты\времени последней записи чтобы от неё потом вести отсчёт
            string LastRecordDateTimeStr = LastRecord[6 + 8].ToString("X").PadLeft(2, '0') + "." + LastRecord[7 + 8].ToString("X").PadLeft(2, '0')
                               + "." + LastRecord[8 + 8].ToString("X").PadLeft(2, '0') + " " + LastRecord[4 + 8].ToString("X").PadLeft(2, '0')
                               + ":" + LastRecord[5 + 8].ToString("X").PadLeft(2, '0');
            int LastRecordAddressInt = Convert.ToInt32(LastRecordAddressStr, 16);//переводим строку адреса последней записи в целое число для расчёта адресов записей с искомыми датами
            LastRecordAddressInt *= multiplier;//использум множитель, который зависит от версии ПО счётчика
            DateTime LastRecordDateTime = Convert.ToDateTime(LastRecordDateTimeStr);//переводим сформированную строку даты последней записи в тип DateTime
            //------------------------------------------------------------------------------------------------------

            //------------------------здесь идёт блок формирования пакетов для опроса недостающих записей--------------     
            DataTable AbsentRecords = new DataTable("AbsentRecords");//таблица недостающих записей профиля
            DataTable AbsentRecordsList = new DataTable("AbsentRecordsList");//таблица пар дат нижняя-верхняя на основе предыдущей таблицы
            if (ReReadOnlyAbsent)//смотрим флаг переопроса недостающих записей.
            {
                currentDate = DateTime.Now;
                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Black;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Считываем только недостающие записи за указанный период\r");
                    richText.ScrollToCaret();
                }));

                int count_absent_records = this.ProfileDataTable.Rows.Count;//количество недочитанных записей профиля. По-умолчанию равно общему количеству записей профиля
                if (count_absent_records > 0)//если профиль был загружен в экземпляр счётчика
                {
                    AbsentRecords.Clear();
                    //добавляем столбцы
                    DataColumn dc = new DataColumn("date_time");
                    AbsentRecords.Columns.Add(dc);//нужны только даты
                    //идём в цикле по таблице профиля в счётчике с учётом недостающих записей
                    foreach (DataRow row in this.ProfileDataTable.Rows)
                    {
                        if (Convert.IsDBNull(row[3]))//если значение активной энергии отсутствует
                        {//когда профиль за выбранный период полный, то сюда управление не попадёт
                            DataRow dr_a = AbsentRecords.NewRow();
                            dr_a["date_time"] = row[7];
                            AbsentRecords.Rows.Add(dr_a);//значит дату нужно добавить в список недостающих записей
                            count_absent_records -= 1;//вычитаем пустую строку из количества недочитанных
                        }
                    }
                }

                //if (AbsentRecords.Rows.Count != 0)//если эта таблица пуста, то возможны два варианта: профиля вообще нет и нужно считывать с нуля, либо профиль за выбранный период полный
                if ((count_absent_records > 0) && (count_absent_records < this.ProfileDataTable.Rows.Count))//если количество недочитанных записей профиля больше 0 и меньше общего количества записей профиля, то можно составлять списки
                {   //разделяем таблицу на отдельные пакеты              
                    DataColumn dc2 = new DataColumn("dateN"); AbsentRecordsList.Columns.Add(dc2);//нижняя дата 
                    dc2 = new DataColumn("dateK"); AbsentRecordsList.Columns.Add(dc2);//верхняя дата
                                                                                      //в цикле проходим по таблице недостающих записей чтобы разбить её на пары дат нижняя-верхняя
                    DateTime dateMax = new DateTime();
                    DateTime dateMin = new DateTime();
                    int recordsObserved = 0;//количество просмотренных записей
                    for (int i = 0; i < AbsentRecords.Rows.Count - 1; i++)
                    {
                        recordsObserved += 1;//наращиваем количество просмотренных записей
                        DateTime dateN = Convert.ToDateTime(AbsentRecords.Rows[i]["date_time"]);
                        DateTime dateK = new DateTime();
                        try
                        {
                            dateK = Convert.ToDateTime(AbsentRecords.Rows[i + 1]["date_time"]);
                        }
                        catch
                        {//сюда попадёт если i + 1 записи нет
                            dateK = dateN.AddMinutes(LastRecord[9 + 8]);
                        }
                        TimeSpan ts_absent = Convert.ToDateTime(dateK).Subtract(dateN);//разница между текущей и следующей строкой
                        int MinutesElapsed_absent = (int)Math.Round(ts_absent.TotalMinutes);//количество минут между первой и второй датой
                        if (MinutesElapsed_absent == LastRecord[9 + 8])//если количество минут между первой и второй датой равно периоду интегрирования, то эти даты попадают в один пакет
                        {
                            dateMax = dateK;
                        }
                        if (dateMax < dateN) dateMax = dateN.AddMinutes(LastRecord[9 + 8]);//это условие отработает, когда максимальной дате не присвоили значение
                        //если количество минут между первой и второй датой не равно периоду интегрирования, значит делаем отсечку (начинаем следующий пакет)
                        //либо если цикл на следующей итерации завершится. 
                        if (MinutesElapsed_absent != LastRecord[9 + 8] || (AbsentRecords.Rows.Count - 1 - i) == 1)
                        {
                            dateMin = Convert.ToDateTime(AbsentRecords.Rows[i - (recordsObserved - 1)]["date_time"]);//минимальная дата для пакета
                            DataRow dr = AbsentRecordsList.NewRow(); dr["dateN"] = dateMin; dr["dateK"] = dateMax;
                            AbsentRecordsList.Rows.Add(dr);
                            recordsObserved = 0;//обнуляем количество просмотренных записей перед тем как начать следующий пакет
                        }
                    }
                }
                else
                {
                    if ((count_absent_records == this.ProfileDataTable.Rows.Count) && this.ProfileDataTable.Rows.Count > 0)
                    {//если весь загруженный профиль уже был считан ранее, то выходим
                        currentDate = DateTime.Now;
                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Orange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " За указанный период весь профиль уже был считан ранее\r");
                            richText.ScrollToCaret();
                        }));
                        return dt;
                    }
                    //если считывание профиля идёт с нуля (профиль счётчика пуст или не был загружен), то берём даты из заголовка процедуры и работаем в обычном режиме, будто бы галочка переопроса не была поставлена
                    currentDate = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Orange;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Профиль счётчика пуст или не был загружен. Читаем все записи\r");
                        richText.ScrollToCaret();
                    }));

                    DataColumn dc2 = new DataColumn("dateN"); AbsentRecordsList.Columns.Add(dc2);//нижняя дата 
                    dc2 = new DataColumn("dateK"); AbsentRecordsList.Columns.Add(dc2);//верхняя дата
                    DataRow dr = AbsentRecordsList.NewRow(); dr["dateN"] = DateN; dr["dateK"] = DateK;//присваиваем даты, котороые были поданы в процедуру
                    AbsentRecordsList.Rows.Add(dr);//в этой таблице будет только одна запись с одной парой дат
                }
            }
            else//галочка не стояла - читаем как обычно
            {  //если считывание профиля идёт с нуля, то берём даты из заголовка процедуры
                DataColumn dc2 = new DataColumn("dateN"); AbsentRecordsList.Columns.Add(dc2);//нижняя дата 
                dc2 = new DataColumn("dateK"); AbsentRecordsList.Columns.Add(dc2);//верхняя дата
                DataRow dr = AbsentRecordsList.NewRow(); dr["dateN"] = DateN; dr["dateK"] = DateK;//присваиваем даты, которые были поданы в процедуру извне
                AbsentRecordsList.Rows.Add(dr);//в этой таблице будет только одна запись с одной парой дат
            }
            //----------------------------------------------------------------------------------------------------
            //мутим цикл по списку полученных диапазонов
            foreach (DataRow row in AbsentRecordsList.Rows)//в случае, если нет задачи переопроса профиля или опрос ведётся с нуля, то этот цикл делает одну итерацию 
            {
                richText.Invoke(new Action(delegate { pb.Value = 0; }));

                DateTime row_dateN = Convert.ToDateTime(row["DateN"]).AddMinutes(-LastRecord[9 + 8]);
                DateTime row_dateK = Convert.ToDateTime(row["DateK"]);
                CurFiguredDateTime = row_dateN;//расчётные дата и время. Начальное значение

                TimeSpan ts = LastRecordDateTime.Subtract(row_dateN); //разница между датами (начальной и датой последней записи)
                int MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до даты последней записи
                int PeriodsCount = MinutesElapsed / LastRecord[9 + 8]; //количество периодов интегрирования (количество прошедших минут / период интегрирования)          
                int DateNRecordAddressInt = LastRecordAddressInt - 0x0010 * (PeriodsCount - 1); //высчитываем адрес записи с начальной датой

                if (DateNRecordAddressInt > 0xFFFF) { DateNRecordAddressInt = Convert.ToInt16(DateNRecordAddressInt.ToString("X").Substring(1, 4), 16); }//если число слишком велико (больше 2 байт), то отрезаем первую цифру

                ts = row_dateK.Subtract(row_dateN); //разница между датами (начальной и конечной)
                MinutesElapsed = (int)Math.Round(ts.TotalMinutes); //количество прошедших минут от начальной даты до конечной даты
                PeriodsCount = MinutesElapsed / LastRecord[9 + 8]; //количество периодов интегрирования от начальной даты до конечной даты                    

                string CurRecordAddressStr = DateNRecordAddressInt.ToString("X").PadLeft(4, '0');//конвертируем полученный адрес для начальной даты для последующего разбора на байты         
                                                                                                 //если число отрицательное, то видимо адресное пространство начало заполняться по кругу. Тогда убираем первые FFFF
                if (DateNRecordAddressInt < 0)
                {
                    CurRecordAddressStr = CurRecordAddressStr.Substring(4, 4);
                    DateNRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);
                }

                int s2 = PeriodsCount;
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Читаем профиль: " + row_dateN.ToString() + "---" + row_dateK.ToString() + "...\r");
                    richText.ScrollToCaret();
                    pb.Maximum = PeriodsCount;
                    crl.Text = "0"; lrl.Text = " из " + PeriodsCount.ToString();//устанавливаем значение текстовых меток для отображения прогресса считывания
                }));

                if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                int s3 = 0;
                int recordsPerPackage = 50;//количество записей, которое мы хотим передать в одном пакете       
                {
                    int iters = 1;//кол-во итераций отправки запросов на записи профиля. По-умолчанию одна итерация
                    if (PeriodsCount > recordsPerPackage)
                    {
                        iters = PeriodsCount / recordsPerPackage;
                        if (PeriodsCount % recordsPerPackage > 0) iters += 1; //т.к. отправляем по 50 записей, то если от деления есть остаток - накидываем ещё одну итерацию
                    }

                    int CurRecordAddressInt = 0;
                    int attempts = 2;//кол-во попыток съема получасовки (проблема 17го бита)
                    byte t = 30;//период интегрирования (по-умолчанию)
                    for (int s = 1; s <= iters; s++)//кол-во итерации отправки порций записи профиля. Разбито на порции для того чтобы не перегружать буффер шлюза
                    {
                        if (attempts == 0)//если попыток съема получасвки не осталось из-за того что попалась пустышка (для версии По больше 70100), то идём на следующую получасовку
                        {
                            t = 30;//возвращаем период интегрирования
                            DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                            drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01");
                            dt.Rows.Add(drE);

                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                            richText.Invoke(new Action(delegate
                            {
                                s2 -= 1;
                                iters += 1;
                                pb.Value += 1; //прогресс бар - ошибка получения очередной записи профи ля т.к. там пустышка по этому адресу
                                crl.Text = pb.Value.ToString();
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: не осталось попыток для считывания текущего расчётного времени. Расчётное время: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Версия ПО счётчика: " + param.value + ". " + State_Str + "\r");
                                richText.ScrollToCaret();
                            }));

                            CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                            CurRecordAddressInt += 0x0010;//перепрыгиваем пустую получасовку
                            if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой 
                            CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                            CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);//наращиваем расчётную дату при помощи периода интегрирования  
                            attempts = 2;
                            continue;
                        }

                        if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                        Thread.Sleep(100);//ждём чтобы шлюз переварил информацию перед следующей порцией получасовок
                        int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
                        List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов) 
                        if (s2 >= recordsPerPackage) s3 = recordsPerPackage; else s3 = s2;//готовим не больше 50 записей за итерацию и меньше 50 если осталось меньше 50 из общего количества которое мы хотим считать
                        for (int i = 1; i <= s3; i++)
                        {
                            packnum += 1;//наращиваем номер пакета
                            byte RecordAddessHi = Convert.ToByte(CurRecordAddressStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущей записи
                            byte RecordAddessLow = Convert.ToByte(CurRecordAddressStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущей записи
                            byte[] Package = gate.FormPackage(this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, MemoryNumberAnd17thBit, 0x000F), 1, dp, packnum);//формируем запрос на получение профиля
                            PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                            CountToWait += 18 + 9;//наращиваем общее кол-во байт ответа (кол-во полезной нагрузки ответа + заголовок + хвост пакета
                            CurRecordAddressInt = Convert.ToInt32(CurRecordAddressStr, 16);//переводим полученную строку в целое число
                            CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                            if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                            CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора
                        }
                        //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                        //составляем его из окна пакетов       
                        byte[] OutBufPowerProf = new byte[0];
                        foreach (byte[] pack in PackagesBuffer)
                        {
                            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                            Array.Resize(ref OutBufPowerProf, OutBufPowerProf.Length + pack.Length);//корректируем размер итогового массива исходя из длины очередного пакета
                            Array.Copy(pack, 0, OutBufPowerProf, OutBufPowerProf.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                        }
                        //--------------------------------------------------------------------------------------------------------------------------------------------------
                        if (PackagesBuffer.Count == 0) { continue; }//когда всё нормально (есть сформированные запросы), это условие не должно удовлетворяться. НОВАЯ СТРОКА
                        //--------------------------------------------------------------------------------------------------------------------------------------------------
                        ex = dp.SendData(OutBufPowerProf, 0); //посылаем пакет запросов
                        if (ex != null)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
                                richText.ScrollToCaret();
                            }));
                            return dt;
                        }
                        if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                        answerArray = dp.Read(CountToWait, 3000, true);//ждём определённое кол-ва байт ответа               
                        if (answerArray.Length == 5)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: короткий ответ (5 байт). Версия ПО счётчика: " + param.value + "\r");
                                richText.ScrollToCaret();
                            }));
                            return dt;
                        }
                        //если нормально то продолжаем
                        int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета)                    
                        for (int i = 0; i < s3; i++)
                        {
                            try
                            {
                                if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                                byte[] ProfileRecord = new byte[18];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                                Array.Copy(answerArray, offset, ProfileRecord, 0, 18);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа
                                if (ProfileRecord.Length == 5)//если ответ слишком короткий (данных нет)
                                {
                                    DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                                    drE["e_r_plus"] = 0; drE["e_a_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                                    dt.Rows.Add(drE);

                                    currentDate = DateTime.Now;
                                    richText.Invoke(new Action(delegate
                                    {
                                        s2 -= 1;
                                        pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля
                                        crl.Text = pb.Value.ToString();//численное отображение прогресса
                                        richText.SelectionColor = Color.Red;
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: короткий ответ (5 байт). Предположительно нет данных на следующий период интегрирования: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Версия ПО счётчика: " + param.value + "\r");
                                        richText.ScrollToCaret();
                                    }));

                                    CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                                    CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора 
                                    if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                                    CurFiguredDateTime = CurFiguredDateTime.AddMinutes(LastRecord[9 + 8]);//наращиваем расчётные дату и время
                                    attempts = 2;

                                    continue;
                                }
                                //если данные есть (не 5 байт), то продолжаем
                                State_Str = AnalyzeStateByte(ProfileRecord[1]);//анализируем байт состояния для каждой считанной записи профиля мощности

                                DataRow dr = dt.NewRow(); //добавляем строку в таблицу
                                t = ProfileRecord[7];//длительность периода интегрирования - нужна при расчёте
                                if (t == 0) t = 1;//чтобы не делить на 0 при расчёте
                                int a = Convert.ToInt16(this.CounterConst);//постоянная сч-ка - нужна при расчёте                                         
                                                                              //разбираем полученную запись
                                string valueStr = ProfileRecord[9].ToString("X").PadLeft(2, '0') + ProfileRecord[8].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (A+)
                                if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                                double N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                                double value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                                dr["e_a_plus"] = value / 2;//заносим значение в строку

                                valueStr = ProfileRecord[11].ToString("X").PadLeft(2, '0') + ProfileRecord[10].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (A-)
                                if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                                N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                                value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                                dr["e_a_minus"] = value / 2;//заносим значение в строку

                                valueStr = ProfileRecord[13].ToString("X").PadLeft(2, '0') + ProfileRecord[12].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (R+)
                                if (valueStr == "FFFF") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                                N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                                value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                                dr["e_r_plus"] = value / 2;//заносим значение в строку

                                valueStr = ProfileRecord[15].ToString("X").PadLeft(2, '0') + ProfileRecord[14].ToString("X").PadLeft(2, '0');//формируем 16-ричную строку числа (R-)
                                if (valueStr == "FFFF") { valueStr = "0"; }//если получили такие байты, это значит что данных нет
                                N = Convert.ToInt64(valueStr, 16);//конвертируем строку в число                                                                                          
                                value = (N * (60 / t)) / (2 * a);//расчёт по формуле
                                dr["e_r_minus"] = value / 2;//заносим значение в строку
                                                            //собираем дату
                                valueStr = ProfileRecord[4].ToString("X").PadLeft(2, '0') + "." + ProfileRecord[5].ToString("X").PadLeft(2, '0')
                                           + "." + ProfileRecord[6].ToString("X") + " " + ProfileRecord[2].ToString("X").PadLeft(2, '0')
                                           + ":" + ProfileRecord[3].ToString("X").PadLeft(2, '0');

                                if (valueStr == "00.00.0 00:00") //если получили такую строку, то это значит, что информации за желаемый период в ответе нет (данные есть, но они 0).
                                {
                                    //подкидываем заведомо ложную дату чтобы перейти на блок исправления 17го бита и избежать исключения при конвертации (которое должно вызываться при настоящей ошибке или некорректных данных)
                                    valueStr = "01.01.2000 00:30";
                                }
                                //если расчётная дата и фактическая по расчитанному адресу памяти не совпадают
                                if (Convert.ToDateTime(valueStr).AddMinutes(-t) != CurFiguredDateTime)
                                {
                                    //здесь идёт проверка на правильность 17го бита адреса памяти для версии ПО старше 7.1.0. СПОСОБА РАСЧЁТА ЗАРАНЕЕ ПОКА НЕ НАШЁЛ
                                    if (Convert.ToInt32(param.value) >= 70100)
                                    {
                                        attempts -= 1;//уменьшаем кол-во попыток съема получасовки т.к. дата не соответствует расчётной                                                                                          
                                        if (MemoryNumberAnd17thBit == 0x0003)
                                        {
                                            {
                                                iters += 1;//добавляем итерацию
                                                MemoryNumberAnd17thBit = 0x00083;
                                                CurRecordAddressInt -= 0x0010 * (s3 - i);//откатываем текущий адрес назад для переопрсоа
                                                if (CurRecordAddressInt < 0) CurRecordAddressInt += 16;
                                                CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим адрес в строку для очередного разбора 
                                                break;//вышли из цикла разбора для новой попытки считывания
                                            }
                                        }

                                        if (MemoryNumberAnd17thBit == 0x0083)
                                        {
                                            {
                                                iters += 1;//добавляем итерацию
                                                MemoryNumberAnd17thBit = 0x00003;
                                                CurRecordAddressInt -= 0x0010 * (s3 - i);//откатываем текущий адрес назад для переопрсоа
                                                if (CurRecordAddressInt < 0) CurRecordAddressInt += 16;
                                                CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим адрес в строку для очередного разбора 
                                                break;//вышли из цикла разбора для новой попытки считывания
                                            }
                                        }
                                    }
                                    else
                                    {
                                        t = 30;//возвращаем период интегрирования
                                        currentDate = DateTime.Now;
                                        if (valueStr == "01.01.2000 00:30") { valueStr = "нет данных"; } //если мы ранее подкинули левую дату, то по этому ветвлению пишем "нет данных"
                                        richText.Invoke(new Action(delegate
                                        {
                                            s2 -= 1;
                                            pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля т.к. там пустышка по этому адресу
                                            crl.Text = pb.Value.ToString();

                                            richText.SelectionColor = Color.Red;
                                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: предположительно нет данных на следующий период интегрирования либо время в счётчике не совпадает с расчётным: " + CurFiguredDateTime.AddMinutes(t).ToString() + ". Фактическая метка времени по этому адресу памяти: " + valueStr + ". Версия ПО счётчика: " + param.value + ". " + State_Str + "\r");
                                            richText.ScrollToCaret();
                                        }));

                                        if (valueStr != "нет данных")//если данные были (не 5 байт) и не нули (не 00.00.00)
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.Black;
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Пытаемся записать в базу...\r");
                                                richText.ScrollToCaret();
                                            }));

                                            dr["date_time"] = valueStr;
                                            dr["period"] = t;
                                            dt.Rows.Add(dr);

                                            Exception ex20 = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                                            dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);

                                            if (ex20 != null)
                                            {
                                                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                                richText.Invoke(new Action(delegate
                                                {
                                                    richText.SelectionColor = Color.Red;
                                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи профиля в базу: " + ex20.Message + "\r");
                                                    richText.ScrollToCaret();
                                                }));
                                            }
                                        }
                                        offset += 8 + 18 + 1;//наращиваем отступ вглубь массива ответа
                                        CurFiguredDateTime = CurFiguredDateTime.AddMinutes(t);

                                        continue;//продолжаем цикл разбора
                                    }//конец ветки по версии меньше 70100
                                }//конец ветки по несоответствию дат

                                dr["date_time"] = valueStr;
                                dr["period"] = t;
                                dt.Rows.Add(dr);
                                offset += 8 + 18 + 1;//наращиваем отступ вглубь массива ответа

                                richText.Invoke(new Action(delegate
                                {
                                    s2 -= 1;
                                    pb.Value += 1; //прогресс бар - успех получения очередной записи профиля 
                                    crl.Text = pb.Value.ToString();//численное отображение прогресса 
                                }));
                                //=========записываем полученную строку в таблицу для хранения профилей=====================
                                Exception ex2 = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                                    dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);
                                if (ex2 != null)
                                {
                                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.SelectionColor = Color.Orange;
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи профиля в базу: " + ex2.Message + "\r");
                                        richText.ScrollToCaret();
                                    }));
                                }
                            }
                            catch (Exception ex2)
                            {
                                currentDate = DateTime.Now;
                                //добавляем строку с ошибкой если нарвались на исключение
                                DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0; drE["period"] = 0;
                                drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                                dt.Rows.Add(drE);

                                richText.Invoke(new Action(delegate
                                {
                                    s2 -= 1;
                                    pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля
                                    crl.Text = pb.Value.ToString();//численное отображение прогресса
                                    richText.SelectionColor = Color.Red;
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка чтения профиля: исключение '" + ex2.Message + "'. Расчётное время периода интегрирования: " + CurFiguredDateTime.ToString() + ". Версия ПО счётчика: " + param.value + "\r");
                                    richText.ScrollToCaret();
                                }));

                                CurRecordAddressInt += 0x0010; //наращиваем адрес для того чтобы вытащить следующую получасовку
                                CurRecordAddressStr = CurRecordAddressInt.ToString("X").PadLeft(4, '0'); //переводим новый адрес в строку для очередного разбора 
                                if (CurRecordAddressInt > MaxRecordAddressInt) { CurRecordAddressInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
                                CurFiguredDateTime = CurFiguredDateTime.AddMinutes(LastRecord[9 + 8]);//наращиваем расчётные дату и время
                                offset += 8 + 18 + 1;//наращиваем отступ вглубь массива ответа
                                attempts = 2;

                                continue;//продолжаем цикл разбора ответа
                            }
                            CurFiguredDateTime = CurFiguredDateTime.AddMinutes(LastRecord[9 + 8]);//наращиваем расчётные дату и время
                            attempts = 2;
                        }//конец цикла разбора                                     
                    }//конец итерации считывания
                }
            }//конец цикла по пакетам с недостающими записями 
            return dt;
        }

        private string AnalyzeStateByte(byte pbyte)
        {//здесь анализируем байт состояния записи профиля мощности
            string Byte_Str = Convert.ToString(pbyte, 2).PadLeft(8, '0');//переводим байт в строковое представление двоичной формы числа
            string State_Str = "";
            switch (Byte_Str[3])
            {
                case '0': State_Str += " Признак профиля: основной. "; break;
                case '1': State_Str += " Признак профиля: дополнительный. "; break;
            }
            switch (Byte_Str[4])
            {
                case '0': State_Str += " Признак сезонного времени: лето. "; break;
                case '1': State_Str += " Признак сезонного времени: зима. "; break;
            }
            switch (Byte_Str[5])
            {
                case '0': State_Str += " Флаг выполнения инициализации памяти: нет. "; break;
                case '1': State_Str += " Флаг выполнения инициализации памяти: да. "; break;
            }
            switch (Byte_Str[6])
            {
                case '0': State_Str += " Флаг неполного среза: нет. "; break;
                case '1': State_Str += " Флаг неполного среза: да. "; break;
            }
            switch (Byte_Str[7])
            {
                case '0': State_Str += " Флаг переполнения массива срезов: нет. "; break;
                case '1': State_Str += " Флаг переполнения массива срезов: да. "; break;
            }
            return State_Str;
        }
    }
}
