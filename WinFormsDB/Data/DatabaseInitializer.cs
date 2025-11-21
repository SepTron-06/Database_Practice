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

            // УДАЛЯЕМ ВСЕ ТАБЛИЦЫ В ПРАВИЛЬНОМ ПОРЯДКЕ
            var dropTables = @"
        DROP TABLE IF EXISTS bills CASCADE;
        DROP TABLE IF EXISTS tariffs CASCADE;
        DROP TABLE IF EXISTS addresses CASCADE;
        DROP TABLE IF EXISTS services CASCADE;
        DROP TABLE IF EXISTS clients CASCADE;";

            // СОЗДАЕМ ТАБЛИЦЫ ЗАНОВО
            var createClientsTable = @"
        CREATE TABLE clients (
            client_id SERIAL PRIMARY KEY,
            first_name VARCHAR(50) NOT NULL,
            last_name VARCHAR(50) NOT NULL,
            phone VARCHAR(20),
            email VARCHAR(100)
        )";

            var createServicesTable = @"
        CREATE TABLE services (
            service_id SERIAL PRIMARY KEY,
            service_name VARCHAR(100) NOT NULL,
            service_type VARCHAR(50) NOT NULL
        )";

            var createAddressesTable = @"
        CREATE TABLE addresses (
            address_id SERIAL PRIMARY KEY,
            client_id INTEGER NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
            street VARCHAR(100) NOT NULL,
            house VARCHAR(10) NOT NULL,
            apartment VARCHAR(10),
            living_area DECIMAL(10,2) NOT NULL,
            residents_count INTEGER NOT NULL
        )";

            var createTariffsTable = @"
        CREATE TABLE tariffs (
            tariff_id SERIAL PRIMARY KEY,
            service_id INTEGER NOT NULL REFERENCES services(service_id) ON DELETE CASCADE,
            service_name VARCHAR(100) NOT NULL,
            price_per_square_meter DECIMAL(10,4) DEFAULT 0,
            price_per_person DECIMAL(10,4) DEFAULT 0,
            price_per_unit DECIMAL(10,4) DEFAULT 0
        )";

            // ИСПРАВЛЕННАЯ таблица bills - использует tariff_id вместо service_id
            var createBillsTable = @"
        CREATE TABLE bills (
            bill_id SERIAL PRIMARY KEY,
            address_id INTEGER NOT NULL REFERENCES addresses(address_id) ON DELETE CASCADE,
            tariff_id INTEGER NOT NULL REFERENCES tariffs(tariff_id) ON DELETE CASCADE,
            amount DECIMAL(10,2) NOT NULL,
            issue_date DATE NOT NULL,
            payment_date DATE NULL,
            is_paid BOOLEAN NOT NULL DEFAULT false
        )";

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // Удаляем таблицы
                using var dropCmd = new NpgsqlCommand(dropTables, connection, transaction);
                await dropCmd.ExecuteNonQueryAsync();

                // Создаем таблицы заново
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
                Console.WriteLine("Все таблицы успешно пересозданы с новой структурой");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Ошибка пересоздания таблиц: {ex.Message}");
                throw;
            }
        }

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