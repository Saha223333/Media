using System.Collections;
using System.Data;

namespace NewProject
{
    public class CounterJournalCQCToRead : IParametersToRead
    {
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе        
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра         

        public DataTable ValuesTable;//таблица для хранения значений

        public CounterJournalCQCToRead(string name, byte[] bytes, byte btw, bool check)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = btw;
            //описываем таблицу, которая будет хранить массив значений параметра
            ValuesTable = new DataTable("ValuesTable"); ValuesTable.Clear();
            //добавляем столбцы
            DataColumn dc = new DataColumn("dummy1"); dc.AllowDBNull = true;
            ValuesTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково

            dc = new DataColumn("dummy2"); dc.AllowDBNull = true;
            ValuesTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково

            dc = new DataColumn("dummy3"); dc.AllowDBNull = true;
            ValuesTable.Columns.Add(dc);//пустышка для выгрузки в Excel. Нужна чтобы алгоритм выгрузки работал для всех гридов одинаково          

            dc = new DataColumn("time_off_limits"); ValuesTable.Columns.Add(dc);//время выхода за допустимые пределы
            dc = new DataColumn("time_in_limits"); ValuesTable.Columns.Add(dc);//время возврата
        }     
    }
}
