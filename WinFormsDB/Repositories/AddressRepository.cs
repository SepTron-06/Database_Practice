using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsDB.Data;
using WinFormsDB.Models;

namespace WinFormsDB.Repositories
{
    public class AddressRepository
    {
        private readonly DatabaseConnection _dbConnection;

        public AddressRepository(DatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> AddAddressAsync(Address address)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            INSERT INTO Addresses (ClientID, Street, House, Apartment, LivingArea, ResidentsCount)
            VALUES (@ClientID, @Street, @House, @Apartment, @LivingArea, @ResidentsCount)
            RETURNING AddressID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", address.ClientID);
            cmd.Parameters.AddWithValue("@Street", address.Street);
            cmd.Parameters.AddWithValue("@House", address.House);
            cmd.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LivingArea", address.LivingArea);
            cmd.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<List<Address>> GetAddressesByClientAsync(int clientId)
        {
            var addresses = new List<Address>();
            using var connection = await _dbConnection.GetConnectionAsync();

            var sql = "SELECT * FROM Addresses WHERE ClientID = @ClientID ORDER BY Street, House";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ClientID", clientId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                addresses.Add(new Address
                {
                    AddressID = reader.GetInt32("AddressID"),
                    ClientID = reader.GetInt32("ClientID"),
                    Street = reader.GetString("Street"),
                    House = reader.GetString("House"),
                    Apartment = reader.IsDBNull("Apartment") ? null : reader.GetString("Apartment"),
                    LivingArea = reader.GetDecimal("LivingArea"),
                    ResidentsCount = reader.GetInt32("ResidentsCount")
                });
            }

            return addresses;
        }

        public async Task<Address> GetAddressByIdAsync(int addressId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = "SELECT * FROM Addresses WHERE AddressID = @AddressID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", addressId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Address
                {
                    AddressID = reader.GetInt32("AddressID"),
                    ClientID = reader.GetInt32("ClientID"),
                    Street = reader.GetString("Street"),
                    House = reader.GetString("House"),
                    Apartment = reader.IsDBNull("Apartment") ? null : reader.GetString("Apartment"),
                    LivingArea = reader.GetDecimal("LivingArea"),
                    ResidentsCount = reader.GetInt32("ResidentsCount")
                };
            }

            return null;
        }

        public async Task UpdateAddressAsync(Address address)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = @"
            UPDATE Addresses 
            SET Street = @Street, House = @House, Apartment = @Apartment, 
                LivingArea = @LivingArea, ResidentsCount = @ResidentsCount
            WHERE AddressID = @AddressID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", address.AddressID);
            cmd.Parameters.AddWithValue("@Street", address.Street);
            cmd.Parameters.AddWithValue("@House", address.House);
            cmd.Parameters.AddWithValue("@Apartment", address.Apartment ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LivingArea", address.LivingArea);
            cmd.Parameters.AddWithValue("@ResidentsCount", address.ResidentsCount);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAddressAsync(int addressId)
        {
            using var connection = await _dbConnection.GetConnectionAsync();
            var sql = "DELETE FROM Addresses WHERE AddressID = @AddressID";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@AddressID", addressId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
