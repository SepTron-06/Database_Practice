using System;
using System.Windows.Forms;
using WinFormsDB.Data;
using WinFormsDB.Forms;

namespace WinFormsDB
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Показываем форму подключения к базе данных
            var connectionForm = new DatabaseConnectionForm();

            if (connectionForm.ShowDialog() == DialogResult.OK)
            {
                // Создаем подключение с введенными параметрами
                var dbConnection = new DatabaseConnection();
                dbConnection.SetConnectionParameters(
                    connectionForm.Host,
                    connectionForm.Port,
                    connectionForm.DatabaseName,
                    connectionForm.Username,
                    connectionForm.Password
                );

                // Проверяем подключение
                try
                {
                    // Запускаем главную форму
                    Application.Run(new MainForm(dbConnection));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка запуска приложения: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Пользователь отменил подключение
                MessageBox.Show("Подключение к базе данных отменено. Приложение будет закрыто.",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
