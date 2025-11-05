using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій для роботи з платежами використовуючи EF Core
/// </summary>
public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(AuctionDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Отримати платіж за ID аукціону
    /// </summary>
    public async Task<Payment?> GetByAuctionIdAsync(long auctionId)
    {
        return await _dbSet
            .Include(p => p.Auction)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.AuctionId == auctionId);
    }

    /// <summary>
    /// Отримати платежі користувача
    /// </summary>
    public async Task<IEnumerable<Payment>> GetPaymentsByUserAsync(long userId)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .Include(p => p.Auction)
            .OrderByDescending(p => p.PaymentTime)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати платежі за статусом
    /// </summary>
    public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(TransactionStatus status)
    {
        return await _dbSet
            .Where(p => p.TransactionStatus == status)
            .Include(p => p.Auction)
            .Include(p => p.User)
            .OrderByDescending(p => p.PaymentTime)
            .ToListAsync();
    }
}
