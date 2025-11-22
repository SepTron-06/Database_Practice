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

        public ServiceRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Основные методы работы с БД
        public async Task<List<Service>> GetServicesAsync()
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
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки услуг из базы данных: {ex.Message}", ex);
            }

            return services;
        }

        public async Task<int> AddServiceAsync(Service service)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO services (service_name, service_type)
                        VALUES (@ServiceName, @ServiceType)
                        RETURNING service_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                        command.Parameters.AddWithValue("@ServiceType", service.ServiceType);

                        var result = await command.ExecuteScalarAsync();
                        return (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления услуги в базу данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteServiceAsync(int serviceId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    // Сначала проверяем, есть ли связанные тарифы
                    var checkQuery = "SELECT COUNT(*) FROM tariffs WHERE service_id = @ServiceID";
                    using (var checkCommand = new NpgsqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@ServiceID", serviceId);
                        var tariffCount = (long)await checkCommand.ExecuteScalarAsync();

                        if (tariffCount > 0)
                        {
                            throw new System.Exception("Невозможно удалить услугу, так как с ней связаны тарифы. Сначала удалите связанные тарифы.");
                        }
                    }

                    var query = "DELETE FROM services WHERE service_id = @ServiceID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления услуги из базы данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateServiceAsync(Service service)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE services 
                        SET service_name = @ServiceName, 
                            service_type = @ServiceType
                        WHERE service_id = @ServiceID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", service.ServiceID);
                        command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                        command.Parameters.AddWithValue("@ServiceType", service.ServiceType);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка обновления услуги в базе данных: {ex.Message}", ex);
            }
        }

        public async Task<Service> GetServiceByIdAsync(int serviceId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT service_id, service_name, service_type 
                        FROM services 
                        WHERE service_id = @ServiceID";

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
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки услуги по ID: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<Service> GetServiceByNameAsync(string serviceName)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT service_id, service_name, service_type 
                        FROM services 
                        WHERE service_name = @ServiceName";

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
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки услуги по имени: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<bool> ServiceExistsAsync(int serviceId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT 1 FROM services WHERE service_id = @ServiceID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceID", serviceId);
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

        public async Task<int> GetServicesCountAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT COUNT(*) FROM services";
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
        public List<Service> GetServices()
        {
            return GetServicesAsync().GetAwaiter().GetResult();
        }

        public int AddService(Service service)
        {
            return AddServiceAsync(service).GetAwaiter().GetResult();
        }

        public bool DeleteService(int serviceId)
        {
            return DeleteServiceAsync(serviceId).GetAwaiter().GetResult();
        }

        public bool UpdateService(Service service)
        {
            return UpdateServiceAsync(service).GetAwaiter().GetResult();
        }

        public Service GetServiceById(int serviceId)
        {
            return GetServiceByIdAsync(serviceId).GetAwaiter().GetResult();
        }

        public Service GetServiceByName(string serviceName)
        {
            return GetServiceByNameAsync(serviceName).GetAwaiter().GetResult();
        }

        public bool ServiceExists(int serviceId)
        {
            return ServiceExistsAsync(serviceId).GetAwaiter().GetResult();
        }

        public int GetServicesCount()
        {
            return GetServicesCountAsync().GetAwaiter().GetResult();
        }
    }
}