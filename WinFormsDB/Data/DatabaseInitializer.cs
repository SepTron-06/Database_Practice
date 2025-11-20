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
            CREATE TABLE IF NOT EXISTS bills (
                bill_id SERIAL PRIMARY KEY,
                address_id INTEGER NOT NULL REFERENCES addresses(address_id) ON DELETE CASCADE,
                service_id INTEGER NOT NULL REFERENCES services(service_id) ON DELETE CASCADE,
                amount DECIMAL(10,2) NOT NULL,
                issue_date DATE NOT NULL,
                payment_date DATE NULL,
                is_paid BOOLEAN NOT NULL DEFAULT false
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

        // ДОБАВЛЕННЫЙ МЕТОД для проверки существования таблицы
        public async Task<bool> CheckTableExistsAsync(string tableName)
        {
            using var connection = await _dbConnection.GetConnectionAsync();

            var query = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = @tableName
                )";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@tableName", tableName.ToLower());

            return (bool)await command.ExecuteScalarAsync();
        }

        public async Task ValidateTableStructureAsync()
        {
            using var connection = await _dbConnection.GetConnectionAsync();

            // Проверяем основные таблицы
            var checkTables = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name IN ('clients', 'services', 'addresses', 'tariffs', 'bills')";

            using var command = new NpgsqlCommand(checkTables, connection);
            using var reader = await command.ExecuteReaderAsync();

            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            if (tables.Count < 5)
            {
                throw new Exception("Не все таблицы были созданы успешно");
            }
        }
    }
}