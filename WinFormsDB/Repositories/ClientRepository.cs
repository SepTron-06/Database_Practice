using WinFormsDB.Data;
using WinFormsDB.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinFormsDB.Repositories
{
    public class ClientRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private List<Client> _clients;

        public ClientRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _clients = new List<Client>
            {
                new Client { ClientID = 1, FirstName = "Иван", LastName = "Иванов", Phone = "+79991234567", Email = "ivan@mail.ru" },
                new Client { ClientID = 2, FirstName = "Петр", LastName = "Петров", Phone = "+79997654321", Email = "petr@mail.ru" }
            };
        }

        public async Task<List<Client>> GetClientsAsync()
        {
            return await Task.FromResult(_clients);
        }

        public async Task AddClientAsync(Client client)
        {
            var newId = _clients.Count > 0 ? _clients.Max(c => c.ClientID) + 1 : 1;
            client.ClientID = newId;
            _clients.Add(client);
            await Task.CompletedTask;
        }

        public async Task DeleteClientAsync(int clientId)
        {
            var client = _clients.FirstOrDefault(c => c.ClientID == clientId);
            if (client != null)
            {
                _clients.Remove(client);
            }
            await Task.CompletedTask;
        }
    }
}