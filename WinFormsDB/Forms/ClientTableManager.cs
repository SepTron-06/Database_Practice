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
        // Методы для работы с клиентами
        private async Task LoadClientsAsync()
        {
            try
            {
                UpdateStatus("Загрузка клиентов...");
                var clients = await _clientRepository.GetClientsAsync();
                dataGridViewClients.DataSource = clients;

                ConfigureClientGridColumns();
                UpdateStatus($"Загружено клиентов: {clients.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Ошибка загрузки данных");
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

        private async void ShowAddClientForm()
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

                        MessageBox.Show("Клиент успешно добавлен!", "Успех",
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
                MessageBox.Show($"Ошибка открытия формы: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DeleteSelectedClient()
        {
            if (dataGridViewClients.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите клиента для удаления!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridViewClients.SelectedRows[0];
            var client = selectedRow.DataBoundItem as Client;

            if (client == null)
            {
                MessageBox.Show("Ошибка получения данных клиента", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента {client.LastName} {client.FirstName}?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("Удаление клиента...");
                    await _clientRepository.DeleteClientAsync(client.ClientID);
                    await LoadClientsAsync();
                    MessageBox.Show("Клиент успешно удален!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatus("Клиент удален");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении клиента: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Ошибка удаления клиента");
                }
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
    }
}