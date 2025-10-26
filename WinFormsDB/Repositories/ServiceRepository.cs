using WinFormsDB.Data;
using WinFormsDB.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinFormsDB.Repositories
{
    public class ServiceRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private List<Service> _services;

        public ServiceRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _services = new List<Service>
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

        public async Task<List<Service>> GetServicesAsync()
        {
            return await Task.FromResult(_services);
        }

        public async Task AddServiceAsync(Service service)
        {
            var newId = _services.Count > 0 ? _services.Max(s => s.ServiceID) + 1 : 1;
            service.ServiceID = newId;
            _services.Add(service);
            await Task.CompletedTask;
        }

        public async Task DeleteServiceAsync(int serviceId)
        {
            var service = _services.FirstOrDefault(s => s.ServiceID == serviceId);
            if (service != null)
            {
                _services.Remove(service);
            }
            await Task.CompletedTask;
        }
    }
}