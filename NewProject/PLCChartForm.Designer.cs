namespace NewProject
{
    partial class PLCChartForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.ChartLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ChartHistory = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.ChartDerivative = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.ChartLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ChartHistory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChartDerivative)).BeginInit();
            this.SuspendLayout();
            // 
            // ChartLayoutPanel
            // 
            this.ChartLayoutPanel.ColumnCount = 1;
            this.ChartLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChartLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChartLayoutPanel.Controls.Add(this.ChartHistory, 0, 0);
            this.ChartLayoutPanel.Controls.Add(this.ChartDerivative, 0, 1);
            this.ChartLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ChartLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.ChartLayoutPanel.Name = "ChartLayoutPanel";
            this.ChartLayoutPanel.RowCount = 2;
            this.ChartLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChartLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ChartLayoutPanel.Size = new System.Drawing.Size(699, 508);
            this.ChartLayoutPanel.TabIndex = 0;
            // 
            // ChartHistory
            // 
            chartArea1.Name = "ChartArea1";
            this.ChartHistory.ChartAreas.Add(chartArea1);
            this.ChartHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.ChartHistory.Legends.Add(legend1);
            this.ChartHistory.Location = new System.Drawing.Point(3, 3);
            this.ChartHistory.Name = "ChartHistory";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.ChartHistory.Series.Add(series1);
            this.ChartHistory.Size = new System.Drawing.Size(693, 248);
            this.ChartHistory.TabIndex = 0;
            this.ChartHistory.Text = "chart1";
            // 
            // ChartDerivative
            // 
            chartArea2.Name = "ChartArea1";
            this.ChartDerivative.ChartAreas.Add(chartArea2);
            this.ChartDerivative.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Name = "Legend1";
            this.ChartDerivative.Legends.Add(legend2);
            this.ChartDerivative.Location = new System.Drawing.Point(3, 257);
            this.ChartDerivative.Name = "ChartDerivative";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.ChartDerivative.Series.Add(series2);
            this.ChartDerivative.Size = new System.Drawing.Size(693, 248);
            this.ChartDerivative.TabIndex = 1;
            this.ChartDerivative.Text = "chart1";
            // 
            // PLCChartForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(699, 508);
            this.Controls.Add(this.ChartLayoutPanel);
            this.Name = "PLCChartForm";
            this.Text = "График";
            this.ChartLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ChartHistory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ChartDerivative)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel ChartLayoutPanel;
        private System.Windows.Forms.DataVisualization.Charting.Chart ChartHistory;
        private System.Windows.Forms.DataVisualization.Charting.Chart ChartDerivative;
    }
}