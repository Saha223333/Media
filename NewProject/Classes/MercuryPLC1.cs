using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;

namespace NewProject
{
    public class MercuryPLC1 : IDevice, ICounter
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public int NetAddress { get; set; }
        public string SerialNumber { get; set; }
        public bool clonemode;//режим клонирования. Нужен для того, чтобы избежать записи показаний в базу (этот параметр подаётся в хранимую процедуру записи показаний). Клонированные сч-ки ссылаются на реальные сч-ки
        //в БД
        public bool virtualmode;//виртуальный режим. Нужен для того, чтобы избежать обработки 4-х значных показаний (вообще не доходить до хранимой процедуры записи показаний). Виртуальные сч-ки не имеют реального представления в БД. Режим использууется при опросе концентратора до 1024

        public BindingList<MercuryPLCPackage> Packages;//коллекция пакетов, содержащихся в ответе от концентратора
        public DataTable ProfileDataTable { get; set; } //таблица, хранящая снятый профиль

        public BindingList<CounterEnergyToRead> EnergyToRead { get; set; } //перечень энергии для опроса счётчиков с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter)     
        public BindingList<CounterParameterToRead> ParametersToRead { get; set; } //перечень параметров для опроса счётчиков с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter) 
        public BindingList<CounterParameterToWrite> ParametersToWrite { get; set; } //перечень параметров для записи в счётчики с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter)      
        public BindingList<CounterMonitorParameterToRead> MonitorToRead { get; set; } //перечень параметров тока (монитор) для счётчиков с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter)      
        public BindingList<CounterJournalToRead> JournalToRead { get; set; } //перечень журнала для счётчиков с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter) 
        public BindingList<CounterJournalCQCToRead> JournalCQCToRead { get; set; } //перечень журнала ПКЭ для счётчиков с цифровым интерфейсом (здесь только для того чтобы удовлетворять ICounter)

        public int Divider { get; set; }//хранит значение делителя для вычисления энергии для счётчиков с цифровым интерфейсом (здесь не используется)
        public int CounterConst { get; set; }
        public int TransformationRate { get; set; }//коэффициент трансформации (берётся из БД)

        public DateTime? lastDateZone0 = null; //последние дата и время суммы
        public DateTime? lastDateZone1 = null; //последние дата и время первого тарифа
        public DateTime? lastDateZone2 = null; //последние дата и время второго тарифа
        public DateTime? lastDateZone3 = null; //последние дата и время третьего тарифа
        public DateTime? lastDateZone4 = null; //последние дата и время четвёртого тарифа

        public MercuryPLC1(int pid, int pparentid, string pname, int pnetadr, string psernum, bool pclonemode, bool pvirtualmode)
        {
            this.ID = pid;
            this.ParentID = pparentid;
            this.Name = pname;
            this.NetAddress = pnetadr;
            this.SerialNumber = psernum;
            this.clonemode = pclonemode;
            this.virtualmode = pvirtualmode;
            Packages = new BindingList<MercuryPLCPackage>();
        }

        public string ReadEnergyPLC(DataProcessing dp, byte[] array, int offset)
        {   //процедура чтения энергии и раскидывающая по тарифам
            Packages.Clear();      
            try
            {
                int count = array.Length / 11;//количество пакетов в полезной нагрузке ответа от концентратора (целое число без остатка)
                for (int i = 3; i < count * 11; i += 11)//циклимся по полезной нагрузке ответа от концентратора
                {
                    int type = array[i + offset];//смотрим какой тип PLC-пакета. Идёт в массиве перед данными. Описан в протоколе
                    //смотрим какой тип PLC-пакета, и решаем как его обработать (как сформировать число)
                    string valueStr = ""; double energy = 0;
                    if ((type == 0x0000) || (type == 0x0001) || (type == 0x0002) || (type == 0x0003) || (type == 0x000F) || (type == 0x001F) || (type == 0x0010) || (type == 0x0011) || (type == 0x0012)
                    || (type == 0x0013) || (type == 0x0080) || (type == 0x0081) || (type == 0x0082) || (type == 0x0083))//для этих типов пакетов показания формируются таким образом
                    {
                        valueStr = array[i + 2 + offset].ToString("X").PadLeft(2, '0') + array[i + 1 + offset].ToString("X").PadLeft(2, '0');//получаем 16-ричную строку поля base 
                        //energy = Convert.ToInt64(valueStr, 16) + array[i + 3 + offset];//энергия = base + inc
                        energy = Convert.ToInt64(valueStr, 16);//энергия = base ТЕПЕРЬ БЕЗ ПОЛЯ INC!!!!!!!!!
                    }//для этих типов пакетов показания формируются другим образом 
                    if ((type == 0x0040) || (type == 0x0041) || (type == 0x0042) || (type == 0x0043) || (type == 0x004F))//для этих типов пакетов показания формируются таким образом
                    {
                        valueStr = array[i + 3 + offset].ToString("X").PadLeft(2, '0') +
                            array[i + 2 + offset].ToString("X").PadLeft(2, '0') + array[i + 1 + offset].ToString("X").PadLeft(2, '0');
                        //+","+ array[i + 4 + offset].ToString("X").PadLeft(2, '0');//получаем 16-ричную строку поля kwth
                        energy = Convert.ToInt64(valueStr, 16) + (array[i + 4 + offset] / (double)100);//получаем десятиричное представление целой части + сотые доли
                    }
                    
                    array[i + 8 + offset] += 1; //наращиваем номер дня т.к. явно дата в массиве расходится с датой, отображаемой в ПО от производителя железа
                    array[i + 9 + offset] += 1; //наращиваем номер месяца т.к. явно дата в массиве расходится с датой, отображаемой в ПО от производителя железа
                   
                    valueStr = array[i + 8 + offset].ToString().PadLeft(2, '0') + "." + array[i + 9 + offset].ToString().PadLeft(2, '0')
                     + "." + array[i + 10 + offset].ToString() + " " + array[i + 7 + offset].ToString().PadLeft(2, '0')
                     + ":" + array[i + 6 + offset].ToString().PadLeft(2, '0') + ":" + "00";//получаем дату\время в виде строки  
                    
                    string name = "";
                    switch (type)//в зависимости от типа пакета присваиваем ему имя для прозрачности при отладке
                    {
                        case 0x000F: name = "Сумма, текущее потребление"; break;
                        case 0x0000: name = "Тариф 1, текущее потребление"; break;
                        case 0x0001: name = "Тариф 2, текущее потребление"; break;
                        case 0x0002: name = "Тариф 3, текущее потребление"; break;
                        case 0x0003: name = "Тариф 4, текущее потребление"; break;
                        
                        case 0x001F: name = "Сумма, текущее потребление"; break;
                        case 0x0010: name = "Тариф 1, текущее потребление"; break;
                        case 0x0011: name = "Тариф 2, текущее потребление"; break;
                        case 0x0012: name = "Тариф 3, текущее потребление"; break;
                        case 0x0013: name = "Тариф 4, текущее потребление"; break;

                        case 0x0080: name = "Холодная вода A, текущее потребление"; break;//пойдёт в первый тариф
                        case 0x0081: name = "Холодная вода B, текущее потребление"; break;//пойдёт во второй тариф
                        case 0x0082: name = "Горячая вода А, текущее потребление"; break;//пойдёт в третий тариф
                        case 0x0083: name = "Горячая вода B, текущее потребление"; break;//пойдёт в четвёртый тариф

                        case 0x004F: name = "Сумма, суточный срез"; break;
                        case 0x0040: name = "Тариф 1, суточный срез"; break;
                        case 0x0041: name = "Тариф 2, суточный срез"; break;
                        case 0x0042: name = "Тариф 3, суточный срез"; break;
                        case 0x0043: name = "Тариф 4, суточный срез"; break;                      
                    }
                    DateTime datetime = Convert.ToDateTime("31.01." + DateTime.Today.Year + " " + array[i + 7 + offset].ToString().PadLeft(2, '0')
                     + ":" + array[i + 6 + offset].ToString().PadLeft(2, '0') + ":" + "00");//инициализируем дату-время
                    try
                    {
                        datetime = Convert.ToDateTime(valueStr);//переводим строку в дату\время для того чтобы потом сортировать                     
                        Packages.Add(new MercuryPLCPackage(type, energy, datetime, name));//добавляем пакет от концентратора в коллекцию
                    }
                    catch
                    {       
                        continue;
                    }
                }

                if (Packages.Count == 0) return "-1";
                //нужно запомнить даты  последних показаний счётчика во временные переменные чтобы откатить после клонированного опроса.
                //Проблема в том, что при любом опросе (в т.ч. клонированном) даты пишутся в экземпляр и подаются в хранимую процедуру создания строки показаний PLC. 
                //Но в хранимой процедуре при клонированном опросе показания в базу не записываются (значит и в экземпляр счётчика записываться не должны)
                DateTime? lastDateZone0Tmp = this.lastDateZone0;
                DateTime? lastDateZone1Tmp = this.lastDateZone1;
                DateTime? lastDateZone2Tmp = this.lastDateZone2;
                DateTime? lastDateZone3Tmp = this.lastDateZone3;
                DateTime? lastDateZone4Tmp = this.lastDateZone4;

                double currentValueZone0 = -1;
                double currentValueZone1 = -1;
                double currentValueZone2 = -1;
                double currentValueZone3 = -1;
                double currentValueZone4 = -1;          
                //нужно проанализировать пакеты, пришедшие от концентратора, чтобы выбрать наиболее свежий
                string packname = "";//имя для пакета для записи в базу
                string details = "дата считанных показаний меньше или равна дате последних показаний! Следующие даты и показания хранятся в концентраторе:";//значение по-умолчанию на случай если не дойдёт до хранимой процедуры (не будет свежих пакетов, удовлетворяющих условиям по дате)
                foreach (MercuryPLCPackage pack in Packages)
                {//анализируем очередной пакет в наборе пришедших                                   
                    //смотрим какой тип пакета
                    if (pack.type == 0x001F || pack.type == 0x000F || pack.type == 0x004F)//сумма тарифов (текущее потребление и суточный срез)                   
                        if (pack.datetime > Convert.ToDateTime(this.lastDateZone0))//если текущее хранящееся значение даты меньше чем вновь прибывшее, то перекрываем новым
                        {
                            this.lastDateZone0 = pack.datetime; 
                            currentValueZone0 = pack.value; //также и с энергией         
                            packname = "Энергия, текущее потребление";//значение по-умолчанию
                            if (pack.type == 0x004F) packname = "Энергия, суточный срез";
                        }
                        else details += ' ' + pack.datetime.ToString() + " --- " + pack.value.ToString() + " \\\\ ";//на случай если не дойдёт до хранимой процедуры

                    if (pack.type == 0x0010 || pack.type == 0x0000 || pack.type == 0x0080 || pack.type == 0x0040) //первый тариф (текущее потребление и суточный срез) + холодная вода А   
                        if (pack.datetime > Convert.ToDateTime(this.lastDateZone1))//если текущее хранящееся значение даты меньше чем вновь прибывшее, то перекрываем новым
                        {
                            this.lastDateZone1 = pack.datetime; 
                            currentValueZone1 = pack.value; //также и с энергией      
                            packname = "Энергия, текущее потребление";//значение по-умолчанию
                            if (pack.type == 0x0040) packname = "Энергия, суточный срез";
                            if (pack.type == 0x0080) packname = "Вода, текущее потребление";
                        }
                        else details += ' ' + pack.datetime.ToString() + " --- " + pack.value.ToString() + " \\\\ ";//на случай если не дойдёт до хранимой процедуры

                    if (pack.type == 0x0011 || pack.type == 0x0001 || pack.type == 0x0081 || pack.type == 0x0041) //второй тариф (текущее потребление и суточный срез) + холодная вода Б           
                        if (pack.datetime > Convert.ToDateTime(this.lastDateZone2))//если текущее хранящееся значение даты меньше чем вновь прибывшее, то перекрываем новым
                        {
                            this.lastDateZone2 = pack.datetime; 
                            currentValueZone2 = pack.value; //также и с энергией
                            packname = "Энергия, текущее потребление";//значение по-умолчанию
                            if (pack.type == 0x0041) packname = "Энергия, суточный срез";
                            if (pack.type == 0x0081) packname = "Вода, текущее потребление";
                        }
                        else details += ' ' + pack.datetime.ToString() + " --- " + pack.value.ToString() + " \\\\ ";//на случай если не дойдёт до хранимой процедуры

                    if (pack.type == 0x0012 || pack.type == 0x0002 || pack.type == 0x0082 || pack.type == 0x0042) //третий тариф (текущее потребление и суточный срез) + горячая вода А                  
                        if (pack.datetime > Convert.ToDateTime(this.lastDateZone3))//если текущее хранящееся значение даты меньше чем вновь прибывшее, то перекрываем новым
                        {
                            this.lastDateZone3 = pack.datetime; 
                            currentValueZone3 = pack.value; //также и с энергией
                            packname = "Энергия, текущее потребление";//значение по-умолчанию
                            if (pack.type == 0x0042) packname = "Энергия, суточный срез";
                            if (pack.type == 0x0082) packname = "Вода, текущее потребление";
                        }
                        else details += ' ' + pack.datetime.ToString() + " --- " + pack.value.ToString() + " \\\\ ";//на случай если не дойдёт до хранимой процедуры

                    if (pack.type == 0x0013 || pack.type == 0x0003 || pack.type == 0x0083 || pack.type == 0x0043) //четвёртый тариф (текущее потребление и суточный срез) + горячая вода Б            
                        if (pack.datetime > Convert.ToDateTime(this.lastDateZone4))//если текущее хранящееся значение даты меньше чем вновь прибывшее, то перекрываем новым
                        {
                            this.lastDateZone4 = pack.datetime; 
                            currentValueZone4 = pack.value; //также и с энергией  
                            packname = "Энергия, текущее потребление";//значение по-умолчанию
                            if (pack.type == 0x0043) packname = "Энергия, суточный срез";
                            if (pack.type == 0x0083) packname = "Вода, текущее потребление";
                        }
                        else details += ' ' + pack.datetime.ToString() + " --- " + pack.value.ToString() + " \\\\ ";//на случай если не дойдёт до хранимой процедуры
                }
                //---------------------------ВИРТУАЛЬНЫЙ РЕЖИМ--------------------------------------------
                if (this.virtualmode == true)
                {//если находимся в виртуальном режиме (для опроса концентратора до 1024), то нет необходимости делать какие-либо анализы т.к. виртуальные сч-ки не привязаны к БД и выдаём информацию как есть
                 //(наиболее свежие пакеты: наибольшие даты будут сохранены в экземпляре счётчика, а показания будут просто выведены в строку details и показаны в логе )
                    if (currentValueZone0 == -1) { currentValueZone0 += 1; }
                    if (currentValueZone1 == -1) { currentValueZone1 += 1; }
                    if (currentValueZone2 == -1) { currentValueZone2 += 1; }
                    if (currentValueZone3 == -1) { currentValueZone3 += 1; }
                    if (currentValueZone4 == -1) { currentValueZone4 += 1; }

                    details = "последние даты и показания по текущему сетевому адресу хранятся в концентраторе: Т0=" + currentValueZone0.ToString() + "/" + this.lastDateZone0.ToString()
                                  + ", T1=" + currentValueZone1.ToString() + "/" + this.lastDateZone1.ToString() + ", T2=" + currentValueZone2.ToString() + "/" + this.lastDateZone2.ToString() + ", T3=" + currentValueZone3.ToString() + "/" + this.lastDateZone3.ToString() + ", T4=" + currentValueZone4.ToString() + "/" + this.lastDateZone4.ToString();
                    return details;
                }
                //-----------------------------------------------------------------------------------------------
                //после того как проанализировали все пакеты и выбрали наиболее свежий, пишем новые показания в базу
                //сначала проверим, есть ли смысл что-то записывать
                //в случае если пришло хотя бы одно свежее показание (-1 сменилось на большее значение), то пытаемся записать показания в базу
                if (currentValueZone0 > -1 || currentValueZone1 > -1 || currentValueZone2 > -1 || currentValueZone3 > -1 || currentValueZone4 > -1)
                {//перед попыткой записать показания в базу нужно проверить, не получили ли мы ошибку "нулевых" показаний (-1 сменилось на 0 при анализе пакетов от концентратора)
                    if (currentValueZone0 == 0 || currentValueZone1 == 0 || currentValueZone2 == 0 || currentValueZone3 == 0 || currentValueZone4 == 0)
                    {//если хотя бы один из тарифов прислал 0, то пишем ошибку и не идём в хранимую процедуру обработки показаний 
                        details = "ошибка нулевых показаний! Такие показания не будут переданы в обработку. Следующие даты и показания хранятся в концентраторе: Т0=" + currentValueZone0.ToString() + "/" + this.lastDateZone0.ToString()
                                  + ", T1=" + currentValueZone1.ToString() + "/" + this.lastDateZone1.ToString() + ", T2=" + currentValueZone2.ToString() + "/" + this.lastDateZone2.ToString() + ", T3=" + currentValueZone3.ToString() + "/" + this.lastDateZone3.ToString() + ", T4=" + currentValueZone4.ToString() + "/" + this.lastDateZone4.ToString();
                        return details;
                    }
                    //правим те показания, которые остались в значении -1 чтобы получить 0 и в хранимой процедуре всё работало по-прежнему
                    if (currentValueZone0 == -1) { currentValueZone0 += 1; }
                    if (currentValueZone1 == -1) { currentValueZone1 += 1; }
                    if (currentValueZone2 == -1) { currentValueZone2 += 1; }
                    if (currentValueZone3 == -1) { currentValueZone3 += 1; }
                    if (currentValueZone4 == -1) { currentValueZone4 += 1; }

                    details = DataBaseManagerMSSQL.Create_EnergyPLC_Row(this.SerialNumber, currentValueZone0, currentValueZone1, currentValueZone2, currentValueZone3, currentValueZone4,
                                                                           this.lastDateZone0, this.lastDateZone1, this.lastDateZone2, this.lastDateZone3, this.lastDateZone4, packname, this.ID, this.clonemode);
                }
                
                if (this.clonemode == true || details[0] == 'П')//если счётчик клон или мы получили перерасход из процедуры создания строки показаний, то даты последних показаний в экземпляре нужно откатить, чтобы при полноценном опросе не вылетала ошибка дат 
                {
                    this.lastDateZone0 = lastDateZone0Tmp;
                    this.lastDateZone1 = lastDateZone1Tmp;
                    this.lastDateZone2 = lastDateZone2Tmp;
                    this.lastDateZone3 = lastDateZone3Tmp;
                    this.lastDateZone4 = lastDateZone4Tmp;
                }
                return details;//возвращаем строку с показаниями и датами либо ошибкой подключения к БД (ошибка -2)   
            }
            catch 
            {
                return "-1";
            }
        }
        //------------------далее идёт набор пустых методов, т.к. их наличия требует интерфейс ICounter. Для этого счётчика методы не делают ничего
        //------------------и никогда не вызываются------------------------------------------------------------------------------------------------
        public double ParseEnergyValue(DataProcessing dp, byte[] array, int offset)
        {
            return -1;
        }

        public DateTime ReadTime(DataProcessing dp, byte[] array, int offset, int d)
        {//процедура чтения данных, возвращающая время
            return Convert.ToDateTime("");
        }

        public double ParseMonitorValue(DataProcessing dp, byte[] array, string pname, int phaseNo, int offset)
        {
            return -1;
        }

        public string ParseParameterValue(byte[] array, string pname, int offset)
        {
            return "";
        }

        public byte[] FormTestArray(DataProcessing dp)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormPowerProfileArray(DataProcessing dp, byte RecordAddressHi, byte RecordAddressLow, byte MemoryNumber, byte BytesInfo)
        {
            byte[] a = new byte[0];
            return a;
        }

        public Bitmap DrawVectorDiagramm(PictureBox pixBox, RichTextBox richText)
        {

            Bitmap map = new Bitmap(0, 0);
            return map;
        }

        public byte[] FormParameterArray(DataProcessing dp, CounterParameterToRead param)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormMonitorArray(DataProcessing dp, CounterMonitorParameterToRead monitor)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormLastProfileRecordArray(DataProcessing dp)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormJournalArray(DataProcessing dp, CounterJournalToRead journal, int recNo)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormGainAccessArray(DataProcessing dp, byte lvl, byte pwd)
        {
            byte[] a = new byte[0];
            return a;
        }

        public byte[] FormEnergyArray(DataProcessing dp, CounterEnergyToRead energy, int zoneNo = 0)
        {
            byte[] a = new byte[0];
            return a;
        }

        public void LoadLastEnergyIntoEnergyList()
        {

        }

        public bool ValidateReadParameterAnswer(byte[] answerArray, byte commandCode = 0, int offset = 0)
        {
            return true;
        }

        public string ValidateWriteParameterAnswer(byte[] InArray, byte offset)
        {         
            return String.Empty;
        }

        public byte[] FormParamNewValueArray(List<FieldsValuesToWrite> formControlValuesList, string paramName, byte[] additional = null, char stringDivider = '/')
        {
            byte[] result = new byte[0];
            return result;
        }

        public byte[] FormParameterArray(DataProcessing dp, CounterParameterToWrite param)
        {
            byte[] OutBuf = new byte[1];
            return OutBuf;
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

        public void ReadEnergyOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {

        }
        public void ReadEnergyOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {

        }

        public void ReadJournalOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {

        }
        public void ReadJournalOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {

        }

        public void ReadParametersOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Modem modem
            //, WriteParameterToDeviceDelegate WriteDelegate = null
            )
        {

        }

        public void ReadParametersOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {

        }

        public bool GainAccessOnModem(DataProcessing dp, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait)//метод открытия канала к счётчику ЧЕРЕЗ МОДЕМ
        {
            return false;
        }

        public bool GainAccessOnGate(Mercury228 gate, DataProcessing dp, int packnum, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait)//метод открытия канала к счётчику ЧЕРЕЗ ШЛЮЗ
        {
            return false;
        }

        public void ReadMonitorOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ МОДЕМ
        {

        }

        public void ReadMonitorOnGate(Mercury228 gate, string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)//метод считывания параметров тока ЧЕРЕЗ ШЛЮЗ
        {

        }

        public DataTable GetPowerProfileForCounterOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent)
        {
            return null;
        }

        public DataTable GetPowerProfileForCounterOnGate(string workingPort, Mercury228 gate, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent, int packnum)
        {
            return null;
        }

        public void ReadJournalCQCOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText)
        {

        }

        public void ReadJournalCQCOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum)
        {

        }
    }
}






   

