using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з платежами (Dapper)
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(long paymentId);
    Task<Payment?> GetByAuctionIdAsync(long auctionId);
    Task<long> CreatePaymentAsync(long auctionId, long userId, decimal amount);
}
