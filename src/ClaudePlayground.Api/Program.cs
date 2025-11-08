using System.Text;
using ClaudePlayground.Api.Endpoints;
using ClaudePlayground.Api.Extensions;
using ClaudePlayground.Api.Settings;
using ClaudePlayground.Application;
using ClaudePlayground.Application.Configuration;
using ClaudePlayground.Infrastructure;
using ClaudePlayground.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (observability, service discovery, resilience)
builder.AddServiceDefaults();
ConfigurationManager config = builder.Configuration;
config.BindConfigSection(out RedisSettings redisSettings);

// Add health checks for MongoDB and Redis
config.BindConfigSection(out  MongoDbSettings mongoDbSettings);
builder.Services.AddHealthChecks(mongoDbSettings, redisSettings);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application (includes validators)
builder.Services.AddApplication();

// Add Infrastructure (includes Application services and MongoDB)
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT Authentication
config.BindConfigSection(out JwtSettings jwtSettings);

// Allow JWT secret to be overridden by environment variable for production security
string jwtSecretKey = config["JWT_SECRET_KEY"] ?? jwtSettings.SecretKey;

// Validate JWT secret key with strong cryptographic requirements
if (string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        "JWT Secret Key is required. " +
        "Set it via 'JWT_SECRET_KEY' environment variable or 'JwtSettings:SecretKey' in appsettings.");
}

if (jwtSecretKey.Length < 64)
{
    throw new InvalidOperationException(
        $"JWT Secret Key must be at least 64 characters long for production use (current length: {jwtSecretKey.Length}). " +
        "Set a secure random key via 'JWT_SECRET_KEY' environment variable.");
}

// Check for placeholder values
if (jwtSecretKey.Contains("REPLACE", StringComparison.OrdinalIgnoreCase) ||
    jwtSecretKey.Contains("change-this", StringComparison.OrdinalIgnoreCase) ||
    jwtSecretKey.Contains("example", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "JWT Secret Key contains placeholder text and has not been properly configured. " +
        "Set a secure random key via 'JWT_SECRET_KEY' environment variable.");
}

// Check for sufficient entropy (at least 16 distinct characters for 64+ char key)
int distinctChars = jwtSecretKey.Distinct().Count();
if (distinctChars < 16)
{
    throw new InvalidOperationException(
        $"JWT Secret Key must be cryptographically random with sufficient entropy " +
        $"(found only {distinctChars} distinct characters, need at least 16). " +
        "Generate a secure random key using a cryptographic random number generator.");
}

// Warn if key appears to have low complexity (too many repeated characters)
var charGroups = jwtSecretKey.GroupBy(c => c).OrderByDescending(g => g.Count()).First();
if (charGroups.Count() > jwtSecretKey.Length / 4)
{
    throw new InvalidOperationException(
        $"JWT Secret Key has insufficient randomness (character '{charGroups.Key}' appears {charGroups.Count()} times). " +
        "Generate a secure random key using a cryptographic random number generator.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorization();

// Configure Redis
// Support both Aspire connection strings and traditional appsettings.json
string redisConnectionString = config.GetConnectionString("redis")
    ?? redisSettings.ConnectionString
    ?? "localhost:6379";


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

WebApplication app = builder.Build();

// Ensure MongoDB indexes are created
using (IServiceScope? scope = app.Services.CreateScope())
{
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoint groups
app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapUserEndpoints();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.Run();
