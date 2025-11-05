using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з аукціонами (EF Core)
/// </summary>
public interface IAuctionRepository : IRepository<Auction>
{
    Task<IEnumerable<Auction>> GetActiveAuctionsAsync();
    Task<IEnumerable<Auction>> GetAuctionsWithBidsAsync();
    Task<Auction?> GetAuctionWithDetailsAsync(long auctionId);
    Task<IEnumerable<Auction>> GetAuctionsBySellerAsync(long sellerId);
    Task<IEnumerable<Auction>> GetAuctionsByStatusAsync(AuctionStatus status);
}
