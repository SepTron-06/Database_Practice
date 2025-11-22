using System;
using System.Windows.Forms;
using WinFormsDB.Data;

namespace WinFormsDB.Forms
{
    public partial class DatabaseConnectionForm : Form
    {
        public string DatabaseName { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public string Host { get; private set; } = "localhost";
        public string Port { get; private set; } = "5432";
        public string Username { get; private set; } = "postgres";

        public DatabaseConnectionForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Подключение к базе данных";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(20);

            // Хост
            var lblHost = new Label
            {
                Text = "Хост:",
                Location = new Point(20, 20),
                Width = 120,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var txtHost = new TextBox
            {
                Location = new Point(150, 18),
                Width = 200,
                Font = new Font("Arial", 9),
                Text = "localhost"
            };

            // Порт
            var lblPort = new Label
            {
                Text = "Порт:",
                Location = new Point(20, 60),
                Width = 120,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var txtPort = new TextBox
            {
                Location = new Point(150, 58),
                Width = 200,
                Font = new Font("Arial", 9),
                Text = "5432"
            };

            // Имя базы данных
            var lblDatabase = new Label
            {
                Text = "База данных:",
                Location = new Point(20, 100),
                Width = 120,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var txtDatabase = new TextBox
            {
                Location = new Point(150, 98),
                Width = 200,
                Font = new Font("Arial", 9),
                PlaceholderText = "Введите название базы данных"
            };

            // Имя пользователя
            var lblUsername = new Label
            {
                Text = "Пользователь:",
                Location = new Point(20, 140),
                Width = 120,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var txtUsername = new TextBox
            {
                Location = new Point(150, 138),
                Width = 200,
                Font = new Font("Arial", 9),
                Text = "postgres"
            };

            // Пароль
            var lblPassword = new Label
            {
                Text = "Пароль:",
                Location = new Point(20, 180),
                Width = 120,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var txtPassword = new TextBox
            {
                Location = new Point(150, 178),
                Width = 200,
                Font = new Font("Arial", 9),
                UseSystemPasswordChar = true,
                PlaceholderText = "Введите пароль"
            };

            // Метка для ошибок
            var lblError = new Label
            {
                Text = "",
                Location = new Point(20, 210),
                Width = 340,
                Height = 20,
                ForeColor = Color.Red,
                Font = new Font("Arial", 8, FontStyle.Bold),
                Visible = false
            };

            // Кнопки
            var btnConnect = new Button
            {
                Text = "Подключиться",
                Location = new Point(150, 235),
                Size = new Size(110, 30),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(260, 235),
                Size = new Size(80, 30),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 9)
            };

            // Функция для отображения ошибки
            void ShowError(string message)
            {
                lblError.Text = message;
                lblError.Visible = true;
            }

            // Функция для скрытия ошибки
            void HideError()
            {
                lblError.Text = "";
                lblError.Visible = false;
            }

            // Функция валидации
            bool ValidateForm()
            {
                HideError();

                if (string.IsNullOrWhiteSpace(txtHost.Text))
                {
                    ShowError("Поле 'Хост' обязательно для заполнения");
                    txtHost.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtPort.Text))
                {
                    ShowError("Поле 'Порт' обязательно для заполнения");
                    txtPort.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtDatabase.Text))
                {
                    ShowError("Поле 'База данных' обязательно для заполнения");
                    txtDatabase.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    ShowError("Поле 'Пользователь' обязательно для заполнения");
                    txtUsername.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    ShowError("Поле 'Пароль' обязательно для заполнения");
                    txtPassword.Focus();
                    return false;
                }

                return true;
            }

            // Обработчик кнопки подключения
            btnConnect.Click += async (s, e) =>
            {
                if (!ValidateForm())
                    return;

                try
                {
                    btnConnect.Enabled = false;
                    btnConnect.Text = "Проверка...";
                    btnCancel.Enabled = false;

                    // Сохраняем введенные данные
                    Host = txtHost.Text.Trim();
                    Port = txtPort.Text.Trim();
                    DatabaseName = txtDatabase.Text.Trim();
                    Username = txtUsername.Text.Trim();
                    Password = txtPassword.Text;

                    // Тестируем подключение
                    var connectionString = $"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}";
                    var testConnection = new DatabaseConnection(connectionString);

                    // Пытаемся подключиться
                    using (var connection = await testConnection.GetConnectionAsync())
                    {
                        // Если дошли сюда - подключение успешно
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    btnConnect.Enabled = true;
                    btnConnect.Text = "Подключиться";
                    btnCancel.Enabled = true;
                    ShowError($"Ошибка подключения: {ex.Message}");
                }
            };

            // Обработчик кнопки отмены
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // Обработчики изменения текста для скрытия ошибок
            void HideErrorOnTextChange(object sender, EventArgs e)
            {
                HideError();
            }

            txtHost.TextChanged += HideErrorOnTextChange;
            txtPort.TextChanged += HideErrorOnTextChange;
            txtDatabase.TextChanged += HideErrorOnTextChange;
            txtUsername.TextChanged += HideErrorOnTextChange;
            txtPassword.TextChanged += HideErrorOnTextChange;

            // Настройка формы
            this.AcceptButton = btnConnect;
            this.CancelButton = btnCancel;

            // Добавляем контролы на форму
            this.Controls.AddRange(new Control[]
            {
                lblHost, txtHost,
                lblPort, txtPort,
                lblDatabase, txtDatabase,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblError,
                btnConnect, btnCancel
            });

            txtDatabase.Focus();
        }
    }
}