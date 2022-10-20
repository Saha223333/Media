namespace NewProject
{
    public class CounterParameterToRead : IParametersToRead
    {         
        public byte[] bytes { get; set; } //байты запроса
        public bool check { get; set; } //галочка считывать или нет
        public string name { get; set; } //название параметра              
        public string value { get; set; } //значение после считывания
        public byte bytesToWait { get; set; } //сколько байт ожидаем в ответе       

        public CounterParameterToRead(string name, byte[] bytes, byte btw, bool check)
        {
            this.name = name;
            this.bytes = bytes;
            this.check = check;
            this.bytesToWait = btw;
        }
    }
}
