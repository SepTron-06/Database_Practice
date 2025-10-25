using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class ClientRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public ClientRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> AddClientAsync(Client client)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            INSERT INTO Clients (FirstName, LastName, Phone, Email)
            VALUES (@FirstName, @LastName, @Phone, @Email)
            RETURNING ClientID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastName", client.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", client.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<List<Client>> GetClientsAsync()
        {
            var clients = new List<Client>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = "SELECT * FROM Clients ORDER BY LastName, FirstName";
            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                try
                {
                    var client = new Client
                    {
                        ClientID = GetSafeInt32(reader, "ClientID"),
                        FirstName = GetSafeString(reader, "FirstName"),
                        LastName = GetSafeString(reader, "LastName"),
                        Phone = GetSafeString(reader, "Phone"),
                        Email = GetSafeString(reader, "Email")
                    };
                    clients.Add(client);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но продолжаем обработку других записей
                    Console.WriteLine($"Ошибка при чтении клиента: {ex.Message}");
                }
            }

            return clients;
        }

        public async Task<Client> GetClientByIdAsync(int clientId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = "SELECT * FROM Clients WHERE ClientID = @ClientID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", clientId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                try
                {
                    return new Client
                    {
                        ClientID = GetSafeInt32(reader, "ClientID"),
                        FirstName = GetSafeString(reader, "FirstName"),
                        LastName = GetSafeString(reader, "LastName"),
                        Phone = GetSafeString(reader, "Phone"),
                        Email = GetSafeString(reader, "Email")
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении клиента {clientId}: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        public async Task UpdateClientAsync(Client client)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            UPDATE Clients 
            SET FirstName = @FirstName, LastName = @LastName, Phone = @Phone, Email = @Email
            WHERE ClientID = @ClientID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", client.ClientID);
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastName", client.LastName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", client.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteClientAsync(int clientId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = "DELETE FROM Clients WHERE ClientID = @ClientID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", clientId);

            await cmd.ExecuteNonQueryAsync();
        }

        // Вспомогательные методы для безопасного чтения данных
        private string GetSafeString(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения столбца {columnName}: {ex.Message}");
                return null;
            }
        }

        private int GetSafeInt32(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения столбца {columnName}: {ex.Message}");
                return 0;
            }
        }

        private decimal GetSafeDecimal(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetDecimal(ordinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения столбца {columnName}: {ex.Message}");
                return 0;
            }
        }

        private DateTime GetSafeDateTime(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения столбца {columnName}: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        private bool GetSafeBoolean(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения столбца {columnName}: {ex.Message}");
                return false;
            }
        }
    }
}

