using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class ServiceRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private List<Service> _services;

        public ServiceRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _services = new List<Service>();

            // Инициализируем данные при создании репозитория
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // Сначала пытаемся загрузить из базы данных
                var servicesFromDb = await LoadServicesFromDatabaseAsync();
                if (servicesFromDb.Any())
                {
                    _services = servicesFromDb;
                }
                else
                {
                    // Если в базе нет данных, создаем демо-данные и сохраняем в базу
                    _services = GetDefaultServices();
                    await SaveDefaultServicesToDatabaseAsync();
                }
            }
            catch
            {
                // Если ошибка базы, используем только in-memory данные
                _services = GetDefaultServices();
            }
        }

        private List<Service> GetDefaultServices()
        {
            return new List<Service>
            {
                new Service { ServiceID = 1, ServiceName = "Подача холодной воды", ServiceType = "Водоснабжение" },
                new Service { ServiceID = 2, ServiceName = "Подача горячей воды", ServiceType = "Водоснабжение" },
                new Service { ServiceID = 3, ServiceName = "Подача газа в квартиру", ServiceType = "Газоснабжение" },
                new Service { ServiceID = 4, ServiceName = "Подача электричества в квартиру", ServiceType = "Электроснабжение" },
                new Service { ServiceID = 5, ServiceName = "Взнос на капитальный ремонт дома", ServiceType = "Капитальный ремонт" },
                new Service { ServiceID = 6, ServiceName = "Обращение с ТКО", ServiceType = "Вывоз мусора" },
                new Service { ServiceID = 7, ServiceName = "Отопление", ServiceType = "Теплоснабжение" },
                new Service { ServiceID = 8, ServiceName = "Электронное запирающее устройство", ServiceType = "Электроснабжение" }
            };
        }

        private async Task<List<Service>> LoadServicesFromDatabaseAsync()
        {
            var services = new List<Service>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT service_id, service_name, service_type 
                        FROM services 
                        ORDER BY service_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            services.Add(new Service
                            {
                                ServiceID = reader.GetInt32("service_id"),
                                ServiceName = reader.GetString("service_name"),
                                ServiceType = reader.GetString("service_type")
                            });
                        }
                    }
                }
            }
            catch
            {
                // Возвращаем пустой список при ошибке
            }

            return services;
        }

        private async Task SaveDefaultServicesToDatabaseAsync()
        {
            try
            {
                // Убрали создание таблицы - предполагаем, что она уже создана в MainForm
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    foreach (var service in _services)
                    {
                        // Используем INSERT с обработкой конфликтов (если услуга уже существует)
                        var query = @"
                            INSERT INTO services (service_name, service_type)
                            VALUES (@ServiceName, @ServiceType)
                            ON CONFLICT (service_name) DO NOTHING";

                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                            command.Parameters.AddWithValue("@ServiceType", service.ServiceType);
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

        public async Task<List<Service>> GetServicesAsync()
        {
            // Возвращаем данные из памяти (быстро)
            return await Task.FromResult(_services);
        }

        public async Task AddServiceAsync(Service service)
        {
            // Добавляем в память
            var newId = _services.Count > 0 ? _services.Max(s => s.ServiceID) + 1 : 1;
            service.ServiceID = newId;
            _services.Add(service);

            // Сохраняем в базу данных (асинхронно, не блокируем UI)
            _ = SaveServiceToDatabaseAsync(service);

            await Task.CompletedTask;
        }

        private async Task SaveServiceToDatabaseAsync(Service service)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO services (service_name, service_type)
                        VALUES (@ServiceName, @ServiceType)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                        command.Parameters.AddWithValue("@ServiceType", service.ServiceType);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task DeleteServiceAsync(int serviceId)
        {
            // Удаляем из памяти
            var service = _services.FirstOrDefault(s => s.ServiceID == serviceId);
            if (service != null)
            {
                _services.Remove(service);

                // Удаляем из базы данных (асинхронно)
                _ = DeleteServiceFromDatabaseAsync(serviceId);
            }

            await Task.CompletedTask;
        }

        private async Task DeleteServiceFromDatabaseAsync(int serviceId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM services WHERE service_id = @ServiceID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        // Синхронные методы для использования в формах
        public List<Service> GetServices()
        {
            return _services;
        }

        public void AddService(Service service)
        {
            var newId = _services.Count > 0 ? _services.Max(s => s.ServiceID) + 1 : 1;
            service.ServiceID = newId;
            _services.Add(service);

            // Асинхронное сохранение в базу
            _ = SaveServiceToDatabaseAsync(service);
        }

        public void DeleteService(int serviceId)
        {
            var service = _services.FirstOrDefault(s => s.ServiceID == serviceId);
            if (service != null)
            {
                _services.Remove(service);

                // Асинхронное удаление из базы
                _ = DeleteServiceFromDatabaseAsync(serviceId);
            }
        }

        // Дополнительные методы для работы с базой данных
        public async Task UpdateServiceAsync(Service service)
        {
            // Обновляем в памяти
            var existingService = _services.FirstOrDefault(s => s.ServiceID == service.ServiceID);
            if (existingService != null)
            {
                existingService.ServiceName = service.ServiceName;
                existingService.ServiceType = service.ServiceType;
            }

            // Обновляем в базе данных
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE services 
                        SET service_name = @ServiceName, service_type = @ServiceType
                        WHERE service_id = @ServiceID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", service.ServiceID);
                        command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                        command.Parameters.AddWithValue("@ServiceType", service.ServiceType);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task<Service> GetServiceByIdAsync(int serviceId)
        {
            // Ищем в памяти
            var service = _services.FirstOrDefault(s => s.ServiceID == serviceId);
            if (service != null)
            {
                return service;
            }

            // Если не нашли в памяти, ищем в базе
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT * FROM services WHERE service_id = @ServiceID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Service
                                {
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    ServiceType = reader.GetString("service_type")
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

        // Метод для получения услуги по имени (полезно для тарифов)
        public async Task<Service> GetServiceByNameAsync(string serviceName)
        {
            // Ищем в памяти
            var service = _services.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service != null)
            {
                return service;
            }

            // Если не нашли в памяти, ищем в базе
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT * FROM services WHERE service_name = @ServiceName";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceName", serviceName);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Service
                                {
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    ServiceType = reader.GetString("service_type")
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