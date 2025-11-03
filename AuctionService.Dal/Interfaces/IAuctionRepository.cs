using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з аукціонами (Dapper)
/// </summary>
public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(long auctionId);
    Task<IEnumerable<Auction>> GetAllAsync();
    Task<IEnumerable<Auction>> GetActiveAuctionsAsync();
    Task<long> CreateAsync(Auction auction);
    Task<bool> UpdateAsync(Auction auction);
    Task<bool> DeleteAsync(long auctionId);
}
