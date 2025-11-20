using WinFormsDB.Data;
using WinFormsDB.Repositories;
using WinFormsDB.Services;
using WinFormsDB.Models;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WinFormsDB.Forms
{
    public partial class MainForm : Form
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ClientRepository _clientRepository;
        private readonly ServiceRepository _serviceRepository;
        private readonly TariffRepository _tariffRepository;
        private readonly AddressRepository _addressRepository;
        private readonly BillRepository _billRepository;

        private DataGridView? dataGridViewClients;
        private DataGridView? dataGridViewServices;
        private DataGridView? dataGridViewTariffs;
        private DataGridView? dataGridViewAddresses;
        private DataGridView? dataGridViewBills;
        private MenuStrip? mainMenu;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;

        private string currentTable = "Clients";

        public MainForm(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _ = InitializeDatabaseAsync();
            // Сначала создаем ClientRepository
            _clientRepository = new ClientRepository(_dbConnection);

            // Затем создаем остальные репозитории, передавая ClientRepository в AddressRepository
            _serviceRepository = new ServiceRepository(_dbConnection);
            _tariffRepository = new TariffRepository(_dbConnection);
            _addressRepository = new AddressRepository(_dbConnection, _clientRepository); // Добавлен clientRepository
            _billRepository = new BillRepository(_dbConnection);


            InitializeComponent();
            InitializeForm();

            // Асинхронная загрузка данных после показа формы
            this.Shown += async (s, e) => await LoadClientsAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var initializer = new DatabaseInitializer(_dbConnection);
                await initializer.InitializeDatabaseAsync();

                // Проверяем, что таблица bills существует
                bool billsTableExists = await initializer.CheckTableExistsAsync("bills");
                if (!billsTableExists)
                {
                    MessageBox.Show("Таблица bills не была создана. Проверьте подключение к БД.", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // После создания таблиц инициализируем тестовые данные в BillRepository
                await _billRepository.InitializeSampleDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeForm()
        {
            this.Text = "Управление коммунальными услугами";
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            CreateMenu();
            CreateDataGrids();
            CreateStatusBar();

            ShowTable("Clients");
        }

        // Метод для безопасного обновления UI из любого потока
        private void SafeInvoke(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
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

            // Меню "Таблицы"
            var tablesMenu = new ToolStripMenuItem("Таблицы");

            var clientsTableItem = new ToolStripMenuItem("Клиенты");
            clientsTableItem.Click += (s, e) => ShowTable("Clients");

            var servicesTableItem = new ToolStripMenuItem("Услуги");
            servicesTableItem.Click += (s, e) => ShowTable("Services");

            var tariffsTableItem = new ToolStripMenuItem("Тарифы");
            tariffsTableItem.Click += (s, e) => ShowTable("Tariffs");

            var addressesTableItem = new ToolStripMenuItem("Адреса");
            addressesTableItem.Click += (s, e) => ShowTable("Addresses");

            var billsTableItem = new ToolStripMenuItem("Счета");
            billsTableItem.Click += (s, e) => ShowTable("Bills");

            tablesMenu.DropDownItems.Add(clientsTableItem);
            tablesMenu.DropDownItems.Add(servicesTableItem);
            tablesMenu.DropDownItems.Add(tariffsTableItem);
            tablesMenu.DropDownItems.Add(addressesTableItem);
            tablesMenu.DropDownItems.Add(billsTableItem);

            // Меню "Клиенты"
            var clientsMenu = CreateClientsMenu();

            // Меню "Услуги"
            var servicesMenu = CreateServicesMenu();

            // Меню "Тарифы"
            var tariffsMenu = CreateTariffsMenu();

            // Меню "Адреса"
            var addressesMenu = CreateAddressesMenu();

            // Меню "Счета"
            var billsMenu = CreateBillsMenu();

            // Меню "Отчеты"
            var reportsMenu = new ToolStripMenuItem("Отчеты");
            var unpaidBillsItem = new ToolStripMenuItem("Неоплаченные счета");
            unpaidBillsItem.Click += async (s, e) => await ShowUnpaidBillsReportAsync();
            reportsMenu.DropDownItems.Add(unpaidBillsItem);

            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(tablesMenu);
            mainMenu.Items.Add(clientsMenu);
            mainMenu.Items.Add(servicesMenu);
            mainMenu.Items.Add(tariffsMenu);
            mainMenu.Items.Add(addressesMenu);
            mainMenu.Items.Add(billsMenu);
            mainMenu.Items.Add(reportsMenu);

            this.Controls.Add(mainMenu);
            this.MainMenuStrip = mainMenu;
        }

        private ToolStripMenuItem CreateClientsMenu()
        {
            var clientsMenu = new ToolStripMenuItem("Клиенты");

            var addClientItem = new ToolStripMenuItem("Добавить клиента");
            addClientItem.Click += async (s, e) => await ShowAddClientFormAsync();

            var deleteClientItem = new ToolStripMenuItem("Удалить клиента");
            deleteClientItem.Click += async (s, e) => await DeleteSelectedClientAsync();

            var refreshClientsItem = new ToolStripMenuItem("Обновить список");
            refreshClientsItem.Click += async (s, e) => await LoadClientsAsync();

            clientsMenu.DropDownItems.Add(addClientItem);
            clientsMenu.DropDownItems.Add(deleteClientItem);
            clientsMenu.DropDownItems.Add(new ToolStripSeparator());
            clientsMenu.DropDownItems.Add(refreshClientsItem);

            return clientsMenu;
        }

        private ToolStripMenuItem CreateServicesMenu()
        {
            var servicesMenu = new ToolStripMenuItem("Услуги");

            var addServiceItem = new ToolStripMenuItem("Добавить услугу");
            addServiceItem.Click += async (s, e) => await ShowAddServiceFormAsync();

            var deleteServiceItem = new ToolStripMenuItem("Удалить услугу");
            deleteServiceItem.Click += async (s, e) => await DeleteSelectedServiceAsync();

            var refreshServicesItem = new ToolStripMenuItem("Обновить список");
            refreshServicesItem.Click += async (s, e) => await LoadServicesAsync();

            servicesMenu.DropDownItems.Add(addServiceItem);
            servicesMenu.DropDownItems.Add(deleteServiceItem);
            servicesMenu.DropDownItems.Add(refreshServicesItem);

            return servicesMenu;
        }

        private ToolStripMenuItem CreateTariffsMenu()
        {
            var tariffsMenu = new ToolStripMenuItem("Тарифы");

            var addTariffItem = new ToolStripMenuItem("Добавить тариф");
            addTariffItem.Click += async (s, e) => await ShowAddTariffFormAsync();

            var deleteTariffItem = new ToolStripMenuItem("Удалить тариф");
            deleteTariffItem.Click += async (s, e) => await DeleteSelectedTariffAsync();

            var refreshTariffsItem = new ToolStripMenuItem("Обновить список");
            refreshTariffsItem.Click += async (s, e) => await LoadTariffsAsync();

            tariffsMenu.DropDownItems.Add(addTariffItem);
            tariffsMenu.DropDownItems.Add(deleteTariffItem);
            tariffsMenu.DropDownItems.Add(refreshTariffsItem);

            return tariffsMenu;
        }

        private ToolStripMenuItem CreateAddressesMenu()
        {
            var addressesMenu = new ToolStripMenuItem("Адреса");

            var addAddressItem = new ToolStripMenuItem("Добавить адрес");
            addAddressItem.Click += async (s, e) => await ShowAddAddressFormAsync(); // Эта строка уже есть

            var deleteAddressItem = new ToolStripMenuItem("Удалить адрес");
            deleteAddressItem.Click += async (s, e) => await DeleteSelectedAddressAsync();

            var refreshAddressesItem = new ToolStripMenuItem("Обновить список");
            refreshAddressesItem.Click += async (s, e) => await LoadAddressesAsync();

            addressesMenu.DropDownItems.Add(addAddressItem);
            addressesMenu.DropDownItems.Add(deleteAddressItem);
            addressesMenu.DropDownItems.Add(refreshAddressesItem);

            return addressesMenu;
        }

        private ToolStripMenuItem CreateBillsMenu()
        {
            var billsMenu = new ToolStripMenuItem("Счета");

            var addBillItem = new ToolStripMenuItem("Добавить счет");
            addBillItem.Click += async (s, e) => await ShowAddBillFormAsync();

            var deleteBillItem = new ToolStripMenuItem("Удалить счет");
            deleteBillItem.Click += async (s, e) => await DeleteSelectedBillAsync();

            var markAsPaidItem = new ToolStripMenuItem("Отметить как оплаченный");
            markAsPaidItem.Click += async (s, e) => await MarkBillAsPaidAsync();

            var refreshBillsItem = new ToolStripMenuItem("Обновить список");
            refreshBillsItem.Click += async (s, e) => await LoadBillsAsync();

            billsMenu.DropDownItems.Add(addBillItem);
            billsMenu.DropDownItems.Add(deleteBillItem);
            billsMenu.DropDownItems.Add(markAsPaidItem);
            billsMenu.DropDownItems.Add(new ToolStripSeparator());
            billsMenu.DropDownItems.Add(refreshBillsItem);

            return billsMenu;
        }

        private void CreateDataGrids()
        {
            // Таблица клиентов
            dataGridViewClients = new DataGridView
            {
                Name = "dataGridViewClients",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 24),
                Size = new Size(1200, 626),
                Visible = false
            };

            // Таблица услуг
            dataGridViewServices = new DataGridView
            {
                Name = "dataGridViewServices",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 24),
                Size = new Size(1200, 626),
                Visible = false
            };

            // Таблица тарифов
            dataGridViewTariffs = new DataGridView
            {
                Name = "dataGridViewTariffs",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 24),
                Size = new Size(1200, 626),
                Visible = false
            };

            // Таблица адресов
            dataGridViewAddresses = new DataGridView
            {
                Name = "dataGridViewAddresses",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 24),
                Size = new Size(1200, 626),
                Visible = false
            };

            // Таблица счетов
            dataGridViewBills = new DataGridView
            {
                Name = "dataGridViewBills",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(0, 24),
                Size = new Size(1200, 626),
                Visible = false
            };

            // Добавляем обработчики двойного клика
            dataGridViewClients.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewClients != null)
                {
                    var client = dataGridViewClients.Rows[e.RowIndex].DataBoundItem as Client;
                    if (client != null)
                    {
                        ShowClientDetails(client);
                    }
                }
            };

            dataGridViewServices.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewServices != null)
                {
                    var service = dataGridViewServices.Rows[e.RowIndex].DataBoundItem as Service;
                    if (service != null)
                    {
                        ShowServiceDetails(service);
                    }
                }
            };

            dataGridViewTariffs.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewTariffs != null)
                {
                    var tariff = dataGridViewTariffs.Rows[e.RowIndex].DataBoundItem as Tariff;
                    if (tariff != null)
                    {
                        ShowTariffDetails(tariff);
                    }
                }
            };

            dataGridViewAddresses.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewAddresses != null)
                {
                    var address = dataGridViewAddresses.Rows[e.RowIndex].DataBoundItem as Address;
                    if (address != null)
                    {
                        ShowAddressDetails(address);
                    }
                }
            };

            dataGridViewBills.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewBills != null)
                {
                    var bill = dataGridViewBills.Rows[e.RowIndex].DataBoundItem as Bill;
                    if (bill != null)
                    {
                        ShowBillDetails(bill);
                    }
                }
            };

            this.Controls.Add(dataGridViewClients);
            this.Controls.Add(dataGridViewServices);
            this.Controls.Add(dataGridViewTariffs);
            this.Controls.Add(dataGridViewAddresses);
            this.Controls.Add(dataGridViewBills);
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

        private void ShowTable(string tableName)
        {
            SafeInvoke(() =>
            {
                if (dataGridViewClients == null || dataGridViewServices == null ||
                    dataGridViewTariffs == null || dataGridViewAddresses == null || dataGridViewBills == null)
                    return;

                // Скрываем все таблицы
                dataGridViewClients.Visible = false;
                dataGridViewServices.Visible = false;
                dataGridViewTariffs.Visible = false;
                dataGridViewAddresses.Visible = false;
                dataGridViewBills.Visible = false;

                // Обновляем меню в зависимости от активной таблицы
                UpdateMenuVisibility(tableName);

                // Показываем выбранную таблицу и загружаем данные
                switch (tableName)
                {
                    case "Clients":
                        dataGridViewClients.Visible = true;
                        currentTable = "Clients";
                        _ = LoadClientsAsync();
                        UpdateStatus("Таблица: Клиенты");
                        break;
                    case "Services":
                        dataGridViewServices.Visible = true;
                        currentTable = "Services";
                        _ = LoadServicesAsync();
                        UpdateStatus("Таблица: Услуги");
                        break;
                    case "Tariffs":
                        dataGridViewTariffs.Visible = true;
                        currentTable = "Tariffs";
                        _ = LoadTariffsAsync();
                        UpdateStatus("Таблица: Тарифы");
                        break;
                    case "Addresses":
                        dataGridViewAddresses.Visible = true;
                        currentTable = "Addresses";
                        _ = LoadAddressesAsync();
                        UpdateStatus("Таблица: Адреса");
                        break;
                    case "Bills":
                        dataGridViewBills.Visible = true;
                        currentTable = "Bills";
                        _ = LoadBillsAsync();
                        UpdateStatus("Таблица: Счета");
                        break;
                }
            });
        }

        private void UpdateMenuVisibility(string activeTable)
        {
            if (mainMenu == null) return;

            // Скрываем/показываем меню в зависимости от активной таблицы
            foreach (ToolStripItem item in mainMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text == "Клиенты")
                    {
                        menuItem.Visible = (activeTable == "Clients");
                    }
                    else if (menuItem.Text == "Услуги")
                    {
                        menuItem.Visible = (activeTable == "Services");
                    }
                    else if (menuItem.Text == "Тарифы")
                    {
                        menuItem.Visible = (activeTable == "Tariffs");
                    }
                    else if (menuItem.Text == "Адреса")
                    {
                        menuItem.Visible = (activeTable == "Addresses");
                    }
                    else if (menuItem.Text == "Счета")
                    {
                        menuItem.Visible = (activeTable == "Bills");
                    }
                }
            }
        }

        private void UpdateStatus(string message)
        {
            SafeInvoke(() =>
            {
                if (statusLabel != null)
                {
                    statusLabel.Text = message;
                    statusStrip?.Refresh();
                }
            });
        }

        #region Методы для работы со счетами
        private async Task LoadBillsAsync()
        {
            try
            {
                UpdateStatus("Загрузка счетов...");
                var bills = await _billRepository.GetBillsAsync();

                SafeInvoke(() =>
                {
                    dataGridViewBills.DataSource = bills;
                    ConfigureBillsGridColumns();
                    UpdateStatus($"Загружено счетов: {bills.Count}");
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки счетов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка загрузки данных");
                });
            }
        }

        private void ConfigureBillsGridColumns()
        {
            dataGridViewBills.AutoGenerateColumns = false;
            dataGridViewBills.Columns.Clear();

            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BillID",
                DataPropertyName = "BillID",
                HeaderText = "ID",
                Width = 50
            });

            // Заменяем ClientID на информацию об адресе
            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AddressInfo",
                HeaderText = "Адрес",
                Width = 200
            });

            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServiceName",
                HeaderText = "Услуга",
                Width = 120
            });

            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                DataPropertyName = "Amount",
                HeaderText = "Сумма",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IssueDate",
                DataPropertyName = "IssueDate",
                HeaderText = "Дата выдачи",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" }
            });

            dataGridViewBills.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PaymentDate",
                DataPropertyName = "PaymentDate",
                HeaderText = "Дата оплаты",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" }
            });

            dataGridViewBills.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "IsPaid",
                DataPropertyName = "IsPaid",
                HeaderText = "Оплачен",
                Width = 70
            });

            // Настраиваем отображение адреса и услуги
            dataGridViewBills.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var bill = dataGridViewBills.Rows[e.RowIndex].DataBoundItem as Bill;
                    if (bill != null)
                    {
                        if (e.ColumnIndex == dataGridViewBills.Columns["AddressInfo"].Index)
                        {
                            var address = bill.Address;
                            if (address != null)
                            {
                                e.Value = $"{address.Street}, {address.House}{(string.IsNullOrEmpty(address.Apartment) ? "" : $", кв. {address.Apartment}")}";
                                e.FormattingApplied = true;
                            }
                        }
                        else if (e.ColumnIndex == dataGridViewBills.Columns["ServiceName"].Index)
                        {
                            e.Value = bill.Service?.ServiceName ?? "Не указана";
                            e.FormattingApplied = true;
                        }
                    }
                }
            };
        }

        private async Task ShowAddBillFormAsync()
        {
            try
            {
                await Task.Run(() => ShowAddBillForm());
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void ShowAddBillForm()
        {
            try
            {
                var addForm = new Form
                {
                    Text = "Добавить новый счет",
                    Size = new Size(500, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Padding = new Padding(20)
                };

                // ВЫПАДАЮЩИЙ СПИСОК ДЛЯ ВЫБОРА АДРЕСА
                var lblAddress = new Label
                {
                    Text = "Адрес:*",
                    Location = new Point(20, 20),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var cmbAddress = new ComboBox
                {
                    Location = new Point(150, 18),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем комбобокс адресами с информацией о клиентах
                try
                {
                    var addresses = _addressRepository.GetAddresses();
                    if (addresses.Count == 0)
                    {
                        MessageBox.Show("Нет доступных адресов. Сначала добавьте адрес.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    cmbAddress.DisplayMember = "DisplayAddressWithClient";
                    cmbAddress.ValueMember = "AddressID";
                    cmbAddress.DataSource = addresses.Select(a => new
                    {
                        a.AddressID,
                        DisplayAddressWithClient = $"{a.Street}, {a.House}{(string.IsNullOrEmpty(a.Apartment) ? "" : $", кв. {a.Apartment}")} - {a.Client?.LastName} {a.Client?.FirstName}"
                    }).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки адресов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Выпадающий список для выбора услуги
                var lblService = new Label
                {
                    Text = "Услуга:*",
                    Location = new Point(20, 60),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var cmbService = new ComboBox
                {
                    Location = new Point(150, 58),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем комбобокс услугами
                try
                {
                    var services = _serviceRepository.GetServices();
                    if (services.Count == 0)
                    {
                        MessageBox.Show("Нет доступных услуг. Сначала добавьте услугу.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    cmbService.DisplayMember = "ServiceName";
                    cmbService.ValueMember = "ServiceID";
                    cmbService.DataSource = services;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Поле для суммы
                var lblAmount = new Label
                {
                    Text = "Сумма:*",
                    Location = new Point(20, 100),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtAmount = new NumericUpDown
                {
                    Location = new Point(150, 98),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    DecimalPlaces = 2,
                    Minimum = 0,
                    Maximum = 1000000,
                    Increment = 0.01m
                };

                // Поле для даты выдачи
                var lblIssueDate = new Label
                {
                    Text = "Дата выдачи:*",
                    Location = new Point(20, 140),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var dtpIssueDate = new DateTimePicker
                {
                    Location = new Point(150, 138),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    Format = DateTimePickerFormat.Short,
                    Value = DateTime.Now
                };

                // Поле для даты оплаты
                var lblPaymentDate = new Label
                {
                    Text = "Дата оплаты:",
                    Location = new Point(20, 180),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var dtpPaymentDate = new DateTimePicker
                {
                    Location = new Point(150, 178),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    Format = DateTimePickerFormat.Short,
                    Enabled = false,
                    Value = DateTime.Now
                };

                // Чекбокс для статуса оплаты
                var chkIsPaid = new CheckBox
                {
                    Text = "Счет оплачен",
                    Location = new Point(150, 218),
                    Width = 150,
                    Font = new Font("Arial", 9),
                    Checked = false
                };

                // Обработчик изменения чекбокса
                chkIsPaid.CheckedChanged += (s, e) =>
                {
                    dtpPaymentDate.Enabled = chkIsPaid.Checked;
                    if (!chkIsPaid.Checked)
                    {
                        dtpPaymentDate.Value = DateTime.Now;
                    }
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 250),
                    Width = 440,
                    Height = 30,
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Visible = false
                };

                // Кнопки
                var btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(150, 290),
                    Size = new Size(100, 35),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(260, 290),
                    Size = new Size(100, 35),
                    BackColor = Color.LightCoral,
                    Font = new Font("Arial", 9)
                };

                void ShowError(string message)
                {
                    lblError.Text = message;
                    lblError.Visible = true;
                }

                void HideError()
                {
                    lblError.Text = "";
                    lblError.Visible = false;
                }

                bool ValidateForm()
                {
                    HideError();

                    // Проверяем адрес вместо клиента
                    if (cmbAddress.SelectedItem == null)
                    {
                        ShowError("Выберите адрес");
                        cmbAddress.Focus();
                        return false;
                    }

                    if (cmbService.SelectedItem == null)
                    {
                        ShowError("Выберите услугу");
                        cmbService.Focus();
                        return false;
                    }

                    if (txtAmount.Value <= 0)
                    {
                        ShowError("Сумма должна быть больше 0");
                        txtAmount.Focus();
                        return false;
                    }

                    if (dtpIssueDate.Value > DateTime.Now)
                    {
                        ShowError("Дата выдачи не может быть в будущем");
                        dtpIssueDate.Focus();
                        return false;
                    }

                    if (chkIsPaid.Checked && dtpPaymentDate.Value < dtpIssueDate.Value)
                    {
                        ShowError("Дата оплаты не может быть раньше даты выдачи");
                        dtpPaymentDate.Focus();
                        return false;
                    }

                    return true;
                }

                btnSave.Click += async (s, e) =>
                {
                    if (!ValidateForm())
                    {
                        return;
                    }

                    try
                    {
                        btnSave.Enabled = false;
                        btnSave.Text = "Сохранение...";
                        btnCancel.Enabled = false;

                        var selectedAddress = (dynamic)cmbAddress.SelectedItem;
                        var selectedService = (Service)cmbService.SelectedItem;

                        var bill = new Bill
                        {
                            AddressID = selectedAddress.AddressID,
                            ServiceID = selectedService.ServiceID,
                            Amount = txtAmount.Value,
                            IssueDate = dtpIssueDate.Value,
                            PaymentDate = chkIsPaid.Checked ? dtpPaymentDate.Value : (DateTime?)null,
                            IsPaid = chkIsPaid.Checked
                        };

                        var newBillId = await _billRepository.AddBillAsync(bill);

                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        await LoadBillsAsync();

                        MessageBox.Show($"Счет успешно добавлен! ID: {newBillId}", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        btnSave.Enabled = true;
                        btnSave.Text = "Сохранить";
                        btnCancel.Enabled = true;
                        ShowError($"Ошибка сохранения: {ex.Message}");
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                void HideErrorOnChange(object sender, EventArgs e)
                {
                    HideError();
                }

                cmbAddress.SelectedIndexChanged += HideErrorOnChange;
                cmbService.SelectedIndexChanged += HideErrorOnChange;
                txtAmount.ValueChanged += HideErrorOnChange;
                dtpIssueDate.ValueChanged += HideErrorOnChange;
                dtpPaymentDate.ValueChanged += HideErrorOnChange;
                chkIsPaid.CheckedChanged += HideErrorOnChange;

                addForm.AcceptButton = btnSave;
                addForm.CancelButton = btnCancel;

                addForm.Controls.AddRange(new Control[]
                {
            lblAddress, cmbAddress,
            lblService, cmbService,
            lblAmount, txtAmount,
            lblIssueDate, dtpIssueDate,
            lblPaymentDate, dtpPaymentDate,
            chkIsPaid,
            lblError,
            btnSave, btnCancel
                });

                cmbAddress.Focus();

                if (cmbAddress.Items.Count > 0)
                    cmbAddress.SelectedIndex = 0;

                if (cmbService.Items.Count > 0)
                    cmbService.SelectedIndex = 0;

                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedBillAsync()
        {
            if (dataGridViewBills?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите счет для удаления!", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewBills.SelectedRows[0];
            var bill = selectedRow.DataBoundItem as Bill;

            if (bill == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных счета", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Вы уверены, что хотите удалить счет №{bill.BillID}?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление счета...");
                    var success = await _billRepository.DeleteBillAsync(bill.BillID);

                    if (success)
                    {
                        await LoadBillsAsync();
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Счет успешно удален!", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                        UpdateStatus("Счет удален");
                    }
                    else
                    {
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Не удалось удалить счет", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                        UpdateStatus("Ошибка удаления счета");
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении счета: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка удаления счета");
                }
            }
        }
        private async Task MarkBillAsPaidAsync()
        {
            if (dataGridViewBills?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите счет для отметки об оплате!", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewBills.SelectedRows[0];
            var bill = selectedRow.DataBoundItem as Bill;

            if (bill == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных счета", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            if (bill.IsPaid)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Этот счет уже оплачен!", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Отметить счет №{bill.BillID} как оплаченный?",
                    "Подтверждение оплаты", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Отметка счета как оплаченного...");
                    var success = await _billRepository.MarkBillAsPaidAsync(bill.BillID);

                    if (success)
                    {
                        await LoadBillsAsync();
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Счет отмечен как оплаченный!", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                        UpdateStatus("Счет оплачен");
                    }
                    else
                    {
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Не удалось отметить счет как оплаченный", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                        UpdateStatus("Ошибка отметки счета");
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при отметке счета: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка отметки счета");
                }
            }
        }

        private void ShowBillDetails(Bill bill)
        {
            try
            {
                var addressInfo = bill.Address != null ?
                    $"{bill.Address.Street}, {bill.Address.House}{(string.IsNullOrEmpty(bill.Address.Apartment) ? "" : $", кв. {bill.Address.Apartment}")}" : "Не указан";

                var clientInfo = bill.Address?.Client != null ?
                    $"{bill.Address.Client.LastName} {bill.Address.Client.FirstName}" : "Не указан";

                var serviceName = bill.Service?.ServiceName ?? "Не указана";
                var paymentDate = bill.PaymentDate?.ToString("dd.MM.yyyy") ?? "Не оплачен";
                var status = bill.IsPaid ? "Оплачен" : "Не оплачен";

                var details = $"Счет №{bill.BillID}\n" +
                             $"Адрес: {addressInfo}\n" +
                             $"Клиент: {clientInfo}\n" +
                             $"Услуга: {serviceName}\n" +
                             $"Сумма: {bill.Amount:C2}\n" +
                             $"Дата выдачи: {bill.IssueDate:dd.MM.yyyy}\n" +
                             $"Дата оплаты: {paymentDate}\n" +
                             $"Статус: {status}";

                SafeInvoke(() =>
                {
                    MessageBox.Show(details, "Информация о счете",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
                });
            }
        }
        #endregion

        #region Методы для работы с клиентами
        private async Task LoadClientsAsync()
        {
            try
            {
                UpdateStatus("Загрузка клиентов...");
                var clients = await _clientRepository.GetClientsAsync();

                SafeInvoke(() =>
                {
                    dataGridViewClients.DataSource = clients;
                    ConfigureClientGridColumns();
                    UpdateStatus($"Загружено клиентов: {clients.Count}");
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка загрузки данных");
                });
            }
        }

        private void ConfigureClientGridColumns()
        {
            dataGridViewClients.AutoGenerateColumns = false;
            dataGridViewClients.Columns.Clear();

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ClientID",
                DataPropertyName = "ClientID",
                HeaderText = "ID",
                Width = 50
            });

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LastName",
                DataPropertyName = "LastName",
                HeaderText = "Фамилия",
                Width = 120
            });

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FirstName",
                DataPropertyName = "FirstName",
                HeaderText = "Имя",
                Width = 120
            });

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Phone",
                DataPropertyName = "Phone",
                HeaderText = "Телефон",
                Width = 120
            });

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Email",
                DataPropertyName = "Email",
                HeaderText = "Email",
                Width = 150
            });
        }

        private async Task ShowAddClientFormAsync()
        {
            try
            {
                await Task.Run(() => ShowAddClientForm());
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void ShowAddClientForm()
        {
            try
            {
                var addForm = new Form
                {
                    Text = "Добавить нового клиента",
                    Size = new Size(400, 320),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Padding = new Padding(20)
                };

                // Поле для имени
                var lblFirstName = new Label
                {
                    Text = "Имя:",
                    Location = new Point(20, 20),
                    Width = 80,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtFirstName = new TextBox
                {
                    Location = new Point(120, 18),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Имя"
                };

                // Поле для фамилии
                var lblLastName = new Label
                {
                    Text = "Фамилия:",
                    Location = new Point(20, 60),
                    Width = 80,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtLastName = new TextBox
                {
                    Location = new Point(120, 58),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Фамилия"
                };

                // Поле для телефона
                var lblPhone = new Label
                {
                    Text = "Телефон:",
                    Location = new Point(20, 100),
                    Width = 80,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtPhone = new TextBox
                {
                    Location = new Point(120, 98),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Телефон"
                };

                // Поле для email
                var lblEmail = new Label
                {
                    Text = "Email:",
                    Location = new Point(20, 140),
                    Width = 80,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtEmail = new TextBox
                {
                    Location = new Point(120, 138),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Email"
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 175),
                    Width = 340,
                    Height = 20,
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Visible = false
                };

                // Кнопки
                var btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(120, 200),
                    Size = new Size(80, 30),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(210, 200),
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

                // Функция валидации данных
                bool ValidateForm()
                {
                    HideError();

                    if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                    {
                        ShowError("Поле 'Имя' обязательно для заполнения");
                        txtFirstName.Focus();
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(txtLastName.Text))
                    {
                        ShowError("Поле 'Фамилия' обязательно для заполнения");
                        txtLastName.Focus();
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
                    {
                        ShowError("Введите корректный email адрес");
                        txtEmail.Focus();
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(txtPhone.Text) && !IsValidPhone(txtPhone.Text))
                    {
                        ShowError("Введите корректный номер телефона");
                        txtPhone.Focus();
                        return false;
                    }

                    return true;
                }

                btnSave.Click += async (s, e) =>
                {
                    if (!ValidateForm())
                    {
                        return;
                    }

                    try
                    {
                        btnSave.Enabled = false;
                        btnSave.Text = "Сохранение...";
                        btnCancel.Enabled = false;

                        var client = new Client
                        {
                            FirstName = txtFirstName.Text.Trim(),
                            LastName = txtLastName.Text.Trim(),
                            Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                            Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim()
                        };

                        await _clientRepository.AddClientAsync(client);

                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        await LoadClientsAsync();

                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Клиент успешно добавлен!", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        btnSave.Enabled = true;
                        btnSave.Text = "Сохранить";
                        btnCancel.Enabled = true;
                        ShowError($"Ошибка сохранения: {ex.Message}");
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                void HideErrorOnTextChange(object sender, EventArgs e)
                {
                    HideError();
                }

                txtFirstName.TextChanged += HideErrorOnTextChange;
                txtLastName.TextChanged += HideErrorOnTextChange;
                txtEmail.TextChanged += HideErrorOnTextChange;
                txtPhone.TextChanged += HideErrorOnTextChange;

                addForm.AcceptButton = btnSave;
                addForm.CancelButton = btnCancel;

                addForm.Controls.AddRange(new Control[]
                {
                    lblFirstName, txtFirstName,
                    lblLastName, txtLastName,
                    lblPhone, txtPhone,
                    lblEmail, txtEmail,
                    lblError,
                    btnSave, btnCancel
                });

                txtFirstName.Focus();
                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private async Task DeleteSelectedClientAsync()
        {
            if (dataGridViewClients?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите клиента для удаления!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewClients.SelectedRows[0];
            var client = selectedRow.DataBoundItem as Client;

            if (client == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных клиента", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Вы уверены, что хотите удалить клиента {client.LastName} {client.FirstName}?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление клиента...");
                    await _clientRepository.DeleteClientAsync(client.ClientID);
                    await LoadClientsAsync();
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Клиент успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                    UpdateStatus("Клиент удален");
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении клиента: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка удаления клиента");
                }
            }
        }
        #endregion

        #region Методы для работы с услугами
        private async Task LoadServicesAsync()
        {
            try
            {
                UpdateStatus("Загрузка услуг...");
                var services = await _serviceRepository.GetServicesAsync();

                SafeInvoke(() =>
                {
                    dataGridViewServices.DataSource = services;
                    ConfigureServicesGridColumns();
                    UpdateStatus($"Загружено услуг: {services.Count}");
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка загрузки данных");
                });
            }
        }

        private void ConfigureServicesGridColumns()
        {
            dataGridViewServices.AutoGenerateColumns = false;
            dataGridViewServices.Columns.Clear();

            dataGridViewServices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServiceID",
                DataPropertyName = "ServiceID",
                HeaderText = "УслугаID",
                Width = 80
            });

            dataGridViewServices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServiceName",
                DataPropertyName = "ServiceName",
                HeaderText = "Название услуги",
                Width = 200
            });

            dataGridViewServices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServiceType",
                DataPropertyName = "ServiceType",
                HeaderText = "Тип услуги",
                Width = 150
            });
        }

        private async Task ShowAddServiceFormAsync()
        {
            try
            {
                await Task.Run(() => ShowAddServiceForm());
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void ShowAddServiceForm()
        {
            try
            {
                var addForm = new Form
                {
                    Text = "Добавить новую услугу",
                    Size = new Size(450, 220),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Padding = new Padding(20)
                };

                // Поле для названия услуги
                var lblServiceName = new Label
                {
                    Text = "Название услуги:",
                    Location = new Point(20, 20),
                    Width = 150,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtServiceName = new TextBox
                {
                    Location = new Point(180, 18),
                    Width = 220,
                    Font = new Font("Arial", 9)
                };

                // Поле для типа услуги
                var lblServiceType = new Label
                {
                    Text = "Тип услуги:",
                    Location = new Point(20, 60),
                    Width = 150,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtServiceType = new TextBox
                {
                    Location = new Point(180, 58),
                    Width = 220,
                    Font = new Font("Arial", 9)
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 100),
                    Width = 400,
                    Height = 30,
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Visible = false
                };

                // Кнопки
                var btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(150, 140),
                    Size = new Size(100, 35),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(260, 140),
                    Size = new Size(100, 35),
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

                // Функция валидации данных
                bool ValidateForm()
                {
                    HideError();

                    if (string.IsNullOrWhiteSpace(txtServiceName.Text))
                    {
                        ShowError("Поле 'Название услуги' обязательно для заполнения");
                        txtServiceName.Focus();
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(txtServiceType.Text))
                    {
                        ShowError("Поле 'Тип услуги' обязательно для заполнения");
                        txtServiceType.Focus();
                        return false;
                    }

                    return true;
                }

                btnSave.Click += async (s, e) =>
                {
                    if (!ValidateForm())
                    {
                        return;
                    }

                    try
                    {
                        btnSave.Enabled = false;
                        btnSave.Text = "Сохранение...";
                        btnCancel.Enabled = false;

                        var service = new Service
                        {
                            ServiceName = txtServiceName.Text.Trim(),
                            ServiceType = txtServiceType.Text.Trim()
                        };

                        await _serviceRepository.AddServiceAsync(service);

                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        if (currentTable == "Services")
                        {
                            await LoadServicesAsync();
                        }
                        else
                        {
                            ShowTable("Services");
                        }

                        MessageBox.Show("Услуга успешно добавлена!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        btnSave.Enabled = true;
                        btnSave.Text = "Сохранить";
                        btnCancel.Enabled = true;
                        ShowError($"Ошибка сохранения: {ex.Message}");
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                void HideErrorOnTextChange(object sender, EventArgs e)
                {
                    HideError();
                }

                txtServiceName.TextChanged += HideErrorOnTextChange;
                txtServiceType.TextChanged += HideErrorOnTextChange;

                addForm.AcceptButton = btnSave;
                addForm.CancelButton = btnCancel;

                addForm.Controls.AddRange(new Control[]
                {
                    lblServiceName, txtServiceName,
                    lblServiceType, txtServiceType,
                    lblError,
                    btnSave, btnCancel
                });

                txtServiceName.Focus();
                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedServiceAsync()
        {
            if (dataGridViewServices?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите услугу для удаления!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewServices.SelectedRows[0];
            var service = selectedRow.DataBoundItem as Service;

            if (service == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных услуги", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Вы уверены, что хотите удалить услугу \"{service.ServiceName}\"?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление услуги...");
                    await _serviceRepository.DeleteServiceAsync(service.ServiceID);
                    await LoadServicesAsync();
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Услуга успешно удалена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                    UpdateStatus("Услуга удалена");
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка удаления услуги");
                }
            }
        }
        #endregion

        #region Методы для работы с тарифами
        private async Task LoadTariffsAsync()
        {
            try
            {
                UpdateStatus("Загрузка тарифов...");
                var tariffs = await _tariffRepository.GetTariffsAsync();

                SafeInvoke(() =>
                {
                    dataGridViewTariffs.DataSource = tariffs;
                    ConfigureTariffsGridColumns();
                    UpdateStatus($"Загружено тарифов: {tariffs.Count}");
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки тарифов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка загрузки данных");
                });
            }
        }

        private void ConfigureTariffsGridColumns()
        {
            dataGridViewTariffs.AutoGenerateColumns = false;
            dataGridViewTariffs.Columns.Clear();

            dataGridViewTariffs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TariffID",
                DataPropertyName = "TariffID",
                HeaderText = "ТарифID",
                Width = 70
            });

            dataGridViewTariffs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ServiceName",
                DataPropertyName = "ServiceName",
                HeaderText = "Услуга",
                Width = 180
            });

            dataGridViewTariffs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PricePerSquareMeter",
                DataPropertyName = "PricePerSquareMeter",
                HeaderText = "Цена за кв.м",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            dataGridViewTariffs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PricePerPerson",
                DataPropertyName = "PricePerPerson",
                HeaderText = "Цена за человека",
                Width = 140,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            dataGridViewTariffs.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PricePerUnit",
                DataPropertyName = "PricePerUnit",
                HeaderText = "Цена за объем",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
        }

        private async Task ShowAddTariffFormAsync()
        {
            try
            {
                await Task.Run(() => ShowAddTariffForm());
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void ShowAddTariffForm()
        {
            try
            {
                var services = _serviceRepository.GetServices();

                var addForm = new Form
                {
                    Text = "Добавить новый тариф",
                    Size = new Size(450, 300),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Padding = new Padding(20)
                };

                // Выпадающий список для выбора услуги
                var lblService = new Label
                {
                    Text = "Услуга:",
                    Location = new Point(20, 20),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var cmbService = new ComboBox
                {
                    Location = new Point(150, 18),
                    Width = 250,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                cmbService.DisplayMember = "ServiceName";
                cmbService.ValueMember = "ServiceID";
                cmbService.DataSource = services;

                // Выпадающий список для выбора типа цены
                var lblPriceType = new Label
                {
                    Text = "Тип цены:",
                    Location = new Point(20, 60),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var cmbPriceType = new ComboBox
                {
                    Location = new Point(150, 58),
                    Width = 250,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                cmbPriceType.Items.AddRange(new string[] {
                    "Цена за квадратный метр",
                    "Цена за человека",
                    "Цена за потребляемый объем"
                });

                // Поле для ввода цены
                var lblPrice = new Label
                {
                    Text = "Цена:",
                    Location = new Point(20, 100),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtPrice = new NumericUpDown
                {
                    Location = new Point(150, 98),
                    Width = 250,
                    Font = new Font("Arial", 9),
                    DecimalPlaces = 2,
                    Minimum = 0,
                    Maximum = 1000000,
                    Increment = 0.01m
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 140),
                    Width = 400,
                    Height = 30,
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Visible = false
                };

                // Кнопки
                var btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(150, 180),
                    Size = new Size(100, 35),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(260, 180),
                    Size = new Size(100, 35),
                    BackColor = Color.LightCoral,
                    Font = new Font("Arial", 9)
                };

                void ShowError(string message)
                {
                    lblError.Text = message;
                    lblError.Visible = true;
                }

                void HideError()
                {
                    lblError.Text = "";
                    lblError.Visible = false;
                }

                bool ValidateForm()
                {
                    HideError();

                    if (cmbService.SelectedItem == null)
                    {
                        ShowError("Выберите услугу");
                        cmbService.Focus();
                        return false;
                    }

                    if (cmbPriceType.SelectedItem == null)
                    {
                        ShowError("Выберите тип цены");
                        cmbPriceType.Focus();
                        return false;
                    }

                    if (txtPrice.Value <= 0)
                    {
                        ShowError("Цена должна быть больше 0");
                        txtPrice.Focus();
                        return false;
                    }

                    return true;
                }

                btnSave.Click += async (s, e) =>
                {
                    if (!ValidateForm())
                    {
                        return;
                    }

                    try
                    {
                        btnSave.Enabled = false;
                        btnSave.Text = "Сохранение...";
                        btnCancel.Enabled = false;

                        var selectedService = (Service)cmbService.SelectedItem;
                        var priceType = cmbPriceType.SelectedItem.ToString();
                        var price = txtPrice.Value;

                        var tariff = new Tariff
                        {
                            ServiceID = selectedService.ServiceID,
                            ServiceName = selectedService.ServiceName,
                            PricePerSquareMeter = priceType == "Цена за квадратный метр" ? price : 0m,
                            PricePerPerson = priceType == "Цена за человека" ? price : 0m,
                            PricePerUnit = priceType == "Цена за потребляемый объем" ? price : 0m
                        };

                        await _tariffRepository.AddTariffAsync(tariff);

                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        if (currentTable == "Tariffs")
                        {
                            await LoadTariffsAsync();
                        }
                        else
                        {
                            ShowTable("Tariffs");
                        }

                        MessageBox.Show("Тариф успешно добавлен!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        btnSave.Enabled = true;
                        btnSave.Text = "Сохранить";
                        btnCancel.Enabled = true;

                        string errorMessage = ex.Message;
                        if (ex.InnerException != null)
                        {
                            errorMessage += $"\nДетали: {ex.InnerException.Message}";
                        }
                        ShowError($"Ошибка сохранения: {errorMessage}");
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                void HideErrorOnChange(object sender, EventArgs e)
                {
                    HideError();
                }

                cmbService.SelectedIndexChanged += HideErrorOnChange;
                cmbPriceType.SelectedIndexChanged += HideErrorOnChange;
                txtPrice.ValueChanged += HideErrorOnChange;

                addForm.AcceptButton = btnSave;
                addForm.CancelButton = btnCancel;

                addForm.Controls.AddRange(new Control[]
                {
                    lblService, cmbService,
                    lblPriceType, cmbPriceType,
                    lblPrice, txtPrice,
                    lblError,
                    btnSave, btnCancel
                });

                cmbService.Focus();

                if (cmbService.Items.Count > 0)
                    cmbService.SelectedIndex = 0;

                if (cmbPriceType.Items.Count > 0)
                    cmbPriceType.SelectedIndex = 0;

                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedTariffAsync()
        {
            if (dataGridViewTariffs?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите тариф для удаления!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewTariffs.SelectedRows[0];
            var tariff = selectedRow.DataBoundItem as Tariff;

            if (tariff == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных тарифа", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Вы уверены, что хотите удалить тариф для услуги \"{tariff.ServiceName}\"?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление тарифа...");
                    await _tariffRepository.DeleteTariffAsync(tariff.TariffID);
                    await LoadTariffsAsync();
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Тариф успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                    UpdateStatus("Тариф удален");
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении тарифа: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка удаления тарифа");
                }
            }
        }
        #endregion

        #region Методы для работы с адресами
        private async Task LoadAddressesAsync()
        {
            try
            {
                UpdateStatus("Загрузка адресов...");
                var addresses = await _addressRepository.GetAddressesAsync();

                SafeInvoke(() =>
                {
                    dataGridViewAddresses.DataSource = addresses;
                    ConfigureAddressGridColumns();
                    UpdateStatus($"Загружено адресов: {addresses.Count}");
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки адресов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка загрузки данных");
                });
            }
        }

        private void ConfigureAddressGridColumns()
        {
            dataGridViewAddresses.AutoGenerateColumns = false;
            dataGridViewAddresses.Columns.Clear();

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AddressID",
                DataPropertyName = "AddressID",
                HeaderText = "ID",
                Width = 50
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ClientName",
                HeaderText = "Клиент",
                Width = 150
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Street",
                DataPropertyName = "Street",
                HeaderText = "Улица",
                Width = 150
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "House",
                DataPropertyName = "House",
                HeaderText = "Дом",
                Width = 80
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Apartment",
                DataPropertyName = "Apartment",
                HeaderText = "Квартира",
                Width = 80
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "LivingArea",
                DataPropertyName = "LivingArea",
                HeaderText = "Площадь (м²)",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "F2" }
            });

            dataGridViewAddresses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ResidentsCount",
                DataPropertyName = "ResidentsCount",
                HeaderText = "Жильцов",
                Width = 80
            });

            // Настраиваем отображение имени клиента
            dataGridViewAddresses.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == dataGridViewAddresses.Columns["ClientName"].Index && e.RowIndex >= 0)
                {
                    var address = dataGridViewAddresses.Rows[e.RowIndex].DataBoundItem as Address;
                    if (address?.Client != null)
                    {
                        e.Value = $"{address.Client.LastName} {address.Client.FirstName}";
                    }
                    else
                    {
                        // Если клиент не загружен, пытаемся найти его по ClientID
                        var client = _clientRepository.GetClients().FirstOrDefault(c => c.ClientID == address.ClientID);
                        if (client != null)
                        {
                            e.Value = $"{client.LastName} {client.FirstName}";
                            address.Client = client; // Сохраняем клиента в объект адреса
                        }
                        else
                        {
                            e.Value = "Не указан";
                        }
                    }
                    e.FormattingApplied = true;
                }
            };
        }

        private async Task ShowAddAddressFormAsync()
        {
            try
            {
                await Task.Run(() => ShowAddAddressForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowAddAddressForm()
        {
            try
            {
                var addForm = new Form
                {
                    Text = "Добавить новый адрес",
                    Size = new Size(500, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Padding = new Padding(20)
                };

                // 1. ВЫБОР КЛИЕНТА
                var lblClient = new Label
                {
                    Text = "Клиент:*",
                    Location = new Point(20, 20),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var cmbClient = new ComboBox
                {
                    Location = new Point(150, 18),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем комбобокс клиентами
                try
                {
                    var clients = _clientRepository.GetClients();
                    if (clients.Count == 0)
                    {
                        MessageBox.Show("Нет доступных клиентов. Сначала добавьте клиента.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    cmbClient.DisplayMember = "DisplayName";
                    cmbClient.ValueMember = "ClientID";
                    cmbClient.DataSource = clients.Select(c => new
                    {
                        c.ClientID,
                        DisplayName = $"{c.LastName} {c.FirstName}",
                        ClientObject = c // Сохраняем объект клиента
                    }).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. АДРЕСНЫЕ ДАННЫЕ
                var lblStreet = new Label
                {
                    Text = "Улица:*",
                    Location = new Point(20, 60),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtStreet = new TextBox
                {
                    Location = new Point(150, 58),
                    Width = 300,
                    Font = new Font("Arial", 9)
                };

                var lblHouse = new Label
                {
                    Text = "Дом:*",
                    Location = new Point(20, 100),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtHouse = new TextBox
                {
                    Location = new Point(150, 98),
                    Width = 300,
                    Font = new Font("Arial", 9)
                };

                var lblApartment = new Label
                {
                    Text = "Квартира:",
                    Location = new Point(20, 140),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtApartment = new TextBox
                {
                    Location = new Point(150, 138),
                    Width = 300,
                    Font = new Font("Arial", 9)
                };

                // 3. ТЕХНИЧЕСКИЕ ДАННЫЕ
                var lblLivingArea = new Label
                {
                    Text = "Площадь (м²):*",
                    Location = new Point(20, 180),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtLivingArea = new NumericUpDown
                {
                    Location = new Point(150, 178),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    DecimalPlaces = 2,
                    Minimum = 10,
                    Maximum = 1000,
                    Value = 50
                };

                var lblResidentsCount = new Label
                {
                    Text = "Кол-во жильцов:*",
                    Location = new Point(20, 220),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtResidentsCount = new NumericUpDown
                {
                    Location = new Point(150, 218),
                    Width = 300,
                    Font = new Font("Arial", 9),
                    Minimum = 1,
                    Maximum = 50,
                    Value = 1
                };

                // 4. ВАЛИДАЦИЯ И КНОПКИ
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 260),
                    Width = 440,
                    Height = 30,
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    Visible = false
                };

                var btnSave = new Button
                {
                    Text = "Сохранить",
                    Location = new Point(150, 300),
                    Size = new Size(100, 35),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(260, 300),
                    Size = new Size(100, 35),
                    BackColor = Color.LightCoral,
                    Font = new Font("Arial", 9)
                };

                // Функции валидации
                void ShowError(string message)
                {
                    lblError.Text = message;
                    lblError.Visible = true;
                }

                void HideError()
                {
                    lblError.Text = "";
                    lblError.Visible = false;
                }

                bool ValidateForm()
                {
                    HideError();

                    if (cmbClient.SelectedItem == null)
                    {
                        ShowError("Выберите клиента");
                        cmbClient.Focus();
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(txtStreet.Text))
                    {
                        ShowError("Введите улицу");
                        txtStreet.Focus();
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(txtHouse.Text))
                    {
                        ShowError("Введите номер дома");
                        txtHouse.Focus();
                        return false;
                    }

                    if (txtLivingArea.Value <= 0)
                    {
                        ShowError("Площадь должна быть больше 0");
                        txtLivingArea.Focus();
                        return false;
                    }

                    if (txtResidentsCount.Value <= 0)
                    {
                        ShowError("Количество жильцов должно быть больше 0");
                        txtResidentsCount.Focus();
                        return false;
                    }

                    return true;
                }

                // Обработчик сохранения
                  btnSave.Click += async (s, e) =>
        {
            if (!ValidateForm())
                return;

            try
            {
                btnSave.Enabled = false;
                btnCancel.Enabled = false;
                btnSave.Text = "Сохранение...";

                var selectedClient = (dynamic)cmbClient.SelectedItem;

                var address = new Address
                {
                    ClientID = selectedClient.ClientID,
                    Client = selectedClient.ClientObject, // Сохраняем объект клиента
                    Street = txtStreet.Text.Trim(),
                    House = txtHouse.Text.Trim(),
                    Apartment = string.IsNullOrWhiteSpace(txtApartment.Text) ? null : txtApartment.Text.Trim(),
                    LivingArea = txtLivingArea.Value,
                    ResidentsCount = (int)txtResidentsCount.Value
                };

                // Сохраняем адрес в базу данных
                var newAddressId = await _addressRepository.AddAddressAsync(address);

                // Закрываем форму и обновляем таблицу
                addForm.DialogResult = DialogResult.OK;
                addForm.Close();

                // Обновляем таблицу адресов
                await LoadAddressesAsync();

                MessageBox.Show($"Адрес успешно добавлен! ID: {newAddressId}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
                btnSave.Text = "Сохранить";
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        };

                btnCancel.Click += (s, e) =>
                {
                    addForm.DialogResult = DialogResult.Cancel;
                    addForm.Close();
                };

                // Скрываем ошибку при изменении
                void HideErrorOnChange(object sender, EventArgs e) => HideError();

                cmbClient.SelectedIndexChanged += HideErrorOnChange;
                txtStreet.TextChanged += HideErrorOnChange;
                txtHouse.TextChanged += HideErrorOnChange;
                txtLivingArea.ValueChanged += HideErrorOnChange;
                txtResidentsCount.ValueChanged += HideErrorOnChange;

                addForm.AcceptButton = btnSave;
                addForm.CancelButton = btnCancel;

                // Добавляем элементы на форму
                addForm.Controls.AddRange(new Control[]
                {
            lblClient, cmbClient,
            lblStreet, txtStreet,
            lblHouse, txtHouse,
            lblApartment, txtApartment,
            lblLivingArea, txtLivingArea,
            lblResidentsCount, txtResidentsCount,
            lblError,
            btnSave, btnCancel
                });

                txtStreet.Focus();
                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteSelectedAddressAsync()
        {
            if (dataGridViewAddresses?.SelectedRows.Count == 0)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Выберите адрес для удаления!", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
                return;
            }

            var selectedRow = dataGridViewAddresses.SelectedRows[0];
            var address = selectedRow.DataBoundItem as Address;

            if (address == null)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show("Ошибка получения данных адреса", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
                return;
            }

            DialogResult result = DialogResult.No;
            SafeInvoke(() =>
            {
                result = MessageBox.Show($"Вы уверены, что хотите удалить адрес: {address.Street}, {address.House}{(!string.IsNullOrEmpty(address.Apartment) ? $", кв. {address.Apartment}" : "")}?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            });

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление адреса...");
                    var success = await _addressRepository.DeleteAddressAsync(address.AddressID);

                    if (success)
                    {
                        await LoadAddressesAsync();
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Адрес успешно удален!", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                        UpdateStatus("Адрес удален");
                    }
                    else
                    {
                        SafeInvoke(() =>
                        {
                            MessageBox.Show("Не удалось удалить адрес", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                        UpdateStatus("Ошибка удаления адреса");
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show($"Ошибка при удалении адреса: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    UpdateStatus("Ошибка удаления адреса");
                }
            }
        }
        #endregion

        #region Вспомогательные методы

        private async Task ShowUnpaidBillsReportAsync()
        {
            try
            {
                UpdateStatus("Формирование отчета...");
                var reportService = new ReportService(_dbConnection);

                var clients = await _clientRepository.GetClientsAsync();
                if (clients.Count == 0)
                {
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Нет клиентов для отчета", "Информация");
                    });
                    return;
                }

                var unpaidBills = await reportService.GetUnpaidBillsByClientAsync(clients[0].ClientID);
                var reportText = $"Неоплаченные счета для {clients[0].LastName} {clients[0].FirstName}:\n\n";

                foreach (var bill in unpaidBills)
                {
                    reportText += $"{bill.ServiceName}: {bill.Amount:C2} (до {bill.PaymentDate:dd.MM.yyyy})\n";
                }

                if (unpaidBills.Count == 0)
                {
                    reportText += "Неоплаченных счетов нет";
                }

                SafeInvoke(() =>
                {
                    MessageBox.Show(reportText, "Отчет по неоплаченным счетам",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
                UpdateStatus("Отчет сформирован");
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка");
                });
                UpdateStatus("Ошибка формирования отчета");
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

                SafeInvoke(() =>
                {
                    MessageBox.Show(details, "Информация о клиенте",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
                });
            }
        }

        private void ShowServiceDetails(Service service)
        {
            try
            {
                var details = $"Услуга: {service.ServiceName}\n" +
                             $"ID: {service.ServiceID}\n" +
                             $"Тип: {service.ServiceType}";

                SafeInvoke(() =>
                {
                    MessageBox.Show(details, "Информация об услуге",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
                });
            }
        }

        private void ShowTariffDetails(Tariff tariff)
        {
            try
            {
                var details = $"Тариф ID: {tariff.TariffID}\n" +
                             $"Услуга: {tariff.ServiceName}\n" +
                             $"Цена за кв.м: {tariff.PricePerSquareMeter:C2}\n" +
                             $"Цена за человека: {tariff.PricePerPerson:C2}\n" +
                             $"Цена за объем: {tariff.PricePerUnit:C2}";

                SafeInvoke(() =>
                {
                    MessageBox.Show(details, "Информация о тарифе",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
                });
            }
        }

        private void ShowAddressDetails(Address address)
        {
            try
            {
                var clientName = address.Client != null ?
                    $"{address.Client.LastName} {address.Client.FirstName}" : "Не указан";

                var details = $"Адрес ID: {address.AddressID}\n" +
                             $"Клиент: {clientName}\n" +
                             $"Улица: {address.Street}\n" +
                             $"Дом: {address.House}\n" +
                             $"Квартира: {address.Apartment ?? "не указана"}\n" +
                             $"Площадь: {address.LivingArea:F2} м²\n" +
                             $"Жильцов: {address.ResidentsCount}";

                SafeInvoke(() =>
                {
                    MessageBox.Show(details, "Информация об адресе",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                SafeInvoke(() =>
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true;

            var cleanPhone = phone.Trim();
            return Regex.IsMatch(cleanPhone, @"^[\+]?[0-9\s\-\(\)]{5,20}$");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
        #endregion
    }
}