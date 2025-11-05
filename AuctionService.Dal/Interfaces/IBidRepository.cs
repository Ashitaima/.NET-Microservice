using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи зі ставками (EF Core)
/// </summary>
public interface IBidRepository : IRepository<Bid>
{
    Task<IEnumerable<Bid>> GetByAuctionIdAsync(long auctionId);
    Task<IEnumerable<Bid>> GetByUserIdAsync(long userId);
    Task<Bid?> GetHighestBidForAuctionAsync(long auctionId);
    Task<IEnumerable<Bid>> GetBidsWithUsersAsync(long auctionId);
}
