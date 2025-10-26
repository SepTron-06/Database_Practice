using WinFormsDB.Data;
using WinFormsDB.Repositories;
using WinFormsDB.Models;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using WinFormsDB.Services;

namespace WinFormsDB.Forms
{
    public partial class MainForm
    {
        // Методы для работы с тарифами
        private async Task LoadTariffsAsync()
        {
            try
            {
                UpdateStatus("Загрузка тарифов...");
                var tariffs = await _tariffRepository.GetTariffsAsync();
                dataGridViewTariffs.DataSource = tariffs;

                ConfigureTariffsGridColumns();
                UpdateStatus($"Загружено тарифов: {tariffs.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки тарифов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ошибка загрузки данных");
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

        private async void ShowAddTariffForm()
        {
            try
            {
                // Получаем список услуг для выпадающего списка
                var services = await _serviceRepository.GetServicesAsync();

                var addForm = new Form
                {
                    Text = "Добавить новый тариф",
                    Size = new Size(400, 250),
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
                    Width = 200,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем комбобокс услугами
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
                    Width = 200,
                    Font = new Font("Arial", 9),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем комбобокс типами цен
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
                    Width = 200,
                    Font = new Font("Arial", 9),
                    DecimalPlaces = 2,
                    Minimum = 0,
                    Maximum = 100000,
                    Increment = 0.01m
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 130),
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
                    Location = new Point(150, 160),
                    Size = new Size(80, 30),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(240, 160),
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
                            ServiceName = selectedService.ServiceName,
                            PricePerSquareMeter = priceType == "Цена за квадратный метр" ? price : 0m,
                            PricePerPerson = priceType == "Цена за человека" ? price : 0m,
                            PricePerUnit = priceType == "Цена за потребляемый объем" ? price : 0m
                        };

                        await _tariffRepository.AddTariffAsync(tariff);

                        addForm.DialogResult = DialogResult.OK;
                        addForm.Close();

                        await LoadTariffsAsync();

                        MessageBox.Show("Тариф успешно добавлен!", "Успех",
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
                addForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DeleteSelectedTariff()
        {
            if (dataGridViewTariffs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите тариф для удаления!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridViewTariffs.SelectedRows[0];
            var tariff = selectedRow.DataBoundItem as Tariff;

            if (tariff == null)
            {
                MessageBox.Show("Ошибка получения данных тарифа", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить тариф для услуги \"{tariff.ServiceName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление тарифа...");
                    await _tariffRepository.DeleteTariffAsync(tariff.TariffID);
                    await LoadTariffsAsync();
                    MessageBox.Show("Тариф успешно удален!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatus("Тариф удален");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении тарифа: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка удаления тарифа");
                }
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

                MessageBox.Show(details, "Информация о тарифе",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
            }
        }

        // Вспомогательные методы
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
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
            return System.Text.RegularExpressions.Regex.IsMatch(cleanPhone,
                @"^[\+]?[0-9\s\-\(\)]{5,20}$");
        }

        private async void ShowUnpaidBillsReport()
        {
            try
            {
                UpdateStatus("Формирование отчета...");
                var reportService = new ReportService(_dbConnection);

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
    }
}