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
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Get Paginated Businesses - Authenticated users (tenant-scoped for non-SuperUsers)
        group.MapGet("/paged", async (int page, int pageSize, IBusinessService service, CancellationToken ct) =>
        {
            PagedResult<BusinessDto> businesses = await service.GetPagedAsync(page, pageSize, ct);
            return Results.Ok(businesses);
        })
        .WithName("GetPagedBusinesses")
        .WithOpenApi();

        // Get Business by ID - Authenticated users (tenant-scoped)
        group.MapGet("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result.Match(
                onSuccess: business => Results.Ok(business),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("GetBusinessById")
        .WithOpenApi();

        app.MapPost("/api/businesses/with-user", async (CreateBusinessWithUserDto dto, IBusinessService service, CancellationToken ct) =>
        {
            var result = await service.CreateWithUserAsync(dto, ct);
            return result.Match(
                onSuccess: businessWithUser => Results.Created($"/api/businesses/{businessWithUser.Business.Id}", businessWithUser),
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error = error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error = error.Message }),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("CreateBusinessWithUser")
        .WithOpenApi()
        .WithTags("Businesses")
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Create Business - Super-user only
        group.MapPost("/", async (CreateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(dto, ct);
            return result.Match(
                onSuccess: business => Results.Created($"/api/businesses/{business.Id}", business),
                onFailure: error => Results.BadRequest(new { error = error.Message }));
        })
        .WithName("CreateBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        // Update Business - Super-user (any) or BusinessOwner (own tenant only)
        group.MapPut("/{id}", async (string id, UpdateBusinessDto dto, IBusinessService service, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, dto, ct);
            return result.Match(
                onSuccess: business => Results.Ok(business),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Forbidden => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("UpdateBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue, Roles.BusinessOwnerValue));

        // Delete Business - Super-user only
        group.MapDelete("/{id}", async (string id, IBusinessService service, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ct);
            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error = error.Message }),
                    ErrorType.Forbidden => Results.Forbid(),
                    _ => Results.BadRequest(new { error = error.Message })
                });
        })
        .WithName("DeleteBusiness")
        .WithOpenApi()
        .RequireAuthorization(policy => policy.RequireRole(Roles.SuperUserValue));

        return app;
    }
}
