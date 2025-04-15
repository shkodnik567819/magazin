using Npgsql;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nastya_praktika
{
    public partial class AuthForm : Form
    {
        private readonly string ConnectionString = "Host=91.233.173.91;Username=selectel;Password=selectel;Database=selectel";

        public AuthForm()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            // Настройка формы
            this.Text = "Авторизация";
            this.ClientSize = new Size(350, 300);
            this.BackColor = Color.FromArgb(255, 253, 245);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Логотип
            PictureBox logo = new PictureBox();
            try
            {
                logo.Image = Properties.Resources.nastya_logo;
                logo.SizeMode = PictureBoxSizeMode.Zoom;
                logo.Size = new Size(100, 100);
                logo.Location = new Point((this.ClientSize.Width - logo.Width) / 2, 20);
                this.Controls.Add(logo);
            }
            catch
            {
                MessageBox.Show("Логотип не найден", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Надпись "Логин"
            Label lblLogin = new Label();
            lblLogin.Text = "Логин:";
            lblLogin.Location = new Point(50, 120);
            lblLogin.Size = new Size(100, 20);
            lblLogin.ForeColor = Color.FromArgb(70, 70, 70);
            this.Controls.Add(lblLogin);

            // Поле для ввода логина
            TextBox txtLogin = new TextBox();
            txtLogin.Location = new Point(50, 140);
            txtLogin.Size = new Size(250, 23);
            txtLogin.BackColor = Color.FromArgb(255, 251, 240);
            txtLogin.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtLogin);

            // Надпись "Пароль"
            Label lblPassword = new Label();
            lblPassword.Text = "Пароль:";
            lblPassword.Location = new Point(50, 180);
            lblPassword.Size = new Size(100, 20);
            lblPassword.ForeColor = Color.FromArgb(70, 70, 70);
            this.Controls.Add(lblPassword);

            // Поле для ввода пароля
            TextBox txtPassword = new TextBox();
            txtPassword.Location = new Point(50, 200);
            txtPassword.Size = new Size(250, 23);
            txtPassword.PasswordChar = '*';
            txtPassword.BackColor = Color.FromArgb(255, 251, 240);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtPassword);

            // Кнопка "Войти"
            Button btnLogin = new Button();
            btnLogin.Text = "Войти";
            btnLogin.Location = new Point(120, 240);
            btnLogin.Size = new Size(100, 30);
            btnLogin.BackColor = Color.FromArgb(230, 220, 200);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderColor = Color.FromArgb(200, 190, 180);
            btnLogin.ForeColor = Color.FromArgb(70, 70, 70);
            btnLogin.Click += (sender, e) =>
            {
                if (string.IsNullOrEmpty(txtLogin.Text) || string.IsNullOrEmpty(txtPassword.Text))
                {
                    MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
                    {
                        connection.Open();

                        string query = "SELECT id FROM candy_shop.shop WHERE login = @login AND password = @password";
                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                            command.Parameters.AddWithValue("@password", txtPassword.Text.Trim());

                            object result = command.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                int id = Convert.ToInt32(result);
                                MessageBox.Show("Добро пожаловать!", "Успешная авторизация", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                MainForm mainForm = new MainForm();
                                mainForm.stuff = id;
                                mainForm.FormClosed += (s, args) => this.Close();
                                mainForm.Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnLogin);
        }
    }
}