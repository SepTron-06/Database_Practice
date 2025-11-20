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
        private List<Client> _clients;

        public ClientRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _clients = new List<Client>();

            // Инициализация данных при создании репозитория
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // Пытаемся загрузить клиентов из базы
                await LoadClientsFromDatabaseAsync();

                // Если в базе нет данных, создаем демо-клиентов
                if (!_clients.Any())
                {
                    await CreateAndSaveDefaultClientsAsync();
                }
            }
            catch
            {
                // При ошибке БД используем демо-данные
                CreateDefaultClientsInMemory();
            }
        }

        private void CreateDefaultClientsInMemory()
        {
            _clients = new List<Client>
            {
                new Client { ClientID = 1, FirstName = "Ярослав", LastName = "Зайцев", Phone = "7(959)506-97-48", Email = "Zaitsevyaroslav@mail.ru" },
                new Client { ClientID = 2, FirstName = "Андрей", LastName = "Крюков", Phone = "7(959)489-61-02", Email = "KryukovAndrey@mail.ru" }
            };
        }

        private async Task CreateAndSaveDefaultClientsAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Сбрасываем последовательность и вставляем данные с явными ID
                    var resetAndInsertQuery = @"
                        -- Сбрасываем последовательность
                        ALTER SEQUENCE clients_clientid_seq RESTART WITH 1;
                        
                        -- Вставляем клиентов с явными ID
                        INSERT INTO clients (clientid, firstname, lastname, phone, email) 
                        VALUES 
                        (1, 'Ярослав', 'Зайцев', '7(959)506-97-48', 'Zaitsevyaroslav@mail.ru'),
                        (2, 'Андрей', 'Крюков', '7(959)489-61-02', 'KryukovAndrey@mail.ru');
                        
                        -- Устанавливаем последовательность на следующий доступный ID
                        SELECT setval('clients_clientid_seq', (SELECT MAX(clientid) FROM clients));";

                    using (var command = new NpgsqlCommand(resetAndInsertQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // После сохранения загружаем данные из базы
                await LoadClientsFromDatabaseAsync();
            }
            catch
            {
                // Если ошибка при сохранении в БД, создаем в памяти
                CreateDefaultClientsInMemory();
            }
        }

        private async Task LoadClientsFromDatabaseAsync()
        {
            try
            {
                var clients = new List<Client>();

                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT clientid, firstname, lastname, phone, email FROM clients ORDER BY clientid";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            clients.Add(new Client
                            {
                                ClientID = reader.GetInt32("clientid"),
                                FirstName = reader.GetString("firstname"),
                                LastName = reader.GetString("lastname"),
                                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                Email = reader.IsDBNull("email") ? null : reader.GetString("email")
                            });
                        }
                    }
                }

                _clients = clients; // Заменяем весь список
            }
            catch
            {
                // При ошибке оставляем пустой список
                _clients.Clear();
            }
        }

        // Основные методы
        public async Task<List<Client>> GetClientsAsync()
        {
            return await Task.FromResult(_clients);
        }

        public async Task AddClientAsync(Client client)
        {
            // Сначала сохраняем в базу, чтобы получить правильный ID
            var newId = await SaveClientToDatabaseAsync(client);

            // Затем добавляем в память с правильным ID
            client.ClientID = newId;
            _clients.Add(client);
        }

        public async Task DeleteClientAsync(int clientId)
        {
            var client = _clients.FirstOrDefault(c => c.ClientID == clientId);
            if (client != null)
            {
                _clients.Remove(client);
                // Удаляем из базы данных
                await DeleteClientFromDatabaseAsync(clientId);
            }
        }

        // Метод для сохранения в БД с возвратом нового ID
        private async Task<int> SaveClientToDatabaseAsync(Client client)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO clients (firstname, lastname, phone, email)
                        VALUES (@FirstName, @LastName, @Phone, @Email)
                        RETURNING clientid";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", client.FirstName);
                        command.Parameters.AddWithValue("@LastName", client.LastName);
                        command.Parameters.AddWithValue("@Phone", client.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", client.Email ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();
                        return (int)(long)result;
                    }
                }
            }
            catch
            {
                // Если ошибка БД, генерируем ID в памяти
                var newId = _clients.Count > 0 ? _clients.Max(c => c.ClientID) + 1 : 1;
                return newId;
            }
        }

        private async Task DeleteClientFromDatabaseAsync(int clientId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM clients WHERE clientid = @ClientID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", clientId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки БД
            }
        }

        // Синхронные методы для обратной совместимости
        public List<Client> GetClients() => _clients;

        public void AddClient(Client client) => AddClientAsync(client).GetAwaiter().GetResult();

        public void DeleteClient(int clientId) => DeleteClientAsync(clientId).GetAwaiter().GetResult();
    }
}