using System;

namespace NewProject
{
    public class MercuryRS485ParametersToWrite : IParametersToWrite
    {
        public byte[] bytes { get; set; } //байты запроса
        public byte[] value { get; set; } //байты нового значения, которое пойдёт в устройство (счётчик)
        public bool check { get; set; } //галочка записывать или нет
        public string name { get; set; } //название параметра   
        public byte[] additional { get; set; } //дополнительные параметры

        public MercuryRS485ParametersToWrite(string name, byte[] bytes, bool check, byte[] newval)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.value = newval;          
        }

        public void FormParamNewValueArray(string newValueStr, int aux = 0, char stringDivider = '/')
        {//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки (формируется в интерфейсе)
            byte[] result = new byte[0];
            switch (this.bytes[1])
            {
                case 0xC://установка времени
                    {
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
                        this.value = result;
                        break;
                    }

                case 0xD://коррекция времени в пределах 4 мин
                    {
                        Array.Resize(ref result, 3);
                        result[0] = Convert.ToByte(newValueStr.Substring(18, 2).PadLeft(2, '0'), 16);//секунды
                        result[1] = Convert.ToByte(newValueStr.Substring(15, 2).PadLeft(2, '0'), 16);//минуты
                        result[2] = Convert.ToByte(newValueStr.Substring(12, 2).PadLeft(2, '0'), 16);//часы
                        this.value = result;
                        break;
                    }

                case 0x0://инициализация профиля
                    {
                        Array.Resize(ref result, 2);
                        result[0] = (Convert.ToByte(newValueStr));//период интегрирования
                        result[1] = (byte)aux;//дополнительный параметр стирать память или нет
                        this.value = result;
                        break;
                    }
            }
        }
    }
}
