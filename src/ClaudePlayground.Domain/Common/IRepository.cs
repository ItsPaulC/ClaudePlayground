using System.Linq.Expressions;
using ClaudePlayground.Domain.Entities;

namespace ClaudePlayground.Domain.Common;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    // Tenant-scoped methods for secure multi-tenancy
    Task<T?> GetByIdAndTenantAsync(string id, string tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    // Generic filter method for custom queries (e.g., by email, etc.)
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    // Session-aware methods for transaction support (session is object to avoid MongoDB dependency in Domain layer)
    Task<T> CreateWithSessionAsync(T entity, object session, CancellationToken cancellationToken = default);
}
