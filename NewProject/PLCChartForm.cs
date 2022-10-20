using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NewProject
{
    public partial class PLCChartForm : Form
    {//на это форме выводим график показаний PLC-счётчика
        public PLCChartForm(DataTable dt)
        {
            InitializeComponent();                    

            //======================================ГРАФИК ПОКЗАНИЙ=============================================================
            System.Windows.Forms.DataVisualization.Charting.SeriesChartType charttype;
            charttype = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            
            ChartHistory.Series.Clear(); //очищаем все существующие серии
                                  //создаём серии
            ChartHistory.Series.Add("Сумма"); //по сумме
            ChartHistory.Series["Сумма"].ChartType = charttype;
            ChartHistory.Series["Сумма"].XValueMember = "date_time_t0";
            ChartHistory.Series["Сумма"].YValueMembers = "energy_t0";
            ChartHistory.Series["Сумма"].BorderWidth = 2;
            ChartHistory.Series["Сумма"].EmptyPointStyle.Color = Color.Yellow;
            ChartHistory.Series["Сумма"].EmptyPointStyle.BorderWidth = 2;
            ChartHistory.Series["Сумма"].EmptyPointStyle.BorderColor = Color.Yellow;

            ChartHistory.Series.Add("Тариф 1"); //тариф 1
            ChartHistory.Series["Тариф 1"].ChartType = charttype;
            ChartHistory.Series["Тариф 1"].XValueMember = "date_time_t1";
            ChartHistory.Series["Тариф 1"].YValueMembers = "energy_t1";

            ChartHistory.Series.Add("Тариф 2");//тариф 2
            ChartHistory.Series["Тариф 2"].ChartType = charttype;
            ChartHistory.Series["Тариф 2"].XValueMember = "date_time_t2";
            ChartHistory.Series["Тариф 2"].YValueMembers = "energy_t2";

            ChartHistory.Series.Add("Тариф 3");//тариф 3
            ChartHistory.Series["Тариф 3"].ChartType = charttype;
            ChartHistory.Series["Тариф 3"].XValueMember = "date_time_t3";
            ChartHistory.Series["Тариф 3"].YValueMembers = "energy_t3";

            ChartHistory.Series.Add("Тариф 4");//тариф 4
            ChartHistory.Series["Тариф 4"].ChartType = charttype;
            ChartHistory.Series["Тариф 4"].XValueMember = "date_time_t4";
            ChartHistory.Series["Тариф 4"].YValueMembers = "energy_t4";

            ChartHistory.DataSource = dt;
            ChartHistory.DataBind();

            ChartHistory.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            ChartHistory.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            //======================================ГРАФИК ПОТРЕБЛЕНИЯ=============================================================
            //нужно создать новую таблицу на основе старой
            DataTable dt_deriv = new DataTable();
            //создаём столбцы
            DataColumn dc = new DataColumn("delta");//расход
            dc.DataType = System.Type.GetType("System.Double");
            dt_deriv.Columns.Add(dc);
            
            dc = new DataColumn("delta_avg");//средний расход
            dc.DataType = System.Type.GetType("System.Double");
            dt_deriv.Columns.Add(dc); 

            dc = new DataColumn("date_time_t0");//верхняя дата
            dt_deriv.Columns.Add(dc);


            for (int i = 0; i < dt.Rows.Count - 1; i++)
            {
                int a = Convert.ToInt32(dt.Rows[i][0]);//показание с большей датой
                int b = Convert.ToInt32(dt.Rows[i + 1][0]);//показание с меньше датой
              
                double delta = a - b;//разница между двумя показаниями
               
                DateTime d1 = Convert.ToDateTime(dt.Rows[i][1]);
                DateTime d2 = Convert.ToDateTime(dt.Rows[i + 1][1]);
                d1 = d1.Date;//нужно округлить дату чтобы разница была минимум сутки
                d2 = d2.Date;//нужно округлить дату чтобы разница была минимум сутки
                TimeSpan s = d1.Subtract(d2);//считаем разницу между датами
                int delta_days = s.Days;//разница между датами в днях

                if (delta_days == 0)//не можем делить на 0, поэтому если разницы между датами нет (два показания одним днём)
                {
                    delta_days = 1;
                }

                double delta_avg = delta / delta_days;//средний расход
                DataRow dr = dt_deriv.NewRow();//добавляем строку в таблицу

                dr["delta"] = delta;//расход                      
                dr["delta_avg"] = delta_avg;//средний расход
                dr["date_time_t0"] = Convert.ToDateTime(d1.ToShortDateString());//верхняя дата
  

                dt_deriv.Rows.Add(dr);
            }

            dt_deriv.DefaultView.Sort = "date_time_t0 asc";//отсортируем таблицу по возрастанию даты
            dt_deriv = dt_deriv.DefaultView.ToTable();

            ChartDerivative.Series.Clear();

            ChartDerivative.Series.Add("Расход, сумма"); //по сумме
            ChartDerivative.Series["Расход, сумма"].ChartType = charttype;
            ChartDerivative.Series["Расход, сумма"].XValueMember = "date_time_t0";
            ChartDerivative.Series["Расход, сумма"].YValueMembers = "delta";
            ChartDerivative.Series["Расход, сумма"].BorderWidth = 2;
            ChartDerivative.Series["Расход, сумма"].EmptyPointStyle.Color = Color.Yellow;
            ChartDerivative.Series["Расход, сумма"].EmptyPointStyle.BorderWidth = 2;
            ChartDerivative.Series["Расход, сумма"].EmptyPointStyle.BorderColor = Color.Yellow;

            ChartDerivative.Series.Add("Средний расход, сумма"); //по сумме
            ChartDerivative.Series["Средний расход, сумма"].ChartType = charttype;
            ChartDerivative.Series["Средний расход, сумма"].XValueMember = "date_time_t0";
            ChartDerivative.Series["Средний расход, сумма"].YValueMembers = "delta_avg";
            ChartDerivative.Series["Средний расход, сумма"].BorderWidth = 2;
            ChartDerivative.Series["Средний расход, сумма"].EmptyPointStyle.Color = Color.Yellow;
            ChartDerivative.Series["Средний расход, сумма"].EmptyPointStyle.BorderWidth = 2;
            ChartDerivative.Series["Средний расход, сумма"].EmptyPointStyle.BorderColor = Color.Yellow;

            ChartDerivative.DataSource = dt_deriv;
            ChartDerivative.DataBind();
        }
    }
}
