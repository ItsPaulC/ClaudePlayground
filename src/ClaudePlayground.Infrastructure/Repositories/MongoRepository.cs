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
}
