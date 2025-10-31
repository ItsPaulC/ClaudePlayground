using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Infrastructure.Configuration;
using MongoDB.Driver;

namespace ClaudePlayground.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        MongoClient client = new(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    public async Task EnsureIndexesAsync()
    {
        // Create unique index on User email field
        IMongoCollection<User> usersCollection = GetCollection<User>("users");

        IndexKeysDefinition<User> keys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        CreateIndexModel<User> indexModel = new(
            keys,
            new CreateIndexOptions
            {
                Unique = true,
                Name = "email_unique"
            }
        );

        await usersCollection.Indexes.CreateOneAsync(indexModel);
    }
}
