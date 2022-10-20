using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class CounterMonitorParameterToRead : IParametersToRead
    {
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе        
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра    
        public double phase0 { get; set; } //значение суммы     
        public double phase1 { get; set; } //значение фазы 1
        public double phase2 { get; set; } //значение фазы 2
        public double phase3 { get; set; } //значение фазы 3      

        public CounterMonitorParameterToRead(string name, byte[] bytes, byte btw, bool check)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = btw;
        }

        public void spreadValueByName(string phaseName, double value)
        {//процедура, распределяющая значения по полям класса, в зависимости от названия
         //пришлось ввести в виду цикла по параметрам
            switch (phaseName)
            {
                case "phase0":
                    {//принимаем значение суммы                       
                        { this.phase0 = value; }
                    }
                    break;
                case "phase1":
                    {//принимаем значение фазы 1
                        { this.phase1 = value; }
                    }
                    break;
                case "phase2":
                    {//принимаем значение фазы 2                  
                        { this.phase2 = value; }
                    }
                    break;
                case "phase3":
                    {//принимаем значение фазы 3                 
                        {  this.phase3 = value; }
                    }
                    break;
            }
        }
    }
}
