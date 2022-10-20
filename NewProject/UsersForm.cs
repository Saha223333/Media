using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Data.SqlClient;

namespace NewProject
{
    public partial class UsersForm : Form
    {
        public UsersForm()
        {
            InitializeComponent();
            
        }

        private void RefreshUsersGrid()
        {
            DataTable UsersTable = DataBaseManagerMSSQL.Return_Users();//вытаскиваем список пользователей
            UsersGrid.DataSource = UsersTable;//привязываем грид к таблице            
        }

        private void AddComboBoxes()
        {//здесь добавляем выпадающие списки с ролями в грид                  
            if (UsersGrid[3, UsersGrid.CurrentCell.RowIndex].GetType() != typeof(DataGridViewComboBoxCell))
                {
                    DataGridViewComboBoxCell RoleComboBox = new DataGridViewComboBoxCell();
                    RoleComboBox.FlatStyle = FlatStyle.Flat;
                    RoleComboBox.Items.Add("editor");
                    RoleComboBox.Items.Add("reader");
                    RoleComboBox.Items.Add("observer");
                    UsersGrid[3, UsersGrid.CurrentCell.RowIndex] = RoleComboBox;
                }                      
        }

        private void UsersGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {           
            if (e.RowIndex == -1) return;//щелчок по заголовку нам не нравится

            if (e.ColumnIndex == 3) AddComboBoxes();//если щелкаем по ячейкам в третьем столбце, то создаем выпадающие списки 
            //ПРИШЛОСЬ ЗАСУНУТЬ ЭТО СЮДА Т.К. ВО ВРЕМЯ ОБНОВЛЕНИЯ ГРИДА ВЫПАДАЮЩИЕ СПИСКИ ПОЧЕМУ-ТО НЕ СОЗДАЮТСЯ!!!!!!
            //заставляем выпадающие списки раскрыться с первого клика (изначально это не так)
            if (UsersGrid[e.ColumnIndex, e.RowIndex].GetType() == typeof(DataGridViewComboBoxCell))
             {
                    UsersGrid.BeginEdit(true);//вводим нажатую ячейку в режим редактирования
                    ComboBox comboBox = (ComboBox)UsersGrid.EditingControl;//получаем контрол, находящийся в ячейке, если он в режиме редактирования
                    comboBox.DroppedDown = true;//раскрываем выпадающий список
             }         
        }

        private void UpdateUserButt_Click(object sender, EventArgs e)
        {
            DataBaseManagerMSSQL.Update_User(Convert.ToByte( UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[0].Value.ToString()),
                                                             UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[1].Value.ToString(),
                                                             UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[2].Value.ToString(),
                                                             UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[3].Value.ToString()
                                            );
            RefreshUsersGrid();
        }

        private void UsersGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            //
        }

        private void DeleteUserButt_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Точно удалить?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;
            DataBaseManagerMSSQL.Delete_User(Convert.ToInt16(UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[0].Value.ToString()));
            RefreshUsersGrid();
        }

        private void AddUserButt_Click(object sender, EventArgs e)
        {
            if ((LoginTextBox.Text == String.Empty) || (NameTextBox.Text == String.Empty) || (RoleComboBox.Text == String.Empty))
            {
                MessageBox.Show("Не хватает данных для создания пользователя!", "Ошибка создания пользователя",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SqlException ex = DataBaseManagerMSSQL.Create_Server_Login_And_DBUser(LoginTextBox.Text);
            if (ex != null)
            {
                MessageBox.Show(ex.Message, "Ошибка создания пользователя!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DataBaseManagerMSSQL.Create_User(LoginTextBox.Text, NameTextBox.Text, RoleComboBox.Text);
            RefreshUsersGrid();
        }

        private void DelegateRole_Click(object sender, EventArgs e)
        {
            DataBaseManagerMSSQL.Delegate_Admin_Role(
                Convert.ToByte(UsersGrid.Rows[UsersGrid.CurrentCell.RowIndex].Cells[0].Value.ToString())
                );

            MessageBox.Show("Права администратора и задания переданы другому пользователю! Изменения вступят в силу после перезагрузки приложения.", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshUsersGrid();
        }

        private void UsersGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            //оформляем грид 
            UsersGrid.Columns[0].Visible = false;
            UsersGrid.Columns[1].HeaderText = "Логин";
            UsersGrid.Columns[2].HeaderText = "Имя";
            UsersGrid.Columns[3].HeaderText = "Роль";
            UsersGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Yellow;
            UsersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            UsersGrid.EnableHeadersVisualStyles = false;
        }

        private void UsersForm_Shown(object sender, EventArgs e)
        {
          
        }

        private void UsersForm_Load(object sender, EventArgs e)
        {
            RefreshUsersGrid();//обновляем грид с пользователями   
        }
    }
}
