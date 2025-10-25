using Npgsql;
using System.Windows.Forms;
using WinFormsDB.Configuration;
using WinFormsDB.Data;
using WinFormsDB.Forms;

namespace WinFormsDB
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            // Инициализация конфигурации приложения ДО всего остального
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Инициализация базы данных
            //try
            //{
            //    var dbConnection = new DatabaseConnection(AppConfig.ConnectionString);

            //    // Создаем и инициализируем БД
            //    var initializer = new DatabaseInitializer(dbConnection);
            //    await initializer.InitializeDatabaseAsync();

            //    // Проверяем структуру (для отладки)
            //    var validator = new DatabaseValidator(dbConnection);
            //    await validator.ValidateTableStructureAsync();

            //    // Запускаем главную форму
            //    Application.Run(new Forms.MainForm(dbConnection));
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Ошибка инициализации приложения: {ex.Message}", "Ошибка",
            //        MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
            try
            {
                // Показываем информацию о подключении
                MessageBox.Show($"Попытка подключения к: {AppConfig.ConnectionString}", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                var dbConnection = new DatabaseConnection(AppConfig.ConnectionString);

                // Тестируем подключение
                await TestConnection(dbConnection);

                var initializer = new DatabaseInitializer(dbConnection);
                await initializer.InitializeDatabaseAsync();

                // Проверяем структуру
                var validator = new DatabaseValidator(dbConnection);
                await validator.ValidateTableStructureAsync();

                //Application.Run(new MainForm(dbConnection));
                Application.Run(new Forms.MainForm(dbConnection));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации приложения:\n{ex.Message}\n\n" +
                    $"Проверьте:\n" +
                    $"1. Запущен ли PostgreSQL\n" +
                    $"2. Правильность пароля в AppConfig.cs\n" +
                    $"3. Существует ли база данных",
                    "Ошибка подключения",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private static async Task TestConnection(DatabaseConnection dbConnection)
        {
            try
            {
                using var connection = await dbConnection.GetConnectionAsync();
                using var cmd = new NpgsqlCommand("SELECT version();", connection);
                var version = await cmd.ExecuteScalarAsync();
                MessageBox.Show($"Подключение успешно!\nPostgreSQL version: {version}", "Успех");
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось подключиться к PostgreSQL: {ex.Message}");
            }
        }

    }
}