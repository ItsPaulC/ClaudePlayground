using ClaudePlayground.Api.Endpoints;
using ClaudePlayground.Api.Extensions;
using ClaudePlayground.Api.Settings;
using ClaudePlayground.Application;
using ClaudePlayground.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (observability, service discovery, resilience)
builder.AddServiceDefaults();
ConfigurationManager config = builder.Configuration;

config.BindConfigSection(out RedisSettings redisSettings);

// Add health checks for MongoDB and Redis
config.BindConfigSection(out MongoDbSettings mongoDbSettings);
builder.Services.AddHealthChecks(mongoDbSettings, redisSettings);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application (includes validators)
builder.Services.AddApplication();

// Add Infrastructure (includes Application services and MongoDB)
builder.Services.AddInfrastructure(builder.Configuration);

builder.ConfigureJwtAuthenticationAndAuthorization();

// Configure Redis and FusionCache
// Support both Aspire connection strings and traditional appsettings.json
string redisConnectionString = redisSettings.ConnectionString
    ?? "localhost:6379";

builder.ConfigureRedis(redisSettings, redisConnectionString);

WebApplication app = builder.Build();

// Ensure MongoDB indexes are created
await app.MongoEnsureIndexesAreCreated();

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
