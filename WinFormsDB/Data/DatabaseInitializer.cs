using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Data
{
    public class DatabaseInitializer
    {
        private readonly DatabaseConnection _dbConnection;

        public DatabaseInitializer(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = await _dbConnection.GetConnectionAsync();

            // Создание таблицы Clients
            var createClientsTable = @"
            CREATE TABLE IF NOT EXISTS Clients (
                ClientID SERIAL PRIMARY KEY,
                FirstName VARCHAR(50) NOT NULL,
                LastName VARCHAR(50) NOT NULL,
                Phone VARCHAR(20),
                Email VARCHAR(100)
            )";

            // Создание таблицы Services
            var createServicesTable = @"
            CREATE TABLE IF NOT EXISTS Services (
                ServiceID SERIAL PRIMARY KEY,
                ServiceName VARCHAR(100) NOT NULL,
                ServiceType VARCHAR(50) NOT NULL
            )";

            // Создание таблицы Addresses
            var createAddressesTable = @"
            CREATE TABLE IF NOT EXISTS Addresses (
                AddressID SERIAL PRIMARY KEY,
                ClientID INTEGER NOT NULL REFERENCES Clients(ClientID) ON DELETE CASCADE,
                Street VARCHAR(100) NOT NULL,
                House VARCHAR(10) NOT NULL,
                Apartment VARCHAR(10),
                LivingArea DECIMAL(10,2) NOT NULL,
                ResidentsCount INTEGER NOT NULL
            )";

            // Создание таблицы Tariffs
            var createTariffsTable = @"
            CREATE TABLE IF NOT EXISTS Tariffs (
                TariffID SERIAL PRIMARY KEY,
                ServiceID INTEGER NOT NULL REFERENCES Services(ServiceID) ON DELETE CASCADE,
                TariffName VARCHAR(100) NOT NULL,
                Rate DECIMAL(10,4) NOT NULL,
                Unit VARCHAR(20) NOT NULL
            )";

            // Создание таблицы Bills
            var createBillsTable = @"
            CREATE TABLE IF NOT EXISTS Bills (
                BillID SERIAL PRIMARY KEY,
                AddressID INTEGER NOT NULL REFERENCES Addresses(AddressID) ON DELETE CASCADE,
                TariffID INTEGER NOT NULL REFERENCES Tariffs(TariffID) ON DELETE CASCADE,
                ConsumedVolume DECIMAL(10,2) NOT NULL,
                Amount DECIMAL(10,2) NOT NULL,
                PaymentDate TIMESTAMP NOT NULL,
                IsPaid BOOLEAN NOT NULL DEFAULT false
            )";

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                using var cmd1 = new NpgsqlCommand(createClientsTable, connection, transaction);
                await cmd1.ExecuteNonQueryAsync();

                using var cmd2 = new NpgsqlCommand(createServicesTable, connection, transaction);
                await cmd2.ExecuteNonQueryAsync();

                using var cmd3 = new NpgsqlCommand(createAddressesTable, connection, transaction);
                await cmd3.ExecuteNonQueryAsync();

                using var cmd4 = new NpgsqlCommand(createTariffsTable, connection, transaction);
                await cmd4.ExecuteNonQueryAsync();

                using var cmd5 = new NpgsqlCommand(createBillsTable, connection, transaction);
                await cmd5.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


    }
}
