using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
using System.IO.Ports;
using System.Text;

namespace NewProject
{
    public class DataProcessing
    {
        private System.Timers.Timer breakTimer = new System.Timers.Timer();
        private NotifyIcon ni;//значок в левом нижнем углу экрана
        public SerialPort sp;      

        public DataProcessing (SerialPort psp, NotifyIcon pni)
        {//перегрузка конструктора для серийного порта
            sp = psp;
            ni = pni;
        }

        public DataProcessing(string ip, string port, NotifyIcon pni)
        {//перегрузка конструктора для сокета
            ni = pni;
        }

        public SerialPort createSerialPort(string PortName, int rate)
        {
            try
            {
                //создаём новый объект порта
                sp = new SerialPort(PortName, rate, Parity.None, 8, StopBits.One);
                sp.DiscardNull = false;
                //задаём таймаут записи
                sp.WriteTimeout = 1000;
                Thread.Sleep(1000);//рекомендуется подождать некоторое время перед открытием порта, если перед этим был он был закрыт или уничтожен
                sp.Open();
                //очищаем входной и выходной буферы порта
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                sp.DtrEnable = false;
                sp.RtsEnable = true;
                return sp;
            }
            catch
            {               
                return sp;
            }
        }

        //первая перегрузка метода записи в порт - только строка
        public Exception SendData(string txt)
        {         
            try 
            {
              ni.Icon = Properties.Resources.ReadingDataNotifyMid;
              sp.DiscardInBuffer();//это здесь должно быть?? А  как же работа со шлюзом?
              sp.DiscardOutBuffer();
              Thread.Sleep(300);
              //подаём строковую команду
              sp.Write(txt);                                      
            }
            catch (Exception ex) 
            {
                ni.Icon = Properties.Resources.NotReadingDataIcon;
                return ex; 
            }
            ni.Icon = Properties.Resources.NotReadingDataIcon;
            return null;
        }
       
        //вторая перегрузка метода записи в порт - массив байт
        public Exception SendData(byte[] byte_array, int offset)
        {
            try
            {
                ni.Icon = Properties.Resources.ReadingDataNotifyMid;
                sp.DiscardInBuffer();//это здесь должно быть?? А  как же работа со шлюзом?
                sp.DiscardOutBuffer();                             
                //подаём массив байт
                sp.Write(byte_array, offset, byte_array.Length);
            }
            catch (Exception ex)
            {
                ni.Icon = Properties.Resources.NotReadingDataIcon;
                return ex;
            }
            ni.Icon = Properties.Resources.NotReadingDataIcon;
            return null;
        }

        private void StartTimer(int timeout)
        {
            sp.ReadTimeout = timeout;
            //описываем таймер, который будет следить за таймаутом операции
            breakTimer.Interval = timeout;//похоже здесь происходит "доступ к ликвидированному объекту". Почему? Годами этой ошибки не было
            breakTimer.Elapsed += timer_Elapsed;
            breakTimer.Enabled = true;
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            breakTimer.Enabled = false;
        }
        
        private bool WaitForData(int timeout)
        {//процедура ожидания факта наличия данных во входном буффере порта
            sp.ReadTimeout = timeout;
            //перезапускаем таймер
            breakTimer.Stop();
            breakTimer.Interval = timeout;
            breakTimer.Elapsed += timer_Elapsed;
            breakTimer.Start();                                  
            //пока нет байтов во входном буффере порта, приложение ждёт данные или пока выйдет таймаут
            do
            {
                Thread.Sleep(10);//задержка чтобы не перегружать процессор циклом
            }
            while ((sp.BytesToRead == 0) && (breakTimer.Enabled == true));       
            //если кол-во байт в буффере = 0 и таймер отключился, то выходим из процедуры т.к. ответа мы не дождались
            if ((sp.BytesToRead == 0) && (breakTimer.Enabled == false))
            {
                return false;
            }
            return true;
        }

        //первая перегрузка процедуры чтения из порта - возвращается только строка
        public string Read(string terminationString, int timeout)
        {
            ni.Icon = Properties.Resources.ReadingDataNotifyMid;
            //строка ответа
            StringBuilder answerStr = new StringBuilder();          
            //как только пошла инфа, считываем её, складывая в строку, до тех пор, пока не появится 
            //последовательность символов окончания сообщения от устройства
            int index = -1;
              //пытаемся читать данные 
                 try
                 {
                     StartTimer(timeout);
                     //т.к. процедура не вернула управление, значит данные есть в буффере
                     //перезапускаем таймер для ожидания нужной последовательности
                     breakTimer.Enabled = false;
                     breakTimer.Enabled = true;
                     ni.Icon = Properties.Resources.ReadingDataNotifyMid;
                     do
                     {                         
                         Thread.Sleep(10);//задержка чтобы не перегружать процессор циклом
                         string data = sp.ReadExisting();
                         answerStr.Append(data);
                         string bufferString = answerStr.ToString();
                         index = bufferString.IndexOf(terminationString);      
                     }
                     while ((index == -1) && (breakTimer.Enabled == true));
                 }
                 catch
                 {
                    answerStr.Append("ERROR\r");
                    sp.DiscardInBuffer();                   
                    ni.Icon = Properties.Resources.NotReadingDataIcon;
                    return answerStr.ToString();
                 }
                //если данные пошли, но искомая последовательность не была найдена за отведённое время,то пишем ошибку
                if ((index == -1) && (breakTimer.Enabled == false))
                {
                    answerStr.Append("ERROR\r");
                    sp.DiscardInBuffer();
                    ni.Icon = Properties.Resources.NotReadingDataIcon;
                    return answerStr.ToString();
                }

            sp.DiscardInBuffer();
            ni.Icon = Properties.Resources.NotReadingDataIcon;            
            return answerStr.ToString();             
        }

        public byte[] Read(int count, int timeout, bool discard)
        {   //процедура, ая определённая кол-во байт из порта
            //массив ответа
            byte[] answerArray = new byte[0];          
            //если не дождались ответа за отведённое время, то массив не меняется и выходим
            if (WaitForData(timeout) == false)
            {
                Array.Resize(ref answerArray, 5);
                answerArray = new byte[] { 0x0045, 0x0052, 0x0052, 0x004F, 0x0052 };
                sp.DiscardInBuffer();
                return answerArray;
            }
            //если функция ожидания возвращает истину пытаемся читать данные 
            try
            {
                //т.к. процедура не вернула управление, значит данные есть в буффере, перезапускаем таймер для ожидания необходимого количества байт
                breakTimer.Stop();
                breakTimer.Interval = timeout;
                breakTimer.Start();
                ni.Icon = Properties.Resources.ReadingDataNotifyMid;
                do
                {
                    Array.Resize(ref answerArray, answerArray.Length + 1);//увеличиваем максимальный размер результирующего массива
                    sp.Read(answerArray, answerArray.Length - 1, 1);//читаем очередной байт из буфера в конец результирующего массива
                }
                while (answerArray.Length < count && (breakTimer.Enabled == true));
            }
            catch
            {
                Array.Resize(ref answerArray, 5);
                answerArray = new byte[] { 0x0045, 0x0052, 0x0052, 0x004F, 0x0052 };
                sp.DiscardInBuffer();
                ni.Icon = Properties.Resources.NotReadingDataIcon;
                return answerArray;
            }
            //если данные пошли, но необходимое кол-во бай не пришло за отведённое время,то пишем ошибку
            if (answerArray.Length < count)
            {
                Array.Resize(ref answerArray, 5);
                answerArray = new byte[] { 0x0045, 0x0052, 0x0052, 0x004F, 0x0052 };
                sp.DiscardInBuffer();
                ni.Icon = Properties.Resources.NotReadingDataIcon;
                return answerArray;
            }

            if (discard == true)
            {
                sp.DiscardInBuffer();
                //sp.DiscardOutBuffer();
            }
            ni.Icon = Properties.Resources.NotReadingDataIcon;
            return answerArray;
        }
    
        private int findSubSet(byte[] setToFind, byte[] setToSearch, int startPos)
        {
            //процедура поиска подмножества setToFind в множестве setToSearch начиная с указанной позиции startPos
            //если начальная позиция поиска больше чем длина множества то выходим
            if (startPos > setToSearch.Length) { return -1; }
            //искомое подмножество по длине должно быть не больше множества
            if (setToFind.Length > setToSearch.Length) { return -1; }
            int j = startPos; int i;

            do
            {
                i = 0;//счётчик цикла по подмножеству. Вынесен наверх потому что его проверка должна быть в WHILE
                for (; i < setToFind.Length; i++)
                {
                    if (setToFind[i] != setToSearch[i + j])
                    {
                        j += 1;
                        break;
                    }
                }
            }
            while (i != setToFind.Length && j != setToSearch.Length);
            //если счётчик искомого подмножества дошел до предела, то это значит что подмножетсво было найдено
            if (i == setToFind.Length)
            {
                return j;
            }

            return -1;
        }

        public static List<string> ReturnAvailablePorts(RichTextBox rtb)
        {
        //формируем список доступных портов
        List<string> availablePorts = new List<string>();
        byte[] PortNumbers = new byte[0];//числовой массив номеров портов. Нужен для сортировки списка доступных портов (сортировки не по строке, а по числам)
            //Выполняем проход по массиву имен последовательных портов для текущего компьютера которые возвращает функция SerialPort.GetPortNames().
            DateTime currentDate = DateTime.Now;

            foreach (string portName in SerialPort.GetPortNames())
             {
                try
                {
                    rtb.Invoke(new Action(delegate
                    {                      
                        SerialPort Port = new SerialPort(portName);//Представляем ресурс последовательного порта.                      
                        {
                            Port.Open();//Открываем новое соединение последовательного порта.
                            Thread.Sleep(100);//эта строка предотвращает зависание              
                            //Выполняем проверку полученного порта
                            if (Port.IsOpen)
                            {
                                //Если порт открыт то добавляем его в список доступных портов
                                //Выделяем из строки число (номер порта) и добавляем в числовой массив (для будущей сортировки)
                                Array.Resize(ref PortNumbers, PortNumbers.Length + 1);
                                PortNumbers[PortNumbers.Length - 1] = Convert.ToByte(Port.PortName.Substring(3));
                                Port.Close();
                                Port = null;
                            }
                        }
                    }));
                }
            //Ловим все ошибки и отображаем, что открытых портов не найдено               
            catch (Exception ex)
                {
                    currentDate = DateTime.Now;
                    rtb.Invoke(new Action(delegate
                    {
                        rtb.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка при сканировании портов: " + ex.Message + "...\r");
                        rtb.ScrollToCaret();
                    }));
                    continue;
                }
            }
            Array.Sort(PortNumbers); //нужно отсортировать полученный список не по строке, а по номеру порта (числу)            
            foreach (byte pn in PortNumbers) availablePorts.Add("COM" + pn.ToString());//после того как отсортировали, нужно вновь составить список строк
            //возвращаем отсортированный список портов
            return availablePorts;
        }

        public void ClosePort()
        {
            sp.Close();
        }

        public bool ReturnDtrState()
        {
            return sp.DtrEnable;
        }

        public UInt16 ComputeCrc(byte[] data)
        {
            ushort[] CrcTable = {
             0X0000, 0XC0C1, 0XC181, 0X0140, 0XC301, 0X03C0, 0X0280, 0XC241,
             0XC601, 0X06C0, 0X0780, 0XC741, 0X0500, 0XC5C1, 0XC481, 0X0440,
             0XCC01, 0X0CC0, 0X0D80, 0XCD41, 0X0F00, 0XCFC1, 0XCE81, 0X0E40,
             0X0A00, 0XCAC1, 0XCB81, 0X0B40, 0XC901, 0X09C0, 0X0880, 0XC841,
             0XD801, 0X18C0, 0X1980, 0XD941, 0X1B00, 0XDBC1, 0XDA81, 0X1A40,
             0X1E00, 0XDEC1, 0XDF81, 0X1F40, 0XDD01, 0X1DC0, 0X1C80, 0XDC41,
             0X1400, 0XD4C1, 0XD581, 0X1540, 0XD701, 0X17C0, 0X1680, 0XD641,
             0XD201, 0X12C0, 0X1380, 0XD341, 0X1100, 0XD1C1, 0XD081, 0X1040,
             0XF001, 0X30C0, 0X3180, 0XF141, 0X3300, 0XF3C1, 0XF281, 0X3240,
             0X3600, 0XF6C1, 0XF781, 0X3740, 0XF501, 0X35C0, 0X3480, 0XF441,
             0X3C00, 0XFCC1, 0XFD81, 0X3D40, 0XFF01, 0X3FC0, 0X3E80, 0XFE41,
             0XFA01, 0X3AC0, 0X3B80, 0XFB41, 0X3900, 0XF9C1, 0XF881, 0X3840,
             0X2800, 0XE8C1, 0XE981, 0X2940, 0XEB01, 0X2BC0, 0X2A80, 0XEA41,
             0XEE01, 0X2EC0, 0X2F80, 0XEF41, 0X2D00, 0XEDC1, 0XEC81, 0X2C40,
             0XE401, 0X24C0, 0X2580, 0XE541, 0X2700, 0XE7C1, 0XE681, 0X2640,
             0X2200, 0XE2C1, 0XE381, 0X2340, 0XE101, 0X21C0, 0X2080, 0XE041,
             0XA001, 0X60C0, 0X6180, 0XA141, 0X6300, 0XA3C1, 0XA281, 0X6240,
             0X6600, 0XA6C1, 0XA781, 0X6740, 0XA501, 0X65C0, 0X6480, 0XA441,
             0X6C00, 0XACC1, 0XAD81, 0X6D40, 0XAF01, 0X6FC0, 0X6E80, 0XAE41,
             0XAA01, 0X6AC0, 0X6B80, 0XAB41, 0X6900, 0XA9C1, 0XA881, 0X6840,
             0X7800, 0XB8C1, 0XB981, 0X7940, 0XBB01, 0X7BC0, 0X7A80, 0XBA41,
             0XBE01, 0X7EC0, 0X7F80, 0XBF41, 0X7D00, 0XBDC1, 0XBC81, 0X7C40,
             0XB401, 0X74C0, 0X7580, 0XB541, 0X7700, 0XB7C1, 0XB681, 0X7640,
             0X7200, 0XB2C1, 0XB381, 0X7340, 0XB101, 0X71C0, 0X7080, 0XB041,
             0X5000, 0X90C1, 0X9181, 0X5140, 0X9301, 0X53C0, 0X5280, 0X9241,
             0X9601, 0X56C0, 0X5780, 0X9741, 0X5500, 0X95C1, 0X9481, 0X5440,
             0X9C01, 0X5CC0, 0X5D80, 0X9D41, 0X5F00, 0X9FC1, 0X9E81, 0X5E40,
             0X5A00, 0X9AC1, 0X9B81, 0X5B40, 0X9901, 0X59C0, 0X5880, 0X9841,
             0X8801, 0X48C0, 0X4980, 0X8941, 0X4B00, 0X8BC1, 0X8A81, 0X4A40,
             0X4E00, 0X8EC1, 0X8F81, 0X4F40, 0X8D01, 0X4DC0, 0X4C80, 0X8C41,
             0X4400, 0X84C1, 0X8581, 0X4540, 0X8701, 0X47C0, 0X4680, 0X8641,
             0X8201, 0X42C0, 0X4380, 0X8341, 0X4100, 0X81C1, 0X8081, 0X4040 };

            ushort crc = 0xFFFF;

            foreach (byte datum in data)
            {
                crc = (ushort)((crc >> 8) ^ CrcTable[(crc ^ datum) & 0xFF]);
            }

            return crc;
        }

        public long ComputeCrc2(byte[] data)
        {
            long CRC24_INIT = 0x00b704ce;
            long CRC24_POLY = 0x01864cfb;
            long crc;

            crc = CRC24_INIT;

            foreach (byte datum in data)
            {
                long temp = datum;
                temp = temp << 16;
                crc = crc ^ temp;

                for (int i = 0; i <= 7; i++)
                {
                    crc = crc << 1;
                    temp = crc & 0x01000000;
                    if (temp != 0)
                    {
                        crc = crc ^ CRC24_POLY;
                    }
                }             
            }
            crc = crc & 0x00ffffff;
            return crc;
        }

        public static DataTable SumUpProfile(DataTable dt, int count)
        {//здесь для суммирования профиля создаём новую таблицу
            DataTable dtNew = new DataTable("PowerProfileTable"); dtNew.Clear();
            try
            {
                //формируем таблицу аналогичную оригиналу
                DataColumn dcNew = new DataColumn("dummy1"); dtNew.Columns.Add(dcNew);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково
                dcNew = new DataColumn("dummy2"); dtNew.Columns.Add(dcNew);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково
                dcNew = new DataColumn("dummy3"); dtNew.Columns.Add(dcNew);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково
                 
                dcNew = new DataColumn("e_a_plus");  dcNew.DataType = System.Type.GetType("System.Double"); dtNew.Columns.Add(dcNew);
                dcNew = new DataColumn("e_a_minus"); dcNew.DataType = System.Type.GetType("System.Double"); dtNew.Columns.Add(dcNew);
                dcNew = new DataColumn("e_r_plus");  dcNew.DataType = System.Type.GetType("System.Double"); dtNew.Columns.Add(dcNew);
                dcNew = new DataColumn("e_r_minus"); dcNew.DataType = System.Type.GetType("System.Double"); dtNew.Columns.Add(dcNew);              
                dcNew = new DataColumn("date_time"); dcNew.DataType = System.Type.GetType("System.DateTime"); dtNew.Columns.Add(dcNew);
                dcNew = new DataColumn("period"); dcNew.DataType = System.Type.GetType("System.Double"); dtNew.Columns.Add(dcNew);
                //циклимся по записям
                for (int i = 0; i < dt.Rows.Count; i += count)
                {
                    double valAplus = 0; double valAminus = 0;
                    double valRplus = 0; double valRminus = 0;
                    //нужно убедиться что мы не пытаемся сложить значения типа DBNull
                    if (!Convert.IsDBNull(dt.Rows[i]["e_a_plus"]))  valAplus =  Convert.ToDouble(dt.Rows[i]["e_a_plus"]);
                    if (!Convert.IsDBNull(dt.Rows[i]["e_a_minus"])) valAminus = Convert.ToDouble(dt.Rows[i]["e_a_minus"]);
                    if (!Convert.IsDBNull(dt.Rows[i]["e_r_plus"]))  valRplus =  Convert.ToDouble(dt.Rows[i]["e_r_plus"]);
                    if (!Convert.IsDBNull(dt.Rows[i]["e_r_minus"])) valRminus = Convert.ToDouble(dt.Rows[i]["e_r_minus"]);

                    string datetime = dt.Rows[i + 1]["date_time"].ToString();

                    for (int j = 1 + i; j < count + i; j++)
                    {
                        double valAplusNext = 0; double valAminusNext = 0;
                        double valRplusNext = 0; double valRminusNext = 0;
                        //нужно убедиться что мы не пытаемся сложить значения типа DBNull
                        if (!Convert.IsDBNull(dt.Rows[j]["e_a_plus"]))  valAplusNext =   Convert.ToDouble(dt.Rows[j]["e_a_plus"]);
                        if (!Convert.IsDBNull(dt.Rows[j]["e_a_minus"])) valAminusNext =  Convert.ToDouble(dt.Rows[j]["e_a_minus"]);
                        if (!Convert.IsDBNull(dt.Rows[j]["e_r_plus"]))  valRplusNext =   Convert.ToDouble(dt.Rows[j]["e_r_plus"]);
                        if (!Convert.IsDBNull(dt.Rows[j]["e_r_minus"])) valRminusNext =  Convert.ToDouble(dt.Rows[j]["e_r_minus"]);
                        //присовокупляем значения
                        valAplus += valAplusNext;
                        valAminus += valAminusNext;
                        valRplus  += valRplusNext;
                        valRminus += valRminusNext;
                    }
                    DataRow dr = dtNew.NewRow(); //добавляем строку в таблицу
                    dr["e_a_plus"] = valAplus; dr["e_a_minus"] = valAminus;
                    dr["e_r_plus"] = valRplus; dr["e_r_minus"] = valRminus;
                    dr["date_time"] = datetime;
                    dtNew.Rows.Add(dr);                                        
                }
                return dtNew;
            }
            catch
            {             
                return dtNew;
            }
        }
    }
}
