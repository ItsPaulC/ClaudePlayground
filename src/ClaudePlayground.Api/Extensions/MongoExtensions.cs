// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ClaudePlayground.Infrastructure.Persistence;

namespace ClaudePlayground.Api.Extensions;

public static class MongoExtensions
{
    /// <summary>
    /// Ensures MongoDB indexes are created during application startup.
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task MongoEnsureIndexesAreCreated(this WebApplication app)
    {
        using IServiceScope? scope = app.Services.CreateScope();

        if (scope?.ServiceProvider == null)
        {
            throw new InvalidOperationException("Failed to create service scope for MongoDB initialization.");
        }

        MongoDbContext dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

        if (dbContext == null)
        {
            throw new InvalidOperationException("Failed to resolve MongoDbContext from service provider.");
        }

        await dbContext.EnsureIndexesAsync(CancellationToken.None);
    }
}
