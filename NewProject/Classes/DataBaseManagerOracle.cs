using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OracleClient;
using System.Data;
using System.Windows.Forms;

namespace NewProject
{
    public class DataBaseManagerOracle
    {
        private string ConnectionString;
        private RichTextBox richText;

        public DataBaseManagerOracle(string pConnectionString, RichTextBox rtb)
        {
            this.ConnectionString = pConnectionString;
            this.richText = rtb;
        }

        public bool ConnectToDB()
        {
            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        public DataTable SelectRoll(string roll_id)
        {//выбираем показания из реестра
            DataTable table = new DataTable();
            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            {
                connection.Open();
                using (OracleCommand command = new OracleCommand("select * from sch_val where roll = " + roll_id.ToString(), connection))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                        table.Load(command.ExecuteReader());
                    }
                    catch (Exception ex)
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new System.Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения реестра №" + roll_id.ToString() + ": " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return null;
                    }
                }
            }
            return table;
        }

        public void DeleteRoll(string roll_id)
        {//удаляем пустой реестр
            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            {
                connection.Open();
                using (OracleCommand command = new OracleCommand("delete from roll where id = " + roll_id.ToString(), connection))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new System.Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка удаления реестра №" + roll_id.ToString() + ": " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                    }
                }
            }
        }

        public int InsertNewRoll(int source_id)
        {//здесь вставляем новый реестр в АСКУЭ
            int new_roll_id = -1;
            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            {
                connection.Open();                
                using (OracleCommand command = new OracleCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.CommandText = "askue_proc.get_roll_id";

                        OracleParameter tmpVar = new OracleParameter();
                        tmpVar.ParameterName = "tmpVar";
                        tmpVar.Direction = System.Data.ParameterDirection.ReturnValue;
                        tmpVar.OracleType = OracleType.Number;
                        command.Parameters.Add(tmpVar);
                        command.ExecuteNonQuery();

                        new_roll_id = Convert.ToInt16(command.Parameters["tmpVar"].Value);
                    }
                    catch (Exception ex)
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new System.Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка получения номера последнего реестра: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return -1;
                    }
                }
                //вставляем новый реестр
                using (OracleCommand command = new OracleCommand("insert into roll values (" + new_roll_id + ", " + "trunc(sysdate), " + source_id.ToString() + ")", connection))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new System.Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка вставки нового реестра: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return -1;
                    }
                }
            }

            return new_roll_id;//возвращаем номер нового реестра
        }

        public void InsertIntoSchVal(string roll, string askue_src, string inter_type, string serial_number, string val_date, string sch_val1, int val_num)
        {//здесь вставляем новую запись в таблицу SchVal
            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            {
                //отрезаем время, оставляем только дату
                val_date = val_date.Remove(10);
                connection.Open();
                using (OracleCommand command = new OracleCommand(
                    " Insert into askue.sch_val(roll, askue_src, inter_type, ser_num, val_date, sch_val1, val_num) values " +
                    " (" + roll + "," + askue_src + "," + inter_type + ",'" + serial_number + "', '" + val_date + "',ceil(" + sch_val1 + ")," + val_num + ")", connection))
                {
                    try
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        DateTime currentDate = DateTime.Now; //запоминаем текущее время для отображения в логе
                        richText.Invoke(new System.Action(delegate
                        {
                            richText.AppendText(currentDate + "." + currentDate.Millisecond + " Ошибка вставки записи в таблицу Sch_Val: " + ex.Message + "\r");
                            richText.ScrollToCaret();
                        }));
                        return;
                    }
                }
            }
        }
    }
}
