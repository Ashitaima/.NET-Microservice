namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Unit of Work для керування транзакціями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IAuctionRepository Auctions { get; }
    IBidRepository Bids { get; }
    IPaymentRepository Payments { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
