using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class Mercury228ParametersToRead : IParametersToRead
    {       
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра         
        public string value { get; set; } //значение
        public byte port { get; set; } //номер порта шлюза, в который подаётся конкретный запрос
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе        

        public Mercury228ParametersToRead(string name, byte[] bytes, bool check, byte pport, byte btw)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = btw;
            this.port = pport;
        }
    }
}
