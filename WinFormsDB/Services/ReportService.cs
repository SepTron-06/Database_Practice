using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Services
{
    public class ReportService
    {
        private readonly DatabaseConnection _dbConnection;

        public ReportService(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Асинхронный метод
        public async Task<List<BillReport>> GetUnpaidBillsByClientAsync(int clientId)
        {
            var unpaidBills = new List<BillReport>();

            using (var connection = await _dbConnection.GetConnectionAsync())
            {
                var query = @"
                    SELECT b.BillID, s.ServiceName, b.Amount, b.PaymentDate, b.IsPaid
                    FROM Bills b
                    INNER JOIN Services s ON b.ServiceID = s.ServiceID
                    WHERE b.ClientID = @ClientID AND b.IsPaid = false";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClientID", clientId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            unpaidBills.Add(new BillReport
                            {
                                BillID = reader.GetInt32("BillID"),
                                ServiceName = reader.GetString("ServiceName"),
                                Amount = reader.GetDecimal("Amount"),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                IsPaid = reader.GetBoolean("IsPaid")
                            });
                        }
                    }
                }
            }

            return unpaidBills;
        }

        // Синхронный метод для обратной совместимости
        public List<BillReport> GetUnpaidBillsByClient(int clientId)
        {
            return GetUnpaidBillsByClientAsync(clientId).GetAwaiter().GetResult();
        }
    }

    public class BillReport
    {
        public int BillID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsPaid { get; set; }
    }
}