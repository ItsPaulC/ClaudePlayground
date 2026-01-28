using System.Linq.Expressions;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Infrastructure.Persistence;
using MongoDB.Driver;

namespace ClaudePlayground.Infrastructure.Repositories;

public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(MongoDbContext context)
    {
        string collectionName = typeof(T).Name.ToLower() + "s";
        _collection = context.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = Builders<T>.Filter.Eq(e => e.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // WARNING: This method loads ALL entities across ALL tenants
        // Should only be used by SuperUser operations
        // Use GetAllByTenantAsync for tenant-scoped queries
        return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        FilterDefinition<T> filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = Builders<T>.Filter.Eq(e => e.Id, id);
        DeleteResult result = await _collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    // Tenant-scoped methods for secure multi-tenancy
    public async Task<T?> GetByIdAndTenantAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.TenantId, tenantId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = Builders<T>.Filter.Eq(e => e.TenantId, tenantId);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    // Generic filter methods for custom queries
    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    // Session-aware method for transaction support
    public async Task<T> CreateWithSessionAsync(T entity, object session, CancellationToken cancellationToken = default)
    {
        if (session is not IClientSessionHandle clientSession)
        {
            throw new ArgumentException("Session must be an IClientSessionHandle", nameof(session));
        }

        entity.CreatedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(clientSession, entity, cancellationToken: cancellationToken);
        return entity;
    }

    // Pagination methods for efficient data retrieval with sorting and filtering
    public async Task<PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        // Validate and normalize parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size to prevent excessive memory usage

        // Build filter
        FilterDefinition<T> filterDefinition = filter != null
            ? Builders<T>.Filter.Where(filter)
            : Builders<T>.Filter.Empty;

        // Get total count
        var totalCount = await _collection.CountDocumentsAsync(filterDefinition, cancellationToken: cancellationToken);

        // Calculate skip
        var skip = (page - 1) * pageSize;

        // Build query with optional sorting
        var query = _collection.Find(filterDefinition);

        if (!string.IsNullOrEmpty(sortBy))
        {
            var sortDefinition = sortDescending
                ? Builders<T>.Sort.Descending(sortBy)
                : Builders<T>.Sort.Ascending(sortBy);
            query = query.Sort(sortDefinition);
        }

        // Get paginated items
        var items = await query
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            Items: items,
            TotalCount: (int)totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<PagedResult<T>> GetPagedByTenantAsync(
        string tenantId,
        int page,
        int pageSize,
        string? sortBy = null,
        bool sortDescending = false,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        // Validate and normalize parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max page size to prevent excessive memory usage

        // Build filter combining tenant filter with optional additional filter
        FilterDefinition<T> tenantFilter = Builders<T>.Filter.Eq(e => e.TenantId, tenantId);
        FilterDefinition<T> finalFilter = filter != null
            ? Builders<T>.Filter.And(tenantFilter, Builders<T>.Filter.Where(filter))
            : tenantFilter;

        // Get total count for tenant with filters
        var totalCount = await _collection.CountDocumentsAsync(finalFilter, cancellationToken: cancellationToken);

        // Calculate skip
        var skip = (page - 1) * pageSize;

        // Build query with optional sorting
        var query = _collection.Find(finalFilter);

        if (!string.IsNullOrEmpty(sortBy))
        {
            var sortDefinition = sortDescending
                ? Builders<T>.Sort.Descending(sortBy)
                : Builders<T>.Sort.Ascending(sortBy);
            query = query.Sort(sortDefinition);
        }

        // Get paginated items for tenant
        var items = await query
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            Items: items,
            TotalCount: (int)totalCount,
            Page: page,
            PageSize: pageSize
        );
    }
}
