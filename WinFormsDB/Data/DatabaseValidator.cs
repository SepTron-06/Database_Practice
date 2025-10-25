using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Data
{
    public class DatabaseValidator
    {
        private readonly DatabaseConnection _dbConnection;

        public DatabaseValidator(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<bool> ValidateTableStructureAsync()
        {
            try
            {
                using var connection = await _dbConnection.GetConnectionAsync();

                // Проверяем существование таблицы Clients и ее структуру
                var sql = @"
                SELECT column_name, data_type, is_nullable
                FROM information_schema.columns 
                WHERE table_name = 'clients' 
                ORDER BY ordinal_position";

                using var cmd = new NpgsqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine("Структура таблицы Clients:");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"Столбец: {reader.GetString(0)}, Тип: {reader.GetString(1)}, Nullable: {reader.GetString(2)}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке структуры БД: {ex.Message}");
                return false;
            }
        }
    }
}
