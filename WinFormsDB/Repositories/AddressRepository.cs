using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class AddressRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private List<Address> _addresses;
        private readonly ClientRepository _clientRepository;

        public AddressRepository(DatabaseConnection dbConnection, ClientRepository clientRepository)
        {
            _dbConnection = dbConnection;
            _clientRepository = clientRepository;
            _addresses = new List<Address>();

            // Инициализация данных при создании репозитория
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // Пытаемся загрузить адреса из базы
                await LoadAddressesFromDatabaseAsync();

                // Если в базе нет данных, создаем демо-адреса
                if (!_addresses.Any())
                {
                    CreateDefaultAddresses();
                    await SaveDefaultAddressesToDatabaseAsync();
                }
            }
            catch
            {
                // При ошибке БД используем демо-данные
                CreateDefaultAddresses();
            }
        }

        private void CreateDefaultAddresses()
        {
            // Получаем клиентов для связывания
            var clients = _clientRepository.GetClients();
            var client1 = clients.FirstOrDefault(c => c.ClientID == 1); // Ярослав Зайцев
            var client2 = clients.FirstOrDefault(c => c.ClientID == 2); // Андрей Крюков

            _addresses = new List<Address>
            {
                new Address
                {
                    AddressID = 1,
                    ClientID = 1,
                    Client = client1,
                    Street = "Ленина",
                    House = "10",
                    Apartment = "25",
                    LivingArea = 65.5m,
                    ResidentsCount = 3
                },
                new Address
                {
                    AddressID = 2,
                    ClientID = 2,
                    Client = client2,
                    Street = "Центральная",
                    House = "15",
                    Apartment = "10",
                    LivingArea = 45.0m,
                    ResidentsCount = 1
                }
            };
        }

        private async Task LoadAddressesFromDatabaseAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                SELECT 
                    a.address_id as AddressID,
                    a.client_id as ClientID,
                    a.street as Street,
                    a.house as House,
                    a.apartment as Apartment,
                    a.living_area as LivingArea,
                    a.residents_count as ResidentsCount,
                    c.client_id,
                    c.first_name as FirstName,
                    c.last_name as LastName,
                    c.phone as Phone,
                    c.email as Email
                FROM addresses a 
                LEFT JOIN clients c ON a.client_id = c.client_id
                ORDER BY a.street, a.house";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var address = new Address
                            {
                                AddressID = reader.GetInt32("AddressID"),
                                ClientID = reader.GetInt32("ClientID"),
                                Street = reader.GetString("Street"),
                                House = reader.GetString("House"),
                                LivingArea = reader.GetDecimal("LivingArea"),
                                ResidentsCount = reader.GetInt32("ResidentsCount")
                            };

                            if (!reader.IsDBNull(reader.GetOrdinal("Apartment")))
                            {
                                address.Apartment = reader.GetString("Apartment");
                            }

                            // Всегда создаем объект клиента, если есть ClientID
                            address.Client = new Client
                            {
                                ClientID = reader.GetInt32("client_id"),
                                FirstName = reader.GetString("FirstName"),
                                LastName = reader.GetString("LastName"),
                                Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                                Email = reader.IsDBNull("Email") ? null : reader.GetString("Email")
                            };

                            _addresses.Add(address);
                        }
                    }
                }
            }
            catch
            {
                // При ошибке оставляем пустой список
                _addresses.Clear();
            }
        }

        private async Task SaveDefaultAddressesToDatabaseAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    foreach (var address in _addresses)
                    {
                        var query = @"
                            INSERT INTO addresses (client_id, street, house, apartment, living_area, residents_count)
                            VALUES (@ClientID, @Street, @House, @Apartment, @LivingArea, @ResidentsCount)";

                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ClientID", address.ClientID);
                            command.Parameters.AddWithValue("@Street", address.Street);
                            command.Parameters.AddWithValue("@House", address.House);
                            command.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@LivingArea", address.LivingArea);
                            command.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }

        public async Task<List<Address>> GetAddressesAsync()
        {
            return await Task.FromResult(_addresses);
        }

        public async Task<int> AddAddressAsync(Address address)
        {
            // Добавляем в память
            var newId = _addresses.Count > 0 ? _addresses.Max(a => a.AddressID) + 1 : 1;
            address.AddressID = newId;

            // Находим клиента и привязываем его к адресу
            var client = _clientRepository.GetClients().FirstOrDefault(c => c.ClientID == address.ClientID);
            if (client != null)
            {
                address.Client = client;
            }

            _addresses.Add(address);

            // Сохраняем в базу данных
            await SaveAddressToDatabaseAsync(address);

            return newId;
        }

        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            // Удаляем из памяти
            var address = _addresses.FirstOrDefault(a => a.AddressID == addressId);
            if (address != null)
            {
                _addresses.Remove(address);

                // Удаляем из базы данных
                await DeleteAddressFromDatabaseAsync(addressId);
                return true;
            }

            return false;
        }

        // Простые методы работы с БД
        private async Task SaveAddressToDatabaseAsync(Address address)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO addresses (client_id, street, house, apartment, living_area, residents_count)
                        VALUES (@ClientID, @Street, @House, @Apartment, @LivingArea, @ResidentsCount)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", address.ClientID);
                        command.Parameters.AddWithValue("@Street", address.Street);
                        command.Parameters.AddWithValue("@House", address.House);
                        command.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LivingArea", address.LivingArea);
                        command.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки БД
            }
        }

        private async Task DeleteAddressFromDatabaseAsync(int addressId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM addresses WHERE address_id = @AddressID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", addressId);
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
        public List<Address> GetAddresses() => _addresses;

        public void AddAddress(Address address) => AddAddressAsync(address).GetAwaiter().GetResult();

        public void DeleteAddress(int addressId) => DeleteAddressAsync(addressId).GetAwaiter().GetResult();

        // Дополнительные методы для работы с адресами
        public async Task<List<Address>> GetAddressesByClientIdAsync(int clientId)
        {
            var addresses = _addresses.Where(a => a.ClientID == clientId).ToList();
            return await Task.FromResult(addresses);
        }

        public async Task<Address> GetAddressByIdAsync(int addressId)
        {
            var address = _addresses.FirstOrDefault(a => a.AddressID == addressId);
            return await Task.FromResult(address);
        }
    }
}