namespace ClaudePlayground.Infrastructure.Persistence.Mapping;

public static class MongoDbMappingConfiguration
{
    private static bool _isConfigured;
    private static readonly object _lock = new();

    public static void Configure()
    {
        // Ensure mappings are only registered once
        lock (_lock)
        {
            if (_isConfigured)
                return;

            // Register value objects first (they may be used in entities)
            AddressMap.Configure();
            RoleMap.Configure();

            // Register entities
            BusinessMap.Configure();
            UserMap.Configure();
            RefreshTokenMap.Configure();

            _isConfigured = true;
        }
    }
}
