using ArtAuction.Domain.Common;
using System.Linq.Expressions;

namespace ArtAuction.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, 
                                      CancellationToken cancellationToken = default);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate,
                         CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, 
                         CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, 
                          CancellationToken cancellationToken = default);
    
    // MongoDB-specific methods
    Task<IReadOnlyList<T>> FindWithSortAsync(Expression<Func<T, bool>> predicate,
                                             Expression<Func<T, object>> orderBy,
                                             bool descending = false,
                                             CancellationToken cancellationToken = default);
    
    Task<(IReadOnlyList<T> Items, long TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        CancellationToken cancellationToken = default);
}
