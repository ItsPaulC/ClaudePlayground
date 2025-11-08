// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ClaudePlayground.Api.Settings;

namespace ClaudePlayground.Api.Extensions;

public static class HealthCheckExtensions
{
    public static void AddHealthChecks(
        this IServiceCollection services,
        MongoDbSettings mongoDbSettings,
        RedisSettings redisSettings)
    {

        services.AddHealthChecks()
            .AddMongoDb(
                clientFactory: _ => new MongoDB.Driver.MongoClient(mongoDbSettings.ConnectionString),
                databaseNameFactory: _ => "ClaudePlayground",
                name: "mongodb",
                tags: ["ready", "db"])
            .AddRedis(redisSettings.ConnectionString ?? "localhost:6379",
                name: "redis",
                tags: ["ready", "cache"]);
    }
}
