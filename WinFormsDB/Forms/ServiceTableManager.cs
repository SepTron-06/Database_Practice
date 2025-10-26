using WinFormsDB.Data;
using WinFormsDB.Repositories;
using WinFormsDB.Models;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace WinFormsDB.Forms
{
    public partial class MainForm
    {
        // Методы для работы с услугами
        private async Task LoadServicesAsync()
        {
            try
            {
                UpdateStatus("Загрузка услуг...");
                var services = await _serviceRepository.GetServicesAsync();
                dataGridViewServices.DataSource = services;

                ConfigureServicesGridColumns();
                UpdateStatus($"Загружено услуг: {services.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ошибка загрузки данных");
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

        private async void ShowAddServiceForm()
        {
            try
            {
                var addForm = new Form
                {
                    Text = "Добавить новую услугу",
                    Size = new Size(400, 200),
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
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtServiceName = new TextBox
                {
                    Location = new Point(150, 18),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Название услуги"
                };

                // Поле для типа услуги
                var lblServiceType = new Label
                {
                    Text = "Тип услуги:",
                    Location = new Point(20, 60),
                    Width = 120,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                var txtServiceType = new TextBox
                {
                    Location = new Point(150, 58),
                    Width = 200,
                    Font = new Font("Arial", 9),
                    Tag = "Тип услуги"
                };

                // Метка для отображения ошибок
                var lblError = new Label
                {
                    Text = "",
                    Location = new Point(20, 95),
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
                    Location = new Point(150, 120),
                    Size = new Size(80, 30),
                    BackColor = Color.LightGreen,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(240, 120),
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

                        await LoadServicesAsync();

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

        private async void DeleteSelectedService()
        {
            if (dataGridViewServices.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите услугу для удаления!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridViewServices.SelectedRows[0];
            var service = selectedRow.DataBoundItem as Service;

            if (service == null)
            {
                MessageBox.Show("Ошибка получения данных услуги", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить услугу \"{service.ServiceName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление услуги...");
                    await _serviceRepository.DeleteServiceAsync(service.ServiceID);
                    await LoadServicesAsync();
                    MessageBox.Show("Услуга успешно удалена!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatus("Услуга удалена");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка удаления услуги");
                }
            }
        }

        private void ShowServiceDetails(Service service)
        {
            try
            {
                var details = $"Услуга: {service.ServiceName}\n" +
                             $"ID: {service.ServiceID}\n" +
                             $"Тип: {service.ServiceType}";

                MessageBox.Show(details, "Информация об услуге",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отображения деталей: {ex.Message}", "Ошибка");
            }
        }
    }
}