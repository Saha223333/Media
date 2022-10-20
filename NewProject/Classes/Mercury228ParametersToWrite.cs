 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NewProject
{
    public class Mercury228ParametersToWrite : IParametersToWrite
    {
        public byte[] bytes { get; set; } //байты запроса
        public byte[] value { get; set; } //байты нового значения
        public bool check { get; set; } //галочка записывать или нет
        public string name { get; set; } //название параметра               
        public byte[] additional { get; set; } //дополнительные параметры

        public Mercury228ParametersToWrite(string pname, byte[] pbytes, bool pcheck, byte[] pnewval)
        {
            this.name = pname;
            this.bytes = pbytes;
            this.value = pnewval;
            this.check = pcheck;
        }

        public void FormParamNewValueArray(string newValueStr)
        {//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки       
            string valueResultStr = String.Empty;//строка представляющая результирующий массив для последующего перевода в массив байт
            if ((name == "Настройки порта 1") || (name == "Настройки порта 2"))
            {
             //отныне сама процедура разбора строки с настройками порта статически 
             //реализована в классе шлюза, чтобы можно было вызывать из других мест
             //без создания экземпляра класса
                valueResultStr = Mercury228.FormConfigString(newValueStr);

                value[0] = Convert.ToByte(valueResultStr.Substring(0, 8), 2);//получаем первый байт (поле UART) будущих настроек порта из двоичной строки
                value[1] = Convert.ToByte(valueResultStr.Substring(8, 8), 2);//получаем второй байт (поле PAUSE) будущих настроек порта из двоичной строки
                value[2] = Convert.ToByte(valueResultStr.Substring(16, 8), 2);//получаем третий байт (поле WAIT) будущих настроек порта из двоичной строки
            }
        }
    }
}
