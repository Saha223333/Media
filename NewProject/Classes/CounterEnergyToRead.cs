using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Drawing;

namespace NewProject
{
    public class CounterEnergyToRead : IParametersToRead
    {
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе        
        public byte[] bytes { get; set; } //байты запроса   
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра     
        public string lastTime { get; set; }
        public double lastValueZone0 { get; set; } //последнее значение суммы                       
        public double lastValueZone1 { get; set; } //последнее значение первого тарифа
        public double lastValueZone2 { get; set; } //последнее значение второго тарифа
        public double lastValueZone3 { get; set; } //последнее значение третьего тарифа
        public double lastValueZone4 { get; set; } //последнее значение четвёртого тарифа      

        public CounterEnergyToRead(string name, byte[] bytes, byte bytesToWait, bool check, double lastValue0, double lastValue1, double lastValue2, double lastValue3, double lastValue4, string lastTime)
        {
            this.name = name; 
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = bytesToWait;
            this.lastValueZone1 = lastValue1;
            this.lastValueZone2 = lastValue2;
            this.lastValueZone3 = lastValue3;
            this.lastValueZone4 = lastValue4;
            this.lastValueZone0 = lastValue0;
            this.lastTime = lastTime;
        }
        public void spreadValueByName(string paramName, double value)
        {//процедура, распределяющая значения по полям класса, в зависимости от названия
         //пришлось ввести ввиду цикла по параметрам
            switch (paramName)
            {
                case "lastValueZone0":
                    {//принимаем значение если оно больше последнего
                        if (value > lastValueZone0)
                        { this.lastValueZone0 = value; }
                    }
                     break;
                case "lastValueZone1":
                    {//принимаем значение если оно больше последнего
                        if (value > lastValueZone1)
                        { this.lastValueZone1 = value; }
                    }
                    break;
                case "lastValueZone2":
                    {//принимаем значение если оно больше последнего
                        if (value > lastValueZone2)
                        { this.lastValueZone2 = value; }
                    }
                    break; 
                case "lastValueZone3": 
                    {//принимаем значение если оно больше последнего
                        if (value > lastValueZone3)
                        { this.lastValueZone3 = value; }
                    }
                    break;
                case "lastValueZone4": 
                    {//принимаем значение если оно больше последнего
                        if (value > lastValueZone4)
                        { this.lastValueZone4 = value; }
                    }
                    break;
            }
            this.lastTime = DateTime.Now.ToString();
        }

        public void saveToDataBase(string serial_number, RichTextBox richText)
        {//процедура сохранения значений энергии в базу
            SqlException ex =  DataBaseManagerMSSQL.Create_EnergyRS_Row(serial_number, this.lastValueZone0, this.lastValueZone1, this.lastValueZone2, this.lastValueZone3, this.lastValueZone4, this.name);
           
            if (ex != null)
            {
                if (ex.Number == 2627) return;//если duplicate key то просто молча выходим
                else//иначе пишем ошибку
                {
                    richText.Invoke(new Action(delegate
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.SelectionColor = Color.Red;
                        richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка записи показаний в базу: " + ex.Message + "\r");
                        richText.ScrollToCaret();
                    }));
                }
            }
        }
    }
}
