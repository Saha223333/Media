namespace NewProject
{
    public interface IParametersToRead
    {
        byte[] bytes { get; set; } //байты запроса
        bool check { get; set; } //галочка считывать или нет
        string name { get; set; } //название параметра         
        byte bytesToWait { get; set; } //сколько байт ожидаем в ответе 
    }
}
