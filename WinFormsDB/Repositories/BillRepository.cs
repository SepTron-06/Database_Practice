using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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

        public async Task<int> AddBillAsync(Bill bill)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            INSERT INTO Bills (AddressID, TariffID, ConsumedVolume, Amount, PaymentDate, IsPaid)
            VALUES (@AddressID, @TariffID, @ConsumedVolume, @Amount, @PaymentDate, @IsPaid)
            RETURNING BillID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", bill.AddressID);
            cmd.Parameters.AddWithValue("@TariffID", bill.TariffID);
            cmd.Parameters.AddWithValue("@ConsumedVolume", bill.ConsumedVolume);
            cmd.Parameters.AddWithValue("@Amount", bill.Amount);
            cmd.Parameters.AddWithValue("@PaymentDate", bill.PaymentDate);
            cmd.Parameters.AddWithValue("@IsPaid", bill.IsPaid);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<Bill>> GetBillsByAddressAsync(int addressId)
        {
            var bills = new List<Bill>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = "SELECT * FROM Bills WHERE AddressID = @AddressID ORDER BY PaymentDate DESC";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", addressId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bills.Add(new Bill
                {
                    BillID = reader.GetInt32("BillID"),
                    AddressID = reader.GetInt32("AddressID"),
                    TariffID = reader.GetInt32("TariffID"),
                    ConsumedVolume = reader.GetDecimal("ConsumedVolume"),
                    Amount = reader.GetDecimal("Amount"),
                    PaymentDate = reader.GetDateTime("PaymentDate"),
                    IsPaid = reader.GetBoolean("IsPaid")
                });
            }

            return bills;
        }

        public async Task<List<BillWithDetails>> GetBillsWithDetailsByAddressAsync(int addressId)
        {
            var bills = new List<BillWithDetails>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = @"
            SELECT b.*, t.TariffName, s.ServiceName, a.Street, a.House, a.Apartment
            FROM Bills b
            JOIN Tariffs t ON b.TariffID = t.TariffID
            JOIN Services s ON t.ServiceID = s.ServiceID
            JOIN Addresses a ON b.AddressID = a.AddressID
            WHERE b.AddressID = @AddressID
            ORDER BY b.PaymentDate DESC";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", addressId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                bills.Add(new BillWithDetails
                {
                    BillID = reader.GetInt32("BillID"),
                    AddressID = reader.GetInt32("AddressID"),
                    TariffID = reader.GetInt32("TariffID"),
                    ConsumedVolume = reader.GetDecimal("ConsumedVolume"),
                    Amount = reader.GetDecimal("Amount"),
                    PaymentDate = reader.GetDateTime("PaymentDate"),
                    IsPaid = reader.GetBoolean("IsPaid"),
                    TariffName = reader.GetString("TariffName"),
                    ServiceName = reader.GetString("ServiceName"),
                    Street = reader.GetString("Street"),
                    House = reader.GetString("House"),
                    Apartment = reader.IsDBNull("Apartment") ? null : reader.GetString("Apartment")
                });
            }

            return bills;
        }

        public async Task MarkBillAsPaidAsync(int billId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = "UPDATE Bills SET IsPaid = true WHERE BillID = @BillID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@BillID", billId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
