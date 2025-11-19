namespace ArtAuction.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAuctionRepository Auctions { get; }
    IUserRepository Users { get; }
    IPaymentRepository Payments { get; }
    
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
