using System.Data;
using Dapper;
using Npgsql;
using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій платежів з використанням Dapper та PostgreSQL functions
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly string _connectionString;
    private readonly IDbTransaction? _transaction;

    public PaymentRepository(string connectionString, IDbTransaction? transaction = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _transaction = transaction;
    }

    public async Task<Payment?> GetByIdAsync(long paymentId)
    {
        const string sql = @"
            SELECT payment_id AS PaymentId, auction_id AS AuctionId, user_id AS UserId, 
                   amount AS Amount, payment_time AS PaymentTime, transaction_status AS TransactionStatus
            FROM payments 
            WHERE payment_id = @PaymentId";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { PaymentId = paymentId });
    }

    public async Task<Payment?> GetByAuctionIdAsync(long auctionId)
    {
        const string sql = @"
            SELECT payment_id AS PaymentId, auction_id AS AuctionId, user_id AS UserId, 
                   amount AS Amount, payment_time AS PaymentTime, transaction_status AS TransactionStatus
            FROM payments 
            WHERE auction_id = @AuctionId";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { AuctionId = auctionId });
    }

    /// <summary>
    /// Створення платежу через PostgreSQL function
    /// </summary>
    public async Task<long> CreatePaymentAsync(long auctionId, long userId, decimal amount)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Викликаємо PostgreSQL function sp_create_payment
        const string sql = "SELECT * FROM sp_create_payment(@AuctionId, @UserId, @Amount)";
        
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            sql,
            new { AuctionId = auctionId, UserId = userId, Amount = amount }
        );

        if (result?.success == 1)
        {
            return (long)result.payment_id;
        }

        throw new InvalidOperationException(result?.message ?? "Failed to create payment");
    }
}
