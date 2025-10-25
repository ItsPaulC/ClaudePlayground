using ClaudePlayground.Infrastructure.Configuration;
using ClaudePlayground.Infrastructure.Persistence;
using Testcontainers.MongoDb;

namespace ClaudePlayground.Infrastructure.Tests.Integration.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    public MongoDbContext MongoDbContext { get; private set; } = null!;
    public string ConnectionString => _mongoContainer.GetConnectionString();

    public MongoDbFixture()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        MongoDbSettings settings = new()
        {
            ConnectionString = _mongoContainer.GetConnectionString(),
            DatabaseName = "TestDatabase"
        };

        MongoDbContext = new MongoDbContext(settings);
    }

    public async ValueTask DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}
