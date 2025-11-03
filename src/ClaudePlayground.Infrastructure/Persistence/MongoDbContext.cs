using ClaudePlayground.Domain.Entities;
using ClaudePlayground.Infrastructure.Configuration;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace ClaudePlayground.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        // Configure MongoDB client with OpenTelemetry instrumentation
        MongoClientSettings clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());

        _client = new MongoClient(clientSettings);
        _database = _client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    public async Task<IClientSessionHandle> StartSessionAsync(CancellationToken cancellationToken = default)
    {
        return await _client.StartSessionAsync(cancellationToken: cancellationToken);
    }

    public async Task EnsureIndexesAsync()
    {
        // User collection indexes
        IMongoCollection<User> usersCollection = GetCollection<User>("users");

        // Unique index on email
        IndexKeysDefinition<User> emailKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
        CreateIndexModel<User> emailIndex = new(
            emailKeys,
            new CreateIndexOptions
            {
                Unique = true,
                Name = "email_unique"
            }
        );

        // Index on TenantId for tenant-scoped queries
        IndexKeysDefinition<User> userTenantKeys = Builders<User>.IndexKeys.Ascending(u => u.TenantId);
        CreateIndexModel<User> userTenantIndex = new(
            userTenantKeys,
            new CreateIndexOptions { Name = "tenantId_idx" }
        );

        // Index on EmailVerificationToken for email verification lookups
        IndexKeysDefinition<User> emailVerificationTokenKeys = Builders<User>.IndexKeys.Ascending(u => u.EmailVerificationToken);
        CreateIndexModel<User> emailVerificationTokenIndex = new(
            emailVerificationTokenKeys,
            new CreateIndexOptions
            {
                Name = "emailVerificationToken_idx",
                Sparse = true // Only index documents where this field exists
            }
        );

        // Index on PasswordResetToken for password reset lookups
        IndexKeysDefinition<User> passwordResetTokenKeys = Builders<User>.IndexKeys.Ascending(u => u.PasswordResetToken);
        CreateIndexModel<User> passwordResetTokenIndex = new(
            passwordResetTokenKeys,
            new CreateIndexOptions
            {
                Name = "passwordResetToken_idx",
                Sparse = true // Only index documents where this field exists
            }
        );

        await usersCollection.Indexes.CreateManyAsync(
        [
            emailIndex,
            userTenantIndex,
            emailVerificationTokenIndex,
            passwordResetTokenIndex
        ]);

        // Business collection indexes
        IMongoCollection<Business> businessCollection = GetCollection<Business>("businesses");

        // Index on TenantId for tenant-scoped queries
        IndexKeysDefinition<Business> businessTenantKeys = Builders<Business>.IndexKeys.Ascending(b => b.TenantId);
        CreateIndexModel<Business> businessTenantIndex = new(
            businessTenantKeys,
            new CreateIndexOptions { Name = "tenantId_idx" }
        );

        await businessCollection.Indexes.CreateOneAsync(businessTenantIndex);

        // RefreshToken collection indexes
        IMongoCollection<RefreshToken> refreshTokenCollection = GetCollection<RefreshToken>("refreshtokens");

        // Index on Token for token lookup
        IndexKeysDefinition<RefreshToken> tokenKeys = Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token);
        CreateIndexModel<RefreshToken> tokenIndex = new(
            tokenKeys,
            new CreateIndexOptions { Name = "token_idx" }
        );

        // Index on UserId for user-specific token queries
        IndexKeysDefinition<RefreshToken> userIdKeys = Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.UserId);
        CreateIndexModel<RefreshToken> userIdIndex = new(
            userIdKeys,
            new CreateIndexOptions { Name = "userId_idx" }
        );

        // Compound index on UserId and IsRevoked for efficient "get active tokens for user" queries
        IndexKeysDefinition<RefreshToken> userIdRevokedKeys = Builders<RefreshToken>.IndexKeys
            .Ascending(rt => rt.UserId)
            .Ascending(rt => rt.IsRevoked);
        CreateIndexModel<RefreshToken> userIdRevokedIndex = new(
            userIdRevokedKeys,
            new CreateIndexOptions { Name = "userId_isRevoked_idx" }
        );

        await refreshTokenCollection.Indexes.CreateManyAsync(
        [
            tokenIndex,
            userIdIndex,
            userIdRevokedIndex
        ]);
    }
}
