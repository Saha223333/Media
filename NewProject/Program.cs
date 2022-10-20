using System;
using System.Threading;
using System.Windows.Forms;


namespace NewProject
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
       {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
         
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);//это событие вызывается при возникновении исключения в интерфейсе, и его можно игнорировать, продолжив выполнение программы
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            /*To catch exceptions that occur in threads not created and owned by Windows Forms, use the AppDomain.UnhandledException.It allows the application to log information about the exception before the system default handler reports the exception to the user and terminates the application. The handling of this exception does not prevent application to be terminated.
              The maximum that could be done (program data can become corrupted when exceptions are not handled) is saving program data for later recovery. After that the application domain is unloaded and the application terminates.
            */
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);//исключения, возникшие вне пользовательского интерфейса в любом случае обрушат приложение. См комментарий выше

            Application.Run(new MainForm());//запускает стандартный цикл обработки сообщений приложения в текущем потоке и делает указанную форму видимой
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {   //отправляем письмо с сообщением об ошибке
            //string[] recips = new string[1]; recips[0] = "gtr54@yandex.ru";
            //ReportsSender rs = new ReportsSender();
            //rs.SendMail(recips, "Возникло необработанное исключение в пользовательском интерфейсе", e.Exception.Message + ". Источник: " + e.Exception.Source + ". Стек вызовов: " + e.Exception.StackTrace);
            //MessageBox.Show(e.Exception.Message, "Возникло необработанное исключение в пользовательском интерфейсе");
            
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //string[] recips = new string[1]; recips[0] = "gtr54@yandex.ru";
            //ReportsSender rs = new ReportsSender();
            //rs.SendMail(recips, "Возникло необработанное исключение в домене приложения", (e.ExceptionObject as Exception).Message);
            //MessageBox.Show((e.ExceptionObject as Exception).Message, "Возникло необработанное исключение в домене приложения");
        }
    }
}
