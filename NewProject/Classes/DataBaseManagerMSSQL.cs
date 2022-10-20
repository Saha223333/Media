using System;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace NewProject
{
    public class DataBaseManagerMSSQL
    {
        public static bool ConnectToDB(string servName, string dbName, string userID, string pwd)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder["Server"] = servName;
            builder["Integrated Security"] = false;
            builder["Database"] = dbName;
            builder["User ID"] = userID;
            builder["Password"] = pwd;

            Properties.Settings.Default.ConnectionString = builder.ConnectionString;

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                try
                {                  
                    connection.Open();                    
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public int count()
        {//здесь считаем сколько у нас в базе подключений
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand("Select count(id) from dbo.connection_points", connection))
                {
                    try
                    {
                        return (int)cmd.ExecuteScalar();                   
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }

             return -1;
        }            

        public DataSet CreateWorkDataSet(int i)
        {   //при подключении к базе данных помещаем необходимые объекты в один датасет при помощи множественных запросов
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //новый запрос, нацеленный на порционную загрузку объектов
                StringBuilder sb = new StringBuilder();
                sb.Append("declare @id_range_min int;");
                sb.Append("set @id_range_min = " + (i * 100).ToString() +";");
                sb.Append("Select * from connection_points a order by name offset @id_range_min rows fetch next 100 rows only;");
                sb.Append("Select * from concentrator_points a where a.id_connection in (select id from connection_points order by name offset @id_range_min rows fetch next 100 rows only);");
                sb.Append("Select * from counters_plc a where a.id_concentrator in (select id from concentrator_points where id_connection in (select id from connection_points order by name offset @id_range_min rows fetch next 100 rows only));");
                sb.Append("Select * from counters_rs a where a.id_connection in (select id from connection_points order by name offset @id_range_min rows fetch next 100 rows only);");
                sb.Append("Select * from power_profile a where a.id_counter in (select id from counters_rs where id_connection in (select id from connection_points order by name offset @id_range_min rows fetch next 100 rows only))");

                using (SqlDataAdapter da = new SqlDataAdapter(sb.ToString(), connection))

                //Это старый вид
                //using (SqlDataAdapter da = new SqlDataAdapter("select * from connection_points order by name;"
                //                                      + "Select * from concentrator_points;"
                //                                      + "Select * from counters_plc;"
                //                                      + "Select * from counters_rs;"
                //                                      + "Select * from power_profile;"
                //                                      + "Select * from task", connection))
                {
                    
                    DataSet ds = new DataSet();

                    da.Fill(ds);
                    ds.Tables[0].TableName = "connection_points";
                    ds.Tables[1].TableName = "concentrator_points";
                    ds.Tables[2].TableName = "counters_plc";
                    ds.Tables[3].TableName = "counters_rs";
                    ds.Tables[4].TableName = "power_profile";

                    return ds;
                }

            }
        }

        public static DataTable Return_Objects_Quantity()
        {
            //возвращаем количество объектов в системе
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                DataTable dt = new DataTable();
                string query =
                    "select 'Счётчики PLC', count(*) from media.dbo.counters_plc " +
                    "union " +
                    "select 'Счётчики RS485', count(*) from media.dbo.counters_rs " +
                    "union " +
                    "select 'Концентраторы', count(*) from media.dbo.concentrator_points " +
                    "union " +
                    "select 'Шлюзы', count(*) from media.dbo.connection_points where type_id = 2 " +
                    "union " +
                    "select 'Модемы', count(*) from media.dbo.connection_points where type_id = 1 ";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            dt.Load(dr);                            
                        }
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static Exception Create_Backup(string path)
        {//создаём резервную копию
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Backup", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("path", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = path; cmd.Parameters.Add(inParam);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка создания резервной копии базы данных", MessageBoxButtons.OK, MessageBoxIcon.Error);                     
                        return ex;
                    }
                    return null;
                }
            }
        }

        public static void Add_Peak_Hours_Period(string peakhoursperiod, int month)
        {//здесь добавляем часы-пик для указанного месяца
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "update dbo.Month_Peak_Hours set peak_hours = peak_hours + @peakhoursperiod where month_number = @month";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter param = new SqlParameter("peakhoursperiod", peakhoursperiod); param.DbType = DbType.String; cmd.Parameters.Add(param);
                    param = new SqlParameter("month", month); param.DbType = DbType.Int16; cmd.Parameters.Add(param);
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                }
            }
        }



        public static void Update_Calendar_Day(DateTime datetime, bool workday)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "update dbo.Calendar set work_day=" + Convert.ToInt16(workday).ToString() + " where day='" + datetime.ToString() + "'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {                         
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
                  
            }
        }

        public static string Return_Month_Peak_Hours(int month_number)
        {//возвращаем часы-пик для отчёта фактической мощности для текущего месяца
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.Month_Peak_Hours where month_number = @monthnumber";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter param = new SqlParameter("monthnumber", month_number); param.DbType = DbType.Int16; cmd.Parameters.Add(param);
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                        
                        MessageBox.Show(ex.Message.ToString());
                        return "0";
                    }
                }
                  
                return dt.Rows[0]["peak_hours"].ToString();
            }
        }

        public static DataTable Return_Calendar(DateTime DateN, DateTime DateK)
        {//процедура, возвращающая календарь с выходными за месяц
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.Calendar where day >= '" + DateN.Year + "-" + DateN.Month + "-" + DateN.Day + "' and day <= '" + DateK.Year + "-" + DateK.Month + "-" + DateK.Day + "'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Districts()
        {//процедура, возвращающая справочник районов
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.Districts order by name asc";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                       
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Streets()
        {//процедура, возвращающая справочник улиц
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select distinct street from COUNTERS_PLC "+"union "+"select distinct street from COUNTERS_RS "+"order by 1";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                 
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static SqlException Add_Profile_Record(int id_counter, string DateTime, string e_a_plus, string e_a_minus, string e_r_plus, string e_r_minus, byte period)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (SqlException sqlex)
                {
                    return sqlex;
                }
                //процедура вставки запись в таблицу профиля
                DateTime datetime = Convert.ToDateTime(DateTime);

                string query = "insert into dbo.power_profile (id_counter, date_time, e_a_plus, e_a_minus, e_r_plus, e_r_minus, period) VALUES "
                   + " (@id_counter, @datetime, @e_a_plus, @e_a_minus, @e_r_plus, @e_r_minus, @period)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter param = new SqlParameter("datetime", datetime); param.DbType = DbType.DateTime; cmd.Parameters.Add(param);
                    param = new SqlParameter("e_a_plus", Convert.ToDouble(e_a_plus)); param.DbType = DbType.Double; cmd.Parameters.Add(param);
                    param = new SqlParameter("e_a_minus", Convert.ToDouble(e_a_minus)); param.DbType = DbType.Double; cmd.Parameters.Add(param);
                    param = new SqlParameter("e_r_plus", Convert.ToDouble(e_r_plus)); param.DbType = DbType.Double; cmd.Parameters.Add(param);
                    param = new SqlParameter("e_r_minus", Convert.ToDouble(e_r_minus)); param.DbType = DbType.Double; cmd.Parameters.Add(param);
                    param = new SqlParameter("id_counter", id_counter); param.DbType = DbType.Int32; cmd.Parameters.Add(param);
                    param = new SqlParameter("period", period); param.DbType = DbType.Byte; cmd.Parameters.Add(param);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {                     
                        return ex;
                    }                     
                    return null;
                }
            }
        }

        public static Exception Add_Counter_To_Task(int node_id, int task_id)
        {//тут добавялем счётчик в расписание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "insert into dbo.task_grid values (@task_id, @node_id)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter param = new SqlParameter("task_id", task_id); param.DbType = DbType.Int16; cmd.Parameters.Add(param);
                    param = new SqlParameter("node_id", node_id); param.DbType = DbType.Int16; cmd.Parameters.Add(param);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {                    
                        return ex;
                    }              
                    return null;
                }
            }
        }

        public static int Add_Image(int object_id, string txtImagePath)
        {//тут добавялем картинку в базу
            try
            {
                using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.Create_Image_Row", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        Image img = Image.FromFile(txtImagePath);
                        using (MemoryStream tmpStream = new MemoryStream())//объявляем поток
                        {
                            img.Save(tmpStream, ImageFormat.Jpeg);//сохраняем изображение в указанный поток в указанном формате
                            tmpStream.Seek(0, SeekOrigin.Begin);//встаём в указанную позицию в потоке
                            byte[] imgBytes = new byte[tmpStream.Length];//объявляем массив байт в котором будет хранится картинка, длиной в количество байт в потоке
                            tmpStream.Read(imgBytes, 0, Convert.ToInt32(tmpStream.Length));//считываем из потока картинку в массив байт
                                                                                           //задаём входные параметры
                            SqlParameter inParam = new SqlParameter("id_object", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = object_id; cmd.Parameters.Add(inParam);
                            inParam = new SqlParameter("image", SqlDbType.Image); inParam.Direction = ParameterDirection.Input; inParam.Value = imgBytes; cmd.Parameters.Add(inParam);
                            SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);
                            //выполняем хранимую процедуру
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch
                            {              
                                return -1;
                            }
                              
                            return (int)outParam.Value;
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }
        }

        public static int Add_Image(int object_id, Image img)
        {//тут добавялем картинку в базу
            try
            {
                using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.Create_Image_Row", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (MemoryStream tmpStream = new MemoryStream())//объявляем поток байтов
                        {
                            img.Save(tmpStream, ImageFormat.Jpeg);//сохраняем изображение в указанный поток в указанном формате
                            tmpStream.Seek(0, SeekOrigin.Begin);//встаём в указанную позицию в потоке
                            byte[] imgBytes = new byte[tmpStream.Length];//объявляем массив байт в котором будет хранится картинка, длиной в количество байт в потоке
                            tmpStream.Read(imgBytes, 0, Convert.ToInt32(tmpStream.Length));//считываем из потока картинку в массив байт
                                                                                           //задаём входные параметры
                            SqlParameter inParam = new SqlParameter("id_object", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = object_id; cmd.Parameters.Add(inParam);
                            inParam = new SqlParameter("image", SqlDbType.Image); inParam.Direction = ParameterDirection.Input; inParam.Value = imgBytes; cmd.Parameters.Add(inParam);
                            SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);
                            //выполняем хранимую процедуру
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch
                            {                
                                return -1;
                            }
                              
                            return (int)outParam.Value;
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }
        }

        public static DataTable Return_Images(int id_object)
        {
            //процедура, достающая из базы картинки, привязанные к указанному объекту
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.images where id_object=" + id_object.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {              
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Task_Reports(int id_task)
        {
            //процедура, достающая из базы отчёты, назначенные заданию
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                DataTable dt = new DataTable();
                try
                {
                    connection.Open();
                }
                catch
                {
                    return dt;
                }              
                string query = "select * from dbo.task_report where id_task=" + id_task.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                            dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                 
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Add_Task_Reports(int id_task, string name, int sum)
        {
            //процедура, добавляющая отчёт для задания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "insert into dbo.task_report values (" + id_task.ToString() + ", N'" + name + "', " + sum.ToString() + ")";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 2627) MessageBox.Show("Такой отчёт уже есть: " + name, "Попытка добавить существующий отчёт для задания", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                      
                }
            }
        }

        public static void Delete_Image(int id)
        {//удаляем картинку по идентификатору
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "delete from dbo.images where id = " + id.ToString();
                SqlCommand cmd = new SqlCommand(query, connection);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
                  
            }
        }

        public static DataTable Return_User_Credentials()
        {//процедура, достающая из базы данные пользователя
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from USERS where login= '" + Environment.UserName + "'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                    
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Add_Task_Profile_Automation(int task_id, int automation_id)
        {//процедура, добавляющая автоматизацию для снятия профиля в задании (за предыдущие сутки, за предыдущий месяц...)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "insert into dbo.task_profile_automation values (" + task_id.ToString() + "," + automation_id.ToString() + ")";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {                 
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                }
            }
        }


        public static DataTable Return_Profile_Automations()
        {//процедура, достающая из базы все возможные типы автоматизации для профиля
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();

                DataTable dt = new DataTable();
                string query = "select * from dbo.profile_automations";
                SqlCommand cmd = new SqlCommand(query, connection);

                try
                {
                    using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                }
                catch (Exception ex)
                {                    
                    MessageBox.Show(ex.Message.ToString());
                }
                  
                return dt;
            }
        }

        public static DataTable Return_Task_Profile_Automation(int task_id)
        {//процедура, достающая из базы все автоматизации снятия профиля для выбранного задания (за предыдущие сутки, за предыдущий месяц...)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.task_profile_automation where task_id=" + task_id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                     
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Delete_Task_Profile_Automation(int task_id)
        {//процедура, удаляющая автоматизацию съема профиля (шаблон периода - за предыдущие сутки, за предыдущий месяц...)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "delete from dbo.task_profile_automation where task_id=" + task_id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) {   
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return;
                }
            }
        }

        public static void Delete_Node_From_Task_Grid(int id_node, int id_task)
        {//процедура, удаляющая узел из сетки расписания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "delete from task_grid where id_node = " + id_node.ToString() + " and id_task =" + id_task.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {

                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader());
                          
                    }
                    catch (Exception ex)
                    {                        
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                }
            }
        }

        public static void Whether_Admin_Exists()
        {//процедура, проверяющая, привязан ли логин к роли админа
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand("select id, login from USERS where role= 'admin'", connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                            dt.Load(dr);
                        //  
                        if (dt.Rows.Count == 0)
                        {//если нет роли админа (удалили), то автоматически создаём
                            Create_User(String.Empty, String.Empty, "admin");
                            MessageBox.Show("Роль администратора приложения не была найдена! Приложение будет закрыто. Для продолжения работы перезапустите приложение.",
                                "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(Environment.ExitCode);//выходим из приложения
                        }
                        //если есть роль админа, но к ней не привязан ни один логин, тогда предлагаем привязать текущий
                        string awe = dt.Rows[0][1].ToString();
                        if (awe == String.Empty)
                        {
                            DialogResult drslt = MessageBox.Show("Роли администратора программы не соответствует ни одна учётная запись. Использовать текущую?",
                                "Внимание!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (drslt == DialogResult.Yes)
                            {
                                Update_User((byte)dt.Rows[0][0], Environment.UserName, String.Empty, "admin");
                            }
                            else //если пользователь отказывается привязать его учётку к роли администратора, то создаём логин с минимальными правами
                            {
                                Create_User(Environment.UserName, String.Empty, "observer");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                }
            }
        }

        public static DataTable Return_Users()
        {//процедура, достающая из базы всех пользователей
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand("select * from USERS", connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static Exception Add_Task_Profile(int id_task, DateTime lower_datetime, DateTime upper_datetime)
        {//тут добавялем расписание профиля для задания (т.е. профиль для выбранного задания будет сниматься в заданное время по расписанию)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Task_Profile_Row", connection))
                {
                    connection.Open();
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id_task", SqlDbType.SmallInt); inParam.Direction = ParameterDirection.Input; inParam.Value = id_task; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("lower_datetime", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lower_datetime; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("upper_datetime", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = upper_datetime; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }

                    return null;
                }
            }
        }

        public static Exception Delete_Task_Report(int id_task, string report_name)
        {//тут удаляем отчёт для задания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "delete from dbo.task_report where id_task = " + id_task.ToString() + " and report_name = N'" + report_name + "'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {                          
                        return ex;
                    }
                      
                    return null;
                }
            }
        }

        public static Exception Delete_Task_Profile_Row(int id_task)
        {//тут удаляем расписание профиля для задания (т.е. профиль для выбранного задания будет сниматься в заданное время по расписанию)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "delete from dbo.task_profile where id_task = " + id_task.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {                         
                        return ex;
                    }
                      
                    return null;
                }
            }
        }

        public static Exception Add_Parameter_To_Task(int id_task, int id_counter, string param_name)
        {//тут добавялем параметр счётчика в задание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "insert into dbo.task_parameters values (" + id_task.ToString() + "," + id_counter.ToString() + ",N'" + param_name + "')";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {                         
                        return ex;
                    }
                      
                    return null;
                }
            }
        }

        public static SqlException Update_Task(string name, string comments, int task_id)
        {//процедура, обновляющая задание (имя и комментарии согласно строкам в гриде на форме заданий)
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "update dbo.task set name = N'" + name + "', comments = N'" + comments + "' where id=" + task_id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {                          
                        return ex;
                    }
                      
                    return null;
                }
            }
        }

        public static SqlException Update_User(byte user_id, string login, string name, string role)
        {//процедура, обновляющая пользователя
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "update dbo.users set login = N'" + login + "', name = N'" + name + "' , role='" + role + "' where id= " + user_id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 2627) MessageBox.Show("Такой логин уже есть: " + login, "Повторяется логин", MessageBoxButtons.OK, MessageBoxIcon.Error);                          
                    }
                      
                    return null;
                }
            }
        }

        public static Exception Update_Schedule(string name, int day_of_month, int day_of_week, DateTime time, DateTime start_date, int times_repeat, byte active, int schedule_id)
        {//процедура, обновляющая расписание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Update_Schedule_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = schedule_id; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("day_of_month", SqlDbType.SmallInt); inParam.Direction = ParameterDirection.Input; inParam.Value = day_of_month; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("day_of_week", SqlDbType.SmallInt); inParam.Direction = ParameterDirection.Input; inParam.Value = day_of_week; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("time", SqlDbType.Time); inParam.Direction = ParameterDirection.Input; inParam.Value = time.TimeOfDay; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("start_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = start_date; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("times_repeat", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = times_repeat; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("active", SqlDbType.Bit); inParam.Direction = ParameterDirection.Input; inParam.Value = active; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {                         
                        return ex;
                    }
                      
                    return null;
                }
            }
        }

        public static DataTable Return_Binded_Mail_Recievers(int id_task)
        {
            //процедура, достающая из базы список получателей почты, привязанных к  выбранному заданию 
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select id as id_reciever, email from mail_recievers where id in (select id_reciever from TASK_MAILING where id_task =" + id_task.ToString() + ")";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                         
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Bind_Mail_Recievers(int id_task, int id_reciever)
        {//процедура, привязывающая или отвязывающая получателя почты
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Bind_MailReciever", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id_task", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_task; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("id_reciever", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_reciever; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру
                    cmd.ExecuteNonQuery();
                      
                }
            }
        }

        public static DataTable Return_Binded_Schedules(int id_task)
        {
            //процедура, достающая из базы список привязанных к заданию расписаний
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select id_schedule from schedules_tasks_table where id_task=" + id_task.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                   
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Bind_Schedule(int id_task, int id_schedule)
        {//процедура, привязывающая или отвязывающая расписание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Bind_Schedule", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id_task", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_task; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("id_schedule", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_schedule; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру
                    cmd.ExecuteNonQuery();
                      
                }
            }
        }

        public static DataTable Return_Schedules()
        {
            //процедура, достающая из базы список расписаний
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.schedules";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {

                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                    
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }

        public static void Return_Tasks(DataGridView dgv)
        {
            //процедура, достающая из базы список заданий. Перегрузка с обратной связью через DataAdapter
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                
                DataTable dt = new DataTable();
                string query = String.Empty;
                if (User.Role == "admin")
                    query = "select id, name, comments from task";//если роль пользователя - администратор, то возвращаем все задания в том числе задания администратора
                else
                    query = "select id, name, comments from task except select id, name, comments from task where user_id = (select id from users where role = 'admin')";//если не админ, то возвращаем все задания, кроме заданий админа

                try
                {
                    connection.Open();
                    //здесь using нельзя потому что к этому SqlDataAdapter идёт обращение извне (из формы заданий)
                    SqlDataAdapter da = new SqlDataAdapter(query, connection);//создаём адаптер данных (позволяет иметь обратную связь с источником данных, а не только считывать)
                    //описываем команду обновления чтобы она автоматически отрабатывала по изменении данных в гриде
                    SqlCommand cmd = new SqlCommand("UPDATE dbo.Task SET id = @id, name = @name, comments = @comments where id = @old_id", connection);//задаём команду обновления
                    cmd.Parameters.Add("@id", SqlDbType.Int, 10, "id");//задаём параметры
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar, 150, "name");//задаём параметры
                    cmd.Parameters.Add("@comments", SqlDbType.NVarChar, 350, "comments");//задаём параметры
                    cmd.Parameters.Add("@old_id", SqlDbType.Int, 10, "id");//этот параметр особый. Отвечает за то, чтобы идентификаторы строк оставались прежними???
                    cmd.Parameters["@old_id"].SourceVersion = DataRowVersion.Original;
                    da.UpdateCommand = cmd;//задаём команду, которую сочинили                                                  
                    da.Fill(dt);//заполняем таблицу данными
                    BindingSource bs = new BindingSource();//привязываем источник данных к гриду через этот класс
                    bs.DataSource = dt;//задаём источники данных
                    dgv.DataSource = bs;//задаём источники данных
                    dgv.Tag = da;//помещаем DataAdapter в Tag грида чтобы потом его легко получить на форме заданий                                                
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        public static DataTable Return_EMail_Recievers()
        {
            //процедура, достающая из базы список получателей почты
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable(); string query = String.Empty;                
                query = "select id, email from mail_recievers";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {                         
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }


        public static DataTable Return_Schedules_Tasks_Table()
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                //процедура, достающая из базы сетку расписаний с заданиями текущего пользователя. Общие задания здесь игнорируются, т.к. обычный пользователь не может их опрашивать. Только администратор
                try
                {
                    connection.Open();
                }
                catch
                {
                    return null;
                }
                DateTime current_date_time = DateTime.Now;
                string time = (current_date_time.TimeOfDay.Hours).ToString() + ":" + (current_date_time.TimeOfDay.Minutes).ToString() + ":" + (current_date_time.TimeOfDay.Seconds).ToString();
                //вытаскиваем все расписания на текущий момент времени
                string query = "select a.id, a.name, b.name as task_name, b.id as task_id from dbo.SCHEDULES as a, dbo.TASK as b, "
                              + " dbo.SCHEDULES_TASKS_TABLE as c where a.id = c.id_schedule and b.id = c.id_task and a.active=1 and b.user_id=" + User.ID.ToString()
                              + " and a.time='" + time + "' and a.start_date<=@current_date_time"
                              + " and a.times_repeat<>0"
                              + " and (a.day_of_week = @day_of_week or a.day_of_month = @day_of_month or a.day_of_week = 0 or a.day_of_month = 0)";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        //SqlParameter inParam = new SqlParameter("time", time); inParam.DbType = DbType.Time; cmd.Parameters.Add(inParam);
                        SqlParameter inParam = new SqlParameter("day_of_week", (int)current_date_time.DayOfWeek); inParam.DbType = DbType.Int16; cmd.Parameters.Add(inParam);//день недели
                        inParam = new SqlParameter("day_of_month", current_date_time.Day); inParam.DbType = DbType.Int16; cmd.Parameters.Add(inParam);//день месяца
                        inParam = new SqlParameter("current_date_time", current_date_time); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())

                        {
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                try
                                {
                                    dt.Load(dr);
                                    return dt;
                                }
                                catch (Exception ex)
                                {
                                    return null;
                                }
                            }
                        }
                    }               
            }
        }

        public static DataTable Return_Task_Profile(int id_task)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, достающая из базы параметры пролфиля для задания, если такие есть
                DataTable dt = new DataTable();
                string query = "select * from dbo.task_profile where id_task=" + id_task.ToString();
                SqlCommand cmd = new SqlCommand(query, connection);

                try
                {
                    using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                }
                catch (Exception ex)
                {
                      
                    MessageBox.Show(ex.Message.ToString());
                }
                  
                return dt;
            }
        }

        public static DataTable Return_Profile(int id_counter, DateTime daten, DateTime datek)
        {//процедура, достающая из базы профиль
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();//начиная с 22.05.2020 этот запрос округляет метки времени с точностью до периода интегрирования, чтобы кривые метки (из счётчиков со сбитыми часаи) не мешали обрабатывать данные
                                                //также этот запрос суммирует показания с группировкой по времени чтобы исключить "двойников" (записей с одинаковым временем, но разными показаниями)
                string query = "select 'dummy1','dummy2','dummy3', sum(e_a_plus) as e_a_plus, sum(e_a_minus) as e_a_minus, sum(e_r_plus) as e_r_plus, sum(e_r_minus) as e_r_minus, "
                    +"dateadd(minute, datediff(minute, 0, date_time) / 30 * 30, 0) as "
                    +" date_time, period from power_profile where id_counter = " + id_counter.ToString()
                    + "  and date_time>'" + daten.Day.ToString().PadLeft(2, '0') + "-" + daten.Month.ToString().PadLeft(2, '0') + "-" + daten.Year.ToString() + " " + daten.Hour.ToString().PadLeft(2, '0') + ":" + daten.Minute.ToString().PadLeft(2, '0')
                    + "' and date_time<='" + datek.Day.ToString().PadLeft(2, '0') + "-" + datek.Month.ToString().PadLeft(2, '0') + "-" + datek.Year.ToString() + " " + datek.Hour.ToString().PadLeft(2, '0') + ":" + datek.Minute.ToString().PadLeft(2, '0') + "' group by dateadd(minute, datediff(minute, 0, date_time) / 30 * 30, 0), period order by date_time";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) { dt.Load(dr); }
                    }
                    catch (Exception ex)
                    {

                        //MessageBox.Show(ex.Message.ToString());
                    }

                    return dt;
                }
            }
        }

        public static DataTable Decrease_Schedule_Times_Repeat(int id_schedule)
        {//сокращаем количество повторов выполнения расписания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "update dbo.schedules set times_repeat = times_repeat - 1 where id =" + id_schedule.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        return dt;
                    }
                    catch
                    {
                          
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Task_Grid(int id_task)
        {//возвращаем сетку задания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                string query = "select * from dbo.task_grid where id_task = @id_task";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter param = new SqlParameter("id_task", id_task); param.DbType = DbType.Int16; cmd.Parameters.Add(param);
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Task_Parameters_Grid(int id_task, int id_counter)
        {//возвращаем сетку параметров счётчиков для задания
            DataTable dt = new DataTable();
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = "select * from dbo.task_parameters where id_task=" + id_task.ToString() + " and id_counter=" + id_counter.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }
                      
                    return dt;
                }
            }
        }

        public static void Update_Connection_Row(int id, string name, string phone_number, string ip_address, string ip_port, string gsm_cbst, bool autoconfig, string config_string, string district,
            string street, string house, string comments)
        {//процедура обновления подключения
            byte auto = 0;
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                if (autoconfig) auto = 1; else auto = 0;

                using (SqlCommand cmd = new SqlCommand("dbo.Update_Connection_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("phone_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = phone_number; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("ip_address", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = ip_address; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("ip_port", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = ip_port; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("gsm_cbst", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = gsm_cbst; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("autoconfig", SqlDbType.Bit); inParam.Direction = ParameterDirection.Input; inParam.Value = auto; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("config_string", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = config_string; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("district", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = district; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("comments", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = comments; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("street", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = street; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("house", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = house; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка сохранения. Проверьте значения полей.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                          
                        return;
                    }

                      
                }
            }
        }    

        public static void Update_Concentrator_Row(int id, string name, int net_address, string comments)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Update_Concentrator_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("net_address", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = net_address; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("comments", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = comments; cmd.Parameters.Add(inParam);
                    //выполняем хранимую процедуру
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка сохранения. Проверьте значения полей.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                          
                        return;
                    }
                      
                }
            }
        }

        public static void Update_CounterRS_Row(int id, string name, string street, string house, string serial_number, int net_address, string comments, string district, int power_profile_exists,
                                                   int integrated_feed, string pwd1, string pwd2, int transformation_rate, double calc_addendum, string misc_id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Update_CounterRS_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("street", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = street; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("house", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = house; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("serial_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = serial_number; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("net_address", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = net_address; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("comments", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = comments; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("district", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = district; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("power_profile_exist", SqlDbType.Bit); inParam.Direction = ParameterDirection.Input; inParam.Value = power_profile_exists; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("integrated_feed", SqlDbType.Bit); inParam.Direction = ParameterDirection.Input; inParam.Value = integrated_feed; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("pwd1", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = pwd1; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("pwd2", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = pwd2; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("transformation_rate", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = transformation_rate; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("calc_addendum", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = calc_addendum; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("misc_id", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = misc_id; cmd.Parameters.Add(inParam);
                    SqlParameter outParam = new SqlParameter("plc_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//идентификатор RS-счётчика если он есть               
                    //выполняем хранимую процедуру
                    try
                    {
                        cmd.ExecuteNonQuery();
                        //если RS-счётчик с таким серийным номером уже имеется, то пишем ошибку
                        //if ((int)cmd.Parameters["plc_id"].Value > -1)
                        //{
                        //    MessageBox.Show("Такой серийный номер уже есть в PLC-счётчиках: " + serial_number.ToString(), "Повторяется серийный номер", MessageBoxButtons.OK, MessageBoxIcon.Error);                              
                        //    return;
                        //}
                    }
                    catch (SqlException ex)
                    {//ошибка duplicate key
                        if (ex.Number == 2627) MessageBox.Show("Такой серийный номер уже есть" , "Повторяется серийный номер", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else MessageBox.Show(ex.Message, "Ошибка сохранения. Проверьте значения полей.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                          
                        return;
                    }
                      
                }
            }
        }

        public static void Update_CounterPLC_Row(int id, string name, string street, string house, string serial_number, int net_address, string comments, string district
          , double e_t0_last, double e_t1_last, double e_t2_last, double e_t3_last, double e_t4_last
          , DateTime? e_t0_last_date, DateTime? e_t1_last_date, DateTime? e_t2_last_date,
           DateTime? e_t3_last_date, DateTime? e_t4_last_date)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Update_CounterPLC_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("street", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = street; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("house", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = house; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("serial_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = serial_number; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("net_address", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = net_address; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("comments", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = comments; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("district", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = district; cmd.Parameters.Add(inParam);

                    inParam = new SqlParameter("e_t0_last", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = e_t0_last; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t1_last", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = e_t1_last; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t2_last", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = e_t2_last; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t3_last", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = e_t3_last; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t4_last", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = e_t4_last; cmd.Parameters.Add(inParam);
                    //DateTime не принимает значение NULL по-умолчанию, поэтому столько преобразований
                    inParam = new SqlParameter("e_t0_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input;
                    if (e_t0_last_date == null) inParam.Value = DBNull.Value; else inParam.Value = e_t0_last_date; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t1_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input;
                    if (e_t1_last_date == null) inParam.Value = DBNull.Value; else inParam.Value = e_t1_last_date; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t2_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input;
                    if (e_t2_last_date == null) inParam.Value = DBNull.Value; else inParam.Value = e_t2_last_date; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t3_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input;
                    if (e_t3_last_date == null) inParam.Value = DBNull.Value; else inParam.Value = e_t3_last_date; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("e_t4_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input;
                    if (e_t4_last_date == null) inParam.Value = DBNull.Value; else inParam.Value = e_t4_last_date; cmd.Parameters.Add(inParam);

                    SqlParameter outParam = new SqlParameter("rs_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//идентификатор RS-счётчика если он есть 
                    //выполняем хранимую процедуру
                    try
                    {
                        cmd.ExecuteNonQuery();
                        //если RS-счётчик с таким серийным номером уже имеется, то пишем ошибку
                        //if ((int)cmd.Parameters["rs_id"].Value > -1)
                        //{
                        //    MessageBox.Show("Такой серийный номер уже есть в RS-счётчиках", "Повторяется серийный номер", MessageBoxButtons.OK, MessageBoxIcon.Error);                            
                        //    return;
                        //}
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 2627) MessageBox.Show("Такой серийный номер уже есть" + serial_number, "Повторяется серийный номер", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else MessageBox.Show(ex.Message, "Ошибка сохранения.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                          
                        return;
                    }
                      
                }
            }
        }

        public static int Create_Connection_Row(string object_type)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Connection_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam1 = new SqlParameter("object_type", SqlDbType.NVarChar); inParam1.Direction = ParameterDirection.Input; inParam1.Value = object_type; cmd.Parameters.Add(inParam1);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//вывод - идентификатор нового объекта                  
                    cmd.ExecuteNonQuery();
                    //Update_Change_Row();
                      
                    return (int)cmd.Parameters["new_id"].Value;
                }
            }
        }

        public static int Create_Concentrator_Row(string object_type, int parent_id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Concentrator_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam1 = new SqlParameter("object_type", SqlDbType.NVarChar); inParam1.Direction = ParameterDirection.Input; inParam1.Value = object_type; cmd.Parameters.Add(inParam1);
                    SqlParameter inParam2 = new SqlParameter("parent_id", SqlDbType.Int); inParam2.Direction = ParameterDirection.Input; inParam2.Value = parent_id; cmd.Parameters.Add(inParam2);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//вывод - идентификатор нового объекта                  
                    cmd.ExecuteNonQuery();
                      
                    return (int)cmd.Parameters["new_id"].Value;
                }
            }
        }

        public static int Return_Allowed_Task(string task_type)
        {//здесь смотрим, в какое задание можно автоматически кинуть счётчик (для последующего ежедневного опроса) при добавлении в главное дерево
         //сделано для того, чтобы при добавлении очередного счётчика пользователь не беспокоился о его попадании в ежедневный опрос
            int allowedTaskID = -1;//номер разрешённого задания
            int max = 0;//максимальное количество счётчиков в задании общего опроса
            if (task_type == "rs485") max = 1000;// если это RS-задание
            if (task_type == "plc") max = 10000;//если это PLC-задание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable allTasks = new DataTable();//возвращаем задания общего опроса (созданные администратором программы) 
                string query = "select * from task where user_id= (select id from USERS where role='admin')";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader()) allTasks.Load(dr);
                    if (allTasks.Rows.Count == 0)
                    {//если заданий вообще нет
                          
                        return allowedTaskID;
                    }
                    string filter = "type='" + task_type + "'";
                    DataRow[] typedTasks = allTasks.Select(filter);//список заданий текущего типа (rs или plc)        
                    if (typedTasks.Count<DataRow>() == 0)
                    {//если заданий текущего типа нет, то выходим
                          
                        return allowedTaskID;
                    }
                    //в том случае если задания текущего типа нашлись, нужно пройти по ним цикл и найти подходящее
                    foreach (DataRow row in typedTasks)
                    {//смотрим какое количество счётчиков содержится в каждом задании
                        DataTable grid = Return_Task_Grid(Convert.ToInt16(row["id"].ToString()));//возвращаем сетку задания по идентификатору
                        if (grid.Rows.Count >= max) continue; //если количество счётчиков в задании больше допустимого, то идём дальше
                        //как только нашли подходящее задание общего опроса, сразу же возвращаем его номер
                        allowedTaskID = Convert.ToInt16(row["id"].ToString());
                        break;
                    }

                      
                    return allowedTaskID;
                }
            }
        }

        public static int Create_CounterRS_Row(int type_id, int parent_id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_CounterRS_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam1 = new SqlParameter("type_id", SqlDbType.TinyInt); inParam1.Direction = ParameterDirection.Input; inParam1.Value = type_id; cmd.Parameters.Add(inParam1);
                    SqlParameter inParam2 = new SqlParameter("parent_id", SqlDbType.Int); inParam2.Direction = ParameterDirection.Input; inParam2.Value = parent_id; cmd.Parameters.Add(inParam2);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//вывод - идентификатор нового объекта       
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return -1;
                    }
                    // Update_Change_Row();
                      
                    return (int)cmd.Parameters["new_id"].Value;
                }
            }
        }

        public static int Create_CounterPLC_Row(int parent_id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_CounterPLC_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam2 = new SqlParameter("parent_id", SqlDbType.Int); inParam2.Direction = ParameterDirection.Input; inParam2.Value = parent_id; cmd.Parameters.Add(inParam2);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);//вывод - идентификатор нового объекта                  
                    cmd.ExecuteNonQuery();
                      
                    return (int)cmd.Parameters["new_id"].Value;
                }
            }
        }

        public static DataTable Return_CounterRS_Last_Energy(string serial_number, string energy_name)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {//процедура, вытаскивающая из базы энергию для счётчика RS по названию энергии
                DataTable dt = new DataTable("CounterRS_Last_Energy"); //чтобы создать и заполнить DataRow нужно создать таблицу
                connection.Open();

                DataColumn
                dc = new DataColumn("e_t0_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t1_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t2_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t3_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t4_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_last_date"); dc.AllowDBNull = true; dt.Columns.Add(dc);

                DataRow dr = dt.NewRow();

                using (SqlCommand cmd = new SqlCommand("dbo.Return_CounterRS_Last_Energy_By_Name", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("serial_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = serial_number; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("energy_name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = energy_name; cmd.Parameters.Add(inParam);
                    //исходящие параметры      
                    SqlParameter outParam_t0last = new SqlParameter("e_t0_last", SqlDbType.Float); outParam_t0last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t0last);
                    SqlParameter outParam_t1last = new SqlParameter("e_t1_last", SqlDbType.Float); outParam_t1last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t1last);
                    SqlParameter outParam_t2last = new SqlParameter("e_t2_last", SqlDbType.Float); outParam_t2last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t2last);
                    SqlParameter outParam_t3last = new SqlParameter("e_t3_last", SqlDbType.Float); outParam_t3last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t3last);
                    SqlParameter outParam_t4last = new SqlParameter("e_t4_last", SqlDbType.Float); outParam_t4last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t4last);
                    SqlParameter outParam_last_date = new SqlParameter("e_last_date", SqlDbType.DateTime); outParam_last_date.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_last_date);
                    //выполняем хранимую процедуру
                    cmd.ExecuteNonQuery();
                      
                    //заполняем строку DataRow данными, полученными через исходящие параметры хранимой процедуры
                    dr["e_t0_last"] = outParam_t0last.Value; dr["e_t1_last"] = outParam_t1last.Value; dr["e_t2_last"] = outParam_t2last.Value;
                    dr["e_t3_last"] = outParam_t3last.Value; dr["e_t4_last"] = outParam_t4last.Value; dr["e_last_date"] = outParam_last_date.Value;

                    if (outParam_t0last.Value == DBNull.Value)
                    {
                        dr["e_t0_last"] = 0; dr["e_t1_last"] = 0; dr["e_t2_last"] = 0;
                        dr["e_t3_last"] = 0; dr["e_t4_last"] = 0; dr["e_last_date"] = String.Empty;
                    }

                    dt.Rows.Add(dr);
                    return dt;
                }
            }
        }

        public static DataTable Return_CounterRS_Last_Energy(int id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open(); 
                //процедура, вытаскивающая из базы последние показания счётчика RS по энергии от сброса
                DataTable dt = new DataTable("CounterRS_Last_Energy"); //чтобы создать и заполнить DataRow нужно создать таблицу
                DataColumn
                dc = new DataColumn("e_t0_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t1_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t2_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t3_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_t4_last"); dc.AllowDBNull = true; dt.Columns.Add(dc);
                dc = new DataColumn("e_last_date"); dc.AllowDBNull = true; dt.Columns.Add(dc);

                DataRow dr = dt.NewRow();

                using (SqlCommand cmd = new SqlCommand("dbo.Return_CounterRS_Last_Energy", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);
                    //исходящие параметры      
                    SqlParameter outParam_t0last = new SqlParameter("e_t0_last", SqlDbType.Float); outParam_t0last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t0last);
                    SqlParameter outParam_t1last = new SqlParameter("e_t1_last", SqlDbType.Float); outParam_t1last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t1last);
                    SqlParameter outParam_t2last = new SqlParameter("e_t2_last", SqlDbType.Float); outParam_t2last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t2last);
                    SqlParameter outParam_t3last = new SqlParameter("e_t3_last", SqlDbType.Float); outParam_t3last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t3last);
                    SqlParameter outParam_t4last = new SqlParameter("e_t4_last", SqlDbType.Float); outParam_t4last.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_t4last);
                    SqlParameter outParam_last_date = new SqlParameter("e_last_date", SqlDbType.DateTime); outParam_last_date.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam_last_date);
                    //выполняем хранимую процедуру
                    cmd.ExecuteNonQuery();
                      
                    //заполняем строку DataRow данными, полученными через исходящие параметры хранимой процедуры
                    dr["e_t0_last"] = outParam_t0last.Value; dr["e_t1_last"] = outParam_t1last.Value; dr["e_t2_last"] = outParam_t2last.Value;
                    dr["e_t3_last"] = outParam_t3last.Value; dr["e_t4_last"] = outParam_t4last.Value; dr["e_last_date"] = outParam_last_date.Value;

                    dt.Rows.Add(dr);
                    return dt;
                }
            }
        }

        public static DataTable Return_Integral_Parameters_Row(int id_counter)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы параметры выгрузки интегрального акта для текущего счётчика
                DataTable dt = new DataTable();
                string query = "select * from dbo.integral_report_parameters where id_counter= " + id_counter.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        return dt;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                    return dt;
                }
            }
        }

        public static DataTable Return_CounterRS_Row(int id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы данные выбранного в интерфейсе счётчика RS
                DataTable dt = new DataTable();
                string query = "select a.name, a.street, a.house, a.serial_number, a.net_address, a.comments, a.district, a.date_create, a.power_profile_exist,a.integrated_feed, a.pwd1, a.pwd2, " +
                                " a.type_id, a.transformation_rate, b.calculated_addendum_percent, b.misc_id " +
                                " from dbo.counters_rs a LEFT JOIN dbo.integral_report_parameters b on a.id = b.id_counter where a.id = " + id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим                            
                            return dt;
                        }
                          
                        dt = TransponeTable(dt);

                        //строки первого столбца являются заголовками транспонированной твблицы
                        dt.Rows[0][0] = "Объект"; dt.Rows[1][0] = "Улица";
                        dt.Rows[2][0] = "Дом"; dt.Rows[3][0] = "Серийный номер";
                        dt.Rows[4][0] = "Сетевой"; dt.Rows[5][0] = "Комментарий";
                        dt.Rows[6][0] = "Район"; dt.Rows[7][0] = "Дата создания";
                        dt.Rows[8][0] = "Профиль"; dt.Rows[9][0] = "Внутр. пит.";
                        dt.Rows[10][0] = "Пароль 1"; dt.Rows[11][0] = "Пароль 2";
                        dt.Rows[13][0] = "Коэфф. тр-ции"; dt.Rows[14][0] = "Коэфф. потерь";
                        dt.Rows[15][0] = "ИД для сбытовой организации";

                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    } 
                    
                    return dt;
                }
            }
        }

        public static DataTable Return_Concentrator_Row(int id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы данные выбранного в интерфейсе концентратора
                DataTable dt = new DataTable();
                string query = "select name, net_address, comments from dbo.concentrator_points where id=" + id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим
                              
                            return dt;
                        }
                          
                        dt = TransponeTable(dt);
                        //строки первого столбца являются заголовками транспонированной твблицы
                        dt.Rows[0][0] = "Объект"; dt.Rows[1][0] = "Сетевой";
                        dt.Rows[2][0] = "Комментарий";

                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }

        public static DataTable Return_CounterPLC_Row(int id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open(); 
                //процедура, вытаскивающая из базы данные выбранного в интерфейсе счётчика PLC
                DataTable dt = new DataTable();
                string query = "select name,street,house,serial_number,net_address,comments,district,date_create,e_t0_last,e_t1_last,e_t2_last,e_t3_last,e_t4_last"
                    + ",e_t0_last_date,e_t1_last_date,e_t2_last_date,e_t3_last_date,e_t4_last_date,e_t0_last_avg,e_t1_last_avg,e_t2_last_avg"
                    + ",e_t3_last_avg,e_t4_last_avg from dbo.counters_plc where id=" + id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим                           
                            return dt;
                        }
                        dt = TransponeTable(dt);
                        //строки первого столбца являются заголовками транспонированной твблицы
                        dt.Rows[0][0] = "Объект";
                        dt.Rows[1][0] = "Улица";
                        dt.Rows[2][0] = "Дом";
                        dt.Rows[3][0] = "Серийный номер";
                        dt.Rows[4][0] = "Сетевой";
                        dt.Rows[5][0] = "Комментарий";
                        dt.Rows[6][0] = "Район";
                        dt.Rows[7][0] = "Дата создания";
                        dt.Rows[8][0] = "Посл. пок. сумма";
                        dt.Rows[9][0] = "Посл. пок. Т1";
                        dt.Rows[10][0] = "Посл. пок. Т2";
                        dt.Rows[11][0] = "Посл. пок. Т3";
                        dt.Rows[12][0] = "Посл. пок. Т4";
                        dt.Rows[13][0] = "Посл. пок. сумма, дата";
                        dt.Rows[14][0] = "Посл. пок. Т1, дата";
                        dt.Rows[15][0] = "Посл. пок. Т2, дата";
                        dt.Rows[16][0] = "Посл. пок. Т3, дата";
                        dt.Rows[17][0] = "Посл. пок. Т4, дата";

                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Connection_Row(int id)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы данные выбранного в интерфейсе подключения
                DataTable dt = new DataTable();
                string query = "select name,phone_number,ip_address,ip_port,type_id,gsm_cbst,autoconfig,config_string,district,street,house,comments from dbo.connection_points where id=" + id.ToString();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим
                              
                            return dt;
                        }
                          
                        dt = TransponeTable(dt);
                        //первый столбец транспонированной таблицы - это свойства объекта из реальной таблицы
                        dt.Rows[0][0] = "Объект"; dt.Rows[1][0] = "Номер"; dt.Rows[2][0] = "IP-адрес";
                        dt.Rows[3][0] = "Порт"; dt.Rows[5][0] = "Строка инициализации модема"; dt.Rows[6][0] = "Автоконфигурация порта";
                        dt.Rows[8][0] = "Район"; dt.Rows[9][0] = "Улица"; dt.Rows[10][0] = "Дом";
                        dt.Rows[11][0] = "Комментарий";

                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }



        public static DataTable TransponeTable(DataTable dt_old)
        {//здесь траспонируем входную таблицу
            DataTable dt_new = new DataTable("DeviceProperties");
            //добавляем в новую таблицу два столбца "Свойство" и "Значение"
            DataColumn dc = new DataColumn("Property"); dc.AllowDBNull = true; dt_new.Columns.Add(dc);
            dc = new DataColumn("Value"); dc.AllowDBNull = true; dt_new.Columns.Add(dc);
            //циклимся столбцам старой таблицы
            int i = 0;//счётчик номера столбца старой таблицы
            foreach (DataColumn col in dt_old.Columns)
            {//для каждого столбца старой таблицы создаём строку в новой            
                DataRow dr = dt_new.NewRow();
                dr["Property"] = col.ColumnName;
                dr["Value"] = dt_old.Rows[0][i];
                dt_new.Rows.Add(dr);
                i += 1;
            }

            return dt_new;
        }

        public static DataTable Return_Counter_Energy_PLC_History(string serial_number, int id_source, DateTime DateN, DateTime DateK, string energy_name)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open(); 
                //возвращаем историю показаний по указанному счётчику с указанным названием энергии
                DataTable dt = new DataTable();
                string query = "select energy_t0, date_time_t0, energy_t1,date_time_t1, energy_t2,date_time_t2, energy_t3,date_time_t3, energy_t4, date_time_t4, name " +
                               "from energy where serial_number = '" + serial_number + "' and id_source=" + id_source.ToString() + " and ((date_time_t0 >= '" + DateN.ToString() + "' and date_time_t0 <='" + DateK.ToString()
                              + "') or (date_time_t1 >= '" + DateN.ToString() + "' and date_time_t1 <='" + DateK.ToString() + "') or (date_time_t2 >= '" + DateN.ToString() + "' and date_time_t2 <='" + DateK.ToString()
                              + "') or (date_time_t3 >= '" + DateN.ToString() + "' and date_time_t3 <='" + DateK.ToString() + "') or (date_time_t4 >= '" + DateN.ToString() + "' and date_time_t4 <='" + DateK.ToString()
                              + "')) and name in (" + energy_name + ") order by date_time_t0 desc, date_time_t1 desc, date_time_t2 desc, date_time_t3 desc, date_time_t4 desc";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                          
                        return dt;
                    }
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_Counter_Energy_RS_History(string serial_number, int id_source, DateTime DateN, DateTime DateK, string energy_name)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open(); 
                //возвращаем историю показаний по указанному счётчику с указанным названием энергии
                DataTable dt = new DataTable();
                string query = "select '0', date_time_t0, energy_t0, energy_t1, energy_t2, energy_t3, energy_t4, name " +
                               "from energy where serial_number = '" + serial_number + "' and id_source=" + id_source.ToString() + " and ((date_time_t0 >= '" + DateN.ToString() + "' and date_time_t0 <='" + DateK.ToString()
                              + "') or (date_time_t1 >= '" + DateN.ToString() + "' and date_time_t1 <='" + DateK.ToString() + "') or (date_time_t2 >= '" + DateN.ToString() + "' and date_time_t2 <='" + DateK.ToString()
                              + "') or (date_time_t3 >= '" + DateN.ToString() + "' and date_time_t3 <='" + DateK.ToString() + "') or (date_time_t4 >= '" + DateN.ToString() + "' and date_time_t4 <='" + DateK.ToString()
                              + "')) and name in (" + energy_name + ") order by date_time_t0 desc, date_time_t1 desc, date_time_t2 desc, date_time_t3 desc, date_time_t4 desc";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                          
                        return dt;
                    }
                      
                    return dt;
                }
            }
        }

        public static void Reassign_Parent(int object_id, int new_parent_id, object @object)
        {//здесь переназначаем родителя узла
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //в зависимости от типа объекта делаем изменения в соответствующей таблице
                string query = String.Empty;
                if (@object is ICounter && @object.GetType() != typeof(MercuryPLC1)) query = "update dbo.counters_rs set id_connection =" + new_parent_id.ToString() + " where id=" + object_id.ToString();               
                if (@object.GetType() == typeof(MercuryPLC1)) query = "update dbo.counters_plc set id_concentrator =" + new_parent_id.ToString() + " where id=" + object_id.ToString();
                if (@object.GetType() == typeof(Mercury225PLC1)) query = "update dbo.concentrator_points set id_connection =" + new_parent_id.ToString() + " where id=" + object_id.ToString();

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {

                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader());
                              
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                          
                    }
                }
            }
        }

        public static DataTable Return_Concentrator_Last_Energy(TreeNode concentrator_node)
        {//возвращаем последние показания для всех счётчиков на текущем выбранном концентраторе
            //сначала составляем список дочерних узлов концентратора в виде запроса IN
            DataTable dt = new DataTable();
            if (concentrator_node.Nodes.Count == 0) return dt;
            string queryIN = String.Empty;

            foreach (TreeNode node in concentrator_node.Nodes)
            {
                var device = (IDevice)node.Tag;
                queryIN += String.Empty + device.ID.ToString() + ",";
                // queryIN = string.Join(",", device.ID.ToString());
            }
            //удаляем последнюю запятую из запрсоа
            queryIN = queryIN.Remove(queryIN.Length - 1);

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open(); 
                string query = "select street + ' ' + house + ' (' + convert(varchar(4), net_address) +')' as name, serial_number, district, "
                   + " e_t0_last, e_t0_last_date, e_t1_last, e_t1_last_date, e_t2_last,e_t2_last_date, e_t3_last, e_t3_last_date, e_t4_last, e_t4_last_date, comments   " +
                               " from counters_plc where id in (" + queryIN + ")";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                        //  
                        return dt;
                    }
                    //  
                    return dt;
                }
            }
        }

        public static byte Create_User(string login, string name, string role)
        {//создаём пользователя
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_User_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("login", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = login; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("role", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = role; cmd.Parameters.Add(inParam);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.TinyInt); outParam.Direction = ParameterDirection.ReturnValue; cmd.Parameters.Add(outParam);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {//ошибка duplicate key
                        if (ex.Number == 2627) MessageBox.Show("Такой логин уже есть: " + login, "Повторяется логин", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //  
                    }

                    //  
                    return Convert.ToByte(outParam.Value);//возвращаем идентификатор нового пользователя
                }
            }
        }

        public static SqlException Create_Server_Login_And_DBUser(string username)
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Server_Login_And_DBUser", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlParameter inParam = new SqlParameter("username", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = username; cmd.Parameters.Add(inParam);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        //  
                        return ex;
                    }
                    //  
                    return null;
                }
            }
        }

        public static void Delete_User(int id)
        {//удаляем пользователя
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_User", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);//вход - идентификатор объекта                  
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
        }

        public static void Delegate_Admin_Role(byte user_id)
        {//передаём права администратора выбранному пользователю
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delegate_Admin_Role", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("user_id", SqlDbType.TinyInt); inParam.Direction = ParameterDirection.Input; inParam.Value = user_id; cmd.Parameters.Add(inParam);//вход - идентификатор пользователя                  
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
        }


        public static void Delete_Connection_Row(int id)
        {//удаляем подключение
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_Connection_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);//вход - идентификатор объекта                  
                    cmd.ExecuteNonQuery();//выполняем
                                          //Update_Change_Row();
                    //  
                }
            }
        }

        public static void Delete_CounterRS_Row(int id)
        {//удаляем счётчик с витой парой
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_CounterRS_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);//вход - идентификатор объекта                  
                    cmd.ExecuteNonQuery();//выполняем

                    //  
                }
            }
        }

        public static void Delete_CounterPLC_Row(int id)
        {//удаляем PLC-счётчик
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_CounterPLC_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);//вход - идентификатор объекта                  
                    cmd.ExecuteNonQuery();//выполняем

                    //  
                }
            }
        }

        public static void Delete_Concentrator_Row(int id)
        {//удаляем концентратор
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_Concentrator_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id; cmd.Parameters.Add(inParam);//вход - идентификатор объекта                  
                    cmd.ExecuteNonQuery();//выполняем

                    //  
                }
            }
        }

        public static SqlException Create_EnergyRS_Row(string serial_number, double lastValueZone0, double lastValueZone1, double lastValueZone2, double lastValueZone3, double lastValueZone4, string name)
        {//добавляем строку показаний для источника RS485, source = 1
            try
            {
                using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.Create_EnergyRS_Row", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //задаём входные параметры
                        SqlParameter
                        inParam = new SqlParameter("serial_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = serial_number; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("lastValueZone0", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone0; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("lastValueZone1", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone1; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("lastValueZone2", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone2; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("lastValueZone3", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone3; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("lastValueZone4", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone4; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                return ex;
            }
            return null;
        }

        public static string Create_EnergyPLC_Row(string serial_number, double lastValueZone0, double lastValueZone1, double lastValueZone2, double lastValueZone3, double lastValueZone4,
                                          DateTime? lastDateZone0, DateTime? lastDateZone1, DateTime? lastDateZone2, DateTime? lastDateZone3, DateTime? lastDateZone4, string name, int id_object, bool clonemode)
        {//добавляем строку показаний для источника PLC, source = 2
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch
                {
                    return "-2";
                }
                using (SqlCommand cmd = new SqlCommand("dbo.Create_EnergyPLC_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter
                    inParam = new SqlParameter("serial_number", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = serial_number; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentValueZone0", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone0; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentValueZone1", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone1; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentValueZone2", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone2; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentValueZone3", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone3; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentValueZone4", SqlDbType.Float); inParam.Direction = ParameterDirection.Input; inParam.Value = lastValueZone4; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentDateZone0", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lastDateZone0; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentDateZone1", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lastDateZone1; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentDateZone2", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lastDateZone2; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentDateZone3", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lastDateZone3; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("CurrentDateZone4", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = lastDateZone4; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("id_object", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_object; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("clonemode", SqlDbType.Bit); inParam.Direction = ParameterDirection.Input; inParam.Value = clonemode; cmd.Parameters.Add(inParam);

                    SqlParameter outParam = new SqlParameter("details", SqlDbType.NVarChar, 1000); outParam.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam);
                    cmd.ExecuteNonQuery();
                    //  
                    return outParam.Value.ToString();
                }
            }
        }

        public static SqlException Create_Error_Row(byte type, string text, int id_object)
        {//добавляем строку с ошибкой в базу
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (SqlException ex)
                {                 
                    return ex;
                }
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Error_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("type", SqlDbType.TinyInt); inParam.Direction = ParameterDirection.Input; inParam.Value = type; cmd.Parameters.Add(inParam);//тип ошибки из справочника  
                    inParam = new SqlParameter("text", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = text; cmd.Parameters.Add(inParam);//текст, любой (может быть имя объекта)  
                    inParam = new SqlParameter("id_object", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = id_object; cmd.Parameters.Add(inParam);//текст, любой (может быть имя объекта)                  
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
            return null;
        }

        public static int Create_Task_Row(string name, string tv_type)
        {//добавляем строку с заданием
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Task_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("type", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = tv_type; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("user_id", SqlDbType.TinyInt); inParam.Direction = ParameterDirection.Input; inParam.Value = User.ID; cmd.Parameters.Add(inParam);
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam);
                    cmd.ExecuteNonQuery();//выполняем
                    //  

                    return Convert.ToInt16(outParam.Value);//возвращаем идентификатор нового задания
                }
            }
        }

        public static void Create_MailReciever_Row(string email)
        {//добавляем строку с заданием
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_MailReciever_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = email; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("email", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = email; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("comment", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = email; cmd.Parameters.Add(inParam);   
                    //SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam);
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                    //return Convert.ToInt16(outParam.Value);//возвращаем идентификатор нового задания
                }
            }
        }

        public static int Create_Schedule_Row(string name)
        {//добавляем строку с расписанием
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Create_Schedule_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("name", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = name; cmd.Parameters.Add(inParam);//текст, любой (может быть имя объекта)  
                    SqlParameter outParam = new SqlParameter("new_id", SqlDbType.Int); outParam.Direction = ParameterDirection.Output; cmd.Parameters.Add(outParam);
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                    return Convert.ToInt16(outParam.Value);//возвращаем идентификатор нового расписания
                }
            }
        }

        public static void Delete_Task_Grid(int task_id)
        {//удаляем сетку задания
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_Task_Grid", connection))
                {
                    //cmd = new SqlCommand("dbo.Delete_Task_Grid", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("task_id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = task_id; cmd.Parameters.Add(inParam);//вход - идентификатор задания          
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
        }

        public static void Delete_Task_Row(int task_id)
        {//удаляем расписание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_Task_Row", connection))
                {
                    //cmd = new SqlCommand("dbo.Delete_Task_Row", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = task_id; cmd.Parameters.Add(inParam);//вход - идентификатор задания          
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
        }

        public static void Delete_Schedule_Row(int schedule_id)
        {//удаляем расписание
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("dbo.Delete_Schedule_Row", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    //задаём входные параметры
                    SqlParameter inParam = new SqlParameter("id", SqlDbType.Int); inParam.Direction = ParameterDirection.Input; inParam.Value = schedule_id; cmd.Parameters.Add(inParam);//вход - идентификатор расписания          
                    cmd.ExecuteNonQuery();//выполняем
                    //  
                }
            }
        }

        public static DataTable Return_Connections_Errors(DateTime ndate, DateTime kdate)
        {//возвращаем ошибки по подключениям
            string query = "select c.id as id_obj, c.district, c.name, c.phone_number, b.name as error_name, c.comments, a.date_time from dbo.errors a, dbo.error_type b, dbo.connection_points c where a.id_type = 1 " +
               " and a.date_time >= @ndate and a.date_time <= @kdate and a.id_type = b.id and a.id_object = c.id order by a.date_time desc";

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("ndate", ndate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("kdate", kdate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }

        public static DataTable Return_FastErrors_List(int day_shift)
        {//быстрый отчёт об ошибках
            string query =  "SELECT b.district, b.phone_number, a.message, b.comments FROM[Media].[dbo].[ERRORS] a" +
            ", CONNECTION_POINTS b " +
            "where a.id_type = 1 and a.date_time > (getdate() - @d) and a.date_time<getdate() and a.id_object = b.id " +
            "group by b.comments, b.district, b.phone_number, a.message having count(a.message) > @d - 1" +
            "order by district";         

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("d", day_shift); inParam.DbType = DbType.Int16; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }

        public static DataTable Return_Counters_PLC_Errors(DateTime ndate, DateTime kdate)
        {//возвращаем ошибки по PLC          
            string query = "select c.id as id_obj, e.district, e.name, c.name, c.street + ' ' + c.house + ' (' + convert(varchar(4), c.net_address) + ')' as full_name, c.serial_number, b.name as error_name, " +
                            "a.message, c.comments, a.date_time from dbo.errors a, dbo.error_type b, dbo.counters_plc c, dbo.CONCENTRATOR_POINTS d, dbo.CONNECTION_POINTS e where a.id_type in (4,5) and c.id_concentrator = d.id " +
                            "and d.id_connection = e.id and a.date_time >= @ndate and a.date_time <= @kdate " +
                            "and a.id_type = b.id and a.id_object = c.id order by a.date_time desc";

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("ndate", ndate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("kdate", kdate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }

        public static DataTable Return_Counters_RS_Errors(DateTime ndate, DateTime kdate)
        {//возвращаем ошибки по RS        
            string query = "select c.id as id_obj, d.district, d.name, c.name, c.street + ' ' + c.house + ' (' + convert(varchar(4), c.net_address) + ')' as full_name, " +
                           "c.serial_number, b.name as error_name, c.comments, a.date_time " +
                           "from dbo.errors a, dbo.error_type b, dbo.counters_rs c, dbo.CONNECTION_POINTS d " +
                           "where a.id_type in (2) and a.date_time >= @ndate and a.date_time <= @kdate " +
                           "and a.id_type = b.id and a.id_object = c.id and c.id_connection = d.id order by a.date_time desc";

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("ndate", ndate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("kdate", kdate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }

        public static DataTable Return_Changes(DateTime ndate, DateTime kdate, string tablename)
        {//возвращаем данные из таблицы изменений    
            string query = "select b.name, a.field, a.old_value, a.new_value, a.timedate, a.username from dbo.Changes a, dbo."
                + tablename + " b where a.timedate >= @ndate and a.timedate <= @kdate and a.id_object = b.id ";
            //+" union "
            //+ " select b.name, a.field, a.old_value, a.new_value, a.timedate, a.username from dbo.Changes a, dbo."
            //+ tablename + " b where a.timedate >= @ndate and a.timedate <= @kdate and a.new_value is null and a.old_value is not null";

            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("ndate", ndate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("kdate", kdate); inParam.DbType = DbType.DateTime; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }


        public static string BuildQueryString(DataGridView dgv)
        {
            StringBuilder QueryString = new StringBuilder("1=1");
            //составляем запрос на основании строковых полей поиска из грида
            //циклимся по столбцам грида
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (column.Index == 0) continue;
                ToolStripTextBox tstb = (ToolStripTextBox)column.Tag;

                if (column.ValueType == Type.GetType("System.String")) QueryString.Insert(QueryString.Length, " and " + column.Name + " like '%" + tstb.Text + "%'");//когда работаем со строковым столбцом
                if (column.ValueType == Type.GetType("System.DateTime")) //теперь случай с датой        
                {
                    if (tstb.Text.Length < 11)//если меньше 11 символов, то возвращаем всё
                        QueryString.Insert(QueryString.Length, " and " + column.Name + ">='01.01.2000'");
                    else///иначе есть смысл применять фильтр по дате                        
                    {
                        QueryString.Insert(QueryString.Length, " and " + column.Name + tstb.Text + "'");
                        QueryString.Insert(QueryString.Length - 11, "'");
                    }
                }
            }
            return QueryString.ToString();
        }


        public static void FilterApply(object sender, EventArgs e)
        {
            ToolStripTextBox textbox = sender as ToolStripTextBox;
            BindingNavigator bn = (BindingNavigator)textbox.GetCurrentParent();
            DataGridView dgv = (DataGridView)bn.Parent;

            if (FilterByValue(dgv, BuildQueryString(dgv)) == -1)//если ничего не нашли
            {
                return;
            }
        }

        public static int FilterByValue(DataGridView dgv, string query)
        {//фильтр грида по указаннному запросу
            try
            {
                if (dgv.DataSource.GetType() == typeof(DataTable))
                {
                    DataTable dt = (DataTable)dgv.DataSource;
                    dt.DefaultView.RowFilter = query;
                }

                if (dgv.DataSource.GetType() == typeof(BindingSource))
                {
                    BindingSource bs = (BindingSource)dgv.DataSource;
                    bs.Filter = query;
                }

                dgv.Refresh();
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        public static int FilterByValue(DataGridView dgrv, DataGridViewColumn dc, string criteria)
        {//фильтр столбца любого грида по значению фильтра
            try
            {         
                string columnname = dc.Name;//запоминаем имя столбца             
                string query = String.Empty; //формируем запрос с учётом имени столбца
                //проверяем тип столбца для того чтобы правильно построить запрос с его учётом                
                if (dc.ValueType == Type.GetType("System.String")) query = columnname + " like '%" + criteria + "%'";                                                 
                if (dc.ValueType == Type.GetType("System.DateTime")) //теперь случай с датой        
                {
                    if (criteria.Count() < 11) return -1;////хотим видеть определённый формат строки поиска - не меньше 10 знаков даты и один знак сравнения
                    query = columnname + criteria.Insert(1, "'").Insert(criteria.Length + 1, "'");
                }

                DataTable dt = (DataTable)dgrv.DataSource;
                dt.DefaultView.RowFilter = query;
                dgrv.Refresh();
                return 0;
            }
            catch 
            {                
                return -1;
            }
        }

        public static int FilterByValue(DataGridView dgrv, ToolStripTextBox tooltext)
        {//фильтр столбца любого грида по значению фокусной ячейки либо выпадающего текстового поля из контекстного меню
            try
            {
                if (dgrv.CurrentCell == null) return -1;
                System.Data.DataTable original = (System.Data.DataTable)dgrv.DataSource;//запоминаем оригинальный набор данных
                DataGridViewColumn dc = dgrv.Columns[dgrv.CurrentCell.ColumnIndex];//получаем текущий столбец грида на котором фокус 
                string columnname = dc.Name;//запоминаем имя столбца
                                            //формируем запрос с учётом имени столбца
                string query = String.Empty;
                //проверяем тип столбца для того чтобы правильно построить запрос с его учётом
                //если строковый столбец, то используем сравнение строк
                if (tooltext.Text != String.Empty)//если выпадающее текстовое поле непустое, то фильтруем грид по значению из поля для ввода        
                {
                    if (dc.ValueType == Type.GetType("System.String")) query = columnname + " like '%" + tooltext.Text + "%'";//по-умолчанию берём значение из выпадающего текстового поля             
                    //теперь случай с датой. Делаем сравнение по датам
                    if (dc.ValueType == Type.GetType("System.DateTime")) query = columnname + tooltext.Text.Insert(1, "'").Insert(tooltext.Text.Length + 1, "'");
                    //теперь с числами 
                    if (dc.ValueType == Type.GetType("System.Int16") || dc.ValueType == Type.GetType("System.Int32")
                        || dc.ValueType == Type.GetType("System.Decimal"))
                        query = columnname + '=' + tooltext;
                }

                if (tooltext.Text == String.Empty)//если выпадающее текстовое поле пустое, то фильтруем грид по значению из выбранной ячейки грида          
                {
                    string criteria = dgrv.CurrentCell.Value.ToString();
                    if ((criteria == String.Empty) && (dc.ValueType == Type.GetType("System.DateTime")))
                    { query = columnname + " is  null"; }
                    else
                    { query = columnname + " = '" + criteria + "'"; }
                }

                DataRow[] filter = original.Select(query);//фильтруем данные согласно запросу
                System.Data.DataTable filteredTable = filter.CopyToDataTable();
                dgrv.DataSource = null;
                dgrv.DataSource = filteredTable;
                dgrv.Refresh();
                return filteredTable.Rows.Count;
            }
            catch
            {
                //MessageBox.Show("По вашему запросу ничего не было найдено!", "Результат поиска",
                //MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return -1;
            }
        }

        public static DataTable Return_Last_PLC_Error(int id_object, int id_type)
        {//возвращаем последнюю ошибку по PLC для выбранного счётчика нужного типа         
            string query = "select max(date_time) as dt, message from dbo.Errors where id_object = @id_object and id_type = @id_type group by message order by dt desc";
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
          
            connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    SqlParameter inParam = new SqlParameter("id_object", id_object); inParam.DbType = DbType.Int16; cmd.Parameters.Add(inParam);
                    inParam = new SqlParameter("id_type", id_type); inParam.DbType = DbType.Int16; cmd.Parameters.Add(inParam);

                    using (DataTable dt = new DataTable())
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            try
                            {
                                dt.Load(dr);
                                return dt;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message.ToString());
                                return dt;
                            }
                        }
                    }
                }
            }
        }

        public static void Update_Last_PLC_Energy(double energy, int zone, int id, DateTime? dt)
        {//в этой процедуре насильно обновляем последние показания счётчика PLC для исправления перерасхода
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = String.Empty;

                if (dt == null)
                {//вариант когда дата не указана
                    //q.Append("update dbo.Counters_plc set e_t");
                    //q.Append(zone);
                    //q.Append("_last = ");
                    //q.Append(energy);
                    //q.Append(" where id = ");
                    //q.Append(id);

                    query = "update dbo.Counters_plc set e_t" + zone + "_last = " + energy.ToString() + " where id = " + id.ToString();
                }
                else
                {//вариант когда дату подали
                    query = "update dbo.Counters_plc set e_t" + zone + "_last = " + energy.ToString() + ", e_t" + zone + "_last_date = '" + dt.ToString() + "' where id = " + id.ToString();
                }

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        //  
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
                //  
            }
        }


        public static void Update_Last_PLC_Energy(int zone, int id, DateTime? dt)
        {//в этой процедуре насильно обновляем дату последних показаний
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                string query = String.Empty;
               
                {//вариант когда дату подали
                    query = "update dbo.Counters_plc set e_t" + zone + "_last_date = '" + dt.ToString() + "' where id = " + id.ToString();
                }

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        SqlDataReader dr = cmd.ExecuteReader();
                    }
                    catch (Exception ex)
                    {                         
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
                //  
            }
        }

        public static DataTable Return_Connections_List()
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы список подключений
                DataTable dt = new DataTable();
                string query = "select id, district, name, phone_number, street, comments from dbo.connection_points";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим
                              
                            return dt;
                        }
                          
                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }

        public static void Return_Connections_List(DataGridView dgv)
        {//перегрузка с возможностью изменения данных через грид
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы список подключений
                DataTable dt = new DataTable();
                string query = "select id, district, name, phone_number, street, comments from dbo.connection_points";

                try
                {
                    //using (SqlDataAdapter da = new SqlDataAdapter(query, connection))
                    {
                            SqlDataAdapter da = new SqlDataAdapter(query, connection);//создаём адаптер данных (позволяет иметь обратную связь с источником данных, а не только считывать)
                            SqlCommand cmd = new SqlCommand("UPDATE dbo.Connection_Points SET id = @id, district = @district, name = @name, phone_number = @phone_number, "+
                                                            " street = @street, comments = @comments where id = @old_id", connection);
                            //описываем команду обновления чтобы она автоматически отрабатывала по изменении данных в гриде
                            cmd.Parameters.Add("@id", SqlDbType.Int, 10, "id");
                            cmd.Parameters.Add("@district", SqlDbType.NVarChar, 150, "district");
                            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 150, "name");
                            cmd.Parameters.Add("@phone_number", SqlDbType.NVarChar, 20, "phone_number");
                            cmd.Parameters.Add("@street", SqlDbType.NVarChar, 150, "street");
                            cmd.Parameters.Add("@comments", SqlDbType.NVarChar, 3500, "comments");
                            SqlParameter parameter = cmd.Parameters.Add("@old_id", SqlDbType.NChar, 10, "id");
                            parameter.SourceVersion = DataRowVersion.Original;
                            da.UpdateCommand = cmd;                
                            da.Fill(dt);
                            BindingSource bs = new BindingSource();
                            bs.DataSource = dt;
                            dgv.DataSource = bs;
                            dgv.Tag = da;//помещаем DataAdapter в Tag грида чтобы потом его легко получить
                              
                    }
                      
                }
                catch (Exception ex)
                {
                      
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }

        public static DataTable Return_CounterPLC_List()
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы данных все PLC счётчики
                DataTable dt = new DataTable();
                string query = "select a.id, a.district, c.name as name_tp, a.name, a.street, a.house, a.serial_number, cast(a.net_address as varchar) as net_adr, cast(a.e_t0_last as varchar) as last_energy"
                    + ", cast(isnull(a.e_t0_last_date,'01.01.2000') AS DATE), a.comments from dbo.counters_plc a, dbo.concentrator_points b, dbo.connection_points c where a.id_concentrator = b.id and b.id_connection = c.id --and a.street = '123'";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим
                              
                            return dt;
                        }

                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    }

                      
                    return dt;
                }
            }
        }

        public static DataTable Return_CounterRS_List()
        {
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                //процедура, вытаскивающая из базы данных все счётчики по витой паре
                DataTable dt = new DataTable();
                string query = "select a.id, a.district, b.name as name_tp, a.name, a.street, a.house, a.serial_number, cast(a.net_address as varchar), cast(isnull(a.e_last_date,'01.01.2000') AS DATE), a.comments from dbo.counters_rs a, " +
                    "dbo.connection_points b where a.id_connection=b.id";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader()) dt.Load(dr);
                        if (dt.Rows.Count == 0)
                        {
                            //если ничего не вернулось, то выходим
                              
                            return dt;
                        }
                          
                     
                        return dt;
                    }
                    catch (Exception ex)
                    {
                          
                        MessageBox.Show(ex.Message.ToString());
                    } 
                      
                    return dt;
                }
            }
        }

        public static DataTable Return_District_Statistics(string district, DateTime energyLastDate, DateTime errorLastDate)
        {//возвращаем статистику по району
            using (SqlConnection connection = new SqlConnection(Properties.Settings.Default.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                using (SqlCommand cmd = new SqlCommand("dbo.Return_District_Statistics", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {                     
                        //задаём входные параметры
                        SqlParameter inParam = new SqlParameter("district", SqlDbType.NVarChar); inParam.Direction = ParameterDirection.Input; inParam.Value = district; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("energy_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = energyLastDate; cmd.Parameters.Add(inParam);
                        inParam = new SqlParameter("error_last_date", SqlDbType.DateTime); inParam.Direction = ParameterDirection.Input; inParam.Value = errorLastDate; cmd.Parameters.Add(inParam);
                        da.Fill(dt);
                          
                        return dt;
                    }
                }
            }
        }
    }
}

