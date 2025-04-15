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
            // ��������� �����
            this.Text = "�����������";
            this.ClientSize = new Size(350, 300);
            this.BackColor = Color.FromArgb(255, 253, 245);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // �������
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
                MessageBox.Show("������� �� ������", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // ������� "�����"
            Label lblLogin = new Label();
            lblLogin.Text = "�����:";
            lblLogin.Location = new Point(50, 120);
            lblLogin.Size = new Size(100, 20);
            lblLogin.ForeColor = Color.FromArgb(70, 70, 70);
            this.Controls.Add(lblLogin);

            // ���� ��� ����� ������
            TextBox txtLogin = new TextBox();
            txtLogin.Location = new Point(50, 140);
            txtLogin.Size = new Size(250, 23);
            txtLogin.BackColor = Color.FromArgb(255, 251, 240);
            txtLogin.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtLogin);

            // ������� "������"
            Label lblPassword = new Label();
            lblPassword.Text = "������:";
            lblPassword.Location = new Point(50, 180);
            lblPassword.Size = new Size(100, 20);
            lblPassword.ForeColor = Color.FromArgb(70, 70, 70);
            this.Controls.Add(lblPassword);

            // ���� ��� ����� ������
            TextBox txtPassword = new TextBox();
            txtPassword.Location = new Point(50, 200);
            txtPassword.Size = new Size(250, 23);
            txtPassword.PasswordChar = '*';
            txtPassword.BackColor = Color.FromArgb(255, 251, 240);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtPassword);

            // ������ "�����"
            Button btnLogin = new Button();
            btnLogin.Text = "�����";
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
                    MessageBox.Show("������� ����� � ������", "������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                                MessageBox.Show("����� ����������!", "�������� �����������", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                MainForm mainForm = new MainForm();
                                mainForm.stuff = id;
                                mainForm.FormClosed += (s, args) => this.Close();
                                mainForm.Show();
                                this.Hide();
                            }
                            else
                            {
                                MessageBox.Show("�������� ����� ��� ������", "������ �����������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (NpgsqlException ex)
                {
                    MessageBox.Show($"������ ����������� � ���� ������: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"��������� ������: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnLogin);
        }
    }
}