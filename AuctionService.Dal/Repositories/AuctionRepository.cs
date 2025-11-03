using System.Data;
using Dapper;
using Npgsql;
using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій аукціонів з використанням Dapper для PostgreSQL
/// </summary>
public class AuctionRepository : IAuctionRepository
{
    private readonly string _connectionString;
    private readonly IDbTransaction? _transaction;

    public AuctionRepository(string connectionString, IDbTransaction? transaction = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _transaction = transaction;
    }

    public async Task<Auction?> GetByIdAsync(long auctionId)
    {
        const string sql = @"
            SELECT auction_id AS AuctionId, artwork_id AS ArtworkId, artwork_name AS ArtworkName, 
                   seller_user_id AS SellerUserId, start_price AS StartPrice, current_price AS CurrentPrice, 
                   start_time AS StartTime, end_time AS EndTime, status AS Status, winner_user_id AS WinnerUserId
            FROM auctions 
            WHERE auction_id = @AuctionId";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Auction>(sql, new { AuctionId = auctionId });
    }

    public async Task<IEnumerable<Auction>> GetAllAsync()
    {
        const string sql = @"
            SELECT auction_id AS AuctionId, artwork_id AS ArtworkId, artwork_name AS ArtworkName, 
                   seller_user_id AS SellerUserId, start_price AS StartPrice, current_price AS CurrentPrice, 
                   start_time AS StartTime, end_time AS EndTime, status AS Status, winner_user_id AS WinnerUserId
            FROM auctions 
            ORDER BY start_time DESC";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Auction>(sql);
    }

    public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync()
    {
        const string sql = @"
            SELECT auction_id AS AuctionId, artwork_id AS ArtworkId, artwork_name AS ArtworkName, 
                   seller_user_id AS SellerUserId, start_price AS StartPrice, current_price AS CurrentPrice, 
                   start_time AS StartTime, end_time AS EndTime, status AS Status, winner_user_id AS WinnerUserId
            FROM auctions 
            WHERE status = 1 AND end_time > CURRENT_TIMESTAMP
            ORDER BY end_time ASC";

        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Auction>(sql);
    }

    public async Task<long> CreateAsync(Auction auction)
    {
        const string sql = @"
            INSERT INTO auctions (artwork_id, artwork_name, seller_user_id, start_price, current_price, 
                                  start_time, end_time, status, winner_user_id)
            VALUES (@ArtworkId, @ArtworkName, @SellerUserId, @StartPrice, @CurrentPrice, 
                    @StartTime, @EndTime, @Status, @WinnerUserId)
            RETURNING auction_id";

        using var connection = new NpgsqlConnection(_connectionString);
        
        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            auction.ArtworkId,
            auction.ArtworkName,
            auction.SellerUserId,
            auction.StartPrice,
            auction.CurrentPrice,
            auction.StartTime,
            auction.EndTime,
            Status = (int)auction.Status,
            auction.WinnerUserId
        });

        return id;
    }

    public async Task<bool> UpdateAsync(Auction auction)
    {
        const string sql = @"
            UPDATE auctions 
            SET artwork_name = @ArtworkName,
                start_price = @StartPrice,
                current_price = @CurrentPrice,
                start_time = @StartTime,
                end_time = @EndTime,
                status = @Status,
                winner_user_id = @WinnerUserId
            WHERE auction_id = @AuctionId";

        using var connection = new NpgsqlConnection(_connectionString);
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            auction.AuctionId,
            auction.ArtworkName,
            auction.StartPrice,
            auction.CurrentPrice,
            auction.StartTime,
            auction.EndTime,
            Status = (int)auction.Status,
            auction.WinnerUserId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(long auctionId)
    {
        const string sql = "DELETE FROM auctions WHERE auction_id = @AuctionId";

        using var connection = new NpgsqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { AuctionId = auctionId });

        return rowsAffected > 0;
    }
}
