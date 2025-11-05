using AuctionService.Domain.Entities;

namespace AuctionService.Dal.Interfaces;

/// <summary>
/// Репозиторій для роботи з користувачами (EF Core)
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetUsersWithAuctionsAsync();
    Task<User?> GetUserWithBidsAsync(long userId);
}
