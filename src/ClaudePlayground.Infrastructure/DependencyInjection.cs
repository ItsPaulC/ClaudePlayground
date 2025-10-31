using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Application.Services;
using ClaudePlayground.Domain.Common;
using ClaudePlayground.Infrastructure.Configuration;
using ClaudePlayground.Infrastructure.Persistence;
using ClaudePlayground.Infrastructure.Repositories;
using ClaudePlayground.Infrastructure.Services;
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

        // JWT Configuration
        JwtSettings jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? new();
        services.AddSingleton(jwtSettings);

        // Multi-Tenancy
        services.AddScoped<ITenantProvider, HttpTenantProvider>();

        // Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Email Service
        services.AddScoped<IEmailService, ConsoleEmailService>();

        // Repository Registration
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // Application Services
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
