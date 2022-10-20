using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Collections.Generic;

namespace NewProject
{
    public interface ICounter : IDevice
    {
        int Divider { get; set; }//хранит значение делителя для вычисления энергии (1000 или с учётом постоянной счётчика)
        int NetAddress { get; set; }   //сетевой вдрес
        int CounterConst { get; set; }// постоянная сч-ка. Запоминается при считывании этого параметра       
        string SerialNumber { get; set; } //серийный номер
        int TransformationRate { get; set; }//коэффициент трансформации (берётся из БД)

        DataTable ProfileDataTable { get; set; } //таблица, хранящая снятый профиль
        BindingList<CounterEnergyToRead> EnergyToRead { get; set; } //перечень энергии для опроса счётчиков с цифровым интерфейсом       
        BindingList<CounterJournalToRead> JournalToRead { get; set; } //перечень журнала для счётчиков с цифровым интерфейсом
        BindingList<CounterParameterToRead> ParametersToRead { get; set; } //перечень параметров для опроса счётчиков с цифровым интерфейсом      
        BindingList<CounterParameterToWrite> ParametersToWrite { get; set; } //перечень параметров для записи в счётчики с цифровым интерфейсом      
        BindingList<CounterMonitorParameterToRead> MonitorToRead { get; set; } //перечень параметров тока (монитор) для счётчиков с цифровым интерфейсом     
        BindingList<CounterJournalCQCToRead> JournalCQCToRead { get; set; }//журнал считывания ПКЭ

        void ReadMonitorOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker);//метод считывания параметров тока ЧЕРЕЗ МОДЕМ
        void ReadMonitorOnGate(Mercury228 gate, string workingPort, DataProcessing dp, ToolStripProgressBar pb, PictureBox pixBox, RichTextBox richText, ref BackgroundWorker worker);//метод считывания параметров тока ЧЕРЕЗ ШЛЮЗ
        bool GainAccessOnModem(DataProcessing dp, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait);//метод открытия канала к счётчику ЧЕРЕЗ МОДЕМ
        bool GainAccessOnGate(Mercury228 gate, DataProcessing dp, int packnum, RichTextBox richText, byte lvl, byte pwd, ref BackgroundWorker worker, int bytestowait);//метод открытия канала к счётчику ЧЕРЕЗ ШЛЮЗ
        string ParseParameterValue(byte[] array, string pname, int offset);//разбирает байты параметра из массива ответа
        void ReadEnergyOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText);//этот метод полностью реализует чтение энергии счётчика по его правилам ЧЕРЕЗ МОДЕМ
        void ReadJournalOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText);//этот метод полностью реализует чтение журнала счётчика по его правилам ЧЕРЕЗ МОДЕМ
        void ReadJournalCQCOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText);//этот метод полностью реализует чтение журнала ПКЭ счётчика по его правилам ЧЕРЕЗ МОДЕМ
        void ReadJournalCQCOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum);//этот метод полностью реализует чтение журнала ПКЭ счётчика по его правилам ЧЕРЕЗ ШЛЮЗ (в пакетном режиме)   
        void ReadParametersOnModem(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Modem modem);////этот метод полностью реализует чтение параметров счётчика по его правилам ЧЕРЕЗ МОДЕМ
        void ReadParametersOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum);//этот метод полностью реализует чтение параметров счётчика по его правилам ЧЕРЕЗ ШЛЮЗ (в пакетном режиме)
        void ReadEnergyOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum);//этот метод полностью реализует чтение энергии счётчика по его правилам ЧЕРЕЗ ШЛЮЗ (в пакетном режиме)
        void ReadJournalOnGate(DataProcessing dp, BackgroundWorker worker, RichTextBox richText, Mercury228 gate, int packnum);//этот метод полностью реализует чтение журнала счётчика по его правилам ЧЕРЕЗ ШЛЮЗ (в пакетном режиме)         
        double ParseEnergyValue(DataProcessing dp, byte[] array, int offset);//разбирает байты энергии из массива ответа
        double ParseMonitorValue(DataProcessing dp, byte[] array, string pname, int phaseNo, int offset);//разбирает байты паараметров тока из массива ответа
        byte[] FormTestArray(DataProcessing dp);//процедура формирования запроса на тест связи
        byte[] FormEnergyArray(DataProcessing dp, CounterEnergyToRead energy, int zoneNo = 0);//процедура формирования запроса на чтение энергии
        byte[] FormJournalArray(DataProcessing dp, CounterJournalToRead journal, int recNo);//процедура формирования запроса на чтение журнала
        byte[] FormMonitorArray(DataProcessing dp, CounterMonitorParameterToRead monitor);//процедура формирования запроса на чтение параметров тока
        byte[] FormParameterArray(DataProcessing dp, CounterParameterToRead param);//процедура формирования запроса для чтения параметра в виде исходящего массива байт
        byte[] FormParameterArray(DataProcessing dp, CounterParameterToWrite param);//процедура формирования запроса для записи параметра в виде исходящего массива байт
        Bitmap DrawVectorDiagramm(PictureBox pixBox, RichTextBox richText);//картинка с векторной диаграммой
        byte[] FormGainAccessArray(DataProcessing dp, byte lvl, byte pwd);//процедура формирования запроса на открытие канала
        byte[] FormPowerProfileArray(DataProcessing dp, byte RecordAddressHi, byte RecordAddressLow, byte MemoryNumber, byte BytesInfo);//процедура формирования запроса на чтение записи профиля с указанным адресом
        bool ValidateReadParameterAnswer(byte[] answer, byte commandCode = 0, int offset = 0);//здесь счётчик будет сам анализировать успешность выполнения запросов чтения и вернёт ответ в вызывающее устройство (модем, шлюз и т.д.)
        byte[] FormLastProfileRecordArray(DataProcessing dp);//процедура формирования запроса на чтение последней записи профиля       
        void LoadLastEnergyIntoEnergyList();//здесь тащим из базы данных последние значения энергии
        string ValidateWriteParameterAnswer(byte[] InArray, byte offset);//здесь счётчик будет сам анализировать успешность выполнения запросов записи и вернёт ответ в вызывающее устройство (модем, шлюз и т.д.)                          
        byte[] FormParamNewValueArray(List<FieldsValuesToWrite> formControlValuesList, string paramName, byte[] additional = null, char stringDivider = '/');//процедруа, призванная сформировать массив байтов будущего значения параметра исходя из поданной строки (формируется в интерфейсе)
        DataTable GetPowerProfileForCounterOnModem(string workingPort, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent);
        DataTable GetPowerProfileForCounterOnGate(string workingPort, Mercury228 gate, DataProcessing dp, ToolStripProgressBar pb,
            DateTime DateN, DateTime DateK, int count, ToolStripLabel crl, ToolStripLabel lrl, RichTextBox richText, BackgroundWorker worker, bool ReReadOnlyAbsent, int packnum);
    }
}
