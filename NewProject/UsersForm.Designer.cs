namespace NewProject
{
    partial class UsersForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UsersForm));
            this.UsersFormLayout = new System.Windows.Forms.TableLayoutPanel();
            this.UsersGrid = new System.Windows.Forms.DataGridView();
            this.ButtFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.UpdateUserButt = new System.Windows.Forms.Button();
            this.DeleteUserButt = new System.Windows.Forms.Button();
            this.DelegateRole = new System.Windows.Forms.Button();
            this.FieldFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.LoginLabel = new System.Windows.Forms.Label();
            this.LoginTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.RoleLabel = new System.Windows.Forms.Label();
            this.RoleComboBox = new System.Windows.Forms.ComboBox();
            this.AddUserButt = new System.Windows.Forms.Button();
            this.UsersFormLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UsersGrid)).BeginInit();
            this.ButtFlowLayout.SuspendLayout();
            this.FieldFlowLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // UsersFormLayout
            // 
            this.UsersFormLayout.ColumnCount = 2;
            this.UsersFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.UsersFormLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.UsersFormLayout.Controls.Add(this.UsersGrid, 0, 0);
            this.UsersFormLayout.Controls.Add(this.ButtFlowLayout, 0, 1);
            this.UsersFormLayout.Controls.Add(this.FieldFlowLayout, 1, 1);
            this.UsersFormLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UsersFormLayout.Location = new System.Drawing.Point(0, 0);
            this.UsersFormLayout.Name = "UsersFormLayout";
            this.UsersFormLayout.RowCount = 2;
            this.UsersFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.UsersFormLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.UsersFormLayout.Size = new System.Drawing.Size(960, 467);
            this.UsersFormLayout.TabIndex = 0;
            // 
            // UsersGrid
            // 
            this.UsersGrid.AllowUserToAddRows = false;
            this.UsersGrid.AllowUserToDeleteRows = false;
            this.UsersGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.UsersFormLayout.SetColumnSpan(this.UsersGrid, 2);
            this.UsersGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UsersGrid.Location = new System.Drawing.Point(3, 3);
            this.UsersGrid.MultiSelect = false;
            this.UsersGrid.Name = "UsersGrid";
            this.UsersGrid.RowHeadersVisible = false;
            this.UsersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.UsersGrid.Size = new System.Drawing.Size(954, 274);
            this.UsersGrid.TabIndex = 0;
            this.UsersGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.UsersGrid_CellClick);
            this.UsersGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.UsersGrid_DataBindingComplete);
            this.UsersGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.UsersGrid_DataError);
            // 
            // ButtFlowLayout
            // 
            this.ButtFlowLayout.Controls.Add(this.UpdateUserButt);
            this.ButtFlowLayout.Controls.Add(this.DeleteUserButt);
            this.ButtFlowLayout.Controls.Add(this.DelegateRole);
            this.ButtFlowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ButtFlowLayout.Location = new System.Drawing.Point(3, 283);
            this.ButtFlowLayout.Name = "ButtFlowLayout";
            this.ButtFlowLayout.Size = new System.Drawing.Size(474, 181);
            this.ButtFlowLayout.TabIndex = 1;
            // 
            // UpdateUserButt
            // 
            this.UpdateUserButt.Location = new System.Drawing.Point(3, 3);
            this.UpdateUserButt.Name = "UpdateUserButt";
            this.UpdateUserButt.Size = new System.Drawing.Size(75, 23);
            this.UpdateUserButt.TabIndex = 2;
            this.UpdateUserButt.Text = "Сохранить";
            this.UpdateUserButt.UseVisualStyleBackColor = true;
            this.UpdateUserButt.Click += new System.EventHandler(this.UpdateUserButt_Click);
            // 
            // DeleteUserButt
            // 
            this.DeleteUserButt.Location = new System.Drawing.Point(84, 3);
            this.DeleteUserButt.Name = "DeleteUserButt";
            this.DeleteUserButt.Size = new System.Drawing.Size(75, 23);
            this.DeleteUserButt.TabIndex = 1;
            this.DeleteUserButt.Text = "Удалить";
            this.DeleteUserButt.UseVisualStyleBackColor = true;
            this.DeleteUserButt.Click += new System.EventHandler(this.DeleteUserButt_Click);
            // 
            // DelegateRole
            // 
            this.DelegateRole.Location = new System.Drawing.Point(165, 3);
            this.DelegateRole.Name = "DelegateRole";
            this.DelegateRole.Size = new System.Drawing.Size(184, 23);
            this.DelegateRole.TabIndex = 3;
            this.DelegateRole.Text = "Передать права администратора";
            this.DelegateRole.UseVisualStyleBackColor = true;
            this.DelegateRole.Click += new System.EventHandler(this.DelegateRole_Click);
            // 
            // FieldFlowLayout
            // 
            this.FieldFlowLayout.Controls.Add(this.LoginLabel);
            this.FieldFlowLayout.Controls.Add(this.LoginTextBox);
            this.FieldFlowLayout.Controls.Add(this.NameLabel);
            this.FieldFlowLayout.Controls.Add(this.NameTextBox);
            this.FieldFlowLayout.Controls.Add(this.RoleLabel);
            this.FieldFlowLayout.Controls.Add(this.RoleComboBox);
            this.FieldFlowLayout.Controls.Add(this.AddUserButt);
            this.FieldFlowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FieldFlowLayout.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.FieldFlowLayout.Location = new System.Drawing.Point(483, 283);
            this.FieldFlowLayout.Name = "FieldFlowLayout";
            this.FieldFlowLayout.Size = new System.Drawing.Size(474, 181);
            this.FieldFlowLayout.TabIndex = 2;
            // 
            // LoginLabel
            // 
            this.LoginLabel.AutoSize = true;
            this.LoginLabel.Location = new System.Drawing.Point(3, 0);
            this.LoginLabel.Name = "LoginLabel";
            this.LoginLabel.Size = new System.Drawing.Size(38, 13);
            this.LoginLabel.TabIndex = 0;
            this.LoginLabel.Text = "Логин";
            // 
            // LoginTextBox
            // 
            this.LoginTextBox.Location = new System.Drawing.Point(3, 16);
            this.LoginTextBox.Name = "LoginTextBox";
            this.LoginTextBox.Size = new System.Drawing.Size(459, 20);
            this.LoginTextBox.TabIndex = 1;
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(3, 39);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(29, 13);
            this.NameLabel.TabIndex = 2;
            this.NameLabel.Text = "Имя";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(3, 55);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(459, 20);
            this.NameTextBox.TabIndex = 3;
            // 
            // RoleLabel
            // 
            this.RoleLabel.AutoSize = true;
            this.RoleLabel.Location = new System.Drawing.Point(3, 78);
            this.RoleLabel.Name = "RoleLabel";
            this.RoleLabel.Size = new System.Drawing.Size(32, 13);
            this.RoleLabel.TabIndex = 4;
            this.RoleLabel.Text = "Роль";
            // 
            // RoleComboBox
            // 
            this.RoleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RoleComboBox.FormattingEnabled = true;
            this.RoleComboBox.Items.AddRange(new object[] {
            "editor",
            "reader",
            "observer"});
            this.RoleComboBox.Location = new System.Drawing.Point(3, 94);
            this.RoleComboBox.Name = "RoleComboBox";
            this.RoleComboBox.Size = new System.Drawing.Size(121, 21);
            this.RoleComboBox.TabIndex = 5;
            // 
            // AddUserButt
            // 
            this.AddUserButt.Location = new System.Drawing.Point(3, 121);
            this.AddUserButt.Name = "AddUserButt";
            this.AddUserButt.Size = new System.Drawing.Size(75, 23);
            this.AddUserButt.TabIndex = 0;
            this.AddUserButt.Text = "Добавить";
            this.AddUserButt.UseVisualStyleBackColor = true;
            this.AddUserButt.Click += new System.EventHandler(this.AddUserButt_Click);
            // 
            // UsersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 467);
            this.Controls.Add(this.UsersFormLayout);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UsersForm";
            this.Text = "Пользователи";
            this.Load += new System.EventHandler(this.UsersForm_Load);
            this.Shown += new System.EventHandler(this.UsersForm_Shown);
            this.UsersFormLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UsersGrid)).EndInit();
            this.ButtFlowLayout.ResumeLayout(false);
            this.FieldFlowLayout.ResumeLayout(false);
            this.FieldFlowLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel UsersFormLayout;
        private System.Windows.Forms.DataGridView UsersGrid;
        private System.Windows.Forms.FlowLayoutPanel ButtFlowLayout;
        private System.Windows.Forms.Button AddUserButt;
        private System.Windows.Forms.Button DeleteUserButt;
        private System.Windows.Forms.Button UpdateUserButt;
        private System.Windows.Forms.FlowLayoutPanel FieldFlowLayout;
        private System.Windows.Forms.Label LoginLabel;
        private System.Windows.Forms.TextBox LoginTextBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label RoleLabel;
        private System.Windows.Forms.ComboBox RoleComboBox;
        private System.Windows.Forms.Button DelegateRole;
    }
}