using WinFormsDB.Data;
using WinFormsDB.Models;
using Npgsql;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WinFormsDB.Repositories
{
    public class ClientRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public ClientRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Основные методы работы с БД
        public async Task<List<Client>> GetClientsAsync()
        {
            var clients = new List<Client>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Простая проверка существования таблицы
                    var checkTableQuery = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = 'clients'
                )";

                    using (var checkCommand = new NpgsqlCommand(checkTableQuery, connection))
                    {
                        var tableExists = (bool)await checkCommand.ExecuteScalarAsync();
                        if (!tableExists)
                        {
                            throw new Exception("Таблица 'clients' не существует в базе данных");
                        }
                    }

                    var query = @"
                SELECT client_id, first_name, last_name, phone, email 
                FROM clients 
                ORDER BY client_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            clients.Add(new Client
                            {
                                ClientID = reader.GetInt32("client_id"),
                                FirstName = reader.GetString("first_name"),
                                LastName = reader.GetString("last_name"),
                                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                Email = reader.IsDBNull("email") ? null : reader.GetString("email")
                            });
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки клиентов из базы данных: {ex.Message}", ex);
            }

            return clients;
        }

        public async Task<int> AddClientAsync(Client client)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO clients (first_name, last_name, phone, email)
                        VALUES (@FirstName, @LastName, @Phone, @Email)
                        RETURNING client_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", client.FirstName);
                        command.Parameters.AddWithValue("@LastName", client.LastName);
                        command.Parameters.AddWithValue("@Phone", client.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        return (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления клиента в базу данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteClientAsync(int clientId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM clients WHERE client_id = @ClientID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", clientId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления клиента из базы данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateClientAsync(Client client)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE clients 
                        SET first_name = @FirstName, 
                            last_name = @LastName, 
                            phone = @Phone, 
                            email = @Email
                        WHERE client_id = @ClientID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", client.ClientID);
                        command.Parameters.AddWithValue("@FirstName", client.FirstName);
                        command.Parameters.AddWithValue("@LastName", client.LastName);
                        command.Parameters.AddWithValue("@Phone", client.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка обновления клиента в базе данных: {ex.Message}", ex);
            }
        }

        public async Task<Client> GetClientByIdAsync(int clientId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT client_id, first_name, last_name, phone, email 
                        FROM clients 
                        WHERE client_id = @ClientID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", clientId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Client
                                {
                                    ClientID = reader.GetInt32("client_id"),
                                    FirstName = reader.GetString("first_name"),
                                    LastName = reader.GetString("last_name"),
                                    Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                    Email = reader.IsDBNull("email") ? null : reader.GetString("email")
                                };
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки клиента по ID: {ex.Message}", ex);
            }

            return null;
        }

        // Синхронные методы для обратной совместимости
        public List<Client> GetClients()
        {
            return GetClientsAsync().GetAwaiter().GetResult();
        }

        public void AddClient(Client client)
        {
            AddClientAsync(client).GetAwaiter().GetResult();
        }

        public void DeleteClient(int clientId)
        {
            DeleteClientAsync(clientId).GetAwaiter().GetResult();
        }

        public void UpdateClient(Client client)
        {
            UpdateClientAsync(client).GetAwaiter().GetResult();
        }
    }
}