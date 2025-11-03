using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з користувачами (ADO.NET)
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId);
    Task<IEnumerable<User>> GetAllAsync();
    Task<long> CreateAsync(User user);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(long userId);
}
