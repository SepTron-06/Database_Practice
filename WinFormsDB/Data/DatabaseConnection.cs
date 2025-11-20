
using Npgsql;
using System.Threading.Tasks;

namespace WinFormsDB.Data
{
    public class DatabaseConnection
    {
        public string ConnectionString { get; set; }

        // Конструктор по умолчанию (для формы подключения)
        public DatabaseConnection()
        {
            // Пустая строка подключения - будет установлена позже
            ConnectionString = string.Empty;
        }

        // Конструктор с параметром
        public DatabaseConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Метод для создания строки подключения из параметров
        public void SetConnectionParameters(string host, string port, string database, string username, string password)
        {
            ConnectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException("Строка подключения не установлена");
            }

            var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = await GetConnectionAsync())
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
