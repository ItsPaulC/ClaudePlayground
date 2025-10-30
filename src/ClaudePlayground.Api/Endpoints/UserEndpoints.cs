using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        // Get All Users - Super-user can see all, others see only their tenant
        group.MapGet("/", async (IUserService service, CancellationToken ct) =>
        {
            IEnumerable<UserDto> users = await service.GetAllAsync(ct);
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Get User by ID - Super-user or users in the same tenant
        group.MapGet("/{id}", async (string id, IUserService service, CancellationToken ct) =>
        {
            UserDto? user = await service.GetByIdAsync(id, ct);
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetUserById")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Create User - Super-user can create any role, BusinessOwner can create User/ReadOnlyUser in their tenant
        group.MapPost("/", async (CreateUserDto dto, IUserService service, CancellationToken ct) =>
        {
            try
            {
                UserDto user = await service.CreateAsync(dto, null, ct);
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
        group.MapPost("/tenant/{tenantId}", async (string tenantId, CreateUserDto dto, IUserService service, CancellationToken ct) =>
        {
            try
            {
                UserDto user = await service.CreateAsync(dto, tenantId, ct);
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
        group.MapPut("/{id}", async (string id, UpdateUserDto dto, IUserService service, CancellationToken ct) =>
        {
            try
            {
                UserDto user = await service.UpdateAsync(id, dto, ct);
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
        group.MapDelete("/{id}", async (string id, IUserService service, CancellationToken ct) =>
        {
            bool deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteUser")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        return app;
    }
}
