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
}
