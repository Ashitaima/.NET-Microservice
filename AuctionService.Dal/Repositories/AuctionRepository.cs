using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій для роботи з аукціонами використовуючи EF Core
/// </summary>
public class AuctionRepository : Repository<Auction>, IAuctionRepository
{
    public AuctionRepository(AuctionDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Отримати активні аукціони
    /// </summary>
    public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync()
    {
        return await _dbSet
            .Where(a => a.Status == AuctionStatus.Active)
            .OrderBy(a => a.EndTime)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати аукціони з ставками (Eager Loading)
    /// </summary>
    public async Task<IEnumerable<Auction>> GetAuctionsWithBidsAsync()
    {
        return await _dbSet
            .Include(a => a.Bids)
                .ThenInclude(b => b.User)
            .Include(a => a.Seller)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати аукціон з усіма деталями (Eager Loading)
    /// </summary>
    public async Task<Auction?> GetAuctionWithDetailsAsync(long auctionId)
    {
        return await _dbSet
            .Include(a => a.Seller)
            .Include(a => a.Winner)
            .Include(a => a.Bids)
                .ThenInclude(b => b.User)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.AuctionId == auctionId);
    }

    /// <summary>
    /// Отримати аукціони продавця
    /// </summary>
    public async Task<IEnumerable<Auction>> GetAuctionsBySellerAsync(long sellerId)
    {
        return await _dbSet
            .Where(a => a.SellerUserId == sellerId)
            .Include(a => a.Bids)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати аукціони за статусом
    /// </summary>
    public async Task<IEnumerable<Auction>> GetAuctionsByStatusAsync(AuctionStatus status)
    {
        return await _dbSet
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
    }
}
