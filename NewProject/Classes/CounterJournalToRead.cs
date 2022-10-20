using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class CounterJournalToRead : IParametersToRead
    {
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе        
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра         
        public string record1{ get; set; } //значение 1
        public string record2 { get; set; } //значение 2
        public string record3 { get; set; } //значение 3
        public string record4 { get; set; } //значение 4
        public string record5 { get; set; } //..
        public string record6 { get; set; } //..
        public string record7 { get; set; } //..
        public string record8 { get; set; }
        public string record9 { get; set; }
        public string record10 { get; set; }

        public CounterJournalToRead(string name, byte[] bytes, byte btw, bool check)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = btw;
        }

        public void spreadValueByName(string paramName, string value)
        {//процедура, распределяющая значения по полям класса, в зависимости от названия
         //пришлось ввести ввиду цикла по параметрам
            switch (paramName)
            {
                case "record0": { this.record1 = value; } break;
                case "record1": { this.record2 = value; } break;
                case "record2": { this.record3 = value; } break;
                case "record3": { this.record4 = value; } break;
                case "record4": { this.record5 = value; } break;
                case "record5": { this.record6 = value; } break;
                case "record6": { this.record7 = value; } break;
                case "record7": { this.record8 = value; } break;
                case "record8": { this.record9 = value; } break;
                case "record9": { this.record10 = value; } break;
            }
        }
    }
}
