using AuctionService.Dal.Interfaces;
using AuctionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Dal.Repositories;

/// <summary>
/// Репозиторій для роботи з користувачами використовуючи EF Core
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AuctionDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Отримати користувача за ім'ям
    /// </summary>
    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.UserName == userName);
    }

    /// <summary>
    /// Отримати користувачів з аукціонами (Eager Loading)
    /// </summary>
    public async Task<IEnumerable<User>> GetUsersWithAuctionsAsync()
    {
        return await _dbSet
            .Include(u => u.SellerAuctions)
            .Include(u => u.WonAuctions)
            .ToListAsync();
    }

    /// <summary>
    /// Отримати користувача зі ставками (Explicit Loading)
    /// </summary>
    public async Task<User?> GetUserWithBidsAsync(long userId)
    {
        var user = await _dbSet.FindAsync(userId);
        
        if (user != null)
        {
            await _context.Entry(user)
                .Collection(u => u.Bids)
                .Query()
                .Include(b => b.Auction)
                .LoadAsync();
        }
        
        return user;
    }
}
