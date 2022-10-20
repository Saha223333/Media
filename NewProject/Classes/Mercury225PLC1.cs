using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;

namespace NewProject
{
    public class Mercury225PLC1 : IDevice, IConcentrator, IReadable, IWritable
    {
    
        public int ID { get; set; }
        public int ParentID { get; set; }
        public string Name { get; set; }
        public string NetAddress { get; set; }
        public string Password { get; set; }//пока просто повисит, потом будем использовать
        public int NetSize = 0;

        public BindingList<Mercury225PLC1ParametersToRead> ParametersToRead; //перечень параметров концентратора  для чтения
        public BindingList<Mercury225PLC1ParametersToWrite> ParametersToWrite { get; set; } //перечень параметров концентратора для записи

        public Mercury225PLC1 (int pid,int pparentid,string pname,string pnetadr,//int pparentdevice, 
            string ppwd)
        {
            Password = ppwd;
            ID = pid; Name = pname;
            ParentID = pparentid; NetAddress = pnetadr;

            ParametersToRead = new BindingList<Mercury225PLC1ParametersToRead>();
            {
                byte[] b = new byte[1];
                b[0] = 0x0083;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Версия ПО", b, false));
            }

            {
                byte[] b = new byte[1];
                b[0] = 0x0080;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Конфигурация", b, true));
            }

            {
                byte[] b = new byte[1];
                b[0] = 0x0081;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Дата и время", b, true));
            }

            {
                byte[] b = new byte[1];
                b[0] = 0x0086;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Сетевой адрес", b, false));
            }

            {
                byte[] b = new byte[1];
                b[0] = 0x0089;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Расчётный день", b, false));
            }

            {
                byte[] b = new byte[1];
                b[0] = 0x0088;
                ParametersToRead.Add(new Mercury225PLC1ParametersToRead("Скорость порта", b, false));
            }

            ParametersToWrite = new BindingList<Mercury225PLC1ParametersToWrite>();
            //{
            //    byte[] b = new byte[1]; byte[] newValue = new byte[Password.Length];
            //    //преобразуем строковый пароль концентратора в массив байтов
            //    if (newValue.Length > 0) { newValue = Encoding.ASCII.GetBytes(Password); }
            //    b[0] = 0x000B;
            //    ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Разрешение защищённой команды", b, false, newValue));
            //}

            //{
            //    byte[] b = new byte[1]; byte[] newValue = new byte[0];
            //    b[0] = 0x0008;
            //    ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Скорость порта", b, false, newValue));
            //}

            {
                byte[] b = new byte[1]; byte[] newValue = new byte[0];
                b[0] = 0x0001;
                ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Дата и время", b, false, newValue));
            }

            {
                byte[] b = new byte[1]; byte[] newValue = new byte[0];
                b[0] = 0x0000;
                ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Конфигурация", b, false, newValue));
            }

            {
                byte[] b = new byte[1]; byte[] newValue = new byte[0];
                b[0] = 0x0006;
                ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Сетевой адрес", b, false, newValue));
            }

            {
                byte[] b = new byte[1]; byte[] newValue = new byte[0];
                b[0] = 0x0009;
                ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("Расчётный день", b, false, newValue));
            }

            //{
            //    byte[] b = new byte[1]; byte[] newValue = new byte[0];
            //    b[0] = 0x0009;
            //    ParametersToWrite.Add(new Mercury225PLC1ParametersToWrite("ПО", b, false, newValue));
            //}
        }

        public string ReadStr(byte[] array, string pname, int offset)
        {
            try {
                switch (pname)
                {
                    case "Дата и время":
                        {

                            string valueStr = String.Empty;
                            array[5 + offset] += 1;//нужно добавить день и месяц т.к. в массиве день и месяц приходят на единицу меньше
                            array[6 + offset] += 1;
                            valueStr = array[5 + offset].ToString().PadLeft(2, '0') + "." + array[6 + offset].ToString().PadLeft(2, '0') + ".20" + array[7 + offset].ToString();
                            DateTime dt = Convert.ToDateTime(valueStr);
                            //получаем и присовокупляем время
                            valueStr = dt.ToString().Substring(0, 10) + " " + array[3 + offset].ToString().PadLeft(2, '0') + ":" + array[2 + offset].ToString().PadLeft(2, '0') + ":" + array[1 + offset].ToString().PadLeft(2, '0');

                            return valueStr;
                        }

                    case "Конфигурация":
                        {
                            string valueStr = String.Empty;
                            string vol = array[2 + offset].ToString("X").PadLeft(2, '0') + array[1 + offset].ToString("X").PadLeft(2, '0');
                            valueStr = "Ёмкость: " + Convert.ToInt16(vol, 16) + "; ";
                            //запоминаем ёмоксть в отдельном параметре, чтобы потом выдать ошибку если ёмкость сбросилась до 16
                            this.NetSize = Convert.ToInt16(vol, 16);
                            string a = Convert.ToString(array[3 + offset], 2); //переводим байт ответа в двоичную строку для анализа
                            a = a.PadLeft(8, '0'); //добавляем нули слева до 8 бит если их нет   
                            string b = String.Empty; //значение параметра

                            switch (a.Substring(2, 1)) //смотрим что там в битах
                            {
                                case "0": { b = " Запрет функ-я PLC-интерфейса НЕТ;"; } break;
                                case "1": { b = " Запрет функ-я PLC-интерфейса ДА;"; } break;
                            }

                            switch (a.Substring(3, 1))
                            {
                                case "0": { b = " Авто зима/лето НЕТ;"; } break;
                                case "1": { b = " Авто зима/лето ДА;"; } break;
                            }

                            switch (a.Substring(4, 2))
                            {
                                case "00": { b = " Режим: Обычный;"; } break;
                                case "01": { b = " Режим: Master (SR);"; } break;
                                case "10": { b = " Режим: Slave (SRT);"; } break;
                                case "11": { b = " Режим: Slave (SR);"; } break;
                            }
                            valueStr += b;
                            switch (a.Substring(6, 1))
                            {
                                case "0": { b = " Нулевой порог НЕТ;"; } break;
                                case "1": { b = " Нулевой порог ДА;"; } break;
                            }

                            switch (a.Substring(7, 1))
                            {
                                case "0": { b = " Непрозрачный;"; } break;
                                case "1": { b = " Прозрачный;"; } break;
                            }
                            valueStr += b;
                            return valueStr;
                        }

                    case "Сетевой адрес":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[2 + offset].ToString("X").PadLeft(2, '0') + array[1 + offset].ToString("X").PadLeft(2, '0');

                            return valueStr;
                        }

                    case "Версия ПО":
                        {
                            string valueStr = String.Empty;
                            valueStr = (Encoding.ASCII.GetString(array));
                            return valueStr;
                        }
                    case "Расчётный день":
                        {
                            string valueStr = String.Empty;
                            valueStr = array[1 + offset].ToString();

                            return valueStr;
                        }
                    case "Скорость порта":
                        {
                            string valueStr = String.Empty;
                            switch (array[1 + offset])
                            {
                                case 0:
                                    valueStr = "38400"; break;
                                case 1:
                                    valueStr = "19200"; break;
                                case 2:
                                    valueStr = "9600"; break;
                            }
                            return valueStr;
                        }
                }
            }
            catch
            {
                return "Ошибка";
            }
            return "Ошибка";
        }

        public byte[] FormEnergyArrayForCounter(DataProcessing dp, TreeNode counterNode, string parentAddress)
        {   //процедура, формирующая пакет для снятия энергии PLC-счётчиков            
            MercuryPLC1 counter = (MercuryPLC1)counterNode.Tag;
            //-------полезная нагрузка (PAYLOAD )----------------------------------------------------------------------
            byte[] PayLoad = new byte[3]; byte[] OutBuf = new byte[12]; byte[] Header = new byte[5];
            string CounterAddress = counter.NetAddress.ToString("X").PadLeft(4, '0');//переводим целочисленный адрес  счётчика в 16-ричную строку
            byte CounterAddressHi = Convert.ToByte(CounterAddress.Substring(0, 2), 16);//старший адрес счётчика
            byte CounterAddressLow = Convert.ToByte(CounterAddress.Substring(2, 2), 16);//младший адрес счётчика
            PayLoad[0] = 0x0085;//непосредственно команда - чтение буфера последних месяцев
            PayLoad[1] = CounterAddressLow;//номер счетчика
            PayLoad[2] = CounterAddressHi;
            //заголовок (crc24=3,src=2,dst=2,len=1)            
            Header[0] = 0x00FF;//адрес источника (компьютер)
            Header[1] = 0x00FF;
            string ConcentratorAddress = parentAddress.PadLeft(4, '0');//адрес концентратора
            byte ConcentratorAddressHi = Convert.ToByte(ConcentratorAddress.Substring(0, 2), 16);//старший адрес концентратора
            byte ConcentratorAddressLow = Convert.ToByte(ConcentratorAddress.Substring(2, 2), 16);//младший адрес концентратора
            Header[2] = ConcentratorAddressLow;//адрес получателя-концентратора
            Header[3] = ConcentratorAddressHi;
            Header[4] = (byte)PayLoad.Length;//длина полезной нагрузки
            long crc = dp.ComputeCrc2(Header); //контрольная сумма заголовочной части пакета
            string crcStr = crc.ToString("X").PadLeft(6, '0');//переводим контрольную сумму в 16-ричную строку
            //наконец наполняем итоговый массив который уйдёт в порт
            OutBuf[0] = Convert.ToByte(crcStr.Substring(4, 2), 16);
            OutBuf[1] = Convert.ToByte(crcStr.Substring(2, 2), 16);
            OutBuf[2] = Convert.ToByte(crcStr.Substring(0, 2), 16);   
            OutBuf[3] = Header[0];//адрес источника пакета (в данном случае компьютер)
            OutBuf[4] = Header[1];//   
            OutBuf[5] = Header[2];//адрес получателя (концентратора) пакета (младший байт вперед)
            OutBuf[6] = Header[3];//
            OutBuf[7] = Header[4];//длина полезной нагрузки       
            Array.Copy(PayLoad, 0, OutBuf, 8, PayLoad.Length);//копируем массив полезной нагрузки в результирующий массив пакета
            OutBuf[8 + PayLoad.Length] = (byte)CalcCheckSum(PayLoad);// Младший байт суммы всех байтов поля PAYLOAD минус 1 

            return OutBuf;
        }

        public byte[] FormParameterArray(DataProcessing dp, Mercury225PLC1ParametersToRead param)
        {//процедура, формирующая массив для чтения параметра
            byte[] PayLoad = param.bytes;  byte[] Header = new byte[5];
            byte[] OutBuf = new byte[Header.Length + PayLoad.Length + 4];
            Header[0] = 0x00FF;//адрес источника (компьютер)
            Header[1] = 0x00FF;
            string ConcentratorAddress = NetAddress.ToString();//переводим целочисленный адрес концентратора в 16-ричную строку
            byte ConcentratorAddressHi = Convert.ToByte(ConcentratorAddress.Substring(0, 2), 16);//старший адрес концентратора
            byte ConcentratorAddressLow = Convert.ToByte(ConcentratorAddress.Substring(2, 2), 16);//младший адрес концентратора
            Header[2] = ConcentratorAddressLow;//адрес получателя-концентратора
            Header[3] = ConcentratorAddressHi;
            Header[4] = (byte)PayLoad.Length;//длина полезной нагрузки
            long crc = dp.ComputeCrc2(Header); //контрольная сумма заголовочной части пакета
            string crcStr = crc.ToString("X").PadLeft(6, '0');//переводим контрольную сумму в 16-ричную строку
            //наконец наполняем итоговый массив который уйдёт в порт
            OutBuf[0] = Convert.ToByte(crcStr.Substring(4, 2), 16);
            OutBuf[1] = Convert.ToByte(crcStr.Substring(2, 2), 16);
            OutBuf[2] = Convert.ToByte(crcStr.Substring(0, 2), 16);
            OutBuf[3] = Header[0];//адрес источника пакета (в данном случае компьютер)
            OutBuf[4] = Header[1];//   
            OutBuf[5] = Header[2];//адрес получателя (концентратора) пакета (младший байт вперед)
            OutBuf[6] = Header[3];//
            OutBuf[7] = Header[4];//длина полезной нагрузки       
            Array.Copy(PayLoad, 0, OutBuf, 8, PayLoad.Length);//копируем массив полезной нагрузки в результирующий массив пакета
            OutBuf[8 + PayLoad.Length] = (byte)CalcCheckSum(PayLoad);// Младший байт суммы всех байтов поля PAYLOAD минус 1

            return OutBuf;
        }

        public byte[] FormParameterArray(DataProcessing dp, Mercury225PLC1ParametersToWrite param)
        {//процедура, формирующая массив для записи параметра
            byte[] PayLoad = param.bytes;
            Array.Resize(ref PayLoad, PayLoad.Length + param.value.Length);//наращиваем к массиву полезной нагрузки длину массива нового значения 
            Array.Copy(param.value, 0, PayLoad, PayLoad.Length - param.value.Length, param.value.Length);//присовокупляем массив нового значения к массиву полезной нагрузки
            byte[] Header = new byte[5];
            byte[] OutBuf = new byte[Header.Length + PayLoad.Length + 4];
            Header[0] = 0x00FF;//адрес источника (компьютер)
            Header[1] = 0x00FF;
            string ConcentratorAddress = NetAddress.ToString();//переводим целочисленный адрес концентратора в 16-ричную строку
            byte ConcentratorAddressHi = Convert.ToByte(ConcentratorAddress.Substring(0, 2), 16);//старший адрес концентратора
            byte ConcentratorAddressLow = Convert.ToByte(ConcentratorAddress.Substring(2, 2), 16);//младший адрес концентратора
            Header[2] = ConcentratorAddressLow;//адрес получателя-концентратора
            Header[3] = ConcentratorAddressHi;
            Header[4] = (byte)PayLoad.Length;//длина полезной нагрузки
            long crc = dp.ComputeCrc2(Header); //контрольная сумма заголовочной части пакета
            string crcStr = crc.ToString("X").PadLeft(6, '0');//переводим контрольную сумму в 16-ричную строку
            //наконец наполняем итоговый массив который уйдёт в порт
            OutBuf[0] = Convert.ToByte(crcStr.Substring(4, 2), 16);
            OutBuf[1] = Convert.ToByte(crcStr.Substring(2, 2), 16);
            OutBuf[2] = Convert.ToByte(crcStr.Substring(0, 2), 16);
            OutBuf[3] = Header[0];//адрес источника пакета (в данном случае компьютер)
            OutBuf[4] = Header[1];//   
            OutBuf[5] = Header[2];//адрес получателя (концентратора) пакета (младший байт вперед)
            OutBuf[6] = Header[3];//
            OutBuf[7] = Header[4];//длина полезной нагрузки       
            Array.Copy(PayLoad, 0, OutBuf, 8, PayLoad.Length);//копируем массив полезной нагрузки в результирующий массив пакета
            OutBuf[8 + PayLoad.Length] = (byte)CalcCheckSum(PayLoad);// Младший байт суммы всех байтов поля PAYLOAD минус 1

            return OutBuf;
        }

        //public byte[] FormProtectedArray (DataProcessing dp, byte[] pwd)
        //{//процедура формирующая запрос на выполнение одной защищенноё команды для концентратора
        //    byte[] PayLoad = new byte[1 + pwd.Length]; byte[] Header = new byte[5];
        //    byte[] OutBuf = new byte[Header.Length + PayLoad.Length + 4];
        //    PayLoad[0] = 0x0000B;//сама команды подачи пароля на концентратор
        //    Array.Copy(pwd, 0, PayLoad, 1, pwd.Length);//вставляем в полезную нагрузку строку с паролем
        //    Header[0] = 0x00FF;//адрес источника (компьютер)
        //    Header[1] = 0x00FF;
        //    string ConcentratorAddress = NetAddress.ToString("X");//переводим целочисленный адрес концентратора в 16-ричную строку
        //    byte ConcentratorAddressHi = Convert.ToByte(ConcentratorAddress.Substring(0, 2), 16);//старший адрес концентратора
        //    byte ConcentratorAddressLow = Convert.ToByte(ConcentratorAddress.Substring(2, 2), 16);//младший адрес концентратора
        //    Header[2] = ConcentratorAddressLow;//адрес получателя-концентратора
        //    Header[3] = ConcentratorAddressHi;
        //    Header[4] = (byte)PayLoad.Length;//длина полезной нагрузки
        //    long crc = dp.ComputeCrc2(Header); //контрольная сумма заголовочной части пакета
        //    string crcStr = crc.ToString("X").PadLeft(6, '0');//переводим контрольную сумму в 16-ричную строку
        //    //наконец наполняем итоговый массив который уйдёт в порт
        //    OutBuf[0] = Convert.ToByte(crcStr.Substring(4, 2), 16);
        //    OutBuf[1] = Convert.ToByte(crcStr.Substring(2, 2), 16);
        //    OutBuf[2] = Convert.ToByte(crcStr.Substring(0, 2), 16);
        //    OutBuf[3] = Header[0];//адрес источника пакета (в данном случае компьютер)
        //    OutBuf[4] = Header[1];//   
        //    OutBuf[5] = Header[2];//адрес получателя (концентратора) пакета (младший байт вперед)
        //    OutBuf[6] = Header[3];//
        //    OutBuf[7] = Header[4];//длина полезной нагрузки       
        //    Array.Copy(PayLoad, 0, OutBuf, 8, PayLoad.Length);//копируем массив полезной нагрузки в результирующий массив пакета
        //    OutBuf[8 + PayLoad.Length] = (byte)CalcCheckSum(PayLoad);// Младший байт суммы всех байтов поля PAYLOAD минус 1
        //    //очищаем буферы порта
        //    System.Threading.Thread.Sleep(100);
        //    dp.clearBuffs();
        //    System.Threading.Thread.Sleep(100);

        //    return OutBuf;
        //}


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

        public bool Search(string textToFind, StringComparison compare, string add = "")
        {
            return false;
        }
    }
}
