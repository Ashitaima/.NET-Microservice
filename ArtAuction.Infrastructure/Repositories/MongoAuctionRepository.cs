using ArtAuction.Application.Common.Interfaces;
using ArtAuction.Domain.Entities;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace ArtAuction.Infrastructure.Repositories;

public class MongoAuctionRepository : IAuctionRepository
{
    private readonly IMongoCollection<Auction> _auctions;
    private readonly ILogger<MongoAuctionRepository> _logger;

    public MongoAuctionRepository(IMongoDatabase database, ILogger<MongoAuctionRepository> logger)
    {
        _auctions = database.GetCollection<Auction>("auctions");
        _logger = logger;
    }

    public async Task<Auction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, id);
            return await _auctions.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auction by id {AuctionId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auctions.Find(_ => true).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all auctions");
            throw;
        }
    }

    public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Lte(a => a.StartTime, now),
                Builders<Auction>.Filter.Gte(a => a.EndTime, now),
                Builders<Auction>.Filter.Eq(a => a.Status, Domain.Enums.AuctionStatus.Active)
            );
            return await _auctions.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active auctions");
            throw;
        }
    }

    public async Task<Auction> AddAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        try
        {
            await _auctions.InsertOneAsync(auction, cancellationToken: cancellationToken);
            _logger.LogInformation("Auction {AuctionId} created successfully", auction.Id);
            return auction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding auction");
            throw;
        }
    }

    public async Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, auction.Id);
            await _auctions.ReplaceOneAsync(filter, auction, cancellationToken: cancellationToken);
            _logger.LogInformation("Auction {AuctionId} updated successfully", auction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auction {AuctionId}", auction.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, id);
            await _auctions.DeleteOneAsync(filter, cancellationToken);
            _logger.LogInformation("Auction {AuctionId} deleted successfully", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting auction {AuctionId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Auction>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var skip = (pageNumber - 1) * pageSize;
            return await _auctions.Find(_ => true)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged auctions");
            throw;
        }
    }

    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _auctions.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting auctions");
            throw;
        }
    }
}
