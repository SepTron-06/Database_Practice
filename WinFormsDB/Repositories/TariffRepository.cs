using WinFormsDB.Data;
using WinFormsDB.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinFormsDB.Repositories
{
    public class TariffRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private List<Tariff> _tariffs;

        public TariffRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _tariffs = new List<Tariff>
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

        public async Task<List<Tariff>> GetTariffsAsync()
        {
            return await Task.FromResult(_tariffs);
        }

        public async Task AddTariffAsync(Tariff tariff)
        {
            var newId = _tariffs.Count > 0 ? _tariffs.Max(t => t.TariffID) + 1 : 1;
            tariff.TariffID = newId;
            _tariffs.Add(tariff);
            await Task.CompletedTask;
        }

        public async Task DeleteTariffAsync(int tariffId)
        {
            var tariff = _tariffs.FirstOrDefault(t => t.TariffID == tariffId);
            if (tariff != null)
            {
                _tariffs.Remove(tariff);
            }
            await Task.CompletedTask;
        }
    }
}