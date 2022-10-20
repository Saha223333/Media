using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;

namespace NewProject
{
    public class User
    {
        public static byte ID;//идентификатор
        public static string Login;//логин
        public static string Name;//имя
        public static string Role;//роль

        public static void Identify_User()
        {//здесь определяем что за пользователь вошёл в систему
            DataTable dt = DataBaseManagerMSSQL.Return_User_Credentials();
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Вашей учётной записи нет в программе!", "Ошибка учётной записи", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(Environment.ExitCode);//выходим из приложения
            }

            User.ID = (byte)dt.Rows[0]["id"];
            User.Login = dt.Rows[0]["login"].ToString();
            User.Name = dt.Rows[0]["name"].ToString();
            User.Role = dt.Rows[0]["role"].ToString();
        }
    } 
}
