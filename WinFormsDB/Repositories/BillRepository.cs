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
        private readonly TariffRepository _tariffRepository;
        private readonly ClientRepository _clientRepository;
        private List<Bill> _bills;

        public BillRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _clientRepository = new ClientRepository(dbConnection);
            _addressRepository = new AddressRepository(dbConnection, _clientRepository);
            _tariffRepository = new TariffRepository(dbConnection);
            _bills = new List<Bill>();

            // Загружаем данные из базы при создании репозитория
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                var billsFromDb = await LoadBillsFromDatabaseAsync();
                if (billsFromDb.Any())
                {
                    _bills = billsFromDb;
                }
                else
                {
                    // Если в базе нет данных, используем демо-данные
                    _bills = GetDefaultBills();
                    await SaveDefaultBillsToDatabaseAsync();
                }
            }
            catch
            {
                // Если ошибка базы, используем только in-memory
                _bills = GetDefaultBills();
            }
        }

        private List<Bill> GetDefaultBills()
        {
            return new List<Bill>
            {
                new Bill
                {
                    BillID = 1,
                    AddressID = 1,
                    TariffID = 1,
                    Amount = 34.00m,
                    IssueDate = System.DateTime.Now,
                    PaymentDate = null,
                    IsPaid = false
                },
                new Bill
                {
                    BillID = 2,
                    AddressID = 2,
                    TariffID = 2,
                    Amount = 7005.00m,
                    IssueDate = System.DateTime.Now,
                    PaymentDate = new System.DateTime(2025, 11, 24),
                    IsPaid = true
                }
            };
        }

        private async Task<List<Bill>> LoadBillsFromDatabaseAsync()
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
                    b.tariff_id as TariffID,
                    b.amount as Amount,
                    b.issue_date as IssueDate,
                    b.payment_date as PaymentDate,
                    b.is_paid as IsPaid
                FROM bills b
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
                                TariffID = reader.GetInt32("TariffID"),
                                Amount = reader.GetDecimal("Amount"),
                                IssueDate = reader.GetDateTime("IssueDate"),
                                PaymentDate = reader.IsDBNull("PaymentDate") ? null : reader.GetDateTime("PaymentDate"),
                                IsPaid = reader.GetBoolean("IsPaid")
                            };

                            // ВАЖНО: загружаем связанные данные
                            await LoadRelatedDataForDisplayAsync(bill);

                            bills.Add(bill);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетов из БД: {ex.Message}");
            }

            return bills;
        }
        private async Task LoadRelatedDataForDisplayAsync(Bill bill)
        {
            try
            {
                // Используем асинхронные методы для загрузки данных
                var addresses = await _addressRepository.GetAddressesAsync();
                var address = addresses.FirstOrDefault(a => a.AddressID == bill.AddressID);

                if (address != null)
                {
                    bill.Address = address;

                    // Если клиент не загружен, загружаем его
                    if (address.Client == null && address.ClientID > 0)
                    {
                        var clients = await _clientRepository.GetClientsAsync();
                        address.Client = clients.FirstOrDefault(c => c.ClientID == address.ClientID);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Адрес с ID {bill.AddressID} не найден для счета {bill.BillID}");
                }

                // Загружаем тариф асинхронно
                var tariffs = await _tariffRepository.GetTariffsAsync();
                bill.Tariff = tariffs.FirstOrDefault(t => t.TariffID == bill.TariffID);

                if (bill.Tariff == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Тариф с ID {bill.TariffID} не найден для счета {bill.BillID}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки связанных данных для счета {bill.BillID}: {ex.Message}");
            }
        }

        private async Task SaveDefaultBillsToDatabaseAsync()
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    foreach (var bill in _bills)
                    {
                        // Проверяем существование адреса и тарифа перед сохранением
                        var addressExists = await CheckAddressExistsAsync(bill.AddressID);
                        var tariffExists = await CheckTariffExistsAsync(bill.TariffID);

                        if (!addressExists || !tariffExists)
                        {
                            System.Diagnostics.Debug.WriteLine($"Пропускаем счет {bill.BillID}: адрес или тариф не существует");
                            continue;
                        }

                        var query = @"
                            INSERT INTO bills (address_id, tariff_id, amount, issue_date, payment_date, is_paid)
                            VALUES (@AddressID, @TariffID, @Amount, @IssueDate, @PaymentDate, @IsPaid)";

                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@AddressID", bill.AddressID);
                            command.Parameters.AddWithValue("@TariffID", bill.TariffID);
                            command.Parameters.AddWithValue("@Amount", bill.Amount);
                            command.Parameters.AddWithValue("@IssueDate", bill.IssueDate);
                            command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

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

        private async Task<bool> CheckAddressExistsAsync(int addressId)
        {
            try
            {
                var addresses = _addressRepository.GetAddresses();
                return addresses.Any(a => a.AddressID == addressId);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckTariffExistsAsync(int tariffId)
        {
            try
            {
                var tariffs = _tariffRepository.GetTariffs();
                return tariffs.Any(t => t.TariffID == tariffId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Bill>> GetBillsAsync()
        {
            // Всегда возвращаем актуальные данные из памяти
            return await Task.FromResult(_bills);
        }

        public async Task<int> AddBillAsync(Bill bill)
        {
            try
            {
                // Проверяем существование адреса и тарифа
                var addressExists = await CheckAddressExistsAsync(bill.AddressID);
                var tariffExists = await CheckTariffExistsAsync(bill.TariffID);

                if (!addressExists)
                    throw new System.Exception($"Адрес с ID {bill.AddressID} не существует");

                if (!tariffExists)
                    throw new System.Exception($"Тариф с ID {bill.TariffID} не существует");

                // Добавляем в память
                var newId = _bills.Count > 0 ? _bills.Max(b => b.BillID) + 1 : 1;
                bill.BillID = newId;

                // Загружаем связанные данные для отображения
                await LoadRelatedDataForDisplayAsync(bill);

                _bills.Add(bill);

                // Пытаемся сохранить в базу (не блокируем UI при ошибках)
                _ = SaveBillToDatabaseAsync(bill);

                return newId;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления счета: {ex.Message}", ex);
            }
        }

        private async Task SaveBillToDatabaseAsync(Bill bill)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        INSERT INTO bills (address_id, tariff_id, amount, issue_date, payment_date, is_paid)
                        VALUES (@AddressID, @TariffID, @Amount, @IssueDate, @PaymentDate, @IsPaid)";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", bill.AddressID);
                        command.Parameters.AddWithValue("@TariffID", bill.TariffID);
                        command.Parameters.AddWithValue("@Amount", bill.Amount);
                        command.Parameters.AddWithValue("@IssueDate", bill.IssueDate);
                        command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task<bool> DeleteBillAsync(int billId)
        {
            try
            {
                // Удаляем из памяти
                var bill = _bills.FirstOrDefault(b => b.BillID == billId);
                if (bill != null)
                {
                    _bills.Remove(bill);

                    // Пытаемся удалить из базы
                    _ = DeleteBillFromDatabaseAsync(billId);
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления счета: {ex.Message}", ex);
            }
        }

        private async Task DeleteBillFromDatabaseAsync(int billId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "DELETE FROM bills WHERE bill_id = @BillID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        public async Task<bool> MarkBillAsPaidAsync(int billId)
        {
            try
            {
                // Обновляем в памяти
                var bill = _bills.FirstOrDefault(b => b.BillID == billId);
                if (bill != null)
                {
                    bill.IsPaid = true;
                    bill.PaymentDate = System.DateTime.Now;

                    // Обновляем в базе данных
                    await UpdateBillInDatabaseAsync(bill);
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка отметки счета как оплаченного: {ex.Message}", ex);
            }
        }

        private async Task UpdateBillInDatabaseAsync(Bill bill)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE bills 
                        SET is_paid = @IsPaid, payment_date = @PaymentDate 
                        WHERE bill_id = @BillID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", bill.BillID);
                        command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);
                        command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки базы данных
            }
        }

        // Синхронные методы для обратной совместимости
        public List<Bill> GetBills() => _bills;
        public int AddBill(Bill bill) => AddBillAsync(bill).GetAwaiter().GetResult();
        public void DeleteBill(int billId) => DeleteBillAsync(billId).GetAwaiter().GetResult();
        public void MarkBillAsPaid(int billId) => MarkBillAsPaidAsync(billId).GetAwaiter().GetResult();

        // Дополнительные методы для работы со счетами
        public async Task<Bill> GetBillByIdAsync(int billId)
        {
            // Ищем в памяти
            var bill = _bills.FirstOrDefault(b => b.BillID == billId);
            if (bill != null)
            {
                return bill;
            }

            // Если не нашли в памяти, ищем в базе
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = "SELECT * FROM bills WHERE bill_id = @BillID";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var dbBill = new Bill
                                {
                                    BillID = reader.GetInt32("bill_id"),
                                    AddressID = reader.GetInt32("address_id"),
                                    TariffID = reader.GetInt32("tariff_id"),
                                    Amount = reader.GetDecimal("amount"),
                                    IssueDate = reader.GetDateTime("issue_date"),
                                    PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                    IsPaid = reader.GetBoolean("is_paid")
                                };

                                // Загружаем связанные данные
                                await LoadRelatedDataForDisplayAsync(dbBill);

                                // Добавляем в память для будущих запросов
                                _bills.Add(dbBill);

                                return dbBill;
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

        public async Task<List<Bill>> GetUnpaidBillsAsync()
        {
            var unpaidBills = _bills.Where(b => !b.IsPaid).ToList();
            return await Task.FromResult(unpaidBills);
        }

        public async Task<List<Bill>> GetBillsByAddressAsync(int addressId)
        {
            var addressBills = _bills.Where(b => b.AddressID == addressId).ToList();
            return await Task.FromResult(addressBills);
        }

        public async Task<List<Bill>> GetBillsByTariffAsync(int tariffId)
        {
            var tariffBills = _bills.Where(b => b.TariffID == tariffId).ToList();
            return await Task.FromResult(tariffBills);
        }

        // Метод для обратной совместимости
        public async Task InitializeSampleDataAsync()
        {
            await InitializeDataAsync();
        }

        public void InitializeSampleData() => InitializeSampleDataAsync().GetAwaiter().GetResult();
    }
}