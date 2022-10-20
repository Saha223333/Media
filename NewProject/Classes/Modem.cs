using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace NewProject
{
    
    public class Modem : IDevice, IConnection
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string IP { get; set; }
        public string Port { get; set; } = String.Empty;
        public int Channel { get; set; } //определяет канал связи (0 - GSM, 1 - Интернет)
        public string CBST { get; set; }        

        public Modem(int pid, string pname, string pphone, string pip, string pcbst)
        {
            ID = pid; ParentID = 0; Name = pname; Phone = pphone; IP = pip; CBST = pcbst;
        }

        public bool TestCounter(ICounter counter, DataProcessing dp, RichTextBox richText, ref BackgroundWorker worker)
        {  //процедура проверки связи со счётчиком
         try {
                DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Тест связи с " + counter.Name + "\r");
                    richText.ScrollToCaret();
                }));

                byte[] testOutBuf = counter.FormTestArray(dp); //вызываем процедуру формирования тестового массива
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

                if (worker.CancellationPending == true) { return false; } //проверяем, был ли запрос на отмену работы
                byte[] answerArray = dp.Read(4, 10000, true);

                if ((answerArray[0] != counter.NetAddress) || (answerArray.Length == 5)) //первый байт ответа всегда должен быть сетевой вдрес
                {
                    currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Неудачно\r");
                        richText.ScrollToCaret();
                    }));
                    //Thread.Sleep(300);
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
                return false;
            }
        }

        public void WriteParametersToDevice(TreeNode node, DataProcessing dp,  
            RichTextBox richText, BackgroundWorker worker, List<FieldsValuesToWrite> ValuesList, List<FieldsValuesToWrite> list = null)
        {//процедура, выполняющая запись параметров в выбранное устройство (например шлюз или концентратор)
            if (!(node.Tag is IWritable)) return;

            //WriteParameterToDeviceDelegate del = new WriteParameterToDeviceDelegate(WriteParametersToDevice);//создаём экземпляр делегата чтобы передать метод
            if (node.Tag.GetType() == typeof(Mercury225PLC1))//если концентратор
            { 
                Mercury225PLC1 concentrator = (Mercury225PLC1)node.Tag;
                var checkedParameters = from param in concentrator.ParametersToWrite where param.check == true select param;
                if (checkedParameters.Count<Mercury225PLC1ParametersToWrite>() == 0)
                {
                    DateTime currentDateA = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDateA + "." + currentDateA.Millisecond + " Ни одного параметра концентратора на запись не обнаружено!\r");
                        richText.ScrollToCaret();
                    }));
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
                byte[] OutBuf = null; //инициализируем результирующий массив запроса
                //цикл по отмеченным параметрам
                foreach (Mercury225PLC1ParametersToWrite param in checkedParameters)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы 
                    currentDate = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "\r");
                        richText.ScrollToCaret();
                    }));

                    //-----------------------------------------------------------------------------------------------------------------
                    ////сначала выполняем запрос на выполнение одной защищенной команды, т.е. подаём пароль на концентратор
                    //OutBuf =  FormPackage(concentrator.FormParameterArray(dp, concentrator.ParametersToWrite[0]), 1, dp, packnum);
                    //dp.SendData(OutBuf, 0);
                    //byte[] answerArray = dp.Read(param.newValue.Length + 1,10000,true);
                    //-----------------------------------------------------------------------------------------------------------------

                    FieldsValuesToWrite field = list.Find(x => x.name == param.name);//ищем в списке текущий параметр
                    string value = field.value;
                    param.FormParamNewValueArray(value);//подаём новое значение параметра на разбор в процедуру

                    OutBuf = concentrator.FormParameterArray(dp, param);
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

                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    byte[] answerArray = dp.Read(param.value.Length + param.bytes.Length + 9, 10000, true);

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
                }
            }
            //----------------------------------------------------------------------
            if (node.Tag is ICounter)//если счётчик и в него можно писать параметры
            {
                ICounter counter = (ICounter)node.Tag;
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
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Работаем со счётчиком... " + counter.NetAddress.ToString() + "...\r");
                    richText.ScrollToCaret();
                }));

                if (counter.GainAccessOnModem(dp, richText, 2, 2, ref worker, 4) == false)
                {
                    return;
                }
                currentDate = DateTime.Now;
                richText.Invoke(new Action(delegate
                {
                    richText.AppendText(currentDate + "." + currentDate.Millisecond + " Запись параметров счётчика... " + counter.NetAddress.ToString() + "...\r");
                    richText.ScrollToCaret();
                }));

                byte[] OutBuf = null; //инициализируем результирующий массив запроса
                //цикл по отмеченным параметрам
                foreach (CounterParameterToWrite param in checkedParameters)
                {
                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы 
                    currentDate = DateTime.Now;
                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "\r");
                        richText.ScrollToCaret();
                    }));

                    param.value = counter.FormParamNewValueArray(ValuesList, param.name, param.additional);//подаём новое значение параметра на разбор в процедуру
                    OutBuf = counter.FormParameterArray(dp, param);//формируем исходящий запрос исходя из правил формирования в текущем счётчике
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

                    if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                    byte[] answerArray = dp.Read(4, 5000, true);//читаем ответ

                    string answerStrError = counter.ValidateWriteParameterAnswer(answerArray, 0);//вызываем процедуру проверки ответа от счётчика

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
                }
            }
        }       

        public void GatherDevicesData(string portname, TreeNodeCollection childNodes, DataProcessing dp, ToolStripProgressBar pb, DateTime ndate, DateTime kdate, 
            int countSumm, bool GetProfile, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsentProfile)
        {   //процедура опроса устройств, подключенных к модему                 
            byte[] errorArray = { 0x0045, 0x0052, 0x0052, 0x004F, 0x0052 }; //ERROR
            //цикл по дочерним узлам подключения
            foreach (TreeNode node in childNodes)
            {
                if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы   
                //проверяем какой тип дочернего устройства
                if (!(node.Tag is IReadable)) { continue; }
                if (node.Tag is ICounter)
                {                 
                    ICounter counter = (ICounter)node.Tag;
                    //получение доступа (открытие канала)                  
                    if (counter.GainAccessOnModem(dp, richText, 1, 1, ref worker, 4) == false)
                    {
                        continue;
                    }
                    //считывание параметров
                    counter.ReadParametersOnModem(dp, worker, richText, this);
                    //считывание журнала
                    counter.ReadJournalOnModem(dp, worker, richText);
                    //считывание журнала ПКЭ
                    counter.ReadJournalCQCOnModem(dp, worker, richText);
                    //считывание энергии
                    counter.ReadEnergyOnModem(dp, worker, richText);
                    
                    //после всего снимаем профиль если стоит галочка
                    if (GetProfile == true)
                    {
                        GetPowerProfileForCounter(portname, counter, dp, pb, ndate, kdate, countSumm, crl, lrl, richText, worker, ReReadOnlyAbsentProfile);
                    }
                }//конец условия по счётчикам
                //--------------------------------------------------------------------------------------------------------------------
                if (node.Tag.GetType() == typeof(Mercury225PLC1))//если текущий дочерний узел - концентратор
                {
                    Mercury225PLC1 concentrator = (Mercury225PLC1)node.Tag;
                    DateTime currentDate = DateTime.Now;

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
                    //берём только те параметры, которые были отмечены
                    var checkedParameters = from param in concentrator.ParametersToRead where param.check == true select param;
                    foreach (Mercury225PLC1ParametersToRead param in checkedParameters)
                    {
                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        currentDate = DateTime.Now;

                        richText.Invoke(new Action(delegate
                        {
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " " + param.name + "\r");
                           richText.ScrollToCaret();
                        }));
                        int CountToWait = 0;
                        OutBuf = concentrator.FormParameterArray(dp, param);//формируем запрос к концентратору (читаем его параметры)
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

                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        byte[] answerArray = dp.Read(8, 10000, false);//ждём данные. Сначала читаем заголовок пакета чтобы узнать, сколько байт нам нужно принять

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
                       
                        CountToWait = answerArray[7];//последний байт заголовка
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
                        continue;
                        }
                        param.value = concentrator.ReadStr(answerArray, param.name, 0);
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                           richText.SelectionColor = Color.DarkGreen;
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Удачно: " + param.value + "\r");
                           richText.ScrollToCaret();
                        }));                    
                    }
                    //смотрим, какой размер сети у концентратора. Если 16, значит настройки слетели
                    if (concentrator.NetSize == 16)
                    {//предупреждаем об этом
                        currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                           richText.SelectionColor = Color.DarkOrange;
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

                    if (node.Nodes.Count == 0) continue;//если у концентратора нет детей, то мдём на следующий концентратор
                    currentDate = DateTime.Now;

                    richText.Invoke(new Action(delegate
                    {
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Чтение буфера последних месяцев концентратора " + concentrator.NetAddress.ToString() + "...\r");
                        richText.ScrollToCaret();
                    }));

                    OutBuf = null; //инициализируем результирующий массив запросов                 
                    foreach (TreeNode childNode in node.Nodes)
                    {//циклимся по дочерним PLC1-счётчикам
                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                     
                        MercuryPLC1 counter = (MercuryPLC1)childNode.Tag;

                        if (node.Text[node.Text.Length - 1] == 'K') counter.clonemode = true;//если маркер клонирования на концентраторе присутствует, то выставляем счётчику режим клонирования
                        else counter.clonemode = false;//иначе его показания нужно обрабатывать и записывать в базу (если при этом он не находится в виртуальном режиме)

                        currentDate = DateTime.Now;

                        richText.Invoke(new Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counter.Name + " (" + counter.NetAddress + ")" + "\r");
                            richText.ScrollToCaret();
                        }));
                        OutBuf = concentrator.FormEnergyArrayForCounter(dp, childNode, concentrator.NetAddress);//формируем запрос к дочернему PLC-счётчику. 
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

                        if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы
                        byte[] answerArray = dp.Read(8, 10000, false);//ждём данные. Сначала читаем заголовок пакета (8 байт). 8ой байт - количество байт полезной нагрузки
                                                               //в случае ошибки
                        if (answerArray.Length == 5)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                        richText.Invoke(new Action(delegate
                        {
                           richText.SelectionColor = Color.Red;
                           richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counter.Name + " (" + counter.NetAddress + ") неудачно\r");
                           richText.ScrollToCaret();
                        }));
                        continue;

                        }
                        
                        byte count = answerArray[7]; //количество байт полезной нагрузки пакета от концентратора

                        if (worker.CancellationPending == true) { return; }//перед попыткой дождаться ответа от устройства, проверим, не остановил ли пользователь работу
                        answerArray = dp.Read(count, 10000, true);//ждём данные опять и читаем уже количество байт полезной нагрузки

                        //в случае ошибки. 8 байт ответного массива - запрос считывания буффера последних месяцев
                        if (answerArray.Length == 5)
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counter.Name + " (" + counter.NetAddress + ") неудачно\r");
                                richText.ScrollToCaret();
                            }));
                            continue;
                        }

                        string result = counter.ReadEnergyPLC(dp, answerArray, 0);

                        if (result == "-2")//вернулась ошибка разрыва связи с БД
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " ОБРЫВ СОЕДИНЕНИЯ С БАЗОЙ ДАННЫХ!!!\r");
                                richText.ScrollToCaret();
                            }));
                            if (worker.CancellationPending == true) { return; } //проверяем, был ли запрос на отмену работы                         
                            continue;
                        }

                        if (result == "-1")//читаем энергию и раскидываем по тарифам. Если вернулась строка -1, то значит произошёл сбой
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.Red;
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counter.Name + " (" + counter.NetAddress + ") неудачно\r");
                                richText.ScrollToCaret();
                            }));
                            continue;
                        }

                        else//иначе всё прошло нормально
                        {
                            currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

                            richText.Invoke(new Action(delegate
                            {
                                richText.SelectionColor = Color.DarkGreen;
                                if (result[0] == 'П') richText.SelectionColor = Color.Red;//если пошёл перерасход, то подсвечиваем красным
                                if (result[0] == 'д') richText.SelectionColor = Color.Red;//если дата новых показаний меньше или равна дате последних показаний
                                if (result[0] == 'о') richText.SelectionColor = Color.Red;//ошибка нулевых показаний, подсвечиваем красным
                                if (result[0] == 'Б') richText.SelectionColor = Color.Red;//ошибка большого интервала времени между показаниями , подсвечиваем красным
                                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Счётчик " + counter.Name + " (" + counter.NetAddress + "): " + result + "\r");
                                richText.ScrollToCaret();
                            }));
                        }
                    }//конец по счётчикам концентратора
                }//конец концентратора PLC1
            } //конец цикла foreach (TreeNode node in childNodes)- цикла по дочерним узлам модема
        }//конец процедуры GatherDevicesData
       
        public void GetMonitorForCounter(string workingPort, ICounter counter, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox,
            RichTextBox richText, ref BackgroundWorker worker)
        {//процедура получения монитора (параметры тока)
            counter.ReadMonitorOnModem(workingPort, dp, pb, pixBox, richText, ref worker);
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

            DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе

            richText.Invoke(new Action(delegate
            {
                richText.AppendText(currentDate + "." + currentDate.Millisecond + " Пытаемся получить профиль мощности...\r");
                richText.ScrollToCaret();
            }));

            if (worker.CancellationPending == true) { return dt; } //проверяем, был ли запрос на отмену работы
            //непосредственно сама процедура получения профиля мощности, реализованная в счётчике
            dt = counter.GetPowerProfileForCounterOnModem(workingPort, dp, pb, DateN, DateK, count, crl, lrl, richText, worker, ReReadOnlyAbsent);

            return dt;
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
