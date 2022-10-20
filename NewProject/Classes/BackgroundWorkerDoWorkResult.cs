using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{
    public class BackgroundWorkerDoWorkResult
    {//структура для передачи в BackgroundWorker.Result чтобы обработчик события DoWorkComplete получил нужные значения и обработал их (закрыл порт, закрыл форму и т.д)
        public DataProcessing dp;
        public ReadingLogForm rlf;
        public string taskname;
        public bool autoClosePort;

        public BackgroundWorkerDoWorkResult(DataProcessing pdp, ReadingLogForm prlf, string taskname, bool autoClosePort)
        {
            this.dp = pdp;//экземпляр обработчика данных чтобы закрыть порт после работы
            this.rlf = prlf;//экземпляр формы лога чтобы закрыть её или нет после работы и не дать пользователю сделать это до завершения работы
            this.taskname = taskname;
            this.autoClosePort = autoClosePort;//закрывать автоматически порт или нет. Можно и нужно это делать при опросе группы вручную или по расписанию. Но нельзя это делать при опросе профиля после ручног дозвона
        }
    }
}
