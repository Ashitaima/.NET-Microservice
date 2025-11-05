using AuctionService.Dal.Interfaces;
using AuctionService.Dal.Repositories;

namespace AuctionService.Dal;

/// <summary>
/// Unit of Work для керування транзакціями та репозиторіями з EF Core
/// </summary>
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AuctionDbContext _context;
    private IAuctionRepository? _auctionRepository;
    private IBidRepository? _bidRepository;
    private IUserRepository? _userRepository;
    private IPaymentRepository? _paymentRepository;

    public UnitOfWork(AuctionDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IAuctionRepository Auctions => _auctionRepository ??= new AuctionRepository(_context);
    public IBidRepository Bids => _bidRepository ??= new BidRepository(_context);
    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IPaymentRepository Payments => _paymentRepository ??= new PaymentRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync();
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

