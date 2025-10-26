using WinFormsDB.Data;
using WinFormsDB.Repositories;
using WinFormsDB.Services;
using WinFormsDB.Models;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace WinFormsDB.Forms
{
    public partial class MainForm : Form
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ClientRepository _clientRepository;
        private readonly ServiceRepository _serviceRepository;
        private readonly TariffRepository _tariffRepository;

        private DataGridView? dataGridViewClients;
        private DataGridView? dataGridViewServices;
        private DataGridView? dataGridViewTariffs;
        private MenuStrip? mainMenu;
        private StatusStrip? statusStrip;
        private ToolStripStatusLabel? statusLabel;

        // Текущая активная таблица
        private string currentTable = "Clients";

        public MainForm(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _clientRepository = new ClientRepository(_dbConnection);
            _serviceRepository = new ServiceRepository(_dbConnection);
            _tariffRepository = new TariffRepository(_dbConnection);

            InitializeComponent();
            InitializeForm();
            _ = LoadClientsAsync();
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
            CreateDataGrids();
            CreateStatusBar();

            // Показываем таблицу клиентов по умолчанию
            ShowTable("Clients");
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

            tablesMenu.DropDownItems.Add(clientsTableItem);
            tablesMenu.DropDownItems.Add(servicesTableItem);
            tablesMenu.DropDownItems.Add(tariffsTableItem);

            // Меню "Клиенты" (только когда активна таблица клиентов)
            var clientsMenu = new ToolStripMenuItem("Клиенты");

            var addClientItem = new ToolStripMenuItem("Добавить клиента");
            addClientItem.Click += (s, e) => ShowAddClientForm();

            var deleteClientItem = new ToolStripMenuItem("Удалить клиента");
            deleteClientItem.Click += (s, e) => DeleteSelectedClient();

            var refreshClientsItem = new ToolStripMenuItem("Обновить список");
            refreshClientsItem.Click += (s, e) => LoadClientsAsync();

            var separator = new ToolStripSeparator();

            clientsMenu.DropDownItems.Add(addClientItem);
            clientsMenu.DropDownItems.Add(deleteClientItem);
            clientsMenu.DropDownItems.Add(separator);
            clientsMenu.DropDownItems.Add(refreshClientsItem);

            // Меню "Услуги" (только когда активна таблица услуг)
            var servicesMenu = new ToolStripMenuItem("Услуги");

            var addServiceItem = new ToolStripMenuItem("Добавить услугу");
            addServiceItem.Click += (s, e) => ShowAddServiceForm();

            var deleteServiceItem = new ToolStripMenuItem("Удалить услугу");
            deleteServiceItem.Click += (s, e) => DeleteSelectedService();

            var refreshServicesItem = new ToolStripMenuItem("Обновить список");
            refreshServicesItem.Click += (s, e) => LoadServicesAsync();

            servicesMenu.DropDownItems.Add(addServiceItem);
            servicesMenu.DropDownItems.Add(deleteServiceItem);
            servicesMenu.DropDownItems.Add(refreshServicesItem);

            // Меню "Тарифы" (только когда активна таблица тарифов)
            var tariffsMenu = new ToolStripMenuItem("Тарифы");

            var addTariffItem = new ToolStripMenuItem("Добавить тариф");
            addTariffItem.Click += (s, e) => ShowAddTariffForm();

            var deleteTariffItem = new ToolStripMenuItem("Удалить тариф");
            deleteTariffItem.Click += (s, e) => DeleteSelectedTariff();

            var refreshTariffsItem = new ToolStripMenuItem("Обновить список");
            refreshTariffsItem.Click += (s, e) => LoadTariffsAsync();

            tariffsMenu.DropDownItems.Add(addTariffItem);
            tariffsMenu.DropDownItems.Add(deleteTariffItem);
            tariffsMenu.DropDownItems.Add(refreshTariffsItem);

            // Меню "Отчеты"
            var reportsMenu = new ToolStripMenuItem("Отчеты");
            var unpaidBillsItem = new ToolStripMenuItem("Неоплаченные счета");
            unpaidBillsItem.Click += (s, e) => ShowUnpaidBillsReport();
            reportsMenu.DropDownItems.Add(unpaidBillsItem);

            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(tablesMenu);
            mainMenu.Items.Add(clientsMenu);
            mainMenu.Items.Add(servicesMenu);
            mainMenu.Items.Add(tariffsMenu);
            mainMenu.Items.Add(reportsMenu);

            this.Controls.Add(mainMenu);
            this.MainMenuStrip = mainMenu;
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

            dataGridViewServices!.CellDoubleClick += (s, e) =>
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

            dataGridViewTariffs!.CellDoubleClick += (s, e) =>
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

            this.Controls.Add(dataGridViewClients);
            this.Controls.Add(dataGridViewServices);
            this.Controls.Add(dataGridViewTariffs);
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
            if (dataGridViewClients == null || dataGridViewServices == null || dataGridViewTariffs == null)
                return;

            // Скрываем все таблицы
            dataGridViewClients.Visible = false;
            dataGridViewServices.Visible = false;
            dataGridViewTariffs.Visible = false;

            // Обновляем меню в зависимости от активной таблицы
            UpdateMenuVisibility(tableName);

            // Показываем выбранную таблицу и загружаем данные
            switch (tableName)
            {
                case "Clients":
                    dataGridViewClients.Visible = true;
                    currentTable = "Clients";
                    LoadClientsAsync();
                    UpdateStatus("Таблица: Клиенты");
                    break;
                case "Services":
                    dataGridViewServices.Visible = true;
                    currentTable = "Services";
                    LoadServicesAsync();
                    UpdateStatus("Таблица: Услуги");
                    break;
                case "Tariffs":
                    dataGridViewTariffs.Visible = true;
                    currentTable = "Tariffs";
                    LoadTariffsAsync();
                    UpdateStatus("Таблица: Тарифы");
                    break;
            }
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
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
                statusStrip!.Refresh();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
    }
}