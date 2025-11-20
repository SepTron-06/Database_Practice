using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class BillRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly AddressRepository _addressRepository;
        private readonly ServiceRepository _serviceRepository;

        public BillRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _addressRepository = new AddressRepository(dbConnection, new ClientRepository(dbConnection));
            _serviceRepository = new ServiceRepository(dbConnection);
        }

        public async Task InitializeSampleDataAsync()
        {
            try
            {
                // Проверяем, есть ли уже счета
                var existingBills = await GetBillsAsync();
                if (existingBills.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Тестовые счета уже существуют, пропускаем создание");
                    return;
                }

                // Получаем адреса
                var addresses = _addressRepository.GetAddresses();
                if (addresses.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine("Недостаточно адресов для создания тестовых счетов");
                    return;
                }

                // Получаем услуги
                var services = _serviceRepository.GetServices();
                if (services.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine("Недостаточно услуг для создания тестовых счетов");
                    return;
                }

                // Создаем список тестовых счетов
                var sampleBills = new List<Bill>
                {
                    new Bill
                    {
                        AddressID = addresses[0].AddressID,
                        ServiceID = services[0].ServiceID,
                        Amount = 2500.75m,
                        IssueDate = new System.DateTime(2001, 8, 1),
                        PaymentDate = new System.DateTime(2001, 9, 11),
                        IsPaid = true
                    },
                    new Bill
                    {
                        AddressID = addresses[1].AddressID,
                        ServiceID = services[1].ServiceID,
                        Amount = 1800.50m,
                        IssueDate = new System.DateTime(2001, 9, 1),
                        PaymentDate = null,
                        IsPaid = false
                    }
                };

                System.Diagnostics.Debug.WriteLine($"Создаем {sampleBills.Count} тестовых счета...");

                // Добавляем все счета из списка
                foreach (var bill in sampleBills)
                {
                    try
                    {
                        var newBillId = await AddBillAsync(bill);
                        System.Diagnostics.Debug.WriteLine($"Создан счет ID: {newBillId}");
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка создания счета: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("Тестовые счета успешно созданы");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания тестовых счетов: {ex.Message}");
                // Не бросаем исключение, чтобы не ломать приложение
            }
        }

        public async Task<List<Bill>> GetBillsAsync()
        {
            var bills = new List<Bill>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            b.bill_id as BillID,
                            b.address_id as AddressID,
                            b.service_id as ServiceID,
                            b.amount as Amount,
                            b.issue_date as IssueDate,
                            b.payment_date as PaymentDate,
                            b.is_paid as IsPaid,
                            a.address_id,
                            a.street as Street,
                            a.house as House,
                            a.apartment as Apartment,
                            a.living_area as LivingArea,
                            a.residents_count as ResidentsCount,
                            s.service_id,
                            s.service_name as ServiceName,
                            s.service_type as ServiceType,
                            c.client_id,
                            c.first_name as FirstName,
                            c.last_name as LastName
                        FROM bills b
                        LEFT JOIN addresses a ON b.address_id = a.address_id
                        LEFT JOIN services s ON b.service_id = s.service_id
                        LEFT JOIN clients c ON a.client_id = c.client_id
                        ORDER BY b.issue_date DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var bill = new Bill
                            {
                                BillID = reader.GetInt32("BillID"),
                                AddressID = reader.GetInt32("AddressID"),
                                ServiceID = reader.GetInt32("ServiceID"),
                                Amount = reader.GetDecimal("Amount"),
                                IssueDate = reader.GetDateTime("IssueDate"),
                                PaymentDate = reader.IsDBNull("PaymentDate") ? null : reader.GetDateTime("PaymentDate"),
                                IsPaid = reader.GetBoolean("IsPaid"),
                                Address = new Address
                                {
                                    AddressID = reader.GetInt32("address_id"),
                                    Street = reader.GetString("Street"),
                                    House = reader.GetString("House"),
                                    Apartment = reader.IsDBNull("Apartment") ? null : reader.GetString("Apartment"),
                                    LivingArea = reader.GetDecimal("LivingArea"),
                                    ResidentsCount = reader.GetInt32("ResidentsCount"),
                                    Client = new Client
                                    {
                                        ClientID = reader.GetInt32("client_id"),
                                        FirstName = reader.GetString("FirstName"),
                                        LastName = reader.GetString("LastName")
                                    }
                                },
                                Service = new Service
                                {
                                    ServiceID = reader.GetInt32("service_id"),
                                    ServiceName = reader.GetString("ServiceName"),
                                    ServiceType = reader.GetString("ServiceType")
                                }
                            };
                            bills.Add(bill);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетов: {ex.Message}");
            }

            return bills;
        }

        public async Task<int> AddBillAsync(Bill bill)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO bills (address_id, service_id, amount, issue_date, payment_date, is_paid)
                        VALUES (@AddressID, @ServiceID, @Amount, @IssueDate, @PaymentDate, @IsPaid)
                        RETURNING bill_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", bill.AddressID);
                        command.Parameters.AddWithValue("@ServiceID", bill.ServiceID);
                        command.Parameters.AddWithValue("@Amount", bill.Amount);
                        command.Parameters.AddWithValue("@IssueDate", bill.IssueDate);
                        command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

                        var newId = await command.ExecuteScalarAsync();
                        return (int)(long)newId;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления счета: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteBillAsync(int billId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM bills WHERE bill_id = @BillID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления счета: {ex.Message}", ex);
            }
        }

        public async Task<bool> MarkBillAsPaidAsync(int billId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE bills 
                        SET is_paid = true, payment_date = @PaymentDate 
                        WHERE bill_id = @BillID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);
                        command.Parameters.AddWithValue("@PaymentDate", System.DateTime.Now);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка отметки счета как оплаченного: {ex.Message}", ex);
            }
        }

        // Синхронные методы для обратной совместимости
        public List<Bill> GetBills() => GetBillsAsync().GetAwaiter().GetResult();

        public void AddBill(Bill bill) => AddBillAsync(bill).GetAwaiter().GetResult();

        public void DeleteBill(int billId) => DeleteBillAsync(billId).GetAwaiter().GetResult();

        public void InitializeSampleData() => InitializeSampleDataAsync().GetAwaiter().GetResult();
    }
}