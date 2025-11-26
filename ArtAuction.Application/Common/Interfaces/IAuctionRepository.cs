using ArtAuction.Domain.Entities;

namespace ArtAuction.Application.Common.Interfaces;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default);
    Task<Auction> AddAsync(Auction auction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Auction auction, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Auction>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
