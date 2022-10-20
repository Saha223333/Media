using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Data.SqlClient;
using System.Threading;

namespace NewProject
{
    public class MicronSET: IDevice, ICounter, IReadable, IWritable
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
        //public int ParentDevice { get; set; } //временное пооле пока пользуемся инстаровской базой
        public string SerialNumber { get; set; }
        public DataTable ProfileDataTable { get; set; } //таблица, хранящая снятый профиль
        public int Divider { get; set; }//хранит значение делителя для вычисления энергии (1000 или с учётом постоянной счётчика)
        public int TransformationRate { get; set; }//коэффициент трансформации (берётся из БД)

        public BindingList<CounterEnergyToRead> EnergyToRead { get; set; } //перечень энергии для опроса счётчиков с цифровым интерфейсом       
        public BindingList<CounterParameterToRead> ParametersToRead { get; set; } //перечень параметров для опроса счётчиков с цифровым интерфейсом      
        public BindingList<CounterMonitorParameterToRead> MonitorToRead { get; set; } //перечень параметров тока (монитор) для счётчиков с цифровым интерфейсом      
        public BindingList<CounterJournalToRead> JournalToRead { get; set; } //перечень журнала для счётчиков с цифровым интерфейсом
        public BindingList<CounterParameterToWrite> ParametersToWrite { get; set; } //перечень параметров для записи в счётчик с цифровым интерфейсом     
        public BindingList<CounterJournalCQCToRead> JournalCQCToRead { get; set; } //перечень журнала ПКЭ для счётчиков с цифровым интерфейсом
        private TreeNode tn;

        public MicronSET(int pid, int pparentid, string pname, byte pnetadr, bool ppowerprofile, string psernum, int ptransformationrate, TreeNode ptn)
        {
            this.tn = ptn;//нужен узел дерева, который является носителем этого объекта
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
            JournalCQCToRead = new BindingList<CounterJournalCQCToRead>();
            ParametersToWrite = new BindingList<CounterParameterToWrite>();

            ID = pid; Name = pname; PowerProfile = ppowerprofile;
            ParentID = pparentid; NetAddress = pnetadr; SerialNumber = psernum;
            TransformationRate = ptransformationrate;
            //заполняем список параметров
            //запрос на серийный номер
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0000;
                ParametersToRead.Add(new CounterParameterToRead("Серийный номер и дата выпуска", b, 10, false));
            }
            //запрос на чтение текущего времени
            {
                byte[] b = new byte[2];
                b[0] = 0x0004;//запрос на чтение массива времён
                b[1] = 0x0000;//параметр чтения текущего времени
                ParametersToRead.Add(new CounterParameterToRead("Дата и время", b, 11, true));
            }       
            //запрос на коэффициенты трансформации
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0002;
                ParametersToRead.Add(new CounterParameterToRead("Коэфф. тр-ции по напряжению", b, 13, false));
            }
            //в ответе эти параметры идут вместе, но здесь они будут читаться и выводиться отдельно
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0002;
                ParametersToRead.Add(new CounterParameterToRead("Коэфф. тр-ции по току", b, 13, false));
            }
            //запросы на вариант исполнения. Посылается всегда один запрос, но в ответе анализируются разные байты
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Класс точности А+", b, 6, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Номинальный ток", b, 6, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Номинальное напряжение", b, 6, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Профиль мощности", b, 6, false)); //да или нет
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Вариант исполнения", b, 6, false));
            }
           
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Число фаз", b, 6, false));
            }
           
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Число направлений", b, 6, false));
            }
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Постоянная сч-ка", b, 6, false));
            }
           
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0001;
                ParametersToRead.Add(new CounterParameterToRead("Температура", b, 5, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Температурный диапазон", b, 6, false));
            }
          
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0003;
                ParametersToRead.Add(new CounterParameterToRead("Версия ПО", b, 6, false));
            }          
            
            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0006;
                ParametersToRead.Add(new CounterParameterToRead("Длительность периода интегрирования профиля 1", b, 5, false));
            }

            {
                byte[] b = new byte[2];
                b[0] = 0x0008; b[1] = 0x0012;
                ParametersToRead.Add(new CounterParameterToRead("Тип счётчика", b, 6, false));
            }
            //журнал
            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0001; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выключения\\включения прибора", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0002; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время до\\после коррекции часов", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0007; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выключения\\включения фазы 1", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0008; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выключения\\включения фазы 2", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0009; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выключения\\включения фазы 3", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x000A; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время вскрытия\\закрытия прибора", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0004; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время коррекции тарифного расписания", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0003; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время коррекции расписания праздничных дней", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0006; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время инициализации массива профиля мощности (1-го или един-ственного)", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x003D; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время инициализации массива профиля мощности (2-го)", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x003F; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время коррекции расписания утренних и вечерних максимумов мощности", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0005; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время сброса показаний", b, 10, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x003B; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выхода\\возврата среднего значения Р+ за установленный порог", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0042; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выхода\\возврата среднего значения Р- за установленный порог", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0043; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выхода\\возврата среднего значения Q+ за установленный порог", b, 17, false));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0009; b[1] = 0x0044; b[2] = 0x00FF;
                JournalToRead.Add(new CounterJournalToRead("Время выхода\\возврата среднего значения Q- за установленный порог", b, 17, false));
            }
            //параметры тока (монитор, векторная диаграмма)
            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0000;//байт BWRI = 0000|00|00 мощность P по сумме и фазам
                //Для удобства можно сначала анализировать какой запрос будет формироваться в двоичном виде а подавать массив сразу в шестнадцатеричном виде
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
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0010;//байт BWRI = 1 00 00
                MonitorToRead.Add(new CounterMonitorParameterToRead("Напряжение", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x00A0;//байт BWRI = 1010 0000
                MonitorToRead.Add(new CounterMonitorParameterToRead("Напряжение, усреднённое", b, 15, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0016; b[2] = 0x0020;//байт BWRI = 10 00 00
                MonitorToRead.Add(new CounterMonitorParameterToRead("Ток", b, 15, true));
            }


            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0011; b[2] = 0x0040;//байт BWRI = 0100 0000
                MonitorToRead.Add(new CounterMonitorParameterToRead("Частота", b, 6, true));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0008; b[1] = 0x0011; b[2] = 0x0090;//байт BWRI = 1001 0000
                MonitorToRead.Add(new CounterMonitorParameterToRead("Частота, усреднённая", b, 6, true));
            }

            ////энергия
            {
                //вытаскиваем последние значения энергии из базы для этого вида энергии
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0000; b[2] = 0x0000;

                EnergyToRead.Add(new CounterEnergyToRead("Энергия от сброса", b, 19, true, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0010; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за текущий год", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0020; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за предыдущий год", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0040; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за текущие сутки", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0050; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за предыдущие сутки", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }
            //-------------------------------энрегия за месяц
            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0031; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за январь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0032; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за февраль", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0033; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за март", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0034; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за апрель", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0035; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за май", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0036; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за июнь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0037; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за июль", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0038; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за август", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x0039; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за сентябрь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003A; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за октябрь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003B; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за ноябрь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[3];
                b[0] = 0x0005; b[1] = 0x003C; b[2] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия за декабрь", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }
            //-----------------------энергия на начало периода
            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A; b[1] = 0x0081; b[2] = 0x0000; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало текущего года", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A; b[1] = 0x0082; b[2] = 0x0000; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало предыдущего года", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A; b[1] = 0x0084; b[2] = 0x0000; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало текущих суток", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A; b[1] = 0x0085; b[2] = 0x0000; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало предыдущих суток", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }
            //--------------------Энергия на начало месяца---------------------------------
            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A;    b[1] = 0x0083;    b[2] = 0x0001;     b[3] = 0x0000;     b[4] = 0x000F;         b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало января", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                //номер запроса   //номер массива   //номер месяца     //номер тарифа    //маска данных ответа //формат данных ответа
                b[0] = 0x000A;    b[1] = 0x0083;     b[2] = 0x0002;     b[3] = 0x0000;    b[4] = 0x000F;       b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало февраля", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0003; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало марта", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0004; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало апреля", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0005; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало мая", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0006; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало июня", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0007; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало июля", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0008; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало августа", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x0009; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало сентября", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x000A; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало октября", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x000B; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало ноября", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

            {
                byte[] b = new byte[6];
                b[0] = 0x000A; b[1] = 0x0083; b[2] = 0x000C; b[3] = 0x0000; b[4] = 0x000F; b[5] = 0x0000;
                EnergyToRead.Add(new CounterEnergyToRead("Энергия на начало декабря", b, 19, false, 0, 0,
                                                                                          0, 0,
                                                                                          0, ""));
            }

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
                ParametersToWrite.Add(new CounterParameterToWrite("Коррекция времени в пределах 2 мин", b, false, newValue));
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

        public string ParseParameterValue(byte[] array, string pname, int offset)
        {//процедура разбора ответа и приведения в читаемый вид. Возвращает строку
            switch (pname)
            {
                case "Тип счётчика":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[3 + offset], 2); //переводим третий байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(0, 4)) //смотрим что там в первых четырёх битах
                        {
                            case "0000": { b = "СЭТ-4ТМ.02 / СЭТ-1М.01"; } break;
                            case "0001": { b = "СЭТ-4ТМ.03"; } break;
                            case "1000": { b = "СЭТ-4ТМ.02М / СЭТ-4ТМ.03М"; } break;
                        }
                        return b;
                    }

                case "Дата и время":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + ".20" + array[7 + offset].ToString("X").PadLeft(2, '0') + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Версия ПО":
                    {
                        string valueStr = "";
                        valueStr = array[1 + offset].ToString("X") + array[2 + offset].ToString("X").PadLeft(2, '0') + array[3 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Серийный номер и дата выпуска":
                    {
                        string valueStr = "";
                        //формируем 16-ричную строку серийного номера из имеющихся байтов
                        valueStr = array[1 + offset].ToString("X").PadLeft(2, '0') + array[2 + offset].ToString("X").PadLeft(2, '0')
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + array[4 + offset].ToString("X").PadLeft(2, '0');
                        double valueDbl = Convert.ToInt64(valueStr, 16);//переводим 16-ричную строку серийного номера в число
                        valueStr = "";//обнуляем строку для конечного формирования
                        valueStr += valueDbl.ToString() + " Дата выпуска: "
                            + array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + ".20" + array[7 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Коэфф. тр-ции по напряжению":
                    {
                        string valueStr = "";
                        valueStr = array[1 + offset].ToString() + array[2 + offset].ToString();
                        return valueStr;
                    }

                case "Коэфф. тр-ции по току":
                    {
                        string valueStr = "";
                        valueStr = array[3 + offset].ToString() + array[4 + offset].ToString();
                        return valueStr;
                    }

                case "Длительность периода интегрирования профиля 1":
                    {
                        string valueStr = "";
                        valueStr = array[2 + offset].ToString();
                        return valueStr;
                    }

                case "Класс точности А+":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

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
                        string valueStr = "";
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

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
                        string valueStr = "";
                        valueStr = Convert.ToString(array[1 + offset], 2); //переводим первый байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(4, 2)) //смотрим что там в двух битах
                        {
                            case "00": { b = "57,7В"; } break;
                            case "01": { b = "230В"; } break;
                        }
                        return b;
                    }

                case "Профиль мощности":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим второй байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(2, 1)) //смотрим что там в бите
                        {
                            case "0": { b = "Нет"; } break;
                            case "1": { b = "Да"; } break;
                        }
                        return b;
                    }                    

                case "Постоянная сч-ка":
                    {
                        string valueStr = String.Empty;
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим второй байт ответа в двоичную строку для анализа
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
                            this.Divider = CounterConst * 2;//после получения постоянной счётчика помещаем её в поле делителя счётчика для расчёта энергии
                        }
                        catch
                        {
                            b = "Ошибка";
                        }
                        
                        return b;
                    }

                case "Число фаз":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим второй байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(2, 2)) //смотрим что там в двух битах
                        {
                            case "00": { b = "3"; } break;
                            case "01": { b = "1"; } break;
                        }
                        return b;
                    }

                case "Число направлений":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим второй байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(0, 1)) //смотрим что там в  бите                        
                        {
                            case "0": { b = "2"; } break;
                            case "1": { b = "1"; } break;
                        }
                        return b;
                    }

                case "Температура":
                    {
                        string valueStr = "";
                        valueStr = array[2 + offset].ToString().PadLeft(2, '0'); //переводим второй байт ответа в двоичную строку для анализа
                        return valueStr;
                    }

                case "Температурный диапазон":
                    {
                        string valueStr = "";
                        valueStr = Convert.ToString(array[2 + offset], 2); //переводим второй байт ответа в двоичную строку для анализа
                        string a = valueStr.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                        string b = ""; //значение параметра

                        switch (a.Substring(1, 1)) //смотрим что там в бите
                        {
                            case "0": { b = "20 С"; } break;
                            case "1": { b = "40 С"; } break;
                        }
                        return b;
                    }
                //журнал
                case "Время выключения\\включения прибора":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время до\\после коррекции часов":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                             + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                             + "\\"
                             + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                             + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выключения\\включения фазы 1":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выключения\\включения фазы 2":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                             + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                             + "\\"
                             + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                             + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выключения\\включения фазы 3":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                              + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                              + "\\"
                              + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                              + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время коррекции тарифного расписания":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время коррекции расписания праздничных дней":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время вскрытия\\закрытия прибора":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время инициализации массива профиля мощности (1-го или един-ственного)":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время инициализации массива профиля мощности (2-го)":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время коррекции расписания утренних и вечерних максимумов мощности":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время сброса показаний":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0')
                            + "." + array[7 + offset].ToString("X") + " " + array[3 + offset].ToString("X").PadLeft(2, '0')
                            + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выхода\\возврата среднего значения Р+ за установленный порог":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                            + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                            + "\\"
                            + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                            + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выхода\\возврата среднего значения Р- за установленный порог":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                             + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                             + "\\"
                             + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                             + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выхода\\возврата среднего значения Q+ за установленный порог":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                             + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                             + "\\"
                             + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                             + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }

                case "Время выхода\\возврата среднего значения Q- за установленный порог":
                    {
                        string valueStr = "";
                        valueStr = array[5 + offset].ToString("X").PadLeft(2, '0') + "." + array[6 + offset].ToString("X").PadLeft(2, '0') + "." + array[7 + offset].ToString("X").PadLeft(2, '0') + " "
                             + array[3 + offset].ToString("X").PadLeft(2, '0') + ":" + array[2 + offset].ToString("X").PadLeft(2, '0') + ":" + array[1 + offset].ToString("X").PadLeft(2, '0')
                             + "\\"
                             + array[12 + offset].ToString("X").PadLeft(2, '0') + "." + array[13 + offset].ToString("X").PadLeft(2, '0') + "." + array[14 + offset].ToString("X").PadLeft(2, '0')
                             + " " + array[10 + offset].ToString("X").PadLeft(2, '0') + ":" + array[9 + offset].ToString("X").PadLeft(2, '0') + ":" + array[8 + offset].ToString("X").PadLeft(2, '0');
                        return valueStr;
                    }
            }
            return "";
        }

        public double ParseEnergyValue(DataProcessing dp, byte[] array, int offset)
        {//процедура чтения данных, возвращающая энергию
            try
            {
                string valueStr = "";
                valueStr = array[1 + offset].ToString("X").PadLeft(2, '0') + array[2 + offset].ToString("X").PadLeft(2, '0')
                         + array[3 + offset].ToString("X").PadLeft(2, '0') + array[4 + offset].ToString("X").PadLeft(2, '0');
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
                            string valueStr = "";//ДОБАВИТЬ НАПРАВЛЕНИЕ ЭНЕРГИИ???
                            valueStr = array[(1 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0') +
                                      array[(2 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0')
                                     + array[(3 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 1000;
                            return valueDbl;
                        }

                    case "Мощность S":
                        {
                            string valueStr = "";
                            valueStr = array[1 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 1000;
                            return valueDbl;
                        }

                    case "Мощность Q":
                        {
                            string valueStr = "";
                            valueStr = array[1 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 1000;
                            return valueDbl;
                        }

                    case "Напряжение":
                        {
                            string valueStr = "";
                            valueStr = array[(1 + phaseNo * 3) - 3 + offset].ToString("X".PadLeft(2, '0'))
                                     + array[(2 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[(3 + phaseNo * 3) - 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Коэффициент мощности":
                        {
                            string valueStr = "";
                            valueStr = array[1 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 1000;
                            return valueDbl;
                        }

                    case "Ток":
                        {
                            string valueStr = "";
                            valueStr = array[(1 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0')
                                     + array[(2 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0')
                                     + array[(3 + phaseNo * 3) + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 10000;
                            return valueDbl;
                        }

                    case "Частота":
                        {
                            string valueStr = "";
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Частота, усреднённая":
                        {
                            string valueStr = "";
                            valueStr = array[1 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + offset].ToString("X").PadLeft(2, '0');

                            double valueDbl = Convert.ToInt64(valueStr, 16);
                            valueDbl = valueDbl / 100;
                            return valueDbl;
                        }

                    case "Напряжение, усреднённое":
                        {
                            string valueStr = "";
                            valueStr = array[1 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[2 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0')
                                     + array[3 + phaseNo * 3 + offset].ToString("X").PadLeft(2, '0');

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
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тариф " + (i).ToString() + " удачно: " + value.ToString() + "\r");
                        richText.ScrollToCaret();
                    }));
                }
                //после того, как прошлись по тарифам, нужно сохранить значения энергии в базу
                energy.saveToDataBase(this.SerialNumber, richText);
            }//конец цикла по энергии
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
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись " + (i + 1).ToString() + " удачно\r");
                        richText.ScrollToCaret();
                    }));
                }
            }
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
            //Thread.Sleep(100);
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
            //Thread.Sleep(100);
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
                    //Thread.Sleep(100);
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

                    //пробуем: если время между часами счётчика и часами компа расходятся больше чем на 2 минуты в абсолютном выражении, то либо добавляем к часам счётчика 1 минуту, либо отнимаем от часов счётчика 1 минуту
                    //Почему так, а не просто попытка прошивки времени с компа:
                    //потому что команда коррекции в пределах 2 минут может быть выполнена один раз в сутки и этот процесс может быть итеративным изо дня в день, постепенно приближать часы счётчика к нужному времени.
                    if (Math.Abs(delta_minutes) > 2)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Orange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Разница во времени составляет " + Math.Abs(delta_minutes).ToString() + " минут с учётом сдвига часов " + hours_shift.ToString()  + ". Пытаюсь провести коррекцию часов счётчика в пределах 2 минут... " + "\r");
                            richText.ScrollToCaret();
                        }));

                        if (delta_minutes < 0)//если разница во времени отрицательная, значит время на счётчике больше и его надо уменьшить
                        {
                            dt = dt.AddMinutes(-1);//корректируем часы счётчика на 1 минуту назад чтобы приблизить ко времени на компе. 
                        }
                        else
                        {//если разница во времени положительная, значит время на счётчике меньше и его надо увеличить
                            dt = dt.AddMinutes(1);//корректируем часы счётчика на 1 минуту вперёд чтобы приблизить ко времени на компе. 
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
        {//в счётчиках типа Меркурий 485 и СЭТ первый байт ответа - всегда сетевой номер, поэтому здесь достаточно проверить первый байт ответа на соответствие сетевому номеру счётчика (для любой команды)
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
            //очищаем буферы порта                                                
            System.Threading.Thread.Sleep(100);
            return testOutBuf;
        }

        public byte[] FormGainAccessArray(DataProcessing dp, byte lvl, byte pwd)
        {//процедура формирования массива открытия канала
            byte[] OutBuf = new byte[8];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0001; //запрос на открытие канала
            OutBuf[2] = 0x0030; //пароль по-умолчанию
            OutBuf[3] = 0x0030;
            OutBuf[4] = 0x0030;
            OutBuf[5] = 0x0030;
            OutBuf[6] = 0x0030;
            OutBuf[7] = 0x0030;
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, 10);
            OutBuf[8] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[9] = Convert.ToByte(crc / 256);                                             
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormParameterArray(DataProcessing dp, CounterParameterToRead param)
        {//процедура, формирующая массив для чтения параметра согласно правилам формирования запросов в данном типе счётчиков
            byte[] OutBuf = new byte[1];
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, param.bytes.Length + 1);
            for (int i = 1; i <= param.bytes.Length; i++) { OutBuf[i] = param.bytes[i - 1]; } //формируем пакет для счётчика.                                                                    
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
            OutBuf[3] = Convert.ToByte(recNo); //корректируем номер записи журнала по счётчику цикла
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
            OutBuf[0] = Convert.ToByte(this.NetAddress);

            Array.Resize(ref OutBuf, energy.bytes.Length + 1);
            for (int j = 1; j <= energy.bytes.Length; j++) { OutBuf[j] = energy.bytes[j - 1]; } //формируем пакет           
            if (energy.bytes[0] == 0x0000A) OutBuf[4] = Convert.ToByte(zoneNo);//если считываем энергию на начало месяца            
            if (energy.bytes[0] == 0x00005) OutBuf[3] = Convert.ToByte(zoneNo);//если энергия нарастающим итогом
                                  
            UInt16 crc = dp.ComputeCrc(OutBuf); //контрольная сумма
            Array.Resize(ref OutBuf, OutBuf.Length + 2); //расширяем массив для добавления контрольной суммы
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //младший байт контрольной суммы
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256); //старший байт контрольной суммы 
                                                          
            System.Threading.Thread.Sleep(100);
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
        {   //процедура формирующая массив для получения текущей записи профиля
            byte[] OutBuf = new byte[3];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0008; //запрос
            OutBuf[2] = 0x0004;
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, OutBuf.Length + 2);
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256);                                          
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormSearchHeaderStateArray(DataProcessing dp)
        {   //процедура формирующая массив для чтения слова состояния задачи поиска адреса заголовка базового массива профиля
            byte[] OutBuf = new byte[4];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0008; //запрос
            OutBuf[2] = 0x0018; //код параметра
            OutBuf[3] = 0x0000; //задача поиска адреса заголовка базового массива профиля
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, OutBuf.Length + 2);
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256);                                   
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormHeaderProfileArray(DataProcessing dp, byte hh, byte dd, byte mm, int yy, byte it)
        {   //процедура формирующая массив для поиска адреса зоголовка профиля
            byte[] OutBuf = new byte[12];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0003; //запрос
            OutBuf[2] = 0x0028; //код параметра
            OutBuf[3] = 0x0000; //номер массива
            OutBuf[4] = 0x00FF; //старший адрес начала поиска (смотрим всё подряд)
            OutBuf[5] = 0x00FF; //младший адрес начала поиска (смотрим всё подряд)
            OutBuf[6] = Convert.ToByte(hh.ToString(), 16);//час
            OutBuf[7] = Convert.ToByte(dd.ToString(), 16);//число
            OutBuf[8] = Convert.ToByte(mm.ToString(), 16);//месяц
            string yystr = yy.ToString();//переводим год в строку чтобы вытащить последние 2 цифры
            OutBuf[9] = Convert.ToByte(yystr.Substring(2,2),16);//из полного года (например 2017) берём последние 2 цифры (17)
            OutBuf[10] = 0x00FF;//признак зима\лето
            OutBuf[11] = it;//время интегрирования
            UInt16 crc = dp.ComputeCrc(OutBuf);
            Array.Resize(ref OutBuf, OutBuf.Length + 2);
            OutBuf[OutBuf.Length - 2] = Convert.ToByte(crc % 256); //контрольная сумма
            OutBuf[OutBuf.Length - 1] = Convert.ToByte(crc / 256);                                          
            System.Threading.Thread.Sleep(100);
            return OutBuf;
        }

        public byte[] FormReadCurrentPointer(DataProcessing dp)
        {//процедура, формирующая массив для поиска текущего указателя в профиле мощности
            byte[] OutBuf = new byte[5];

            return OutBuf;
        }

        public byte[] FormPowerProfileArray(DataProcessing dp, byte RecordAddressHi, byte RecordAddressLow, byte MemoryNumber, byte BytesInfo)
        {   //функция возвращает массив для запроса на чтение профиля мощности              
            //RecordAddress - адрес очередной записи
            byte[] OutBuf = new byte[6];
            OutBuf[0] = Convert.ToByte(this.NetAddress); //сетевой адрес
            OutBuf[1] = 0x0006; //запрос на профиль
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

        public Bitmap DrawVectorDiagramm(PictureBox pixBox, RichTextBox richText)
        {
            //рисуем векторную диаграмму
            Bitmap map = new Bitmap(pixBox.Width, pixBox.Height); //создаём точечное изображение         
            Graphics g = Graphics.FromImage(map);//создаём графический объект на основе точечного изображения
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//сглаживание
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; //сглаживание текста       
            Pen p1 = new Pen(Color.Red, 5);//перо для рисования фазы 1 (вектор напряжения)
            p1.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
            Font f = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);

            float r = (float)this.MonitorToRead[4].phase1 - 50;//длина линии
            float x_centre = pixBox.Width / 2;//центральные координаты канвы 
            float y_centre = pixBox.Height / 2;//центральные координаты канвы
            //рисуем первую фазу (от неё под углом идут все остальные)
            g.DrawString("Пофазная векторная диаграмма", f, Brushes.Black, 100, 2);
            g.DrawLine(p1, x_centre, y_centre, x_centre + r, y_centre);//рисуем вектор напряжения для фалы 1
            g.DrawString("Фаза 1", richText.Font, Brushes.Red, x_centre + r, y_centre);
            //вектор тока фазы 1
            {
                Pen p = new Pen(Color.Pink, 5);//перо для рисования фазы 2 (вектор тока)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase1) * 180) / Math.PI);//получаем угол вектора тока
                if (this.MonitorToRead[3].phase1 < 0) { angle_current = 360 - angle_current; }
                float x_current, y_current; r = 100;//длина линии
                x_current = x_centre + r * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_current / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                y_current = y_centre + r * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_current / 360));//вычисляем координату У второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                p.Dispose();
            }
            //Угол между фазами 1 и 2
            {
                Pen p = new Pen(Color.Blue, 5);//перо для рисования фазы 2 (вектор напряжения)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                double angle_voltage = this.MonitorToRead[7].phase0; //углы между фазами
                float x, y; r = (float)this.MonitorToRead[4].phase2 - 50;//длина линии
                x = x_centre + r * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_voltage / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                y = y_centre + r * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_voltage / 360));//вычисляем координату У второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x, y);//рисуем фазу           
                g.DrawString("Фаза 2", richText.Font, Brushes.Blue, x, y);
                p.Dispose();
                //вектор тока фазы 2
                {
                    Pen p2 = new Pen(Color.LightBlue, 5);//перо для рисования фазы 2 (вектор тока)
                    p2.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль завершения линии - стрелка
                    double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase2) * 180) / Math.PI);//получаем угол вектора тока
                    if (this.MonitorToRead[3].phase2 < 0) { angle_current = 360 - angle_current; }
                    float x_current, y_current; r = 100;//длина линии
                    x_current = x_centre + r * Convert.ToSingle(Math.Cos(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                    y_current = y_centre + r * Convert.ToSingle(Math.Sin(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату У второго конца линии в зависимости от угла
                    g.DrawLine(p2, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                    p2.Dispose();
                }
            }
            //Угол между фазами 1 и 3
            {
                Pen p = new Pen(Color.Green, 5);//перо для рисования фазы 3 (вектор напряжения)
                p.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
                double angle_voltage = this.MonitorToRead[8].phase0; //углы между фазами
                float x, y; r = (float)this.MonitorToRead[4].phase3 - 50;//длина линии
                x = x_centre + r * Convert.ToSingle(Math.Cos(2 * Math.PI * angle_voltage / 360));//вычиляем координаты второго конца линии в зависимости от угла
                y = y_centre + r * Convert.ToSingle(Math.Sin(2 * Math.PI * angle_voltage / 360));//вычиляем координаты второго конца линии в зависимости от угла
                g.DrawLine(p, x_centre, y_centre, x, y);//рисуем фазу              
                g.DrawString("Фаза 3", richText.Font, Brushes.Green, x, y);
                p.Dispose();
                //вектор тока фазы 3
                {
                    Pen p2 = new Pen(Color.PaleGreen, 5);//перо для рисования фазы 2 (вектор тока)
                    p2.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;//стиль звершения линии - стрелка
                    double angle_current = Convert.ToSingle((Math.Acos(this.MonitorToRead[3].phase3) * 180) / Math.PI);//получаем угол вектора тока
                    if (this.MonitorToRead[3].phase3 < 0) { angle_current = 360 - angle_current; }
                    float x_current, y_current; r = 100;//длина линии
                    x_current = x_centre + r * Convert.ToSingle(Math.Cos(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату Х второго конца линии в зависимости от угла
                    y_current = y_centre + r * Convert.ToSingle(Math.Sin(2 * Math.PI * (angle_current + angle_voltage) / 360));//вычисляем координату У второго конца линии в зависимости от угла
                    g.DrawLine(p2, x_centre, y_centre, x_current, y_current);//рисуем вектор тока       
                    p2.Dispose();
                }
            }
            p1.Dispose(); g.Dispose();
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


        public byte[] FormParamNewValueArray(List<FieldsValuesToWrite> formControlValuesList, string paramName, byte[] additional = null, char stringDivider = '/')
        {//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки (формируется в интерфейсе)   
            byte[] result = new byte[0];
            string newValueStr = String.Empty;
            switch (paramName)
            {
                case "Дата и время"://установка времени
                    {
                        FieldsValuesToWrite field = formControlValuesList.Find(x => x.name == "DTPicker");//ищем в списке значение поля по названию контрола
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

                case "Коррекция времени в пределах 2 мин"://коррекция времени в пределах 2 мин
                    {
                        FieldsValuesToWrite field = formControlValuesList.Find(x => x.name == "DTPicker");//ищем в списке значение поля по названию контрола
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

        public void ReadMonitorOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ МОДЕМ
        {
            //if (this.GainAccessOnModem(dp, richText, 1, 1, ref worker, 4) == false) { return; }//получение доступа (открытие канала)

            //richText.Invoke(new Action(delegate
            //{
            //    pb.Value = 0;//обнуляем прогресс бар
            //    pb.Maximum = this.MonitorToRead.Count; //количество сегментов прогресс бара = количество параметров
            //}));

            //foreach (var monitor in this.MonitorToRead)
            //{
            //    DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            //    richText.Invoke(new Action(delegate
            //    {
            //        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + monitor.name + " " + this.Name + "\r");
            //        richText.ScrollToCaret();
            //    }));
            //    byte[] OutBuf = this.FormMonitorArray(dp, monitor);//посылаем запрос на монитор (прилетят сумма + фазы)
            //    Exception ex = dp.SendData(OutBuf, 0); //посылаем запрос
            //    if (ex != null)
            //    {
            //        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            //        richText.Invoke(new Action(delegate
            //        {
            //            richText.SelectionColor = Color.Red;
            //            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение " + ex.Message + "\r");
            //            richText.ScrollToCaret();
            //        }));
            //        return;
            //    }

            //    if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
            //    byte[] answerArray = dp.Read(monitor.bytesToWait, 20000, true); //ждём определённое кол-ва байт ответа   

            //    int phasestart = 0;//номер начальной фазы (определяет считать с суммой или только по фазам или только сумму)
            //    int phaseend = 3;//номер последней фазы (определяет считать с суммой или только по фазам или только сумму)
            //    if ((monitor.name == "Мощность P") || (monitor.name == "Мощность S") || (monitor.name == "Мощность Q")) { phasestart = 0; phaseend = 3; }//вытаскиваем сумму и фазы
            //    if ((monitor.name == "Ток") || (monitor.name == "Коэффициент мощности") || (monitor.name == "Напряжение, усреднённое") || (monitor.name == "Напряжение")) { phasestart = 1; phaseend = 3; }//вытаскиваем только фазы
            //    if ((monitor.name == "Частота") || (monitor.name == "Частота, усреднённая") || (monitor.name == "Угол между фазными напряжениями 1 и 2") || (monitor.name == "Угол между фазными напряжениями 1 и 3")
            //    || (monitor.name == "Угол между фазными напряжениями 2 и 3"))
            //    { phasestart = 0; phaseend = 0; }//будем вытаскивать только сумму
            //                                     //в цикле разбираем ответы чтобы раскидать по фазам
            //    for (int i = phasestart; i <= phaseend; i++)
            //    {
            //        double value = this.ParseMonitorValue(dp, answerArray, monitor.name, i, 0);//читаем значение монитора из принятого ответа (в ответе все фазы)
            //                                                                                   //проверяем корректность. Если первый байт ответа не сетевой адрес, или значение монитора посчиталось с ошибкой, то следующая итерация
            //        if ((answerArray[0] != this.NetAddress) || (value == -1))
            //        {
            //            monitor.spreadValueByName("phase" + i.ToString(), -1);
            //            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            //            richText.Invoke(new Action(delegate
            //            {
            //                richText.SelectionColor = Color.Red;
            //                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " неудачно\r");
            //                richText.ScrollToCaret();
            //            }));
            //            continue;
            //        }
            //        //после того как получили ответ вызываем процедуру разбора ответа и приведения его в читаемый вид
            //        //эта процедура распределяет ответы по свойствам класса по имени свойства (в данном случае номер фазы)
            //        monitor.spreadValueByName("phase" + i.ToString(), value);
            //        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            //        richText.Invoke(new Action(delegate
            //        {
            //            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Фаза " + (i).ToString() + " удачно\r");
            //            richText.ScrollToCaret();
            //        }));
            //    }
            //}
            //pixBox.Image = this.DrawVectorDiagramm(pixBox, richText);//рисуем векторную диаграмму
        }

        public void ReadMonitorOnGate(Mercury228 gate, string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ ШЛЮЗ
        {
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

                        //пробуем: если время между часами счётчика и часами компа расходятся больше чем на 2 минуты в абсолютном выражении, то либо добавляем к часам счётчика 1 минуту, либо отнимаем от часов счётчика 1 минуту
                        //Почему так, а не просто попытка прошивки времени с компа:
                        //потому что команда коррекции в пределах 2 минут может быть выполнена один раз в сутки и этот процесс может быть итеративным изо дня в день, постепенно приближать часы счётчика к нужному времени.
                        if (Math.Abs(delta_minutes) > 2)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Orange;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Разница во времени составляет " + Math.Abs(delta_minutes).ToString() + " минут с учётом сдвига часов " + hours_shift.ToString() + ". Пытаюсь провести коррекцию часов счётчика в пределах 2 минут... " + "\r");
                                richText.ScrollToCaret();
                            }));

                            if (delta_minutes < 0)//если разница во времени отрицательная, значит время на счётчике больше и его надо уменьшить
                            {
                                dt = dt.AddMinutes(-1);//корректируем часы счётчика на 1 минуту назад чтобы приблизить ко времени на компе. 
                            }
                            else
                            {//если разница во времени положительная, значит время на счётчике меньше и его надо увеличить
                                dt = dt.AddMinutes(1);//корректируем часы счётчика на 1 минуту вперёд чтобы приблизить ко времени на компе. 
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

        public DataTable GetPowerProfileForCounterOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent)
        {          
            DataTable dt = this.ProfileDataTable;
            dt.Clear();

            dt.Columns[0].AllowDBNull = true;
            dt.Columns[1].AllowDBNull = true;
            dt.Columns[2].AllowDBNull = true;
            dt.Columns[8].AllowDBNull = true;
            //---получение доступа (открытие канала)
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
 
            //=================================КОЭФФИЦИЕНТ ТРАНСФОРМАЦИИ ПО НАПРЯЖЕНИЮ==================================================
            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения 
            }));
            param = this.ParametersToRead[2];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));
            byte[] OutBufTransformationVoltage = this.FormParameterArray(dp, param);
            ex = dp.SendData(OutBufTransformationVoltage, 0); //посылаем запрос    
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
            byte[] OutBufTransformationVoltageAnswer = dp.Read(param.bytesToWait, 10000, true); //ждём определённое кол-ва байт ответа

            if ((OutBufTransformationVoltageAnswer.Length == 5) && (OutBufTransformationVoltageAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения коэффициента транформации по напряжению\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(OutBufTransformationVoltageAnswer, param.name, 0);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
            }));
            //=================================КОЭФФИЦИЕНТ ТРАНСФОРМАЦИИ ПО ТОКУ==================================================

            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения 
            }));

            param = this.ParametersToRead[3];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));

            byte[] OutBufTransformationCurrent = this.FormParameterArray(dp, param);
            ex = dp.SendData(OutBufTransformationCurrent, 0); //посылаем запрос    
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
            byte[] OutBufTransformationCurrentAnswer = dp.Read(param.bytesToWait, 10000, true); //ждём определённое кол-ва байт ответа

            if ((OutBufTransformationCurrentAnswer.Length == 5) && (OutBufTransformationCurrentAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения коэффициента транформации по току\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(OutBufTransformationCurrentAnswer, param.name, 0);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));
            //====================================ДЛИТЕЛЬНОСТЬ ПЕРИОДА ИНТЕГРИРОВАНИЯ==============================================
            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения
            }));

            param = this.ParametersToRead[15];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));

            byte[] OutBufIntegrationTime = this.FormParameterArray(dp, param);
            ex = dp.SendData(OutBufIntegrationTime, 0); //посылаем запрос    
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
            byte[] IntegrationTimeAnswer = dp.Read(param.bytesToWait, 5000, true); //ждём определённое кол-ва байт ответа

            if ((IntegrationTimeAnswer.Length == 5) && (IntegrationTimeAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения длительности периода интегрирования профиля 1\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(IntegrationTimeAnswer, param.name, 0);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));
            //---------------------------------ПОСЛЕДНЯЯ ЗАПИСЬ ПРОФИЛЯ----------------------------------------------------------------
            //формируем запрос на текущую (последнюю) запись профиля
            byte[] OutBufCurRec = this.FormLastProfileRecordArray(dp);
            ex = dp.SendData(OutBufCurRec, 0); //посылаем запрос      
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
            byte[] CurRecordAnswer = dp.Read(10, 5000, true);//ждём определённое кол-ва байт ответа

            if ((CurRecordAnswer.Length == 5) && (CurRecordAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения текущей записи профиля мощности\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем здесь адрес текущей (последней) записи, считаем его пока максимальным, пока не вычислили адрес верхней даты
            string HigherHeaderAddrStr = CurRecordAnswer[6].ToString("X").PadLeft(2, '0') + CurRecordAnswer[7].ToString("X").PadLeft(2, '0');
            //-------------------------------------===============НИЖНЯЯ ДАТА=============-----------------------------------------------------
            //формируем запрос поиска адреса заголовка профиля мощности (на начало периода - нижняя дата)
            byte[] OutBufLowerHeaderAddr = this.FormHeaderProfileArray(dp, (byte)DateN.Hour, (byte)DateN.Day, (byte)DateN.Month, DateN.Year, Convert.ToByte(param.value));
            ex = dp.SendData(OutBufLowerHeaderAddr, 0); //посылаем запрос      
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
            byte[] LowerHeaderAddrAnswer = dp.Read(4, 20000, true);//ждём определённое кол-ва байт ответа

            //если вернулось error, то ошибка 
            if ((LowerHeaderAddrAnswer.Length == 5) && (LowerHeaderAddrAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (начало периода)\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //-------------------------------------------------------------------------------------------
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.Black;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Попытки чтения слова состояния задачи поиска начального адреса...\r");
                richText.ScrollToCaret();
            }));

            byte[] OutBufSearchState = { };//массив для исходящего запроса чтения слова состояния задачи поиска заголовка (для нижнего и верхнего адресов)
            byte[] SearchLowerHeaderAnswer = { };//ответ на запрос чтения слова состояния задачи поиска начального адреса (нижней даты) 
            bool LowerAdressIsFound = false;//логическая переменная, показывающая найден ли адрес заголовка нижней даты
            //делаем несколько итераций
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(1000);//ждём немного чтобы дать счётчику время
                //формируем запрос чтения слова состояния задачи поиска начального адреса (нижней даты) 
                OutBufSearchState = this.FormSearchHeaderStateArray(dp);
                ex = dp.SendData(OutBufSearchState, 0); //посылаем запрос      
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
                SearchLowerHeaderAnswer = dp.Read(8, 5000, true);//ждём определённое кол-ва байт ответа     

                //если вернулось error или слово состояния не равно 0 (т.е. заголовок не найден), то пробуём ещё раз
                if (((SearchLowerHeaderAnswer.Length == 5) && (SearchLowerHeaderAnswer[0] == 0x00045)) || (SearchLowerHeaderAnswer[1] != 0))
                {
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Orange;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (начала периода) не найден. Пробуем ещё...\r");
                        richText.ScrollToCaret();
                    }));
                    LowerAdressIsFound = false;
                    continue;
                }
                else
                {
                    LowerAdressIsFound = true;//говорим, что нашли адрес заголовка нижней даты
                    break;//и выходим из цикла
                }
            }
            //если значение логической переменной не стало истинным, то даём ошибку
            if (!LowerAdressIsFound)
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (начало периода)\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //-----------------------------------------------------------------------------------------------------
            string LowerHeaderAddrStr = SearchLowerHeaderAnswer[4].ToString("X").PadLeft(2, '0')
                + SearchLowerHeaderAnswer[5].ToString("X").PadLeft(2, '0');//здесь запоминаем начальный адрес (нижней даты)          
            //---------------------------------================ВЕРХНЯЯ ДАТА============----------------------------------------------
            //формируем запрос поиска адреса заголовка профиля мощности (на конец периода - верхняя дата)
            byte[] OutBufHigherHeaderAddr = this.FormHeaderProfileArray(dp, (byte)DateK.Hour, (byte)DateK.Day, (byte)DateK.Month, DateK.Year, Convert.ToByte(param.value));
            ex = dp.SendData(OutBufHigherHeaderAddr, 0); //посылаем запрос    
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
            byte[] HigherHeaderAddrAnswer = dp.Read(4, 20000, true);//ждём определённое кол-ва байт ответа

            //если вернулось error, то ошибка 
            if ((HigherHeaderAddrAnswer.Length == 5) && (HigherHeaderAddrAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Orange;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (конец периода). Чтение будет произведено до последнего адреса\r");
                    richText.ScrollToCaret();
                }));
                //т.к. получили ошибку при попытке считать адрес заголовка верхней даты, то не пытаемся делать запрос на слово состояния задачи поиска и перепрыгиваем следюбщий блок
                goto LetsRoll;
            }
            //-------------------------------------------------------------------------------------------
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.Black;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Попытки чтения слова состояния задачи поиска верхнего адреса...\r");
                richText.ScrollToCaret();
            }));

            byte[] SearchHigherHeaderAnswer = { };//ответ на запрос чтения слова состояния задачи поиска верхнего адреса (верхней даты) 
            bool HigherAdressIsFound = false;//логическая переменная, показывающая найден ли адрес заголовка верхней даты
            //делаем несколько итераций
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(1000);//ждём немного чтобы дать счётчику время
                //формируем запрос чтения слова состояния задачи поиска последнего адреса (верхней даты)
                OutBufSearchState = this.FormSearchHeaderStateArray(dp);
                ex = dp.SendData(OutBufSearchState, 0); //посылаем запрос      
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
                SearchHigherHeaderAnswer = dp.Read(8, 5000, true);//ждём определённое кол-ва байт ответа

                //если вернулось error или слово состояния не равно 0 (т.е. заголовок не найден), то пробуем ещё раз
                if (((SearchHigherHeaderAnswer.Length == 5) && (SearchHigherHeaderAnswer[0] == 0x00045)) || (SearchHigherHeaderAnswer[1] != 0))
                {
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Orange;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (конец периода) не найден. Пробуем ещё...\r");
                        richText.ScrollToCaret();
                    }));
                    HigherAdressIsFound = false;
                    continue;
                }
                else
                {
                    //если получилось, то заменяем адрес текущей (последней) записи на адрес, соответствующий верхней дате (концу периода)                  
                    HigherHeaderAddrStr = SearchHigherHeaderAnswer[4].ToString("X").PadLeft(2, '0') + SearchHigherHeaderAnswer[5].ToString("X").PadLeft(2, '0');
                    HigherAdressIsFound = true;//говорим, что нашли адрес заголовка верхней даты
                    break;
                }
            }
            if (!HigherAdressIsFound)//если не нашли адрес заголовка верхней даты
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Orange;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (конец периода). Чтение будет произведено до последней записи\r");
                    richText.ScrollToCaret();
                }));
            }
            else
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Green;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (конец периода) найден\r");
                    richText.ScrollToCaret();
                }));
            }
            //------------------------------------------------------------------------------------------------------------------------------------------------------------
            LetsRoll://поехали
            int CurrentAddrInt = Convert.ToInt32(LowerHeaderAddrStr, 16);//числовое представление текущего адреса (инициализируем с начального адреса - нижней даты)
            int HigherAddrInt = Convert.ToInt32(HigherHeaderAddrStr, 16);//числовое представление последнего адреса
            int PeriodsCount = Convert.ToInt16(Math.Abs(HigherAddrInt - CurrentAddrInt) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5));//количество периодов интегрирования
            int diff = HigherAddrInt - CurrentAddrInt;//разница между адресами
            if (diff < 0)//случай, когда адрес нижней записи больше адреса верхней записи (из-за цикличности адресного пространства)
            {
                PeriodsCount = Convert.ToInt16((65536 - Math.Abs(diff)) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5));//количество периодов интегрирования
                //PeriodsCount = Math.Abs(Convert.ToInt16((0 - Math.Abs(diff)) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5)));//количество периодов интегрирования
            }
            //цикл запросов от начального адреса к конечному (или от начального адреса к последней записи если заголовок с верхней датой не был найден)
            richText.Invoke(new Action(delegate
            {
                pb.Maximum = PeriodsCount;
                crl.Text = "0";
                lrl.Text = " из " + PeriodsCount.ToString();//устанавливаем значения текстовых меток для отображения прогресса считывания
            }));
            do
            {
                //сначала читаем заголовок, потом энергию
                //=====================================================ЗАГОЛОВОК=================================================================
                string CurrentAddrStr = CurrentAddrInt.ToString("X").PadLeft(4, '0');//строковое представление текущего адреса
                byte RecordAddessHi = Convert.ToByte(CurrentAddrStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущего адреса
                byte RecordAddessLow = Convert.ToByte(CurrentAddrStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущего адреса
                //формируем запрос на получение профиля
                byte[] Package = this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, 0x0003, 0x0008);
                ex = dp.SendData(Package, 0);//посылаем
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
                answerArray = dp.Read(11, 5000, true);//ждём определённое кол-ва байт ответа 
                //если косяк
                if (answerArray.Length == 5)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска заголовка записи профиля по указанному адресу: " + RecordAddessLow.ToString("X") + ' ' + RecordAddessHi.ToString("X") + "\r");
                        richText.ScrollToCaret();
                    }));

                    DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0;
                    drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                    dt.Rows.Add(drE);

                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 2; //прогресс бар - ошибка получения очередной записи профиля
                    }));

                    CurrentAddrInt += 0x0008 * ((60 / Convert.ToInt16(this.ParametersToRead[15].value.ToString().PadLeft(2, '0'))) + 1); //наращиваем адрес для того чтобы вытащить следующий заголовок если текущий не нашли
                    if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой

                    richText.Invoke(new Action(delegate
                    {
                        crl.Text = pb.Value.ToString();//численное отображение прогресса 
                    }));

                    continue;
                }
                //запоминаем метку времени из заголовка (дата + час)
                string DateTimeStr = answerArray[2].ToString("X").PadLeft(2, '0') + "." + answerArray[3].ToString("X").PadLeft(2, '0')
                                           + "." + answerArray[4].ToString("X") + " " + answerArray[1].ToString("X").PadLeft(2, '0');

                //========================================ЭНЕРГИЯ=========================================================================
                //количество итераций: час / время интегрирования - 1
                for (int i = 0; i <= (60 / Convert.ToInt16(this.ParametersToRead[15].value)) - 1; i++)
                {//цикл по первой и второй получасовкам после заголовка

                    if (pb.Value == PeriodsCount) { break; }
                    if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                    DataRow dr = dt.NewRow();
                    //подрисовываем минуты к дате и часу
                    try
                    {
                        dr["date_time"] = DateTimeStr + ":" + (i * Convert.ToInt16(this.ParametersToRead[15].value)).ToString().PadLeft(2, '0');//получаем дату\время
                    }
                    catch (Exception ex_date)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Исключение: " + ex_date.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return dt;
                    }
                    CurrentAddrInt += 8;//наращиваем адрес в случае успеха для того чтобы вытащить показания                  
                    if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }
                    //формируем запрос на показания текущего часа
                    CurrentAddrStr = CurrentAddrInt.ToString("X").PadLeft(4, '0');
                    RecordAddessHi = Convert.ToByte(CurrentAddrStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущей записи
                    RecordAddessLow = Convert.ToByte(CurrentAddrStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущей записи
                    Package = this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, 0x0003, 0x0008);//формируем запрос на получение профиля
                    ex = dp.SendData(Package, 0);
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
                    answerArray = dp.Read(11, 3000, true);//ждём определённое кол-ва байт ответа 

                    if (answerArray.Length == 5)
                    {
                        DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0;
                        drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                        dt.Rows.Add(drE);

                        richText.Invoke(new Action(delegate
                        {
                            pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля 
                            crl.Text = pb.Value.ToString();//численное отображение прогресса   
                        }));

                        continue;
                    }

                    byte t = Convert.ToByte(this.ParametersToRead[15].value);//длительность периода интегрирования - нужна при расчёте
                    if (t == 0) t = 1;//чтобы не делить на 0 при расчёте
                    int a = Convert.ToInt16(this.CounterConst);//постоянная сч-ка - нужна при расчёте                                         
                    //разбираем полученную запись
                  
                    string valueStr = '0' + Convert.ToString(answerArray[1], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[2], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (A+)             
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                 
                    double N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    double value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_a_plus"] = value;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[3], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[4], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (A-)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_a_minus"] = value / 2;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[5], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[6], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (R+)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет      
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_r_plus"] = value / 2;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[7], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[8], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (R-)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет     
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_r_minus"] = value / 2;//заносим значение в строку

                    dt.Rows.Add(dr);//добавляем текущую строку в таблицу

                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 1;
                        crl.Text = pb.Value.ToString();//численное отображение прогресса
                    }));
                    //=========записываем полученную строку в таблицу для хранения профилей=================
                    ex = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                        dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.DarkOrange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + "Ошибка записи профиля в базу: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                    }
                }
                CurrentAddrInt += 0x0008;//наращиваем адрес для следующего заголовка
                if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой
            }
            //while (CurrentAddrInt < HigherAddrInt);
            while (crl.Text != pb.Maximum.ToString()) ;//ждём, пока не сравняются счётчик считанных записей и расчётное количество записей
            return dt;
        }

        public DataTable GetPowerProfileForCounterOnGate(string workingPort, Mercury228 gate, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent, int packnum)
        {
            DataTable dt = this.ProfileDataTable;
            dt.Clear();

            dt.Columns[0].AllowDBNull = true;
            dt.Columns[1].AllowDBNull = true;
            dt.Columns[2].AllowDBNull = true;
            dt.Columns[8].AllowDBNull = true;

            //---получение доступа (открытие канала)
            if (this.GainAccessOnGate(gate, dp, packnum, richText, 1, 1, ref worker, 4) == false) { return dt; }
            //---нужно получить постоянную счётчика                                                 
            CounterParameterToRead param = this.ParametersToRead[11];

            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "...\r");
                richText.ScrollToCaret();
                pb.Value = 0; pb.Maximum = 2;//прогресс бар начальные значения  
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
       
            //=================================КОЭФФИЦИЕНТ ТРАНСФОРМАЦИИ ПО НАПРЯЖЕНИЮ==================================================
            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения 
            }));

            param = this.ParametersToRead[2];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));

            packnum += 1;
            byte[] OutBufTransformationVoltage = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
            ex = dp.SendData(OutBufTransformationVoltage, 0); //посылаем запрос    

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
            byte[] OutBufTransformationVoltageAnswer = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

            if ((OutBufTransformationVoltageAnswer.Length == 5) && (OutBufTransformationVoltageAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения коэффициента транформации по напряжению\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(OutBufTransformationVoltageAnswer, param.name, 8);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
            }));
            //=================================КОЭФФИЦИЕНТ ТРАНСФОРМАЦИИ ПО ТОКУ==================================================

            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения 
            }));

            param = this.ParametersToRead[3];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));
            packnum = +1;
            byte[] OutBufTransformationCurrent = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
            ex = dp.SendData(OutBufTransformationCurrent, 0); //посылаем запрос    
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
            byte[] OutBufTransformationCurrentAnswer = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

            if ((OutBufTransformationCurrentAnswer.Length == 5) && (OutBufTransformationCurrentAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения коэффициента транформации по току\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(OutBufTransformationCurrentAnswer, param.name, 8);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
            }));
            //====================================ДЛИТЕЛЬНОСТЬ ПЕРИОДА ИНТЕГРИРОВАНИЯ==============================================
            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;
                pb.Maximum = 2;//прогресс бар начальные значения
            }));

            param = this.ParametersToRead[15];
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " " + this.Name + "\r");
                richText.ScrollToCaret();
            }));
            packnum += 1;
            byte[] OutBufIntegrationTime = gate.FormPackage(this.FormParameterArray(dp, param), 1, dp, packnum);
            ex = dp.SendData(OutBufIntegrationTime, 0); //посылаем запрос    
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
            byte[] IntegrationTimeAnswer = dp.Read(param.bytesToWait + 9, 10000, true); //ждём определённое кол-ва байт ответа

            if ((IntegrationTimeAnswer.Length == 5) && (IntegrationTimeAnswer[0] == 0x00045))
            {
                param.value = "Ошибка";
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения длительности периода интегрирования профиля 1\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем значение параметра (длительность периода интегрирования профиля)
            param.value = this.ParseParameterValue(IntegrationTimeAnswer, param.name, 8);
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));
            //---------------------------------ПОСЛЕДНЯЯ ЗАПИСЬ ПРОФИЛЯ----------------------------------------------------------------
            packnum += 1;//наращиваем номер пакета для шлюза
            //формируем запрос на текущую (последнюю) запись профиля
            byte[] OutBufCurRec = gate.FormPackage(this.FormLastProfileRecordArray(dp), 1, dp, packnum);
            ex = dp.SendData(OutBufCurRec, 0); //посылаем запрос      
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
            byte[] CurRecordAnswer = dp.Read(10 + 9, 10000, true);//ждём определённое кол-ва байт ответа

            if ((CurRecordAnswer.Length == 5) && (CurRecordAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения текущей записи профиля мощности\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //запоминаем здесь адрес текущей (последней) записи, считаем его пока максимальным, пока не вычислили адрес верхней даты
            string HigherHeaderAddrStr = CurRecordAnswer[6 + 8].ToString("X").PadLeft(2, '0') + CurRecordAnswer[7 + 8].ToString("X").PadLeft(2, '0');
            //-------------------------------------===============НИЖНЯЯ ДАТА=============-----------------------------------------------------
            packnum += 1;//наращиваем номер пакета для шлюза
            //формируем запрос поиска адреса заголовка профиля мощности (на начало периода - нижняя дата)
            byte[] OutBufLowerHeaderAddr = gate.FormPackage(this.FormHeaderProfileArray(dp, (byte)DateN.Hour, (byte)DateN.Day, (byte)DateN.Month, DateN.Year, Convert.ToByte(param.value)), 1, dp, packnum);
            ex = dp.SendData(OutBufLowerHeaderAddr, 0); //посылаем запрос      
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
            byte[] LowerHeaderAddrAnswer = dp.Read(4 + 9, 20000, true);//ждём определённое кол-ва байт ответа
            //если вернулось error, то ошибка 
            if ((LowerHeaderAddrAnswer.Length == 5) && (LowerHeaderAddrAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (начало периода)\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //-------------------------------------------------------------------------------------------
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.Black;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Попытки чтения слова состояния задачи поиска начального адреса...\r");
                richText.ScrollToCaret();
            }));

            byte[] OutBufSearchState = { };//массив для исходящего запроса чтения слова состояния задачи поиска заголовка (для нижнего и верхнего адресов)
            byte[] SearchLowerHeaderAnswer = { };//ответ на запрос чтения слова состояния задачи поиска начального адреса (нижней даты) 
            bool LowerAdressIsFound = false;//логическая переменная, показывающая найден ли адрес заголовка нижней даты
            //делаем несколько итераций
            for (int i = 0; i < 20; i++)
            {
                packnum += 1;//наращиваем номер пакета для шлюза
                Thread.Sleep(1000);//ждём немного чтобы дать счётчику время
                //формируем запрос чтения слова состояния задачи поиска начального адреса (нижней даты) 
                OutBufSearchState = gate.FormPackage(this.FormSearchHeaderStateArray(dp), 1, dp, packnum);
                ex = dp.SendData(OutBufSearchState, 0); //посылаем запрос      
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
                SearchLowerHeaderAnswer = dp.Read(8 + 9, 20000, true);//ждём определённое кол-ва байт ответа     
                //если вернулось error или слово состояния не равно 0 (т.е. заголовок не найден), то пробуём ещё раз
                if (((SearchLowerHeaderAnswer.Length == 5) && (SearchLowerHeaderAnswer[0] == 0x00045)) || (SearchLowerHeaderAnswer[1 + 8] != 0))
                {
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Orange;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (начала периода) не найден. Пробуем ещё...\r");
                        richText.ScrollToCaret();
                    }));
                    LowerAdressIsFound = false;
                    continue;
                }
                else
                {
                    LowerAdressIsFound = true;//говорим, что нашли адрес заголовка нижней даты
                    break;//и выходим из цикла
                }
            }
            //если значение логической переменной не стало истинным, то даём ошибку
            if (!LowerAdressIsFound)
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Red;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (начало периода)\r");
                    richText.ScrollToCaret();
                }));
                return dt;
            }
            //-----------------------------------------------------------------------------------------------------
            string LowerHeaderAddrStr = SearchLowerHeaderAnswer[4 + 8].ToString("X").PadLeft(2, '0')
                + SearchLowerHeaderAnswer[5 + 8].ToString("X").PadLeft(2, '0');//здесь запоминаем начальный адрес (нижней даты)          
            //int LowerHeaderAddrInt = Convert.ToInt16(LowerHeaderAddrStr, 16);//числовое представление начального адреса
            //---------------------------------================ВЕРХНЯЯ ДАТА============----------------------------------------------
            packnum += 1;//наращиваем номер пакета для шлюза
            //формируем запрос поиска адреса заголовка профиля мощности (на конец периода - верхняя дата)
            byte[] OutBufHigherHeaderAddr = gate.FormPackage(this.FormHeaderProfileArray(dp, (byte)DateK.Hour, (byte)DateK.Day, (byte)DateK.Month, DateK.Year, Convert.ToByte(param.value)), 1, dp, packnum);
            ex = dp.SendData(OutBufHigherHeaderAddr, 0); //посылаем запрос    
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
            byte[] HigherHeaderAddrAnswer = dp.Read(4 + 9, 20000, true);//ждём определённое кол-ва байт ответа
            //если вернулось error, то ошибка 
            if ((HigherHeaderAddrAnswer.Length == 5) && (HigherHeaderAddrAnswer[0] == 0x00045))
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Orange;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (конец периода). Чтение будет произведено до последнего адреса\r");
                    richText.ScrollToCaret();
                }));
                //т.к. получили ошибку при попытке считать адрес заголовка верхней даты, то не пытаемся делать запрос на слово состояния задачи поиска и перепрыгиваем следюбщий блок
                goto LetsRoll;
            }
            //-------------------------------------------------------------------------------------------
            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.Black;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Попытки чтения слова состояния задачи поиска верхнего адреса...\r");
                richText.ScrollToCaret();
            }));

            byte[] SearchHigherHeaderAnswer = { };//ответ на запрос чтения слова состояния задачи поиска верхнего адреса (верхней даты) 
            bool HigherAdressIsFound = false;//логическая переменная, показывающая найден ли адрес заголовка верхней даты
            //делаем несколько итераций
            for (int i = 0; i < 20; i++)
            {
                packnum += 1;//наращиваем номер пакета для шлюза
                Thread.Sleep(1000);//ждём немного чтобы дать счётчику время
                //формируем запрос чтения слова состояния задачи поиска последнего адреса (верхней даты)
                OutBufSearchState = gate.FormPackage(this.FormSearchHeaderStateArray(dp), 1, dp, packnum);
                ex = dp.SendData(OutBufSearchState, 0); //посылаем запрос      
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
                SearchHigherHeaderAnswer = dp.Read(8 + 9, 20000, true);//ждём определённое кол-ва байт ответа
                //если вернулось error или слово состояния не равно 0 (т.е. заголовок не найден), то пробуем ещё раз
                if (((SearchHigherHeaderAnswer.Length == 5) && (SearchHigherHeaderAnswer[0] == 0x00045)) || (SearchHigherHeaderAnswer[1 + 8] != 0))
                {
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Orange;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (конец периода) не найден. Пробуем ещё...\r");
                        richText.ScrollToCaret();
                    }));
                    HigherAdressIsFound = false;
                    continue;
                }
                else
                {
                    //если получилось, то заменяем адрес текущей (последней) записи на адрес, соответствующий верхней дате (концу периода)                  
                    HigherHeaderAddrStr = SearchHigherHeaderAnswer[4 + 8].ToString("X").PadLeft(2, '0') + SearchHigherHeaderAnswer[5 + 8].ToString("X").PadLeft(2, '0');
                    HigherAdressIsFound = true;//говорим, что нашли адрес заголовка верхней даты
                    break;
                }
            }
            if (!HigherAdressIsFound)//если не нашли адрес заголовка верхней даты
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Orange;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка поиска адреса заголовка профиля мощности (конец периода). Чтение будет произведено до последней записи\r");
                    richText.ScrollToCaret();
                }));
            }
            else
            {
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.Green;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Адрес заголовка записи профиля мощности (конец периода) найден\r");
                    richText.ScrollToCaret();
                }));
            }
            //------------------------------------------------------------------------------------------------------------------------------------------------------------
            LetsRoll://поехали
            int CurrentAddrInt = Convert.ToInt32(LowerHeaderAddrStr, 16);//числовое представление текущего адреса (инициализируем с начального адреса - нижней даты)
            int HigherAddrInt = Convert.ToInt32(HigherHeaderAddrStr, 16);//числовое представление верхнего адреса
            int diff = HigherAddrInt - CurrentAddrInt;//разница между адресами
            int PeriodsCount = Convert.ToInt16(Math.Abs(diff) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5));//количество периодов интегрирования
            //ЧТО БУДЕТ, ЕСЛИ ДЛЯ РАСЧЁТА КОЛИЧЕСТВА ПЕРИОДОВ СДЕЛАЕМ СЛЕДУЮЩЕЕ
            if (diff < 0)//случай, когда адрес нижней записи больше адреса верхней записи (из-за цикличности адресного пространства)
            {
                PeriodsCount = Convert.ToInt16((65536 - Math.Abs(diff)) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5));//количество периодов интегрирования
                //PeriodsCount = Math.Abs(Convert.ToInt16((0 - Math.Abs(diff)) / (Convert.ToDouble(this.ParametersToRead[15].value) / 2.5)));//количество периодов интегрирования
            }
            //цикл запросов от начального адреса к конечному (или от начального адреса к последней записи если заголовок с верхней датой не был найден)                                                                                                                                                   
            //работаем пока адрес текущей записи не сравняется с адресом последней или пользователь не остановит процесс
            //int offset = 8;//отступ вглубь общего массива ответа (наичнаем с 8 потому что это заголовок первого пакета) 
            richText.Invoke(new Action(delegate
            {
                pb.Maximum = PeriodsCount;
                crl.Text = "0";
                lrl.Text = " из " + PeriodsCount.ToString();//устанавливаем значение текстовых меток для отображения прогресса считывания
            }));
            do
            {
                //сначала читаем заголовок, потом энергию
                //=====================================================ЗАГОЛОВОК=================================================================
                string CurrentAddrStr = CurrentAddrInt.ToString("X").PadLeft(4, '0');//строковое представление текущего адреса
                byte RecordAddessHi = Convert.ToByte(CurrentAddrStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущего адреса
                byte RecordAddessLow = Convert.ToByte(CurrentAddrStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущего адреса
                //формируем запрос на получение профиля
                byte[] Package = gate.FormPackage(this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, 0x0003, 0x0008), 1, dp, packnum);
                ex = dp.SendData(Package, 0);//посылаем
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
                answerArray = dp.Read(11 + 9, 3000, true);//ждём определённое кол-ва байт ответа 
                                                                 //если косяк
                if (answerArray.Length == 5)
                {
                    DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0;
                    drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                    dt.Rows.Add(drE);

                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 2; //прогресс бар - ошибка получения очередной записи профиля
                    }));

                    CurrentAddrInt += 0x0008 * ((60 / Convert.ToInt16(this.ParametersToRead[15].value.ToString().PadLeft(2, '0'))) + 1); //наращиваем адрес для того чтобы вытащить следующий заголовок если текущий не нашли
                    if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой

                    richText.Invoke(new Action(delegate
                    {
                        crl.Text = pb.Value.ToString();//численное отображение прогресса 
                    }));

                    continue;
                }
                //запоминаем метку времени из заголовка (дата + час)
                string DateTimeStr = answerArray[10].ToString("X").PadLeft(2, '0') + "." + answerArray[11].ToString("X").PadLeft(2, '0')
                                           + "." + answerArray[12].ToString("X") + " " + answerArray[9].ToString("X").PadLeft(2, '0');

                //========================================ЭНЕРГИЯ=========================================================================
                //количество итераций: час / время интегрирования - 1
                for (int i = 0; i <= (60 / Convert.ToInt16(this.ParametersToRead[15].value)) - 1; i++)
                {//цикл по первой и второй получасовкам после заголовка

                    if (pb.Value == PeriodsCount) { break; }
                    if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
                    DataRow dr = dt.NewRow();
                    //подрисовываем минуты к дате и часу
                    dr["date_time"] = DateTimeStr + ":" + (i * Convert.ToInt16(this.ParametersToRead[15].value)).ToString().PadLeft(2, '0');//получаем дату\время
                    CurrentAddrInt += 8;//наращиваем адрес в случае успеха для того чтобы вытащить показания                  
                    if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }
                    //формируем запрос на показания текущего часа
                    CurrentAddrStr = CurrentAddrInt.ToString("X").PadLeft(4, '0');
                    RecordAddessHi = Convert.ToByte(CurrentAddrStr.Substring(0, 2), 16);  //получаем старший байт адреса из строки текущей записи
                    RecordAddessLow = Convert.ToByte(CurrentAddrStr.Substring(2, 2), 16); //получаем младший байт адреса из строки текущей записи
                    Package = gate.FormPackage(this.FormPowerProfileArray(dp, RecordAddessHi, RecordAddessLow, 0x0003, 0x0008), 1, dp, packnum);//формируем запрос на получение профиля
                    ex = dp.SendData(Package, 0);
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
                    answerArray = dp.Read(11 + 9, 5000, true);//ждём определённое кол-ва байт ответа 

                    if (answerArray.Length == 5)
                    {
                        DataRow drE = dt.NewRow(); drE["e_a_plus"] = 0; drE["e_a_minus"] = 0;
                        drE["e_r_plus"] = 0; drE["e_r_minus"] = 0; drE["date_time"] = Convert.ToDateTime("01.01.2000 01:01:01"); ;
                        dt.Rows.Add(drE);

                        richText.Invoke(new Action(delegate
                        {
                            pb.Value += 1; //прогресс бар - ошибка получения очередной записи профиля 
                            crl.Text = pb.Value.ToString();//численное отображение прогресса   
                        }));

                        continue;
                    }

                    byte t = Convert.ToByte(this.ParametersToRead[15].value);//длительность периода интегрирования - нужна при расчёте
                    if (t == 0) t = 1;//чтобы не делить на 0 при расчёте
                    int a = Convert.ToInt16(this.CounterConst);//постоянная сч-ка - нужна при расчёте    

                    string valueStr = '0' + Convert.ToString(answerArray[9], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[10], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (A+)             
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет

                    double N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    double value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_a_plus"] = value;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[11], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[12], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (A-)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_a_minus"] = value / 2;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[13], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[14], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (R+)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет      
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_r_plus"] = value / 2;//заносим значение в строку

                    valueStr = '0' + Convert.ToString(answerArray[15], 2).PadLeft(7, '0').Remove(0, 1) + Convert.ToString(answerArray[16], 2).PadLeft(8, '0');//формируем 2-ичную строку числа (R-)
                    if (valueStr == "1111111111111111") { valueStr = "0"; } //если получили такие байты, это значит что данных нет     
                    N = Convert.ToInt64(valueStr, 2);//конвертируем строку в число                                                                                          
                    value = ((N * (60 / t)) / (2 * a)) * Convert.ToInt64(this.ParametersToRead[3].value) * Convert.ToInt64(this.ParametersToRead[2].value);//расчёт по формуле
                    dr["e_r_minus"] = value / 2;//заносим значение в строку
                   
                    dt.Rows.Add(dr);//добавляем текущую строку в таблицу

                    richText.Invoke(new Action(delegate
                    {
                        pb.Value += 1;
                        crl.Text = pb.Value.ToString();//численное отображение прогресса
                    }));
                    //=========записываем полученную строку в таблицу для хранения профилей=================
                    ex = DataBaseManagerMSSQL.Add_Profile_Record(this.ID, dr["date_time"].ToString(), dr["e_a_plus"].ToString(),
                        dr["e_a_minus"].ToString(), dr["e_r_plus"].ToString(), dr["e_r_minus"].ToString(), t);
                    if (ex != null)
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.DarkOrange;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + "Ошибка записи профиля в базу: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                    }                   
                }
                CurrentAddrInt += 0x0008;//наращиваем адрес для следующего заголовка             
                if (CurrentAddrInt > 65535) { CurrentAddrInt = 0; }//если дошли до конца адресного пространства, то нужно начать по-новой //ПОМЕНЯТЬ НА 65534???                     
            }
            //while (CurrentAddrInt < HigherAddrInt);
            while (crl.Text != pb.Maximum.ToString());//ждём, пока не сравняются счётчик считанных записей и расчётное количество записей
            return dt;
        }

        public void ReadJournalCQCOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {

        }

        public void ReadJournalCQCOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {

        }
    }
}


