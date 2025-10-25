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
}
