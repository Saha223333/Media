using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class CounterParameterToWrite : IParametersToWrite
    {
        public byte[] bytes { get; set; } //байты запроса
        public byte[] value { get; set; } //байты нового значения, которое пойдёт в устройство (счётчик)
        public bool check { get; set; } //галочка записывать или нет
        public string name { get; set; } //название параметра   
        public byte[] additional { get; set; } //дополнительные параметры

        public CounterParameterToWrite(string name, byte[] bytes, bool check, byte[] newval, byte[] additional = null)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.value = newval;
            this.additional = additional;
        }
    }
}
