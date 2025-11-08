// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ClaudePlayground.Api.Settings;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace ClaudePlayground.Api.Extensions;

public static class RedisExtensions
{
    /// <summary>
    /// Configures Redis connection and FusionCache with distributed caching and backplane support.
    /// </summary>
    /// <param name="builder">The web application builder</param>
    /// <param name="redisSettings">Redis configuration settings</param>
    /// <param name="redisConnectionString">The Redis connection string (from config or Aspire)</param>
    public static void ConfigureRedis(
        this WebApplicationBuilder builder,
        RedisSettings redisSettings,
        string redisConnectionString)
    {
        // Connect to Redis with error handling
        IConnectionMultiplexer redis;
        try
        {
            redis = ConnectionMultiplexer.Connect(redisConnectionString);

            if (redis == null || !redis.IsConnected)
            {
                throw new InvalidOperationException("Redis connection was established but is not connected.");
            }

            builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Redis at '{redisConnectionString}'. " +
                "Ensure Redis is running and accessible. " +
                $"Error: {ex.Message}", ex);
        }

        // Add FusionCache with Redis
        builder.Services.AddFusionCache()
            .WithSerializer(
                new FusionCacheSystemTextJsonSerializer()
            )
            .WithDistributedCache(
                new RedisCache(new RedisCacheOptions
                {
                    Configuration = redisConnectionString,
                    InstanceName = redisSettings.InstanceName
                })
            )
            .WithBackplane(
                new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = redisConnectionString
                })
            );
    }
}
