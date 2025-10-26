using Npgsql;
using System.Threading.Tasks;

namespace WinFormsDB.Data
{
    public class DatabaseConnection
    {
        public string ConnectionString { get; set; }

        // Конструктор без параметров
        public DatabaseConnection()
        {
            ConnectionString = "Host=localhost;Port=5432;Database=UD;Username=postgres;Password=1";
        }

        // Конструктор с параметром (если нужен)
        public DatabaseConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public bool TestConnection()
        {
            return true;
        }
    }
}