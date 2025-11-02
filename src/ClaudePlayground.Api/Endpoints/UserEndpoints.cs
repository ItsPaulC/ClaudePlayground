using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;
using FluentValidation;
using ZiggyCreatures.Caching.Fusion;

namespace ClaudePlayground.Api.Endpoints;

public static class UserEndpoints
{
    // Cache key constants
    private const string AllUsersCacheKey = "users:all";
    private const string UserByIdCacheKeyPrefix = "users:id:";
    private const string CurrentUserCacheKeyPrefix = "users:me:";

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // Get All Users - SuperUser and BusinessOwner only
        group.MapGet("/", async (IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            try
            {
                // Include tenant in cache key to ensure tenant isolation
                string tenantId = tenantProvider.GetTenantId() ?? "global";
                string cacheKey = $"{AllUsersCacheKey}:{tenantId}";

                IEnumerable<UserDto> users = await cache.GetOrSetAsync<IEnumerable<UserDto>>(
                    cacheKey,
                    async (ctx, ct) => await service.GetAllAsync(ct),
                    new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(24) },
                    ct
                );

                return Results.Ok(users);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("GetAllUsers")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Get Paginated Users - SuperUser and BusinessOwner only
        group.MapGet("/paged", async (int page, int pageSize, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            try
            {
                // Include tenant, page, and pageSize in cache key for tenant isolation and pagination
                string tenantId = tenantProvider.GetTenantId() ?? "global";
                string cacheKey = $"users:paged:{tenantId}:{page}:{pageSize}";

                PagedResult<UserDto> users = await cache.GetOrSetAsync<PagedResult<UserDto>>(
                    cacheKey,
                    async (ctx, ct) => await service.GetPagedAsync(page, pageSize, ct),
                    new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) }, // Shorter cache for paginated results
                    ct
                );

                return Results.Ok(users);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("GetPagedUsers")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Get Current User - Any authenticated user can get their own information
        group.MapGet("/me", async (IUserService service, IFusionCache cache, ICurrentUserService currentUserService, CancellationToken ct) =>
        {
            string? userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.NotFound();
            }

            string cacheKey = $"{CurrentUserCacheKeyPrefix}{userId}";

            var result = await cache.GetOrSetAsync<Result<UserDto>>(
                cacheKey,
                async (ctx, ct) => await service.GetMeAsync(ct),
                new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(24) },
                ct
            );

            return result.Match(
                onSuccess: user => Results.Ok(user),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("GetMe")
        .WithOpenApi();

        // Get User by ID - SuperUser and BusinessOwner can view users, regular users can only view themselves
        group.MapGet("/{id}", async (string id, IUserService service, IFusionCache cache, CancellationToken ct) =>
        {
            string cacheKey = $"{UserByIdCacheKeyPrefix}{id}";

            var result = await cache.GetOrSetAsync<Result<UserDto>>(
                cacheKey,
                async (ctx, ct) => await service.GetByIdAsync(id, ct),
                new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(24) },
                ct
            );

            return result.Match(
                onSuccess: user => Results.Ok(user),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Forbidden => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("GetUserById")
        .WithOpenApi();

        // Create User - Super-user can create any role, BusinessOwner can create User/ReadOnlyUser in their tenant
        group.MapPost("/", async (CreateUserDto dto, IValidator<CreateUserDto> validator, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await service.CreateAsync(dto, null, ct);

            return result.Match(
                onSuccess: user =>
                {
                    // Invalidate cache
                    string tenantId = tenantProvider.GetTenantId() ?? "global";
                    cache.Remove($"{AllUsersCacheKey}:{tenantId}");
                    return Results.Created($"/api/users/{user.Id}", user);
                },
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                    ErrorType.Unauthorized => Results.Forbid(),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("CreateUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Create User in Specific Tenant - Super-user only (cross-tenant user creation)
        group.MapPost("/tenant/{tenantId}", async (string tenantId, CreateUserDto dto, IValidator<CreateUserDto> validator, IUserService service, IFusionCache cache, CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await service.CreateAsync(dto, tenantId, ct);

            return result.Match(
                onSuccess: user =>
                {
                    // Invalidate cache for the target tenant
                    cache.Remove($"{AllUsersCacheKey}:{tenantId}");
                    return Results.Created($"/api/users/{user.Id}", user);
                },
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                    ErrorType.Unauthorized => Results.Forbid(),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("CreateUserInTenant")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Update User - Super-user (any) or BusinessOwner (own tenant only)
        group.MapPut("/{id}", async (string id, UpdateUserDto dto, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);

            return result.Match(
                onSuccess: user =>
                {
                    // Invalidate cache
                    string tenantId = tenantProvider.GetTenantId() ?? "global";
                    cache.Remove($"{AllUsersCacheKey}:{tenantId}");
                    cache.Remove($"{UserByIdCacheKeyPrefix}{id}");
                    cache.Remove($"{CurrentUserCacheKeyPrefix}{id}");
                    return Results.Ok(user);
                },
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Unauthorized => Results.Forbid(),
                    ErrorType.Forbidden => Results.Forbid(),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Delete User - Super-user (any) or BusinessOwner (own tenant only)
        group.MapDelete("/{id}", async (string id, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);

            return result.Match(
                onSuccess: () =>
                {
                    // Invalidate cache
                    string tenantId = tenantProvider.GetTenantId() ?? "global";
                    cache.Remove($"{AllUsersCacheKey}:{tenantId}");
                    cache.Remove($"{UserByIdCacheKeyPrefix}{id}");
                    cache.Remove($"{CurrentUserCacheKeyPrefix}{id}");
                    return Results.NoContent();
                },
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Unauthorized => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        return app;
    }
}
