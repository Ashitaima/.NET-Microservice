using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи зі ставками (Dapper)
/// </summary>
public interface IBidRepository
{
    Task<Bid?> GetByIdAsync(long bidId);
    Task<IEnumerable<Bid>> GetByAuctionIdAsync(long auctionId);
    Task<IEnumerable<Bid>> GetByUserIdAsync(long userId);
    Task<long> CreateAsync(Bid bid);
    Task<bool> PlaceBidAsync(long auctionId, long userId, decimal bidAmount);
}
