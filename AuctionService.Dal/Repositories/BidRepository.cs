using System.Data;
using Dapper;
using Npgsql;
using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій ставок з використанням Dapper та PostgreSQL functions
/// </summary>
public class BidRepository : IBidRepository
{
    private readonly string _connectionString;
    private readonly IDbTransaction? _transaction;

    public BidRepository(string connectionString, IDbTransaction? transaction = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _transaction = transaction;
    }

    public async Task<Bid?> GetByIdAsync(long bidId)
    {
        const string sql = @"
            SELECT bid_id AS BidId, auction_id AS AuctionId, user_id AS UserId, 
                   bid_amount AS BidAmount, timestamp AS Timestamp
            FROM bids 
            WHERE bid_id = @BidId";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Bid>(sql, new { BidId = bidId });
    }

    public async Task<IEnumerable<Bid>> GetByAuctionIdAsync(long auctionId)
    {
        const string sql = @"
            SELECT bid_id AS BidId, auction_id AS AuctionId, user_id AS UserId, 
                   bid_amount AS BidAmount, timestamp AS Timestamp
            FROM bids 
            WHERE auction_id = @AuctionId
            ORDER BY bid_amount DESC, timestamp DESC";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Bid>(sql, new { AuctionId = auctionId });
    }

    public async Task<IEnumerable<Bid>> GetByUserIdAsync(long userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Використовуємо PostgreSQL function sp_get_user_bids
        const string sql = "SELECT * FROM sp_get_user_bids(@UserId)";
        
        var result = await connection.QueryAsync(sql, new { UserId = userId });

        return result.Select(r => new Bid
        {
            BidId = r.bid_id,
            AuctionId = r.auction_id,
            BidAmount = r.bid_amount,
            Timestamp = r.bid_timestamp,
            UserId = userId
        });
    }

    public async Task<long> CreateAsync(Bid bid)
    {
        const string sql = @"
            INSERT INTO bids (auction_id, user_id, bid_amount, timestamp)
            VALUES (@AuctionId, @UserId, @BidAmount, @Timestamp)
            RETURNING bid_id";

        using var connection = new NpgsqlConnection(_connectionString);
        
        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            bid.AuctionId,
            bid.UserId,
            bid.BidAmount,
            Timestamp = bid.Timestamp == default ? DateTime.UtcNow : bid.Timestamp
        });

        return id;
    }

    /// <summary>
    /// Розміщення ставки через PostgreSQL function з блокуванням
    /// </summary>
    public async Task<bool> PlaceBidAsync(long auctionId, long userId, decimal bidAmount)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Викликаємо PostgreSQL function sp_place_bid
        const string sql = "SELECT * FROM sp_place_bid(@AuctionId, @UserId, @BidAmount)";
        
        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
            sql,
            new { AuctionId = auctionId, UserId = userId, BidAmount = bidAmount }
        );

        return result?.success == 1;
    }
}
