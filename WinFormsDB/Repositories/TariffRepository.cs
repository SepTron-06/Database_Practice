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
        private List<Tariff> _tariffs;

        public TariffRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _tariffs = new List<Tariff>();

            // Загружаем данные из базы при создании репозитория
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var tariffsFromDb = await LoadTariffsFromDatabaseAsync();
                if (tariffsFromDb.Any())
                {
                    _tariffs = tariffsFromDb;
                }
                else
                {
                    // Если в базе нет данных, используем демо-данные
                    _tariffs = GetDefaultTariffs();
                    await SaveDefaultTariffsToDatabaseAsync();
                }
            }
            catch
            {
                // Если ошибка базы, используем только in-memory
                _tariffs = GetDefaultTariffs();
            }
        }

        private List<Tariff> GetDefaultTariffs()
        {
            return new List<Tariff>
            {
                new Tariff { TariffID = 1, ServiceName = "Подача холодной воды", PricePerSquareMeter = 0m, PricePerPerson = 0m, PricePerUnit = 67.15m },
                new Tariff { TariffID = 2, ServiceName = "Подача горячей воды", PricePerSquareMeter = 0m, PricePerPerson = 0m, PricePerUnit = 73.62m },
                new Tariff { TariffID = 3, ServiceName = "Подача газа в квартиру", PricePerSquareMeter = 0m, PricePerPerson = 0m, PricePerUnit = 8.19m },
                new Tariff { TariffID = 4, ServiceName = "Взнос в капитальный ремонт дома", PricePerSquareMeter = 0m, PricePerPerson = 0m, PricePerUnit = 5.24m },
                new Tariff { TariffID = 5, ServiceName = "Отопление", PricePerSquareMeter = 3785.86m, PricePerPerson = 0m, PricePerUnit = 0m },
                new Tariff { TariffID = 6, ServiceName = "Взнос в капитальный ремонт дома", PricePerSquareMeter = 14.08m, PricePerPerson = 0m, PricePerUnit = 0m },
                new Tariff { TariffID = 7, ServiceName = "Обращение с ТКО", PricePerSquareMeter = 0m, PricePerPerson = 122.13m, PricePerUnit = 0m },
                new Tariff { TariffID = 8, ServiceName = "Электронное запирающее устройство", PricePerSquareMeter = 0m, PricePerPerson = 0m, PricePerUnit = 60.00m }
            };
        }

        private async Task<List<Tariff>> LoadTariffsFromDatabaseAsync()
        {
            var tariffs = new List<Tariff>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Предполагаем, что таблица tariffs уже создана
                    var query = @"
                        SELECT 
                            tariff_id as TariffID,
                            service_name as ServiceName,
                            price_per_square_meter as PricePerSquareMeter,
                            price_per_person as PricePerPerson,
                            price_per_unit as PricePerUnit
                        FROM tariffs 
                        ORDER BY tariff_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tariffs.Add(new Tariff
                            {
                                TariffID = reader.GetInt32("TariffID"),
                                ServiceName = reader.GetString("ServiceName"),
                                PricePerSquareMeter = reader.GetDecimal("PricePerSquareMeter"),
                                PricePerPerson = reader.GetDecimal("PricePerPerson"),
                                PricePerUnit = reader.GetDecimal("PricePerUnit")
                            });
                        }
                    }
                }
            }
            catch
            {
                // Если ошибка - возвращаем пустой список
            }

            return tariffs;
        }

        private async Task SaveDefaultTariffsToDatabaseAsync()
        {
            try
            {
                // Предполагаем, что таблица tariffs уже создана в MainForm
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    foreach (var tariff in _tariffs)
                    {
                        // Используем INSERT с обработкой конфликтов
                        var query = @"
                            INSERT INTO tariffs (service_name, price_per_square_meter, price_per_person, price_per_unit)
                            VALUES (@ServiceName, @PricePerSquareMeter, @PricePerPerson, @PricePerUnit)
                            ON CONFLICT (service_name) DO NOTHING";

                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ServiceName", tariff.ServiceName);
                            command.Parameters.AddWithValue("@PricePerSquareMeter", tariff.PricePerSquareMeter);
                            command.Parameters.AddWithValue("@PricePerPerson", tariff.PricePerPerson);
                            command.Parameters.AddWithValue("@PricePerUnit", tariff.PricePerUnit);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки записи в базу
            }
        }

        public async Task<List<Tariff>> GetTariffsAsync()
        {
            // Всегда возвращаем актуальные данные из памяти
            return await Task.FromResult(_tariffs);
        }

        public async Task AddTariffAsync(Tariff tariff)
        {
            // Добавляем в память
            var newId = _tariffs.Count > 0 ? _tariffs.Max(t => t.TariffID) + 1 : 1;
            tariff.TariffID = newId;
            _tariffs.Add(tariff);

            // Пытаемся сохранить в базу (не блокируем UI при ошибках)
            _ = SaveTariffToDatabaseAsync(tariff);

            await Task.CompletedTask;
        }

        private async Task SaveTariffToDatabaseAsync(Tariff tariff)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO tariffs (service_name, price_per_square_meter, price_per_person, price_per_unit)
                        VALUES (@ServiceName, @PricePerSquareMeter, @PricePerPerson, @PricePerUnit)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceName", tariff.ServiceName);
                        command.Parameters.AddWithValue("@PricePerSquareMeter", tariff.PricePerSquareMeter);
                        command.Parameters.AddWithValue("@PricePerPerson", tariff.PricePerPerson);
                        command.Parameters.AddWithValue("@PricePerUnit", tariff.PricePerUnit);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task DeleteTariffAsync(int tariffId)
        {
            // Удаляем из памяти
            var tariff = _tariffs.FirstOrDefault(t => t.TariffID == tariffId);
            if (tariff != null)
            {
                _tariffs.Remove(tariff);

                // Пытаемся удалить из базы
                _ = DeleteTariffFromDatabaseAsync(tariffId);
            }

            await Task.CompletedTask;
        }

        private async Task DeleteTariffFromDatabaseAsync(int tariffId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM tariffs WHERE tariff_id = @TariffID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariffId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        // Синхронные методы для форм
        public List<Tariff> GetTariffs()
        {
            return _tariffs;
        }

        public void AddTariff(Tariff tariff)
        {
            var newId = _tariffs.Count > 0 ? _tariffs.Max(t => t.TariffID) + 1 : 1;
            tariff.TariffID = newId;
            _tariffs.Add(tariff);
        }

        public void DeleteTariff(int tariffId)
        {
            var tariff = _tariffs.FirstOrDefault(t => t.TariffID == tariffId);
            if (tariff != null)
            {
                _tariffs.Remove(tariff);
            }
        }

        // Дополнительные методы для работы с тарифами
        public async Task UpdateTariffAsync(Tariff tariff)
        {
            // Обновляем в памяти
            var existingTariff = _tariffs.FirstOrDefault(t => t.TariffID == tariff.TariffID);
            if (existingTariff != null)
            {
                existingTariff.ServiceName = tariff.ServiceName;
                existingTariff.PricePerSquareMeter = tariff.PricePerSquareMeter;
                existingTariff.PricePerPerson = tariff.PricePerPerson;
                existingTariff.PricePerUnit = tariff.PricePerUnit;
            }

            // Обновляем в базе данных
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE tariffs 
                        SET service_name = @ServiceName,
                            price_per_square_meter = @PricePerSquareMeter,
                            price_per_person = @PricePerPerson,
                            price_per_unit = @PricePerUnit
                        WHERE tariff_id = @TariffID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariff.TariffID);
                        command.Parameters.AddWithValue("@ServiceName", tariff.ServiceName);
                        command.Parameters.AddWithValue("@PricePerSquareMeter", tariff.PricePerSquareMeter);
                        command.Parameters.AddWithValue("@PricePerPerson", tariff.PricePerPerson);
                        command.Parameters.AddWithValue("@PricePerUnit", tariff.PricePerUnit);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task<Tariff> GetTariffByIdAsync(int tariffId)
        {
            // Ищем в памяти
            var tariff = _tariffs.FirstOrDefault(t => t.TariffID == tariffId);
            if (tariff != null)
            {
                return tariff;
            }

            // Если не нашли в памяти, ищем в базе
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT * FROM tariffs WHERE tariff_id = @TariffID";
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
            catch
            {
                // Игнорируем ошибки
            }

            return null;
        }
    }
}