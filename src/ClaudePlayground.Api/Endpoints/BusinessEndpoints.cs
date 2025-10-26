using ClaudePlayground.Application.DTOs;
using ClaudePlayground.Application.Interfaces;
using ClaudePlayground.Domain.Common;

namespace ClaudePlayground.Api.Endpoints;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/businesses")
            .WithTags("Businesses")
            .RequireAuthorization();

        // Get All Businesses - Super-user only (cross-tenant access)
        group.MapGet("/", async (IBusinessService service, CancellationToken ct) =>
        {
            IEnumerable<BusinessDto> businesses = await service.GetAllAsync(ct);
            return Results.Ok(businesses);
        })
        .WithName("GetAllBusinesses")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUser));

        // Get Business by ID - Authenticated users (tenant-scoped)
        group.MapGet("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            BusinessDto? business = await service.GetByIdAsync(id, ct);
            return business is not null ? Results.Ok(business) : Results.NotFound();
        })
        .WithName("GetBusinessById")
        .WithOpenApi();

        // Create Business - Super-user only
        group.MapPost("/", async (CreateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            BusinessDto business = await service.CreateAsync(dto, ct);
            return Results.Created($"/api/businesses/{business.Id}", business);
        })
        .WithName("CreateBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUser));

        // Update Business - Super-user (any) or Admin (own tenant only)
        group.MapPut("/{id}", async (string id, UpdateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            try
            {
                BusinessDto business = await service.UpdateAsync(id, dto, ct);
                return Results.Ok(business);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .WithName("UpdateBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUser, Roles.Admin));

        // Delete Business - Super-user only
        group.MapDelete("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            bool deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUser));

        return app;
    }
}
