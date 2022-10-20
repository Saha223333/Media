namespace NewProject
{
    public interface IParametersToWrite
    {
        byte[] bytes { get; set; } //байты запроса
        bool check { get; set; } //галочка записывать или нет
        string name { get; set; } //название параметра         
        byte[] value { get; set; } //байты нового значения
        byte[] additional { get; set; } //дополнительные параметры
    }
}

