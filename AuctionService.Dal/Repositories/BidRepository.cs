using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій для роботи зі ставками використовуючи EF Core
/// </summary>
public class BidRepository : Repository<Bid>, IBidRepository
{
    public BidRepository(AuctionDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Отримати ставки за ID аукціону
    /// </summary>
    public async Task<IEnumerable<Bid>> GetByAuctionIdAsync(long auctionId)
    {
        return await _dbSet
            .Where(b => b.AuctionId == auctionId)
            .Include(b => b.User)
            .OrderByDescending(b => b.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати ставки за ID користувача
    /// </summary>
    public async Task<IEnumerable<Bid>> GetByUserIdAsync(long userId)
    {
        return await _dbSet
            .Where(b => b.UserId == userId)
            .Include(b => b.Auction)
            .OrderByDescending(b => b.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати найвищу ставку для аукціону
    /// </summary>
    public async Task<Bid?> GetHighestBidForAuctionAsync(long auctionId)
    {
        return await _dbSet
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.BidAmount)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Отримати ставки з користувачами (Eager Loading)
    /// </summary>
    public async Task<IEnumerable<Bid>> GetBidsWithUsersAsync(long auctionId)
    {
        return await _dbSet
            .Where(b => b.AuctionId == auctionId)
            .Include(b => b.User)
            .Include(b => b.Auction)
            .OrderByDescending(b => b.BidAmount)
            .ToListAsync();
    }
}
