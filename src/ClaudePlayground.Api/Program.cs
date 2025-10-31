using System.Text;
using ClaudePlayground.Api.Endpoints;
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

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application (includes validators)
builder.Services.AddApplication();

// Add Infrastructure (includes Application services and MongoDB)
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT Authentication
JwtSettings jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// Configure Redis
string redisConnectionString = builder.Configuration["RedisSettings:ConnectionString"] ?? "localhost:6379";
string redisInstanceName = builder.Configuration["RedisSettings:InstanceName"] ?? "ClaudePlayground:";

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Add FusionCache with Redis
builder.Services.AddFusionCache()
    .WithSerializer(
        new FusionCacheSystemTextJsonSerializer()
    )
    .WithDistributedCache(
        new RedisCache(new RedisCacheOptions
        {
            Configuration = redisConnectionString,
            InstanceName = redisInstanceName
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
using (IServiceScope scope = app.Services.CreateScope())
{
    MongoDbContext dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await dbContext.EnsureIndexesAsync();
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

app.Run();
