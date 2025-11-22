using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class TariffRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public TariffRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Основные методы работы с БД
        public async Task<List<Tariff>> GetTariffsAsync()
        {
            var tariffs = new List<Tariff>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            tariff_id, 
                            service_id,
                            service_name, 
                            price_per_square_meter, 
                            price_per_person, 
                            price_per_unit
                        FROM tariffs 
                        ORDER BY tariff_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tariffs.Add(new Tariff
                            {
                                TariffID = reader.GetInt32("tariff_id"),
                                ServiceID = reader.GetInt32("service_id"),
                                ServiceName = reader.GetString("service_name"),
                                PricePerSquareMeter = reader.GetDecimal("price_per_square_meter"),
                                PricePerPerson = reader.GetDecimal("price_per_person"),
                                PricePerUnit = reader.GetDecimal("price_per_unit")
                            });
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки тарифов из базы данных: {ex.Message}", ex);
            }

            return tariffs;
        }

        public async Task<int> AddTariffAsync(Tariff tariff)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Сначала проверяем, существует ли услуга
                    var checkServiceQuery = "SELECT 1 FROM services WHERE service_id = @ServiceID";
                    using (var checkCommand = new NpgsqlCommand(checkServiceQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ServiceID", tariff.ServiceID);
                        var serviceExists = await checkCommand.ExecuteScalarAsync();

                        if (serviceExists == null)
                        {
                            throw new System.Exception($"Услуга с ID {tariff.ServiceID} не существует");
                        }
                    }

                    var query = @"
                        INSERT INTO tariffs (service_id, service_name, price_per_square_meter, price_per_person, price_per_unit)
                        VALUES (@ServiceID, @ServiceName, @PricePerSquareMeter, @PricePerPerson, @PricePerUnit)
                        RETURNING tariff_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", tariff.ServiceID);
                        command.Parameters.AddWithValue("@ServiceName", tariff.ServiceName);
                        command.Parameters.AddWithValue("@PricePerSquareMeter", tariff.PricePerSquareMeter);
                        command.Parameters.AddWithValue("@PricePerPerson", tariff.PricePerPerson);
                        command.Parameters.AddWithValue("@PricePerUnit", tariff.PricePerUnit);

                        var result = await command.ExecuteScalarAsync();
                        return (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления тарифа в базу данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteTariffAsync(int tariffId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Сначала проверяем, есть ли связанные счета
                    var checkQuery = "SELECT COUNT(*) FROM bills WHERE tariff_id = @TariffID";
                    using (var checkCommand = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@TariffID", tariffId);
                        var billCount = (long)await checkCommand.ExecuteScalarAsync();

                        if (billCount > 0)
                        {
                            throw new System.Exception("Невозможно удалить тариф, так как с ним связаны счета. Сначала удалите связанные счета.");
                        }
                    }

                    var query = "DELETE FROM tariffs WHERE tariff_id = @TariffID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariffId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления тарифа из базы данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateTariffAsync(Tariff tariff)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Сначала проверяем, существует ли услуга
                    var checkServiceQuery = "SELECT 1 FROM services WHERE service_id = @ServiceID";
                    using (var checkCommand = new NpgsqlCommand(checkServiceQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ServiceID", tariff.ServiceID);
                        var serviceExists = await checkCommand.ExecuteScalarAsync();

                        if (serviceExists == null)
                        {
                            throw new System.Exception($"Услуга с ID {tariff.ServiceID} не существует");
                        }
                    }

                    var query = @"
                        UPDATE tariffs 
                        SET service_id = @ServiceID,
                            service_name = @ServiceName,
                            price_per_square_meter = @PricePerSquareMeter,
                            price_per_person = @PricePerPerson,
                            price_per_unit = @PricePerUnit
                        WHERE tariff_id = @TariffID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariff.TariffID);
                        command.Parameters.AddWithValue("@ServiceID", tariff.ServiceID);
                        command.Parameters.AddWithValue("@ServiceName", tariff.ServiceName);
                        command.Parameters.AddWithValue("@PricePerSquareMeter", tariff.PricePerSquareMeter);
                        command.Parameters.AddWithValue("@PricePerPerson", tariff.PricePerPerson);
                        command.Parameters.AddWithValue("@PricePerUnit", tariff.PricePerUnit);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка обновления тарифа в базе данных: {ex.Message}", ex);
            }
        }

        public async Task<Tariff> GetTariffByIdAsync(int tariffId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            tariff_id, 
                            service_id,
                            service_name, 
                            price_per_square_meter, 
                            price_per_person, 
                            price_per_unit
                        FROM tariffs 
                        WHERE tariff_id = @TariffID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariffId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Tariff
                                {
                                    TariffID = reader.GetInt32("tariff_id"),
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    PricePerSquareMeter = reader.GetDecimal("price_per_square_meter"),
                                    PricePerPerson = reader.GetDecimal("price_per_person"),
                                    PricePerUnit = reader.GetDecimal("price_per_unit")
                                };
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки тарифа по ID: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<List<Tariff>> GetTariffsByServiceIdAsync(int serviceId)
        {
            var tariffs = new List<Tariff>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            tariff_id, 
                            service_id,
                            service_name, 
                            price_per_square_meter, 
                            price_per_person, 
                            price_per_unit
                        FROM tariffs 
                        WHERE service_id = @ServiceID
                        ORDER BY tariff_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tariffs.Add(new Tariff
                                {
                                    TariffID = reader.GetInt32("tariff_id"),
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    PricePerSquareMeter = reader.GetDecimal("price_per_square_meter"),
                                    PricePerPerson = reader.GetDecimal("price_per_person"),
                                    PricePerUnit = reader.GetDecimal("price_per_unit")
                                });
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки тарифов по ID услуги: {ex.Message}", ex);
            }

            return tariffs;
        }

        public async Task<Tariff> GetTariffByServiceIdAsync(int serviceId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            tariff_id, 
                            service_id,
                            service_name, 
                            price_per_square_meter, 
                            price_per_person, 
                            price_per_unit
                        FROM tariffs 
                        WHERE service_id = @ServiceID
                        LIMIT 1";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Tariff
                                {
                                    TariffID = reader.GetInt32("tariff_id"),
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    PricePerSquareMeter = reader.GetDecimal("price_per_square_meter"),
                                    PricePerPerson = reader.GetDecimal("price_per_person"),
                                    PricePerUnit = reader.GetDecimal("price_per_unit")
                                };
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки тарифа по ID услуги: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<bool> TariffExistsAsync(int tariffId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT 1 FROM tariffs WHERE tariff_id = @TariffID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariffId);
                        var result = await command.ExecuteScalarAsync();
                        return result != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetTariffsCountAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT COUNT(*) FROM tariffs";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // Синхронные методы для обратной совместимости
        public List<Tariff> GetTariffs()
        {
            return GetTariffsAsync().GetAwaiter().GetResult();
        }

        public int AddTariff(Tariff tariff)
        {
            return AddTariffAsync(tariff).GetAwaiter().GetResult();
        }

        public bool DeleteTariff(int tariffId)
        {
            return DeleteTariffAsync(tariffId).GetAwaiter().GetResult();
        }

        public bool UpdateTariff(Tariff tariff)
        {
            return UpdateTariffAsync(tariff).GetAwaiter().GetResult();
        }

        public Tariff GetTariffById(int tariffId)
        {
            return GetTariffByIdAsync(tariffId).GetAwaiter().GetResult();
        }

        public List<Tariff> GetTariffsByServiceId(int serviceId)
        {
            return GetTariffsByServiceIdAsync(serviceId).GetAwaiter().GetResult();
        }

        public Tariff GetTariffByServiceId(int serviceId)
        {
            return GetTariffByServiceIdAsync(serviceId).GetAwaiter().GetResult();
        }

        public bool TariffExists(int tariffId)
        {
            return TariffExistsAsync(tariffId).GetAwaiter().GetResult();
        }

        public int GetTariffsCount()
        {
            return GetTariffsCountAsync().GetAwaiter().GetResult();
        }
    }
}