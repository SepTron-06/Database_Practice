using WinFormsDB.Data;
using WinFormsDB.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinFormsDB.Services
{
    public class ReportService
    {
        private readonly DatabaseConnection _dbConnection;

        public ReportService(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<BillWithDetails>> GetUnpaidBillsByClient(int clientId)
        {
            // Временная реализация - возвращаем тестовые данные
            return await Task.FromResult(new List<BillWithDetails>
            {
                new BillWithDetails
                {
                    BillID = 1,
                    ClientName = "Иванов Иван",
                    ServiceName = "Подача холодной воды",
                    Amount = 1500.50m,
                    PaymentDate = System.DateTime.Now.AddDays(-10),
                    IsPaid = false
                },
                new BillWithDetails
                {
                    BillID = 2,
                    ClientName = "Иванов Иван",
                    ServiceName = "Отопление",
                    Amount = 3200.00m,
                    PaymentDate = System.DateTime.Now.AddDays(-5),
                    IsPaid = false
                }
            });
        }
    }
}