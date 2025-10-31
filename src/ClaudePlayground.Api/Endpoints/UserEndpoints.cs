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

        // Get Current User - Any authenticated user can get their own information
        group.MapGet("/me", async (IUserService service, IFusionCache cache, ICurrentUserService currentUserService, CancellationToken ct) =>
        {
            string? userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.NotFound();
            }

            string cacheKey = $"{CurrentUserCacheKeyPrefix}{userId}";

            UserDto? user = await cache.GetOrSetAsync<UserDto?>(
                cacheKey,
                async (ctx, ct) => await service.GetMeAsync(ct),
                new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(24) },
                ct
            );

            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetMe")
        .WithOpenApi();

        // Get User by ID - SuperUser and BusinessOwner can view users, regular users can only view themselves
        group.MapGet("/{id}", async (string id, IUserService service, IFusionCache cache, CancellationToken ct) =>
        {
            string cacheKey = $"{UserByIdCacheKeyPrefix}{id}";

            UserDto? user = await cache.GetOrSetAsync<UserDto?>(
                cacheKey,
                async (ctx, ct) => await service.GetByIdAsync(id, ct),
                new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(24) },
                ct
            );

            return user is not null ? Results.Ok(user) : Results.NotFound();
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

            try
            {
                UserDto user = await service.CreateAsync(dto, null, ct);

                // Invalidate cache
                string tenantId = tenantProvider.GetTenantId() ?? "global";
                await cache.RemoveAsync($"{AllUsersCacheKey}:{tenantId}", token: ct);

                return Results.Created($"/api/users/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
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

            try
            {
                UserDto user = await service.CreateAsync(dto, tenantId, ct);

                // Invalidate cache for the target tenant
                await cache.RemoveAsync($"{AllUsersCacheKey}:{tenantId}", token: ct);

                return Results.Created($"/api/users/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateUserInTenant")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Update User - Super-user (any) or BusinessOwner (own tenant only)
        group.MapPut("/{id}", async (string id, UpdateUserDto dto, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            try
            {
                UserDto user = await service.UpdateAsync(id, dto, ct);

                // Invalidate cache
                string tenantId = tenantProvider.GetTenantId() ?? "global";
                await cache.RemoveAsync($"{AllUsersCacheKey}:{tenantId}", token: ct);
                await cache.RemoveAsync($"{UserByIdCacheKeyPrefix}{id}", token: ct);
                await cache.RemoveAsync($"{CurrentUserCacheKeyPrefix}{id}", token: ct);

                return Results.Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Delete User - Super-user (any) or BusinessOwner (own tenant only)
        group.MapDelete("/{id}", async (string id, IUserService service, IFusionCache cache, ITenantProvider tenantProvider, CancellationToken ct) =>
        {
            bool deleted = await service.DeleteAsync(id, ct);

            if (deleted)
            {
                // Invalidate cache
                string tenantId = tenantProvider.GetTenantId() ?? "global";
                await cache.RemoveAsync($"{AllUsersCacheKey}:{tenantId}", token: ct);
                await cache.RemoveAsync($"{UserByIdCacheKeyPrefix}{id}", token: ct);
                await cache.RemoveAsync($"{CurrentUserCacheKeyPrefix}{id}", token: ct);
            }

            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        return app;
    }
}
