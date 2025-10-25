using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Services;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Infrastructure.Configuration;
using ClaudePlayground.Infrastructure.Persistence;
using ClaudePlayground.Infrastructure.Repositories;
using ClaudePlayground.Infrastructure.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudePlayground.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Context Accessor for multi-tenancy
        services.AddHttpContextAccessor();

        // MongoDB Configuration
        MongoDbSettings mongoSettings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()
            ?? new();
        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoDbContext>();

        // Multi-Tenancy
        services.AddScoped<ITenantProvider, HttpTenantProvider>();

        // Repository Registration
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // Application Services
        services.AddScoped<IBusinessService, BusinessService>();

        return services;
    }
}
