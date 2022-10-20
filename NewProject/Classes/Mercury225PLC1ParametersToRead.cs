using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;


namespace NewProject
{
    public class Mercury225PLC1ParametersToRead : IParametersToRead
    {      
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра         
        public string value { get; set; } //значение
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе    

        public Mercury225PLC1ParametersToRead(string name, byte[] bytes, bool check)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
        }
    }
}
