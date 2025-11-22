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
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, существуют ли уже таблицы
                bool tablesExist = await CheckIfAllTablesExistAsync();

                if (tablesExist)
                {
                    Console.WriteLine("Таблицы уже существуют, пропускаем создание");
                    await CheckAndCreateDefaultDataAsync();
                    return;
                }

                Console.WriteLine("Создаем таблицы...");

                // СОЗДАЕМ ТАБЛИЦЫ ТОЛЬКО ЕСЛИ ИХ НЕТ
                var createClientsTable = @"
                CREATE TABLE IF NOT EXISTS clients (
                    client_id SERIAL PRIMARY KEY,
                    first_name VARCHAR(50) NOT NULL,
                    last_name VARCHAR(50) NOT NULL,
                    phone VARCHAR(20),
                    email VARCHAR(100)
                )";

                var createServicesTable = @"
                CREATE TABLE IF NOT EXISTS services (
                    service_id SERIAL PRIMARY KEY,
                    service_name VARCHAR(100) NOT NULL,
                    service_type VARCHAR(50) NOT NULL
                )";

                var createAddressesTable = @"
                CREATE TABLE IF NOT EXISTS addresses (
                    address_id SERIAL PRIMARY KEY,
                    client_id INTEGER NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
                    street VARCHAR(100) NOT NULL,
                    house VARCHAR(10) NOT NULL,
                    apartment VARCHAR(10),
                    living_area DECIMAL(10,2) NOT NULL,
                    residents_count INTEGER NOT NULL
                )";

                var createTariffsTable = @"
                CREATE TABLE IF NOT EXISTS tariffs (
                    tariff_id SERIAL PRIMARY KEY,
                    service_id INTEGER NOT NULL REFERENCES services(service_id) ON DELETE CASCADE,
                    service_name VARCHAR(100) NOT NULL,
                    price_per_square_meter DECIMAL(10,4) DEFAULT 0,
                    price_per_person DECIMAL(10,4) DEFAULT 0,
                    price_per_unit DECIMAL(10,4) DEFAULT 0
                )";

                var createBillsTable = @"
                CREATE TABLE IF NOT EXISTS bills (
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
                    // Создаем таблицы в правильном порядке с учетом зависимостей
                    using var cmd1 = new NpgsqlCommand(createClientsTable, connection, transaction);
                    await cmd1.ExecuteNonQueryAsync();
                    Console.WriteLine("Таблица clients создана");

                    using var cmd2 = new NpgsqlCommand(createServicesTable, connection, transaction);
                    await cmd2.ExecuteNonQueryAsync();
                    Console.WriteLine("Таблица services создана");

                    using var cmd3 = new NpgsqlCommand(createAddressesTable, connection, transaction);
                    await cmd3.ExecuteNonQueryAsync();
                    Console.WriteLine("Таблица addresses создана");

                    using var cmd4 = new NpgsqlCommand(createTariffsTable, connection, transaction);
                    await cmd4.ExecuteNonQueryAsync();
                    Console.WriteLine("Таблица tariffs создана");

                    using var cmd5 = new NpgsqlCommand(createBillsTable, connection, transaction);
                    await cmd5.ExecuteNonQueryAsync();
                    Console.WriteLine("Таблица bills создана");

                    await transaction.CommitAsync();
                    Console.WriteLine("Все таблицы успешно созданы");

                    // Создаем базовые данные после создания таблиц
                    await CreateDefaultDataAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Ошибка создания таблиц: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации базы данных: {ex.Message}");
                throw;
            }
        }

        private async Task CreateDefaultDataAsync()
        {
            try
            {
                Console.WriteLine("Создание базовых данных...");

                // Создаем в правильной последовательности с учетом зависимостей
                await CreateDefaultClientsAsync();
                await CreateDefaultServicesAsync();
                await CreateDefaultTariffsAsync();
                await CreateDefaultAddressesAsync(); // ДОБАВЛЕНО
                await CreateDefaultBillsAsync();     // ДОБАВЛЕНО

                Console.WriteLine("Базовые данные успешно созданы");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания базовых данных: {ex.Message}");
                throw;
            }
        }

        private async Task CheckAndCreateDefaultDataAsync()
        {
            try
            {
                Console.WriteLine("Проверка базовых данных...");

                // Проверяем все необходимые таблицы на наличие данных
                bool hasClients = await CheckIfTableHasDataAsync("clients");
                bool hasServices = await CheckIfTableHasDataAsync("services");
                bool hasTariffs = await CheckIfTableHasDataAsync("tariffs");
                bool hasAddresses = await CheckIfTableHasDataAsync("addresses"); // ДОБАВЛЕНО
                bool hasBills = await CheckIfTableHasDataAsync("bills");         // ДОБАВЛЕНО

                if (!hasClients)
                {
                    Console.WriteLine("Создаем базовых клиентов...");
                    await CreateDefaultClientsAsync();
                }

                if (!hasServices)
                {
                    Console.WriteLine("Создаем базовые услуги...");
                    await CreateDefaultServicesAsync();
                }

                if (!hasTariffs)
                {
                    Console.WriteLine("Создаем базовые тарифы...");
                    await CreateDefaultTariffsAsync();
                }

                if (!hasAddresses)
                {
                    Console.WriteLine("Создаем базовые адреса...");
                    await CreateDefaultAddressesAsync();
                }

                if (!hasBills)
                {
                    Console.WriteLine("Создаем базовые счета...");
                    await CreateDefaultBillsAsync();
                }

                Console.WriteLine("Проверка базовых данных завершена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки базовых данных: {ex.Message}");
            }
        }

        private async Task CreateDefaultClientsAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, нет ли уже этих клиентов
                var checkQuery = "SELECT COUNT(*) FROM clients WHERE email IN ('Zaitsevyaroslav@mail.ru', 'KryukovAndrey@mail.ru')";
                using var checkCommand = new NpgsqlCommand(checkQuery, connection);
                var existingCount = (long)await checkCommand.ExecuteScalarAsync();

                if (existingCount > 0)
                {
                    Console.WriteLine("Базовые клиенты уже существуют");
                    return;
                }

                var insertQuery = @"
                    INSERT INTO clients (first_name, last_name, phone, email) 
                    VALUES 
                    ('Ярослав', 'Зайцев', '7(959)506-97-48', 'Zaitsevyaroslav@mail.ru'),
                    ('Андрей', 'Крюков', '7(959)489-61-02', 'KryukovAndrey@mail.ru')";

                using var command = new NpgsqlCommand(insertQuery, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Базовые клиенты успешно добавлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления базовых клиентов: {ex.Message}");
                throw;
            }
        }

        private async Task CreateDefaultServicesAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, нет ли уже этих услуг
                var checkQuery = "SELECT COUNT(*) FROM services";
                using var checkCommand = new NpgsqlCommand(checkQuery, connection);
                var existingCount = (long)await checkCommand.ExecuteScalarAsync();

                if (existingCount > 0)
                {
                    Console.WriteLine("Базовые услуги уже существуют");
                    return;
                }

                var insertQuery = @"
                    INSERT INTO services (service_name, service_type) 
                    VALUES 
                    ('Подача холодной воды', 'Водоснабжение'),
                    ('Подача горячей воды', 'Водоснабжение'),
                    ('Подача газа в квартиру', 'Газоснабжение'),
                    ('Подача электричества в квартиру', 'Электроснабжение'),
                    ('Взнос на капитальный ремонт дома', 'Капитальный ремонт'),
                    ('Обращение с ТКО', 'Вывоз мусора'),
                    ('Отопление', 'Теплоснабжение'),
                    ('Электронное запирающее устройство', 'Электроснабжение')";

                using var command = new NpgsqlCommand(insertQuery, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Базовые услуги успешно добавлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления базовых услуг: {ex.Message}");
                throw;
            }
        }

        private async Task CreateDefaultTariffsAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, нет ли уже этих тарифов
                var checkQuery = "SELECT COUNT(*) FROM tariffs";
                using var checkCommand = new NpgsqlCommand(checkQuery, connection);
                var existingCount = (long)await checkCommand.ExecuteScalarAsync();

                if (existingCount > 0)
                {
                    Console.WriteLine("Базовые тарифы уже существуют");
                    return;
                }

                var insertQuery = @"
                    INSERT INTO tariffs (service_id, service_name, price_per_square_meter, price_per_person, price_per_unit) 
                    VALUES 
                    (1, 'Подача холодной воды', 0, 0, 67.15),
                    (2, 'Подача горячей воды', 0, 0, 73.62),
                    (3, 'Подача газа в квартиру', 0, 0, 8.19),
                    (4, 'Подача электричества в квартиру', 0, 0, 5.24),
                    (5, 'Взнос на капитальный ремонт дома', 14.08, 0, 0),
                    (6, 'Обращение с ТКО', 0, 122.13, 0),
                    (7, 'Отопление', 3785.86, 0, 0),
                    (8, 'Электронное запирающее устройство', 0, 0, 60.00)";

                using var command = new NpgsqlCommand(insertQuery, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Базовые тарифы успешно добавлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления базовых тарифов: {ex.Message}");
                throw;
            }
        }

        // НОВЫЙ МЕТОД: Создание тестовых адресов
        private async Task CreateDefaultAddressesAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, нет ли уже адресов
                var checkQuery = "SELECT COUNT(*) FROM addresses";
                using var checkCommand = new NpgsqlCommand(checkQuery, connection);
                var existingCount = (long)await checkCommand.ExecuteScalarAsync();

                if (existingCount > 0)
                {
                    Console.WriteLine("Базовые адреса уже существуют");
                    return;
                }

                // Получаем ID созданных клиентов
                var getClientsQuery = "SELECT client_id FROM clients ORDER BY client_id";
                using var getClientsCommand = new NpgsqlCommand(getClientsQuery, connection);
                using var reader = await getClientsCommand.ExecuteReaderAsync();

                var clientIds = new List<int>();
                while (await reader.ReadAsync())
                {
                    clientIds.Add(reader.GetInt32(0));
                }
                reader.Close();

                if (clientIds.Count < 2)
                {
                    Console.WriteLine("Недостаточно клиентов для создания адресов");
                    return;
                }

                var insertQuery = @"
                    INSERT INTO addresses (client_id, street, house, apartment, living_area, residents_count) 
                    VALUES 
                    (@ClientId1, 'Ленина', '10', '25', 65.50, 3),
                    (@ClientId2, 'Пушкина', '15', '42', 48.30, 2)";

                using var command = new NpgsqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@ClientId1", clientIds[0]);
                command.Parameters.AddWithValue("@ClientId2", clientIds[1]);

                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Базовые адреса успешно добавлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления базовых адресов: {ex.Message}");
                throw;
            }
        }

        // НОВЫЙ МЕТОД: Создание тестовых счетов
        private async Task CreateDefaultBillsAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем, нет ли уже счетов
                var checkQuery = "SELECT COUNT(*) FROM bills";
                using var checkCommand = new NpgsqlCommand(checkQuery, connection);
                var existingCount = (long)await checkCommand.ExecuteScalarAsync();

                if (existingCount > 0)
                {
                    Console.WriteLine("Базовые счета уже существуют");
                    return;
                }

                // Получаем ID созданных адресов и тарифов
                var getAddressesQuery = "SELECT address_id FROM addresses ORDER BY address_id";
                using var getAddressesCommand = new NpgsqlCommand(getAddressesQuery, connection);
                using var addressesReader = await getAddressesCommand.ExecuteReaderAsync();

                var addressIds = new List<int>();
                while (await addressesReader.ReadAsync())
                {
                    addressIds.Add(addressesReader.GetInt32(0));
                }
                addressesReader.Close();

                var getTariffsQuery = "SELECT tariff_id FROM tariffs ORDER BY tariff_id LIMIT 2";
                using var getTariffsCommand = new NpgsqlCommand(getTariffsQuery, connection);
                using var tariffsReader = await getTariffsCommand.ExecuteReaderAsync();

                var tariffIds = new List<int>();
                while (await tariffsReader.ReadAsync())
                {
                    tariffIds.Add(tariffsReader.GetInt32(0));
                }
                tariffsReader.Close();

                if (addressIds.Count < 2 || tariffIds.Count < 2)
                {
                    Console.WriteLine("Недостаточно адресов или тарифов для создания счетов");
                    return;
                }

                var insertQuery = @"
                    INSERT INTO bills (address_id, tariff_id, amount, issue_date, payment_date, is_paid) 
                    VALUES 
                    (@AddressId1, @TariffId1, 1500.75, @IssueDate1, NULL, false),
                    (@AddressId2, @TariffId2, 2300.50, @IssueDate2, @PaymentDate2, true)";

                using var command = new NpgsqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@AddressId1", addressIds[0]);
                command.Parameters.AddWithValue("@TariffId1", tariffIds[0]);
                command.Parameters.AddWithValue("@IssueDate1", DateTime.Now.AddDays(-30));

                command.Parameters.AddWithValue("@AddressId2", addressIds[1]);
                command.Parameters.AddWithValue("@TariffId2", tariffIds[1]);
                command.Parameters.AddWithValue("@IssueDate2", DateTime.Now.AddDays(-45));
                command.Parameters.AddWithValue("@PaymentDate2", DateTime.Now.AddDays(-15));

                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Базовые счета успешно добавлены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления базовых счетов: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> CheckIfTableHasDataAsync(string tableName)
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                var query = $"SELECT COUNT(*) FROM {tableName}";

                using var command = new NpgsqlCommand(query, connection);
                var count = (long)await command.ExecuteScalarAsync();

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки данных в таблице {tableName}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckTableExistsAsync(string tableName)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки существования таблицы {tableName}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckIfAllTablesExistAsync()
        {
            var requiredTables = new[] { "clients", "services", "addresses", "tariffs", "bills" };

            foreach (var tableName in requiredTables)
            {
                if (!await CheckTableExistsAsync(tableName))
                {
                    Console.WriteLine($"Таблица {tableName} не существует");
                    return false;
                }
            }

            Console.WriteLine("Все таблицы существуют");
            return true;
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
                throw new Exception($"Не все таблицы были созданы успешно. Создано: {tables.Count}/5");
            }

            Console.WriteLine("Структура таблиц проверена успешно");
        }

        public async Task RecreateDatabaseAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                var dropTables = @"
                    DROP TABLE IF EXISTS bills CASCADE;
                    DROP TABLE IF EXISTS tariffs CASCADE;
                    DROP TABLE IF EXISTS addresses CASCADE;
                    DROP TABLE IF EXISTS services CASCADE;
                    DROP TABLE IF EXISTS clients CASCADE;";

                using var command = new NpgsqlCommand(dropTables, connection);
                await command.ExecuteNonQueryAsync();

                Console.WriteLine("Таблицы удалены");

                // Теперь создаем таблицы заново
                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка пересоздания базы данных: {ex.Message}");
                throw;
            }
        }

        public async Task TestConnectionAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();
                using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();
                Console.WriteLine("Подключение к базе данных успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения к базе данных: {ex.Message}");
                throw;
            }
        }
    }
}