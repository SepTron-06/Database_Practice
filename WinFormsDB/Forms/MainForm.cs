using WinFormsDB.Data;
using WinFormsDB.Repositories;
using WinFormsDB.Models;
using System.Windows.Forms;
using System.Drawing;

namespace WinFormsDB.Forms
{
    public class MainForm : Form
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ClientRepository _clientRepository;
        private DataGridView dataGridViewClients;
        private MenuStrip mainMenu;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        public MainForm(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _clientRepository = new ClientRepository(_dbConnection);

            InitializeForm();
            LoadClientsAsync();
        }

        private void InitializeForm()
        {
            // Настройка основной формы
            this.Text = "Управление коммунальными услугами";
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Создаем элементы интерфейса
            CreateMenu();
            CreateDataGrid();
            CreateStatusBar();
        }

        private void CreateMenu()
        {
            mainMenu = new MenuStrip();
            mainMenu.Dock = DockStyle.Top;

            // Меню "Файл"
            var fileMenu = new ToolStripMenuItem("Файл");
            var exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) => Application.Exit();
            fileMenu.DropDownItems.Add(exitItem);

            // Меню "Клиенты"
            var clientsMenu = new ToolStripMenuItem("Клиенты");
            var addClientItem = new ToolStripMenuItem("Добавить клиента");
            addClientItem.Click += (s, e) => ShowAddClientForm();
            var refreshClientsItem = new ToolStripMenuItem("Обновить список");
            refreshClientsItem.Click += (s, e) => LoadClientsAsync();
            clientsMenu.DropDownItems.Add(addClientItem);
            clientsMenu.DropDownItems.Add(refreshClientsItem);

            // Меню "Отчеты"
            var reportsMenu = new ToolStripMenuItem("Отчеты");
            var unpaidBillsItem = new ToolStripMenuItem("Неоплаченные счета");
            unpaidBillsItem.Click += (s, e) => ShowUnpaidBillsReport();
            reportsMenu.DropDownItems.Add(unpaidBillsItem);

            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(clientsMenu);
            mainMenu.Items.Add(reportsMenu);

            this.Controls.Add(mainMenu);
            this.MainMenuStrip = mainMenu;
        }

        private void CreateDataGrid()
        {
            dataGridViewClients = new DataGridView
            {
                Name = "dataGridViewClients",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 50),
                Size = new Size(1200, 600)
            };

            // Добавляем обработчик двойного клика
            dataGridViewClients.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var client = dataGridViewClients.Rows[e.RowIndex].DataBoundItem as Client;
                    if (client != null)
                    {
                        ShowClientDetails(client);
                    }
                }
            };

            this.Controls.Add(dataGridViewClients);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.Dock = DockStyle.Bottom;

            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "Готово";
            statusStrip.Items.Add(statusLabel);

            this.Controls.Add(statusStrip);
        }

        private async void LoadClientsAsync()
        {
            try
            {
                UpdateStatus("Загрузка клиентов...");
                var clients = await _clientRepository.GetClientsAsync();
                dataGridViewClients.DataSource = clients;

                UpdateStatus($"Загружено клиентов: {clients.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ошибка загрузки данных");
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }

        private void ShowAddClientForm()
        {
            try
            {
                // Временная реализация - диалог для ввода данных
                var addForm = new Form
                {
                    Text = "Добавить клиента",
                    Size = new Size(300, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var lblFirstName = new Label { Text = "Имя:", Location = new Point(10, 20), Width = 80 };
                var txtFirstName = new TextBox { Location = new Point(100, 20), Width = 150 };

                var lblLastName = new Label { Text = "Фамилия:", Location = new Point(10, 50), Width = 80 };
                var txtLastName = new TextBox { Location = new Point(100, 50), Width = 150 };

                var btnSave = new Button { Text = "Сохранить", Location = new Point(100, 100), Width = 80 };
                var btnCancel = new Button { Text = "Отмена", Location = new Point(190, 100), Width = 80 };

                btnSave.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
                    {
                        MessageBox.Show("Заполните имя и фамилию", "Ошибка");
                        return;
                    }

                    try
                    {
                        var client = new Client
                        {
                            FirstName = txtFirstName.Text,
                            LastName = txtLastName.Text
                        };

                        await _clientRepository.AddClientAsync(client);
                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        LoadClientsAsync(); // Перезагружаем список
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                addForm.Controls.AddRange(new Control[]
                {
                    lblFirstName, txtFirstName,
                    lblLastName, txtLastName,
                    btnSave, btnCancel
                });

                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка");
            }
        }

        private void ShowClientDetails(Client client)
        {
            try
            {
                var details = $"Клиент: {client.LastName} {client.FirstName}\n" +
                             $"ID: {client.ClientID}\n" +
                             $"Телефон: {client.Phone ?? "не указан"}\n" +
                             $"Email: {client.Email ?? "не указан"}";

                MessageBox.Show(details, "Информация о клиенте",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
            }
        }

        private async void ShowUnpaidBillsReport()
        {
            try
            {
                UpdateStatus("Формирование отчета...");

                var reportService = new ReportService(_dbConnection);

                // Получаем первого клиента для демонстрации
                var clients = await _clientRepository.GetClientsAsync();
                if (clients.Count == 0)
                {
                    MessageBox.Show("Нет клиентов для отчета", "Информация");
                    return;
                }

                var unpaidBills = await reportService.GetUnpaidBillsByClient(clients[0].ClientID);

                var reportText = $"Неоплаченные счета для {clients[0].LastName} {clients[0].FirstName}:\n\n";
                foreach (var bill in unpaidBills)
                {
                    reportText += $"{bill.ServiceName}: {bill.Amount:C2} (до {bill.PaymentDate:dd.MM.yyyy})\n";
                }

                if (unpaidBills.Count == 0)
                {
                    reportText += "Неоплаченных счетов нет";
                }

                MessageBox.Show(reportText, "Отчет по неоплаченным счетам",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateStatus("Отчет сформирован");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка");
                UpdateStatus("Ошибка формирования отчета");
            }
        }

        // Убираем метод Dispose, так как DatabaseConnection его не требует
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
    }
}