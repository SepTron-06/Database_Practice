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

        public BillRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Основные методы работы с БД
        public async Task<List<Bill>> GetBillsAsync()
        {
            var bills = new List<Bill>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                SELECT 
                    b.bill_id,
                    b.address_id,
                    b.tariff_id,
                    b.amount,
                    b.issue_date,
                    b.payment_date,
                    b.is_paid,
                    a.address_id as addr_id,
                    a.street,
                    a.house,
                    a.apartment,
                    a.living_area,
                    a.residents_count,
                    t.tariff_id as tariff_id,
                    t.service_name,
                    t.price_per_square_meter,
                    t.price_per_person,
                    t.price_per_unit
                FROM bills b
                LEFT JOIN addresses a ON b.address_id = a.address_id
                LEFT JOIN tariffs t ON b.tariff_id = t.tariff_id
                ORDER BY b.issue_date DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var bill = new Bill
                            {
                                BillID = reader.GetInt32("bill_id"),
                                AddressID = reader.GetInt32("address_id"),
                                TariffID = reader.GetInt32("tariff_id"),
                                Amount = reader.GetDecimal("amount"),
                                IssueDate = reader.GetDateTime("issue_date"),
                                PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                IsPaid = reader.GetBoolean("is_paid")
                            };

                            // Заполняем объект адреса
                            if (!reader.IsDBNull(reader.GetOrdinal("addr_id")))
                            {
                                bill.Address = new Address
                                {
                                    AddressID = reader.GetInt32("addr_id"),
                                    Street = reader.GetString("street"),
                                    House = reader.GetString("house"),
                                    Apartment = reader.IsDBNull("apartment") ? null : reader.GetString("apartment"),
                                    LivingArea = reader.GetDecimal("living_area"),
                                    ResidentsCount = reader.GetInt32("residents_count")
                                };
                            }

                            // Заполняем объект тарифа
                            if (!reader.IsDBNull(reader.GetOrdinal("tariff_id")))
                            {
                                bill.Tariff = new Tariff
                                {
                                    TariffID = reader.GetInt32("tariff_id"),
                                    ServiceName = reader.GetString("service_name"),
                                    PricePerSquareMeter = reader.GetDecimal("price_per_square_meter"),
                                    PricePerPerson = reader.GetDecimal("price_per_person"),
                                    PricePerUnit = reader.GetDecimal("price_per_unit")
                                };
                            }

                            bills.Add(bill);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки счетов из базы данных: {ex.Message}", ex);
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
                        INSERT INTO bills (address_id, tariff_id, amount, issue_date, payment_date, is_paid)
                        VALUES (@AddressID, @TariffID, @Amount, @IssueDate, @PaymentDate, @IsPaid)
                        RETURNING bill_id";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", bill.AddressID);
                        command.Parameters.AddWithValue("@TariffID", bill.TariffID);
                        command.Parameters.AddWithValue("@Amount", bill.Amount);
                        command.Parameters.AddWithValue("@IssueDate", bill.IssueDate);
                        command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

                        var result = await command.ExecuteScalarAsync();
                        return (int)result;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка добавления счета в базу данных: {ex.Message}", ex);
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
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка удаления счета из базы данных: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateBillAsync(Bill bill)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        UPDATE bills 
                        SET address_id = @AddressID,
                            tariff_id = @TariffID,
                            amount = @Amount,
                            issue_date = @IssueDate,
                            payment_date = @PaymentDate,
                            is_paid = @IsPaid
                        WHERE bill_id = @BillID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", bill.BillID);
                        command.Parameters.AddWithValue("@AddressID", bill.AddressID);
                        command.Parameters.AddWithValue("@TariffID", bill.TariffID);
                        command.Parameters.AddWithValue("@Amount", bill.Amount);
                        command.Parameters.AddWithValue("@IssueDate", bill.IssueDate);
                        command.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка обновления счета в базе данных: {ex.Message}", ex);
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
                        SET is_paid = true, 
                            payment_date = @PaymentDate 
                        WHERE bill_id = @BillID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);
                        command.Parameters.AddWithValue("@PaymentDate", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка отметки счета как оплаченного: {ex.Message}", ex);
            }
        }

        public async Task<Bill> GetBillByIdAsync(int billId)
        {
            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            bill_id,
                            address_id,
                            tariff_id,
                            amount,
                            issue_date,
                            payment_date,
                            is_paid
                        FROM bills 
                        WHERE bill_id = @BillID";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BillID", billId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Bill
                                {
                                    BillID = reader.GetInt32("bill_id"),
                                    AddressID = reader.GetInt32("address_id"),
                                    TariffID = reader.GetInt32("tariff_id"),
                                    Amount = reader.GetDecimal("amount"),
                                    IssueDate = reader.GetDateTime("issue_date"),
                                    PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                    IsPaid = reader.GetBoolean("is_paid")
                                };
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки счета по ID: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<List<Bill>> GetUnpaidBillsAsync()
        {
            var bills = new List<Bill>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            bill_id,
                            address_id,
                            tariff_id,
                            amount,
                            issue_date,
                            payment_date,
                            is_paid
                        FROM bills 
                        WHERE is_paid = false
                        ORDER BY issue_date";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            bills.Add(new Bill
                            {
                                BillID = reader.GetInt32("bill_id"),
                                AddressID = reader.GetInt32("address_id"),
                                TariffID = reader.GetInt32("tariff_id"),
                                Amount = reader.GetDecimal("amount"),
                                IssueDate = reader.GetDateTime("issue_date"),
                                PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                IsPaid = reader.GetBoolean("is_paid")
                            });
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки неоплаченных счетов: {ex.Message}", ex);
            }

            return bills;
        }

        public async Task<List<Bill>> GetBillsByAddressAsync(int addressId)
        {
            var bills = new List<Bill>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            bill_id,
                            address_id,
                            tariff_id,
                            amount,
                            issue_date,
                            payment_date,
                            is_paid
                        FROM bills 
                        WHERE address_id = @AddressID
                        ORDER BY issue_date DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AddressID", addressId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bills.Add(new Bill
                                {
                                    BillID = reader.GetInt32("bill_id"),
                                    AddressID = reader.GetInt32("address_id"),
                                    TariffID = reader.GetInt32("tariff_id"),
                                    Amount = reader.GetDecimal("amount"),
                                    IssueDate = reader.GetDateTime("issue_date"),
                                    PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                    IsPaid = reader.GetBoolean("is_paid")
                                });
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки счетов по адресу: {ex.Message}", ex);
            }

            return bills;
        }

        public async Task<List<Bill>> GetBillsByTariffAsync(int tariffId)
        {
            var bills = new List<Bill>();

            try
            {
                using (var connection = await _dbConnection.GetConnectionAsync())
                {
                    var query = @"
                        SELECT 
                            bill_id,
                            address_id,
                            tariff_id,
                            amount,
                            issue_date,
                            payment_date,
                            is_paid
                        FROM bills 
                        WHERE tariff_id = @TariffID
                        ORDER BY issue_date DESC";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TariffID", tariffId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bills.Add(new Bill
                                {
                                    BillID = reader.GetInt32("bill_id"),
                                    AddressID = reader.GetInt32("address_id"),
                                    TariffID = reader.GetInt32("tariff_id"),
                                    Amount = reader.GetDecimal("amount"),
                                    IssueDate = reader.GetDateTime("issue_date"),
                                    PaymentDate = reader.IsDBNull("payment_date") ? null : reader.GetDateTime("payment_date"),
                                    IsPaid = reader.GetBoolean("is_paid")
                                });
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Ошибка загрузки счетов по тарифу: {ex.Message}", ex);
            }

            return bills;
        }

        // Синхронные методы для обратной совместимости
        public List<Bill> GetBills()
        {
            return GetBillsAsync().GetAwaiter().GetResult();
        }

        public int AddBill(Bill bill)
        {
            return AddBillAsync(bill).GetAwaiter().GetResult();
        }

        public bool DeleteBill(int billId)
        {
            return DeleteBillAsync(billId).GetAwaiter().GetResult();
        }

        public bool UpdateBill(Bill bill)
        {
            return UpdateBillAsync(bill).GetAwaiter().GetResult();
        }

        public bool MarkBillAsPaid(int billId)
        {
            return MarkBillAsPaidAsync(billId).GetAwaiter().GetResult();
        }

        public Bill GetBillById(int billId)
        {
            return GetBillByIdAsync(billId).GetAwaiter().GetResult();
        }

        public List<Bill> GetUnpaidBills()
        {
            return GetUnpaidBillsAsync().GetAwaiter().GetResult();
        }

        public List<Bill> GetBillsByAddress(int addressId)
        {
            return GetBillsByAddressAsync(addressId).GetAwaiter().GetResult();
        }

        public List<Bill> GetBillsByTariff(int tariffId)
        {
            return GetBillsByTariffAsync(tariffId).GetAwaiter().GetResult();
        }
    }
}