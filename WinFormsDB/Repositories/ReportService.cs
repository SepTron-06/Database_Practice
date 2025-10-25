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
    public class ReportService
    {
        private readonly DatabaseConnection _dbConnection;

        public ReportService(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<decimal> GetTotalAmountByClient(int clientId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            SELECT COALESCE(SUM(b.Amount), 0) as TotalAmount
            FROM Bills b
            JOIN Addresses a ON b.AddressID = a.AddressID
            WHERE a.ClientID = @ClientID AND b.IsPaid = true";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", clientId);

            var result = await cmd.ExecuteScalarAsync();
            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        public async Task<List<BillWithDetails>> GetUnpaidBillsByClient(int clientId)
        {
            var bills = new List<BillWithDetails>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = @"
            SELECT b.*, t.TariffName, s.ServiceName, a.Street, a.House, a.Apartment
            FROM Bills b
            JOIN Addresses a ON b.AddressID = a.AddressID
            JOIN Tariffs t ON b.TariffID = t.TariffID
            JOIN Services s ON t.ServiceID = s.ServiceID
            WHERE a.ClientID = @ClientID AND b.IsPaid = false
            ORDER BY b.PaymentDate";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", clientId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    var bill = new BillWithDetails
                    {
                        BillID = GetSafeInt32(reader, "BillID"),
                        AddressID = GetSafeInt32(reader, "AddressID"),
                        TariffID = GetSafeInt32(reader, "TariffID"),
                        ConsumedVolume = GetSafeDecimal(reader, "ConsumedVolume"),
                        Amount = GetSafeDecimal(reader, "Amount"),
                        PaymentDate = GetSafeDateTime(reader, "PaymentDate"),
                        IsPaid = GetSafeBoolean(reader, "IsPaid"),
                        TariffName = GetSafeString(reader, "TariffName"),
                        ServiceName = GetSafeString(reader, "ServiceName"),
                        Street = GetSafeString(reader, "Street"),
                        House = GetSafeString(reader, "House"),
                        Apartment = GetSafeString(reader, "Apartment")
                    };
                    bills.Add(bill);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении счета: {ex.Message}");
                }
            }

            return bills;
        }

        // Вспомогательные методы для безопасного чтения
        private string GetSafeString(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private int GetSafeInt32(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetSafeDecimal(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetDecimal(ordinal);
            }
            catch
            {
                return 0;
            }
        }

        private DateTime GetSafeDateTime(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private bool GetSafeBoolean(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? false : reader.GetBoolean(ordinal);
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<BillWithDetails>> GetBillsByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var bills = new List<BillWithDetails>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = @"
            SELECT b.*, t.TariffName, s.ServiceName, a.Street, a.House, a.Apartment,
                   c.FirstName, c.LastName
            FROM Bills b
            JOIN Addresses a ON b.AddressID = a.AddressID
            JOIN Clients c ON a.ClientID = c.ClientID
            JOIN Tariffs t ON b.TariffID = t.TariffID
            JOIN Services s ON t.ServiceID = s.ServiceID
            WHERE b.PaymentDate BETWEEN @StartDate AND @EndDate
            ORDER BY b.PaymentDate, c.LastName";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@StartDate", startDate);
            cmd.Parameters.AddWithValue("@EndDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var bill = new BillWithDetails
                {
                    BillID = GetSafeInt32(reader, "BillID"),
                    AddressID = GetSafeInt32(reader, "AddressID"),
                    TariffID = GetSafeInt32(reader, "TariffID"),
                    ConsumedVolume = GetSafeDecimal(reader, "ConsumedVolume"),
                    Amount = GetSafeDecimal(reader, "Amount"),
                    PaymentDate = GetSafeDateTime(reader, "PaymentDate"),
                    IsPaid = GetSafeBoolean(reader, "IsPaid"),
                    TariffName = GetSafeString(reader, "TariffName"),
                    ServiceName = GetSafeString(reader, "ServiceName"),
                    Street = GetSafeString(reader, "Street"),
                    House = GetSafeString(reader, "House"),
                    Apartment = GetSafeString(reader, "Apartment")
                };
                bills.Add(bill);
            }

            return bills;
        }


    }
}
