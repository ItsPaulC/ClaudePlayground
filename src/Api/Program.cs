using System.Threading.RateLimiting;
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

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add rate limiting to protect against brute-force and DoS attacks
builder.Services.AddRateLimiter(options =>
{
    // Strict rate limiting for authentication endpoints (login, register, password reset)
    // Prevents brute-force attacks and credential stuffing
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // 5 requests
                Window = TimeSpan.FromMinutes(1), // per minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing - reject immediately when limit exceeded
            }));

    // Moderate rate limiting for general API endpoints
    // Prevents DoS attacks while allowing legitimate high-volume usage
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous"
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100, // 100 requests
                Window = TimeSpan.FromMinutes(1), // per minute
                SegmentsPerWindow = 4, // Smoother rate limiting with 4 segments
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10 // Allow small queue for burst traffic
            }));

    // Strict rate limiting for password reset to prevent abuse
    options.AddPolicy("password-reset", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3, // 3 requests
                Window = TimeSpan.FromMinutes(15), // per 15 minutes
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No queuing
            }));

    // Global fallback rate limiter for unconfigured endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partition by IP address for anonymous requests, user ID for authenticated
        string partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });

    // Custom response when rate limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Rate limit exceeded. Please try again later.",
            retryAfter = retryAfter.TotalSeconds
        }, cancellationToken);
    };
});

// Configure request size limits to prevent DoS attacks via large payloads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB limit for file uploads
});

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB limit for request bodies
});

// Configure CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow any origin for easy local testing
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Production: Require specific allowed origins from configuration
            // Set AllowedOrigins in appsettings.json or environment variable
            string allowedOrigins = config["AllowedOrigins"] ?? "";
            if (!string.IsNullOrEmpty(allowedOrigins))
            {
                policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                      .AllowCredentials()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // No origins configured - create restrictive policy that denies all cross-origin requests
                policy.WithOrigins(); // Empty origins list = deny all CORS requests
            }
        }
    });
});

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

// Enable CORS (must be before UseRateLimiter, UseAuthentication, UseAuthorization)
app.UseCors("ApiPolicy");

// Enable rate limiting middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoint groups
app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapUserEndpoints();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.Run();
