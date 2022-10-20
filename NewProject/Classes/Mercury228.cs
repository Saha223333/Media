using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace NewProject
{
    public class Mercury228 : IDevice, IConnection, IReadable, IWritable
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IP { get; set; }
        public string Port { get; set; } = String.Empty;
        public int Channel { get; set; } //определяет канал связи (0 - GSM, 1 - Интернет)
        public int  AutoConfig; //определяет автоматическую настройку порта шлюза
        public string ConfigStr; //строка автонастройки шлюза (порта 1)
        public string CBST { get; set; }

        public BindingList<Mercury228ParametersToRead> ParametersToRead; //перечень параметров шлюза 228 для чтения
        public BindingList<Mercury228ParametersToWrite> ParametersToWrite { get; set; } //перечень параметров шлюза 228 для записи

        public Mercury228(int pid, string pname, string pphone, string pip, string pconfig_str, string pcbst)
        {
            ID = pid; ParentID = 0; Name = pname; Phone = pphone; IP = pip; ConfigStr = pconfig_str; CBST = pcbst;
            ParametersToRead = new BindingList<Mercury228ParametersToRead>();
            //заполняем список параметров
            {//запрос на чтение конфигурации
                byte[] b = new byte[1];
                b[0] = 0x0080;//запрос на чтение конфигурации. Он же - полезная нагрузка пакета
                ParametersToRead.Add(new Mercury228ParametersToRead("Конфигурация", b, true, 0, 13));//btw = 8 (заголовок) + кол-во байт полезной нагрузки ответа + 1(поле checksum)
            }
            {//запрос на чтение настроек порта 1
                byte[] b = new byte[1];
                b[0] = 0x0081;
                ParametersToRead.Add(new Mercury228ParametersToRead("Настройки порта 1", b, false, 0, 13));//btw = 8 (заголовок) + кол-во байт полезной нагрузки ответа + 1(поле checksum)
            }
            {//запрос на чтение настроек порта 2
                byte[] b = new byte[1];
                b[0] = 0x0082;
                ParametersToRead.Add(new Mercury228ParametersToRead("Настройки порта 2", b, false, 0, 13));//btw = 8 (заголовок) + кол-во байт полезной нагрузки ответа + 1(поле checksum)
            }
            //---------------------------------------------------------------------------------
            ParametersToWrite = new BindingList<Mercury228ParametersToWrite>();
            {//запрос на запись настроек порта №1
                byte[] b = new byte[1]; byte[] newValue = new byte[3];
                b[0] = 0x0001;
                ParametersToWrite.Add(new Mercury228ParametersToWrite("Настройки порта 1", b, false, newValue));
            }

            {//запрос на запись настроек порта №1
                byte[] b = new byte[1]; byte[] newValue = new byte[3];
                b[0] = 0x0002;
                ParametersToWrite.Add(new Mercury228ParametersToWrite("Настройки порта 2", b, false, newValue));
            }
        }

        public string ReadStr(byte[] array, string pname, int offset)
        {
            try
            {
                if (pname == "Конфигурация")
                {
                    string valueStr = String.Empty; string rssi = String.Empty;
                    //switch здесь не подходит, т.к. case не принимает множественные условия и диапазоны
                    if (array[10 + offset] == 0) { rssi = "-113 дБм, или меньше"; }
                    if (array[10 + offset] == 1) { rssi = "-111 дБм"; }
                    if ((array[10 + offset] >= 2) || (array[11] <= 30)) { rssi = "-109..-53 дБм"; }
                    if (array[10 + offset] == 31) { rssi = "-51 дБм, или больше"; }
                    if (array[10 + offset] > 31) { rssi = "Ошибочная величина"; }

                    valueStr = array[9].ToString("X") + " / " + rssi + " / " + array[11].ToString("X");
                    return valueStr;
                }
                if ((pname == "Настройки порта 1") || (pname == "Настройки порта 2"))
                {
                    string byteStr = String.Empty; string valueStr = String.Empty;
                    byteStr = Convert.ToString(array[9 + offset], 2).PadLeft(8, '0');//переводим байт ответа в двоичную строку для анализа. Смотрим поле UART в ответе
                    string astr = byteStr.Substring(4, 4);//выделяем необходимые биты из всего байта
                    byte a = Convert.ToByte(astr.PadLeft(4, '0'), 2);//конвертируем выделенные биты в число. Битовая скорость порта
                    string val = String.Empty;//значение параметра

                    switch (a) //смотрим что там в двух битах. Битовая скорость порта
                    {
                        case 1: { val = "300"; } break;
                        case 2: { val = "600"; } break;
                        case 3: { val = "1200"; } break;
                        case 4: { val = "2400"; } break;
                        case 5: { val = "4800"; } break;
                        case 6: { val = "9600"; } break;
                        case 7: { val = "14400"; } break;
                        case 8: { val = "19200"; } break;
                        case 9: { val = "28800"; } break;
                        case 10: { val = "38400"; } break;
                        case 11: { val = "57600"; } break;
                        case 12: { val = "115200"; } break;
                    }
                    valueStr += val;//присовокупляем полученное значение параметра

                    astr = byteStr.Substring(3, 1);//выделяем необходимые биты из всего байта
                    a = Convert.ToByte(astr);//конвертируем выделенные биты в число. Длина символа
                    switch (a)
                    {//длина символа
                        case 0: val = " / 7 бит данных "; break;
                        case 1: val = " / 8 бит данных "; break;
                    }
                    valueStr += val;

                    astr = byteStr.Substring(2, 1);//выделяем необходимые биты из всего байта
                    a = Convert.ToByte(astr);//конвертируем выделенные биты в число. Число стоп-битов
                    switch (a)
                    {//Число стоп-битов
                        case 0: val = " / 1 стоп-бит "; break;
                        case 1: val = " / 2 стоп-бит "; break;
                    }
                    valueStr += val;

                    astr = byteStr.Substring(0, 1);//выделяем необходимые биты из всего байта
                    a = Convert.ToByte(astr);//конвертируем выделенные биты в число. Проверка чётности
                    switch (a)
                    {//Проверка чётности
                        case 0: val = " / чёт. не пров. "; break;
                        case 1: val = " / чёт. пров. "; break;
                    }
                    valueStr += val;

                    if (a == 1)//если чётность проверяется, то смотрим бит чётности
                    {
                        astr = byteStr.Substring(1, 1);//выделяем необходимые биты из всего байта. Тип чётности
                        switch (a)
                        {//Тип чётности
                            case 0: val = " / чёт "; break;
                            case 1: val = " / нечет "; break;
                        }
                        valueStr += val;
                    }

                    byteStr = Convert.ToString(array[10 + offset], 2).PadLeft(8, '0');//переводим байт ответа в двоичную строку для анализа. Смотрим поле WAIT в ответе

                    double var1 = 8 * Convert.ToDouble(byteStr.Substring(4, 1));
                    double var2 = 4 * Convert.ToDouble(byteStr.Substring(5, 1));
                    double var3 = 2 * Convert.ToDouble(byteStr.Substring(6, 1));
                    double var4 = Convert.ToDouble(byteStr.Substring(7, 1));
                    double var5 = 2 * Convert.ToDouble(byteStr.Substring(2, 1));
                    double var6 = Convert.ToDouble(byteStr.Substring(3, 1));

                    double wait = (var1 + var2 + var3 + var4) * (Math.Pow(10, var5 + var6));
                    valueStr += " / т-аут " + wait.ToString() + " мс ";
                    return valueStr;
                }
            }
            catch
            {
                return "Ошибка";
            }
            return "Ошибка";
        }

        public bool TestCounter(ICounter counter, DataProcessing dp, int packnum, RichTextBox richText, ref BackgroundWorker worker)
        {   //процедура проверки связи со счётчиком
            try {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                 {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тест связи с " + counter.Name + "\r");
                    richText.ScrollToCaret();
                 }));
                byte[] testOutBuf = FormPackage(counter.FormTestArray(dp), 1, dp, packnum); //вызываем процедуру формирования тестового массива        
                Exception ex = dp.SendData(testOutBuf, 0); //посылаем запрос
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
                byte[] answerArray = dp.Read(4 + 8, 10000, true);

                if ((answerArray[8] != counter.NetAddress) || (answerArray.Length == 5)) //первый байт ответа после заголовка пакета всегда должен быть сетевой вдрес счётчика
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                     {
                       richText.SelectionColor = Color.Red;
                       richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                       richText.ScrollToCaret();
                     }));
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
                return false;
            }
        }

        public byte[] FormPackage(byte[] payload, byte port, DataProcessing dp, int packnum)
        {
            byte[] Header = new byte[5];//заголовок без контрольной суммы
            byte[] Package = new byte[9 + payload.Length];//инициализируем итоговый массив сформированного пакета (длина = 8 (заголовок) + кол-во байт полезной нагрузки запроса + 1(поле checksum)
            //если параметр отмечен, то нужно сформировать пакет для него и поместить в окно перед отправкой
            //формируем номер пакета
            string PackNumStr = packnum.ToString("X").PadLeft(4, '0');//преобразуем номер пакета в 16-ричную строку для последующего разбиения на байты
            byte PackNumHi = Convert.ToByte(PackNumStr.Substring(0, 2), 16);//старший байт номера пакета для шлюза
            byte PackNumLow = Convert.ToByte(PackNumStr.Substring(2, 2), 16);//младший байт номера пакета для шлю         
            Header[0] = PackNumLow;//поле num младший байт
            Header[1] = PackNumHi;//поле num старший байт
            string lenStr = payload.Length.ToString("X").PadLeft(4, '0');//преобразуем длину полезной нагрузки пакета в 16-ричную строку для последующего разбиения на байты
            byte lenHi = Convert.ToByte(lenStr.Substring(0, 2), 16);//старший байт длины полезной нагрузки
            byte lenLow = Convert.ToByte(lenStr.Substring(2, 2), 16);//младший байт длины полезной нагрузки       
            Header[2] = lenLow;//поле len младший байт
            Header[3] = lenHi;//поле len старший байт
            Header[4] = Convert.ToByte(port);//поле port
            long crc = dp.ComputeCrc2(Header); //контрольная сумма заголовочной части пакета
            string crcStr = crc.ToString("X").PadLeft(6, '0');//переводим контрольную сумму в 16-ричную строку
            //формируем массив пакета прежде чем добавить его в итоговый буффер пакетов                                                  
            Package[0] = Convert.ToByte(crcStr.Substring(4, 2), 16);//контрольная сумма
            Package[1] = Convert.ToByte(crcStr.Substring(2, 2), 16);
            Package[2] = Convert.ToByte(crcStr.Substring(0, 2), 16);
            Package[3] = Header[0];//поле num младший байт
            Package[4] = Header[1];//поле num старший байт
            Package[5] = Header[2];//поле len младший байт
            Package[6] = Header[3];//поле len старший байт
            Package[7] = Header[4];//поле port
            Array.Copy(payload, 0, Package, 8, payload.Length);//копируем массив полезной нагрузки в результирующий массив пакета
            Package[8 + payload.Length] = (byte)CalcCheckSum(payload);// Младший байт суммы всех байтов поля PAYLOAD минус 1
            //проверяем не привышает ли размер текущего пакета 274 байта (ограничение ПО шлюза)
            if (Package.Length > 274)//если да, то отрезаем лишнее
            {
                Array.Resize(ref Package, 274);
            }
            return Package;
        }

        public void ReadGateParameters(DataProcessing dp, ToolStripProgressBar pb, int packnum, RichTextBox richText, ref BackgroundWorker worker)
        {
            //делаем запрос только на те параметры, которые отмечены
            var checkedParameters = from param in this.ParametersToRead where param.check == true select param;
            if (checkedParameters.Count<Mercury228ParametersToRead>() == 0)
            {
                return;
            }
            //процедура чтения параметров шлюза         
            byte[] OutBuf = new byte[0];
            int PackNumInt = packnum;//начальный номер пакета для шлюза        
            List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов) 
            int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза                   
            DateTime currentDate = DateTime.Now;

            richText.Invoke(new Action(delegate
            {
               richText.AppendText(currentDate + "." + currentDate.Millisecond + " Чтение параметров шлюз " + this.Name + "...\r");
               richText.ScrollToCaret();
            }));
            //циклимся по параметрам шлюза
            //цикл по отмеченным параметрам
            foreach (var param in checkedParameters)
            {
                byte[] Package = FormPackage(param.bytes, param.port, dp, PackNumInt);
                PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов                                
                PackNumInt += 1;//наращиваем номер пакета
                CountToWait += param.bytesToWait;
            }
            //наконец формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
            //составляем его из окна пакетов       
            foreach (byte[] pack in PackagesBuffer)
            {
                Array.Resize(ref OutBuf, OutBuf.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                Array.Copy(pack, 0, OutBuf, OutBuf.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
            }
            //после того, как сформировали серию пакетов, отправляем её на порт и ждём ответов
            Exception ex = dp.SendData(OutBuf, 0);//посылаем запрос
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
            byte[] answerArray = dp.Read(CountToWait, 15000, true);//ждём ответ. Ожидаемая длина складывается из поля BytesToWait всех параметров (суммы длин всех пакетов в ответе)

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
            //теперь нужно разобрать ответ и оформить
            //опять циклимся по параметрам, чтобы выделить те ответы из общего потока, которых мы отметили ранее и ждали
            int offset = 0;//отступ вглубь общего массива ответа
            foreach (var param in checkedParameters)
            {//если параметр был отмечен галочкой, значит мы ищем ответ на него в общем потоке                                
             //Т.к. ответы приходят в том же порядке, в котором уходят запросы, то можно смотреть подряд
                byte[] curAnswer = new byte[param.bytesToWait];//отсчитываем в общем потоке то кол-во байт, которое должно содержаться в ответе на конкретный параметр
                Array.Copy(answerArray, offset, curAnswer, 0, param.bytesToWait);//помещаем искомый фрагмент в отдельный массив с учётом отступа вглубь общего массива ответа
                param.value = ReadStr(curAnswer, param.name, 0);//отправляем ответ на разбор и возвращаем ответ
                if (param.value == "Ошибка")
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                       {
                           richText.SelectionColor = Color.Red;
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " неудачно\r");
                           richText.ScrollToCaret();
                       })); continue;
                }
                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                richText.Invoke(new Action(delegate
                {
                    richText.SelectionColor = Color.DarkGreen;
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + " удачно\r");
                    richText.ScrollToCaret();
                }));
                offset += param.bytesToWait;//наращиваем отступ вглубь общего массива ответа
            }
        }

        public void WriteParametersToDevice(TreeNode node, DataProcessing dp, 
            RichTextBox richText, BackgroundWorker worker, List<FieldsValuesToWrite> ValuesList, List<FieldsValuesToWrite> list = null)
        {//процедура, выполняющая запись параметров в выбранное устройство (например шлюз или концетратор)
         //в зависимости от выбранного устройства выбирается порт. Шлюз настраивается по порту 0, а концентратор по порту 1
            if (!(node.Tag is IWritable)) return;
            if (node.Tag.GetType() == typeof(Mercury228))//если шлюз
            {
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                Mercury228 gate = (Mercury228)node.Tag;
                var checkedParameters = from param in gate.ParametersToWrite where param.check == true select param;
                if (checkedParameters.Count<Mercury228ParametersToWrite>() == 0) { return; }

                DateTime currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                     richText.AppendText(currentDate + "." + currentDate.Millisecond + " Работаем со шлюзом" + gate.Name + "...\r");
                     richText.ScrollToCaret();
                }));

                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                       {
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись параметров порта №1" + gate.Name + "...\r");
                           richText.ScrollToCaret();
                       }));
                int packnum = 0;//номер пакета для шлюза
                byte[] OutBuf = null; //инициализируем результирующий массив запроса

                foreach (Mercury228ParametersToWrite param in checkedParameters)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "...\r");
                            richText.ScrollToCaret();
                        }));

                    FieldsValuesToWrite field = list.Find(x => x.name == param.name);//ищем в списке значений текущий параметр(который планируем записать)
                    string value = field.value;//запоминаем в строке новое значение
                    param.FormParamNewValueArray(value);  //подаём новое значение параметра на разбор в процедуру для формирования массива нового значения
                    //в итоге получаем массив param.value
                    //временный массив, включающий в себя массив param.bytes и массив param.value одновременно
                    byte[] ResultParamArray = new byte[4];//понадобился, потому что у шлюза нет процедуры формирования массива для записи параметра как
                     //у концентратора (concentrator.FormParameterArray) ибо параметров мало и нет смысла в таковой
                    Array.Copy(param.bytes, 0, ResultParamArray, 0, param.bytes.Length);//включаем во временный массив запрос
                    Array.Copy(param.value, 0, ResultParamArray, param.bytes.Length, param.value.Length); //включаем во временный массив новое значение
                    OutBuf = FormPackage(ResultParamArray, 0, dp, packnum); //формируем пакет для шлюза
                    Exception ex = dp.SendData(OutBuf, 0);//отправляем
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
                    //ждём ответ
                    //длина ответа = длина нового значения + длина запроса + заголовок и хвост пакета
                    if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                    byte[] answerArray = dp.Read(param.value.Length + param.bytes.Length + 9, 10000, true);

                    if (answerArray.Length == 5)//если длина ответа слишком мала, то так не пойдёт
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
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.DarkGreen;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                            richText.ScrollToCaret();
                        }));
                    packnum += 1;
                }
            }//конец ветвления по шлюзу

            if (node.Tag.GetType() == typeof(Mercury225PLC1))//если концентратор
            {
                Mercury225PLC1 concentrator = (Mercury225PLC1)node.Tag;
                //делаем запрос только на те параметры, которые отмечены
                var checkedParameters = from param in concentrator.ParametersToWrite where param.check == true select param;
                if (checkedParameters.Count<Mercury225PLC1ParametersToWrite>() == 0)
                {
                    return;
                }

                DateTime currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Работаем с концентратором " + concentrator.NetAddress.ToString() + "...\r");
                            richText.ScrollToCaret();
                        }));
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                       {
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись параметров концентратора " + concentrator.NetAddress.ToString() + "...\r");
                           richText.ScrollToCaret();
                       }));
                int packnum = 0;
                byte[] OutBuf = null; //инициализируем результирующий массив запроса

                //цикл по отмеченным параметрам
                foreach (Mercury225PLC1ParametersToWrite param in checkedParameters)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                       {
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "...\r");
                           richText.ScrollToCaret();
                       }));
                    ////сначала выполняем запрос на выполнение одной защищенной команды, т.е. подаём пароль на концентратор
                    //OutBuf =  FormPackage(concentrator.FormParameterArray(dp, concentrator.ParametersToWrite[0]), 1, dp, packnum);
                    //dp.SendData(OutBuf, 0);
                    //byte[] answerArray = dp.Read(param.newValue.Length + 1,10000,true);
                    FieldsValuesToWrite field = list.Find(x => x.name == param.name);//ищем в списке значений текущий параметр (который плаируем записать)
                    string value = field.value;
                    param.FormParamNewValueArray(value);//подаём новое значение параметра на разбор в процедуру

                    OutBuf = FormPackage(concentrator.FormParameterArray(dp, param), 1, dp, packnum);
                    Exception ex = dp.SendData(OutBuf, 0);
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
                    byte[] answerArray = dp.Read(param.value.Length + param.bytes.Length + 9 + 9, 10000, true);

                    if (answerArray.Length == 5)
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
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                       {
                           richText.SelectionColor = Color.DarkGreen;
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                           richText.ScrollToCaret();
                       }));
                    packnum += 1;
                }
            }//конец ветвления по концентратору

            if (node.Tag is ICounter)//если счётчик
            {
                ICounter counter = (ICounter)node.Tag;
                //делаем запрос только на те параметры, которые отмечены
                var checkedParameters = from param in counter.ParametersToWrite where param.check == true select param;
                if (checkedParameters.Count() == 0)
                {
                    DateTime currentDateA = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDateA + "." + currentDateA.Millisecond + " Ни одного параметра счётчика на запись не обнаружено!\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                DateTime currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Работаем со счётчиком " + counter.NetAddress.ToString() + "...\r");
                    richText.ScrollToCaret();
                }));
                int packnum = 0;
                packnum += 1;//наращиваем номер пакета
                if (counter.GainAccessOnGate(this, dp, packnum, richText, 2, 2, ref worker, 4) == false)
                {
                    return;
                }

                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись параметров счётчика " + counter.NetAddress.ToString() + "...\r");
                    richText.ScrollToCaret();
                }));
                
                packnum += 1;//наращиваем номер пакета
                byte[] OutBuf = null; //инициализируем результирующий массив запроса

                //цикл по отмеченным параметрам
                foreach (CounterParameterToWrite param in checkedParameters)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "...\r");
                        richText.ScrollToCaret();
                    }));

                    param.value = counter.FormParamNewValueArray(ValuesList, param.name, param.additional);//подаём новое значение параметра на разбор в процедуру

                    OutBuf = FormPackage(counter.FormParameterArray(dp, param), 1, dp, packnum);
                    Exception ex = dp.SendData(OutBuf, 0);
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
                    byte[] answerArray = dp.Read(13, 5000, true);

                    string answerStrError = counter.ValidateWriteParameterAnswer(answerArray, 8);//вызываем процедуру проверки ответа от счётчика

                    if (answerStrError != String.Empty)//если сообщение ошибки не пустое
                    {
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new Action(delegate
                        {
                            richText.SelectionColor = Color.Red;
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + answerStrError + "\r");
                            richText.ScrollToCaret();
                        }));
                        continue;
                    }
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.DarkGreen;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                        richText.ScrollToCaret();
                    }));
                    packnum += 1;
                }
            }//конец ветвления по счётчику
        }

        public byte[] PortConfigArray(byte portNum)
        {//процедура формирования массива конфигурации шлюза (порта 1) с использованием строки автоконфигурации
            byte[] result = new byte[4];

            result[0] = portNum;
            result[1] = Convert.ToByte(this.ConfigStr.Substring(0, 2), 16);
            result[2] = Convert.ToByte(this.ConfigStr.Substring(2, 2), 16);
            result[3] = Convert.ToByte(this.ConfigStr.Substring(4, 2), 16);

            return result;
        }

        public void GatherDevicesData(string portname, TreeNodeCollection childNodes, DataProcessing dp, ToolStripProgressBar pb, DateTime ndate, DateTime kdate,
                                      int countSumm, bool GetProfile, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsentProfile)
        {   //процедура считывания данных с устройств, подключенных к шлюзу (групповой опрос)        
            int packnum = 0;//инициализируем нумерацию пакетов
            byte[] Package = null;
            byte[] answerArray = null;
            DateTime currentDate;
            //============================================================================================================================================
            //сначала нужно записать настройки порта№1 в шлюз (автоконфигурация)--------------------------------------------------------------------------            
            {
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                if (ConfigStr == null || ConfigStr.Length != 6)
                {
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Автоконфигурация включена, но нет строки автоконфигурации для шлюза " + this.Name + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }
                currentDate = DateTime.Now;

                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Автоконфигурация порта №1...\r");
                    richText.ScrollToCaret();
                }));
                //формируем пакет для записи настроек в порт шлюза
                Package = this.FormPackage(PortConfigArray(1), 0, dp, packnum);
                Exception ex = dp.SendData(Package, 0);//посылаем запрос
                if (ex != null)
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Возникло исключение при автоконфигурации порта шлюза: " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                    return;
                }

                if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                answerArray = dp.Read(ParametersToWrite[0].bytes.Length + ParametersToWrite[0].value.Length + 9, 3000, true);//ждём ответ

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
            }         
            //==============================================================================================================================================
            packnum = +1;
            this.ReadGateParameters(dp, pb, packnum, richText, ref worker);//сначала считаем параметры шлюза

            richText.Invoke(new Action(delegate
            {
                pb.Value = 0;//обнуляем прогресс бар
                pb.Maximum = childNodes.Count; //количество сегментов прогресс бара = количество дочерних узлов  
            }));
            //цикл по дочерним узлам шлюза               
            foreach (TreeNode node in childNodes)
            {
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                //проверяем какой тип дочернего устройства
                if (!(node.Tag is IReadable)) { continue; }
                if (node.Tag is ICounter)
                {
                    ICounter counter = (ICounter)node.Tag;
     
                    packnum += 1;//наращиваем номер пакета
                    //получение доступа (открытие канала)
                    if (counter.GainAccessOnGate(this, dp, packnum, richText, 1, 1, ref worker, 4) == false) continue;

                    ReadParameters://метка поставлена для более удобного чтения кода
                    {//считаем параметры----------------------------------------------------------------------------------------------------------------------------------------------------
                        var checkedParameters = from param in counter.ParametersToRead where param.check == true select param;
                        if (checkedParameters.Count<CounterParameterToRead>() == 0)
                        {//если ничего не отмечено идём на энергию
                            goto ReadEnergy;
                        }
                        //непосредственно сама процедура считывания параметров со счётчика
                        counter.ReadParametersOnGate(dp, worker, richText, this, packnum);
                    }
              
                    ReadEnergy:
                    {//считываем энергию----------------------------------------------------------------------------------------------------------------------------------------------------
                        //делаем запрос только на те параметры, которые отмечены
                        var checkedEnergy = from energy in counter.EnergyToRead where energy.check == true select energy;
                        if (checkedEnergy.Count<CounterEnergyToRead>() == 0)
                        {//если ничего не отмечено, то идём на журнал      
                            goto ReadJournal;
                        }
                        //непосредственно сама процедура считывания энергии со счётчика
                        counter.ReadEnergyOnGate(dp, worker, richText, this, packnum);
                    }

                    ReadJournal:
                    {//считываем журнал----------------------------------------------------------------------------------------------------------------------------------------------------
                        //делаем запрос только на те параметры, которые отмечены
                        var checkedJournal = from journal in counter.JournalToRead where journal.check == true select journal;
                        if (checkedJournal.Count<CounterJournalToRead>() == 0)
                        {//если ничего нет, то идём на профиль
                            goto ReadCQCJournal;
                        }
                        //непосредственно процедура чтения журнала
                        counter.ReadJournalOnGate(dp, worker, richText, this, packnum);
                    }

                    ReadCQCJournal:
                    {//считываем журнал ПКЭ----------------------------------------------------------------------------------------------------------------------------------------------------                  
                        //делаем запрос только на те параметры, которые отмечены
                        var checkedJournal = from journal in counter.JournalCQCToRead where journal.check == true select journal;
                        if (checkedJournal.Count<CounterJournalCQCToRead>() == 0)
                        {//если ничего нет, то идём на профиль
                            goto ReadProfile;
                        }
                        //непосредственно процедура чтения журнала
                        counter.ReadJournalCQCOnGate(dp, worker, richText, this, packnum);
                    }

                    //после всего снимаем профиль если стоит галочка
                    ReadProfile:
                    if (GetProfile == true)
                    {
                        GetPowerProfileForCounter(portname, counter, dp, pb, ndate, kdate, countSumm, crl, lrl, richText, worker, ReReadOnlyAbsentProfile);
                    }                                              
                }//конец счётчика
                  
                    //работаем с концентратором
                    if (node.Tag.GetType() == typeof(Mercury225PLC1))
                        {
                            Mercury225PLC1 concentrator = (Mercury225PLC1)node.Tag;
                            currentDate = DateTime.Now;

                            richText.Invoke(new Action(delegate
                               {
                                   richText.AppendText(currentDate + "." + currentDate.Millisecond + " Работаем с концентратором " + concentrator.NetAddress.ToString() + "...\r");
                                   richText.ScrollToCaret();
                               }));

                            currentDate = DateTime.Now;

                            richText.Invoke(new Action(delegate
                               {
                                   richText.AppendText(currentDate + "." + currentDate.Millisecond + " Чтение параметров концентратора " + concentrator.NetAddress.ToString() + "...\r");
                                   richText.ScrollToCaret();
                               }));
                            byte[] OutBuf = null; //инициализируем результирующий массив запросов 
                                                  //параметры концентратора
                                                  //делаем запрос только на те параметры, которые отмечены
                            var checkedParameters = from param in concentrator.ParametersToRead where param.check == true select param;
                            if (checkedParameters.Count<Mercury225PLC1ParametersToRead>() == 0)
                            {//если ничего нет - идём на счётчики
                                goto ReadLastMonth;
                            }
                            //цикл по отмеченным параметрам                                 
                            foreach (Mercury225PLC1ParametersToRead param in checkedParameters)
                            {
                                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                                if (param.check == true)
                                {
                                    packnum += 1;
                                    currentDate = DateTime.Now;

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "\r");
                                        richText.ScrollToCaret();
                                    }));
                                    int CountToWait = 0;
                                    OutBuf = FormPackage(concentrator.FormParameterArray(dp, param), 1, dp, packnum);//формируем запрос к концентратору (читаем его параметры)
                                    Exception ex = dp.SendData(OutBuf, 0);//посылаем запрос
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
                                    answerArray = dp.Read(8, 10000, false);//ждём данные. Сначала читаем заголовок пакета чтобы узнать, сколько байт нам нужно принять

                                    if (answerArray.Length == 5)
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

                                    CountToWait = Convert.ToInt16(answerArray[6].ToString("X").PadLeft(2, '0') + answerArray[5].ToString("X").PadLeft(2, '0'), 16); //смотрим сколько байт информации в пакете мы хотим вытащить

                                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                                    answerArray = dp.Read(CountToWait, 10000, true);

                                    if (answerArray.Length == 5)
                                    {
                                        param.value = "Ошибка";
                                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                        richText.Invoke(new Action(delegate
                                        {
                                            richText.SelectionColor = Color.Red;
                                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                                            richText.ScrollToCaret();
                                        }));
                                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                                        continue;
                                    }
                                    param.value = concentrator.ReadStr(answerArray, param.name, 8);//читаем значения параметров
                                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                                        richText.ScrollToCaret();
                                    }));
                                }
                            }

                            //смотрим, какой размер сети у концентратора. Если 16, значит настройки слетели
                            if (concentrator.NetSize == 16)
                            {//предупреждаем об этом
                                currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                richText.Invoke(new Action(delegate
                                {
                                    richText.SelectionColor = Color.Orange;
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " ВНИМАНИЕ! РАЗМЕР СЕТИ КОНЦЕНТРАТОРА СБРОСИЛСЯ ДО 16!\r");
                                    richText.ScrollToCaret();
                                }));
                                //пишем ошибку в базу
                                SqlException sqlex = DataBaseManagerMSSQL.Create_Error_Row(3, concentrator.Name, concentrator.ID);
                                if (sqlex != null)
                                {
                                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                    richText.Invoke(new Action(delegate
                                    {
                                        richText.SelectionColor = Color.Red;
                                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи в базу " + sqlex.Message + "\r");
                                        richText.ScrollToCaret();
                                    }));
                                }
                            }

                            ReadLastMonth:
                            //чтение последних месяцев концентратора для привязанных к нему счетчиков
                            {
                                if (node.Nodes.Count == 0) continue;//если у концентратора нет детей, идём на следующий узел (например концентратор)
                                currentDate = DateTime.Now;

                                richText.Invoke(new Action(delegate
                                {
                                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Чтение буфера последних месяцев концентратора " + concentrator.NetAddress.ToString() + "...\r");
                                    richText.ScrollToCaret();
                                }));
                                //для начала разбиваем счётчики концентратора на группы чтобы шлюз успевал отработать все пакеты
                                List<TreeNode[]> groups = new List<TreeNode[]>();//группы счётчиков              
                                int iters = 1;//кол-во итераций разбиения
                                int s2 = node.Nodes.Count;//общее количество счётчиков в концентраторе
                                int countersPerPackage = 20;//макс кол-во счётчиков в одном пакете 
                                                            //int countersPerPackage = 1;
                                int s3 = 0;//фактическое кол-во счётчиков в пакете
                                if (s2 > countersPerPackage)//если общее количество счётчиков концентратора больше чем максимальное кол-во сч-ков в пакете, то кол-во итераций нужно пересчитать
                                {
                                    iters = s2 / countersPerPackage;//считаем кол-во итераций 
                                    if (s2 % countersPerPackage > 0) iters += 1; //если от деления есть остаток - накидываем ещё одну итерацию
                                }

                                int k = 0;
                                //собираем счётчики в группы
                                for (int s = 0; s <= iters - 1; s++)
                                {
                                    TreeNode[] countersgroup = new TreeNode[0];//группа счётчиков                          
                                    if (s2 >= countersPerPackage) s3 = countersPerPackage; else s3 = s2;
                                    for (int j = 0; j <= s3 - 1; j++)
                                    {
                                        //нужно добавить счётчики в группы
                                        Array.Resize(ref countersgroup, countersgroup.Length + 1);
                                        countersgroup[j] = node.Nodes[j + k + s * 10];
                                        //countersgroup[j] = node.Nodes[s];
                                        s2 -= 1;
                                    }
                                    groups.Add(countersgroup);//добавляем группу счётчиков в список групп
                                    k += 10;
                                }
                                //Task[] tasks = new Task[groups.Count];//массив заданий для каждой группы счётчиков
                                //циклимся по списку групп счётчиков
                                foreach (TreeNode[] group in groups)
                                {
                                    //для каждоый группы счётчиков создаём своё задание
                                    //tasks[groups.IndexOf(group)] = new Task(() => { });

                                    byte[] OutBufLastMonth = new byte[0]; //инициализируем результирующий массив запросов  
                                    List<byte[]> PackagesBuffer = new List<byte[]>();//окно пакетов (коллекция массивов)    

                                    foreach (TreeNode childNode in group)
                                    {//циклимся по дочерним PLC1-счётчикам
                                        packnum += 1;
                                        MercuryPLC1 counterPLC = (MercuryPLC1)childNode.Tag;

                                        if (node.Text[node.Text.Length - 1] == 'K') counterPLC.clonemode = true;//если маркер клонирования на концентраторе присутствует, то выставляем счётчику режим клонирования
                                        else counterPLC.clonemode = false;//иначе его показания нужно обрабатывать и записывать в базу (если при этом он не находится в виртуальном режиме)

                                        Package = FormPackage(concentrator.FormEnergyArrayForCounter(dp, childNode, concentrator.NetAddress), 1, dp, packnum);//формируем запрос к дочернему PLC-счётчику
                                        PackagesBuffer.Add(Package);//добавляем сформированный пакет в окно пакетов           
                                    }
                                    //формируем результирующий массив, который пойдёт на порт компа и потом попадёт в шлюз  
                                    //составляем его из окна пакетов       
                                    foreach (byte[] pack in PackagesBuffer)
                                    {
                                        Array.Resize(ref OutBufLastMonth, OutBufLastMonth.Length + pack.Length);//корректируем размер итого массива исходя из длины очередного пакета
                                        Array.Copy(pack, 0, OutBufLastMonth, OutBufLastMonth.Length - pack.Length, pack.Length); //помещаем очередной пакет из окна пакетов в конец результирующего массива
                                    }
                                    Exception ex = dp.SendData(OutBufLastMonth, 0);//посылаем запрос
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
                                    //даём шлюзу время ответить
                                    int time = (group.Count() * 100) + 2000;
                                
                                    //читаем ответы
                                    foreach (TreeNode childNode in group)
                                    {
                                        MercuryPLC1 counterPLC = (MercuryPLC1)childNode.Tag;

                                        if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                                        answerArray = dp.Read(16, time, false);//Сначала читаем заголовки пакетов (шлюза и конц-ра в нём) чтобы узнать, сколько байт нам нужно принять

                                        if ((answerArray.Length == 5))
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.Red;
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counterPLC.Name + " (" + counterPLC.NetAddress + ") неудачно\r");
                                                richText.ScrollToCaret();
                                            }));
                                            // answerArray = dp.Read(1, 1000, false);//избавляемся от одного лишнего байта в буффере порта (контрольной сумма пакета) для продолжения считывания информации по другим счётчикам
                                            continue;
                                        }

                                        int CountToWait = 0;//кол-во байт, которые мы в итоге хотим увидеть в ответе от шлюза
                                        CountToWait = Convert.ToInt16(answerArray[6].ToString("X").PadLeft(2, '0') + answerArray[5].ToString("X").PadLeft(2, '0'), 16); //смотрим сколько байт информации в пакете мы хотим вытащить

                                        if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                                        answerArray = dp.Read(CountToWait - 8, time, false);//читаем байт сколько нужно минус первый заголовок пакета концентратора

                                        if ((answerArray.Length == 5) || (CountToWait == 10)) //если в пакете 10 байт - это значит что данных на текущий счётчик в концентраторе нет
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.Red;
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counterPLC.Name + " (" + counterPLC.NetAddress + ") неудачно\r");
                                                richText.ScrollToCaret();
                                            }));

                                            if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                                            answerArray = dp.Read(1, time, false);//избавляемся от одного лишнего байта в буффере порта (контрольной сумма пакета) для продолжения считывания информации по другим счётчикам

                                            continue;
                                        }
                                        string result = counterPLC.ReadEnergyPLC(dp, answerArray, 0);

                                        if (result == "-2")//вернулась ошибка разрыва связи с БД
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.Red;
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " ОБРЫВ СОЕДИНЕНИЯ С БАЗОЙ ДАННЫХ!!!\r");
                                                richText.ScrollToCaret();
                                            }));
                                            continue;
                                        }

                                        if (result == "-1")//читаем энергию и раскидываем по тарифам. Если вернулась строка -1, то значит произошёл сбой
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.Red;
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counterPLC.Name + " (" + counterPLC.NetAddress + ") неудачно\r");
                                                richText.ScrollToCaret();
                                            }));

                                            if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                                            answerArray = dp.Read(1, time, false);//избавляемся от одного лишнего байта в буффере порта (контрольной сумма пакета) для продолжения считывания информации по другим счётчикам

                                            continue;
                                        }
                                        else//иначе всё прошло нормально
                                        {
                                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                                            richText.Invoke(new Action(delegate
                                            {
                                                richText.SelectionColor = Color.DarkGreen;
                                                if (result[0] == 'П') richText.SelectionColor = Color.Red;//если пошёл перерасход, то подсвечиваем красным
                                                if (result[0] == 'д') richText.SelectionColor = Color.Red;//если дата новых показаний меньше или равна дате последних показаний, то подсвечиваем красным
                                                if (result[0] == 'о') richText.SelectionColor = Color.Red;//ошибка нулевых показаний, подсвечиваем красным
                                                if (result[0] == 'Б') richText.SelectionColor = Color.Red;//ошибка большого интервала времени между показаниями , подсвечиваем красным
                                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counterPLC.Name + " (" + counterPLC.NetAddress + "): " + result + "\r");
                                                richText.ScrollToCaret();
                                            }));
                                        }
                                        
                                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                                        answerArray = dp.Read(1, time, false);//избавляемся от одного лишнего байта в буффере порта (контрольной сумма пакета) для продолжения считывания информации по другим счётчикам
                                    }
                                    //tasks[groups.IndexOf(group)].Start();//запускаем задания одно за другим сразу после создания
                                }//конец foreach (TreeNode[] group in groups)                      
                                 //Task.WaitAll(tasks);//после запуска всех заданий для каждой группы счётчиков нужно подождать выполнение всех заданий
                            }//конец ReadLastMonth
                        }
                    }
                //}
            }      

        private int CalcCheckSum(byte[] InBuf)
        {   //процедура, возвращающая поле checksum пакета для концентратора
            //Младший байт суммы всех байтов поля PAYLOAD минус 1.
            int sum = 0; byte lowByte = 0;
            for (int i = 0; i < InBuf.Length; i++) { sum += InBuf[i]; } //суммируем элементы массива
            sum -= 1;//отнимаем единицу согласно правилам
            string sumStr = sum.ToString("X").PadLeft(4, '0');//переводим целое число в 16-ричную строку
            lowByte = Convert.ToByte(sumStr.Substring(2, 2), 16);//выделяем младший байт
            return lowByte;
        }

        public DataTable GetPowerProfileForCounter(string workingPort, ICounter counter, DataProcessing dp, ToolStripProgressBar pb, DateTime DateN, DateTime DateK,
            int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent)
        {//эта процедура ветвит получение профиля в зависимости от типа счётчика (т.к. для каждого типа счётчика свои правила получения профиля)
            DataTable dt = counter.ProfileDataTable;

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
            byte[] Package = this.FormPackage(PortConfigArray(1), 0, dp, packnum);
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
                return dt;
            }

            if (worker.CancellationPending == true) { return dt; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
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
                return dt;
            }
            currentDate = DateTime.Now;

            richText.Invoke(new Action(delegate
            {
                richText.SelectionColor = Color.DarkGreen;
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно\r");
                richText.ScrollToCaret();
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
            }));           

            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
               richText.AppendText(currentDate + "." + currentDate.Millisecond + " Пытаемся получить профиль мощности...\r");
               richText.ScrollToCaret();             
            }));

            packnum += 1;
            //непосредственно сама процедура чтения профиля, реализованная в счётчике
            dt = counter.GetPowerProfileForCounterOnGate(workingPort, this, dp, pb, DateN, DateK, count, crl, lrl, richText, worker, ReReadOnlyAbsent, packnum);
            return dt;
        }
           
        public void GetMonitorForCounter(string workingPort, ICounter counter, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker)
        {//процедура получения монитора (параметры тока)
            counter.ReadMonitorOnGate(this, workingPort, dp, pb, pixBox, richText, ref worker);
        }

        public static string DecodeConfigString(string hexValue)
        {//процедура расшифрования 2-чной строки автоконфигурации шлюза
            string valueResultStr = String.Empty;
            //нужно перевернуть строку
            string reverseHexValue = String.Empty;
            for (int i = 1; i <= 3; i++) { reverseHexValue += hexValue.Substring(hexValue.Length - i * 2, 2).PadLeft(2, '0'); }

            string binValue = Convert.ToString(Convert.ToInt32(reverseHexValue, 16), 2).PadLeft(24,'0');//переводим 16-ричную строку в двоичный вид
            string propertyvalue = binValue.Substring(binValue.Length - 4, 4);//смотрим биты поля UART - скорость
            //смотрим значение полей и составляем строку
            switch (propertyvalue)
            {
                case "0001": valueResultStr += "300/"; break;
                case "0010": valueResultStr += "600/"; break;
                case "0011": valueResultStr += "1200/"; break;
                case "0100": valueResultStr += "2400/"; break;
                case "0101": valueResultStr += "4800/"; break;
                case "0110": valueResultStr += "9600/"; break;
                case "0111": valueResultStr += "14400/"; break;
                case "1000": valueResultStr += "19200/"; break;
                case "1001": valueResultStr += "28800/"; break;
                case "1010": valueResultStr += "38400/"; break;
                case "1011": valueResultStr += "57600/"; break;
                case "1100": valueResultStr += "115200/"; break;
            }

            propertyvalue = binValue.Substring(binValue.Length - 5, 1);//смотрим биты поля UART - биты данных
            switch (propertyvalue)
            {
                case "0": valueResultStr += "7 б.д./"; break;
                case "1": valueResultStr += "8 б.д./"; break;              
            }

            propertyvalue = binValue.Substring(binValue.Length - 6, 1);//смотрим биты поля UART - количество стоповых бит
            switch (propertyvalue)
            {
                case "0": valueResultStr += "1 ст.б./"; break;
                case "1": valueResultStr += "2 ст.б./"; break;
            }

            propertyvalue = binValue.Substring(binValue.Length - 7, 1);//смотрим биты поля UART - проверка чётности
            switch (propertyvalue)
            {
                case "0": valueResultStr += "Чёт не пров/"; break;
                case "1": valueResultStr += "Чёт пров/"; break;
            }

            propertyvalue = binValue.Substring(binValue.Length - 8, 1);//смотрим биты поля UART - чётность
            switch (propertyvalue)
            {
                case "0": valueResultStr += "Чёт/"; break;
                case "1": valueResultStr += "Нечет/"; break;
            }

            propertyvalue = binValue.Substring(binValue.Length - 16, 8);//смотрим биты поля WAIT
            switch (propertyvalue)
            {
                case "00100011": valueResultStr += "300 мс/"; break;
                case "00110011": valueResultStr += "3000 мс/"; break;
            }

            propertyvalue = binValue.Substring(binValue.Length - 24, 8);//смотрим биты поля PAUSE
            valueResultStr += Convert.ToInt16(propertyvalue, 2).ToString();//сразу переводим его двоичное значение в десятиричное

            return valueResultStr;
        }

        public static string FormConfigString(string newValueStr)
        {//процедура, призванная сформировать массив байтов строки автоконфигурации исходя из поданной строки настроек      
            string valueResultStr = String.Empty;//строка представляющая результирующий массив для последующего перевода в массив байт                     
                int pos = 0;//позиция символа-разделителя
                for (int i = 0; i < 7; i++)//цикл по строке с разделителями
                {
                    pos = newValueStr.IndexOf("/", 0);//находим первое вхождение разделяющего символа в строке
                    string propertyvalue = newValueStr.Substring(0, pos);//читаем параметры порта один за другим через разделитель
                    //начинаем формировать промежуточную строку valueResultStr, исходя из поданной строки значения newValueStr. 
                    //Промежуточную строку потом переведём в массив байт  
                    switch (propertyvalue)
                    {
                        //поле UART
                        case "300": { valueResultStr += "0001"; break; } //1
                        case "600": { valueResultStr += "0010"; break; } //2
                        case "1200": { valueResultStr += "0011"; break; } //3
                        case "2400": { valueResultStr += "0100"; break; } //4
                        case "4800": { valueResultStr += "0101"; break; } //5
                        case "9600": { valueResultStr += "0110"; break; } //6
                        case "14400": { valueResultStr += "0111"; break; } //7
                        case "19200": { valueResultStr += "1000"; break; } //8
                        case "28800": { valueResultStr += "1001"; break; } //9
                        case "38400": { valueResultStr += "1010"; break; } //10
                        case "57600": { valueResultStr += "1011"; break; } //11
                        case "115200": { valueResultStr += "1100"; break; } //12
                        case "7 б.д.": { valueResultStr += "0"; break; }//7 бит данных
                        case "8 б.д.": { valueResultStr += "1"; break; }//8 бит данных
                        case "1 ст.б.": { valueResultStr += "0"; break; }//один стоповый бит
                        case "2 ст.б.": { valueResultStr += "1"; break; }//два стоповых бита
                        case "Чёт не пров": { valueResultStr += "0"; break; }//чётность не проверяется
                        case "Чёт пров": { valueResultStr += "1"; break; }//чётность проверяется
                        case "Чёт": { valueResultStr += "0"; break; }//чёт
                        case "Нечет": { valueResultStr += "1"; break; }//нечет
                        //поле WAIT. Не будем вычислять. Просто подставим известные значения
                        //case "200 мс": { break; }
                        case "300 мс": { valueResultStr += "00100011"; break; }
                        case "3000 мс": { valueResultStr += "00110011"; break; }
                        //поле PAUSE
                        case "3 симв": { valueResultStr += "00000011"; break; }
                        case "4 симв": { valueResultStr += "00000100"; break; }
                    }
                    //отрезаем от начала строки использованные свойства
                    newValueStr = newValueStr.Substring(pos + 1);
                }
            return valueResultStr;
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

        public bool Search(string textToFind, StringComparison compare, string add = "")
        {
            if ((Utils.Contains(this.Name, textToFind, StringComparison.CurrentCultureIgnoreCase))
             || (Utils.Contains(this.Phone, textToFind, StringComparison.CurrentCultureIgnoreCase))
             || (Utils.Contains(add, textToFind, StringComparison.CurrentCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }  
}
