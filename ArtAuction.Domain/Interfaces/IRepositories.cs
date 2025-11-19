using ArtAuction.Domain.Entities;

namespace ArtAuction.Domain.Interfaces;

public interface IAuctionRepository : IRepository<Auction>
{
    Task<IReadOnlyList<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetAuctionsBySellerAsync(string sellerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetAuctionsByWinnerAsync(string winnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> SearchByArtworkNameAsync(string searchTerm, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByAuctionIdAsync(string auctionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
