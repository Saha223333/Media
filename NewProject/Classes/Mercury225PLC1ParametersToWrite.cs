using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class Mercury225PLC1ParametersToWrite : IParametersToWrite
    {
        public byte[] bytes { get; set; } //байты запроса
        public byte[] value { get; set; } //байты нового значения, которое пойдёт в устройство (концентратор)
        public bool check { get; set; } //галочка записывать или нет
        public string name { get; set; } //название параметра             
        public byte[] additional { get; set; } //дополнительные параметры

        public Mercury225PLC1ParametersToWrite(string name, byte[] bytes, bool check,  byte[] newval)
        {
            this.name = name;
            this.bytes = bytes;
            this.value = newval;
            this.check = check;
        }

        public void FormParamNewValueArray(string newValueStr)
        {//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки (формируется в интерфейсе)
            byte[] result = new byte[0];
            switch (this.name)
            {
                case "Скорость порта":
                    {
                        Array.Resize(ref result, 1);
                        switch (newValueStr)
                        {
                            case "38400":                            
                                result[0] = 0; break;
                            case "19200":
                                result[0] = 1; break;
                            case "9600":
                                result[0] = 2; break;
                        }
                        this.value = result;
                        break;
                    }

                case "Дата и время":
                    {
                        Array.Resize(ref result, 7);

                        result[0] = (byte)DateTime.Now.Second;
                        result[1] = (byte)DateTime.Now.Minute;
                        result[2] = (byte)DateTime.Now.Hour;
                        result[3] = (byte)(DateTime.Now.DayOfWeek - 1);
                        result[4] = (byte)(DateTime.Now.Day - 1);
                        result[5] = (byte)(DateTime.Now.Month - 1);
                        result[6] = (byte)(DateTime.Now.Year - 2000);

                        //result[0] = Convert.ToByte((DateTime.Now.Second).ToString().PadLeft(2, '0'), 16);
                        //result[1] = Convert.ToByte((DateTime.Now.Minute).ToString().PadLeft(2, '0'), 16);
                        //result[2] = Convert.ToByte((DateTime.Now.Hour).ToString().PadLeft(2, '0'), 16);
                        //result[3] = (byte)(DateTime.Now.DayOfWeek - 1);
                        //result[4] = Convert.ToByte((DateTime.Now.Day - 1).ToString().PadLeft(2, '0'), 16);
                        //result[5] = Convert.ToByte((DateTime.Now.Month - 1).ToString().PadLeft(2, '0'), 16);
                        //result[6] = Convert.ToByte((DateTime.Now.Year - 2000).ToString().PadLeft(2, '0'), 16);

                        this.value = result;
                        break;
                    }

                case "Конфигурация":
                    {
                        Array.Resize(ref result, 3);
                        //пытаемся получить 16ти-ричное представление числа ёмкости сети для отправки на устройство
                        int netsize10 = Convert.ToInt16(newValueStr.Substring(0, newValueStr.IndexOf('/')).PadLeft(4, '0'), 10);
                        string netsize16 = netsize10.ToString("X").PadLeft(4, '0');
                        result[0] = Convert.ToByte(netsize16.Substring(2, 2).PadLeft(2, '0'), 16);//младший байт ёмкости сети
                        result[1] = Convert.ToByte(netsize16.Substring(0, 2).PadLeft(2, '0'), 16);//старший байт ёмкости сети
                        string mode = newValueStr.Substring(newValueStr.IndexOf("/") + 1);
                        switch (mode)
                        {
                            case "Обычный": result[2] = 0; break;   //байт конфигурации 00000000
                            case "Master (SR)": result[2] = 4; break; //байт конфигурации 000000100
                            case "Slave (SRT)": result[2] = 8; break; //байт конфигурации 000001000
                            case "Slave (SR)": result[2] = 0x000C; break; //байт конфигурации 000001100
                        }

                        this.value = result;
                        break;
                    }

                case "Сетевой адрес":
                    {
                        Array.Resize(ref result, 7);

                        this.value = result;
                        break;
                    }

                case "Расчётный день":
                    {
                        Array.Resize(ref result, 1);
                        result[0] = Convert.ToByte(newValueStr);
                        this.value = result;
                        break;
                    }
            }
        }
    }
}
