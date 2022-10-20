using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.Data;
using System.Security.Permissions;

namespace NewProject
{
    public class ReportsSender
    {
        private Microsoft.Office.Interop.Outlook.Application outlk;
        public ReportsSender()
        {                    
            try
            {             
                if (Process.GetProcessesByName("OUTLOOK").Count() > 0)
                {//если почтовый клиент запущен, то достаточно получить его процесс               
                    outlk = Marshal.GetActiveObject("Outlook.Application") as Microsoft.Office.Interop.Outlook.Application;
                }
                else
                {//если почтовый клиент не запущен, то нужно его создать  
                    //outlk = new Microsoft.Office.Interop.Outlook.Application();
                    //Microsoft.Office.Interop.Outlook.NameSpace nameSpace = outlk.GetNamespace("MAPI");
                    //nameSpace.Logon("protasov@skek.ru", "y8Fr_I9uU", Missing.Value, Missing.Value);
                    //nameSpace = null;
                    return;
                }
            }
            catch
            {
                return;
            }
        }

        public void SendMail(string filename, DataTable recipients, string subject)
        {
            try
            {
                //создаём письмо
                Microsoft.Office.Interop.Outlook.MailItem oMsg = (Microsoft.Office.Interop.Outlook.MailItem)outlk.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                oMsg.Body = "123";//описываем тело письма
                int iPosition = (int)oMsg.Body.Length + 1;
                int iAttachType = (int)Microsoft.Office.Interop.Outlook.OlAttachmentType.olByValue;
                //прикрепляем файл
                if (filename != String.Empty)
                {//если строка пути к файлу не пуста
                    try
                    {
                        Microsoft.Office.Interop.Outlook.Attachment oAttach = oMsg.Attachments.Add(@filename, iAttachType, iPosition);
                    }
                    catch
                    {

                    }
                }
                oMsg.Subject = subject;
                Microsoft.Office.Interop.Outlook.Recipients oRecips = oMsg.Recipients;
                // Change the recipient in the next line if necessary.
                foreach (DataRow recipient in recipients.Rows)
                {
                    Microsoft.Office.Interop.Outlook.Recipient oRecip = oRecips.Add(recipient["email"].ToString());
                    oRecip.Resolve();
                }                             
                //отправляем письмо
                oMsg.Send();
                //убираем ссылки перменные чтобы сборщик мусора потом выгрузил их из памяти              
                oRecips = null;
                oMsg = null;
                outlk = null;
            }
            catch
            {
                return;
            }
            finally
            {
                outlk = null;
            }
        }

        public void SendMail(DataTable recipients, string subject)
        {
            try
            {
                //создаём письмо
                Microsoft.Office.Interop.Outlook.MailItem oMsg = (Microsoft.Office.Interop.Outlook.MailItem)outlk.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                oMsg.Body = "123";//описываем тело письма
                int iPosition = (int)oMsg.Body.Length + 1;
                
                oMsg.Subject = subject;
                Microsoft.Office.Interop.Outlook.Recipients oRecips = oMsg.Recipients;
                // Change the recipient in the next line if necessary.
                foreach (DataRow recipient in recipients.Rows)
                {
                    Microsoft.Office.Interop.Outlook.Recipient oRecip = oRecips.Add(recipient["email"].ToString());
                    oRecip.Resolve();
                }
                //отправляем письмо
                oMsg.Send();
                //убираем ссылки перменные чтобы сборщик мусора потом выгрузил их из памяти              
                oRecips = null;
                oMsg = null;
                outlk = null;
            }
            catch
            {
                return;
            }
            finally
            {
                outlk = null;
            }
        }

        public void SendMail(string[] recipients, string subject, string body)
        {
            try
            {
                //создаём письмо
                Microsoft.Office.Interop.Outlook.MailItem oMsg = (Microsoft.Office.Interop.Outlook.MailItem)outlk.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                oMsg.Body = body;//описываем тело письма
                int iPosition = (int)oMsg.Body.Length + 1;
                
                oMsg.Subject = subject;
                Microsoft.Office.Interop.Outlook.Recipients oRecips = oMsg.Recipients;
                // Change the recipient in the next line if necessary.
                foreach (string recipient in recipients)
                {
                    Microsoft.Office.Interop.Outlook.Recipient oRecip = oRecips.Add(recipient);
                    oRecip.Resolve();
                }
                //отправляем письмо
                oMsg.Send();
                //убираем ссылки перменные чтобы сборщик мусора потом выгрузил их из памяти              
                oRecips = null;
                oMsg = null;
                outlk = null;
            }
            catch
            {
                return;
            }
            finally
            {
                outlk = null;
            }
        }
    }
}
