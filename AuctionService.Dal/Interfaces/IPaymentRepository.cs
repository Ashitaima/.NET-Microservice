using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з платежами (EF Core)
/// </summary>
public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByAuctionIdAsync(long auctionId);
    Task<IEnumerable<Payment>> GetPaymentsByUserAsync(long userId);
    Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(TransactionStatus status);
}
