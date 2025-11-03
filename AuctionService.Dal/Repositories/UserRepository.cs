using System.Data;
using Npgsql;
using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій користувачів з використанням чистого ADO.NET для PostgreSQL
/// Демонструє параметризовані запити, NpgsqlCommand, NpgsqlDataReader
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly string _connectionString;
    private readonly IDbTransaction? _transaction;

    public UserRepository(string connectionString, IDbTransaction? transaction = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _transaction = transaction;
    }

    public async Task<User?> GetByIdAsync(long userId)
    {
        const string sql = "SELECT user_id, user_name, balance FROM users WHERE user_id = @user_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", userId);

        using var reader = await command.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = "SELECT user_id, user_name, balance FROM users ORDER BY user_name";
        var users = new List<User>();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task<long> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (user_id, user_name, balance) 
            VALUES (@user_id, @user_name, @balance)
            RETURNING user_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", user.UserId);
        command.Parameters.AddWithValue("@user_name", user.UserName);
        command.Parameters.AddWithValue("@balance", user.Balance);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE users 
            SET user_name = @user_name, 
                balance = @balance 
            WHERE user_id = @user_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", user.UserId);
        command.Parameters.AddWithValue("@user_name", user.UserName);
        command.Parameters.AddWithValue("@balance", user.Balance);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(long userId)
    {
        const string sql = "DELETE FROM users WHERE user_id = @user_id";

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /// <summary>
    /// Маппінг NpgsqlDataReader на модель User
    /// </summary>
    private static User MapUser(NpgsqlDataReader reader)
    {
        return new User
        {
            UserId = reader.GetInt64(reader.GetOrdinal("user_id")),
            UserName = reader.GetString(reader.GetOrdinal("user_name")),
            Balance = reader.GetDecimal(reader.GetOrdinal("balance"))
        };
    }
}
