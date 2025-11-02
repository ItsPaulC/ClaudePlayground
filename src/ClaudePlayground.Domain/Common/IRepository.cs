using System.Linq.Expressions;
using ClaudePlayground.Domain.Entities;

namespace ClaudePlayground.Domain.Common;

/// <summary>
/// Generic repository interface for data access operations on domain entities.
/// Provides CRUD operations, tenant-scoped queries, filtering, pagination, and transaction support.
/// </summary>
/// <typeparam name="T">The entity type that inherits from BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The entity if found, otherwise null</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of type T
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity in the repository
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The created entity with generated Id and timestamps</returns>
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    /// <param name="entity">The entity to update with modified properties</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The updated entity</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if the entity was deleted, false if not found</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity by its unique identifier scoped to a specific tenant for secure multi-tenancy
    /// </summary>
    /// <param name="id">The unique identifier of the entity</param>
    /// <param name="tenantId">The tenant identifier to scope the query</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The entity if found within the tenant, otherwise null</returns>
    Task<T?> GetByIdAndTenantAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities scoped to a specific tenant for secure multi-tenancy
    /// </summary>
    /// <param name="tenantId">The tenant identifier to scope the query</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A collection of all entities within the tenant</returns>
    Task<IEnumerable<T>> GetAllByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a single entity matching the specified filter expression
    /// </summary>
    /// <param name="filter">Lambda expression defining the filter criteria (e.g., u => u.Email == "user@example.com")</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The first entity matching the filter, otherwise null</returns>
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all entities matching the specified filter expression
    /// </summary>
    /// <param name="filter">Lambda expression defining the filter criteria</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A collection of entities matching the filter</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of entities with optional sorting and filtering
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortBy">The property name to sort by (e.g., "CreatedAt", "Email"). If null, no sorting is applied</param>
    /// <param name="sortDescending">True for descending sort, false for ascending (default)</param>
    /// <param name="filter">Optional filter expression to apply before pagination</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A PagedResult containing items, total count, and pagination metadata</returns>
    Task<PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of entities scoped to a specific tenant with optional sorting and filtering
    /// </summary>
    /// <param name="tenantId">The tenant identifier to scope the query</param>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortBy">The property name to sort by (e.g., "CreatedAt", "Email"). If null, no sorting is applied</param>
    /// <param name="sortDescending">True for descending sort, false for ascending (default)</param>
    /// <param name="filter">Optional filter expression to apply before pagination (combined with tenant filter)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>A PagedResult containing items, total count, and pagination metadata</returns>
    Task<PagedResult<T>> GetPagedByTenantAsync(
        string tenantId,
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity within a transaction session for atomic multi-document operations.
    /// The session parameter is typed as object to avoid MongoDB dependency in the Domain layer.
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <param name="session">The transaction session (IClientSessionHandle in MongoDB implementation)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>The created entity with generated Id and timestamps</returns>
    Task<T> CreateWithSessionAsync(T entity, object session, CancellationToken cancellationToken = default);
}
