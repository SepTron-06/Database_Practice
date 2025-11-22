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

        public AddressRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Основные методы работы с БД
        public async Task<List<Address>> GetAddressesAsync()
        {
            var addresses = new List<Address>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                SELECT 
                    a.address_id,
                    a.client_id,
                    a.street,
                    a.house,
                    a.apartment,
                    a.living_area,
                    a.residents_count,
                    c.client_id as client_id,
                    c.first_name,
                    c.last_name,
                    c.phone,
                    c.email
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
                                AddressID = reader.GetInt32("address_id"),
                                ClientID = reader.GetInt32("client_id"),
                                Street = reader.GetString("street"),
                                House = reader.GetString("house"),
                                LivingArea = reader.GetDecimal("living_area"),
                                ResidentsCount = reader.GetInt32("residents_count")
                            };

                            if (!reader.IsDBNull(reader.GetOrdinal("apartment")))
                            {
                                address.Apartment = reader.GetString("apartment");
                            }

                            // Создаем объект клиента для навигационного свойства
                            if (!reader.IsDBNull(reader.GetOrdinal("first_name")))
                            {
                                address.Client = new Client
                                {
                                    ClientID = reader.GetInt32("client_id"),
                                    FirstName = reader.GetString("first_name"),
                                    LastName = reader.GetString("last_name"),
                                    Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                    Email = reader.IsDBNull("email") ? null : reader.GetString("email")
                                };
                            }

                            addresses.Add(address);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки адресов из базы данных: {ex.Message}", ex);
            }

            return addresses;
        }

        public async Task<int> AddAddressAsync(Address address)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO addresses (client_id, street, house, apartment, living_area, residents_count)
                        VALUES (@ClientID, @Street, @House, @Apartment, @LivingArea, @ResidentsCount)
                        RETURNING address_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", address.ClientID);
                        command.Parameters.AddWithValue("@Street", address.Street);
                        command.Parameters.AddWithValue("@House", address.House);
                        command.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LivingArea", address.LivingArea);
                        command.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);

                        var result = await command.ExecuteScalarAsync();
                        return (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления адреса в базу данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM addresses WHERE address_id = @AddressID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", addressId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления адреса из базы данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateAddressAsync(Address address)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE addresses 
                        SET client_id = @ClientID,
                            street = @Street,
                            house = @House,
                            apartment = @Apartment,
                            living_area = @LivingArea,
                            residents_count = @ResidentsCount
                        WHERE address_id = @AddressID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", address.AddressID);
                        command.Parameters.AddWithValue("@ClientID", address.ClientID);
                        command.Parameters.AddWithValue("@Street", address.Street);
                        command.Parameters.AddWithValue("@House", address.House);
                        command.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@LivingArea", address.LivingArea);
                        command.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка обновления адреса в базе данных: {ex.Message}", ex);
            }
        }

        public async Task<Address> GetAddressByIdAsync(int addressId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            a.address_id,
                            a.client_id,
                            a.street,
                            a.house,
                            a.apartment,
                            a.living_area,
                            a.residents_count,
                            c.client_id,
                            c.first_name,
                            c.last_name,
                            c.phone,
                            c.email
                        FROM addresses a 
                        LEFT JOIN clients c ON a.client_id = c.client_id
                        WHERE a.address_id = @AddressID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", addressId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var address = new Address
                                {
                                    AddressID = reader.GetInt32("address_id"),
                                    ClientID = reader.GetInt32("client_id"),
                                    Street = reader.GetString("street"),
                                    House = reader.GetString("house"),
                                    LivingArea = reader.GetDecimal("living_area"),
                                    ResidentsCount = reader.GetInt32("residents_count")
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("apartment")))
                                {
                                    address.Apartment = reader.GetString("apartment");
                                }

                                address.Client = new Client
                                {
                                    ClientID = reader.GetInt32("client_id"),
                                    FirstName = reader.GetString("first_name"),
                                    LastName = reader.GetString("last_name"),
                                    Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                    Email = reader.IsDBNull("email") ? null : reader.GetString("email")
                                };

                                return address;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки адреса по ID: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<List<Address>> GetAddressesByClientIdAsync(int clientId)
        {
            var addresses = new List<Address>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            a.address_id,
                            a.client_id,
                            a.street,
                            a.house,
                            a.apartment,
                            a.living_area,
                            a.residents_count
                        FROM addresses a 
                        WHERE a.client_id = @ClientID
                        ORDER BY a.street, a.house";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClientID", clientId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var address = new Address
                                {
                                    AddressID = reader.GetInt32("address_id"),
                                    ClientID = reader.GetInt32("client_id"),
                                    Street = reader.GetString("street"),
                                    House = reader.GetString("house"),
                                    LivingArea = reader.GetDecimal("living_area"),
                                    ResidentsCount = reader.GetInt32("residents_count")
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("apartment")))
                                {
                                    address.Apartment = reader.GetString("apartment");
                                }

                                addresses.Add(address);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки адресов по ID клиента: {ex.Message}", ex);
            }

            return addresses;
        }

        // Синхронные методы для обратной совместимости
        public List<Address> GetAddresses()
        {
            return GetAddressesAsync().GetAwaiter().GetResult();
        }

        public int AddAddress(Address address)
        {
            return AddAddressAsync(address).GetAwaiter().GetResult();
        }

        public bool DeleteAddress(int addressId)
        {
            return DeleteAddressAsync(addressId).GetAwaiter().GetResult();
        }

        public bool UpdateAddress(Address address)
        {
            return UpdateAddressAsync(address).GetAwaiter().GetResult();
        }

        public Address GetAddressById(int addressId)
        {
            return GetAddressByIdAsync(addressId).GetAwaiter().GetResult();
        }

        public List<Address> GetAddressesByClientId(int clientId)
        {
            return GetAddressesByClientIdAsync(clientId).GetAwaiter().GetResult();
        }
    }
}